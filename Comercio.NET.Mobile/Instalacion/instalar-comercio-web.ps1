# ============================================================
# Script de Instalacion - Comercio.NET Web
# Ejecutar como Administrador en el servidor del cliente
# ============================================================
param(
    [string]$RutaInstalacion = "C:\ComercioWeb",
    [switch]$SinPostgres,
    [switch]$SinServicio
)

$ErrorActionPreference = "Stop"
$Host.UI.RawUI.WindowTitle = "Instalador Comercio.NET Web"

function Write-Header($texto) {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "  $texto" -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Paso($numero, $texto) {
    Write-Host "[$numero] $texto" -ForegroundColor Yellow
}

function Write-OK($texto) {
    Write-Host "    [OK] $texto" -ForegroundColor Green
}

function Write-Aviso($texto) {
    Write-Host "    [!] $texto" -ForegroundColor DarkYellow
}

# ============================================================
Write-Header "INSTALADOR COMERCIO.NET WEB"
Write-Host "  Este script instalara y configurara la aplicacion"
Write-Host "  Comercio.NET Web en este servidor."
Write-Host ""
Write-Host "  Ruta de instalacion: $RutaInstalacion"
Write-Host ""

# Verificar que se ejecuta como administrador
$esAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $esAdmin) {
    Write-Host "ERROR: Este script debe ejecutarse como Administrador." -ForegroundColor Red
    Write-Host "Haga clic derecho en PowerShell y seleccione 'Ejecutar como administrador'." -ForegroundColor Red
    exit 1
}

# ============================================================
# PASO 1: Instalar .NET 8 Runtime
# ============================================================
Write-Paso 1 "Verificando .NET 8 Runtime..."

$dotnetInstalado = $false
try {
    $runtimes = dotnet --list-runtimes 2>$null
    if ($runtimes -match "Microsoft.AspNetCore.App 8") {
        $dotnetInstalado = $true
        Write-OK ".NET 8 ASP.NET Core Runtime ya esta instalado"
    }
} catch {}

if (-not $dotnetInstalado) {
    Write-Host "    Descargando .NET 8 ASP.NET Core Hosting Bundle..." -ForegroundColor White
    $dotnetUrl = "https://download.visualstudio.microsoft.com/download/pr/hosting-bundle-8.0-latest-win-x64.exe"
    $dotnetInstaller = "$env:TEMP\dotnet-hosting-8.exe"

    try {
        Invoke-WebRequest -Uri "https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-aspnetcore-8.0.11-windows-hosting-bundle-installer" -OutFile $dotnetInstaller -UseBasicParsing
        # Fallback a URL directa
        if (-not (Test-Path $dotnetInstaller) -or (Get-Item $dotnetInstaller).Length -lt 1MB) {
            Invoke-WebRequest -Uri $dotnetUrl -OutFile $dotnetInstaller -UseBasicParsing
        }
    } catch {
        Write-Aviso "No se pudo descargar automaticamente."
        Write-Host "    Descargue manualmente el .NET 8 Hosting Bundle desde:" -ForegroundColor White
        Write-Host "    https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
        Read-Host "    Presione Enter cuando lo haya instalado"
    }

    if (Test-Path $dotnetInstaller) {
        Write-Host "    Instalando .NET 8 Runtime..." -ForegroundColor White
        Start-Process -FilePath $dotnetInstaller -ArgumentList "/quiet /norestart" -Wait
        Write-OK ".NET 8 Runtime instalado"
    }
}

# ============================================================
# PASO 2: Instalar PostgreSQL
# ============================================================
Write-Paso 2 "Verificando PostgreSQL..."

$pgInstalado = $false
$pgPath = ""

# Buscar PostgreSQL instalado
$pgPaths = @(
    "C:\Program Files\PostgreSQL\16",
    "C:\Program Files\PostgreSQL\15",
    "C:\Program Files\PostgreSQL\14"
)
foreach ($p in $pgPaths) {
    if (Test-Path "$p\bin\psql.exe") {
        $pgInstalado = $true
        $pgPath = "$p\bin"
        break
    }
}

