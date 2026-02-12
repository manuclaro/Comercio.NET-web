using Microsoft.AspNetCore.Mvc;
using Comercio.NET.Mobile.Server.Services;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArqueoCajaController : ControllerBase
    {
        private readonly ArqueoCajaService _service;
        private readonly AuthService _authService;

        public ArqueoCajaController(ArqueoCajaService service, AuthService authService)
        {
            _service = service;
            _authService = authService;
        }

        private bool ValidarAutorizacion()
        {
            var authorization = Request.Headers["Authorization"].FirstOrDefault();
            var token = authorization?.Replace("Bearer ", "");
            return _authService.ValidarToken(token);
        }

        [HttpGet("cajeros")]
        public async Task<IActionResult> ObtenerCajeros()
        {
            if (!ValidarAutorizacion())
                return Unauthorized(new { error = "No autorizado" });

            try
            {
                var cajeros = await _service.ObtenerCajerosAsync();
                return Ok(cajeros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("hoy")]
        public async Task<IActionResult> ObtenerArqueoHoy([FromQuery] string? cajero = null)
        {
            if (!ValidarAutorizacion())
                return Unauthorized(new { error = "No autorizado" });

            try
            {
                var resultado = await _service.ObtenerArqueoAsync(DateTime.Today, cajero);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("fecha/{fecha}")]
        public async Task<IActionResult> ObtenerArqueoPorFecha(DateTime fecha, [FromQuery] string? cajero = null)
        {
            if (!ValidarAutorizacion())
                return Unauthorized(new { error = "No autorizado" });

            try
            {
                var resultado = await _service.ObtenerArqueoAsync(fecha, cajero);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ✅ NUEVO ENDPOINT: Obtener detalle de pagos a proveedores
        [HttpGet("pagos-proveedores")]
        public async Task<IActionResult> ObtenerDetallePagosProveedores(
            [FromQuery] DateTime fecha,
            [FromQuery] string? cajero = null)
        {
            if (!ValidarAutorizacion())
                return Unauthorized(new { error = "No autorizado" });

            try
            {
                var detalle = await _service.ObtenerDetallePagosProveedoresAsync(fecha, cajero);
                return Ok(detalle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}