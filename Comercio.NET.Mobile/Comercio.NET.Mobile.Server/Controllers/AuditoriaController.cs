using Comercio.NET.Mobile.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditoriaController : ControllerBase
    {
        private readonly IAuditoriaService _auditoriaService;

        public AuditoriaController(IAuditoriaService auditoriaService)
        {
            _auditoriaService = auditoriaService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditoria(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] string usuario,
            [FromQuery] int? numeroCajero)
        {
            var fechaDesde = desde ?? DateTime.Today;
            var fechaHasta = hasta ?? DateTime.Today;
            var registros = await _auditoriaService.GetAuditoriaAsync(fechaDesde, fechaHasta, usuario, numeroCajero);
            return Ok(registros);
        }
    }
}