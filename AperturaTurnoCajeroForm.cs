using Comercio.NET.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class AperturaTurnoCajeroForm : Form
    {
        private Label lblCajeroAsignado; // ✅ CAMBIO: Label en lugar de ComboBox
        private TextBox txtMontoInicial;
        private TextBox txtObservaciones;
        private Button btnAbrirTurno, btnCancelar;
        private Label lblEstadoTurno;
        private Panel panelInfo;
        private int numeroCajeroLogueado = -1; // ✅ NUEVO: Almacenar número de cajero

        public AperturaTurnoCajeroForm()
        {
            InitializeComponent();
            ConfigurarFormulario();
            _ = CargarCajeroLogueado(); // ✅ CAMBIO: Cargar solo el cajero logueado
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new Size(550, 400);
            this.MinimumSize = new Size(550, 400);
            this.Name = "AperturaTurnoCajeroForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Apertura de Turno de Cajero";
            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            this.Text = "🔓 Apertura de Turno";
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = new Font("Segoe UI", 9F);

            CrearControles();
            ConfigurarEventos();
        }

        private void CrearControles()
        {
            int margin = 15;
            int currentY = 15;

            // Título compacto
            var lblTitulo = new Label
            {
                Text = "🔓 APERTURA DE TURNO",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80),
                Location = new Point(margin, currentY),
                Size = new Size(520, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitulo);
            currentY += 35;

            // Panel de información compacto
            panelInfo = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(520, 335),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelInfo);

            // Estado del turno
            lblEstadoTurno = new Label
            {
                Text = "Estado: Verificando...",
                Location = new Point(15, 15),
                Size = new Size(490, 28),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 152, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(255, 243, 224),
                BorderStyle = BorderStyle.FixedSingle
            };
            panelInfo.Controls.Add(lblEstadoTurno);

            // ✅ CAMBIO: Cajero como Label (solo lectura)
            panelInfo.Controls.Add(new Label
            {
                Text = "Cajero:",
                Location = new Point(15, 58),
                Size = new Size(80, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            lblCajeroAsignado = new Label
            {
                Location = new Point(100, 56),
                Size = new Size(405, 22),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Text = "Cargando...",
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 2, 0, 0)
            };
            panelInfo.Controls.Add(lblCajeroAsignado);

            // Fecha y Hora
            panelInfo.Controls.Add(new Label
            {
                Text = "Fecha/Hora:",
                Location = new Point(15, 88),
                Size = new Size(80, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            var lblFechaHora = new Label
            {
                Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                Location = new Point(100, 88),
                Size = new Size(405, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(63, 81, 181)
            };
            panelInfo.Controls.Add(lblFechaHora);

            // Timer para actualizar fecha/hora
            var timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += (s, e) => lblFechaHora.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            timer.Start();

            // Usuario
            panelInfo.Controls.Add(new Label
            {
                Text = "Usuario:",
                Location = new Point(15, 118),
                Size = new Size(80, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            string usuarioActual = AuthenticationService.SesionActual?.Usuario?.NombreUsuario ?? "Sistema";
            panelInfo.Controls.Add(new Label
            {
                Text = usuarioActual,
                Location = new Point(100, 118),
                Size = new Size(405, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(63, 81, 181)
            });

            // Monto Inicial
            panelInfo.Controls.Add(new Label
            {
                Text = "Monto Inicial:",
                Location = new Point(15, 148),
                Size = new Size(80, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            txtMontoInicial = new TextBox
            {
                Location = new Point(100, 146),
                Size = new Size(120, 22),
                Font = new Font("Segoe UI", 9F),
                Text = "0.00",
                TextAlign = HorizontalAlignment.Right
            };
            txtMontoInicial.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',')
                {
                    e.Handled = true;
                }
                if ((e.KeyChar == '.' || e.KeyChar == ',') && (txtMontoInicial.Text.Contains(".") || txtMontoInicial.Text.Contains(",")))
                {
                    e.Handled = true;
                }
            };
            panelInfo.Controls.Add(txtMontoInicial);

            panelInfo.Controls.Add(new Label
            {
                Text = "💡 Efectivo inicial del turno",
                Location = new Point(230, 148),
                Size = new Size(275, 20),
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.Gray
            });

            // Observaciones
            panelInfo.Controls.Add(new Label
            {
                Text = "Observaciones:",
                Location = new Point(15, 178),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            txtObservaciones = new TextBox
            {
                Location = new Point(15, 203),
                Size = new Size(490, 85),
                Font = new Font("Segoe UI", 8F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "Notas opcionales sobre la apertura del turno..."
            };
            panelInfo.Controls.Add(txtObservaciones);

            // Botones
            btnAbrirTurno = new Button
            {
                Text = "🔓 Abrir Turno",
                Location = new Point(15, 295),
                Size = new Size(160, 32),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Enabled = false
            };
            btnAbrirTurno.FlatAppearance.BorderSize = 0;
            panelInfo.Controls.Add(btnAbrirTurno);

            btnCancelar = new Button
            {
                Text = "❌ Cancelar",
                Location = new Point(185, 295),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            panelInfo.Controls.Add(btnCancelar);
        }

        private void ConfigurarEventos()
        {
            btnAbrirTurno.Click += async (s, e) => await AbrirTurno();
            btnCancelar.Click += (s, e) => this.Close();
        }

        // ✅ NUEVO MÉTODO: Cargar el cajero del usuario logueado
        private async Task CargarCajeroLogueado()
        {
            try
            {
                var usuarioActual = AuthenticationService.SesionActual?.Usuario;
                
                if (usuarioActual == null)
                {
                    MessageBox.Show("No hay sesión activa", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                numeroCajeroLogueado = usuarioActual.NumeroCajero;

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                connection.Open();
                
                var query = @"
                    SELECT TOP 1 COALESCE(Nombre + ' ' + Apellido, 'Cajero ' + CAST(NumeroCajero AS NVARCHAR)) as NombreCajero
                    FROM Usuarios
                    WHERE NumeroCajero = @numeroCajero
                    AND Activo = 1";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@numeroCajero", numeroCajeroLogueado);

                var nombreCajero = await cmd.ExecuteScalarAsync();
                
                if (nombreCajero != null)
                {
                    lblCajeroAsignado.Text = $"Cajero #{numeroCajeroLogueado} - {nombreCajero}";
                    await VerificarEstadoTurno();
                }
                else
                {
                    MessageBox.Show(
                        $"El usuario '{usuarioActual.NombreUsuario}' no tiene un cajero asignado",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando cajero: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        // ✅ MODIFICADO: Usa numeroCajeroLogueado en lugar de ComboBox
        private async Task VerificarEstadoTurno()
        {
            try
            {
                if (numeroCajeroLogueado <= 0)
                {
                    lblEstadoTurno.Text = "⚠️ No se pudo identificar el cajero";
                    lblEstadoTurno.ForeColor = Color.FromArgb(244, 67, 54);
                    lblEstadoTurno.BackColor = Color.FromArgb(255, 235, 238);
                    btnAbrirTurno.Enabled = false;
                    return;
                }

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                var query = @"
                    SELECT TOP 1 Id, FechaApertura, MontoInicial, Usuario
                    FROM TurnosCajero 
                    WHERE NumeroCajero = @numeroCajero 
                    AND Estado = 'Abierto'
                    ORDER BY FechaApertura DESC";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@numeroCajero", numeroCajeroLogueado);

                using var reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    DateTime fechaApertura = reader.GetDateTime(1);
                    decimal montoInicial = reader.GetDecimal(2);
                    string usuario = reader.GetString(3);

                    lblEstadoTurno.Text = $"⚠️ Ya existe un turno ABIERTO desde {fechaApertura:dd/MM/yyyy HH:mm}";
                    lblEstadoTurno.ForeColor = Color.FromArgb(244, 67, 54);
                    lblEstadoTurno.BackColor = Color.FromArgb(255, 235, 238);
                    btnAbrirTurno.Enabled = false;

                    MessageBox.Show(
                        $"⚠️ Su cajero ya tiene un turno abierto\n\n" +
                        $"Usuario: {usuario}\n" +
                        $"Apertura: {fechaApertura:dd/MM/yyyy HH:mm}\n" +
                        $"Monto: {montoInicial:C2}\n\n" +
                        $"Debe cerrar el turno actual primero.",
                        "Turno Abierto",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else
                {
                    lblEstadoTurno.Text = "✅ Puede abrir un nuevo turno";
                    lblEstadoTurno.ForeColor = Color.FromArgb(76, 175, 80);
                    lblEstadoTurno.BackColor = Color.FromArgb(232, 245, 233);
                    btnAbrirTurno.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verificando estado: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ✅ MODIFICADO: Usa numeroCajeroLogueado en lugar de ComboBox
        private async Task AbrirTurno()
        {
            try
            {
                if (numeroCajeroLogueado <= 0)
                {
                    MessageBox.Show("No se pudo identificar el cajero", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!decimal.TryParse(txtMontoInicial.Text.Replace(",", "."), out decimal montoInicial) || montoInicial < 0)
                {
                    MessageBox.Show("El monto inicial debe ser válido (≥0)", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMontoInicial.Focus();
                    return;
                }

                var resultado = MessageBox.Show(
                    $"¿Confirma la apertura del turno?\n\n" +
                    $"Cajero: {lblCajeroAsignado.Text}\n" +
                    $"Monto: {montoInicial:C2}\n" +
                    $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                    "Confirmar",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resultado != DialogResult.Yes) return;

                btnAbrirTurno.Enabled = false;
                btnAbrirTurno.Text = "⏳ Abriendo...";

                string usuario = AuthenticationService.SesionActual?.Usuario?.NombreUsuario ?? "Sistema";

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                var query = @"
                    INSERT INTO TurnosCajero 
                    (NumeroCajero, Usuario, FechaApertura, FechaCierre, MontoInicial, Estado, Observaciones)
                    OUTPUT INSERTED.Id
                    VALUES 
                    (@numeroCajero, @usuario, @fechaApertura, NULL, @montoInicial, 'Abierto', @observaciones)";

                int idTurno;
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@numeroCajero", numeroCajeroLogueado);
                    cmd.Parameters.AddWithValue("@usuario", usuario);
                    cmd.Parameters.AddWithValue("@fechaApertura", DateTime.Now);
                    cmd.Parameters.AddWithValue("@montoInicial", montoInicial);
                    cmd.Parameters.AddWithValue("@observaciones", txtObservaciones.Text ?? "");

                    idTurno = (int)await cmd.ExecuteScalarAsync();
                }

                MessageBox.Show(
                    $"✅ Turno abierto exitosamente\n\n" +
                    $"ID: {idTurno}\n" +
                    $"Cajero: {lblCajeroAsignado.Text}\n" +
                    $"Monto: {montoInicial:C2}",
                    "Éxito",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnAbrirTurno.Text = "🔓 Abrir Turno";
                btnAbrirTurno.Enabled = true;
            }
        }
    }
}