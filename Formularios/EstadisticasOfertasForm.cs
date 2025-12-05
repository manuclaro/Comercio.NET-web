using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;

namespace Comercio.NET.Formularios
{
    public class EstadisticasOfertasForm : Form
    {
        private DataGridView dgvEstadisticas;
        private DateTimePicker dtpDesde;
        private DateTimePicker dtpHasta;
        private Button btnBuscar;
        private Label lblTotalProductosOferta;
        private Label lblTotalDescuento;
        private Label lblTotalVentasOferta;

        public EstadisticasOfertasForm()
        {
            InicializarComponentes();
            ConfigurarControles();
            ConfigurarEventos();
            
            this.Load += (s, e) => CargarEstadisticas();
        }

        private void InicializarComponentes()
        {
            this.Text = "Estadísticas de Ventas con Ofertas";
            this.Size = new Size(900, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = Color.White;
            this.MinimumSize = new Size(700, 400);
        }

        private void ConfigurarControles()
        {
            // Panel Totales (Bottom) - Agregar PRIMERO
            var panelTotales = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(15, 10, 15, 10)
            };

            lblTotalProductosOferta = new Label
            {
                Text = "Total Productos en Oferta: 0",
                Location = new Point(15, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 150, 136)
            };

            lblTotalDescuento = new Label
            {
                Text = "Total Descuento Aplicado: $0,00",
                Location = new Point(15, 32),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 53, 69)
            };

            lblTotalVentasOferta = new Label
            {
                Text = "Total Ventas con Oferta: $0,00",
                Location = new Point(15, 56),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            panelTotales.Controls.AddRange(new Control[] {
        lblTotalProductosOferta, lblTotalDescuento, lblTotalVentasOferta
    });

            this.Controls.Add(panelTotales);

            // DataGridView (Fill) - Agregar SEGUNDO
            dgvEstadisticas = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 30 },
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand,
                Margin = new Padding(10)
            };

