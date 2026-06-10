#Requires -Version 5.1
<#
.SYNOPSIS
    Instalador de Comercio.NET Web

.DESCRIPTION
    Instala Comercio.NET Web en un equipo Windows nuevo de forma desatendida.
    - Instala .NET 8 ASP.NET Core Runtime (desde archivo local si existe, sino descarga)
    - Instala PostgreSQL 16 (binarios ZIP, 100% desatendido)
    - Restaura la base de datos desde backup-comercio.dump
    - Registra la aplicacion como servicio de Windows (via NSSM incluido)
    - Configura firewall para puerto 8080
    - Crea estructura de carpetas para certificados AFIP

.NOTES
    Archivos requeridos en la misma carpeta que este script:
      - publish\                     (archivos publicados de la app)
      - backup-comercio.dump         (backup de la base de datos)
      - nssm.exe                     (incluido en la carpeta)
      - Certificados\Testing\        (certificado .p12 de testing AFIP)
      - aspnetcore-runtime-8-win-x64.exe  (opcional, sino descarga)

    Ejecutar como Administrador:
      PowerShell -ExecutionPolicy Bypass -File instalar.ps1
#>

[CmdletBinding()]
param(
    [string]$RutaInstalacion = "C:\ComercioWeb"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# CONSTANTES FIJAS - No requieren configuracion
# ---------------------------------------------------------------------------
$PG_VERSION      = "16"
$PG_SERVICE_NAME = "postgresql-16"
$PG_ROOT_DIR     = "C:\PostgreSQL\16"
$PG_DATA_DIR     = "C:\PostgreSQL\16\data"
$PG_BIN_DIR      = "C:\PostgreSQL\16\bin"
$PG_ZIP_URL      = "https://get.enterprisedb.com/postgresql/postgresql-16.6-1-windows-x64-binaries.zip"

$DB_HOST         = "localhost"
$DB_PORT         = "5432"
$DB_NAME         = "comercio"
$DB_USER         = "postgres"
$DB_PASSWORD     = "michael"

$APP_PORT        = "8080"
$SERVICE_NAME    = "ComercioNETWeb"

$DOTNET_MIN_VERSION = [version]"8.0.27"
$DOTNET_CORE_URL    = "https://aka.ms/dotnet/8.0/dotnet-runtime-win-x64.exe"
$DOTNET_ASPNET_URL  = "https://aka.ms/dotnet/8.0/aspnetcore-runtime-win-x64.exe"

# ---------------------------------------------------------------------------
# HELPERS
# ---------------------------------------------------------------------------
function Write-Header {
    param([string]$Texto)
    Write-Host ""
    Write-Host ("=" * 62) -ForegroundColor Cyan
    Write-Host "  $Texto" -ForegroundColor Cyan
    Write-Host ("=" * 62) -ForegroundColor Cyan
    Write-Host ""
}
function Write-Paso  { param([string]$N, [string]$T) Write-Host "[$N] $T" -ForegroundColor Yellow }
function Write-OK    { param([string]$M) Write-Host "     OK  $M" -ForegroundColor Green   }
function Write-Info  { param([string]$M) Write-Host "     >>  $M" -ForegroundColor Gray    }
function Write-Aviso { param([string]$M) Write-Host "     !!  $M" -ForegroundColor Magenta }
function Write-Error2{ param([string]$M) Write-Host "     XX  $M" -ForegroundColor Red     }

function Detener-Script {
    param([string]$Motivo)
    Write-Host ""
    Write-Error2 $Motivo
    Write-Host ""
    Read-Host "Presione ENTER para salir"
    exit 1
}

# ---------------------------------------------------------------------------
# VERIFICAR ADMINISTRADOR
# ---------------------------------------------------------------------------
$esAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $esAdmin) {
    Write-Host "Reiniciando con permisos de Administrador..." -ForegroundColor Yellow
    Start-Process powershell -Verb RunAs -ArgumentList "-ExecutionPolicy Bypass -File `"$PSCommandPath`""
    exit
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Clear-Host
Write-Header "INSTALADOR COMERCIO.NET WEB"
Write-Host "  Destino   : $RutaInstalacion" -ForegroundColor White
Write-Host "  Base datos: $DB_NAME @ ${DB_HOST}:${DB_PORT}  usuario=$DB_USER" -ForegroundColor White
Write-Host "  Puerto web: $APP_PORT" -ForegroundColor White
Write-Host "  Fecha/Hora: $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')" -ForegroundColor White
Write-Host ""

# ===========================================================================
# DATOS DEL COMERCIO (unico onboarding necesario)
# ===========================================================================
Write-Host "-----------------------------------------------------------" -ForegroundColor DarkCyan
Write-Host "  Datos del Comercio" -ForegroundColor Cyan
Write-Host "-----------------------------------------------------------" -ForegroundColor DarkCyan
Write-Host ""

$nombreComercio = Read-Host "  Nombre del comercio"
while ([string]::IsNullOrWhiteSpace($nombreComercio)) {
    Write-Aviso "El nombre no puede estar vacio."
    $nombreComercio = Read-Host "  Nombre del comercio"
}

$domicilio = Read-Host "  Domicilio"
if ([string]::IsNullOrWhiteSpace($domicilio)) { $domicilio = "" }

$cuit = Read-Host "  CUIT (Enter para 20-28069473-9)"
if ([string]::IsNullOrWhiteSpace($cuit)) { $cuit = "20-28069473-9" }

$ingBrutos = Read-Host "  Ingresos Brutos (Enter para usar CUIT)"
if ([string]::IsNullOrWhiteSpace($ingBrutos)) { $ingBrutos = $cuit }

Write-Host ""

# ===========================================================================
# PASO 1 - .NET 8 RUNTIME (Core + ASP.NET)
# ===========================================================================
Write-Paso "1/6" "Verificando .NET 8 Runtime requerido ($DOTNET_MIN_VERSION)..."

function Test-DotNetMinVersion {
    param([version]$MinVersion)

    $coreOk = $false
    $aspNetOk = $false

    try {
        $runtimes = & dotnet --list-runtimes 2>$null

        foreach ($line in $runtimes) {
            if ($line -match '^Microsoft\.NETCore\.App\s+([0-9]+\.[0-9]+\.[0-9]+)') {
                if ([version]$Matches[1] -ge $MinVersion) { $coreOk = $true }
            }
            if ($line -match '^Microsoft\.AspNetCore\.App\s+([0-9]+\.[0-9]+\.[0-9]+)') {
                if ([version]$Matches[1] -ge $MinVersion) { $aspNetOk = $true }
            }
        }
    } catch {
        return $false
    }

    return ($coreOk -and $aspNetOk)
}

function Install-DotNetPackage {
    param(
        [string]$Nombre,
        [string]$LocalPattern,
        [string]$Url,
        [string]$TmpFileName
    )

    $localInstaller = Get-ChildItem $scriptDir -Filter $LocalPattern -ErrorAction SilentlyContinue | Select-Object -First 1

    if ($localInstaller) {
        Write-Info "Instalando $Nombre desde archivo local: $($localInstaller.Name)"
        $p = Start-Process -FilePath $localInstaller.FullName -ArgumentList "/quiet /norestart" -Wait -PassThru
        if ($p.ExitCode -ne 0 -and $p.ExitCode -ne 3010) {
            Detener-Script "La instalacion de $Nombre fallo (codigo $($p.ExitCode))."
        }
        return
    }

    Write-Info "Instalador local de $Nombre no encontrado. Descargando..."
    $tmpDotnet = Join-Path $env:TEMP $TmpFileName
    try {
        (New-Object System.Net.WebClient).DownloadFile($Url, $tmpDotnet)
        $p = Start-Process -FilePath $tmpDotnet -ArgumentList "/quiet /norestart" -Wait -PassThru
        Remove-Item $tmpDotnet -Force -ErrorAction SilentlyContinue
        if ($p.ExitCode -ne 0 -and $p.ExitCode -ne 3010) {
            Detener-Script "La instalacion de $Nombre fallo (codigo $($p.ExitCode))."
        }
    } catch {
        Detener-Script "No se pudo descargar/instalar ${Nombre}: $_"
    }
}

$dotnetOk = Test-DotNetMinVersion -MinVersion $DOTNET_MIN_VERSION

if ($dotnetOk) {
    Write-OK ".NET Runtime compatible ya esta instalado."
} else {
    Install-DotNetPackage -Nombre ".NET Runtime" -LocalPattern "dotnet-runtime-8*win-x64.exe" -Url $DOTNET_CORE_URL -TmpFileName "dotnet-runtime-8-win-x64.exe"
    Install-DotNetPackage -Nombre "ASP.NET Core Runtime" -LocalPattern "aspnetcore-runtime-8*win-x64.exe" -Url $DOTNET_ASPNET_URL -TmpFileName "aspnetcore-runtime-8-win-x64.exe"

    # Revalidar version efectiva
    if (Test-DotNetMinVersion -MinVersion $DOTNET_MIN_VERSION) {
        Write-OK ".NET Runtime actualizado correctamente."
    } else {
        Detener-Script "No se detecto la version minima requerida de .NET ($DOTNET_MIN_VERSION) luego de instalar."
    }
}

# ===========================================================================
# PASO 2 - POSTGRESQL 16
# ===========================================================================
Write-Paso "2/6" "Verificando PostgreSQL $PG_VERSION..."

function Find-PgBin {
    # Buscar primero en ruta propia (sin restricciones de permisos)
    if (Test-Path (Join-Path $PG_BIN_DIR "psql.exe")) { return $PG_BIN_DIR }
    # Fallback: rutas estandar de instaladores oficiales
    foreach ($v in @("16","17","15","14","13","18")) {
        $p = "C:\Program Files\PostgreSQL\$v\bin"
        if (Test-Path (Join-Path $p "psql.exe")) { return $p }
    }
    $found = Get-Command psql -ErrorAction SilentlyContinue
    if ($found) { return (Split-Path $found.Source) }
    return $null
}

$pgBin = Find-PgBin

if ($pgBin) {
    Write-OK "PostgreSQL encontrado en: $pgBin"
} else {
    Write-Info "PostgreSQL no detectado. Descargando binarios (~230 MB)..."

    $tmpPg = Join-Path $env:TEMP "postgresql-16-binaries.zip"
    try {
        Write-Info "Descargando desde EnterpriseDB (puede demorar varios minutos)..."
        (New-Object System.Net.WebClient).DownloadFile($PG_ZIP_URL, $tmpPg)
        Write-OK "Descarga completada."
    } catch {
        Detener-Script "Error descargando PostgreSQL: $_"
    }

    # Usar C:\PostgreSQL para evitar restricciones de permisos de C:\Program Files
    $pgExtractBase = "C:\PostgreSQL"
    if (-not (Test-Path $pgExtractBase)) { New-Item -ItemType Directory -Path $pgExtractBase -Force | Out-Null }

    Write-Info "Extrayendo archivos en $pgExtractBase (puede demorar 1-2 minutos)..."
    try {
        Expand-Archive -Path $tmpPg -DestinationPath $pgExtractBase -Force
        # El ZIP extrae como "pgsql", renombrar a "16"
        $pgsqlDir = Join-Path $pgExtractBase "pgsql"
        if (Test-Path $pgsqlDir) {
            Rename-Item -Path $pgsqlDir -NewName $PG_VERSION -Force
        }
        Write-OK "Extraccion completada."
    } catch {
        Detener-Script "Error al extraer PostgreSQL: $_"
    } finally {
        Remove-Item $tmpPg -Force -ErrorAction SilentlyContinue
    }

    $pgBin = Find-PgBin
    if (-not $pgBin) { Detener-Script "No se pudieron localizar los binarios de PostgreSQL." }

    # Dar permisos completos a la cuenta de servicio de Windows (Network Service)
    Write-Info "Configurando permisos..."
    & icacls $pgExtractBase /grant "*S-1-5-20:(OI)(CI)F" /T /C /Q | Out-Null
    & icacls $pgExtractBase /grant "*S-1-5-19:(OI)(CI)F" /T /C /Q | Out-Null
    & icacls $pgExtractBase /grant "Todos:(OI)(CI)F" /T /C /Q | Out-Null

    # Crear directorio de datos con permisos correctos desde el inicio
    if (-not (Test-Path $PG_DATA_DIR)) {
        New-Item -ItemType Directory -Path $PG_DATA_DIR -Force | Out-Null
    }
    & icacls $PG_DATA_DIR /grant "*S-1-5-20:(OI)(CI)F" /T /C /Q | Out-Null
    & icacls $PG_DATA_DIR /grant "*S-1-5-19:(OI)(CI)F" /T /C /Q | Out-Null
    & icacls $PG_DATA_DIR /grant "Todos:(OI)(CI)F" /T /C /Q | Out-Null

    # Inicializar cluster (initdb)
    Write-Info "Inicializando base de datos (initdb)..."
    $initDbExe = Join-Path $pgBin "initdb.exe"
    $pwFile    = Join-Path $env:TEMP "pg_pw_temp.txt"
    $DB_PASSWORD | Out-File $pwFile -Encoding ascii -NoNewline

    & $initDbExe -D "$PG_DATA_DIR" -U $DB_USER -A scram-sha-256 --pwfile="$pwFile" --encoding=UTF8
    Remove-Item $pwFile -Force -ErrorAction SilentlyContinue

    & icacls $PG_DATA_DIR /grant "*S-1-5-20:(OI)(CI)F" /T /C /Q | Out-Null

    # Registrar como servicio de Windows
    Write-Info "Registrando servicio de Windows para PostgreSQL..."
    $pgCtlExe = Join-Path $pgBin "pg_ctl.exe"
    & $pgCtlExe register -N $PG_SERVICE_NAME -D "$PG_DATA_DIR" -w | Out-Null

    $svc = Get-Service -Name $PG_SERVICE_NAME -ErrorAction SilentlyContinue
    if (-not $svc) { Detener-Script "No se pudo registrar el servicio de PostgreSQL." }

    Set-Service -Name $PG_SERVICE_NAME -StartupType Automatic
    Start-Service -Name $PG_SERVICE_NAME -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 6

    $svc = Get-Service -Name $PG_SERVICE_NAME
    if ($svc.Status -ne 'Running') {
        Detener-Script "El servicio PostgreSQL no pudo iniciar. Revise el Visor de Eventos."
    }
    Write-OK "PostgreSQL $PG_VERSION instalado e iniciado."

    # Agregar regla de firewall para PostgreSQL
    netsh advfirewall firewall add rule name="PostgreSQL 5432" protocol=TCP dir=in localport=5432 action=allow | Out-Null
    Write-OK "Firewall: puerto 5432 habilitado."
}

# Verificar que el servicio este corriendo
$pgSvc = Get-Service -Name $PG_SERVICE_NAME -ErrorAction SilentlyContinue
if ($pgSvc -and $pgSvc.Status -ne 'Running') {
    Write-Info "Iniciando servicio PostgreSQL..."

    # Reforzar permisos por si quedaron incompletos
    if (Test-Path "C:\PostgreSQL") {
        & icacls "C:\PostgreSQL" /grant "*S-1-5-20:(OI)(CI)F" /T /C /Q | Out-Null
        & icacls "C:\PostgreSQL" /grant "*S-1-5-19:(OI)(CI)F" /T /C /Q | Out-Null
        & icacls "C:\PostgreSQL" /grant "Todos:(OI)(CI)F" /T /C /Q | Out-Null
    }

    try {
        Start-Service -Name $PG_SERVICE_NAME -ErrorAction Stop
        Start-Sleep -Seconds 4
    } catch {
        Write-Aviso "No se pudo iniciar el servicio PostgreSQL al primer intento. Reintentando..."

        # Si quedo un lock file de una ejecucion anterior, eliminarlo
        $pidFile = Join-Path $PG_DATA_DIR "postmaster.pid"
        $pgProc = Get-Process postgres -ErrorAction SilentlyContinue
        if ((Test-Path $pidFile) -and (-not $pgProc)) {
            Remove-Item $pidFile -Force -ErrorAction SilentlyContinue
            Write-Info "Se elimino lock file stale: postmaster.pid"
        }

        try {
            Start-Service -Name $PG_SERVICE_NAME -ErrorAction Stop
            Start-Sleep -Seconds 4
        } catch {
            # Reparar registro del servicio y volver a intentar
            $pgCtlExe = Join-Path $pgBin "pg_ctl.exe"
            Write-Aviso "Reparando registro del servicio PostgreSQL..."
            & $pgCtlExe unregister -N $PG_SERVICE_NAME | Out-Null
            & $pgCtlExe register -N $PG_SERVICE_NAME -D "$PG_DATA_DIR" -w | Out-Null
            Start-Sleep -Seconds 2
            Start-Service -Name $PG_SERVICE_NAME -ErrorAction Stop
            Start-Sleep -Seconds 4
        }
    }

    $pgSvc = Get-Service -Name $PG_SERVICE_NAME -ErrorAction SilentlyContinue
    if (-not $pgSvc -or $pgSvc.Status -ne 'Running') {
        Detener-Script "No se pudo iniciar PostgreSQL. Revise el Visor de Eventos y el log en C:\PostgreSQL\16\data\log."
    }
}

# ===========================================================================
# PASO 3 - BASE DE DATOS
# ===========================================================================
Write-Paso "3/6" "Configurando base de datos '$DB_NAME'..."

$psql      = Join-Path $pgBin "psql.exe"
$createdb  = Join-Path $pgBin "createdb.exe"
$dropdb    = Join-Path $pgBin "dropdb.exe"
$pgrestore = Join-Path $pgBin "pg_restore.exe"
$env:PGPASSWORD = $DB_PASSWORD

# Crear base limpia para evitar conflictos al reinstalar
$existe = & "$psql" -h $DB_HOST -p $DB_PORT -U $DB_USER -tAc "SELECT 1 FROM pg_database WHERE datname='$DB_NAME'" 2>$null
if ($existe -eq "1") {
    Write-Info "La base '$DB_NAME' ya existe. Se recreara para una restauracion limpia..."
    & "$dropdb" -h $DB_HOST -p $DB_PORT -U $DB_USER --if-exists $DB_NAME
}

Write-Info "Creando base de datos '$DB_NAME'..."
& "$createdb" -h $DB_HOST -p $DB_PORT -U $DB_USER $DB_NAME
Write-OK "Base de datos '$DB_NAME' creada."

# Restaurar backup
$backupDump = Join-Path $scriptDir "backup-comercio.dump"
$backupSql  = Join-Path $scriptDir "backup-comercio.sql"

if (Test-Path $backupSql) {
    Write-Info "Restaurando backup SQL (compatible con cualquier version)..."

    # Algunos backups generados con pg_dump nuevo incluyen metacomandos \restrict/\unrestrict
    # que psql de versiones anteriores no entiende. Se filtran antes de restaurar.
    $backupSqlLimpio = Join-Path $env:TEMP "backup-comercio-limpio.sql"
    $lineasLimpias = Get-Content -Path $backupSql |
        ForEach-Object { $_ -replace "^`uFEFF", "" } |
        Where-Object {
            $_ -notmatch '^\\(un)?restrict\b' -and
            $_ -notmatch '^SET\s+transaction_timeout\s*=\s*' -and
            $_ -notmatch '^SET\s+idle_in_transaction_session_timeout\s*=\s*' -and
            $_ -notmatch '^SET\s+default_table_access_method\s*=\s*'
        }
    $utf8SinBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllLines($backupSqlLimpio, $lineasLimpias, $utf8SinBom)

    & "$psql" -v ON_ERROR_STOP=1 -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f $backupSqlLimpio
    if ($LASTEXITCODE -ne 0) {
        Write-Aviso "psql reporto errores al restaurar (codigo $LASTEXITCODE)."
        Detener-Script "La restauracion de base de datos fallo. Verifique el archivo backup-comercio.sql."
    } else {
        Write-OK "Backup restaurado correctamente."
    }

    Remove-Item $backupSqlLimpio -Force -ErrorAction SilentlyContinue
} elseif (Test-Path $backupDump) {
    Write-Info "Restaurando backup .dump..."
    & "$pgrestore" -h $DB_HOST -p $DB_PORT -U $DB_USER --no-owner --no-privileges -d $DB_NAME $backupDump
    if ($LASTEXITCODE -ne 0) {
        Write-Aviso "pg_restore reporto errores (codigo $LASTEXITCODE)."
        Write-Aviso "Puede haber incompatibilidad de versiones. Use generar-backup.ps1 para regenerar."
    } else {
        Write-OK "Backup restaurado correctamente."
    }
} else {
    Write-Aviso "No se encontro backup-comercio.sql ni backup-comercio.dump."
    Write-Aviso "La base de datos quedara vacia."
}

