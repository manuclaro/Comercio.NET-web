using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Comercio.NET.Servicios;

namespace Comercio.NET.Formularios
{
    public class frmActualizacion : Form
    {
        private Label lblTitulo;
        private Label lblVersionActual;
        private Label lblVersionNueva;
        private Label lblTamañoArchivo;
        private TextBox txtChangeLog;
        private ProgressBar progressBar;
        private Label lblProgreso;
        private Button btnActualizar;
        private Button btnOmitir;
        private Panel panelInfo;
        private PictureBox picIcono;

        private readonly AutoUpdaterService _updaterService;
        private readonly VersionInfo _versionInfo;
        private readonly string _currentVersion;

        public frmActualizacion(VersionInfo versionInfo, string currentVersion, string updateServerUrl)
        {
            _versionInfo = versionInfo ?? throw new ArgumentNullException(nameof(versionInfo));
            _currentVersion = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));
            
            _updaterService = new AutoUpdaterService(updateServerUrl, currentVersion);

            InitializeComponent();
            MostrarInformacion();
        }

        private void InitializeComponent()
        {
            this.Text = "Actualización Disponible - Comercio .NET";
            this.Size = new Size(650, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            // Panel superior con gradiente
            panelInfo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140,
                BackColor = Color.FromArgb(0, 120, 215)
            };
            panelInfo.Paint += (s, e) =>
            {
                var rect = panelInfo.ClientRectangle;
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    rect, Color.FromArgb(0, 120, 215), Color.FromArgb(0, 90, 180), 90f))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
            };

            // Icono de actualización
            picIcono = new PictureBox
            {
                Size = new Size(64, 64),
                Location = new Point(20, 38),
                SizeMode = PictureBoxSizeMode.CenterImage,
                BackColor = Color.Transparent
            };
            // Aquí puedes agregar un icono si tienes uno en recursos
            panelInfo.Controls.Add(picIcono);

            lblTitulo = new Label
            {
                Text = "?? Nueva versión disponible",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(100, 30),
                BackColor = Color.Transparent
            };

            lblVersionActual = new Label
            {
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(220, 220, 220),
                AutoSize = true,
                Location = new Point(100, 70),
                BackColor = Color.Transparent
            };

            lblVersionNueva = new Label
            {
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(144, 238, 144),
                AutoSize = true,
                Location = new Point(100, 95),
                BackColor = Color.Transparent
            };

            lblTamañoArchivo = new Label
            {
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true,
                Location = new Point(100, 118),
                BackColor = Color.Transparent
            };

            panelInfo.Controls.AddRange(new Control[] { 
                lblTitulo, lblVersionActual, lblVersionNueva, lblTamañoArchivo
            });

            // Título del ChangeLog
            var lblChangeLogTitulo = new Label
            {
                Text = "?? Novedades en esta versión:",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(20, 160),
                AutoSize = true
            };

            // TextBox para ChangeLog
            txtChangeLog = new TextBox
            {
                Location = new Point(20, 190),
                Size = new Size(590, 200),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Barra de progreso
            progressBar = new ProgressBar
            {
                Location = new Point(20, 410),
                Size = new Size(590, 30),
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            lblProgreso = new Label
            {
                Location = new Point(20, 445),
                Size = new Size(590, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Visible = false
            };

            // Botones
            btnActualizar = new Button
            {
                Text = "?? Actualizar Ahora",
                Size = new Size(180, 45),
                Location = new Point(290, 460),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnActualizar.FlatAppearance.BorderSize = 0;
            btnActualizar.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 142, 59);
            btnActualizar.Click += BtnActualizar_Click;

            btnOmitir = new Button
            {
                Text = "Omitir por ahora",
                Size = new Size(140, 45),
                Location = new Point(480, 460),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            btnOmitir.FlatAppearance.BorderSize = 0;
            btnOmitir.FlatAppearance.MouseOverBackColor = Color.FromArgb(90, 98, 104);
            btnOmitir.Click += (s, e) => this.Close();

            Controls.AddRange(new Control[] {
                panelInfo, lblChangeLogTitulo, txtChangeLog,
                progressBar, lblProgreso, btnActualizar, btnOmitir
            });
        }

        private void MostrarInformacion()
        {
            lblVersionActual.Text = $"?? Versión actual: {_currentVersion}";
            lblVersionNueva.Text = $"?? Nueva versión: {_versionInfo.Version}";

            // Mostrar tamaño del archivo
            if (_versionInfo.FileSize > 0)
            {
                double sizeMB = _versionInfo.FileSize / 1024.0 / 1024.0;
                lblTamañoArchivo.Text = $"?? Tamaño: {sizeMB:F2} MB";
            }

            // Mostrar changelog
            if (_versionInfo.ChangeLog != null && _versionInfo.ChangeLog.Length > 0)
            {
                txtChangeLog.Lines = _versionInfo.ChangeLog;
            }
            else
            {
                txtChangeLog.Text = "• Mejoras generales de rendimiento\r\n• Correcciones de errores";
            }

            // Si es actualización obligatoria
            if (_versionInfo.IsRequired)
            {
                btnOmitir.Enabled = false;
                btnOmitir.Text = "Obligatoria";
                lblTitulo.Text = "?? Actualización Requerida";
                panelInfo.BackColor = Color.FromArgb(220, 53, 69);
                
                var msgObligatoria = new Label
                {
                    Text = "?? Esta actualización es obligatoria y debe instalarse para continuar usando la aplicación.",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(220, 53, 69),
                    Location = new Point(20, 395),
                    Size = new Size(590, 40),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                Controls.Add(msgObligatoria);
            }
        }

        private async void BtnActualizar_Click(object sender, EventArgs e)
        {
            try
            {
                // Deshabilitar botones
                btnActualizar.Enabled = false;
                btnOmitir.Enabled = false;
                
                // Mostrar progreso
                progressBar.Visible = true;
                progressBar.Value = 0;
                lblProgreso.Visible = true;
                lblProgreso.Text = "Descargando actualización...";

                var progress = new Progress<int>(percent =>
                {
                    if (progressBar.InvokeRequired)
                    {
                        progressBar.Invoke(new Action(() => {
                            progressBar.Value = percent;
                            lblProgreso.Text = $"Descargando: {percent}%";
                        }));
                    }
                    else
                    {
                        progressBar.Value = percent;
                        lblProgreso.Text = $"Descargando: {percent}%";
                    }
                });

                var success = await _updaterService.DownloadAndInstallAsync(_versionInfo, progress);

                if (!success)
                {
                    MessageBox.Show(
                        "Error al descargar la actualización.\n\n" +
                        "Por favor, intente nuevamente más tarde o contacte al soporte técnico.",
                        "Error de Actualización", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                    
                    // Rehabilitar botones
                    btnActualizar.Enabled = true;
                    if (!_versionInfo.IsRequired)
                        btnOmitir.Enabled = true;
                    
                    progressBar.Visible = false;
                    lblProgreso.Visible = false;
                }
                // Si success == true, la aplicación se cerrará automáticamente
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error durante la actualización:\n\n{ex.Message}\n\n" +
                    "Por favor, contacte al soporte técnico.",
                    "Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);

                btnActualizar.Enabled = true;
                if (!_versionInfo.IsRequired)
                    btnOmitir.Enabled = true;
                
                progressBar.Visible = false;
                lblProgreso.Visible = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updaterService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}