using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    // Form modal para ingresar pagos por distintos medios y permitir pagos parciales
    public class FormaPagoProveedorForm : Form
    {
        private DataGridView dgvPagos;
        private ComboBox cmbMetodo;
        private TextBox txtMonto;
        private TextBox txtReferencia;
        private Button btnAgregar;
        private Button btnEliminar;
        private Button btnConfirmar;
        private Button btnCancelar;
        private Label lblTotal;
        private Label lblPagado;
        private Label lblSaldo;

        private readonly decimal totalCompra;
        private readonly int? proveedorId;
        private readonly int? compraId; // <-- ahora nullable
        private readonly string proveedorNombre;

        public List<PagoInfo> Pagos { get; private set; } = new List<PagoInfo>();

        public FormaPagoProveedorForm(decimal totalCompra, int? proveedorId, int? compraId, string proveedorNombre)
        {
            this.totalCompra = totalCompra;
            this.proveedorId = proveedorId;
            this.compraId = compraId;
            this.proveedorNombre = proveedorNombre ?? "";

            InitializeComponent();
            ActualizarTotales();
        }

        private void InitializeComponent()
        {
            this.Text = "Forma de Pago";
            this.ClientSize = new Size(570, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F);

            var lblInfo = new Label { Text = $"Proveedor: {proveedorNombre}", Left = 12, Top = 12, AutoSize = true };
            var lblTotalCompra = new Label { Text = $"Total Compra: {totalCompra.ToString("C2", CultureInfo.CurrentCulture)}", Left = 12, Top = lblInfo.Bottom + 6, AutoSize = true };

            dgvPagos = new DataGridView
            {
                Left = 12,
                Top = lblTotalCompra.Bottom + 12,
                Width = this.ClientSize.Width - 24,
                Height = 160,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White
            };
            dgvPagos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Metodo", HeaderText = "Medio de Pago", Width = 150 });
            dgvPagos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Monto", HeaderText = "Monto", Width = 120 });
            dgvPagos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Referencia", HeaderText = "Referencia", Width = 220 });

            var lblMetodo = new Label { Text = "Medio", Left = 12, Top = dgvPagos.Bottom + 10, Width = 50 };
            cmbMetodo = new ComboBox { Left = lblMetodo.Right + 6, Top = lblMetodo.Top - 3, Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbMetodo.Items.AddRange(new[] { "Efectivo", "DNI", "Transferencia", "MercadoPago" });
            cmbMetodo.SelectedIndex = 0;

            var lblMonto = new Label { Text = "Monto", Left = cmbMetodo.Right + 12, Top = lblMetodo.Top, Width = 50 };
            txtMonto = new TextBox { Left = lblMonto.Right + 6, Top = lblMonto.Top - 3, Width = 100 };

            var lblRef = new Label { Text = "Referencia", Left = txtMonto.Right + 12, Top = lblMetodo.Top, Width = 70 };
            txtReferencia = new TextBox { Left = lblRef.Right + 6, Top = lblMetodo.Top - 3, Width = 160 };

            btnAgregar = new Button { Text = "Agregar", Left = 12, Top = txtMonto.Bottom + 10, Width = 100 };
            btnEliminar = new Button { Text = "Eliminar", Left = btnAgregar.Right + 12, Top = btnAgregar.Top, Width = 100 };

            lblTotal = new Label { Text = $"Total: {totalCompra.ToString("C2", CultureInfo.CurrentCulture)}", Left = 12, Top = btnAgregar.Bottom + 12, AutoSize = true };
            lblPagado = new Label { Text = $"Pagado: {0m.ToString("C2", CultureInfo.CurrentCulture)}", Left = lblTotal.Right + 20, Top = lblTotal.Top, AutoSize = true };
            lblSaldo = new Label { Text = $"Saldo: {totalCompra.ToString("C2", CultureInfo.CurrentCulture)}", Left = lblPagado.Right + 20, Top = lblTotal.Top, AutoSize = true, ForeColor = Color.DarkRed };

            btnConfirmar = new Button { Text = "Confirmar", Left = this.ClientSize.Width - 240, Top = lblTotal.Bottom + 16, Width = 110, BackColor = Color.FromArgb(60, 179, 113), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
            btnConfirmar.FlatAppearance.BorderSize = 0;
            btnCancelar = new Button { Text = "Cancelar", Left = btnConfirmar.Right + 12, Top = btnConfirmar.Top, Width = 110, BackColor = Color.FromArgb(160, 160, 160), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
            btnCancelar.FlatAppearance.BorderSize = 0;

            this.Controls.Add(lblInfo);
            this.Controls.Add(lblTotalCompra);
            this.Controls.Add(dgvPagos);
            this.Controls.Add(lblMetodo);
            this.Controls.Add(cmbMetodo);
            this.Controls.Add(lblMonto);
            this.Controls.Add(txtMonto);
            this.Controls.Add(lblRef);
            this.Controls.Add(txtReferencia);
            this.Controls.Add(btnAgregar);
            this.Controls.Add(btnEliminar);
            this.Controls.Add(lblTotal);
            this.Controls.Add(lblPagado);
            this.Controls.Add(lblSaldo);
            this.Controls.Add(btnConfirmar);
            this.Controls.Add(btnCancelar);

            btnAgregar.Click += BtnAgregar_Click;
            btnEliminar.Click += BtnEliminar_Click;
            btnConfirmar.Click += BtnConfirmar_Click;
            btnCancelar.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
        }

        private void BtnAgregar_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtMonto.Text.Trim().Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Monto inválido.");
                return;
            }

            dgvPagos.Rows.Add(cmbMetodo.Text, monto.ToString("F2", CultureInfo.InvariantCulture), txtReferencia.Text.Trim());
            txtMonto.Clear();
            txtReferencia.Clear();
            cmbMetodo.Focus();
            ActualizarTotales();
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (dgvPagos.SelectedRows.Count > 0)
            {
                dgvPagos.Rows.RemoveAt(dgvPagos.SelectedRows[0].Index);
                ActualizarTotales();
            }
        }

        private void ActualizarTotales()
        {
            decimal pagado = 0m;
            foreach (DataGridViewRow r in dgvPagos.Rows)
            {
                if (r.Cells["Monto"].Value != null && decimal.TryParse(r.Cells["Monto"].Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal m))
                    pagado += m;
            }

            lblPagado.Text = $"Pagado: {pagado.ToString("C2", CultureInfo.CurrentCulture)}";
            decimal saldo = totalCompra - pagado;
            lblSaldo.Text = $"Saldo: {saldo.ToString("C2", CultureInfo.CurrentCulture)}";
            lblSaldo.ForeColor = saldo > 0 ? Color.DarkRed : Color.DarkGreen;
        }

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            Pagos = new List<PagoInfo>();
            foreach (DataGridViewRow r in dgvPagos.Rows)
            {
                if (r.IsNewRow) continue;
                var metodo = r.Cells["Metodo"].Value?.ToString() ?? "";
                var referencia = r.Cells["Referencia"].Value?.ToString() ?? "";
                if (!decimal.TryParse(r.Cells["Monto"].Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto)) monto = 0m;
                if (monto <= 0) continue;
                Pagos.Add(new PagoInfo { Metodo = metodo, Monto = monto, Referencia = referencia });
            }

            this.DialogResult = DialogResult.OK;
        }
    }
}