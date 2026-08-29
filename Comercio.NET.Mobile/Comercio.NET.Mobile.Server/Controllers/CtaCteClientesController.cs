using Microsoft.AspNetCore.Mvc;
using Comercio.NET.Mobile.Server.Models;
using Comercio.NET.Mobile.Server.Services;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CtaCteClientesController : ControllerBase
    {
        private readonly CtaCteClienteService _service;
        private readonly AuthService _authService;
        private readonly ILogger<CtaCteClientesController> _logger;

        public CtaCteClientesController(CtaCteClienteService service, AuthService authService, ILogger<CtaCteClientesController> logger)
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
            try { return Ok(await _service.ListarClientesAsync(buscar)); }
            catch (Exception ex) { _logger.LogError(ex, "Error listando clientes ctacte"); return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Obtener(int id)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            try
            {
                var c = await _service.ObtenerClienteAsync(id);
                if (c == null) return NotFound(new { error = "Cliente no encontrado" });
                return Ok(c);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpGet("{id:int}/movimientos")]
        public async Task<IActionResult> ObtenerMovimientos(int id)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            try { return Ok(await _service.ObtenerMovimientosAsync(id)); }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPost("{id:int}/pagos")]
        public async Task<IActionResult> RegistrarPago(int id, [FromBody] RegistrarPagoCtaCteDto dto)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            if (dto.Monto <= 0) return BadRequest(new { error = "El monto debe ser mayor a cero" });
            try
            {
                await _service.RegistrarPagoAsync(id, dto);
                return Ok(new { ok = true, mensaje = "Pago registrado correctamente." });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] GuardarClienteCtaCteDto dto)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            if (string.IsNullOrWhiteSpace(dto.Nombre)) return BadRequest(new { error = "El nombre es requerido" });
            try
            {
                await _service.CrearClienteAsync(dto);
                return Ok(new { ok = true, mensaje = "Cliente creado correctamente." });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarClienteCtaCteDto dto)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            if (string.IsNullOrWhiteSpace(dto.Nombre)) return BadRequest(new { error = "El nombre es requerido" });
            try
            {
                await _service.ActualizarClienteAsync(id, dto);
                return Ok(new { ok = true, mensaje = "Cliente actualizado correctamente." });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }
    }
}
