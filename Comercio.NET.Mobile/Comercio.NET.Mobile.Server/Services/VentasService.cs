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

        public async Task<IEnumerable<VentaDto>> GetVentasDelDiaAsync(DateTime desde, DateTime hasta, int? numeroCajero = null, string formaPago = null, string tipoFactura = null)
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
                WHERE CAST(v.fecha AS DATE) BETWEEN @desde AND @hasta";

            if (numeroCajero.HasValue)
                sql += " AND CAST(f.Cajero AS INT) = @numeroCajero";

            if (!string.IsNullOrWhiteSpace(formaPago))
                sql += " AND f.FormadePago = @formaPago";

            if (!string.IsNullOrWhiteSpace(tipoFactura))
            {
                sql += string.Equals(tipoFactura, "Factura", StringComparison.OrdinalIgnoreCase)
                    ? " AND f.TipoFactura LIKE 'Factura%'"
                    : " AND f.TipoFactura = @tipoFactura";
            }

            sql += " ORDER BY v.id DESC";

            var parameters = new Dictionary<string, object?>
            {
                { "@desde", desde.Date.ToString("yyyy-MM-dd") },
                { "@hasta", hasta.Date.ToString("yyyy-MM-dd") }
            };

            if (numeroCajero.HasValue)
                parameters["@numeroCajero"] = numeroCajero.Value;

            if (!string.IsNullOrWhiteSpace(formaPago))
                parameters["@formaPago"] = formaPago;

            if (!string.IsNullOrWhiteSpace(tipoFactura) &&
                !string.Equals(tipoFactura, "Factura", StringComparison.OrdinalIgnoreCase))
                parameters["@tipoFactura"] = tipoFactura;

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
                _logger.LogError(ex, "Error obteniendo ventas {Desde} - {Hasta}", desde, hasta);
                throw;
            }

            return ventas;
        }

        public async Task<ResumenVentasDto> GetResumenAsync(DateTime desde, DateTime hasta, int? numeroCajero = null, string formaPago = null, string tipoFactura = null)
        {
            // El total se calcula sumando ImporteFinal de Facturas (una fila por venta real),
            // igual que lo hace el arqueo de caja. La tabla Ventas tiene items individuales
            // cuya suma de v.total NO necesariamente coincide con ImporteFinal (descuentos, etc).
            // Para los desgoses por forma de pago se usa un subquery escalar por factura.
            var sql = @"
                WITH FacturasUnicas AS (
                    SELECT
                        f.NumeroRemito,
                        f.ImporteFinal,
                        f.FormadePago,
                        f.TipoFactura,
                        f.Cajero,
                        f.esctacte,
                        ROW_NUMBER() OVER (PARTITION BY f.NumeroRemito ORDER BY f.Id ASC) AS rn
                    FROM Facturas f
                    WHERE CAST(f.Fecha AS DATE) BETWEEN @desde AND @hasta
                      AND ISNULL(f.Cajero, '') <> ''
                      AND ISNULL(f.esctacte, 0) = 0
                )
                SELECT
                    ISNULL(SUM(fu.ImporteFinal), 0)         AS TotalVendido,
                    COUNT(*)                                AS CantidadTransacciones,
                    ISNULL((
                        SELECT SUM(v2.cantidad)
                        FROM Ventas v2
                        INNER JOIN FacturasUnicas fu2 ON fu2.NumeroRemito = v2.nrofactura
                        WHERE fu2.rn = 1
                          AND CAST(v2.fecha AS DATE) BETWEEN @desde AND @hasta
                    ), 0)                                   AS CantidadProductos,
                    ISNULL(SUM(CASE WHEN LOWER(fu.FormadePago) = 'efectivo'
                        THEN fu.ImporteFinal ELSE 0 END), 0)                      AS TotalEfectivo,
                    ISNULL(SUM(CASE WHEN LOWER(fu.FormadePago) LIKE '%mercado%pago%'
                        THEN fu.ImporteFinal ELSE 0 END), 0)                      AS TotalMercadoPago,
                    ISNULL(SUM(CASE WHEN LOWER(fu.FormadePago) = 'dni'
                        THEN fu.ImporteFinal ELSE 0 END), 0)                      AS TotalDni,
                    0                                       AS TotalCtaCte,
                    ISNULL(SUM(CASE WHEN LOWER(fu.FormadePago) NOT IN ('efectivo', 'dni')
                                     AND LOWER(fu.FormadePago) NOT LIKE '%mercado%pago%'
                        THEN fu.ImporteFinal ELSE 0 END), 0)                      AS TotalOtros
                FROM FacturasUnicas fu
                WHERE fu.rn = 1";

            if (numeroCajero.HasValue)
                sql += " AND CAST(fu.Cajero AS INT) = @numeroCajero";

            if (!string.IsNullOrWhiteSpace(formaPago))
                sql += " AND fu.FormadePago = @formaPago";

            if (!string.IsNullOrWhiteSpace(tipoFactura))
            {
                sql += string.Equals(tipoFactura, "Factura", StringComparison.OrdinalIgnoreCase)
                    ? " AND fu.TipoFactura LIKE 'Factura%'"
                    : " AND fu.TipoFactura = @tipoFactura";
            }

            var parameters = new Dictionary<string, object?>
            {
                { "@desde", desde.Date.ToString("yyyy-MM-dd") },
                { "@hasta", hasta.Date.ToString("yyyy-MM-dd") }
            };

            if (numeroCajero.HasValue)
                parameters["@numeroCajero"] = numeroCajero.Value;

            if (!string.IsNullOrWhiteSpace(formaPago))
                parameters["@formaPago"] = formaPago;

            if (!string.IsNullOrWhiteSpace(tipoFactura) &&
                !string.Equals(tipoFactura, "Factura", StringComparison.OrdinalIgnoreCase))
                parameters["@tipoFactura"] = tipoFactura;

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
                _logger.LogError(ex, "Error obteniendo resumen {Desde} - {Hasta}", desde, hasta);
                throw;
            }

            return new ResumenVentasDto();
        }

        public Task<IEnumerable<VentaDto>> GetVentasPorTurnoAsync(
            DateTime desde, int? numeroCajero = null, string? formaPago = null, string? tipoFactura = null)
            => GetVentasDelDiaAsync(desde, DateTime.Now, numeroCajero, formaPago, tipoFactura);

        public Task<ResumenVentasDto> GetResumenPorTurnoAsync(
            DateTime desde, int? numeroCajero = null, string? formaPago = null, string? tipoFactura = null)
            => GetResumenAsync(desde, DateTime.Now, numeroCajero, formaPago, tipoFactura);

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