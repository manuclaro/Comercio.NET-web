using System;
using System.Drawing;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class EditarCantidadDialog : Form
    {
        public int NuevaCantidad { get; private set; }
        public bool Confirmado { get; private set; } = false;

        private readonly string codigoProducto;
        private readonly string descripcionProducto;
        private readonly int cantidadActual;
        private NumericUpDown nudCantidad; // Add as class field

        public EditarCantidadDialog(string codigo, string descripcion, int cantidadActual)
        {
            this.codigoProducto = codigo;
            this.descripcionProducto = descripcion;
            this.cantidadActual = cantidadActual;
            
            InitializeComponent();
            ConfigurarFormulario();
            CargarDatos();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Configuración del formulario
            this.Text = "Editar Cantidad";
            this.Size = new Size(450, 280);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F);

            // Panel superior con icono y título
            var panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(0, 120, 215),
                Padding = new Padding(20, 15, 20, 15)
            };

            var lblTitulo = new Label
            {
                Text = "📝 Editar Cantidad",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 18)
            };

            panelHeader.Controls.Add(lblTitulo);
            this.Controls.Add(panelHeader);

            // Panel principal
            var panelMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25, 20, 25, 20)
            };

            // Información del producto
            var lblProducto = new Label
            {
                Text = "Producto:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(0, 10),
                AutoSize = true
            };

            var lblCodigoDesc = new Label
            {
                Text = $"[{codigoProducto}] {descripcionProducto}",
                Location = new Point(0, 35),
                Size = new Size(380, 40),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            // Cantidad actual
            var lblCantidadActualTxt = new Label
            {
                Text = "Cantidad actual:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(0, 85),
                AutoSize = true
            };

            var lblCantidadActual = new Label
            {
                Text = cantidadActual.ToString(),
                Location = new Point(120, 85),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                AutoSize = true
            };

            // Nueva cantidad
            var lblNuevaCantidad = new Label
            {
                Text = "Nueva cantidad:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(0, 115),
                AutoSize = true
            };

            nudCantidad = new NumericUpDown // Change from 'var' to direct assignment
            {
                Location = new Point(120, 113),
                Size = new Size(80, 25),
                Minimum = 1,
                Maximum = 9999,
                Value = cantidadActual,
                Font = new Font("Segoe UI", 11F),
                TextAlign = HorizontalAlignment.Center
            };

            // Panel de botones
            var panelBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(25, 15, 25, 15)
            };

            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Size = new Size(90, 32),
                Location = new Point(210, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                DialogResult = DialogResult.Cancel
            };
            btnCancelar.FlatAppearance.BorderSize = 0;

            var btnAceptar = new Button
            {
                Text = "✓ Confirmar",
                Size = new Size(110, 32),
                Location = new Point(310, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            btnAceptar.FlatAppearance.BorderSize = 0;

            // Eventos
            btnAceptar.Click += (s, e) =>
            {
                if (nudCantidad.Value <= 0)
                {
                    MessageBox.Show("La cantidad debe ser mayor a cero.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                NuevaCantidad = (int)nudCantidad.Value;
                Confirmado = true;
            };

            nudCantidad.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    btnAceptar.PerformClick();
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    btnCancelar.PerformClick();
                    e.SuppressKeyPress = true;
                }
            };

            // Agregar controles
            panelMain.Controls.AddRange(new Control[] {
                lblProducto, lblCodigoDesc, lblCantidadActualTxt, 
                lblCantidadActual, lblNuevaCantidad, nudCantidad
            });

            panelBotones.Controls.AddRange(new Control[] { btnCancelar, btnAceptar });

            this.Controls.Add(panelMain);
            this.Controls.Add(panelBotones);

            this.AcceptButton = btnAceptar;
            this.CancelButton = btnCancelar;

            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            // Configuración adicional si es necesaria
        }

        private void CargarDatos()
        {
            // Seleccionar el texto del NumericUpDown cuando se abra
            this.Shown += (s, e) =>
            {
                if (nudCantidad != null) // Direct field access, no Controls.Find needed
                {
                    nudCantidad.Select(0, nudCantidad.Text.Length);
                    nudCantidad.Focus();
                }
            };
        }
    }
}