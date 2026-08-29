# Script de Ejemplo: Crear Subdominios para Clientes
# Archivo: crear-subdominios-ejemplo.ps1
# 
# Este script muestra cómo crear subdominios en tpqsolutions.com.ar
# para diferentes clientes usando Cloudflare Tunnel

# ============================================
# CONFIGURACIÓN INICIAL (Solo una vez)
# ============================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CREAR SUBDOMINIOS PARA CLIENTES" -ForegroundColor Cyan
Write-Host "  Dominio: tpqsolutions.com.ar" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar que cloudflared esté instalado
if (-not (Test-Path "C:\cloudflared\cloudflared.exe")) {
    Write-Host "? ERROR: cloudflared no está instalado en C:\cloudflared\" -ForegroundColor Red
    Write-Host "   Descárgalo de: https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/install-and-setup/installation/" -ForegroundColor Yellow
    exit 1
}

# ============================================
# PASO 1: AUTENTICARSE EN CLOUDFLARE
# ============================================
Write-Host "PASO 1: Autenticarse en Cloudflare" -ForegroundColor Green
Write-Host "---------------------------------------" -ForegroundColor Gray
Write-Host ""
Write-Host "??  IMPORTANTE: Solo necesitas hacer esto UNA VEZ en tu máquina" -ForegroundColor Yellow
Write-Host ""
Write-Host "Se abrirá un navegador. Debes:" -ForegroundColor White
Write-Host "  1. Iniciar sesión con: manuclaro@gmail.com" -ForegroundColor White
Write-Host "  2. Seleccionar el dominio: tpqsolutions.com.ar" -ForegroundColor White
Write-Host ""

$respuesta = Read-Host "¿Ya te autenticaste anteriormente? (s/n)"

if ($respuesta -eq "n" -or $respuesta -eq "N") {
    Write-Host "Ejecutando: cloudflared tunnel login..." -ForegroundColor Cyan
    C:\cloudflared\cloudflared.exe tunnel login

    Write-Host ""
    Write-Host "? Autenticación completada" -ForegroundColor Green
    Write-Host "   Certificado guardado en: $env:USERPROFILE\.cloudflared\cert.pem" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "? Usando certificado existente" -ForegroundColor Green
    Write-Host ""
}

# ============================================
# PASO 2: CREAR TUNNELS Y SUBDOMINIOS
# ============================================
Write-Host ""
Write-Host "PASO 2: Crear Tunnels y Subdominios" -ForegroundColor Green
Write-Host "---------------------------------------" -ForegroundColor Gray
Write-Host ""

# Lista de clientes
$clientes = @(
    @{Nombre="victor"; Descripcion="Comercio Victor"},
    @{Nombre="pepe"; Descripcion="Almacén Pepe"},
    @{Nombre="abc"; Descripcion="Supermercado ABC"}
)

Write-Host "Se crearán subdominios para $($clientes.Count) clientes:" -ForegroundColor White
foreach ($cliente in $clientes) {
    Write-Host "  • $($cliente.Nombre).tpqsolutions.com.ar - $($cliente.Descripcion)" -ForegroundColor Cyan
}
Write-Host ""

$respuesta = Read-Host "¿Deseas continuar? (s/n)"

if ($respuesta -ne "s" -and $respuesta -ne "S") {
    Write-Host "? Cancelado por el usuario" -ForegroundColor Yellow
    exit 0
}

Write-Host ""

# Crear tunnel y subdominio para cada cliente
foreach ($cliente in $clientes) {
    $tunnelName = "sqlbridge-$($cliente.Nombre)"
    $subdominio = "$($cliente.Nombre).tpqsolutions.com.ar"

    Write-Host "????????????????????????????????????????" -ForegroundColor DarkGray
    Write-Host "Cliente: $($cliente.Descripcion)" -ForegroundColor Yellow
    Write-Host "????????????????????????????????????????" -ForegroundColor DarkGray
    Write-Host ""

    # Paso 2.1: Crear el tunnel
    Write-Host "  ? Creando tunnel: $tunnelName..." -ForegroundColor Cyan

    $output = C:\cloudflared\cloudflared.exe tunnel create $tunnelName 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ? Tunnel creado exitosamente" -ForegroundColor Green

        # Extraer el Tunnel ID de la salida
        $tunnelIdMatch = $output | Select-String -Pattern "[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}"
        if ($tunnelIdMatch) {
            $tunnelId = $tunnelIdMatch.Matches[0].Value
            Write-Host "  ?? Tunnel ID: $tunnelId" -ForegroundColor Gray
        }
    } else {
        Write-Host "  ??  El tunnel ya existe (esto es normal)" -ForegroundColor Yellow
    }

    Write-Host ""

    # Paso 2.2: Crear el subdominio (asociar DNS al tunnel)
    Write-Host "  ? Creando subdominio: $subdominio..." -ForegroundColor Cyan

    $output = C:\cloudflared\cloudflared.exe tunnel route dns $tunnelName $subdominio 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ? Subdominio creado exitosamente" -ForegroundColor Green
        Write-Host "  ?? URL: https://$subdominio" -ForegroundColor Cyan
    } else {
        if ($output -like "*already exists*") {
            Write-Host "  ??  El subdominio ya existe (esto es normal)" -ForegroundColor Yellow
            Write-Host "  ?? URL: https://$subdominio" -ForegroundColor Cyan
        } else {
            Write-Host "  ? ERROR al crear subdominio: $output" -ForegroundColor Red
        }
    }

    Write-Host ""
}

# ============================================
# PASO 3: VERIFICAR LOS SUBDOMINIOS CREADOS
# ============================================
Write-Host ""
Write-Host "PASO 3: Verificar Subdominios Creados" -ForegroundColor Green
Write-Host "---------------------------------------" -ForegroundColor Gray
Write-Host ""

Write-Host "Listando todos los tunnels..." -ForegroundColor Cyan
Write-Host ""

C:\cloudflared\cloudflared.exe tunnel list

Write-Host ""
Write-Host "????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "? PROCESO COMPLETADO" -ForegroundColor Green
Write-Host "????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Subdominios creados:" -ForegroundColor Yellow
foreach ($cliente in $clientes) {
    Write-Host "  • https://$($cliente.Nombre).tpqsolutions.com.ar" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Verificación en Cloudflare:" -ForegroundColor Yellow
Write-Host "  1. Ve a: https://dash.cloudflare.com" -ForegroundColor White
Write-Host "  2. Selecciona: tpqsolutions.com.ar" -ForegroundColor White
Write-Host "  3. Ve a: DNS ? Records" -ForegroundColor White
Write-Host "  4. Deberías ver registros CNAME para: victor, pepe, abc" -ForegroundColor White

Write-Host ""
Write-Host "Próximos pasos:" -ForegroundColor Yellow
Write-Host "  1. En la PC de cada cliente, crear el archivo config.yml" -ForegroundColor White
Write-Host "  2. Instalar SQLBridge como servicio" -ForegroundColor White
Write-Host "  3. Iniciar el servicio cloudflared" -ForegroundColor White
Write-Host "  4. Probar: https://nombrecliente.tpqsolutions.com.ar/health" -ForegroundColor White

Write-Host ""
Write-Host "?? Documentación completa en:" -ForegroundColor Cyan
Write-Host "   docs/GUIA_INSTALACION_CLIENTE_NUEVO.md" -ForegroundColor Gray
Write-Host "   docs/ARQUITECTURA_MULTICLIENTE.md" -ForegroundColor Gray
Write-Host ""
