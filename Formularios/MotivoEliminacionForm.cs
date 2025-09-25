using System;
using System.Drawing;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public class MotivoEliminacionForm : Form
    {
        public string Motivo { get; private set; }

        // NUEVO: Propiedades para almacenar información del producto
        public string DescripcionProducto { get; set; }
        public int CantidadProducto { get; set; }
        public string CodigoProducto { get; set; }
        public decimal PrecioProducto { get; set; }

        // NUEVO: Propiedad para la cantidad a eliminar (puede ser diferente a la total)
        public int CantidadAEliminar { get; private set; }

        // NUEVO: Constructor que acepta información del producto
        public MotivoEliminacionForm(string descripcion = "", int cantidad = 0, string codigo = "", decimal precio = 0)
        {
            DescripcionProducto = descripcion;
            CantidadProducto = cantidad;
            CodigoProducto = codigo;
            PrecioProducto = precio;
            CantidadAEliminar = cantidad; // Por defecto, eliminar toda la cantidad

            ConfigurarFormulario();
        }

        // MANTENIDO: Constructor sin parámetros para compatibilidad
        public MotivoEliminacionForm()
        {
            ConfigurarFormulario();
        }

        private void ConfigurarFormulario()
        {
            this.Text = "Motivo de Eliminación";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(250, 252, 254);

            int yPosition = 15;
            const int margen = 30; // AUMENTADO: Más margen
            const int espaciado = 10;
            int formWidth = 550; // AUMENTADO: De 520 a 650

            // NUEVO: Panel de información del producto
            if (!string.IsNullOrEmpty(DescripcionProducto))
            {
                var panelProducto = new Panel
                {
                    Location = new Point(margen, yPosition),
                    Size = new Size(formWidth - (margen * 2), 90), // AUMENTADO: De 90 a 100
                    BackColor = Color.FromArgb(240, 248, 255),
                    BorderStyle = BorderStyle.FixedSingle
                };

                var lblTitulo = new Label
                {
                    Text = "🗑️ PRODUCTO A ELIMINAR:",
                    Location = new Point(10, 5),
                    Size = new Size(panelProducto.Width - 20, 20),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(220, 53, 69)
                };

                var lblDescripcion = new Label
                {
                    Text = $"📦 {DescripcionProducto}",
                    Location = new Point(10, 25),
                    Size = new Size(panelProducto.Width - 20, 20),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(62, 80, 100)
                };

                var lblDetalles = new Label
                {
                    Text = $"🔢 Código: {CodigoProducto} | 💰 Precio: {PrecioProducto:C2}",
                    Location = new Point(10, 45),
                    Size = new Size(280, 20),
                    Font = new Font("Segoe UI", 8.5F),
                    ForeColor = Color.FromArgb(102, 125, 139)
                };

                var lblCantidadDisponible = new Label
                {
                    Text = $"📊 Cantidad en venta: {CantidadProducto}",
                    Location = new Point(10, 65),
                    Size = new Size(200, 20),
                    Font = new Font("Segoe UI", 8.5F),
                    ForeColor = Color.FromArgb(102, 125, 139)
                };

                panelProducto.Controls.AddRange(new Control[] { lblTitulo, lblDescripcion, lblDetalles, lblCantidadDisponible });
                this.Controls.Add(panelProducto);

                yPosition += 105; // AUMENTADO: De 105 a 115
            }

            // NUEVO: Panel de selección de cantidad (solo si la cantidad es mayor a 1)
            if (CantidadProducto > 1)
            {
                var panelCantidad = new Panel
                {
                    Location = new Point(margen, yPosition),
                    Size = new Size(formWidth - (margen * 2), 70), // AUMENTADO: De 70 a 80
                    BackColor = Color.FromArgb(255, 248, 225),
                    BorderStyle = BorderStyle.FixedSingle
                };

                var lblTituloCantidad = new Label
                {
                    Text = "📊 SELECCIONAR CANTIDAD A ELIMINAR:",
                    Location = new Point(10, 5),
                    Size = new Size(panelCantidad.Width - 20, 20),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(255, 111, 0)
                };

                var lblCantidad = new Label
                {
                    Text = "Cantidad:",
                    Location = new Point(10, 30), // AJUSTADO: De 30 a 35
                    Size = new Size(70, 25),
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.FromArgb(62, 80, 100),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                var txtCantidad = new TextBox
                {
                    Name = "txtCantidad",
                    Location = new Point(85, 30), // AJUSTADO: De 30 a 35
                    Size = new Size(60, 25),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    TextAlign = HorizontalAlignment.Center,
                    Text = CantidadProducto.ToString(), // PRECARGADO con la cantidad total
                    MaxLength = 3
                };

                var lblDe = new Label
                {
                    Text = $"de {CantidadProducto}",
                    Location = new Point(155, 30), // AJUSTADO: De 30 a 35
                    Size = new Size(60, 25),
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.FromArgb(102, 125, 139),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                var btnMaximo = new Button
                {
                    Text = "Todo",
                    Location = new Point(230, 28), // AJUSTADO: De 28 a 33
                    Size = new Size(50, 29),
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(255, 152, 0),
                    ForeColor = Color.White,
                    Cursor = Cursors.Hand
                };
                btnMaximo.FlatAppearance.BorderSize = 0;

                var lblTotal = new Label
                {
                    Name = "lblTotalEliminar",
                    Text = $"💵 Total a eliminar: {(CantidadProducto * PrecioProducto):C2}",
                    Location = new Point(290, 30), // AJUSTADO: De 30 a 35
                    Size = new Size(panelCantidad.Width - 300, 25),
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(220, 53, 69),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                // EVENTOS para el control de cantidad
                txtCantidad.KeyPress += (s, e) =>
                {
                    if (char.IsControl(e.KeyChar)) return;
                    if (!char.IsDigit(e.KeyChar)) e.Handled = true;
                };

                txtCantidad.TextChanged += (s, e) =>
                {
                    if (int.TryParse(txtCantidad.Text, out int cantidad) && cantidad > 0 && cantidad <= CantidadProducto)
                    {
                        CantidadAEliminar = cantidad;
                        lblTotal.Text = $"💵 Total a eliminar: {(cantidad * PrecioProducto):C2}";
                        txtCantidad.BackColor = Color.White;
                    }
                    else if (!string.IsNullOrEmpty(txtCantidad.Text))
                    {
                        txtCantidad.BackColor = Color.FromArgb(255, 235, 238);
                        lblTotal.Text = $"💵 Total a eliminar: --";
                    }
                };

                btnMaximo.Click += (s, e) =>
                {
                    txtCantidad.Text = CantidadProducto.ToString();
                    txtCantidad.Focus();
                    txtCantidad.SelectAll();
                };

                // Hover effects
                btnMaximo.MouseEnter += (s, e) => btnMaximo.BackColor = Color.FromArgb(245, 124, 0);
                btnMaximo.MouseLeave += (s, e) => btnMaximo.BackColor = Color.FromArgb(255, 152, 0);

                panelCantidad.Controls.AddRange(new Control[] { lblTituloCantidad, lblCantidad, txtCantidad, lblDe, btnMaximo, lblTotal });
                this.Controls.Add(panelCantidad);

                yPosition += 80; // AUMENTADO: De 80 a 95
            }

            // Etiqueta del motivo
            var lblMotivo = new Label
            {
                Text = "📝 Ingrese el motivo de la eliminación:",
                Location = new Point(margen, yPosition),
                Size = new Size(formWidth - (margen * 2), 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100)
            };

            yPosition += 25;

            // TextBox del motivo - AUMENTADO significativamente
            var txtMotivo = new TextBox
            {
                Location = new Point(margen, yPosition),
                Size = new Size(formWidth - (margen * 2), 60), // AUMENTADO: De 60 a 120
                Multiline = true,
                Name = "txtMotivo",
                Font = new Font("Segoe UI", 10F), // AUMENTADO: De 9F a 10F
                BackColor = Color.White,
                ForeColor = Color.FromArgb(62, 80, 100),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "Ej: Error en venta, producto dañado, cambio de precio, etc."
            };

            yPosition += 60; // AUMENTADO: De 60 a 135

            // CORREGIDO: Botones con posicionamiento correcto
            var btnAceptar = new Button
            {
                Text = "✅ Confirmar Eliminación",
                Location = new Point(margen + 200, yPosition + 5), // AJUSTADO: Más centrado
                Size = new Size(150, 30), // AUMENTADO: De 35 a 40
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnAceptar.FlatAppearance.BorderSize = 0;

            var btnCancelar = new Button
            {
                Text = "❌ Cancelar",
                Location = new Point(btnAceptar.Right + 15, yPosition + 5), // AUMENTADO: Más separación
                Size = new Size(100, 30), // AUMENTADO: De 35 a 40
                DialogResult = DialogResult.Cancel,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;

            // CORREGIDO: Ajustar el tamaño del formulario para que los botones se vean completamente
            int formHeight = yPosition + 80; // AUMENTADO: De 70 a 80
            this.Size = new Size(formWidth, formHeight);

            // Eventos de hover para los botones
            btnAceptar.MouseEnter += (s, e) => btnAceptar.BackColor = Color.FromArgb(200, 43, 59);
            btnAceptar.MouseLeave += (s, e) => btnAceptar.BackColor = Color.FromArgb(220, 53, 69);

            btnCancelar.MouseEnter += (s, e) => btnCancelar.BackColor = Color.FromArgb(138, 138, 138);
            btnCancelar.MouseLeave += (s, e) => btnCancelar.BackColor = Color.FromArgb(158, 158, 158);

            // MEJORADO: Validación del motivo
            btnAceptar.Click += (s, e) =>
            {
                Motivo = txtMotivo.Text.Trim();
                if (string.IsNullOrEmpty(Motivo))
                {
                    MessageBox.Show(
                        "⚠️ Debe ingresar un motivo para la eliminación.\n\n" +
                        "El motivo es obligatorio para mantener un registro de auditoría.",
                        "Motivo requerido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    txtMotivo.Focus();
                    return;
                }

                // NUEVO: Validación de longitud mínima
                if (Motivo.Length < 5)
                {
                    MessageBox.Show(
                        "⚠️ El motivo debe tener al menos 5 caracteres.\n\n" +
                        "Por favor, proporcione una descripción más detallada.",
                        "Motivo muy corto",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    txtMotivo.Focus();
                    txtMotivo.SelectAll();
                    return;
                }

                // NUEVO: Validar cantidad si es aplicable
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

                // NUEVO: Confirmación final de eliminación
                string mensajeConfirmacion;
                if (CantidadProducto > 1)
                {
                    mensajeConfirmacion = $"⚠️ CONFIRMACIÓN FINAL\n\n" +
                        $"¿Está seguro que desea eliminar este producto?\n\n" +
                        $"Producto: {DescripcionProducto}\n" +
                        $"Cantidad a eliminar: {CantidadAEliminar} de {CantidadProducto}\n" +
                        $"Total a eliminar: {(CantidadAEliminar * PrecioProducto):C2}\n" +
                        $"Motivo: {Motivo}\n\n" +
                        "Esta acción quedará registrada en auditoría.";
                }
                else
                {
                    mensajeConfirmacion = $"⚠️ CONFIRMACIÓN FINAL\n\n" +
                        $"¿Está seguro que desea eliminar este producto?\n\n" +
                        $"Producto: {DescripcionProducto}\n" +
                        $"Cantidad: {CantidadProducto}\n" +
                        $"Motivo: {Motivo}\n\n" +
                        "Esta acción quedará registrada en auditoría.";
                }

                var confirmacion = MessageBox.Show(
                    mensajeConfirmacion,
                    "Confirmar Eliminación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2); // Botón "No" por defecto

                if (confirmacion == DialogResult.Yes)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                // Si dice "No", no cerrar el formulario
            };

            this.Controls.AddRange(new Control[] { lblMotivo, txtMotivo, btnAceptar, btnCancelar });
            this.AcceptButton = btnAceptar;
            this.CancelButton = btnCancelar;

            txtMotivo.Focus();
        }

        private void InitializeComponent()
        {

        }
    }
}