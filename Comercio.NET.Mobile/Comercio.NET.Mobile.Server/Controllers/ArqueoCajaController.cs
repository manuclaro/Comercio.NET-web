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
        private readonly ILogger<ArqueoCajaController> _logger;

        public ArqueoCajaController(
            ArqueoCajaService service, 
            AuthService authService,
            ILogger<ArqueoCajaController> logger)
        {
            _service = service;
            _authService = authService;
            _logger = logger;
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
                _logger.LogError(ex, "Error obteniendo cajeros");
                return StatusCode(500, new { error = ex.Message, detalle = ex.StackTrace });
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
                _logger.LogError(ex, "Error obteniendo arqueo de hoy");
                return StatusCode(500, new { error = ex.Message, detalle = ex.StackTrace });
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
                _logger.LogError(ex, "Error obteniendo arqueo por fecha: {Fecha}", fecha);
                return StatusCode(500, new { error = ex.Message, detalle = ex.StackTrace });
            }
        }

        // ✅ ENDPOINT MEJORADO: Obtener detalle de pagos a proveedores
        [HttpGet("pagos-proveedores")]
        public async Task<IActionResult> ObtenerDetallePagosProveedores(
            [FromQuery] string fecha,
            [FromQuery] string? cajero = null)
        {
            _logger.LogInformation("📊 Solicitando pagos a proveedores - Fecha: {Fecha}, Cajero: {Cajero}", fecha, cajero ?? "Todos");

            if (!ValidarAutorizacion())
            {
                _logger.LogWarning("❌ Acceso no autorizado a pagos-proveedores");
                return Unauthorized(new { error = "No autorizado" });
            }

            try
            {
                // ✅ Parsear la fecha desde string
                if (!DateTime.TryParse(fecha, out DateTime fechaParsed))
                {
                    _logger.LogWarning("❌ Formato de fecha inválido: {Fecha}", fecha);
                    return BadRequest(new { error = "Formato de fecha inválido", fechaRecibida = fecha });
                }

                _logger.LogInformation("✅ Fecha parseada correctamente: {Fecha}", fechaParsed);

                var detalle = await _service.ObtenerDetallePagosProveedoresAsync(fechaParsed, cajero);
                
                _logger.LogInformation("✅ Se obtuvieron {Cantidad} pagos", detalle.Count);
                
                return Ok(detalle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error obteniendo detalle de pagos - Fecha: {Fecha}, Cajero: {Cajero}", fecha, cajero);
                return StatusCode(500, new 
                { 
                    error = ex.Message, 
                    detalle = ex.StackTrace,
                    innerException = ex.InnerException?.Message
                });
            }
        }
    }
}