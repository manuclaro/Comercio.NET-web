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

        // Cache de tokens para evitar duplicados en AFIP
        private static string _cachedTokenWsfe = null;
        private static string _cachedSignWsfe = null;
        private static DateTime _cachedTokenExpiryWsfe = DateTime.MinValue;

        // Propiedades para almacenar datos del CAE - VERIFICAR que sean públicas
        public string CAENumero { get; private set; } = "";
        public DateTime? CAEVencimiento { get; private set; } = null;
        public int NumeroFacturaAfip { get; private set; } = 0;

        public string TokenAfip { get; set; }
        public string SignAfip { get; set; }

        // NUEVO: Variable para controlar debug
        private bool debugMode = true; // Cambiar a false para producción

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

            // CORREGIDO: Event handlers con mejor manejo de errores
            btnRemito.Click += async (s, e) => 
            {
                try
                {
                    await ProcesarRemito();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error en Remito: {ex.Message}\n\nStack: {ex.StackTrace}", 
                        "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnFacturaB.Click += async (s, e) => 
            {
                try
                {
                    await ProcesarFacturaB();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error en Factura B: {ex.Message}\n\nStack: {ex.StackTrace}", 
                        "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnFacturaA.Click += async (s, e) => 
            {
                try
                {
                    await ProcesarFacturaA();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error en Factura A: {ex.Message}\n\nStack: {ex.StackTrace}", 
                        "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
        }

        // NUEVO: Método helper para debug condicional
        private void DebugMessage(string message, string title = "Debug")
        {
            if (debugMode)
            {
                System.Diagnostics.Debug.WriteLine($"[{title}] {message}");
                Console.WriteLine($"[{title}] {message}");
            }
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

        // NUEVO: Método ProcesarFacturaA (similar a ProcesarFacturaB pero para Factura A)
        private async Task ProcesarFacturaA()
        {
            try
            {
                // Validar que se haya ingresado CUIT para Factura A
                string cuit = txtCuit.Text.Trim();
                if (string.IsNullOrEmpty(cuit) || cuit.Length != 11 || !EsCuitValido(cuit))
                {
                    MessageBox.Show("❌ Para Factura A debe ingresar un CUIT válido.", 
                        "CUIT Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCuit.Focus();
                    return;
                }

                DebugMessage("ProcesarFacturaA iniciado correctamente");
                
                OpcionSeleccionada = OpcionImpresion.FacturaA;
                string formaPago = OpcionPagoSeleccionada.ToString();
                
                DebugMessage($"Datos preparados: Opción={OpcionSeleccionada}, Pago={formaPago}, CUIT={cuit}, Importe=${importeTotalVenta}");
                
                DebugMessage("Llamando a CrearFacturaAAsync()...");
                
                bool exito = await CrearFacturaAAsync();
                
                DebugMessage($"CrearFacturaAAsync() terminó. Resultado: {exito}");
                
                if (exito)
                {
                    DebugMessage("Éxito = true, formateando número...");
                    
                    // Formatear el número de factura antes de enviarlo
                    string numeroFormateado = FormatearNumeroFactura(1, 1, NumeroFacturaAfip); // 1 = Factura A
                    
                    DebugMessage($"Número formateado: {numeroFormateado}");
                    
                    if (OnProcesarVenta != null)
                    {
                        DebugMessage("Llamando callback OnProcesarVenta...");
                        
                        // Pasar todos los parámetros incluyendo el CUIT y número formateado
                        await OnProcesarVenta("FacturaA", formaPago, cuit, CAENumero, CAEVencimiento, NumeroFacturaAfip, numeroFormateado);
                        
                        DebugMessage("Callback terminado exitosamente");
                    }
                    else
                    {
                        DebugMessage("ERROR: OnProcesarVenta es NULL");
                    }

                    DebugMessage("Cerrando modal...");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("❌ Error creando Factura A en AFIP.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            } 
            catch (Exception ex)
            {
                MessageBox.Show($"🚨 Excepción en ProcesarFacturaA:\n{ex.Message}\n\nStack:\n{ex.StackTrace}", 
                    "Error Detallado", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw; // Re-lanzar para que el handler del botón también la capture
            }
        }

        // NUEVO: CrearFacturaAAsync (similar a CrearFacturaBAsync pero para Factura A)
        private async Task<bool> CrearFacturaAAsync()
        {
            try
            {
                DebugMessage("CrearFacturaAAsync iniciado");
                
                var resultado = await CrearFacturaAfipAsync(
                    cbteTipo: 1,  // 1 = Factura A
                    condicionIVAReceptor: 1, // 1 = IVA Responsable Inscripto (típico para Factura A)
                    docTipo: 80, // 80 = CUIT
                    docNro: long.Parse(txtCuit.Text.Trim()),
                    alicuotaIVA: 21m
                );
                
                DebugMessage($"CrearFacturaAfipAsync terminó: {resultado}");
                
                return resultado;
            }
            catch (Exception ex)
            {
                DebugMessage($"Error en CrearFacturaAAsync: {ex.Message}");
                MessageBox.Show($"🚨 Error en CrearFacturaAAsync:\n{ex.Message}", 
                    "Error CrearFacturaAAsync", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // CORREGIDO: ProcesarFacturaB con debug detallado
        private async Task ProcesarFacturaB()
        {
            try
            {
                DebugMessage("ProcesarFacturaB iniciado correctamente");
                
                OpcionSeleccionada = OpcionImpresion.FacturaB;
                string formaPago = OpcionPagoSeleccionada.ToString();
                
                DebugMessage($"Datos preparados: Opción={OpcionSeleccionada}, Pago={formaPago}, Importe=${importeTotalVenta}");
                
                DebugMessage("Llamando a CrearFacturaBAsync()...");
                
                bool exito = await CrearFacturaBAsync();
                
                DebugMessage($"CrearFacturaBAsync() terminó. Resultado: {exito}");
                
                if (exito)
                {
                    DebugMessage("Éxito = true, formateando número...");
                    
                    // Formatear el número de factura antes de enviarlo
                    string numeroFormateado = FormatearNumeroFactura(6, 1, NumeroFacturaAfip); // 6 = Factura B
                    
                    DebugMessage($"Número formateado: {numeroFormateado}");
                    
                    if (OnProcesarVenta != null)
                    {
                        DebugMessage("Llamando callback OnProcesarVenta...");
                        
                        // CORRECCIÓN: Pasar todos los parámetros incluyendo el número formateado
                        await OnProcesarVenta("FacturaB", formaPago, "", CAENumero, CAEVencimiento, NumeroFacturaAfip, numeroFormateado);
                        
                        DebugMessage("Callback terminado exitosamente");
                    }
                    else
                    {
                        DebugMessage("ERROR: OnProcesarVenta es NULL");
                    }

                    DebugMessage("Cerrando modal...");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("❌ Error creando Factura B en AFIP. Verifique su conexión y certificados.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            } 
            catch (Exception ex)
            {
                MessageBox.Show($"🚨 Excepción en ProcesarFacturaB:\n{ex.Message}\n\nStack:\n{ex.StackTrace}", 
                    "Error Detallado", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw; // Re-lanzar para que el handler del botón también la capture
            }
        }

        // CORREGIDO: CrearFacturaBAsync con debug detallado
        private async Task<bool> CrearFacturaBAsync()
        {
            try
            {
                DebugMessage("CrearFacturaBAsync iniciado");
                
                var resultado = await CrearFacturaAfipAsync(
                    cbteTipo: 6,
                    condicionIVAReceptor: 5,
                    docTipo: 99,
                    docNro: 0,
                    alicuotaIVA: 21m
                );
                
                DebugMessage($"CrearFacturaAfipAsync terminó: {resultado}");
                
                return resultado;
            }
            catch (Exception ex)
            {
                DebugMessage($"Error en CrearFacturaBAsync: {ex.Message}");
                MessageBox.Show($"🚨 Error en CrearFacturaBAsync:\n{ex.Message}", 
                    "Error CrearFacturaBAsync", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // CORREGIDO: CrearFacturaAfipAsync con manejo de errores específicos mejorado
        private async Task<bool> CrearFacturaAfipAsync(int cbteTipo, int condicionIVAReceptor, int docTipo, long docNro, decimal alicuotaIVA)
        {
            try
            {
                DebugMessage("=== INICIO CrearFacturaAfipAsync ===");
                
                string cuit = "20280694739";
                string pfxPath = @"C:\Certificados\certificado.pfx";
                string pfxPassword = "Micertificado";
                string wsaaUrl = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";

                DebugMessage("Configuración cargada - Verificando certificado...");

                // NUEVO: Verificar certificado antes de usarlo
                var (valido, mensaje, vence) = AfipAuthenticator.VerificarCertificado(pfxPath, pfxPassword);
                
                if (!valido)
                {
                    MessageBox.Show($"❌ Problema con el certificado AFIP:\n\n{mensaje}", "Error Certificado", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                
                if (mensaje.Contains("⚠️"))
                {
                    // Mostrar advertencia pero continuar
                    DebugMessage($"ADVERTENCIA: {mensaje}");
                }

                DebugMessage("Certificado verificado, obteniendo credenciales AFIP...");

                // CORREGIDO: Usar AfipAuthenticator.GetTAAsync con mejor manejo de errores
                (string token, string sign) = await AfipAuthenticator.GetTAAsync("wsfe", pfxPath, pfxPassword, wsaaUrl);

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(sign))
                {
                    MessageBox.Show("❌ Error obteniendo credenciales AFIP. Verifique su certificado y conexión.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // NUEVO: Detectar tokens placeholder
                if (token == "PLACEHOLDER_TOKEN")
                {
                    MessageBox.Show(
                        "⏳ AFIP indica que ya existe un token válido activo.\n\n" +
                        "💡 Esto es normal y significa que:\n" +
                        "• Ya se obtuvo un token anteriormente\n" +
                        "• El token anterior aún no ha expirado\n" +
                        "• AFIP no permite tokens duplicados\n\n" +
                        "🔄 El sistema reintentará automáticamente en unos minutos.\n" +
                        "Mientras tanto, puede intentar cerrar y reabrir la aplicación.", 
                        "Token AFIP Activo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                DebugMessage($"Token obtenido exitosamente. Length: {token.Length}");

                TokenAfip = token;
                SignAfip = sign;

                DebugMessage("Creando cliente WSFE...");

                // Continuar con WSFE
                var client = new ArcaWS.ServiceSoapClient(ArcaWS.ServiceSoapClient.EndpointConfiguration.ServiceSoap);

                var auth = new ArcaWS.FEAuthRequest
                {
                    Token = token,
                    Sign = sign,
                    Cuit = Convert.ToInt64(cuit)
                };

                DebugMessage("Probando conexión con FEDummy...");

                // Probar FEDummy primero
                var dummyResp = await client.FEDummyAsync();
                
                if (dummyResp?.Body?.FEDummyResult == null)
                {
                    MessageBox.Show("❌ Error de conectividad con WSFE de AFIP", "Error WSFE", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                DebugMessage($"WSFE operativo - AppServer: {dummyResp.Body.FEDummyResult.AppServer}");

                DebugMessage("Obteniendo último número autorizado...");

                // Si llegamos aquí, todo funciona - continuar con facturación real
                int ptoVta = 1;
                
                var ultimoResp = await client.FECompUltimoAutorizadoAsync(auth, ptoVta, cbteTipo);
                
                if (ultimoResp?.Body?.FECompUltimoAutorizadoResult == null)
                {
                    MessageBox.Show("❌ Error obteniendo último número de comprobante de AFIP", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                int ultimoNroAfip = ultimoResp.Body.FECompUltimoAutorizadoResult.CbteNro;
                int nuevoNroComprobante = ultimoNroAfip + 1;

                DebugMessage($"Último número AFIP: {ultimoNroAfip}, Nuevo: {nuevoNroComprobante}");

                decimal impTotal = ObtenerImporteTotalVenta();
                
                if (impTotal <= 0)
                {
                    MessageBox.Show("❌ El importe total debe ser mayor a cero", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                decimal impIVA = Math.Round(impTotal - (impTotal / (1 + (alicuotaIVA / 100m))), 2);
                decimal impNeto = Math.Round(impTotal - impIVA, 2);

                DebugMessage($"Cálculos - Total: {impTotal:C}, Neto: {impNeto:C}, IVA: {impIVA:C}");

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

                DebugMessage("Enviando solicitud CAE a AFIP...");

                var respuesta = await client.FECAESolicitarAsync(auth, feCAEReq);
                var resultado = respuesta?.Body?.FECAESolicitarResult;

                DebugMessage("Respuesta recibida, procesando...");

                if (resultado == null)
                {
                    MessageBox.Show("❌ Respuesta nula de AFIP. Intente nuevamente.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Verificar errores generales
                if (resultado.Errors != null && resultado.Errors.Length > 0)
                {
                    string errores = "❌ ERRORES GENERALES AFIP:\n\n";
                    foreach (var error in resultado.Errors)
                    {
                        errores += $"• Código {error.Code}: {error.Msg}\n";
                    }
                    MessageBox.Show(errores, "Error AFIP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                if (resultado.FeDetResp == null || resultado.FeDetResp.Length == 0)
                {
                    MessageBox.Show("❌ Sin respuesta de detalle de AFIP", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                var detalle = resultado.FeDetResp[0];
                DebugMessage($"Resultado AFIP: {detalle.Resultado}");
                
                if (detalle.Resultado == "A") // Aprobada
                {
                    CAENumero = detalle.CAE;
                    NumeroFacturaAfip = (int)detalle.CbteDesde;
                    
                    string numeroFacturaFormateado = FormatearNumeroFactura(cbteTipo, ptoVta, NumeroFacturaAfip);
                    
                    if (DateTime.TryParseExact(detalle.CAEFchVto, "yyyyMMdd", null, DateTimeStyles.None, out DateTime fechaVto))
                    {
                        CAEVencimiento = fechaVto;
                    }

                    DebugMessage($"FACTURA EXITOSA! CAE: {CAENumero}, Número: {numeroFacturaFormateado}");
                    MessageBox.Show($"🎉 Factura autorizada por AFIP\n\nCAE: {CAENumero}\nNúmero: {numeroFacturaFormateado}", 
                        "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
                else if (detalle.Resultado == "R") // Rechazada
                {
                    string mensajeRechazo = "❌ FACTURA RECHAZADA POR AFIP\n\n";
                    if (detalle.Observaciones != null && detalle.Observaciones.Length > 0)
                    {
                        foreach (var obs in detalle.Observaciones)
                        {
                            mensajeRechazo += $"• Código {obs.Code}: {obs.Msg}\n";
                        }
                    }
                    mensajeRechazo += "\n💡 Verifique los datos e intente nuevamente.";
                    MessageBox.Show(mensajeRechazo, "Rechazo AFIP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                else
                {
                    MessageBox.Show($"❌ Estado desconocido de AFIP: {detalle.Resultado}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            // CORREGIDO: Manejo de excepciones compatible con .NET 8
            catch (System.ServiceModel.CommunicationException ex)
            {
                DebugMessage($"Error de comunicación WCF: {ex.Message}");
                MessageBox.Show($"❌ Error de comunicación con AFIP:\n{ex.Message}\n\n💡 Verifique su conexión a internet.", "Error Comunicación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                DebugMessage($"Timeout en operación AFIP: {ex.Message}");
                MessageBox.Show($"❌ Tiempo de espera agotado con AFIP:\n{ex.Message}\n\n💡 Intente nuevamente.", "Timeout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (TimeoutException ex)
            {
                DebugMessage($"Timeout directo: {ex.Message}");
                MessageBox.Show($"❌ Tiempo de espera agotado:\n{ex.Message}\n\n💡 Intente nuevamente.", "Timeout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                DebugMessage($"Error HTTP: {ex.Message}");
                MessageBox.Show($"❌ Error de conectividad HTTP:\n{ex.Message}\n\n💡 Verifique su conexión a internet.", "Error HTTP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                DebugMessage($"ERROR GENERAL: {ex.GetType().Name} - {ex.Message}");
                MessageBox.Show($"❌ Error inesperado: {ex.GetType().Name}\n{ex.Message}", "Error General", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
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
                // Agregar más tipos según necesite
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
            // CORREGIDO: Declarar tipos explícitamente
            (string tokenPadron, string signPadron) = await GetTokenAndSignAsync("ws_sr_padron_a5");

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
                // CORREGIDO: Declarar tipos explícitamente
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
                    NumeroComprobante = numeroParaTicket
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
