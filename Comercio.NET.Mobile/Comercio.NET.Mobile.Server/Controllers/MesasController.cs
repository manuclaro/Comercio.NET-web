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

        // ── Mesas ─────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetMesasAbiertas()
        {
            try { return Ok(await _mesasService.GetMesasAbiertasAsync()); }
            catch (Exception ex) { _logger.LogError(ex, "Error en GetMesasAbiertas"); return StatusCode(500, new { error = ex.Message }); }
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
            catch (Exception ex) { _logger.LogError(ex, "Error en GetMesa {Id}", id); return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpGet("{id}/items")]
        public async Task<IActionResult> GetItems(int id)
        {
            try { return Ok(await _mesasService.GetItemsMesaAsync(id)); }
            catch (Exception ex) { _logger.LogError(ex, "Error en GetItems mesa {Id}", id); return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> AbrirMesa([FromBody] AbrirMesaRequest request)
        {
            try { return Ok(await _mesasService.AbrirMesaAsync(request)); }
            catch (Exception ex) { _logger.LogError(ex, "Error en AbrirMesa"); return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPost("{id}/items")]
        public async Task<IActionResult> AgregarItem(int id, [FromBody] AgregarItemRequest request)
        {
            try { await _mesasService.AgregarItemAsync(id, request); return Ok(); }
            catch (Exception ex) { _logger.LogError(ex, "Error en AgregarItem mesa {Id}", id); return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpDelete("items/{itemId}")]
        public async Task<IActionResult> EliminarItem(int itemId)
        {
            try { await _mesasService.EliminarItemAsync(itemId); return Ok(); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EliminarItem {ItemId}", itemId); return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPost("{id}/cerrar")]
        public async Task<IActionResult> CerrarMesa(int id, [FromBody] CerrarMesaRequest request)
        {
            try { return Ok(await _mesasService.CerrarMesaAsync(id, request)); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CerrarMesa {Id}", id); return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpGet("mozos")]
        public async Task<IActionResult> GetMozos()
        {
            try { return Ok(await _mesasService.GetMozosAsync()); }
            catch (Exception ex) { _logger.LogError(ex, "Error en GetMozos"); return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPost("mozos")]
        public async Task<IActionResult> CrearMozo([FromBody] MozoDto dto)
        {
            try { return Ok(await _mesasService.CrearMozoAsync(dto.Nombre)); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CrearMozo"); return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpDelete("mozos/{id}")]
        public async Task<IActionResult> EliminarMozo(int id)
        {
            try { await _mesasService.EliminarMozoAsync(id); return Ok(); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EliminarMozo {Id}", id); return StatusCode(500, new { error = ex.Message }); }
        }

        // ── Productos Bar ─────────────────────────────────────────────────────

        [HttpGet("productos-bar")]
        public async Task<IActionResult> GetProductosBar()
        {
            try { return Ok(await _mesasService.GetProductosBarAsync()); }
            catch (Exception ex) { _logger.LogError(ex, "Error en GetProductosBar"); return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPost("productos-bar")]
        public async Task<IActionResult> CrearProductoBar([FromBody] ProductoBarDto dto)
        {
            try { return Ok(await _mesasService.CrearProductoBarAsync(dto)); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CrearProductoBar"); return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpDelete("productos-bar/{id}")]
        public async Task<IActionResult> EliminarProductoBar(int id)
        {
            try { await _mesasService.EliminarProductoBarAsync(id); return Ok(); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EliminarProductoBar {Id}", id); return StatusCode(500, new { error = ex.Message }); }
        }

        // ── Ventas del Día ────────────────────────────────────────────────────

        [HttpGet("ventas-dia")]
        public async Task<IActionResult> GetVentasDelDia()
        {
            try { return Ok(await _mesasService.GetVentasDelDiaAsync()); }
            catch (Exception ex) { _logger.LogError(ex, "Error en GetVentasDelDia"); return StatusCode(500, new { error = ex.Message }); }
        }
    }
}