using Microsoft.Data.SqlClient;
using Comercio.NET.Mobile.Server.Models;

namespace Comercio.NET.Mobile.Server.Services
{
    public class ArqueoCajaService
    {
        private readonly string _connectionString;
        private readonly ILogger<ArqueoCajaService> _logger;

        public ArqueoCajaService(IConfiguration configuration, ILogger<ArqueoCajaService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string no configurada");
            _logger = logger;
        }

        public async Task<List<string>> ObtenerCajerosAsync()
        {
            var cajeros = new List<string>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT DISTINCT Cajero
                    FROM Facturas
                    WHERE ISNULL(Cajero, '') <> ''
                    ORDER BY Cajero";

                using var cmd = new SqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    cajeros.Add(reader.GetString(0));
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
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        COUNT(DISTINCT NumeroRemito) as TotalVentas,
                        SUM(CAST(ISNULL(ImporteFinal, 0) AS DECIMAL(18,2))) as TotalIngresos,
                        SUM(CASE WHEN FormadePago = 'DNI' 
                            THEN CAST(ImporteFinal AS DECIMAL(18,2)) ELSE 0 END) as DNI,
                        SUM(CASE WHEN FormadePago = 'Efectivo' 
                            THEN CAST(ImporteFinal AS DECIMAL(18,2)) ELSE 0 END) as Efectivo,
                        SUM(CASE WHEN FormadePago LIKE '%Mercado%Pago%' OR FormadePago = 'MercadoPago'
                            THEN CAST(ImporteFinal AS DECIMAL(18,2)) ELSE 0 END) as MercadoPago,
                        SUM(CASE WHEN FormadePago = 'Otro' 
                            THEN CAST(ImporteFinal AS DECIMAL(18,2)) ELSE 0 END) as Otro
                    FROM Facturas
                    WHERE CAST(Fecha AS DATE) = @fecha
                    AND (Cajero = @cajero OR @cajero IS NULL)
                    AND ISNULL(Cajero, '') <> ''";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.Parameters.AddWithValue("@cajero", string.IsNullOrEmpty(cajero) ? DBNull.Value : cajero);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    resultado.CantidadVentas = reader.GetInt32(0);
                    resultado.TotalIngresos = reader.GetDecimal(1);
                    resultado.DNI = reader.GetDecimal(2);
                    resultado.Efectivo = reader.GetDecimal(3);
                    resultado.MercadoPago = reader.GetDecimal(4);
                    resultado.Otro = reader.GetDecimal(5);
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo arqueo");
                throw;
            }
        }
    }
}