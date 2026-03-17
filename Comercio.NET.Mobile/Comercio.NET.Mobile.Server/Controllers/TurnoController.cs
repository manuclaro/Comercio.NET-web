using Comercio.NET.Mobile.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TurnoController : ControllerBase
    {
        private readonly ITurnoService _turnoService;
        private readonly ILogger<TurnoController> _logger;

        public TurnoController(ITurnoService turnoService, ILogger<TurnoController> logger)
        {
            _turnoService = turnoService;
            _logger = logger;
        }

        [HttpGet("activo")]
        public async Task<IActionResult> GetActivo()
        {
            try
            {
                var turno = await _turnoService.GetTurnoActivoAsync();
                if (turno is null) return Ok(new { abierto = false });
                return Ok(new { abierto = true, turno });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetTurnoActivo");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("abrir")]
        public async Task<IActionResult> Abrir()
        {
            try
            {
                var turno = await _turnoService.AbrirTurnoAsync();
                return Ok(turno);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en AbrirTurno");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("cerrar")]
        public async Task<IActionResult> Cerrar()
        {
            try
            {
                var hayMesas = await _turnoService.HayMesasAbiertasAsync();
                if (hayMesas)
                    return BadRequest(new { error = "Hay mesas abiertas. Cerralas antes de cerrar el turno." });

                var turno = await _turnoService.CerrarTurnoAsync();
                return Ok(turno);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CerrarTurno");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}