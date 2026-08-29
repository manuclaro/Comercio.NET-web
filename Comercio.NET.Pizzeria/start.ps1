# Script de inicio para Comercio.NET Pizzería
# Configura las variables de entorno necesarias y ejecuta el servidor

Write-Host "=== Comercio.NET Pizzería ===" -ForegroundColor Cyan
Write-Host ""

# Solicitar SQL Bridge URL si no está configurada
$sqlBridgeUrl = $env:SQL_BRIDGE_URL
if ([string]::IsNullOrEmpty($sqlBridgeUrl)) {
    Write-Host "SQL_BRIDGE_URL no está configurada." -ForegroundColor Yellow
    $sqlBridgeUrl = Read-Host "Ingrese la URL del SQL Bridge (ej: http://localhost:5000)"
    $env:SQL_BRIDGE_URL = $sqlBridgeUrl
}

Write-Host "SQL Bridge URL: $sqlBridgeUrl" -ForegroundColor Green

# Puerto del servidor
$port = $env:PORT
if ([string]::IsNullOrEmpty($port)) {
    $port = "8081"
    $env:PORT = $port
}

Write-Host "Puerto del servidor: $port" -ForegroundColor Green
Write-Host ""
Write-Host "Iniciando servidor..." -ForegroundColor Cyan
Write-Host "La aplicación estará disponible en: http://localhost:$port" -ForegroundColor Yellow
Write-Host ""
Write-Host "Credenciales de acceso:" -ForegroundColor Cyan
Write-Host "  Usuario: pizzeria" -ForegroundColor White
Write-Host "  Contraseña: pizzeria" -ForegroundColor White
Write-Host ""
Write-Host "Presione Ctrl+C para detener el servidor" -ForegroundColor Yellow
Write-Host ""

# Cambiar al directorio del servidor y ejecutar
Set-Location "$PSScriptRoot\Comercio.NET.Pizzeria.Server"
dotnet run
