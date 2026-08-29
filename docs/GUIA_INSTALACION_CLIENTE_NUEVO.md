# ?? Guía de Instalación SQLBridge - Cliente Nuevo

**Versión:** 1.0  
**Última actualización:** Febrero 2026  
**Propósito:** Configurar SQLBridge en la PC del cliente para habilitar la aplicación móvil

---

## ? Guías Rápidas

¿Primera vez configurando? Consulta estas guías:

| Guía | Descripción | Tiempo |
|------|-------------|--------|
| **[Crear Subdominios](GUIA_RAPIDA_SUBDOMINIOS.md)** | Cómo generar subdominios en tpqsolutions.com.ar | 2 min |
| **[Diagrama Visual](DIAGRAMA_SUBDOMINIOS.txt)** | Diagrama completo del proceso | - |
| **[Arquitectura Multi-Cliente](ARQUITECTURA_MULTICLIENTE.md)** | Gestión de múltiples clientes | - |
| **[Script Automático](scripts/crear-subdominios-ejemplo.ps1)** | Script PowerShell para crear subdominios | - |

**?? TIP:** Si solo necesitas crear un subdominio, ve directamente a [Guía Rápida Subdominios](GUIA_RAPIDA_SUBDOMINIOS.md)

---

## ?? Índice

