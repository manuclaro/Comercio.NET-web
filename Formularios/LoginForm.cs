using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.Json;
using Comercio.NET.Services;
using Comercio.NET.Models;

namespace Comercio.NET.Formularios
{
    public class LoginForm : Form
    {
        private readonly AuthenticationService _authService;
        private TextBox txtUsuario, txtPassword;
        private Button btnIngresar, btnCancelar;
        private CheckBox chkRecordar;
        private Label lblMensaje;

        private System.ComponentModel.IContainer components = null;

        public bool LoginExitoso { get; private set; }
        public bool MostrarDebugAutenticacion { get; set; } = false;

        public LoginForm()
        {
            _authService = new AuthenticationService();
            InitializeComponent();
            ConfigurarFormulario();
            CargarUsuarioRecordado(); // AGREGADO: Cargar usuario recordado al inicializar
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // LoginForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 380);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Iniciar Sesión";
            this.ResumeLayout(false);
        }

        #endregion

        private void ConfigurarFormulario()
        {
            this.Text = "Iniciar Sesión - Comercio .NET";
            this.Size = new Size(400, 380);
            this.StartPosition = FormStartPosition.CenterScreen;
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
            int centerX = this.Width / 2;
            int currentY = 30;

            // Logo/Título
            var lblTitulo = new Label
            {
                Text = "🏪 Comercio .NET",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(centerX - 100, currentY),
                Size = new Size(200, 35),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitulo);
            currentY += 50;

            // Panel contenedor
            var panel = new Panel
            {
                Location = new Point(50, currentY),
                Size = new Size(300, 140),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panel);

            int panelY = 20;

            // Usuario
            var lblUsuario = new Label
            {
                Text = "Usuario:",
                Location = new Point(20, panelY),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            panel.Controls.Add(lblUsuario);

            txtUsuario = new TextBox
            {
                Location = new Point(90, panelY),
                Size = new Size(180, 25),
                Font = new Font("Segoe UI", 10F)
            };
            panel.Controls.Add(txtUsuario);
            panelY += 35;

            // Contraseña
            var lblPassword = new Label
            {
                Text = "Contraseña:",
                Location = new Point(20, panelY),
                Size = new Size(70, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            panel.Controls.Add(lblPassword);

            txtPassword = new TextBox
            {
                Location = new Point(90, panelY),
                Size = new Size(180, 25),
                Font = new Font("Segoe UI", 10F),
                PasswordChar = '●'
            };
            panel.Controls.Add(txtPassword);
            panelY += 35;

            // CORREGIDO: Recordar usuario con validación mejorada
            chkRecordar = new CheckBox
            {
                Text = "Recordar usuario",
                Location = new Point(90, panelY),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9F),
                Checked = AuthenticationService.ConfiguracionLogin?.RecordarUltimoUsuario ?? false
            };
            panel.Controls.Add(chkRecordar);

            currentY += 160;

            // Mensaje de estado
            lblMensaje = new Label
            {
                Location = new Point(50, currentY),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblMensaje);
            currentY += 25;

            // Botones principales
            btnIngresar = new Button
            {
                Text = "Ingresar",
                Location = new Point(200, currentY),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnIngresar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnIngresar);

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(290, currentY),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)    
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCancelar);

            currentY += 50;

            // Botón para cambiar contraseña
            var btnCambiarPassword = new Button
            {
                Text = "Cambiar contraseña",
                Location = new Point(80, currentY),
                Size = new Size(240, 30),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(0, 120, 215),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Underline),
                Cursor = Cursors.Hand
            };
            btnCambiarPassword.FlatAppearance.BorderSize = 0;
            btnCambiarPassword.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 240, 250);
            btnCambiarPassword.Click += (s, e) =>
            {
                using (var cambiarForm = new CambiarPasswordForm())
                {
                    cambiarForm.ShowDialog(this);
                }
            };
            this.Controls.Add(btnCambiarPassword);

            // Separador visual
            var lblSeparador = new Label
            {
                Text = "───────────────────────────────",
                Location = new Point(50, currentY - 20),
                Size = new Size(300, 15),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(200, 200, 200),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblSeparador);

            // Configurar tab order
            txtUsuario.TabIndex = 0;
            txtPassword.TabIndex = 1;
            chkRecordar.TabIndex = 2;
            btnIngresar.TabIndex = 3;
            btnCancelar.TabIndex = 4;
            btnCambiarPassword.TabIndex = 5;

