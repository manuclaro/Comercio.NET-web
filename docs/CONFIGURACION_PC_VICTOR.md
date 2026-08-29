# ?? Configuración en la PC del Cliente Victor

**Tunnel creado:** `sqlbridge-victor`  
**Subdominio:** `victor.tpqsolutions.com.ar`  
**Tunnel ID:** `f4f26148-1585-4ec7-8b48-...` (visible en tu captura)

---

## ? Resumen Rápido (15 minutos)

En la PC de Victor necesitas:
1. ? Instalar dependencias (.NET 8, NSSM, cloudflared)
2. ? Instalar SQLBridge
3. ? Configurar el tunnel con el archivo `config.yml`
4. ? Iniciar los servicios

---

## ?? PASO 1: Instalar Dependencias

### 1.1 - Instalar .NET 8 Runtime

```powershell
# Descargar de: https://dotnet.microsoft.com/download/dotnet/8.0
# Instalar: ASP.NET Core Runtime 8.0.x (Hosting Bundle)

# Verificar instalación
dotnet --version
# Debe mostrar: 8.0.x
```

### 1.2 - Instalar NSSM

```powershell
# Crear directorio
New-Item -Path "C:\nssm" -ItemType Directory -Force

# Descargar NSSM de: https://nssm.cc/download
# Extraer nssm.exe (win64) a C:\nssm\

# Verificar
C:\nssm\nssm.exe version
```

### 1.3 - Instalar cloudflared

```powershell
# Crear directorio
New-Item -Path "C:\cloudflared" -ItemType Directory -Force

# Descargar cloudflared de:
# https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/install-and-setup/installation/

# Guardar cloudflared.exe en C:\cloudflared\

# Verificar
C:\cloudflared\cloudflared.exe --version
```

---

## ??? PASO 2: Instalar SQLBridge

### 2.1 - Crear Directorio

```powershell
# Crear directorio principal
New-Item -Path "C:\SqlBridge" -ItemType Directory -Force

# Crear subdirectorio para logs
New-Item -Path "C:\SqlBridge\logs" -ItemType Directory -Force
```

### 2.2 - Copiar Archivos de SQLBridge

**Opción A: Desde ZIP**
```powershell
# Copiar SqlBridge.zip al escritorio de Victor
# Luego descomprimir:
$zipPath = "$env:USERPROFILE\Desktop\SqlBridge.zip"
Expand-Archive -Path $zipPath -DestinationPath "C:\SqlBridge" -Force
```

**Opción B: Desde tu máquina de desarrollo**
```powershell
# En tu máquina:
cd "C:\Users\Manuel\source\repos\Comercio .NET\..\Comercio.NET.SqlBridge\Comercio.NET.SqlBridge.Server"
dotnet publish -c Release -o publish
Compress-Archive -Path "publish\*" -DestinationPath "$env:USERPROFILE\Desktop\SqlBridge-Victor.zip" -Force

# Transferir el ZIP a la PC de Victor
```

### 2.3 - Configurar Connection String

```powershell
# Editar appsettings.json
notepad C:\SqlBridge\appsettings.json
```

**Contenido (ajustar según los datos de Victor):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=192.168.1.100,1433;Database=Comercio;User Id=sa;Password=PasswordVictor123;TrustServerCertificate=True;Encrypt=False;"
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

**?? Reemplazar:**
- `192.168.1.100` ? IP real del SQL Server de Victor
- `Comercio` ? Nombre de la base de datos
- `sa` ? Usuario de SQL Server
- `PasswordVictor123` ? Contraseña de SQL Server

**Guardar el archivo.**

### 2.4 - Probar SQLBridge Manualmente

```powershell
# Ejecutar manualmente para verificar
cd C:\SqlBridge
C:\"Program Files"\dotnet\dotnet.exe Comercio.NET.SqlBridge.Server.dll

# Debe mostrar:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: http://0.0.0.0:5000
```

**En otra terminal PowerShell:**
```powershell
# Probar health check
curl http://localhost:5000/health

# Debe responder: {"status":"ok","timestamp":"..."}
```

**Si funciona, presionar CTRL+C para detener y continuar.**

---

