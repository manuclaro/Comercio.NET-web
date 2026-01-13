using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Comercio.NET.Services;

namespace Comercio.NET.Formularios
{
    public partial class PagoProveedorRapidoForm : Form
    {
        private ComboBox cboProveedor;
        private TextBox txtMonto;
        private TextBox txtObservaciones;
        private Button btnConfirmar;
        private Button btnCancelar;
        private Label lblTotal;
        private Label lblTotalPendiente;

        public decimal Monto { get; private set; }
        public int ProveedorIdSeleccionado { get; private set; } // ✅ NUEVO
        public string ProveedorSeleccionado { get; private set; }
        public string Observaciones { get; private set; }
        public bool Confirmado { get; private set; }

        public PagoProveedorRapidoForm()
        {
            InitializeComponent();
            ConfigurarFormulario();
            CrearControles();
            CargarProveedores();
        }

        private void ConfigurarFormulario()
        {
            this.Text = "💳 Pago Rápido a Proveedor";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.WhiteSmoke;
            this.Font = new Font("Segoe UI", 10F);
        }

        private void CrearControles()
        {
            int margin = 20;
            int labelWidth = 150;
            int controlWidth = 300;
            int currentY = margin + 10;

            // Panel de título
            var panelTitulo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(0, 150, 136)
            };
            var lblTitulo = new Label
            {
                Text = "💳 Registrar Pago a Proveedor",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelTitulo.Controls.Add(lblTitulo);
            this.Controls.Add(panelTitulo);
            currentY += 70;

            // Proveedor
            var lblProveedor = new Label
            {
                Text = "Proveedor:",
                Location = new Point(margin, currentY),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblProveedor);

            cboProveedor = new ComboBox
            {
                Location = new Point(margin + labelWidth, currentY),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            cboProveedor.SelectedIndexChanged += CboProveedor_SelectedIndexChanged;
            this.Controls.Add(cboProveedor);
            currentY += 40;

            // Total pendiente del proveedor
            lblTotalPendiente = new Label
            {
                Text = "Pendiente: -",
                Location = new Point(margin + labelWidth, currentY),
                Size = new Size(controlWidth, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(220, 53, 69)
            };
            this.Controls.Add(lblTotalPendiente);
            currentY += 35;

            // Monto
            var lblMonto = new Label
            {
                Text = "Monto a pagar:",
                Location = new Point(margin, currentY),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblMonto);

            txtMonto = new TextBox
            {
                Location = new Point(margin + labelWidth, currentY),
                Size = new Size(controlWidth, 30),
                Font = new Font("Segoe UI", 12F),
                PlaceholderText = "Ingrese el monto"
            };
            txtMonto.KeyPress += TxtMonto_KeyPress;
            txtMonto.TextChanged += TxtMonto_TextChanged;
            this.Controls.Add(txtMonto);
            currentY += 50;

            // Observaciones
            var lblObservaciones = new Label
            {
                Text = "Observaciones:",
                Location = new Point(margin, currentY),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblObservaciones);

            txtObservaciones = new TextBox
            {
                Location = new Point(margin + labelWidth, currentY),
                Size = new Size(controlWidth, 60),
                Font = new Font("Segoe UI", 10F),
                Multiline = true,
                PlaceholderText = "Ej: Pago parcial factura #123"
            };
            this.Controls.Add(txtObservaciones);
            currentY += 80;

            // Panel de botones
            var panelBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Size = new Size(100, 35),
                Location = new Point(this.Width - 230, 10),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += BtnCancelar_Click;
            panelBotones.Controls.Add(btnCancelar);

            btnConfirmar = new Button
            {
                Text = "Confirmar Pago",
                Size = new Size(120, 35),
                Location = new Point(this.Width - 120, 10),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnConfirmar.FlatAppearance.BorderSize = 0;
            btnConfirmar.Click += BtnConfirmar_Click;
            panelBotones.Controls.Add(btnConfirmar);

            this.Controls.Add(panelBotones);
        }

        private void CargarProveedores()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    // ✅ CORREGIDO: Cargar desde tabla Proveedores
                    var query = @"
                SELECT Id, Nombre 
                FROM Proveedores 
                WHERE Activo = 1
                ORDER BY Nombre";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        connection.Open();

                        cboProveedor.Items.Clear();
                        cboProveedor.Items.Add(new { Id = -1, Nombre = "-- Seleccione proveedor --" });

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                string nombre = reader.GetString(1);

                                cboProveedor.Items.Add(new
                                {
                                    Id = id,
                                    Nombre = nombre
                                });
                            }
                        }
                    }
                }

