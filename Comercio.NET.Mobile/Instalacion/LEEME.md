# Instalación de Comercio.NET Web

## Requisitos del Servidor

- Windows 10 (versión 1607 o superior), Windows 11, o Windows Server 2012+
- Mínimo 2 GB de RAM (recomendado 4 GB)
- Puerto 8080 disponible (o el que se configure)
- Conexión a la red local donde estarán los dispositivos

## Pasos para instalar en un nuevo cliente

### 1. Publicar la aplicación (desde tu PC de desarrollo)

```powershell
cd "C:\Users\Manuel\source\repos\Comercio .NET"
.\Comercio.NET.Mobile\Instalacion\publicar.ps1
```

Esto genera los archivos compilados en `Comercio.NET.Mobile\Instalacion\publish\`.

### 2. Copiar al servidor del cliente

Copie toda la carpeta `Instalacion` al servidor del cliente (USB, red, etc.):
```
Instalacion\
??? instalar-comercio-web.ps1    ? Script principal
??? crear-base-datos.sql          ? Esquema de BD
??? publicar.ps1                  ? (solo para desarrollo)
??? publish\                      ? Archivos de la aplicación
?   ??? Comercio.NET.Mobile.Server.dll
?   ??? wwwroot\
?   ??? ...
??? LEEME.md                      ? Este archivo
```

### 3. Ejecutar el instalador en el servidor

Abrir **PowerShell como Administrador** y ejecutar:

```powershell
cd C:\ruta\donde\copio\Instalacion
.\instalar-comercio-web.ps1
```

El script le pedirá:
- Datos del comercio (nombre, CUIT, domicilio)
- Datos de conexión a PostgreSQL
- Ruta del certificado AFIP
- Puerto deseado

### 4. Verificar la instalación

Desde el servidor, abrir un navegador en: `http://localhost:8080`

Desde otros dispositivos en la misma red: `http://[IP-DEL-SERVIDOR]:8080`

## Opciones del instalador

```powershell
# Instalar en una ruta personalizada
.\instalar-comercio-web.ps1 -RutaInstalacion "D:\MiComercio"

# Instalar sin PostgreSQL (ya está en otro servidor)
.\instalar-comercio-web.ps1 -SinPostgres

# Instalar sin registrar como servicio de Windows
.\instalar-comercio-web.ps1 -SinServicio
```

## Después de instalar

### Acceso desde dispositivos

| Dispositivo | URL |
|---|---|
| PC en la misma red | `http://192.168.x.x:8080` |
| Tablet WiFi | `http://192.168.x.x:8080` |
| Celular WiFi | `http://192.168.x.x:8080` |

### Credenciales por defecto

- **Usuario:** admin
- **Contraseña:** admin
- ?? **Cambiar la contraseña** después del primer inicio

### Administrar el servicio

```powershell
# Ver estado
Get-Service ComercioNETWeb

# Reiniciar
Restart-Service ComercioNETWeb

# Detener
Stop-Service ComercioNETWeb

# Ver logs
Get-Content "C:\ComercioWeb\logs\*" -Tail 50
```

### Modificar configuración

Editar `C:\ComercioWeb\appsettings.json` y reiniciar el servicio:
```powershell
Restart-Service ComercioNETWeb
```

## Solución de problemas

| Problema | Solución |
|---|---|
| No se puede acceder desde otro dispositivo | Verificar firewall y que estén en la misma red |
| Error de base de datos | Verificar que PostgreSQL esté corriendo y los datos de conexión |
| Error de AFIP | Verificar ruta del certificado y que no esté vencido |
| La app no inicia | Revisar logs en `C:\ComercioWeb\logs\` |
