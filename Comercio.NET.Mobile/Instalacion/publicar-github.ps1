#Requires -Version 5.1
<#
.SYNOPSIS
    Sube el ZIP del instalador web al release de GitHub existente.

.NOTES
    Requiere un Personal Access Token de GitHub con permisos de repo.
    El token se puede guardar en variable de entorno GITHUB_TOKEN
    o ingresarlo cuando el script lo solicite.
#>

$REPO        = "manuclaro/Comercio.NET-web"
$RELEASE_TAG = "v1.7.0"
$ZIP_NAME    = "ComercioNET-Web-Instalacion.zip"

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$instalDir   = Split-Path -Parent $scriptDir   # sube un nivel desde Scripts\ o usa la raiz
$zipOut      = Join-Path $env:TEMP $ZIP_NAME

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  PUBLICAR INSTALADOR WEB EN GITHUB" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Repositorio : $REPO" -ForegroundColor Gray
Write-Host "  Release     : $RELEASE_TAG" -ForegroundColor Gray
Write-Host "  Archivo     : $ZIP_NAME" -ForegroundColor Gray
Write-Host ""

# ---------------------------------------------------------------------------
# TOKEN
# ---------------------------------------------------------------------------
$token = $env:GITHUB_TOKEN
if ([string]::IsNullOrWhiteSpace($token)) {
    $token = Read-Host "  Token de GitHub (Personal Access Token)"
}
if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host "  ERROR: Se requiere un token de GitHub." -ForegroundColor Red
    Read-Host "ENTER para salir"
    exit 1
}

$headers = @{
    "Authorization" = "token $token"
    "User-Agent"    = "ComercioNET-Publisher"
    "Accept"        = "application/vnd.github.v3+json"
}

# ---------------------------------------------------------------------------
# GENERAR ZIP DEL INSTALADOR
# ---------------------------------------------------------------------------
Write-Host "  Generando ZIP del instalador web..." -ForegroundColor Yellow

$instalacionDir = Join-Path $scriptDir "Instalacion"
if (-not (Test-Path $instalacionDir)) {
    # Buscar la carpeta Instalacion relativa a este script
    $instalacionDir = Join-Path (Split-Path -Parent $scriptDir) "Comercio.NET.Mobile\Instalacion"
}

if (-not (Test-Path $instalacionDir)) {
    Write-Host "  ERROR: No se encontro la carpeta Instalacion." -ForegroundColor Red
    Write-Host "  Ruta buscada: $instalacionDir" -ForegroundColor Yellow
    Read-Host "ENTER para salir"
    exit 1
}

# Archivos a incluir en el ZIP (excluir archivos obsoletos)
$excluir = @("backup-comercio.sql", "crear-base-datos.sql", "instalar-manual.ps1",
             "instalar-comercio-web.ps1", "publicar.ps1", "*.bak")

if (Test-Path $zipOut) { Remove-Item $zipOut -Force }

Write-Host "  Comprimiendo desde: $instalacionDir" -ForegroundColor Gray

# Crear ZIP filtrando archivos obsoletos
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($zipOut, 'Create')

$archivos = Get-ChildItem $instalacionDir -Recurse -File | Where-Object {
    $nombre = $_.Name
    $excluir -notcontains $nombre -and $nombre -notlike "*.bak"
}

foreach ($archivo in $archivos) {
    $entryName = $archivo.FullName.Substring($instalacionDir.Length + 1)
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $archivo.FullName, $entryName) | Out-Null
}
$zip.Dispose()

$sizeMB = [math]::Round((Get-Item $zipOut).Length / 1MB, 1)
Write-Host "  OK  ZIP generado: $ZIP_NAME ($sizeMB MB)" -ForegroundColor Green
Write-Host ""

# ---------------------------------------------------------------------------
# OBTENER EL RELEASE
# ---------------------------------------------------------------------------
Write-Host "  Buscando release $RELEASE_TAG en GitHub..." -ForegroundColor Yellow

try {
    $releaseUrl = "https://api.github.com/repos/$REPO/releases/tags/$RELEASE_TAG"
    $release = Invoke-RestMethod -Uri $releaseUrl -Headers $headers -Method Get
    Write-Host "  OK  Release encontrado: $($release.name)" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: No se encontro el release '$RELEASE_TAG'." -ForegroundColor Red
    Write-Host "  Verifique que el tag exista en: https://github.com/$REPO/releases" -ForegroundColor Yellow
    Read-Host "ENTER para salir"
    exit 1
}

# ---------------------------------------------------------------------------
# ELIMINAR ASSET ANTERIOR SI EXISTE
# ---------------------------------------------------------------------------
$assetExistente = $release.assets | Where-Object { $_.name -eq $ZIP_NAME }
if ($assetExistente) {
    Write-Host "  Eliminando version anterior del asset..." -ForegroundColor Gray
    try {
        Invoke-RestMethod -Uri "https://api.github.com/repos/$REPO/releases/assets/$($assetExistente.id)" `
            -Headers $headers -Method Delete | Out-Null
        Write-Host "  OK  Asset anterior eliminado." -ForegroundColor Green
    } catch {
        Write-Host "  !!  No se pudo eliminar el asset anterior: $_" -ForegroundColor Magenta
    }
}

# ---------------------------------------------------------------------------
# SUBIR EL NUEVO ZIP
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "  Subiendo $ZIP_NAME a GitHub..." -ForegroundColor Yellow
Write-Host "  (Puede demorar dependiendo del tamaño y la conexion)" -ForegroundColor Gray

$uploadUrl = $release.upload_url -replace "\{\?name,label\}", "?name=$ZIP_NAME"

try {
    $uploadHeaders = $headers + @{ "Content-Type" = "application/zip" }
    $resultado = Invoke-RestMethod -Uri $uploadUrl -Headers $uploadHeaders `
        -Method Post -InFile $zipOut

    Write-Host ""
    Write-Host "  OK  Archivo subido correctamente!" -ForegroundColor Green
    Write-Host "  URL de descarga:" -ForegroundColor White
    Write-Host "  $($resultado.browser_download_url)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Para instalar en el cliente ejecutar:" -ForegroundColor White
    Write-Host "  irm '$($resultado.browser_download_url)' -OutFile C:\inst.zip; Expand-Archive C:\inst.zip C:\Instalacion -Force; PowerShell -ExecutionPolicy Bypass -File C:\Instalacion\instalar.ps1" -ForegroundColor Gray

} catch {
    Write-Host "  ERROR al subir el archivo: $_" -ForegroundColor Red
}

Remove-Item $zipOut -Force -ErrorAction SilentlyContinue

Write-Host ""
Read-Host "Presione ENTER para cerrar"
