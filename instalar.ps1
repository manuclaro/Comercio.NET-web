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
    [int]   $PgPort      = 5432,
    [bool]  $InstallDBeaver = $true
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

$PG_VERSION          = "16.6"
# URL directa al ZIP oficial de binarios para Windows x64
$PG_INSTALLER_URL    = "https://sbp.enterprisedb.com/get/dbdownloads/postgresql-16.6-1-windows-x64-binaries.zip"
$PG_INSTALLER_FILE   = "postgresql-16-binaries.zip"
$PG_DEFAULT_DATA_DIR = "C:\Program Files\PostgreSQL\16\data"
$PG_BIN_CANDIDATES   = @("C:\Program Files\PostgreSQL\16\bin")

$DB_NAME        = "comercio"
$DB_USER        = "postgres"
$DB_DUMP_FILE   = "database\comercio_inicial.dump"
$DB_INIT_SCRIPT = "database\init_comercio_pg.sql"

$DBEAVER_WINGET_ID = "DBeaver.DBeaver"
$DBEAVER_EXE_CANDIDATES = @(
    "C:\Program Files\DBeaver\dbeaver.exe",
    "C:\Program Files\DBeaver Community\dbeaver.exe"
)

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

function Test-DBeaverInstalled {
    foreach ($path in $DBEAVER_EXE_CANDIDATES) {
        if (Test-Path $path) { return $true }
    }

    $uninstallRoots = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    )

    foreach ($root in $uninstallRoots) {
        if (Test-Path $root) {
            $match = Get-ChildItem $root -ErrorAction SilentlyContinue |
                Get-ItemProperty -ErrorAction SilentlyContinue |
                Where-Object { $_.DisplayName -like "*DBeaver*" } |
                Select-Object -First 1
            if ($match) { return $true }
        }
    }

    return $false
}

function Get-PgService {
    return Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue |
        Sort-Object Name |
        Select-Object -First 1
}

function Ensure-PgServiceRunning {
    param([int]$TimeoutSeconds = 45)

    $svc = Get-PgService
    if (-not $svc) { return $false }

    if ($svc.Status -ne 'Running') {
        try {
            Start-Service -Name $svc.Name -ErrorAction Stop
        } catch {
            return $false
        }
    }

    $elapsed = 0
    while ($elapsed -lt $TimeoutSeconds) {
        $svc = Get-Service -Name $svc.Name -ErrorAction SilentlyContinue
        if ($svc -and $svc.Status -eq 'Running') { return $true }
        Start-Sleep -Seconds 3
        $elapsed += 3
    }

    return $false
}

function Test-PgConfigSyntax {
    param(
        [string]$PgBin,
        [string]$PgDataDir
    )

    $postgresExe = Join-Path $PgBin "postgres.exe"
    if (-not (Test-Path $postgresExe)) { return $true }

    try {
        $output = & $postgresExe -D $PgDataDir -C data_directory 2>&1
        $exitCode = $LASTEXITCODE

        if ($exitCode -eq 0) { return $true }

        $outputStr = $output -join "`n"
        if ($outputStr -match "syntax error|invalid|fatal" -and $outputStr -notmatch "lock file") {
            return $false
        }

        return $true
    } catch {
        return $true
    }
}

