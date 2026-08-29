# Especificación Funcional - Instalador Comercio .NET v1.7.0+

**Versión del documento:** 1.0  
**Fecha:** 25 de mayo de 2026  
**Sistema:** Comercio .NET - Sistema de Gestión Comercial  
**Base de datos:** PostgreSQL 16+  
**Framework:** .NET 8.0  

---

## 1. RESUMEN EJECUTIVO

El instalador automatizado de Comercio .NET es un script PowerShell (`instalar.ps1`) que configura todo el entorno necesario para ejecutar el sistema en una PC Windows nueva, incluyendo:

- Runtime de .NET 8
- Motor de base de datos PostgreSQL 16
- Configuración de red para acceso multi-PC
- Restauración completa de la base de datos con datos históricos
- Herramientas de administración opcionales (DBeaver)
- Validaciones post-instalación automáticas

---

## 2. ARQUITECTURA DEL SISTEMA

### 2.1 Componentes principales

```
???????????????????????????????????????????????????
?  SERVIDOR (PC Principal)                        ?
?  ?????????????????????????????????????????????  ?
?  ? Comercio .NET.exe                         ?  ?
?  ? (.NET 8 Desktop Runtime)                  ?  ?
?  ?????????????????????????????????????????????  ?
?                  ?                               ?
?  ?????????????????????????????????????????????  ?
?  ? PostgreSQL 16                             ?  ?
?  ? - Puerto: 5432                            ?  ?
?  ? - Database: comercio                      ?  ?
?  ? - User: postgres                          ?  ?
?  ? - Listen: 0.0.0.0 (todas las interfaces) ?  ?
?  ?????????????????????????????????????????????  ?
???????????????????????????????????????????????????
                     ?
          ???????????????????????
          ?   Red Local TCP     ?
          ?   Puerto 5432       ?
          ???????????????????????
                     ?
     ?????????????????????????????????
     ?                               ?
????????????                  ????????????
? PC       ?                  ? PC       ?
? Cliente  ?                  ? Cliente  ?
? 2        ?                  ? N        ?
????????????                  ????????????
```

### 2.2 Requisitos del sistema

**Servidor (PC principal):**
- Windows 10/11 (64-bit)
- 4 GB RAM mínimo (8 GB recomendado)
- 5 GB espacio en disco
- Conexión a Internet (solo para instalación)
- Permisos de Administrador

**Clientes (PCs secundarias):**
- Windows 10/11 (64-bit)
- 2 GB RAM mínimo
- 500 MB espacio en disco
- Acceso a red local del servidor

---

## 3. PROCESO DE INSTALACIÓN DETALLADO

### 3.1 Invocación del instalador

**Comando remoto (recomendado):**
```powershell
irm https://raw.githubusercontent.com/manuclaro/Comercio.NET-web/recuperado3/instalar.ps1 | iex
```

**Comando local (desde archivo descargado):**
```powershell
.\instalar.ps1 -InstallDir "C:\Comercio.NET" -PgPassword "michael" -PgPort 5432
```

**Parámetros disponibles:**

| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `InstallDir` | String | `C:\Comercio.NET` | Carpeta de instalación |
| `GitHubRepo` | String | `manuclaro/Comercio.NET-web` | Repositorio GitHub |
| `GitHubToken` | String | `""` | Token para repositorios privados |
| `PgPassword` | String | `michael` | Password del usuario postgres |
| `PgPort` | Int | `5432` | Puerto de PostgreSQL |
| `InstallDBeaver` | Bool | `$true` | Instalar DBeaver Community |

### 3.2 Paso 0: Verificación de privilegios

**Objetivo:** Asegurar que el script se ejecuta con permisos de Administrador.

**Acciones:**
1. Verificar si el proceso actual es Administrador
2. Si no lo es:
   - Mostrar advertencia
   - Relanzar PowerShell con elevación (UAC)
   - Pasar todos los parámetros al proceso elevado
   - Terminar el proceso original

**Resultado esperado:**
- Script ejecutándose con privilegios de Administrador

---

### 3.3 Paso 1: Instalación de .NET 8 Desktop Runtime

**Objetivo:** Instalar el runtime necesario para ejecutar la aplicación.

**Verificación previa:**
```powershell
dotnet --list-runtimes | Select-String "Microsoft.WindowsDesktop.App 8.0"
```

**Acciones si no está instalado:**
1. Descargar instalador de .NET 8 Desktop Runtime (x64) desde:
   ```
   https://download.visualstudio.microsoft.com/download/pr/[...]/windowsdesktop-runtime-8.0.12-win-x64.exe
   ```
2. Ejecutar instalador silencioso:
   ```
   windowsdesktop-runtime-8.0-win-x64.exe /quiet /norestart
   ```
3. Verificar código de salida (0 o 3010 = éxito)
4. Eliminar archivo temporal

**Resultado esperado:**
- .NET 8.0 Desktop Runtime instalado
- Salida: `OK .NET 8.0 Desktop Runtime ya instalado.`

**Manejo de errores:**
- Si falla descarga: mostrar URL para descarga manual
- Si falla instalación: continuar con advertencia

---

### 3.4 Paso 2: Instalación de PostgreSQL

**Objetivo:** Instalar motor de base de datos PostgreSQL 16.

**Verificación previa:**
- Buscar `psql.exe` en rutas conocidas:
  ```
  C:\Program Files\PostgreSQL\[16|17|18]\bin
  ```
- Verificar en PATH del sistema

**Acciones si no está instalado:**
1. Descargar instalador de EDB (~300 MB):
   ```
   https://get.enterprisedb.com/postgresql/postgresql-16.6-1-windows-x64.exe
   ```

