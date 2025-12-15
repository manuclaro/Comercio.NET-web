using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;

namespace Comercio.NET.Formularios
{
    public partial class ActualizacionMasivaForm : Form
    {
        private ComboBox cmbFiltroTipo;
        private ComboBox cmbValorFiltro;
        private TextBox txtFiltroDescripcion; // NUEVO: Para búsqueda por descripción
        private CheckBox chkAplicarFiltro;
        
        private RadioButton rbPorcentaje;
        private RadioButton rbPorcentajeGanancia;
        private RadioButton rbValorFijo;
        private RadioButton rbPrecioFinal;
        
        private TextBox txtValorActualizacion;
        private Label lblUnidadActualizacion;
        
        private CheckBox chkActualizarCosto;
        private CheckBox chkActualizarPorcentaje;
        private CheckBox chkActualizarPrecio;
        
        private DataGridView dgvVistaPrevia;
        private Label lblContador;
        private Label lblMensaje;
        
        private Button btnCargarPreview;
        private Button btnAplicarCambios;
        private Button btnCancelar;
        private Button btnLimpiar;
        private Button btnPrecargarFiltro; // NUEVO: Botón para precargar productos filtrados
        
        private DataTable dtProductosAfectados;
        private bool cambiosAplicados = false;

        public ActualizacionMasivaForm()
        {
            InitializeComponent();
            ConfigurarFormulario();
            _ = CargarDatosFiltros();
        }

        private void ConfigurarFormulario()
        {
            this.Text = "⚡ Actualización Masiva de Precios";

            // ✅ REDUCIDO: Altura del formulario
            var screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
            var formHeight = Math.Min(screenHeight - 100, 700); // REDUCIDO: De 850 a 720

            this.Size = new Size(1100, formHeight);
            this.MinimumSize = new Size(900, 600); // REDUCIDO: De 650 a 600
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = new Font("Segoe UI", 9F);

            // ✅ IMPORTANTE: Habilitar scroll automático
            this.AutoScroll = true;
            this.AutoScrollMinSize = new Size(1160, 600); // REDUCIDO: De 720 a 600

            // ✅ Permitir maximizar
            this.MaximizeBox = true;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            CrearControles();
            ConfigurarEventos();
        }

        private void CrearControles()
        {
            int margin = 20;
            int currentY = 15;
            int panelWidth = this.ClientSize.Width - (margin * 2);

            // === TÍTULO ===
            var lblTitulo = new Label
            {
                Text = "⚡ ACTUALIZACIÓN MASIVA DE PRECIOS",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(margin, currentY),
                Size = new Size(panelWidth, 35),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblTitulo);
            currentY += 50;

            // === PANEL DE FILTROS ===
            var panelFiltros = CrearPanelFiltros(margin, currentY, panelWidth);
            this.Controls.Add(panelFiltros);
            currentY += panelFiltros.Height + 15;

            // === PANEL DE CRITERIOS DE ACTUALIZACIÓN ===
            var panelCriterios = CrearPanelCriterios(margin, currentY, panelWidth);
            this.Controls.Add(panelCriterios);
            currentY += panelCriterios.Height + 15;

            // === PANEL DE VISTA PREVIA ===
            var panelPreview = CrearPanelVistaPrevia(margin, currentY, panelWidth);
            this.Controls.Add(panelPreview);
            currentY += panelPreview.Height + 15;

            // === MENSAJE DE ESTADO ===
            lblMensaje = new Label
            {
                Location = new Point(margin, currentY),
                Size = new Size(panelWidth, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.Blue,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(lblMensaje);
        }

        private Panel CrearPanelFiltros(int x, int y, int ancho)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ancho, 120), // AUMENTADO: De 90 a 120 para el nuevo botón
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Título del panel
            var lblTituloPan = new Label
            {
                Text = "🔍 FILTRAR PRODUCTOS",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(15, 5),
                Size = new Size(250, 25)
            };
            panel.Controls.Add(lblTituloPan);

            // Checkbox para activar filtro
            chkAplicarFiltro = new CheckBox
            {
                Text = "Aplicar filtro de selección",
                Location = new Point(15, 35),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Checked = false
            };
            panel.Controls.Add(chkAplicarFiltro);

            // Tipo de filtro
            panel.Controls.Add(new Label
            {
                Text = "Filtrar por:",
                Location = new Point(230, 35),
                Size = new Size(75, 25),
                TextAlign = ContentAlignment.MiddleLeft
            });

            cmbFiltroTipo = new ComboBox
            {
                Location = new Point(310, 33),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F),
                Enabled = false
            };
            cmbFiltroTipo.Items.AddRange(new object[] { "Descripción", "Rubro", "Marca", "Proveedor" });
            cmbFiltroTipo.SelectedIndex = 0;
            panel.Controls.Add(cmbFiltroTipo);

            // Valor del filtro
            panel.Controls.Add(new Label
            {
                Text = "Valor:",
                Location = new Point(480, 35),
                Size = new Size(50, 25),
                TextAlign = ContentAlignment.MiddleLeft
            });

