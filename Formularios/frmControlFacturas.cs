using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Comercio.NET.Servicios;
using TicketConfig = Comercio.NET.Servicios.TicketConfig; // Alias específico

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
        private CheckBox chkCtaCte; // Checkbox para Cuenta Corriente
        private Panel panelTotales;
        private Label lblDetalleTiposFactura;
        private Label lblDetalleFormasPago;
        private TextBox txtFiltroCtaCte; // AGREGAR: TextBox para filtrar por nombre Cta Cte

        public frmControlFacturas()
        {
            InitializeComponent();
            ConfigurarFormulario();
            CrearVentanaDetalle();
            CargarVentasDelDia();
        }

        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmControlFacturas));
            dgvVentas = new DataGridView();
            dtpFecha = new DateTimePicker();
            btnBuscar = new Button();
            btnHoy = new Button();
            lblTotal = new Label();
            lblCantidadVentas = new Label();
            lblTitulo = new Label();
            panelFiltros = new Panel();
            txtFiltroCtaCte = new TextBox();
            chkCtaCte = new CheckBox();
            panelResumen = new Panel();
            panelTotales = new Panel();
            lblDetalleTiposFactura = new Label();
            lblDetalleFormasPago = new Label();
            ((System.ComponentModel.ISupportInitialize)dgvVentas).BeginInit();
            panelFiltros.SuspendLayout();
            panelResumen.SuspendLayout();
            panelTotales.SuspendLayout();
            SuspendLayout();
            // 
            // dgvVentas
            // 
            dgvVentas.AllowUserToAddRows = false;
            dgvVentas.AllowUserToDeleteRows = false;
            dgvVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvVentas.BackgroundColor = Color.White;
            dgvVentas.BorderStyle = BorderStyle.None;
            dgvVentas.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(248, 249, 250);
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = Color.Black;
            dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(248, 249, 250);
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dgvVentas.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvVentas.ColumnHeadersHeight = 35;
            dgvVentas.Dock = DockStyle.Fill;
            dgvVentas.EnableHeadersVisualStyles = false;
            dgvVentas.Location = new Point(0, 110);
            dgvVentas.Name = "dgvVentas";
            dgvVentas.ReadOnly = true;
            dgvVentas.RowHeadersVisible = false;
            dgvVentas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvVentas.Size = new Size(991, 391);
            dgvVentas.TabIndex = 0;
            dgvVentas.CellClick += DgvVentas_CellClick;
            dgvVentas.Click += FrmControlFacturas_Click;
            // 
            // dtpFecha
            // 
            dtpFecha.Font = new Font("Segoe UI", 10F);
            dtpFecha.Format = DateTimePickerFormat.Short;
            dtpFecha.Location = new Point(20, 15);
            dtpFecha.Name = "dtpFecha";
            dtpFecha.Size = new Size(120, 25);
            dtpFecha.TabIndex = 4;
            dtpFecha.Value = new DateTime(2025, 9, 5, 0, 0, 0, 0);
            dtpFecha.Value = DateTime.Today;
            // 
            // btnBuscar
            // 
            btnBuscar.BackColor = Color.FromArgb(0, 120, 215);
            btnBuscar.FlatStyle = FlatStyle.Flat;
            btnBuscar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnBuscar.ForeColor = Color.White;
            btnBuscar.Location = new Point(150, 15);
            btnBuscar.Name = "btnBuscar";
            btnBuscar.Size = new Size(80, 30);
            btnBuscar.TabIndex = 3;
            btnBuscar.Text = "Buscar";
            btnBuscar.UseVisualStyleBackColor = false;
            btnBuscar.Click += BtnBuscar_Click;
            // 
            // btnHoy
            // 
            btnHoy.BackColor = Color.FromArgb(0, 150, 136);
            btnHoy.FlatStyle = FlatStyle.Flat;
            btnHoy.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnHoy.ForeColor = Color.White;
            btnHoy.Location = new Point(240, 15);
            btnHoy.Name = "btnHoy";
            btnHoy.Size = new Size(80, 30);
            btnHoy.TabIndex = 2;
            btnHoy.Text = "Hoy";
            btnHoy.UseVisualStyleBackColor = false;
            btnHoy.Click += BtnHoy_Click;
            // 
            // lblTotal
            // 
            lblTotal.Dock = DockStyle.Top;
            lblTotal.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTotal.ForeColor = Color.White;
            lblTotal.Location = new Point(0, 0);
            lblTotal.Name = "lblTotal";
            lblTotal.Size = new Size(991, 25);
            lblTotal.TabIndex = 2;
            lblTotal.Text = "Total: $0,00";
            lblTotal.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblCantidadVentas
            // 
            lblCantidadVentas.Dock = DockStyle.Left;
            lblCantidadVentas.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblCantidadVentas.ForeColor = Color.White;
            lblCantidadVentas.Location = new Point(0, 0);
            lblCantidadVentas.Name = "lblCantidadVentas";
            lblCantidadVentas.Padding = new Padding(20, 0, 0, 0);
            lblCantidadVentas.Size = new Size(200, 80);
            lblCantidadVentas.TabIndex = 0;
            lblCantidadVentas.Text = "Ventas: 0";
            lblCantidadVentas.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblTitulo
            // 
            lblTitulo.Dock = DockStyle.Top;
            lblTitulo.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitulo.ForeColor = Color.FromArgb(0, 120, 215);
            lblTitulo.Location = new Point(0, 0);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(991, 50);
            lblTitulo.TabIndex = 3;
            lblTitulo.Text = "Control de Facturas - Ventas del Día";
            lblTitulo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelFiltros
            // 
            panelFiltros.BackColor = Color.FromArgb(248, 249, 250);
            panelFiltros.Controls.Add(txtFiltroCtaCte);
            panelFiltros.Controls.Add(chkCtaCte);
            panelFiltros.Controls.Add(btnHoy);
            panelFiltros.Controls.Add(btnBuscar);
            panelFiltros.Controls.Add(dtpFecha);
            panelFiltros.Dock = DockStyle.Top;
            panelFiltros.Location = new Point(0, 50);
            panelFiltros.Name = "panelFiltros";
            panelFiltros.Padding = new Padding(10);
            panelFiltros.Size = new Size(991, 60);
            panelFiltros.TabIndex = 2;
            panelFiltros.Click += FrmControlFacturas_Click;
            // 
            // txtFiltroCtaCte
            // 
            txtFiltroCtaCte.Font = new Font("Segoe UI", 10F);
            txtFiltroCtaCte.Location = new Point(470, 17);
            txtFiltroCtaCte.Name = "txtFiltroCtaCte";
            txtFiltroCtaCte.PlaceholderText = "Buscar cliente...";
            txtFiltroCtaCte.Size = new Size(200, 25);
            txtFiltroCtaCte.TabIndex = 0;
            txtFiltroCtaCte.Visible = false;
            txtFiltroCtaCte.TextChanged += TxtFiltroCtaCte_TextChanged;
            // 
            // chkCtaCte
            // 
            chkCtaCte.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            chkCtaCte.ForeColor = Color.FromArgb(0, 120, 215);
            chkCtaCte.Location = new Point(340, 15);
            chkCtaCte.Name = "chkCtaCte";
            chkCtaCte.Size = new Size(120, 30);
            chkCtaCte.TabIndex = 1;
            chkCtaCte.Text = "Cta. Cte.";
            chkCtaCte.UseVisualStyleBackColor = true;
            chkCtaCte.CheckedChanged += ChkCtaCte_CheckedChanged;
            // 
            // panelResumen
            // 
            panelResumen.BackColor = Color.FromArgb(0, 120, 215);
            panelResumen.Controls.Add(lblCantidadVentas);
            panelResumen.Controls.Add(panelTotales);
            panelResumen.Dock = DockStyle.Bottom;
            panelResumen.Location = new Point(0, 501);
            panelResumen.Name = "panelResumen";
            panelResumen.Size = new Size(991, 80);
            panelResumen.TabIndex = 1;
            panelResumen.Click += FrmControlFacturas_Click;
            // 
            // panelTotales
            // 
            panelTotales.BackColor = Color.FromArgb(0, 120, 215);
            panelTotales.Controls.Add(lblDetalleTiposFactura);
            panelTotales.Controls.Add(lblDetalleFormasPago);
            panelTotales.Controls.Add(lblTotal);
            panelTotales.Dock = DockStyle.Fill;
            panelTotales.Location = new Point(0, 0);
            panelTotales.Name = "panelTotales";
            panelTotales.Size = new Size(991, 80);
            panelTotales.TabIndex = 1;
            // 
            // lblDetalleTiposFactura
            // 
            lblDetalleTiposFactura.Dock = DockStyle.Top;
            lblDetalleTiposFactura.Font = new Font("Segoe UI", 9F);
            lblDetalleTiposFactura.ForeColor = Color.White;
            lblDetalleTiposFactura.Location = new Point(0, 45);
            lblDetalleTiposFactura.Name = "lblDetalleTiposFactura";
            lblDetalleTiposFactura.Size = new Size(991, 20);
            lblDetalleTiposFactura.TabIndex = 0;
            lblDetalleTiposFactura.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblDetalleFormasPago
            // 
            lblDetalleFormasPago.Dock = DockStyle.Top;
            lblDetalleFormasPago.Font = new Font("Segoe UI", 9F);
            lblDetalleFormasPago.ForeColor = Color.White;
            lblDetalleFormasPago.Location = new Point(0, 25);
            lblDetalleFormasPago.Name = "lblDetalleFormasPago";
            lblDetalleFormasPago.Size = new Size(991, 20);
            lblDetalleFormasPago.TabIndex = 0;
            lblDetalleFormasPago.TextAlign = ContentAlignment.MiddleRight;
            // 
            // frmControlFacturas
            // 
            AutoScaleDimensions = new SizeF(8F, 16F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(991, 581);
            Controls.Add(dgvVentas);
            Controls.Add(panelResumen);
            Controls.Add(panelFiltros);
            Controls.Add(lblTitulo);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "frmControlFacturas";
            Text = "Control de Facturas";
            WindowState = FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)dgvVentas).EndInit();
            panelFiltros.ResumeLayout(false);
            panelFiltros.PerformLayout();
            panelResumen.ResumeLayout(false);
            panelTotales.ResumeLayout(false);
            ResumeLayout(false);

        }

        private void ConfigurarFormulario()
        {
            // Configuración inicial del formulario
            this.dgvVentas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvVentas.MultiSelect = false;
            this.dgvVentas.ReadOnly = true;
            this.dgvVentas.AutoGenerateColumns = true; // CAMBIAR: permitir auto-generación

            // Cargar los eventos de las columnas
            CargarEventosColumnas();

            // NO formatear columnas aquí, se hará después de cargar datos
            // FormatearColumnas(); // COMENTAR esta línea

            // Establecer el rango de fechas por defecto
            dtpFecha.Value = DateTime.Today;
        }

        private void CrearVentanaDetalle()
        {
            // Crear la ventana flotante para el detalle de la factura
            frmDetalle = new Form();
            frmDetalle.Text = "Detalle de Factura";
            frmDetalle.Size = new Size(800, 600);
            frmDetalle.StartPosition = FormStartPosition.CenterParent;
            frmDetalle.FormBorderStyle = FormBorderStyle.FixedDialog;
            frmDetalle.MaximizeBox = false;
            frmDetalle.MinimizeBox = false;

            // Agregar un DataGridView para mostrar los detalles de la factura
            var dgvDetalle = new DataGridView();
            dgvDetalle.Name = "dgvDetalle";
            dgvDetalle.Dock = DockStyle.Fill;
            dgvDetalle.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDetalle.ReadOnly = true;
            dgvDetalle.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            frmDetalle.Controls.Add(dgvDetalle);

            // Agregar un botón para cerrar la ventana de detalle
            var btnCerrar = new Button();
            btnCerrar.Text = "Cerrar";
            btnCerrar.Dock = DockStyle.Bottom;
            btnCerrar.Click += (s, e) => { frmDetalle.Hide(); };
            frmDetalle.Controls.Add(btnCerrar);
        }

        private void CargarVentasDelDia()
        {
            // Usar el método que ya funciona correctamente
            CargarVentasPorFecha(DateTime.Today);
        }

        // MODIFICAR: Método de carga con filtro de Cuenta Corriente y columna dinámica
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
                    // Query dinámico según el checkbox
                    var query = chkCtaCte.Checked 
                        ? @"SELECT 
                            NumeroRemito as 'N° Remito',
                            NroFactura as 'N° Factura',
                            Fecha as 'Fecha',
                            Hora as 'Hora',
                            ImporteTotal as 'Total Venta',
                            FormadePago as 'Forma de Pago',
                            TipoFactura as 'Tipo Factura',
                            CAENumero as 'CAE',
                            CtaCteNombre as 'Cta. Cte. Nombre'
                        FROM Facturas 
                        WHERE CAST(Fecha AS DATE) = @fecha 
                        AND esCtaCte = @esCtaCte
                        ORDER BY NumeroRemito DESC"
                        : @"SELECT 
                            NumeroRemito as 'N° Remito',
                            NroFactura as 'N° Factura',
                            Fecha as 'Fecha',
                            Hora as 'Hora',
                            ImporteTotal as 'Total Venta',
                            FormadePago as 'Forma de Pago',
                            TipoFactura as 'Tipo Factura',
                            CAENumero as 'CAE',
                            CUITCliente as 'CUIT Cliente'
                        FROM Facturas 
                        WHERE CAST(Fecha AS DATE) = @fecha 
                        AND esCtaCte = @esCtaCte
                        ORDER BY NumeroRemito DESC";

                    // Depuración: mostrar la consulta que se va a ejecutar
                    System.Diagnostics.Debug.WriteLine($"Ejecutando consulta: {query}");
                    System.Diagnostics.Debug.WriteLine($"Parámetros: fecha={fecha:yyyy-MM-dd}, esCtaCte={chkCtaCte.Checked}");

                    using (var adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@fecha", fecha.Date);
                        adapter.SelectCommand.Parameters.AddWithValue("@esCtaCte", chkCtaCte.Checked);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        
                        dgvVentas.DataSource = dt;
                        FormatearColumnas();
                        
                        // Si hay filtro de texto aplicado, reaplicarlo
                        if (chkCtaCte.Checked && !string.IsNullOrEmpty(txtFiltroCtaCte.Text))
                        {
                            FiltrarDatosCtaCte();
                        }
                        else
                        {
                            ActualizarResumen(dt);
                        }
                    }
                }

                // Actualizar título para mostrar el tipo de ventas
                string tipoVenta = chkCtaCte.Checked ? "Cuenta Corriente" : "Contado";
                lblTitulo.Text = fecha.Date == DateTime.Today 
                    ? $"Control de Facturas - Ventas del Día ({tipoVenta})" 
                    : $"Control de Facturas - Ventas del {fecha:dd/MM/yyyy} ({tipoVenta})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las ventas: {ex.Message}\n\nDetalles: {ex.ToString()}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Event handler para el botón Buscar
        private void BtnBuscar_Click(object sender, EventArgs e)
        {
            CargarVentasPorFecha(dtpFecha.Value.Date);
        }

        // Event handler para el botón Hoy
        private void BtnHoy_Click(object sender, EventArgs e)
        {
            dtpFecha.Value = DateTime.Today;
            CargarVentasPorFecha(DateTime.Today);
        }

        // Método para formatear columnas del DataGridView de detalle
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

        // Método para actualizar totales en la ventana de detalle
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

        // Método para actualizar resumen en el panel principal
        private void ActualizarResumen(DataTable dt)
        {
            try
            {
                int cantidadVentas = dt.Rows.Count;
                decimal totalVentas = 0;
                
                // Diccionarios para contar tipos de factura y formas de pago
                var tiposFactura = new Dictionary<string, int>();
                var formasPago = new Dictionary<string, decimal>();

                foreach (DataRow row in dt.Rows)
                {
                    // Sumar total de ventas
                    if (decimal.TryParse(row["Total Venta"].ToString(), out decimal total))
                    {
                        totalVentas += total;
                    }

                    // Contar tipos de factura
                    string tipoFactura = row["Tipo Factura"]?.ToString() ?? "Sin especificar";
                    if (tiposFactura.ContainsKey(tipoFactura))
                        tiposFactura[tipoFactura]++;
                    else
                        tiposFactura[tipoFactura] = 1;

                    // Sumar por formas de pago
                    string formaPago = row["Forma de Pago"]?.ToString() ?? "Sin especificar";
                    if (formasPago.ContainsKey(formaPago))
                        formasPago[formaPago] += total;
                    else
                        formasPago[formaPago] = total;
                }

                // Actualizar labels principales
                lblCantidadVentas.Text = $"Ventas: {cantidadVentas}";
                lblTotal.Text = $"Total: {totalVentas:C2}";

                // Actualizar detalle de tipos de factura
                string detalleTipos = string.Join(" | ", 
                    tiposFactura.Select(kv => $"{kv.Key}: {kv.Value}"));
                lblDetalleTiposFactura.Text = detalleTipos;

                // Actualizar detalle de formas de pago
                string detalleFormas = string.Join(" | ", 
                    formasPago.Select(kv => $"{kv.Key}: {kv.Value:C2}"));
                lblDetalleFormasPago.Text = detalleFormas;
            }
            catch (Exception ex)
            {
                // En caso de error, mostrar valores por defecto
                lblCantidadVentas.Text = "Ventas: 0";
                lblTotal.Text = "Total: $0,00";
                lblDetalleTiposFactura.Text = "";
                lblDetalleFormasPago.Text = "";
            }
        }

        // Event handler para el botón Imprimir
        private void BtnImprimir_Click(object sender, EventArgs e)
        {
            try
            {
                // Obtener el número de Remito actual desde el título de la ventana
                string tituloCompleto = frmDetalle.Text;
                string numeroRemito = ExtraerNumeroRemitoDelTitulo(tituloCompleto);
                
                if (string.IsNullOrEmpty(numeroRemito))
                {
                    MessageBox.Show("No se puede determinar el número de Remito para imprimir.", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Obtener datos de la factura
                var datosFactura = ObtenerDatosFactura(numeroRemito);
                if (datosFactura == null)
                {
                    MessageBox.Show("No se pudieron obtener los datos de la Remito.", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Obtener detalle de productos de la venta
                var dgvDetalle = frmDetalle.Controls.Find("dgvDetalle", true).FirstOrDefault() as DataGridView;
                if (dgvDetalle?.DataSource is DataTable dtDetalle && dtDetalle.Rows.Count > 0)
                {
                    // Configurar el ticket con los datos reales usando la clase del servicio
                    var config = new TicketConfig
                    {
                        NombreComercio = datosFactura.NombreComercio,
                        DomicilioComercio = datosFactura.DomicilioComercio,
                        NumeroComprobante = numeroRemito,
                        FormaPago = datosFactura.FormaPago,
                        MensajePie = "Gracias por su compra!",
                        TipoComprobante = ObtenerTipoComprobanteLegible(datosFactura.TipoFactura),
                        CAE = datosFactura.CAENumero,
                        CAEVencimiento = datosFactura.CAEVencimiento,
                        CUIT = datosFactura.CUITCliente
                    };

                    // Convertir los datos del DataGridView al formato esperado por el servicio
                    DataTable dtParaImpresion = ConvertirDatosParaImpresion(dtDetalle);

                    // Usar el servicio de impresión
                    using (var ticketService = new TicketPrintingService())
                    {
                        ticketService.ImprimirTicket(dtParaImpresion, config);
                    }

                    MessageBox.Show("Ticket enviado a la impresora.", "Éxito", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No hay productos para imprimir.", "Información", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Método auxiliar para extraer número de remito del título
        private string ExtraerNumeroRemitoDelTitulo(string titulo)
        {
            // Buscar patrón "N° XXXX" en el título
            var match = System.Text.RegularExpressions.Regex.Match(titulo, @"N°\s*(\d+)");
            return match.Success ? match.Groups[1].Value : "";
        }

        // Event handler para el click en el formulario
        private void FrmControlFacturas_Click(object sender, EventArgs e)
        {
            if (frmDetalle != null && frmDetalle.Visible)
            {
                frmDetalle.Hide();
            }
        }

        // Método para cargar eventos de columnas
        private void CargarEventosColumnas()
        {
            // Aquí puedes agregar eventos específicos para las columnas si es necesario
        }

        // Método para calcular total de ventas
        private void CalcularTotalVentas()
        {
            decimal total = 0;
            
            if (dgvVentas.DataSource is DataTable dt)
            {
                foreach (DataRow row in dt.Rows)
                {
                    if (decimal.TryParse(row["Total Venta"].ToString(), out decimal totalVenta))
                    {
                        total += totalVenta;
                    }
                }
            }
            
            lblTotal.Text = $"Total: {total:C2}";
        }

        // Clase auxiliar para los datos de la factura
        private class DatosFactura
        {
            public string TipoFactura { get; set; }
            public string FormaPago { get; set; }
            public string CAENumero { get; set; }
            public DateTime? CAEVencimiento { get; set; }
            public string CUITCliente { get; set; }
            public string NombreComercio { get; set; }
            public string DomicilioComercio { get; set; }
        }

        // Event handler para el click en las celdas del DataGridView
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

        // Método para formatear columnas del DataGridView principal
        private void FormatearColumnas()
        {
            if (dgvVentas.Columns.Count == 0) return;

            var originalAutoSizeMode = dgvVentas.AutoSizeColumnsMode;
            dgvVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            try
            {
                // Formatear columnas principales
                var totalVentaCol = dgvVentas.Columns["Total Venta"];
                if (totalVentaCol != null)
                {
                    totalVentaCol.DefaultCellStyle.Format = "C2";
                    totalVentaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    totalVentaCol.Width = 120;
                }

                var fechaCol = dgvVentas.Columns["Fecha"];
                if (fechaCol != null)
                {
                    fechaCol.DefaultCellStyle.Format = "dd/MM/yyyy";
                    fechaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    fechaCol.Width = 100;
                }

                var horaCol = dgvVentas.Columns["Hora"];
                if (horaCol != null)
                {
                    horaCol.Width = 80;
                    horaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
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

                // Formatear columnas dinámicas si existen
                var remitoCol = dgvVentas.Columns["N° Remito"];
                if (remitoCol != null)
                {
                    remitoCol.Width = 100;
                    remitoCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var ctaCteCol = dgvVentas.Columns["Cta. Cte. Nombre"];
                if (ctaCteCol != null)
                {
                    ctaCteCol.Width = 150;
                    ctaCteCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }
            }
            finally
            {
                dgvVentas.AutoSizeColumnsMode = originalAutoSizeMode;
            }
        }

        // MODIFICAR: Event handler para el checkbox
        private void ChkCtaCte_CheckedChanged(object sender, EventArgs e)
        {
            // Mostrar/ocultar el TextBox de filtro
            txtFiltroCtaCte.Visible = chkCtaCte.Checked;
            
            if (chkCtaCte.Checked)
            {
                // Limpiar el filtro y hacer foco
                txtFiltroCtaCte.Text = "";
                txtFiltroCtaCte.Focus();
            }
            
            // Recargar los datos con el filtro actual
            CargarVentasPorFecha(dtpFecha.Value.Date);
        }

        // AGREGAR: Event handler para el TextBox de filtro
        private void TxtFiltroCtaCte_TextChanged(object sender, EventArgs e)
        {
            if (chkCtaCte.Checked)
            {
                // Filtrar los datos existentes en tiempo real
                FiltrarDatosCtaCte();
            }
        }

        // AGREGAR: Método para filtrar datos por nombre de cuenta corriente
        private void FiltrarDatosCtaCte()
        {
            if (dgvVentas.DataSource is DataTable dt)
            {
                string filtro = txtFiltroCtaCte.Text.Trim();
                
                if (string.IsNullOrEmpty(filtro))
                {
                    // Si no hay filtro, mostrar todas las filas
                    dt.DefaultView.RowFilter = "";
                }
                else
                {
                    // Aplicar filtro por nombre de cuenta corriente (búsqueda parcial, sin distinguir mayúsculas)
                    dt.DefaultView.RowFilter = $"[Cta. Cte. Nombre] LIKE '%{filtro.Replace("'", "''")}%'";
               
                    // Si se encuentra una coincidencia exacta, seleccionar esa fila
                    if (dt.DefaultView.Count == 1 && dgvVentas.Rows.Count > 0)
                    {
                        try
                        {
                            // Buscar la fila que corresponde al registro filtrado
                            var dataRowView = dt.DefaultView[0];
                            string numeroRemito = dataRowView["N° Remito"]?.ToString() ?? dataRowView["N° Factura"]?.ToString();

                            // Buscar la fila en el DataGridView que tenga ese número
                            foreach (DataGridViewRow dgvRow in dgvVentas.Rows)
                            {
                                string valorCelda = dgvRow.Cells["N° Remito"]?.Value?.ToString() ?? dgvRow.Cells["N° Factura"]?.Value?.ToString();
                                if (valorCelda == numeroRemito)
                                {
                                    // Seleccionar la fila encontrada
                                    dgvVentas.ClearSelection();
                                    dgvRow.Selected = true;
                                    dgvVentas.CurrentCell = dgvRow.Cells[0];
                                    
                                    // Hacer scroll para que la fila sea visible
                                    if (dgvRow.Index < dgvVentas.FirstDisplayedScrollingRowIndex || 
                                        dgvRow.Index > dgvVentas.FirstDisplayedScrollingRowIndex + dgvVentas.DisplayedRowCount(false))
                                    {
                                        dgvVentas.FirstDisplayedScrollingRowIndex = dgvRow.Index;
                                    }
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            // Si hay algún error, simplemente no seleccionar nada
                        }
                    }
                }
                
                // Actualizar el resumen con los datos filtrados
                ActualizarResumenFiltrado();
            }
        }

        // AGREGAR: Método para actualizar resumen con datos filtrados
        private void ActualizarResumenFiltrado()
        {
            if (dgvVentas.DataSource is DataTable dt)
            {
                // Crear un DataTable temporal con solo las filas visibles
                DataTable dtFiltrado = dt.DefaultView.ToTable();
                ActualizarResumen(dtFiltrado);
            }
        }

        // Método para cargar detalle de una factura específica
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
                    // Consultar desde la tabla Ventas usando nrofactura
                    var query = @"
                        SELECT 
                            codigo as 'Código',
                            descripcion as 'Producto',
                            cantidad as 'Cantidad',
                            precio as 'Precio Unit.',
                            total as 'Total'
                        FROM Ventas 
                        WHERE numero = @nroFactura
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

        // Método para mostrar la ventana de detalle
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

        // Método para actualizar el título de la ventana de detalle
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
                            tipofactura,
                            formapago,
                            caenumero,
                            cuitcliente
                        FROM Ventas 
                        WHERE numero = @nroFactura";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@nroFactura", nroFactura);
                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string tipoFactura = reader["tipofactura"]?.ToString() ?? "";
                                string formaPago = reader["formapago"]?.ToString() ?? "";
                                string cae = reader["caenumero"]?.ToString() ?? "";
                                string cuit = reader["cuitcliente"]?.ToString() ?? "";

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

        // Método para obtener datos de la factura para impresión
        private DatosFactura ObtenerDatosFactura(string numeroFactura)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                
                string connectionString = config.GetConnectionString("DefaultConnection");
                string nombreComercio = config["Comercio:Nombre"] ?? "Comercio";
                string domicilioComercio = config["Comercio:Domicilio"] ?? "Domicilio";

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"SELECT tipofactura, formapago, caenumero, cuitcliente 
                                 FROM Ventas WHERE numero = @numeroFactura";
                    
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@numeroFactura", numeroFactura);
                        connection.Open();
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new DatosFactura
                                {
                                    TipoFactura = reader["tipofactura"]?.ToString() ?? "",
                                    FormaPago = reader["formapago"]?.ToString() ?? "",
                                    CAENumero = reader["caenumero"]?.ToString() ?? "",
                                    CAEVencimiento = null,
                                    CUITCliente = reader["cuitcliente"]?.ToString() ?? "",
                                    NombreComercio = nombreComercio,
                                    DomicilioComercio = domicilioComercio
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener datos de la factura: {ex.Message}", "Error");
            }
            
            return null;
        }

        // Método para obtener tipo de comprobante legible
        private string ObtenerTipoComprobanteLegible(string tipoFactura)
        {
            return tipoFactura switch
            {
                "Remito" => "REMITO",
                "FacturaA" => "FACTURA A",
                "FacturaB" => "FACTURA B",
                _ => "COMPROBANTE"
            };
        }

        // Método para convertir datos para impresión
        private DataTable ConvertirDatosParaImpresion(DataTable dtOriginal)
        {
            // Crear una nueva tabla con las columnas esperadas por el servicio de impresión
            DataTable dtConvertida = new DataTable();
            dtConvertida.Columns.Add("codigo", typeof(string));
            dtConvertida.Columns.Add("descripcion", typeof(string));
            dtConvertida.Columns.Add("cantidad", typeof(int));
            dtConvertida.Columns.Add("precio", typeof(decimal));
            dtConvertida.Columns.Add("total", typeof(decimal));

            foreach (DataRow row in dtOriginal.Rows)
            {
                DataRow newRow = dtConvertida.NewRow();
                newRow["codigo"] = row["Código"];
                newRow["descripcion"] = row["Producto"];
                newRow["cantidad"] = Convert.ToInt32(row["Cantidad"]);
                newRow["precio"] = Convert.ToDecimal(row["Precio Unit."]);
                newRow["total"] = Convert.ToDecimal(row["Total"]);
                dtConvertida.Rows.Add(newRow);
            }

            return dtConvertida;
        }
    }
}
