using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Comercio.NET.Services;

namespace Comercio.NET.Formularios
{
    public partial class CambiarPasswordForm : Form
    {
        private readonly AuthenticationService _authService;
        private TextBox txtUsuario, txtPasswordActual, txtPasswordNueva, txtConfirmarPassword;
        private Button btnCambiar, btnCancelar;
        private Label lblMensaje;

        public CambiarPasswordForm()
        {
            _authService = new AuthenticationService();
            InitializeComponent();
            ConfigurarFormulario();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // CambiarPasswordForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 350);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CambiarPasswordForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Cambiar Contraseña";
            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            this.Text = "Cambiar Contraseña";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.Font = new Font("Segoe UI", 10F);

            CrearControles();
            ConfigurarEventos();
        }

        private void CrearControles()
        {
            int currentY = 30;
            int labelWidth = 150;
            int textBoxWidth = 200;
            int margin = 20;

            // Título
            var lblTitulo = new Label
            {
                Text = "🔐 Cambiar Contraseña",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(margin, currentY),
                Size = new Size(350, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitulo);
            currentY += 50;

            // Usuario
            var lblUsuario = new Label
            {
                Text = "Usuario:",
                Location = new Point(margin, currentY),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblUsuario);

            txtUsuario = new TextBox
            {
                Location = new Point(margin + labelWidth, currentY),
                Size = new Size(textBoxWidth, 25),
                Font = new Font("Segoe UI", 10F)
            };
            this.Controls.Add(txtUsuario);
            currentY += 35;

            // Contraseña actual
            var lblPasswordActual = new Label
            {
                Text = "Contraseña actual:",
                Location = new Point(margin, currentY),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblPasswordActual);

            txtPasswordActual = new TextBox
            {
                Location = new Point(margin + labelWidth, currentY),
                Size = new Size(textBoxWidth, 25),
                Font = new Font("Segoe UI", 10F),
                PasswordChar = '●'
            };
            this.Controls.Add(txtPasswordActual);
            currentY += 35;

            // Nueva contraseña
            var lblPasswordNueva = new Label
            {
                Text = "Nueva contraseña:",
                Location = new Point(margin, currentY),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblPasswordNueva);

            txtPasswordNueva = new TextBox
            {
                Location = new Point(margin + labelWidth, currentY),
                Size = new Size(textBoxWidth, 25),
                Font = new Font("Segoe UI", 10F),
                PasswordChar = '●'
            };
            this.Controls.Add(txtPasswordNueva);
            currentY += 35;

            // Confirmar contraseña
            var lblConfirmarPassword = new Label
            {
                Text = "Confirmar contraseña:",
                Location = new Point(margin, currentY),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblConfirmarPassword);

            txtConfirmarPassword = new TextBox
            {
                Location = new Point(margin + labelWidth, currentY),
                Size = new Size(textBoxWidth, 25),
                Font = new Font("Segoe UI", 10F),
                PasswordChar = '●'
            };
            this.Controls.Add(txtConfirmarPassword);
            currentY += 50;

            // Mensaje de estado
            lblMensaje = new Label
            {
                Location = new Point(margin, currentY),
                Size = new Size(350, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblMensaje);
            currentY += 35;

            // Botones
            btnCambiar = new Button
            {
                Text = "Cambiar",
                Location = new Point(margin + 50, currentY),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCambiar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCambiar);

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(margin + 160, currentY),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCancelar);

            // ELIMINADO: El botón "Cambiar Contraseña" que estaba aquí creando el loop

            // Configurar tab order
            txtUsuario.TabIndex = 0;
            txtPasswordActual.TabIndex = 1;
            txtPasswordNueva.TabIndex = 2;
            txtConfirmarPassword.TabIndex = 3;
            btnCambiar.TabIndex = 4;
            btnCancelar.TabIndex = 5;

            this.AcceptButton = btnCambiar;
            this.CancelButton = btnCancelar;
        }

        private void ConfigurarEventos()
        {
            btnCambiar.Click += async (s, e) => await CambiarPassword();
            btnCancelar.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Load += (s, e) => txtUsuario.Focus();
        }

        private async Task CambiarPassword()
        {
            if (string.IsNullOrWhiteSpace(txtUsuario.Text) ||
                string.IsNullOrWhiteSpace(txtPasswordActual.Text) ||
                string.IsNullOrWhiteSpace(txtPasswordNueva.Text) ||
                string.IsNullOrWhiteSpace(txtConfirmarPassword.Text))
            {
                MostrarMensaje("Complete todos los campos", Color.Red);
                return;
            }

            if (txtPasswordNueva.Text != txtConfirmarPassword.Text)
            {
                MostrarMensaje("Las contraseñas nuevas no coinciden", Color.Red);
                txtConfirmarPassword.Focus();
                return;
            }

            if (txtPasswordNueva.Text.Length < 4)
            {
                MostrarMensaje("La nueva contraseña debe tener al menos 4 caracteres", Color.Red);
                txtPasswordNueva.Focus();
                return;
            }

            btnCambiar.Enabled = false;
            btnCambiar.Text = "Cambiando...";
            MostrarMensaje("Verificando credenciales...", Color.Blue);

            try
            {
                // Primero verificar que la contraseña actual sea correcta
                var (exito, mensaje, usuario) = await _authService.AutenticarAsync(txtUsuario.Text.Trim(), txtPasswordActual.Text);

                if (!exito)
                {
                    MostrarMensaje("Contraseña actual incorrecta", Color.Red);
                    txtPasswordActual.Clear();
                    txtPasswordActual.Focus();
                    return;
                }

                // Si la autenticación fue exitosa, cambiar la contraseña
                bool actualizado = await _authService.ActualizarPasswordUsuarioAsync(txtUsuario.Text.Trim(), txtPasswordNueva.Text);

                if (actualizado)
                {
                    MostrarMensaje("Contraseña actualizada correctamente", Color.Green);
                    await Task.Delay(1500);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MostrarMensaje("Error al actualizar la contraseña", Color.Red);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error: {ex.Message}", Color.Red);
            }
            finally
            {
                btnCambiar.Enabled = true;
                btnCambiar.Text = "Cambiar";
            }
        }

        private void MostrarMensaje(string mensaje, Color color)
        {
            lblMensaje.Text = mensaje;
            lblMensaje.ForeColor = color;
        }
    }
}