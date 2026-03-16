using Comercio.NET.Mobile.Server.Models;
using Comercio.NET.Mobile.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MesasController : ControllerBase
    {
        private readonly IMesasService _mesasService;
        private readonly ILogger<MesasController> _logger;

        public MesasController(IMesasService mesasService, ILogger<MesasController> logger)
        {
            _mesasService = mesasService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetMesasAbiertas()
        {
            try
            {
                var mesas = await _mesasService.GetMesasAbiertasAsync();
                return Ok(mesas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetMesasAbiertas");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMesa(int id)
        {
            try
            {
                var mesa = await _mesasService.GetMesaAsync(id);
                if (mesa is null) return NotFound();
                return Ok(mesa);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetMesa {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}/items")]
        public async Task<IActionResult> GetItems(int id)
        {
            try
            {
                var items = await _mesasService.GetItemsMesaAsync(id);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetItems mesa {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AbrirMesa([FromBody] AbrirMesaRequest request)
        {
            try
            {
                var mesa = await _mesasService.AbrirMesaAsync(request);
                return Ok(mesa);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en AbrirMesa");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{id}/items")]
        public async Task<IActionResult> AgregarItem(int id, [FromBody] AgregarItemRequest request)
        {
            try
            {
                await _mesasService.AgregarItemAsync(id, request);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en AgregarItem mesa {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("items/{itemId}")]
        public async Task<IActionResult> EliminarItem(int itemId)
        {
            try
            {
                await _mesasService.EliminarItemAsync(itemId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en EliminarItem {ItemId}", itemId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{id}/cerrar")]
        public async Task<IActionResult> CerrarMesa(int id, [FromBody] CerrarMesaRequest request)
        {
            try
            {
                var mesa = await _mesasService.CerrarMesaAsync(id, request);
                return Ok(mesa);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CerrarMesa {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}