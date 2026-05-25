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
                WITH ventascontotal AS (
                    SELECT
                        v.nrofactura,
                        CASE
                            WHEN UPPER(COALESCE(p.rubro, '')) LIKE '%CARNI%'   THEN 'CARNICERIA'
                            WHEN UPPER(COALESCE(p.rubro, '')) LIKE '%VERDULE%' THEN 'VERDULERIA'
                            WHEN UPPER(COALESCE(p.rubro, '')) LIKE '%PANADE%'
                              OR UPPER(COALESCE(p.rubro, '')) LIKE '%PASTEL%'  THEN 'PANADERIA'
                            WHEN UPPER(COALESCE(p.rubro, '')) LIKE '%FIAMB%'
                              OR UPPER(COALESCE(p.rubro, '')) LIKE '%QUESO%'
                              OR UPPER(COALESCE(p.rubro, '')) LIKE '%EMBUT%'   THEN 'FIAMBRERIA'
                            ELSE 'ALMACEN'
                        END AS Rubro,
                        CAST(v.total AS DECIMAL(18,2)) AS TotalProducto,
                        SUM(CAST(v.total AS DECIMAL(18,2))) OVER (PARTITION BY v.nrofactura) AS TotalFacturaVentas,
                        CAST(f.importefinal AS DECIMAL(18,2)) AS ImporteFinalFactura
                    FROM ventas v
                    INNER JOIN productos p ON v.codigo = p.codigo
                    INNER JOIN facturas f  ON v.nrofactura = f.numeroremito
                    WHERE f.fecha >= @desde::date
                      AND f.fecha <  @hasta::date + INTERVAL '1 day'
                )
                SELECT
                    Rubro,
                    COUNT(DISTINCT nrofactura)  AS CantidadFacturas,
                    COUNT(*)                    AS CantidadProductos,
                    CAST(SUM(
                        CASE
                            WHEN TotalFacturaVentas > 0
                            THEN (TotalProducto / TotalFacturaVentas) * ImporteFinalFactura
                            ELSE 0
                        END
                    ) AS DECIMAL(18,2))         AS TotalVentas
                FROM ventascontotal
                GROUP BY Rubro
                ORDER BY TotalVentas DESC";

            var payload = new
            {
                query,
                parameters = new Dictionary<string, object?>
                {
                    // Formato yyyyMMdd: SQL Server lo interpreta sin ambig�edad con CONVERT(..., 112)
                    { "@desde", desde.ToString("yyyyMMdd") },
                    { "@hasta", hasta.ToString("yyyyMMdd") }
                }
            };

            var resultados = new List<EstadisticaVentaDto>();

            try
            {
                using var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError("SQL Bridge error: {StatusCode} - {Content}", response.StatusCode, content);
                    throw new Exception($"Error en SQL Bridge: {response.StatusCode}");
                }

                var resultado = await response.Content.ReadFromJsonAsync<QueryResult>(JsonSerializerDefaults.CaseInsensitive);

                if (resultado?.Data != null)
                {
                    foreach (var row in resultado.Data)
                    {
                        resultados.Add(new EstadisticaVentaDto
                        {
                            Rubro = ConvertToString(row.Count > 0 ? row[0] : null),
                            CantidadProductos = ConvertToInt32(row.Count > 2 ? row[2] : null),
                            TotalVentas = ConvertToDecimal(row.Count > 3 ? row[3] : null)
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