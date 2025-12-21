using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClosedXML.Excel;

namespace Comercio.NET.Formularios
{
    public partial class ActualizacionExcelForm : Form
    {
        private DataTable dtExcel;
        private DataTable dtPrevia;
        private string archivoExcel = "";
        private string connectionString = "";

        // Controles UI
        private Button btnSeleccionarExcel;
        private Button btnCargarPrevia;
        private Button btnAplicarCambios;
        private Button btnCancelar;
        private DataGridView dgvPrevia;
        private ProgressBar progressBar;
        private Label lblEstado;
        private Label lblArchivo;
        private Label lblContadores;
        private CheckBox chkInsertarNuevos;

        public ActualizacionExcelForm()
        {
            InitializeComponent();
            CargarConfiguracion();
            ConfigurarControles();
            ConfigurarEventos();
        }

        private void InitializeComponent()
        {
            this.Text = "Actualización Masiva desde Excel";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }

        private void CargarConfiguracion()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                connectionString = config.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    MessageBox.Show("❌ Error: No se encontró la cadena de conexión en appsettings.json",
                        "Error de Configuración", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error cargando configuración:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void ConfigurarControles()
        {
            // ========== PANEL SUPERIOR ==========
            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.FromArgb(240, 248, 255),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Label título
            var lblTitulo = new Label
            {
                Text = "📊 Actualización Masiva de Productos desde Excel",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                AutoSize = true,
                Left = 20,
                Top = 15
            };

            // Label instrucciones
            var lblInstrucciones = new Label
            {
                Text = "Columnas requeridas: codigo, costo, porcentaje, precio\n" +
                       "Columnas opcionales (para productos nuevos): descripcion, marca, rubro, proveedor",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.DarkSlateGray,
                AutoSize = false,
                Left = 20,
                Top = 45,
                Width = 950,
                Height = 35
            };

            // Label archivo seleccionado
            lblArchivo = new Label
            {
                Text = "📄 Ningún archivo seleccionado",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.DarkOrange,
                AutoSize = true,
                Left = 20,
                Top = 85
            };

            // CheckBox para insertar nuevos
            chkInsertarNuevos = new CheckBox
            {
                Text = "✅ Insertar productos que no existen en BD",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 0),
                AutoSize = true,
                Left = 600,
                Top = 85,
                Checked = true
            };

            panelTop.Controls.AddRange(new Control[] {
                lblTitulo, lblInstrucciones, lblArchivo, chkInsertarNuevos
            });

            // ========== PANEL BOTONES SUPERIORES ==========
            var panelBotones = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White
            };

