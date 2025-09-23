using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Comercio.NET.Services;
using Comercio.NET.Models;

namespace Comercio.NET.Formularios
{
    public partial class UsuarioForm : Form
    {
        private readonly AuthenticationService _authService;
        private readonly bool _esEdicion;
        private readonly string _nombreUsuarioOriginal;

        // Controles del formulario
        private TextBox txtNombreUsuario, txtNombre, txtApellido, txtEmail, txtPassword, txtConfirmarPassword;
        private ComboBox cmbNivel;
        private NumericUpDown nudNumeroCajero;
        private CheckBox chkActivo, chkPuedeEliminarProductos, chkPuedeEditarPrecios, chkPuedeVerReportes, chkPuedeGestionarUsuarios, chkPuedeAnularFacturas;
        private Button btnGuardar, btnCancelar;
        private Label lblMensaje;
        private Panel panelPassword, panelPrincipal;
        
        // AGREGADO: Variables para controlar posiciones dinámicas
        private Label lblNivel, lblCajero, lblPermisos;
        private int _posicionBaseNivel;

        public UsuarioForm(string nombreUsuario = null)
        {
            _authService = new AuthenticationService();
            _esEdicion = !string.IsNullOrEmpty(nombreUsuario);
            _nombreUsuarioOriginal = nombreUsuario;

            InitializeComponent();
            ConfigurarFormulario();
            
            if (_esEdicion)
            {
                CargarDatosUsuario(nombreUsuario);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // UsuarioForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new Size(500, 580);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UsuarioForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Usuario";
            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            this.Text = _esEdicion ? "Modificar Usuario" : "Agregar Usuario";
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = new Font("Segoe UI", 9F);

            CrearControles();
            ConfigurarEventos();
            ConfigurarValidaciones();
        }

        private void CrearControles()
        {
            int currentY = 15;
            int labelWidth = 100;
            int controlWidth = 320;
            int margin = 15;
            int lineHeight = 30;

            // Título
            var lblTitulo = new Label
            {
                Text = _esEdicion ? "✏️ Modificar Usuario" : "➕ Agregar Nuevo Usuario",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(margin, currentY),
                Size = new Size(470, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitulo);
            currentY += 35;

            // Panel principal
            panelPrincipal = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(470, 380),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelPrincipal);

            int panelY = 15;

            // Nombre de Usuario
            panelPrincipal.Controls.Add(new Label
            {
                Text = "Usuario:",
                Location = new Point(15, panelY),
                Size = new Size(labelWidth, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            txtNombreUsuario = new TextBox
            {
                Location = new Point(15 + labelWidth, panelY - 2),
                Size = new Size(controlWidth, 22),
                Font = new Font("Segoe UI", 9F),
                MaxLength = 50
            };
            panelPrincipal.Controls.Add(txtNombreUsuario);
            panelY += lineHeight;

            // Nombre
            panelPrincipal.Controls.Add(new Label
            {
                Text = "Nombre:",
                Location = new Point(15, panelY),
                Size = new Size(labelWidth, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            txtNombre = new TextBox
            {
                Location = new Point(15 + labelWidth, panelY - 2),
                Size = new Size(controlWidth, 22),
                Font = new Font("Segoe UI", 9F),
                MaxLength = 100
            };
            panelPrincipal.Controls.Add(txtNombre);
            panelY += lineHeight;

            // Apellido
            panelPrincipal.Controls.Add(new Label
            {
                Text = "Apellido:",
                Location = new Point(15, panelY),
                Size = new Size(labelWidth, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            txtApellido = new TextBox
            {
                Location = new Point(15 + labelWidth, panelY - 2),
                Size = new Size(controlWidth, 22),
                Font = new Font("Segoe UI", 9F),
                MaxLength = 100
            };
            panelPrincipal.Controls.Add(txtApellido);
            panelY += lineHeight;

            // Email
            panelPrincipal.Controls.Add(new Label
            {
                Text = "Email:",
                Location = new Point(15, panelY),
                Size = new Size(labelWidth, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            txtEmail = new TextBox
            {
                Location = new Point(15 + labelWidth, panelY - 2),
                Size = new Size(controlWidth, 22),
                Font = new Font("Segoe UI", 9F),
                MaxLength = 200
            };
            panelPrincipal.Controls.Add(txtEmail);
            panelY += lineHeight;

            // Botón "Cambiar Contraseña" para modo edición (ANTES del panel)
            if (_esEdicion)
            {
                var btnCambiarPassword = new Button
                {
                    Text = "🔑 Cambiar Contraseña",
                    Location = new Point(15 + labelWidth, panelY - 2),
                    Size = new Size(150, 25),
                    BackColor = Color.FromArgb(255, 152, 0),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    UseVisualStyleBackColor = false
                };
                btnCambiarPassword.FlatAppearance.BorderSize = 0;
                btnCambiarPassword.Click += (s, e) =>
                {
                    panelPassword.Visible = !panelPassword.Visible;
                    btnCambiarPassword.Text = panelPassword.Visible ? "❌ Cancelar" : "🔑 Cambiar Contraseña";
                    btnCambiarPassword.BackColor = panelPassword.Visible ? Color.FromArgb(244, 67, 54) : Color.FromArgb(255, 152, 0);

                    // CORREGIDO: Mover controles en lugar de solo redimensionar
                    ReposicionarControles(panelPassword.Visible);
                    
                    if (panelPassword.Visible)
                    {
                        txtPassword.Clear();
                        txtConfirmarPassword.Clear();
                        txtPassword.Focus();
                    }
                };
                panelPrincipal.Controls.Add(btnCambiarPassword);
                panelY += lineHeight;
            }

            // Panel de contraseñas
            panelPassword = new Panel
            {
                Location = new Point(5, panelY),
                Size = new Size(460, 65),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = !_esEdicion
            };
            panelPrincipal.Controls.Add(panelPassword);

            // Contraseña
            panelPassword.Controls.Add(new Label
            {
                Text = _esEdicion ? "Nueva contraseña:" : "Contraseña:",
                Location = new Point(10, 10),
                Size = new Size(labelWidth, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            txtPassword = new TextBox
            {
                Location = new Point(10 + labelWidth, 8),
                Size = new Size(controlWidth - 10, 22),
                Font = new Font("Segoe UI", 9F),
                PasswordChar = '●'
            };
            panelPassword.Controls.Add(txtPassword);

            // Confirmar contraseña
            panelPassword.Controls.Add(new Label
            {
                Text = "Confirmar:",
                Location = new Point(10, 37),
                Size = new Size(labelWidth, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            txtConfirmarPassword = new TextBox
            {
                Location = new Point(10 + labelWidth, 35),
                Size = new Size(controlWidth - 10, 22),
                Font = new Font("Segoe UI", 9F),
                PasswordChar = '●'
            };
            panelPassword.Controls.Add(txtConfirmarPassword);

            // Avanzar panelY
            if (!_esEdicion)
            {
                panelY += 75; // Espacio para panel visible
            }

            // CORREGIDO: Guardar posición base para reposicionamiento dinámico
            _posicionBaseNivel = panelY;

            // Nivel de Usuario y Cajero
            lblNivel = new Label
            {
                Text = "Nivel:",
                Location = new Point(15, panelY),
                Size = new Size(45, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            panelPrincipal.Controls.Add(lblNivel);

            cmbNivel = new ComboBox
            {
                Location = new Point(65, panelY - 2),
                Size = new Size(140, 22),
                Font = new Font("Segoe UI", 9F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            cmbNivel.Items.Add(new { Text = "Administrador", Value = NivelUsuario.Administrador });
            cmbNivel.Items.Add(new { Text = "Supervisor", Value = NivelUsuario.Supervisor });
            cmbNivel.Items.Add(new { Text = "Vendedor", Value = NivelUsuario.Vendedor });
            cmbNivel.Items.Add(new { Text = "Invitado", Value = NivelUsuario.Invitado });
            
            cmbNivel.DisplayMember = "Text";
            cmbNivel.ValueMember = "Value";
            cmbNivel.SelectedIndex = 2;
            
            panelPrincipal.Controls.Add(cmbNivel);

            // Número de Cajero
            lblCajero = new Label
            {
                Text = "Cajero #:",
                Location = new Point(220, panelY),
                Size = new Size(60, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            panelPrincipal.Controls.Add(lblCajero);

            nudNumeroCajero = new NumericUpDown
            {
                Location = new Point(285, panelY - 2),
                Size = new Size(80, 22),
                Font = new Font("Segoe UI", 9F),
                Minimum = 1,
                Maximum = 99,
                Value = 1,
                TextAlign = HorizontalAlignment.Center
            };
            panelPrincipal.Controls.Add(nudNumeroCajero);
            panelY += lineHeight;

            // Checkbox Activo
            chkActivo = new CheckBox
            {
                Text = "✅ Usuario activo",
                Location = new Point(15, panelY + 5),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80),
                Checked = true
            };
            panelPrincipal.Controls.Add(chkActivo);
            panelY += 30;

            // Sección de permisos
            lblPermisos = new Label
            {
                Text = "🔐 PERMISOS:",
                Location = new Point(15, panelY),
                Size = new Size(400, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            };
            panelPrincipal.Controls.Add(lblPermisos);
            panelY += 25;

            // Permisos en dos columnas
            chkPuedeEliminarProductos = CrearCheckboxPermiso("Eliminar productos", new Point(15, panelY));
            chkPuedeEditarPrecios = CrearCheckboxPermiso("Editar precios", new Point(240, panelY));
            panelPrincipal.Controls.Add(chkPuedeEliminarProductos);
            panelPrincipal.Controls.Add(chkPuedeEditarPrecios);
            panelY += 25;

            chkPuedeVerReportes = CrearCheckboxPermiso("Ver reportes", new Point(15, panelY));
            chkPuedeGestionarUsuarios = CrearCheckboxPermiso("Gestionar usuarios", new Point(240, panelY));
            panelPrincipal.Controls.Add(chkPuedeVerReportes);
            panelPrincipal.Controls.Add(chkPuedeGestionarUsuarios);
            panelY += 25;

            chkPuedeAnularFacturas = CrearCheckboxPermiso("Anular facturas", new Point(15, panelY));
            panelPrincipal.Controls.Add(chkPuedeAnularFacturas);

            currentY += 390;

            // Mensaje de estado
            lblMensaje = new Label
            {
                Location = new Point(margin, currentY),
                Size = new Size(470, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblMensaje);
            currentY += 25;

            // Crear botones
            CrearBotones(currentY);

            // Configurar TabIndex
            txtNombreUsuario.TabIndex = 0;
            txtNombre.TabIndex = 1;
            txtApellido.TabIndex = 2;
            txtEmail.TabIndex = 3;
            txtPassword.TabIndex = 4;
            txtConfirmarPassword.TabIndex = 5;
            cmbNivel.TabIndex = 6;
            nudNumeroCajero.TabIndex = 7;
            chkActivo.TabIndex = 8;
            btnGuardar.TabIndex = 9;
            btnCancelar.TabIndex = 10;

            this.AcceptButton = btnGuardar;
            this.CancelButton = btnCancelar;
        }

        // CORREGIDO: Método para reposicionar controles dinámicamente - AJUSTES FINALES
        private void ReposicionarControles(bool panelPasswordVisible)
        {
            int desplazamiento = panelPasswordVisible ? 75 : 0;
            int nuevaPosicionNivel = _posicionBaseNivel + desplazamiento;

            // Reposicionar controles de Nivel y Cajero
            lblNivel.Location = new Point(lblNivel.Location.X, nuevaPosicionNivel);
            cmbNivel.Location = new Point(cmbNivel.Location.X, nuevaPosicionNivel - 2);
            lblCajero.Location = new Point(lblCajero.Location.X, nuevaPosicionNivel);
            nudNumeroCajero.Location = new Point(nudNumeroCajero.Location.X, nuevaPosicionNivel - 2);

            // Reposicionar checkbox Activo
            chkActivo.Location = new Point(chkActivo.Location.X, nuevaPosicionNivel + 35);

            // Reposicionar sección de permisos
            lblPermisos.Location = new Point(lblPermisos.Location.X, nuevaPosicionNivel + 65);
            
            chkPuedeEliminarProductos.Location = new Point(chkPuedeEliminarProductos.Location.X, nuevaPosicionNivel + 90);
            chkPuedeEditarPrecios.Location = new Point(chkPuedeEditarPrecios.Location.X, nuevaPosicionNivel + 90);
            
            chkPuedeVerReportes.Location = new Point(chkPuedeVerReportes.Location.X, nuevaPosicionNivel + 115);
            chkPuedeGestionarUsuarios.Location = new Point(chkPuedeGestionarUsuarios.Location.X, nuevaPosicionNivel + 115);
            
            chkPuedeAnularFacturas.Location = new Point(chkPuedeAnularFacturas.Location.X, nuevaPosicionNivel + 140);

            // CORRECCIÓN FINAL: Ajustes más conservadores para asegurar que no haya superposición
            if (panelPasswordVisible)
            {
                // Expandir formulario con medidas más generosas
                this.Height = 700; // AUMENTADO significativamente
                panelPrincipal.Height = 400; // REDUCIDO más para dar mayor espacio
                
                // Colocar mensaje y botones bien separados del panel
                lblMensaje.Location = new Point(lblMensaje.Location.X, 415); // Separación segura
                btnGuardar.Location = new Point(btnGuardar.Location.X, 445); // Separación segura  
                btnCancelar.Location = new Point(btnCancelar.Location.X, 445); // Separación segura
            }
            else
            {
                // Contraer formulario a tamaño original
                this.Height = 580;
                panelPrincipal.Height = 380;
                
                // Posiciones originales
                lblMensaje.Location = new Point(lblMensaje.Location.X, 405);
                btnGuardar.Location = new Point(btnGuardar.Location.X, 435);
                btnCancelar.Location = new Point(btnCancelar.Location.X, 435);
            }
        }

        private void CrearBotones(int yPosition)
        {
            int margin = 15;

            btnGuardar = new Button
            {
                Text = _esEdicion ? "💾 Guardar Cambios" : "➕ Crear Usuario",
                Location = new Point(margin + 60, yPosition),
                Size = new Size(140, 30),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnGuardar);

            btnCancelar = new Button
            {
                Text = "❌ Cancelar",
                Location = new Point(margin + 220, yPosition),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCancelar);
        }

        private CheckBox CrearCheckboxPermiso(string texto, Point ubicacion)
        {
            return new CheckBox
            {
                Text = texto,
                Location = ubicacion,
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(62, 80, 100)
            };
        }

        private void ConfigurarEventos()
        {
            btnGuardar.Click += async (s, e) => await GuardarUsuario();
            btnCancelar.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            cmbNivel.SelectedIndexChanged += CmbNivel_SelectedIndexChanged;

            this.Load += (s, e) =>
            {
                if (!_esEdicion)
                {
                    txtNombreUsuario.Focus();
                    nudNumeroCajero.Visible = true;
                    nudNumeroCajero.Enabled = true;
                }
                else
                {
                    txtNombre.Focus();
                }
            };
        }

        private void CmbNivel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbNivel.SelectedItem is { } item)
            {
                var nivel = (NivelUsuario)item.GetType().GetProperty("Value").GetValue(item);
                
                switch (nivel)
                {
                    case NivelUsuario.Administrador:
                        chkPuedeEliminarProductos.Checked = true;
                        chkPuedeEditarPrecios.Checked = true;
                        chkPuedeVerReportes.Checked = true;
                        chkPuedeGestionarUsuarios.Checked = true;
                        chkPuedeAnularFacturas.Checked = true;
                        break;
                    
                    case NivelUsuario.Supervisor:
                        chkPuedeEliminarProductos.Checked = false;
                        chkPuedeEditarPrecios.Checked = true;
                        chkPuedeVerReportes.Checked = true;
                        chkPuedeGestionarUsuarios.Checked = false;
                        chkPuedeAnularFacturas.Checked = true;
                        break;
                    
                    case NivelUsuario.Vendedor:
                        chkPuedeEliminarProductos.Checked = false;
                        chkPuedeEditarPrecios.Checked = false;
                        chkPuedeVerReportes.Checked = true;
                        chkPuedeGestionarUsuarios.Checked = false;
                        chkPuedeAnularFacturas.Checked = false;
                        break;
                    
                    case NivelUsuario.Invitado:
                        chkPuedeEliminarProductos.Checked = false;
                        chkPuedeEditarPrecios.Checked = false;
                        chkPuedeVerReportes.Checked = false;
                        chkPuedeGestionarUsuarios.Checked = false;
                        chkPuedeAnularFacturas.Checked = false;
                        break;
                }
            }
        }

        private void ConfigurarValidaciones()
        {
            // Validación en tiempo real para nombre de usuario
            txtNombreUsuario.TextChanged += (s, e) =>
            {
                if (txtNombreUsuario.Text.Length > 0 && txtNombreUsuario.Text.Length < 3)
                {
                    MostrarMensaje("El nombre de usuario debe tener al menos 3 caracteres", Color.Red);
                }
                else
                {
                    MostrarMensaje("", Color.Black);
                }
            };

            // Validación de email
            txtEmail.Leave += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtEmail.Text) && !EsEmailValido(txtEmail.Text))
                {
                    MostrarMensaje("El formato del email no es válido", Color.Red);
                }
            };

            // Validación de contraseñas
            txtConfirmarPassword.TextChanged += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtPassword.Text) && !string.IsNullOrEmpty(txtConfirmarPassword.Text))
                {
                    if (txtPassword.Text != txtConfirmarPassword.Text)
                    {
                        MostrarMensaje("Las contraseñas no coinciden", Color.Red);
                    }
                    else
                    {
                        MostrarMensaje("", Color.Black);
                    }
                }
            };
        }

        private async void CargarDatosUsuario(string nombreUsuario)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                var query = "SELECT * FROM Usuarios WHERE NombreUsuario = @nombreUsuario";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@nombreUsuario", nombreUsuario);

                connection.Open();
                using var reader = await cmd.ExecuteReaderAsync();

                if (reader.Read())
                {
                    txtNombreUsuario.Text = reader["NombreUsuario"].ToString();
                    txtNombreUsuario.ReadOnly = true;
                    txtNombre.Text = reader["Nombre"].ToString();
                    txtApellido.Text = reader["Apellido"].ToString();
                    txtEmail.Text = reader["Email"].ToString();
                    
                    var nivel = (NivelUsuario)reader.GetInt32("Nivel");
                    for (int i = 0; i < cmbNivel.Items.Count; i++)
                    {
                        var item = cmbNivel.Items[i];
                        if ((NivelUsuario)item.GetType().GetProperty("Value").GetValue(item) == nivel)
                        {
                            cmbNivel.SelectedIndex = i;
                            break;
                        }
                    }
                    
                    nudNumeroCajero.Value = reader.GetInt32("NumeroCajero");
                    chkActivo.Checked = reader.GetBoolean("Activo");
                    chkPuedeEliminarProductos.Checked = reader.GetBoolean("PuedeEliminarProductos");
                    chkPuedeEditarPrecios.Checked = reader.GetBoolean("PuedeEditarPrecios");
                    chkPuedeVerReportes.Checked = reader.GetBoolean("PuedeVerReportes");
                    chkPuedeGestionarUsuarios.Checked = reader.GetBoolean("PuedeGestionarUsuarios");
                    chkPuedeAnularFacturas.Checked = reader.GetBoolean("PuedeAnularFacturas");
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar usuario: {ex.Message}", Color.Red);
            }
        }

        private async Task GuardarUsuario()
        {
            if (!ValidarDatos())
                return;

            try
            {
                btnGuardar.Enabled = false;
                btnGuardar.Text = "💾 Guardando...";
                MostrarMensaje("Guardando usuario...", Color.Blue);

                var usuario = new Usuario
                {
                    NombreUsuario = txtNombreUsuario.Text.Trim(),
                    Nombre = txtNombre.Text.Trim(),
                    Apellido = txtApellido.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Nivel = (NivelUsuario)cmbNivel.SelectedItem.GetType().GetProperty("Value").GetValue(cmbNivel.SelectedItem),
                    NumeroCajero = (int)nudNumeroCajero.Value,
                    Activo = chkActivo.Checked,
                    PuedeEliminarProductos = chkPuedeEliminarProductos.Checked,
                    PuedeEditarPrecios = chkPuedeEditarPrecios.Checked,
                    PuedeVerReportes = chkPuedeVerReportes.Checked,
                    PuedeGestionarUsuarios = chkPuedeGestionarUsuarios.Checked,
                    PuedeAnularFacturas = chkPuedeAnularFacturas.Checked
                };

                bool exito;
                if (_esEdicion)
                {
                    exito = await ActualizarUsuario(usuario);
                    
                    // Si cambió la contraseña, actualizarla por separado
                    if (panelPassword.Visible && !string.IsNullOrEmpty(txtPassword.Text))
                    {
                        await _authService.ActualizarPasswordUsuarioAsync(usuario.NombreUsuario, txtPassword.Text);
                    }
                }
                else
                {
                    exito = await _authService.CrearUsuarioAsync(usuario, txtPassword.Text);
                }

                if (exito)
                {
                    MostrarMensaje("✅ Usuario guardado correctamente", Color.Green);
                    await Task.Delay(1000);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MostrarMensaje("❌ Error al guardar el usuario", Color.Red);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error: {ex.Message}", Color.Red);
            }
            finally
            {
                btnGuardar.Enabled = true;
                btnGuardar.Text = _esEdicion ? "💾 Guardar Cambios" : "➕ Crear Usuario";
            }
        }

        private async Task<bool> ActualizarUsuario(Usuario usuario)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                var query = @"
                    UPDATE Usuarios SET 
                        Nombre = @nombre,
                        Apellido = @apellido,
                        Email = @email,
                        Nivel = @nivel,
                        NumeroCajero = @numeroCajero,
                        Activo = @activo,
                        PuedeEliminarProductos = @puedeEliminarProductos,
                        PuedeEditarPrecios = @puedeEditarPrecios,
                        PuedeVerReportes = @puedeVerReportes,
                        PuedeGestionarUsuarios = @puedeGestionarUsuarios,
                        PuedeAnularFacturas = @puedeAnularFacturas
                    WHERE NombreUsuario = @nombreUsuario";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@nombre", usuario.Nombre);
                cmd.Parameters.AddWithValue("@apellido", usuario.Apellido);
                cmd.Parameters.AddWithValue("@email", usuario.Email ?? "");
                cmd.Parameters.AddWithValue("@nivel", (int)usuario.Nivel);
                cmd.Parameters.AddWithValue("@numeroCajero", usuario.NumeroCajero);
                cmd.Parameters.AddWithValue("@activo", usuario.Activo);
                cmd.Parameters.AddWithValue("@puedeEliminarProductos", usuario.PuedeEliminarProductos);
                cmd.Parameters.AddWithValue("@puedeEditarPrecios", usuario.PuedeEditarPrecios);
                cmd.Parameters.AddWithValue("@puedeVerReportes", usuario.PuedeVerReportes);
                cmd.Parameters.AddWithValue("@puedeGestionarUsuarios", usuario.PuedeGestionarUsuarios);
                cmd.Parameters.AddWithValue("@puedeAnularFacturas", usuario.PuedeAnularFacturas);
                cmd.Parameters.AddWithValue("@nombreUsuario", _nombreUsuarioOriginal);

                connection.Open();
                int filasAfectadas = await cmd.ExecuteNonQueryAsync();
                return filasAfectadas > 0;
            }
            catch
            {
                return false;
            }
        }

        private bool ValidarDatos()
        {
            if (string.IsNullOrWhiteSpace(txtNombreUsuario.Text) || txtNombreUsuario.Text.Length < 3)
            {
                MostrarMensaje("El nombre de usuario debe tener al menos 3 caracteres", Color.Red);
                txtNombreUsuario.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MostrarMensaje("El nombre es requerido", Color.Red);
                txtNombre.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtApellido.Text))
            {
                MostrarMensaje("El apellido es requerido", Color.Red);
                txtApellido.Focus();
                return false;
            }

            if (!string.IsNullOrEmpty(txtEmail.Text) && !EsEmailValido(txtEmail.Text))
            {
                MostrarMensaje("El formato del email no es válido", Color.Red);
                txtEmail.Focus();
                return false;
            }

            if (!_esEdicion || panelPassword.Visible)
            {
                if (string.IsNullOrEmpty(txtPassword.Text) || txtPassword.Text.Length < 4)
                {
                    MostrarMensaje("La contraseña debe tener al menos 4 caracteres", Color.Red);
                    txtPassword.Focus();
                    return false;
                }

                if (txtPassword.Text != txtConfirmarPassword.Text)
                {
                    MostrarMensaje("Las contraseñas no coinciden", Color.Red);
                    txtConfirmarPassword.Focus();
                    return false;
                }
            }

            if (cmbNivel.SelectedItem == null)
            {
                MostrarMensaje("Debe seleccionar un nivel de usuario", Color.Red);
                cmbNivel.Focus();
                return false;
            }

            return true;
        }

        private bool EsEmailValido(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void MostrarMensaje(string mensaje, Color color)
        {
            lblMensaje.Text = mensaje;
            lblMensaje.ForeColor = color;
        }
    }
}