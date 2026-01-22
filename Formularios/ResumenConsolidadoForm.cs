using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;

namespace Comercio.NET.Formularios
{
    public class ResumenConsolidadoForm : Form
    {
        private readonly DateTime _fechaDesde;
        private readonly DateTime _fechaHasta;
        private readonly bool _soloCtaCte;
        
        private Panel panelIVA;
        private Panel panelRubros;
        private Panel panelTotales;
        private Button btnImprimir;
        private Button btnExportar;
        private Button btnCerrar;

        private decimal _totalGeneral;
        private decimal _totalIVA;
        private decimal _subtotalSinIVA;

        private DataTable _dtIVA;
        private DataTable _dtRubros;

        public ResumenConsolidadoForm(DateTime desde, DateTime hasta, bool soloCtaCte)
        {
            _fechaDesde = desde;
            _fechaHasta = hasta;
            _soloCtaCte = soloCtaCte;
            
            InitializeComponent();
            _ = CargarDatosAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "Reporte Consolidado de Ventas";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = Color.White;

            // Panel superior: Título y periodo
            var panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(0, 120, 215),
                Padding = new Padding(20)
            };

            var lblTitulo = new Label
            {
                Text = "REPORTE CONSOLIDADO DE VENTAS",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 15)
            };

            var lblPeriodo = new Label
            {
                Text = $"Período: {_fechaDesde:dd/MM/yyyy} - {_fechaHasta:dd/MM/yyyy}" +
                       (_soloCtaCte ? " (Solo Cuenta Corriente)" : ""),
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 45)
            };

            panelHeader.Controls.Add(lblTitulo);
            panelHeader.Controls.Add(lblPeriodo);

            // Panel de contenido scrolleable
            var panelContenido = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };

            // ✅ Panel de IVA
            panelIVA = new Panel
            {
                Location = new Point(20, 20),
                Width = 940,
                Height = 190, // ✅ CAMBIO: de 180 a 190 (agregar 10px)
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 249, 250)
            };

            var lblTituloIVA = new Label
            {
                Text = "📊 RESUMEN DE IVA POR ALÍCUOTA",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(10, 10),
                AutoSize = true
            };
            panelIVA.Controls.Add(lblTituloIVA);

            // Panel Rubros - ajustar posición Y
            panelRubros = new Panel
            {
                Location = new Point(20, 220), // ✅ CAMBIO: de 210 a 220
                Width = 940,
                Height = 200,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 249, 250)
            };

            var lblTituloRubros = new Label
            {
                Text = "🏪 RESUMEN POR RUBRO",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(10, 10),
                AutoSize = true
            };
            panelRubros.Controls.Add(lblTituloRubros);

            // Panel Totales - ajustar posición Y
            panelTotales = new Panel
            {
                Location = new Point(20, 430), // ✅ CAMBIO: de 420 a 430
                Width = 940,
                Height = 80,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(40, 167, 69)
            };

            panelContenido.Controls.Add(panelIVA);
            panelContenido.Controls.Add(panelRubros);
            panelContenido.Controls.Add(panelTotales);

            // Panel de botones
            var panelBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(20, 10, 20, 10)
            };

            btnImprimir = new Button
            {
                Text = "🖨️ Imprimir",
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(20, 10)
            };
            btnImprimir.Click += BtnImprimir_Click;

            btnExportar = new Button
            {
                Text = "📄 Exportar HTML",
                Size = new Size(140, 40),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(150, 10)
            };
            btnExportar.Click += BtnExportar_Click;

            btnCerrar = new Button
            {
                Text = "Cerrar",
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnCerrar.Location = new Point(panelBotones.Width - 120, 10);
            btnCerrar.Click += (s, e) => this.Close();

            panelBotones.Controls.Add(btnImprimir);
            panelBotones.Controls.Add(btnExportar);
            panelBotones.Controls.Add(btnCerrar);

            this.Controls.Add(panelContenido);
            this.Controls.Add(panelBotones);
            this.Controls.Add(panelHeader);
        }

        private async Task CargarDatosAsync()
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                // ✅ Cargar datos en paralelo sin bloquear el UI thread
                var taskIVA = CargarResumenIVA();
                var taskRubros = CargarResumenRubros();

                await Task.WhenAll(taskIVA, taskRubros);

                // ✅ AGREGADO: Validar antes de actualizar totales
                if (panelTotales != null)
                {
                    ActualizarPanelTotales();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                System.Diagnostics.Debug.WriteLine($"❌ Error completo:\n{ex}");
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private async Task CargarResumenIVA()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            string connectionString = config.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                // ✅ NUEVA QUERY: Agrupar por alícuota de IVA
                var query = @"
            WITH FacturasConIVA AS (
                SELECT 
                    f.ImporteFinal,
                    f.IVA,
                    CASE 
                        WHEN f.IVA > 0 AND f.ImporteFinal > 0 
                        THEN CAST(ROUND((f.IVA / (f.ImporteFinal - f.IVA)) * 100, 2) AS DECIMAL(5,2))
                        ELSE 0
                    END AS Alicuota
                FROM Facturas f
                WHERE f.Fecha >= @desde AND f.Fecha <= @hasta";

                if (_soloCtaCte)
                    query += " AND f.TipoFactura = 'CtaCte'";

                query += @"
            )
            SELECT 
                CASE 
                    WHEN Alicuota = 0 THEN 'Sin IVA (0%)'
                    WHEN Alicuota BETWEEN 10 AND 11 THEN 'IVA (10.5%)'
                    WHEN Alicuota BETWEEN 20 AND 22 THEN 'IVA (21%)'
                    WHEN Alicuota BETWEEN 26 AND 28 THEN 'IVA (27%)'
                    ELSE 'Otros (' + CAST(Alicuota AS VARCHAR) + '%)'
                END AS TipoIVA,
                COUNT(*) as Cantidad,
                SUM(CAST(ISNULL(ImporteFinal, 0) AS DECIMAL(18,2))) as Total,
                SUM(CAST(ISNULL(IVA, 0) AS DECIMAL(18,2))) as TotalIVA,
                SUM(CAST(ISNULL(ImporteFinal, 0) - ISNULL(IVA, 0) AS DECIMAL(18,2))) as Subtotal
            FROM FacturasConIVA
            GROUP BY 
                CASE 
                    WHEN Alicuota = 0 THEN 'Sin IVA (0%)'
                    WHEN Alicuota BETWEEN 10 AND 11 THEN 'IVA (10.5%)'
                    WHEN Alicuota BETWEEN 20 AND 22 THEN 'IVA (21%)'
                    WHEN Alicuota BETWEEN 26 AND 28 THEN 'IVA (27%)'
                    ELSE 'Otros (' + CAST(Alicuota AS VARCHAR) + '%)'
                END
            ORDER BY TotalIVA DESC";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@desde", _fechaDesde);
                    cmd.Parameters.AddWithValue("@hasta", _fechaHasta);

                    await connection.OpenAsync();

                    var dgvIVA = new DataGridView
                    {
                        Name = "dgvIVA",
                        Location = new Point(10, 45),
                        Size = new Size(920, 120), // ✅ REDUCIDO
                        ReadOnly = true,
                        AllowUserToAddRows = false,
                        RowHeadersVisible = false,
                        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                        BackgroundColor = Color.White,
                        BorderStyle = BorderStyle.None,
                        EnableHeadersVisualStyles = false,
                        AllowUserToResizeRows = false
                    };

                    var dtIVA = new DataTable();
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dtIVA);
                    }

                    _dtIVA = dtIVA; // ✅ GUARDAR para usar en impresión

                    dgvIVA.DataSource = dtIVA;
                    FormatearGridIVA(dgvIVA);
                    panelIVA.Controls.Add(dgvIVA);

                    if (dtIVA.Rows.Count > 0)
                    {
                        _totalIVA = dtIVA.AsEnumerable().Sum(r => r.Field<decimal>("TotalIVA"));
                        _subtotalSinIVA = dtIVA.AsEnumerable().Sum(r => r.Field<decimal>("Subtotal"));
                        _totalGeneral = dtIVA.AsEnumerable().Sum(r => r.Field<decimal>("Total"));
                    }
                    else
                    {
                        _totalIVA = 0;
                        _subtotalSinIVA = 0;
                        _totalGeneral = 0;
                    }
                }
            }
        }

        private async Task CargarResumenRubros()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            string connectionString = config.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"
                    WITH VentasConTotal AS (
                        SELECT 
                            v.NroFactura,
                            CASE 
                                WHEN UPPER(ISNULL(p.rubro, '')) LIKE '%CARNI%' THEN 'CARNICERIA'
                                WHEN UPPER(ISNULL(p.rubro, '')) LIKE '%VERDULE%' THEN 'VERDULERIA'
                                WHEN UPPER(ISNULL(p.rubro, '')) LIKE '%CIGARR%' OR UPPER(ISNULL(p.rubro, '')) LIKE '%TABAQU%' THEN 'CIGARRILLOS'
                                ELSE 'ALMACEN'
                            END AS Rubro,
                            CAST(v.total AS DECIMAL(18,2)) AS TotalProducto,
                            SUM(CAST(v.total AS DECIMAL(18,2))) OVER (PARTITION BY v.NroFactura) AS TotalFacturaVentas,
                            CAST(f.ImporteFinal AS DECIMAL(18,2)) AS ImporteFinalFactura
                        FROM Ventas v
                        INNER JOIN Productos p ON v.codigo = p.codigo
                        INNER JOIN Facturas f ON v.NroFactura = f.NumeroRemito
                        WHERE f.Fecha >= @desde AND f.Fecha <= @hasta";

                if (_soloCtaCte)
                    query += " AND f.TipoFactura = 'CtaCte'";

                query += @"
                    )
                    SELECT 
                        Rubro,
                        COUNT(DISTINCT NroFactura) AS CantidadFacturas,
                        COUNT(*) AS CantidadProductos,
                        CAST(SUM(
                            CASE 
                                WHEN TotalFacturaVentas > 0 
                                THEN (TotalProducto / TotalFacturaVentas) * ImporteFinalFactura
                                ELSE 0
                            END
                        ) AS DECIMAL(18,2)) AS MontoTotal
                    FROM VentasConTotal
                    GROUP BY Rubro
                    ORDER BY MontoTotal DESC";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@desde", _fechaDesde);
                    cmd.Parameters.AddWithValue("@hasta", _fechaHasta);

                    await connection.OpenAsync();

                    var dgvRubros = new DataGridView
                    {
                        Name = "dgvRubros",
                        Location = new Point(10, 45),
                        Size = new Size(920, 140), // era 245 - CAMBIAR ESTA LÍNEA
                        ReadOnly = true,
                        AllowUserToAddRows = false,
                        RowHeadersVisible = false,
                        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                        BackgroundColor = Color.White,
                        BorderStyle = BorderStyle.None,
                        EnableHeadersVisualStyles = false,
                        AllowUserToResizeRows = false // ✅ AGREGAR esta línea
                    };

                    var dtRubros = new DataTable();
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dtRubros);
                    }

                    _dtRubros = dtRubros; // ✅ GUARDAR para usar en impresión

                    dgvRubros.DataSource = dtRubros;
                    FormatearGridRubros(dgvRubros);

                    panelRubros.Controls.Add(dgvRubros);
                }
            }
        }

        private void ImprimirPagina(object sender, PrintPageEventArgs e)
        {
            try
            {
                var g = e.Graphics;
                float y = 50;
                float x = 50;
                float pageWidth = e.PageBounds.Width - 100;

                // Fuentes
                var fuenteTitulo = new Font("Arial", 16, FontStyle.Bold);
                var fuenteSubtitulo = new Font("Arial", 12, FontStyle.Bold);
                var fuenteNormal = new Font("Arial", 9);
                var fuenteHeader = new Font("Arial", 9, FontStyle.Bold);
                var fuenteTotales = new Font("Arial", 11, FontStyle.Bold);

                // ===== ENCABEZADO =====
                g.DrawString("REPORTE CONSOLIDADO DE VENTAS", fuenteTitulo, Brushes.Black, x, y);
                y += 35;

                string periodo = $"Período: {_fechaDesde:dd/MM/yyyy} - {_fechaHasta:dd/MM/yyyy}";
                if (_soloCtaCte)
                    periodo += " (Solo Cuenta Corriente)";
                g.DrawString(periodo, fuenteNormal, Brushes.Black, x, y);
                y += 30;

                // ===== SECCIÓN IVA =====
                g.DrawString("RESUMEN DE IVA POR ALÍCUOTA", fuenteSubtitulo, Brushes.DarkBlue, x, y);
                y += 25;

                if (_dtIVA != null && _dtIVA.Rows.Count > 0)
                {
                    // Encabezados de tabla IVA
                    float col1 = x;
                    float col2 = x + 180;
                    float col3 = x + 260;
                    float col4 = x + 380;
                    float col5 = x + 500;

                    g.FillRectangle(new SolidBrush(Color.FromArgb(0, 120, 215)), col1, y, pageWidth, 20);
                    g.DrawString("Condición IVA", fuenteHeader, Brushes.White, col1 + 5, y + 3);
                    g.DrawString("Facturas", fuenteHeader, Brushes.White, col2 + 5, y + 3);
                    g.DrawString("Total", fuenteHeader, Brushes.White, col3 + 5, y + 3);
                    g.DrawString("IVA", fuenteHeader, Brushes.White, col4 + 5, y + 3);
                    g.DrawString("Subtotal", fuenteHeader, Brushes.White, col5 + 5, y + 3);
                    y += 22;

                    // Filas de datos IVA
                    foreach (DataRow row in _dtIVA.Rows)
                    {
                        string tipoIVA = row["TipoIVA"]?.ToString() ?? "";
                        int cantidad = Convert.ToInt32(row["Cantidad"]);
                        decimal total = Convert.ToDecimal(row["Total"]);
                        decimal totalIVA = Convert.ToDecimal(row["TotalIVA"]);
                        decimal subtotal = Convert.ToDecimal(row["Subtotal"]);

                        g.DrawString(tipoIVA, fuenteNormal, Brushes.Black, col1 + 5, y);
                        g.DrawString(cantidad.ToString(), fuenteNormal, Brushes.Black, col2 + 20, y);
                        g.DrawString(total.ToString("C2"), fuenteNormal, Brushes.DarkGreen, col3 + 5, y);
                        g.DrawString(totalIVA.ToString("C2"), fuenteNormal, Brushes.DarkRed, col4 + 5, y);
                        g.DrawString(subtotal.ToString("C2"), fuenteNormal, Brushes.Black, col5 + 5, y);
                        y += 18;
                    }
                }
                else
                {
                    g.DrawString("Sin datos de IVA", fuenteNormal, Brushes.Gray, x + 10, y);
                    y += 20;
                }

                y += 20;

                // ===== SECCIÓN RUBROS =====
                g.DrawString("RESUMEN POR RUBRO", fuenteSubtitulo, Brushes.DarkBlue, x, y);
                y += 25;

                if (_dtRubros != null && _dtRubros.Rows.Count > 0)
                {
                    // Encabezados de tabla Rubros
                    float colR1 = x;
                    float colR2 = x + 150;
                    float colR3 = x + 280;
                    float colR4 = x + 410;

                    g.FillRectangle(new SolidBrush(Color.FromArgb(0, 120, 215)), colR1, y, pageWidth, 20);
                    g.DrawString("Rubro", fuenteHeader, Brushes.White, colR1 + 5, y + 3);
                    g.DrawString("Facturas", fuenteHeader, Brushes.White, colR2 + 5, y + 3);
                    g.DrawString("Productos", fuenteHeader, Brushes.White, colR3 + 5, y + 3);
                    g.DrawString("Monto Total", fuenteHeader, Brushes.White, colR4 + 5, y + 3);
                    y += 22;

                    // Filas de datos Rubros
                    foreach (DataRow row in _dtRubros.Rows)
                    {
                        string rubro = row["Rubro"]?.ToString() ?? "";
                        int cantFacturas = Convert.ToInt32(row["CantidadFacturas"]);
                        int cantProductos = Convert.ToInt32(row["CantidadProductos"]);
                        decimal montoTotal = Convert.ToDecimal(row["MontoTotal"]);

                        g.DrawString(rubro, fuenteNormal, Brushes.Black, colR1 + 5, y);
                        g.DrawString(cantFacturas.ToString(), fuenteNormal, Brushes.Black, colR2 + 20, y);
                        g.DrawString(cantProductos.ToString(), fuenteNormal, Brushes.Black, colR3 + 20, y);
                        g.DrawString(montoTotal.ToString("C2"), fuenteNormal, Brushes.DarkGreen, colR4 + 5, y);
                        y += 18;
                    }
                }
                else
                {
                    g.DrawString("Sin datos de rubros", fuenteNormal, Brushes.Gray, x + 10, y);
                    y += 20;
                }

                y += 30;

                // ===== TOTALES FINALES =====
                var rectTotales = new RectangleF(x, y, pageWidth, 60);
                g.FillRectangle(new SolidBrush(Color.FromArgb(40, 167, 69)), rectTotales);

                string textoTotales = $"TOTAL GENERAL: {_totalGeneral:C2}  |  IVA: {_totalIVA:C2}  |  Subtotal: {_subtotalSinIVA:C2}";
                var sizeTotales = g.MeasureString(textoTotales, fuenteTotales);
                float xCentrado = x + (pageWidth - sizeTotales.Width) / 2;
                g.DrawString(textoTotales, fuenteTotales, Brushes.White, xCentrado, y + 20);

                // Pie de página
                var fuentePie = new Font("Arial", 8, FontStyle.Italic);
                string textoPie = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}";
                g.DrawString(textoPie, fuentePie, Brushes.Gray, x, e.PageBounds.Height - 50);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en ImprimirPagina: {ex.Message}");
                MessageBox.Show($"Error al generar la impresión: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatearGridIVA(DataGridView dgv)
        {
            if (dgv == null || dgv.Columns.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ FormatearGridIVA: Grid vacío o sin columnas");
                return;
            }

            try
            {
                // ✅ MODIFICADO: Nueva columna "TipoIVA"
                if (dgv.Columns["TipoIVA"] != null)
                {
                    dgv.Columns["TipoIVA"].HeaderText = "Condición de IVA";
                    dgv.Columns["TipoIVA"].Width = 180;
                    dgv.Columns["TipoIVA"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }

                if (dgv.Columns["Cantidad"] != null)
                {
                    dgv.Columns["Cantidad"].HeaderText = "Facturas";
                    dgv.Columns["Cantidad"].Width = 80;
                    dgv.Columns["Cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                if (dgv.Columns["Total"] != null)
                {
                    dgv.Columns["Total"].HeaderText = "Total";
                    dgv.Columns["Total"].DefaultCellStyle.Format = "C2";
                    dgv.Columns["Total"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dgv.Columns["Total"].DefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                    dgv.Columns["Total"].DefaultCellStyle.ForeColor = Color.FromArgb(40, 167, 69);
                }

                if (dgv.Columns["TotalIVA"] != null)
                {
                    dgv.Columns["TotalIVA"].HeaderText = "IVA";
                    dgv.Columns["TotalIVA"].DefaultCellStyle.Format = "C2";
                    dgv.Columns["TotalIVA"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dgv.Columns["TotalIVA"].DefaultCellStyle.ForeColor = Color.FromArgb(220, 53, 69);
                    dgv.Columns["TotalIVA"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }

                if (dgv.Columns["Subtotal"] != null)
                {
                    dgv.Columns["Subtotal"].HeaderText = "Subtotal (sin IVA)";
                    dgv.Columns["Subtotal"].DefaultCellStyle.Format = "C2";
                    dgv.Columns["Subtotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }

                dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 120, 215);
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv.ColumnHeadersHeight = 35;
                dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
                dgv.RowTemplate.Height = 28; // ✅ Filas más compactas
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en FormatearGridIVA: {ex.Message}");
            }
        }

        private void FormatearGridRubros(DataGridView dgv)
        {
            if (dgv == null || dgv.Columns.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ FormatearGridRubros: Grid vacío o sin columnas");
                return;
            }

            try
            {
                if (dgv.Columns["Rubro"] != null)
                {
                    dgv.Columns["Rubro"].HeaderText = "Rubro";
                    dgv.Columns["Rubro"].Width = 150;
                }

                if (dgv.Columns["CantidadFacturas"] != null)
                {
                    dgv.Columns["CantidadFacturas"].HeaderText = "Facturas";
                    dgv.Columns["CantidadFacturas"].Width = 100;
                    dgv.Columns["CantidadFacturas"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                if (dgv.Columns["CantidadProductos"] != null)
                {
                    dgv.Columns["CantidadProductos"].HeaderText = "Productos";
                    dgv.Columns["CantidadProductos"].Width = 100;
                    dgv.Columns["CantidadProductos"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                if (dgv.Columns["MontoTotal"] != null)
                {
                    dgv.Columns["MontoTotal"].HeaderText = "Monto Total";
                    dgv.Columns["MontoTotal"].DefaultCellStyle.Format = "C2";
                    dgv.Columns["MontoTotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dgv.Columns["MontoTotal"].DefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                    dgv.Columns["MontoTotal"].DefaultCellStyle.ForeColor = Color.FromArgb(40, 167, 69);
                }

                dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 120, 215);
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv.ColumnHeadersHeight = 35;
                dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
                dgv.RowTemplate.Height = 28; // ✅ AGREGAR esta línea
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en FormatearGridRubros: {ex.Message}");
            }
        }

        private void ActualizarPanelTotales()
        {
            var lblTotales = new Label
            {
                Text = $"💰 TOTAL GENERAL: {_totalGeneral:C2}  |  IVA: {_totalIVA:C2}  |  Subtotal: {_subtotalSinIVA:C2}",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 25)
            };

            panelTotales.Controls.Add(lblTotales);
        }

        private void BtnImprimir_Click(object sender, EventArgs e)
        {
            try
            {
                var pd = new PrintDocument();
                pd.PrintPage += ImprimirPagina;
                
                var ppd = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = 1000,
                    Height = 800
                };
                ppd.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //private void ImprimirPagina(object sender, PrintPageEventArgs e)
        //{
        //    // Implementar lógica de impresión
        //    var g = e.Graphics;
        //    var fuente = new Font("Arial", 10);
        //    var fuenteTitulo = new Font("Arial", 14, FontStyle.Bold);
            
        //    float y = 50;
            
        //    g.DrawString("REPORTE CONSOLIDADO DE VENTAS", fuenteTitulo, Brushes.Black, 50, y);
        //    y += 30;
        //    g.DrawString($"Período: {_fechaDesde:dd/MM/yyyy} - {_fechaHasta:dd/MM/yyyy}", fuente, Brushes.Black, 50, y);
        //    y += 40;
            
        //    // Agregar contenido de IVA y Rubros...
        //    g.DrawString($"Total General: {_totalGeneral:C2}", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, 50, e.PageBounds.Height - 100);
        //}

        private void BtnExportar_Click(object sender, EventArgs e)
        {
            // Reutilizar la lógica de exportación HTML existente
            try
            {
                var html = GenerarHTMLConsolidado();
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Archivos HTML (*.html)|*.html",
                    FileName = $"ReporteConsolidado_{DateTime.Now:yyyyMMdd_HHmmss}.html"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllText(saveDialog.FileName, html, Encoding.UTF8);
                    MessageBox.Show("Reporte exportado exitosamente", "Éxito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GenerarHTMLConsolidado()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
            sb.AppendLine("<title>Reporte Consolidado</title>");
            sb.AppendLine("<style>body{font-family:Arial;margin:20px;}table{width:100%;border-collapse:collapse;margin:20px 0;}th,td{border:1px solid #ddd;padding:8px;text-align:left;}th{background-color:#0078d7;color:white;}</style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine($"<h1>REPORTE CONSOLIDADO DE VENTAS</h1>");
            sb.AppendLine($"<p>Período: {_fechaDesde:dd/MM/yyyy} - {_fechaHasta:dd/MM/yyyy}</p>");
            sb.AppendLine("<h2>Resumen de IVA</h2>");
            // Agregar tablas...
            sb.AppendLine($"<h2>Total General: {_totalGeneral:C2}</h2>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }
    }
}