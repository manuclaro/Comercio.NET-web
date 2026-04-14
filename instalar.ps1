#Requires -Version 5.1
<#
.SYNOPSIS
    Instalador de Comercio .NET
    
.DESCRIPTION
    Descarga e instala Comercio .NET desde GitHub Releases.
    Instala el .NET 8 Runtime si no está presente.
    Crea la carpeta de instalación, un acceso directo en el escritorio
    y genera un appsettings.json inicial listo para configurar.

.PARAMETER InstallDir
    Carpeta de instalación. Por defecto: C:\Comercio.NET

.PARAMETER GitHubRepo
    Repositorio de GitHub en formato owner/repo.
    Por defecto: manuclaro/Comercio.NET-web

.PARAMETER GitHubToken
    Token de acceso personal para repositorios privados (opcional).
    Si el repositorio es público, no es necesario.

.EXAMPLE
# Instalación estándar (una línea desde PowerShell):
irm https://raw.githubusercontent.com/manuclaro/Comercio.NET-web/master/instalar.ps1 | iex

.EXAMPLE
    # Con carpeta personalizada:
    .\instalar.ps1 -InstallDir "D:\MiComercio"

.EXAMPLE
    # Con token para repo privado:
    .\instalar.ps1 -GitHubToken "ghp_tutoken"
#>

