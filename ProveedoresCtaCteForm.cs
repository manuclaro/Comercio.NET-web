using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;

namespace Comercio.NET.Formularios
{
    public class ProveedoresCtaCteForm : Form
    {
        private DataGridView dgv;
        private Button btnRefrescar;
        private Button btnRegistrarPago;
        private Button btnDetalle;
        private TextBox txtFiltroProveedor;

        // estructura simple para bind
        private class CtaCteItem
        {
            public int Id { get; set; }
            public int? ProveedorId { get; set; }
            public int? CompraId { get; set; }
            public DateTime Fecha { get; set; }
            public decimal MontoTotal { get; set; }
            public decimal MontoAdeudado { get; set; }
            public decimal Saldo { get; set; }
            public decimal TotalPagado { get; set; }
            public string Observaciones { get; set; }
            public string ProveedorNombre { get; set; }
        }

        public ProveedoresCtaCteForm()
        {
            InitializeComponent();
            this.Load += async (s, e) => await CargarCtaCteAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "CtaCte Proveedores";
            this.ClientSize = new Size(920, 520);
            this.Font = new Font("Segoe UI", 9F);
            this.StartPosition = FormStartPosition.CenterParent;

            dgv = new DataGridView
            {
                Left = 12,
                Top = 44,
                Width = this.ClientSize.Width - 24,
                Height = this.ClientSize.Height - 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Id", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Proveedor", HeaderText = "Proveedor", Width = 220 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "Fecha", Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompraId", HeaderText = "CompraId", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "MontoTotal", HeaderText = "Monto Total", Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "MontoAdeudado", HeaderText = "Monto Adeudado", Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPagado", HeaderText = "Total Pagado", Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Saldo", HeaderText = "Saldo", Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Observaciones", HeaderText = "Observaciones", Width = 220 });

            btnRefrescar = new Button { Text = "Refrescar", Left = 12, Top = 10, Width = 100 };
            btnRegistrarPago = new Button { Text = "Registrar Pago", Left = btnRefrescar.Right + 8, Top = 10, Width = 120 };
            btnDetalle = new Button { Text = "Detalle", Left = btnRegistrarPago.Right + 8, Top = 10, Width = 100 };

            txtFiltroProveedor = new TextBox { Left = btnDetalle.Right + 16, Top = 12, Width = 260, PlaceholderText = "Filtrar por proveedor..." };

            this.Controls.Add(dgv);
            this.Controls.Add(btnRefrescar);
            this.Controls.Add(btnRegistrarPago);
            this.Controls.Add(btnDetalle);
            this.Controls.Add(txtFiltroProveedor);

