# Instalación de Comercio.NET en un Nuevo Cliente

## Arquitectura del sistema

```
[PC del cliente]                          [Railway - Cloud]
???????????????????????????????           ????????????????????????????????
?  Base de datos              ?           ?  API Mobile (Docker)         ?
?  SQL Server o PostgreSQL    ?           ?  Comercio.NET.Mobile         ?
?                             ?           ?                              ?
?  SqlBridge                  ?????????????  SQL_BRIDGE_URL =            ?
?  (Windows Service)          ?  HTTPS    ?  https://[cliente]           ?
?  http://localhost:5000      ?  túnel    ?  .tpqsolutions.com.ar        ?
?                             ?           ?                              ?
?  Cloudflared                ?????????????                              ?
?  (Windows Service)          ?  saliente ?                              ?
?  túnel ? subdominio         ?           ????????????????????????????????
???????????????????????????????
```

> **Ventaja clave:** Cloudflared hace conexiones **salientes**, no se necesita abrir puertos ni configurar el router del cliente.

---

## Requisitos previos (una sola vez, desde tu PC)

- Cuenta en [Cloudflare](https://cloudflare.com) con el dominio `tpqsolutions.com.ar`
- Visual Studio con el proyecto `Comercio.NET.SqlBridge` abierto
- Acceso al panel de Railway del proyecto

---

## PARTE 1 — Publicar el SqlBridge (desde tu PC)

### 1.1 Publicar el ejecutable

En Visual Studio, click derecho sobre `Comercio.NET.SqlBridge.Server` ? **Publish** ? perfil `FolderProfile` ? **Publish**.

O desde terminal:

```powershell
cd "C:\Users\Manuel\source\repos\Comercio.NET.SqlBridge\Comercio.NET.SqlBridge.Server"
dotnet publish -c Release -o "C:\SqlBridge"
```

### 1.2 Llevar los archivos al cliente

Copiar toda la carpeta `C:\SqlBridge` al equipo del cliente (USB, carpeta compartida, etc.).
Ruta recomendada en el cliente: `C:\SqlBridge`

---

## PARTE 2 — Configurar el SqlBridge en el cliente

> Todos los comandos de esta sección se ejecutan en **PowerShell como Administrador** en el equipo del cliente.

### 2.1 Editar appsettings.json

Abrir el archivo `C:\SqlBridge\appsettings.json` y configurarlo según la base de datos del cliente.

**?? IMPORTANTE:** editar este archivo **antes** de instalar el servicio.

**Para SQL Server:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "DbEngine": "sqlserver",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=comercio;User Id=michael;Password=michael;TrustServerCertificate=True;"
  }
}
```

**Para PostgreSQL:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "DbEngine": "postgres",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=comercio;Username=postgres;Password=michael;"
  }
}
```

> Si la base de datos está en otro equipo de la red, reemplazar `localhost` por la IP de ese servidor.

### 2.2 Instalar el servicio SqlBridge

```powershell
sc.exe create "SqlBridge" `
    binPath= "C:\SqlBridge\Comercio.NET.SqlBridge.Server.exe" `
    start= auto `
    DisplayName= "Comercio.NET SqlBridge"

sc.exe failure "SqlBridge" reset=60 actions=restart/5000/restart/10000/restart/30000

sc.exe start "SqlBridge"
```

### 2.3 Verificar que el SqlBridge levantó correctamente

```powershell
Start-Sleep -Seconds 5
Invoke-RestMethod http://localhost:5000/health
```

Respuesta esperada (ejemplo para Postgres):
```
status  motor      timestamp
------  -----      ---------
ok      PostgreSQL 2026-06-24T15:38:14...
```

> Si dice `SQL Server` habiendo configurado Postgres, el `appsettings.json` no fue guardado correctamente. Verificar con `Get-Content C:\SqlBridge\appsettings.json` y reiniciar: `sc.exe stop SqlBridge` ? `sc.exe start SqlBridge`.

---

## PARTE 3 — Instalar Cloudflared y crear el túnel

### 3.1 Instalar cloudflared

```powershell
winget install Cloudflare.cloudflared
```

Copiar el exe a una ruta sin espacios (evita problemas con el servicio de Windows):

```powershell
New-Item -ItemType Directory -Path "C:\cloudflared" -Force
Copy-Item "$env:LOCALAPPDATA\Microsoft\WinGet\Links\cloudflared.exe" "C:\cloudflared\cloudflared.exe" -Force
```

### 3.2 Autenticarse con Cloudflare

```powershell
C:\cloudflared\cloudflared.exe tunnel login
```

> Abre el navegador ? iniciar sesión con la cuenta de Cloudflare ? seleccionar el dominio `tpqsolutions.com.ar` ? autorizar.
> Esto guarda las credenciales en `C:\Users\[usuario]\.cloudflared\cert.pem`.

### 3.3 Crear el túnel

Reemplazar `[cliente]` por el nombre real (ej: `pele90`, `victor`, `comercio1`).

```powershell
C:\cloudflared\cloudflared.exe tunnel create sqlbridge-[cliente]
```

Anotar el **ID del túnel** que aparece en pantalla (formato `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`).

Verificar que se creó:
```powershell
C:\cloudflared\cloudflared.exe tunnel list
```

### 3.4 Copiar credenciales al perfil SYSTEM

El servicio de Windows corre como usuario `SYSTEM` y necesita los archivos en una ruta específica:

```powershell
$dst = "C:\Windows\System32\config\systemprofile\.cloudflared"
New-Item -ItemType Directory -Path $dst -Force

