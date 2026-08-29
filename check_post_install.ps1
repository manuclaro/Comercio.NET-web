#Requires -Version 5.1
param(
    [int]$PgPort = 5432,
    [string]$PgUser = "postgres",
    [string]$PgPassword = "michael",
    [string]$DbName = "comercio",
    [string]$InstallDir = "C:\Comercio.NET"
)

$ErrorActionPreference = "SilentlyContinue"

function Write-OK { param([string]$m) Write-Host "[OK]  $m" -ForegroundColor Green }
function Write-Warn { param([string]$m) Write-Host "[WARN] $m" -ForegroundColor Yellow }
function Write-Fail { param([string]$m) Write-Host "[FAIL] $m" -ForegroundColor Red }

$allOk = $true

$pgSvc = Get-Service -Name "postgresql*" | Sort-Object Name | Select-Object -First 1
if ($pgSvc -and $pgSvc.Status -eq 'Running') {
    Write-OK "Servicio PostgreSQL en ejecucion ($($pgSvc.Name))."
} else {
    Write-Fail "Servicio PostgreSQL detenido o no encontrado."
    $allOk = $false
}

$pgBinCandidates = @(
    "C:\Program Files\PostgreSQL\18\bin",
    "C:\Program Files\PostgreSQL\17\bin",
    "C:\Program Files\PostgreSQL\16\bin",
    "C:\Program Files\PostgreSQL\15\bin",
    "C:\Program Files\PostgreSQL\14\bin"
)

$pgBin = $null
foreach ($p in $pgBinCandidates) {
    if (Test-Path (Join-Path $p "psql.exe")) { $pgBin = $p; break }
}
if (-not $pgBin) {
    $cmd = Get-Command psql
    if ($cmd) { $pgBin = Split-Path $cmd.Source }
}

if (-not $pgBin) {
    Write-Fail "No se encontro psql.exe."
    $allOk = $false
} else {
    $psql = Join-Path $pgBin "psql.exe"
    $env:PGPASSWORD = $PgPassword

    $q1 = (& $psql -U $PgUser -p $PgPort -d postgres -tAc "SELECT 1;" 2>&1) -join ""
    if ($LASTEXITCODE -eq 0 -and $q1.Trim() -eq "1") {
        Write-OK "Conexion local a PostgreSQL correcta (SELECT 1)."
    } else {
        Write-Fail "Fallo conexion local a PostgreSQL. Detalle: $q1"
        $allOk = $false
    }

    $q2 = (& $psql -U $PgUser -p $PgPort -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$DbName';" 2>&1) -join ""
    if ($LASTEXITCODE -eq 0 -and $q2.Trim() -eq "1") {
        Write-OK "Base de datos '$DbName' existe."
    } else {
        Write-Fail "Base de datos '$DbName' no existe o no es accesible."
        $allOk = $false
    }

    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

try {
    $fw = netsh advfirewall firewall show rule name="PostgreSQL $PgPort" 2>$null
    if ($fw -match "PostgreSQL $PgPort") {
        Write-OK "Regla de firewall para puerto $PgPort encontrada."
    } else {
        Write-Warn "No se encontro regla de firewall PostgreSQL $PgPort."
    }
} catch {
    Write-Warn "No se pudo comprobar regla de firewall."
}

$appSettings = Join-Path $InstallDir "appsettings.json"
if (Test-Path $appSettings) {
    $raw = Get-Content $appSettings -Raw
    if ($raw -match '"DefaultConnection"\s*:\s*"Host=') {
        Write-OK "appsettings.json contiene DefaultConnection PostgreSQL."
    } else {
        Write-Warn "appsettings.json no parece tener cadena PostgreSQL valida."
    }
} else {
    Write-Warn "No se encontro appsettings.json en $InstallDir"
}

if ($allOk) {
    Write-Host ""; Write-Host "Resultado general: OK" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""; Write-Host "Resultado general: CON ERRORES" -ForegroundColor Red
    exit 1
}
