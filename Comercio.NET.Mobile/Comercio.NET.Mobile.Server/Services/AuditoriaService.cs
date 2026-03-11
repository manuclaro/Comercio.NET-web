using Comercio.NET.Mobile.Server.Models;
using Microsoft.Data.SqlClient;

namespace Comercio.NET.Mobile.Server.Services
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly string _connectionString;

        public AuditoriaService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<AuditoriaDto>> GetAuditoriaAsync(DateTime desde, DateTime hasta, string usuario = null, int? numeroCajero = null)
        {
            var registros = new List<AuditoriaDto>();

            var sql = @"
                SELECT 
                    Id, CodigoProducto, DescripcionProducto, PrecioUnitario,
                    Cantidad, TotalEliminado, NumeroFactura, FechaEliminacion,
                    MotivoEliminacion, UsuarioEliminacion, NumeroCajero,
                    NombreEquipo, ISNULL(EsCtaCte, 0) AS EsCtaCte,
                    ISNULL(NombreCtaCte, '') AS NombreCtaCte,
                    EsEliminacionCompleta, CantidadOriginal
                FROM AuditoriaProductosEliminados
                WHERE CAST(FechaEliminacion AS DATE) BETWEEN @desde AND @hasta";

            if (!string.IsNullOrWhiteSpace(usuario))
                sql += " AND UsuarioEliminacion = @usuario";

            if (numeroCajero.HasValue)
                sql += " AND NumeroCajero = @numeroCajero";

            sql += " ORDER BY FechaEliminacion DESC";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@desde", desde.Date);
            cmd.Parameters.AddWithValue("@hasta", hasta.Date);

            if (!string.IsNullOrWhiteSpace(usuario))
                cmd.Parameters.AddWithValue("@usuario", usuario);

            if (numeroCajero.HasValue)
                cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                registros.Add(new AuditoriaDto
                {
                    Id                    = Convert.ToInt32(reader["Id"]),
                    CodigoProducto        = reader["CodigoProducto"]?.ToString(),
                    DescripcionProducto   = reader["DescripcionProducto"]?.ToString(),
                    PrecioUnitario        = Convert.ToDecimal(reader["PrecioUnitario"]),
                    Cantidad              = Convert.ToInt32(reader["Cantidad"]),
                    TotalEliminado        = Convert.ToDecimal(reader["TotalEliminado"]),
                    NumeroFactura         = Convert.ToInt32(reader["NumeroFactura"]),
                    FechaEliminacion      = Convert.ToDateTime(reader["FechaEliminacion"]),
                    MotivoEliminacion     = reader["MotivoEliminacion"]?.ToString(),
                    UsuarioEliminacion    = reader["UsuarioEliminacion"]?.ToString(),
                    NumeroCajero          = Convert.ToInt32(reader["NumeroCajero"]),
                    NombreEquipo          = reader["NombreEquipo"]?.ToString(),
                    EsCtaCte              = Convert.ToBoolean(reader["EsCtaCte"]),
                    NombreCtaCte          = reader["NombreCtaCte"]?.ToString(),
                    EsEliminacionCompleta = reader["EsEliminacionCompleta"] == DBNull.Value ? null : Convert.ToBoolean(reader["EsEliminacionCompleta"]),
                    CantidadOriginal      = reader["CantidadOriginal"] == DBNull.Value ? null : Convert.ToInt32(reader["CantidadOriginal"])
                });
            }

            return registros;
        }
    }
}