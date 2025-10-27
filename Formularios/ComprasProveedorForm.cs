using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public class ComprasProveedorForm : Form
    {
        private TextBox txtNumero;
        private TextBox txtProveedor;
        private TextBox txtCuit;
        private DateTimePicker dtpFecha;
        private TextBox txtImporteNeto;
        private TextBox txtImporteIva;
        private TextBox txtImporteTotal;
        private DataGridView dgvIva;
        private Button btnAgregarIva;
        private Button btnEliminarIva;
        private Button btnGuardar;
        private Button btnCancelar;
        private TextBox txtAlicuota;
        private TextBox txtBase;

        public ComprasProveedorForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Registrar Compra Proveedor";
            this.ClientSize = new Size(760, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var lblNumero = new Label { Text = "N° Factura", Left = 12, Top = 12, Width = 100 };
            txtNumero = new TextBox { Left = 120, Top = 10, Width = 220 };

            var lblProveedor = new Label { Text = "Proveedor", Left = 12, Top = 44, Width = 100 };
            txtProveedor = new TextBox { Left = 120, Top = 42, Width = 400 };

            var lblCuit = new Label { Text = "CUIT", Left = 12, Top = 76, Width = 100 };
            txtCuit = new TextBox { Left = 120, Top = 74, Width = 150 };

            var lblFecha = new Label { Text = "Fecha", Left = 300, Top = 76, Width = 50 };
            dtpFecha = new DateTimePicker { Left = 360, Top = 72, Width = 160, Format = DateTimePickerFormat.Short };

            var pnlIva = new Panel { Left = 12, Top = 110, Width = 736, Height = 300, BorderStyle = BorderStyle.FixedSingle };

            dgvIva = new DataGridView { Left = 8, Top = 8, Width = 716, Height = 220, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
            dgvIva.Columns.Add(new DataGridViewTextBoxColumn { Name = "Alicuota", HeaderText = "Alicuota %", Width = 100 });
            dgvIva.Columns.Add(new DataGridViewTextBoxColumn { Name = "Base", HeaderText = "Base Imponible", Width = 180 });
            dgvIva.Columns.Add(new DataGridViewTextBoxColumn { Name = "ImporteIva", HeaderText = "IVA $", Width = 160 });

            var lblAlicuota = new Label { Text = "Alicuota %", Left = 8, Top = 236, Width = 70 };
            txtAlicuota = new TextBox { Left = 86, Top = 234, Width = 80 };
            var lblBase = new Label { Text = "Base", Left = 176, Top = 236, Width = 40 };
            txtBase = new TextBox { Left = 220, Top = 234, Width = 120 };

            btnAgregarIva = new Button { Text = "Agregar alícuota", Left = 352, Top = 232, Width = 130 };
            btnEliminarIva = new Button { Text = "Eliminar seleccionada", Left = 492, Top = 232, Width = 140 };

            pnlIva.Controls.Add(dgvIva);
            pnlIva.Controls.Add(lblAlicuota);
            pnlIva.Controls.Add(txtAlicuota);
            pnlIva.Controls.Add(lblBase);
            pnlIva.Controls.Add(txtBase);
            pnlIva.Controls.Add(btnAgregarIva);
            pnlIva.Controls.Add(btnEliminarIva);

            var lblNeto = new Label { Text = "Importe Neto", Left = 12, Top = 428, Width = 100 };
            txtImporteNeto = new TextBox { Left = 120, Top = 424, Width = 140, ReadOnly = true };

            var lblIva = new Label { Text = "Importe IVA", Left = 280, Top = 428, Width = 100 };
            txtImporteIva = new TextBox { Left = 360, Top = 424, Width = 140, ReadOnly = true };

            var lblTotal = new Label { Text = "Importe Total", Left = 520, Top = 428, Width = 100 };
            txtImporteTotal = new TextBox { Left = 620, Top = 424, Width = 128, ReadOnly = true };

            btnGuardar = new Button { Text = "Guardar", Left = 540, Top = 464, Width = 100 };
            btnCancelar = new Button { Text = "Cancelar", Left = 660, Top = 464, Width = 100 };

            this.Controls.Add(lblNumero);
            this.Controls.Add(txtNumero);
            this.Controls.Add(lblProveedor);
            this.Controls.Add(txtProveedor);
            this.Controls.Add(lblCuit);
            this.Controls.Add(txtCuit);
            this.Controls.Add(lblFecha);
            this.Controls.Add(dtpFecha);
            this.Controls.Add(pnlIva);
            this.Controls.Add(lblNeto);
            this.Controls.Add(txtImporteNeto);
            this.Controls.Add(lblIva);
            this.Controls.Add(txtImporteIva);
            this.Controls.Add(lblTotal);
            this.Controls.Add(txtImporteTotal);
            this.Controls.Add(btnGuardar);
            this.Controls.Add(btnCancelar);

            btnAgregarIva.Click += BtnAgregarIva_Click;
            btnEliminarIva.Click += BtnEliminarIva_Click;
            dgvIva.RowsRemoved += (s, e) => RecalcularTotales();
            btnGuardar.Click += async (s, e) => await BtnGuardar_ClickAsync();
            btnCancelar.Click += (s, e) => this.Close();
        }

        private void BtnAgregarIva_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtAlicuota.Text.Trim().Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal alicuota))
            {
                MessageBox.Show("Alicuota inválida.");
                return;
            }
            if (!decimal.TryParse(txtBase.Text.Trim().Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal baseImponible))
            {
                MessageBox.Show("Base imponible inválida.");
                return;
            }

            decimal importeIva = Math.Round(baseImponible * alicuota / 100m, 2);
            dgvIva.Rows.Add(alicuota.ToString("F2", CultureInfo.InvariantCulture), baseImponible.ToString("F2", CultureInfo.InvariantCulture), importeIva.ToString("F2", CultureInfo.InvariantCulture));

            txtAlicuota.Clear();
            txtBase.Clear();
            RecalcularTotales();
        }

        private void BtnEliminarIva_Click(object sender, EventArgs e)
        {
            if (dgvIva.SelectedRows.Count > 0)
            {
                dgvIva.Rows.RemoveAt(dgvIva.SelectedRows[0].Index);
            }
        }

        private void RecalcularTotales()
        {
            decimal sumaBase = 0m, sumaIva = 0m;
            foreach (DataGridViewRow r in dgvIva.Rows)
            {
                if (r.Cells["Base"].Value != null && decimal.TryParse(r.Cells["Base"].Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal b))
                    sumaBase += b;
                if (r.Cells["ImporteIva"].Value != null && decimal.TryParse(r.Cells["ImporteIva"].Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal i))
                    sumaIva += i;
            }

            txtImporteNeto.Text = sumaBase.ToString("C2", CultureInfo.CurrentCulture);
            txtImporteIva.Text = sumaIva.ToString("C2", CultureInfo.CurrentCulture);
            txtImporteTotal.Text = (sumaBase + sumaIva).ToString("C2", CultureInfo.CurrentCulture);
        }

        private string GetConnectionString()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return config.GetConnectionString("DefaultConnection");
        }

        private async Task BtnGuardar_ClickAsync()
        {
            if (string.IsNullOrWhiteSpace(txtNumero.Text) || string.IsNullOrWhiteSpace(txtProveedor.Text))
            {
                MessageBox.Show("Complete número y proveedor.");
                return;
            }

            // calcular valores numéricos
            decimal sumaBase = 0m, sumaIva = 0m;
            foreach (DataGridViewRow r in dgvIva.Rows)
            {
                if (r.Cells["Base"].Value != null && decimal.TryParse(r.Cells["Base"].Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal b))
                    sumaBase += b;
                if (r.Cells["ImporteIva"].Value != null && decimal.TryParse(r.Cells["ImporteIva"].Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal i))
                    sumaIva += i;
            }
            decimal total = sumaBase + sumaIva;

            string connectionString = GetConnectionString();

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            var insertSql = @"INSERT INTO ComprasProveedores
                                (NumeroFactura, Fecha, Proveedor, CUIT, ImporteNeto, ImporteIVA, ImporteTotal, EsCtaCte, NombreCtaCte, Observaciones, Usuario)
                                VALUES (@Numero, @Fecha, @Proveedor, @CUIT, @ImporteNeto, @ImporteIVA, @ImporteTotal, @EsCtaCte, @NombreCtaCte, @Observaciones, @Usuario);
                                SELECT CAST(SCOPE_IDENTITY() AS INT);";

                            int compraId;
                            using (var cmd = new SqlCommand(insertSql, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Numero", txtNumero.Text.Trim());
                                cmd.Parameters.AddWithValue("@Fecha", dtpFecha.Value.Date);
                                cmd.Parameters.AddWithValue("@Proveedor", txtProveedor.Text.Trim());
                                cmd.Parameters.AddWithValue("@CUIT", string.IsNullOrWhiteSpace(txtCuit.Text) ? (object)DBNull.Value : txtCuit.Text.Trim());
                                cmd.Parameters.AddWithValue("@ImporteNeto", sumaBase);
                                cmd.Parameters.AddWithValue("@ImporteIVA", sumaIva);
                                cmd.Parameters.AddWithValue("@ImporteTotal", total);
                                cmd.Parameters.AddWithValue("@EsCtaCte", false);
                                cmd.Parameters.AddWithValue("@NombreCtaCte", DBNull.Value);
                                cmd.Parameters.AddWithValue("@Observaciones", DBNull.Value);
                                cmd.Parameters.AddWithValue("@Usuario", Environment.UserName);

                                var res = await cmd.ExecuteScalarAsync();
                                compraId = res != null && int.TryParse(res.ToString(), out int id) ? id : 0;
                            }

                            // insertar detalles IVA
                            var insertDet = @"INSERT INTO ComprasProveedoresIvaDetalle
                                (CompraId, Alicuota, BaseImponible, ImporteIva)
                                VALUES (@CompraId, @Alicuota, @BaseImponible, @ImporteIva);";

                            foreach (DataGridViewRow r in dgvIva.Rows)
                            {
                                if (r.IsNewRow) continue;
                                if (!decimal.TryParse(r.Cells["Alicuota"].Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal ali)) continue;
                                if (!decimal.TryParse(r.Cells["Base"].Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal baseVal)) continue;
                                if (!decimal.TryParse(r.Cells["ImporteIva"].Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal ivaVal)) continue;

                                using (var cmdDet = new SqlCommand(insertDet, conn, tx))
                                {
                                    cmdDet.Parameters.AddWithValue("@CompraId", compraId);
                                    cmdDet.Parameters.AddWithValue("@Alicuota", ali);
                                    cmdDet.Parameters.AddWithValue("@BaseImponible", baseVal);
                                    cmdDet.Parameters.AddWithValue("@ImporteIva", ivaVal);
                                    await cmdDet.ExecuteNonQueryAsync();
                                }
                            }

                            tx.Commit();
                            MessageBox.Show("Compra guardada correctamente.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.Close();
                        }
                        catch (Exception exTx)
                        {
                            tx.Rollback();
                            MessageBox.Show($"Error guardando compra: {exTx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}