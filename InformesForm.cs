using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Comercio.NET.Formularios
{
    public partial class InformesForm : Form
    {
        public InformesForm()
        {
            InitializeComponent();
            ConfigurarEstilo();
        }

        private void ConfigurarEstilo()
        {
            // Fondo general
            this.BackColor = Color.White;

            // Panel superior
            var panelTitulo = new Panel
            {
                Name = "panelTitulo",
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(0, 120, 215),
                Padding = new Padding(20, 10, 20, 10)
            };

            var lblTitulo = new Label
            {
                Text = "📈 Informes y Reportes",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            panelTitulo.Controls.Add(lblTitulo);
            this.Controls.Add(panelTitulo);
            this.Controls.SetChildIndex(panelTitulo, 0);

            // Botón
            btnReporteIVAxDia.BackColor = Color.FromArgb(0, 150, 136);
            btnReporteIVAxDia.ForeColor = Color.White;
            btnReporteIVAxDia.FlatStyle = FlatStyle.Flat;
            btnReporteIVAxDia.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnReporteIVAxDia.FlatAppearance.BorderSize = 0;
            btnReporteIVAxDia.Cursor = Cursors.Hand;

            // DataGridView
            dgvReporteIVAxDia.BackgroundColor = Color.White;
            dgvReporteIVAxDia.BorderStyle = BorderStyle.None;
            dgvReporteIVAxDia.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvReporteIVAxDia.RowHeadersVisible = false;
            dgvReporteIVAxDia.EnableHeadersVisualStyles = false;
            dgvReporteIVAxDia.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(230, 240, 250),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                SelectionBackColor = Color.FromArgb(230, 240, 250),
                WrapMode = DataGridViewTriState.True
            };
            dgvReporteIVAxDia.ColumnHeadersHeight = 45;
        }

        private async void btnReporteIVAxDia_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Obtener días del mes actual
                var hoy = DateTime.Today;
                int diasMes = DateTime.DaysInMonth(hoy.Year, hoy.Month);
                var dias = Enumerable.Range(1, diasMes)
                    .Select(d => new DateTime(hoy.Year, hoy.Month, d))
                    .ToList();

                // 2. Consultar alícuotas distintas del mes actual
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                List<decimal> alicuotas = new List<decimal>();
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var queryAlicuotas = @"
                        SELECT DISTINCT p.iva
                        FROM Facturas f
                        INNER JOIN Ventas v ON f.NumeroRemito = v.NroFactura
                        INNER JOIN Productos p ON v.codigo = p.codigo
                        WHERE YEAR(f.Fecha) = @anio AND MONTH(f.Fecha) = @mes
                        AND f.TipoFactura IN ('FacturaA', 'FacturaB')
                    ";
                    using (var cmd = new SqlCommand(queryAlicuotas, connection))
                    {
                        cmd.Parameters.AddWithValue("@anio", hoy.Year);
                        cmd.Parameters.AddWithValue("@mes", hoy.Month);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                                alicuotas.Add(reader.GetDecimal(0));
                        }
                    }
                }
                alicuotas = alicuotas.Distinct().OrderByDescending(a => a).ToList();

                // 3. Armar DataTable para el reporte
                var dt = new DataTable();
                dt.Columns.Add("Día", typeof(string));
                foreach (var alic in alicuotas)
                    dt.Columns.Add($"{alic:N2}%", typeof(decimal));
                dt.Columns.Add("Total", typeof(decimal)); // <-- Agrega la columna Total

                // 4. Consultar totales por día y alícuota
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    foreach (var dia in dias)
                    {
                        var row = dt.NewRow();
                        row["Día"] = dia.ToString("dd/MM/yyyy");
                        decimal sumaTotal = 0;
                        foreach (var alic in alicuotas)
                        {
                            var query = @"
                                SELECT SUM(v.total) as TotalVentas
                                FROM Facturas f
                                INNER JOIN Ventas v ON f.NumeroRemito = v.NroFactura
                                INNER JOIN Productos p ON v.codigo = p.codigo
                                WHERE CAST(f.Fecha AS DATE) = @fecha
                                AND p.iva = @iva
                                AND f.TipoFactura IN ('FacturaA', 'FacturaB')
                            ";
                            using (var cmd = new SqlCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@fecha", dia.Date);
                                cmd.Parameters.AddWithValue("@iva", alic);
                                var totalVentas = await cmd.ExecuteScalarAsync();
                                decimal total = 0;
                                if (totalVentas != DBNull.Value && totalVentas != null)
                                {
                                    decimal totalConIVA = Convert.ToDecimal(totalVentas);
                                    decimal baseImponible = Math.Round(totalConIVA / (1 + (alic / 100m)), 2);
                                    decimal montoIVA = Math.Round(totalConIVA - baseImponible, 2);
                                    total = montoIVA;
                                }
                                row[$"{alic:N2}%"] = total;
                                sumaTotal += total;
                            }
                        }
                        row["Total"] = sumaTotal; // Asigna la suma a la columna Total
                        dt.Rows.Add(row);
                    }
                }

                // 5. Mostrar en el DataGridView
                dgvReporteIVAxDia.DataSource = dt;
                dgvReporteIVAxDia.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

                // Formatear columnas de importes: $ y alineación derecha
                foreach (DataGridViewColumn col in dgvReporteIVAxDia.Columns)
                {
                    if (col.Name != "Día")
                    {
                        col.DefaultCellStyle.Format = "C2"; // Muestra $ y dos decimales según cultura local
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generando el informe: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}