#Requires -Version 5.1
<#
.SYNOPSIS
    Instalador de Comercio .NET (PostgreSQL)

.DESCRIPTION
    Instala Comercio .NET en un equipo Windows nuevo.
    - Instala .NET 8 Desktop Runtime si no esta presente.
    - Instala PostgreSQL 16 si no esta presente.
    - Configura PostgreSQL para conexiones remotas (listen_addresses = '*').
    - Abre puerto 5432 en el Firewall de Windows.
    - Restaura la BD desde comercio_inicial.dump (pg_restore)
      o ejecuta init_comercio_pg.sql como fallback DDL.
    - Descarga la ultima version desde GitHub Releases.
    - Genera appsettings.json con la IP real del servidor.
    - Crea acceso directo en el escritorio.

.PARAMETER InstallDir
    Carpeta de instalacion. Por defecto: C:\Comercio.NET

.PARAMETER GitHubRepo
    Repositorio GitHub en formato owner/repo.

.PARAMETER GitHubToken
    Token de acceso personal para repos privados (opcional).

.PARAMETER PgPassword
    Password del usuario postgres. Por defecto: michael

.PARAMETER PgPort
    Puerto de PostgreSQL. Por defecto: 5432

.EXAMPLE
    irm https://raw.githubusercontent.com/manuclaro/Comercio.NET-web/master/instalar.ps1 | iex

.EXAMPLE
    .\instalar.ps1 -InstallDir "D:\MiComercio" -PgPassword "MiClave"
#>

[CmdletBinding()]
param(
    [string]$InstallDir  = "C:\Comercio.NET",
    [string]$GitHubRepo  = "manuclaro/Comercio.NET-web",
    [string]$GitHubToken = "",
    [string]$PgPassword  = "michael",
    [int]   $PgPort      = 5432
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# CONSTANTES
# ---------------------------------------------------------------------------
$APP_NAME            = "Comercio .NET"
$APP_EXE             = "Comercio .NET.exe"
$DOTNET_VERSION      = "8.0"
$DOTNET_RUNTIME_URL  = "https://download.visualstudio.microsoft.com/download/pr/b6f19ef3-52d7-4b4b-98a7-84e9cdc82e8c/f4d27595d2b7c798d5eca2f0547f3d16/windowsdesktop-runtime-8.0.12-win-x64.exe"
$DOTNET_RUNTIME_FILE = "windowsdesktop-runtime-8.0-win-x64.exe"

$PG_VERSION          = "16"
$PG_INSTALLER_URL    = "https://get.enterprisedb.com/postgresql/postgresql-16.6-1-windows-x64.exe"
$PG_INSTALLER_FILE   = "postgresql-16-installer.exe"
$PG_DEFAULT_DATA_DIR = "C:\Program Files\PostgreSQL\$PG_VERSION\data"
$PG_BIN_CANDIDATES   = @(
    "C:\Program Files\PostgreSQL\16\bin",
    "C:\Program Files\PostgreSQL\15\bin",
    "C:\Program Files\PostgreSQL\14\bin"
)

$DB_NAME        = "comercio"
$DB_USER        = "postgres"
$DB_DUMP_FILE   = "database\comercio_inicial.dump"
$DB_INIT_SCRIPT = "database\init_comercio_pg.sql"

# ---------------------------------------------------------------------------
# HELPERS DE CONSOLA
# ---------------------------------------------------------------------------
function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host ("=" * 60) -ForegroundColor Cyan
    Write-Host "  $Text" -ForegroundColor Cyan
    Write-Host ("=" * 60) -ForegroundColor Cyan
    Write-Host ""
}
function Write-Step { param([string]$N, [string]$T) Write-Host "[$N] $T" -ForegroundColor Yellow }
function Write-OK   { param([string]$M) Write-Host "    OK  $M" -ForegroundColor Green  }
function Write-Info { param([string]$M) Write-Host "    >>  $M" -ForegroundColor Gray   }
function Write-Warn { param([string]$M) Write-Host "    !!  $M" -ForegroundColor Magenta }
function Write-Fail { param([string]$M) Write-Host "    XX  $M" -ForegroundColor Red    }