            cmbValorFiltro = new ComboBox
            {
                Location = new Point(535, 33),
                Size = new Size(250, 25),
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                Font = new Font("Segoe UI", 9F),
                Enabled = false,
                Visible = true
            };
            panel.Controls.Add(cmbValorFiltro);

            // TextBox para búsqueda por descripción
            txtFiltroDescripcion = new TextBox
            {
                Location = new Point(535, 33),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Escribe parte de la descripción...",
                Enabled = false,
                Visible = false
            };
            panel.Controls.Add(txtFiltroDescripcion);

            // NUEVO: Botón para precargar productos filtrados
            btnPrecargarFiltro = new Button
            {
                Text = "👁️ Ver Productos",
                Location = new Point(805, 31),
                Size = new Size(130, 30),
                BackColor = Color.FromArgb(156, 39, 176), // Color púrpura
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnPrecargarFiltro.FlatAppearance.BorderSize = 0;
            panel.Controls.Add(btnPrecargarFiltro);

            // Label informativo para búsqueda por descripción
            var lblInfoDescripcion = new Label
            {
                Text = "💡 La búsqueda encuentra productos que contengan el texto en cualquier parte de la descripción",
                Location = new Point(15, 68),
                Size = new Size(ancho - 30, 16),
                Font = new Font("Segoe UI", 7F, FontStyle.Italic),
                ForeColor = Color.FromArgb(33, 150, 243),
                Visible = false
            };
            panel.Controls.Add(lblInfoDescripcion);

            // NUEVO: Label informativo para el botón de precarga
            var lblInfoPrecarga = new Label
            {
                Text = "💡 Usa 'Ver Productos' para verificar qué productos serán afectados por los filtros antes de calcular",
                Location = new Point(15, 90),
                Size = new Size(ancho - 30, 20),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(156, 39, 176),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(lblInfoPrecarga);

            return panel;
        }

        private Panel CrearPanelCriterios(int x, int y, int ancho)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ancho, 165),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Título del panel
            var lblTitulo = new Label
            {
                Text = "⚙️ CRITERIO DE ACTUALIZACIÓN",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(15, 8),
                Size = new Size(350, 25)
            };
            panel.Controls.Add(lblTitulo);

