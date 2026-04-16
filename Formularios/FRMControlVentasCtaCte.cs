using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class FRMControlVentasCtaCte : Form
    {
        private DataGridView dgvVentas;
        private DateTimePicker dtpDesde;
        private DateTimePicker dtpHasta;
        private Label lblDesde;
        private Label lblHasta;
        private Button btnHoy;
        private Button btnAyer;
        private Button btnSemana;
        private Button btnMes;
        private Button btnBuscar;
        private Label lblTotal;
        private Label lblCantidadVentas;
        private Label lblTitulo;
        private Panel panelFiltros;
        private Panel panelResumen;
        private ComboBox cboFiltroCliente;
        private Label lblFiltroCliente;

        private DataTable ventasOriginales;

        public FRMControlVentasCtaCte()
        {
            InitializeComponent();
            ConfigurarFormulario();
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Text = "Control de Ventas Cta. Cte.";
            this.Font = new Font("Segoe UI", 9F);
        }

        private void ConfigurarFormulario()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.WhiteSmoke;

            // 1. Panel de resumen (abajo) - PRIMERO
            CrearPanelResumen();

            // 2. DataGridView (centro) - SEGUNDO
            CrearDataGridView();

            // 3. Panel de filtros (arriba) - TERCERO
            panelFiltros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(10, 5, 10, 5)
            };
            this.Controls.Add(panelFiltros);

            // 4. Título (arriba del todo) - ÚLTIMO
            lblTitulo = new Label
            {
                Text = "Control de Ventas Cta. Cte.",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White,
                Padding = new Padding(0, 5, 0, 5)
            };
            this.Controls.Add(lblTitulo);

            // 5. Controles de filtros
            CrearControlesFiltros();

            // Cargar datos del día al abrir
            this.Load += (s, e) => CargarVentasDelDia();
        }

        private void CrearControlesFiltros()
        {
            int y = 5;
            int x = 10;
            int controlHeight = 25;
            int spacing = 8;

            // --- Primera fila: fechas y botones ---
            lblDesde = new Label
            {
                Text = "Desde:",
                Location = new Point(x, y + 3),
                Size = new Size(55, controlHeight),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            panelFiltros.Controls.Add(lblDesde);

            dtpDesde = new DateTimePicker
            {
                Location = new Point(x + 60, y),
                Size = new Size(115, controlHeight),
                Format = DateTimePickerFormat.Short
            };
            panelFiltros.Controls.Add(dtpDesde);

            lblHasta = new Label
            {
                Text = "Hasta:",
                Location = new Point(x + 185, y + 3),
                Size = new Size(55, controlHeight),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            panelFiltros.Controls.Add(lblHasta);

            dtpHasta = new DateTimePicker
            {
                Location = new Point(x + 245, y),
                Size = new Size(115, controlHeight),
                Format = DateTimePickerFormat.Short
            };
            panelFiltros.Controls.Add(dtpHasta);

            // Botones de acceso rápido
            btnHoy = CrearBotonAtajo("Hoy", new Point(x + 375, y), BtnHoy_Click);
            btnAyer = CrearBotonAtajo("Ayer", new Point(x + 445, y), BtnAyer_Click);
            btnSemana = CrearBotonAtajo("Semana", new Point(x + 515, y), BtnSemana_Click);
            btnMes = CrearBotonAtajo("Mes", new Point(x + 595, y), BtnMes_Click);

            btnBuscar = new Button
            {
                Text = "Buscar",
                Location = new Point(x + 670, y),
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

            // --- Segunda fila: filtro por cliente ---
            lblFiltroCliente = new Label
            {
                Text = "Cliente:",
                Location = new Point(x, y + 3),
                Size = new Size(55, controlHeight),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            panelFiltros.Controls.Add(lblFiltroCliente);

            cboFiltroCliente = new ComboBox
            {
                Location = new Point(x + 60, y),
                Size = new Size(250, controlHeight),
                Font = new Font("Segoe UI", 9F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboFiltroCliente.SelectedIndexChanged += (s, e) => AplicarFiltroCliente();
            panelFiltros.Controls.Add(cboFiltroCliente);
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

            lblCantidadVentas = new Label
            {
                Text = "Ventas Cta. Cte.: 0",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Left,
                Width = 300,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelResumen.Controls.Add(lblCantidadVentas);

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

                string query = @"
                    SELECT
                        f.NumeroRemito        AS 'Remito',
                        f.NroFactura          AS 'N° Factura',
                        ISNULL(f.CtaCteNombre, '') AS 'Cliente',
                        CAST(ISNULL(f.ImporteFinal, 0) AS DECIMAL(18,2)) AS 'Importe Final',
                        ISNULL(f.FormadePago, '') AS 'Forma de Pago',
                        ISNULL(f.TipoFactura, '') AS 'Tipo',
                        ISNULL(f.Cajero, '')  AS 'Cajero',
                        CAST(f.Fecha AS DATE)  AS 'Fecha',
                        CASE
                            WHEN f.Hora IS NULL THEN ''
                            ELSE CONVERT(VARCHAR(5), f.Hora, 108)
                        END AS 'Hora'
                    FROM Facturas f
                    WHERE CAST(f.Fecha AS DATE) BETWEEN @desde AND @hasta
                      AND f.esCtaCte = 1
                    ORDER BY f.NumeroRemito DESC";

                using var adapter = new SqlDataAdapter(query, connection);
                adapter.SelectCommand.Parameters.AddWithValue("@desde", desde.Date);
                adapter.SelectCommand.Parameters.AddWithValue("@hasta", hasta.Date);

                var dt = new DataTable();
                adapter.Fill(dt);

                ventasOriginales = dt.Copy();
                dgvVentas.DataSource = dt;

                CargarComboCliente(dt);
                FormatearColumnas();
                ActualizarResumen(dt);
                ActualizarTitulo(desde, hasta);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ventas Cta. Cte.: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarComboCliente(DataTable dt)
        {
            // Desconectar evento para evitar filtrados mientras se carga
            cboFiltroCliente.SelectedIndexChanged -= (s, e) => AplicarFiltroCliente();

            var clientes = dt.AsEnumerable()
                .Select(r => r["Cliente"].ToString())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            clientes.Insert(0, "-- Todos --");
            cboFiltroCliente.DataSource = clientes;
            cboFiltroCliente.SelectedIndex = 0;

            cboFiltroCliente.SelectedIndexChanged += (s, e) => AplicarFiltroCliente();
        }

        private void AplicarFiltroCliente()
        {
            if (ventasOriginales == null) return;

            if (cboFiltroCliente.SelectedIndex <= 0)
            {
                dgvVentas.DataSource = ventasOriginales.Copy();
                ActualizarResumen(ventasOriginales);
                return;
            }

            string clienteSeleccionado = cboFiltroCliente.SelectedItem.ToString();
            var filtrado = ventasOriginales.AsEnumerable()
                .Where(r => r["Cliente"].ToString().Equals(clienteSeleccionado, StringComparison.OrdinalIgnoreCase))
                .CopyToDataTable();

            dgvVentas.DataSource = filtrado;
            ActualizarResumen(filtrado);
        }

        private void FormatearColumnas()
        {
            if (dgvVentas.Columns.Count == 0) return;

            dgvVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            SetColumn("Remito", width: 80, align: DataGridViewContentAlignment.MiddleCenter, bold: true);
            SetColumn("N° Factura", width: 110, align: DataGridViewContentAlignment.MiddleCenter);
            SetColumn("Cliente", fillWeight: 160, align: DataGridViewContentAlignment.MiddleLeft);
            SetColumn("Importe Final", width: 130, align: DataGridViewContentAlignment.MiddleRight,
                format: "C2", foreColor: Color.FromArgb(40, 167, 69), bold: true, headerText: "Total Final");
            SetColumn("Forma de Pago", width: 120, align: DataGridViewContentAlignment.MiddleCenter, headerText: "Forma Pago");
            SetColumn("Tipo", width: 90, align: DataGridViewContentAlignment.MiddleCenter);
            SetColumn("Cajero", width: 65, align: DataGridViewContentAlignment.MiddleCenter);
            SetColumn("Fecha", width: 90, align: DataGridViewContentAlignment.MiddleCenter, format: "dd/MM/yyyy");
            SetColumn("Hora", width: 65, align: DataGridViewContentAlignment.MiddleCenter);
        }

        private void SetColumn(string name, int width = 0, int fillWeight = 0,
            DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleLeft,
            string format = null, Color? foreColor = null, bool bold = false, string headerText = null)
        {
            var col = dgvVentas.Columns[name];
            if (col == null) return;

            if (fillWeight > 0)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.FillWeight = fillWeight;
            }
            else if (width > 0)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                col.Width = width;
            }

            col.DefaultCellStyle.Alignment = align;
            col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

            if (!string.IsNullOrEmpty(format))
                col.DefaultCellStyle.Format = format;

            if (foreColor.HasValue)
                col.DefaultCellStyle.ForeColor = foreColor.Value;

            if (bold)
                col.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            if (!string.IsNullOrEmpty(headerText))
                col.HeaderText = headerText;
        }

        private void ActualizarResumen(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                lblCantidadVentas.Text = "Ventas Cta. Cte.: 0";
                lblTotal.Text = "Total: $0.00";
                return;
            }

            int cantidadVentas = dt.Rows.Count;
            decimal totalVentas = dt.AsEnumerable()
                .Sum(r => r["Importe Final"] != DBNull.Value ? Convert.ToDecimal(r["Importe Final"]) : 0m);

            lblCantidadVentas.Text = $"Ventas Cta. Cte.: {cantidadVentas}";
            lblTotal.Text = $"Total: {totalVentas:C2}";
        }

        private void ActualizarTitulo(DateTime desde, DateTime hasta)
        {
            string titulo;
            if (desde == DateTime.Today && hasta == DateTime.Today)
                titulo = "Control de Ventas Cta. Cte. - Hoy";
            else if (desde == hasta)
                titulo = $"Control de Ventas Cta. Cte. - {desde:dd/MM/yyyy}";
            else
                titulo = $"Control de Ventas Cta. Cte. - {desde:dd/MM/yyyy}  a  {hasta:dd/MM/yyyy}";

            lblTitulo.Text = titulo;
            lblTitulo.ForeColor = Color.FromArgb(0, 120, 215);
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
