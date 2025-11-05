using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class HistorialCierresForm : Form
    {
        private ComboBox cmbCajero, cmbEstado;
        private DateTimePicker dtpDesde, dtpHasta;
        private DataGridView dgvHistorial, dgvDetalleCierre;
        private Button btnBuscar, btnVerDetalle, btnExportar, btnCerrar;
        private Label lblTotalCierres, lblTotalDeclarado, lblTotalDiferencias;
        private Panel panelFiltros, panelResumen, panelDetalle;
        private int? cierreSeleccionadoId = null;

        public HistorialCierresForm()
        {
            InitializeComponent();
            ConfigurarFormulario();
            _ = CargarCajeros();
            _ = CargarHistorial();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new Size(1400, 800);
            this.MinimumSize = new Size(1200, 700);
            this.Name = "HistorialCierresForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Historial de Cierres de Turno";
            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            this.Text = "?? Historial de Cierres de Turno";
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = new Font("Segoe UI", 10F);

            CrearControles();
            ConfigurarEventos();
        }

        private void CrearControles()
        {
            int margin = 20;
            int currentY = 20;

            // Título
            var lblTitulo = new Label
            {
                Text = "?? HISTORIAL DE CIERRES DE TURNO",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(margin, currentY),
                Size = new Size(600, 35),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblTitulo);
            currentY += 50;

            // Panel de Filtros
            panelFiltros = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(1360, 100),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelFiltros);

            // Título del panel
            panelFiltros.Controls.Add(new Label
            {
                Text = "?? FILTROS DE BÚSQUEDA",
                Location = new Point(15, 10),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            // Cajero
            panelFiltros.Controls.Add(new Label
            {
                Text = "Cajero:",
                Location = new Point(15, 45),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            cmbCajero = new ComboBox
            {
                Location = new Point(100, 43),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            panelFiltros.Controls.Add(cmbCajero);

            // Desde
            panelFiltros.Controls.Add(new Label
            {
                Text = "Desde:",
                Location = new Point(320, 45),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            dtpDesde = new DateTimePicker
            {
                Location = new Point(385, 43),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Short
            };
            dtpDesde.Value = DateTime.Today.AddMonths(-1);
            panelFiltros.Controls.Add(dtpDesde);

            // Hasta
            panelFiltros.Controls.Add(new Label
            {
                Text = "Hasta:",
                Location = new Point(555, 45),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            dtpHasta = new DateTimePicker
            {
                Location = new Point(620, 43),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Short
            };
            dtpHasta.Value = DateTime.Today;
            panelFiltros.Controls.Add(dtpHasta);

            // Estado
            panelFiltros.Controls.Add(new Label
            {
                Text = "Estado:",
                Location = new Point(790, 45),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            cmbEstado = new ComboBox
            {
                Location = new Point(855, 43),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbEstado.Items.AddRange(new object[] 
            { 
                new { Value = "", Display = "Todos" },
                new { Value = "Abierto", Display = "Abierto" },
                new { Value = "Cerrado", Display = "Cerrado" }
            });
            cmbEstado.DisplayMember = "Display";
            cmbEstado.ValueMember = "Value";
            cmbEstado.SelectedIndex = 0;
            panelFiltros.Controls.Add(cmbEstado);

            // Botón Buscar
            btnBuscar = new Button
            {
                Text = "?? Buscar",
                Location = new Point(1025, 40),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            panelFiltros.Controls.Add(btnBuscar);

            // Botón Exportar
            btnExportar = new Button
            {
                Text = "?? Exportar",
                Location = new Point(1195, 40),
                Size = new Size(140, 35),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnExportar.FlatAppearance.BorderSize = 0;
            panelFiltros.Controls.Add(btnExportar);

            currentY += 120;

            // Panel Resumen de Totales
            panelResumen = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(1360, 60),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelResumen);

            panelResumen.Controls.Add(new Label
            {
                Text = "Total Cierres:",
                Location = new Point(15, 20),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            lblTotalCierres = new Label
            {
                Text = "0",
                Location = new Point(140, 20),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243)
            };
            panelResumen.Controls.Add(lblTotalCierres);

            panelResumen.Controls.Add(new Label
            {
                Text = "Total Declarado:",
                Location = new Point(450, 20),
                Size = new Size(130, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            lblTotalDeclarado = new Label
            {
                Text = "$0.00",
                Location = new Point(585, 20),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80)
            };
            panelResumen.Controls.Add(lblTotalDeclarado);

            panelResumen.Controls.Add(new Label
            {
                Text = "Total Diferencias:",
                Location = new Point(900, 20),
                Size = new Size(140, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            lblTotalDiferencias = new Label
            {
                Text = "$0.00",
                Location = new Point(1045, 20),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 67, 54)
            };
            panelResumen.Controls.Add(lblTotalDiferencias);

            currentY += 80;

            // DataGridView Principal - Historial
            dgvHistorial = new DataGridView
            {
                Location = new Point(margin, currentY),
                Size = new Size(1360, 350),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 9F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Configurar columnas
            dgvHistorial.Columns.Add("Id", "ID");
            dgvHistorial.Columns.Add("NumeroCajero", "Cajero");
            dgvHistorial.Columns.Add("Usuario", "Usuario");
            dgvHistorial.Columns.Add("FechaApertura", "Apertura");
            dgvHistorial.Columns.Add("FechaCierre", "Cierre");
            dgvHistorial.Columns.Add("MontoInicial", "Monto Inicial");
            dgvHistorial.Columns.Add("TotalEsperado", "Total Esperado");
            dgvHistorial.Columns.Add("TotalDeclarado", "Total Declarado");
            dgvHistorial.Columns.Add("Diferencia", "Diferencia");
            dgvHistorial.Columns.Add("Estado", "Estado");

            dgvHistorial.Columns["Id"].Width = 50;
            dgvHistorial.Columns["NumeroCajero"].Width = 80;
            dgvHistorial.Columns["Usuario"].Width = 120;
            dgvHistorial.Columns["FechaApertura"].Width = 140;
            dgvHistorial.Columns["FechaCierre"].Width = 140;
            dgvHistorial.Columns["MontoInicial"].Width = 120;
            dgvHistorial.Columns["TotalEsperado"].Width = 120;
            dgvHistorial.Columns["TotalDeclarado"].Width = 120;
            dgvHistorial.Columns["Diferencia"].Width = 120;
            dgvHistorial.Columns["Estado"].Width = 100;

            this.Controls.Add(dgvHistorial);
            currentY += 370;

            // Panel Detalle del Cierre
            panelDetalle = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(1360, 200),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelDetalle);

            panelDetalle.Controls.Add(new Label
            {
                Text = "?? DETALLE DEL CIERRE SELECCIONADO",
                Location = new Point(15, 10),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            // DataGridView Detalle
            dgvDetalleCierre = new DataGridView
            {
                Location = new Point(15, 45),
                Size = new Size(1100, 140),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 9F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgvDetalleCierre.Columns.Add("MedioPago", "Medio de Pago");
            dgvDetalleCierre.Columns.Add("CantidadTransacciones", "Cantidad");
            dgvDetalleCierre.Columns.Add("TotalEsperado", "Esperado");
            dgvDetalleCierre.Columns.Add("TotalDeclarado", "Declarado");
            dgvDetalleCierre.Columns.Add("Diferencia", "Diferencia");

            panelDetalle.Controls.Add(dgvDetalleCierre);

            // Botones en el panel de detalle
            btnVerDetalle = new Button
            {
                Text = "??? Ver Completo",
                Location = new Point(1130, 55),
                Size = new Size(200, 40),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Enabled = false
            };
            btnVerDetalle.FlatAppearance.BorderSize = 0;
            panelDetalle.Controls.Add(btnVerDetalle);

            btnCerrar = new Button
            {
                Text = "? Cerrar",
                Location = new Point(1130, 110),
                Size = new Size(200, 40),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            panelDetalle.Controls.Add(btnCerrar);
        }

        private void ConfigurarEventos()
        {
            btnBuscar.Click += async (s, e) => await CargarHistorial();
            btnExportar.Click += (s, e) => ExportarHistorial();
            btnVerDetalle.Click += (s, e) => VerDetalleCompleto();
            btnCerrar.Click += (s, e) => this.Close();
            dgvHistorial.SelectionChanged += async (s, e) => await CargarDetalleCierre();
        }

        private async Task CargarCajeros()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                
                var query = @"
                    SELECT DISTINCT NumeroCajero, 
                           COALESCE(MIN(Nombre + ' ' + Apellido), 'Cajero ' + CAST(NumeroCajero AS NVARCHAR)) as NombreCajero
                    FROM Usuarios
                    WHERE Activo = 1
                    GROUP BY NumeroCajero
                    ORDER BY NumeroCajero";

                using var cmd = new SqlCommand(query, connection);
                connection.Open();

                cmbCajero.Items.Clear();
                cmbCajero.Items.Add(new { NumeroCajero = -1, Display = "Todos los cajeros" });

                using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    int numero = reader.GetInt32(0);
                    string nombre = reader.GetString(1);
                    cmbCajero.Items.Add(new 
                    { 
                        NumeroCajero = numero, 
                        Display = $"Cajero #{numero} - {nombre}" 
                    });
                }

                cmbCajero.DisplayMember = "Display";
                cmbCajero.ValueMember = "NumeroCajero";
                cmbCajero.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando cajeros: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CargarHistorial()
        {
            try
            {
                btnBuscar.Enabled = false;
                btnBuscar.Text = "? Cargando...";

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                dynamic cajeroSeleccionado = cmbCajero.SelectedItem;
                int? numeroCajero = cajeroSeleccionado.NumeroCajero == -1 ? (int?)null : cajeroSeleccionado.NumeroCajero;

                dynamic estadoSeleccionado = cmbEstado.SelectedItem;
                string estado = estadoSeleccionado.Value;

                var query = @"
                    SELECT 
                        t.Id,
                        t.NumeroCajero,
                        t.Usuario,
                        t.FechaApertura,
                        t.FechaCierre,
                        t.MontoInicial,
                        COALESCE(SUM(c.TotalEsperado), 0) as TotalEsperado,
                        COALESCE(SUM(c.TotalDeclarado), 0) as TotalDeclarado,
                        COALESCE(SUM(c.Diferencia), 0) as Diferencia,
                        t.Estado
                    FROM TurnosCajero t
                    LEFT JOIN CierreTurnoCajero c ON t.Id = c.IdTurno
                    WHERE (@numeroCajero IS NULL OR t.NumeroCajero = @numeroCajero)
                    AND (@estado = '' OR t.Estado = @estado)
                    AND t.FechaApertura BETWEEN @fechaDesde AND @fechaHasta
                    GROUP BY t.Id, t.NumeroCajero, t.Usuario, t.FechaApertura, t.FechaCierre, t.MontoInicial, t.Estado
                    ORDER BY t.FechaApertura DESC";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@numeroCajero", (object)numeroCajero ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@estado", estado);
                cmd.Parameters.AddWithValue("@fechaDesde", dtpDesde.Value.Date);
                cmd.Parameters.AddWithValue("@fechaHasta", dtpHasta.Value.Date.AddDays(1).AddSeconds(-1));

                dgvHistorial.Rows.Clear();
                decimal totalDeclarado = 0;
                decimal totalDiferencias = 0;
                int totalCierres = 0;

                using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    int cajero = reader.GetInt32(1);
                    string usuario = reader.GetString(2);
                    DateTime apertura = reader.GetDateTime(3);
                    DateTime? cierre = reader.IsDBNull(4) ? null : reader.GetDateTime(4);
                    decimal montoInicial = reader.GetDecimal(5);
                    decimal esperado = reader.GetDecimal(6);
                    decimal declarado = reader.GetDecimal(7);
                    decimal diferencia = reader.GetDecimal(8);
                    string estadoTurno = reader.GetString(9);

                    dgvHistorial.Rows.Add(
                        id,
                        $"Cajero #{cajero}",
                        usuario,
                        apertura.ToString("dd/MM/yyyy HH:mm"),
                        cierre?.ToString("dd/MM/yyyy HH:mm") ?? "Sin cerrar",
                        montoInicial.ToString("C2"),
                        esperado.ToString("C2"),
                        declarado.ToString("C2"),
                        diferencia.ToString("C2"),
                        estadoTurno
                    );

                    int rowIndex = dgvHistorial.Rows.Count - 1;

                    // Colorear según estado
                    if (estadoTurno == "Abierto")
                    {
                        dgvHistorial.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 224);
                    }
                    else if (estadoTurno == "Cerrado")
                    {
                        dgvHistorial.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(232, 245, 233);
                        
                        // Colorear diferencia
                        if (diferencia != 0)
                        {
                            dgvHistorial.Rows[rowIndex].Cells["Diferencia"].Style.ForeColor = diferencia > 0 ? Color.Green : Color.Red;
                            dgvHistorial.Rows[rowIndex].Cells["Diferencia"].Style.Font = new Font(dgvHistorial.Font, FontStyle.Bold);
                        }

                        totalDeclarado += declarado;
                        totalDiferencias += diferencia;
                        totalCierres++;
                    }
                }

                // Actualizar resumen
                lblTotalCierres.Text = totalCierres.ToString();
                lblTotalDeclarado.Text = totalDeclarado.ToString("C2");
                lblTotalDiferencias.Text = totalDiferencias.ToString("C2");
                lblTotalDiferencias.ForeColor = totalDiferencias >= 0 ? Color.FromArgb(76, 175, 80) : Color.FromArgb(244, 67, 54);

                btnBuscar.Text = "?? Buscar";
                btnBuscar.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando historial: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnBuscar.Text = "?? Buscar";
                btnBuscar.Enabled = true;
            }
        }

        private async Task CargarDetalleCierre()
        {
            try
            {
                if (dgvHistorial.SelectedRows.Count == 0)
                {
                    dgvDetalleCierre.Rows.Clear();
                    btnVerDetalle.Enabled = false;
                    return;
                }

                int turnoId = Convert.ToInt32(dgvHistorial.SelectedRows[0].Cells["Id"].Value);
                cierreSeleccionadoId = turnoId;

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                var query = @"
                    SELECT 
                        MedioPago,
                        CantidadTransacciones,
                        TotalEsperado,
                        TotalDeclarado,
                        Diferencia
                    FROM CierreTurnoCajero
                    WHERE IdTurno = @idTurno
                    ORDER BY MedioPago";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@idTurno", turnoId);

                dgvDetalleCierre.Rows.Clear();

                using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    string medioPago = reader.GetString(0);
                    int cantidad = reader.GetInt32(1);
                    decimal esperado = reader.GetDecimal(2);
                    decimal declarado = reader.GetDecimal(3);
                    decimal diferencia = reader.GetDecimal(4);

                    dgvDetalleCierre.Rows.Add(
                        medioPago,
                        cantidad,
                        esperado.ToString("C2"),
                        declarado.ToString("C2"),
                        diferencia.ToString("C2")
                    );

                    int rowIndex = dgvDetalleCierre.Rows.Count - 1;
                    if (diferencia != 0)
                    {
                        dgvDetalleCierre.Rows[rowIndex].Cells["Diferencia"].Style.ForeColor = diferencia > 0 ? Color.Green : Color.Red;
                    }
                }

                btnVerDetalle.Enabled = dgvDetalleCierre.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando detalle: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void VerDetalleCompleto()
        {
            if (!cierreSeleccionadoId.HasValue) return;

            try
            {
                // Abrir el formulario de cierre en modo consulta
                using var formDetalle = new DetalleCierreCompletoForm(cierreSeleccionadoId.Value);
                formDetalle.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error mostrando detalle: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportarHistorial()
        {
            try
            {
                if (dgvHistorial.Rows.Count == 0)
                {
                    MessageBox.Show("No hay datos para exportar", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var sfd = new SaveFileDialog
                {
                    Filter = "Archivo CSV|*.csv|Todos los archivos|*.*",
                    FileName = $"HistorialCierres_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using var writer = new System.IO.StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                    
                    // Escribir encabezados
                    var headers = new System.Collections.Generic.List<string>();
                    foreach (DataGridViewColumn col in dgvHistorial.Columns)
                    {
                        headers.Add(col.HeaderText);
                    }
                    writer.WriteLine(string.Join(";", headers));

                    // Escribir datos
                    foreach (DataGridViewRow row in dgvHistorial.Rows)
                    {
                        if (row.IsNewRow) continue;

                        var values = new System.Collections.Generic.List<string>();
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            values.Add(cell.Value?.ToString() ?? "");
                        }
                        writer.WriteLine(string.Join(";", values));
                    }

                    MessageBox.Show($"? Datos exportados correctamente a:\n{sfd.FileName}", "Éxito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exportando: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Formulario de detalle completo (vista de solo lectura)
    public class DetalleCierreCompletoForm : Form
    {
        private int turnoId;
        private DataGridView dgvDetalle, dgvTransacciones;
        private Label lblTurnoInfo, lblTotalEsperado, lblTotalDeclarado, lblDiferencia;
        private TextBox txtObservaciones;

        public DetalleCierreCompletoForm(int turnoId)
        {
            this.turnoId = turnoId;
            InitializeComponent();
            _ = CargarDatos();
        }

        private void InitializeComponent()
        {
            this.ClientSize = new Size(1000, 700);
            this.Text = $"?? Detalle del Cierre - Turno #{turnoId}";
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = new Font("Segoe UI", 10F);

            CrearControles();
        }

        private void CrearControles()
        {
            int margin = 20;
            int currentY = 20;

            // Información del turno
            lblTurnoInfo = new Label
            {
                Text = "Cargando información...",
                Location = new Point(margin, currentY),
                Size = new Size(960, 60),
                Font = new Font("Segoe UI", 11F),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10)
            };
            this.Controls.Add(lblTurnoInfo);
            currentY += 80;

            // Detalle por medio de pago
            this.Controls.Add(new Label
            {
                Text = "?? DETALLE POR MEDIO DE PAGO",
                Location = new Point(margin, currentY),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });
            currentY += 35;

            dgvDetalle = new DataGridView
            {
                Location = new Point(margin, currentY),
                Size = new Size(960, 200),
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 9F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            this.Controls.Add(dgvDetalle);
            currentY += 220;

            // Totales
            var panelTotales = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(960, 80),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelTotales);

            panelTotales.Controls.Add(new Label
            {
                Text = "TOTAL ESPERADO:",
                Location = new Point(20, 15),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            lblTotalEsperado = new Label
            {
                Text = "$0.00",
                Location = new Point(180, 15),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243)
            };
            panelTotales.Controls.Add(lblTotalEsperado);

            panelTotales.Controls.Add(new Label
            {
                Text = "TOTAL DECLARADO:",
                Location = new Point(350, 15),
                Size = new Size(160, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            lblTotalDeclarado = new Label
            {
                Text = "$0.00",
                Location = new Point(520, 15),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 152, 0)
            };
            panelTotales.Controls.Add(lblTotalDeclarado);

            panelTotales.Controls.Add(new Label
            {
                Text = "DIFERENCIA:",
                Location = new Point(20, 45),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            });

            lblDiferencia = new Label
            {
                Text = "$0.00",
                Location = new Point(180, 45),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 67, 54)
            };
            panelTotales.Controls.Add(lblDiferencia);

            currentY += 100;

            // Observaciones
            this.Controls.Add(new Label
            {
                Text = "?? OBSERVACIONES:",
                Location = new Point(margin, currentY),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });
            currentY += 30;

            txtObservaciones = new TextBox
            {
                Location = new Point(margin, currentY),
                Size = new Size(960, 80),
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.White
            };
            this.Controls.Add(txtObservaciones);
            currentY += 100;

            // Botón Cerrar
            var btnCerrar = new Button
            {
                Text = "Cerrar",
                Location = new Point(880, currentY),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCerrar.Click += (s, e) => this.Close();
            this.Controls.Add(btnCerrar);
        }

        private async Task CargarDatos()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Cargar información del turno
                var queryTurno = @"
                    SELECT NumeroCajero, Usuario, FechaApertura, FechaCierre, MontoInicial, Estado, Observaciones
                    FROM TurnosCajero
                    WHERE Id = @id";

                using (var cmd = new SqlCommand(queryTurno, connection))
                {
                    cmd.Parameters.AddWithValue("@id", turnoId);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (reader.Read())
                    {
                        int cajero = reader.GetInt32(0);
                        string usuario = reader.GetString(1);
                        DateTime apertura = reader.GetDateTime(2);
                        DateTime? cierre = reader.IsDBNull(3) ? null : reader.GetDateTime(3);
                        decimal montoInicial = reader.GetDecimal(4);
                        string estado = reader.GetString(5);
                        string obs = reader.IsDBNull(6) ? "" : reader.GetString(6);

                        lblTurnoInfo.Text = $"Cajero #{cajero} | Usuario: {usuario} | Apertura: {apertura:dd/MM/yyyy HH:mm}\n" +
                                           $"Cierre: {cierre?.ToString("dd/MM/yyyy HH:mm") ?? "Sin cerrar"} | Monto Inicial: {montoInicial:C2} | Estado: {estado}";
                        txtObservaciones.Text = obs;
                    }
                }

                // Cargar detalle del cierre
                var queryDetalle = @"
                    SELECT MedioPago, CantidadTransacciones, TotalEsperado, TotalDeclarado, Diferencia
                    FROM CierreTurnoCajero
                    WHERE IdTurno = @id
                    ORDER BY MedioPago";

                dgvDetalle.Columns.Clear();
                dgvDetalle.Columns.Add("MedioPago", "Medio de Pago");
                dgvDetalle.Columns.Add("Cantidad", "Cantidad");
                dgvDetalle.Columns.Add("Esperado", "Esperado");
                dgvDetalle.Columns.Add("Declarado", "Declarado");
                dgvDetalle.Columns.Add("Diferencia", "Diferencia");

                decimal totalEsperado = 0;
                decimal totalDeclarado = 0;
                decimal totalDiferencia = 0;

                using (var cmd = new SqlCommand(queryDetalle, connection))
                {
                    cmd.Parameters.AddWithValue("@id", turnoId);
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        string medio = reader.GetString(0);
                        int cantidad = reader.GetInt32(1);
                        decimal esperado = reader.GetDecimal(2);
                        decimal declarado = reader.GetDecimal(3);
                        decimal diferencia = reader.GetDecimal(4);

                        dgvDetalle.Rows.Add(medio, cantidad, esperado.ToString("C2"), declarado.ToString("C2"), diferencia.ToString("C2"));

                        totalEsperado += esperado;
                        totalDeclarado += declarado;
                        totalDiferencia += diferencia;
                    }
                }

                lblTotalEsperado.Text = totalEsperado.ToString("C2");
                lblTotalDeclarado.Text = totalDeclarado.ToString("C2");
                lblDiferencia.Text = totalDiferencia.ToString("C2");
                lblDiferencia.ForeColor = totalDiferencia >= 0 ? Color.FromArgb(76, 175, 80) : Color.FromArgb(244, 67, 54);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando datos: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}