            btnSeleccionarExcel = new Button
            {
                Text = "📂 Seleccionar Excel",
                Width = 150,
                Height = 40,
                Left = 20,
                Top = 10,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            btnCargarPrevia = new Button
            {
                Text = "👁️ Vista Previa",
                Width = 150,
                Height = 40,
                Left = 190,
                Top = 10,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };

            btnAplicarCambios = new Button
            {
                Text = "✅ Aplicar Cambios",
                Width = 150,
                Height = 40,
                Left = 360,
                Top = 10,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };

            btnCancelar = new Button
            {
                Text = "❌ Cancelar",
                Width = 120,
                Height = 40,
                Left = 530,
                Top = 10,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };

            panelBotones.Controls.AddRange(new Control[] {
                btnSeleccionarExcel, btnCargarPrevia, btnAplicarCambios, btnCancelar
            });

            // ========== DATAGRIDVIEW ==========
            dgvPrevia = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9F),
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(240, 248, 255)
                }
            };

            // ========== PANEL INFERIOR ==========
            var panelBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle
            };

            lblEstado = new Label
            {
                Text = "ℹ️ Esperando archivo Excel...",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.DarkSlateGray,
                AutoSize = false,
                Left = 20,
                Top = 10,
                Width = 800,
                Height = 20
            };

            lblContadores = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                AutoSize = false,
                Left = 20,
                Top = 30,
                Width = 940,
                Height = 20,
                Visible = false
            };

            progressBar = new ProgressBar
            {
                Left = 20,
                Top = 55,
                Width = 940,
                Height = 15,
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            panelBottom.Controls.AddRange(new Control[] { lblEstado, lblContadores, progressBar });

            // ========== AGREGAR TODO AL FORM ==========
            this.Controls.Add(dgvPrevia);
            this.Controls.Add(panelBotones);
            this.Controls.Add(panelTop);
            this.Controls.Add(panelBottom);
        }

        private void ConfigurarEventos()
        {
            btnSeleccionarExcel.Click += BtnSeleccionarExcel_Click;
            btnCargarPrevia.Click += BtnCargarPrevia_Click;
            btnAplicarCambios.Click += BtnAplicarCambios_Click;
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    this.Close();
                }
            };
        }

        // ==================== EVENTOS ====================

        private void BtnSeleccionarExcel_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Seleccionar archivo Excel";
                openFileDialog.Filter = "Archivos Excel (*.xls;*.xlsx)|*.xls;*.xlsx|Todos los archivos (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    archivoExcel = openFileDialog.FileName;
                    lblArchivo.Text = $"📄 {Path.GetFileName(archivoExcel)}";
                    lblArchivo.ForeColor = Color.Green;
                    btnCargarPrevia.Enabled = true;
                    lblEstado.Text = "✅ Archivo cargado. Presione 'Vista Previa' para continuar.";
                    lblContadores.Visible = false;
                }
            }
        }

        private async void BtnCargarPrevia_Click(object sender, EventArgs e)
        {
            await CargarVistaPreviaAsync();
        }

        private async void BtnAplicarCambios_Click(object sender, EventArgs e)
        {
            await AplicarCambiosAsync();
        }

        // ==================== LÓGICA PRINCIPAL ====================

        private async Task CargarVistaPreviaAsync()
        {
            try
            {
                btnCargarPrevia.Enabled = false;
                btnAplicarCambios.Enabled = false;
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                lblEstado.Text = "🔄 Leyendo archivo Excel...";
                lblContadores.Visible = false;
                this.Cursor = Cursors.WaitCursor;

                await Task.Run(() =>
                {
                    dtExcel = LeerExcelConClosedXML(archivoExcel);
                });

                if (dtExcel == null || dtExcel.Rows.Count == 0)
                {
                    MessageBox.Show("⚠️ El archivo Excel está vacío o no tiene datos válidos.",
                        "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!ValidarColumnasRequeridas(dtExcel))
                {
                    return;
                }

                lblEstado.Text = "🔄 Generando vista previa...";

                dtPrevia = await GenerarTablaPreviaAsync(dtExcel);

                // 🔹 NUEVO: Filtrar productos "SIN CAMBIOS" antes de mostrar
                var dtPreviaFiltrada = dtPrevia.AsEnumerable()
                    .Where(r => r["Accion"].ToString() != "SIN CAMBIOS")
                    .CopyToDataTable();

                // Ordenar por Estado antes de mostrar
                DataView dv = dtPreviaFiltrada.DefaultView;
                dv.Sort = "Estado ASC";
                var dtFinal = dv.ToTable();

                dgvPrevia.DataSource = dtFinal;
                FormatearDataGridView();

                ActualizarContadores();

                btnAplicarCambios.Enabled = true;
                lblEstado.Text = $"✅ Vista previa generada: {dtFinal.Rows.Count} productos requieren acción.";
                progressBar.Visible = false;
            }
            catch (IOException ioEx) when (ioEx.Message.Contains("being used by another process"))
            {
                MessageBox.Show(
                    $"⚠️ No se puede acceder al archivo Excel.\n\n" +
                    $"El archivo está siendo usado por otro proceso.\n" +
                    $"Por favor cierre el archivo en Excel y vuelva a intentar.\n\n" +
                    $"Archivo: {Path.GetFileName(archivoExcel)}",
                    "Archivo Bloqueado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                lblEstado.Text = "❌ Archivo en uso por otro proceso.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error al cargar vista previa:\n\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblEstado.Text = "❌ Error al cargar vista previa.";
            }
            finally
            {
                btnCargarPrevia.Enabled = true;
                progressBar.Visible = false;
                this.Cursor = Cursors.Default;
            }
        }

        private void ActualizarContadores()
        {
            if (dtPrevia == null || dtPrevia.Rows.Count == 0)
            {
                lblContadores.Visible = false;
                return;
            }

            int actualizar = dtPrevia.AsEnumerable().Count(r => r["Accion"].ToString() == "ACTUALIZAR");
            int insertar = dtPrevia.AsEnumerable().Count(r => r["Accion"].ToString() == "INSERTAR");
            int sinCambios = dtPrevia.AsEnumerable().Count(r => r["Accion"].ToString() == "SIN CAMBIOS");
            int omitir = dtPrevia.AsEnumerable().Count(r => r["Accion"].ToString() == "OMITIR");

            // 🔹 MODIFICADO: Actualizar mensaje para indicar que los "sin cambios" están ocultos
            lblContadores.Text = $"📊 Actualizar: {actualizar}  |  🆕 Insertar: {insertar}  |  ⚠️ Omitir: {omitir}  |  ✔️ Sin Cambios (ocultos): {sinCambios}";
            lblContadores.Visible = true;
        }

        private DataTable LeerExcelConClosedXML(string rutaArchivo)
        {
            var dt = new DataTable();

            using (var fileStream = new FileStream(rutaArchivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var workbook = new XLWorkbook(fileStream))
            {
                var worksheet = workbook.Worksheet(1);
                var firstRow = worksheet.FirstRowUsed();
                var lastColumnUsed = firstRow.LastCellUsed().Address.ColumnNumber;

                // Crear columnas desde la primera fila (encabezados)
                for (int col = 1; col <= lastColumnUsed; col++)
                {
                    string columnName = firstRow.Cell(col).GetString().Trim().ToLower();
                    if (!string.IsNullOrWhiteSpace(columnName))
                    {
                        dt.Columns.Add(columnName);
                    }
                }

                // Leer datos desde la segunda fila
                var rows = worksheet.RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    var dataRow = dt.NewRow();
                    bool isEmpty = true;

                    for (int col = 0; col < dt.Columns.Count; col++)
                    {
                        string value = row.Cell(col + 1).GetString().Trim();
                        dataRow[col] = value;

                        if (!string.IsNullOrWhiteSpace(value))
                            isEmpty = false;
                    }

                    if (!isEmpty)
                    {
                        dt.Rows.Add(dataRow);
                    }
                }
            }

            return dt;
        }

        private bool ValidarColumnasRequeridas(DataTable dt)
        {
            string[] columnasRequeridas = { "codigo", "costo", "porcentaje", "precio" };
            var columnasFaltantes = columnasRequeridas
                .Where(col => !dt.Columns.Contains(col))
                .ToList();

            if (columnasFaltantes.Any())
            {
                MessageBox.Show(
                    $"⚠️ El archivo Excel NO contiene las columnas requeridas:\n\n" +
                    $"Faltantes: {string.Join(", ", columnasFaltantes)}\n\n" +
                    $"Columnas encontradas: {string.Join(", ", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}",
                    "Error de Estructura",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private async Task<DataTable> GenerarTablaPreviaAsync(DataTable dtExcel)
        {
            var dtPrevia = new DataTable();
            dtPrevia.Columns.Add("Estado", typeof(string));
            dtPrevia.Columns.Add("Codigo", typeof(string));
            dtPrevia.Columns.Add("Descripcion", typeof(string));
            dtPrevia.Columns.Add("Costo_Actual", typeof(decimal));
            dtPrevia.Columns.Add("Costo_Nuevo", typeof(decimal));
            dtPrevia.Columns.Add("Porcentaje_Actual", typeof(decimal));
            dtPrevia.Columns.Add("Porcentaje_Nuevo", typeof(decimal));
            dtPrevia.Columns.Add("Precio_Actual", typeof(decimal));
            dtPrevia.Columns.Add("Precio_Nuevo", typeof(decimal));
            dtPrevia.Columns.Add("Accion", typeof(string));

            var culture = System.Globalization.CultureInfo.InvariantCulture;
            var numberStyle = System.Globalization.NumberStyles.Any;

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                foreach (DataRow rowExcel in dtExcel.Rows)
                {
                    string codigo = rowExcel["codigo"]?.ToString()?.Trim() ?? "";

                    if (string.IsNullOrEmpty(codigo))
                        continue;

                    // Obtener y normalizar valores
                    string costoStr = rowExcel["costo"]?.ToString()?.Trim().Replace(",", ".") ?? "";
                    string porcentajeStr = rowExcel["porcentaje"]?.ToString()?.Trim().Replace(",", ".") ?? "";
                    string precioStr = rowExcel["precio"]?.ToString()?.Trim().Replace(",", ".") ?? "";

                    // Validar datos numéricos
                    bool costoValido = decimal.TryParse(costoStr, numberStyle, culture, out decimal costoNuevo);
                    bool porcentajeValido = decimal.TryParse(porcentajeStr, numberStyle, culture, out decimal porcentajeNuevo);
                    bool precioValido = decimal.TryParse(precioStr, numberStyle, culture, out decimal precioNuevo);

                    if (!costoValido || !porcentajeValido || !precioValido)
                    {
                        string errores = "";
                        if (!costoValido) errores += $"Costo inválido: '{costoStr}' ";
                        if (!porcentajeValido) errores += $"Porcentaje inválido: '{porcentajeStr}' ";
                        if (!precioValido) errores += $"Precio inválido: '{precioStr}' ";

                        dtPrevia.Rows.Add("⚠️ ERROR", codigo, $"Datos numéricos inválidos: {errores}", 0, 0, 0, 0, 0, 0, "OMITIR");
                        continue;
                    }

                    // Buscar producto en BD
                    var query = "SELECT Descripcion, Costo, Porcentaje, Precio FROM Productos WHERE Codigo = @Codigo";
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigo);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // PRODUCTO EXISTE - Verificar si hay cambios
                                string descripcion = reader["Descripcion"] == DBNull.Value ? "" : reader["Descripcion"].ToString();
                                decimal costoActual = reader["Costo"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Costo"]);
                                decimal porcentajeActual = reader["Porcentaje"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Porcentaje"]);
                                decimal precioActual = reader["Precio"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Precio"]);

                                // Verificar si hay cambios en el precio
                                bool hayCambios = (precioNuevo != precioActual);

                                string accion;
                                string estado;

                                if (hayCambios)
                                {
                                    accion = "ACTUALIZAR";
                                    estado = "🔄 ACTUALIZAR";
                                }
                                else
                                {
                                    accion = "SIN CAMBIOS";
                                    estado = "✔️ SIN CAMBIOS";
                                }

                                // 🔹 IMPORTANTE: Agregar TODOS los registros (incluso "SIN CAMBIOS")
                                // El filtrado se hace después en CargarVistaPreviaAsync
                                dtPrevia.Rows.Add(
                                    estado,
                                    codigo,
                                    descripcion,
                                    costoActual,
                                    costoNuevo,
                                    porcentajeActual,
                                    porcentajeNuevo,
                                    precioActual,
                                    precioNuevo,
                                    accion
                                );
                            }
                            else
                            {
                                // PRODUCTO NO EXISTE - INSERT
                                if (chkInsertarNuevos.Checked)
                                {
                                    string descripcion = rowExcel.Table.Columns.Contains("descripcion")
                                        ? rowExcel["descripcion"]?.ToString() ?? "SIN DESCRIPCIÓN"
                                        : "SIN DESCRIPCIÓN";

                                    dtPrevia.Rows.Add(
                                        "🆕 NUEVO",
                                        codigo,
                                        descripcion,
                                        0,
                                        costoNuevo,
                                        0,
                                        porcentajeNuevo,
                                        0,
                                        precioNuevo,
                                        "INSERTAR"
                                    );
                                }
                                else
                                {
                                    dtPrevia.Rows.Add(
                                        "⚠️ NO EXISTE",
                                        codigo,
                                        "Producto no encontrado en BD",
                                        0,
                                        costoNuevo,
                                        0,
                                        porcentajeNuevo,
                                        0,
                                        precioNuevo,
                                        "OMITIR"
                                    );
                                }
                            }
                        }
                    }
                }
            }

            return dtPrevia;
        }

        private void FormatearDataGridView()
        {
            dgvPrevia.Columns["Accion"].Visible = false;

            dgvPrevia.Columns["Estado"].Width = 120;
            dgvPrevia.Columns["Codigo"].Width = 110;
            dgvPrevia.Columns["Descripcion"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // Formatear columnas numéricas
            foreach (DataGridViewColumn col in dgvPrevia.Columns)
            {
                if (col.Name.Contains("Costo") || col.Name.Contains("Precio") || col.Name.Contains("Porcentaje"))
                {
                    col.DefaultCellStyle.Format = "N2";
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            // Colorear filas según estado
            foreach (DataGridViewRow row in dgvPrevia.Rows)
            {
                string estado = row.Cells["Estado"].Value?.ToString() ?? "";

                if (estado.Contains("ACTUALIZAR"))
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 205);
                }
                else if (estado.Contains("NUEVO"))
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(212, 237, 218);
                }
                else if (estado.Contains("SIN CAMBIOS"))
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(230, 230, 230);
                }
                else if (estado.Contains("ERROR") || estado.Contains("NO EXISTE"))
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(248, 215, 218);
                }
            }
        }

        private async Task AplicarCambiosAsync()
        {
            if (dtPrevia == null || dtPrevia.Rows.Count == 0)
            {
                MessageBox.Show("⚠️ No hay cambios para aplicar.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int actualizar = dtPrevia.AsEnumerable().Count(r => r["Accion"].ToString() == "ACTUALIZAR");
            int insertar = dtPrevia.AsEnumerable().Count(r => r["Accion"].ToString() == "INSERTAR");
            int sinCambios = dtPrevia.AsEnumerable().Count(r => r["Accion"].ToString() == "SIN CAMBIOS");
            int omitir = dtPrevia.AsEnumerable().Count(r => r["Accion"].ToString() == "OMITIR");

            if (actualizar == 0 && insertar == 0)
            {
                MessageBox.Show("⚠️ No hay productos para actualizar ni insertar.", "Sin Cambios", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var resultado = MessageBox.Show(
                $"¿Confirma aplicar los cambios?\n\n" +
                $"Registros a procesar: {actualizar + insertar}\n" +
                $"• Actualizar: {actualizar}\n" +
                $"• Insertar: {insertar}\n" +
                $"• Sin Cambios: {sinCambios}\n" +
                $"• Omitir: {omitir}",
                "Confirmar Cambios",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (resultado != DialogResult.Yes)
                return;

            try
            {
                btnAplicarCambios.Enabled = false;
                btnCargarPrevia.Enabled = false;
                btnSeleccionarExcel.Enabled = false;
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 0;
                progressBar.Maximum = dtPrevia.Rows.Count;
                this.Cursor = Cursors.WaitCursor;

                int actualizados = 0;
                int insertados = 0;
                int omitidos = 0;
                int errores = 0;

                // 🔹 NUEVO: StringBuilder para acumular errores
                var erroresDetalle = new System.Text.StringBuilder();
                int maxErroresAMostrar = 10; // Solo mostrar los primeros 10 errores

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    foreach (DataRow row in dtPrevia.Rows)
                    {
                        string accion = row["Accion"].ToString();
                        string codigo = row["Codigo"].ToString();

                        try
                        {
                            if (accion == "ACTUALIZAR")
                            {
                                await ActualizarProductoAsync(connection, row);
                                actualizados++;
                            }
                            else if (accion == "INSERTAR")
                            {
                                await InsertarProductoAsync(connection, row);
                                insertados++;
                            }
                            else
                            {
                                omitidos++;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Error procesando {codigo}: {ex.Message}");
                            errores++;

                            // 🔹 NUEVO: Acumular detalles de error
                            if (errores <= maxErroresAMostrar)
                            {
                                erroresDetalle.AppendLine($"[{errores}] Código: {codigo} | Acción: {accion}");
                                erroresDetalle.AppendLine($"    Error: {ex.Message}");
                                erroresDetalle.AppendLine();
                            }
                        }

                        progressBar.Value++;
                        lblEstado.Text = $"Procesando: {progressBar.Value}/{progressBar.Maximum} | Errores: {errores}";
                        Application.DoEvents();
                    }
                }

                // 🔹 MODIFICADO: Mostrar mensaje con detalles de errores
                string mensajeResultado = $"✅ Proceso completado:\n\n" +
                    $"• Actualizados: {actualizados}\n" +
                    $"• Insertados: {insertados}\n" +
                    $"• Omitidos: {omitidos}\n" +
                    $"• Errores: {errores}";

                if (errores > 0)
                {
                    mensajeResultado += $"\n\n⚠️ PRIMEROS {Math.Min(errores, maxErroresAMostrar)} ERRORES:\n\n";
                    mensajeResultado += erroresDetalle.ToString();

                    if (errores > maxErroresAMostrar)
                    {
                        mensajeResultado += $"\n... y {errores - maxErroresAMostrar} errores más.";
                    }

                    // Mostrar en un form con TextBox para scroll
                    var errorForm = new Form
                    {
                        Text = "Resultado del Proceso",
                        Size = new Size(800, 600),
                        StartPosition = FormStartPosition.CenterParent
                    };

                    var txtResultado = new TextBox
                    {
                        Dock = DockStyle.Fill,
                        Multiline = true,
                        ScrollBars = ScrollBars.Both,
                        Font = new Font("Consolas", 9F),
                        Text = mensajeResultado,
                        ReadOnly = true
                    };

                    var btnCerrar = new Button
                    {
                        Text = "Cerrar",
                        Dock = DockStyle.Bottom,
                        Height = 40,
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                    };

                    btnCerrar.Click += (s, ev) => errorForm.Close();

                    errorForm.Controls.Add(txtResultado);
                    errorForm.Controls.Add(btnCerrar);
                    errorForm.ShowDialog();
                }
                else
                {
                    MessageBox.Show(mensajeResultado, "Resultado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                if (actualizados > 0 || insertados > 0)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error crítico:\n\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnAplicarCambios.Enabled = true;
                btnCargarPrevia.Enabled = true;
                btnSeleccionarExcel.Enabled = true;
                progressBar.Visible = false;
                this.Cursor = Cursors.Default;
            }
        }

        private async Task ActualizarProductoAsync(SqlConnection connection, DataRow row)
        {
            string codigo = row["Codigo"].ToString();
            decimal costoNuevo = Convert.ToDecimal(row["Costo_Nuevo"]);
            decimal porcentajeNuevo = Convert.ToDecimal(row["Porcentaje_Nuevo"]);
            decimal precioNuevo = Convert.ToDecimal(row["Precio_Nuevo"]);
            decimal costoAnterior = Convert.ToDecimal(row["Costo_Actual"]);
            decimal porcentajeAnterior = Convert.ToDecimal(row["Porcentaje_Actual"]);
            decimal precioAnterior = Convert.ToDecimal(row["Precio_Actual"]);

            // 🔹 CORRECCIÓN: Eliminar FechaModificacion
            var queryUpdate = @"
        UPDATE Productos 
        SET Costo = @Costo, 
            Porcentaje = @Porcentaje, 
            Precio = @Precio
        WHERE Codigo = @Codigo";

            using (var cmd = new SqlCommand(queryUpdate, connection))
            {
                cmd.Parameters.AddWithValue("@Codigo", codigo);
                cmd.Parameters.AddWithValue("@Costo", costoNuevo);
                cmd.Parameters.AddWithValue("@Porcentaje", porcentajeNuevo);
                cmd.Parameters.AddWithValue("@Precio", precioNuevo);

                int filasAfectadas = await cmd.ExecuteNonQueryAsync();

                if (filasAfectadas == 0)
                {
                    throw new Exception($"No se pudo actualizar el producto {codigo}. No se encontró en la BD.");
                }
            }

            await RegistrarAuditoriaAsync(connection, "UPDATE", codigo,
                costoAnterior, costoNuevo,
                porcentajeAnterior, porcentajeNuevo,
                precioAnterior, precioNuevo);
        }

        private async Task InsertarProductoAsync(SqlConnection connection, DataRow row)
        {
            string codigo = row["Codigo"].ToString();
            decimal costoNuevo = Convert.ToDecimal(row["Costo_Nuevo"]);
            decimal porcentajeNuevo = Convert.ToDecimal(row["Porcentaje_Nuevo"]);
            decimal precioNuevo = Convert.ToDecimal(row["Precio_Nuevo"]);

            var rowExcel = dtExcel.AsEnumerable()
                .FirstOrDefault(r => r["codigo"]?.ToString()?.Trim() == codigo);

            if (rowExcel == null)
            {
                throw new Exception($"No se encontró el código {codigo} en el archivo Excel");
            }

            string descripcion = rowExcel.Table.Columns.Contains("descripcion")
                ? rowExcel["descripcion"]?.ToString()?.Trim() ?? "SIN DESCRIPCIÓN"
                : "SIN DESCRIPCIÓN";

            string marca = rowExcel.Table.Columns.Contains("marca")
                ? rowExcel["marca"]?.ToString()?.Trim() ?? ""
                : "";

            string rubro = rowExcel.Table.Columns.Contains("rubro")
                ? rowExcel["rubro"]?.ToString()?.Trim() ?? ""
                : "";

            string proveedor = rowExcel.Table.Columns.Contains("proveedor")
                ? rowExcel["proveedor"]?.ToString()?.Trim() ?? ""
                : "";

            // 🔹 CORRECCIÓN: Eliminar FechaCreacion
            var queryInsert = @"
                INSERT INTO Productos (Codigo, Descripcion, Costo, Porcentaje, Precio, Marca, Rubro, Proveedor)
                VALUES (@Codigo, @Descripcion, @Costo, @Porcentaje, @Precio, @Marca, @Rubro, @Proveedor)";

            using (var cmd = new SqlCommand(queryInsert, connection))
            {
                cmd.Parameters.AddWithValue("@Codigo", codigo);
                cmd.Parameters.AddWithValue("@Descripcion", descripcion);
                cmd.Parameters.AddWithValue("@Costo", costoNuevo);
                cmd.Parameters.AddWithValue("@Porcentaje", porcentajeNuevo);
                cmd.Parameters.AddWithValue("@Precio", precioNuevo);
                cmd.Parameters.AddWithValue("@Marca", string.IsNullOrEmpty(marca) ? (object)DBNull.Value : marca);
                cmd.Parameters.AddWithValue("@Rubro", string.IsNullOrEmpty(rubro) ? (object)DBNull.Value : rubro);
                cmd.Parameters.AddWithValue("@Proveedor", string.IsNullOrEmpty(proveedor) ? (object)DBNull.Value : proveedor);

                await cmd.ExecuteNonQueryAsync();
            }

            await RegistrarAuditoriaAsync(connection, "INSERT", codigo,
                0, costoNuevo,
                0, porcentajeNuevo,
                0, precioNuevo);
        }

        private async Task RegistrarAuditoriaAsync(SqlConnection connection, string accion, string codigo,
            decimal costoAnterior, decimal costoNuevo,
            decimal porcentajeAnterior, decimal porcentajeNuevo,
            decimal precioAnterior, decimal precioNuevo)
        {
            try
            {
                var queryAuditoria = @"
                    INSERT INTO AuditoriaProductos (
                        Codigo, Accion, 
                        CostoAnterior, CostoNuevo, 
                        PorcentajeAnterior, PorcentajeNuevo, 
                        PrecioAnterior, PrecioNuevo, 
                        Usuario, Fecha, Origen
                    )
                    VALUES (
                        @Codigo, @Accion, 
                        @CostoAnterior, @CostoNuevo, 
                        @PorcentajeAnterior, @PorcentajeNuevo, 
                        @PrecioAnterior, @PrecioNuevo, 
                        @Usuario, GETDATE(), 'ActualizacionExcel'
                    )";

                using (var cmd = new SqlCommand(queryAuditoria, connection))
                {
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.Parameters.AddWithValue("@Accion", accion);
                    cmd.Parameters.AddWithValue("@CostoAnterior", costoAnterior);
                    cmd.Parameters.AddWithValue("@CostoNuevo", costoNuevo);
                    cmd.Parameters.AddWithValue("@PorcentajeAnterior", porcentajeAnterior);
                    cmd.Parameters.AddWithValue("@PorcentajeNuevo", porcentajeNuevo);
                    cmd.Parameters.AddWithValue("@PrecioAnterior", precioAnterior);
                    cmd.Parameters.AddWithValue("@PrecioNuevo", precioNuevo);
                    cmd.Parameters.AddWithValue("@Usuario", Environment.UserName);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error en auditoría para {codigo}: {ex.Message}");
            }
        }
    }
}