[CmdletBinding()]
param(
    [string]$InstallDir    = "C:\Comercio.NET",
    [string]$GitHubRepo    = "manuclaro/Comercio.NET-web",
    [string]$GitHubToken   = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ?????????????????????????????????????????????????????????????????????????????
# CONSTANTES
# ?????????????????????????????????????????????????????????????????????????????
$APP_NAME          = "Comercio .NET"
$APP_EXE           = "Comercio .NET.exe"
$DOTNET_VERSION    = "8.0"
# URL del runtime framework-dependent (sin self-contained). ~56 MB.
$DOTNET_RUNTIME_URL = "https://download.visualstudio.microsoft.com/download/pr/b6f19ef3-52d7-4b4b-98a7-84e9cdc82e8c/f4d27595d2b7c798d5eca2f0547f3d16/windowsdesktop-runtime-8.0.12-win-x64.exe"
$DOTNET_RUNTIME_FILENAME = "windowsdesktop-runtime-8.0-win-x64.exe"
# Nombre del script SQL de inicialización incluido en el .zip del release
$DB_INIT_SCRIPT    = "database\init_comercio.sql"
$DB_NAME           = "comercio"
# SQL Server Express 2022 – instalador offline completo (~290 MB, soporta /FEATURES etc.)
$SQLEXPRESS_URL      = "https://download.microsoft.com/download/5/1/4/5147f99c-f20b-4e9c-af7b-492f6c00c74c/SQLEXPR_x64_ENU.exe"
$SQLEXPRESS_FILENAME = "SQLEXPR_x64_ENU.exe"
# Nombre de instancia que usa el instalador silencioso de Express
$SQL_INSTANCE_NAME   = "SQLEXPRESS"

# ?????????????????????????????????????????????????????????????????????????????
# HELPERS DE CONSOLA
# ?????????????????????????????????????????????????????????????????????????????
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

# ?????????????????????????????????????????????????????????????????????????????
# PASO 0 – VERIFICAR PRIVILEGIOS DE ADMINISTRADOR
# ?????????????????????????????????????????????????????????????????????????????
function Test-Admin {
    $current = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($current)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Admin)) {
    Write-Warn "El instalador necesita permisos de administrador."
    Write-Warn "Reiniciando con elevación..."
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

# ?????????????????????????????????????????????????????????????????????????????
# ENCABEZADO
# ?????????????????????????????????????????????????????????????????????????????
Clear-Host
Write-Header "INSTALADOR DE $APP_NAME"
Write-Info "Repositorio : $GitHubRepo"
Write-Info "Destino     : $InstallDir"
Write-Info "Fecha/Hora  : $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')"
Write-Host ""

# ?????????????????????????????????????????????????????????????????????????????
# PASO 1 – VERIFICAR / INSTALAR .NET 8 RUNTIME
# ?????????????????????????????????????????????????????????????????????????????
Write-Step "1/8" "Verificando .NET $DOTNET_VERSION Runtime..."

$dotnetInstalled = $false
try {
    $runtimes = & dotnet --list-runtimes 2>$null
    if ($runtimes -match "Microsoft\.WindowsDesktop\.App $DOTNET_VERSION") {
        $dotnetInstalled = $true
        Write-OK ".NET $DOTNET_VERSION Desktop Runtime ya instalado."
    }
} catch {
    # dotnet.exe no encontrado en PATH, seguir adelante
}

if (-not $dotnetInstalled) {
    Write-Info ".NET $DOTNET_VERSION no encontrado. Descargando instalador (~56 MB)..."

    $tempRuntime = Join-Path $env:TEMP $DOTNET_RUNTIME_FILENAME
    try {
        $wc = New-Object System.Net.WebClient
        $wc.DownloadFile($DOTNET_RUNTIME_URL, $tempRuntime)
        Write-OK "Descarga completada."
    } catch {
        Write-Fail "Error descargando .NET runtime: $_"
        Write-Warn "Por favor instale manualmente desde:"
        Write-Warn "https://dotnet.microsoft.com/download/dotnet/8.0"
        Read-Host "Presione ENTER para continuar de todos modos o Ctrl+C para cancelar"
    }

    if (Test-Path $tempRuntime) {
        Write-Info "Instalando .NET $DOTNET_VERSION Runtime (puede tardar unos minutos)..."
        $proc = Start-Process -FilePath $tempRuntime -ArgumentList "/quiet /norestart" -Wait -PassThru
        if ($proc.ExitCode -eq 0) {
            Write-OK ".NET $DOTNET_VERSION instalado correctamente."
        } else {
            Write-Warn "El instalador de .NET finalizó con código $($proc.ExitCode)."
            Write-Warn "Puede que ya estuviera instalado o requiera reinicio."
        }
        Remove-Item $tempRuntime -Force -ErrorAction SilentlyContinue
    }
} else {
    Write-OK ".NET $DOTNET_VERSION listo."
}

# ?????????????????????????????????????????????????????????????????????????????
# PASO 2 – VERIFICAR / INSTALAR SQL SERVER EXPRESS
# ?????????????????????????????????????????????????????????????????????????????
Write-Step "2/8" "Verificando SQL Server..."

# Detectar si ya hay alguna instancia de SQL Server instalada
$sqlInstalled    = $false
$sqlInstanceName = $null   # nombre real de la instancia encontrada

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
    # Instancia por defecto (MSSQLSERVER) ? "localhost"; instancia nombrada ? "localhost\NOMBRE"
    $script:SqlServerConn = if ($sqlInstanceName -eq "MSSQLSERVER") {
        "localhost"
    } else {
        "localhost\$sqlInstanceName"
    }
} else {
    Write-Info "SQL Server no detectado. Descargando SQL Server 2022 Express..."
    Write-Info "(~290 MB, instalador offline completo - puede tardar varios minutos)"
    Write-Warn "Esta descarga puede tardar varios minutos según la conexión."
    Write-Host ""

    $tempSql = Join-Path $env:TEMP $SQLEXPRESS_FILENAME
    Remove-Item $tempSql -Force -ErrorAction SilentlyContinue

    try {
        $wc2 = New-Object System.Net.WebClient
        $wc2.DownloadFile($SQLEXPRESS_URL, $tempSql)
        Write-OK "Instalador de SQL Express descargado."
    } catch {
        Write-Fail "Error descargando SQL Server Express: $_"
        Write-Warn "Instale SQL Server Express manualmente desde:"
        Write-Warn "https://www.microsoft.com/sql-server/sql-server-downloads"
        Read-Host "Presione ENTER para continuar de todos modos o Ctrl+C para cancelar"
    }

    if (Test-Path $tempSql) {
        Write-Info "Instalando SQL Server 2022 Express en modo silencioso..."
        Write-Info "(Esto puede tardar entre 5 y 15 minutos, por favor espere)"

        # Argumentos para el instalador offline SQLEXPR_x64_ENU.exe
        # /Q                    – sin interfaz gráfica
        # /ACTION=Install       – nueva instalación
        # /FEATURES=SQLEngine   – solo el motor de base de datos
        # /INSTANCENAME         – nombre estándar de Express
        # /SQLSYSADMINACCOUNTS  – los Administradores locales son sysadmin
        # /TCPENABLED=1         – habilitar TCP/IP para conexiones locales
        # /IACCEPTSQLSERVERLICENSETERMS – requerido en modo silencioso
        $sqlArgs = '/Q /ACTION=Install /FEATURES=SQLEngine ' +
                   '/INSTANCENAME=SQLEXPRESS ' +
                   '/SQLSVCACCOUNT="NT AUTHORITY\NETWORK SERVICE" ' +
                   '/SQLSYSADMINACCOUNTS="BUILTIN\Administrators" ' +
                   '/TCPENABLED=1 /NPENABLED=0 ' +
                   '/IACCEPTSQLSERVERLICENSETERMS'

        $procSql = Start-Process -FilePath $tempSql -ArgumentList $sqlArgs -Wait -PassThru
        Remove-Item $tempSql -Force -ErrorAction SilentlyContinue

        if ($procSql.ExitCode -eq 0 -or $procSql.ExitCode -eq 3010) {
            Write-OK "SQL Server 2022 Express instalado correctamente."
            if ($procSql.ExitCode -eq 3010) {
                Write-Warn "Se recomienda reiniciar el equipo después de la instalación."
            }
            $script:SqlServerConn = "localhost\$SQL_INSTANCE_NAME"

            # Esperar a que el servicio SQL Server esté disponible (hasta 60 segundos)
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
                Write-Warn "El servicio no respondió en 60 segundos. Se intentará continuar."
            }

            # Agregar la ruta del motor al PATH de esta sesión para que el paso 7 encuentre sqlcmd
            $sqlToolsPath = "${env:ProgramFiles}\Microsoft SQL Server\160\Tools\Binn"
            if (Test-Path $sqlToolsPath) {
                $env:PATH = "$sqlToolsPath;$env:PATH"
                Write-Info "Ruta de sqlcmd agregada al PATH de la sesión."
            }
        } else {
            Write-Warn "El instalador de SQL Express finalizó con código $($procSql.ExitCode)."
            Write-Warn "Consulte el log en: C:\Program Files\Microsoft SQL Server\*\Setup Bootstrap\Log"
            $script:SqlServerConn = "localhost\$SQL_INSTANCE_NAME"
        }
    } else {
        Write-Warn "No se pudo descargar SQL Server Express. Continuando sin él."
        $script:SqlServerConn = "localhost\$SQL_INSTANCE_NAME"
    }
}

