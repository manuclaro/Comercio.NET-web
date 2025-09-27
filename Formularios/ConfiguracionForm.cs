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
        
        private Button btnGuardar, btnCancelar, btnTestearConexion, btnEditarBaseDatos;
        private Button btnColapsarComercio, btnColapsarFacturacion, btnColapsarInventario, btnColapsarBaseDatos;
        private CheckBox chkVerificarStock;
        private Label lblMensaje;
        private Panel panelPrincipal, panelComercio, panelFacturacion, panelBaseDatos, panelInventario;
        
        private string _rutaAppsettings;
        private JObject _configuracionOriginal;
        
        // Estados de colapso para cada sección
        private bool _comercioColapsado = false;
        private bool _facturacionColapsada = false;
        private bool _inventarioColapsado = false;
        private bool _baseDatosColapsada = true;
        private bool _edicionBaseDatosHabilitada = false;

        public ConfiguracionForm()
        {
            System.Diagnostics.Debug.WriteLine("[CONFIG] Iniciando ConfiguracionForm");
            
            InitializeComponent();
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
            this.ClientSize = new Size(650, 510); // Aumentar altura de 520 a 580
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfiguracionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configuración del Sistema";
            this.ResumeLayout(false);
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
                Size = new Size(panelWidth, 385), // Aumentar altura de 385 a 420
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };
            this.Controls.Add(panelPrincipal);

            // === CREAR TODAS LAS SECCIONES SIN POSICIONAMIENTO FIJO ===
            CrearTodasLasSecciones(panelWidth - 30);

            currentY += 390; // Aumentar de 370 a 430 para dar más espacio

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

            // === SECCIÓN BASE DE DATOS ===
            panelBaseDatos = CrearSeccionBaseDatos("🗄️ BASE DE DATOS", 0, ancho);
            panelPrincipal.Controls.Add(panelBaseDatos);
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
                Size = new Size(ancho, 160),
                BackColor = Color.FromArgb(248, 250, 252),
                Visible = true // Iniciar expandido
            };
            panel.Controls.Add(panelContenido);

            // Agregar campos de facturación al contenido
            // Primera fila: Razón Social y CUIT
            panelContenido.Controls.Add(CrearLabel("Razón Social:", 15, 10));
            txtRazonSocial = CrearTextBox(120, 8, 200);
            panelContenido.Controls.Add(txtRazonSocial);

            panelContenido.Controls.Add(CrearLabelCorta("CUIT:", 340, 10));
            txtCUIT = new TextBox
            {
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

            panelContenido.Controls.Add(CrearLabelCorta("Cond.:", 340, 40));
            cmbCondicion = new ComboBox
            {
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

            // Configurar eventos
            EventHandler clickHandler = (s, e) => ToggleColapsarFacturacion();
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

        private Label CrearLabel(string texto, int x, int y)
        {
            return new Label
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100)
            };
        }

        private Label CrearLabelCorta(string texto, int x, int y)
        {
            return new Label
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(45, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100)
            };
        }

        private Label CrearLabelLarga(string texto, int x, int y)
        {
            return new Label
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(130, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100)
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

        private void CrearBotones(int yPosition, int panelWidth)
        {
            int margin = 20;
            int anchoBotonGuardar = 160;
            int anchoBotonCancelar = 100;
            int espacioEntreBotones = 20;
            
            int totalAnchoBotones = anchoBotonGuardar + espacioEntreBotones + anchoBotonCancelar;
            int inicioX = margin + (panelWidth - totalAnchoBotones) / 2; // CORREGIDO: cambiar totalAnchoBotonGuardar por totalAnchoBotones

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
            chkVerificarStock.TabIndex = 9;
            txtConnectionString.TabIndex = 10;
            btnTestearConexion.TabIndex = 11;
            btnEditarBaseDatos.TabIndex = 12;
            btnGuardar.TabIndex = 13;
            btnCancelar.TabIndex = 14;
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

            // Formatear CUIT mientras el usuario escribe
            txtCUIT.TextChanged += (s, e) => FormatearCUIT();

            this.Load += (s, e) => {
                System.Diagnostics.Debug.WriteLine("[CONFIG] Formulario cargado");
                txtNombreComercio.Focus();
            };
            
            System.Diagnostics.Debug.WriteLine("[CONFIG] Eventos configurados");
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

                // Cargar configuración de inventario - CORREGIDO: usar la sección correcta
                bool verificarStock = _configuracionOriginal["Inventario"]?["VerificarStock"]?.ToObject<bool>() ?? 
                                     _configuracionOriginal["Validaciones"]?["ValidarStockDisponible"]?.ToObject<bool>() ?? 
                                     true;
                chkVerificarStock.Checked = verificarStock;

                // Cargar cadena de conexión
                txtConnectionString.Text = _configuracionOriginal["ConnectionStrings"]?["DefaultConnection"]?.ToString() ?? "";

                // NUEVO: Debug para confirmar valores cargados
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - Nombre: '{txtNombreComercio.Text}'");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - CUIT: '{txtCUIT.Text}'");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Cargado - Stock: {chkVerificarStock.Checked}");

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
                        ["Condicion"] = ""
                    },
                    ["Inventario"] = new JObject
                    {
                        ["VerificarStock"] = true
                    },
                    ["Validaciones"] = new JObject
                    {
                        ["ValidarStockDisponible"] = true
                    }
                };

                string jsonFormateado = JsonConvert.SerializeObject(configuracionDefault, Formatting.Indented);
                File.WriteAllText(_rutaAppsettings, jsonFormateado);
                
                _configuracionOriginal = configuracionDefault;
                System.Diagnostics.Debug.WriteLine("[CONFIG] Archivo por defecto creado");
                MostrarMensaje("✅ Archivo de configuración creado", Color.Blue);
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
            System.Diagnostics.Debug.WriteLine($"[SAVE] Nombre actual: '{txtNombreComercio?.Text ?? "NULL"}'");
            System.Diagnostics.Debug.WriteLine($"[SAVE] CUIT actual: '{txtCUIT?.Text ?? "NULL"}'");
            System.Diagnostics.Debug.WriteLine($"[SAVE] Stock actual: {chkVerificarStock?.Checked ?? false}");
            
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
                System.Diagnostics.Debug.WriteLine($"[SAVE] Archivo existe: {File.Exists(_rutaAppsettings)}");

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

                System.Diagnostics.Debug.WriteLine($"[SAVE] Facturación - CUIT: '{txtCUIT.Text.Trim()}'");

                // Mantener compatibilidad con sección "Validaciones" existente
                if (nuevaConfiguracion["Validaciones"] == null)
                    nuevaConfiguracion["Validaciones"] = new JObject();
        
                nuevaConfiguracion["Validaciones"]["ValidarStockDisponible"] = chkVerificarStock.Checked;

                System.Diagnostics.Debug.WriteLine($"[SAVE] Inventario - VerificarStock: {chkVerificarStock.Checked}");

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

            // Solo validar la cadena de conexión si la edición está habilitada
            if (_edicionBaseDatosHabilitada && string.IsNullOrWhiteSpace(txtConnectionString.Text))
            {
                MostrarMensaje("❌ La cadena de conexión es requerida", Color.Red);
                _baseDatosColapsada = false;
                ActualizarEstadoBaseDatos();
                ActualizarPosicionesTodasLasSecciones();
                txtConnectionString.Focus();
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

            // Sección Inventario
            panelInventario.Location = new Point(10, currentY);
            currentY += panelInventario.Height + spacing;

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
    }
}