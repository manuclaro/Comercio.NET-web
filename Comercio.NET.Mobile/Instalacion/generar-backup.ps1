#Requires -Version 5.1
<#
.SYNOPSIS
    Genera el backup de la base de datos para distribuir con el instalador.
    Usa pg_dump version 16 para garantizar compatibilidad con el instalador.

.NOTES
    Ejecutar desde la PC de desarrollo ANTES de distribuir el instalador.
    Genera: backup-comercio.dump en la misma carpeta que este script.
#>

$ErrorActionPreference = "Stop"

$DB_HOST     = "localhost"
$DB_PORT     = "5433"
$DB_NAME     = "comercio"
$DB_USER     = "postgres"

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputFile  = Join-Path $scriptDir "backup-comercio.sql"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  GENERADOR DE BACKUP - Comercio.NET" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# Buscar pg_dump — cualquier version sirve porque generamos SQL plano
$pgdump = $null
foreach ($v in @("18","17","16","15","14","13")) {
    $p = "C:\Program Files\PostgreSQL\$v\bin\pg_dump.exe"
    if (Test-Path $p) { $pgdump = $p; break }
}
if (-not $pgdump) {
    $found = Get-Command pg_dump -ErrorAction SilentlyContinue
    if ($found) { $pgdump = $found.Source }
}

if (-not $pgdump) {
    Write-Host "  ERROR: No se encontro pg_dump.exe" -ForegroundColor Red
    Read-Host "ENTER para salir"
    exit 1
}

# Mostrar version que se va a usar
$versionInfo = & "$pgdump" --version 2>$null
Write-Host "  pg_dump: $versionInfo" -ForegroundColor Gray
Write-Host "  Base   : $DB_NAME @ ${DB_HOST}:${DB_PORT}" -ForegroundColor Gray
Write-Host "  Salida : $outputFile" -ForegroundColor Gray
Write-Host ""

# Solicitar contraseña
$pgPass = Read-Host "  Contrasena de PostgreSQL local (Enter para 'michael')"
if ([string]::IsNullOrWhiteSpace($pgPass)) { $pgPass = "michael" }

Write-Host ""
Write-Host "  Generando backup..." -ForegroundColor Yellow

$env:PGPASSWORD = $pgPass

& "$pgdump" `
    -h $DB_HOST -p $DB_PORT -U $DB_USER `
    --no-owner --no-privileges --no-reconnect `
    -F p `
    -f $outputFile `
    $DB_NAME

$exitCode = $LASTEXITCODE
$env:PGPASSWORD = ""

if ($exitCode -eq 0 -and (Test-Path $outputFile)) {
    $size = [math]::Round((Get-Item $outputFile).Length / 1KB)
    Write-Host ""
    Write-Host "  OK  Backup generado correctamente ($size KB)" -ForegroundColor Green
    Write-Host "  >>  Archivo: $outputFile" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Ahora puede distribuir la carpeta Instalacion al cliente." -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "  ERROR: pg_dump fallo (codigo $exitCode)" -ForegroundColor Red
    Write-Host "  Verifique que PostgreSQL este corriendo y la contrasena sea correcta." -ForegroundColor Yellow
}

Write-Host ""
Read-Host "Presione ENTER para cerrar"
