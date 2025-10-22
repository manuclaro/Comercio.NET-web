using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Comercio.NET.Servicios;

namespace Comercio.NET.Formularios
{
    public partial class CartelitosPrecios : Form
    {
        private List<ProductoCartelito> productosSeleccionados;
        private DataTable tablaProductos;
        
        // Controles del formulario
        private TextBox txtCodigoProducto;
        private DataGridView dgvProductosSeleccionados;
        private Label lblInstrucciones;
        private Label lblTotalProductos;
        private Button btnAgregarProducto;
        private Button btnEliminarSeleccionado;
        private Button btnLimpiarLista;
        private GroupBox gbTamañosCartel;
        private RadioButton rbTamañoEstandar;
        private RadioButton rbTamañoPerfumeria;
        private RadioButton rbTamañoOferta;
        private Button btnVistaPrevia;
        private Button btnImprimir;
        private Button btnCerrar;
        private Panel panelInferior;
        private TableLayoutPanel layoutPrincipal;

        public CartelitosPrecios()
        {
            InitializeComponent();
            productosSeleccionados = new List<ProductoCartelito>();
            ConfigurarFormulario();
            ConfigurarEventos();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configuración del formulario
            this.Text = "Generador de Cartelitos de Precios";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.WhiteSmoke;
            this.Font = new Font("Segoe UI", 10F);

            // Layout principal
            layoutPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = Color.Transparent
            };