2. Ejecutar instalación desatendida:
   ```batch
   postgresql-16.6-1-windows-x64.exe ^
     --mode unattended ^
     --unattendedmodeui none ^
     --superpassword "michael" ^
     --serverport 5432 ^
     --servicename postgresql-16 ^
     --enable-components server,commandlinetools
   ```

3. Esperar a que el servicio `postgresql-16` inicie (timeout: 60 segundos)

4. Si el servicio no arranca automáticamente:
   - Intentar `Start-Service postgresql-16`
   - Timeout adicional de 30 segundos

**Resultado esperado:**
- PostgreSQL 16 instalado en `C:\Program Files\PostgreSQL\16\`
- Servicio `postgresql-16` en estado `Running`
- `psql.exe` accesible en PATH

**Manejo de errores:**
- Si descarga falla: mostrar URL manual
- Si servicio no arranca: advertencia pero continuar
- Si no se encuentra `psql.exe` después de instalación: ERROR y salir

---

### 3.5 Paso 3: Configuración de PostgreSQL para conexiones remotas

**Objetivo:** Permitir que otros equipos de la red local se conecten a la base de datos.

#### 3.5.1 Localización del directorio de datos

**Orden de búsqueda:**
1. Ruta por defecto: `C:\Program Files\PostgreSQL\16\data`
2. Leer del servicio Windows (WMI):
   ```powershell
   Get-WmiObject Win32_Service -Filter "Name LIKE 'postgresql%'"
   # Extraer parámetro -D de PathName
   ```

#### 3.5.2 Backup de configuración

Antes de modificar, crear backups:
```
postgresql.conf       ? postgresql.conf.bak.comercionet
pg_hba.conf           ? pg_hba.conf.bak.comercionet
```

#### 3.5.3 Modificación de `postgresql.conf`

**Cambio requerido:**
```ini
# Antes (default):
#listen_addresses = 'localhost'

# Después:
listen_addresses = '*'
```

**Implementación:**
- Leer archivo línea por línea
- Reemplazar línea comentada o existente
- Si no existe, agregar al final
- Escribir con codificación UTF-8 sin BOM

#### 3.5.4 Modificación de `pg_hba.conf`

**Regla a agregar:**
```ini
# Comercio.NET - acceso remoto red local
host    all             all             0.0.0.0/0               md5
```

**Significado:**
- `host`: conexiones TCP/IP
- `all`: todas las bases de datos
- `all`: todos los usuarios
- `0.0.0.0/0`: cualquier dirección IP
- `md5`: autenticación por password cifrado

**Implementación:**
- Leer contenido completo
- Verificar si la regla ya existe
- Si no existe, agregar al final
- Escribir con codificación UTF-8 sin BOM

#### 3.5.5 Validación de sintaxis (opcional)

```powershell
postgres.exe -D "C:\Program Files\PostgreSQL\16\data" -C data_directory
```

- Si retorna error de sintaxis: restaurar backups y salir
- Si retorna valor válido o advertencias menores: continuar

#### 3.5.6 Reinicio del servicio

```powershell
Restart-Service postgresql-16 -Force
```

**Manejo de errores:**
1. Si `Restart-Service` falla: intentar `Start-Service`
2. Si `Start-Service` falla: restaurar backups de configuración
3. Reintentar inicio con configuración original
4. Si sigue fallando: ERROR y salir

#### 3.5.7 Configuración de Firewall de Windows

**Regla a crear:**
```powershell
netsh advfirewall firewall add rule ^
  name="PostgreSQL 5432" ^
  protocol=TCP ^
  dir=in ^
  localport=5432 ^
  action=allow
