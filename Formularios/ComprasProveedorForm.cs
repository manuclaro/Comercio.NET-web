using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public class ComprasProveedorForm : Form
    {
        private TextBox txtNumero;
        private ComboBox cmbProveedor;
        private Button btnGestionProveedores;
        private TextBox txtCuit;
        private TextBox txtDomicilio;
        private TextBox txtTelefono;
        private DateTimePicker dtpFecha;
        private TextBox txtImporteNeto;
        private TextBox txtImporteIva;
        private TextBox txtImporteTotal;
        private DataGridView dgvIva;
        private Button btnAgregarIva;
        private Button btnEliminarIva;
        private Button btnGuardar;
        private Button btnCancelar;
        private TextBox txtAlicuota;
        private TextBox txtBase;

        // Lista de proveedores cargados
        private List<ProveedorItem> proveedores = new List<ProveedorItem>();

        // Indica que el formulario está inicializando para evitar que el evento SelectedIndexChanged
        // rellene controles mientras se carga el DataSource.
        private bool isInitializing = false;

        public ComprasProveedorForm()
        {
            InitializeComponent();

            // Cargar proveedores al mostrar el formulario, con guard para evitar eventos durante la carga
            this.Load += async (s, e) =>
            {
                isInitializing = true;
                await CargarProveedoresAsync();
                // mantener los controles vacíos hasta que el usuario elija
                cmbProveedor.SelectedIndex = -1;
                isInitializing = false;
            };
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form general
            this.Text = "Registrar Compra Proveedor";
            this.ClientSize = new Size(740, 450);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(250, 250, 250);

            // Header (barra coloreada similar al formulario Productos)
            var headerHeight = 64;
            var pnlHeader = new Panel
            {
                Left = 0,
                Top = 0,
                Width = this.ClientSize.Width,
                Height = headerHeight,
                BackColor = Color.FromArgb(63, 81, 181) // tono indigo similar a captura
            };
            pnlHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var lblIcon = new Label
            {
                Text = "+",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point),
                Left = 12,
                Top = 8,
                AutoSize = true
            };

            var lblTitle = new Label
            {
                Text = "Registrar Compra Proveedor",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Left = lblIcon.Right + 8,
                Top = 12,
                AutoSize = true
            };

            var lblSubtitle = new Label
            {
                Text = "Complete todos los campos de la compra",
                ForeColor = Color.FromArgb(230, 230, 255),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Left = lblIcon.Right + 8,
                Top = lblTitle.Bottom - 6,
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblIcon);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // Contenedor principal de contenido
            var padding = 12;
            var contentTop = pnlHeader.Bottom + padding;
            // Inicial: se asigna un valor alto; se ajustará después para eliminar hueco
            var pnlContent = new Panel
            {
                Left = padding,
                Top = contentTop,
                Width = this.ClientSize.Width - padding * 2,
                Height = this.ClientSize.Height - headerHeight - padding * 2,
                BackColor = Color.White
            };
            pnlContent.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Campos arriba (N° Factura, Proveedor + btn)
            var lblNumero = new Label { Text = "N° Factura", Left = 12, Top = 12, Width = 75, TextAlign = ContentAlignment.MiddleLeft };
            txtNumero = new TextBox { Left = lblNumero.Right + 6, Top = lblNumero.Top - 2, Width = 160 };

            var lblProveedor = new Label { Text = "Proveedor", Left = txtNumero.Right + 25, Top = 12, Width = 75, TextAlign = ContentAlignment.MiddleLeft };
            cmbProveedor = new ComboBox
            {
                Left = lblProveedor.Right + 3,
                Top = lblProveedor.Top - 2,
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                FormattingEnabled = true
            };

            btnGestionProveedores = new Button
            {
                Left = cmbProveedor.Right + 8,
                Top = cmbProveedor.Top,
                Width = 28,
                Height = cmbProveedor.Height,
                Text = "..."
            };
            var toolTip = new ToolTip();
            toolTip.SetToolTip(btnGestionProveedores, "Gestionar Proveedores");

            // Segunda fila: CUIT, Domicilio
            var lblCuit = new Label { Text = "CUIT", Left = 12, Top = lblNumero.Bottom + 12, Width = 75, TextAlign = ContentAlignment.MiddleLeft };
            txtCuit = new TextBox { Left = lblCuit.Right + 6, Top = lblCuit.Top - 2, Width = 160 };

            var lblDomicilio = new Label { Text = "Domicilio", Left = txtCuit.Right + 25, Top = lblCuit.Top, Width = 70, TextAlign = ContentAlignment.MiddleLeft };
            txtDomicilio = new TextBox { Left = lblDomicilio.Right + 8, Top = lblCuit.Top - 2, Width = 335 };

            // Tercera fila: Telefono y Fecha (Telefono más pequeño)
            var lblTelefono = new Label { Text = "Teléfono", Left = 12, Top = lblCuit.Bottom + 12, Width = 75, TextAlign = ContentAlignment.MiddleLeft };
            txtTelefono = new TextBox { Left = lblTelefono.Right + 6, Top = lblTelefono.Top - 2, Width = 160 };

            var lblFecha = new Label { Text = "Fecha", Left = txtTelefono.Right + 37, Top = lblTelefono.Top, Width = 48, TextAlign = ContentAlignment.MiddleLeft };
            dtpFecha = new DateTimePicker { Left = lblFecha.Right + 18, Top = lblTelefono.Top - 2, Width = 140, Format = DateTimePickerFormat.Short };

            // Panel IVA a la izquierda del contenido
            var pnlIva = new Panel
            {
                Left = 12,
                Top = dtpFecha.Bottom + 16,
                Width = 420,
                Height = 200,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 250)
            };

            dgvIva = new DataGridView
            {
                Left = 8,
                Top = 8,
                Width = pnlIva.Width - 24,
                Height = 110,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White
            };
            dgvIva.Columns.Add(new DataGridViewTextBoxColumn { Name = "Alicuota", HeaderText = "Alícuota %", Width = 80 });
            dgvIva.Columns.Add(new DataGridViewTextBoxColumn { Name = "Base", HeaderText = "Base Imponible", Width = 160 });
            dgvIva.Columns.Add(new DataGridViewTextBoxColumn { Name = "ImporteIva", HeaderText = "IVA $", Width = 120 });

            var lblAlicuota = new Label { Text = "Alícuota %", Left = 8, Top = dgvIva.Bottom + 10, Width = 70 };
            txtAlicuota = new TextBox { Left = lblAlicuota.Right + 6, Top = lblAlicuota.Top - 2, Width = 60 };

            var lblBase = new Label { Text = "Base", Left = txtAlicuota.Right + 12, Top = lblAlicuota.Top, Width = 40 };
            txtBase = new TextBox { Left = lblBase.Right + 6, Top = lblAlicuota.Top - 2, Width = 140 };

            // Botones Agregar/Eliminar ubicados debajo de los controles Alicuota/Base (alineados)
            btnAgregarIva = new Button { Text = "Agregar", Left = 80, Top = txtAlicuota.Bottom + 12, Width = 110 };
            btnEliminarIva = new Button { Text = "Eliminar", Left = btnAgregarIva.Right + 12, Top = btnAgregarIva.Top, Width = 110 };

            pnlIva.Controls.Add(dgvIva);
            pnlIva.Controls.Add(lblAlicuota);
            pnlIva.Controls.Add(txtAlicuota);
            pnlIva.Controls.Add(lblBase);
            pnlIva.Controls.Add(txtBase);
            pnlIva.Controls.Add(btnAgregarIva);
            pnlIva.Controls.Add(btnEliminarIva);

            // Totales a la derecha del panel IVA
            var pnlRight = new Panel
            {
                Left = pnlIva.Right + 18,
                Top = pnlIva.Top,
                Width = pnlContent.Width - (pnlIva.Right + 30),
                Height = pnlIva.Height,
                BackColor = Color.White
            };

            var lblNeto = new Label { Text = "Importe Neto", Left = 12, Top = 12, Width = 90, TextAlign = ContentAlignment.MiddleLeft };
            txtImporteNeto = new TextBox { Left = lblNeto.Right + 6, Top = lblNeto.Top - 2, Width = 130, ReadOnly = true };

            var lblIva = new Label { Text = "Importe IVA", Left = 12, Top = lblNeto.Bottom + 14, Width = 90, TextAlign = ContentAlignment.MiddleLeft };
            txtImporteIva = new TextBox { Left = lblIva.Right + 6, Top = lblIva.Top - 2, Width = 130, ReadOnly = true };

            var lblTotal = new Label { Text = "Importe Total", Left = 12, Top = lblIva.Bottom + 14, Width = 90, TextAlign = ContentAlignment.MiddleLeft };
            txtImporteTotal = new TextBox { Left = lblTotal.Right + 6, Top = lblTotal.Top - 2, Width = 130, ReadOnly = true };

            pnlRight.Controls.Add(lblNeto);
            pnlRight.Controls.Add(txtImporteNeto);
            pnlRight.Controls.Add(lblIva);
            pnlRight.Controls.Add(txtImporteIva);
            pnlRight.Controls.Add(lblTotal);
            pnlRight.Controls.Add(txtImporteTotal);

            // Botones Guardar / Cancelar: quitamos el panel footer y los colocamos debajo de Importe Total
            btnGuardar = new Button
            {
                Text = "Guardar",
                Width = 120,
                Height = 36,
                Left = lblNeto.Left, // alineado con el campo Importe Total
                Top = txtImporteTotal.Bottom + 12,
                BackColor = Color.FromArgb(60, 179, 113), // verde similar
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnGuardar.FlatAppearance.BorderSize = 0;

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Width = 120,
                Height = 36,
                Left = btnGuardar.Right + 12,
                Top = btnGuardar.Top,
                BackColor = Color.FromArgb(160, 160, 160),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnCancelar.FlatAppearance.BorderSize = 0;

            pnlRight.Controls.Add(btnGuardar);
            pnlRight.Controls.Add(btnCancelar);

            // Añadir controles al panel content
            pnlContent.Controls.Add(lblNumero);
            pnlContent.Controls.Add(txtNumero);
            pnlContent.Controls.Add(lblProveedor);
            pnlContent.Controls.Add(cmbProveedor);
            pnlContent.Controls.Add(btnGestionProveedores);
            pnlContent.Controls.Add(lblCuit);
            pnlContent.Controls.Add(txtCuit);
            pnlContent.Controls.Add(lblDomicilio);
            pnlContent.Controls.Add(txtDomicilio);
            pnlContent.Controls.Add(lblTelefono);
            pnlContent.Controls.Add(txtTelefono);
            pnlContent.Controls.Add(lblFecha);
            pnlContent.Controls.Add(dtpFecha);
            pnlContent.Controls.Add(pnlIva);
            pnlContent.Controls.Add(pnlRight);

            // Ajuste dinámico de alturas para eliminar espacio vacío inferior
            // btnGuardar.Bottom es relativo a pnlRight; convertimos a coordenada de pnlContent
            int bottomButtonsAbsolute = pnlRight.Top + btnGuardar.Bottom;
            int neededContentHeight = Math.Max(pnlIva.Bottom, bottomButtonsAbsolute) + 12;
            pnlContent.Height = neededContentHeight;

            // Ajustar tamaño del formulario justo para el header + padding + contenido + pequeño margen
            int newFormHeight = pnlHeader.Height + padding + pnlContent.Height + padding;
            this.ClientSize = new Size(this.ClientSize.Width, newFormHeight);

            // Actualizar anchos dependientes del form width
            pnlHeader.Width = this.ClientSize.Width;
            pnlContent.Width = this.ClientSize.Width - padding * 2;
            pnlRight.Width = pnlContent.Width - (pnlIva.Right + 30);

            // Añadir todos los paneles al form
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlContent);

            // eventos (mantener lógica original)
            btnAgregarIva.Click += BtnAgregarIva_Click;
            btnEliminarIva.Click += BtnEliminarIva_Click;
            dgvIva.RowsRemoved += (s, e) => RecalcularTotales();
            btnGuardar.Click += async (s, e) => await BtnGuardar_ClickAsync();
            btnCancelar.Click += (s, e) => this.Close();
            btnGestionProveedores.Click += async (s, e) => await AbrirABMProveedoresAsync();
            cmbProveedor.SelectedIndexChanged += CmbProveedor_SelectedIndexChanged;

            // Habilitar que Enter funcione como Tab en TextBox (no afecta TextBox multiline)
            EnableEnterAsTab(this);

            // Ajustes finales
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        // Hace que la tecla Enter funcione como Tab en todos los TextBox no-multiline del control (recursivo)
        private void EnableEnterAsTab(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is TextBox tb)
                {
                    if (!tb.Multiline)
                    {
                        tb.KeyDown -= TextBox_EnterAsTab_KeyDown;
                        tb.KeyDown += TextBox_EnterAsTab_KeyDown;
                    }
                }
                else
                {
                    // recursivo para contenedores
                    if (c.HasChildren)
                        EnableEnterAsTab(c);
                }
            }
        }

        private void TextBox_EnterAsTab_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // evita el ding
                // Mover al siguiente control en el orden de tabulación
                var current = sender as Control;
                if (current != null)
                {
                    this.SelectNextControl(current, true, true, true, true);
                }
            }
        }

        private void CmbProveedor_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Evitar llenar controles durante la carga inicial del form/dataSource
            if (isInitializing) return;

            if (cmbProveedor.SelectedItem is ProveedorItem p)
            {
                txtCuit.Text = p.Cuit ?? "";
                txtDomicilio.Text = p.Domicilio ?? "";
                txtTelefono.Text = p.Telefono ?? "";
            }
            else
            {
                // Ningún proveedor seleccionado: mantener controles vacíos
                txtCuit.Text = "";
                txtDomicilio.Text = "";
                txtTelefono.Text = "";
            }
        }

        private void BtnAgregarIva_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtAlicuota.Text.Trim().Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal alicuota))
            {
                MessageBox.Show("Alicuota inválida.");
                return;
            }
            if (!decimal.TryParse(txtBase.Text.Trim().Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal baseImponible))
            {
                MessageBox.Show("Base imponible inválida.");
                return;
            }

            decimal importeIva = Math.Round(baseImponible * alicuota / 100m, 2);
            dgvIva.Rows.Add(alicuota.ToString("F2", CultureInfo.InvariantCulture), baseImponible.ToString("F2", CultureInfo.InvariantCulture), importeIva.ToString("F2", CultureInfo.InvariantCulture));

            txtAlicuota.Clear();
            txtBase.Clear();
            RecalcularTotales();

            // devolver el foco a Alicuota para entrada rápida de la siguiente fila
            txtAlicuota.Focus();
            txtAlicuota.SelectAll();
        }

        private void BtnEliminarIva_Click(object sender, EventArgs e)
        {
            if (dgvIva.SelectedRows.Count > 0)
            {
                dgvIva.Rows.RemoveAt(dgvIva.SelectedRows[0].Index);
            }
        }

        private void RecalcularTotales()
        {
            decimal sumaBase = 0m, sumaIva = 0m;
            foreach (DataGridViewRow r in dgvIva.Rows)
            {
                if (r.Cells["Base"].Value != null && decimal.TryParse(r.Cells["Base"].Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal b))
                    sumaBase += b;
                if (r.Cells["ImporteIva"].Value != null && decimal.TryParse(r.Cells["ImporteIva"].Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal i))
                    sumaIva += i;
            }

            txtImporteNeto.Text = sumaBase.ToString("C2", CultureInfo.CurrentCulture);
            txtImporteIva.Text = sumaIva.ToString("C2", CultureInfo.CurrentCulture);
            txtImporteTotal.Text = (sumaBase + sumaIva).ToString("C2", CultureInfo.CurrentCulture);
        }

        private string GetConnectionString()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return config.GetConnectionString("DefaultConnection");
        }

        // Carga proveedores activos y los vincula al ComboBox
        private async Task CargarProveedoresAsync()
        {
            try
            {
                proveedores.Clear();

                string cs = GetConnectionString();
                using (var conn = new SqlConnection(cs))
                {
                    var sql = @"SELECT Id, Nombre, CUIT, Domicilio, Telefono FROM Proveedores WHERE Activo = 1 ORDER BY Nombre";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        await conn.OpenAsync();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int id = reader.GetInt32(0);
                                string nombre = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();
                                string cuit = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();
                                string domicilio = reader.IsDBNull(3) ? "" : reader.GetString(3).Trim();
                                string telefono = reader.IsDBNull(4) ? "" : reader.GetString(4).Trim();

                                if (string.IsNullOrWhiteSpace(nombre)) continue;
                                proveedores.Add(new ProveedorItem { Id = id, Nombre = nombre, Cuit = cuit, Domicilio = domicilio, Telefono = telefono });
                            }
                        }
                    }
                }

                // Bind al ComboBox
                cmbProveedor.DataSource = null;
                cmbProveedor.DataSource = proveedores;
                cmbProveedor.DisplayMember = nameof(ProveedorItem.Nombre);
                cmbProveedor.ValueMember = nameof(ProveedorItem.Id);

                // dejar la selección vacía (se gestionará por isInitializing en Load)
                // cmbProveedor.SelectedIndex = -1; // now set by the Load handler
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando proveedores: {ex.Message}");
            }
        }

        // Abre el ABM de Proveedores y refresca la lista al cerrar; selecciona el proveedor creado/actualizado
        private async Task AbrirABMProveedoresAsync()
        {
            try
            {
                int? lastId = null;
                using (var frm = new ProveedoresForm())
                {
                    frm.ShowDialog(this);
                    lastId = frm.LastSavedProveedorId;
                }

                // Refrescar proveedores después de cerrar ABM
                await CargarProveedoresAsync();

                // Si se creó/editar un proveedor, seleccionarlo automáticamente
                if (lastId.HasValue)
                {
                    var match = proveedores.FirstOrDefault(p => p.Id == lastId.Value);
                    if (match != null)
                    {
                        cmbProveedor.SelectedItem = match;
                        cmbProveedor.Text = match.Nombre;
                        txtCuit.Text = match.Cuit ?? "";
                        txtDomicilio.Text = match.Domicilio ?? "";
                        txtTelefono.Text = match.Telefono ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo ABM de Proveedores: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Intentar obtener el Id del proveedor según lo seleccionado o escrito
        private int? ObtenerProveedorIdSeleccionado()
        {
            if (cmbProveedor.SelectedItem is ProveedorItem sel)
                return sel.Id;

            // Si el usuario escribió un nombre que coincide exactamente con alguno de la lista
            var text = cmbProveedor.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return null;
            var match = proveedores.FirstOrDefault(p => string.Equals(p.Nombre, text, StringComparison.CurrentCultureIgnoreCase));
            return match?.Id;
        }

        private async Task BtnGuardar_ClickAsync()
        {
            if (string.IsNullOrWhiteSpace(txtNumero.Text) || string.IsNullOrWhiteSpace(cmbProveedor.Text))
            {
                MessageBox.Show("Complete número y proveedor.");
                return;
            }

            // calcular valores numéricos
            decimal sumaBase = 0m, sumaIva = 0m;
            foreach (DataGridViewRow r in dgvIva.Rows)
            {
                if (r.Cells["Base"].Value != null && decimal.TryParse(r.Cells["Base"].Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal b))
                    sumaBase += b;
                if (r.Cells["ImporteIva"].Value != null && decimal.TryParse(r.Cells["ImporteIva"].Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal i))
                    sumaIva += i;
            }
            decimal total = sumaBase + sumaIva;

            string connectionString = GetConnectionString();

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            // intentar resolver proveedor existente
                            int? proveedorId = ObtenerProveedorIdSeleccionado();

                            var insertSql = @"INSERT INTO ComprasProveedores
                                (NumeroFactura, Fecha, Proveedor, ProveedorId, CUIT, ImporteNeto, ImporteIVA, ImporteTotal, EsCtaCte, NombreCtaCte, Observaciones, Usuario)
                                VALUES (@Numero, @Fecha, @Proveedor, @ProveedorId, @CUIT, @ImporteNeto, @ImporteIVA, @ImporteTotal, @EsCtaCte, @NombreCtaCte, @Observaciones, @Usuario);
                                SELECT CAST(SCOPE_IDENTITY() AS INT);";

                            int compraId;
                            using (var cmd = new SqlCommand(insertSql, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Numero", txtNumero.Text.Trim());
                                cmd.Parameters.AddWithValue("@Fecha", dtpFecha.Value.Date);
                                cmd.Parameters.AddWithValue("@Proveedor", cmbProveedor.Text.Trim());
                                cmd.Parameters.AddWithValue("@ProveedorId", proveedorId.HasValue ? (object)proveedorId.Value : DBNull.Value);
                                cmd.Parameters.AddWithValue("@CUIT", string.IsNullOrWhiteSpace(txtCuit.Text) ? (object)DBNull.Value : txtCuit.Text.Trim());
                                cmd.Parameters.AddWithValue("@ImporteNeto", sumaBase);
                                cmd.Parameters.AddWithValue("@ImporteIVA", sumaIva);
                                cmd.Parameters.AddWithValue("@ImporteTotal", total);
                                cmd.Parameters.AddWithValue("@EsCtaCte", false);
                                cmd.Parameters.AddWithValue("@NombreCtaCte", DBNull.Value);
                                cmd.Parameters.AddWithValue("@Observaciones", DBNull.Value);
                                cmd.Parameters.AddWithValue("@Usuario", Environment.UserName);

                                var res = await cmd.ExecuteScalarAsync();
                                compraId = res != null && int.TryParse(res.ToString(), out int id) ? id : 0;
                            }

                            // insertar detalles IVA
                            var insertDet = @"INSERT INTO ComprasProveedoresIvaDetalle
                                (CompraId, Alicuota, BaseImponible, ImporteIva)
                                VALUES (@CompraId, @Alicuota, @BaseImponible, @ImporteIva);";

                            foreach (DataGridViewRow r in dgvIva.Rows)
                            {
                                if (r.IsNewRow) continue;
                                if (!decimal.TryParse(r.Cells["Alicuota"].Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal ali)) continue;
                                if (!decimal.TryParse(r.Cells["Base"].Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal baseVal)) continue;
                                if (!decimal.TryParse(r.Cells["ImporteIva"].Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal ivaVal)) continue;

                                using (var cmdDet = new SqlCommand(insertDet, conn, tx))
                                {
                                    cmdDet.Parameters.AddWithValue("@CompraId", compraId);
                                    cmdDet.Parameters.AddWithValue("@Alicuota", ali);
                                    cmdDet.Parameters.AddWithValue("@BaseImponible", baseVal);
                                    cmdDet.Parameters.AddWithValue("@ImporteIva", ivaVal);
                                    await cmdDet.ExecuteNonQueryAsync();
                                }
                            }

                            tx.Commit();
                            MessageBox.Show("Compra guardada correctamente.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.Close();
                        }
                        catch (Exception exTx)
                        {
                            tx.Rollback();
                            MessageBox.Show($"Error guardando compra: {exTx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Clase simple para bind del ComboBox
        private class ProveedorItem
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public string Cuit { get; set; }
            public string Domicilio { get; set; }
            public string Telefono { get; set; }

            public override string ToString() => Nombre;
        }
    }
}