# ?? Scripts de Administración SQLBridge

Esta carpeta contiene scripts útiles para la administración de subdominios y tunnels de Cloudflare.

---

## ?? Scripts Disponibles

### 1. `crear-subdominios-ejemplo.ps1`

**Propósito:** Crear subdominios automáticamente para múltiples clientes.

**Uso:**
```powershell
# Ejecutar desde PowerShell (como Administrador)
cd "C:\Users\Manuel\source\repos\Comercio .NET\docs\scripts"
.\crear-subdominios-ejemplo.ps1
```

**Qué hace:**
1. ? Verifica que cloudflared esté instalado
2. ? Autentica en Cloudflare (si es necesario)
3. ? Crea tunnels para cada cliente
4. ? Crea subdominios automáticamente
5. ? Lista todos los tunnels creados

**Personalizar:**
Edita la lista de clientes en el script:
```powershell
$clientes = @(
    @{Nombre="victor"; Descripcion="Comercio Victor"},
    @{Nombre="pepe"; Descripcion="Almacén Pepe"},
    @{Nombre="abc"; Descripcion="Supermercado ABC"}
)
```

---

## ??? Otros Scripts Útiles

### Listar Todos los Tunnels

```powershell
# Listar tunnels
C:\cloudflared\cloudflared.exe tunnel list
```

### Monitorear Clientes

```powershell
# Script de monitoreo (crear como monitoreo-clientes.ps1)
$clientes = @("victor", "pepe", "abc")

foreach ($cliente in $clientes) {
    $url = "https://$cliente.tpqsolutions.com.ar/health"
    try {
        $response = Invoke-RestMethod -Uri $url -TimeoutSec 5
        Write-Host "? $cliente - OK" -ForegroundColor Green
    }
    catch {
        Write-Host "? $cliente - ERROR" -ForegroundColor Red
    }
}
```

### Eliminar Tunnel

```powershell
# Eliminar un tunnel completo
$tunnelName = "sqlbridge-nombrecliente"
$subdominio = "nombrecliente.tpqsolutions.com.ar"

# 1. Desasociar DNS
C:\cloudflared\cloudflared.exe tunnel route dns delete $tunnelName $subdominio

# 2. Eliminar tunnel
C:\cloudflared\cloudflared.exe tunnel delete $tunnelName
```

---

## ?? Documentación

- **[Guía Rápida Subdominios](../GUIA_RAPIDA_SUBDOMINIOS.md)** - Cómo crear subdominios manualmente
- **[Guía Instalación Completa](../GUIA_INSTALACION_CLIENTE_NUEVO.md)** - Instalación completa paso a paso
- **[Arquitectura Multi-Cliente](../ARQUITECTURA_MULTICLIENTE.md)** - Gestión de múltiples clientes

---

## ?? Requisitos

- Windows PowerShell 5.1+
- cloudflared instalado en `C:\cloudflared\cloudflared.exe`
- Acceso a la cuenta de Cloudflare (manuclaro@gmail.com)
- Dominio: tpqsolutions.com.ar

---

## ?? Tips

1. **Ejecución Remota:** Puedes ejecutar estos scripts desde tu máquina para crear los subdominios antes de ir al cliente.

2. **Automatización:** Considera usar Task Scheduler para ejecutar el script de monitoreo cada hora.

3. **Backup:** Guarda una copia de los archivos de credenciales (`.json`) de cada tunnel.

---

## ?? Seguridad

- ?? Los archivos de credenciales (`.json`) son sensibles
- ?? No los subas a repositorios públicos
- ?? Guárdalos en ubicación segura con backup

---

**Última actualización:** Febrero 2026
