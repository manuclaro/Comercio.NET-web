using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;
using Comercio.NET.Mobile.Server.Models;

namespace Comercio.NET.Mobile.Server.Services
{
    public class TurnoService : ITurnoService
    {
        private readonly DbService _db;
        private readonly ILogger<TurnoService> _logger;

        public TurnoService(DbService db, ILogger<TurnoService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<TurnoDto?> GetTurnoActivoAsync()
        {
            var sql = @"SELECT id, numerocajero, usuario, fechaapertura, fechacierre, montoinicial, estado,
                    COALESCE(observaciones, '') AS observaciones
                FROM turnoscajero
                WHERE estado = 'Abierto'
                ORDER BY id DESC
                LIMIT 1";
            try
            {
                var rows = await _db.QueryAsync(sql, new Dictionary<string, object?>());
                if (rows.Count == 0) return null;
                return MapTurno(rows[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetTurnoActivoAsync");
                return null;
            }
        }

        public async Task<TurnoDto> AbrirTurnoAsync(decimal montoInicial = 0, int numeroCajero = 1, string usuario = "")
        {
            var sql = @"INSERT INTO turnoscajero (fechaapertura, estado, montoinicial, numerocajero, usuario)
                VALUES (NOW(), 'Abierto', @montoInicial, @numeroCajero, @usuario)
                RETURNING id, numerocajero, usuario, fechaapertura, fechacierre, montoinicial, estado,
                    COALESCE(observaciones, '') AS observaciones";

            var rows = await _db.QueryAsync(sql, new Dictionary<string, object?> { 
                { "@montoInicial", montoInicial },
                { "@numeroCajero", numeroCajero },
                { "@usuario", usuario }
            });
            var row = rows.FirstOrDefault() ?? throw new Exception("No se pudo obtener el turno creado.");
            return MapTurno(row);
        }

        public async Task<TurnoDto> CerrarTurnoAsync()
        {
            await _db.ExecuteAsync(
                "UPDATE turnoscajero SET fechacierre = NOW(), estado = 'Cerrado' WHERE estado = 'Abierto'",
                new Dictionary<string, object?>());

            var sql = @"SELECT id, numerocajero, usuario, fechaapertura, fechacierre, montoinicial, estado,
                    COALESCE(observaciones, '') AS observaciones
                FROM turnoscajero WHERE estado = 'Cerrado' ORDER BY id DESC LIMIT 1";
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object?>());
            var row = rows.FirstOrDefault() ?? throw new Exception("No se pudo obtener el turno cerrado.");
            return MapTurno(row);
        }

        public async Task<TurnoDto> CerrarTurnoPorIdAsync(int id, List<DeclaracionMedioPago>? declaraciones = null, int cantidadVentas = 0)
        {
            await _db.ExecuteAsync(
                "UPDATE turnoscajero SET fechacierre = NOW(), estado = 'Cerrado' WHERE id = @id",
                new Dictionary<string, object?> { { "@id", id } });

            // Insertar registros en cierreturnocajero por cada medio de pago
            if (declaraciones != null && declaraciones.Count > 0)
            {
                var usuario = (await _db.QueryAsync(
                    "SELECT COALESCE(usuario, '') FROM turnoscajero WHERE id = @id",
                    new Dictionary<string, object?> { { "@id", id } }))
                    .FirstOrDefault()?[0].GetString() ?? "";

                foreach (var d in declaraciones)
                {
                    var diferencia = d.TotalDeclarado - d.TotalEsperado;
                    var sqlInsert = @"INSERT INTO cierreturnocajero 
                        (idturno, mediopago, totalesperado, totaldeclarado, diferencia, cantidadtransacciones, fechacierre, usuariocierre)
                        VALUES (@idturno, @mediopago, @totalesperado, @totaldeclarado, @diferencia, @cantidadtransacciones, NOW(), @usuariocierre)";
                    await _db.ExecuteAsync(sqlInsert, new Dictionary<string, object?>
                    {
                        { "@idturno", id },
                        { "@mediopago", d.MedioPago },
                        { "@totalesperado", d.TotalEsperado },
                        { "@totaldeclarado", d.TotalDeclarado },
                        { "@diferencia", diferencia },
                        { "@cantidadtransacciones", cantidadVentas },
                        { "@usuariocierre", usuario }
                    });
                }
            }

            var sql = @"SELECT id, numerocajero, usuario, fechaapertura, fechacierre, montoinicial, estado,
                    COALESCE(observaciones, '') AS observaciones
                FROM turnoscajero WHERE id = @id";
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object?> { { "@id", id } });
            var row = rows.FirstOrDefault() ?? throw new Exception("No se pudo obtener el turno cerrado.");
            return MapTurno(row);
        }

        public async Task<List<TurnoDto>> GetTurnosAbiertosAsync()
        {
            var sql = @"SELECT id, numerocajero, usuario, fechaapertura, fechacierre, montoinicial, estado,
                    COALESCE(observaciones, '') AS observaciones
                FROM turnoscajero WHERE estado = 'Abierto' ORDER BY id DESC";
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object?>());
            return rows.Select(MapTurno).ToList();
        }

