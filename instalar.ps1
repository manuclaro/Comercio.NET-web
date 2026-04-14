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
$DOTNET_RUNTIME_URL = "https://download.visualstudio.microsoft.com/download/pr/b6f19ef3-52d7-4b4b-98a7-84e9cdc82e8c/f4d27595d2b7c798d5eca2f0547f3d16/windowsdesktop-runtime-8.0.12-win-x64.exe"
$DOTNET_RUNTIME_FILENAME = "windowsdesktop-runtime-8.0-win-x64.exe"
$DB_INIT_SCRIPT    = "database\init_comercio.sql"
$DB_NAME           = "comercio"
# SQL Server Express 2022 - instalador web SSEI (~6 MB, descarga el medio offline)
$SQLEXPRESS_URL      = "https://go.microsoft.com/fwlink/p/?linkid=2216019&clcid=0x0409&culture=en-us&country=us"
$SQLEXPRESS_FILENAME = "SQL2022-SSEI-Expr.exe"
$SQL_INSTANCE_NAME   = "SQLEXPRESS"

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
Write-Step "1/8" "Verificando .NET $DOTNET_VERSION Runtime..."

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
        $wc = New-Object System.Net.WebClient
        $wc.DownloadFile($DOTNET_RUNTIME_URL, $tempRuntime)
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
Write-Step "2/8" "Verificando SQL Server..."

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

if ($sqlInstalled) {
    Write-OK "SQL Server encontrado. Instancia: $sqlInstanceName"
    $script:SqlServerConn = if ($sqlInstanceName -eq "MSSQLSERVER") {
        "localhost"
    } else {
        "localhost\$sqlInstanceName"
    }
} else {
    Write-Info "SQL Server no detectado. Descargando SQL Server 2022 Express..."
    Write-Info "(Instalador web SSEI ~6 MB + descarga medio offline ~280 MB)"
    Write-Warn "Este proceso puede tardar varios minutos segun la conexion."
    Write-Host ""

    $tempSsei = Join-Path $env:TEMP $SQLEXPRESS_FILENAME
    $tempSqlMedia = Join-Path $env:TEMP "SqlExpressMedia"
    Remove-Item $tempSsei -Force -ErrorAction SilentlyContinue
    Remove-Item $tempSqlMedia -Recurse -Force -ErrorAction SilentlyContinue

    try {
        $wc2 = New-Object System.Net.WebClient
        $wc2.DownloadFile($SQLEXPRESS_URL, $tempSsei)
        Write-OK "Instalador SSEI descargado (~6 MB)."
    } catch {
        Write-Fail "Error descargando SQL Server Express: $_"
        Write-Warn "Instale SQL Server Express manualmente desde:"
        Write-Warn "https://www.microsoft.com/sql-server/sql-server-downloads"
        Read-Host "Presione ENTER para continuar o Ctrl+C para cancelar"
    }

    if (Test-Path $tempSsei) {
        # Paso A: Usar SSEI para descargar el medio offline completo
        Write-Info "Descargando medio de instalacion offline (~280 MB, puede tardar)..."
        New-Item -ItemType Directory -Path $tempSqlMedia -Force | Out-Null

        $downloadArgs = "/Action=Download /MediaPath=`"$tempSqlMedia`" /MediaType=Core /Quiet"
        $procDownload = Start-Process -FilePath $tempSsei -ArgumentList $downloadArgs -Wait -PassThru

        Remove-Item $tempSsei -Force -ErrorAction SilentlyContinue

        if ($procDownload.ExitCode -ne 0) {
            Write-Warn "SSEI finalizo descarga con codigo $($procDownload.ExitCode)."
        }

        # Buscar el SQLEXPR_x64_ENU.exe descargado por SSEI
        $setupExe = $null
        $mediaExe = Get-ChildItem -Path $tempSqlMedia -Filter "SQLEXPR*" -File -ErrorAction SilentlyContinue | Select-Object -First 1

        if ($mediaExe) {
            # Es un exe autoextraible - extraerlo con timeout de 10 minutos
            Write-Info "Extrayendo medio de instalacion (puede tardar varios minutos)..."
            $extractDir = Join-Path $env:TEMP "SqlExpressExtracted"
            Remove-Item $extractDir -Recurse -Force -ErrorAction SilentlyContinue
            New-Item -ItemType Directory -Path $extractDir -Force | Out-Null

            $procExtract = Start-Process -FilePath $mediaExe.FullName -ArgumentList "/Q /x:`"$extractDir`"" -PassThru
            $timeoutMs = 10 * 60 * 1000  # 10 minutos
            $finished = $procExtract.WaitForExit($timeoutMs)

            if (-not $finished) {
                Write-Warn "Extraccion supero 10 minutos. Terminando proceso..."
                $procExtract.Kill()
            }

            # Buscar setup.exe en el directorio extraido
            $found = Get-ChildItem -Path $extractDir -Filter "setup.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($found) { $setupExe = $found.FullName }
        }

        if (-not $setupExe -or -not (Test-Path $setupExe)) {
            # Buscar setup.exe directamente en la carpeta de media (algunos builds lo dejan ahi)
            $found = Get-ChildItem -Path $tempSqlMedia -Filter "setup.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($found) { $setupExe = $found.FullName }
        }

        if ($setupExe -and (Test-Path $setupExe)) {
            # Paso B: Ejecutar setup.exe en modo silencioso
            Write-Info "Instalando SQL Server 2022 Express en modo silencioso..."
            Write-Info "(Esto puede tardar entre 5 y 15 minutos, por favor espere)"

            $sqlArgs = '/Q /ACTION=Install /FEATURES=SQLEngine ' +
                       '/INSTANCENAME=SQLEXPRESS ' +
                       '/SQLSVCACCOUNT="NT AUTHORITY\NETWORK SERVICE" ' +
                       '/SQLSYSADMINACCOUNTS="BUILTIN\Administrators" ' +
                       '/TCPENABLED=1 /NPENABLED=0 ' +
                       '/IACCEPTSQLSERVERLICENSETERMS'

            $procSql = Start-Process -FilePath $setupExe -ArgumentList $sqlArgs -PassThru
            Write-Info "Esperando que finalice la instalacion (timeout: 30 min)..."
            $sqlFinished = $procSql.WaitForExit(30 * 60 * 1000)
            if (-not $sqlFinished) {
                Write-Warn "La instalacion de SQL Server supero 30 minutos."
                Write-Warn "El proceso continua en segundo plano. Verifique el servicio MSSQL`$SQLEXPRESS manualmente."
                $procSql.Kill()
            }

            if ($sqlFinished -and ($procSql.ExitCode -eq 0 -or $procSql.ExitCode -eq 3010)) {
                Write-OK "SQL Server 2022 Express instalado correctamente."
                if ($procSql.ExitCode -eq 3010) {
                    Write-Warn "Se recomienda reiniciar el equipo despues de la instalacion."
                }
                $script:SqlServerConn = "localhost\$SQL_INSTANCE_NAME"

                # Esperar a que el servicio SQL Server este disponible (hasta 60 segundos)
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

                # Agregar la ruta del motor al PATH de esta sesion para que el paso 7 encuentre sqlcmd
                $sqlToolsPath = "${env:ProgramFiles}\Microsoft SQL Server\160\Tools\Binn"
                if (Test-Path $sqlToolsPath) {
                    $env:PATH = "$sqlToolsPath;$env:PATH"
                    Write-Info "Ruta de sqlcmd agregada al PATH de la sesion."
                }
            } else {
                if ($sqlFinished) {
                    Write-Warn "setup.exe finalizo con codigo $($procSql.ExitCode)."
                }
                Write-Warn "Consulte el log en: C:\Program Files\Microsoft SQL Server\*\Setup Bootstrap\Log"
                $script:SqlServerConn = "localhost\$SQL_INSTANCE_NAME"
            }
        } else {
            Write-Warn "No se encontro setup.exe en el medio descargado."
            Write-Warn "Instale SQL Server Express manualmente desde:"
            Write-Warn "https://www.microsoft.com/sql-server/sql-server-downloads"
            $script:SqlServerConn = "localhost\$SQL_INSTANCE_NAME"
        }

        # Limpieza de archivos temporales de SQL
        Remove-Item $tempSqlMedia -Recurse -Force -ErrorAction SilentlyContinue
        $extractDir2 = Join-Path $env:TEMP "SqlExpressExtracted"
        Remove-Item $extractDir2 -Recurse -Force -ErrorAction SilentlyContinue
    } else {
        Write-Warn "No se pudo descargar SQL Server Express. Continuando sin el."
        $script:SqlServerConn = "localhost\$SQL_INSTANCE_NAME"
    }
}

