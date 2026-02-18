using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Comercio.NET.Services;

namespace Comercio.NET.Formularios
{
    public partial class ArqueoCajaForm : Form
    {
        private ComboBox cmbCajero;
        private DateTimePicker dtpFechaDesde, dtpFechaHasta;
        private DataGridView dgvResumenPorMedio, dgvDetalleTransacciones;
        private Button btnCalcular, btnExportar;
        private Label lblTotalEsperado, lblEstadoCaja, lblTiempoTranscurrido;
        private TextBox txtNotas;
        private Panel panelInfo, panelResumen, panelDetalle;
        private Label lblMontoInicial, lblTotalIngresos, lblTotalEgresos, lblSaldoActual;

        public ArqueoCajaForm()
        {
            InitializeComponent();
            ConfigurarFormulario();
            _ = CargarCajeros();
            ActualizarFechasPorDefecto();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new Size(900, 510);
            this.MinimumSize = new Size(900, 510);
            this.Name = "ArqueoCajaForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Arqueo de Caja";
            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            this.Text = "📋 Arqueo de Caja";
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = new Font("Segoe UI", 9F);

            CrearControles();
            ConfigurarEventos();
        }


        private void CrearControles()
        {
            int margin = 15;
            int currentY = 15;

            // Título compacto
            var lblTitulo = new Label
            {
                Text = "📋 ARQUEO DE CAJA",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(margin, currentY),
                Size = new Size(870, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblTitulo);
            currentY += 35;

            // Panel de Filtros reducido
            panelInfo = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(870, 90),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelInfo);

            // Título del panel
            panelInfo.Controls.Add(new Label
            {
                Text = "🔍 SELECCIÓN Y ESTADO",
                Location = new Point(10, 8),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            // Estado de caja
            lblEstadoCaja = new Label
            {
                Text = "⚫ Estado: Sin calcular",
                Location = new Point(530, 8),
                Size = new Size(330, 20),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleRight
            };
            panelInfo.Controls.Add(lblEstadoCaja);

            // Cajero
            panelInfo.Controls.Add(new Label
            {
                Text = "Cajero:",
                Location = new Point(10, 35),
                Size = new Size(60, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            cmbCajero = new ComboBox
            {
                Location = new Point(75, 33),
                Size = new Size(160, 22),
                Font = new Font("Segoe UI", 9F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            panelInfo.Controls.Add(cmbCajero);

            // Desde
            panelInfo.Controls.Add(new Label
            {
                Text = "Desde:",
                Location = new Point(250, 35),
                Size = new Size(50, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            dtpFechaDesde = new DateTimePicker
            {
                Location = new Point(305, 33),
                Size = new Size(130, 22),
                Font = new Font("Segoe UI", 8F),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yy HH:mm"
            };
            panelInfo.Controls.Add(dtpFechaDesde);

            // Hasta
            panelInfo.Controls.Add(new Label
            {
                Text = "Hasta:",
                Location = new Point(450, 35),
                Size = new Size(45, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });
            panelInfo.Controls.Add(dtpFechaHasta);

            dtpFechaHasta = new DateTimePicker
            {
                Location = new Point(500, 33),
                Size = new Size(130, 22),
                Font = new Font("Segoe UI", 8F),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yy HH:mm"
            };
            panelInfo.Controls.Add(dtpFechaHasta);

            // Botón Calcular
            btnCalcular = new Button
            {
                Text = "📊 Calcular",
                Location = new Point(650, 30),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnCalcular.FlatAppearance.BorderSize = 0;
            panelInfo.Controls.Add(btnCalcular);

            // Botón Exportar
            btnExportar = new Button
            {
                Text = "📄",
                Location = new Point(760, 30),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Enabled = false,
                //ToolTipText = "Exportar arqueo"
            };
            btnExportar.FlatAppearance.BorderSize = 0;
            panelInfo.Controls.Add(btnExportar);

            // Tiempo transcurrido
            lblTiempoTranscurrido = new Label
            {
                Text = "⏱️ Turno: --:--",
                Location = new Point(10, 65),
                Size = new Size(850, 18),
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            panelInfo.Controls.Add(lblTiempoTranscurrido);

            currentY += 105;

            // Panel de Resumen Financiero compacto
            var panelResumenFinanciero = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(870, 60),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelResumenFinanciero);

            // ✅ Monto Inicial - AJUSTADO
            panelResumenFinanciero.Controls.Add(new Label
            {
                Text = "💰 Inicial:",
                Location = new Point(15, 12),
                Size = new Size(70, 18),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            });

            lblMontoInicial = new Label
            {
                Text = "$0.00",
                Location = new Point(15, 32),
                Size = new Size(120, 20),  // ✅ Aumentado de 100 a 120
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            };
            panelResumenFinanciero.Controls.Add(lblMontoInicial);

            // ✅ Total Ingresos - AJUSTADO
            panelResumenFinanciero.Controls.Add(new Label
            {
                Text = "📈 Ingresos:",
                Location = new Point(155, 12),  // ✅ Movido de 135 a 155
                Size = new Size(75, 18),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            });

            lblTotalIngresos = new Label
            {
                Text = "$0.00",
                Location = new Point(155, 32),  // ✅ Movido de 135 a 155
                Size = new Size(120, 20),  // ✅ Aumentado de 100 a 120
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80)
            };
            panelResumenFinanciero.Controls.Add(lblTotalIngresos);

            // ✅ Total Egresos - AJUSTADO
            panelResumenFinanciero.Controls.Add(new Label
            {
                Text = "📉 Egresos:",
                Location = new Point(295, 12),  // ✅ Movido de 255 a 295
                Size = new Size(70, 18),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            });

            lblTotalEgresos = new Label
            {
                Text = "$0.00",
                Location = new Point(295, 32),  // ✅ Movido de 255 a 295
                Size = new Size(120, 20),  // ✅ Aumentado de 100 a 120
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 67, 54)
            };
            panelResumenFinanciero.Controls.Add(lblTotalEgresos);

            // ✅ Saldo Actual - AJUSTADO
            panelResumenFinanciero.Controls.Add(new Label
            {
                Text = "💵 Saldo:",
                Location = new Point(435, 12),  // ✅ Movido de 375 a 435
                Size = new Size(60, 18),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            });

            lblSaldoActual = new Label
            {
                Text = "$0.00",
                Location = new Point(435, 32),  // ✅ Movido de 375 a 435
                Size = new Size(130, 20),  // ✅ Aumentado de 110 a 130
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 152, 0)
            };
            panelResumenFinanciero.Controls.Add(lblSaldoActual);

            // ✅ Nota informativa - AJUSTADA
            var lblInfo = new Label
            {
                Text = "ℹ️ El arqueo NO cierra el turno",
                Location = new Point(590, 20),  // ✅ Movido de 520 a 590
                Size = new Size(270, 30),  // ✅ Ajustado de 340 a 270
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            panelResumenFinanciero.Controls.Add(lblInfo);

            currentY += 75;

            // Panel Resumen por Medio de Pago (izquierda) - MÁS REDUCIDO
            panelResumen = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(420, 270), // Era 300, ahora 270
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelResumen);

            panelResumen.Controls.Add(new Label
            {
                Text = "💳 RESUMEN POR MEDIO",
                Location = new Point(10, 10),
                Size = new Size(400, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            dgvResumenPorMedio = new DataGridView
            {
                Location = new Point(10, 35),
                Size = new Size(400, 195), // Era 225, ahora 195
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 8F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToResizeRows = false
            };

            dgvResumenPorMedio.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            dgvResumenPorMedio.RowTemplate.Height = 22;

            dgvResumenPorMedio.Columns.Add("MedioPago", "Medio");
            dgvResumenPorMedio.Columns.Add("Cantidad", "Cant.");
            dgvResumenPorMedio.Columns.Add("Ingresos", "Ingresos");
            dgvResumenPorMedio.Columns.Add("Egresos", "Egresos");
            dgvResumenPorMedio.Columns.Add("Neto", "Neto");

            dgvResumenPorMedio.Columns["MedioPago"].FillWeight = 25;
            dgvResumenPorMedio.Columns["Cantidad"].FillWeight = 15;
            dgvResumenPorMedio.Columns["Ingresos"].FillWeight = 20;
            dgvResumenPorMedio.Columns["Egresos"].FillWeight = 20;
            dgvResumenPorMedio.Columns["Neto"].FillWeight = 20;

            panelResumen.Controls.Add(dgvResumenPorMedio);

            // Total esperado
            panelResumen.Controls.Add(new Label
            {
                Text = "TOTAL NETO:",
                Location = new Point(10, 237), // Ajustado: era 267, ahora 237
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243)
            });

            lblTotalEsperado = new Label
            {
                Text = "$0.00",
                Location = new Point(280, 235), // Ajustado: era 265, ahora 235
                Size = new Size(130, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                TextAlign = ContentAlignment.MiddleRight
            };
            panelResumen.Controls.Add(lblTotalEsperado);

            // Panel Detalle (derecha) - MÁS REDUCIDO
            panelDetalle = new Panel
            {
                Location = new Point(margin + 435, currentY),
                Size = new Size(440, 270), // Era 300, ahora 270
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelDetalle);

            panelDetalle.Controls.Add(new Label
            {
                Text = "📝 DETALLE DE TRANSACCIONES",
                Location = new Point(10, 10),
                Size = new Size(420, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            dgvDetalleTransacciones = new DataGridView
            {
                Location = new Point(10, 35),
                Size = new Size(420, 170), // Era 195, ahora 170
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 8F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToResizeRows = false
            };

            dgvDetalleTransacciones.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            dgvDetalleTransacciones.RowTemplate.Height = 20;

            dgvDetalleTransacciones.Columns.Add("Fecha", "Fecha/Hora");
            dgvDetalleTransacciones.Columns.Add("NumeroFactura", "Nº Fact.");
            dgvDetalleTransacciones.Columns.Add("MedioPago", "Medio");
            dgvDetalleTransacciones.Columns.Add("Importe", "Importe");
            dgvDetalleTransacciones.Columns.Add("Tipo", "Tipo");

            dgvDetalleTransacciones.Columns["Fecha"].FillWeight = 23;
            dgvDetalleTransacciones.Columns["NumeroFactura"].FillWeight = 18;
            dgvDetalleTransacciones.Columns["MedioPago"].FillWeight = 18;
            dgvDetalleTransacciones.Columns["Importe"].FillWeight = 18;
            dgvDetalleTransacciones.Columns["Tipo"].FillWeight = 23;

            panelDetalle.Controls.Add(dgvDetalleTransacciones);

            // Notas
            panelDetalle.Controls.Add(new Label
            {
                Text = "📝 Notas:",
                Location = new Point(10, 213), // Ajustado: era 238, ahora 213
                Size = new Size(80, 18),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            });

            txtNotas = new TextBox
            {
                Location = new Point(10, 233), // Ajustado: era 258, ahora 233
                Size = new Size(420, 30), // Era 35, ahora 30
                Multiline = true,
                Font = new Font("Segoe UI", 8F),
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "Anotaciones opcionales..."
            };
            panelDetalle.Controls.Add(txtNotas);

            // Botón Cerrar (abajo a la derecha) - eliminado, se cierra desde el MDI
        }

        private void ConfigurarEventos()
        {
            btnCalcular.Click += async (s, e) => await CalcularArqueo();
            btnExportar.Click += (s, e) => ExportarArqueo();
            cmbCajero.SelectedIndexChanged += (s, e) => ActualizarFechasPorDefecto();
        }

        private void ActualizarFechasPorDefecto()
        {
            // Buscar el turno abierto del cajero seleccionado
            if (cmbCajero.SelectedIndex > 0)
            {
                _ = CargarFechasTurnoAbierto();
            }
            else
            {
                // Por defecto: turno del día actual
                dtpFechaDesde.Value = DateTime.Today.AddHours(DateTime.Now.Hour < 6 ? -18 : 6);
                dtpFechaHasta.Value = DateTime.Now;
            }
        }

        private async Task CargarFechasTurnoAbierto()
        {
            try
            {
                if (cmbCajero.SelectedIndex <= 0) return;

                dynamic cajeroSeleccionado = cmbCajero.SelectedItem;
                int numeroCajero = cajeroSeleccionado.NumeroCajero;

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                var query = @"
                    SELECT TOP 1 FechaApertura, MontoInicial
                    FROM TurnosCajero
                    WHERE NumeroCajero = @numeroCajero
                    AND Estado = 'Abierto'
                    ORDER BY FechaApertura DESC";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);

                using var reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    DateTime fechaApertura = reader.GetDateTime(0);
                    dtpFechaDesde.Value = fechaApertura;
                    dtpFechaHasta.Value = DateTime.Now;

                    lblEstadoCaja.Text = $"🟢 Turno ABIERTO desde {fechaApertura:dd/MM/yyyy HH:mm}";
                    lblEstadoCaja.ForeColor = Color.FromArgb(76, 175, 80);
                }
                else
                {
                    lblEstadoCaja.Text = "🟡 Sin turno abierto - Usando período del día";
                    lblEstadoCaja.ForeColor = Color.FromArgb(255, 152, 0);

                    dtpFechaDesde.Value = DateTime.Today.AddHours(6);
                    dtpFechaHasta.Value = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando fechas: {ex.Message}");
            }
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

                // ✅ MODIFICADO: Solo cargar cajeros con turnos abiertos
                var query = @"
            SELECT DISTINCT 
                t.NumeroCajero, 
                COALESCE(MIN(u.Nombre + ' ' + u.Apellido), 'Cajero ' + CAST(t.NumeroCajero AS NVARCHAR)) as NombreCajero,
                MIN(t.FechaApertura) as FechaApertura
            FROM TurnosCajero t
            LEFT JOIN Usuarios u ON t.NumeroCajero = u.NumeroCajero AND u.Activo = 1
            WHERE t.Estado = 'Abierto'
            GROUP BY t.NumeroCajero
            ORDER BY t.NumeroCajero";

                using var cmd = new SqlCommand(query, connection);
                connection.Open();

                cmbCajero.Items.Clear();
                cmbCajero.Items.Add(new { NumeroCajero = -1, Display = "-- Seleccionar Cajero --" });

                int cajerosEncontrados = 0;

                using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    int numero = reader.GetInt32(0);
                    string nombre = reader.GetString(1);
                    DateTime fechaApertura = reader.GetDateTime(2);

                    cmbCajero.Items.Add(new
                    {
                        NumeroCajero = numero,
                        Display = $"Cajero #{numero} - {nombre}"
                    });

                    cajerosEncontrados++;
                }

                cmbCajero.DisplayMember = "Display";
                cmbCajero.ValueMember = "NumeroCajero";
                cmbCajero.SelectedIndex = 0;

                // ✅ Mostrar mensaje informativo si no hay cajeros con turnos abiertos
                if (cajerosEncontrados == 0)
                {
                    MessageBox.Show(
                        "⚠️ No hay cajeros con turnos abiertos actualmente.\n\n" +
                        "Para realizar un arqueo, primero debe abrir un turno desde el módulo de Apertura de Turno.",
                        "Sin Turnos Abiertos",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando cajeros: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CalcularArqueo()
        {
            try
            {
                if (cmbCajero.SelectedIndex <= 0)
                {
                    MessageBox.Show("Debe seleccionar un cajero", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (dtpFechaHasta.Value < dtpFechaDesde.Value)
                {
                    MessageBox.Show("La fecha final debe ser mayor a la fecha inicial", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                btnCalcular.Enabled = false;
                btnCalcular.Text = "⏳ Calculando...";

                dynamic cajeroSeleccionado = cmbCajero.SelectedItem;
                int numeroCajero = cajeroSeleccionado.NumeroCajero;

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Obtener monto inicial del turno abierto
                decimal montoInicial = await ObtenerMontoInicial(connection, numeroCajero);
                lblMontoInicial.Text = montoInicial.ToString("C2");

                // Calcular resumen por medio de pago (igual que en CierreTurno)
                var resumenPorMedio = await CalcularResumenPorMedioPago(connection, numeroCajero);

                // Llenar grilla
                dgvResumenPorMedio.Rows.Clear();
                decimal totalIngresos = 0;
                decimal totalEgresos = 0;
                decimal totalNeto = 0;

                foreach (var kvp in resumenPorMedio.OrderBy(x => x.Key))
                {
                    string medioPago = kvp.Key;
                    decimal ingresos = kvp.Value.Ingresos;
                    decimal egresos = kvp.Value.Egresos;
                    int cantTotal = kvp.Value.CantIngresos + kvp.Value.CantEgresos;
                    decimal neto = ingresos - egresos;

                    dgvResumenPorMedio.Rows.Add(
                        medioPago,
                        cantTotal,
                        ingresos.ToString("C2"),
                        egresos.ToString("C2"),
                        neto.ToString("C2")
                    );

                    totalIngresos += ingresos;
                    totalEgresos += egresos;
                    totalNeto += neto;

                    // Colorear
                    int rowIndex = dgvResumenPorMedio.Rows.Count - 1;
                    if (egresos > 0)
                    {
                        dgvResumenPorMedio.Rows[rowIndex].Cells["Egresos"].Style.ForeColor = Color.Red;
                        dgvResumenPorMedio.Rows[rowIndex].Cells["Egresos"].Style.Font = new Font(dgvResumenPorMedio.Font, FontStyle.Bold);
                    }
                }

                // Actualizar totales
                lblTotalIngresos.Text = totalIngresos.ToString("C2");
                lblTotalEgresos.Text = totalEgresos.ToString("C2");
                lblSaldoActual.Text = (montoInicial + totalNeto).ToString("C2");
                lblTotalEsperado.Text = totalNeto.ToString("C2");

                // Cargar detalle
                await CargarDetalleTransacciones(connectionString, numeroCajero);

                // Actualizar estado
                TimeSpan tiempoTranscurrido = dtpFechaHasta.Value - dtpFechaDesde.Value;
                lblTiempoTranscurrido.Text = $"⏱️ Período: {tiempoTranscurrido.Days}d {tiempoTranscurrido.Hours}h {tiempoTranscurrido.Minutes}m";

                lblEstadoCaja.Text = "✅ Arqueo calculado correctamente";
                lblEstadoCaja.ForeColor = Color.FromArgb(76, 175, 80);

                btnExportar.Enabled = true;
                btnCalcular.Text = "📊 Calcular Arqueo";
                btnCalcular.Enabled = true;

                //MessageBox.Show(
                //    $"✅ Arqueo calculado exitosamente\n\n" +
                //    $"Total transacciones: {dgvDetalleTransacciones.Rows.Count}\n" +
                //    $"Saldo actual: {lblSaldoActual.Text}",
                //    "Arqueo Completado",
                //    MessageBoxButtons.OK,
                //    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculando arqueo: {ex.Message}\n\n{ex.StackTrace}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnCalcular.Text = "📊 Calcular Arqueo";
                btnCalcular.Enabled = true;
            }
        }

        private async Task<decimal> ObtenerMontoInicial(SqlConnection connection, int numeroCajero)
        {
            var query = @"
                SELECT TOP 1 MontoInicial
                FROM TurnosCajero
                WHERE NumeroCajero = @numeroCajero
                AND Estado = 'Abierto'
                ORDER BY FechaApertura DESC";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToDecimal(result) : 0m;
        }

        private async Task<System.Collections.Generic.Dictionary<string, (decimal Ingresos, decimal Egresos, int CantIngresos, int CantEgresos)>>
    CalcularResumenPorMedioPago(SqlConnection connection, int numeroCajero)
        {
            var resumen = new Dictionary<string, (decimal Ingresos, decimal Egresos, int CantIngresos, int CantEgresos)>();

            try
            {
                // ========================================
                // QUERY DE INGRESOS (VENTAS) - ✅ USANDO CAMPO HORA
                // ========================================
                var queryIngresos = @"
                    WITH TransaccionesSimples AS (
                        SELECT 
                            COALESCE(f.FormadePago, 'Efectivo') as MedioPago,
                            f.ImporteTotal as Importe,
                            'Ingreso' as TipoMovimiento
                        FROM Facturas f
                        INNER JOIN Usuarios u ON f.UsuarioVenta = u.NombreUsuario
                        WHERE u.NumeroCajero = @numeroCajero
                        AND f.Hora BETWEEN @fechaInicio AND @fechaFin
                        AND COALESCE(f.FormadePago, 'Efectivo') NOT IN ('Múltiples Medios', 'Multiple')
                        AND COALESCE(f.esCtaCte, 0) = 0
                    ),
                    TransaccionesMultiples AS (
                        SELECT 
                            dp.MedioPago,
                            dp.Importe,
                            'Ingreso' as TipoMovimiento
                        FROM DetallesPagoFactura dp
                        INNER JOIN Facturas f ON dp.IdFactura = f.idFactura
                        INNER JOIN Usuarios u ON f.UsuarioVenta = u.NombreUsuario
                        WHERE u.NumeroCajero = @numeroCajero
                        AND f.Hora BETWEEN @fechaInicio AND @fechaFin
                        AND COALESCE(f.FormadePago, 'Efectivo') IN ('Múltiples Medios', 'Multiple')
                        AND COALESCE(f.esCtaCte, 0) = 0
                    )
                    SELECT 
                        MedioPago,
                        SUM(Importe) as TotalIngresos,
                        COUNT(*) as CantidadIngresos
                    FROM (
                        SELECT * FROM TransaccionesSimples
                        UNION ALL
                        SELECT * FROM TransaccionesMultiples
                    ) TodasTransacciones
                    GROUP BY MedioPago";

                using (var cmd = new SqlCommand(queryIngresos, connection))
                {
                    cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                    cmd.Parameters.AddWithValue("@fechaInicio", dtpFechaDesde.Value);
                    cmd.Parameters.AddWithValue("@fechaFin", dtpFechaHasta.Value);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        string medioPago = reader.GetString(0);
                        decimal ingresos = reader.GetDecimal(1);
                        int cantidad = reader.GetInt32(2);

                        if (!resumen.ContainsKey(medioPago))
                            resumen[medioPago] = (0m, 0m, 0, 0);

                        var actual = resumen[medioPago];
                        resumen[medioPago] = (ingresos, actual.Egresos, cantidad, actual.CantEgresos);
                    }
                }

                // ========================================
                // ✅ QUERY DE EGRESOS (PAGOS A PROVEEDORES)
                // ========================================
                var queryPagosProveedores = @"
                    SELECT 
                        SUM(Monto) AS TotalEgresos,
                        COUNT(*) AS CantidadEgresos
                    FROM PagosProveedores
                    WHERE NumeroCajero = @numeroCajero
                    AND FechaPago BETWEEN @fechaInicio AND @fechaFin
                    AND (Origen IS NULL OR Origen <> 'PagoGeneral')";

                using (var cmd = new SqlCommand(queryPagosProveedores, connection))
                {
                    cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                    cmd.Parameters.AddWithValue("@fechaInicio", dtpFechaDesde.Value);
                    cmd.Parameters.AddWithValue("@fechaFin", dtpFechaHasta.Value);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (reader.Read() && reader["TotalEgresos"] != DBNull.Value)
                    {
                        string medioPago = "Efectivo";  // ✅ SIEMPRE Efectivo por ahora
                        decimal egresos = reader.GetDecimal(0);
                        int cantidad = reader.GetInt32(1);

                        if (!resumen.ContainsKey(medioPago))
                            resumen[medioPago] = (0m, 0m, 0, 0);

                        var actual = resumen[medioPago];
                        resumen[medioPago] = (
                            actual.Ingresos,
                            actual.Egresos + egresos,
                            actual.CantIngresos,
                            actual.CantEgresos + cantidad);

                        System.Diagnostics.Debug.WriteLine(
                            $"💳 PAGOS PROVEEDORES SUMADOS:\n" +
                            $"   Total egresos: {egresos:C2}\n" +
                            $"   Cantidad: {cantidad}");
                    }
                }

                // ========================================
                // ✅ Query de retiros de efectivo (si existe)
                // ========================================
                var queryRetiros = @"
                    SELECT 
                        SUM(Monto) AS TotalRetiros,
                        COUNT(*) AS CantidadRetiros
                    FROM RetirosEfectivo
                    WHERE NumeroCajero = @numeroCajero
                    AND FechaRetiro BETWEEN @fechaInicio AND @fechaFin";

                using (var cmd = new SqlCommand(queryRetiros, connection))
                {
                    cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                    cmd.Parameters.AddWithValue("@fechaInicio", dtpFechaDesde.Value);
                    cmd.Parameters.AddWithValue("@fechaFin", dtpFechaHasta.Value);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (reader.Read() && reader["TotalRetiros"] != DBNull.Value)
                    {
                        decimal totalRetiros = reader.GetDecimal(0);
                        int cantidadRetiros = reader.GetInt32(1);

                        // ✅ Registrar retiros como EGRESO de EFECTIVO
                        if (totalRetiros > 0)
                        {
                            string medioPago = "Efectivo";

                            if (!resumen.ContainsKey(medioPago))
                                resumen[medioPago] = (0m, 0m, 0, 0);

                            var actual = resumen[medioPago];
                            resumen[medioPago] = (
                                actual.Ingresos,
                                actual.Egresos + totalRetiros,  // ✅ SUMAR retiros a egresos
                                actual.CantIngresos,
                                actual.CantEgresos + cantidadRetiros
                            );

                            System.Diagnostics.Debug.WriteLine(
                                $"💰 RETIROS SUMADOS:\n" +
                                $"   Total: {totalRetiros:C2}\n" +
                                $"   Cantidad: {cantidadRetiros}");
                        }
                    }
                }

                // ✅ NUEVO: Debug final para verificar el resumen completo
                System.Diagnostics.Debug.WriteLine($"\n📊 RESUMEN FINAL POR MEDIO:");
                foreach (var kvp in resumen)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"   {kvp.Key}:\n" +
                        $"      Ingresos: {kvp.Value.Ingresos:C2} ({kvp.Value.CantIngresos} trans.)\n" +
                        $"      Egresos: {kvp.Value.Egresos:C2} ({kvp.Value.CantEgresos} trans.)\n" +
                        $"      Neto: {(kvp.Value.Ingresos - kvp.Value.Egresos):C2}");
                }

                return resumen;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en CalcularResumenPorMedioPago: {ex.Message}");
                throw;
            }
        }

        private async Task CargarDetalleTransacciones(string connectionString, int numeroCajero)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var queryDetalle = @"
        -- Ventas (✅ USANDO CAMPO HORA)
        WITH TransaccionesVentasSimples AS (
            SELECT 
                f.Hora as Fecha,
                COALESCE(f.NroFactura, CAST(f.NumeroRemito AS NVARCHAR)) as NumeroFactura,
                COALESCE(f.FormadePago, 'Efectivo') as MedioPago,
                f.ImporteTotal as Importe,
                'Ingreso (Venta)' as Tipo
            FROM Facturas f
            INNER JOIN Usuarios u ON f.UsuarioVenta = u.NombreUsuario
            WHERE u.NumeroCajero = @numeroCajero
            AND f.Hora BETWEEN @fechaInicio AND @fechaFin
            AND COALESCE(f.FormadePago, 'Efectivo') NOT IN ('Múltiples Medios', 'Multiple')
            AND COALESCE(f.esCtaCte, 0) = 0
        ),
        TransaccionesVentasMultiples AS (
            SELECT 
                f.Hora as Fecha,
                COALESCE(f.NroFactura, CAST(f.NumeroRemito AS NVARCHAR)) as NumeroFactura,
                dp.MedioPago,
                dp.Importe,
                'Ingreso (Venta)' as Tipo
            FROM DetallesPagoFactura dp
            INNER JOIN Facturas f ON dp.IdFactura = f.idFactura
            INNER JOIN Usuarios u ON f.UsuarioVenta = u.NombreUsuario
            WHERE u.NumeroCajero = @numeroCajero
            AND f.Hora BETWEEN @fechaInicio AND @fechaFin
            AND COALESCE(f.FormadePago, 'Efectivo') IN ('Múltiples Medios', 'Multiple')
            AND COALESCE(f.esCtaCte, 0) = 0
        ),
        -- ✅ CORREGIDO: Pagos a Proveedores
        TransaccionesPagosProveedores AS (
            SELECT 
                pp.FechaPago as Fecha,
                'Pago #' + CAST(pp.Id AS NVARCHAR) + ' - ' + pp.Proveedor as NumeroFactura,
                'Efectivo' AS MedioPago, 
                pp.Monto as Importe,
                'Egreso (Pago Prov.)' as Tipo
            FROM PagosProveedores pp
            WHERE pp.NumeroCajero = @numeroCajero
            AND pp.FechaPago BETWEEN @fechaInicio AND @fechaFin
        ),
        -- Retiros de Efectivo
        TransaccionesRetiros AS (
            SELECT 
                FechaRetiro as Fecha,
                'Retiro #' + CAST(Id AS NVARCHAR) + ' - ' + Responsable as NumeroFactura,
                'Efectivo' as MedioPago,
                Monto as Importe,
                'Egreso (Retiro)' as Tipo
            FROM RetirosEfectivo
            WHERE NumeroCajero = @numeroCajero
            AND FechaRetiro BETWEEN @fechaInicio AND @fechaFin
        )
        SELECT Fecha, NumeroFactura, MedioPago, Importe, Tipo
        FROM (
            SELECT * FROM TransaccionesVentasSimples
            UNION ALL
            SELECT * FROM TransaccionesVentasMultiples
            UNION ALL
            SELECT * FROM TransaccionesPagosProveedores
            UNION ALL
            SELECT * FROM TransaccionesRetiros
        ) TodasTransacciones
        ORDER BY Fecha DESC";

            using var cmd = new SqlCommand(queryDetalle, connection);
            cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);
            cmd.Parameters.AddWithValue("@fechaInicio", dtpFechaDesde.Value);
            cmd.Parameters.AddWithValue("@fechaFin", dtpFechaHasta.Value);

            dgvDetalleTransacciones.Rows.Clear();

            try
            {
                using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    DateTime fecha = reader.GetDateTime(0);
                    string numeroFactura = reader.GetString(1);
                    string medioPago = reader.GetString(2);
                    decimal importe = reader.GetDecimal(3);
                    string tipo = reader.GetString(4);

                    dgvDetalleTransacciones.Rows.Add(
                        fecha.ToString("dd/MM/yy HH:mm"),
                        numeroFactura,
                        medioPago,
                        importe.ToString("C2"),
                        tipo
                    );

                    int rowIndex = dgvDetalleTransacciones.Rows.Count - 1;
                    if (tipo.Contains("Egreso"))
                    {
                        dgvDetalleTransacciones.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(244, 67, 54);
                        dgvDetalleTransacciones.Rows[rowIndex].DefaultCellStyle.Font = new Font(dgvDetalleTransacciones.Font, FontStyle.Bold);
                    }
                    else
                    {
                        dgvDetalleTransacciones.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(76, 175, 80);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando detalle: {ex.Message}");
                MessageBox.Show($"Error parcial cargando detalle de transacciones:\n{ex.Message}\n\nLos datos disponibles se mostrarán.",
                    "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ExportarArqueo()
        {
            try
            {
                if (dgvResumenPorMedio.Rows.Count == 0)
                {
                    MessageBox.Show("No hay datos para exportar", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var sfd = new SaveFileDialog
                {
                    Filter = "Archivo CSV|*.csv|Todos los archivos|*.*",
                    FileName = $"ArqueoCaja_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using var writer = new System.IO.StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);

                    // Encabezado del arqueo
                    writer.WriteLine($"ARQUEO DE CAJA - {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                    writer.WriteLine($"Cajero: {cmbCajero.Text}");
                    writer.WriteLine($"Período: {dtpFechaDesde.Value:dd/MM/yyyy HH:mm} - {dtpFechaHasta.Value:dd/MM/yyyy HH:mm}");
                    writer.WriteLine($"Monto Inicial: {lblMontoInicial.Text}");
                    writer.WriteLine($"Total Ingresos: {lblTotalIngresos.Text}");
                    writer.WriteLine($"Total Egresos: {lblTotalEgresos.Text}");
                    writer.WriteLine($"Saldo Actual: {lblSaldoActual.Text}");
                    writer.WriteLine();

                    // Resumen por medio de pago
                    writer.WriteLine("RESUMEN POR MEDIO DE PAGO");
                    writer.WriteLine("Medio de Pago;Cantidad;Ingresos;Egresos;Neto");
                    foreach (DataGridViewRow row in dgvResumenPorMedio.Rows)
                    {
                        if (row.IsNewRow) continue;
                        writer.WriteLine($"{row.Cells[0].Value};{row.Cells[1].Value};{row.Cells[2].Value};{row.Cells[3].Value};{row.Cells[4].Value}");
                    }

                    writer.WriteLine();
                    writer.WriteLine("DETALLE DE TRANSACCIONES");
                    writer.WriteLine("Fecha/Hora;Nº Factura;Medio Pago;Importe;Tipo");
                    foreach (DataGridViewRow row in dgvDetalleTransacciones.Rows)
                    {
                        if (row.IsNewRow) continue;
                        writer.WriteLine($"{row.Cells[0].Value};{row.Cells[1].Value};{row.Cells[2].Value};{row.Cells[3].Value};{row.Cells[4].Value}");
                    }

                    if (!string.IsNullOrWhiteSpace(txtNotas.Text))
                    {
                        writer.WriteLine();
                        writer.WriteLine($"NOTAS: {txtNotas.Text}");
                    }

                    MessageBox.Show($"✅ Arqueo exportado correctamente a:\n{sfd.FileName}", "Éxito",
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
}