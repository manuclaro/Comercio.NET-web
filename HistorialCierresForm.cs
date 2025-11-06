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
        private Button btnBuscar, btnVerDetalle, btnExportar;
        private Label lblTotalCierres, lblTotalDeclarado, lblTotalDiferencias;
        private Panel panelFiltros, panelResumen, panelDetalle;
        private int? cierreSeleccionadoId = null;

        public HistorialCierresForm()
        {
            InitializeComponent();
            ConfigurarFormulario();
            _ = InicializarDatosAsync();
        }

        private async Task InicializarDatosAsync()
        {
            await CargarCajeros();
            await CargarHistorial();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new Size(900, 510);
            this.MinimumSize = new Size(900, 510);
            this.Name = "HistorialCierresForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Historial de Cierres de Turno";
            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            this.Text = "📋 Historial de Cierres de Turno";
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = new Font("Segoe UI", 9F);

            CrearControles();
            ConfigurarEventos();
        }

        private void CrearControles()
        {
            int margin = 15;
            int currentY = 15;

            // Título
            var lblTitulo = new Label
            {
                Text = "📋 HISTORIAL DE CIERRES DE TURNO",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(margin, currentY),
                Size = new Size(420, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblTitulo);
            currentY += 30;

            // Panel de Filtros - MÁS COMPACTO (reducido verticalmente)
            panelFiltros = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(870, 62), // Era 75, ahora 62
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelFiltros);

            // Título del panel
            panelFiltros.Controls.Add(new Label
            {
                Text = "🔍 FILTROS",
                Location = new Point(8, 5), // Era 6, ahora 5
                Size = new Size(80, 16),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            // Cajero
            panelFiltros.Controls.Add(new Label
            {
                Text = "Cajero:",
                Location = new Point(8, 28), // Era 32, ahora 28
                Size = new Size(48, 18),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            });

            cmbCajero = new ComboBox
            {
                Location = new Point(60, 26), // Era 30, ahora 26
                Size = new Size(140, 20),
                Font = new Font("Segoe UI", 8F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            panelFiltros.Controls.Add(cmbCajero);

            // Desde
            panelFiltros.Controls.Add(new Label
            {
                Text = "Desde:",
                Location = new Point(210, 28), // Era 32, ahora 28
                Size = new Size(42, 18),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            });

            dtpDesde = new DateTimePicker
            {
                Location = new Point(255, 26), // Era 30, ahora 26
                Size = new Size(95, 20),
                Font = new Font("Segoe UI", 8F),
                Format = DateTimePickerFormat.Short
            };
            dtpDesde.Value = DateTime.Today.AddMonths(-1);
            panelFiltros.Controls.Add(dtpDesde);

            // Hasta
            panelFiltros.Controls.Add(new Label
            {
                Text = "Hasta:",
                Location = new Point(360, 28), // Era 32, ahora 28
                Size = new Size(38, 18),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            });

            dtpHasta = new DateTimePicker
            {
                Location = new Point(402, 26), // Era 30, ahora 26
                Size = new Size(95, 20),
                Font = new Font("Segoe UI", 8F),
                Format = DateTimePickerFormat.Short
            };
            dtpHasta.Value = DateTime.Today;
            panelFiltros.Controls.Add(dtpHasta);

            // Estado
            panelFiltros.Controls.Add(new Label
            {
                Text = "Estado:",
                Location = new Point(507, 28), // Era 32, ahora 28
                Size = new Size(43, 18),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            });

            cmbEstado = new ComboBox
            {
                Location = new Point(553, 26), // Era 30, ahora 26
                Size = new Size(85, 20),
                Font = new Font("Segoe UI", 8F),
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
                Text = "🔍 Buscar",
                Location = new Point(648, 24), // Era 28, ahora 24
                Size = new Size(100, 24),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            panelFiltros.Controls.Add(btnBuscar);

            // Botón Exportar
            btnExportar = new Button
            {
                Text = "📊 Exportar",
                Location = new Point(758, 24), // Era 28, ahora 24
                Size = new Size(100, 24),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btnExportar.FlatAppearance.BorderSize = 0;
            panelFiltros.Controls.Add(btnExportar);

            currentY += 75; // Era 88, ahora 75

            // Panel Resumen de Totales - MÁS COMPACTO
            panelResumen = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(870, 42),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelResumen);

            panelResumen.Controls.Add(new Label
            {
                Text = "Cierres:",
                Location = new Point(12, 12),
                Size = new Size(55, 18),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            });

            lblTotalCierres = new Label
            {
                Text = "0",
                Location = new Point(70, 12),
                Size = new Size(60, 18),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243)
            };
            panelResumen.Controls.Add(lblTotalCierres);

            panelResumen.Controls.Add(new Label
            {
                Text = "Declarado:",
                Location = new Point(280, 12),
                Size = new Size(70, 18),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            });

            lblTotalDeclarado = new Label
            {
                Text = "$0.00",
                Location = new Point(355, 12),
                Size = new Size(100, 18),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80)
            };
            panelResumen.Controls.Add(lblTotalDeclarado);

            panelResumen.Controls.Add(new Label
            {
                Text = "Diferencias:",
                Location = new Point(590, 12),
                Size = new Size(75, 18),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            });

            lblTotalDiferencias = new Label
            {
                Text = "$0.00",
                Location = new Point(670, 12),
                Size = new Size(100, 18),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 67, 54)
            };
            panelResumen.Controls.Add(lblTotalDiferencias);

            currentY += 55;

            // DataGridView Principal - Historial
            dgvHistorial = new DataGridView
            {
                Location = new Point(margin, currentY),
                Size = new Size(870, 190),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 7.5F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };

            dgvHistorial.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            dgvHistorial.RowTemplate.Height = 20;

            // Configurar columnas
            dgvHistorial.Columns.Add("Id", "ID");
            dgvHistorial.Columns.Add("NumeroCajero", "Caj.");
            dgvHistorial.Columns.Add("Usuario", "Usuario");
            dgvHistorial.Columns.Add("FechaApertura", "Apertura");
            dgvHistorial.Columns.Add("FechaCierre", "Cierre");
            dgvHistorial.Columns.Add("MontoInicial", "Inic.");
            dgvHistorial.Columns.Add("TotalEsperado", "Esperado");
            dgvHistorial.Columns.Add("TotalDeclarado", "Declarado");
            dgvHistorial.Columns.Add("Diferencia", "Dif.");
            dgvHistorial.Columns.Add("Estado", "Estado");

            dgvHistorial.Columns["Id"].FillWeight = 6;
            dgvHistorial.Columns["NumeroCajero"].FillWeight = 8;
            dgvHistorial.Columns["Usuario"].FillWeight = 14;
            dgvHistorial.Columns["FechaApertura"].FillWeight = 14;
            dgvHistorial.Columns["FechaCierre"].FillWeight = 14;
            dgvHistorial.Columns["MontoInicial"].FillWeight = 11;
            dgvHistorial.Columns["TotalEsperado"].FillWeight = 11;
            dgvHistorial.Columns["TotalDeclarado"].FillWeight = 11;
            dgvHistorial.Columns["Diferencia"].FillWeight = 11;
            dgvHistorial.Columns["Estado"].FillWeight = 9;

            this.Controls.Add(dgvHistorial);
            currentY += 203;

            // Panel Detalle del Cierre - MÁS COMPACTO (grilla más pequeña)
            panelDetalle = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(870, 115), // Era 140, ahora 110
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelDetalle);

            panelDetalle.Controls.Add(new Label
            {
                Text = "📊 DETALLE DEL CIERRE SELECCIONADO",
                Location = new Point(8, 6),
                Size = new Size(280, 18),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            // DataGridView Detalle - MÁS PEQUEÑO (solo 3-4 filas)
            dgvDetalleCierre = new DataGridView
            {
                Location = new Point(8, 28),
                Size = new Size(725, 80), // Era 105, ahora 75
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 7.5F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };

            dgvDetalleCierre.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            dgvDetalleCierre.RowTemplate.Height = 18;

            dgvDetalleCierre.Columns.Add("MedioPago", "Medio de Pago");
            dgvDetalleCierre.Columns.Add("CantidadTransacciones", "Cant.");
            dgvDetalleCierre.Columns.Add("TotalEsperado", "Esperado");
            dgvDetalleCierre.Columns.Add("TotalDeclarado", "Declarado");
            dgvDetalleCierre.Columns.Add("Diferencia", "Diferencia");

            panelDetalle.Controls.Add(dgvDetalleCierre);

            // Botón Ver Detalle
            btnVerDetalle = new Button
            {
                Text = "👁️ Ver Completo",
                Location = new Point(743, 40), // Ajustado: era 55, ahora 40
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Enabled = false
            };
            btnVerDetalle.FlatAppearance.BorderSize = 0;
            panelDetalle.Controls.Add(btnVerDetalle);
        }

        private void ConfigurarEventos()
        {
            btnBuscar.Click += async (s, e) => await CargarHistorial();
            btnExportar.Click += (s, e) => ExportarHistorial();
            btnVerDetalle.Click += (s, e) => VerDetalleCompleto();
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
                cmbCajero.Items.Add(new { NumeroCajero = -1, Display = "Todos" });

                using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    int numero = reader.GetInt32(0);
                    string nombre = reader.GetString(1);
                    cmbCajero.Items.Add(new 
                    { 
                        NumeroCajero = numero, 
                        Display = $"#{numero} - {nombre}" 
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
                // Validar que los controles estén inicializados
                if (cmbCajero.SelectedItem == null || cmbEstado.SelectedItem == null)
                {
                    return;
                }

                btnBuscar.Enabled = false;
                btnBuscar.Text = "⏳ Cargando...";

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
                        $"#{cajero}",
                        usuario,
                        apertura.ToString("dd/MM HH:mm"),
                        cierre?.ToString("dd/MM HH:mm") ?? "Sin cerrar",
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

                btnBuscar.Text = "🔍 Buscar";
                btnBuscar.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando historial: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnBuscar.Text = "🔍 Buscar";
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

                    MessageBox.Show($"✅ Datos exportados correctamente a:\n{sfd.FileName}", "Éxito",
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
        private DataGridView dgvDetalle;
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
            this.ClientSize = new Size(800, 520);
            this.Text = $"📄 Detalle del Cierre - Turno #{turnoId}";
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = new Font("Segoe UI", 9F);

            CrearControles();
        }

        private void CrearControles()
        {
            int margin = 15;
            int currentY = 15;

            // Información del turno
            lblTurnoInfo = new Label
            {
                Text = "Cargando información...",
                Location = new Point(margin, currentY),
                Size = new Size(770, 45),
                Font = new Font("Segoe UI", 8.5F),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8)
            };
            this.Controls.Add(lblTurnoInfo);
            currentY += 58;

            // Detalle por medio de pago
            this.Controls.Add(new Label
            {
                Text = "💵 DETALLE POR MEDIO DE PAGO",
                Location = new Point(margin, currentY),
                Size = new Size(250, 18),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });
            currentY += 26;

            dgvDetalle = new DataGridView
            {
                Location = new Point(margin, currentY),
                Size = new Size(770, 160),
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 8.5F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };
            dgvDetalle.RowTemplate.Height = 20;
            this.Controls.Add(dgvDetalle);
            currentY += 173;

            // Totales
            var panelTotales = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(770, 62),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelTotales);

            panelTotales.Controls.Add(new Label
            {
                Text = "TOTAL ESPERADO:",
                Location = new Point(12, 10),
                Size = new Size(125, 18),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            });

            lblTotalEsperado = new Label
            {
                Text = "$0.00",
                Location = new Point(142, 10),
                Size = new Size(100, 18),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243)
            };
            panelTotales.Controls.Add(lblTotalEsperado);

            panelTotales.Controls.Add(new Label
            {
                Text = "TOTAL DECLARADO:",
                Location = new Point(285, 10),
                Size = new Size(135, 18),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            });

            lblTotalDeclarado = new Label
            {
                Text = "$0.00",
                Location = new Point(425, 10),
                Size = new Size(100, 18),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 152, 0)
            };
            panelTotales.Controls.Add(lblTotalDeclarado);

            panelTotales.Controls.Add(new Label
            {
                Text = "DIFERENCIA:",
                Location = new Point(12, 36),
                Size = new Size(125, 20),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            });

            lblDiferencia = new Label
            {
                Text = "$0.00",
                Location = new Point(142, 36),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 67, 54)
            };
            panelTotales.Controls.Add(lblDiferencia);

            currentY += 75;

            // Observaciones
            this.Controls.Add(new Label
            {
                Text = "📝 OBSERVACIONES:",
                Location = new Point(margin, currentY),
                Size = new Size(160, 18),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            });
            currentY += 23;

            txtObservaciones = new TextBox
            {
                Location = new Point(margin, currentY),
                Size = new Size(770, 62),
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Segoe UI", 8.5F),
                BackColor = Color.White,
                ScrollBars = ScrollBars.Vertical
            };
            this.Controls.Add(txtObservaciones);
            currentY += 75;

            // Botón Cerrar
            var btnCerrar = new Button
            {
                Text = "Cerrar",
                Location = new Point(695, currentY),
                Size = new Size(90, 28),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
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

                dgvDetalle.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

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

                        int rowIndex = dgvDetalle.Rows.Count - 1;
                        if (diferencia != 0)
                        {
                            dgvDetalle.Rows[rowIndex].Cells["Diferencia"].Style.ForeColor = diferencia > 0 ? Color.Green : Color.Red;
                            dgvDetalle.Rows[rowIndex].Cells["Diferencia"].Style.Font = new Font(dgvDetalle.Font, FontStyle.Bold);
                        }
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