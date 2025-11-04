using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Collections.Generic;
using System.Globalization;
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
        private Button btnImprimir; // boton existente (totales por día)
        private Button btnImprimirMensual; // nuevo botón (totales por mes)
        private Button btnImprimirMensualProveedor; // nuevo botón (mensual detallado por proveedor)
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

        // Nuevo botón para registrar pagos sobre una compra seleccionada
        private Button btnRegistrarPago;

        // Flag para evitar reentradas mientras poblamos el combo
        private bool isPopulatingProveedor = false;

        // padding usado en todo el control (antes era variable local)
        private readonly int contentPadding = 12;

        // Datos y rango actuales (para impresión / reuso)
        private DataTable currentComprasDt;
        private DataTable currentAlicuotasDt;
        private DateTime lastDesde;
        private DateTime lastHasta;
        private string lastProveedorSeleccionado = "";

        // Impresión
        private PrintDocument printDoc;
        private List<string> printLines;
        private Font printFont = new Font("Consolas", 9F);
        private int currentPrintLine;

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
                Font = new Font("Segoe UI", 36F, FontStyle.Bold, GraphicsUnit.Point),
                Left = 12,
                Top = -6,
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
                Top = lblTitle.Bottom - 4,
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

            btnImprimir = new Button
            {
                Top = 10,
                Width = 100,
                Text = "Imprimir (día)",
                BackColor = Color.FromArgb(33, 150, 243),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnImprimir.FlatAppearance.BorderSize = 0;
            btnImprimir.Click += async (s, e) => await BtnImprimir_Click(s, e);

            // Nuevo botón: imprimir resumen mensual (1 total por mes)
            btnImprimirMensual = new Button
            {
                Top = 10,
                Width = 130,
                Text = "Imprimir (meses)",
                BackColor = Color.FromArgb(0, 150, 136),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnImprimirMensual.FlatAppearance.BorderSize = 0;
            btnImprimirMensual.Click += async (s, e) => await BtnImprimirMensual_Click(s, e);

            // Nuevo botón: imprimir resumen mensual detallado por proveedor
            btnImprimirMensualProveedor = new Button
            {
                Top = 10,
                Width = 170,
                Text = "Imprimir (meses x proveedor)",
                BackColor = Color.FromArgb(0, 121, 107),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnImprimirMensualProveedor.FlatAppearance.BorderSize = 0;
            btnImprimirMensualProveedor.Click += async (s, e) => await BtnImprimirMensualPorProveedor_Click(s, e);

            // Nuevo botón: Registrar Pago sobre compra seleccionada
            btnRegistrarPago = new Button
            {
                Top = 10,
                Width = 120,
                Text = "Registrar Pago",
                BackColor = Color.FromArgb(220, 57, 18), // color distintivo
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnRegistrarPago.FlatAppearance.BorderSize = 0;
            btnRegistrarPago.Click += BtnRegistrarPago_Click;

            // Por defecto dejaremos la grilla principal más alta; la altura final se ajusta en Resize
            dgv = new DataGridView
            {
                Left = 12,
                Top = cmbRango.Bottom + 12,
                Width = pnlContent.Width - 24,
                Height = Math.Max(100, pnlContent.Height - 260),
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
                Height = 160,
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

            pnlContent.Controls.AddRange(new Control[] { lblProveedor, cmbProveedor, lblFecha, cmbRango, dtpDesde, dtpHasta, btnRefrescar, btnRegistrarPago, btnImprimirMensualProveedor, btnImprimirMensual, btnImprimir, btnNuevo, dgv, lblTotales, dgvIvaTotals });
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
                // Dentro del manejador this.Resize, reemplazar la asignación de Height de pnlContent:
                pnlContent.Height = Math.Max(220, this.ClientSize.Height - pnlHeader.Height - contentPadding * 2 );
                
                // Colocación horizontal de controles de filtros (más robusta)
                int startX = 12;
                int gap = 8;

                // ajustar ancho de controles según espacio disponible
                int availableWidth = pnlContent.ClientSize.Width - 24;
                int btnsTotalWidth = btnNuevo.Width + gap + btnImprimir.Width + gap + btnImprimirMensual.Width + gap + btnImprimirMensualProveedor.Width + gap + btnRegistrarPago.Width + gap + btnRefrescar.Width + 12; // margen derecho
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

                // colocar botones de acciones principales (excluyendo botones de impresión)
                int rightPadding = 12;
                btnNuevo.Left = pnlContent.ClientSize.Width - rightPadding - btnNuevo.Width;
                btnRefrescar.Left = btnNuevo.Left - gap - btnRefrescar.Width;
                // nuevo: colocar btnRegistrarPago a la izquierda de btnRefrescar
                btnRegistrarPago.Left = btnRefrescar.Left - gap - btnRegistrarPago.Width;
                // dejamos botones de impresión para posicionarlos junto a dgvIvaTotals en AjustarIvaTotalsSize

                // Si los filtros invaden el espacio de los botones, reducir ancho del combo proveedor
                if (cmbProveedor.Right + 12 > btnRegistrarPago.Left)
                {
                    int allowed = Math.Max(80, btnRegistrarPago.Left - cmbProveedor.Left - 12);
                    cmbProveedor.Width = allowed;
                }

                // Asegurar que dtp y combos no queden fuera
                if (cmbProveedor.Right + 12 > pnlContent.ClientSize.Width - btnsTotalWidth)
                {
                    // mover botones debajo de filtros si no hay espacio horizontal
                    btnRefrescar.Top = dtpDesde.Bottom + 8;
                    btnRegistrarPago.Top = btnRefrescar.Top;
                    btnNuevo.Top = btnRefrescar.Top;
                }
                else
                {
                    btnRefrescar.Top = 10;
                    btnRegistrarPago.Top = 10;
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

            // Reemplazar el cálculo de altura máxima y asignación dentro de `AjustarIvaTotalsSize()`
            // para permitir que la grilla de alícuotas sea más alta y forzar un mínimo razonable.
            int headerH = dgvIvaTotals.ColumnHeadersHeight;
            int rowsH = dgvIvaTotals.RowTemplate.Height * Math.Max(1, dgvIvaTotals.Rows.Count);
            int desiredHeight = headerH + rowsH + 8;

            // permitir un máximo mayor (hasta la mitad del panel) y garantizar mínimo visual
            int maxHeight = Math.Min(320, pnlContent.ClientSize.Height / 2);
            dgvIvaTotals.Height = Math.Min(Math.Max(desiredHeight, 120), maxHeight);

            // ajustar posición como antes
            dgvIvaTotals.Left = (pnlContent.ClientSize.Width - dgvIvaTotals.Width) / 2;
            dgvIvaTotals.Top = pnlContent.ClientSize.Height - contentPadding - dgvIvaTotals.Height;

            // lblTotales justo encima de la grilla de alícuatas, centrado horizontalmente
            int lblWidth = Math.Min(600, pnlContent.ClientSize.Width - 24);
            lblTotales.Width = lblWidth;
            lblTotales.Left = (pnlContent.ClientSize.Width - lblTotales.Width) / 2;
            lblTotales.Top = dgvIvaTotals.Top - 6 - lblTotales.Height;

            // ajustar altura de la grilla principal según la nueva posición (dejamos un poco más de separación)
            dgv.Height = Math.Max(60, lblTotales.Top - dgv.Top - 24);

            // -----------------------------
            // Posicionar botones de impresión a la derecha de la grilla de alícuotas
            // -----------------------------
            try
            {
                int gap = 8;
                int rightPadding = 12;

                // espacio a la derecha de la grilla
                int gridRight = dgvIvaTotals.Left + dgvIvaTotals.Width;
                int spaceRight = pnlContent.ClientSize.Width - rightPadding - gridRight;

                // intentamos colocar los botones a la derecha de la grilla (apilados verticalmente)
                int btnsTotalHeight = btnImprimir.Height + gap + btnImprimirMensual.Height + gap + btnImprimirMensualProveedor.Height;
                int startTop = dgvIvaTotals.Top + Math.Max(0, (dgvIvaTotals.Height - btnsTotalHeight) / 2);

                if (spaceRight >= Math.Max(Math.Max(btnImprimir.Width, btnImprimirMensual.Width), btnImprimirMensualProveedor.Width) + gap)
                {
                    // colocamos a la derecha, alineados al borde izquierdo del espacio disponible
                    int left = gridRight + gap;
                    // si no caben ambos botones en ancho, alineamos al borde derecho del panel
                    if (left + Math.Max(Math.Max(btnImprimirMensual.Width, btnImprimir.Width), btnImprimirMensualProveedor.Width) > pnlContent.ClientSize.Width - rightPadding)
                    {
                        left = pnlContent.ClientSize.Width - rightPadding - Math.Max(Math.Max(btnImprimirMensual.Width, btnImprimir.Width), btnImprimirMensualProveedor.Width);
                    }

                    btnImprimir.Left = left;
                    btnImprimirMensual.Left = left;
                    btnImprimirMensualProveedor.Left = left;
                    btnImprimir.Top = startTop;
                    btnImprimirMensual.Top = startTop + btnImprimir.Height + gap;
                    btnImprimirMensualProveedor.Top = startTop + btnImprimir.Height + gap + btnImprimirMensual.Height + gap;
                }
                else
                {
                    // fallback: si no hay suficiente espacio a la derecha, centramos los botones debajo de la grilla
                    int totalBtnsWidth = btnImprimir.Width + gap + btnImprimirMensual.Width + gap + btnImprimirMensualProveedor.Width;
                    int centerLeft = dgvIvaTotals.Left + Math.Max(0, (dgvIvaTotals.Width - totalBtnsWidth) / 2);
                    int btnTop = dgvIvaTotals.Bottom + gap;

                    btnImprimir.Left = Math.Max(12, centerLeft);
                    btnImprimirMensual.Left = btnImprimir.Left + btnImprimir.Width + gap;
                    btnImprimirMensualProveedor.Left = btnImprimirMensual.Left + btnImprimirMensual.Width + gap;
                    btnImprimir.Top = btnTop;
                    btnImprimirMensual.Top = btnTop;
                    btnImprimirMensualProveedor.Top = btnTop;
                }
            }
            catch
            {
                // no interrumpir layout por fallos en posicionamiento de botones
            }
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

            // guardar últimos valores para impresión
            lastDesde = desde;
            lastHasta = hasta;
            lastProveedorSeleccionado = cmbProveedor?.SelectedValue?.ToString() ?? "";

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
       cp.ImporteNeto, cp.ImporteIVA, cp.ImporteTotal, cp.Usuario, cp.Cajero
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

                    if (dgv.Columns["Cajero"] != null)
                    {
                        var colCajero = dgv.Columns["Cajero"];
                        colCajero.HeaderText = "Cajero";
                        colCajero.Width = 80;
                        colCajero.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        // Si quieres que sea la última columna:
                        colCajero.DisplayIndex = dgv.Columns.Count - 1;
                    }

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

                    // rellenar dgvIvaTotals desde dtAlicuatas
                    dgvIvaTotals.Rows.Clear();
                    foreach (DataRow r in dtAlicuotas.Rows)
                    {
                        var ali = r["Alicuota"] == DBNull.Value ? 0m : Convert.ToDecimal(r["Alicuota"]);
                        var baseSum = r["BaseSum"] == DBNull.Value ? 0m : Convert.ToDecimal(r["BaseSum"]);
                        var ivaSum = r["IvaSum"] == DBNull.Value ? 0m : Convert.ToDecimal(r["IvaSum"]);
                        dgvIvaTotals.Rows.Add(ali.ToString("N2"), baseSum.ToString("C2"), ivaSum.ToString("C2"));
                    }

                    // Guardar datos actuales para impresión / reuso
                    currentComprasDt = dt;
                    currentAlicuotasDt = dtAlicuotas;
                    lastDesde = desde;
                    lastHasta = hasta;
                    lastProveedorSeleccionado = proveedorSeleccionado;
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

        // Nuevo: abrir modal de pago para la compra seleccionada (similar a ProveedoresCtaCteForm)
        private async void BtnRegistrarPago_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una compra.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgv.SelectedRows[0];
            if (row.Cells["Id"].Value == null)
            {
                MessageBox.Show("La fila seleccionada no contiene una compra válida.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!int.TryParse(row.Cells["Id"].Value.ToString(), out int compraId))
            {
                MessageBox.Show("Id de compra inválido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string cs = GetConnectionString();
                decimal importeTotal = 0m;
                int? proveedorId = null;
                string proveedorNombre = "";

                using (var conn = new SqlConnection(cs))
                {
                    await conn.OpenAsync();

                    // Obtener importe total y proveedor
                    var sql = @"
SELECT cp.ImporteTotal, cp.ProveedorId, ISNULL(p.Nombre, cp.Proveedor) AS ProveedorNombre
FROM ComprasProveedores cp
LEFT JOIN Proveedores p ON cp.ProveedorId = p.Id
WHERE cp.Id = @id;
";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", compraId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                importeTotal = reader.IsDBNull(0) ? 0m : reader.GetDecimal(0);
                                proveedorId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
                                proveedorNombre = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            }
                            else
                            {
                                MessageBox.Show("Compra no encontrada en la base de datos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                    }

                    // Sumar pagos ya registrados para esta compra
                    var cmdSum = new SqlCommand("SELECT ISNULL(SUM(Monto),0) FROM ComprasProveedoresPagos WHERE CompraId = @id;", conn);
                    cmdSum.Parameters.AddWithValue("@id", compraId);
                    var obj = await cmdSum.ExecuteScalarAsync();
                    decimal totalPagado = obj == null ? 0m : Convert.ToDecimal(obj);

                    decimal saldo = importeTotal - totalPagado;
                    if (saldo <= 0m)
                    {
                        var resp = MessageBox.Show("La compra ya figura como saldada. ¿Desea registrar un pago igualmente?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (resp == DialogResult.No) return;
                    }

                    // Abrir modal de forma de pago
                    using (var frmPago = new FormaPagoProveedorForm(saldo, proveedorId, compraId, proveedorNombre))
                    {
                        var dr = frmPago.ShowDialog(this.FindForm());
                        if (dr != DialogResult.OK) return;
                        var pagos = frmPago.Pagos ?? new List<PagoInfo>();
                        if (pagos.Count == 0)
                        {
                            MessageBox.Show("No se registraron pagos.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        // Guardar pagos y actualizar CtaCte si corresponde
                        await GuardarPagosYActualizarCtaCteAsync(compraId, proveedorId, pagos, importeTotal, proveedorNombre);
                        // refrescar vista
                        await AplicarRangoYCargarAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error procesando pago: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Guarda pagos asociados a una compra y mantiene/crea/actualiza registro en ProveedoresCtaCte si queda saldo.
        private async Task GuardarPagosYActualizarCtaCteAsync(int compraId, int? proveedorId, List<PagoInfo> pagos, decimal importeTotal, string proveedorNombre)
        {
            string cs = GetConnectionString();
            try
            {
                using (var conn = new SqlConnection(cs))
                {
                    await conn.OpenAsync();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            decimal totalPagadoNuevo = 0m;
                            var insertPagoSql = @"
INSERT INTO ComprasProveedoresPagos
(CompraId, Metodo, Monto, Referencia, Fecha, Usuario)
VALUES (@CompraId, @Metodo, @Monto, @Referencia, @Fecha, @Usuario);";

                            foreach (var p in pagos)
                            {
                                using (var cmd = new SqlCommand(insertPagoSql, conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@CompraId", compraId);
                                    cmd.Parameters.AddWithValue("@Metodo", string.IsNullOrWhiteSpace(p.Metodo) ? (object)DBNull.Value : p.Metodo);
                                    cmd.Parameters.AddWithValue("@Monto", p.Monto);
                                    cmd.Parameters.AddWithValue("@Referencia", string.IsNullOrWhiteSpace(p.Referencia) ? (object)DBNull.Value : p.Referencia);
                                    cmd.Parameters.AddWithValue("@Fecha", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@Usuario", Environment.UserName);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                                totalPagadoNuevo += p.Monto;
                            }

                            // recalcular total pagado para la compra (incluye pagos previos)
                            var cmdSum = new SqlCommand("SELECT ISNULL(SUM(Monto),0) FROM ComprasProveedoresPagos WHERE CompraId = @id;", conn, tx);
                            cmdSum.Parameters.AddWithValue("@id", compraId);
                            var obj = await cmdSum.ExecuteScalarAsync();
                            decimal totalPagado = obj == null ? 0m : Convert.ToDecimal(obj);

                            decimal saldo = importeTotal - totalPagado;

                            // Si queda saldo, crear o actualizar ProveedoresCtaCte; si no, dejar saldo en 0
                            var selectCta = new SqlCommand("SELECT Id FROM ProveedoresCtaCte WHERE CompraId = @compraId;", conn, tx);
                            selectCta.Parameters.AddWithValue("@compraId", compraId);
                            var existing = await selectCta.ExecuteScalarAsync();

                            if (saldo > 0m)
                            {
                                if (existing != null)
                                {
                                    // actualizar
                                    var upd = new SqlCommand(@"UPDATE ProveedoresCtaCte SET MontoAdeudado = @montoAdeudado, Saldo = @saldo WHERE Id = @id;", conn, tx);
                                    upd.Parameters.AddWithValue("@montoAdeudado", saldo);
                                    upd.Parameters.AddWithValue("@saldo", saldo);
                                    upd.Parameters.AddWithValue("@id", Convert.ToInt32(existing));
                                    await upd.ExecuteNonQueryAsync();
                                }
                                else
                                {
                                    // insertar nuevo registro de CtaCte
                                    var ins = new SqlCommand(@"
INSERT INTO ProveedoresCtaCte
(ProveedorId, CompraId, Fecha, MontoTotal, MontoAdeudado, Saldo, Observaciones, Usuario)
VALUES (@ProveedorId, @CompraId, @Fecha, @MontoTotal, @MontoAdeudado, @Saldo, @Observaciones, @Usuario);", conn, tx);
                                    ins.Parameters.AddWithValue("@ProveedorId", proveedorId.HasValue ? (object)proveedorId.Value : DBNull.Value);
                                    ins.Parameters.AddWithValue("@CompraId", compraId);
                                    ins.Parameters.AddWithValue("@Fecha", DateTime.Now);
                                    ins.Parameters.AddWithValue("@MontoTotal", importeTotal);
                                    ins.Parameters.AddWithValue("@MontoAdeudado", saldo);
                                    ins.Parameters.AddWithValue("@Saldo", saldo);
                                    ins.Parameters.AddWithValue("@Observaciones", "Deuda generada por pagos parciales desde Control Compras");
                                    ins.Parameters.AddWithValue("@Usuario", Environment.UserName);
                                    await ins.ExecuteNonQueryAsync();
                                }

                                // marcar compra como CtaCte
                                var updCompra = new SqlCommand("UPDATE ComprasProveedores SET EsCtaCte = 1, NombreCtaCte = @nombre WHERE Id = @id;", conn, tx);
                                updCompra.Parameters.AddWithValue("@nombre", string.IsNullOrWhiteSpace(proveedorNombre) ? (object)DBNull.Value : proveedorNombre);
                                updCompra.Parameters.AddWithValue("@id", compraId);
                                await updCompra.ExecuteNonQueryAsync();
                            }
                            else
                            {
                                // saldo <= 0: si hay registro de ctacte, poner saldo 0; además marcar compra como no cta cte
                                if (existing != null)
                                {
                                    var upd = new SqlCommand(@"UPDATE ProveedoresCtaCte SET MontoAdeudado = 0, Saldo = 0 WHERE Id = @id;", conn, tx);
                                    upd.Parameters.AddWithValue("@id", Convert.ToInt32(existing));
                                    await upd.ExecuteNonQueryAsync();
                                }

                                var updCompra = new SqlCommand("UPDATE ComprasProveedores SET EsCtaCte = 0, NombreCtaCte = @nombre WHERE Id = @id;", conn, tx);
                                updCompra.Parameters.AddWithValue("@nombre", string.IsNullOrWhiteSpace(proveedorNombre) ? (object)DBNull.Value : proveedorNombre);
                                updCompra.Parameters.AddWithValue("@id", compraId);
                                await updCompra.ExecuteNonQueryAsync();
                            }

                            tx.Commit();
                            MessageBox.Show("Pago(s) registrado(s) correctamente.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            MessageBox.Show($"Error guardando pagos y actualizando CtaCte: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión al guardar pagos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        // -----------------------------
        // IMPRESIÓN: totales por día
        // -----------------------------

        // Obtiene desde la BD los totales por día y alícuota (agrupados por fecha)
        private async Task<DataTable> ObtenerTotalesPorDiaAsync(DateTime desde, DateTime hasta, string proveedorSeleccionado)
        {
            var dt = new DataTable();
            try
            {
                string cs = GetConnectionString();
                using (var conn = new SqlConnection(cs))
                {
                    var desdeFecha = desde.Date;
                    var hastaFecha = hasta.Date.AddDays(1).AddTicks(-1);

                    string sql = @"
SELECT CAST(cp.Fecha AS DATE) AS Fecha, d.Alicuota, SUM(d.BaseImponible) AS BaseSum, SUM(d.ImporteIva) AS IvaSum
FROM ComprasProveedoresIvaDetalle d
INNER JOIN ComprasProveedores cp ON d.CompraId = cp.Id
LEFT JOIN Proveedores p ON cp.ProveedorId = p.Id
WHERE cp.Fecha BETWEEN @desde AND @hasta
";
                    if (!string.IsNullOrEmpty(proveedorSeleccionado))
                    {
                        sql += " AND ISNULL(p.Nombre, cp.Proveedor) = @proveedorName\n";
                    }
                    sql += @"
GROUP BY CAST(cp.Fecha AS DATE), d.Alicuota
ORDER BY CAST(cp.Fecha AS DATE) DESC, d.Alicuota DESC;";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@desde", desdeFecha);
                        cmd.Parameters.AddWithValue("@hasta", hastaFecha);
                        if (!string.IsNullOrEmpty(proveedorSeleccionado))
                            cmd.Parameters.AddWithValue("@proveedorName", proveedorSeleccionado);
                        using (var da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));
                        }
                    }
                }
            }
            catch
            {
                // dejar dt vacío si falla
            }
            return dt;
        }

        // -----------------------------
        // IMPRESIÓN: totales por mes (resumen mensual simple)
        // -----------------------------
        private async Task<DataTable> ObtenerTotalesPorMesAsync(DateTime desde, DateTime hasta, string proveedorSeleccionado)
        {
            var dt = new DataTable();
            try
            {
                string cs = GetConnectionString();
                using (var conn = new SqlConnection(cs))
                {
                    var desdeFecha = desde.Date;
                    var hastaFecha = hasta.Date.AddDays(1).AddTicks(-1);

                    string sql = @"
SELECT YEAR(cp.Fecha) AS [Year], MONTH(cp.Fecha) AS [Month],
       SUM(d.BaseImponible) AS BaseSum, SUM(d.ImporteIva) AS IvaSum
FROM ComprasProveedoresIvaDetalle d
INNER JOIN ComprasProveedores cp ON d.CompraId = cp.Id
LEFT JOIN Proveedores p ON cp.ProveedorId = p.Id
WHERE cp.Fecha BETWEEN @desde AND @hasta
";
                    if (!string.IsNullOrEmpty(proveedorSeleccionado))
                    {
                        sql += " AND ISNULL(p.Nombre, cp.Proveedor) = @proveedorName\n";
                    }
                    sql += @"
GROUP BY YEAR(cp.Fecha), MONTH(cp.Fecha)
ORDER BY YEAR(cp.Fecha) DESC, MONTH(cp.Fecha) DESC;";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@desde", desdeFecha);
                        cmd.Parameters.AddWithValue("@hasta", hastaFecha);
                        if (!string.IsNullOrEmpty(proveedorSeleccionado))
                            cmd.Parameters.AddWithValue("@proveedorName", proveedorSeleccionado);
                        using (var da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));
                        }
                    }
                }
            }
            catch
            {
                // dejar dt vacío si falla
            }
            return dt;
        }

        // -----------------------------
        // IMPRESIÓN: totales por mes + desglose por alícuota
        // -----------------------------
        private async Task<DataTable> ObtenerTotalesPorMesYAlicuotaAsync(DateTime desde, DateTime hasta, string proveedorSeleccionado)
        {
            var dt = new DataTable();
            try
            {
                string cs = GetConnectionString();
                using (var conn = new SqlConnection(cs))
                {
                    var desdeFecha = desde.Date;
                    var hastaFecha = hasta.Date.AddDays(1).AddTicks(-1);

                    string sql = @"
SELECT YEAR(cp.Fecha) AS [Year], MONTH(cp.Fecha) AS [Month], d.Alicuota, 
       SUM(d.BaseImponible) AS BaseSum, SUM(d.ImporteIva) AS IvaSum
FROM ComprasProveedoresIvaDetalle d
INNER JOIN ComprasProveedores cp ON d.CompraId = cp.Id
LEFT JOIN Proveedores p ON cp.ProveedorId = p.Id
WHERE cp.Fecha BETWEEN @desde AND @hasta
";
                    if (!string.IsNullOrEmpty(proveedorSeleccionado))
                    {
                        sql += " AND ISNULL(p.Nombre, cp.Proveedor) = @proveedorName\n";
                    }
                    sql += @"
GROUP BY YEAR(cp.Fecha), MONTH(cp.Fecha), d.Alicuota
ORDER BY YEAR(cp.Fecha) DESC, MONTH(cp.Fecha) DESC, d.Alicuota DESC;";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@desde", desdeFecha);
                        cmd.Parameters.AddWithValue("@hasta", hastaFecha);
                        if (!string.IsNullOrEmpty(proveedorSeleccionado))
                            cmd.Parameters.AddWithValue("@proveedorName", proveedorSeleccionado);
                        using (var da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));
                        }
                    }
                }
            }
            catch
            {
                // dejar dt vacío si falla
            }
            return dt;
        }

        // -----------------------------
        // IMPRESIÓN: totales por mes por proveedor (desglose mensual por proveedor)
        // -----------------------------
        private async Task<DataTable> ObtenerTotalesPorMesPorProveedorAsync(DateTime desde, DateTime hasta, string proveedorSeleccionado)
        {
            var dt = new DataTable();
            try
            {
                string cs = GetConnectionString();
                using (var conn = new SqlConnection(cs))
                {
                    var desdeFecha = desde.Date;
                    var hastaFecha = hasta.Date.AddDays(1).AddTicks(-1);

                    string sql = @"
SELECT YEAR(cp.Fecha) AS [Year], MONTH(cp.Fecha) AS [Month], ISNULL(p.Nombre, cp.Proveedor) AS Proveedor, d.Alicuota,
       SUM(d.BaseImponible) AS BaseSum, SUM(d.ImporteIva) AS IvaSum
FROM ComprasProveedoresIvaDetalle d
INNER JOIN ComprasProveedores cp ON d.CompraId = cp.Id
LEFT JOIN Proveedores p ON cp.ProveedorId = p.Id
WHERE cp.Fecha BETWEEN @desde AND @hasta
";
                    if (!string.IsNullOrEmpty(proveedorSeleccionado))
                    {
                        sql += " AND ISNULL(p.Nombre, cp.Proveedor) = @proveedorName\n";
                    }
                    sql += @"
GROUP BY YEAR(cp.Fecha), MONTH(cp.Fecha), ISNULL(p.Nombre, cp.Proveedor), d.Alicuota
ORDER BY YEAR(cp.Fecha) DESC, MONTH(cp.Fecha) DESC, Proveedor ASC, d.Alicuota DESC;";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@desde", desdeFecha);
                        cmd.Parameters.AddWithValue("@hasta", hastaFecha);
                        if (!string.IsNullOrEmpty(proveedorSeleccionado))
                            cmd.Parameters.AddWithValue("@proveedorName", proveedorSeleccionado);
                        using (var da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));
                        }
                    }
                }
            }
            catch
            {
                // dejar dt vacío si falla
            }
            return dt;
        }

        // Evento del botón Imprimir: prepara datos y muestra vista previa (por día)
        private async Task BtnImprimir_Click(object sender, EventArgs e)
        {
            try
            {
                // usar últimos filtros aplicados
                var desde = lastDesde;
                var hasta = lastHasta;
                var proveedor = lastProveedorSeleccionado;

                var dtPorDia = await ObtenerTotalesPorDiaAsync(desde, hasta, proveedor);

                // preparar líneas de texto para imprimir
                printLines = new List<string>();
                printLines.Add("Control Compras Proveedores - Totales por día");
                printLines.Add($"Rango: {desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}");
                var provLabel = string.IsNullOrEmpty(proveedor) ? "Todos" : proveedor;
                printLines.Add($"Proveedor: {provLabel}");
                printLines.Add(new string('-', 80));
                printLines.Add(string.Format(CultureInfo.CurrentCulture, "{0,-12} {1,10} {2,15} {3,15}", "Fecha", "Alicuota", "Base", "IVA"));
                printLines.Add(new string('-', 80));

                // agrupar por fecha
                var rows = dtPorDia.AsEnumerable().Select(r => new
                {
                    Fecha = r.Field<DateTime>("Fecha"),
                    Alicuota = r.Field<decimal>("Alicuota"),
                    Base = r.Field<decimal>("BaseSum"),
                    Iva = r.Field<decimal>("IvaSum")
                }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Alicuota).ToList();

                DateTime? lastDate = null;
                decimal dayBaseSum = 0m, dayIvaSum = 0m;
                foreach (var r in rows)
                {
                    if (!lastDate.HasValue || r.Fecha.Date != lastDate.Value.Date)
                    {
                        if (lastDate.HasValue)
                        {
                            // imprimir totales del día anterior
                            printLines.Add(new string('-', 60));
                            printLines.Add(string.Format(CultureInfo.CurrentCulture, "{0,-12} {1,10} {2,15} {3,15}", "", "Total:", "", (dayBaseSum + dayIvaSum).ToString("C2")));
                            printLines.Add("");
                        }
                        lastDate = r.Fecha.Date;
                        dayBaseSum = 0m;
                        dayIvaSum = 0m;
                        printLines.Add(r.Fecha.ToString("dd/MM/yyyy"));
                    }

                    printLines.Add(string.Format(CultureInfo.CurrentCulture, "{0,-12} {1,10} {2,15} {3,15}",
                        "", // espacio para fecha ya impresa
                        r.Alicuota.ToString("N2"),
                        r.Base.ToString("C2"),
                        r.Iva.ToString("C2")));

                    dayBaseSum += r.Base;
                    dayIvaSum += r.Iva;
                }

                if (lastDate.HasValue)
                {
                    printLines.Add(new string('-', 60));
                    printLines.Add(string.Format(CultureInfo.CurrentCulture, "{0,-12} {1,10} {2,15} {3,15}", "", "Total del día:", dayBaseSum.ToString("C2"), dayIvaSum.ToString("C2")));
                }
                else
                {
                    printLines.Add("No hay datos para el rango seleccionado.");
                }

                // inicializar PrintDocument
                printDoc = new PrintDocument();
                printDoc.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);
                printDoc.PrintPage += PrintDoc_PrintPage;

                currentPrintLine = 0;

                using (var pp = new PrintPreviewDialog { Document = printDoc, Width = 900, Height = 700 })
                {
                    // ShowDialog puede lanzarlo como modal con el control padre
                    pp.ShowDialog(this.FindForm());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparando impresión: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Evento del botón ImprimirMensual: prepara datos y muestra vista previa (por mes, 1 total por mes
        // pero ahora incluyendo desglose por alícuotas)
        private async Task BtnImprimirMensual_Click(object sender, EventArgs e)
        {
            try
            {
                var desde = lastDesde;
                var hasta = lastHasta;
                var proveedor = lastProveedorSeleccionado;

                // obtenemos por mes+alícuota
                var dtPorMesAlicuota = await ObtenerTotalesPorMesYAlicuotaAsync(desde, hasta, proveedor);

                // preparar líneas de texto para imprimir
                printLines = new List<string>();
                printLines.Add("Control Compras Proveedores - Totales por mes (desglose por alícuota)");
                printLines.Add($"Rango: {desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}");
                var provLabel = string.IsNullOrEmpty(proveedor) ? "Todos" : proveedor;
                printLines.Add($"Proveedor: {provLabel}");
                printLines.Add(new string('-', 100));
                printLines.Add(string.Format(CultureInfo.CurrentCulture, "{0,-20} {1,10} {2,18} {3,18} {4,18}", "Mes", "Alicuota", "Base", "IVA", "Total"));
                printLines.Add(new string('-', 100));

                var rows = dtPorMesAlicuota.AsEnumerable().Select(r => new
                {
                    Year = r.Field<int>("Year"),
                    Month = r.Field<int>("Month"),
                    Alicuota = r.Field<decimal>("Alicuota"),
                    Base = r.Field<decimal>("BaseSum"),
                    Iva = r.Field<decimal>("IvaSum")
                }).OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).ThenByDescending(x => x.Alicuota).ToList();

                if (rows.Count == 0)
                {
                    printLines.Add("No hay datos para el rango seleccionado.");
                }
                else
                {
                    decimal grandBase = 0m, grandIva = 0m;
                    int curYear = -1, curMonth = -1;
                    decimal monthBase = 0m, monthIva = 0m;

                    foreach (var r in rows)
                    {
                        if (r.Year != curYear || r.Month != curMonth)
                        {
                            // cerrar mes anterior
                            if (curYear != -1)
                            {
                                printLines.Add(new string('-', 100));
                                // ahora colocamos "Total mes:" en la columna Mes (primera columna)
                                var monthTotal = monthBase + monthIva;
                                printLines.Add(string.Format(CultureInfo.CurrentCulture, "{0,-20} {1,10} {2,18} {3,18} {4,18}",
                                    "Total mes:", "", monthBase.ToString("C2"), monthIva.ToString("C2"), monthTotal.ToString("C2")));
                                printLines.Add("");
                                monthBase = 0m; monthIva = 0m;
                            }

                            // nuevo encabezado de mes
                            curYear = r.Year; curMonth = r.Month;
                            var dtMonth = new DateTime(curYear, curMonth, 1);
                            printLines.Add(dtMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture).ToUpperInvariant());
                        }

                        var lineTotal = (r.Base + r.Iva);
                        printLines.Add(string.Format(CultureInfo.CurrentCulture, "{0,-20} {1,10} {2,18} {3,18} {4,18}",
                            "", // espacio para mes ya impreso
                            r.Alicuota.ToString("N2"),
                            r.Base.ToString("C2"),
                            r.Iva.ToString("C2"),
                            lineTotal.ToString("C2")));

                        monthBase += r.Base;
                        monthIva += r.Iva;
                        grandBase += r.Base;
                        grandIva += r.Iva;
                    }

                    // totales último mes
                    printLines.Add(new string('-', 100));
                    var lastMonthTotal = monthBase + monthIva;
                    printLines.Add(string.Format(CultureInfo.CurrentCulture, "{0,-20} {1,10} {2,18} {3,18} {4,18}",
                        "Total mes:", "", monthBase.ToString("C2"), monthIva.ToString("C2"), lastMonthTotal.ToString("C2")));
                    printLines.Add(new string('-', 100));
                    printLines.Add(string.Format(CultureInfo.CurrentCulture, "{0,-20} {1,10} {2,18} {3,18} {4,18}",
                        "TOTAL GENERAL", "", grandBase.ToString("C2"), grandIva.ToString("C2"), (grandBase + grandIva).ToString("C2")));
                }

                // inicializar PrintDocument
                printDoc = new PrintDocument();
                printDoc.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);
                printDoc.PrintPage += PrintDoc_PrintPage;

                currentPrintLine = 0;

                using (var pp = new PrintPreviewDialog { Document = printDoc, Width = 900, Height = 700 })
                {
                    pp.ShowDialog(this.FindForm());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparando impresión mensual: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // -----------------------------
        // IMPRESIÓN: Totales por mes DETALLADO POR PROVEEDOR
        // -----------------------------
        private async Task BtnImprimirMensualPorProveedor_Click(object sender, EventArgs e)
        {
            try
            {
                var desde = lastDesde;
                var hasta = lastHasta;
                var proveedorFiltro = lastProveedorSeleccionado;

                var dt = await ObtenerTotalesPorMesPorProveedorAsync(desde, hasta, proveedorFiltro);

                // Column widths reducidos: Proveedor más angosto
                const int monthW = 20;
                const int provW = 18; // más angosto según tu petición
                const int aliW = 6;
                const int baseW = 15;
                const int ivaW = 15;
                const int totalW = 15;

                printLines = new List<string>();
                printLines.Add("Control Compras Proveedores - Totales por mes (detallado por proveedor y alícuota)");
                printLines.Add($"Rango: {desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}");
                var provLabel = string.IsNullOrEmpty(proveedorFiltro) ? "Todos" : proveedorFiltro;
                printLines.Add($"Proveedor (filtro): {provLabel}");
                printLines.Add(new string('-', monthW + provW + aliW + baseW + ivaW + totalW + 10));
                printLines.Add(string.Format(CultureInfo.CurrentCulture,
                    "{0,-" + monthW + "} {1,-" + provW + "} {2," + aliW + "} {3," + baseW + "} {4," + ivaW + "} {5," + totalW + "}",
                    "Mes", "Proveedor", "Ali%", "Base", "IVA", "Total"));
                printLines.Add(new string('-', monthW + provW + aliW + baseW + ivaW + totalW + 10));

                var rows = dt.AsEnumerable().Select(r => new
                {
                    Year = r.Field<int>("Year"),
                    Month = r.Field<int>("Month"),
                    Proveedor = r.Field<string>("Proveedor") ?? "(Sin proveedor)",
                    Alicuota = r.Field<decimal>("Alicuota"),
                    Base = r.Field<decimal>("BaseSum"),
                    Iva = r.Field<decimal>("IvaSum")
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ThenBy(x => x.Proveedor, StringComparer.CurrentCultureIgnoreCase)
                .ThenByDescending(x => x.Alicuota)
                .ToList();

                if (rows.Count == 0)
                {
                    printLines.Add("No hay datos para el rango seleccionado.");
                }
                else
                {
                    decimal grandBase = 0m, grandIva = 0m;
                    int curYear = -1, curMonth = -1;
                    string curProveedor = null;
                    decimal monthBase = 0m, monthIva = 0m;
                    decimal provBase = 0m, provIva = 0m;

                    // Totales por alícuota para el mes actual y global
                    var monthAliTotals = new Dictionary<decimal, (decimal Base, decimal Iva)>();
                    var grandAliTotals = new Dictionary<decimal, (decimal Base, decimal Iva)>();

                    foreach (var r in rows)
                    {
                        // cambio de mes
                        if (r.Year != curYear || r.Month != curMonth)
                        {
                            // cerrar proveedor anterior (del mes previo)
                            if (curProveedor != null)
                            {
                                var provTotal = provBase + provIva;
                                // subtotal proveedor: no repetir nombre, usar etiqueta
                                printLines.Add(string.Format(CultureInfo.CurrentCulture,
                                    "{0,-" + monthW + "} {1,-" + provW + "} {2," + aliW + "} {3," + baseW + "} {4," + ivaW + "} {5," + totalW + "}",
                                    "", "Total proveedor:", "", provBase.ToString("C2"), provIva.ToString("C2"), provTotal.ToString("C2")));
                                printLines.Add("");
                                provBase = 0m; provIva = 0m;
                                curProveedor = null;
                            }

                            // cerrar mes anterior
                            if (curYear != -1)
                            {
                                // imprimir desglose por alícuota del mes anterior (si existen)
                                if (monthAliTotals.Count > 0)
                                {
                                    printLines.Add("  Desglose alícuotas (mes):");
                                    foreach (var kv in monthAliTotals.OrderByDescending(k => k.Key))
                                    {
                                        var ali = kv.Key;
                                        var b = kv.Value.Base;
                                        var iv = kv.Value.Iva;
                                        var tot = b + iv;
                                        printLines.Add(string.Format(CultureInfo.CurrentCulture,
                                            "{0,-" + monthW + "} {1,-" + provW + "} {2," + aliW + "} {3," + baseW + "} {4," + ivaW + "} {5," + totalW + "}",
                                            "", "", ali.ToString("N2"), b.ToString("C2"), iv.ToString("C2"), tot.ToString("C2")));
                                    }
                                    printLines.Add("");
                                }

                                printLines.Add(new string('-', monthW + provW + aliW + baseW + ivaW + totalW + 10));
                                var monthTotal = monthBase + monthIva;
                                printLines.Add(string.Format(CultureInfo.CurrentCulture,
                                    "{0,-" + monthW + "} {1,-" + provW + "} {2," + aliW + "} {3," + baseW + "} {4," + ivaW + "} {5," + totalW + "}",
                                    "Total mes:", "", "", monthBase.ToString("C2"), monthIva.ToString("C2"), monthTotal.ToString("C2")));
                                printLines.Add("");
                                monthBase = 0m; monthIva = 0m;

                                // limpiar acumulador de alícuotas del mes
                                monthAliTotals.Clear();
                            }

                            // nuevo encabezado de mes
                            curYear = r.Year; curMonth = r.Month;
                            var dtMonth = new DateTime(curYear, curMonth, 1);
                            printLines.Add(dtMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture).ToUpperInvariant());
                        }

                        // cambio de proveedor dentro del mismo mes
                        if (!string.Equals(r.Proveedor, curProveedor, StringComparison.CurrentCultureIgnoreCase))
                        {
                            // cerrar proveedor anterior
                            if (curProveedor != null)
                            {
                                var provTotal = provBase + provIva;
                                printLines.Add(string.Format(CultureInfo.CurrentCulture,
                                    "{0,-" + monthW + "} {1,-" + provW + "} {2," + aliW + "} {3," + baseW + "} {4," + ivaW + "} {5," + totalW + "}",
                                    "", "Total proveedor:", "", provBase.ToString("C2"), provIva.ToString("C2"), provTotal.ToString("C2")));
                                printLines.Add("");
                                provBase = 0m; provIva = 0m;
                            }

                            // nuevo proveedor: imprimir una sola vez (encabezado de proveedor)
                            curProveedor = r.Proveedor;
                            printLines.Add(string.Format(CultureInfo.CurrentCulture,
                                "{0,-" + monthW + "} {1,-" + provW + "}", "", TruncateForPrint(curProveedor, provW)));
                        }

                        // línea por alícuota bajo el proveedor actual (no repetimos el proveedor)
                        var lineTotal = r.Base + r.Iva;
                        printLines.Add(string.Format(CultureInfo.CurrentCulture,
                            "{0,-" + monthW + "} {1,-" + provW + "} {2," + aliW + "} {3," + baseW + "} {4," + ivaW + "} {5," + totalW + "}",
                            "", "", r.Alicuota.ToString("N2"), r.Base.ToString("C2"), r.Iva.ToString("C2"), lineTotal.ToString("C2")));

                        // acumular subtotales
                        provBase += r.Base;
                        provIva += r.Iva;
                        monthBase += r.Base;
                        monthIva += r.Iva;
                        grandBase += r.Base;
                        grandIva += r.Iva;

                        // actualizar acumuladores de alícuota (mes y global)
                        if (!monthAliTotals.TryGetValue(r.Alicuota, out var mtuple))
                            monthAliTotals[r.Alicuota] = (r.Base, r.Iva);
                        else
                            monthAliTotals[r.Alicuota] = (mtuple.Base + r.Base, mtuple.Iva + r.Iva);

                        if (!grandAliTotals.TryGetValue(r.Alicuota, out var gtuple))
                            grandAliTotals[r.Alicuota] = (r.Base, r.Iva);
                        else
                            grandAliTotals[r.Alicuota] = (gtuple.Base + r.Base, gtuple.Iva + r.Iva);
                    }

                    // cerrar último proveedor del último mes
                    if (curProveedor != null)
                    {
                        var provTotal = provBase + provIva;
                        printLines.Add(string.Format(CultureInfo.CurrentCulture,
                            "{0,-" + monthW + "} {1,-" + provW + "} {2," + aliW + "} {3," + baseW + "} {4," + ivaW + "} {5," + totalW + "}",
                            "", "Total proveedor:", "", provBase.ToString("C2"), provIva.ToString("C2"), provTotal.ToString("C2")));
                        printLines.Add("");
                    }

                    // imprimir desglose por alícuota del último mes
                    if (monthAliTotals.Count > 0)
                    {
                        printLines.Add("  Desglose alícuotas (mes):");
                        foreach (var kv in monthAliTotals.OrderByDescending(k => k.Key))
                        {
                            var ali = kv.Key;
                            var b = kv.Value.Base;
                            var iv = kv.Value.Iva;
                            var tot = b + iv;
                            printLines.Add(string.Format(CultureInfo.CurrentCulture,
                                "{0,-" + monthW + "} {1,-" + provW + "} {2," + aliW + "} {3," + baseW + "} {4," + ivaW + "} {5," + totalW + "}",
                                "", "", ali.ToString("N2"), b.ToString("C2"), iv.ToString("C2"), tot.ToString("C2")));
                        }
                        printLines.Add("");
                    }

                    // totales último mes
                    printLines.Add(new string('-', monthW + provW + aliW + baseW + ivaW + totalW + 10));
                    var lastMonthTotal = monthBase + monthIva;
                    printLines.Add(string.Format(CultureInfo.CurrentCulture,
                        "{0,-" + monthW + "} {1,-" + provW + "} {2," + aliW + "} {3," + baseW + "} {4," + ivaW + "} {5," + totalW + "}",
                        "Total mes:", "", "", monthBase.ToString("C2"), monthIva.ToString("C2"), lastMonthTotal.ToString("C2")));
                    printLines.Add(new string('-', monthW + provW + aliW + baseW + ivaW + totalW + 10));

                    // imprimir desglose por alícuota GLOBAL antes del TOTAL GENERAL
                    if (grandAliTotals.Count > 0)
                    {
                        printLines.Add("  Desglose alícuotas (total general):");
                        foreach (var kv in grandAliTotals.OrderByDescending(k => k.Key))
                        {
                            var ali = kv.Key;
                            var b = kv.Value.Base;
                            var iv = kv.Value.Iva;
                            var tot = b + iv;
                            printLines.Add(string.Format(CultureInfo.CurrentCulture,
                                "{0,-" + monthW + "} {1,-" + provW + "} {2," + aliW + "} {3," + baseW + "} {4," + ivaW + "} {5," + totalW + "}",
                                "", "", ali.ToString("N2"), b.ToString("C2"), iv.ToString("C2"), tot.ToString("C2")));
                        }
                        printLines.Add("");
                    }

                    // TOTAL GENERAL
                    printLines.Add(string.Format(CultureInfo.CurrentCulture,
                        "{0,-" + monthW + "} {1,-" + provW + "} {2," + aliW + "} {3," + baseW + "} {4," + ivaW + "} {5," + totalW + "}",
                        "TOTAL GENERAL", "", "", grandBase.ToString("C2"), grandIva.ToString("C2"), (grandBase + grandIva).ToString("C2")));
                }

                // inicializar PrintDocument
                printDoc = new PrintDocument();
                printDoc.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);
                printDoc.PrintPage += PrintDoc_PrintPage;

                currentPrintLine = 0;

                using (var pp = new PrintPreviewDialog { Document = printDoc, Width = 900, Height = 700 })
                {
                    pp.ShowDialog(this.FindForm());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparando impresión mensual por proveedor: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper para recortar texto largo para impresión en columna fija
        private string TruncateForPrint(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 3) + "...";
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            float left = e.MarginBounds.Left;
            float top = e.MarginBounds.Top;
            float lineHeight = printFont.GetHeight(e.Graphics) + 2;
            int linesPerPage = (int)(e.MarginBounds.Height / lineHeight);

            int line = 0;
            while (currentPrintLine < printLines.Count && line < linesPerPage)
            {
                string text = printLines[currentPrintLine];
                e.Graphics.DrawString(text, printFont, Brushes.Black, left, top + line * lineHeight);
                currentPrintLine++;
                line++;
            }

            e.HasMorePages = currentPrintLine < printLines.Count;
            if (!e.HasMorePages)
            {
                // restablecer para próxima impresión
                currentPrintLine = 0;
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

            // <-- línea añadida -->
            this.WindowState = FormWindowState.Maximized;

            control = new ControlComprasProveedores { Dock = DockStyle.Fill };
            this.Controls.Add(control);
        }
    }
}