# ============================================================
# Script para publicar la aplicacion antes de instalar
# Ejecutar desde la carpeta raiz del proyecto
# ============================================================
param(
    [string]$OutputDir = ".\Comercio.NET.Mobile\Instalacion\publish"
)

Write-Host "Publicando Comercio.NET Web..." -ForegroundColor Cyan
Write-Host ""

# Limpiar carpeta publish anterior
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}

# Publicar
dotnet publish ".\Comercio.NET.Mobile\Comercio.NET.Mobile.Server\Comercio.NET.Mobile.Server.csproj" `
    -c Release `
    -o $OutputDir `
    --self-contained false

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Publicacion exitosa!" -ForegroundColor Green
    Write-Host "Archivos en: $OutputDir" -ForegroundColor Green
    Write-Host ""
    Write-Host "Proximo paso:" -ForegroundColor Yellow
    Write-Host "  Copie la carpeta 'Instalacion' completa al servidor del cliente" -ForegroundColor White
    Write-Host "  y ejecute 'instalar-comercio-web.ps1' como Administrador." -ForegroundColor White
} else {
    Write-Host "Error al publicar. Revise los errores anteriores." -ForegroundColor Red
}
