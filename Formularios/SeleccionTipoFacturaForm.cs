using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;

namespace Comercio.NET.Formularios
{
    public class SeleccionTipoFacturaForm : Form
    {
        private ComboBox cboTipoFactura;
        private TextBox txtCuit;
        private Label lblCuit;
        private Button btnGenerar;
        private Button btnCancelar;
        private Label lblImporteTotal;

        public string TipoFacturaSeleccionado { get; private set; }
        public string CuitCliente { get; private set; }

        public SeleccionTipoFacturaForm(dynamic datosRemito)
        {
            InitializeComponent();
            ConfigurarFormulario(datosRemito);
        }

        private void InitializeComponent()
        {
            this.Text = "Generar Factura AFIP";
            this.Size = new Size(500, 350);
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
                Text = "📄 Generar Factura AFIP",
                Location = new Point(margin, y),
                Size = new Size(450, 35),
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 123, 255)
            };
            this.Controls.Add(lblTitulo);
            y += 50;

            // Panel informativo
            var panelInfo = new Panel
            {
                Location = new Point(margin, y),
                Size = new Size(450, 60),
                BackColor = Color.FromArgb(217, 237, 247),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblInfo = new Label
            {
                Text = "Se generará una factura electrónica en AFIP asociada\n" +
                       "a este remito. El remito se actualizará con el CAE.",
                Location = new Point(10, 10),
                Size = new Size(430, 40),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(49, 112, 143)
            };
            panelInfo.Controls.Add(lblInfo);
            this.Controls.Add(panelInfo);
            y += 70;

            // Importe total
            lblImporteTotal = new Label
            {
                Text = "Importe: $0,00",
                Location = new Point(margin, y),
                Size = new Size(450, 30),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 167, 69)
            };
            this.Controls.Add(lblImporteTotal);
            y += 40;

            // Tipo de factura
            var lblTipoFactura = new Label
            {
                Text = "Tipo de Factura:",
                Location = new Point(margin, y),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblTipoFactura);

            cboTipoFactura = new ComboBox
            {
                Location = new Point(margin + 130, y),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            cboTipoFactura.SelectedIndexChanged += CboTipoFactura_SelectedIndexChanged;
            this.Controls.Add(cboTipoFactura);
            y += 40;

            // CUIT (solo para Factura A)
            lblCuit = new Label
            {
                Text = "CUIT Cliente:",
                Location = new Point(margin, y),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Visible = false
            };
            this.Controls.Add(lblCuit);

            txtCuit = new TextBox
            {
                Location = new Point(margin + 130, y),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "20-12345678-9",
                Visible = false
            };
            this.Controls.Add(txtCuit);
            y += 50;

            // Botones
            btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(260, y),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnCancelar);

            btnGenerar = new Button
            {
                Text = "Generar Factura",
                Location = new Point(370, y),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGenerar.FlatAppearance.BorderSize = 0;
            btnGenerar.Click += BtnGenerar_Click;
            this.Controls.Add(btnGenerar);
        }

        private void ConfigurarFormulario(dynamic datosRemito)
        {
            lblImporteTotal.Text = $"Importe: {datosRemito.ImporteTotal:C2}";

            // Cargar tipos de factura según configuración
            CargarTiposFacturaSegunConfiguracion();
        }

        private void CargarTiposFacturaSegunConfiguracion()
        {
            try
            {
                var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string ambienteActivo = config["AFIP:AmbienteActivo"] ?? "Testing";
                string condicionIVA = config[$"AFIP:{ambienteActivo}:CondicionIVA"] ?? "Monotributo";

                cboTipoFactura.Items.Clear();

                if (condicionIVA.Equals("Monotributo", StringComparison.OrdinalIgnoreCase))
                {
                    cboTipoFactura.Items.Add("Factura C");
                    cboTipoFactura.SelectedIndex = 0;
                    cboTipoFactura.Enabled = false;
                }
                else
                {
                    cboTipoFactura.Items.Add("Factura A");
                    cboTipoFactura.Items.Add("Factura B");
                    cboTipoFactura.SelectedIndex = 1; // Por defecto B
                    cboTipoFactura.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando configuración: {ex.Message}");
                cboTipoFactura.Items.Clear();
                cboTipoFactura.Items.AddRange(new object[] { "Factura A", "Factura B", "Factura C" });
                cboTipoFactura.SelectedIndex = 2;
            }
        }

        private void CboTipoFactura_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool requiereCuit = cboTipoFactura.SelectedItem?.ToString() == "Factura A";
            lblCuit.Visible = requiereCuit;
            txtCuit.Visible = requiereCuit;
        }

        private void BtnGenerar_Click(object sender, EventArgs e)
        {
            string tipoFactura = cboTipoFactura.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(tipoFactura))
            {
                MessageBox.Show("Debe seleccionar un tipo de factura.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Extraer solo la letra (A, B o C)
            TipoFacturaSeleccionado = tipoFactura.Replace("Factura ", "").Trim();

            // Validar CUIT para Factura A
            if (TipoFacturaSeleccionado == "A")
            {
                CuitCliente = txtCuit.Text.Trim();

                if (string.IsNullOrEmpty(CuitCliente))
                {
                    MessageBox.Show("Debe ingresar el CUIT del cliente para Factura A.",
                        "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCuit.Focus();
                    return;
                }
            }
            else
            {
                CuitCliente = "";
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}