# Copiar cert.pem y el .json de credenciales del túnel
Copy-Item "$env:USERPROFILE\.cloudflared\cert.pem" "$dst\" -Force
Copy-Item "$env:USERPROFILE\.cloudflared\*.json"   "$dst\" -Force
```

### 3.5 Crear el archivo config.yml en el perfil SYSTEM

```powershell
$tunnelId   = "PEGAR-EL-ID-DEL-TUNEL-AQUI"
$cliente    = "[cliente]"
$configPath = "C:\Windows\System32\config\systemprofile\.cloudflared\config.yml"
$jsonFile   = "C:\Windows\System32\config\systemprofile\.cloudflared\$tunnelId.json"

@"
tunnel: sqlbridge-$cliente
credentials-file: $jsonFile

ingress:
  - hostname: $cliente.tpqsolutions.com.ar
    service: http://localhost:5000
  - service: http_status:404
"@ | Set-Content $configPath -Encoding UTF8

# Verificar que quedó bien
Get-Content $configPath
```

### 3.6 Crear el registro DNS en Cloudflare

```powershell
C:\cloudflared\cloudflared.exe tunnel route dns sqlbridge-[cliente] [cliente].tpqsolutions.com.ar
```

> Esto crea automáticamente el registro en el panel de Cloudflare como tipo **Tunnel ? Proxied**.

### 3.7 Instalar el servicio cloudflared

```powershell
# Registrar el servicio con la ruta correcta y los argumentos necesarios
sc.exe create "cloudflared" `
    binPath= "C:\cloudflared\cloudflared.exe --config C:\Windows\System32\config\systemprofile\.cloudflared\config.yml tunnel run" `
    start= auto `
    DisplayName= "Cloudflared agent"

sc.exe start "cloudflared"
Start-Sleep -Seconds 10
Get-Service cloudflared
```

> Si el servicio ya existía de una instalación anterior fallida:
> ```powershell
> sc.exe delete "cloudflared"
> Start-Sleep -Seconds 3
> # Luego repetir el sc.exe create de arriba
> ```

### 3.8 Verificar el túnel desde afuera

```powershell
Invoke-RestMethod https://[cliente].tpqsolutions.com.ar/health
```

Respuesta esperada:
```
status  motor      timestamp
------  -----      ---------
ok      PostgreSQL 2026-06-24T15:38:14...
```

---

## PARTE 4 — Configurar Railway

En Railway ? servicio API Mobile ? pestaña **Variables** ? agregar o actualizar:

| Variable | Valor |
|---|---|
| `SQL_BRIDGE_URL` | `https://[cliente].tpqsolutions.com.ar` |

Hacer **redeploy** del servicio para que tome la nueva variable.

### Verificar que Railway conecta

```
https://TU-APP.railway.app/api/health
```

Debe responder con `"hasSqlBridgeUrl": true` y la URL del cliente.

---

## PARTE 5 — Verificación final completa

Ejecutar en el cliente:

```powershell
Write-Host "=== SERVICIOS ===" -ForegroundColor Cyan
Get-Service SqlBridge, cloudflared | Format-Table Name, Status, DisplayName

Write-Host "`n=== SQLBRIDGE LOCAL ===" -ForegroundColor Cyan
Invoke-RestMethod http://localhost:5000/health

Write-Host "`n=== TUNEL CLOUDFLARE ===" -ForegroundColor Cyan
Invoke-RestMethod https://[cliente].tpqsolutions.com.ar/health
```

Todo correcto si los tres pasos responden sin errores y el motor es el esperado.

---

## Referencia rápida: valores por cliente

| Campo | Valor |
|---|---|
| Nombre del túnel | `sqlbridge-[cliente]` |
| Subdominio | `[cliente].tpqsolutions.com.ar` |
| Variable Railway | `SQL_BRIDGE_URL = https://[cliente].tpqsolutions.com.ar` |
| Puerto SqlBridge | `5000` (solo local, no se expone al router) |
| Config del servicio | `C:\Windows\System32\config\systemprofile\.cloudflared\` |
| Ejecutable cloudflared | `C:\cloudflared\cloudflared.exe` |
| Logs SqlBridge | `C:\SqlBridge\logs\sqlbridge_YYYYMMDD.log` |

---

## Solución de problemas frecuentes

### El servicio cloudflared arranca y cae inmediatamente

Verificar que el `config.yml` tiene la ruta correcta al `.json` de credenciales:
```powershell
Get-Content "C:\Windows\System32\config\systemprofile\.cloudflared\config.yml"
Get-ChildItem "C:\Windows\System32\config\systemprofile\.cloudflared\"
```

### El túnel responde error 1033

El servicio cloudflared no está corriendo o el SqlBridge no escucha en el puerto 5000:
```powershell
Get-Service cloudflared, SqlBridge
netstat -ano | findstr ":5000"
```

### El SqlBridge dice "SQL Server" habiendo configurado Postgres

El `appsettings.json` fue sobreescrito por una publicación nueva. Volver a editarlo y reiniciar el servicio:
```powershell
# Editar C:\SqlBridge\appsettings.json manualmente, luego:
sc.exe stop SqlBridge
sc.exe start SqlBridge
Invoke-RestMethod http://localhost:5000/health
```

### Error "date = text" en los logs de Railway

El SqlBridge es una versión anterior que no convierte fechas para PostgreSQL. Republicar con la versión más reciente y reemplazar los archivos en `C:\SqlBridge` (excepto `appsettings.json`).

### El servicio cloudflared ya estaba instalado (de una instalación anterior)

```powershell
Stop-Process -Name "cloudflared" -Force -ErrorAction SilentlyContinue
sc.exe delete "cloudflared"
Start-Sleep -Seconds 5
# Repetir el paso 3.7
```
