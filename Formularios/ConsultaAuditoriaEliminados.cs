using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;

namespace Comercio.NET.Formularios
{
    public class ConsultaAuditoriaEliminados : Form
    {
        private DataGridView dgvAuditoria;
        private DateTimePicker dtpDesde, dtpHasta;
        private TextBox txtCodigoProducto, txtNumeroFactura, txtUsuario, txtCajero;
        private Button btnBuscar, btnExportar, btnSalir;
        private Label lblTotalRegistros;

        public ConsultaAuditoriaEliminados()
        {
            ConfigurarFormulario();
            ConfigurarEventosTextBoxes(); // NUEVO: Configurar eventos de navegación
            CrearVentanaDetalle();
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

        // ✅ NUEVO: Constructor sobrecargado que acepta rango de fechas
        public ConsultaAuditoriaEliminados(DateTime fechaDesde, DateTime fechaHasta) : this()
        {
            // Establecer las fechas recibidas como parámetros
            dtpDesde.Value = fechaDesde;
            dtpHasta.Value = fechaHasta;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            CargarDatosIniciales();
        }

        // NUEVO: Método para configurar eventos de navegación con Enter en TextBoxes
        private void ConfigurarEventosTextBoxes()
        {
            // Configurar cada TextBox para navegar con Enter
            ConfigurarTextBoxNavegacion(txtCodigoProducto);
            ConfigurarTextBoxNavegacion(txtNumeroFactura);
            ConfigurarTextBoxNavegacion(txtUsuario);
            ConfigurarTextBoxNavegacion(txtCajero);
        }

        // NUEVO: Método helper para configurar navegación con Enter en un TextBox específico
        private void ConfigurarTextBoxNavegacion(TextBox textBox)
        {
            textBox.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true; // Evitar el beep del sistema
                    
                    // Navegar al siguiente control en el orden de tabulación
                    this.SelectNextControl(textBox, true, true, true, true);
                }
            };

            // OPCIONAL: Seleccionar todo el texto cuando el TextBox recibe foco
            textBox.Enter += (sender, e) =>
            {
                textBox.SelectAll();
            };
        }