```

**Verificación previa:**
```powershell
netsh advfirewall firewall show rule name="PostgreSQL 5432"
```

**Resultado esperado:**
- Regla de firewall creada y habilitada
- Puerto TCP 5432 abierto para tráfico entrante

---

### 3.6 Paso 4: Obtención de la última versión desde GitHub

**Objetivo:** Descargar el release más reciente desde GitHub Releases.

**API utilizada:**
```
GET https://api.github.com/repos/manuclaro/Comercio.NET-web/releases/latest
```

**Headers:**
```json
{
  "User-Agent": "ComercioNET-Installer/2.0",
  "Accept": "application/vnd.github.v3+json",
  "Authorization": "Bearer [token]" // opcional
}
```

**Procesamiento de respuesta:**
1. Extraer `tag_name` (ej: `v1.7.0`)
2. Buscar asset con nombre `*.zip`
3. Obtener `browser_download_url` o `url` (si hay token)
4. Extraer tamaño del archivo para mostrar

**Resultado esperado:**
```
OK Version: 1.7.0 (25.3 MB)
```

**Manejo de errores:**
- Si falla conexión: ERROR y salir
- Si no hay assets ZIP: ERROR y salir

---

### 3.7 Paso 5: Descarga y extracción de la aplicación

**Objetivo:** Descargar el ZIP y extraer todos los archivos.

#### 3.7.1 Descarga

**Ubicación temporal:**
```
%TEMP%\ComercioNET_Install_1.7.0.zip
```

**Método:**
```powershell
$wc = New-Object System.Net.WebClient
$wc.DownloadFile($downloadUrl, $tempZip)
```

#### 3.7.2 Extracción

**Ubicación temporal:**
```
%TEMP%\ComercioNET_Install_1.7.0\
```

**Método:**
```powershell
Expand-Archive -LiteralPath $tempZip -DestinationPath $tempExtract -Force
```

**Normalización de estructura:**
- Si el ZIP contiene una sola carpeta raíz, usar su contenido directamente
- Si el ZIP contiene múltiples archivos/carpetas en la raíz, usar la raíz

**Resultado esperado:**
- Todos los archivos extraídos en carpeta temporal
- Estructura lista para copiado

---

### 3.8 Paso 6: Instalación de archivos de la aplicación

**Objetivo:** Copiar archivos al directorio de instalación preservando configuración existente.

#### 3.8.1 Archivos protegidos (no sobrescribir)

```
appsettings.json
loginconfig.json
afip_tokens.json
version.txt
```

**Carpetas protegidas:**
```
Certificados FE/
migrations/
```

#### 3.8.2 Proceso de instalación

**Si es primera instalación:**
1. Crear carpeta `C:\Comercio.NET`
2. Copiar todo el contenido
3. Crear carpetas complementarias:
   ```
   Certificados FE\Testing\
   Certificados FE\Produccion\
   migrations\
   ```

**Si es actualización:**
1. Crear backup temporal de archivos protegidos:
   ```
   %TEMP%\ComercioNET_Backup_[timestamp]\
   ```

2. Copiar archivos protegidos al backup

3. Copiar todo el contenido nuevo (sobrescribe todo)

4. Restaurar archivos protegidos desde backup

5. Eliminar carpeta de backup

#### 3.8.3 Registro de versión

Crear o actualizar `version.txt`:
```
1.7.0
```

**Resultado esperado:**
```
OK Archivos copiados.
>> Restaurado: appsettings.json
>> Restaurado: loginconfig.json
>> Restaurado: Certificados FE
OK Version 1.7.0 registrada.
```

---

### 3.9 Paso 7: Inicialización de base de datos PostgreSQL

**Objetivo:** Crear la base de datos y restaurar estructura y datos.

#### 3.9.1 Verificación de servicio PostgreSQL

**Prerequisito:**
```powershell
Get-Service postgresql* | Where-Object {$_.Status -eq 'Running'}
```

**Si no está corriendo:**
- Intentar `Start-Service`
- Timeout: 30 segundos
- Si falla: ERROR y salir

#### 3.9.2 Creación de la base de datos

**Verificación de existencia:**
```sql
SELECT 1 FROM pg_database WHERE datname='comercio';
```

**Casos:**

**Caso A: Base de datos NO existe**
```powershell
createdb -U postgres -p 5432 -E UTF8 comercio
```

**Caso B: Base de datos existe Y hay dump completo**
```powershell
# Dropear y recrear para instalación limpia
psql -U postgres -p 5432 -d postgres -c "DROP DATABASE IF EXISTS comercio;"
createdb -U postgres -p 5432 -E UTF8 comercio
```

**Caso C: Base de datos existe y NO hay dump**
- No hacer nada, usar BD existente

#### 3.9.3 Restauración de datos

**Orden de prioridad:**

**1. Dump SQL plano (preferido):**
```powershell
# Archivo: database\comercio_inicial.sql
psql -U postgres -p 5432 -d comercio -f "C:\Comercio.NET\database\comercio_inicial.sql"
```

**Ventajas:**
- Compatible entre versiones PostgreSQL 12-18+
- Fácil de inspeccionar y editar
- No requiere `pg_restore`

**2. Dump custom (fallback):**
```powershell
# Archivo: database\comercio_inicial.dump
pg_restore -U postgres -p 5432 -d comercio --no-owner --no-acl --if-exists -c "database\comercio_inicial.dump"
```

**Ventajas:**
- Más compacto
- Más rápido en grandes volúmenes

**Desventaja:**
- Dependiente de versión PostgreSQL

**3. Script DDL vacío (último recurso):**
```powershell
# Archivo: database\init_comercio_pg.sql
psql -U postgres -p 5432 -d comercio -f "database\init_comercio_pg.sql"
```

**Resultado:**
- Solo crea estructura (tablas, índices, claves foráneas)
- **NO** incluye datos

#### 3.9.4 Contenido típico del dump completo

```
Tablas:          37 (artículos, clientes, ventas, etc.)
Secuencias:      35 (para IDs autoincrementales)
Vistas:          2  (vistas de reporting)
Índices:         45 (optimización de consultas)
Claves foráneas: 36 (integridad referencial)

Datos:
- Artículos:         ~2,500 registros
- Clientes:          ~240 registros
- Movimientos stock: ~18,000 registros
- Ventas:            ~680 registros
- Empleados:         ~5 registros
```

**Resultado esperado:**
```
OK Base de datos 'comercio' recreada.
>> Restaurando desde comercio_inicial.sql (formato SQL plano)...
>> CREATE TABLE
>> CREATE SEQUENCE
>> COPY 2498  (artículos)
>> COPY 240   (clientes)
>> COPY 18236 (movimientos stock)
OK Base de datos restaurada desde dump SQL plano.
```

**Manejo de errores:**
- Códigos de salida `psql` != 0: advertencia (puede ser normal en reinstalación)
- Errores de sintaxis: investigar y corregir dump
- Permisos insuficientes: verificar contraseña postgres

---

### 3.10 Paso 8: Configuración de appsettings.json

**Objetivo:** Generar o actualizar la configuración de la aplicación con la IP del servidor.

#### 3.10.1 Detección de IP de red local

**Algoritmo:**
```powershell
Get-NetIPAddress -AddressFamily IPv4 | 
  Where-Object {
    $_.InterfaceAlias -notlike '*Loopback*' -and
    $_.IPAddress -notlike '169.254.*' -and
    $_.IPAddress -notlike '127.*'
  } |
  Sort-Object {
    [System.Net.IPAddress]::Parse($_.IPAddress).GetAddressBytes()[0]
  } -Descending |
  Select-Object -First 1
