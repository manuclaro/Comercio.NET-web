using Comercio.NET.Formularios;
using Comercio.NET.Services;
using Comercio.NET;
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

            // NUEVO: Configurar ícono de configuración en tiempo de ejecución
            ConfigurarIconoConfiguracion();
            // llamar a la creación dinámica del menú y botón
            AgregarMenuProveedores();
            MoverCompraProveedoresAMenuProveedores();
        }

        // NUEVO: Método para configurar el ícono de configuración
        private void ConfigurarIconoConfiguracion()
        {
            try
            {
                if (toolStripConfiguracion != null)
                {
                    toolStripConfiguracion.Image = CrearIconoConfiguracionLocal();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configurando ícono: {ex.Message}");
                // Si hay error, seguir sin ícono
            }
        }


        // NUEVO: Método local para crear el ícono (copia del método de ConfiguracionForm)
        private Bitmap CrearIconoConfiguracionLocal()
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);

                // Dibujar una rueda dentada simple
                using (var brush = new SolidBrush(Color.Gray))
                {
                    // Centro del ícono
                    g.FillEllipse(brush, 6, 6, 4, 4);

                    // Dientes de la rueda
                    Rectangle[] dientes = {
                        new Rectangle(7, 2, 2, 3),   // Arriba
                        new Rectangle(11, 7, 3, 2),  // Derecha
                        new Rectangle(7, 11, 2, 3),  // Abajo
                        new Rectangle(2, 7, 3, 2)    // Izquierda
                    };

                    foreach (var diente in dientes)
                    {
                        g.FillRectangle(brush, diente);
                    }
                }
            }
            return bitmap;
        }

        private void ShowNewForm(object sender, EventArgs e)
        {
            Form childForm = new Form();
            childForm.MdiParent = this;
            childForm.Text = "Ventana " + childFormNumber++;
            childForm.Show();
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

                // CORREGIDO: Verificar que los controles existen antes de usarlos
                if (gestionUsuariosToolStripMenuItem != null)
                {
                    gestionUsuariosToolStripMenuItem.Visible = puedeGestionarUsuarios;
                }

                if (toolStripGestionUsuarios != null)
                {
                    toolStripGestionUsuarios.Visible = puedeGestionarUsuarios;
                }

                // Solo mostrar configuración a administradores
                bool esAdministrador = usuario.Nivel == Models.NivelUsuario.Administrador;

                if (InformesToolStripMenuItem != null)
                {
                    InformesToolStripMenuItem.Visible = esAdministrador;
                }

                if (toolStripConfiguracion != null)
                {
                    toolStripConfiguracion.Visible = esAdministrador;
                }
            }
            else
            {
                // Si no hay usuario logueado, ocultar opciones administrativas
                if (gestionUsuariosToolStripMenuItem != null)
                    gestionUsuariosToolStripMenuItem.Visible = false;
                if (toolStripGestionUsuarios != null)
                    toolStripGestionUsuarios.Visible = false;
                if (InformesToolStripMenuItem != null)
                    InformesToolStripMenuItem.Visible = false;
                if (toolStripConfiguracion != null)
                    toolStripConfiguracion.Visible = false;

                // NUEVO: También ocultar cartelitos si no hay usuario
                if (cartelitosToolStripMenuItem != null)
                    cartelitosToolStripMenuItem.Visible = false;
                if (toolStripCartelitos != null)
                    toolStripCartelitos.Visible = false;
            }
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

        // MODIFICADO: Verificar permisos antes de abrir productos
        private void productosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Verificar si el usuario tiene permisos para editar precios
            if (AuthenticationService.SesionActual?.Usuario != null)
            {
                var usuario = AuthenticationService.SesionActual.Usuario;

                // Solo permitir acceso si puede editar precios o es administrador
                if (usuario.PuedeEditarPrecios || usuario.Nivel == Models.NivelUsuario.Administrador)
                {
                    var productosForm = new ProductosOptimizado();
                    productosForm.MdiParent = this;
                    productosForm.Show();
                }
                else
                {
                    MessageBox.Show(
                        "⚠️ ACCESO DENEGADO\n\n" +
                        "No tienes permisos para acceder a la gestión de productos.\n\n" +
                        "Este módulo requiere el permiso 'Editar Precios'.\n" +
                        "Contacta a un administrador si necesitas acceso.",
                        "Permisos Insuficientes",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("No hay una sesión activa.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void printPreviewToolStripButton_Click(object sender, EventArgs e)
        {
            var ventasForm = new Ventas();
            ventasForm.MdiParent = this;
            ventasForm.Show();
        }

        // MODIFICADO: Verificar permisos antes de abrir control de facturas
        private void controlFacturasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Verificar si el usuario tiene permisos para ver reportes
            if (AuthenticationService.SesionActual?.Usuario != null)
            {
                var usuario = AuthenticationService.SesionActual.Usuario;

                // Solo permitir acceso si puede ver reportes o es administrador
                if (usuario.PuedeVerReportes || usuario.Nivel == Models.NivelUsuario.Administrador)
                {
                    var ControlFacturasForm = new frmControlFacturas();
                    ControlFacturasForm.MdiParent = this;
                    ControlFacturasForm.Show();
                }
                else
                {
                    MessageBox.Show(
                        "⚠️ ACCESO DENEGADO\n\n" +
                        "No tienes permisos para acceder al control de facturas.\n\n" +
                        "Este módulo requiere el permiso 'Ver Reportes'.\n" +
                        "Contacta a un administrador si necesitas acceso.",
                        "Permisos Insuficientes",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("No hay una sesión activa.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NUEVO: Método para abrir cartelitos de precios desde el menú
        private void cartelitosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar si hay una sesión activa
                if (AuthenticationService.SesionActual?.Usuario == null)
                {
                    MessageBox.Show("No hay una sesión activa.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Verificar si ya está abierto el formulario
                foreach (Form form in this.MdiChildren)
                {
                    if (form is CartelitosPrecios)
                    {
                        form.Activate();
                        return;
                    }
                }

                // Abrir nuevo formulario de cartelitos
                var cartelitosForm = new CartelitosPrecios();
                cartelitosForm.MdiParent = this;
                cartelitosForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir cartelitos de precios: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NUEVO: Método para el botón de cartelitos en la toolbar
        private void toolStripCartelitos_Click(object sender, EventArgs e)
        {
            cartelitosToolStripMenuItem_Click(sender, e);
        }

        // MODIFICADO: Verificar permisos antes de abrir productos desde toolbar
        private void toolStripProductos_Click(object sender, EventArgs e)
        {
            // Reutilizar la lógica del menú
            productosToolStripMenuItem_Click(sender, e);
        }

        // MODIFICADO: Verificar permisos antes de abrir control de facturas desde toolbar
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            // Reutilizar la lógica del menú
            controlFacturasToolStripMenuItem_Click(sender, e);
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

        // NUEVO: Método para abrir configuración del sistema
        private void configuracionSistemaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar permisos de administrador
                if (AuthenticationService.SesionActual?.Usuario != null)
                {
                    var usuario = AuthenticationService.SesionActual.Usuario;
                    bool esAdministrador = usuario.Nivel == Models.NivelUsuario.Administrador;

                    if (!esAdministrador)
                    {
                        MessageBox.Show("Solo los administradores pueden acceder a la configuración del sistema.",
                            "Acceso Denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Verificar si ya está abierto
                    foreach (Form form in this.MdiChildren)
                    {
                        if (form is ConfiguracionForm)
                        {
                            form.Activate();
                            return;
                        }
                    }

                    // Abrir formulario de configuración
                    var configuracionForm = new ConfiguracionForm();
                    configuracionForm.MdiParent = this;
                    configuracionForm.Show();
                }
                else
                {
                    MessageBox.Show("No hay una sesión activa.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir configuración: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NUEVO: Método para el botón de configuración en la toolbar
        private void toolStripConfiguracion_Click(object sender, EventArgs e)
        {
            configuracionSistemaToolStripMenuItem_Click(sender, e);
        }

        private void configuracionSistemaToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            var informesForm = new InformesForm();
            informesForm.MdiParent = this;
            informesForm.Show();
        }

        private void toolBotonCompras_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar si ya está abierto
                foreach (Form form in this.MdiChildren)
                {
                    if (form is Comercio.NET.Formularios.ComprasProveedorForm)
                    {
                        form.Activate();
                        return;
                    }
                }

                var comprasForm = new Comercio.NET.Formularios.ComprasProveedorForm();
                comprasForm.MdiParent = this;
                comprasForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo Compras Proveedores: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void compraProveedoresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar si ya está abierto
                foreach (Form form in this.MdiChildren)
                {
                    if (form is Comercio.NET.Formularios.ComprasProveedorForm)
                    {
                        form.Activate();
                        return;
                    }
                }

                var comprasForm = new Comercio.NET.Formularios.ComprasProveedorForm();
                comprasForm.MdiParent = this;
                comprasForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo Compras Proveedores: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MODIFICAR el método AgregarMenuProveedores existente:
        private void AgregarMenuProveedores()
        {
            try
            {
                if (this.menuStrip != null)
                {
                    // Evitar duplicados
                    if (!this.menuStrip.Items.Cast<ToolStripItem>().Any(i => string.Equals(i.Name, "proveedoresToolStripMenuItem", StringComparison.OrdinalIgnoreCase)))
                    {
                        var menu = new ToolStripMenuItem("Proveedores") { Name = "proveedoresToolStripMenuItem" };

                        var submenuAbm = new ToolStripMenuItem("ABM Proveedores", null, (s, e) =>
                        {
                            // abrir como MDI child
                            foreach (Form f in this.MdiChildren)
                                if (f is Comercio.NET.Formularios.ProveedoresForm) { f.Activate(); return; }

                            var frm = new Comercio.NET.Formularios.ProveedoresForm { MdiParent = this };
                            frm.Show();
                        })
                        { Name = "abmProveedoresToolStripMenuItem" };

                        // Intentar detectar si ya existe el item top-level "compraProveedoresToolStripMenuItem"
                        ToolStripItem existingCompraItem = this.menuStrip.Items
                            .Cast<ToolStripItem>()
                            .FirstOrDefault(i =>
                                string.Equals(i.Name, "compraProveedoresToolStripMenuItem", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(i.Text, "Compras Proveedores", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(i.Text, "Compra Proveedores", StringComparison.OrdinalIgnoreCase));

                        // Construir menú en el orden deseado:
                        // 1) ABM Proveedores
                        // 2) separador
                        // 3) Cargar Compra (existingCompraItem si existe)
                        // 4) separador
                        // 5) Control Compras
                        // 6) separador
                        // 7) Cuenta Corriente <- NUEVO
                        menu.DropDownItems.Add(submenuAbm);
                        menu.DropDownItems.Add(new ToolStripSeparator());

                        // Preparar el item "Control Compras" (siempre disponible)
                        var submenuControlCompras = new ToolStripMenuItem("Control Compras", null, ControlComprasToolStripMenuItem_Click)
                        {
                            Name = "controlComprasToolStripMenuItem"
                        };

                        // ✅ NUEVO: item para CtaCte Proveedores (SIN ÍCONO)
                        var submenuCtaCte = new ToolStripMenuItem("Cuenta Corriente Proveedores", null, CtaCteProveedoresToolStripMenuItem_Click)
                        {
                            Name = "ctaCteProveedoresToolStripMenuItem"
                            // Sin propiedad Image - no hay ícono
                        };

                        if (existingCompraItem != null)
                        {
                            // Si ya existe, removerlo de su ubicación actual y reubicarlo aquí (evita duplicados)
                            if (existingCompraItem.OwnerItem is ToolStripMenuItem ownerMenu)
                            {
                                ownerMenu.DropDownItems.Remove(existingCompraItem);
                            }
                            else
                            {
                                // era top-level
                                menuStrip.Items.Remove(existingCompraItem);
                            }

                            // Añadir "Cargar Compra" justo debajo del separador
                            menu.DropDownItems.Add(existingCompraItem);

                            // separador entre Cargar Compra y Control Compras
                            menu.DropDownItems.Add(new ToolStripSeparator());

                            // añadir Control Compras debajo de Cargar Compra
                            menu.DropDownItems.Add(submenuControlCompras);
                        }
                        else
                        {
                            // Si no existe el item "Cargar Compra" se añade sólo Control Compras
                            menu.DropDownItems.Add(submenuControlCompras);
                        }

                        // ✅ Añadir separador y luego el item CtaCte (colocado después de Control Compras)
                        menu.DropDownItems.Add(new ToolStripSeparator());
                        menu.DropDownItems.Add(submenuCtaCte);

                        // Insertar el menú "Proveedores" antes de "Ver" (viewMenu) si existe, para moverlo más a la izquierda
                        int insertIndex = -1;
                        if (menuStrip.Items.Contains(viewMenu))
                        {
                            insertIndex = menuStrip.Items.IndexOf(viewMenu);
                        }

                        if (insertIndex >= 0)
                            menuStrip.Items.Insert(insertIndex, menu);
                        else
                            this.menuStrip.Items.Add(menu);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error agregando menu Proveedores: {ex.Message}");
            }
        }

        // ✅ MODIFICAR el handler para abrir CtaCte con el Form contenedor correcto:
        private void CtaCteProveedoresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar si ya está abierto (con nombre correcto del Form)
                foreach (Form f in this.MdiChildren)
                {
                    if (f.Name == "FormCtaCteProveedores")
                    {
                        f.Activate();
                        return;
                    }
                }

                // Crear Form contenedor
                var form = new Form
                {
                    Text = "Cuenta Corriente Proveedores",
                    Name = "FormCtaCteProveedores",
                    MdiParent = this,
                    WindowState = FormWindowState.Maximized
                };

                // Agregar el control
                var control = new Comercio.NET.Formularios.ProveedoresCtaCteControl
                {
                    Dock = DockStyle.Fill
                };
                form.Controls.Add(control);
                form.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo Cuenta Corriente Proveedores: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ✅ AGREGAR: Método para abrir Control de Compras
        private void ControlComprasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar si ya está abierto
                foreach (Form f in this.MdiChildren)
                {
                    if (f is Comercio.NET.Formularios.ComprasProveedorControlForm)
                    {
                        f.Activate();
                        return;
                    }
                }

                // Crear y mostrar el formulario
                var controlForm = new Comercio.NET.Formularios.ComprasProveedorControlForm
                {
                    MdiParent = this
                };
                controlForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo Control Compras Proveedores: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ✅ AGREGAR: Método para mover "Compra Proveedores" al menú Proveedores
        private void MoverCompraProveedoresAMenuProveedores()
        {
            try
            {
                if (menuStrip == null) return;

                // Buscar el menú "Proveedores" (ya creado por AgregarMenuProveedores)
                var proveedoresMenuItem = menuStrip.Items
                    .OfType<ToolStripItem>()
                    .FirstOrDefault(i => string.Equals(i.Name, "proveedoresToolStripMenuItem", StringComparison.OrdinalIgnoreCase))
                    as ToolStripMenuItem;

                if (proveedoresMenuItem == null)
                {
                    // No existe el menú Proveedores todavía
                    return;
                }

                // Función recursiva para buscar el ToolStripItem por Name o por texto exacto "Compra Proveedores"
                ToolStripItem EncontrarItem(ToolStripItemCollection items)
                {
                    foreach (ToolStripItem it in items)
                    {
                        if (string.Equals(it.Name, "compraProveedoresToolStripMenuItem", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(it.Text, "Compra Proveedores", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(it.Text, "Compras Proveedores", StringComparison.OrdinalIgnoreCase))
                        {
                            return it;
                        }

                        if (it is ToolStripMenuItem mi && mi.DropDownItems.Count > 0)
                        {
                            var found = EncontrarItem(mi.DropDownItems);
                            if (found != null) return found;
                        }
                    }
                    return null;
                }

                // Buscar el item en todo el menuStrip
                var itemEncontrado = EncontrarItem(menuStrip.Items);
                if (itemEncontrado == null)
                {
                    // No se encontró; puede que ya esté en Proveedores o tenga otro nombre
                    System.Diagnostics.Debug.WriteLine("⚠️ No se encontró 'Compra Proveedores' para mover.");
                    return;
                }

                // Si ya está bajo el menú Proveedores, nada que hacer
                if (itemEncontrado.OwnerItem == proveedoresMenuItem)
                {
                    System.Diagnostics.Debug.WriteLine("✅ 'Compra Proveedores' ya está bajo 'Proveedores'.");
                    return;
                }

                // Remover del padre actual
                if (itemEncontrado.OwnerItem is ToolStripMenuItem ownerMenu)
                {
                    ownerMenu.DropDownItems.Remove(itemEncontrado);
                }
                else
                {
                    // Podría ser top-level
                    menuStrip.Items.Remove(itemEncontrado);
                }

                // Intentar insertar antes del item "Control Compras" si existe,
                // para que "Compra Proveedores" quede arriba y "Control Compras" abajo.
                var dropItems = proveedoresMenuItem.DropDownItems.Cast<ToolStripItem>().ToList();
                int indexControl = dropItems.FindIndex(i => string.Equals(i.Name, "controlComprasToolStripMenuItem", StringComparison.OrdinalIgnoreCase));

                if (indexControl >= 0)
                {
                    proveedoresMenuItem.DropDownItems.Insert(indexControl, itemEncontrado);
                }
                else
                {
                    // Si no existe "Control Compras", intentar insertar después de "ABM Proveedores"
                    int indexAbm = dropItems.FindIndex(i => string.Equals(i.Name, "abmProveedoresToolStripMenuItem", StringComparison.OrdinalIgnoreCase));
                    if (indexAbm >= 0)
                    {
                        // Insertar después del ABM y su separador
                        proveedoresMenuItem.DropDownItems.Insert(indexAbm + 2, itemEncontrado);
                    }
                    else
                    {
                        // fallback: añadir al final
                        proveedoresMenuItem.DropDownItems.Add(itemEncontrado);
                    }
                }

                System.Diagnostics.Debug.WriteLine("✅ 'Compra Proveedores' reubicado bajo 'Proveedores' (antes de Control Compras).");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error reubicando Compra Proveedores: {ex.Message}");
            }
        }
    }
}