            dgvEstadisticas.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(230, 240, 250),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                SelectionBackColor = Color.FromArgb(230, 240, 250),
                WrapMode = DataGridViewTriState.True,
                Padding = new Padding(5)
            };
            dgvEstadisticas.ColumnHeadersHeight = 40;

            this.Controls.Add(dgvEstadisticas);

            // Panel Filtros (Top) - Agregar TERCERO
            var panelFiltros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = Color.FromArgb(248, 249, 250)
            };

            var lblDesde = new Label
            {
                Text = "Desde:",
                Location = new Point(15, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            dtpDesde = new DateTimePicker
            {
                Location = new Point(65, 12),
                Width = 120,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-30),
                Font = new Font("Segoe UI", 9F)
            };

            var lblHasta = new Label
            {
                Text = "Hasta:",
                Location = new Point(200, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            dtpHasta = new DateTimePicker
            {
                Location = new Point(245, 12),
                Width = 120,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Font = new Font("Segoe UI", 9F)
            };

            btnBuscar = new Button
            {
                Text = "🔍 Buscar",
                Location = new Point(380, 10),
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;

            panelFiltros.Controls.AddRange(new Control[] {
        lblDesde, dtpDesde, lblHasta, dtpHasta, btnBuscar
    });

            this.Controls.Add(panelFiltros);

            // Panel Título (Top) - Agregar AL FINAL
            var panelTitulo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.FromArgb(0, 150, 136),
                Padding = new Padding(15, 5, 15, 5)
            };

            var lblTitulo = new Label
            {
                Text = "🎁 Estadísticas de Ofertas",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            panelTitulo.Controls.Add(lblTitulo);
            this.Controls.Add(panelTitulo);
        }

        private void ConfigurarEventos()
        {
            btnBuscar.Click += (s, e) => CargarEstadisticas();
            dgvEstadisticas.CellDoubleClick += DgvEstadisticas_CellDoubleClick;
        }

        private void DgvEstadisticas_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                try
                {
                    var nombreOferta = dgvEstadisticas.Rows[e.RowIndex].Cells["Oferta"].Value?.ToString();
                    
                    if (!string.IsNullOrEmpty(nombreOferta))
                    {
                        MostrarDetalleOferta(nombreOferta);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al mostrar detalle: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void MostrarDetalleOferta(string nombreOferta)
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
                    connection.Open();

                    var query = @"
                        SELECT 
                            p.codigo as 'Código',
                            p.descripcion as 'Producto',
                            SUM(v.cantidad) as 'Cantidad Vendida',
                            v.precio as 'Precio Unit.',
                            CAST(SUM(ISNULL(v.DescuentoAplicado, 0) * v.cantidad) AS DECIMAL(18,2)) as 'Descuento Total',
                            CAST(SUM(ISNULL(v.total, 0)) AS DECIMAL(18,2)) as 'Total Vendido',
                            COUNT(DISTINCT v.NroFactura) as 'Nro Ventas'
                        FROM Ventas v
                        INNER JOIN Facturas f ON v.NroFactura = f.NumeroRemito
                        INNER JOIN Productos p ON v.codigo = p.codigo
                        WHERE v.EsOferta = 1
                        AND v.NombreOferta = @nombreOferta
                        AND CAST(f.Fecha AS DATE) BETWEEN @desde AND @hasta
                        GROUP BY p.codigo, p.descripcion, v.precio
                        ORDER BY SUM(v.cantidad) DESC";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@nombreOferta", nombreOferta);
                        cmd.Parameters.AddWithValue("@desde", dtpDesde.Value.Date);
                        cmd.Parameters.AddWithValue("@hasta", dtpHasta.Value.Date);

                        var adapter = new SqlDataAdapter(cmd);
                        var dt = new DataTable();
                        adapter.Fill(dt);

                        MostrarFormularioDetalle(nombreOferta, dt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar detalle de oferta: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MostrarFormularioDetalle(string nombreOferta, DataTable datos)
        {
            var formDetalle = new Form
            {
                Text = $"Detalle de Oferta: {nombreOferta}",
                Size = new Size(850, 500),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                BackColor = Color.White,
                MinimumSize = new Size(700, 400)
            };

            // ═══════════════════════════════════════════════════════════
            // 1️⃣ Panel inferior (BOTTOM) - Agregar PRIMERO
            // ═══════════════════════════════════════════════════════════
            var panelInferior = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(15, 10, 15, 10)
            };

            // Calcular totales
            int totalProductos = datos.Rows.Count;
            int totalCantidad = 0;
            decimal totalDescuento = 0;
            decimal totalVendido = 0;
            int totalVentas = 0;

            foreach (DataRow row in datos.Rows)
            {
                totalCantidad += Convert.ToInt32(row["Cantidad Vendida"]);
                totalDescuento += Convert.ToDecimal(row["Descuento Total"]);
                totalVendido += Convert.ToDecimal(row["Total Vendido"]);
                totalVentas += Convert.ToInt32(row["Nro Ventas"]);
            }

            var lblResumen1 = new Label
            {
                Text = $"Productos Distintos: {totalProductos}  |  Cantidad Total: {totalCantidad:N0}  |  Nro de Ventas: {totalVentas}",
                Location = new Point(15, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 150, 136)
            };

            var lblResumen2 = new Label
            {
                Text = $"Descuento Total: {totalDescuento:C2}",
                Location = new Point(15, 35),
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 53, 69)
            };

            var lblResumen3 = new Label
            {
                Text = $"Total Vendido: {totalVendido:C2}",
                Location = new Point(15, 60),
                AutoSize = true,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            var btnCerrar = new Button
            {
                Text = "Cerrar",
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Location = new Point(panelInferior.Width - 110, 45);
            btnCerrar.Click += (s, e) => formDetalle.Close();

            panelInferior.Controls.AddRange(new Control[] { lblResumen1, lblResumen2, lblResumen3, btnCerrar });
            formDetalle.Controls.Add(panelInferior);

            // ═══════════════════════════════════════════════════════════
            // 2️⃣ DataGridView (FILL) - Agregar SEGUNDO
            // ═══════════════════════════════════════════════════════════
            var dgvDetalle = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 28 },
                Font = new Font("Segoe UI", 9F),
                DataSource = datos,
                Margin = new Padding(10)
            };

            dgvDetalle.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(230, 240, 250),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                SelectionBackColor = Color.FromArgb(230, 240, 250),
                WrapMode = DataGridViewTriState.True,
                Padding = new Padding(5)
            };
            //dgvDetalle.ColumnHeadersHeight = 40;
            //dgvDetalle.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvDetalle.ColumnHeadersHeight = 55; // ✅ AUMENTADO de 50 a 55 píxeles
            formDetalle.Controls.Add(dgvDetalle);
            FormatearColumnasDetalle(dgvDetalle);

            // ═══════════════════════════════════════════════════════════
            // 3️⃣ Panel título (TOP) - Agregar AL FINAL
            // ═══════════════════════════════════════════════════════════
            var panelTitulo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(0, 150, 136),
                Padding = new Padding(15, 10, 15, 10)
            };

            var lblTitulo = new Label
            {
                Text = $"📦 Detalle: {nombreOferta}",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            panelTitulo.Controls.Add(lblTitulo);
            formDetalle.Controls.Add(panelTitulo);

            formDetalle.ShowDialog(this);
        }

        private void FormatearColumnasDetalle(DataGridView dgv)
        {
            if (dgv.Columns.Count == 0) return;

            if (dgv.Columns["Código"] != null)
            {
                dgv.Columns["Código"].FillWeight = 60;
                dgv.Columns["Código"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (dgv.Columns["Producto"] != null)
            {
                dgv.Columns["Producto"].FillWeight = 150;
                dgv.Columns["Producto"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }

            if (dgv.Columns["Cantidad Vendida"] != null)
            {
                dgv.Columns["Cantidad Vendida"].FillWeight = 70;
                dgv.Columns["Cantidad Vendida"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv.Columns["Cantidad Vendida"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }

            if (dgv.Columns["Precio Unit."] != null)
            {
                dgv.Columns["Precio Unit."].FillWeight = 70;
                dgv.Columns["Precio Unit."].DefaultCellStyle.Format = "C2";
                dgv.Columns["Precio Unit."].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            if (dgv.Columns["Descuento Total"] != null)
            {
                dgv.Columns["Descuento Total"].FillWeight = 80;
                dgv.Columns["Descuento Total"].DefaultCellStyle.Format = "C2";
                dgv.Columns["Descuento Total"].DefaultCellStyle.ForeColor = Color.FromArgb(220, 53, 69);
                dgv.Columns["Descuento Total"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            if (dgv.Columns["Total Vendido"] != null)
            {
                dgv.Columns["Total Vendido"].FillWeight = 90;
                dgv.Columns["Total Vendido"].DefaultCellStyle.Format = "C2";
                dgv.Columns["Total Vendido"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                dgv.Columns["Total Vendido"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            if (dgv.Columns["Nro Ventas"] != null)
            {
                dgv.Columns["Nro Ventas"].FillWeight = 60;
                dgv.Columns["Nro Ventas"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void CargarEstadisticas()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📊 Iniciando carga de estadísticas...");
                
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception("No se pudo obtener la cadena de conexión desde appsettings.json");
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    System.Diagnostics.Debug.WriteLine("✅ Conexión establecida");

                    var query = @"
                        SELECT 
                            ISNULL(v.NombreOferta, 'Sin nombre') as 'Oferta',
                            COUNT(DISTINCT v.codigo) as 'Productos',
                            CAST(SUM(v.cantidad) AS INT) as 'Cantidad',
                            CAST(SUM(ISNULL(v.DescuentoAplicado, 0) * v.cantidad) AS DECIMAL(18,2)) as 'Descuento',
                            CAST(SUM(ISNULL(v.total, 0)) AS DECIMAL(18,2)) as 'Total Vendido'
                        FROM Ventas v
                        INNER JOIN Facturas f ON v.NroFactura = f.NumeroRemito
                        WHERE v.EsOferta = 1
                        AND CAST(f.Fecha AS DATE) BETWEEN @desde AND @hasta
                        GROUP BY v.NombreOferta
                        ORDER BY SUM(ISNULL(v.total, 0)) DESC";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@desde", dtpDesde.Value.Date);
                        cmd.Parameters.AddWithValue("@hasta", dtpHasta.Value.Date);

                        var adapter = new SqlDataAdapter(cmd);
                        var dt = new DataTable();
                        adapter.Fill(dt);

                        System.Diagnostics.Debug.WriteLine($"📦 Registros obtenidos: {dt.Rows.Count}");

                        if (dt.Rows.Count == 0)
                        {
                            dgvEstadisticas.DataSource = null;
                            dgvEstadisticas.Columns.Clear();
                            
                            MessageBox.Show(
                                $"No se encontraron ventas con ofertas entre {dtpDesde.Value:dd/MM/yyyy} y {dtpHasta.Value:dd/MM/yyyy}",
                                "Sin datos",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        else
                        {
                            dgvEstadisticas.DataSource = dt;
                            FormatearColumnas();
                        }

                        ActualizarTotales(connection);
                        
                        System.Diagnostics.Debug.WriteLine("✅ Estadísticas cargadas correctamente");
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error SQL: {sqlEx.Message}");
                MessageBox.Show(
                    $"Error de base de datos:\n{sqlEx.Message}\n\nVerifique que las tablas Ventas y Facturas existan y tengan las columnas necesarias (EsOferta, NombreOferta, DescuentoAplicado).",
                    "Error de Base de Datos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error general: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show(
                    $"Error al cargar estadísticas:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void FormatearColumnas()
        {
            if (dgvEstadisticas.Columns.Count == 0) return;

            if (dgvEstadisticas.Columns["Oferta"] != null)
            {
                dgvEstadisticas.Columns["Oferta"].FillWeight = 150;
                dgvEstadisticas.Columns["Oferta"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }

            if (dgvEstadisticas.Columns["Productos"] != null)
            {
                dgvEstadisticas.Columns["Productos"].FillWeight = 60;
                dgvEstadisticas.Columns["Productos"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (dgvEstadisticas.Columns["Cantidad"] != null)
            {
                dgvEstadisticas.Columns["Cantidad"].FillWeight = 60;
                dgvEstadisticas.Columns["Cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (dgvEstadisticas.Columns["Descuento"] != null)
            {
                dgvEstadisticas.Columns["Descuento"].DefaultCellStyle.Format = "C2";
                dgvEstadisticas.Columns["Descuento"].DefaultCellStyle.ForeColor = Color.FromArgb(220, 53, 69);
                dgvEstadisticas.Columns["Descuento"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvEstadisticas.Columns["Descuento"].FillWeight = 80;
            }

            if (dgvEstadisticas.Columns["Total Vendido"] != null)
            {
                dgvEstadisticas.Columns["Total Vendido"].DefaultCellStyle.Format = "C2";
                dgvEstadisticas.Columns["Total Vendido"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                dgvEstadisticas.Columns["Total Vendido"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvEstadisticas.Columns["Total Vendido"].FillWeight = 100;
            }
        }

        private void ActualizarTotales(SqlConnection connection)
        {
            try
            {
                var queryTotales = @"
                    SELECT 
                        CAST(SUM(ISNULL(v.cantidad, 0)) AS INT) as TotalProductos,
                        CAST(SUM(ISNULL(v.DescuentoAplicado, 0) * ISNULL(v.cantidad, 0)) AS DECIMAL(18,2)) as TotalDescuento,
                        CAST(SUM(ISNULL(v.total, 0)) AS DECIMAL(18,2)) as TotalVentas
                    FROM Ventas v
                    INNER JOIN Facturas f ON v.NroFactura = f.NumeroRemito
                    WHERE v.EsOferta = 1
                    AND CAST(f.Fecha AS DATE) BETWEEN @desde AND @hasta";

                using (var cmd = new SqlCommand(queryTotales, connection))
                {
                    cmd.Parameters.AddWithValue("@desde", dtpDesde.Value.Date);
                    cmd.Parameters.AddWithValue("@hasta", dtpHasta.Value.Date);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int totalProductos = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0));
                            decimal totalDescuento = reader.IsDBNull(1) ? 0 : Convert.ToDecimal(reader.GetValue(1));
                            decimal totalVentas = reader.IsDBNull(2) ? 0 : Convert.ToDecimal(reader.GetValue(2));

                            lblTotalProductosOferta.Text = $"Total Productos en Oferta: {totalProductos:N0}";
                            lblTotalDescuento.Text = $"Total Descuento Aplicado: {totalDescuento:C2}";
                            lblTotalVentasOferta.Text = $"Total Ventas con Oferta: {totalVentas:C2}";
                            
                            System.Diagnostics.Debug.WriteLine($"💰 Totales - Productos: {totalProductos}, Descuento: {totalDescuento:C2}, Ventas: {totalVentas:C2}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando totales: {ex.Message}");
                lblTotalProductosOferta.Text = "Total Productos en Oferta: Error";
                lblTotalDescuento.Text = "Total Descuento Aplicado: Error";
                lblTotalVentasOferta.Text = "Total Ventas con Oferta: Error";
            }
        }
    }
}