# ? Guía Rápida: Configuración en PC de Victor

**Ya tienes:** Tunnel creado (`sqlbridge-victor`) y DNS configurado (`victor.tpqsolutions.com.ar`)

**Ahora necesitas:** Configurar la PC de Victor para que el subdominio apunte a su SQL Server local

---

## ?? Resumen: 5 Pasos

```
1. Instalar dependencias     ? 10 min
2. Copiar SQLBridge          ? 2 min
3. Copiar credenciales       ? 1 min
4. Configurar archivos       ? 2 min
5. Instalar servicios        ? 3 min
?????????????????????????????????????
TOTAL:                         ~18 min
```

---

## ?? PASO A PASO SIMPLIFICADO

### 1?? En la PC de Victor - Instalar Dependencias

```powershell
# Descargar e instalar:
# 1. .NET 8 Runtime Hosting Bundle
#    https://dotnet.microsoft.com/download/dotnet/8.0

# 2. NSSM (extraer a C:\nssm\)
#    https://nssm.cc/download

# 3. cloudflared (guardar en C:\cloudflared\)
#    https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/install-and-setup/installation/
```

---

### 2?? Copiar SQLBridge

```powershell
# Copiar los archivos de SQLBridge a C:\SqlBridge\
# (Traer el ZIP desde tu máquina o compilar in situ)

New-Item -Path "C:\SqlBridge" -ItemType Directory -Force
# Descomprimir SqlBridge.zip en C:\SqlBridge\
```

---

### 3?? Copiar Archivo de Credenciales

**En tu máquina (donde creaste el tunnel):**

```powershell
# Buscar el archivo de credenciales
dir "$env:USERPROFILE\.cloudflared\*.json"

# Verás algo como:
# f4f26148-1585-4ec7-8b48-XXXXXXXX.json

# Copiar este archivo a USB o transferir por red
```

**En la PC de Victor:**

```powershell
# Crear directorio
New-Item -Path "$env:USERPROFILE\.cloudflared" -ItemType Directory -Force

# Copiar el archivo desde USB
Copy-Item "D:\f4f26148-1585-4ec7-8b48-XXXXXXXX.json" "$env:USERPROFILE\.cloudflared\"
```

**?? Nota:** El nombre exacto está en Cloudflare Dashboard ? Tunnels ? sqlbridge-victor ? "Tunnel ID"

---

### 4?? Configurar Archivos

#### A) Crear `C:\cloudflared\config.yml`

```yaml
tunnel: f4f26148-1585-4ec7-8b48-XXXXXXXX
credentials-file: C:\Users\Victor\.cloudflared\f4f26148-1585-4ec7-8b48-XXXXXXXX.json

ingress:
  - hostname: victor.tpqsolutions.com.ar
    service: http://localhost:5000
  - service: http_status:404
```

**Reemplazar:**
- `f4f26148-1585-4ec7-8b48-XXXXXXXX` ? Tu Tunnel ID completo
- `Victor` ? Nombre del usuario de Windows en la PC de Victor

#### B) Editar `C:\SqlBridge\appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=192.168.1.100,1433;Database=Comercio;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;Encrypt=False;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Reemplazar:**
- `192.168.1.100` ? IP del SQL Server de Victor
- `Comercio` ? Nombre de la base de datos
- `sa` ? Usuario SQL
- `TU_PASSWORD` ? Contraseña SQL

---

### 5?? Instalar y Probar

#### A) Probar Manualmente (Opcional pero Recomendado)

```powershell
# Terminal 1: Iniciar SQLBridge
cd C:\SqlBridge
C:\"Program Files"\dotnet\dotnet.exe Comercio.NET.SqlBridge.Server.dll

# Terminal 2: Iniciar Tunnel
C:\cloudflared\cloudflared.exe tunnel --config C:\cloudflared\config.yml run

# Terminal 3: Probar
curl http://localhost:5000/health
curl https://victor.tpqsolutions.com.ar/health
```

#### B) Instalar como Servicios

```powershell
# SQLBridge
C:\nssm\nssm.exe install SqlBridgeWeb "C:\Program Files\dotnet\dotnet.exe" "C:\SqlBridge\Comercio.NET.SqlBridge.Server.dll"
C:\nssm\nssm.exe set SqlBridgeWeb AppDirectory "C:\SqlBridge"
C:\nssm\nssm.exe set SqlBridgeWeb Start SERVICE_AUTO_START
C:\nssm\nssm.exe start SqlBridgeWeb

# Cloudflare Tunnel
C:\cloudflared\cloudflared.exe service install
Set-Service -Name cloudflared -StartupType Automatic
Start-Service cloudflared
```

---

## ? Verificación

```powershell
# 1. Local
curl http://localhost:5000/health

# 2. Público
curl https://victor.tpqsolutions.com.ar/health

# 3. Servicios
Get-Service SqlBridgeWeb, cloudflared
# Ambos deben estar "Running"
```

---

## ?? ALTERNATIVA: Script Automático

**Usa el script de configuración:**

```powershell
# Descargar el script a la PC de Victor
# Ejecutar como Administrador:

.\configurar-cliente.ps1 -TunnelId "f4f26148-1585-4ec7-8b48-XXXXXXXX" -ClienteName "victor" -SqlServerIp "192.168.1.100" -DatabaseName "Comercio"
```

**El script hace automáticamente:**
- ? Crea directorios
- ? Genera config.yml
- ? Configura appsettings.json (pide credenciales SQL)
- ? Prueba SQLBridge
- ? Instala servicios
- ? Inicia servicios
- ? Verifica health checks

**Documentación:** `docs/scripts/configurar-cliente.ps1`

---

## ?? Arquitectura Final

```
Internet
    ?
victor.tpqsolutions.com.ar (Cloudflare)
    ?
Tunnel: sqlbridge-victor
    ?
PC de Victor
    ?? cloudflared (lee config.yml + credenciales.json)
    ?       ?
    ?? SQLBridge (puerto 5000)
            ?
        SQL Server (192.168.1.100:1433)
```

---

## ?? Documentación Completa

- **Guía Detallada:** `docs/CONFIGURACION_PC_VICTOR.md`
- **Script Automático:** `docs/scripts/configurar-cliente.ps1`
- **Troubleshooting:** `docs/GUIA_INSTALACION_CLIENTE_NUEVO.md` (sección Troubleshooting)

---

## ?? Próximo Paso

**Configurar Railway:**

Una vez que `https://victor.tpqsolutions.com.ar/health` funcione, configura la variable en Railway:

```bash
SQL_BRIDGE_URL=https://victor.tpqsolutions.com.ar
```

Y ya podrás usar la app móvil con los datos de Victor.

---

**?? Tiempo total: ~20 minutos**

**? ¡Listo!**
