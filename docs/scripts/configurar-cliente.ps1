# Script de Configuración - PC del Cliente
# Ejecutar en la PC de Victor como Administrador
# 
# Este script automatiza parte de la configuración
# Aún necesitarás:
#   1. Instalar manualmente: .NET 8 Runtime, NSSM, cloudflared
#   2. Copiar el archivo de credenciales .json
#   3. Configurar appsettings.json con el connection string

param(
    [Parameter(Mandatory=$true)]
    [string]$TunnelId,

    [Parameter(Mandatory=$true)]
    [string]$ClienteName,

    [Parameter(Mandatory=$false)]
    [string]$SqlServerIp = "localhost",

    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "Comercio"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CONFIGURACIÓN SQLBRIDGE - $ClienteName" -ForegroundColor Cyan
Write-Host "  Subdominio: $ClienteName.tpqsolutions.com.ar" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ============================================
# VALIDACIONES
# ============================================

Write-Host "Validando requisitos..." -ForegroundColor Yellow
Write-Host ""

# Verificar que se está ejecutando como administrador
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "? ERROR: Este script debe ejecutarse como Administrador" -ForegroundColor Red
    Write-Host "   Haz clic derecho en PowerShell y selecciona 'Ejecutar como administrador'" -ForegroundColor Yellow
    exit 1
}

