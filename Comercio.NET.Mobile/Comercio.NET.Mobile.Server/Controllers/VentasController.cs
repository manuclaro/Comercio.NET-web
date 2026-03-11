using Comercio.NET.Mobile.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VentasController : ControllerBase
    {
        private readonly IVentasService _ventasService;
        private readonly ILogger<VentasController> _logger;

        public VentasController(IVentasService ventasService, ILogger<VentasController> logger)
        {
            _ventasService = ventasService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetVentas(
            [FromQuery] string? fecha,
            [FromQuery] int? numeroCajero,
            [FromQuery] string? formaPago,
            [FromQuery] string? tipoFactura)
        {
            var fechaConsulta = DateTime.TryParse(fecha, out var f) ? f : DateTime.Today;

            try
            {
                var ventas = await _ventasService.GetVentasDelDiaAsync(fechaConsulta, numeroCajero, formaPago, tipoFactura);
                return Ok(ventas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetVentas");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen(
            [FromQuery] string? fecha,
            [FromQuery] int? numeroCajero)
        {
            var fechaConsulta = DateTime.TryParse(fecha, out var f) ? f : DateTime.Today;

            try
            {
                var resumen = await _ventasService.GetResumenAsync(fechaConsulta, numeroCajero);
                return Ok(resumen);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetResumen");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}