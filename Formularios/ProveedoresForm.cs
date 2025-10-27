using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public class ProveedoresForm : Form
    {
        private DataGridView dgv;
        private Button btnAgregar;
        private Button btnEditar;
        private Button btnEliminar;
        private Button btnRefrescar;
        private TextBox txtBuscar;
        private Label lblBuscar;

        // Nuevo: Id del último proveedor creado/guardado desde este form
        public int? LastSavedProveedorId { get; private set; }

        // Elementos de estilo
        private Panel pnlHeader;
        private Panel pnlContent;

        public ProveedoresForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Form general
            this.Text = "Proveedores";
            this.ClientSize = new Size(900, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(250, 250, 250);

            // Header
            var headerHeight = 64;
            pnlHeader = new Panel
            {
                Left = 0,
                Top = 0,
                Width = this.ClientSize.Width,
                Height = headerHeight,
                BackColor = Color.FromArgb(63, 81, 181) // indigo
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
                Text = "Proveedores",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Left = lblIcon.Right + 8,
                Top = 12,
                AutoSize = true
            };

            var lblSubtitle = new Label
            {
                Text = "Alta, edición y listado de proveedores",
                ForeColor = Color.FromArgb(230, 230, 255),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Left = lblIcon.Right + 8,
                Top = lblTitle.Bottom - 6,
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblIcon);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // Contenedor principal (panel blanco con padding)
            var padding = 12;
            var contentTop = pnlHeader.Bottom + padding;
            pnlContent = new Panel
            {
                Left = padding,
                Top = contentTop,
                Width = this.ClientSize.Width - padding * 2,
                Height = this.ClientSize.Height - headerHeight - padding * 2,
                BackColor = Color.White
            };
            pnlContent.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            // Controles dentro de pnlContent
            lblBuscar = new Label { Text = "Buscar:", Left = 12, Top = 12, Width = 60 };
            txtBuscar = new TextBox { Left = lblBuscar.Right + 6, Top = 10, Width = 320 };
            txtBuscar.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await CargarProveedoresAsync(txtBuscar.Text.Trim());
                }
            };

            btnRefrescar = new Button
            {
                Text = "Refrescar",
                Left = txtBuscar.Right + 12,
                Top = 8,
                Width = 100,
                BackColor = Color.FromArgb(160, 160, 160),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnRefrescar.FlatAppearance.BorderSize = 0;
            btnRefrescar.Click += async (s, e) => await CargarProveedoresAsync(txtBuscar.Text.Trim());

            dgv = new DataGridView
            {
                Left = 12,
                Top = lblBuscar.Bottom + 12,
                Width = pnlContent.Width - 24,
                Height = pnlContent.Height - 120,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false
            };

            btnAgregar = new Button
            {
                Text = "Agregar",
                Left = 12,
                Top = pnlContent.Height - 52,
                Width = 110,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.FromArgb(60, 179, 113),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnAgregar.FlatAppearance.BorderSize = 0;
            btnAgregar.Click += BtnAgregar_Click;

            btnEditar = new Button
            {
                Text = "Editar",
                Left = btnAgregar.Right + 8,
                Top = btnAgregar.Top,
                Width = 110,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.FromArgb(120, 120, 120),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnEditar.FlatAppearance.BorderSize = 0;
            btnEditar.Click += BtnEditar_Click;

            btnEliminar = new Button
            {
                Text = "Eliminar",
                Left = btnEditar.Right + 8,
                Top = btnAgregar.Top,
                Width = 110,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.FromArgb(160, 160, 160),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnEliminar.FlatAppearance.BorderSize = 0;
            btnEliminar.Click += BtnEliminar_Click;

            // Añadir controles al panel de contenido
            pnlContent.Controls.AddRange(new Control[] { lblBuscar, txtBuscar, btnRefrescar, dgv, btnAgregar, btnEditar, btnEliminar });

            // Añadir panels al form
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlContent);

            // Manejo de resize para mantener layout
            this.Resize += (s, e) =>
            {
                pnlHeader.Width = this.ClientSize.Width;
                pnlContent.Left = padding;
                pnlContent.Top = pnlHeader.Bottom + padding;
                pnlContent.Width = this.ClientSize.Width - padding * 2;
                pnlContent.Height = Math.Max(140, this.ClientSize.Height - pnlHeader.Height - padding * 2);

                dgv.Width = pnlContent.ClientSize.Width - 24;
                dgv.Height = pnlContent.ClientSize.Height - (lblBuscar.Bottom + 64);

                btnAgregar.Top = pnlContent.ClientSize.Height - 52;
                btnEditar.Top = btnAgregar.Top;
                btnEliminar.Top = btnAgregar.Top;

                // Mantener botones en fila izquierda
                btnAgregar.Left = 12;
                btnEditar.Left = btnAgregar.Right + 8;
                btnEliminar.Left = btnEditar.Right + 8;

                // Alinear búsqueda/refresh
                txtBuscar.Width = Math.Max(160, pnlContent.ClientSize.Width - 12 - 100 - 160);
                btnRefrescar.Left = txtBuscar.Right + 12;
            };

            // Load
            this.Load += async (s, e) =>
            {
                LastSavedProveedorId = null;
                await CargarProveedoresAsync();
            };
        }

        private string GetConnectionString()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return config.GetConnectionString("DefaultConnection");
        }

        public async Task CargarProveedoresAsync(string filtro = "")
        {
            try
            {
                string cs = GetConnectionString();
                using (var conn = new SqlConnection(cs))
                {
                    var sql = @"SELECT Id, Nombre, CUIT, Domicilio, Telefono, Email, CondicionIVA, Activo
                                FROM Proveedores
                                WHERE (@filtro = '' OR Nombre LIKE '%' + @filtro + '%' OR CUIT LIKE '%' + @filtro + '%')
                                ORDER BY Nombre";
                    using (var da = new SqlDataAdapter(sql, conn))
                    {
                        da.SelectCommand.Parameters.AddWithValue("@filtro", filtro ?? "");
                        var dt = new DataTable();
                        await Task.Run(() => da.Fill(dt));
                        dgv.DataSource = dt;
                        if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
                        if (dgv.Columns["Nombre"] != null) dgv.Columns["Nombre"].HeaderText = "Nombre";
                        if (dgv.Columns["CUIT"] != null) dgv.Columns["CUIT"].HeaderText = "CUIT";
                        if (dgv.Columns["Domicilio"] != null) dgv.Columns["Domicilio"].HeaderText = "Domicilio";
                        if (dgv.Columns["Telefono"] != null) dgv.Columns["Telefono"].HeaderText = "Teléfono";
                        if (dgv.Columns["Email"] != null) dgv.Columns["Email"].HeaderText = "Email";
                        if (dgv.Columns["CondicionIVA"] != null) dgv.Columns["CondicionIVA"].HeaderText = "Condición IVA";
                        if (dgv.Columns["Activo"] != null) dgv.Columns["Activo"].HeaderText = "Activo";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando proveedores: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnAgregar_Click(object sender, EventArgs e)
        {
            using (var dlg = new ProveedorEditForm())
            {
                var res = dlg.ShowDialog(this);
                if (res == DialogResult.OK)
                {
                    // Capturar Id creado y re-cargar
                    LastSavedProveedorId = dlg.ProveedorIdResult;
                    await CargarProveedoresAsync(txtBuscar.Text.Trim());
                }
            }
        }

        private async void BtnEditar_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un proveedor para editar.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgv.SelectedRows[0];
            int id = Convert.ToInt32(row.Cells["Id"].Value);
            string nombre = row.Cells["Nombre"].Value?.ToString();
            string cuit = row.Cells["CUIT"]?.Value?.ToString();
            string domicilio = row.Cells["Domicilio"]?.Value?.ToString();
            string telefono = row.Cells["Telefono"]?.Value?.ToString();
            // Corregido: leer Value en lugar de ToString() de la celda
            string email = row.Cells["Email"]?.Value?.ToString();
            string condicion = row.Cells["CondicionIVA"]?.Value?.ToString();
            bool activo = row.Cells["Activo"] != null && Convert.ToBoolean(row.Cells["Activo"].Value);

            using (var dlg = new ProveedorEditForm(id, nombre, cuit, domicilio, telefono, email, condicion, activo))
            {
                var res = dlg.ShowDialog(this);
                if (res == DialogResult.OK)
                {
                    // Capturar Id editado y recargar
                    LastSavedProveedorId = dlg.ProveedorIdResult ?? id;
                    await CargarProveedoresAsync(txtBuscar.Text.Trim());
                }
            }
        }

        private async void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un proveedor para eliminar.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgv.SelectedRows[0];
            int id = Convert.ToInt32(row.Cells["Id"].Value);
            string nombre = row.Cells["Nombre"].Value?.ToString();

            var confirmar = MessageBox.Show($"¿Confirma eliminar al proveedor '{nombre}'? (Se marcará como inactivo)", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmar != DialogResult.Yes) return;

            try
            {
                string cs = GetConnectionString();
                using (var conn = new SqlConnection(cs))
                {
                    await conn.OpenAsync();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            var cmd = new SqlCommand("UPDATE Proveedores SET Activo = 0 WHERE Id = @id", conn, tx);
                            cmd.Parameters.AddWithValue("@id", id);
                            await cmd.ExecuteNonQueryAsync();
                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
                await CargarProveedoresAsync(txtBuscar.Text.Trim());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error eliminando proveedor: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}