$env:PGPASSWORD = ""

# ===========================================================================
# PASO 4 - CARPETAS Y ARCHIVOS DE LA APLICACION
# ===========================================================================
Write-Paso "4/6" "Instalando archivos de la aplicacion..."

# Crear estructura de carpetas
$carpetas = @(
    $RutaInstalacion,
    "$RutaInstalacion\logs",
    "$RutaInstalacion\Certificados",
    "$RutaInstalacion\Certificados\Testing",
    "$RutaInstalacion\Certificados\Produccion"
)
foreach ($carpeta in $carpetas) {
    New-Item -ItemType Directory -Force -Path $carpeta | Out-Null
}
Write-OK "Estructura de carpetas creada en $RutaInstalacion"

# Copiar certificado de testing si existe en la carpeta de instalacion
$certOrigen = Get-ChildItem $scriptDir -Filter "*.p12" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($certOrigen) {
    $certDestino = "$RutaInstalacion\Certificados\Testing\$($certOrigen.Name)"
    if ((Resolve-Path $certOrigen.FullName).Path -ne (& { if (Test-Path $certDestino) { (Resolve-Path $certDestino).Path } else { "" } })) {
        Copy-Item $certOrigen.FullName $certDestino -Force
        Write-OK "Certificado AFIP Testing copiado: $($certOrigen.Name)"
    }
    $certPath = $certDestino
} else {
    Write-Aviso "No se encontro un certificado .p12 en la carpeta del instalador."
    Write-Aviso "Copie el certificado manualmente a: $RutaInstalacion\Certificados\Testing\"
    $certPath = "$RutaInstalacion\Certificados\Testing\certificado.p12"
}