function Restore-PgConfigBackups {
    param(
        [string]$PgConf,
        [string]$PgHba,
        [string]$PgConfBackup,
        [string]$PgHbaBackup
    )

    if (Test-Path $PgConfBackup) { Copy-Item $PgConfBackup $PgConf -Force }
    if (Test-Path $PgHbaBackup) { Copy-Item $PgHbaBackup $PgHba -Force }
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
        "-PgPort", $PgPort,
        "-InstallDBeaver", $InstallDBeaver
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
Write-Step "1/10" "Verificando .NET $DOTNET_VERSION Desktop Runtime..."

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
# PASO 2 - POSTGRESQL VIA BINARIOS ZIP (100% Controlable y Confiable)
# ===========================================================================
Write-Step "2/10" "Verificando PostgreSQL..."

$pgBin = Find-PgBin

if ($pgBin) {
    Write-OK "PostgreSQL encontrado en: $pgBin"
} else {
    Write-Info "PostgreSQL no detectado. Descargando binarios ZIP oficiales (~230 MB)..."
    $tmpPg = Join-Path $env:TEMP $PG_INSTALLER_FILE
    try {
        (New-Object System.Net.WebClient).DownloadFile($PG_INSTALLER_URL, $tmpPg)
        Write-OK "Descarga completada."
    } catch {
        Write-Fail "Error descargando el ZIP de PostgreSQL: $_"
        Read-Host "ENTER para salir"
        exit 1
    }

    if (Test-Path $tmpPg) {
        $pgTargetRoot = "C:\Program Files\PostgreSQL"
        if (-not (Test-Path $pgTargetRoot)) { New-Item -ItemType Directory -Path $pgTargetRoot -Force | Out-Null }

        Write-Info "Extrayendo binarios en $pgTargetRoot... (Puede demorar un minuto)"
        try {
            Expand-Archive -Path $tmpPg -DestinationPath $pgTargetRoot -Force
            if (Test-Path (Join-Path $pgTargetRoot "pgsql")) {
                Rename-Item -Path (Join-Path $pgTargetRoot "pgsql") -NewName "16" -Force
            }
            Write-OK "Extracción completada."
        } catch {
            Write-Fail "Error al extraer los archivos: $_"
            exit 1
        } finally {
            Remove-Item $tmpPg -Force -ErrorAction SilentlyContinue
        }

        $pgBin = Find-PgBin
        if (-not $pgBin) {
            Write-Fail "No se pudieron localizar los binarios extraídos."
            exit 1
        }

        # 1. Crear directorio Data y forzar permisos correctos antes de inicializar
        Write-Info "Configurando directorio de datos y permisos de Windows..."
        if (-not (Test-Path $PG_DEFAULT_DATA_DIR)) {
            New-Item -ItemType Directory -Path $PG_DEFAULT_DATA_DIR -Force | Out-Null
        }
        
        & icacls "C:\Program Files\PostgreSQL" /grant "*S-1-5-20:(OI)(CI)F" /T /C /Q | Out-Null
        & icacls "C:\Program Files\PostgreSQL" /grant "*S-1-5-19:(OI)(CI)F" /T /C /Q | Out-Null

        # 2. Inicializar la base de datos de manera nativa (initdb)
        Write-Info "Inicializando base de datos de forma limpia (initdb)..."
        $initDbExe = Join-Path $pgBin "initdb.exe"
        
        $pwFile = Join-Path $env:TEMP "pg_pw.txt"
        $PgPassword | Out-File $pwFile -Encoding ascii
        
        & $initDbExe -D "$PG_DEFAULT_DATA_DIR" -U postgres -A md5 --pwfile="$pwFile" --E=UTF8 | Out-Null
        Remove-Item $pwFile -Force -ErrorAction SilentlyContinue

        & icacls "$PG_DEFAULT_DATA_DIR" /grant "*S-1-5-20:(OI)(CI)F" /T /C /Q | Out-Null

        # 3. Registrar PostgreSQL como Servicio de Windows
        Write-Info "Registrando PostgreSQL como Servicio de Windows..."
        $pgCtlExe = Join-Path $pgBin "pg_ctl.exe"
        & $pgCtlExe register -N "postgresql-16" -D "$PG_DEFAULT_DATA_DIR" -w | Out-Null

        # 4. Iniciar el servicio recién creado
        Write-Info "Iniciando servicio por primera vez..."
        $svc = Get-Service -Name "postgresql-16" -ErrorAction SilentlyContinue
        if ($svc) {
            Set-Service -Name $svc.Name -StartupType Automatic
            Start-Service -Name $svc.Name -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 5
            
            $svc = Get-Service -Name $svc.Name
            if ($svc.Status -eq 'Running') {
                Write-OK "Servicio PostgreSQL levantado correctamente y corriendo."
            } else {
                Write-Fail "El motor se creó pero no inició. Revise el Visor de Eventos."
                Read-Host "ENTER para salir"
                exit 1
            }
        } else {
            Write-Fail "No se pudo registrar el servicio de Windows."
            exit 1
        }
    }
}

# ===========================================================================
# PASO 3 - CONFIGURAR POSTGRESQL PARA CONEXIONES REMOTAS
# ===========================================================================
Write-Step "3/10" "Configurando PostgreSQL para conexiones remotas..."

$pgDataDir = $null
if (Test-Path $PG_DEFAULT_DATA_DIR) {
    $pgDataDir = $PG_DEFAULT_DATA_DIR
} else {
    $pgSvcObj = Get-CimInstance -ClassName Win32_Service -Filter "Name LIKE 'postgresql%'" `
                -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($pgSvcObj -and $pgSvcObj.PathName -match "-D\s+`"?([^`"]+)`"?") {
        $pgDataDir = $Matches[1].Trim('"').Trim()
    }
}

if ($pgDataDir -and (Test-Path $pgDataDir)) {
    $pgConf = Join-Path $pgDataDir "postgresql.conf"
    $pgHba  = Join-Path $pgDataDir "pg_hba.conf"
    $pgConfBackup = "$pgConf.bak.comercionet"
    $pgHbaBackup  = "$pgHba.bak.comercionet"

    if (-not (Test-Path $pgConf) -or -not (Test-Path $pgHba)) {
        Write-Fail "No se encontraron archivos de configuracion de PostgreSQL en: $pgDataDir"
        Read-Host "ENTER para salir"
        exit 1
    }

    Copy-Item $pgConf $pgConfBackup -Force
    Copy-Item $pgHba  $pgHbaBackup  -Force
    Write-Info "Backups de configuracion creados (.bak.comercionet)."

    # Modificación de postgresql.conf sin alterar encoding
    $confLines = Get-Content $pgConf -Encoding UTF8
    $hasListenArr = $false
    $newConfLines = @()

    foreach ($line in $confLines) {
        if ($line -match "^\s*#?\s*listen_addresses\s*=") {
            $newConfLines += "listen_addresses = '*'"
            $hasListenArr = $true
        } else {
            $newConfLines += $line
        }
    }
    if (-not $hasListenArr) {
        $newConfLines += "listen_addresses = '*'"
    }

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllLines($pgConf, $newConfLines, $utf8NoBom)
    Write-OK "postgresql.conf: listen_addresses = '*'"

    # Modificación de pg_hba.conf
    $hbaLines = Get-Content $pgHba -Encoding UTF8
    $hbaLine  = "host    all             all             0.0.0.0/0               md5"
    
    if ($hbaLines -notcontains $hbaLine) {
        $newHbaLines = $hbaLines + @("", "# Comercio.NET - acceso remoto red local", $hbaLine)
        [System.IO.File]::WriteAllLines($pgHba, $newHbaLines, $utf8NoBom)
        Write-OK "pg_hba.conf: md5 desde 0.0.0.0/0 agregado."
    } else {
        Write-OK "pg_hba.conf: regla remota ya existente."
    }

    # Validar sintaxis antes de reiniciar (solo para errores criticos)
    $syntaxOk = Test-PgConfigSyntax -PgBin $pgBin -PgDataDir $pgDataDir
    if (-not $syntaxOk) {
        Write-Warn "Validacion de sintaxis reporto advertencias. Intentando arrancar de todos modos..."
    }

    # Reiniciar servicio para aplicar cambios
    $svc = Get-PgService
    if ($svc) {
        try {
            Write-Info "Reiniciando el servicio $($svc.Name)..."
            Restart-Service -Name $svc.Name -Force -ErrorAction Stop
            Start-Sleep -Seconds 5
            Write-OK "PostgreSQL reiniciado con nueva configuracion ($($svc.Name))."
        } catch {
            Write-Warn "No se pudo reiniciar PostgreSQL de forma estandar: $($_.Exception.Message)"
            Write-Info "Intentando iniciar servicio..."
            if (Ensure-PgServiceRunning -TimeoutSeconds 30) {
                Write-OK "Servicio PostgreSQL iniciado con exito tras reintento."
            } else {
                Write-Fail "No pudo iniciarse el servicio PostgreSQL."
                Write-Warn "Esto puede deberse a errores en la configuracion. Restaurando backups..."
                Restore-PgConfigBackups -PgConf $pgConf -PgHba $pgHba -PgConfBackup $pgConfBackup -PgHbaBackup $pgHbaBackup

                Write-Info "Intentando reiniciar con configuracion original..."
                try {
                    Restart-Service -Name $svc.Name -Force -ErrorAction Stop
                    Start-Sleep -Seconds 5
                    if ((Get-Service -Name $svc.Name).Status -eq 'Running') {
                        Write-Warn "Servicio PostgreSQL iniciado con configuracion original."
                        Write-Warn "Las conexiones remotas no estan habilitadas."
                        Write-Warn "Configure manualmente postgresql.conf y pg_hba.conf"
                    } else {
                        Write-Fail "El servicio PostgreSQL no arranca. Revise logs en: $pgDataDir\log"
                        Read-Host "ENTER para salir"
                        exit 1
                    }
                } catch {
                    Write-Fail "No se pudo reiniciar PostgreSQL. Revise services.msc y logs."
                    Read-Host "ENTER para salir"
                    exit 1
                }
            }
        }
    }
} else {
    Write-Warn "No se encontro el directorio de datos de PostgreSQL."
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
    Write-Warn "No se pudo configure el firewall: $_"
}

# ===========================================================================
# PASO 4 - OBTENER ULTIMA VERSION DE GITHUB
# ===========================================================================
Write-Step "4/10" "Consultando ultima version en GitHub..."

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
Write-Step "5/10" "Descargando $APP_NAME v$version..."

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
Write-Step "6/10" "Instalando en $InstallDir..."

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
Write-Step "7/10" "Inicializando base de datos PostgreSQL..."

$env:PGPASSWORD = $PgPassword
$psqlExe        = Join-Path $pgBin "psql.exe"
$pgRestoreExe   = Join-Path $pgBin "pg_restore.exe"
$createdbExe    = Join-Path $pgBin "createdb.exe"

if (-not (Ensure-PgServiceRunning -TimeoutSeconds 30)) {
    Write-Fail "PostgreSQL no esta en ejecucion. No se puede inicializar la base de datos."
    Write-Warn "Inicie el servicio PostgreSQL desde services.msc y reintente el instalador."
    Read-Host "ENTER para salir"
    exit 1
}

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

# Restaurar desde dump o ejecutar DDL (prioridad a SQL plano por portabilidad)
$dumpPathSQL   = Join-Path $InstallDir "database\comercio_inicial.sql"
$dumpPath      = Join-Path $InstallDir $DB_DUMP_FILE
$initScript    = Join-Path $InstallDir $DB_INIT_SCRIPT

if (Test-Path $dumpPathSQL) {
    Write-Info "Restaurando desde comercio_inicial.sql (formato SQL plano)..."
    $env:PGPASSWORD = $PgPassword
    & $psqlExe -U $DB_USER -p $PgPort -d $DB_NAME -f $dumpPathSQL 2>&1 |
        ForEach-Object { Write-Info $_ }
    if ($LASTEXITCODE -eq 0) {
        Write-OK "Base de datos restaurada desde dump SQL plano."
    } else {
        Write-Warn "psql codigo $LASTEXITCODE. Algunos errores pueden ser normales en reinstalacion."
    }
} elseif (Test-Path $dumpPath) {
    Write-Info "Restaurando desde comercio_inicial.dump (formato custom)..."
    $env:PGPASSWORD = $PgPassword
    & $pgRestoreExe `
        -U $DB_USER -p $PgPort -d $DB_NAME `
        --no-owner --no-acl --if-exists -c `
        $dumpPath 2>&1 | ForEach-Object { Write-Info $_ }
    if ($LASTEXITCODE -eq 0) {
        Write-OK "Base de datos restaurada desde dump custom."
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
Write-Step "8/10" "Configurando appsettings.json..."

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
Write-Step "9/10" "Creando acceso directo en el escritorio..."

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
# VALIDACIONES AUTOMATICAS POST-INSTALACION
# ===========================================================================
Write-Step "VALIDACION" "Ejecutando validaciones post-instalacion..."

$validacionesOk = $true

# V1: Servicio PostgreSQL
$svc = Get-PgService
if ($svc -and $svc.Status -eq 'Running') {
    Write-OK "V1 Servicio PostgreSQL en ejecucion ($($svc.Name))."
} else {
    Write-Fail "V1 Servicio PostgreSQL no esta en ejecucion."
    $validacionesOk = $false
}

# V2: Conexion SQL a PostgreSQL
$env:PGPASSWORD = $PgPassword
$testQuery = (& $psqlExe -U $DB_USER -p $PgPort -d postgres -tAc "SELECT 1;" 2>&1) -join ""
if (($LASTEXITCODE -eq 0) -and ($testQuery.Trim() -eq '1')) {
    Write-OK "V2 Conexion PostgreSQL local correcta (SELECT 1)."
} else {
    Write-Fail "V2 Fallo de conexion PostgreSQL local en puerto $PgPort."
    Write-Info "Detalle: $testQuery"
    $validacionesOk = $false
}

# V3: Base de datos objetivo creada
$dbExists = (& $psqlExe -U $DB_USER -p $PgPort -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$DB_NAME';" 2>&1) -join ""
if (($LASTEXITCODE -eq 0) -and ($dbExists.Trim() -eq '1')) {
    Write-OK "V3 Base de datos '$DB_NAME' existe."
} else {
    Write-Fail "V3 Base de datos '$DB_NAME' no encontrada."
    Write-Info "Detalle: $dbExists"
    $validacionesOk = $false
}

# V4: Regla de firewall
try {
    $fw = netsh advfirewall firewall show rule name="PostgreSQL $PgPort" 2>$null
    if ($fw -match "PostgreSQL $PgPort") {
        Write-OK "V4 Regla de firewall para puerto $PgPort presente."
    } else {
        Write-Fail "V4 Regla de firewall para puerto $PgPort no encontrada."
        $validacionesOk = $false
    }
} catch {
    Write-Fail "V4 No se pudo validar firewall: $($_.Exception.Message)"
    $validacionesOk = $false
}

# V5: appsettings.json
if (Test-Path $appSettings) {
    $appSettingsRaw = Get-Content $appSettings -Raw -ErrorAction SilentlyContinue
    if ($appSettingsRaw -match [regex]::Escape($connString)) {
        Write-OK "V5 appsettings.json contiene la cadena de conexion esperada."
    } else {
        Write-Warn "V5 appsettings.json no coincide exactamente con la cadena generada."
        Write-Info "Verificar manualmente: $appSettings"
    }
} else {
    Write-Fail "V5 appsettings.json no encontrado en: $appSettings"
    $validacionesOk = $false
}

Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue

if ($validacionesOk) {
    Write-OK "Validaciones post-instalacion: COMPLETAS."
} else {
    Write-Warn "Validaciones post-instalacion: CON ERRORES. Revise mensajes anteriores."
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
Write-Host "  CONEXION DESDE OTRAS PCS:" -ForegroundColor Yellow
Write-Host "  $connString" -ForegroundColor Cyan
Write-Host ""
Write-Host "  USUARIO BD: $DB_USER  |  Password: $PgPassword" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Iniciar la aplicacion desde el acceso directo del escritorio." -ForegroundColor White
if ($InstallDBeaver) {
    if (Test-DBeaverInstalled) {
        Write-Host "  4. DBeaver instalado: use Host=$localIp, Port=$PgPort, Database=$DB_NAME" -ForegroundColor White
    } else {
        Write-Host "  4. DBeaver no instalado automaticamente. Descarga: https://dbeaver.io/download/" -ForegroundColor Yellow
    }
}
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host ""

$resp = Read-Host "Desea abrir la carpeta de instalacion ahora? (S/N)"
if ($resp -match "^[sS]") {
    Start-Process explorer.exe -ArgumentList $InstallDir
}
