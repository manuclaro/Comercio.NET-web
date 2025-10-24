using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Linq;
using System.Collections.Generic;

namespace Comercio.NET.Formularios
{
    public partial class ConfiguracionForm : Form
    {
        // Controles del formulario
        private TextBox txtNombreComercio, txtDomicilioComercio, txtConnectionString;
        // Nuevos controles para facturación
        private TextBox txtRazonSocial, txtCUIT, txtIngBrutos, txtDomicilioFiscal, txtCodigoPostal;
        private DateTimePicker dtpInicioActividades;
        private ComboBox cmbCondicion;
        
        // NUEVO: Controles para Cuentas Corrientes
        private ListBox lstNombresCtaCte;
        private TextBox txtNuevoNombreCtaCte;
        private Button btnAgregarNombre, btnEliminarNombre, btnEditarNombre;
        
        // NUEVO: Controles para configuración AFIP
        private TextBox txtAfipCuit, txtAfipCertificadoPath, txtAfipCertificadoPassword;
        private TextBox txtAfipWSAAUrl, txtAfipWSFEUrl;
        private Button btnColapsarAfip, btnSeleccionarCertificado, btnVerificarCertificado;
        private Panel panelAfip;
        
        // NUEVO: Controles para restricciones de impresión
        private Panel panelRestriccionesImpresion;
        private CheckBox chkRestringirRemitoPorPago;
        private Button btnColapsarRestriccionesImpresion;
        
        private Button btnGuardar, btnCancelar, btnTestearConexion, btnEditarBaseDatos;
        private Button btnColapsarComercio, btnColapsarFacturacion, btnColapsarInventario, btnColapsarBaseDatos, btnColapsarCuentasCorrientes;
        private CheckBox chkVerificarStock;
        private Label lblMensaje;
        private Panel panelPrincipal, panelComercio, panelFacturacion, panelBaseDatos, panelInventario, panelCuentasCorrientes;
        
        private string _rutaAppsettings;
        private JObject _configuracionOriginal;

        // NUEVO: Opciones para permitir Factura A / Factura B
        private CheckBox chkPermitirFacturaA;
        private CheckBox chkPermitirFacturaB;

        // Estados de colapso para cada sección
        private bool _comercioColapsado = false;
        private bool _facturacionColapsada = false;
        private bool _inventarioColapsado = false;
        private bool _cuentasCorrientesColapsado = false; // NUEVO
        private bool _baseDatosColapsada = true;
        private bool _edicionBaseDatosHabilitada = false;
        private bool _afipColapsado = false; // NUEVO
        private bool _restriccionesImpresionColapsado = false; // NUEVO

        // NUEVO: Variable para ToolTip
        private ToolTip toolTip;