# =============================================================================
# PASO 3 - OBTENER ULTIMA VERSION DE GITHUB
# =============================================================================
Write-Step "3/8" "Consultando ultima version en GitHub..."

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
Write-Step "4/8" "Descargando $APP_NAME v$version..."

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
Write-Step "5/8" "Instalando en $InstallDir..."

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
Write-Step "6/8" "Verificando configuracion inicial..."

$appSettingsPath = Join-Path $InstallDir "appsettings.json"

if (-not (Test-Path $appSettingsPath)) {
    Write-Info "Generando appsettings.json con valores de ejemplo..."

    $appsettingsTemplate = @'
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=comercio;Trusted_Connection=True;TrustServerCertificate=True;"
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
Write-Step "7/8" "Inicializando base de datos SQL Server..."

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
    Write-Info "Ejecutando script de inicializacion..."

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
            Write-Info "  Usuario creado: admin / password: 1506"
            Write-Warn "  Cambie la password del administrador desde la aplicacion."

            $connString = "Server=$sqlServer;Database=$DB_NAME;Trusted_Connection=True;TrustServerCertificate=True;"
            if (Test-Path $appSettingsPath) {
                try {
                    $json = Get-Content $appSettingsPath -Raw -Encoding UTF8
                    $json = $json -replace '(?<="DefaultConnection"\s*:\s*")[^"]*(?=")', $connString
                    Set-Content -Path $appSettingsPath -Value $json -Encoding UTF8
                    Write-OK "appsettings.json actualizado con la conexion: $sqlServer"
                } catch {
                    Write-Warn "No se pudo actualizar appsettings.json: $_"
                    Write-Info "Connection string: $connString"
                }
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
# PASO 8 - ACCESO DIRECTO EN EL ESCRITORIO
# =============================================================================
Write-Step "8/8" "Creando acceso directo en el escritorio..."

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
