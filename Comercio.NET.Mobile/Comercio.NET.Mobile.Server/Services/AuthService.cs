using Comercio.NET.Mobile.Server.Models;
using System.Text;

namespace Comercio.NET.Mobile.Server.Services
{
    public class AuthService
    {
        private readonly ILogger<AuthService> _logger;

        // ✅ CREDENCIALES HARDCODEADAS (temporal)
        private readonly Dictionary<string, (string Password, string NombreCompleto, string Rol)> _usuariosHardcoded = new()
        {
            { "admin", ("123", "Administrador del Sistema", "Admin") },
            //{ "cajero", ("cajero123", "Cajero Principal", "Cajero") },
            //{ "demo", ("demo", "Usuario Demo", "Consulta") }
        };

        public AuthService(ILogger<AuthService> logger)
        {
            _logger = logger;
        }

        public async Task<LoginResponse> ValidarUsuarioAsync(string usuario, string clave)
        {
            try
            {
                _logger.LogInformation("Intento de login para usuario: {Usuario}", usuario);

                // Simular delay de red (opcional, para que se vea más real)
                await Task.Delay(500);

                // Buscar usuario en el diccionario hardcoded
                if (_usuariosHardcoded.TryGetValue(usuario.ToLower(), out var datosUsuario))
                {
                    // Validar contraseña
                    if (datosUsuario.Password == clave)
                    {
                        var token = GenerarToken(usuario);

                        _logger.LogInformation("Login exitoso para usuario: {Usuario}", usuario);

                        return new LoginResponse
                        {
                            Exito = true,
                            Mensaje = "Login exitoso",
                            Token = token,
                            Usuario = new Usuario
                            {
                                Id = GetUserId(usuario),
                                NombreUsuario = usuario,
                                NombreCompleto = datosUsuario.NombreCompleto,
                                Rol = datosUsuario.Rol
                            }
                        };
                    }
                }

                _logger.LogWarning("Login fallido para usuario: {Usuario}", usuario);

                return new LoginResponse
                {
                    Exito = false,
                    Mensaje = "Usuario o contraseña incorrectos"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar usuario");
                return new LoginResponse
                {
                    Exito = false,
                    Mensaje = $"Error: {ex.Message}"
                };
            }
        }

        private string GenerarToken(string usuario)
        {
            // Token simple: Base64(usuario:timestamp:random)
            // En producción, usar JWT
            var data = $"{usuario}:{DateTime.UtcNow.Ticks}:{Guid.NewGuid()}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        }

        public bool ValidarToken(string? token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var parts = decoded.Split(':');

                if (parts.Length != 3)
                    return false;

                // Validar que el token no sea muy antiguo (ej: 24 horas)
                var timestamp = long.Parse(parts[1]);
                var tokenDate = new DateTime(timestamp);
                var edad = DateTime.UtcNow - tokenDate;

                return edad.TotalHours < 24;
            }
            catch
            {
                return false;
            }
        }

        // Helper para generar IDs únicos basados en el nombre de usuario
        private int GetUserId(string usuario)
        {
            return usuario.ToLower() switch
            {
                "admin" => 1,
                "cajero" => 2,
                "demo" => 3,
                _ => 999
            };
        }
    }
}