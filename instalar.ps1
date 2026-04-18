#Requires -Version 5.1
<#
.SYNOPSIS
    Instalador de Comercio .NET
    
.DESCRIPTION
    Descarga e instala Comercio .NET desde GitHub Releases.
    Instala el .NET 8 Runtime si no esta presente.
    Crea la carpeta de instalacion, un acceso directo en el escritorio
    y genera un appsettings.json inicial listo para configurar.

.PARAMETER InstallDir
    Carpeta de instalacion. Por defecto: C:\Comercio.NET

.PARAMETER GitHubRepo
    Repositorio de GitHub en formato owner/repo.

.PARAMETER GitHubToken
    Token de acceso personal para repositorios privados (opcional).

.EXAMPLE
irm https://raw.githubusercontent.com/manuclaro/Comercio.NET-web/master/instalar.ps1 | iex

.EXAMPLE
    .\instalar.ps1 -InstallDir "D:\MiComercio"
#>

[CmdletBinding()]
param(
    [string]$InstallDir    = "C:\Comercio.NET",
    [string]$GitHubRepo    = "manuclaro/Comercio.NET-web",
    [string]$GitHubToken   = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# =============================================================================
# CONSTANTES
# =============================================================================
$APP_NAME          = "Comercio .NET"
$APP_EXE           = "Comercio .NET.exe"
$DOTNET_VERSION    = "8.0"
$DOTNET_RUNTIME_URL = "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe"
$DOTNET_RUNTIME_FILENAME = "windowsdesktop-runtime-8.0-win-x64.exe"
$DB_INIT_SCRIPT    = "database\init_comercio.sql"
$DB_NAME           = "comercio"
# SQL Server Express 2022 - winget package ID (principal) + SSEI URL de respaldo
$SQLEXPRESS_WINGET_ID = "Microsoft.SQLServer.2022.Express"
$SQLEXPRESS_URL       = "https://go.microsoft.com/fwlink/p/?linkid=2215158&clcid=0x0409&culture=en-us&country=us"
$SQLEXPRESS_FILENAME  = "SQL2022-SSEI-Expr.exe"
$SQL_INSTANCE_NAME    = "SQLEXPRESS"
# SSMS 20.2 - URL oficial de descarga directa
$SSMS_URL      = "https://aka.ms/ssmsfullsetup"
$SSMS_FILENAME = "SSMS-Setup-ENU.exe"

# =============================================================================
# DIAGNOSTICO DE BASE DE DATOS
# =============================================================================
function Test-DatabaseDiagnostic {
    param(
        [string]$AppSettingsPath,
        [string]$InstallDir,
        [string]$DbInitScript
    )

    Write-Host ""
    Write-Host "  DIAGNOSTICO DE BASE DE DATOS" -ForegroundColor Yellow
    Write-Host "  ====================================================" -ForegroundColor Yellow

    # 1. Servicio SQL Server
    $sqlSvc = Get-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction SilentlyContinue
    if (-not $sqlSvc) {
        $sqlSvc = Get-Service -Name "MSSQLSERVER" -ErrorAction SilentlyContinue
    }

    if ($sqlSvc -and $sqlSvc.Status -eq 'Running') {
        Write-Host "  [OK] Servicio SQL Server   : CORRIENDO ($($sqlSvc.Name))" -ForegroundColor Green
    } elseif ($sqlSvc) {
        Write-Host "  [!!] Servicio SQL Server   : $($sqlSvc.Status) ($($sqlSvc.Name))" -ForegroundColor Red
        Write-Host "       Intente: Start-Service '$($sqlSvc.Name)'" -ForegroundColor Gray
    } else {
        Write-Host "  [XX] Servicio SQL Server   : NO ENCONTRADO" -ForegroundColor Red
        Write-Host "       SQL Server no esta instalado en este equipo." -ForegroundColor Gray
        Write-Host "       Descargue e instale desde: https://www.microsoft.com/sql-server/sql-server-downloads" -ForegroundColor Gray
    }

    # 2. sqlcmd disponible
    $sqlcmdFound = $null
    $sqlcmdCandidates = @(
        "sqlcmd",
        "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\160\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles}\Microsoft SQL Server\160\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles}\Microsoft SQL Server\150\Tools\Binn\sqlcmd.exe"
    )
    foreach ($c in $sqlcmdCandidates) {
        try {
            $r = & $c -? 2>$null
            if ($LASTEXITCODE -eq 0 -or $r) { $sqlcmdFound = $c; break }
        } catch { }
    }

    if ($sqlcmdFound) {
        Write-Host "  [OK] sqlcmd                : $sqlcmdFound" -ForegroundColor Green
    } else {
        Write-Host "  [!!] sqlcmd                : NO ENCONTRADO" -ForegroundColor Magenta
        Write-Host "       Instale las SQL Server Command Line Utilities:" -ForegroundColor Gray
        Write-Host "       https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-utility" -ForegroundColor Gray
    }

    # 3. Conectividad al servidor y existencia de la base de datos 'comercio'
    if ($sqlcmdFound -and $sqlSvc -and $sqlSvc.Status -eq 'Running') {
        $svcName  = $sqlSvc.Name
        $connStr  = if ($svcName -eq "MSSQLSERVER") { "localhost" } else { "localhost\$($svcName -replace 'MSSQL\$','')" }

        # Intentar con autenticacion Windows
        $dbExists = $null
        try {
            $dbExists = & $sqlcmdFound -S $connStr -E -Q "SELECT name FROM sys.databases WHERE name='comercio'" -b -l 10 2>&1
        } catch { }

        if ($dbExists -match "comercio") {
            Write-Host "  [OK] Base de datos 'comercio': EXISTE en $connStr" -ForegroundColor Green
        } else {
            Write-Host "  [XX] Base de datos 'comercio': NO EXISTE en $connStr" -ForegroundColor Red
            Write-Host "       Cree la base de datos ejecutando el siguiente script en SSMS o sqlcmd:" -ForegroundColor Gray
            Write-Host "       $DbInitScript" -ForegroundColor Cyan
            Write-Host "       O ejecute directamente:" -ForegroundColor Gray
            Write-Host "       sqlcmd -S $connStr -E -i `"$DbInitScript`"" -ForegroundColor Cyan
        }
    }

    # 4. Cadena de conexion en appsettings.json
    if (Test-Path $AppSettingsPath) {
        try {
            $json = Get-Content $AppSettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
            $connStringActual = $json.ConnectionStrings.DefaultConnection
            if ($connStringActual) {
                Write-Host "  [>>] Connection string actual:" -ForegroundColor Gray
                Write-Host "       $connStringActual" -ForegroundColor Cyan
            }
        } catch { }
    } else {
        Write-Host "  [XX] appsettings.json      : NO ENCONTRADO en $AppSettingsPath" -ForegroundColor Red
    }

    Write-Host "  ====================================================" -ForegroundColor Yellow
    Write-Host ""
}

# =============================================================================
# HELPERS DE CONSOLA
# =============================================================================
function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host ("=" * 60) -ForegroundColor Cyan
    Write-Host "  $Text" -ForegroundColor Cyan
    Write-Host ("=" * 60) -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step {
    param([string]$Numero, [string]$Texto)
    Write-Host "[$Numero] $Texto" -ForegroundColor Yellow
}

function Write-OK   { param([string]$Msg) Write-Host "    OK  $Msg" -ForegroundColor Green  }
function Write-Info { param([string]$Msg) Write-Host "    >>  $Msg" -ForegroundColor Gray   }
function Write-Warn { param([string]$Msg) Write-Host "    !!  $Msg" -ForegroundColor Magenta }
function Write-Fail { param([string]$Msg) Write-Host "    XX  $Msg" -ForegroundColor Red    }

# =============================================================================
# HELPERS DE DIAGNOSTICO SQL
# =============================================================================
function Get-SqlSetupLogSummary {
    <#
    .SYNOPSIS
        Lee el log de instalacion de SQL Server y devuelve las lineas de error mas relevantes.
    #>
    $logRoot = "C:\Program Files\Microsoft SQL Server"
    $bootstrapLog = $null

    # Buscar el ultimo directorio de log de Bootstrap
    $bootstrapDirs = @(
        "$logRoot\160\Setup Bootstrap\Log",
        "$logRoot\150\Setup Bootstrap\Log",
        "$logRoot\140\Setup Bootstrap\Log"
    )

    foreach ($dir in $bootstrapDirs) {
        if (Test-Path $dir) {
            $latest = Get-ChildItem -Path $dir -Directory -ErrorAction SilentlyContinue |
                      Sort-Object LastWriteTime -Descending |
                      Select-Object -First 1
            if ($latest) {
                $summaryFile = Get-ChildItem -Path $latest.FullName -Filter "Summary*.txt" -ErrorAction SilentlyContinue |
                               Select-Object -First 1
                if ($summaryFile) {
                    $bootstrapLog = $summaryFile.FullName
                    break
                }
            }
        }
    }

    if ($bootstrapLog -and (Test-Path $bootstrapLog)) {
        Write-Warn "--- Extracto del log de instalacion SQL Server ---"
        Write-Info "Archivo: $bootstrapLog"
        # Mostrar lineas con errores o estado final
        $lines = Get-Content $bootstrapLog -ErrorAction SilentlyContinue
        $relevant = $lines | Where-Object {
            $_ -match '(Failed|Error|Exception|Exit code|Overall summary|Passed|warning)' -and
            $_ -notmatch '^\s*$'
        } | Select-Object -Last 30
        foreach ($line in $relevant) {
            Write-Info $line
        }
        Write-Warn "--- Fin del extracto ---"
        Write-Info "Log completo en: $bootstrapLog"
    } else {
        Write-Info "No se encontro log de Bootstrap de SQL Server en rutas estandar."
    }
}

function Test-SqlPartialInstall {
    <#
    .SYNOPSIS
        Detecta instalaciones parciales o pendientes de reinicio de SQL Server
        que podrian causar el error 1603.
    #>
    $issues = @()

    # 1. Verificar reinicio pendiente SOLO por claves confiables (CBS y WindowsUpdate).
    #    PendingFileRenameOperations se ignora: casi siempre tiene contenido en Windows
    #    por actualizaciones rutinarias y genera falsos positivos constantemente.
    $rebootPending = $false
    if (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending") {
        $rebootPending = $true
    }
    if (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired") {
        $rebootPending = $true
    }

    if ($rebootPending) {
        $issues += "REINICIO_PENDIENTE: El sistema tiene actualizaciones pendientes que requieren reinicio."
    }

    # 2. Verificar si existe la clave de instalacion parcial de SQL Server
    $sqlPendingKey = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\SQLEXPRESS"
    if (Test-Path $sqlPendingKey) {
        $issues += "INSTANCIA_PREVIA: Se detectaron restos de una instalacion anterior de SQLEXPRESS."
    }

    # 3. Verificar si el servicio existe pero no esta corriendo (instalacion rota)
    $svc = Get-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction SilentlyContinue
    if ($svc -and $svc.Status -ne 'Running') {
        $issues += "SERVICIO_DETENIDO: El servicio MSSQL`$SQLEXPRESS existe pero no esta corriendo (estado: $($svc.Status))."
    }

    return $issues
}

# =============================================================================
# PASO 0 - VERIFICAR PRIVILEGIOS DE ADMINISTRADOR
# =============================================================================
function Test-Admin {
    $current = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($current)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Admin)) {
    Write-Warn "El instalador necesita permisos de administrador."
    Write-Warn "Reiniciando con elevacion..."
    Start-Sleep -Seconds 2

    $args = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", "`"$PSCommandPath`"",
        "-InstallDir", "`"$InstallDir`"",
        "-GitHubRepo", "`"$GitHubRepo`""
    )
    if ($GitHubToken) { $args += @("-GitHubToken", "`"$GitHubToken`"") }

    Start-Process powershell -Verb RunAs -ArgumentList $args
    exit
}

# =============================================================================
# ENCABEZADO
# =============================================================================
Clear-Host
Write-Header "INSTALADOR DE $APP_NAME"
Write-Info "Repositorio : $GitHubRepo"
Write-Info "Destino     : $InstallDir"
Write-Info "Fecha/Hora  : $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')"
Write-Host ""

# =============================================================================
# PASO 1 - VERIFICAR / INSTALAR .NET 8 RUNTIME
# =============================================================================
Write-Step "1/11" "Verificando .NET $DOTNET_VERSION Runtime..."

$dotnetInstalled = $false
try {
    $runtimes = & dotnet --list-runtimes 2>$null
    if ($runtimes -match "Microsoft\.WindowsDesktop\.App $DOTNET_VERSION") {
        $dotnetInstalled = $true
        Write-OK ".NET $DOTNET_VERSION Desktop Runtime ya instalado."
    }
} catch { }

if (-not $dotnetInstalled) {
    Write-Info ".NET $DOTNET_VERSION no encontrado. Descargando instalador (~56 MB)..."

    $tempRuntime = Join-Path $env:TEMP $DOTNET_RUNTIME_FILENAME
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri $DOTNET_RUNTIME_URL -OutFile $tempRuntime -UseBasicParsing -ErrorAction Stop
        Write-OK "Descarga completada."
    } catch {
        Write-Fail "Error descargando .NET runtime: $_"
        Write-Warn "Instale manualmente desde: https://dotnet.microsoft.com/download/dotnet/8.0"
        Read-Host "Presione ENTER para continuar o Ctrl+C para cancelar"
    }

    if (Test-Path $tempRuntime) {
        Write-Info "Instalando .NET $DOTNET_VERSION Runtime (puede tardar unos minutos)..."
        $proc = Start-Process -FilePath $tempRuntime -ArgumentList "/quiet /norestart" -Wait -PassThru
        if ($proc.ExitCode -eq 0) {
            Write-OK ".NET $DOTNET_VERSION instalado correctamente."
        } else {
            Write-Warn "Instalador .NET finalizo con codigo $($proc.ExitCode)."
        }
        Remove-Item $tempRuntime -Force -ErrorAction SilentlyContinue
    }
} else {
    Write-OK ".NET $DOTNET_VERSION listo."
}

# =============================================================================
# PASO 2 - VERIFICAR / INSTALAR SQL SERVER EXPRESS
# =============================================================================
Write-Step "2/11" "Verificando SQL Server..."

$sqlInstalled    = $false
$sqlInstanceName = $null

$sqlRegPaths = @(
    "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server\Instance Names\SQL"
)
foreach ($regPath in $sqlRegPaths) {
    if (Test-Path $regPath) {
        $instances = Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue
        if ($instances) {
            $firstInstance = ($instances.PSObject.Properties |
                Where-Object { $_.Name -notmatch '^PS' } |
                Select-Object -First 1).Name
            if ($firstInstance) {
                $sqlInstalled    = $true
                $sqlInstanceName = $firstInstance
                break
            }
        }
    }
}

$script:SqlInstallOk = $false

if ($sqlInstalled) {
    Write-OK "SQL Server encontrado. Instancia: $sqlInstanceName"
    $script:SqlServerConn = if ($sqlInstanceName -eq "MSSQLSERVER") {
        "localhost"
    } else {
        "localhost\$sqlInstanceName"
    }
    $script:SqlInstallOk = $true
} else {
    # -------------------------------------------------------------------------
    # PRE-FLIGHT: Verificar condiciones que causan error 1603
    # -------------------------------------------------------------------------
    Write-Info "Verificando pre-requisitos de instalacion..."
    $preflightIssues = @(Test-SqlPartialInstall)

    if ($preflightIssues.Count -gt 0) {
        foreach ($issue in $preflightIssues) {
            Write-Warn "Aviso pre-instalacion: $issue"
        }
        if ($preflightIssues -match "INSTANCIA_PREVIA|SERVICIO_DETENIDO") {
            Write-Warn "Se detectaron restos de una instalacion anterior de SQL Server."
            Write-Warn "Se intentara la instalacion de todos modos."
            Write-Warn "Si falla, desinstale manualmente SQL Server desde 'Agregar o quitar programas'"
            Write-Warn "y vuelva a ejecutar el instalador."
        }
    } else {
        Write-OK "Pre-requisitos verificados correctamente."
    }

    Write-Info "SQL Server no detectado. Iniciando instalacion de SQL Server 2022 Express..."
    Write-Host ""

    $sqlInstalledOk = $false
    $sqlSetupArgs   = "/Q /ACTION=Install /FEATURES=SQLEngine " +
                      "/INSTANCENAME=SQLEXPRESS " +
                      "/SQLSVCACCOUNT=`"NT AUTHORITY\NETWORK SERVICE`" " +
                      "/SQLSYSADMINACCOUNTS=`"BUILTIN\Administrators`" " +
                      "/TCPENABLED=1 /NPENABLED=0 " +
                      "/IACCEPTSQLSERVERLICENSETERMS"

    # Helper: ejecutar setup.exe con los argumentos de instalacion y esperar resultado
    function Invoke-SqlSetup {
        param([string]$SetupPath)
        Write-Info "Ejecutando: $SetupPath"
        Write-Info "(Esto puede tardar entre 5 y 15 minutos)"
        $proc = Start-Process -FilePath $SetupPath -ArgumentList $sqlSetupArgs -PassThru
        $done = $proc.WaitForExit(40 * 60 * 1000)
        $code = if ($done) { $proc.ExitCode } else { $proc.Kill(); -1 }
        Write-Info "Codigo de salida: $code"
        return $code
    }

    # -------------------------------------------------------------------------
    # Metodo 1: winget — ID de paquete estable, no depende de URLs de Microsoft
    # Disponible en Windows 10 1709+ / Windows 11
    # -------------------------------------------------------------------------
    $wingetCmd = Get-Command winget -ErrorAction SilentlyContinue
    if ($wingetCmd) {
        Write-Info "[Metodo 1/3] Instalando via winget (Microsoft.SQLServer.2022.Express)..."

        $wingetArgs = "install --id $SQLEXPRESS_WINGET_ID -e --silent " +
                      "--accept-source-agreements --accept-package-agreements " +
                      "--override `"$sqlSetupArgs`""

        $procWinget = Start-Process -FilePath "winget" -ArgumentList $wingetArgs -PassThru
        $wgFinished = $procWinget.WaitForExit(60 * 60 * 1000)

        if ($wgFinished -and ($procWinget.ExitCode -eq 0 -or $procWinget.ExitCode -eq -1978335189)) {
            Write-OK "SQL Server 2022 Express instalado via winget."
            $sqlInstalledOk = $true
        } elseif (-not $wgFinished) {
            Write-Warn "winget supero 60 minutos. Intentando metodo alternativo..."
            $procWinget.Kill()
        } else {
            Write-Warn "winget finalizo con codigo $($procWinget.ExitCode). Intentando metodo alternativo..."
        }
    } else {
        Write-Warn "[Metodo 1/3] winget no disponible en este equipo."
    }

    # -------------------------------------------------------------------------
    # Metodo 2: SSEI descarga ISO ? montar ? setup.exe
    # El SSEI bootstrapper NO acepta parametros de instalacion directamente.
    # Hay que usarlo para descargar el medio (ISO) y luego ejecutar setup.exe.
    # -------------------------------------------------------------------------
    if (-not $sqlInstalledOk) {
        Write-Info "[Metodo 2/3] Descargando medio SQL Server 2022 Express via SSEI..."
        Write-Info "(~280 MB, puede tardar varios minutos segun la conexion)"

        $tempSsei     = Join-Path $env:TEMP $SQLEXPRESS_FILENAME
        $tempSqlMedia = Join-Path $env:TEMP "SqlExpressMedia"
        Remove-Item $tempSsei     -Force -ErrorAction SilentlyContinue
        Remove-Item $tempSqlMedia -Recurse -Force -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Path $tempSqlMedia -Force | Out-Null

        $sseiOk = $false
        try {
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
            Invoke-WebRequest -Uri $SQLEXPRESS_URL -OutFile $tempSsei -UseBasicParsing -ErrorAction Stop
            Write-OK "SSEI descargado."
            $sseiOk = $true
        } catch {
            Write-Fail "Error descargando SSEI: $_"
        }

        if ($sseiOk) {
            # Descargar el medio como ISO (un solo archivo .iso bien definido)
            Write-Info "Descargando ISO de instalacion..."
            $dlProc = Start-Process -FilePath $tempSsei `
                        -ArgumentList "/Action=Download /MediaPath=`"$tempSqlMedia`" /MediaType=ISO /Quiet" `
                        -Wait -PassThru
            Remove-Item $tempSsei -Force -ErrorAction SilentlyContinue

            if ($dlProc.ExitCode -eq 0) {
                $isoFile = Get-ChildItem -Path $tempSqlMedia -Filter "*.iso" -ErrorAction SilentlyContinue |
                           Select-Object -First 1

                if ($isoFile) {
                    Write-OK "ISO descargado: $($isoFile.Name)"
                    try {
                        Write-Info "Montando ISO..."
                        $mountResult = Mount-DiskImage -ImagePath $isoFile.FullName -PassThru -ErrorAction Stop
                        $driveLetter = ($mountResult | Get-Volume).DriveLetter
                        $setupPath   = "${driveLetter}:\setup.exe"

                        if (Test-Path $setupPath) {
                            $exitCode = Invoke-SqlSetup -SetupPath $setupPath
                            if ($exitCode -eq 0 -or $exitCode -eq 3010 -or $exitCode -eq 1641) {
                                Write-OK "SQL Server 2022 Express instalado correctamente."
                                if ($exitCode -eq 3010) { Write-Warn "Se requiere reiniciar el equipo." }
                                $sqlInstalledOk = $true
                            } else {
                                Write-Fail "setup.exe finalizo con codigo $exitCode."
                                if ($exitCode -eq 1603) {
                                    Write-Warn "ERROR 1603: Reinicie el equipo, desinstale restos de SQL Server y reintente."
                                }
                                if ($exitCode -eq -1) { Write-Fail "Timeout: setup supero 40 minutos." }
                                Get-SqlSetupLogSummary
                            }
                        } else {
                            Write-Fail "No se encontro setup.exe en el ISO montado (unidad $driveLetter)."
                        }

                        Dismount-DiskImage -ImagePath $isoFile.FullName -ErrorAction SilentlyContinue
                    } catch {
                        Write-Fail "Error montando el ISO: $_"
                    }
                } else {
                    Write-Fail "El SSEI no genero un archivo ISO en $tempSqlMedia."
                    Write-Info "Archivos descargados: $(Get-ChildItem $tempSqlMedia | Select-Object -ExpandProperty Name)"
                }
            } else {
                Write-Fail "El SSEI finalizo la descarga con codigo $($dlProc.ExitCode)."
            }
        }

        Remove-Item $tempSqlMedia -Recurse -Force -ErrorAction SilentlyContinue
    }

    $script:SqlInstallOk = $sqlInstalledOk

    if ($script:SqlInstallOk) {
        $script:SqlServerConn = "localhost\$SQL_INSTANCE_NAME"

        Write-Info "Esperando que el servicio SQL Server inicie..."
        $sqlService = "MSSQL`$$SQL_INSTANCE_NAME"
        $intentos = 0
        do {
            Start-Sleep -Seconds 3
            $intentos++
            $svc = Get-Service -Name $sqlService -ErrorAction SilentlyContinue
        } while (($svc -eq $null -or $svc.Status -ne 'Running') -and ($intentos -lt 20))

        if ($svc -and $svc.Status -eq 'Running') {
            Write-OK "Servicio SQL Server listo."
        } else {
            Write-Warn "El servicio no respondio en 60 segundos. Se intentara continuar."
        }

        $sqlToolsPath = "${env:ProgramFiles}\Microsoft SQL Server\160\Tools\Binn"
        if (Test-Path $sqlToolsPath) {
            $env:PATH = "$sqlToolsPath;$env:PATH"
            Write-Info "Ruta de sqlcmd agregada al PATH de la sesion."
        }
    } else {
        $script:SqlServerConn = "localhost\$SQL_INSTANCE_NAME"
    }
}

# =============================================================================
# PASO 3 - OBTENER ULTIMA VERSION DE GITHUB
# =============================================================================
Write-Step "3/11" "Consultando ultima version en GitHub..."

$headers = @{
    "User-Agent" = "ComercioNET-Installer/1.0"
    "Accept"     = "application/vnd.github.v3+json"
}
if ($GitHubToken) {
    $headers["Authorization"] = "Bearer $GitHubToken"
}

$apiUrl = "https://api.github.com/repos/$GitHubRepo/releases/latest"

try {
    $release = Invoke-RestMethod -Uri $apiUrl -Headers $headers -ErrorAction Stop
} catch {
    Write-Fail "No se pudo conectar a GitHub: $_"
    Write-Warn "Verifique la conexion a internet y que el repositorio exista."
    Read-Host "Presione ENTER para salir"
    exit 1
}

$version    = $release.tag_name -replace '^v', ''
$zipAsset   = $release.assets | Where-Object { $_.name -like "*.zip" } | Select-Object -First 1

if (-not $zipAsset) {
    Write-Fail "No se encontro un archivo .zip en el release $version."
    Read-Host "Presione ENTER para salir"
    exit 1
}

$downloadUrl = if ($GitHubToken) { $zipAsset.url } else { $zipAsset.browser_download_url }
$sizeMB      = [math]::Round($zipAsset.size / 1MB, 1)

Write-OK "Version encontrada : $version"
Write-Info "Archivo            : $($zipAsset.name) ($sizeMB MB)"
Write-Info "Publicado          : $($release.published_at)"

# =============================================================================
# PASO 4 - DESCARGAR Y EXTRAER LA APLICACION
# =============================================================================
Write-Step "4/11" "Descargando $APP_NAME v$version..."

$tempZip    = Join-Path $env:TEMP "ComercioNET_Install_$version.zip"
$tempExtract = Join-Path $env:TEMP "ComercioNET_Install_$version"

Remove-Item $tempZip     -Force -ErrorAction SilentlyContinue
Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue

try {
    $downloadHeaders = $headers.Clone()
    if ($GitHubToken -and $downloadUrl -match "api\.github\.com") {
        $downloadHeaders["Accept"] = "application/octet-stream"
    }

    $wc = New-Object System.Net.WebClient
    foreach ($key in $downloadHeaders.Keys) {
        $wc.Headers.Add($key, $downloadHeaders[$key])
    }

    Write-Info "Descargando desde GitHub..."
    $wc.DownloadFile($downloadUrl, $tempZip)
    Write-OK "Descarga completada."
} catch {
    Write-Fail "Error en la descarga: $_"
    Read-Host "Presione ENTER para salir"
    exit 1
}

Write-Info "Extrayendo archivos..."
try {
    Expand-Archive -LiteralPath $tempZip -DestinationPath $tempExtract -Force
    Write-OK "Archivos extraidos."
} catch {
    Write-Fail "Error extrayendo el zip: $_"
    exit 1
}

# =============================================================================
# PASO 5 - CREAR CARPETA DE INSTALACION Y COPIAR ARCHIVOS
# =============================================================================
Write-Step "5/11" "Instalando en $InstallDir..."

$archivosProtegidos = @(
    "appsettings.json",
    "loginconfig.json",
    "afip_tokens.json",
    "debug_auth.txt",
    "version.txt"
)

$backupDir = $null
if (Test-Path $InstallDir) {
    $backupDir = Join-Path $env:TEMP "ComercioNET_ConfigBackup_$(Get-Date -Format 'yyyyMMddHHmmss')"
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

    foreach ($archivo in $archivosProtegidos) {
        $origen = Join-Path $InstallDir $archivo
        if (Test-Path $origen) {
            Copy-Item $origen -Destination $backupDir -Force
            Write-Info "Backup: $archivo"
        }
    }

    $certDir = Join-Path $InstallDir "Certificados FE"
    if (Test-Path $certDir) {
        $backupCertDir = Join-Path $backupDir "Certificados FE"
        Copy-Item $certDir -Destination $backupCertDir -Recurse -Force
        Write-Info "Backup: carpeta 'Certificados FE'"
    }

    $migrDir = Join-Path $InstallDir "migrations"
    if (Test-Path $migrDir) {
        $backupMigrDir = Join-Path $backupDir "migrations"
        Copy-Item $migrDir -Destination $backupMigrDir -Recurse -Force
        Write-Info "Backup: carpeta 'migrations'"
    }

    Write-OK "Backup de configuracion guardado en $backupDir"
}

New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null

$extractedItems = Get-ChildItem -Path $tempExtract
$sourceDir = if ($extractedItems.Count -eq 1 -and $extractedItems[0].PSIsContainer) {
    $extractedItems[0].FullName
} else {
    $tempExtract
}

Write-Info "Copiando archivos de la aplicacion..."
Copy-Item -Path "$sourceDir\*" -Destination $InstallDir -Recurse -Force
Write-OK "Archivos copiados."

if ($backupDir -and (Test-Path $backupDir)) {
    Write-Info "Restaurando configuraciones anteriores..."

    foreach ($archivo in $archivosProtegidos) {
        $backupArchivo = Join-Path $backupDir $archivo
        if (Test-Path $backupArchivo) {
            Copy-Item $backupArchivo -Destination $InstallDir -Force
            Write-Info "  Restaurado: $archivo"
        }
    }

    $backupCertDir = Join-Path $backupDir "Certificados FE"
    if (Test-Path $backupCertDir) {
        $destCertDir = Join-Path $InstallDir "Certificados FE"
        New-Item -ItemType Directory -Path $destCertDir -Force | Out-Null
        Copy-Item "$backupCertDir\*" -Destination $destCertDir -Recurse -Force
        Write-Info "  Restaurados: certificados AFIP"
    }

    $backupMigrDir = Join-Path $backupDir "migrations"
    if (Test-Path $backupMigrDir) {
        $destMigrDir = Join-Path $InstallDir "migrations"
        New-Item -ItemType Directory -Path $destMigrDir -Force | Out-Null
        Copy-Item "$backupMigrDir\*" -Destination $destMigrDir -Recurse -Force
        Write-Info "  Restaurados: scripts de migrations"
    }

    Write-OK "Configuraciones anteriores restauradas."
    Remove-Item $backupDir -Recurse -Force -ErrorAction SilentlyContinue
}

Set-Content -Path (Join-Path $InstallDir "version.txt") -Value $version -Encoding UTF8
Write-OK "Version $version registrada."

# =============================================================================
# PASO 6 - CREAR appsettings.json SI NO EXISTE
# =============================================================================
Write-Step "6/11" "Verificando configuracion inicial..."

$appSettingsPath = Join-Path $InstallDir "appsettings.json"

if (-not (Test-Path $appSettingsPath)) {
    Write-Info "Generando appsettings.json con valores de ejemplo..."

    $appsettingsTemplate = @'
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=comercio;User Id=michael;Password=michael;TrustServerCertificate=True;"
  },
  "Comercio": {
    "Nombre": "MI COMERCIO",
    "Domicilio": "Calle 000 N 000 - Ciudad"
  },
  "Facturacion": {
    "RazonSocial": "Nombre Apellido",
    "CUIT": "00-00000000-0",
    "IngBrutos": "00-00000000-0",
    "DomicilioFiscal": "Calle 000 N 000 - Ciudad",
    "CodigoPostal": "0000",
    "InicioActividades": "2020-01-01",
    "Condicion": "Monotributo",
    "PermitirFacturaA": false,
    "PermitirFacturaB": false,
    "PermitirFacturaC": true
  },
  "Validaciones": {
    "ValidarStockDisponible": false
  },
  "CuentasCorrientes": {
    "NombresCtaCte": []
  },
  "AFIP": {
    "AmbienteActivo": "Testing",
    "Testing": {
      "CUIT": "00-00000000-0",
      "CondicionIVA": "Monotributo",
      "PuntoVenta": 1,
      "CertificadoPath": "C:\\Certificados FE\\Testing\\MiCertificadoTesting.p12",
      "CertificadoPassword": "password_del_certificado",
      "WSAAUrl": "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",
      "WSFEUrl": "https://wswhomo.afip.gov.ar/wsfev1/service.asmx",
      "Servicios": {
        "Facturacion": "wsfe"
      }
    },
    "Produccion": {
      "CUIT": "00-00000000-0",
      "CondicionIVA": "Monotributo",
      "PuntoVenta": 1,
      "CertificadoPath": "C:\\Certificados FE\\Produccion\\MiCertificado.p12",
      "CertificadoPassword": "password_del_certificado",
      "WSAAUrl": "https://wsaa.afip.gov.ar/ws/services/LoginCms",
      "WSFEUrl": "https://servicios1.afip.gov.ar/wsfev1/service.asmx",
      "Servicios": {
        "Facturacion": "wsfe"
      }
    }
  },
  "RestriccionesImpresion": {
    "RestringirRemitoPorPago": false,
    "UsarVistaPrevia": true,
    "LimitarFacturacion": false,
    "MontoLimiteFacturacion": 0.00
  },
  "Descuentos": {
    "OpcionesDisponibles": [ 5, 10, 15, 20 ],
    "PorcentajeMaximo": 20,
    "RestringirPorMetodoPago": false,
    "MetodosPagoPermitidos": [ "Efectivo" ]
  },
  "BaseDatos": {
    "AmbienteActivo": "Testing"
  }
}
'@

    Set-Content -Path $appSettingsPath -Value $appsettingsTemplate -Encoding UTF8
    Write-OK "appsettings.json creado con valores de ejemplo."
    Write-Warn "IMPORTANTE: Edite $appSettingsPath antes de usar la aplicacion."
    Write-Warn "  - Cadena de conexion SQL Server"
    Write-Warn "  - Datos del comercio (nombre, domicilio)"
    Write-Warn "  - CUIT y certificados AFIP"
} else {
    Write-OK "appsettings.json existente conservado (no sobreescrito)."
}

