using Comercio.NET.Mobile.Server.Models;
using Microsoft.Data.SqlClient;

namespace Comercio.NET.Mobile.Server.Services
{
    public class VentasService : IVentasService
    {
        private readonly string _connectionString;

        public VentasService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<VentaDto>> GetVentasDelDiaAsync(DateTime fecha, int? numeroCajero = null, string formaPago = null)
        {
            var ventas = new List<VentaDto>();

            var sql = @"
                SELECT 
                    v.id, v.nrofactura, v.codigo, v.descripcion,
                    v.precio, v.cantidad, v.total, v.PorcentajeIva,
                    ISNULL(v.EsOferta, 0) AS EsOferta,
                    ISNULL(v.NombreOferta, '') AS NombreOferta,
                    ISNULL(f.FormadePago, '') AS FormaPago,
                    ISNULL(f.TipoFactura, '') AS TipoFactura,
                    ISNULL(CAST(v.fecha AS DATE), CAST(GETDATE() AS DATE)) AS Fecha,
                    ISNULL(v.hora, '') AS Hora,
                    ISNULL(v.EsCtaCte, 0) AS EsCtaCte,
                    ISNULL(v.NombreCtaCte, '') AS NombreCtaCte,
                    ISNULL(f.UsuarioVenta, '') AS UsuarioVenta,
                    ISNULL(CAST(f.Cajero AS INT), 0) AS NumeroCajero
                FROM Ventas v
                LEFT JOIN Facturas f ON f.NumeroRemito = v.nrofactura
                WHERE CAST(v.fecha AS DATE) = @fecha";

            if (numeroCajero.HasValue)
                sql += " AND CAST(f.Cajero AS INT) = @numeroCajero";

            if (!string.IsNullOrWhiteSpace(formaPago))
                sql += " AND f.FormadePago = @formaPago";

            sql += " ORDER BY v.id DESC";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@fecha", fecha.Date);

            if (numeroCajero.HasValue)
                cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero.Value);

            if (!string.IsNullOrWhiteSpace(formaPago))
                cmd.Parameters.AddWithValue("@formaPago", formaPago);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ventas.Add(new VentaDto
                {
                    Id            = reader.GetInt32(reader.GetOrdinal("id")),
                    NroFactura    = reader.GetInt32(reader.GetOrdinal("nrofactura")),
                    Codigo        = reader["codigo"]?.ToString(),
                    Descripcion   = reader["descripcion"]?.ToString(),
                    Precio        = Convert.ToDecimal(reader["precio"]),
                    Cantidad      = Convert.ToInt32(reader["cantidad"]),
                    Total         = Convert.ToDecimal(reader["total"]),
                    PorcentajeIva = Convert.ToDecimal(reader["PorcentajeIva"]),
                    EsOferta      = Convert.ToBoolean(reader["EsOferta"]),
                    NombreOferta  = reader["NombreOferta"]?.ToString(),
                    FormaPago     = reader["FormaPago"]?.ToString(),
                    TipoFactura   = reader["TipoFactura"]?.ToString(),
                    Fecha         = Convert.ToDateTime(reader["Fecha"]),
                    Hora          = reader["Hora"]?.ToString(),
                    EsCtaCte      = Convert.ToBoolean(reader["EsCtaCte"]),
                    NombreCtaCte  = reader["NombreCtaCte"]?.ToString(),
                    UsuarioVenta  = reader["UsuarioVenta"]?.ToString(),
                    NumeroCajero  = Convert.ToInt32(reader["NumeroCajero"])
                });
            }

            return ventas;
        }

        public async Task<ResumenVentasDto> GetResumenAsync(DateTime fecha, int? numeroCajero = null)
        {
            var sql = @"
                SELECT
                    ISNULL(SUM(f.ImporteFinal), 0)                                              AS TotalVendido,
                    COUNT(DISTINCT f.NumeroRemito)                                              AS CantidadTransacciones,
                    ISNULL(SUM(v.cantidad), 0)                                                  AS CantidadProductos,
                    ISNULL(SUM(CASE WHEN LOWER(f.FormadePago) = 'efectivo' THEN f.ImporteFinal ELSE 0 END), 0) AS TotalEfectivo,
                    ISNULL(SUM(CASE WHEN LOWER(f.FormadePago) LIKE '%tarjeta%' THEN f.ImporteFinal ELSE 0 END), 0) AS TotalTarjeta,
                    ISNULL(SUM(CASE WHEN f.esCtaCte = 1 THEN f.ImporteFinal ELSE 0 END), 0)    AS TotalCtaCte,
                    ISNULL(SUM(CASE WHEN LOWER(f.FormadePago) NOT IN ('efectivo') 
                                     AND LOWER(f.FormadePago) NOT LIKE '%tarjeta%'
                                     AND f.esCtaCte = 0 THEN f.ImporteFinal ELSE 0 END), 0)   AS TotalOtros
                FROM Facturas f
                INNER JOIN Ventas v ON v.nrofactura = f.NumeroRemito
                WHERE CAST(f.Fecha AS DATE) = @fecha";

            if (numeroCajero.HasValue)
                sql += " AND CAST(f.Cajero AS INT) = @numeroCajero";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@fecha", fecha.Date);

            if (numeroCajero.HasValue)
                cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ResumenVentasDto
                {
                    TotalVendido          = Convert.ToDecimal(reader["TotalVendido"]),
                    CantidadTransacciones = Convert.ToInt32(reader["CantidadTransacciones"]),
                    CantidadProductos     = Convert.ToInt32(reader["CantidadProductos"]),
                    TotalEfectivo         = Convert.ToDecimal(reader["TotalEfectivo"]),
                    TotalTarjeta          = Convert.ToDecimal(reader["TotalTarjeta"]),
                    TotalCtaCte           = Convert.ToDecimal(reader["TotalCtaCte"]),
                    TotalOtros            = Convert.ToDecimal(reader["TotalOtros"])
                };
            }

            return new ResumenVentasDto();
        }
    }
}