# Detener el servicio si ya existe (para liberar archivos bloqueados)
$svcPrevia = Get-Service -Name $SERVICE_NAME -ErrorAction SilentlyContinue
if ($svcPrevia -and $svcPrevia.Status -eq 'Running') {
    Write-Info "Deteniendo servicio anterior para liberar archivos..."
    Stop-Service $SERVICE_NAME -Force
    Start-Sleep -Seconds 3
    Write-OK "Servicio detenido."
}

# Copiar archivos publicados de la aplicacion
$publishDir = Join-Path $scriptDir "publish"
if (Test-Path $publishDir) {
    Write-Info "Copiando archivos de la aplicacion..."
    Copy-Item "$publishDir\*" $RutaInstalacion -Recurse -Force
    Write-OK "Archivos copiados desde 'publish'."
} else {
    Detener-Script "No se encontro la carpeta 'publish' junto al script. Es necesaria para continuar."
}

$certPassword    = "Micertificado"
$codigoPostal    = "7303"
$inicioActividades = "2010-10-01"
$condicionIVA    = "Monotributo"

# Generar appsettings.json
$certPathJson = $certPath -replace '\\', '\\\\'
$connectionString = "Host=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"

$appsettings = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "$connectionString"
  },
  "Comercio": {
    "Nombre": "$nombreComercio",
    "Domicilio": "$domicilio"
  },
  "Facturacion": {
    "RazonSocial": "$nombreComercio",
    "CUIT": "$cuit",
    "IngBrutos": "$ingBrutos",
    "DomicilioFiscal": "$domicilio",
    "CodigoPostal": "$codigoPostal",
    "InicioActividades": "$inicioActividades",
    "Condicion": "$condicionIVA"
  },
  "Caja": {
    "ObligarFacturaElectronica": true,
    "FormasPagoConFactura": ["DNI", "Mercado Pago"],
    "CtaCte": {
      "Habilitado": false,
      "Clientes": []
    }
  },
  "Descuentos": {
    "OpcionesDisponibles": [5, 10, 15, 20],
    "RestringirPorMetodoPago": true,
    "MetodosPagoPermitidos": ["Efectivo"]
  },
  "AFIP": {
    "AmbienteActivo": "Testing",
    "Testing": {
      "CUIT": "$cuit",
      "CondicionIVA": "$condicionIVA",
      "PuntoVenta": 7,
      "CertificadoPath": "$certPathJson",
      "CertificadoPassword": "$certPassword",
      "WSAAUrl": "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",
      "WSFEUrl": "https://wswhomo.afip.gov.ar/wsfev1/service.asmx",
      "Servicios": {
        "Facturacion": "wsfe"
      }
    },
    "Produccion": {
      "CUIT": "$cuit",
      "CondicionIVA": "Responsable Inscripto",
      "PuntoVenta": 7,
      "CertificadoPath": "$($RutaInstalacion -replace '\\','\\\\')\\\\Certificados\\\\Produccion\\\\certificado.p12",
      "CertificadoPassword": "",
      "WSAAUrl": "https://wsaa.afip.gov.ar/ws/services/LoginCms",
      "WSFEUrl": "https://servicios1.afip.gov.ar/wsfev1/service.asmx",
      "Servicios": {
        "Facturacion": "wsfe"
      }
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:$APP_PORT"
      }
    }
  }
}
"@

