using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class VentasService : IVentasService
    {
        private readonly DbService _db;
        private readonly ILogger<VentasService> _logger;

        public VentasService(DbService db, ILogger<VentasService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<VentaDto>> GetVentasDelDiaAsync(DateTime desde, DateTime hasta, int? numeroCajero = null, string formaPago = null, string tipoFactura = null)
        {
            var ventas = new List<VentaDto>();

            var esOfertaCoalesce = _db.UsaPostgres ? "COALESCE(v.esoferta, 0::bit)" : "COALESCE(v.esoferta, 0)";
            var esCtaCteCoalesceV = _db.UsaPostgres ? "COALESCE(v.esctacte, 0::bit)" : "COALESCE(v.esctacte, 0)";
            var esCtaCteCastF = _db.UsaPostgres ? "MAX(esctacte::int)" : "MAX(CAST(esctacte AS INT))";
            var cajeroIntCast = _db.UsaPostgres
                ? "COALESCE(NULLIF(f.Cajero, '')::int, 0)"
                : "COALESCE(CAST(f.Cajero AS INT), 0)";
            var horaExpr = _db.UsaPostgres
                ? "COALESCE(CAST(v.hora AS TEXT), '')"
                : "COALESCE(v.hora, '')";

            var sql = $@"
                SELECT 
                    v.id, v.nrofactura, v.codigo, v.descripcion,
                    v.precio, v.cantidad, v.total, v.porcentajeiva,
                    {esOfertaCoalesce}                          AS EsOferta,
                    COALESCE(v.nombreoferta, '')               AS NombreOferta,
                    COALESCE(f.FormadePago, '')                AS FormaPago,
                    COALESCE(f.TipoFactura, '')                AS TipoFactura,
                    COALESCE(CAST(v.fecha AS DATE), CURRENT_DATE) AS Fecha,
                    {horaExpr}                                 AS Hora,
                    {esCtaCteCoalesceV}                         AS EsCtaCte,
                    COALESCE(v.nombrectacte, '')               AS NombreCtaCte,
                    COALESCE(f.UsuarioVenta, '')               AS UsuarioVenta,
                    {cajeroIntCast}                             AS NumeroCajero
                FROM ventas v
                LEFT JOIN (
                    SELECT
                        numeroremito,
                        MIN(idfactura)              AS IdFactura,
                        MAX(formadepago)            AS FormadePago,
                        MAX(tipofactura)            AS TipoFactura,
                        MAX(cajero)                 AS Cajero,
                        MAX(usuarioventa)           AS UsuarioVenta,
                        {esCtaCteCastF}             AS esCtaCte
                    FROM facturas
                    WHERE CAST(fecha AS DATE) BETWEEN @desde AND @hasta
                    GROUP BY numeroremito
                ) f ON f.numeroremito = v.nrofactura
                WHERE CAST(v.fecha AS DATE) BETWEEN @desde AND @hasta";

            if (numeroCajero.HasValue)
                sql += _db.UsaPostgres ? " AND NULLIF(f.cajero, '')::int = @numeroCajero" : " AND CAST(f.cajero AS INT) = @numeroCajero";

            if (!string.IsNullOrWhiteSpace(formaPago))
                sql += " AND f.formadepago = @formaPago";

            if (!string.IsNullOrWhiteSpace(tipoFactura))
            {
                sql += string.Equals(tipoFactura, "Factura", StringComparison.OrdinalIgnoreCase)
                    ? " AND f.tipofactura LIKE 'Factura%'"
                    : " AND f.tipofactura = @tipoFactura";
            }

            sql += " ORDER BY v.id DESC";

            var parameters = new Dictionary<string, object?>
            {
                { "@desde", desde.Date },
                { "@hasta", hasta.Date }
            };

            if (numeroCajero.HasValue)
                parameters["@numeroCajero"] = numeroCajero.Value;

            if (!string.IsNullOrWhiteSpace(formaPago))
                parameters["@formaPago"] = formaPago;

            if (!string.IsNullOrWhiteSpace(tipoFactura) &&
                !string.Equals(tipoFactura, "Factura", StringComparison.OrdinalIgnoreCase))
                parameters["@tipoFactura"] = tipoFactura;

            try
            {
                _logger.LogInformation("GetVentasDelDiaAsync -> desde={Desde}, hasta={Hasta}, cajero={Cajero}, pago={Pago}, tipo={Tipo}",
                    desde, hasta, numeroCajero, formaPago, tipoFactura);

                var rows = await _db.QueryAsync(sql, parameters);

                _logger.LogInformation("GetVentasDelDiaAsync ? Filas recibidas: {Count}", rows.Count);

                foreach (var row in rows)
                {
                    ventas.Add(new VentaDto
                    {
                        Id            = ConvertToInt32(row.Count > 0  ? row[0]  : default),
                        NroFactura    = ConvertToInt32(row.Count > 1  ? row[1]  : default),
                        Codigo        = ConvertToString(row.Count > 2  ? row[2]  : default),
                        Descripcion   = ConvertToString(row.Count > 3  ? row[3]  : default),
                        Precio        = ConvertToDecimal(row.Count > 4  ? row[4]  : default),
                        Cantidad      = ConvertToInt32(row.Count > 5  ? row[5]  : default),
                        Total         = ConvertToDecimal(row.Count > 6  ? row[6]  : default),
                        PorcentajeIva = ConvertToDecimal(row.Count > 7  ? row[7]  : default),
                        EsOferta      = ConvertToBoolean(row.Count > 8  ? row[8]  : default),
                        NombreOferta  = ConvertToString(row.Count > 9  ? row[9]  : default),
                        FormaPago     = ConvertToString(row.Count > 10 ? row[10] : default),
                        TipoFactura   = ConvertToString(row.Count > 11 ? row[11] : default),
                        Fecha         = ConvertToDateTime(row.Count > 12 ? row[12] : default),
                        Hora          = ConvertToString(row.Count > 13 ? row[13] : default),
                        EsCtaCte      = ConvertToBoolean(row.Count > 14 ? row[14] : default),
                        NombreCtaCte  = ConvertToString(row.Count > 15 ? row[15] : default),
                        UsuarioVenta  = ConvertToString(row.Count > 16 ? row[16] : default),
                        NumeroCajero  = ConvertToInt32(row.Count > 17 ? row[17] : default),
                    });
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
                filtrosFactura.Append(_db.UsaPostgres ? " AND NULLIF(f.cajero, '')::int = @numeroCajero" : " AND CAST(f.cajero AS INT) = @numeroCajero");

            if (!string.IsNullOrWhiteSpace(formaPago))
                filtrosFactura.Append(" AND f.formadepago = @formaPago");

            if (!string.IsNullOrWhiteSpace(tipoFactura))
            {
                filtrosFactura.Append(
                    string.Equals(tipoFactura, "Factura", StringComparison.OrdinalIgnoreCase)
                        ? " AND f.tipofactura LIKE 'Factura%'"
                        : " AND f.tipofactura = @tipoFactura");
            }

            var esCtaCteZero = _db.UsaPostgres ? "esctacte = 0::bit" : "COALESCE(esctacte, 0) = 0";
            var esCtaCteOne = _db.UsaPostgres ? "esctacte = 1::bit" : "COALESCE(esctacte, 0) = 1";
            var castDec = _db.UsaPostgres ? "NUMERIC(18,2)" : "DECIMAL(18,2)";

            var sql = $@"
                SELECT
                    COALESCE(SUM(CAST(f.importefinal AS {castDec})), 0) AS TotalVendido,
                    COUNT(DISTINCT f.numeroremito)                          AS CantidadTransacciones,
                    COALESCE((
                        SELECT SUM(v.cantidad)
                        FROM ventas v
                        WHERE CAST(v.fecha AS DATE) BETWEEN @desde AND @hasta
                          AND EXISTS (
                              SELECT 1 FROM facturas f2
                              WHERE f2.numeroremito = v.nrofactura
                                AND CAST(f2.fecha AS DATE) BETWEEN @desde AND @hasta
                                AND COALESCE(f2.cajero, '') <> ''
                                AND f2.{esCtaCteZero}
                          )
                    ), 0)                                                    AS CantidadProductos,
                    COALESCE(SUM(CASE WHEN LOWER(f.formadepago) = 'efectivo'
                        THEN CAST(f.importefinal AS {castDec}) ELSE 0 END), 0) AS TotalEfectivo,
                    COALESCE(SUM(CASE WHEN LOWER(f.formadepago) LIKE '%mercado%pago%'
                        THEN CAST(f.importefinal AS {castDec}) ELSE 0 END), 0) AS TotalMercadoPago,
                    COALESCE(SUM(CASE WHEN LOWER(f.formadepago) = 'dni'
                        THEN CAST(f.importefinal AS {castDec}) ELSE 0 END), 0) AS TotalDni,
                    COALESCE((
                        SELECT SUM(CAST(fc.importefinal AS {castDec}))
                        FROM facturas fc
                        WHERE CAST(fc.fecha AS DATE) BETWEEN @desde AND @hasta
                          AND fc.{esCtaCteOne}
                    ), 0)                                                    AS TotalCtaCte,
                    COALESCE(SUM(CASE WHEN LOWER(f.formadepago) NOT IN ('efectivo', 'dni')
                                     AND LOWER(f.formadepago) NOT LIKE '%mercado%pago%'
                        THEN CAST(f.importefinal AS {castDec}) ELSE 0 END), 0) AS TotalOtros,
                    COALESCE(SUM(CASE WHEN f.tipofactura IN ('FacturaA','FacturaB','FacturaC')
                        THEN CAST(f.importefinal AS {castDec}) ELSE 0 END), 0) AS TotalFacturasElectronicas
                FROM facturas f
                WHERE CAST(f.fecha AS DATE) BETWEEN @desde AND @hasta
                  AND COALESCE(f.cajero, '') <> ''
                  AND f.{esCtaCteZero}
                  {filtrosFactura}";

            var parameters = new Dictionary<string, object?>
            {
                { "@desde", desde.Date },
                { "@hasta", hasta.Date }
            };

            if (numeroCajero.HasValue)
                parameters["@numeroCajero"] = numeroCajero.Value;

            if (!string.IsNullOrWhiteSpace(formaPago))
                parameters["@formaPago"] = formaPago;

            if (!string.IsNullOrWhiteSpace(tipoFactura) &&
                !string.Equals(tipoFactura, "Factura", StringComparison.OrdinalIgnoreCase))
                parameters["@tipoFactura"] = tipoFactura;

            try
            {
                _logger.LogInformation("GetResumenAsync -> desde={Desde}, hasta={Hasta}, cajero={Cajero}, pago={Pago}, tipo={Tipo}",
                    desde, hasta, numeroCajero, formaPago, tipoFactura);

                var rows = await _db.QueryAsync(sql, parameters);

                _logger.LogInformation("GetResumenAsync ? Data rows: {Count}, First row columns: {Cols}",
                    rows.Count, rows.FirstOrDefault()?.Count ?? 0);

                if (rows.Count > 0)
                {
                    var row = rows[0];

                    _logger.LogInformation("GetResumenAsync ? Raw values: [{V}]",
                        string.Join(", ", row.Select(v => v.ToString())));

                    return new ResumenVentasDto
                    {
                        TotalVendido          = ConvertToDecimal(row.Count > 0 ? row[0] : default),
                        CantidadTransacciones = ConvertToInt32(row.Count > 1  ? row[1] : default),
                        CantidadProductos     = ConvertToInt32(row.Count > 2  ? row[2] : default),
                        TotalEfectivo         = ConvertToDecimal(row.Count > 3 ? row[3] : default),
                        TotalMercadoPago      = ConvertToDecimal(row.Count > 4 ? row[4] : default),
                        TotalDni              = ConvertToDecimal(row.Count > 5 ? row[5] : default),
                        TotalCtaCte           = ConvertToDecimal(row.Count > 6 ? row[6] : default),
                        TotalOtros            = ConvertToDecimal(row.Count > 7 ? row[7] : default),
                        TotalFacturasElectronicas = ConvertToDecimal(row.Count > 8 ? row[8] : default),
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

        private static int ConvertToInt32(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.Number => value.GetInt32(),
                JsonValueKind.String => int.TryParse(value.GetString(), out var r) ? r : 0,
                _ => 0
            };
        }

        private static decimal ConvertToDecimal(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.Number => value.GetDecimal(),
                JsonValueKind.String => decimal.TryParse(value.GetString(), out var r) ? r : 0m,
                _ => 0m
            };
        }

        private static bool ConvertToBoolean(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.True   => true,
                JsonValueKind.False  => false,
                JsonValueKind.Number => value.GetInt32() != 0,
                JsonValueKind.String => value.GetString() is "1" or "true" or "True",
                _ => false
            };
        }

        private static string ConvertToString(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? string.Empty,
                JsonValueKind.Null   => string.Empty,
                _                   => value.ToString()
            };
        }

        private static DateTime ConvertToDateTime(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.String)
                return DateTime.TryParse(value.GetString(), out var d) ? d : DateTime.MinValue;
            return DateTime.MinValue;
        }
    }
}