```

**Resultado ejemplo:**
```
192.168.100.83
```

**Fallback:**
Si no se detecta IP de red, usar `localhost`.

#### 3.10.2 Generación de cadena de conexión

**Formato PostgreSQL (Npgsql):**
```
Host=192.168.100.83;Port=5432;Database=comercio;Username=postgres;Password=michael;
```

**Componentes:**
- `Host`: IP de red del servidor
- `Port`: Puerto de PostgreSQL (default: 5432)
- `Database`: Nombre de la BD (default: comercio)
- `Username`: Usuario de PostgreSQL (default: postgres)
- `Password`: Password configurada en instalación

#### 3.10.3 Creación de appsettings.json (primera instalación)

**Estructura completa:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=192.168.100.83;Port=5432;...",
    "Testing": "Host=192.168.100.83;Port=5432;...",
    "Produccion": "Host=192.168.100.83;Port=5432;..."
  },
  "Comercio": {
    "Nombre": "MI COMERCIO",
    "Domicilio": "Calle 000 N 000 - Ciudad"
  },
  "Facturacion": {
    "RazonSocial": "Nombre Apellido",
    "CUIT": "00-00000000-0",
    "IngBrutos": "00-00000000-0",
    "DomicilioFiscal": "Calle 000 N 000 - Ciudad",
    "CodigoPostal": "0000",
    "InicioActividades": "2020-01-01",
    "Condicion": "Monotributo",
    "PermitirFacturaA": false,
    "PermitirFacturaB": false,
    "PermitirFacturaC": true
  },
  "Validaciones": {
    "ValidarStockDisponible": false
  },
  "CuentasCorrientes": {
    "NombresCtaCte": []
  },
  "AFIP": {
    "AmbienteActivo": "Testing",
    "Testing": {
      "CUIT": "00-00000000-0",
      "CondicionIVA": "Monotributo",
      "PuntoVenta": 1,
      "CertificadoPath": "C:\\Certificados FE\\Testing\\MiCertificadoTesting.p12",
      "CertificadoPassword": "password_del_certificado",
      "WSAAUrl": "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",
      "WSFEUrl": "https://wswhomo.afip.gov.ar/wsfev1/service.asmx",
      "Servicios": { "Facturacion": "wsfe" }
    },
    "Produccion": {
      "CUIT": "00-00000000-0",
      "CondicionIVA": "Monotributo",
      "PuntoVenta": 1,
      "CertificadoPath": "C:\\Certificados FE\\Produccion\\MiCertificado.p12",
      "CertificadoPassword": "password_del_certificado",
      "WSAAUrl": "https://wsaa.afip.gov.ar/ws/services/LoginCms",
      "WSFEUrl": "https://servicios1.afip.gov.ar/wsfev1/service.asmx",
      "Servicios": { "Facturacion": "wsfe" }
    }
  },
  "RestriccionesImpresion": {
    "RestringirRemitoPorPago": false,
    "UsarVistaPrevia": true,
    "LimitarFacturacion": false,
    "MontoLimiteFacturacion": 0.00
  },
  "Descuentos": {
    "OpcionesDisponibles": [ 5, 10, 15, 20 ],
    "PorcentajeMaximo": 20,
    "RestringirPorMetodoPago": false,
    "MetodosPagoPermitidos": [ "Efectivo" ]
  },
  "BaseDatos": {
    "AmbienteActivo": "Testing"
  }
}
```

#### 3.10.4 Actualización de appsettings.json (reinstalación)

**Si el archivo ya existe:**
- Leer contenido JSON
- Reemplazar solo `DefaultConnection` con nueva cadena
- Preservar el resto de configuración
- Guardar con codificación UTF-8

**Resultado esperado:**
```
>> IP de red del servidor: 192.168.100.83
OK appsettings.json actualizado con IP 192.168.100.83.

+-- CADENA DE CONEXION PARA PCs CLIENTES --------------------+
Host=192.168.100.83;Port=5432;Database=comercio;Username=postgres;Password=michael;
Copie esta linea en el appsettings.json de cada PC cliente.
+-------------------------------------------------------------+
```

---

### 3.11 Paso 9: Creación de acceso directo en el escritorio

**Objetivo:** Crear acceso directo para todos los usuarios.

**Ubicación:**
```
C:\Users\Public\Desktop\Comercio .NET.lnk
```

**Propiedades del acceso directo:**
```powershell
$shortcut = $wsh.CreateShortcut($path)
$shortcut.TargetPath       = "C:\Comercio.NET\Comercio .NET.exe"
$shortcut.WorkingDirectory = "C:\Comercio.NET"
$shortcut.Description      = "Comercio .NET - Sistema de Gestion Comercial"
$shortcut.IconLocation     = "C:\Comercio.NET\Comercio .NET.exe, 0"
$shortcut.Save()
```

**Resultado esperado:**
```
OK Acceso directo creado en el escritorio.
```

**Manejo de errores:**
- Si el EXE no existe: advertencia
- Si falla creación del .lnk: advertencia (no crítico)

---

### 3.12 Paso 10: Instalación de DBeaver Community (opcional)

**Objetivo:** Instalar herramienta de administración de bases de datos.

#### 3.12.1 Verificación de instalación existente

**Métodos de detección:**

1. **Rutas conocidas:**
   ```
   C:\Program Files\DBeaver\dbeaver.exe
   C:\Program Files\DBeaver Community\dbeaver.exe
   ```

