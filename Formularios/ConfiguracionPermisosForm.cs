using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Comercio.NET.Models;
using Comercio.NET.Services;

namespace Comercio.NET.Formularios
{
    /// <summary>
    /// Formulario para configurar permisos de cada perfil de usuario de forma gráfica
    /// </summary>
    public partial class ConfiguracionPermisosForm : Form
    {
        private ComboBox cmbPerfiles;
        private DataGridView dgvPermisos;
        private Button btnGuardar, btnCancelar;
        private Label lblTitulo, lblPerfil, lblMensaje;
        private Dictionary<NivelUsuario, Dictionary<string, bool>> permisosOriginales;

        // Lista de todas las funcionalidades del sistema
        private readonly List<Funcionalidad> funcionalidades = new List<Funcionalidad>
        {
            new Funcionalidad("Ventas", "Acceso al módulo de ventas", "ventas", true),
            new Funcionalidad("Imprimir Cartelitos", "Generador de etiquetas de precios", "cartelitos", true),
            new Funcionalidad("Apertura de Caja", "Abrir turno de cajero", "apertura_caja", true),
            new Funcionalidad("Cierre de Turno", "Cerrar turno de cajero", "cierre_turno", false),
            new Funcionalidad("Actualización Rápida", "Actualización rápida de precios/stock", "actualizacion_rapida", true),
            new Funcionalidad("Compras Proveedores", "Registro de compras a proveedores", "compras_proveedores", true),
            new Funcionalidad("Pagos Proveedores", "Gestión de pagos a proveedores", "pagos_proveedores", true),
            new Funcionalidad("─────────", "───────────────────────────────", "sep1", false),
            new Funcionalidad("ABM Productos", "Alta, baja y modificación de productos", "abm_productos", false),
            new Funcionalidad("Actualización Masiva", "Actualización masiva de precios", "actualizacion_masiva", false),
            new Funcionalidad("Ofertas y Combos", "Gestión de ofertas y descuentos", "ofertas", false),
            new Funcionalidad("Control de Facturas", "Consulta de facturas emitidas", "control_facturas", false),
            new Funcionalidad("ABM Proveedores", "Gestión de proveedores", "abm_proveedores", false),
            new Funcionalidad("Cuenta Corriente", "Cuenta corriente de proveedores", "cta_cte_proveedores", false),
            new Funcionalidad("Arqueo de Caja", "Verificación de caja sin cerrar turno", "arqueo_caja", false),
            new Funcionalidad("Historial Cierres", "Consulta de cierres de turno", "historial_cierres", false),
            new Funcionalidad("─────────", "───────────────────────────────", "sep2", false),
            new Funcionalidad("Gestión de Usuarios", "Administración de usuarios del sistema", "gestion_usuarios", false),
            new Funcionalidad("Configuración Sistema", "Configuración general del sistema", "configuracion", false),
            new Funcionalidad("Informes", "Acceso a informes y reportes", "informes", false)
        };

        public ConfiguracionPermisosForm()
        {
            InitializeComponent();
            ConfigurarFormulario();
            _ = CargarPermisos();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new Size(900, 510); // ✅ REDUCIDO de 600 a 550
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfiguracionPermisosForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Configuración de Permisos por Perfil";
            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = new Font("Segoe UI", 10F);

            CrearControles();
            ConfigurarEventos();
        }

        private void CrearControles()
        {
            int margin = 20;
            int currentY = 20;

            // Título
            lblTitulo = new Label
            {
                Text = "🔐 CONFIGURACIÓN DE PERMISOS POR PERFIL",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold), // ✅ REDUCIDO de 16F a 14F
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(margin, currentY),
                Size = new Size(860, 30), // ✅ REDUCIDO de 35 a 30
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitulo);
            currentY += 40; // ✅ REDUCIDO de 50 a 40

            // Panel de selección de perfil
            var panelPerfil = new Panel
            {
                Location = new Point(margin, currentY),
                Size = new Size(860, 55), // ✅ REDUCIDO de 60 a 55
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelPerfil);