## ?? PASO 3: Configurar Cloudflare Tunnel

### 3.1 - Copiar el Archivo de Credenciales

**?? IMPORTANTE:** Necesitas el archivo de credenciales del tunnel que creaste.

**En tu máquina (donde creaste el tunnel):**
```powershell
# El archivo está en:
# C:\Users\Manuel\.cloudflared\f4f26148-1585-4ec7-8b48-XXXXXXXX.json

# Copiarlo a un USB o transferirlo a la PC de Victor
```

**En la PC de Victor:**
```powershell
# Crear directorio .cloudflared
New-Item -Path "$env:USERPROFILE\.cloudflared" -ItemType Directory -Force

# Copiar el archivo de credenciales
# Ejemplo: copiar desde USB
Copy-Item "D:\f4f26148-1585-4ec7-8b48-XXXXXXXX.json" "$env:USERPROFILE\.cloudflared\"
```

**?? Nota:** El nombre exacto del archivo lo puedes ver ejecutando en tu máquina:
```powershell
C:\cloudflared\cloudflared.exe tunnel info sqlbridge-victor
```

### 3.2 - Crear config.yml

```powershell
# Crear el archivo de configuración
notepad C:\cloudflared\config.yml
```

**Contenido:**
```yaml
tunnel: f4f26148-1585-4ec7-8b48-XXXXXXXX
credentials-file: C:\Users\Victor\.cloudflared\f4f26148-1585-4ec7-8b48-XXXXXXXX.json

ingress:
  - hostname: victor.tpqsolutions.com.ar
    service: http://localhost:5000
  - service: http_status:404
```

**?? Reemplazar:**
- `f4f26148-1585-4ec7-8b48-XXXXXXXX` ? Tu Tunnel ID completo (visible en Cloudflare)
- `Victor` ? Nombre real del usuario de Windows en la PC de Victor
- El nombre del archivo `.json` debe coincidir con el que copiaste

**Guardar el archivo.**

### 3.3 - Probar el Tunnel Manualmente

```powershell
# En una terminal, tener SQLBridge corriendo
cd C:\SqlBridge
C:\"Program Files"\dotnet\dotnet.exe Comercio.NET.SqlBridge.Server.dll

# En OTRA terminal, iniciar el tunnel
C:\cloudflared\cloudflared.exe tunnel --config C:\cloudflared\config.yml run

# Debe mostrar:
# INF Connection registered connIndex=0 location=EZE
# INF Connection registered connIndex=1 location=EZE
```

**Probar desde Internet (desde tu celular o tu PC):**
```
https://victor.tpqsolutions.com.ar/health
```

**Debe responder:** `{"status":"ok","timestamp":"..."}`

**Si funciona, presionar CTRL+C en ambas terminales y continuar.**

---

## ?? PASO 4: Instalar como Servicios de Windows

### 4.1 - Instalar SQLBridge como Servicio

```powershell
# Instalar servicio con NSSM
C:\nssm\nssm.exe install SqlBridgeWeb "C:\Program Files\dotnet\dotnet.exe" "C:\SqlBridge\Comercio.NET.SqlBridge.Server.dll"

# Configurar directorio de trabajo
C:\nssm\nssm.exe set SqlBridgeWeb AppDirectory "C:\SqlBridge"

# Configurar inicio automático
C:\nssm\nssm.exe set SqlBridgeWeb Start SERVICE_AUTO_START

# Configurar descripción
C:\nssm\nssm.exe set SqlBridgeWeb Description "SQL Bridge para aplicación móvil Comercio.NET"

# Configurar logs (opcional)
C:\nssm\nssm.exe set SqlBridgeWeb AppStdout "C:\SqlBridge\logs\nssm-stdout.log"
C:\nssm\nssm.exe set SqlBridgeWeb AppStderr "C:\SqlBridge\logs\nssm-stderr.log"

# Iniciar el servicio
C:\nssm\nssm.exe start SqlBridgeWeb
```

### 4.2 - Instalar Cloudflare Tunnel como Servicio

