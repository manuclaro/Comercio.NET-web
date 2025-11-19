using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public class FormaPagoProveedorForm : Form
    {
        private readonly decimal saldoTotal;
        private readonly int? proveedorId;
        private readonly int? compraId;
        private readonly string proveedorNombre;

        private Label lblProveedor;
        private Label lblTotalCompras;
        private ComboBox cmbMedioPago;
        private TextBox txtMonto;
        private TextBox txtReferencia;
        private DataGridView dgvPagos;
        private Button btnAgregar;
        private Button btnEliminar;
        private Button btnConfirmar;
        private Button btnCancelar;
        private Label lblTotal;
        private Label lblPagado;
        private Label lblSaldo;

        public List<PagoInfo> Pagos { get; private set; }

        public FormaPagoProveedorForm(decimal saldo, int? proveedorId, int? compraId, string proveedorNombre)
        {
            this.saldoTotal = saldo;
            this.proveedorId = proveedorId;
            this.compraId = compraId;
            this.proveedorNombre = proveedorNombre;
            this.Pagos = new List<PagoInfo>();

            InitializeComponent();
            ConfigurarFormulario();
            ActualizarLabelsDeResumen();
        }

        private void InitializeComponent()
        {
            this.Text = "Forma de Pago";
            this.Size = new Size(700, 600); // ✅ AUMENTADO de 550 a 600
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            CrearControles();
        }

        private void CrearControles()
        {
            int yPos = 20;
            int leftMargin = 20;
            int controlWidth = 640;

            // Panel de información del proveedor y total
            var panelInfo = new Panel
            {
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 80),
                BackColor = Color.FromArgb(240, 248, 255),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelInfo);

            lblProveedor = new Label
            {
                Text = $"Proveedor: {proveedorNombre}",
                Location = new Point(15, 15),
                Size = new Size(600, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            panelInfo.Controls.Add(lblProveedor);

            lblTotalCompras = new Label
            {
                Text = $"Total Compras: {saldoTotal:C2}",
                Location = new Point(15, 45),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(0, 120, 215)
            };
            panelInfo.Controls.Add(lblTotalCompras);

            yPos += 100;

            // Labels para los campos de entrada
            var lblMedio = new Label
            {
                Text = "Medio",
                Location = new Point(leftMargin, yPos),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(lblMedio);

            var lblMontoLabel = new Label
            {
                Text = "Monto",
                Location = new Point(leftMargin + 200, yPos),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(lblMontoLabel);

            var lblReferenciaLabel = new Label
            {
                Text = "Referencia",
                Location = new Point(leftMargin + 400, yPos),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(lblReferenciaLabel);

            yPos += 25;

            // ComboBox Medio de Pago
            cmbMedioPago = new ComboBox
            {
                Location = new Point(leftMargin, yPos),
                Size = new Size(170, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cmbMedioPago.Items.AddRange(new string[] { "Efectivo", "Transferencia", "Cheque", "Tarjeta" });
            cmbMedioPago.SelectedIndex = 0;
            this.Controls.Add(cmbMedioPago);

            // TextBox Monto
            txtMonto = new TextBox
            {
                Location = new Point(leftMargin + 200, yPos),
                Size = new Size(170, 25),
                Font = new Font("Segoe UI", 9F),
                TextAlign = HorizontalAlignment.Right
            };
            this.Controls.Add(txtMonto);

            // TextBox Referencia
            txtReferencia = new TextBox
            {
                Location = new Point(leftMargin + 400, yPos),
                Size = new Size(170, 25),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Opcional"
            };
            this.Controls.Add(txtReferencia);

            // Botón Agregar (más pequeño, alineado a la derecha)
            btnAgregar = new Button
            {
                Text = "Agregar",
                Location = new Point(leftMargin + 590, yPos - 2),
                Size = new Size(70, 28),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btnAgregar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnAgregar);

            yPos += 50;

            // DataGridView para mostrar los pagos agregados - ✅ REDUCIDO de 180 a 150
            dgvPagos = new DataGridView
            {
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 150), // ✅ REDUCIDO
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9F)
            };

            dgvPagos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Medio de Pago",
                HeaderText = "Medio de Pago",
                Width = 200
            });

            dgvPagos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Monto",
                HeaderText = "Monto",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "C2",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

            dgvPagos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Referencia",
                HeaderText = "Referencia",
                Width = 270
            });

            this.Controls.Add(dgvPagos);

            yPos += 170; // ✅ AJUSTADO de 200 a 170

            // Botón Eliminar
            btnEliminar = new Button
            {
                Text = "Eliminar",
                Location = new Point(leftMargin, yPos),
                Size = new Size(90, 32),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Enabled = false
            };
            btnEliminar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnEliminar);

            yPos += 45; // ✅ REDUCIDO de 50 a 45

            // Panel para resumen de totales - ✅ REDUCIDO de 60 a 50
            var panelResumen = new Panel
            {
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 50), // ✅ REDUCIDO
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelResumen);

            lblTotal = new Label
            {
                Text = $"Total: {saldoTotal:C2}",
                Location = new Point(15, 15), // ✅ CENTRADO verticalmente
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };
            panelResumen.Controls.Add(lblTotal);

            lblPagado = new Label
            {
                Text = "Pagado: $0.00",
                Location = new Point(250, 15), // ✅ CENTRADO verticalmente
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 167, 69)
            };
            panelResumen.Controls.Add(lblPagado);

            lblSaldo = new Label
            {
                Text = $"Saldo: {saldoTotal:C2}",
                Location = new Point(470, 15), // ✅ CENTRADO verticalmente
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 53, 69)
            };
            panelResumen.Controls.Add(lblSaldo);

            yPos += 60; // ✅ AJUSTADO de 70 a 60

            // Botones de acción - ✅ AHORA SÍ SERÁN VISIBLES
            btnConfirmar = new Button
            {
                Text = "Confirmar",
                Location = new Point(controlWidth - 200 + leftMargin, yPos),
                Size = new Size(100, 32),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Enabled = false
            };
            btnConfirmar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnConfirmar);

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(controlWidth - 90 + leftMargin, yPos),
                Size = new Size(100, 32),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCancelar);

            this.AcceptButton = btnConfirmar;
            this.CancelButton = btnCancelar;
        }

        private void ConfigurarFormulario()
        {
            // Evento para pre-cargar el monto cuando se selecciona un medio de pago
            cmbMedioPago.SelectedIndexChanged += (s, e) =>
            {
                // Solo pre-cargar si el campo está vacío
                if (string.IsNullOrWhiteSpace(txtMonto.Text))
                {
                    decimal saldoRestante = ObtenerSaldoRestante();
                    if (saldoRestante > 0)
                    {
                        txtMonto.Text = saldoRestante.ToString("N2");
                        txtMonto.SelectAll();
                    }
                }
            };

            // Validar solo números y decimales en txtMonto
            txtMonto.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',')
                {
                    e.Handled = true;
                }

                if ((e.KeyChar == '.' || e.KeyChar == ',') && (txtMonto.Text.Contains('.') || txtMonto.Text.Contains(',')))
                {
                    e.Handled = true;
                }
            };

            btnAgregar.Click += BtnAgregar_Click;
            btnEliminar.Click += BtnEliminar_Click;
            btnConfirmar.Click += BtnConfirmar_Click;
            btnCancelar.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            dgvPagos.SelectionChanged += (s, e) =>
            {
                btnEliminar.Enabled = dgvPagos.SelectedRows.Count > 0;
            };
        }

        private void BtnAgregar_Click(object sender, EventArgs e)
        {
            try
            {
                // Validar que se haya ingresado un monto
                if (string.IsNullOrWhiteSpace(txtMonto.Text))
                {
                    MessageBox.Show("Debe ingresar un monto.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMonto.Focus();
                    return;
                }

                // Parsear el monto
                if (!decimal.TryParse(txtMonto.Text, out decimal monto) || monto <= 0)
                {
                    MessageBox.Show("El monto debe ser un valor numérico mayor a cero.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMonto.Focus();
                    txtMonto.SelectAll();
                    return;
                }

                // Calcular el saldo restante
                decimal saldoRestante = ObtenerSaldoRestante();

                // Validar que el monto no exceda el saldo restante
                if (monto > saldoRestante)
                {
                    MessageBox.Show(
                        $"El monto ingresado ({monto:C2}) excede el saldo pendiente ({saldoRestante:C2}).\n\n" +
                        "No se puede ingresar un monto mayor al saldo de la factura.",
                        "Monto excedido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    txtMonto.Text = saldoRestante.ToString("N2");
                    txtMonto.SelectAll();
                    return;
                }

                // Agregar el pago a la lista
                Pagos.Add(new PagoInfo
                {
                    Metodo = cmbMedioPago.SelectedItem.ToString(),
                    Monto = monto,
                    Referencia = txtReferencia.Text.Trim()
                });

                // Actualizar la grilla
                dgvPagos.Rows.Add(cmbMedioPago.SelectedItem.ToString(), monto, txtReferencia.Text.Trim());

                // Limpiar los controles
                txtMonto.Clear();
                txtReferencia.Clear();
                cmbMedioPago.SelectedIndex = 0;

                // Actualizar los labels de resumen
                ActualizarLabelsDeResumen();

                // Enfocar el combo para agregar otro pago si es necesario
                cmbMedioPago.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar pago: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvPagos.SelectedRows.Count == 0)
                    return;

                int rowIndex = dgvPagos.SelectedRows[0].Index;

                // Eliminar de la lista de pagos
                Pagos.RemoveAt(rowIndex);

                // Eliminar de la grilla
                dgvPagos.Rows.RemoveAt(rowIndex);

                // Actualizar los labels de resumen
                ActualizarLabelsDeResumen();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar pago: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            try
            {
                // Validar que haya al menos un pago
                if (Pagos.Count == 0)
                {
                    MessageBox.Show("Debe agregar al menos un pago.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validar que el total de pagos no exceda el saldo
                decimal totalPagado = ObtenerTotalPagado();
                if (totalPagado > saldoTotal)
                {
                    MessageBox.Show(
                        $"El total de pagos ({totalPagado:C2}) excede el saldo de la factura ({saldoTotal:C2}).\n\n" +
                        "Elimine o modifique algunos pagos para continuar.",
                        "Monto excedido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // Validar que el pago cubra el total
                if (totalPagado < saldoTotal)
                {
                    var result = MessageBox.Show(
                        $"El total de pagos ({totalPagado:C2}) es menor al saldo ({saldoTotal:C2}).\n\n" +
                        $"Quedará un saldo pendiente de {(saldoTotal - totalPagado):C2}.\n\n" +
                        "¿Desea continuar?",
                        "Pago parcial",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.No)
                        return;
                }

                // Si llegamos aquí, todo está correcto
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al confirmar: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private decimal ObtenerSaldoRestante()
        {
            decimal totalPagado = ObtenerTotalPagado();
            return saldoTotal - totalPagado;
        }

        private decimal ObtenerTotalPagado()
        {
            return Pagos.Sum(p => p.Monto);
        }

        private void ActualizarLabelsDeResumen()
        {
            decimal totalPagado = ObtenerTotalPagado();
            decimal saldoRestante = ObtenerSaldoRestante();

            lblPagado.Text = $"Pagado: {totalPagado:C2}";
            lblSaldo.Text = $"Saldo: {saldoRestante:C2}";

            // Cambiar color según el estado
            if (saldoRestante == 0)
            {
                lblSaldo.ForeColor = Color.FromArgb(40, 167, 69); // Verde
                btnConfirmar.Enabled = true;
            }
            else if (totalPagado > 0)
            {
                lblSaldo.ForeColor = Color.FromArgb(255, 193, 7); // Amarillo
                btnConfirmar.Enabled = true;
            }
            else
            {
                lblSaldo.ForeColor = Color.FromArgb(220, 53, 69); // Rojo
                btnConfirmar.Enabled = false;
            }
        }
    }
}