2. **Registro de Windows:**
   ```
   HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall
   HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall
   ```
   Buscar `DisplayName` que contenga "DBeaver"

#### 3.12.2 Instalación con winget

**Prerequisito:**
```powershell
Get-Command winget -ErrorAction SilentlyContinue
```

**Comando de instalación:**
```powershell
winget install --id DBeaver.DBeaver --accept-package-agreements --accept-source-agreements --silent
```

**Casos:**

**A. DBeaver ya instalado:**
```
OK DBeaver ya esta instalado.
```

**B. Instalación exitosa:**
```
>> Instalando DBeaver Community con winget...
OK DBeaver instalado correctamente.
```

**C. winget no disponible:**
```
!! winget no disponible en este equipo.
>> Instale DBeaver manualmente desde: https://dbeaver.io/download/
```

**D. Instalación omitida por parámetro:**
```powershell
.\instalar.ps1 -InstallDBeaver:$false
```

**Resultado esperado en resumen final:**
```
4. DBeaver instalado: use Host=192.168.100.83, Port=5432, Database=comercio
```

---

### 3.13 Validaciones post-instalación automáticas

**Objetivo:** Verificar que todos los componentes están correctamente instalados y funcionando.

#### V1: Servicio PostgreSQL

**Verificación:**
```powershell
Get-Service postgresql* | Where-Object {$_.Status -eq 'Running'}
```

**Resultado esperado:**
```
OK V1 Servicio PostgreSQL en ejecucion (postgresql-16).
```

#### V2: Conexión PostgreSQL local

**Verificación:**
```powershell
psql -U postgres -p 5432 -d postgres -tAc "SELECT 1;"
```

**Resultado esperado:**
```
OK V2 Conexion PostgreSQL local correcta (SELECT 1).
```

#### V3: Existencia de base de datos

**Verificación:**
```sql
SELECT 1 FROM pg_database WHERE datname='comercio';
```

**Resultado esperado:**
```
OK V3 Base de datos 'comercio' existe.
```

#### V4: Regla de firewall

**Verificación:**
```powershell
netsh advfirewall firewall show rule name="PostgreSQL 5432"
```

**Resultado esperado:**
```
OK V4 Regla de firewall para puerto 5432 presente.
```

#### V5: Configuración appsettings.json

**Verificación:**
```powershell
Get-Content "C:\Comercio.NET\appsettings.json" | Select-String "Host=192.168.100.83"
```

**Resultado esperado:**
```
OK V5 appsettings.json contiene la cadena de conexion esperada.
```

#### Resultado general

**Si todas las validaciones pasan:**
```
OK Validaciones post-instalacion: COMPLETAS.
```

**Si alguna falla:**
```
!! Validaciones post-instalacion: CON ERRORES. Revise mensajes anteriores.
```

---

## 4. PANTALLA DE RESUMEN FINAL

```
============================================================
  INSTALACION COMPLETADA -- Comercio .NET v1.7.0
============================================================

  Carpeta     : C:\Comercio.NET
  Base datos  : PostgreSQL 16.6 >> comercio en 192.168.100.83:5432

  PASOS OBLIGATORIOS ANTES DE USAR:
  1. Editar appsettings.json:
     C:\Comercio.NET\appsettings.json
     - Comercio.Nombre y Comercio.Domicilio
     - Facturacion: CUIT, RazonSocial, Condicion
     - AFIP: CUIT, PuntoVenta, rutas de certificados

  2. Copiar certificados AFIP (.p12/.pfx) en:
     C:\Comercio.NET\Certificados FE

  CONEXION DESDE OTRAS PCS:
  Host=192.168.100.83;Port=5432;Database=comercio;Username=postgres;Password=michael;

  USUARIO BD: postgres  |  Password: michael

  3. Iniciar la aplicacion desde el acceso directo del escritorio.
  4. DBeaver instalado: use Host=192.168.100.83, Port=5432, Database=comercio
============================================================

Desea abrir la carpeta de instalacion ahora? (S/N):
```

---

## 5. CONFIGURACIÓN DE PCS CLIENTES

### 5.1 Opciones de despliegue

**Opción A: Instalador completo (sin PostgreSQL)**

Ejecutar el mismo `instalar.ps1` pero PostgreSQL ya detectado en red:
- Se instala .NET 8
- Se descarga la app desde GitHub
- Se crea appsettings.json apuntando al servidor
- **NO** se instala PostgreSQL localmente

**Opción B: Copia manual**

1. Copiar carpeta `C:\Comercio.NET` desde el servidor
2. Editar `appsettings.json`:
   ```json
   "DefaultConnection": "Host=192.168.100.83;Port=5432;Database=comercio;Username=postgres;Password=michael;"
   ```
3. Crear acceso directo manualmente

### 5.2 Validación de conectividad desde cliente

**Desde PowerShell:**
```powershell
Test-NetConnection -ComputerName 192.168.100.83 -Port 5432
```

**Esperado:**
```
TcpTestSucceeded : True
```

**Con psql (si está instalado):**
```powershell
$env:PGPASSWORD="michael"
psql -h 192.168.100.83 -p 5432 -U postgres -d comercio -c "SELECT NOW();"
```

---

## 6. GENERACIÓN DE PAQUETES DE DISTRIBUCIÓN

### 6.1 Script: build_and_package.bat

**Objetivo:** Crear paquete ZIP listo para subir a GitHub Releases.

**Ubicación:** Raíz del repositorio

**Parámetros configurables (editar al inicio del .bat):**
```batch
set PG_BIN=C:\Program Files\PostgreSQL\18\bin
set PG_USER=postgres
set PG_PORT=5433
set PG_DB=comercio
set PGPASSWORD=michael
```

