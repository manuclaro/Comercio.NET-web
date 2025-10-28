using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;

namespace Comercio.NET.Formularios
{
    public partial class ControlComprasProveedores : UserControl
    {
        private ComboBox cmbRango;
        private DateTimePicker dtpDesde;
        private DateTimePicker dtpHasta;
        private Button btnRefrescar;
        private Button btnNuevo;
        private DataGridView dgv;
        private Label lblTotales;

        // Grilla para totalizar por alícuota
        private DataGridView dgvIvaTotals;

        // Nuevos elementos para estilo
        private Panel pnlHeader;
        private Panel pnlContent;

        // Nuevo filtro por proveedor
        private ComboBox cmbProveedor;
        private Label lblProveedor; // nueva label
        private Label lblFecha; // nueva label para el filtro Fecha

        // Flag para evitar reentradas mientras poblamos el combo
        private bool isPopulatingProveedor = false;

        // padding usado en todo el control (antes era variable local)
        private readonly int contentPadding = 12;

        public ControlComprasProveedores()
        {
            InitializeComponent();
            // carga inicial: solo aplicar rango (antes cargábamos proveedores desde la tabla)
            this.Load += async (s, e) =>
            {
                await AplicarRangoYCargarAsync();
            };
        }

        private void InitializeComponent()
        {
            // Estilos generales del control (coincidentes con ComprasProveedorForm)
            this.Dock = DockStyle.Fill;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(250, 250, 250);

            // Header
            var headerHeight = 64;
            pnlHeader = new Panel
            {
                Left = 0,
                Top = 0,
                Width = this.ClientSize.Width,
                Height = headerHeight,
                BackColor = Color.FromArgb(63, 81, 181) // tono indigo
            };
            pnlHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var lblIcon = new Label
            {
                Text = "+",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point),
                Left = 12,
                Top = 8,
                AutoSize = true
            };

            var lblTitle = new Label
            {
                Text = "Control Compras Proveedores",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Left = lblIcon.Right + 8,
                Top = 12,
                AutoSize = true
            };

            var lblSubtitle = new Label
            {
                Text = "Listado y gestión de compras por proveedor",
                ForeColor = Color.FromArgb(230, 230, 255),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Left = lblIcon.Right + 8,
                Top = lblTitle.Bottom - 6,
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblIcon);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // Contenedor principal de contenido (panel blanco con padding)
            var contentTop = pnlHeader.Bottom + contentPadding;
            pnlContent = new Panel
            {
                Left = contentPadding,
                Top = contentTop,
                Width = this.ClientSize.Width - contentPadding * 2,
                Height = this.ClientSize.Height - headerHeight - contentPadding * 2,
                BackColor = Color.White
            };
            pnlContent.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;


            // Controles dentro de pnlContent

            // Label para el filtro Fecha (añadir antes de cmbRango)
            lblFecha = new Label
            {
                Left = 12,
                Top = 14,
                AutoSize = true,
                Text = "Fecha:",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.Black
            };

