using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly DbService _db;
        private readonly ILogger<AuditoriaService> _logger;

        public AuditoriaService(DbService db, ILogger<AuditoriaService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<AuditoriaDto>> GetAuditoriaAsync(DateTime desde, DateTime hasta, string usuario = null, int? numeroCajero = null)
        {
            var registros = new List<AuditoriaDto>();

            var esCtaCteCoalesce = _db.UsaPostgres ? "COALESCE(EsCtaCte, 0::bit)" : "COALESCE(EsCtaCte, 0)";

            var sql = $@"
                SELECT
                    IdAuditoriaProductosEliminados,
                    CodigoProducto,
                    DescripcionProducto,
                    PrecioUnitario,
                    Cantidad,
                    TotalEliminado,
                    NumeroFactura,
                    FechaEliminacion,
                    COALESCE(UsuarioEliminacion, '')       AS UsuarioEliminacion,
                    COALESCE(MotivoEliminacion, '')        AS MotivoEliminacion,
                    {esCtaCteCoalesce}                     AS EsCtaCte,
                    COALESCE(NombreCtaCte, '')             AS NombreCtaCte,
                    COALESCE(IPUsuario, '')                AS IPUsuario,
                    COALESCE(NombreEquipo, '')             AS NombreEquipo,
                    FechaHoraVentaOriginal,
                    COALESCE(NumeroCajero, 0)             AS NumeroCajero,
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
                { "@desde", desde.Date },
                { "@hasta", hasta.Date }
            };

            if (!string.IsNullOrWhiteSpace(usuario))
                parameters["@usuario"] = usuario;

            if (numeroCajero.HasValue)
                parameters["@numeroCajero"] = numeroCajero.Value;

            try
            {
                var rows = await _db.QueryAsync(sql, parameters);

                foreach (var row in rows)
                {
                    registros.Add(new AuditoriaDto
                    {
                        Id                     = ConvertToInt32(row.Count > 0  ? row[0]  : default),
                        CodigoProducto         = ConvertToString(row.Count > 1  ? row[1]  : default),
                        DescripcionProducto    = ConvertToString(row.Count > 2  ? row[2]  : default),
                        PrecioUnitario         = ConvertToDecimal(row.Count > 3  ? row[3]  : default),
                        Cantidad               = ConvertToInt32(row.Count > 4  ? row[4]  : default),
                        TotalEliminado         = ConvertToDecimal(row.Count > 5  ? row[5]  : default),
                        NumeroFactura          = ConvertToInt32(row.Count > 6  ? row[6]  : default),
                        FechaEliminacion       = ConvertToDateTime(row.Count > 7  ? row[7]  : default),
                        UsuarioEliminacion     = ConvertToString(row.Count > 8  ? row[8]  : default),
                        MotivoEliminacion      = ConvertToString(row.Count > 9  ? row[9]  : default),
                        EsCtaCte               = ConvertToBoolean(row.Count > 10 ? row[10] : default),
                        NombreCtaCte           = ConvertToString(row.Count > 11 ? row[11] : default),
                        IPUsuario              = ConvertToString(row.Count > 12 ? row[12] : default),
                        NombreEquipo           = ConvertToString(row.Count > 13 ? row[13] : default),
                        FechaHoraVentaOriginal = ConvertToNullableDateTime(row.Count > 14 ? row[14] : default),
                        NumeroCajero           = ConvertToInt32(row.Count > 15 ? row[15] : default),
                        IvaEliminado           = ConvertToNullableDecimal(row.Count > 16 ? row[16] : default),
                        CantidadOriginal       = ConvertToNullableInt32(row.Count > 17 ? row[17] : default),
                        EsEliminacionCompleta  = ConvertToNullableBoolean(row.Count > 18 ? row[18] : default),
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo auditoría desde {Desde} hasta {Hasta}", desde, hasta);
                throw;
            }

            return registros;
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

        private static int? ConvertToNullableInt32(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined) return null;
            return ConvertToInt32(value);
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

        private static decimal? ConvertToNullableDecimal(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined) return null;
            return ConvertToDecimal(value);
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

        private static bool? ConvertToNullableBoolean(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined) return null;
            return ConvertToBoolean(value);
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

        private static DateTime? ConvertToNullableDateTime(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined) return null;
            return ConvertToDateTime(value);
        }
    }
}
