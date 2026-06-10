using Microsoft.AspNetCore.Mvc;
using Comercio.NET.Mobile.Server.Models;
using Comercio.NET.Mobile.Server.Services;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProveedoresController : ControllerBase
    {
        private readonly ProveedoresService _service;
        private readonly AuthService _authService;
        private readonly ILogger<ProveedoresController> _logger;

        public ProveedoresController(ProveedoresService service, AuthService authService, ILogger<ProveedoresController> logger)
        {
            _service     = service;
            _authService = authService;
            _logger      = logger;
        }

        private bool Autorizado() =>
            _authService.ValidarToken(Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", ""));

        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] string? buscar)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            try { return Ok(await _service.ListarAsync(buscar)); }
            catch (Exception ex) { _logger.LogError(ex, "Error listando proveedores"); return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Obtener(int id)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            try
            {
                var p = await _service.ObtenerAsync(id);
                if (p == null) return NotFound(new { error = "Proveedor no encontrado" });
                return Ok(p);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] GuardarProveedorDto dto)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            if (string.IsNullOrWhiteSpace(dto.Nombre)) return BadRequest(new { error = "El nombre es requerido" });
            try
            {
                await _service.CrearAsync(dto);
                return Ok(new { ok = true, mensaje = "Proveedor creado correctamente." });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarProveedorDto dto)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            if (string.IsNullOrWhiteSpace(dto.Nombre)) return BadRequest(new { error = "El nombre es requerido" });
            try
            {
                await _service.ActualizarAsync(id, dto);
                return Ok(new { ok = true, mensaje = "Proveedor actualizado correctamente." });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            try
            {
                await _service.EliminarAsync(id);
                return Ok(new { ok = true, mensaje = "Proveedor dado de baja correctamente." });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }
    }
}
