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
                if (turno is null)
                    return Ok(Array.Empty<object>());

                var ventas = await _ventasService.GetVentasPorTurnoAsync(
                    turno.FechaApertura, numeroCajero, formaPago, tipoFactura);
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
                if (turno is null)
                    return Ok(new { });

                var resumen = await _ventasService.GetResumenPorTurnoAsync(
                    turno.FechaApertura, numeroCajero, formaPago, tipoFactura);
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