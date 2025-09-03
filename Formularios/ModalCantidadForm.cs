using System;
using System.Drawing;
using System.Windows.Forms;

namespace Comercio.NET
{
    public partial class ModalCantidadForm : Form
    {
        public int CantidadSeleccionada { get; private set; } = 1;
        
        private TextBox txtCantidad;
        private Button btnAceptar;
        private Button btnCancelar;
        private Label lblCantidad;

        public ModalCantidadForm()
        {
            InitializeComponent();
            ConfigurarEventos();
        }

        private void InitializeComponent()
        {
            txtCantidad = new TextBox();
            btnAceptar = new Button();
            btnCancelar = new Button();
            lblCantidad = new Label();
            SuspendLayout();
            // 
            // txtCantidad
            // 
            txtCantidad.Font = new Font("Segoe UI", 14F);
            txtCantidad.Location = new Point(105, 12);
            txtCantidad.MaxLength = 2;
            txtCantidad.Name = "txtCantidad";
            txtCantidad.Size = new Size(100, 32);
            txtCantidad.TabIndex = 0;
            txtCantidad.Text = "1";
            txtCantidad.TextAlign = HorizontalAlignment.Center;
            // 
            // btnAceptar
            // 
            btnAceptar.BackColor = Color.FromArgb(0, 120, 215);
            btnAceptar.FlatStyle = FlatStyle.Flat;
            btnAceptar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnAceptar.ForeColor = Color.White;
            btnAceptar.Location = new Point(12, 60);
            btnAceptar.Name = "btnAceptar";
            btnAceptar.Size = new Size(90, 35);
            btnAceptar.TabIndex = 1;
            btnAceptar.Text = "Aceptar";
            btnAceptar.UseVisualStyleBackColor = false;
            btnAceptar.Click += btnAceptar_Click;
            // 
            // btnCancelar
            // 
            btnCancelar.BackColor = Color.FromArgb(220, 53, 69);
            btnCancelar.FlatStyle = FlatStyle.Flat;
            btnCancelar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCancelar.ForeColor = Color.White;
            btnCancelar.Location = new Point(115, 60);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(90, 35);
            btnCancelar.TabIndex = 0;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = false;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // lblCantidad
            // 
            lblCantidad.AutoSize = true;
            lblCantidad.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblCantidad.Location = new Point(12, 15);
            lblCantidad.Name = "lblCantidad";
            lblCantidad.Size = new Size(83, 21);
            lblCantidad.TabIndex = 3;
            lblCantidad.Text = "Cantidad:";
            // 
            // ModalCantidadForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(220, 110);
            Controls.Add(btnCancelar);
            Controls.Add(btnAceptar);
            Controls.Add(txtCantidad);
            Controls.Add(lblCantidad);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ModalCantidadForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Ingrese Cantidad";
            ResumeLayout(false);
            PerformLayout();
        }

        private void ConfigurarEventos()
        {
            // EVENTO PARA HACER FOCO AL MOSTRAR EL FORMULARIO (más confiable que Load)
            this.Shown += (s, e) =>
            {
                txtCantidad.Focus();
                txtCantidad.SelectAll(); // Seleccionar todo el texto
            };

            // TAMBIÉN AGREGAR Load como respaldo
            this.Load += (s, e) =>
            {
                this.ActiveControl = txtCantidad;
            };

            // EVENTO KEYPRESS - SOLO NÚMEROS
            txtCantidad.KeyPress += (s, e) =>
            {
                // Permitir teclas de control (backspace, delete, etc.)
                if (char.IsControl(e.KeyChar))
                    return;

                // Solo permitir dígitos
                if (!char.IsDigit(e.KeyChar))
                {
                    e.Handled = true; // Cancelar la tecla presionada
                    return;
                }
            };

            // EVENTO KEYDOWN - ENTER Y ESCAPE
            txtCantidad.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    btnAceptar_Click(null, null); // Simular click en Aceptar
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    btnCancelar_Click(null, null); // Simular click en Cancelar
                }
            };

            // EVENTO PARA VALIDAR QUE NO ESTÉ VACÍO
            txtCantidad.TextChanged += (s, e) =>
            {
                // Si está vacío, deshabilitar el botón Aceptar
                btnAceptar.Enabled = !string.IsNullOrWhiteSpace(txtCantidad.Text);
            };
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            // Validar que no esté vacío
            if (string.IsNullOrWhiteSpace(txtCantidad.Text))
            {
                MessageBox.Show("Ingrese una cantidad válida.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCantidad.Focus();
                return;
            }

            // Validar que sea un número válido mayor a 0
            if (int.TryParse(txtCantidad.Text, out int cantidad) && cantidad > 0 && cantidad <= 99)
            {
                CantidadSeleccionada = cantidad;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Ingrese una cantidad válida entre 1 y 99.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCantidad.Focus();
                txtCantidad.SelectAll();
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}