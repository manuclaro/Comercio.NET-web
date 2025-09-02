using Newtonsoft.Json.Linq;
using System;
using System.Windows.Forms;
using WSconsultaCUIT;

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
            Transferencia,
            TarjetaCredito
        }

        public OpcionImpresion OpcionSeleccionada { get; private set; } = OpcionImpresion.Ninguna;
        public OpcionPago OpcionPagoSeleccionada { get; private set; } = OpcionPago.Efectivo;

        private TextBox txtCuit;
        private Label lblRazonSocial;

        public SeleccionImpresionForm()
        {
            this.Text = "Seleccione tipo de impresión";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Width = 600;
            this.Height = 270;

            var fontRadio = new Font("Segoe UI", 12F, FontStyle.Regular);

            // Opciones de impresión
            var btnRemito = new Button { Text = "Remito (Ticket)", Width = 130, Left = 60, Top = 150, Height = 40, DialogResult = DialogResult.OK };
            var btnFacturaB = new Button { Text = "Factura B", Width = 130, Left = 210, Top = 150, Height = 40, DialogResult = DialogResult.OK };
            var btnFacturaA = new Button { Text = "Factura A", Width = 130, Left = 360, Top = 150, Height = 40, DialogResult = DialogResult.OK };

            btnRemito.Click += (s, e) => { OpcionSeleccionada = OpcionImpresion.RemitoTicket; this.Close(); };
            btnFacturaB.Click += (s, e) => { OpcionSeleccionada = OpcionImpresion.FacturaB; this.Close(); };
            btnFacturaA.Click += (s, e) => { OpcionSeleccionada = OpcionImpresion.FacturaA; this.Close(); };

            // Opciones de pago (RadioButtons)
            var lblPago = new Label { Text = "Forma de pago:", Left = 40, Top = 30, Width = 200, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            var rbEfectivo = new RadioButton { Text = "Efectivo", Left = 70, Top = 70, Width = 140, Height = 30, Font = fontRadio, Checked = true };
            var rbTransferencia = new RadioButton { Text = "Transferencia", Left = 210, Top = 70, Width = 160, Height = 30, Font = fontRadio };
            var rbTarjeta = new RadioButton { Text = "Tarjeta/crédito", Left = 370, Top = 70, Width = 180, Height = 30, Font = fontRadio };

            rbEfectivo.CheckedChanged += (s, e) => { if (rbEfectivo.Checked) OpcionPagoSeleccionada = OpcionPago.Efectivo; };
            rbTransferencia.CheckedChanged += (s, e) => { if (rbTransferencia.Checked) OpcionPagoSeleccionada = OpcionPago.Transferencia; };
            rbTarjeta.CheckedChanged += (s, e) => { if (rbTarjeta.Checked) OpcionPagoSeleccionada = OpcionPago.TarjetaCredito; };

            this.Controls.Add(lblPago);
            this.Controls.Add(rbEfectivo);
            this.Controls.Add(rbTransferencia);
            this.Controls.Add(rbTarjeta);

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
                        // Si 'error' es un array de string, muestra todos los mensajes
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
            // Solo obtiene el TA para el padrón
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
            return await AfipAuthenticator.GetTAAsync(service, pfxPath, pfxPassword, wsaaUrl);
        }

        public string TokenAfip { get; set; }
        public string SignAfip { get; set; }
    }
}
