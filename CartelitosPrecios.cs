using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Comercio.NET.Servicios;
using Comercio.NET.Formularios;
using Comercio.NET;
using System.IO;
using System.Text.Json;

namespace Comercio.NET.Formularios
{
    public partial class CartelitosPrecios : Form
    {
        private List<ProductoCartelito> productosSeleccionados;
        private DataTable tablaProductos;
        
        // NUEVO: Ruta del archivo para persistencia
        private readonly string archivoCartelitos = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cartelitos_productos.json");
        
        // Controles del formulario
        private TextBox txtCodigoProducto;
        private DataGridView dgvProductosSeleccionados;
        private Label lblInstrucciones;
        private Label lblTotalProductos;
        private Button btnAgregarProducto;
        private Button btnEliminarSeleccionado;
        private Button btnLimpiarLista;
        private GroupBox gbTamañosCartel;
        private RadioButton rbTamañoEstandar;
        private RadioButton rbTamañoPerfumeria;
        private RadioButton rbTamañoOferta;
        private Button btnVistaPrevia;
        private Button btnImprimir;
        private Button btnCerrar;
        private Panel panelInferior;
        private TableLayoutPanel layoutPrincipal;

        public CartelitosPrecios()
        {
            InitializeComponent();
            productosSeleccionados = new List<ProductoCartelito>();
            ConfigurarFormulario();
            ConfigurarEventos();
            
            // NUEVO: Cargar productos guardados
            CargarProductosGuardados();
        }

