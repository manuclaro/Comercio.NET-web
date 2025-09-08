using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Comercio.NET.Formularios
{
    public partial class Productos : Form
    {
        private DataTable productosTable;
        private System.Windows.Forms.Timer searchTimer;
        private string lastSearchText = "";
        private bool isInitialized = false;
        
        // NUEVO: Paneles para organizar el layout
        private Panel panelSuperior;
        private Panel panelInferior;
        private Panel panelCentral;
        
        // NUEVO: Controles para indicar carga
        private Panel panelCarga;
        private ProgressBar progressBarCarga;
        private Label lblCargando;
        private PictureBox picSpinner;
        private System.Windows.Forms.Timer timerSpinner;
        private int spinnerAngle = 0;
        
        // OPTIMIZACIÓN: Cache de fuentes para evitar recrearlas constantemente
        private static readonly Font _headerFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        private static readonly Font _normalFont = new Font("Segoe UI", 9F);
        private static readonly Font _boldFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        private static readonly Font _textBoxFont = new Font("Segoe UI", 10F);
        private static readonly Font _filterFont = new Font("Segoe UI", 11F);

        public Productos()
        {
            InitializeComponent();
            // OPTIMIZACIÓN: Configurar limpieza de recursos al cerrar
            this.FormClosed += Productos_FormClosed;
        }

        // OPTIMIZACIÓN: Limpieza de recursos cuando se cierra el formulario
        private void Productos_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                searchTimer?.Stop();
                searchTimer?.Dispose();
                searchTimer = null;
                
                // NUEVO: Limpiar recursos del spinner
                timerSpinner?.Stop();
                timerSpinner?.Dispose();
                timerSpinner = null;
                
                productosTable?.Dispose();
                productosTable = null;
            }
            catch
            {
                // Ignorar errores en limpieza
            }
        }

        private void ConfigurarFormularioPersonalizado()
        {
            if (isInitialized) return;

            // Configuración general del formulario
            this.Text = "Gestión de Productos";
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(1000, 500);
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = _normalFont;

            // NUEVO: Crear estructura de paneles ANTES de aplicar estilos
            CrearEstructuraPaneles();
            
            // NUEVO: Crear controles de carga
            CrearControlesdeCarga();
            
            AplicarEstilosModernos();
            ConfigurarSearchTimer();

            isInitialized = true;
        }

        // NUEVO: Crear controles para mostrar indicadores de carga
        private void CrearControlesdeCarga()
        {
            // Panel de carga que se superpone al panel central
            panelCarga = new Panel
            {
                Name = "panelCarga",
                BackColor = Color.FromArgb(240, 245, 248, 250), // Transparencia simulada
                Dock = DockStyle.Fill,
                Visible = false
            };

            // Barra de progreso
            progressBarCarga = new ProgressBar
            {
                Style = ProgressBarStyle.Continuous, // CAMBIO: de Marquee a Continuous
                Maximum = 100,                       // NUEVO: Valor máximo
                Value = 0,                          // NUEVO: Valor inicial
                Size = new Size(300, 8),            // CAMBIO: Altura de 6 a 8
                ForeColor = Color.FromArgb(63, 81, 181)
            };

            // Etiqueta de carga
            lblCargando = new Label
            {
                Text = "🔄 Cargando productos...",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Spinner personalizado
            picSpinner = new PictureBox
            {
                Size = new Size(32, 32),
                BackColor = Color.Transparent
            };
            picSpinner.Paint += PicSpinner_Paint;

            // Timer para animar el spinner
            timerSpinner = new System.Windows.Forms.Timer
            {
                Interval = 50 // 20 FPS
            };
            timerSpinner.Tick += TimerSpinner_Tick;

            // Agregar controles al panel de carga
            panelCarga.Controls.Add(progressBarCarga);
            panelCarga.Controls.Add(lblCargando);
            panelCarga.Controls.Add(picSpinner);

            // Evento para centrar controles cuando cambie el tamaño
            panelCarga.Resize += PanelCarga_Resize;
        }

        // NUEVO: Dibujar spinner personalizado
        private void PicSpinner_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int centerX = picSpinner.Width / 2;
            int centerY = picSpinner.Height / 2;
            int radius = Math.Min(centerX, centerY) - 2;

            // Dibujar círculo de puntos giratorio
            for (int i = 0; i < 12; i++)
            {
                double angle = (spinnerAngle + i * 30) * Math.PI / 180;
                float x = centerX + (float)(radius * 0.7 * Math.Cos(angle));
                float y = centerY + (float)(radius * 0.7 * Math.Sin(angle));

                // Fade effect para los puntos
                int alpha = Math.Max(50, 255 - (i * 20));
                using (var brush = new SolidBrush(Color.FromArgb(alpha, 63, 81, 181)))
                {
                    g.FillEllipse(brush, x - 2, y - 2, 4, 4);
                }
            }
        }

        // NUEVO: Animar el spinner
        private void TimerSpinner_Tick(object sender, EventArgs e)
        {
            spinnerAngle = (spinnerAngle + 30) % 360;
            picSpinner.Invalidate();
        }

        // NUEVO: Centrar controles de carga
        private void PanelCarga_Resize(object sender, EventArgs e)
        {
            if (panelCarga == null) return;

            int centerX = panelCarga.Width / 2;
            int centerY = panelCarga.Height / 2;

            // Posicionar spinner
            picSpinner.Location = new Point(centerX - 16, centerY - 50);

            // Posicionar etiqueta
            lblCargando.Location = new Point(centerX - lblCargando.Width / 2, centerY - 10);

            // Posicionar barra de progreso
            progressBarCarga.Location = new Point(centerX - 150, centerY + 20);
        }

        // NUEVO: Mostrar indicadores de carga
        private void MostrarCarga(string mensaje = "🔄 Cargando productos...")
        {
            this.Invoke((Action)(() =>
            {
                if (panelCarga != null && panelCentral != null)
                {
                    lblCargando.Text = mensaje;
                    
                    // Agregar panel de carga al panel central
                    if (!panelCentral.Controls.Contains(panelCarga))
                    {
                        panelCentral.Controls.Add(panelCarga);
                        panelCarga.BringToFront();
                    }
                    
                    panelCarga.Visible = true;
                    progressBarCarga.Visible = true;
                    timerSpinner.Start();
                    
                    // Reposicionar controles
                    PanelCarga_Resize(null, null);
                }
            }));
        }

        // NUEVO: Ocultar indicadores de carga
        private void OcultarCarga()
        {
            this.Invoke((Action)(() =>
            {
                timerSpinner?.Stop();
                if (panelCarga != null)
                {
                    panelCarga.Visible = false;
                    panelCentral?.Controls.Remove(panelCarga);
                }
            }));
        }

        // NUEVO: Crear estructura con paneles superior, central e inferior
        private void CrearEstructuraPaneles()
        {
            this.SuspendLayout();

            try
            {
                // Panel Superior (botones y filtros)
                panelSuperior = new Panel
                {
                    Name = "panelSuperior",
                    Height = 80,
                    Dock = DockStyle.Top,
                    BackColor = Color.FromArgb(245, 248, 250),
                    Padding = new Padding(10, 10, 10, 5)
                };

                // Panel Inferior (contador y información)
                panelInferior = new Panel
                {
                    Name = "panelInferior", 
                    Height = 50,
                    Dock = DockStyle.Bottom,
                    BackColor = Color.FromArgb(245, 248, 250),
                    Padding = new Padding(10, 5, 10, 10)
                };

                // Panel Central (grilla)
                panelCentral = new Panel
                {
                    Name = "panelCentral",
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Padding = new Padding(10, 5, 10, 5)
                };

                // Agregar paneles al formulario en orden correcto
                this.Controls.Add(panelCentral);  // Primero el central
                this.Controls.Add(panelInferior); // Luego el inferior
                this.Controls.Add(panelSuperior); // Finalmente el superior

                // NUEVO: Mover controles existentes a los paneles apropiados
                MoverControlesAPaneles();
            }
            finally
            {
                this.ResumeLayout();
            }
        }

        // NUEVO: Mover controles existentes a sus paneles correspondientes
        private void MoverControlesAPaneles()
        {
            // Buscar y mover controles al panel superior
            var controlesToMove = new List<Control>();
            
            foreach (Control control in this.Controls)
            {
                if (control.Name != "panelSuperior" && 
                    control.Name != "panelInferior" && 
                    control.Name != "panelCentral" &&
                    control != GrillaProductos)
                {
                    controlesToMove.Add(control);
                }
            }

            // Mover controles al panel superior
            foreach (var control in controlesToMove)
            {
                this.Controls.Remove(control);
                
                // Filtro y botones van al panel superior
                if (control.Name?.Contains("btn") == true || 
                    control.Name?.Contains("txt") == true ||
                    control.Name?.Contains("lbl") == true && !control.Name.Contains("Contador"))
                {
                    panelSuperior.Controls.Add(control);
                }
                // Contador va al panel inferior
                else if (control.Name?.Contains("Contador") == true)
                {
                    panelInferior.Controls.Add(control);
                }
            }

            // Mover la grilla al panel central
            if (GrillaProductos != null && GrillaProductos.Parent == this)
            {
                this.Controls.Remove(GrillaProductos);
                panelCentral.Controls.Add(GrillaProductos);
                GrillaProductos.Dock = DockStyle.Fill;
            }

            // NUEVO: Reposicionar controles en sus nuevos paneles
            ReposicionarControlesEnPaneles();
        }

        // NUEVO: Reposicionar controles dentro de los paneles
        private void ReposicionarControlesEnPaneles()
        {
            // Reposicionar en panel superior
            if (panelSuperior != null)
            {
                int margen = 15;
                int yFila1 = 15;  // Primera fila para el filtro
                int yFila2 = 45;  // Segunda fila para los botones
                int x = margen;

                // Ubicar controles de filtro (sin cambios)
                var lblFiltro = panelSuperior.Controls.OfType<Label>()
                    .FirstOrDefault(l => l.Name?.Contains("Filtro") == true ||
                                          l.Name?.Contains("Buscar") == true ||
                                          l.Text?.Contains("Buscar") == true ||
                                          l.Text?.Contains("Filtro") == true);
                if (lblFiltro != null)
                {
                    lblFiltro.Location = new Point(margen, yFila1 - 25);
                    lblFiltro.Size = new Size(200, 20);
                    lblFiltro.Text = "🔍 Buscar producto:";
                }
                if (txtFiltroDescripcion != null)
                {
                    txtFiltroDescripcion.Location = new Point(margen, yFila1);
                    txtFiltroDescripcion.Size = new Size(300, 25);
                    txtFiltroDescripcion.PlaceholderText = "Escriba para filtrar productos...";
                }

                // Construir la lista de botones en el orden deseado
                var todosLosBotones = new List<Button>();
                if (btnAgregarProducto != null)
                    todosLosBotones.Add(btnAgregarProducto);
                if (btnModificarProducto != null)
                    todosLosBotones.Add(btnModificarProducto);

                // Insertar el botón "Actualizar Stock" (btnAbrirActualizar) después de "Modificar"
                var btnAbrirActualizar = panelSuperior.Controls.OfType<Button>()
                    .FirstOrDefault(b => b.Name == "btnAbrirActualizar");
                if (btnAbrirActualizar != null)
                    todosLosBotones.Add(btnAbrirActualizar);

                // Agregar los demás botones dinámicos
                var btnEliminar = panelSuperior.Controls.OfType<Button>()
                    .FirstOrDefault(b => b.Name == "btnEliminar");
                if (btnEliminar != null)
                    todosLosBotones.Add(btnEliminar);
                var btnRefrescar = panelSuperior.Controls.OfType<Button>()
                    .FirstOrDefault(b => b.Name == "btnRefrescar");
                if (btnRefrescar != null)
                    todosLosBotones.Add(btnRefrescar);
                var btnSalir = panelSuperior.Controls.OfType<Button>()
                    .FirstOrDefault(b => b.Name == "btnSalir" || b.Text.Contains("Salir"));
                if (btnSalir != null)
                    todosLosBotones.Add(btnSalir);

                // Posicionar cada botón con espaciado adecuado
                foreach (var boton in todosLosBotones)
                {
                    boton.Location = new Point(x, yFila2);
                    if (boton.Name == "btnSalir")
                        boton.Size = new Size(80, 35);
                    else if (boton.Name == "btnRefrescar")
                        boton.Size = new Size(110, 35);
                    else
                        boton.Size = new Size(120, 35);

                    x += boton.Width + 12; // Espaciado entre botones
                }

                // Ubicar los demás controles (etiquetas, textbox, etc.) que no han sido incluidos en la lista.
                var otrosControles = panelSuperior.Controls.Cast<Control>()
                    .Where(c => c != txtFiltroDescripcion && 
                                c != lblFiltro && 
                                !todosLosBotones.Contains(c))
                    .ToList();
                foreach (var control in otrosControles)
                {
                    if (control is Label lbl && lbl != lblFiltro)
                    {
                        lbl.Location = new Point(txtFiltroDescripcion.Right + 20, yFila1);
                        lbl.Size = new Size(200, 25);
                    }
                    else if (control is TextBox || control is ComboBox)
                    {
                        control.Location = new Point(txtFiltroDescripcion.Right + 20, yFila1);
                        control.Size = new Size(150, 25);
                    }
                }
            }

            // Reposicionar en panel inferior
            if (panelInferior != null)
            {
                // CORREGIDO: Buscar el contador de forma más específica
                var lblContador = panelInferior.Controls.OfType<Label>()
                    .FirstOrDefault(l => l.Name?.Contains("Contador") == true ||
                                        l.Name?.Contains("Total") == true ||
                                        l.Text?.Contains("Registros") == true ||
                                        l.Text?.Contains("📊") == true);

                if (lblContador != null)
                {
                    lblContador.Location = new Point(15, 15);
                    lblContador.Size = new Size(500, 25);
                    lblContador.Anchor = AnchorStyles.Left | AnchorStyles.Top;
                    lblContador.Font = _boldFont;
                    lblContador.ForeColor = Color.FromArgb(62, 80, 100);
                }

                // Si hay otros controles en el panel inferior
                var otrosControlesInferior = panelInferior.Controls.Cast<Control>()
                    .Where(c => c != lblContador)
                    .ToList();

                int xInferior = lblContador?.Right + 30 ?? 15;
                foreach (var control in otrosControlesInferior)
                {
                    control.Location = new Point(xInferior, 15);
                    control.Size = new Size(100, 25);
                    xInferior += control.Width + 15;
                }
            }
        }

        // NUEVO: Método helper para organizar mejor los controles después de crearlos
        private void OrganizarControlesPanelSuperior()
        {
            if (panelSuperior == null) return;

            // Llamar después de crear todos los controles
            Task.Delay(100).ContinueWith(_ => 
            {
                this.Invoke((Action)(() => ReposicionarControlesEnPaneles()));
            });
        }

        // AGREGADO: Método AplicarEstilosModernos faltante
        private void AplicarEstilosModernos()
        {
            this.SuspendLayout();

            try
            {
                // OPTIMIZACIÓN: Configurar la grilla con estilos modernos más eficientes
                if (GrillaProductos != null)
                {
                    GrillaProductos.SuspendLayout();

                    // OPTIMIZACIÓN: Configuración básica más rápida
                    GrillaProductos.BackgroundColor = Color.White;
                    GrillaProductos.BorderStyle = BorderStyle.None;
                    GrillaProductos.AllowUserToAddRows = false;
                    GrillaProductos.AllowUserToDeleteRows = false;
                    GrillaProductos.ReadOnly = true;
                    GrillaProductos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                    GrillaProductos.MultiSelect = false;
                    GrillaProductos.RowHeadersVisible = false;
                    GrillaProductos.EnableHeadersVisualStyles = false;
                    GrillaProductos.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                    GrillaProductos.GridColor = Color.FromArgb(230, 236, 240);
                    
                    // OPTIMIZACIÓN: Mejorar rendimiento del DataGridView
                    GrillaProductos.VirtualMode = false;
                    GrillaProductos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                    GrillaProductos.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
                    
                    // CAMBIO: Configuración para usar todo el espacio del panel central
                    GrillaProductos.ScrollBars = ScrollBars.Both;
                    GrillaProductos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                    // CAMBIO: Estilo de encabezados - TODOS centrados y mismo color
                    GrillaProductos.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(63, 81, 181);
                    GrillaProductos.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                    GrillaProductos.ColumnHeadersDefaultCellStyle.Font = _headerFont;
                    GrillaProductos.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    GrillaProductos.ColumnHeadersHeight = 40;

                    // Estilo de filas - usar fuentes cacheadas
                    GrillaProductos.DefaultCellStyle.BackColor = Color.White;
                    GrillaProductos.DefaultCellStyle.ForeColor = Color.FromArgb(62, 80, 100);
                    GrillaProductos.DefaultCellStyle.SelectionBackColor = Color.FromArgb(227, 242, 253);
                    GrillaProductos.DefaultCellStyle.SelectionForeColor = Color.FromArgb(62, 80, 100);
                    GrillaProductos.DefaultCellStyle.Font = _normalFont;
                    GrillaProductos.RowTemplate.Height = 35;

                    // Filas alternadas
                    GrillaProductos.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

                    // OPTIMIZACIÓN: Agregar eventos solo una vez
                    GrillaProductos.SelectionChanged -= GrillaProductos_SelectionChanged;
                    GrillaProductos.CellDoubleClick -= GrillaProductos_CellDoubleClick;
                    GrillaProductos.SelectionChanged += GrillaProductos_SelectionChanged;
                    GrillaProductos.CellDoubleClick += GrillaProductos_CellDoubleClick;

                    GrillaProductos.ResumeLayout();
                }

                // Configurar el TextBox de filtro
                if (txtFiltroDescripcion != null)
                {
                    txtFiltroDescripcion.Font = _filterFont;
                    txtFiltroDescripcion.BackColor = Color.FromArgb(250, 252, 254);
                    txtFiltroDescripcion.ForeColor = Color.FromArgb(62, 80, 100);
                    txtFiltroDescripcion.BorderStyle = BorderStyle.FixedSingle;

                    // OPTIMIZACIÓN: Remover eventos anteriores antes de agregar
                    txtFiltroDescripcion.TextChanged -= TxtFiltroDescripcion_TextChanged;
                    txtFiltroDescripcion.KeyDown -= TxtFiltroDescripcion_KeyDown;
                    txtFiltroDescripcion.TextChanged += TxtFiltroDescripcion_TextChanged;
                    txtFiltroDescripcion.KeyDown += TxtFiltroDescripcion_KeyDown;
                }

                ConfigurarBotonesModernos();
                
                // NUEVO: Aplicar estilos a los paneles
                AplicarEstilosPaneles();
            }
            finally
            {
                this.ResumeLayout();
            }
        }

        // AGREGADO: Método AplicarEstilosPaneles faltante
        private void AplicarEstilosPaneles()
        {
            if (panelSuperior != null)
            {
                panelSuperior.BackColor = Color.FromArgb(250, 252, 254);
                // Agregar un borde sutil al panel superior
                panelSuperior.Paint += (s, e) =>
                {
                    using (var pen = new Pen(Color.FromArgb(220, 226, 230), 1))
                    {
                        e.Graphics.DrawLine(pen, 0, panelSuperior.Height - 1, panelSuperior.Width, panelSuperior.Height - 1);
                    }
                };
            }

            if (panelInferior != null)
            {
                panelInferior.BackColor = Color.FromArgb(250, 252, 254);
                // Agregar un borde sutil al panel inferior
                panelInferior.Paint += (s, e) =>
                {
                    using (var pen = new Pen(Color.FromArgb(220, 226, 230), 1))
                    {
                        e.Graphics.DrawLine(pen, 0, 0, panelInferior.Width, 0);
                    }
                };
            }

            if (panelCentral != null)
            {
                panelCentral.BackColor = Color.White;
            }
        }

        // AGREGADO: Método ConfigurarBoton faltante
        private void ConfigurarBoton(Button btn, string texto, Color color, Size tamaño)
        {
            btn.Text = texto;
            btn.Size = tamaño;
            btn.BackColor = color;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = _boldFont;
            btn.Cursor = Cursors.Hand;
            AplicarHoverButton(btn);
        }

        // AGREGADO: Método BtnSalir_Click faltante
        private void BtnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // CORREGIDO: Actualizar el método de creación de botones para mejor posicionamento
        private void CrearBotonSiNoExiste(string nombre, string texto, Color color, Size tamaño, EventHandler clickHandler)
        {
            var boton = this.Controls.Find(nombre, true).FirstOrDefault() as Button;
            if (boton != null) return; // Ya existe

            boton = new Button
            {
                Name = nombre,
                Text = texto,
                Size = tamaño,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = _boldFont,
                Cursor = Cursors.Hand
            };
            
            boton.FlatAppearance.BorderSize = 0;
            boton.Click += clickHandler;

            // CAMBIO: Agregar al panel superior
            if (panelSuperior != null)
            {
                panelSuperior.Controls.Add(boton);
                // NUEVO: Reorganizar después de agregar
                OrganizarControlesPanelSuperior();
            }

            AplicarHoverButton(boton);
        }

        // CORREGIDO: Actualizar el método de configuración de botones
        private void ConfigurarBotonesModernos()
        {
            // OPTIMIZACIÓN: Configurar botones más eficientemente
            if (btnAgregarProducto != null)
            {
                btnAgregarProducto.Click -= BtnAgregarProducto_Click;
                btnAgregarProducto.Click += BtnAgregarProducto_Click;
                ConfigurarBoton(btnAgregarProducto, "➕ Agregar", Color.FromArgb(76, 175, 80), new Size(120, 35));
            }

            if (btnModificarProducto != null)
            {
                btnModificarProducto.Click -= BtnModificarProducto_Click;
                btnModificarProducto.Click += BtnModificarProducto_Click;
                ConfigurarBoton(btnModificarProducto, "✏️ Modificar", Color.FromArgb(255, 152, 0), new Size(120, 35));
            }

            // CORREGIDO: Quitar espacios en el nombre del método
            CrearBotonSiNoExiste("btnEliminar", "🗑️ Eliminar", Color.FromArgb(244, 67, 54), new Size(120, 35), BtnEliminar_Click);
            CrearBotonSiNoExiste("btnRefrescar", "🔄 Refrescar", Color.FromArgb(96, 125, 139), new Size(110, 35), BtnRefrescar_Click);
            
            ConfigurarBotonSalir();

            // NUEVO: Crear y agregar el botón "Actualizar Stock" si no existe
            var btnActualizar = this.Controls.Find("btnAbrirActualizar", true).FirstOrDefault() as Button;
            if (btnActualizar == null)
            {
                btnActualizar = new Button
                {
                    Name = "btnAbrirActualizar",
                    Text = "Actualizar Stock",
                    Size = new Size(120, 35),
                    BackColor = Color.FromArgb(96, 125, 139),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = _boldFont,
                    Cursor = Cursors.Hand
                };

                btnActualizar.FlatAppearance.BorderSize = 0;
                btnActualizar.Click += btnAbrirActualizar_Click;

                if (panelSuperior != null)
                {
                    panelSuperior.Controls.Add(btnActualizar);
                    OrganizarControlesPanelSuperior();
                }

                AplicarHoverButton(btnActualizar);
            }

            // NUEVO: Reorganizar todos los controles después de configurar
            OrganizarControlesPanelSuperior();
        }

        // CORREGIDO: Actualizar configuración del botón salir
        private void ConfigurarBotonSalir()
        {
            var btnSalir = this.Controls.Find("btnSalir", true).FirstOrDefault() as Button;
            if (btnSalir != null)
            {
                btnSalir.Text = "❌ Salir";
                btnSalir.Size = new Size(80, 35);
                btnSalir.BackColor = Color.FromArgb(158, 158, 158);
                btnSalir.ForeColor = Color.White;
                btnSalir.FlatStyle = FlatStyle.Flat;
                btnSalir.FlatAppearance.BorderSize = 0;
                btnSalir.Font = _boldFont;
                btnSalir.Cursor = Cursors.Hand;

                btnSalir.Click -= BtnSalir_Click;
                btnSalir.Click += BtnSalir_Click;

                AplicarHoverButton(btnSalir);

                // NUEVO: Asegurar que esté en el panel superior
                if (panelSuperior != null && btnSalir.Parent != panelSuperior)
                {
                    btnSalir.Parent?.Controls.Remove(btnSalir);
                    panelSuperior.Controls.Add(btnSalir);
                }
            }
        }

        private void ConfigurarSearchTimer()
        {
            if (searchTimer == null)
            {
                searchTimer = new System.Windows.Forms.Timer();
                searchTimer.Interval = 500; // OPTIMIZACIÓN: Aumentar intervalo para reducir búsquedas
                searchTimer.Tick += SearchTimer_Tick;
            }
        }

        private void AplicarHoverButton(Button btn)
        {
            Color originalColor = btn.BackColor;

            btn.MouseEnter += (s, e) =>
            {
                btn.BackColor = Color.FromArgb(
                    Math.Max(0, originalColor.R - 20),
                    Math.Max(0, originalColor.G - 20),
                    Math.Max(0, originalColor.B - 20)
                );
            };

            btn.MouseLeave += (s, e) =>
            {
                btn.BackColor = originalColor;
            };
        }

        private void Productos_Load(object sender, EventArgs e)
        {
            ConfigurarFormularioPersonalizado();

            // CAMBIO: Mostrar indicador de carga antes de cargar datos
            MostrarCarga("🔄 Cargando productos...");

            // OPTIMIZACIÓN: Cargar datos en background thread más eficiente
            Task.Run(async () =>
            {
                await CargarProductosAsync();
                
                this.Invoke((Action)(() =>
                {
                    OcultarCarga(); // NUEVO: Ocultar indicador al terminar
                    txtFiltroDescripcion?.Focus();
                }));
            });
        }

        // MODIFICADO: Método asíncrono para cargar productos con indicadores de progreso
        private async Task CargarProductosAsync()
        {
            try
            {
                this.Invoke((Action)(() => this.Cursor = Cursors.WaitCursor));

                ActualizarProgreso(10, "📡 Conectando a la base de datos...");

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");
                var newTable = new DataTable();

                ActualizarProgreso(25, "📋 Consultando productos...");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    ActualizarProgreso(40, "📋 Ejecutando consulta...");
                    
                    var query = "SELECT codigo, descripcion, rubro, marca, costo, porcentaje, precio, cantidad, proveedor FROM Productos ORDER BY descripcion";
                    
                    using (var adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.SelectCommand.CommandTimeout = 30;
                        
                        // Simular progreso durante la carga de datos
                        await Task.Run(() => 
                        {
                            adapter.Fill(newTable);
                        });
                        
                        ActualizarProgreso(75, "⚙️ Procesando datos...");
                    }
                }

                ActualizarProgreso(90, "🎨 Configurando vista...");

                // Pequeña pausa para mostrar el progreso
                await Task.Delay(200);

                // OPTIMIZACIÓN: Actualizar UI en thread principal
                this.Invoke((Action)(() =>
                {
                    productosTable = newTable;
                    
                    if (GrillaProductos != null)
                    {
                        GrillaProductos.SelectionChanged -= GrillaProductos_SelectionChanged;
                        GrillaProductos.DataSource = productosTable;
                        ConfigurarColumnas();
                        GrillaProductos.SelectionChanged += GrillaProductos_SelectionChanged;
                    }

                    ActualizarContador();
                }));
                
                ActualizarProgreso(100, "✅ Carga completada");
                await Task.Delay(300); // Mostrar "completado" brevemente
            }
            catch (Exception ex)
            {
                this.Invoke((Action)(() =>
                {
                    OcultarCarga();
                    MessageBox.Show($"Error al cargar productos: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
            finally
            {
                this.Invoke((Action)(() => this.Cursor = Cursors.Default));
            }
        }

        // MODIFICADO: Método síncrono con indicadores de carga
        private void CargarProductos()
        {
            MostrarCarga("🔄 Refrescando datos...");
            Task.Run(async () => 
            {
                await CargarProductosAsync();
                this.Invoke((Action)(() => OcultarCarga()));
            });
        }

        // CAMBIO: Nueva configuración de columnas para aprovechar todo el ancho
        private void ConfigurarColumnas()
        {
            if (GrillaProductos?.Columns?.Count == 0) return;
                
            GrillaProductos.SuspendLayout();

            try
            {
                var columnConfig = new Dictionary<string, (double percentage, string header, DataGridViewContentAlignment headerAlign, DataGridViewContentAlignment cellAlign, string format)>
                {
                    // CAMBIO: Todos los headers centrados
                    ["codigo"] = (0.10, "CÓDIGO", DataGridViewContentAlignment.MiddleCenter, DataGridViewContentAlignment.MiddleCenter, ""),
                    ["descripcion"] = (0.35, "DESCRIPCIÓN", DataGridViewContentAlignment.MiddleCenter, DataGridViewContentAlignment.MiddleLeft, ""),
                    ["rubro"] = (0.12, "RUBRO", DataGridViewContentAlignment.MiddleCenter, DataGridViewContentAlignment.MiddleCenter, ""),
                    ["marca"] = (0.12, "MARCA", DataGridViewContentAlignment.MiddleCenter, DataGridViewContentAlignment.MiddleCenter, ""),
                    ["costo"] = (0.10, "COSTO", DataGridViewContentAlignment.MiddleCenter, DataGridViewContentAlignment.MiddleCenter, "C2"),
                    ["porcentaje"] = (0.06, "%", DataGridViewContentAlignment.MiddleCenter, DataGridViewContentAlignment.MiddleCenter, ""),
                    ["precio"] = (0.10, "PRECIO", DataGridViewContentAlignment.MiddleCenter, DataGridViewContentAlignment.MiddleCenter, "C2"),
                    ["cantidad"] = (0.08, "STOCK", DataGridViewContentAlignment.MiddleCenter, DataGridViewContentAlignment.MiddleCenter, ""),
                    ["proveedor"] = (0.07, "PROV.", DataGridViewContentAlignment.MiddleCenter, DataGridViewContentAlignment.MiddleCenter, "")
                };

                foreach (var config in columnConfig)
                {
                    var col = GrillaProductos.Columns[config.Key];
                    if (col != null)
                    {
                        // CAMBIO: Configurar anchos proporcionales
                        col.FillWeight = (float)(config.Value.percentage * 100);
                        col.HeaderText = config.Value.header;

                        // CAMBIO: Header siempre centrado, pero celda con alineación específica
                        col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter; // TODOS centrados
                        col.DefaultCellStyle.Alignment = config.Value.cellAlign;
                        
                        if (!string.IsNullOrEmpty(config.Value.format))
                            col.DefaultCellStyle.Format = config.Value.format;
                    }
                }

                // NUEVO: Asegurar que todas las columnas tengan el mismo estilo
                if (GrillaProductos != null)
                {
                    GrillaProductos.DataBindingComplete += (s, e) =>
                    {
                        foreach (DataGridViewColumn col in GrillaProductos.Columns)
                        {
                            col.HeaderCell.Style.BackColor = Color.FromArgb(63, 81, 181);
                            col.HeaderCell.Style.ForeColor = Color.White;
                            col.HeaderCell.Style.Font = _headerFont;
                            col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        }
                    };
                }

                // OPTIMIZACIÓN: Aplicar formato solo después de configurar columnas
                AplicarFormatoStock();
            }
            finally
            {
                GrillaProductos.ResumeLayout();
            }
        }

        // OPTIMIZACIÓN: Filtro más eficiente
        private void AplicarFiltroDescripcion(string texto)
        {
            if (productosTable == null) return;

            try
            {
                texto = texto.Replace("'", "''").Trim();
                
                if (string.IsNullOrEmpty(texto))
                {
                    productosTable.DefaultView.RowFilter = "";
                }
                else
                {
                    // OPTIMIZACIÓN: Filtro más simple para mayor velocidad
                    if (texto.Length < 3)
                    {
                        // Para búsquedas cortas, solo buscar en código y descripción
                        productosTable.DefaultView.RowFilter = $"(codigo LIKE '{texto}%' OR descripcion LIKE '%{texto}%')";
                    }
                    else
                    {
                        // Para búsquedas más largas, usar filtro completo
                        var palabras = texto.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var filtros = palabras.Take(3).Select(palabra => // OPTIMIZACIÓN: Limitar a 3 palabras máximo
                            $"(descripcion LIKE '%{palabra}%' OR codigo LIKE '%{palabra}%' OR marca LIKE '%{palabra}%' OR rubro LIKE '%{palabra}%')");
                        string filtroFinal = string.Join(" AND ", filtros);
                        productosTable.DefaultView.RowFilter = filtroFinal;
                    }
                }

                ActualizarContador();
                
                // OPTIMIZACIÓN: Solo aplicar formato si hay pocos registros visibles
                if (productosTable.DefaultView.Count < 1000)
                {
                    AplicarFormatoStock();
                }
            }
            catch (Exception ex)
            {
                // Si hay error en el filtro, limpiar
                productosTable.DefaultView.RowFilter = "";
                Console.WriteLine($"Error en filtro: {ex.Message}");
            }
        }

        // OPTIMIZACIÓN: Método separado para actualizar contador
        private void ActualizarContador()
        {
            if (lblContador != null && productosTable != null)
            {
                lblContador.Text = $"📊 Registros: {productosTable.DefaultView.Count:N0} de {productosTable.Rows.Count:N0}";
            }
        }

        // OPTIMIZACIÓN: Formato de stock más eficiente
        private void AplicarFormatoStock()
        {
            if (GrillaProductos?.Rows == null) return;

            // OPTIMIZACIÓN: Cache de colores y fuentes
            var stockBajoRowColor = Color.FromArgb(255, 235, 238);
            var stockBajoRowForeColor = Color.FromArgb(139, 0, 0);
            var stockBajoCellColor = Color.FromArgb(255, 199, 206);
            var stockBajoCellForeColor = Color.FromArgb(183, 28, 28);
            var stockLimitadoCellColor = Color.FromArgb(255, 248, 225);
            var stockLimitadoCellForeColor = Color.FromArgb(255, 111, 0);

            var normalRowColor = Color.White;
            var normalForeColor = Color.FromArgb(62, 80, 100);

            foreach (DataGridViewRow row in GrillaProductos.Rows)
            {
                var cantidadCell = row.Cells["cantidad"];
                if (cantidadCell?.Value != null && decimal.TryParse(cantidadCell.Value.ToString(), out decimal stock))
                {
                    if (stock <= 5)
                    {
                        // Stock bajo - aplicar formato completo
                        row.DefaultCellStyle.BackColor = stockBajoRowColor;
                        row.DefaultCellStyle.ForeColor = stockBajoRowForeColor;
                        cantidadCell.Style.BackColor = stockBajoCellColor;
                        cantidadCell.Style.ForeColor = stockBajoCellForeColor;
                        cantidadCell.Style.Font = _boldFont;
                    }
                    else if (stock <= 10)
                    {
                        // Stock limitado - solo celda
                        row.DefaultCellStyle.BackColor = normalRowColor;
                        row.DefaultCellStyle.ForeColor = normalForeColor;
                        cantidadCell.Style.BackColor = stockLimitadoCellColor;
                        cantidadCell.Style.ForeColor = stockLimitadoCellForeColor;
                        cantidadCell.Style.Font = _boldFont;
                    }
                    else
                    {
                        // Stock normal - resetear
                        row.DefaultCellStyle.BackColor = normalRowColor;
                        row.DefaultCellStyle.ForeColor = normalForeColor;
                        cantidadCell.Style.BackColor = normalRowColor;
                        cantidadCell.Style.ForeColor = normalRowColor;
                        cantidadCell.Style.Font = _normalFont;
                    }
                }
            }
        }

        private void TxtFiltroDescripcion_TextChanged(object sender, EventArgs e)
        {
            searchTimer?.Stop();
            searchTimer?.Start();
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer?.Stop();
            string currentText = txtFiltroDescripcion?.Text ?? "";
            if (currentText != lastSearchText)
            {
                lastSearchText = currentText;
                AplicarFiltroDescripcion(currentText);
            }
        }

        private void TxtFiltroDescripcion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                txtFiltroDescripcion?.Clear();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (GrillaProductos?.Rows?.Count > 0)
                {
                    GrillaProductos.Focus();
                    GrillaProductos.Rows[0].Selected = true;
                }
            }
        }

        private void GrillaProductos_SelectionChanged(object sender, EventArgs e)
        {
            // Mantener simple el título del formulario
        }

        private void GrillaProductos_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                BtnModificarProducto_Click(sender, e);
            }
        }

        // OPTIMIZACIÓN: Estilo moderno más eficiente
        private void AplicarEstiloModerno(Form formulario)
        {
            formulario.SuspendLayout();

            try
            {
                formulario.Font = _normalFont;
                formulario.BackColor = Color.FromArgb(245, 248, 250);
                formulario.FormBorderStyle = FormBorderStyle.FixedDialog;
                formulario.MaximizeBox = false;

                AplicarEstiloControlRecursivo(formulario);
                ReorganizarControlesFormulario(formulario);
            }
            finally
            {
                formulario.ResumeLayout();
            }
        }

        // OPTIMIZACIÓN: Estilo recursivo más eficiente
        private void AplicarEstiloControlRecursivo(Control container)
        {
            foreach (Control control in container.Controls)
            {
                switch (control)
                {
                    case TextBox textBox:
                        textBox.Font = _textBoxFont;
                        textBox.BackColor = Color.FromArgb(250, 252, 254);
                        textBox.ForeColor = Color.FromArgb(62, 80, 100);
                        textBox.BorderStyle = BorderStyle.FixedSingle;
                        textBox.Height = 25;
                        break;
                        
                    case Label label:
                        label.Font = _boldFont;
                        label.ForeColor = Color.FromArgb(62, 80, 100);
                        label.Height = 20;
                        label.AutoSize = false;
                        label.TextAlign = ContentAlignment.MiddleLeft;
                        break;
                        
                    case Button button:
                        button.Font = _headerFont;
                        button.FlatStyle = FlatStyle.Flat;
                        button.FlatAppearance.BorderSize = 0;
                        button.Cursor = Cursors.Hand;
                        button.Height = 35;

                        if (button.Text.Contains("Guardar") || button.Text.Contains("Aceptar"))
                        {
                            button.BackColor = Color.FromArgb(76, 175, 80);
                            button.ForeColor = Color.White;
                        }
                        else if (button.Text.Contains("Cancelar") || button.Text.Contains("Salir"))
                        {
                            button.BackColor = Color.FromArgb(158, 158, 158);
                            button.ForeColor = Color.White;
                        }
                        else
                        {
                            button.BackColor = Color.FromArgb(96, 125, 139);
                            button.ForeColor = Color.White;
                        }

                        AplicarHoverButton(button);
                        break;
                        
                    case Panel panel:
                        panel.BackColor = Color.White;
                        AplicarEstiloControlRecursivo(panel); // Recursión para paneles
                        break;
                        
                    default:
                        if (control.HasChildren)
                        {
                            AplicarEstiloControlRecursivo(control); // Recursión para otros contenedores
                        }
                        break;
                }
            }
        }

        private void ReorganizarControlesFormulario(Form formulario)
        {
            try
            {
                var ordenCampos = new[] {
                    "codigo", "descripcion", "rubro", "marca",
                    "costo", "porcentaje", "precio", "cantidad", "proveedor"
                };

                var labels = formulario.Controls.OfType<Label>().ToList();
                var textBoxes = formulario.Controls.OfType<TextBox>().ToList();
                var buttons = formulario.Controls.OfType<Button>().ToList();

                int yPosition = 20;
                const int labelHeight = 20;
                const int textBoxHeight = 25;
                const int spacing = 8;
                const int margin = 20;

                foreach (var campo in ordenCampos)
                {
                    var textBox = textBoxes.FirstOrDefault(tb =>
                        tb.Name.ToLower().Contains(campo.ToLower()));
                    var label = labels.FirstOrDefault(lbl =>
                        lbl.Text.ToLower().Replace(":", "").Trim().Contains(campo.ToLower()) ||
                        campo.ToLower().Contains(lbl.Text.ToLower().Replace(":", "").Trim()));

                    if (textBox != null && label != null)
                    {
                        label.Location = new Point(margin, yPosition);
                        label.Size = new Size(100, labelHeight);

                        yPosition += labelHeight + 2;
                        textBox.Location = new Point(margin, yPosition);
                        textBox.Size = new Size(320, textBoxHeight);

                        yPosition += textBoxHeight + spacing;
                    }
                }

                var btnGuardar = buttons.FirstOrDefault(b =>
                    b.Text.Contains("Guardar") || b.Text.Contains("Aceptar"));
                var btnCancelar = buttons.FirstOrDefault(b =>
                    b.Text.Contains("Cancelar") || b.Text.Contains("Salir"));

                if (btnGuardar != null)
                {
                    btnGuardar.Location = new Point(margin + 120, yPosition + 10);
                    btnGuardar.Size = new Size(100, 35);
                }

                if (btnCancelar != null && btnGuardar != null)
                {
                    btnCancelar.Location = new Point(btnGuardar.Right + 10, btnGuardar.Top);
                    btnCancelar.Size = new Size(100, 35);
                }

                int formHeight = yPosition + 100;
                int formWidth = Math.Max(400, margin + 320 + margin);

                formulario.Size = new Size(formWidth, formHeight);
                formulario.MinimumSize = new Size(formWidth, formHeight);
            }
            catch (Exception)
            {
                // Continuar sin cambios si hay error
            }
        }

        private void BtnAgregarProducto_Click(object sender, EventArgs e)
        {
            try
            {
                using (var form = new frmAgregarProducto())
                {
                    // CAMBIO: Configurar para centrar respecto al padre
                    form.StartPosition = FormStartPosition.CenterParent;
                    
                    form.Load += (s, args) => 
                    {
                        AplicarEstiloModerno(form);
                        // Opcional: aplicar centrado personalizado si es necesario
                        // CentrarFormulario(form);
                    };

                    var result = form.ShowDialog(this); // IMPORTANTE: pasar 'this' como padre
                    if (result == DialogResult.OK)
                    {
                        CargarProductos();
                        
                        try
                        {
                            var codigoProperty = form.GetType().GetProperty("CodigoAgregado");
                            if (codigoProperty != null)
                            {
                                var codigo = codigoProperty.GetValue(form)?.ToString();
                                if (!string.IsNullOrEmpty(codigo))
                                {
                                    SeleccionarProducto(codigo);
                                }
                            }
                        }
                        catch
                        {
                            // Continuar sin seleccionar
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir formulario de agregar: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnModificarProducto_Click(object sender, EventArgs e)
        {
            if (GrillaProductos?.CurrentRow == null)
            {
                MessageBox.Show("Seleccione un producto para modificar.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var row = GrillaProductos.CurrentRow;
                using (var form = new frmAgregarProducto())
                {
                    // CAMBIO: Configurar para centrar respecto al padre
                    form.StartPosition = FormStartPosition.CenterParent;
                    
                    form.Load += (s, args) =>
                    {
                        AplicarEstiloModerno(form);
                        CargarDatosEnFormulario(form, row);
                        // Opcional: aplicar centrado personalizado si es necesario
                        // CentrarFormulario(form);
                    };

                    var result = form.ShowDialog(this); // IMPORTANTE: pasar 'this' como padre
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir formulario de modificar: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarDatosEnFormulario(Form form, DataGridViewRow row)
        {
            try
            {
                var textBoxes = form.Controls.OfType<TextBox>().ToList();

                foreach (var textBox in textBoxes)
                {
                    string campo = textBox.Name.Replace("txt", "").ToLower();
                    if (row.Cells[campo] != null)
                    {
                        textBox.Text = row.Cells[campo]?.Value?.ToString() ?? "";
                    }
                }
            }
            catch
            {
                // Continuar sin cargar datos
            }
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (GrillaProductos?.CurrentRow == null)
            {
                MessageBox.Show("Seleccione un producto para eliminar.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var codigo = GrillaProductos.CurrentRow.Cells["codigo"]?.Value?.ToString();
            var descripcion = GrillaProductos.CurrentRow.Cells["descripcion"]?.Value?.ToString();

            var result = MessageBox.Show(
                $"¿Está seguro que desea eliminar el producto?\n\n" +
                $"Código: {codigo}\n" +
                $"Descripción: {descripcion}\n\n" +
                $"⚠️ Esta acción no se puede deshacer.",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // NUEVO: Mostrar indicador durante eliminación
                    MostrarCarga("🗑️ Eliminando producto...");

                    var config = new ConfigurationBuilder()
                        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                        .AddJsonFile("appsettings.json")
                        .Build();

                    string connectionString = config.GetConnectionString("DefaultConnection");

                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        var query = "DELETE FROM Productos WHERE codigo = @codigo";
                        using (var cmd = new SqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@codigo", codigo);
                            int affected = cmd.ExecuteNonQuery();

                            if (affected > 0)
                            {
                                OcultarCarga();
                                MessageBox.Show("✅ Producto eliminado correctamente.", "Éxito",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                                CargarProductos(); // Ya incluye indicadores de carga
                                txtFiltroDescripcion?.Focus();
                            }
                            else
                            {
                                OcultarCarga();
                                MessageBox.Show("❌ No se pudo eliminar el producto.", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    OcultarCarga();
                    MessageBox.Show($"❌ Error al eliminar el producto: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnRefrescar_Click(object sender, EventArgs e)
        {
            CargarProductos(); // Ya incluye indicadores de carga
            txtFiltroDescripcion?.Clear();
            txtFiltroDescripcion?.Focus();
            this.Text = "Gestión de Productos";
        }

        private void SeleccionarProducto(string codigo)
        {
            if (GrillaProductos?.Rows == null) return;

            foreach (DataGridViewRow row in GrillaProductos.Rows)
            {
                var cellValue = row.Cells["codigo"]?.Value?.ToString();
                if (cellValue == codigo)
                {
                    row.Selected = true;
                    GrillaProductos.CurrentCell = row.Cells["codigo"];

                    // Centrar la fila seleccionada en la vista
                    var firstDisplayed = Math.Max(0, row.Index - (GrillaProductos.DisplayedRowCount(false) / 2));
                    if (firstDisplayed < GrillaProductos.Rows.Count)
                    {
                        GrillaProductos.FirstDisplayedScrollingRowIndex = firstDisplayed;
                    }
                    break;
                }
            }   
        }

        // NUEVO: Método para actualizar el progreso
        private void ActualizarProgreso(int porcentaje, string mensaje = null)
        {
            this.Invoke((Action)(() =>
            {
                if (progressBarCarga != null)
                {
                    progressBarCarga.Value = Math.Min(100, Math.Max(0, porcentaje));
                }
                
                if (!string.IsNullOrEmpty(mensaje) && lblCargando != null)
                {
                    lblCargando.Text = mensaje;
                    // Reposicionar el label después de cambiar texto
                    lblCargando.Location = new Point(
                        panelCarga.Width / 2 - lblCargando.Width / 2, 
                        panelCarga.Height / 2 - 10
                    );
                }
            }));
        }

        // NUEVO: Método para simular progreso en operaciones rápidas
        private async Task SimularProgresoRapido()
        {
            for (int i = 40; i <= 70; i += 5)
            {
                ActualizarProgreso(i);
                await Task.Delay(50); // 50ms entre actualizaciones
            }
        }

        // CAMBIO: Mejorar el redimensionamiento para usar todo el espacio
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (isInitialized && GrillaProductos?.Columns?.Count > 0)
            {
                // NUEVO: Reconfigurar columnas al redimensionar para usar todo el ancho
                ConfigurarColumnas();
            }
        }

        // NUEVO: Método helper para centrar formularios
        private void CentrarFormulario(Form formulario)
        {
            // Obtener el área de trabajo de la pantalla primaria
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            
            // Calcular posición centrada
            int x = workingArea.X + (workingArea.Width - formulario.Width) / 2;
            int y = workingArea.Y + (workingArea.Height - formulario.Height) / 2;
            
            // Asegurar que el formulario esté dentro de los límites de la pantalla
            x = Math.Max(workingArea.X, Math.Min(x, workingArea.Right - formulario.Width));
            y = Math.Max(workingArea.Y, Math.Min(y, workingArea.Bottom - formulario.Height));
            
            formulario.Location = new Point(x, y);
        }

        private void btnAbrirActualizar_Click(object sender, EventArgs e)
        {
            using (var frm = new frmActualizarProducto())
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    // Aquí puedes recargar los datos del producto o actualizar la interfaz.
                }
            }
        }

        // NUEVO: Método para recargar productos desde otros formularios
        public void RefrescarProductos()
        {
            // Llama al método que recarga los productos (por ejemplo, CargarProductos o un método similar)
            CargarProductos();
            txtFiltroDescripcion?.Clear();
            txtFiltroDescripcion?.Focus();
            this.Text = "Gestión de Productos";
        }
    }
}
