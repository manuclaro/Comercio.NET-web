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
            
            // FORZAR MAXIMIZACIÓN
            //this.WindowState = FormWindowState.Maximized;
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
            dgvVentas.Location = new Point(0, 103);
            dgvVentas.Name = "dgvVentas";
            dgvVentas.ReadOnly = true;
            dgvVentas.RowHeadersVisible = false;
            dgvVentas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvVentas.Size = new Size(909, 333);
            dgvVentas.TabIndex = 0;
            dgvVentas.CellClick += DgvVentas_CellClick;
            dgvVentas.Click += FrmControlFacturas_Click;
            // 
            // dtpFecha
            // 
            dtpFecha.Font = new Font("Segoe UI", 10F);
            dtpFecha.Format = DateTimePickerFormat.Short;
            dtpFecha.Location = new Point(18, 14);
            dtpFecha.Name = "dtpFecha";
            dtpFecha.Size = new Size(106, 25);
            dtpFecha.TabIndex = 4;
            dtpFecha.Value = new DateTime(2025, 9, 20, 0, 0, 0, 0);
            // 
            // btnBuscar
            // 
            btnBuscar.BackColor = Color.FromArgb(0, 120, 215);
            btnBuscar.FlatStyle = FlatStyle.Flat;
            btnBuscar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnBuscar.ForeColor = Color.White;
            btnBuscar.Location = new Point(131, 14);
            btnBuscar.Name = "btnBuscar";
            btnBuscar.Size = new Size(70, 28);
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
            btnHoy.Location = new Point(210, 14);
            btnHoy.Name = "btnHoy";
            btnHoy.Size = new Size(70, 28);
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
            lblTotal.Size = new Size(909, 23);
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
            lblCantidadVentas.Padding = new Padding(18, 0, 0, 0);
            lblCantidadVentas.Size = new Size(175, 75);
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
            lblTitulo.Size = new Size(909, 47);
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
            panelFiltros.Location = new Point(0, 47);
            panelFiltros.Name = "panelFiltros";
            panelFiltros.Padding = new Padding(9);
            panelFiltros.Size = new Size(909, 56);
            panelFiltros.TabIndex = 2;
            panelFiltros.Click += FrmControlFacturas_Click;
            // 
            // txtFiltroCtaCte
            // 
            txtFiltroCtaCte.Font = new Font("Segoe UI", 10F);
            txtFiltroCtaCte.Location = new Point(411, 16);
            txtFiltroCtaCte.Name = "txtFiltroCtaCte";
            txtFiltroCtaCte.PlaceholderText = "Buscar cliente...";
            txtFiltroCtaCte.Size = new Size(176, 25);
            txtFiltroCtaCte.TabIndex = 0;
            txtFiltroCtaCte.Visible = false;
            txtFiltroCtaCte.TextChanged += TxtFiltroCtaCte_TextChanged;
            // 
            // chkCtaCte
            // 
            chkCtaCte.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            chkCtaCte.ForeColor = Color.FromArgb(0, 120, 215);
            chkCtaCte.Location = new Point(298, 14);
            chkCtaCte.Name = "chkCtaCte";
            chkCtaCte.Size = new Size(105, 28);
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
            panelResumen.Location = new Point(0, 436);
            panelResumen.Name = "panelResumen";
            panelResumen.Size = new Size(909, 75);
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
            panelTotales.Size = new Size(909, 75);
            panelTotales.TabIndex = 1;
            // 
            // lblDetalleTiposFactura
            // 
            lblDetalleTiposFactura.Dock = DockStyle.Top;
            lblDetalleTiposFactura.Font = new Font("Segoe UI", 9F);
            lblDetalleTiposFactura.ForeColor = Color.White;
            lblDetalleTiposFactura.Location = new Point(0, 42);
            lblDetalleTiposFactura.Name = "lblDetalleTiposFactura";
            lblDetalleTiposFactura.Size = new Size(909, 19);
            lblDetalleTiposFactura.TabIndex = 0;
            lblDetalleTiposFactura.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblDetalleFormasPago
            // 
            lblDetalleFormasPago.Dock = DockStyle.Top;
            lblDetalleFormasPago.Font = new Font("Segoe UI", 9F);
            lblDetalleFormasPago.ForeColor = Color.White;
            lblDetalleFormasPago.Location = new Point(0, 23);
            lblDetalleFormasPago.Name = "lblDetalleFormasPago";
            lblDetalleFormasPago.Size = new Size(909, 19);
            lblDetalleFormasPago.TabIndex = 0;
            lblDetalleFormasPago.TextAlign = ContentAlignment.MiddleRight;
            // 
            // frmControlFacturas
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(909, 511);
            Controls.Add(dgvVentas);
            Controls.Add(panelResumen);
            Controls.Add(panelFiltros);
            Controls.Add(lblTitulo);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(925, 550);
            Name = "frmControlFacturas";
            Text = "Control de Facturas";
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
            // Crear la ventana flotante para el detalle de la factura (MÁS PEQUEÑA)
            frmDetalle = new Form();
            frmDetalle.Text = "Detalle de Factura";
            frmDetalle.Size = new Size(600, 400); // REDUCIDO: era 800x600, ahora 600x400
            frmDetalle.StartPosition = FormStartPosition.Manual; // CAMBIO: Manual para control total
            frmDetalle.FormBorderStyle = FormBorderStyle.FixedDialog;
            frmDetalle.MaximizeBox = false;
            frmDetalle.MinimizeBox = false;
            frmDetalle.BackColor = Color.White;

            // Agregar un DataGridView para mostrar los detalles de la factura (IGUAL QUE LA GRILLA PRINCIPAL)
            var dgvDetalle = new DataGridView();
            dgvDetalle.Name = "dgvDetalle";
            dgvDetalle.Dock = DockStyle.Fill;
            dgvDetalle.AllowUserToAddRows = false;
            dgvDetalle.AllowUserToDeleteRows = false;
            dgvDetalle.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDetalle.BackgroundColor = Color.White;
            dgvDetalle.BorderStyle = BorderStyle.None;
            dgvDetalle.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvDetalle.ReadOnly = true;
            dgvDetalle.RowHeadersVisible = false;
            dgvDetalle.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDetalle.EnableHeadersVisualStyles = false;
            
            // MISMO ESTILO DE HEADER QUE LA GRILLA PRINCIPAL
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(248, 249, 250);
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = Color.Black;
            dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(248, 249, 250);
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dgvDetalle.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvDetalle.ColumnHeadersHeight = 35;

            frmDetalle.Controls.Add(dgvDetalle);

            // NUEVO: Panel para mostrar totales de la factura
            var panelTotales = new Panel();
            panelTotales.Dock = DockStyle.Bottom;
            panelTotales.Height = 40;
            panelTotales.BackColor = Color.FromArgb(0, 120, 215);
            frmDetalle.Controls.Add(panelTotales);

            // REORGANIZADO: Labels distribuidos correctamente
            var lblCantidadProductos = new Label();
            lblCantidadProductos.Name = "lblCantidadProductos";
            lblCantidadProductos.Text = "Productos: 0";
            lblCantidadProductos.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblCantidadProductos.ForeColor = Color.White;
            lblCantidadProductos.AutoSize = false;
            lblCantidadProductos.Dock = DockStyle.Left;
            lblCantidadProductos.Width = 120; // A LA IZQUIERDA
            lblCantidadProductos.TextAlign = ContentAlignment.MiddleLeft;
            lblCantidadProductos.Padding = new Padding(10, 0, 0, 0);
            panelTotales.Controls.Add(lblCantidadProductos);

            var lblTotalFactura = new Label();
            lblTotalFactura.Name = "lblTotalFactura";
            lblTotalFactura.Text = "Total: $0,00";
            lblTotalFactura.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTotalFactura.ForeColor = Color.White;
            lblTotalFactura.AutoSize = false;
            lblTotalFactura.Dock = DockStyle.Right;
            lblTotalFactura.Width = 200; // MÁS ANCHO: era 150, ahora 200
            lblTotalFactura.TextAlign = ContentAlignment.MiddleRight;
            lblTotalFactura.Padding = new Padding(0, 0, 10, 0);
            panelTotales.Controls.Add(lblTotalFactura);

            var lblCantidadTotalDetalle = new Label();
            lblCantidadTotalDetalle.Name = "lblCantidadTotalDetalle";
            lblCantidadTotalDetalle.Text = "Cantidad: 0";
            lblCantidadTotalDetalle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblCantidadTotalDetalle.ForeColor = Color.White;
            lblCantidadTotalDetalle.AutoSize = false;
            lblCantidadTotalDetalle.Dock = DockStyle.Fill; // CAMBIO: Fill para ocupar el centro
            lblCantidadTotalDetalle.TextAlign = ContentAlignment.MiddleCenter; // CENTRADO
            lblCantidadTotalDetalle.Padding = new Padding(0);
            panelTotales.Controls.Add(lblCantidadTotalDetalle);

            // Panel inferior para botones (SIMPLE Y LIMPIO)
            var panelBotones = new Panel();
            panelBotones.Dock = DockStyle.Bottom;
            panelBotones.Height = 50;
            panelBotones.BackColor = Color.FromArgb(248, 249, 250);
            frmDetalle.Controls.Add(panelBotones);

            // Botón para imprimir (MISMO ESTILO QUE LOS BOTONES PRINCIPALES)
            var btnImprimir = new Button();
            btnImprimir.Name = "btnImprimir";
            btnImprimir.Text = "Imprimir";
            btnImprimir.BackColor = Color.FromArgb(0, 120, 215);
            btnImprimir.ForeColor = Color.White;
            btnImprimir.FlatStyle = FlatStyle.Flat;
            btnImprimir.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnImprimir.Size = new Size(80, 30);
            btnImprimir.Location = new Point(panelBotones.Width - 180, 10);
            btnImprimir.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnImprimir.Click += BtnImprimir_Click;
            panelBotones.Controls.Add(btnImprimir);

            // Botón para cerrar (MISMO ESTILO QUE LOS BOTONES PRINCIPALES)
            var btnCerrar = new Button();
            btnCerrar.Text = "Cerrar";
            btnCerrar.BackColor = Color.FromArgb(0, 150, 136);
            btnCerrar.ForeColor = Color.White;
            btnCerrar.FlatStyle = FlatStyle.Flat;
            btnCerrar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCerrar.Size = new Size(80, 30);
            btnCerrar.Location = new Point(panelBotones.Width - 90, 10);
            btnCerrar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCerrar.Click += (s, e) => { frmDetalle.Hide(); };
            panelBotones.Controls.Add(btnCerrar);
            
            // PREVENT form disposal when user clicks X button
            frmDetalle.FormClosing += (s, e) => {
                e.Cancel = true;  // Cancel the close operation
                frmDetalle.Hide(); // Just hide instead
            };

            // Ajustar posición de botones cuando se redimensiona
            panelBotones.Resize += (s, e) => {
                btnCerrar.Location = new Point(panelBotones.Width - 90, 10);
                btnImprimir.Location = new Point(panelBotones.Width - 180, 10);
            };
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
                            NumeroRemito as 'Remito',
                            NroFactura as 'N° Factura',
                            Fecha as 'Fecha',
                            Hora as 'Hora',
                            ImporteTotal as 'Total Venta',
                            FormadePago as 'Forma de Pago',
                            TipoFactura as 'Tipo',
                            CAENumero as 'CAE',
                            CtaCteNombre as 'Cta. Cte. Nombre'
                        FROM Facturas 
                        WHERE CAST(Fecha AS DATE) = @fecha 
                        AND esCtaCte = @esCtaCte
                        ORDER BY NumeroRemito DESC"
                        : @"SELECT 
                            NumeroRemito as 'Remito',
                            NroFactura as 'N° Factura',
                            Fecha as 'Fecha',
                            Hora as 'Hora',
                            ImporteTotal as 'Total Venta',
                            FormadePago as 'Forma de Pago',
                            TipoFactura as 'Tipo',
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

        // Método para formatear columnas del DataGridView de detalle (IGUAL QUE LA GRILLA PRINCIPAL)
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
                    codigoCol.Width = 100;
                    codigoCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var productoCol = dgvDetalle.Columns["Producto"];
                if (productoCol != null)
                {
                    productoCol.Width = 300;
                }

                var cantidadCol = dgvDetalle.Columns["Cantidad"];
                if (cantidadCol != null)
                {
                    cantidadCol.Width = 100;
                    cantidadCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var precioCol = dgvDetalle.Columns["Precio Unit."];
                if (precioCol != null)
                {
                    precioCol.DefaultCellStyle.Format = "C2";
                    precioCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    precioCol.Width = 120;
                }

                var totalCol = dgvDetalle.Columns["Total"];
                if (totalCol != null)
                {
                    totalCol.DefaultCellStyle.Format = "C2";
                    totalCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    totalCol.Width = 120;
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
                    string tipoFactura = row["Tipo"]?.ToString() ?? "Sin especificar";
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
                    // NUEVO: Determinar el número correcto según el tipo de factura
                    string numeroParaTicket;
                    if (datosFactura.TipoFactura == "Remito")
                    {
                        numeroParaTicket = numeroRemito; // Usar número de remito
                    }
                    else
                    {
                        // Para facturas, intentar obtener el número formateado de la BD
                        // Si ya está formateado en la BD, usarlo directamente
                        numeroParaTicket = ObtenerNumeroFacturaFormateado(numeroRemito) ?? numeroRemito;
                    }

                    // Configurar el ticket con los datos reales
                    var config = new TicketConfig
                    {
                        NombreComercio = datosFactura.NombreComercio,
                        DomicilioComercio = datosFactura.DomicilioComercio,
                        NumeroComprobante = numeroParaTicket, // Usar el número correcto
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

        // NUEVO: Método para obtener número de factura formateado de la BD
        private string ObtenerNumeroFacturaFormateado(string numeroRemito)
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
                    var query = "SELECT NroFactura FROM Facturas WHERE NumeroRemito = @numeroRemito";
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@numeroRemito", numeroRemito);
                        connection.Open();
                        var resultado = cmd.ExecuteScalar();
                        return resultado?.ToString();
                    }
                }
            }
            catch
            {
                return null;
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
            if (frmDetalle != null && !frmDetalle.IsDisposed && frmDetalle.Visible)
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
                var numeroRemito = dgvVentas.Rows[e.RowIndex].Cells["Remito"].Value?.ToString();
                if (!string.IsNullOrEmpty(numeroRemito))
                {
                    CargarDetalleFactura(numeroRemito);
                    MostrarVentanaDetalle();
                }
            }
        }

        // Método para formatear columnas del DataGridView principal
        private void FormatearColumnas()
        {
            if (dgvVentas.Columns.Count == 0) return;

            // CAMBIO: Usar None temporalmente para configuración manual
            var originalAutoSizeMode = dgvVentas.AutoSizeColumnsMode;
            dgvVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            try
            {
                // COLUMNAS FIJAS (primeras 4 - no se redimensionan)
                var remitoCol = dgvVentas.Columns["Remito"];
                if (remitoCol != null)
                {
                    remitoCol.Width = 60;
                    remitoCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // FIJA
                    remitoCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; // CENTRADO
                }

                var facturaCol = dgvVentas.Columns["N° Factura"];
                if (facturaCol != null)
                {
                    facturaCol.Width = 100;
                    facturaCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // FIJA
                    facturaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var fechaCol = dgvVentas.Columns["Fecha"];
                if (fechaCol != null)
                {
                    fechaCol.DefaultCellStyle.Format = "dd/MM/yyyy";
                    fechaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    fechaCol.Width = 100;
                    fechaCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // FIJA
                }

                var horaCol = dgvVentas.Columns["Hora"];
                if (horaCol != null)
                {
                    horaCol.Width = 80;
                    horaCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // FIJA
                    horaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    horaCol.DefaultCellStyle.Format = "HH:mm"; // FORMATO SOLO HORA SIN FECHA
                }

                // COLUMNAS QUE SE REDIMENSIONAN (resto de columnas)
                var totalVentaCol = dgvVentas.Columns["Total Venta"];
                if (totalVentaCol != null)
                {
                    totalVentaCol.DefaultCellStyle.Format = "C2";
                    totalVentaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    totalVentaCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // SE REDIMENSIONA
                    totalVentaCol.FillWeight = 120; // Peso relativo
                }

                var formaPagoCol = dgvVentas.Columns["Forma de Pago"];
                if (formaPagoCol != null)
                {
                    formaPagoCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    formaPagoCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // SE REDIMENSIONA
                    formaPagoCol.FillWeight = 150; // Peso relativo más alto
                }

                var tipoFacturaCol = dgvVentas.Columns["Tipo"];
                if (tipoFacturaCol != null)
                {
                    tipoFacturaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    tipoFacturaCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // SE REDIMENSIONA
                    tipoFacturaCol.FillWeight = 100; // Peso relativo
                }

                var caeCol = dgvVentas.Columns["CAE"];
                if (caeCol != null)
                {
                    caeCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    caeCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // SE REDIMENSIONA
                    caeCol.FillWeight = 120; // Peso relativo
                }

                var cuitCol = dgvVentas.Columns["CUIT Cliente"];
                if (cuitCol != null)
                {
                    cuitCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    cuitCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // SE REDIMENSIONA
                    cuitCol.FillWeight = 120; // Peso relativo
                }

                // Columna dinámica para Cuenta Corriente
                var ctaCteCol = dgvVentas.Columns["Cta. Cte. Nombre"];
                if (ctaCteCol != null)
                {
                    ctaCteCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    ctaCteCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // SE REDIMENSIONA
                    ctaCteCol.FillWeight = 150; // Peso relativo más alto
                }
            }
            finally
            {
                // CAMBIO: Mantener el modo None para respetar las configuraciones individuales
                dgvVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
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
                            string numeroRemito = dataRowView["Remito"]?.ToString() ?? dataRowView["N° Factura"]?.ToString();

                            // Buscar la fila en el DataGridView que tenga ese número
                            foreach (DataGridViewRow dgvRow in dgvVentas.Rows)
                            {
                                string valorCelda = dgvRow.Cells["Remito"]?.Value?.ToString() ?? dgvRow.Cells["N° Factura"]?.Value?.ToString();
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
                    // CORREGIDO: Usar el nombre correcto de la columna
                    var query = @"
                        SELECT 
                            codigo as 'Código',
                            descripcion as 'Producto',
                            cantidad as 'Cantidad',
                            precio as 'Precio Unit.',
                            total as 'Total'
                        FROM Ventas 
                        WHERE NroFactura = @nroFactura
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
            // Check if form is disposed and recreate if necessary
            if (frmDetalle == null || frmDetalle.IsDisposed)
            {
                CrearVentanaDetalle();
            }
            
            if (frmDetalle != null && !frmDetalle.IsDisposed)
            {
                // CENTRAR RESPECTO AL FORMULARIO MDI PADRE
                Form mdiParent = this.MdiParent;
                
                if (mdiParent != null)
                {
                    Rectangle parentBounds;
            
            if (mdiParent.WindowState == FormWindowState.Maximized)
            {
                // Si el MDI parent está maximizado, usar el área de trabajo de la pantalla
                parentBounds = Screen.FromControl(mdiParent).WorkingArea;
            }
            else
            {
                // Si el MDI parent no está maximizado, usar sus bounds reales
                parentBounds = mdiParent.Bounds;
            }
            
            int x = parentBounds.X + (parentBounds.Width - frmDetalle.Width) / 2;
            int y = parentBounds.Y + (parentBounds.Height - frmDetalle.Height) / 2;
            
            // Asegurar que no se salga de la pantalla
            Rectangle screenBounds = Screen.FromControl(this).WorkingArea;
            if (x < screenBounds.X) x = screenBounds.X;
            if (y < screenBounds.Y) y = screenBounds.Y;
            if (x + frmDetalle.Width > screenBounds.Right) 
                x = screenBounds.Right - frmDetalle.Width;
            if (y + frmDetalle.Height > screenBounds.Bottom) 
                y = screenBounds.Bottom - frmDetalle.Height;
            
            frmDetalle.Location = new Point(x, y);
                }
                else
                {
                    // Si no hay MDI parent, centrar en pantalla
                    Rectangle workingArea = Screen.FromControl(this).WorkingArea;
                    int x = workingArea.X + (workingArea.Width - frmDetalle.Width) / 2;
                    int y = workingArea.Y + (workingArea.Height - frmDetalle.Height) / 2;
                    frmDetalle.Location = new Point(x, y);
                }
                
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
                    // MODIFICADO: Obtener también el número de remito y número de factura
                    var query = @"
                        SELECT 
                            NumeroRemito,
                            NroFactura,
                            TipoFactura,
                            FormadePago,
                            CAENumero,
                            CUITCliente
                        FROM Facturas 
                        WHERE NumeroRemito = @nroFactura"; // Usar NumeroRemito ya que nroFactura contiene el número de remito

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@nroFactura", nroFactura);
                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string numeroRemito = reader["NumeroRemito"]?.ToString() ?? "";
                                string numeroFactura = reader["NroFactura"]?.ToString() ?? "";
                                string tipoFactura = reader["TipoFactura"]?.ToString() ?? "";
                                string formaPago = reader["FormadePago"]?.ToString() ?? "";
                                string cae = reader["CAENumero"]?.ToString() ?? "";
                                string cuit = reader["CUITCliente"]?.ToString() ?? "";

                                // NUEVO: Construir título con número de remito y factura según corresponda
                                string titulo = "";
                        
                                if (tipoFactura == "FacturaA" || tipoFactura == "FacturaB")
                                {
                                    // Para facturas A/B: mostrar tanto remito como factura
                                    titulo = $"Detalle  - Remito N° {numeroRemito}";
                                    if (!string.IsNullOrEmpty(numeroFactura))
                                    {
                                        titulo += $" - Factura N° {numeroFactura}";
                                    }
                                }
                                else
                                {
                                    // Para remitos: solo mostrar número de remito
                                    titulo = $"Detalle {tipoFactura} N° {numeroRemito}";
                                }

                                // Agregar información adicional
                                if (!string.IsNullOrEmpty(cae))
                                    titulo += $" - CAE: {cae}";

                                if (!string.IsNullOrEmpty(cuit))
                                    titulo += $" - CUIT: {cuit}";

                                frmDetalle.Text = titulo;
                            }
                            else
                            {
                                frmDetalle.Text = $"Detalle de Comprobante N° {nroFactura}";
                            }
                        }
                    }
                }
            }
            catch
            {
                frmDetalle.Text = $"Detalle de Comprobante N° {nroFactura}";
            }
        }

        // Método para obtener datos de la factura para impresión
        private DatosFactura ObtenerDatosFactura(string numeroRemito)
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
                    // CORREGIDO: Obtener datos de la tabla Facturas usando NumeroRemito
                    var query = @"SELECT TipoFactura, FormadePago, CAENumero, CUITCliente 
                                 FROM Facturas WHERE NumeroRemito = @numeroRemito";
                    
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@numeroRemito", numeroRemito);
                        connection.Open();
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new DatosFactura
                                {
                                    TipoFactura = reader["TipoFactura"]?.ToString() ?? "",
                                    FormaPago = reader["FormadePago"]?.ToString() ?? "",
                                    CAENumero = reader["CAENumero"]?.ToString() ?? "",
                                    CAEVencimiento = null,
                                    CUITCliente = reader["CUITCliente"]?.ToString() ?? "",
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
                "FacturaA" => "FACTURA",
                "FacturaB" => "FACTURA",
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (frmDetalle != null && !frmDetalle.IsDisposed)
                {
                    frmDetalle.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
