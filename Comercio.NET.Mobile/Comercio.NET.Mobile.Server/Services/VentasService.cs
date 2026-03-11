using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class VentasService : IVentasService
    {
        private readonly string _sqlBridgeUrl;
        private readonly ILogger<VentasService> _logger;
        private readonly HttpClient _httpClient;

        public VentasService(
            IConfiguration configuration,
            ILogger<VentasService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _sqlBridgeUrl = Environment.GetEnvironmentVariable("SQL_BRIDGE_URL")
                ?? configuration["SqlBridgeUrl"]
                ?? throw new InvalidOperationException("SQL_BRIDGE_URL no está configurada");
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IEnumerable<VentaDto>> GetVentasDelDiaAsync(DateTime fecha, int? numeroCajero = null, string formaPago = null)
        {
            var ventas = new List<VentaDto>();

            var sql = @"
                SELECT 
                    v.id, v.nrofactura, v.codigo, v.descripcion,
                    v.precio, v.cantidad, v.total, v.PorcentajeIva,
                    ISNULL(v.EsOferta, 0)            AS EsOferta,
                    ISNULL(v.NombreOferta, '')        AS NombreOferta,
                    ISNULL(f.FormadePago, '')         AS FormaPago,
                    ISNULL(f.TipoFactura, '')         AS TipoFactura,
                    ISNULL(CAST(v.fecha AS DATE), CAST(GETDATE() AS DATE)) AS Fecha,
                    ISNULL(v.hora, '')                AS Hora,
                    ISNULL(v.EsCtaCte, 0)             AS EsCtaCte,
                    ISNULL(v.NombreCtaCte, '')        AS NombreCtaCte,
                    ISNULL(f.UsuarioVenta, '')        AS UsuarioVenta,
                    ISNULL(CAST(f.Cajero AS INT), 0)  AS NumeroCajero
                FROM Ventas v
                LEFT JOIN Facturas f ON f.NumeroRemito = v.nrofactura
                WHERE CAST(v.fecha AS DATE) = @fecha";

            if (numeroCajero.HasValue)
                sql += " AND CAST(f.Cajero AS INT) = @numeroCajero";

            if (!string.IsNullOrWhiteSpace(formaPago))
                sql += " AND f.FormadePago = @formaPago";

            sql += " ORDER BY v.id DESC";

            var parameters = new Dictionary<string, object?> { { "@fecha", fecha.Date.ToString("yyyy-MM-dd") } };

            if (numeroCajero.HasValue)
                parameters["@numeroCajero"] = numeroCajero.Value;

            if (!string.IsNullOrWhiteSpace(formaPago))
                parameters["@formaPago"] = formaPago;

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
                        ventas.Add(new VentaDto
                        {
                            Id            = ConvertToInt32(row.Count > 0  ? row[0]  : null),
                            NroFactura    = ConvertToInt32(row.Count > 1  ? row[1]  : null),
                            Codigo        = ConvertToString(row.Count > 2  ? row[2]  : null),
                            Descripcion   = ConvertToString(row.Count > 3  ? row[3]  : null),
                            Precio        = ConvertToDecimal(row.Count > 4  ? row[4]  : null),
                            Cantidad      = ConvertToInt32(row.Count > 5  ? row[5]  : null),
                            Total         = ConvertToDecimal(row.Count > 6  ? row[6]  : null),
                            PorcentajeIva = ConvertToDecimal(row.Count > 7  ? row[7]  : null),
                            EsOferta      = ConvertToBoolean(row.Count > 8  ? row[8]  : null),
                            NombreOferta  = ConvertToString(row.Count > 9  ? row[9]  : null),
                            FormaPago     = ConvertToString(row.Count > 10 ? row[10] : null),
                            TipoFactura   = ConvertToString(row.Count > 11 ? row[11] : null),
                            Fecha         = ConvertToDateTime(row.Count > 12 ? row[12] : null),
                            Hora          = ConvertToString(row.Count > 13 ? row[13] : null),
                            EsCtaCte      = ConvertToBoolean(row.Count > 14 ? row[14] : null),
                            NombreCtaCte  = ConvertToString(row.Count > 15 ? row[15] : null),
                            UsuarioVenta  = ConvertToString(row.Count > 16 ? row[16] : null),
                            NumeroCajero  = ConvertToInt32(row.Count > 17 ? row[17] : null),
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo ventas del día {Fecha}", fecha);
                throw;
            }

            return ventas;
        }

        public async Task<ResumenVentasDto> GetResumenAsync(DateTime fecha, int? numeroCajero = null)
        {
            var sql = @"
                SELECT
                    ISNULL(SUM(f.ImporteFinal), 0) AS TotalVendido,
                    COUNT(DISTINCT f.NumeroRemito)  AS CantidadTransacciones,
                    ISNULL(SUM(v.cantidad), 0)      AS CantidadProductos,
                    ISNULL(SUM(CASE WHEN LOWER(f.FormadePago) = 'efectivo'     THEN f.ImporteFinal ELSE 0 END), 0) AS TotalEfectivo,
                    ISNULL(SUM(CASE WHEN LOWER(f.FormadePago) = 'mercado pago' THEN f.ImporteFinal ELSE 0 END), 0) AS TotalMercadoPago,
                    ISNULL(SUM(CASE WHEN LOWER(f.FormadePago) = 'dni'          THEN f.ImporteFinal ELSE 0 END), 0) AS TotalDni,
                    ISNULL(SUM(CASE WHEN f.esCtaCte = 1                        THEN f.ImporteFinal ELSE 0 END), 0) AS TotalCtaCte,
                    ISNULL(SUM(CASE WHEN LOWER(f.FormadePago) NOT IN ('efectivo', 'mercado pago', 'dni')
                                     AND f.esCtaCte = 0                        THEN f.ImporteFinal ELSE 0 END), 0) AS TotalOtros
                FROM Facturas f
                INNER JOIN Ventas v ON v.nrofactura = f.NumeroRemito
                WHERE CAST(f.Fecha AS DATE) = @fecha";

            if (numeroCajero.HasValue)
                sql += " AND CAST(f.Cajero AS INT) = @numeroCajero";

            var parameters = new Dictionary<string, object?> { { "@fecha", fecha.Date.ToString("yyyy-MM-dd") } };

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

                if (resultado?.Data != null && resultado.Data.Count > 0)
                {
                    var row = resultado.Data[0];
                    return new ResumenVentasDto
                    {
                        TotalVendido          = ConvertToDecimal(row.Count > 0 ? row[0] : null),
                        CantidadTransacciones = ConvertToInt32(row.Count > 1  ? row[1] : null),
                        CantidadProductos     = ConvertToInt32(row.Count > 2  ? row[2] : null),
                        TotalEfectivo         = ConvertToDecimal(row.Count > 3 ? row[3] : null),
                        TotalMercadoPago      = ConvertToDecimal(row.Count > 4 ? row[4] : null),
                        TotalDni              = ConvertToDecimal(row.Count > 5 ? row[5] : null),
                        TotalCtaCte           = ConvertToDecimal(row.Count > 6 ? row[6] : null),
                        TotalOtros            = ConvertToDecimal(row.Count > 7 ? row[7] : null),
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen del día {Fecha}", fecha);
                throw;
            }

            return new ResumenVentasDto();
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