        public ConfiguracionForm()
        {
            System.Diagnostics.Debug.WriteLine("[CONFIG] Iniciando ConfiguracionForm");
            
            InitializeComponent();

            
            // NUEVO: Inicializar ToolTip
            toolTip = new ToolTip();
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 1000;
            toolTip.ReshowDelay = 500;
            toolTip.ShowAlways = true;
            
            ConfigurarFormulario();
            
            // Para producción - usar el archivo donde se ejecuta
            _rutaAppsettings = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            System.Diagnostics.Debug.WriteLine($"[CONFIG] Ruta appsettings: {_rutaAppsettings}");
            
            if (!VerificarPermisosEscritura())
            {
                MessageBox.Show(
                    "⚠️ ADVERTENCIA: La aplicación no tiene permisos para escribir en el archivo de configuración.\n\n" +
                    "Es posible que los cambios no se guarden correctamente.\n\n" +
                    "Ejecute la aplicación como administrador si es necesario.",
                    "Permisos Insuficientes",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            
            CargarConfiguracion();
            System.Diagnostics.Debug.WriteLine("[CONFIG] Constructor completado");
        }

        // NUEVO: Métodos auxiliares para crear controles
        private Label CrearLabel(string texto, int x, int y)
        {
            return new Label
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(62, 80, 100),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private Panel CrearSeccionFacturacionColapsable(string titulo, int y, int ancho)
        {
            var panel = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(ancho, 35),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Header con título y botón colapsar
            var panelHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(ancho, 30),
                BackColor = Color.FromArgb(230, 235, 240),
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(panelHeader);

            var lblTitulo = new Label
            {
                Text = titulo,
                Location = new Point(10, 6),
                Size = new Size(ancho - 50, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Cursor = Cursors.Hand
            };
            panelHeader.Controls.Add(lblTitulo);

            // Botón colapsar/expandir
            btnColapsarFacturacion = new Button
            {
                Text = "▼", // Iniciar expandido
                Location = new Point(ancho - 35, 3),
                Size = new Size(25, 24),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            btnColapsarFacturacion.FlatAppearance.BorderSize = 0;
            panelHeader.Controls.Add(btnColapsarFacturacion);

            // Contenido colapsable con todos los campos de facturación
            var panelContenido = new Panel
            {
                Name = "panelContenidoFacturacion",
                Location = new Point(0, 30),
                Size = new Size(ancho, 180), // Aumentada ligeramente para alojar checkboxes
                BackColor = Color.FromArgb(248, 250, 252),
                Visible = true // Iniciar expandido
            };
            panel.Controls.Add(panelContenido);

            // Agregar campos de facturación al contenido
            // Primera fila: Razón Social y CUIT
            panelContenido.Controls.Add(CrearLabel("Razón Social:", 15, 10));
            txtRazonSocial = CrearTextBox(120, 8, 200);
            panelContenido.Controls.Add(txtRazonSocial);

            // CUIT: etiqueta corta, alineada a la derecha
            var lblCUIT = new Label
            {
                Name = "lblCUIT",
                Text = "CUIT:",
                Location = new Point(340, 10),
                Size = new Size(44, 22),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(62, 80, 100),
                TextAlign = ContentAlignment.MiddleRight
            };
            panelContenido.Controls.Add(lblCUIT);

            txtCUIT = new TextBox
            {
                Name = "txtCUIT",
                Location = new Point(390, 8),
                Size = new Size(170, 22),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "XX-XXXXXXXX-X"
            };
            panelContenido.Controls.Add(txtCUIT);

            // Segunda fila: Ingresos Brutos y Condición
            panelContenido.Controls.Add(CrearLabel("Ing. Brutos:", 15, 40));
            txtIngBrutos = CrearTextBox(120, 38, 200);
            panelContenido.Controls.Add(txtIngBrutos);

            var lblCondicion = new Label
            {
                Name = "lblCondicion",
                Text = "Cond.:",
                Location = new Point(340, 40),
                Size = new Size(44, 22),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(62, 80, 100),
                TextAlign = ContentAlignment.MiddleRight
            };
            panelContenido.Controls.Add(lblCondicion);

            cmbCondicion = new ComboBox
            {
                Name = "cmbCondicion",
                Location = new Point(390, 38),
                Size = new Size(170, 22),
                Font = new Font("Segoe UI", 9F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCondicion.Items.AddRange(new string[] {
        "Responsable Inscripto",
        "Monotributo",
        "Exento",
        "Consumidor Final",
        "Responsable No Inscripto"
    });
            panelContenido.Controls.Add(cmbCondicion);

            // Tercera fila: Domicilio Fiscal
            panelContenido.Controls.Add(CrearLabel("Domicilio Fiscal:", 15, 70));
            txtDomicilioFiscal = CrearTextBox(120, 68, 440);
            panelContenido.Controls.Add(txtDomicilioFiscal);

            // Cuarta fila: Código Postal e Inicio de Actividades
            panelContenido.Controls.Add(CrearLabel("C.P.:", 15, 100));
            txtCodigoPostal = CrearTextBox(120, 98, 100);
            panelContenido.Controls.Add(txtCodigoPostal);

            panelContenido.Controls.Add(CrearLabelLarga("Inicio de Actividades:", 330, 100));
            dtpInicioActividades = new DateTimePicker
            {
                Location = new Point(460, 98),
                Size = new Size(100, 22),
                Font = new Font("Segoe UI", 9F),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now
            };
            panelContenido.Controls.Add(dtpInicioActividades);

            // NUEVO: Checkboxes para permitir Factura A / Factura B (espacio sin solapamiento)
            chkPermitirFacturaA = new CheckBox
            {
                Name = "chkPermitirFacturaA",
                Text = "Permitir Factura A",
                Location = new Point(15, 130),
                Size = new Size(160, 22),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(62, 80, 100),
                Checked = true
            };
            panelContenido.Controls.Add(chkPermitirFacturaA);

            chkPermitirFacturaB = new CheckBox
            {
                Name = "chkPermitirFacturaB",
                Text = "Permitir Factura B",
                Location = new Point(190, 130),
                Size = new Size(160, 22),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(62, 80, 100),
                Checked = true
            };
            panelContenido.Controls.Add(chkPermitirFacturaB);

            // Ajuste final: aumentar altura visible del panel si es necesario
            panelContenido.Height = 160 + 20;

            // Configurar eventos
            EventHandler clickHandler = (s, e) => ToggleColapsarFacturacion();
            panelHeader.Click += clickHandler;
            lblTitulo.Click += clickHandler;

            return panel;
        }

        private Label CrearLabelCorta(string texto, int x, int y)
        {
            return new Label
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(130, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(62, 80, 100),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private Label CrearLabelLarga(string texto, int x, int y)
        {
            return new Label
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(62, 80, 100),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private TextBox CrearTextBox(int x, int y, int ancho)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(ancho, 22),
                Font = new Font("Segoe UI", 9F)
            };
        }

        /// <summary>
        /// NUEVO: Verifica si se puede escribir en el archivo appsettings.json
        /// </summary>
        private bool VerificarPermisosEscritura()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[CONFIG] Verificando permisos de escritura");
                
                // Intentar escribir un archivo temporal
                string testFile = _rutaAppsettings + ".test";
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                
                System.Diagnostics.Debug.WriteLine("[CONFIG] Permisos de escritura OK");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Error permisos: {ex.Message}");
                MostrarMensaje($"❌ Sin permisos de escritura: {ex.Message}", Color.Red);
                return false;
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ConfiguracionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new Size(650, 530); // AUMENTADO: Aumentar altura para la nueva sección
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfiguracionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configuración del Sistema";
            this.ResumeLayout(false);
        }

        // Añadir en la clase del formulario (por ejemplo, justo después de InitializeComponent o en una región de utilidades)
        private void AjustarEtiquetasDatosFacturacion()
        {
            try
            {
                // Reemplaza los nombres de controles por los que uses en tu form:
                // txtCUIT       -> TextBox (o control) donde se muestra el CUIT
                // cboCondicion  -> ComboBox (o control) de condición ante IVA
                // lblCUIT       -> Label de "CUIT"
                // lblCondicion  -> Label de "Cond." (o como se llame)

                if (this.Controls.Find("txtCUIT", true).FirstOrDefault() is Control txtCUIT &&
                    this.Controls.Find("cboCondicion", true).FirstOrDefault() is Control cboCondicion &&
                    this.Controls.Find("lblCUIT", true).FirstOrDefault() is Label lblCUIT &&
                    this.Controls.Find("lblCondicion", true).FirstOrDefault() is Label lblCondicion)
                {
                    // Fijar ancho cómodo para etiquetas y alinear texto a la derecha
                    int labelWidth = 70; // ajustar (60..90) según fuentes. Prueba localmente.
                    int espacio = 6;     // separación entre label y control

                    lblCUIT.AutoSize = false;
                    lblCUIT.Size = new Size(labelWidth, txtCUIT.Height);
                    lblCUIT.TextAlign = ContentAlignment.MiddleRight;
                    lblCUIT.Anchor = AnchorStyles.Left;
                    lblCUIT.Location = new Point(Math.Max(6, txtCUIT.Left - labelWidth - espacio), txtCUIT.Top);

                    lblCondicion.AutoSize = false;
                    lblCondicion.Size = new Size(labelWidth, cboCondicion.Height);
                    lblCondicion.TextAlign = ContentAlignment.MiddleRight;
                    lblCondicion.Anchor = AnchorStyles.Left;
                    lblCondicion.Location = new Point(Math.Max(6, cboCondicion.Left - labelWidth - espacio), cboCondicion.Top);
                }
            }
            catch
            {
                // No bloquear la inicialización si falla el ajuste automático
            }
        }

        private void ConfigurarFormulario()
        {
            this.Text = "⚙️ Configuración del Sistema";
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = new Font("Segoe UI", 9F);

            CrearControles();
            ConfigurarEventos();
        }

        private void CrearControles()
        {
            int currentY = 15;
            int margin = 20;
            int panelWidth = 610;

            // Título
            var lblTitulo = new Label
            {
                Text = "⚙️ Configuración General del Sistema",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(margin, currentY),
                Size = new Size(panelWidth, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitulo);
            currentY += 40;

            // Panel principal contenedor - AUMENTAR ALTURA
            panelPrincipal = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(panelWidth, 400), // AUMENTADO: más altura para la nueva sección
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };
            this.Controls.Add(panelPrincipal);

            // === CREAR TODAS LAS SECCIONES SIN POSICIONAMIENTO FIJO ===
            CrearTodasLasSecciones(panelWidth - 30);

            currentY += 400; // AUMENTADO: Ajustar para el panel más grande

            // Mensaje de estado
            lblMensaje = new Label
            {
                Location = new Point(margin, currentY),
                Size = new Size(panelWidth, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblMensaje);
            currentY += 30;

            // Botones
            CrearBotones(currentY, panelWidth);

            // Configurar TabIndex
            ConfigurarTabIndex();

            this.AcceptButton = btnGuardar;
            this.CancelButton = btnCancelar;

            // Inicializar estados y posiciones
            ActualizarPosicionesTodasLasSecciones();
        }

        private void CrearTodasLasSecciones(int ancho)
        {
            // === SECCIÓN COMERCIO ===
            panelComercio = CrearSeccionColapsable("🏪 INFORMACIÓN DEL COMERCIO", 0, ancho, "Comercio");
            panelPrincipal.Controls.Add(panelComercio);
            
            // Agregar controles al contenido del comercio
            var panelContenidoComercio = panelComercio.Controls["panelContenidoComercio"] as Panel;
            panelContenidoComercio.Controls.Add(CrearLabelCorta("Nombre del Comercio:", 15, 10));
            txtNombreComercio = CrearTextBox(150, 8, 410);
            panelContenidoComercio.Controls.Add(txtNombreComercio);

            panelContenidoComercio.Controls.Add(CrearLabelCorta("Domicilio:", 15, 40));
            txtDomicilioComercio = CrearTextBox(150, 38, 410);
            panelContenidoComercio.Controls.Add(txtDomicilioComercio);

            // === SECCIÓN FACTURACIÓN ===
            panelFacturacion = CrearSeccionFacturacionColapsable("📄 DATOS DE FACTURACIÓN", 0, ancho);
            panelPrincipal.Controls.Add(panelFacturacion);

            // === NUEVA SECCIÓN AFIP ===
            panelAfip = CrearSeccionAfipColapsable("🔐 CONFIGURACIÓN AFIP", 0, ancho);
            panelPrincipal.Controls.Add(panelAfip);

            // === NUEVA SECCIÓN: RESTRICCIONES DE IMPRESIÓN ===
            panelRestriccionesImpresion = CrearSeccionRestriccionesImpresionColapsable("🚫 RESTRICCIONES DE IMPRESIÓN", 0, ancho);
            panelPrincipal.Controls.Add(panelRestriccionesImpresion);

            // === SECCIÓN INVENTARIO ===
            panelInventario = CrearSeccionColapsable("📦 CONFIGURACIÓN DE INVENTARIO", 0, ancho, "Inventario");
            panelPrincipal.Controls.Add(panelInventario);

            // Agregar controles al contenido del inventario
            var panelContenidoInventario = panelInventario.Controls["panelContenidoInventario"] as Panel;
            chkVerificarStock = new CheckBox
            {
                Text = "Verificar disponibilidad de stock antes de realizar ventas",
                Location = new Point(15, 10),
                Size = new Size(570, 25),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(62, 80, 100),
                Checked = true
            };
            panelContenidoInventario.Controls.Add(chkVerificarStock);

            var lblDescripcion = new Label
            {
                Text = "Cuando está habilitado, el sistema impedirá ventas si no hay stock suficiente.",
                Location = new Point(35, 35),
                Size = new Size(540, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray
            };
            panelContenidoInventario.Controls.Add(lblDescripcion);

            // === SECCIÓN CUENTAS CORRIENTES ===
            panelCuentasCorrientes = CrearSeccionCuentasCorrientesColapsable("💳 CUENTAS CORRIENTES", 0, ancho);
            panelPrincipal.Controls.Add(panelCuentasCorrientes);

            // === SECCIÓN BASE DE DATOS ===
            panelBaseDatos = CrearSeccionBaseDatos("🗄️ BASE DE DATOS", 0, ancho);
            panelPrincipal.Controls.Add(panelBaseDatos);
        }

        // NUEVO: Crear sección de AFIP - AUMENTAR ALTURA
        private Panel CrearSeccionAfipColapsable(string titulo, int y, int ancho)
        {
            var panel = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(ancho, 35),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Header con título y botón colapsar
            var panelHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(ancho, 30),
                BackColor = Color.FromArgb(230, 235, 240),
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(panelHeader);

            var lblTitulo = new Label
            {
                Text = titulo,
                Location = new Point(10, 6),
                Size = new Size(ancho - 50, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Cursor = Cursors.Hand
            };
            panelHeader.Controls.Add(lblTitulo);

            // Botón colapsar/expandir
            btnColapsarAfip = new Button
            {
                Text = "▼", // Iniciar expandido
                Location = new Point(ancho - 35, 3),
                Size = new Size(25, 24),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            btnColapsarAfip.FlatAppearance.BorderSize = 0;
            panelHeader.Controls.Add(btnColapsarAfip);

            // Contenido colapsable
            var panelContenido = new Panel
            {
                Name = "panelContenidoAfip",
                Location = new Point(0, 30),
                Size = new Size(ancho, 200), // Altura para acomodar todos los campos
                BackColor = Color.FromArgb(248, 250, 252),
                Visible = true // Iniciar expandido
            };
            panel.Controls.Add(panelContenido);

            // === CAMPOS DE AFIP ===

            // Primera fila: CUIT AFIP
            panelContenido.Controls.Add(CrearLabel("CUIT AFIP:", 15, 10));
            txtAfipCuit = new TextBox
            {
                Location = new Point(120, 8),
                Size = new Size(150, 22),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "XX-XXXXXXXX-X"
            };
            panelContenido.Controls.Add(txtAfipCuit);

            // Segunda fila: Certificado
            panelContenido.Controls.Add(CrearLabel("Certificado (.p12):", 15, 40));
            txtAfipCertificadoPath = new TextBox
            {
                Location = new Point(120, 38),
                Size = new Size(350, 22),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "C:\\ruta\\al\\certificado.p12",
                ReadOnly = true,
                BackColor = Color.FromArgb(250, 250, 250)
            };
            panelContenido.Controls.Add(txtAfipCertificadoPath);

            btnSeleccionarCertificado = new Button
            {
                Text = "📁",
                Location = new Point(480, 38),
                Size = new Size(30, 22),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F),
                UseVisualStyleBackColor = false
            };
            btnSeleccionarCertificado.FlatAppearance.BorderSize = 0;
            // CORREGIDO: Usar ToolTip en lugar de ToolTipText
            toolTip.SetToolTip(btnSeleccionarCertificado, "Seleccionar certificado");
            panelContenido.Controls.Add(btnSeleccionarCertificado);

            btnVerificarCertificado = new Button
            {
                Text = "✅",
                Location = new Point(520, 38),
                Size = new Size(30, 22),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F),
                UseVisualStyleBackColor = false
            };
            btnVerificarCertificado.FlatAppearance.BorderSize = 0;
            // CORREGIDO: Usar ToolTip en lugar de ToolTipText
            toolTip.SetToolTip(btnVerificarCertificado, "Verificar certificado");
            panelContenido.Controls.Add(btnVerificarCertificado);

            // Tercera fila: Contraseña del certificado
            panelContenido.Controls.Add(CrearLabel("Contraseña:", 15, 70));
            txtAfipCertificadoPassword = new TextBox
            {
                Location = new Point(120, 68),
                Size = new Size(200, 22),
                Font = new Font("Segoe UI", 9F),
                UseSystemPasswordChar = true,
                PlaceholderText = "Contraseña del certificado"
            };
            panelContenido.Controls.Add(txtAfipCertificadoPassword);

            // Cuarta fila: URLs de servicios
            panelContenido.Controls.Add(CrearLabel("WSAA URL:", 15, 100));
            txtAfipWSAAUrl = new TextBox
            {
                Location = new Point(120, 98),
                Size = new Size(430, 22),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms"
            };
            panelContenido.Controls.Add(txtAfipWSAAUrl);

            // Quinta fila: WSFE URL
            panelContenido.Controls.Add(CrearLabel("WSFE URL:", 15, 130));
            txtAfipWSFEUrl = new TextBox
            {
                Location = new Point(120, 128),
                Size = new Size(430, 22),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx"
            };
            panelContenido.Controls.Add(txtAfipWSFEUrl);

            // Información adicional
            var lblInfo = new Label
            {
                Text = "💡 Configuración para Facturación Electrónica AFIP. Use URLs de homologación para pruebas.",
                Location = new Point(15, 160),
                Size = new Size(535, 30),
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.TopLeft
            };
            panelContenido.Controls.Add(lblInfo);

            // Configurar eventos
            EventHandler clickHandler = (s, e) => ToggleColapsarAfip();
            panelHeader.Click += clickHandler;
            lblTitulo.Click += clickHandler;

            return panel;
        }

        private Panel CrearSeccionColapsable(string titulo, int y, int ancho, string tipoSeccion)
        {
            var panel = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(ancho, 35), // Altura mínima cuando está colapsado
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Header con título y botón colapsar
            var panelHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(ancho, 30),
                BackColor = Color.FromArgb(230, 235, 240),
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(panelHeader);

            var lblTitulo = new Label
            {
                Text = titulo,
                Location = new Point(10, 6),
                Size = new Size(ancho - 50, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Cursor = Cursors.Hand
            };
            panelHeader.Controls.Add(lblTitulo);

            // Botón colapsar/expandir
            Button btnColapsar = new Button
            {
                Text = "▼", // Iniciar expandido para comercio e inventario
                Location = new Point(ancho - 35, 3),
                Size = new Size(25, 24),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            btnColapsar.FlatAppearance.BorderSize = 0;
            panelHeader.Controls.Add(btnColapsar);

            // Asignar el botón según el tipo de sección
            switch (tipoSeccion)
            {
                case "Comercio":
                    btnColapsarComercio = btnColapsar;
                    break;
                case "Inventario":
                    btnColapsarInventario = btnColapsar;
                    break;
            }

            // Contenido colapsable
            var panelContenido = new Panel
            {
                Name = $"panelContenido{tipoSeccion}",
                Location = new Point(0, 30),
                Size = new Size(ancho, tipoSeccion == "Comercio" ? 70 : 65),
                BackColor = Color.FromArgb(248, 250, 252),
                Visible = true // Iniciar expandido
            };
            panel.Controls.Add(panelContenido);

            // Configurar eventos
            EventHandler clickHandler = (s, e) => 
            {
                switch (tipoSeccion)
                {
                    case "Comercio":
                        ToggleColapsarComercio();
                        break;
                    case "Inventario":
                        ToggleColapsarInventario();
                        break;
                }
            };

            panelHeader.Click += clickHandler;
            lblTitulo.Click += clickHandler;

            return panel;
        }


        private Panel CrearSeccionBaseDatos(string titulo, int y, int ancho)
        {
            var panel = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(ancho, 35),
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Header con título y botón colapsar
            var panelHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(ancho, 30),
                BackColor = Color.FromArgb(230, 230, 230),
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(panelHeader);

            var lblTitulo = new Label
            {
                Text = titulo,
                Location = new Point(10, 6),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                Cursor = Cursors.Hand
            };
            panelHeader.Controls.Add(lblTitulo);

            // Botón colapsar/expandir
            btnColapsarBaseDatos = new Button
            {
                Text = "▶", // Iniciar colapsado
                Location = new Point(ancho - 90, 3),
                Size = new Size(25, 24),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            btnColapsarBaseDatos.FlatAppearance.BorderSize = 0;
            panelHeader.Controls.Add(btnColapsarBaseDatos);

            // Botón habilitar edición
            btnEditarBaseDatos = new Button
            {
                Text = "🔒",
                Location = new Point(ancho - 60, 3),
                Size = new Size(25, 24),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            btnEditarBaseDatos.FlatAppearance.BorderSize = 0;
            panelHeader.Controls.Add(btnEditarBaseDatos);

            // Contenido colapsable
            var panelContenido = new Panel
            {
                Name = "panelContenidoBaseDatos",
                Location = new Point(0, 30),
                Size = new Size(ancho, 80),
                BackColor = Color.FromArgb(248, 248, 248),
                Visible = false
            };
            panel.Controls.Add(panelContenido);

            // Connection String
            panelContenido.Controls.Add(CrearLabelLarga("Cadena de Conexión:", 15, 10));
            txtConnectionString = new TextBox
            {
                Location = new Point(15, 30),
                Size = new Size(490, 40),
                Font = new Font("Consolas", 8F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Enabled = false,
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.Gray
            };
            panelContenido.Controls.Add(txtConnectionString);

            // Botón testear conexión
            btnTestearConexion = new Button
            {
                Text = "🔧\nTest",
                Location = new Point(510, 30),
                Size = new Size(60, 40),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                Enabled = false
            };
            btnTestearConexion.FlatAppearance.BorderSize = 0;
            panelContenido.Controls.Add(btnTestearConexion);

            // Configurar eventos
            EventHandler clickHandler = (s, e) => ToggleColapsarBaseDatos();
            panelHeader.Click += clickHandler;
            lblTitulo.Click += clickHandler;

            return panel;
        }

        // NUEVO: Crear sección de Restricciones de Impresión
        private Panel CrearSeccionRestriccionesImpresionColapsable(string titulo, int y, int ancho)
        {
            var panel = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(ancho, 35),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Header con título y botón colapsar
            var panelHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(ancho, 30),
                BackColor = Color.FromArgb(230, 235, 240),
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(panelHeader);

            var lblTitulo = new Label
            {
                Text = titulo,
                Location = new Point(10, 6),
                Size = new Size(ancho - 50, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Cursor = Cursors.Hand
            };
            panelHeader.Controls.Add(lblTitulo);

            // Botón colapsar/expandir
            btnColapsarRestriccionesImpresion = new Button
            {
                Text = "▼", // Iniciar expandido
                Location = new Point(ancho - 35, 3),
                Size = new Size(25, 24),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            btnColapsarRestriccionesImpresion.FlatAppearance.BorderSize = 0;
            panelHeader.Controls.Add(btnColapsarRestriccionesImpresion);

            // Contenido colapsable
            var panelContenido = new Panel
            {
                Name = "panelContenidoRestriccionesImpresion",
                Location = new Point(0, 30),
                Size = new Size(ancho, 120), // Altura para acomodar todos los controles
                BackColor = Color.FromArgb(248, 250, 252),
                Visible = true // Iniciar expandido
            };
            panel.Controls.Add(panelContenido);

            // === CONTENIDO DE LA SECCIÓN ===

            // Descripción principal
            var lblDescripcion = new Label
            {
                Text = "Configure las restricciones de impresión según los tipos de pago seleccionados:",
                Location = new Point(15, 10),
                Size = new Size(540, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(62, 80, 100)
            };
            panelContenido.Controls.Add(lblDescripcion);

            // CheckBox principal para la restricción
            chkRestringirRemitoPorPago = new CheckBox
            {
                Text = "Restringir generación de REMITO para pagos no efectivo",
                Location = new Point(15, 40),
                Size = new Size(520, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100),
                Checked = true // Por defecto habilitado para cumplir normativas
            };
            panelContenido.Controls.Add(chkRestringirRemitoPorPago);

            // Descripción de la funcionalidad
            var lblExplicacion = new Label
            {
                Text = "Cuando está habilitado, el sistema solo permitirá generar REMITO si el tipo de pago es 'Efectivo'.\n" +
                       "Para otros tipos de pago (DNI, MercadoPago, Débito, Crédito, etc.) solo se permitirán Facturas A o B.",
                Location = new Point(35, 65),
                Size = new Size(520, 40),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray
            };
            panelContenido.Controls.Add(lblExplicacion);

            // Configurar eventos
            EventHandler clickHandler = (s, e) => ToggleColapsarRestriccionesImpresion();
            panelHeader.Click += clickHandler;
            lblTitulo.Click += clickHandler;

            return panel;
        }

        private void CrearBotones(int yPosition, int panelWidth)
        {
            int margin = 20;
            int anchoBotonGuardar = 160;
            int anchoBotonCancelar = 100;
            int espacioEntreBotones = 20;
            
            int totalAnchoBotones = anchoBotonGuardar + espacioEntreBotones + anchoBotonCancelar;
            int inicioX = margin + (panelWidth - totalAnchoBotones) / 2;

            btnGuardar = new Button
            {
                Text = "💾 Guardar Configuración",
                Location = new Point(inicioX, yPosition),
                Size = new Size(anchoBotonGuardar, 32),
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
                Location = new Point(inicioX + anchoBotonGuardar + espacioEntreBotones, yPosition),
                Size = new Size(anchoBotonCancelar, 32),
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

        private void ConfigurarTabIndex()
        {
            txtNombreComercio.TabIndex = 0;
            txtDomicilioComercio.TabIndex = 1;
            txtRazonSocial.TabIndex = 2;
            txtCUIT.TabIndex = 3;
            txtIngBrutos.TabIndex = 4;
            txtDomicilioFiscal.TabIndex = 5;
            txtCodigoPostal.TabIndex = 6;
            dtpInicioActividades.TabIndex = 7;
            cmbCondicion.TabIndex = 8;

            // Nuevos checkboxes
            chkPermitirFacturaA.TabIndex = 9;
            chkPermitirFacturaB.TabIndex = 10;

            // TabIndex para controles AFIP (desplazados)
            txtAfipCuit.TabIndex = 11;
            txtAfipCertificadoPath.TabIndex = 12;
            txtAfipCertificadoPassword.TabIndex = 13;
            txtAfipWSAAUrl.TabIndex = 14;
            txtAfipWSFEUrl.TabIndex = 15;
            btnSeleccionarCertificado.TabIndex = 16;
            btnVerificarCertificado.TabIndex = 17;

            // NUEVO: TabIndex para restricciones de impresión
            chkRestringirRemitoPorPago.TabIndex = 18;

            chkVerificarStock.TabIndex = 19;
            lstNombresCtaCte.TabIndex = 20;
            txtNuevoNombreCtaCte.TabIndex = 21;
            btnAgregarNombre.TabIndex = 22;
            btnEditarNombre.TabIndex = 23;
            btnEliminarNombre.TabIndex = 24;
            txtConnectionString.TabIndex = 25;
            btnTestearConexion.TabIndex = 26;
            btnEditarBaseDatos.TabIndex = 27;
            btnGuardar.TabIndex = 28;
            btnCancelar.TabIndex = 29;
        }

        private void ConfigurarEventos()
        {
            System.Diagnostics.Debug.WriteLine("[CONFIG] Configurando eventos");
            
            btnGuardar.Click += async (s, e) => {
                System.Diagnostics.Debug.WriteLine("[CONFIG] Click en btnGuardar");
                await GuardarConfiguracion();
            };
            
            btnCancelar.Click += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("[CONFIG] Click en btnCancelar");
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            btnTestearConexion.Click += async (s, e) => await TestearConexion();
            btnColapsarBaseDatos.Click += (s, e) => ToggleColapsarBaseDatos();
            btnEditarBaseDatos.Click += (s, e) => ToggleEdicionBaseDatos();
            btnColapsarComercio.Click += (s, e) => ToggleColapsarComercio();
            btnColapsarFacturacion.Click += (s, e) => ToggleColapsarFacturacion();
            btnColapsarInventario.Click += (s, e) => ToggleColapsarInventario();
            btnColapsarCuentasCorrientes.Click += (s, e) => ToggleColapsarCuentasCorrientes();
            
            // Eventos para AFIP
            btnColapsarAfip.Click += (s, e) => ToggleColapsarAfip();
            btnSeleccionarCertificado.Click += (s, e) => SeleccionarCertificado();
            btnVerificarCertificado.Click += (s, e) => VerificarCertificadoAfip();

            // NUEVO: Evento para Restricciones de Impresión
            btnColapsarRestriccionesImpresion.Click += (s, e) => ToggleColapsarRestriccionesImpresion();

            // Eventos para gestión de nombres de cuenta corriente
            btnAgregarNombre.Click += (s, e) => AgregarNombreCtaCte();
            btnEditarNombre.Click += (s, e) => EditarNombreCtaCte();
            btnEliminarNombre.Click += (s, e) => EliminarNombreCtaCte();
            lstNombresCtaCte.SelectedIndexChanged += (s, e) => ActualizarBotonesCtaCte();
            
            // Permitir agregar con Enter en el TextBox
            txtNuevoNombreCtaCte.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    AgregarNombreCtaCte();
                }
            };

            // Formatear CUITs mientras el usuario escribe
            txtCUIT.TextChanged += (s, e) => FormatearCUIT();
            txtAfipCuit.TextChanged += (s, e) => FormatearCUITAfip();

            this.Load += (s, e) => {
                System.Diagnostics.Debug.WriteLine("[CONFIG] Formulario cargado");
                txtNombreComercio.Focus();
            };
            
            System.Diagnostics.Debug.WriteLine("[CONFIG] Eventos configurados");
        }

        // NUEVOS: Métodos para gestionar nombres de cuenta corriente
        private void AgregarNombreCtaCte()
        {
            string nuevoNombre = txtNuevoNombreCtaCte.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(nuevoNombre))
            {
                MostrarMensaje("❌ Ingrese un nombre válido", Color.Red);
                txtNuevoNombreCtaCte.Focus();
                return;
            }

            // Verificar si ya existe
            if (lstNombresCtaCte.Items.Contains(nuevoNombre))
            {
                MostrarMensaje("❌ Este nombre ya existe en la lista", Color.Red);
                txtNuevoNombreCtaCte.Focus();
                txtNuevoNombreCtaCte.SelectAll();
                return;
            }

            // Agregar a la lista
            lstNombresCtaCte.Items.Add(nuevoNombre);
            txtNuevoNombreCtaCte.Clear();
            txtNuevoNombreCtaCte.Focus();
            
            MostrarMensaje($"✅ Nombre '{nuevoNombre}' agregado correctamente", Color.Green);
            
            // Limpiar mensaje después de 2 segundos
            var timer = new System.Windows.Forms.Timer { Interval = 2000 };
            timer.Tick += (s, e) =>
            {
                if (lblMensaje.Text.Contains("agregado correctamente"))
                    lblMensaje.Text = "";
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private void EditarNombreCtaCte()
        {
            if (lstNombresCtaCte.SelectedIndex == -1)
            {
                MostrarMensaje("❌ Seleccione un nombre para editar", Color.Red);
                return;
            }

            string nombreActual = lstNombresCtaCte.SelectedItem.ToString();
            string nombreNuevo = Microsoft.VisualBasic.Interaction.InputBox(
                $"Editar nombre:\n\nNombre actual: {nombreActual}\n\nIngrese el nuevo nombre:",
                "Editar Nombre de Cuenta Corriente",
                nombreActual);

            if (string.IsNullOrWhiteSpace(nombreNuevo) || nombreNuevo == nombreActual)
            {
                return; // Usuario canceló o no cambió nada
            }

            // Verificar si el nuevo nombre ya existe
            if (lstNombresCtaCte.Items.Contains(nombreNuevo))
            {
                MostrarMensaje("❌ Este nombre ya existe en la lista", Color.Red);
                return;
            }

            // Actualizar el nombre
            int indiceSeleccionado = lstNombresCtaCte.SelectedIndex;
            lstNombresCtaCte.Items[indiceSeleccionado] = nombreNuevo;
            lstNombresCtaCte.SelectedIndex = indiceSeleccionado; // Mantener seleccionado
            
            MostrarMensaje($"✅ Nombre actualizado a '{nombreNuevo}'", Color.Green);
            
            // Limpiar mensaje después de 2 segundos
            var timer = new System.Windows.Forms.Timer { Interval = 2000 };
            timer.Tick += (s, e) =>
            {
                if (lblMensaje.Text.Contains("actualizado"))
                    lblMensaje.Text = "";
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private void EliminarNombreCtaCte()
        {
            if (lstNombresCtaCte.SelectedIndex == -1)
            {
                MostrarMensaje("❌ Seleccione un nombre para eliminar", Color.Red);
                return;
            }

            string nombreSeleccionado = lstNombresCtaCte.SelectedItem.ToString();
            
            var resultado = MessageBox.Show(
                $"¿Está seguro de eliminar el siguiente nombre?\n\n'{nombreSeleccionado}'\n\nEsta acción no se puede deshacer.",
                "Confirmar Eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (resultado == DialogResult.Yes)
            {
                lstNombresCtaCte.Items.RemoveAt(lstNombresCtaCte.SelectedIndex);
                MostrarMensaje($"✅ Nombre '{nombreSeleccionado}' eliminado", Color.Green);
                
                // Limpiar mensaje después de 2 segundos
                var timer = new System.Windows.Forms.Timer { Interval = 2000 };
                timer.Tick += (s, e) =>
                {
                    if (lblMensaje.Text.Contains("eliminado"))
                        lblMensaje.Text = "";
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
        }

        private void ActualizarBotonesCtaCte()
        {
            bool haySeleccion = lstNombresCtaCte.SelectedIndex != -1;
            btnEditarNombre.Enabled = haySeleccion;
            btnEliminarNombre.Enabled = haySeleccion;
        }

        private void CargarConfiguracion()
        {
            System.Diagnostics.Debug.WriteLine("[CONFIG] Iniciando carga de configuración");
            
            try
            {
                if (_rutaAppsettings == null)
                {
                    _rutaAppsettings = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                }
                
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargando desde: {_rutaAppsettings}");
                
                if (!File.Exists(_rutaAppsettings))
                {
                    System.Diagnostics.Debug.WriteLine("[CONFIG] Archivo no existe, creando por defecto");
                    MostrarMensaje("❌ No se encontró el archivo appsettings.json", Color.Red);
                    CrearArchivoConfiguracionDefault();
                    return;
                }

                string jsonContent = File.ReadAllText(_rutaAppsettings);
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Contenido leído: {jsonContent.Length} caracteres");
                
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    System.Diagnostics.Debug.WriteLine("[CONFIG] Archivo vacío, creando por defecto");
                    MostrarMensaje("❌ El archivo appsettings.json está vacío", Color.Red);
                    CrearArchivoConfiguracionDefault();
                    return;
                }

                _configuracionOriginal = JObject.Parse(jsonContent);
                System.Diagnostics.Debug.WriteLine("[CONFIG] JSON parseado correctamente");

                // Cargar información del comercio
                txtNombreComercio.Text = _configuracionOriginal["Comercio"]?["Nombre"]?.ToString() ?? "Mi Comercio";
                txtDomicilioComercio.Text = _configuracionOriginal["Comercio"]?["Domicilio"]?.ToString() ?? "Dirección del comercio";

                // Cargar datos de facturación
                txtRazonSocial.Text = _configuracionOriginal["Facturacion"]?["RazonSocial"]?.ToString() ?? "";
                txtCUIT.Text = _configuracionOriginal["Facturacion"]?["CUIT"]?.ToString() ?? "";
                txtIngBrutos.Text = _configuracionOriginal["Facturacion"]?["IngBrutos"]?.ToString() ?? "";
                txtDomicilioFiscal.Text = _configuracionOriginal["Facturacion"]?["DomicilioFiscal"]?.ToString() ?? "";
                txtCodigoPostal.Text = _configuracionOriginal["Facturacion"]?["CodigoPostal"]?.ToString() ?? "";

                // Cargar opciones de facturación: permitir Factura A / B
                bool permitirA = _configuracionOriginal["Facturacion"]?["PermitirFacturaA"]?.ToObject<bool>() ?? true;
                bool permitirB = _configuracionOriginal["Facturacion"]?["PermitirFacturaB"]?.ToObject<bool>() ?? true;
                if (chkPermitirFacturaA != null) chkPermitirFacturaA.Checked = permitirA;
                if (chkPermitirFacturaB != null) chkPermitirFacturaB.Checked = permitirB;

                // Cargar fecha de inicio de actividades
                if (DateTime.TryParse(_configuracionOriginal["Facturacion"]?["InicioActividades"]?.ToString(), out DateTime fechaInicio))
                {
                    dtpInicioActividades.Value = fechaInicio;
                }
                else
                {
                    dtpInicioActividades.Value = DateTime.Now;
                }

                // Cargar condición
                string condicion = _configuracionOriginal["Facturacion"]?["Condicion"]?.ToString() ?? "";
                if (cmbCondicion.Items.Contains(condicion))
                {
                    cmbCondicion.SelectedItem = condicion;
                }
                else if (cmbCondicion.Items.Count > 0)
                {
                    cmbCondicion.SelectedIndex = 0; // Seleccionar el primero por defecto
                }

                // Cargar configuración de AFIP
                txtAfipCuit.Text = _configuracionOriginal["AFIP"]?["CUIT"]?.ToString() ?? "";
                txtAfipCertificadoPath.Text = _configuracionOriginal["AFIP"]?["CertificadoPath"]?.ToString() ?? "";
                txtAfipCertificadoPassword.Text = _configuracionOriginal["AFIP"]?["CertificadoPassword"]?.ToString() ?? "";
                txtAfipWSAAUrl.Text = _configuracionOriginal["AFIP"]?["WSAAUrl"]?.ToString() ?? "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";
                txtAfipWSFEUrl.Text = _configuracionOriginal["AFIP"]?["WSFEUrl"]?.ToString() ?? "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";

                // NUEVO: Cargar configuración de restricciones de impresión
                bool restringirRemitoPorPago = _configuracionOriginal["RestriccionesImpresion"]?["RestringirRemitoPorPago"]?.ToObject<bool>() ?? true;
                chkRestringirRemitoPorPago.Checked = restringirRemitoPorPago;

                // Cargar configuración de inventario
                bool verificarStock = _configuracionOriginal["Inventario"]?["VerificarStock"]?.ToObject<bool>() ?? 
                                     _configuracionOriginal["Validaciones"]?["ValidarStockDisponible"]?.ToObject<bool>() ?? 
                                     true;
                chkVerificarStock.Checked = verificarStock;

                // Cargar nombres de cuenta corriente
                CargarNombresCuentasCorrientes();

                // Cargar cadena de conexión
                txtConnectionString.Text = _configuracionOriginal["ConnectionStrings"]?["DefaultConnection"]?.ToString() ?? "";

                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - Nombre: '{txtNombreComercio.Text}'");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - CUIT: '{txtCUIT.Text}'");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - AFIP CUIT: '{txtAfipCuit.Text}'");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - Certificado: '{txtAfipCertificadoPath.Text}'");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - Stock: {chkVerificarStock.Checked}");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - Nombres CtaCte: {lstNombresCtaCte.Items.Count}");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - Restringir Remito: {chkRestringirRemitoPorPago.Checked}");

                MostrarMensaje("✅ Configuración cargada correctamente", Color.Green);
                
                var timer = new System.Windows.Forms.Timer { Interval = 2000 };
                timer.Tick += (s, e) =>
                {
                    lblMensaje.Text = "";
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CONFIG ERROR] Error cargando: {ex}");
                MostrarMensaje($"❌ Error cargando configuración: {ex.Message}", Color.Red);
            }
        }

        // NUEVO: Cargar nombres de cuentas corrientes desde configuración
        private void CargarNombresCuentasCorrientes()
        {
            try
            {
                lstNombresCtaCte.Items.Clear();

                var nombresArray = _configuracionOriginal["CuentasCorrientes"]?["NombresCtaCte"] as JArray;
                
                if (nombresArray != null)
                {
                    foreach (var nombre in nombresArray)
                    {
                        string nombreTexto = nombre.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(nombreTexto))
                        {
                            lstNombresCtaCte.Items.Add(nombreTexto);
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargados {lstNombresCtaCte.Items.Count} nombres de cuenta corriente");
                }
                else
                {
                    // Si no existe la sección, crear algunos nombres por defecto
                    lstNombresCtaCte.Items.AddRange(new string[] {
                        "Cliente General",
                        "Juan Pérez",
                        "María García"
                    });
                    
                    System.Diagnostics.Debug.WriteLine("[CONFIG] Sección CuentasCorrientes no encontrada, usando nombres por defecto");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CONFIG ERROR] Error cargando nombres cuenta corriente: {ex.Message}");
                
                // Cargar nombres por defecto en caso de error
                lstNombresCtaCte.Items.Clear();
                lstNombresCtaCte.Items.AddRange(new string[] {
                    "Cliente General",
                    "Juan Pérez",
                    "María García"
                });
            }
        }

        /// <summary>
        /// NUEVO: Crea un archivo de configuración por defecto si no existe
        /// </summary>
        private void CrearArchivoConfiguracionDefault()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[CONFIG] Creando archivo por defecto");

                var configuracionDefault = new JObject
                {
                    ["ConnectionStrings"] = new JObject
                    {
                        ["DefaultConnection"] = "Server=localhost;Database=comercio;Trusted_Connection=True;TrustServerCertificate=True;"
                    },
                    ["Comercio"] = new JObject
                    {
                        ["Nombre"] = "Mi Comercio",
                        ["Domicilio"] = "Dirección del comercio"
                    },
                    ["Facturacion"] = new JObject
                    {
                        ["RazonSocial"] = "",
                        ["CUIT"] = "",
                        ["IngBrutos"] = "",
                        ["DomicilioFiscal"] = "",
                        ["CodigoPostal"] = "",
                        ["InicioActividades"] = DateTime.Now.ToString("yyyy-MM-dd"),
                        ["Condicion"] = "",
                        ["PermitirFacturaA"] = true,
                        ["PermitirFacturaB"] = true
                    },
                    ["Inventario"] = new JObject
                    {
                        ["VerificarStock"] = true
                    },
                    ["Validaciones"] = new JObject
                    {
                        ["ValidarStockDisponible"] = true
                    },
                    // Sección de Cuentas Corrientes por defecto
                    ["CuentasCorrientes"] = new JObject
                    {
                        ["NombresCtaCte"] = new JArray
                {
                    "Cliente General",
                    "Juan Pérez",
                    "María García",
                    "Carlos López"
                }
                    },
                    // Sección de configuración AFIP por defecto
                    ["AFIP"] = new JObject
                    {
                        ["CUIT"] = "20-12345678-9",
                        ["CertificadoPath"] = "C:\\ruta\\al\\certificado.p12",
                        ["CertificadoPassword"] = "clave123",
                        ["WSAAUrl"] = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",
                        ["WSFEUrl"] = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx"
                    },
                    // Sección de restricciones de impresión por defecto
                    ["RestriccionesImpresion"] = new JObject
                    {
                        ["RestringirRemitoPorPago"] = true
                    }
                };

                string jsonFormateado = JsonConvert.SerializeObject(configuracionDefault, Formatting.Indented);
                File.WriteAllText(_rutaAppsettings, jsonFormateado);

                _configuracionOriginal = configuracionDefault;
                System.Diagnostics.Debug.WriteLine("[CONFIG] Archivo por defecto creado");
                MostrarMensaje("✅ Archivo de configuración creado", Color.Blue);

                // Cargar los nombres por defecto en la lista
                CargarNombresCuentasCorrientes();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CONFIG ERROR] Error creando por defecto: {ex}");
                MostrarMensaje($"❌ Error creando configuración: {ex.Message}", Color.Red);
            }
        }

        private async Task GuardarConfiguracion()
        {
            System.Diagnostics.Debug.WriteLine("[SAVE] === INICIANDO GUARDADO ===");
            System.Diagnostics.Debug.WriteLine($"[SAVE] Nombres CtaCte: {lstNombresCtaCte.Items.Count}");
            System.Diagnostics.Debug.WriteLine($"[SAVE] AFIP CUIT: {txtAfipCuit.Text}");
            System.Diagnostics.Debug.WriteLine($"[SAVE] Certificado: {txtAfipCertificadoPath.Text}");
            System.Diagnostics.Debug.WriteLine($"[SAVE] Restringir Remito: {chkRestringirRemitoPorPago.Checked}");
            
            if (!ValidarDatos())
            {
                System.Diagnostics.Debug.WriteLine("[SAVE] Validación falló, cancelando guardado");
                return;
            }

            try
            {
                btnGuardar.Enabled = false;
                btnGuardar.Text = "💾 Guardando...";
                MostrarMensaje("Guardando configuración...", Color.Blue);

                System.Diagnostics.Debug.WriteLine($"[SAVE] Iniciando guardado en: {_rutaAppsettings}");

                // Verificar que tenemos la configuración original
                if (_configuracionOriginal == null)
                {
                    System.Diagnostics.Debug.WriteLine("[SAVE ERROR] _configuracionOriginal es null");
                    MostrarMensaje("❌ Error: Configuración original no cargada", Color.Red);
                    return;
                }

                var nuevaConfiguracion = JObject.Parse(_configuracionOriginal.ToString());

                // Actualizar información del comercio
                if (nuevaConfiguracion["Comercio"] == null)
                    nuevaConfiguracion["Comercio"] = new JObject();
    
                nuevaConfiguracion["Comercio"]["Nombre"] = txtNombreComercio.Text.Trim();
                nuevaConfiguracion["Comercio"]["Domicilio"] = txtDomicilioComercio.Text.Trim();

                System.Diagnostics.Debug.WriteLine($"[SAVE] Comercio - Nombre: '{txtNombreComercio.Text.Trim()}'");

                // Actualizar datos de facturación
                if (nuevaConfiguracion["Facturacion"] == null)
                    nuevaConfiguracion["Facturacion"] = new JObject();

                nuevaConfiguracion["Facturacion"]["RazonSocial"] = txtRazonSocial.Text.Trim();
                nuevaConfiguracion["Facturacion"]["CUIT"] = txtCUIT.Text.Trim();
                nuevaConfiguracion["Facturacion"]["IngBrutos"] = txtIngBrutos.Text.Trim();
                nuevaConfiguracion["Facturacion"]["DomicilioFiscal"] = txtDomicilioFiscal.Text.Trim();
                nuevaConfiguracion["Facturacion"]["CodigoPostal"] = txtCodigoPostal.Text.Trim();
                nuevaConfiguracion["Facturacion"]["InicioActividades"] = dtpInicioActividades.Value.ToString("yyyy-MM-dd");
                nuevaConfiguracion["Facturacion"]["Condicion"] = cmbCondicion.SelectedItem?.ToString() ?? "";

                // Actualizar configuración de AFIP
                if (nuevaConfiguracion["AFIP"] == null)
                    nuevaConfiguracion["AFIP"] = new JObject();

                nuevaConfiguracion["AFIP"]["CUIT"] = txtAfipCuit.Text.Trim();
                nuevaConfiguracion["AFIP"]["CertificadoPath"] = txtAfipCertificadoPath.Text.Trim();
                nuevaConfiguracion["AFIP"]["CertificadoPassword"] = txtAfipCertificadoPassword.Text;
                nuevaConfiguracion["AFIP"]["WSAAUrl"] = txtAfipWSAAUrl.Text.Trim();
                nuevaConfiguracion["AFIP"]["WSFEUrl"] = txtAfipWSFEUrl.Text.Trim();

                // Opciones adicionales de facturación
                nuevaConfiguracion["Facturacion"]["PermitirFacturaA"] = chkPermitirFacturaA?.Checked ?? true;
                nuevaConfiguracion["Facturacion"]["PermitirFacturaB"] = chkPermitirFacturaB?.Checked ?? true;

                System.Diagnostics.Debug.WriteLine($"[SAVE] AFIP configurado - CUIT: '{txtAfipCuit.Text.Trim()}'");

                // NUEVO: Actualizar configuración de restricciones de impresión
                if (nuevaConfiguracion["RestriccionesImpresion"] == null)
                    nuevaConfiguracion["RestriccionesImpresion"] = new JObject();

                nuevaConfiguracion["RestriccionesImpresion"]["RestringirRemitoPorPago"] = chkRestringirRemitoPorPago.Checked;

                System.Diagnostics.Debug.WriteLine($"[SAVE] Restricciones configuradas - Restringir Remito: {chkRestringirRemitoPorPago.Checked}");

                // Mantener compatibilidad con sección "Validaciones" existente
                if (nuevaConfiguracion["Validaciones"] == null)
                    nuevaConfiguracion["Validaciones"] = new JObject();
    
                nuevaConfiguracion["Validaciones"]["ValidarStockDisponible"] = chkVerificarStock.Checked;

                // Guardar nombres de cuentas corrientes
                GuardarNombresCuentasCorrientes(nuevaConfiguracion);

                // Actualizar cadena de conexión solo si la edición está habilitada
                if (_edicionBaseDatosHabilitada)
                {
                    if (nuevaConfiguracion["ConnectionStrings"] == null)
                        nuevaConfiguracion["ConnectionStrings"] = new JObject();
                
                    nuevaConfiguracion["ConnectionStrings"]["DefaultConnection"] = txtConnectionString.Text.Trim();
                    System.Diagnostics.Debug.WriteLine("[SAVE] Connection String actualizada");
                }

                // Crear backup del archivo original
                string backupPath = _rutaAppsettings + $".backup.{DateTime.Now:yyyyMMdd_HHmmss}";
                if (File.Exists(_rutaAppsettings))
                {
                    File.Copy(_rutaAppsettings, backupPath);
                    System.Diagnostics.Debug.WriteLine($"[SAVE] Backup creado: {backupPath}");
                }

                // Guardar nueva configuración con formato JSON legible
                string jsonFormateado = JsonConvert.SerializeObject(nuevaConfiguracion, Formatting.Indented);
                System.Diagnostics.Debug.WriteLine($"[SAVE] JSON a guardar:\n{jsonFormateado}");
        
                await File.WriteAllTextAsync(_rutaAppsettings, jsonFormateado);
                System.Diagnostics.Debug.WriteLine("[SAVE] Archivo escrito");

                // Verificar que se guardó correctamente
                if (File.Exists(_rutaAppsettings))
                {
                    string contenidoGuardado = await File.ReadAllTextAsync(_rutaAppsettings);
                    System.Diagnostics.Debug.WriteLine($"[SAVE] Verificación - Archivo guardado correctamente. Tamaño: {contenidoGuardado.Length} caracteres");
                    
                    // Actualizar configuración original para futuras comparaciones
                    _configuracionOriginal = nuevaConfiguracion;
                    
                    MostrarMensaje("✅ Configuración guardada correctamente", Color.Green);
                    
                    var result = MessageBox.Show(
                        $"✅ Configuración guardada exitosamente.\n\n" +
                        $"Se creó un backup en:\n{Path.GetFileName(backupPath)}\n\n" +
                        "⚠️ IMPORTANTE: Reinicie la aplicación para que todos los cambios surtan efecto.\n\n" +
                        "¿Desea reiniciar la aplicación ahora?",
                        "Configuración Guardada",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        System.Diagnostics.Debug.WriteLine("[SAVE] Usuario eligió reiniciar");
                        Application.Restart();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[SAVE] Usuario eligió no reiniciar, cerrando formulario");
                        await Task.Delay(2000);
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[SAVE ERROR] El archivo no existe después del guardado");
                    MostrarMensaje("❌ Error: El archivo no se guardó correctamente", Color.Red);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SAVE ERROR] Excepción: {ex}");
                MostrarMensaje($"❌ Error al guardar: {ex.Message}", Color.Red);
            }
            finally
            {
                btnGuardar.Enabled = true;
                btnGuardar.Text = "💾 Guardar Configuración";
                System.Diagnostics.Debug.WriteLine("[SAVE] === GUARDADO FINALIZADO ===");
            }
        }

        // NUEVO: Guardar nombres de cuentas corrientes en la configuración
        private void GuardarNombresCuentasCorrientes(JObject configuracion)
        {
            try
            {
                // Obtener la sección de cuentas corrientes
                if (configuracion["CuentasCorrientes"] == null)
                    configuracion["CuentasCorrientes"] = new JObject();
                
                if (configuracion["CuentasCorrientes"]["NombresCtaCte"] == null)
                    configuracion["CuentasCorrientes"]["NombresCtaCte"] = new JArray();

                var nombresArray = configuracion["CuentasCorrientes"]["NombresCtaCte"] as JArray;

                // Limpiar la lista actual
                nombresArray.Clear();

                // Agregar los nombres actuales de la lista
                foreach (var item in lstNombresCtaCte.Items)
                {
                    nombresArray.Add(item.ToString());
                }

                System.Diagnostics.Debug.WriteLine($"[SAVE] Nombres de cuentas corrientes guardados: {lstNombresCtaCte.Items.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SAVE ERROR] Error guardando nombres CtaCte: {ex.Message}");
                MostrarMensaje($"❌ Error guardando nombres de cuenta corriente: {ex.Message}", Color.Red);
            }
        }

        private async Task TestearConexion()
        {
            if (string.IsNullOrWhiteSpace(txtConnectionString.Text))
            {
                MostrarMensaje("❌ La cadena de conexión no puede estar vacía", Color.Red);
                return;
            }

            try
            {
                btnTestearConexion.Enabled = false;
                btnTestearConexion.Text = "🔄\n...";
                MostrarMensaje("Probando conexión...", Color.Blue);

                using var connection = new SqlConnection(txtConnectionString.Text);
                await connection.OpenAsync();
                
                using var cmd = new SqlCommand("SELECT 1", connection);
                await cmd.ExecuteScalarAsync();

                MostrarMensaje("✅ Conexión exitosa", Color.Green);
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error de conexión: {ex.Message}", Color.Red);
            }
            finally
            {
                btnTestearConexion.Enabled = true;
                btnTestearConexion.Text = "🔧\nTest";
            }
        }

        private bool ValidarDatos()
        {
            if (string.IsNullOrWhiteSpace(txtNombreComercio.Text))
            {
                MostrarMensaje("❌ El nombre del comercio es requerido", Color.Red);
                _comercioColapsado = false;
                ActualizarEstadoSeccion(panelComercio, "panelContenidoComercio", _comercioColapsado, btnColapsarComercio, 35, 105);
                ActualizarPosicionesTodasLasSecciones();
                txtNombreComercio.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtDomicilioComercio.Text))
            {
                MostrarMensaje("❌ El domicilio del comercio es requerido", Color.Red);
                _comercioColapsado = false;
                ActualizarEstadoSeccion(panelComercio, "panelContenidoComercio", _comercioColapsado, btnColapsarComercio, 35, 105);
                ActualizarPosicionesTodasLasSecciones();
                txtDomicilioComercio.Focus();
                return false;
            }

            // Validaciones para datos de facturación
            if (string.IsNullOrWhiteSpace(txtRazonSocial.Text))
            {
                MostrarMensaje("❌ La razón social es requerida", Color.Red);
                _facturacionColapsada = false;
                ActualizarEstadoSeccion(panelFacturacion, "panelContenidoFacturacion", _facturacionColapsada, btnColapsarFacturacion, 35, 195);
                ActualizarPosicionesTodasLasSecciones();
                txtRazonSocial.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtCUIT.Text))
            {
                MostrarMensaje("❌ El CUIT es requerido", Color.Red);
                _facturacionColapsada = false;
                ActualizarEstadoSeccion(panelFacturacion, "panelContenidoFacturacion", _facturacionColapsada, btnColapsarFacturacion, 35, 195);
                ActualizarPosicionesTodasLasSecciones();
                txtCUIT.Focus();
                return false;
            }

            // ACTUALIZADO: Usar la nueva validación completa del CUIT
            if (!ValidarCUITCompleto(txtCUIT.Text))
            {
                string cuitLimpio = txtCUIT.Text.Replace("-", "");
                
                if (cuitLimpio.Length != 11)
                {
                    MostrarMensaje("❌ El CUIT debe tener 11 dígitos en formato XX-XXXXXXXX-X", Color.Red);
                }
                else if (!cuitLimpio.All(char.IsDigit))
                {
                    MostrarMensaje("❌ El CUIT solo puede contener números", Color.Red);
                }
                else
                {
                    // Calcular el dígito verificador correcto para mostrar al usuario
                    string primeros10 = cuitLimpio.Substring(0, 10);
                    int digitoCorrectoCalculado = CalcularDigitoVerificadorCUIT(primeros10);
                    int digitoIngresado = int.Parse(cuitLimpio[10].ToString());
                    
                    MostrarMensaje($"❌ CUIT inválido: El dígito verificador debería ser {digitoCorrectoCalculado}, pero se ingresó {digitoIngresado}", Color.Red);
                }
                
                _facturacionColapsada = false;
                ActualizarEstadoSeccion(panelFacturacion, "panelContenidoFacturacion", _facturacionColapsada, btnColapsarFacturacion, 35, 195);
                ActualizarPosicionesTodasLasSecciones();
                txtCUIT.Focus();
                txtCUIT.SelectAll();
                return false;
            }

            if (cmbCondicion.SelectedItem == null)
            {
                MostrarMensaje("❌ Debe seleccionar una condición fiscal", Color.Red);
                _facturacionColapsada = false;
                ActualizarEstadoSeccion(panelFacturacion, "panelContenidoFacturacion", _facturacionColapsada, btnColapsarFacturacion, 35, 195);
                ActualizarPosicionesTodasLasSecciones();
                cmbCondicion.Focus();
                return false;
            }

            // NUEVO: Validaciones para datos de AFIP
            if (string.IsNullOrWhiteSpace(txtAfipCuit.Text))
            {
                MostrarMensaje("❌ El CUIT AFIP es requerido", Color.Red);
                _afipColapsado = false;
                ActualizarEstadoSeccion(panelAfip, "panelContenidoAfip", _afipColapsado, btnColapsarAfip, 35, 235);
                ActualizarPosicionesTodasLasSecciones();
                txtAfipCuit.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtAfipCertificadoPath.Text))
            {
                MostrarMensaje("❌ La ruta del certificado es requerida", Color.Red);
                _afipColapsado = false;
                ActualizarEstadoSeccion(panelAfip, "panelContenidoAfip", _afipColapsado, btnColapsarAfip, 35, 235);
                ActualizarPosicionesTodasLasSecciones();
                txtAfipCertificadoPath.Focus();
                return false;
            }

            if (!File.Exists(txtAfipCertificadoPath.Text))
            {
                MostrarMensaje("❌ El archivo del certificado no existe", Color.Red);
                _afipColapsado = false;
                ActualizarEstadoSeccion(panelAfip, "panelContenidoAfip", _afipColapsado, btnColapsarAfip, 35, 235);
                ActualizarPosicionesTodasLasSecciones();
                txtAfipCertificadoPath.Focus();
                txtAfipCertificadoPath.SelectAll();
                return false;
            }

            return true;
        }

        private void MostrarMensaje(string mensaje, Color color)
        {
            lblMensaje.Text = mensaje;
            lblMensaje.ForeColor = color;
        }

        public static Bitmap CrearIconoConfiguracion()
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                
                using (var brush = new SolidBrush(Color.Gray))
                {
                    g.FillEllipse(brush, 6, 6, 4, 4);
                    
                    Rectangle[] dientes = {
                        new Rectangle(7, 2, 2, 3),
                        new Rectangle(11, 7, 3, 2),
                        new Rectangle(7, 11, 2, 3),
                        new Rectangle(2, 7, 3, 2)
                    };
                    
                    foreach (var diente in dientes)
                    {
                        g.FillRectangle(brush, diente);
                    }
                }
            }
            return bitmap;
        }

        // Métodos de colapso para cada sección
        private void ToggleColapsarComercio()
        {
            _comercioColapsado = !_comercioColapsado;
            ActualizarEstadoSeccion(panelComercio, "panelContenidoComercio", _comercioColapsado, btnColapsarComercio, 35, 105);
            ActualizarPosicionesTodasLasSecciones();
        }

        private void ToggleColapsarFacturacion()
        {
            _facturacionColapsada = !_facturacionColapsada;
            ActualizarEstadoSeccion(panelFacturacion, "panelContenidoFacturacion", _facturacionColapsada, btnColapsarFacturacion, 35, 195);
            ActualizarPosicionesTodasLasSecciones();
        }

        private void ToggleColapsarInventario()
        {
            _inventarioColapsado = !_inventarioColapsado;
            ActualizarEstadoSeccion(panelInventario, "panelContenidoInventario", _inventarioColapsado, btnColapsarInventario, 35, 100);
            ActualizarPosicionesTodasLasSecciones();
        }

        private void ToggleColapsarCuentasCorrientes()
        {
            _cuentasCorrientesColapsado = !_cuentasCorrientesColapsado;
            // CORREGIDO: Actualizar con la nueva altura aumentada
            ActualizarEstadoSeccion(panelCuentasCorrientes, "panelContenidoCuentasCorrientes", _cuentasCorrientesColapsado, btnColapsarCuentasCorrientes, 35, 225); // AUMENTADO: de 150 a 225
            ActualizarPosicionesTodasLasSecciones();
        }

        private void ToggleColapsarBaseDatos()
        {
            _baseDatosColapsada = !_baseDatosColapsada;
            ActualizarEstadoBaseDatos();
            ActualizarPosicionesTodasLasSecciones();
        }

        private void ToggleEdicionBaseDatos()
        {
            _edicionBaseDatosHabilitada = !_edicionBaseDatosHabilitada;
            ActualizarEstadoBaseDatos();
        }

        private void ToggleColapsarAfip()
        {
            _afipColapsado = !_afipColapsado;
            ActualizarEstadoSeccion(panelAfip, "panelContenidoAfip", _afipColapsado, btnColapsarAfip, 35, 235);
            ActualizarPosicionesTodasLasSecciones();
        }

        private void ToggleColapsarRestriccionesImpresion()
        {
            _restriccionesImpresionColapsado = !_restriccionesImpresionColapsado;
            ActualizarEstadoSeccion(panelRestriccionesImpresion, "panelContenidoRestriccionesImpresion", 
                _restriccionesImpresionColapsado, btnColapsarRestriccionesImpresion, 35, 155);
            ActualizarPosicionesTodasLasSecciones();
        }

        private void ActualizarEstadoSeccion(Panel panel, string nombrePanelContenido, bool colapsado, Button botonColapsar, int alturaColapsada, int alturaExpandida)
        {
            var panelContenido = panel.Controls[nombrePanelContenido];
            panelContenido.Visible = !colapsado;

            if (colapsado)
            {
                panel.Size = new Size(panel.Width, alturaColapsada);
                botonColapsar.Text = "▶";
            }
            else
            {
                panel.Size = new Size(panel.Width, alturaExpandida);
                botonColapsar.Text = "▼";
            }
        }

        private void ActualizarEstadoBaseDatos()
        {
            var panelContenido = panelBaseDatos.Controls["panelContenidoBaseDatos"];
            
            panelContenido.Visible = !_baseDatosColapsada;
            
            if (_baseDatosColapsada)
            {
                panelBaseDatos.Size = new Size(panelBaseDatos.Width, 35);
                btnColapsarBaseDatos.Text = "▶";
            }
            else
            {
                panelBaseDatos.Size = new Size(panelBaseDatos.Width, 115);
                btnColapsarBaseDatos.Text = "▼";
            }

            txtConnectionString.Enabled = _edicionBaseDatosHabilitada;
            btnTestearConexion.Enabled = _edicionBaseDatosHabilitada;

            if (_edicionBaseDatosHabilitada)
            {
                txtConnectionString.BackColor = Color.White;
                txtConnectionString.ForeColor = Color.Black;
                btnEditarBaseDatos.Text = "🔓";
                btnEditarBaseDatos.BackColor = Color.FromArgb(76, 175, 80);
            }
            else
            {
                txtConnectionString.BackColor = Color.FromArgb(245, 245, 245);
                txtConnectionString.ForeColor = Color.Gray;
                btnEditarBaseDatos.Text = "🔒";
                btnEditarBaseDatos.BackColor = Color.FromArgb(255, 152, 0);
            }
        }

        // MÉTODO PRINCIPAL: Actualizar posiciones dinámicamente
        private void ActualizarPosicionesTodasLasSecciones()
        {
            int currentY = 10;
            int spacing = 10;

            // Sección Comercio
            panelComercio.Location = new Point(10, currentY);
            currentY += panelComercio.Height + spacing;

            // Sección Facturación
            panelFacturacion.Location = new Point(10, currentY);
            currentY += panelFacturacion.Height + spacing;

            // Sección AFIP
            panelAfip.Location = new Point(10, currentY);
            currentY += panelAfip.Height + spacing;

            // Sección Restricciones de Impresión
            panelRestriccionesImpresion.Location = new Point(10, currentY);
            currentY += panelRestriccionesImpresion.Height + spacing;

            // Sección Inventario
            panelInventario.Location = new Point(10, currentY);
            currentY += panelInventario.Height + spacing;

            // Sección Cuentas Corrientes
            panelCuentasCorrientes.Location = new Point(10, currentY);
            currentY += panelCuentasCorrientes.Height + spacing;

            // Sección Base de Datos
            panelBaseDatos.Location = new Point(10, currentY);
            currentY += panelBaseDatos.Height + spacing;

            // Actualizar el scroll del panel principal si es necesario
            panelPrincipal.AutoScrollMinSize = new Size(0, currentY);
        }

        private void FormatearCUIT()
        {
            // Evitar recursión infinita
            if (txtCUIT.Text.Length == 0) return;
    
            // Guardar posición del cursor
            int cursorPosition = txtCUIT.SelectionStart;
    
            // Remover todos los guiones existentes y mantener solo números
            string soloNumeros = new string(txtCUIT.Text.Where(char.IsDigit).ToArray());
    
            // Limitar a máximo 11 dígitos
            if (soloNumeros.Length > 11)
            {
                soloNumeros = soloNumeros.Substring(0, 11);
            }
    
            string cuitFormateado = soloNumeros;
    
            // Aplicar formato XX-XXXXXXXX-X
            if (soloNumeros.Length > 2)
            {
                cuitFormateado = soloNumeros.Substring(0, 2) + "-" + soloNumeros.Substring(2);
            }
    
            if (soloNumeros.Length > 10)
            {
                cuitFormateado = soloNumeros.Substring(0, 2) + "-" + 
                                soloNumeros.Substring(2, 8) + "-" + 
                                soloNumeros.Substring(10);
            }
    
            // Solo actualizar si el texto cambió para evitar bucle infinito
            if (txtCUIT.Text != cuitFormateado)
            {
                // Temporalmente desconectar el evento para evitar recursión
                txtCUIT.TextChanged -= (s, e) => FormatearCUIT();
                
                txtCUIT.Text = cuitFormateado;
                
                // Ajustar posición del cursor
                int nuevaPosicion = Math.Min(cursorPosition, cuitFormateado.Length);
                
                // Si el cursor estaba después de una posición donde se insertó un guión, ajustar
                if (cursorPosition >= 2 && soloNumeros.Length > 2)
                    nuevaPosicion = Math.Min(cursorPosition + 1, cuitFormateado.Length);
                if (cursorPosition >= 10 && soloNumeros.Length > 10)
                    nuevaPosicion = Math.Min(nuevaPosicion + 1, cuitFormateado.Length);
                    
                txtCUIT.SelectionStart = nuevaPosicion;
                
                // Reconectar el evento
                txtCUIT.TextChanged += (s, e) => FormatearCUIT();
            }
    
            // Validar dígito verificador cuando está completo
            if (soloNumeros.Length == 11)
            {
                ValidarDigitoVerificadorCUIT(soloNumeros);
            }
            else
            {
                // Limpiar indicadores de validación si no está completo
                txtCUIT.BackColor = Color.White;
            }
        }

        /// <summary>
        /// Valida el dígito verificador del CUIT usando el algoritmo oficial argentino
        /// </summary>
        /// <param name="cuit">CUIT de 11 dígitos sin guiones</param>
        private void ValidarDigitoVerificadorCUIT(string cuit)
        {
            if (cuit.Length != 11 || !cuit.All(char.IsDigit))
            {
                txtCUIT.BackColor = Color.FromArgb(255, 235, 238); // Fondo rojo claro
                return;
            }
            
            // Multiplicadores para el algoritmo del CUIT
            int[] multiplicadores = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
            
            int suma = 0;
            
            // Calcular la suma ponderada de los primeros 10 dígitos
            for (int i = 0; i < 10; i++)
            {
                suma += int.Parse(cuit[i].ToString()) * multiplicadores[i];
            }
            
            // Calcular el dígito verificador
            int resto = suma % 11;
            int digitoVerificadorCalculado;
            
            if (resto == 0)
                digitoVerificadorCalculado = 0;
            else if (resto == 1)
                digitoVerificadorCalculado = 9;
            else
                digitoVerificadorCalculado = 11 - resto;
            
            // Comparar con el dígito verificador ingresado
            int digitoVerificadorIngresado = int.Parse(cuit[10].ToString());
            
            if (digitoVerificadorCalculado == digitoVerificadorIngresado)
            {
                // CUIT válido - fondo verde claro
                txtCUIT.BackColor = Color.FromArgb(232, 245, 233);
            }
            else
            {
                // CUIT inválido - fondo rojo claro
                txtCUIT.BackColor = Color.FromArgb(255, 235, 238);
            }
        }

        /// <summary>
        /// Calcula el dígito verificador correcto para un CUIT de 10 dígitos
        /// </summary>
        /// <param name="cuitSinDigitoVerificador">Primeros 10 dígitos del CUIT</param>
        /// <returns>Dígito verificador calculado</returns>
        public static int CalcularDigitoVerificadorCUIT(string cuitSinDigitoVerificador)
        {
            if (cuitSinDigitoVerificador.Length != 10 || !cuitSinDigitoVerificador.All(char.IsDigit))
                throw new ArgumentException("Debe proporcionar exactamente 10 dígitos");
            
            int[] multiplicadores = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
            int suma = 0;
            
            for (int i = 0; i < 10; i++)
            {
                suma += int.Parse(cuitSinDigitoVerificador[i].ToString()) * multiplicadores[i];
            }
            
            int resto = suma % 11;
            
            if (resto == 0)
                return 0;
            else if (resto == 1)
                return 9;
            else
                return 11 - resto;
        }

        /// <summary>
        /// Valida completamente un CUIT con formato y dígito verificador
        /// </summary>
        /// <param name="cuit">CUIT a validar (con o sin guiones)</param>
        /// <returns>True si el CUIT es válido</returns>
        public static bool ValidarCUITCompleto(string cuit)
        {
            if (string.IsNullOrWhiteSpace(cuit))
                return false;
            
            // Limpiar guiones
            string cuitLimpio = cuit.Replace("-", "").Trim();
            
            // Validar longitud y que solo contenga números
            if (cuitLimpio.Length != 11 || !cuitLimpio.All(char.IsDigit))
                return false;
            
            // Validar que los primeros dos dígitos sean válidos para CUIT
            string prefijo = cuitLimpio.Substring(0, 2);
            string[] prefijosValidos = { "20", "23", "24", "27", "30", "33", "34" };
            
            if (!prefijosValidos.Contains(prefijo))
                return false;
            
            // Validar dígito verificador
            try
            {
                int digitoCalculado = CalcularDigitoVerificadorCUIT(cuitLimpio.Substring(0, 10));
                int digitoIngresado = int.Parse(cuitLimpio[10].ToString());
                
                return digitoCalculado == digitoIngresado;
            }
            catch
            {
                return false;
            }
        }

        // NUEVO: Formatear CUIT AFIP
        private void FormatearCUITAfip()
        {
            // Evitar recursión infinita
            if (txtAfipCuit.Text.Length == 0) return;

            // Guardar posición del cursor
            int cursorPosition = txtAfipCuit.SelectionStart;

            // Remover todos los guiones existentes y mantener solo números
            string soloNumeros = new string(txtAfipCuit.Text.Where(char.IsDigit).ToArray());

            // Limitar a máximo 11 dígitos
            if (soloNumeros.Length > 11)
            {
                soloNumeros = soloNumeros.Substring(0, 11);
            }

            string cuitFormateado = soloNumeros;

            // Aplicar formato XX-XXXXXXXX-X
            if (soloNumeros.Length > 2)
            {
                cuitFormateado = soloNumeros.Substring(0, 2) + "-" + soloNumeros.Substring(2);
            }

            if (soloNumeros.Length > 10)
            {
                cuitFormateado = soloNumeros.Substring(0, 2) + "-" + 
                                soloNumeros.Substring(2, 8) + "-" + 
                                soloNumeros.Substring(10);
            }

            // Solo actualizar si el texto cambió para evitar bucle infinito
            if (txtAfipCuit.Text != cuitFormateado)
            {
                // Temporalmente desconectar el evento para evitar recursión
                txtAfipCuit.TextChanged -= (s, e) => FormatearCUITAfip();
                
                txtAfipCuit.Text = cuitFormateado;
                
                // Ajustar posición del cursor
                int nuevaPosicion = Math.Min(cursorPosition, cuitFormateado.Length);
                
                // Si el cursor estaba después de una posición donde se insertó un guión, ajustar
                if (cursorPosition >= 2 && soloNumeros.Length > 2)
                    nuevaPosicion = Math.Min(cursorPosition + 1, cuitFormateado.Length);
                if (cursorPosition >= 10 && soloNumeros.Length > 10)
                    nuevaPosicion = Math.Min(nuevaPosicion + 1, cuitFormateado.Length);
                    
                txtAfipCuit.SelectionStart = nuevaPosicion;
                
                // Reconectar el evento
                txtAfipCuit.TextChanged += (s, e) => FormatearCUITAfip();
            }

            // Validar dígito verificador cuando está completo
            if (soloNumeros.Length == 11)
            {
                ValidarDigitoVerificadorCUITAfip(soloNumeros);
            }
            else
            {
                // Limpiar indicadores de validación si no está completo
                txtAfipCuit.BackColor = Color.White;
            }
        }

        // NUEVO: Validar dígito verificador para CUIT AFIP
        private void ValidarDigitoVerificadorCUITAfip(string cuit)
        {
            if (cuit.Length != 11 || !cuit.All(char.IsDigit))
            {
                txtAfipCuit.BackColor = Color.FromArgb(255, 235, 238); // Fondo rojo claro
                return;
            }
            
            // Usar el método estático existente para validar
            if (ValidarCUITCompleto(txtAfipCuit.Text))
            {
                txtAfipCuit.BackColor = Color.FromArgb(232, 245, 233); // Verde claro
            }
            else
            {
                txtAfipCuit.BackColor = Color.FromArgb(255, 235, 238); // Rojo claro
            }
        }

        private void SeleccionarCertificado()
        {
            try
            {
                using var openFileDialog = new OpenFileDialog
                {
                    Title = "Seleccionar Certificado AFIP",
                    Filter = "Archivos de Certificado (*.p12;*.pfx)|*.p12;*.pfx|Todos los archivos (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtAfipCertificadoPath.Text = openFileDialog.FileName;
                    MostrarMensaje($"✅ Certificado seleccionado: {Path.GetFileName(openFileDialog.FileName)}", Color.Green);
                    
                    // Auto-limpiar el mensaje después de 3 segundos
                    var timer = new System.Windows.Forms.Timer { Interval = 3000 };
                    timer.Tick += (s, e) =>
                    {
                        if (lblMensaje.Text.Contains("Certificado seleccionado"))
                            lblMensaje.Text = "";
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error seleccionando certificado: {ex.Message}", Color.Red);
            }
        }

        private void VerificarCertificadoAfip()
        {
            try
            {
                string rutaCertificado = txtAfipCertificadoPath.Text.Trim();
                string password = txtAfipCertificadoPassword.Text;

                if (string.IsNullOrWhiteSpace(rutaCertificado))
                {
                    MostrarMensaje("❌ Seleccione un certificado primero", Color.Red);
                    return;
                }

                if (!File.Exists(rutaCertificado))
                {
                    MostrarMensaje("❌ El archivo del certificado no existe", Color.Red);
                    return;
                }

                // Verificar usando el servicio existente de AFIP
                var (valido, mensaje, vencimiento) = Comercio.NET.Servicios.AfipAuthenticator.VerificarCertificado(rutaCertificado, password);

                if (valido)
                {
                    MostrarMensaje($"✅ Certificado válido - {mensaje}", Color.Green);
                    
                    // Mostrar detalles del certificado
                    string detalles = $"CERTIFICADO AFIP VERIFICADO\n" +
                                     $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                                     $"📄 Archivo: {Path.GetFileName(rutaCertificado)}\n" +
                                     $"📅 {mensaje}\n\n" +
                                     $"✅ El certificado es válido y puede utilizarse\n" +
                                     $"   para la facturación electrónica AFIP.";

                    MessageBox.Show(detalles, "Verificación de Certificado AFIP", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MostrarMensaje($"❌ Error en certificado: {mensaje}", Color.Red);
                    
                    string detallesError = $"PROBLEMA CON CERTIFICADO AFIP\n" +
                                          $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                                          $"📄 Archivo: {Path.GetFileName(rutaCertificado)}\n" +
                                          $"❌ Error: {mensaje}\n\n" +
                                          $"SOLUCIONES:\n" +
                                          $"• Verifique que sea un certificado válido de AFIP\n" +
                                          $"• Asegúrese de que la contraseña sea correcta\n" +
                                          $"• El archivo debe ser .p12 con clave privada incluida";

                    MessageBox.Show(detallesError, "Error en Certificado AFIP", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error verificando certificado: {ex.Message}", Color.Red);
            }
        }

        private void CargarDatosPrueba()
        {
            // Cargar datos de prueba para facilitar las pruebas de la interfaz
            txtNombreComercio.Text = "Comercio de Prueba";
            txtDomicilioComercio.Text = "Av. Siempre Viva 123";
            
            txtRazonSocial.Text = "Prueba S.R.L.";
            txtCUIT.Text = "20-12345678-9";
            txtIngBrutos.Text = "123456789";
            txtDomicilioFiscal.Text = "Calle Falsa 456";
            txtCodigoPostal.Text = "1234";
            dtpInicioActividades.Value = new DateTime(2022, 1, 1);
            cmbCondicion.SelectedItem = "Responsable Inscripto";

            // Opciones de facturación de prueba
            chkPermitirFacturaA.Checked = true;
            chkPermitirFacturaB.Checked = true;

            chkVerificarStock.Checked = true;
            
            // Datos de AFIP
            txtAfipCuit.Text = "20-12345678-9";
            txtAfipCertificadoPath.Text = @"C:\Certificados\mi_certificado.p12";
            txtAfipCertificadoPassword.Text = "clave123";
            txtAfipWSAAUrl.Text = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";
            txtAfipWSFEUrl.Text = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";
            
            // Nombres de cuentas corrientes de prueba
            lstNombresCtaCte.Items.Clear();
            lstNombresCtaCte.Items.AddRange(new string[] {
                "Cliente 1",
                "Cliente 2",
                "Cliente 3"
            });
            
            // Cadena de conexión de ejemplo (no funcional)
            txtConnectionString.Text = "Server=localhost;Database=comercio;User Id=miusuario;Password=miclave;";
        }

        // NUEVO: Crear sección de Cuentas Corrientes
        private Panel CrearSeccionCuentasCorrientesColapsable(string titulo, int y, int ancho)
        {
            var panel = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(ancho, 35),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Header con título y botón colapsar
            var panelHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(ancho, 30),
                BackColor = Color.FromArgb(230, 235, 240),
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(panelHeader);

            var lblTitulo = new Label
            {
                Text = titulo,
                Location = new Point(10, 6),
                Size = new Size(ancho - 50, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Cursor = Cursors.Hand
            };
            panelHeader.Controls.Add(lblTitulo);

            // Botón colapsar/expandir
            btnColapsarCuentasCorrientes = new Button
            {
                Text = "▼", // Iniciar expandido
                Location = new Point(ancho - 35, 3),
                Size = new Size(25, 24),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            btnColapsarCuentasCorrientes.FlatAppearance.BorderSize = 0;
            panelHeader.Controls.Add(btnColapsarCuentasCorrientes);

            // Contenido colapsable
            var panelContenido = new Panel
            {
                Name = "panelContenidoCuentasCorrientes",
                Location = new Point(0, 30),
                Size = new Size(ancho, 190), // Altura aumentada
                BackColor = Color.FromArgb(248, 250, 252),
                Visible = true // Iniciar expandido
            };
            panel.Controls.Add(panelContenido);

            // Descripción
            var lblDescripcionCtaCte = new Label
            {
                Text = "Lista de nombres para cuentas corrientes disponibles en el formulario de ventas:",
                Location = new Point(15, 10),
                Size = new Size(550, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(62, 80, 100)
            };
            panelContenido.Controls.Add(lblDescripcionCtaCte);

            // Lista de nombres con mayor altura
            lstNombresCtaCte = new ListBox
            {
                Location = new Point(15, 35),
                Size = new Size(300, 130), // Altura aumentada
                Font = new Font("Segoe UI", 9F),
                SelectionMode = SelectionMode.One,
                BorderStyle = BorderStyle.FixedSingle
            };
            panelContenido.Controls.Add(lstNombresCtaCte);

            // Panel para agregar nuevo nombre
            var panelAgregar = new Panel
            {
                Location = new Point(330, 35),
                Size = new Size(240, 130), // Altura aumentada para alinearse
                BackColor = Color.FromArgb(240, 245, 250),
                BorderStyle = BorderStyle.FixedSingle
            };
            panelContenido.Controls.Add(panelAgregar);

            var lblNuevoNombre = new Label
            {
                Text = "Agregar nuevo nombre:",
                Location = new Point(10, 10),
                Size = new Size(220, 20),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100)
            };
            panelAgregar.Controls.Add(lblNuevoNombre);

            txtNuevoNombreCtaCte = new TextBox
            {
                Location = new Point(10, 30),
                Size = new Size(220, 22),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Ingrese el nombre..."
            };
            panelAgregar.Controls.Add(txtNuevoNombreCtaCte);

            // Botones reposicionados para mejor distribución
            btnAgregarNombre = new Button
            {
                Text = "➕ Agregar",
                Location = new Point(10, 60),
                Size = new Size(70, 25),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };
            btnAgregarNombre.FlatAppearance.BorderSize = 0;
            panelAgregar.Controls.Add(btnAgregarNombre);

            btnEditarNombre = new Button
            {
                Text = "✏️ Editar",
                Location = new Point(85, 60),
                Size = new Size(70, 25),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Enabled = false
            };
            btnEditarNombre.FlatAppearance.BorderSize = 0;
            panelAgregar.Controls.Add(btnEditarNombre);

            btnEliminarNombre = new Button
            {
                Text = "🗑️ Quitar",
                Location = new Point(160, 60),
                Size = new Size(70, 25),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Enabled = false
            };
            btnEliminarNombre.FlatAppearance.BorderSize = 0;
            panelAgregar.Controls.Add(btnEliminarNombre);

            // Etiqueta informativa
            var lblInfo = new Label
            {
                Text = "💡 Tip: Estos nombres aparecerán como opciones al activar 'Cuenta Corriente' en ventas",
                Location = new Point(10, 95),
                Size = new Size(220, 30),
                Font = new Font("Segoe UI", 7F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.TopLeft
            };
            panelAgregar.Controls.Add(lblInfo);

            // Configurar eventos
            EventHandler clickHandler = (s, e) => ToggleColapsarCuentasCorrientes();
            panelHeader.Click += clickHandler;
            lblTitulo.Click += clickHandler;

            return panel;
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Liberar el ToolTip
                toolTip?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}