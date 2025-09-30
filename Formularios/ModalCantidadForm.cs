using System;
using System.Drawing;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class ModalCantidadForm : Form
    {
        public int CantidadSeleccionada { get; private set; } = 1;
        
        // MODIFICADO: Cambiar a propiedad pública con setter
        public int CantidadInicial { get; set; } = 1;

        private TextBox txtCantidad; // Declarar como campo para acceder desde ConfigurarFormulario


        public ModalCantidadForm()
        {
            InitializeComponent();
            ConfigurarFormulario();
        }
        public string DescripcionProducto
        {
            get => lblDescripcion.Text;
            set => lblDescripcion.Text = value;
        }
        private void ConfigurarFormulario()
        {
            this.Text = "Cantidad Personalizada";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;
            this.Size = new Size(300, 200);
            this.KeyPreview = true;

            // Crear controles
            var lblTitulo = new Label
            {
                Text = "Ingrese la cantidad:",
                Location = new Point(20, 40),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100)
            };

            txtCantidad = new TextBox
            {
                Name = "txtCantidad",
                Location = new Point(20, 60),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 12F),
                TextAlign = HorizontalAlignment.Center,
                Text = CantidadInicial.ToString() // MODIFICADO: Usar CantidadInicial
            };

            var btnAceptar = new Button
            {
                Text = "Aceptar",
                Location = new Point(50, 100),
                Size = new Size(80, 35),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnAceptar.FlatAppearance.BorderSize = 0;

            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(140, 100),
                Size = new Size(80, 35),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnCancelar.FlatAppearance.BorderSize = 0;

            // Agregar controles al formulario
            this.Controls.AddRange(new Control[] { lblTitulo, txtCantidad, btnAceptar, btnCancelar });

            // Configurar eventos
            txtCantidad.KeyPress += (s, e) =>
            {
                // Solo permitir números
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
                
                // Limitar a 3 dígitos
                if (txtCantidad.Text.Length >= 3 && !char.IsControl(e.KeyChar))
                {
                    e.Handled = true;
                }
            };

            txtCantidad.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    btnAceptar.PerformClick();
                }
            };

            btnAceptar.Click += (s, e) =>
            {
                if (int.TryParse(txtCantidad.Text, out int cantidad) && cantidad > 0)
                {
                    CantidadSeleccionada = cantidad;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Ingrese una cantidad válida (mayor a 0).", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCantidad.Focus();
                    txtCantidad.SelectAll();
                }
            };

            btnCancelar.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    btnCancelar.PerformClick();
                }
            };

            this.AcceptButton = btnAceptar;
            this.CancelButton = btnCancelar;

            // MODIFICADO: Actualizar el valor inicial del TextBox cuando se carga el formulario
            this.Load += (s, e) =>
            {
                txtCantidad.Text = CantidadInicial.ToString();
                txtCantidad.Focus();
                txtCantidad.SelectAll();
            };
        }

        private void InitializeComponent()
        {
            lblDescripcion = new Label();
            SuspendLayout();
            // 
            // lblDescripcion
            // 
            lblDescripcion.AutoSize = true;
            lblDescripcion.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblDescripcion.Location = new Point(21, 9);
            lblDescripcion.Name = "lblDescripcion";
            lblDescripcion.Size = new Size(0, 17);
            lblDescripcion.TabIndex = 0;
            // 
            // ModalCantidadForm
            // 
            ClientSize = new Size(284, 261);
            Controls.Add(lblDescripcion);
            Name = "ModalCantidadForm";
            ResumeLayout(false);
            PerformLayout();
        }
        private Label lblDescripcion;
    }
}