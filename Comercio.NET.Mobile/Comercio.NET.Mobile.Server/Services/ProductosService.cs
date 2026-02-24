using Comercio.NET.Mobile.Server.Controllers;
using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class ProductosService : IProductosService
    {
        private readonly string _sqlBridgeUrl;
        private readonly ILogger<ProductosService> _logger;
        private readonly HttpClient _httpClient;

        public ProductosService(
            IConfiguration configuration,
            ILogger<ProductosService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _sqlBridgeUrl = Environment.GetEnvironmentVariable("SQL_BRIDGE_URL")
                ?? configuration["SqlBridgeUrl"]
                ?? throw new InvalidOperationException("SQL_BRIDGE_URL no está configurada");
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IEnumerable<ProductoDto>> BuscarProductosAsync(string termino)
        {
            var productos = new List<ProductoDto>();

            var query = @"
                SELECT codigo, descripcion, costo, precio, cantidad, rubro, marca
                FROM Productos
                WHERE Activo = 1
                  AND (
                        codigo      LIKE @termino
                     OR descripcion LIKE @termino
                     OR rubro       LIKE @termino
                     OR marca       LIKE @termino
                  )
                ORDER BY descripcion";

            var payload = new
            {
                query,
                parameters = new Dictionary<string, object?>
                {
                    { "@termino", $"%{termino}%" }
                }
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("SQL Bridge error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    throw new Exception($"Error en SQL Bridge: {response.StatusCode}");
                }

                // El SQL Bridge devuelve { "data": [[col0, col1, ...], ...] }
                var resultado = await JsonSerializer.DeserializeAsync<QueryResult>(
                    new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (resultado?.Data != null)
                {
                    foreach (var row in resultado.Data)
                    {
                        // Orden de columnas: codigo, descripcion, costo, precio, cantidad, rubro, marca
                        productos.Add(new ProductoDto
                        {
                            Codigo = ConvertToString(row.Count > 0 ? row[0] : null),
                            Descripcion = ConvertToString(row.Count > 1 ? row[1] : null),
                            Costo = ConvertToDecimal(row.Count > 2 ? row[2] : null),
                            Precio = ConvertToDecimal(row.Count > 3 ? row[3] : null),
                            Stock = ConvertToInt32(row.Count > 4 ? row[4] : null),
                            Rubro = ConvertToString(row.Count > 5 ? row[5] : null),
                            Marca = ConvertToString(row.Count > 6 ? row[6] : null),
                        });
                    }
                }

                _logger.LogInformation("Búsqueda '{Termino}': {Count} producto(s) encontrado(s)", termino, productos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando productos con término '{Termino}'", termino);
                throw;
            }

            return productos;
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