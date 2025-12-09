using Comercio.NET;
using Comercio.NET.Formularios;
using Comercio.NET.Services;
using Comercio.NET.Servicios;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Grpc.Core.Metadata;

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
            ConfigurarInformacionUsuario();
            ConfigurarMenuSegunPermisos();
            ConfigurarIconoConfiguracion();
            AgregarMenuProveedores();
            CrearMenuProductos(); // ✅ AGREGAR ESTA LÍNEA - crea el menú en la posición correcta
            MoverCompraProveedoresAMenuProveedores();
            AgregarMenuCaja();
            ConfigurarMenuCaja();
            AgregarBotonCierreTurnoToolbar();
            AgregarActualizacionRapidaAlMenu();
            AgregarOpcionGestionOfertas();
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

            // MEJORADO: Validación de configuración AFIP con mejor UX
            try
            {
                var (ambiente, cuit, wsaaUrl, wsfeUrl, certValido) = AfipAuthenticator.ObtenerInformacionAmbiente();

                // Verificar si hubo error al cargar la configuración
                if (ambiente == "Error" || string.IsNullOrWhiteSpace(cuit))
                {
                    System.Diagnostics.Debug.WriteLine("[AFIP] ⚠️ Configuración AFIP no disponible o incompleta");

                    var resultado = MessageBox.Show(
                        "⚠️ CONFIGURACIÓN AFIP NO DISPONIBLE\n\n" +
                        "No se pudo cargar la configuración de AFIP.\n" +
                        "Esto puede deberse a:\n\n" +
                        "• Archivo appsettings.json no encontrado\n" +
                        "• Sección AFIP mal configurada\n" +
                        "• CUIT no especificado\n" +
                        "• Certificado no encontrado\n\n" +
                        "¿Desea continuar sin facturación electrónica?\n\n" +
                        "NOTA: Las facturas electrónicas NO funcionarán hasta que configure AFIP correctamente.",
                        "Configuración AFIP",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (resultado == DialogResult.No)
                    {
                        // Ofrecer abrir la configuración
                        var abrirConfig = MessageBox.Show(
                            "¿Desea abrir la configuración del sistema para revisar los datos de AFIP?",
                            "Abrir Configuración",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (abrirConfig == DialogResult.Yes)
                        {
                            // Abrir configuración automáticamente si el usuario es administrador
                            if (AuthenticationService.SesionActual?.Usuario?.Nivel == Models.NivelUsuario.Administrador)
                            {
                                var configuracionForm = new ConfiguracionForm();
                                configuracionForm.MdiParent = this;
                                configuracionForm.Show();
                            }
                            else
                            {
                                MessageBox.Show(
                                    "Solo los administradores pueden acceder a la configuración del sistema.\n\n" +
                                    "Por favor, contacte a un administrador para revisar la configuración de AFIP.",
                                    "Acceso Restringido",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                            }
                        }
                    }

                    return; // No continuar con las validaciones de certificado si no hay configuración
                }

                // Si llegamos aquí, la configuración básica está OK
                string icono = ambiente == "Testing" ? "🧪" : "🏭";
                string estado = certValido ? "✅" : "⚠️";

                System.Diagnostics.Debug.WriteLine($"{icono} AFIP configurado en modo: {ambiente}");
                System.Diagnostics.Debug.WriteLine($"{estado} Certificado: {(certValido ? "Válido" : "Requiere atención")}");
                System.Diagnostics.Debug.WriteLine($"📡 CUIT: {cuit}");

                // Solo mostrar advertencia de certificado si la configuración está completa
                if (!certValido)
                {
                    var resultado = MessageBox.Show(
                        $"⚠️ ADVERTENCIA: CERTIFICADO AFIP\n\n" +
                        $"Ambiente: {ambiente}\n" +
                        $"CUIT: {cuit}\n" +
                        $"Estado del certificado: No válido o próximo a vencer\n\n" +
                        $"¿Desea continuar de todos modos?\n\n" +
                        $"Nota: La facturación electrónica podría no funcionar correctamente.",
                        "Advertencia Certificado AFIP",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (resultado == DialogResult.No)
                    {
                        MessageBox.Show(
                            "Por favor, verifique la configuración AFIP en el menú Configuración " +
                            "y asegúrese de que el certificado sea válido y no esté expirado.\n\n" +
                            "Ruta esperada del certificado:\n" +
                            (wsaaUrl.Contains("homo") ?
                                "C:\\Certificados\\testing.p12" :
                                "C:\\Certificados\\produccion.p12"),
                            "Certificado Requerido",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
                else
                {
                    // NUEVO: Mostrar mensaje de éxito en Debug si todo está OK
                    System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Configuración validada correctamente");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Sistema listo para facturación electrónica en ambiente {ambiente}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] 💥 Error crítico en validación: {ex.Message}");

                MessageBox.Show(
                    $"❌ ERROR AL VALIDAR CONFIGURACIÓN AFIP\n\n" +
                    $"Detalles del error:\n{ex.Message}\n\n" +
                    $"La aplicación se iniciará, pero la facturación electrónica\n" +
                    $"NO estará disponible hasta que se corrija este problema.\n\n" +
                    $"Revise el archivo appsettings.json en la carpeta de la aplicación.",
                    "Error de Configuración",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        //private void ConfigurarMenuSegunPermisos()
        //{
        //    // Verificar permisos del usuario actual para mostrar/ocultar opciones
        //    if (AuthenticationService.SesionActual?.Usuario != null)
        //    {
        //        var usuario = AuthenticationService.SesionActual.Usuario;

        //        // Solo mostrar gestión de usuarios si tiene permisos
        //        bool puedeGestionarUsuarios = usuario.Nivel == Models.NivelUsuario.Administrador ||
        //                                     usuario.PuedeGestionarUsuarios;

        //        // CORREGIDO: Verificar que los controles existen antes de usarlos
        //        if (gestionUsuariosToolStripMenuItem != null)
        //        {
        //            gestionUsuariosToolStripMenuItem.Visible = puedeGestionarUsuarios;
        //        }

        //        if (toolStripGestionUsuarios != null)
        //        {
        //            toolStripGestionUsuarios.Visible = puedeGestionarUsuarios;
        //        }

        //        // Solo mostrar configuración a administradores
        //        bool esAdministrador = usuario.Nivel == Models.NivelUsuario.Administrador;

        //        if (InformesToolStripMenuItem != null)
        //        {
        //            InformesToolStripMenuItem.Visible = esAdministrador;
        //        }

        //        if (toolStripConfiguracion != null)
        //        {
        //            toolStripConfiguracion.Visible = esAdministrador;
        //        }
        //    }
        //    else
        //    {
        //        // Si no hay usuario logueado, ocultar opciones administrativas
        //        if (gestionUsuariosToolStripMenuItem != null)
        //            gestionUsuariosToolStripMenuItem.Visible = false;
        //        if (toolStripGestionUsuarios != null)
        //            toolStripGestionUsuarios.Visible = false;
        //        if (InformesToolStripMenuItem != null)
        //            InformesToolStripMenuItem.Visible = false;
        //        if (toolStripConfiguracion != null)
        //            toolStripConfiguracion.Visible = false;

        //        // NUEVO: También ocultar cartelitos si no hay usuario
        //        if (cartelitosToolStripMenuItem != null)
        //            cartelitosToolStripMenuItem.Visible = false;
        //        if (toolStripCartelitos != null)
        //            toolStripCartelitos.Visible = false;
        //    }
        //}

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
            if (!ventasForm.InicializacionExitosa)
            {
                ventasForm.Dispose();
                return;
            }

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

        // MODIFICADO el método AgregarMenuProveedores existente:
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

                        // Construir menú en el orden deseado
                        menu.DropDownItems.Add(submenuAbm);
                        menu.DropDownItems.Add(new ToolStripSeparator());

                        // Preparar el item "Control Compras"
                        var submenuControlCompras = new ToolStripMenuItem("Control Compras", null, ControlComprasToolStripMenuItem_Click)
                        {
                            Name = "controlComprasToolStripMenuItem"
                        };

                        // Item para CtaCte Proveedores
                        var submenuCtaCte = new ToolStripMenuItem("Cuenta Corriente Proveedores", null, CtaCteProveedoresToolStripMenuItem_Click)
                        {
                            Name = "ctaCteProveedoresToolStripMenuItem"
                        };

                        if (existingCompraItem != null)
                        {
                            if (existingCompraItem.OwnerItem is ToolStripMenuItem ownerMenu)
                            {
                                ownerMenu.DropDownItems.Remove(existingCompraItem);
                            }
                            else
                            {
                                menuStrip.Items.Remove(existingCompraItem);
                            }

                            menu.DropDownItems.Add(existingCompraItem);
                            menu.DropDownItems.Add(new ToolStripSeparator());
                            menu.DropDownItems.Add(submenuControlCompras);
                        }
                        else
                        {
                            menu.DropDownItems.Add(submenuControlCompras);
                        }

                        menu.DropDownItems.Add(new ToolStripSeparator());
                        menu.DropDownItems.Add(submenuCtaCte);

                        // ✅ MODIFICADO: Insertar "Proveedores" antes de "Ver" (viewMenu)
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

        // ✅ NUEVO: Método para crear el menú "Productos" en la posición correcta
        private void CrearMenuProductos()
        {
            try
            {
                if (this.menuStrip == null) return;

                // Verificar si ya existe el menú "Productos"
                var productosMenuExistente = this.menuStrip.Items
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(i => i.Name == "productosToolStripMenuItem");

                if (productosMenuExistente != null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ El menú 'Productos' ya existe, no se creará de nuevo.");
                    return;
                }

                // ✅ CREAR el menú "Productos" como menú principal (SIN evento Click directo)
                var menuProductos = new ToolStripMenuItem("Productos")
                {
                    Name = "productosToolStripMenuItem"
                    // ❌ NO asignar Click aquí - será un menú con submenús
                };

                // ========================================
                // CREAR SUBMENÚS DE PRODUCTOS
                // ========================================

                // 1. ABM Productos
                var itemAbmProductos = new ToolStripMenuItem("📦 ABM Productos", null, productosToolStripMenuItem_Click)
                {
                    Name = "abmProductosToolStripMenuItem",
                    ToolTipText = "Administración de productos (Alta, Baja, Modificación)"
                };

                // 2. Actualización Rápida (ya existe, lo agregamos aquí también)
                var itemActualizacionRapida = new ToolStripMenuItem("⚡ Actualización Rápida Precio/Stock", null, ActualizacionRapida_Click)
                {
                    Name = "actualizacionRapidaToolStripMenuItem",
                    ShortcutKeys = Keys.Control | Keys.Shift | Keys.P,
                    ToolTipText = "Actualización rápida de precios y stock (Ctrl+Shift+P)"
                };

                // 3. Ofertas y Combos (ya existe, lo agregamos aquí también)
                var itemOfertas = new ToolStripMenuItem("🎁 Ofertas y Combos", null, (s, e) => AbrirGestionOfertas())
                {
                    Name = "ofertasYCombosToolStripMenuItem",
                    ToolTipText = "Gestionar ofertas y descuentos por cantidad"
                };

                // ========================================
                // AGREGAR LOS SUBMENÚS AL MENÚ PRODUCTOS
                // ========================================
                menuProductos.DropDownItems.Add(itemAbmProductos);
                menuProductos.DropDownItems.Add(new ToolStripSeparator());
                menuProductos.DropDownItems.Add(itemActualizacionRapida);
                menuProductos.DropDownItems.Add(new ToolStripSeparator());
                menuProductos.DropDownItems.Add(itemOfertas);

                // ✅ CALCULAR la posición correcta: después de "Proveedores"
                int insertIndex = -1;

                // Buscar el menú "Proveedores"
                var proveedoresMenu = this.menuStrip.Items
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(i => i.Name == "proveedoresToolStripMenuItem");

                if (proveedoresMenu != null)
                {
                    // Insertar justo después de "Proveedores"
                    insertIndex = this.menuStrip.Items.IndexOf(proveedoresMenu) + 1;
                }
                else
                {
                    // Si no existe "Proveedores", buscar "Ver" para insertar antes
                    var verMenu = this.menuStrip.Items
                        .OfType<ToolStripMenuItem>()
                        .FirstOrDefault(i => i.Name == "viewMenu");

                    if (verMenu != null)
                    {
                        insertIndex = this.menuStrip.Items.IndexOf(verMenu);
                    }
                }

                // ✅ INSERTAR en la posición correcta
                if (insertIndex >= 0 && insertIndex <= this.menuStrip.Items.Count)
                {
                    this.menuStrip.Items.Insert(insertIndex, menuProductos);
                    System.Diagnostics.Debug.WriteLine($"✅ Menú 'Productos' creado en posición {insertIndex} con submenús");
                }
                else
                {
                    // Fallback: agregar al final
                    this.menuStrip.Items.Add(menuProductos);
                    System.Diagnostics.Debug.WriteLine("⚠️ Menú 'Productos' agregado al final (fallback)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error creando menú Productos: {ex.Message}");
            }
        }

        // En el método ConfigurarMenuSegunPermisos() o donde tengas tus opciones de menú
        private void cierreTurnoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar permisos
                if (AuthenticationService.SesionActual?.Usuario != null)
                {
                    var usuario = AuthenticationService.SesionActual.Usuario;
                    
                    // Solo supervisores y administradores pueden cerrar turnos
                    if (usuario.Nivel != Models.NivelUsuario.Administrador && 
                        usuario.Nivel != Models.NivelUsuario.Supervisor)
                    {
                        MessageBox.Show(
                            "⚠️ ACCESO DENEGADO\n\n" +
                            "Solo los supervisores y administradores pueden acceder al cierre de turnos.",
                            "Permisos Insuficientes",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    // Verificar si ya está abierto
                    foreach (Form form in this.MdiChildren)
                    {
                        if (form is Comercio.NET.Formularios.CierreTurnoCajeroForm)
                        {
                            form.Activate();
                            return;
                        }
                    }

                    // Abrir formulario
                    var cierreForm = new Comercio.NET.Formularios.CierreTurnoCajeroForm();
                    cierreForm.MdiParent = this;
                    cierreForm.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo cierre de turno: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Busca en el Designer o agrega dinámicamente:
        private void AgregarMenuCaja()
        {
            try
            {
                if (this.menuStrip == null) return;

                // Verificar si ya existe el menú Caja
                var menuCajaExistente = this.menuStrip.Items
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(i => string.Equals(i.Name, "cajaToolStripMenuItem", StringComparison.OrdinalIgnoreCase));

                if (menuCajaExistente == null)
                {
                    // ========================================
                    // CREAR MENÚ PRINCIPAL "CAJA"
                    // ========================================
                    var menuCaja = new ToolStripMenuItem("💰 Caja")
                    {
                        Name = "cajaToolStripMenuItem"
                    };

                    // ========================================
                    // 1. APERTURA DE TURNO ✅ AHORA HABILITADO
                    // ========================================
                    var itemAperturaTurno = new ToolStripMenuItem("🔓 Apertura de Turno", null, (s, e) => AbrirAperturaTurno())
                    {
                        Name = "aperturaTurnoToolStripMenuItem",
                        Enabled = true, // ✅ HABILITADO
                        ShortcutKeys = Keys.Control | Keys.Shift | Keys.A,
                        ToolTipText = "Abrir turno de cajero (Ctrl+Shift+A)"
                    };

                    // ========================================
                    // 2. CIERRE DE TURNO ✅ HABILITADO
                    // ========================================
                    var itemCierreTurno = new ToolStripMenuItem("💵 Cierre de Turno", null, cierreTurnoToolStripMenuItem_Click)
                    {
                        Name = "cierreTurnoToolStripMenuItem",
                        Enabled = true, // ✅ HABILITADO
                        ShortcutKeys = Keys.Control | Keys.Shift | Keys.T,
                        ToolTipText = "Realizar cierre de turno del cajero (Ctrl+Shift+T)"
                    };

                    // ========================================
                    // 3. SEPARADOR
                    // ========================================
                    var separador1 = new ToolStripSeparator();

                    // ========================================
                    // 4. HISTORIAL DE CIERRES ✅ AHORA HABILITADO
                    // ========================================
                    var itemHistorialCierres = new ToolStripMenuItem("📊 Historial de Cierres", null, (s, e) => AbrirHistorialCierres())
                    {
                        Name = "historialCierresToolStripMenuItem",
                        Enabled = true, // ✅ HABILITADO
                        ToolTipText = "Consultar historial de cierres de turno"
                    };

                    // ========================================
                    // 5. ARQUEO DE CAJA (Deshabilitado - futuro)
                    // ========================================
                    var itemArqueoCaja = new ToolStripMenuItem("📋 Arqueo de Caja", null, (s, e) => AbrirArqueoCaja())
                    {
                        Name = "arqueoCajaToolStripMenuItem",
                        Enabled = true, // ✅ HABILITADO
                        ToolTipText = "Realizar arqueo de caja (verificación sin cerrar turno)"
                    };

                    // ========================================
                    // AGREGAR TODOS LOS ITEMS AL MENÚ CAJA
                    // ========================================
                    menuCaja.DropDownItems.Add(itemAperturaTurno);      // ✅ Habilitado
                    menuCaja.DropDownItems.Add(itemCierreTurno);        // ✅ Habilitado
                    menuCaja.DropDownItems.Add(separador1);             // ────────
                    menuCaja.DropDownItems.Add(itemHistorialCierres);   // ✅ Habilitado
                    menuCaja.DropDownItems.Add(itemArqueoCaja);         // 🔒 Deshabilitado

                    // ========================================
                    // INSERTAR EL MENÚ EN LA POSICIÓN CORRECTA
                    // ========================================
                    int insertIndex = -1;

                    // Buscar el menú "Ventas" para insertar después
                    var ventasMenu = this.menuStrip.Items
                        .OfType<ToolStripMenuItem>()
                        .FirstOrDefault(i => i.Text.Contains("Ventas") || i.Text.Contains("Facturación"));

                    if (ventasMenu != null)
                    {
                        insertIndex = this.menuStrip.Items.IndexOf(ventasMenu) + 1;
                    }
                    else
                    {
                        // Si no se encuentra "Ventas", buscar "Proveedores" para insertar antes
                        var proveedoresMenu = this.menuStrip.Items
                            .OfType<ToolStripMenuItem>()
                            .FirstOrDefault(i => string.Equals(i.Name, "proveedoresToolStripMenuItem", StringComparison.OrdinalIgnoreCase));

                        if (proveedoresMenu != null)
                        {
                            insertIndex = this.menuStrip.Items.IndexOf(proveedoresMenu);
                        }
                    }

                    // Insertar en la posición correcta o al principio como fallback
                    if (insertIndex >= 0 && insertIndex <= this.menuStrip.Items.Count)
                    {
                        this.menuStrip.Items.Insert(insertIndex, menuCaja);
                        System.Diagnostics.Debug.WriteLine($"✅ Menú 'Caja' agregado en posición {insertIndex}");
                    }
                    else
                    {
                        // Fallback: agregar después del menú File (índice 1 o 2)
                        this.menuStrip.Items.Insert(Math.Min(2, this.menuStrip.Items.Count), menuCaja);
                        System.Diagnostics.Debug.WriteLine("✅ Menú 'Caja' agregado en posición fallback");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ℹ️ Menú 'Caja' ya existe");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error agregando menú Caja: {ex.Message}");
                MessageBox.Show($"Error al configurar menú Caja: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AgregarActualizacionRapidaAlMenu()
        {
            try
            {
                if (this.menuStrip == null) return;

                // Buscar el menú "Productos" existente
                var productosMenuItem = this.menuStrip.Items
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(i => i.Text.Contains("Productos"));

                if (productosMenuItem != null)
                {
                    // Verificar si ya existe el item
                    var existeItem = productosMenuItem.DropDownItems
                        .OfType<ToolStripMenuItem>()
                        .Any(i => i.Name == "actualizacionRapidaToolStripMenuItem");

                    if (existeItem)
                    {
                        System.Diagnostics.Debug.WriteLine("ℹ️ Item 'Actualización Rápida' ya existe en el menú Productos");
                        return; // ✅ Ya existe, no agregarlo de nuevo
                    }

                    // Agregar separador si no existe
                    if (productosMenuItem.DropDownItems.Count > 0 &&
                        !(productosMenuItem.DropDownItems[productosMenuItem.DropDownItems.Count - 1] is ToolStripSeparator))
                    {
                        productosMenuItem.DropDownItems.Add(new ToolStripSeparator());
                    }

                    // Crear el item de Actualización Rápida
                    var itemActualizacionRapida = new ToolStripMenuItem("⚡ Actualización Rápida Precio/Stock", null, ActualizacionRapida_Click)
                    {
                        Name = "actualizacionRapidaToolStripMenuItem",
                        ShortcutKeys = Keys.Control | Keys.Shift | Keys.P,
                        ToolTipText = "Actualización rápida de precios y stock (Ctrl+Shift+P)"
                    };

                    productosMenuItem.DropDownItems.Add(itemActualizacionRapida);
                    System.Diagnostics.Debug.WriteLine("✅ Item 'Actualización Rápida' agregado al menú Productos");
                }

                // También agregar botón a la toolbar
                AgregarBotonActualizacionRapidaToolbar();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error agregando actualización rápida al menú: {ex.Message}");
            }
        }

        private void ActualizacionRapida_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar permisos
                if (AuthenticationService.SesionActual?.Usuario != null)
                {
                    var usuario = AuthenticationService.SesionActual.Usuario;

                    if (usuario.PuedeEditarPrecios || usuario.Nivel == Models.NivelUsuario.Administrador)
                    {
                        using (var form = new Comercio.NET.Formularios.ActualizacionRapidaForm())
                        {
                            form.ShowDialog(this);
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            "⚠️ ACCESO DENEGADO\n\n" +
                            "No tienes permisos para actualizar precios.\n\n" +
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
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir actualización rápida: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Bitmap CrearIconoActualizacionRapida()
        {
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Dibujar rayo (símbolo de rapidez)
                using (var brush = new SolidBrush(Color.FromArgb(255, 152, 0)))
                {
                    Point[] lightning = new Point[]
                    {
                new Point(18, 4),
                new Point(12, 14),
                new Point(16, 14),
                new Point(10, 28),
                new Point(16, 16),
                new Point(12, 16)
                    };
                    g.FillPolygon(brush, lightning);
                }

                // Dibujar etiqueta de precio
                using (var pen = new Pen(Color.FromArgb(255, 152, 0), 2))
                using (var brush = new SolidBrush(Color.FromArgb(255, 224, 178)))
                {
                    var rect = new Rectangle(20, 18, 10, 12);
                    g.FillRectangle(brush, rect);
                    g.DrawRectangle(pen, rect);

                    using (var font = new Font("Arial", 7F, FontStyle.Bold))
                    using (var textBrush = new SolidBrush(Color.FromArgb(230, 81, 0)))
                    {
                        g.DrawString("$", font, textBrush, 22, 20);
                    }
                }
            }
            return bitmap;
        }

        private void AgregarBotonActualizacionRapidaToolbar()
        {
            try
            {
                if (this.toolStrip == null) return;

                // Verificar si ya existe el botón
                var botonExistente = this.toolStrip.Items
                    .OfType<ToolStripButton>()
                    .FirstOrDefault(b => b.Name == "toolStripActualizacionRapida");

                if (botonExistente == null)
                {
                    var btnActualizacionRapida = new ToolStripButton
                    {
                        Name = "toolStripActualizacionRapida",
                        Text = "⚡ Act. Rápida",
                        ToolTipText = "Actualización Rápida de Precios y Stock (Ctrl+Shift+P)",
                        DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                        ImageTransparentColor = System.Drawing.Color.Magenta,
                        TextImageRelation = TextImageRelation.ImageBeforeText
                    };

                    btnActualizacionRapida.Image = CrearIconoActualizacionRapida();
                    btnActualizacionRapida.Click += (s, e) => ActualizacionRapida_Click(s, e);

                    // Buscar posición después del botón de Productos
                    int insertIndex = -1;
                    var btnProductos = this.toolStrip.Items
                        .OfType<ToolStripButton>()
                        .FirstOrDefault(b => b.Name == "toolStripProductos");

                    if (btnProductos != null)
                    {
                        insertIndex = this.toolStrip.Items.IndexOf(btnProductos) + 1;
                    }

                    if (insertIndex >= 0 && insertIndex < this.toolStrip.Items.Count)
                    {
                        this.toolStrip.Items.Insert(insertIndex, btnActualizacionRapida);
                    }
                    else
                    {
                        this.toolStrip.Items.Add(btnActualizacionRapida);
                    }

                    System.Diagnostics.Debug.WriteLine("✅ Botón 'Actualización Rápida' agregado a la toolbar");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error agregando botón actualización rápida a toolbar: {ex.Message}");
            }
        }

        // ESTE MÉTODO YA ESTÁ CORRECTO EN TU MenuPrincipal.cs
        private void AbrirArqueoCaja()
        {
            try
            {
                // Verificar permisos
                if (AuthenticationService.SesionActual?.Usuario != null)
                {
                    var usuario = AuthenticationService.SesionActual.Usuario;

                    // Solo supervisores y administradores
                    if (usuario.Nivel != Models.NivelUsuario.Administrador &&
                        usuario.Nivel != Models.NivelUsuario.Supervisor)
                    {
                        MessageBox.Show(
                            "⚠️ ACCESO DENEGADO\n\n" +
                            "Solo los supervisores y administradores pueden realizar arqueos de caja.",
                            "Permisos Insuficientes",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    // Verificar si ya está abierto
                    foreach (Form form in this.MdiChildren)
                    {
                        if (form is ArqueoCajaForm)
                        {
                            form.Activate();
                            return;
                        }
                    }

                    // ✅ Abrir como hijo del MDI
                    var arqueoForm = new ArqueoCajaForm();
                    arqueoForm.MdiParent = this;
                    arqueoForm.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo arqueo de caja: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NUEVO: Método para agregar botón de Cierre Turno a la toolbar
        private void AgregarBotonCierreTurnoToolbar()
        {
            try
            {
                if (this.toolStrip == null) return;

                // Verificar si ya existe el botón
                var botonExistente = this.toolStrip.Items
                    .OfType<ToolStripButton>()
                    .FirstOrDefault(b => b.Name == "toolStripCierreTurno");

                if (botonExistente == null)
                {
                    // Crear el botón de cierre de turno
                    var btnCierreTurno = new ToolStripButton
                    {
                        Name = "toolStripCierreTurno",
                        Text = "💵 Cierre Turno",
                        ToolTipText = "Cierre de Turno de Cajero (Ctrl+Shift+T)",
                        DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                        ImageTransparentColor = System.Drawing.Color.Magenta,
                        TextImageRelation = TextImageRelation.ImageBeforeText
                    };

                    // Crear ícono personalizado para el botón
                    btnCierreTurno.Image = CrearIconoCierreTurno();

                    // Asignar el evento Click
                    btnCierreTurno.Click += (s, e) => cierreTurnoToolStripMenuItem_Click(s, e);

                    // Buscar la posición ideal: después del botón de Control Facturas
                    int insertIndex = -1;
                    
                    // Intentar encontrar el botón de Control Facturas
                    var btnControlFacturas = this.toolStrip.Items
                        .OfType<ToolStripButton>()
                        .FirstOrDefault(b => b.Name == "toolStripButton1" || b.Text.Contains("Facturas"));

                    if (btnControlFacturas != null)
                    {
                        insertIndex = this.toolStrip.Items.IndexOf(btnControlFacturas) + 1;
                    }
                    else
                    {
                        // Si no se encuentra, buscar el botón de Ventas
                        var btnVentas = this.toolStrip.Items
                            .OfType<ToolStripButton>()
                            .FirstOrDefault(b => b.Name == "printPreviewToolStripButton" || b.Text.Contains("Venta"));

                        if (btnVentas != null)
                        {
                            insertIndex = this.toolStrip.Items.IndexOf(btnVentas) + 1;
                        }
                    }

                    // Insertar en la posición correcta
                    if (insertIndex >= 0 && insertIndex < this.toolStrip.Items.Count)
                    {
                        // Agregar separador antes del botón si no existe
                        if (!(this.toolStrip.Items[insertIndex - 1] is ToolStripSeparator))
                        {
                            this.toolStrip.Items.Insert(insertIndex, new ToolStripSeparator());
                            insertIndex++;
                        }
                        
                        this.toolStrip.Items.Insert(insertIndex, btnCierreTurno);
                    }
                    else
                    {
                        // Fallback: agregar al final
                        this.toolStrip.Items.Add(new ToolStripSeparator());
                        this.toolStrip.Items.Add(btnCierreTurno);
                    }

                    System.Diagnostics.Debug.WriteLine("✅ Botón 'Cierre Turno' agregado a la toolbar");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error agregando botón Cierre Turno a toolbar: {ex.Message}");
            }
        }

        // NUEVO: Método para crear ícono de Cierre Turno
        private Bitmap CrearIconoCierreTurno()
        {
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Dibujar billete de dinero (rectángulo con bordes redondeados)
                using (var pen = new Pen(Color.FromArgb(76, 175, 80), 2))
                using (var brush = new SolidBrush(Color.FromArgb(200, 230, 201)))
                {
                    // Fondo del billete
                    var rect = new Rectangle(4, 8, 24, 16);
                    g.FillRectangle(brush, rect);
                    g.DrawRectangle(pen, rect);

                    // Símbolo de dinero ($) en el centro
                    using (var fontBold = new Font("Arial", 10F, FontStyle.Bold))
                    using (var brushText = new SolidBrush(Color.FromArgb(27, 94, 32)))
                    {
                        var textRect = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
                        var format = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };
                        g.DrawString("$", fontBold, brushText, textRect, format);
                    }

                    // Detalle de esquinas (círculos pequeños)
                    using (var brushCircle = new SolidBrush(Color.FromArgb(139, 195, 74)))
                    {
                        g.FillEllipse(brushCircle, 6, 10, 4, 4);
                        g.FillEllipse(brushCircle, 22, 10, 4, 4);
                        g.FillEllipse(brushCircle, 6, 18, 4, 4);
                        g.FillEllipse(brushCircle, 22, 18, 4, 4);
                    }
                }

                // Dibujar calculadora o reloj pequeño en la esquina inferior derecha
                using (var penClock = new Pen(Color.FromArgb(33, 150, 243), 1.5f))
                using (var brushClock = new SolidBrush(Color.FromArgb(227, 242, 253)))
                {
                    // Fondo del reloj
                    g.FillEllipse(brushClock, 19, 20, 10, 10);
                    g.DrawEllipse(penClock, 19, 20, 10, 10);

                    // Manecillas del reloj
                    g.DrawLine(penClock, 24, 25, 24, 22); // Manecilla de hora
                    g.DrawLine(penClock, 24, 25, 26, 25); // Manecilla de minuto
                }
            }
            return bitmap;
        }

        // ✅ OPCIONAL: Método para actualizar visibilidad del botón según permisos
        // Agrégalo dentro de ConfigurarMenuSegunPermisos():
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

                // ✅ NUEVO: Controlar visibilidad del menú Caja según permisos
                bool puedeCerrarTurnos = usuario.Nivel == Models.NivelUsuario.Administrador ||
                                        usuario.Nivel == Models.NivelUsuario.Supervisor;

                // Menú Caja en menuStrip
                var menuCaja = this.menuStrip?.Items
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(i => i.Name == "cajaToolStripMenuItem");

                if (menuCaja != null)
                {
                    menuCaja.Visible = puedeCerrarTurnos;
                }

                // Botón Cierre Turno en toolbar
                var btnCierreTurno = this.toolStrip?.Items
                    .OfType<ToolStripButton>()
                    .FirstOrDefault(b => b.Name == "toolStripCierreTurno");

                if (btnCierreTurno != null)
                {
                    btnCierreTurno.Visible = puedeCerrarTurnos;
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

                // ✅ NUEVO: Ocultar menú Caja si no hay sesión
                var menuCaja = this.menuStrip?.Items
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(i => i.Name == "cajaToolStripMenuItem");

                if (menuCaja != null)
                    menuCaja.Visible = false;

                // Ocultar botón toolbar
                var btnCierreTurno = this.toolStrip?.Items
                    .OfType<ToolStripButton>()
                    .FirstOrDefault(b => b.Name == "toolStripCierreTurno");

                if (btnCierreTurno != null)
                    btnCierreTurno.Visible = false;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Atajo para Cierre de Turno: Ctrl+Shift+T
            if (keyData == (Keys.Control | Keys.Shift | Keys.T))
            {
                cierreTurnoToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }

            // AGREGAR: Atajo para Actualización Rápida: Ctrl+Shift+P
            if (keyData == (Keys.Control | Keys.Shift | Keys.P))
            {
                ActualizacionRapida_Click(this, EventArgs.Empty);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // NUEVO: Método para configurar el menú Caja y sus eventos
        private void ConfigurarMenuCaja()
        {
            // Habilitar el menú Caja si corresponde
            var menuCaja = this.menuStrip?.Items
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(i => i.Name == "cajaToolStripMenuItem");

            if (menuCaja != null)
            {
                bool puedeCerrarTurnos = AuthenticationService.SesionActual?.Usuario != null &&
                                        (AuthenticationService.SesionActual.Usuario.Nivel == Models.NivelUsuario.Administrador ||
                                         AuthenticationService.SesionActual.Usuario.Nivel == Models.NivelUsuario.Supervisor);

                menuCaja.Visible = puedeCerrarTurnos;

                // Para el item de "Apertura de Turno"
                var aperturaTurnoItem = menuCaja.DropDownItems
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(x => x.Text.Contains("Apertura de Turno"));
                
                if (aperturaTurnoItem != null)
                {
                    aperturaTurnoItem.Enabled = true; // Habilitar el menú
                    aperturaTurnoItem.Click += (s, e) => AbrirAperturaTurno();
                }

                // NUEVO: Para el item de "Historial de Cierres"
                var historialCierresItem = menuCaja.DropDownItems
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(x => x.Text.Contains("Historial de Cierres"));
                
                if (historialCierresItem != null)
                {
                    historialCierresItem.Enabled = true; // Habilitar el menú
                    historialCierresItem.Click += (s, e) => AbrirHistorialCierres();
                }
            }
        }

        // MODIFICAR: Método para abrir el formulario de Apertura de Turno COMO HIJO DEL MDI
        private void AbrirAperturaTurno()
        {
            try
            {
                // Verificar si ya está abierto
                foreach (Form form in this.MdiChildren)
                {
                    if (form is AperturaTurnoCajeroForm)
                    {
                        form.Activate();
                        return;
                    }
                }

                // Abrir como hijo del MDI
                var formApertura = new AperturaTurnoCajeroForm();
                formApertura.MdiParent = this;
                formApertura.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo formulario: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MODIFICAR: Método para abrir el historial de cierres COMO HIJO DEL MDI
        private void AbrirHistorialCierres()
        {
            try
            {
                // Verificar si ya está abierto
                foreach (Form form in this.MdiChildren)
                {
                    if (form is HistorialCierresForm)
                    {
                        form.Activate();
                        return;
                    }
                }

                // Abrir como hijo del MDI
                var formHistorial = new HistorialCierresForm();
                formHistorial.MdiParent = this;
                formHistorial.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo historial: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // AGREGAR en el método ConfigurarMenu() o donde tengas los botones/menús:
        private void AgregarOpcionGestionOfertas()
        {
            try
            {
                if (this.menuStrip == null) return;

                // Buscar el menú "Productos" para agregar la opción ahí
                var productosMenuItem = this.menuStrip.Items
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(i => i.Text.Contains("Productos"));

                if (productosMenuItem != null)
                {
                    // Verificar si ya existe el item
                    var existeItem = productosMenuItem.DropDownItems
                        .OfType<ToolStripMenuItem>()
                        .Any(i => i.Name == "ofertasYCombosToolStripMenuItem");

                    if (existeItem)
                    {
                        System.Diagnostics.Debug.WriteLine("ℹ️ Item 'Ofertas y Combos' ya existe en el menú Productos");
                        return; // ✅ Ya existe, no agregarlo de nuevo
                    }

                    // Agregar separador si no existe
                    if (productosMenuItem.DropDownItems.Count > 0 &&
                        !(productosMenuItem.DropDownItems[productosMenuItem.DropDownItems.Count - 1] is ToolStripSeparator))
                    {
                        productosMenuItem.DropDownItems.Add(new ToolStripSeparator());
                    }

                    // Crear el item de Ofertas y Combos
                    var menuOfertas = new ToolStripMenuItem
                    {
                        Name = "ofertasYCombosToolStripMenuItem",
                        Text = "🎁 Ofertas y Combos",
                        ToolTipText = "Gestionar ofertas y descuentos por cantidad"
                    };
                    menuOfertas.Click += (s, e) => AbrirGestionOfertas();

                    productosMenuItem.DropDownItems.Add(menuOfertas);
                    System.Diagnostics.Debug.WriteLine("✅ Item 'Ofertas y Combos' agregado al menú Productos");
                }
                else
                {
                    // Si no existe el menú Productos, crearlo (esto no debería suceder con CrearMenuProductos)
                    System.Diagnostics.Debug.WriteLine("⚠️ Menú 'Productos' no encontrado");
                }

                // También agregar botón a la toolbar
                AgregarBotonOfertasToolbar();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error agregando ofertas al menú: {ex.Message}");
            }
        }

        //// ✅ NUEVO: Crear menú Productos si no existe
        //private void CrearMenuProductosConOfertas()
        //{
        //    try
        //    {
        //        var menuProductos = new ToolStripMenuItem("Productos")
        //        {
        //            Name = "productosToolStripMenuItem"
        //        };

        //        // ABM Productos
        //        var itemAbmProductos = new ToolStripMenuItem("ABM Productos", null, productosToolStripMenuItem_Click)
        //        {
        //            Name = "abmProductosToolStripMenuItem"
        //        };

        //        // Ofertas
        //        var itemOfertas = new ToolStripMenuItem("🎁 Ofertas y Combos", null, (s, e) => AbrirGestionOfertas())
        //        {
        //            Name = "ofertasYCombosToolStripMenuItem"
        //        };

        //        menuProductos.DropDownItems.Add(itemAbmProductos);
        //        menuProductos.DropDownItems.Add(new ToolStripSeparator());
        //        menuProductos.DropDownItems.Add(itemOfertas);

        //        // Insertar después del menú "Ventas" si existe
        //        int insertIndex = -1;
        //        var ventasMenu = this.menuStrip.Items
        //            .OfType<ToolStripMenuItem>()
        //            .FirstOrDefault(i => i.Text.Contains("Ventas"));

        //        if (ventasMenu != null)
        //        {
        //            insertIndex = this.menuStrip.Items.IndexOf(ventasMenu) + 1;
        //        }

        //        if (insertIndex >= 0 && insertIndex <= this.menuStrip.Items.Count)
        //        {
        //            this.menuStrip.Items.Insert(insertIndex, menuProductos);
        //        }
        //        else
        //        {
        //            this.menuStrip.Items.Add(menuProductos);
        //        }

        //        System.Diagnostics.Debug.WriteLine("✅ Menú 'Productos' creado con Ofertas");
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"⚠️ Error creando menú Productos: {ex.Message}");
        //    }
        //}

        // ✅ NUEVO: Agregar botón a la toolbar
        private void AgregarBotonOfertasToolbar()
        {
            try
            {
                if (this.toolStrip == null) return;

                // Verificar si ya existe el botón
                var botonExistente = this.toolStrip.Items
                    .OfType<ToolStripButton>()
                    .FirstOrDefault(b => b.Name == "toolStripOfertas");

                if (botonExistente == null)
                {
                    var btnOfertas = new ToolStripButton
                    {
                        Name = "toolStripOfertas",
                        Text = "🎁 Ofertas",
                        ToolTipText = "Gestión de Ofertas y Combos",
                        DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                        ImageTransparentColor = System.Drawing.Color.Magenta,
                        TextImageRelation = TextImageRelation.ImageBeforeText
                    };

                    btnOfertas.Image = CrearIconoOfertas();
                    btnOfertas.Click += (s, e) => AbrirGestionOfertas();

                    // Buscar posición después del botón de Productos
                    int insertIndex = -1;
                    var btnProductos = this.toolStrip.Items
                        .OfType<ToolStripButton>()
                        .FirstOrDefault(b => b.Name == "toolStripProductos");

                    if (btnProductos != null)
                    {
                        insertIndex = this.toolStrip.Items.IndexOf(btnProductos) + 1;
                    }

                    if (insertIndex >= 0 && insertIndex < this.toolStrip.Items.Count)
                    {
                        this.toolStrip.Items.Insert(insertIndex, btnOfertas);
                    }
                    else
                    {
                        this.toolStrip.Items.Add(btnOfertas);
                    }

                    System.Diagnostics.Debug.WriteLine("✅ Botón 'Ofertas' agregado a la toolbar");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error agregando botón ofertas a toolbar: {ex.Message}");
            }
        }

        // ✅ NUEVO: Crear ícono para ofertas
        private Bitmap CrearIconoOfertas()
        {
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Dibujar etiqueta de oferta (forma de etiqueta de precio)
                using (var brush = new SolidBrush(Color.FromArgb(255, 152, 0)))
                using (var pen = new Pen(Color.FromArgb(230, 81, 0), 2))
                {
                    // Cuerpo de la etiqueta
                    Point[] tag = new Point[]
                    {
                new Point(8, 8),
                new Point(24, 8),
                new Point(28, 16),
                new Point(24, 24),
                new Point(8, 24),
                new Point(4, 16)
                    };
                    g.FillPolygon(brush, tag);
                    g.DrawPolygon(pen, tag);

                    // Agujero de la etiqueta
                    using (var brushHole = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(brushHole, 20, 14, 4, 4);
                    }

                    // Símbolo de porcentaje (%)
                    using (var font = new Font("Arial", 10F, FontStyle.Bold))
                    using (var brushText = new SolidBrush(Color.White))
                    {
                        g.DrawString("%", font, brushText, 8, 12);
                    }
                }
            }
            return bitmap;
        }

        // ✅ MÉTODO PRINCIPAL: Abrir formulario de gestión de ofertas
        private void AbrirGestionOfertas()
        {
            try
            {
                // Verificar permisos
                if (AuthenticationService.SesionActual?.Usuario != null)
                {
                    var usuario = AuthenticationService.SesionActual.Usuario;

                    // Solo administradores o usuarios con permiso de editar precios
                    if (usuario.PuedeEditarPrecios || usuario.Nivel == Models.NivelUsuario.Administrador)
                    {
                        // Verificar si ya está abierto
                        foreach (Form form in this.MdiChildren)
                        {
                            if (form is GestionOfertasForm)
                            {
                                form.Activate();
                                return;
                            }
                        }

                        // Abrir como hijo del MDI
                        var formOfertas = new GestionOfertasForm();
                        formOfertas.MdiParent = this;
                        formOfertas.Show();
                    }
                    else
                    {
                        MessageBox.Show(
                            "⚠️ ACCESO DENEGADO\n\n" +
                            "No tienes permisos para gestionar ofertas.\n\n" +
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
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir gestión de ofertas: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
