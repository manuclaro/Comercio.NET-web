using Comercio.NET.Mobile.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditoriaController : ControllerBase
    {
        private readonly IAuditoriaService _auditoriaService;
        private readonly ILogger<AuditoriaController> _logger;

        public AuditoriaController(IAuditoriaService auditoriaService, ILogger<AuditoriaController> logger)
        {
            _auditoriaService = auditoriaService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditoria(
            [FromQuery] string desde,
            [FromQuery] string hasta,
            [FromQuery] string usuario,
            [FromQuery] int? numeroCajero)
        {
            var fechaDesde = DateTime.TryParse(desde, out var d) ? d : DateTime.Today;
            var fechaHasta = DateTime.TryParse(hasta, out var h) ? h : DateTime.Today;

            try
            {
                var registros = await _auditoriaService.GetAuditoriaAsync(fechaDesde, fechaHasta, usuario, numeroCajero);
                return Ok(registros);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetAuditoria");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}