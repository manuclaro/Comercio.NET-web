using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Comercio.NET.Services;

namespace Comercio.NET.Formularios
{
    public partial class GestionOfertasForm : Form
    {
        private DataGridView dgvOfertas;
        private DataGridView dgvDetalleOferta;
        private Button btnNuevaOferta;
        private Button btnEditarOferta;
        private Button btnEliminarOferta;
        private Button btnAgregarProducto;
        private Button btnQuitarProducto;
        private Button btnGuardar;
        private Button btnCancelar;
        private TextBox txtNombreOferta;
        private TextBox txtDescripcion;
        private DateTimePicker dtpFechaInicio;
        private DateTimePicker dtpFechaFin;
        private CheckBox chkActivo;
        private ComboBox cboTipoOferta;
        private Panel panelEdicion;
        private Label lblOfertaActual;

        // ✅ NUEVO: Controles adicionales para Combo
        private TextBox txtPrecioCombo;
        private Label lblPrecioCombo;

        // ✅ NUEVO: Controles adicionales para Descuento
        private NumericUpDown nudPorcentajeDescuento;
        private Label lblPorcentajeDescuento;

        private int ofertaSeleccionadaId = 0;
        private bool modoEdicion = false;

        // ✅ NUEVO: Control para mostrar suma de productos
        private TextBox txtSumaProductos;
        private Label lblSumaProductos;

        // ✅ NUEVO: Controles para tipo PorGrupo
        private NumericUpDown nudCantidadGrupo;
        private Label lblCantidadGrupo;
        private TextBox txtPrecioGrupo;
        private Label lblPrecioGrupo;

        // ✅ NUEVO: Controles para búsqueda de ofertas
        private TextBox txtBuscarOferta;
        private Label lblBuscarOferta;
        private DataTable dtOfertasCompleto; // Copia completa para filtrar

        public GestionOfertasForm()
        {
            InitializeComponent();
            ConfigurarFormulario();
            ConfigurarControles();
            ConfigurarEventos();
            CargarOfertas();
        }

        private void InitializeComponent()
        {
            this.Text = "Gestión de Ofertas y Combos";
            this.Size = new Size(1000, 620); // ✅ MODIFICADO: formulario más alto
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
        }