            // Configurar columnas y filas
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            layoutPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
            layoutPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));

            // Panel de entrada de productos
            var panelEntrada = CrearPanelEntrada();
            layoutPrincipal.Controls.Add(panelEntrada, 0, 0);
            layoutPrincipal.SetColumnSpan(panelEntrada, 2);

            // DataGridView
            dgvProductosSeleccionados = CrearDataGridView();
            layoutPrincipal.Controls.Add(dgvProductosSeleccionados, 0, 1);

            // Panel de opciones
            var panelOpciones = CrearPanelOpciones();
            layoutPrincipal.Controls.Add(panelOpciones, 1, 1);

            // Panel inferior con botones
            panelInferior = CrearPanelInferior();
            layoutPrincipal.Controls.Add(panelInferior, 0, 2);
            layoutPrincipal.SetColumnSpan(panelInferior, 2);

            this.Controls.Add(layoutPrincipal);
            this.ResumeLayout(false);
        }

        private Panel CrearPanelEntrada()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 248, 255),
                Padding = new Padding(10)
            };

            // Instrucciones
            lblInstrucciones = new Label
            {
                Text = "Ingrese el código del producto y presione Enter o haga clic en Agregar:",
                Location = new Point(10, 10),
                Size = new Size(500, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 118, 210)
            };

            // TextBox para código
            txtCodigoProducto = new TextBox
            {
                Location = new Point(10, 35),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 11F),
                PlaceholderText = "Código producto..."
            };

            // Botón agregar
            btnAgregarProducto = new Button
            {
                Text = "Agregar",
                Location = new Point(170, 34),
                Size = new Size(80, 27),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnAgregarProducto.FlatAppearance.BorderSize = 0;

            // Label contador
            lblTotalProductos = new Label
            {
                Text = "Productos en lista: 0",
                Location = new Point(270, 38),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            panel.Controls.AddRange(new Control[] { 
                lblInstrucciones, txtCodigoProducto, btnAgregarProducto, lblTotalProductos 
            });

            return panel;
        }

        private DataGridView CrearDataGridView()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10, 0, 5, 0),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.White,
                GridColor = Color.FromArgb(230, 230, 230)
            };

            // Estilos
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.ColumnHeadersHeight = 35;

            dgv.DefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.ForeColor = Color.Black;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9F);

            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            dgv.RowTemplate.Height = 28;

            // Columnas
            dgv.Columns.Add("Codigo", "Código");
            dgv.Columns.Add("Descripcion", "Descripción");
            dgv.Columns.Add("Precio", "Precio");
            dgv.Columns.Add("Marca", "Marca");

            // Configurar anchos
            dgv.Columns["Codigo"].Width = 80;
            dgv.Columns["Descripcion"].Width = 300;
            dgv.Columns["Precio"].Width = 100;
            dgv.Columns["Marca"].Width = 120;

            // Formato de precio
            dgv.Columns["Precio"].DefaultCellStyle.Format = "C2";
            dgv.Columns["Precio"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            return dgv;
        }

        private Panel CrearPanelOpciones()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 0, 10, 0),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Título
            var lblTitulo = new Label
            {
                Text = "OPCIONES DE IMPRESIÓN",
                Location = new Point(10, 10),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 118, 210)
            };

            // GroupBox para tamaños
            gbTamañosCartel = new GroupBox
            {
                Text = "Tamaño del cartelito",
                Location = new Point(10, 45),
                Size = new Size(320, 150),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            // RadioButtons para tamaños
            rbTamañoEstandar = new RadioButton
            {
                Text = "Estándar (7x5 cm)\nProductos generales",
                Location = new Point(15, 25),
                Size = new Size(280, 35),
                Checked = true,
                Font = new Font("Segoe UI", 9F)
            };

            rbTamañoPerfumeria = new RadioButton
            {
                Text = "Perfumería (5x3 cm)\nProductos pequeños",
                Location = new Point(15, 65),
                Size = new Size(280, 35),
                Font = new Font("Segoe UI", 9F)
            };

            rbTamañoOferta = new RadioButton
            {
                Text = "Oferta (10x7 cm)\nProductos destacados",
                Location = new Point(15, 105),
                Size = new Size(280, 35),
                Font = new Font("Segoe UI", 9F)
            };

            gbTamañosCartel.Controls.AddRange(new Control[] { 
                rbTamañoEstandar, rbTamañoPerfumeria, rbTamañoOferta 
            });

            // Botones de acción
            btnEliminarSeleccionado = new Button
            {
                Text = "Eliminar\nSeleccionado",
                Location = new Point(15, 210),
                Size = new Size(90, 50),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            btnEliminarSeleccionado.FlatAppearance.BorderSize = 0;

            btnLimpiarLista = new Button
            {
                Text = "Limpiar\nTodo",
                Location = new Point(115, 210),
                Size = new Size(90, 50),
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            btnLimpiarLista.FlatAppearance.BorderSize = 0;

            btnVistaPrevia = new Button
            {
                Text = "Vista\nPrevia",
                Location = new Point(215, 210),
                Size = new Size(90, 50),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            btnVistaPrevia.FlatAppearance.BorderSize = 0;

            panel.Controls.AddRange(new Control[] { 
                lblTitulo, gbTamañosCartel, btnEliminarSeleccionado, 
                btnLimpiarLista, btnVistaPrevia 
            });

            return panel;
        }

        private Panel CrearPanelInferior()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(10)
            };

            // Botón imprimir
            btnImprimir = new Button
            {
                Text = "IMPRIMIR CARTELITOS",
                Location = new Point(10, 15),
                Size = new Size(180, 40),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Enabled = false
            };
            btnImprimir.FlatAppearance.BorderSize = 0;

            // Botón cerrar
            btnCerrar = new Button
            {
                Text = "CERRAR",
                Location = new Point(200, 15),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;

            panel.Controls.AddRange(new Control[] { btnImprimir, btnCerrar });

            return panel;
        }

        private void ConfigurarFormulario()
        {
            this.KeyPreview = true;
            ActualizarContador();
        }

        private void ConfigurarEventos()
        {
            // Eventos de controles
            txtCodigoProducto.KeyDown += TxtCodigoProducto_KeyDown;
            txtCodigoProducto.KeyPress += TxtCodigoProducto_KeyPress;
            btnAgregarProducto.Click += BtnAgregarProducto_Click;
            btnEliminarSeleccionado.Click += BtnEliminarSeleccionado_Click;
            btnLimpiarLista.Click += BtnLimpiarLista_Click;
            btnVistaPrevia.Click += BtnVistaPrevia_Click;
            btnImprimir.Click += BtnImprimir_Click;
            btnCerrar.Click += (s, e) => this.Close();

            // Eventos del DataGridView
            dgvProductosSeleccionados.SelectionChanged += DgvProductosSeleccionados_SelectionChanged;
            dgvProductosSeleccionados.KeyDown += DgvProductosSeleccionados_KeyDown;

            // Eventos del formulario
            this.Load += CartelitosPrecios_Load;
            this.KeyDown += CartelitosPrecios_KeyDown;
        }

        private void CartelitosPrecios_Load(object sender, EventArgs e)
        {
            txtCodigoProducto.Focus();
        }

        private void CartelitosPrecios_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }

        private void TxtCodigoProducto_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Permitir solo números
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void TxtCodigoProducto_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                BtnAgregarProducto_Click(sender, e);
            }
        }

        private async void BtnAgregarProducto_Click(object sender, EventArgs e)
        {
            string codigo = txtCodigoProducto.Text.Trim();
            if (string.IsNullOrEmpty(codigo))
            {
                MessageBox.Show("Ingrese un código de producto.", "Información", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCodigoProducto.Focus();
                return;
            }

            // Limpiar ceros a la izquierda
            codigo = codigo.TrimStart('0');
            if (string.IsNullOrEmpty(codigo))
                codigo = "0";

            await AgregarProductoPorCodigo(codigo);
        }

        private async Task AgregarProductoPorCodigo(string codigo)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                btnAgregarProducto.Enabled = false;

                // Verificar si el producto ya está en la lista
                if (productosSeleccionados.Any(p => p.Codigo == codigo))
                {
                    MessageBox.Show($"El producto con código '{codigo}' ya está en la lista.", 
                        "Producto duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCodigoProducto.Focus();
                    txtCodigoProducto.SelectAll();
                    return;
                }

                // Buscar el producto en la base de datos
                var producto = await BuscarProductoAsync(codigo);
                if (producto == null)
                {
                    MessageBox.Show($"No se encontró un producto con el código '{codigo}'.", 
                        "Producto no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCodigoProducto.Focus();
                    txtCodigoProducto.SelectAll();
                    return;
                }

                // Agregar a la lista
                productosSeleccionados.Add(producto);
                ActualizarDataGridView();
                ActualizarContador();

                // Limpiar y enfocar para el siguiente producto
                txtCodigoProducto.Clear();
                txtCodigoProducto.Focus();

                System.Diagnostics.Debug.WriteLine($"✅ Producto agregado: {codigo} - {producto.Descripcion}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar producto: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnAgregarProducto.Enabled = true;
            }
        }

        private async Task<ProductoCartelito> BuscarProductoAsync(string codigo)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"SELECT codigo, descripcion, precio, marca, rubro 
                                  FROM Productos 
                                  WHERE codigo = @codigo";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@codigo", codigo);
                        await connection.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                return new ProductoCartelito
                                {
                                    Codigo = reader["codigo"].ToString(),
                                    Descripcion = reader["descripcion"].ToString(),
                                    Precio = Convert.ToDecimal(reader["precio"]),
                                    Marca = reader["marca"].ToString(),
                                    Rubro = reader["rubro"].ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error buscando producto: {ex.Message}");
            }

            return null;
        }

        private void ActualizarDataGridView()
        {
            dgvProductosSeleccionados.Rows.Clear();

            foreach (var producto in productosSeleccionados)
            {
                dgvProductosSeleccionados.Rows.Add(
                    producto.Codigo,
                    producto.Descripcion,
                    producto.Precio,
                    producto.Marca
                );
            }

            // Actualizar estado de botones
            bool hayProductos = productosSeleccionados.Count > 0;
            btnImprimir.Enabled = hayProductos;
            btnVistaPrevia.Enabled = hayProductos;
            btnLimpiarLista.Enabled = hayProductos;
        }

        private void ActualizarContador()
        {
            lblTotalProductos.Text = $"Productos en lista: {productosSeleccionados.Count}";
            
            bool haySeleccion = dgvProductosSeleccionados.SelectedRows.Count > 0;
            btnEliminarSeleccionado.Enabled = haySeleccion;
        }

        private void DgvProductosSeleccionados_SelectionChanged(object sender, EventArgs e)
        {
            ActualizarContador();
        }

        private void DgvProductosSeleccionados_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                BtnEliminarSeleccionado_Click(sender, e);
            }
        }

        private void BtnEliminarSeleccionado_Click(object sender, EventArgs e)
        {
            if (dgvProductosSeleccionados.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un producto para eliminar.", "Información", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgvProductosSeleccionados.SelectedRows[0];
            string codigo = row.Cells["Codigo"].Value.ToString();
            string descripcion = row.Cells["Descripcion"].Value.ToString();

            var resultado = MessageBox.Show(
                $"¿Está seguro de eliminar el producto:\n{descripcion}?",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (resultado == DialogResult.Yes)
            {
                productosSeleccionados.RemoveAll(p => p.Codigo == codigo);
                ActualizarDataGridView();
                ActualizarContador();
            }
        }

        private void BtnLimpiarLista_Click(object sender, EventArgs e)
        {
            if (productosSeleccionados.Count == 0)
            {
                MessageBox.Show("La lista ya está vacía.", "Información", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var resultado = MessageBox.Show(
                $"¿Está seguro de eliminar todos los productos de la lista?\n\nSe eliminarán {productosSeleccionados.Count} productos.",
                "Confirmar limpieza",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (resultado == DialogResult.Yes)
            {
                productosSeleccionados.Clear();
                ActualizarDataGridView();
                ActualizarContador();
                txtCodigoProducto.Focus();
            }
        }

        private void BtnVistaPrevia_Click(object sender, EventArgs e)
        {
            if (productosSeleccionados.Count == 0)
            {
                MessageBox.Show("No hay productos en la lista para mostrar.", "Información", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var tamañoSeleccionado = ObtenerTamañoSeleccionado();
            MostrarVistaPrevia(tamañoSeleccionado);
        }

        private void BtnImprimir_Click(object sender, EventArgs e)
        {
            if (productosSeleccionados.Count == 0)
            {
                MessageBox.Show("No hay productos en la lista para imprimir.", "Información", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var tamañoSeleccionado = ObtenerTamañoSeleccionado();
            ImprimirCartelitos(tamañoSeleccionado);
        }

        private TamañoCartelito ObtenerTamañoSeleccionado()
        {
            if (rbTamañoPerfumeria.Checked)
                return TamañoCartelito.Perfumeria;
            else if (rbTamañoOferta.Checked)
                return TamañoCartelito.Oferta;
            else
                return TamañoCartelito.Estandar;
        }

        private void MostrarVistaPrevia(TamañoCartelito tamaño)
        {
            try
            {
                var servicioPrint = new ServicioImpresionCartelitos(productosSeleccionados, tamaño);
                servicioPrint.MostrarVistaPrevia();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al mostrar vista previa: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImprimirCartelitos(TamañoCartelito tamaño)
        {
            try
            {
                var resultado = MessageBox.Show(
                    $"¿Está seguro de imprimir {productosSeleccionados.Count} cartelitos en tamaño {tamaño}?",
                    "Confirmar impresión",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resultado == DialogResult.Yes)
                {
                    var servicioPrint = new ServicioImpresionCartelitos(productosSeleccionados, tamaño);
                    servicioPrint.Imprimir();
                    
                    MessageBox.Show("Cartelitos enviados a la impresora correctamente.", "Impresión exitosa", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Clase para representar un producto en el cartelito
    public class ProductoCartelito
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public string Marca { get; set; }
        public string Rubro { get; set; }
    }

    // Enumeración para los tamaños de cartelito
    public enum TamañoCartelito
    {
        Estandar,   // 7x5 cm
        Perfumeria, // 5x3 cm  
        Oferta      // 10x7 cm
    }
}