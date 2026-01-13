using System;
using System.Drawing;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public class RetiroEfectivoForm : Form
    {
        private TextBox txtMonto;
        private TextBox txtMotivo;
        private TextBox txtResponsable;
        private Button btnAceptar;
        private Button btnCancelar;

        public decimal Monto { get; private set; }
        public string Motivo { get; private set; }
        public string Responsable { get; private set; }
        public bool Confirmado { get; private set; }

        public RetiroEfectivoForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Retiro de Efectivo";
            this.Size = new Size(450, 280);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            // Label y TextBox para Monto
            var lblMonto = new Label
            {
                Text = "Monto a retirar: $",
                Location = new Point(20, 20),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            txtMonto = new TextBox
            {
                Location = new Point(145, 18),
                Size = new Size(270, 30),
                Font = new Font("Segoe UI", 14F),
                PlaceholderText = "0.00"
            };
            txtMonto.KeyPress += TxtMonto_KeyPress;

            // Label y TextBox para Motivo
            var lblMotivo = new Label
            {
                Text = "Motivo:",
                Location = new Point(20, 70),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            txtMotivo = new TextBox
            {
                Location = new Point(145, 68),
                Size = new Size(270, 30),
                Font = new Font("Segoe UI", 11F),
                PlaceholderText = "Gastos, pagos, etc.",
                MaxLength = 500
            };

            // Label y TextBox para Responsable
            var lblResponsable = new Label
            {
                Text = "Responsable:",
                Location = new Point(20, 120),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            txtResponsable = new TextBox
            {
                Location = new Point(145, 118),
                Size = new Size(270, 30),
                Font = new Font("Segoe UI", 11F),
                PlaceholderText = "Nombre del responsable",
                MaxLength = 100
            };

            // Botones
            btnAceptar = new Button
            {
                Text = "Confirmar",
                Size = new Size(120, 40),
                Location = new Point(230, 180),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            btnAceptar.FlatAppearance.BorderSize = 0;
            btnAceptar.Click += BtnAceptar_Click;

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Size = new Size(120, 40),
                Location = new Point(360, 180),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => { Confirmado = false; this.Close(); };

            // Agregar controles
            this.Controls.Add(lblMonto);
            this.Controls.Add(txtMonto);
            this.Controls.Add(lblMotivo);
            this.Controls.Add(txtMotivo);
            this.Controls.Add(lblResponsable);
            this.Controls.Add(txtResponsable);
            this.Controls.Add(btnAceptar);
            this.Controls.Add(btnCancelar);

            // Foco inicial
            this.Shown += (s, e) => txtMonto.Focus();
        }

        private void TxtMonto_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Permitir solo números, punto decimal y teclas de control
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',')
            {
                e.Handled = true;
            }

            // Convertir coma a punto
            if (e.KeyChar == ',')
            {
                e.KeyChar = '.';
            }

            // Permitir solo un punto decimal
            if (e.KeyChar == '.' && ((TextBox)sender).Text.Contains('.'))
            {
                e.Handled = true;
            }

            // Enter para confirmar
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                btnAceptar.PerformClick();
            }
        }

        private void BtnAceptar_Click(object sender, EventArgs e)
        {
            // Validar monto
            if (string.IsNullOrWhiteSpace(txtMonto.Text) || !decimal.TryParse(txtMonto.Text, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Debe ingresar un monto válido mayor a cero.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMonto.Focus();
                return;
            }

            // Validar motivo
            if (string.IsNullOrWhiteSpace(txtMotivo.Text))
            {
                MessageBox.Show("Debe ingresar un motivo para el retiro.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMotivo.Focus();
                return;
            }

            // Validar responsable
            if (string.IsNullOrWhiteSpace(txtResponsable.Text))
            {
                MessageBox.Show("Debe ingresar el nombre del responsable.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtResponsable.Focus();
                return;
            }

            Monto = monto;
            Motivo = txtMotivo.Text.Trim();
            Responsable = txtResponsable.Text.Trim();
            Confirmado = true;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}