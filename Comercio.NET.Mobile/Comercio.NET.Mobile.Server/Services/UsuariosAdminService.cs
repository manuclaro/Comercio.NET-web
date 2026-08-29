using Comercio.NET.Mobile.Server.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class UsuariosAdminService
    {
        private readonly DbService _db;
        private readonly ILogger<UsuariosAdminService> _logger;
        private const string SALT = "ComercioNET_Salt";

        public UsuariosAdminService(DbService db, ILogger<UsuariosAdminService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<UsuarioAdminDto>> ListarAsync(string? buscar = null)
        {
            var likeOp = _db.UsaPostgres ? "ILIKE" : "LIKE";
            var parms = new Dictionary<string, object?>();
            string whereExtra = "";
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                parms["@b"] = $"%{buscar.Trim()}%";
                whereExtra = $" AND (nombreusuario {likeOp} @b OR nombre {likeOp} @b OR apellido {likeOp} @b)";
            }
            var sql = $"SELECT idusuarios, nombreusuario, nombre, apellido, nivel, numerocajero, activo FROM usuarios WHERE 1=1{whereExtra} ORDER BY nombreusuario";
            var rows = await _db.QueryAsync(sql, parms);
            return rows.Select(r => MapRow(r));
        }

        public async Task<UsuarioAdminDto?> ObtenerAsync(int id)
        {
            var sql = "SELECT idusuarios, nombreusuario, nombre, apellido, nivel, numerocajero, activo FROM usuarios WHERE idusuarios = @id";
            var rows = await _db.QueryAsync(sql, new() { { "@id", id } });
            return rows.Count == 0 ? null : MapRow(rows[0]);
        }

        public async Task CrearAsync(GuardarUsuarioDto dto)
        {
            var hash = string.IsNullOrWhiteSpace(dto.Password) ? "" : ComputeHash(dto.Password);
            var activoVal = _db.UsaPostgres ? "TRUE" : "1";
            var sql = $@"INSERT INTO usuarios (nombreusuario, nombre, apellido, nivel, numerocajero, passwordhash, activo)
                         VALUES (@usuario, @nombre, @apellido, @nivel, @cajero, @hash, {activoVal})";
            await _db.ExecuteAsync(sql, new()
            {
                { "@usuario",   dto.NombreUsuario },
                { "@nombre",    dto.Nombre },
                { "@apellido",  dto.Apellido },
                { "@nivel",     dto.Nivel },
                { "@cajero",    dto.NumeroCajero },
                { "@hash",      hash }
            });
        }

        public async Task ActualizarAsync(int id, GuardarUsuarioDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                var hash = ComputeHash(dto.Password);
                var sql = "UPDATE usuarios SET nombreusuario=@usuario, nombre=@nombre, apellido=@apellido, nivel=@nivel, numerocajero=@cajero, passwordhash=@hash WHERE idusuarios=@id";
                await _db.ExecuteAsync(sql, new()
                {
                    { "@usuario",  dto.NombreUsuario },
                    { "@nombre",   dto.Nombre },
                    { "@apellido", dto.Apellido },
                    { "@nivel",    dto.Nivel },
                    { "@cajero",   dto.NumeroCajero },
                    { "@hash",     hash },
                    { "@id",       id }
                });
            }
            else
            {
                var sql = "UPDATE usuarios SET nombreusuario=@usuario, nombre=@nombre, apellido=@apellido, nivel=@nivel, numerocajero=@cajero WHERE idusuarios=@id";
                await _db.ExecuteAsync(sql, new()
                {
                    { "@usuario",  dto.NombreUsuario },
                    { "@nombre",   dto.Nombre },
                    { "@apellido", dto.Apellido },
                    { "@nivel",    dto.Nivel },
                    { "@cajero",   dto.NumeroCajero },
                    { "@id",       id }
                });
            }
        }

        public async Task CambiarPasswordAsync(int id, string nuevaPassword)
        {
            var hash = ComputeHash(nuevaPassword);
            var sql = "UPDATE usuarios SET passwordhash=@hash WHERE idusuarios=@id";
            await _db.ExecuteAsync(sql, new() { { "@hash", hash }, { "@id", id } });
        }

        public async Task ToggleActivoAsync(int id, bool activo)
        {
            var val = _db.UsaPostgres ? (activo ? "TRUE" : "FALSE") : (activo ? "1" : "0");
            var sql = $"UPDATE usuarios SET activo = {val} WHERE idusuarios = @id";
            await _db.ExecuteAsync(sql, new() { { "@id", id } });
        }

        private static UsuarioAdminDto MapRow(List<JsonElement> r) => new()
        {
            Id            = GetInt(r, 0),
            NombreUsuario = GetStr(r, 1),
            Nombre        = GetStr(r, 2),
            Apellido      = GetStr(r, 3),
            Nivel         = GetInt(r, 4),
            NumeroCajero  = GetInt(r, 5),
            Activo        = GetBool(r, 6),
            Rol           = GetInt(r, 4) switch { 4 => "Admin", 3 => "Supervisor", _ => "Cajero" }
        };

        private static string ComputeHash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input + SALT));
            return Convert.ToBase64String(bytes);
        }

        private static string GetStr(List<JsonElement> r, int i) =>
            r.Count > i && r[i].ValueKind != JsonValueKind.Null ? (r[i].ValueKind == JsonValueKind.String ? r[i].GetString() ?? "" : r[i].ToString()) : "";
        private static int GetInt(List<JsonElement> r, int i) =>
            r.Count > i && r[i].ValueKind == JsonValueKind.Number ? r[i].GetInt32() : 0;
        private static bool GetBool(List<JsonElement> r, int i)
        {
            if (r.Count <= i) return false;
            var el = r[i];
            if (el.ValueKind == JsonValueKind.True) return true;
            if (el.ValueKind == JsonValueKind.False) return false;
            if (el.ValueKind == JsonValueKind.Number) return el.GetInt32() != 0;
            if (el.ValueKind == JsonValueKind.String) return el.GetString() == "1" || el.GetString()?.ToLower() == "true";
            return false;
        }
    }
}
