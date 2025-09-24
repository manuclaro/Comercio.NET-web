using Comercio.NET.Formularios;
using Comercio.NET.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comercio.NET
{
    public partial class MenuPrincipal : Form
    {
        private int childFormNumber = 0;
        
        // NUEVOS: Controles para información del usuario
        private ToolStripStatusLabel lblUsuarioActual;
        private ToolStripSplitButton btnCambiarUsuario;

        public MenuPrincipal()
        {
            InitializeComponent();
            ConfigurarInformacionUsuario(); // NUEVO: Configurar información del usuario
            ConfigurarMenuSegunPermisos();
        }

        // NUEVO: Método para configurar la información del usuario en el StatusStrip
        private void ConfigurarInformacionUsuario()
        {
            // Crear separador flexible para empujar los controles de usuario a la derecha
            var separadorFlexible = new ToolStripStatusLabel()
            {
                Spring = true,
                Text = ""
            };

            // Crear label para mostrar el usuario actual
            lblUsuarioActual = new ToolStripStatusLabel()
            {
                Text = "👤 No logueado",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                AutoSize = true,
                Margin = new Padding(0, 0, 10, 0)
            };

            // Crear botón para cambiar usuario
            btnCambiarUsuario = new ToolStripSplitButton()
            {
                Text = "⚙️",
                ToolTipText = "Opciones de usuario",
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };

            // Agregar opciones al menú del botón
            var menuCambiarUsuario = new ToolStripMenuItem("🔄 Cambiar Usuario", null, CambiarUsuario_Click);
            var menuCerrarSesion = new ToolStripMenuItem("🚪 Cerrar Sesión", null, CerrarSesion_Click);
            var separadorMenu = new ToolStripSeparator();
            var menuInfoUsuario = new ToolStripMenuItem("ℹ️ Info del Usuario", null, InfoUsuario_Click);

            btnCambiarUsuario.DropDownItems.Add(menuCambiarUsuario);
            btnCambiarUsuario.DropDownItems.Add(separadorMenu);
            btnCambiarUsuario.DropDownItems.Add(menuInfoUsuario);
            btnCambiarUsuario.DropDownItems.Add(new ToolStripSeparator());
            btnCambiarUsuario.DropDownItems.Add(menuCerrarSesion);

            // Agregar los controles al StatusStrip (insertando antes del estado existente)
            statusStrip.Items.Insert(0, separadorFlexible);
            statusStrip.Items.Insert(1, lblUsuarioActual);
            statusStrip.Items.Insert(2, btnCambiarUsuario);

            // Actualizar la información del usuario
            ActualizarInformacionUsuario();
        }

        // NUEVO: Método para actualizar la información del usuario mostrada
        private void ActualizarInformacionUsuario()
        {
            if (AuthenticationService.SesionActual?.Usuario != null)
            {
                var usuario = AuthenticationService.SesionActual.Usuario;
                lblUsuarioActual.Text = $"👤 {usuario.Nombre} {usuario.Apellido} ({usuario.NombreUsuario})";
                lblUsuarioActual.ForeColor = Color.FromArgb(76, 175, 80); // Verde para logueado
                
                // Mostrar nivel del usuario con color
                string nivelTexto = usuario.Nivel.ToString();
                Color colorNivel = usuario.Nivel switch
                {
                    Models.NivelUsuario.Administrador => Color.FromArgb(244, 67, 54), // Rojo
                    Models.NivelUsuario.Supervisor => Color.FromArgb(255, 152, 0),     // Naranja
                    Models.NivelUsuario.Vendedor => Color.FromArgb(33, 150, 243),      // Azul
                    Models.NivelUsuario.Invitado => Color.FromArgb(158, 158, 158),     // Gris
                    _ => Color.Black
                };
                
                lblUsuarioActual.Text += $" - {nivelTexto}";
                btnCambiarUsuario.Enabled = true;
            }
            else
            {
                lblUsuarioActual.Text = "👤 No logueado";
                lblUsuarioActual.ForeColor = Color.FromArgb(244, 67, 54); // Rojo para no logueado
                btnCambiarUsuario.Enabled = false;
            }
        }

        // NUEVO: Evento para cambiar usuario
        private void CambiarUsuario_Click(object sender, EventArgs e)
        {
            try
            {
                // Confirmar cambio de usuario
                var resultado = MessageBox.Show(
                    "¿Está seguro que desea cambiar de usuario?\n\nSe cerrarán todas las ventanas abiertas.",
                    "Cambiar Usuario",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resultado == DialogResult.Yes)
                {
                    // Cerrar todas las ventanas hijas
                    foreach (Form childForm in MdiChildren.ToArray())
                    {
                        childForm.Close();
                    }

                    // Cerrar sesión actual
                    var authService = new AuthenticationService();
                    authService.CerrarSesion();

                    // Mostrar formulario de login
                    using (var loginForm = new LoginForm())
                    {
                        var loginResult = loginForm.ShowDialog();
                        
                        if (loginResult == DialogResult.OK && loginForm.LoginExitoso)
                        {
                            // Login exitoso - actualizar información
                            ActualizarInformacionUsuario();
                            ConfigurarMenuSegunPermisos();
                            
                            MessageBox.Show($"Bienvenido {AuthenticationService.SesionActual.Usuario.Nombre}",
                                "Cambio de Usuario Exitoso",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        else
                        {
                            // Login cancelado o fallido - cerrar aplicación
                            MessageBox.Show("No se pudo cambiar de usuario. La aplicación se cerrará.",
                                "Cambio de Usuario Cancelado",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            
                            Application.Exit();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cambiar usuario: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // NUEVO: Evento para cerrar sesión
        private void CerrarSesion_Click(object sender, EventArgs e)
        {
            try
            {
                var resultado = MessageBox.Show(
                    "¿Está seguro que desea cerrar la sesión?\n\nLa aplicación se cerrará.",
                    "Cerrar Sesión",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resultado == DialogResult.Yes)
                {
                    // Cerrar sesión y salir
                    var authService = new AuthenticationService();
                    authService.CerrarSesion();
                    Application.Exit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cerrar sesión: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // NUEVO: Evento para mostrar información del usuario
        private void InfoUsuario_Click(object sender, EventArgs e)
        {
            try
            {
                if (AuthenticationService.SesionActual?.Usuario != null)
                {
                    var usuario = AuthenticationService.SesionActual.Usuario;
                    var sesion = AuthenticationService.SesionActual;

                    string info = $"INFORMACIÓN DEL USUARIO\n" +
                                 $"━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                                 $"👤 Usuario: {usuario.NombreUsuario}\n" +
                                 $"📝 Nombre: {usuario.Nombre} {usuario.Apellido}\n" +
                                 $"📧 Email: {usuario.Email ?? "No especificado"}\n" +
                                 $"🏷️ Nivel: {usuario.Nivel}\n" +
                                 $"💳 Cajero #: {usuario.NumeroCajero}\n" +
                                 $"📅 Creado: {usuario.FechaCreacion:dd/MM/yyyy HH:mm}\n" +
                                 $"🕒 Último acceso: {usuario.UltimoAcceso?.ToString("dd/MM/yyyy HH:mm") ?? "Primera vez"}\n" +
                                 $"🔐 Inicio sesión: {sesion.InicioSesion:dd/MM/yyyy HH:mm}\n" +
                                 $"⏰ Última actividad: {sesion.UltimaActividad:dd/MM/yyyy HH:mm}\n\n" +
                                 $"PERMISOS:\n" +
                                 $"━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                                 $"🗑️ Eliminar productos: {(usuario.PuedeEliminarProductos ? "✅" : "❌")}\n" +
                                 $"💰 Editar precios: {(usuario.PuedeEditarPrecios ? "✅" : "❌")}\n" +
                                 $"📊 Ver reportes: {(usuario.PuedeVerReportes ? "✅" : "❌")}\n" +
                                 $"👥 Gestionar usuarios: {(usuario.PuedeGestionarUsuarios ? "✅" : "❌")}\n" +
                                 $"❌ Anular facturas: {(usuario.PuedeAnularFacturas ? "✅" : "❌")}";

                    MessageBox.Show(info, "Información del Usuario", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al mostrar información: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // MODIFICADO: Actualizar también la información del usuario al cargar
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ActualizarInformacionUsuario();
        }

        private void ConfigurarMenuSegunPermisos()
        {
            // Verificar permisos del usuario actual para mostrar/ocultar opciones
            if (AuthenticationService.SesionActual?.Usuario != null)
            {
                var usuario = AuthenticationService.SesionActual.Usuario;
                
                // Solo mostrar gestión de usuarios si tiene permisos
                bool puedeGestionarUsuarios = usuario.Nivel == Models.NivelUsuario.Administrador || 
                                             usuario.PuedeGestionarUsuarios;
                
                // La opción de gestión de usuarios se agregará en el Designer
                if (!puedeGestionarUsuarios && gestionUsuariosToolStripMenuItem != null)
                {
                    gestionUsuariosToolStripMenuItem.Visible = false;
                    toolStripGestionUsuarios.Visible = false; // AGREGADO: También ocultar el botón de la toolbar
                }
                else
                {
                    if (gestionUsuariosToolStripMenuItem != null)
                    {
                        gestionUsuariosToolStripMenuItem.Visible = true;
                        toolStripGestionUsuarios.Visible = true;
                    }
                }
            }
        }

        private void ShowNewForm(object sender, EventArgs e)
        {
            Form childForm = new Form();
            childForm.MdiParent = this;
            childForm.Text = "Ventana " + childFormNumber++;
            childForm.Show();
        }

        private void OpenFile(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            openFileDialog.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = openFileDialog.FileName;
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            saveFileDialog.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = saveFileDialog.FileName;
            }
        }

        private void ExitToolsStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void ToolBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStrip.Visible = toolBarToolStripMenuItem.Checked;
        }

        private void StatusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            statusStrip.Visible = statusBarToolStripMenuItem.Checked;
        }

        private void CascadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void TileVerticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void TileHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void ArrangeIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.ArrangeIcons);
        }

        private void CloseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form childForm in MdiChildren)
            {
                childForm.Close();
            }
        }

        private void ventasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ventasForm = new Ventas();
            ventasForm.MdiParent = this;
            ventasForm.Show();
        }

        private void productosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var productosForm = new Productos();
            productosForm.MdiParent = this;
            productosForm.Show();
        }

        private void printPreviewToolStripButton_Click(object sender, EventArgs e)
        {
            var ventasForm = new Ventas();
            ventasForm.MdiParent = this;
            ventasForm.Show();
        }

        private void controlFacturasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ControlFacturasForm = new frmControlFacturas();
            ControlFacturasForm.MdiParent = this;
            ControlFacturasForm.Show();
        }

        private void toolStripProductos_Click(object sender, EventArgs e)
        {
            var productosForm = new Productos();
            productosForm.MdiParent = this;
            productosForm.Show();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            var ControlFacturasForm = new frmControlFacturas();
            ControlFacturasForm.MdiParent = this;
            ControlFacturasForm.Show();
        }

        // NUEVO: Método para abrir gestión de usuarios
        private void gestionUsuariosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar permisos antes de abrir
                if (AuthenticationService.SesionActual?.Usuario != null)
                {
                    var usuario = AuthenticationService.SesionActual.Usuario;
                    bool puedeGestionar = usuario.Nivel == Models.NivelUsuario.Administrador || 
                                         usuario.PuedeGestionarUsuarios;

                    if (!puedeGestionar)
                    {
                        MessageBox.Show("No tienes permisos para acceder a la gestión de usuarios.", 
                            "Acceso Denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Verificar si ya está abierto
                    foreach (Form form in this.MdiChildren)
                    {
                        if (form is GestionUsuariosMainForm)
                        {
                            form.Activate();
                            return;
                        }
                    }

                    // Abrir nuevo formulario
                    var gestionUsuariosForm = new GestionUsuariosMainForm();
                    gestionUsuariosForm.MdiParent = this;
                    gestionUsuariosForm.Show();
                }
                else
                {
                    MessageBox.Show("No hay una sesión activa.", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir gestión de usuarios: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NUEVO: Método para el botón de gestión de usuarios en la toolbar
        private void toolStripGestionUsuarios_Click(object sender, EventArgs e)
        {
            gestionUsuariosToolStripMenuItem_Click(sender, e);
        }
    }
}
