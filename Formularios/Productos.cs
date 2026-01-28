using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.ComponentModel;

namespace Comercio.NET.Formularios
{
    public partial class ProductosOptimizado : Form
    {
        private DataTable? productosTable;
        private System.Windows.Forms.Timer? searchTimer;
        private string lastSearchText = "";
        private bool isInitialized = false;

        // Sistema de cache para datos
        private static DataTable? _cacheProductos = null;
        private static DateTime _ultimaActualizacionCache = DateTime.MinValue;
        private static readonly TimeSpan DURACION_CACHE = TimeSpan.FromMinutes(5);
        
        // Control de carga
        private bool _cargandoDatos = false;

        // Sistema de filtro
        private CancellationTokenSource? _filtroTokenSource;
        private readonly object _filtroLock = new object();

        public ProductosOptimizado()
        {
            InitializeComponent();
            ConfigurarFormulario();
        }

        private void ConfigurarFormulario()
        {
            // Configuración básica del formulario
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Configurar timer de búsqueda
            //searchTimer = new System.Windows.Forms.Timer();
            //searchTimer.Interval = 500;
            //searchTimer.Tick += SearchTimer_Tick;
            
            // Configurar eventos de filtro
            txtFiltroDescripcion.TextChanged += TxtFiltroDescripcion_TextChanged;
            txtFiltroDescripcion.KeyDown += TxtFiltroDescripcion_KeyDown;
            
            // Configurar eventos de botones
            btnAgregarProducto.Click += BtnAgregarProducto_Click;
            btnModificarProducto.Click += BtnModificarProducto_Click;
            
            // Configurar textos de los controles (sin emojis)
            ConfigurarTextos();
            
            isInitialized = true;
        }