# ---------------------------------------------------------------------------
# HELPER: localizar binarios de PostgreSQL
# ---------------------------------------------------------------------------
function Find-PgBin {
    foreach ($dir in $PG_BIN_CANDIDATES) {
        if (Test-Path (Join-Path $dir "psql.exe")) { return $dir }
    }
    $found = Get-Command psql -ErrorAction SilentlyContinue
    if ($found) { return (Split-Path $found.Source) }
    return $null
}

# ---------------------------------------------------------------------------
# PASO 0 - PRIVILEGIOS DE ADMINISTRADOR
# ---------------------------------------------------------------------------
function Test-Admin {
    $cur = [Security.Principal.WindowsIdentity]::GetCurrent()
    return (New-Object Security.Principal.WindowsPrincipal($cur)).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Admin)) {
    Write-Warn "Se necesitan permisos de administrador. Reiniciando elevado..."
    $argList = @(
        "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", "`"$PSCommandPath`"",
        "-InstallDir", "`"$InstallDir`"",
        "-GitHubRepo", "`"$GitHubRepo`"",
        "-PgPassword", "`"$PgPassword`"",
        "-PgPort", $PgPort
    )
    if ($GitHubToken) { $argList += @("-GitHubToken", "`"$GitHubToken`"") }
    Start-Process powershell -Verb RunAs -ArgumentList $argList
    exit
}

Clear-Host
Write-Header "INSTALADOR DE $APP_NAME"
Write-Info "Repositorio : $GitHubRepo"
Write-Info "Destino     : $InstallDir"
Write-Info "BD          : PostgreSQL $PG_VERSION  puerto=$PgPort  db=$DB_NAME  usuario=$DB_USER"
Write-Info "Fecha/Hora  : $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')"
Write-Host ""

# ===========================================================================
# PASO 1 - .NET 8 DESKTOP RUNTIME
# ===========================================================================
Write-Step "1/9" "Verificando .NET $DOTNET_VERSION Desktop Runtime..."

$dotnetOk = $false
try {
    $runtimes = & dotnet --list-runtimes 2>$null
    if ($runtimes -match "Microsoft\.WindowsDesktop\.App $DOTNET_VERSION") { $dotnetOk = $true }
} catch { }

if ($dotnetOk) {
    Write-OK ".NET $DOTNET_VERSION Desktop Runtime ya instalado."
} else {
    Write-Info "Descargando .NET $DOTNET_VERSION Runtime (~56 MB)..."
    $tmpDotnet = Join-Path $env:TEMP $DOTNET_RUNTIME_FILE
    try {
        (New-Object System.Net.WebClient).DownloadFile($DOTNET_RUNTIME_URL, $tmpDotnet)
        Write-OK "Descarga completada."
    } catch {
        Write-Fail "Error descargando .NET Runtime: $_"
        Write-Warn "Instale manualmente: https://dotnet.microsoft.com/download/dotnet/8.0"
        Read-Host "ENTER para continuar de todos modos"
    }
    if (Test-Path $tmpDotnet) {
        $p = Start-Process -FilePath $tmpDotnet -ArgumentList "/quiet /norestart" -Wait -PassThru
        if ($p.ExitCode -eq 0 -or $p.ExitCode -eq 3010) {
            Write-OK ".NET $DOTNET_VERSION instalado."
        } else {
            Write-Warn "Instalador .NET codigo de salida: $($p.ExitCode)"
        }
        Remove-Item $tmpDotnet -Force -ErrorAction SilentlyContinue
    }
}

# ===========================================================================
# PASO 2 - POSTGRESQL
# ===========================================================================
Write-Step "2/9" "Verificando PostgreSQL..."

$pgBin = Find-PgBin

if ($pgBin) {
    Write-OK "PostgreSQL encontrado en: $pgBin"
} else {
    Write-Info "PostgreSQL no detectado. Descargando instalador de EDB (~300 MB)..."
    $tmpPg = Join-Path $env:TEMP $PG_INSTALLER_FILE
    try {
        (New-Object System.Net.WebClient).DownloadFile($PG_INSTALLER_URL, $tmpPg)
        Write-OK "Descarga completada."
    } catch {
        Write-Fail "Error descargando PostgreSQL: $_"
        Write-Warn "Descargue manualmente: https://www.postgresql.org/download/windows/"
        Read-Host "ENTER para continuar o Ctrl+C para cancelar"
    }

    if (Test-Path $tmpPg) {
        Write-Info "Instalando PostgreSQL $PG_VERSION en modo silencioso (5-10 min)..."
        $pgArgs = "--mode unattended --unattendedmodeui none " +
                  "--superpassword `"$PgPassword`" " +
                  "--serverport $PgPort " +
                  "--servicename postgresql-$PG_VERSION " +
                  "--enable-components server,commandlinetools"

        $pPg = Start-Process -FilePath $tmpPg -ArgumentList $pgArgs -Wait -PassThru
        Remove-Item $tmpPg -Force -ErrorAction SilentlyContinue

        switch ($pPg.ExitCode) {
            0    { Write-OK "PostgreSQL $PG_VERSION instalado correctamente." }
            3010 { Write-OK "PostgreSQL instalado. Reinicio requerido para completar." }
            default { Write-Warn "Instalador PostgreSQL codigo: $($pPg.ExitCode)" }
        }

        # Esperar a que el servicio inicie
        $pgSvcName = "postgresql-x64-$PG_VERSION"
        $intentos  = 0
        do {
            Start-Sleep -Seconds 3
            $intentos++
            $svc = Get-Service -Name $pgSvcName -ErrorAction SilentlyContinue
        } while (($null -eq $svc -or $svc.Status -ne 'Running') -and $intentos -lt 20)

        if ($svc -and $svc.Status -eq 'Running') {
            Write-OK "Servicio PostgreSQL listo."
        } else {
            Write-Warn "Servicio no respondio en 60 seg. Continuando de todos modos..."
        }

        $pgBin = Find-PgBin
    }
}

if (-not $pgBin) {
    Write-Fail "No se pudo localizar psql.exe. Instale PostgreSQL manualmente."
    Read-Host "ENTER para salir"
    exit 1
}

# ===========================================================================
# PASO 3 - CONFIGURAR POSTGRESQL PARA CONEXIONES REMOTAS
# ===========================================================================
Write-Step "3/9" "Configurando PostgreSQL para conexiones remotas..."

# Detectar directorio de datos
$pgDataDir = $null
if (Test-Path $PG_DEFAULT_DATA_DIR) {
    $pgDataDir = $PG_DEFAULT_DATA_DIR
} else {
    $pgSvcObj = Get-WmiObject Win32_Service -Filter "Name LIKE 'postgresql%'" `
                -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($pgSvcObj -and $pgSvcObj.PathName -match "-D\s+`"?([^`"]+)`"?") {
        $pgDataDir = $Matches[1].Trim('"').Trim()
    }
}

if ($pgDataDir -and (Test-Path $pgDataDir)) {

    # postgresql.conf: listen_addresses = '*'
    $pgConf = Join-Path $pgDataDir "postgresql.conf"
    if (Test-Path $pgConf) {
        $confContent = Get-Content $pgConf -Raw
        if ($confContent -match "(?m)^\s*#?\s*listen_addresses\s*=") {
            $confContent = $confContent -replace "(?m)^\s*#?\s*listen_addresses\s*=.*$",
                           "listen_addresses = '*'"
        } else {
            $confContent += "`nlisten_addresses = '*'`n"
        }
        Set-Content -Path $pgConf -Value $confContent -Encoding UTF8
        Write-OK "postgresql.conf: listen_addresses = '*'"
    } else {
        Write-Warn "No se encontro postgresql.conf en: $pgDataDir"
    }

    # pg_hba.conf: autenticacion md5 desde cualquier IP
    $pgHba = Join-Path $pgDataDir "pg_hba.conf"
    if (Test-Path $pgHba) {
        $hbaContent = Get-Content $pgHba -Raw
        $hbaLine    = "host    all             all             0.0.0.0/0               md5"
        if ($hbaContent -notmatch [regex]::Escape($hbaLine)) {
            $hbaContent += "`n# Comercio.NET - acceso remoto red local`n$hbaLine`n"
            Set-Content -Path $pgHba -Value $hbaContent -Encoding UTF8
            Write-OK "pg_hba.conf: md5 desde 0.0.0.0/0 agregado."
        } else {
            Write-OK "pg_hba.conf: regla remota ya existente."
        }
    } else {
        Write-Warn "No se encontro pg_hba.conf en: $pgDataDir"
    }

    # Reiniciar servicio para aplicar cambios
    $pgSvcName = (Get-WmiObject Win32_Service -Filter "Name LIKE 'postgresql%'" `
                  -ErrorAction SilentlyContinue | Select-Object -First 1).Name
    if ($pgSvcName) {
        try {
            Restart-Service -Name $pgSvcName -Force -ErrorAction Stop
            Start-Sleep -Seconds 5
            Write-OK "PostgreSQL reiniciado con nueva configuracion."
        } catch {
            Write-Warn "No se pudo reiniciar PostgreSQL: $_"
            Write-Info "Reinicielo manualmente en services.msc"
        }
    }

} else {
    Write-Warn "No se encontro el directorio de datos de PostgreSQL."
    Write-Warn "Configure manualmente:"
    Write-Warn "  postgresql.conf  ->  listen_addresses = '*'"
    Write-Warn "  pg_hba.conf      ->  host all all 0.0.0.0/0 md5"
}

# Abrir puerto en Firewall
Write-Info "Configurando firewall (TCP $PgPort)..."
try {
    $existingRule = netsh advfirewall firewall show rule name="PostgreSQL $PgPort" 2>$null
    if ($existingRule -notmatch "PostgreSQL $PgPort") {
        netsh advfirewall firewall add rule `
            name="PostgreSQL $PgPort" protocol=TCP dir=in `
            localport=$PgPort action=allow | Out-Null
        Write-OK "Regla de firewall creada: TCP $PgPort entrada permitida."
    } else {
        Write-OK "Regla de firewall para puerto $PgPort ya existe."
    }
} catch {
    Write-Warn "No se pudo configurar el firewall: $_"
    Write-Warn "Abra manualmente el puerto $PgPort TCP entrante."
}

# ===========================================================================
# PASO 4 - OBTENER ULTIMA VERSION DE GITHUB
# ===========================================================================
Write-Step "4/9" "Consultando ultima version en GitHub..."

$headers = @{
    "User-Agent" = "ComercioNET-Installer/2.0"
    "Accept"     = "application/vnd.github.v3+json"
}
if ($GitHubToken) { $headers["Authorization"] = "Bearer $GitHubToken" }

try {
    $release = Invoke-RestMethod `
        -Uri "https://api.github.com/repos/$GitHubRepo/releases/latest" `
        -Headers $headers -ErrorAction Stop
} catch {
    Write-Fail "No se pudo conectar a GitHub: $_"
    Read-Host "ENTER para salir"
    exit 1
}

$version  = $release.tag_name -replace '^v', ''
$zipAsset = $release.assets | Where-Object { $_.name -like "*.zip" } | Select-Object -First 1

if (-not $zipAsset) {
    Write-Fail "No se encontro un .zip en el release $version."
    Read-Host "ENTER para salir"
    exit 1
}

$downloadUrl = if ($GitHubToken) { $zipAsset.url } else { $zipAsset.browser_download_url }
$sizeMB      = [math]::Round($zipAsset.size / 1MB, 1)
Write-OK "Version: $version  ($sizeMB MB)"

# ===========================================================================
# PASO 5 - DESCARGAR Y EXTRAER LA APLICACION
# ===========================================================================
Write-Step "5/9" "Descargando $APP_NAME v$version..."

$tempZip     = Join-Path $env:TEMP "ComercioNET_Install_$version.zip"
$tempExtract = Join-Path $env:TEMP "ComercioNET_Install_$version"
Remove-Item $tempZip     -Force -ErrorAction SilentlyContinue
Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue

try {
    $wc = New-Object System.Net.WebClient
    foreach ($k in $headers.Keys) { $wc.Headers.Add($k, $headers[$k]) }
    if ($GitHubToken -and $downloadUrl -match "api\.github\.com") {
        $wc.Headers["Accept"] = "application/octet-stream"
    }
    $wc.DownloadFile($downloadUrl, $tempZip)
    Write-OK "Descarga completada."
} catch {
    Write-Fail "Error en la descarga: $_"
    Read-Host "ENTER para salir"
    exit 1
}

try {
    Expand-Archive -LiteralPath $tempZip -DestinationPath $tempExtract -Force
    Write-OK "Archivos extraidos."
} catch {
    Write-Fail "Error extrayendo el zip: $_"
    exit 1
}

# ===========================================================================
# PASO 6 - INSTALAR ARCHIVOS DE LA APLICACION
# ===========================================================================
Write-Step "6/9" "Instalando en $InstallDir..."

$archivosProtegidos = @(
    "appsettings.json",
    "loginconfig.json",
    "afip_tokens.json",
    "version.txt"
)

$backupDir = $null
if (Test-Path $InstallDir) {
    $backupDir = Join-Path $env:TEMP "ComercioNET_Backup_$(Get-Date -Format 'yyyyMMddHHmmss')"
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    foreach ($f in $archivosProtegidos) {
        $src = Join-Path $InstallDir $f
        if (Test-Path $src) {
            Copy-Item $src -Destination $backupDir -Force
            Write-Info "Backup: $f"
        }
    }
    foreach ($subdir in @("Certificados FE", "migrations")) {
        $src = Join-Path $InstallDir $subdir
        if (Test-Path $src) {
            Copy-Item $src -Destination (Join-Path $backupDir $subdir) -Recurse -Force
            Write-Info "Backup: $subdir"
        }
    }
    Write-OK "Backup guardado en: $backupDir"
}

New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null

$extractedItems = Get-ChildItem -Path $tempExtract
$sourceDir = if ($extractedItems.Count -eq 1 -and $extractedItems[0].PSIsContainer) {
    $extractedItems[0].FullName
} else {
    $tempExtract
}

Copy-Item -Path "$sourceDir\*" -Destination $InstallDir -Recurse -Force
Write-OK "Archivos copiados."

if ($backupDir -and (Test-Path $backupDir)) {
    foreach ($f in $archivosProtegidos) {
        $src = Join-Path $backupDir $f
        if (Test-Path $src) {
            Copy-Item $src -Destination $InstallDir -Force
            Write-Info "Restaurado: $f"
        }
    }
    foreach ($subdir in @("Certificados FE", "migrations")) {
        $src = Join-Path $backupDir $subdir
        if (Test-Path $src) {
            $dst = Join-Path $InstallDir $subdir
            New-Item -ItemType Directory -Path $dst -Force | Out-Null
            Copy-Item "$src\*" -Destination $dst -Recurse -Force
            Write-Info "Restaurado: $subdir"
        }
    }
    Remove-Item $backupDir -Recurse -Force -ErrorAction SilentlyContinue
}

Set-Content -Path (Join-Path $InstallDir "version.txt") -Value $version -Encoding UTF8

# Crear carpetas complementarias si no existen
foreach ($subdir in @(
    "Certificados FE",
    "Certificados FE\Testing",
    "Certificados FE\Produccion",
    "migrations"
)) {
    $p = Join-Path $InstallDir $subdir
    if (-not (Test-Path $p)) { New-Item -ItemType Directory -Path $p -Force | Out-Null }
}

Write-OK "Version $version registrada."

# ===========================================================================
# PASO 7 - INICIALIZAR BASE DE DATOS POSTGRESQL
# ===========================================================================
Write-Step "7/9" "Inicializando base de datos PostgreSQL..."

$env:PGPASSWORD = $PgPassword
$psqlExe        = Join-Path $pgBin "psql.exe"
$pgRestoreExe   = Join-Path $pgBin "pg_restore.exe"
$createdbExe    = Join-Path $pgBin "createdb.exe"

# Crear la BD si no existe
$dbCheck = (& $psqlExe -U $DB_USER -p $PgPort -d postgres -tAc `
    "SELECT 1 FROM pg_database WHERE datname='$DB_NAME';" 2>&1) -join ""
