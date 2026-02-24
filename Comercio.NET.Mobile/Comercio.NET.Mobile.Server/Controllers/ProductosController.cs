using Microsoft.AspNetCore.Mvc;
using Comercio.NET.Mobile.Server.Controllers;
using Comercio.NET.Mobile.Server.Services;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly IProductosService _productosService;
        private readonly AuthService _authService;
        private readonly ILogger<ProductosController> _logger;

        public ProductosController(
            IProductosService productosService,
            AuthService authService,
            ILogger<ProductosController> logger)
        {
            _productosService = productosService;
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
        /// Busca productos por código, descripción, rubro o marca.
        /// </summary>
        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar([FromQuery] string termino)
        {
            if (!ValidarAutorizacion())
                return Unauthorized(new { error = "No autorizado" });

            if (string.IsNullOrWhiteSpace(termino))
                return BadRequest(new { error = "El término de búsqueda no puede estar vacío." });

            try
            {
                var productos = await _productosService.BuscarProductosAsync(termino);
                return Ok(productos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando productos con término '{Termino}'", termino);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}