using Comercio.NET.Mobile.Server.Models;
using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class ProveedoresService
    {
        private readonly DbService _db;
        private readonly ILogger<ProveedoresService> _logger;

        public ProveedoresService(DbService db, ILogger<ProveedoresService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // La tabla real usa "domicilio", no "direccion"
        private const string COLS = "id, nombre, COALESCE(cuit,'') AS cuit, COALESCE(telefono,'') AS telefono, " +
                                    "COALESCE(email,'') AS email, COALESCE(domicilio,'') AS domicilio, " +
                                    "COALESCE(contacto,'') AS contacto, activo";

        public async Task<IEnumerable<ProveedorDto>> ListarAsync(string? buscar = null)
        {
            var parms = new Dictionary<string, object?>();
            var likeOp = _db.UsaPostgres ? "ILIKE" : "LIKE";
            string filtroActivo = _db.UsaPostgres ? "COALESCE(activo, FALSE) IS TRUE" : "COALESCE(activo, 0) = 1";
            string sql;

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                parms["@b"] = $"%{buscar.Trim()}%";
                sql = $"SELECT {COLS} FROM proveedores WHERE {filtroActivo} AND (nombre {likeOp} @b OR cuit {likeOp} @b) ORDER BY nombre";
            }
            else
            {
                sql = $"SELECT {COLS} FROM proveedores WHERE {filtroActivo} ORDER BY nombre";
            }

            var rows = await _db.QueryAsync(sql, parms);
            return rows.Select(MapRow);
        }

        public async Task<ProveedorDto?> ObtenerAsync(int id)
        {
            var sql = $"SELECT {COLS} FROM proveedores WHERE id = @id";
            var rows = await _db.QueryAsync(sql, new() { { "@id", id } });
            return rows.Count == 0 ? null : MapRow(rows[0]);
        }

        public async Task CrearAsync(GuardarProveedorDto dto)
        {
            var activoVal = _db.UsaPostgres ? "TRUE" : "1";
            var sql = $"INSERT INTO proveedores (nombre, cuit, telefono, domicilio, email, contacto, activo) " +
                      $"VALUES (@nombre, @cuit, @telefono, @domicilio, @email, @contacto, {activoVal})";
            await _db.ExecuteAsync(sql, new()
            {
                { "@nombre",    dto.Nombre },
                { "@cuit",      dto.Cuit },
                { "@telefono",  dto.Telefono },
                { "@domicilio", dto.Direccion },
                { "@email",     dto.Email },
                { "@contacto",  dto.Contacto }
            });
        }

        public async Task ActualizarAsync(int id, GuardarProveedorDto dto)
        {
            var sql = "UPDATE proveedores SET nombre=@nombre, cuit=@cuit, telefono=@telefono, " +
                      "domicilio=@domicilio, email=@email, contacto=@contacto WHERE id=@id";
            await _db.ExecuteAsync(sql, new()
            {
                { "@nombre",    dto.Nombre },
                { "@cuit",      dto.Cuit },
                { "@telefono",  dto.Telefono },
                { "@domicilio", dto.Direccion },
                { "@email",     dto.Email },
                { "@contacto",  dto.Contacto },
                { "@id",        id }
            });
        }

        public async Task EliminarAsync(int id)
        {
            var inactivo = _db.UsaPostgres ? "FALSE" : "0";
            var sql = $"UPDATE proveedores SET activo = {inactivo} WHERE id = @id";
            await _db.ExecuteAsync(sql, new() { { "@id", id } });
        }

        private static ProveedorDto MapRow(List<JsonElement> r) => new()
        {
            Id        = GetInt(r, 0),
            Nombre    = GetStr(r, 1),
            Cuit      = GetStr(r, 2),
            Telefono  = GetStr(r, 3),
            Email     = GetStr(r, 4),
            Direccion = GetStr(r, 5),
            Contacto  = GetStr(r, 6),
            Activo    = GetBool(r, 7)
        };

        private static string GetStr(List<JsonElement> r, int i) =>
            r.Count > i && r[i].ValueKind != JsonValueKind.Null
                ? (r[i].ValueKind == JsonValueKind.String ? r[i].GetString() ?? "" : r[i].ToString())
                : "";
        private static int GetInt(List<JsonElement> r, int i) =>
            r.Count > i && r[i].ValueKind == JsonValueKind.Number ? r[i].GetInt32() : 0;
        private static bool GetBool(List<JsonElement> r, int i)
        {
            if (r.Count <= i) return false;
            var el = r[i];
            if (el.ValueKind == JsonValueKind.True)  return true;
            if (el.ValueKind == JsonValueKind.False) return false;
            if (el.ValueKind == JsonValueKind.Number) return el.GetInt32() != 0;
            if (el.ValueKind == JsonValueKind.String)
                return el.GetString() == "1" || el.GetString()?.ToLower() is "true" or "t";
            return false;
        }
    }
}
