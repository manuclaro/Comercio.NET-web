# Guía de Instalación — Comercio.NET Web

## Requisitos previos

- PC de desarrollo con Visual Studio y el proyecto `Comercio.NET` abierto
- PostgreSQL 18 instalado localmente
- Acceso a internet
- Cuenta GitHub `manuclaro` con Personal Access Token

---

## PARTE 1 — Preparar el instalador desde tu PC de desarrollo

Estos pasos se realizan **cada vez que quieras distribuir una nueva versión** a un cliente.

### Paso 1 — Publicar la aplicación

Abre PowerShell en la carpeta raíz del proyecto y ejecuta:

```powershell
dotnet publish "Comercio.NET.Mobile\Comercio.NET.Mobile.Server\Comercio.NET.Mobile.Server.csproj" `
    -c Release -o "Comercio.NET.Mobile\Instalacion\publish" --self-contained false
```

> ⚠️ Siempre publicar antes de distribuir para incluir los últimos cambios.

### Paso 2 — Generar el backup de la base de datos

El backup incluye estructura de tablas, productos y el usuario `admin`.

```powershell
PowerShell -ExecutionPolicy Bypass -File "Comercio.NET.Mobile\Instalacion\generar-backup.ps1"
```

Genera `backup-comercio.sql` en formato SQL plano (compatible con cualquier versión de PostgreSQL).

### Paso 3 — Verificar archivos en la carpeta Instalacion

| Archivo | Descripción |
|---|---|
| `instalar.ps1` | Script principal del instalador |
| `publish\` | Archivos compilados de la aplicación |
| `backup-comercio.sql` | Backup de la base de datos |
| `nssm.exe` | Herramienta para registrar el servicio de Windows |
| `aspnetcore-runtime-8-win-x64.exe` | Instalador de .NET 8 |
| `FETesting.p12` | Certificado AFIP de Testing |

### Paso 4 — Subir el instalador a GitHub

```powershell
PowerShell -ExecutionPolicy Bypass -File "Comercio.NET.Mobile\Instalacion\publicar-github.ps1"
```

Pide el Personal Access Token de GitHub, genera el ZIP y lo sube al release `v1.7.0`.

> Para obtener o renovar el token: https://github.com/settings/tokens
> → "Generate new token (classic)" → tildar permiso `repo`

---

## PARTE 2 — Instalar en el PC del cliente

### Requisitos del cliente

- Windows 10 64-bit o Windows 11
- Mínimo 4 GB de RAM
- Conexión a internet (para descargar PostgreSQL ~230 MB la primera vez)
- PowerShell 5.1 o superior (incluido en Windows 10/11)

### Paso 1 — Descargar e instalar

Abrir **PowerShell como Administrador** y ejecutar:

```powershell
$url = "https://github.com/manuclaro/Comercio.NET-web/releases/download/v1.7.0/ComercioNET-Instalacion.zip"
Invoke-WebRequest $url -OutFile "C:\inst.zip"
Expand-Archive "C:\inst.zip" "C:\Instalacion" -Force
PowerShell -ExecutionPolicy Bypass -File "C:\Instalacion\instalar.ps1"
```

### Paso 2 — Completar el onboarding

El instalador hace solo 4 preguntas:

| Pregunta | Ejemplo |
|---|---|
| Nombre del comercio | `Almacen El Sol` |
| Domicilio | `Av. San Martín 123` |
| CUIT | Enter para usar `20-28069473-9` |
| Ingresos Brutos | Enter para usar el mismo CUIT |

El resto es **completamente automático**.

### Paso 3 — Qué instala automáticamente

| Componente | Detalle |
|---|---|
| .NET 8 Runtime | Desde archivo local incluido en el ZIP |
| PostgreSQL 18 | Descarga ~230 MB, instala en `C:\PostgreSQL\18\` |
| Base de datos | Crea BD `comercio` y restaura el backup con productos y usuario |
| Certificado AFIP | Copia `FETesting.p12` a `C:\ComercioWeb\Certificados\Testing\` |
| Aplicación web | Copia archivos a `C:\ComercioWeb\` y genera `appsettings.json` |
| Servicio Windows | Registra `ComercioNETWeb` con inicio automático |
| Firewall | Abre el puerto 8080 para acceso desde la red local |

### Paso 4 — Verificar la instalación

Al finalizar el instalador muestra la IP del servidor. Desde cualquier dispositivo en la misma red:

```
http://[IP-DEL-SERVIDOR]:8080
```

**Credenciales iniciales:**
- Usuario: `admin`
- Contraseña: `admin1`

> ⚠️ Cambiar la contraseña del admin después del primer ingreso.

---

## PARTE 3 — Administración del servicio

```powershell
# Ver estado
Get-Service ComercioNETWeb

# Reiniciar (después de cambiar appsettings.json)
Restart-Service ComercioNETWeb

# Detener / Iniciar
Stop-Service ComercioNETWeb
Start-Service ComercioNETWeb

# Ver log de errores (últimas 50 líneas)
Get-Content "C:\ComercioWeb\logs\stderr.log" -Tail 50

# Seguir el log en tiempo real
Get-Content "C:\ComercioWeb\logs\stdout.log" -Wait -Tail 20
```

Editar configuración:
```powershell
notepad C:\ComercioWeb\appsettings.json
Restart-Service ComercioNETWeb
```

---

## PARTE 4 — Solución de problemas comunes

| Problema | Solución |
|---|---|
| Error `ejecución de scripts deshabilitada` | Usar `PowerShell -ExecutionPolicy Bypass -File ...` |
| No se puede acceder desde otro dispositivo | Verificar que estén en la misma red WiFi/LAN |
| Error de base de datos al ingresar | Verificar `appsettings.json` y que el servicio `postgresql-18` esté corriendo |
| El servicio no inicia | Ejecutar `dotnet C:\ComercioWeb\Comercio.NET.Mobile.Server.dll` para ver el error exacto |
| PostgreSQL no inicia | Ejecutar `icacls C:\PostgreSQL /grant "Todos:(OI)(CI)F" /T` |
| ZIP no descarga | Descargar manualmente desde `https://github.com/manuclaro/Comercio.NET-web/releases` |

---

## PARTE 5 — Actualizar a una nueva versión

```powershell
$url = "https://github.com/manuclaro/Comercio.NET-web/releases/download/v1.7.0/ComercioNET-Instalacion.zip"
Invoke-WebRequest $url -OutFile "C:\inst.zip"
Expand-Archive "C:\inst.zip" "C:\Instalacion" -Force
PowerShell -ExecutionPolicy Bypass -File "C:\Instalacion\instalar.ps1"
```

El instalador detiene el servicio, sobreescribe los archivos y lo vuelve a iniciar.
**La base de datos y la configuración existente no se modifican.**

---

## Estructura de carpetas en el servidor instalado

```
C:\ComercioWeb\
├── Comercio.NET.Mobile.Server.dll   <- aplicación principal
├── appsettings.json                 <- configuración (BD, AFIP, comercio)
├── wwwroot\                         <- archivos web (HTML, CSS, JS)
├── nssm.exe                         <- gestor del servicio
├── logs\
│   ├── stdout.log                   <- log de la aplicación
│   └── stderr.log                   <- log de errores
└── Certificados\
    ├── Testing\
    │   └── FETesting.p12            <- certificado AFIP testing
    └── Produccion\                  <- para cuando pase a producción

C:\PostgreSQL\18\
├── bin\                             <- herramientas PostgreSQL (psql, pg_dump, etc.)
└── data\                            <- datos de la base de datos
```
