using System;
using System.Windows.Forms;

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

        public SeleccionImpresionForm()
        {
            this.Text = "Seleccione tipo de impresión";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Width = 500;
            this.Height = 270;

            var fontRadio = new Font("Segoe UI", 12F, FontStyle.Regular);

            // Opciones de impresión
            var btnRemito = new Button { Text = "Remito (Ticket)", Width = 130, Left = 40, Top = 150, Height = 40, DialogResult = DialogResult.OK };
            var btnFacturaB = new Button { Text = "Factura B", Width = 130, Left = 180, Top = 150, Height = 40, DialogResult = DialogResult.OK };
            var btnFacturaA = new Button { Text = "Factura A", Width = 130, Left = 320, Top = 150, Height = 40, DialogResult = DialogResult.OK };

            btnRemito.Click += (s, e) => { OpcionSeleccionada = OpcionImpresion.RemitoTicket; this.Close(); };
            btnFacturaB.Click += (s, e) => { OpcionSeleccionada = OpcionImpresion.FacturaB; this.Close(); };
            btnFacturaA.Click += (s, e) => { OpcionSeleccionada = OpcionImpresion.FacturaA; this.Close(); };

            // Opciones de pago (RadioButtons)
            var lblPago = new Label { Text = "Forma de pago:", Left = 40, Top = 30, Width = 200, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            var rbEfectivo = new RadioButton { Text = "Efectivo", Left = 60, Top = 70, Width = 140, Height = 30, Font = fontRadio, Checked = true };
            var rbTransferencia = new RadioButton { Text = "Transferencia", Left = 200, Top = 70, Width = 160, Height = 30, Font = fontRadio };
            var rbTarjeta = new RadioButton { Text = "Tarjeta de crédito", Left = 360, Top = 70, Width = 180, Height = 30, Font = fontRadio };

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
        }
    }
}
