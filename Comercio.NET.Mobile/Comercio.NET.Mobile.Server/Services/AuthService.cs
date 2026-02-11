using Comercio.NET.Mobile.Server.Models;
using System.Security.Cryptography;
using System.Text;

namespace Comercio.NET.Mobile.Server.Services
{
    public class AuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResponse> ValidarUsuarioAsync(string usuario, string clave)
        {
            try
            {
                var sqlBridgeUrl = _configuration["SQL_BRIDGE_URL"] 
                    ?? Environment.GetEnvironmentVariable("SQL_BRIDGE_URL");

                if (string.IsNullOrEmpty(sqlBridgeUrl))
                {
                    return new LoginResponse
                    {
                        Exito = false,
                        Mensaje = "Servicio de autenticación no disponible"
                    };
                }

                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(sqlBridgeUrl);

                // Consultar usuario en la base de datos
                var query = $"SELECT Id, Usuario, Clave, Nombre, Rol FROM Usuarios WHERE Usuario = '{usuario.Replace("'", "''")}'";
                var response = await client.GetAsync($"/api/query?sql={Uri.EscapeDataString(query)}");

                if (!response.IsSuccessStatusCode)
                {
                    return new LoginResponse
                    {
                        Exito = false,
                        Mensaje = "Error al validar credenciales"
                    };
                }

                var data = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

                if (data == null || data.Count == 0)
                {
                    return new LoginResponse
                    {
                        Exito = false,
                        Mensaje = "Usuario o contraseña incorrectos"
                    };
                }

                var usuarioData = data[0];
                var claveAlmacenada = usuarioData["Clave"]?.ToString() ?? string.Empty;

                // Validar contraseña (puedes usar hash si lo prefieres)
                if (!ValidarClave(clave, claveAlmacenada))
                {
                    return new LoginResponse
                    {
                        Exito = false,
                        Mensaje = "Usuario o contraseña incorrectos"
                    };
                }

                // Crear token simple (en producción usa JWT)
                var token = GenerarToken(usuario);

                return new LoginResponse
                {
                    Exito = true,
                    Mensaje = "Login exitoso",
                    Token = token,
                    Usuario = new Usuario
                    {
                        Id = Convert.ToInt32(usuarioData["Id"]),
                        NombreUsuario = usuarioData["Usuario"]?.ToString() ?? string.Empty,
                        NombreCompleto = usuarioData["Nombre"]?.ToString(),
                        Rol = usuarioData["Rol"]?.ToString()
                    }
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

        private bool ValidarClave(string claveIngresada, string claveAlmacenada)
        {
            // Si la clave almacenada está hasheada, usar hash
            // Por ahora comparación directa
            return claveIngresada == claveAlmacenada;
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
    }
}