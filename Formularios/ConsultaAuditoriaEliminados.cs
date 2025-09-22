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
        private TextBox txtCodigoProducto, txtNumeroFactura, txtUsuario; // AGREGADO: txtUsuario
        private Button btnBuscar, btnExportar, btnSalir; // AGREGADO: btnSalir
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
            this.StartPosition = FormStartPosition.CenterScreen;

            // Panel de filtros
            var panelFiltros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            // PRIMERA FILA - Todos los filtros y botón Buscar
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

            // CAMBIO: Botón Buscar al lado del filtro Usuario
            btnBuscar = new Button
            {
                Text = "Buscar",
                Location = new Point(895, 10), // CAMBIO: Al lado del filtro Usuario
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnBuscar.Click += BtnBuscar_Click;

            // SEGUNDA FILA - Solo botones Exportar y Salir
            btnExportar = new Button
            {
                Text = "Exportar",
                Location = new Point(10, 45), // CAMBIO: Exportar queda a la izquierda
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnExportar.Click += BtnExportar_Click;

            btnSalir = new Button
            {
                Text = "Salir",
                Location = new Point(100, 45), // CAMBIO: Salir al lado de Exportar
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSalir.Click += BtnSalir_Click;

            // Label para total de registros - En la segunda fila a la derecha
            lblTotalRegistros = new Label
            {
                Text = "Total: 0 registros",
                Location = new Point(300, 50), // CAMBIO: Ajustado para dar más espacio
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            // Agregar todos los controles al panel
            panelFiltros.Controls.AddRange(new Control[]
            {
                lblDesde, dtpDesde, lblHasta, dtpHasta,
                lblCodigo, txtCodigoProducto, lblFactura, txtNumeroFactura,
                lblUsuario, txtUsuario, btnBuscar, // Buscar en la primera fila
                btnExportar, btnSalir, // Exportar y Salir en la segunda fila
                lblTotalRegistros
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

        // NUEVO: Event handler para el botón Salir
        private void BtnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // NUEVO: Método para configurar estilos del DataGridView
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

        // MODIFICADO: Agregar filtro por usuario
        private async Task BuscarRegistros()
        {
            try
            {
                string connectionString = GetConnectionString();
                string query = @"
                    SELECT 
                        IdAuditoriaProductosEliminados,
                        CodigoProducto AS 'Código',
                        DescripcionProducto AS 'Descripción del Producto',
                        PrecioUnitario AS 'Precio Unitario',
                        Cantidad AS 'Cant.',
                        TotalEliminado AS 'Total Eliminado',
                        NumeroFactura AS 'Remito',
                        FechaHoraVentaOriginal AS 'Fecha Factura',
                        FechaEliminacion AS 'Fecha Eliminación',
                        MotivoEliminacion AS 'Motivo de Eliminación',
                        CASE WHEN EsCtaCte = 1 THEN 'Sí' ELSE 'No' END AS 'CtaCte',
                        UsuarioEliminacion AS 'Usuario',
                        NombreEquipo AS 'Equipo',
                        IPUsuario AS 'IP'
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

                // NUEVO: Filtro por usuario
                if (!string.IsNullOrWhiteSpace(txtUsuario.Text))
                {
                    query += " AND UsuarioEliminacion LIKE @usuario";
                    parametros.Add(new SqlParameter("@usuario", $"%{txtUsuario.Text.Trim()}%"));
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

        // MEJORADO: Formateo con el nuevo orden y anchos de columnas
        private void FormatearDataGridView()
        {
            if (dgvAuditoria.Columns.Count == 0) return;

            // Ocultar columna Id
            if (dgvAuditoria.Columns["IdAuditoriaProductosEliminados"] != null)
                dgvAuditoria.Columns["IdAuditoriaProductosEliminados"].Visible = false;

            // NUEVO: Ocultar columna Precio Unitario
            if (dgvAuditoria.Columns["Precio Unitario"] != null)
                dgvAuditoria.Columns["Precio Unitario"].Visible = false;

            // Configuración de columnas (sin la línea de Precio Unitario)
            ConfigurarColumna("Código", 100, DataGridViewContentAlignment.MiddleCenter);
            ConfigurarColumna("Descripción del Producto", 160, DataGridViewContentAlignment.MiddleLeft);
            ConfigurarColumna("Cant.", 45, DataGridViewContentAlignment.MiddleCenter);
            ConfigurarColumna("Total Eliminado", 100, DataGridViewContentAlignment.MiddleRight, "C2");
            ConfigurarColumna("Remito", 70, DataGridViewContentAlignment.MiddleCenter);
            ConfigurarColumna("Fecha Factura", 110, DataGridViewContentAlignment.MiddleCenter, "dd/MM/yyyy HH:mm");
            ConfigurarColumna("Fecha Eliminación", 120, DataGridViewContentAlignment.MiddleCenter, "dd/MM/yyyy HH:mm");
            ConfigurarColumna("Motivo de Eliminación", 160, DataGridViewContentAlignment.MiddleLeft);
            ConfigurarColumna("CtaCte", 50, DataGridViewContentAlignment.MiddleCenter);
            ConfigurarColumna("Usuario", 75, DataGridViewContentAlignment.MiddleCenter);
            ConfigurarColumna("Equipo", 75, DataGridViewContentAlignment.MiddleCenter);
            ConfigurarColumna("IP", 90, DataGridViewContentAlignment.MiddleCenter);

            // CAMBIO: Ajustar ancho mínimo de "Motivo de Eliminación" para la expansión automática
            if (dgvAuditoria.Columns["Motivo de Eliminación"] != null)
            {
                dgvAuditoria.Columns["Motivo de Eliminación"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvAuditoria.Columns["Motivo de Eliminación"].MinimumWidth = 180;
            }
        }

        // MEJORADO: Método unificado para configurar columnas
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

                // NUEVO: Colores especiales para ciertos tipos de columnas
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
            }
            catch (Exception)
            {
                // Si hay error, intentar después de que el control se haya renderizado
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