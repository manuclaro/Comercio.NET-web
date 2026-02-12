using System.Text.Json;
using Comercio.NET.Mobile.Server.Models;

namespace Comercio.NET.Mobile.Server.Services
{
    public class ArqueoCajaService
    {
        private readonly string _sqlBridgeUrl;
        private readonly ILogger<ArqueoCajaService> _logger;
        private readonly HttpClient _httpClient;

        public ArqueoCajaService(IConfiguration configuration, ILogger<ArqueoCajaService> logger, IHttpClientFactory httpClientFactory)
        {
            _sqlBridgeUrl = Environment.GetEnvironmentVariable("SQL_BRIDGE_URL")
                ?? configuration["SqlBridgeUrl"]
                ?? throw new InvalidOperationException("SQL_BRIDGE_URL no está configurada");
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<List<string>> ObtenerCajerosAsync()
        {
            try
            {
                var query = @"
                    SELECT DISTINCT Cajero
                    FROM Facturas
                    WHERE ISNULL(Cajero, '') <> ''
                    ORDER BY Cajero";

                var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", new { query });
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<QueryResult>();

                var cajeros = new List<string>();
                if (result?.Data != null)
                {
                    foreach (var row in result.Data)
                    {
                        if (row.Count > 0 && row[0] != null)
                        {
                            cajeros.Add(ConvertToString(row[0]));
                        }
                    }
                }

                return cajeros;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo lista de cajeros");
                throw;
            }
        }

        public async Task<ArqueoCajaDto> ObtenerArqueoAsync(DateTime fecha, string? cajero = null)
        {
            var resultado = new ArqueoCajaDto { Fecha = fecha, Cajero = cajero };

            try
            {
                // ✅ QUERY ACTUALIZADA: Incluye pagos a proveedores
                var query = @"
                    SELECT 
                        COUNT(DISTINCT NumeroRemito) as TotalVentas,
                        SUM(CAST(ISNULL(ImporteFinal, 0) AS DECIMAL(18,2))) as TotalIngresos,
                        SUM(CASE WHEN FormadePago = 'DNI' 
                            THEN CAST(ImporteFinal AS DECIMAL(18,2)) ELSE 0 END) as DNI,
                        SUM(CASE WHEN FormadePago = 'Efectivo' 
                            THEN CAST(ImporteFinal AS DECIMAL(18,2)) ELSE 0 END) as Efectivo,
                        SUM(CASE WHEN FormadePago LIKE '%Mercado%Pago%' OR FormadePago = 'MercadoPago'
                            THEN CAST(ImporteFinal AS DECIMAL(18,2)) ELSE 0 END) as MercadoPago,
                        SUM(CASE WHEN FormadePago = 'Otro' 
                            THEN CAST(ImporteFinal AS DECIMAL(18,2)) ELSE 0 END) as Otro,
                        SUM(CASE WHEN TipoFactura = 'FacturaC' OR TipoFactura = 'Factura C' OR TipoFactura = 'C'
                            THEN CAST(ImporteFinal AS DECIMAL(18,2)) ELSE 0 END) as FacturaC,
                        -- ✅ NUEVO: Total de pagos a proveedores del día
                        ISNULL((
                            SELECT SUM(CAST(Monto AS DECIMAL(18,2)))
                            FROM PagosProveedores
                            WHERE CAST(FechaPago AS DATE) = @fecha
                            AND (@cajero IS NULL OR UsuarioRegistro = @cajero)
                        ), 0) as PagosProveedores
                    FROM Facturas
                    WHERE CAST(Fecha AS DATE) = @fecha
                    AND esctacte = 0
                    AND (@cajero IS NULL OR Cajero = @cajero)
                    AND ISNULL(Cajero, '') <> ''";

                var parameters = new Dictionary<string, object?>
                {
                    { "@fecha", fecha.ToString("yyyy-MM-dd") },
                    { "@cajero", string.IsNullOrEmpty(cajero) ? null : cajero }
                };

                var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", new { query, parameters });
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<QueryResult>();

                if (result?.Data != null && result.Data.Count > 0)
                {
                    var row = result.Data[0];

                    resultado.CantidadVentas = ConvertToInt32(row[0]);
                    resultado.TotalIngresos = ConvertToDecimal(row[1]);
                    resultado.DNI = ConvertToDecimal(row[2]);
                    resultado.Efectivo = ConvertToDecimal(row[3]);
                    resultado.MercadoPago = ConvertToDecimal(row[4]);
                    resultado.Otro = ConvertToDecimal(row[5]);
                    resultado.FacturaC = ConvertToDecimal(row[6]);
                    resultado.PagosProveedores = ConvertToDecimal(row[7]);  // ✅ NUEVO
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo arqueo");
                throw;
            }
        }

        public async Task<List<DetallePagoProveedorDto>> ObtenerDetallePagosProveedoresAsync(DateTime fecha, string? cajero = null)
        {
            _logger.LogInformation("🔍 Iniciando consulta de pagos a proveedores - Fecha: {Fecha}, Cajero: {Cajero}", 
                fecha.ToString("yyyy-MM-dd"), cajero ?? "NULL");

            try
            {
                // ✅ Query actualizada con los nombres correctos de columnas
                var query = @"
                    SELECT 
                        pp.Id,
                        pp.Proveedor,
                        pp.Monto,
                        pp.FechaPago,
                        ISNULL(pp.Observaciones, '') as Observaciones,
                        ISNULL(pp.UsuarioRegistro, '') as UsuarioRegistro,
                        pp.NumeroCajero,
                        pp.NumeroRemito,
                        ISNULL(pp.NombreEquipo, '') as NombreEquipo,
                        pp.FechaRegistro,
                        pp.IdProveedor,
                        pp.CompraId,
                        pp.CtaCteId,
                        ISNULL(pp.Origen, '') as Origen
                    FROM PagosProveedores pp
                    WHERE CAST(pp.FechaPago AS DATE) = @fecha
                    AND (@cajero IS NULL OR pp.UsuarioRegistro = @cajero)
                    ORDER BY pp.FechaPago DESC";

                var parameters = new Dictionary<string, object?>
                {
                    { "@fecha", fecha.ToString("yyyy-MM-dd") },
                    { "@cajero", string.IsNullOrEmpty(cajero) ? null : cajero }
                };

                _logger.LogInformation("📤 Ejecutando query");

                var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", new { query, parameters });
                
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ SQL Bridge error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    
                    // Si la tabla no existe o hay error, retornar lista vacía
                    if (responseContent.Contains("Invalid object name") || 
                        responseContent.Contains("PagosProveedores") ||
                        response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    {
                        _logger.LogWarning("⚠️ Error en base de datos, retornando lista vacía");
                        return new List<DetallePagoProveedorDto>();
                    }
                    
                    throw new Exception($"Error en SQL Bridge: {responseContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<QueryResult>();

                var pagos = new List<DetallePagoProveedorDto>();
                
                if (result?.Data != null && result.Data.Count > 0)
                {
                    _logger.LogInformation("📊 Procesando {Count} filas", result.Data.Count);

                    foreach (var row in result.Data)
                    {
                        try
                        {
                            pagos.Add(new DetallePagoProveedorDto
                            {
                                Id = ConvertToInt32(row[0]),
                                Proveedor = ConvertToString(row[1]),
                                Monto = ConvertToDecimal(row[2]),
                                FechaPago = Convert.ToDateTime(row[3]),
                                Observaciones = ConvertToString(row[4]),
                                UsuarioRegistro = ConvertToString(row[5]),
                                NumeroCajero = ConvertToInt32(row[6]),
                                NumeroRemito = row[7] != null ? ConvertToInt32(row[7]) : null,
                                NombreEquipo = ConvertToString(row[8]),
                                FechaRegistro = Convert.ToDateTime(row[9]),
                                IdProveedor = row[10] != null ? ConvertToInt32(row[10]) : null,
                                CompraId = row[11] != null ? ConvertToInt32(row[11]) : null,
                                CtaCteId = row[12] != null ? ConvertToInt32(row[12]) : null,
                                Origen = ConvertToString(row[13])
                            });
                        }
                        catch (Exception exRow)
                        {
                            _logger.LogError(exRow, "❌ Error procesando fila");
                        }
                    }
                }

                _logger.LogInformation("✅ Consulta finalizada - Total pagos: {Count}", pagos.Count);
                return pagos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo detalle de pagos a proveedores");
                
                // Retornar lista vacía en lugar de error para no romper la UI
                return new List<DetallePagoProveedorDto>();
            }
        }

        // Métodos auxiliares para convertir JsonElement
        private static int ConvertToInt32(object? value)
        {
            if (value == null) return 0;

            if (value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind switch
                {
                    JsonValueKind.Number => jsonElement.GetInt32(),
                    JsonValueKind.String => int.TryParse(jsonElement.GetString(), out int result) ? result : 0,
                    _ => 0
                };
            }

            return Convert.ToInt32(value);
        }

        private static decimal ConvertToDecimal(object? value)
        {
            if (value == null) return 0;

            if (value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind switch
                {
                    JsonValueKind.Number => jsonElement.GetDecimal(),
                    JsonValueKind.String => decimal.TryParse(jsonElement.GetString(), out decimal result) ? result : 0,
                    _ => 0
                };
            }

            return Convert.ToDecimal(value);
        }

        private static string ConvertToString(object? value)
        {
            if (value == null) return string.Empty;

            if (value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                    JsonValueKind.Number => jsonElement.ToString(),
                    JsonValueKind.Null => string.Empty,
                    _ => jsonElement.ToString()
                };
            }

            return value.ToString() ?? string.Empty;
        }
    }

    // Clase auxiliar para deserializar la respuesta del SQL Bridge
    public class QueryResult
    {
        public List<List<object?>> Data { get; set; } = new();
    }
}