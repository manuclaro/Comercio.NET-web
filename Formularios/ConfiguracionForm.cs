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
using System.Globalization;

namespace Comercio.NET.Formularios
{
    public partial class ConfiguracionForm : Form
    {
        // Controles del formulario
        private TextBox txtNombreComercio, txtDomicilioComercio;
        // Nuevos controles para facturación
        private TextBox txtRazonSocial, txtCUIT, txtIngBrutos, txtDomicilioFiscal, txtCodigoPostal;
        private DateTimePicker dtpInicioActividades;
        private ComboBox cmbCondicion;

        // NUEVO: Controles para Cuentas Corrientes
        private ListBox lstNombresCtaCte;
        private TextBox txtNuevoNombreCtaCte;
        private Button btnAgregarNombre, btnEliminarNombre, btnEditarNombre;

        // NUEVO: Controles para configuración AFIP
        //private TextBox txtAfipCuit, txtAfipCertificadoPath, txtAfipCertificadoPassword;
        //private TextBox txtAfipWSAAUrl, txtAfipWSFEUrl;
        //private Button btnColapsarAfip, btnSeleccionarCertificado, btnVerificarCertificado;
        private Button btnColapsarAfip;
        // Controles duplicados para ambiente de Testing
        private TextBox txtAfipTestingCuit, txtAfipTestingCertificadoPath, txtAfipTestingCertificadoPassword;
        private TextBox txtAfipTestingWSAAUrl, txtAfipTestingWSFEUrl;
        private Button btnSeleccionarCertificadoTesting, btnVerificarCertificadoTesting;

        private Panel panelDescuentos;
        private Button btnColapsarDescuentos;
        private bool _descuentosColapsado = false;

        // NUEVO: Controles para configuración de descuentos
        private ListBox lstOpcionesDescuento;
        private TextBox txtNuevaOpcionDescuento;
        private Button btnAgregarOpcionDescuento, btnEliminarOpcionDescuento, btnEditarOpcionDescuento;
        private TextBox txtPorcentajeMaximo;
        private CheckBox chkRestringirPorMetodoPago;
        private CheckedListBox chkListMetodosPago;

        // Controles duplicados para ambiente de Producción  
        private TextBox txtAfipProduccionCuit, txtAfipProduccionCertificadoPath, txtAfipProduccionCertificadoPassword;
        private TextBox txtAfipProduccionWSAAUrl, txtAfipProduccionWSFEUrl;
        private Button btnSeleccionarCertificadoProduccion, btnVerificarCertificadoProduccion;
        private Panel panelAfip;

        private TextBox txtAfipTestingPuntoVenta, txtAfipProduccionPuntoVenta;

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
        private CheckBox chkPermitirFacturaC; // ✅ NUEVO

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

        // AGREGAR: En la sección de variables de la clase (línea ~54)
        private CheckBox chkVistaPreviaImpresionDirecta; // NUEVO: Control para elegir entre vista previa o impresión directa
        private CheckBox chkLimitarFacturacion; // NUEVO: Control para limitar facturación
        private TextBox txtMontoLimiteFacturacion; // NUEVO: Control para el monto límite

        // Controles para selección de ambiente AFIP
        private RadioButton rbAfipTesting, rbAfipProduccion;
        private Panel panelAfipTesting, panelAfipProduccion;

        // Controles para selección de ambiente de BD
        private RadioButton rbDbTesting, rbDbProduccion;
        private Panel panelDbTesting, panelDbProduccion;
        private TextBox txtConnectionStringTesting, txtConnectionStringProduccion;
        private Button btnTestearConexionTesting, btnTestearConexionProduccion;

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

