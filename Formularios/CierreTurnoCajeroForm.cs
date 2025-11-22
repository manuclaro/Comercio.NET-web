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
                Size = new Size(85, 28),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnCalcular.FlatAppearance.BorderSize = 0;
            panelFiltros.Controls.Add(btnCalcular);

            // Botón Imprimir
            btnImprimir = new Button
            {
                Text = "🖨️",
                Location = new Point(750, 10), // X=685 ❌ ¡Muy cerca!
                Size = new Size(75, 28),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Enabled = false
            };
            btnImprimir.FlatAppearance.BorderSize = 0;
            panelFiltros.Controls.Add(btnImprimir);

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
                Size = new Size(80, 20),
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
                cmbCajero.Items.Add(new { NumeroCajero = -1, Display = "-- Seleccionar --" });

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

                dynamic cajeroSeleccionado = cmbCajero.SelectedItem;
                int numeroCajero = cajeroSeleccionado.NumeroCajero;

                if (dtpFechaFin.Value < dtpFechaInicio.Value)
                {
                    MessageBox.Show("La fecha final debe ser mayor a la fecha inicial", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int? turnoAbiertoId = await ObtenerTurnoAbiertoId(numeroCajero);
                
                if (turnoAbiertoId == null)
                {
                    var resultado = MessageBox.Show(
                        "⚠️ No hay turno abierto.\n\n¿Desea abrir un turno ahora?",
                        "Sin Turno",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (resultado == DialogResult.Yes)
                    {
                        using var formApertura = new AperturaTurnoCajeroForm();
                        formApertura.ShowDialog();
                    }
                    return;
                }

                turnoActualId = turnoAbiertoId.Value;

                var config = new ConfigurationBuilder()
                   .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                   .AddJsonFile("appsettings.json")
                   .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                //MessageBox.Show(
                //    $"Rango de búsqueda:\n" +
                //    $"Desde: {dtpFechaInicio.Value:dd/MM/yyyy HH:mm:ss}\n" +
                //    $"Hasta: {dtpFechaFin.Value:dd/MM/yyyy HH:mm:ss}",
                //    "Debug - Fechas",
                //    MessageBoxButtons.OK,
                //    MessageBoxIcon.Information);

                var queryIngresos = @"
                    WITH TransaccionesSimples AS (
                        SELECT 
                            COALESCE(f.FormadePago, 'Efectivo') as MedioPago,
                            f.ImporteTotal as Importe,
                            'Ingreso' as TipoMovimiento
                        FROM Facturas f
                        INNER JOIN Usuarios u ON f.UsuarioVenta = u.NombreUsuario
                        WHERE u.NumeroCajero = @numeroCajero
                        AND f.Hora >= @fechaInicio   -- ✅ CAMBIO: f.Hora en lugar de f.Fecha
                        AND f.Hora <= @fechaFin      -- ✅ CAMBIO: f.Hora en lugar de f.Fecha
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
                        AND f.Hora >= @fechaInicio   -- ✅ CAMBIO: f.Hora
                        AND f.Hora <= @fechaFin      -- ✅ CAMBIO: f.Hora
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


                // ✅ NUEVO: Obtener el monto inicial del turno
                montoInicialTurno = await ObtenerMontoInicialTurno(connectionString, numeroCajero);

                // ✅ Mostrar en la label permanente
                lblMontoInicial.Text = montoInicialTurno.ToString("C2");

                bool turnoCerrado = await VerificarTurnoCerrado(
                    numeroCajero, 
                    dtpFechaInicio.Value,
                    dtpFechaFin.Value
                );
                
                if (turnoCerrado)
                {
                    return;
                }

                btnCalcular.Enabled = false;
                btnCalcular.Text = "⏳...";

               
                var resumenPorMedio = new Dictionary<string, (decimal Ingresos, decimal Egresos, int CantIngresos, int CantEgresos)>();

                using (var cmdIngresos = new SqlCommand(queryIngresos, connection))
                {
                    cmdIngresos.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                    cmdIngresos.Parameters.AddWithValue("@fechaInicio", dtpFechaInicio.Value); // ✅ Con hora
                    cmdIngresos.Parameters.AddWithValue("@fechaFin", dtpFechaFin.Value);       // ✅ Con hora

                    using var reader = await cmdIngresos.ExecuteReaderAsync();
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

                using (var cmdEgresos = new SqlCommand(queryEgresos, connection))
                {
                    cmdEgresos.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                    cmdEgresos.Parameters.AddWithValue("@fechaInicio", dtpFechaInicio.Value);
                    cmdEgresos.Parameters.AddWithValue("@fechaFin", dtpFechaFin.Value);

                    using var reader = await cmdEgresos.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        string medioPago = reader.GetString(0);
                        decimal egresos = reader.GetDecimal(1);
                        int cantidad = reader.GetInt32(2);

                        if (!resumenPorMedio.ContainsKey(medioPago))
                            resumenPorMedio[medioPago] = (0m, 0m, 0, 0);

                        var actual = resumenPorMedio[medioPago];
                        resumenPorMedio[medioPago] = (actual.Ingresos, egresos, actual.CantIngresos, cantidad);
                    }
                }

                dgvResumenPorMedio.Rows.Clear();
                decimal totalEsperado = 0;

                // ✅ CAMBIO: Ordenar para que "Efectivo" siempre sea primero
                var mediosOrdenados = resumenPorMedio
                    .OrderByDescending(kvp => kvp.Key.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                    .ThenBy(kvp => kvp.Key);

                foreach (var kvp in mediosOrdenados)
                {
                    string medioPago = kvp.Key;
                    decimal ingresos = kvp.Value.Ingresos;
                    decimal egresos = kvp.Value.Egresos;
                    int cantTotal = kvp.Value.CantIngresos + kvp.Value.CantEgresos;
                    decimal neto = ingresos - egresos;

                    // ✅ CAMBIO: Si es efectivo, sumar el monto inicial
                    decimal netoConInicial = medioPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase) 
                        ? neto + montoInicialTurno 
                        : neto;

                    dgvResumenPorMedio.Rows.Add(
                        medioPago,
                        cantTotal,
                        ingresos.ToString("C2"),
                        egresos.ToString("C2"),
                        netoConInicial.ToString("C2"),
                        "$0.00",
                        "$0.00"
                    );

                    // ✅ Sumar al total esperado el neto CON monto inicial si es efectivo
                    totalEsperado += netoConInicial;

                    if (egresos > 0)
                    {
                        dgvResumenPorMedio.Rows[dgvResumenPorMedio.Rows.Count - 1].Cells["Egresos"].Style.ForeColor = Color.Red;
                    }
            
                    // ✅ OPCIONAL: Marcar la fila de efectivo con un color diferente para destacarla
                    if (medioPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase) && montoInicialTurno > 0)
                    {
                        int rowIndex = dgvResumenPorMedio.Rows.Count - 1;
                        dgvResumenPorMedio.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 205); // Amarillo claro
                    }
                }

                // ✅ Mostrar el monto inicial si existe
                if (montoInicialTurno > 0)
                {
                    MessageBox.Show(
                        $"✅ Calculado: {totalEsperado:C2}\n\n" +
                        $"💰 Monto inicial del turno: {montoInicialTurno:C2}\n" +
                        $"💵 Total en efectivo a rendir: {(resumenPorMedio.ContainsKey("Efectivo") ? resumenPorMedio["Efectivo"].Ingresos - resumenPorMedio["Efectivo"].Egresos + montoInicialTurno : montoInicialTurno):C2}",
                        "Éxito",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"✅ Calculado: {totalEsperado:C2}", "Éxito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                lblTotalEsperado.Text = totalEsperado.ToString("C2");

                await CargarDetalleTransacciones(connectionString, numeroCajero);

                btnDeclarar.Enabled = true;
                btnImprimir.Enabled = true;
                btnCalcular.Text = "📊 Calcular";
                btnCalcular.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
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
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var queryDetalle = @"
        WITH TransaccionesVentasSimples AS (
            SELECT 
                f.Hora as Fecha,  -- ✅ CAMBIO: Usar f.Hora
                COALESCE(f.NroFactura, CAST(f.NumeroRemito AS NVARCHAR)) as NumeroFactura,
                COALESCE(f.FormadePago, 'Efectivo') as MedioPago,
                f.ImporteTotal as Importe,
                'Ingreso' as Tipo
            FROM Facturas f
            INNER JOIN Usuarios u ON f.UsuarioVenta = u.NombreUsuario
            WHERE u.NumeroCajero = @numeroCajero
            AND f.Hora >= @fechaInicio    -- ✅ CAMBIO: f.Hora
            AND f.Hora <= @fechaFin        -- ✅ CAMBIO: f.Hora
            AND COALESCE(f.FormadePago, 'Efectivo') NOT IN ('Múltiples Medios', 'Multiple')
        ),
        TransaccionesVentasMultiples AS (
            SELECT 
                f.Hora as Fecha,  -- ✅ CAMBIO: Usar f.Hora
                COALESCE(f.NroFactura, CAST(f.NumeroRemito AS NVARCHAR)) as NumeroFactura,
                dp.MedioPago,
                dp.Importe,
                'Ingreso' as Tipo
            FROM DetallesPagoFactura dp
            INNER JOIN Facturas f ON dp.IdFactura = f.idFactura
            INNER JOIN Usuarios u ON f.UsuarioVenta = u.NombreUsuario
            WHERE u.NumeroCajero = @numeroCajero
            AND f.Hora >= @fechaInicio    -- ✅ CAMBIO: f.Hora
            AND f.Hora <= @fechaFin        -- ✅ CAMBIO: f.Hora
            AND COALESCE(f.FormadePago, 'Efectivo') IN ('Múltiples Medios', 'Multiple')
        ),
        TransaccionesPagos AS (
            SELECT 
                cpp.Fecha,
                'Pago #' + CAST(cpp.Id AS NVARCHAR) as NumeroFactura,
                COALESCE(cpp.Metodo, 'Efectivo') as MedioPago,
                cpp.Monto as Importe,
                'Egreso' as Tipo
            FROM ComprasProveedoresPagos cpp
            INNER JOIN ComprasProveedores cp ON cpp.CompraId = cp.Id
            WHERE cp.Cajero = @numeroCajero
            AND cpp.Fecha >= @fechaInicio   -- Mantener cpp.Fecha (esta tabla es correcta)
            AND cpp.Fecha <= @fechaFin
        )
        SELECT * FROM TransaccionesVentasSimples
        UNION ALL
        SELECT * FROM TransaccionesVentasMultiples
        UNION ALL
        SELECT * FROM TransaccionesPagos
        ORDER BY Fecha DESC";

            using var cmdDetalle = new SqlCommand(queryDetalle, connection);
            cmdDetalle.Parameters.AddWithValue("@numeroCajero", numeroCajero);
            cmdDetalle.Parameters.AddWithValue("@fechaInicio", dtpFechaInicio.Value);
            cmdDetalle.Parameters.AddWithValue("@fechaFin", dtpFechaFin.Value);

            dgvDetalleTransacciones.Rows.Clear();

            using var reader = await cmdDetalle.ExecuteReaderAsync();
            while (reader.Read())
            {
                DateTime fecha = reader.GetDateTime(0);
                string numeroFactura = reader.GetString(1);
                string medioPago = reader.GetString(2);
                decimal importe = reader.GetDecimal(3);
                string tipo = reader.GetString(4);

                dgvDetalleTransacciones.Rows.Add(
                    fecha.ToString("dd/MM HH:mm"),
                    numeroFactura,
                    medioPago,
                    importe.ToString("C2"),
                    tipo
                );

                int rowIndex = dgvDetalleTransacciones.Rows.Count - 1;
                if (tipo == "Egreso")
                {
                    dgvDetalleTransacciones.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(244, 67, 54);
                }
                else
                {
                    dgvDetalleTransacciones.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(76, 175, 80);
                }
            }
        }

        private void DeclarMontos()
        {
            using var formDeclaracion = new DeclaracionMontosForm(dgvResumenPorMedio);
            if (formDeclaracion.ShowDialog() == DialogResult.OK)
            {
                decimal totalDeclarado = 0;
                decimal totalEsperado = decimal.Parse(lblTotalEsperado.Text, NumberStyles.Currency, CultureInfo.CurrentCulture);

                foreach (DataGridViewRow row in dgvResumenPorMedio.Rows)
                {
                    if (row.IsNewRow) continue;

                    string declaradoStr = row.Cells["Declarado"].Value?.ToString() ?? "$0.00";
                    decimal declarado = decimal.Parse(declaradoStr, NumberStyles.Currency, CultureInfo.CurrentCulture);
                    totalDeclarado += declarado;

                    string netoStr = row.Cells["Neto"].Value.ToString();
                    decimal neto = decimal.Parse(netoStr, NumberStyles.Currency, CultureInfo.CurrentCulture);
                    decimal diferencia = declarado - neto;
                    
                    row.Cells["Diferencia"].Value = diferencia.ToString("C2");
                    
                    if (diferencia != 0)
                    {
                        row.Cells["Diferencia"].Style.ForeColor = diferencia > 0 ? Color.Green : Color.Red;
                    }
                }

                lblTotalDeclarado.Text = totalDeclarado.ToString("C2");
                decimal diferenciaTotal = totalDeclarado - totalEsperado;
                lblDiferencia.Text = diferenciaTotal.ToString("C2");
                lblDiferencia.ForeColor = diferenciaTotal >= 0 ? Color.Green : Color.Red;

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

                if (turnoActualId > 0)
                {
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

                        await cmdActualizar.ExecuteNonQueryAsync();
                    }

                    idTurno = turnoActualId;
                }
                else
                {
                    var queryTurno = @"
                        INSERT INTO TurnosCajero (NumeroCajero, Usuario, FechaApertura, FechaCierre, Estado, Observaciones)
                        OUTPUT INSERTED.Id
                        VALUES (@numeroCajero, @usuario, @fechaInicio, @fechaFin, 'Cerrado', @observaciones)";

                    using (var cmdTurno = new SqlCommand(queryTurno, connection))
                    {
                        cmdTurno.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                        cmdTurno.Parameters.AddWithValue("@usuario", usuarioCierre);
                        cmdTurno.Parameters.AddWithValue("@fechaInicio", dtpFechaInicio.Value);
                        cmdTurno.Parameters.AddWithValue("@fechaFin", dtpFechaFin.Value);
                        cmdTurno.Parameters.AddWithValue("@observaciones", txtObservaciones.Text ?? "");

                        idTurno = (int)await cmdTurno.ExecuteScalarAsync();
                    }
                }

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

                MessageBox.Show("✅ Turno cerrado", "Éxito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                turnoActualId = 0;
                LimpiarFormulario();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
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
            lblMontoInicial.Text = "$0.00";  // ✅ NUEVO
            lblTotalEsperado.Text = "$0.00";
            lblTotalDeclarado.Text = "$0.00";
            lblDiferencia.Text = "$0.00";
            txtObservaciones.Clear();
            btnDeclarar.Enabled = false;
            btnCerrarTurno.Enabled = false;
            btnImprimir.Enabled = false;
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
                connection.Open();

                var query = @"
                    SELECT TOP 1 Id
                    FROM TurnosCajero 
                    WHERE NumeroCajero = @numeroCajero 
                    AND Estado = 'Abierto'
                    ORDER BY FechaApertura DESC";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);

                var result = await cmd.ExecuteScalarAsync();
                return result != null ? (int?)result : null;
            }
            catch
            {
                return null;
            }
        }
    }

    public class DeclaracionMontosForm : Form
    {
        private DataGridView dgvDeclaracion;
        private Button btnGuardar, btnCancelar;
        private DataGridView dgvReferencia;

        public DeclaracionMontosForm(DataGridView dgvReferencia)
        {
            this.dgvReferencia = dgvReferencia;
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
                dgvDeclaracion.Rows.Add(
                    row.Cells["MedioPago"].Value,
                    row.Cells["Neto"].Value,
                    "$0,00",
                    false
                );
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