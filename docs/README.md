# 🏪 Comercio.NET - Sistema de Arqueo de Caja

Sistema distribuido de gestión de arqueos de caja con arquitectura de microservicios, desarrollado en .NET 8.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Railway](https://img.shields.io/badge/Deploy-Railway-0B0D0E?logo=railway)](https://railway.app/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## 🌟 Características

- ✅ **Consulta en tiempo real** de arqueos de caja
- ✅ **Filtrado por cajero y fecha**
- ✅ **Múltiples formas de pago** (Efectivo, MercadoPago, DNI, Otros)
- ✅ **Arquitectura distribuida** con SQL Bridge
- ✅ **Logging detallado** para diagnóstico
- ✅ **Manejo robusto de errores**

## 🏗️ Arquitectura
┌─────────────┐      HTTPS      ┌──────────────┐     HTTPS     ┌─────────────┐ │   Cliente   │ ──────────────> │ API Railway  │ ────────────> │ SQL Bridge  │ │  Web/Móvil  │                 │   (.NET 8)   │               │  (Windows)  │ └─────────────┘                 └──────────────┘               └─────────────┘ │ TCP 1433 ▼ ┌─────────────┐ │ SQL Server  │ └─────────────┘


## 🚀 Stack Tecnológico

| Componente | Tecnología |
|-----------|-----------|
| **Backend API** | ASP.NET Core 8.0 |
| **SQL Bridge** | ASP.NET Core 8.0 (Windows Service) |
| **Base de Datos** | SQL Server 2019+ |
| **Hosting API** | Railway |
| **Hosting Bridge** | Windows Server |
| **Frontend** | HTML5 + JavaScript + CSS3 |

## 📁 Estructura del Proyecto
Comercio.NET/ ├── Comercio.NET.Mobile/ │   └── Comercio.NET.Mobile.Server/ │       ├── Controllers/          # API Endpoints │       ├── Services/             # Lógica de negocio │       ├── Models/               # DTOs │       ├── wwwroot/              # Frontend │       └── Program.cs │ ├── Comercio.NET.SqlBridge/ │   └── Comercio.NET.SqlBridge.Server/ │       ├── Program.cs            # SQL Bridge service │       └── logs/                 # Logging │ └── docs/ └── ARQUITECTURA_SQLBRIDGE.md # Documentación técnica


## 🔧 Instalación

### Requisitos Previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server 2019+
- (Para SQL Bridge) Windows Server + [NSSM](https://nssm.cc/)

### Configuración Local

1. **Clonar el repositorio**
git clone https://github.com/manuclaro/Comercio.NET-web.git cd Comercio.NET-web git checkout recuperado3


2. **Configurar variables de entorno**
En `Comercio.NET.Mobile.Server/appsettings.json`:
{ "SqlBridgeUrl": "https://sql.comerciopele.com.ar" }


3. **Ejecutar la API**
cd Comercio.NET.Mobile/Comercio.NET.Mobile.Server dotnet run


4. **Abrir en el navegador**
http://localhost:5000


## 🚢 Despliegue

### API en Railway

1. Conectar repositorio de GitHub
2. Configurar variables de entorno:
   - `SQL_BRIDGE_URL`: `https://sql.comerciopele.com.ar`
   - `ASPNETCORE_ENVIRONMENT`: `Production`
3. Railway detectará automáticamente el `Dockerfile`
4. Deploy automático en cada push

### SQL Bridge en Windows Server

Publicar proyecto
cd Comercio.NET.SqlBridge/Comercio.NET.SqlBridge.Server dotnet publish -c Release -o C:\SqlBridge
Instalar como servicio
C:\nssm\nssm.exe install SqlBridgeWeb "C:\Program Files\dotnet\dotnet.exe" "C:\SqlBridge\Comercio.NET.SqlBridge.Server.dll"
Iniciar servicio
C:\nssm\nssm.exe start SqlBridgeWeb


## 📖 API Reference

### Endpoints Principales

#### `GET /api/arqueocaja/cajeros`
Obtiene lista de cajeros disponibles.

**Response:**
["Cajero1", "Cajero2", "Cajero3"]

#### `GET /api/arqueocaja`
Obtiene arqueo de caja.

**Parámetros:**
- `fecha` (required): `yyyy-MM-dd`
- `cajero` (optional): Nombre del cajero

**Response:**
{ "fecha": "2026-02-10T00:00:00", "cajero": "Cajero1", "cantidadVentas": 45, "totalIngresos": 125000.50, "efectivo": 75000.00, "mercadoPago": 40000.50, "dni": 10000.00, "otro": 0.00 }


### SQL Bridge

#### `POST /query`
Ejecuta query SQL con parámetros.

**Request:**
{ "query": "SELECT * FROM Facturas WHERE Fecha = @fecha", "parameters": { "@fecha": "2026-02-10" } }


## 🧪 Testing

Health check
curl http://localhost:5000/health
Obtener cajeros
curl http://localhost:5000/api/arqueocaja/cajeros
Obtener arqueo
curl "http://localhost:5000/api/arqueocaja?fecha=2026-02-10&cajero=Cajero1"


## 📊 Logging

### SQL Bridge

Los logs se guardan automáticamente en:
C:\SqlBridge\logs\sqlbridge_YYYYMMDD.log


**Ver logs en tiempo real:**
Get-Content C:\SqlBridge\logs\sqlbridge_$(Get-Date -Format 'yyyyMMdd').log -Wait -Tail 20


## 🐛 Troubleshooting

### Error 500 en /query

**Problema:** `InvalidCastException: Unable to cast JsonElement`

**Solución:** Verificar que `ArqueoCajaService.cs` tenga los métodos de conversión:
- `ConvertToInt32()`
- `ConvertToDecimal()`
- `ConvertToString()`

### SQL Bridge no inicia

Verificar estado
C:\nssm\nssm.exe status SqlBridgeWeb
Ver logs
Get-Content C:\SqlBridge\logs*.log -Tail 50
Reiniciar
C:\nssm\nssm.exe restart SqlBridgeWeb


### Timeout en queries

Aumentar timeout en `Program.cs`:
cmd.CommandTimeout = 60; // segundos


## 🔒 Seguridad

### Implementado
- ✅ HTTPS en todas las comunicaciones externas
- ✅ Parámetros SQL parametrizados (prevención de SQL injection)
- ✅ Validación de queries
- ✅ Logging de accesos

### Recomendado para Producción
- ⚠️ Implementar autenticación JWT
- ⚠️ Rate limiting
- ⚠️ IP whitelisting
- ⚠️ Encriptación de connection strings

## 📚 Documentación

- **[Especificación Técnica Completa](docs/ARQUITECTURA_SQLBRIDGE.md)** - Documentación detallada del sistema
- **[Changelog](CHANGELOG.md)** - Historial de cambios

## 🤝 Contribuir

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📝 Changelog

| Versión | Fecha | Cambios |
|---------|-------|---------|
| 1.3.0 | 2026-02-10 | Logging a archivo |
| 1.2.0 | 2026-02-10 | Migración a 0.0.0.0:5000 |
| 1.1.0 | 2026-02-10 | Manejo de JsonElement |
| 1.0.0 | 2026-02-10 | Release inicial |

## 👨‍💻 Autor

**Manuel Claro**

- GitHub: [@manuclaro](https://github.com/manuclaro)
- Repositorio: [Comercio.NET-web](https://github.com/manuclaro/Comercio.NET-web)

## 📄 Licencia

Este proyecto está bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para más detalles.

## 🌐 Links

- **API en Producción:** https://comercio-net-web-production.up.railway.app
- **SQL Bridge:** https://sql.comerciopele.com.ar
- **Repositorio:** https://github.com/manuclaro/Comercio.NET-web

---

⭐ Si este proyecto te fue útil, considera darle una estrella en GitHub!

*Última actualización: 10 de febrero de 2026*




