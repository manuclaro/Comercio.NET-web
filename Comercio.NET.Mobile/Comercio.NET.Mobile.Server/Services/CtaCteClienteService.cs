using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class CtaCteClienteService
    {
        private readonly DbService _db;
        private readonly ILogger<CtaCteClienteService> _logger;

        public CtaCteClienteService(DbService db, ILogger<CtaCteClienteService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<ClienteCtaCteDto>> ListarClientesAsync(string? buscar = null)
        {
            // La tabla de clientes ctacte se deduce del uso en ventas: la columna "nombrectacte" en facturas
            // y la tabla configurable. Usamos la tabla "ctacte" con columnas: id, nombre, telefono, email, dni, activo
            // Si no existe esa tabla, devolvemos los nombres únicos de facturas como clientes.
            var likeOp = _db.UsaPostgres ? "ILIKE" : "LIKE";
            var parms = new Dictionary<string, object?>();
            string whereExtra = "";
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                parms["@b"] = $"%{buscar.Trim()}%";
                whereExtra = $" AND (c.nombre {likeOp} @b OR c.dni LIKE @b OR c.telefono LIKE @b)";
            }

            // Calculamos saldo como suma de ventas ctacte menos pagos registrados
            var activoVal = _db.UsaPostgres ? "TRUE" : "1";
            var sql = $@"
                SELECT 
                    c.id,
                    c.nombre,
                    COALESCE(c.telefono, '') AS telefono,
                    COALESCE(c.email, '') AS email,
                    COALESCE(c.dni, '') AS dni,
                    COALESCE(c.activo, {activoVal}) AS activo,
                    COALESCE(ventas.total_compras, 0) AS total_compras,
                    COALESCE(pagos.total_pagado, 0) AS total_pagado,
                    MAX(ventas.ultima_compra) AS ultima_compra,
                    MAX(pagos.ultimo_pago) AS ultimo_pago
                FROM ctacte c
                LEFT JOIN (
                    SELECT LOWER(nombrectacte) AS nombre_key, SUM(f.total) AS total_compras, MAX(f.fecha) AS ultima_compra
                    FROM facturas f
                    WHERE f.esctacte = {activoVal} AND f.nombrectacte IS NOT NULL
                    GROUP BY LOWER(nombrectacte)
                ) ventas ON LOWER(c.nombre) = ventas.nombre_key
                LEFT JOIN (
                    SELECT cliente_id, SUM(monto) AS total_pagado, MAX(fecha) AS ultimo_pago
                    FROM pagosctacte
                    GROUP BY cliente_id
                ) pagos ON pagos.cliente_id = c.id
                WHERE c.activo = {activoVal}{whereExtra}
                GROUP BY c.id, c.nombre, c.telefono, c.email, c.dni, c.activo, ventas.total_compras, pagos.total_pagado
                ORDER BY c.nombre";

            try
            {
                var rows = await _db.QueryAsync(sql, parms);
                return rows.Select(r =>
                {
                    var totalCompras = GetDecimal(r, 6);
                    var totalPagado  = GetDecimal(r, 7);
                    return new ClienteCtaCteDto
                    {
                        Id            = GetInt(r, 0),
                        Nombre        = GetStr(r, 1),
                        Telefono      = GetStr(r, 2),
                        Email         = GetStr(r, 3),
                        Dni           = GetStr(r, 4),
                        Activo        = GetBool(r, 5),
                        TotalCompras  = totalCompras,
                        TotalPagado   = totalPagado,
                        SaldoDeudor   = totalCompras - totalPagado,
                        UltimaCompra  = GetDateTimeNullable(r, 8),
                        UltimoPago    = GetDateTimeNullable(r, 9)
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando clientes ctacte, intentando consulta simplificada");
                // Fallback: tabla sin joins de ventas/pagos
                var sqlSimple = $"SELECT id, nombre, COALESCE(telefono,'') AS telefono, COALESCE(email,'') AS email, COALESCE(dni,'') AS dni, activo FROM ctacte WHERE activo = {activoVal} ORDER BY nombre";
                var rows2 = await _db.QueryAsync(sqlSimple, parms.ContainsKey("@b") ? new Dictionary<string, object?> { { "@b", parms["@b"] } } : new());
                return rows2.Select(r => new ClienteCtaCteDto
                {
                    Id       = GetInt(r, 0),
                    Nombre   = GetStr(r, 1),
                    Telefono = GetStr(r, 2),
                    Email    = GetStr(r, 3),
                    Dni      = GetStr(r, 4),
                    Activo   = GetBool(r, 5)
                });
            }
        }

        public async Task<ClienteCtaCteDto?> ObtenerClienteAsync(int id)
        {
            var activoVal = _db.UsaPostgres ? "TRUE" : "1";
            var sql = $@"
                SELECT 
                    c.id, c.nombre,
                    COALESCE(c.telefono,'') AS telefono,
                    COALESCE(c.email,'') AS email,
                    COALESCE(c.dni,'') AS dni,
                    c.activo,
                    COALESCE(ventas.total_compras, 0) AS total_compras,
                    COALESCE(pagos.total_pagado, 0) AS total_pagado,
                    MAX(ventas.ultima_compra) AS ultima_compra,
                    MAX(pagos.ultimo_pago) AS ultimo_pago
                FROM ctacte c
                LEFT JOIN (
                    SELECT LOWER(nombrectacte) AS nombre_key, SUM(f.total) AS total_compras, MAX(f.fecha) AS ultima_compra
                    FROM facturas f
                    WHERE f.esctacte = {activoVal} AND f.nombrectacte IS NOT NULL
                    GROUP BY LOWER(nombrectacte)
                ) ventas ON LOWER(c.nombre) = ventas.nombre_key
                LEFT JOIN (
                    SELECT cliente_id, SUM(monto) AS total_pagado, MAX(fecha) AS ultimo_pago
                    FROM pagosctacte GROUP BY cliente_id
                ) pagos ON pagos.cliente_id = c.id
                WHERE c.id = @id
                GROUP BY c.id, c.nombre, c.telefono, c.email, c.dni, c.activo, ventas.total_compras, pagos.total_pagado";

            try
            {
                var rows = await _db.QueryAsync(sql, new() { { "@id", id } });
                if (rows.Count == 0) return null;
                var r = rows[0];
                var totalCompras = GetDecimal(r, 6);
                var totalPagado  = GetDecimal(r, 7);
                return new ClienteCtaCteDto
                {
                    Id           = GetInt(r, 0),
                    Nombre       = GetStr(r, 1),
                    Telefono     = GetStr(r, 2),
                    Email        = GetStr(r, 3),
                    Dni          = GetStr(r, 4),
                    Activo       = GetBool(r, 5),
                    TotalCompras = totalCompras,
                    TotalPagado  = totalPagado,
                    SaldoDeudor  = totalCompras - totalPagado,
                    UltimaCompra = GetDateTimeNullable(r, 8),
                    UltimoPago   = GetDateTimeNullable(r, 9)
                };
            }
            catch
            {
                var sqlSimple = "SELECT id, nombre, COALESCE(telefono,''), COALESCE(email,''), COALESCE(dni,''), activo FROM ctacte WHERE id = @id";
                var rows2 = await _db.QueryAsync(sqlSimple, new() { { "@id", id } });
                if (rows2.Count == 0) return null;
                var r = rows2[0];
                return new ClienteCtaCteDto { Id = GetInt(r, 0), Nombre = GetStr(r, 1), Telefono = GetStr(r, 2), Email = GetStr(r, 3), Dni = GetStr(r, 4), Activo = GetBool(r, 5) };
            }
        }

        public async Task<IEnumerable<MovimientoCtaCteDto>> ObtenerMovimientosAsync(int clienteId)
        {
            var activoVal = _db.UsaPostgres ? "TRUE" : "1";
            var movimientos = new List<MovimientoCtaCteDto>();

            // Obtener nombre del cliente
            var rowsCliente = await _db.QueryAsync("SELECT nombre FROM ctacte WHERE id = @id", new() { { "@id", clienteId } });
            if (rowsCliente.Count == 0) return movimientos;
            var nombreCliente = GetStr(rowsCliente[0], 0);

            // Ventas (facturas) del cliente
            try
            {
                var sqlVentas = $@"
                    SELECT idfactura, COALESCE(total,0) AS total, fecha, COALESCE(nroremito,'') AS nro, COALESCE(formadepago,'') AS fp
                    FROM facturas
                    WHERE esctacte = {activoVal} AND LOWER(nombrectacte) = LOWER(@nombre)
                    ORDER BY fecha DESC";
                var rowsV = await _db.QueryAsync(sqlVentas, new() { { "@nombre", nombreCliente } });
                foreach (var r in rowsV)
                {
                    movimientos.Add(new MovimientoCtaCteDto
                    {
                        Id         = GetInt(r, 0),
                        ClienteId  = clienteId,
                        Tipo       = "Venta",
                        Monto      = GetDecimal(r, 1),
                        Fecha      = GetDateTime(r, 2),
                        NroFactura = GetStr(r, 3),
                        MedioPago  = GetStr(r, 4)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error obteniendo ventas ctacte para cliente {Id}", clienteId);
            }

            // Pagos registrados
            try
            {
                var sqlPagos = @"SELECT id, monto, mediopago, COALESCE(referencia,''), usuario, fecha
                                 FROM pagosctacte WHERE cliente_id = @id ORDER BY fecha DESC";
                var rowsP = await _db.QueryAsync(sqlPagos, new() { { "@id", clienteId } });
                foreach (var r in rowsP)
                {
                    movimientos.Add(new MovimientoCtaCteDto
                    {
                        Id        = GetInt(r, 0),
                        ClienteId = clienteId,
                        Tipo      = "Pago",
                        Monto     = GetDecimal(r, 1),
                        MedioPago = GetStr(r, 2),
                        Referencia= GetStr(r, 3),
                        Usuario   = GetStr(r, 4),
                        Fecha     = GetDateTime(r, 5)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error obteniendo pagos ctacte para cliente {Id}", clienteId);
            }

            return movimientos.OrderByDescending(m => m.Fecha);
        }

        public async Task RegistrarPagoAsync(int clienteId, RegistrarPagoCtaCteDto dto)
        {
            var sql = @"INSERT INTO pagosctacte (cliente_id, monto, mediopago, referencia, usuario, fecha)
                        VALUES (@clienteId, @monto, @medio, @ref, @usuario, @fecha)";
            await _db.ExecuteAsync(sql, new()
            {
                { "@clienteId", clienteId },
                { "@monto",     dto.Monto },
                { "@medio",     dto.MedioPago },
                { "@ref",       dto.Referencia ?? "" },
                { "@usuario",   dto.Usuario ?? "" },
                { "@fecha",     DateTime.Now }
            });
        }

        public async Task CrearClienteAsync(GuardarClienteCtaCteDto dto)
        {
            var activoVal = _db.UsaPostgres ? "TRUE" : "1";
            var sql = $"INSERT INTO ctacte (nombre, telefono, email, dni, activo) VALUES (@nombre, @tel, @email, @dni, {activoVal})";
            await _db.ExecuteAsync(sql, new()
            {
                { "@nombre", dto.Nombre },
                { "@tel",    dto.Telefono },
                { "@email",  dto.Email },
                { "@dni",    dto.Dni }
            });
        }

        public async Task ActualizarClienteAsync(int id, GuardarClienteCtaCteDto dto)
        {
            var sql = "UPDATE ctacte SET nombre=@nombre, telefono=@tel, email=@email, dni=@dni WHERE id=@id";
            await _db.ExecuteAsync(sql, new()
            {
                { "@nombre", dto.Nombre },
                { "@tel",    dto.Telefono },
                { "@email",  dto.Email },
                { "@dni",    dto.Dni },
                { "@id",     id }
            });
        }

        private static string GetStr(List<JsonElement> r, int i) =>
            r.Count > i && r[i].ValueKind != JsonValueKind.Null ? (r[i].ValueKind == JsonValueKind.String ? r[i].GetString() ?? "" : r[i].ToString()) : "";
        private static int GetInt(List<JsonElement> r, int i) =>
            r.Count > i && r[i].ValueKind == JsonValueKind.Number ? r[i].GetInt32() : 0;
        private static decimal GetDecimal(List<JsonElement> r, int i) =>
            r.Count > i && r[i].ValueKind == JsonValueKind.Number ? r[i].GetDecimal() : 0m;
        private static DateTime GetDateTime(List<JsonElement> r, int i)
        {
            if (r.Count <= i || r[i].ValueKind == JsonValueKind.Null) return DateTime.MinValue;
            if (r[i].ValueKind == JsonValueKind.String && DateTime.TryParse(r[i].GetString(), out var d)) return d;
            return DateTime.MinValue;
        }
        private static DateTime? GetDateTimeNullable(List<JsonElement> r, int i)
        {
            if (r.Count <= i || r[i].ValueKind == JsonValueKind.Null) return null;
            if (r[i].ValueKind == JsonValueKind.String && DateTime.TryParse(r[i].GetString(), out var d)) return d;
            return null;
        }
        private static bool GetBool(List<JsonElement> r, int i)
        {
            if (r.Count <= i) return true;
            var el = r[i];
            if (el.ValueKind == JsonValueKind.True) return true;
            if (el.ValueKind == JsonValueKind.False) return false;
            if (el.ValueKind == JsonValueKind.Number) return el.GetInt32() != 0;
            if (el.ValueKind == JsonValueKind.String) return el.GetString() == "1" || el.GetString()?.ToLower() == "true";
            return true;
        }
    }
}