                cboProveedor.DisplayMember = "Nombre";
                cboProveedor.ValueMember = "Id";
                cboProveedor.SelectedIndex = 0;

                System.Diagnostics.Debug.WriteLine($"✅ Proveedores cargados: {cboProveedor.Items.Count - 1}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar proveedores: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Fallback: si no existe la tabla, mostrar mensaje informativo
                cboProveedor.Items.Clear();
                cboProveedor.Items.Add(new { Id = -1, Nombre = "⚠️ Tabla Proveedores no disponible" });
                cboProveedor.SelectedIndex = 0;
                cboProveedor.Enabled = false;
            }
        }

        // ✅ MODIFICADO: Actualizar para usar el Id del proveedor
        private void CboProveedor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboProveedor.SelectedIndex > 0)
            {
                dynamic proveedorSeleccionado = cboProveedor.SelectedItem;
                int idProveedor = proveedorSeleccionado.Id;
                string nombreProveedor = proveedorSeleccionado.Nombre;

                CargarSaldoPendiente(idProveedor, nombreProveedor);
            }
            else
            {
                lblTotalPendiente.Text = "Pendiente: -";
            }

            ValidarFormulario();
        }

        // ✅ MODIFICADO: Usar Id en lugar de nombre para consultar saldo
        private void CargarSaldoPendiente(int idProveedor, string nombreProveedor)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    // Buscar saldo en tabla de cuenta corriente de proveedores
                    var query = @"
                SELECT ISNULL(SUM(Debe - Haber), 0) as Saldo
                FROM CtaCteProveedores
                WHERE IdProveedor = @idProveedor";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@idProveedor", idProveedor);
                        connection.Open();

                        var saldo = cmd.ExecuteScalar();
                        if (saldo != null && saldo != DBNull.Value)
                        {
                            decimal saldoPendiente = Convert.ToDecimal(saldo);
                            lblTotalPendiente.Text = $"Pendiente: {saldoPendiente:C2}";
                            lblTotalPendiente.ForeColor = saldoPendiente > 0
                                ? Color.FromArgb(220, 53, 69)
                                : Color.FromArgb(0, 150, 136);
                        }
                    }
                }
            }
            catch
            {
                // Si no existe la tabla, mostrar mensaje informativo
                lblTotalPendiente.Text = "Pendiente: No disponible";
                lblTotalPendiente.ForeColor = Color.Gray;
            }
        }

        private void TxtMonto_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Permitir solo números, punto decimal, coma y backspace
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && 
                e.KeyChar != '.' && e.KeyChar != ',')
            {
                e.Handled = true;
            }

            // Permitir solo un separador decimal
            if ((e.KeyChar == '.' || e.KeyChar == ',') && 
                (txtMonto.Text.Contains('.') || txtMonto.Text.Contains(',')))
            {
                e.Handled = true;
            }
        }

        private void TxtMonto_TextChanged(object sender, EventArgs e)
        {
            ValidarFormulario();
        }

        private void ValidarFormulario()
        {
            bool valido = cboProveedor.SelectedIndex > 0 && 
                         !string.IsNullOrWhiteSpace(txtMonto.Text) &&
                         decimal.TryParse(txtMonto.Text.Replace(',', '.'), out decimal monto) &&
                         monto > 0;

            btnConfirmar.Enabled = valido;
        }

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtMonto.Text.Replace(',', '.'), out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Ingrese un monto válido mayor a cero.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMonto.Focus();
                return;
            }

            if (cboProveedor.SelectedIndex <= 0)
            {
                MessageBox.Show("Seleccione un proveedor.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboProveedor.Focus();
                return;
            }

            dynamic proveedorSeleccionado = cboProveedor.SelectedItem;
            int idProveedor = proveedorSeleccionado.Id;
            string nombreProveedor = proveedorSeleccionado.Nombre;

            var resultado = MessageBox.Show(
                $"¿Confirmar pago de {monto:C2} a {nombreProveedor}?",
                "Confirmar Pago",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (resultado == DialogResult.Yes)
            {
                Monto = monto;
                ProveedorIdSeleccionado = idProveedor; // ✅ NUEVO: Guardar Id
                ProveedorSeleccionado = nombreProveedor;
                Observaciones = txtObservaciones.Text.Trim();
                Confirmado = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            Confirmado = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(500, 400);
            this.Name = "PagoProveedorRapidoForm";
            this.ResumeLayout(false);
        }
    }
}