if ($pgInstalado) {
    Write-OK "PostgreSQL encontrado en: $pgPath"
} elseif (-not $SinPostgres) {
    Write-Aviso "PostgreSQL no esta instalado."
    Write-Host "    Opciones:" -ForegroundColor White
    Write-Host "      1. Descargar e instalar ahora (se abrira el navegador)" -ForegroundColor White
    Write-Host "      2. Ya tengo PostgreSQL en otro servidor (continuar sin instalar)" -ForegroundColor White
    $opcion = Read-Host "    Seleccione (1/2)"

    if ($opcion -eq "1") {
        Start-Process "https://www.postgresql.org/download/windows/"
        Write-Host ""
        Write-Host "    Instale PostgreSQL con las opciones por defecto." -ForegroundColor White
        Write-Host "    Recuerde la contrasena del usuario 'postgres'." -ForegroundColor White
        Read-Host "    Presione Enter cuando termine la instalacion"

        foreach ($p in $pgPaths) {
            if (Test-Path "$p\bin\psql.exe") {
                $pgInstalado = $true
                $pgPath = "$p\bin"
                break
            }
        }
    }
}

# ============================================================
# PASO 3: Recopilar datos del cliente
# ============================================================
Write-Paso 3 "Configuracion del comercio..."
Write-Host ""

$nombreComercio = Read-Host "    Nombre del comercio"
$domicilio = Read-Host "    Domicilio del comercio"
$razonSocial = Read-Host "    Razon Social"
$cuit = Read-Host "    CUIT (formato: XX-XXXXXXXX-X)"
$ingBrutos = Read-Host "    Ingresos Brutos (o Enter si es igual al CUIT)"
if ([string]::IsNullOrWhiteSpace($ingBrutos)) { $ingBrutos = $cuit }
$domicilioFiscal = Read-Host "    Domicilio Fiscal"
$codigoPostal = Read-Host "    Codigo Postal"
$inicioActividades = Read-Host "    Inicio de Actividades (formato: YYYY-MM-DD)"

Write-Host ""
Write-Host "    Condicion frente al IVA:" -ForegroundColor White
Write-Host "      1. Responsable Inscripto" -ForegroundColor White
Write-Host "      2. Monotributo" -ForegroundColor White
$condIva = Read-Host "    Seleccione (1/2)"
$condicionIVA = if ($condIva -eq "1") { "Responsable inscripto" } else { "Monotributo" }

Write-Host ""
Write-Host "    Configuracion de Base de Datos:" -ForegroundColor White
$pgHost = Read-Host "    Host PostgreSQL (Enter para localhost)"
if ([string]::IsNullOrWhiteSpace($pgHost)) { $pgHost = "localhost" }
$pgPort = Read-Host "    Puerto (Enter para 5432)"
if ([string]::IsNullOrWhiteSpace($pgPort)) { $pgPort = "5432" }
$pgDatabase = Read-Host "    Nombre de la base de datos (Enter para 'comercio')"
if ([string]::IsNullOrWhiteSpace($pgDatabase)) { $pgDatabase = "comercio" }
$pgUser = Read-Host "    Usuario (Enter para 'postgres')"
if ([string]::IsNullOrWhiteSpace($pgUser)) { $pgUser = "postgres" }
$pgPassword = Read-Host "    Contrasena de PostgreSQL" -AsSecureString
$pgPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($pgPassword))

Write-Host ""
Write-Host "    Configuracion AFIP:" -ForegroundColor White
Write-Host "      1. Testing (pruebas)" -ForegroundColor White
Write-Host "      2. Produccion" -ForegroundColor White
$ambienteAfip = Read-Host "    Seleccione (1/2)"
$ambiente = if ($ambienteAfip -eq "1") { "Testing" } else { "Produccion" }