$certFolder = Join-Path $InstallDir "Certificados FE"
if (-not (Test-Path $certFolder)) {
    New-Item -ItemType Directory -Path $certFolder -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $certFolder "Testing") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $certFolder "Produccion") -Force | Out-Null
    Write-OK "Carpeta 'Certificados FE' creada."
}

$migrFolder = Join-Path $InstallDir "migrations"
if (-not (Test-Path $migrFolder)) {
    New-Item -ItemType Directory -Path $migrFolder -Force | Out-Null
    Write-OK "Carpeta 'migrations' creada."
}

# =============================================================================
# PASO 7 - INICIALIZAR BASE DE DATOS
# =============================================================================
Write-Step "7/11" "Inicializando base de datos SQL Server..."

$sqlcmdPath = $null
$sqlcmdCandidates = @(
    "sqlcmd",
    "${env:ProgramFiles}\Microsoft SQL Server\160\Tools\Binn\sqlcmd.exe",
    "${env:ProgramFiles}\Microsoft SQL Server\150\Tools\Binn\sqlcmd.exe",
    "${env:ProgramFiles}\Microsoft SQL Server\140\Tools\Binn\sqlcmd.exe",
    "${env:ProgramFiles}\Microsoft SQL Server\130\Tools\Binn\sqlcmd.exe",
    "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
    "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\160\Tools\Binn\sqlcmd.exe",
    "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\130\Tools\Binn\sqlcmd.exe",
    "${env:ProgramFiles(x86)}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
    "${env:ProgramFiles(x86)}\Microsoft SQL Server\Client SDK\ODBC\160\Tools\Binn\sqlcmd.exe",
    "${env:LOCALAPPDATA}\Microsoft\go-sqlcmd\sqlcmd.exe"
)