        private void ConfigurarFormulario()
        {
            this.BackColor = Color.WhiteSmoke;
            this.Font = new Font("Segoe UI", 10F);

            // ✅ MODIFICADO: Panel superior más alto para acomodar buscador + grilla más grande
            var panelSuperior = new Panel
            {
                Dock = DockStyle.Top,
                Height = 255,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            var lblTituloOfertas = new Label
            {
                Text = "📋 Ofertas Registradas",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };

            // ✅ NUEVO: Buscador de ofertas
            lblBuscarOferta = new Label
            {
                Text = "🔍 Buscar:",
                Location = new Point(10, 40),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            txtBuscarOferta = new TextBox
            {
                Location = new Point(80, 37),
                Width = 300,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Filtrar por nombre de oferta..."
            };

            // ✅ MODIFICADO: Grilla más grande (más filas visibles)
            dgvOfertas = new DataGridView
            {
                Location = new Point(10, 65),
                Width = 780,
                Height = 175,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            int botonX = 800;
            int botonY = 65; // ✅ MODIFICADO: alineado con la nueva posición de la grilla
            int espacioVertical = 34;

            btnNuevaOferta = new Button
            {
                Text = "Nueva Oferta",
                Location = new Point(botonX, botonY),
                Size = new Size(170, 30),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnEditarOferta = new Button
            {
                Text = "Editar",
                Location = new Point(botonX, botonY + espacioVertical),
                Size = new Size(170, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnEliminarOferta = new Button
            {
                Text = "Eliminar",
                Location = new Point(botonX, botonY + (espacioVertical * 2)),
                Size = new Size(170, 30),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            panelSuperior.Controls.Add(lblTituloOfertas);
            panelSuperior.Controls.Add(lblBuscarOferta);
            panelSuperior.Controls.Add(txtBuscarOferta);
            panelSuperior.Controls.Add(dgvOfertas);
            panelSuperior.Controls.Add(btnNuevaOferta);
            panelSuperior.Controls.Add(btnEditarOferta);
            panelSuperior.Controls.Add(btnEliminarOferta);

            // ========================================
            // PANEL DE EDICIÓN
            // ========================================
            panelEdicion = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = false,
                Padding = new Padding(15),
                Visible = false,
                BackColor = Color.White
            };

            lblOfertaActual = new Label
            {
                Text = "📝 Nueva Oferta",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(15, 10),
                AutoSize = true,
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            // FILA 1: Nombre y Tipo
            var lblNombre = new Label
            {
                Text = "Nombre:",
                Location = new Point(15, 45),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            txtNombreOferta = new TextBox
            {
                Location = new Point(100, 42),
                Width = 280,
                Font = new Font("Segoe UI", 9F)
            };

            var lblTipo = new Label
            {
                Text = "Tipo:",
                Location = new Point(410, 45),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            cboTipoOferta = new ComboBox
            {
                Location = new Point(460, 42),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cboTipoOferta.Items.AddRange(new[] { "PorCantidad", "Combo", "Descuento", "PorGrupo" });
            cboTipoOferta.SelectedIndex = 0;

            chkActivo = new CheckBox
            {
                Text = "✓ Oferta Activa",
                Location = new Point(630, 44),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 150, 136)
            };

            // FILA 2: Descripción + Fechas
            var lblDescripcion = new Label
            {
                Text = "Descripción:",
                Location = new Point(15, 80),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            txtDescripcion = new TextBox
            {
                Location = new Point(100, 77),
                Width = 360,
                Height = 60,
                Multiline = true,
                Font = new Font("Segoe UI", 9F)
            };

            var lblFechaInicio = new Label
            {
                Text = "Fecha Inicio:",
                Location = new Point(480, 80),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            dtpFechaInicio = new DateTimePicker
            {
                Location = new Point(575, 77),
                Width = 150,
                Format = DateTimePickerFormat.Short
            };

            var lblFechaFin = new Label
            {
                Text = "Fecha Fin:",
                Location = new Point(480, 110),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            dtpFechaFin = new DateTimePicker
            {
                Location = new Point(575, 107),
                Width = 150,
                Format = DateTimePickerFormat.Short
            };

            // ✅ Controles específicos para tipo Combo
            lblSumaProductos = new Label
            {
                Text = "Suma Productos:",
                Location = new Point(750, 45),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Visible = false
            };
            txtSumaProductos = new TextBox
            {
                Location = new Point(750, 65),
                Width = 120,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Visible = false,
                ReadOnly = true,
                BackColor = Color.FromArgb(255, 255, 200),
                ForeColor = Color.FromArgb(0, 100, 0),
                Text = "$0.00",
                TextAlign = HorizontalAlignment.Right
            };

            lblPrecioCombo = new Label
            {
                Text = "Precio Combo:",
                Location = new Point(750, 95),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Visible = false
            };
            txtPrecioCombo = new TextBox
            {
                Location = new Point(750, 115),
                Width = 120,
                Font = new Font("Segoe UI", 9F),
                Visible = false,
                Text = "0.00",
                TextAlign = HorizontalAlignment.Right
            };

            // ✅ Controles específicos para tipo Descuento
            lblPorcentajeDescuento = new Label
            {
                Text = "% Descuento:",
                Location = new Point(750, 80),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Visible = false
            };
            nudPorcentajeDescuento = new NumericUpDown
            {
                Location = new Point(750, 100),
                Width = 120,
                Font = new Font("Segoe UI", 9F),
                Visible = false,
                Minimum = 0,
                Maximum = 100,
                DecimalPlaces = 2,
                Value = 0
            };

            // ✅ Controles específicos para tipo PorGrupo
            lblCantidadGrupo = new Label
            {
                Text = "Cant. mín. grupo:",
                Location = new Point(750, 44),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Visible = false
            };
            nudCantidadGrupo = new NumericUpDown
            {
                Location = new Point(750, 64),
                Width = 120,
                Font = new Font("Segoe UI", 9F),
                Visible = false,
                Minimum = 1,
                Maximum = 9999,
                DecimalPlaces = 0,
                Value = 3
            };
            lblPrecioGrupo = new Label
            {
                Text = "Precio grupo:",
                Location = new Point(750, 95),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Visible = false
            };
            txtPrecioGrupo = new TextBox
            {
                Location = new Point(750, 115),
                Width = 120,
                Font = new Font("Segoe UI", 9F),
                Visible = false,
                Text = "0,00",
                TextAlign = HorizontalAlignment.Right
            };

            // Separador
            var separador = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Height = 2,
                Location = new Point(15, 150),
                Width = 950,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // Grid de productos
            var lblProductos = new Label
            {
                Text = "🛒 Productos en la Oferta",
                Location = new Point(15, 160),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true
            };

            dgvDetalleOferta = new DataGridView
            {
                Location = new Point(15, 185),
                Size = new Size(780, 120),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            ConfigurarGridDetalleOferta();

            // Botones inferiores
            int botonDetalleX = 800;
            int botonDetalleY = 185;
            int espacioVerticalDetalle = 32;

            btnAgregarProducto = new Button
            {
                Text = "➕ Agregar",
                Location = new Point(botonDetalleX, botonDetalleY),
                Size = new Size(170, 28),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnQuitarProducto = new Button
            {
                Text = "➖ Quitar",
                Location = new Point(botonDetalleX, botonDetalleY + espacioVerticalDetalle),
                Size = new Size(170, 28),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnGuardar = new Button
            {
                Text = "💾 Guardar",
                Location = new Point(botonDetalleX, botonDetalleY + (espacioVerticalDetalle * 2)),
                Size = new Size(170, 28),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnCancelar = new Button
            {
                Text = "✖ Cancelar",
                Location = new Point(botonDetalleX, botonDetalleY + (espacioVerticalDetalle * 3)),
                Size = new Size(170, 28),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            // Agregar todos los controles al panel de edición
            panelEdicion.Controls.Add(lblOfertaActual);
            panelEdicion.Controls.Add(lblNombre);
            panelEdicion.Controls.Add(txtNombreOferta);
            panelEdicion.Controls.Add(lblTipo);
            panelEdicion.Controls.Add(cboTipoOferta);
            panelEdicion.Controls.Add(chkActivo);
            panelEdicion.Controls.Add(lblDescripcion);
            panelEdicion.Controls.Add(txtDescripcion);
            panelEdicion.Controls.Add(lblFechaInicio);
            panelEdicion.Controls.Add(dtpFechaInicio);
            panelEdicion.Controls.Add(lblFechaFin);
            panelEdicion.Controls.Add(dtpFechaFin);
            panelEdicion.Controls.Add(lblPrecioCombo);
            panelEdicion.Controls.Add(txtPrecioCombo);
            panelEdicion.Controls.Add(lblPorcentajeDescuento);
            panelEdicion.Controls.Add(nudPorcentajeDescuento);
            panelEdicion.Controls.Add(lblCantidadGrupo);
            panelEdicion.Controls.Add(nudCantidadGrupo);
            panelEdicion.Controls.Add(separador);
            panelEdicion.Controls.Add(lblProductos);
            panelEdicion.Controls.Add(dgvDetalleOferta);
            panelEdicion.Controls.Add(btnAgregarProducto);
            panelEdicion.Controls.Add(btnQuitarProducto);
            panelEdicion.Controls.Add(btnGuardar);
            panelEdicion.Controls.Add(btnCancelar);
            panelEdicion.Controls.Add(lblSumaProductos);
            panelEdicion.Controls.Add(txtSumaProductos);
            panelEdicion.Controls.Add(lblPrecioGrupo);
            panelEdicion.Controls.Add(txtPrecioGrupo);

            // Agregar los paneles principales al formulario
            this.Controls.Add(panelEdicion);
            this.Controls.Add(panelSuperior);
        }

        // ✅ NUEVO: Ajustar columnas según tipo de oferta
        private void ConfigurarGridDetalleOferta()
        {
            dgvDetalleOferta.Columns.Clear();

            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "IdProducto",
                HeaderText = "IdProducto",
                Visible = false
            });

            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "Id",
                Visible = false
            });

            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CodigoProducto",
                HeaderText = "Código",
                Width = 120,
                ReadOnly = false
            });

            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Descripcion",
                HeaderText = "Descripción",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });

            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PrecioOriginal",
                HeaderText = "Precio Normal",
                Width = 100,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });

            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CantidadMinima",
                HeaderText = "Cant. Mín.",
                Width = 70,
                ReadOnly = false,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PrecioOferta",
                HeaderText = "Precio Oferta",
                Width = 100,
                ReadOnly = false,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });

            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PorcentajeDescuento",
                HeaderText = "% Desc.",
                Width = 70,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N2",
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });

            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CantidadCombo",
                HeaderText = "Cant.",
                Width = 60,
                ReadOnly = false,
                Visible = false,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
        }

        private void AjustarColumnasSegunTipo(string tipoOferta)
        {
            switch (tipoOferta)
            {
                case "PorCantidad":
                    dgvDetalleOferta.Columns["CantidadMinima"].Visible = true;
                    dgvDetalleOferta.Columns["CantidadMinima"].HeaderText = "Cant. Mín.";
                    dgvDetalleOferta.Columns["PrecioOferta"].Visible = true;
                    dgvDetalleOferta.Columns["PrecioOferta"].ReadOnly = false;
                    dgvDetalleOferta.Columns["PorcentajeDescuento"].Visible = true;
                    dgvDetalleOferta.Columns["CantidadCombo"].Visible = false;

                    lblSumaProductos.Visible = false;
                    txtSumaProductos.Visible = false;
                    lblPrecioCombo.Visible = false;
                    txtPrecioCombo.Visible = false;
                    lblPorcentajeDescuento.Visible = false;
                    nudPorcentajeDescuento.Visible = false;
                    lblCantidadGrupo.Visible = false;
                    nudCantidadGrupo.Visible = false;
                    lblPrecioGrupo.Visible = false;
                    txtPrecioGrupo.Visible = false;
                    break;

                case "Combo":
                    dgvDetalleOferta.Columns["CantidadMinima"].Visible = true;
                    dgvDetalleOferta.Columns["CantidadMinima"].HeaderText = "Cantidad";
                    dgvDetalleOferta.Columns["PrecioOferta"].Visible = false;
                    dgvDetalleOferta.Columns["PorcentajeDescuento"].Visible = false;
                    dgvDetalleOferta.Columns["CantidadCombo"].Visible = false;

                    lblSumaProductos.Visible = true;
                    txtSumaProductos.Visible = true;
                    lblPrecioCombo.Visible = true;
                    txtPrecioCombo.Visible = true;
                    lblPorcentajeDescuento.Visible = false;
                    nudPorcentajeDescuento.Visible = false;
                    lblCantidadGrupo.Visible = false;
                    nudCantidadGrupo.Visible = false;
                    lblPrecioGrupo.Visible = false;
                    txtPrecioGrupo.Visible = false;

                    CalcularSumaProductosCombo();
                    break;

                case "Descuento":
                    dgvDetalleOferta.Columns["CantidadMinima"].Visible = false;
                    dgvDetalleOferta.Columns["PrecioOferta"].Visible = false;
                    dgvDetalleOferta.Columns["PorcentajeDescuento"].Visible = false;
                    dgvDetalleOferta.Columns["CantidadCombo"].Visible = false;

                    lblSumaProductos.Visible = false;
                    txtSumaProductos.Visible = false;
                    lblPrecioCombo.Visible = false;
                    txtPrecioCombo.Visible = false;
                    lblPorcentajeDescuento.Visible = true;
                    nudPorcentajeDescuento.Visible = true;
                    lblCantidadGrupo.Visible = false;
                    nudCantidadGrupo.Visible = false;
                    lblPrecioGrupo.Visible = false;
                    txtPrecioGrupo.Visible = false;
                    break;

                case "PorGrupo":
                    dgvDetalleOferta.Columns["CantidadMinima"].Visible = false;
                    dgvDetalleOferta.Columns["PrecioOferta"].Visible = false;
                    dgvDetalleOferta.Columns["PorcentajeDescuento"].Visible = false;
                    dgvDetalleOferta.Columns["CantidadCombo"].Visible = false;

                    lblSumaProductos.Visible = false;
                    txtSumaProductos.Visible = false;
                    lblPrecioCombo.Visible = false;
                    txtPrecioCombo.Visible = false;
                    lblPorcentajeDescuento.Visible = false;
                    nudPorcentajeDescuento.Visible = false;
                    lblCantidadGrupo.Visible = true;
                    nudCantidadGrupo.Visible = true;
                    lblPrecioGrupo.Visible = true;
                    txtPrecioGrupo.Visible = true;
                    break;
            }
        }

        private void CalcularSumaProductosCombo()
        {
            if (cboTipoOferta.SelectedItem?.ToString() != "Combo")
                return;

            decimal sumaTotal = 0;

            foreach (DataGridViewRow row in dgvDetalleOferta.Rows)
            {
                if (row.IsNewRow)
                    continue;

                if (decimal.TryParse(row.Cells["PrecioOriginal"].Value?.ToString(), out decimal precioOriginal))
                {
                    int cantidad = 1;
                    if (int.TryParse(row.Cells["CantidadMinima"].Value?.ToString(), out int cant) && cant > 0)
                        cantidad = cant;

                    sumaTotal += precioOriginal * cantidad;
                }
            }

            txtSumaProductos.Text = sumaTotal.ToString("C2");
        }

        private Button CrearBoton(string texto, int left, Color backColor)
        {
            return new Button
            {
                Text = texto,
                Location = new Point(left, 5),
                Size = new Size(140, 35),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }

        private void ConfigurarControles()
        {
            btnEditarOferta.Enabled = false;
            btnEliminarOferta.Enabled = false;
        }

        private void ConfigurarEventos()
        {
            btnNuevaOferta.Click += BtnNuevaOferta_Click;
            btnEditarOferta.Click += BtnEditarOferta_Click;
            btnEliminarOferta.Click += BtnEliminarOferta_Click;
            btnAgregarProducto.Click += BtnAgregarProducto_Click;
            btnQuitarProducto.Click += BtnQuitarProducto_Click;
            btnGuardar.Click += BtnGuardar_Click;
            btnCancelar.Click += BtnCancelar_Click;

            dgvOfertas.SelectionChanged += DgvOfertas_SelectionChanged;
            dgvDetalleOferta.CellValueChanged += DgvDetalleOferta_CellValueChanged;
            dgvDetalleOferta.CellEndEdit += DgvDetalleOferta_CellEndEdit;

            dgvDetalleOferta.RowsAdded += (s, e) => CalcularSumaProductosCombo();
            dgvDetalleOferta.RowsRemoved += (s, e) => CalcularSumaProductosCombo();

            cboTipoOferta.SelectedIndexChanged += CboTipoOferta_SelectedIndexChanged;
            dgvOfertas.CellDoubleClick += DgvOfertas_CellDoubleClick;

            // ✅ NUEVO: Evento de búsqueda en tiempo real
            txtBuscarOferta.TextChanged += TxtBuscarOferta_TextChanged;
        }

        // ✅ NUEVO: Filtrar la grilla según el texto ingresado
        private void TxtBuscarOferta_TextChanged(object sender, EventArgs e)
        {
            if (dtOfertasCompleto == null)
                return;

            string filtro = txtBuscarOferta.Text.Trim();

            if (string.IsNullOrEmpty(filtro))
            {
                dgvOfertas.DataSource = dtOfertasCompleto;
            }
            else
            {
                var dvFiltrado = new DataView(dtOfertasCompleto)
                {
                    RowFilter = $"Nombre LIKE '%{filtro.Replace("'", "''")}%'"
                };
                dgvOfertas.DataSource = dvFiltrado;
            }

            // Restaurar visibilidad de columna Id
            if (dgvOfertas.Columns["Id"] != null)
                dgvOfertas.Columns["Id"].Visible = false;
        }

        private void DgvOfertas_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                BtnEditarOferta_Click(sender, e);
        }

        private void CboTipoOferta_SelectedIndexChanged(object sender, EventArgs e)
        {
            AjustarColumnasSegunTipo(cboTipoOferta.SelectedItem.ToString());
        }

        private void CargarOfertas()
        {
            try
            {
                string connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"
                SELECT 
                    Id,
                    Nombre,
                    TipoOferta AS 'Tipo',
                    FechaInicio AS 'Inicio',
                    FechaFin AS 'Fin',
                    CASE WHEN Activo = 1 THEN 'Sí' ELSE 'No' END AS 'Activa',
                    (SELECT COUNT(*) FROM DetalleOfertasProductos WHERE IdOferta = OfertasProductos.Id) AS 'Productos'
                FROM OfertasProductos
                ORDER BY Activo DESC, FechaInicio DESC";

                    var adapter = new SqlDataAdapter(query, connection);
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    // ✅ NUEVO: Guardar copia completa para el filtrado
                    dtOfertasCompleto = dt;

                    dgvOfertas.DataSource = dt;

                    if (dgvOfertas.Columns["Id"] != null)
                        dgvOfertas.Columns["Id"].Visible = false;

                    if (dgvOfertas.Columns["Nombre"] != null)
                    {
                        dgvOfertas.Columns["Nombre"].Width = 200;
                        dgvOfertas.Columns["Nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    }

                    if (dgvOfertas.Columns["Tipo"] != null)
                    {
                        dgvOfertas.Columns["Tipo"].Width = 110;
                        dgvOfertas.Columns["Tipo"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    }

                    if (dgvOfertas.Columns["Inicio"] != null)
                    {
                        dgvOfertas.Columns["Inicio"].Width = 90;
                        dgvOfertas.Columns["Inicio"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        dgvOfertas.Columns["Inicio"].DefaultCellStyle.Format = "dd/MM/yyyy";
                    }

                    if (dgvOfertas.Columns["Fin"] != null)
                    {
                        dgvOfertas.Columns["Fin"].Width = 90;
                        dgvOfertas.Columns["Fin"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        dgvOfertas.Columns["Fin"].DefaultCellStyle.Format = "dd/MM/yyyy";
                    }

                    if (dgvOfertas.Columns["Activa"] != null)
                    {
                        dgvOfertas.Columns["Activa"].Width = 60;
                        dgvOfertas.Columns["Activa"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        dgvOfertas.Columns["Activa"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }

                    if (dgvOfertas.Columns["Productos"] != null)
                    {
                        dgvOfertas.Columns["Productos"].Width = 70;
                        dgvOfertas.Columns["Productos"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        dgvOfertas.Columns["Productos"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ofertas: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnNuevaOferta_Click(object sender, EventArgs e)
        {
            modoEdicion = true;
            ofertaSeleccionadaId = 0;
            lblOfertaActual.Text = "📝 Nueva Oferta";
            LimpiarFormularioEdicion();
            panelEdicion.Visible = true;
            txtNombreOferta.Focus();
            AjustarColumnasSegunTipo("PorCantidad");
        }

        private void BtnEditarOferta_Click(object sender, EventArgs e)
        {
            if (dgvOfertas.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una oferta para editar.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            modoEdicion = true;
            ofertaSeleccionadaId = Convert.ToInt32(dgvOfertas.SelectedRows[0].Cells["Id"].Value);
            lblOfertaActual.Text = $"✏️ Editando Oferta #{ofertaSeleccionadaId}";
            CargarOfertaParaEdicion(ofertaSeleccionadaId);
            panelEdicion.Visible = true;
        }

        private void CargarOfertaParaEdicion(int idOferta)
        {
            try
            {
                string connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var queryOferta = @"
                        SELECT Nombre, Descripcion, FechaInicio, FechaFin, Activo, TipoOferta,
                               PrecioCombo, PorcentajeDescuentoGlobal, CantidadMinimaGrupo, PrecioGrupo
                        FROM OfertasProductos
                        WHERE Id = @Id";

                    using (var cmd = new SqlCommand(queryOferta, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", idOferta);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtNombreOferta.Text = reader["Nombre"].ToString();
                                txtDescripcion.Text = reader["Descripcion"].ToString();
                                dtpFechaInicio.Value = Convert.ToDateTime(reader["FechaInicio"]);

                                if (reader["FechaFin"] != DBNull.Value)
                                    dtpFechaFin.Value = Convert.ToDateTime(reader["FechaFin"]);

                                chkActivo.Checked = Convert.ToBoolean(reader["Activo"]);
                                string tipoOferta = reader["TipoOferta"].ToString();
                                cboTipoOferta.SelectedItem = tipoOferta;

                                if (tipoOferta == "Combo" && reader["PrecioCombo"] != DBNull.Value)
                                    txtPrecioCombo.Text = reader["PrecioCombo"].ToString();

                                if (tipoOferta == "Descuento" && reader["PorcentajeDescuentoGlobal"] != DBNull.Value)
                                    nudPorcentajeDescuento.Value = Convert.ToDecimal(reader["PorcentajeDescuentoGlobal"]);

                                if (tipoOferta == "PorGrupo")
                                {
                                    if (reader["CantidadMinimaGrupo"] != DBNull.Value)
                                        nudCantidadGrupo.Value = Convert.ToDecimal(reader["CantidadMinimaGrupo"]);

                                    if (reader["PrecioGrupo"] != DBNull.Value)
                                        txtPrecioGrupo.Text = Convert.ToDecimal(reader["PrecioGrupo"]).ToString("N2");
                                }
                            }
                        }
                    }

                    var queryDetalle = @"
                        SELECT 
                            d.Id,
                            d.IdProducto,
                            p.codigo AS CodigoProducto,
                            p.descripcion AS Descripcion,
                            p.precio AS PrecioOriginal,
                            d.CantidadMinima,
                            d.PrecioOferta,
                            d.PorcentajeDescuento
                        FROM DetalleOfertasProductos d
                        INNER JOIN productos p ON d.IdProducto = p.ID
                        WHERE d.IdOferta = @Id";

                    dgvDetalleOferta.Rows.Clear();

                    using (var cmd = new SqlCommand(queryDetalle, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", idOferta);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                dgvDetalleOferta.Rows.Add(
                                    reader["IdProducto"],
                                    reader["Id"],
                                    reader["CodigoProducto"],
                                    reader["Descripcion"],
                                    reader["PrecioOriginal"],
                                    reader["CantidadMinima"],
                                    reader["PrecioOferta"],
                                    reader["PorcentajeDescuento"],
                                    reader["CantidadMinima"]
                                );
                            }
                        }
                    }

                    AjustarColumnasSegunTipo(cboTipoOferta.SelectedItem.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar oferta: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEliminarOferta_Click(object sender, EventArgs e)
        {
            if (dgvOfertas.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una oferta para eliminar.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int idOferta = Convert.ToInt32(dgvOfertas.SelectedRows[0].Cells["Id"].Value);
            string nombreOferta = dgvOfertas.SelectedRows[0].Cells["Nombre"].Value.ToString();

            var resultado = MessageBox.Show(
                $"¿Está seguro de eliminar la oferta '{nombreOferta}'?\n\n" +
                "Esta acción eliminará también todos los productos asociados.",
                "Confirmar Eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (resultado != DialogResult.Yes)
                return;

            try
            {
                string connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = "DELETE FROM OfertasProductos WHERE Id = @Id";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", idOferta);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Oferta eliminada correctamente.", "Éxito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                CargarOfertas();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar oferta: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAgregarProducto_Click(object sender, EventArgs e)
        {
            dgvDetalleOferta.Rows.Add(0, "", "", 0, 1, 0, 0, 1);
            dgvDetalleOferta.CurrentCell = dgvDetalleOferta.Rows[dgvDetalleOferta.Rows.Count - 1].Cells["CodigoProducto"];
            dgvDetalleOferta.BeginEdit(true);
        }

        private void BtnQuitarProducto_Click(object sender, EventArgs e)
        {
            if (dgvDetalleOferta.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un producto para quitar.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dgvDetalleOferta.Rows.RemoveAt(dgvDetalleOferta.SelectedRows[0].Index);
        }

        private void DgvDetalleOferta_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dgvDetalleOferta.Columns["CodigoProducto"].Index)
            {
                var row = dgvDetalleOferta.Rows[e.RowIndex];
                string codigo = row.Cells["CodigoProducto"].Value?.ToString()?.Trim();

                if (string.IsNullOrEmpty(codigo))
                    return;

                try
                {
                    string connectionString = GetConnectionString();

                    using (var connection = new SqlConnection(connectionString))
                    {
                        var query = "SELECT ID, descripcion, precio FROM Productos WHERE codigo = @codigo";

                        using (var cmd = new SqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@codigo", codigo);
                            connection.Open();

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    row.Cells["IdProducto"].Value = reader["ID"];
                                    row.Cells["Descripcion"].Value = reader["descripcion"];
                                    row.Cells["PrecioOriginal"].Value = reader["precio"];
                                    row.Cells["PrecioOferta"].Value = reader["precio"];
                                    row.Cells["CantidadMinima"].Value = 1;
                                    row.Cells["PorcentajeDescuento"].Value = 0;
                                    row.Cells["CantidadCombo"].Value = 1;

                                    CalcularSumaProductosCombo();
                                }
                                else
                                {
                                    MessageBox.Show($"No se encontró el producto con código '{codigo}'.",
                                        "Producto no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                                    row.Cells["CodigoProducto"].Value = "";
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al buscar producto: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (e.ColumnIndex == dgvDetalleOferta.Columns["CantidadMinima"].Index)
                CalcularSumaProductosCombo();
        }

        private void DgvDetalleOferta_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvDetalleOferta.Rows[e.RowIndex];

            if (e.ColumnIndex == dgvDetalleOferta.Columns["PrecioOferta"].Index)
            {
                if (decimal.TryParse(row.Cells["PrecioOriginal"].Value?.ToString(), out decimal precioOriginal) &&
                    decimal.TryParse(row.Cells["PrecioOferta"].Value?.ToString(), out decimal precioOferta) &&
                    precioOriginal > 0)
                {
                    decimal porcentaje = ((precioOriginal - precioOferta) / precioOriginal) * 100;
                    row.Cells["PorcentajeDescuento"].Value = Math.Round(porcentaje, 2);
                }
            }
        }

        private async void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (!ValidarFormulario())
                return;

            try
            {
                string connectionString = GetConnectionString();
                string tipoOferta = cboTipoOferta.SelectedItem.ToString();

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            if (ofertaSeleccionadaId == 0)
                            {
                                var queryInsert = @"
                                INSERT INTO OfertasProductos 
                                    (Nombre, Descripcion, FechaInicio, FechaFin, Activo, TipoOferta, 
                                     PrecioCombo, PorcentajeDescuentoGlobal, UsuarioCreacion, CantidadMinimaGrupo, PrecioGrupo)
                                VALUES 
                                    (@Nombre, @Descripcion, @FechaInicio, @FechaFin, @Activo, @TipoOferta,
                                     @PrecioCombo, @PorcentajeDescuentoGlobal, @Usuario, @CantidadMinimaGrupo, @PrecioGrupo);
                                SELECT CAST(SCOPE_IDENTITY() AS INT);";

                                using (var cmd = new SqlCommand(queryInsert, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@Nombre", txtNombreOferta.Text.Trim());
                                    cmd.Parameters.AddWithValue("@Descripcion", txtDescripcion.Text.Trim());
                                    cmd.Parameters.AddWithValue("@FechaInicio", dtpFechaInicio.Value.Date);
                                    cmd.Parameters.AddWithValue("@FechaFin",
                                        dtpFechaFin.Value.Date > dtpFechaInicio.Value.Date
                                            ? (object)dtpFechaFin.Value.Date
                                            : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Activo", chkActivo.Checked);
                                    cmd.Parameters.AddWithValue("@TipoOferta", tipoOferta);

                                    cmd.Parameters.AddWithValue("@PrecioCombo",
                                        tipoOferta == "Combo" && decimal.TryParse(txtPrecioCombo.Text, out decimal precio)
                                            ? (object)precio
                                            : DBNull.Value);

                                    cmd.Parameters.AddWithValue("@PorcentajeDescuentoGlobal",
                                        tipoOferta == "Descuento"
                                            ? (object)nudPorcentajeDescuento.Value
                                            : DBNull.Value);

                                    cmd.Parameters.AddWithValue("@CantidadMinimaGrupo",
                                        tipoOferta == "PorGrupo"
                                            ? (object)(int)nudCantidadGrupo.Value
                                            : DBNull.Value);

                                    cmd.Parameters.AddWithValue("@PrecioGrupo",
                                        tipoOferta == "PorGrupo" && decimal.TryParse(txtPrecioGrupo.Text, out decimal pgInsert)
                                            ? (object)pgInsert
                                            : DBNull.Value);

                                    cmd.Parameters.AddWithValue("@Usuario",
                                        AuthenticationService.SesionActual?.Usuario?.NombreUsuario ?? Environment.UserName);

                                    ofertaSeleccionadaId = (int)cmd.ExecuteScalar();
                                }
                            }
                            else
                            {
                                var queryUpdate = @"
                                    UPDATE OfertasProductos
                                    SET Nombre = @Nombre,
                                        Descripcion = @Descripcion,
                                        FechaInicio = @FechaInicio,
                                        FechaFin = @FechaFin,
                                        Activo = @Activo,
                                        TipoOferta = @TipoOferta,
                                        PrecioCombo = @PrecioCombo,
                                        PorcentajeDescuentoGlobal = @PorcentajeDescuentoGlobal,
                                        CantidadMinimaGrupo = @CantidadMinimaGrupo,
                                        PrecioGrupo = @PrecioGrupo
                                    WHERE Id = @Id";

                                using (var cmd = new SqlCommand(queryUpdate, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@Id", ofertaSeleccionadaId);
                                    cmd.Parameters.AddWithValue("@Nombre", txtNombreOferta.Text.Trim());
                                    cmd.Parameters.AddWithValue("@Descripcion", txtDescripcion.Text.Trim());
                                    cmd.Parameters.AddWithValue("@FechaInicio", dtpFechaInicio.Value.Date);
                                    cmd.Parameters.AddWithValue("@FechaFin",
                                        dtpFechaFin.Value.Date > dtpFechaInicio.Value.Date
                                            ? (object)dtpFechaFin.Value.Date
                                            : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Activo", chkActivo.Checked);
                                    cmd.Parameters.AddWithValue("@TipoOferta", tipoOferta);

                                    cmd.Parameters.AddWithValue("@PrecioCombo",
                                        tipoOferta == "Combo" && decimal.TryParse(txtPrecioCombo.Text, out decimal precio)
                                            ? (object)precio
                                            : DBNull.Value);

                                    cmd.Parameters.AddWithValue("@PorcentajeDescuentoGlobal",
                                        tipoOferta == "Descuento"
                                            ? (object)nudPorcentajeDescuento.Value
                                            : DBNull.Value);

                                    cmd.Parameters.AddWithValue("@CantidadMinimaGrupo",
                                        tipoOferta == "PorGrupo"
                                            ? (object)(int)nudCantidadGrupo.Value
                                            : DBNull.Value);

                                    cmd.Parameters.AddWithValue("@PrecioGrupo",
                                        tipoOferta == "PorGrupo" && decimal.TryParse(txtPrecioGrupo.Text, out decimal pgUpdate)
                                            ? (object)pgUpdate
                                            : DBNull.Value);

                                    cmd.ExecuteNonQuery();
                                }

                                var queryDeleteDetalle = "DELETE FROM DetalleOfertasProductos WHERE IdOferta = @IdOferta";
                                using (var cmd = new SqlCommand(queryDeleteDetalle, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@IdOferta", ofertaSeleccionadaId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            foreach (DataGridViewRow row in dgvDetalleOferta.Rows)
                            {
                                if (string.IsNullOrEmpty(row.Cells["CodigoProducto"].Value?.ToString()))
                                    continue;

                                var queryDetalle = @"
                                    INSERT INTO DetalleOfertasProductos
                                        (IdOferta, IdProducto, CantidadMinima, PrecioOferta, PorcentajeDescuento)
                                    VALUES
                                        (@IdOferta, @IdProducto, @CantidadMinima, @PrecioOferta, @PorcentajeDescuento)";

                                using (var cmd = new SqlCommand(queryDetalle, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@IdOferta", ofertaSeleccionadaId);
                                    cmd.Parameters.AddWithValue("@IdProducto",
                                        Convert.ToInt32(row.Cells["IdProducto"].Value));

                                    int cantidadMinima = 1;
                                    decimal precioOferta = 0;
                                    decimal porcentajeDescuento = 0;

                                    switch (tipoOferta)
                                    {
                                        case "PorCantidad":
                                            cantidadMinima = Convert.ToInt32(row.Cells["CantidadMinima"].Value ?? 1);
                                            precioOferta = Convert.ToDecimal(row.Cells["PrecioOferta"].Value ?? 0);
                                            porcentajeDescuento = Convert.ToDecimal(row.Cells["PorcentajeDescuento"].Value ?? 0);
                                            break;

                                        case "Combo":
                                            cantidadMinima = Convert.ToInt32(row.Cells["CantidadMinima"].Value ?? 1);
                                            break;

                                        case "Descuento":
                                            porcentajeDescuento = nudPorcentajeDescuento.Value;
                                            decimal precioOriginal = Convert.ToDecimal(row.Cells["PrecioOriginal"].Value ?? 0);
                                            precioOferta = precioOriginal * (1 - (porcentajeDescuento / 100));
                                            break;

                                        case "PorGrupo":
                                            break;
                                    }

                                    cmd.Parameters.AddWithValue("@CantidadMinima", cantidadMinima);
                                    cmd.Parameters.AddWithValue("@PrecioOferta", precioOferta);
                                    cmd.Parameters.AddWithValue("@PorcentajeDescuento", porcentajeDescuento);

                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();

                            MessageBox.Show("✅ Oferta guardada correctamente.", "Éxito",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                            panelEdicion.Visible = false;
                            CargarOfertas();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error al guardar oferta: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(txtNombreOferta.Text))
            {
                MessageBox.Show("Ingrese un nombre para la oferta.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNombreOferta.Focus();
                return false;
            }

            if (dgvDetalleOferta.Rows.Count == 0)
            {
                MessageBox.Show("Debe agregar al menos un producto a la oferta.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string tipoOferta = cboTipoOferta.SelectedItem.ToString();

            if (tipoOferta == "Combo")
            {
                if (!decimal.TryParse(txtPrecioCombo.Text, out decimal precioCombo) || precioCombo <= 0)
                {
                    MessageBox.Show("Ingrese un precio válido para el combo.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPrecioCombo.Focus();
                    return false;
                }
            }

            if (tipoOferta == "Descuento")
            {
                if (nudPorcentajeDescuento.Value <= 0 || nudPorcentajeDescuento.Value > 100)
                {
                    MessageBox.Show("El porcentaje de descuento debe estar entre 0 y 100.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    nudPorcentajeDescuento.Focus();
                    return false;
                }
            }

            if (tipoOferta == "PorGrupo")
            {
                if (nudCantidadGrupo.Value < 1)
                {
                    MessageBox.Show("La cantidad mínima del grupo debe ser al menos 1.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    nudCantidadGrupo.Focus();
                    return false;
                }

                if (!decimal.TryParse(txtPrecioGrupo.Text, out decimal precioGrupo) || precioGrupo <= 0)
                {
                    MessageBox.Show("El precio del grupo debe ser un valor mayor a cero.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPrecioGrupo.Focus();
                    return false;
                }
            }

            foreach (DataGridViewRow row in dgvDetalleOferta.Rows)
            {
                if (string.IsNullOrEmpty(row.Cells["CodigoProducto"].Value?.ToString()))
                {
                    MessageBox.Show("Hay productos sin código. Complete o elimine las filas vacías.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (tipoOferta == "PorCantidad")
                {
                    if (!int.TryParse(row.Cells["CantidadMinima"].Value?.ToString(), out int cantidad) || cantidad < 1)
                    {
                        MessageBox.Show("La cantidad mínima debe ser mayor a 0.", "Validación",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    if (!decimal.TryParse(row.Cells["PrecioOferta"].Value?.ToString(), out decimal precio) || precio < 0)
                    {
                        MessageBox.Show("El precio de oferta no es válido.", "Validación",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
            }

            return true;
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            var resultado = MessageBox.Show(
                "¿Desea cancelar la edición? Se perderán los cambios no guardados.",
                "Confirmar Cancelación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (resultado == DialogResult.Yes)
            {
                panelEdicion.Visible = false;
                LimpiarFormularioEdicion();
            }
        }

        private void LimpiarFormularioEdicion()
        {
            txtNombreOferta.Clear();
            txtDescripcion.Clear();
            dtpFechaInicio.Value = DateTime.Now;
            dtpFechaFin.Value = DateTime.Now.AddMonths(1);
            cboTipoOferta.SelectedIndex = 0;
            chkActivo.Checked = true;
            txtPrecioCombo.Text = "0.00";
            txtSumaProductos.Text = "$0.00";
            nudPorcentajeDescuento.Value = 0;
            dgvDetalleOferta.Rows.Clear();
            ofertaSeleccionadaId = 0;
            nudCantidadGrupo.Value = 3;
            txtPrecioGrupo.Text = "0,00";
        }

        private void DgvOfertas_SelectionChanged(object sender, EventArgs e)
        {
            btnEditarOferta.Enabled = dgvOfertas.SelectedRows.Count > 0;
            btnEliminarOferta.Enabled = dgvOfertas.SelectedRows.Count > 0;
        }

        private string GetConnectionString()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return config.GetConnectionString("DefaultConnection");
        }
    }
}