$dbCheck = $dbCheck.Trim()

if ($dbCheck -ne "1") {
    Write-Info "Creando base de datos '$DB_NAME'..."
    & $createdbExe -U $DB_USER -p $PgPort -E UTF8 $DB_NAME 2>&1 | ForEach-Object { Write-Info $_ }
    Write-OK "Base de datos '$DB_NAME' creada."
} else {
    Write-OK "Base de datos '$DB_NAME' ya existe."
}

# Restaurar desde dump o ejecutar DDL
$dumpPath   = Join-Path $InstallDir $DB_DUMP_FILE
$initScript = Join-Path $InstallDir $DB_INIT_SCRIPT

if (Test-Path $dumpPath) {
    Write-Info "Restaurando desde comercio_inicial.dump..."
    $env:PGPASSWORD = $PgPassword
    & $pgRestoreExe `
        -U $DB_USER -p $PgPort -d $DB_NAME `
        --no-owner --no-acl --if-exists -c `
        $dumpPath 2>&1 | ForEach-Object { Write-Info $_ }
    if ($LASTEXITCODE -eq 0) {
        Write-OK "Base de datos restaurada desde dump."
    } else {
        Write-Warn "pg_restore codigo $LASTEXITCODE (puede ser normal en reinstalacion)."
    }
} elseif (Test-Path $initScript) {
    Write-Info "Dump no encontrado. Ejecutando DDL init_comercio_pg.sql..."
    $env:PGPASSWORD = $PgPassword
    & $psqlExe -U $DB_USER -p $PgPort -d $DB_NAME -f $initScript 2>&1 |
        ForEach-Object { Write-Info $_ }
    if ($LASTEXITCODE -eq 0) {
        Write-OK "Esquema creado correctamente."
    } else {
        Write-Warn "psql codigo $LASTEXITCODE. Revise los mensajes anteriores."
    }
} else {
    Write-Warn "No se encontro dump ni script DDL en $InstallDir."
    Write-Warn "Inicialice la BD manualmente:"
    Write-Warn "  psql -U $DB_USER -p $PgPort -d $DB_NAME -f <ruta>\init_comercio_pg.sql"
}

Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue

# ===========================================================================
# PASO 8 - GENERAR / ACTUALIZAR appsettings.json
# ===========================================================================
Write-Step "8/9" "Configurando appsettings.json..."

# Detectar IP de red local del servidor
$localIp = (Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
    Where-Object {
        $_.InterfaceAlias -notlike '*Loopback*' -and
        $_.IPAddress -notlike '169.254.*' -and
        $_.IPAddress -notlike '127.*'
    } |
    Sort-Object {
        [System.Net.IPAddress]::Parse($_.IPAddress).GetAddressBytes()[0]
    } -Descending |
    Select-Object -First 1).IPAddress

if (-not $localIp) { $localIp = "localhost" }
Write-Info "IP de red del servidor: $localIp"

$connString  = "Host=$localIp;Port=$PgPort;Database=$DB_NAME;Username=$DB_USER;Password=$PgPassword;"
$appSettings = Join-Path $InstallDir "appsettings.json"

if (-not (Test-Path $appSettings)) {

    $lines = @(
        '{',
        '  "ConnectionStrings": {',
        "    `"DefaultConnection`": `"$connString`",",
        "    `"Testing`": `"$connString`",",
        "    `"Produccion`": `"$connString`"",
        '  },',
        '  "Comercio": {',
        '    "Nombre": "MI COMERCIO",',
        '    "Domicilio": "Calle 000 N 000 - Ciudad"',
        '  },',
        '  "Facturacion": {',
        '    "RazonSocial": "Nombre Apellido",',
        '    "CUIT": "00-00000000-0",',
        '    "IngBrutos": "00-00000000-0",',
        '    "DomicilioFiscal": "Calle 000 N 000 - Ciudad",',
        '    "CodigoPostal": "0000",',
        '    "InicioActividades": "2020-01-01",',
        '    "Condicion": "Monotributo",',
        '    "PermitirFacturaA": false,',
        '    "PermitirFacturaB": false,',
        '    "PermitirFacturaC": true',
        '  },',
        '  "Validaciones": {',
        '    "ValidarStockDisponible": false',
        '  },',
        '  "CuentasCorrientes": {',
        '    "NombresCtaCte": []',
        '  },',
        '  "AFIP": {',
        '    "AmbienteActivo": "Testing",',
        '    "Testing": {',
        '      "CUIT": "00-00000000-0",',
        '      "CondicionIVA": "Monotributo",',
        '      "PuntoVenta": 1,',
        '      "CertificadoPath": "C:\\\\Certificados FE\\\\Testing\\\\MiCertificadoTesting.p12",',
        '      "CertificadoPassword": "password_del_certificado",',
        '      "WSAAUrl": "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",',
        '      "WSFEUrl": "https://wswhomo.afip.gov.ar/wsfev1/service.asmx",',
        '      "Servicios": { "Facturacion": "wsfe" }',
        '    },',
        '    "Produccion": {',
        '      "CUIT": "00-00000000-0",',
        '      "CondicionIVA": "Monotributo",',
        '      "PuntoVenta": 1,',
        '      "CertificadoPath": "C:\\\\Certificados FE\\\\Produccion\\\\MiCertificado.p12",',
        '      "CertificadoPassword": "password_del_certificado",',
        '      "WSAAUrl": "https://wsaa.afip.gov.ar/ws/services/LoginCms",',
        '      "WSFEUrl": "https://servicios1.afip.gov.ar/wsfev1/service.asmx",',
        '      "Servicios": { "Facturacion": "wsfe" }',
        '    }',
        '  },',
        '  "RestriccionesImpresion": {',
        '    "RestringirRemitoPorPago": false,',
        '    "UsarVistaPrevia": true,',
        '    "LimitarFacturacion": false,',
        '    "MontoLimiteFacturacion": 0.00',
        '  },',
        '  "Descuentos": {',
        '    "OpcionesDisponibles": [ 5, 10, 15, 20 ],',
        '    "PorcentajeMaximo": 20,',
        '    "RestringirPorMetodoPago": false,',
        '    "MetodosPagoPermitidos": [ "Efectivo" ]',
        '  },',
        '  "BaseDatos": {',
        '    "AmbienteActivo": "Testing"',
        '  }',
        '}'
    )

    $lines | Set-Content -Path $appSettings -Encoding UTF8
    Write-OK "appsettings.json creado con valores de ejemplo."
    Write-Warn "IMPORTANTE: edite $appSettings antes de usar la aplicacion."

} else {

    try {
        $json    = Get-Content $appSettings -Raw -Encoding UTF8
        $escaped = $connString -replace '\\', '\\'
        $json    = $json -replace '(?<="DefaultConnection"\s*:\s*")[^"]*(?=")', $escaped
        Set-Content -Path $appSettings -Value $json -Encoding UTF8
        Write-OK "appsettings.json actualizado con IP $localIp."
    } catch {
        Write-Warn "No se pudo actualizar appsettings.json: $_"
        Write-Info "Connection string: $connString"
    }

}