foreach ($candidate in $sqlcmdCandidates) {
    try {
        $result = & $candidate -? 2>$null
        if ($LASTEXITCODE -eq 0 -or $result) {
            $sqlcmdPath = $candidate
            break
        }
    } catch { }
}

$dbScriptDest = Join-Path $InstallDir $DB_INIT_SCRIPT

$dbScriptSrc = Join-Path $sourceDir $DB_INIT_SCRIPT
if (-not (Test-Path (Join-Path $InstallDir "database"))) {
    New-Item -ItemType Directory -Path (Join-Path $InstallDir "database") -Force | Out-Null
}
if (Test-Path $dbScriptSrc) {
    Copy-Item $dbScriptSrc -Destination $dbScriptDest -Force
}

if (-not $sqlcmdPath) {
    Write-Warn "sqlcmd no encontrado en este equipo."
    Write-Warn "La base de datos NO se inicializara automaticamente."
    Write-Info "Para crearla manualmente, ejecute en SSMS o sqlcmd:"
    Write-Info "  $dbScriptDest"
    Write-Info "O instale las SQL Server Command Line Utilities desde:"
    Write-Info "  https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-utility"
} elseif (-not (Test-Path $dbScriptDest)) {
    Write-Warn "Script SQL no encontrado en: $dbScriptDest"
    Write-Warn "Asegurese de que el .zip del release incluya la carpeta database\"
} else {
    $sqlServer = if ($script:SqlServerConn) { $script:SqlServerConn } else { "localhost" }
    Write-Info "sqlcmd encontrado  : $sqlcmdPath"
    Write-Info "Servidor SQL target: $sqlServer"

    # -------------------------------------------------------------------------
    # HABILITAR SQL SERVER AUTHENTICATION (modo mixto) para conexiones remotas
    # -------------------------------------------------------------------------
    Write-Info "Habilitando autenticacion SQL Server (modo mixto)..."
    try {
        $enableMixedMode = @"
USE [master]
GO
-- Habilitar modo mixto (Windows + SQL Authentication)
EXEC xp_instance_regwrite N'HKEY_LOCAL_MACHINE',
    N'Software\Microsoft\MSSQLServer\MSSQLServer',
    N'LoginMode', REG_DWORD, 2
GO
"@
        $tmpMixedScript = Join-Path $env:TEMP "enable_mixed_mode.sql"
        Set-Content -Path $tmpMixedScript -Value $enableMixedMode -Encoding UTF8

        & $sqlcmdPath -S $sqlServer -E -i $tmpMixedScript -b -l 30 2>&1 | Out-Null
        Remove-Item $tmpMixedScript -Force -ErrorAction SilentlyContinue

        # Reiniciar el servicio SQL para que el cambio de modo tome efecto
        $sqlSvcName = "MSSQL`$$SQL_INSTANCE_NAME"
        $svcCheck = Get-Service -Name $sqlSvcName -ErrorAction SilentlyContinue
        if ($svcCheck) {
            Write-Info "Reiniciando servicio SQL Server para aplicar modo mixto..."
            Restart-Service -Name $sqlSvcName -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 8
            # Esperar a que vuelva a estar en Running
            $intentos = 0
            do {
                Start-Sleep -Seconds 3
                $intentos++
                $svcCheck = Get-Service -Name $sqlSvcName -ErrorAction SilentlyContinue
            } while (($svcCheck -eq $null -or $svcCheck.Status -ne 'Running') -and ($intentos -lt 15))

            if ($svcCheck -and $svcCheck.Status -eq 'Running') {
                Write-OK "Servicio SQL Server reiniciado. Modo mixto habilitado."
            } else {
                Write-Warn "El servicio no respondio tras el reinicio. Verifique manualmente."
            }
        } else {
            Write-Warn "No se encontro el servicio $sqlSvcName para reiniciar."
        }
    } catch {
        Write-Warn "No se pudo habilitar el modo mixto automaticamente: $_"
        Write-Warn "Habilitelo manualmente en SSMS: propiedades del servidor > Seguridad > Modo mixto."
    }

    # -------------------------------------------------------------------------
    # ABRIR PUERTO 1433 EN EL FIREWALL DE WINDOWS
    # -------------------------------------------------------------------------
    Write-Info "Configurando firewall de Windows para SQL Server (puerto 1433)..."
    try {
        $existingRule = netsh advfirewall firewall show rule name="SQL Server 1433" 2>$null
        if ($existingRule -notmatch 'SQL Server 1433') {
            netsh advfirewall firewall add rule name="SQL Server 1433" protocol=TCP dir=in localport=1433 action=allow | Out-Null
            Write-OK "Regla de firewall creada: TCP 1433 entrada permitida."
        } else {
            Write-OK "Regla de firewall para puerto 1433 ya existe."
        }
    } catch {
        Write-Warn "No se pudo configurar el firewall automaticamente: $_"
        Write-Warn "Abra manualmente el puerto 1433 TCP entrante en el Firewall de Windows."
    }

    # -------------------------------------------------------------------------
    # HABILITAR Y ARRANCAR SQL SERVER BROWSER
    # -------------------------------------------------------------------------
    Write-Info "Habilitando SQL Server Browser (necesario para conexiones remotas)..."
    try {
        $browserSvc = Get-Service -Name 'SQLBrowser' -ErrorAction SilentlyContinue
        if ($browserSvc) {
            Set-Service -Name 'SQLBrowser' -StartupType Automatic -ErrorAction SilentlyContinue
            if ($browserSvc.Status -ne 'Running') {
                Start-Service -Name 'SQLBrowser' -ErrorAction SilentlyContinue
                Start-Sleep -Seconds 3
            }
            $browserSvc = Get-Service -Name 'SQLBrowser' -ErrorAction SilentlyContinue
            if ($browserSvc.Status -eq 'Running') {
                Write-OK "SQL Server Browser habilitado y corriendo."
            } else {
                Write-Warn "SQL Server Browser no pudo iniciarse. Las conexiones remotas pueden requerir el puerto 1433 explicito."
            }
        } else {
            Write-Warn "Servicio SQLBrowser no encontrado. Las conexiones remotas usaran IP,1433 directamente."
        }
    } catch {
        Write-Warn "No se pudo configurar SQL Server Browser: $_"
    }

    # -------------------------------------------------------------------------
    # CREAR LOGIN SQL michael/michael (Windows Auth, siempre funciona)
    # Se hace ANTES del script principal para garantizar que existe
    # independientemente del estado del modo mixto durante el script
    # -------------------------------------------------------------------------
    Write-Info "Creando usuario SQL michael para conexiones remotas..."
    try {
        $createMichaelScript = @"
USE [master]
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'michael')
BEGIN
    CREATE LOGIN [michael] WITH PASSWORD = 'michael',
        DEFAULT_DATABASE = [comercio],
        CHECK_EXPIRATION = OFF,
        CHECK_POLICY = OFF
    PRINT 'Login michael creado.'
END
ELSE
    PRINT 'Login michael ya existe.'
GO
USE [comercio]
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'michael')
BEGIN
    CREATE USER [michael] FOR LOGIN [michael]
    PRINT 'Usuario michael creado en base comercio.'
