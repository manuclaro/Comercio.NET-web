# Especificación Técnica - Sistema de Arqueo de Caja

## 📋 Tabla de Contenidos

1. [Visión General](#visión-general)
2. [Arquitectura del Sistema](#arquitectura-del-sistema)
3. [Componentes](#componentes)
4. [Flujo de Datos](#flujo-de-datos)
5. [Especificación de APIs](#especificación-de-apis)
6. [Configuración y Despliegue](#configuración-y-despliegue)
7. [Manejo de Errores](#manejo-de-errores)
8. [Consideraciones de Seguridad](#consideraciones-de-seguridad)
9. [Troubleshooting](#troubleshooting)

---

## Visión General

### Propósito
Sistema distribuido para consultar información de arqueo de caja desde una base de datos SQL Server remota a través de una arquitectura de microservicios.

### Stack Tecnológico
- **Backend API**: .NET 8 (ASP.NET Core Minimal API)
- **SQL Bridge**: .NET 8 (Windows Service)
- **Base de Datos**: SQL Server 2019+
- **Serialización**: System.Text.Json
- **Hosting**: Railway (API) + Windows Server (SQL Bridge)
- **Proxy Reverso**: Configurado para `https://sql.comerciopele.com.ar`

---

## Arquitectura del Sistema

### Diagrama de Componentes

graph TB Client[Cliente Web/Móvil] API[API Mobile Server<br/>Railway] Bridge[SQL Bridge<br/>Windows Service] DB[(SQL Server<br/>192.168.100.200:1433)]
Client -->|HTTPS| API
API -->|HTTPS| Bridge
Bridge -->|TCP 1433| DB

subgraph "Red Interna Cliente"
    Bridge
    DB
end

subgraph "Cloud - Railway"
    API
end

### Flujo de Comunicación

┌──────────────┐      HTTPS       ┌─────────────────┐      HTTPS      ┌──────────────┐ │   Cliente    │ ───────────────> │   API Railway   │ ──────────────> │  SQL Bridge  │ │  Web/Móvil   │                  │  .NET 8         │                 │   (Windows   │ └──────────────┘                  │                 │                 │   Service)   │ └─────────────────┘                 └──────────────┘ │ │ TCP 1433 ▼ ┌──────────────┐ │  SQL Server  │ │   Comercio   │ └──────────────┘


---

## Componentes

### 1. Cliente (Frontend)

**Ubicación**: `Comercio.NET.Mobile/Comercio.NET.Mobile.Server/wwwroot/`

**Archivos principales**:
- `index.html`: Interfaz de usuario
- `js/app.js`: Lógica de interacción con la API
- `css/styles.css`: Estilos visuales

**Funcionalidades**:
- Selección de fecha para consulta
- Filtro por cajero específico
- Visualización de totales por forma de pago
- Dashboard interactivo con tarjetas de resumen

**Tecnologías**:
- HTML5
- JavaScript Vanilla
- CSS3
- Fetch API para comunicación HTTP

---

### 2. API Mobile Server

**Ubicación**: `Comercio.NET.Mobile/Comercio.NET.Mobile.Server/`

**Tecnología**: ASP.NET Core 8.0

#### Estructura de Archivos

Comercio.NET.Mobile.Server/ ├── Controllers/ │   └── ArqueoCajaController.cs      # Endpoints REST ├── Services/ │   └── ArqueoCajaService.cs         # Lógica de negocio ├── Models/ │   └── ArqueoCajaDto.cs             # DTOs ├── wwwroot/                          # Frontend │   ├── index.html │   ├── js/app.js │   └── css/styles.css ├── Program.cs                        # Configuración principal ├── appsettings.json                  # Configuración └── Dockerfile                        # Para Railway


#### Endpoints Principales

##### `GET /api/arqueocaja/cajeros`
Obtiene la lista de cajeros disponibles.

**Response**:
[ "Cajero1", "Cajero2", "Cajero3" ]

**Código**:
[HttpGet("cajeros")] public async Task<ActionResult<List<string>>> GetCajeros() { var cajeros = await _arqueoCajaService.ObtenerCajerosAsync(); return Ok(cajeros); }


##### `GET /api/arqueocaja`
Obtiene el arqueo de caja para una fecha y cajero específicos.

**Query Parameters**:
- `fecha` (required): Fecha en formato `yyyy-MM-dd`
- `cajero` (optional): Nombre del cajero

**Response**:
{ "fecha": "2026-02-10T00:00:00", "cajero": "Cajero1", "cantidadVentas": 45, "totalIngresos": 125000.50, "efectivo": 75000.00, "mercadoPago": 40000.50, "dni": 10000.00, "otro": 0.00 }


**Código**:
[HttpGet] public async Task<ActionResult<ArqueoCajaDto>> GetArqueo( [FromQuery] DateTime fecha, [FromQuery] string? cajero = null) { var arqueo = await _arqueoCajaService.ObtenerArqueoAsync(fecha, cajero); return Ok(arqueo); }


#### Variables de Entorno

| Variable | Descripción | Valor Ejemplo |
|----------|-------------|---------------|
| `SQL_BRIDGE_URL` | URL del SQL Bridge | `https://sql.comerciopele.com.ar` |
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución | `Production` |

---

### 3. SQL Bridge (Windows Service)

**Ubicación**: `Comercio.NET.SqlBridge/Comercio.NET.SqlBridge.Server/`

**Tecnología**: ASP.NET Core 8.0 como Windows Service (NSSM)

#### Características Principales

- ✅ Escucha en `http://0.0.0.0:5000` (todas las interfaces de red)
- ✅ Logging a archivo en `C:\SqlBridge\logs\`
- ✅ Manejo de parámetros con `System.Text.Json`
- ✅ Conversión automática de tipos `JsonElement`
- ✅ Timeout de 30 segundos por query
- ✅ Registro detallado de cada petición

#### Endpoints

##### `GET /health`
Health check del servicio.

**Response**:
{ "status": "ok", "timestamp": "2026-02-10T22:30:00" }


##### `POST /query`
Ejecuta una query SQL con parámetros parametrizados.

**Request Body**:
{ "query": "SELECT * FROM Facturas WHERE CAST(Fecha AS DATE) = @fecha", "parameters": { "@fecha": "2026-02-10", "@cajero": "Cajero1" } }


**Response**:
{ "data": [ [3961, 4316, "2026-02-10T00:00:00", "2026-02-10T09:15:30", 4500.00, "MercadoPago", false], [3962, 4317, "2026-02-10T00:00:00", "2026-02-10T10:20:15", 3200.50, "Efectivo", false] ] }


#### Logging

**Ubicación**: `C:\SqlBridge\logs\sqlbridge_YYYYMMDD.log`

**Formato**:
[2026-02-10 22:52:42.975] [Information] === INICIANDO SQL BRIDGE === [2026-02-10 22:52:42.978] [Information] BaseDirectory: C:\SqlBridge
[2026-02-10 22:52:42.984] [Information] 🚀 SQL Bridge iniciado en http://0.0.0.0:5000


---

## Flujo de Datos

### Secuencia de Arqueo de Caja


sequenceDiagram participant C as Cliente participant A as API Railway participant B as SQL Bridge participant D as SQL Server
C->>A: GET /api/arqueocaja?fecha=2026-02-10
A->>B: POST /query (con parámetros)
B->>D: SQL Query con @fecha
D-->>B: ResultSet
B->>B: Convertir a JSON
B-->>A: {"data": [[...]]}
A->>A: Convertir JsonElement
A->>A: Mapear a ArqueoCajaDto
A-->>C: JSON Response



### Manejo de Parámetros

#### En SQL Bridge (Servidor)

// Conversión de JsonElement a tipos SQL if (param.Value is System.Text.Json.JsonElement jsonElement) { value = jsonElement.ValueKind switch { JsonValueKind.String => (object?)jsonElement.GetString(), JsonValueKind.Number => jsonElement.TryGetInt32(out int i) ? (object)i : (object)jsonElement.GetDecimal(), JsonValueKind.True => (object?)true, JsonValueKind.False => (object?)false, JsonValueKind.Null => null, _ => (object?)jsonElement.ToString() }; }
var sqlValue = value ?? DBNull.Value; cmd.Parameters.AddWithValue(param.Key, sqlValue);


#### En API (Cliente)

private static decimal ConvertToDecimal(object? value) { if (value == null) return 0;
if (value is JsonElement jsonElement)
{
    return jsonElement.ValueKind switch
    {
        JsonValueKind.Number => jsonElement.GetDecimal(),
        JsonValueKind.String => decimal.TryParse(
            jsonElement.GetString(), 
            out decimal result
        ) ? result : 0,
        _ => 0
    };
}

return Convert.ToDecimal(value);
}


---

## Especificación de APIs

### API Mobile Server

#### Base URL

Production: https://comercio-net-web-production.up.railway.app Local: http://localhost:5000


#### Headers Requeridos

Content-Type: application/json Accept: application/json


### SQL Bridge API

#### Base URL
Production: https://sql.comerciopele.com.ar Internal: http://192.168.100.200:5000 Local: http://localhost:5000


#### Timeout
- Conexión: 30 segundos
- Query: 30 segundos

---

## Configuración y Despliegue

### Requisitos Previos

#### SQL Bridge (Windows Server)
- Windows Server 2016+
- .NET 8 Runtime
- NSSM (Non-Sucking Service Manager)
- Acceso a red interna donde está SQL Server
- Puerto 5000 disponible

#### API Mobile (Railway)
- Cuenta en Railway
- GitHub repository conectado
- Variables de entorno configuradas

### Despliegue de SQL Bridge

#### Instalación Inicial

1. Crear directorio
New-Item -Path "C:\SqlBridge" -ItemType Directory
2. Instalar como servicio con NSSM
C:\nssm\nssm.exe install SqlBridgeWeb "C:\Program Files\dotnet\dotnet.exe" "C:\SqlBridge\Comercio.NET.SqlBridge.Server.dll"
3. Configurar startup automático
C:\nssm\nssm.exe set SqlBridgeWeb Start SERVICE_AUTO_START
4. Iniciar servicio
C:\nssm\nssm.exe start SqlBridgeWeb


#### Script de Actualización

$nssm = "C:\nssm\nssm.exe" $dir = "C:\SqlBridge" $zip = "$env:USERPROFILE\Desktop\SqlBridge.zip"
Detener servicio
& $nssm stop SqlBridgeWeb Start-Sleep -Seconds 3
Backup
$backup = "C:\SqlBridge_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')" Copy-Item $dir $backup -Recurse -Force
Actualizar
$temp = "$env:TEMP\SqlBridge_Update" Expand-Archive -Path $zip -DestinationPath $temp -Force Get-ChildItem $temp | Where-Object { $.Name -ne 'logs' } | ForEach-Object { Copy-Item $.FullName $dir -Recurse -Force } Remove-Item $temp -Recurse -Force
Iniciar
& $nssm start SqlBridgeWeb


### Despliegue de API en Railway

#### Variables de Entorno
SQL_BRIDGE_URL=https://sql.comerciopele.com.ar ASPNETCORE_ENVIRONMENT=Production


---

## Manejo de Errores

### Códigos de Error Comunes

| Código | Descripción | Causa | Solución |
|--------|-------------|-------|----------|
| 400 | Bad Request | Query vacía | Verificar payload |
| 500 | Internal Server Error | Error SQL | Revisar logs |
| 503 | Service Unavailable | SQL Bridge caído | Verificar servicio |

### Logging

#### En SQL Bridge

[2026-02-10 23:15:30.123] [Information] === INICIO REQUEST /query === [2026-02-10 23:15:30.124] [Information] Remote IP: 203.0.113.45 [2026-02-10 23:15:30.147] [Information]   - @fecha = '2026-02-10' (tipo: String) [2026-02-10 23:15:30.235] [Information] Filas obtenidas: 1 [2026-02-10 23:15:30.236] [Information] === FIN REQUEST /query (EXITOSO) ===




---

## Consideraciones de Seguridad

### Implementadas

1. ✅ **HTTPS en comunicación externa**
2. ✅ **Parámetros SQL parametrizados** (previene SQL injection)
3. ✅ **Validación de queries**
4. ✅ **Logging detallado**
5. ✅ **Timeout en queries**

### Recomendaciones Pendientes

1. ⚠️ **Autenticación API-to-API** (JWT o API Keys)
2. ⚠️ **Rate Limiting**
3. ⚠️ **Whitelist de IPs**
4. ⚠️ **CORS configurado**

---

## Troubleshooting

### SQL Bridge no inicia

Ver estado
C:\nssm\nssm.exe status SqlBridgeWeb
Ver logs
Get-Content C:\SqlBridge\logs\sqlbridge_$(Get-Date -Format 'yyyyMMdd').log -Tail 50
Reiniciar servicio
C:\nssm\nssm.exe restart SqlBridgeWeb



### Error 500 en /query

**Causa**: Falta manejo de `JsonElement`

**Solución**: Verificar métodos `ConvertToInt32`, `ConvertToDecimal`, `ConvertToString`

### Timeout en queries

**Solución**: Aumentar timeout

cmd.CommandTimeout = 60; // segundos


---

## Anexos

### A. Queries SQL Utilizadas

#### Obtener Cajeros

SELECT DISTINCT Cajero FROM Facturas WHERE ISNULL(Cajero, '') <> '' ORDER BY Cajero


#### Obtener Arqueo

SELECT COUNT(DISTINCT NumeroRemito) as TotalVentas, SUM(CAST(ISNULL(ImporteFinal, 0) AS DECIMAL(18,2))) as TotalIngresos, SUM(CASE WHEN FormadePago = 'DNI' THEN CAST(ImporteFinal AS DECIMAL(18,2)) ELSE 0 END) as DNI, SUM(CASE WHEN FormadePago = 'Efectivo' THEN CAST(ImporteFinal AS DECIMAL(18,2)) ELSE 0 END) as Efectivo, SUM(CASE WHEN FormadePago LIKE '%Mercado%Pago%' OR FormadePago = 'MercadoPago' THEN CAST(ImporteFinal AS DECIMAL(18,2)) ELSE 0 END) as MercadoPago, SUM(CASE WHEN FormadePago = 'Otro' THEN CAST(ImporteFinal AS DECIMAL(18,2)) ELSE 0 END) as Otro FROM Facturas WHERE CAST(Fecha AS DATE) = @fecha AND (@cajero IS NULL OR Cajero = @cajero) AND ISNULL(Cajero, '') <> ''


### B. Comandos Útiles

#### Compilar y Publicar

cd Comercio.NET.SqlBridge/Comercio.NET.SqlBridge.Server dotnet publish -c Release -o publish Compress-Archive -Path "publish*" -DestinationPath "SqlBridge.zip" -Force



#### Gestión de Servicio
Ver logs en tiempo real
Get-Content C:\SqlBridge\logs\sqlbridge_$(Get-Date -Format 'yyyyMMdd').log -Wait -Tail 20
Limpiar logs antiguos
Get-ChildItem C:\SqlBridge\logs*.log | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } | Remove-Item



#### Pruebas

Health check
Invoke-RestMethod http://localhost:5000/health
Query de prueba
$body = @{ query = "SELECT TOP 5 * FROM Facturas WHERE CAST(Fecha AS DATE) = @fecha" parameters = @{ "@fecha" = "2026-02-10" } } | ConvertTo-Json
Invoke-RestMethod -Method POST -Uri "http://localhost:5000/query" ` -ContentType "application/json" -Body $body



### C. Modelos de Datos

#### ArqueoCajaDto

public class ArqueoCajaDto { public DateTime Fecha { get; set; } public string? Cajero { get; set; } public int CantidadVentas { get; set; } public decimal TotalIngresos { get; set; } public decimal Efectivo { get; set; } public decimal MercadoPago { get; set; } public decimal DNI { get; set; } public decimal Otro { get; set; } }


#### QueryRequest

public record QueryRequest( string Query, Dictionary<string, object?>? Parameters = null );


---

## Changelog

| Versión | Fecha | Cambios |
|---------|-------|---------|
| 1.0.0 | 2026-02-10 | Release inicial |
| 1.1.0 | 2026-02-10 | Manejo de JsonElement |
| 1.2.0 | 2026-02-10 | Migración a 0.0.0.0:5000 |
| 1.3.0 | 2026-02-10 | Logging a archivo |

---

## Contacto

**Repositorio**: https://github.com/manuclaro/Comercio.NET-web  
**Branch**: recuperado3

---

*Documentación generada el 10 de febrero de 2026*