# Verificar .NET 8
try {
    $dotnetVersion = dotnet --version
    Write-Host "? .NET instalado: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "? .NET 8 Runtime no encontrado" -ForegroundColor Red
    Write-Host "   Descárgalo de: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}

# Verificar NSSM
if (-not (Test-Path "C:\nssm\nssm.exe")) {
    Write-Host "? NSSM no encontrado en C:\nssm\" -ForegroundColor Red
    Write-Host "   Descárgalo de: https://nssm.cc/download" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "? NSSM instalado" -ForegroundColor Green
}

# Verificar cloudflared
if (-not (Test-Path "C:\cloudflared\cloudflared.exe")) {
    Write-Host "? cloudflared no encontrado en C:\cloudflared\" -ForegroundColor Red
    Write-Host "   Descárgalo de: https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/install-and-setup/installation/" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "? cloudflared instalado" -ForegroundColor Green
}

Write-Host ""

# ============================================
# PASO 1: CREAR DIRECTORIOS
# ============================================

Write-Host "PASO 1: Creando directorios..." -ForegroundColor Green
Write-Host "-----------------------------------" -ForegroundColor Gray

New-Item -Path "C:\SqlBridge" -ItemType Directory -Force | Out-Null
New-Item -Path "C:\SqlBridge\logs" -ItemType Directory -Force | Out-Null
New-Item -Path "$env:USERPROFILE\.cloudflared" -ItemType Directory -Force | Out-Null

Write-Host "? Directorios creados:" -ForegroundColor Green
Write-Host "   • C:\SqlBridge\" -ForegroundColor Gray
Write-Host "   • C:\SqlBridge\logs\" -ForegroundColor Gray
Write-Host "   • $env:USERPROFILE\.cloudflared\" -ForegroundColor Gray
Write-Host ""

# ============================================
# PASO 2: VERIFICAR ARCHIVOS
# ============================================

Write-Host "PASO 2: Verificando archivos necesarios..." -ForegroundColor Green
Write-Host "-----------------------------------" -ForegroundColor Gray

# Verificar SQLBridge
if (-not (Test-Path "C:\SqlBridge\Comercio.NET.SqlBridge.Server.dll")) {
    Write-Host "??  SQLBridge no encontrado en C:\SqlBridge\" -ForegroundColor Yellow
    Write-Host "   Copia los archivos de SQLBridge a C:\SqlBridge\" -ForegroundColor Yellow
    Write-Host ""
    $respuesta = Read-Host "¿Los archivos de SQLBridge ya están en C:\SqlBridge\? (s/n)"
    if ($respuesta -ne "s" -and $respuesta -ne "S") {
        Write-Host "? Por favor copia los archivos de SQLBridge primero" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "? SQLBridge encontrado" -ForegroundColor Green
}

# Verificar archivo de credenciales
$credentialsFile = "$env:USERPROFILE\.cloudflared\$TunnelId.json"
if (-not (Test-Path $credentialsFile)) {
    Write-Host "??  Archivo de credenciales no encontrado:" -ForegroundColor Yellow
    Write-Host "   $credentialsFile" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   Copia el archivo de credenciales del tunnel a:" -ForegroundColor Yellow
    Write-Host "   $env:USERPROFILE\.cloudflared\" -ForegroundColor Gray
    Write-Host ""
    $respuesta = Read-Host "¿El archivo de credenciales ya está copiado? (s/n)"
    if ($respuesta -ne "s" -and $respuesta -ne "S") {
        Write-Host "? Por favor copia el archivo de credenciales primero" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "? Archivo de credenciales encontrado" -ForegroundColor Green
}

Write-Host ""

# ============================================
# PASO 3: CREAR config.yml
# ============================================

Write-Host "PASO 3: Creando config.yml..." -ForegroundColor Green
Write-Host "-----------------------------------" -ForegroundColor Gray

$configContent = @"
tunnel: $TunnelId
credentials-file: $env:USERPROFILE\.cloudflared\$TunnelId.json

ingress:
  - hostname: $ClienteName.tpqsolutions.com.ar
    service: http://localhost:5000
  - service: http_status:404
"@

$configContent | Out-File -FilePath "C:\cloudflared\config.yml" -Encoding UTF8 -Force

Write-Host "? config.yml creado en C:\cloudflared\config.yml" -ForegroundColor Green
Write-Host ""
Write-Host "Contenido:" -ForegroundColor Gray
Write-Host $configContent -ForegroundColor DarkGray
Write-Host ""

# ============================================
# PASO 4: CONFIGURAR appsettings.json
# ============================================

Write-Host "PASO 4: Configurando appsettings.json..." -ForegroundColor Green
Write-Host "-----------------------------------" -ForegroundColor Gray

Write-Host ""
Write-Host "??  NECESITAS CONFIGURAR LA CONEXIÓN A SQL SERVER MANUALMENTE" -ForegroundColor Yellow
Write-Host ""

$sqlUser = Read-Host "Usuario de SQL Server (ej: sa)"
$sqlPassword = Read-Host "Contraseña de SQL Server" -AsSecureString
$sqlPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($sqlPassword))

$appsettingsContent = @"
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=$SqlServerIp,1433;Database=$DatabaseName;User Id=$sqlUser;Password=$sqlPasswordPlain;TrustServerCertificate=True;Encrypt=False;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
"@

$appsettingsContent | Out-File -FilePath "C:\SqlBridge\appsettings.json" -Encoding UTF8 -Force

Write-Host ""
Write-Host "? appsettings.json creado" -ForegroundColor Green
Write-Host ""

# ============================================
# PASO 5: PROBAR SQLBRIDGE
# ============================================

Write-Host "PASO 5: Probando SQLBridge..." -ForegroundColor Green
Write-Host "-----------------------------------" -ForegroundColor Gray

Write-Host "Iniciando SQLBridge (esto puede tardar unos segundos)..." -ForegroundColor Cyan

$sqlBridgeJob = Start-Job -ScriptBlock {
    Set-Location "C:\SqlBridge"
    & "C:\Program Files\dotnet\dotnet.exe" "Comercio.NET.SqlBridge.Server.dll"
}

Start-Sleep -Seconds 5

# Probar health check
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/health" -TimeoutSec 5
    Write-Host "? SQLBridge está funcionando correctamente" -ForegroundColor Green
    Write-Host "   Response: $($response | ConvertTo-Json -Compress)" -ForegroundColor Gray
} catch {
    Write-Host "? Error al conectar con SQLBridge" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    Stop-Job -Job $sqlBridgeJob
    Remove-Job -Job $sqlBridgeJob
    exit 1
}

Stop-Job -Job $sqlBridgeJob
Remove-Job -Job $sqlBridgeJob

Write-Host ""

# ============================================
# PASO 6: INSTALAR SERVICIOS
# ============================================

Write-Host "PASO 6: Instalando servicios de Windows..." -ForegroundColor Green
Write-Host "-----------------------------------" -ForegroundColor Gray

# Instalar SQLBridge
Write-Host "Instalando servicio SqlBridgeWeb..." -ForegroundColor Cyan

& "C:\nssm\nssm.exe" install SqlBridgeWeb "C:\Program Files\dotnet\dotnet.exe" "C:\SqlBridge\Comercio.NET.SqlBridge.Server.dll"
& "C:\nssm\nssm.exe" set SqlBridgeWeb AppDirectory "C:\SqlBridge"
& "C:\nssm\nssm.exe" set SqlBridgeWeb Start SERVICE_AUTO_START
& "C:\nssm\nssm.exe" set SqlBridgeWeb Description "SQL Bridge para aplicación móvil Comercio.NET"
& "C:\nssm\nssm.exe" set SqlBridgeWeb AppStdout "C:\SqlBridge\logs\nssm-stdout.log"
& "C:\nssm\nssm.exe" set SqlBridgeWeb AppStderr "C:\SqlBridge\logs\nssm-stderr.log"

Write-Host "? Servicio SqlBridgeWeb instalado" -ForegroundColor Green

# Instalar cloudflared
Write-Host "Instalando servicio cloudflared..." -ForegroundColor Cyan

& "C:\cloudflared\cloudflared.exe" service install

Set-Service -Name cloudflared -StartupType Automatic

Write-Host "? Servicio cloudflared instalado" -ForegroundColor Green

Write-Host ""

# ============================================
# PASO 7: INICIAR SERVICIOS
# ============================================

Write-Host "PASO 7: Iniciando servicios..." -ForegroundColor Green
Write-Host "-----------------------------------" -ForegroundColor Gray

# Iniciar SQLBridge
Write-Host "Iniciando SqlBridgeWeb..." -ForegroundColor Cyan
& "C:\nssm\nssm.exe" start SqlBridgeWeb
Start-Sleep -Seconds 3

$status = & "C:\nssm\nssm.exe" status SqlBridgeWeb
Write-Host "   Estado: $status" -ForegroundColor Gray

# Iniciar cloudflared
Write-Host "Iniciando cloudflared..." -ForegroundColor Cyan
Start-Service cloudflared
Start-Sleep -Seconds 3

$status = Get-Service cloudflared
Write-Host "   Estado: $($status.Status)" -ForegroundColor Gray

Write-Host ""

# ============================================
# PASO 8: VERIFICACIÓN FINAL
# ============================================

Write-Host "PASO 8: Verificación final..." -ForegroundColor Green
Write-Host "-----------------------------------" -ForegroundColor Gray
Write-Host ""

Write-Host "Esperando a que los servicios estén listos (10 segundos)..." -ForegroundColor Cyan
Start-Sleep -Seconds 10

# Health check local
Write-Host "1. Probando health check local..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/health" -TimeoutSec 5
    Write-Host "   ? Local: OK" -ForegroundColor Green
} catch {
    Write-Host "   ? Local: ERROR" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Health check público
Write-Host "2. Probando health check público..." -ForegroundColor Cyan
Write-Host "   URL: https://$ClienteName.tpqsolutions.com.ar/health" -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri "https://$ClienteName.tpqsolutions.com.ar/health" -TimeoutSec 10
    Write-Host "   ? Público: OK" -ForegroundColor Green
    Write-Host "   Response: $($response | ConvertTo-Json -Compress)" -ForegroundColor Gray
} catch {
    Write-Host "   ? Público: ERROR" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "   Posibles causas:" -ForegroundColor Yellow
    Write-Host "   • El tunnel necesita unos segundos más para conectarse" -ForegroundColor Yellow
    Write-Host "   • Verifica que el archivo de credenciales sea correcto" -ForegroundColor Yellow
    Write-Host "   • Verifica que el config.yml tenga el Tunnel ID correcto" -ForegroundColor Yellow
}