        private Panel CrearPanelAmbienteBD(string ambiente, int y, int ancho, bool esTesting)
        {
            var panel = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(ancho, 260), // ✅ AUMENTADO de 230 a 260 para más espacio
                BackColor = Color.FromArgb(245, 248, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = esTesting // Solo Testing visible por defecto
            };

            // Título del ambiente
            var lblTituloAmbiente = new Label
            {
                Text = esTesting ? "🧪 AMBIENTE DE TESTING (Desarrollo)" : "🏭 AMBIENTE DE PRODUCCIÓN",
                Location = new Point(10, 10),
                Size = new Size(ancho - 20, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = esTesting ? Color.FromArgb(255, 152, 0) : Color.FromArgb(76, 175, 80),
                BackColor = esTesting ? Color.FromArgb(255, 243, 224) : Color.FromArgb(232, 245, 233),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(lblTituloAmbiente);

            // Label para cadena de conexión
            var lblConnectionString = new Label
            {
                Text = "Cadena de Conexión:",
                Location = new Point(15, 45),
                Size = new Size(130, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100)
            };
            panel.Controls.Add(lblConnectionString);

            // TextBox multilinea para la cadena de conexión
            var txtConnString = new TextBox
            {
                Location = new Point(15, 70),
                Size = new Size(ancho - 100, 80), // ✅ REDUCIDO de 100 a 80 de altura
                Font = new Font("Consolas", 8F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = esTesting
                    ? "Server=localhost;Database=comercio_dev;Trusted_Connection=True;TrustServerCertificate=True;"
                    : "Server=production-server;Database=comercio;User Id=usuario;Password=****;TrustServerCertificate=True;",
                ReadOnly = true, // ✅ AGREGADO: Inicialmente bloqueado
                BackColor = Color.FromArgb(245, 245, 245), // ✅ AGREGADO: Color de fondo bloqueado
                ForeColor = Color.Gray // ✅ AGREGADO: Texto gris cuando bloqueado
            };

            if (esTesting)
                txtConnectionStringTesting = txtConnString;
            else
                txtConnectionStringProduccion = txtConnString;

            panel.Controls.Add(txtConnString);

            // Botón testear conexión
            var btnTest = new Button
            {
                Text = "🔧\nTest",
                Location = new Point(ancho - 75, 70),
                Size = new Size(60, 50),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                Enabled = false // ✅ AGREGADO: Inicialmente deshabilitado
            };
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.Click += esTesting
                ? (s, e) => TestearConexionTesting()
                : (s, e) => TestearConexionProduccion();

            if (esTesting)
                btnTestearConexionTesting = btnTest;
            else
                btnTestearConexionProduccion = btnTest;

            panel.Controls.Add(btnTest);

            // Nota importante (ahora más arriba)
            var lblNota = new Label
            {
                Text = esTesting
                    ? "⚠️ Base de datos de DESARROLLO - Para pruebas y desarrollo"
                    : "🔴 Base de datos REAL - Datos de producción en vivo",
                Location = new Point(15, 160), // ✅ MOVIDO de 130 a 160
                Size = new Size(ancho - 30, 35),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = esTesting ? Color.DarkOrange : Color.DarkRed,
                BackColor = esTesting ? Color.FromArgb(255, 249, 235) : Color.FromArgb(255, 235, 238),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(lblNota);

            // Información de ejemplo (ahora más abajo y visible)
            var lblEjemplo = new Label
            {
                Text = "Ejemplo:\n" + (esTesting
                    ? "Server=localhost;Database=comercio_dev;Trusted_Connection=True;"
                    : "Server=192.168.1.100;Database=comercio;User Id=admin;Password=****;"),
                Location = new Point(15, 205), // ✅ MOVIDO de 175 a 205
                Size = new Size(ancho - 30, 40),
                Font = new Font("Consolas", 7F),
                ForeColor = Color.Gray
            };
            panel.Controls.Add(lblEjemplo);

            return panel;
        }

        private void RbDbAmbiente_CheckedChanged(object sender, EventArgs e)
        {
            if (rbDbTesting.Checked)
            {
                panelDbTesting.Visible = true;
                panelDbProduccion.Visible = false;
                System.Diagnostics.Debug.WriteLine("[CONFIG] Seleccionado ambiente BD TESTING");
            }
            else if (rbDbProduccion.Checked)
            {
                panelDbTesting.Visible = false;
                panelDbProduccion.Visible = true;
                System.Diagnostics.Debug.WriteLine("[CONFIG] Seleccionado ambiente BD PRODUCCIÓN");
            }
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

            // ✅ NUEVO: CheckBox para Factura C
            chkPermitirFacturaC = new CheckBox
            {
                Name = "chkPermitirFacturaC",
                Text = "Permitir Factura C",
                Location = new Point(365, 130),
                Size = new Size(160, 22),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(62, 80, 100),
                Checked = true
            };
            panelContenido.Controls.Add(chkPermitirFacturaC);

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

            // ✅ NUEVA SECCIÓN: CONFIGURACIÓN DE DESCUENTOS
            panelDescuentos = CrearSeccionDescuentosColapsable("💰 CONFIGURACIÓN DE DESCUENTOS", 0, ancho);
            panelPrincipal.Controls.Add(panelDescuentos);

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

        // NUEVO: Método para crear la sección de descuentos
        private Panel CrearSeccionDescuentosColapsable(string titulo, int y, int ancho)
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
            btnColapsarDescuentos = new Button
            {
                Text = "▼",
                Location = new Point(ancho - 35, 3),
                Size = new Size(25, 24),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            btnColapsarDescuentos.FlatAppearance.BorderSize = 0;
            panelHeader.Controls.Add(btnColapsarDescuentos);

            // Contenido colapsable
            var panelContenido = new Panel
            {
                Name = "panelContenidoDescuentos",
                Location = new Point(0, 30),
                Size = new Size(ancho, 380),
                BackColor = Color.FromArgb(248, 250, 252),
                Visible = true
            };
            panel.Controls.Add(panelContenido);

            // === OPCIONES DE DESCUENTO DISPONIBLES ===
            var lblOpcionesDescuento = new Label
            {
                Text = "Opciones de descuento disponibles (%):",
                Location = new Point(15, 10),
                Size = new Size(280, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100)
            };
            panelContenido.Controls.Add(lblOpcionesDescuento);

            // Lista de opciones
            lstOpcionesDescuento = new ListBox
            {
                Location = new Point(15, 35),
                Size = new Size(150, 120),
                Font = new Font("Segoe UI", 9F),
                SelectionMode = SelectionMode.One,
                BorderStyle = BorderStyle.FixedSingle
            };
            lstOpcionesDescuento.SelectedIndexChanged += (s, e) => ActualizarBotonesDescuento();
            panelContenido.Controls.Add(lstOpcionesDescuento);

            // Panel para agregar nueva opción
            var panelAgregarOpcion = new Panel
            {
                Location = new Point(175, 35),
                Size = new Size(200, 120),
                BackColor = Color.FromArgb(240, 245, 250),
                BorderStyle = BorderStyle.FixedSingle
            };
            panelContenido.Controls.Add(panelAgregarOpcion);

            var lblNuevaOpcion = new Label
            {
                Text = "Agregar opción (%):",
                Location = new Point(10, 10),
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100)
            };
            panelAgregarOpcion.Controls.Add(lblNuevaOpcion);

            txtNuevaOpcionDescuento = new TextBox
            {
                Location = new Point(10, 30),
                Size = new Size(180, 22),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Ej: 15"
            };
            // Validar solo números
            txtNuevaOpcionDescuento.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            };
            panelAgregarOpcion.Controls.Add(txtNuevaOpcionDescuento);

            // Botones de gestión
            btnAgregarOpcionDescuento = new Button
            {
                Text = "➕ Agregar",
                Location = new Point(10, 60),
                Size = new Size(55, 25),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };
            btnAgregarOpcionDescuento.FlatAppearance.BorderSize = 0;
            btnAgregarOpcionDescuento.Click += (s, e) => AgregarOpcionDescuento();
            panelAgregarOpcion.Controls.Add(btnAgregarOpcionDescuento);

            btnEditarOpcionDescuento = new Button
            {
                Text = "✏️ Editar",
                Location = new Point(70, 60),
                Size = new Size(55, 25),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Enabled = false
            };
            btnEditarOpcionDescuento.FlatAppearance.BorderSize = 0;
            btnEditarOpcionDescuento.Click += (s, e) => EditarOpcionDescuento();
            panelAgregarOpcion.Controls.Add(btnEditarOpcionDescuento);

            btnEliminarOpcionDescuento = new Button
            {
                Text = "🗑️ Quitar",
                Location = new Point(130, 60),
                Size = new Size(60, 25),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Enabled = false
            };
            btnEliminarOpcionDescuento.FlatAppearance.BorderSize = 0;
            btnEliminarOpcionDescuento.Click += (s, e) => EliminarOpcionDescuento();
            panelAgregarOpcion.Controls.Add(btnEliminarOpcionDescuento);

            // === PORCENTAJE MÁXIMO ===
            var lblPorcentajeMaximo = new Label
            {
                Text = "Porcentaje máximo de descuento:",
                Location = new Point(15, 170),
                Size = new Size(220, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100)
            };
            panelContenido.Controls.Add(lblPorcentajeMaximo);

            txtPorcentajeMaximo = new TextBox
            {
                Location = new Point(240, 168),
                Size = new Size(80, 22),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Ej: 20"
            };
            // Validar solo números
            txtPorcentajeMaximo.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            };
            panelContenido.Controls.Add(txtPorcentajeMaximo);

            var lblPorcentaje = new Label
            {
                Text = "%",
                Location = new Point(325, 170),
                Size = new Size(20, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100)
            };
            panelContenido.Controls.Add(lblPorcentaje);

            var lblInfoMaximo = new Label
            {
                Text = "Este es el valor máximo de descuento que puede aplicarse en el sistema",
                Location = new Point(35, 195),
                Size = new Size(520, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray
            };
            panelContenido.Controls.Add(lblInfoMaximo);

            // === RESTRICCIÓN POR MÉTODO DE PAGO ===
            chkRestringirPorMetodoPago = new CheckBox
            {
                Text = "Restringir descuentos por método de pago",
                Location = new Point(15, 230),
                Size = new Size(350, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100),
                Checked = true
            };
            chkRestringirPorMetodoPago.CheckedChanged += (s, e) =>
            {
                chkListMetodosPago.Enabled = chkRestringirPorMetodoPago.Checked;
            };
            panelContenido.Controls.Add(chkRestringirPorMetodoPago);

            var lblMetodosPago = new Label
            {
                Text = "Métodos de pago permitidos para descuentos:",
                Location = new Point(35, 260),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100)
            };
            panelContenido.Controls.Add(lblMetodosPago);

            // CheckedListBox para métodos de pago
            chkListMetodosPago = new CheckedListBox
            {
                Location = new Point(35, 285),
                Size = new Size(200, 80),
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.FixedSingle,
                CheckOnClick = true
            };
            chkListMetodosPago.Items.AddRange(new string[] {
        "Efectivo",
        "DNI",
        "MercadoPago"
    });
            panelContenido.Controls.Add(chkListMetodosPago);

            var lblInfoMetodos = new Label
            {
                Text = "Seleccione los métodos de pago en los que se permitirán descuentos.\n" +
                       "Si no restringe, los descuentos estarán disponibles para todos los métodos.",
                Location = new Point(250, 285),
                Size = new Size(310, 40),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray
            };
            panelContenido.Controls.Add(lblInfoMetodos);

            // Configurar eventos
            EventHandler clickHandler = (s, e) => ToggleColapsarDescuentos();
            panelHeader.Click += clickHandler;
            lblTitulo.Click += clickHandler;

            return panel;
        }

        // NUEVO: Toggle para la sección de descuentos
        private void ToggleColapsarDescuentos()
        {
            _descuentosColapsado = !_descuentosColapsado;
            ActualizarEstadoSeccion(panelDescuentos, "panelContenidoDescuentos",
                _descuentosColapsado, btnColapsarDescuentos, 35, 415);
            ActualizarPosicionesTodasLasSecciones();
        }

        // NUEVO: Métodos para gestionar opciones de descuento
        private void AgregarOpcionDescuento()
        {
            string porcentajeTexto = txtNuevaOpcionDescuento.Text.Trim();

            if (string.IsNullOrWhiteSpace(porcentajeTexto))
            {
                MostrarMensaje("❌ Ingrese un porcentaje válido", Color.Red);
                txtNuevaOpcionDescuento.Focus();
                return;
            }

            if (!int.TryParse(porcentajeTexto, out int porcentaje) || porcentaje <= 0 || porcentaje > 100)
            {
                MostrarMensaje("❌ El porcentaje debe estar entre 1 y 100", Color.Red);
                txtNuevaOpcionDescuento.Focus();
                txtNuevaOpcionDescuento.SelectAll();
                return;
            }

            // Verificar si ya existe
            foreach (var item in lstOpcionesDescuento.Items)
            {
                if (int.TryParse(item.ToString(), out int existente) && existente == porcentaje)
                {
                    MostrarMensaje("❌ Este porcentaje ya existe en la lista", Color.Red);
                    txtNuevaOpcionDescuento.Focus();
                    txtNuevaOpcionDescuento.SelectAll();
                    return;
                }
            }

            lstOpcionesDescuento.Items.Add(porcentaje);
            // Ordenar la lista
            var items = lstOpcionesDescuento.Items.Cast<int>().OrderBy(x => x).ToList();
            lstOpcionesDescuento.Items.Clear();
            foreach (var item in items)
            {
                lstOpcionesDescuento.Items.Add(item);
            }

            txtNuevaOpcionDescuento.Clear();
            txtNuevaOpcionDescuento.Focus();

            MostrarMensaje($"✅ Opción {porcentaje}% agregada correctamente", Color.Green);
        }

        private void EditarOpcionDescuento()
        {
            if (lstOpcionesDescuento.SelectedIndex == -1)
            {
                MostrarMensaje("❌ Seleccione una opción para editar", Color.Red);
                return;
            }

            string opcionActual = lstOpcionesDescuento.SelectedItem.ToString();
            string opcionNueva = Microsoft.VisualBasic.Interaction.InputBox(
                $"Editar opción de descuento:\n\nPorcentaje actual: {opcionActual}%\n\nIngrese el nuevo porcentaje (1-100):",
                "Editar Opción de Descuento",
                opcionActual);

            if (string.IsNullOrWhiteSpace(opcionNueva) || opcionNueva == opcionActual)
            {
                return;
            }

            if (!int.TryParse(opcionNueva, out int porcentaje) || porcentaje <= 0 || porcentaje > 100)
            {
                MostrarMensaje("❌ El porcentaje debe estar entre 1 y 100", Color.Red);
                return;
            }

            // Verificar si el nuevo porcentaje ya existe
            foreach (var item in lstOpcionesDescuento.Items)
            {
                if (item.ToString() != opcionActual &&
                    int.TryParse(item.ToString(), out int existente) &&
                    existente == porcentaje)
                {
                    MostrarMensaje("❌ Este porcentaje ya existe en la lista", Color.Red);
                    return;
                }
            }

            int indiceSeleccionado = lstOpcionesDescuento.SelectedIndex;
            lstOpcionesDescuento.Items[indiceSeleccionado] = porcentaje;

            // Reordenar
            var items = lstOpcionesDescuento.Items.Cast<int>().OrderBy(x => x).ToList();
            lstOpcionesDescuento.Items.Clear();
            foreach (var item in items)
            {
                lstOpcionesDescuento.Items.Add(item);
            }

            MostrarMensaje($"✅ Opción actualizada a {porcentaje}%", Color.Green);
        }

        private void EliminarOpcionDescuento()
        {
            if (lstOpcionesDescuento.SelectedIndex == -1)
            {
                MostrarMensaje("❌ Seleccione una opción para eliminar", Color.Red);
                return;
            }

            string opcionSeleccionada = lstOpcionesDescuento.SelectedItem.ToString();

            var resultado = MessageBox.Show(
                $"¿Está seguro de eliminar la opción de descuento {opcionSeleccionada}%?",
                "Confirmar Eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (resultado == DialogResult.Yes)
            {
                lstOpcionesDescuento.Items.RemoveAt(lstOpcionesDescuento.SelectedIndex);
                MostrarMensaje($"✅ Opción {opcionSeleccionada}% eliminada", Color.Green);
            }
        }

        private void ActualizarBotonesDescuento()
        {
            bool haySeleccion = lstOpcionesDescuento.SelectedIndex != -1;
            btnEditarOpcionDescuento.Enabled = haySeleccion;
            btnEliminarOpcionDescuento.Enabled = haySeleccion;
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

            // Contenido colapsable - ALTURA AUMENTADA
            var panelContenido = new Panel
            {
                Name = "panelContenidoAfip",
                Location = new Point(0, 30),
                Size = new Size(ancho, 430), // Altura aumentada para acomodar dos ambientes
                BackColor = Color.FromArgb(248, 250, 252),
                Visible = true
            };
            panel.Controls.Add(panelContenido);

            // === SELECTOR DE AMBIENTE ===
            var lblAmbiente = new Label
            {
                Text = "🌐 Ambiente AFIP a utilizar:",
                Location = new Point(15, 10),
                Size = new Size(180, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            };
            panelContenido.Controls.Add(lblAmbiente);

            rbAfipTesting = new RadioButton
            {
                Text = "🧪 Testing (Homologación)",
                Location = new Point(200, 12),
                Size = new Size(180, 22),
                Font = new Font("Segoe UI", 9F),
                Checked = true // Por defecto Testing
            };
            rbAfipTesting.CheckedChanged += RbAfipAmbiente_CheckedChanged;
            panelContenido.Controls.Add(rbAfipTesting);

            rbAfipProduccion = new RadioButton
            {
                Text = "🏭 Producción",
                Location = new Point(390, 12),
                Size = new Size(130, 22),
                Font = new Font("Segoe UI", 9F)
            };
            rbAfipProduccion.CheckedChanged += RbAfipAmbiente_CheckedChanged;
            panelContenido.Controls.Add(rbAfipProduccion);

            // Línea separadora
            var separador = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(15, 40),
                Size = new Size(ancho - 30, 2)
            };
            panelContenido.Controls.Add(separador);

            // === PANEL TESTING ===
            panelAfipTesting = CrearPanelAmbienteAfip("TESTING", 50, ancho - 30, true);
            panelContenido.Controls.Add(panelAfipTesting);

            // === PANEL PRODUCCIÓN ===
            panelAfipProduccion = CrearPanelAmbienteAfip("PRODUCCION", 50, ancho - 30, false);
            panelContenido.Controls.Add(panelAfipProduccion);

            // Información adicional
            var lblInfo = new Label
            {
                Text = "💡 Configure ambos ambientes y seleccione cuál desea utilizar. Los cambios se aplicarán al guardar.",
                Location = new Point(15, 395),
                Size = new Size(ancho - 30, 30),
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

        private Panel CrearPanelAmbienteAfip(string ambiente, int y, int ancho, bool esTesting)
        {
            var panel = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(ancho, 360), // Altura aumentada para el nuevo campo
                BackColor = Color.FromArgb(245, 248, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = esTesting // Solo Testing visible por defecto
            };

            // Título del ambiente
            var lblTituloAmbiente = new Label
            {
                Text = esTesting ? "🧪 AMBIENTE DE TESTING (Homologación)" : "🏭 AMBIENTE DE PRODUCCIÓN",
                Location = new Point(10, 10),
                Size = new Size(ancho - 20, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = esTesting ? Color.FromArgb(255, 152, 0) : Color.FromArgb(76, 175, 80),
                BackColor = esTesting ? Color.FromArgb(255, 243, 224) : Color.FromArgb(232, 245, 233),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(lblTituloAmbiente);

            // CUIT
            panel.Controls.Add(CrearLabel("CUIT:", 15, 45));
            var txtCuit = new TextBox
            {
                Location = new Point(120, 43),
                Size = new Size(150, 22),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "XX-XXXXXXXX-X"
            };

            if (esTesting)
            {
                txtAfipTestingCuit = txtCuit;
                txtCuit.TextChanged += (s, e) => FormatearCUITAfipTesting();
            }
            else
            {
                txtAfipProduccionCuit = txtCuit;
                txtCuit.TextChanged += (s, e) => FormatearCUITAfipProduccion();
            }
            panel.Controls.Add(txtCuit);

            // ✅ NUEVO: Punto de Venta
            panel.Controls.Add(CrearLabel("Punto de Venta:", 290, 45));
            var txtPuntoVenta = new TextBox
            {
                Location = new Point(400, 43),
                Size = new Size(80, 22),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "1-9999",
                MaxLength = 4
            };

            // Validar que solo sean números
            txtPuntoVenta.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            };

            if (esTesting)
            {
                txtAfipTestingPuntoVenta = txtPuntoVenta;
            }
            else
            {
                txtAfipProduccionPuntoVenta = txtPuntoVenta;
            }
            panel.Controls.Add(txtPuntoVenta);

            // ✅ Tooltip para punto de venta
            toolTip.SetToolTip(txtPuntoVenta,
                "Número de punto de venta asignado por AFIP\n" +
                "Rango válido: 1 a 9999");

            // Certificado (ahora en línea 75 en lugar de 45)
            panel.Controls.Add(CrearLabel("Certificado (.p12):", 15, 75));
            var txtCertPath = new TextBox
            {
                Location = new Point(120, 73),
                Size = new Size(350, 22),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "C:\\ruta\\al\\certificado.p12",
                ReadOnly = true,
                BackColor = Color.FromArgb(250, 250, 250)
            };

            if (esTesting)
                txtAfipTestingCertificadoPath = txtCertPath;
            else
                txtAfipProduccionCertificadoPath = txtCertPath;

            panel.Controls.Add(txtCertPath);

            var btnSelCert = new Button
            {
                Text = "📁",
                Location = new Point(480, 73),
                Size = new Size(30, 22),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F),
                UseVisualStyleBackColor = false
            };
            btnSelCert.FlatAppearance.BorderSize = 0;
            toolTip.SetToolTip(btnSelCert, "Seleccionar certificado");
            btnSelCert.Click += esTesting ?
                (s, e) => SeleccionarCertificadoTesting() :
                (s, e) => SeleccionarCertificadoProduccion();

            if (esTesting)
                btnSeleccionarCertificadoTesting = btnSelCert;
            else
                btnSeleccionarCertificadoProduccion = btnSelCert;

            panel.Controls.Add(btnSelCert);

            var btnVerCert = new Button
            {
                Text = "✅",
                Location = new Point(520, 73),
                Size = new Size(30, 22),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F),
                UseVisualStyleBackColor = false
            };
            btnVerCert.FlatAppearance.BorderSize = 0;
            toolTip.SetToolTip(btnVerCert, "Verificar certificado");
            btnVerCert.Click += esTesting ?
                (s, e) => VerificarCertificadoAfipTesting() :
                (s, e) => VerificarCertificadoAfipProduccion();

            if (esTesting)
                btnVerificarCertificadoTesting = btnVerCert;
            else
                btnVerificarCertificadoProduccion = btnVerCert;

            panel.Controls.Add(btnVerCert);

            // Contraseña
            panel.Controls.Add(CrearLabel("Contraseña:", 15, 105));
            var txtPass = new TextBox
            {
                Location = new Point(120, 103),
                Size = new Size(200, 22),
                Font = new Font("Segoe UI", 9F),
                UseSystemPasswordChar = true,
                PlaceholderText = "Contraseña del certificado"
            };

            if (esTesting)
                txtAfipTestingCertificadoPassword = txtPass;
            else
                txtAfipProduccionCertificadoPassword = txtPass;

            panel.Controls.Add(txtPass);

            // WSAA URL
            panel.Controls.Add(CrearLabel("WSAA URL:", 15, 135));
            var txtWSAA = new TextBox
            {
                Location = new Point(120, 133),
                Size = new Size(430, 22),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = esTesting ?
                    "https://wsaahomo.afip.gov.ar/ws/services/LoginCms" :
                    "https://wsaa.afip.gov.ar/ws/services/LoginCms",
                Text = esTesting ?
                    "https://wsaahomo.afip.gov.ar/ws/services/LoginCms" :
                    "https://wsaa.afip.gov.ar/ws/services/LoginCms"
            };

            if (esTesting)
                txtAfipTestingWSAAUrl = txtWSAA;
            else
                txtAfipProduccionWSAAUrl = txtWSAA;

            panel.Controls.Add(txtWSAA);

            // WSFE URL
            panel.Controls.Add(CrearLabel("WSFE URL:", 15, 165));
            var txtWSFE = new TextBox
            {
                Location = new Point(120, 163),
                Size = new Size(430, 22),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = esTesting ?
                    "https://wswhomo.afip.gov.ar/wsfev1/service.asmx" :
                    "https://servicios1.afip.gov.ar/wsfev1/service.asmx",
                Text = esTesting ?
                    "https://wswhomo.afip.gov.ar/wsfev1/service.asmx" :
                    "https://servicios1.afip.gov.ar/wsfev1/service.asmx"
            };

            if (esTesting)
                txtAfipTestingWSFEUrl = txtWSFE;
            else
                txtAfipProduccionWSFEUrl = txtWSFE;

            panel.Controls.Add(txtWSFE);

            // Nota importante
            var lblNota = new Label
            {
                Text = esTesting ?
                    "⚠️ Ambiente de PRUEBAS - Las facturas generadas NO tienen validez fiscal" :
                    "🔴 AMBIENTE REAL - Las facturas generadas TIENEN validez fiscal oficial",
                Location = new Point(15, 195),
                Size = new Size(ancho - 30, 40),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = esTesting ? Color.DarkOrange : Color.DarkRed,
                BackColor = esTesting ? Color.FromArgb(255, 249, 235) : Color.FromArgb(255, 235, 238),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(lblNota);

            return panel;
        }

        private void RbAfipAmbiente_CheckedChanged(object sender, EventArgs e)
        {
            if (rbAfipTesting.Checked)
            {
                panelAfipTesting.Visible = true;
                panelAfipProduccion.Visible = false;
                System.Diagnostics.Debug.WriteLine("[CONFIG] Seleccionado ambiente TESTING");
            }
            else if (rbAfipProduccion.Checked)
            {
                panelAfipTesting.Visible = false;
                panelAfipProduccion.Visible = true;
                System.Diagnostics.Debug.WriteLine("[CONFIG] Seleccionado ambiente PRODUCCIÓN");
            }
        }

        // 5. AGREGAR métodos de selección de certificados:

        private void SeleccionarCertificadoTesting()
        {
            SeleccionarCertificadoParaAmbiente(txtAfipTestingCertificadoPath, "Testing");
        }

        private void SeleccionarCertificadoProduccion()
        {
            SeleccionarCertificadoParaAmbiente(txtAfipProduccionCertificadoPath, "Producción");
        }

        private void SeleccionarCertificadoParaAmbiente(TextBox textBox, string ambiente)
        {
            try
            {
                using var openFileDialog = new OpenFileDialog
                {
                    Title = $"Seleccionar Certificado AFIP ({ambiente})",
                    Filter = "Archivos de Certificado (*.p12;*.pfx)|*.p12;*.pfx|Todos los archivos (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    textBox.Text = openFileDialog.FileName;
                    MostrarMensaje($"✅ Certificado {ambiente} seleccionado: {Path.GetFileName(openFileDialog.FileName)}", Color.Green);

                    var timer = new System.Windows.Forms.Timer { Interval = 3000 };
                    timer.Tick += (s, e) =>
                    {
                        if (lblMensaje.Text.Contains("Certificado"))
                            lblMensaje.Text = "";
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error seleccionando certificado {ambiente}: {ex.Message}", Color.Red);
            }
        }

        // 6. AGREGAR métodos de verificación de certificados:

        private void VerificarCertificadoAfipTesting()
        {
            VerificarCertificadoParaAmbiente(
                txtAfipTestingCertificadoPath.Text,
                txtAfipTestingCertificadoPassword.Text,
                "Testing"
            );
        }

        private void VerificarCertificadoAfipProduccion()
        {
            VerificarCertificadoParaAmbiente(
                txtAfipProduccionCertificadoPath.Text,
                txtAfipProduccionCertificadoPassword.Text,
                "Producción"
            );
        }

        private void VerificarCertificadoParaAmbiente(string rutaCertificado, string password, string ambiente)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rutaCertificado))
                {
                    MostrarMensaje($"❌ Seleccione un certificado {ambiente} primero", Color.Red);
                    return;
                }

                if (!File.Exists(rutaCertificado))
                {
                    MostrarMensaje($"❌ El archivo del certificado {ambiente} no existe", Color.Red);
                    return;
                }

                var (valido, mensaje, vencimiento) = Comercio.NET.Servicios.AfipAuthenticator.VerificarCertificado(rutaCertificado, password);

                if (valido)
                {
                    MostrarMensaje($"✅ Certificado {ambiente} válido - {mensaje}", Color.Green);
                    
                    string detalles = $"CERTIFICADO AFIP {ambiente.ToUpper()} VERIFICADO\n" +
                                     $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                                     $"📄 Archivo: {Path.GetFileName(rutaCertificado)}\n" +
                                     $"📅 {mensaje}\n\n" +
                                     $"✅ El certificado es válido y puede utilizarse\n" +
                                     $"   para la facturación electrónica AFIP en ambiente {ambiente}.";

                    MessageBox.Show(detalles, $"Verificación de Certificado AFIP ({ambiente})",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MostrarMensaje($"❌ Error en certificado {ambiente}: {mensaje}", Color.Red);

                    string detallesError = $"PROBLEMA CON CERTIFICADO AFIP {ambiente.ToUpper()}\n" +
                                          $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                                          $"📄 Archivo: {Path.GetFileName(rutaCertificado)}\n" +
                                          $"❌ Error: {mensaje}\n\n" +
                                          $"SOLUCIONES:\n" +
                                          $"• Verifique que sea un certificado válido de AFIP\n" +
                                          $"• Asegúrese de que la contraseña sea correcta\n" +
                                          $"• El archivo debe ser .p12 con clave privada incluida\n" +
                                          $"• Para {ambiente}, use el certificado correspondiente al ambiente";

                    MessageBox.Show(detallesError, $"Error en Certificado AFIP ({ambiente})",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error verificando certificado {ambiente}: {ex.Message}", Color.Red);
            }
        }

        // 7. AGREGAR métodos de formateo de CUIT:

        private void FormatearCUITAfipTesting()
        {
            FormatearCUITParaAmbiente(txtAfipTestingCuit);
        }

        private void FormatearCUITAfipProduccion()
        {
            FormatearCUITParaAmbiente(txtAfipProduccionCuit);
        }

        private void FormatearCUITParaAmbiente(TextBox textBox)
        {
            if (textBox.Text.Length == 0) return;

            int cursorPosition = textBox.SelectionStart;
            string soloNumeros = new string(textBox.Text.Where(char.IsDigit).ToArray());

            if (soloNumeros.Length > 11)
            {
                soloNumeros = soloNumeros.Substring(0, 11);
            }

            string cuitFormateado = soloNumeros;

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

            if (textBox.Text != cuitFormateado)
            {
                textBox.TextChanged -= soloNumeros.Length > 0 ?
                    (EventHandler)((s, e) => FormatearCUITAfipTesting()) :
                    (EventHandler)((s, e) => FormatearCUITAfipProduccion());

                textBox.Text = cuitFormateado;

                int nuevaPosicion = Math.Min(cursorPosition, cuitFormateado.Length);
                if (cursorPosition >= 2 && soloNumeros.Length > 2)
                    nuevaPosicion = Math.Min(cursorPosition + 1, cuitFormateado.Length);
                if (cursorPosition >= 10 && soloNumeros.Length > 10)
                    nuevaPosicion = Math.Min(nuevaPosicion + 1, cuitFormateado.Length);

                textBox.SelectionStart = nuevaPosicion;

                textBox.TextChanged += soloNumeros.Length > 0 ?
                    (EventHandler)((s, e) => FormatearCUITAfipTesting()) :
                    (EventHandler)((s, e) => FormatearCUITAfipProduccion());
            }

            if (soloNumeros.Length == 11)
            {
                if (ValidarCUITCompleto(textBox.Text))
                {
                    textBox.BackColor = Color.FromArgb(232, 245, 233);
                }
                else
                {
                    textBox.BackColor = Color.FromArgb(255, 235, 238);
                }
            }
            else
            {
                textBox.BackColor = Color.White;
            }
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
            chkPermitirFacturaC.TabIndex = 11; // ✅ NUEVO

            // TabIndex para controles AFIP (desplazados)
            //txtAfipCuit.TabIndex = 11;
            //txtAfipCertificadoPath.TabIndex = 12;
            //txtAfipCertificadoPassword.TabIndex = 13;
            //txtAfipWSAAUrl.TabIndex = 14;
            //txtAfipWSFEUrl.TabIndex = 15;
            //btnSeleccionarCertificado.TabIndex = 16;
            //btnVerificarCertificado.TabIndex = 17;

            // NUEVO: TabIndex para restricciones de impresión
            chkRestringirRemitoPorPago.TabIndex = 18;
            chkVistaPreviaImpresionDirecta.TabIndex = 19; // NUEVO
            chkLimitarFacturacion.TabIndex = 20; // NUEVO

            // ✅ NUEVO: TabIndex para controles de descuentos
            lstOpcionesDescuento.TabIndex = 21;
            txtNuevaOpcionDescuento.TabIndex = 22;
            btnAgregarOpcionDescuento.TabIndex = 23;
            btnEditarOpcionDescuento.TabIndex = 24;
            btnEliminarOpcionDescuento.TabIndex = 25;
            txtPorcentajeMaximo.TabIndex = 26;
            chkRestringirPorMetodoPago.TabIndex = 27;
            chkListMetodosPago.TabIndex = 28;

            // Ajustar todos los siguientes TabIndex +1:
            chkVerificarStock.TabIndex = 21; // Era 19
            lstNombresCtaCte.TabIndex = 22; // Era 20
            txtNuevoNombreCtaCte.TabIndex = 23; // Era 21
            btnAgregarNombre.TabIndex = 24; // Era 22
            btnEditarNombre.TabIndex = 25; // Era 23
            btnEliminarNombre.TabIndex = 26; // Era 24
            //txtConnectionString.TabIndex = 27; // Era 25
            //btnTestearConexion.TabIndex = 28; // Era 26
            txtConnectionStringTesting.TabIndex = 27;
            txtConnectionStringProduccion.TabIndex = 28;
            btnTestearConexionTesting.TabIndex = 29;
            btnTestearConexionProduccion.TabIndex = 30;
            btnEditarBaseDatos.TabIndex = 31; // Era 29
            btnGuardar.TabIndex = 32; // Era 30
            btnCancelar.TabIndex = 33; // Era 31
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

            //btnTestearConexion.Click += async (s, e) => await TestearConexion();
            btnColapsarBaseDatos.Click += (s, e) => ToggleColapsarBaseDatos();
            btnEditarBaseDatos.Click += (s, e) => ToggleEdicionBaseDatos();
            btnColapsarComercio.Click += (s, e) => ToggleColapsarComercio();
            btnColapsarFacturacion.Click += (s, e) => ToggleColapsarFacturacion();
            btnColapsarInventario.Click += (s, e) => ToggleColapsarInventario();
            btnColapsarCuentasCorrientes.Click += (s, e) => ToggleColapsarCuentasCorrientes();
            
            //// Eventos para AFIP
            //btnColapsarAfip.Click += (s, e) => ToggleColapsarAfip();
            //btnSeleccionarCertificado.Click += (s, e) => SeleccionarCertificado();
            //btnVerificarCertificado.Click += (s, e) => VerificarCertificadoAfip();

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

            // NUEVO: Evento para Descuentos
            btnColapsarDescuentos.Click += (s, e) => ToggleColapsarDescuentos();

            // Permitir agregar opción con Enter
            txtNuevaOpcionDescuento.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    AgregarOpcionDescuento();
                }
            };
            // Formatear CUITs mientras el usuario escribe
            txtCUIT.TextChanged += (s, e) => FormatearCUIT();
            //txtAfipCuit.TextChanged += (s, e) => FormatearCUITAfip();

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
                bool permitirC = _configuracionOriginal["Facturacion"]?["PermitirFacturaC"]?.ToObject<bool>() ?? true; // ✅ NUEVO

                if (chkPermitirFacturaA != null) chkPermitirFacturaA.Checked = permitirA;
                if (chkPermitirFacturaB != null) chkPermitirFacturaB.Checked = permitirB;
                if (chkPermitirFacturaC != null) chkPermitirFacturaC.Checked = permitirC; // ✅ NUEVO

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
                //txtAfipCuit.Text = _configuracionOriginal["AFIP"]?["CUIT"]?.ToString() ?? "";
                //txtAfipCertificadoPath.Text = _configuracionOriginal["AFIP"]?["CertificadoPath"]?.ToString() ?? "";
                //txtAfipCertificadoPassword.Text = _configuracionOriginal["AFIP"]?["CertificadoPassword"]?.ToString() ?? "";
                //txtAfipWSAAUrl.Text = _configuracionOriginal["AFIP"]?["WSAAUrl"]?.ToString() ?? "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";
                //txtAfipWSFEUrl.Text = _configuracionOriginal["AFIP"]?["WSFEUrl"]?.ToString() ?? "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";

                // NUEVO: Cargar configuración de restricciones de impresión
                bool restringirRemitoPorPago = _configuracionOriginal["RestriccionesImpresion"]?["RestringirRemitoPorPago"]?.ToObject<bool>() ?? true;
                chkRestringirRemitoPorPago.Checked = restringirRemitoPorPago;

                // NUEVO: Cargar configuración de vista previa de impresión
                bool usarVistaPrevia = _configuracionOriginal["RestriccionesImpresion"]?["UsarVistaPrevia"]?.ToObject<bool>() ?? true;
                chkVistaPreviaImpresionDirecta.Checked = usarVistaPrevia;

                // NUEVO: Cargar configuración de límite de facturación
                bool limitarFacturacion = _configuracionOriginal["RestriccionesImpresion"]?["LimitarFacturacion"]?.ToObject<bool>() ?? false;
                chkLimitarFacturacion.Checked = limitarFacturacion;

                decimal montoLimite = _configuracionOriginal["RestriccionesImpresion"]?["MontoLimiteFacturacion"]?.ToObject<decimal>() ?? 0m;
                if (montoLimite > 0)
                {
                    txtMontoLimiteFacturacion.Text = montoLimite.ToString("F2");
                }

                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - Usar Vista Previa: {chkVistaPreviaImpresionDirecta.Checked}");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - Limitar Facturación: {chkLimitarFacturacion.Checked}, Monto: ${montoLimite:F2}");

                // NUEVO: Cargar configuración de descuentos
                var descuentos = _configuracionOriginal["Descuentos"];
                if (descuentos != null)
                {
                    // Cargar opciones disponibles
                    lstOpcionesDescuento.Items.Clear();
                    var opciones = descuentos["OpcionesDisponibles"] as JArray;
                    if (opciones != null)
                    {
                        foreach (var opcion in opciones.OrderBy(x => (int)x))
                        {
                            lstOpcionesDescuento.Items.Add((int)opcion);
                        }
                    }

                    // Cargar porcentaje máximo
                    txtPorcentajeMaximo.Text = descuentos["PorcentajeMaximo"]?.ToString() ?? "20";

                    // Cargar restricción por método de pago
                    chkRestringirPorMetodoPago.Checked =
                        descuentos["RestringirPorMetodoPago"]?.ToObject<bool>() ?? true;

                    // Cargar métodos de pago permitidos
                    var metodosPagos = descuentos["MetodosPagoPermitidos"] as JArray;
                    if (metodosPagos != null)
                    {
                        for (int i = 0; i < chkListMetodosPago.Items.Count; i++)
                        {
                            string metodo = chkListMetodosPago.Items[i].ToString();
                            chkListMetodosPago.SetItemChecked(i,
                                metodosPagos.Any(m => m.ToString() == metodo));
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - Opciones Descuento: {lstOpcionesDescuento.Items.Count}");
                    System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - % Máximo: {txtPorcentajeMaximo.Text}");
                }

                // Cargar configuración de inventario
                bool verificarStock = _configuracionOriginal["Inventario"]?["VerificarStock"]?.ToObject<bool>() ?? 
                                     _configuracionOriginal["Validaciones"]?["ValidarStockDisponible"]?.ToObject<bool>() ?? 
                                     true;
                chkVerificarStock.Checked = verificarStock;

                // Cargar nombres de cuenta corriente
                CargarNombresCuentasCorrientes();

                // Cargar cadena de conexión
                string ambienteBDSeleccionado = _configuracionOriginal["BaseDatos"]?["AmbienteActivo"]?.ToString() ?? "Testing";
                if (ambienteBDSeleccionado == "Produccion")
                {
                    rbDbProduccion.Checked = true;
                }
                else
                {
                    rbDbTesting.Checked = true;
                }

                // Cargar cadena de conexión Testing
                txtConnectionStringTesting.Text = _configuracionOriginal["ConnectionStrings"]?["Testing"]?.ToString()
                    ?? "Server=localhost;Database=comercio_dev;Trusted_Connection=True;TrustServerCertificate=True;";

                // Cargar cadena de conexión Producción
                txtConnectionStringProduccion.Text = _configuracionOriginal["ConnectionStrings"]?["Produccion"]?.ToString()
                    ?? "Server=localhost;Database=comercio;Trusted_Connection=True;TrustServerCertificate=True;";

                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - BD Ambiente: {ambienteBDSeleccionado}");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - BD Testing: {txtConnectionStringTesting.Text}");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - BD Producción: {txtConnectionStringProduccion.Text}");

                // Cargar ambiente seleccionado
                string ambienteSeleccionado = _configuracionOriginal["AFIP"]?["AmbienteActivo"]?.ToString() ?? "Testing";
                if (ambienteSeleccionado == "Produccion")
                {
                    rbAfipProduccion.Checked = true;
                }
                else
                {
                    rbAfipTesting.Checked = true;
                }

                // Cargar configuración de Testing
                var afipTesting = _configuracionOriginal["AFIP"]?["Testing"];
                if (afipTesting != null)
                {
                    txtAfipTestingCuit.Text = afipTesting["CUIT"]?.ToString() ?? "";
                    txtAfipTestingPuntoVenta.Text = afipTesting["PuntoVenta"]?.ToString() ?? "1"; // ✅ NUEVO
                    txtAfipTestingCertificadoPath.Text = afipTesting["CertificadoPath"]?.ToString() ?? "";
                    txtAfipTestingCertificadoPassword.Text = afipTesting["CertificadoPassword"]?.ToString() ?? "";
                    txtAfipTestingWSAAUrl.Text = afipTesting["WSAAUrl"]?.ToString() ?? "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";
                    txtAfipTestingWSFEUrl.Text = afipTesting["WSFEUrl"]?.ToString() ?? "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";
                }

                // Cargar configuración de Producción
                var afipProduccion = _configuracionOriginal["AFIP"]?["Produccion"];
                if (afipProduccion != null)
                {
                    txtAfipProduccionCuit.Text = afipProduccion["CUIT"]?.ToString() ?? "";
                    txtAfipProduccionPuntoVenta.Text = afipProduccion["PuntoVenta"]?.ToString() ?? "1"; // ✅ NUEVO
                    txtAfipProduccionCertificadoPath.Text = afipProduccion["CertificadoPath"]?.ToString() ?? "";
                    txtAfipProduccionCertificadoPassword.Text = afipProduccion["CertificadoPassword"]?.ToString() ?? "";
                    txtAfipProduccionWSAAUrl.Text = afipProduccion["WSAAUrl"]?.ToString() ?? "https://wsaa.afip.gov.ar/ws/services/LoginCms";
                    txtAfipProduccionWSFEUrl.Text = afipProduccion["WSFEUrl"]?.ToString() ?? "https://servicios1.afip.gov.ar/wsfev1/service.asmx";
                }

                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - Nombre: '{txtNombreComercio.Text}'");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - CUIT: '{txtCUIT.Text}'");
                //System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - AFIP CUIT: '{txtAfipCuit.Text}'");
                //System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - Certificado: '{txtAfipCertificadoPath.Text}'");
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
                        ["Testing"] = "Server=localhost;Database=comercio_dev;Trusted_Connection=True;TrustServerCertificate=True;",
                        ["Produccion"] = "Server=localhost;Database=comercio;Trusted_Connection=True;TrustServerCertificate=True;",
                        ["DefaultConnection"] = "Server=localhost;Database=comercio_dev;Trusted_Connection=True;TrustServerCertificate=True;"
                    },
                    ["BaseDatos"] = new JObject
                    {
                        ["AmbienteActivo"] = "Testing"
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
                        ["PermitirFacturaB"] = true,
                        ["PermitirFacturaC"] = true  // ✅ NUEVO
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
                        ["AmbienteActivo"] = "Testing", // Por defecto usar Testing
                        ["Testing"] = new JObject
                        {
                            ["CUIT"] = "",
                            ["CertificadoPath"] = "",
                            ["CertificadoPassword"] = "",
                            ["WSAAUrl"] = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",
                            ["WSFEUrl"] = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx"
                        },
                        ["Produccion"] = new JObject
                        {
                            ["CUIT"] = "",
                            ["CertificadoPath"] = "",
                            ["CertificadoPassword"] = "",
                            ["WSAAUrl"] = "https://wsaa.afip.gov.ar/ws/services/LoginCms",
                            ["WSFEUrl"] = "https://servicios1.afip.gov.ar/wsfev1/service.asmx"
                        }
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

                System.Diagnostics.Debug.WriteLine($"[SAVE] === RUTAS DE GUARDADO ===");

                // ✅ RUTA 1: Runtime (bin\Debug\...)
                string rutaRuntime = _rutaAppsettings;
                System.Diagnostics.Debug.WriteLine($"[SAVE] Runtime: {rutaRuntime}");

                // ✅ RUTA 2: Proyecto (raíz)
                string rutaProyecto = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    @"..\..\..\appsettings.json"
                );
                rutaProyecto = Path.GetFullPath(rutaProyecto);
                System.Diagnostics.Debug.WriteLine($"[SAVE] Proyecto: {rutaProyecto}");
                System.Diagnostics.Debug.WriteLine($"[SAVE] Proyecto existe: {File.Exists(rutaProyecto)}");
                System.Diagnostics.Debug.WriteLine($"[SAVE] ================================");

                // Verificar que tenemos la configuración original
                if (_configuracionOriginal == null)
                {
                    System.Diagnostics.Debug.WriteLine("[SAVE ERROR] _configuracionOriginal es null");
                    MostrarMensaje("❌ Error: Configuración original no cargada", Color.Red);
                    return;
                }

                var nuevaConfiguracion = JObject.Parse(_configuracionOriginal.ToString());

                // === ACTUALIZAR TODA LA CONFIGURACIÓN ===

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
                nuevaConfiguracion["Facturacion"]["PermitirFacturaA"] = chkPermitirFacturaA?.Checked ?? true;
                nuevaConfiguracion["Facturacion"]["PermitirFacturaB"] = chkPermitirFacturaB?.Checked ?? true;
                nuevaConfiguracion["Facturacion"]["PermitirFacturaC"] = chkPermitirFacturaC?.Checked ?? true; // ✅ NUEVO

                // Actualizar configuración de AFIP
                if (nuevaConfiguracion["AFIP"] == null)
                    nuevaConfiguracion["AFIP"] = new JObject();

                nuevaConfiguracion["AFIP"]["AmbienteActivo"] = rbAfipProduccion.Checked ? "Produccion" : "Testing";

                // Testing
                if (nuevaConfiguracion["AFIP"]["Testing"] == null)
                    nuevaConfiguracion["AFIP"]["Testing"] = new JObject();

                nuevaConfiguracion["AFIP"]["Testing"]["CUIT"] = txtAfipTestingCuit.Text.Trim();
                nuevaConfiguracion["AFIP"]["Testing"]["PuntoVenta"] = int.TryParse(txtAfipTestingPuntoVenta.Text.Trim(), out int pvTesting) ? pvTesting : 1; // ✅ NUEVO
                nuevaConfiguracion["AFIP"]["Testing"]["CertificadoPath"] = txtAfipTestingCertificadoPath.Text.Trim();
                nuevaConfiguracion["AFIP"]["Testing"]["CertificadoPassword"] = txtAfipTestingCertificadoPassword.Text;
                nuevaConfiguracion["AFIP"]["Testing"]["WSAAUrl"] = txtAfipTestingWSAAUrl.Text.Trim();
                nuevaConfiguracion["AFIP"]["Testing"]["WSFEUrl"] = txtAfipTestingWSFEUrl.Text.Trim();
                nuevaConfiguracion["AFIP"]["Testing"]["CondicionIVA"] = cmbCondicion.SelectedItem?.ToString() ?? "Responsable Inscripto";

                // Producción
                if (nuevaConfiguracion["AFIP"]["Produccion"] == null)
                    nuevaConfiguracion["AFIP"]["Produccion"] = new JObject();

                nuevaConfiguracion["AFIP"]["Produccion"]["CUIT"] = txtAfipProduccionCuit.Text.Trim();
                nuevaConfiguracion["AFIP"]["Produccion"]["PuntoVenta"] = int.TryParse(txtAfipProduccionPuntoVenta.Text.Trim(), out int pvProduccion) ? pvProduccion : 1; // ✅ NUEVO
                nuevaConfiguracion["AFIP"]["Produccion"]["CertificadoPath"] = txtAfipProduccionCertificadoPath.Text.Trim();
                nuevaConfiguracion["AFIP"]["Produccion"]["CertificadoPassword"] = txtAfipProduccionCertificadoPassword.Text;
                nuevaConfiguracion["AFIP"]["Produccion"]["WSAAUrl"] = txtAfipProduccionWSAAUrl.Text.Trim();
                nuevaConfiguracion["AFIP"]["Produccion"]["WSFEUrl"] = txtAfipProduccionWSFEUrl.Text.Trim();
                nuevaConfiguracion["AFIP"]["Produccion"]["CondicionIVA"] = cmbCondicion.SelectedItem?.ToString() ?? "Responsable Inscripto";

                System.Diagnostics.Debug.WriteLine($"[SAVE] AFIP - Ambiente: {(rbAfipProduccion.Checked ? "Producción" : "Testing")}");

                // Restricciones de impresión
                if (nuevaConfiguracion["RestriccionesImpresion"] == null)
                    nuevaConfiguracion["RestriccionesImpresion"] = new JObject();

                nuevaConfiguracion["RestriccionesImpresion"]["RestringirRemitoPorPago"] = chkRestringirRemitoPorPago.Checked;
                nuevaConfiguracion["RestriccionesImpresion"]["UsarVistaPrevia"] = chkVistaPreviaImpresionDirecta.Checked;
                nuevaConfiguracion["RestriccionesImpresion"]["LimitarFacturacion"] = chkLimitarFacturacion.Checked;

                decimal montoLimite = 0m;
                if (!string.IsNullOrWhiteSpace(txtMontoLimiteFacturacion.Text))
                {
                    string montoTexto = txtMontoLimiteFacturacion.Text.Trim().Replace(",", ".");
                    if (decimal.TryParse(montoTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto))
                    {
                        montoLimite = Math.Round(monto, 2);
                    }
                }
                nuevaConfiguracion["RestriccionesImpresion"]["MontoLimiteFacturacion"] = montoLimite;

                // NUEVO: Actualizar configuración de descuentos
                if (nuevaConfiguracion["Descuentos"] == null)
                    nuevaConfiguracion["Descuentos"] = new JObject();

                // Opciones disponibles
                var opcionesArray = new JArray();
                foreach (int opcion in lstOpcionesDescuento.Items)
                {
                    opcionesArray.Add(opcion);
                }
                nuevaConfiguracion["Descuentos"]["OpcionesDisponibles"] = opcionesArray;

                // Porcentaje máximo
                if (int.TryParse(txtPorcentajeMaximo.Text, out int porcentajeMax))
                {
                    nuevaConfiguracion["Descuentos"]["PorcentajeMaximo"] = porcentajeMax;
                }
                else
                {
                    nuevaConfiguracion["Descuentos"]["PorcentajeMaximo"] = 20;
                }

                // Restricción por método de pago
                nuevaConfiguracion["Descuentos"]["RestringirPorMetodoPago"] = chkRestringirPorMetodoPago.Checked;

                // Métodos de pago permitidos
                var metodosPermitidos = new JArray();
                for (int i = 0; i < chkListMetodosPago.Items.Count; i++)
                {
                    if (chkListMetodosPago.GetItemChecked(i))
                    {
                        metodosPermitidos.Add(chkListMetodosPago.Items[i].ToString());
                    }
                }
                nuevaConfiguracion["Descuentos"]["MetodosPagoPermitidos"] = metodosPermitidos;

                System.Diagnostics.Debug.WriteLine($"[SAVE] Descuentos - Opciones: {opcionesArray.Count}");
                System.Diagnostics.Debug.WriteLine($"[SAVE] Descuentos - % Máximo: {porcentajeMax}");
                System.Diagnostics.Debug.WriteLine($"[SAVE] Descuentos - Métodos permitidos: {metodosPermitidos.Count}");


                // Inventario
                if (nuevaConfiguracion["Validaciones"] == null)
                    nuevaConfiguracion["Validaciones"] = new JObject();

                nuevaConfiguracion["Validaciones"]["ValidarStockDisponible"] = chkVerificarStock.Checked;

                // Cuentas corrientes
                GuardarNombresCuentasCorrientes(nuevaConfiguracion);

                // Connection string (si está habilitada la edición)
                if (_edicionBaseDatosHabilitada)
                {
                    if (nuevaConfiguracion["BaseDatos"] == null)
                        nuevaConfiguracion["BaseDatos"] = new JObject();

                    nuevaConfiguracion["BaseDatos"]["AmbienteActivo"] = rbDbProduccion.Checked ? "Produccion" : "Testing";

                    // Connection strings (si está habilitada la edición)
                    if (_edicionBaseDatosHabilitada)
                    {
                        if (nuevaConfiguracion["ConnectionStrings"] == null)
                            nuevaConfiguracion["ConnectionStrings"] = new JObject();

                        nuevaConfiguracion["ConnectionStrings"]["Testing"] = txtConnectionStringTesting.Text.Trim();
                        nuevaConfiguracion["ConnectionStrings"]["Produccion"] = txtConnectionStringProduccion.Text.Trim();

                        // ✅ TAMBIÉN GUARDAR DefaultConnection como la activa
                        string connectionActiva = rbDbProduccion.Checked
                            ? txtConnectionStringProduccion.Text.Trim()
                            : txtConnectionStringTesting.Text.Trim();
                        nuevaConfiguracion["ConnectionStrings"]["DefaultConnection"] = connectionActiva;
                    }

                    System.Diagnostics.Debug.WriteLine($"[SAVE] BD - Ambiente: {(rbDbProduccion.Checked ? "Producción" : "Testing")}");
                }
                    
                    // ✅ CREAR BACKUP DEL ARCHIVO
                    // RUNTIME
                    string backupPath = rutaRuntime + $".backup.{DateTime.Now:yyyyMMdd_HHmmss}";
                if (File.Exists(rutaRuntime))
                {
                    File.Copy(rutaRuntime, backupPath);
                    System.Diagnostics.Debug.WriteLine($"[SAVE] Backup creado: {backupPath}");
                }

                // ✅ FORMATEAR JSON UNA SOLA VEZ
                string jsonFormateado = JsonConvert.SerializeObject(nuevaConfiguracion, Formatting.Indented);
                System.Diagnostics.Debug.WriteLine($"[SAVE] JSON generado ({jsonFormateado.Length} caracteres)");

                // ✅ === GUARDADO DUAL COMIENZA AQUÍ ===

                bool guardadoRuntimeExitoso = false;
                bool guardadoProyectoExitoso = false;
                string errorRuntime = "";
                string errorProyecto = "";

                // ✅ GUARDAR EN RUNTIME
                try
                {
                    await File.WriteAllTextAsync(rutaRuntime, jsonFormateado);

                    if (File.Exists(rutaRuntime))
                    {
                        string verificacion = await File.ReadAllTextAsync(rutaRuntime);
                        guardadoRuntimeExitoso = verificacion.Length == jsonFormateado.Length;
                        System.Diagnostics.Debug.WriteLine($"[SAVE] ✅ Runtime guardado - {verificacion.Length} bytes");
                    }
                }
                catch (Exception ex)
                {
                    errorRuntime = ex.Message;
                    System.Diagnostics.Debug.WriteLine($"[SAVE] ❌ Error en runtime: {ex.Message}");
                }

                // ✅ GUARDAR EN PROYECTO (solo si existe)
                if (File.Exists(rutaProyecto))
                {
                    try
                    {
                        // Crear backup del proyecto también
                        string backupProyecto = rutaProyecto + $".backup.{DateTime.Now:yyyyMMdd_HHmmss}";
                        File.Copy(rutaProyecto, backupProyecto);
                        System.Diagnostics.Debug.WriteLine($"[SAVE] Backup proyecto creado: {backupProyecto}");

                        await File.WriteAllTextAsync(rutaProyecto, jsonFormateado);

                        if (File.Exists(rutaProyecto))
                        {
                            string verificacion = await File.ReadAllTextAsync(rutaProyecto);
                            guardadoProyectoExitoso = verificacion.Length == jsonFormateado.Length;
                            System.Diagnostics.Debug.WriteLine($"[SAVE] ✅ Proyecto guardado - {verificacion.Length} bytes");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorProyecto = ex.Message;
                        System.Diagnostics.Debug.WriteLine($"[SAVE] ⚠️ Error en proyecto: {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[SAVE] ℹ️ Archivo de proyecto no encontrado, solo se guardó en runtime");
                }

                // ✅ === EVALUAR RESULTADOS DEL GUARDADO ===

                if (guardadoRuntimeExitoso)
                {
                    // Actualizar configuración original
                    _configuracionOriginal = nuevaConfiguracion;

                    string mensajeExito = "✅ CONFIGURACIÓN GUARDADA EXITOSAMENTE\n\n";

                    if (guardadoProyectoExitoso)
                    {
                        mensajeExito += "📁 Guardado en:\n" +
                                       "  ✓ Ejecutable (runtime)\n" +
                                       "  ✓ Proyecto (código fuente)\n\n" +
                                       "Los cambios se aplicarán inmediatamente y\n" +
                                       "persisti rán en futuras compilaciones.";
                    }
                    else
                    {
                        mensajeExito += "📁 Guardado en:\n" +
                                       "  ✓ Ejecutable (runtime)\n" +
                                       $"  ⚠️ Proyecto: {(File.Exists(rutaProyecto) ? "Error al guardar" : "No encontrado")}\n\n";

                        if (!string.IsNullOrEmpty(errorProyecto))
                        {
                            mensajeExito += $"⚠️ Advertencia: {errorProyecto}\n\n";
                        }

                        mensajeExito += "Los cambios se aplicarán pero podrían perderse\n" +
                                       "al recompilar si no se guardaron en el proyecto.";
                    }

                    MostrarMensaje("✅ Configuración guardada", Color.Green);

                    var result = MessageBox.Show(
                        mensajeExito + "\n\n¿Desea aplicar los cambios ahora en las ventanas abiertas?",
                        "Configuración Guardada",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            Comercio.NET.Servicios.SettingsManager.ReloadConfiguration();
                            MostrarMensaje("✅ Cambios aplicados en caliente", Color.Green);
                            System.Diagnostics.Debug.WriteLine("[SAVE] SettingsManager.ReloadConfiguration invocado");
                        }
                        catch (Exception exNotify)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SAVE] Error notificando: {exNotify.Message}");
                            MostrarMensaje("⚠️ No se pudieron notificar todos los formularios", Color.Orange);
                        }

                        await Task.Delay(800);
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
                else
                {
                    // Error crítico - no se pudo guardar en runtime
                    string mensajeError = "❌ ERROR AL GUARDAR CONFIGURACIÓN\n\n" +
                                         "No se pudo guardar el archivo en la ubicación de ejecución.\n\n" +
                                         $"Error: {errorRuntime}\n\n" +
                                         "Los cambios NO se aplicaron.";

                    MostrarMensaje("❌ Error al guardar", Color.Red);
                    MessageBox.Show(mensajeError, "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                System.Diagnostics.Debug.WriteLine("[SAVE] === RESUMEN GUARDADO ===");
                System.Diagnostics.Debug.WriteLine($"[SAVE] Runtime: {(guardadoRuntimeExitoso ? "✅ OK" : "❌ FALLÓ")}");
                System.Diagnostics.Debug.WriteLine($"[SAVE] Proyecto: {(guardadoProyectoExitoso ? "✅ OK" : File.Exists(rutaProyecto) ? "⚠️ ERROR" : "ℹ️ N/A")}");
                System.Diagnostics.Debug.WriteLine("[SAVE] ===========================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SAVE ERROR] Excepción general: {ex}");
                MostrarMensaje($"❌ Error: {ex.Message}", Color.Red);

                MessageBox.Show(
                    $"❌ ERROR INESPERADO\n\n{ex.Message}\n\n" +
                    $"Tipo: {ex.GetType().Name}\n" +
                    $"StackTrace:\n{ex.StackTrace}",
                    "Error Crítico",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

        //private async Task TestearConexion()
        //{
        //    if (string.IsNullOrWhiteSpace(txtConnectionString.Text))
        //    {
        //        MostrarMensaje("❌ La cadena de conexión no puede estar vacía", Color.Red);
        //        return;
        //    }

        //    try
        //    {
        //        btnTestearConexion.Enabled = false;
        //        btnTestearConexion.Text = "🔄\n...";
        //        MostrarMensaje("Probando conexión...", Color.Blue);

        //        using var connection = new SqlConnection(txtConnectionString.Text);
        //        await connection.OpenAsync();
                
        //        using var cmd = new SqlCommand("SELECT DB_NAME() AS DatabaseName", connection);
        //        var dbName = await cmd.ExecuteScalarAsync();

        //        MostrarMensaje($"✅ Conexión exitosa a '{dbName}'", Color.Green);
        //    }
        //    catch (Exception ex)
        //    {
        //        MostrarMensaje($"❌ Error de conexión: {ex.Message}", Color.Red);
        //    }
        //    finally
        //    {
        //        btnTestearConexion.Enabled = true;
        //        btnTestearConexion.Text = "🔧\nTest";
        //    }
        //}

        private async Task TestearConexionTesting()
        {
            await TestearConexionParaAmbiente(txtConnectionStringTesting.Text, "Testing");
        }

        private async Task TestearConexionProduccion()
        {
            await TestearConexionParaAmbiente(txtConnectionStringProduccion.Text, "Producción");
        }

        private async Task TestearConexionParaAmbiente(string connectionString, string ambiente)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                MostrarMensaje($"❌ La cadena de conexión {ambiente} no puede estar vacía", Color.Red);
                return;
            }

            var btnActivo = ambiente == "Testing" ? btnTestearConexionTesting : btnTestearConexionProduccion;

            try
            {
                btnActivo.Enabled = false;
                btnActivo.Text = "🔄\n...";
                MostrarMensaje($"Probando conexión {ambiente}...", Color.Blue);

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                using var cmd = new SqlCommand("SELECT DB_NAME() AS DatabaseName", connection);
                var dbName = await cmd.ExecuteScalarAsync();

                MostrarMensaje($"✅ Conexión {ambiente} exitosa a '{dbName}'", Color.Green);
                
                MessageBox.Show(
                    $"CONEXIÓN EXITOSA ({ambiente})\n\n" +
                    $"Base de datos: {dbName}\n" +
                    $"Servidor: {connection.DataSource}\n\n" +
                    $"✅ La conexión está funcionando correctamente.",
                    $"Test de Conexión - {ambiente}",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error en conexión {ambiente}: {ex.Message}", Color.Red);
                
                MessageBox.Show(
                    $"ERROR DE CONEXIÓN ({ambiente})\n\n" +
                    $"Mensaje: {ex.Message}\n\n" +
                    $"Tipo: {ex.GetType().Name}\n\n" +
                    $"Verifique:\n" +
                    $"• El servidor esté accesible\n" +
                    $"• Las credenciales sean correctas\n" +
                    $"• La base de datos exista\n" +
                    $"• El firewall permita la conexión",
                    $"Error de Conexión - {ambiente}",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnActivo.Enabled = true;
                btnActivo.Text = "🔧\nTest";
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

            // Validaciones para AFIP - Solo validar el ambiente activo
            bool esProduccion = rbAfipProduccion.Checked;
            string ambienteNombre = esProduccion ? "Producción" : "Testing";
            var txtCuitActivo = esProduccion ? txtAfipProduccionCuit : txtAfipTestingCuit;
            var txtCertPathActivo = esProduccion ? txtAfipProduccionCertificadoPath : txtAfipTestingCertificadoPath;

            if (string.IsNullOrWhiteSpace(txtCuitActivo.Text))
            {
                MostrarMensaje($"❌ El CUIT AFIP para {ambienteNombre} es requerido", Color.Red);
                _afipColapsado = false;
                ActualizarEstadoSeccion(panelAfip, "panelContenidoAfip", _afipColapsado, btnColapsarAfip, 35, 465);
                ActualizarPosicionesTodasLasSecciones();
                txtCuitActivo.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtCertPathActivo.Text))
            {
                MostrarMensaje($"❌ La ruta del certificado para {ambienteNombre} es requerida", Color.Red);
                _afipColapsado = false;
                ActualizarEstadoSeccion(panelAfip, "panelContenidoAfip", _afipColapsado, btnColapsarAfip, 35, 465);
                ActualizarPosicionesTodasLasSecciones();
                txtCertPathActivo.Focus();
                return false;
            }

            if (!File.Exists(txtCertPathActivo.Text))
            {
                MostrarMensaje($"❌ El archivo del certificado para {ambienteNombre} no existe", Color.Red);
                _afipColapsado = false;
                ActualizarEstadoSeccion(panelAfip, "panelContenidoAfip", _afipColapsado, btnColapsarAfip, 35, 465);
                ActualizarPosicionesTodasLasSecciones();
                txtCertPathActivo.Focus();
                txtCertPathActivo.SelectAll();
                return false;
            }

            // ✅ NUEVO: Validar punto de venta
            var txtPuntoVentaActivo = esProduccion ? txtAfipProduccionPuntoVenta : txtAfipTestingPuntoVenta;

            if (string.IsNullOrWhiteSpace(txtPuntoVentaActivo.Text))
            {
                MostrarMensaje($"❌ El punto de venta para {ambienteNombre} es requerido", Color.Red);
                _afipColapsado = false;
                ActualizarEstadoSeccion(panelAfip, "panelContenidoAfip", _afipColapsado, btnColapsarAfip, 35, 465);
                ActualizarPosicionesTodasLasSecciones();
                txtPuntoVentaActivo.Focus();
                return false;
            }

            if (!int.TryParse(txtPuntoVentaActivo.Text, out int puntoVenta) || puntoVenta < 1 || puntoVenta > 9999)
            {
                MostrarMensaje($"❌ El punto de venta debe ser un número entre 1 y 9999", Color.Red);
                _afipColapsado = false;
                ActualizarEstadoSeccion(panelAfip, "panelContenidoAfip", _afipColapsado, btnColapsarAfip, 35, 465);
                ActualizarPosicionesTodasLasSecciones();
                txtPuntoVentaActivo.Focus();
                txtPuntoVentaActivo.SelectAll();
                return false;
            }

            // ✅ NUEVO: Validaciones para descuentos
            if (lstOpcionesDescuento.Items.Count == 0)
            {
                MostrarMensaje("❌ Debe agregar al menos una opción de descuento", Color.Red);
                _descuentosColapsado = false;
                ActualizarEstadoSeccion(panelDescuentos, "panelContenidoDescuentos",
                    _descuentosColapsado, btnColapsarDescuentos, 35, 415);
                ActualizarPosicionesTodasLasSecciones();
                txtNuevaOpcionDescuento.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPorcentajeMaximo.Text))
            {
                MostrarMensaje("❌ El porcentaje máximo de descuento es requerido", Color.Red);
                _descuentosColapsado = false;
                ActualizarEstadoSeccion(panelDescuentos, "panelContenidoDescuentos",
                    _descuentosColapsado, btnColapsarDescuentos, 35, 415);
                ActualizarPosicionesTodasLasSecciones();
                txtPorcentajeMaximo.Focus();
                return false;
            }

            if (!int.TryParse(txtPorcentajeMaximo.Text, out int maxPorcentaje) ||
                maxPorcentaje <= 0 || maxPorcentaje > 100)
            {
                MostrarMensaje("❌ El porcentaje máximo debe estar entre 1 y 100", Color.Red);
                _descuentosColapsado = false;
                ActualizarEstadoSeccion(panelDescuentos, "panelContenidoDescuentos",
                    _descuentosColapsado, btnColapsarDescuentos, 35, 415);
                ActualizarPosicionesTodasLasSecciones();
                txtPorcentajeMaximo.Focus();
                txtPorcentajeMaximo.SelectAll();
                return false;
            }

            // Verificar que todas las opciones sean <= al máximo
            foreach (int opcion in lstOpcionesDescuento.Items)
            {
                if (opcion > maxPorcentaje)
                {
                    MostrarMensaje($"❌ La opción {opcion}% supera el porcentaje máximo permitido ({maxPorcentaje}%)", Color.Red);
                    _descuentosColapsado = false;
                    ActualizarEstadoSeccion(panelDescuentos, "panelContenidoDescuentos",
                        _descuentosColapsado, btnColapsarDescuentos, 35, 415);
                    ActualizarPosicionesTodasLasSecciones();
                    lstOpcionesDescuento.SelectedItem = opcion;
                    return false;
                }
            }

            // Verificar que si está restringido, hay al menos un método seleccionado
            if (chkRestringirPorMetodoPago.Checked)
            {
                bool hayMetodoSeleccionado = false;
                for (int i = 0; i < chkListMetodosPago.Items.Count; i++)
                {
                    if (chkListMetodosPago.GetItemChecked(i))
                    {
                        hayMetodoSeleccionado = true;
                        break;
                    }
                }

                if (!hayMetodoSeleccionado)
                {
                    MostrarMensaje("❌ Debe seleccionar al menos un método de pago permitido", Color.Red);
                    _descuentosColapsado = false;
                    ActualizarEstadoSeccion(panelDescuentos, "panelContenidoDescuentos",
                        _descuentosColapsado, btnColapsarDescuentos, 35, 415);
                    ActualizarPosicionesTodasLasSecciones();
                    chkListMetodosPago.Focus();
                    return false;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[VALIDACIÓN] Punto de venta {ambienteNombre}: {puntoVenta}");

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
            ActualizarEstadoSeccion(panelAfip, "panelContenidoAfip", _afipColapsado, btnColapsarAfip, 35, 465); // ACTUALIZADO: altura
            ActualizarPosicionesTodasLasSecciones();
        }

        // MODIFICAR: El método ToggleColapsarRestriccionesImpresion (línea ~2027)
        private void ToggleColapsarRestriccionesImpresion()
        {
            _restriccionesImpresionColapsado = !_restriccionesImpresionColapsado;
            ActualizarEstadoSeccion(panelRestriccionesImpresion, "panelContenidoRestriccionesImpresion",
                _restriccionesImpresionColapsado, btnColapsarRestriccionesImpresion, 35, 330); // ACTUALIZADO: altura de 235 a 330
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
                panelBaseDatos.Size = new Size(panelBaseDatos.Width, 405); // ✅ AUMENTADO de 365 a 405
                btnColapsarBaseDatos.Text = "▼";
            }

            // ✅ CORREGIDO: Habilitar/deshabilitar según estado de edición
            if (txtConnectionStringTesting != null)
            {
                txtConnectionStringTesting.ReadOnly = !_edicionBaseDatosHabilitada;
                txtConnectionStringTesting.BackColor = _edicionBaseDatosHabilitada
                    ? Color.White
                    : Color.FromArgb(245, 245, 245);
                txtConnectionStringTesting.ForeColor = _edicionBaseDatosHabilitada
                    ? Color.Black
                    : Color.Gray;
            }

            if (txtConnectionStringProduccion != null)
            {
                txtConnectionStringProduccion.ReadOnly = !_edicionBaseDatosHabilitada;
                txtConnectionStringProduccion.BackColor = _edicionBaseDatosHabilitada
                    ? Color.White
                    : Color.FromArgb(245, 245, 245);
                txtConnectionStringProduccion.ForeColor = _edicionBaseDatosHabilitada
                    ? Color.Black
                    : Color.Gray;
            }

            if (btnTestearConexionTesting != null)
                btnTestearConexionTesting.Enabled = _edicionBaseDatosHabilitada;

            if (btnTestearConexionProduccion != null)
                btnTestearConexionProduccion.Enabled = _edicionBaseDatosHabilitada;

            // Actualizar botón de edición
            if (btnEditarBaseDatos != null)
            {
                if (_edicionBaseDatosHabilitada)
                {
                    btnEditarBaseDatos.Text = "🔓";
                    btnEditarBaseDatos.BackColor = Color.FromArgb(76, 175, 80);
                }
                else
                {
                    btnEditarBaseDatos.Text = "🔒";
                    btnEditarBaseDatos.BackColor = Color.FromArgb(255, 152, 0);
                }
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

            // ✅ NUEVA: Sección Descuentos
            panelDescuentos.Location = new Point(10, currentY);
            currentY += panelDescuentos.Height + spacing;

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
        /// <returns>Dígito verificador calculado</returns>
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
        //private void FormatearCUITAfip()
        //{
        //    // Evitar recursión infinita
        //    if (txtAfipCuit.Text.Length == 0) return;

        //    // Guardar posición del cursor
        //    int cursorPosition = txtAfipCuit.SelectionStart;

        //    // Remover todos los guiones existentes y mantener solo números
        //    string soloNumeros = new string(txtAfipCuit.Text.Where(char.IsDigit).ToArray());

        //    // Limitar a máximo 11 dígitos
        //    if (soloNumeros.Length > 11)
        //    {
        //        soloNumeros = soloNumeros.Substring(0, 11);
        //    }

        //    string cuitFormateado = soloNumeros;

        //    // Aplicar formato XX-XXXXXXXX-X
        //    if (soloNumeros.Length > 2)
        //    {
        //        cuitFormateado = soloNumeros.Substring(0, 2) + "-" + soloNumeros.Substring(2);
        //    }

        //    if (soloNumeros.Length > 10)
        //    {
        //        cuitFormateado = soloNumeros.Substring(0, 2) + "-" + 
        //                        soloNumeros.Substring(2, 8) + "-" + 
        //                        soloNumeros.Substring(10);
        //    }

        //    // Solo actualizar si el texto cambió para evitar bucle infinito
        //    if (txtAfipCuit.Text != cuitFormateado)
        //    {
        //        // Temporalmente desconectar el evento para evitar recursión
        //        txtAfipCuit.TextChanged -= (s, e) => FormatearCUITAfip();

        //        txtAfipCuit.Text = cuitFormateado;

        //        // Ajustar posición del cursor
        //        int nuevaPosicion = Math.Min(cursorPosition, cuitFormateado.Length);

        //        // Si el cursor estaba después de una posición donde se insertó un guión, ajustar
        //        if (cursorPosition >= 2 && soloNumeros.Length > 2)
        //            nuevaPosicion = Math.Min(cursorPosition + 1, cuitFormateado.Length);
        //        if (cursorPosition >= 10 && soloNumeros.Length > 10)
        //            nuevaPosicion = Math.Min(nuevaPosicion + 1, cuitFormateado.Length);

        //        txtAfipCuit.SelectionStart = nuevaPosicion;

        //        // Reconectar el evento
        //        txtAfipCuit.TextChanged += (s, e) => FormatearCUITAfip();
        //    }

        //    // Validar dígito verificador cuando está completo
        //    if (soloNumeros.Length == 11)
        //    {
        //        ValidarDigitoVerificadorCUITAfip(soloNumeros);
        //    }
        //    else
        //    {
        //        // Limpiar indicadores de validación si no está completo
        //        txtAfipCuit.BackColor = Color.White;
        //    }
        //}

        // NUEVO: Validar dígito verificador para CUIT AFIP
        //private void ValidarDigitoVerificadorCUITAfip(string cuit)
        //{
        //    if (cuit.Length != 11 || !cuit.All(char.IsDigit))
        //    {
        //        txtAfipCuit.BackColor = Color.FromArgb(255, 235, 238); // Fondo rojo claro
        //        return;
        //    }

        //    // Usar el método estático existente para validar
        //    if (ValidarCUITCompleto(txtAfipCuit.Text))
        //    {
        //        txtAfipCuit.BackColor = Color.FromArgb(232, 245, 233); // Verde claro
        //    }
        //    else
        //    {
        //        txtAfipCuit.BackColor = Color.FromArgb(255, 235, 238); // Rojo claro
        //    }
        //}

        //private void SeleccionarCertificado()
        //{
        //    try
        //    {
        //        using var openFileDialog = new OpenFileDialog
        //        {
        //            Title = "Seleccionar Certificado AFIP",
        //            Filter = "Archivos de Certificado (*.p12;*.pfx)|*.p12;*.pfx|Todos los archivos (*.*)|*.*",
        //            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        //        };

        //        if (openFileDialog.ShowDialog() == DialogResult.OK)
        //        {
        //            txtAfipCertificadoPath.Text = openFileDialog.FileName;
        //            MostrarMensaje($"✅ Certificado seleccionado: {Path.GetFileName(openFileDialog.FileName)}", Color.Green);

        //            // Auto-limpiar el mensaje después de 3 segundos
        //            var timer = new System.Windows.Forms.Timer { Interval = 3000 };
        //            timer.Tick += (s, e) =>
        //            {
        //                if (lblMensaje.Text.Contains("Certificado seleccionado"))
        //                    lblMensaje.Text = "";
        //                timer.Stop();
        //                timer.Dispose();
        //            };
        //            timer.Start();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MostrarMensaje($"❌ Error seleccionando certificado: {ex.Message}", Color.Red);
        //    }
        //}

        //private void VerificarCertificadoAfip()
        //{
        //    try
        //    {
        //        string rutaCertificado = txtAfipCertificadoPath.Text.Trim();
        //        string password = txtAfipCertificadoPassword.Text;

        //        if (string.IsNullOrWhiteSpace(rutaCertificado))
        //        {
        //            MostrarMensaje("❌ Seleccione un certificado primero", Color.Red);
        //            return;
        //        }

        //        if (!File.Exists(rutaCertificado))
        //        {
        //            MostrarMensaje("❌ El archivo del certificado no existe", Color.Red);
        //            return;
        //        }

        //        // Verificar usando el servicio existente de AFIP
        //        var (valido, mensaje, vencimiento) = Comercio.NET.Servicios.AfipAuthenticator.VerificarCertificado(rutaCertificado, password);

        //        if (valido)
        //        {
        //            MostrarMensaje($"✅ Certificado válido - {mensaje}", Color.Green);

        //            // Mostrar detalles del certificado
        //            string detalles = $"CERTIFICADO AFIP VERIFICADO\n" +
        //                             $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
        //                             $"📄 Archivo: {Path.GetFileName(rutaCertificado)}\n" +
        //                             $"📅 {mensaje}\n\n" +
        //                             $"✅ El certificado es válido y puede utilizarse\n" +
        //                             $"   para la facturación electrónica AFIP.";

        //            MessageBox.Show(detalles, "Verificación de Certificado AFIP", 
        //                MessageBoxButtons.OK, MessageBoxIcon.Information);
        //        }
        //        else
        //        {
        //            MostrarMensaje($"❌ Error en certificado: {mensaje}", Color.Red);

        //            string detallesError = $"PROBLEMA CON CERTIFICADO AFIP\n" +
        //                                  $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
        //                                  $"📄 Archivo: {Path.GetFileName(rutaCertificado)}\n" +
        //                                  $"❌ Error: {mensaje}\n\n" +
        //                                  $"SOLUCIONES:\n" +
        //                                  $"• Verifique que sea un certificado válido de AFIP\n" +
        //                                  $"• Asegúrese de que la contraseña sea correcta\n" +
        //                                  $"• El archivo debe ser .p12 con clave privada incluida";

        //            MessageBox.Show(detallesError, "Error en Certificado AFIP", 
        //                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MostrarMensaje($"❌ Error verificando certificado: {ex.Message}", Color.Red);
        //    }
        //}

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

            //// Datos de AFIP
            //txtAfipCuit.Text = "20-12345678-9";
            //txtAfipCertificadoPath.Text = @"C:\Certificados\mi_certificado.p12";
            //txtAfipCertificadoPassword.Text = "clave123";
            //txtAfipWSAAUrl.Text = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";
            //txtAfipWSFEUrl.Text = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";

            // Nombres de cuentas corrientes de prueba
            lstNombresCtaCte.Items.Clear();
            lstNombresCtaCte.Items.AddRange(new string[] {
                "Cliente 1",
                "Cliente 2",
                "Cliente 3"
            });

            // Cadena de conexión de ejemplo (no funcional)
            // Cadenas de conexión de ejemplo (no funcionales)
            txtConnectionStringTesting.Text = "Server=localhost;Database=comercio_dev;Trusted_Connection=True;";
            txtConnectionStringProduccion.Text = "Server=production-server;Database=comercio;User Id=admin;Password=****;";
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

        // NUEVO: Método para manejar el cambio del checkbox
        private void ChkLimitarFacturacion_CheckedChanged(object sender, EventArgs e)
        {
            bool habilitado = chkLimitarFacturacion.Checked;
            
            // Buscar y mostrar/ocultar los controles relacionados
            if (panelRestriccionesImpresion != null)
            {
                var panelContenido = panelRestriccionesImpresion.Controls["panelContenidoRestriccionesImpresion"] as Panel;
                if (panelContenido != null)
                {
                    foreach (Control ctrl in panelContenido.Controls)
                    {
                        if (ctrl is Label lbl && (lbl.Text.Contains("Monto límite") || lbl.Text.Contains("El límite se verifica")))
                        {
                            lbl.Visible = habilitado;
                        }
                        else if (ctrl == txtMontoLimiteFacturacion)
                        {
                            ctrl.Visible = habilitado;
                            if (habilitado)
                            {
                                ctrl.Focus();
                            }
                        }
                    }
                }
            }
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
        Visible = true // Iniciar expandidos
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

            // Contenido colapsable - ALTURA AUMENTADA
            var panelContenido = new Panel
            {
                Name = "panelContenidoBaseDatos",
                Location = new Point(0, 30),
                Size = new Size(ancho, 370), // ✅ Altura aumentada para dos ambientes
                BackColor = Color.FromArgb(248, 248, 248),
                Visible = false
            };
            panel.Controls.Add(panelContenido);

            // === SELECTOR DE AMBIENTE ===
            var lblAmbiente = new Label
            {
                Text = "🌐 Ambiente de Base de Datos a utilizar:",
                Location = new Point(15, 10),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            };
            panelContenido.Controls.Add(lblAmbiente);

            rbDbTesting = new RadioButton
            {
                Text = "🧪 Testing (Desarrollo)",
                Location = new Point(270, 12),
                Size = new Size(150, 22),
                Font = new Font("Segoe UI", 9F),
                Checked = true // Por defecto Testing
            };
            rbDbTesting.CheckedChanged += RbDbAmbiente_CheckedChanged;
            panelContenido.Controls.Add(rbDbTesting);

            rbDbProduccion = new RadioButton
            {
                Text = "🏭 Producción",
                Location = new Point(430, 12),
                Size = new Size(130, 22),
                Font = new Font("Segoe UI", 9F)
            };
            rbDbProduccion.CheckedChanged += RbDbAmbiente_CheckedChanged;
            panelContenido.Controls.Add(rbDbProduccion);

            // Línea separadora
            var separador = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(15, 40),
                Size = new Size(ancho - 30, 2)
            };
            panelContenido.Controls.Add(separador);

            // === PANEL TESTING ===
            panelDbTesting = CrearPanelAmbienteBD("TESTING", 50, ancho - 30, true);
            panelContenido.Controls.Add(panelDbTesting);

            // === PANEL PRODUCCIÓN ===
            panelDbProduccion = CrearPanelAmbienteBD("PRODUCCION", 50, ancho - 30, false);
            panelContenido.Controls.Add(panelDbProduccion);

            // Información adicional
            var lblInfo = new Label
            {
                Text = "💡 Configure ambas cadenas de conexión y seleccione cuál desea utilizar. Los cambios se aplicarán al guardar.",
                Location = new Point(15, 335),
                Size = new Size(ancho - 30, 30),
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.TopLeft
            };
            panelContenido.Controls.Add(lblInfo);

            // Configurar eventos
            EventHandler clickHandler = (s, e) => ToggleColapsarBaseDatos();
            panelHeader.Click += clickHandler;
            lblTitulo.Click += clickHandler;

            return panel;
        }


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

    // Contenido colapsable - ALTURA AUMENTADA
    var panelContenido = new Panel
    {
        Name = "panelContenidoRestriccionesImpresion",
        Location = new Point(0, 30),
        Size = new Size(ancho, 330), // Altura aumentada
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

    // CheckBox principal para la restricción de remito
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

    // CheckBox para elegir entre vista previa o impresión directa
    chkVistaPreviaImpresionDirecta = new CheckBox
    {
        Text = "Usar vista previa antes de imprimir (recomendado)",
        Location = new Point(15, 120),
        Size = new Size(520, 25),
        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
        ForeColor = Color.FromArgb(62, 80, 100),
        Checked = true // Por defecto habilitado (vista previa)
    };
    panelContenido.Controls.Add(chkVistaPreviaImpresionDirecta);

    // Descripción de la nueva funcionalidad
    var lblExplicacionVistaPrevia = new Label
    {
        Text = "• Habilitado: Se mostrará una vista previa antes de enviar el documento a la impresora.\n" +
               "• Deshabilitado: Los comprobantes se enviarán directamente a la impresora sin confirmación.",
        Location = new Point(35, 145),
        Size = new Size(520, 40),
        Font = new Font("Segoe UI", 8F),
        ForeColor = Color.Gray
    };
    panelContenido.Controls.Add(lblExplicacionVistaPrevia);

    // CheckBox para limitar facturación diaria
    chkLimitarFacturacion = new CheckBox
    {
        Text = "Limitar facturación diaria",
        Location = new Point(15, 200),
        Size = new Size(520, 25),
        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
        ForeColor = Color.FromArgb(62, 80, 100),
        Checked = false // Por defecto deshabilitado
    };
    chkLimitarFacturacion.CheckedChanged += ChkLimitarFacturacion_CheckedChanged;
    panelContenido.Controls.Add(chkLimitarFacturacion);

    // Label para el monto
    var lblMontoLimite = new Label
    {
        Text = "Monto límite diario:",
        Location = new Point(35, 230),
        Size = new Size(120, 20),
        Font = new Font("Segoe UI", 9F),
        ForeColor = Color.FromArgb(62, 80, 100),
        Visible = false
    };
    panelContenido.Controls.Add(lblMontoLimite);

    // TextBox para el monto límite
    txtMontoLimiteFacturacion = new TextBox
    {
        Location = new Point(160, 228),
        Size = new Size(150, 22),
        Font = new Font("Segoe UI", 9F),
        PlaceholderText = "Ej: 50000.00",
        Visible = false
    };
    // Validar que solo se ingresen números y punto decimal
    txtMontoLimiteFacturacion.KeyPress += (s, e) =>
    {
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && 
            e.KeyChar != '.' && e.KeyChar != ',')
        {
            e.Handled = true;
        }
        
        // Solo permitir un punto decimal
        if ((e.KeyChar == '.' || e.KeyChar == ',') && 
            (txtMontoLimiteFacturacion.Text.Contains(".") || 
             txtMontoLimiteFacturacion.Text.Contains(",")))
        {
            e.Handled = true;
        }
    };
    panelContenido.Controls.Add(txtMontoLimiteFacturacion);

    // Descripción de la funcionalidad
    var lblExplicacionLimite = new Label
    {
        Text = "Cuando está habilitado, el sistema impedirá generar facturas si se supera el monto límite diario.\n" +
               "El límite se verifica sumando todas las facturas del día actual.",
        Location = new Point(35, 255),
        Size = new Size(520, 40),
        Font = new Font("Segoe UI", 8F),
        ForeColor = Color.Gray,
        Visible = false
    };
    panelContenido.Controls.Add(lblExplicacionLimite);

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
    }
}