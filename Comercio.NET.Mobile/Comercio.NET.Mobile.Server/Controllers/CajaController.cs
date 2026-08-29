using Comercio.NET.Mobile.Server.Models;
using Comercio.NET.Mobile.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Comercio.NET.Mobile.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CajaController : ControllerBase
    {
        private readonly ICajaService _cajaService;
        private readonly AuthService  _authService;
        private readonly ITurnoService _turnoService;
        private readonly IConfiguration _config;
        private readonly ILogger<CajaController> _logger;

        public CajaController(
            ICajaService cajaService,
            AuthService authService,
            ITurnoService turnoService,
            IConfiguration config,
            ILogger<CajaController> logger)
        {
            _cajaService  = cajaService;
            _authService  = authService;
            _turnoService = turnoService;
            _config       = config;
            _logger       = logger;
        }

        private bool Autorizado()
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            return _authService.ValidarToken(token);
        }

        /// <summary>Devuelve la configuración de caja: formas de pago, cuáles requieren FE, número de cajero.</summary>
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });

            // Leer configuracion de FE
            var obligarFE = _config.GetValue<bool>("Caja:ObligarFacturaElectronica", true);
            var formasConFe = obligarFE
                ? (_config.GetSection("Caja:FormasPagoConFactura").Get<List<string>>() ?? new List<string> { "DNI", "Mercado Pago" })
                : new List<string>(); // Si no obliga, ninguna forma requiere FE

            var formasDisponibles = _config.GetSection("Caja:FormasPagoDisponibles")
                .Get<List<string>>() ?? new List<string>
                {
                    "Efectivo", "DNI", "Mercado Pago", "Otro"
                };

            // Leer condicion IVA del ambiente AFIP activo
            var ambienteActivo = _config["AFIP:AmbienteActivo"] ?? "Testing";
            var condicionIVA = _config[$"AFIP:{ambienteActivo}:CondicionIVA"] ?? "Monotributo";

            return Ok(new CajaConfigDto
            {
                FormasPagoConFactura    = formasConFe,
                FormasPagoDisponibles   = formasDisponibles,
                NumeroCajero            = 1,
                CondicionIVA            = condicionIVA,
                CtaCteHabilitado        = _config.GetValue<bool>("Caja:CtaCte:Habilitado", false),
                CtaCteClientes          = _config.GetSection("Caja:CtaCte:Clientes").Get<List<string>>() ?? new List<string>(),
                DescuentoOpciones       = _config.GetSection("Descuentos:OpcionesDisponibles").Get<List<decimal>>() ?? new List<decimal>(),
                DescuentoRestringirPorPago = _config.GetValue<bool>("Descuentos:RestringirPorMetodoPago", false),
                DescuentoMetodosPagoPermitidos = _config.GetSection("Descuentos:MetodosPagoPermitidos").Get<List<string>>() ?? new List<string>()
            });
        }

        /// <summary>Devuelve los datos de facturacion del comercio para impresion.</summary>
        [HttpGet("datos-facturacion")]
        public IActionResult GetDatosFacturacion()
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });

            return Ok(new
            {
                nombreComercio = _config["Comercio:Nombre"] ?? "",
                domicilioComercio = _config["Comercio:Domicilio"] ?? "",
                razonSocial = _config["Facturacion:RazonSocial"] ?? "",
                cuit = _config["Facturacion:CUIT"] ?? "",
                ingBrutos = _config["Facturacion:IngBrutos"] ?? "",
                domicilioFiscal = _config["Facturacion:DomicilioFiscal"] ?? "",
                codigoPostal = _config["Facturacion:CodigoPostal"] ?? "",
                inicioActividades = _config["Facturacion:InicioActividades"] ?? "",
                condicion = _config["Facturacion:Condicion"] ?? ""
            });
        }        /// <summary>Genera un nuevo número de remito para iniciar un ticket.</summary>
        [HttpGet("nuevo-remito")]
        public async Task<IActionResult> NuevoRemito()
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            try
            {
                var nro = await _cajaService.GenerarNroRemitoAsync();
                return Ok(new { nroRemito = nro });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando número de remito");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>Devuelve el estado actual del ticket (ítems + total).</summary>
        [HttpGet("ticket/{nroRemito}")]
        public async Task<IActionResult> GetTicket(string nroRemito)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            try
            {
                var ticket = await _cajaService.GetTicketAsync(nroRemito);
                return Ok(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo ticket {NroRemito}", nroRemito);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>Agrega o acumula un producto en el ticket.</summary>
        [HttpPost("agregar-item")]
        public async Task<IActionResult> AgregarItem([FromBody] AgregarItemRequest request)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            if (string.IsNullOrWhiteSpace(request.NroRemito)) return BadRequest(new { error = "nroRemito es requerido" });
            if (string.IsNullOrWhiteSpace(request.Codigo))    return BadRequest(new { error = "codigo es requerido" });
            if (request.Cantidad <= 0)                        return BadRequest(new { error = "cantidad debe ser mayor a 0" });
            try
            {
                var item = await _cajaService.AgregarItemAsync(request);
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error agregando ítem al ticket {NroRemito}", request.NroRemito);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>Actualiza la cantidad de un ítem ya existente en el ticket.</summary>
        [HttpPatch("item/{idVenta:long}")]
        public async Task<IActionResult> ActualizarCantidad(long idVenta, [FromBody] ActualizarCantidadRequest request)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            if (request.Cantidad <= 0) return BadRequest(new { error = "La cantidad debe ser mayor a 0" });
            try
            {
                var item = await _cajaService.ActualizarCantidadAsync(idVenta, request.Cantidad);
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando cantidad del ítem {IdVenta}", idVenta);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>Elimina un ítem del ticket por su id de venta.</summary>
        [HttpDelete("item/{idVenta:long}")]
        public async Task<IActionResult> EliminarItem(long idVenta, [FromQuery] string? usuario = null, [FromQuery] int? cajero = null, [FromQuery] string? motivo = null)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            try
            {
                await _cajaService.EliminarItemAsync(idVenta, usuario, cajero, motivo);
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando ítem {IdVenta}", idVenta);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>Cancela y elimina todos los ítems de un ticket.</summary>
        [HttpDelete("ticket/{nroRemito}")]
        public async Task<IActionResult> CancelarTicket(string nroRemito, [FromQuery] string? usuario = null, [FromQuery] int? cajero = null)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            try
            {
                await _cajaService.CancelarTicketAsync(nroRemito, usuario, cajero);
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelando ticket {NroRemito}", nroRemito);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>Confirma la venta: registra la factura, detalles de pago y descuenta stock.</summary>
        [HttpPost("confirmar")]
        public async Task<IActionResult> Confirmar([FromBody] ConfirmarVentaRequest request)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            if (string.IsNullOrWhiteSpace(request.NroRemito)) return BadRequest(new { error = "nroRemito es requerido" });
            try
            {
                var resultado = await _cajaService.ConfirmarVentaAsync(request);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirmando venta {NroRemito}", request.NroRemito);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>Agrega un nuevo cliente a la lista de Cta.Cte. en la configuración.</summary>
        [HttpPost("ctacte/agregar")]
        public IActionResult AgregarClienteCtaCte([FromBody] AgregarCtaCteRequest req)
        {
            if (!Autorizado()) return Unauthorized(new { error = "No autorizado" });
            if (string.IsNullOrWhiteSpace(req?.Nombre)) return BadRequest(new { error = "Nombre requerido" });

            try
            {
                var clientes = _config.GetSection("Caja:CtaCte:Clientes").Get<List<string>>() ?? new List<string>();
                if (clientes.Contains(req.Nombre, StringComparer.OrdinalIgnoreCase))
                    return Ok(new { ok = true, mensaje = "Cliente ya existe", clientes });

                clientes.Add(req.Nombre.Trim());

                // Persistir en appsettings.json
                var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (!System.IO.File.Exists(appSettingsPath))
                    appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

                var json = System.IO.File.ReadAllText(appSettingsPath);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                using var ms = new MemoryStream();
                using (var writer = new System.Text.Json.Utf8JsonWriter(ms, new System.Text.Json.JsonWriterOptions { Indented = true }))
                {
                    writer.WriteStartObject();
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Name == "Caja")
                        {
                            writer.WritePropertyName("Caja");
                            writer.WriteStartObject();
                            foreach (var cajaProp in prop.Value.EnumerateObject())
                            {
                                if (cajaProp.Name == "CtaCte")
                                {
                                    writer.WritePropertyName("CtaCte");
                                    writer.WriteStartObject();
                                    writer.WriteBoolean("Habilitado", true);
                                    writer.WritePropertyName("Clientes");
                                    writer.WriteStartArray();
                                    foreach (var c in clientes) writer.WriteStringValue(c);
                                    writer.WriteEndArray();
                                    writer.WriteEndObject();
                                }
                                else
                                {
                                    cajaProp.WriteTo(writer);
                                }
                            }
                            writer.WriteEndObject();
                        }
                        else
                        {
                            prop.WriteTo(writer);
                        }
                    }
                    writer.WriteEndObject();
                }
                System.IO.File.WriteAllBytes(appSettingsPath, ms.ToArray());

                return Ok(new { ok = true, mensaje = "Cliente agregado", clientes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error agregando cliente CtaCte");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class ActualizarCantidadRequest
    {
        public int Cantidad { get; set; }
    }

    public class AgregarCtaCteRequest
    {
        public string Nombre { get; set; } = "";
    }
}