### 6.2 Proceso de empaquetado

**Paso 1: Compilación**
```batch
dotnet build "Comercio.NET.sln" -c Release
```

**Paso 2: Copia de archivos**
```batch
xcopy "bin\Release\net8.0-windows\*" "Releases\v1.7.0\app\" /E /I /Y /Q
```

**Archivos excluidos:**
- `appsettings.json` (configuración específica del dev)
- `*.db` (bases de datos SQLite legacy)
- `*.log` (logs de desarrollo)

**Paso 3: Generación de dump SQL**

**3a. Ejecutar pg_dump:**
```batch
pg_dump.exe -U postgres -p 5433 -d comercio ^
  --format=plain ^
  --no-owner ^
  --no-acl ^
  --no-table-access-method ^
  --no-tablespaces ^
  --no-comments ^
  --file="Releases\v1.7.0\app\database\comercio_inicial.sql"
```

**Opciones explicadas:**
- `--format=plain`: SQL legible (no binario)
- `--no-owner`: omitir propietarios (compatibilidad)
- `--no-acl`: omitir permisos (compatibilidad)
- `--no-table-access-method`: omitir métodos de acceso (PG 12+)
- `--no-tablespaces`: omitir tablespaces (portabilidad)
- `--no-comments`: omitir comentarios (menos tamaño)

**3b. Limpieza de comandos PostgreSQL 18:**
```powershell
$dump = Get-Content "comercio_inicial.sql"
$clean = $dump -notmatch '^\s*\\(un)?restrict'
[IO.File]::WriteAllLines($dump, $clean, [Text.UTF8Encoding]::new($false))
```

**Elimina:**
- `\restrict [token]` (comando de seguridad PG 18)
- `\unrestrict` (comando de seguridad PG 18)

**Paso 4: Copia de script DDL fallback**
```batch
copy database\init_comercio_pg.sql Releases\v1.7.0\app\database\
```

**Paso 5: Creación del ZIP**
```powershell
Compress-Archive -Path "Releases\v1.7.0\app\*" ^
  -DestinationPath "Releases\v1.7.0\ComercioNET_v1.7.0.zip" ^
  -CompressionLevel Optimal -Force
```

**Paso 6: Generación de version.json**
```json
{
  "Version": "1.7.0",
  "DownloadUrl": "https://github.com/manuclaro/Comercio.NET-web/releases/download/v1.7.0/ComercioNET_v1.7.0.zip",
  "ReleaseDate": "2026-05-25T12:00:00",
  "IsRequired": false,
  "FileSize": 0,
  "ChangeLog": [
    "NUEVO: Describe las nuevas funcionalidades",
    "MEJORA: Describe las mejoras realizadas",
    "CORRECCION: Describe los bugs corregidos"
  ]
}
```

### 6.3 Contenido final del ZIP

```
ComercioNET_v1.7.0.zip (26.2 MB)
?
??? Comercio .NET.exe                  (ejecutable principal)
??? Comercio .NET.dll                  (librería principal)
??? appsettings.json.example           (template de configuración)
??? [~200 DLLs de dependencias]
?
??? database\
?   ??? comercio_inicial.sql           (4.3 MB - dump completo)
?   ??? init_comercio_pg.sql           (28 KB - DDL vacío)
?
??? runtimes\                          (runtimes nativos)
??? es\                                (recursos localizados)
??? fr\
??? ...
```

---

## 7. PUBLICACIÓN EN GITHUB RELEASES

### 7.1 Creación del release

1. Ir a https://github.com/manuclaro/Comercio.NET-web/releases
2. Click en "Draft a new release"
3. Tag version: `v1.7.0`
4. Release title: `Comercio .NET v1.7.0`
5. Description (ejemplo):
   ```markdown
   ## Novedades v1.7.0

   ### ?? Nuevo
   - Migración completa a PostgreSQL
   - Soporte multi-PC en red local
   - Instalador automatizado con validaciones

   ### ? Mejoras
   - Rendimiento mejorado en consultas de stock
   - Interfaz optimizada para pantallas táctiles

   ### ?? Correcciones
   - Corregido error en cálculo de IVA en facturas B
   - Solucionado problema de sincronización en red

   ## ?? Instalación

   **PC Servidor (principal):**
   ```powershell
   irm https://raw.githubusercontent.com/manuclaro/Comercio.NET-web/recuperado3/instalar.ps1 | iex
   ```

   **Requisitos:**
   - Windows 10/11 (64-bit)
   - 4 GB RAM
   - Conexión a Internet (solo para instalación)
   ```

6. Adjuntar archivos:
   - `ComercioNET_v1.7.0.zip` (26.2 MB)
   - `version.json`

7. Marcar como "Latest release"

8. Publicar

### 7.2 Actualización de instalar.ps1 en el repositorio

**Importante:** El script `instalar.ps1` debe estar en la rama `master` o `recuperado3` para que el comando remoto funcione:

```bash
git add instalar.ps1 build_and_package.bat check_post_install.ps1
git commit -m "release: v1.7.0 con soporte PostgreSQL y multi-PC"
git push origin recuperado3
```

---

## 8. SCRIPT DE DIAGNÓSTICO

### 8.1 check_post_install.ps1

**Objetivo:** Script independiente para validar instalación en cualquier momento.

**Ubicación:** Raíz del repositorio

**Uso:**
```powershell
.\check_post_install.ps1 -PgPort 5432 -PgUser postgres -PgPassword michael -DbName comercio -InstallDir "C:\Comercio.NET"
```

