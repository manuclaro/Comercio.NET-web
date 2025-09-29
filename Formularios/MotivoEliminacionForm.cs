using System;
using System.Drawing;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class MotivoEliminacionForm : Form
    {
        public string Motivo { get; private set; }
        public int CantidadAEliminar { get; private set; }
        
        private string DescripcionProducto;
        private int CantidadProducto;
        private string CodigoProducto;
        private decimal PrecioProducto;

        public MotivoEliminacionForm(string descripcionProducto, int cantidadProducto, string codigoProducto, decimal precioProducto)
        {
            InitializeComponent();
            
            DescripcionProducto = descripcionProducto;
            CantidadProducto = cantidadProducto;
            CodigoProducto = codigoProducto;
            PrecioProducto = precioProducto;
            CantidadAEliminar = cantidadProducto; // Por defecto eliminar todo
            
            ConfigurarFormulario();
        }

        // NUEVO: Constructor simplificado para casos donde no necesitamos toda la información del producto
        public MotivoEliminacionForm() : this("Producto", 1, "000", 0)
        {
        }

        // NUEVO: Propiedad pública para acceso al motivo (manteniendo compatibilidad)
        public string MotivoSeleccionado 
        { 
            get { return Motivo; }
        }

        private void ConfigurarFormulario()
        {
            this.Text = "Motivo de Eliminación";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;
            this.KeyPreview = true;
            this.BackColor = Color.White;

            // CORREGIDO: Aumentar significativamente la altura para que todo se vea
            int baseHeight = CantidadProducto > 1 ? 420 : 380; // Aumentado considerablemente
            this.Size = new Size(480, baseHeight); // También aumenté el ancho
            this.MinimumSize = new Size(480, baseHeight);

            int yPos = 15; // Reducir margen superior
            int leftMargin = 20;
            int rightMargin = 20;
            int controlWidth = this.ClientSize.Width - leftMargin - rightMargin;

            // CORREGIDO: Panel superior más alto para mostrar toda la información
            var panelInfo = new Panel
            {
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 120), // Aumentado de 100 a 120
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblInfo = new Label
            {
                Text = $"📝 INFORMACIÓN DEL PRODUCTO\n\n" +
                       $"Código: {CodigoProducto}\n" +
                       $"Descripción: {DescripcionProducto}\n" +
                       $"Precio unitario: {PrecioProducto:C2}\n" +
                       $"Cantidad disponible: {CantidadProducto}",
                Location = new Point(10, 10),
                Size = new Size(controlWidth - 20, 100), // Aumentado para mostrar todo el texto
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(62, 80, 100),
                AutoSize = false // IMPORTANTE: Deshabilitar AutoSize para controlar el tamaño
            };
            panelInfo.Controls.Add(lblInfo);
            this.Controls.Add(panelInfo);
            yPos += 135; // Aumentado el espaciado

            // Si hay más de 1 unidad, permitir seleccionar cantidad
            TextBox txtCantidad = null;
            if (CantidadProducto > 1)
            {
                var lblCantidad = new Label
                {
                    Text = "Cantidad a eliminar:",
                    Location = new Point(leftMargin, yPos),
                    Size = new Size(120, 20),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };

                txtCantidad = new TextBox
                {
                    Name = "txtCantidad",
                    Text = CantidadProducto.ToString(),
                    Location = new Point(leftMargin + 130, yPos - 2),
                    Size = new Size(60, 25),
                    Font = new Font("Segoe UI", 10F),
                    TextAlign = HorizontalAlignment.Center,
                    BorderStyle = BorderStyle.FixedSingle
                };

                txtCantidad.KeyPress += (s, e) =>
                {
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                    {
                        e.Handled = true;
                    }
                };

                var lblTotal = new Label
                {
                    Name = "lblTotal",
                    Text = $"Total: {(CantidadProducto * PrecioProducto):C2}",
                    Location = new Point(leftMargin + 200, yPos),
                    Size = new Size(150, 20),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(220, 53, 69)
                };

                txtCantidad.TextChanged += (s, e) =>
                {
                    if (int.TryParse(txtCantidad.Text, out int cant) && cant > 0)
                    {
                        lblTotal.Text = $"Total: {(cant * PrecioProducto):C2}";
                    }
                    else
                    {
                        lblTotal.Text = "Total: $0,00";
                    }
                };

                this.Controls.AddRange(new Control[] { lblCantidad, txtCantidad, lblTotal });
                yPos += 40;
            }

            // Campo de motivo
            var lblMotivo = new Label
            {
                Text = "⚠️ Motivo de la eliminación (mínimo 5 caracteres):",
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 53, 69)
            };
            yPos += 25;

            // TextBox del motivo
            var txtMotivo = new TextBox
            {
                Name = "txtMotivo",
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 80), // Aumentado de 60 a 80 para mejor usabilidad
                Multiline = true,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Ej: Error de precio, producto dañado, cambio de cliente, etc.",
                ScrollBars = ScrollBars.Vertical
            };
            yPos += 95; // Aumentado el espaciado

            // CORREGIDO: Botones con posicionamiento seguro
            var btnCancelar = new Button
            {
                Text = "✗ Cancelar",
                Location = new Point(leftMargin + controlWidth - 215, yPos), // Más margen para seguridad
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right // IMPORTANTE: Anclar para prevenir problemas
            };
            btnCancelar.FlatAppearance.BorderSize = 0;

            var btnAceptar = new Button
            {
                Text = "✓ Continuar", // CAMBIADO: Era "✓ Eliminar"
                Location = new Point(leftMargin + controlWidth - 105, yPos), // Más margen para seguridad
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(0, 120, 215), // CAMBIADO: Era rojo, ahora azul
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right // IMPORTANTE: Anclar para prevenir problemas
            };
            btnAceptar.FlatAppearance.BorderSize = 0;

            // VERIFICACIÓN: Asegurar que los botones están dentro del área visible
            if (yPos + 35 + 20 > this.ClientSize.Height) // 35 altura botón + 20 margen inferior
            {
                this.Height = yPos + 35 + 50; // Ajustar altura automáticamente si es necesario
            }

            // Eventos
            btnCancelar.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    btnCancelar.PerformClick();
                }
            };

            // CORREGIDO: Eliminar la confirmación adicional
            btnAceptar.Click += (s, e) =>
            {
                // Validar motivo
                if (string.IsNullOrWhiteSpace(txtMotivo.Text) || txtMotivo.Text.Trim().Length < 5)
                {
                    MessageBox.Show(
                        "⚠️ Debe ingresar un motivo válido.\n\n" +
                        "El motivo debe tener al menos 5 caracteres.",
                        "Motivo requerido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    txtMotivo.Focus();
                    txtMotivo.SelectAll();
                    return;
                }

                Motivo = txtMotivo.Text.Trim();

                // Validar cantidad si es aplicable
                if (CantidadProducto > 1)
                {
                    var txtCantidadControl = this.Controls.Find("txtCantidad", true)[0] as TextBox;
                    if (!int.TryParse(txtCantidadControl.Text, out int cantidadSeleccionada) ||
                        cantidadSeleccionada <= 0 || cantidadSeleccionada > CantidadProducto)
                    {
                        MessageBox.Show(
                            $"⚠️ Cantidad inválida.\n\n" +
                            $"Debe ingresar un número entre 1 y {CantidadProducto}",
                            "Cantidad inválida",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        txtCantidadControl.Focus();
                        txtCantidadControl.SelectAll();
                        return;
                    }
                    CantidadAEliminar = cantidadSeleccionada;
                }

                // ELIMINADO: Ya no hay confirmación aquí - ir directamente a OK
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            // IMPORTANTE: Agregar los controles en el orden correcto
            this.Controls.AddRange(new Control[] { lblMotivo, txtMotivo, btnAceptar, btnCancelar });
            this.AcceptButton = btnAceptar;
            this.CancelButton = btnCancelar;

            // Configurar tab order
            int tabIndex = 0;
            if (txtCantidad != null) 
            {
                txtCantidad.TabIndex = tabIndex++;
            }
            txtMotivo.TabIndex = tabIndex++;
            btnAceptar.TabIndex = tabIndex++;
            btnCancelar.TabIndex = tabIndex++;

            // Enfocar el campo apropiado
            if (txtCantidad != null && CantidadProducto > 1)
            {
                txtCantidad.Focus();
                txtCantidad.SelectAll();
            }
            else
            {
                txtMotivo.Focus();
            }

            // DEBUG: Para verificar las dimensiones
            System.Diagnostics.Debug.WriteLine($"=== DIMENSIONES FORMULARIO ===");
            System.Diagnostics.Debug.WriteLine($"Tamaño del formulario: {this.Size}");
            System.Diagnostics.Debug.WriteLine($"Área del cliente: {this.ClientSize}");
            System.Diagnostics.Debug.WriteLine($"Posición de botones Y: {yPos}");
            System.Diagnostics.Debug.WriteLine($"Altura total necesaria: {yPos + 35 + 20}");
            System.Diagnostics.Debug.WriteLine($"=============================");
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // Configuración básica del formulario
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ResumeLayout(false);
        }
    }
}