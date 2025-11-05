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
        private Button btnCalcular, btnExportar, btnCerrar;
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
            this.ClientSize = new Size(1400, 850);
            this.MinimumSize = new Size(1200, 700);
            this.Name = "ArqueoCajaForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Arqueo de Caja";
            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            this.Text = "📋 Arqueo de Caja";
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = new Font("Segoe UI", 10F);

            CrearControles();
            ConfigurarEventos();
        }

        private void CrearControles()
        {
            int margin = 20;
            int currentY = 20;

            // ========================================
            // TÍTULO
            // ========================================
            var lblTitulo = new Label
            {
                Text = "📋 ARQUEO DE CAJA",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(margin, currentY),
                Size = new Size(600, 35),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblTitulo);
            currentY += 50;

            // ========================================
            // PANEL DE FILTROS Y ESTADO
            // ========================================
            panelInfo = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(1360, 120),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelInfo);

            // Título del panel
            panelInfo.Controls.Add(new Label
            {
                Text = "🔍 SELECCIÓN Y ESTADO DEL TURNO",
                Location = new Point(15, 10),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            // Estado de caja (superior derecha)
            lblEstadoCaja = new Label
            {
                Text = "⚫ Estado: Sin calcular",
                Location = new Point(900, 10),
                Size = new Size(440, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleRight
            };
            panelInfo.Controls.Add(lblEstadoCaja);

            // Cajero
            panelInfo.Controls.Add(new Label
            {
                Text = "Cajero:",
                Location = new Point(15, 50),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            cmbCajero = new ComboBox
            {
                Location = new Point(100, 48),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            panelInfo.Controls.Add(cmbCajero);

            // Desde
            panelInfo.Controls.Add(new Label
            {
                Text = "Desde:",
                Location = new Point(370, 50),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            dtpFechaDesde = new DateTimePicker
            {
                Location = new Point(435, 48),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm"
            };
            panelInfo.Controls.Add(dtpFechaDesde);

            // Hasta
            panelInfo.Controls.Add(new Label
            {
                Text = "Hasta:",
                Location = new Point(655, 50),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            dtpFechaHasta = new DateTimePicker
            {
                Location = new Point(720, 48),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm"
            };
            panelInfo.Controls.Add(dtpFechaHasta);

            // Botón Calcular
            btnCalcular = new Button
            {
                Text = "📊 Calcular Arqueo",
                Location = new Point(940, 45),
                Size = new Size(180, 35),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCalcular.FlatAppearance.BorderSize = 0;
            panelInfo.Controls.Add(btnCalcular);

            // Botón Exportar
            btnExportar = new Button
            {
                Text = "📄 Exportar",
                Location = new Point(1140, 45),
                Size = new Size(130, 35),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Enabled = false
            };
            btnExportar.FlatAppearance.BorderSize = 0;
            panelInfo.Controls.Add(btnExportar);

            //// TEMPORAL: Botón Diagnóstico
            //var btnDiagnostico = new Button
            //{
            //    Text = "🔍 Diagnóstico",
            //    Location = new Point(1285, 45),
            //    Size = new Size(70, 35),
            //    BackColor = Color.FromArgb(255, 152, 0),
            //    ForeColor = Color.White,
            //    FlatStyle = FlatStyle.Flat,
            //    Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            //};
            //btnDiagnostico.FlatAppearance.BorderSize = 0;
            //btnDiagnostico.Click += async (s, e) => await DiagnosticarProblemaVentas();
            //panelInfo.Controls.Add(btnDiagnostico);

            // Tiempo transcurrido (abajo del panel)
            lblTiempoTranscurrido = new Label
            {
                Text = "⏱️ Turno: --:--",
                Location = new Point(15, 85),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            panelInfo.Controls.Add(lblTiempoTranscurrido);

            currentY += 140;

            // ========================================
            // PANEL DE RESUMEN FINANCIERO
            // ========================================
            var panelResumenFinanciero = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(1360, 80),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelResumenFinanciero);

            // Monto Inicial
            panelResumenFinanciero.Controls.Add(new Label
            {
                Text = "💰 Monto Inicial:",
                Location = new Point(20, 15),
                Size = new Size(140, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            lblMontoInicial = new Label
            {
                Text = "$0.00",
                Location = new Point(20, 40),
                Size = new Size(140, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            };
            panelResumenFinanciero.Controls.Add(lblMontoInicial);

            // Total Ingresos
            panelResumenFinanciero.Controls.Add(new Label
            {
                Text = "📈 Total Ingresos:",
                Location = new Point(200, 15),
                Size = new Size(140, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            lblTotalIngresos = new Label
            {
                Text = "$0.00",
                Location = new Point(200, 40),
                Size = new Size(140, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80)
            };
            panelResumenFinanciero.Controls.Add(lblTotalIngresos);

            // Total Egresos
            panelResumenFinanciero.Controls.Add(new Label
            {
                Text = "📉 Total Egresos:",
                Location = new Point(380, 15),
                Size = new Size(140, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            lblTotalEgresos = new Label
            {
                Text = "$0.00",
                Location = new Point(380, 40),
                Size = new Size(140, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 67, 54)
            };
            panelResumenFinanciero.Controls.Add(lblTotalEgresos);

            // Saldo Actual
            panelResumenFinanciero.Controls.Add(new Label
            {
                Text = "💵 Saldo Actual:",
                Location = new Point(560, 15),
                Size = new Size(140, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            lblSaldoActual = new Label
            {
                Text = "$0.00",
                Location = new Point(560, 40),
                Size = new Size(140, 25),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 152, 0)
            };
            panelResumenFinanciero.Controls.Add(lblSaldoActual);

            // Nota informativa
            var lblInfo = new Label
            {
                Text = "ℹ️ El arqueo NO cierra el turno. Es solo una verificación del estado actual de caja.",
                Location = new Point(750, 25),
                Size = new Size(590, 40),
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            panelResumenFinanciero.Controls.Add(lblInfo);

            currentY += 100;

            // ========================================
            // PANEL RESUMEN POR MEDIO DE PAGO
            // ========================================
            panelResumen = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(660, 450),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelResumen);

            panelResumen.Controls.Add(new Label
            {
                Text = "💳 RESUMEN POR MEDIO DE PAGO",
                Location = new Point(15, 15),
                Size = new Size(630, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            dgvResumenPorMedio = new DataGridView
            {
                Location = new Point(15, 50),
                Size = new Size(630, 350),
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 9F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgvResumenPorMedio.Columns.Add("MedioPago", "Medio de Pago");
            dgvResumenPorMedio.Columns.Add("Cantidad", "Cant.");
            dgvResumenPorMedio.Columns.Add("Ingresos", "Ingresos");
            dgvResumenPorMedio.Columns.Add("Egresos", "Egresos");
            dgvResumenPorMedio.Columns.Add("Neto", "Neto");

            dgvResumenPorMedio.Columns["MedioPago"].FillWeight = 30;
            dgvResumenPorMedio.Columns["Cantidad"].FillWeight = 15;
            dgvResumenPorMedio.Columns["Ingresos"].FillWeight = 20;
            dgvResumenPorMedio.Columns["Egresos"].FillWeight = 20;
            dgvResumenPorMedio.Columns["Neto"].FillWeight = 20;

            panelResumen.Controls.Add(dgvResumenPorMedio);

            // Total esperado
            panelResumen.Controls.Add(new Label
            {
                Text = "TOTAL NETO:",
                Location = new Point(15, 410),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243)
            });

            lblTotalEsperado = new Label
            {
                Text = "$0.00",
                Location = new Point(480, 410),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                TextAlign = ContentAlignment.MiddleRight
            };
            panelResumen.Controls.Add(lblTotalEsperado);

            // ========================================
            // PANEL DETALLE DE TRANSACCIONES
            // ========================================
            panelDetalle = new Panel
            {
                Location = new Point(margin + 680, currentY),
                Size = new Size(680, 450),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelDetalle);

            panelDetalle.Controls.Add(new Label
            {
                Text = "📝 DETALLE DE TRANSACCIONES",
                Location = new Point(15, 15),
                Size = new Size(650, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            dgvDetalleTransacciones = new DataGridView
            {
                Location = new Point(15, 50),
                Size = new Size(650, 300),
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 9F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgvDetalleTransacciones.Columns.Add("Fecha", "Fecha/Hora");
            dgvDetalleTransacciones.Columns.Add("NumeroFactura", "Nº Factura");
            dgvDetalleTransacciones.Columns.Add("MedioPago", "Medio Pago");
            dgvDetalleTransacciones.Columns.Add("Importe", "Importe");
            dgvDetalleTransacciones.Columns.Add("Tipo", "Tipo");

            dgvDetalleTransacciones.Columns["Fecha"].FillWeight = 25;
            dgvDetalleTransacciones.Columns["NumeroFactura"].FillWeight = 20;
            dgvDetalleTransacciones.Columns["MedioPago"].FillWeight = 20;
            dgvDetalleTransacciones.Columns["Importe"].FillWeight = 20;
            dgvDetalleTransacciones.Columns["Tipo"].FillWeight = 15;

            panelDetalle.Controls.Add(dgvDetalleTransacciones);

            // Notas
            panelDetalle.Controls.Add(new Label
            {
                Text = "📝 Notas del Arqueo:",
                Location = new Point(15, 360),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            txtNotas = new TextBox
            {
                Location = new Point(15, 390),
                Size = new Size(650, 50),
                Multiline = true,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Anotaciones sobre el arqueo (opcional)..."
            };
            panelDetalle.Controls.Add(txtNotas);

            // Botón Cerrar (abajo a la derecha)
            currentY += 470;
            btnCerrar = new Button
            {
                Text = "❌ Cerrar",
                Location = new Point(1240, currentY),
                Size = new Size(140, 40),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCerrar);
        }

        private void ConfigurarEventos()
        {
            btnCalcular.Click += async (s, e) => await CalcularArqueo();
            btnExportar.Click += (s, e) => ExportarArqueo();
            btnCerrar.Click += (s, e) => this.Close();
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
                cmbCajero.Items.Add(new { NumeroCajero = -1, Display = "-- Seleccionar Cajero --" });

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

                MessageBox.Show(
                    $"✅ Arqueo calculado exitosamente\n\n" +
                    $"Total transacciones: {dgvDetalleTransacciones.Rows.Count}\n" +
                    $"Saldo actual: {lblSaldoActual.Text}",
                    "Arqueo Completado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
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

        // ✅ REEMPLAZA el método CalcularResumenPorMedioPago COMPLETO (sin errores):

        private async Task<System.Collections.Generic.Dictionary<string, (decimal Ingresos, decimal Egresos, int CantIngresos, int CantEgresos)>>
    CalcularResumenPorMedioPago(SqlConnection connection, int numeroCajero)
        {
            var resumen = new System.Collections.Generic.Dictionary<string, (decimal Ingresos, decimal Egresos, int CantIngresos, int CantEgresos)>();

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
            // QUERY DE EGRESOS (PAGOS A PROVEEDORES)
            // ========================================
            var queryEgresos = @"
        SELECT 
            COALESCE(cpp.Metodo, 'Efectivo') as MedioPago,
            SUM(cpp.Monto) as TotalEgresos,
            COUNT(*) as CantidadEgresos
        FROM ComprasProveedoresPagos cpp
        INNER JOIN ComprasProveedores cp ON cpp.CompraId = cp.Id
        WHERE cp.Cajero = @numeroCajero
        AND cpp.Fecha BETWEEN @fechaInicio AND @fechaFin
        GROUP BY COALESCE(cpp.Metodo, 'Efectivo')";

            using (var cmd = new SqlCommand(queryEgresos, connection))
            {
                cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                cmd.Parameters.AddWithValue("@fechaInicio", dtpFechaDesde.Value);
                cmd.Parameters.AddWithValue("@fechaFin", dtpFechaHasta.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    string medioPago = reader.GetString(0);
                    decimal egresos = reader.GetDecimal(1);
                    int cantidad = reader.GetInt32(2);

                    if (!resumen.ContainsKey(medioPago))
                        resumen[medioPago] = (0m, 0m, 0, 0);

                    var actual = resumen[medioPago];
                    resumen[medioPago] = (actual.Ingresos, egresos, actual.CantIngresos, cantidad);
                }
            }

            return resumen;
        }
        // ✅ AGREGA este método al final de la clase, antes del cierre de la clase:

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
        ),
        -- Pagos a Proveedores
        TransaccionesPagosProveedores AS (
            SELECT 
                cpp.Fecha,
                'Pago #' + CAST(cpp.Id AS NVARCHAR) + ' - ' + cp.Proveedor as NumeroFactura,
                COALESCE(cpp.Metodo, 'Efectivo') as MedioPago,
                cpp.Monto as Importe,
                'Egreso (Pago Prov.)' as Tipo
            FROM ComprasProveedoresPagos cpp
            INNER JOIN ComprasProveedores cp ON cpp.CompraId = cp.Id
            WHERE cp.Cajero = @numeroCajero
            AND cpp.Fecha BETWEEN @fechaInicio AND @fechaFin
        )
        SELECT * FROM TransaccionesVentasSimples
        UNION ALL
        SELECT * FROM TransaccionesVentasMultiples
        UNION ALL
        SELECT * FROM TransaccionesPagosProveedores
        ORDER BY Fecha DESC";

            using var cmd = new SqlCommand(queryDetalle, connection);
            cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);
            cmd.Parameters.AddWithValue("@fechaInicio", dtpFechaDesde.Value);
            cmd.Parameters.AddWithValue("@fechaFin", dtpFechaHasta.Value);

            dgvDetalleTransacciones.Rows.Clear();

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

        // ✅ AGREGA también el método ExportarArqueo que está comentado:

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