Write-Host ""
Write-Host "  +-- CADENA DE CONEXION PARA PCs CLIENTES --------------------+" -ForegroundColor Cyan
Write-Host "  $connString" -ForegroundColor White
Write-Host "  Copie esta linea en el appsettings.json de cada PC cliente." -ForegroundColor Gray
Write-Host "  +-------------------------------------------------------------+" -ForegroundColor Cyan
Write-Host ""

# ===========================================================================
# PASO 9 - ACCESO DIRECTO EN EL ESCRITORIO
# ===========================================================================
Write-Step "9/9" "Creando acceso directo en el escritorio..."

$exePath      = Join-Path $InstallDir $APP_EXE
$shortcutPath = Join-Path ([Environment]::GetFolderPath("CommonDesktopDirectory")) "$APP_NAME.lnk"

if (Test-Path $exePath) {
    try {
        $wsh = New-Object -ComObject WScript.Shell
        $sc  = $wsh.CreateShortcut($shortcutPath)
        $sc.TargetPath       = $exePath
        $sc.WorkingDirectory = $InstallDir
        $sc.Description      = "$APP_NAME - Sistema de Gestion Comercial"
        $sc.IconLocation     = "$exePath, 0"
        $sc.Save()
        Write-OK "Acceso directo creado en el escritorio."
    } catch {
        Write-Warn "No se pudo crear el acceso directo: $_"
        Write-Info "Creelo manualmente desde: $exePath"
    }
} else {
    Write-Warn "No se encontro $APP_EXE en $InstallDir."
}

