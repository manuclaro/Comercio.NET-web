using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public class ModificarMedioPagoForm : Form
    {
        private DataGridView dgvPagos;
        private ComboBox cboMedioPago;
        private NumericUpDown numImporte;
        private TextBox txtObservaciones;
        private Button btnAgregar;
        private Button btnEliminar;
        private Button btnGuardar;
        private Button btnCancelar;
        private Label lblTotal;
        private Label lblAsignado;
        private Label lblPendiente;
        private ProgressBar progressPago;

        // ✅ NUEVO: Agregar campo para guardar los datos de la factura
        private DatosFacturaModificar _datosFactura;

        private decimal importeTotalFactura;
        private List<DetallePagoModificar> pagosActuales;

        public List<DetallePagoModificar> MediosPagoActualizados { get; private set; }

        public ModificarMedioPagoForm(DatosFacturaModificar datosFactura)
        {
            // ✅ NUEVO: Guardar referencia a datosFactura
            _datosFactura = datosFactura;

            importeTotalFactura = datosFactura.ImporteTotal;
            pagosActuales = new List<DetallePagoModificar>(); // ✅ CAMBIO: Arrancar con lista vacía

            InitializeComponent();
            ConfigurarFormulario(datosFactura);
            CargarDatosIniciales();
        }

        private void InitializeComponent()
        {
            this.Text = "Modificar Medios de Pago";
            this.Size = new Size(700, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            int y = 20;
            int margin = 20;

            // Título
            var lblTitulo = new Label
            {
                Text = "💳 Modificar Medios de Pago",
                Location = new Point(margin, y),
                Size = new Size(650, 35),
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 193, 7)
            };
            this.Controls.Add(lblTitulo);
            y += 45;

            // Panel de información
            var panelInfo = new Panel
            {
                Location = new Point(margin, y),
                Size = new Size(650, 60),
                BackColor = Color.FromArgb(255, 243, 205),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblInfo = new Label
            {
                Text = "Puede modificar los medios de pago asignados a esta factura.\n" +
                       "El total asignado debe coincidir con el importe total de la factura.",
                Location = new Point(10, 10),
                Size = new Size(630, 40),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(133, 100, 4)
            };
            panelInfo.Controls.Add(lblInfo);
            this.Controls.Add(panelInfo);
            y += 70;

            // Totales
            lblTotal = new Label
            {
                Text = "Total Factura: $0,00",
                Location = new Point(margin, y),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblTotal);

            lblAsignado = new Label
            {
                Text = "Asignado: $0,00",
                Location = new Point(margin + 210, y),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10F)
            };
            this.Controls.Add(lblAsignado);

            lblPendiente = new Label
            {
                Text = "Pendiente: $0,00",
                Location = new Point(margin + 420, y),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Red
            };
            this.Controls.Add(lblPendiente);
            y += 35;

            // Progress bar
            progressPago = new ProgressBar
            {
                Location = new Point(margin, y),
                Size = new Size(650, 25),
                Minimum = 0,
                Maximum = 100
            };
            this.Controls.Add(progressPago);
            y += 35;

            // Agregar pago
            var lblMedioPago = new Label
            {
                Text = "Medio de Pago:",
                Location = new Point(margin, y),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblMedioPago);

            cboMedioPago = new ComboBox
            {
                Location = new Point(margin + 120, y),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cboMedioPago.Items.AddRange(new object[] { "Efectivo", "DNI", "MercadoPago", "Transferencia", "Otro" });
            cboMedioPago.SelectedIndex = 0;
            this.Controls.Add(cboMedioPago);

            var lblImporte = new Label
            {
                Text = "Importe:",
                Location = new Point(margin + 285, y),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblImporte);

            numImporte = new NumericUpDown
            {
                Location = new Point(margin + 345, y),
                Size = new Size(120, 25),
                DecimalPlaces = 2,
                Maximum = 999999,
                Minimum = 0,
                Value = 0,
                Font = new Font("Segoe UI", 9F),
                TextAlign = HorizontalAlignment.Right
            };
            this.Controls.Add(numImporte);

            btnAgregar = new Button
            {
                Text = "➕ Agregar",
                Location = new Point(margin + 480, y),
                Size = new Size(90, 28),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAgregar.FlatAppearance.BorderSize = 0;
            btnAgregar.Click += BtnAgregar_Click;
            this.Controls.Add(btnAgregar);
            y += 40;

            // Observaciones
            var lblObservaciones = new Label
            {
                Text = "Observaciones:",
                Location = new Point(margin, y),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblObservaciones);

            txtObservaciones = new TextBox
            {
                Location = new Point(margin + 120, y),
                Size = new Size(550, 25),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Opcional..."
            };
            this.Controls.Add(txtObservaciones);
            y += 40;

            // DataGridView
            dgvPagos = new DataGridView
            {
                Location = new Point(margin, y),
                Size = new Size(650, 150),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 30
            };

            dgvPagos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MedioPago",
                HeaderText = "Medio de Pago",
                DataPropertyName = "MedioPago",
                Width = 150
            });

            dgvPagos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Importe",
                HeaderText = "Importe",
                DataPropertyName = "Importe",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2", Alignment = DataGridViewContentAlignment.MiddleRight },
                Width = 120
            });

            dgvPagos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Observaciones",
                HeaderText = "Observaciones",
                DataPropertyName = "Observaciones",
                Width = 360
            });

            this.Controls.Add(dgvPagos);
            y += 160;

            // Botón eliminar
            btnEliminar = new Button
            {
                Text = "🗑️ Eliminar Seleccionado",
                Location = new Point(margin, y),
                Size = new Size(160, 30),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnEliminar.FlatAppearance.BorderSize = 0;
            btnEliminar.Click += BtnEliminar_Click;
            this.Controls.Add(btnEliminar);

            dgvPagos.SelectionChanged += (s, e) => { btnEliminar.Enabled = dgvPagos.SelectedRows.Count > 0; };

            // Botones finales
            btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(470, y),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnCancelar);

            btnGuardar = new Button
            {
                Text = "✅ Guardar",
                Location = new Point(570, y),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;
            this.Controls.Add(btnGuardar);
        }

        private void ConfigurarFormulario(DatosFacturaModificar datosFactura)
        {
            lblTotal.Text = $"Total Factura: {datosFactura.ImporteTotal:C2}";
        }

        // ✅ CORREGIDO: Método simplificado y sin errores
        private void CargarDatosIniciales()
        {
            try
            {
                // ✅ Cargar grilla con los pagos actuales (vacía al inicio)
                dgvPagos.DataSource = null;
                dgvPagos.DataSource = new BindingSource { DataSource = pagosActuales };

                // ✅ Precargar el campo "Importe" con el total disponible
                decimal totalAsignado = pagosActuales.Sum(p => p.Importe);
                decimal pendiente = importeTotalFactura - totalAsignado;
                numImporte.Value = pendiente > 0 ? pendiente : 0;

                // ✅ Actualizar resumen
                ActualizarTotales();

                System.Diagnostics.Debug.WriteLine($"[MODIFICAR PAGO] Grilla actualizada - Pagos: {pagosActuales.Count}, Pendiente: {pendiente:C2}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando datos iniciales: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAgregar_Click(object sender, EventArgs e)
        {
            if (numImporte.Value <= 0)
            {
                MessageBox.Show("El importe debe ser mayor a cero.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal totalAsignado = pagosActuales.Sum(p => p.Importe);
            decimal pendiente = importeTotalFactura - totalAsignado;

            if (numImporte.Value > pendiente)
            {
                MessageBox.Show($"El importe excede el monto pendiente de ${pendiente:N2}.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            pagosActuales.Add(new DetallePagoModificar
            {
                MedioPago = cboMedioPago.SelectedItem.ToString(),
                Importe = numImporte.Value,
                Observaciones = txtObservaciones.Text.Trim()
            });

            txtObservaciones.Clear();
            CargarDatosIniciales(); // ✅ Esto actualiza la grilla y recalcula el pendiente
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (dgvPagos.SelectedRows.Count == 0) return;

            int index = dgvPagos.SelectedRows[0].Index;
            pagosActuales.RemoveAt(index);
            CargarDatosIniciales();
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            decimal totalAsignado = pagosActuales.Sum(p => p.Importe);

            if (totalAsignado != importeTotalFactura)
            {
                MessageBox.Show($"El total asignado (${totalAsignado:N2}) no coincide con el total de la factura (${importeTotalFactura:N2}).",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (pagosActuales.Count == 0)
            {
                MessageBox.Show("Debe agregar al menos un medio de pago.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MediosPagoActualizados = pagosActuales;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ActualizarTotales()
        {
            decimal totalAsignado = pagosActuales.Sum(p => p.Importe);
            decimal pendiente = importeTotalFactura - totalAsignado;

            lblAsignado.Text = $"Asignado: {totalAsignado:C2}";
            lblPendiente.Text = $"Pendiente: {pendiente:C2}";
            lblPendiente.ForeColor = pendiente == 0 ? Color.Green : Color.Red;

            int porcentaje = importeTotalFactura > 0
                ? (int)((totalAsignado / importeTotalFactura) * 100)
                : 0;
            progressPago.Value = Math.Min(porcentaje, 100);

            if (pendiente > 0)
            {
                numImporte.Maximum = pendiente;
            }
        }
    }
}