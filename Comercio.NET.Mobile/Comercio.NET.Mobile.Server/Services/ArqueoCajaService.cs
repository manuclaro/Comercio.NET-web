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