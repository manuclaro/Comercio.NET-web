using System;
using System.Drawing;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class EliminarProductoDialog : Form
    {
        public int CantidadAEliminar { get; private set; }
        public string Motivo { get; private set; } = "";
        public bool Confirmado { get; private set; } = false;
        public bool EliminarCompleto { get; private set; } = false;

        private readonly string codigoProducto;
        private readonly string descripcionProducto;
        private readonly int cantidadActual;
        private readonly decimal precioUnitario;
        private readonly decimal totalLinea;
        private TextBox txtMotivo; // Add as class field

        public EliminarProductoDialog(string codigo, string descripcion, int cantidad, decimal precio, decimal total)
        {
            this.codigoProducto = codigo;
            this.descripcionProducto = descripcion;
            this.cantidadActual = cantidad;
            this.precioUnitario = precio;
            this.totalLinea = total;
            
            InitializeComponent();
            ConfigurarFormulario();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Configuración del formulario
            this.Text = "Eliminar Producto";
            this.Size = new Size(500, 420);
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
                BackColor = Color.FromArgb(220, 53, 69), // Rojo para indicar eliminación
                Padding = new Padding(20, 15, 20, 15)
            };

            var lblTitulo = new Label
            {
                Text = "??? Eliminar Producto",
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
                Padding = new Padding(25, 20, 25, 80) // Espacio extra abajo para botones
            };

            // Información del producto
            var lblProducto = new Label
            {
                Text = "Producto a eliminar:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(0, 10),
                AutoSize = true
            };

            var lblCodigoDesc = new Label
            {
                Text = $"[{codigoProducto}] {descripcionProducto}",
                Location = new Point(0, 35),
                Size = new Size(420, 25),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            // Detalles de la línea
            var lblDetalles = new Label
            {
                Text = "Detalles de la línea:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(0, 75),
                AutoSize = true
            };

            var lblInfoLinea = new Label
            {
                Text = $"Cantidad: {cantidadActual}   |   Precio unitario: {precioUnitario:C2}   |   Total: {totalLinea:C2}",
                Location = new Point(0, 100),
                Size = new Size(420, 25),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            // Opciones de eliminación (solo si cantidad > 1)
            GroupBox grpOpciones = null;
            RadioButton rbCompleto = null;
            RadioButton rbParcial = null;
            NumericUpDown nudCantidadEliminar = null;

            if (cantidadActual > 1)
            {
                grpOpciones = new GroupBox
                {
                    Text = "Opciones de eliminación",
                    Location = new Point(0, 135),
                    Size = new Size(420, 100),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };

                rbCompleto = new RadioButton
                {
                    Text = $"Eliminar toda la línea ({cantidadActual} unidades)",
                    Location = new Point(15, 25),
                    Size = new Size(300, 20),
                    Checked = true,
                    Font = new Font("Segoe UI", 9F)
                };

                rbParcial = new RadioButton
                {
                    Text = "Eliminar cantidad específica:",
                    Location = new Point(15, 50),
                    Size = new Size(200, 20),
                    Font = new Font("Segoe UI", 9F)
                };

                nudCantidadEliminar = new NumericUpDown
                {
                    Location = new Point(220, 48),
                    Size = new Size(60, 25),
                    Minimum = 1,
                    Maximum = cantidadActual,
                    Value = 1,
                    Font = new Font("Segoe UI", 9F),
                    Enabled = false
                };

                // Eventos para habilitar/deshabilitar el NumericUpDown
                rbCompleto.CheckedChanged += (s, e) =>
                {
                    if (rbCompleto.Checked)
                    {
                        nudCantidadEliminar.Enabled = false;
                        EliminarCompleto = true;
                        CantidadAEliminar = cantidadActual;
                    }
                };

                rbParcial.CheckedChanged += (s, e) =>
                {
                    if (rbParcial.Checked)
                    {
                        nudCantidadEliminar.Enabled = true;
                        nudCantidadEliminar.Focus();
                        EliminarCompleto = false;
                        CantidadAEliminar = (int)nudCantidadEliminar.Value;
                    }
                };

                nudCantidadEliminar.ValueChanged += (s, e) =>
                {
                    CantidadAEliminar = (int)nudCantidadEliminar.Value;
                };

                grpOpciones.Controls.AddRange(new Control[] { rbCompleto, rbParcial, nudCantidadEliminar });
            }
            else
            {
                // Si cantidad es 1, eliminar completo automáticamente
                EliminarCompleto = true;
                CantidadAEliminar = 1;
            }

            // Motivo de eliminación
            var lblMotivo = new Label
            {
                Text = "Motivo de la eliminación:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(0, cantidadActual > 1 ? 250 : 140),
                AutoSize = true
            };

            txtMotivo = new TextBox // Change from 'var' to direct assignment
            {
                Location = new Point(0, cantidadActual > 1 ? 275 : 165),
                Size = new Size(420, 50),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Ingrese el motivo de la eliminación (requerido)"
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
                Size = new Size(100, 32),
                Location = new Point(250, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                DialogResult = DialogResult.Cancel
            };
            btnCancelar.FlatAppearance.BorderSize = 0;

            var btnEliminar = new Button
            {
                Text = "??? Eliminar",
                Size = new Size(120, 32),
                Location = new Point(360, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnEliminar.FlatAppearance.BorderSize = 0;

            // Eventos
            btnEliminar.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtMotivo.Text))
                {
                    MessageBox.Show("Debe ingresar un motivo para la eliminación.", "Motivo Requerido",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMotivo.Focus();
                    return;
                }

                // Actualizar valores según la selección
                if (cantidadActual > 1)
                {
                    if (rbCompleto?.Checked == true)
                    {
                        EliminarCompleto = true;
                        CantidadAEliminar = cantidadActual;
                    }
                    else if (rbParcial?.Checked == true)
                    {
                        EliminarCompleto = false;
                        CantidadAEliminar = (int)nudCantidadEliminar.Value;
                    }
                }

                Motivo = txtMotivo.Text.Trim();

                // Confirmación final
                string mensaje;
                if (EliminarCompleto)
                {
                    mensaje = $"¿Confirma la eliminación completa del producto?\n\n" +
                             $"Producto: {descripcionProducto}\n" +
                             $"Cantidad a eliminar: {CantidadAEliminar}\n" +
                             $"Valor: {(precioUnitario * CantidadAEliminar):C2}\n" +
                             $"Motivo: {Motivo}\n\n" +
                             "Esta acción será registrada en el sistema.";
                }
                else
                {
                    mensaje = $"¿Confirma la eliminación parcial del producto?\n\n" +
                             $"Producto: {descripcionProducto}\n" +
                             $"Cantidad actual: {cantidadActual}\n" +
                             $"Cantidad a eliminar: {CantidadAEliminar}\n" +
                             $"Cantidad restante: {cantidadActual - CantidadAEliminar}\n" +
                             $"Valor a eliminar: {(precioUnitario * CantidadAEliminar):C2}\n" +
                             $"Motivo: {Motivo}\n\n" +
                             "Esta acción será registrada en el sistema.";
                }

                var confirmacion = MessageBox.Show(mensaje, "Confirmar Eliminación",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

                if (confirmacion == DialogResult.Yes)
                {
                    Confirmado = true;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };

            txtMotivo.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    btnCancelar.PerformClick();
                    e.SuppressKeyPress = true;
                }
            };

            // Agregar controles al panel principal
            panelMain.Controls.Add(lblProducto);
            panelMain.Controls.Add(lblCodigoDesc);
            panelMain.Controls.Add(lblDetalles);
            panelMain.Controls.Add(lblInfoLinea);
            
            if (grpOpciones != null)
            {
                panelMain.Controls.Add(grpOpciones);
            }
            
            panelMain.Controls.Add(lblMotivo);
            panelMain.Controls.Add(txtMotivo);

            panelBotones.Controls.AddRange(new Control[] { btnCancelar, btnEliminar });

            this.Controls.Add(panelMain);
            this.Controls.Add(panelBotones);

            this.CancelButton = btnCancelar;

            // Establecer valores iniciales
            EliminarCompleto = cantidadActual == 1;
            CantidadAEliminar = cantidadActual == 1 ? 1 : cantidadActual;

            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            // Enfocar el campo de motivo al abrir
            this.Shown += (s, e) =>
            {
                if (txtMotivo != null) // Direct field access, no Controls.Find needed
                {
                    txtMotivo.Focus();
                }
            };
        }
    }
}