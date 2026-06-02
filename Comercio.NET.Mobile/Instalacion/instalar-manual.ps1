# ============================================================
# Script de Instalacion Manual - Comercio.NET Web
# Ejecutar como Administrador
# Uso: PowerShell -ExecutionPolicy Bypass -File instalar-manual.ps1
# ============================================================

$ErrorActionPreference = "Stop"
$RutaInstalacion = "C:\ComercioWeb"
$pgHost     = "localhost"
$pgPort     = "5432"
$pgDatabase = "comercio"
$pgUser     = "postgres"
$pgPassword = "michael"

function Write-OK($t)    { Write-Host "    [OK] $t" -ForegroundColor Green }
function Write-Aviso($t) { Write-Host "    [!]  $t" -ForegroundColor DarkYellow }
function Write-Err($t)   { Write-Host "    [ERROR] $t" -ForegroundColor Red }

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  INSTALACION MANUAL COMERCIO.NET WEB" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# ------------------------------------------------------------
# PASO 1: Verificar .NET 8
# ------------------------------------------------------------
Write-Host "[1] Verificando .NET 8 Runtime..." -ForegroundColor Yellow
$dotnetOk = $false
try {
    $runtimes = & dotnet --list-runtimes 2>$null
    if ($runtimes -match "Microsoft.AspNetCore.App 8") { $dotnetOk = $true }
} catch {}

if ($dotnetOk) {
    Write-OK ".NET 8 ASP.NET Core Runtime detectado"
} else {
    Write-Err ".NET 8 Runtime NO esta instalado."
    Write-Host "    Descargue e instale el Hosting Bundle desde:" -ForegroundColor Yellow
    Write-Host "    https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
    Write-Host "    Luego REINICIE la PC y vuelva a ejecutar este script." -ForegroundColor Yellow
    exit 1
}

# ------------------------------------------------------------
# PASO 2: Verificar archivos de la aplicacion
# ------------------------------------------------------------
Write-Host "[2] Verificando archivos de la aplicacion..." -ForegroundColor Yellow
$dllPath = "$RutaInstalacion\Comercio.NET.Mobile.Server.dll"
if (-not (Test-Path $dllPath)) {
    Write-Err "No se encontro $dllPath"
    Write-Host "    Asegurese de haber copiado la carpeta 'publish' a $RutaInstalacion" -ForegroundColor Yellow
    exit 1
}
Write-OK "Archivos encontrados en $RutaInstalacion"

# ------------------------------------------------------------
# PASO 3: Crear base de datos
# ------------------------------------------------------------
Write-Host "[3] Configurando base de datos PostgreSQL..." -ForegroundColor Yellow

# Buscar psql.exe
$pgPaths = @(
    "C:\Program Files\PostgreSQL\17\bin",
    "C:\Program Files\PostgreSQL\16\bin",
    "C:\Program Files\PostgreSQL\15\bin",
    "C:\Program Files\PostgreSQL\14\bin",
    "C:\Program Files\PostgreSQL\13\bin",
    "C:\Program Files (x86)\PostgreSQL\17\bin",
    "C:\Program Files (x86)\PostgreSQL\16\bin",
    "C:\Program Files (x86)\PostgreSQL\15\bin"
)
$psql = $null
foreach ($p in $pgPaths) {
    if (Test-Path "$p\psql.exe") { $psql = "$p\psql.exe"; break }
}

# Busqueda dinamica si no se encontro en rutas conocidas
if ($null -eq $psql) {
    Write-Host "    Buscando psql.exe en el sistema (puede tardar unos segundos)..." -ForegroundColor White
    $encontrado = Get-ChildItem "C:\Program Files" -Recurse -Filter "psql.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($encontrado) { $psql = $encontrado.FullName }
}

if ($null -eq $psql) {
    Write-Aviso "No se encontro psql.exe. Omitiendo creacion de base de datos."
    Write-Host "    Ejecute manualmente el script 'crear-base-datos.sql' en PostgreSQL." -ForegroundColor Yellow
} else {
    $env:PGPASSWORD = $pgPassword

    # Crear base de datos si no existe
    $existe = & "$psql" -h $pgHost -p $pgPort -U $pgUser -tAc "SELECT 1 FROM pg_database WHERE datname='$pgDatabase'" 2>$null
    if ($existe -ne "1") {
        Write-Host "    Creando base de datos '$pgDatabase'..." -ForegroundColor White
        $createdb = (Split-Path $psql) + "\createdb.exe"
        & "$createdb" -h $pgHost -p $pgPort -U $pgUser $pgDatabase
        Write-OK "Base de datos '$pgDatabase' creada"
    } else {
        Write-OK "Base de datos '$pgDatabase' ya existe"
    }

    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $pgrestore  = (Split-Path $psql) + "\pg_restore.exe"
    $backupDump = Join-Path $scriptDir "backup-comercio.dump"
    $backupSql  = Join-Path $scriptDir "backup-comercio.sql"
    $sqlScript  = Join-Path $scriptDir "crear-base-datos.sql"

    # Usar backup en formato custom (.dump) si existe — el mas confiable
    if (Test-Path $backupDump) {
        Write-Host "    Restaurando backup completo (estructura + productos)..." -ForegroundColor White
        & "$pgrestore" -h $pgHost -p $pgPort -U $pgUser --no-owner --no-privileges -d $pgDatabase $backupDump
        Write-OK "Backup restaurado correctamente"

        # Insertar usuario admin/admin
        Write-Host "    Creando usuario admin..." -ForegroundColor White
        $sqlAdmin = "INSERT INTO usuarios (nombreusuario, nombre, apellido, passwordhash, nivel, numerocajero, activo) SELECT 'admin', 'Administrador', '', '5nfB2FE7FcuJhUbVIfULjMxVw5S2KL529WRnpwOrjX0=', 4, 1, B'1' WHERE NOT EXISTS (SELECT 1 FROM usuarios WHERE nombreusuario = 'admin');"
        & "$psql" -h $pgHost -p $pgPort -U $pgUser -d $pgDatabase -c $sqlAdmin
        Write-OK "Usuario admin creado (contrasena: admin)"

    } elseif (Test-Path $sqlScript) {
        Write-Host "    Aplicando esquema SQL minimo..." -ForegroundColor White
        & "$psql" -h $pgHost -p $pgPort -U $pgUser -d $pgDatabase -f $sqlScript
        Write-OK "Esquema aplicado correctamente"
    } else {
        Write-Aviso "No se encontro backup ni esquema SQL junto al script."
    }

    $env:PGPASSWORD = ""
}