$puntoVenta = Read-Host "    Punto de venta AFIP (numero)"
$certPath = Read-Host "    Ruta completa del certificado AFIP (.p12)"
$certPassword = Read-Host "    Contrasena del certificado"

Write-Host ""
$puerto = Read-Host "    Puerto de la aplicacion web (Enter para 8080)"
if ([string]::IsNullOrWhiteSpace($puerto)) { $puerto = "8080" }

# ============================================================
# PASO 4: Crear estructura de carpetas
# ============================================================
Write-Paso 4 "Creando estructura de carpetas..."

New-Item -ItemType Directory -Force -Path $RutaInstalacion | Out-Null
New-Item -ItemType Directory -Force -Path "$RutaInstalacion\logs" | Out-Null
New-Item -ItemType Directory -Force -Path "$RutaInstalacion\Certificados" | Out-Null

Write-OK "Carpetas creadas en $RutaInstalacion"

# Copiar certificado si existe
if (Test-Path $certPath) {
    $certDestino = "$RutaInstalacion\Certificados\$(Split-Path $certPath -Leaf)"
    Copy-Item $certPath $certDestino -Force
    $certPath = $certDestino
    Write-OK "Certificado copiado a $certDestino"
}

# ============================================================
# PASO 5: Copiar archivos de la aplicacion
# ============================================================
Write-Paso 5 "Copiando archivos de la aplicacion..."

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishDir = Join-Path $scriptDir "publish"

if (Test-Path $publishDir) {
    Copy-Item "$publishDir\*" $RutaInstalacion -Recurse -Force
    Write-OK "Archivos copiados desde carpeta 'publish'"
} else {
    Write-Aviso "No se encontro la carpeta 'publish' junto al script."
    Write-Host "    Copie manualmente los archivos publicados a: $RutaInstalacion" -ForegroundColor White
    Write-Host "    (Use: dotnet publish -c Release -o ./publish)" -ForegroundColor White
    Read-Host "    Presione Enter cuando los archivos esten copiados"
}

# ============================================================
# PASO 6: Generar appsettings.json
# ============================================================
Write-Paso 6 "Generando configuracion (appsettings.json)..."

$connectionString = "Host=$pgHost;Port=$pgPort;Database=$pgDatabase;Username=$pgUser;Password=$pgPasswordPlain"

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
    "RazonSocial": "$razonSocial",
    "CUIT": "$cuit",
    "IngBrutos": "$ingBrutos",
    "DomicilioFiscal": "$domicilioFiscal",
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
    "AmbienteActivo": "$ambiente",
    "Testing": {
      "CUIT": "$cuit",
      "CondicionIVA": "$condicionIVA",
      "PuntoVenta": $puntoVenta,
      "CertificadoPath": "$($certPath -replace '\\', '\\\\')",
      "CertificadoPassword": "$certPassword",
      "WSAAUrl": "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",
      "WSFEUrl": "https://wswhomo.afip.gov.ar/wsfev1/service.asmx",
      "Servicios": {
        "Facturacion": "wsfe",
        "Padron": "ws_sr_constancia_inscripcion",
        "PadronA5": "ws_sr_padron_a5"
      }
    },
    "Produccion": {
      "CUIT": "$cuit",
      "CondicionIVA": "$condicionIVA",
      "PuntoVenta": $puntoVenta,
      "CertificadoPath": "$($certPath -replace '\\', '\\\\')",
      "CertificadoPassword": "$certPassword",
      "WSAAUrl": "https://wsaa.afip.gov.ar/ws/services/LoginCms",
      "WSFEUrl": "https://servicios1.afip.gov.ar/wsfev1/service.asmx",
      "Servicios": {
        "Facturacion": "wsfe",
        "Padron": "ws_sr_constancia_inscripcion",
        "PadronA5": "ws_sr_padron_a5"
      }
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:$puerto"
      }
    }
  }
}
"@

$appsettings | Out-File "$RutaInstalacion\appsettings.json" -Encoding UTF8
Write-OK "appsettings.json generado"