# ===========================================================================
# LIMPIEZA
# ===========================================================================
Remove-Item $tempZip     -Force -ErrorAction SilentlyContinue
Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue

# ===========================================================================
# RESUMEN FINAL
# ===========================================================================
Write-Header "INSTALACION COMPLETADA -- Comercio .NET v$version"

Write-Host "  Carpeta     : $InstallDir" -ForegroundColor Cyan
Write-Host "  Base datos  : PostgreSQL $PG_VERSION >> $DB_NAME en ${localIp}:$PgPort" -ForegroundColor Cyan
Write-Host ""
Write-Host "  PASOS OBLIGATORIOS ANTES DE USAR:" -ForegroundColor Yellow
Write-Host "  1. Editar appsettings.json:" -ForegroundColor White
Write-Host "     $appSettings" -ForegroundColor Gray
Write-Host "     - Comercio.Nombre y Comercio.Domicilio" -ForegroundColor Gray
Write-Host "     - Facturacion: CUIT, RazonSocial, Condicion" -ForegroundColor Gray
Write-Host "     - AFIP: CUIT, PuntoVenta, rutas de certificados" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Copiar certificados AFIP (.p12/.pfx) en:" -ForegroundColor White
Write-Host "     $(Join-Path $InstallDir 'Certificados FE')" -ForegroundColor Gray
Write-Host ""
Write-Host "  CONEXION DESDE OTRAS PCs:" -ForegroundColor Yellow
Write-Host "  $connString" -ForegroundColor Cyan
Write-Host ""
Write-Host "  USUARIO BD: $DB_USER  |  Password: $PgPassword" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Iniciar la aplicacion desde el acceso directo del escritorio." -ForegroundColor White
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host ""

$resp = Read-Host "Desea abrir la carpeta de instalacion ahora? (S/N)"
if ($resp -match "^[sS]") {
    Start-Process explorer.exe -ArgumentList $InstallDir
}
