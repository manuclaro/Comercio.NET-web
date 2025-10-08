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

        // Delegate para el callback después de procesar la venta
        public Func<string, string, string, string, DateTime?, int, string, Task> OnProcesarVenta { get; set; }

        private decimal importeTotalVenta;
        private Ventas formularioPadre;

        // Cache de tokens para evitar duplicados en AFIP
        private static string _cachedTokenWsfe = null;
        private static string _cachedSignWsfe = null;
        private static DateTime _cachedTokenExpiryWsfe = DateTime.MinValue;

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
            this.Height = 550;

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

            // Controles para CUIT y Razón Social
            int topCuit = 340; // Ajustar posición según el modo

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

            var lblCuit = new Label
            {
                Text = "CUIT:",
                Left = 40,
                Top = topCuit + 2,
                Width = 50,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            // Botones de impresión - CORREGIDOS: Sin emojis y con mejor tamaño
            int topBotones = 380;

            btnRemito = new Button 
            { 
                Text = "Remito", 
                Width = 130, // AUMENTADO para que no se corte
                Left = 60, 
                Top = topBotones, 
                Height = 45,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(102, 51, 153), // CAMBIADO: Morado oscuro en lugar de gris
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnFacturaB = new Button 
            { 
                Text = "Factura B", 
                Width = 130, // AUMENTADO para consistencia
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
                Width = 130, // AUMENTADO para consistencia
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
                Top = topBotones,
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
                panelPagoMultiple,
                lblCuit,
                txtCuit,
                lblRazonSocial,
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

                if (esPagoMultiple)
                {
                    multiplePagosControl.EstablecerImporteTotal(importeTotalVenta);
                    this.Height = 550;
                    
                    txtCuit.Top = 340;
                    lblRazonSocial.Top = 342;
                    this.Controls.Find("lblCuit", true).FirstOrDefault()?.SetBounds(40, 342, 50, 20);

                    btnRemito.Top = 380;
                    btnFacturaB.Top = 380;
                    btnFacturaA.Top = 380;
                }
                else
                {
                    this.Height = 320;
                    
                    txtCuit.Top = 180;
                    lblRazonSocial.Top = 182;
                    this.Controls.Find("lblCuit", true).FirstOrDefault()?.SetBounds(40, 182, 50, 20);

                    btnRemito.Top = 220;
                    btnFacturaB.Top = 220;
                    btnFacturaA.Top = 220;
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
                    // SIMPLIFICADO: Solo procesar como pago simple por ahora
                    OpcionSeleccionada = OpcionImpresion.FacturaB;
                    string formaPago = EsPagoMultiple ? "Múltiple" : OpcionPagoSeleccionada.ToString();
                    
                    if (OnProcesarVenta != null)
                    {
                        await OnProcesarVenta("FacturaB", formaPago, "", "", null, 0, "");
                    }

                    this.DialogResult = DialogResult.OK;
                    this.Close();
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

                    OpcionSeleccionada = OpcionImpresion.FacturaA;
                    string formaPago = EsPagoMultiple ? "Múltiple" : OpcionPagoSeleccionada.ToString();
                    
                    if (OnProcesarVenta != null)
                    {
                        await OnProcesarVenta("FacturaA", formaPago, cuit, "", null, 0, "");
                    }

                    this.DialogResult = DialogResult.OK;
                    this.Close();
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

            // Aplicar restricciones de impresión
            bool puedeRemito = !hayPagosDigitales && pagoCompleto;
            bool puedeFacturas = pagoCompleto;

            btnRemito.Enabled = puedeRemito;
            btnFacturaA.Enabled = puedeFacturas;
            btnFacturaB.Enabled = puedeFacturas;

            // Actualizar apariencia visual - CORREGIDO: Mejor texto cuando está deshabilitado
            if (!puedeRemito)
            {
                btnRemito.BackColor = System.Drawing.Color.LightGray;
                btnRemito.ForeColor = System.Drawing.Color.DarkGray;
                btnRemito.Text = "Remito";// (No disponible)
            }
            else
            {
                btnRemito.BackColor = System.Drawing.Color.FromArgb(102, 51, 153); // CAMBIADO: Mismo morado oscuro
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

        private Label lblMensajeEstado = null;

        private void MostrarInformacionEstado(bool hayPagosDigitales, bool pagoCompleto)
        {
            if (lblMensajeEstado != null)
            {
                this.Controls.Remove(lblMensajeEstado);
                lblMensajeEstado.Dispose();
                lblMensajeEstado = null;
            }

            string mensaje = "";
            System.Drawing.Color colorFondo = System.Drawing.Color.Transparent;
            System.Drawing.Color colorTexto = System.Drawing.Color.Black;

            if (EsPagoMultiple && !pagoCompleto)
            {
                mensaje = "ATENCION: Complete el pago para habilitar las opciones de impresión"; // CORREGIDO: Sin emoji
                colorFondo = System.Drawing.Color.FromArgb(255, 248, 225);
                colorTexto = System.Drawing.Color.FromArgb(133, 100, 4);
            }
            else if (hayPagosDigitales)
            {
                mensaje = "INFO: Para pagos digitales solo se permiten facturas electrónicas"; // CORREGIDO: Sin emoji
                colorFondo = System.Drawing.Color.FromArgb(217, 237, 247);
                colorTexto = System.Drawing.Color.FromArgb(12, 84, 96);
            }
            else if (EsPagoMultiple && pagoCompleto)
            {
                mensaje = "LISTO: Pago completo - Todas las opciones disponibles"; // CORREGIDO: Sin emoji
                colorFondo = System.Drawing.Color.FromArgb(212, 237, 218);
                colorTexto = System.Drawing.Color.FromArgb(21, 87, 36);
            }

            if (!string.IsNullOrEmpty(mensaje))
            {
                lblMensajeEstado = new Label
                {
                    Text = mensaje,
                    Left = 40,
                    Top = EsPagoMultiple ? 435 : 275,
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
        }

        private async Task ProcesarRemito()
        {
            try
            {
                if (EsPagoMultiple)
                {
                    if (!multiplePagosControl.PagoCompleto)
                    {
                        MessageBox.Show(
                            $"ERROR: El pago no está completo.\n\n" + // CORREGIDO: Sin emoji
                            $"Total factura: {importeTotalVenta:C2}\n" +
                            $"Importe asignado: {multiplePagosControl.ImporteAsignado:C2}\n" +
                            $"Importe pendiente: {multiplePagosControl.ImportePendiente:C2}\n\n" +
                            "Complete el pago antes de continuar.",
                            "Pago incompleto",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    if (multiplePagosControl.TienePagoDigital())
                    {
                        MessageBox.Show(
                            "ERROR: No se puede generar un remito cuando hay pagos digitales.\n\n" + // CORREGIDO: Sin emoji
                            $"Medios de pago registrados:\n{multiplePagosControl.ObtenerResumenPagos()}\n\n" +
                            "Para pagos digitales debe generar una factura electrónica (A o B).",
                            "Método de pago no compatible",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }
                else
                {
                    if (OpcionPagoSeleccionada == OpcionPago.DNI || OpcionPagoSeleccionada == OpcionPago.MercadoPago)
                    {
                        MessageBox.Show(
                            "ERROR: No se puede generar un remito con métodos de pago digitales.\n\n" + // CORREGIDO: Sin emoji
                            "Para pagos con DNI o MercadoPago debe generar una factura electrónica (A o B).",
                            "Método de pago no compatible",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
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
    }
}
