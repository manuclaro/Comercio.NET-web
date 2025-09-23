using Newtonsoft.Json.Linq;
using System;
using System.Windows.Forms;
using WSconsultaCUIT;
using ArcaWS;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Drawing.Printing;
using System.Globalization;
using System.ServiceModel; // AGREGAR ESTA LÍNEA
using System.Threading.Tasks; // AGREGAR ESTA LÍNEA SI NO ESTÁ
using Comercio.NET.Servicios;

namespace Comercio.NET
{
    public partial class SeleccionImpresionForm : Form
    {
        public enum OpcionImpresion
        {
            Ninguna,
            RemitoTicket,
            FacturaB,
            FacturaA
        }

        public enum OpcionPago
        {
            Efectivo,
            DNI,
            MercadoPago
        }

        public OpcionImpresion OpcionSeleccionada { get; private set; } = OpcionImpresion.Ninguna;
        public OpcionPago OpcionPagoSeleccionada { get; private set; } = OpcionPago.Efectivo;

        private TextBox txtCuit;
        private Label lblRazonSocial;

        // Delegate para el callback después de procesar la venta (CAMBIO: agregar parámetro numeroFacturaAfip)
        public Func<string, string, string, string, DateTime?, int, string, Task> OnProcesarVenta { get; set; }

        private decimal importeTotalVenta;
        private Ventas formularioPadre; // AGREGAR ESTA LÍNEA

        // Modificar el constructor para recibir el importe total
        public SeleccionImpresionForm(decimal importeTotal = 0, Ventas padre = null)
        {
            this.importeTotalVenta = importeTotal;
            this.formularioPadre = padre;

            this.Text = "Seleccione tipo de impresión";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Width = 600;
            this.Height = 270;

            var fontRadio = new Font("Segoe UI", 12F, FontStyle.Regular);

            // Opciones de impresión - CAMBIAR LOS EVENT HANDLERS
            var btnRemito = new Button { Text = "Remito (Ticket)", Width = 130, Left = 60, Top = 150, Height = 40 };
            var btnFacturaB = new Button { Text = "Factura B", Width = 130, Left = 210, Top = 150, Height = 40 };
            var btnFacturaA = new Button { Text = "Factura A", Width = 130, Left = 360, Top = 150, Height = 40 };

            btnRemito.Click += async (s, e) => await ProcesarRemito();
            btnFacturaB.Click += async (s, e) => await ProcesarFacturaB();
            btnFacturaA.Click += async (s, e) => await ProcesarFacturaA();

            // Opciones de pago (RadioButtons)
            var lblPago = new Label { Text = "Forma de pago:", Left = 40, Top = 30, Width = 200, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            var rbEfectivo = new RadioButton { Text = "Efectivo", Left = 70, Top = 70, Width = 140, Height = 30, Font = fontRadio, Checked = true };
            var rbDNI = new RadioButton { Text = "DNI", Left = 210, Top = 70, Width = 160, Height = 30, Font = fontRadio };
            var rbMercadoPago = new RadioButton { Text = "MercadoPago", Left = 370, Top = 70, Width = 180, Height = 30, Font = fontRadio };

            rbEfectivo.CheckedChanged += (s, e) => { if (rbEfectivo.Checked) OpcionPagoSeleccionada = OpcionPago.Efectivo; };
            rbDNI.CheckedChanged += (s, e) => { if (rbDNI.Checked) OpcionPagoSeleccionada = OpcionPago.DNI; };
            rbMercadoPago.CheckedChanged += (s, e) => { if (rbMercadoPago.Checked) OpcionPagoSeleccionada = OpcionPago.MercadoPago; };

            this.Controls.Add(lblPago);
            this.Controls.Add(rbEfectivo);
            this.Controls.Add(rbDNI);
            this.Controls.Add(rbMercadoPago);

            this.Controls.Add(btnRemito);
            this.Controls.Add(btnFacturaB);
            this.Controls.Add(btnFacturaA);

            // Controles para CUIT y Razón Social
            txtCuit = new TextBox
            {
                Left = 90,
                Top = 110,
                Width = 120,
                Font = new Font("Segoe UI", 10F)
            };
            lblRazonSocial = new Label
            {
                Text = "",
                Left = 220,
                Top = 112,
                Width = 600,
                Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                ForeColor = System.Drawing.Color.DarkGreen
            };
            var lblCuit = new Label
            {
                Text = "CUIT:",
                Left = 40,
                Top = 112,
                Width = 50,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            txtCuit.Leave += async (s, e) => await ConsultarCuitAsync();
            txtCuit.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await ConsultarCuitAsync();
                }
            };

            this.Controls.Add(lblCuit);
            this.Controls.Add(txtCuit);
            this.Controls.Add(lblRazonSocial);

            // Poner el foco en el botón Remito al abrir el modal
            this.Shown += (s, e) => btnRemito.Focus();
        }

