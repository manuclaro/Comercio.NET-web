using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly string _sqlBridgeUrl;
        private readonly ILogger<AuditoriaService> _logger;
        private readonly HttpClient _httpClient;

        public AuditoriaService(
            IConfiguration configuration,
            ILogger<AuditoriaService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _sqlBridgeUrl = Environment.GetEnvironmentVariable("SQL_BRIDGE_URL")
                ?? configuration["SqlBridgeUrl"]
                ?? throw new InvalidOperationException("SQL_BRIDGE_URL no está configurada");
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IEnumerable<AuditoriaDto>> GetAuditoriaAsync(DateTime desde, DateTime hasta, string usuario = null, int? numeroCajero = null)
        {
            var registros = new List<AuditoriaDto>();

            var sql = @"
                SELECT
                    IdAuditoriaProductosEliminados,
                    CodigoProducto,
                    DescripcionProducto,
                    PrecioUnitario,
                    Cantidad,
                    TotalEliminado,
                    NumeroFactura,
                    FechaEliminacion,
                    ISNULL(UsuarioEliminacion, '')       AS UsuarioEliminacion,
                    ISNULL(MotivoEliminacion, '')        AS MotivoEliminacion,
                    ISNULL(EsCtaCte, 0)                 AS EsCtaCte,
                    ISNULL(NombreCtaCte, '')             AS NombreCtaCte,
                    ISNULL(IPUsuario, '')                AS IPUsuario,
                    ISNULL(NombreEquipo, '')             AS NombreEquipo,
                    FechaHoraVentaOriginal,
                    ISNULL(NumeroCajero, 0)             AS NumeroCajero,
                    IvaEliminado,
                    CantidadOriginal,
                    EsEliminacionCompleta   
                FROM AuditoriaProductosEliminados
                WHERE CAST(FechaEliminacion AS DATE) BETWEEN @desde AND @hasta";

            if (!string.IsNullOrWhiteSpace(usuario))
                sql += " AND UsuarioEliminacion = @usuario";

            if (numeroCajero.HasValue)
                sql += " AND NumeroCajero = @numeroCajero";

            sql += " ORDER BY FechaEliminacion DESC";

            var parameters = new Dictionary<string, object?>
            {
                { "@desde", desde.Date.ToString("yyyy-MM-dd") },
                { "@hasta", hasta.Date.ToString("yyyy-MM-dd") }
            };

            if (!string.IsNullOrWhiteSpace(usuario))
                parameters["@usuario"] = usuario;

            if (numeroCajero.HasValue)
                parameters["@numeroCajero"] = numeroCajero.Value;

            var payload = new { query = sql, parameters };

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);
                var content  = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("SQL Bridge error: {StatusCode} - {Content}", response.StatusCode, content);
                    throw new Exception($"Error en SQL Bridge: {response.StatusCode}");
                }

                var resultado = JsonSerializer.Deserialize<QueryResult>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (resultado?.Data != null)
                {
                    foreach (var row in resultado.Data)
                    {
                        registros.Add(new AuditoriaDto
                        {
                            Id                    = ConvertToInt32(row.Count > 0  ? row[0]  : null),
                            CodigoProducto        = ConvertToString(row.Count > 1  ? row[1]  : null),
                            DescripcionProducto   = ConvertToString(row.Count > 2  ? row[2]  : null),
                            PrecioUnitario        = ConvertToDecimal(row.Count > 3  ? row[3]  : null),
                            Cantidad              = ConvertToInt32(row.Count > 4  ? row[4]  : null),
                            TotalEliminado        = ConvertToDecimal(row.Count > 5  ? row[5]  : null),
                            NumeroFactura         = ConvertToInt32(row.Count > 6  ? row[6]  : null),
                            FechaEliminacion      = ConvertToDateTime(row.Count > 7  ? row[7]  : null),
                            MotivoEliminacion     = ConvertToString(row.Count > 8  ? row[8]  : null),
                            UsuarioEliminacion    = ConvertToString(row.Count > 9  ? row[9]  : null),
                            NumeroCajero          = ConvertToInt32(row.Count > 10 ? row[10] : null),
                            NombreEquipo          = ConvertToString(row.Count > 11 ? row[11] : null),
                            EsCtaCte              = ConvertToBoolean(row.Count > 12 ? row[12] : null),
                            NombreCtaCte          = ConvertToString(row.Count > 13 ? row[13] : null),
                            EsEliminacionCompleta = ConvertToNullableBoolean(row.Count > 14 ? row[14] : null),
                            CantidadOriginal      = ConvertToNullableInt32(row.Count > 15 ? row[15] : null),
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo auditoría desde {Desde} hasta {Hasta}", desde, hasta);
                throw;
            }

            return registros;
        }

        private static int ConvertToInt32(object? value)
        {
            if (value is null) return 0;
            if (value is JsonElement j)
                return j.ValueKind switch
                {
                    JsonValueKind.Number => j.GetInt32(),
                    JsonValueKind.String => int.TryParse(j.GetString(), out var r) ? r : 0,
                    _ => 0
                };
            return Convert.ToInt32(value);
        }

        private static int? ConvertToNullableInt32(object? value)
        {
            if (value is null) return null;
            if (value is JsonElement j && j.ValueKind == JsonValueKind.Null) return null;
            return ConvertToInt32(value);
        }

        private static decimal ConvertToDecimal(object? value)
        {
            if (value is null) return 0m;
            if (value is JsonElement j)
                return j.ValueKind switch
                {
                    JsonValueKind.Number => j.GetDecimal(),
                    JsonValueKind.String => decimal.TryParse(j.GetString(), out var r) ? r : 0m,
                    _ => 0m
                };
            return Convert.ToDecimal(value);
        }

        private static bool ConvertToBoolean(object? value)
        {
            if (value is null) return false;
            if (value is JsonElement j)
                return j.ValueKind switch
                {
                    JsonValueKind.True   => true,
                    JsonValueKind.False  => false,
                    JsonValueKind.Number => j.GetInt32() != 0,
                    JsonValueKind.String => j.GetString() is "1" or "true" or "True",
                    _ => false
                };
            return Convert.ToBoolean(value);
        }

        private static bool? ConvertToNullableBoolean(object? value)
        {
            if (value is null) return null;
            if (value is JsonElement j && j.ValueKind == JsonValueKind.Null) return null;
            return ConvertToBoolean(value);
        }

        private static string ConvertToString(object? value)
        {
            if (value is null) return string.Empty;
            if (value is JsonElement j)
                return j.ValueKind switch
                {
                    JsonValueKind.String => j.GetString() ?? string.Empty,
                    JsonValueKind.Null   => string.Empty,
                    _                   => j.ToString()
                };
            return value.ToString() ?? string.Empty;
        }

        private static DateTime ConvertToDateTime(object? value)
        {
            if (value is null) return DateTime.MinValue;
            if (value is JsonElement j && j.ValueKind == JsonValueKind.String)
                return DateTime.TryParse(j.GetString(), out var d) ? d : DateTime.MinValue;
            return Convert.ToDateTime(value);
        }
    }
}