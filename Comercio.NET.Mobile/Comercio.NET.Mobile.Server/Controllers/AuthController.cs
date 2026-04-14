using Microsoft.AspNetCore.Mvc;
using Comercio.NET.Mobile.Server.Models;
using Comercio.NET.Mobile.Server.Services;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Clave))
                {
                    return BadRequest(new LoginResponse
                    {
                        Exito = false,
                        Mensaje = "Usuario y contraseña son requeridos"
                    });
                }

                var resultado = await _authService.ValidarUsuarioAsync(request.Usuario, request.Clave);

                if (resultado.Exito)
                {
                    _logger.LogInformation("Login exitoso para usuario: {Usuario}", request.Usuario);
                    return Ok(resultado);
                }
                else
                {
                    _logger.LogWarning("Intento de login fallido para usuario: {Usuario}", request.Usuario);
                    return Unauthorized(resultado);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login");
                return StatusCode(500, new LoginResponse
                {
                    Exito = false,
                    Mensaje = "Error interno del servidor"
                });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // En una implementación real, invalidarías el token aquí
            return Ok(new { mensaje = "Sesión cerrada" });
        }

        [HttpGet("validar")]
        public IActionResult ValidarToken([FromHeader(Name = "Authorization")] string? authorization)
        {
            var token = authorization?.Replace("Bearer ", "");
            var esValido = _authService.ValidarToken(token);

            return Ok(new { valido = esValido });
        }
    }
}