            cmbRango = new ComboBox
            {
                Left = lblFecha.Right + 6, // ahora a la derecha de lblFecha
                Top = 12,
                Width = 180, // reducido
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Hice los controles más compactos para que quepan en pantallas pequeñas
            cmbRango = new ComboBox
            {
                Left = 12,
                Top = 12,
                Width = 180, // reducido
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbRango.Items.AddRange(new object[] { "Hoy", "Esta semana", "Este mes", "Rango personalizado" });
            cmbRango.SelectedIndex = 0;
            cmbRango.SelectedIndexChanged += CmbRango_SelectedIndexChanged;

            dtpDesde = new DateTimePicker { Left = cmbRango.Right + 8, Top = 12, Width = 100, Format = DateTimePickerFormat.Short, Visible = false };
            dtpHasta = new DateTimePicker { Left = dtpDesde.Right + 8, Top = 12, Width = 100, Format = DateTimePickerFormat.Short, Visible = false };

            // Label para el combo proveedor
            lblProveedor = new Label
            {
                Left = 12,
                Top = 14,
                AutoSize = true,
                Text = "Proveedor:",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.Black
            };

            // Combo de Proveedores
            cmbProveedor = new ComboBox
            {
                Left = lblProveedor.Right + 6,
                Top = 12,
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbProveedor.SelectedIndexChanged += CmbProveedor_SelectedIndexChanged;

            // Label para el filtro Fecha (después de Proveedor)
            lblFecha = new Label
            {
                Left = cmbProveedor.Right + 8,
                Top = 14,
                AutoSize = true,
                Text = "Fecha:",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.Black
            };

            // Combo de rango de fecha (a la derecha de lblFecha)
            cmbRango = new ComboBox
            {
                Left = lblFecha.Right + 6,
                Top = 12,
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbRango.Items.AddRange(new object[] { "Hoy", "Esta semana", "Este mes", "Rango personalizado" });
            cmbRango.SelectedIndex = 0;
            cmbRango.SelectedIndexChanged += CmbRango_SelectedIndexChanged;

            // DatePickers (a la derecha de cmbRango)
            dtpDesde = new DateTimePicker { Left = cmbRango.Right + 8, Top = 12, Width = 100, Format = DateTimePickerFormat.Short, Visible = false };
            dtpHasta = new DateTimePicker { Left = dtpDesde.Right + 8, Top = 12, Width = 100, Format = DateTimePickerFormat.Short, Visible = false };

            // Botones (se colocan después; posicionamiento final en Resize)
            btnRefrescar = new Button
            {
                Top = 10,
                Width = 100,
                Text = "Refrescar",
                BackColor = Color.FromArgb(160, 160, 160),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnRefrescar.FlatAppearance.BorderSize = 0;
            btnRefrescar.Click += async (s, e) => await AplicarRangoYCargarAsync();

            btnNuevo = new Button
            {
                Top = 10,
                Width = 100,
                Text = "Nuevo",
                BackColor = Color.FromArgb(60, 179, 113),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnNuevo.FlatAppearance.BorderSize = 0;
            btnNuevo.Click += BtnNuevo_Click;

            // Por defecto dejaremos la grilla principal más alta; la altura final se ajusta en Resize
            dgv = new DataGridView
            {
                Left = 12,
                Top = cmbRango.Bottom + 12,
                Width = pnlContent.Width - 24,
                Height = Math.Max(100, pnlContent.Height - 200),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                // Deshabilitar agregar/eliminar filas por parte del usuario
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false
            };
            dgv.DoubleClick += Dgv_DoubleClick;

            lblTotales = new Label
            {
                Left = 12,
                Top = dgv.Bottom + 6,
                Width = 500, // ancho fijo para centrar después
                Height = 22,
                Anchor = AnchorStyles.Bottom,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = "Totales: Neto $0.00 | IVA $0.00 | Total $0.00",
                TextAlign = ContentAlignment.MiddleCenter // centrar texto
            };

            // Grilla para totalización por alícuota (será más angosta y centrada)
            dgvIvaTotals = new DataGridView
            {
                // width y left se calcularán en el Resize para centrarla
                Top = lblTotales.Bottom + 1,
                Height = 110,
                Anchor = AnchorStyles.Bottom,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                ScrollBars = ScrollBars.None
            };
            // Columnas con anchos fijos (más ancha para mostrar la 3ª columna)
            dgvIvaTotals.Columns.Add(new DataGridViewTextBoxColumn { Name = "Alicuota", HeaderText = "Alícuota %", Width = 100 });
            dgvIvaTotals.Columns.Add(new DataGridViewTextBoxColumn { Name = "Base", HeaderText = "Base Imponible", Width = 220 });
            dgvIvaTotals.Columns.Add(new DataGridViewTextBoxColumn { Name = "ImporteIva", HeaderText = "IVA $", Width = 160 });

            // Alineaciones de encabezados
            dgvIvaTotals.Columns["Alicuota"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvIvaTotals.Columns["Base"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvIvaTotals.Columns["ImporteIva"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;

        pnlContent.Controls.AddRange(new Control[] { lblProveedor, cmbProveedor, lblFecha, cmbRango, dtpDesde, dtpHasta, btnRefrescar, btnNuevo, dgv, lblTotales, dgvIvaTotals });
            // Añadir header y contenido al control
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlContent);

    
            // Manejo de resize para ajustar dgv y paneles y centrar la grilla de alícuotas
            this.Resize += (s, e) =>
            {
                pnlHeader.Width = this.ClientSize.Width;
                pnlContent.Left = contentPadding;
                pnlContent.Top = pnlHeader.Bottom + contentPadding;
                pnlContent.Width = this.ClientSize.Width - contentPadding * 2;
                pnlContent.Height = Math.Max(220, this.ClientSize.Height - pnlHeader.Height - contentPadding * 2);

                // Colocación horizontal de controles de filtros (más robusta)
                int startX = 12;
                int gap = 8;

                // ajustar ancho de controles según espacio disponible
                int availableWidth = pnlContent.ClientSize.Width - 24;
                int btnsTotalWidth = btnNuevo.Width + gap + btnRefrescar.Width + 12; // margen derecho
                int maxFiltersWidth = Math.Max(200, availableWidth - btnsTotalWidth - 40);

                // Proveedor a la izquierda
                lblProveedor.Left = startX;
                lblProveedor.Top = 14;
                cmbProveedor.Left = lblProveedor.Right + 6;
                //cmbProveedor.Width = Math.Min(220, Math.Max(90, maxFiltersWidth / 3));

                // Fecha a la derecha de Proveedor
                lblFecha.Left = cmbProveedor.Right + gap;
                lblFecha.Top = 14;
                cmbRango.Left = lblFecha.Right + gap;
                cmbRango.Width = Math.Min(220, Math.Max(140, maxFiltersWidth / 4));

                // DatePickers a la derecha de cmbRango
                dtpDesde.Left = cmbRango.Right + gap;
                dtpDesde.Width = Math.Min(120, Math.Max(90, (maxFiltersWidth / 8)));
                dtpHasta.Left = dtpDesde.Right + gap;
                dtpHasta.Width = dtpDesde.Width;

                // colocar botones alineados a la derecha
                int rightPadding = 12;
                btnNuevo.Left = pnlContent.ClientSize.Width - rightPadding - btnNuevo.Width;
                btnRefrescar.Left = btnNuevo.Left - gap - btnRefrescar.Width;

                // Si los filtros invaden el espacio de los botones, reducir ancho del combo proveedor
                if (cmbProveedor.Right + 12 > btnRefrescar.Left)
                {
                    int allowed = Math.Max(80, btnRefrescar.Left - cmbProveedor.Left - 12);
                    cmbProveedor.Width = allowed;
                }

                // Asegurar que dtp y combos no queden fuera
                if (cmbProveedor.Right + 12 > pnlContent.ClientSize.Width - btnsTotalWidth)
                {
                    // mover botones debajo de filtros si no hay espacio horizontal
                    btnRefrescar.Top = dtpDesde.Bottom + 8;
                    btnNuevo.Top = btnRefrescar.Top;
                }
                else
                {
                    btnRefrescar.Top = 10;
                    btnNuevo.Top = 10;
                }

                // Ajustar altura y ancho de la grilla principal
                dgv.Left = 12;
                dgv.Top = cmbRango.Bottom + 12;
                dgv.Width = pnlContent.ClientSize.Width - 24;

                // lblTotales y posicionamiento de la grilla de alícuotas se calcularán dentro AjustarIvaTotalsSize
                AjustarIvaTotalsSize();
            };
        }

        // Ajusta el tamaño de dgvIvaTotals para que calce exactamente a sus columnas y filas
        private void AjustarIvaTotalsSize()
        {
            if (dgvIvaTotals == null || pnlContent == null) return;

            // ancho total requerido por columnas
            int totalColsWidth = dgvIvaTotals.Columns.Cast<DataGridViewColumn>().Sum(c => c.Width);
            int rowHeader = dgvIvaTotals.RowHeadersVisible ? dgvIvaTotals.RowHeadersWidth : 0;

            // si las filas exceden la altura visible, aparecerá scrollbar vertical; estimamos si es necesario
            int visibleRows = Math.Max(1, (pnlContent.ClientSize.Height - 200) / dgvIvaTotals.RowTemplate.Height);
            bool needVScroll = dgvIvaTotals.Rows.Count > visibleRows;

            int vScrollWidth = needVScroll ? SystemInformation.VerticalScrollBarWidth : 0;

            // ancho objetivo: suma columnas + posibles scroll + bordes
            int desiredWidth = totalColsWidth + rowHeader + vScrollWidth + 4;

            // limitar al ancho disponible menos márgenes
            int maxAvailable = Math.Max(0, pnlContent.ClientSize.Width - 24);
            dgvIvaTotals.Width = Math.Min(desiredWidth, maxAvailable);

            // si el ancho resultante es menor que la suma de columnas, habilitamos scroll horizontal
            if (dgvIvaTotals.Width < desiredWidth)
                dgvIvaTotals.ScrollBars = ScrollBars.Horizontal | ScrollBars.Vertical;
            else
                dgvIvaTotals.ScrollBars = ScrollBars.None;

            // altura: header + filas (ajustada)
            int headerH = dgvIvaTotals.ColumnHeadersHeight;
            int rowsH = dgvIvaTotals.RowTemplate.Height * Math.Max(1, dgvIvaTotals.Rows.Count);
            int desiredHeight = headerH + rowsH + 4;

            // dejar un máximo razonable (evita comer toda la pantalla)
            int maxHeight = Math.Min(200, pnlContent.ClientSize.Height / 3 + 20);
            dgvIvaTotals.Height = Math.Min(desiredHeight, maxHeight);

            // centrar horizontalmente y pegar al borde inferior
            dgvIvaTotals.Left = (pnlContent.ClientSize.Width - dgvIvaTotals.Width) / 2;
            dgvIvaTotals.Top = pnlContent.ClientSize.Height - contentPadding - dgvIvaTotals.Height;

            // lblTotales justo encima de la grilla de alícuotas, centrado horizontalmente
            int lblWidth = Math.Min(600, pnlContent.ClientSize.Width - 24);
            lblTotales.Width = lblWidth;
            lblTotales.Left = (pnlContent.ClientSize.Width - lblTotales.Width) / 2;
            lblTotales.Top = dgvIvaTotals.Top - 6 - lblTotales.Height;

            // ajustar altura de la grilla principal según la nueva posición
            dgv.Height = Math.Max(80, lblTotales.Top - dgv.Top - 12);
        }

        private async void CmbRango_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool personalizado = cmbRango.SelectedItem?.ToString() == "Rango personalizado";
            dtpDesde.Visible = dtpHasta.Visible = personalizado;
            await AplicarRangoYCargarAsync();
        }

        // Nuevo handler seguro para el combo de proveedores (evita reentradas)
        private async void CmbProveedor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isPopulatingProveedor) return;
            await AplicarRangoYCargarAsync();
        }

        private async Task AplicarRangoYCargarAsync()
        {
            DateTime desde, hasta;
            var opcion = (cmbRango.SelectedItem?.ToString() ?? "Hoy");
            var hoy = DateTime.Today;

            switch (opcion)
            {
                case "Hoy":
                    desde = hoy;
                    hasta = hoy;
                    break;
                case "Esta semana":
                    desde = StartOfWeek(hoy, DayOfWeek.Monday);
                    hasta = hoy;
                    break;
                case "Este mes":
                    desde = new DateTime(hoy.Year, hoy.Month, 1);
                    hasta = hoy;
                    break;
                case "Rango personalizado":
                    desde = dtpDesde.Value.Date;
                    hasta = dtpHasta.Value.Date;
                    if (hasta < desde) { MessageBox.Show("El rango no es válido: 'Hasta' es anterior a 'Desde'.", "Rango inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    break;
                default:
                    desde = hoy;
                    hasta = hoy;
                    break;
            }

            await CargarComprasAsync(desde, hasta);
        }

        private DateTime StartOfWeek(DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        private string GetConnectionString()
        {
            var cfg = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return cfg.GetConnectionString("DefaultConnection");
        }

        private async Task CargarComprasAsync(DateTime desde, DateTime hasta)
        {
            try
            {
                DataTable dt = new DataTable();
                string cs = GetConnectionString();
                using (var conn = new SqlConnection(cs))
                {
                    // Incluir día completo: desde 00:00 hasta 23:59:59
                    var desdeFecha = desde.Date;
                    var hastaFecha = hasta.Date.AddDays(1).AddTicks(-1);

                    // OBTENER compras SIN FILTRO POR PROVEEDOR — necesitaremos la lista de proveedores desde estos registros
                    string sql = @"
SELECT cp.Id, cp.NumeroFactura, cp.Fecha, ISNULL(p.Nombre, cp.Proveedor) AS Proveedor, 
       cp.ImporteNeto, cp.ImporteIVA, cp.ImporteTotal, cp.Usuario
FROM ComprasProveedores cp
LEFT JOIN Proveedores p ON cp.ProveedorId = p.Id
WHERE cp.Fecha BETWEEN @desde AND @hasta
ORDER BY cp.Fecha DESC, cp.Id DESC;";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@desde", desdeFecha);
                        cmd.Parameters.AddWithValue("@hasta", hastaFecha);
                        using (var da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));
                        }
                    }

                    // POPULAR combo de proveedores desde los registros obtenidos
                    ActualizarComboProveedorDesdeCompras(dt);

                    // Determinar proveedor seleccionado (nombre). Si es "Todos" o vacío -> sin filtro por proveedor
                    string proveedorSeleccionado = "";
                    if (cmbProveedor?.SelectedValue != null)
                        proveedorSeleccionado = cmbProveedor.SelectedValue.ToString();

                    // Filtrar en memoria la tabla de compras para mostrar sólo el proveedor seleccionado (si corresponde)
                    DataTable dtParaMostrar = dt;
                    if (!string.IsNullOrEmpty(proveedorSeleccionado))
                    {
                        // Escapar comillas simples para RowFilter
                        var escaped = proveedorSeleccionado.Replace("'", "''");
                        var dv = new DataView(dt) { RowFilter = $"Proveedor = '{escaped}'" };
                        dtParaMostrar = dv.ToTable();
                    }

                    // asignar valores a la UI (fuera del using de conexión)
                    dgv.DataSource = dtParaMostrar;

                    FormatearGrilla();
                    

                    // Calcular totales sobre dtParaMostrar
                    decimal sumaNeto = 0m, sumaIva = 0m, sumaTotal = 0m;
                    foreach (DataRow row in dtParaMostrar.Rows)
                    {
                        if (row["ImporteNeto"] != DBNull.Value && decimal.TryParse(row["ImporteNeto"].ToString(), out decimal n)) sumaNeto += n;
                        if (row["ImporteIVA"] != DBNull.Value && decimal.TryParse(row["ImporteIVA"].ToString(), out decimal v)) sumaIva += v;
                        if (row["ImporteTotal"] != DBNull.Value && decimal.TryParse(row["ImporteTotal"].ToString(), out decimal t)) sumaTotal += t;
                    }

                    lblTotales.Text = $"Totales ({desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}):     Neto {sumaNeto:C2}    |    IVA {sumaIva:C2}    |    Total {sumaTotal:C2}";

                    // OBTENER totales por alícuota desde la BD:
                    var dtAlicuotas = new DataTable();

                    // Base SQL y, si hay proveedor seleccionado, se agrega condición de nombre de proveedor
                    string sqlAlicuotas = @"
                        SELECT d.Alicuota, SUM(d.BaseImponible) AS BaseSum, SUM(d.ImporteIva) AS IvaSum
                        FROM ComprasProveedoresIvaDetalle d
                        INNER JOIN ComprasProveedores cp ON d.CompraId = cp.Id
                        LEFT JOIN Proveedores p ON cp.ProveedorId = p.Id
                        WHERE cp.Fecha BETWEEN @desde AND @hasta
                        ";
                    if (!string.IsNullOrEmpty(proveedorSeleccionado))
                    {
                        sqlAlicuotas += "  AND ISNULL(p.Nombre, cp.Proveedor) = @proveedorName\n";
                    }
                    sqlAlicuotas += @"
                        GROUP BY d.Alicuota
                        ORDER BY d.Alicuota DESC;";

                    using (var cmdA = new SqlCommand(sqlAlicuotas, conn))
                    {
                        cmdA.Parameters.AddWithValue("@desde", desdeFecha);
                        cmdA.Parameters.AddWithValue("@hasta", hastaFecha);
                        if (!string.IsNullOrEmpty(proveedorSeleccionado))
                            cmdA.Parameters.AddWithValue("@proveedorName", proveedorSeleccionado);
                        using (var da2 = new SqlDataAdapter(cmdA))
                        {
                            await Task.Run(() => da2.Fill(dtAlicuotas));
                        }
                    }

                    // rellenar dgvIvaTotals desde dtAlicuotas
                    dgvIvaTotals.Rows.Clear();
                    foreach (DataRow r in dtAlicuotas.Rows)
                    {
                        var ali = r["Alicuota"] == DBNull.Value ? 0m : Convert.ToDecimal(r["Alicuota"]);
                        var baseSum = r["BaseSum"] == DBNull.Value ? 0m : Convert.ToDecimal(r["BaseSum"]);
                        var ivaSum = r["IvaSum"] == DBNull.Value ? 0m : Convert.ToDecimal(r["IvaSum"]);
                        dgvIvaTotals.Rows.Add(ali.ToString("N2"), baseSum.ToString("C2"), ivaSum.ToString("C2"));
                    }
                }

                // Alineaciones de celdas de la grilla de alícuotas (fuera del using)
                if (dgvIvaTotals.Columns.Contains("Alicuota")) dgvIvaTotals.Columns["Alicuota"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (dgvIvaTotals.Columns.Contains("Base")) dgvIvaTotals.Columns["Base"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                if (dgvIvaTotals.Columns.Contains("ImporteIva")) dgvIvaTotals.Columns["ImporteIva"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                // Ajustar tamaño y posición tras llenar filas
                AjustarIvaTotalsSize();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando compras: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Rellena el combo de proveedores usando la columna "Proveedor" del DataTable de compras
        private void ActualizarComboProveedorDesdeCompras(DataTable comprasDt)
        {
            try
            {
                var dtProv = new DataTable();
                dtProv.Columns.Add("Nombre", typeof(string)); // texto mostrado
                dtProv.Columns.Add("Value", typeof(string));  // valor real usado para filtrar

                // Opción "Todos" -> mostrar "Todos", valor vacío para indicar sin filtro
                var rowTodos = dtProv.NewRow();
                rowTodos["Nombre"] = "Todos";
                rowTodos["Value"] = "";
                dtProv.Rows.Add(rowTodos);

                if (comprasDt != null && comprasDt.Rows.Count > 0)
                {
                    var providers = comprasDt.AsEnumerable()
                        .Select(r => r["Proveedor"] == DBNull.Value ? "" : r["Proveedor"].ToString())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct(StringComparer.CurrentCultureIgnoreCase)
                        .OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase)
                        .ToList();

                    foreach (var p in providers)
                    {
                        var r = dtProv.NewRow();
                        r["Nombre"] = p;
                        r["Value"] = p;
                        dtProv.Rows.Add(r);
                    }
                }

                // Evitar que la asignación del DataSource dispare SelectedIndexChanged y cause reentrada
                isPopulatingProveedor = true;
                try
                {
                    // preservar selección previa (por valor)
                    var previous = cmbProveedor?.SelectedValue?.ToString();

                    cmbProveedor.DataSource = dtProv;
                    cmbProveedor.DisplayMember = "Nombre";
                    cmbProveedor.ValueMember = "Value";

                    // restaurar selección previa si existe en la nueva lista; sino "Todos" (valor "")
                    if (!string.IsNullOrEmpty(previous))
                    {
                        var exists = dtProv.AsEnumerable().Any(r => string.Equals(r.Field<string>("Value"), previous, StringComparison.CurrentCultureIgnoreCase));
                        cmbProveedor.SelectedValue = exists ? previous : "";
                    }
                    else
                    {
                        cmbProveedor.SelectedValue = "";
                    }
                }
                finally
                {
                    isPopulatingProveedor = false;
                }
            }
            catch
            {
                // no interrumpir la carga por fallos al poblar el combo
                cmbProveedor.DataSource = null;
            }
        }

        private void FormatearGrilla()
        {
            if (dgv.Columns.Contains("Fecha"))
            {
                dgv.Columns["Fecha"].DefaultCellStyle.Format = "dd/MM/yyyy";
                dgv.Columns["Fecha"].Width = 100;
            }
            if (dgv.Columns.Contains("ImporteNeto"))
            {
                dgv.Columns["ImporteNeto"].DefaultCellStyle.Format = "C2";
                dgv.Columns["ImporteNeto"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgv.Columns["ImporteNeto"].Width = 100;
            }
            if (dgv.Columns.Contains("ImporteIVA"))
            {
                dgv.Columns["ImporteIVA"].DefaultCellStyle.Format = "C2";
                dgv.Columns["ImporteIVA"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgv.Columns["ImporteIVA"].Width = 100;
            }
            if (dgv.Columns.Contains("ImporteTotal"))
            {
                dgv.Columns["ImporteTotal"].DefaultCellStyle.Format = "C2";
                dgv.Columns["ImporteTotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgv.Columns["ImporteTotal"].Width = 120;
            }

            // Oculta Id si existe
            if (dgv.Columns.Contains("Id")) dgv.Columns["Id"].Visible = false;
        }

        private async void BtnNuevo_Click(object sender, EventArgs e)
        {
            try
            {
                using (var frm = new ComprasProveedorForm())
                {
                    var res = frm.ShowDialog(this);
                    // al cerrar, refrescar la vista (mantener rango actual)
                }
                await AplicarRangoYCargarAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo formulario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Dgv_DoubleClick(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) return;
            var row = dgv.SelectedRows[0];
            if (row.Cells["Id"].Value == null) return;
            if (!int.TryParse(row.Cells["Id"].Value.ToString(), out int compraId)) return;

            // Obtener el formulario padre para centrar el modal respecto al MDI principal
            var ownerForm = this.FindForm();
            using (var detalle = new DetalleCompraForm(compraId))
            {
                // Asegurar centrado respecto al owner
                detalle.StartPosition = FormStartPosition.CenterParent;
                detalle.ShowDialog(ownerForm);
            }
        }

        // Form modal simple para mostrar detalle IVA por compra
        private class DetalleCompraForm : Form
        {
            public DetalleCompraForm(int compraId)
            {
                this.Text = $"Detalle compra #{compraId}";
                this.ClientSize = new Size(420, 160);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;

                var dgvDet = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    // Deshabilitar agregar/eliminar filas en el detalle
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect
                };

                // Panel footer con totalizador
                var pnlFooter = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 36,
                    Padding = new Padding(6)
                };
                var lblTotal = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = "Totales: Base $0.00 | IVA $0.00 | Total $0.00",
                    TextAlign = ContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };
                pnlFooter.Controls.Add(lblTotal);

                this.Controls.Add(dgvDet);
                this.Controls.Add(pnlFooter);

                Load += async (s, e) =>
                {
                    var dt = new DataTable();
                    try
                    {
                        var cs = new ConfigurationBuilder()
                            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                            .AddJsonFile("appsettings.json")
                            .Build()
                            .GetConnectionString("DefaultConnection");

                        using (var conn = new SqlConnection(cs))
                        {
                            var sql = @"SELECT Alicuota, BaseImponible, ImporteIva FROM ComprasProveedoresIvaDetalle WHERE CompraId = @id";
                            using (var cmd = new SqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", compraId);
                                using (var da = new SqlDataAdapter(cmd))
                                {
                                    await Task.Run(() => da.Fill(dt));
                                }
                            }
                        }

                        dgvDet.DataSource = dt;

                        if (dgvDet.Columns.Contains("BaseImponible")) dgvDet.Columns["BaseImponible"].DefaultCellStyle.Format = "C2";
                        if (dgvDet.Columns.Contains("ImporteIva")) dgvDet.Columns["ImporteIva"].DefaultCellStyle.Format = "C2";
                        if (dgvDet.Columns.Contains("Alicuota")) dgvDet.Columns["Alicuota"].DefaultCellStyle.Format = "N2";

                        // Alineaciones solicitadas
                        if (dgvDet.Columns.Contains("Alicuota")) dgvDet.Columns["Alicuota"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        if (dgvDet.Columns.Contains("BaseImponible")) dgvDet.Columns["BaseImponible"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        if (dgvDet.Columns.Contains("ImporteIva")) dgvDet.Columns["ImporteIva"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                        // Calcular totales y actualizar label
                        decimal sumaBase = 0m, sumaIva = 0m;
                        foreach (DataRow r in dt.Rows)
                        {
                            if (r["BaseImponible"] != DBNull.Value && decimal.TryParse(r["BaseImponible"].ToString(), out decimal b)) sumaBase += b;
                            if (r["ImporteIva"] != DBNull.Value && decimal.TryParse(r["ImporteIva"].ToString(), out decimal iv)) sumaIva += iv;
                        }
                        var sumaTotal = sumaBase + sumaIva;
                        lblTotal.Text = $"Totales: Base {sumaBase:C2} | IVA {sumaIva:C2} | Total {sumaTotal:C2}";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error cargando detalle: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
            }
        }
    }

    public class ComprasProveedorControlForm : Form
    {
        private ControlComprasProveedores control;

        public ComprasProveedorControlForm()
        {
            this.Text = "Control Compras Proveedores";
            this.ClientSize = new Size(900, 515);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.BackColor = Color.FromArgb(250, 250, 250);

            control = new ControlComprasProveedores { Dock = DockStyle.Fill };
            this.Controls.Add(control);
        }
    }
}