**Parámetros:**

| Parámetro | Default | Descripción |
|-----------|---------|-------------|
| `PgPort` | `5432` | Puerto de PostgreSQL |
| `PgUser` | `postgres` | Usuario de PostgreSQL |
| `PgPassword` | `michael` | Password de PostgreSQL |
| `DbName` | `comercio` | Nombre de la base de datos |
| `InstallDir` | `C:\Comercio.NET` | Carpeta de instalación |

**Validaciones realizadas:**
1. Servicio PostgreSQL en ejecución
2. `psql.exe` accesible
3. Conexión a PostgreSQL con `SELECT 1`
4. Existencia de base de datos `comercio`
5. Regla de firewall para puerto configurado
6. `appsettings.json` con cadena PostgreSQL

**Salida ejemplo:**
```
[OK]   Servicio PostgreSQL en ejecucion (postgresql-16).
[OK]   Conexion local a PostgreSQL correcta (SELECT 1).
[OK]   Base de datos 'comercio' existe.
[OK]   Regla de firewall para puerto 5432 encontrada.
[OK]   appsettings.json contiene DefaultConnection PostgreSQL.

Resultado general: OK
```

**Código de salida:**
- `0`: Todas las validaciones OK
- `1`: Una o más validaciones fallaron

---

## 9. TROUBLESHOOTING

### 9.1 PostgreSQL no arranca después de configuración

**Síntomas:**
```
!! No se pudo reiniciar PostgreSQL: No pudo iniciarse el servicio 'postgresql-16'
```

**Causa más común:**
- Error de sintaxis en `postgresql.conf` o `pg_hba.conf`

**Solución:**
1. Revisar logs de PostgreSQL:
   ```
   C:\Program Files\PostgreSQL\16\data\log\postgresql-*.log
   ```

2. Restaurar backups automáticos:
   ```
   postgresql.conf.bak.comercionet
   pg_hba.conf.bak.comercionet
   ```

3. Reintentar inicio del servicio:
   ```powershell
   Start-Service postgresql-16
   ```

### 9.2 Error "versión no soportada" al restaurar dump

**Síntomas:**
```
pg_restore: error: versión no soportada (1.16) en el encabezado del archivo
```

**Causa:**
- Dump generado con PostgreSQL 18 incompatible con PostgreSQL 16

**Solución:**
- El instalador ya está configurado para buscar primero el dump SQL plano (`comercio_inicial.sql`) que es compatible
- Si persiste, regenerar el paquete con `build_and_package.bat` actualizado

### 9.3 Comando `\restrict` no válido

**Síntomas:**
```
psql: error: orden \restrict no válida
```

**Causa:**
- Comando exclusivo de PostgreSQL 18 en el dump

**Solución:**
- Regenerar paquete con `build_and_package.bat` v1.7.0+ que incluye limpieza automática
- O ejecutar manualmente:
  ```powershell
  $dump = "C:\Comercio.NET\database\comercio_inicial.sql"
  $lines = Get-Content $dump
  $clean = $lines -notmatch '^\s*\\(un)?restrict'
  $clean | Set-Content $dump -Encoding UTF8
  ```

### 9.4 No se puede conectar desde PC cliente

**Síntomas:**
```
Connection refused (0x0000274D/10061)
```

**Causas posibles:**

**A. Firewall de Windows bloqueando:**
```powershell
# Verificar regla
netsh advfirewall firewall show rule name="PostgreSQL 5432"

# Crear manualmente si falta
netsh advfirewall firewall add rule name="PostgreSQL 5432" protocol=TCP dir=in localport=5432 action=allow
```

**B. PostgreSQL no escuchando en todas las interfaces:**
```ini
# Verificar en postgresql.conf
listen_addresses = '*'  # Debe estar así, no 'localhost'
```

**C. pg_hba.conf no permite conexiones remotas:**
```ini
# Debe existir esta línea en pg_hba.conf
host    all             all             0.0.0.0/0               md5
```

**D. IP incorrecta en appsettings.json del cliente:**
```json
"DefaultConnection": "Host=192.168.100.83;Port=5432;..."
                           ^^^^^^^^^^^^^ debe ser IP del servidor
```

**Validación desde cliente:**
```powershell
Test-NetConnection -ComputerName 192.168.100.83 -Port 5432
```

### 9.5 Aplicación no inicia - error "Microsoft.Data.SqlClient"

**Síntomas:**
```
System.IO.FileNotFoundException: Could not load file or assembly 'Microsoft.Data.SqlClient'
```

**Causa:**
- Versión antigua de la app que todavía usa SQL Server

**Solución:**
- Descargar versión v1.7.0 o superior que usa Npgsql (PostgreSQL)
- Verificar que el ZIP descargado sea el correcto (26.2 MB)

---

## 10. MANTENIMIENTO Y ACTUALIZACIÓN

### 10.1 Actualización a nueva versión (ej: v1.8.0)

**En el servidor:**
```powershell
irm https://raw.githubusercontent.com/manuclaro/Comercio.NET-web/recuperado3/instalar.ps1 | iex
```

**El instalador:**
1. Detecta versión nueva en GitHub
2. Hace backup de configuración actual
3. Descarga y extrae nueva versión
4. Restaura configuración personalizada
5. **NO** toca la base de datos (preserva datos)

**En los clientes:**
- Repetir el mismo proceso
- O copiar manualmente la carpeta actualizada desde el servidor

### 10.2 Backup de base de datos

