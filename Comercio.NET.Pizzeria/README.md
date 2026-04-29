# Comercio.NET Pizzería

Sistema de gestión para pizzería/bar independiente, extraído del proyecto principal Comercio.NET.

## Características

- **Gestión de Mesas**: Apertura, seguimiento y cierre de mesas
- **Mozos**: CRUD completo de mozos
- **Productos de Bar**: Gestión de productos y precios
- **Formas de Pago**: Configuración de métodos de pago
- **Ventas del Día**: Resumen de ventas por mesa
- **Reportes**: Ventas por producto en rangos de fechas

## Estructura del Proyecto

```
Comercio.NET.Pizzeria/
??? Comercio.NET.Pizzeria.sln          # Solución independiente
??? Comercio.NET.Pizzeria.Server/
    ??? Controllers/
    ?   ??? AuthController.cs          # Login/autenticación
    ?   ??? MesasController.cs         # API completa de pizzería
    ??? Services/
    ?   ??? AuthService.cs             # Autenticación (usuario: pizzeria)
    ?   ??? MesasService.cs            # Lógica de negocio
    ?   ??? IMesasService.cs           # Interface del servicio
    ?   ??? JsonSerializerDefaults.cs  # Configuración JSON
    ??? Models/
    ?   ??? MesaDto.cs                 # DTOs de mesas y relacionados
    ?   ??? FormaPagoDto.cs            # DTO de formas de pago
    ?   ??? VentaProductoDto.cs        # DTO de ventas
    ?   ??? Usuario.cs                 # DTO de usuario/login
    ??? wwwroot/
        ??? index.html                  # Redirect al dashboard
        ??? login.html                  # Página de login
        ??? dashboard-pizzeria.html     # Dashboard principal
        ??? mesas.html                  # Gestión de mesas
        ??? mozos.html                  # Gestión de mozos
        ??? productos-bar.html          # Gestión de productos
        ??? formas-pago.html            # Gestión de formas de pago
        ??? ventas-mesas.html           # Reporte de ventas
        ??? js/                         # JavaScript
        ??? css/                        # Estilos
```

## Requisitos

- **.NET 8 SDK**
- **SQL Bridge**: URL del servicio SQL Bridge configurado
- **Base de datos SQL Server** con las siguientes tablas:
  - `Mesas`
  - `MesasItems`
  - `Mozos`
  - `ProductosBar`
  - `FormasPago`

## Configuración

### 1. Variable de Entorno

Configurar la URL del SQL Bridge:

```bash
# Windows PowerShell
$env:SQL_BRIDGE_URL = "http://localhost:5000"

# Windows CMD
set SQL_BRIDGE_URL=http://localhost:5000

# Linux/Mac
export SQL_BRIDGE_URL=http://localhost:5000
```

### 2. Puerto de la Aplicación

Por defecto usa el puerto **8081**. Para cambiarlo:

```bash
$env:PORT = "8082"
```

### 3. Credenciales

**Usuario:** `pizzeria`  
**Contraseña:** `pizzeria`

## Ejecución

```bash
cd Comercio.NET.Pizzeria/Comercio.NET.Pizzeria.Server
dotnet run
```

La aplicación estará disponible en: `http://localhost:8081`

## Compilación

```bash
cd Comercio.NET.Pizzeria
dotnet build Comercio.NET.Pizzeria.sln
```

## Publicación

```bash
cd Comercio.NET.Pizzeria/Comercio.NET.Pizzeria.Server
dotnet publish -c Release -o ./publish
```

## API Endpoints

### Autenticación
- `POST /api/auth/login` - Login
- `GET /api/auth/validar` - Validar token
- `POST /api/auth/logout` - Logout

### Mesas
- `GET /api/mesas` - Mesas abiertas
- `GET /api/mesas/{id}` - Detalle de mesa
- `GET /api/mesas/{id}/items` - Items de una mesa
- `POST /api/mesas` - Abrir mesa
- `POST /api/mesas/{id}/items` - Agregar item
- `POST /api/mesas/{id}/cerrar` - Cerrar mesa
- `PUT /api/mesas/items/{itemId}` - Actualizar cantidad
- `DELETE /api/mesas/items/{itemId}` - Eliminar item

### Mozos
- `GET /api/mesas/mozos` - Lista de mozos
- `POST /api/mesas/mozos` - Crear mozo
- `PUT /api/mesas/mozos/{id}` - Actualizar mozo
- `DELETE /api/mesas/mozos/{id}` - Eliminar mozo

### Productos Bar
- `GET /api/mesas/productos-bar` - Lista de productos
- `POST /api/mesas/productos-bar` - Crear producto
- `PUT /api/mesas/productos-bar/{id}` - Actualizar producto
- `DELETE /api/mesas/productos-bar/{id}` - Eliminar producto

### Formas de Pago
- `GET /api/mesas/formas-pago` - Lista de formas de pago
- `POST /api/mesas/formas-pago` - Crear forma de pago
- `PUT /api/mesas/formas-pago/{id}` - Actualizar forma de pago
- `DELETE /api/mesas/formas-pago/{id}` - Eliminar forma de pago

### Ventas
- `GET /api/mesas/ventas-dia` - Ventas del día
- `GET /api/mesas/ventas-por-producto` - Ventas por producto (con filtros de fecha)

## Arquitectura

El proyecto usa una arquitectura de tres capas:

1. **Controllers** - Reciben las peticiones HTTP
2. **Services** - Lógica de negocio y comunicación con SQL Bridge
3. **Models** - DTOs para transferencia de datos

### SQL Bridge

Este proyecto **NO** se conecta directamente a SQL Server. Utiliza el **SQL Bridge** como intermediario, enviando consultas SQL a través de HTTP POST:

```csharp
POST {SQL_BRIDGE_URL}/query
{
  "query": "SELECT * FROM Mesas WHERE Estado = @estado",
  "parameters": { "@estado": "Abierta" }
}
```

## Dependencias

```xml
<PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="8.*" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
```

## Notas

- El proyecto es completamente **independiente** del proyecto principal `Comercio.NET.Mobile`
- Usa su propia autenticación simplificada (solo rol pizzeria)
- Comparte la misma base de datos pero a través del SQL Bridge
- El frontend es estático (HTML/CSS/JS vanilla)

## Licencia

Parte del proyecto Comercio.NET
