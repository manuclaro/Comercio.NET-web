using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Comercio.NET.Services;

namespace Comercio.NET.Formularios
{
    public partial class PagoProveedorRapidoForm : Form
    {
        private ComboBox cboProveedor;
        private ComboBox cboMetodoPago;
        private ComboBox cboFacturaPendiente;
        private TextBox txtMonto;
        private TextBox txtObservaciones;
        private Button btnConfirmar;
        private Button btnCancelar;
        private Label lblTotal;
        private Label lblTotalPendiente;

        public decimal Monto { get; private set; }
        public int ProveedorIdSeleccionado { get; private set; }
        public string ProveedorSeleccionado { get; private set; }
        public string MetodoPago { get; private set; }
        public string Referencia { get; private set; }
        public string Observaciones { get; private set; }
        public bool Confirmado { get; private set; }

        public int? CompraIdSeleccionada { get; private set; }

        public PagoProveedorRapidoForm()
        {
            InitializeComponent();
            ConfigurarFormulario();
            CrearControles();
            CargarProveedores();
        }

        private void ConfigurarFormulario()
        {
            this.Text = "💳 Pago Rápido a Proveedor";
            this.Size = new Size(550, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.WhiteSmoke;
            this.Font = new Font("Segoe UI", 10F);
        }

        private void CrearControles()
        {
            int margin = 20;
            int labelWidth = 150;
            int controlWidth = 350;
            int currentY = margin + 10;

            // Panel de título
            var panelTitulo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(0, 150, 136)
            };
            var lblTitulo = new Label
            {
                Text = "💳 Registrar Pago a Proveedor",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelTitulo.Controls.Add(lblTitulo);
            this.Controls.Add(panelTitulo);
            currentY += 70;

            // ========== Proveedor ==========
            var lblProveedor = new Label
            {
                Text = "Proveedor:",
                Location = new Point(margin, currentY),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblProveedor);

            cboProveedor = new ComboBox
            {
                Location = new Point(margin + labelWidth, currentY),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            cboProveedor.SelectedIndexChanged += CboProveedor_SelectedIndexChanged;
            this.Controls.Add(cboProveedor);
            currentY += 40;

            // Total pendiente del proveedor
            lblTotalPendiente = new Label
            {
                Text = "Pendiente: -",
                Location = new Point(margin + labelWidth, currentY),
                Size = new Size(controlWidth, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(220, 53, 69)
            };
            this.Controls.Add(lblTotalPendiente);
            currentY += 35;

            // ========== Factura Pendiente ==========
            var lblFactura = new Label
            {
                Text = "Factura Pendiente:",
                Location = new Point(margin, currentY),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblFactura);

            cboFacturaPendiente = new ComboBox
            {
                Location = new Point(margin + labelWidth, currentY),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Enabled = false
            };
            cboFacturaPendiente.SelectedIndexChanged += CboFacturaPendiente_SelectedIndexChanged;
            this.Controls.Add(cboFacturaPendiente);
            currentY += 45;

            // ========== Monto ==========
            var lblMonto = new Label
            {
                Text = "Monto a pagar:",
                Location = new Point(margin, currentY),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblMonto);

            txtMonto = new TextBox
            {
                Location = new Point(margin + labelWidth, currentY),
                Size = new Size(controlWidth, 30),
                Font = new Font("Segoe UI", 12F),
                PlaceholderText = "Ingrese el monto"
            };
            txtMonto.KeyPress += TxtMonto_KeyPress;
            txtMonto.TextChanged += TxtMonto_TextChanged;
            this.Controls.Add(txtMonto);
            currentY += 50;

            // ========== Método de Pago ==========
            var lblMetodo = new Label
            {
                Text = "Método de Pago:",
                Location = new Point(margin, currentY),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblMetodo);

            cboMetodoPago = new ComboBox
            {
                Location = new Point(margin + labelWidth, currentY),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            cboMetodoPago.Items.AddRange(new object[] {
                "Efectivo",
                "DNI",
                "MercadoPago"
            });
            cboMetodoPago.SelectedIndex = 0;
            cboMetodoPago.SelectedIndexChanged += (s, e) => ValidarFormulario();
            this.Controls.Add(cboMetodoPago);
            currentY += 45;

            // ========== Observaciones ==========
            var lblObservaciones = new Label
            {
                Text = "Observaciones:",
                Location = new Point(margin, currentY),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblObservaciones);

            txtObservaciones = new TextBox
            {
                Location = new Point(margin + labelWidth, currentY),
                Size = new Size(controlWidth, 60),
                Font = new Font("Segoe UI", 10F),
                Multiline = true,
                PlaceholderText = "Información adicional (opcional)"
            };
            this.Controls.Add(txtObservaciones);
            currentY += 80;

            // Panel de botones
            var panelBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Size = new Size(100, 35),
                Location = new Point(this.Width - 230, 10),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += BtnCancelar_Click;
            panelBotones.Controls.Add(btnCancelar);

            btnConfirmar = new Button
            {
                Text = "Confirmar Pago",
                Size = new Size(120, 35),
                Location = new Point(this.Width - 120, 10),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnConfirmar.FlatAppearance.BorderSize = 0;
            btnConfirmar.Click += BtnConfirmar_Click;
            panelBotones.Controls.Add(btnConfirmar);

            this.Controls.Add(panelBotones);
        }

        private void CargarFacturasPendientes(int idProveedor)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"
                SELECT 
                    cta.CompraId,
                    cp.NumeroFactura,
                    cp.Fecha,
                    cta.Saldo
                FROM ProveedoresCtaCte cta
                LEFT JOIN ComprasProveedores cp ON cta.CompraId = cp.Id
                WHERE cta.ProveedorId = @idProveedor
                    AND cta.Saldo > 0
                ORDER BY cp.Fecha DESC";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@idProveedor", idProveedor);
                        connection.Open();

                        cboFacturaPendiente.Items.Clear();
                        cboFacturaPendiente.Items.Add(new
                        {
                            CompraId = (int?)null,
                            Texto = "-- Sin vincular a factura específica --",
                            Saldo = 0m
                        });

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int? compraId = reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0);
                                string numeroFactura = reader.IsDBNull(1) ? "Sin Nro." : reader.GetString(1);
                                DateTime fecha = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetDateTime(2);
                                decimal saldo = reader.GetDecimal(3);

                                string texto = $"{numeroFactura} - {fecha:dd/MM/yyyy} - Saldo: {saldo:C2}";

                                cboFacturaPendiente.Items.Add(new
                                {
                                    CompraId = compraId,
                                    Texto = texto,
                                    Saldo = saldo
                                });
                            }
                        }
                    }
                }