Write-Host ""

# ============================================
# RESUMEN FINAL
# ============================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ? CONFIGURACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "INFORMACIÓN DE LA INSTALACIÓN:" -ForegroundColor Yellow
Write-Host "????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Cliente:           $ClienteName" -ForegroundColor White
Write-Host "Tunnel ID:         $TunnelId" -ForegroundColor White
Write-Host "Subdominio:        $ClienteName.tpqsolutions.com.ar" -ForegroundColor White
Write-Host "SQL Server:        $SqlServerIp" -ForegroundColor White
Write-Host "Base de Datos:     $DatabaseName" -ForegroundColor White
Write-Host ""
Write-Host "URLs:" -ForegroundColor Yellow
Write-Host "  Health Check:    https://$ClienteName.tpqsolutions.com.ar/health" -ForegroundColor Cyan
Write-Host "  Query Endpoint:  https://$ClienteName.tpqsolutions.com.ar/query" -ForegroundColor Cyan
Write-Host ""
Write-Host "Servicios Windows:" -ForegroundColor Yellow
Write-Host "  • SqlBridgeWeb   (Puerto 5000)" -ForegroundColor White
Write-Host "  • cloudflared    (Tunnel)" -ForegroundColor White
Write-Host ""
Write-Host "Archivos Importantes:" -ForegroundColor Yellow
Write-Host "  • Config:        C:\cloudflared\config.yml" -ForegroundColor White
Write-Host "  • Credenciales:  $env:USERPROFILE\.cloudflared\$TunnelId.json" -ForegroundColor White
Write-Host "  • appsettings:   C:\SqlBridge\appsettings.json" -ForegroundColor White
Write-Host "  • Logs:          C:\SqlBridge\logs\" -ForegroundColor White
Write-Host ""

Write-Host "PRÓXIMOS PASOS:" -ForegroundColor Yellow
Write-Host "????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "1. Reinicia la PC para verificar que todo inicia automáticamente" -ForegroundColor White
Write-Host "2. Configura la variable SQL_BRIDGE_URL en Railway:" -ForegroundColor White
Write-Host "   SQL_BRIDGE_URL=https://$ClienteName.tpqsolutions.com.ar" -ForegroundColor Cyan
Write-Host "3. Prueba la aplicación móvil" -ForegroundColor White
Write-Host ""

Write-Host "COMANDOS ÚTILES:" -ForegroundColor Yellow
Write-Host "????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Ver logs SQLBridge:" -ForegroundColor White
Write-Host "  Get-Content 'C:\SqlBridge\logs\sqlbridge_`$(Get-Date -Format 'yyyyMMdd').log' -Tail 20" -ForegroundColor Gray
Write-Host ""
Write-Host "Reiniciar servicios:" -ForegroundColor White
Write-Host "  Restart-Service SqlBridgeWeb, cloudflared" -ForegroundColor Gray
Write-Host ""
Write-Host "Ver estado de servicios:" -ForegroundColor White
Write-Host "  Get-Service SqlBridgeWeb, cloudflared" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Script completado exitosamente" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
