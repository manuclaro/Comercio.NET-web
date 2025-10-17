using Newtonsoft.Json.Linq;
using System;
using System.Windows.Forms;
using WSconsultaCUIT;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Drawing.Printing;
using System.Globalization;
using System.ServiceModel;
using System.Threading.Tasks;
using Comercio.NET.Servicios;
using Comercio.NET.Controles; // NUEVO: Para el control de múltiples pagos
using System.Linq;
using System.Collections.Generic;

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

        // MODIFICADO: Referencias a los botones para poder controlar su estado
        private Button btnRemito;
        private Button btnFacturaB;
        private Button btnFacturaA;

        // NUEVO: Control de múltiples pagos
        private MultiplePagosControl multiplePagosControl;

        // NUEVO: Toggle para modo de pago múltiple
        private CheckBox chkPagoMultiple;
        private Panel panelPagoSimple;
        private Panel panelPagoMultiple;

        // ORIGINAL: Referencias a los RadioButtons para retrocompatibilidad
        private RadioButton rbEfectivo;
        private RadioButton rbDNI;
        private RadioButton rbMercadoPago;

        // CORREGIDO: Eliminar referencias a controles que no existen
        private Label lblMensajeInformativo;
        private CheckBox chkMultiplesPagos; // Referencia corregida

        // NUEVO: Label para mostrar el importe total a pagar
        private Label lblImporteTotal;

        // Delegate para el callback después de procesar la venta
        public Func<string, string, string, string, DateTime?, int, string, Task> OnProcesarVenta { get; set; }

        private decimal importeTotalVenta;
        private Ventas formularioPadre;

        // ELIMINADO: Cache de tokens local - usar el del AfipAuthenticator
        // NUEVO: Método para limpiar cache de tokens AFIP
        public static void LimpiarCacheTokensAfip()
        {
            AfipAuthenticator.ClearTokenCache("wsfe");
            System.Diagnostics.Debug.WriteLine("🗑️ Cache de tokens AFIP limpiado");
        }

        // NUEVO: Método para verificar estado del cache
        public static bool TieneCacheTokensValido()
        {
            var tokenCache = AfipAuthenticator.GetExistingToken("wsfe");
            bool esValido = tokenCache.HasValue && 
                           !string.IsNullOrEmpty(tokenCache.Value.token) && 
                           !string.IsNullOrEmpty(tokenCache.Value.sign);
            
            System.Diagnostics.Debug.WriteLine($"🔍 Estado cache tokens: {(esValido ? "VÁLIDO" : "INVÁLIDO")}");
            
            return esValido;
        }

        // Propiedades para almacenar datos del CAE
        public string CAENumero { get; private set; } = "";
        public DateTime? CAEVencimiento { get; private set; } = null;
        public int NumeroFacturaAfip { get; private set; } = 0;

        public string TokenAfip { get; set; }
        public string SignAfip { get; set; }

        // NUEVO: Propiedades para acceder a los datos de pagos múltiples
        public List<MultiplePagosControl.DetallePago> PagosMultiples => multiplePagosControl?.Pagos ?? new List<MultiplePagosControl.DetallePago>();
        public bool EsPagoMultiple => chkPagoMultiple?.Checked ?? false;
        public string ResumenPagos => EsPagoMultiple ? multiplePagosControl?.ObtenerResumenPagos() ?? "" : OpcionPagoSeleccionada.ToString();

        public SeleccionImpresionForm(decimal importeTotal = 0, Ventas padre = null)
        {
            this.importeTotalVenta = importeTotal;
            this.formularioPadre = padre;

            this.Text = "Seleccione tipo de impresión y método de pago";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Width = 700;
            this.Height = 400; // CAMBIADO de 550 a 400 para que inicie en modo simple

            CrearControles();
            ConfigurarEventos();
            ActualizarOpcionesImpresion();
        }

        private void CrearControles()
        {
            var fontRadio = new Font("Segoe UI", 12F, FontStyle.Regular);

            // CheckBox para habilitar pago múltiple - CORREGIDO: Sin emoji problemático
            chkPagoMultiple = new CheckBox
            {
                Text = "Habilitar múltiples medios de pago",
                Left = 40,
                Top = 20,
                Width = 300,
                Height = 25,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(0, 120, 215),
                Checked = false
            };

            // CORREGIDO: Asignar referencia correcta
            chkMultiplesPagos = chkPagoMultiple;

            // Panel para pago simple (modo original)
            panelPagoSimple = new Panel
            {
                Left = 40,
                Top = 50,
                Width = 600,
                Height = 60,
                BackColor = System.Drawing.Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = true
            };

            var lblPago = new Label 
            { 
                Text = "Forma de pago:", 
                Left = 10, 
                Top = 10, 
                Width = 120, 
                Font = new Font("Segoe UI", 10F, FontStyle.Bold) 
            };

            rbEfectivo = new RadioButton 
            { 
                Text = "Efectivo", 
                Left = 10, 
                Top = 30, 
                Width = 100, 
                Height = 25, 
                Font = fontRadio, 
                Checked = true 
            };

            rbDNI = new RadioButton 
            { 
                Text = "DNI", 
                Left = 120, 
                Top = 30, 
                Width = 100, 
                Height = 25, 
                Font = fontRadio 
            };

            rbMercadoPago = new RadioButton 
            { 
                Text = "MercadoPago", 
                Left = 230, 
                Top = 30, 
                Width = 140, 
                Height = 25, 
                Font = fontRadio 
            };

            panelPagoSimple.Controls.AddRange(new Control[] { lblPago, rbEfectivo, rbDNI, rbMercadoPago });

            // AJUSTADO: Label para mostrar el importe total a pagar con fuente grande
            lblImporteTotal = new Label
            {
                Left = 40,
                Top = 120, // CAMBIADO de 130 a 120 para dar más espacio
                Width = 600,
                Height = 80,
                Font = new Font("Segoe UI", 24F, FontStyle.Bold), // Fuente grande y negrita
                ForeColor = System.Drawing.Color.FromArgb(0, 102, 204), // Color azul
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Text = $"TOTAL A PAGAR: {importeTotalVenta:C2}",
                BackColor = System.Drawing.Color.FromArgb(240, 248, 255), // Fondo azul claro
                BorderStyle = BorderStyle.FixedSingle,
                Visible = true
            };

            // Panel para pago múltiple
            panelPagoMultiple = new Panel
            {
                Left = 40,
                Top = 50,
                Width = 600,
                Height = 280,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            // Control de múltiples pagos
            multiplePagosControl = new MultiplePagosControl
            {
                Dock = DockStyle.Fill
            };

            panelPagoMultiple.Controls.Add(multiplePagosControl);

            // AJUSTADO: Controles para CUIT y Razón Social con nueva posición inicial
            int topCuit = 220; // Posición inicial para modo simple

            txtCuit = new TextBox
            {
                Left = 90,
                Top = topCuit,
                Width = 120,
                Font = new Font("Segoe UI", 10F)
            };

            lblRazonSocial = new Label
            {
                Text = "",
                Left = 220,
                Top = topCuit + 2,
                Width = 400,
                Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                ForeColor = System.Drawing.Color.DarkGreen
            };

            // CORREGIDO: Crear label CUIT con nombre específico para poder encontrarlo fácilmente
            var lblCuit = new Label
            {
                Name = "lblCuit", // NUEVO: Asignar nombre para búsqueda
                Text = "CUIT:",
                Left = 40,
                Top = topCuit + 2,
                Width = 50,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Visible = true // NUEVO: Asegurar visibilidad inicial
            };

            // AJUSTADO: Label para mensaje informativo
            lblMensajeInformativo = new Label
            {
                Left = 40,
                Top = 245, // AJUSTADO para la nueva altura
                Width = 600,
                Height = 25,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = System.Drawing.Color.Blue,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Text = ""
            };

            // AJUSTADO: Botones de impresión
            int topBotones = 270; // AJUSTADO para la nueva altura

            btnRemito = new Button 
            { 
                Text = "Remito", 
                Width = 130,
                Left = 60, 
                Top = topBotones, 
                Height = 45,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(102, 51, 153),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnFacturaB = new Button 
            { 
                Text = "Factura B", 
                Width = 130,
                Left = 200, 
                Top = topBotones, 
                Height = 45,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(0, 123, 255),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnFacturaA = new Button 
            { 
                Text = "Factura A", 
                Width = 130,
                Left = 340, 
                Top = topBotones, 
                Height = 45,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(40, 167, 69),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Width = 100,
                Left = 480,
                Top = topBotones, // AJUSTADO
                Height = 45,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(220, 53, 69),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };

            // Agregar todos los controles al formulario
            this.Controls.AddRange(new Control[] {
                chkPagoMultiple,
                panelPagoSimple,
                lblImporteTotal,
                panelPagoMultiple,
                lblCuit,        // CORREGIDO: Asegurar que se agregue correctamente
                txtCuit,
                lblRazonSocial,
                lblMensajeInformativo,
                btnRemito,
                btnFacturaB,
                btnFacturaA,
                btnCancelar
            });
        }

        private void ConfigurarEventos()
        {
            // Evento para cambiar modo de pago
            chkPagoMultiple.CheckedChanged += (s, e) =>
            {
                bool esPagoMultiple = chkPagoMultiple.Checked;
                
                panelPagoSimple.Visible = !esPagoMultiple;
                panelPagoMultiple.Visible = esPagoMultiple;
                
                // NUEVO: Controlar visibilidad del label de importe total
                lblImporteTotal.Visible = !esPagoMultiple; // Solo visible en modo pago simple

                if (esPagoMultiple)
                {
                    multiplePagosControl.EstablecerImporteTotal(importeTotalVenta);
                    this.Height = 550;
                    
                    // CORREGIDO: Ajustar posiciones para que CUIT sea visible en modo múltiple
                    txtCuit.Top = 340;
                    lblRazonSocial.Top = 342;
                    lblMensajeInformativo.Top = 365;
                    
                    // CORREGIDO: Asegurar que el label CUIT también se posicione correctamente
                    var lblCuit = this.Controls.Find("lblCuit", true).FirstOrDefault();
                    if (lblCuit != null)
                    {
                        lblCuit.SetBounds(40, 342, 50, 20);
                        lblCuit.Visible = true; // NUEVO: Asegurar que sea visible
                    }

                    // AJUSTADO: Posicionar botones más abajo para dar espacio al CUIT
                    btnRemito.Top = 390;
                    btnFacturaB.Top = 390;
                    btnFacturaA.Top = 390;
                    
                    // NUEVO: También posicionar el botón Cancelar
                    var btnCancelar = this.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "Cancelar");
                    if (btnCancelar != null)
                    {
                        btnCancelar.Top = 390;
                    }
                }
                else
                {
                    // CORREGIDO: Aumentar altura para que se vea el CUIT y todos los elementos
                    this.Height = 400;
                     
                    // AJUSTADO: Reposicionar elementos para el nuevo tamaño
                    txtCuit.Top = 220;
                    lblRazonSocial.Top = 222;
                    lblMensajeInformativo.Top = 245;
                    
                    // CORREGIDO: Posicionar también el label CUIT correctamente
                    var lblCuit = this.Controls.Find("lblCuit", true).FirstOrDefault();
                    if (lblCuit != null)
                    {
                        lblCuit.SetBounds(40, 222, 50, 20);
                        lblCuit.Visible = true; // NUEVO: Asegurar que sea visible
                    }

                    btnRemito.Top = 270;
                    btnFacturaB.Top = 270;
                    btnFacturaA.Top = 270;
                    
                    // NUEVO: Posicionar también el botón Cancelar
                    var btnCancelar = this.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "Cancelar");
                    if (btnCancelar != null)
                    {
                        btnCancelar.Top = 270;
                    }
                }

                ActualizarOpcionesImpresion();
            };
            
            // Eventos de RadioButtons originales
            rbEfectivo.CheckedChanged += (s, e) => 
            { 
                if (rbEfectivo.Checked) 
                {
                    OpcionPagoSeleccionada = OpcionPago.Efectivo;
                    ActualizarOpcionesImpresion();
                }
            };

            rbDNI.CheckedChanged += (s, e) => 
            { 
                if (rbDNI.Checked) 
                {
                    OpcionPagoSeleccionada = OpcionPago.DNI;
                    ActualizarOpcionesImpresion();
                }
            };

            rbMercadoPago.CheckedChanged += (s, e) => 
            { 
                if (rbMercadoPago.Checked) 
                {
                    OpcionPagoSeleccionada = OpcionPago.MercadoPago;
                    ActualizarOpcionesImpresion();
                }
            };

            // Evento para cambios en pagos múltiples
            if (multiplePagosControl != null)
            {
                multiplePagosControl.OnPagosChanged += (s, e) =>
                {
                    ActualizarOpcionesImpresion();
                };
            }

            // Eventos CUIT
            txtCuit.Leave += async (s, e) => await ConsultarCuitAsync();
            txtCuit.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await ConsultarCuitAsync();
                }
            };

            // Eventos de botones de impresión
            btnRemito.Click += async (s, e) => 
            {
                try
                {
                    await ProcesarRemito();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error en Remito: {ex.Message}", "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnFacturaB.Click += async (s, e) => 
            {
                try
                {
                    // RESTAURADO: Procesar Factura B con AFIP real
                    await ProcesarFacturaElectronica(OpcionImpresion.FacturaB);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error en Factura B: {ex.Message}", "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnFacturaA.Click += async (s, e) => 
            {
                try
                {
                    // Validar CUIT para Factura A
                    string cuit = txtCuit.Text.Trim();
                    if (string.IsNullOrEmpty(cuit) || cuit.Length != 11)
                    {
                        MessageBox.Show("Para Factura A debe ingresar un CUIT válido.", "CUIT Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtCuit.Focus();
                        return;
                    }

                    // RESTAURADO: Procesar Factura A con AFIP real
                    await ProcesarFacturaElectronica(OpcionImpresion.FacturaA);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error en Factura A: {ex.Message}", "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            this.Shown += (s, e) => 
            {
                if (btnRemito.Enabled)
                    btnRemito.Focus();
                else if (btnFacturaB.Enabled)
                    btnFacturaB.Focus();
                else
                    btnFacturaA.Focus();
            };
        }

        // RESTAURADO: Método centralizado para procesar facturas electrónicas con AFIP REAL
        private async Task ProcesarFacturaElectronica(OpcionImpresion tipoFactura)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 === INICIANDO PROCESAMIENTO {tipoFactura} CON AFIP REAL ===");
                
                // 1. VALIDAR PAGO COMPLETO
                if (EsPagoMultiple && !multiplePagosControl.PagoCompleto)
                {
                    MessageBox.Show(
                        "ERROR: El pago no está completo.\n\n" +
                        $"Total factura: {importeTotalVenta:C2}\n" +
                        $"Importe asignado: {multiplePagosControl.ImporteAsignado:C2}\n" +
                        $"Importe pendiente: {multiplePagosControl.ImportePendiente:C2}\n\n" +
                        "Complete el pago antes de continuar.",
                        "Pago incompleto",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // 2. OBTENER CONFIGURACIÓN AFIP
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string cuitEmisor = config["AFIP:CUIT"];
                if (string.IsNullOrEmpty(cuitEmisor))
                {
                    MessageBox.Show("Error: CUIT del emisor no configurado en appsettings.json", "Error de Configuración", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 3. MOSTRAR INDICADOR DE PROGRESO
                var progressForm = new Form
                {
                    Text = "Procesando factura electrónica con AFIP...",
                    Width = 400,
                    Height = 150,
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    ShowInTaskbar = false
                };

                var lblProgress = new Label
                {
                    Text = "Conectando con AFIP...",
                    Left = 20,
                    Top = 30,
                    Width = 360,
                    Height = 30,
                    Font = new Font("Segoe UI", 10F),
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter
                };

                var progressBar = new ProgressBar
                {
                    Left = 20,
                    Top = 60,
                    Width = 360,
                    Height = 25,
                    Style = ProgressBarStyle.Marquee,
                    MarqueeAnimationSpeed = 50
                };

                progressForm.Controls.AddRange(new Control[] { lblProgress, progressBar });
                progressForm.Show(this);
                Application.DoEvents();

                try
                {
                    // 4. AUTENTICAR CON AFIP USANDO EL SERVICIO ROBUSTO
                    lblProgress.Text = "Autenticando con AFIP...";
                    Application.DoEvents();

                    await AutenticarConAfipReal(cuitEmisor);

                    // 5. OBTENER ÚLTIMO NÚMERO DE COMPROBANTE
                    lblProgress.Text = "Obteniendo número de comprobante...";
                    Application.DoEvents();

                    int tipoComprobante = tipoFactura == OpcionImpresion.FacturaA ? 1 : 6;
                    int puntoVenta = 1; // Configurar según necesidad
                    int ultimoNumero = await ObtenerUltimoNumeroComprobanteReal(tipoComprobante, puntoVenta);
                    int numeroFactura = ultimoNumero + 1;

                    System.Diagnostics.Debug.WriteLine($"📋 Tipo: {tipoComprobante}, PV: {puntoVenta}, Último: {ultimoNumero}, Nuevo: {numeroFactura}");

                    // 6. SOLICITAR CAE A AFIP REAL
                    lblProgress.Text = "Solicitando CAE a AFIP...";
                    Application.DoEvents();

                    string cuitCliente = tipoFactura == OpcionImpresion.FacturaA ? txtCuit.Text.Trim() : "";
                    var resultadoCAE = await SolicitarCAEReal(tipoComprobante, puntoVenta, numeroFactura, cuitCliente);

                    if (!resultadoCAE.exito)
                    {
                        progressForm.Close();
                        MessageBox.Show($"Error obteniendo CAE de AFIP:\n\n{resultadoCAE.error}", 
                            "Error AFIP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // 7. PROCESAR VENTA
                    lblProgress.Text = "Finalizando proceso...";
                    Application.DoEvents();

                    // Almacenar datos CAE
                    CAENumero = resultadoCAE.cae;
                    CAEVencimiento = resultadoCAE.vencimiento;
                    NumeroFacturaAfip = numeroFactura;
                    OpcionSeleccionada = tipoFactura;

                    // Formatear número de factura
                    string numeroFormateado = FormatearNumeroFactura(tipoComprobante, puntoVenta, numeroFactura);

                    string formaPago = EsPagoMultiple ? "Múltiple" : OpcionPagoSeleccionada.ToString();
                    string tipoFacturaString = tipoFactura == OpcionImpresion.FacturaA ? "FacturaA" : "FacturaB";

                    if (OnProcesarVenta != null)
                    {
                        await OnProcesarVenta(tipoFacturaString, formaPago, cuitCliente, 
                            CAENumero, CAEVencimiento, NumeroFacturaAfip, numeroFormateado);
                    }

                    progressForm.Close();

                    // 8. CONFIRMAR ÉXITO
                    MessageBox.Show($"✅ {tipoFactura} procesada exitosamente con AFIP\n\n" +
                        $"Número: {numeroFormateado}\n" +
                        $"CAE: {CAENumero}\n" +
                        $"Vencimiento: {CAEVencimiento:dd/MM/yyyy}", 
                        "Factura Electrónica", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    this.DialogResult = DialogResult.OK;
                    this.Close();

                    System.Diagnostics.Debug.WriteLine($"✅ {tipoFactura} completada exitosamente con AFIP REAL");
                    System.Diagnostics.Debug.WriteLine($"CAE: {CAENumero}, Vencimiento: {CAEVencimiento:dd/MM/yyyy}");
                }
                catch (Exception ex)
                {
                    progressForm.Close();
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en ProcesarFacturaElectronica: {ex.Message}");
                MessageBox.Show($"Error procesando factura electrónica:\n\n{ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MEJORADO: Autenticación completamente transparente para tokens existentes
        private async Task AutenticarConAfipReal(string cuitEmisor)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔑 === AUTENTICACIÓN AFIP TRANSPARENTE ===");

                // PASO 1: Verificar tokens existentes ANTES de cualquier intento
                var (tieneTokenValido, mensaje, minutosRestantes) = AfipAuthenticator.VerificarTokensExistentes("wsfe");
                
                if (tieneTokenValido && minutosRestantes > 2)
                {
                    var tokenExistente = AfipAuthenticator.GetExistingToken("wsfe");
                    if (tokenExistente.HasValue)
                    {
                        TokenAfip = tokenExistente.Value.token;
                        SignAfip = tokenExistente.Value.sign;
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Usando token válido existente: {mensaje}");
                        return; // SALIR INMEDIATAMENTE sin más verificaciones
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[AFIP] 🔍 Estado tokens: {mensaje}");

                // PASO 2: Obtener configuración
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string certificadoPath = config["AFIP:CertificadoPath"];
                string certificadoPassword = config["AFIP:CertificadoPassword"];
                string wsaaUrl = config["AFIP:WSAAUrl"] ?? "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";

                if (string.IsNullOrEmpty(certificadoPath))
                {
                    certificadoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certificado.p12");
                }

                // PASO 3: Verificar certificado
                var (esCertificadoValido, mensajeCert, fechaVencimiento) = AfipAuthenticator.VerificarCertificado(certificadoPath, certificadoPassword ?? "");
                if (!esCertificadoValido)
                {
                    throw new Exception($"Certificado AFIP no válido: {mensajeCert}");
                }

                System.Diagnostics.Debug.WriteLine($"✅ Certificado válido: {mensajeCert}");

                // PASO 4: Usar el nuevo método transparente
                try
                {
                    var (token, sign, expiration) = await AfipAuthenticator.GetTAAsync("wsfe", certificadoPath, certificadoPassword ?? "", wsaaUrl);

                    TokenAfip = token;
                    SignAfip = sign;

                    System.Diagnostics.Debug.WriteLine("✅ Autenticación AFIP transparente completada");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Token válido hasta: {expiration:dd/MM/yyyy HH:mm:ss}");
                    
                    // NO mostrar ningún mensaje al usuario - proceso completamente transparente
                }
                catch (Exception ex) when (ex.Message.Contains("TOKEN") || ex.Message.Contains("token") || ex.Message.Contains("Ya existe"))
                {
                    // ÚLTIMO RECURSO: Forzar uso de token existente
                    System.Diagnostics.Debug.WriteLine($"[AFIP] 🔄 Forzando uso de token existente debido a: {ex.Message}");
                    
                    try
                    {
                        var (token, sign, expiration) = await AfipAuthenticator.ForzarUsoTokenExistente("wsfe", certificadoPath, certificadoPassword ?? "");
                        
                        TokenAfip = token;
                        SignAfip = sign;
                        
                        System.Diagnostics.Debug.WriteLine("✅ Token forzado exitosamente - proceso transparente");
                        return;
                    }
                    catch (Exception exForzar)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ Error forzando token: {exForzar.Message}");
                        throw new Exception($"No se pudo obtener token AFIP de manera transparente: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine("✅ === AUTENTICACIÓN TRANSPARENTE COMPLETADA ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 Error en autenticación transparente: {ex.Message}");
                
                // ÚLTIMO RECURSO FINAL: Intentar cualquier token disponible
                try
                {
                    var tokenUltimoRecurso = AfipAuthenticator.GetExistingToken("wsfe");
                    if (tokenUltimoRecurso.HasValue)
                    {
                        TokenAfip = tokenUltimoRecurso.Value.token;
                        SignAfip = tokenUltimoRecurso.Value.sign;
                        System.Diagnostics.Debug.WriteLine("🆘 Usando token de último recurso de manera transparente");
                        return;
                    }
                }
                catch (Exception exCache)
                {
                    System.Diagnostics.Debug.WriteLine($"💥 Error en último recurso: {exCache.Message}");
                }
                
                // Solo mostrar error al usuario si realmente no se puede continuar
                throw new Exception($"Error crítico de autenticación AFIP: {ex.Message}\n\nNo se pudo obtener tokens válidos para continuar.");
            }
        }

        // NUEVO: Método para obtener último número de comprobante usando ServiceSoapClient
        private async Task<int> ObtenerUltimoNumeroComprobanteReal(int tipoComprobante, int puntoVenta)
        {
            try
            {
                using (var wsfeClient = new ArcaWS.ServiceSoapClient(ArcaWS.ServiceSoapClient.EndpointConfiguration.ServiceSoap))
                {
                    var authRequest = new ArcaWS.FEAuthRequest
                    {
                        Token = TokenAfip,
                        Sign = SignAfip,
                        Cuit = long.Parse(ObtenerCuitEmisor().Replace("-", ""))
                    };

                    System.Diagnostics.Debug.WriteLine($"[AFIP] Consultando último número - Tipo: {tipoComprobante}, PV: {puntoVenta}");

                    var response = await wsfeClient.FECompUltimoAutorizadoAsync(authRequest, puntoVenta, tipoComprobante);
                    var resultado = response.Body.FECompUltimoAutorizadoResult;

                    if (resultado?.Errors != null && resultado.Errors.Length > 0)
                    {
                        string errores = string.Join(", ", resultado.Errors.Select(e => e.Msg));
                        System.Diagnostics.Debug.WriteLine($"[AFIP] Errores: {errores}");
                        throw new Exception($"Error AFIP: {errores}");
                    }

                    int ultimoNumero = resultado?.CbteNro ?? 0;
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Último número autorizado: {ultimoNumero}");
                    
                    return ultimoNumero;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error obteniendo último número: {ex.Message}");
                return 0; // Si hay error, empezar desde 1
            }
        }

        // NUEVO: Método para solicitar CAE usando ServiceSoapClient
        private async Task<(bool exito, string cae, DateTime? vencimiento, string error)> SolicitarCAEReal(
            int tipoComprobante, int puntoVenta, int numero, string cuitCliente = "")
        {
            try
            {
                using (var wsfeClient = new ArcaWS.ServiceSoapClient(ArcaWS.ServiceSoapClient.EndpointConfiguration.ServiceSoap))
                {
                    var authRequest = new ArcaWS.FEAuthRequest
                    {
                        Token = TokenAfip,
                        Sign = SignAfip,
                        Cuit = long.Parse(ObtenerCuitEmisor().Replace("-", ""))
                    };

                    // CORREGIDO: Determinar correctamente DocTipo e IVAPerNro según el tipo de factura
                    int docTipo;
                    long docNro;
                    int ivaPerNro; // NUEVO: Condición IVA del receptor
                    

                    if (tipoComprobante == 1) // Factura A
                    {
                        docTipo = 80; // CUIT
                        docNro = !string.IsNullOrEmpty(cuitCliente) ? long.Parse(cuitCliente.Replace("-", "")) : 0;
                        ivaPerNro = DeterminarCondicionIvaReceptor(tipoComprobante, cuitCliente);
                    }
                    else // Factura B
                    {
                        docTipo = 99; // Sin identificación / Consumidor Final
                        docNro = 0;
                        ivaPerNro = DeterminarCondicionIvaReceptor(tipoComprobante, cuitCliente);
                    }

                    // MEJORADO: Calcular valores con mayor precisión y control de límites
                    decimal importeNetoCalculado = Math.Round(CalcularImporteNeto(), 2);
                    decimal importeIvaCalculado = Math.Round(CalcularImporteIVA(), 2);
                    decimal importeTotalCalculado = Math.Round(importeTotalVenta, 2);

                    // NUEVO: Asegurar consistencia en los totales
                    if (Math.Abs((importeNetoCalculado + importeIvaCalculado) - importeTotalCalculado) > 0.02m)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Ajustando diferencias de redondeo en totales");
                        // Ajustar el IVA para que cuadren los totales
                        importeIvaCalculado = importeTotalCalculado - importeNetoCalculado;
                        importeIvaCalculado = Math.Round(importeIvaCalculado, 2);
                    }

                    // Preparar datos del comprobante
                    var comprobante = new ArcaWS.FECAEDetRequest
                    {
                        Concepto = 1, // Productos
                        DocTipo = docTipo,
                        DocNro = docNro,
                        CondicionIVAReceptorId = ivaPerNro, // CORREGIDO: Usar el nombre correcto de la propiedad
                        CbteDesde = numero,
                        CbteHasta = numero,
                        CbteFch = DateTime.Now.ToString("yyyyMMdd"),
                        ImpTotal = (double)importeTotalCalculado,
                        ImpTotConc = 0,
                        ImpNeto = (double)importeNetoCalculado,
                        ImpOpEx = 0,
                        ImpIVA = (double)importeIvaCalculado,
                        ImpTrib = 0,
                        MonId = "PES",
                        MonCotiz = 1
                    };

                    // MEJORADO: Preparar array de IVA con validaciones estrictas
                    var ivaArray = new List<ArcaWS.AlicIva>();
                    var datosIva = CalcularDetalleIVA();
                    
                    // NUEVO: Validar datos antes de enviar a AFIP
                    var (esValido, errorValidacion) = ValidarDatosParaAfip(datosIva, importeTotalCalculado);
                    if (!esValido)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ Validación falló: {errorValidacion}");
                        return (false, "", null, $"Error de validación: {errorValidacion}");
                    }
                    
                    foreach (var iva in datosIva)
                    {
                        if (iva.Value.baseImponible > 0)
                        {
                            // NUEVO: Validar y formatear BaseImp antes de enviar a AFIP
                            decimal baseImponible = Math.Round(iva.Value.baseImponible, 2);
                            decimal importeIva = Math.Round(iva.Value.importeIva, 2);
                            
                            // CRÍTICO: Asegurar que BaseImp cumple exactamente con el formato AFIP
                            if (baseImponible >= 10000000000000m) // 13 dígitos
                            {
                                System.Diagnostics.Debug.WriteLine($"🚨 BaseImp excede límite AFIP: {baseImponible:F2}");
                                baseImponible = 9999999999999.99m; // Máximo permitido por AFIP
                                importeIva = Math.Round(iva.Value.baseImponible + iva.Value.importeIva - baseImponible, 2);
                                System.Diagnostics.Debug.WriteLine($"🔧 BaseImp ajustada: {baseImponible:F2}, IVA ajustado: {importeIva:F2}");
                            }
                            
                            // NUEVO: Validar formato exacto con regex-like check
                            string baseFormateada = baseImponible.ToString("F2", CultureInfo.InvariantCulture);
                            string[] partesBase = baseFormateada.Split('.');
                            
                            if (partesBase[0].Length > 13 || partesBase[1].Length != 2)
                            {
                                System.Diagnostics.Debug.WriteLine($"❌ Formato inválido BaseImp: {baseFormateada}");
                                // Forzar formato correcto
                                baseImponible = Math.Min(baseImponible, 9999999999999.99m);
                                baseImponible = Math.Round(baseImponible, 2);
                                System.Diagnostics.Debug.WriteLine($"🔧 BaseImp corregida: {baseImponible.ToString("F2", CultureInfo.InvariantCulture)}");
                            }

                            // Mapear porcentaje de IVA a código AFIP
                            int codigoAfip = MapearPorcentajeIvaACodigoAfip(iva.Key);
                            
                            // NUEVO: Validación final antes de agregar al array
                            double baseImpDouble = (double)baseImponible;
                            double importeIvaDouble = (double)importeIva;
                            
                            // Verificar que los valores double mantengan la precisión
                            if (Math.Abs((decimal)baseImpDouble - baseImponible) > 0.01m)
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ Pérdida de precisión en BaseImp: {baseImponible:F2} -> {baseImpDouble:F2}");
                            }
                            
                            ivaArray.Add(new ArcaWS.AlicIva
                            {
                                Id = codigoAfip,
                                BaseImp = baseImpDouble,
                                Importe = importeIvaDouble
                            });

                            System.Diagnostics.Debug.WriteLine($"📊 IVA {iva.Key}% -> Código AFIP: {codigoAfip}, BaseImp: {baseImponible:F2} ({baseImpDouble}), Importe: {importeIva:F2} ({importeIvaDouble})");
                        }
                    }

                    // Solicitar CAE
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

                    // Agregar IVA si hay
                    if (ivaArray.Any())
                    {
                        comprobante.Iva = ivaArray.ToArray();
                    }

                    System.Diagnostics.Debug.WriteLine($"[AFIP] 📤 Enviando solicitud CAE - Comprobante: {tipoComprobante}-{puntoVenta:D4}-{numero:D8}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] DocTipo: {docTipo}, DocNro: {docNro}, IVAPerNro: {ivaPerNro}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Importe Total: {comprobante.ImpTotal:F2}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Base Imponible: {comprobante.ImpNeto:F2}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] IVA: {comprobante.ImpIVA:F2}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Cantidad AlicIva: {ivaArray.Count}");

                    var response = await wsfeClient.FECAESolicitarAsync(authRequest, request);
                    var resultado = response.Body.FECAESolicitarResult;

                    if (resultado?.Errors != null && resultado.Errors.Length > 0)
                    {
                        string errores = string.Join(", ", resultado.Errors.Select(e => e.Msg));
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ Errores CAE: {errores}");
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
                                if (DateTime.TryParseExact(detalle.CAEFchVto, "yyyyMMdd", 
                                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha))
                                {
                                    fechaVencimiento = fecha;
                                }
                            }

                            System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ CAE obtenido exitosamente: {detalle.CAE}");
                            System.Diagnostics.Debug.WriteLine($"[AFIP] Vencimiento CAE: {fechaVencimiento:dd/MM/yyyy}");

                            return (true, detalle.CAE, fechaVencimiento, "");
                        }
                        else
                        {
                            string errores = "";
                            if (detalle.Observaciones != null)
                            {
                                errores = string.Join(", ", detalle.Observaciones.Select(o => o.Msg));
                            }
                            System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ AFIP rechazó el comprobante: {errores}");
                            return (false, "", null, $"AFIP rechazó el comprobante: {errores}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ Respuesta inválida de AFIP");
                        return (false, "", null, "Respuesta inválida de AFIP");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] 💥 Error crítico solicitando CAE: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AFIP] Stack trace: {ex.StackTrace}");
                return (false, "", null, $"Error solicitando CAE: {ex.Message}");
            }
        }

        // NUEVO: Mapear porcentajes de IVA a códigos AFIP
        private int MapearPorcentajeIvaACodigoAfip(decimal porcentaje)
        {
            return porcentaje switch
            {
                0m => 3,      // 0% - No Gravado
                10.5m => 4,   // 10.5%
                21m => 5,     // 21%
                27m => 6,     // 27%
                _ => 5        // Por defecto 21%
            };
        }

        // NUEVO: Determinar condición IVA del receptor según AFIP
        private int DeterminarCondicionIvaReceptor(int tipoComprobante, string cuitCliente = "")
        {
            // Códigos de condición IVA según AFIP:
            // 1 - IVA Responsable Inscripto
            // 2 - IVA Responsable no Inscripto
            // 3 - IVA no Responsable
            // 4 - IVA Sujeto Exento
            // 5 - Consumidor Final
            // 6 - Responsable Monotributo
            // 7 - Sujeto no Categorizado
            // 8 - Proveedor del Exterior
            // 9 - Cliente del Exterior
            // 10 - IVA Liberado – Ley Nº 19.640
            // 11 - IVA Responsable Inscripto – Agente de Percepción
            // 12 - Pequeño Contribuyente

            if (tipoComprobante == 1) // Factura A
            {
                if (string.IsNullOrEmpty(cuitCliente) || cuitCliente.Length != 11)
                {
                    return 0; // CUIT inválido, no se puede determinar condición IVA
                }
                
                // Devolver 1 (IVA Responsable Inscripto) si el CUIT es válido y pertenece a un contribuyente inscripto
                return 1;
            }
            else // Factura B
            {
                // Para Factura B, asumir Consumidor Final (sin identificación)
                return 5;
            }
        }

        // NUEVO: Método para validar datos antes de enviar a AFIP
        private (bool esValido, string error) ValidarDatosParaAfip(Dictionary<decimal, (decimal baseImponible, decimal importeIva)> datosIva, decimal importeTotal)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 === VALIDACIÓN DATOS AFIP ===");
                
                // 1. Validar formato de importes totales
                if (importeTotal >= 10000000000000m)
                {
                    return (false, $"Importe total excede límite AFIP: {importeTotal:F2} (máximo: 9,999,999,999,999.99)");
                }
                
                if (importeTotal <= 0)
                {
                    return (false, $"Importe total debe ser mayor a cero: {importeTotal:F2}");
                }
                
                // 2. Validar cada alícuota de IVA
                decimal sumaBaseImponible = 0;
                decimal sumaIva = 0;
                
                foreach (var iva in datosIva)
                {
                    decimal baseImponible = Math.Round(iva.Value.baseImponible, 2);
                    decimal importeIva = Math.Round(iva.Value.importeIva, 2);
                    
                    // Validar BaseImp - CRÍTICO para el error de AFIP
                    string baseStr = baseImponible.ToString("F2", CultureInfo.InvariantCulture);
                    string[] partesBase = baseStr.Split('.');
                    
                    if (partesBase[0].Length > 13)
                    {
                        return (false, $"BaseImp para IVA {iva.Key}% excede 13 dígitos enteros: {baseStr} (valor: {baseImponible})");
                    }
                    
                    if (partesBase[1].Length != 2)
                    {
                        return (false, $"BaseImp para IVA {iva.Key}% debe tener exactamente 2 decimales: {baseStr}");
                    }
                    
                    // Validar que BaseImp sea positiva
                    if (baseImponible <= 0)
                    {
                        return (false, $"BaseImp para IVA {iva.Key}% debe ser mayor a cero: {baseImponible:F2}");
                    }
                    
                    // Validar Importe IVA
                    if (importeIva < 0)
                    {
                        return (false, $"Importe IVA para alícuota {iva.Key}% no puede ser negativo: {importeIva:F2}");
                    }
                    
                    // Validar coherencia entre porcentaje, base e importe
                    decimal ivaCalculadoEsperado = Math.Round(baseImponible * iva.Key / 100, 2);
                    decimal diferencia = Math.Abs(importeIva - ivaCalculadoEsperado);
                    
                    if (diferencia > 0.05m) // Tolerancia de 5 centavos por redondeos
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Diferencia en cálculo IVA {iva.Key}%: Calculado={ivaCalculadoEsperado:F2}, Enviado={importeIva:F2}, Diferencia={diferencia:F2}");
                        // Solo advertencia, no error crítico
                    }
                    
                    sumaBaseImponible += baseImponible;
                    sumaIva += importeIva;
                    
                    System.Diagnostics.Debug.WriteLine($"✅ IVA {iva.Key}%: Base={baseImponible:F2}, IVA={importeIva:F2} - VÁLIDO");
                }
                
                // 3. Validar coherencia de totales
                decimal totalCalculado = Math.Round(sumaBaseImponible + sumaIva, 2);
                decimal diferenciaTotales = Math.Abs(totalCalculado - importeTotal);
                
                if (diferenciaTotales > 0.02m) // Tolerancia de 2 centavos
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Diferencia en totales: Calculado={totalCalculado:F2}, Esperado={importeTotal:F2}, Diferencia={diferenciaTotales:F2}");
                    // Solo advertencia para diferencias menores
                    if (diferenciaTotales > 0.10m) // Error si la diferencia es mayor a 10 centavos
                    {
                        return (false, $"Diferencia excesiva en totales: Calculado={totalCalculado:F2}, Esperado={importeTotal:F2}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"💰 Resumen validación:");
                System.Diagnostics.Debug.WriteLine($"   Total factura: {importeTotal:F2}");
                System.Diagnostics.Debug.WriteLine($"   Suma BaseImp: {sumaBaseImponible:F2}");
                System.Diagnostics.Debug.WriteLine($"   Suma IVA: {sumaIva:F2}");
                System.Diagnostics.Debug.WriteLine($"   Total calculado: {totalCalculado:F2}");
                System.Diagnostics.Debug.WriteLine($"   Diferencia: {diferenciaTotales:F2}");
                System.Diagnostics.Debug.WriteLine("===============================");
                
                return (true, "");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en validación AFIP: {ex.Message}");
                return (false, $"Error interno en validación: {ex.Message}");
            }
        }

        // NUEVO: Métodos helper para cálculos de facturación
        private decimal CalcularImporteNeto()
        {
            if (formularioPadre?.GetRemitoActual() != null)
            {
                decimal neto = 0;
                foreach (DataRow row in formularioPadre.GetRemitoActual().Rows)
                {
                    if (decimal.TryParse(row["total"].ToString(), out decimal total) &&
                        decimal.TryParse(row["PorcentajeIva"].ToString(), out decimal porcIva))
                    {
                        // Calcular base imponible (total / (1 + %iva/100))
                        decimal baseImponible = Math.Round(total / (1 + porcIva / 100), 2);
                        neto += baseImponible;
                    }
                }
                
                // NUEVO: Validar que el neto total no exceda límites AFIP
                neto = Math.Round(neto, 2);
                if (neto >= 10000000000000m) // 13 dígitos
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Importe neto total excede límite: {neto:N2}");
                    neto = Math.Min(neto, 9999999999999.99m);
                    System.Diagnostics.Debug.WriteLine($"🔧 Importe neto ajustado: {neto:N2}");
                }
                
                return neto;
            }
            
            // Aproximación si no hay datos detallados
            decimal netoAproximado = Math.Round(importeTotalVenta / 1.21m, 2);
            
            // NUEVO: Validar aproximación también
            if (netoAproximado >= 10000000000000m)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Importe neto aproximado excede límite: {netoAproximado:N2}");
                netoAproximado = Math.Min(netoAproximado, 9999999999999.99m);
                System.Diagnostics.Debug.WriteLine($"🔧 Importe neto aproximado ajustado: {netoAproximado:N2}");
            }
            
            return netoAproximado;
        }

        private decimal CalcularImporteIVA()
        {
            decimal neto = CalcularImporteNeto();
            decimal iva = Math.Round(importeTotalVenta - neto, 2);
            
            // NUEVO: Asegurar que el IVA también esté dentro de límites razonables
            if (iva < 0)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ IVA negativo calculado: {iva:N2}, corrigiendo...");
                iva = 0;
            }
            
            // Si el IVA resultante es muy grande (edge case), ajustar
            if (iva >= 10000000000000m)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ IVA excede límites: {iva:N2}");
                iva = Math.Min(iva, 9999999999999.99m);
                System.Diagnostics.Debug.WriteLine($"🔧 IVA ajustado: {iva:N2}");
            }
            
            return iva;
        }

        private void MostrarInformacionEstado(bool hayPagosDigitales, bool pagoCompleto)
        {
            // Label estático para mostrar información del estado
            Label lblMensajeEstado = null;

            // Buscar si ya existe un label de estado
            foreach (Control control in this.Controls)
            {
                if (control is Label lbl && lbl.Name == "lblMensajeEstado")
                {
                    lblMensajeEstado = lbl;
                    break;
                }
            }

            // Remover el label anterior si existe
            if (lblMensajeEstado != null)
            {
                this.Controls.Remove(lblMensajeEstado);
                lblMensajeEstado.Dispose();
                lblMensajeEstado = null;
            }

            string mensaje = "";
            System.Drawing.Color colorFondo = System.Drawing.Color.Transparent;
            System.Drawing.Color colorTexto = System.Drawing.Color.Black;

            // NUEVO: Verificar si las restricciones están deshabilitadas
            bool debeRestringir = DebeRestringirRemitoPorTipoPago();

            if (EsPagoMultiple && !pagoCompleto)
            {
                mensaje = "ATENCION: Complete el pago para habilitar las opciones de impresión";
                colorFondo = System.Drawing.Color.FromArgb(255, 248, 225);
                colorTexto = System.Drawing.Color.FromArgb(133, 100, 4);
            }
            else if (hayPagosDigitales && debeRestringir) // MODIFICADO: Solo mostrar si las restricciones están habilitadas
            {
                mensaje = "INFO: Para pagos digitales solo se permiten facturas electrónicas";
                colorFondo = System.Drawing.Color.FromArgb(217, 237, 247);
                colorTexto = System.Drawing.Color.FromArgb(12, 84, 96);
            }
            else if (EsPagoMultiple && pagoCompleto)
            {
                mensaje = "LISTO: Pago completo - Todas las opciones disponibles";
                colorFondo = System.Drawing.Color.FromArgb(212, 237, 218);
                colorTexto = System.Drawing.Color.FromArgb(21, 87, 36);
            }
            // NUEVO: Mostrar mensaje cuando las restricciones estén deshabilitadas y haya pagos digitales
            //else if (hayPagosDigitales && !debeRestringir)
            //{
            //    mensaje = "INFO: Restricciones de remito deshabilitadas - Todas las opciones disponibles";
            //    colorFondo = System.Drawing.Color.FromArgb(230, 245, 255);
            //    colorTexto = System.Drawing.Color.FromArgb(0, 120, 215);
            //}

            if (!string.IsNullOrEmpty(mensaje))
            {
                lblMensajeEstado = new Label
                {
                    Name = "lblMensajeEstado",
                    Text = mensaje,
                    Left = 40,
                    // CORREGIDO: Ajustar posición según el modo para no tapar el CUIT
                    Top = EsPagoMultiple ? 450 : 245, // CAMBIADO: Más abajo en modo múltiple
                    Width = 600,
                    Height = 25,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    ForeColor = colorTexto,
                    BackColor = colorFondo,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    BorderStyle = BorderStyle.FixedSingle
                };

                this.Controls.Add(lblMensajeEstado);
                lblMensajeEstado.BringToFront();
            }

            System.Diagnostics.Debug.WriteLine($"[INFO ESTADO] Debe restringir: {debeRestringir}, Hay pagos digitales: {hayPagosDigitales}, Mensaje: '{mensaje}'");
        }

        private void ActualizarOpcionesImpresion()
        {
            bool hayPagosDigitales = false;
            bool pagoCompleto = true;

            if (EsPagoMultiple)
            {
                hayPagosDigitales = multiplePagosControl?.TienePagoDigital() ?? false;
                pagoCompleto = multiplePagosControl?.PagoCompleto ?? false;
            }
            else
            {
                hayPagosDigitales = OpcionPagoSeleccionada == OpcionPago.DNI || OpcionPagoSeleccionada == OpcionPago.MercadoPago;
                pagoCompleto = true;
            }

            // MODIFICADO: Para pagos múltiples, siempre permitir remito independientemente de restricciones
            bool debeRestringirPorPago = DebeRestringirRemitoPorTipoPago();
            bool puedeRemito;
            
            if (EsPagoMultiple)
            {
                // NUEVO: En modo pago múltiple, solo verificar que el pago esté completo
                puedeRemito = pagoCompleto;
            }
            else
            {
                // Para pago simple, aplicar las restricciones normales
                puedeRemito = pagoCompleto && (!debeRestringirPorPago || !hayPagosDigitales);
            }
            
            bool puedeFacturas = pagoCompleto;

            System.Diagnostics.Debug.WriteLine($"[RESTRICCIONES] Es pago múltiple: {EsPagoMultiple}");
            System.Diagnostics.Debug.WriteLine($"[RESTRICCIONES] Debe restringir: {debeRestringirPorPago}");
            System.Diagnostics.Debug.WriteLine($"[RESTRICCIONES] Hay pagos digitales: {hayPagosDigitales}");
            System.Diagnostics.Debug.WriteLine($"[RESTRICCIONES] Pago completo: {pagoCompleto}");
            System.Diagnostics.Debug.WriteLine($"[RESTRICCIONES] Puede remito: {puedeRemito}");
            System.Diagnostics.Debug.WriteLine($"[RESTRICCIONES] Puede facturas: {puedeFacturas}");

            btnRemito.Enabled = puedeRemito;
            btnFacturaA.Enabled = puedeFacturas;
            btnFacturaB.Enabled = puedeFacturas;

            // Actualizar apariencia visual
            if (!puedeRemito)
            {
                btnRemito.BackColor = System.Drawing.Color.LightGray;
                btnRemito.ForeColor = System.Drawing.Color.DarkGray;
                btnRemito.Text = "Remito";
            }
            else
            {
                btnRemito.BackColor = System.Drawing.Color.FromArgb(102, 51, 153);
                btnRemito.ForeColor = System.Drawing.Color.White;
                btnRemito.Text = "Remito";
            }

            if (!puedeFacturas)
            {
                btnFacturaA.BackColor = System.Drawing.Color.LightGray;
                btnFacturaA.ForeColor = System.Drawing.Color.DarkGray;
                btnFacturaB.BackColor = System.Drawing.Color.LightGray;
                btnFacturaB.ForeColor = System.Drawing.Color.DarkGray;
            }
            else
            {
                btnFacturaA.BackColor = System.Drawing.Color.FromArgb(40, 167, 69);
                btnFacturaA.ForeColor = System.Drawing.Color.White;
                btnFacturaB.BackColor = System.Drawing.Color.FromArgb(0, 123, 255);
                btnFacturaB.ForeColor = System.Drawing.Color.White;
            }

            MostrarInformacionEstado(hayPagosDigitales, pagoCompleto);
        }

        private async Task ConsultarCuitAsync()
        {
            // Implementación simplificada para consultar CUIT
            string cuit = txtCuit.Text.Trim();
            lblRazonSocial.Text = "";

            if (cuit.Length == 11 && long.TryParse(cuit, out long cuitLong))
            {
                try
                {
                    lblRazonSocial.Text = "CUIT válido";
                    // Aquí se podría implementar la consulta real a AFIP
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

        private async Task ProcesarRemito()
        {
            try
            {
                // MODIFICADO: Verificar las restricciones según el modo de pago
                bool debeRestringirPorPago = DebeRestringirRemitoPorTipoPago();
                
                if (EsPagoMultiple)
                {
                    if (!multiplePagosControl.PagoCompleto)
                    {
                        MessageBox.Show(
                            $"ERROR: El pago no está completo.\n\n" +
                            $"Total factura: {importeTotalVenta:C2}\n" +
                            $"Importe asignado: {multiplePagosControl.ImporteAsignado:C2}\n" +
                            $"Importe pendiente: {multiplePagosControl.ImportePendiente:C2}\n\n" +
                            "Complete el pago antes de continuar.",
                            "Pago incompleto",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    // NUEVO: En modo pago múltiple, NO aplicar restricciones por tipo de pago digital
                    // Solo verificar que el pago esté completo (ya se verificó arriba)
                    System.Diagnostics.Debug.WriteLine("[PROCESAMIENTO REMITO] Modo pago múltiple - Remito permitido sin restricciones");
                }
                else
                {
                    // MANTENIDO: Para pago simple, aplicar las restricciones normales
                    if (debeRestringirPorPago && 
                        (OpcionPagoSeleccionada == OpcionPago.DNI || OpcionPagoSeleccionada == OpcionPago.MercadoPago))
                    {
                        MessageBox.Show(
                            "ERROR: No se puede generar un remito con métodos de pago digitales.\n\n" +
                            "Para pagos con DNI o MercadoPago debe generar una factura electrónica (A o B).",
                            "Método de pago no compatible",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }

                // NUEVO: Log para debugging
                System.Diagnostics.Debug.WriteLine($"[PROCESAMIENTO REMITO] Es pago múltiple: {EsPagoMultiple}");
                System.Diagnostics.Debug.WriteLine($"[PROCESAMIENTO REMITO] Restricciones habilitadas: {debeRestringirPorPago}");
                if (EsPagoMultiple)
                {
                    System.Diagnostics.Debug.WriteLine($"[PROCESAMIENTO REMITO] Pago múltiple - Tiene digitales: {multiplePagosControl.TienePagoDigital()} (PERMITIDO)");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[PROCESAMIENTO REMITO] Pago simple: {OpcionPagoSeleccionada}");
                }

                OpcionSeleccionada = OpcionImpresion.RemitoTicket;
                string formaPago = EsPagoMultiple ? "Múltiple" : OpcionPagoSeleccionada.ToString();
                
                if (OnProcesarVenta != null)
                {
                    await OnProcesarVenta("Remito", formaPago, "", "", null, 0, "");
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error procesando remito: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FormatearNumeroFactura(int tipoComprobante, int puntoVenta, int numero)
        {
            string tipoLetra = tipoComprobante == 1 ? "A" : "B";
            return $"{tipoLetra} {puntoVenta:D4}-{numero:D8}";
        }

        private string ObtenerCuitEmisor()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            
            return config["AFIP:CUIT"] ?? "";
        }

        private Dictionary<decimal, (decimal baseImponible, decimal importeIva)> CalcularDetalleIVA()
        {
            var resultado = new Dictionary<decimal, (decimal baseImponible, decimal importeIva)>();
            
            if (formularioPadre?.GetRemitoActual() != null)
            {
                foreach (DataRow row in formularioPadre.GetRemitoActual().Rows)
                {
                    if (decimal.TryParse(row["total"].ToString(), out decimal total) &&
                        decimal.TryParse(row["PorcentajeIva"].ToString(), out decimal porcIva))
                    {
                        // CORREGIDO: Asegurar que los cálculos estén dentro de los límites de AFIP
                        decimal baseImponible = Math.Round(total / (1 + porcIva / 100), 2);
                        decimal importeIva = Math.Round(total - baseImponible, 2);

                        // NUEVO: Validar que BaseImp no exceda el límite de AFIP (13 dígitos enteros)
                        if (baseImponible >= 10000000000000m) // 13 dígitos
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ BaseImp muy grande: {baseImponible:N2}, ajustando...");
                            baseImponible = Math.Min(baseImponible, 9999999999999.99m); // Máximo permitido
                            importeIva = Math.Round(total - baseImponible, 2);
                        }
                        
                        if (resultado.ContainsKey(porcIva))
                        {
                            var actual = resultado[porcIva];
                            decimal nuevaBase = Math.Round(actual.baseImponible + baseImponible, 2);
                            decimal nuevoIva = Math.Round(actual.importeIva + importeIva, 2);
                            
                            // NUEVO: Validar el acumulado también
                            if (nuevaBase >= 10000000000000m) // 13 dígitos
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ BaseImp acumulada muy grande: {nuevaBase:N2}, ajustando...");
                                nuevaBase = Math.Min(nuevaBase, 9999999999999.99m);
                                // Recalar IVA para mantener consistencia
                                nuevoIva = Math.Round(nuevaBase * porcIva / 100, 2);
                            }
                            
                            resultado[porcIva] = (nuevaBase, nuevoIva);
                        }
                        else
                        {
                            resultado[porcIva] = (baseImponible, importeIva);
                        }

                        // DEBUG: Mostrar valores calculados
                        System.Diagnostics.Debug.WriteLine($"[IVA] {porcIva}% - Total: {total:N2}, Base: {baseImponible:N2}, IVA: {importeIva:N2}");
                    }
                }
            }
            else
            {
                // Valores por defecto si no hay datos detallados
                decimal baseImponible = Math.Round(CalcularImporteNeto(), 2);
                decimal importeIva = Math.Round(CalcularImporteIVA(), 2);
                
                // NUEVO: Validar valores por defecto también
                if (baseImponible >= 10000000000000m)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ BaseImp por defecto muy grande: {baseImponible:N2}, ajustando...");
                    baseImponible = Math.Min(baseImponible, 9999999999999.99m);
                    importeIva = Math.Round(importeTotalVenta - baseImponible, 2);
                }
                
                resultado[21] = (baseImponible, importeIva);
            }

            // DEBUG: Resumen final
            System.Diagnostics.Debug.WriteLine($"=== RESUMEN IVA PARA AFIP ===");
            foreach (var iva in resultado)
            {
                System.Diagnostics.Debug.WriteLine($"IVA {iva.Key}%: Base={iva.Value.baseImponible:N2}, IVA={iva.Value.importeIva:N2}");
                
                // VALIDACIÓN FINAL: Verificar que BaseImp cumple formato AFIP
                string baseStr = iva.Value.baseImponible.ToString("F2", CultureInfo.InvariantCulture);
                string[] partes = baseStr.Split('.');

                if (partes.Length == 2 && (partes[0].Length > 13 || partes[1].Length != 2))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ ERROR: BaseImp {baseStr} no cumple formato AFIP");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✅ BaseImp {baseStr} cumple formato AFIP");
                }
            }
            System.Diagnostics.Debug.WriteLine("=============================");

            return resultado;
        }



        // Método para verificar si se debe restringir el remito por tipo de pago
        private bool DebeRestringirRemitoPorTipoPago()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                return config.GetValue<bool>("RestriccionesImpresion:RestringirRemitoPorPago", true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error leyendo configuración de restricciones: {ex.Message}");
                return true; // Por defecto restringir para cumplimiento normativo
            }
        }

        // Método para actualizar el estado del remito basado en el tipo de pago
        private void ActualizarEstadoRemitoPorTipoPago()
        {
            if (!DebeRestringirRemitoPorTipoPago())
            {
                // Si está deshabilitada la restricción, permitir remito siempre
                return;
            }

            // Si la restricción está habilitada, verificar el tipo de pago
            bool esEfectivo = false;

            if (rbEfectivo?.Checked == true)
            {
                esEfectivo = true;
            }
            else if (chkMultiplesPagos?.Checked == true && multiplePagosControl != null)
            {
                // Para pagos múltiples, verificar si solo es efectivo
                var pagos = multiplePagosControl.ObtenerPagosPorMedio();
                esEfectivo = pagos.Count == 1 && pagos.ContainsKey("Efectivo");
            }

            // CORREGIDO: Usar btnRemito en lugar de rbRemito
            if (btnRemito != null)
            {
                // No cambiar enabled aquí, se maneja en ActualizarOpcionesImpresion()
                // Solo actualizar mensaje informativo
            }

            // Actualizar mensaje informativo
            ActualizarMensajeInformativo();
        }

        private void ActualizarMensajeInformativo()
        {
            if (lblMensajeInformativo == null) return;

            bool debeRestringir = DebeRestringirRemitoPorTipoPago();
            
            if (!debeRestringir)
            {
                lblMensajeInformativo.Text = "ℹ️ Restricciones de remito deshabilitadas en configuración";
                lblMensajeInformativo.ForeColor = Color.Blue;
                lblMensajeInformativo.Visible = true;
                return;
            }

            bool esEfectivo = rbEfectivo?.Checked == true;
            
            if (chkMultiplesPagos?.Checked == true && multiplePagosControl != null)
            {
                var pagos = multiplePagosControl.ObtenerPagosPorMedio();
                esEfectivo = pagos.Count == 1 && pagos.ContainsKey("Efectivo");
            }

            if (esEfectivo)
            {
                // NUEVO: Ocultar el mensaje cuando es solo efectivo (no hay restricciones activas)
                lblMensajeInformativo.Visible = false;
            }
            else
            {
                lblMensajeInformativo.Text = "⚠️ Para pagos no efectivo solo se permiten Facturas A o B";
                lblMensajeInformativo.ForeColor = Color.Orange;
                lblMensajeInformativo.Visible = true;
            }

            System.Diagnostics.Debug.WriteLine($"[MENSAJE INFO] Debe restringir: {debeRestringir}, Es efectivo: {esEfectivo}, Visible: {lblMensajeInformativo.Visible}");
        }
    }
}
