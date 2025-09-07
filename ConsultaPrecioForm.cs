using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comercio.NET
{
    public partial class ConsultaPrecioForm : Form
    {
        private TextBox txtCodigo;
        private Label lblDescripcion;
        private Label lblPrecio;
        private Label lblStock;
        private Label lblMarca;
        private Label lblRubro;
        private Button btnConsultar;
        private Button btnCerrar;
        
        // NUEVO: Timer para búsqueda en tiempo real
        private System.Windows.Forms.Timer searchTimer;
        private string lastSearchText = "";

        public ConsultaPrecioForm()
        {
            InitializeComponent();
            ConfigurarFormulario();
        }

        private void ConfigurarFormulario()
        {
            this.Text = "Consulta Rápida de Precios";
            this.Size = new Size(550, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.WhiteSmoke;
            this.Font = new Font("Segoe UI", 10F);

            CrearControles();
            ConfigurarEventos();
            ConfigurarAtajosTeclado();
        }

        private void CrearControles()
        {
            int margin = 20;
            int currentY = margin;
            int labelHeight = 25;
            int spacing = 10;

            // Título
            var lblTitulo = new Label
            {
                Text = "🔍 Consulta Rápida de Precios",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(margin, currentY),
                Size = new Size(500, 30), // Aumentado ancho
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitulo);
            currentY += 40;

            // Campo código con mejor posicionamiento:
            var lblCodigo = new Label
            {
                Text = "Código de producto:", // Se muestra el texto completo
                Location = new Point(margin, currentY + 5), // +5 para centrar verticalmente
                Size = new Size(160, labelHeight), // Aumentado ancho para mostrar "producto"
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblCodigo);

            txtCodigo = new TextBox
            {
                Location = new Point(margin + 160, currentY), // Ajustado espacio para mayor separación
                Size = new Size(200, 28), // Aumentado altura
                Font = new Font("Segoe UI", 12F),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txtCodigo);

            btnConsultar = new Button
            {
                Text = "Consultar",
                Location = new Point(txtCodigo.Right + 10, currentY), // Misma Y que txtCodigo
                Size = new Size(80, 28), // Misma altura que txtCodigo
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnConsultar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnConsultar);
            currentY += 40; // Aumentado el espaciado

            // Panel de resultados - se ajusta posición y tamaño
            var panelResultados = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(this.Width - (margin * 2) - 20, 110), // Se reduce ancho para evitar overflow
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelResultados);

            // Labels de información del producto
            int panelMargin = 15;
            int panelY = panelMargin;

            lblDescripcion = new Label
            {
                Text = "Escriba un código para buscar...",
                Location = new Point(panelMargin, panelY),
                Size = new Size(panelResultados.Width - (panelMargin * 2), 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.Gray
            };
            panelResultados.Controls.Add(lblDescripcion);
            panelY += 22;

            lblPrecio = new Label
            {
                Text = "Precio: -",
                Location = new Point(panelMargin, panelY),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 150, 136)
            };
            panelResultados.Controls.Add(lblPrecio);

            lblStock = new Label
            {
                Text = "Stock: -",
                Location = new Point(panelMargin + 220, panelY),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 10F)
            };
            panelResultados.Controls.Add(lblStock);
            panelY += 22;

            lblMarca = new Label
            {
                Text = "Marca: -",
                Location = new Point(panelMargin, panelY),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 10F)
            };
            panelResultados.Controls.Add(lblMarca);

            lblRubro = new Label
            {
                Text = "Rubro: -",
                Location = new Point(panelMargin + 220, panelY),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 10F)
            };
            panelResultados.Controls.Add(lblRubro);

            // Botón cerrar - Se reubica para mostrarse entero
            btnCerrar = new Button
            {
                Text = "Cerrar",
                Location = new Point(this.Width - 100, this.Height - 80), // Y ajustado para estar más arriba
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCerrar);
        }

        private void ConfigurarEventos()
        {
            // Evento para consultar
            btnConsultar.Click += async (s, e) => await ConsultarProducto();
            
            // Evento para cerrar
            btnCerrar.Click += (s, e) => this.Close();
            
            // MODIFICADO: Solo Enter, sin consulta automática
            txtCodigo.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await ConsultarProducto();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    this.Close();
                }
            };

            // NUEVO: Configurar búsqueda en tiempo real
            ConfigurarBusquedaTiempoReal();

            // Focus al cargar
            this.Load += (s, e) => txtCodigo.Focus();
        }

        private void ConfigurarBusquedaTiempoReal()
        {
            // Configurar timer para búsqueda
            searchTimer = new System.Windows.Forms.Timer();
            searchTimer.Interval = 500; // 500ms de retraso
            searchTimer.Tick += SearchTimer_Tick;

            // Evento TextChanged para activar el timer
            txtCodigo.TextChanged += (s, e) =>
            {
                // Detener el timer anterior
                searchTimer?.Stop();
                
                string currentText = txtCodigo.Text.Trim();
                
                // Si está vacío, limpiar resultados inmediatamente
                if (string.IsNullOrEmpty(currentText))
                {
                    LimpiarResultados();
                    lastSearchText = "";
                    return;
                }
                
                // Si el texto cambió, iniciar búsqueda con retraso
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
            
            // Solo buscar si el texto realmente cambió
            if (currentText != lastSearchText && !string.IsNullOrEmpty(currentText))
            {
                lastSearchText = currentText;
                await ConsultarProductoSilencioso();
            }
        }

        private void ConfigurarAtajosTeclado()
        {
            this.KeyPreview = true; // Importante: permite que el formulario capture las teclas
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F9)
                {
                    e.SuppressKeyPress = true;
                    // Alternar el estado del checkbox
                    //chkCantidad.Checked = !chkCantidad.Checked;
                }
                else if (e.KeyCode == Keys.F6)
                {
                    e.SuppressKeyPress = true;
                    // Abrir consulta rápida de precios
                    AbrirConsultaRapidaPrecios();
                }
            };
        }

        private async Task ConsultarProducto()
        {
            string codigo = txtCodigo.Text.Trim();
            
            if (string.IsNullOrEmpty(codigo))
            {
                MessageBox.Show("Ingrese un código de producto.", "Información", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCodigo.Focus();
                return;
            }

            // Procesar código especial si es necesario (similar al formulario principal)
            string codigoBuscado = ProcesarCodigo(codigo);

            try
            {
                var producto = await BuscarProductoAsync(codigoBuscado);
                
                if (producto != null)
                {
                    MostrarResultados(producto);
                }
                else
                {
                    MostrarProductoNoEncontrado(codigoBuscado);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar producto: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ConsultarProductoSilencioso()
        {
            string codigo = txtCodigo.Text.Trim();
            
            if (string.IsNullOrEmpty(codigo))
            {
                LimpiarResultados();
                return;
            }

            // Procesar código especial si es necesario
            string codigoBuscado = ProcesarCodigo(codigo);

            try
            {
                var producto = await BuscarProductoAsync(codigoBuscado);
                
                if (producto != null)
                {
                    MostrarResultados(producto);
                }
                else
                {
                    MostrarProductoNoEncontrado(codigoBuscado);
                }
            }
            catch
            {
                // SILENCIOSO: No mostrar errores en búsqueda automática
                MostrarProductoNoEncontrado(codigoBuscado);
            }
        }

        private string ProcesarCodigo(string codigo)
        {
            // Eliminar ceros a la izquierda
            string codigoBuscado = codigo.TrimStart('0');
            if (string.IsNullOrEmpty(codigoBuscado))
                codigoBuscado = "0";

            // Si es código especial (formato 50XXXXX...), extraer el código del producto
            if (codigo.StartsWith("50") && codigo.Length == 13)
            {
                try
                {
                    string codigoProducto = codigo.Substring(2, 5).TrimStart('0');
                    if (string.IsNullOrEmpty(codigoProducto))
                        codigoProducto = "0";
                    codigoBuscado = codigoProducto;
                }
                catch
                {
                    // Si hay error, usar el código original procesado
                }
            }

            return codigoBuscado;
        }

        private async Task<DataRow> BuscarProductoAsync(string codigo)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            
            string connectionString = config.GetConnectionString("DefaultConnection");

            using var connection = new SqlConnection(connectionString);
            var query = @"SELECT codigo, descripcion, precio, cantidad, marca, rubro, costo, proveedor 
                         FROM Productos WHERE codigo = @codigo";

            using var adapter = new SqlDataAdapter(query, connection);
            adapter.SelectCommand.Parameters.AddWithValue("@codigo", codigo);

            var dt = new DataTable();
            adapter.Fill(dt);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        private void MostrarResultados(DataRow producto)
        {
            lblDescripcion.Text = $"Descripción: {producto["descripcion"]}";
            lblPrecio.Text = $"Precio: {Convert.ToDecimal(producto["precio"]):C2}";
            lblStock.Text = $"Stock: {producto["cantidad"]}";
            lblMarca.Text = $"Marca: {producto["marca"]}";
            lblRubro.Text = $"Rubro: {producto["rubro"]}";

            // Cambiar color del stock según la cantidad
            if (int.TryParse(producto["cantidad"].ToString(), out int stock))
            {
                if (stock <= 5)
                {
                    lblStock.ForeColor = Color.Red;
                    lblStock.Font = new Font(lblStock.Font, FontStyle.Bold);
                }
                else if (stock <= 10)
                {
                    lblStock.ForeColor = Color.Orange;
                    lblStock.Font = new Font(lblStock.Font, FontStyle.Bold);
                }
                else
                {
                    lblStock.ForeColor = Color.Green;
                    lblStock.Font = new Font(lblStock.Font, FontStyle.Regular);
                }
            }
        }

        private void MostrarProductoNoEncontrado(string codigo)
        {
            lblDescripcion.Text = $"❌ Producto no encontrado (código: {codigo})";
            lblDescripcion.ForeColor = Color.Red;
            lblPrecio.Text = "Precio: -";
            lblStock.Text = "Stock: -";
            lblMarca.Text = "Marca: -";
            lblRubro.Text = "Rubro: -";
        }

        private void LimpiarResultados()
        {
            lblDescripcion.Text = "Escriba un código para buscar...";
            lblDescripcion.ForeColor = Color.Gray;
            lblPrecio.Text = "Precio: -";
            lblStock.Text = "Stock: -";
            lblStock.ForeColor = Color.Black;
            lblStock.Font = new Font(lblStock.Font, FontStyle.Regular);
            lblMarca.Text = "Marca: -";
            lblRubro.Text = "Rubro: -";
        }

        private void AbrirConsultaRapidaPrecios()
        {
            using (var consultaForm = new ConsultaPrecioForm())
            {
                consultaForm.ShowDialog(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Limpiar el timer
                searchTimer?.Stop();
                searchTimer?.Dispose();
                searchTimer = null;
                
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}