                cboFacturaPendiente.DisplayMember = "Texto";
                cboFacturaPendiente.ValueMember = "CompraId";
                cboFacturaPendiente.SelectedIndex = 0;
                cboFacturaPendiente.Enabled = true;

                System.Diagnostics.Debug.WriteLine($"✅ Facturas pendientes cargadas: {cboFacturaPendiente.Items.Count - 1}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando facturas pendientes: {ex.Message}");
                cboFacturaPendiente.Items.Clear();
                cboFacturaPendiente.Items.Add(new
                {
                    CompraId = (int?)null,
                    Texto = "⚠️ Error cargando facturas",
                    Saldo = 0m
                });
                cboFacturaPendiente.SelectedIndex = 0;
                cboFacturaPendiente.Enabled = false;
            }
        }

        private void CboFacturaPendiente_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboFacturaPendiente.SelectedIndex > 0)
            {
                dynamic facturaSeleccionada = cboFacturaPendiente.SelectedItem;
                decimal saldo = facturaSeleccionada.Saldo;

                txtMonto.Text = saldo.ToString("F2");
            }

            ValidarFormulario();
        }

        // ✅ MÉTODO CORREGIDO con las columnas EXACTAS de la tabla PagoProveedores
        public async Task<bool> GuardarPagoEnBaseDatos()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");
                string usuario = AuthenticationService.SesionActual?.Usuario?.NombreUsuario ?? "Sistema";

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // ✅ CORREGIDO: Usar SOLO las columnas que existen en la tabla PagoProveedores
                            var queryInsertPago = @"
                                INSERT INTO PagoProveedores 
                                (CompraId, CtaCteId, MedioPago, Monto, Referencia, FechaPago, Usuario)
                                VALUES 
                                (@compraId, @ctaCteId, @medioPago, @monto, @referencia, @fechaPago, @usuario)";

                            using (var cmd = new SqlCommand(queryInsertPago, connection, transaction))
                            {
                                // CompraId: Puede ser NULL si no se vincula a una factura específica
                                if (CompraIdSeleccionada.HasValue)
                                    cmd.Parameters.AddWithValue("@compraId", CompraIdSeleccionada.Value);
                                else
                                    cmd.Parameters.AddWithValue("@compraId", DBNull.Value);

                                // CtaCteId: Puede ser NULL (para pagos generales)
                                cmd.Parameters.AddWithValue("@ctaCteId", DBNull.Value);

                                // MedioPago: Efectivo, DNI, MercadoPago
                                cmd.Parameters.AddWithValue("@medioPago", MetodoPago);

                                // Monto
                                cmd.Parameters.AddWithValue("@monto", Monto);

                                // Referencia: Observaciones completas
                                string observacionesCompletas = ConstruirObservaciones();
                                cmd.Parameters.AddWithValue("@referencia",
                                    string.IsNullOrWhiteSpace(observacionesCompletas) ? (object)DBNull.Value : observacionesCompletas);

                                // FechaPago
                                cmd.Parameters.AddWithValue("@fechaPago", DateTime.Now);

                                // Usuario
                                cmd.Parameters.AddWithValue("@usuario", usuario);

                                await cmd.ExecuteNonQueryAsync();

                                System.Diagnostics.Debug.WriteLine($"✅ Pago insertado en PagoProveedores");
                            }

                            // ✅ PASO 2: Actualizar saldo en ProveedoresCtaCte SI hay CompraId
                            if (CompraIdSeleccionada.HasValue)
                            {
                                decimal saldoActual = 0m;
                                var queryObtenerSaldo = @"
                                    SELECT Saldo 
                                    FROM ProveedoresCtaCte 
                                    WHERE CompraId = @compraId AND ProveedorId = @proveedorId";

                                using (var cmdSaldo = new SqlCommand(queryObtenerSaldo, connection, transaction))
                                {
                                    cmdSaldo.Parameters.AddWithValue("@compraId", CompraIdSeleccionada.Value);
                                    cmdSaldo.Parameters.AddWithValue("@proveedorId", ProveedorIdSeleccionado);

                                    var result = await cmdSaldo.ExecuteScalarAsync();
                                    if (result != null && result != DBNull.Value)
                                    {
                                        saldoActual = Convert.ToDecimal(result);
                                    }
                                }

                                decimal nuevoSaldo = saldoActual - Monto;
                                if (nuevoSaldo < 0) nuevoSaldo = 0;

                                var queryUpdateSaldo = @"
                                    UPDATE ProveedoresCtaCte 
                                    SET Saldo = @nuevoSaldo,
                                        MontoAdeudado = @nuevoSaldo
                                    WHERE CompraId = @compraId AND ProveedorId = @proveedorId";

                                using (var cmdUpdate = new SqlCommand(queryUpdateSaldo, connection, transaction))
                                {
                                    cmdUpdate.Parameters.AddWithValue("@nuevoSaldo", nuevoSaldo);
                                    cmdUpdate.Parameters.AddWithValue("@compraId", CompraIdSeleccionada.Value);
                                    cmdUpdate.Parameters.AddWithValue("@proveedorId", ProveedorIdSeleccionado);

                                    await cmdUpdate.ExecuteNonQueryAsync();
                                }

                                if (nuevoSaldo == 0)
                                {
                                    var queryUpdateCompra = @"
                                        UPDATE ComprasProveedores 
                                        SET EsCtaCte = 0 
                                        WHERE Id = @compraId";

                                    using (var cmdCompra = new SqlCommand(queryUpdateCompra, connection, transaction))
                                    {
                                        cmdCompra.Parameters.AddWithValue("@compraId", CompraIdSeleccionada.Value);
                                        await cmdCompra.ExecuteNonQueryAsync();
                                    }

                                    System.Diagnostics.Debug.WriteLine($"✅ Factura saldada - CompraId: {CompraIdSeleccionada.Value}");
                                }

                                System.Diagnostics.Debug.WriteLine(
                                    $"✅ Saldo actualizado:\n" +
                                    $"   CompraId: {CompraIdSeleccionada.Value}\n" +
                                    $"   Saldo anterior: {saldoActual:C2}\n" +
                                    $"   Pago: {Monto:C2}\n" +
                                    $"   Saldo nuevo: {nuevoSaldo:C2}");
                            }
                            else
                            {
                                // FIFO: Aplicar a factura más antigua
                                var queryFacturasMasAntiguas = @"
                                    SELECT TOP 1 CompraId, Saldo 
                                    FROM ProveedoresCtaCte 
                                    WHERE ProveedorId = @proveedorId AND Saldo > 0
                                    ORDER BY Fecha ASC";

                                int? compraIdMasAntigua = null;
                                decimal saldoMasAntiguo = 0m;

                                using (var cmdAntigua = new SqlCommand(queryFacturasMasAntiguas, connection, transaction))
                                {
                                    cmdAntigua.Parameters.AddWithValue("@proveedorId", ProveedorIdSeleccionado);

                                    using (var reader = await cmdAntigua.ExecuteReaderAsync())
                                    {
                                        if (await reader.ReadAsync())
                                        {
                                            compraIdMasAntigua = reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0);
                                            saldoMasAntiguo = reader.GetDecimal(1);
                                        }
                                    }
                                }

                                if (compraIdMasAntigua.HasValue)
                                {
                                    decimal montoADescontar = Math.Min(Monto, saldoMasAntiguo);
                                    decimal nuevoSaldo = saldoMasAntiguo - montoADescontar;

                                    var queryUpdateSaldoFIFO = @"
                                        UPDATE ProveedoresCtaCte 
                                        SET Saldo = @nuevoSaldo,
                                            MontoAdeudado = @nuevoSaldo
                                        WHERE CompraId = @compraId AND ProveedorId = @proveedorId";

                                    using (var cmdUpdate = new SqlCommand(queryUpdateSaldoFIFO, connection, transaction))
                                    {
                                        cmdUpdate.Parameters.AddWithValue("@nuevoSaldo", nuevoSaldo);
                                        cmdUpdate.Parameters.AddWithValue("@compraId", compraIdMasAntigua.Value);
                                        cmdUpdate.Parameters.AddWithValue("@proveedorId", ProveedorIdSeleccionado);

                                        await cmdUpdate.ExecuteNonQueryAsync();
                                    }

                                    System.Diagnostics.Debug.WriteLine($"✅ Pago FIFO aplicado - CompraId: {compraIdMasAntigua.Value}");
                                }
                            }

                            transaction.Commit();

                            System.Diagnostics.Debug.WriteLine(
                                $"✅✅✅ PAGO GUARDADO EXITOSAMENTE:\n" +
                                $"   Proveedor: {ProveedorSeleccionado}\n" +
                                $"   Monto: {Monto:C2}\n" +
                                $"   Método: {MetodoPago}\n" +
                                $"   CompraId: {CompraIdSeleccionada?.ToString() ?? "N/A"}");

                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"❌ Error en transacción: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error guardando pago: {ex.Message}");
                MessageBox.Show($"Error al guardar el pago en la base de datos:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private string ConstruirObservaciones()
        {
            var partes = new System.Collections.Generic.List<string>();

            if (CompraIdSeleccionada.HasValue)
            {
                partes.Add($"Compra #{CompraIdSeleccionada.Value}");
            }

            if (!string.IsNullOrWhiteSpace(Referencia))
            {
                partes.Add($"Ref: {Referencia}");
            }

            if (!string.IsNullOrWhiteSpace(Observaciones))
            {
                partes.Add(Observaciones);
            }

            return string.Join(" | ", partes);
        }

        private void CargarProveedores()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"
                        SELECT Id, Nombre 
                        FROM Proveedores 
                        WHERE Activo = 1
                        ORDER BY Nombre";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        connection.Open();

                        cboProveedor.Items.Clear();
                        cboProveedor.Items.Add(new { Id = -1, Nombre = "-- Seleccione proveedor --" });

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                string nombre = reader.GetString(1);

                                cboProveedor.Items.Add(new
                                {
                                    Id = id,
                                    Nombre = nombre
                                });
                            }
                        }
                    }
                }

                cboProveedor.DisplayMember = "Nombre";
                cboProveedor.ValueMember = "Id";
                cboProveedor.SelectedIndex = 0;

                System.Diagnostics.Debug.WriteLine($"✅ Proveedores cargados: {cboProveedor.Items.Count - 1}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar proveedores: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                cboProveedor.Items.Clear();
                cboProveedor.Items.Add(new { Id = -1, Nombre = "⚠️ Tabla Proveedores no disponible" });
                cboProveedor.SelectedIndex = 0;
                cboProveedor.Enabled = false;
            }
        }

        private void CboProveedor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboProveedor.SelectedIndex > 0)
            {
                dynamic proveedorSeleccionado = cboProveedor.SelectedItem;
                int idProveedor = proveedorSeleccionado.Id;
                string nombreProveedor = proveedorSeleccionado.Nombre;

                CargarSaldoPendiente(idProveedor, nombreProveedor);
                CargarFacturasPendientes(idProveedor);
            }
            else
            {
                lblTotalPendiente.Text = "Pendiente: -";
                cboFacturaPendiente.Items.Clear();
                cboFacturaPendiente.Enabled = false;
            }

            ValidarFormulario();
        }

        private void CargarSaldoPendiente(int idProveedor, string nombreProveedor)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"
                        SELECT ISNULL(SUM(Saldo), 0) as SaldoTotal
                        FROM ProveedoresCtaCte
                        WHERE ProveedorId = @idProveedor AND Saldo > 0";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@idProveedor", idProveedor);
                        connection.Open();

                        var saldo = cmd.ExecuteScalar();
                        if (saldo != null && saldo != DBNull.Value)
                        {
                            decimal saldoPendiente = Convert.ToDecimal(saldo);
                            lblTotalPendiente.Text = $"Pendiente: {saldoPendiente:C2}";
                            lblTotalPendiente.ForeColor = saldoPendiente > 0
                                ? Color.FromArgb(220, 53, 69)
                                : Color.FromArgb(0, 150, 136);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando saldo: {ex.Message}");
                lblTotalPendiente.Text = "Pendiente: No disponible";
                lblTotalPendiente.ForeColor = Color.Gray;
            }
        }

        private void TxtMonto_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
                e.KeyChar != '.' && e.KeyChar != ',')
            {
                e.Handled = true;
            }

            if ((e.KeyChar == '.' || e.KeyChar == ',') &&
                (txtMonto.Text.Contains('.') || txtMonto.Text.Contains(',')))
            {
                e.Handled = true;
            }
        }

        private void TxtMonto_TextChanged(object sender, EventArgs e)
        {
            ValidarFormulario();
        }

        private void ValidarFormulario()
        {
            bool valido = cboProveedor.SelectedIndex > 0 &&
                         !string.IsNullOrWhiteSpace(txtMonto.Text) &&
                         decimal.TryParse(txtMonto.Text.Replace(',', '.'), out decimal monto) &&
                         monto > 0;

            btnConfirmar.Enabled = valido;
        }

        private async void BtnConfirmar_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtMonto.Text.Replace(',', '.'), out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Ingrese un monto válido mayor a cero.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMonto.Focus();
                return;
            }

            if (cboProveedor.SelectedIndex <= 0)
            {
                MessageBox.Show("Seleccione un proveedor.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboProveedor.Focus();
                return;
            }

            dynamic proveedorSeleccionado = cboProveedor.SelectedItem;
            int idProveedor = proveedorSeleccionado.Id;
            string nombreProveedor = proveedorSeleccionado.Nombre;

            string facturaTexto = "Sin vincular";
            int? compraId = null;

            if (cboFacturaPendiente.SelectedIndex > 0)
            {
                dynamic facturaSeleccionada = cboFacturaPendiente.SelectedItem;
                facturaTexto = facturaSeleccionada.Texto;
                compraId = facturaSeleccionada.CompraId;
            }

            var mensaje = $"¿Confirmar pago de {monto:C2} a {nombreProveedor}?\n\n" +
                          $"Factura: {facturaTexto}\n" +
                          $"Método: {cboMetodoPago.SelectedItem}\n";

            var resultado = MessageBox.Show(mensaje, "Confirmar Pago",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (resultado == DialogResult.Yes)
            {
                Monto = monto;
                ProveedorIdSeleccionado = idProveedor;
                ProveedorSeleccionado = nombreProveedor;
                MetodoPago = cboMetodoPago.SelectedItem?.ToString() ?? "Efectivo";
                Referencia = facturaTexto;
                CompraIdSeleccionada = compraId;
                Observaciones = txtObservaciones.Text.Trim();

                bool guardadoExitoso = await GuardarPagoEnBaseDatos();

                if (guardadoExitoso)
                {
                    Confirmado = true;

                    MessageBox.Show(
                        $"✅ Pago registrado exitosamente\n\n" +
                        $"Proveedor: {ProveedorSeleccionado}\n" +
                        $"Monto: {Monto:C2}\n" +
                        $"Método: {MetodoPago}",
                        "Pago Exitoso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show(
                        "❌ No se pudo guardar el pago.\n\n" +
                        "Revise los logs para más detalles.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            Confirmado = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(550, 520);
            this.Name = "PagoProveedorRapidoForm";
            this.ResumeLayout(false);
        }
    }
}