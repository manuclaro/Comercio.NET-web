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
            [FromQuery] int? numeroCajero,
            [FromQuery] string? formaPago,
            [FromQuery] string? tipoFactura)
        {
            try
            {
                var turno = await _turnoService.GetTurnoActivoAsync();

                // Si hay turno abierto, mostrar desde su apertura hasta ahora.
                // Si no hay turno, mostrar las ventas del día actual completo.
                var desde = turno?.FechaApertura ?? DateTime.Today;
                var hasta = DateTime.Now;

                var ventas = await _ventasService.GetVentasDelDiaAsync(
                    desde, hasta, numeroCajero, formaPago, tipoFactura);
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
            [FromQuery] int? numeroCajero,
            [FromQuery] string? formaPago,
            [FromQuery] string? tipoFactura)
        {
            try
            {
                var turno = await _turnoService.GetTurnoActivoAsync();

                // Mismo criterio: turno activo o día de hoy si no hay turno.
                var desde = turno?.FechaApertura ?? DateTime.Today;
                var hasta = DateTime.Now;

                var resumen = await _ventasService.GetResumenAsync(
                    desde, hasta, numeroCajero, formaPago, tipoFactura);
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