# ------------------------------------------------------------
# PASO 4: Registrar servicio de Windows con NSSM
# ------------------------------------------------------------
Write-Host "[4] Registrando servicio de Windows..." -ForegroundColor Yellow

# Obtener dotnet.exe
$dotnetExe = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (-not $dotnetExe) {
    Write-Err "No se pudo localizar dotnet.exe en el PATH."
    exit 1
}
Write-OK "dotnet.exe encontrado en: $dotnetExe"

# Descargar o reusar NSSM
$nssmExe = "$RutaInstalacion\nssm.exe"
if (-not (Test-Path $nssmExe)) {
    Write-Host "    Descargando NSSM..." -ForegroundColor White
    $zipPath = "$env:TEMP\nssm.zip"
    try {
        Invoke-WebRequest -Uri "https://nssm.cc/release/nssm-2.24.zip" -OutFile $zipPath -UseBasicParsing
        Expand-Archive $zipPath -DestinationPath "$env:TEMP\nssm_extract" -Force
        Copy-Item "$env:TEMP\nssm_extract\nssm-2.24\win64\nssm.exe" $nssmExe
        Write-OK "NSSM descargado"
    } catch {
        Write-Err "No se pudo descargar NSSM. Verifique la conexion a internet."
        exit 1
    }
} else {
    Write-OK "NSSM ya disponible en $nssmExe"
}

# Eliminar servicio anterior si existe
$serviceName = "ComercioNETWeb"
$svc = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($svc) {
    Write-Host "    Deteniendo y eliminando servicio anterior..." -ForegroundColor White
    Stop-Service $serviceName -Force -ErrorAction SilentlyContinue
    & $nssmExe remove $serviceName confirm
    Start-Sleep -Seconds 2
}

# Registrar servicio
Write-Host "    Registrando servicio..." -ForegroundColor White
& $nssmExe install $serviceName "$dotnetExe" "$dllPath"
& $nssmExe set $serviceName AppDirectory $RutaInstalacion
& $nssmExe set $serviceName DisplayName "Comercio.NET Web"
& $nssmExe set $serviceName Description "Servidor web del sistema de gestion comercial Comercio.NET"
& $nssmExe set $serviceName Start SERVICE_AUTO_START
& $nssmExe set $serviceName AppStdout "$RutaInstalacion\logs\servicio-stdout.log"
& $nssmExe set $serviceName AppStderr "$RutaInstalacion\logs\servicio-stderr.log"
& $nssmExe set $serviceName AppRotateFiles 1
& $nssmExe set $serviceName AppRotateBytes 5000000

Write-OK "Servicio registrado"

# Iniciar servicio
Write-Host "    Iniciando servicio..." -ForegroundColor White
Start-Sleep -Seconds 1
try {
    Start-Service $serviceName -ErrorAction Stop
    Write-OK "Servicio iniciado correctamente"
} catch {
    Write-Host ""
    Write-Err "El servicio no pudo iniciar. Revise los logs en $RutaInstalacion\logs\"
    Write-Host "    O ejecute manualmente para ver el error:" -ForegroundColor Yellow
    Write-Host "    cd $RutaInstalacion" -ForegroundColor White
    Write-Host "    dotnet Comercio.NET.Mobile.Server.dll" -ForegroundColor White
    exit 1
}

# ------------------------------------------------------------
# PASO 5: Configurar Firewall
# ------------------------------------------------------------
Write-Host "[5] Configurando Firewall..." -ForegroundColor Yellow
$ruleName = "Comercio.NET Web Puerto 8080"
$existeRegla = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
if (-not $existeRegla) {
    New-NetFirewallRule -DisplayName $ruleName `
        -Direction Inbound -Protocol TCP -LocalPort 8080 `
        -Action Allow -Profile Private,Domain | Out-Null
    Write-OK "Regla de firewall creada para puerto 8080"
} else {
    Write-OK "Regla de firewall ya existia"
}

# ------------------------------------------------------------
# RESUMEN
# ------------------------------------------------------------
Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  INSTALACION COMPLETADA" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Acceso local:  http://localhost:8080" -ForegroundColor White
$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notmatch "Loopback" } | Select-Object -First 1).IPAddress
if ($ip) {
    Write-Host "  Acceso en red: http://${ip}:8080" -ForegroundColor White
}
Write-Host ""
Write-Host "  Servicio: $serviceName" -ForegroundColor White
Write-Host "  Logs:     $RutaInstalacion\logs\" -ForegroundColor White
Write-Host ""
