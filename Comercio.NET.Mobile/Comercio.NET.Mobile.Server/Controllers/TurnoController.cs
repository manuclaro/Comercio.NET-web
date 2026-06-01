using Comercio.NET.Mobile.Server.Services;
using Comercio.NET.Mobile.Server.Models;
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
        public async Task<IActionResult> Abrir([FromBody] AbrirTurnoRequest? request)
        {
            try
            {
                var monto = request?.MontoInicial ?? 0;
                var cajero = request?.NumeroCajero ?? 1;
                var usuario = request?.Usuario ?? "";
                var turno = await _turnoService.AbrirTurnoAsync(monto, cajero, usuario);
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

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen([FromQuery] int? id = null)
        {
            try
            {
                var resumen = await _turnoService.GetResumenTurnoAsync(id);
                if (resumen == null) return Ok(new { error = "No hay turno abierto" });
                return Ok(resumen);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetResumenTurno");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("abiertos")]
        public async Task<IActionResult> GetAbiertos()
        {
            try
            {
                var turnos = await _turnoService.GetTurnosAbiertosAsync();
                return Ok(turnos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetTurnosAbiertos");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("del-dia")]
        public async Task<IActionResult> GetDelDia([FromQuery] string? fecha = null)
        {
            try
            {
                var dia = string.IsNullOrEmpty(fecha) ? DateTime.Today : DateTime.Parse(fecha);
                var turnos = await _turnoService.GetTurnosDelDiaAsync(dia);
                return Ok(turnos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetTurnosDelDia");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("cerrar/{id}")]
        public async Task<IActionResult> CerrarPorId(int id, [FromBody] CerrarTurnoRequest? request)
        {
            try
            {
                var turno = await _turnoService.CerrarTurnoPorIdAsync(id, request?.Declaraciones, request?.CantidadVentas ?? 0);
                return Ok(turno);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CerrarTurnoPorId");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("historial-cierres")]
        public async Task<IActionResult> GetHistorialCierres([FromQuery] string desde, [FromQuery] string hasta, [FromQuery] string? cajero = null)
        {
            try
            {
                var fechaDesde = DateTime.Parse(desde);
                var fechaHasta = DateTime.Parse(hasta).AddDays(1).AddSeconds(-1);
                var historial = await _turnoService.GetHistorialCierresAsync(fechaDesde, fechaHasta, cajero);
                return Ok(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetHistorialCierres");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("cajeros")]
        public async Task<IActionResult> GetCajeros()
        {
            try
            {
                var cajeros = await _turnoService.GetCajerosAsync();
                return Ok(cajeros);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetCajeros");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}