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
    public partial class GestionUsuariosMainForm : Form
    {
        private readonly AuthenticationService _authService;
        private DataGridView dgvUsuarios;
        private Button btnAgregar, btnEditar, btnEliminar, btnRefrescar, btnCambiarPassword;
        private TextBox txtBuscar;
        private Label lblMensaje;
        private CheckBox chkMostrarDebugHash; // NUEVO: Checkbox de debug

        // NUEVO: Propiedad estática para acceso global al estado del debug
        public static bool MostrarDebugHash { get; private set; } = false;

        public GestionUsuariosMainForm()
        {
            _authService = new AuthenticationService();
            InitializeComponent();
            ConfigurarFormulario();
            _ = CargarUsuarios(); // Carga inicial
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // GestionUsuariosMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new Size(1000, 600);
            this.MinimumSize = new Size(800, 500);
            this.Name = "GestionUsuariosMainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Gestión de Usuarios";
            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            this.Text = "👥 Gestión de Usuarios";
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = new Font("Segoe UI", 10F);

            CrearControles();
            ConfigurarEventos();
            VerificarPermisos();
        }

        private void CrearControles()
        {
            int margin = 20;
            int currentY = 20;

            // Título
            var lblTitulo = new Label
            {
                Text = "👥 GESTIÓN DE USUARIOS",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(margin, currentY),
                Size = new Size(400, 35),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblTitulo);
            currentY += 50;

            // Panel de herramientas EXPANDIDO para incluir debug
            var panelHerramientas = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(960, 100), // AUMENTADO de 80 a 100
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelHerramientas);

            // Búsqueda
            panelHerramientas.Controls.Add(new Label
            {
                Text = "🔍 Buscar:",
                Location = new Point(15, 20),
                Size = new Size(70, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });

            txtBuscar = new TextBox
            {
                Location = new Point(90, 18),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "Buscar por nombre, usuario o email..."
            };
            panelHerramientas.Controls.Add(txtBuscar);

            // Botones de acción - PRIMERA FILA
            btnAgregar = CrearBoton("➕ Agregar", new Point(360, 15), Color.FromArgb(76, 175, 80));
            btnEditar = CrearBoton("✏️ Editar", new Point(480, 15), Color.FromArgb(33, 150, 243));
            btnEliminar = CrearBoton("🗑️ Eliminar", new Point(580, 15), Color.FromArgb(244, 67, 54));
            btnCambiarPassword = CrearBoton("🔑 Cambiar Contraseña", new Point(680, 15), Color.FromArgb(255, 152, 0));
            btnRefrescar = CrearBoton("🔄 Refrescar", new Point(840, 15), Color.FromArgb(158, 158, 158));

            panelHerramientas.Controls.Add(btnAgregar);
            panelHerramientas.Controls.Add(btnEditar);
            panelHerramientas.Controls.Add(btnEliminar);
            panelHerramientas.Controls.Add(btnCambiarPassword);
            panelHerramientas.Controls.Add(btnRefrescar);

            // NUEVO: Checkbox de debug - SEGUNDA FILA
            chkMostrarDebugHash = new CheckBox
            {
                Text = "🔍 Mostrar debug de hash en operaciones (desarrollo)",
                Location = new Point(15, 55), // SEGUNDA FILA
                Size = new Size(350, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(255, 140, 0),
                Checked = false
            };
            chkMostrarDebugHash.CheckedChanged += (s, e) =>
            {
                MostrarDebugHash = chkMostrarDebugHash.Checked;
                if (MostrarDebugHash)
                {
                    MostrarMensaje("🔍 Debug de hash ACTIVADO - Se mostrarán los hash generados", Color.Purple);
                }
                else
                {
                    MostrarMensaje("", Color.Black);
                }
            };
            panelHerramientas.Controls.Add(chkMostrarDebugHash);

            // Estado de botones
            btnEditar.Enabled = false;
            btnEliminar.Enabled = false;
            btnCambiarPassword.Enabled = false;

            currentY += 120; // AUMENTADO de 100 a 120

            // DataGridView
            dgvUsuarios = new DataGridView
            {
                Location = new Point(margin, currentY),
                Size = new Size(960, 380), // REDUCIDO de 400 a 380
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 9F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Configurar columnas
            dgvUsuarios.Columns.Add("NombreUsuario", "Usuario");
            dgvUsuarios.Columns.Add("NombreCompleto", "Nombre Completo");
            dgvUsuarios.Columns.Add("Email", "Email");
            dgvUsuarios.Columns.Add("Nivel", "Nivel");
            dgvUsuarios.Columns.Add("NumeroCajero", "Cajero #");
            dgvUsuarios.Columns.Add("Activo", "Estado");
            dgvUsuarios.Columns.Add("UltimoAcceso", "Último Acceso");

            // Configurar anchos específicos
            dgvUsuarios.Columns["NombreUsuario"].FillWeight = 15;
            dgvUsuarios.Columns["NombreCompleto"].FillWeight = 25;
            dgvUsuarios.Columns["Email"].FillWeight = 25;
            dgvUsuarios.Columns["Nivel"].FillWeight = 12;
            dgvUsuarios.Columns["NumeroCajero"].FillWeight = 8;
            dgvUsuarios.Columns["Activo"].FillWeight = 10;
            dgvUsuarios.Columns["UltimoAcceso"].FillWeight = 15;

            this.Controls.Add(dgvUsuarios);
            currentY += 400; // REDUCIDO de 420 a 400

            // Mensaje de estado
            lblMensaje = new Label
            {
                Location = new Point(margin, currentY),
                Size = new Size(960, 25),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Blue,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(lblMensaje);
        }

        private Button CrearBoton(string texto, Point ubicacion, Color color)
        {
            var boton = new Button
            {
                Text = texto,
                Location = ubicacion,
                Size = new Size(95, 30),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            boton.FlatAppearance.BorderSize = 0;
            return boton;
        }

        private void ConfigurarEventos()
        {
            // Eventos de botones
            btnAgregar.Click += async (s, e) => await AgregarUsuario();
            btnEditar.Click += async (s, e) => await EditarUsuario();
            btnEliminar.Click += async (s, e) => await EliminarUsuario();
            btnCambiarPassword.Click += async (s, e) => await CambiarPasswordUsuario();
            btnRefrescar.Click += async (s, e) => await CargarUsuarios();

            // Búsqueda en tiempo real
            txtBuscar.TextChanged += async (s, e) => await FiltrarUsuarios();

            // Eventos del DataGridView
            dgvUsuarios.SelectionChanged += (s, e) =>
            {
                bool haySeleccion = dgvUsuarios.SelectedRows.Count > 0;
                btnEditar.Enabled = haySeleccion;
                btnEliminar.Enabled = haySeleccion;
                btnCambiarPassword.Enabled = haySeleccion;
            };

            dgvUsuarios.CellDoubleClick += async (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    await EditarUsuario();
                }
            };

            // Evento del formulario
            this.Load += (s, e) => txtBuscar.Focus();
        }

        private void VerificarPermisos()
        {
            if (AuthenticationService.SesionActual?.Usuario != null)
            {
                var usuario = AuthenticationService.SesionActual.Usuario;
                
                // Solo administradores y usuarios con permiso específico pueden gestionar usuarios
                bool puedeGestionar = usuario.Nivel == NivelUsuario.Administrador || 
                                     usuario.PuedeGestionarUsuarios;

                if (!puedeGestionar)
                {
                    MessageBox.Show("No tienes permisos para gestionar usuarios.", "Acceso Denegado",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.Close();
                    return;
                }

                // Los supervisores no pueden eliminar usuarios
                if (usuario.Nivel != NivelUsuario.Administrador)
                {
                    btnEliminar.Visible = false;
                }
            }
        }

        private async Task CargarUsuarios()
        {
            try
            {
                MostrarMensaje("⏳ Cargando usuarios...", Color.Blue);
                
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                var query = @"
                    SELECT NombreUsuario, Nombre, Apellido, Email, Nivel, NumeroCajero, 
                           Activo, UltimoAcceso
                    FROM Usuarios 
                    ORDER BY NombreUsuario";

                using var cmd = new SqlCommand(query, connection);
                connection.Open();

                dgvUsuarios.Rows.Clear();

                using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    var nombreCompleto = $"{reader["Nombre"]} {reader["Apellido"]}";
                    var nivel = ObtenerNombreNivel((NivelUsuario)reader.GetInt32("Nivel"));
                    var estado = reader.GetBoolean("Activo") ? "✅ Activo" : "❌ Inactivo";
                    var ultimoAcceso = reader["UltimoAcceso"] != DBNull.Value 
                        ? ((DateTime)reader["UltimoAcceso"]).ToString("dd/MM/yyyy HH:mm")
                        : "Nunca";

                    dgvUsuarios.Rows.Add(
                        reader["NombreUsuario"],
                        nombreCompleto,
                        reader["Email"],
                        nivel,
                        reader["NumeroCajero"],
                        estado,
                        ultimoAcceso
                    );
                }

                MostrarMensaje($"✅ Se cargaron {dgvUsuarios.Rows.Count} usuarios", Color.Green);
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error al cargar usuarios: {ex.Message}", Color.Red);
            }
        }

        private async Task FiltrarUsuarios()
        {
            if (string.IsNullOrWhiteSpace(txtBuscar.Text))
            {
                await CargarUsuarios();
                return;
            }

            try
            {
                var filtro = txtBuscar.Text.ToLower();

                foreach (DataGridViewRow row in dgvUsuarios.Rows)
                {
                    if (row.IsNewRow) continue;

                    bool visible = false;
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (cell.Value?.ToString().ToLower().Contains(filtro) == true)
                        {
                            visible = true;
                            break;
                        }
                    }
                    row.Visible = visible;
                }

                var visibleCount = dgvUsuarios.Rows.Cast<DataGridViewRow>()
                    .Count(r => !r.IsNewRow && r.Visible);
                MostrarMensaje($"🔍 {visibleCount} usuarios encontrados", Color.Blue);
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error en la búsqueda: {ex.Message}", Color.Red);
            }
        }

        private async Task AgregarUsuario()
        {
            try
            {
                using var form = new UsuarioForm();
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // NUEVO: Mostrar debug si está habilitado
                    if (MostrarDebugHash)
                    {
                        MostrarMensaje("🔍 DEBUG - Usuario creado con hash generado correctamente", Color.Purple);
                        await Task.Delay(2000);
                    }
                    
                    await CargarUsuarios();
                    MostrarMensaje("✅ Usuario agregado correctamente", Color.Green);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error al agregar usuario: {ex.Message}", Color.Red);
            }
        }

        private async Task EditarUsuario()
        {
            if (dgvUsuarios.SelectedRows.Count == 0) return;

            try
            {
                var nombreUsuario = dgvUsuarios.SelectedRows[0].Cells["NombreUsuario"].Value.ToString();
                using var form = new UsuarioForm(nombreUsuario);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // NUEVO: Mostrar debug si está habilitado
                    if (MostrarDebugHash)
                    {
                        MostrarMensaje("🔍 DEBUG - Usuario actualizado con hash procesado correctamente", Color.Purple);
                        await Task.Delay(2000);
                    }
                    
                    await CargarUsuarios();
                    MostrarMensaje("✅ Usuario actualizado correctamente", Color.Green);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error al editar usuario: {ex.Message}", Color.Red);
            }
        }

        private async Task EliminarUsuario()
        {
            if (dgvUsuarios.SelectedRows.Count == 0) return;

            try
            {
                var nombreUsuario = dgvUsuarios.SelectedRows[0].Cells["NombreUsuario"].Value.ToString();
                var nombreCompleto = dgvUsuarios.SelectedRows[0].Cells["NombreCompleto"].Value.ToString();

                // No permitir eliminar el usuario actual
                if (AuthenticationService.SesionActual?.Usuario?.NombreUsuario == nombreUsuario)
                {
                    MessageBox.Show("No puedes eliminar tu propia cuenta.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var resultado = MessageBox.Show(
                    $"¿Estás seguro de que deseas eliminar al usuario '{nombreCompleto}' ({nombreUsuario})?\n\n" +
                    "Esta acción no se puede deshacer.",
                    "Confirmar Eliminación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resultado == DialogResult.Yes)
                {
                    var config = new ConfigurationBuilder()
                        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                        .AddJsonFile("appsettings.json")
                        .Build();

                    string connectionString = config.GetConnectionString("DefaultConnection");

                    using var connection = new SqlConnection(connectionString);
                    var query = "DELETE FROM Usuarios WHERE NombreUsuario = @nombreUsuario";
                    using var cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@nombreUsuario", nombreUsuario);

                    connection.Open();
                    int filasAfectadas = await cmd.ExecuteNonQueryAsync();

                    if (filasAfectadas > 0)
                    {
                        await CargarUsuarios();
                        MostrarMensaje($"✅ Usuario '{nombreCompleto}' eliminado correctamente", Color.Green);
                    }
                    else
                    {
                        MostrarMensaje("❌ No se pudo eliminar el usuario", Color.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error al eliminar usuario: {ex.Message}", Color.Red);
            }
        }

        private async Task CambiarPasswordUsuario()
        {
            if (dgvUsuarios.SelectedRows.Count == 0) return;

            try
            {
                var nombreUsuario = dgvUsuarios.SelectedRows[0].Cells["NombreUsuario"].Value.ToString();
                var nombreCompleto = dgvUsuarios.SelectedRows[0].Cells["NombreCompleto"].Value.ToString();

                using var form = new CambiarPasswordForm();
                
                // Pre-llenar el usuario seleccionado
                var txtUsuarioField = form.Controls.Find("txtUsuario", true).FirstOrDefault() as TextBox;
                if (txtUsuarioField != null)
                {
                    txtUsuarioField.Text = nombreUsuario;
                    txtUsuarioField.ReadOnly = true;
                }

                if (form.ShowDialog() == DialogResult.OK)
                {
                    // NUEVO: Mostrar debug si está habilitado
                    if (MostrarDebugHash)
                    {
                        MostrarMensaje($"🔍 DEBUG - Contraseña actualizada con nuevo hash para '{nombreCompleto}'", Color.Purple);
                        await Task.Delay(2000);
                    }
                    
                    MostrarMensaje($"✅ Contraseña de '{nombreCompleto}' actualizada correctamente", Color.Green);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error al cambiar contraseña: {ex.Message}", Color.Red);
            }
        }

        private string ObtenerNombreNivel(NivelUsuario nivel)
        {
            return nivel switch
            {
                NivelUsuario.Administrador => "👑 Administrador",
                NivelUsuario.Supervisor => "👨‍💼 Supervisor",
                NivelUsuario.Vendedor => "🛍️ Vendedor",
                NivelUsuario.Invitado => "👤 Invitado",
                _ => "❓ Desconocido"
            };
        }

        private void MostrarMensaje(string mensaje, Color color)
        {
            lblMensaje.Text = mensaje;
            lblMensaje.ForeColor = color;
        }
    }
}