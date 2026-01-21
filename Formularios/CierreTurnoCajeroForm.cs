using Comercio.NET.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class CierreTurnoCajeroForm : Form
    {
        private ComboBox cmbCajero;
        private DateTimePicker dtpFechaInicio, dtpFechaFin;
        private DataGridView dgvResumenPorMedio;
        private DataGridView dgvDetalleTransacciones;
        private Button btnCalcular, btnDeclarar, btnCerrarTurno, btnImprimir;
        private Label lblTotalEsperado, lblTotalDeclarado, lblDiferencia, lblMontoInicial;
        private TextBox txtObservaciones;
        private Panel panelResumen, panelDeclaracion;
        private bool turnoAbierto = false;
        private int turnoActualId = 0;

        // ✅ Agregar un campo para almacenar el monto inicial
        private decimal montoInicialTurno = 0m;
        private decimal totalEsperadoTurno = 0m;

        public CierreTurnoCajeroForm()
        {
            InitializeComponent();
            ConfigurarFormulario();
            _ = VerificarYCrearTablas();
            _ = CargarCajeros();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new Size(900, 510);
            this.MinimumSize = new Size(900, 510);
            this.Name = "CierreTurnoCajeroForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Cierre de Turno de Cajero";
            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            this.Text = "💰 Cierre de Turno";
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
                Text = "💰 CIERRE DE TURNO",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(margin, currentY),
                Size = new Size(400, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblTitulo);
            currentY += 35;

            // Panel de Filtros - Más ancho para acomodar DateTimePicker con hora
            var panelFiltros = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(870, 65), // Mantener altura
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelFiltros);

            // Cajero
            panelFiltros.Controls.Add(new Label
            {
                Text = "Cajero:",
                Location = new Point(10, 15),
                Size = new Size(55, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            cmbCajero = new ComboBox
            {
                Location = new Point(70, 12),
                Size = new Size(160, 22),
                Font = new Font("Segoe UI", 9F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            panelFiltros.Controls.Add(cmbCajero);

            // Fecha Inicio
            panelFiltros.Controls.Add(new Label
            {
                Text = "Desde:",
                Location = new Point(245, 15),
                Size = new Size(50, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            // En CrearControles(), cambia:
            dtpFechaInicio = new DateTimePicker
            {
                Location = new Point(300, 12),
                Size = new Size(140, 22),
                Font = new Font("Segoe UI", 8.5F),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm"
            };
            // ✅ CAMBIO: Inicializar con hora actual en lugar de 00:00
            dtpFechaInicio.Value = DateTime.Now.AddHours(-1); // Por defecto, última hora
            panelFiltros.Controls.Add(dtpFechaInicio);

            // Fecha Fin - Mover más a la derecha
            panelFiltros.Controls.Add(new Label
            {
                Text = "Hasta:",
                Location = new Point(450, 15), // Era 415, ahora 450
                Size = new Size(45, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            dtpFechaFin = new DateTimePicker
            {
                Location = new Point(500, 12), // Era 465, ahora 500
                Size = new Size(140, 22),
                Font = new Font("Segoe UI", 8.5F),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm"
            };
            dtpFechaFin.Value = DateTime.Now;
            panelFiltros.Controls.Add(dtpFechaFin);

            // Botón Calcular
            btnCalcular = new Button
            {
                Text = "📊 Calcular",
                Location = new Point(650, 10), // X=650
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnCalcular.FlatAppearance.BorderSize = 0;
            panelFiltros.Controls.Add(btnCalcular);

            // ✅ NUEVO: Agregar tooltip
            var tooltipCalcular = new ToolTip();
            tooltipCalcular.SetToolTip(btnCalcular, "Calcular el turno actual del cajero seleccionado");

            // Botón Imprimir
            btnImprimir = new Button
            {
                Text = "🖨️ Imprimir",
                Location = new Point(760, 10), // X=685 ❌ ¡Muy cerca!
                Size = new Size(90, 28),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Enabled = false
            };
            btnImprimir.FlatAppearance.BorderSize = 0;
            panelFiltros.Controls.Add(btnImprimir);

            // ✅ NUEVO: Agregar tooltip
            var tooltipImprimir = new ToolTip();
            tooltipImprimir.SetToolTip(btnImprimir, "Imprimir el cierre de turno");


            // Totales en el mismo panel - ALINEADOS
            int labelY = 42;  // ✅ Altura fija para todas las etiquetas
            int valueY = 40;  // ✅ Altura fija para todos los valores

            // ✅ NUEVO: Monto Inicial
            panelFiltros.Controls.Add(new Label
            {
                Text = "Inicial:",
                Location = new Point(10, labelY),
                Size = new Size(45, 18),
                Font = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.MiddleLeft
            });

            lblMontoInicial = new Label
            {
                Text = "$0.00",
                Location = new Point(55, valueY),
                Size = new Size(80, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(96, 125, 139),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelFiltros.Controls.Add(lblMontoInicial);

            // Esperado
            panelFiltros.Controls.Add(new Label
            {
                Text = "Esperado:",
                Location = new Point(145, labelY),
                Size = new Size(60, 18),
                Font = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.MiddleLeft
            });

            lblTotalEsperado = new Label
            {
                Text = "$0.00",
                Location = new Point(205, valueY),
                Size = new Size(80, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelFiltros.Controls.Add(lblTotalEsperado);

            // Declarado
            panelFiltros.Controls.Add(new Label
            {
                Text = "Declarado:",
                Location = new Point(295, labelY),
                Size = new Size(65, 18),
                Font = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.MiddleLeft
            });

            lblTotalDeclarado = new Label
            {
                Text = "$0.00",
                Location = new Point(360, valueY),
                Size = new Size(80, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 152, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelFiltros.Controls.Add(lblTotalDeclarado);

            // Diferencia
            panelFiltros.Controls.Add(new Label
            {
                Text = "Diferencia:",
                Location = new Point(450, labelY),
                Size = new Size(65, 18),
                Font = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.MiddleLeft
            });

            lblDiferencia = new Label
            {
                Text = "$0.00",
                Location = new Point(515, valueY),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 67, 54),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelFiltros.Controls.Add(lblDiferencia);

            currentY += 80;

            // ✅ Panel Resumen MÁS COMPACTO (reducido de 320 a 200)
            panelResumen = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(870, 200),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelResumen);

            panelResumen.Controls.Add(new Label
            {
                Text = "📊 RESUMEN POR MEDIO DE PAGO",
                Location = new Point(10, 8),
                Size = new Size(400, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            // ✅ DataGridView Resumen REDUCIDO (de 240 a 120 de altura)
            dgvResumenPorMedio = new DataGridView
            {
                Location = new Point(10, 35),
                Size = new Size(850, 120),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 8F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToResizeRows = false,
                ScrollBars = ScrollBars.Vertical
            };

            dgvResumenPorMedio.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            dgvResumenPorMedio.RowTemplate.Height = 22;

            dgvResumenPorMedio.Columns.Add("MedioPago", "Medio");
            dgvResumenPorMedio.Columns.Add("Cantidad", "Cant.");
            dgvResumenPorMedio.Columns.Add("Ingresos", "Ingresos");
            dgvResumenPorMedio.Columns.Add("Egresos", "Egresos");
            dgvResumenPorMedio.Columns.Add("Neto", "Neto");
            dgvResumenPorMedio.Columns.Add("Declarado", "Declarado");
            dgvResumenPorMedio.Columns.Add("Diferencia", "Dif.");

            dgvResumenPorMedio.Columns["MedioPago"].FillWeight = 20;
            dgvResumenPorMedio.Columns["Cantidad"].FillWeight = 10;
            dgvResumenPorMedio.Columns["Ingresos"].FillWeight = 15;
            dgvResumenPorMedio.Columns["Egresos"].FillWeight = 15;
            dgvResumenPorMedio.Columns["Neto"].FillWeight = 15;
            dgvResumenPorMedio.Columns["Declarado"].FillWeight = 15;
            dgvResumenPorMedio.Columns["Diferencia"].FillWeight = 10;

            panelResumen.Controls.Add(dgvResumenPorMedio);

            // ✅ Botones reposicionados (de Y=282 a Y=162)
            btnDeclarar = new Button
            {
                Text = "💵 Declarar",
                Location = new Point(10, 162),
                Size = new Size(110, 30),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Enabled = false
            };
            btnDeclarar.FlatAppearance.BorderSize = 0;
            panelResumen.Controls.Add(btnDeclarar);

            btnCerrarTurno = new Button
            {
                Text = "✅ Cerrar Turno",
                Location = new Point(130, 162),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Enabled = false
            };
            btnCerrarTurno.FlatAppearance.BorderSize = 0;
            panelResumen.Controls.Add(btnCerrarTurno);

            /// Observaciones reposicionadas (de Y=285 a Y=165)
            panelResumen.Controls.Add(new Label
            {
                Text = "Observaciones:",
                Location = new Point(340, 165),
                Size = new Size(90, 20),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            });

            txtObservaciones = new TextBox
            {
                Location = new Point(435, 163),
                Size = new Size(425, 28),
                Font = new Font("Segoe UI", 8F),
                PlaceholderText = "Notas del cierre..."
            };
            panelResumen.Controls.Add(txtObservaciones);

            // ✅ Panel Detalle reposicionado (reducido de currentY+335 a currentY+215)
            currentY += 215;

            // ✅ Panel Detalle AMPLIADO (de 165 a 285 de altura)
            panelDeclaracion = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(870, 160),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelDeclaracion);

            panelDeclaracion.Controls.Add(new Label
            {
                Text = "📋 DETALLE DE TRANSACCIONES",
                Location = new Point(10, 8),
                Size = new Size(400, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            // ✅ DataGridView Detalle AMPLIADO (de 120 a 240 de altura)
            dgvDetalleTransacciones = new DataGridView
            {
                Location = new Point(10, 35),
                Size = new Size(850, 120),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 8F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToResizeRows = false
            };

            dgvDetalleTransacciones.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            dgvDetalleTransacciones.RowTemplate.Height = 20;

            dgvDetalleTransacciones.Columns.Add("Fecha", "Fecha/Hora");
            dgvDetalleTransacciones.Columns.Add("NumeroFactura", "Factura");
            dgvDetalleTransacciones.Columns.Add("MedioPago", "Medio");
            dgvDetalleTransacciones.Columns.Add("Importe", "Importe");
            dgvDetalleTransacciones.Columns.Add("Tipo", "Tipo");

            dgvDetalleTransacciones.Columns["Fecha"].FillWeight = 22;
            dgvDetalleTransacciones.Columns["NumeroFactura"].FillWeight = 18;
            dgvDetalleTransacciones.Columns["MedioPago"].FillWeight = 18;
            dgvDetalleTransacciones.Columns["Importe"].FillWeight = 18;
            dgvDetalleTransacciones.Columns["Tipo"].FillWeight = 24;

            panelDeclaracion.Controls.Add(dgvDetalleTransacciones);
        }

        private void ConfigurarEventos()
        {
            btnCalcular.Click += async (s, e) => await CalcularTurno();
            btnDeclarar.Click += (s, e) => DeclarMontos();
            btnCerrarTurno.Click += async (s, e) => await CerrarTurno();
            btnImprimir.Click += (s, e) => ImprimirCierre();

            // ✅ CAMBIO: Cargar fechas del turno cuando se selecciona cajero
            cmbCajero.SelectedIndexChanged += async (s, e) =>
            {
                LimpiarFormulario();
                await CargarFechasTurnoAbierto();
            };
        }

        private async Task CargarFechasTurnoAbierto()
        {
            try
            {
                if (cmbCajero.SelectedIndex <= 0)
                {
                    dtpFechaInicio.Value = DateTime.Today;
                    dtpFechaFin.Value = DateTime.Now;
                    return;
                }

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
            SELECT TOP 1 FechaApertura
            FROM TurnosCajero 
            WHERE NumeroCajero = @numeroCajero 
            AND Estado = 'Abierto'
            ORDER BY FechaApertura DESC";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);

                var resultado = await cmd.ExecuteScalarAsync();

                if (resultado != null)
                {
                    DateTime fechaApertura = (DateTime)resultado;

                    dtpFechaInicio.Value = fechaApertura;

                    // ✅ CAMBIO: Agregar 1 minuto extra para asegurar inclusión de transacciones recientes
                    dtpFechaFin.Value = DateTime.Now.AddMinutes(1);
                }
                else
                {
                    dtpFechaInicio.Value = DateTime.Today;
                    dtpFechaFin.Value = DateTime.Now.AddMinutes(1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando fechas del turno: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task VerificarYCrearTablas()
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

                // Verificar y crear tabla TurnosCajero
                var queryVerificarTurnos = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = 'TurnosCajero'";

                using (var cmd = new SqlCommand(queryVerificarTurnos, connection))
                {
                    int existe = (int)await cmd.ExecuteScalarAsync();

                    if (existe == 0)
                    {
                        var queryCrearTurnos = @"
                            CREATE TABLE TurnosCajero (
                                Id INT IDENTITY(1,1) PRIMARY KEY,
                                NumeroCajero INT NOT NULL,
                                Usuario NVARCHAR(100) NOT NULL,
                                FechaApertura DATETIME NOT NULL,
                                FechaCierre DATETIME NULL,
                                MontoInicial DECIMAL(18,2) NOT NULL DEFAULT 0,
                                Estado NVARCHAR(20) NOT NULL DEFAULT 'Abierto',
                                Observaciones NVARCHAR(500) NULL
                            )";

                        using var cmdCrear = new SqlCommand(queryCrearTurnos, connection);
                        await cmdCrear.ExecuteNonQueryAsync();
                    }
                }

                // Verificar y crear tabla CierreTurnoCajero
                var queryVerificarCierre = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = 'CierreTurnoCajero'";

                using (var cmd = new SqlCommand(queryVerificarCierre, connection))
                {
                    int existe = (int)await cmd.ExecuteScalarAsync();

                    if (existe == 0)
                    {
                        var queryCrearCierre = @"
                            CREATE TABLE CierreTurnoCajero (
                                Id INT IDENTITY(1,1) PRIMARY KEY,
                                IdTurno INT NULL,
                                MedioPago NVARCHAR(50) NOT NULL,
                                TotalEsperado DECIMAL(18,2) NOT NULL,
                                TotalDeclarado DECIMAL(18,2) NULL,
                                Diferencia DECIMAL(18,2) NULL,
                                CantidadTransacciones INT NOT NULL,
                                FechaCierre DATETIME NOT NULL,
                                UsuarioCierre NVARCHAR(100) NOT NULL
                            )";

                        using var cmdCrear = new SqlCommand(queryCrearCierre, connection);
                        await cmdCrear.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verificando tablas: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                cmbCajero.Items.Add(new { NumeroCajero = -1, Display = "-- Seleccionar --" });

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
                        "Para realizar un cierre de turno, primero debe abrir un turno desde el módulo de Apertura de Turno.",
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

        private async Task CalcularTurno()
        {
            try
            {
                if (cmbCajero.SelectedIndex <= 0)
                {
                    MessageBox.Show("Debe seleccionar un cajero", "Validación",
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

                // ========================================
                // ✅ VALIDACIÓN: VERIFICAR SI HAY TURNO ABIERTO
                // ========================================
                var idTurnoAbierto = await ObtenerTurnoAbiertoId(numeroCajero);

                if (!idTurnoAbierto.HasValue)
                {
                    // ❌ NO HAY TURNO ABIERTO
                    MessageBox.Show(
                        "⚠️ No hay un turno abierto para este cajero.\n\n" +
                        "Para realizar un cierre de turno, primero debe abrir un turno desde el módulo de Apertura de Turno.",
                        "Sin Turno Abierto",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    btnCalcular.Text = "📊 Calcular";
                    btnCalcular.Enabled = true;

                    // ✅ Limpiar formulario
                    LimpiarFormulario();
                    return;
                }

                // ✅ Guardar el ID del turno abierto
                turnoActualId = idTurnoAbierto.Value;

                // ========================================
                // 1. OBTENER MONTO INICIAL
                // ========================================
                montoInicialTurno = await ObtenerMontoInicialTurno(connectionString, numeroCajero);
                lblMontoInicial.Text = montoInicialTurno.ToString("C2");

                // ========================================
                // 2. CALCULAR RESUMEN POR MEDIO DE PAGO
                // ========================================
                var resumenPorMedio = new Dictionary<string, (decimal Ingresos, decimal Egresos, int CantIngresos, int CantEgresos)>();

                // ========================================
                // QUERY DE INGRESOS (VENTAS)
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
                    cmd.Parameters.AddWithValue("@fechaInicio", dtpFechaInicio.Value);
                    cmd.Parameters.AddWithValue("@fechaFin", dtpFechaFin.Value);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        string medioPago = reader.GetString(0);
                        decimal ingresos = reader.GetDecimal(1);
                        int cantidad = reader.GetInt32(2);

                        if (!resumenPorMedio.ContainsKey(medioPago))
                            resumenPorMedio[medioPago] = (0m, 0m, 0, 0);

                        var actual = resumenPorMedio[medioPago];
                        resumenPorMedio[medioPago] = (ingresos, actual.Egresos, cantidad, actual.CantEgresos);
                    }
                }

                // ========================================
                // QUERY DE EGRESOS (PAGOS A PROVEEDORES)
                // ========================================
                var queryPagosRapidos = @"
    SELECT 
        SUM(Monto) AS TotalEgresos,
        COUNT(*) AS CantidadEgresos
    FROM PagosProveedores
    WHERE NumeroCajero = @numeroCajero
    AND FechaPago BETWEEN @fechaInicio AND @fechaFin";

                using (var cmd = new SqlCommand(queryPagosRapidos, connection))
                {
                    cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                    cmd.Parameters.AddWithValue("@fechaInicio", dtpFechaInicio.Value);
                    cmd.Parameters.AddWithValue("@fechaFin", dtpFechaFin.Value);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (reader.Read() && reader["TotalEgresos"] != DBNull.Value)
                    {
                        string medioPago = "Efectivo";  // ✅ SIEMPRE Efectivo
                        decimal egresos = reader.GetDecimal(0);
                        int cantidad = reader.GetInt32(1);

                        if (!resumenPorMedio.ContainsKey(medioPago))
                            resumenPorMedio[medioPago] = (0m, 0m, 0, 0);

                        var actual = resumenPorMedio[medioPago];

                        // ✅ SUMAR a los egresos existentes
                        resumenPorMedio[medioPago] = (
                            actual.Ingresos,
                            actual.Egresos + egresos,  // ✅ SUMAR
                            actual.CantIngresos,
                            actual.CantEgresos + cantidad  // ✅ SUMAR
                        );

                        System.Diagnostics.Debug.WriteLine(
                            $"💳 PAGOS A PROVEEDORES SUMADOS:\n" +
                            $"   Total egresos: {egresos:C2}\n" +
                            $"   Cantidad: {cantidad}\n" +
                            $"   Egresos acumulados en Efectivo: {resumenPorMedio[medioPago].Egresos:C2}");
                    }
                }

                // ========================================
                // ✅ NUEVO: QUERY DE RETIROS DE EFECTIVO
                // ========================================
                var queryRetiros = @"
            SELECT 
                SUM(Monto) as TotalRetiros,
                COUNT(*) as CantidadRetiros
            FROM RetirosEfectivo
            WHERE NumeroCajero = @numeroCajero
            AND FechaRetiro BETWEEN @fechaInicio AND @fechaFin";

                using (var cmd = new SqlCommand(queryRetiros, connection))
                {
                    cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                    cmd.Parameters.AddWithValue("@fechaInicio", dtpFechaInicio.Value);
                    cmd.Parameters.AddWithValue("@fechaFin", dtpFechaFin.Value);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (reader.Read())
                    {
                        decimal totalRetiros = reader["TotalRetiros"] != DBNull.Value
                            ? reader.GetDecimal(0)
                            : 0m;
                        int cantidadRetiros = reader["CantidadRetiros"] != DBNull.Value
                            ? reader.GetInt32(1)
                            : 0;

                        // ✅ Registrar retiros como EGRESO de EFECTIVO
                        if (totalRetiros > 0)
                        {
                            string medioPago = "Efectivo";

                            if (!resumenPorMedio.ContainsKey(medioPago))
                                resumenPorMedio[medioPago] = (0m, 0m, 0, 0);

                            var actual = resumenPorMedio[medioPago];
                            resumenPorMedio[medioPago] = (
                                actual.Ingresos,
                                actual.Egresos + totalRetiros,  // ✅ SUMAR retiros a egresos
                                actual.CantIngresos,
                                actual.CantEgresos + cantidadRetiros
                            );

                            System.Diagnostics.Debug.WriteLine(
                                $"💰 RETIROS REGISTRADOS EN CIERRE:\n" +
                                $"   Total: {totalRetiros:C2}\n" +
                                $"   Cantidad: {cantidadRetiros}\n" +
                                $"   Medio: {medioPago}");
                        }
                    }
                }

                // ========================================
                // 3. LLENAR GRILLA CON EL RESUMEN
                // ========================================
                dgvResumenPorMedio.Rows.Clear();
                decimal totalEsperado = montoInicialTurno; // Partir del monto inicial

                // ✅ CAMBIO: Ordenar para que Efectivo aparezca primero
                var medioPagosOrdenados = resumenPorMedio
                    .OrderByDescending(x => x.Key.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                    .ThenBy(x => x.Key);

                foreach (var kvp in medioPagosOrdenados)
                {
                    string medioPago = kvp.Key;
                    decimal ingresos = kvp.Value.Ingresos;
                    decimal egresos = kvp.Value.Egresos;
                    decimal neto = ingresos - egresos;

                    dgvResumenPorMedio.Rows.Add(
                        medioPago,
                        kvp.Value.CantIngresos + kvp.Value.CantEgresos,
                        ingresos.ToString("C2"),
                        egresos.ToString("C2"),
                        neto.ToString("C2"),
                        "0.00" // Declarado (inicialmente en 0)
                    );

                    // Solo sumar al total esperado si es efectivo
                    if (medioPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                    {
                        totalEsperado += neto;
                    }

                    // Colorear egresos en rojo
                    int rowIndex = dgvResumenPorMedio.Rows.Count - 1;
                    if (egresos > 0)
                    {
                        dgvResumenPorMedio.Rows[rowIndex].Cells["Egresos"].Style.ForeColor = Color.Red;
                        dgvResumenPorMedio.Rows[rowIndex].Cells["Egresos"].Style.Font = new Font(dgvResumenPorMedio.Font, FontStyle.Bold);
                    }
                }

                // ✅ GUARDAR el valor calculado en la variable de clase
                totalEsperadoTurno = totalEsperado;

                // ✅ CORREGIDO: Solo mostrar valores sin texto descriptivo
                lblTotalEsperado.Text = totalEsperado.ToString("C2");
                lblTotalDeclarado.Text = "$0.00";
                lblDiferencia.Text = "$0.00";

                // Cargar detalle de transacciones
                await CargarDetalleTransacciones(connectionString, numeroCajero);

                // Habilitar botones
                btnDeclarar.Enabled = true;
                btnCerrarTurno.Enabled = false;
                btnImprimir.Enabled = false;

                btnCalcular.Text = "🔄 Recalcular";
                btnCalcular.Enabled = true;

                turnoAbierto = true;

                //MessageBox.Show(
                //    $"✅ Turno calculado exitosamente\n\n" +
                //    $"Monto Inicial: {montoInicialTurno:C2}\n" +
                //    $"Total Esperado en Efectivo: {totalEsperado:C2}\n" +
                //    $"Transacciones: {dgvDetalleTransacciones.Rows.Count}",
                //    "Cálculo Completado",
                //    MessageBoxButtons.OK,
                //    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al calcular turno: {ex.Message}\n\n{ex.StackTrace}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnCalcular.Text = "📊 Calcular";
                btnCalcular.Enabled = true;
            }
        }

        // ✅ NUEVO MÉTODO: Obtener el monto inicial del turno abierto
        private async Task<decimal> ObtenerMontoInicialTurno(string connectionString, int numeroCajero)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

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
            catch
            {
                return 0m;
            }
        }

        private async Task CargarDetalleTransacciones(string connectionString, int numeroCajero)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                var queryCombinada = @"
            WITH 
            -- ✅ Ventas/Remitos - CORREGIDO: Convertir todo a NVARCHAR
            TransaccionesVentas AS (
                SELECT 
                    v.fecha AS Fecha,
                    CAST(v.NroFactura AS NVARCHAR) AS NumeroFactura,  -- ✅ CONVERTIR a NVARCHAR
                    COALESCE(f.FormadePago, 'Efectivo') AS MedioPago,
                    f.ImporteTotal AS Importe,
                    CASE 
                        WHEN f.TipoFactura IS NULL OR f.TipoFactura = '' THEN 'Ingreso (Remito)'
                        ELSE 'Ingreso (Factura)'
                    END AS Tipo
                FROM Ventas v
                LEFT JOIN Facturas f ON v.NroFactura = f.NumeroRemito
                WHERE v.fecha BETWEEN @fechaInicio AND @fechaFin
                GROUP BY v.fecha, v.NroFactura, f.FormadePago, f.ImporteTotal, f.TipoFactura
            ),
            
            -- ✅ CORREGIDO: Pagos a Proveedores SIN columna 'Metodo'
            TransaccionesPagosProveedores AS (
                SELECT 
                    pp.FechaPago AS Fecha,
                    'Pago #' + CAST(pp.Id AS NVARCHAR) + ' - ' + pp.Proveedor AS NumeroFactura,
                    'Efectivo' AS MedioPago,  -- ✅ HARDCODED
                    pp.Monto AS Importe,
                    'Egreso (Pago Prov.)' AS Tipo
                FROM PagosProveedores pp
                WHERE pp.NumeroCajero = @numeroCajero
                AND pp.FechaPago BETWEEN @fechaInicio AND @fechaFin
            ),
            
            -- Retiros de efectivo
            TransaccionesRetiros AS (
                SELECT 
                    r.FechaRetiro AS Fecha,
                    'Retiro #' + CAST(r.Id AS NVARCHAR) + ' - ' + r.Motivo AS NumeroFactura,
                    'Efectivo' AS MedioPago,
                    r.Monto AS Importe,
                    'Egreso (Retiro)' AS Tipo
                FROM RetirosEfectivo r
                WHERE r.NumeroCajero = @numeroCajero
                AND r.FechaRetiro BETWEEN @fechaInicio AND @fechaFin
            )
            
            -- UNION de todas las transacciones
            SELECT * FROM TransaccionesVentas
            UNION ALL
            SELECT * FROM TransaccionesPagosProveedores
            UNION ALL
            SELECT * FROM TransaccionesRetiros
            ORDER BY Fecha ASC";

                using (var cmd = new SqlCommand(queryCombinada, connection))
                {
                    cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                    cmd.Parameters.AddWithValue("@fechaInicio", dtpFechaInicio.Value);
                    cmd.Parameters.AddWithValue("@fechaFin", dtpFechaFin.Value);

                    using var reader = await cmd.ExecuteReaderAsync();

                    dgvDetalleTransacciones.Rows.Clear();

                    while (await reader.ReadAsync())
                    {
                        string fecha = reader["Fecha"] != DBNull.Value
                            ? Convert.ToDateTime(reader["Fecha"]).ToString("dd/MM/yyyy HH:mm")
                            : "";
                        string numero = reader["NumeroFactura"]?.ToString() ?? "";
                        string medio = reader["MedioPago"]?.ToString() ?? "Sin especificar";
                        string importe = reader["Importe"] != DBNull.Value
                            ? Convert.ToDecimal(reader["Importe"]).ToString("C2")
                            : "$0,00";
                        string tipo = reader["Tipo"]?.ToString() ?? "";

                        dgvDetalleTransacciones.Rows.Add(fecha, numero, medio, importe, tipo);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Detalle de transacciones cargado: {dgvDetalleTransacciones.Rows.Count} registros");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando detalle transacciones: {ex.Message}");
                throw;
            }
        }

        private void DeclarMontos()
        {
            // ✅ PASAR el monto inicial al formulario
            using var formDeclaracion = new DeclaracionMontosForm(dgvResumenPorMedio, montoInicialTurno);
            if (formDeclaracion.ShowDialog() == DialogResult.OK)
            {
                decimal totalDeclaradoEfectivo = 0; // ✅ Solo efectivo
                decimal totalEsperado = totalEsperadoTurno;

                foreach (DataGridViewRow row in dgvResumenPorMedio.Rows)
                {
                    if (row.IsNewRow) continue;

                    string medioPago = row.Cells["MedioPago"].Value.ToString();
                    string declaradoStr = row.Cells["Declarado"].Value?.ToString() ?? "$0.00";
                    decimal declarado = decimal.Parse(declaradoStr, NumberStyles.Currency, CultureInfo.CurrentCulture);

                    string netoStr = row.Cells["Neto"].Value.ToString();
                    decimal neto = decimal.Parse(netoStr, NumberStyles.Currency, CultureInfo.CurrentCulture);

                    // ✅ CORREGIDO: Para efectivo, sumar el monto inicial al neto esperado
                    decimal esperadoReal = neto;
                    if (medioPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                    {
                        esperadoReal += montoInicialTurno;
                    }

                    decimal diferencia = declarado - esperadoReal;

                    row.Cells["Diferencia"].Value = diferencia.ToString("C2");

                    if (diferencia != 0)
                    {
                        row.Cells["Diferencia"].Style.ForeColor = diferencia > 0 ? Color.Green : Color.Red;
                    }

                    // ✅ CORREGIDO: Solo sumar el efectivo declarado
                    if (medioPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                    {
                        totalDeclaradoEfectivo = declarado;
                    }
                }

                // ✅ Mostrar solo el total de efectivo declarado
                lblTotalDeclarado.Text = totalDeclaradoEfectivo.ToString("C2");

                // ✅ Calcular diferencia solo con efectivo
                decimal diferenciaTotal = totalDeclaradoEfectivo - totalEsperado;
                lblDiferencia.Text = diferenciaTotal.ToString("C2");

                // ✅ CORREGIDO: Verde = Sobra (positivo), Rojo = Falta (negativo)
                if (diferenciaTotal > 0)
                {
                    lblDiferencia.ForeColor = Color.Green; // Sobra dinero
                }
                else if (diferenciaTotal < 0)
                {
                    lblDiferencia.ForeColor = Color.Red; // Falta dinero
                }
                else
                {
                    lblDiferencia.ForeColor = Color.FromArgb(96, 125, 139); // Exacto (gris)
                }

                btnCerrarTurno.Enabled = true;
            }
        }


        private async Task CerrarTurno()
        {
            try
            {
                var resultado = MessageBox.Show(
                    "¿Está seguro de cerrar el turno?",
                    "Confirmar",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resultado != DialogResult.Yes) return;

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                dynamic cajeroSeleccionado = cmbCajero.SelectedItem;
                int numeroCajero = cajeroSeleccionado.NumeroCajero;

                string usuarioCierre = AuthenticationService.SesionActual?.Usuario?.NombreUsuario ?? "Sistema";
                int idTurno;

                // ✅ DIAGNÓSTICO: Mostrar estado antes de cerrar
                System.Diagnostics.Debug.WriteLine($"🔍 CERRANDO TURNO - turnoActualId: {turnoActualId}");

                if (turnoActualId > 0)
                {
                    // ✅ CASO 1: Actualizar turno existente
                    var queryActualizar = @"
                UPDATE TurnosCajero 
                SET FechaCierre = @fechaCierre, 
                    Estado = 'Cerrado',
                    Observaciones = COALESCE(Observaciones, '') + CHAR(13) + CHAR(10) + @obs
                WHERE Id = @idTurno";

                    using (var cmdActualizar = new SqlCommand(queryActualizar, connection))
                    {
                        cmdActualizar.Parameters.AddWithValue("@idTurno", turnoActualId);
                        cmdActualizar.Parameters.AddWithValue("@fechaCierre", DateTime.Now);
                        cmdActualizar.Parameters.AddWithValue("@obs", txtObservaciones.Text ?? "");

                        int rowsAffected = await cmdActualizar.ExecuteNonQueryAsync();

                        System.Diagnostics.Debug.WriteLine($"✅ Turno actualizado. Rows affected: {rowsAffected}");
                    }

                    idTurno = turnoActualId;
                }
                else
                {
                    // ✅ CASO 2: Intentar obtener el ID del turno abierto
                    var idTurnoAbierto = await ObtenerTurnoAbiertoId(numeroCajero);

                    if (idTurnoAbierto.HasValue)
                    {
                        // Actualizar el turno abierto encontrado
                        var queryActualizar = @"
                UPDATE TurnosCajero 
                SET FechaCierre = @fechaCierre, 
                    Estado = 'Cerrado',
                    Observaciones = COALESCE(Observaciones, '') + CHAR(13) + CHAR(10) + @obs
                WHERE Id = @idTurno";

                        using (var cmdActualizar = new SqlCommand(queryActualizar, connection))
                        {
                            cmdActualizar.Parameters.AddWithValue("@idTurno", idTurnoAbierto.Value);
                            cmdActualizar.Parameters.AddWithValue("@fechaCierre", DateTime.Now);
                            cmdActualizar.Parameters.AddWithValue("@obs", txtObservaciones.Text ?? "");

                            int rowsAffected = await cmdActualizar.ExecuteNonQueryAsync();

                            System.Diagnostics.Debug.WriteLine($"✅ Turno abierto encontrado y actualizado. ID: {idTurnoAbierto.Value}, Rows: {rowsAffected}");
                        }

                        idTurno = idTurnoAbierto.Value;
                    }
                    else
                    {
                        // ⚠️ CASO 3: No hay turno abierto, crear uno cerrado (no recomendado)
                        MessageBox.Show(
                            "⚠️ No se encontró un turno abierto para este cajero.\n\n" +
                            "Esto no debería ocurrir. Por favor, verifique que el cajero tenga un turno abierto.",
                            "Advertencia",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }

                // Guardar detalles del cierre
                foreach (DataGridViewRow row in dgvResumenPorMedio.Rows)
                {
                    if (row.IsNewRow) continue;

                    string medioPago = row.Cells["MedioPago"].Value.ToString();
                    int cantidad = int.Parse(row.Cells["Cantidad"].Value.ToString());
                    decimal esperado = decimal.Parse(row.Cells["Neto"].Value.ToString(), NumberStyles.Currency, CultureInfo.CurrentCulture);
                    decimal declarado = decimal.Parse(row.Cells["Declarado"].Value.ToString(), NumberStyles.Currency, CultureInfo.CurrentCulture);
                    decimal diferencia = decimal.Parse(row.Cells["Diferencia"].Value.ToString(), NumberStyles.Currency, CultureInfo.CurrentCulture);

                    var queryCierre = @"
                INSERT INTO CierreTurnoCajero 
                (IdTurno, MedioPago, TotalEsperado, TotalDeclarado, Diferencia, CantidadTransacciones, FechaCierre, UsuarioCierre)
                VALUES 
                (@idTurno, @medioPago, @esperado, @declarado, @diferencia, @cantidad, @fechaCierre, @usuarioCierre)";

                    using var cmdCierre = new SqlCommand(queryCierre, connection);
                    cmdCierre.Parameters.AddWithValue("@idTurno", idTurno);
                    cmdCierre.Parameters.AddWithValue("@medioPago", medioPago);
                    cmdCierre.Parameters.AddWithValue("@esperado", esperado);
                    cmdCierre.Parameters.AddWithValue("@declarado", declarado);
                    cmdCierre.Parameters.AddWithValue("@diferencia", diferencia);
                    cmdCierre.Parameters.AddWithValue("@cantidad", cantidad);
                    cmdCierre.Parameters.AddWithValue("@fechaCierre", DateTime.Now);
                    cmdCierre.Parameters.AddWithValue("@usuarioCierre", usuarioCierre);

                    await cmdCierre.ExecuteNonQueryAsync();
                }

                MessageBox.Show($"✅ Turno #{idTurno} cerrado exitosamente", "Éxito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                turnoActualId = 0;
                LimpiarFormulario();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImprimirCierre()
        {
            MessageBox.Show("Función en desarrollo", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LimpiarFormulario()
        {
            dgvResumenPorMedio.Rows.Clear();
            dgvDetalleTransacciones.Rows.Clear();
            lblMontoInicial.Text = "$0.00";
            lblTotalEsperado.Text = "$0.00";
            lblTotalDeclarado.Text = "$0.00";
            lblDiferencia.Text = "$0.00";
            lblDiferencia.ForeColor = Color.FromArgb(244, 67, 54); // ✅ Resetear color
            txtObservaciones.Clear();
            btnDeclarar.Enabled = false;
            btnCerrarTurno.Enabled = false;
            btnImprimir.Enabled = false;

            // ✅ Resetear botón Calcular
            btnCalcular.Text = "📊 Calcular";
            btnCalcular.Enabled = true;

            // ✅ RESETEAR las variables de clase
            montoInicialTurno = 0m;
            totalEsperadoTurno = 0m;
            turnoActualId = 0; // ✅ Importante: resetear el ID del turno
            turnoAbierto = false; // ✅ Importante: resetear el flag
        }

        private async Task<bool> VerificarTurnoCerrado(int numeroCajero, DateTime fechaInicio, DateTime fechaFin)
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

                // 1. Obtener el último cierre del cajero
                var queryUltimoCierre = @"
            SELECT TOP 1 FechaCierre
            FROM TurnosCajero 
            WHERE NumeroCajero = @numeroCajero 
            AND Estado = 'Cerrado'
            AND FechaCierre IS NOT NULL
            ORDER BY FechaCierre DESC";

                using (var cmd = new SqlCommand(queryUltimoCierre, connection))
                {
                    cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                    var resultado = await cmd.ExecuteScalarAsync();

                    // 2. Validar que la fecha de inicio sea posterior al último cierre
                    if (resultado != null)
                    {
                        DateTime ultimoCierre = (DateTime)resultado;

                        if (fechaInicio <= ultimoCierre)
                        {
                            MessageBox.Show(
                                $"⚠️ El período seleccionado es anterior o igual al último cierre.\n\n" +
                                $"Último cierre: {ultimoCierre:dd/MM/yyyy HH:mm}\n" +
                                $"Fecha inicio seleccionada: {fechaInicio:dd/MM/yyyy HH:mm}\n\n" +
                                $"Debe seleccionar un período posterior al último cierre.",
                                "Período Inválido",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return true;
                        }
                    }
                }

                // 3. Validar que no haya solapamiento con turnos cerrados
                var querySolapamiento = @"
            SELECT COUNT(*) 
            FROM TurnosCajero 
            WHERE NumeroCajero = @numeroCajero 
            AND Estado = 'Cerrado'
            AND (
                -- El nuevo período está completamente dentro de un turno cerrado
                (FechaApertura <= @fechaInicio AND FechaCierre >= @fechaFin)
                OR
                -- El nuevo período comienza dentro de un turno cerrado
                (@fechaInicio >= FechaApertura AND @fechaInicio < FechaCierre)
                OR
                -- El nuevo período termina dentro de un turno cerrado
                (@fechaFin > FechaApertura AND @fechaFin <= FechaCierre)
                OR
                -- El nuevo período envuelve completamente un turno cerrado
                (@fechaInicio <= FechaApertura AND @fechaFin >= FechaCierre)
            )";

                using (var cmd = new SqlCommand(querySolapamiento, connection))
                {
                    cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                    cmd.Parameters.AddWithValue("@fechaInicio", fechaInicio);
                    cmd.Parameters.AddWithValue("@fechaFin", fechaFin);

                    int count = (int)await cmd.ExecuteScalarAsync();

                    if (count > 0)
                    {
                        MessageBox.Show(
                            $"⚠️ El período seleccionado se solapa con un turno ya cerrado.\n\n" +
                            $"Período seleccionado:\n" +
                            $"Desde: {fechaInicio:dd/MM/yyyy HH:mm}\n" +
                            $"Hasta: {fechaFin:dd/MM/yyyy HH:mm}\n\n" +
                            $"No puede haber solapamiento entre turnos.",
                            "Turno Solapado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verificando turno: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void MostrarHistorialCierres(int numeroCajero)
        {
            try
            {
                using var formHistorial = new HistorialCierresForm();
                formHistorial.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<int?> ObtenerTurnoAbiertoId(int numeroCajero)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // ✅ MEJORADO: Consulta más robusta con debugging completo
                var query = @"
            SELECT 
                Id,
                NumeroCajero,
                Usuario,
                Estado,
                '[' + Estado + ']' AS EstadoConCorchetes,
                LEN(Estado) AS LongitudEstado,
                FechaApertura
            FROM TurnosCajero 
            WHERE NumeroCajero = @numeroCajero 
            AND LTRIM(RTRIM(UPPER(Estado))) = 'ABIERTO'
            ORDER BY FechaApertura DESC";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);

                int? turnoId = null;
                string debugInfo = "";

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        int cajero = reader.GetInt32(1);
                        string usuario = reader.GetString(2);
                        string estado = reader.GetString(3);
                        string estadoCorchetes = reader.GetString(4);
                        int longitudEstado = reader.GetInt32(5);
                        DateTime fechaApertura = reader.GetDateTime(6);

                        // ✅ GUARDAR información para debug
                        debugInfo =
                            $"✅ TURNO ABIERTO ENCONTRADO:\n" +
                            $"   ID: {id}\n" +
                            $"   Cajero: #{cajero}\n" +
                            $"   Usuario: {usuario}\n" +
                            $"   Estado: '{estado}'\n" +
                            $"   Estado con corchetes: {estadoCorchetes}\n" +
                            $"   Longitud Estado: {longitudEstado}\n" +
                            $"   Fecha Apertura: {fechaApertura:dd/MM/yyyy HH:mm:ss}";

                        turnoId = id;
                    }
                } // ✅ CRÍTICO: Cerrar el primer reader ANTES de ejecutar la consulta de diagnóstico

                // ✅ Si se encontró turno, mostrar info y retornar
                if (turnoId.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine(debugInfo);
                    return turnoId;
                }

                // ❌ NO se encontró turno abierto - AHORA ejecutar diagnóstico
                System.Diagnostics.Debug.WriteLine(
                    $"❌ NO se encontró turno abierto:\n" +
                    $"   Cajero buscado: #{numeroCajero}");

                // ✅ NUEVA CONSULTA: Verificar TODOS los turnos de este cajero
                var queryDiagnostico = @"
            SELECT 
                Id,
                NumeroCajero,
                Usuario,
                Estado,
                '[' + Estado + ']' AS EstadoConCorchetes,
                LEN(Estado) AS LongitudEstado,
                FechaApertura,
                FechaCierre
            FROM TurnosCajero 
            WHERE NumeroCajero = @numeroCajero
            ORDER BY FechaApertura DESC";

                using var cmdDiag = new SqlCommand(queryDiagnostico, connection);
                cmdDiag.Parameters.AddWithValue("@numeroCajero", numeroCajero);

                using (var readerDiag = await cmdDiag.ExecuteReaderAsync())
                {
                    System.Diagnostics.Debug.WriteLine($"\n📋 TODOS LOS TURNOS DEL CAJERO #{numeroCajero}:");
                    int count = 0;

                    while (await readerDiag.ReadAsync())
                    {
                        count++;
                        int id = readerDiag.GetInt32(0);
                        int cajeroNum = readerDiag.GetInt32(1);
                        string usuario = readerDiag.GetString(2);
                        string estado = readerDiag.GetString(3);
                        string estadoCorchetes = readerDiag.GetString(4);
                        int longitudEstado = readerDiag.GetInt32(5);
                        DateTime fechaApertura = readerDiag.GetDateTime(6);
                        string fechaCierre = readerDiag["FechaCierre"] != DBNull.Value
                            ? readerDiag.GetDateTime(7).ToString("dd/MM/yyyy HH:mm:ss")
                            : "NULL";

                        System.Diagnostics.Debug.WriteLine(
                            $"   Turno {count}:\n" +
                            $"      ID: {id}\n" +
                            $"      Cajero: #{cajeroNum}\n" +
                            $"      Usuario: {usuario}\n" +
                            $"      Estado: '{estado}'\n" +
                            $"      Estado con corchetes: {estadoCorchetes}\n" +
                            $"      Longitud: {longitudEstado} caracteres\n" +
                            $"      Apertura: {fechaApertura:dd/MM/yyyy HH:mm:ss}\n" +
                            $"      Cierre: {fechaCierre}\n");
                    }

                    if (count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"   ⚠️ Este cajero NO tiene NINGÚN turno registrado");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"\n💡 DIAGNÓSTICO:\n" +
                            $"   - Se encontraron {count} turno(s) para el cajero #{numeroCajero}\n" +
                            $"   - Pero NINGUNO tiene Estado = 'ABIERTO' (normalizado)\n" +
                            $"   - Revise los valores de Estado arriba para identificar el problema");
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                // ✅ CRÍTICO: Mostrar el error completo
                System.Diagnostics.Debug.WriteLine(
                    $"❌ ERROR en ObtenerTurnoAbiertoId:\n" +
                    $"   Mensaje: {ex.Message}\n" +
                    $"   StackTrace: {ex.StackTrace}");

                MessageBox.Show(
                    $"Error al verificar el turno:\n\n{ex.Message}",
                    "Error de Base de Datos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return null;
            }
        }

        public class DeclaracionMontosForm : Form
        {
            private DataGridView dgvDeclaracion;
            private Button btnGuardar, btnCancelar;
            private DataGridView dgvReferencia;
            private decimal montoInicial; // ✅ NUEVO

            public DeclaracionMontosForm(DataGridView dgvReferencia, decimal montoInicial) // ✅ PARÁMETRO NUEVO
            {
                this.dgvReferencia = dgvReferencia;
                this.montoInicial = montoInicial; // ✅ GUARDAR
                InitializeComponent();
                CrearControles();
            }

            private void InitializeComponent()
            {
                this.ClientSize = new Size(600, 250);
                this.Text = "💵 Declarar Montos";
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
            }

            private void CrearControles()
            {
                this.Controls.Add(new Label
                {
                    Text = "Ingrese el monto real por cada medio:",
                    Location = new Point(15, 15),
                    Size = new Size(570, 25),
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = Color.FromArgb(63, 81, 181)
                });

                dgvDeclaracion = new DataGridView
                {
                    Location = new Point(15, 45),
                    Size = new Size(570, 150),
                    BackgroundColor = Color.White,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    SelectionMode = DataGridViewSelectionMode.CellSelect,
                    Font = new Font("Segoe UI", 9F),
                    RowHeadersVisible = false,
                    AllowUserToResizeRows = false,
                    ScrollBars = ScrollBars.Vertical
                };

                dgvDeclaracion.Columns.Add("MedioPago", "Medio");
                dgvDeclaracion.Columns.Add("Esperado", "Esperado");

                var colDeclarado = new DataGridViewTextBoxColumn
                {
                    Name = "Declarado",
                    HeaderText = "Declarado",
                    ValueType = typeof(string)
                };
                dgvDeclaracion.Columns.Add(colDeclarado);

                var colCheck = new DataGridViewCheckBoxColumn
                {
                    Name = "AutoCopiar",
                    HeaderText = "✓ Auto",
                    Width = 70,
                    ReadOnly = false
                };
                dgvDeclaracion.Columns.Add(colCheck);

                dgvDeclaracion.Columns["AutoCopiar"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvDeclaracion.Columns["AutoCopiar"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                dgvDeclaracion.Columns["MedioPago"].ReadOnly = true;
                dgvDeclaracion.Columns["Esperado"].ReadOnly = true;
                dgvDeclaracion.Columns["MedioPago"].Width = 140;
                dgvDeclaracion.Columns["Esperado"].Width = 140;
                dgvDeclaracion.Columns["Declarado"].Width = 150;

                dgvDeclaracion.RowTemplate.Height = 35;

                // Eventos
                dgvDeclaracion.CellContentClick += DgvDeclaracion_CellContentClick;
                dgvDeclaracion.EditingControlShowing += DgvDeclaracion_EditingControlShowing;
                dgvDeclaracion.CellEndEdit += DgvDeclaracion_CellEndEdit;
                dgvDeclaracion.CellBeginEdit += DgvDeclaracion_CellBeginEdit;

                var filasOrdenadas = dgvReferencia.Rows.Cast<DataGridViewRow>()
                    .Where(row => !row.IsNewRow)
                    .OrderByDescending(row => row.Cells["MedioPago"].Value?.ToString()?.Equals("Efectivo", StringComparison.OrdinalIgnoreCase) ?? false)
                    .ThenBy(row => row.Cells["MedioPago"].Value?.ToString());

                foreach (DataGridViewRow row in filasOrdenadas)
                {
                    string medioPago = row.Cells["MedioPago"].Value.ToString();
                    decimal neto = decimal.Parse(row.Cells["Neto"].Value.ToString(), NumberStyles.Currency, CultureInfo.CurrentCulture);

                    // ✅ SUMAR monto inicial solo al efectivo
                    decimal esperado = neto;
                    if (medioPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                    {
                        esperado += montoInicial;
                    }

                    // ✅ CAMBIO: Precargar valores automáticamente para medios diferentes a Efectivo
                    bool esEfectivo = medioPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase);
                    string valorDeclarado = esEfectivo ? "$0,00" : esperado.ToString("C2");
                    bool autoCopiado = !esEfectivo; // ✅ Marcar checkbox para no-efectivo

                    dgvDeclaracion.Rows.Add(
                        medioPago,
                        esperado.ToString("C2"),
                        valorDeclarado, // ✅ Precargar si no es efectivo
                        autoCopiado // ✅ Marcar checkbox si no es efectivo
                    );

                    // ✅ NUEVO: Colorear la fila de Efectivo para destacarla
                    int rowIndex = dgvDeclaracion.Rows.Count - 1;
                    if (esEfectivo)
                    {
                        dgvDeclaracion.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 252, 231); // Amarillo claro
                        dgvDeclaracion.Rows[rowIndex].DefaultCellStyle.Font = new Font(dgvDeclaracion.Font, FontStyle.Bold);
                    }
                }

                this.Controls.Add(dgvDeclaracion);

                btnGuardar = new Button
                {
                    Text = "💾 Guardar",
                    Location = new Point(370, 205),
                    Size = new Size(100, 32),
                    BackColor = Color.FromArgb(76, 175, 80),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };
                btnGuardar.FlatAppearance.BorderSize = 0;
                btnGuardar.Click += (s, e) =>
                {
                    GuardarDeclaracion();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                this.Controls.Add(btnGuardar);

                btnCancelar = new Button
                {
                    Text = "❌ Cancelar",
                    Location = new Point(485, 205),
                    Size = new Size(100, 32),
                    BackColor = Color.FromArgb(158, 158, 158),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };
                btnCancelar.FlatAppearance.BorderSize = 0;
                btnCancelar.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancelar);
            }

            private void DgvDeclaracion_CellContentClick(object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == dgvDeclaracion.Columns["AutoCopiar"].Index)
                {
                    dgvDeclaracion.CommitEdit(DataGridViewDataErrorContexts.Commit);

                    bool isChecked = Convert.ToBoolean(dgvDeclaracion.Rows[e.RowIndex].Cells["AutoCopiar"].Value ?? false);

                    if (isChecked)
                    {
                        string esperadoStr = dgvDeclaracion.Rows[e.RowIndex].Cells["Esperado"].Value?.ToString() ?? "$0.00";
                        decimal esperado = decimal.Parse(esperadoStr, System.Globalization.NumberStyles.Currency, System.Globalization.CultureInfo.CurrentCulture);
                        dgvDeclaracion.Rows[e.RowIndex].Cells["Declarado"].Value = esperado.ToString("C2");
                    }
                    else
                    {
                        dgvDeclaracion.Rows[e.RowIndex].Cells["Declarado"].Value = "$0.00";
                    }
                }
            }

            private void DgvDeclaracion_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
            {
                if (dgvDeclaracion.CurrentCell.ColumnIndex == dgvDeclaracion.Columns["Declarado"].Index)
                {
                    TextBox txt = e.Control as TextBox;
                    if (txt != null)
                    {
                        txt.KeyPress -= Txt_KeyPress;
                        txt.TextChanged -= Txt_TextChanged;
                        txt.KeyPress += Txt_KeyPress;
                        txt.TextChanged += Txt_TextChanged;
                    }
                }
            }

            private void Txt_KeyPress(object sender, KeyPressEventArgs e)
            {
                // Permitir: números, backspace, delete, coma, punto
                if (!char.IsDigit(e.KeyChar) &&
                    e.KeyChar != ',' &&
                    e.KeyChar != '.' &&
                    e.KeyChar != (char)Keys.Back)
                {
                    e.Handled = true;
                }

                // Evitar múltiples separadores decimales
                TextBox txt = sender as TextBox;
                if (txt != null && (e.KeyChar == ',' || e.KeyChar == '.'))
                {
                    if (txt.Text.Contains(",") || txt.Text.Contains("."))
                    {
                        e.Handled = true;
                    }
                }
            }

            private void Txt_TextChanged(object sender, EventArgs e)
            {
                // ✅ SIMPLIFICADO: No hacer nada mientras se escribe
                // Solo permitir que el usuario escriba números normales
                // El formato se aplicará al salir de la celda en CellEndEdit
            }

            private void DgvDeclaracion_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
            {
                if (e.ColumnIndex == dgvDeclaracion.Columns["Declarado"].Index)
                {
                    var cell = dgvDeclaracion.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    string valor = cell.Value?.ToString() ?? "$0,00";

                    // Si es $0,00 o $0.00, limpiar para que el usuario pueda escribir desde cero
                    if (valor == "$0,00" || valor == "$0.00")
                    {
                        cell.Value = "";
                    }
                }
            }

            // ✅ MÉTODO: Al terminar de editar, aplicar formato completo
            private void DgvDeclaracion_CellEndEdit(object sender, DataGridViewCellEventArgs e)
            {
                if (e.ColumnIndex == dgvDeclaracion.Columns["Declarado"].Index)
                {
                    var cell = dgvDeclaracion.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    string valor = cell.Value?.ToString() ?? "";

                    string valorLimpio = valor.Replace("$", "").Replace(" ", "").Trim();

                    if (string.IsNullOrEmpty(valorLimpio))
                    {
                        cell.Value = "$0,00";
                        return;
                    }

                    valorLimpio = valorLimpio.Replace(".", "");
                    valorLimpio = valorLimpio.Replace(",", ".");

                    if (decimal.TryParse(valorLimpio, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal importe))
                    {
                        cell.Value = importe.ToString("C2", CultureInfo.CurrentCulture);
                    }
                    else
                    {
                        cell.Value = "$0,00";
                    }
                }
            }

            private void GuardarDeclaracion()
            {
                for (int i = 0; i < dgvDeclaracion.Rows.Count; i++)
                {
                    string medioPago = dgvDeclaracion.Rows[i].Cells["MedioPago"].Value.ToString();
                    string declaradoStr = dgvDeclaracion.Rows[i].Cells["Declarado"].Value?.ToString() ?? "$0,00";

                    if (decimal.TryParse(declaradoStr, System.Globalization.NumberStyles.Currency,
                        System.Globalization.CultureInfo.CurrentCulture, out decimal declarado))
                    {
                        foreach (DataGridViewRow row in dgvReferencia.Rows)
                        {
                            if (row.IsNewRow) continue;

                            if (row.Cells["MedioPago"].Value.ToString() == medioPago)
                            {
                                row.Cells["Declarado"].Value = declarado.ToString("C2");
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}