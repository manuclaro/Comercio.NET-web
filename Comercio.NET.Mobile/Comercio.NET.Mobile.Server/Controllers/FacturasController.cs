using Microsoft.AspNetCore.Mvc;
using Comercio.NET.Mobile.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacturasController : ControllerBase
    {
        private readonly FacturasService _service;
        private readonly AuthService _authService;
        private readonly AfipService _afipService;
        private readonly DbService _db;
        private readonly IConfiguration _config;
        private readonly ILogger<FacturasController> _logger;

        public FacturasController(FacturasService service, AuthService authService, AfipService afipService, DbService db, IConfiguration config, ILogger<FacturasController> logger)
        {
            _service = service;
            _authService = authService;
            _afipService = afipService;
            _db = db;
            _config = config;
            _logger = logger;
        }

        private bool ValidarAutorizacion()
        {
            var authorization = Request.Headers["Authorization"].FirstOrDefault();
            var token = authorization?.Replace("Bearer ", "");
            return _authService.ValidarToken(token);
        }

        [HttpGet]
        public async Task<IActionResult> GetFacturas(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] string? cajero,
            [FromQuery] string? formaPago,
            [FromQuery] string? tipoFactura)
        {
            if (!ValidarAutorizacion())
                return Unauthorized(new { error = "No autorizado" });

            try
            {
                var fechaDesde = desde?.Date ?? DateTime.Today;
                var fechaHasta = hasta?.Date ?? DateTime.Today;
                var facturas = await _service.ObtenerFacturasAsync(fechaDesde, fechaHasta, cajero, formaPago, tipoFactura);
                return Ok(facturas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetFacturas");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] string? cajero,
            [FromQuery] string? formaPago,
            [FromQuery] string? tipoFactura)
        {
            if (!ValidarAutorizacion())
                return Unauthorized(new { error = "No autorizado" });

            try
            {
                var fechaDesde = desde?.Date ?? DateTime.Today;
                var fechaHasta = hasta?.Date ?? DateTime.Today;
                var resumen = await _service.ObtenerResumenAsync(fechaDesde, fechaHasta, cajero, formaPago, tipoFactura);
                return Ok(resumen);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetResumen facturas");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>Obtiene los productos/items de una factura por su numero de remito.</summary>
        [HttpGet("{numeroRemito}/detalle")]
        public async Task<IActionResult> GetDetalle(int numeroRemito)
        {
            if (!ValidarAutorizacion())
                return Unauthorized(new { error = "No autorizado" });

            try
            {
                var sql = @"SELECT codigo, descripcion, precio, cantidad, total 
                            FROM ventas WHERE nrofactura = @nroRemito ORDER BY id";
                var rows = await _db.QueryAsync(sql, new Dictionary<string, object?> { { "@nroRemito", numeroRemito } });

                var items = rows.Select(r => new
                {
                    codigo = r[0].GetString(),
                    descripcion = r[1].GetString(),
                    precio = r[2].GetDecimal(),
                    cantidad = r[3].GetInt32(),
                    total = r[4].GetDecimal()
                }).ToList();

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo detalle factura {Remito}", numeroRemito);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>Modifica la forma de pago de una factura.</summary>
        [HttpPut("{numeroRemito}/forma-pago")]
        public async Task<IActionResult> CambiarFormaPago(int numeroRemito, [FromBody] CambiarFormaPagoRequest request)
        {
            if (!ValidarAutorizacion())
                return Unauthorized(new { error = "No autorizado" });

            try
            {
                await _db.ExecuteAsync(
                    "UPDATE facturas SET formadepago = @formaPago WHERE numeroremito = @nroRemito",
                    new Dictionary<string, object?> { { "@formaPago", request.FormaPago }, { "@nroRemito", numeroRemito } });

                // Actualizar tambien en detallespagofactura
                await _db.ExecuteAsync(
                    "UPDATE detallespagofactura SET mediopago = @formaPago WHERE numeroremito = @nroRemito",
                    new Dictionary<string, object?> { { "@formaPago", request.FormaPago }, { "@nroRemito", numeroRemito } });

                return Ok(new { ok = true, mensaje = "Forma de pago actualizada" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cambiando forma de pago factura {Remito}", numeroRemito);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>Genera factura electronica para un remito existente.</summary>
        [HttpPost("{numeroRemito}/generar-fe")]
        public async Task<IActionResult> GenerarFacturaElectronica(int numeroRemito)
        {
            if (!ValidarAutorizacion())
                return Unauthorized(new { error = "No autorizado" });

            try
            {
                // Obtener importe de la factura
                var importeObj = await _db.ScalarAsync<decimal>(
                    "SELECT COALESCE(importefinal, 0) FROM facturas WHERE numeroremito = @nroRemito",
                    new Dictionary<string, object?> { { "@nroRemito", numeroRemito } });
                decimal importe = importeObj;

                if (importe <= 0)
                    return BadRequest(new { error = "No se encontro la factura o el importe es 0" });

                // Determinar tipo segun condicion IVA
                var ambienteActivo = _config["AFIP:AmbienteActivo"] ?? "Testing";
                var condicionIVA = _config[$"AFIP:{ambienteActivo}:CondicionIVA"] ?? "Monotributo";
                string tipoLetra = condicionIVA.Contains("Monotributo", StringComparison.OrdinalIgnoreCase) ? "C" : "B";

                var resultado = await _afipService.GenerarFacturaElectronicaAsync(tipoLetra, importe, null);

                if (!resultado.Exito)
                    return BadRequest(new { error = resultado.Error });

                // Actualizar factura con CAE y numero
                await _db.ExecuteAsync(@"
                    UPDATE facturas SET 
                        nrofactura = @nroFactura,
                        tipofactura = @tipoFactura,
                        caenumero = @cae,
                        caevencimiento = @caeVenc
                    WHERE numeroremito = @nroRemito",
                    new Dictionary<string, object?>
                    {
                        { "@nroFactura", resultado.NumeroFactura },
                        { "@tipoFactura", $"Factura{tipoLetra}" },
                        { "@cae", resultado.CAE },
                        { "@caeVenc", resultado.VencimientoCAE },
                        { "@nroRemito", numeroRemito }
                    });

                _logger.LogInformation("FE generada para remito {Remito}: {Factura} CAE {CAE}", numeroRemito, resultado.NumeroFactura, resultado.CAE);

                return Ok(new { ok = true, nroFactura = resultado.NumeroFactura, cae = resultado.CAE, mensaje = $"Factura {resultado.NumeroFactura} - CAE: {resultado.CAE}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando FE para remito {Remito}", numeroRemito);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class CambiarFormaPagoRequest
    {
        public string FormaPago { get; set; } = "";
    }
}