        private void ConfigurarFormulario()
        {
            this.Text = "Consulta de Productos Eliminados - Auditoría";
            this.Size = new Size(1200, 500);
            this.MinimumSize = new Size(1000, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            var panelFiltros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80, 
                BackColor = Color.FromArgb(240, 240, 240)
            };

            // PRIMERA FILA - Filtros de fecha, código, remito y usuario
            var lblDesde = new Label { Text = "Desde:", Location = new Point(10, 15), Size = new Size(50, 20) };
            dtpDesde = new DateTimePicker { Location = new Point(65, 12), Size = new Size(120, 23), TabIndex = 0 }; // NUEVO: TabIndex
            //dtpDesde.Value = DateTime.Now.AddDays(-30);
            dtpDesde.Value = DateTime.Now;

            var lblHasta = new Label { Text = "Hasta:", Location = new Point(200, 15), Size = new Size(50, 20) };
            dtpHasta = new DateTimePicker { Location = new Point(255, 12), Size = new Size(120, 23), TabIndex = 1 }; // NUEVO: TabIndex

            var lblCodigo = new Label { Text = "Código:", Location = new Point(390, 15), Size = new Size(50, 20) };
            txtCodigoProducto = new TextBox { Location = new Point(445, 12), Size = new Size(100, 23), TabIndex = 2 }; // NUEVO: TabIndex

            var lblFactura = new Label { Text = "Remito:", Location = new Point(560, 15), Size = new Size(50, 20) };
            txtNumeroFactura = new TextBox { Location = new Point(615, 12), Size = new Size(100, 23), TabIndex = 3 }; // NUEVO: TabIndex

            var lblUsuario = new Label { Text = "Usuario:", Location = new Point(730, 15), Size = new Size(50, 20) };
            txtUsuario = new TextBox { Location = new Point(785, 12), Size = new Size(100, 23), TabIndex = 4 }; // NUEVO: TabIndex

            // SEGUNDA FILA - Cajero y TODOS los botones juntos
            var lblCajero = new Label { Text = "Cajero:", Location = new Point(10, 45), Size = new Size(50, 20) };
            txtCajero = new TextBox { Location = new Point(65, 42), Size = new Size(80, 23), TabIndex = 5 }; // NUEVO: TabIndex

            // TODOS LOS BOTONES EN LA SEGUNDA FILA - Alineados horizontalmente
            btnBuscar = new Button
            {
                Text = "Buscar",
                Location = new Point(160, 40),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                TabIndex = 6 // NUEVO: TabIndex
            };
            btnBuscar.Click += BtnBuscar_Click;

            btnExportar = new Button
            {
                Text = "Exportar",
                Location = new Point(250, 40),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                TabIndex = 7 // NUEVO: TabIndex
            };
            btnExportar.Click += BtnExportar_Click;

            btnSalir = new Button
            {
                Text = "Salir",
                Location = new Point(340, 40), 
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                TabIndex = 8 // NUEVO: TabIndex
            };
            btnSalir.Click += BtnSalir_Click;

            lblTotalRegistros = new Label
            {
                Text = "Total: 0 registros",
                Location = new Point(450, 45),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            // Agregar todos los controles al panel
            panelFiltros.Controls.AddRange(new Control[]
            {
                lblDesde, dtpDesde, lblHasta, dtpHasta,
                lblCodigo, txtCodigoProducto, lblFactura, txtNumeroFactura,
                lblUsuario, txtUsuario, // Primera fila
                lblCajero, txtCajero, btnBuscar, btnExportar, btnSalir, lblTotalRegistros // TODOS en segunda fila
            });

            // MEJORADO: DataGridView con mejores estilos
            dgvAuditoria = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                AllowUserToResizeColumns = true,
                AllowUserToOrderColumns = true,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(230, 230, 230),
                BackgroundColor = Color.White,
                EnableHeadersVisualStyles = false,
                TabIndex = 9 // NUEVO: TabIndex para el DataGridView
            };

            // NUEVO: Estilos mejorados para el DataGridView
            ConfigurarEstilosDataGridView();

            this.Controls.Add(dgvAuditoria);
            this.Controls.Add(panelFiltros);

            // ✅ AGREGAR en el constructor o InitializeComponent:
            dgvAuditoria.CellDoubleClick += DgvAuditoria_CellDoubleClick;

            // NUEVO: Configurar el orden de tabulación y foco inicial
            this.TabStop = true;
            dtpDesde.Select(); // Establecer foco inicial en el primer control
        }

        // ✅ NUEVO: Event handler para doble clic en auditoría
        private void DgvAuditoria_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                // ✅ CAMBIO: Usar "Remito" en lugar de "NumeroFactura"
                var numeroFactura = dgvAuditoria.Rows[e.RowIndex].Cells["Remito"]?.Value?.ToString();

