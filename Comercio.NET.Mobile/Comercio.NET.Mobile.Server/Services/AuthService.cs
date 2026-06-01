using Comercio.NET.Mobile.Server.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class AuthService
    {
        private readonly ILogger<AuthService> _logger;
        private readonly DbService _db;

        // Salt idéntico al de la app de escritorio (AuthenticationService.cs)
        private const string SALT = "ComercioNET_Salt";

        public AuthService(ILogger<AuthService> logger, DbService db)
        {
            _logger = logger;
            _db     = db;
        }

        public async Task<LoginResponse> ValidarUsuarioAsync(string usuario, string clave)
        {
            try
            {
                _logger.LogInformation("Intento de login para usuario: {Usuario}", usuario);

                var hash = ComputeHash(clave);

                var sql = @"
                    SELECT idusuarios, nombreusuario, nombre, apellido,
                           nivel, numerocajero, activo
                    FROM usuarios
                    WHERE LOWER(nombreusuario) = LOWER(@usuario)
                      AND passwordhash = @hash
                      AND activo = true";

                // SQL Server usa activo = 1; Postgres usa activo = 1::bit (columna tipo bit)
                var sqlAdaptado = _db.UsaPostgres
                    ? sql.Replace("AND activo = true", "AND activo = 1::bit")
                    : sql.Replace("AND activo = true", "AND activo = 1");

                var rows = await _db.QueryAsync(sqlAdaptado, new()
                {
                    { "@usuario", usuario },
                    { "@hash",    hash }
                });

                if (rows.Count == 0)
                {
                    _logger.LogWarning("Login fallido para usuario: {Usuario}", usuario);
                    return new LoginResponse { Exito = false, Mensaje = "Usuario o contraseña incorrectos" };
                }

                var r = rows[0];
                string nombreCompleto = $"{GetStr(r, 2)} {GetStr(r, 3)}".Trim();
                int    nivel          = GetInt(r, 4);
                int    numeroCajero   = GetInt(r, 5);

                string rol = nivel switch
                {
                    4 => "Admin",
                    3 => "Supervisor",
                    _ => "Cajero"
                };

                var token = GenerarToken(usuario);

                _logger.LogInformation("Login exitoso: {Usuario} ({Rol})", usuario, rol);

                return new LoginResponse
                {
                    Exito = true,
                    Mensaje = "Login exitoso",
                    Token = token,
                    Usuario = new Usuario
                    {
                        Id             = GetInt(r, 0),
                        NombreUsuario  = GetStr(r, 1),
                        NombreCompleto = nombreCompleto,
                        Rol            = rol,
                        NumeroCajero   = numeroCajero
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar usuario {Usuario}", usuario);
                return new LoginResponse { Exito = false, Mensaje = $"Error al autenticar: {ex.Message}" };
            }
        }

        private static string ComputeHash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input + SALT));
            return Convert.ToBase64String(bytes);
        }

        private static string GetStr(List<System.Text.Json.JsonElement> row, int idx)
        {
            if (idx >= row.Count || row[idx].ValueKind == JsonValueKind.Null) return "";
            return row[idx].GetString() ?? "";
        }

        private static int GetInt(List<System.Text.Json.JsonElement> row, int idx)
        {
            if (idx >= row.Count || row[idx].ValueKind == JsonValueKind.Null) return 0;
            return row[idx].ValueKind == JsonValueKind.Number ? row[idx].GetInt32() : 0;
        }

        private string GenerarToken(string usuario)
        {
            var data = $"{usuario}:{DateTime.UtcNow.Ticks}:{Guid.NewGuid()}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        }

        public bool ValidarToken(string? token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var parts   = decoded.Split(':');
                if (parts.Length != 3) return false;
                var edad = DateTime.UtcNow - new DateTime(long.Parse(parts[1]));
                return edad.TotalHours < 24;
            }
            catch { return false; }
        }

        private int GetUserId(string usuario) => Math.Abs(usuario.GetHashCode()) % 10000;
    }
}
