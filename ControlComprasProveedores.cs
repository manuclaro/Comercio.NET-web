using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        // padding usado en todo el control (antes era variable local)
        private readonly int contentPadding = 12;

        public ControlComprasProveedores()
        {
            InitializeComponent();
            // carga inicial: hoy
            this.Load += async (s, e) => await AplicarRangoYCargarAsync();
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
            cmbRango = new ComboBox
            {
                Left = 12,
                Top = 12,
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbRango.Items.AddRange(new object[] { "Hoy", "Esta semana", "Este mes", "Rango personalizado" });
            cmbRango.SelectedIndex = 0;
            cmbRango.SelectedIndexChanged += CmbRango_SelectedIndexChanged;

            dtpDesde = new DateTimePicker { Left = cmbRango.Right + 8, Top = 12, Width = 120, Format = DateTimePickerFormat.Short, Visible = false };
            dtpHasta = new DateTimePicker { Left = dtpDesde.Right + 8, Top = 12, Width = 120, Format = DateTimePickerFormat.Short, Visible = false };

            btnRefrescar = new Button
            {
                Left = dtpHasta.Right + 12,
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
                Left = btnRefrescar.Right + 8,
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
                Width = pnlContent.Width - 24,
                Height = 22,
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = "Totales: Neto $0.00 | IVA $0.00 | Total $0.00"
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

            pnlContent.Controls.AddRange(new Control[] { cmbRango, dtpDesde, dtpHasta, btnRefrescar, btnNuevo, dgv, lblTotales, dgvIvaTotals });

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

                // Ajustar altura de la grilla principal para ocupar el espacio restante
                dgv.Left = 12;
                dgv.Top = cmbRango.Bottom + 12;
                dgv.Width = pnlContent.ClientSize.Width - 24;

                // lblTotales y posicionamiento de la grilla de alícuotas se calcularán dentro AjustarIvaTotalsSize
                AjustarIvaTotalsSize();

                // colocar botones a la derecha si cabe
                var rightStart = pnlContent.ClientSize.Width - 12 - btnNuevo.Width;
                btnNuevo.Left = Math.Max(btnNuevo.Left, rightStart);
                btnRefrescar.Left = Math.Max(12 + cmbRango.Width + 8, btnNuevo.Left - 8 - btnRefrescar.Width);
                dtpDesde.Left = Math.Min(dtpDesde.Left, Math.Max(12 + cmbRango.Width + 8, pnlContent.ClientSize.Width - 360));
                dtpHasta.Left = dtpDesde.Right + 8;
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

            // lblTotales justo encima de la grilla de alícuotas
            lblTotales.Top = dgvIvaTotals.Top - 6 - lblTotales.Height;
            lblTotales.Width = pnlContent.ClientSize.Width - 24;

            // ajustar altura de la grilla principal según la nueva posición
            dgv.Height = Math.Max(80, lblTotales.Top - dgv.Top - 12);
        }

        private async void CmbRango_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool personalizado = cmbRango.SelectedItem?.ToString() == "Rango personalizado";
            dtpDesde.Visible = dtpHasta.Visible = personalizado;
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

                    var sql = @"
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

                    // también obtener totales por alícuota agrupados
                    var dtAlicuotas = new DataTable();
                    var sqlAlicuotas = @"
SELECT d.Alicuota, SUM(d.BaseImponible) AS BaseSum, SUM(d.ImporteIva) AS IvaSum
FROM ComprasProveedoresIvaDetalle d
INNER JOIN ComprasProveedores cp ON d.CompraId = cp.Id
WHERE cp.Fecha BETWEEN @desde AND @hasta
GROUP BY d.Alicuota
ORDER BY d.Alicuota DESC;";
                    using (var cmdA = new SqlCommand(sqlAlicuotas, conn))
                    {
                        cmdA.Parameters.AddWithValue("@desde", desdeFecha);
                        cmdA.Parameters.AddWithValue("@hasta", hastaFecha);
                        using (var da2 = new SqlDataAdapter(cmdA))
                        {
                            await Task.Run(() => da2.Fill(dtAlicuotas));
                        }
                    }

                    // asignar valores a la UI (fuera del using de conexión)
                    dgv.DataSource = dt;

                    FormatearGrilla();

                    // Calcular totales
                    decimal sumaNeto = 0m, sumaIva = 0m, sumaTotal = 0m;
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["ImporteNeto"] != DBNull.Value && decimal.TryParse(row["ImporteNeto"].ToString(), out decimal n)) sumaNeto += n;
                        if (row["ImporteIVA"] != DBNull.Value && decimal.TryParse(row["ImporteIVA"].ToString(), out decimal v)) sumaIva += v;
                        if (row["ImporteTotal"] != DBNull.Value && decimal.TryParse(row["ImporteTotal"].ToString(), out decimal t)) sumaTotal += t;
                    }

                    lblTotales.Text = $"Totales ({desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}): Neto {sumaNeto:C2} | IVA {sumaIva:C2} | Total {sumaTotal:C2}";

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
                this.ClientSize = new Size(320, 260);
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
                this.Controls.Add(dgvDet);

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
            this.ClientSize = new Size(900, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.BackColor = Color.FromArgb(250, 250, 250);

            control = new ControlComprasProveedores { Dock = DockStyle.Fill };
            this.Controls.Add(control);
        }
    }
}