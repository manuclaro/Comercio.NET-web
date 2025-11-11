using Microsoft.Extensions.Configuration;
using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class ActualizacionRapidaForm : Form
    {
        private TextBox txtCodigo;
        private Label lblDescripcion;
        private Label lblPrecioActual;
        private Label lblStockActual;
        private TextBox txtNuevoPrecio;
        private TextBox txtNuevoStock;
        private Button btnGuardar;
        private Button btnLimpiar;
        private Button btnCerrar;
        private Panel panelInfo;
        private Panel panelEdicion;
        
        // Para búsqueda automática
        private System.Windows.Forms.Timer searchTimer;
        private string lastSearchText = "";
        private string codigoActual = "";

        public ActualizacionRapidaForm()
        {
            InitializeComponent();
            ConfigurarFormulario();
        }

        private void ConfigurarFormulario()
        {
            this.Text = "⚡ Actualización Rápida de Precios y Stock";
            // ✅ CAMBIO: Reducir altura del formulario para que el botón Cerrar sea visible
            this.Size = new Size(600, 520); // Era 480, ahora 450
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = new Font("Segoe UI", 10F);
            this.KeyPreview = true;

            CrearControles();
            ConfigurarEventos();
            ConfigurarBusquedaTiempoReal();
        }

        private void CrearControles()
        {
            int margin = 20;
            int currentY = margin;

            // ======================================
            // CABECERA
            // ======================================
            var panelHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(this.ClientSize.Width, 70),
                BackColor = Color.FromArgb(63, 81, 181)
            };
            this.Controls.Add(panelHeader);

            var lblTitulo = new Label
            {
                Text = "⚡ ACTUALIZACIÓN RÁPIDA",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(margin, 12),
                AutoSize = true
            };
            panelHeader.Controls.Add(lblTitulo);

            var lblSubtitulo = new Label
            {
                Text = "Actualice precios y stock de forma rápida y eficiente",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(200, 210, 255),
                Location = new Point(margin, 40),
                AutoSize = true
            };
            panelHeader.Controls.Add(lblSubtitulo);

            currentY = 90;

            // ======================================
            // SECCIÓN: BUSCAR PRODUCTO
            // ======================================
            var lblCodigo = new Label
            {
                Text = "🔍 Código del producto:",
                Location = new Point(margin, currentY),
                Size = new Size(190, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblCodigo);

            txtCodigo = new TextBox
            {
                Location = new Point(margin + 190, currentY),
                Size = new Size(170, 28),
                Font = new Font("Segoe UI", 12F),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Escanee o escriba..."
            };
            this.Controls.Add(txtCodigo);

            var lblAyuda = new Label
            {
                Text = "💡 El producto se buscará automáticamente",
                Location = new Point(margin + 370, currentY + 5),
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            this.Controls.Add(lblAyuda);

            currentY += 50;

            // ======================================
            // PANEL DE INFORMACIÓN DEL PRODUCTO
            // ======================================
            panelInfo = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(this.ClientSize.Width - (margin * 2), 100),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            this.Controls.Add(panelInfo);

            lblDescripcion = new Label
            {
                Text = "Descripción del producto",
                Location = new Point(15, 10),
                Size = new Size(panelInfo.Width - 30, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243)
            };
            panelInfo.Controls.Add(lblDescripcion);

            lblPrecioActual = new Label
            {
                Text = "💰 Precio actual: $0.00",
                Location = new Point(15, 45),
                Size = new Size(250, 22),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(0, 150, 136)
            };
            panelInfo.Controls.Add(lblPrecioActual);

            lblStockActual = new Label
            {
                Text = "📦 Stock actual: 0",
                Location = new Point(15, 70),
                Size = new Size(250, 22),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(255, 152, 0)
            };
            panelInfo.Controls.Add(lblStockActual);

            currentY += 120;

            // ======================================
            // PANEL DE EDICIÓN
            // ======================================
            panelEdicion = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(this.ClientSize.Width - (margin * 2), 140),
                BackColor = Color.FromArgb(232, 245, 233),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            this.Controls.Add(panelEdicion);

            var lblTituloEdicion = new Label
            {
                Text = "✏️ ACTUALIZAR VALORES",
                Location = new Point(15, 10),
                Size = new Size(panelEdicion.Width - 30, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32)
            };
            panelEdicion.Controls.Add(lblTituloEdicion);

            // Campo Precio
            var lblNuevoPrecio = new Label
            {
                Text = "💵 Nuevo Precio:",
                Location = new Point(15, 45),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelEdicion.Controls.Add(lblNuevoPrecio);

            txtNuevoPrecio = new TextBox
            {
                Location = new Point(140, 43),
                Size = new Size(150, 28),
                Font = new Font("Segoe UI", 11F),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "0.00"
            };
            panelEdicion.Controls.Add(txtNuevoPrecio);

            // Campo Stock
            var lblNuevoStock = new Label
            {
                Text = "📦 Nuevo Stock:",
                Location = new Point(310, 45),
                Size = new Size(110, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelEdicion.Controls.Add(lblNuevoStock);

            txtNuevoStock = new TextBox
            {
                Location = new Point(425, 43),
                Size = new Size(100, 28),
                Font = new Font("Segoe UI", 11F),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center,
                PlaceholderText = "0"
            };
            panelEdicion.Controls.Add(txtNuevoStock);

            // Botones de acción dentro del panel
            btnGuardar = new Button
            {
                Text = "💾 Guardar",
                Location = new Point(15, 90),
                Size = new Size(120, 38),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            panelEdicion.Controls.Add(btnGuardar);

            btnLimpiar = new Button
            {
                Text = "🔄 Limpiar",
                Location = new Point(145, 90),
                Size = new Size(100, 38),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLimpiar.FlatAppearance.BorderSize = 0;
            panelEdicion.Controls.Add(btnLimpiar);

            currentY += 160;

            // ======================================
            // BOTÓN CERRAR (inferior)
            // ======================================
            btnCerrar = new Button
            {
                Text = "❌ Cerrar",
                Location = new Point(this.ClientSize.Width - 120 - margin, currentY),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                // ✅ NUEVO: Anclar al lado derecho inferior
        Anchor = AnchorStyles.Right | AnchorStyles.Bottom
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCerrar);
        }

        private void ConfigurarEventos()
        {
            // Eventos de texto
            txtNuevoPrecio.KeyPress += TxtPrecio_KeyPress;
            txtNuevoPrecio.KeyDown += Control_KeyDown;
            txtCodigo.KeyDown += Control_KeyDown;
            txtNuevoStock.KeyPress += TxtStock_KeyPress;
            txtNuevoStock.KeyDown += Control_KeyDown;

            // Eventos de botones
            btnGuardar.Click += async (s, e) => await GuardarCambios();
            btnLimpiar.Click += (s, e) => LimpiarFormulario();
            btnCerrar.Click += (s, e) => this.Close();

            // Foco inicial
            this.Load += (s, e) => txtCodigo.Focus();

            // Teclas globales
            this.KeyDown += Form_KeyDown;
        }

        private void ConfigurarBusquedaTiempoReal()
        {
            searchTimer = new System.Windows.Forms.Timer();
            searchTimer.Interval = 500; // 500ms
            searchTimer.Tick += SearchTimer_Tick;

            txtCodigo.TextChanged += (s, e) =>
            {
                searchTimer?.Stop();
                string currentText = txtCodigo.Text.Trim();

                if (string.IsNullOrEmpty(currentText))
                {
                    OcultarInformacionProducto();
                    lastSearchText = "";
                    return;
                }

                if (currentText != lastSearchText)
                {
                    searchTimer.Start();
                }
            };
        }

        private async void SearchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer?.Stop();
            string currentText = txtCodigo.Text.Trim();

            if (currentText != lastSearchText && !string.IsNullOrEmpty(currentText))
            {
                lastSearchText = currentText;
                await BuscarProductoAsync(currentText);
            }
        }

        private async Task BuscarProductoAsync(string codigo)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                // Procesar código (similar a otros formularios)
                string codigoBuscado = ProcesarCodigo(codigo);

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"SELECT codigo, descripcion, precio, cantidad, marca, rubro 
                             FROM Productos WHERE codigo = @codigo";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@codigo", codigoBuscado);

                using var reader = await cmd.ExecuteReaderAsync();

                if (reader.Read())
                {
                    MostrarInformacionProducto(reader);
                }
                else
                {
                    MostrarProductoNoEncontrado(codigoBuscado);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error buscando producto: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private string ProcesarCodigo(string codigo)
        {
            string codigoBuscado = codigo.TrimStart('0');
            if (string.IsNullOrEmpty(codigoBuscado))
                codigoBuscado = "0";

            if (codigo.StartsWith("50") && codigo.Length == 13)
            {
                try
                {
                    string codigoProducto = codigo.Substring(2, 5).TrimStart('0');
                    if (string.IsNullOrEmpty(codigoProducto))
                        codigoProducto = "0";
                    codigoBuscado = codigoProducto;
                }
                catch { }
            }

            return codigoBuscado;
        }

        private void MostrarInformacionProducto(SqlDataReader reader)
        {
            codigoActual = reader["codigo"].ToString();
            string descripcion = reader["descripcion"].ToString();
            decimal precio = Convert.ToDecimal(reader["precio"]);
            int stock = Convert.ToInt32(reader["cantidad"]);
            string marca = reader["marca"]?.ToString() ?? "";
            string rubro = reader["rubro"]?.ToString() ?? "";

            // Mostrar información
            lblDescripcion.Text = $"{descripcion}";
            lblPrecioActual.Text = $"💰 Precio actual: {precio:C2}";
            lblStockActual.Text = $"📦 Stock actual: {stock} unidades";

            // Precargar valores
            txtNuevoPrecio.Text = precio.ToString("F2");
            txtNuevoStock.Text = stock.ToString();

            // Mostrar paneles
            panelInfo.Visible = true;
            panelEdicion.Visible = true;

            // Enfocar el precio para edición
            txtNuevoPrecio.Focus();
            txtNuevoPrecio.SelectAll();
        }

        private void MostrarProductoNoEncontrado(string codigo)
        {
            OcultarInformacionProducto();
            MessageBox.Show($"❌ No se encontró ningún producto con el código: {codigo}",
                "Producto no encontrado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void OcultarInformacionProducto()
        {
            panelInfo.Visible = false;
            panelEdicion.Visible = false;
            codigoActual = "";
        }

        private async Task GuardarCambios()
        {
            if (string.IsNullOrEmpty(codigoActual))
            {
                MessageBox.Show("Primero busque un producto.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!decimal.TryParse(txtNuevoPrecio.Text.Replace(".", ","), NumberStyles.Number, CultureInfo.CurrentCulture, out decimal nuevoPrecio))
            {
                MessageBox.Show("Ingrese un precio válido.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNuevoPrecio.Focus();
                return;
            }

            if (nuevoPrecio <= 0)
            {
                MessageBox.Show("El precio debe ser mayor a cero.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNuevoPrecio.Focus();
                return;
            }

            if (!int.TryParse(txtNuevoStock.Text, out int nuevoStock))
            {
                MessageBox.Show("Ingrese un stock válido (solo números enteros).", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNuevoStock.Focus();
                txtNuevoStock.SelectAll();
                return;
            }

            // ✅ NUEVO: Permitir stock negativo pero mostrar advertencia
            if (nuevoStock < 0)
            {
                var resultado = MessageBox.Show(
                    $"⚠️ ADVERTENCIA: Stock negativo ({nuevoStock})\n\n" +
                    "Está ingresando un valor de stock negativo.\n" +
                    "Esto puede indicar faltante o deuda de inventario.\n\n" +
                    "¿Desea continuar de todas formas?",
                    "Confirmación de Stock Negativo",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (resultado != DialogResult.Yes)
                {
                    txtNuevoStock.Focus();
                    txtNuevoStock.SelectAll();
                    return;
                }
            }

            try
            {
                this.Cursor = Cursors.WaitCursor;
                btnGuardar.Enabled = false;
                btnGuardar.Text = "⏳ Guardando...";

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"UPDATE Productos 
                             SET precio = @precio, cantidad = @cantidad 
                             WHERE codigo = @codigo";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@precio", nuevoPrecio);
                cmd.Parameters.AddWithValue("@cantidad", nuevoStock);
                cmd.Parameters.AddWithValue("@codigo", codigoActual);

                await cmd.ExecuteNonQueryAsync();

                // Limpiar cache si existe
                ProductosOptimizado.LimpiarCache();

                MessageBox.Show("✅ Producto actualizado correctamente.",
                    "Éxito",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Limpiar para siguiente producto
                LimpiarFormulario();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnGuardar.Enabled = true;
                btnGuardar.Text = "💾 Guardar";
            }
        }

        private void LimpiarFormulario()
        {
            txtCodigo.Clear();
            txtNuevoPrecio.Clear();
            txtNuevoStock.Clear();
            OcultarInformacionProducto();
            txtCodigo.Focus();
        }

        private void TxtPrecio_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '.')
                e.KeyChar = ',';

            if (char.IsControl(e.KeyChar))
                return;

            TextBox tb = sender as TextBox;
            if (char.IsDigit(e.KeyChar) || e.KeyChar == ',')
            {
                if (e.KeyChar == ',' && tb.Text.Contains(","))
                    e.Handled = true;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void TxtStock_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Permitir solo dígitos y teclas de control
            if (char.IsControl(e.KeyChar))
                return;

            // Permitir solo números
            if (!char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
                return;
            }

            TextBox tb = sender as TextBox;
            // Limitar a 4 dígitos máximo
            if (tb.Text.Length >= 4 && tb.SelectionLength == 0)
            {
                e.Handled = true;
            }
        }
        private async void Control_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                if (sender == txtCodigo)
                {
                    // Si presiona Enter en código, buscar inmediatamente
                    await BuscarProductoAsync(txtCodigo.Text.Trim());
                }
                else if (sender == txtNuevoPrecio || sender == txtNuevoStock)
                {
                    // Si presiona Enter en los campos de edición, guardar
                    await GuardarCambios();
                }
            }
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (!string.IsNullOrEmpty(codigoActual))
                {
                    LimpiarFormulario();
                }
                else
                {
                    this.Close();
                }
            }
            else if (e.Control && e.KeyCode == Keys.G)
            {
                e.SuppressKeyPress = true;
                _ = GuardarCambios();
            }
            else if (e.Control && e.KeyCode == Keys.L)
            {
                e.SuppressKeyPress = true;
                LimpiarFormulario();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                searchTimer?.Stop();
                searchTimer?.Dispose();
                searchTimer = null;
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new Size(600, 480);
            this.Name = "ActualizacionRapidaForm";
            this.ResumeLayout(false);
        }
    }
}