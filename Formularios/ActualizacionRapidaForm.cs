using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;

namespace Comercio.NET.Formularios
{
    public partial class ActualizacionRapidaForm : Form
    {
        private static ActualizacionRapidaForm instanciaActual = null;

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

        // Controles de actualización
        private RadioButton rbActualizarCosto;
        private RadioButton rbActualizarPrecio;
        private GroupBox grpTipoActualizacion;
        private Label lblCostoActual;
        private Label lblPorcentajeGanancia;
        private TextBox txtNuevoCosto;

        // ✅ NUEVO: Checkbox para sumar stock
        private CheckBox chkSumarStock;
        private Label lblInfoStock;

        // Variables de estado
        //private System.Windows.Forms.Timer searchTimer;
        private string lastSearchText = "";
        private string codigoActual = "";
        private decimal costoActual = 0;
        private decimal porcentajeActual = 0;
        private decimal precioActual = 0;
        private int stockActual = 0; // ✅ NUEVO: Variable para guardar stock actual

        // ✅ NUEVO: Método estático para mostrar el formulario
        public static void MostrarFormulario(Form mdiParent)
        {
            // Si ya existe una instancia, traerla al frente
            if (instanciaActual != null && !instanciaActual.IsDisposed)
            {
                instanciaActual.Focus();
                instanciaActual.BringToFront();
                if (instanciaActual.WindowState == FormWindowState.Minimized)
                    instanciaActual.WindowState = FormWindowState.Normal;
                return;
            }

            // Crear nueva instancia
            instanciaActual = new ActualizacionRapidaForm
            {
                MdiParent = mdiParent
            };

            instanciaActual.Show();
        }

        public ActualizacionRapidaForm()
        {
            InitializeComponent();
            ConfigurarFormulario();
        }

        private void ConfigurarFormulario()
        {
            this.Text = "⚡ Actualización Rápida de Precios y Stock";
            this.Size = new Size(620, 640); // ✅ CAMBIO: Reducir altura
            this.StartPosition = FormStartPosition.CenterScreen; // ✅ CAMBIO
            this.FormBorderStyle = FormBorderStyle.Sizable; // ✅ CAMBIO: Permitir redimensionar
            this.MaximizeBox = true; // ✅ CAMBIO
            this.MinimizeBox = true; // ✅ CAMBIO
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = new Font("Segoe UI", 10F);
            this.KeyPreview = true;

            CrearControles();
            ConfigurarEventos();
            ConfigurarBusquedaTiempoReal();
        }

        // ✅ NUEVO: Limpiar instancia al cerrar
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            instanciaActual = null;
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
                Text = "Actualice precios, costos y stock de forma rápida y eficiente",
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
                Size = new Size(this.ClientSize.Width - (margin * 2), 110), // ✅ CAMBIO: de 130 a 110
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

            lblCostoActual = new Label
            {
                Text = "💵 Costo actual: $0.00",
                Location = new Point(15, 45),
                Size = new Size(250, 22),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(244, 67, 54)
            };
            panelInfo.Controls.Add(lblCostoActual);

            lblPrecioActual = new Label
            {
                Text = "💰 Precio actual: $0.00",
                Location = new Point(280, 45), // ✅ CAMBIO: al lado del costo
                Size = new Size(250, 22),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(0, 150, 136)
            };
            panelInfo.Controls.Add(lblPrecioActual);

            lblPorcentajeGanancia = new Label
            {
                Text = "📊 Ganancia: 0%",
                Location = new Point(15, 75), // ✅ CAMBIO: debajo del costo
                Size = new Size(250, 22),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(156, 39, 176)
            };
            panelInfo.Controls.Add(lblPorcentajeGanancia);

            lblStockActual = new Label
            {
                Text = "📦 Stock actual: 0",
                Location = new Point(280, 75), // ✅ CAMBIO: al lado del porcentaje
                Size = new Size(250, 22),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(255, 152, 0)
            };
            panelInfo.Controls.Add(lblStockActual);

            currentY += 130; // ✅ CAMBIO: de 150 a 130

            // ======================================
            // SELECTOR DE TIPO DE ACTUALIZACIÓN
            // ======================================
            grpTipoActualizacion = new GroupBox
            {
                Text = "⚙️ Tipo de Actualización",
                Location = new Point(margin, currentY),
                Size = new Size(this.ClientSize.Width - (margin * 2), 90), // ✅ CAMBIO: Aumentar altura de 60 a 90
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Visible = false
            };
            this.Controls.Add(grpTipoActualizacion);

