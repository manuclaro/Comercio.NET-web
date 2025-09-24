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
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            CargarDatosIniciales();
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
            dtpDesde = new DateTimePicker { Location = new Point(65, 12), Size = new Size(120, 23) };
            dtpDesde.Value = DateTime.Now.AddDays(-30);

            var lblHasta = new Label { Text = "Hasta:", Location = new Point(200, 15), Size = new Size(50, 20) };
            dtpHasta = new DateTimePicker { Location = new Point(255, 12), Size = new Size(120, 23) };

            var lblCodigo = new Label { Text = "Código:", Location = new Point(390, 15), Size = new Size(50, 20) };
            txtCodigoProducto = new TextBox { Location = new Point(445, 12), Size = new Size(100, 23) };

            var lblFactura = new Label { Text = "Remito:", Location = new Point(560, 15), Size = new Size(50, 20) };
            txtNumeroFactura = new TextBox { Location = new Point(615, 12), Size = new Size(100, 23) };

            var lblUsuario = new Label { Text = "Usuario:", Location = new Point(730, 15), Size = new Size(50, 20) };
            txtUsuario = new TextBox { Location = new Point(785, 12), Size = new Size(100, 23) };

            // SEGUNDA FILA - Cajero y TODOS los botones juntos
            var lblCajero = new Label { Text = "Cajero:", Location = new Point(10, 45), Size = new Size(50, 20) };
            txtCajero = new TextBox { Location = new Point(65, 42), Size = new Size(80, 23) };

            // TODOS LOS BOTONES EN LA SEGUNDA FILA - Alineados horizontalmente
            btnBuscar = new Button
            {
                Text = "Buscar",
                Location = new Point(160, 40),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnBuscar.Click += BtnBuscar_Click;

            btnExportar = new Button
            {
                Text = "Exportar",
                Location = new Point(250, 40),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnExportar.Click += BtnExportar_Click;

            btnSalir = new Button
            {
                Text = "Salir",
                Location = new Point(340, 40), 
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
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
                EnableHeadersVisualStyles = false
            };

            // NUEVO: Estilos mejorados para el DataGridView
            ConfigurarEstilosDataGridView();

            this.Controls.Add(dgvAuditoria);
            this.Controls.Add(panelFiltros);
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
            ConfigurarColumna("Fecha Eliminación", 120, DataGridViewContentAlignment.MiddleCenter, "dd/MM/yyyy HH:mm");
            ConfigurarColumna("Motivo de Eliminación", 140, DataGridViewContentAlignment.MiddleLeft);
            ConfigurarColumna("CtaCte", 60, DataGridViewContentAlignment.MiddleCenter);
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