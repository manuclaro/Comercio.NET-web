# ?? Inicio Rápido - Comercio.NET Pizzería

## ? Ejecución en 30 Segundos

```powershell
# 1. Navegar al proyecto
cd "C:\Users\Manuel\source\repos\Comercio .NET\Comercio.NET.Pizzeria"

# 2. Ejecutar el script
.\start.ps1
```

## ?? Credenciales

- **Usuario:** `pizzeria`
- **Contraseña:** `pizzeria`

## ?? URLs

- **Aplicación:** http://localhost:8081
- **Health Check:** http://localhost:8081/api/health
- **Swagger:** http://localhost:8081/swagger (si está habilitado)

## ??? Comandos Útiles

### Compilar
```powershell
dotnet build Comercio.NET.Pizzeria.sln
```

### Ejecutar en Desarrollo
```powershell
cd Comercio.NET.Pizzeria.Server
dotnet run
```

### Ejecutar en Producción
```powershell
cd Comercio.NET.Pizzeria.Server
dotnet run --configuration Release
```

### Publicar
```powershell
cd Comercio.NET.Pizzeria.Server
dotnet publish -c Release -o ./publish
```

## ?? Configuración Rápida

### Variables de Entorno

```powershell
# SQL Bridge (REQUERIDO)
$env:SQL_BRIDGE_URL = "http://localhost:5000"

# Puerto (opcional, por defecto 8081)
$env:PORT = "8081"
```

### Archivo appsettings.json

```json
{
  "SqlBridgeUrl": "http://localhost:5000"
}
```

## ?? Funcionalidades

| Módulo | Descripción |
|--------|-------------|
| ??? **Mesas** | Abrir, gestionar y cerrar mesas |
| ????? **Mozos** | CRUD de mozos |
| ?? **Productos Bar** | Gestión de productos |
| ?? **Formas de Pago** | Configurar métodos de pago |
| ?? **Ventas** | Reportes y resúmenes |

## ?? Troubleshooting

### Error: SQL Bridge no configurado
```powershell
$env:SQL_BRIDGE_URL = "http://localhost:5000"
```

### Error: Puerto en uso
```powershell
$env:PORT = "8082"  # Cambiar a otro puerto
```

### Error: No se puede conectar a SQL Bridge
1. Verificar que SQL Bridge esté ejecutándose
2. Comprobar la URL: `http://localhost:5000/api/health`

## ?? Documentación Completa

- **README.md** - Documentación detallada
- **MIGRATION.md** - Guía de migración
- **COMPLETED.md** - Resumen de la migración

## ?? Enlaces API

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/api/auth/login` | POST | Login |
| `/api/mesas` | GET | Listar mesas abiertas |
| `/api/mesas/{id}` | GET | Detalle de mesa |
| `/api/mesas/mozos` | GET | Listar mozos |
| `/api/mesas/productos-bar` | GET | Listar productos |
| `/api/mesas/formas-pago` | GET | Listar formas de pago |
| `/api/mesas/ventas-dia` | GET | Ventas del día |

---

**¿Dudas?** Revisar `README.md` para información completa.
