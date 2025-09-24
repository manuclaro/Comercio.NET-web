using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace Comercio.NET.Formularios
{
    public partial class ConfiguracionForm : Form
    {
        // Controles del formulario
        private TextBox txtNombreComercio, txtDomicilioComercio, txtConnectionString;
        private CheckBox chkValidarStockDisponible;
        private Button btnGuardar, btnCancelar, btnTestearConexion;
        private Label lblMensaje;
        private Panel panelPrincipal, panelComercio, panelValidaciones, panelBaseDatos;
        
        private string _rutaAppsettings;
        private JObject _configuracionOriginal;

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
            this.ClientSize = new Size(550, 480);
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
            int panelWidth = 510;

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
            currentY += 35;

            // Panel principal contenedor
            panelPrincipal = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(panelWidth, 320),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };
            this.Controls.Add(panelPrincipal);

            int panelY = 10;

            // === SECCIÓN COMERCIO ===
            panelComercio = CrearSeccion("🏪 INFORMACIÓN DEL COMERCIO", panelY, panelWidth - 20);
            panelPrincipal.Controls.Add(panelComercio);

            // Nombre del comercio
            panelComercio.Controls.Add(CrearLabel("Nombre del Comercio:", 15, 35));
            txtNombreComercio = CrearTextBox(150, 33, 320);
            panelComercio.Controls.Add(txtNombreComercio);

            // Domicilio del comercio
            panelComercio.Controls.Add(CrearLabel("Domicilio:", 15, 65));
            txtDomicilioComercio = CrearTextBox(150, 63, 320);
            panelComercio.Controls.Add(txtDomicilioComercio);

            panelY += 100;

            // === SECCIÓN BASE DE DATOS ===
            panelBaseDatos = CrearSeccion("🗄️ BASE DE DATOS", panelY, panelWidth - 20);
            panelPrincipal.Controls.Add(panelBaseDatos);

            // Connection String
            panelBaseDatos.Controls.Add(CrearLabel("Cadena de Conexión:", 15, 35));
            txtConnectionString = new TextBox
            {
                Location = new Point(15, 55),
                Size = new Size(400, 40),
                Font = new Font("Consolas", 8F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            panelBaseDatos.Controls.Add(txtConnectionString);

            // Botón testear conexión
            btnTestearConexion = new Button
            {
                Text = "🔧\nTest",
                Location = new Point(420, 55),
                Size = new Size(60, 40),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            btnTestearConexion.FlatAppearance.BorderSize = 0;
            panelBaseDatos.Controls.Add(btnTestearConexion);

            panelY += 120;

            // === SECCIÓN VALIDACIONES ===
            panelValidaciones = CrearSeccion("✅ VALIDACIONES DEL SISTEMA", panelY, panelWidth - 20);
            panelPrincipal.Controls.Add(panelValidaciones);

            // Checkbox validar stock
            chkValidarStockDisponible = new CheckBox
            {
                Text = "⚠️ Validar stock disponible al realizar ventas",
                Location = new Point(15, 35),
                Size = new Size(400, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(62, 80, 100)
            };
            panelValidaciones.Controls.Add(chkValidarStockDisponible);

            // Descripción de la validación
            var lblDescripcionStock = new Label
            {
                Text = "Si está habilitado, el sistema mostrará advertencias cuando no hay stock suficiente.\n" +
                       "El descuento de stock siempre se realiza, independientemente de esta configuración.",
                Location = new Point(35, 60),
                Size = new Size(440, 40),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = false
            };
            panelValidaciones.Controls.Add(lblDescripcionStock);

            currentY += 330;

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
            currentY += 25;

            // Botones
            CrearBotones(currentY);

            // Configurar TabIndex
            txtNombreComercio.TabIndex = 0;
            txtDomicilioComercio.TabIndex = 1;
            txtConnectionString.TabIndex = 2;
            btnTestearConexion.TabIndex = 3;
            chkValidarStockDisponible.TabIndex = 4;
            btnGuardar.TabIndex = 5;
            btnCancelar.TabIndex = 6;

            this.AcceptButton = btnGuardar;
            this.CancelButton = btnCancelar;
        }

        private Panel CrearSeccion(string titulo, int y, int ancho)
        {
            var panel = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(ancho, 110),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitulo = new Label
            {
                Text = titulo,
                Location = new Point(10, 8),
                Size = new Size(ancho - 20, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            };
            panel.Controls.Add(lblTitulo);

            return panel;
        }

        private Label CrearLabel(string texto, int x, int y)
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

        private void CrearBotones(int yPosition)
        {
            int margin = 20;

            btnGuardar = new Button
            {
                Text = "💾 Guardar Configuración",
                Location = new Point(margin + 80, yPosition),
                Size = new Size(160, 32),
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
                Location = new Point(margin + 260, yPosition),
                Size = new Size(100, 32),
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

        private void ConfigurarEventos()
        {
            btnGuardar.Click += async (s, e) => await GuardarConfiguracion();
            btnCancelar.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            btnTestearConexion.Click += async (s, e) => await TestearConexion();

            this.Load += (s, e) => txtNombreComercio.Focus();
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

                // Cargar cadena de conexión
                txtConnectionString.Text = _configuracionOriginal["ConnectionStrings"]?["DefaultConnection"]?.ToString() ?? "";

                // Cargar validaciones
                chkValidarStockDisponible.Checked = _configuracionOriginal["Validaciones"]?["ValidarStockDisponible"]?.ToObject<bool>() ?? true;

                MostrarMensaje("✅ Configuración cargada correctamente", Color.Green);
                
                // Limpiar mensaje después de 2 segundos
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

                // Crear copia de la configuración original
                var nuevaConfiguracion = JObject.Parse(_configuracionOriginal.ToString());

                // Actualizar información del comercio
                if (nuevaConfiguracion["Comercio"] == null)
                    nuevaConfiguracion["Comercio"] = new JObject();
                
                nuevaConfiguracion["Comercio"]["Nombre"] = txtNombreComercio.Text.Trim();
                nuevaConfiguracion["Comercio"]["Domicilio"] = txtDomicilioComercio.Text.Trim();

                // Actualizar cadena de conexión
                if (nuevaConfiguracion["ConnectionStrings"] == null)
                    nuevaConfiguracion["ConnectionStrings"] = new JObject();
                
                nuevaConfiguracion["ConnectionStrings"]["DefaultConnection"] = txtConnectionString.Text.Trim();

                // Actualizar validaciones
                if (nuevaConfiguracion["Validaciones"] == null)
                    nuevaConfiguracion["Validaciones"] = new JObject();
                
                nuevaConfiguracion["Validaciones"]["ValidarStockDisponible"] = chkValidarStockDisponible.Checked;

                // Crear backup del archivo original
                string backupPath = _rutaAppsettings + $".backup.{DateTime.Now:yyyyMMdd_HHmmss}";
                File.Copy(_rutaAppsettings, backupPath);

                // Guardar nueva configuración con formato JSON legible
                string jsonFormateado = JsonConvert.SerializeObject(nuevaConfiguracion, Formatting.Indented);
                await File.WriteAllTextAsync(_rutaAppsettings, jsonFormateado);

                MostrarMensaje("✅ Configuración guardada correctamente", Color.Green);
                
                // Mostrar información del backup
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
                    // Reiniciar aplicación
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
                
                // Probar una consulta simple
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
                txtNombreComercio.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtDomicilioComercio.Text))
            {
                MostrarMensaje("❌ El domicilio del comercio es requerido", Color.Red);
                txtDomicilioComercio.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtConnectionString.Text))
            {
                MostrarMensaje("❌ La cadena de conexión es requerida", Color.Red);
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

        // AGREGAR este método estático para crear el ícono de configuración
        public static Bitmap CrearIconoConfiguracion()
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                
                // Dibujar una rueda dentada simple
                using (var brush = new SolidBrush(Color.Gray))
                {
                    // Centro del ícono
                    g.FillEllipse(brush, 6, 6, 4, 4);
                    
                    // Dientes de la rueda
                    Rectangle[] dientes = {
                        new Rectangle(7, 2, 2, 3),   // Arriba
                        new Rectangle(11, 7, 3, 2),  // Derecha
                        new Rectangle(7, 11, 2, 3),  // Abajo
                        new Rectangle(2, 7, 3, 2)    // Izquierda
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