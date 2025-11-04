using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;

namespace Comercio.NET.Formularios
{
    public class PagosDetalleForm : Form
    {
        private DataGridView dgv;
        private Button btnCerrar;
        private Label lblTotalCompra;
        private Label lblTotalPagado;
        private Label lblSaldo;
        private readonly int? compraId;
        private readonly int? ctaCteId;

        public PagosDetalleForm(int? compraId = null, int? ctaCteId = null)
        {
            this.compraId = compraId;
            this.ctaCteId = ctaCteId;
            InitializeComponent();
            this.Load += async (s, e) => await CargarPagosAsync();
        }

        private void InitializeComponent()
        {
            this.Text = compraId.HasValue ? $"Pagos - Compra #{compraId}" : $"Pagos - CtaCte #{ctaCteId}";
            this.ClientSize = new Size(720, 380);
            this.Font = new Font("Segoe UI", 9F);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            lblTotalCompra = new Label
            {
                Left = 12,
                Top = 12,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            lblTotalPagado = new Label
            {
                Left = 12,
                Top = lblTotalCompra.Bottom + 6,
                AutoSize = true
            };

            lblSaldo = new Label
            {
                Left = lblTotalPagado.Right + 24,
                Top = lblTotalCompra.Bottom + 6,
                AutoSize = true,
                ForeColor = Color.DarkRed
            };

            dgv = new DataGridView
            {
                Left = 12,
                Top = lblTotalPagado.Bottom + 12,
                Width = this.ClientSize.Width - 24,
                Height = this.ClientSize.Height - 96,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Id", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Metodo", HeaderText = "Medio de Pago", Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Monto", HeaderText = "Monto", Width = 100 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Referencia", HeaderText = "Referencia", Width = 200 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "Fecha", Width = 140 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Usuario", HeaderText = "Usuario", Width = 100 });

            btnCerrar = new Button
            {
                Text = "Cerrar",
                Width = 100,
                Height = 30,
                Left = this.ClientSize.Width - 124,
                Top = this.ClientSize.Height - 44,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            btnCerrar.Click += (s, e) => this.Close();

            this.Controls.Add(lblTotalCompra);
            this.Controls.Add(lblTotalPagado);
            this.Controls.Add(lblSaldo);
            this.Controls.Add(dgv);
            this.Controls.Add(btnCerrar);
        }

        private async Task CargarPagosAsync()
        {
            dgv.Rows.Clear();
            lblTotalCompra.Text = "";
            lblTotalPagado.Text = "";
            lblSaldo.Text = "";

            decimal totalPagado = 0m;
            decimal montoTotal = 0m;
            decimal saldoActual = 0m;

            string cs = GetConnectionString();
            try
            {
                using (var conn = new SqlConnection(cs))
                {
                    await conn.OpenAsync();

                    // 1) Cargar pagos relacionados
                    var sql = @"
                        SELECT Id, CompraId, CtaCteId, Metodo, Monto, Referencia, Fecha, Usuario
                        FROM ComprasProveedoresPagos
                        WHERE (@compraId IS NOT NULL AND CompraId = @compraId)
                           OR (@compraId IS NULL AND @ctacteId IS NOT NULL AND CtaCteId = @ctacteId)
                        ORDER BY Fecha;
                    ";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        if (compraId.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@compraId", compraId.Value);
                            cmd.Parameters.AddWithValue("@ctacteId", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@compraId", DBNull.Value);
                            cmd.Parameters.AddWithValue("@ctacteId", ctaCteId.HasValue ? (object)ctaCteId.Value : DBNull.Value);
                        }

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int id = reader.GetInt32(0);
                                string metodo = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                decimal monto = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4);
                                string referencia = reader.IsDBNull(5) ? "" : reader.GetString(5);
                                DateTime fecha = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6);
                                string usuario = reader.IsDBNull(7) ? "" : reader.GetString(7);

                                dgv.Rows.Add(id, metodo, monto.ToString("C2", CultureInfo.CurrentCulture), referencia, fecha.ToString("g"), usuario);
                                totalPagado += monto;
                            }
                        }
                    }

                    // 2) Obtener monto total y saldo según contexto
                    if (compraId.HasValue)
                    {
                        var sqlCompra = @"SELECT ImporteTotal FROM ComprasProveedores WHERE Id = @id;";
                        using (var cmd2 = new SqlCommand(sqlCompra, conn))
                        {
                            cmd2.Parameters.AddWithValue("@id", compraId.Value);
                            var res = await cmd2.ExecuteScalarAsync();
                            if (res != null && decimal.TryParse(res.ToString(), out var t))
                                montoTotal = t;
                        }
                        saldoActual = montoTotal - totalPagado;
                    }
                    else if (ctaCteId.HasValue)
                    {
                        var sqlCta = @"SELECT MontoTotal, Saldo FROM ProveedoresCtaCte WHERE Id = @id;";
                        using (var cmd3 = new SqlCommand(sqlCta, conn))
                        {
                            cmd3.Parameters.AddWithValue("@id", ctaCteId.Value);
                            using (var r = await cmd3.ExecuteReaderAsync())
                            {
                                if (await r.ReadAsync())
                                {
                                    montoTotal = r.IsDBNull(0) ? 0m : r.GetDecimal(0);
                                    saldoActual = r.IsDBNull(1) ? 0m : r.GetDecimal(1);
                                }
                            }
                        }
                    }

                    // 3) Actualizar labels
                    if (montoTotal > 0m)
                        lblTotalCompra.Text = $"Total: {montoTotal.ToString("C2", CultureInfo.CurrentCulture)}";
                    else
                        lblTotalCompra.Text = compraId.HasValue ? "Total: (no disponible)" : "";

                    lblTotalPagado.Text = $"Pagado: {totalPagado.ToString("C2", CultureInfo.CurrentCulture)}";

                    if (montoTotal > 0m)
                    {
                        var saldoCalculado = montoTotal - totalPagado;
                        if (ctaCteId.HasValue && saldoActual != 0m)
                        {
                            lblSaldo.Text = $"Saldo (tabla): {saldoActual.ToString("C2", CultureInfo.CurrentCulture)}   |   Saldo (calc): {saldoCalculado.ToString("C2", CultureInfo.CurrentCulture)}";
                        }
                        else
                        {
                            lblSaldo.Text = $"Saldo: {saldoCalculado.ToString("C2", CultureInfo.CurrentCulture)}";
                        }
                        lblSaldo.ForeColor = (montoTotal - totalPagado) > 0 ? Color.DarkRed : Color.DarkGreen;
                    }
                    else
                    {
                        if (ctaCteId.HasValue)
                        {
                            lblSaldo.Text = $"Saldo (tabla): {saldoActual.ToString("C2", CultureInfo.CurrentCulture)}";
                            lblSaldo.ForeColor = saldoActual > 0 ? Color.DarkRed : Color.DarkGreen;
                        }
                        else
                        {
                            lblSaldo.Text = "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando pagos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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