            rbActualizarCosto = new RadioButton
            {
                Text = "💵 Actualizar COSTO (calcula precio automáticamente)",
                Location = new Point(15, 25),
                Size = new Size(330, 25),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(62, 80, 100),
                Checked = false
            };
            grpTipoActualizacion.Controls.Add(rbActualizarCosto);

            rbActualizarPrecio = new RadioButton
            {
                Text = "💰 Actualizar PRECIO DE VENTA",
                Location = new Point(350, 25),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(62, 80, 100),
                Checked = true
            };
            grpTipoActualizacion.Controls.Add(rbActualizarPrecio);

            // ✅ NUEVO: Checkbox AHORA dentro del GroupBox
            chkSumarStock = new CheckBox
            {
                Text = "➕ SUMAR al stock actual (en lugar de reemplazar)",
                Location = new Point(15, 55),
                Size = new Size(grpTipoActualizacion.Width - 30, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Checked = false,
                Cursor = Cursors.Hand,
                Visible = true // ✅ CAMBIO: Ahora siempre visible
            }            ;
            grpTipoActualizacion.Controls.Add(chkSumarStock);// ✅ Agregar al GroupBox

            currentY += 110; // ✅ CAMBIO: Aumentar de 80 a 110

            // ✅ NUEVO: Label informativo de stock (fuera del panel)
            lblInfoStock = new Label
            {
                Name = "lblInfoStock",
                Text = "ℹ️ La cantidad REEMPLAZARÁ el stock actual",
                Location = new Point(margin + 15, currentY),
                Size = new Size(this.ClientSize.Width - (margin * 2) - 30, 20),
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(255, 152, 0),
                Visible = false
            };
            this.Controls.Add(lblInfoStock); // ✅ Agregar al Form, no al panel

            currentY += 30; // Espacio para el label


            // ======================================
            // PANEL DE EDICIÓN
            // ======================================
            panelEdicion = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(this.ClientSize.Width - (margin * 2), 140), // ✅ CAMBIO: Reducir altura de 180 a 140
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

            // Campo Costo
            var lblNuevoCosto = new Label
            {
                Text = "💵 Nuevo Costo:",
                Location = new Point(15, 45),
                Size = new Size(110, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelEdicion.Controls.Add(lblNuevoCosto);

            txtNuevoCosto = new TextBox
            {
                Location = new Point(130, 43),
                Size = new Size(150, 28),
                Font = new Font("Segoe UI", 11F),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "0.00",
                Visible = false
            };
            panelEdicion.Controls.Add(txtNuevoCosto);

            // Campo Precio
            var lblNuevoPrecio = new Label
            {
                Text = "💰 Nuevo Precio:",
                Location = new Point(15, 45),
                Size = new Size(110, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelEdicion.Controls.Add(lblNuevoPrecio);

            txtNuevoPrecio = new TextBox
            {
                Location = new Point(130, 43),
                Size = new Size(150, 28),
                Font = new Font("Segoe UI", 11F),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "0.00"
            };
            panelEdicion.Controls.Add(txtNuevoPrecio);

            // Campo Stock
            var lblNuevoStock = new Label
            {
                Text = "📦 Cantidad:",
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

           

            // Label informativo para precio calculado
            var lblInfoCalculo = new Label
            {
                Name = "lblInfoCalculo",
                Text = "ℹ️ El precio se calculará automáticamente al guardar",
                Location = new Point(15, 80),
                Size = new Size(panelEdicion.Width - 30, 20),
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(33, 150, 243),
                Visible = false
            };
            panelEdicion.Controls.Add(lblInfoCalculo);

            // Botones de acción
            btnGuardar = new Button
            {
                Text = "💾 Guardar",
                Location = new Point(15, 95),
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
                Location = new Point(145, 95),
                Size = new Size(100, 38),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLimpiar.FlatAppearance.BorderSize = 0;
            panelEdicion.Controls.Add(btnLimpiar);

            currentY += 150;

            // ======================================
            // BOTÓN CERRAR
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
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom // ✅ Importante para que se ajuste al redimensionar
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCerrar);
        }

        private void ConfigurarEventos()
        {
            // Eventos de texto
            txtNuevoPrecio.KeyPress += TxtPrecio_KeyPress;
            txtNuevoPrecio.KeyDown += Control_KeyDown;
            txtNuevoCosto.KeyPress += TxtPrecio_KeyPress;
            txtNuevoCosto.KeyDown += Control_KeyDown;
            txtCodigo.KeyDown += Control_KeyDown;
            txtNuevoStock.KeyPress += TxtStock_KeyPress;
            txtNuevoStock.KeyDown += Control_KeyDown;

            // Eventos de RadioButtons
            rbActualizarCosto.CheckedChanged += RadioButton_CheckedChanged;
            rbActualizarPrecio.CheckedChanged += RadioButton_CheckedChanged;

            // ✅ NUEVO: Evento del Checkbox
            chkSumarStock.CheckedChanged += ChkSumarStock_CheckedChanged;

            // Eventos de botones
            btnGuardar.Click += async (s, e) => await GuardarCambios();
            btnLimpiar.Click += (s, e) => LimpiarFormulario();
            btnCerrar.Click += (s, e) => this.Close();

            // Foco inicial
            this.Load += (s, e) => txtCodigo.Focus();

            // Teclas globales
            this.KeyDown += Form_KeyDown;
        }

        // ✅ NUEVO: Manejador del Checkbox
        private void ChkSumarStock_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSumarStock.Checked)
            {
                lblInfoStock.Text = "➕ La cantidad se SUMARÁ al stock actual";
                lblInfoStock.ForeColor = Color.FromArgb(76, 175, 80);

                // ✅ CAMBIO: Solo actualizar si hay un producto cargado
                if (!string.IsNullOrEmpty(codigoActual))
                {
                    txtNuevoStock.Text = "0";
                    txtNuevoStock.BackColor = Color.FromArgb(232, 245, 233);
                }
            }
            else
            {
                lblInfoStock.Text = "ℹ️ La cantidad REEMPLAZARÁ el stock actual";
                lblInfoStock.ForeColor = Color.FromArgb(255, 152, 0);

                // ✅ CAMBIO: Solo actualizar si hay un producto cargado
                if (!string.IsNullOrEmpty(codigoActual))
                {
                    txtNuevoStock.Text = stockActual.ToString();
                    txtNuevoStock.BackColor = Color.White;
                }
            }
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (rbActualizarCosto.Checked)
            {
                txtNuevoCosto.Visible = true;
                txtNuevoPrecio.Visible = false;

                var lblInfo = panelEdicion.Controls.Find("lblInfoCalculo", false).FirstOrDefault();
                if (lblInfo != null)
                    lblInfo.Visible = true;

                // ✅ NUEVO: Restaurar fondo blanco del textbox stock
                txtNuevoStock.BackColor = Color.White;

                if (!string.IsNullOrEmpty(codigoActual))
                {
                    txtNuevoCosto.Text = costoActual.ToString("F2");
                    txtNuevoCosto.Focus();
                    txtNuevoCosto.SelectAll();
                }
            }
            else if (rbActualizarPrecio.Checked)
            {
                txtNuevoCosto.Visible = false;
                txtNuevoPrecio.Visible = true;

                var lblInfo = panelEdicion.Controls.Find("lblInfoCalculo", false).FirstOrDefault();
                if (lblInfo != null)
                    lblInfo.Visible = false;

                // ✅ CAMBIO: Mostrar checkbox y label
                chkSumarStock.Visible = true;
                lblInfoStock.Visible = true;

                // ✅ NUEVO: Aplicar estado del checkbox al fondo
                txtNuevoStock.BackColor = chkSumarStock.Checked
                    ? Color.FromArgb(232, 245, 233)
                    : Color.White;

                if (!string.IsNullOrEmpty(codigoActual))
                {
                    txtNuevoPrecio.Text = precioActual.ToString("F2");
                    txtNuevoPrecio.Focus();
                    txtNuevoPrecio.SelectAll();
                }
            }
        }

        private void ConfigurarBusquedaTiempoReal()
        {
            // ✅ Búsqueda solo al perder el foco o presionar ENTER

            // Buscar cuando pierde el foco
            txtCodigo.Leave += async (s, e) =>
            {
                string currentText = txtCodigo.Text.Trim();

                if (!string.IsNullOrEmpty(currentText) && currentText != lastSearchText)
                {
                    lastSearchText = currentText;
                    await BuscarProductoAsync(currentText);
                }
            };

            // También buscar con ENTER (ya está manejado en Control_KeyDown)
        }

        //private async void SearchTimer_Tick(object sender, EventArgs e)
        //{
        //    searchTimer?.Stop();
        //    string currentText = txtCodigo.Text.Trim();

        //    if (currentText != lastSearchText && !string.IsNullOrEmpty(currentText))
        //    {
        //        lastSearchText = currentText;
        //        await BuscarProductoAsync(currentText);
        //    }
        //}

        private async Task BuscarProductoAsync(string codigo)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                string codigoBuscado = ProcesarCodigo(codigo);

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"SELECT codigo, descripcion, precio, cantidad, marca, rubro, costo, porcentaje 
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
            precioActual = Convert.ToDecimal(reader["precio"]);
            stockActual = Convert.ToInt32(reader["cantidad"]); // ✅ Guardar en variable
            string marca = reader["marca"]?.ToString() ?? "";
            string rubro = reader["rubro"]?.ToString() ?? "";

            costoActual = reader["costo"] != DBNull.Value ? Convert.ToDecimal(reader["costo"]) : 0;
            porcentajeActual = reader["porcentaje"] != DBNull.Value ? Convert.ToDecimal(reader["porcentaje"]) : 0;

            // Mostrar información
            lblDescripcion.Text = $"{descripcion}";
            lblCostoActual.Text = $"💵 Costo actual: {costoActual:C2}";
            lblPorcentajeGanancia.Text = $"📊 Ganancia: {porcentajeActual:F2}%";
            lblPrecioActual.Text = $"💰 Precio actual: {precioActual:C2}";
            lblStockActual.Text = $"📦 Stock actual: {stockActual} unidades";

            // Precargar valores según el tipo de actualización seleccionado
            if (rbActualizarCosto.Checked)
            {
                txtNuevoCosto.Text = costoActual.ToString("F2");
                txtNuevoStock.Text = stockActual.ToString();
                txtNuevoCosto.Focus();
                txtNuevoCosto.SelectAll();
            }
            else
            {
                txtNuevoPrecio.Text = precioActual.ToString("F2");
                txtNuevoStock.Text = stockActual.ToString();
                txtNuevoPrecio.Focus();
                txtNuevoPrecio.SelectAll();
            }

            // ✅ NUEVO: Resetear checkbox
            //chkSumarStock.Checked = false;

            // ✅ NUEVO: Aplicar el estado del checkbox al stock
            if (rbActualizarPrecio.Checked)
            {
                if (chkSumarStock.Checked)
                {
                    txtNuevoStock.Text = "0";
                    txtNuevoStock.BackColor = Color.FromArgb(232, 245, 233);
                }
                else
                {
                    txtNuevoStock.Text = stockActual.ToString();
                    txtNuevoStock.BackColor = Color.White;
                }
            }
            else
            {
                txtNuevoStock.Text = stockActual.ToString();
                txtNuevoStock.BackColor = Color.White;
            }

            // Mostrar paneles
            panelInfo.Visible = true;
            grpTipoActualizacion.Visible = true;
            panelEdicion.Visible = true;
            lblInfoStock.Visible = true;
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
            grpTipoActualizacion.Visible = false;
            panelEdicion.Visible = false;
            lblInfoStock.Visible = false;
            codigoActual = "";
            costoActual = 0;
            porcentajeActual = 0;
            precioActual = 0;
            stockActual = 0; // ✅ Resetear
        }

        private async Task GuardarCambios()
        {
            if (string.IsNullOrEmpty(codigoActual))
            {
                MessageBox.Show("Primero busque un producto.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            decimal nuevoPrecio = 0;
            decimal nuevoCosto = 0;
            int stockFinal = 0;

            // Validar según el tipo de actualización
            if (rbActualizarCosto.Checked)
            {
                // Actualizar por COSTO
                if (!decimal.TryParse(txtNuevoCosto.Text.Replace(".", ","), NumberStyles.Number, CultureInfo.CurrentCulture, out nuevoCosto))
                {
                    MessageBox.Show("Ingrese un costo válido.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtNuevoCosto.Focus();
                    return;
                }

                if (nuevoCosto < 0)
                {
                    MessageBox.Show("El costo no puede ser negativo.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtNuevoCosto.Focus();
                    return;
                }

                // Calcular precio automáticamente
                if (porcentajeActual > 0)
                {
                    nuevoPrecio = nuevoCosto + ((nuevoCosto * porcentajeActual) / 100);
                    nuevoPrecio = Math.Round(nuevoPrecio, 2);
                }
                else
                {
                    var resultado = MessageBox.Show(
                        "⚠️ El producto no tiene un porcentaje de ganancia configurado.\n\n" +
                        $"Costo nuevo: {nuevoCosto:C2}\n\n" +
                        "¿Desea usar el precio actual como precio de venta?\n" +
                        "(O cancele para configurar el porcentaje primero)",
                        "Porcentaje de Ganancia No Configurado",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Warning);

                    if (resultado != DialogResult.OK)
                    {
                        return;
                    }

                    nuevoPrecio = precioActual;
                }
            }
            else
            {
                // Actualizar por PRECIO
                if (!decimal.TryParse(txtNuevoPrecio.Text.Replace(".", ","), NumberStyles.Number, CultureInfo.CurrentCulture, out nuevoPrecio))
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

                nuevoCosto = costoActual;
            }

            // ✅ MODIFICADO: Validar y calcular stock
            if (!int.TryParse(txtNuevoStock.Text, out int valorStock))
            {
                MessageBox.Show("Ingrese un stock válido (solo números enteros).", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNuevoStock.Focus();
                txtNuevoStock.SelectAll();
                return;
            }

            // ✅ NUEVO: Calcular stock final según checkbox
            if (chkSumarStock.Checked)
            {
                stockFinal = stockActual + valorStock;

                // Mostrar confirmación de suma
                var confirmacion = MessageBox.Show(
                    $"📦 CONFIRMAR SUMA DE STOCK:\n\n" +
                    $"Stock actual: {stockActual}\n" +
                    $"Cantidad a sumar: {valorStock:+#;-#;0}\n" +
                    $"Stock final: {stockFinal}\n\n" +
                    $"¿Desea continuar?",
                    "Confirmación de Stock",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmacion != DialogResult.Yes)
                {
                    return;
                }
            }
            else
            {
                stockFinal = valorStock;

                // Validar stock negativo solo cuando se reemplaza
                if (stockFinal < 0)
                {
                    var resultado = MessageBox.Show(
                        $"⚠️ ADVERTENCIA: Stock negativo ({stockFinal})\n\n" +
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
                             SET precio = @precio, costo = @costo, cantidad = @cantidad 
                             WHERE codigo = @codigo";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@precio", nuevoPrecio);
                cmd.Parameters.AddWithValue("@costo", nuevoCosto);
                cmd.Parameters.AddWithValue("@cantidad", stockFinal); // ✅ Usar stock final calculado
                cmd.Parameters.AddWithValue("@codigo", codigoActual);

                await cmd.ExecuteNonQueryAsync();

                ProductosOptimizado.LimpiarCache();

                // ✅ MODIFICADO: Mensaje detallado según operación
                //string mensajeStock = chkSumarStock.Checked
                //    ? $"Stock: {stockActual} + {valorStock} = {stockFinal}"
                //    : $"Stock: {stockFinal}";

                //string mensajeExito = rbActualizarCosto.Checked
                //    ? $"✅ Producto actualizado correctamente.\n\n" +
                //      $"Costo: {nuevoCosto:C2}\n" +
                //      $"Precio calculado: {nuevoPrecio:C2} (con {porcentajeActual:F2}% de ganancia)\n" +
                //      $"{mensajeStock}"
                //    : $"✅ Producto actualizado correctamente.\n\n" +
                //      $"Precio: {nuevoPrecio:C2}\n" +
                //      $"{mensajeStock}";

                //MessageBox.Show(mensajeExito,
                //    "Éxito",
                //    MessageBoxButtons.OK,
                //    MessageBoxIcon.Information);

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
            txtNuevoCosto.Clear();
            txtNuevoStock.Clear();
            // ✅ NO resetear: chkSumarStock.Checked = false;
            txtNuevoStock.BackColor = Color.White; // ✅ Resetear color
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
            // ✅ MODIFICADO: Permitir signo negativo si está sumando
            if (char.IsControl(e.KeyChar))
                return;

            TextBox tb = sender as TextBox;

            // Permitir signo negativo solo al inicio y si está sumando
            if (e.KeyChar == '-' && chkSumarStock.Checked && tb.SelectionStart == 0 && !tb.Text.Contains("-"))
            {
                return;
            }

            if (!char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
                return;
            }

            if (tb.Text.Length >= 6 && tb.SelectionLength == 0)
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
                    await BuscarProductoAsync(txtCodigo.Text.Trim());
                }
                else if (sender == txtNuevoPrecio || sender == txtNuevoCosto || sender == txtNuevoStock)
                {
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
               
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new Size(620, 600); // ✅ CAMBIO: Reducir de 640 a 600
            this.Name = "ActualizacionRapidaForm";
            this.ResumeLayout(false);
        }
    }
}