**Backup completo:**
```powershell
$env:PGPASSWORD="michael"
& "C:\Program Files\PostgreSQL\16\bin\pg_dump.exe" ^
  -U postgres -p 5432 -d comercio ^
  --format=custom ^
  --file="C:\Backups\comercio_$(Get-Date -Format 'yyyyMMdd_HHmmss').dump"
```

**Restauración:**
```powershell
$env:PGPASSWORD="michael"
& "C:\Program Files\PostgreSQL\16\bin\pg_restore.exe" ^
  -U postgres -p 5432 -d comercio_nuevo ^
  --no-owner --no-acl ^
  "C:\Backups\comercio_20260525_230000.dump"
```

### 10.3 Migración de datos desde SQL Server (legacy)

**Herramientas recomendadas:**
1. **DBeaver** (incluido en instalación)
2. **pgAdmin 4** (instalación manual)
3. **Scripts ETL personalizados**

**Proceso general:**
1. Exportar tablas desde SQL Server a CSV
2. Ajustar tipos de datos si es necesario
3. Importar a PostgreSQL con `COPY`:
   ```sql
   COPY articulos(codigo, nombre, precio, stock)
   FROM 'C:/export/articulos.csv'
   DELIMITER ','
   CSV HEADER;
   ```

---

## 11. ARQUITECTURA DE ARCHIVOS

### 11.1 Estructura del repositorio

```
Comercio.NET-web/
?
??? instalar.ps1                    (instalador automatizado)
??? build_and_package.bat           (empaquetador de releases)
??? check_post_install.ps1          (script de diagnóstico)
?
??? database/
?   ??? init_comercio_pg.sql        (DDL PostgreSQL vacío)
?   ??? migrations/                 (migraciones de esquema)
?
??? Releases/
?   ??? v1.7.0/
?       ??? ComercioNET_v1.7.0.zip  (paquete de distribución)
?       ??? version.json            (metadata del release)
?       ??? app/                    (contenido desempaquetado)
?
??? src/
?   ??? Comercio.NET.csproj
?   ??? Program.cs
?   ??? Forms/
?   ??? Services/
?   ??? Models/
?
??? docs/
    ??? INSTALADOR_ESPECIFICACION.md (este documento)
    ??? API_AFIP.md
    ??? MANUAL_USUARIO.md
```

### 11.2 Estructura de instalación en el cliente

```
C:\Comercio.NET\
?
??? Comercio .NET.exe               (ejecutable principal)
??? Comercio .NET.dll
??? appsettings.json                (configuración)
??? loginconfig.json                (usuarios)
??? afip_tokens.json                (tokens AFIP cached)
??? version.txt                     (versión actual)
?
??? database/
?   ??? comercio_inicial.sql        (dump completo)
?   ??? init_comercio_pg.sql        (DDL vacío)
?
??? Certificados FE/
?   ??? Testing/
?   ?   ??? MiCertificadoTesting.p12
?   ??? Produccion/
?       ??? MiCertificado.p12
?
??? migrations/                     (historial de cambios de esquema)
?
??? [~200 DLLs y dependencias]
```

---

## 12. SEGURIDAD

### 12.1 Credenciales de PostgreSQL

**Por defecto:**
- Usuario: `postgres`
- Password: `michael`
- Puerto: `5432`

**Recomendaciones para producción:**

1. **Cambiar password después de instalación:**
   ```sql
   ALTER USER postgres WITH PASSWORD 'nueva_password_segura';
   ```

2. **Actualizar appsettings.json en todos los equipos:**
   ```json
   "DefaultConnection": "Host=...;Password=nueva_password_segura;"
   ```

3. **Restringir acceso por IP en pg_hba.conf:**
   ```ini
   # En vez de 0.0.0.0/0, especificar red local:
   host    all    all    192.168.100.0/24    md5
   ```

### 12.2 Certificados AFIP

**Ubicación:**
```
C:\Comercio.NET\Certificados FE\
```

**Permisos recomendados:**
- Solo usuarios administradores deben tener acceso
- Aplicar cifrado EFS a la carpeta (Windows Pro/Enterprise)

**Backup:**
- Copiar certificados a ubicación segura externa
- No incluir en el control de versiones (Git)

---

## 13. LIMITACIONES CONOCIDAS

### 13.1 Versiones de PostgreSQL soportadas

- **Mínimo:** PostgreSQL 12
- **Máximo:** PostgreSQL 18
- **Recomendado:** PostgreSQL 16

### 13.2 Sistemas operativos soportados

- **Soportado:** Windows 10 (64-bit), Windows 11
- **No soportado:** Windows 7, Windows 8, Windows 32-bit

### 13.3 Tamaño de base de datos

- **Práctico:** Hasta 100,000 artículos
- **Probado:** Hasta 500,000 movimientos de stock
- **Límite teórico PostgreSQL:** 32 TB por base de datos

---

## 14. CONTACTO Y SOPORTE

**Repositorio GitHub:**  
https://github.com/manuclaro/Comercio.NET-web

**Issues y bugs:**  
https://github.com/manuclaro/Comercio.NET-web/issues

**Wiki:**  
https://github.com/manuclaro/Comercio.NET-web/wiki

---

## 15. CHANGELOG DE VERSIONES

### v1.7.0 (2026-05-25)
- ? Migración completa a PostgreSQL
- ? Instalador automatizado con PowerShell
- ? Soporte multi-PC en red local
- ? Configuración automática de firewall
- ? Validaciones post-instalación
- ? Dump SQL compatible entre versiones PG
- ? Instalación opcional de DBeaver
- ? Script de diagnóstico independiente

### v1.6.x (legacy)
- Base de datos: SQL Server LocalDB
- Solo single-PC
- Instalación manual

---

**Fin del documento**
