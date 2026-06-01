using System.Text.Json;
using Comercio.NET.Mobile.Server.Models;

namespace Comercio.NET.Mobile.Server.Services
{
    public class ArqueoCajaService
    {
        private readonly DbService _db;
        private readonly ILogger<ArqueoCajaService> _logger;

        public ArqueoCajaService(DbService db, ILogger<ArqueoCajaService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<string>> ObtenerCajerosAsync()
        {
            try
            {
                var query = @"
                    SELECT DISTINCT Cajero
                    FROM Facturas
                    WHERE COALESCE(Cajero, '') <> ''
                    ORDER BY Cajero";

                var rows = await _db.QueryAsync(query, new Dictionary<string, object?>());

                var cajeros = new List<string>();
                foreach (var row in rows)
                {
                    if (row.Count > 0 && row[0].ValueKind != JsonValueKind.Null)
                    {
                        cajeros.Add(ConvertToString(row[0]));
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
                var esCtaCteFilter = _db.UsaPostgres ? "esctacte = 0::bit" : "esctacte = 0";
                var cajeroFilter = string.IsNullOrEmpty(cajero) ? "" : "AND Cajero = @cajero";
                var cajeroFilterPP = string.IsNullOrEmpty(cajero) ? "" : "AND UsuarioRegistro = @cajero";
                var castType = _db.UsaPostgres ? "NUMERIC(18,2)" : "DECIMAL(18,2)";
                var query = $@"
                    SELECT 
                        COUNT(DISTINCT NumeroRemito) as TotalVentas,
                        SUM(CAST(COALESCE(ImporteFinal, 0) AS {castType})) as TotalIngresos,
                        SUM(CASE WHEN FormadePago = 'DNI' 
                            THEN CAST(ImporteFinal AS {castType}) ELSE 0 END) as DNI,
                        SUM(CASE WHEN FormadePago = 'Efectivo' 
                            THEN CAST(ImporteFinal AS {castType}) ELSE 0 END) as Efectivo,
                        SUM(CASE WHEN FormadePago LIKE '%Mercado%' OR FormadePago = 'MercadoPago'
                            THEN CAST(ImporteFinal AS {castType}) ELSE 0 END) as MercadoPago,
                        SUM(CASE WHEN FormadePago = 'Otro' 
                            THEN CAST(ImporteFinal AS {castType}) ELSE 0 END) as Otro,
                        SUM(CASE WHEN TipoFactura = 'FacturaC' OR TipoFactura = 'Factura C' OR TipoFactura = 'C'
                            THEN CAST(ImporteFinal AS {castType}) ELSE 0 END) as FacturaC,
                        COALESCE((
                            SELECT SUM(CAST(Monto AS {castType}))
                            FROM PagosProveedores
                            WHERE CAST(FechaPago AS DATE) = @fecha
                            {cajeroFilterPP}
                        ), 0) as PagosProveedores
                    FROM Facturas
                    WHERE CAST(Fecha AS DATE) = @fecha
                    AND {esCtaCteFilter}
                    {cajeroFilter}
                    AND COALESCE(Cajero, '') <> ''";

                var parameters = new Dictionary<string, object?>
                {
                    { "@fecha", fecha.Date }
                };
                if (!string.IsNullOrEmpty(cajero))
                    parameters["@cajero"] = cajero;

                var rows = await _db.QueryAsync(query, parameters);

                if (rows.Count > 0)
                {
                    var row = rows[0];

                    resultado.CantidadVentas = ConvertToInt32(row.Count > 0 ? row[0] : default);
                    resultado.TotalIngresos = ConvertToDecimal(row.Count > 1 ? row[1] : default);
                    resultado.DNI = ConvertToDecimal(row.Count > 2 ? row[2] : default);
                    resultado.Efectivo = ConvertToDecimal(row.Count > 3 ? row[3] : default);
                    resultado.MercadoPago = ConvertToDecimal(row.Count > 4 ? row[4] : default);
                    resultado.Otro = ConvertToDecimal(row.Count > 5 ? row[5] : default);
                    resultado.FacturaC = ConvertToDecimal(row.Count > 6 ? row[6] : default);
                    resultado.PagosProveedores = ConvertToDecimal(row.Count > 7 ? row[7] : default);
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo arqueo");
                throw;
            }
        }

        public async Task<List<DetallePagoProveedorDto>> ObtenerDetallePagosProveedoresAsync(DateTime fecha, string? cajero = null)
        {
            _logger.LogInformation("Iniciando consulta de pagos a proveedores - Fecha: {Fecha}, Cajero: {Cajero}", 
                fecha.ToString("yyyy-MM-dd"), cajero ?? "NULL");

            try
            {
                var cajeroFilterPP2 = string.IsNullOrEmpty(cajero) ? "" : "AND pp.UsuarioRegistro = @cajero";
                var query = $@"
                    SELECT 
                        pp.Id,
                        pp.Proveedor,
                        pp.Monto,
                        pp.FechaPago,
                        COALESCE(pp.Observaciones, '') as Observaciones,
                        COALESCE(pp.UsuarioRegistro, '') as UsuarioRegistro,
                        pp.NumeroCajero,
                        pp.NumeroRemito,
                        COALESCE(pp.NombreEquipo, '') as NombreEquipo,
                        pp.FechaRegistro,
                        pp.IdProveedor,
                        pp.CompraId,
                        pp.CtaCteId,
                        COALESCE(pp.Origen, '') as Origen
                    FROM PagosProveedores pp
                    WHERE CAST(pp.FechaPago AS DATE) = @fecha
                    {cajeroFilterPP2}
                    ORDER BY pp.FechaPago DESC";

                var parameters = new Dictionary<string, object?>
                {
                    { "@fecha", fecha.Date }
                };
                if (!string.IsNullOrEmpty(cajero))
                    parameters["@cajero"] = cajero;

                var rows = await _db.QueryAsync(query, parameters);

                var pagos = new List<DetallePagoProveedorDto>();

                if (rows.Count > 0)
                {
                    _logger.LogInformation("Procesando {Count} filas", rows.Count);

                    foreach (var row in rows)
                    {
                        try
                        {
                            pagos.Add(new DetallePagoProveedorDto
                            {
                                Id = ConvertToInt32(row.Count > 0 ? row[0] : default),
                                Proveedor = ConvertToString(row.Count > 1 ? row[1] : default),
                                Monto = ConvertToDecimal(row.Count > 2 ? row[2] : default),
                                FechaPago = ConvertToDateTime(row.Count > 3 ? row[3] : default),
                                Observaciones = ConvertToString(row.Count > 4 ? row[4] : default),
                                UsuarioRegistro = ConvertToString(row.Count > 5 ? row[5] : default),
                                NumeroCajero = ConvertToInt32(row.Count > 6 ? row[6] : default),
                                NumeroRemito = row.Count > 7 && row[7].ValueKind != JsonValueKind.Null ? ConvertToInt32(row[7]) : null,
                                NombreEquipo = ConvertToString(row.Count > 8 ? row[8] : default),
                                FechaRegistro = ConvertToDateTime(row.Count > 9 ? row[9] : default),
                                IdProveedor = row.Count > 10 && row[10].ValueKind != JsonValueKind.Null ? ConvertToInt32(row[10]) : null,
                                CompraId = row.Count > 11 && row[11].ValueKind != JsonValueKind.Null ? ConvertToInt32(row[11]) : null,
                                CtaCteId = row.Count > 12 && row[12].ValueKind != JsonValueKind.Null ? ConvertToInt32(row[12]) : null,
                                Origen = ConvertToString(row.Count > 13 ? row[13] : default)
                            });
                        }
                        catch (Exception exRow)
                        {
                            _logger.LogError(exRow, "Error procesando fila de pago a proveedor");
                        }
                    }
                }

                _logger.LogInformation("Consulta finalizada - Total pagos: {Count}", pagos.Count);
                return pagos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo detalle de pagos a proveedores");
                return new List<DetallePagoProveedorDto>();
            }
        }

        private static DateTime ConvertToDateTime(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.String)
                return DateTime.TryParse(value.GetString(), out var d) ? d : DateTime.MinValue;
            return DateTime.MinValue;
        }

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
                JsonValueKind.String => decimal.TryParse(value.GetString(), out var r) ? r : 0,
                _ => 0
            };
        }

        private static string ConvertToString(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? string.Empty,
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.Null => string.Empty,
                _ => value.ToString()
            };
        }
    }
}
