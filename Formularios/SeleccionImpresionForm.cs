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

            // Opciones de impressão - CAMBIAR LOS EVENT HANDLERS
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

        // NUEVO: Método para debug del estado del caché
        private void DebugCacheStatus()
        {
            if (debugMode)
            {
                bool cacheValido = !string.IsNullOrEmpty(_cachedTokenWsfe) && 
                                  !string.IsNullOrEmpty(_cachedSignWsfe) && 
                                  _cachedTokenExpiryWsfe > DateTime.UtcNow.AddMinutes(5);

                DebugMessage($"=== ESTADO CACHE TOKENS ===");
                DebugMessage($"Token exists: {!string.IsNullOrEmpty(_cachedTokenWsfe)}");
                DebugMessage($"Sign exists: {!string.IsNullOrEmpty(_cachedSignWsfe)}");
                DebugMessage($"Expiry time: {_cachedTokenExpiryWsfe}");
                DebugMessage($"Current time: {DateTime.UtcNow}");
                DebugMessage($"Cache valid: {cacheValido}");
                DebugMessage($"Minutes until expiry: {(_cachedTokenExpiryWsfe - DateTime.UtcNow).TotalMinutes:F1}");
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
                DebugMessage($"{ex.Message} - {ex.GetType().Name}");
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

        // NUEVA: Excepción personalizada para tokens que ya existen
        public class TokenAlreadyExistsException : Exception
        {
            public string FaultString { get; }

            public TokenAlreadyExistsException(string message, string faultString) : base(message)
            {
                FaultString = faultString;
            }
        }

        // SIMPLIFICADO: Sistema de tokens sin intervención del usuario
        private async Task<bool> CrearFacturaAfipAsync(int cbteTipo, int condicionIVAReceptor, int docTipo, long docNro, decimal alicuotaIVA)
        {
            try
            {
                DebugMessage("=== INICIO CrearFacturaAfipAsync ===");
                DebugCacheStatus();

                string cuit = "20280694739";
                string pfxPath = @"C:\Certificados\certificado.pfx";
                string pfxPassword = "Micertificado";
                string wsaaUrl = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";

                // Verificar certificado antes de usarlo
                var (valido, mensaje, vence) = AfipAuthenticator.VerificarCertificado(pfxPath, pfxPassword);
                if (!valido)
                {
                    MessageBox.Show($"❌ Problema con el certificado AFIP:\n\n{mensaje}", "Error Certificado", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                string token = null, sign = null;
                DateTime expiracion = DateTime.MinValue;

                // 1. Verificar caché local
                if (!string.IsNullOrEmpty(_cachedTokenWsfe) && !string.IsNullOrEmpty(_cachedSignWsfe) && _cachedTokenExpiryWsfe > DateTime.UtcNow.AddMinutes(5))
                {
                    token = _cachedTokenWsfe;
                    sign = _cachedSignWsfe;
                    expiracion = _cachedTokenExpiryWsfe;
                    DebugMessage("✅ Usando token del caché local válido");
                }
                // 2. Verificar caché de AfipAuthenticator
                else if (AfipAuthenticator.GetExistingToken("wsfe") is { } tokenExistente && 
                         !string.IsNullOrEmpty(tokenExistente.token) && !string.IsNullOrEmpty(tokenExistente.sign))
                {
                    token = tokenExistente.token;
                    sign = tokenExistente.sign;
                    DebugMessage("✅ Token encontrado en caché de AfipAuthenticator");
                    _cachedTokenWsfe = token;
                    _cachedSignWsfe = sign;
                    // No actualices expiración aquí porque no la tienes real
                }
                // 3. Verificar archivo persistente
                else
                {
                    var (tokenPersistente, signPersistente, expiracionPersistente) = CargarTokenPersistente();
                    if (!string.IsNullOrEmpty(tokenPersistente) && !string.IsNullOrEmpty(signPersistente) && expiracionPersistente > DateTime.UtcNow.AddMinutes(5))
                    {
                        token = tokenPersistente;
                        sign = signPersistente;
                        expiracion = expiracionPersistente;
                        DebugMessage("✅ Token válido encontrado en caché persistente");
                        _cachedTokenWsfe = token;
                        _cachedSignWsfe = sign;
                        _cachedTokenExpiryWsfe = expiracion;
                    }
                    else
                    {
                        // 4. Obtener uno nuevo automáticamente de AFIP
                        DebugMessage("⏳ No hay token válido, obteniendo uno nuevo de AFIP automáticamente...");
                        var (nuevoToken, nuevoSign, nuevaExpiracion) = await AfipAuthenticator.GetTAAsync("wsfe", pfxPath, pfxPassword, wsaaUrl);
                        if (!string.IsNullOrEmpty(nuevoToken) && !string.IsNullOrEmpty(nuevoSign))
                        {
                            token = nuevoToken;
                            sign = nuevoSign;
                            expiracion = nuevaExpiracion;
                            ActualizarTodosLosCaches(token, sign, expiracion);
                            GuardarTokenPersistente(token, sign, expiracion);
                            DebugMessage("✅ Nuevo token obtenido y guardado en caché");
                        }
                        else
                        {
                            MessageBox.Show("❌ No se pudo obtener un nuevo token de AFIP.", "Error Token AFIP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                }

                // Actualizar propiedades de instancia
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

                DebugMessage($"✅ WSFE operativo - AppServer: {dummyResp.Body.FEDummyResult.AppServer}");

                DebugMessage("Obteniendo último número autorizado...");

                int ptoVta = 1;
                var ultimoResp = await client.FECompUltimoAutorizadoAsync(auth, ptoVta, cbteTipo);

                if (ultimoResp?.Body?.FECompUltimoAutorizadoResult == null)
                {
                    MessageBox.Show("❌ Error obteniendo último número de comprobante de AFIP", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                int ultimoNroAfip = ultimoResp.Body.FECompUltimoAutorizadoResult.CbteNro;
                int nuevoNroComprobante = ultimoNroAfip + 1;

                DebugMessage($"✅ Último número AFIP: {ultimoNroAfip}, Nuevo: {nuevoNroComprobante}");

                // Obtener productos de la venta actual con sus IVAs específicos
                var productosVenta = await ObtenerProductosVentaConIva();

                if (productosVenta == null || productosVenta.Count == 0)
                {
                    MessageBox.Show("❌ No hay productos en la venta para facturar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                decimal impTotal = ObtenerImporteTotalVenta();

                if (impTotal <= 0)
                {
                    MessageBox.Show("❌ El importe total debe ser mayor a cero", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Calcular IVA agrupado por alícuota
                var ivasAgrupados = CalcularIvasPorAlicuota(productosVenta);

                DebugMessage($"Total productos: {productosVenta.Count}, IVAs agrupados: {ivasAgrupados.Count}");

                // Validar que no haya alícuotas duplicadas antes de crear el array
                var alicuotasIva = new List<ArcaWS.AlicIva>();
                var idsUtilizados = new HashSet<int>();

                foreach (var ivaGroup in ivasAgrupados)
                {
                    int codigoAfip = ObtenerCodigoAlicuotaAfip(ivaGroup.Key);

                    // Verificar que no se repita el código AFIP
                    if (idsUtilizados.Contains(codigoAfip))
                    {
                        DebugMessage($"⚠️ ADVERTENCIA: Se detectó código AFIP duplicado: {codigoAfip} para alícuota {ivaGroup.Key}%");
                        MessageBox.Show($"❌ Error interno: Se detectó una alícuota duplicada.\nCódigo AFIP: {codigoAfip}\nAlícuota: {ivaGroup.Key}%",
                            "Error AlicIVA", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    idsUtilizados.Add(codigoAfip);

                    var alicuota = new ArcaWS.AlicIva
                    {
                        Id = codigoAfip,
                        BaseImp = (double)ivaGroup.Value.BaseImponible,
                        Importe = (double)ivaGroup.Value.ImporteIva
                    };
                    alicuotasIva.Add(alicuota);

                    DebugMessage($"✅ Alícuota {ivaGroup.Key}% → Código AFIP: {codigoAfip} - Base: ${alicuota.BaseImp:C} - IVA: ${alicuota.Importe:C}");
                }

                // Validar totales antes de enviar
                decimal totalBaseCalculada = alicuotasIva.Sum(a => (decimal)a.BaseImp);
                decimal totalIvaCalculado = alicuotasIva.Sum(a => (decimal)a.Importe);
                decimal totalCalculado = totalBaseCalculada + totalIvaCalculado;

                DebugMessage($"VALIDACIÓN TOTALES:");
                DebugMessage($"  Base calculada: ${totalBaseCalculada}");
                DebugMessage($"  IVA calculado: ${totalIvaCalculado}");
                DebugMessage($"  Total calculado: ${totalCalculado}");
                DebugMessage($"  Total venta: ${impTotal}");
                DebugMessage($"  Diferencia: ${Math.Abs(totalCalculado - impTotal)}");

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
                    ImpNeto = (double)totalBaseCalculada,
                    ImpIVA = (double)totalIvaCalculado,
                    MonId = "PES",
                    MonCotiz = 1,
                    CondicionIVAReceptorId = condicionIVAReceptor,
                    ImpTrib = 0,
                    ImpOpEx = 0,
                    Iva = alicuotasIva.ToArray()
                };

                var feCAEReq = new ArcaWS.FECAERequest
                {
                    FeCabReq = feCabReq,
                    FeDetReq = new ArcaWS.FECAEDetRequest[] { feDetReq }
                };

                DebugMessage("=== ESTRUCTURA ENVIADA A AFIP ===");
                DebugMessage($"Cantidad de alícuotas: {alicuotasIva.Count}");
                foreach (var alicuota in alicuotasIva)
                {
                    DebugMessage($"  ID: {alicuota.Id}, Base: {alicuota.BaseImp}, Importe: {alicuota.Importe}");
                }

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
                        DebugMessage($"ERROR AFIP: {error.Code} - {error.Msg}");
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

                    DebugMessage($"🎉 FACTURA EXITOSA! CAE: {CAENumero}, Número: {numeroFacturaFormateado}");
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
                            DebugMessage($"OBSERVACIÓN AFIP: {obs.Code} - {obs.Msg}");
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

        // NUEVOS MÉTODOS DE SOPORTE

        private enum EstrategiaToken
        {
            Esperar,
            ReiniciarAplicacion,
            Cancelar
        }

        private EstrategiaToken MostrarOpcionesToken()
        {
            using (var form = new Form())
            {
                form.Text = "Token AFIP Activo";
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.StartPosition = FormStartPosition.CenterParent;
                form.Size = new System.Drawing.Size(500, 300);

                var label = new Label
                {
                    Text = "⏳ AFIP detectó un token activo previo\n\n" +
                           "💡 El sistema de facturación electrónica mantiene tokens activos\n" +
                           "por seguridad. Esto es normal al generar facturas consecutivas.\n\n" +
                           "🔄 Seleccione una opción:",
                    Location = new System.Drawing.Point(20, 20),
                    Size = new System.Drawing.Size(440, 120),
                    Font = new System.Drawing.Font("Segoe UI", 9)
                };

                var btnEsperar = new Button
                {
                    Text = "⏱️ Esperar 15 segundos e intentar",
                    Location = new System.Drawing.Point(20, 150),
                    Size = new System.Drawing.Size(200, 35),
                    DialogResult = DialogResult.Retry
                };

                var btnReiniciar = new Button
                {
                    Text = "🔄 Reiniciar aplicación",
                    Location = new System.Drawing.Point(240, 150),
                    Size = new System.Drawing.Size(150, 35),
                    DialogResult = DialogResult.Abort
                };

                var btnCancelar = new Button
                {
                    Text = "❌ Cancelar",
                    Location = new System.Drawing.Point(400, 150),
                    Size = new System.Drawing.Size(70, 35),
                    DialogResult = DialogResult.Cancel
                };

                form.Controls.AddRange(new Control[] { label, btnEsperar, btnReiniciar, btnCancelar });

                var result = form.ShowDialog();
                return result switch
                {
                    DialogResult.Retry => EstrategiaToken.Esperar,
                    DialogResult.Abort => EstrategiaToken.ReiniciarAplicacion,
                    _ => EstrategiaToken.Cancelar
                };
            }
        }

        private void LimpiarTodosLosCaches()
        {
            // Limpiar caché local
            _cachedTokenWsfe = null;
            _cachedSignWsfe = null;
            _cachedTokenExpiryWsfe = DateTime.MinValue;
            
            // Limpiar caché de AfipAuthenticator
            AfipAuthenticator.ClearTokenCache("wsfe");
            
            // Limpiar archivo persistente
            EliminarTokenPersistente();
            
            DebugMessage("Todos los cachés limpiados");
        }

        private void ActualizarTodosLosCaches(string token, string sign, DateTime expiracion)
        {
            _cachedTokenWsfe = token;
            _cachedSignWsfe = sign;
            _cachedTokenExpiryWsfe = expiracion;
            
            DebugMessage("Todos los cachés actualizados");
        }

        private static readonly string TokenCacheFile = Path.Combine(Path.GetTempPath(), "afip_token_cache.dat");

        private (string token, string sign, DateTime expiration) CargarTokenPersistente()
        {
            try
            {
                if (!File.Exists(TokenCacheFile))
                    return (null, null, DateTime.MinValue);

                var lines = File.ReadAllLines(TokenCacheFile);
                if (lines.Length >= 3)
                {
                    string token = lines[0];
                    string sign = lines[1];
                    if (DateTime.TryParse(lines[2], out DateTime expiration))
                    {
                        DebugMessage($"Token persistente cargado - Expira: {expiration}");
                        return (token, sign, expiration);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugMessage($"Error cargando token persistente: {ex.Message}");
            }
            
            return (null, null, DateTime.MinValue);
        }

        private void GuardarTokenPersistente(string token, string sign, DateTime expiration)
        {
            try
            {
                File.WriteAllLines(TokenCacheFile, new[] { token, sign, expiration.ToString("O") });
                DebugMessage($"Token persistente guardado - Expira: {expiration}");
            }
            catch (Exception ex)
            {
                DebugMessage($"Error guardando token persistente: {ex.Message}");
            }
        }

        private void EliminarTokenPersistente()
        {
            try
            {
                if (File.Exists(TokenCacheFile))
                {
                    File.Delete(TokenCacheFile);
                    DebugMessage("Token persistente eliminado");
                }
            }
            catch (Exception ex)
            {
                DebugMessage($"Error eliminando token persistente: {ex.Message}");
            }
        }

        // CORREGIDO: Método de formateo simple y claro
        private string FormatearNumeroFactura(int cbteTipo, int ptoVta, int numeroFactura)
        {
            DebugMessage($"=== FORMATEANDO NÚMERO FACTURA ===");
            DebugMessage($"Tipo comprobante (cbteTipo): {cbteTipo}");
            DebugMessage($"Punto de venta (ptoVta): {ptoVta}");
            DebugMessage($"Número factura (numeroFactura): {numeroFactura}");
            
            // Obtener la letra según el tipo de comprobante
            string letra = ObtenerLetraComprobante(cbteTipo);
            DebugMessage($"Letra obtenida: {letra}");

            // FORMATO ESTÁNDAR AFIP: Letra + Espacio + PtoVenta(4) + Guión + Número(8)
            // Ejemplo: "A 0001-00000123" o "B 0001-00000456"
            string numeroFormateado = $"{letra} {ptoVta:D4}-{numeroFactura:D8}";
            DebugMessage($"Número final formateado: {numeroFormateado}");
            DebugMessage($"=== FIN FORMATEO ===");
            
            return numeroFormateado;
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
            (var tokenPadron, var signPadron, _) = await GetTokenAndSignAsync("ws_sr_padron_a5");

            if (!string.IsNullOrEmpty(tokenPadron) && !string.IsNullOrEmpty(signPadron))
            {
                TokenAfip = tokenPadron;
                SignAfip = signPadron;
                return true;
            }
            return false;
        }

        private async Task<(string token, string sign, DateTime expiration)> GetTokenAndSignAsync(string service)
        {
            string pfxPath = @"C:\Certificados\certificado.pfx";
            string pfxPassword = "Micertificado";
            string wsaaUrl = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";
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
                var (token, sign, expiration) = await AfipAuthenticator.GetTAAsync(service, pfxPath, pfxPassword, wsaaUrl);

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

        // CORREGIDO: Método de impresión con lógica correcta para tipos de factura
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

                string numeroParaTicket;
                if (tipoComprobante == "REMITO")
                {
                    numeroParaTicket = formularioPadre?.GetNroRemitoActual().ToString() ?? numeroComprobante;
                    DebugMessage($"[Impresión] Remito - númeroParaTicket: {numeroParaTicket}");
                }
                else
                {
                    if (NumeroFacturaAfip > 0)
                    {
                        int cbteTipo;
                        string leyenda = "";
                        if (tipoComprobante == "FacturaA")
                        {
                            cbteTipo = 1;
                            leyenda = "Factura A N° ";
                        }
                        else if (tipoComprobante == "FacturaB")
                        {
                            cbteTipo = 6;
                            leyenda = "Factura B N° ";
                        }
                        else
                        {
                            cbteTipo = 6;
                            leyenda = "Factura N° ";
                        }

                        string numeroFormateado = FormatearNumeroFactura(cbteTipo, 1, NumeroFacturaAfip);
                        DebugMessage($"[Impresión] Antes de manipular: leyenda='{leyenda}', numeroFormateado='{numeroFormateado}'");

                        // Quitar la letra y el espacio inicial si lo deseas
                        if (numeroFormateado.Length > 2 && (numeroFormateado[0] == 'A' || numeroFormateado[0] == 'B' || numeroFormateado[0] == 'C' || numeroFormateado[0] == 'M'))
                            numeroFormateado = numeroFormateado.Substring(2);

                        DebugMessage($"[Impresión] Después de manipular: numeroFormateado='{numeroFormateado}'");

                        numeroParaTicket = $"{leyenda}{numeroFormateado}";
                        DebugMessage($"[Impresión] Final numeroParaTicket: '{numeroParaTicket}'");
                    }
                    else
                    {
                        numeroParaTicket = numeroComprobante;
                        DebugMessage($"[Impresión] Sin NumeroFacturaAfip, numeroParaTicket: '{numeroParaTicket}'");
                    }
                }

                var config = new TicketConfig
                {
                    NombreComercio = formularioPadre?.GetNombreComercio() ?? "Tu Comercio",
                    DomicilioComercio = formularioPadre?.GetDomicilioComercio() ?? "Tu Domicilio",
                    TipoComprobante = tipoComprobante,
                    NumeroComprobante = numeroParaTicket
                };

                if (tipoComprobante.Contains("Factura"))
                {
                    config.CAE = CAENumero;
                    config.CAEVencimiento = CAEVencimiento;
                    if (tipoComprobante == "FacturaA")
                    {
                        config.CUIT = txtCuit.Text.Trim();
                    }
                }

                DebugMessage($"[Impresión] Llamando a ImprimirTicket con NumeroComprobante: '{config.NumeroComprobante}'");

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

        // NUEVO: Método para limpiar caché de tokens manualmente
        public static void LimpiarCacheTokens()
        {
            _cachedTokenWsfe = null;
            _cachedSignWsfe = null;
            _cachedTokenExpiryWsfe = DateTime.MinValue;
            
            // También limpiar el caché del AfipAuthenticator
            AfipAuthenticator.ClearTokenCache("wsfe");
            
            System.Diagnostics.Debug.WriteLine("[CACHE] Caché de tokens limpiado manualmente");
        }

        // NUEVO: Método para obtener productos de la venta con sus IVAs
        private async Task<List<ProductoVenta>> ObtenerProductosVentaConIva()
        {
            try
            {
                var productos = new List<ProductoVenta>();
                DataTable ventaActual = formularioPadre?.GetRemitoActual();
                
                if (ventaActual == null) return productos;

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    foreach (DataRow row in ventaActual.Rows)
                    {
                        string codigo = row["codigo"]?.ToString();
                        if (string.IsNullOrEmpty(codigo)) continue;

                        string query = "SELECT iva FROM Productos WHERE codigo = @codigo";
                        using (var cmd = new SqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@codigo", codigo);
                            var result = await cmd.ExecuteScalarAsync();
                            
                            decimal iva = result != null && decimal.TryParse(result.ToString(), out decimal ivaValue) 
                                ? ivaValue 
                                : 21.00m; // Default

                            productos.Add(new ProductoVenta
                            {
                                Codigo = codigo,
                                Descripcion = row["descripcion"]?.ToString() ?? "",
                                Precio = decimal.TryParse(row["precio"]?.ToString(), out decimal precio) ? precio : 0,
                                Cantidad = int.TryParse(row["cantidad"]?.ToString(), out int cantidad) ? cantidad : 0,
                                Subtotal = decimal.TryParse(row["total"]?.ToString(), out decimal total) ? total : 0,
                                IVA = iva
                            });
                        }
                    }
                }

                return productos;
            }
            catch (Exception ex)
            {
                DebugMessage($"Error obteniendo productos con IVA: {ex.Message}");
                return new List<ProductoVenta>();
            }
        }

        // CORRECCIÓN: Método para calcular IVAs agrupados por alícuota con validación mejorada
        private Dictionary<decimal, (decimal BaseImponible, decimal ImporteIva)> CalcularIvasPorAlicuota(List<ProductoVenta> productos)
        {
            var ivasAgrupados = new Dictionary<decimal, (decimal BaseImponible, decimal ImporteIva)>();

            DebugMessage("=== INICIANDO CÁLCULO DE IVAs ===");

            foreach (var producto in productos)
            {
                // Calcular base imponible e IVA para este producto
                decimal subtotal = producto.Subtotal;
                decimal baseImponible = Math.Round(subtotal / (1 + (producto.IVA / 100m)), 2);
                decimal importeIva = Math.Round(subtotal - baseImponible, 2);

                DebugMessage($"Producto: {producto.Codigo} - IVA: {producto.IVA}% - Subtotal: ${subtotal} - Base: ${baseImponible} - IVA: ${importeIva}");

                if (ivasAgrupados.ContainsKey(producto.IVA))
                {
                    var actual = ivasAgrupados[producto.IVA];
                    ivasAgrupados[producto.IVA] = (
                        actual.BaseImponible + baseImponible,
                        actual.ImporteIva + importeIva
                    );
                    DebugMessage($"AGREGADO a alícuota existente {producto.IVA}% - Nueva Base: ${ivasAgrupados[producto.IVA].BaseImponible} - Nuevo IVA: ${ivasAgrupados[producto.IVA].ImporteIva}");
                }
                else
                {
                    ivasAgrupados[producto.IVA] = (baseImponible,importeIva);
                    DebugMessage($"NUEVA alícuota {producto.IVA}% - Base: ${baseImponible} - IVA: ${importeIva}");
                }
            }

            DebugMessage($"=== RESULTADO FINAL: {ivasAgrupados.Count} alícuotas únicas ===");

            return ivasAgrupados;
        }

        // CORRECCIÓN: Método sin referencias al 6,63% que ya fue corregido en la base de datos
        private int ObtenerCodigoAlicuotaAfip(decimal porcentajeIva)
        {
            DebugMessage($"Obteniendo código AFIP para alícuota: {porcentajeIva}%");
            
            decimal porcentajeNormalizado = Math.Round(porcentajeIva, 2);
            
            int codigo = porcentajeNormalizado switch
            {
                0m => 3,       // No gravado
                2.5m => 9,     // 2.5%
                5m => 8,       // 5%
                10.5m => 4,    // 10.5%
                21m => 5,      // 21%
                27m => 6,      // 27%
                
                _ => throw new Exception($"⚠️ ALÍCUOTA NO SOPORTADA: {porcentajeNormalizado}%\n\n" +
                    "Alícuotas válidas de AFIP:\n" +
                    "• 0% (Código 3 - No gravado)\n" +
                    "• 2.5% (Código 9)\n" +
                    "• 5% (Código 8)\n" +
                    "• 10.5% (Código 4)\n" +
                    "• 21% (Código 5)\n" +
                    "• 27% (Código 6)\n\n" +
                    "Verifique la configuración del producto en la base de datos.")
            };
            
            DebugMessage($"Alícuota {porcentajeNormalizado}% → Código AFIP: {codigo}");
            return codigo;
        }

        // NUEVO: Clase para representar productos en la venta
        public class ProductoVenta
        {
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public decimal Precio { get; set; }
            public int Cantidad { get; set; }
            public decimal Subtotal { get; set; }
            public decimal IVA { get; set; }
        }
    }
}
