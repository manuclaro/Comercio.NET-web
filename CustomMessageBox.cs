using System;
using System.Drawing;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class CustomMessageBox : Form
    {
        public string Titulo { get; set; }
        public string Mensaje { get; set; }

        public CustomMessageBox(string mensaje, string titulo = "Mensaje")
        {
            Mensaje = mensaje;
            Titulo = titulo;
            InitializeComponent();
            ConfigurarFormulario();
        }

        private void ConfigurarFormulario()
        {
            this.Text = Titulo;
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            var lblMensaje = new Label
            {
                Text = Mensaje,
                Location = new Point(20, 20),
                Size = new Size(340, 80),
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var btnSi = new Button
            {
                Text = "Sí",
                Location = new Point(200, 120),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Yes
            };

            var btnNo = new Button
            {
                Text = "No",
                Location = new Point(290, 120),
                Size = new Size(80, 30),
                DialogResult = DialogResult.No
            };

            this.Controls.AddRange(new Control[] { lblMensaje, btnSi, btnNo });
            this.AcceptButton = btnSi;
            this.CancelButton = btnNo;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }
    }
}