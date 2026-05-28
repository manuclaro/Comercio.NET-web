using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    /// <summary>
    /// Módulo de estadísticas: Compras vs Ventas por Rubro.
    /// Muestra para cada rubro (identificado por el campo Rubro del proveedor)
    /// el total comprado, el total vendido y el porcentaje de ganancia/pérdida.
    /// </summary>
    public class EstadisticasRubrosForm : Form
    {
        // --- controles ---
        private DateTimePicker dtpDesde;
        private DateTimePicker dtpHasta;
        private ComboBox cboFiltroRubro;
        private Button btnConsultar;
        private Button btnExportar;
        private DataGridView dgvRubros;
        private Label lblResumen;
        private Panel pnlGrafico;
        private Panel pnlHeader;

        // --- datos ---
        private List<RubroStats> _datos = new List<RubroStats>();

        public EstadisticasRubrosForm()
        {
            InitializeLayout();
        }

        // ----------------------------------------------------------------
        // LAYOUT
        // ----------------------------------------------------------------
        private void InitializeLayout()
        {
            this.Text = "Estadísticas: Compras vs Ventas por Rubro";
            this.ClientSize = new Size(1000, 680);
            this.MinimumSize = new Size(800, 560);
            this.StartPosition = FormStartPosition.WindowsDefaultBounds;
            this.Font = new Font("Segoe UI", 9.5F);
            this.BackColor = Color.FromArgb(245, 248, 250);

            // HEADER
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(63, 81, 181)
            };
            var lblTitle = new Label { Text = "Estadísticas Compras vs Ventas por Rubro", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.White, Left = 16, Top = 10, AutoSize = true };
            var lblSub   = new Label { Text = "Comparativa de rentabilidad por rubro de proveedores", Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(220, 220, 255), Left = 16, Top = lblTitle.Bottom - 2, AutoSize = true };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // FILTROS
            var pnlFiltros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White,
                Padding = new Padding(10, 8, 10, 4)
            };
            var lblDesde = new Label { Text = "Desde:", Left = 10, Top = 14, AutoSize = true };
            dtpDesde = new DateTimePicker { Left = 65, Top = 10, Width = 130, Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
            var lblHasta = new Label { Text = "Hasta:", Left = 210, Top = 14, AutoSize = true };
            dtpHasta = new DateTimePicker { Left = 258, Top = 10, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            var lblRubro = new Label { Text = "Rubro:", Left = 400, Top = 14, AutoSize = true };
            cboFiltroRubro = new ComboBox
            {
                Left = 450, Top = 10, Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cboFiltroRubro.Items.Add("(Todos los rubros)");
            cboFiltroRubro.SelectedIndex = 0;

            btnConsultar = new Button
            {
                Text = "Consultar",
                Left = 622, Top = 8, Width = 110, Height = 32,
                BackColor = Color.FromArgb(63, 81, 181),
                FlatStyle = FlatStyle.Flat, ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnConsultar.FlatAppearance.BorderSize = 0;
            btnConsultar.Click += async (s, e) => await ConsultarAsync();

            btnExportar = new Button
            {
                Text = "Exportar CSV",
                Left = 742, Top = 8, Width = 110, Height = 32,
                BackColor = Color.FromArgb(76, 175, 80),
                FlatStyle = FlatStyle.Flat, ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnExportar.FlatAppearance.BorderSize = 0;
            btnExportar.Click += BtnExportar_Click;

            pnlFiltros.Controls.AddRange(new Control[] { lblDesde, dtpDesde, lblHasta, dtpHasta, lblRubro, cboFiltroRubro, btnConsultar, btnExportar });

            // LABEL RESUMEN
            lblResumen = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                BackColor = Color.FromArgb(232, 234, 246),
                Padding = new Padding(10, 0, 0, 0),
                Text = "Seleccione un período y presione Consultar."
            };

            // GRILLA
            dgvRubros = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 9.5F) },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(63, 81, 181),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    SelectionBackColor = Color.FromArgb(63, 81, 181)
                },
                EnableHeadersVisualStyles = false
            };
            dgvRubros.DataBindingComplete += DgvRubros_DataBindingComplete;

            // PANEL GRÁFICO (barras horizontales simples)
            pnlGrafico = new Panel
            {
                Dock = DockStyle.Right,
                Width = 310,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            pnlGrafico.Paint += PnlGrafico_Paint;

            // Splitter entre grilla y gráfico
            var splitter = new Splitter { Dock = DockStyle.Right, Width = 4, BackColor = Color.FromArgb(220, 220, 220) };

            // PANEL CENTRAL (grilla + gráfico)
            var pnlCentro = new Panel { Dock = DockStyle.Fill };
            pnlCentro.Controls.Add(dgvRubros);
            pnlCentro.Controls.Add(splitter);
            pnlCentro.Controls.Add(pnlGrafico);

            // Agregar en orden correcto (DockStyle.Top se apila de abajo hacia arriba en Controls)
            this.Controls.Add(pnlCentro);
            this.Controls.Add(lblResumen);
            this.Controls.Add(pnlFiltros);
            this.Controls.Add(pnlHeader);

            this.Load += async (s, e) => await ConsultarAsync();
        }

        // Recarga el combo con los rubros que efectivamente aparecen en la grilla
        private void ActualizarComboDesdeGrilla()
        {
            var seleccionado = cboFiltroRubro.SelectedItem?.ToString();
            cboFiltroRubro.BeginUpdate();
            cboFiltroRubro.Items.Clear();
            cboFiltroRubro.Items.Add("(Todos los rubros)");
            foreach (var r in _datos.Select(d => d.Rubro).OrderBy(r => r, StringComparer.OrdinalIgnoreCase))
                cboFiltroRubro.Items.Add(r);
            // Restaurar selección previa si el rubro sigue en la grilla
            int idx = seleccionado != null ? cboFiltroRubro.Items.IndexOf(seleccionado) : 0;
            cboFiltroRubro.SelectedIndex = idx > 0 ? idx : 0;
            cboFiltroRubro.EndUpdate();
        }

        // ----------------------------------------------------------------
        // CONSULTA SQL
        // ----------------------------------------------------------------
        private async Task ConsultarAsync()
        {
            try
            {
                btnConsultar.Enabled = false;
                lblResumen.Text = "Consultando...";
                _datos.Clear();
                pnlGrafico.Invalidate();

                string cs = GetConnectionString();
                string rubroFiltro = cboFiltroRubro.SelectedIndex > 0 ? cboFiltroRubro.SelectedItem.ToString() : null;

                using (var conn = new SqlConnection(cs))
                {
                    await conn.OpenAsync();

                    // ---- COMPRAS por rubro ----
                    var sqlCompras = rubroFiltro == null
                        ? @"SELECT ISNULL(NULLIF(LTRIM(RTRIM(p.Rubro)), ''), 'Sin rubro') AS Rubro,
                                   SUM(c.ImporteTotal) AS TotalCompras
                            FROM ComprasProveedores c
                            LEFT JOIN Proveedores p ON p.Id = c.ProveedorId
                            WHERE c.Fecha BETWEEN @Desde AND @Hasta
                            GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(p.Rubro)), ''), 'Sin rubro')"
                        : @"SELECT ISNULL(NULLIF(LTRIM(RTRIM(p.Rubro)), ''), 'Sin rubro') AS Rubro,
                                   SUM(c.ImporteTotal) AS TotalCompras
                            FROM ComprasProveedores c
                            LEFT JOIN Proveedores p ON p.Id = c.ProveedorId
                            WHERE c.Fecha BETWEEN @Desde AND @Hasta
                              AND LTRIM(RTRIM(p.Rubro)) = @Rubro
                            GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(p.Rubro)), ''), 'Sin rubro')";

                    var comprasPorRubro = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                    using (var cmd = new SqlCommand(sqlCompras, conn))
                    {
                        cmd.Parameters.AddWithValue("@Desde", dtpDesde.Value.Date);
                        cmd.Parameters.AddWithValue("@Hasta", dtpHasta.Value.Date);
                        if (rubroFiltro != null) cmd.Parameters.AddWithValue("@Rubro", rubroFiltro);
                        using (var rdr = await cmd.ExecuteReaderAsync())
                            while (await rdr.ReadAsync())
                                comprasPorRubro[rdr.GetString(0)] = rdr.GetDecimal(1);
                    }

                    // ---- VENTAS por rubro ----
                    var sqlVentas = rubroFiltro == null
                        ? BuildVentasQuery()
                        : BuildVentasQuery(filtrarPorRubro: true);

                    var ventasPorRubro = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                    using (var cmd = new SqlCommand(sqlVentas, conn))
                    {
                        cmd.Parameters.AddWithValue("@Desde", dtpDesde.Value.Date);
                        cmd.Parameters.AddWithValue("@Hasta", dtpHasta.Value.Date);
                        if (rubroFiltro != null) cmd.Parameters.AddWithValue("@Rubro", rubroFiltro);
                        try
                        {
                            using (var rdr = await cmd.ExecuteReaderAsync())
                                while (await rdr.ReadAsync())
                                {
                                    string rubro = rdr.IsDBNull(0) || string.IsNullOrWhiteSpace(rdr.GetString(0))
                                        ? "Sin rubro" : rdr.GetString(0).Trim();
                                    ventasPorRubro[rubro] = rdr.GetDecimal(1);
                                }
                        }
                        catch { }
                    }

                    // ---- Combinar ----
                    var rubros = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var k in comprasPorRubro.Keys) rubros.Add(k);
                    foreach (var k in ventasPorRubro.Keys) rubros.Add(k);

                    foreach (var rubro in rubros.OrderBy(r => r))
                    {
                        decimal compras = comprasPorRubro.ContainsKey(rubro) ? comprasPorRubro[rubro] : 0m;
                        decimal ventas  = ventasPorRubro.ContainsKey(rubro)  ? ventasPorRubro[rubro]  : 0m;
                        decimal ganancia = ventas - compras;
                        decimal pct = compras > 0 ? Math.Round(ganancia / compras * 100, 1) : (ventas > 0 ? 100m : 0m);

                        _datos.Add(new RubroStats
                        {
                            Rubro       = rubro,
                            TotalCompras = compras,
                            TotalVentas  = ventas,
                            Ganancia     = ganancia,
                            PorcentajeGanancia = pct
                        });
                    }
                }

                // Mostrar en grilla
                var dt = new DataTable();
                dt.Columns.Add("Rubro",                typeof(string));
                dt.Columns.Add("Compras ($)",          typeof(decimal));
                dt.Columns.Add("Ventas ($)",           typeof(decimal));
                dt.Columns.Add("Ganancia/Pérdida ($)", typeof(decimal));
                dt.Columns.Add("% Margen",             typeof(decimal));

                foreach (var d in _datos)
                    dt.Rows.Add(d.Rubro, d.TotalCompras, d.TotalVentas, d.Ganancia, d.PorcentajeGanancia);

                dgvRubros.DataSource = dt;

                // Actualizar el combo con exactamente los rubros que quedaron en la grilla
                ActualizarComboDesdeGrilla();

                // Totales resumen
                decimal totalC = _datos.Sum(x => x.TotalCompras);
                decimal totalV = _datos.Sum(x => x.TotalVentas);
                decimal totalG = totalV - totalC;
                decimal totalPct = totalC > 0 ? Math.Round(totalG / totalC * 100, 1) : 0;
                string signo = totalG >= 0 ? "+" : "";
                lblResumen.Text = $"  Período {dtpDesde.Value:dd/MM/yyyy} – {dtpHasta.Value:dd/MM/yyyy}   |   " +
                                  $"Total Compras: ${totalC:N2}   Total Ventas: ${totalV:N2}   " +
                                  $"Ganancia neta: {signo}${totalG:N2}  ({signo}{totalPct}%)   |   " +
                                  $"{_datos.Count} rubros";
                lblResumen.ForeColor = totalG >= 0 ? Color.FromArgb(27, 94, 32) : Color.FromArgb(183, 28, 28);
                lblResumen.BackColor = totalG >= 0 ? Color.FromArgb(200, 230, 201) : Color.FromArgb(255, 205, 210);

                pnlGrafico.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConsultar.Enabled = true;
            }
        }

        // Query de ventas agrupadas por rubro.
        // La tabla 'ventas' ya tiene la columna 'rubro' directamente en cada línea de venta.
        private string BuildVentasQuery(bool filtrarPorRubro = false)
        {
            string whereRubro = filtrarPorRubro ? "AND LTRIM(RTRIM(rubro)) = @Rubro" : "";
            return $@"
                SELECT
                    ISNULL(NULLIF(LTRIM(RTRIM(rubro)), ''), 'Sin rubro') AS Rubro,
                    SUM(total) AS TotalVentas
                FROM ventas
                WHERE CAST(fecha AS date) BETWEEN @Desde AND @Hasta
                  {whereRubro}
                GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(rubro)), ''), 'Sin rubro')";
        }

        // ----------------------------------------------------------------
        // GRÁFICO DE BARRAS HORIZONTAL
        // ----------------------------------------------------------------
        private void PnlGrafico_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            if (_datos.Count == 0)
            {
                g.DrawString("Sin datos", new Font("Segoe UI", 9F), Brushes.Gray, new PointF(10, 10));
                return;
            }

            var titulo = new Font("Segoe UI", 9F, FontStyle.Bold);
            var fuente = new Font("Segoe UI", 8F);
            g.DrawString("Ganancia % por Rubro", titulo, new SolidBrush(Color.FromArgb(63, 81, 181)), new PointF(8, 6));

            int margenTop   = 28;
            int margenLeft  = 8;
            int barAltura   = 18;
            int espaciado   = 8;
            int etiquetaW   = 90;  // espacio para el nombre del rubro
            int barX        = margenLeft + etiquetaW + 5;
            int reservaTexto = 58; // espacio para el texto "+569.3%"
            int maxBarWidth  = Math.Max(10, pnlGrafico.ClientSize.Width - barX - reservaTexto - margenLeft);

            var datosOrdenados = _datos.OrderByDescending(d => d.PorcentajeGanancia).Take(15).ToList();
            decimal maxAbs = datosOrdenados.Count > 0 ? datosOrdenados.Max(d => Math.Abs(d.PorcentajeGanancia)) : 1m;
            if (maxAbs == 0) maxAbs = 1;

            for (int i = 0; i < datosOrdenados.Count; i++)
            {
                var d = datosOrdenados[i];
                int y = margenTop + i * (barAltura + espaciado);
                float escala = (float)(Math.Abs(d.PorcentajeGanancia) / maxAbs);
                int barWidth = (int)(escala * maxBarWidth);
                bool ganancia = d.PorcentajeGanancia >= 0;

                // Etiqueta rubro (truncar si es largo)
                string label = d.Rubro.Length > 13 ? d.Rubro.Substring(0, 12) + "…" : d.Rubro;
                g.DrawString(label, fuente, Brushes.DimGray, new PointF(margenLeft, y + 2));

                var colorBarra = ganancia ? Color.FromArgb(76, 175, 80) : Color.FromArgb(244, 67, 54);
                using (var brush = new SolidBrush(colorBarra))
                    g.FillRectangle(brush, barX, y, Math.Max(2, barWidth), barAltura);

                // Valor porcentaje — siempre dentro del panel
                string pctText = $"{(ganancia ? "+" : "")}{d.PorcentajeGanancia:N1}%";
                int textX = Math.Min(barX + barWidth + 3, pnlGrafico.ClientSize.Width - reservaTexto);
                g.DrawString(pctText, fuente,
                    ganancia ? new SolidBrush(Color.FromArgb(27, 94, 32)) : new SolidBrush(Color.FromArgb(183, 28, 28)),
                    new PointF(textX, y + 2));
            }
        }

        // ----------------------------------------------------------------
        // COLOREADO DE FILAS EN LA GRILLA
        // ----------------------------------------------------------------
        private void DgvRubros_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // Formatear columnas numéricas y colorear según ganancia/pérdida
            if (dgvRubros.Columns.Count < 5) return;

            foreach (DataGridViewColumn col in dgvRubros.Columns)
            {
                if (col.Index > 0)
                    col.DefaultCellStyle.Format = "N2";
                col.DefaultCellStyle.Alignment = col.Index == 0
                    ? DataGridViewContentAlignment.MiddleLeft
                    : DataGridViewContentAlignment.MiddleRight;
                if (col.Index > 0) col.FillWeight = 80;
            }
            dgvRubros.Columns[0].FillWeight = 160;

            foreach (DataGridViewRow row in dgvRubros.Rows)
            {
                if (row.Cells[3].Value == null) continue;
                decimal ganancia = Convert.ToDecimal(row.Cells[3].Value);
                if (ganancia > 0)
                {
                    row.Cells[3].Style.ForeColor = Color.FromArgb(27, 94, 32);
                    row.Cells[4].Style.ForeColor = Color.FromArgb(27, 94, 32);
                }
                else if (ganancia < 0)
                {
                    row.Cells[3].Style.ForeColor = Color.FromArgb(183, 28, 28);
                    row.Cells[4].Style.ForeColor = Color.FromArgb(183, 28, 28);
                }
            }
        }

        // ----------------------------------------------------------------
        // EXPORTAR CSV
        // ----------------------------------------------------------------
        private void BtnExportar_Click(object sender, EventArgs e)
        {
            if (_datos.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"ComprasVentasRubros_{dtpDesde.Value:yyyyMMdd}_{dtpHasta.Value:yyyyMMdd}.csv",
                Title = "Exportar a CSV"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    var lines = new System.Collections.Generic.List<string>
                    {
                        "Rubro;TotalCompras;TotalVentas;Ganancia;PorcentajeMargen"
                    };
                    foreach (var d in _datos)
                        lines.Add($"{d.Rubro};{d.TotalCompras:N2};{d.TotalVentas:N2};{d.Ganancia:N2};{d.PorcentajeGanancia:N1}%");
                    System.IO.File.WriteAllLines(dlg.FileName, lines, System.Text.Encoding.UTF8);
                    MessageBox.Show("Exportación completada.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exportando: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ----------------------------------------------------------------
        // HELPERS
        // ----------------------------------------------------------------
        private string GetConnectionString()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return config.GetConnectionString("DefaultConnection");
        }

        // ----------------------------------------------------------------
        // MODELO
        // ----------------------------------------------------------------
        private class RubroStats
        {
            public string Rubro { get; set; }
            public decimal TotalCompras { get; set; }
            public decimal TotalVentas  { get; set; }
            public decimal Ganancia     { get; set; }
            public decimal PorcentajeGanancia { get; set; }
        }
    }
}
