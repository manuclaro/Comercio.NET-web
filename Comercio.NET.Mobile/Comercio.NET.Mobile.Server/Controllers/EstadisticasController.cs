using Microsoft.AspNetCore.Mvc;
using Comercio.NET.Mobile.Server.Services;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EstadisticasController : ControllerBase
    {
        private readonly EstadisticasService _estadisticasService;
        private readonly AuthService _authService;
        private readonly ILogger<EstadisticasController> _logger;

        public EstadisticasController(
            EstadisticasService estadisticasService,
            AuthService authService,
            ILogger<EstadisticasController> logger)
        {
            _estadisticasService = estadisticasService;
            _authService = authService;
            _logger = logger;
        }

        private bool ValidarAutorizacion()
        {
            var authorization = Request.Headers["Authorization"].FirstOrDefault();
            var token = authorization?.Replace("Bearer ", "");
            return _authService.ValidarToken(token);
        }

        /// <summary>
        /// Devuelve el total de ventas agrupado por rubro en un rango de fechas.
        /// </summary>
        [HttpGet("ventas-por-rubro")]
        public async Task<IActionResult> VentasPorRubro(
            [FromQuery] string desde,
            [FromQuery] string hasta)
        {
            if (!ValidarAutorizacion())
                return Unauthorized(new { error = "No autorizado" });

            if (!DateTime.TryParse(desde, out var fechaDesde) ||
                !DateTime.TryParse(hasta, out var fechaHasta))
                return BadRequest(new { error = "Las fechas deben tener formato yyyy-MM-dd." });

            if (fechaDesde > fechaHasta)
                return BadRequest(new { error = "La fecha 'desde' no puede ser mayor que 'hasta'." });

            try
            {
                var datos = await _estadisticasService.ObtenerVentasPorRubroAsync(fechaDesde, fechaHasta);
                return Ok(datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo ventas por rubro");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}