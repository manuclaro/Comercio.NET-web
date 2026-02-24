using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class EstadisticasService
    {
        private readonly string _sqlBridgeUrl;
        private readonly ILogger<EstadisticasService> _logger;
        private readonly HttpClient _httpClient;

        public EstadisticasService(
            IConfiguration configuration,
            ILogger<EstadisticasService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _sqlBridgeUrl = Environment.GetEnvironmentVariable("SQL_BRIDGE_URL")
                ?? configuration["SqlBridgeUrl"]
                ?? throw new InvalidOperationException("SQL_BRIDGE_URL no está configurada");
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IEnumerable<EstadisticaVentaDto>> ObtenerVentasPorRubroAsync(DateTime desde, DateTime hasta)
        {
            var query = @"
                SELECT
                    p.rubro,
                    SUM(r.precio * r.cantidad) AS totalVentas,
                    COUNT(DISTINCT p.codigo)   AS cantidadProductos
                FROM Renglones r
                INNER JOIN Productos p ON p.codigo = r.codigo
                INNER JOIN Ventas v    ON v.numero  = r.numero
                WHERE v.fecha >= @desde
                  AND v.fecha <= @hasta
                  AND p.rubro IN ('VERDULERIA', 'PANADERIA', 'FIAMBRERIA', 'CARNICERIA')
                GROUP BY p.rubro
                ORDER BY totalVentas DESC";

            var payload = new
            {
                query,
                parameters = new Dictionary<string, object?>
                {
                    { "@desde", desde.ToString("yyyy-MM-dd") },
                    { "@hasta", hasta.ToString("yyyy-MM-dd 23:59:59") }
                }
            };

            var resultados = new List<EstadisticaVentaDto>();

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("SQL Bridge error: {StatusCode} - {Content}", response.StatusCode, content);
                    throw new Exception($"Error en SQL Bridge: {response.StatusCode}");
                }

                var resultado = await JsonSerializer.DeserializeAsync<QueryResult>(
                    new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (resultado?.Data != null)
                {
                    foreach (var row in resultado.Data)
                    {
                        resultados.Add(new EstadisticaVentaDto
                        {
                            Rubro = ConvertToString(row.Count > 0 ? row[0] : null),
                            TotalVentas = ConvertToDecimal(row.Count > 1 ? row[1] : null),
                            CantidadProductos = ConvertToInt32(row.Count > 2 ? row[2] : null)
                        });
                    }
                }

                _logger.LogInformation("Estadísticas por rubro: {Count} rubro(s) desde {Desde} hasta {Hasta}",
                    resultados.Count, desde, hasta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas de ventas por rubro");
                throw;
            }

            return resultados;
        }

        private static int ConvertToInt32(object? value)
        {
            if (value == null) return 0;
            if (value is JsonElement j)
                return j.ValueKind switch
                {
                    JsonValueKind.Number => j.GetInt32(),
                    JsonValueKind.String => int.TryParse(j.GetString(), out var r) ? r : 0,
                    _ => 0
                };
            return Convert.ToInt32(value);
        }

        private static decimal ConvertToDecimal(object? value)
        {
            if (value == null) return 0;
            if (value is JsonElement j)
                return j.ValueKind switch
                {
                    JsonValueKind.Number => j.GetDecimal(),
                    JsonValueKind.String => decimal.TryParse(j.GetString(), out var r) ? r : 0,
                    _ => 0
                };
            return Convert.ToDecimal(value);
        }

        private static string ConvertToString(object? value)
        {
            if (value == null) return string.Empty;
            if (value is JsonElement j)
                return j.ValueKind switch
                {
                    JsonValueKind.String => j.GetString() ?? string.Empty,
                    JsonValueKind.Null => string.Empty,
                    _ => j.ToString()
                };
            return value.ToString() ?? string.Empty;
        }
    }
}