END
ELSE
    PRINT 'Usuario michael ya existe en base comercio.'
GO
IF IS_ROLEMEMBER('db_owner', 'michael') = 0
BEGIN
    ALTER ROLE [db_owner] ADD MEMBER [michael]
    PRINT 'Rol db_owner asignado a michael.'
END
ELSE
    PRINT 'michael ya tiene rol db_owner.'
GO
"@
        $tmpMichaelScript = Join-Path $env:TEMP "create_michael.sql"
        Set-Content -Path $tmpMichaelScript -Value $createMichaelScript -Encoding UTF8

        $michaelOutput = & $sqlcmdPath -S $sqlServer -E -i $tmpMichaelScript -b -l 30 2>&1
        Remove-Item $tmpMichaelScript -Force -ErrorAction SilentlyContinue

        if ($LASTEXITCODE -eq 0) {
            Write-OK "Usuario SQL michael creado/verificado correctamente."
        } else {
            Write-Warn "Advertencia al crear usuario michael (codigo $LASTEXITCODE)."
            Write-Warn "Puede que la base 'comercio' aun no exista - se creara en el siguiente paso."
        }
    } catch {
        Write-Warn "No se pudo crear el usuario michael en este paso: $_"
        Write-Info "Se intentara nuevamente al final del script de inicializacion."
    }

    # -------------------------------------------------------------------------
    # EJECUTAR SCRIPT DE INICIALIZACION DE BASE DE DATOS
    # -------------------------------------------------------------------------
    Write-Info "Ejecutando script de inicializacion de base de datos..."

    $sqlLogPath = Join-Path $InstallDir "database\init_log.txt"

    try {
        $sqlOutput = & $sqlcmdPath `
            -S $sqlServer `
            -E `
            -i $dbScriptDest `
            -b `
            -l 60 `
            2>&1

        $sqlOutput | Out-File -FilePath $sqlLogPath -Encoding UTF8

        if ($LASTEXITCODE -eq 0) {
            Write-OK "Base de datos '$DB_NAME' inicializada correctamente."
            Write-Info "  Usuario app creado: admin / password: 1506"
            Write-Info "  Usuario SQL remoto: michael / password: michael"
            Write-Warn "  Cambie las contrasenas desde la aplicacion."

            # Obtener la IP de red local real (192.168.x.x) para conexiones remotas
            $localIp = (Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
                Where-Object {
                    $_.InterfaceAlias -notlike '*Loopback*' -and
                    $_.IPAddress -notlike '169.254.*' -and
                    $_.IPAddress -notlike '127.*'
                } |
                Sort-Object { [System.Net.IPAddress]::Parse($_.IPAddress).GetAddressBytes()[0] } -Descending |
                Select-Object -First 1).IPAddress

            if (-not $localIp) { $localIp = 'localhost' }
            Write-Info "  IP de red detectada: $localIp"

            # Cadena de conexion con IP real para que funcione tanto local como remoto
            $connString = "Server=$localIp,1433;Database=$DB_NAME;User Id=michael;Password=michael;TrustServerCertificate=True;"
            if (Test-Path $appSettingsPath) {
                try {
                    $json = Get-Content $appSettingsPath -Raw -Encoding UTF8
                    $json = $json -replace '(?<="DefaultConnection"\s*:\s*")[^"]*(?=")', $connString
                    Set-Content -Path $appSettingsPath -Value $json -Encoding UTF8
                    Write-OK "appsettings.json actualizado con IP de red ($localIp)."
                    Write-Info "  Cadena: $connString"
                    Write-Info "  Usar esta misma cadena en las PCs clientes de la red."
                } catch {
                    Write-Warn "No se pudo actualizar appsettings.json: $_"
                    Write-Info "Connection string: $connString"
                }
            }

            # -----------------------------------------------------------------
            # SEGUNDA PASADA: garantizar que michael existe ahora que la BD existe
            # -----------------------------------------------------------------
            Write-Info "Verificando usuario SQL michael (segunda pasada)..."
            try {
                $verifyMichael = @"
USE [master]
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'michael')
BEGIN
    CREATE LOGIN [michael] WITH PASSWORD = 'michael',
        DEFAULT_DATABASE = [comercio],
        CHECK_EXPIRATION = OFF,
        CHECK_POLICY = OFF
    PRINT 'Login michael creado (segunda pasada).'
END
GO
USE [comercio]
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'michael')
BEGIN
    CREATE USER [michael] FOR LOGIN [michael]
    PRINT 'Usuario michael creado en comercio (segunda pasada).'
END
GO
IF IS_ROLEMEMBER('db_owner', 'michael') = 0
BEGIN
    ALTER ROLE [db_owner] ADD MEMBER [michael]
    PRINT 'Rol db_owner asignado a michael (segunda pasada).'
END
GO
"@
                $tmpVerify = Join-Path $env:TEMP "verify_michael.sql"
                Set-Content -Path $tmpVerify -Value $verifyMichael -Encoding UTF8
                & $sqlcmdPath -S $sqlServer -E -i $tmpVerify -b -l 30 2>&1 | Out-Null
                Remove-Item $tmpVerify -Force -ErrorAction SilentlyContinue
                Write-OK "Usuario SQL michael verificado."
            } catch {
                Write-Warn "No se pudo verificar usuario michael: $_"
            }
        } else {
            Write-Warn "sqlcmd finalizo con codigo $LASTEXITCODE."
            Write-Warn "Puede que la BD ya existiera (normal en reinstalacion) o haya un error."
            Write-Info "Revise el log en: $sqlLogPath"
        }
    } catch {
        Write-Fail "Error ejecutando sqlcmd: $_"
        Write-Info "Ejecute manualmente el script: $dbScriptDest"
    }
}