        private void CrearBotonActualizacionRapida()
        {
            // Crear botón para actualización rápida - MOVIDO MÁS A LA DERECHA Y MÁS ABAJO
            var btnActualizacionRapida = new Button
            {
                Text = "⚡ Actualización Rápida",
                Location = new Point(820, 15), // ✅ Movido más a la derecha (de 790 a 820) y un poco más abajo (de 8 a 15)
                Size = new Size(180, 25),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnActualizacionRapida.FlatAppearance.BorderSize = 0;
            btnActualizacionRapida.Click += BtnActualizacionRapida_Click;

            this.Controls.Add(btnActualizacionRapida);
            btnActualizacionRapida.BringToFront();
        }

        private void BtnActualizacionRapida_Click(object sender, EventArgs e)
        {
            try
            {
                // ✅ CAMBIO: Usar el método estático para mostrar como MDI Child con instancia única
                ActualizacionRapidaForm.MostrarFormulario(this.MdiParent);

                // ✅ NUEVO: Configurar evento para refrescar cuando se cierre
                // (El formulario ahora es MDI, no modal, así que manejamos esto diferente)

                // Nota: Como ahora es MDI Child (no modal), el refresh se hará cuando
                // el usuario cierre el formulario. Si quieres auto-refresh periódico,
                // puedes agregarlo después.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir actualización rápida: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CrearBotonActualizacionMasiva()
        {
            // Crear botón para actualización masiva - MOVIDO MÁS A LA DERECHA Y AJUSTADO
            var btnActualizacionMasiva = new Button
            {
                Text = "⚡⚡ Actualización Masiva",
                Location = new Point(820, 45), // ✅ Movido más a la derecha (de 790 a 820) y más abajo (de 35 a 45)
                Size = new Size(180, 25),
                BackColor = Color.FromArgb(156, 39, 176),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnActualizacionMasiva.FlatAppearance.BorderSize = 0;
            btnActualizacionMasiva.Click += BtnActualizacionMasiva_Click;

            this.Controls.Add(btnActualizacionMasiva);
            btnActualizacionMasiva.BringToFront();
        }


        private void BtnActualizacionMasiva_Click(object sender, EventArgs e)
        {
            try
            {
                // ✅ CORRECTO: Mostrar como diálogo modal SIN asignar MdiParent
                using (var form = new ActualizacionMasivaForm())
                {
                    // NO asignar MdiParent cuando usamos ShowDialog
                    // form.MdiParent = this; // ❌ NO HACER ESTO con ShowDialog

                    form.ShowDialog(this);

                    // Refrescar la grilla después de cerrar el formulario
                    _ = CargarProductosAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir actualización masiva: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigurarTextos()
        {
            // Configurar textos sin emojis para evitar problemas de codificación
            if (lblFiltro != null)
                lblFiltro.Text = "Buscar producto:";
            
            if (btnAgregarProducto != null)
                btnAgregarProducto.Text = "Agregar";
            
            if (btnModificarProducto != null)
                btnModificarProducto.Text = "Modificar";
            
            if (lblContador != null)
                lblContador.Text = "Registros: 0 de 0";
        }

        private async void ProductosOptimizado_Load(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚀 Cargando formulario de productos");

                // Mostrar el formulario rápidamente
                this.Show();
                this.Refresh();

                // Verificar y crear columnas necesarias en la BD
                await VerificarColumnasBaseDatos();

                // Configurar la grilla antes de cargar datos
                ConfigurarGrilla();

                CrearBotonActualizacionRapida();
                CrearBotonActualizacionMasiva();
                CrearBotonEliminar(); // ✅ AGREGAR ESTA LÍNEA

                // Cargar productos
                await CargarProductosAsync();

                // Enfocar el filtro
                txtFiltroDescripcion.Focus();

                System.Diagnostics.Debug.WriteLine("✅ Formulario cargado exitosamente");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando formulario: {ex.Message}");
                MessageBox.Show($"Error al cargar productos: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task VerificarColumnasBaseDatos()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection") ?? "";
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // ✅ NUEVO: Verificar columna Activo
                    string checkActivoQuery = @"
                                            SELECT COUNT(*) 
                                            FROM INFORMATION_SCHEMA.COLUMNS 
                                            WHERE TABLE_NAME = 'Productos' 
                                            AND COLUMN_NAME = 'Activo'";

                    using (var checkCmd = new SqlCommand(checkActivoQuery, connection))
                    {
                        int columnExists = (int)await checkCmd.ExecuteScalarAsync();

                        if (columnExists == 0)
                        {
                            string addColumnQuery = @"
                                                ALTER TABLE Productos 
                                                ADD Activo BIT NOT NULL DEFAULT 1";

                            using (var addCmd = new SqlCommand(addColumnQuery, connection))
                            {
                                await addCmd.ExecuteNonQueryAsync();
                                System.Diagnostics.Debug.WriteLine("✅ Columna Activo creada exitosamente");
                            }
                        }
                    }
                    // Verificar si existe la columna EditarPrecio
                    string checkColumnQuery = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Productos' 
                        AND COLUMN_NAME = 'EditarPrecio'";
                    
                    using (var checkCmd = new SqlCommand(checkColumnQuery, connection))
                    {
                        int columnExists = (int)await checkCmd.ExecuteScalarAsync();
                        
                        if (columnExists == 0)
                        {
                            // Crear la columna EditarPrecio
                            string addColumnQuery = @"
                                ALTER TABLE Productos 
                                ADD EditarPrecio BIT NOT NULL DEFAULT 0";
                            
                            using (var addCmd = new SqlCommand(addColumnQuery, connection))
                            {
                                await addCmd.ExecuteNonQueryAsync();
                                System.Diagnostics.Debug.WriteLine("✅ Columna EditarPrecio creada exitosamente");
                            }
                        }
                    }
                    
                    // Verificar si existe la columna PermiteAcumular (por si acaso)
                    string checkPermiteAcumularQuery = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Productos' 
                        AND COLUMN_NAME = 'PermiteAcumular'";
                    
                    using (var checkCmd2 = new SqlCommand(checkPermiteAcumularQuery, connection))
                    {
                        int columnExists2 = (int)await checkCmd2.ExecuteScalarAsync();
                        
                        if (columnExists2 == 0)
                        {
                            // Crear la columna PermiteAcumular
                            string addColumnQuery2 = @"
                                ALTER TABLE Productos 
                                ADD PermiteAcumular BIT NOT NULL DEFAULT 0";
                            
                            using (var addCmd2 = new SqlCommand(addColumnQuery2, connection))
                            {
                                await addCmd2.ExecuteNonQueryAsync();
                                System.Diagnostics.Debug.WriteLine("✅ Columna PermiteAcumular creada exitosamente");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Advertencia al verificar/crear columnas: {ex.Message}");
                // No lanzar excepción para no interrumpir la carga del formulario
            }
        }

        private void ConfigurarGrilla()
        {
            if (GrillaProductos == null) return;

            GrillaProductos.SuspendLayout();
            
            try
            {
                // Configuración básica
                GrillaProductos.BackgroundColor = Color.White;
                GrillaProductos.BorderStyle = BorderStyle.None;
                GrillaProductos.AllowUserToAddRows = false;
                GrillaProductos.AllowUserToDeleteRows = false;
                GrillaProductos.ReadOnly = false; // Cambiar a false para permitir edición
                GrillaProductos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                GrillaProductos.MultiSelect = false;
                GrillaProductos.RowHeadersVisible = false;
                GrillaProductos.EnableHeadersVisualStyles = false;
                GrillaProductos.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                GrillaProductos.GridColor = Color.FromArgb(230, 236, 240);

                // Configuración de headers
                GrillaProductos.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(63, 81, 181);
                GrillaProductos.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                GrillaProductos.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                GrillaProductos.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                GrillaProductos.ColumnHeadersHeight = 40;

                // Configuración de celdas
                GrillaProductos.DefaultCellStyle.BackColor = Color.White;
                GrillaProductos.DefaultCellStyle.ForeColor = Color.FromArgb(62, 80, 100);
                GrillaProductos.DefaultCellStyle.SelectionBackColor = Color.FromArgb(227, 242, 253);
                GrillaProductos.DefaultCellStyle.SelectionForeColor = Color.FromArgb(62, 80, 100);
                GrillaProductos.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
                GrillaProductos.RowTemplate.Height = 35;

                // Filas alternadas
                GrillaProductos.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

                // Configuración de redimensionamiento
                GrillaProductos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                GrillaProductos.ScrollBars = ScrollBars.Both;
                GrillaProductos.AllowUserToResizeColumns = true;
                GrillaProductos.AllowUserToResizeRows = false;

                // Agregar eventos para la edición de celdas
                GrillaProductos.CellValueChanged += GrillaProductos_CellValueChanged;
                GrillaProductos.CurrentCellDirtyStateChanged += GrillaProductos_CurrentCellDirtyStateChanged;
                GrillaProductos.CellBeginEdit += GrillaProductos_CellBeginEdit;
                //GrillaProductos.CellDoubleClick += GrillaProductos_CellDoubleClick;
            }
            finally
            {
                GrillaProductos.ResumeLayout();
            }
        }

        private async Task CargarProductosAsync()
        {
            if (_cargandoDatos) return;
            _cargandoDatos = true;
            
            try
            {
                this.Cursor = Cursors.WaitCursor;
                lblContador.Text = "🔄 Cargando productos...";
                
                // Verificar cache
                if (_cacheProductos != null && 
                    DateTime.Now - _ultimaActualizacionCache < DURACION_CACHE)
                {
                    System.Diagnostics.Debug.WriteLine("📦 Usando cache de productos");
                    productosTable = _cacheProductos.Copy();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("🔄 Cargando datos frescos");
                    productosTable = await CargarProductosDesdeBDAsync();
                    
                    // Actualizar cache
                    if (productosTable != null)
                    {
                        _cacheProductos?.Dispose();
                        _cacheProductos = productosTable.Copy();
                        _ultimaActualizacionCache = DateTime.Now;
                    }
                }
                
                // Asignar datos a la grilla
                if (GrillaProductos != null && productosTable != null)
                {
                    GrillaProductos.DataSource = productosTable;
                    ConfigurarColumnas();
                    AplicarFormatoStock();
                    ActualizarContador();
                }
                
                this.Text = $"Gestión de Productos ({productosTable?.Rows?.Count ?? 0:N0} productos) - ✓ Primeras columnas editables";
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando productos: {ex.Message}");
                MessageBox.Show($"Error al cargar productos: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                _cargandoDatos = false;
            }
        }

        private async Task<DataTable> CargarProductosDesdeBDAsync()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            string connectionString = config.GetConnectionString("DefaultConnection") ?? "";
            var dataTable = new DataTable();
            var productosConError = new List<string>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // ✅ MODIFICADO: Query más robusta que maneja valores fuera de rango
                var query = @"SELECT 
                codigo, 
                descripcion, 
                marca,
                -- Validar y limitar valores que puedan causar overflow
                CASE 
                    WHEN ISNUMERIC(costo) = 1 AND ABS(CAST(costo AS DECIMAL(18,2))) <= 99999999.99 
                    THEN CAST(costo AS DECIMAL(10,2))
                    ELSE 0.00 
                END as costo,
                CASE 
                    WHEN ISNUMERIC(porcentaje) = 1 AND ABS(CAST(porcentaje AS DECIMAL(7,2))) <= 999.99 
                    THEN CAST(porcentaje AS DECIMAL(5,2))
                    ELSE 0.00 
                END as porcentaje,
                CASE 
                    WHEN ISNUMERIC(precio) = 1 AND ABS(CAST(precio AS DECIMAL(18,2))) <= 99999999.99 
                    THEN CAST(precio AS DECIMAL(10,2))
                    ELSE 0.00 
                END as precio,
                CAST(cantidad AS INT) as cantidad, 
                CAST(ISNULL(iva, 21.00) AS DECIMAL(5,2)) as iva,
                CAST(ISNULL(Activo, 1) AS BIT) as Activo,
                CAST(ISNULL(PermiteAcumular, 0) AS BIT) as PermiteAcumular,
                CAST(ISNULL(EditarPrecio, 0) AS BIT) as EditarPrecio,
                -- Columna auxiliar para detectar errores
                CASE 
                    WHEN (ISNUMERIC(costo) = 0 OR ABS(CAST(costo AS DECIMAL(18,2))) > 99999999.99)
                      OR (ISNUMERIC(porcentaje) = 0 OR ABS(CAST(porcentaje AS DECIMAL(7,2))) > 999.99)
                      OR (ISNUMERIC(precio) = 0 OR ABS(CAST(precio AS DECIMAL(18,2))) > 99999999.99)
                    THEN 1 
                    ELSE 0 
                END as TieneError
            FROM Productos 
            ORDER BY descripcion";

                using (var adapter = new SqlDataAdapter(query, connection))
                {
                    adapter.SelectCommand.CommandTimeout = 60;
                    await Task.Run(() => adapter.Fill(dataTable));
                }

                // Detectar productos con errores
                foreach (DataRow row in dataTable.Rows)
                {
                    if (row["TieneError"] != DBNull.Value && Convert.ToInt32(row["TieneError"]) == 1)
                    {
                        productosConError.Add($"• {row["codigo"]} - {row["descripcion"]}");
                    }
                }
            }

            // ⚠️ Si hay productos con error, mostrar advertencia
            if (productosConError.Count > 0)
            {
                string mensaje = $"⚠️ ATENCIÓN: Se encontraron {productosConError.Count} producto(s) con valores fuera de rango.\n\n" +
                                "Estos productos tienen valores de COSTO, PORCENTAJE o PRECIO demasiado altos.\n" +
                                "Se mostrarán en ROJO en la grilla.\n\n" +
                                "Productos con error:\n" +
                                string.Join("\n", productosConError.Take(10));

                if (productosConError.Count > 10)
                {
                    mensaje += $"\n... y {productosConError.Count - 10} más.";
                }

                mensaje += "\n\n⚠️ Por favor, edite estos productos para corregir los valores.";

                System.Diagnostics.Debug.WriteLine($"⚠️ Productos con error detectados: {productosConError.Count}");

                // Mostrar mensaje sin bloquear la carga
                MessageBox.Show(mensaje,
                    "⚠️ Productos con Valores Inválidos Detectados",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            return dataTable;
        }

        
        private void ConfigurarColumnas()
        {
            if (GrillaProductos?.Columns?.Count == 0) return;

            try
            {
                // ✅ NUEVO: Ocultar columna auxiliar de error
                if (GrillaProductos.Columns.Contains("TieneError"))
                {
                    GrillaProductos.Columns["TieneError"].Visible = false;
                }

                var columnConfig = new Dictionary<string, (int width, string header, DataGridViewContentAlignment align, string format)>
                {
                    ["codigo"] = (90, "CÓDIGO", DataGridViewContentAlignment.MiddleCenter, ""),
                    ["descripcion"] = (280, "DESCRIPCIÓN", DataGridViewContentAlignment.MiddleLeft, ""),
                    ["marca"] = (90, "MARCA", DataGridViewContentAlignment.MiddleCenter, ""),
                    ["costo"] = (70, "COSTO", DataGridViewContentAlignment.MiddleRight, "C2"),
                    ["porcentaje"] = (45, "%", DataGridViewContentAlignment.MiddleCenter, "N1"),
                    ["precio"] = (90, "PRECIO VENTA", DataGridViewContentAlignment.MiddleRight, "C2"),
                    ["cantidad"] = (50, "STOCK", DataGridViewContentAlignment.MiddleCenter, "N0"),
                    ["iva"] = (55, "IVA %", DataGridViewContentAlignment.MiddleCenter, "N2"),
                    ["activo"] = (70, "ACTIVO", DataGridViewContentAlignment.MiddleCenter, ""),
                    ["permiteacumular"] = (90, "ACUMULAR", DataGridViewContentAlignment.MiddleCenter, ""),
                    ["editarprecio"] = (90, "EDIT PRECIO", DataGridViewContentAlignment.MiddleCenter, "")
                };

                foreach (DataGridViewColumn col in GrillaProductos.Columns)
                {
                    var columnName = col.Name.ToLower();

                    if (columnConfig.ContainsKey(columnName))
                    {
                        var config = columnConfig[columnName];
                        col.Width = config.width;
                        col.HeaderText = config.header;
                        col.DefaultCellStyle.Alignment = config.align;
                        col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

                        if (!string.IsNullOrEmpty(config.format))
                            col.DefaultCellStyle.Format = config.format;

                        // Configuración especial por columna
                        if (columnName == "iva")
                        {
                            col.DefaultCellStyle.BackColor = Color.FromArgb(240, 248, 255);
                            col.DefaultCellStyle.ForeColor = Color.FromArgb(25, 118, 210);
                            col.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                            col.ReadOnly = true;
                        }
                        else if (columnName == "activo" || columnName == "permiteacumular" || columnName == "editarprecio")
                        {
                            col.ReadOnly = false;

                            if (columnName == "activo")
                            {
                                col.DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 255);
                                col.DefaultCellStyle.ForeColor = Color.FromArgb(0, 0, 100);
                            }
                            else if (columnName == "permiteacumular")
                            {
                                col.DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                                col.DefaultCellStyle.ForeColor = Color.FromArgb(0, 100, 0);
                            }
                            else if (columnName == "editarprecio")
                            {
                                col.DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 240);
                                col.DefaultCellStyle.ForeColor = Color.FromArgb(100, 0, 0);
                            }

                            col.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        }
                        else
                        {
                            col.ReadOnly = true;
                        }
                    }
                }

                GrillaProductos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

                if (GrillaProductos.Columns["descripcion"] != null)
                {
                    GrillaProductos.Columns["descripcion"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    GrillaProductos.Columns["descripcion"].MinimumWidth = 220;
                    GrillaProductos.Columns["descripcion"].FillWeight = 100;
                }

                ConfigurarColumnasCheckbox();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configurando columnas: {ex.Message}");
            }
        }

        private void ConfigurarColumnasCheckbox()
        {
            try
            {
                var activoIndex = -1;
                var permiteAcumularIndex = -1;
                var editarPrecioIndex = -1;

                // Buscar índices de las columnas
                for (int i = 0; i < GrillaProductos.Columns.Count; i++)
                {
                    var colName = GrillaProductos.Columns[i].Name;
                    if (colName.Equals("Activo", StringComparison.OrdinalIgnoreCase))
                        activoIndex = i;
                    else if (colName.Equals("PermiteAcumular", StringComparison.OrdinalIgnoreCase))
                        permiteAcumularIndex = i;
                    else if (colName.Equals("EditarPrecio", StringComparison.OrdinalIgnoreCase))
                        editarPrecioIndex = i;
                }

                var columnasCheckbox = new List<DataGridViewCheckBoxColumn>();

                // ✅ NUEVO: Configurar columna Activo como checkbox
                if (activoIndex >= 0)
                {
                    string dataPropertyName = GrillaProductos.Columns[activoIndex].DataPropertyName;

                    var checkCol = new DataGridViewCheckBoxColumn
                    {
                        Name = "Activo",
                        DataPropertyName = dataPropertyName,
                        HeaderText = "ACTIVO",
                        Width = 70,
                        ReadOnly = false,
                        TrueValue = true,
                        FalseValue = false,
                        IndeterminateValue = false,
                        ToolTipText = "Marcar si el producto está activo (visible en el sistema). Desmarcar para ocultarlo sin eliminarlo."
                    };
                    checkCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    checkCol.DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 255);

                    columnasCheckbox.Add(checkCol);
                    GrillaProductos.Columns.RemoveAt(activoIndex);

                    // Ajustar índices
                    if (permiteAcumularIndex > activoIndex) permiteAcumularIndex--;
                    if (editarPrecioIndex > activoIndex) editarPrecioIndex--;
                }

                // Configurar columna PermiteAcumular
                if (permiteAcumularIndex >= 0)
                {
                    string dataPropertyName1 = GrillaProductos.Columns[permiteAcumularIndex].DataPropertyName;

                    var checkCol1 = new DataGridViewCheckBoxColumn
                    {
                        Name = "PermiteAcumular",
                        DataPropertyName = dataPropertyName1,
                        HeaderText = "ACUMULAR",
                        Width = 90,
                        ReadOnly = false,
                        TrueValue = true,
                        FalseValue = false,
                        IndeterminateValue = false,
                        ToolTipText = "Marcar si el producto permite acumular descuentos o promociones (se desmarca automáticamente si se marca 'EDIT PRECIO')"
                    };
                    checkCol1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    checkCol1.DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);

                    columnasCheckbox.Add(checkCol1);
                    GrillaProductos.Columns.RemoveAt(permiteAcumularIndex);

                    if (editarPrecioIndex > permiteAcumularIndex) editarPrecioIndex--;
                }

                // Configurar columna EditarPrecio
                if (editarPrecioIndex >= 0)
                {
                    string dataPropertyName2 = GrillaProductos.Columns[editarPrecioIndex].DataPropertyName;

                    var checkCol2 = new DataGridViewCheckBoxColumn
                    {
                        Name = "EditarPrecio",
                        DataPropertyName = dataPropertyName2,
                        HeaderText = "EDIT PRECIO",
                        Width = 90,
                        ReadOnly = false,
                        TrueValue = true,
                        FalseValue = false,
                        IndeterminateValue = false,
                        ToolTipText = "Marcar si se permite editar el precio del producto en ventas (se desmarca automáticamente si se marca 'ACUMULAR')"
                    };
                    checkCol2.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    checkCol2.DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 240);

                    columnasCheckbox.Add(checkCol2);
                    GrillaProductos.Columns.RemoveAt(editarPrecioIndex);
                }

                // Insertar las columnas checkbox al comienzo
                for (int i = columnasCheckbox.Count - 1; i >= 0; i--)
                {
                    GrillaProductos.Columns.Insert(0, columnasCheckbox[i]);
                }

                if (GrillaProductos.Tag == null)
                {
                    var toolTip = new ToolTip();
                    toolTip.SetToolTip(GrillaProductos, "Primeras columnas ✓ ACTIVO, ACUMULAR y EDIT PRECIO son editables. Haga clic para cambiar valores.");
                    GrillaProductos.Tag = toolTip;
                }

                System.Diagnostics.Debug.WriteLine($"✅ Columnas checkbox (incluyendo Activo) configuradas al comienzo de la grilla");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configurando columnas checkbox: {ex.Message}");
            }
        }

        private void AplicarFormatoStock()
        {
            if (GrillaProductos?.Rows == null) return;

            try
            {
                foreach (DataGridViewRow row in GrillaProductos.Rows)
                {
                    // ✅ NUEVO: Detectar y resaltar productos con errores
                    bool tieneError = false;
                    if (row.Cells["TieneError"]?.Value != DBNull.Value)
                    {
                        tieneError = Convert.ToInt32(row.Cells["TieneError"].Value) == 1;
                    }

                    if (tieneError)
                    {
                        // ⚠️ Resaltar TODA LA FILA en rojo si tiene error
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 238);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(183, 28, 28);
                        row.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

                        // Agregar tooltip explicativo
                        row.Cells["codigo"].ToolTipText = "⚠️ PRODUCTO CON ERROR: Valores fuera de rango. Haga doble clic para editar.";
                        row.Cells["descripcion"].ToolTipText = "⚠️ PRODUCTO CON ERROR: Valores fuera de rango. Haga doble clic para editar.";
                    }
                    else
                    {
                        // Formato normal de stock (solo si no hay error)
                        var cantidadCell = row.Cells["cantidad"];
                        if (cantidadCell?.Value != null && decimal.TryParse(cantidadCell.Value.ToString(), out decimal stock))
                        {
                            if (stock <= 5)
                            {
                                cantidadCell.Style.BackColor = Color.FromArgb(255, 199, 206);
                                cantidadCell.Style.ForeColor = Color.FromArgb(183, 28, 28);
                                cantidadCell.Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                            }
                            else if (stock <= 10)
                            {
                                cantidadCell.Style.BackColor = Color.FromArgb(255, 248, 225);
                                cantidadCell.Style.ForeColor = Color.FromArgb(255, 111, 0);
                                cantidadCell.Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aplicando formato de stock: {ex.Message}");
            }
        }

        private void ActualizarContador()
        {
            try
            {
                if (lblContador != null && productosTable != null)
                {
                    int total = productosTable.Rows.Count;
                    int filtrados = productosTable.DefaultView.Count;
                    
                    // CORREGIDO: Obtener texto de manera segura
                    string textoFiltro = "";
                    if (txtFiltroDescripcion?.Text != null)
                    {
                        textoFiltro = txtFiltroDescripcion.Text.Trim();
                    }
                    
                    if (string.IsNullOrEmpty(textoFiltro))
                    {
                        // Sin filtro - mostrar todos los registros
                        lblContador.Text = $"Registros: {total:N0} (todos)";
                        lblContador.ForeColor = Color.FromArgb(62, 80, 100); // Color normal
                    }
                    else
                    {
                        // Con filtro - mostrar información de filtrado
                        string[] palabras = textoFiltro.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                      .Take(4)
                                                      .ToArray();
                        
                        string infoFiltro = "";
                        if (palabras.Length > 1)
                        {
                            infoFiltro = $" (Filtro: {palabras.Length} palabras - Todos deben coincidir)";
                        }
                        else if (palabras.Length == 1)
                        {
                            infoFiltro = $" (Filtro: '{palabras[0]}')";
                        }
                        
                        lblContador.Text = $"Registros: {filtrados:N0} de {total:N0}{infoFiltro}";
                        
                        // Cambiar color según resultados
                        if (filtrados == 0)
                        {
                            lblContador.ForeColor = Color.FromArgb(183, 28, 28); // Rojo si no hay resultados
                        }
                        else if (filtrados < total)
                        {
                            lblContador.ForeColor = Color.FromArgb(255, 111, 0); // Naranja si hay filtro aplicado
                        }
                        else
                        {
                            lblContador.ForeColor = Color.FromArgb(62, 80, 100); // Color normal
                        }
                        
                        // Debug: mostrar las palabras que se están buscando
                        System.Diagnostics.Debug.WriteLine($"🔍 Buscando: [{string.Join(", ", palabras)}] - Resultados: {filtrados}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando contador: {ex.Message}");
            }
        }

        private void TxtFiltroDescripcion_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (!isInitialized) return;

                // ✅ OPCIÓN 2: Ya no se usa el timer automático
                // Solo mantener este evento por si se necesita en el futuro
                // o eliminarlo directamente si usas búsqueda manual

                // ❌ ELIMINAR estas líneas si usas Opción 2:
                // searchTimer?.Stop();
                // searchTimer?.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en TxtFiltroDescripcion_TextChanged: {ex.Message}");
            }
        }

        //private async void SearchTimer_Tick(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        searchTimer?.Stop();

        //        var textoBuscar = txtFiltroDescripcion.Text.Trim();

        //        // Evitar búsquedas idénticas consecutivas
        //        if (lastSearchText.Equals(textoBuscar, StringComparison.OrdinalIgnoreCase))
        //            return;

        //        lastSearchText = textoBuscar;

        //        System.Diagnostics.Debug.WriteLine($"🔍 Aplicando filtro: '{textoBuscar}'");

        //        // Aplicar filtro de búsqueda (ahora con manejo mejorado de texto vacío)
        //        await AplicarFiltro(textoBuscar);
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"Error en SearchTimer_Tick: {ex.Message}");
        //    }
        //}

        private async void BtnAgregarProducto_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("➕ Agregar nuevo producto");

                // Guardar el filtro actual antes de abrir el formulario
                string filtroActual = txtFiltroDescripcion.Text;

                // ✅ CREAR formulario y asignar MdiParent
                var form = new ProductoFormUnificado(
                    ProductoFormUnificado.ModoOperacion.Agregar,
                    "",
                    ProductoFormUnificado.OrigenLlamada.Productos);

                // ✅ ASIGNAR MdiParent del formulario padre (Productos)
                form.MdiParent = this.MdiParent;

                // Configurar evento de cierre para actualizar cuando se cierre
                form.FormClosed += async (s, args) =>
                {
                    // Actualizar la grilla manteniendo el filtro aplicado
                    await ActualizarDatosManteniendoFiltro(filtroActual);

                    // Si se agregó un producto, intentar seleccionarlo en la grilla
                    if (!string.IsNullOrEmpty(form.CodigoAgregado))
                    {
                        await SeleccionarProductoEnGrilla(form.CodigoAgregado);
                    }

                    txtFiltroDescripcion.Focus();
                };

                form.Show(); // ✅ Muestra el formulario de forma no modal DENTRO del MDI
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en BtnAgregarProducto_Click: {ex.Message}");
                MessageBox.Show($"Error al agregar producto: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnModificarProducto_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("✏️ Modificar producto seleccionado");

                if (GrillaProductos.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Seleccione un producto para modificar.", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var filaSeleccionada = GrillaProductos.SelectedRows[0];
                var productoId = filaSeleccionada.Cells["codigo"].Value.ToString();

                // Guardar el filtro actual antes de abrir el formulario
                string filtroActual = txtFiltroDescripcion.Text;

                // ✅ CREAR formulario y asignar MdiParent
                var form = new ProductoFormUnificado(
                    ProductoFormUnificado.ModoOperacion.Modificar,
                    productoId,
                    ProductoFormUnificado.OrigenLlamada.Productos);

                // ✅ ASIGNAR MdiParent del formulario padre (Productos)
                form.MdiParent = this.MdiParent;

                // Configurar evento de cierre para actualizar cuando se cierre
                form.FormClosed += async (s, args) =>
                {
                    // Actualizar la grilla manteniendo el filtro aplicado
                    await ActualizarDatosManteniendoFiltro(filtroActual);

                    // Mantener la selección en el producto modificado
                    await SeleccionarProductoEnGrilla(productoId);

                    txtFiltroDescripcion.Focus();
                };

                form.Show(); // ✅ Muestra el formulario de forma no modal DENTRO del MDI
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en BtnModificarProducto_Click: {ex.Message}");
                MessageBox.Show($"Error al modificar producto: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void GrillaProductos_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                // Verificar si es una columna editable (checkbox)
                var columnName = GrillaProductos.Columns[e.ColumnIndex].Name;
                if (columnName == "PermiteAcumular" || columnName == "EditarPrecio" || columnName == "Activo")
                {
                    // No abrir el formulario de edición para estas columnas
                    return;
                }

                // Guardar el filtro actual antes de abrir el formulario
                string filtroActual = txtFiltroDescripcion.Text;

                // Obtener el código del producto de la fila seleccionada
                var filaSeleccionada = GrillaProductos.Rows[e.RowIndex];
                var productoId = filaSeleccionada.Cells["codigo"].Value.ToString();

                try
                {
                    System.Diagnostics.Debug.WriteLine($"✏️ Modificar producto desde doble click: {productoId}");

                    // ✅ CREAR formulario y asignar MdiParent
                    var form = new ProductoFormUnificado(
                        ProductoFormUnificado.ModoOperacion.Modificar,
                        productoId,
                        ProductoFormUnificado.OrigenLlamada.Productos);

                    // ✅ ASIGNAR MdiParent del formulario padre (Productos)
                    form.MdiParent = this.MdiParent;

                    // Configurar evento de cierre para actualizar cuando se cierre
                    form.FormClosed += async (s, args) =>
                    {
                        // Actualizar la grilla manteniendo el filtro aplicado
                        await ActualizarDatosManteniendoFiltro(filtroActual);

                        // Mantener la selección en el producto modificado
                        await SeleccionarProductoEnGrilla(productoId);

                        txtFiltroDescripcion.Focus();
                    };

                    form.Show(); // ✅ Muestra el formulario de forma no modal DENTRO del MDI
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en GrillaProductos_CellDoubleClick: {ex.Message}");
                    MessageBox.Show($"Error al modificar producto: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // MÉTODO SeleccionarProductoEnGrilla agregado
        private async Task SeleccionarProductoEnGrilla(string codigo)
        {
            try
            {
                await Task.Delay(100); // Pequeña pausa para asegurar que la grilla esté actualizada

                if (GrillaProductos?.Rows != null)
                {
                    foreach (DataGridViewRow row in GrillaProductos.Rows)
                    {
                        if (row.Cells["codigo"]?.Value?.ToString() == codigo)
                        {
                            GrillaProductos.ClearSelection();
                            row.Selected = true;
                            GrillaProductos.FirstDisplayedScrollingRowIndex = row.Index;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error seleccionando producto en grilla: {ex.Message}");
            }
        }

        private async Task ActualizarDatosManteniendoFiltro(string filtroAMantener)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 Actualizando datos manteniendo filtro: '{filtroAMantener}'");
                
                // Limpiar cache para forzar recarga desde BD
                LimpiarCache();
                
                // Cargar datos frescos
                await CargarProductosAsync();
                
                // Si había un filtro aplicado, reaplicarlo
                if (!string.IsNullOrEmpty(filtroAMantener))
                {
                    // Temporalmente limpiar el textbox para evitar eventos duplicados
                    txtFiltroDescripcion.TextChanged -= TxtFiltroDescripcion_TextChanged;
                    
                    // Restaurar el texto del filtro
                    txtFiltroDescripcion.Text = filtroAMantener;
                    
                    // Reaplicar el filtro inmediatamente
                    await AplicarFiltro(filtroAMantener);
                    
                    // Restaurar el evento
                    txtFiltroDescripcion.TextChanged += TxtFiltroDescripcion_TextChanged;
                    
                    // Actualizar la variable de seguimiento
                    lastSearchText = filtroAMantener;
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Filtro '{filtroAMantener}' reaplicado exitosamente");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✅ Datos actualizados sin filtro");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando datos con filtro: {ex.Message}");
                MessageBox.Show($"Error al actualizar datos: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task AplicarFiltro(string texto)
        {
            if (productosTable == null) return;

            _filtroTokenSource?.Cancel();
            _filtroTokenSource = new CancellationTokenSource();

            try
            {
                // Limpiar texto de espacios al inicio y final
                texto = texto?.Trim() ?? "";

                if (string.IsNullOrEmpty(texto))
                {
                    // Si no hay texto, mostrar todos los registros
                    productosTable.DefaultView.RowFilter = "";
                    System.Diagnostics.Debug.WriteLine("🔍 Filtro limpiado - mostrando todos los registros");
                }
                else
                {
                    // Dividir el texto en palabras (máximo 4)
                    string[] palabras = texto.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                             .Take(4)
                                             .ToArray();

                    // Escapar comillas simples en cada palabra
                    for (int i = 0; i < palabras.Length; i++)
                    {
                        palabras[i] = palabras[i].Replace("'", "''");
                    }

                    string filtro = "";

                    if (palabras.Length == 1)
                    {
                        // ✅ MODIFICADO: Eliminado 'rubro' de la búsqueda
                        string palabra = palabras[0];
                        if (palabra.Length <= 3)
                        {
                            filtro = $"codigo LIKE '{palabra}%'";
                        }
                        else
                        {
                            filtro = $"(descripcion LIKE '%{palabra}%' OR codigo LIKE '%{palabra}%' OR marca LIKE '%{palabra}%')";
                        }
                    }
                    else
                    {
                        // ✅ MODIFICADO: Eliminado 'rubro' de la búsqueda con múltiples palabras
                        var condicionesPalabras = new List<string>();

                        foreach (string palabra in palabras)
                        {
                            string condicionPalabra = $"(descripcion LIKE '%{palabra}%' OR marca LIKE '%{palabra}%')";
                            condicionesPalabras.Add($"({condicionPalabra})");
                        }

                        filtro = string.Join(" AND ", condicionesPalabras);
                    }

                    System.Diagnostics.Debug.WriteLine($"🔍 Filtro aplicado: {filtro}");
                    productosTable.DefaultView.RowFilter = filtro;
                }

                // Actualizar contador y formato
                ActualizarContador();

                // Solo aplicar formato de stock si hay pocos registros para mejor rendimiento
                if (productosTable.DefaultView.Count <= 100)
                {
                    AplicarFormatoStock();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚡ Omitiendo formato de stock para {productosTable.DefaultView.Count} registros (optimización de rendimiento)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aplicando filtro: {ex.Message}");
                // En caso de error, limpiar filtro para mostrar todos los registros
                productosTable.DefaultView.RowFilter = "";
                ActualizarContador();
            }
        }

        //private async void TxtFiltroDescripcion_KeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.KeyCode == Keys.Enter)
        //    {
        //        e.SuppressKeyPress = true;
        //        await RealizarBusqueda(); // ✅ Buscar al presionar ENTER
        //    }
        //    else if (e.KeyCode == Keys.Escape)
        //    {
        //        txtFiltroDescripcion.Clear();
        //        await AplicarFiltro("");
        //        System.Diagnostics.Debug.WriteLine("🧹 Filtro limpiado con ESC");
        //    }
        //    else if (e.KeyCode == Keys.Down && GrillaProductos?.Rows?.Count > 0)
        //    {
        //        GrillaProductos.Focus();
        //        if (GrillaProductos.Rows.Count > 0)
        //            GrillaProductos.Rows[0].Selected = true;
        //    }
        //}

        private void TxtFiltroDescripcion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                // Limpiar el textbox y aplicar filtro vacío inmediatamente
                txtFiltroDescripcion.Clear();
                _ = AplicarFiltro(""); // Forzar aplicación inmediata del filtro vacío
                System.Diagnostics.Debug.WriteLine("🧹 Filtro limpiado con ESC - mostrando todos los registros");
            }
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (GrillaProductos?.Rows?.Count > 0)
                {
                    GrillaProductos.Focus();
                    if (GrillaProductos.Rows.Count > 0)
                        GrillaProductos.Rows[0].Selected = true;
                }
            }
        }

        #region Eventos de Grilla

        private void GrillaProductos_SelectionChanged(object sender, EventArgs e)
        {
            // Mantener simple - solo para cumplir con el evento
        }

        private void GrillaProductos_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.Reset)
            {
                AplicarFormatoStock();
                ConfigurarColumnasCheckbox();
            }
        }

        private void GrillaProductos_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            // Permitir edición de las columnas checkbox (incluyendo Activo)
            var columnName = GrillaProductos.Columns[e.ColumnIndex].Name;
            if (columnName != "Activo" && columnName != "PermiteAcumular" && columnName != "EditarPrecio")
            {
                e.Cancel = true;
            }
        }

        private void GrillaProductos_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (GrillaProductos.IsCurrentCellDirty)
            {
                var columnName = GrillaProductos.CurrentCell?.OwningColumn?.Name;
                if (columnName == "Activo" || columnName == "PermiteAcumular" || columnName == "EditarPrecio")
                {
                    GrillaProductos.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            }
        }

        private async void GrillaProductos_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var columnName = GrillaProductos.Columns[e.ColumnIndex].Name;

            // Procesar cambios en las columnas editables (incluyendo Activo)
            if (columnName != "Activo" && columnName != "PermiteAcumular" && columnName != "EditarPrecio")
                return;

            try
            {
                var row = GrillaProductos.Rows[e.RowIndex];
                var codigo = row.Cells["codigo"]?.Value?.ToString();

                if (string.IsNullOrEmpty(codigo))
                    return;

                var nuevoValor = row.Cells[columnName]?.Value;
                bool valorBool = nuevoValor != null && (bool)nuevoValor;

                // Lógica de exclusión mutua solo para PermiteAcumular y EditarPrecio
                if (valorBool && columnName != "Activo")
                {
                    if (columnName == "PermiteAcumular")
                    {
                        var editarPrecioCell = row.Cells["EditarPrecio"];
                        if (editarPrecioCell != null && editarPrecioCell.Value != null && (bool)editarPrecioCell.Value)
                        {
                            editarPrecioCell.Value = false;
                            await ActualizarCampoEnBD(codigo, "EditarPrecio", false);
                            System.Diagnostics.Debug.WriteLine($"🔄 EditarPrecio desmarcado automáticamente para {codigo}");
                        }
                    }
                    else if (columnName == "EditarPrecio")
                    {
                        var permitirAcumularCell = row.Cells["PermiteAcumular"];
                        if (permitirAcumularCell != null && permitirAcumularCell.Value != null && (bool)permitirAcumularCell.Value)
                        {
                            permitirAcumularCell.Value = false;
                            await ActualizarCampoEnBD(codigo, "PermiteAcumular", false);
                            System.Diagnostics.Debug.WriteLine($"🔄 PermiteAcumular desmarcado automáticamente para {codigo}");
                        }
                    }
                }

                // Actualizar el campo en la BD
                await ActualizarCampoEnBD(codigo, columnName, valorBool);

                // Feedback visual
                Color colorFeedback = valorBool ? Color.FromArgb(200, 255, 200) : Color.FromArgb(255, 230, 230);
                row.Cells[columnName].Style.BackColor = colorFeedback;

                // Restaurar color después de 1 segundo
                var timer = new System.Windows.Forms.Timer { Interval = 1000 };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    timer.Dispose();

                    if (e.RowIndex < GrillaProductos.Rows.Count && e.ColumnIndex < GrillaProductos.Columns.Count)
                    {
                        Color colorFondo = columnName == "Activo"
                            ? Color.FromArgb(240, 240, 255)
                            : (columnName == "PermiteAcumular"
                                ? Color.FromArgb(240, 255, 240)
                                : Color.FromArgb(255, 240, 240));

                        row.Cells[columnName].Style.BackColor = colorFondo;

                        if (valorBool && columnName != "Activo")
                        {
                            string otraColumna = columnName == "PermiteAcumular" ? "EditarPrecio" : "PermiteAcumular";
                            var otraCell = row.Cells[otraColumna];
                            if (otraCell != null)
                            {
                                Color otroColorFondo = otraColumna == "PermiteAcumular"
                                    ? Color.FromArgb(240, 255, 240)
                                    : Color.FromArgb(255, 240, 240);
                                otraCell.Style.BackColor = otroColorFondo;
                            }
                        }
                    }
                };
                timer.Start();

                System.Diagnostics.Debug.WriteLine($"✅ Campo {columnName} actualizado para producto {codigo}: {valorBool}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando campo: {ex.Message}");
                MessageBox.Show($"Error al actualizar el campo: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ActualizarCampoEnBD(string codigo, string campo, bool valor)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection") ?? "";
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    string query = $@"UPDATE Productos 
                                     SET {campo} = @valor 
                                     WHERE codigo = @codigo";
                    
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@valor", valor ? 1 : 0);
                        cmd.Parameters.AddWithValue("@codigo", codigo);
                        
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Limpiar cache para que se refleje el cambio
                LimpiarCache();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al actualizar {campo}: {ex.Message}");
            }
        }

        private void CrearBotonEliminar()
        {
            // Crear botón Eliminar - CON EL MISMO DISEÑO que Modificar y Agregar
            var btnEliminar = new Button
            {
                Text = "🗑️ Eliminar",
                Location = new Point(685, 40), // ✅ Alineado a la derecha de "Agregar"
                Size = new Size(120, 27), // ✅ MISMO tamaño (27 de alto, no 28)
                Font = new Font("Segoe UI", 9F), // ✅ MISMA fuente (sin Bold)
                UseVisualStyleBackColor = true, // ✅ MISMO estilo visual que los otros
                Cursor = Cursors.Hand,
                Name = "btnEliminar",
                TabIndex = 5
            };
            btnEliminar.Click += BtnEliminar_Click;

            this.Controls.Add(btnEliminar);
            btnEliminar.BringToFront();
        }

        private async void BtnEliminar_Click(object sender, EventArgs e)
        {
            try
            {
                if (GrillaProductos.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Seleccione un producto para eliminar.", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var filaSeleccionada = GrillaProductos.SelectedRows[0];
                var codigo = filaSeleccionada.Cells["codigo"].Value.ToString();
                var descripcion = filaSeleccionada.Cells["descripcion"].Value.ToString();

                // Confirmar eliminación PERMANENTE
                var resultado = MessageBox.Show(
                    $"⚠️ ADVERTENCIA: Esta acción eliminará PERMANENTEMENTE el producto de la base de datos.\n\n" +
                    $"Código: {codigo}\n" +
                    $"Descripción: {descripcion}\n\n" +
                    $"Esta acción NO se puede deshacer.\n\n" +
                    $"Si desea ocultarlo temporalmente, use el checkbox 'ACTIVO' en su lugar.\n\n" +
                    $"¿Está seguro de que desea ELIMINAR PERMANENTEMENTE este producto?",
                    "⚠️ Confirmar Eliminación Permanente",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (resultado == DialogResult.Yes)
                {
                    // Eliminar producto PERMANENTEMENTE de la base de datos
                    await EliminarProductoPermanentemente(codigo);

                    // Guardar el filtro actual
                    string filtroActual = txtFiltroDescripcion.Text;

                    // Actualizar la grilla manteniendo el filtro
                    await ActualizarDatosManteniendoFiltro(filtroActual);

                    MessageBox.Show("Producto eliminado permanentemente de la base de datos.", "Eliminación Exitosa",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    txtFiltroDescripcion.Focus();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en BtnEliminar_Click: {ex.Message}");
                MessageBox.Show($"Error al eliminar producto: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task EliminarProductoPermanentemente(string codigo)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection") ?? "";

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // ✅ ELIMINAR PERMANENTEMENTE el producto
                    string query = @"DELETE FROM Productos WHERE codigo = @codigo";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@codigo", codigo);
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            throw new Exception("No se encontró el producto a eliminar.");
                        }
                    }
                }

                // Limpiar cache
                LimpiarCache();

                System.Diagnostics.Debug.WriteLine($"✅ Producto {codigo} ELIMINADO PERMANENTEMENTE de la base de datos");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al eliminar producto permanentemente: {ex.Message}");
            }
        }

        public static void LimpiarCache()
        {
            try
            {
                // Limpiar cache de productos
                _cacheProductos?.Dispose();
                _cacheProductos = null;
                _ultimaActualizacionCache = DateTime.MinValue;
                
                System.Diagnostics.Debug.WriteLine("🧹 Cache de productos limpiado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error limpiando cache: {ex.Message}");
            }
        }
        #endregion
    }
}