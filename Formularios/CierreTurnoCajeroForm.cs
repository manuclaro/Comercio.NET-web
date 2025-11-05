using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Comercio.NET.Services;
using System.Linq;
using System.Collections.Generic;

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
                                NumeroCajero INT NOT NULL,
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

                btnCalcular.Enabled = false;
                btnCalcular.Text = "⏳ Calculando...";

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // ✅ CORREGIDO: Usar nombres REALES de columnas de la tabla Facturas
                var queryResumen = @"
                    WITH TransaccionesSimples AS (
                        SELECT 
                            COALESCE(f.FormadePago, 'Efectivo') as MedioPago,
                            f.ImporteTotal as Importe,
                            f.Fecha,
                            COALESCE(f.NroFactura, CAST(f.NumeroRemito AS NVARCHAR)) as NumeroFactura,
                            CASE WHEN f.TipoFactura LIKE 'NC%' THEN 'Egreso' ELSE 'Ingreso' END as TipoMovimiento
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
                            f.Fecha,
                            COALESCE(f.NroFactura, CAST(f.NumeroRemito AS NVARCHAR)) as NumeroFactura,
                            CASE WHEN f.TipoFactura LIKE 'NC%' THEN 'Egreso' ELSE 'Ingreso' END as TipoMovimiento
                        FROM DetallesPagoFactura dp
                        INNER JOIN Facturas f ON dp.IdFactura = f.idFactura
                        INNER JOIN Usuarios u ON f.UsuarioVenta = u.NombreUsuario
                        WHERE u.NumeroCajero = @numeroCajero
                        AND f.Fecha BETWEEN @fechaInicio AND @fechaFin
                        AND COALESCE(f.FormadePago, 'Efectivo') IN ('Múltiples Medios', 'Multiple')
                    ),
                    TodasTransacciones AS (
                        SELECT * FROM TransaccionesSimples
                        UNION ALL
                        SELECT * FROM TransaccionesMultiples
                    )
                    SELECT 
                        MedioPago,
                        COUNT(*) as Cantidad,
                        SUM(CASE WHEN TipoMovimiento = 'Ingreso' THEN Importe ELSE 0 END) as Ingresos,
                        SUM(CASE WHEN TipoMovimiento = 'Egreso' THEN Importe ELSE 0 END) as Egresos,
                        SUM(CASE WHEN TipoMovimiento = 'Ingreso' THEN Importe ELSE -Importe END) as Neto
                    FROM TodasTransacciones
                    GROUP BY MedioPago
                    ORDER BY MedioPago";

                using var cmdResumen = new SqlCommand(queryResumen, connection);
                cmdResumen.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                cmdResumen.Parameters.AddWithValue("@fechaInicio", dtpFechaInicio.Value.Date);
                cmdResumen.Parameters.AddWithValue("@fechaFin", dtpFechaFin.Value.Date.AddDays(1).AddSeconds(-1));

                dgvResumenPorMedio.Rows.Clear();
                decimal totalEsperado = 0;

                using (var reader = await cmdResumen.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        string medioPago = reader.GetString(0);
                        int cantidad = reader.GetInt32(1);
                        decimal ingresos = reader.GetDecimal(2);
                        decimal egresos = reader.GetDecimal(3);
                        decimal neto = reader.GetDecimal(4);

                        dgvResumenPorMedio.Rows.Add(
                            medioPago,
                            cantidad,
                            ingresos.ToString("C2"),
                            egresos.ToString("C2"),
                            neto.ToString("C2"),
                            "$0.00", // Declarado (a completar)
                            "$0.00"  // Diferencia (a calcular)
                        );

                        totalEsperado += neto;
                    }
                }

                lblTotalEsperado.Text = totalEsperado.ToString("C2");

                // Cargar detalle de transacciones
                await CargarDetalleTransacciones(connectionString, numeroCajero);

                btnDeclarar.Enabled = true;
                btnImprimir.Enabled = true;
                btnCalcular.Text = "📊 Calcular Turno";
                btnCalcular.Enabled = true;

                MessageBox.Show("✅ Cálculo completado correctamente", "Éxito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            // ✅ CORREGIDO: Usar nombres REALES de columnas de la tabla Facturas
            var queryDetalle = @"
                WITH TransaccionesSimples AS (
                    SELECT 
                        f.Fecha,
                        COALESCE(f.NroFactura, CAST(f.NumeroRemito AS NVARCHAR)) as NumeroFactura,
                        COALESCE(f.FormadePago, 'Efectivo') as MedioPago,
                        f.ImporteTotal as Importe,
                        CASE WHEN f.TipoFactura LIKE 'NC%' THEN 'Egreso' ELSE 'Ingreso' END as Tipo
                    FROM Facturas f
                    INNER JOIN Usuarios u ON f.UsuarioVenta = u.NombreUsuario
                    WHERE u.NumeroCajero = @numeroCajero
                    AND f.Fecha BETWEEN @fechaInicio AND @fechaFin
                    AND COALESCE(f.FormadePago, 'Efectivo') NOT IN ('Múltiples Medios', 'Multiple')
                ),
                TransaccionesMultiples AS (
                    SELECT 
                        f.Fecha,
                        COALESCE(f.NroFactura, CAST(f.NumeroRemito AS NVARCHAR)) as NumeroFactura,
                        dp.MedioPago,
                        dp.Importe,
                        CASE WHEN f.TipoFactura LIKE 'NC%' THEN 'Egreso' ELSE 'Ingreso' END as Tipo
                    FROM DetallesPagoFactura dp
                    INNER JOIN Facturas f ON dp.IdFactura = f.idFactura
                    INNER JOIN Usuarios u ON f.UsuarioVenta = u.NombreUsuario
                    WHERE u.NumeroCajero = @numeroCajero
                    AND f.Fecha BETWEEN @fechaInicio AND @fechaFin
                    AND COALESCE(f.FormadePago, 'Efectivo') IN ('Múltiples Medios', 'Multiple')
                )
                SELECT * FROM TransaccionesSimples
                UNION ALL
                SELECT * FROM TransaccionesMultiples
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
                decimal totalEsperado = decimal.Parse(lblTotalEsperado.Text.Replace("$", "").Replace(",", ""));

                foreach (DataGridViewRow row in dgvResumenPorMedio.Rows)
                {
                    if (row.IsNewRow) continue;

                    string declaradoStr = row.Cells["Declarado"].Value?.ToString() ?? "$0.00";
                    decimal declarado = decimal.Parse(declaradoStr.Replace("$", "").Replace(",", ""));
                    totalDeclarado += declarado;

                    string netoStr = row.Cells["Neto"].Value.ToString();
                    decimal neto = decimal.Parse(netoStr.Replace("$", "").Replace(",", ""));
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

                // Crear o actualizar turno
                var queryTurno = @"
                    INSERT INTO TurnosCajero (NumeroCajero, Usuario, FechaApertura, FechaCierre, Estado, Observaciones)
                    OUTPUT INSERTED.Id
                    VALUES (@numeroCajero, @usuario, @fechaInicio, @fechaFin, 'Cerrado', @observaciones)";

                int idTurno;
                using (var cmdTurno = new SqlCommand(queryTurno, connection))
                {
                    cmdTurno.Parameters.AddWithValue("@numeroCajero", numeroCajero);
                    cmdTurno.Parameters.AddWithValue("@usuario", usuarioCierre);
                    cmdTurno.Parameters.AddWithValue("@fechaInicio", dtpFechaInicio.Value);
                    cmdTurno.Parameters.AddWithValue("@fechaFin", dtpFechaFin.Value);
                    cmdTurno.Parameters.AddWithValue("@observaciones", txtObservaciones.Text ?? "");

                    idTurno = (int)await cmdTurno.ExecuteScalarAsync();
                }

                // Guardar detalle del cierre
                foreach (DataGridViewRow row in dgvResumenPorMedio.Rows)
                {
                    if (row.IsNewRow) continue;

                    string medioPago = row.Cells["MedioPago"].Value.ToString();
                    int cantidad = int.Parse(row.Cells["Cantidad"].Value.ToString());
                    decimal esperado = decimal.Parse(row.Cells["Neto"].Value.ToString().Replace("$", "").Replace(",", ""));
                    decimal declarado = decimal.Parse(row.Cells["Declarado"].Value.ToString().Replace("$", "").Replace(",", ""));
                    decimal diferencia = decimal.Parse(row.Cells["Diferencia"].Value.ToString().Replace("$", "").Replace(",", ""));

                    var queryCierre = @"
                        INSERT INTO CierreTurnoCajero 
                        (IdTurno, NumeroCajero, MedioPago, TotalEsperado, TotalDeclarado, Diferencia, CantidadTransacciones, FechaCierre, UsuarioCierre)
                        VALUES 
                        (@idTurno, @numeroCajero, @medioPago, @esperado, @declarado, @diferencia, @cantidad, @fechaCierre, @usuarioCierre)";

                    using var cmdCierre = new SqlCommand(queryCierre, connection);
                    cmdCierre.Parameters.AddWithValue("@idTurno", idTurno);
                    cmdCierre.Parameters.AddWithValue("@numeroCajero", numeroCajero);
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