# ?????????????????????????????????????????????????????????????????????????????
# PASO 3 – OBTENER ÚLTIMA VERSIÓN DE GITHUB
# ?????????????????????????????????????????????????????????????????????????????
Write-Step "3/8" "Consultando última versión en GitHub..."

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
    Write-Warn "Verifique la conexión a internet y que el repositorio exista."
    Write-Warn "URL consultada: $apiUrl"
    Read-Host "Presione ENTER para salir"
    exit 1
}

$version    = $release.tag_name -replace '^v', ''
$zipAsset   = $release.assets | Where-Object { $_.name -like "*.zip" } | Select-Object -First 1

if (-not $zipAsset) {
    Write-Fail "No se encontró un archivo .zip en el release $version."
    Write-Warn "El desarrollador debe adjuntar un .zip con los binarios al GitHub Release."
    Read-Host "Presione ENTER para salir"
    exit 1
}

# Para repositorios privados usar la URL de la API; públicos usar browser_download_url
$downloadUrl = if ($GitHubToken) { $zipAsset.url } else { $zipAsset.browser_download_url }
$sizeMB      = [math]::Round($zipAsset.size / 1MB, 1)

Write-OK "Versión encontrada : $version"
Write-Info "Archivo            : $($zipAsset.name) ($sizeMB MB)"
Write-Info "Publicado          : $($release.published_at)"