        // Modificar SOLO el método ProcesarRemito para no imprimir inmediatamente
        private async Task ProcesarRemito()
        {
            try
            {
                OpcionSeleccionada = OpcionImpresion.RemitoTicket;
                string formaPago = OpcionPagoSeleccionada.ToString();
                
                // Llamar al callback para guardar en BD
                if (OnProcesarVenta != null)
                {
                    await OnProcesarVenta("Remito", formaPago, "", "", null, 0, ""); // Agregar parámetro vacío para remitos
                }

                // NO IMPRIMIR AÚN - Solo cerrar el modal
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error procesando remito: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ProcesarFacturaB()
        {
            try
            {
                OpcionSeleccionada = OpcionImpresion.FacturaB;
                
                string formaPago = OpcionPagoSeleccionada.ToString();
                bool exito = await CrearFacturaBAsync();
                
                if (exito)
                {
                    // Formatear el número de factura antes de enviarlo
                    string numeroFormateado = FormatearNumeroFactura(6, 1, NumeroFacturaAfip); // 6 = Factura B
                    
                    if (OnProcesarVenta != null)
                    {
                        // CORRECCIÓN: Pasar todos los parámetros incluyendo el número formateado
                        await OnProcesarVenta("FacturaB", formaPago, "", CAENumero, CAEVencimiento, NumeroFacturaAfip, numeroFormateado);
                    }

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            } 
            catch (Exception ex)
            {
                MessageBox.Show($"Error procesando Factura B: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ProcesarFacturaA()
        {
            try
            {
                string cuit = txtCuit.Text.Trim();
                if (string.IsNullOrWhiteSpace(cuit) || cuit.Length != 11)
                {
                    MessageBox.Show("Debe ingresar un CUIT válido de 11 dígitos para la Factura A.", "CUIT Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCuit.Focus();
                    return;
                }

                OpcionSeleccionada = OpcionImpresion.FacturaA;
                
                string formaPago = OpcionPagoSeleccionada.ToString();
                bool exito = await CrearFacturaAAsync(cuit);
                
                if (exito)
                {
                    // Formatear el número de factura antes de enviarlo
                    string numeroFormateado = FormatearNumeroFactura(1, 1, NumeroFacturaAfip); // 1 = Factura A
                    
                    if (OnProcesarVenta != null)
                    {
                        // CORRECCIÓN: Pasar CUIT como tercer parámetro y número formateado como último
                        await OnProcesarVenta("FacturaA", formaPago, cuit, CAENumero, CAEVencimiento, NumeroFacturaAfip, numeroFormateado);
                    }

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error procesando Factura A: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Propiedades para almacenar datos del CAE - VERIFICAR que sean públicas
        public string CAENumero { get; private set; } = "";
        public DateTime? CAEVencimiento { get; private set; } = null;
        public int NumeroFacturaAfip { get; private set; } = 0;

        // Métodos para crear facturas (movidos desde Ventas.cs)
        private async Task<bool> CrearFacturaBAsync()
        {
            return await CrearFacturaAfipAsync(
                cbteTipo: 6,
                condicionIVAReceptor: 5,
                docTipo: 99,
                docNro: 0,
                alicuotaIVA: 21m
            );
        }

        private async Task<bool> CrearFacturaAAsync(string cuitReceptor)
        {
            if (!long.TryParse(cuitReceptor, out long cuitValido) || cuitReceptor.Length != 11)
            {
                MessageBox.Show("Debe ingresar un CUIT válido de 11 dígitos para la Factura A.");
                return false;
            }
            if (!EsCuitValido(cuitReceptor))
            {
                MessageBox.Show("Debe ingresar un CUIT válido para la Factura A.");
                return false;
            }
            return await CrearFacturaAfipAsync(
                cbteTipo: 1,
                condicionIVAReceptor: 1,
                docTipo: 80,
                docNro: cuitValido,
                alicuotaIVA: 21
            );
        }

        private async Task<bool> CrearFacturaAfipAsync(int cbteTipo, int condicionIVAReceptor, int docTipo, long docNro, decimal alicuotaIVA)
        {
            const int maxReintentos = 3;
            const int tiempoEsperaMs = 2000;

            for (int intento = 1; intento <= maxReintentos; intento++)
            {
                try
                {
                    string cuit = "20280694739";
                    string service = "wsfe";
                    string pfxPath = @"C:\Certificados\certificado.pfx";
                    string pfxPassword = "Micertificado";
                    string wsaaUrl = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";

                    // CAMBIO: Usar el servicio AfipAuthenticator en lugar de Ventas.AfipAuthenticator
                    (string token, string sign) = await AfipAuthenticator.GetTAAsync(service, pfxPath, pfxPassword, wsaaUrl);

                    TokenAfip = token;
                    SignAfip = sign;

                    var client = new ArcaWS.ServiceSoapClient(ArcaWS.ServiceSoapClient.EndpointConfiguration.ServiceSoap);

                    var auth = new ArcaWS.FEAuthRequest
                    {
                        Token = token,
                        Sign = sign,
                        Cuit = Convert.ToInt64(cuit)
                    };

                    int ptoVta = 1;
                    var ultimoResp = await client.FECompUltimoAutorizadoAsync(auth, ptoVta, cbteTipo);
                    int ultimoNroAfip = ultimoResp.Body.FECompUltimoAutorizadoResult.CbteNro;
                    int nuevoNroComprobante = ultimoNroAfip + 1;

                    decimal impTotal = ObtenerImporteTotalVenta();
                    decimal impIVA = Math.Round(impTotal - (impTotal / (1 + (alicuotaIVA / 100m))), 2);
                    decimal impNeto = Math.Round(impTotal - impIVA, 2);

                    var iva = new ArcaWS.AlicIva
                    {
                        Id = (alicuotaIVA == 21m) ? 5 : 4,
                        BaseImp = (double)impNeto,
                        Importe = (double)impIVA
                    };

                    var feCabReq = new ArcaWS.FECAECabRequest
                    {
                        CantReg = 1,
                        PtoVta = ptoVta,
                        CbteTipo = cbteTipo
                    };

                    var feDetReq = new ArcaWS.FECAEDetRequest
                    {
                        Concepto = 1,
                        DocTipo = docTipo,
                        DocNro = docNro,
                        CbteDesde = nuevoNroComprobante,
                        CbteHasta = nuevoNroComprobante,
                        CbteFch = DateTime.Now.ToString("yyyyMMdd"),
                        ImpTotal = (double)impTotal,
                        ImpNeto = (double)impNeto,
                        ImpIVA = (double)impIVA,
                        MonId = "PES",
                        MonCotiz = 1,
                        CondicionIVAReceptorId = condicionIVAReceptor,
                        ImpTrib = 0,
                        ImpOpEx = 0,
                        Iva = new ArcaWS.AlicIva[] { iva }
                    };

                    var feCAEReq = new ArcaWS.FECAERequest
                    {
                        FeCabReq = feCabReq,
                        FeDetReq = new ArcaWS.FECAEDetRequest[] { feDetReq }
                    };

                    var respuesta = await client.FECAESolicitarAsync(auth, feCAEReq);
                    var resultado = respuesta?.Body?.FECAESolicitarResult;

                    if (resultado != null && resultado.FeDetResp != null && resultado.FeDetResp.Length > 0)
                    {
                        var detalle = resultado.FeDetResp[0];
                        if (detalle.Resultado == "A")
                        {
                            CAENumero = detalle.CAE;
                            NumeroFacturaAfip = (int)detalle.CbteDesde;
                            
                            // NUEVO: Formatear el número de factura según estándar AFIP
                            string numeroFacturaFormateado = FormatearNumeroFactura(cbteTipo, ptoVta, NumeroFacturaAfip);
                            
                            if (DateTime.TryParseExact(detalle.CAEFchVto, "yyyyMMdd", null, DateTimeStyles.None, out DateTime fechaVto))
                            {
                                CAEVencimiento = fechaVto;
                            }

                            MessageBox.Show($"Factura Aprobada.\nCAE: {CAENumero}\nComprobante: {numeroFacturaFormateado}");
                            return true;
                        }
                        else if (detalle.Resultado == "R")
                        {
                            string mensaje = "La factura fue RECHAZADA.\n";
                            if (detalle.Observaciones != null && detalle.Observaciones.Length > 0)
                            {
                                foreach (var obs in detalle.Observaciones)
                                {
                                    mensaje += $"Código: {obs.Code} - {obs.Msg}\n";
                                }
                            }
                            mensaje += "\nVerifique los datos ingresados o intente nuevamente.";
                            MessageBox.Show(mensaje, "Rechazo AFIP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                    }

                    MessageBox.Show("No se obtuvo respuesta válida de AFIP.");
                    return false;
                }
                catch (Exception ex) when (ex.Message.Contains("no está disponible") || 
                                       ex.Message.Contains("not available") || 
                                       ex.Message.Contains("timeout") ||
                                       ex.Message.Contains("endpoint"))
                {
                    if (intento == maxReintentos)
                    {
                        MessageBox.Show($"El servicio AFIP no está disponible después de {maxReintentos} intentos.\n" +
                                      "Esto puede deberse a:\n" +
                                      "• Mantenimiento del servicio AFIP\n" +
                                      "• Problemas de conectividad\n" +
                                      "• Sobrecarga del servidor\n\n" +
                                      "Intente nuevamente en unos minutos o considere emitir un remito temporalmente.\n\n" +
                                      $"Error técnico: {ex.Message}",
                                      "Servicio AFIP No Disponible", 
                                      MessageBoxButtons.OK, 
                                      MessageBoxIcon.Warning);
                        return false;
                    }
                    
                    MessageBox.Show($"Intento {intento} falló. Reintentando en {tiempoEsperaMs / 1000} segundos...", 
                                   "Reintentando Conexión", 
                                   MessageBoxButtons.OK, 
                                   MessageBoxIcon.Information);
                    
                    await Task.Delay(tiempoEsperaMs);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error inesperado: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            
            return false;
        }

        // NUEVO: Método para formatear número de factura según estándar AFIP
        private string FormatearNumeroFactura(int cbteTipo, int ptoVta, int numeroFactura)
        {
            // Obtener la letra según el tipo de comprobante
            string letra = ObtenerLetraComprobante(cbteTipo);

            // MODIFICADO: Agregar espacio entre la letra y los números
            // Formatear: Letra + ESPACIO + PtoVta (4 dígitos) + Número (8 dígitos)
            // Ejemplo: "A 0001-00000123" o "B 0001-00000456"
            return $"{letra} {ptoVta:D4}-{numeroFactura:D8}";
        }

        // NUEVO: Método para obtener la letra del comprobante según código AFIP
        private string ObtenerLetraComprobante(int cbteTipo)
        {
            return cbteTipo switch
            {
                1 => "A",     // Factura A
                6 => "B",     // Factura B
                11 => "C",    // Factura C
                51 => "M",    // Factura M
                // Agregar más tipos según necesites
                _ => "X"      // Tipo desconocido
            };
        }

        private decimal ObtenerImporteTotalVenta()
        {
            return importeTotalVenta;
        }

        private bool EsCuitValido(string cuit)
        {
            if (cuit.Length != 11 || !long.TryParse(cuit, out _))
                return false;

            int[] coef = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
            int suma = 0;
            for (int i = 0; i < 10; i++)
                suma += int.Parse(cuit[i].ToString()) * coef[i];

            int resto = suma % 11;
            int digitoVerificador = resto == 0 ? 0 : resto == 1 ? 9 : 11 - resto;

            return digitoVerificador == int.Parse(cuit[10].ToString());
        }

        public string TokenAfip { get; set; }
        public string SignAfip { get; set; }

        private async Task ConsultarCuitAsync()
        {
            string cuit = txtCuit.Text.Trim();
            lblRazonSocial.Text = "";

            if (cuit.Length == 11 && long.TryParse(cuit, out long cuitLong))
            {
                try
                {
                    lblRazonSocial.Text = "Consultando...";

                    bool tieneCredenciales = await EnsureTokenAndSignAsync();
                    if (!tieneCredenciales)
                    {
                        lblRazonSocial.Text = "No se pudo obtener credenciales AFIP.";
                        return;
                    }

                    var client = new WSconsultaCUIT.PersonaServiceA5Client();
                    var respuesta = await client.getPersonaAsync(
                        TokenAfip ?? "",
                        SignAfip ?? "",
                        20280694739, // CUIT de tu empresa
                        cuitLong
                    );

                    var persona = respuesta.personaReturn;

                    if (persona != null && persona.errorConstancia == null)
                    {
                        if (!string.IsNullOrEmpty(persona.datosGenerales?.razonSocial))
                            lblRazonSocial.Text = persona.datosGenerales.razonSocial;
                        else if (!string.IsNullOrEmpty(persona.datosGenerales?.nombre) && !string.IsNullOrEmpty(persona.datosGenerales?.apellido))
                            lblRazonSocial.Text = $"{persona.datosGenerales.nombre} {persona.datosGenerales.apellido}";
                        else
                            lblRazonSocial.Text = "CUIT válido, sin razón social.";
                    }
                    else if (persona != null && persona.errorConstancia != null)
                    {
                        var errores = persona.errorConstancia.GetType().GetProperty("error")?.GetValue(persona.errorConstancia) as string[];
                        if (errores != null && errores.Length > 0)
                            lblRazonSocial.Text = string.Join(" | ", errores);
                        else
                            lblRazonSocial.Text = "CUIT no encontrado o inválido.";
                    }
                    else
                    {
                        lblRazonSocial.Text = "CUIT no encontrado o inválido.";
                    }
                }
                catch (Exception ex)
                {
                    lblRazonSocial.Text = "Error consultando CUIT: " + ex.Message;
                }
            }
            else if (!string.IsNullOrEmpty(cuit))
            {
                lblRazonSocial.Text = "CUIT inválido.";
            }
        }

        private async Task<bool> EnsureTokenAndSignAsync()
        {
            var (tokenPadron, signPadron) = await GetTokenAndSignAsync("ws_sr_padron_a5");

            if (!string.IsNullOrEmpty(tokenPadron) && !string.IsNullOrEmpty(signPadron))
            {
                TokenAfip = tokenPadron;
                SignAfip = signPadron;
                return true;
            }
            return false;
        }

        private async Task<(string token, string sign)> GetTokenAndSignAsync(string service)
        {
            string pfxPath = @"C:\Certificados\certificado.pfx";
            string pfxPassword = "Micertificado";
            string wsaaUrl = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";
            
            // CAMBIO: Usar el servicio AfipAuthenticator en lugar de Ventas.AfipAuthenticator
            return await AfipAuthenticator.GetTAAsync(service, pfxPath, pfxPassword, wsaaUrl);
        }

        private async Task<bool> VerificarEstadoServicioAfipAsync()
        {
            try
            {
                string cuit = "20280694739";
                string service = "wsfe";
                string pfxPath = @"C:\Certificados\certificado.pfx";
                string pfxPassword = "Micertificado";
                string wsaaUrl = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";

                // CAMBIO: Usar el servicio AfipAuthenticator en lugar de Ventas.AfipAuthenticator
                (string token, string sign) = await AfipAuthenticator.GetTAAsync(service, pfxPath, pfxPassword, wsaaUrl);

                var client = new ArcaWS.ServiceSoapClient(ArcaWS.ServiceSoapClient.EndpointConfiguration.ServiceSoap);
                
                var auth = new ArcaWS.FEAuthRequest
                {
                    Token = token,
                    Sign = sign,
                    Cuit = Convert.ToInt64(cuit)
                };

                var respuesta = await client.FEDummyAsync();
                return respuesta?.Body?.FEDummyResult != null;
            }
            catch
            {
                return false;
            }
        }

        // MODIFICAR: Actualizar el método de impresión para usar el número correcto
        public void ImprimirTicketDespuesDeGuardar(string tipoComprobante, string numeroComprobante)
        {
            try
            {
                DataTable datosTicket = formularioPadre?.GetRemitoActual();
                
                if (datosTicket == null || datosTicket.Rows.Count == 0)
                {
                    MessageBox.Show("No hay productos para imprimir.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // NUEVO: Determinar el número correcto a mostrar
                string numeroParaTicket;
                if (tipoComprobante == "REMITO")
                {
                    // Para remitos, usar el número de remito
                    numeroParaTicket = formularioPadre?.GetNroRemitoActual().ToString() ?? numeroComprobante;
                }
                else
                {
                    // Para facturas, usar el número formateado de AFIP
                    if (NumeroFacturaAfip > 0)
                    {
                        int cbteTipo = tipoComprobante == "FACTURA" ? 1 : 6;
                        numeroParaTicket = FormatearNumeroFactura(cbteTipo, 1, NumeroFacturaAfip);
                    }
                    else
                    {
                        numeroParaTicket = numeroComprobante;
                    }
                }

                var config = new TicketConfig
                {
                    NombreComercio = formularioPadre?.GetNombreComercio() ?? "Tu Comercio",
                    DomicilioComercio = formularioPadre?.GetDomicilioComercio() ?? "Tu Domicilio",
                    TipoComprobante = tipoComprobante,
                    NumeroComprobante = numeroParaTicket, // Usar el número correcto
                    FormaPago = OpcionPagoSeleccionada.ToString(),
                    MensajePie = "Gracias por su compra!"
                };

                if (tipoComprobante.Contains("FACTURA"))
                {
                    config.CAE = CAENumero;
                    config.CAEVencimiento = CAEVencimiento;
                    if (tipoComprobante == "FACTURA")
                    {
                        config.CUIT = txtCuit.Text.Trim();
                    }
                }

                using (var ticketService = new TicketPrintingService())
                {
                    ticketService.ImprimirTicket(datosTicket, config);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir ticket: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
