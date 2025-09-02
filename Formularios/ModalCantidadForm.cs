using System;
using System.Drawing;
using System.Windows.Forms;

namespace Comercio.NET
{
    public partial class ModalCantidadForm : Form
    {
        private TextBox txtCantidad;
        private Button btnAceptar;
        private Button btnCancelar;

        public int CantidadSeleccionada { get; private set; } = 1;

        public ModalCantidadForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Cantidad del producto";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Width = 300;
            this.Height = 150; 

            var lblCantidad = new Label
            {
                Text = "Ingrese la cantidad:",
                Left = 20,
                Top = 20,
                Width = 150,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            txtCantidad = new TextBox
            {
                Left = 20,
                Top = 50,
                Width = 100,
                Font = new Font("Segoe UI", 12F),
                Text = "1",
                MaxLength = 2,
                TextAlign = HorizontalAlignment.Center // Centrar el contenido
            };

            // Solo permite números enteros
            txtCantidad.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            };

            txtCantidad.TextChanged += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtCantidad.Text))
                    txtCantidad.Text = "1";
                
                if (int.TryParse(txtCantidad.Text, out int valor))
                {
                    if (valor > 99)
                        txtCantidad.Text = "99";
                    else if (valor < 1)
                        txtCantidad.Text = "1";
                }
            };

            btnAceptar = new Button
            {
                Text = "Aceptar",
                Left = 175,
                Top = 15, // Bajado de 45 a 60
                Width = 80,
                Height = 30,
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Left = 175,
                Top = 55, // Bajado de 80 a 100
                Width = 80,
                Height = 30,
                DialogResult = DialogResult.Cancel,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnAceptar.Click += (s, e) =>
            {
                if (int.TryParse(txtCantidad.Text, out int cantidad) && cantidad >= 1 && cantidad <= 99)
                {
                    CantidadSeleccionada = cantidad;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Ingrese una cantidad válida entre 1 y 99.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCantidad.Focus();
                }
            };

            this.Controls.Add(lblCantidad);
            this.Controls.Add(txtCantidad);
            this.Controls.Add(btnAceptar);
            this.Controls.Add(btnCancelar);

            // Seleccionar todo el texto al mostrar el modal
            this.Shown += (s, e) => txtCantidad.SelectAll();
            
            // Enter en el textbox = clic en Aceptar
            txtCantidad.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    btnAceptar.PerformClick();
                }
            };
        }
    }
}