# ?????????????????????????????????????????????????????????????????????????????
# PASO 3 – DESCARGAR Y EXTRAER LA APLICACIÓN
# ?????????????????????????????????????????????????????????????????????????????
Write-Step "4/8" "Descargando $APP_NAME v$version..."

$tempZip    = Join-Path $env:TEMP "ComercioNET_Install_$version.zip"
$tempExtract = Join-Path $env:TEMP "ComercioNET_Install_$version"

# Limpiar archivos temporales anteriores si existen
Remove-Item $tempZip     -Force -ErrorAction SilentlyContinue
Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue

try {
    $downloadHeaders = $headers.Clone()

    # Para assets de la API de GitHub se necesita Accept: application/octet-stream
    if ($GitHubToken -and $downloadUrl -match "api\.github\.com") {
        $downloadHeaders["Accept"] = "application/octet-stream"
    }

    # Descargar con progreso
    $wc = New-Object System.Net.WebClient
    foreach ($key in $downloadHeaders.Keys) {
        $wc.Headers.Add($key, $downloadHeaders[$key])
    }

    Write-Info "Descargando desde GitHub... (puede tardar según la conexión)"
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
    Write-OK "Archivos extraídos."
} catch {
    Write-Fail "Error extrayendo el zip: $_"
    exit 1
}

# ?????????????????????????????????????????????????????????????????????????????
# PASO 4 – CREAR CARPETA DE INSTALACIÓN Y COPIAR ARCHIVOS
# ?????????????????????????????????????????????????????????????????????????????
Write-Step "5/8" "Instalando en $InstallDir..."

# Si ya existe una instalación previa, hacer backup de los archivos de configuración
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

    # Backup de certificados AFIP (.pfx / .p12)
    $certDir = Join-Path $InstallDir "Certificados FE"
    if (Test-Path $certDir) {
        $backupCertDir = Join-Path $backupDir "Certificados FE"
        Copy-Item $certDir -Destination $backupCertDir -Recurse -Force
        Write-Info "Backup: carpeta 'Certificados FE'"
    }

    # Backup de la carpeta migrations (scripts SQL personalizados)
    $migrDir = Join-Path $InstallDir "migrations"
    if (Test-Path $migrDir) {
        $backupMigrDir = Join-Path $backupDir "migrations"
        Copy-Item $migrDir -Destination $backupMigrDir -Recurse -Force
        Write-Info "Backup: carpeta 'migrations'"
    }

    Write-OK "Backup de configuración guardado en $backupDir"
}

# Crear carpeta de instalación
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null

# Determinar la raíz del contenido extraído
# El zip puede tener una subcarpeta raíz o los archivos directamente
$extractedItems = Get-ChildItem -Path $tempExtract
$sourceDir = if ($extractedItems.Count -eq 1 -and $extractedItems[0].PSIsContainer) {
    $extractedItems[0].FullName  # hay una subcarpeta raíz
} else {
    $tempExtract  # los archivos están en la raíz del zip
}

# Copiar todos los archivos al directorio de instalación
Write-Info "Copiando archivos de la aplicación..."
Copy-Item -Path "$sourceDir\*" -Destination $InstallDir -Recurse -Force
Write-OK "Archivos copiados."