# =============================================================================
# PASO 8 - IMPORTAR PRODUCTOS DESDE CSV (OPCIONAL)
# =============================================================================
Write-Step "8/11" "Importando productos desde CSV (si existe)..."

# El instalador busca un archivo llamado 'productos_export.csv' en:
#   1. La misma carpeta donde se ejecuta el script
#   2. El escritorio del usuario
#   3. La carpeta de instalacion destino
$csvCandidates = @(
    $(if ($PSScriptRoot) { Join-Path $PSScriptRoot "productos_export.csv" } else { $null }),
    (Join-Path ([Environment]::GetFolderPath("Desktop")) "productos_export.csv"),
    (Join-Path $InstallDir            "productos_export.csv")
) | Where-Object { $_ }
$csvProductos = $csvCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $csvProductos) {
    Write-Info "No se encontro 'productos_export.csv'. Se omite la importacion."
    Write-Info "Para importar productos en el futuro, coloque el archivo CSV en:"
    Write-Info "  $($csvCandidates[0])"
    Write-Info "y vuelva a ejecutar el instalador, o use la funcion de importacion"
    Write-Info "dentro de la aplicacion."
} elseif (-not $sqlcmdPath) {
    Write-Warn "sqlcmd no disponible. No se puede importar el CSV automaticamente."
    Write-Info "Copie '$csvProductos' en $InstallDir y use la importacion desde la app."
} else {
    Write-Info "Archivo encontrado: $csvProductos"

    try {
        # Leer el CSV - acepta separador ; o ,
        $rawContent = Get-Content $csvProductos -Encoding UTF8 -Raw
        $separator  = if ($rawContent -match ';') { ';' } else { ',' }
        $csvData    = Import-Csv -Path $csvProductos -Delimiter $separator -Encoding UTF8

        if ($csvData.Count -eq 0) {
            Write-Warn "El archivo CSV esta vacio. No hay productos para importar."
        } else {
            Write-Info "Filas encontradas en el CSV: $($csvData.Count)"

            # Columnas que acepta la tabla productos (sin ID que es IDENTITY)
            $colsTabla = @(
                'codigo','descripcion','rubro','marca','precio','costo',
                'porcentaje','cantidad','nini','vital','maxi','bultocosto',
                'bultoventa','costoanterior','paquetes','proveedor','envase',
                'modificado','contar','imprimir','StockAnterior',
                'PermiteAcumular','EditarPrecio','iva','Activo'
            )

            # Detectar que columnas del CSV coinciden con las de la tabla
            $colsCSV    = $csvData[0].PSObject.Properties.Name
            $colsUsar   = $colsTabla | Where-Object { $colsCSV -contains $_ }
            $colsInsert = $colsUsar -join ', '

            if ($colsUsar.Count -eq 0) {
                Write-Warn "El CSV no tiene columnas reconocidas de la tabla 'productos'."
                Write-Warn "Columnas esperadas (al menos una): $($colsTabla -join ', ')"
            } else {
                Write-Info "Columnas que se importaran: $colsInsert"

                # Helper para formatear valores de texto como literal SQL
                $fmtStr = {
                    param([string]$v)
                    if ([string]::IsNullOrWhiteSpace($v)) { return 'NULL' }
                    return "'" + ($v.Trim() -replace "'", "''") + "'"
                }

                # Columnas numericas (no van entre comillas)
                $numCols = @('precio','costo','porcentaje','cantidad',
                             'bultocosto','bultoventa','costoanterior',
                             'paquetes','StockAnterior','iva',
                             'PermiteAcumular','EditarPrecio','Activo')

                $fmtVal = {
                    param([string]$col, [string]$raw)
                    if ($numCols -contains $col) {
                        if ([string]::IsNullOrWhiteSpace($raw)) { return 'NULL' }
                        $n = $raw.Trim() -replace ',', '.'
                        if ($n -match '^-?[0-9]+(\.[0-9]+)?$') { return $n } else { return 'NULL' }
                    }
                    return & $fmtStr $raw
                }

                # Generar script SQL con MERGE (insert o update segun codigo)
                $sqlLines = [System.Collections.Generic.List[string]]::new()
                $sqlLines.Add("USE [comercio]")
                $sqlLines.Add("GO")
                $sqlLines.Add("SET NOCOUNT ON")
                $sqlLines.Add("DECLARE @nuevos INT = 0, @actualizados INT = 0")
                $sqlLines.Add("")

                $filasSql = 0
                foreach ($row in $csvData) {
                    $codigoRaw = if ($colsCSV -contains 'codigo') { $row.codigo } else { '' }
                    if ([string]::IsNullOrWhiteSpace($codigoRaw)) { continue }
                    $codigoEsc = $codigoRaw.Trim() -replace "'", "''"

                    # Valores para el INSERT (todas las columnas usadas)
                    $insertVals = ($colsUsar | ForEach-Object {
                        $raw = if ($colsCSV -contains $_) { $row.$_ } else { '' }
                        & $fmtVal $_ $raw
                    }) -join ', '

                    # Partes del SET para el UPDATE (excluye codigo)
                    $setParts = ($colsUsar | Where-Object { $_ -ne 'codigo' } | ForEach-Object {
                        $raw = if ($colsCSV -contains $_) { $row.$_ } else { '' }
                        "$_ = $(& $fmtVal $_ $raw)"
                    }) -join ', '

                    $sqlLines.Add("MERGE [dbo].[productos] AS tgt")
                    $sqlLines.Add("USING (SELECT '$codigoEsc' AS codigo) AS src ON tgt.codigo = src.codigo")
                    $sqlLines.Add("WHEN MATCHED THEN UPDATE SET $setParts")
                    $sqlLines.Add("WHEN NOT MATCHED THEN INSERT ($colsInsert) VALUES ($insertVals);")
                    $sqlLines.Add("IF @@ROWCOUNT > 0")
                    $sqlLines.Add("BEGIN")
                    $sqlLines.Add("    IF EXISTS (SELECT 1 FROM [dbo].[productos] WHERE codigo = '$codigoEsc')")
                    $sqlLines.Add("        SET @actualizados = @actualizados + 1")
                    $sqlLines.Add("    ELSE")
                    $sqlLines.Add("        SET @nuevos = @nuevos + 1")
                    $sqlLines.Add("END")
                    $sqlLines.Add("")
                    $filasSql++
                }

                $sqlLines.Add("PRINT 'Productos nuevos     : ' + CAST(@nuevos       AS NVARCHAR(10))")
                $sqlLines.Add("PRINT 'Productos actualizados: ' + CAST(@actualizados AS NVARCHAR(10))")
                $sqlLines.Add("GO")

                $tmpImportSql = Join-Path $env:TEMP "import_productos_$(Get-Date -Format 'yyyyMMddHHmmss').sql"
                $sqlLines | Set-Content -Path $tmpImportSql -Encoding UTF8

                $sqlServer = if ($script:SqlServerConn) { $script:SqlServerConn } else { 'localhost' }
                Write-Info "Ejecutando importacion en $sqlServer ($filasSql filas)..."

                $importOutput = & $sqlcmdPath -S $sqlServer -E -i $tmpImportSql -b -l 120 2>&1
                $importOutput | ForEach-Object { Write-Info $_ }
                Remove-Item $tmpImportSql -Force -ErrorAction SilentlyContinue

                if ($LASTEXITCODE -eq 0) {
                    Write-OK "$filasSql producto(s) procesado(s) desde el CSV."
                    # Guardar copia del CSV importado como respaldo
                    $csvBackup = Join-Path $InstallDir "database\productos_importados_$(Get-Date -Format 'yyyyMMddHHmmss').csv"
                    Copy-Item $csvProductos -Destination $csvBackup -Force -ErrorAction SilentlyContinue
                    Write-Info "Copia del CSV guardada en: $csvBackup"
                } else {
                    Write-Warn "La importacion finalizo con codigo $LASTEXITCODE. Revise los mensajes anteriores."
                }
            }
        }
    } catch {
        Write-Warn "Error durante la importacion de productos: $_"
        Write-Info "Puede importar productos manualmente desde la aplicacion."
    }
}

