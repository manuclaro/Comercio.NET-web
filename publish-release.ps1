# ============================================================
# publish-release.ps1 — Comercio .NET
# 
# Uso: .\publish-release.ps1 -Version "1.6.0"
#      .\publish-release.ps1 -Version "1.6.0" -Mensaje "Correcciones de AFIP"
#
# Qué hace:
#   1. Actualiza la versión en el .csproj
#   2. Compila en Release
#   3. Crea el .zip listo para subir a GitHub
#   4. Muestra el checklist final para publicar el release
# ============================================================

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $false)]
    [string]$Mensaje = ""
)

$ErrorActionPreference = "Stop"

# ?? Configuración ??????????????????????????????????????????
$CsprojPath   = "$PSScriptRoot\Comercio .NET.csproj"
$PublishDir   = "$PSScriptRoot\bin\Release\net8.0-windows\publish"
$OutputDir    = "$PSScriptRoot\releases"
$ZipName      = "Comercio.NET_v$Version.zip"
$ZipPath      = "$OutputDir\$ZipName"
$MigrationsDir = "$PSScriptRoot\migrations"
# ???????????????????????????????????????????????????????????

function Write-Step($num, $texto) {
    Write-Host ""
    Write-Host "[$num] $texto" -ForegroundColor Cyan
}

function Write-OK($texto) {
    Write-Host "    OK: $texto" -ForegroundColor Green
}

function Write-Fail($texto) {
    Write-Host "    ERROR: $texto" -ForegroundColor Red
    exit 1
}

# Validar formato de versión
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Fail "Formato de version invalido: '$Version'. Use X.Y.Z (ej: 1.6.0)"
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Yellow
Write-Host "  PUBLICANDO COMERCIO .NET v$Version" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Yellow

# ?? PASO 1: Actualizar versión en .csproj ??????????????????
Write-Step 1 "Actualizando version en .csproj..."

$csproj = Get-Content $CsprojPath -Raw

# Leer versión actual para mostrarla
if ($csproj -match '<Version>(.+?)</Version>') {
    $versionAnterior = $Matches[1]
    Write-Host "    Anterior: $versionAnterior  ?  Nueva: $Version" -ForegroundColor Gray
}

$csproj = $csproj -replace '<Version>.+?</Version>',           "<Version>$Version</Version>"
$csproj = $csproj -replace '<AssemblyVersion>.+?</AssemblyVersion>', "<AssemblyVersion>$Version.0</AssemblyVersion>"
$csproj = $csproj -replace '<FileVersion>.+?</FileVersion>',   "<FileVersion>$Version.0</FileVersion>"

Set-Content $CsprojPath $csproj -Encoding UTF8
Write-OK ".csproj actualizado a v$Version"

# Actualizar también version.txt en la raíz del proyecto (se copiará al bin\ en el próximo build)
Set-Content "$PSScriptRoot\version.txt" $Version -Encoding UTF8 -NoNewline
Write-OK "version.txt actualizado a v$Version"

# ?? PASO 2: Compilar en Release ????????????????????????????
Write-Step 2 "Compilando en Release (dotnet publish)..."

$publishArgs = @(
    "publish"
    "`"$CsprojPath`""
    "-c", "Release"
    "-r", "win-x64"
    "--self-contained", "false"
    "-o", "`"$PublishDir`""
    "/p:DebugType=None"
    "/p:DebugSymbols=false"
)

$result = & dotnet @publishArgs 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host $result -ForegroundColor Red
    Write-Fail "La compilacion fallo. Revise los errores anteriores."
}

Write-OK "Compilacion exitosa en: $PublishDir"

# ?? PASO 3: Crear carpeta releases\ si no existe ??????????
Write-Step 3 "Preparando archivo .zip..."

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Eliminar zip anterior si existe
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

# Copiar scripts de migración al publish si existen
$migrationsToCopy = Get-ChildItem "$MigrationsDir\migrate_*.sql" -ErrorAction SilentlyContinue
if ($migrationsToCopy.Count -gt 0) {
    $publishMigrations = "$PublishDir\migrations"
    New-Item -ItemType Directory -Force -Path $publishMigrations | Out-Null
    foreach ($m in $migrationsToCopy) {
        Copy-Item $m.FullName -Destination $publishMigrations -Force
        Write-Host "    + Migracion incluida: $($m.Name)" -ForegroundColor Gray
    }
}

# Escribir version.txt en el publish (lo leerá la app después de actualizarse)
Set-Content "$PublishDir\version.txt" $Version -Encoding UTF8 -NoNewline
Write-Host "    + version.txt incluido: $Version" -ForegroundColor Gray

# Crear el .zip
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath -Force
Write-OK "ZIP creado: $ZipName ($zipSize MB)"
Write-OK "Ruta: $ZipPath"

# ?? PASO 4: Checklist final ????????????????????????????????
Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  COMPILACION EXITOSA - v$Version lista para publicar" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Pasos finales en GitHub:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  1. Ir a: https://github.com/manuclaro/Comercio.NET-web/releases/new"
Write-Host "  2. Tag:             v$Version"
Write-Host "  3. Release title:   Comercio .NET v$Version"

$bodyRelease = "## Comercio .NET v$Version`n`n"
if ($migrationsToCopy.Count -gt 0) {
    $bodyRelease += "[DB-MIGRATION]`n`n"
    Write-Host "  4. Body (incluye DB-MIGRATION):" -ForegroundColor Yellow
} else {
    Write-Host "  4. Body:" -ForegroundColor Yellow
}

if ($Mensaje -ne "") {
    $bodyRelease += "- $Mensaje`n"
}

Write-Host ""
Write-Host "?????????????????????????????????????" -ForegroundColor Gray
Write-Host $bodyRelease
Write-Host "?????????????????????????????????????" -ForegroundColor Gray
Write-Host ""
Write-Host "  5. Adjuntar el archivo: $ZipPath"
Write-Host "  6. Publicar el release"
Write-Host ""

# Abrir el explorador en la carpeta releases\
explorer $OutputDir
