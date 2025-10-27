using Microsoft.Extensions.Configuration;
using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public class ProveedorEditForm : Form
    {
        private TextBox txtNombre;
        private TextBox txtCuit;
        private TextBox txtDomicilio;
        private TextBox txtTelefono;
        private TextBox txtEmail;
        private TextBox txtCondicion;
        private CheckBox chkActivo;
        private Button btnAceptar;
        private Button btnCancelar;

        private int? proveedorId;

        // Nuevo: Id del proveedor creado/actualizado (expuesto al llamador)
        public int? ProveedorIdResult { get; private set; }

        // Elementos de estilo
        private Panel pnlHeader;
        private Panel pnlContent;

        public ProveedorEditForm(int? id = null, string nombre = "", string cuit = "", string domicilio = "", string telefono = "", string email = "", string condicion = "", bool activo = true)
        {
            proveedorId = id;
            InitializeComponent();

            txtNombre.Text = nombre;
            txtCuit.Text = cuit;
            txtDomicilio.Text = domicilio;
            txtTelefono.Text = telefono;
            txtEmail.Text = email;
            txtCondicion.Text = condicion;
            chkActivo.Checked = activo;

            this.Text = id.HasValue ? "Editar Proveedor" : "Agregar Proveedor";
        }

        private void InitializeComponent()
        {
            // Form general y estilo coherente
            this.ClientSize = new Size(520, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
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
                BackColor = Color.FromArgb(63, 81, 181)
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
                Text = this.Text,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Left = lblIcon.Right + 8,
                Top = 12,
                AutoSize = true
            };

            var lblSubtitle = new Label
            {
                Text = "Complete los datos del proveedor",
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
            var lblNombre = new Label { Text = "Nombre", Left = 12, Top = 12, Width = 100 };
            txtNombre = new TextBox { Left = 120, Top = lblNombre.Top - 2, Width = 380 };

            var lblCuit = new Label { Text = "CUIT", Left = 12, Top = lblNombre.Bottom + 12, Width = 100 };
            txtCuit = new TextBox { Left = 120, Top = lblCuit.Top - 2, Width = 200 };

            var lblDomicilio = new Label { Text = "Domicilio", Left = 12, Top = lblCuit.Bottom + 12, Width = 100 };
            txtDomicilio = new TextBox { Left = 120, Top = lblDomicilio.Top - 2, Width = 380 };

            var lblTelefono = new Label { Text = "Teléfono", Left = 12, Top = lblDomicilio.Bottom + 12, Width = 100 };
            txtTelefono = new TextBox { Left = 120, Top = lblTelefono.Top - 2, Width = 200 };

            var lblEmail = new Label { Text = "Email", Left = 12, Top = lblTelefono.Bottom + 12, Width = 100 };
            txtEmail = new TextBox { Left = 120, Top = lblEmail.Top - 2, Width = 260 };

            var lblCond = new Label { Text = "Condición IVA", Left = 12, Top = lblEmail.Bottom + 12, Width = 100 };
            txtCondicion = new TextBox { Left = 120, Top = lblCond.Top - 2, Width = 200 };

            chkActivo = new CheckBox { Text = "Activo", Left = 120, Top = txtCondicion.Bottom + 12, Checked = true };

            // Botones estilo plano y colores coherentes
            btnAceptar = new Button
            {
                Text = "Aceptar",
                Width = 110,
                Height = 36,
                BackColor = Color.FromArgb(60, 179, 113),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnAceptar.FlatAppearance.BorderSize = 0;
            btnAceptar.Click += async (s, e) => await GuardarAsync();

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Width = 110,
                Height = 36,
                BackColor = Color.FromArgb(160, 160, 160),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                DialogResult = DialogResult.Cancel
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            // Posicionar botones al final (alineados a la derecha dentro del panel)
            btnCancelar.Left = pnlContent.Width - padding - btnCancelar.Width;
            btnCancelar.Top = pnlContent.Height - padding - btnCancelar.Height;
            btnAceptar.Left = btnCancelar.Left - 12 - btnAceptar.Width;
            btnAceptar.Top = btnCancelar.Top;

            // Añadir controles al panel de contenido
            pnlContent.Controls.AddRange(new Control[] {
                lblNombre, txtNombre, lblCuit, txtCuit, lblDomicilio, txtDomicilio,
                lblTelefono, txtTelefono, lblEmail, txtEmail, lblCond, txtCondicion,
                chkActivo, btnAceptar, btnCancelar
            });

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

                // reajustar posiciones de campos y botones según nuevo tamaño
                txtNombre.Width = Math.Max(200, pnlContent.ClientSize.Width - 140);
                txtDomicilio.Width = txtNombre.Width;
                txtEmail.Width = Math.Max(160, pnlContent.ClientSize.Width - 320);

                btnCancelar.Left = pnlContent.ClientSize.Width - padding - btnCancelar.Width;
                btnCancelar.Top = pnlContent.ClientSize.Height - padding - btnCancelar.Height;
                btnAceptar.Left = btnCancelar.Left - 12 - btnAceptar.Width;
                btnAceptar.Top = btnCancelar.Top;
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

        private async Task GuardarAsync()
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
                            if (proveedorId.HasValue)
                            {
                                var cmd = new SqlCommand(@"UPDATE Proveedores
                                                           SET Nombre = @Nombre, CUIT = @CUIT, Domicilio = @Domicilio,
                                                               Telefono = @Telefono, Email = @Email, CondicionIVA = @Condicion, Activo = @Activo
                                                           WHERE Id = @Id", conn, tx);
                                cmd.Parameters.AddWithValue("@Nombre", txtNombre.Text.Trim());
                                cmd.Parameters.AddWithValue("@CUIT", string.IsNullOrWhiteSpace(txtCuit.Text) ? (object)DBNull.Value : txtCuit.Text.Trim());
                                cmd.Parameters.AddWithValue("@Domicilio", string.IsNullOrWhiteSpace(txtDomicilio.Text) ? (object)DBNull.Value : txtDomicilio.Text.Trim());
                                cmd.Parameters.AddWithValue("@Telefono", string.IsNullOrWhiteSpace(txtTelefono.Text) ? (object)DBNull.Value : txtTelefono.Text.Trim());
                                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text.Trim());
                                cmd.Parameters.AddWithValue("@Condicion", string.IsNullOrWhiteSpace(txtCondicion.Text) ? (object)DBNull.Value : txtCondicion.Text.Trim());
                                cmd.Parameters.AddWithValue("@Activo", chkActivo.Checked);
                                cmd.Parameters.AddWithValue("@Id", proveedorId.Value);
                                await cmd.ExecuteNonQueryAsync();

                                // Nuevo: exponer Id actualizado
                                ProveedorIdResult = proveedorId.Value;
                            }
                            else
                            {
                                var cmd = new SqlCommand(@"INSERT INTO Proveedores
                                                           (Nombre, CUIT, Domicilio, Telefono, Email, CondicionIVA, Activo, FechaCreacion, UsuarioCreacion)
                                                           VALUES (@Nombre, @CUIT, @Domicilio, @Telefono, @Email, @Condicion, @Activo, SYSUTCDATETIME(), @Usuario);
                                                           SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);
                                cmd.Parameters.AddWithValue("@Nombre", txtNombre.Text.Trim());
                                cmd.Parameters.AddWithValue("@CUIT", string.IsNullOrWhiteSpace(txtCuit.Text) ? (object)DBNull.Value : txtCuit.Text.Trim());
                                cmd.Parameters.AddWithValue("@Domicilio", string.IsNullOrWhiteSpace(txtDomicilio.Text) ? (object)DBNull.Value : txtDomicilio.Text.Trim());
                                cmd.Parameters.AddWithValue("@Telefono", string.IsNullOrWhiteSpace(txtTelefono.Text) ? (object)DBNull.Value : txtTelefono.Text.Trim());
                                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text.Trim());
                                cmd.Parameters.AddWithValue("@Condicion", string.IsNullOrWhiteSpace(txtCondicion.Text) ? (object)DBNull.Value : txtCondicion.Text.Trim());
                                cmd.Parameters.AddWithValue("@Activo", chkActivo.Checked);
                                cmd.Parameters.AddWithValue("@Usuario", Environment.UserName ?? "Sistema");

                                var res = await cmd.ExecuteScalarAsync();
                                proveedorId = res != null && int.TryParse(res.ToString(), out int id) ? id : (int?)null;

                                // Nuevo: exponer Id creado
                                ProveedorIdResult = proveedorId;
                            }

                            tx.Commit();
                            this.DialogResult = DialogResult.OK;
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error guardando proveedor: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}