            // Panel para valor de actualización (arriba a la derecha)
            var panelValor = new Panel
            {
                Location = new Point(500, 8),
                Size = new Size(ancho - 530, 90),
                BackColor = Color.FromArgb(240, 245, 250),
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(panelValor);

            panelValor.Controls.Add(new Label
            {
                Text = "Valor de actualización:",
                Location = new Point(10, 10),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            txtValorActualizacion = new TextBox
            {
                Location = new Point(10, 35),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Right,
                PlaceholderText = "0.00"
            };
            panelValor.Controls.Add(txtValorActualizacion);

            lblUnidadActualizacion = new Label
            {
                Text = "%",
                Location = new Point(170, 35),
                Size = new Size(30, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelValor.Controls.Add(lblUnidadActualizacion);

            // Botón "Cargar Vista Previa" dentro del panel de valor
            btnCargarPreview = new Button
            {
                Text = "👁️ Vista Previa",
                Location = new Point(210, 33),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCargarPreview.FlatAppearance.BorderSize = 0;
            panelValor.Controls.Add(btnCargarPreview);

            var lblEjemplo = new Label
            {
                Text = "Ej: 10 = +10%  |  -5 = -5%",
                Location = new Point(10, 65),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 7F, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            panelValor.Controls.Add(lblEjemplo);

            // Radio buttons para tipo de actualización
            int radioY = 40;

            rbPorcentaje = new RadioButton
            {
                Text = "🔼 Incremento/Decremento por porcentaje sobre precio actual",
                Location = new Point(15, radioY),
                Size = new Size(470, 17),
                Checked = true,
                Font = new Font("Segoe UI", 8F)
            };
            panel.Controls.Add(rbPorcentaje);
            radioY += 19;

            rbPorcentajeGanancia = new RadioButton
            {
                Text = "📊 Actualizar porcentaje de ganancia (recalcula precio desde costo)",
                Location = new Point(15, radioY),
                Size = new Size(470, 17),
                Font = new Font("Segoe UI", 8F)
            };
            panel.Controls.Add(rbPorcentajeGanancia);
            radioY += 19;

            rbValorFijo = new RadioButton
            {
                Text = "💰 Incremento/Decremento por valor fijo en pesos",
                Location = new Point(15, radioY),
                Size = new Size(470, 17),
                Font = new Font("Segoe UI", 8F)
            };
            panel.Controls.Add(rbValorFijo);
            radioY += 19;

            rbPrecioFinal = new RadioButton
            {
                Text = "🎯 Establecer precio final específico",
                Location = new Point(15, radioY),
                Size = new Size(470, 17),
                Font = new Font("Segoe UI", 8F)
            };
            panel.Controls.Add(rbPrecioFinal);

            // Checkboxes para qué actualizar
            var lblQueActualizar = new Label
            {
                Text = "¿Qué actualizar?",
                Location = new Point(15, 135),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            panel.Controls.Add(lblQueActualizar);

            chkActualizarCosto = new CheckBox
            {
                Text = "💵 Costo",
                Location = new Point(150, 135),
                Size = new Size(100, 20),
                Checked = false
            };
            panel.Controls.Add(chkActualizarCosto);

            chkActualizarPorcentaje = new CheckBox
            {
                Text = "📊 % Ganancia",
                Location = new Point(270, 135),
                Size = new Size(120, 20),
                Checked = false
            };
            panel.Controls.Add(chkActualizarPorcentaje);

            chkActualizarPrecio = new CheckBox
            {
                Text = "💰 Precio Venta",
                Location = new Point(410, 135),
                Size = new Size(130, 20),
                Checked = true
            };
            panel.Controls.Add(chkActualizarPrecio);

            // Botones de acción a la derecha de los checkboxes
            int btnY = 110;
            int btnX = 560;
            int btnWidth = 140;
            int btnSpacing = 10;

            btnLimpiar = new Button
            {
                Text = "🧹 Limpiar",
                Location = new Point(btnX, btnY),
                Size = new Size(btnWidth, 35),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLimpiar.FlatAppearance.BorderSize = 0;
            panel.Controls.Add(btnLimpiar);

            btnAplicarCambios = new Button
            {
                Text = "✅ Aplicar Cambios",
                Location = new Point(btnX + btnWidth + btnSpacing, btnY),
                Size = new Size(btnWidth, 35),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnAplicarCambios.FlatAppearance.BorderSize = 0;
            panel.Controls.Add(btnAplicarCambios);

            btnCancelar = new Button
            {
                Text = "❌ Cancelar",
                Location = new Point(btnX + (btnWidth + btnSpacing) * 2, btnY),
                Size = new Size(btnWidth, 35),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            panel.Controls.Add(btnCancelar);

            return panel;
        }

        private Panel CrearPanelVistaPrevia(int x, int y, int ancho)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ancho, 220), // AUMENTADO: De 250 a 280
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // Título del panel con contador
            var lblTitulo = new Label
            {
                Text = "👁️ VISTA PREVIA DE CAMBIOS",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(15, 10),
                Size = new Size(300, 25)
            };
            panel.Controls.Add(lblTitulo);

            lblContador = new Label
            {
                Text = "0 productos afectados",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 152, 0),
                Location = new Point(ancho - 220, 10),
                Size = new Size(200, 25),
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            panel.Controls.Add(lblContador);

            // DataGridView
            dgvVistaPrevia = new DataGridView
            {
                Location = new Point(15, 40),
                Size = new Size(ancho - 30, 170), // AUMENTADO: De 195 a 225
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // Configurar columnas
            ConfigurarColumnasGrid();

            panel.Controls.Add(dgvVistaPrevia);

            return panel;
        }

        private void ConfigurarColumnasGrid()
        {
            dgvVistaPrevia.Columns.Clear();

            dgvVistaPrevia.Columns.Add("codigo", "Código");
            dgvVistaPrevia.Columns.Add("descripcion", "Descripción");
            dgvVistaPrevia.Columns.Add("marca", "Marca");
            // REMOVIDO: dgvVistaPrevia.Columns.Add("rubro", "Rubro");
            dgvVistaPrevia.Columns.Add("costo_actual", "Costo Actual");
            dgvVistaPrevia.Columns.Add("porcentaje_actual", "% Actual");
            dgvVistaPrevia.Columns.Add("precio_actual", "Precio Actual");
            dgvVistaPrevia.Columns.Add("costo_nuevo", "Costo Nuevo");
            dgvVistaPrevia.Columns.Add("porcentaje_nuevo", "% Nuevo");
            dgvVistaPrevia.Columns.Add("precio_nuevo", "Precio Nuevo");
            dgvVistaPrevia.Columns.Add("diferencia", "Diferencia");

            // Configurar anchos y formato
            dgvVistaPrevia.Columns["codigo"].Width = 100; // REDUCIDO: De 120 a 100
            dgvVistaPrevia.Columns["descripcion"].Width = 220; // REDUCIDO: De 280 a 220
            dgvVistaPrevia.Columns["marca"].Width = 90; // REDUCIDO: De 100 a 90
            dgvVistaPrevia.Columns["costo_actual"].Width = 85; // REDUCIDO: De 90 a 85
            dgvVistaPrevia.Columns["porcentaje_actual"].Width = 65; // REDUCIDO: De 70 a 65
            dgvVistaPrevia.Columns["precio_actual"].Width = 85; // REDUCIDO: De 90 a 85
            dgvVistaPrevia.Columns["costo_nuevo"].Width = 85; // REDUCIDO: De 90 a 85
            dgvVistaPrevia.Columns["porcentaje_nuevo"].Width = 65; // REDUCIDO: De 70 a 65
            dgvVistaPrevia.Columns["precio_nuevo"].Width = 85; // REDUCIDO: De 90 a 85
            dgvVistaPrevia.Columns["diferencia"].Width = 85; // REDUCIDO: De 90 a 85

            // Formato de moneda para columnas
            var columnasMoneda = new[] { "costo_actual", "precio_actual", "costo_nuevo", "precio_nuevo", "diferencia" };
            foreach (var col in columnasMoneda)
            {
                dgvVistaPrevia.Columns[col].DefaultCellStyle.Format = "C2";
                dgvVistaPrevia.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // Formato de porcentaje
            dgvVistaPrevia.Columns["porcentaje_actual"].DefaultCellStyle.Format = "N2";
            dgvVistaPrevia.Columns["porcentaje_actual"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvVistaPrevia.Columns["porcentaje_nuevo"].DefaultCellStyle.Format = "N2";
            dgvVistaPrevia.Columns["porcentaje_nuevo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Colores para destacar cambios
            dgvVistaPrevia.Columns["costo_nuevo"].DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 225);
            dgvVistaPrevia.Columns["porcentaje_nuevo"].DefaultCellStyle.BackColor = Color.FromArgb(232, 245, 233);
            dgvVistaPrevia.Columns["precio_nuevo"].DefaultCellStyle.BackColor = Color.FromArgb(232, 245, 233);
            dgvVistaPrevia.Columns["precio_nuevo"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvVistaPrevia.Columns["diferencia"].DefaultCellStyle.BackColor = Color.FromArgb(227, 242, 253);
            dgvVistaPrevia.Columns["diferencia"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        }

        private void ConfigurarEventos()
        {
            // Eventos de checkboxes y radio buttons
            chkAplicarFiltro.CheckedChanged += (s, e) =>
            {
                cmbFiltroTipo.Enabled = chkAplicarFiltro.Checked;
                btnPrecargarFiltro.Enabled = chkAplicarFiltro.Checked; // NUEVO: Habilitar botón con filtro
                ActualizarVisibilidadControlesFiltro();
            };

            cmbFiltroTipo.SelectedIndexChanged += (s, e) =>
            {
                ActualizarVisibilidadControlesFiltro();
                if (cmbFiltroTipo.SelectedItem?.ToString() != "Descripción")
                {
                    _ = CargarValoresFiltro();
                }
            };

            // Evento para el TextBox de descripción
            txtFiltroDescripcion.TextChanged += (s, e) =>
            {
                var panel = txtFiltroDescripcion.Parent;
                var lblInfo = panel.Controls.OfType<Label>()
                    .FirstOrDefault(l => l.ForeColor == Color.FromArgb(33, 150, 243));
                
                if (lblInfo != null)
                {
                    lblInfo.Visible = !string.IsNullOrWhiteSpace(txtFiltroDescripcion.Text);
                }
            };

            // NUEVO: Evento del botón de precarga
            btnPrecargarFiltro.Click += async (s, e) => await PrecargarProductosFiltrados();

            var radioButtons = new[] { rbPorcentaje, rbPorcentajeGanancia, rbValorFijo, rbPrecioFinal };
            foreach (var rb in radioButtons)
            {
                rb.CheckedChanged += ActualizarUnidadActualizacion;
            }

            txtValorActualizacion.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && 
                    e.KeyChar != '.' && e.KeyChar != ',' && e.KeyChar != '-')
                {
                    e.Handled = true;
                }

                if (e.KeyChar == '-' && txtValorActualizacion.Text.Contains("-"))
                {
                    e.Handled = true;
                }
            };

            // Eventos de botones
            btnCargarPreview.Click += async (s, e) => await CargarVistaPrevia();
            btnLimpiar.Click += LimpiarFormulario;
            btnAplicarCambios.Click += async (s, e) => await AplicarCambios();
            btnCancelar.Click += (s, e) => 
            {
                if (cambiosAplicados)
                {
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                }
                this.Close();
            };

            // Evento de cierre del formulario
            this.FormClosing += (s, e) =>
            {
                if (dgvVistaPrevia.Rows.Count > 0 && !cambiosAplicados && btnAplicarCambios.Enabled)
                {
                    var result = MessageBox.Show(
                        "Tienes cambios en la vista previa sin aplicar.\n\n¿Estás seguro de que deseas salir?",
                        "Cambios sin Aplicar",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                    {
                        e.Cancel = true;
                    }
                }
            };
        }

        // NUEVO: Método para actualizar visibilidad de controles según tipo de filtro
        private void ActualizarVisibilidadControlesFiltro()
        {
            bool filtroActivo = chkAplicarFiltro.Checked;
            bool esDescripcion = cmbFiltroTipo.SelectedItem?.ToString() == "Descripción";

            // Mostrar/ocultar controles según el tipo de filtro
            if (filtroActivo && esDescripcion)
            {
                cmbValorFiltro.Visible = false;
                cmbValorFiltro.Enabled = false;
                txtFiltroDescripcion.Visible = true;
                txtFiltroDescripcion.Enabled = true;
                txtFiltroDescripcion.Focus();
                
                // Mostrar label informativo
                var panel = txtFiltroDescripcion.Parent;
                var lblInfo = panel.Controls.OfType<Label>()
                    .FirstOrDefault(l => l.ForeColor == Color.FromArgb(33, 150, 243));
                if (lblInfo != null)
                {
                    lblInfo.Visible = true;
                }
            }
            else
            {
                cmbValorFiltro.Visible = filtroActivo;
                cmbValorFiltro.Enabled = filtroActivo;
                txtFiltroDescripcion.Visible = false;
                txtFiltroDescripcion.Enabled = false;
                
                // Ocultar label informativo
                var panel = txtFiltroDescripcion.Parent;
                var lblInfo = panel.Controls.OfType<Label>()
                    .FirstOrDefault(l => l.ForeColor == Color.FromArgb(33, 150, 243));
                if (lblInfo != null)
                {
                    lblInfo.Visible = false;
                }
            }
        }

        private async Task CargarDatosFiltros()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Cargar rubros, marcas y proveedores para usar según el filtro seleccionado
                // Por ahora solo cargamos los rubros por defecto
                await CargarValoresFiltro();
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error cargando datos: {ex.Message}", Color.Red);
            }
        }

        private async Task CargarValoresFiltro()
        {
            if (!chkAplicarFiltro.Checked || cmbFiltroTipo.SelectedItem == null)
                return;

            // NUEVO: No cargar valores si es filtro por descripción
            string tipoFiltro = cmbFiltroTipo.SelectedItem.ToString();
            if (tipoFiltro == "Descripción")
                return;

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");
                string columna = tipoFiltro.ToLower();

                using var connection = new SqlConnection(connectionString);
                var query = $"SELECT DISTINCT {columna} FROM Productos WHERE {columna} IS NOT NULL AND {columna} <> '' ORDER BY {columna}";
                
                using var cmd = new SqlCommand(query, connection);
                await connection.OpenAsync();

                cmbValorFiltro.Items.Clear();
                using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    cmbValorFiltro.Items.Add(reader[columna].ToString());
                }

                if (cmbValorFiltro.Items.Count > 0)
                {
                    cmbValorFiltro.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error cargando filtros: {ex.Message}", Color.Red);
            }
        }

        private void ActualizarUnidadActualizacion(object sender, EventArgs e)
        {
            if (rbPorcentaje.Checked || rbPorcentajeGanancia.Checked)
            {
                lblUnidadActualizacion.Text = "%";
                txtValorActualizacion.PlaceholderText = "Ej: 10 o -5";
            }
            else if (rbValorFijo.Checked)
            {
                lblUnidadActualizacion.Text = "$";
                txtValorActualizacion.PlaceholderText = "Ej: 100 o -50";
            }
            else if (rbPrecioFinal.Checked)
            {
                lblUnidadActualizacion.Text = "$ (final)";
                txtValorActualizacion.PlaceholderText = "Ej: 1500";
            }
        }

        private async Task CargarVistaPrevia()
        {
            if (!ValidarDatos())
                return;

            try
            {
                btnCargarPreview.Enabled = false;
                btnAplicarCambios.Enabled = false;
                MostrarMensaje("⏳ Generando vista previa...", Color.Blue);

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                // Construir query con filtros - MODIFICADO
                string query = "SELECT codigo, descripcion, marca, rubro, costo, porcentaje, precio FROM Productos";

                if (chkAplicarFiltro.Checked)
                {
                    string tipoFiltro = cmbFiltroTipo.SelectedItem.ToString();

                    if (tipoFiltro == "Descripción")
                    {
                        // Filtro por descripción con LIKE
                        if (!string.IsNullOrWhiteSpace(txtFiltroDescripcion.Text))
                        {
                            query += " WHERE descripcion LIKE @valorFiltro";
                        }
                    }
                    else
                    {
                        // Filtro por rubro, marca o proveedor
                        if (cmbValorFiltro.SelectedItem != null)
                        {
                            string columna = tipoFiltro.ToLower();
                            query += $" WHERE {columna} = @valorFiltro";
                        }
                    }
                }

                using var connection = new SqlConnection(connectionString);
                using var cmd = new SqlCommand(query, connection);

                if (chkAplicarFiltro.Checked)
                {
                    string tipoFiltro = cmbFiltroTipo.SelectedItem.ToString();

                    if (tipoFiltro == "Descripción")
                    {
                        if (!string.IsNullOrWhiteSpace(txtFiltroDescripcion.Text))
                        {
                            cmd.Parameters.AddWithValue("@valorFiltro", $"%{txtFiltroDescripcion.Text.Trim()}%");
                        }
                    }
                    else
                    {
                        if (cmbValorFiltro.SelectedItem != null)
                        {
                            cmd.Parameters.AddWithValue("@valorFiltro", cmbValorFiltro.SelectedItem.ToString());
                        }
                    }
                }

                await connection.OpenAsync();

                dgvVistaPrevia.Rows.Clear();
                dtProductosAfectados = new DataTable();

                using var reader = await cmd.ExecuteReaderAsync();
                int contador = 0;

                while (reader.Read())
                {
                    string codigo = reader["codigo"].ToString();
                    string descripcion = reader["descripcion"].ToString();
                    string marca = reader["marca"].ToString();
                    string rubro = reader["rubro"].ToString();

                    // ✅ CORRECCIÓN: Conversión segura de tipos numéricos
                    decimal costoActual = ObtenerDecimalSeguro(reader, "costo");
                    decimal porcentajeActual = ObtenerDecimalSeguro(reader, "porcentaje");
                    decimal precioActual = ObtenerDecimalSeguro(reader, "precio");

                    // Calcular nuevos valores según criterio
                    var (costoNuevo, porcentajeNuevo, precioNuevo) = CalcularNuevosValores(
                        costoActual, porcentajeActual, precioActual);

                    decimal diferencia = precioNuevo - precioActual;

                    dgvVistaPrevia.Rows.Add(
                        codigo,
                        descripcion,
                        marca,
                        costoActual,
                        porcentajeActual,
                        precioActual,
                        costoNuevo,
                        porcentajeNuevo,
                        precioNuevo,
                        diferencia
                    );

                    // Aplicar colores según el cambio
                    var row = dgvVistaPrevia.Rows[dgvVistaPrevia.Rows.Count - 1];
                    if (diferencia > 0)
                    {
                        row.Cells["diferencia"].Style.ForeColor = Color.Green;
                    }
                    else if (diferencia < 0)
                    {
                        row.Cells["diferencia"].Style.ForeColor = Color.Red;
                    }

                    contador++;
                }

                lblContador.Text = $"{contador} productos afectados";
                btnAplicarCambios.Enabled = contador > 0;

                if (contador > 0)
                {
                    MostrarMensaje($"✅ Vista previa generada: {contador} productos listos para actualizar", Color.Green);
                }
                else
                {
                    MostrarMensaje("⚠️ No se encontraron productos con los filtros aplicados", Color.Orange);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error generando vista previa: {ex.Message}", Color.Red);
                MessageBox.Show($"Error al generar vista previa:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnCargarPreview.Enabled = true;
            }
        }

        private (decimal costoNuevo, decimal porcentajeNuevo, decimal precioNuevo) CalcularNuevosValores(
            decimal costoActual, decimal porcentajeActual, decimal precioActual)
        {
            decimal valor = decimal.Parse(txtValorActualizacion.Text.Replace(",", "."), 
                CultureInfo.InvariantCulture);

            decimal costoNuevo = costoActual;
            decimal porcentajeNuevo = porcentajeActual;
            decimal precioNuevo = precioActual;

            if (rbPorcentaje.Checked)
            {
                // Incremento/decremento porcentual sobre precio actual
                if (chkActualizarPrecio.Checked)
                {
                    precioNuevo = precioActual * (1 + valor / 100);
                    // Recalcular porcentaje si el costo no cambió
                    if (!chkActualizarCosto.Checked && costoActual > 0)
                    {
                        porcentajeNuevo = ((precioNuevo - costoActual) / costoActual) * 100;
                    }
                }
                
                if (chkActualizarCosto.Checked)
                {
                    costoNuevo = costoActual * (1 + valor / 100);
                    // Si no se actualizó precio, recalcularlo con el nuevo costo
                    if (!chkActualizarPrecio.Checked && chkActualizarPorcentaje.Checked)
                    {
                        precioNuevo = costoNuevo * (1 + porcentajeActual / 100);
                    }
                }
            }
            else if (rbPorcentajeGanancia.Checked)
            {
                // Establecer nuevo porcentaje de ganancia
                if (chkActualizarPorcentaje.Checked)
                {
                    porcentajeNuevo = valor;
                }
                
                if (chkActualizarPrecio.Checked && costoActual > 0)
                {
                    precioNuevo = costoActual * (1 + porcentajeNuevo / 100);
                }
            }
            else if (rbValorFijo.Checked)
            {
                // Incremento/decremento fijo en pesos
                if (chkActualizarPrecio.Checked)
                {
                    precioNuevo = precioActual + valor;
                    // Recalcular porcentaje
                    if (!chkActualizarCosto.Checked && costoActual > 0)
                    {
                        porcentajeNuevo = ((precioNuevo - costoActual) / costoActual) * 100;
                    }
                }
                
                if (chkActualizarCosto.Checked)
                {
                    costoNuevo = costoActual + valor;
                }
            }
            else if (rbPrecioFinal.Checked)
            {
                // Establecer precio final específico
                if (chkActualizarPrecio.Checked)
                {
                    precioNuevo = valor;
                    // Recalcular porcentaje
                    if (!chkActualizarCosto.Checked && costoActual > 0)
                    {
                        porcentajeNuevo = ((precioNuevo - costoActual) / costoActual) * 100;
                    }
                }
            }

            // Redondear a 2 decimales
            costoNuevo = Math.Round(costoNuevo, 2);
            porcentajeNuevo = Math.Round(porcentajeNuevo, 2);
            precioNuevo = Math.Round(precioNuevo, 2);

            return (costoNuevo, porcentajeNuevo, precioNuevo);
        }

        private async Task AplicarCambios()
        {
            if (dgvVistaPrevia.Rows.Count == 0)
            {
                MessageBox.Show("No hay cambios para aplicar. Genera primero la vista previa.",
                    "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"¿Estás seguro de que deseas actualizar {dgvVistaPrevia.Rows.Count} productos?\n\n" +
                "Esta acción modificará los precios en la base de datos y NO se puede deshacer.",
                "Confirmar Actualización Masiva",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            try
            {
                btnAplicarCambios.Enabled = false;
                btnCargarPreview.Enabled = false;
                MostrarMensaje("⏳ Aplicando cambios...", Color.Blue);

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");
                int actualizados = 0;
                int errores = 0;

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();
                
                try
                {
                    foreach (DataGridViewRow row in dgvVistaPrevia.Rows)
                    {
                        string codigo = row.Cells["codigo"].Value.ToString();
                        decimal costoNuevo = Convert.ToDecimal(row.Cells["costo_nuevo"].Value);
                        decimal porcentajeNuevo = Convert.ToDecimal(row.Cells["porcentaje_nuevo"].Value);
                        decimal precioNuevo = Convert.ToDecimal(row.Cells["precio_nuevo"].Value);

                        string query = "UPDATE Productos SET ";
                        var campos = new List<string>();
                        
                        if (chkActualizarCosto.Checked)
                            campos.Add("costo = @costo");
                        if (chkActualizarPorcentaje.Checked)
                            campos.Add("porcentaje = @porcentaje");
                        if (chkActualizarPrecio.Checked)
                            campos.Add("precio = @precio");

                        query += string.Join(", ", campos) + " WHERE codigo = @codigo";

                        using var cmd = new SqlCommand(query, connection, transaction);
                        cmd.Parameters.AddWithValue("@codigo", codigo);
                        
                        if (chkActualizarCosto.Checked)
                            cmd.Parameters.AddWithValue("@costo", costoNuevo);
                        if (chkActualizarPorcentaje.Checked)
                            cmd.Parameters.AddWithValue("@porcentaje", porcentajeNuevo);
                        if (chkActualizarPrecio.Checked)
                            cmd.Parameters.AddWithValue("@precio", precioNuevo);

                        int filasAfectadas = await cmd.ExecuteNonQueryAsync();
                        if (filasAfectadas > 0)
                            actualizados++;
                        else
                            errores++;
                    }

                    transaction.Commit();
                    cambiosAplicados = true;

                    MostrarMensaje($"✅ Actualización completada: {actualizados} productos actualizados", Color.Green);

                    MessageBox.Show(
                        $"ACTUALIZACIÓN MASIVA COMPLETADA\n\n" +
                        $"✅ Productos actualizados: {actualizados}\n" +
                        $"{(errores > 0 ? $"⚠️ Errores: {errores}\n" : "")}" +
                        $"\nLos cambios se han aplicado correctamente en la base de datos.",
                        "Actualización Exitosa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    // Limpiar cache de productos
                    ProductosOptimizado.LimpiarCache();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception($"Error en la transacción: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error aplicando cambios: {ex.Message}", Color.Red);
                MessageBox.Show($"Error al aplicar cambios:\n\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnAplicarCambios.Enabled = false;
                btnCargarPreview.Enabled = true;
            }
        }

        private void LimpiarFormulario(object sender, EventArgs e)
        {
            chkAplicarFiltro.Checked = false;
            cmbFiltroTipo.SelectedIndex = 0;
            cmbValorFiltro.Items.Clear();
            cmbValorFiltro.Text = "";
            txtFiltroDescripcion.Clear();
            
            rbPorcentaje.Checked = true;
            txtValorActualizacion.Clear();
            
            chkActualizarCosto.Checked = false;
            chkActualizarPorcentaje.Checked = false;
            chkActualizarPrecio.Checked = true;
            
            dgvVistaPrevia.Rows.Clear();
            lblContador.Text = "0 productos afectados";
            btnAplicarCambios.Enabled = false;
            btnPrecargarFiltro.Enabled = false; // NUEVO: Deshabilitar botón
            
            MostrarMensaje("🧹 Formulario limpiado", Color.Blue);
        }

        private bool ValidarDatos()
        {
            if (string.IsNullOrWhiteSpace(txtValorActualizacion.Text))
            {
                MessageBox.Show("Ingresa un valor de actualización.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtValorActualizacion.Focus();
                return false;
            }

            if (!decimal.TryParse(txtValorActualizacion.Text.Replace(",", "."), 
                NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valor))
            {
                MessageBox.Show("El valor de actualización debe ser un número válido.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtValorActualizacion.Focus();
                return false;
            }

            if (!chkActualizarCosto.Checked && !chkActualizarPorcentaje.Checked && !chkActualizarPrecio.Checked)
            {
                MessageBox.Show("Selecciona al menos un campo para actualizar (Costo, % Ganancia o Precio).",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // MODIFICADO: Validación mejorada para filtros
            if (chkAplicarFiltro.Checked)
            {
                string tipoFiltro = cmbFiltroTipo.SelectedItem?.ToString();
                
                if (tipoFiltro == "Descripción")
                {
                    if (string.IsNullOrWhiteSpace(txtFiltroDescripcion.Text))
                    {
                        MessageBox.Show("Ingresa un texto para buscar en la descripción o desmarca 'Aplicar filtro'.",
                            "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtFiltroDescripcion.Focus();
                        return false;
                    }
                }
                else
                {
                    if (cmbValorFiltro.SelectedItem == null)
                    {
                        MessageBox.Show("Selecciona un valor de filtro o desmarca 'Aplicar filtro'.",
                            "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        cmbValorFiltro.Focus();
                        return false;
                    }
                }
            }

            return true;
        }

        private void MostrarMensaje(string mensaje, Color color)
        {
            lblMensaje.Text = mensaje;
            lblMensaje.ForeColor = color;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ActualizacionMasivaForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new Size(1200, 650); // REDUCIDO: De 720 a 650
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.Name = "ActualizacionMasivaForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Actualización Masiva de Precios";
            this.ResumeLayout(false);
        }

        private decimal ObtenerDecimalSeguro(SqlDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];

                if (value == null || value == DBNull.Value)
                    return 0m;

                // Intentar conversión directa
                if (value is decimal decimalValue)
                    return decimalValue;

                // Convertir desde otros tipos numéricos
                return Convert.ToDecimal(value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error convirtiendo {columnName}: {ex.Message}");
                return 0m;
            }
        }

        // NUEVO: Método para precargar productos filtrados sin cálculos
        private async Task PrecargarProductosFiltrados()
        {
            if (!ValidarFiltros())
                return;

            try
            {
                btnPrecargarFiltro.Enabled = false;
                MostrarMensaje("⏳ Cargando productos filtrados...", Color.Blue);

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                // Construir query con filtros
                string query = "SELECT codigo, descripcion, marca, rubro, costo, porcentaje, precio FROM Productos";

                if (chkAplicarFiltro.Checked)
                {
                    string tipoFiltro = cmbFiltroTipo.SelectedItem.ToString();

                    if (tipoFiltro == "Descripción")
                    {
                        if (!string.IsNullOrWhiteSpace(txtFiltroDescripcion.Text))
                        {
                            query += " WHERE descripcion LIKE @valorFiltro";
                        }
                    }
                    else
                    {
                        if (cmbValorFiltro.SelectedItem != null)
                        {
                            string columna = tipoFiltro.ToLower();
                            query += $" WHERE {columna} = @valorFiltro";
                        }
                    }
                }

                using var connection = new SqlConnection(connectionString);
                using var cmd = new SqlCommand(query, connection);

                if (chkAplicarFiltro.Checked)
                {
                    string tipoFiltro = cmbFiltroTipo.SelectedItem.ToString();

                    if (tipoFiltro == "Descripción")
                    {
                        if (!string.IsNullOrWhiteSpace(txtFiltroDescripcion.Text))
                        {
                            cmd.Parameters.AddWithValue("@valorFiltro", $"%{txtFiltroDescripcion.Text.Trim()}%");
                        }
                    }
                    else
                    {
                        if (cmbValorFiltro.SelectedItem != null)
                        {
                            cmd.Parameters.AddWithValue("@valorFiltro", cmbValorFiltro.SelectedItem.ToString());
                        }
                    }
                }

                await connection.OpenAsync();

                dgvVistaPrevia.Rows.Clear();
                btnAplicarCambios.Enabled = false; // No permitir aplicar cambios sin cálculos

                using var reader = await cmd.ExecuteReaderAsync();
                int contador = 0;

                while (reader.Read())
                {
                    string codigo = reader["codigo"].ToString();
                    string descripcion = reader["descripcion"].ToString();
                    string marca = reader["marca"].ToString();

                    decimal costoActual = ObtenerDecimalSeguro(reader, "costo");
                    decimal porcentajeActual = ObtenerDecimalSeguro(reader, "porcentaje");
                    decimal precioActual = ObtenerDecimalSeguro(reader, "precio"); // ✅ CORREGIDO: Sin espacio

                    // Mostrar solo valores actuales (sin cálculos)
                    dgvVistaPrevia.Rows.Add(
                        codigo,
                        descripcion,
                        marca,
                        costoActual,
                        porcentajeActual,
                        precioActual,
                        "-", // Sin valor nuevo
                        "-",
                        "-",
                        "-"
                    );

                    contador++;
                }

                lblContador.Text = $"{contador} productos encontrados";

                if (contador > 0)
                {
                    MostrarMensaje($"✅ {contador} productos cargados. Ahora configura los criterios y presiona 'Cargar Vista Previa'", Color.Green);
                }
                else
                {
                    MostrarMensaje("⚠️ No se encontraron productos con los filtros aplicados", Color.Orange);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error cargando productos: {ex.Message}", Color.Red);
                MessageBox.Show($"Error al cargar productos:\n\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnPrecargarFiltro.Enabled = true;
            }
        }

        // NUEVO: Método para validar solo los filtros
        private bool ValidarFiltros()
        {
            if (!chkAplicarFiltro.Checked)
            {
                MessageBox.Show("Activa el filtro de selección para usar esta función.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            string tipoFiltro = cmbFiltroTipo.SelectedItem?.ToString();
            
            if (tipoFiltro == "Descripción")
            {
                if (string.IsNullOrWhiteSpace(txtFiltroDescripcion.Text))
                {
                    MessageBox.Show("Ingresa un texto para buscar en la descripción.",
                        "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFiltroDescripcion.Focus();
                    return false;
                }
            }
            else
            {
                if (cmbValorFiltro.SelectedItem == null)
                {
                    MessageBox.Show("Selecciona un valor de filtro.",
                        "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbValorFiltro.Focus();
                    return false;
                }
            }

            return true;
        }
    }
}