            btnRefrescar.Click += async (s, e) => await CargarCtaCteAsync();
            btnRegistrarPago.Click += BtnRegistrarPago_Click;
            btnDetalle.Click += BtnDetalle_Click;
            txtFiltroProveedor.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await CargarCtaCteAsync(txtFiltroProveedor.Text.Trim());
                }
            };

            // Doble clic en la grilla abre detalle
            dgv.CellDoubleClick += Dgv_CellDoubleClick;
        }

        private void Dgv_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            // Evitar headers o índices inválidos
            if (e.RowIndex < 0 || e.RowIndex >= dgv.Rows.Count) return;
            BtnDetalle_Click(sender, EventArgs.Empty);
        }

        private async Task CargarCtaCteAsync(string filtroProveedor = "")
        {
            dgv.Rows.Clear();

            string cs = GetConnectionString();
            try
            {
                using (var conn = new SqlConnection(cs))
                {
                    await conn.OpenAsync();
                    var sql = @"
                SELECT 
                    c.Id, c.ProveedorId, c.CompraId, c.Fecha, c.MontoTotal, c.MontoAdeudado, c.Saldo, c.Observaciones,
                    p.Nombre AS ProveedorNombre,
                    /* Total pagado: sumamos pagos asociados ya sea por CtaCteId o por CompraId cuando exista */
                    ISNULL((
                        SELECT SUM(pago.Monto) FROM ComprasProveedoresPagos pago
                        WHERE (pago.CtaCteId = c.Id)
                           OR (c.CompraId IS NOT NULL AND pago.CompraId = c.CompraId)
                    ), 0) AS TotalPagado
                FROM ProveedoresCtaCte c
                LEFT JOIN Proveedores p ON c.ProveedorId = p.Id
                WHERE (@filtro = '' OR p.Nombre LIKE '%' + @filtro + '%')
                ORDER BY c.Fecha DESC, p.Nombre;
            ";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        var filtro = string.IsNullOrWhiteSpace(filtroProveedor) ? "" : filtroProveedor.Trim();
                        cmd.Parameters.AddWithValue("@filtro", filtro);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int id = reader.GetInt32(0);
                                int? proveedorId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
                                int? compraId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                                DateTime fecha = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3);
                                decimal montoTotal = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4);
                                decimal montoAdeudado = reader.IsDBNull(5) ? 0m : reader.GetDecimal(5);
                                decimal saldo = reader.IsDBNull(6) ? 0m : reader.GetDecimal(6);
                                string obs = reader.IsDBNull(7) ? "" : reader.GetString(7);
                                string provNombre = reader.IsDBNull(8) ? "" : reader.GetString(8);
                                decimal totalPagado = reader.IsDBNull(9) ? 0m : reader.GetDecimal(9);

                                dgv.Rows.Add(
                                    id,
                                    provNombre,
                                    fecha.ToString("g"),
                                    compraId,
                                    montoTotal.ToString("F2"),
                                    montoAdeudado.ToString("F2"),
                                    totalPagado.ToString("F2"),
                                    saldo.ToString("F2"),
                                    obs
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando CtaCte: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDetalle_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una fila.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgv.SelectedRows[0];
            int ctaId = Convert.ToInt32(row.Cells["Id"].Value);
            int? compraId = row.Cells["CompraId"].Value != null && int.TryParse(row.Cells["CompraId"].Value.ToString(), out var c) ? (int?)c : null;

            try
            {
                using (var frm = new PagosDetalleForm(compraId, compraId.HasValue ? (int?)null : ctaId))
                {
                    frm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error mostrando detalle de pagos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnRegistrarPago_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una línea para registrar el pago.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgv.SelectedRows[0];
            int ctaId = Convert.ToInt32(row.Cells["Id"].Value);

            var fila = await ObtenerCtaCtePorIdAsync(ctaId);
            if (fila == null)
            {
                MessageBox.Show("No se encontró la línea seleccionada.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            decimal saldo = fila.Saldo;
            string proveedorNombre = fila.ProveedorNombre ?? "";

            using (var frm = new FormaPagoProveedorForm(saldo, fila.ProveedorId, fila.CompraId, proveedorNombre))
            {
                var dr = frm.ShowDialog(this);
                if (dr != DialogResult.OK) return;
                var pagos = frm.Pagos ?? new List<PagoInfo>();
                if (pagos.Count == 0)
                {
                    MessageBox.Show("No se registraron pagos.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                await GuardarPagosEnCtaCteAsync(ctaId, fila.CompraId, pagos);
                await CargarCtaCteAsync(txtFiltroProveedor.Text.Trim());
            }
        }

        private async Task<CtaCteItem?> ObtenerCtaCtePorIdAsync(int id)
        {
            string cs = GetConnectionString();
            try
            {
                using (var conn = new SqlConnection(cs))
                {
                    await conn.OpenAsync();
                    var sql = @"
                        SELECT c.Id, c.ProveedorId, c.CompraId, c.Fecha, c.MontoTotal, c.MontoAdeudado, c.Saldo, c.Observaciones,
                               p.Nombre AS ProveedorNombre
                        FROM ProveedoresCtaCte c
                        LEFT JOIN Proveedores p ON c.ProveedorId = p.Id
                        WHERE c.Id = @id;
                    ";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new CtaCteItem
                                {
                                    Id = reader.GetInt32(0),
                                    ProveedorId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                                    CompraId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                                    Fecha = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                                    MontoTotal = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4),
                                    MontoAdeudado = reader.IsDBNull(5) ? 0m : reader.GetDecimal(5),
                                    Saldo = reader.IsDBNull(6) ? 0m : reader.GetDecimal(6),
                                    Observaciones = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                    ProveedorNombre = reader.IsDBNull(8) ? "" : reader.GetString(8)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error obteniendo CtaCte: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return null;
        }

        private async Task GuardarPagosEnCtaCteAsync(int ctaId, int? compraId, List<PagoInfo> pagos)
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
                            decimal totalPagado = 0m;

                            var insertPagoSql = @"
                                INSERT INTO ComprasProveedoresPagos
                                (CompraId, CtaCteId, Metodo, Monto, Referencia, Fecha, Usuario)
                                VALUES (@CompraId, @CtaCteId, @Metodo, @Monto, @Referencia, @Fecha, @Usuario);";

                            foreach (var p in pagos)
                            {
                                using (var cmd = new SqlCommand(insertPagoSql, conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@CompraId", compraId.HasValue ? (object)compraId.Value : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CtaCteId", ctaId);

                                    cmd.Parameters.AddWithValue("@Metodo", string.IsNullOrWhiteSpace(p.Metodo) ? (object)DBNull.Value : p.Metodo);
                                    cmd.Parameters.AddWithValue("@Monto", p.Monto);
                                    cmd.Parameters.AddWithValue("@Referencia", string.IsNullOrWhiteSpace(p.Referencia) ? (object)DBNull.Value : p.Referencia);
                                    cmd.Parameters.AddWithValue("@Fecha", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@Usuario", Environment.UserName);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                                totalPagado += p.Monto;
                            }

                            var updateSql = @"UPDATE ProveedoresCtaCte SET Saldo = Saldo - @Pagado WHERE Id = @Id;";
                            using (var cmdUpd = new SqlCommand(updateSql, conn, tx))
                            {
                                cmdUpd.Parameters.AddWithValue("@Pagado", totalPagado);
                                cmdUpd.Parameters.AddWithValue("@Id", ctaId);
                                await cmdUpd.ExecuteNonQueryAsync();
                            }

                            tx.Commit();
                            MessageBox.Show("Pago(s) registrado(s) correctamente.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            MessageBox.Show($"Error guardando pago(s): {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión al guardar pagos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetConnectionString()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return config.GetConnectionString("DefaultConnection");
        }
    }
}