$appsettings | Out-File "$RutaInstalacion\appsettings.json" -Encoding UTF8
Write-OK "appsettings.json generado."

# ===========================================================================
# PASO 5 - SERVICIO DE WINDOWS (NSSM)
# ===========================================================================
Write-Paso "5/6" "Registrando servicio de Windows..."

# Verificar dotnet.exe
$dotnetExe = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (-not $dotnetExe) {
    Detener-Script "No se encontro dotnet.exe en el PATH. Verifique la instalacion de .NET 8."
}

$dllPath = "$RutaInstalacion\Comercio.NET.Mobile.Server.dll"
if (-not (Test-Path $dllPath)) {
    Detener-Script "No se encontro $dllPath. Verifique que la carpeta 'publish' fue copiada correctamente."
}

# Localizar nssm.exe (incluido en la carpeta del instalador)
$nssmLocal = Join-Path $scriptDir "nssm.exe"
$nssmExe   = "$RutaInstalacion\nssm.exe"

if (Test-Path $nssmLocal) {
    try {
        Copy-Item $nssmLocal $nssmExe -Force -ErrorAction Stop
        Write-OK "nssm.exe copiado a $RutaInstalacion"
    } catch {
        if (Test-Path $nssmExe) {
            Write-Aviso "No se pudo sobrescribir nssm.exe porque esta en uso. Se utilizara el existente."
        } else {
            Detener-Script "No se pudo copiar nssm.exe y no existe uno previo en destino. $_"
        }
    }
} elseif (-not (Test-Path $nssmExe)) {
    # Fallback: descargar si no esta en ninguna parte
    Write-Info "nssm.exe no encontrado localmente. Descargando..."
    try {
        $zipNssm = Join-Path $env:TEMP "nssm.zip"
        (New-Object System.Net.WebClient).DownloadFile("https://nssm.cc/release/nssm-2.24.zip", $zipNssm)
        Expand-Archive $zipNssm -DestinationPath "$env:TEMP\nssm_ext" -Force
        Copy-Item "$env:TEMP\nssm_ext\nssm-2.24\win64\nssm.exe" $nssmExe
        Remove-Item $zipNssm -Force -ErrorAction SilentlyContinue
        Write-OK "nssm.exe descargado."
    } catch {
        Detener-Script "No se pudo obtener nssm.exe: $_"
    }
}