        public async Task<List<TurnoDto>> GetTurnosDelDiaAsync(DateTime fecha)
        {
            var sql = @"SELECT id, numerocajero, usuario, fechaapertura, fechacierre, montoinicial, estado,
                    COALESCE(observaciones, '') AS observaciones
                FROM turnoscajero WHERE CAST(fechaapertura AS DATE) = @fecha ORDER BY id DESC";
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object?> { { "@fecha", fecha.Date } });
            return rows.Select(MapTurno).ToList();
        }

        public async Task<bool> HayMesasAbiertasAsync()
        {
            var count = await _db.ScalarAsync<long>(
                "SELECT COUNT(*) FROM mesas WHERE estado = 'Abierta'",
                new Dictionary<string, object?>());
            return count > 0;
        }

        public async Task<ResumenTurnoDto?> GetResumenTurnoAsync(int? turnoId = null)
        {
            TurnoDto? turno;
            if (turnoId.HasValue)
            {
                var sql = @"SELECT id, numerocajero, usuario, fechaapertura, fechacierre, montoinicial, estado,
                    COALESCE(observaciones, '') AS observaciones
                    FROM turnoscajero WHERE id = @id";
                var turnoRows = await _db.QueryAsync(sql, new Dictionary<string, object?> { { "@id", turnoId.Value } });
                turno = turnoRows.Count > 0 ? MapTurno(turnoRows[0]) : null;
            }
            else
            {
                turno = await GetTurnoActivoAsync();
            }
            if (turno == null) return null;

            var fechaApertura = turno.FechaApertura;
            var fechaCierre = turno.FechaCierre;
            var prms = new Dictionary<string, object?> { { "@fecha", fechaApertura } };
            var filtroFecha = "hora >= @fecha";
            if (fechaCierre.HasValue)
            {
                filtroFecha = "hora >= @fecha AND hora <= @fechaCierre";
                prms["@fechaCierre"] = fechaCierre.Value;
            }

            // Totales por forma de pago desde facturas cerradas durante el turno
            var sqlVentas = $@"SELECT 
                COUNT(*) as cantidad,
                COALESCE(SUM(importefinal), 0) as total,
                COALESCE(SUM(CASE WHEN LOWER(formadepago) = 'efectivo' THEN importefinal ELSE 0 END), 0) as efectivo,
                COALESCE(SUM(CASE WHEN LOWER(formadepago) = 'dni' THEN importefinal ELSE 0 END), 0) as dni,
                COALESCE(SUM(CASE WHEN LOWER(formadepago) IN ('mercado pago', 'mercadopago') THEN importefinal ELSE 0 END), 0) as mp,
                COALESCE(SUM(CASE WHEN LOWER(formadepago) = 'otro' THEN importefinal ELSE 0 END), 0) as otro,
                COALESCE(SUM(CASE WHEN LOWER(formadepago) = 'cuenta corriente' THEN importefinal ELSE 0 END), 0) as ctacte
                FROM facturas WHERE {filtroFecha}";

            var rows = await _db.QueryAsync(sqlVentas, prms);
            var row = rows.FirstOrDefault();

            decimal totalVentas = 0, efectivo = 0, dni = 0, mp = 0, otro = 0, ctacte = 0;
            int cantidad = 0;
            if (row != null && row.Count >= 7)
            {
                cantidad = GetInt(row, 0);
                totalVentas = GetDecimal(row, 1);
                efectivo = GetDecimal(row, 2);
                dni = GetDecimal(row, 3);
                mp = GetDecimal(row, 4);
                otro = GetDecimal(row, 5);
                ctacte = GetDecimal(row, 6);
            }

            // Pagos a proveedores durante el turno
            decimal pagosProveedores = 0;
            try
            {
                pagosProveedores = await _db.ScalarAsync<decimal>(
                    $"SELECT COALESCE(SUM(importe), 0) FROM pagosproveedores WHERE {filtroFecha}", prms);
            }
            catch { /* tabla puede no existir */ }

            return new ResumenTurnoDto
            {
                IdTurno = turno.Id,
                FechaApertura = turno.FechaApertura,
                MontoInicial = turno.MontoInicial,
                CantidadVentas = cantidad,
                TotalVentas = totalVentas,
                TotalEfectivo = efectivo,
                TotalDNI = dni,
                TotalMercadoPago = mp,
                TotalOtro = otro,
                TotalCtaCte = ctacte,
                PagosProveedores = pagosProveedores,
                EfectivoEnCaja = turno.MontoInicial + efectivo - pagosProveedores
            };
        }

