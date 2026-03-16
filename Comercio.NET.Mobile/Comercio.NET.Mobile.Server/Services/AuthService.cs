using Comercio.NET.Mobile.Server.Models;
using System.Text;

namespace Comercio.NET.Mobile.Server.Services
{
    public class AuthService
    {
        private readonly ILogger<AuthService> _logger;

        private readonly Dictionary<string, (string Password, string NombreCompleto, string Rol)> _usuariosHardcoded = new()
        {
            { "admin",    ("2201",     "Administrador del Sistema", "Admin")    },
            { "pizzeria", ("pizzeria", "Pizzería",                  "Pizzeria") },
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

                await Task.Delay(500);

                if (_usuariosHardcoded.TryGetValue(usuario.ToLower(), out var datosUsuario))
                {
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

        private int GetUserId(string usuario)
        {
            return usuario.ToLower() switch
            {
                "admin"    => 1,
                "pizzeria" => 2,
                "cajero"   => 3,
                "demo"     => 4,
                _          => 999
            };
        }
    }
}