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
        
        private int ofertaSeleccionadaId = 0;
        private bool modoEdicion = false;

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
            this.Size = new Size(1000, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
        }

        private void ConfigurarFormulario()
        {
            this.BackColor = Color.WhiteSmoke;
            this.Font = new Font("Segoe UI", 10F);

            // ========================================
            // PANEL SUPERIOR - Grilla más ancha, botones más angostos
            // ========================================
            var panelSuperior = new Panel
            {
                Dock = DockStyle.Top,
                Height = 155, // ✅ REDUCIDO: De 165 a 155 (10px menos)
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

            // ✅ GRILLA MÁS ANCHA
            dgvOfertas = new DataGridView
            {
                Location = new Point(10, 40),
                Width = 780, // ✅ AMPLIADO: De 660 a 780 (120px más)
                Height = 100, // ✅ REDUCIDO: De 110 a 100 (10px menos)
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            // ✅ BOTONES MÁS ANGOSTOS A LA DERECHA
            int botonX = 800; // ✅ Más a la derecha
            int botonY = 40;
            int espacioVertical = 34; // ✅ REDUCIDO: De 37 a 34

            btnNuevaOferta = new Button
            {
                Text = "Nueva Oferta",
                Location = new Point(botonX, botonY),
                Size = new Size(170, 30), // ✅ REDUCIDO: De 270x32 a 170x30
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
            panelSuperior.Controls.Add(dgvOfertas);
            panelSuperior.Controls.Add(btnNuevaOferta);
            panelSuperior.Controls.Add(btnEditarOferta);
            panelSuperior.Controls.Add(btnEliminarOferta);

            // ========================================
            // PANEL DE EDICIÓN - MÁS COMPACTO
            // ========================================
            panelEdicion = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = false, // ✅ CAMBIADO: Desactivar scroll ya que optimizamos el espacio
                Padding = new Padding(15),
                Visible = false,
                BackColor = Color.White
            };

            lblOfertaActual = new Label
            {
                Text = "📝 Nueva Oferta",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold), // ✅ REDUCIDO: De 12F a 11F
                Location = new Point(15, 10), // ✅ REDUCIDO: De 15 a 10
                AutoSize = true,
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            // ✅ FILA 1: Nombre y Tipo (más compacta)
            var lblNombre = new Label
            {
                Text = "Nombre:",
                Location = new Point(15, 45), // ✅ REDUCIDO: De 55 a 45
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
            cboTipoOferta.Items.AddRange(new[] { "PorCantidad", "Combo", "Descuento" });
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

            // ✅ FILA 2: Descripción + Fechas (más compacta)
            var lblDescripcion = new Label
            {
                Text = "Descripción:",
                Location = new Point(15, 80), // ✅ REDUCIDO: De 95 a 80
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            txtDescripcion = new TextBox
            {
                Location = new Point(100, 77),
                Width = 360,
                Height = 60, // ✅ REDUCIDO: De 70 a 60
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
                Location = new Point(480, 110), // ✅ REDUCIDO: De 130 a 110
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            dtpFechaFin = new DateTimePicker
            {
                Location = new Point(575, 107),
                Width = 150,
                Format = DateTimePickerFormat.Short
            };

            // Separador
            var separador = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Height = 2,
                Location = new Point(15, 150), // ✅ REDUCIDO: De 175 a 150
                Width = 950,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // Grid de productos
            var lblProductos = new Label
            {
                Text = "🛒 Productos en la Oferta",
                Location = new Point(15, 160), // ✅ REDUCIDO: De 190 a 160
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true
            };

            // ✅ GRILLA INFERIOR MÁS ANCHA
            dgvDetalleOferta = new DataGridView
            {
                Location = new Point(15, 185), // ✅ REDUCIDO: De 220 a 185
                Size = new Size(780, 120), // ✅ AMPLIADO: De 660x130 a 780x120
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            ConfigurarGridDetalleOferta();

            // ✅ BOTONES INFERIORES MÁS ANGOSTOS A LA DERECHA
            int botonDetalleX = 800;
            int botonDetalleY = 185;
            int espacioVerticalDetalle = 32; // ✅ REDUCIDO: De 35 a 32

            btnAgregarProducto = new Button
            {
                Text = "➕ Agregar",
                Location = new Point(botonDetalleX, botonDetalleY),
                Size = new Size(170, 28), // ✅ REDUCIDO: De 270x32 a 170x28
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
            panelEdicion.Controls.Add(separador);
            panelEdicion.Controls.Add(lblProductos);
            panelEdicion.Controls.Add(dgvDetalleOferta);
            panelEdicion.Controls.Add(btnAgregarProducto);
            panelEdicion.Controls.Add(btnQuitarProducto);
            panelEdicion.Controls.Add(btnGuardar);
            panelEdicion.Controls.Add(btnCancelar);

            // Agregar los paneles principales al formulario
            this.Controls.Add(panelEdicion);
            this.Controls.Add(panelSuperior);
        }

        private void ConfigurarGridDetalleOferta()
        {
            dgvDetalleOferta.Columns.Clear();

            // ✅ Columna oculta para IdProducto
            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "IdProducto",
                HeaderText = "IdProducto",
                Visible = false
            });

            // Columna oculta para ID del detalle
            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "Id",
                Visible = false
            });

            // Código de producto
            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CodigoProducto",
                HeaderText = "Código",
                Width = 120, 
                ReadOnly = false
            });

            // Descripción (se expande para ocupar espacio restante)
            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Descripcion",
                HeaderText = "Descripción",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, // ✅ Se expande
                ReadOnly = true
            });

            // Precio original
            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PrecioOriginal",
                HeaderText = "Precio Normal",
                Width = 100, // ✅ REDUCIDO: De 120 a 100
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });

            // Cantidad mínima - ✅ MÁS ANGOSTA
            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CantidadMinima",
                HeaderText = "Cant. Mín.",
                Width = 50, // ✅ REDUCIDO: De 120 a 70
                ReadOnly = false,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            // Precio de oferta - ✅ MÁS ANGOSTA
            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PrecioOferta",
                HeaderText = "Precio Oferta",
                Width = 100, // ✅ REDUCIDO: De 120 a 100
                ReadOnly = false,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });

            // % Descuento
            dgvDetalleOferta.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PorcentajeDescuento",
                HeaderText = "% Desc.",
                Width = 70, // ✅ REDUCIDO: De 100 a 70
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N2",
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });
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

                    dgvOfertas.DataSource = dt;

                    // ✅ OCULTAR columna Id
                    if (dgvOfertas.Columns["Id"] != null)
                        dgvOfertas.Columns["Id"].Visible = false;

                    // ✅ AJUSTAR ANCHOS PARA GRILLA MÁS ANGOSTA (660px total)
                    if (dgvOfertas.Columns["Nombre"] != null)
                    {
                        dgvOfertas.Columns["Nombre"].Width = 200; // ✅ Reducido
                        dgvOfertas.Columns["Nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    }

                    if (dgvOfertas.Columns["Tipo"] != null)
                    {
                        dgvOfertas.Columns["Tipo"].Width = 110; // ✅ Reducido
                        dgvOfertas.Columns["Tipo"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    }

                    if (dgvOfertas.Columns["Inicio"] != null)
                    {
                        dgvOfertas.Columns["Inicio"].Width = 90; // ✅ Reducido
                        dgvOfertas.Columns["Inicio"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        dgvOfertas.Columns["Inicio"].DefaultCellStyle.Format = "dd/MM/yyyy";
                    }

                    if (dgvOfertas.Columns["Fin"] != null)
                    {
                        dgvOfertas.Columns["Fin"].Width = 90; // ✅ Reducido
                        dgvOfertas.Columns["Fin"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        dgvOfertas.Columns["Fin"].DefaultCellStyle.Format = "dd/MM/yyyy";
                    }

                    if (dgvOfertas.Columns["Activa"] != null)
                    {
                        dgvOfertas.Columns["Activa"].Width = 60; // ✅ Reducido
                        dgvOfertas.Columns["Activa"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        dgvOfertas.Columns["Activa"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }

                    if (dgvOfertas.Columns["Productos"] != null)
                    {
                        dgvOfertas.Columns["Productos"].Width = 70; // ✅ Reducido
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

                    // Cargar datos de la oferta
                    var queryOferta = @"
                        SELECT Nombre, Descripcion, FechaInicio, FechaFin, Activo, TipoOferta
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
                                cboTipoOferta.SelectedItem = reader["TipoOferta"].ToString();
                            }
                        }
                    }

                    // Cargar productos de la oferta
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
                                    reader["IdProducto"],        // ✅ Columna oculta
                                    reader["Id"],
                                    reader["CodigoProducto"],    // ✅ Visible para el usuario
                                    reader["Descripcion"],
                                    reader["PrecioOriginal"],
                                    reader["CantidadMinima"],
                                    reader["PrecioOferta"],
                                    reader["PorcentajeDescuento"]
                                );
                            }
                        }
                    }
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
            // Agregar fila vacía para ingreso
            dgvDetalleOferta.Rows.Add(0, "", "", 0, 1, 0, 0);
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

                // ✅ MODIFICADO: Buscar producto y obtener su ID
                try
                {
                    string connectionString = GetConnectionString();

                    using (var connection = new SqlConnection(connectionString))
                    {
                        // ✅ CAMBIO: Incluir ID en la consulta
                        var query = "SELECT ID, descripcion, precio FROM Productos WHERE codigo = @codigo";

                        using (var cmd = new SqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@codigo", codigo);
                            connection.Open();

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    // ✅ NUEVO: Guardar el ID del producto (oculto)
                                    row.Cells["IdProducto"].Value = reader["ID"];
                                    row.Cells["Descripcion"].Value = reader["descripcion"];
                                    row.Cells["PrecioOriginal"].Value = reader["precio"];
                                    row.Cells["PrecioOferta"].Value = reader["precio"];
                                    row.Cells["CantidadMinima"].Value = 1;
                                    row.Cells["PorcentajeDescuento"].Value = 0;
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
        }

        private void DgvDetalleOferta_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvDetalleOferta.Rows[e.RowIndex];

            // Si cambió el precio de oferta, recalcular el % de descuento
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

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            if (ofertaSeleccionadaId == 0)
                            {
                                // Insertar nueva oferta
                                var queryInsert = @"
                                    INSERT INTO OfertasProductos 
                                        (Nombre, Descripcion, FechaInicio, FechaFin, Activo, TipoOferta, UsuarioCreacion)
                                    VALUES 
                                        (@Nombre, @Descripcion, @FechaInicio, @FechaFin, @Activo, @TipoOferta, @Usuario);
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
                                    cmd.Parameters.AddWithValue("@TipoOferta", cboTipoOferta.SelectedItem.ToString());
                                    cmd.Parameters.AddWithValue("@Usuario", 
                                        AuthenticationService.SesionActual?.Usuario?.NombreUsuario ?? Environment.UserName);

                                    ofertaSeleccionadaId = (int)cmd.ExecuteScalar();
                                }
                            }
                            else
                            {
                                // Actualizar oferta existente
                                var queryUpdate = @"
                                    UPDATE OfertasProductos
                                    SET Nombre = @Nombre,
                                        Descripcion = @Descripcion,
                                        FechaInicio = @FechaInicio,
                                        FechaFin = @FechaFin,
                                        Activo = @Activo,
                                        TipoOferta = @TipoOferta
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
                                    cmd.Parameters.AddWithValue("@TipoOferta", cboTipoOferta.SelectedItem.ToString());

                                    cmd.ExecuteNonQuery();
                                }

                                // Eliminar detalles existentes
                                var queryDeleteDetalle = "DELETE FROM DetalleOfertasProductos WHERE IdOferta = @IdOferta";
                                using (var cmd = new SqlCommand(queryDeleteDetalle, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@IdOferta", ofertaSeleccionadaId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            // ✅ MODIFICADO: Insertar detalles usando IdProducto
                            foreach (DataGridViewRow row in dgvDetalleOferta.Rows)
                            {
                                if (string.IsNullOrEmpty(row.Cells["CodigoProducto"].Value?.ToString()))
                                    continue;

                                // ✅ CAMBIO: Usar IdProducto en lugar de CodigoProducto
                                var queryDetalle = @"
                                    INSERT INTO DetalleOfertasProductos
                                        (IdOferta, IdProducto, CantidadMinima, PrecioOferta, PorcentajeDescuento)
                                    VALUES
                                        (@IdOferta, @IdProducto, @CantidadMinima, @PrecioOferta, @PorcentajeDescuento)";

                                using (var cmd = new SqlCommand(queryDetalle, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@IdOferta", ofertaSeleccionadaId);
                                    
                                    // ✅ CRÍTICO: Usar IdProducto en lugar de CodigoProducto
                                    cmd.Parameters.AddWithValue("@IdProducto", 
                                        Convert.ToInt32(row.Cells["IdProducto"].Value));
                                    
                                    cmd.Parameters.AddWithValue("@CantidadMinima", 
                                        Convert.ToInt32(row.Cells["CantidadMinima"].Value ?? 1));
                                    cmd.Parameters.AddWithValue("@PrecioOferta", 
                                        Convert.ToDecimal(row.Cells["PrecioOferta"].Value ?? 0));
                                    cmd.Parameters.AddWithValue("@PorcentajeDescuento",
                                        Convert.ToDecimal(row.Cells["PorcentajeDescuento"].Value ?? 0));

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

            // Validar que todos los productos tengan datos completos
            foreach (DataGridViewRow row in dgvDetalleOferta.Rows)
            {
                if (string.IsNullOrEmpty(row.Cells["CodigoProducto"].Value?.ToString()))
                {
                    MessageBox.Show("Hay productos sin código. Complete o elimine las filas vacías.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

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
            dgvDetalleOferta.Rows.Clear();
            ofertaSeleccionadaId = 0;
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