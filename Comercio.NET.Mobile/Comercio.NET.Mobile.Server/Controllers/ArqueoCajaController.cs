using Microsoft.AspNetCore.Mvc;
using Comercio.NET.Mobile.Server.Services;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArqueoCajaController : ControllerBase
    {
        private readonly ArqueoCajaService _service;

        public ArqueoCajaController(ArqueoCajaService service)
        {
            _service = service;
        }

        [HttpGet("cajeros")]
        public async Task<IActionResult> ObtenerCajeros()
        {
            try
            {
                var cajeros = await _service.ObtenerCajerosAsync();
                return Ok(cajeros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("hoy")]
        public async Task<IActionResult> ObtenerArqueoHoy([FromQuery] string? cajero = null)
        {
            try
            {
                var resultado = await _service.ObtenerArqueoAsync(DateTime.Today, cajero);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("fecha/{fecha}")]
        public async Task<IActionResult> ObtenerArqueoPorFecha(DateTime fecha, [FromQuery] string? cajero = null)
        {
            try
            {
                var resultado = await _service.ObtenerArqueoAsync(fecha, cajero);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}