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
                            cajeros.Add(row[0].ToString()!);
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
                            THEN CAST(ImporteFinal AS DECIMAL(18,2)) ELSE 0 END) as Otro
                    FROM Facturas
                    WHERE CAST(Fecha AS DATE) = @fecha
                    AND (@cajero IS NULL OR Cajero = @cajero)
                    AND ISNULL(Cajero, '') <> ''";

                var parameters = new Dictionary<string, object>
                {
                    { "@fecha", fecha.ToString("yyyy-MM-dd") },
                    { "@cajero", string.IsNullOrEmpty(cajero) ? DBNull.Value : cajero }
                };

                var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", new { query, parameters });
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<QueryResult>();

                if (result?.Data != null && result.Data.Count > 0)
                {
                    var row = result.Data[0];
                    resultado.CantidadVentas = Convert.ToInt32(row[0]);
                    resultado.TotalIngresos = Convert.ToDecimal(row[1]);
                    resultado.DNI = Convert.ToDecimal(row[2]);
                    resultado.Efectivo = Convert.ToDecimal(row[3]);
                    resultado.MercadoPago = Convert.ToDecimal(row[4]);
                    resultado.Otro = Convert.ToDecimal(row[5]);
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo arqueo");
                throw;
            }
        }
    }

    // Clase auxiliar para deserializar la respuesta del SQL Bridge
    public class QueryResult
    {
        public List<List<object?>> Data { get; set; } = new();
    }
}