# Eliminar servicio anterior si existe
$svcExistente = Get-Service -Name $SERVICE_NAME -ErrorAction SilentlyContinue
if ($svcExistente) {
    Write-Info "Deteniendo y eliminando servicio anterior..."
    Stop-Service $SERVICE_NAME -Force -ErrorAction SilentlyContinue
    & $nssmExe remove $SERVICE_NAME confirm | Out-Null
    Start-Sleep -Seconds 2
}

# Registrar nuevo servicio
& $nssmExe install $SERVICE_NAME "$dotnetExe" "$dllPath"          | Out-Null
& $nssmExe set $SERVICE_NAME AppDirectory $RutaInstalacion         | Out-Null
& $nssmExe set $SERVICE_NAME DisplayName "Comercio.NET Web"        | Out-Null
& $nssmExe set $SERVICE_NAME Description "Servidor web Comercio.NET" | Out-Null
& $nssmExe set $SERVICE_NAME Start SERVICE_AUTO_START              | Out-Null
& $nssmExe set $SERVICE_NAME AppStdout "$RutaInstalacion\logs\stdout.log" | Out-Null
& $nssmExe set $SERVICE_NAME AppStderr "$RutaInstalacion\logs\stderr.log" | Out-Null
& $nssmExe set $SERVICE_NAME AppRotateFiles 1                      | Out-Null
& $nssmExe set $SERVICE_NAME AppRotateBytes 5000000                | Out-Null

