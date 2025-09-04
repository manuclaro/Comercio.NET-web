using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public class frmControlFacturas : Form
    {
        private DataGridView dgvVentas;
        private DateTimePicker dtpFecha;
        private Button btnBuscar;
        private Button btnHoy;
        private Label lblTotal;
        private Label lblCantidadVentas;
        private Label lblTitulo;
        private Panel panelFiltros;
        private Panel panelResumen;
        private Form frmDetalle; // Ventana flotante para el detalle

        public frmControlFacturas()
        {
            InitializeComponent();
            ConfigurarFormulario();
            CrearVentanaDetalle();
            CargarVentasDelDia();
        }

        private void InitializeComponent()
        {
            this.dgvVentas = new DataGridView();
            this.dtpFecha = new DateTimePicker();
            this.btnBuscar = new Button();
            this.btnHoy = new Button();
            this.lblTotal = new Label();
            this.lblCantidadVentas = new Label();
            this.lblTitulo = new Label();
            this.panelFiltros = new Panel();
            this.panelResumen = new Panel();
            
            ((System.ComponentModel.ISupportInitialize)(this.dgvVentas)).BeginInit();
            this.panelFiltros.SuspendLayout();
            this.panelResumen.SuspendLayout();
            this.SuspendLayout();

            // lblTitulo
            this.lblTitulo.Dock = DockStyle.Top;
            this.lblTitulo.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this.lblTitulo.ForeColor = Color.FromArgb(0, 120, 215);
            this.lblTitulo.Height = 50;
            this.lblTitulo.Text = "Control de Facturas - Ventas del Día";
            this.lblTitulo.TextAlign = ContentAlignment.MiddleCenter;

            // panelFiltros
            this.panelFiltros.BackColor = Color.FromArgb(248, 249, 250);
            this.panelFiltros.Dock = DockStyle.Top;
            this.panelFiltros.Height = 60;
            this.panelFiltros.Padding = new Padding(10);

            // dtpFecha
            this.dtpFecha.Font = new Font("Segoe UI", 10F);
            this.dtpFecha.Format = DateTimePickerFormat.Short;
            this.dtpFecha.Location = new Point(20, 15);
            this.dtpFecha.Size = new Size(120, 25);
            this.dtpFecha.Value = DateTime.Today;

            // btnBuscar
            this.btnBuscar.BackColor = Color.FromArgb(0, 120, 215);
            this.btnBuscar.FlatStyle = FlatStyle.Flat;
            this.btnBuscar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnBuscar.ForeColor = Color.White;
            this.btnBuscar.Location = new Point(150, 15);
            this.btnBuscar.Size = new Size(80, 30);
            this.btnBuscar.Text = "Buscar";
            this.btnBuscar.Click += BtnBuscar_Click;

            // btnHoy
            this.btnHoy.BackColor = Color.FromArgb(0, 150, 136);
            this.btnHoy.FlatStyle = FlatStyle.Flat;
            this.btnHoy.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnHoy.ForeColor = Color.White;
            this.btnHoy.Location = new Point(240, 15);
            this.btnHoy.Size = new Size(80, 30);
            this.btnHoy.Text = "Hoy";
            this.btnHoy.Click += BtnHoy_Click;

            // dgvVentas
            this.dgvVentas.AllowUserToAddRows = false;
            this.dgvVentas.AllowUserToDeleteRows = false;
            this.dgvVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvVentas.BackgroundColor = Color.White;
            this.dgvVentas.BorderStyle = BorderStyle.None;
            this.dgvVentas.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvVentas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            this.dgvVentas.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.dgvVentas.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            this.dgvVentas.ColumnHeadersHeight = 35;
            this.dgvVentas.Dock = DockStyle.Fill;
            this.dgvVentas.EnableHeadersVisualStyles = false;
            this.dgvVentas.ReadOnly = true;
            this.dgvVentas.RowHeadersVisible = false;
            this.dgvVentas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvVentas.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 252);
            this.dgvVentas.CellClick += DgvVentas_CellClick;
            this.dgvVentas.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 249, 250); // Sin resaltado

            // panelResumen
            this.panelResumen.BackColor = Color.FromArgb(0, 120, 215);
            this.panelResumen.Dock = DockStyle.Bottom;
            this.panelResumen.Height = 60;

            // lblCantidadVentas
            this.lblCantidadVentas.Dock = DockStyle.Left;
            this.lblCantidadVentas.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            this.lblCantidadVentas.ForeColor = Color.White;
            this.lblCantidadVentas.Text = "Ventas: 0";
            this.lblCantidadVentas.TextAlign = ContentAlignment.MiddleLeft;
            this.lblCantidadVentas.Width = 200;
            this.lblCantidadVentas.Padding = new Padding(20, 0, 0, 0);

            // lblTotal
            this.lblTotal.Dock = DockStyle.Right;
            this.lblTotal.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.lblTotal.ForeColor = Color.White;
            this.lblTotal.Text = "Total: $0,00";
            this.lblTotal.TextAlign = ContentAlignment.MiddleRight;
            this.lblTotal.Width = 300;
            this.lblTotal.Padding = new Padding(0, 0, 20, 0);

            // Agregar controles a paneles
            this.panelFiltros.Controls.Add(this.btnHoy);
            this.panelFiltros.Controls.Add(this.btnBuscar);
            this.panelFiltros.Controls.Add(this.dtpFecha);

            this.panelResumen.Controls.Add(this.lblCantidadVentas);
            this.panelResumen.Controls.Add(this.lblTotal);

            // frmControlFacturas
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1000, 600);
            this.Controls.Add(this.dgvVentas);
            this.Controls.Add(this.panelResumen);
            this.Controls.Add(this.panelFiltros);
            this.Controls.Add(this.lblTitulo);
            this.Font = new Font("Segoe UI", 9F);
            this.Text = "Control de Facturas";
            this.WindowState = FormWindowState.Maximized;
            
            // Eventos para ocultar ventana detalle al hacer clic
            this.Click += FrmControlFacturas_Click;
            this.dgvVentas.Click += FrmControlFacturas_Click;
            this.panelFiltros.Click += FrmControlFacturas_Click;
            this.panelResumen.Click += FrmControlFacturas_Click;

            ((System.ComponentModel.ISupportInitialize)(this.dgvVentas)).EndInit();
            this.panelFiltros.ResumeLayout(false);
            this.panelResumen.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private void CrearVentanaDetalle()
        {
            frmDetalle = new Form();
            frmDetalle.Text = "Detalle de Factura";
            frmDetalle.Size = new Size(650, 450);
            frmDetalle.StartPosition = FormStartPosition.CenterScreen;
            frmDetalle.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            frmDetalle.ShowInTaskbar = false;
            frmDetalle.TopMost = true;
            frmDetalle.Visible = false;
            frmDetalle.MinimumSize = new Size(600, 400);
            
            // CAMBIO: Quitar la cruz de cerrar
            frmDetalle.ControlBox = false;
            frmDetalle.MaximizeBox = false;
            frmDetalle.MinimizeBox = false;

            // Panel de totales en la parte inferior - Color diferente
            var panelTotales = new Panel();
            panelTotales.BackColor = Color.FromArgb(76, 175, 80); // Verde Material Design
            panelTotales.Dock = DockStyle.Bottom;
            panelTotales.Height = 50;

            // Label para cantidad de productos
            var lblCantidadProductos = new Label();
            lblCantidadProductos.Name = "lblCantidadProductos";
            lblCantidadProductos.Dock = DockStyle.Left;
            lblCantidadProductos.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblCantidadProductos.ForeColor = Color.White;
            lblCantidadProductos.Text = "Productos: 0";
            lblCantidadProductos.TextAlign = ContentAlignment.MiddleLeft;
            lblCantidadProductos.Width = 150;
            lblCantidadProductos.Padding = new Padding(15, 0, 0, 0);

            // Label para cantidad total
            var lblCantidadTotalDetalle = new Label();
            lblCantidadTotalDetalle.Name = "lblCantidadTotalDetalle";
            lblCantidadTotalDetalle.Dock = DockStyle.Left;
            lblCantidadTotalDetalle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblCantidadTotalDetalle.ForeColor = Color.White;
            lblCantidadTotalDetalle.Text = "Cantidad: 0";
            lblCantidadTotalDetalle.TextAlign = ContentAlignment.MiddleLeft;
            lblCantidadTotalDetalle.Width = 150;
            lblCantidadTotalDetalle.Padding = new Padding(15, 0, 0, 0);

            // Label para total de la factura
            var lblTotalFactura = new Label();
            lblTotalFactura.Name = "lblTotalFactura";
            lblTotalFactura.Dock = DockStyle.Right;
            lblTotalFactura.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTotalFactura.ForeColor = Color.White;
            lblTotalFactura.Text = "Total: $0,00";
            lblTotalFactura.TextAlign = ContentAlignment.MiddleRight;
            lblTotalFactura.Width = 200;
            lblTotalFactura.Padding = new Padding(0, 0, 15, 0);

            // Agregar labels al panel de totales
            panelTotales.Controls.Add(lblCantidadProductos);
            panelTotales.Controls.Add(lblCantidadTotalDetalle);
            panelTotales.Controls.Add(lblTotalFactura);

            // Crear DataGridView para el detalle
            var dgvDetalle = new DataGridView();
            dgvDetalle.AllowUserToAddRows = false;
            dgvDetalle.AllowUserToDeleteRows = false;
            dgvDetalle.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDetalle.BackgroundColor = Color.White;
            dgvDetalle.BorderStyle = BorderStyle.None;
            dgvDetalle.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvDetalle.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 244, 248);
            dgvDetalle.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvDetalle.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dgvDetalle.ColumnHeadersHeight = 30;
            dgvDetalle.Dock = DockStyle.Fill;
            dgvDetalle.EnableHeadersVisualStyles = false;
            dgvDetalle.ReadOnly = true;
            dgvDetalle.RowHeadersVisible = false;
            dgvDetalle.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDetalle.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvDetalle.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dgvDetalle.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 254);
            dgvDetalle.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvDetalle.Name = "dgvDetalle";
            dgvDetalle.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 244, 248); // Sin resaltado

            // Agregar controles al formulario
            frmDetalle.Controls.Add(dgvDetalle);
            frmDetalle.Controls.Add(panelTotales);

            // Evento para ocultar al perder el foco
            frmDetalle.Deactivate += (s, e) => frmDetalle.Hide();
            
            // OPCIONAL: Agregar evento de doble clic en la barra de título para cerrar
            frmDetalle.MouseDoubleClick += (s, e) => 
            {
                if (e.Y <= 30) // Solo si hace doble clic en la barra de título
                    frmDetalle.Hide();
            };
        }

        private void FrmControlFacturas_Click(object sender, EventArgs e)
        {
            if (frmDetalle != null && frmDetalle.Visible)
            {
                frmDetalle.Hide();
            }
        }

        private void ConfigurarFormulario()
        {
            this.StartPosition = FormStartPosition.CenterParent;
            ConfigurarDataGridView();
        }

        private void ConfigurarDataGridView()
        {
            // Configuración adicional del DataGridView principal
            dgvVentas.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dgvVentas.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 254);
            dgvVentas.DefaultCellStyle.SelectionForeColor = Color.Black;
        }

        private void DgvVentas_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var nroFactura = dgvVentas.Rows[e.RowIndex].Cells["N° Factura"].Value?.ToString();
                if (!string.IsNullOrEmpty(nroFactura))
                {
                    CargarDetalleFactura(nroFactura);
                    MostrarVentanaDetalle();
                }
            }
        }

        private void MostrarVentanaDetalle()
        {
            if (frmDetalle != null)
            {
                // Centrar en la pantalla
                frmDetalle.StartPosition = FormStartPosition.CenterScreen;
                frmDetalle.Show();
                frmDetalle.BringToFront();
            }
        }

        private void CargarVentasPorFecha(DateTime fecha)
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
                    var query = @"
                        SELECT 
                            NumeroFactura as 'N° Factura',
                            Fecha as 'Fecha',
                            Hora as 'Hora',
                            ImporteTotal as 'Total Venta',
                            FormadePago as 'Forma de Pago',
                            TipoFactura as 'Tipo Factura',
                            CAENumero as 'CAE',
                            CUITCliente as 'CUIT Cliente'
                        FROM Facturas 
                        WHERE CAST(Fecha AS DATE) = @fecha
                        ORDER BY NumeroFactura DESC";

                    using (var adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@fecha", fecha.Date);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        
                        dgvVentas.DataSource = dt;
                        FormatearColumnas();
                        ActualizarResumen(dt);
                    }
                }

                lblTitulo.Text = fecha.Date == DateTime.Today 
                    ? "Control de Facturas - Ventas del Día" 
                    : $"Control de Facturas - Ventas del {fecha:dd/MM/yyyy}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las ventas: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatearColumnas()
        {
            if (dgvVentas.Columns.Count == 0) return;

            // Temporalmente deshabilitar AutoSizeColumnsMode para permitir configuración manual
            var originalAutoSizeMode = dgvVentas.AutoSizeColumnsMode;
            dgvVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            try
            {
                // Formatear columnas principales
                var totalVentaCol = dgvVentas.Columns["Total Venta"];
                if (totalVentaCol != null)
                {
                    totalVentaCol.DefaultCellStyle.Format = "C2";
                    totalVentaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    totalVentaCol.Width = 120;
                }

                var fechaCol = dgvVentas.Columns["Fecha"];
                if (fechaCol != null)
                {
                    fechaCol.DefaultCellStyle.Format = "dd/MM/yyyy";
                    fechaCol.Width = 100;
                }

                var horaCol = dgvVentas.Columns["Hora"];
                if (horaCol != null)
                {
                    horaCol.Width = 80;
                    horaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    horaCol.DefaultCellStyle.Format = "HH:mm:ss";
                }

                var facturaCol = dgvVentas.Columns["N° Factura"];
                if (facturaCol != null)
                {
                    facturaCol.Width = 100;
                    facturaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var formaPagoCol = dgvVentas.Columns["Forma de Pago"];
                if (formaPagoCol != null)
                {
                    formaPagoCol.Width = 120;
                    formaPagoCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // REMOVIDO: La configuración de la columna "Tipo"

                var tipoFacturaCol = dgvVentas.Columns["Tipo Factura"];
                if (tipoFacturaCol != null)
                {
                    tipoFacturaCol.Width = 100;
                    tipoFacturaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var caeCol = dgvVentas.Columns["CAE"];
                if (caeCol != null)
                {
                    caeCol.Width = 120;
                    caeCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var cuitCol = dgvVentas.Columns["CUIT Cliente"];
                if (cuitCol != null)
                {
                    cuitCol.Width = 120;
                    cuitCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }
            finally
            {
                // Restaurar el modo original
                dgvVentas.AutoSizeColumnsMode = originalAutoSizeMode;
            }
        }

        private void CargarDetalleFactura(string nroFactura)
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
                    // CAMBIO: Consultar desde la tabla Ventas usando nrofactura
                    var query = @"
                        SELECT 
                            codigo as 'Código',
                            descripcion as 'Producto',
                            cantidad as 'Cantidad',
                            precio as 'Precio Unit.',
                            total as 'Total'
                        FROM Ventas 
                        WHERE nrofactura = @nroFactura
                        ORDER BY descripcion";

                    using (var adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@nroFactura", nroFactura);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        
                        // Buscar el DataGridView en la ventana flotante
                        var dgvDetalle = frmDetalle.Controls.Find("dgvDetalle", true).FirstOrDefault() as DataGridView;
                        if (dgvDetalle != null)
                        {
                            dgvDetalle.DataSource = dt;
                            FormatearColumnasDetalle(dgvDetalle);
                        }

                        // Actualizar totales
                        ActualizarTotalesDetalle(dt);
                        
                        // Actualizar el título de la ventana con información adicional
                        ActualizarTituloDetalle(nroFactura);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el detalle de la factura: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ActualizarTituloDetalle(string nroFactura)
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
                    var query = @"
                        SELECT 
                            TipoFactura,
                            FormadePago,
                            CAENumero,
                            CUITCliente
                        FROM Facturas 
                        WHERE NumeroFactura = @nroFactura";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@nroFactura", nroFactura);
                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string tipoFactura = reader["TipoFactura"]?.ToString() ?? "";
                                string formaPago = reader["FormadePago"]?.ToString() ?? "";
                                string cae = reader["CAENumero"]?.ToString() ?? "";
                                string cuit = reader["CUITCliente"]?.ToString() ?? "";

                                string titulo = $"Detalle {tipoFactura} N° {nroFactura}";
                                
                                if (!string.IsNullOrEmpty(cae))
                                    titulo += $" - CAE: {cae}";
                                
                                if (!string.IsNullOrEmpty(cuit))
                                    titulo += $" - CUIT: {cuit}";

                                frmDetalle.Text = titulo;
                            }
                            else
                            {
                                frmDetalle.Text = $"Detalle de Factura N° {nroFactura}";
                            }
                        }
                    }
                }
            }
            catch
            {
                frmDetalle.Text = $"Detalle de Factura N° {nroFactura}";
            }
        }

        private void ActualizarResumen(DataTable dt)
        {
            int cantidadFacturas = dt.Rows.Count;
            decimal totalVentas = 0;

            foreach (DataRow row in dt.Rows)
            {
                if (decimal.TryParse(row["Total Venta"].ToString(), out decimal total))
                {
                    totalVentas += total;
                }
            }

            lblCantidadVentas.Text = $"Facturas: {cantidadFacturas}";
            lblTotal.Text = $"Total: {totalVentas:C2}";
        }

        private void FormatearColumnasDetalle(DataGridView dgvDetalle)
        {
            if (dgvDetalle.Columns.Count == 0) return;

            // Temporalmente deshabilitar AutoSizeColumnsMode para permitir configuración manual
            var originalAutoSizeMode = dgvDetalle.AutoSizeColumnsMode;
            dgvDetalle.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            try
            {
                var codigoCol = dgvDetalle.Columns["Código"];
                if (codigoCol != null)
                {
                    codigoCol.Width = 80;
                    codigoCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var productoCol = dgvDetalle.Columns["Producto"];
                if (productoCol != null)
                {
                    productoCol.Width = 250;
                }

                var cantidadCol = dgvDetalle.Columns["Cantidad"];
                if (cantidadCol != null)
                {
                    cantidadCol.Width = 80;
                    cantidadCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var precioCol = dgvDetalle.Columns["Precio Unit."];
                if (precioCol != null)
                {
                    precioCol.DefaultCellStyle.Format = "C2";
                    precioCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    precioCol.Width = 100;
                }

                var totalCol = dgvDetalle.Columns["Total"];
                if (totalCol != null)
                {
                    totalCol.DefaultCellStyle.Format = "C2";
                    totalCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    totalCol.Width = 100;
                }
            }
            finally
            {
                // Restaurar el modo original
                dgvDetalle.AutoSizeColumnsMode = originalAutoSizeMode;
            }
        }

        private void ActualizarTotalesDetalle(DataTable dt)
        {
            int cantidadProductos = dt.Rows.Count;
            decimal cantidadTotal = 0;
            decimal totalFactura = 0;

            foreach (DataRow row in dt.Rows)
            {
                if (decimal.TryParse(row["Cantidad"].ToString(), out decimal cantidad))
                {
                    cantidadTotal += cantidad;
                }

                if (decimal.TryParse(row["Total"].ToString(), out decimal total))
                {
                    totalFactura += total;
                }
            }

            // Actualizar los labels de totales
            var lblCantidadProductos = frmDetalle.Controls.Find("lblCantidadProductos", true).FirstOrDefault() as Label;
            if (lblCantidadProductos != null)
                lblCantidadProductos.Text = $"Productos: {cantidadProductos}";

            var lblCantidadTotalDetalle = frmDetalle.Controls.Find("lblCantidadTotalDetalle", true).FirstOrDefault() as Label;
            if (lblCantidadTotalDetalle != null)
                lblCantidadTotalDetalle.Text = $"Cantidad: {cantidadTotal:N0}";

            var lblTotalFactura = frmDetalle.Controls.Find("lblTotalFactura", true).FirstOrDefault() as Label;
            if (lblTotalFactura != null)
                lblTotalFactura.Text = $"Total: {totalFactura:C2}";
        }

        private void BtnBuscar_Click(object sender, EventArgs e)
        {
            if (frmDetalle != null && frmDetalle.Visible)
                frmDetalle.Hide();
            CargarVentasPorFecha(dtpFecha.Value.Date);
        }

        private void BtnHoy_Click(object sender, EventArgs e)
        {
            if (frmDetalle != null && frmDetalle.Visible)
                frmDetalle.Hide();
            dtpFecha.Value = DateTime.Today;
            CargarVentasDelDia();
        }

        private void CargarVentasDelDia()
        {
            CargarVentasPorFecha(DateTime.Today);
        }

        private string GetConnectionString()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return config.GetConnectionString("DefaultConnection");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                frmDetalle?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