# =============================================================================
# PASO 9 - ACCESO DIRECTO EN EL ESCRITORIO
# =============================================================================
Write-Step "9/11" "Creando acceso directo en el escritorio..."

$exePath       = Join-Path $InstallDir $APP_EXE
$shortcutPath  = Join-Path ([Environment]::GetFolderPath("CommonDesktopDirectory")) "$APP_NAME.lnk"

if (Test-Path $exePath) {
    try {
        $wshell   = New-Object -ComObject WScript.Shell
        $shortcut = $wshell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath       = $exePath
        $shortcut.WorkingDirectory = $InstallDir
        $shortcut.Description      = "$APP_NAME - Sistema de Gestion Comercial"
        $shortcut.IconLocation = "$exePath, 0"
        $shortcut.Save()
        Write-OK "Acceso directo creado en el escritorio."
    } catch {
        Write-Warn "No se pudo crear el acceso directo: $_"
        Write-Info "Puede crearlo manualmente desde: $exePath"
    }
} else {
    Write-Warn "No se encontro $APP_EXE en $InstallDir."
    Write-Warn "Verifique que el .zip del release contenga el ejecutable."
}

# =============================================================================
# PASO 10 - CONFIGURAR BACKUP AUTOMATICO DIARIO DE LA BASE DE DATOS
# =============================================================================
Write-Step "10/11" "Configurando backup automatico de la base de datos..."

