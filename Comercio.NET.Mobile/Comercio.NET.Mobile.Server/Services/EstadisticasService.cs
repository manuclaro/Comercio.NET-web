using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class EstadisticasService
    {
        private readonly DbService _db;
        private readonly ILogger<EstadisticasService> _logger;

        public EstadisticasService(DbService db, ILogger<EstadisticasService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<EstadisticaVentaDto>> ObtenerVentasPorRubroAsync(DateTime desde, DateTime hasta)
        {
            var query = _db.UsaPostgres
                ? @"
                WITH ventascontotal AS (
                    SELECT
                        v.nrofactura,
                        CASE
                            WHEN UPPER(COALESCE(p.rubro, '')) LIKE '%CARNI%'   THEN 'CARNICERIA'
                            WHEN UPPER(COALESCE(p.rubro, '')) LIKE '%VERDULE%' THEN 'VERDULERIA'
                            WHEN UPPER(COALESCE(p.rubro, '')) LIKE '%PANADE%'
                              OR UPPER(COALESCE(p.rubro, '')) LIKE '%PASTEL%'  THEN 'PANADERIA'
                            WHEN UPPER(COALESCE(p.rubro, '')) LIKE '%FIAMB%'
                              OR UPPER(COALESCE(p.rubro, '')) LIKE '%QUESO%'
                              OR UPPER(COALESCE(p.rubro, '')) LIKE '%EMBUT%'   THEN 'FIAMBRERIA'
                            ELSE 'ALMACEN'
                        END AS Rubro,
                        CAST(v.total AS DECIMAL(18,2)) AS TotalProducto,
                        SUM(CAST(v.total AS DECIMAL(18,2))) OVER (PARTITION BY v.nrofactura) AS TotalFacturaVentas,
                        CAST(f.importefinal AS DECIMAL(18,2)) AS ImporteFinalFactura
                    FROM ventas v
                    INNER JOIN productos p ON v.codigo = p.codigo
                    INNER JOIN facturas f  ON v.nrofactura = f.numeroremito
                    WHERE f.fecha >= @desde::date
                      AND f.fecha <  @hasta::date + INTERVAL '1 day'
                )
                SELECT
                    Rubro,
                    COUNT(DISTINCT nrofactura)  AS CantidadFacturas,
                    COUNT(*)                    AS CantidadProductos,
                    CAST(SUM(
                        CASE
                            WHEN TotalFacturaVentas > 0
                            THEN (TotalProducto / TotalFacturaVentas) * ImporteFinalFactura
                            ELSE 0
                        END
                    ) AS DECIMAL(18,2))         AS TotalVentas
                FROM ventascontotal
                GROUP BY Rubro
                ORDER BY TotalVentas DESC"
                : @"
                WITH ventascontotal AS (
                    SELECT
                        v.nrofactura,
                        CASE
                            WHEN UPPER(COALESCE(p.rubro, '')) LIKE '%CARNI%'   THEN 'CARNICERIA'
                            WHEN UPPER(COALESCE(p.rubro, '')) LIKE '%VERDULE%' THEN 'VERDULERIA'
                            WHEN UPPER(COALESCE(p.rubro, '')) LIKE '%PANADE%'
                              OR UPPER(COALESCE(p.rubro, '')) LIKE '%PASTEL%'  THEN 'PANADERIA'
                            WHEN UPPER(COALESCE(p.rubro, '')) LIKE '%FIAMB%'
                              OR UPPER(COALESCE(p.rubro, '')) LIKE '%QUESO%'
                              OR UPPER(COALESCE(p.rubro, '')) LIKE '%EMBUT%'   THEN 'FIAMBRERIA'
                            ELSE 'ALMACEN'
                        END AS Rubro,
                        CAST(v.total AS DECIMAL(18,2)) AS TotalProducto,
                        SUM(CAST(v.total AS DECIMAL(18,2))) OVER (PARTITION BY v.nrofactura) AS TotalFacturaVentas,
                        CAST(f.importefinal AS DECIMAL(18,2)) AS ImporteFinalFactura
                    FROM ventas v
                    INNER JOIN productos p ON v.codigo = p.codigo
                    INNER JOIN facturas f  ON v.nrofactura = f.numeroremito
                    WHERE CAST(f.fecha AS DATE) >= @desde
                      AND CAST(f.fecha AS DATE) <= @hasta
                )
                SELECT
                    Rubro,
                    COUNT(DISTINCT nrofactura)  AS CantidadFacturas,
                    COUNT(*)                    AS CantidadProductos,
                    CAST(SUM(
                        CASE
                            WHEN TotalFacturaVentas > 0
                            THEN (TotalProducto / TotalFacturaVentas) * ImporteFinalFactura
                            ELSE 0
                        END
                    ) AS DECIMAL(18,2))         AS TotalVentas
                FROM ventascontotal
                GROUP BY Rubro
                ORDER BY TotalVentas DESC";

            var parameters = new Dictionary<string, object?>
            {
                { "@desde", desde.Date },
                { "@hasta", hasta.Date }
            };

            var resultados = new List<EstadisticaVentaDto>();

            try
            {
                var rows = await _db.QueryAsync(query, parameters);

                foreach (var row in rows)
                {
                    resultados.Add(new EstadisticaVentaDto
                    {
                        Rubro = ConvertToString(row.Count > 0 ? row[0] : default),
                        CantidadProductos = ConvertToInt32(row.Count > 2 ? row[2] : default),
                        TotalVentas = ConvertToDecimal(row.Count > 3 ? row[3] : default)
                    });
                }

                _logger.LogInformation("Estadísticas por rubro: {Count} rubro(s) desde {Desde} hasta {Hasta}",
                    resultados.Count, desde, hasta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas de ventas por rubro");
                throw;
            }

            return resultados;
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
                JsonValueKind.Null => string.Empty,
                _ => value.ToString()
            };
        }
    }
}