        private static TurnoDto MapTurno(List<JsonElement> row) => new()
        {
            Id            = GetInt(row, 0),
            NumeroCajero  = GetInt(row, 1),
            Usuario       = GetString(row, 2),
            FechaApertura = GetDateTime(row, 3),
            FechaCierre   = GetNullableDateTime(row, 4),
            MontoInicial  = GetDecimal(row, 5),
            Estado        = GetString(row, 6),
            Observaciones = GetString(row, 7),
        };

        private static int GetInt(List<JsonElement> row, int i) =>
            row.Count > i && row[i].ValueKind == JsonValueKind.Number ? row[i].GetInt32() : 0;

        private static decimal GetDecimal(List<JsonElement> row, int i) =>
            row.Count > i && row[i].ValueKind == JsonValueKind.Number ? row[i].GetDecimal() : 0m;

        private static string GetString(List<JsonElement> row, int i) =>
            row.Count > i
                ? (row[i].ValueKind == JsonValueKind.String ? row[i].GetString() ?? "" : row[i].ToString())
                : "";

        private static DateTime GetDateTime(List<JsonElement> row, int i)
        {
            if (row.Count <= i) return DateTime.MinValue;
            if (row[i].ValueKind == JsonValueKind.String)
                return DateTime.TryParse(row[i].GetString(), out var d) ? d : DateTime.MinValue;
            return DateTime.MinValue;
        }

        private static DateTime? GetNullableDateTime(List<JsonElement> row, int i)
        {
            if (row.Count <= i || row[i].ValueKind == JsonValueKind.Null) return null;
            return GetDateTime(row, i);
        }

        public async Task<List<HistorialCierreDto>> GetHistorialCierresAsync(DateTime desde, DateTime hasta, string? cajero = null)
        {
            var filtro = "WHERE fechacierre >= @desde AND fechacierre <= @hasta";
            var prms = new Dictionary<string, object?>
            {
                { "@desde", desde },
                { "@hasta", hasta }
            };

            if (!string.IsNullOrEmpty(cajero))
            {
                filtro += " AND LOWER(usuariocierre) = LOWER(@cajero)";
                prms["@cajero"] = cajero;
            }

            var sql = $@"SELECT id, idturno, mediopago, totalesperado, totaldeclarado, diferencia, 
                        cantidadtransacciones, fechacierre, usuariocierre
                        FROM cierreturnocajero 
                        {filtro}
                        ORDER BY fechacierre DESC, idturno, id";

            var rows = await _db.QueryAsync(sql, prms);

            var agrupado = new Dictionary<int, HistorialCierreDto>();

            foreach (var row in rows)
            {
                var idTurno = GetInt(row, 1);
                if (!agrupado.ContainsKey(idTurno))
                {
                    agrupado[idTurno] = new HistorialCierreDto
                    {
                        IdTurno = idTurno,
                        FechaCierre = GetDateTime(row, 7),
                        UsuarioCierre = GetString(row, 8)
                    };
                }

                agrupado[idTurno].Detalles.Add(new DetalleCierreDto
                {
                    MedioPago = GetString(row, 2),
                    TotalEsperado = GetDecimal(row, 3),
                    TotalDeclarado = GetDecimal(row, 4),
                    Diferencia = GetDecimal(row, 5),
                    CantidadTransacciones = GetInt(row, 6)
                });
            }

            return agrupado.Values.ToList();
        }

        public async Task<List<string>> GetCajerosAsync()
        {
            var sql = "SELECT DISTINCT usuariocierre FROM cierreturnocajero WHERE usuariocierre IS NOT NULL AND usuariocierre <> '' ORDER BY usuariocierre";
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object?>());
            return rows.Select(r => GetString(r, 0)).Where(s => !string.IsNullOrEmpty(s)).ToList();
        }
    }
}