$backupDir      = Join-Path $InstallDir "backups"
$backupScript   = Join-Path $InstallDir "database\backup_comercio.ps1"
$taskName       = "ComercioNET - Backup diario BD"
$backupHora     = "10:00"

# Crear carpeta de backups
if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    Write-Info "Carpeta de backups creada: $backupDir"
}

# Escribir el script de backup que se instalara en la PC del cliente
$sqlServerConnBackup = if ($script:SqlServerConn) { $script:SqlServerConn } else { "localhost\SQLEXPRESS" }

$backupScriptContent = @"
#Requires -Version 5.1
<#
.SYNOPSIS
    Backup automatico diario de la base de datos Comercio .NET
.DESCRIPTION
    Genera un archivo .bak en la carpeta de backups.
    Mantiene los ultimos 30 backups y elimina los mas antiguos.
    Se ejecuta automaticamente a las $backupHora via Tarea Programada.
    Log en: $InstallDir\database\backup_log.txt
#>

`$ErrorActionPreference = "SilentlyContinue"

`$sqlServer   = "$sqlServerConnBackup"
`$database    = "comercio"
`$backupDir   = "$backupDir"
`$logFile     = "$InstallDir\database\backup_log.txt"
`$maxBackups  = 30
`$fecha       = Get-Date -Format "yyyyMMdd_HHmmss"
`$backupFile  = Join-Path `$backupDir "comercio_`$fecha.bak"

function Write-Log {
    param([string]`$msg)
    `$line = "[`$(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')] `$msg"
    Add-Content -Path `$logFile -Value `$line -Encoding UTF8
}

Write-Log "--- Iniciando backup ---"
Write-Log "Servidor : `$sqlServer"
Write-Log "Destino  : `$backupFile"

# Buscar sqlcmd
`$sqlcmdPath = `$null
`$candidates = @(
    "sqlcmd",
    "`${env:ProgramFiles}\Microsoft SQL Server\160\Tools\Binn\sqlcmd.exe",
    "`${env:ProgramFiles}\Microsoft SQL Server\150\Tools\Binn\sqlcmd.exe",
    "`${env:ProgramFiles}\Microsoft SQL Server\140\Tools\Binn\sqlcmd.exe",
    "`${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
    "`${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\160\Tools\Binn\sqlcmd.exe"
)
foreach (`$c in `$candidates) {
    try {
        `$r = & `$c -? 2>`$null
        if (`$LASTEXITCODE -eq 0 -or `$r) { `$sqlcmdPath = `$c; break }
    } catch { }
}

if (-not `$sqlcmdPath) {
    Write-Log "ERROR: sqlcmd no encontrado. Backup cancelado."
    exit 1
}

# Script SQL de backup
`$sqlLines = @(
    "BACKUP DATABASE [comercio]",
    "TO DISK = N'`$backupFile'",
    "WITH NOFORMAT, NOINIT,",
    "     NAME = N'comercio-backup-`$fecha',",
    "     SKIP, NOREWIND, NOUNLOAD,",
    "     COMPRESSION, STATS = 10",
    "GO"
)
`$sql = `$sqlLines -join [Environment]::NewLine

`$tmpSql = Join-Path `$env:TEMP "backup_comercio_`$fecha.sql"
Set-Content -Path `$tmpSql -Value `$sql -Encoding UTF8

`$output = & `$sqlcmdPath -S `$sqlServer -E -i `$tmpSql -b -l 300 2>&1
Remove-Item `$tmpSql -Force -ErrorAction SilentlyContinue

if (`$LASTEXITCODE -eq 0) {
    `$sizeMB = [math]::Round((Get-Item `$backupFile -ErrorAction SilentlyContinue).Length / 1MB, 2)
    Write-Log "OK  Backup completado. Archivo: `$(Split-Path `$backupFile -Leaf) (`$sizeMB MB)"

    # Eliminar backups antiguos, conservar los ultimos `$maxBackups
    `$viejos = Get-ChildItem -Path `$backupDir -Filter "comercio_*.bak" |
               Sort-Object LastWriteTime -Descending |
               Select-Object -Skip `$maxBackups
    foreach (`$v in `$viejos) {
        Remove-Item `$v.FullName -Force -ErrorAction SilentlyContinue
        Write-Log "    Eliminado backup antiguo: `$(`$v.Name)"
    }
} else {
    Write-Log "ERROR: sqlcmd finalizo con codigo `$LASTEXITCODE."
    `$output | ForEach-Object { Write-Log "    `$_" }
    exit 1
}

Write-Log "--- Backup finalizado ---"
"@

# Guardar el script de backup en la carpeta de instalacion
try {
    Set-Content -Path $backupScript -Value $backupScriptContent -Encoding UTF8
    Write-OK "Script de backup creado: $backupScript"
} catch {
    Write-Warn "No se pudo crear el script de backup: $_"
}

# Registrar la tarea programada en el Programador de Tareas de Windows
if (Test-Path $backupScript) {
    try {
        # Eliminar tarea anterior si ya existe (reinstalacion)
        $existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
        if ($existingTask) {
            Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
            Write-Info "Tarea programada anterior eliminada."
        }

        # Accion: ejecutar powershell con el script de backup
        $action = New-ScheduledTaskAction `
            -Execute "powershell.exe" `
            -Argument "-NoProfile -ExecutionPolicy Bypass -NonInteractive -WindowStyle Hidden -File `"$backupScript`""

        # Disparador: todos los dias a las 10:00 AM
        $trigger = New-ScheduledTaskTrigger -Daily -At $backupHora

        # Configuracion: ejecutar aunque el usuario no este logueado, con maxima prioridad
        $settings = New-ScheduledTaskSettingsSet `
            -ExecutionTimeLimit (New-TimeSpan -Hours 1) `
            -StartWhenAvailable `
            -RunOnlyIfNetworkAvailable:$false `
            -MultipleInstances IgnoreNew

        # Principal: ejecutar como SYSTEM para que funcione sin usuario logueado
        $principal = New-ScheduledTaskPrincipal `
            -UserId "SYSTEM" `
            -LogonType ServiceAccount `
            -RunLevel Highest

        Register-ScheduledTask `
            -TaskName  $taskName `
            -Action    $action `
            -Trigger   $trigger `
            -Settings  $settings `
            -Principal $principal `
            -Description "Backup diario automatico de la base de datos Comercio .NET. Genera archivos .bak en $backupDir" `
            -Force | Out-Null

        Write-OK "Tarea programada registrada correctamente."
        Write-Info "  Nombre  : $taskName"
        Write-Info "  Horario : todos los dias a las $backupHora"
        Write-Info "  Backups : $backupDir"
        Write-Info "  Log     : $InstallDir\database\backup_log.txt"
        Write-Info "  Retiene : ultimos 30 backups (~30 dias)"
    } catch {
        Write-Warn "No se pudo registrar la tarea programada: $_"
        Write-Info "Puede configurarla manualmente en el Programador de Tareas de Windows:"
        Write-Info "  Programa : powershell.exe"
        Write-Info "  Argumentos: -NoProfile -ExecutionPolicy Bypass -File `"$backupScript`""
        Write-Info "  Horario  : diario a las $backupHora"
    }
} else {
    Write-Warn "El script de backup no se pudo crear. Se omite la tarea programada."
}

# =============================================================================
# PASO 11 - INSTALAR SQL SERVER MANAGEMENT STUDIO (SSMS) - OPCIONAL
# =============================================================================
Write-Step "11/11" "SQL Server Management Studio (SSMS)..."

