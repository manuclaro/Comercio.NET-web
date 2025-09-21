using System;
using System.Drawing;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public class MotivoEliminacionForm : Form  // Quitar 'partial'
    {
        public string Motivo { get; private set; }

        public MotivoEliminacionForm()
        {
            // Quitar InitializeComponent();
            ConfigurarFormulario();
        }

        private void ConfigurarFormulario()
        {
            this.Text = "Motivo de Eliminación";
            this.Size = new Size(400, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            var lblMotivo = new Label
            {
                Text = "Ingrese el motivo de la eliminación:",
                Location = new Point(15, 15),
                Size = new Size(350, 20)
            };

            var txtMotivo = new TextBox
            {
                Location = new Point(15, 45),
                Size = new Size(350, 60),
                Multiline = true,
                Name = "txtMotivo"
            };

            var btnAceptar = new Button
            {
                Text = "Aceptar",
                Location = new Point(220, 120),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };

            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(305, 120),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };

            btnAceptar.Click += (s, e) =>
            {
                Motivo = txtMotivo.Text.Trim();
                if (string.IsNullOrEmpty(Motivo))
                {
                    MessageBox.Show("Debe ingresar un motivo.", "Validación", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            this.Controls.AddRange(new Control[] { lblMotivo, txtMotivo, btnAceptar, btnCancelar });
            this.AcceptButton = btnAceptar;
            this.CancelButton = btnCancelar;
            
            txtMotivo.Focus();
        }
    }
}