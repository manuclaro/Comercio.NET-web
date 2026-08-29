using Microsoft.AspNetCore.Mvc;
using Comercio.NET.Mobile.Server.Controllers;
using Comercio.NET.Mobile.Server.Models;
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

        /// <summary>
        /// Actualiza costo, precio y stock de un producto.
        /// </summary>
        [HttpPut("{codigo}")]
        public async Task<IActionResult> Actualizar(string codigo, [FromBody] ActualizarProductoDto datos)
        {
            if (!ValidarAutorizacion())
                return Unauthorized(new { error = "No autorizado" });

            if (string.IsNullOrWhiteSpace(codigo))
                return BadRequest(new { error = "El código del producto es requerido." });

            if (datos.Costo < 0 || datos.Precio < 0 || datos.Stock < 0)
                return BadRequest(new { error = "Los valores no pueden ser negativos." });

            try
            {
                await _productosService.ActualizarProductoAsync(codigo, datos);
                return Ok(new { ok = true, mensaje = "Producto actualizado correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando producto '{Codigo}'", codigo);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("admin")]
        public async Task<IActionResult> ListarAdmin([FromQuery] string? buscar, [FromQuery] int pagina = 1, [FromQuery] int tamano = 50)
        {
            if (!ValidarAutorizacion()) return Unauthorized(new { error = "No autorizado" });
            try
            {
                var productos = await _productosService.ListarTodosAsync(buscar, pagina, tamano);
                var total     = await _productosService.ContarTodosAsync(buscar);
                return Ok(new { productos, total });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpGet("verificar/{codigo}")]
        public async Task<IActionResult> VerificarCodigo(string codigo)
        {
            if (!ValidarAutorizacion()) return Unauthorized(new { error = "No autorizado" });
            try
            {
                var existe = await _productosService.ExisteCodigoAsync(codigo);
                return Ok(new { existe });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpGet("{codigo}")]
        public async Task<IActionResult> Obtener(string codigo)
        {
            if (!ValidarAutorizacion()) return Unauthorized(new { error = "No autorizado" });
            try
            {
                var p = await _productosService.ObtenerAsync(codigo);
                if (p == null) return NotFound(new { error = "Producto no encontrado" });
                return Ok(p);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPut("{codigo}/completo")]
        public async Task<IActionResult> EditarCompleto(string codigo, [FromBody] EditarProductoCompletoDto datos)
        {
            if (!ValidarAutorizacion()) return Unauthorized(new { error = "No autorizado" });
            if (string.IsNullOrWhiteSpace(codigo)) return BadRequest(new { error = "Código requerido" });
            try
            {
                await _productosService.EditarCompletoAsync(codigo, datos);
                return Ok(new { ok = true, mensaje = "Producto actualizado correctamente." });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] NuevoProductoDto datos)
        {
            if (!ValidarAutorizacion()) return Unauthorized(new { error = "No autorizado" });
            if (string.IsNullOrWhiteSpace(datos.Codigo)) return BadRequest(new { error = "El código es requerido" });
            if (string.IsNullOrWhiteSpace(datos.Descripcion)) return BadRequest(new { error = "La descripción es requerida" });
            try
            {
                var codigo = await _productosService.CrearAsync(datos);
                return Ok(new { ok = true, codigo, mensaje = "Producto creado correctamente." });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpDelete("{codigo}")]
        public async Task<IActionResult> Eliminar(string codigo)
        {
            if (!ValidarAutorizacion()) return Unauthorized(new { error = "No autorizado" });
            try
            {
                await _productosService.EliminarAsync(codigo);
                return Ok(new { ok = true, mensaje = "Producto dado de baja correctamente." });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpGet("rubros")]
        public async Task<IActionResult> ListarRubros()
        {
            if (!ValidarAutorizacion()) return Unauthorized(new { error = "No autorizado" });
            try { return Ok(await _productosService.ListarRubrosAsync()); }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpGet("marcas")]
        public async Task<IActionResult> ListarMarcas()
        {
            if (!ValidarAutorizacion()) return Unauthorized(new { error = "No autorizado" });
            try { return Ok(await _productosService.ListarMarcasAsync()); }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }
    }
}