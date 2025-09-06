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
            lblDetalleFormasPago.TabIndex = 1;
            lblDetalleFormasPago.TextAlign = ContentAlignment.MiddleRight;
            // 
            // frmControlFacturas
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(991, 581);
            Controls.Add(dgvVentas);
            Controls.Add(panelResumen);
            Controls.Add(panelFiltros);
            Controls.Add(lblTitulo);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(780, 500);
            Name = "frmControlFacturas";
            Text = "Control de Facturas";
            WindowState = FormWindowState.Maximized;
            Click += FrmControlFacturas_Click;
            ((System.ComponentModel.ISupportInitialize)dgvVentas).EndInit();
            panelFiltros.ResumeLayout(false);
            panelFiltros.PerformLayout();
            panelResumen.ResumeLayout(false);
            panelTotales.ResumeLayout(false);
            ResumeLayout(false);
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
               
                    // CORREGIR: Si se encuentra una coincidencia exacta, seleccionar esa fila
                    if (dt.DefaultView.Count == 1 && dgvVentas.Rows.Count > 0)
                    {
                        try
                        {
                            // Buscar la fila que corresponde al registro filtrado
                            var dataRowView = dt.DefaultView[0];
                            string numeroFactura = dataRowView["N° Factura"].ToString();
                            
                            // Buscar la fila en el DataGridView que tenga ese número de factura
                            foreach (DataGridViewRow dgvRow in dgvVentas.Rows)
                            {
                                if (dgvRow.Cells["N° Factura"].Value?.ToString() == numeroFactura)
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
                            NumeroFactura as 'N° Factura',
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
                        ORDER BY NumeroFactura DESC"
                        : @"SELECT 
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
                        AND esCtaCte = @esCtaCte
                        ORDER BY NumeroFactura DESC";

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
                MessageBox.Show($"Error al cargar las ventas: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            
            // MODIFICAR: Configurar colores alternados más contrastantes para el detalle
            dgvDetalle.DefaultCellStyle.BackColor = Color.White;
            dgvDetalle.DefaultCellStyle.ForeColor = Color.Black;
            dgvDetalle.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(215, 225, 235); // Azul grisáceo más marcado
            dgvDetalle.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;
            
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
            
            // MODIFICAR: Configurar colores alternados más oscuros y contrastantes
            dgvVentas.DefaultCellStyle.BackColor = Color.White;
            dgvVentas.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(220, 230, 240); // Azul claro más visible
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
                    totalVentaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    totalVentaCol.Width = 120;
                }

                var fechaCol = dgvVentas.Columns["Fecha"];
                if (fechaCol != null)
                {
                    fechaCol.DefaultCellStyle.Format = "dd/MM/yyyy";
                    fechaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    fechaCol.Width = 60;
                }

                var horaCol = dgvVentas.Columns["Hora"];
                if (horaCol != null)
                {
                    horaCol.Width = 60;
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

                // NUEVO: Formatear columna dinámica según el checkbox
                if (chkCtaCte.Checked)
                {
                    // Cuando está tildado, mostrar Cta. Cte. Nombre
                    var ctaCteCol = dgvVentas.Columns["Cta. Cte. Nombre"];
                    if (ctaCteCol != null)
                    {
                        ctaCteCol.Width = 150;
                        ctaCteCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    }
                }
                else
                {
                    // Cuando NO está tildado, mostrar CUIT Cliente
                    var cuitCol = dgvVentas.Columns["CUIT Cliente"];
                    if (cuitCol != null)
                    {
                        cuitCol.Width = 120;
                        cuitCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
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
            
            // Totales por forma de pago
            decimal totalEfectivo = 0;
            decimal totalTransferencia = 0;
            decimal totalTarjeta = 0;
            
            // Totales por tipo de factura
            decimal totalRemito = 0;
            decimal totalFacturaA = 0;
            decimal totalFacturaB = 0;

            foreach (DataRow row in dt.Rows)
            {
                if (decimal.TryParse(row["Total Venta"].ToString(), out decimal total))
                {
                    totalVentas += total;
                    
                    // Agrupar por forma de pago - USAR TRIM()
                    string formaPago = (row["Forma de Pago"]?.ToString() ?? "").Trim();
                    switch (formaPago)
                    {
                        case "Efectivo":
                            totalEfectivo += total;
                            break;
                        case "Transferencia":
                            totalTransferencia += total;
                            break;
                        case "TarjetaCredito":
                            totalTarjeta += total;
                            break;
                    }
                    
                    // Agrupar por tipo de factura - USAR TRIM()
                    string tipoFactura = (row["Tipo Factura"]?.ToString() ?? "").Trim();
                    switch (tipoFactura)
                    {
                        case "Remito":
                            totalRemito += total;
                            break;
                        case "FacturaA":
                            totalFacturaA += total;
                            break;
                        case "FacturaB":
                            totalFacturaB += total;
                            break;
                    }
                }
            }

            lblCantidadVentas.Text = $"Facturas: {cantidadFacturas}";
            lblTotal.Text = $"Total: {totalVentas:C2}";

            // Buscar en el panel correcto
            var panelTotales = this.panelResumen.Controls.OfType<Panel>().FirstOrDefault();
            if (panelTotales != null)
            {
                // Actualizar el detalle de formas de pago
                var lblDetalleFormasPago = panelTotales.Controls.Find("lblDetalleFormasPago", false).FirstOrDefault() as Label;
                if (lblDetalleFormasPago != null)
                {
                    string detalle = "";
                    if (totalEfectivo > 0) detalle += $"Efectivo: {totalEfectivo:C2}  ";
                    if (totalTransferencia > 0) detalle += $"Transferencia: {totalTransferencia:C2}  ";
                    if (totalTarjeta > 0) detalle += $"Tarjeta: {totalTarjeta:C2}";
                    
                    lblDetalleFormasPago.Text = string.IsNullOrEmpty(detalle) ? "" : detalle.Trim();
                }

                // Actualizar el detalle de tipos de factura
                var lblDetalleTiposFactura = panelTotales.Controls.Find("lblDetalleTiposFactura", false).FirstOrDefault() as Label;
                if (lblDetalleTiposFactura != null)
                {
                    string detalleTipos = "";
                    if (totalRemito > 0) detalleTipos += $"Remito: {totalRemito:C2}  ";
                    if (totalFacturaA > 0) detalleTipos += $"Factura A: {totalFacturaA:C2}  ";
                    if (totalFacturaB > 0) detalleTipos += $"Factura B: {totalFacturaB:C2}";
                    
                    lblDetalleTiposFactura.Text = string.IsNullOrEmpty(detalleTipos) ? "" : detalleTipos.Trim();
                }
            }
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
