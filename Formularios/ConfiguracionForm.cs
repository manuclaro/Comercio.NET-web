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
            InitializeComponent();
            ConfigurarFormulario();
            CargarConfiguracion();
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
            chkVerificarStock.TabIndex = 9;
            txtConnectionString.TabIndex = 10;
            btnTestearConexion.TabIndex = 11;
            btnEditarBaseDatos.TabIndex = 12;
            btnGuardar.TabIndex = 13;
            btnCancelar.TabIndex = 14;
        }

        private void ConfigurarEventos()
        {
            btnGuardar.Click += async (s, e) => await GuardarConfiguracion();
            btnCancelar.Click += (s, e) =>
            {
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

            this.Load += (s, e) => txtNombreComercio.Focus();
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

        // NUEVO MÉTODO: Actualizar posiciones dinámicamente
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
            string cuit = txtCUIT.Text.Replace("-", "");
            if (cuit.Length >= 2 && cuit.Length <= 11)
            {
                if (cuit.Length > 2 && cuit.Length <= 10)
                {
                    cuit = cuit.Insert(2, "-");
                }
                if (cuit.Length > 11)
                {
                    cuit = cuit.Insert(11, "-");
                }
                
                if (txtCUIT.Text != cuit)
                {
                    int cursorPosition = txtCUIT.SelectionStart;
                    txtCUIT.Text = cuit;
                    txtCUIT.SelectionStart = Math.Min(cursorPosition, cuit.Length);
                }
            }
        }

        private void CargarConfiguracion()
        {
            try
            {
                _rutaAppsettings = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                
                if (!File.Exists(_rutaAppsettings))
                {
                    MostrarMensaje("❌ No se encontró el archivo appsettings.json", Color.Red);
                    return;
                }

                string jsonContent = File.ReadAllText(_rutaAppsettings);
                _configuracionOriginal = JObject.Parse(jsonContent);

                // Cargar información del comercio
                txtNombreComercio.Text = _configuracionOriginal["Comercio"]?["Nombre"]?.ToString() ?? "Comercio";
                txtDomicilioComercio.Text = _configuracionOriginal["Comercio"]?["Domicilio"]?.ToString() ?? "Domicilio";

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

                // Cargar condición
                string condicion = _configuracionOriginal["Facturacion"]?["Condicion"]?.ToString() ?? "";
                if (cmbCondicion.Items.Contains(condicion))
                {
                    cmbCondicion.SelectedItem = condicion;
                }

                // Cargar configuración de inventario
                chkVerificarStock.Checked = _configuracionOriginal["Inventario"]?["VerificarStock"]?.ToObject<bool>() ?? true;

                // Cargar cadena de conexión
                txtConnectionString.Text = _configuracionOriginal["ConnectionStrings"]?["DefaultConnection"]?.ToString() ?? "";

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
                MostrarMensaje($"❌ Error cargando configuración: {ex.Message}", Color.Red);
            }
        }

        private async Task GuardarConfiguracion()
        {
            if (!ValidarDatos())
                return;

            try
            {
                btnGuardar.Enabled = false;
                btnGuardar.Text = "💾 Guardando...";
                MostrarMensaje("Guardando configuración...", Color.Blue);

                var nuevaConfiguracion = JObject.Parse(_configuracionOriginal.ToString());

                // Actualizar información del comercio
                if (nuevaConfiguracion["Comercio"] == null)
                    nuevaConfiguracion["Comercio"] = new JObject();
                
                nuevaConfiguracion["Comercio"]["Nombre"] = txtNombreComercio.Text.Trim();
                nuevaConfiguracion["Comercio"]["Domicilio"] = txtDomicilioComercio.Text.Trim();

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

                // Actualizar configuración de inventario
                if (nuevaConfiguracion["Inventario"] == null)
                    nuevaConfiguracion["Inventario"] = new JObject();
                
                nuevaConfiguracion["Inventario"]["VerificarStock"] = chkVerificarStock.Checked;

                // Actualizar cadena de conexión solo si la edición está habilitada
                if (_edicionBaseDatosHabilitada)
                {
                    if (nuevaConfiguracion["ConnectionStrings"] == null)
                        nuevaConfiguracion["ConnectionStrings"] = new JObject();
                    
                    nuevaConfiguracion["ConnectionStrings"]["DefaultConnection"] = txtConnectionString.Text.Trim();
                }

                // Crear backup del archivo original
                string backupPath = _rutaAppsettings + $".backup.{DateTime.Now:yyyyMMdd_HHmmss}";
                File.Copy(_rutaAppsettings, backupPath);

                // Guardar nueva configuración con formato JSON legible
                string jsonFormateado = JsonConvert.SerializeObject(nuevaConfiguracion, Formatting.Indented);
                await File.WriteAllTextAsync(_rutaAppsettings, jsonFormateado);

                MostrarMensaje("✅ Configuración guardada correctamente", Color.Green);
                
                var result = MessageBox.Show(
                    $"✅ Configuración guardada exitosamente.\n\n" +
                    $"Se creó un backup en:\n{backupPath}\n\n" +
                    "⚠️ IMPORTANTE: Reinicie la aplicación para que todos los cambios surtan efecto.\n\n" +
                    "¿Desea reiniciar la aplicación ahora?",
                    "Configuración Guardada",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    Application.Restart();
                }
                else
                {
                    await Task.Delay(2000);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error al guardar: {ex.Message}", Color.Red);
            }
            finally
            {
                btnGuardar.Enabled = true;
                btnGuardar.Text = "💾 Guardar Configuración";
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

            // Validar formato de CUIT (XX-XXXXXXXX-X)
            string cuitLimpio = txtCUIT.Text.Replace("-", "");
            if (cuitLimpio.Length != 11 || !cuitLimpio.All(char.IsDigit))
            {
                MostrarMensaje("❌ El CUIT debe tener formato XX-XXXXXXXX-X", Color.Red);
                _facturacionColapsada = false;
                ActualizarEstadoSeccion(panelFacturacion, "panelContenidoFacturacion", _facturacionColapsada, btnColapsarFacturacion, 35, 195);
                ActualizarPosicionesTodasLasSecciones();
                txtCUIT.Focus();
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
    }
}