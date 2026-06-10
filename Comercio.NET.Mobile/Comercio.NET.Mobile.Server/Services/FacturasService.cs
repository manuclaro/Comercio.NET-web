using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class FacturasService
    {
        private readonly DbService _db;
        private readonly ILogger<FacturasService> _logger;

        public FacturasService(DbService db, ILogger<FacturasService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<FacturaDto>> ObtenerFacturasAsync(DateTime desde, DateTime hasta, string? cajero = null, string? formaPago = null, string? tipoFactura = null)
        {
            var castType = _db.UsaPostgres ? "NUMERIC(18,2)" : "DECIMAL(18,2)";
            var esCtaCteFilter = _db.UsaPostgres ? "COALESCE(f.esctacte, FALSE) IS FALSE" : "f.esctacte = 0";

            var filtros = "";
            var parameters = new Dictionary<string, object?>
            {
                { "@desde", desde.Date },
                { "@hasta", hasta.Date }
            };

            if (!string.IsNullOrEmpty(cajero))
            {
                filtros += " AND f.cajero = @cajero";
                parameters["@cajero"] = cajero;
            }
            if (!string.IsNullOrEmpty(formaPago))
            {
                filtros += " AND f.formadepago = @formaPago";
                parameters["@formaPago"] = formaPago;
            }
            if (!string.IsNullOrEmpty(tipoFactura))
            {
                filtros += " AND f.tipofactura = @tipoFactura";
                parameters["@tipoFactura"] = tipoFactura;
            }

            var sql = $@"
                SELECT 
                    f.idfactura,
                    f.numeroremito,
                    COALESCE(f.nrofactura, '') as nrofactura,
                    CAST(COALESCE(f.importefinal, 0) AS {castType}) as importefinal,
                    CAST(COALESCE(f.porcentajedescuento, 0) AS {castType}) as porcentajedescuento,
                    CAST(COALESCE(f.importedescuento, 0) AS {castType}) as importedescuento,
                    CAST(COALESCE(f.iva, 0) AS {castType}) as iva,
                    CAST(COALESCE(f.importefinal, 0) - COALESCE(f.iva, 0) AS {castType}) as subtotal,
                    COALESCE(f.cajero, '') as cajero,
                    f.fecha,
                    f.hora,
                    COALESCE(f.formadepago, '') as formadepago,
                    COALESCE(f.tipofactura, '') as tipofactura,
                    COALESCE(f.caenumero, '') as caenumero,
                    COALESCE(f.ctactenombre, '') as ctactenombre,
                    COALESCE(f.usuarioventa, '') as usuarioventa
                FROM facturas f
                WHERE CAST(f.fecha AS DATE) BETWEEN @desde AND @hasta
                AND COALESCE(f.cajero, '') <> ''
                {filtros}
                ORDER BY f.numeroremito DESC
                LIMIT 500";

            if (!_db.UsaPostgres)
                sql = sql.Replace("LIMIT 500", "");

            try
            {
                var rows = await _db.QueryAsync(sql, parameters);
                var facturas = new List<FacturaDto>();

                foreach (var row in rows)
                {
                    facturas.Add(new FacturaDto
                    {
                        IdFactura = GetInt(row, 0),
                        NumeroRemito = GetInt(row, 1),
                        NroFactura = GetString(row, 2),
                        ImporteFinal = GetDecimal(row, 3),
                        PorcentajeDescuento = GetDecimal(row, 4),
                        ImporteDescuento = GetDecimal(row, 5),
                        Iva = GetDecimal(row, 6),
                        Subtotal = GetDecimal(row, 7),
                        Cajero = GetString(row, 8),
                        Fecha = GetDateTime(row, 9),
                        Hora = GetString(row, 10),
                        FormaDePago = GetString(row, 11),
                        TipoFactura = GetString(row, 12),
                        CaeNumero = GetString(row, 13),
                        CtaCteNombre = GetString(row, 14),
                        UsuarioVenta = GetString(row, 15),
                    });
                }

                _logger.LogInformation("Facturas: {C} resultado(s)", facturas.Count);
                return facturas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo facturas");
                throw;
            }
        }

        public async Task<ResumenFacturasDto> ObtenerResumenAsync(DateTime desde, DateTime hasta, string? cajero = null, string? formaPago = null, string? tipoFactura = null)
        {
            var castType = _db.UsaPostgres ? "NUMERIC(18,2)" : "DECIMAL(18,2)";
            var esCtaCteFilter = _db.UsaPostgres ? "COALESCE(f.esctacte, FALSE) IS FALSE" : "f.esctacte = 0";

            var filtros = "";
            var parameters = new Dictionary<string, object?>
            {
                { "@desde", desde.Date },
                { "@hasta", hasta.Date }
            };

            if (!string.IsNullOrEmpty(cajero))
            {
                filtros += " AND f.cajero = @cajero";
                parameters["@cajero"] = cajero;
            }
            if (!string.IsNullOrEmpty(formaPago))
            {
                filtros += " AND f.formadepago = @formaPago";
                parameters["@formaPago"] = formaPago;
            }
            if (!string.IsNullOrEmpty(tipoFactura))
            {
                filtros += " AND f.tipofactura = @tipoFactura";
                parameters["@tipoFactura"] = tipoFactura;
            }

            var esCtaCteTrue = _db.UsaPostgres ? "COALESCE(f.esctacte, FALSE) IS TRUE" : "COALESCE(f.esctacte, 0) = 1";

            var sql = $@"
                SELECT
                    COUNT(*) as cantidad,
                    COALESCE(SUM(CAST(f.importefinal AS {castType})), 0) as total,
                    COALESCE(SUM(CASE WHEN {esCtaCteFilter} AND LOWER(f.formadepago) = 'efectivo' THEN CAST(f.importefinal AS {castType}) ELSE 0 END), 0) as efectivo,
                    COALESCE(SUM(CASE WHEN {esCtaCteFilter} AND LOWER(f.formadepago) LIKE '%mercado%' THEN CAST(f.importefinal AS {castType}) ELSE 0 END), 0) as mercadopago,
                    COALESCE(SUM(CASE WHEN {esCtaCteFilter} AND LOWER(f.formadepago) = 'dni' THEN CAST(f.importefinal AS {castType}) ELSE 0 END), 0) as dni,
                    COALESCE(SUM(CASE WHEN {esCtaCteFilter} AND LOWER(f.formadepago) NOT IN ('efectivo','dni','cuenta corriente') AND LOWER(f.formadepago) NOT LIKE '%mercado%' THEN CAST(f.importefinal AS {castType}) ELSE 0 END), 0) as otros,
                    COALESCE(SUM(CASE WHEN {esCtaCteFilter} AND f.tipofactura IN ('FacturaA','FacturaB','FacturaC') THEN CAST(f.importefinal AS {castType}) ELSE 0 END), 0) as facturaselectronicas,
                    COALESCE(SUM(CASE WHEN {esCtaCteTrue} THEN CAST(f.importefinal AS {castType}) ELSE 0 END), 0) as ctacte
                FROM facturas f
                WHERE CAST(f.fecha AS DATE) BETWEEN @desde AND @hasta
                AND COALESCE(f.cajero, '') <> ''
                {filtros}";

            try
            {
                var rows = await _db.QueryAsync(sql, parameters);
                if (rows.Count > 0)
                {
                    var row = rows[0];
                    return new ResumenFacturasDto
                    {
                        CantidadFacturas = GetInt(row, 0),
                        TotalImporte = GetDecimal(row, 1),
                        TotalEfectivo = GetDecimal(row, 2),
                        TotalMercadoPago = GetDecimal(row, 3),
                        TotalDni = GetDecimal(row, 4),
                        TotalOtros = GetDecimal(row, 5),
                        TotalFacturasElectronicas = GetDecimal(row, 6),
                        TotalCtaCte = GetDecimal(row, 7),
                    };
                }
                return new ResumenFacturasDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen facturas");
                throw;
            }
        }

        private static string GetString(List<JsonElement> row, int i) =>
            row.Count > i ? (row[i].ValueKind == JsonValueKind.String ? row[i].GetString() ?? "" : row[i].ToString()) : "";
        private static int GetInt(List<JsonElement> row, int i) =>
            row.Count > i && row[i].ValueKind == JsonValueKind.Number ? row[i].GetInt32() : 0;
        private static decimal GetDecimal(List<JsonElement> row, int i) =>
            row.Count > i && row[i].ValueKind == JsonValueKind.Number ? row[i].GetDecimal() : 0m;
        private static DateTime GetDateTime(List<JsonElement> row, int i)
        {
            if (row.Count <= i) return DateTime.MinValue;
            var el = row[i];
            if (el.ValueKind == JsonValueKind.String && DateTime.TryParse(el.GetString(), out var dt)) return dt;
            return DateTime.MinValue;
        }
    }
}