# Restaurar archivos de configuración del backup (si había instalación previa)
if ($backupDir -and (Test-Path $backupDir)) {
    Write-Info "Restaurando configuraciones anteriores..."

    foreach ($archivo in $archivosProtegidos) {
        $backupArchivo = Join-Path $backupDir $archivo
        if (Test-Path $backupArchivo) {
            Copy-Item $backupArchivo -Destination $InstallDir -Force
            Write-Info "  Restaurado: $archivo"
        }
    }

    # Restaurar certificados
    $backupCertDir = Join-Path $backupDir "Certificados FE"
    if (Test-Path $backupCertDir) {
        $destCertDir = Join-Path $InstallDir "Certificados FE"
        New-Item -ItemType Directory -Path $destCertDir -Force | Out-Null
        Copy-Item "$backupCertDir\*" -Destination $destCertDir -Recurse -Force
        Write-Info "  Restaurados: certificados AFIP"
    }

    # Restaurar migrations personalizadas
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

# Escribir version.txt
Set-Content -Path (Join-Path $InstallDir "version.txt") -Value $version -Encoding UTF8
Write-OK "Versión $version registrada."

# ?????????????????????????????????????????????????????????????????????????????
# PASO 5 – CREAR appsettings.json SI NO EXISTE
# ?????????????????????????????????????????????????????????????????????????????
Write-Step "6/8" "Verificando configuración inicial..."

$appSettingsPath = Join-Path $InstallDir "appsettings.json"

if (-not (Test-Path $appSettingsPath)) {
    Write-Info "Generando appsettings.json con valores de ejemplo..."

    # Template de appsettings.json con todos los campos que usa la aplicación
    $appsettingsTemplate = @'
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=comercio;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Comercio": {
    "Nombre": "MI COMERCIO",
    "Domicilio": "Calle 000 N° 000 - Ciudad"
  },
  "Facturacion": {
    "RazonSocial": "Nombre Apellido",
    "CUIT": "00-00000000-0",
    "IngBrutos": "00-00000000-0",
    "DomicilioFiscal": "Calle 000 N° 000 - Ciudad",
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
      "CertificadoPassword": "contraseña_del_certificado",
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
      "CertificadoPassword": "contraseña_del_certificado",
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
    Write-Warn "IMPORTANTE: Edite $appSettingsPath antes de usar la aplicación."
    Write-Warn "  - Cadena de conexión SQL Server"
    Write-Warn "  - Datos del comercio (nombre, domicilio)"
    Write-Warn "  - CUIT y certificados AFIP"
} else {
    Write-OK "appsettings.json existente conservado (no sobreescrito)."
}

# Crear carpeta de certificados si no existe
$certFolder = Join-Path $InstallDir "Certificados FE"
if (-not (Test-Path $certFolder)) {
    New-Item -ItemType Directory -Path $certFolder -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $certFolder "Testing") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $certFolder "Produccion") -Force | Out-Null
    Write-OK "Carpeta 'Certificados FE' creada. Copie aquí sus archivos .p12/.pfx de AFIP."
}

# Crear carpeta migrations si no existe
$migrFolder = Join-Path $InstallDir "migrations"
if (-not (Test-Path $migrFolder)) {
    New-Item -ItemType Directory -Path $migrFolder -Force | Out-Null
    Write-OK "Carpeta 'migrations' creada."
}

# ?????????????????????????????????????????????????????????????????????????????
# PASO 6 – INICIALIZAR BASE DE DATOS
# ?????????????????????????????????????????????????????????????????????????????
Write-Step "7/8" "Inicializando base de datos SQL Server..."