            lblPerfil = new Label
            {
                Text = "👤 Seleccione el perfil a configurar:",
                Location = new Point(15, 16), // ✅ AJUSTADO de 18 a 16
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            panelPerfil.Controls.Add(lblPerfil);

            cmbPerfiles = new ComboBox
            {
                Location = new Point(270, 14), // ✅ AJUSTADO de 16 a 14
                Size = new Size(250, 28),
                Font = new Font("Segoe UI", 11F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Agregar perfiles
            cmbPerfiles.Items.Add(new { Text = "👑 Administrador", Value = NivelUsuario.Administrador });
            cmbPerfiles.Items.Add(new { Text = "👨‍💼 Supervisor", Value = NivelUsuario.Supervisor });
            cmbPerfiles.Items.Add(new { Text = "🛍️ Vendedor", Value = NivelUsuario.Vendedor });
            cmbPerfiles.Items.Add(new { Text = "👤 Invitado", Value = NivelUsuario.Invitado });

            cmbPerfiles.DisplayMember = "Text";
            cmbPerfiles.ValueMember = "Value";
            cmbPerfiles.SelectedIndex = 2; // Vendedor por defecto

            panelPerfil.Controls.Add(cmbPerfiles);

            // Info del perfil Vendedor
            var lblInfoVendedor = new Label
            {
                Text = "💡 Para perfil Vendedor: marcar las funcionalidades que necesita",
                Location = new Point(530, 16), // ✅ AJUSTADO de 18 a 16
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(255, 152, 0)
            };
            panelPerfil.Controls.Add(lblInfoVendedor);

            currentY += 65; // ✅ REDUCIDO de 70 a 65

            // DataGridView de permisos
            dgvPermisos = new DataGridView
            {
                Location = new Point(margin, currentY),
                Size = new Size(860, 310), // ✅ REDUCIDO de 400 a 310
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 9F),
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 35 // ✅ REDUCIDO de 40 a 35
            };

            // Configurar columnas
            dgvPermisos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Funcionalidad",
                HeaderText = "FUNCIONALIDAD",
                ReadOnly = true,
                FillWeight = 30
            });