Write-OK "Servicio '$SERVICE_NAME' registrado."

# Iniciar servicio
try {
    Start-Service $SERVICE_NAME -ErrorAction Stop
    Start-Sleep -Seconds 3
    $estado = (Get-Service $SERVICE_NAME).Status
    if ($estado -eq 'Running') {
        Write-OK "Servicio iniciado correctamente. Estado: $estado"
    } else {
        Write-Aviso "El servicio fue creado pero su estado es: $estado"
        Write-Aviso "Revise los logs en: $RutaInstalacion\logs\"
    }
} catch {
    Write-Aviso "El servicio no pudo iniciar automaticamente."
    Write-Aviso "Puede iniciarlo manualmente: Start-Service $SERVICE_NAME"
    Write-Aviso "O revisar errores con: Get-Content '$RutaInstalacion\logs\stderr.log'"
}

# ===========================================================================
# PASO 6 - FIREWALL
# ===========================================================================
Write-Paso "6/6" "Configurando Firewall para puerto $APP_PORT..."

$reglaExiste = Get-NetFirewallRule -DisplayName "Comercio.NET Web $APP_PORT" -ErrorAction SilentlyContinue
if (-not $reglaExiste) {
    New-NetFirewallRule -DisplayName "Comercio.NET Web $APP_PORT" `
        -Direction Inbound -Protocol TCP -LocalPort $APP_PORT `
        -Action Allow -Profile Private,Domain | Out-Null
    Write-OK "Regla de firewall creada para puerto $APP_PORT."
} else {
    Write-OK "Regla de firewall ya existia."
}

