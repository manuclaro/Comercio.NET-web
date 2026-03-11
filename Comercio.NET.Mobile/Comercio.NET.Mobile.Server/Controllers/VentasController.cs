using Comercio.NET.Mobile.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VentasController : ControllerBase
    {
        private readonly IVentasService _ventasService;

        public VentasController(IVentasService ventasService)
        {
            _ventasService = ventasService;
        }

        [HttpGet]
        public async Task<IActionResult> GetVentas(
            [FromQuery] DateTime? fecha,
            [FromQuery] int? numeroCajero,
            [FromQuery] string formaPago)
        {
            var fechaConsulta = fecha ?? DateTime.Today;
            var ventas = await _ventasService.GetVentasDelDiaAsync(fechaConsulta, numeroCajero, formaPago);
            return Ok(ventas);
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen(
            [FromQuery] DateTime? fecha,
            [FromQuery] int? numeroCajero)
        {
            var fechaConsulta = fecha ?? DateTime.Today;
            var resumen = await _ventasService.GetResumenAsync(fechaConsulta, numeroCajero);
            return Ok(resumen);
        }
    }
}