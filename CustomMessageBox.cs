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
            
            // NUEVO: Deshabilitar el cierre con Alt+F4 o X para forzar decisión
            this.ControlBox = true; // Mantener la X visible pero interceptaremos el cierre

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
                DialogResult = DialogResult.Yes,
                BackColor = Color.FromArgb(76, 175, 80), // Verde
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var btnNo = new Button
            {
                Text = "No",
                Location = new Point(290, 120),
                Size = new Size(80, 30),
                DialogResult = DialogResult.No,
                BackColor = Color.FromArgb(244, 67, 54), // Rojo
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            this.Controls.AddRange(new Control[] { lblMensaje, btnSi, btnNo });
            
            // MODIFICADO: Eliminar AcceptButton y CancelButton para que ningún botón tenga foco por defecto
            // this.AcceptButton = btnSi;  // COMENTADO: Eliminar foco por defecto
            // this.CancelButton = btnNo;  // COMENTADO: Eliminar foco por defecto
            
            // NUEVO: Interceptar el evento FormClosing para evitar que se cierre sin decisión
            this.FormClosing += CustomMessageBox_FormClosing;
            
            // NUEVO: Configurar el label para que reciba el foco inicial
            lblMensaje.TabStop = true;
            lblMensaje.TabIndex = 0;
            btnSi.TabIndex = 1;
            btnNo.TabIndex = 2;
            
            // NUEVO: Asegurar que el label reciba el foco al mostrar el formulario
            this.Shown += (s, e) =>
            {
                lblMensaje.Focus();
                // ALTERNATIVA: También se puede enfocar en el formulario mismo
                // this.Focus();
            };
        }

        // NUEVO: Interceptar el cierre del formulario para evitar que se cierre sin una decisión
        private void CustomMessageBox_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Solo permitir el cierre si se seleccionó un DialogResult válido
            if (this.DialogResult == DialogResult.None)
            {
                e.Cancel = true; // Cancelar el cierre
                
                // OPCIONAL: Mostrar un mensaje recordatorio
                System.Media.SystemSounds.Exclamation.Play();
                
                // OPCIONAL: Hacer parpadear los botones para llamar la atención
                ParpadearBotones();
            }
        }

        // NUEVO: Método para hacer parpadear los botones y llamar la atención del usuario
        private void ParpadearBotones()
        {
            var btnSi = this.Controls[1] as Button;
            var btnNo = this.Controls[2] as Button;
            
            if (btnSi != null && btnNo != null)
            {
                var timer = new System.Windows.Forms.Timer(); // CORREGIDO: Usar la clase específica
                int parpadeos = 0;
                var colorOriginalSi = btnSi.BackColor;
                var colorOriginalNo = btnNo.BackColor;
                
                timer.Interval = 200; // 200ms
                timer.Tick += (s, e) =>
                {
                    parpadeos++;
                    
                    if (parpadeos % 2 == 0)
                    {
                        btnSi.BackColor = colorOriginalSi;
                        btnNo.BackColor = colorOriginalNo;
                    }
                    else
                    {
                        btnSi.BackColor = Color.Yellow;
                        btnNo.BackColor = Color.Yellow;
                    }
                    
                    // Parar después de 6 parpadeos (3 ciclos completos)
                    if (parpadeos >= 6)
                    {
                        timer.Stop();
                        timer.Dispose();
                        btnSi.BackColor = colorOriginalSi;
                        btnNo.BackColor = colorOriginalNo;
                    }
                };
                
                timer.Start();
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }
    }
}