                if (!string.IsNullOrEmpty(numeroFactura))
                {
                    CargarDetalleFacturaEliminada(numeroFactura);
                    MostrarVentanaDetalle();
                }
            }
        }

        // ✅ NUEVO: Método para cargar detalle de factura eliminada
        private void CargarDetalleFacturaEliminada(string nroFactura)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DETALLE] Cargando productos del remito desde tabla Ventas: {nroFactura}");

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    // ✅ CAMBIO CRÍTICO: Consultar tabla Ventas en lugar de AuditoriaProductosEliminados
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

                        System.Diagnostics.Debug.WriteLine($"[DETALLE] ✅ Productos encontrados en Ventas: {dt.Rows.Count}");

                        if (dt.Rows.Count == 0)
                        {
                            // ✅ Si no hay productos en Ventas, buscar en auditoría
                            System.Diagnostics.Debug.WriteLine($"[DETALLE] ⚠️ No hay productos en Ventas, buscando en auditoría...");
                            CargarDetalleDesdeAuditoria(nroFactura);
                            return;
                        }

                        // Buscar el DataGridView en la ventana flotante
                        var dgvDetalle = frmDetalle.Controls.Find("dgvDetalle", true).FirstOrDefault() as DataGridView;
                        if (dgvDetalle != null)
                        {
                            dgvDetalle.DataSource = dt;
                            FormatearColumnasDetalleVentas(dgvDetalle); // ✅ Usar formato para Ventas (no auditoría)
                        }

                        // Actualizar totales
                        ActualizarTotalesDetalleVentas(dt, nroFactura);

                        // Actualizar el título de la ventana
                        ActualizarTituloDetalleVentas(nroFactura);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el detalle del remito: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"[ERROR DETALLE] {ex.Message}\n{ex.StackTrace}");
            }
        }

        // ✅ NUEVO: Cargar desde auditoría si el remito ya fue eliminado completamente
        private void CargarDetalleDesdeAuditoria(string nroFactura)
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
                    CodigoProducto as 'Código',
                    DescripcionProducto as 'Producto',
                    Cantidad as 'Cantidad',
                    PrecioUnitario as 'Precio Unit.',
                    TotalEliminado as 'Total',
                    MotivoEliminacion as 'Motivo',
                    FechaEliminacion as 'Fecha Eliminación',
                    UsuarioEliminacion as 'Usuario'
                FROM AuditoriaProductosEliminados 
                WHERE NumeroFactura = @nroFactura
                ORDER BY DescripcionProducto";

                    using (var adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@nroFactura", nroFactura);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        System.Diagnostics.Debug.WriteLine($"[DETALLE] ✅ Productos encontrados en Auditoría: {dt.Rows.Count}");

                        if (dt.Rows.Count == 0)
                        {
                            MessageBox.Show($"No se encontraron productos para el remito N° {nroFactura}.\n\n" +
                                            "El remito no existe ni en Ventas ni en Auditoría.",
                                "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        var dgvDetalle = frmDetalle.Controls.Find("dgvDetalle", true).FirstOrDefault() as DataGridView;
                        if (dgvDetalle != null)
                        {
                            dgvDetalle.DataSource = dt;
                            FormatearColumnasDetalle(dgvDetalle); // Formato con columnas de auditoría
                        }

                        ActualizarTotalesDetalle(dt, nroFactura);
                        ActualizarTituloDetalle(nroFactura, dt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos de auditoría: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ✅ NUEVO: Formatear columnas para datos de Ventas (sin columnas de auditoría)
        private void FormatearColumnasDetalleVentas(DataGridView dgvDetalle)
        {
            if (dgvDetalle.Columns.Count == 0) return;

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
                    precioCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    precioCol.Width = 120;
                }

                var totalCol = dgvDetalle.Columns["Total"];
                if (totalCol != null)
                {
                    totalCol.DefaultCellStyle.Format = "C2";
                    totalCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    totalCol.Width = 120;
                    totalCol.DefaultCellStyle.ForeColor = Color.FromArgb(40, 167, 69); // ✅ Verde (datos actuales)
                    totalCol.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
            }
            finally
            {
                dgvDetalle.AutoSizeColumnsMode = originalAutoSizeMode;
            }
        }

        // ✅ NUEVO: Actualizar totales para datos de Ventas
        private void ActualizarTotalesDetalleVentas(DataTable dt, string nroFactura)
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

            var lblCantidadProductos = frmDetalle.Controls.Find("lblCantidadProductos", true).FirstOrDefault() as Label;
            if (lblCantidadProductos != null)
                lblCantidadProductos.Text = $"Productos: {cantidadProductos}";

            var lblCantidadTotalDetalle = frmDetalle.Controls.Find("lblCantidadTotalDetalle", true).FirstOrDefault() as Label;
            if (lblCantidadTotalDetalle != null)
                lblCantidadTotalDetalle.Text = $"Cantidad: {cantidadTotal:N0}";

            var lblTotalFactura = frmDetalle.Controls.Find("lblTotalFactura", true).FirstOrDefault() as Label;
            if (lblTotalFactura != null)
            {
                lblTotalFactura.Text = $"Total: {totalFactura:C2}";
                lblTotalFactura.ForeColor = Color.White; // ✅ Blanco (datos actuales, no eliminados)
            }
        }

        // ✅ NUEVO: Actualizar título para datos de Ventas
        private void ActualizarTituloDetalleVentas(string nroFactura)
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
                    Fecha
                FROM Facturas 
                WHERE NumeroRemito = @nroFactura";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@nroFactura", nroFactura);
                        connection.Open();

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string tipo = reader["TipoFactura"]?.ToString() ?? "Remito";
                                string formaPago = reader["FormadePago"]?.ToString() ?? "";
                                DateTime? fecha = reader["Fecha"] as DateTime?;

                                string titulo = $"📋 Detalle {tipo} N° {nroFactura}";

                                if (fecha.HasValue)
                                {
                                    titulo += $" - Fecha: {fecha.Value:dd/MM/yyyy}";
                                }

                                if (!string.IsNullOrEmpty(formaPago))
                                {
                                    titulo += $" - {formaPago}";
                                }

                                frmDetalle.Text = titulo;
                                return;
                            }
                        }
                    }
                }

                // Si no se encontró en Facturas
                frmDetalle.Text = $"📋 Detalle Remito N° {nroFactura}";
            }
            catch
            {
                frmDetalle.Text = $"📋 Detalle Remito N° {nroFactura}";
            }
        }

        // ✅ NUEVO: Método para formatear columnas del detalle de auditoría
        private void FormatearColumnasDetalle(DataGridView dgvDetalle)
        {
            if (dgvDetalle.Columns.Count == 0) return;

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
                    totalCol.DefaultCellStyle.ForeColor = Color.FromArgb(220, 53, 69); // ✅ Rojo para indicar eliminación
                    totalCol.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }

                var motivoCol = dgvDetalle.Columns["Motivo"];
                if (motivoCol != null)
                {
                    motivoCol.Width = 200;
                    motivoCol.DefaultCellStyle.ForeColor = Color.FromArgb(255, 152, 0); // ✅ Naranja
                }

                var fechaCol = dgvDetalle.Columns["Fecha Eliminación"];
                if (fechaCol != null)
                {
                    fechaCol.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                    fechaCol.Width = 120;
                    fechaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var usuarioCol = dgvDetalle.Columns["Usuario"];
                if (usuarioCol != null)
                {
                    usuarioCol.Width = 100;
                    usuarioCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }
            finally
            {
                dgvDetalle.AutoSizeColumnsMode = originalAutoSizeMode;
            }
        }

        // ✅ NUEVO: Actualizar totales para factura eliminada
        private void ActualizarTotalesDetalle(DataTable dt, string nroFactura)
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
            {
                lblTotalFactura.Text = $"Total Eliminado: {totalFactura:C2}";
                lblTotalFactura.ForeColor = Color.FromArgb(220, 53, 69); // ✅ Rojo para indicar eliminación
            }
        }

        // ✅ NUEVO: Actualizar título para factura eliminada
        private void ActualizarTituloDetalle(string nroFactura, DataTable dt)
        {
            try
            {
                string usuario = "";
                DateTime? fechaEliminacion = null;

                if (dt.Rows.Count > 0)
                {
                    usuario = dt.Rows[0]["Usuario"]?.ToString() ?? "";
                    fechaEliminacion = dt.Rows[0]["Fecha Eliminación"] as DateTime?;
                }

                string titulo = $"🗑️ Factura Eliminada N° {nroFactura}";

                if (fechaEliminacion.HasValue)
                {
                    titulo += $" - Eliminada: {fechaEliminacion.Value:dd/MM/yyyy HH:mm}";
                }

                if (!string.IsNullOrEmpty(usuario))
                {
                    titulo += $" - Usuario: {usuario}";
                }

                frmDetalle.Text = titulo;
            }
            catch
            {
                frmDetalle.Text = $"🗑️ Factura Eliminada N° {nroFactura}";
            }
        }

        private Form frmDetalle;

        private void CrearVentanaDetalle()
        {
            // ✅ MISMO CÓDIGO QUE EN frmControlFacturas
            frmDetalle = new Form();
            frmDetalle.Text = "Detalle de Factura Eliminada";
            frmDetalle.Size = new Size(800, 500);
            frmDetalle.StartPosition = FormStartPosition.Manual;
            frmDetalle.FormBorderStyle = FormBorderStyle.FixedDialog;
            frmDetalle.MaximizeBox = false;
            frmDetalle.MinimizeBox = false;
            frmDetalle.BackColor = Color.White;

            // DataGridView para mostrar los detalles
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

            DataGridViewCellStyle headerStyle = new DataGridViewCellStyle();
            headerStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            headerStyle.BackColor = Color.FromArgb(220, 53, 69); // ✅ Rojo para auditoría
            headerStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            headerStyle.ForeColor = Color.White;
            headerStyle.SelectionBackColor = Color.FromArgb(220, 53, 69);
            headerStyle.SelectionForeColor = SystemColors.HighlightText;
            headerStyle.WrapMode = DataGridViewTriState.True;
            headerStyle.Padding = new Padding(3, 2, 3, 2);

            dgvDetalle.ColumnHeadersDefaultCellStyle = headerStyle;
            dgvDetalle.ColumnHeadersHeight = 55;
            dgvDetalle.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;

            frmDetalle.Controls.Add(dgvDetalle);

            // Panel para mostrar totales
            var panelTotales = new Panel();
            panelTotales.Dock = DockStyle.Bottom;
            panelTotales.Height = 70;
            panelTotales.BackColor = Color.FromArgb(220, 53, 69); // ✅ Rojo para auditoría
            frmDetalle.Controls.Add(panelTotales);

            var lblCantidadProductos = new Label();
            lblCantidadProductos.Name = "lblCantidadProductos";
            lblCantidadProductos.Text = "Productos: 0";
            lblCantidadProductos.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblCantidadProductos.ForeColor = Color.White;
            lblCantidadProductos.AutoSize = false;
            lblCantidadProductos.Dock = DockStyle.Left;
            lblCantidadProductos.Width = 140;
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
            lblTotalFactura.Width = 220;
            lblTotalFactura.TextAlign = ContentAlignment.MiddleRight;
            lblTotalFactura.Padding = new Padding(0, 0, 12, 0);
            panelTotales.Controls.Add(lblTotalFactura);

            var lblCantidadTotalDetalle = new Label();
            lblCantidadTotalDetalle.Name = "lblCantidadTotalDetalle";
            lblCantidadTotalDetalle.Text = "Cantidad: 0";
            lblCantidadTotalDetalle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblCantidadTotalDetalle.ForeColor = Color.White;
            lblCantidadTotalDetalle.AutoSize = false;
            lblCantidadTotalDetalle.Dock = DockStyle.Fill;
            lblCantidadTotalDetalle.TextAlign = ContentAlignment.MiddleCenter;
            panelTotales.Controls.Add(lblCantidadTotalDetalle);

            // Panel inferior para botones
            var panelBotones = new Panel();
            panelBotones.Dock = DockStyle.Bottom;
            panelBotones.Height = 50;
            panelBotones.BackColor = Color.FromArgb(248, 249, 250);
            frmDetalle.Controls.Add(panelBotones);

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

            frmDetalle.FormClosing += (s, e) => {
                e.Cancel = true;
                frmDetalle.Hide();
            };

            panelBotones.Resize += (s, e) => {
                btnCerrar.Location = new Point(panelBotones.Width - 90, 10);
            };
        }

        private void MostrarVentanaDetalle()
        {
            if (frmDetalle == null || frmDetalle.IsDisposed)
            {
                CrearVentanaDetalle();
            }

            if (frmDetalle != null && !frmDetalle.IsDisposed)
            {
                Form mdiParent = this.MdiParent;

                if (mdiParent != null)
                {
                    Rectangle parentBounds = mdiParent.WindowState == FormWindowState.Maximized
                        ? Screen.FromControl(mdiParent).WorkingArea
                        : mdiParent.Bounds;

                    int x = parentBounds.X + (parentBounds.Width - frmDetalle.Width) / 2;
                    int y = parentBounds.Y + (parentBounds.Height - frmDetalle.Height) / 2;

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
                    Rectangle workingArea = Screen.FromControl(this).WorkingArea;
                    int x = workingArea.X + (workingArea.Width - frmDetalle.Width) / 2;
                    int y = workingArea.Y + (workingArea.Height - frmDetalle.Height) / 2;
                    frmDetalle.Location = new Point(x, y);
                }

                frmDetalle.Show();
                frmDetalle.BringToFront();
            }
        }

        private void BtnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ConfigurarEstilosDataGridView()
        {
            // Estilos de encabezados
            dgvAuditoria.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 120, 215);
            dgvAuditoria.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvAuditoria.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvAuditoria.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvAuditoria.ColumnHeadersHeight = 30;

            // Estilos de celdas
            dgvAuditoria.DefaultCellStyle.BackColor = Color.White;
            dgvAuditoria.DefaultCellStyle.ForeColor = Color.Black;
            dgvAuditoria.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dgvAuditoria.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvAuditoria.DefaultCellStyle.Font = new Font("Segoe UI", 9F);

            // Filas alternadas
            dgvAuditoria.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            dgvAuditoria.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dgvAuditoria.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

            dgvAuditoria.RowTemplate.Height = 25;
        }

        private async void CargarDatosIniciales()
        {
            await BuscarRegistros();
        }

        private async void BtnBuscar_Click(object sender, EventArgs e)
        {
            await BuscarRegistros();    
        }

        private async Task BuscarRegistros()
        {
            try
            {
                string connectionString = GetConnectionString();
                string query = @"
                    SELECT 
                        IdAuditoriaProductosEliminados,
                        CodigoProducto AS 'Código',
                        DescripcionProducto AS 'Descripción Producto',
                        PrecioUnitario AS 'Precio Unitario',
                        Cantidad AS 'Cant.',
                        TotalEliminado AS 'Total Eliminado',
                        NumeroFactura AS 'Remito',
                        FechaHoraVentaOriginal AS 'Fecha Factura',
                        FechaEliminacion AS 'Fecha Eliminación',
                        MotivoEliminacion AS 'Motivo de Eliminación',
                        CASE WHEN EsCtaCte = 1 THEN 'Sí' ELSE 'No' END AS 'CtaCte',
                        UsuarioEliminacion AS 'Usuario',
                        NumeroCajero AS 'Cajero',
                        NombreEquipo AS 'Equipo'
                        -- REMOVIDO: IPUsuario AS 'IP' (columna eliminada)
                    FROM AuditoriaProductosEliminados 
                    WHERE FechaEliminacion >= @fechaDesde 
                      AND FechaEliminacion <= @fechaHasta";

                var parametros = new List<SqlParameter>
                {
                    new SqlParameter("@fechaDesde", dtpDesde.Value.Date),
                    new SqlParameter("@fechaHasta", dtpHasta.Value.Date.AddDays(1).AddSeconds(-1))
                };

                if (!string.IsNullOrWhiteSpace(txtCodigoProducto.Text))
                {
                    query += " AND CodigoProducto LIKE @codigo";
                    parametros.Add(new SqlParameter("@codigo", $"%{txtCodigoProducto.Text.Trim()}%"));
                }

                if (!string.IsNullOrWhiteSpace(txtNumeroFactura.Text))
                {
                    query += " AND NumeroFactura = @numeroFactura";
                    parametros.Add(new SqlParameter("@numeroFactura", txtNumeroFactura.Text.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(txtUsuario.Text))
                {
                    query += " AND UsuarioEliminacion LIKE @usuario";
                    parametros.Add(new SqlParameter("@usuario", $"%{txtUsuario.Text.Trim()}%"));
                }

                // NUEVO: Filtro por número de cajero
                if (!string.IsNullOrWhiteSpace(txtCajero.Text))
                {
                    if (int.TryParse(txtCajero.Text.Trim(), out int numeroCajero))
                    {
                        query += " AND NumeroCajero = @numeroCajero";
                        parametros.Add(new SqlParameter("@numeroCajero", numeroCajero));
                    }
                    else
                    {
                        MessageBox.Show("El número de cajero debe ser un valor numérico.", "Validación", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                query += " ORDER BY FechaEliminacion DESC";

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.SelectCommand.Parameters.AddRange(parametros.ToArray());
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        
                        dgvAuditoria.DataSource = dt;
                        lblTotalRegistros.Text = $"Total: {dt.Rows.Count} registros encontrados";
                        
                        FormatearDataGridView();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatearDataGridView()
        {
            if (dgvAuditoria.Columns.Count == 0) return;

            // Ocultar columna Id
            if (dgvAuditoria.Columns["IdAuditoriaProductosEliminados"] != null)
                dgvAuditoria.Columns["IdAuditoriaProductosEliminados"].Visible = false;

            if (dgvAuditoria.Columns["Precio Unitario"] != null)
                dgvAuditoria.Columns["Precio Unitario"].Visible = false;


            ConfigurarColumna("Código", 100, DataGridViewContentAlignment.MiddleCenter);
            ConfigurarColumna("Descripción Producto", 180, DataGridViewContentAlignment.MiddleLeft);
            ConfigurarColumna("Cant.", 40, DataGridViewContentAlignment.MiddleCenter);
            ConfigurarColumna("Total Eliminado", 100, DataGridViewContentAlignment.MiddleRight, "C2");
            ConfigurarColumna("Remito", 70, DataGridViewContentAlignment.MiddleCenter);
            ConfigurarColumna("Fecha Factura", 110, DataGridViewContentAlignment.MiddleCenter, "dd/MM/yyyy HH:mm");
            ConfigurarColumna("Fecha Eliminación", 115, DataGridViewContentAlignment.MiddleCenter, "dd/MM/yyyy HH:mm");
            ConfigurarColumna("Motivo de Eliminación", 130, DataGridViewContentAlignment.MiddleLeft);
            ConfigurarColumna("CtaCte", 45, DataGridViewContentAlignment.MiddleCenter);
            ConfigurarColumna("Usuario", 70, DataGridViewContentAlignment.MiddleCenter);
            ConfigurarColumna("Cajero", 50, DataGridViewContentAlignment.MiddleCenter);
            ConfigurarColumna("Equipo", 80, DataGridViewContentAlignment.MiddleCenter);

            // Ajustar ancho mínimo de "Motivo de Eliminación" para la expansión automática
            if (dgvAuditoria.Columns["Motivo de Eliminación"] != null)
            {
                dgvAuditoria.Columns["Motivo de Eliminación"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvAuditoria.Columns["Motivo de Eliminación"].MinimumWidth = 200;
            }
        }

        private void ConfigurarColumna(string nombreColumna, int ancho, 
            DataGridViewContentAlignment alineacion, string formato = null)
        {
            if (dgvAuditoria.Columns[nombreColumna] == null) return;

            var columna = dgvAuditoria.Columns[nombreColumna];
            
            try
            {
                columna.Width = ancho;
                columna.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                columna.DefaultCellStyle.Alignment = alineacion;
                columna.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                columna.Resizable = DataGridViewTriState.True;

                if (!string.IsNullOrEmpty(formato))
                {
                    columna.DefaultCellStyle.Format = formato;
                }

                // Colores especiales para ciertos tipos de columnas
                if (formato == "C2") // Columnas monetarias
                {
                    columna.DefaultCellStyle.ForeColor = Color.FromArgb(0, 100, 0); // Verde oscuro
                    columna.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
                else if (nombreColumna.Contains("Fecha")) // Columnas de fecha
                {
                    columna.DefaultCellStyle.ForeColor = Color.FromArgb(0, 0, 150); // Azul oscuro
                }
                else if (nombreColumna == "Motivo de Eliminación") // Columna de motivo
                {
                    columna.DefaultCellStyle.ForeColor = Color.FromArgb(150, 0, 0); // Rojo oscuro
                    columna.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
                }
                else if (nombreColumna == "Cajero") // NUEVO: Estilo especial para columna Cajero
                {
                    columna.DefaultCellStyle.ForeColor = Color.FromArgb(0, 120, 215); // Azul
                    columna.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
            }
            catch (Exception)
            {
                this.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        columna.Width = ancho;
                        columna.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        columna.DefaultCellStyle.Alignment = alineacion;
                        if (!string.IsNullOrEmpty(formato))
                            columna.DefaultCellStyle.Format = formato;
                    }
                    catch { }
                }));
            }
        }

        private void BtnExportar_Click(object sender, EventArgs e)
        {
            if (dgvAuditoria.DataSource == null)
            {
                MessageBox.Show("No hay datos para exportar.", "Información", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Archivos CSV|*.csv|Archivos Excel|*.xlsx";
                saveDialog.FileName = $"AuditoriaEliminados_{DateTime.Now:yyyyMMdd_HHmmss}";
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ExportarACSV(saveDialog.FileName);
                        MessageBox.Show("Datos exportados correctamente.", "Éxito", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al exportar: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportarACSV(string rutaArchivo)
        {
            var dt = (DataTable)dgvAuditoria.DataSource;
            var lines = new List<string>();

            var encabezados = dt.Columns.Cast<DataColumn>()
                .Where(col => col.ColumnName != "IdAuditoriaProductosEliminados")
                .Select(col => col.ColumnName);
            lines.Add(string.Join(",", encabezados));

            foreach (DataRow row in dt.Rows)
            {
                var valores = row.ItemArray
                    .Skip(1) // Saltar columna IdAuditoriaProductosEliminados
                    .Select(field => $"\"{field?.ToString()}\"");
                lines.Add(string.Join(",", valores));
            }

            System.IO.File.WriteAllLines(rutaArchivo, lines, System.Text.Encoding.UTF8);
        }

        private string GetConnectionString()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return config.GetConnectionString("DefaultConnection");
        }
    }
}