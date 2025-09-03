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
        }

        private void InitializeComponent()
        {
            this.txtCantidad = new TextBox();
            this.btnAceptar = new Button();
            this.btnCancelar = new Button();
            this.lblCantidad = new Label();
            this.SuspendLayout();

            // lblCantidad
            this.lblCantidad.AutoSize = true;
            this.lblCantidad.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            this.lblCantidad.Location = new Point(12, 15);
            this.lblCantidad.Name = "lblCantidad";
            this.lblCantidad.Size = new Size(87, 21);
            this.lblCantidad.Text = "Cantidad:";

            // txtCantidad
            this.txtCantidad.Font = new Font("Segoe UI", 14F);
            this.txtCantidad.Location = new Point(105, 12);
            this.txtCantidad.Name = "txtCantidad";
            this.txtCantidad.Size = new Size(100, 32);
            this.txtCantidad.Text = "1";
            this.txtCantidad.TextAlign = HorizontalAlignment.Center;

            // btnAceptar
            this.btnAceptar.BackColor = Color.FromArgb(0, 120, 215);
            this.btnAceptar.FlatStyle = FlatStyle.Flat;
            this.btnAceptar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnAceptar.ForeColor = Color.White;
            this.btnAceptar.Location = new Point(12, 60);
            this.btnAceptar.Name = "btnAceptar";
            this.btnAceptar.Size = new Size(90, 35);
            this.btnAceptar.Text = "Aceptar";
            this.btnAceptar.UseVisualStyleBackColor = false;
            this.btnAceptar.Click += btnAceptar_Click;

            // btnCancelar
            this.btnCancelar.BackColor = Color.FromArgb(220, 53, 69);
            this.btnCancelar.FlatStyle = FlatStyle.Flat;
            this.btnCancelar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnCancelar.ForeColor = Color.White;
            this.btnCancelar.Location = new Point(115, 60);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Size = new Size(90, 35);
            this.btnCancelar.Text = "Cancelar";
            this.btnCancelar.UseVisualStyleBackColor = false;
            this.btnCancelar.Click += btnCancelar_Click;

            // ModalCantidadForm
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(220, 110);
            this.Controls.Add(this.btnCancelar);
            this.Controls.Add(this.btnAceptar);
            this.Controls.Add(this.txtCantidad);
            this.Controls.Add(this.lblCantidad);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ModalCantidadForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Ingrese Cantidad";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtCantidad.Text, out int cantidad) && cantidad > 0)
            {
                CantidadSeleccionada = cantidad;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Ingrese una cantidad válida mayor a 0.");
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}