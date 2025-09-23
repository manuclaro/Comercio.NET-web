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
        private Panel panelPassword;

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
            this.ClientSize = new Size(500, 530); // AUMENTADO de 500 a 530
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

            // Panel principal - ALTURA AUMENTADA
            var panelPrincipal = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(470, 380), // AUMENTADO de 350 a 380
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

            // Panel de contraseñas
            panelPassword = new Panel
            {
                Location = new Point(0, panelY),
                Size = new Size(470, 65),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            panelPrincipal.Controls.Add(panelPassword);

            // Contraseña
            panelPassword.Controls.Add(new Label
            {
                Text = _esEdicion ? "Nueva contraseña:" : "Contraseña:",
                Location = new Point(15, 8),
                Size = new Size(labelWidth, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            txtPassword = new TextBox
            {
                Location = new Point(15 + labelWidth, 6),
                Size = new Size(controlWidth, 22),
                Font = new Font("Segoe UI", 9F),
                PasswordChar = '●'
            };
            panelPassword.Controls.Add(txtPassword);

            // Confirmar contraseña
            panelPassword.Controls.Add(new Label
            {
                Text = "Confirmar:",
                Location = new Point(15, 35),
                Size = new Size(labelWidth, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            txtConfirmarPassword = new TextBox
            {
                Location = new Point(15 + labelWidth, 33),
                Size = new Size(controlWidth, 22),
                Font = new Font("Segoe UI", 9F),
                PasswordChar = '●'
            };
            panelPassword.Controls.Add(txtConfirmarPassword);

            if (_esEdicion)
            {
                panelPassword.Visible = false;
                
                // Botón "Cambiar Contraseña" para modo edición
                var btnCambiarPassword = new Button
                {
                    Text = "🔑 Cambiar Contraseña",
                    Location = new Point(15 + labelWidth, panelY + 10),
                    Size = new Size(150, 28),
                    BackColor = Color.FromArgb(255, 152, 0),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    UseVisualStyleBackColor = false
                };
                btnCambiarPassword.FlatAppearance.BorderSize = 0;
                btnCambiarPassword.FlatAppearance.BorderColor = Color.FromArgb(255, 152, 0);
                btnCambiarPassword.Click += (s, e) =>
                {
                    panelPassword.Visible = !panelPassword.Visible;
                    btnCambiarPassword.Text = panelPassword.Visible ? "❌ Cancelar" : "🔑 Cambiar Contraseña";
                    btnCambiarPassword.BackColor = panelPassword.Visible ? Color.FromArgb(244, 67, 54) : Color.FromArgb(255, 152, 0);
                    
                    // Ajustar la altura del formulario dinámicamente - ACTUALIZADO
                    this.Height = panelPassword.Visible ? 595 : 530; // AJUSTADO para nueva altura
                    panelPrincipal.Height = panelPassword.Visible ? 445 : 380; // AJUSTADO para nueva altura
                };
                panelPrincipal.Controls.Add(btnCambiarPassword);
            }

            // CORREGIDO: Avanzar panelY dependiendo del modo
            if (_esEdicion)
            {
                panelY += panelPassword.Visible ? 70 : 30; // Si es edición, sumar poco espacio
            }
            else
            {
                panelY += 70; // Si es agregar, dejar espacio para el panel de contraseñas visible
            }

            // CORREGIDO: Nivel de Usuario y Cajero - AHORA SIEMPRE VISIBLE
            panelPrincipal.Controls.Add(new Label
            {
                Text = "Nivel:",
                Location = new Point(15, panelY),
                Size = new Size(45, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

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
            cmbNivel.SelectedIndex = 2; // Vendedor por defecto
            
            panelPrincipal.Controls.Add(cmbNivel);

            // CORREGIDO: Número de Cajero - AHORA SIEMPRE VISIBLE
            panelPrincipal.Controls.Add(new Label
            {
                Text = "Cajero #:",
                Location = new Point(220, panelY),
                Size = new Size(60, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            nudNumeroCajero = new NumericUpDown
            {
                Location = new Point(285, panelY - 2),
                Size = new Size(80, 22),
                Font = new Font("Segoe UI", 9F),
                Minimum = 1,
                Maximum = 99,
                Value = 1, // VALOR POR DEFECTO VISIBLE
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
            panelPrincipal.Controls.Add(new Label
            {
                Text = "🔐 PERMISOS:",
                Location = new Point(15, panelY),
                Size = new Size(400, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            });
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

            currentY += 390; // AUMENTADO de 360 a 390

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

            // Botones
            btnGuardar = new Button
            {
                Text = _esEdicion ? "💾 Guardar Cambios" : "➕ Crear Usuario",
                Location = new Point(margin + 60, currentY),
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
                Location = new Point(margin + 220, currentY),
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

            // Evento para autoconfigurar permisos según nivel
            cmbNivel.SelectedIndexChanged += CmbNivel_SelectedIndexChanged;

            this.Load += (s, e) =>
            {
                if (!_esEdicion)
                {
                    txtNombreUsuario.Focus();
                    // ASEGURAR QUE EL CAJERO SEA VISIBLE EN MODO AGREGAR
                    nudNumeroCajero.Visible = true;
                    nudNumeroCajero.Enabled = true;
                    
                    // DEBUG: Verificar que los controles estén correctamente creados
                    System.Diagnostics.Debug.WriteLine($"nudNumeroCajero creado: {nudNumeroCajero != null}");
                    System.Diagnostics.Debug.WriteLine($"nudNumeroCajero visible: {nudNumeroCajero?.Visible}");
                    System.Diagnostics.Debug.WriteLine($"nudNumeroCajero valor: {nudNumeroCajero?.Value}");
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
                
                // Autoconfigurar permisos según nivel
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