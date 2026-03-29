using Comercio.NET.Mobile.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VentasController : ControllerBase
    {
        private readonly IVentasService _ventasService;
        private readonly ITurnoService  _turnoService;
        private readonly ILogger<VentasController> _logger;

        public VentasController(IVentasService ventasService, ITurnoService turnoService, ILogger<VentasController> logger)
        {
            _ventasService = ventasService;
            _turnoService  = turnoService;
            _logger        = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetVentas(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] int? numeroCajero,
            [FromQuery] string? formaPago,
            [FromQuery] string? tipoFactura)
        {
            try
            {
                var fechaDesde = desde?.Date ?? DateTime.Today;
                var fechaHasta = hasta?.Date ?? DateTime.Today;

                var ventas = await _ventasService.GetVentasDelDiaAsync(
                    fechaDesde, fechaHasta, numeroCajero, formaPago, tipoFactura);
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
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] int? numeroCajero,
            [FromQuery] string? formaPago,
            [FromQuery] string? tipoFactura)
        {
            try
            {
                var fechaDesde = desde?.Date ?? DateTime.Today;
                var fechaHasta = hasta?.Date ?? DateTime.Today;

                var resumen = await _ventasService.GetResumenAsync(
                    fechaDesde, fechaHasta, numeroCajero, formaPago, tipoFactura);
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