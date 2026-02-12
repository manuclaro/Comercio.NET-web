using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace Comercio.NET.Formularios
{
    public partial class frmPagosProveedores : Form
    {
        private DataGridView dgvPagos;
        private DateTimePicker dtpDesde, dtpHasta;
        private ComboBox cboProveedor;
        private TextBox txtFiltroCajero;
        private Button btnBuscar, btnHoy, btnSemana, btnMes, btnLimpiarFiltros, btnExportar;
        private Label lblTotal, lblCantidadPagos, lblTitulo;
        private Panel panelFiltros, panelResumen;
        private string connectionString;
        private DataTable datosOriginales;
        private Panel panelGrilla;

        public frmPagosProveedores()
        {
            InitializeComponent();
            CargarConnectionString();
            ConfigurarFormulario();
            CargarProveedores();
            CargarPagosDelDia();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new Size(1200, 700);
            this.Name = "frmPagosProveedores";
            this.Text = "Consulta de Pagos a Proveedores";
            this.StartPosition = FormStartPosition.CenterParent;
            this.WindowState = FormWindowState.Maximized;
            this.ResumeLayout(false);
        }

        private void CargarConnectionString()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            connectionString = config.GetConnectionString("DefaultConnection");
        }

        private void ConfigurarFormulario()
        {
            this.BackColor = Color.FromArgb(240, 244, 248);
            this.Font = new Font("Segoe UI", 9F);

            // ✅ ORDEN CORREGIDO: El orden de Controls.Add importa con Dock
            // Los controles se apilan en orden inverso al que se agregan

            CrearPanelResumen();    // Se agrega PRIMERO pero quedará abajo (Dock.Bottom)
            CrearPanelGrilla();     // Se agrega SEGUNDO y quedará en medio (Dock.Fill)
            CrearPanelFiltros();    // Se agrega ÚLTIMO pero quedará arriba (Dock.Top)
        }

        // ✅ MODIFICADO: Panel contenedor sin padding extra
        private void CrearPanelGrilla()
        {
            panelGrilla = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0), // ✅ SIN padding para usar todo el espacio
                BackColor = Color.FromArgb(240, 244, 248)
            };
            this.Controls.Add(panelGrilla);

            // Crear la grilla DENTRO del panel
            CrearGrillaPagos();
        }

        private void CrearPanelFiltros()
        {
            panelFiltros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 105, // ✅ AUMENTADO de 105 a 115
                BackColor = Color.White,
                Padding = new Padding(10, 5, 10, 5)
            };
            this.Controls.Add(panelFiltros);

            int currentY = 3;
            int margin = 10;

            // Título
            lblTitulo = new Label
            {
                Text = "💳 PAGOS A PROVEEDORES",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185),
                Location = new Point(margin, currentY),
                AutoSize = true
            };
            panelFiltros.Controls.Add(lblTitulo);
            currentY += 28;

            // Primera fila de filtros
            int controlY = currentY;

            // Proveedor
            var lblProveedor = new Label
            {
                Text = "Proveedor:",
                Location = new Point(margin, controlY + 4),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            panelFiltros.Controls.Add(lblProveedor);

            cboProveedor = new ComboBox
            {
                Location = new Point(margin + 75, controlY),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cboProveedor.SelectedIndexChanged += (s, e) => AplicarFiltros();
            panelFiltros.Controls.Add(cboProveedor);

            // Cajero
            var lblCajero = new Label
            {
                Text = "Cajero #:",
                Location = new Point(margin + 340, controlY + 4),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            panelFiltros.Controls.Add(lblCajero);

            txtFiltroCajero = new TextBox
            {
                Location = new Point(margin + 400, controlY),
                Width = 80,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Número..."
            };
            txtFiltroCajero.TextChanged += (s, e) => AplicarFiltros();
            panelFiltros.Controls.Add(txtFiltroCajero);

            controlY += 30;

            // Segunda fila: Fechas y botones
            var lblDesde = new Label
            {
                Text = "Desde:",
                Location = new Point(margin, controlY + 4),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            panelFiltros.Controls.Add(lblDesde);

            dtpDesde = new DateTimePicker
            {
                Location = new Point(margin + 50, controlY),
                Width = 110,
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9F)
            };
            panelFiltros.Controls.Add(dtpDesde);

            var lblHasta = new Label
            {
                Text = "Hasta:",
                Location = new Point(margin + 175, controlY + 4),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            panelFiltros.Controls.Add(lblHasta);

            dtpHasta = new DateTimePicker
            {
                Location = new Point(margin + 220, controlY),
                Width = 110,
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9F)
            };
            panelFiltros.Controls.Add(dtpHasta);

            // Botones más compactos
            int btnX = margin + 345;
            btnHoy = CrearBotonFecha("Hoy", btnX, controlY, Color.FromArgb(52, 152, 219));
            btnHoy.Click += (s, e) =>
            {
                dtpDesde.Value = DateTime.Today;
                dtpHasta.Value = DateTime.Today;
                CargarPagosDelDia();
            };

            btnSemana = CrearBotonFecha("Semana", btnX + 75, controlY, Color.FromArgb(46, 204, 113));
            btnSemana.Click += (s, e) =>
            {
                dtpDesde.Value = DateTime.Today.AddDays(-7);
                dtpHasta.Value = DateTime.Today;
                CargarPagosPorFecha(dtpDesde.Value, dtpHasta.Value);
            };

            btnMes = CrearBotonFecha("Mes", btnX + 150, controlY, Color.FromArgb(155, 89, 182));
            btnMes.Click += (s, e) =>
            {
                dtpDesde.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                dtpHasta.Value = DateTime.Today;
                CargarPagosPorFecha(dtpDesde.Value, dtpHasta.Value);
            };

            btnBuscar = CrearBotonFecha("🔍 Buscar", btnX + 225, controlY, Color.FromArgb(41, 128, 185));
            btnBuscar.Width = 85;
            btnBuscar.Click += (s, e) => CargarPagosPorFecha(dtpDesde.Value, dtpHasta.Value);

            btnLimpiarFiltros = CrearBotonFecha("🗑️ Limpiar", btnX + 315, controlY, Color.FromArgb(231, 76, 60));
            btnLimpiarFiltros.Width = 85;
            btnLimpiarFiltros.Click += (s, e) => LimpiarFiltros();

            btnExportar = CrearBotonFecha("📊 Exportar", btnX + 405, controlY, Color.FromArgb(243, 156, 18));
            btnExportar.Width = 95;
            btnExportar.Click += BtnExportar_Click;
        }

        private Button CrearBotonFecha(string texto, int x, int y, Color color)
        {
            var btn = new Button
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(70, 26), // ✅ Altura reducida de 28 a 26
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            panelFiltros.Controls.Add(btn);
            return btn;
        }

        private void CrearGrillaPagos()
        {
            dgvPagos = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                MultiSelect = false, // ✅ NUEVO: Solo permite seleccionar una fila a la vez
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(250, 250, 250)
                }
            };

            // Estilo de encabezados
            dgvPagos.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(5)
            };

            dgvPagos.ColumnHeadersHeight = 35;
            dgvPagos.RowTemplate.Height = 28;

            // ✅ NUEVO: Evento para quitar selección inicial
            dgvPagos.DataBindingComplete += (s, e) =>
            {
                dgvPagos.ClearSelection();
            };

            panelGrilla.Controls.Add(dgvPagos);
        }

        private void CrearPanelResumen()
        {
            panelResumen = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50, // ✅ REDUCIDO de 60 a 50
                BackColor = Color.FromArgb(52, 73, 94),
                Padding = new Padding(15, 5, 15, 5) // ✅ Padding vertical reducido
            };
            this.Controls.Add(panelResumen);

            lblCantidadPagos = new Label
            {
                Text = "Total de pagos: 0",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold), // ✅ Reducido de 11F
                Location = new Point(15, 8), // ✅ Ajustado
                AutoSize = true
            };
            panelResumen.Controls.Add(lblCantidadPagos);

            lblTotal = new Label
            {
                Text = "Total: $0.00",
                ForeColor = Color.FromArgb(46, 204, 113),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold), // ✅ Reducido de 14F
                Location = new Point(15, 26), // ✅ Ajustado
                AutoSize = true
            };
            panelResumen.Controls.Add(lblTotal);
        }

        private async void CargarProveedores()
        {
            try
            {
                cboProveedor.Items.Clear();
                cboProveedor.Items.Add("Todos los proveedores");

                using var connection = new SqlConnection(connectionString);
                var query = "SELECT DISTINCT Proveedor FROM PagosProveedores WHERE Proveedor IS NOT NULL ORDER BY Proveedor";
                using var cmd = new SqlCommand(query, connection);

                connection.Open();
                using var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    var proveedor = reader["Proveedor"].ToString();
                    if (!string.IsNullOrEmpty(proveedor))
                    {
                        cboProveedor.Items.Add(proveedor);
                    }
                }

                cboProveedor.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar proveedores: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarPagosDelDia()
        {
            dtpDesde.Value = DateTime.Today;
            dtpHasta.Value = DateTime.Today;
            CargarPagosPorFecha(DateTime.Today, DateTime.Today);
        }

        private async void CargarPagosPorFecha(DateTime desde, DateTime hasta)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);

                // ✅ SIMPLIFICADO: Query sin conversión de fechas compleja
                var query = @"
            SELECT 
                Id,
                FechaPago,
                Proveedor,
                Monto,
                NumeroCajero,
                UsuarioRegistro,
                Observaciones,
                NumeroRemito,
                Origen,
                NombreEquipo
            FROM PagosProveedores
            WHERE FechaPago >= @desde AND FechaPago < @hasta
            ORDER BY FechaPago DESC";

                using var adapter = new SqlDataAdapter(query, connection);

                // ✅ CORREGIDO: Simplificar parámetros de fecha
                adapter.SelectCommand.Parameters.AddWithValue("@desde", desde.Date);
                adapter.SelectCommand.Parameters.AddWithValue("@hasta", hasta.Date.AddDays(1)); // Incluye todo el día "hasta"

                var dt = new DataTable();
                adapter.Fill(dt);

                // ✅ Debug: Mostrar cantidad de registros encontrados
                System.Diagnostics.Debug.WriteLine($"📊 Registros encontrados: {dt.Rows.Count}");
                System.Diagnostics.Debug.WriteLine($"📅 Rango: {desde:yyyy-MM-dd} hasta {hasta:yyyy-MM-dd}");

                // ✅ Debug: Mostrar columnas disponibles
                System.Diagnostics.Debug.WriteLine("📊 Columnas disponibles en el DataTable:");
                foreach (DataColumn column in dt.Columns)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {column.ColumnName} ({column.DataType.Name})");
                }

                datosOriginales = dt.Copy();
                dgvPagos.DataSource = dt;
                FormatearColumnas();
                AplicarFiltros();
                ActualizarResumen(dt);

                // ✅ NUEVO: Mostrar mensaje si no hay datos
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show(
                        $"No se encontraron pagos en el rango de fechas seleccionado.\n\n" +
                        $"Desde: {desde:dd/MM/yyyy}\n" +
                        $"Hasta: {hasta:dd/MM/yyyy}\n\n" +
                        $"Intente seleccionar un rango de fechas más amplio.",
                        "Sin resultados",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar pagos: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatearColumnas()
        {
            try
            {
                if (dgvPagos == null || dgvPagos.Columns == null || dgvPagos.Columns.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ No hay columnas para formatear");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"📊 Formateando {dgvPagos.Columns.Count} columnas");

                dgvPagos.SuspendLayout();

                // ✅ MODIFICADO: NumeroRemito ahora se oculta (ancho = 0)
                var configuraciones = new Dictionary<string, (string titulo, int ancho, string formato)>
                {
                    ["Id"] = ("ID", 0, null),
                    ["FechaPago"] = ("Fecha", 150, null),
                    ["Proveedor"] = ("Proveedor", 200, null),
                    ["Monto"] = ("Monto", 120, "C2"),
                    ["NumeroCajero"] = ("Cajero #", 80, null),
                    ["UsuarioRegistro"] = ("Usuario", 150, null),
                    ["NumeroRemito"] = ("Remito #", 0, null), // ✅ CAMBIADO: Ahora se oculta
                    ["Origen"] = ("Origen", 120, null),
                    ["Observaciones"] = ("Observaciones", 250, null),
                    ["NombreEquipo"] = ("Equipo", 0, null)
                };

                foreach (DataGridViewColumn col in dgvPagos.Columns)
                {
                    try
                    {
                        if (col == null) continue;

                        System.Diagnostics.Debug.WriteLine($"  Procesando columna: {col.Name}");

                        if (configuraciones.TryGetValue(col.Name, out var config))
                        {
                            if (!string.IsNullOrEmpty(config.titulo))
                            {
                                col.HeaderText = config.titulo;
                            }

                            if (config.ancho == 0)
                            {
                                col.Visible = false;
                                System.Diagnostics.Debug.WriteLine($"    ✓ Columna '{col.Name}' ocultada");
                            }
                            else
                            {
                                try
                                {
                                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                                    col.Width = config.ancho;
                                    System.Diagnostics.Debug.WriteLine($"    ✓ Columna '{col.Name}' configurada: {config.ancho}px");
                                }
                                catch (Exception exWidth)
                                {
                                    System.Diagnostics.Debug.WriteLine($"    ❌ Error estableciendo ancho de '{col.Name}': {exWidth.Message}");
                                }
                            }

                            if (!string.IsNullOrEmpty(config.formato))
                            {
                                try
                                {
                                    col.DefaultCellStyle.Format = config.formato;
                                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                                    System.Diagnostics.Debug.WriteLine($"    ✓ Formato aplicado a '{col.Name}': {config.formato}");
                                }
                                catch (Exception exFormat)
                                {
                                    System.Diagnostics.Debug.WriteLine($"    ❌ Error aplicando formato a '{col.Name}': {exFormat.Message}");
                                }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"    ⚠️ Columna '{col.Name}' no está en configuraciones");
                        }
                    }
                    catch (Exception exCol)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error procesando columna '{col?.Name ?? "null"}': {exCol.Message}");
                    }
                }

                dgvPagos.ResumeLayout();

                System.Diagnostics.Debug.WriteLine("✅ FormatearColumnas completado exitosamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error crítico en FormatearColumnas: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                MessageBox.Show(
                    $"Advertencia: No se pudo formatear correctamente las columnas.\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    $"Los datos se mostrarán sin formato personalizado.",
                    "Advertencia de Formato",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // ✅ MEJORADO: Método más robusto con validaciones adicionales
        private void ConfigurarColumna(string nombre, string titulo, int ancho, string formato = null)
        {
            // ✅ Verificar que la columna existe
            if (!dgvPagos.Columns.Contains(nombre))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Advertencia: La columna '{nombre}' no existe en el DataGridView");
                return;
            }

            try
            {
                var col = dgvPagos.Columns[nombre];

                // ✅ Verificar que la columna no sea null (defensa adicional)
                if (col == null)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Advertencia: La columna '{nombre}' es null");
                    return;
                }

                col.HeaderText = titulo;
                col.Width = ancho;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

                if (!string.IsNullOrEmpty(formato))
                {
                    col.DefaultCellStyle.Format = formato;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error configurando columna '{nombre}': {ex.Message}");
                // No lanzar excepción, solo registrar el error
            }
        }

        private void AplicarFiltros()
        {
            if (datosOriginales == null) return;

            try
            {
                var filtrados = datosOriginales.AsEnumerable();

                // Filtro por proveedor
                if (cboProveedor.SelectedIndex > 0)
                {
                    string proveedorSeleccionado = cboProveedor.SelectedItem.ToString();
                    filtrados = filtrados.Where(row =>
                        row["Proveedor"].ToString().Equals(proveedorSeleccionado, StringComparison.OrdinalIgnoreCase));
                }

                // ✅ CORREGIDO: Filtro por número de cajero
                if (!string.IsNullOrWhiteSpace(txtFiltroCajero.Text))
                {
                    if (int.TryParse(txtFiltroCajero.Text, out int numeroCajero))
                    {
                        filtrados = filtrados.Where(row =>
                            Convert.ToInt32(row["NumeroCajero"]) == numeroCajero);
                    }
                }

                var dtFiltrado = filtrados.Any() ? filtrados.CopyToDataTable() : datosOriginales.Clone();

                dgvPagos.DataSource = dtFiltrado;
                ActualizarResumen(dtFiltrado);
                ActualizarTituloConFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al aplicar filtros: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ActualizarResumen(DataTable dt)
        {
            int cantidad = dt.Rows.Count;
            decimal total = 0;

            foreach (DataRow row in dt.Rows)
            {
                if (decimal.TryParse(row["Monto"].ToString(), out decimal monto))
                {
                    total += monto;
                }
            }

            lblCantidadPagos.Text = $"Total de pagos: {cantidad}";
            lblTotal.Text = $"Total: {total:C2}";
        }

        private void ActualizarTituloConFiltros()
        {
            int cantidadFiltros = 0;
            if (cboProveedor.SelectedIndex > 0) cantidadFiltros++;
            if (!string.IsNullOrWhiteSpace(txtFiltroCajero.Text)) cantidadFiltros++;

            string filtrosTexto = cantidadFiltros > 0 ? $" ({cantidadFiltros} filtro{(cantidadFiltros > 1 ? "s" : "")} activo{(cantidadFiltros > 1 ? "s" : "")})" : "";

            int registrosFiltrados = dgvPagos.Rows.Count;
            int totalRegistros = datosOriginales?.Rows.Count ?? 0;

            lblTitulo.Text = $"💳 PAGOS A PROVEEDORES{filtrosTexto} - Mostrando {registrosFiltrados} de {totalRegistros}";
        }

        private void LimpiarFiltros()
        {
            cboProveedor.SelectedIndex = 0;
            txtFiltroCajero.Clear();
            AplicarFiltros();
        }

        private void BtnExportar_Click(object sender, EventArgs e)
        {
            if (dgvPagos.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Filter = "Archivo CSV|*.csv|Todos los archivos|*.*",
                DefaultExt = "csv",
                FileName = $"PagosProveedores_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ExportarACSV(dlg.FileName);
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

        private void ExportarACSV(string rutaArchivo)
        {
            var dt = (DataTable)dgvPagos.DataSource;
            var lineas = new List<string>();

            // Encabezados
            var encabezados = new List<string>();
            foreach (DataGridViewColumn col in dgvPagos.Columns)
            {
                if (col.Visible && col.Name != "Id")
                {
                    encabezados.Add($"\"{col.HeaderText}\"");
                }
            }
            lineas.Add(string.Join(",", encabezados));

            // Datos
            foreach (DataRow row in dt.Rows)
            {
                var valores = new List<string>();
                foreach (DataGridViewColumn col in dgvPagos.Columns)
                {
                    if (col.Visible && col.Name != "Id")
                    {
                        var valor = row[col.Name].ToString().Replace("\"", "\"\"");
                        valores.Add($"\"{valor}\"");
                    }
                }
                lineas.Add(string.Join(",", valores));
            }

            System.IO.File.WriteAllLines(rutaArchivo, lineas, System.Text.Encoding.UTF8);
        }
    }
}