# ===========================================================================
# RESUMEN FINAL
# ===========================================================================
$ipLocal = (Get-NetIPAddress -AddressFamily IPv4 |
    Where-Object { $_.InterfaceAlias -notmatch "Loopback" -and $_.IPAddress -notmatch "^169" } |
    Select-Object -First 1).IPAddress

Write-Host ""
Write-Host ("=" * 62) -ForegroundColor Green
Write-Host "  INSTALACION COMPLETADA" -ForegroundColor Green
Write-Host ("=" * 62) -ForegroundColor Green
Write-Host ""
Write-Host "  Acceso local  : http://localhost:$APP_PORT" -ForegroundColor White
if ($ipLocal) {
    Write-Host "  Acceso en red : http://${ipLocal}:$APP_PORT" -ForegroundColor White
}
Write-Host ""
Write-Host "  Usuario       : admin" -ForegroundColor White
Write-Host "  Contrasena    : admin1" -ForegroundColor White
Write-Host ""
Write-Host "  Servicio      : $SERVICE_NAME" -ForegroundColor White
Write-Host "  Instalacion   : $RutaInstalacion" -ForegroundColor White
Write-Host "  Logs          : $RutaInstalacion\logs\" -ForegroundColor White
Write-Host "  Certificados  : $RutaInstalacion\Certificados\" -ForegroundColor White
Write-Host ""
Write-Host "  IMPORTANTE: Cambie la contrasena de admin tras el primer ingreso." -ForegroundColor Yellow
Write-Host ""
Read-Host "Presione ENTER para cerrar"