        // NUEVO: Método para cargar productos guardados
        private void CargarProductosGuardados()
        {
            try
            {
                if (File.Exists(archivoCartelitos))
                {
                    string jsonContent = File.ReadAllText(archivoCartelitos);
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        var productosGuardados = System.Text.Json.JsonSerializer.Deserialize<List<ProductoCartelito>>(jsonContent);
                        if (productosGuardados != null && productosGuardados.Count > 0)
                        {
                            productosSeleccionados.AddRange(productosGuardados);
                            ActualizarDataGridView();
                            ActualizarContador();
                            
                            System.Diagnostics.Debug.WriteLine($"✅ Cargados {productosGuardados.Count} productos desde archivo");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando productos: {ex.Message}");
                // No mostrar error al usuario, simplemente continuar sin productos guardados
            }
        }

        // NUEVO: Método para guardar productos
        private void GuardarProductos()
        {
            try
            {
                if (productosSeleccionados.Count > 0)
                {
                    string jsonContent = System.Text.Json.JsonSerializer.Serialize(productosSeleccionados, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    File.WriteAllText(archivoCartelitos, jsonContent);
                    System.Diagnostics.Debug.WriteLine($"✅ Guardados {productosSeleccionados.Count} productos");
                }
                else
                {
                    // Si no hay productos, eliminar el archivo
                    if (File.Exists(archivoCartelitos))
                    {
                        File.Delete(archivoCartelitos);
                        System.Diagnostics.Debug.WriteLine("✅ Archivo de productos eliminado (lista vacía)");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error guardando productos: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configuración del formulario
            this.Text = "Generador de Cartelitos de Precios";
            this.Size = new Size(1000, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.WhiteSmoke;
            this.Font = new Font("Segoe UI", 10F);

            // Layout principal
            layoutPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = Color.Transparent
            };

            // Configurar columnas y filas
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            layoutPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            layoutPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            // Panel de entrada de productos
            var panelEntrada = CrearPanelEntrada();
            layoutPrincipal.Controls.Add(panelEntrada, 0, 0);
            layoutPrincipal.SetColumnSpan(panelEntrada, 2);

            // DataGridView
            dgvProductosSeleccionados = CrearDataGridView();
            layoutPrincipal.Controls.Add(dgvProductosSeleccionados, 0, 1);

            // Panel de opciones
            var panelOpciones = CrearPanelOpciones();
            layoutPrincipal.Controls.Add(panelOpciones, 1, 1);

            // Panel inferior con botones
            panelInferior = CrearPanelInferior();
            layoutPrincipal.Controls.Add(panelInferior, 0, 2);
            layoutPrincipal.SetColumnSpan(panelInferior, 2);

            this.Controls.Add(layoutPrincipal);
            this.ResumeLayout(false);
        }

        private Panel CrearPanelEntrada()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 248, 255),
                Padding = new Padding(10)
            };

            // Instrucciones
            lblInstrucciones = new Label
            {
                Text = "Ingrese el código del producto y presione Enter o haga clic en Agregar:",
                Location = new Point(10, 10),
                Size = new Size(500, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 118, 210)
            };

            // TextBox para código
            txtCodigoProducto = new TextBox
            {
                Location = new Point(10, 35),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 11F),
                PlaceholderText = "Código producto..."
            };

            // Botón agregar
            btnAgregarProducto = new Button
            {
                Text = "Agregar",
                Location = new Point(170, 34),
                Size = new Size(80, 27),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnAgregarProducto.FlatAppearance.BorderSize = 0;

            // ✅ NUEVO: Botón con menú desplegable para importar modificados
            var btnImportarModificados = new Button
            {
                Text = "📅 Modificados ▼",
                Location = new Point(260, 34),
                Size = new Size(140, 27),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnImportarModificados.FlatAppearance.BorderSize = 0;

            // ✅ NUEVO: Crear menú contextual con opciones
            var menuImportar = new ContextMenuStrip();
            menuImportar.Font = new Font("Segoe UI", 9F);

            // Opción: Hoy
            var menuHoy = new ToolStripMenuItem
            {
                Text = "📅 Modificados Hoy",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            menuHoy.Click += async (s, e) => await ImportarProductosModificados(DateTime.Today);

            // Opción: Ayer
            var menuAyer = new ToolStripMenuItem
            {
                Text = "📅 Modificados Ayer"
            };
            menuAyer.Click += async (s, e) => await ImportarProductosModificados(DateTime.Today.AddDays(-1));

            // Opción: Esta semana
            var menuSemana = new ToolStripMenuItem
            {
                Text = "📅 Modificados esta Semana"
            };
            menuSemana.Click += async (s, e) => await ImportarProductosSemana();

            // Separador
            var separador = new ToolStripSeparator();

            // Opción: Seleccionar fecha personalizada
            var menuFechaPersonalizada = new ToolStripMenuItem
            {
                Text = "📆 Seleccionar Fecha...",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243)
            };
            menuFechaPersonalizada.Click += async (s, e) => await SeleccionarFechaPersonalizada();

            // Agregar opciones al menú
            menuImportar.Items.AddRange(new ToolStripItem[]
            {
        menuHoy,
        menuAyer,
        menuSemana,
        separador,
        menuFechaPersonalizada
            });

            // Evento del botón para mostrar el menú
            btnImportarModificados.Click += (s, e) =>
            {
                menuImportar.Show(btnImportarModificados, new Point(0, btnImportarModificados.Height));
            };

            // Label contador
            lblTotalProductos = new Label
            {
                Text = "Productos en lista: 0",
                Location = new Point(410, 38),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            panel.Controls.AddRange(new Control[] {
        lblInstrucciones, txtCodigoProducto, btnAgregarProducto, btnImportarModificados, lblTotalProductos
    });

            return panel;
        }

        // ✅ NUEVO: Método para importar productos de una fecha específica
        private async Task ImportarProductosModificados(DateTime fecha)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                // Obtener productos modificados en la fecha especificada
                var productos = await ObtenerProductosModificadosPorFechaAsync(fecha);

                if (productos == null || productos.Count == 0)
                {
                    string fechaTexto = fecha.Date == DateTime.Today ? "hoy" : fecha.ToString("dd/MM/yyyy");
                    MessageBox.Show(
                        $"No se encontraron productos modificados {fechaTexto}.\n\n" +
                        "Los productos se marcan como modificados cuando se actualiza su precio o stock.",
                        "Sin productos modificados",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                string fechaMostrar = fecha.Date == DateTime.Today ? "hoy" :
                                     fecha.Date == DateTime.Today.AddDays(-1) ? "ayer" :
                                     $"el {fecha:dd/MM/yyyy}";

                // Confirmar antes de importar
                var resultado = MessageBox.Show(
                    $"Se encontraron {productos.Count} producto(s) modificados {fechaMostrar}.\n\n" +
                    "¿Desea agregarlos a la lista de cartelitos?",
                    "Confirmar importación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resultado == DialogResult.Yes)
                {
                    // Agregar productos a la lista (evitando duplicados)
                    int agregados = 0;
                    int duplicados = 0;

                    foreach (var producto in productos)
                    {
                        // Verificar si ya está en la lista
                        if (!productosSeleccionados.Any(p => p.Codigo == producto.Codigo))
                        {
                            productosSeleccionados.Add(producto);
                            agregados++;
                        }
                        else
                        {
                            duplicados++;
                        }
                    }

                    // Actualizar vista
                    ActualizarDataGridView();
                    ActualizarContador();
                    GuardarProductos();

                    // Mostrar resultado
                    string mensaje = $"✅ Se agregaron {agregados} producto(s) modificados {fechaMostrar}.";
                    if (duplicados > 0)
                    {
                        mensaje += $"\n\n({duplicados} ya estaban en la lista)";
                    }

                    MessageBox.Show(mensaje, "Importación exitosa",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    System.Diagnostics.Debug.WriteLine($"✅ Importados {agregados} productos modificados {fechaMostrar}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al importar productos: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Error importando productos: {ex.Message}");
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        // ✅ NUEVO: Método para importar productos de la semana actual
        private async Task ImportarProductosSemana()
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                // Calcular inicio de la semana (lunes)
                DateTime hoy = DateTime.Today;
                int diasDesdeInicio = ((int)hoy.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                DateTime inicioSemana = hoy.AddDays(-diasDesdeInicio);

                // Obtener productos modificados en la semana
                var productos = await ObtenerProductosModificadosRangoAsync(inicioSemana, hoy);

                if (productos == null || productos.Count == 0)
                {
                    MessageBox.Show(
                        $"No se encontraron productos modificados esta semana.\n\n" +
                        $"Rango: {inicioSemana:dd/MM/yyyy} - {hoy:dd/MM/yyyy}",
                        "Sin productos modificados",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // Confirmar antes de importar
                var resultado = MessageBox.Show(
                    $"Se encontraron {productos.Count} producto(s) modificados esta semana.\n\n" +
                    $"Período: {inicioSemana:dd/MM/yyyy} al {hoy:dd/MM/yyyy}\n\n" +
                    "¿Desea agregarlos a la lista de cartelitos?",
                    "Confirmar importación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resultado == DialogResult.Yes)
                {
                    // Agregar productos a la lista (evitando duplicados)
                    int agregados = 0;
                    int duplicados = 0;

                    foreach (var producto in productos)
                    {
                        if (!productosSeleccionados.Any(p => p.Codigo == producto.Codigo))
                        {
                            productosSeleccionados.Add(producto);
                            agregados++;
                        }
                        else
                        {
                            duplicados++;
                        }
                    }

                    // Actualizar vista
                    ActualizarDataGridView();
                    ActualizarContador();
                    GuardarProductos();

                    // Mostrar resultado
                    string mensaje = $"✅ Se agregaron {agregados} producto(s) de esta semana.";
                    if (duplicados > 0)
                    {
                        mensaje += $"\n\n({duplicados} ya estaban en la lista)";
                    }

                    MessageBox.Show(mensaje, "Importación exitosa",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    System.Diagnostics.Debug.WriteLine($"✅ Importados {agregados} productos de la semana");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al importar productos: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        // ✅ NUEVO: Método para seleccionar fecha personalizada
        private async Task SeleccionarFechaPersonalizada()
        {
            // Crear formulario personalizado para selección de fecha
            using (var formFecha = new Form())
            {
                formFecha.Text = "Seleccionar Fecha";
                formFecha.Size = new Size(420, 375);
                formFecha.StartPosition = FormStartPosition.CenterParent;
                formFecha.FormBorderStyle = FormBorderStyle.FixedDialog;
                formFecha.MaximizeBox = false;
                formFecha.MinimizeBox = false;

                var lblTitulo = new Label
                {
                    Text = "Seleccione la fecha de modificación:",
                    Location = new Point(20, 20),
                    Size = new Size(350, 20),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };

                var dtpFecha = new MonthCalendar
                {
                    Location = new Point(70, 50),
                    MaxDate = DateTime.Today,
                    MaxSelectionCount = 1
                };

                var btnAceptar = new Button
                {
                    Text = "Importar",
                    Location = new Point(180, 270),
                    Size = new Size(100, 30),
                    BackColor = Color.FromArgb(76, 175, 80),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };
                btnAceptar.FlatAppearance.BorderSize = 0;

                var btnCancelar = new Button
                {
                    Text = "Cancelar",
                    Location = new Point(290, 270),
                    Size = new Size(90, 30),
                    BackColor = Color.FromArgb(158, 158, 158),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };
                btnCancelar.FlatAppearance.BorderSize = 0;

                btnAceptar.Click += async (s, e) =>
                {
                    formFecha.DialogResult = DialogResult.OK;
                    formFecha.Close();
                };

                btnCancelar.Click += (s, e) =>
                {
                    formFecha.DialogResult = DialogResult.Cancel;
                    formFecha.Close();
                };

                formFecha.Controls.AddRange(new Control[]
                {
            lblTitulo,
            dtpFecha,
            btnAceptar,
            btnCancelar
                });

                if (formFecha.ShowDialog() == DialogResult.OK)
                {
                    await ImportarProductosModificados(dtpFecha.SelectionStart);
                }
            }
        }

        // ✅ NUEVO: Manejador del botón Importar Modificados Hoy
        private async void BtnImportarModificadosHoy_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                var btn = sender as Button;
                if (btn != null)
                {
                    btn.Enabled = false;
                    btn.Text = "⏳ Cargando...";
                }

                // Obtener productos modificados hoy
                var productosHoy = await ObtenerProductosModificadosHoyAsync();

                if (productosHoy == null || productosHoy.Count == 0)
                {
                    MessageBox.Show(
                        "No se encontraron productos modificados hoy.\n\n" +
                        "Los productos se marcan como modificados cuando se actualiza su precio o stock desde 'Actualización Rápida'.",
                        "Sin productos modificados",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // Confirmar antes de importar
                var resultado = MessageBox.Show(
                    $"Se encontraron {productosHoy.Count} producto(s) modificados hoy.\n\n" +
                    "¿Desea agregarlos a la lista de cartelitos?",
                    "Confirmar importación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resultado == DialogResult.Yes)
                {
                    // Agregar productos a la lista (evitando duplicados)
                    int agregados = 0;
                    int duplicados = 0;

                    foreach (var producto in productosHoy)
                    {
                        // Verificar si ya está en la lista
                        if (!productosSeleccionados.Any(p => p.Codigo == producto.Codigo))
                        {
                            productosSeleccionados.Add(producto);
                            agregados++;
                        }
                        else
                        {
                            duplicados++;
                        }
                    }

                    // Actualizar vista
                    ActualizarDataGridView();
                    ActualizarContador();
                    GuardarProductos();

                    // Mostrar resultado
                    string mensaje = $"✅ Se agregaron {agregados} producto(s) modificados hoy.";
                    if (duplicados > 0)
                    {
                        mensaje += $"\n\n({duplicados} ya estaban en la lista)";
                    }

                    MessageBox.Show(mensaje, "Importación exitosa",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    System.Diagnostics.Debug.WriteLine($"✅ Importados {agregados} productos modificados hoy");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al importar productos: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Error importando productos: {ex.Message}");
            }
            finally
            {
                this.Cursor = Cursors.Default;
                var btn = sender as Button;
                if (btn != null)
                {
                    btn.Enabled = true;
                    btn.Text = "📅 Modificados Hoy";
                }
            }
        }

        // ✅ NUEVO: Método para obtener productos modificados hoy desde la BD
        private async Task<List<ProductoCartelito>> ObtenerProductosModificadosHoyAsync()
        {
            var productos = new List<ProductoCartelito>();

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    // Query para obtener productos modificados hoy
                    var query = @"SELECT codigo, descripcion, precio, marca, rubro 
                          FROM Productos 
                          WHERE CAST(modificado AS DATE) = CAST(GETDATE() AS DATE)
                          ORDER BY descripcion";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        await connection.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                productos.Add(new ProductoCartelito
                                {
                                    Codigo = reader["codigo"].ToString(),
                                    Descripcion = reader["descripcion"].ToString(),
                                    Precio = Convert.ToDecimal(reader["precio"]),
                                    Marca = reader["marca"].ToString(),
                                    Rubro = reader["rubro"].ToString()
                                });
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"📅 Encontrados {productos.Count} productos modificados hoy");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error obteniendo productos modificados: {ex.Message}");
                throw;
            }

            return productos;
        }

        // ✅ NUEVO: Método para obtener productos modificados en una fecha específica
        private async Task<List<ProductoCartelito>> ObtenerProductosModificadosPorFechaAsync(DateTime fecha)
        {
            var productos = new List<ProductoCartelito>();

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    // Query para obtener productos modificados en una fecha específica
                    var query = @"SELECT codigo, descripcion, precio, marca, rubro 
                          FROM Productos 
                          WHERE CAST(modificado AS DATE) = @fecha
                          ORDER BY descripcion";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                        await connection.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                productos.Add(new ProductoCartelito
                                {
                                    Codigo = reader["codigo"].ToString(),
                                    Descripcion = reader["descripcion"].ToString(),
                                    Precio = Convert.ToDecimal(reader["precio"]),
                                    Marca = reader["marca"].ToString(),
                                    Rubro = reader["rubro"].ToString()
                                });
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"📅 Encontrados {productos.Count} productos modificados el {fecha:dd/MM/yyyy}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error obteniendo productos modificados: {ex.Message}");
                throw;
            }

            return productos;
        }
        // ✅ NUEVO: Método para obtener productos modificados en un rango de fechas
        private async Task<List<ProductoCartelito>> ObtenerProductosModificadosRangoAsync(DateTime desde, DateTime hasta)
        {
            var productos = new List<ProductoCartelito>();

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    // Query para obtener productos modificados en un rango de fechas
                    var query = @"SELECT codigo, descripcion, precio, marca, rubro, modificado
                          FROM Productos 
                          WHERE CAST(modificado AS DATE) BETWEEN @desde AND @hasta
                          ORDER BY modificado DESC, descripcion";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@desde", desde.Date);
                        cmd.Parameters.AddWithValue("@hasta", hasta.Date);
                        await connection.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                productos.Add(new ProductoCartelito
                                {
                                    Codigo = reader["codigo"].ToString(),
                                    Descripcion = reader["descripcion"].ToString(),
                                    Precio = Convert.ToDecimal(reader["precio"]),
                                    Marca = reader["marca"].ToString(),
                                    Rubro = reader["rubro"].ToString()
                                });
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"📅 Encontrados {productos.Count} productos modificados entre {desde:dd/MM/yyyy} y {hasta:dd/MM/yyyy}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error obteniendo productos modificados: {ex.Message}");
                throw;
            }

            return productos;
        }

        private DataGridView CrearDataGridView()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10, 0, 5, 0),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.White,
                GridColor = Color.FromArgb(230, 230, 230),
                AllowUserToResizeColumns = true, // NUEVO: Permitir redimensionar columnas
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                RowHeadersVisible = false
            };

            // Estilos
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.ColumnHeadersHeight = 35;

            dgv.DefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.ForeColor = Color.Black;

            // Selección menos agresiva: gris claro y texto negro
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dgv.DefaultCellStyle.SelectionForeColor = Color.Black;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            dgv.RowTemplate.Height = 28;

            // Columnas con anchos específicos y mejor legibilidad
            var colCodigo = new DataGridViewTextBoxColumn
            {
                Name = "Codigo",
                HeaderText = "Código",
                Width = 80,
                MinimumWidth = 60,
                Resizable = DataGridViewTriState.True
            };

            var colDescripcion = new DataGridViewTextBoxColumn
            {
                Name = "Descripcion",
                HeaderText = "Descripción",
                Width = 280, // AUMENTADO para mejor lectura
                MinimumWidth = 200,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill // Se expande con el formulario
            };

            var colPrecio = new DataGridViewTextBoxColumn
            {
                Name = "Precio",
                HeaderText = "Precio",
                Width = 80, // AUMENTADO para mejor lectura de precios
                MinimumWidth = 60,
                Resizable = DataGridViewTriState.True,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "C2",
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold), // Precio en negrita
                    ForeColor = Color.FromArgb(0, 100, 0) // Verde oscuro para precios
                }
            };

            var colMarca = new DataGridViewTextBoxColumn
            {
                Name = "Marca",
                HeaderText = "Marca",
                Width = 140,
                MinimumWidth = 100,
                Resizable = DataGridViewTriState.True
            };

            dgv.Columns.AddRange(new DataGridViewColumn[] { colCodigo, colDescripcion, colPrecio, colMarca });

            return dgv;
        }

        private Panel CrearPanelOpciones()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 0, 5, 0),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Título
            var lblTitulo = new Label
            {
                Text = "OPCIONES DE IMPRESIÓN",
                Location = new Point(10, 10),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 118, 210)
            };

            // GroupBox para tamaños - ✅ AUMENTADO para 4 opciones
            gbTamañosCartel = new GroupBox
            {
                Text = "Tamaño del cartelito",
                Location = new Point(10, 45),
                Size = new Size(320, 220), // ✅ AUMENTADO de 180 a 220
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            // RadioButtons para tamaños
            rbTamañoEstandar = new RadioButton
            {
                Text = "Estándar (7x5 cm)\r\nProductos generales - A4",
                Location = new Point(15, 25),
                Size = new Size(290, 42),
                AutoSize = false,
                Checked = true,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };

            rbTamañoPerfumeria = new RadioButton
            {
                Text = "Perfumería (5x3 cm)\r\nProductos pequeños - A4",
                Location = new Point(15, 70),
                Size = new Size(290, 42),
                AutoSize = false,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };

            rbTamañoOferta = new RadioButton
            {
                Text = "Oferta (10x7 cm)\r\nProductos destacados - A4",
                Location = new Point(15, 115),
                Size = new Size(290, 42),
                AutoSize = false,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // ✅ NUEVO: RadioButton para impresora térmica
            var rbTamañoTermico = new RadioButton
            {
                Name = "rbTamañoTermico",
                Text = "🖨️ Térmico 70mm\r\nImpresora térmica POS",
                Location = new Point(15, 160),
                Size = new Size(290, 42),
                AutoSize = false,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(204, 102, 0) // Color naranja para destacar
            };

            gbTamañosCartel.Controls.AddRange(new Control[] {
        rbTamañoEstandar,
        rbTamañoPerfumeria,
        rbTamañoOferta,
        rbTamañoTermico // ✅ NUEVO
    });

            // Botones de acción - ✅ AJUSTADA POSICIÓN
            btnEliminarSeleccionado = new Button
            {
                Text = "Eliminar\nSeleccionado",
                Location = new Point(15, 275), // ✅ AJUSTADO de 270 a 275
                Size = new Size(90, 50),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            btnEliminarSeleccionado.FlatAppearance.BorderSize = 0;

            btnLimpiarLista = new Button
            {
                Text = "Limpiar\nTodo",
                Location = new Point(115, 275), // ✅ AJUSTADO
                Size = new Size(90, 50),
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            btnLimpiarLista.FlatAppearance.BorderSize = 0;

            btnVistaPrevia = new Button
            {
                Text = "Vista\nPrevia",
                Location = new Point(215, 275), // ✅ AJUSTADO
                Size = new Size(90, 50),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            btnVistaPrevia.FlatAppearance.BorderSize = 0;

            panel.Controls.AddRange(new Control[] {
            lblTitulo, gbTamañosCartel, btnEliminarSeleccionado, btnLimpiarLista, btnVistaPrevia
            });

            return panel;
        }
    

        private Panel CrearPanelInferior()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(10)
            };

            // Botón imprimir
            btnImprimir = new Button
            {
                Text = "IMPRIMIR CARTELITOS",
                Location = new Point(10, 15),
                Size = new Size(180, 25),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Enabled = false
            };
            btnImprimir.FlatAppearance.BorderSize = 0;

            // Botón cerrar
            btnCerrar = new Button
            {
                Text = "CERRAR",
                Location = new Point(200, 15),
                Size = new Size(100, 25),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;

            panel.Controls.AddRange(new Control[] { btnImprimir, btnCerrar });

            return panel;
        }

        private void ConfigurarFormulario()
        {
            this.KeyPreview = true;
            ActualizarContador();
        }

        private void ConfigurarEventos()
        {
            // Eventos de controles
            txtCodigoProducto.KeyDown += TxtCodigoProducto_KeyDown;
            txtCodigoProducto.KeyPress += TxtCodigoProducto_KeyPress;
            btnAgregarProducto.Click += BtnAgregarProducto_Click;
            btnEliminarSeleccionado.Click += BtnEliminarSeleccionado_Click;
            btnLimpiarLista.Click += BtnLimpiarLista_Click;
            btnVistaPrevia.Click += BtnVistaPrevia_Click;
            btnImprimir.Click += BtnImprimir_Click;
            btnCerrar.Click += (s, e) => this.Close();

            // Eventos del DataGridView
            dgvProductosSeleccionados.SelectionChanged += DgvProductosSeleccionados_SelectionChanged;
            dgvProductosSeleccionados.KeyDown += DgvProductosSeleccionados_KeyDown;

            // Eventos del formulario
            this.Load += CartelitosPrecios_Load;
            this.KeyDown += CartelitosPrecios_KeyDown;
        }

        private void CartelitosPrecios_Load(object sender, EventArgs e)
        {
            txtCodigoProducto.Focus();
        }

        private void CartelitosPrecios_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }

        private void TxtCodigoProducto_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Permitir solo números
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void TxtCodigoProducto_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                BtnAgregarProducto_Click(sender, e);
            }
        }

        private async void BtnAgregarProducto_Click(object sender, EventArgs e)
        {
            string codigo = txtCodigoProducto.Text.Trim();
            if (string.IsNullOrEmpty(codigo))
            {
                MessageBox.Show("Ingrese un código de producto.", "Información", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCodigoProducto.Focus();
                return;
            }

            // Limpiar ceros a la izquierda
            codigo = codigo.TrimStart('0');
            if (string.IsNullOrEmpty(codigo))
                codigo = "0";

            await AgregarProductoPorCodigo(codigo);
        }

        private async Task AgregarProductoPorCodigo(string codigo)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                btnAgregarProducto.Enabled = false;

                // Verificar si el producto ya está en la lista
                if (productosSeleccionados.Any(p => p.Codigo == codigo))
                {
                    MessageBox.Show($"El producto con código '{codigo}' ya está en la lista.", 
                        "Producto duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCodigoProducto.Focus();
                    txtCodigoProducto.SelectAll();
                    return;
                }

                // Buscar el producto en la base de datos
                var producto = await BuscarProductoAsync(codigo);
                if (producto == null)
                {
                    MessageBox.Show($"No se encontró un producto con el código '{codigo}'.", 
                        "Producto no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCodigoProducto.Focus();
                    txtCodigoProducto.SelectAll();
                    return;
                }

                // Agregar a la lista
                productosSeleccionados.Add(producto);
                ActualizarDataGridView();
                ActualizarContador();
                
                // NUEVO: Guardar automáticamente
                GuardarProductos();

                // Limpiar y enfocar para el siguiente producto
                txtCodigoProducto.Clear();
                txtCodigoProducto.Focus();

                System.Diagnostics.Debug.WriteLine($"✅ Producto agregado: {codigo} - {producto.Descripcion}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar producto: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnAgregarProducto.Enabled = true;
            }
        }

        private async Task<ProductoCartelito> BuscarProductoAsync(string codigo)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"SELECT codigo, descripcion, precio, marca, rubro 
                                  FROM Productos 
                                  WHERE codigo = @codigo";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@codigo", codigo);
                        await connection.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                return new ProductoCartelito
                                {
                                    Codigo = reader["codigo"].ToString(),
                                    Descripcion = reader["descripcion"].ToString(),
                                    Precio = Convert.ToDecimal(reader["precio"]),
                                    Marca = reader["marca"].ToString(),
                                    Rubro = reader["rubro"].ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error buscando producto: {ex.Message}");
            }

            return null;
        }

        private void ActualizarDataGridView()
        {
            dgvProductosSeleccionados.Rows.Clear();

            foreach (var producto in productosSeleccionados)
            {
                dgvProductosSeleccionados.Rows.Add(
                    producto.Codigo,
                    producto.Descripcion,
                    producto.Precio,
                    producto.Marca
                );
            }

            // Quitar selección por defecto para que no aparezca la primera fila en azul
            dgvProductosSeleccionados.ClearSelection();
            // Alternativa si ClearSelection no basta:
            // if (dgvProductosSeleccionados.Rows.Count > 0) dgvProductosSeleccionados.CurrentCell = null;

            // Actualizar estado de botones
            bool hayProductos = productosSeleccionados.Count > 0;
            btnImprimir.Enabled = hayProductos;
            btnVistaPrevia.Enabled = hayProductos;
            btnLimpiarLista.Enabled = hayProductos;
        }

        private void ActualizarContador()
        {
            lblTotalProductos.Text = $"Productos en lista: {productosSeleccionados.Count}";
            
            bool haySeleccion = dgvProductosSeleccionados.SelectedRows.Count > 0;
            btnEliminarSeleccionado.Enabled = haySeleccion;
        }

        private void DgvProductosSeleccionados_SelectionChanged(object sender, EventArgs e)
        {
            ActualizarContador();
        }

        private void DgvProductosSeleccionados_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                BtnEliminarSeleccionado_Click(sender, e);
            }
        }

        private void BtnEliminarSeleccionado_Click(object sender, EventArgs e)
        {
            if (dgvProductosSeleccionados.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un producto para eliminar.", "Información", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgvProductosSeleccionados.SelectedRows[0];
            string codigo = row.Cells["Codigo"].Value.ToString();
            string descripcion = row.Cells["Descripcion"].Value.ToString();

            var resultado = MessageBox.Show(
                $"¿Está seguro de eliminar el producto:\n{descripcion}?",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (resultado == DialogResult.Yes)
            {
                productosSeleccionados.RemoveAll(p => p.Codigo == codigo);
                ActualizarDataGridView();
                ActualizarContador();
                
                // NUEVO: Guardar automáticamente después de eliminar
                GuardarProductos();
            }
        }

        private void BtnLimpiarLista_Click(object sender, EventArgs e)
        {
            if (productosSeleccionados.Count == 0)
            {
                MessageBox.Show("La lista ya está vacía.", "Información", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var resultado = MessageBox.Show(
                $"¿Está seguro de eliminar todos los productos de la lista?\n\nSe eliminarán {productosSeleccionados.Count} productos.",
                "Confirmar limpieza",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (resultado == DialogResult.Yes)
            {
                productosSeleccionados.Clear();
                ActualizarDataGridView();
                ActualizarContador();
                
                // NUEVO: Guardar automáticamente (eliminará el archivo)
                GuardarProductos();
                
                txtCodigoProducto.Focus();
            }
        }

        private void BtnVistaPrevia_Click(object sender, EventArgs e)
        {
            if (productosSeleccionados.Count == 0)
            {
                MessageBox.Show("No hay productos en la lista para mostrar.", "Información", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var tamañoSeleccionado = ObtenerTamañoSeleccionado();
            MostrarVistaPrevia(tamañoSeleccionado);
        }

        private void BtnImprimir_Click(object sender, EventArgs e)
        {
            if (productosSeleccionados.Count == 0)
            {
                MessageBox.Show("No hay productos en la lista para imprimir.", "Información", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var tamañoSeleccionado = ObtenerTamañoSeleccionado();
            ImprimirCartelitos(tamañoSeleccionado);
        }

        private TamañoCartelito ObtenerTamañoSeleccionado()
        {
            // ✅ NUEVO: Verificar opción térmica primero
            var rbTermico = gbTamañosCartel?.Controls.Find("rbTamañoTermico", true).FirstOrDefault() as RadioButton;
            if (rbTermico?.Checked == true)
            {
                System.Diagnostics.Debug.WriteLine("[CARTELITOS] ✅ Seleccionado: Térmico 70mm");
                return TamañoCartelito.Termico70mm;
            }

            if (rbTamañoPerfumeria.Checked)
                return TamañoCartelito.Perfumeria;
            else if (rbTamañoOferta.Checked)
                return TamañoCartelito.Oferta;
            else
                return TamañoCartelito.Estandar;
        }

        private void MostrarVistaPrevia(TamañoCartelito tamaño)
        {
            try
            {
                var servicioPrint = new ServicioImpresionCartelitos(productosSeleccionados, tamaño);
                servicioPrint.MostrarVistaPrevia();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al mostrar vista previa: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImprimirCartelitos(TamañoCartelito tamaño)
        {
            try
            {
                var resultado = MessageBox.Show(
                    $"¿Está seguro de imprimir {productosSeleccionados.Count} cartelitos en tamaño {tamaño}?",
                    "Confirmar impresión",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resultado == DialogResult.Yes)
                {
                    var servicioPrint = new ServicioImpresionCartelitos(productosSeleccionados, tamaño);
                    servicioPrint.Imprimir();
                    
                    MessageBox.Show("Cartelitos enviados a la impresora correctamente.", "Impresión exitosa", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NUEVO: Guardar al cerrar el formulario
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            GuardarProductos();
            base.OnFormClosed(e);
        }
    }

    // Clase para representar un producto en el cartelito
    public class ProductoCartelito
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public string Marca { get; set; }
        public string Rubro { get; set; }
    }

    // Enumeración para los tamaños de cartelito
    public enum TamañoCartelito
    {
        Estandar,   // 7x5 cm
        Perfumeria, // 5x3 cm  
        Oferta,     // 10x7 cm (A4)
        Termico70mm // ✅ NUEVO: 70mm papel térmico
    }
}