            this.AcceptButton = btnIngresar;
            this.CancelButton = btnCancelar;
        }

        private void ConfigurarEventos()
        {
            btnIngresar.Click += async (s, e) => await ProcesarLogin();
            btnCancelar.Click += (s, e) =>
            {
                LoginExitoso = false;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            txtPassword.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await ProcesarLogin();
                }
            };

            // AGREGADO: Evento para manejar cambios en "Recordar usuario"
            chkRecordar.CheckedChanged += (s, e) =>
            {
                try
                {
                    // Asegurar que ConfiguracionLogin no sea null
                    if (AuthenticationService.ConfiguracionLogin != null)
                    {
                        AuthenticationService.ConfiguracionLogin.RecordarUltimoUsuario = chkRecordar.Checked;
                        
                        // Si se desmarca, limpiar el usuario recordado
                        if (!chkRecordar.Checked)
                        {
                            AuthenticationService.ConfiguracionLogin.UltimoUsuarioLogueado = "";
                            txtUsuario.Clear(); // Limpiar el campo de usuario también
                        }
                        
                        GuardarConfiguracionLogin();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al guardar configuración: {ex.Message}");
                }
            };

            this.Load += (s, e) =>
            {
                // Enfocar el campo correcto según si hay usuario recordado
                if (!string.IsNullOrEmpty(txtUsuario.Text))
                {
                    txtPassword.Focus();
                }
                else
                {
                    txtUsuario.Focus();
                }
            };
        }

        // AGREGADO: Método para cargar usuario recordado
        private void CargarUsuarioRecordado()
        {
            try
            {
                // Asegurar que la configuración esté cargada
                if (AuthenticationService.ConfiguracionLogin != null)
                {
                    if (AuthenticationService.ConfiguracionLogin.RecordarUltimoUsuario && 
                        !string.IsNullOrEmpty(AuthenticationService.ConfiguracionLogin.UltimoUsuarioLogueado))
                    {
                        txtUsuario.Text = AuthenticationService.ConfiguracionLogin.UltimoUsuarioLogueado;
                        chkRecordar.Checked = true;
                    }
                    else
                    {
                        chkRecordar.Checked = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar usuario recordado: {ex.Message}");
            }
        }

        // CORREGIDO: Método para guardar la configuración usando el método estático
        private void GuardarConfiguracionLogin()
        {
            try
            {
                AuthenticationService.GuardarConfiguracionLogin();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al guardar configuración: {ex.Message}");
            }
        }

        private async Task ProcesarLogin()
        {
            if (string.IsNullOrWhiteSpace(txtUsuario.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MostrarMensaje("Complete todos los campos", Color.Red);
                return;
            }

            btnIngresar.Enabled = false;
            btnIngresar.Text = "Verificando...";
            MostrarMensaje("Autenticando usuario...", Color.Blue);

            try
            {
                var (exito, mensaje, usuario) = await _authService.AutenticarAsync(txtUsuario.Text.Trim(), txtPassword.Text);

                if (exito)
                {
                    // AGREGADO: Guardar usuario si "Recordar" está marcado
                    if (chkRecordar.Checked)
                    {
                        if (AuthenticationService.ConfiguracionLogin != null)
                        {
                            AuthenticationService.ConfiguracionLogin.UltimoUsuarioLogueado = txtUsuario.Text.Trim();
                            AuthenticationService.ConfiguracionLogin.RecordarUltimoUsuario = true;
                            GuardarConfiguracionLogin();
                        }
                    }
                    else
                    {
                        // Si no está marcado, limpiar usuario recordado
                        if (AuthenticationService.ConfiguracionLogin != null)
                        {
                            AuthenticationService.ConfiguracionLogin.UltimoUsuarioLogueado = "";
                            AuthenticationService.ConfiguracionLogin.RecordarUltimoUsuario = false;
                            GuardarConfiguracionLogin();
                        }
                    }

                    MostrarMensaje($"Bienvenido, {usuario.NombreCompleto}", Color.Green);
                    LoginExitoso = true;

                    await Task.Delay(1000);

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MostrarMensaje(mensaje, Color.Red);
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error de conexión: {ex.Message}", Color.Red);
            }
            finally
            {
                btnIngresar.Enabled = true;
                btnIngresar.Text = "Ingresar";
            }
        }

        private void MostrarMensaje(string mensaje, Color color)
        {
            lblMensaje.Text = mensaje;
            lblMensaje.ForeColor = color;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!LoginExitoso && this.DialogResult != DialogResult.Cancel)
            {
                var result = MessageBox.Show(
                    "¿Está seguro que desea salir sin iniciar sesión?\nLa aplicación se cerrará.",
                    "Confirmar salida",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }
    }
}