            dgvPermisos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Descripcion",
                HeaderText = "DESCRIPCIÓN",
                ReadOnly = true,
                FillWeight = 50
            });

            dgvPermisos.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Permitido",
                HeaderText = "✅ PERMITIDO",
                FillWeight = 15,
                TrueValue = true,
                FalseValue = false
            });

            dgvPermisos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Codigo",
                HeaderText = "Codigo",
                Visible = false
            });

            // Estilos de encabezado
            dgvPermisos.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(63, 81, 181);
            dgvPermisos.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvPermisos.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvPermisos.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Estilos de filas
            dgvPermisos.DefaultCellStyle.SelectionBackColor = Color.FromArgb(227, 242, 253);
            dgvPermisos.DefaultCellStyle.SelectionForeColor = Color.FromArgb(62, 80, 100);
            dgvPermisos.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvPermisos.RowTemplate.Height = 30; // ✅ REDUCIDO de 35 a 30

            this.Controls.Add(dgvPermisos);
            currentY += 320; // ✅ REDUCIDO de 410 a 320

            // Mensaje de estado
            lblMensaje = new Label
            {
                Location = new Point(margin, currentY),
                Size = new Size(860, 22), // ✅ REDUCIDO de 25 a 22
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.Blue,
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblMensaje);
            currentY += 28; // ✅ REDUCIDO de 30 a 28

            // Botones
            btnGuardar = new Button
            {
                Text = "💾 Guardar Configuración",
                Location = new Point(margin + 530, currentY),
                Size = new Size(180, 35),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnGuardar);

            btnCancelar = new Button
            {
                Text = "❌ Cancelar",
                Location = new Point(margin + 720, currentY),
                Size = new Size(110, 35),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCancelar);
        }

        private void ConfigurarEventos()
        {
            cmbPerfiles.SelectedIndexChanged += CmbPerfiles_SelectedIndexChanged;
            btnGuardar.Click += async (s, e) => await GuardarPermisos();
            btnCancelar.Click += (s, e) => this.Close();
            dgvPermisos.CellContentClick += DgvPermisos_CellContentClick;
            dgvPermisos.CellFormatting += DgvPermisos_CellFormatting;
        }

        private void DgvPermisos_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvPermisos.Columns["Permitido"].Index)
            {
                dgvPermisos.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void DgvPermisos_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var funcionalidad = dgvPermisos.Rows[e.RowIndex].Cells["Funcionalidad"].Value?.ToString();
                
                // Filas separadoras
                if (funcionalidad?.StartsWith("─") == true)
                {
                    e.CellStyle.BackColor = Color.FromArgb(200, 200, 200);
                    e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    e.CellStyle.ForeColor = Color.Gray;
                    dgvPermisos.Rows[e.RowIndex].ReadOnly = true;
                }
            }
        }

        private async Task CargarPermisos()
        {
            try
            {
                permisosOriginales = new Dictionary<NivelUsuario, Dictionary<string, bool>>();

                // Cargar permisos de la base de datos o usar valores predeterminados
                await CargarPermisosDesdeBD();

                // Mostrar permisos del perfil seleccionado
                ActualizarVistaPermisos();
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar permisos: {ex.Message}", Color.Red);
            }
        }

        private async Task CargarPermisosDesdeBD()
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

                // Verificar si existe la tabla de permisos
                var checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = 'PermisosPerfiles'";

                using (var checkCmd = new SqlCommand(checkTableQuery, connection))
                {
                    int tableExists = (int)await checkCmd.ExecuteScalarAsync();
                    
                    if (tableExists == 0)
                    {
                        // Crear tabla de permisos
                        await CrearTablaPermisos(connection);
                        // Cargar valores predeterminados
                        CargarPermisosPredeterminados();
                        return;
                    }
                }

                // Cargar permisos existentes
                var query = "SELECT NivelUsuario, CodigoFuncionalidad, Permitido FROM PermisosPerfiles";
                using var cmd = new SqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    var nivel = (NivelUsuario)reader.GetInt32("NivelUsuario");
                    var codigo = reader.GetString("CodigoFuncionalidad");
                    var permitido = reader.GetBoolean("Permitido");

                    if (!permisosOriginales.ContainsKey(nivel))
                        permisosOriginales[nivel] = new Dictionary<string, bool>();

                    permisosOriginales[nivel][codigo] = permitido;
                }

                // Si está vacío, cargar valores predeterminados
                if (permisosOriginales.Count == 0)
                {
                    CargarPermisosPredeterminados();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando permisos desde BD: {ex.Message}");
                CargarPermisosPredeterminados();
            }
        }

        private async Task CrearTablaPermisos(SqlConnection connection)
        {
            var createTableQuery = @"
                CREATE TABLE PermisosPerfiles (
                    IdPermiso INT IDENTITY(1,1) PRIMARY KEY,
                    NivelUsuario INT NOT NULL,
                    CodigoFuncionalidad NVARCHAR(50) NOT NULL,
                    Permitido BIT NOT NULL DEFAULT 0,
                    UNIQUE(NivelUsuario, CodigoFuncionalidad)
                )";

            using var cmd = new SqlCommand(createTableQuery, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        private void CargarPermisosPredeterminados()
        {
            permisosOriginales = new Dictionary<NivelUsuario, Dictionary<string, bool>>();

            // Configuración para Vendedor (tu caso específico)
            permisosOriginales[NivelUsuario.Vendedor] = new Dictionary<string, bool>
            {
                ["ventas"] = true,
                ["cartelitos"] = true,
                ["apertura_caja"] = true,
                ["cierre_turno"] = false, // Solo supervisores y admin
                ["actualizacion_rapida"] = true,
                ["compras_proveedores"] = true,
                ["pagos_proveedores"] = true,
                ["abm_productos"] = false,
                ["actualizacion_masiva"] = false,
                ["ofertas"] = false,
                ["control_facturas"] = true,
                ["abm_proveedores"] = false,
                ["cta_cte_proveedores"] = false,
                ["arqueo_caja"] = false,
                ["historial_cierres"] = false,
                ["gestion_usuarios"] = false,
                ["configuracion"] = false,
                ["informes"] = false
            };

            // Supervisor
            permisosOriginales[NivelUsuario.Supervisor] = new Dictionary<string, bool>
            {
                ["ventas"] = true,
                ["cartelitos"] = true,
                ["apertura_caja"] = true,
                ["cierre_turno"] = true,
                ["actualizacion_rapida"] = true,
                ["compras_proveedores"] = true,
                ["pagos_proveedores"] = true,
                ["abm_productos"] = true,
                ["actualizacion_masiva"] = true,
                ["ofertas"] = true,
                ["control_facturas"] = true,
                ["abm_proveedores"] = true,
                ["cta_cte_proveedores"] = true,
                ["arqueo_caja"] = true,
                ["historial_cierres"] = true,
                ["gestion_usuarios"] = false,
                ["configuracion"] = false,
                ["informes"] = true
            };

            // Administrador (acceso total)
            permisosOriginales[NivelUsuario.Administrador] = funcionalidades
                .Where(f => !f.Codigo.StartsWith("sep"))
                .ToDictionary(f => f.Codigo, f => true);

            // Invitado (acceso mínimo)
            permisosOriginales[NivelUsuario.Invitado] = funcionalidades
                .Where(f => !f.Codigo.StartsWith("sep"))
                .ToDictionary(f => f.Codigo, f => false);
        }

        private void CmbPerfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActualizarVistaPermisos();
        }

        private void ActualizarVistaPermisos()
        {
            dgvPermisos.Rows.Clear();

            if (cmbPerfiles.SelectedItem == null) return;

            var nivelSeleccionado = (NivelUsuario)cmbPerfiles.SelectedItem.GetType()
                .GetProperty("Value").GetValue(cmbPerfiles.SelectedItem);

            if (!permisosOriginales.ContainsKey(nivelSeleccionado))
                return;

            var permisosNivel = permisosOriginales[nivelSeleccionado];

            foreach (var func in funcionalidades)
            {
                bool permitido = permisosNivel.ContainsKey(func.Codigo) && permisosNivel[func.Codigo];
                
                dgvPermisos.Rows.Add(
                    func.Nombre,
                    func.Descripcion,
                    permitido,
                    func.Codigo
                );
            }

            // Administrador siempre tiene todo habilitado (solo lectura)
            bool esAdmin = nivelSeleccionado == NivelUsuario.Administrador;
            dgvPermisos.Columns["Permitido"].ReadOnly = esAdmin;

            if (esAdmin)
            {
                MostrarMensaje("ℹ️ Los administradores tienen acceso total a todas las funcionalidades", Color.Blue);
            }
            else
            {
                MostrarMensaje("✏️ Marque o desmarque para configurar los permisos de este perfil", Color.Green);
            }
        }

        private async Task GuardarPermisos()
        {
            try
            {
                btnGuardar.Enabled = false;
                btnGuardar.Text = "💾 Guardando...";
                MostrarMensaje("⏳ Guardando configuración de permisos...", Color.Blue);

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Eliminar permisos existentes
                var deleteQuery = "DELETE FROM PermisosPerfiles";
                using (var deleteCmd = new SqlCommand(deleteQuery, connection))
                {
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                // Insertar nuevos permisos
                var insertQuery = @"
                    INSERT INTO PermisosPerfiles (NivelUsuario, CodigoFuncionalidad, Permitido) 
                    VALUES (@nivel, @codigo, @permitido)";

                foreach (var nivelPermisos in permisosOriginales)
                {
                    foreach (var permiso in nivelPermisos.Value)
                    {
                        if (!permiso.Key.StartsWith("sep")) // Ignorar separadores
                        {
                            using var cmd = new SqlCommand(insertQuery, connection);
                            cmd.Parameters.AddWithValue("@nivel", (int)nivelPermisos.Key);
                            cmd.Parameters.AddWithValue("@codigo", permiso.Key);
                            cmd.Parameters.AddWithValue("@permitido", permiso.Value);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                MostrarMensaje("✅ Permisos guardados correctamente", Color.Green);
                await Task.Delay(1500);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MostrarMensaje($"❌ Error al guardar permisos: {ex.Message}", Color.Red);
            }
            finally
            {
                btnGuardar.Enabled = true;
                btnGuardar.Text = "💾 Guardar Configuración";
            }
        }

        private void MostrarMensaje(string mensaje, Color color)
        {
            lblMensaje.Text = mensaje;
            lblMensaje.ForeColor = color;
        }

        // Clase auxiliar para funcionalidades
        private class Funcionalidad
        {
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public string Codigo { get; set; }
            public bool PredeterminadoVendedor { get; set; }

            public Funcionalidad(string nombre, string descripcion, string codigo, bool predeterminadoVendedor)
            {
                Nombre = nombre;
                Descripcion = descripcion;
                Codigo = codigo;
                PredeterminadoVendedor = predeterminadoVendedor;
            }
        }
    }
}