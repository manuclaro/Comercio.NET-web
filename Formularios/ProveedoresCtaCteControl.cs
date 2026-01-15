using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using System.Collections.Generic;
using Comercio.NET.Services;

namespace Comercio.NET.Formularios
{
    public partial class ProveedoresCtaCteControl : UserControl
    {
        private Panel pnlHeader;
        private Panel pnlContent;
        private DataGridView dgvResumen;
        private Button btnRefrescar;
        private Button btnPagoGeneral;
        private Button btnExportar;
        private Label lblTotalDeuda;
        private ComboBox cmbFiltroProveedor;
        private Label lblFiltroProveedor;

        private readonly int contentPadding = 12;

        private bool isLoadingData = false;
        private bool isInitialized = false;

        public ProveedoresCtaCteControl()
        {
            InitializeComponent();
            this.Load += ProveedoresCtaCteControl_Load;
        }

        private void ProveedoresCtaCteControl_Load(object sender, EventArgs e)
        {
            if (!isInitialized)
            {
                isInitialized = true;

                this.BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        await CargarResumenAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al cargar datos iniciales: {ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }));
            }
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(250, 250, 250);

            var headerHeight = 64;
            pnlHeader = new Panel
            {
                Left = 0,
                Top = 0,
                Width = this.ClientSize.Width,
                Height = headerHeight,
                BackColor = Color.FromArgb(156, 39, 176)
            };
            pnlHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var lblIcon = new Label
            {
                Text = "💰",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 28F, FontStyle.Bold, GraphicsUnit.Point),
                Left = 12,
                Top = 8,
                AutoSize = true
            };

            var lblTitle = new Label
            {
                Text = "Cuenta Corriente Proveedores",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Left = lblIcon.Right + 8,
                Top = 12,
                AutoSize = true
            };

            var lblSubtitle = new Label
            {
                Text = "Resumen de deudas y pagos pendientes por proveedor",
                ForeColor = Color.FromArgb(230, 230, 255),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Left = lblIcon.Right + 8,
                Top = lblTitle.Bottom - 4,
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblIcon);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

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

            lblFiltroProveedor = new Label
            {
                Left = 12,
                Top = 14,
                AutoSize = true,
                Text = "Proveedor:",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.Black
            };

            cmbFiltroProveedor = new ComboBox
            {
                Left = lblFiltroProveedor.Right + 6,
                Top = 12,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFiltroProveedor.SelectedIndexChanged += CmbFiltroProveedor_SelectedIndexChanged;

            btnRefrescar = new Button
            {
                Top = 10,
                Width = 100,
                Height = 32,
                Text = "Refrescar",
                BackColor = Color.FromArgb(96, 125, 139),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnRefrescar.FlatAppearance.BorderSize = 0;
            btnRefrescar.Click += async (s, e) => await RefrescarSafeAsync();

            btnPagoGeneral = new Button
            {
                Top = 10,
                Width = 120,
                Height = 32,
                Text = "Pago General",
                BackColor = Color.FromArgb(76, 175, 80),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnPagoGeneral.FlatAppearance.BorderSize = 0;
            btnPagoGeneral.Click += BtnPagoGeneral_Click;

            btnExportar = new Button
            {
                Top = 10,
                Width = 100,
                Height = 32,
                Text = "Exportar",
                BackColor = Color.FromArgb(255, 152, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnExportar.FlatAppearance.BorderSize = 0;
            btnExportar.Click += BtnExportar_Click;

            dgvResumen = new DataGridView
            {
                Left = 12,
                Top = cmbFiltroProveedor.Bottom + 12,
                Width = pnlContent.Width - 24,
                Height = pnlContent.Height - 150,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false
            };
            dgvResumen.DoubleClick += DgvResumen_DoubleClick;

            lblTotalDeuda = new Label
            {
                Left = 12,
                Top = dgvResumen.Bottom + 12,
                Width = pnlContent.Width - 24,
                Height = 30,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Text = "Deuda Total: $0.00",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(211, 47, 47)
            };

            pnlContent.Controls.AddRange(new Control[]
            {
                lblFiltroProveedor, cmbFiltroProveedor,
                btnRefrescar, btnPagoGeneral, btnExportar,
                dgvResumen, lblTotalDeuda
            });

            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlContent);

            this.Resize += (s, e) =>
            {
                pnlHeader.Width = this.ClientSize.Width;
                pnlContent.Left = contentPadding;
                pnlContent.Top = pnlHeader.Bottom + contentPadding;
                pnlContent.Width = this.ClientSize.Width - contentPadding * 2;
                pnlContent.Height = Math.Max(220, this.ClientSize.Height - pnlHeader.Height - contentPadding * 2);

                int rightPadding = 12;
                int gap = 8;
                btnExportar.Left = pnlContent.ClientSize.Width - rightPadding - btnExportar.Width;
                btnPagoGeneral.Left = btnExportar.Left - gap - btnPagoGeneral.Width;
                btnRefrescar.Left = btnPagoGeneral.Left - gap - btnRefrescar.Width;

                dgvResumen.Width = pnlContent.ClientSize.Width - 24;
                lblTotalDeuda.Width = pnlContent.ClientSize.Width - 24;
            };
        }

        private async Task RefrescarSafeAsync()
        {
            if (isLoadingData) return;
            await CargarResumenAsync();
        }

        private async void CmbFiltroProveedor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLoadingData) return;
            if (!isInitialized) return;

            await CargarResumenAsync();
        }

        private string GetConnectionString()
        {
            var cfg = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return cfg.GetConnectionString("DefaultConnection");
        }

        private async Task CargarResumenAsync()
        {
            if (isLoadingData) return;

            isLoadingData = true;

            try
            {
                string cs = GetConnectionString();
                var dtResumen = new DataTable();
                var dtProveedores = new DataTable();

                using (var conn = new SqlConnection(cs))
                {
                    await conn.OpenAsync();

                    var sqlProveedores = @"
                        SELECT DISTINCT 
                            COALESCE(p.Id, 0) AS Id,
                            COALESCE(p.Nombre, cp.Proveedor, '(Sin proveedor)') AS Nombre
                        FROM ProveedoresCtaCte cta
                        LEFT JOIN Proveedores p ON cta.ProveedorId = p.Id
                        LEFT JOIN ComprasProveedores cp ON cta.CompraId = cp.Id
                        WHERE cta.Saldo > 0
                        ORDER BY Nombre;
                    ";

                    using (var cmdProv = new SqlCommand(sqlProveedores, conn))
                    using (var daProv = new SqlDataAdapter(cmdProv))
                    {
                        await Task.Run(() => daProv.Fill(dtProveedores));
                    }

                    var rowTodos = dtProveedores.NewRow();
                    rowTodos["Id"] = 0;
                    rowTodos["Nombre"] = "Todos";
                    dtProveedores.Rows.InsertAt(rowTodos, 0);

                    var proveedorAnterior = cmbFiltroProveedor.SelectedValue;

                    cmbFiltroProveedor.SelectedIndexChanged -= CmbFiltroProveedor_SelectedIndexChanged;
                    cmbFiltroProveedor.DataSource = dtProveedores;
                    cmbFiltroProveedor.DisplayMember = "Nombre";
                    cmbFiltroProveedor.ValueMember = "Id";

                    if (proveedorAnterior != null)
                        cmbFiltroProveedor.SelectedValue = proveedorAnterior;
                    else
                        cmbFiltroProveedor.SelectedIndex = 0;

                    cmbFiltroProveedor.SelectedIndexChanged += CmbFiltroProveedor_SelectedIndexChanged;

                    var proveedorIdFiltro = cmbFiltroProveedor.SelectedValue != null
                        ? Convert.ToInt32(cmbFiltroProveedor.SelectedValue)
                        : 0;

                    var sqlResumen = @"
                        SELECT 
                            COALESCE(p.Id, 0) AS ProveedorId,
                            COALESCE(p.Nombre, cp.Proveedor, '(Sin proveedor)') AS Proveedor,
                            SUM(cta.Saldo) AS TotalAdeudado,
                            COUNT(DISTINCT cta.Id) AS FacturasPendientes,
                            MAX(cta.Fecha) AS UltimaCompra
                        FROM ProveedoresCtaCte cta
                        LEFT JOIN Proveedores p ON cta.ProveedorId = p.Id
                        LEFT JOIN ComprasProveedores cp ON cta.CompraId = cp.Id
                        WHERE cta.Saldo > 0
                    ";

                    if (proveedorIdFiltro > 0)
                    {
                        sqlResumen += " AND COALESCE(p.Id, 0) = @proveedorId";
                    }

                    sqlResumen += @"
                        GROUP BY COALESCE(p.Id, 0), COALESCE(p.Nombre, cp.Proveedor, '(Sin proveedor)')
                        ORDER BY TotalAdeudado DESC;
                    ";

                    using (var cmd = new SqlCommand(sqlResumen, conn))
                    {
                        if (proveedorIdFiltro > 0)
                            cmd.Parameters.AddWithValue("@proveedorId", proveedorIdFiltro);

                        using (var da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dtResumen));
                        }
                    }
                }

                dgvResumen.DataSource = dtResumen;
                FormatearGrilla();

                decimal totalDeuda = 0m;
                foreach (DataRow row in dtResumen.Rows)
                {
                    if (row["TotalAdeudado"] != DBNull.Value)
                        totalDeuda += Convert.ToDecimal(row["TotalAdeudado"]);
                }

                lblTotalDeuda.Text = $"Deuda Total: {totalDeuda:C2}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando resumen: {ex.Message}\n\nStack Trace: {ex.StackTrace}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isLoadingData = false;
            }
        }

        private void FormatearGrilla()
        {
            if (dgvResumen.Columns.Contains("ProveedorId"))
                dgvResumen.Columns["ProveedorId"].Visible = false;

            if (dgvResumen.Columns.Contains("Proveedor"))
            {
                dgvResumen.Columns["Proveedor"].HeaderText = "Proveedor";
                dgvResumen.Columns["Proveedor"].Width = 200;
            }

            if (dgvResumen.Columns.Contains("TotalAdeudado"))
            {
                dgvResumen.Columns["TotalAdeudado"].HeaderText = "Total Adeudado";
                dgvResumen.Columns["TotalAdeudado"].DefaultCellStyle.Format = "C2";
                dgvResumen.Columns["TotalAdeudado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvResumen.Columns["TotalAdeudado"].Width = 150;
                dgvResumen.Columns["TotalAdeudado"].DefaultCellStyle.ForeColor = Color.FromArgb(211, 47, 47);
                dgvResumen.Columns["TotalAdeudado"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }

            if (dgvResumen.Columns.Contains("FacturasPendientes"))
            {
                dgvResumen.Columns["FacturasPendientes"].HeaderText = "Facturas Pendientes";
                dgvResumen.Columns["FacturasPendientes"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvResumen.Columns["FacturasPendientes"].Width = 120;
            }

            if (dgvResumen.Columns.Contains("UltimaCompra"))
            {
                dgvResumen.Columns["UltimaCompra"].HeaderText = "Última Compra";
                dgvResumen.Columns["UltimaCompra"].DefaultCellStyle.Format = "dd/MM/yyyy";
                dgvResumen.Columns["UltimaCompra"].Width = 120;
            }
        }

        private void DgvResumen_DoubleClick(object sender, EventArgs e)
        {
            if (dgvResumen.SelectedRows.Count == 0) return;

            var row = dgvResumen.SelectedRows[0];
            if (row.Cells["ProveedorId"].Value == null) return;

            int proveedorId = Convert.ToInt32(row.Cells["ProveedorId"].Value);
            string proveedorNombre = row.Cells["Proveedor"].Value?.ToString() ?? "";

            using (var detalle = new DetalleCtaCteProveedorForm(proveedorId, proveedorNombre))
            {
                detalle.StartPosition = FormStartPosition.CenterParent;
                var resultado = detalle.ShowDialog(this.FindForm());

                if (resultado == DialogResult.OK)
                {
                    _ = CargarResumenAsync();
                }
            }
        }

        private async void BtnPagoGeneral_Click(object sender, EventArgs e)
        {
            if (dgvResumen.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un proveedor.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgvResumen.SelectedRows[0];
            int proveedorId = Convert.ToInt32(row.Cells["ProveedorId"].Value);
            string proveedorNombre = row.Cells["Proveedor"].Value?.ToString() ?? "";
            decimal totalAdeudado = Convert.ToDecimal(row.Cells["TotalAdeudado"].Value);

            using (var frmPago = new PagoGeneralProveedorForm(proveedorId, proveedorNombre, totalAdeudado))
            {
                frmPago.StartPosition = FormStartPosition.CenterParent;
                var resultado = frmPago.ShowDialog(this.FindForm());

                if (resultado == DialogResult.OK)
                {
                    await CargarResumenAsync();
                }
            }
        }

        private void BtnExportar_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvResumen.Rows.Count == 0)
                {
                    MessageBox.Show("No hay datos para exportar.", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (var sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Archivo CSV|*.csv";
                    sfd.FileName = $"CuentaCorriente_Proveedores_{DateTime.Now:yyyyMMdd}.csv";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        var lines = new List<string>();

                        lines.Add("Proveedor,Total Adeudado,Facturas Pendientes,Última Compra");

                        foreach (DataGridViewRow row in dgvResumen.Rows)
                        {
                            if (row.IsNewRow) continue;

                            var proveedor = row.Cells["Proveedor"].Value?.ToString() ?? "";
                            var total = row.Cells["TotalAdeudado"].Value?.ToString() ?? "0";
                            var facturas = row.Cells["FacturasPendientes"].Value?.ToString() ?? "0";
                            var fecha = row.Cells["UltimaCompra"].Value != null
                                ? Convert.ToDateTime(row.Cells["UltimaCompra"].Value).ToString("dd/MM/yyyy")
                                : "";

                            lines.Add($"\"{proveedor}\",{total},{facturas},{fecha}");
                        }

                        System.IO.File.WriteAllLines(sfd.FileName, lines);
                        MessageBox.Show($"Exportado exitosamente a:\n{sfd.FileName}", "Éxito",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exportando: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class DetalleCtaCteProveedorForm : Form
    {
        private DataGridView dgvDetalle;
        private Label lblProveedor;
        private Label lblTotal;
        private Button btnPagar;
        private Button btnHistorial;
        private Button btnCerrar;

        private readonly int proveedorId;
        private readonly string proveedorNombre;

        public DetalleCtaCteProveedorForm(int proveedorId, string proveedorNombre)
        {
            this.proveedorId = proveedorId;
            this.proveedorNombre = proveedorNombre;
            InitializeComponent();
            this.Load += async (s, e) => await CargarDetalleAsync();
        }

        private void InitializeComponent()
        {
            this.Text = $"Detalle Cuenta Corriente - {proveedorNombre}";
            this.ClientSize = new Size(900, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimizeBox = false;
            this.MaximizeBox = true;
            this.Font = new Font("Segoe UI", 9F);

            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(156, 39, 176)
            };

            lblProveedor = new Label
            {
                Text = proveedorNombre,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Left = 12,
                Top = 12,
                AutoSize = true
            };

            lblTotal = new Label
            {
                Text = "Saldo Total: $0.00",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                Left = 12,
                Top = lblProveedor.Bottom + 4,
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblProveedor);
            pnlHeader.Controls.Add(lblTotal);

            dgvDetalle = new DataGridView
            {
                Left = 12,
                Top = pnlHeader.Bottom + 12,
                Width = this.ClientSize.Width - 24,
                Height = this.ClientSize.Height - pnlHeader.Height - 80,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false
            };
            dgvDetalle.DoubleClick += DgvDetalle_DoubleClick;

            var pnlBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(12)
            };

            btnCerrar = new Button
            {
                Text = "Cerrar",
                Width = 100,
                Height = 32,
                BackColor = Color.FromArgb(96, 125, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            btnHistorial = new Button
            {
                Text = "Ver Historial",
                Width = 120,
                Height = 32,
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right
            };
            btnHistorial.FlatAppearance.BorderSize = 0;
            btnHistorial.Click += BtnHistorial_Click;

            btnPagar = new Button
            {
                Text = "Pagar Factura",
                Width = 120,
                Height = 32,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right
            };
            btnPagar.FlatAppearance.BorderSize = 0;
            btnPagar.Click += async (s, e) => await BtnPagar_Click(s, e);

            pnlBotones.Controls.Add(btnCerrar);
            pnlBotones.Controls.Add(btnHistorial);
            pnlBotones.Controls.Add(btnPagar);

            this.Controls.Add(dgvDetalle);
            this.Controls.Add(pnlBotones);
            this.Controls.Add(pnlHeader);
        }

        private string GetConnectionString()
        {
            var cfg = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return cfg.GetConnectionString("DefaultConnection");
        }

        private async Task CargarDetalleAsync()
        {
            try
            {
                string cs = GetConnectionString();
                var dt = new DataTable();

                using (var conn = new SqlConnection(cs))
                {
                    // ✅ CORREGIDO: Query actualizada para coincidir con la estructura real de PagosProveedores
                    var sql = @"
                SELECT 
                    cta.Id,
                    cta.CompraId,
                    COALESCE(cp.NumeroFactura, 'Sin Nro.') AS NumeroFactura,
                    cta.Fecha,
                    cta.MontoTotal,
                    ISNULL((
                        SELECT SUM(pp.Monto)
                        FROM PagosProveedores pp
                        WHERE pp.IdProveedor = @proveedorId
                            AND (
                                pp.CompraId = cta.CompraId 
                                OR pp.Observaciones LIKE '%Compra #' + CAST(cta.CompraId AS VARCHAR) + '%'
                            )
                    ), 0) AS Pagado,
                    cta.Saldo,
                    cta.Observaciones
                FROM ProveedoresCtaCte cta
                LEFT JOIN ComprasProveedores cp ON cta.CompraId = cp.Id
                WHERE cta.Saldo > 0
                    AND (cta.ProveedorId = @proveedorId OR (@proveedorId = 0 AND cta.ProveedorId IS NULL))
                ORDER BY cta.Fecha DESC";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@proveedorId", proveedorId);

                        using (var da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));
                        }
                    }
                }

                dgvDetalle.DataSource = dt;
                FormatearGrilla();

                decimal totalSaldo = 0m;
                foreach (DataRow row in dt.Rows)
                {
                    if (row["Saldo"] != DBNull.Value)
                        totalSaldo += Convert.ToDecimal(row["Saldo"]);
                }

                lblTotal.Text = $"Saldo Total: {totalSaldo:C2}";

                // ✅ NUEVO: Debug para verificar los valores
                System.Diagnostics.Debug.WriteLine($"📊 DETALLE CARGADO - Proveedor: {proveedorNombre}");
                foreach (DataRow row in dt.Rows)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"   Factura: {row["NumeroFactura"]} | " +
                        $"Total: {row["MontoTotal"]:C2} | " +
                        $"Pagado: {row["Pagado"]:C2} | " +
                        $"Saldo: {row["Saldo"]:C2}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando detalle: {ex.Message}\n\n{ex.StackTrace}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatearGrilla()
        {
            if (dgvDetalle.Columns.Contains("Id"))
                dgvDetalle.Columns["Id"].Visible = false;

            if (dgvDetalle.Columns.Contains("CompraId"))
                dgvDetalle.Columns["CompraId"].Visible = false;

            if (dgvDetalle.Columns.Contains("NumeroFactura"))
            {
                dgvDetalle.Columns["NumeroFactura"].HeaderText = "Nro. Factura";
                dgvDetalle.Columns["NumeroFactura"].Width = 120;
            }

            if (dgvDetalle.Columns.Contains("Fecha"))
            {
                dgvDetalle.Columns["Fecha"].HeaderText = "Fecha";
                dgvDetalle.Columns["Fecha"].DefaultCellStyle.Format = "dd/MM/yyyy";
                dgvDetalle.Columns["Fecha"].Width = 100;
            }

            if (dgvDetalle.Columns.Contains("MontoTotal"))
            {
                dgvDetalle.Columns["MontoTotal"].HeaderText = "Total";
                dgvDetalle.Columns["MontoTotal"].DefaultCellStyle.Format = "C2";
                dgvDetalle.Columns["MontoTotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvDetalle.Columns["MontoTotal"].Width = 120;
            }

            if (dgvDetalle.Columns.Contains("Pagado"))
            {
                dgvDetalle.Columns["Pagado"].HeaderText = "Pagado";
                dgvDetalle.Columns["Pagado"].DefaultCellStyle.Format = "C2";
                dgvDetalle.Columns["Pagado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvDetalle.Columns["Pagado"].Width = 120;
                dgvDetalle.Columns["Pagado"].DefaultCellStyle.ForeColor = Color.FromArgb(76, 175, 80);
            }

            if (dgvDetalle.Columns.Contains("Saldo"))
            {
                dgvDetalle.Columns["Saldo"].HeaderText = "Saldo Pendiente";
                dgvDetalle.Columns["Saldo"].DefaultCellStyle.Format = "C2";
                dgvDetalle.Columns["Saldo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvDetalle.Columns["Saldo"].Width = 120;
                dgvDetalle.Columns["Saldo"].DefaultCellStyle.ForeColor = Color.FromArgb(211, 47, 47);
                dgvDetalle.Columns["Saldo"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }

            if (dgvDetalle.Columns.Contains("Observaciones"))
            {
                dgvDetalle.Columns["Observaciones"].HeaderText = "Observaciones";
                dgvDetalle.Columns["Observaciones"].Width = 200;
            }
        }

        private void DgvDetalle_DoubleClick(object sender, EventArgs e)
        {
            // ✅ MODIFICADO: Abrir historial en lugar de iniciar pago
            using (var historial = new HistorialPagosProveedorForm(proveedorId, proveedorNombre))
            {
                historial.StartPosition = FormStartPosition.CenterParent;
                historial.ShowDialog(this);
            }
        }

        private async Task BtnPagar_Click(object sender, EventArgs e)
        {
            if (dgvDetalle.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una factura.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgvDetalle.SelectedRows[0];
            int ctaCteId = Convert.ToInt32(row.Cells["Id"].Value);
            int? compraId = row.Cells["CompraId"].Value == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(row.Cells["CompraId"].Value);
            decimal saldo = Convert.ToDecimal(row.Cells["Saldo"].Value);

            using (var frmPago = new FormaPagoProveedorForm(saldo, proveedorId == 0 ? (int?)null : proveedorId, compraId, proveedorNombre))
            {
                frmPago.StartPosition = FormStartPosition.CenterParent;
                var resultado = frmPago.ShowDialog(this);

                if (resultado == DialogResult.OK)
                {
                    var pagos = frmPago.Pagos ?? new List<PagoInfo>();
                    if (pagos.Count > 0)
                    {
                        await GuardarPagosAsync(ctaCteId, compraId, pagos, saldo);
                        await CargarDetalleAsync();
                        this.DialogResult = DialogResult.OK;
                    }
                }
            }
        }

        // ✅ CORREGIDO: Usar tabla PagoProveedores
        private async Task GuardarPagosAsync(int ctaCteId, int? compraId, List<PagoInfo> pagos, decimal saldoActual)
        {
            string cs = GetConnectionString();
            string usuario = AuthenticationService.SesionActual?.Usuario?.NombreUsuario ?? Environment.UserName;

            try
            {
                using (var conn = new SqlConnection(cs))
                {
                    await conn.OpenAsync();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            decimal totalPagado = 0m;

                            // ✅ CORREGIDO: Insertar en PagoProveedores
                            foreach (var p in pagos)
                            {
                                var insertSql = @"
                                    INSERT INTO PagosProveedores
                                    (CompraId, CtaCteId, MedioPago, Monto, Referencia, FechaPago, Usuario)
                                    VALUES (@CompraId, @CtaCteId, @MedioPago, @Monto, @Referencia, @FechaPago, @Usuario);
                                ";

                                using (var cmd = new SqlCommand(insertSql, conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@CompraId", compraId.HasValue ? (object)compraId.Value : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CtaCteId", ctaCteId);
                                    cmd.Parameters.AddWithValue("@MedioPago", string.IsNullOrWhiteSpace(p.Metodo) ? (object)DBNull.Value : p.Metodo);
                                    cmd.Parameters.AddWithValue("@Monto", p.Monto);
                                    cmd.Parameters.AddWithValue("@Referencia", string.IsNullOrWhiteSpace(p.Referencia) ? (object)DBNull.Value : p.Referencia);
                                    cmd.Parameters.AddWithValue("@FechaPago", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@Usuario", usuario);
                                    await cmd.ExecuteNonQueryAsync();
                                }

                                totalPagado += p.Monto;
                            }

                            // Actualizar saldo en ProveedoresCtaCte
                            decimal nuevoSaldo = saldoActual - totalPagado;
                            if (nuevoSaldo < 0) nuevoSaldo = 0;

                            var updateSql = @"
                                UPDATE ProveedoresCtaCte 
                                SET Saldo = @nuevoSaldo, 
                                    MontoAdeudado = @nuevoSaldo 
                                WHERE Id = @id;
                            ";

                            using (var cmdUpdate = new SqlCommand(updateSql, conn, tx))
                            {
                                cmdUpdate.Parameters.AddWithValue("@nuevoSaldo", nuevoSaldo);
                                cmdUpdate.Parameters.AddWithValue("@id", ctaCteId);
                                await cmdUpdate.ExecuteNonQueryAsync();
                            }

                            if (nuevoSaldo == 0 && compraId.HasValue)
                            {
                                var updateCompra = new SqlCommand(
                                    "UPDATE ComprasProveedores SET EsCtaCte = 0 WHERE Id = @id;", conn, tx);
                                updateCompra.Parameters.AddWithValue("@id", compraId.Value);
                                await updateCompra.ExecuteNonQueryAsync();
                            }

                            tx.Commit();
                            MessageBox.Show("Pago registrado exitosamente.", "Éxito",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            MessageBox.Show($"Error guardando pago: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnHistorial_Click(object sender, EventArgs e)
        {
            using (var historial = new HistorialPagosProveedorForm(proveedorId, proveedorNombre))
            {
                historial.StartPosition = FormStartPosition.CenterParent;
                historial.ShowDialog(this);
            }
        }
    }

    public class PagoGeneralProveedorForm : Form
    {
        private readonly int proveedorId;
        private readonly string proveedorNombre;
        private readonly decimal totalAdeudado;

        private TextBox txtMonto;
        private ComboBox cmbMetodo;
        private TextBox txtReferencia;
        private Button btnAceptar;
        private Button btnCancelar;
        private CheckBox chkDistribuirAutomatico;

        public PagoGeneralProveedorForm(int proveedorId, string proveedorNombre, decimal totalAdeudado)
        {
            this.proveedorId = proveedorId;
            this.proveedorNombre = proveedorNombre;
            this.totalAdeudado = totalAdeudado;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Pago General a Proveedor";
            this.ClientSize = new Size(450, 320);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9F);

            var lblProveedor = new Label
            {
                Text = $"Proveedor: {proveedorNombre}",
                Left = 20,
                Top = 20,
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            var lblDeuda = new Label
            {
                Text = $"Deuda Total: {totalAdeudado:C2}",
                Left = 20,
                Top = lblProveedor.Bottom + 8,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(211, 47, 47)
            };

            var lblMonto = new Label
            {
                Text = "Monto a Pagar:",
                Left = 20,
                Top = lblDeuda.Bottom + 24,
                Width = 120
            };

            txtMonto = new TextBox
            {
                Left = lblMonto.Right + 10,
                Top = lblMonto.Top - 2,
                Width = 150,
                Font = new Font("Segoe UI", 10F)
            };
            txtMonto.Text = totalAdeudado.ToString("F2");

            var lblMetodo = new Label
            {
                Text = "Medio de Pago:",
                Left = 20,
                Top = txtMonto.Bottom + 16,
                Width = 120
            };

            cmbMetodo = new ComboBox
            {
                Left = lblMetodo.Right + 10,
                Top = lblMetodo.Top - 2,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbMetodo.Items.AddRange(new object[] { "Efectivo", "DNI", "MercadoPago" });
            cmbMetodo.SelectedIndex = 0;

            var lblReferencia = new Label
            {
                Text = "Referencia:",
                Left = 20,
                Top = cmbMetodo.Bottom + 16,
                Width = 120
            };

            txtReferencia = new TextBox
            {
                Left = lblReferencia.Right + 10,
                Top = lblReferencia.Top - 2,
                Width = 280
            };

            chkDistribuirAutomatico = new CheckBox
            {
                Text = "Distribuir automáticamente entre facturas (FIFO)",
                Left = 20,
                Top = txtReferencia.Bottom + 20,
                Width = 400,
                Checked = true
            };

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Left = this.ClientSize.Width - 220,
                Top = this.ClientSize.Height - 50,
                Width = 90,
                Height = 32,
                BackColor = Color.FromArgb(96, 125, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            btnAceptar = new Button
            {
                Text = "Aceptar",
                Left = this.ClientSize.Width - 120,
                Top = this.ClientSize.Height - 50,
                Width = 90,
                Height = 32,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAceptar.FlatAppearance.BorderSize = 0;
            btnAceptar.Click += async (s, e) => await BtnAceptar_Click(s, e);

            this.Controls.AddRange(new Control[]
            {
                lblProveedor, lblDeuda, lblMonto, txtMonto,
                lblMetodo, cmbMetodo, lblReferencia, txtReferencia,
                chkDistribuirAutomatico, btnAceptar, btnCancelar
            });
        }

        // ✅ CORREGIDO: Usar tabla PagoProveedores
        private async Task BtnAceptar_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtMonto.Text, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Ingrese un monto válido.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (monto > totalAdeudado)
            {
                var result = MessageBox.Show(
                    $"El monto ingresado ({monto:C2}) supera la deuda total ({totalAdeudado:C2}).\n¿Desea continuar?",
                    "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No) return;
            }

            try
            {
                string cs = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build()
                    .GetConnectionString("DefaultConnection");

                string usuario = AuthenticationService.SesionActual?.Usuario?.NombreUsuario ?? Environment.UserName;
                string medioPago = cmbMetodo.SelectedItem?.ToString() ?? "Efectivo";
                string referencia = txtReferencia.Text.Trim();

                using (var conn = new SqlConnection(cs))
                {
                    await conn.OpenAsync();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            // Obtener facturas pendientes con fecha y número
                            var sql = @"
                        SELECT cta.Id, cta.CompraId, cta.Saldo, cta.Fecha, COALESCE(cp.NumeroFactura, 'Sin Nro.') AS NumeroFactura
                        FROM ProveedoresCtaCte cta
                        LEFT JOIN ComprasProveedores cp ON cta.CompraId = cp.Id
                        WHERE (cta.ProveedorId = @proveedorId OR (@proveedorId = 0 AND cta.ProveedorId IS NULL))
                            AND cta.Saldo > 0
                        ORDER BY cta.Fecha ASC;
                    ";

                            var facturas = new List<(int Id, int? CompraId, decimal Saldo, DateTime Fecha, string NumeroFactura)>();

                            using (var cmd = new SqlCommand(sql, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@proveedorId", proveedorId);
                                using (var reader = await cmd.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        facturas.Add((
                                            reader.GetInt32(0),
                                            reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                                            reader.GetDecimal(2),
                                            reader.GetDateTime(3),
                                            reader.GetString(4)
                                        ));
                                    }
                                }
                            }

                            decimal montoRestante = monto;

                            foreach (var factura in facturas)
                            {
                                if (montoRestante <= 0) break;

                                decimal montoPagar = Math.Min(montoRestante, factura.Saldo);

                                // ✅ Construir Observaciones IGUAL que PagoProveedorRapidoForm
                                string observaciones = $"Compra #{factura.CompraId ?? 0} | Método: {medioPago} | " +
                                    $"Ref: {referencia} - {factura.Fecha:dd/MM/yyyy} - Saldo: {factura.Saldo:C2}";

                                // ✅ INSERT usando SOLO las columnas que existen
                                var insertPago = @"
                                INSERT INTO PagosProveedores (FechaPago, Proveedor, Monto, Observaciones, NumeroCajero, UsuarioRegistro, NombreEquipo, FechaRegistro, IdProveedor)
                                VALUES (@FechaPago, @Proveedor, @Monto, @Observaciones, @NumeroCajero, @Usuario, @NombreEquipo, @FechaRegistro, @IdProveedor);
";

                                using (var cmdPago = new SqlCommand(insertPago, conn, tx))
                                {
                                    cmdPago.Parameters.AddWithValue("@FechaPago", DateTime.Now);
                                    cmdPago.Parameters.AddWithValue("@Proveedor", proveedorNombre);
                                    cmdPago.Parameters.AddWithValue("@Monto", montoPagar);
                                    cmdPago.Parameters.AddWithValue("@Observaciones", observaciones);
                                    cmdPago.Parameters.AddWithValue("@NumeroCajero",
                                        AuthenticationService.SesionActual?.Usuario?.NumeroCajero ?? 1);
                                    cmdPago.Parameters.AddWithValue("@Usuario", usuario);
                                    cmdPago.Parameters.AddWithValue("@NombreEquipo", Environment.MachineName);
                                    cmdPago.Parameters.AddWithValue("@FechaRegistro", DateTime.Now);
                                    cmdPago.Parameters.AddWithValue("@IdProveedor", proveedorId > 0 ? (object)proveedorId : DBNull.Value);

                                    await cmdPago.ExecuteNonQueryAsync();
                                }

                                // Actualizar saldo
                                decimal nuevoSaldo = factura.Saldo - montoPagar;
                                var updateSaldo = @"
                            UPDATE ProveedoresCtaCte 
                            SET Saldo = @nuevoSaldo, MontoAdeudado = @nuevoSaldo 
                            WHERE Id = @id;
                        ";

                                using (var cmdUpdate = new SqlCommand(updateSaldo, conn, tx))
                                {
                                    cmdUpdate.Parameters.AddWithValue("@nuevoSaldo", nuevoSaldo);
                                    cmdUpdate.Parameters.AddWithValue("@id", factura.Id);
                                    await cmdUpdate.ExecuteNonQueryAsync();
                                }

                                if (nuevoSaldo == 0 && factura.CompraId.HasValue)
                                {
                                    var updateCompra = new SqlCommand(
                                        "UPDATE ComprasProveedores SET EsCtaCte = 0 WHERE Id = @id;", conn, tx);
                                    updateCompra.Parameters.AddWithValue("@id", factura.CompraId.Value);
                                    await updateCompra.ExecuteNonQueryAsync();
                                }

                                montoRestante -= montoPagar;
                            }

                            tx.Commit();
                            MessageBox.Show($"Pago de {monto:C2} registrado exitosamente.", "Éxito",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.DialogResult = DialogResult.OK;
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            MessageBox.Show($"Error procesando pago: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class HistorialPagosProveedorForm : Form
    {
        private readonly int proveedorId;
        private readonly string proveedorNombre;
        private DataGridView dgvHistorial;
        private Label lblTotal;
        private DateTimePicker dtpDesde;
        private DateTimePicker dtpHasta;
        private Button btnFiltrar;

        public HistorialPagosProveedorForm(int proveedorId, string proveedorNombre)
        {
            this.proveedorId = proveedorId;
            this.proveedorNombre = proveedorNombre;
            InitializeComponent();
            this.Load += async (s, e) => await CargarHistorialAsync();
        }

        private void InitializeComponent()
        {
            // ✅ MODIFICADO: Altura reducida de 550 a 450
            this.Text = $"Historial de Pagos - {proveedorNombre}";
            this.ClientSize = new Size(900, 450); // ✅ REDUCIDO
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F);

            // ✅ NUEVO: Panel compacto para filtros
            var pnlFiltros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45, // ✅ Más compacto
                Padding = new Padding(12, 8, 12, 8),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            var lblDesde = new Label
            {
                Text = "Desde:",
                Left = 0,
                Top = 10,
                AutoSize = true
            };

            dtpDesde = new DateTimePicker
            {
                Left = lblDesde.Right + 6,
                Top = 8,
                Width = 110, // ✅ Más estrecho
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddMonths(-1)
            };

            var lblHasta = new Label
            {
                Text = "Hasta:",
                Left = dtpDesde.Right + 10,
                Top = 10,
                AutoSize = true
            };

            dtpHasta = new DateTimePicker
            {
                Left = lblHasta.Right + 6,
                Top = 8,
                Width = 110, // ✅ Más estrecho
                Format = DateTimePickerFormat.Short
            };

            btnFiltrar = new Button
            {
                Text = "Filtrar",
                Left = dtpHasta.Right + 10,
                Top = 7,
                Width = 80,
                Height = 26, // ✅ Más bajo
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnFiltrar.FlatAppearance.BorderSize = 0;
            btnFiltrar.Click += async (s, e) => await CargarHistorialAsync();

            pnlFiltros.Controls.AddRange(new Control[]
            {
            lblDesde, dtpDesde, lblHasta, dtpHasta, btnFiltrar
            });

            // ✅ MODIFICADO: Grilla más grande (ocupa más espacio)
            dgvHistorial = new DataGridView
            {
                Dock = DockStyle.Fill, // ✅ Ocupa todo el espacio disponible
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                BorderStyle = BorderStyle.None // ✅ Sin borde para ganar espacio
            };

            // ✅ MODIFICADO: Footer más compacto
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 35, // ✅ REDUCIDO de ~50 a 35
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(12, 8, 12, 8)
            };

            lblTotal = new Label
            {
                Text = "Total Pagado: $0.00",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243)
            };

            pnlFooter.Controls.Add(lblTotal);

            // ✅ Agregar controles en orden de apilamiento
            this.Controls.Add(dgvHistorial);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlFiltros);
        }

        private string GetConnectionString()
        {
            var cfg = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return cfg.GetConnectionString("DefaultConnection");
        }

        private async Task CargarHistorialAsync()
        {
            try
            {
                string cs = GetConnectionString();
                var dt = new DataTable();

                using (var conn = new SqlConnection(cs))
                {
                    var sql = @"
                SELECT 
                    p.Id,
                    p.FechaPago AS Fecha,
                    CASE 
                        WHEN p.Observaciones LIKE 'Compra #%' 
                        THEN SUBSTRING(p.Observaciones, CHARINDEX('#', p.Observaciones) + 1, 
                             CHARINDEX(' |', p.Observaciones) - CHARINDEX('#', p.Observaciones) - 1)
                        ELSE 'N/A'
                    END AS NumeroFactura,
                    CASE 
                        WHEN p.Observaciones LIKE '%Método:%' 
                        THEN LTRIM(SUBSTRING(p.Observaciones, 
                             CHARINDEX('Método:', p.Observaciones) + 8, 
                             CHARINDEX('|', p.Observaciones, CHARINDEX('Método:', p.Observaciones)) - 
                             CHARINDEX('Método:', p.Observaciones) - 8))
                        ELSE ''
                    END AS Metodo,
                    p.Monto,
                    p.Observaciones AS Referencia
                FROM PagosProveedores p
                WHERE p.Proveedor = @proveedorNombre
                    AND p.FechaPago BETWEEN @desde AND @hasta
                ORDER BY p.FechaPago DESC";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@proveedorNombre", proveedorNombre);
                        cmd.Parameters.AddWithValue("@desde", dtpDesde.Value.Date);
                        cmd.Parameters.AddWithValue("@hasta", dtpHasta.Value.Date.AddDays(1).AddTicks(-1));

                        using (var da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));
                        }
                    }
                }

                dgvHistorial.DataSource = dt;
                FormatearGrilla();

                decimal totalPagado = 0m;
                foreach (DataRow row in dt.Rows)
                {
                    if (row["Monto"] != DBNull.Value)
                        totalPagado += Convert.ToDecimal(row["Monto"]);
                }

                lblTotal.Text = $"Total Pagado: {totalPagado:C2}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando historial: {ex.Message}\n\n{ex.StackTrace}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatearGrilla()
        {
            if (dgvHistorial.Columns.Contains("Id"))
                dgvHistorial.Columns["Id"].Visible = false;

            if (dgvHistorial.Columns.Contains("Fecha"))
            {
                dgvHistorial.Columns["Fecha"].HeaderText = "Fecha";
                dgvHistorial.Columns["Fecha"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                dgvHistorial.Columns["Fecha"].Width = 130; // ✅ Más compacto
            }

            if (dgvHistorial.Columns.Contains("NumeroFactura"))
            {
                dgvHistorial.Columns["NumeroFactura"].HeaderText = "Factura";
                dgvHistorial.Columns["NumeroFactura"].Width = 100; // ✅ Más estrecho
            }

            if (dgvHistorial.Columns.Contains("Metodo"))
            {
                dgvHistorial.Columns["Metodo"].HeaderText = "Método";
                dgvHistorial.Columns["Metodo"].Width = 90; // ✅ Más estrecho
            }

            if (dgvHistorial.Columns.Contains("Monto"))
            {
                dgvHistorial.Columns["Monto"].HeaderText = "Monto";
                dgvHistorial.Columns["Monto"].DefaultCellStyle.Format = "C2";
                dgvHistorial.Columns["Monto"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvHistorial.Columns["Monto"].Width = 110; // ✅ Más estrecho
                dgvHistorial.Columns["Monto"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }

            if (dgvHistorial.Columns.Contains("Referencia"))
            {
                dgvHistorial.Columns["Referencia"].HeaderText = "Referencia";
                dgvHistorial.Columns["Referencia"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // ✅ Usa espacio restante
            }

            // ✅ ELIMINADA: Columna "Usuario" para ahorrar espacio
        }
    }
}