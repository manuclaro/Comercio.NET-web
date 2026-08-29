using Microsoft.AspNetCore.Mvc;
using Comercio.NET.Mobile.Server.Models;
using Comercio.NET.Mobile.Server.Services;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly UsuariosAdminService _service;
        private readonly AuthService _authService;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(UsuariosAdminService service, AuthService authService, ILogger<UsuariosController> logger)
        {
            _service     = service;
            _authService = authService;
            _logger      = logger;
        }

        private bool Autorizado() =>
            _authService.ValidarToken(Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", ""));

        private string? ObtenerRolToken()
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token)) return null;
            try
            {
                var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
                return null; // El token simple no contiene el rol, lo validamos por nivel
            }
            catch { return null; }
        }

        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] string? buscar)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            try { return Ok(await _service.ListarAsync(buscar)); }
            catch (Exception ex) { _logger.LogError(ex, "Error listando usuarios"); return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Obtener(int id)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            try
            {
                var u = await _service.ObtenerAsync(id);
                if (u == null) return NotFound(new { error = "Usuario no encontrado" });
                return Ok(u);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] GuardarUsuarioDto dto)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            if (string.IsNullOrWhiteSpace(dto.NombreUsuario)) return BadRequest(new { error = "El nombre de usuario es requerido" });
            if (string.IsNullOrWhiteSpace(dto.Password)) return BadRequest(new { error = "La contraseña es requerida para usuarios nuevos" });
            try
            {
                await _service.CrearAsync(dto);
                return Ok(new { ok = true, mensaje = "Usuario creado correctamente." });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarUsuarioDto dto)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            if (string.IsNullOrWhiteSpace(dto.NombreUsuario)) return BadRequest(new { error = "El nombre de usuario es requerido" });
            try
            {
                await _service.ActualizarAsync(id, dto);
                return Ok(new { ok = true, mensaje = "Usuario actualizado correctamente." });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPost("{id:int}/password")]
        public async Task<IActionResult> CambiarPassword(int id, [FromBody] CambiarPasswordDto dto)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            if (string.IsNullOrWhiteSpace(dto.NuevaPassword)) return BadRequest(new { error = "La nueva contraseña no puede estar vacía" });
            try
            {
                await _service.CambiarPasswordAsync(id, dto.NuevaPassword);
                return Ok(new { ok = true, mensaje = "Contraseña actualizada correctamente." });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPost("{id:int}/toggle")]
        public async Task<IActionResult> ToggleActivo(int id, [FromQuery] bool activo)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            try
            {
                await _service.ToggleActivoAsync(id, activo);
                return Ok(new { ok = true, mensaje = activo ? "Usuario activado." : "Usuario desactivado." });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }
    }
}
