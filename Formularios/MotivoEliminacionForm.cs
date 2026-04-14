using System;
using System.Drawing;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class MotivoEliminacionForm : Form
    {
        private string DescripcionProducto;
        private int CantidadProducto;
        private string CodigoProducto;
        private decimal PrecioProducto;

        public string Motivo { get; private set; }
        public int CantidadAEliminar { get; private set; } // ❌ ESTO QUEDA EN 0

        public string MotivoSeleccionado => Motivo;

        // ✅ NUEVO: Campos para títulos personalizados
        private string TituloFormulario;
        private string TituloEncabezado;

        // ✅ Constructor ORIGINAL (mantener compatibilidad con Ventas.cs)
        public MotivoEliminacionForm(string descripcion, int cantidad, string codigo, decimal precio)
            : this(descripcion, cantidad, codigo, precio, "Eliminar Producto", "ELIMINAR PRODUCTO")
        {
            // Llama al constructor extendido con títulos por defecto
        }

        // ✅ NUEVO: Constructor EXTENDIDO con títulos personalizados
        public MotivoEliminacionForm(string descripcion, int cantidad, string codigo, decimal precio,
                                      string tituloFormulario, string tituloEncabezado)
        {
            DescripcionProducto = descripcion;
            CantidadProducto = cantidad;
            CodigoProducto = codigo;
            PrecioProducto = precio;
            TituloFormulario = tituloFormulario;
            TituloEncabezado = tituloEncabezado;

            // ✅ CRÍTICO: Inicializar CantidadAEliminar con la cantidad total
            CantidadAEliminar = cantidad;

            InitializeComponent();
            ConfigurarFormulario();
        }

        private void ConfigurarFormulario()
        {
            // ✅ USAR los títulos personalizados
            this.Text = TituloFormulario;

            // Configuración básica del formulario con altura fija adecuada
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;
            this.KeyPreview = true;
            this.BackColor = Color.White;
            this.Size = new Size(520, 550);
            this.MinimumSize = new Size(520, 550);

            // Panel principal con scroll
            var panelPrincipal = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                AutoScroll = true,
                Padding = new Padding(0, 0, 0, 50)
            };
            this.Controls.Add(panelPrincipal);

            int leftMargin = 25;
            int rightMargin = 25;
            int topMargin = 20;
            int controlWidth = panelPrincipal.ClientSize.Width - leftMargin - rightMargin - 20;
            int yPos = topMargin;

            // Panel de encabezado
            var panelHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(panelPrincipal.ClientSize.Width, 60),
                BackColor = Color.FromArgb(220, 53, 69),
                Parent = panelPrincipal
            };

            // ✅ USAR el título de encabezado personalizado
            var lblHeader = new Label
            {
                Text = TituloEncabezado,
                Location = new Point(25, 15),
                Size = new Size(controlWidth - 25, 30),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Parent = panelHeader
            };

            yPos = 80;

            // Panel de información del producto
            var panelInfo = new Panel
            {
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 90),
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Parent = panelPrincipal
            };

            var lblInfo = new Label
            {
                Text = "Detalles de la línea:\n\n" +
                       $"Cantidad: {CantidadProducto}  |  Precio unitario: {PrecioProducto:C2}  |  Total: {(CantidadProducto * PrecioProducto):C2}",
                Location = new Point(15, 10),
                Size = new Size(controlWidth - 30, 70),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(62, 80, 100),
                AutoSize = false,
                Parent = panelInfo
            };

            yPos += 110;

            // Sección de opciones de eliminación
            var lblOpciones = new Label
            {
                Text = "Opciones de eliminación:",
                Location = new Point(leftMargin, yPos),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100),
                Parent = panelPrincipal
            };
            yPos += 30;

            // Panel para opciones
            var panelOpciones = new Panel
            {
                Location = new Point(leftMargin + 10, yPos),
                Size = new Size(controlWidth - 20, 80),
                BackColor = Color.White,
                Parent = panelPrincipal
            };

            var rdoEliminarTodo = new RadioButton
            {
                Text = $"Eliminar toda la línea ({CantidadProducto} unidades)",
                Location = new Point(10, 10),
                Size = new Size(controlWidth - 40, 25),
                Font = new Font("Segoe UI", 10F),
                Checked = true,
                ForeColor = Color.FromArgb(62, 80, 100),
                Parent = panelOpciones
            };

            var rdoEliminarParcial = new RadioButton
            {
                Text = "Eliminar cantidad específica:",
                Location = new Point(10, 40),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(62, 80, 100),
                Parent = panelOpciones
            };

            var numCantidad = new NumericUpDown
            {
                Location = new Point(220, 38),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 10F),
                Minimum = 1,
                Maximum = CantidadProducto,
                Value = CantidadProducto,
                Enabled = false,
                TextAlign = HorizontalAlignment.Center,
                Parent = panelOpciones
            };

            // Eventos para habilitar/deshabilitar el NumericUpDown
            rdoEliminarTodo.CheckedChanged += (s, e) =>
            {
                if (rdoEliminarTodo.Checked)
                {
                    numCantidad.Enabled = false;
                    CantidadAEliminar = CantidadProducto;
                }
            };

            rdoEliminarParcial.CheckedChanged += (s, e) =>
            {
                if (rdoEliminarParcial.Checked)
                {
                    numCantidad.Enabled = true;
                    numCantidad.Focus();
                    numCantidad.Select(0, numCantidad.Text.Length);
                }
            };

            numCantidad.ValueChanged += (s, e) =>
            {
                CantidadAEliminar = (int)numCantidad.Value;
            };

            yPos += 100;

            // Sección de motivo
            var lblMotivo = new Label
            {
                Text = "Motivo de la eliminación:",
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 53, 69),
                Parent = panelPrincipal
            };
            yPos += 25;

            var txtMotivo = new TextBox
            {
                Name = "txtMotivo",
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 80),
                Multiline = true,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Ejemplo: Error de precio, producto dañado, cambio de cliente, etc.",
                ScrollBars = ScrollBars.Vertical,
                Parent = panelPrincipal
            };
            yPos += 100;

            // CORREGIDO: Panel para botones fijo en la parte inferior del formulario
            var panelBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(25, 10, 25, 10)
            };
            this.Controls.Add(panelBotones);

            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Parent = panelBotones
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Location = new Point(panelBotones.Width - 220, 12);

            var btnEliminar = new Button
            {
                Text = "Eliminar",
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Parent = panelBotones
            };
            btnEliminar.FlatAppearance.BorderSize = 0;
            btnEliminar.Location = new Point(panelBotones.Width - 110, 12);

            // Eventos de botones
            btnCancelar.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            btnEliminar.Click += (s, e) =>
            {
                // Validar motivo
                if (string.IsNullOrWhiteSpace(txtMotivo.Text) || txtMotivo.Text.Trim().Length < 5)
                {
                    MessageBox.Show(
                        "Debe ingresar un motivo válido.\n\n" +
                        "El motivo debe tener al menos 5 caracteres.",
                        "Motivo requerido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    txtMotivo.Focus();
                    txtMotivo.SelectAll();
                    return;
                }

                Motivo = txtMotivo.Text.Trim();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            // Eventos de teclado
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    btnCancelar.PerformClick();
                }
            };

            // CORREGIDO: Configurar accept/cancel buttons
            this.AcceptButton = btnEliminar;
            this.CancelButton = btnCancelar;

            // Configurar tab order
            int tabIndex = 0;
            rdoEliminarTodo.TabIndex = tabIndex++;
            rdoEliminarParcial.TabIndex = tabIndex++;
            numCantidad.TabIndex = tabIndex++;
            txtMotivo.TabIndex = tabIndex++;
            btnEliminar.TabIndex = tabIndex++;
            btnCancelar.TabIndex = tabIndex++;

            // Enfocar el campo de motivo inicialmente
            this.Load += (s, e) => txtMotivo.Focus();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ResumeLayout(false);
        }
    }
}