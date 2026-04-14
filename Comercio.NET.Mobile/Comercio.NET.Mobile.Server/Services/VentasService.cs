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

            // La subconsulta agrupa Facturas por NumeroRemito para evitar el producto
            // cartesiano (Facturas tiene una fila por venta, no por producto).
            // Se filtra Facturas por fecha para evitar traer remitos antiguos con el mismo número.
            // El filtro de fecha real se aplica sobre Ventas.fecha en el WHERE principal.
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
                LEFT JOIN (
                    SELECT
                        NumeroRemito,
                        MIN(IdFactura)              AS IdFactura,
                        MAX(FormadePago)            AS FormadePago,
                        MAX(TipoFactura)            AS TipoFactura,
                        MAX(Cajero)                 AS Cajero,
                        MAX(UsuarioVenta)           AS UsuarioVenta,
                        MAX(CAST(esCtaCte AS INT))  AS esCtaCte
                    FROM Facturas
                    WHERE CAST(Fecha AS DATE) BETWEEN @desde AND @hasta
                    GROUP BY NumeroRemito
                ) f ON f.NumeroRemito = v.nrofactura
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
                _logger.LogInformation("GetVentasDelDiaAsync → desde={Desde}, hasta={Hasta}, cajero={Cajero}, pago={Pago}, tipo={Tipo}",
                    desde, hasta, numeroCajero, formaPago, tipoFactura);

                var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("GetVentasDelDiaAsync → StatusCode={StatusCode}, ContentLength={Length}",
                    response.StatusCode, content?.Length ?? 0);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("SQL Bridge error: {StatusCode} - {Content}", response.StatusCode, content);
                    throw new Exception($"Error en SQL Bridge: {response.StatusCode}");
                }

                var resultado = JsonSerializer.Deserialize<QueryResult>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("GetVentasDelDiaAsync → Filas recibidas: {Count}",
                    resultado?.Data?.Count ?? 0);

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
            var filtrosFactura = new System.Text.StringBuilder();

            if (numeroCajero.HasValue)
                filtrosFactura.Append(" AND CAST(f.Cajero AS INT) = @numeroCajero");

            if (!string.IsNullOrWhiteSpace(formaPago))
                filtrosFactura.Append(" AND f.FormadePago = @formaPago");

            if (!string.IsNullOrWhiteSpace(tipoFactura))
            {
                filtrosFactura.Append(
                    string.Equals(tipoFactura, "Factura", StringComparison.OrdinalIgnoreCase)
                        ? " AND f.TipoFactura LIKE 'Factura%'"
                        : " AND f.TipoFactura = @tipoFactura");
            }

            // Los totales monetarios se calculan directamente desde Facturas (1 fila por remito)
            // filtrando por Facturas.Fecha, igual que ArqueoCajaService.
            // La cantidad de productos se obtiene por separado desde Ventas para no multiplicar
            // el ImporteFinal por la cantidad de productos de cada remito.
            var sql = $@"
                SELECT
                    ISNULL(SUM(CAST(f.ImporteFinal AS DECIMAL(18,2))), 0) AS TotalVendido,
                    COUNT(DISTINCT f.NumeroRemito)                        AS CantidadTransacciones,
                    ISNULL((
                        SELECT SUM(v.cantidad)
                        FROM Ventas v
                        WHERE CAST(v.fecha AS DATE) BETWEEN @desde AND @hasta
                          AND EXISTS (
                              SELECT 1 FROM Facturas f2
                              WHERE f2.NumeroRemito = v.nrofactura
                                AND CAST(f2.Fecha AS DATE) BETWEEN @desde AND @hasta
                                AND ISNULL(f2.Cajero, '') <> ''
                                AND ISNULL(f2.esCtaCte, 0) = 0
                          )
                    ), 0)                                                  AS CantidadProductos,
                    ISNULL(SUM(CASE WHEN LOWER(f.FormadePago) = 'efectivo'
                        THEN CAST(f.ImporteFinal AS DECIMAL(18,2)) ELSE 0 END), 0) AS TotalEfectivo,
                    ISNULL(SUM(CASE WHEN LOWER(f.FormadePago) LIKE '%mercado%pago%'
                        THEN CAST(f.ImporteFinal AS DECIMAL(18,2)) ELSE 0 END), 0) AS TotalMercadoPago,
                    ISNULL(SUM(CASE WHEN LOWER(f.FormadePago) = 'dni'
                        THEN CAST(f.ImporteFinal AS DECIMAL(18,2)) ELSE 0 END), 0) AS TotalDni,
                    ISNULL((
                        SELECT SUM(CAST(fc.ImporteFinal AS DECIMAL(18,2)))
                        FROM Facturas fc
                        WHERE CAST(fc.Fecha AS DATE) BETWEEN @desde AND @hasta
                          AND ISNULL(fc.esCtaCte, 0) = 1
                    ), 0)                                                  AS TotalCtaCte,
                    ISNULL(SUM(CASE WHEN LOWER(f.FormadePago) NOT IN ('efectivo', 'dni')
                                     AND LOWER(f.FormadePago) NOT LIKE '%mercado%pago%'
                        THEN CAST(f.ImporteFinal AS DECIMAL(18,2)) ELSE 0 END), 0) AS TotalOtros
                FROM Facturas f
                WHERE CAST(f.Fecha AS DATE) BETWEEN @desde AND @hasta
                  AND ISNULL(f.Cajero, '') <> ''
                  AND ISNULL(f.esCtaCte, 0) = 0
                  {filtrosFactura}";

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
                _logger.LogInformation("GetResumenAsync → desde={Desde}, hasta={Hasta}, cajero={Cajero}, pago={Pago}, tipo={Tipo}",
                    desde, hasta, numeroCajero, formaPago, tipoFactura);

                var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("GetResumenAsync → StatusCode={StatusCode}, Response={Content}",
                    response.StatusCode, content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("SQL Bridge error: {StatusCode} - {Content}", response.StatusCode, content);
                    throw new Exception($"Error en SQL Bridge: {response.StatusCode}");
                }

                var resultado = JsonSerializer.Deserialize<QueryResult>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("GetResumenAsync → Data rows: {Count}, First row columns: {Cols}",
                    resultado?.Data?.Count ?? 0,
                    resultado?.Data?.FirstOrDefault()?.Count ?? 0);

                if (resultado?.Data != null && resultado.Data.Count > 0)
                {
                    var row = resultado.Data[0];

                    _logger.LogInformation("GetResumenAsync → Raw values: [{V}]",
                        string.Join(", ", row.Select(v => v?.ToString() ?? "null")));

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