1. [Requisitos Previos](#requisitos-previos)
2. [Instalación de Dependencias](#instalación-de-dependencias)
3. [Instalación de SQLBridge](#instalación-de-sqlbridge)
4. [Configuración de Cloudflare Tunnel](#configuración-de-cloudflare-tunnel)
5. [Configuración del Servicio Windows](#configuración-del-servicio-windows)
6. [Verificación y Pruebas](#verificación-y-pruebas)
7. [Troubleshooting](#troubleshooting)
8. [Checklist Final](#checklist-final)

---

## ?? Requisitos Previos

### En la PC del Cliente

- ? **Sistema Operativo:** Windows Server 2016+ o Windows 10/11 Pro
- ? **Acceso:** Permisos de administrador
- ? **Red:** Acceso a la red local donde está SQL Server
- ? **SQL Server:** Debe estar accesible en la red local (puerto 1433)
- ? **Conexión a Internet:** Para Cloudflare Tunnel

### Información a Solicitar al Cliente

Antes de comenzar, necesitas recopilar:

1. **IP del SQL Server:** Ejemplo: `192.168.100.200`
2. **Nombre de la base de datos:** Ejemplo: `Comercio`
3. **Usuario y contraseña de SQL Server**
4. **Nombre del cliente:** Para crear el subdominio (Ejemplo: `victor`, `pepe`, `cliente1`)

---

## ?? Instalación de Dependencias

### Paso 1: Instalar .NET 8 Runtime

1. **Descargar .NET 8 Runtime**
   - Ir a: https://dotnet.microsoft.com/download/dotnet/8.0
   - Descargar **ASP.NET Core Runtime 8.0.x (Hosting Bundle)**

2. **Instalar el ejecutable**
   ```powershell
   # Ejecutar el instalador descargado
   # Ejemplo: dotnet-hosting-8.0.x-win.exe
   ```

3. **Verificar instalación**
   ```powershell
   dotnet --version
   # Debe mostrar: 8.0.x
   ```

### Paso 2: Instalar NSSM (Non-Sucking Service Manager)

1. **Descargar NSSM**
   - Ir a: https://nssm.cc/download
   - Descargar la versión más reciente (nssm-2.24.zip)

2. **Extraer y copiar**
   ```powershell
   # Crear directorio
   New-Item -Path "C:\nssm" -ItemType Directory -Force

   # Extraer nssm.exe según la arquitectura
   # Para sistemas de 64 bits, copiar win64\nssm.exe
   # a C:\nssm\nssm.exe
   ```

3. **Verificar instalación**
   ```powershell
   C:\nssm\nssm.exe version
   # Debe mostrar: NSSM 2.24
   ```

### Paso 3: Instalar Cloudflare Tunnel (cloudflared)

1. **Descargar cloudflared**
   - Ir a: https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/install-and-setup/installation/
   - Descargar **cloudflared-windows-amd64.exe**

2. **Instalar**
   ```powershell
   # Cambiar el nombre del ejecutable
   Rename-Item -Path "$env:USERPROFILE\Downloads\cloudflared-windows-amd64.exe" -NewName "cloudflared.exe"

   # Crear directorio
   New-Item -Path "C:\cloudflared" -ItemType Directory -Force

   # Mover el ejecutable
   Move-Item -Path "$env:USERPROFILE\Downloads\cloudflared.exe" -Destination "C:\cloudflared\cloudflared.exe"
   ```

3. **Agregar al PATH (opcional)**
   ```powershell
   # Agregar C:\cloudflared al PATH del sistema
   [Environment]::SetEnvironmentVariable(
       "Path",
       $env:Path + ";C:\cloudflared",
       [EnvironmentVariableTarget]::Machine
   )
   ```

4. **Verificar instalación**
   ```powershell
   C:\cloudflared\cloudflared.exe --version
   # Debe mostrar: cloudflared version X.X.X
   ```

---

## ??? Instalación de SQLBridge

### Paso 1: Crear Directorio Base

```powershell
# Crear directorio principal
New-Item -Path "C:\SqlBridge" -ItemType Directory -Force

# Crear subdirectorio para logs
New-Item -Path "C:\SqlBridge\logs" -ItemType Directory -Force
```

### Paso 2: Copiar Archivos del SQLBridge

**Opción A: Desde ZIP Precompilado**

```powershell
# Descomprimir el ZIP de SQLBridge
$zipPath = "$env:USERPROFILE\Desktop\SqlBridge.zip"
Expand-Archive -Path $zipPath -DestinationPath "C:\SqlBridge" -Force
```

**Opción B: Compilar desde código fuente**

```powershell
# En tu máquina de desarrollo:
cd "Comercio.NET.SqlBridge\Comercio.NET.SqlBridge.Server"
dotnet publish -c Release -o publish

# Comprimir
Compress-Archive -Path "publish\*" -DestinationPath "SqlBridge.zip" -Force

# Transferir SqlBridge.zip a la PC del cliente
# Luego usar Opción A
```

### Paso 3: Configurar Connection String

1. **Editar appsettings.json**
   ```powershell
   notepad C:\SqlBridge\appsettings.json
   ```

2. **Configurar la conexión a SQL Server**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=192.168.100.200,1433;Database=Comercio;User Id=usuario_sql;Password=contraseña_sql;TrustServerCertificate=True;Encrypt=False;"
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

   **?? Importante:** Reemplazar:
   - `192.168.100.200` ? IP real del SQL Server
   - `Comercio` ? Nombre real de la base de datos
   - `usuario_sql` ? Usuario de SQL Server
   - `contraseña_sql` ? Contraseña de SQL Server

3. **Guardar el archivo**

### Paso 4: Probar SQLBridge Manualmente (antes de instalarlo como servicio)

```powershell
# Ejecutar SQLBridge manualmente
cd C:\SqlBridge
C:\"Program Files"\dotnet\dotnet.exe Comercio.NET.SqlBridge.Server.dll

# Debe mostrar:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: http://0.0.0.0:5000
```

**Abrir otra terminal PowerShell y probar:**

```powershell
# Health check
curl http://localhost:5000/health

# Debe responder: {"status":"ok","timestamp":"..."}
```

**Si funciona correctamente, presionar CTRL+C para detener y continuar al siguiente paso.**

---

## ?? Configuración de Cloudflare Tunnel

### ?? Estrategia de Dominios

Tienes dos opciones para configurar los subdominios:

#### **Opción A: Subdominios de tpqsolutions.com.ar (RECOMENDADA)** ?

Usar tu dominio centralizado para todos los clientes:
- `victor.tpqsolutions.com.ar`
- `pepe.tpqsolutions.com.ar`
- `cliente1.tpqsolutions.com.ar`

**Ventajas:**
- ? Una sola cuenta de Cloudflare
- ? Gestión centralizada de todos los tunnels
- ? Sin costo adicional
- ? Fácil mantenimiento
- ? Escalable

**Ideal para:** Aplicaciones internas, APIs, servicios técnicos

#### **Opción B: Dominio del cliente**

Usar el dominio propio del cliente (si lo tiene en Cloudflare):
- `sql.nombrecliente.com.ar`

**Ventajas:**
- ? URL personalizada con marca del cliente
- ? Más profesional para servicios públicos

**Desventajas:**
- ?? Requiere que el cliente tenga su dominio en Cloudflare
- ?? Gestión distribuida en múltiples cuentas

---

### Paso 1: Autenticarse en Cloudflare

**?? IMPORTANTE:** Solo necesitas hacer este paso **UNA VEZ** por PC del cliente. Si ya tienes el certificado de una instalación anterior, **sáltate al Paso 2**.

```powershell
# Ejecutar cloudflared
C:\cloudflared\cloudflared.exe tunnel login

# Se abrirá un navegador
# Iniciar sesión con tu cuenta de Cloudflare (manuclaro@gmail.com)
# Seleccionar el dominio: tpqsolutions.com.ar
```

**Resultado:** Se creará un archivo de certificado en:
```
C:\Users\<Usuario>\.cloudflared\cert.pem
```

**?? Nota:** Este certificado es válido para crear múltiples tunnels en el mismo dominio.

### Paso 2: Crear un Tunnel

```powershell
# Crear el tunnel con un nombre descriptivo (usar el nombre del cliente)
C:\cloudflared\cloudflared.exe tunnel create sqlbridge-victor

# Ejemplo de respuesta:
# Tunnel credentials written to C:\Users\<Usuario>\.cloudflared\<TUNNEL_ID>.json
# {"AccountTag":"xxxxx","TunnelID":"yyyy-zzzz-...","TunnelName":"sqlbridge-victor"}
```

**?? Importante:** Anota el **TUNNEL_ID** que aparece en la respuesta.

**?? Convención de nombres sugerida:**
- `sqlbridge-victor` para el cliente Victor
- `sqlbridge-pepe` para el cliente Pepe
- `sqlbridge-cliente1` para Cliente1
- etc.

### Paso 3: Configurar el DNS

#### Opción A: Usando tpqsolutions.com.ar (RECOMENDADA)

```powershell
# Asociar un subdominio de tpqsolutions.com.ar al tunnel
C:\cloudflared\cloudflared.exe tunnel route dns sqlbridge-victor victor.tpqsolutions.com.ar

# Ejemplo de respuesta:
# Successfully routed tunnel sqlbridge-victor to victor.tpqsolutions.com.ar
```

**Verificar en Cloudflare Dashboard:**
- Ir a: https://dash.cloudflare.com
- Seleccionar el dominio **tpqsolutions.com.ar**
- Ir a **DNS** ? **Records**
- Debe aparecer un registro CNAME: `victor` ? `TUNNEL_ID.cfargotunnel.com`

#### Opción B: Usando dominio del cliente

```powershell
# Si el cliente tiene su propio dominio en Cloudflare
C:\cloudflared\cloudflared.exe tunnel route dns sqlbridge-cliente sql.dominiodelcliente.com.ar

# Ejemplo de respuesta:
# Successfully routed tunnel sqlbridge-cliente to sql.dominiodelcliente.com.ar
```

**?? Nota:** Esta opción requiere que el dominio del cliente esté en tu cuenta de Cloudflare o que tengas acceso a su cuenta.

### Paso 4: Crear Archivo de Configuración del Tunnel

1. **Crear directorio de configuración**
   ```powershell
   New-Item -Path "C:\cloudflared" -ItemType Directory -Force
   ```

2. **Crear config.yml**
   ```powershell
   notepad C:\cloudflared\config.yml
   ```

3. **Agregar la siguiente configuración:**

   **Opción A: Con tpqsolutions.com.ar**
   ```yaml
   tunnel: TUNNEL_ID
   credentials-file: C:\Users\<Usuario>\.cloudflared\TUNNEL_ID.json

   ingress:
     - hostname: victor.tpqsolutions.com.ar
       service: http://localhost:5000
     - service: http_status:404
   ```

   **Opción B: Con dominio del cliente**
   ```yaml
   tunnel: TUNNEL_ID
   credentials-file: C:\Users\<Usuario>\.cloudflared\TUNNEL_ID.json

   ingress:
     - hostname: sql.nombrecliente.com.ar
       service: http://localhost:5000
     - service: http_status:404
   ```

   **?? Reemplazar:**
   - `TUNNEL_ID` ? El ID del tunnel creado en el Paso 2
   - `<Usuario>` ? Nombre del usuario de Windows
   - `victor.tpqsolutions.com.ar` ? El subdominio que configuraste en el Paso 3

4. **Guardar el archivo**

### Paso 5: Probar el Tunnel Manualmente

```powershell
# Iniciar SQLBridge en una terminal
cd C:\SqlBridge
C:\"Program Files"\dotnet\dotnet.exe Comercio.NET.SqlBridge.Server.dll

# En OTRA terminal PowerShell, iniciar el tunnel
C:\cloudflared\cloudflared.exe tunnel --config C:\cloudflared\config.yml run

# Debe mostrar:
# INF Connection registered connIndex=0 location=XXX
# INF Connection registered connIndex=1 location=YYY
```

**Probar desde Internet:**

Desde cualquier navegador (incluso desde tu teléfono con datos móviles):
```
https://victor.tpqsolutions.com.ar/health
```

**Debe responder:** `{"status":"ok","timestamp":"..."}`

**?? Si usas el dominio del cliente, la URL sería:**
```
https://sql.nombrecliente.com.ar/health
```

**Si funciona, presionar CTRL+C en ambas terminales y continuar.**

---

## ?? Configuración del Servicio Windows

### Paso 1: Instalar SQLBridge como Servicio

```powershell
# Instalar con NSSM
C:\nssm\nssm.exe install SqlBridgeWeb "C:\Program Files\dotnet\dotnet.exe" "C:\SqlBridge\Comercio.NET.SqlBridge.Server.dll"

# Configurar el directorio de trabajo
C:\nssm\nssm.exe set SqlBridgeWeb AppDirectory "C:\SqlBridge"

# Configurar inicio automático
C:\nssm\nssm.exe set SqlBridgeWeb Start SERVICE_AUTO_START

# Configurar descripción
C:\nssm\nssm.exe set SqlBridgeWeb Description "SQL Bridge para aplicación móvil Comercio.NET"

# Configurar salida de logs (opcional)
C:\nssm\nssm.exe set SqlBridgeWeb AppStdout "C:\SqlBridge\logs\nssm-stdout.log"
C:\nssm\nssm.exe set SqlBridgeWeb AppStderr "C:\SqlBridge\logs\nssm-stderr.log"
```

### Paso 2: Instalar Cloudflare Tunnel como Servicio

```powershell
# Instalar como servicio usando cloudflared
C:\cloudflared\cloudflared.exe service install

# Debe mostrar:
# INF Installing service with the following command
# INF Successfully installed service cloudflared
```

**El servicio se crea automáticamente con el nombre:** `cloudflared`

**Configurar inicio automático:**
```powershell
Set-Service -Name cloudflared -StartupType Automatic
```

### Paso 3: Iniciar los Servicios

```powershell
# Iniciar SQLBridge
C:\nssm\nssm.exe start SqlBridgeWeb

# Iniciar Cloudflare Tunnel
Start-Service cloudflared

# Verificar estado
C:\nssm\nssm.exe status SqlBridgeWeb
# Debe mostrar: SERVICE_RUNNING

Get-Service cloudflared
# Debe mostrar: Running
```

### Paso 4: Configurar Firewall (si es necesario)

```powershell
# Permitir puerto 5000 en el firewall local (solo si hay problemas)
New-NetFirewallRule -DisplayName "SQLBridge Port 5000" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow
```

**?? Nota:** Generalmente NO es necesario abrir puertos porque el tunnel de Cloudflare hace conexiones salientes.

---

## ? Verificación y Pruebas

### Prueba 1: Health Check Local

```powershell
curl http://localhost:5000/health
```

**Resultado esperado:**
```json
{"status":"ok","timestamp":"2026-02-10T23:45:00"}
```

### Prueba 2: Health Check a través de Cloudflare

Desde un navegador externo (o el móvil):
```
https://victor.tpqsolutions.com.ar/health
```

**Resultado esperado:**
```json
{"status":"ok","timestamp":"2026-02-10T23:45:00"}
```

**?? Recuerda:** Si usas el dominio del cliente, reemplaza `victor.tpqsolutions.com.ar` con `sql.nombrecliente.com.ar`

### Prueba 3: Query SQL

```powershell
# Crear archivo de prueba
$body = @{
    query = "SELECT TOP 5 * FROM Facturas ORDER BY Fecha DESC"
    parameters = @{}
} | ConvertTo-Json

# Ejecutar query localmente
Invoke-RestMethod -Method Post -Uri "http://localhost:5000/query" -Body $body -ContentType "application/json"
```

**Resultado esperado:** JSON con datos de la tabla Facturas

### Prueba 4: Query a través de Cloudflare

```powershell
# Mismo query pero a través del tunnel
Invoke-RestMethod -Method Post -Uri "https://victor.tpqsolutions.com.ar/query" -Body $body -ContentType "application/json"
```

**Resultado esperado:** Mismo JSON con datos

**?? Nota:** Reemplaza `victor.tpqsolutions.com.ar` con el subdominio de tu cliente

### Prueba 5: Verificar Logs

```powershell
# Ver últimas líneas del log
Get-Content "C:\SqlBridge\logs\sqlbridge_$(Get-Date -Format 'yyyyMMdd').log" -Tail 20

# Ver logs en tiempo real
Get-Content "C:\SqlBridge\logs\sqlbridge_$(Get-Date -Format 'yyyyMMdd').log" -Wait -Tail 10
```

**Buscar en los logs:**
- `=== INICIANDO SQL BRIDGE ===`
- `?? SQL Bridge iniciado en http://0.0.0.0:5000`
- `=== INICIO REQUEST /query ===`
- `=== FIN REQUEST /query (EXITOSO) ===`

---

## ?? Troubleshooting

### Problema: SQLBridge no inicia

**Verificar el servicio:**
```powershell
C:\nssm\nssm.exe status SqlBridgeWeb
```

**Si muestra "STOPPED":**
```powershell
# Ver los logs de error
Get-Content "C:\SqlBridge\logs\nssm-stderr.log" -Tail 50

# Intentar iniciar manualmente
cd C:\SqlBridge
C:\"Program Files"\dotnet\dotnet.exe Comercio.NET.SqlBridge.Server.dll
```

**Causas comunes:**
- ? Connection string incorrecta
- ? SQL Server no accesible
- ? .NET 8 Runtime no instalado
- ? Permisos insuficientes en C:\SqlBridge

### Problema: Cloudflare Tunnel no funciona

**Verificar el servicio:**
```powershell
Get-Service cloudflared
```

**Si muestra "Stopped":**
```powershell
# Ver logs del servicio
Get-EventLog -LogName Application -Source cloudflared -Newest 20

# Reiniciar el servicio
Restart-Service cloudflared
```

**Probar manualmente:**
```powershell
C:\cloudflared\cloudflared.exe tunnel --config C:\cloudflared\config.yml run
```

**Causas comunes:**
- ? config.yml mal configurado
- ? TUNNEL_ID incorrecto
- ? Archivo credentials (TUNNEL_ID.json) no encontrado
- ? DNS no propagado (esperar 5-10 minutos)

### Problema: Error 500 en /query

**Verificar logs:**
```powershell
Get-Content "C:\SqlBridge\logs\sqlbridge_$(Get-Date -Format 'yyyyMMdd').log" -Tail 50
```

**Buscar errores SQL:**
- `[Error] Error ejecutando query:`
- `SqlException`
- `Login failed for user`

**Soluciones:**
1. Verificar connection string en `appsettings.json`
2. Probar conexión a SQL Server con SQL Server Management Studio
3. Verificar que el usuario SQL tenga permisos en la base de datos

### Problema: Timeout en queries

**Solución:** Aumentar el timeout en `Program.cs` (requiere recompilar):

```csharp
cmd.CommandTimeout = 60; // segundos (default: 30)
```

### Problema: No se puede acceder desde la app móvil

**Verificar:**
1. ? Ambos servicios corriendo:
   ```powershell
   C:\nssm\nssm.exe status SqlBridgeWeb
   Get-Service cloudflared
   ```

2. ? DNS propagado:
   ```powershell
   nslookup sql.nombrecliente.com.ar
   # Debe resolver a XXX.cfargotunnel.com
   ```

3. ? Health check público funcionando:
   ```
   https://victor.tpqsolutions.com.ar/health
   ```

4. ? Configuración en la API Mobile Server (Railway):
   - Variable de entorno: `SQL_BRIDGE_URL=https://victor.tpqsolutions.com.ar`
   - (O el subdominio correspondiente al cliente)

---

## ?? Checklist Final

### Pre-instalación
- [ ] .NET 8 Runtime instalado
- [ ] NSSM descargado y extraído
- [ ] cloudflared descargado
- [ ] Información del cliente recopilada (IP SQL, DB, usuario, contraseña, subdominio)

### SQLBridge
- [ ] Directorio `C:\SqlBridge` creado
- [ ] Archivos copiados a `C:\SqlBridge`
- [ ] `appsettings.json` configurado con connection string correcta
- [ ] SQLBridge probado manualmente (health check funciona)
- [ ] Servicio `SqlBridgeWeb` instalado con NSSM
- [ ] Servicio `SqlBridgeWeb` iniciado y corriendo
- [ ] Logs creándose en `C:\SqlBridge\logs\`

### Cloudflare Tunnel
- [ ] cloudflared autenticado (`tunnel login`) - **Solo primera vez**
- [ ] Tunnel creado con nombre descriptivo (`tunnel create sqlbridge-nombrecliente`)
- [ ] DNS configurado (`tunnel route dns`) - Subdominio de tpqsolutions.com.ar
- [ ] `config.yml` creado con configuración correcta
- [ ] Tunnel probado manualmente (health check público funciona)
- [ ] Servicio `cloudflared` instalado
- [ ] Servicio `cloudflared` iniciado y corriendo
- [ ] Health check público accesible: `https://nombrecliente.tpqsolutions.com.ar/health`

### Pruebas
- [ ] Health check local: `http://localhost:5000/health` ?
- [ ] Health check público: `https://sql.nombrecliente.com.ar/health` ?
- [ ] Query local funciona
- [ ] Query a través de Cloudflare funciona
- [ ] Logs se están generando correctamente

### Configuración App Móvil
- [ ] Variable `SQL_BRIDGE_URL` configurada en Railway con el subdominio del cliente
- [ ] Ejemplo: `SQL_BRIDGE_URL=https://victor.tpqsolutions.com.ar`
- [ ] App móvil probada con el nuevo cliente
- [ ] Arqueos de caja visibles en la app

### Reinicio de PC
- [ ] PC del cliente reiniciada
- [ ] Ambos servicios inician automáticamente después del reinicio
- [ ] Health checks funcionan después del reinicio

---

## ?? Información para Registrar

Después de completar la instalación, registra la siguiente información:

```
CLIENTE: ___________________________________
FECHA INSTALACIÓN: ________________________

=== INFORMACIÓN TÉCNICA ===
SQL Server IP: ____________________________
Base de Datos: _____________________________
Usuario SQL: _______________________________
Puerto SQL: 1433

=== CLOUDFLARE ===
Tunnel ID: _________________________________
Tunnel Name: sqlbridge-_____________________
Subdominio: ____________.tpqsolutions.com.ar
DNS CNAME: ________ ? ___________.cfargotunnel.com

=== URLs ===
Health Check: https://________.tpqsolutions.com.ar/health
Query Endpoint: https://________.tpqsolutions.com.ar/query
API Mobile: https://comercio-net-web-production.up.railway.app

=== SERVICIOS WINDOWS ===
Servicio 1: SqlBridgeWeb (puerto 5000)
Servicio 2: cloudflared

=== ESTADO ===
Instalación: [ ] OK  [ ] PENDIENTE
Pruebas: [ ] OK  [ ] PENDIENTE
Producción: [ ] OK  [ ] PENDIENTE

NOTAS:
_____________________________________________
_____________________________________________
_____________________________________________
```

---

## ?? Contacto y Soporte

Si encuentras problemas durante la instalación:

1. **Revisar logs:**
   - SQLBridge: `C:\SqlBridge\logs\`
   - NSSM: `C:\SqlBridge\logs\nssm-*.log`
   - Windows Events: Event Viewer ? Application

2. **Documentación adicional:**
   - [Arquitectura SQLBridge](ARQUITECTURA_SQLBRIDGE.md)
   - [Cloudflare Tunnel Docs](https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/)

3. **Comandos útiles para debug:**
   ```powershell
   # Logs en tiempo real
   Get-Content "C:\SqlBridge\logs\sqlbridge_$(Get-Date -Format 'yyyyMMdd').log" -Wait -Tail 20

   # Estado de servicios
   Get-Service SqlBridgeWeb, cloudflared

   # Reiniciar todo
   Restart-Service SqlBridgeWeb, cloudflared
   ```

---

## ?? Actualización del SQLBridge

Cuando necesites actualizar SQLBridge a una nueva versión:

```powershell
# 1. Detener el servicio
C:\nssm\nssm.exe stop SqlBridgeWeb
Start-Sleep -Seconds 3

# 2. Backup
$backup = "C:\SqlBridge_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item "C:\SqlBridge" $backup -Recurse -Force

# 3. Descomprimir nueva versión (SIN sobrescribir logs y config)
$zipPath = "$env:USERPROFILE\Desktop\SqlBridge.zip"
$temp = "$env:TEMP\SqlBridge_Update"
Expand-Archive -Path $zipPath -DestinationPath $temp -Force

# Copiar archivos (excepto logs y appsettings.json)
Get-ChildItem $temp | Where-Object { 
    $_.Name -ne 'logs' -and $_.Name -ne 'appsettings.json' 
} | ForEach-Object { 
    Copy-Item $_.FullName "C:\SqlBridge" -Recurse -Force 
}

Remove-Item $temp -Recurse -Force

# 4. Iniciar el servicio
C:\nssm\nssm.exe start SqlBridgeWeb

# 5. Verificar
Start-Sleep -Seconds 5
curl http://localhost:5000/health
```

---

## ?? Gestión de Múltiples Clientes con tpqsolutions.com.ar

### Dashboard de Cloudflare

Todos tus clientes estarán centralizados en tu cuenta de Cloudflare:

**Ver todos los tunnels:**
```powershell
C:\cloudflared\cloudflared.exe tunnel list
```

**Resultado esperado:**
```
ID                                   NAME                   CREATED
aaaa-bbbb-cccc-dddd-eeee            sqlbridge-victor       2026-02-10T10:30:00Z
ffff-gggg-hhhh-iiii-jjjj            sqlbridge-pepe         2026-02-11T14:20:00Z
kkkk-llll-mmmm-nnnn-oooo            sqlbridge-cliente1     2026-02-12T09:15:00Z
```

### Ejemplo: Tres Clientes Configurados

#### Cliente 1: Victor
- **Tunnel Name:** `sqlbridge-victor`
- **Subdominio:** `victor.tpqsolutions.com.ar`
- **Health Check:** https://victor.tpqsolutions.com.ar/health
- **SQL Server:** 192.168.1.100:1433

#### Cliente 2: Pepe
- **Tunnel Name:** `sqlbridge-pepe`
- **Subdominio:** `pepe.tpqsolutions.com.ar`
- **Health Check:** https://pepe.tpqsolutions.com.ar/health
- **SQL Server:** 192.168.0.50:1433

#### Cliente 3: Comercio ABC
- **Tunnel Name:** `sqlbridge-abc`
- **Subdominio:** `abc.tpqsolutions.com.ar`
- **Health Check:** https://abc.tpqsolutions.com.ar/health
- **SQL Server:** 10.0.0.10:1433

### Vista en Cloudflare DNS

En tu panel de Cloudflare (https://dash.cloudflare.com), dominio **tpqsolutions.com.ar**, sección **DNS**, verás:

| Type  | Name    | Content                                    |
|-------|---------|-------------------------------------------|
| CNAME | victor  | aaaa-bbbb-cccc-dddd.cfargotunnel.com     |
| CNAME | pepe    | ffff-gggg-hhhh-iiii.cfargotunnel.com     |
| CNAME | abc     | kkkk-llll-mmmm-nnnn.cfargotunnel.com     |

### Configuración en Railway (API Mobile)

Para cada cliente, crea una instancia separada de la API o usa variables de entorno dinámicas:

**Opción A: Una instancia por cliente**
- `comercio-victor` ? `SQL_BRIDGE_URL=https://victor.tpqsolutions.com.ar`
- `comercio-pepe` ? `SQL_BRIDGE_URL=https://pepe.tpqsolutions.com.ar`
- `comercio-abc` ? `SQL_BRIDGE_URL=https://abc.tpqsolutions.com.ar`

**Opción B: Una sola API con parámetros**
- Enviar el subdominio como parámetro en cada request

### Monitoreo Centralizado

```powershell
# Script para verificar todos los clientes
$clientes = @(
    @{Nombre="Victor"; URL="https://victor.tpqsolutions.com.ar/health"},
    @{Nombre="Pepe"; URL="https://pepe.tpqsolutions.com.ar/health"},
    @{Nombre="ABC"; URL="https://abc.tpqsolutions.com.ar/health"}
)

foreach ($cliente in $clientes) {
    try {
        $response = Invoke-RestMethod -Uri $cliente.URL -TimeoutSec 5
        Write-Host "? $($cliente.Nombre): OK" -ForegroundColor Green
    }
    catch {
        Write-Host "? $($cliente.Nombre): ERROR" -ForegroundColor Red
    }
}
```

### Ventajas de esta Arquitectura

1. **Administración Centralizada**
   - Un solo dashboard de Cloudflare
   - Todas las métricas en un lugar
   - Fácil seguimiento de todos los clientes

2. **Escalabilidad**
   - Agregar nuevos clientes es simple: crear tunnel + configurar DNS
   - Sin límite de subdominios
   - Sin costos adicionales

3. **Mantenimiento Simplificado**
   - Actualizaciones de certificados automáticas
   - Monitoreo unificado
   - Troubleshooting más rápido

4. **Seguridad**
   - Cada cliente tiene su propio tunnel (aislamiento)
   - Tráfico encriptado por Cloudflare
   - Logs separados por tunnel

### Límites de Cloudflare (Plan Free)

- ? Tunnels ilimitados
- ? Subdominios ilimitados
- ? Tráfico ilimitado
- ?? 1 zona DNS (dominio) - En tu caso: `tpqsolutions.com.ar`

**Conclusión:** Puedes tener **todos los clientes que necesites** usando subdominios de `tpqsolutions.com.ar` sin ningún costo adicional.

---

## ?? Plantilla de Registro Múltiples Clientes

```
=== REGISTRO DE CLIENTES SQLBRIDGE ===
Dominio Base: tpqsolutions.com.ar
Cuenta Cloudflare: manuclaro@gmail.com

??????????????????????????????????????????????????????????????????????????????????????????
? Cliente     ? Tunnel Name      ? Subdominio                          ? SQL Server IP   ?
??????????????????????????????????????????????????????????????????????????????????????????
? Victor      ? sqlbridge-victor ? victor.tpqsolutions.com.ar          ? 192.168.1.100   ?
??????????????????????????????????????????????????????????????????????????????????????????
? Pepe        ? sqlbridge-pepe   ? pepe.tpqsolutions.com.ar            ? 192.168.0.50    ?
??????????????????????????????????????????????????????????????????????????????????????????
? ABC         ? sqlbridge-abc    ? abc.tpqsolutions.com.ar             ? 10.0.0.10       ?
??????????????????????????????????????????????????????????????????????????????????????????
?             ?                  ?                                     ?                 ?
??????????????????????????????????????????????????????????????????????????????????????????

NOTAS:
- Todos los tunnels usan el mismo certificado: C:\Users\<Usuario>\.cloudflared\cert.pem
- Cada PC del cliente tiene su propio config.yml con su tunnel ID específico
- Railway: Una instancia de API por cliente (o una compartida con parámetros)
```

---

**? ¡Instalación Completa!**

Ahora el cliente puede usar la aplicación móvil de Comercio.NET para consultar los arqueos de caja en tiempo real desde cualquier lugar.

Con la estrategia de subdominios de `tpqsolutions.com.ar`, puedes escalar fácilmente a decenas o cientos de clientes sin preocuparte por gestionar múltiples cuentas o dominios.