# Detectar si SSMS ya esta instalado buscando en el registro
$ssmsInstalled = $false
$ssmsRegPaths = @(
    "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
)
foreach ($regBase in $ssmsRegPaths) {
    if (Test-Path $regBase) {
        $found = Get-ChildItem -Path $regBase -ErrorAction SilentlyContinue |
            Get-ItemProperty -ErrorAction SilentlyContinue |
            Where-Object { $_ -ne $null -and $_.PSObject.Properties['DisplayName'] -ne $null -and $_.DisplayName -like "*SQL Server Management Studio*" } |
            Select-Object -First 1
        if ($found) {
            $ssmsInstalled = $true
            Write-OK "SSMS ya instalado: $($found.DisplayName) v$($found.DisplayVersion)"
            break
        }
    }
}

if (-not $ssmsInstalled) {
    Write-Info "SSMS no encontrado en este equipo."
    Write-Host ""
    Write-Host "  SQL Server Management Studio (SSMS) es una herramienta gratuita" -ForegroundColor White
    Write-Host "  de Microsoft para administrar la base de datos visualmente:" -ForegroundColor White
    Write-Host "    - Ver y editar tablas, datos y estructuras" -ForegroundColor Gray
    Write-Host "    - Ejecutar consultas SQL" -ForegroundColor Gray
    Write-Host "    - Hacer backups y restauraciones" -ForegroundColor Gray
    Write-Host "    - Gestionar usuarios y permisos" -ForegroundColor Gray
    Write-Host "  Tamanio de descarga: ~600 MB. Instalacion: ~5-10 minutos." -ForegroundColor Gray
    Write-Host ""

    $instalarSsms = Read-Host "  Desea instalar SSMS ahora? (S/N)"

    if ($instalarSsms -match "^[sS]") {
        $tempSsms = Join-Path $env:TEMP $SSMS_FILENAME
        Remove-Item $tempSsms -Force -ErrorAction SilentlyContinue

        Write-Info "Descargando SSMS (~600 MB, puede tardar varios minutos)..."
        try {
            $wcSsms = New-Object System.Net.WebClient
            $wcSsms.DownloadFile($SSMS_URL, $tempSsms)
            Write-OK "Descarga de SSMS completada."
        } catch {
            Write-Fail "Error descargando SSMS: $_"
            Write-Warn "Descargue manualmente desde: https://aka.ms/ssms"
            $tempSsms = $null
        }

        if ($tempSsms -and (Test-Path $tempSsms)) {
            Write-Info "Instalando SSMS en modo silencioso (puede tardar 5-10 minutos)..."
            try {
                $procSsms = Start-Process -FilePath $tempSsms `
                    -ArgumentList "/install /quiet /norestart" `
                    -Wait -PassThru

                switch ($procSsms.ExitCode) {
                    0    { Write-OK "SSMS instalado correctamente." }
                    3010 {
                        Write-OK "SSMS instalado correctamente."
                        Write-Warn "Se requiere reiniciar el equipo para completar la instalacion de SSMS."
                    }
                    default {
                        Write-Warn "Instalador SSMS finalizo con codigo $($procSsms.ExitCode)."
                        Write-Info "Si SSMS no aparece, descargue manualmente desde: https://aka.ms/ssms"
                    }
                }
            } catch {
                Write-Warn "Error al ejecutar el instalador de SSMS: $_"
            } finally {
                Remove-Item $tempSsms -Force -ErrorAction SilentlyContinue
            }

            # Crear acceso directo a SSMS en el escritorio si el exe existe
            $ssmsPaths = @(
                "${env:ProgramFiles(x86)}\Microsoft SQL Server Management Studio 20\Common7\IDE\Ssms.exe",
                "${env:ProgramFiles}\Microsoft SQL Server Management Studio 20\Common7\IDE\Ssms.exe",
                "${env:ProgramFiles(x86)}\Microsoft SQL Server Management Studio 19\Common7\IDE\Ssms.exe",
                "${env:ProgramFiles}\Microsoft SQL Server Management Studio 19\Common7\IDE\Ssms.exe"
            )
            $ssmsExe = $ssmsPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
            if ($ssmsExe) {
                try {
                    $ssmsShortcut = Join-Path ([Environment]::GetFolderPath("CommonDesktopDirectory")) "SQL Server Management Studio.lnk"
                    $wshellSsms   = New-Object -ComObject WScript.Shell
                    $scSsms       = $wshellSsms.CreateShortcut($ssmsShortcut)
                    $scSsms.TargetPath  = $ssmsExe
                    $scSsms.Description = "SQL Server Management Studio"
                    $scSsms.IconLocation = "$ssmsExe, 0"
                    $scSsms.Save()
                    Write-OK "Acceso directo a SSMS creado en el escritorio."
                } catch {
                    Write-Warn "No se pudo crear el acceso directo de SSMS: $_"
                }
            }
        }
    } else {
        Write-Info "Instalacion de SSMS omitida."
        Write-Info "Puede instalarlo en cualquier momento desde: https://aka.ms/ssms"
    }
} else {
    Write-OK "SSMS listo."
}

# =============================================================================
# LIMPIEZA
# =============================================================================
Remove-Item $tempZip     -Force -ErrorAction SilentlyContinue
Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue

# =============================================================================
# RESUMEN FINAL
# =============================================================================
Write-Header "INSTALACION COMPLETADA"

Write-Host "  Version instalada : " -NoNewline
Write-Host $version -ForegroundColor Green

Write-Host "  Carpeta           : " -NoNewline
Write-Host $InstallDir -ForegroundColor Cyan

Write-Host "  Ejecutable        : " -NoNewline
Write-Host (Join-Path $InstallDir $APP_EXE) -ForegroundColor Cyan

Write-Host ""
if (-not $script:SqlInstallOk) {
    Write-Host ""
    Write-Host ("=" * 60) -ForegroundColor Red
    Write-Host "  ATENCION: SQL SERVER NO FUE INSTALADO" -ForegroundColor Red
    Write-Host ("=" * 60) -ForegroundColor Red
    Write-Host "  La aplicacion NO funcionara hasta que SQL Server este" -ForegroundColor Red
    Write-Host "  instalado y la base de datos 'comercio' sea creada." -ForegroundColor Red
    Write-Host ""
    Write-Host "  PASOS PARA RESOLVER MANUALMENTE:" -ForegroundColor Yellow
    Write-Host "  1. Descargue SQL Server 2022 Express desde:" -ForegroundColor White
    Write-Host "     https://www.microsoft.com/sql-server/sql-server-downloads" -ForegroundColor Cyan
    Write-Host "  2. Instale SQL Server con la instancia 'SQLEXPRESS'" -ForegroundColor White
    Write-Host "  3. Ejecute el script de base de datos:" -ForegroundColor White
    Write-Host "     sqlcmd -S localhost\SQLEXPRESS -E -i `"$(Join-Path $InstallDir $DB_INIT_SCRIPT)`"" -ForegroundColor Cyan
    Write-Host ("=" * 60) -ForegroundColor Red
    Write-Host ""
}

Write-Host "  PASOS OBLIGATORIOS ANTES DE USAR:" -ForegroundColor Yellow
    Write-Host "  ====================================================" -ForegroundColor Yellow
Write-Host "  1. Editar appsettings.json con los datos del cliente:" -ForegroundColor White
Write-Host "     $appSettingsPath" -ForegroundColor Gray
Write-Host "     - ConnectionStrings: cadena de conexion SQL Server" -ForegroundColor Gray
Write-Host "     - Comercio.Nombre y Comercio.Domicilio" -ForegroundColor Gray
Write-Host "     - Facturacion: CUIT, Razon Social, Condicion" -ForegroundColor Gray
Write-Host "     - AFIP: CUIT, PuntoVenta, rutas de certificados" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Copiar certificados AFIP (.p12 / .pfx) en:" -ForegroundColor White
Write-Host "     $certFolder" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Verificar que SQL Server este corriendo" -ForegroundColor White
Write-Host "     Si la BD no se creo, ejecute manualmente:" -ForegroundColor Gray
Write-Host "     $(Join-Path $InstallDir $DB_INIT_SCRIPT)" -ForegroundColor Gray
Write-Host ""
Write-Host "  BACKUP AUTOMATICO DIARIO:" -ForegroundColor Yellow
Write-Host "     Horario : todos los dias a las 10:00 AM" -ForegroundColor Gray
Write-Host "     Carpeta : $backupDir" -ForegroundColor Gray
Write-Host "     Log     : $InstallDir\database\backup_log.txt" -ForegroundColor Gray
Write-Host "     Tarea   : '$taskName'" -ForegroundColor Gray
Write-Host ""
Write-Host "  USUARIO SQL PARA CONEXIONES REMOTAS:" -ForegroundColor Yellow
Write-Host "     Usuario: michael  |  Password: michael" -ForegroundColor Gray
Write-Host "     (Para acceso desde otras PCs en la red local)" -ForegroundColor Gray
Write-Host ""
Write-Host "  4. Ejecutar la aplicacion con el acceso directo" -ForegroundColor White
Write-Host "     del escritorio o desde:" -ForegroundColor Gray
Write-Host "     $exePath" -ForegroundColor Gray
Write-Host ""
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host ""

$respuesta = Read-Host "Desea abrir la carpeta de instalacion ahora? (S/N)"
if ($respuesta -match "^[sS]") {
    Start-Process explorer.exe -ArgumentList $InstallDir
}

# Diagnostico final de base de datos
Test-DatabaseDiagnostic -AppSettingsPath $appSettingsPath -InstallDir $InstallDir -DbInitScript (Join-Path $InstallDir $DB_INIT_SCRIPT)
