# Guía de Migración - Proyecto Pizzería

Este documento describe cómo se separó el módulo de Pizzería del proyecto principal Comercio.NET.

## Estructura Original

El proyecto **Comercio.NET.Mobile** contenía:
- Sistema de ventas general
- Módulo de pizzería (mesas, mozos, productos bar)
- Auditoría y estadísticas
- Arqueología de caja
- Gestión de turnos

## Estructura Actual

### Proyecto Principal: `Comercio.NET.Mobile`
**Ruta:** `Comercio.NET.Mobile\Comercio.NET.Mobile.Server`

Contiene:
- ? Sistema de ventas general
- ? Auditoría y estadísticas  
- ? Arqueología de caja
- ? Gestión de turnos
- ? Productos (catálogo general)
- ? **Eliminado:** Todo lo relacionado con pizzería

### Nuevo Proyecto: `Comercio.NET.Pizzeria`
**Ruta:** `Comercio.NET.Pizzeria\Comercio.NET.Pizzeria.Server`

Contiene:
- ? Gestión de mesas
- ? Control de mozos
- ? Productos de bar
- ? Formas de pago
- ? Ventas por mesa
- ? Reportes de ventas

## Archivos Movidos

### Backend (C#)

| Archivo Original | Destino | Estado |
|---|---|---|
| `Controllers/MesasController.cs` | `Pizzeria/Controllers/MesasController.cs` | ? Movido |
| `Services/MesasService.cs` | `Pizzeria/Services/MesasService.cs` | ? Movido |
| `Services/IMesasService.cs` | `Pizzeria/Services/IMesasService.cs` | ? Movido |
| `Services/AuthService.cs` | `Pizzeria/Services/AuthService.cs` | ? Copiado/Adaptado |
| `Models/MesaDto.cs` | `Pizzeria/Models/MesaDto.cs` | ? Movido |
| `Models/FormaPagoDto.cs` | `Pizzeria/Models/FormaPagoDto.cs` | ? Movido |
| `Models/VentaProductoDto.cs` | `Pizzeria/Models/VentaProductoDto.cs` | ? Movido |
| `Models/Usuario.cs` | `Pizzeria/Models/Usuario.cs` | ? Copiado |

### Frontend (wwwroot)

| Archivo Original | Destino | Estado |
|---|---|---|
| `wwwroot/dashboard-pizzeria.html` | `Pizzeria/wwwroot/dashboard-pizzeria.html` | ? Movido |
| `wwwroot/mesas.html` | `Pizzeria/wwwroot/mesas.html` | ? Movido |
| `wwwroot/mozos.html` | `Pizzeria/wwwroot/mozos.html` | ? Movido |
| `wwwroot/productos-bar.html` | `Pizzeria/wwwroot/productos-bar.html` | ? Movido |
| `wwwroot/formas-pago.html` | `Pizzeria/wwwroot/formas-pago.html` | ? Movido |
| `wwwroot/ventas-mesas.html` | `Pizzeria/wwwroot/ventas-mesas.html` | ? Movido |
| `wwwroot/login.html` | `Pizzeria/wwwroot/login.html` | ? Copiado |
| `wwwroot/js/*.js` (pizzeria) | `Pizzeria/wwwroot/js/*.js` | ? Movido |
| `wwwroot/css/*.css` (compartidos) | `Pizzeria/wwwroot/css/*.css` | ? Copiado |

## Cambios Realizados

### 1. Namespace Actualizado
```csharp
// Antes
namespace Comercio.NET.Mobile.Server.Controllers

// Ahora
namespace Comercio.NET.Pizzeria.Server.Controllers
```

### 2. AuthService Simplificado
Solo mantiene el usuario de pizzería:
```csharp
private readonly Dictionary<string, (string Password, string NombreCompleto, string Rol)> _usuariosHardcoded = new()
{
    { "pizzeria", ("pizzeria", "Pizzería", "Pizzeria") },
};
```

### 3. Puerto Diferente
- **Comercio.NET.Mobile:** Puerto `8080` (por defecto)
- **Comercio.NET.Pizzeria:** Puerto `8081` (por defecto)

### 4. Program.cs Limpiado
**Proyecto principal** - Eliminado:
```csharp
builder.Services.AddSingleton<IMesasService, MesasService>();
```

**Nuevo proyecto** - Solo contiene:
```csharp
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<IMesasService, MesasService>();
```

## Base de Datos

Ambos proyectos comparten la misma base de datos a través del **SQL Bridge**, pero cada uno accede a tablas diferentes:

### Tablas del Proyecto Principal
- `Ventas`
- `Productos`
- `Turnos`
- `ArqueoCaja`
- `Auditoria`

### Tablas del Proyecto Pizzería
- `Mesas`
- `MesasItems`
- `Mozos`
- `ProductosBar`
- `FormasPago`

## Ejecución Independiente

### Proyecto Principal
```bash
cd Comercio.NET.Mobile\Comercio.NET.Mobile.Server
$env:SQL_BRIDGE_URL = "http://localhost:5000"
$env:PORT = "8080"
dotnet run
```

### Proyecto Pizzería
```bash
cd Comercio.NET.Pizzeria\Comercio.NET.Pizzeria.Server
$env:SQL_BRIDGE_URL = "http://localhost:5000"
$env:PORT = "8081"
dotnet run
```

O usar el script:
```bash
cd Comercio.NET.Pizzeria
.\start.ps1
```

## Ventajas de la Separación

? **Despliegue Independiente:** Cada módulo puede actualizarse sin afectar al otro  
? **Escalabilidad:** Cada servicio puede escalar según su demanda  
? **Mantenimiento:** Código más limpio y fácil de mantener  
? **Desarrollo:** Equipos diferentes pueden trabajar en paralelo  
? **Testing:** Pruebas aisladas por módulo  

## Rollback (Volver Atrás)

Si necesitas revertir la separación:

1. Copiar los archivos C# de vuelta a `Comercio.NET.Mobile.Server`
2. Restaurar las referencias en `Program.cs`
3. Copiar los archivos wwwroot de vuelta
4. Cambiar los namespaces a `Comercio.NET.Mobile.Server`

## Notas Importantes

?? Ambos proyectos **DEBEN** usar el mismo **SQL Bridge**  
?? No hay comunicación directa entre los proyectos  
?? Cada uno tiene su propia autenticación  
?? Los puertos deben ser diferentes para ejecutar ambos simultáneamente  

## Versiones

- **Fecha de separación:** 2025-01-XX
- **Versión Comercio.NET:** 1.7.0
- **Versión .NET:** 8.0
- **SQL Bridge:** Compatible con versión existente

## Soporte

Para preguntas o problemas, revisar:
- `README.md` en cada proyecto
- Documentación del SQL Bridge
- Logs en la consola de cada aplicación
