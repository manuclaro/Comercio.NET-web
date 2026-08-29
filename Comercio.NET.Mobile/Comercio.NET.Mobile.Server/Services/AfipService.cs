using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Comercio.NET.Mobile.Server.Services
{
    public class AfipService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AfipService> _logger;
        private string? _token;
        private string? _sign;

        public AfipService(IConfiguration config, ILogger<AfipService> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Genera factura electronica con AFIP y devuelve el CAE, numero de factura y vencimiento.
        /// </summary>
        public async Task<AfipFacturaResult> GenerarFacturaElectronicaAsync(
            string tipoFactura, decimal montoTotal, string? cuitCliente = null)
        {
            try
            {
                _logger.LogInformation("Iniciando factura electronica AFIP - Tipo: {Tipo}, Monto: {Monto}", tipoFactura, montoTotal);

                // 1. Autenticar
                await AutenticarAsync();

                // 2. Determinar tipo de comprobante
                int tipoComprobante = tipoFactura switch
                {
                    "A" => 1,
                    "B" => 6,
                    "C" => 11,
                    _ => 11
                };

                // 3. Obtener punto de venta
                int puntoVenta = ObtenerPuntoVenta();

                // 4. Obtener ultimo numero de comprobante
                int ultimoNumero = await ObtenerUltimoNumeroAsync(tipoComprobante, puntoVenta);
                int numeroComprobante = ultimoNumero + 1;

                _logger.LogInformation("AFIP - PV: {PV}, Ultimo: {Ultimo}, Nuevo: {Nuevo}", puntoVenta, ultimoNumero, numeroComprobante);

                // 5. Solicitar CAE
                var resultado = await SolicitarCAEAsync(tipoComprobante, puntoVenta, numeroComprobante, montoTotal, tipoFactura, cuitCliente);

                if (!resultado.Exito)
                {
                    return new AfipFacturaResult
                    {
                        Exito = false,
                        Error = resultado.Error
                    };
                }

                // 6. Formatear numero de factura
                string tipoLetra = tipoComprobante switch
                {
                    1 => "A",
                    6 => "B",
                    11 => "C",
                    _ => "X"
                };
                string numeroFormateado = $"{tipoLetra} {puntoVenta:D4}-{numeroComprobante:D8}";

                _logger.LogInformation("AFIP - Factura generada: {Numero}, CAE: {CAE}", numeroFormateado, resultado.CAE);

                return new AfipFacturaResult
                {
                    Exito = true,
                    NumeroFactura = numeroFormateado,
                    CAE = resultado.CAE,
                    VencimientoCAE = resultado.Vencimiento ?? DateTime.Now.AddDays(10)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando factura electronica AFIP");
                return new AfipFacturaResult
                {
                    Exito = false,
                    Error = ex.Message
                };
            }
        }

        private async Task AutenticarAsync()
        {
            var (tieneTokenValido, _, minutosRestantes) = AfipAuthenticator.VerificarTokensExistentes("wsfe");

            if (tieneTokenValido && minutosRestantes > 2)
            {
                var tokenExistente = AfipAuthenticator.GetExistingToken("wsfe");
                if (tokenExistente.HasValue)
                {
                    _token = tokenExistente.Value.token;
                    _sign = tokenExistente.Value.sign;
                    _logger.LogInformation("AFIP - Usando token existente valido");
                    return;
                }
            }

            var (token, sign, _) = await AfipAuthenticator.GetTAAsync("wsfe");
            _token = token;
            _sign = sign;
            _logger.LogInformation("AFIP - Nuevo token obtenido");
        }

        private int ObtenerPuntoVenta()
        {
            string ambienteActivo = _config["AFIP:AmbienteActivo"] ?? "Testing";
            string? puntoVentaStr = _config[$"AFIP:{ambienteActivo}:PuntoVenta"];

            if (!string.IsNullOrEmpty(puntoVentaStr) && int.TryParse(puntoVentaStr, out int pv))
                return pv;

            return 1;
        }

        private string ObtenerCuit()
        {
            return AfipAuthenticator.ObtenerCUITActivo();
        }

        private async Task<int> ObtenerUltimoNumeroAsync(int tipoComprobante, int puntoVenta)
        {
            using var wsfeClient = AfipAuthenticator.CrearClienteWSFE();

            var authRequest = new ArcaWS.FEAuthRequest
            {
                Token = _token,
                Sign = _sign,
                Cuit = long.Parse(ObtenerCuit().Replace("-", ""))
            };

            var response = await wsfeClient.FECompUltimoAutorizadoAsync(authRequest, puntoVenta, tipoComprobante);
            var resultado = response.Body.FECompUltimoAutorizadoResult;

            if (resultado?.Errors != null && resultado.Errors.Length > 0)
            {
                string errores = string.Join(", ", resultado.Errors.Select(e => $"{e.Code}: {e.Msg}"));
                throw new Exception($"Error AFIP consultando ultimo comprobante: {errores}");
            }

            return resultado?.CbteNro ?? 0;
        }

        private async Task<(bool Exito, string CAE, DateTime? Vencimiento, string Error)> SolicitarCAEAsync(
            int tipoComprobante, int puntoVenta, int numero, decimal montoTotal, string tipoFactura, string? cuitCliente)
        {
            using var wsfeClient = AfipAuthenticator.CrearClienteWSFE();

            var authRequest = new ArcaWS.FEAuthRequest
            {
                Token = _token,
                Sign = _sign,
                Cuit = long.Parse(ObtenerCuit().Replace("-", ""))
            };

            bool esFacturaC = (tipoComprobante == 11);

            // Determinar docTipo y docNro
            int docTipo;
            long docNro;

            if (tipoComprobante == 1) // Factura A
            {
                docTipo = 80; // CUIT
                docNro = !string.IsNullOrEmpty(cuitCliente) ? long.Parse(cuitCliente.Replace("-", "")) : 0;
            }
            else
            {
                docTipo = 99; // Consumidor Final
                docNro = 0;
            }

            // Calcular importes
            decimal importeNeto;
            decimal importeIva;

            if (esFacturaC)
            {
                importeNeto = montoTotal;
                importeIva = 0;
            }
            else
            {
                importeNeto = Math.Round(montoTotal / 1.21m, 2);
                importeIva = Math.Round(montoTotal - importeNeto, 2);
            }

            var comprobante = new ArcaWS.FECAEDetRequest
            {
                Concepto = 1, // Productos
                DocTipo = docTipo,
                DocNro = docNro,
                CbteDesde = numero,
                CbteHasta = numero,
                CbteFch = DateTime.Now.ToString("yyyyMMdd"),
                ImpTotal = (double)montoTotal,
                ImpTotConc = 0,
                ImpNeto = (double)importeNeto,
                ImpOpEx = 0,
                ImpIVA = (double)importeIva,
                ImpTrib = 0,
                MonId = "PES",
                MonCotiz = 1,
                // Condicion IVA del receptor: 1=Resp.Inscripto, 5=Cons.Final
                CondicionIVAReceptorId = tipoComprobante == 1 ? 1 : 5
            };

            // Agregar IVA para Factura A y B
            if (!esFacturaC && importeIva > 0)
            {
                comprobante.Iva = new[]
                {
                    new ArcaWS.AlicIva
                    {
                        Id = 5, // 21%
                        BaseImp = (double)importeNeto,
                        Importe = (double)importeIva
                    }
                };
            }

            var request = new ArcaWS.FECAERequest
            {
                FeCabReq = new ArcaWS.FECAECabRequest
                {
                    CantReg = 1,
                    PtoVta = puntoVenta,
                    CbteTipo = tipoComprobante
                },
                FeDetReq = new ArcaWS.FECAEDetRequest[] { comprobante }
            };

            var response = await wsfeClient.FECAESolicitarAsync(authRequest, request);
            var resultado = response.Body.FECAESolicitarResult;

            if (resultado?.Errors != null && resultado.Errors.Length > 0)
            {
                string errores = string.Join(", ", resultado.Errors.Select(e => e.Msg));
                return (false, "", null, errores);
            }

            if (resultado?.FeDetResp != null && resultado.FeDetResp.Length > 0)
            {
                var detalle = resultado.FeDetResp[0];

                if (!string.IsNullOrEmpty(detalle.CAE))
                {
                    DateTime? fechaVencimiento = null;
                    if (!string.IsNullOrEmpty(detalle.CAEFchVto))
                    {
                        DateTime.TryParseExact(detalle.CAEFchVto, "yyyyMMdd",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha);
                        fechaVencimiento = fecha;
                    }

                    return (true, detalle.CAE, fechaVencimiento, "");
                }
                else
                {
                    string errores = detalle.Observaciones != null
                        ? string.Join(", ", detalle.Observaciones.Select(o => o.Msg))
                        : "Sin detalles";
                    return (false, "", null, $"AFIP rechazo: {errores}");
                }
            }

            return (false, "", null, "Respuesta invalida de AFIP");
        }
    }

    public class AfipFacturaResult
    {
        public bool Exito { get; set; }
        public string NumeroFactura { get; set; } = "";
        public string CAE { get; set; } = "";
        public DateTime VencimientoCAE { get; set; }
        public string Error { get; set; } = "";
    }
}