# Verificar si sqlcmd está disponible en el sistema
$sqlcmdPath = $null
$sqlcmdCandidates = @(
    "sqlcmd",
    # SQL Server engine Tools\Binn (instalado junto al motor)
    "${env:ProgramFiles}\Microsoft SQL Server\160\Tools\Binn\sqlcmd.exe",
    "${env:ProgramFiles}\Microsoft SQL Server\150\Tools\Binn\sqlcmd.exe",
    "${env:ProgramFiles}\Microsoft SQL Server\140\Tools\Binn\sqlcmd.exe",
    "${env:ProgramFiles}\Microsoft SQL Server\130\Tools\Binn\sqlcmd.exe",
    # Client SDK / ODBC standalone tools
    "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
    "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\160\Tools\Binn\sqlcmd.exe",
    "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\130\Tools\Binn\sqlcmd.exe",
    "${env:ProgramFiles(x86)}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
    "${env:ProgramFiles(x86)}\Microsoft SQL Server\Client SDK\ODBC\160\Tools\Binn\sqlcmd.exe",
    # go-sqlcmd (nueva CLI de Microsoft)
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

# Copiar el script SQL desde la carpeta database\ del zip al directorio de instalación
$dbScriptSrc = Join-Path $sourceDir $DB_INIT_SCRIPT
if (-not (Test-Path (Join-Path $InstallDir "database"))) {
    New-Item -ItemType Directory -Path (Join-Path $InstallDir "database") -Force | Out-Null
}
if (Test-Path $dbScriptSrc) {
    Copy-Item $dbScriptSrc -Destination $dbScriptDest -Force
}

if (-not $sqlcmdPath) {
    Write-Warn "sqlcmd no encontrado en este equipo."
    Write-Warn "La base de datos NO se inicializará automáticamente."
    Write-Info "Para crearla manualmente, ejecute en SSMS o sqlcmd:"
    Write-Info "  $dbScriptDest"
    Write-Info "O instale las SQL Server Command Line Utilities desde:"
    Write-Info "  https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-utility"
} elseif (-not (Test-Path $dbScriptDest)) {
    Write-Warn "Script SQL no encontrado en: $dbScriptDest"
    Write-Warn "Asegúrese de que el .zip del release incluya la carpeta database\"
} else {
    # Usar la instancia detectada/instalada en el paso 2
    $sqlServer = if ($script:SqlServerConn) { $script:SqlServerConn } else { "localhost" }
    Write-Info "sqlcmd encontrado  : $sqlcmdPath"
    Write-Info "Servidor SQL target: $sqlServer"
    Write-Info "Ejecutando script de inicialización..."

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
            Write-Info "  Usuario creado: admin / contraseña: 1506"
            Write-Warn "  Cambie la contraseña del administrador desde la aplicación."

            # ?? Actualizar appsettings.json con la connection string real ??????
            $connString = "Server=$sqlServer;Database=$DB_NAME;Trusted_Connection=True;TrustServerCertificate=True;"
            if (Test-Path $appSettingsPath) {
                try {
                    $json = Get-Content $appSettingsPath -Raw -Encoding UTF8
                    # Reemplazar el valor de DefaultConnection usando regex (preserva el resto del JSON)
                    $json = $json -replace '(?<="DefaultConnection"\s*:\s*")[^"]*(?=")', $connString
                    Set-Content -Path $appSettingsPath -Value $json -Encoding UTF8
                    Write-OK "appsettings.json actualizado con la conexión: $sqlServer"
                } catch {
                    Write-Warn "No se pudo actualizar appsettings.json: $_"
                    Write-Info "Connection string a configurar manualmente:"
                    Write-Info "  $connString"
                }
            }
            # ??????????????????????????????????????????????????????????????????
        } else {
            Write-Warn "sqlcmd finalizó con código $LASTEXITCODE."
            Write-Warn "Puede que la BD ya existiera (normal en reinstalación) o haya un error."
            Write-Info "Revise el log en: $sqlLogPath"
        }
    } catch {
        Write-Fail "Error ejecutando sqlcmd: $_"
        Write-Info "Ejecute manualmente el script: $dbScriptDest"
    }
}

