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
        private TextBox txtCodigoProducto, txtNumeroFactura;
        private Button btnBuscar, btnExportar;
        private Label lblTotalRegistros;

        public ConsultaAuditoriaEliminados()
        {
            ConfigurarFormulario();
            // Remover CargarDatosIniciales() del constructor
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Cargar datos después de que el formulario sea visible
            CargarDatosIniciales();
        }

        private void ConfigurarFormulario()
        {
            this.Text = "Consulta de Productos Eliminados - Auditoría";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Panel de filtros
            var panelFiltros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            // Filtros de fecha
            var lblDesde = new Label { Text = "Desde:", Location = new Point(10, 15), Size = new Size(50, 20) };
            dtpDesde = new DateTimePicker { Location = new Point(65, 12), Size = new Size(120, 23) };
            dtpDesde.Value = DateTime.Now.AddDays(-30);

            var lblHasta = new Label { Text = "Hasta:", Location = new Point(200, 15), Size = new Size(50, 20) };
            dtpHasta = new DateTimePicker { Location = new Point(255, 12), Size = new Size(120, 23) };

            // Filtro por código de producto
            var lblCodigo = new Label { Text = "Código:", Location = new Point(390, 15), Size = new Size(50, 20) };
            txtCodigoProducto = new TextBox { Location = new Point(445, 12), Size = new Size(100, 23) };

            // Filtro por número de factura
            var lblFactura = new Label { Text = "N° Factura:", Location = new Point(560, 15), Size = new Size(70, 20) };
            txtNumeroFactura = new TextBox { Location = new Point(635, 12), Size = new Size(100, 23) };

            // Botones
            btnBuscar = new Button
            {
                Text = "Buscar",
                Location = new Point(750, 10),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnBuscar.Click += BtnBuscar_Click;

            btnExportar = new Button
            {
                Text = "Exportar",
                Location = new Point(840, 10),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnExportar.Click += BtnExportar_Click;

            // Label para total de registros
            lblTotalRegistros = new Label
            {
                Text = "Total: 0 registros",
                Location = new Point(10, 50),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            panelFiltros.Controls.AddRange(new Control[]
            {
                lblDesde, dtpDesde, lblHasta, dtpHasta,
                lblCodigo, txtCodigoProducto, lblFactura, txtNumeroFactura,
                btnBuscar, btnExportar, lblTotalRegistros
            });

            // DataGridView
            dgvAuditoria = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            this.Controls.Add(dgvAuditoria);
            this.Controls.Add(panelFiltros);
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
                        DescripcionProducto AS 'Descripción',
                        PrecioUnitario AS 'Precio Unit.',
                        Cantidad,
                        TotalEliminado AS 'Total Eliminado',
                        NumeroFactura AS 'N° Factura',
                        FechaVentaOriginal AS 'Fecha Venta',
                        FechaEliminacion AS 'Fecha Eliminación',
                        UsuarioEliminacion AS 'Usuario',
                        MotivoEliminacion AS 'Motivo',
                        CASE WHEN EsCtaCte = 1 THEN 'Sí' ELSE 'No' END AS 'Cta. Cte.',
                        NombreCtaCte AS 'Nombre Cta. Cte.',
                        NombreEquipo AS 'Equipo'
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

                query += " ORDER BY FechaEliminacion DESC";

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.SelectCommand.Parameters.AddRange(parametros.ToArray());
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        
                        dgvAuditoria.DataSource = dt;
                        lblTotalRegistros.Text = $"Total: {dt.Rows.Count} registros";
                        
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
            // Ocultar columna Id
            if (dgvAuditoria.Columns["IdAuditoriaProductosEliminados"] != null)
                dgvAuditoria.Columns["IdAuditoriaProductosEliminados"].Visible = false;

            // Formatear columnas monetarias
            FormatearColumnaMonetaria("Precio Unit.");
            FormatearColumnaMonetaria("Total Eliminado");

            // Formatear fechas
            FormatearColumnaFecha("Fecha Venta");
            FormatearColumnaFecha("Fecha Eliminación");

            // Ajustar anchos específicos
            AjustarAnchoColumna("Código", 80);
            AjustarAnchoColumna("Descripción", 200);
            AjustarAnchoColumna("Cantidad", 80);
            AjustarAnchoColumna("Motivo", 150);
        }

        private void FormatearColumnaMonetaria(string nombreColumna)
        {
            if (dgvAuditoria.Columns[nombreColumna] != null)
            {
                dgvAuditoria.Columns[nombreColumna].DefaultCellStyle.Format = "C2";
                dgvAuditoria.Columns[nombreColumna].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        private void FormatearColumnaFecha(string nombreColumna)
        {
            if (dgvAuditoria.Columns[nombreColumna] != null)
            {
                dgvAuditoria.Columns[nombreColumna].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            }
        }

        private void AjustarAnchoColumna(string nombreColumna, int ancho)
        {
            if (dgvAuditoria.Columns[nombreColumna] != null)
            {
                // Protección adicional para asegurar que el control esté completamente inicializado
                try
                {
                    dgvAuditoria.Columns[nombreColumna].Width = ancho;
                    dgvAuditoria.Columns[nombreColumna].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                }
                catch (Exception)
                {
                    // Si hay error, intentar después de que el control se haya renderizado
                    this.BeginInvoke(new Action(() =>
                    {
                        if (dgvAuditoria.Columns[nombreColumna] != null)
                        {
                            dgvAuditoria.Columns[nombreColumna].Width = ancho;
                            dgvAuditoria.Columns[nombreColumna].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        }
                    }));
                }
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

            // Encabezados (excluyendo la columna Id)
            var encabezados = dt.Columns.Cast<DataColumn>()
                .Where(col => col.ColumnName != "IdAuditoriaProductosEliminados")
                .Select(col => col.ColumnName);
            lines.Add(string.Join(",", encabezados));

            // Datos (excluyendo la columna Id)
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