```powershell
# Instalar como servicio (cloudflared lo hace automáticamente)
C:\cloudflared\cloudflared.exe service install

# Configurar inicio automático
Set-Service -Name cloudflared -StartupType Automatic

# Iniciar el servicio
Start-Service cloudflared
```

### 4.3 - Verificar Servicios

```powershell
# Verificar SQLBridge
C:\nssm\nssm.exe status SqlBridgeWeb
# Debe mostrar: SERVICE_RUNNING

# Verificar Cloudflare
Get-Service cloudflared
# Debe mostrar: Running
```

---

## ? PASO 5: Verificación Final

### 5.1 - Health Check Local

```powershell
curl http://localhost:5000/health
```

**Resultado esperado:**
```json
{"status":"ok","timestamp":"2026-02-10T23:45:00"}
```

### 5.2 - Health Check Público

Desde cualquier navegador (o el móvil):
```
https://victor.tpqsolutions.com.ar/health
```

**Resultado esperado:**
```json
{"status":"ok","timestamp":"2026-02-10T23:45:00"}
```

### 5.3 - Probar Query

```powershell
# Crear archivo de prueba
$body = @{
    query = "SELECT TOP 5 * FROM Facturas ORDER BY Fecha DESC"
    parameters = @{}
} | ConvertTo-Json

# Ejecutar query
Invoke-RestMethod -Method Post -Uri "https://victor.tpqsolutions.com.ar/query" -Body $body -ContentType "application/json"
```

**Resultado esperado:** JSON con datos de la tabla Facturas

### 5.4 - Verificar Logs

```powershell
# Ver logs de SQLBridge
Get-Content "C:\SqlBridge\logs\sqlbridge_$(Get-Date -Format 'yyyyMMdd').log" -Tail 20

# Ver logs en tiempo real
Get-Content "C:\SqlBridge\logs\sqlbridge_$(Get-Date -Format 'yyyyMMdd').log" -Wait -Tail 10
```

---

## ?? PASO 6: Probar Reinicio

```powershell
# Reiniciar la PC de Victor
Restart-Computer

# Después del reinicio, verificar que todo funciona:
# 1. Health check local
curl http://localhost:5000/health

# 2. Health check público
# https://victor.tpqsolutions.com.ar/health

# 3. Servicios corriendo
C:\nssm\nssm.exe status SqlBridgeWeb
Get-Service cloudflared
```

---

## ?? Arquitectura Final

```
????????????????????????????????????????????????????????????????
?                   ?? Internet                                ?
?                                                              ?
?  ?? App Móvil ? victor.tpqsolutions.com.ar                   ?
?                                                              ?
????????????????????????????????????????????????????????????????
                          ?
                          ? HTTPS
                          ?
????????????????????????????????????????????????????????????????
?              ??  Cloudflare Network                          ?
?                                                              ?
?  • Resuelve DNS: victor.tpqsolutions.com.ar                  ?
?  • Encuentra tunnel: sqlbridge-victor                        ?
?  • Tunnel ID: f4f26148-1585-4ec7-8b48-XXXXXXXX              ?
?                                                              ?
????????????????????????????????????????????????????????????????
                          ?
                          ? Tunnel Encriptado
                          ?
????????????????????????????????????????????????????????????????
?          ?? PC de Victor (192.168.1.x)                       ?
?                                                              ?
?  ??????????????????????????????????????????????????????     ?
?  ?  cloudflared (Servicio Windows)                    ?     ?
?  ?  • Lee: C:\cloudflared\config.yml                  ?     ?
?  ?  • Usa: f4f26148-1585-4ec7-8b48-XXX.json           ?     ?
?  ?  • Redirige a: http://localhost:5000               ?     ?
?  ??????????????????????????????????????????????????????     ?
?                      ?                                       ?
?                      ?                                       ?
?  ??????????????????????????????????????????????????????     ?
?  ?  SQLBridge (Servicio Windows)                      ?     ?
?  ?  • Puerto: 5000                                    ?     ?
?  ?  • Endpoints: /health, /query                      ?     ?
?  ?  • Logs: C:\SqlBridge\logs\                        ?     ?
?  ??????????????????????????????????????????????????????     ?
?                      ?                                       ?
????????????????????????????????????????????????????????????????
                       ?
                       ? TCP 1433
                       ?
             ???????????????????????
             ?  ?? SQL Server      ?
             ?  192.168.1.100      ?
             ?  DB: Comercio       ?
             ???????????????????????
```