# ?????????????????????????????????????????????????????????????????????????????
# PASO 7 – ACCESO DIRECTO EN EL ESCRITORIO
# ?????????????????????????????????????????????????????????????????????????????
Write-Step "8/8" "Creando acceso directo en el escritorio..."

$exePath       = Join-Path $InstallDir $APP_EXE
$shortcutPath  = Join-Path ([Environment]::GetFolderPath("CommonDesktopDirectory")) "$APP_NAME.lnk"

if (Test-Path $exePath) {
    try {
        $wshell   = New-Object -ComObject WScript.Shell
        $shortcut = $wshell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath       = $exePath
        $shortcut.WorkingDirectory = $InstallDir
        $shortcut.Description      = "$APP_NAME - Sistema de Gestión Comercial"

        # Usar el ícono del propio exe si existe
        $shortcut.IconLocation = "$exePath, 0"

        $shortcut.Save()
        Write-OK "Acceso directo creado en el escritorio."
    } catch {
        Write-Warn "No se pudo crear el acceso directo: $_"
        Write-Info "Puede crearlo manualmente desde: $exePath"
    }
} else {
    Write-Warn "No se encontró $APP_EXE en $InstallDir."
    Write-Warn "Verifique que el .zip del release contenga el ejecutable."
}

# ?????????????????????????????????????????????????????????????????????????????
# LIMPIEZA
# ?????????????????????????????????????????????????????????????????????????????
Remove-Item $tempZip     -Force -ErrorAction SilentlyContinue
Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue

# ?????????????????????????????????????????????????????????????????????????????
# RESUMEN FINAL
# ?????????????????????????????????????????????????????????????????????????????
Write-Header "INSTALACIÓN COMPLETADA"

Write-Host "  Version instalada : " -NoNewline
Write-Host $version -ForegroundColor Green

Write-Host "  Carpeta           : " -NoNewline
Write-Host $InstallDir -ForegroundColor Cyan

Write-Host "  Ejecutable        : " -NoNewline
Write-Host (Join-Path $InstallDir $APP_EXE) -ForegroundColor Cyan

Write-Host ""
Write-Host "  PASOS OBLIGATORIOS ANTES DE USAR:" -ForegroundColor Yellow
Write-Host "  ??????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host "  1. Editar appsettings.json con los datos del cliente:" -ForegroundColor White
Write-Host "     $appSettingsPath" -ForegroundColor Gray
Write-Host "     - ConnectionStrings: cadena de conexión SQL Server" -ForegroundColor Gray
Write-Host "     - Comercio.Nombre y Comercio.Domicilio" -ForegroundColor Gray
Write-Host "     - Facturacion: CUIT, Razón Social, Condición" -ForegroundColor Gray
Write-Host "     - AFIP: CUIT, PuntoVenta, rutas de certificados" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Copiar certificados AFIP (.p12 / .pfx) en:" -ForegroundColor White
Write-Host "     $certFolder" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Verificar que SQL Server esté corriendo en localhost" -ForegroundColor White
Write-Host "     (la BD 'comercio' fue creada automáticamente durante" -ForegroundColor Gray
Write-Host "      la instalación si sqlcmd estaba disponible)." -ForegroundColor Gray
Write-Host "     Si no se creó, ejecute manualmente:" -ForegroundColor Gray
Write-Host "     $(Join-Path $InstallDir $DB_INIT_SCRIPT)" -ForegroundColor Gray
Write-Host ""
Write-Host "  4. Ejecutar la aplicación con el acceso directo" -ForegroundColor White
Write-Host "     del escritorio o desde:" -ForegroundColor Gray
Write-Host "     $exePath" -ForegroundColor Gray
Write-Host ""
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host ""

# Preguntar si abrir la carpeta de instalación
$respuesta = Read-Host "¿Desea abrir la carpeta de instalación ahora? (S/N)"
if ($respuesta -match "^[sS]") {
    Start-Process explorer.exe -ArgumentList $InstallDir
}
