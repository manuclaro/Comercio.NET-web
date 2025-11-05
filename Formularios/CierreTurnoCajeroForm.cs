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
        private Label lblTotalEsperado, lblTotalDeclarado, lblDiferencia;
        private TextBox txtObservaciones;
        private Panel panelResumen, panelDeclaracion;
        private bool turnoAbierto = false;
        private int turnoActualId = 0;

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
            this.ClientSize = new Size(1200, 750);
            this.MinimumSize = new Size(1000, 600);
            this.Name = "CierreTurnoCajeroForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Cierre de Turno de Cajero";
            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            this.Text = "💰 Cierre de Turno de Cajero";
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
                Text = "💰 CIERRE DE TURNO DE CAJERO",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(margin, currentY),
                Size = new Size(500, 35),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblTitulo);
            currentY += 50;

            // Panel de Filtros
            var panelFiltros = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(1160, 80),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelFiltros);

            // Cajero
            panelFiltros.Controls.Add(new Label
            {
                Text = "Cajero:",
                Location = new Point(15, 20),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            cmbCajero = new ComboBox
            {
                Location = new Point(100, 18),
                Size = new Size(180, 25),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            panelFiltros.Controls.Add(cmbCajero);

            // Fecha Inicio
            panelFiltros.Controls.Add(new Label
            {
                Text = "Desde:",
                Location = new Point(300, 20),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            dtpFechaInicio = new DateTimePicker
            {
                Location = new Point(365, 18),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Short
            };
            dtpFechaInicio.Value = DateTime.Today;
            panelFiltros.Controls.Add(dtpFechaInicio);

            // Fecha Fin
            panelFiltros.Controls.Add(new Label
            {
                Text = "Hasta:",
                Location = new Point(530, 20),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            dtpFechaFin = new DateTimePicker
            {
                Location = new Point(595, 18),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Short
            };
            dtpFechaFin.Value = DateTime.Today.AddHours(23).AddMinutes(59);
            panelFiltros.Controls.Add(dtpFechaFin);

            // Botón Calcular
            btnCalcular = new Button
            {
                Text = "📊 Calcular Turno",
                Location = new Point(770, 15),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCalcular.FlatAppearance.BorderSize = 0;
            panelFiltros.Controls.Add(btnCalcular);

            // Botón Imprimir
            btnImprimir = new Button
            {
                Text = "🖨️ Imprimir",
                Location = new Point(940, 15),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Enabled = false
            };
            btnImprimir.FlatAppearance.BorderSize = 0;
            panelFiltros.Controls.Add(btnImprimir);

            currentY += 100;

            // Panel Resumen (Izquierda)
            panelResumen = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(560, 500),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelResumen);

            // Título Resumen
            panelResumen.Controls.Add(new Label
            {
                Text = "📊 RESUMEN POR MEDIO DE PAGO",
                Location = new Point(15, 15),
                Size = new Size(530, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            // DataGridView Resumen
            dgvResumenPorMedio = new DataGridView
            {
                Location = new Point(15, 50),
                Size = new Size(530, 300),
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

            // Configurar columnas del resumen
            dgvResumenPorMedio.Columns.Add("MedioPago", "Medio de Pago");
            dgvResumenPorMedio.Columns.Add("Cantidad", "Cant.");
            dgvResumenPorMedio.Columns.Add("Ingresos", "Ingresos");
            dgvResumenPorMedio.Columns.Add("Egresos", "Egresos");
            dgvResumenPorMedio.Columns.Add("Neto", "Total Neto");
            dgvResumenPorMedio.Columns.Add("Declarado", "Declarado");
            dgvResumenPorMedio.Columns.Add("Diferencia", "Diferencia");

            // Configurar anchos
            dgvResumenPorMedio.Columns["MedioPago"].FillWeight = 25;
            dgvResumenPorMedio.Columns["Cantidad"].FillWeight = 10;
            dgvResumenPorMedio.Columns["Ingresos"].FillWeight = 15;
            dgvResumenPorMedio.Columns["Egresos"].FillWeight = 15;
            dgvResumenPorMedio.Columns["Neto"].FillWeight = 15;
            dgvResumenPorMedio.Columns["Declarado"].FillWeight = 15;
            dgvResumenPorMedio.Columns["Diferencia"].FillWeight = 15;

            panelResumen.Controls.Add(dgvResumenPorMedio);

            // Totales
            int totalY = 360;
            panelResumen.Controls.Add(new Label
            {
                Text = "TOTAL ESPERADO:",
                Location = new Point(15, totalY),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243)
            });

            lblTotalEsperado = new Label
            {
                Text = "$0.00",
                Location = new Point(170, totalY),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                TextAlign = ContentAlignment.MiddleRight
            };
            panelResumen.Controls.Add(lblTotalEsperado);

            totalY += 35;
            panelResumen.Controls.Add(new Label
            {
                Text = "TOTAL DECLARADO:",
                Location = new Point(15, totalY),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 152, 0)
            });

            lblTotalDeclarado = new Label
            {
                Text = "$0.00",
                Location = new Point(170, totalY),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 152, 0),
                TextAlign = ContentAlignment.MiddleRight
            };
            panelResumen.Controls.Add(lblTotalDeclarado);

            totalY += 35;
            panelResumen.Controls.Add(new Label
            {
                Text = "DIFERENCIA:",
                Location = new Point(15, totalY),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 67, 54)
            });

            lblDiferencia = new Label
            {
                Text = "$0.00",
                Location = new Point(170, totalY),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 67, 54),
                TextAlign = ContentAlignment.MiddleRight
            };
            panelResumen.Controls.Add(lblDiferencia);

            // Panel Declaración y Detalle (Derecha)
            panelDeclaracion = new Panel
            {
                Location = new Point(margin + 580, currentY),
                Size = new Size(580, 500),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelDeclaracion);

            // Título Detalle
            panelDeclaracion.Controls.Add(new Label
            {
                Text = "📋 DETALLE DE TRANSACCIONES",
                Location = new Point(15, 15),
                Size = new Size(550, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            // DataGridView Detalle
            dgvDetalleTransacciones = new DataGridView
            {
                Location = new Point(15, 50),
                Size = new Size(550, 300),
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

            // Configurar columnas del detalle
            dgvDetalleTransacciones.Columns.Add("Fecha", "Fecha/Hora");
            dgvDetalleTransacciones.Columns.Add("NumeroFactura", "Factura");
            dgvDetalleTransacciones.Columns.Add("MedioPago", "Medio Pago");
            dgvDetalleTransacciones.Columns.Add("Importe", "Importe");
            dgvDetalleTransacciones.Columns.Add("Tipo", "Tipo");

            dgvDetalleTransacciones.Columns["Fecha"].FillWeight = 25;
            dgvDetalleTransacciones.Columns["NumeroFactura"].FillWeight = 20;
            dgvDetalleTransacciones.Columns["MedioPago"].FillWeight = 20;
            dgvDetalleTransacciones.Columns["Importe"].FillWeight = 20;
            dgvDetalleTransacciones.Columns["Tipo"].FillWeight = 15;

            panelDeclaracion.Controls.Add(dgvDetalleTransacciones);

            // Observaciones
            panelDeclaracion.Controls.Add(new Label
            {
                Text = "Observaciones:",
                Location = new Point(15, 360),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            txtObservaciones = new TextBox
            {
                Location = new Point(15, 390),
                Size = new Size(550, 60),
                Font = new Font("Segoe UI", 9F),
                Multiline = true,
                PlaceholderText = "Notas sobre el cierre de turno..."
            };
            panelDeclaracion.Controls.Add(txtObservaciones);

            // Botones de Acción
            btnDeclarar = new Button
            {
                Text = "💵 Declarar Montos",
                Location = new Point(15, 460),
                Size = new Size(160, 35),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Enabled = false
            };
            btnDeclarar.FlatAppearance.BorderSize = 0;
            panelDeclaracion.Controls.Add(btnDeclarar);

            btnCerrarTurno = new Button
            {
                Text = "✅ Cerrar Turno",
                Location = new Point(195, 460),
                Size = new Size(160, 35),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Enabled = false
            };
            btnCerrarTurno.FlatAppearance.BorderSize = 0;
            panelDeclaracion.Controls.Add(btnCerrarTurno);
        }

        private void ConfigurarEventos()
        {
            btnCalcular.Click += async (s, e) => await CalcularTurno();
            btnDeclarar.Click += (s, e) => DeclarMontos();
            btnCerrarTurno.Click += async (s, e) => await CerrarTurno();
            btnImprimir.Click += (s, e) => ImprimirCierre();
            
            cmbCajero.SelectedIndexChanged += (s, e) => LimpiarFormulario();
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
                
                // Usar NumeroCajero directamente según el esquema de tu BD
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
                MessageBox.Show($"Error cargando cajeros: {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error",
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

                // Validar fechas
                if (dtpFechaFin.Value < dtpFechaInicio.Value)
                {
                    MessageBox.Show("La fecha final debe ser mayor a la fecha inicial", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // ✅ NUEVO: Verificar si hay un turno abierto
                int? turnoAbiertoId = await ObtenerTurnoAbiertoId(numeroCajero);
                
                if (turnoAbiertoId == null)
                {
                    var resultado = MessageBox.Show(
                        "⚠️ No hay un turno abierto para este cajero.\n\n" +
                        "Para realizar un cierre, primero debe abrir un turno.\n\n" +
                        "¿Desea abrir un turno ahora?",
                        "Sin Turno Abierto",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (resultado == DialogResult.Yes)
                    {
                        using var formApertura = new AperturaTurnoCajeroForm();
                        if (formApertura.ShowDialog() == DialogResult.OK)
                        {
                            MessageBox.Show(
                                "✅ Turno abierto correctamente.\n\n" +
                                "Ahora puede calcular y cerrar el turno.",
                                "Información",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                    return;
                }

                // Guardar el ID del turno abierto para usarlo al cerrar
                turnoActualId = turnoAbiertoId.Value;

                // ✅ Verificar si el turno ya fue cerrado
                bool turnoCerrado = await VerificarTurnoCerrado(numeroCajero, dtpFechaInicio.Value.Date, dtpFechaFin.Value.Date.AddDays(1).AddSeconds(-1));
                
                if (turnoCerrado)
                {
                    var resultado = MessageBox.Show(
                        "⚠️ Ya existe un cierre de turno para este cajero en el período seleccionado.\n\n" +
                        "¿Desea ver el historial de cierres?",
                        "Turno Ya Cerrado",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (resultado == DialogResult.Yes)
                    {
                        // Abrir el historial de cierres (implementar más adelante)
                        MostrarHistorialCierres(numeroCajero);
                    }
                    return;
                }

                btnCalcular.Enabled = false;
                btnCalcular.Text = "⏳ Calculando...";

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // ========================================
                // 1. CALCULAR INGRESOS POR VENTAS
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
                        AND f.Fecha BETWEEN @fechaInicio AND @fechaFin
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
                        AND f.Fecha BETWEEN @fechaInicio AND @fechaFin
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

                // ========================================
                // 2. CALCULAR EGRESOS POR PAGOS A PROVEEDORES
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

                // Diccionario para consolidar ingresos y egresos por medio de pago
                var resumenPorMedio = new Dictionary<string, (decimal Ingresos, decimal Egresos, int CantIngresos, int CantEgresos)>();

                // Cargar ingresos
                using (var cmdIngresos = new SqlCommand(queryIngresos, connection))
                {
                    cmdIngresos.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                    cmdIngresos.Parameters.AddWithValue("@fechaInicio", dtpFechaInicio.Value.Date);
                    cmdIngresos.Parameters.AddWithValue("@fechaFin", dtpFechaFin.Value.Date.AddDays(1).AddSeconds(-1));

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

                // Cargar egresos
                using (var cmdEgresos = new SqlCommand(queryEgresos, connection))
                {
                    cmdEgresos.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                    cmdEgresos.Parameters.AddWithValue("@fechaInicio", dtpFechaInicio.Value.Date);
                    cmdEgresos.Parameters.AddWithValue("@fechaFin", dtpFechaFin.Value.Date.AddDays(1).AddSeconds(-1));

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

                // Llenar la grilla con el resumen consolidado
                dgvResumenPorMedio.Rows.Clear();
                decimal totalEsperado = 0;

                foreach (var kvp in resumenPorMedio.OrderBy(x => x.Key))
                {
                    string medioPago = kvp.Key;
                    decimal ingresos = kvp.Value.Ingresos;
                    decimal egresos = kvp.Value.Egresos;
                    int cantIngresos = kvp.Value.CantIngresos;
                    int cantEgresos = kvp.Value.CantEgresos;
                    int cantidadTotal = cantIngresos + cantEgresos;
                    decimal neto = ingresos - egresos;

                    dgvResumenPorMedio.Rows.Add(
                        medioPago,
                        cantidadTotal,
                        ingresos.ToString("C2"),
                        egresos.ToString("C2"),
                        neto.ToString("C2"),
                        "$0.00", // Declarado (a completar)
                        "$0.00"  // Diferencia (a calcular)
                    );

                    totalEsperado += neto;

                    // Colorear egresos en rojo si hay
                    if (egresos > 0)
                    {
                        dgvResumenPorMedio.Rows[dgvResumenPorMedio.Rows.Count - 1].Cells["Egresos"].Style.ForeColor = Color.Red;
                    }
                }

                lblTotalEsperado.Text = totalEsperado.ToString("C2");

                // Cargar detalle de transacciones
                await CargarDetalleTransacciones(connectionString, numeroCajero);

                btnDeclarar.Enabled = true;
                btnImprimir.Enabled = true;
                btnCalcular.Text = "📊 Calcular Turno";
                btnCalcular.Enabled = true;

                // Mensaje informativo
                int totalTransacciones = resumenPorMedio.Sum(x => x.Value.CantIngresos + x.Value.CantEgresos);
                int totalIngresos = resumenPorMedio.Sum(x => x.Value.CantIngresos);
                int totalEgresos = resumenPorMedio.Sum(x => x.Value.CantEgresos);

                MessageBox.Show($"✅ Cálculo completado correctamente\n\n" +
                               $"Total transacciones: {totalTransacciones}\n" +
                               $"• Ingresos (Ventas): {totalIngresos}\n" +
                               $"• Egresos (Pagos a Proveedores): {totalEgresos}\n\n" +
                               $"Saldo neto esperado: {totalEsperado:C2}", 
                               "Éxito",
                               MessageBoxButtons.OK, 
                               MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculando turno: {ex.Message}\n\nDetalle: {ex.StackTrace}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnCalcular.Text = "📊 Calcular Turno";
                btnCalcular.Enabled = true;
            }
        }

        private async Task CargarDetalleTransacciones(string connectionString, int numeroCajero)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            // Query combinada: Ventas + Pagos a Proveedores
            var queryDetalle = @"
                -- Transacciones de Ventas (Ingresos)
                WITH TransaccionesVentasSimples AS (
                    SELECT 
                        f.Fecha,
                        COALESCE(f.NroFactura, CAST(f.NumeroRemito AS NVARCHAR)) as NumeroFactura,
                        COALESCE(f.FormadePago, 'Efectivo') as MedioPago,
                        f.ImporteTotal as Importe,
                        'Ingreso (Venta)' as Tipo
                    FROM Facturas f
                    INNER JOIN Usuarios u ON f.UsuarioVenta = u.NombreUsuario
                    WHERE u.NumeroCajero = @numeroCajero
                    AND f.Fecha BETWEEN @fechaInicio AND @fechaFin
                    AND COALESCE(f.FormadePago, 'Efectivo') NOT IN ('Múltiples Medios', 'Multiple')
                ),
                TransaccionesVentasMultiples AS (
                    SELECT 
                        f.Fecha,
                        COALESCE(f.NroFactura, CAST(f.NumeroRemito AS NVARCHAR)) as NumeroFactura,
                        dp.MedioPago,
                        dp.Importe,
                        'Ingreso (Venta)' as Tipo
                    FROM DetallesPagoFactura dp
                    INNER JOIN Facturas f ON dp.IdFactura = f.idFactura
                    INNER JOIN Usuarios u ON f.UsuarioVenta = u.NombreUsuario
                    WHERE u.NumeroCajero = @numeroCajero
                    AND f.Fecha BETWEEN @fechaInicio AND @fechaFin
                    AND COALESCE(f.FormadePago, 'Efectivo') IN ('Múltiples Medios', 'Multiple')
                ),
                -- Pagos a Proveedores (Egresos)
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

            using var cmdDetalle = new SqlCommand(queryDetalle, connection);
            cmdDetalle.Parameters.AddWithValue("@numeroCajero", numeroCajero);
            cmdDetalle.Parameters.AddWithValue("@fechaInicio", dtpFechaInicio.Value.Date);
            cmdDetalle.Parameters.AddWithValue("@fechaFin", dtpFechaFin.Value.Date.AddDays(1).AddSeconds(-1));

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
                    fecha.ToString("dd/MM/yy HH:mm"),
                    numeroFactura,
                    medioPago,
                    importe.ToString("C2"),
                    tipo
                );

                // Colorear según tipo
                int rowIndex = dgvDetalleTransacciones.Rows.Count - 1;
                if (tipo.Contains("Egreso"))
                {
                    dgvDetalleTransacciones.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(244, 67, 54); // Rojo para egresos
                    dgvDetalleTransacciones.Rows[rowIndex].DefaultCellStyle.Font = new Font(dgvDetalleTransacciones.Font, FontStyle.Bold);
                }
                else
                {
                    dgvDetalleTransacciones.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(76, 175, 80); // Verde para ingresos
                }
            }
        }

        //private async Task CargarDetalleTransacciones(string connectionString, int numeroCajero)
        //{
        //    using var connection = new SqlConnection(connectionString);
        //    connection.Open();

        //    var queryDetalle = @"
        //        WITH TransaccionesSimples AS (
        //            SELECT 
        //                f.FechaHoraFacturacion as Fecha,
        //                f.NumeroFactura,
        //                f.FormaPago as MedioPago,
        //                f.Importe,
        //                CASE WHEN f.TipoFactura LIKE 'NC%' THEN 'Egreso' ELSE 'Ingreso' END as Tipo
        //            FROM Facturas f
        //            INNER JOIN Usuarios u ON f.UsuarioCreador = u.NombreUsuario
        //            WHERE u.NumeroCajero = @numeroCajero
        //            AND f.FechaHoraFacturacion BETWEEN @fechaInicio AND @fechaFin
        //            AND f.FormaPago NOT IN ('Múltiples Medios', 'Multiple')
        //        ),
        //        TransaccionesMultiples AS (
        //            SELECT 
        //                f.FechaHoraFacturacion as Fecha,
        //                f.NumeroFactura,
        //                dp.MedioPago,
        //                dp.Importe,
        //                CASE WHEN f.TipoFactura LIKE 'NC%' THEN 'Egreso' ELSE 'Ingreso' END as Tipo
        //            FROM DetallesPagoFactura dp
        //            INNER JOIN Facturas f ON dp.IdFactura = f.Id
        //            INNER JOIN Usuarios u ON f.UsuarioCreador = u.NombreUsuario
        //            WHERE u.NumeroCajero = @numeroCajero
        //            AND f.FechaHoraFacturacion BETWEEN @fechaInicio AND @fechaFin
        //            AND f.FormaPago IN ('Múltiples Medios', 'Multiple')
        //        )
        //        SELECT * FROM TransaccionesSimples
        //        UNION ALL
        //        SELECT * FROM TransaccionesMultiples
        //        ORDER BY Fecha DESC";

        //    using var cmdDetalle = new SqlCommand(queryDetalle, connection);
        //    cmdDetalle.Parameters.AddWithValue("@numeroCajero", numeroCajero);
        //    cmdDetalle.Parameters.AddWithValue("@fechaInicio", dtpFechaInicio.Value.Date);
        //    cmdDetalle.Parameters.AddWithValue("@fechaFin", dtpFechaFin.Value.Date.AddDays(1).AddSeconds(-1));

        //    dgvDetalleTransacciones.Rows.Clear();

        //    using var reader = await cmdDetalle.ExecuteReaderAsync();
        //    while (reader.Read())
        //    {
        //        DateTime fecha = reader.GetDateTime(0);
        //        string numeroFactura = reader.GetString(1);
        //        string medioPago = reader.GetString(2);
        //        decimal importe = reader.GetDecimal(3);
        //        string tipo = reader.GetString(4);

        //        dgvDetalleTransacciones.Rows.Add(
        //            fecha.ToString("dd/MM/yy HH:mm"),
        //            numeroFactura,
        //            medioPago,
        //            importe.ToString("C2"),
        //            tipo
        //        );

        //        // Colorear según tipo
        //        int rowIndex = dgvDetalleTransacciones.Rows.Count - 1;
        //        if (tipo == "Egreso")
        //        {
        //            dgvDetalleTransacciones.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(244, 67, 54);
        //        }
        //        else
        //        {
        //            dgvDetalleTransacciones.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(76, 175, 80);
        //        }
        //    }
        //}

        private void DeclarMontos()
        {
            using var formDeclaracion = new DeclaracionMontosForm(dgvResumenPorMedio);
            if (formDeclaracion.ShowDialog() == DialogResult.OK)
            {
                // Actualizar totales
                decimal totalDeclarado = 0;
                decimal totalEsperado = decimal.Parse(lblTotalEsperado.Text, NumberStyles.Currency, CultureInfo.CurrentCulture);

                foreach (DataGridViewRow row in dgvResumenPorMedio.Rows)
                {
                    if (row.IsNewRow) continue;

                    string declaradoStr = row.Cells["Declarado"].Value?.ToString() ?? "$0.00";
                    // ✅ CORREGIDO: Usar NumberStyles.Currency
                    decimal declarado = decimal.Parse(declaradoStr, NumberStyles.Currency, CultureInfo.CurrentCulture);
                    totalDeclarado += declarado;

                    string netoStr = row.Cells["Neto"].Value.ToString();
                    // ✅ CORREGIDO: Usar NumberStyles.Currency
                    decimal neto = decimal.Parse(netoStr, NumberStyles.Currency, CultureInfo.CurrentCulture);
                    decimal diferencia = declarado - neto;
                    
                    row.Cells["Diferencia"].Value = diferencia.ToString("C2");
                    
                    // Colorear diferencia
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
                    "¿Está seguro de cerrar el turno?\n\nEsta acción quedará registrada permanentemente.",
                    "Confirmar Cierre",
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
                    // ✅ MEJORADO: Actualizar el turno existente
                    var queryActualizar = @"
                        UPDATE TurnosCajero 
                        SET FechaCierre = @fechaCierre, 
                            Estado = 'Cerrado',
                            Observaciones = COALESCE(Observaciones, '') + CHAR(13) + CHAR(10) + 'CIERRE: ' + @observacionesCierre
                        WHERE Id = @idTurno";

                    using (var cmdActualizar = new SqlCommand(queryActualizar, connection))
                    {
                        cmdActualizar.Parameters.AddWithValue("@idTurno", turnoActualId);
                        cmdActualizar.Parameters.AddWithValue("@fechaCierre", DateTime.Now);
                        cmdActualizar.Parameters.AddWithValue("@observacionesCierre", txtObservaciones.Text ?? "Sin observaciones");

                        await cmdActualizar.ExecuteNonQueryAsync();
                    }

                    idTurno = turnoActualId;
                }
                else
                {
                    // Crear nuevo turno (fallback por compatibilidad)
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

                // Guardar detalle del cierre (código existente)
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

                MessageBox.Show("✅ Turno cerrado exitosamente", "Éxito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                turnoActualId = 0; // Resetear
                LimpiarFormulario();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cerrando turno: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImprimirCierre()
        {
            try
            {
                // TODO: Implementar generación de reporte en PDF o impresión directa
                MessageBox.Show("Función de impresión en desarrollo", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error imprimiendo: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LimpiarFormulario()
        {
            dgvResumenPorMedio.Rows.Clear();
            dgvDetalleTransacciones.Rows.Clear();
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

                var query = @"
                    SELECT COUNT(*) 
                    FROM TurnosCajero 
                    WHERE NumeroCajero = @numeroCajero 
                    AND Estado = 'Cerrado'
                    AND (
                        (FechaApertura <= @fechaFin AND FechaCierre >= @fechaInicio)
                        OR (FechaApertura BETWEEN @fechaInicio AND @fechaFin)
                        OR (FechaCierre BETWEEN @fechaInicio AND @fechaFin)
                    )";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                cmd.Parameters.AddWithValue("@fechaInicio", fechaInicio);
                cmd.Parameters.AddWithValue("@fechaFin", fechaFin);

                int count = (int)await cmd.ExecuteScalarAsync();
                return count > 0;
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
                MessageBox.Show($"Error mostrando historial: {ex.Message}", "Error",
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

    // Formulario auxiliar para declaración de montos
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
            this.ClientSize = new Size(600, 500);
            this.Text = "💵 Declarar Montos Reales";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }

        private void CrearControles()
        {
            this.Controls.Add(new Label
            {
                Text = "Ingrese el monto real declarado por cada medio de pago:",
                Location = new Point(20, 20),
                Size = new Size(560, 40),
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            dgvDeclaracion = new DataGridView
            {
                Location = new Point(20, 70),
                Size = new Size(560, 350),
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                Font = new Font("Segoe UI", 10F)
            };

            dgvDeclaracion.Columns.Add("MedioPago", "Medio de Pago");
            dgvDeclaracion.Columns.Add("Esperado", "Esperado");
            
            var colDeclarado = new DataGridViewTextBoxColumn
            {
                Name = "Declarado",
                HeaderText = "Monto Declarado",
                ValueType = typeof(decimal)
            };
            dgvDeclaracion.Columns.Add(colDeclarado);

            dgvDeclaracion.Columns["MedioPago"].ReadOnly = true;
            dgvDeclaracion.Columns["Esperado"].ReadOnly = true;
            dgvDeclaracion.Columns["MedioPago"].Width = 200;
            dgvDeclaracion.Columns["Esperado"].Width = 150;
            dgvDeclaracion.Columns["Declarado"].Width = 150;

            // Cargar datos
            foreach (DataGridViewRow row in dgvReferencia.Rows)
            {
                if (row.IsNewRow) continue;

                string medioPago = row.Cells["MedioPago"].Value.ToString();
                string esperado = row.Cells["Neto"].Value.ToString();

                dgvDeclaracion.Rows.Add(medioPago, esperado, "0.00");
            }

            this.Controls.Add(dgvDeclaracion);

            btnGuardar = new Button
            {
                Text = "💾 Guardar",
                Location = new Point(360, 430),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
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
                Location = new Point(480, 430),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancelar);
        }

        private void GuardarDeclaracion()
        {
            for (int i = 0; i < dgvDeclaracion.Rows.Count; i++)
            {
                string medioPago = dgvDeclaracion.Rows[i].Cells["MedioPago"].Value.ToString();
                decimal declarado;
                
                if (decimal.TryParse(dgvDeclaracion.Rows[i].Cells["Declarado"].Value?.ToString() ?? "0", out declarado))
                {
                    // Buscar la fila correspondiente en la grilla de referencia
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