---

## ?? Checklist Final

```
[ ] .NET 8 Runtime instalado
[ ] NSSM instalado en C:\nssm\
[ ] cloudflared instalado en C:\cloudflared\

[ ] SQLBridge copiado a C:\SqlBridge\
[ ] appsettings.json configurado con connection string
[ ] SQLBridge probado manualmente (health check funciona)
[ ] Servicio SqlBridgeWeb instalado y corriendo

[ ] Archivo de credenciales .json copiado a C:\Users\Victor\.cloudflared\
[ ] config.yml creado en C:\cloudflared\
[ ] Tunnel probado manualmente (health check público funciona)
[ ] Servicio cloudflared instalado y corriendo

[ ] Health check local funciona: http://localhost:5000/health
[ ] Health check público funciona: https://victor.tpqsolutions.com.ar/health
[ ] Query funciona a través del tunnel
[ ] Logs se están generando en C:\SqlBridge\logs\

[ ] PC reiniciada y servicios inician automáticamente
[ ] Todo funciona después del reinicio
```

---

## ?? Troubleshooting Común

### ? Error: "Tunnel credentials not found"

**Solución:**
```powershell
# Verificar que el archivo .json existe
dir "$env:USERPROFILE\.cloudflared\*.json"

# Verificar que el path en config.yml es correcto
notepad C:\cloudflared\config.yml
```

### ? Error: "Connection refused to localhost:5000"

**Solución:**
```powershell
# Verificar que SQLBridge está corriendo
C:\nssm\nssm.exe status SqlBridgeWeb

# Ver logs
Get-Content "C:\SqlBridge\logs\sqlbridge_$(Get-Date -Format 'yyyyMMdd').log" -Tail 50

# Reiniciar servicio
C:\nssm\nssm.exe restart SqlBridgeWeb
```

### ? Error: "Login failed for user"

**Solución:**
```powershell
# Verificar connection string
notepad C:\SqlBridge\appsettings.json

# Probar conexión con SQL Server Management Studio
# Usar las mismas credenciales
```

---

## ?? Información a Registrar

```
CLIENTE: Victor
FECHA INSTALACIÓN: _______________

=== INFORMACIÓN TÉCNICA ===
SQL Server IP: ________________
Base de Datos: ________________
Usuario SQL: __________________
Puerto SQL: 1433

=== CLOUDFLARE ===
Tunnel ID: f4f26148-1585-4ec7-8b48-XXXXXXXX
Tunnel Name: sqlbridge-victor
Subdominio: victor.tpqsolutions.com.ar
DNS CNAME: victor ? f4f26148-1585-4ec7-8b48-XXXXXXXX.cfargotunnel.com

=== URLs ===
Health Check: https://victor.tpqsolutions.com.ar/health
Query Endpoint: https://victor.tpqsolutions.com.ar/query

=== ARCHIVOS IMPORTANTES ===
Credenciales: C:\Users\Victor\.cloudflared\f4f26148-1585-4ec7-8b48-XXXXXXXX.json
Config: C:\cloudflared\config.yml
appsettings: C:\SqlBridge\appsettings.json

=== SERVICIOS WINDOWS ===
Servicio 1: SqlBridgeWeb (puerto 5000)
Servicio 2: cloudflared

=== ESTADO ===
Instalación: [ ] OK
Pruebas: [ ] OK
Producción: [ ] OK

NOTAS:
_____________________________________________
_____________________________________________
```

---

## ?? Próximo Paso: Configurar API en Railway

Una vez que esto funcione, necesitas configurar la variable de entorno en Railway:

```bash
SQL_BRIDGE_URL=https://victor.tpqsolutions.com.ar
```

**Documentación:** Ver `docs/GUIA_INSTALACION_CLIENTE_NUEVO.md` sección "Configuración App Móvil"

---

**? ¡Todo listo para Victor!**

Ahora Victor puede usar la aplicación móvil para consultar sus arqueos de caja desde cualquier lugar.
