using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class FRMControlVentasProductos : Form
    {
        private DataGridView dgvVentas;
        private DateTimePicker dtpDesde;
        private DateTimePicker dtpHasta;
        private Label lblDesde;
        private Label lblHasta;
        private Button btnSemana;
        private Button btnMes;
        private Button btnBuscar;
        private Button btnHoy;
        private Button btnAyer;
        private Label lblTotal;
        private Label lblCantidadProductos;
        private Label lblTitulo;
        private Panel panelFiltros;
        private Panel panelResumen;
        private TextBox txtFiltroDescripcion;
        private ComboBox cboFiltroMarca; // ✅ CAMBIADO de TextBox a ComboBox
        private ComboBox cboFiltroRubro; // ✅ CAMBIADO de TextBox a ComboBox
        private ComboBox cboFiltroProveedor; // ✅ CAMBIADO de TextBox a ComboBox
        private Label lblFiltroDescripcion;
        private Label lblFiltroMarca;
        private Label lblFiltroRubro;
        private Label lblFiltroProveedor;

        private DataTable ventasOriginales;

        public FRMControlVentasProductos()
        {
            InitializeComponent();
            ConfigurarFormulario();
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Text = "Control de Ventas por Productos";
            this.Font = new Font("Segoe UI", 9F);
        }

        private void ConfigurarFormulario()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.WhiteSmoke;

            // ✅ ORDEN CORRECTO: Crear de abajo hacia arriba

            // 1. Panel de resumen (abajo) - PRIMERO
            CrearPanelResumen();

            // 2. DataGridView (centro) - SEGUNDO
            CrearDataGridView();

            // 3. Panel de filtros (arriba) - TERCERO
            panelFiltros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.White,
                Padding = new Padding(10, 5, 10, 5)
            };
            this.Controls.Add(panelFiltros);

            // 4. Panel de título (arriba del todo) - ÚLTIMO
            lblTitulo = new Label
            {
                Text = "📦 Control de Ventas por Productos",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White,
                Padding = new Padding(0, 5, 0, 5)
            };
            this.Controls.Add(lblTitulo);

            // 5. Configurar controles de filtros
            CrearControlesFiltros();

            // Cargar datos del día actual al abrir
            this.Load += (s, e) => CargarVentasDelDia();
        }

        private void CrearControlesFiltros()
        {
            int y = 5;
            int x = 10;
            int labelWidth = 100;
            int controlHeight = 25;
            int spacing = 8;

            // Primera fila: Fechas y botones de atajo
            lblDesde = new Label
            {
                Text = "Desde:",
                Location = new Point(x, y + 3),
                Size = new Size(60, controlHeight),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            panelFiltros.Controls.Add(lblDesde);

            dtpDesde = new DateTimePicker
            {
                Location = new Point(x + 65, y),
                Size = new Size(115, controlHeight),
                Format = DateTimePickerFormat.Short
            };
            panelFiltros.Controls.Add(dtpDesde);

            lblHasta = new Label
            {
                Text = "Hasta:",
                Location = new Point(x + 190, y + 3),
                Size = new Size(60, controlHeight),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            panelFiltros.Controls.Add(lblHasta);

            dtpHasta = new DateTimePicker
            {
                Location = new Point(x + 250, y),
                Size = new Size(115, controlHeight),
                Format = DateTimePickerFormat.Short
            };
            panelFiltros.Controls.Add(dtpHasta);

            // Botones de atajo
            btnHoy = CrearBotonAtajo("Hoy", new Point(x + 380, y), BtnHoy_Click);
            btnAyer = CrearBotonAtajo("Ayer", new Point(x + 450, y), BtnAyer_Click);
            btnSemana = CrearBotonAtajo("Semana", new Point(x + 520, y), BtnSemana_Click);
            btnMes = CrearBotonAtajo("Mes", new Point(x + 600, y), BtnMes_Click);

            btnBuscar = new Button
            {
                Text = "🔍 Buscar",
                Location = new Point(x + 680, y),
                Size = new Size(95, controlHeight),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += BtnBuscar_Click;
            panelFiltros.Controls.Add(btnBuscar);

            y += controlHeight + spacing;

            // Segunda fila: Filtros de texto y combos
            // Descripción (TextBox)
            lblFiltroDescripcion = new Label
            {
                Text = "Descripción:",
                Location = new Point(x, y + 3),
                Size = new Size(labelWidth, controlHeight),
                Font = new Font("Segoe UI", 9F)
            };
            panelFiltros.Controls.Add(lblFiltroDescripcion);

            txtFiltroDescripcion = new TextBox
            {
                Location = new Point(x + labelWidth, y),
                Size = new Size(170, controlHeight),
                Font = new Font("Segoe UI", 9F)
            };
            txtFiltroDescripcion.TextChanged += (s, e) => AplicarFiltros();
            panelFiltros.Controls.Add(txtFiltroDescripcion);

            // ✅ Marca (ComboBox)
            lblFiltroMarca = new Label
            {
                Text = "Marca:",
                Location = new Point(x + labelWidth + 180, y + 3),
                Size = new Size(55, controlHeight),
                Font = new Font("Segoe UI", 9F)
            };
            panelFiltros.Controls.Add(lblFiltroMarca);

            cboFiltroMarca = new ComboBox
            {
                Location = new Point(x + labelWidth + 235, y),
                Size = new Size(125, controlHeight),
                Font = new Font("Segoe UI", 9F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboFiltroMarca.SelectedIndexChanged += (s, e) => AplicarFiltros();
            panelFiltros.Controls.Add(cboFiltroMarca);

            // ✅ Rubro (ComboBox)
            lblFiltroRubro = new Label
            {
                Text = "Rubro:",
                Location = new Point(x + labelWidth + 370, y + 3),
                Size = new Size(55, controlHeight),
                Font = new Font("Segoe UI", 9F)
            };
            panelFiltros.Controls.Add(lblFiltroRubro);

            cboFiltroRubro = new ComboBox
            {
                Location = new Point(x + labelWidth + 425, y),
                Size = new Size(125, controlHeight),
                Font = new Font("Segoe UI", 9F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboFiltroRubro.SelectedIndexChanged += (s, e) => AplicarFiltros();
            panelFiltros.Controls.Add(cboFiltroRubro);

            // ✅ Proveedor (ComboBox)
            lblFiltroProveedor = new Label
            {
                Text = "Proveedor:",
                Location = new Point(x + labelWidth + 560, y + 3),
                Size = new Size(75, controlHeight),
                Font = new Font("Segoe UI", 9F)
            };
            panelFiltros.Controls.Add(lblFiltroProveedor);

            cboFiltroProveedor = new ComboBox
            {
                Location = new Point(x + labelWidth + 635, y),
                Size = new Size(140, controlHeight),
                Font = new Font("Segoe UI", 9F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboFiltroProveedor.SelectedIndexChanged += (s, e) => AplicarFiltros();
            panelFiltros.Controls.Add(cboFiltroProveedor);
        }

        private Button CrearBotonAtajo(string texto, Point ubicacion, EventHandler clickHandler)
        {
            var btn = new Button
            {
                Text = texto,
                Location = ubicacion,
                Size = new Size(65, 25),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += clickHandler;
            panelFiltros.Controls.Add(btn);
            return btn;
        }

        private void CrearDataGridView()
        {
            dgvVentas = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = Color.FromArgb(230, 240, 250),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 120, 215),
                    SelectionBackColor = Color.FromArgb(230, 240, 250),
                    WrapMode = DataGridViewTriState.True
                },
                ColumnHeadersHeight = 40,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 9F),
                    SelectionBackColor = Color.FromArgb(220, 235, 252),
                    SelectionForeColor = Color.Black
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(248, 249, 250)
                }
            };

            this.Controls.Add(dgvVentas);
            dgvVentas.DataBindingComplete += DgvVentas_DataBindingComplete;
        }

        private void CrearPanelResumen()
        {
            panelResumen = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(0, 120, 215),
                Padding = new Padding(20, 8, 20, 8)
            };
            this.Controls.Add(panelResumen);

            lblCantidadProductos = new Label
            {
                Text = "Productos vendidos: 0",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Left,
                Width = 300,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelResumen.Controls.Add(lblCantidadProductos);

            lblTotal = new Label
            {
                Text = "Total: $0.00",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Right,
                Width = 300,
                TextAlign = ContentAlignment.MiddleRight
            };
            panelResumen.Controls.Add(lblTotal);
        }

        private void CargarVentasDelDia()
        {
            dtpDesde.Value = DateTime.Today;
            dtpHasta.Value = DateTime.Today;
            CargarVentasPorFecha(DateTime.Today, DateTime.Today);
        }

        private void CargarVentasPorFecha(DateTime desde, DateTime hasta)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // ✅ MODIFICADO: Ordenar por NroFactura y hora descendente
                string query = @"
                    SELECT 
                        v.NroFactura AS 'Nro. Remito',
                        v.codigo AS Código,
                        p.descripcion AS Descripción,
                        p.marca AS Marca,
                        p.rubro AS Rubro,
                        p.proveedor AS Proveedor,
                        v.cantidad AS Cantidad,
                        v.precio AS 'Precio Unitario',
                        v.total AS Total,
                        CONVERT(VARCHAR(10), v.fecha, 103) AS Fecha,
                        CASE 
                            WHEN v.hora IS NULL THEN '00:00'
                            ELSE CONVERT(VARCHAR(5), v.hora, 108)
                        END AS Hora
                    FROM Ventas v
                    INNER JOIN Productos p ON v.codigo = p.codigo
                    WHERE v.fecha >= @desde 
                      AND v.fecha < @hastaExclusivo
                    ORDER BY v.NroFactura DESC, 
                             CASE 
                                 WHEN v.hora IS NULL THEN '00:00'
                                 ELSE CONVERT(VARCHAR(5), v.hora, 108)
                             END DESC";

                using var adapter = new SqlDataAdapter(query, connection);
                adapter.SelectCommand.Parameters.AddWithValue("@desde", desde.Date);
                adapter.SelectCommand.Parameters.AddWithValue("@hastaExclusivo", hasta.Date.AddDays(1));

                var dt = new DataTable();
                adapter.Fill(dt);

                ventasOriginales = dt.Copy();
                dgvVentas.DataSource = dt;

                // Cargar combos con valores únicos
                CargarCombosFiltros(dt);

                FormatearColumnas();
                ActualizarResumen(dt);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ventas: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ✅ NUEVO: Método para cargar los ComboBox con valores únicos
        private void CargarCombosFiltros(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;

            // Desactivar eventos temporalmente para evitar filtrado múltiple
            cboFiltroMarca.SelectedIndexChanged -= (s, e) => AplicarFiltros();
            cboFiltroRubro.SelectedIndexChanged -= (s, e) => AplicarFiltros();
            cboFiltroProveedor.SelectedIndexChanged -= (s, e) => AplicarFiltros();

            // Cargar Marcas únicas
            var marcas = dt.AsEnumerable()
                .Select(row => row["Marca"].ToString())
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct()
                .OrderBy(m => m)
                .ToList();
            marcas.Insert(0, "-- Todas --");
            cboFiltroMarca.DataSource = marcas;
            cboFiltroMarca.SelectedIndex = 0;

            // Cargar Rubros únicos
            var rubros = dt.AsEnumerable()
                .Select(row => row["Rubro"].ToString())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct()
                .OrderBy(r => r)
                .ToList();
            rubros.Insert(0, "-- Todos --");
            cboFiltroRubro.DataSource = rubros;
            cboFiltroRubro.SelectedIndex = 0;

            // Cargar Proveedores únicos
            var proveedores = dt.AsEnumerable()
                .Select(row => row["Proveedor"].ToString())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .OrderBy(p => p)
                .ToList();
            proveedores.Insert(0, "-- Todos --");
            cboFiltroProveedor.DataSource = proveedores;
            cboFiltroProveedor.SelectedIndex = 0;

            // Reactivar eventos
            cboFiltroMarca.SelectedIndexChanged += (s, e) => AplicarFiltros();
            cboFiltroRubro.SelectedIndexChanged += (s, e) => AplicarFiltros();
            cboFiltroProveedor.SelectedIndexChanged += (s, e) => AplicarFiltros();
        }

        private void FormatearColumnas()
        {
            if (dgvVentas.Columns.Count == 0) return;

            // Formatear columna Nro. Remito
            if (dgvVentas.Columns["Nro. Remito"] != null)
            {
                dgvVentas.Columns["Nro. Remito"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvVentas.Columns["Nro. Remito"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }

            // Formatear columnas de moneda
            if (dgvVentas.Columns["Precio Unitario"] != null)
            {
                dgvVentas.Columns["Precio Unitario"].DefaultCellStyle.Format = "C2";
                dgvVentas.Columns["Precio Unitario"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            if (dgvVentas.Columns["Total"] != null)
            {
                dgvVentas.Columns["Total"].DefaultCellStyle.Format = "C2";
                dgvVentas.Columns["Total"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvVentas.Columns["Total"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }

            // Formatear columna de cantidad
            if (dgvVentas.Columns["Cantidad"] != null)
            {
                dgvVentas.Columns["Cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // ✅ NUEVO: Formatear columna "Fecha" (separada)
            if (dgvVentas.Columns["Fecha"] != null)
            {
                dgvVentas.Columns["Fecha"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvVentas.Columns["Fecha"].Width = 100;
            }

            // ✅ NUEVO: Formatear columna "Hora" (separada)
            if (dgvVentas.Columns["Hora"] != null)
            {
                dgvVentas.Columns["Hora"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvVentas.Columns["Hora"].Width = 70;
            }

            // Ajustar anchos
            dgvVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Asignar pesos específicos para mejor distribución
            if (dgvVentas.Columns["Nro. Remito"] != null)
                dgvVentas.Columns["Nro. Remito"].FillWeight = 70;

            if (dgvVentas.Columns["Código"] != null)
                dgvVentas.Columns["Código"].FillWeight = 60;

            if (dgvVentas.Columns["Descripción"] != null)
                dgvVentas.Columns["Descripción"].FillWeight = 180;

            if (dgvVentas.Columns["Marca"] != null)
                dgvVentas.Columns["Marca"].FillWeight = 70;

            if (dgvVentas.Columns["Rubro"] != null)
                dgvVentas.Columns["Rubro"].FillWeight = 70;

            if (dgvVentas.Columns["Proveedor"] != null)
                dgvVentas.Columns["Proveedor"].FillWeight = 70;

            if (dgvVentas.Columns["Cantidad"] != null)
                dgvVentas.Columns["Cantidad"].FillWeight = 50;

            if (dgvVentas.Columns["Precio Unitario"] != null)
                dgvVentas.Columns["Precio Unitario"].FillWeight = 70;

            if (dgvVentas.Columns["Total"] != null)
                dgvVentas.Columns["Total"].FillWeight = 70;

            // ✅ NUEVO: Pesos para Fecha y Hora separadas
            if (dgvVentas.Columns["Fecha"] != null)
                dgvVentas.Columns["Fecha"].FillWeight = 80;

            if (dgvVentas.Columns["Hora"] != null)
                dgvVentas.Columns["Hora"].FillWeight = 50;
        }

        private void ActualizarResumen(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                lblCantidadProductos.Text = "Productos vendidos: 0";
                lblTotal.Text = "Total: $0.00";
                return;
            }

            int totalProductos = dt.AsEnumerable().Sum(row => Convert.ToInt32(row["Cantidad"]));
            decimal totalVentas = dt.AsEnumerable().Sum(row => Convert.ToDecimal(row["Total"]));

            lblCantidadProductos.Text = $"Productos vendidos: {totalProductos}";
            lblTotal.Text = $"Total: {totalVentas:C2}";
        }

        // ✅ MODIFICADO: Filtros con ComboBox
        private void AplicarFiltros()
        {
            if (ventasOriginales == null || ventasOriginales.Rows.Count == 0)
                return;

            var filtrado = ventasOriginales.Clone();

            foreach (DataRow row in ventasOriginales.Rows)
            {
                bool cumpleFiltros = true;

                // Filtro por descripción (TextBox)
                if (!string.IsNullOrWhiteSpace(txtFiltroDescripcion.Text))
                {
                    string desc = row["Descripción"].ToString().ToLower();
                    if (!desc.Contains(txtFiltroDescripcion.Text.ToLower()))
                        cumpleFiltros = false;
                }

                // Filtro por marca (ComboBox)
                if (cboFiltroMarca.SelectedIndex > 0)
                {
                    string marcaSeleccionada = cboFiltroMarca.SelectedItem.ToString();
                    string marcaProducto = row["Marca"].ToString();
                    if (!marcaProducto.Equals(marcaSeleccionada, StringComparison.OrdinalIgnoreCase))
                        cumpleFiltros = false;
                }

                // Filtro por rubro (ComboBox)
                if (cboFiltroRubro.SelectedIndex > 0)
                {
                    string rubroSeleccionado = cboFiltroRubro.SelectedItem.ToString();
                    string rubroProducto = row["Rubro"].ToString();
                    if (!rubroProducto.Equals(rubroSeleccionado, StringComparison.OrdinalIgnoreCase))
                        cumpleFiltros = false;
                }

                // Filtro por proveedor (ComboBox)
                if (cboFiltroProveedor.SelectedIndex > 0)
                {
                    string proveedorSeleccionado = cboFiltroProveedor.SelectedItem.ToString();
                    string proveedorProducto = row["Proveedor"].ToString();
                    if (!proveedorProducto.Equals(proveedorSeleccionado, StringComparison.OrdinalIgnoreCase))
                        cumpleFiltros = false;
                }

                if (cumpleFiltros)
                    filtrado.ImportRow(row);
            }

            // ✅ NUEVO: Ordenar los datos filtrados por Nro. Remito y Hora descendente
            var datosOrdenados = filtrado.AsEnumerable()
                .OrderByDescending(row => Convert.ToInt32(row["Nro. Remito"]))
                .ThenByDescending(row => row["Hora"].ToString())
                .CopyToDataTable();

            dgvVentas.DataSource = datosOrdenados;
            ActualizarResumen(datosOrdenados);
        }

        private void DgvVentas_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            FormatearColumnas();
        }

        private void BtnBuscar_Click(object sender, EventArgs e)
        {
            CargarVentasPorFecha(dtpDesde.Value.Date, dtpHasta.Value.Date);
        }

        private void BtnHoy_Click(object sender, EventArgs e)
        {
            dtpDesde.Value = DateTime.Today;
            dtpHasta.Value = DateTime.Today;
            CargarVentasDelDia();
        }

        private void BtnAyer_Click(object sender, EventArgs e)
        {
            dtpDesde.Value = DateTime.Today.AddDays(-1);
            dtpHasta.Value = DateTime.Today.AddDays(-1);
            CargarVentasPorFecha(dtpDesde.Value, dtpHasta.Value);
        }

        private void BtnSemana_Click(object sender, EventArgs e)
        {
            dtpDesde.Value = DateTime.Today.AddDays(-7);
            dtpHasta.Value = DateTime.Today;
            CargarVentasPorFecha(dtpDesde.Value, dtpHasta.Value);
        }

        private void BtnMes_Click(object sender, EventArgs e)
        {
            dtpDesde.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            dtpHasta.Value = DateTime.Today;
            CargarVentasPorFecha(dtpDesde.Value, dtpHasta.Value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private System.ComponentModel.IContainer components = null;
    }
}