# ============================================================
# PASO 7: Crear base de datos
# ============================================================
Write-Paso 7 "Configurando base de datos..."

$scriptSql = Join-Path $scriptDir "crear-base-datos.sql"
if ((Test-Path $scriptSql) -and $pgInstalado) {
    $env:PGPASSWORD = $pgPasswordPlain

    # Crear base de datos si no existe
    & "$pgPath\psql.exe" -h $pgHost -p $pgPort -U $pgUser -tc "SELECT 1 FROM pg_database WHERE datname = '$pgDatabase'" | Out-Null
    if ($LASTEXITCODE -ne 0 -or -not ($_ -match "1")) {
        & "$pgPath\createdb.exe" -h $pgHost -p $pgPort -U $pgUser $pgDatabase 2>$null
    }

    # Ejecutar esquema
    & "$pgPath\psql.exe" -h $pgHost -p $pgPort -U $pgUser -d $pgDatabase -f $scriptSql
    Write-OK "Base de datos '$pgDatabase' creada y esquema aplicado"

    $env:PGPASSWORD = ""
} else {
    Write-Aviso "Ejecute manualmente el script 'crear-base-datos.sql' en su PostgreSQL"
}

# ============================================================
# PASO 8: Registrar como servicio de Windows
# ============================================================
if (-not $SinServicio) {
    Write-Paso 8 "Registrando como servicio de Windows..."

    $serviceName = "ComercioNETWeb"
    $exePath = "$RutaInstalacion\Comercio.NET.Mobile.Server.exe"

    # Detener servicio existente si hay
    $existente = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if ($existente) {
        Stop-Service $serviceName -Force -ErrorAction SilentlyContinue
        sc.exe delete $serviceName | Out-Null
        Start-Sleep -Seconds 2
    }

    # Crear servicio
    New-Service -Name $serviceName `
        -BinaryPathName $exePath `
        -DisplayName "Comercio.NET Web" `
        -Description "Servidor web del sistema de gestion comercial Comercio.NET" `
        -StartupType Automatic | Out-Null

    # Iniciar servicio
    Start-Service $serviceName
    Write-OK "Servicio '$serviceName' creado e iniciado"
}

# ============================================================
# PASO 9: Configurar Firewall
# ============================================================
Write-Paso 9 "Configurando Firewall..."

$ruleName = "Comercio.NET Web (Puerto $puerto)"
$existeRegla = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue

if (-not $existeRegla) {
    New-NetFirewallRule -DisplayName $ruleName `
        -Direction Inbound `
        -Protocol TCP `
        -LocalPort $puerto `
        -Action Allow `
        -Profile Private,Domain | Out-Null
    Write-OK "Regla de firewall creada para puerto $puerto"
} else {
    Write-OK "Regla de firewall ya existia"
}

# ============================================================
# FINALIZADO
# ============================================================
Write-Host ""
Write-Header "INSTALACION COMPLETADA"
Write-Host ""
Write-Host "  La aplicacion esta corriendo en:" -ForegroundColor White
Write-Host ""

# Obtener IP local
$ipLocal = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notmatch "Loopback" -and $_.IPAddress -ne "127.0.0.1" } | Select-Object -First 1).IPAddress

Write-Host "    Desde este equipo:  http://localhost:$puerto" -ForegroundColor Green
if ($ipLocal) {
    Write-Host "    Desde otros equipos: http://${ipLocal}:$puerto" -ForegroundColor Green
}
Write-Host ""
Write-Host "  Para acceder desde tablets/celulares conectados a la misma" -ForegroundColor White
Write-Host "  red WiFi, use la segunda URL en el navegador del dispositivo." -ForegroundColor White
Write-Host ""
Write-Host "  Servicio de Windows: ComercioNETWeb (arranca automaticamente)" -ForegroundColor White
Write-Host "  Carpeta de instalacion: $RutaInstalacion" -ForegroundColor White
Write-Host "  Logs: $RutaInstalacion\logs" -ForegroundColor White
Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
