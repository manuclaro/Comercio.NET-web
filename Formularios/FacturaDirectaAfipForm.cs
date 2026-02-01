using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Comercio.NET.Servicios;
using ArcaWS;

namespace Comercio.NET.Formularios
{
    public partial class FacturaDirectaAfipForm : Form
    {
        private NumericUpDown numMonto;
        private ComboBox cboTipoFactura;
        private TextBox txtCuit;
        private TextBox txtConcepto;
        private Label lblMonto;
        private Label lblTipoFactura;
        private Label lblCuit;
        private Label lblConcepto;
        private Button btnGenerar;
        private Button btnCancelar;
        private Label lblEstado;
        private Panel panelInfo;

        // ✅ NUEVO: Variables para tokens AFIP (igual que en SeleccionImpresionForm)
        private string TokenAfip { get; set; }
        private string SignAfip { get; set; }

        public FacturaDirectaAfipForm()
        {
            InitializeComponent();
            ConfigurarFormulario();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new Size(550, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Generar Factura Directa AFIP";
            this.ShowInTaskbar = false;
            this.BackColor = Color.White;
            this.ResumeLayout(false);
        }

        private void CargarTiposFacturaSegunConfiguracion()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string ambienteActivo = config["AFIP:AmbienteActivo"] ?? "Produccion";
                string condicionConfig = config[$"AFIP:{ambienteActivo}:CondicionIVA"];

                bool esMonotributo = condicionConfig?.Equals("Monotributo", StringComparison.OrdinalIgnoreCase) ?? false;

                System.Diagnostics.Debug.WriteLine($"[FACTURA DIRECTA] Condición IVA: {condicionConfig}");
                System.Diagnostics.Debug.WriteLine($"[FACTURA DIRECTA] Es Monotributo: {esMonotributo}");

                cboTipoFactura.Items.Clear();

                if (esMonotributo)
                {
                    // ✅ MONOTRIBUTO: Solo Factura C
                    cboTipoFactura.Items.Add("Factura C");
                    cboTipoFactura.SelectedIndex = 0;
                    cboTipoFactura.Enabled = false; // No permitir cambiar
                    System.Diagnostics.Debug.WriteLine("[FACTURA DIRECTA] ✅ Configurado para Monotributo - Solo Factura C");
                }
                else
                {
                    // ✅ RESPONSABLE INSCRIPTO: A y B (por defecto B)
                    cboTipoFactura.Items.Add("Factura A");
                    cboTipoFactura.Items.Add("Factura B");
                    cboTipoFactura.SelectedIndex = 1; // Por defecto B
                    cboTipoFactura.Enabled = true; // Permitir cambiar entre A y B
                    System.Diagnostics.Debug.WriteLine("[FACTURA DIRECTA] ✅ Configurado para Responsable Inscripto - A y B disponibles (por defecto B)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FACTURA DIRECTA] Error cargando tipos: {ex.Message}");
                // Fallback: cargar todas las opciones
                cboTipoFactura.Items.Clear();
                cboTipoFactura.Items.AddRange(new object[] { "Factura A", "Factura B", "Factura C" });
                cboTipoFactura.SelectedIndex = 2;
            }
        }

        private void ConfigurarFormulario()
        {
            int margin = 20;
            int yPos = 20;
            int labelWidth = 150;
            int controlWidth = 350;

            // Título
            var lblTitulo = new Label
            {
                Text = "📄 FACTURA DIRECTA AFIP",
                Location = new Point(margin, yPos),
                Size = new Size(500, 35),
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 123, 255)
            };
            this.Controls.Add(lblTitulo);
            yPos += 50;

            // Panel informativo
            panelInfo = new Panel
            {
                Location = new Point(margin, yPos),
                Size = new Size(500, 80),
                BackColor = Color.FromArgb(255, 243, 205),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblInfo = new Label
            {
                Text = "💡 Esta funcionalidad permite generar facturas de AFIP\n" +
                       "   sin necesidad de cargar productos. Útil para ajustes\n" +
                       "   de facturación diaria o ventas externas.",
                Location = new Point(15, 10),
                Size = new Size(470, 60),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(133, 100, 4)
            };
            panelInfo.Controls.Add(lblInfo);
            this.Controls.Add(panelInfo);
            yPos += 100;

            // Tipo de Factura
            lblTipoFactura = new Label
            {
                Text = "Tipo de Factura:",
                Location = new Point(margin, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblTipoFactura);

            cboTipoFactura = new ComboBox
            {
                Location = new Point(margin + labelWidth, yPos),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            // ✅ REMOVIDO: Ya no se cargan items aquí
            // ✅ REMOVIDO: Ya no se establece SelectedIndex aquí
            cboTipoFactura.SelectedIndexChanged += CboTipoFactura_SelectedIndexChanged;
            this.Controls.Add(cboTipoFactura);
            yPos += 40;

            // CUIT (solo para A y B)
            lblCuit = new Label
            {
                Text = "CUIT Cliente:",
                Location = new Point(margin, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Visible = false
            };
            this.Controls.Add(lblCuit);

            txtCuit = new TextBox
            {
                Location = new Point(margin + labelWidth, yPos),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "20-12345678-9",
                Visible = false
            };
            this.Controls.Add(txtCuit);
            yPos += 40;

            // Monto
            lblMonto = new Label
            {
                Text = "Monto a Facturar:",
                Location = new Point(margin, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblMonto);

            numMonto = new NumericUpDown
            {
                Location = new Point(margin + labelWidth, yPos),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                DecimalPlaces = 2,
                Minimum = 0.01m,
                Maximum = 999999.99m,
                Value = 100m,
                ThousandsSeparator = true,
                TextAlign = HorizontalAlignment.Right
            };
            this.Controls.Add(numMonto);
            yPos += 40;

            // Concepto
            lblConcepto = new Label
            {
                Text = "Concepto/Motivo:",
                Location = new Point(margin, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblConcepto);

            txtConcepto = new TextBox
            {
                Location = new Point(margin + labelWidth, yPos),
                Size = new Size(controlWidth, 60),
                Font = new Font("Segoe UI", 10F),
                Multiline = true,
                PlaceholderText = "Ej: Ajuste facturación diaria, venta externa, etc.",
                ScrollBars = ScrollBars.Vertical
            };
            this.Controls.Add(txtConcepto);
            yPos += 80;

            // Estado
            lblEstado = new Label
            {
                Location = new Point(margin, yPos),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Blue,
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblEstado);
            yPos += 35;

            // Botones
            btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(300, yPos),
                Size = new Size(110, 40),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnCancelar);

            btnGenerar = new Button
            {
                Text = "Generar Factura",
                Location = new Point(420, yPos),
                Size = new Size(110, 40),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGenerar.FlatAppearance.BorderSize = 0;
            btnGenerar.Click += BtnGenerar_Click;
            this.Controls.Add(btnGenerar);

            // Enfocar el monto
            this.Load += (s, e) =>
            {
                CargarTiposFacturaSegunConfiguracion(); // ✅ NUEVO: Cargar tipos primero
                numMonto.Focus();
            };
        }

        private void CboTipoFactura_SelectedIndexChanged(object sender, EventArgs e)
        {
            // ✅ MODIFICADO: Mostrar CUIT SOLO para Factura A
            bool requiereCuit = cboTipoFactura.SelectedItem?.ToString() == "Factura A";
            lblCuit.Visible = requiereCuit;
            txtCuit.Visible = requiereCuit;

            System.Diagnostics.Debug.WriteLine($"[FACTURA DIRECTA] Tipo seleccionado: {cboTipoFactura.SelectedItem}, Requiere CUIT: {requiereCuit}");
        }

        private async void BtnGenerar_Click(object sender, EventArgs e)
        {
            try
            {
                // Validaciones
                if (numMonto.Value <= 0)
                {
                    MessageBox.Show("Debe ingresar un monto válido mayor a cero.",
                        "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numMonto.Focus();
                    return;
                }

                // ✅ MODIFICADO: Concepto opcional (sin validación de longitud mínima)
                string concepto = txtConcepto.Text.Trim();
                if (string.IsNullOrWhiteSpace(concepto))
                {
                    concepto = "Factura directa AFIP"; // Valor por defecto
                    System.Diagnostics.Debug.WriteLine("[FACTURA DIRECTA] Concepto vacío - usando valor por defecto");
                }

                string tipoFactura = ObtenerTipoFactura();
                string cuitCliente = txtCuit.Text.Trim();

                // ✅ MODIFICADO: Validar CUIT SOLO para Factura A
                if (tipoFactura == "A" && string.IsNullOrWhiteSpace(cuitCliente))
                {
                    MessageBox.Show("Debe ingresar el CUIT del cliente para Factura A.",
                        "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCuit.Focus();
                    return;
                }

                // Confirmación
                var resultado = MessageBox.Show(
                    $"¿Confirma la generación de la siguiente factura en AFIP?\n\n" +
                    $"Tipo: Factura {tipoFactura}\n" +
                    $"Monto: {numMonto.Value:C2}\n" +
                    $"Concepto: {concepto}\n" + // ✅ Usar variable concepto
                    (string.IsNullOrEmpty(cuitCliente) ? "" : $"CUIT: {cuitCliente}\n") +
                    "\n¿Desea continuar?",
                    "Confirmar generación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resultado != DialogResult.Yes)
                {
                    return;
                }

                // Deshabilitar controles
                btnGenerar.Enabled = false;
                btnCancelar.Enabled = false;
                lblEstado.Text = "⏳ Generando factura en AFIP...";
                lblEstado.ForeColor = Color.Blue;

                // ✅ MODIFICADO: Pasar concepto (no txtConcepto.Text.Trim())
                await GenerarFacturaDirectaAfip(tipoFactura, numMonto.Value, concepto, cuitCliente);

                lblEstado.Text = "✅ Factura generada correctamente";
                lblEstado.ForeColor = Color.Green;

                await Task.Delay(1500);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                lblEstado.Text = $"❌ Error: {ex.Message}";
                lblEstado.ForeColor = Color.Red;
                btnGenerar.Enabled = true;
                btnCancelar.Enabled = true;

                MessageBox.Show($"Error al generar la factura:\n\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string ObtenerTipoFactura()
        {
            string seleccionado = cboTipoFactura.SelectedItem?.ToString() ?? "Factura C";

            if (seleccionado.Contains("A")) return "A";
            if (seleccionado.Contains("B")) return "B";
            if (seleccionado.Contains("C")) return "C";

            return "C"; // Por defecto
        }

        private async Task GenerarFacturaDirectaAfip(string tipoFactura, decimal monto, string concepto, string cuitCliente)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 === FACTURA DIRECTA AFIP - INICIO ===");
                System.Diagnostics.Debug.WriteLine($"[FACTURA DIRECTA] Tipo: {tipoFactura}, Monto: {monto:C2}");

                // ✅ PASO 1: Autenticar con AFIP (MISMO CÓDIGO QUE SeleccionImpresionForm)
                string cuitEmisor = ObtenerCuitEmisor();
                await AutenticarConAfipReal(cuitEmisor);

                // ✅ PASO 2: Determinar tipo de comprobante
                int tipoComprobante = tipoFactura switch
                {
                    "A" => 1,
                    "B" => 6,
                    "C" => 11,
                    _ => 11
                };

                // ✅ PASO 3: Obtener siguiente número (MISMO CÓDIGO QUE SeleccionImpresionForm)
                int puntoVenta = ObtenerPuntoVentaActivo();
                int ultimoNumero = await ObtenerUltimoNumeroComprobanteReal(tipoComprobante, puntoVenta);
                int numeroComprobante = ultimoNumero + 1;

                System.Diagnostics.Debug.WriteLine($"[FACTURA DIRECTA] PV: {puntoVenta}, Último: {ultimoNumero}, Nuevo: {numeroComprobante}");

                // ✅ PASO 4: Solicitar CAE (MISMO CÓDIGO QUE SeleccionImpresionForm)
                var resultadoCAE = await SolicitarCAEReal(tipoComprobante, puntoVenta, numeroComprobante, monto, tipoFactura, cuitCliente);

                if (!resultadoCAE.exito)
                {
                    throw new Exception($"Error obteniendo CAE: {resultadoCAE.error}");
                }

                // ✅ PASO 5: Guardar en base de datos
                string numeroFormateado = FormatearNumeroFactura(tipoComprobante, puntoVenta, numeroComprobante);
                await GuardarFacturaEnBD(
                    tipoFactura,
                    numeroComprobante,
                    puntoVenta,
                    monto,
                    concepto,
                    resultadoCAE.cae,
                    resultadoCAE.vencimiento ?? DateTime.Now,
                    cuitCliente,
                    numeroFormateado);

                System.Diagnostics.Debug.WriteLine($"✅ Factura directa guardada: {numeroFormateado}");
                System.Diagnostics.Debug.WriteLine($"✅ CAE: {resultadoCAE.cae}, Venc: {resultadoCAE.vencimiento:dd/MM/yyyy}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en GenerarFacturaDirectaAfip: {ex.Message}");
                throw;
            }
        }

        // ✅ COPIADO DE SeleccionImpresionForm
        private async Task AutenticarConAfipReal(string cuitEmisor)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔑 === AUTENTICACIÓN AFIP ===");

                var (tieneTokenValido, mensaje, minutosRestantes) = AfipAuthenticator.VerificarTokensExistentes("wsfe");

                if (tieneTokenValido && minutosRestantes > 2)
                {
                    var tokenExistente = AfipAuthenticator.GetExistingToken("wsfe");
                    if (tokenExistente.HasValue)
                    {
                        TokenAfip = tokenExistente.Value.token;
                        SignAfip = tokenExistente.Value.sign;
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Usando token válido existente");
                        return;
                    }
                }

                var (token, sign, expiration) = await AfipAuthenticator.GetTAAsync("wsfe");
                TokenAfip = token;
                SignAfip = sign;

                System.Diagnostics.Debug.WriteLine("✅ Autenticación AFIP completada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 Error en autenticación: {ex.Message}");
                throw new Exception($"Error de autenticación AFIP: {ex.Message}");
            }
        }

        // ✅ COPIADO DE SeleccionImpresionForm
        private async Task<int> ObtenerUltimoNumeroComprobanteReal(int tipoComprobante, int puntoVenta)
        {
            try
            {
                using (var wsfeClient = AfipAuthenticator.CrearClienteWSFE())
                {
                    var authRequest = new ArcaWS.FEAuthRequest
                    {
                        Token = TokenAfip,
                        Sign = SignAfip,
                        Cuit = long.Parse(ObtenerCuitEmisor().Replace("-", ""))
                    };

                    System.Diagnostics.Debug.WriteLine($"[AFIP] Consultando último número - Tipo: {tipoComprobante}, PV: {puntoVenta}");

                    var response = await wsfeClient.FECompUltimoAutorizadoAsync(authRequest, puntoVenta, tipoComprobante);
                    var resultado = response.Body.FECompUltimoAutorizadoResult;

                    if (resultado?.Errors != null && resultado.Errors.Length > 0)
                    {
                        string errores = string.Join(", ", resultado.Errors.Select(e => $"{e.Code}: {e.Msg}"));
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ Errores: {errores}");
                        throw new Exception($"Error AFIP: {errores}");
                    }

                    int ultimoNumero = resultado?.CbteNro ?? 0;
                    System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Último número autorizado: {ultimoNumero}");

                    return ultimoNumero;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] ⚠️ Error obteniendo último número: {ex.Message}");
                throw;
            }
        }

        // ✅ ADAPTADO DE SeleccionImpresionForm - SIMPLIFICADO PARA FACTURA DIRECTA
        private async Task<(bool exito, string cae, DateTime? vencimiento, string error)> SolicitarCAEReal(
            int tipoComprobante, int puntoVenta, int numero, decimal montoTotal, string tipoFactura, string cuitCliente = "")
        {
            try
            {
                using (var wsfeClient = AfipAuthenticator.CrearClienteWSFE())
                {
                    var authRequest = new ArcaWS.FEAuthRequest
                    {
                        Token = TokenAfip,
                        Sign = SignAfip,
                        Cuit = long.Parse(ObtenerCuitEmisor().Replace("-", ""))
                    };

                    System.Diagnostics.Debug.WriteLine($"[AFIP] === SOLICITUD CAE ===");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Tipo: {tipoComprobante}, PV: {puntoVenta}, Num: {numero}");

                    bool esFacturaC = (tipoComprobante == 11);

                    // ✅ Determinar docTipo y docNro según tipo de factura
                    int docTipo;
                    long docNro;

                    if (tipoComprobante == 1) // Factura A
                    {
                        docTipo = 80; // CUIT
                        docNro = !string.IsNullOrEmpty(cuitCliente) ? long.Parse(cuitCliente.Replace("-", "")) : 0;
                    }
                    else // Factura B o C
                    {
                        docTipo = 99; // Sin identificación / Consumidor Final
                        docNro = 0;
                    }

                    // ✅ Calcular importes según tipo de factura
                    decimal importeNetoCalculado;
                    decimal importeIvaCalculado;

                    if (esFacturaC)
                    {
                        // Factura C: Monto total sin discriminar IVA
                        importeNetoCalculado = montoTotal;
                        importeIvaCalculado = 0;
                    }
                    else
                    {
                        // Factura A/B: Discriminar IVA 21%
                        importeNetoCalculado = Math.Round(montoTotal / 1.21m, 2);
                        importeIvaCalculado = Math.Round(montoTotal - importeNetoCalculado, 2);
                    }

                    System.Diagnostics.Debug.WriteLine($"[AFIP] DocTipo: {docTipo}, DocNro: {docNro}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Total: {montoTotal:F2}, Neto: {importeNetoCalculado:F2}, IVA: {importeIvaCalculado:F2}");

                    // ✅ Crear comprobante
                    var comprobante = new ArcaWS.FECAEDetRequest
                    {
                        Concepto = 1, // Productos
                        DocTipo = docTipo,
                        DocNro = docNro,
                        CbteDesde = numero,
                        CbteHasta = numero,
                        CbteFch = DateTime.Now.ToString("yyyyMMdd"),
                        ImpTotal = (double)montoTotal,
                        ImpTotConc = 0,
                        ImpNeto = (double)importeNetoCalculado,
                        ImpOpEx = 0,
                        ImpIVA = (double)importeIvaCalculado,
                        ImpTrib = 0,
                        MonId = "PES",
                        MonCotiz = 1
                    };

                    // ✅ CRÍTICO: Asignar condición IVA receptor
                    int ivaPerNro = tipoComprobante == 1 ? 1 : 5; // Resp. Inscripto (A) o Cons. Final (B/C)

                    var propertyCondicionIVA = comprobante.GetType().GetProperty("CondicionIVAReceptorId");
                    if (propertyCondicionIVA != null)
                    {
                        propertyCondicionIVA.SetValue(comprobante, ivaPerNro);
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ CondicionIVAReceptorId = {ivaPerNro}");
                    }

                    // ✅ Agregar IVA solo para Factura A y B
                    if (!esFacturaC && importeIvaCalculado > 0)
                    {
                        comprobante.Iva = new[]
                        {
                            new ArcaWS.AlicIva
                            {
                                Id = 5, // 21%
                                BaseImp = (double)importeNetoCalculado,
                                Importe = (double)importeIvaCalculado
                            }
                        };
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Array IVA agregado");
                    }

                    var request = new ArcaWS.FECAERequest
                    {
                        FeCabReq = new ArcaWS.FECAECabRequest
                        {
                            CantReg = 1,
                            PtoVta = puntoVenta,
                            CbteTipo = tipoComprobante
                        },
                        FeDetReq = new ArcaWS.FECAEDetRequest[] { comprobante }
                    };

                    System.Diagnostics.Debug.WriteLine($"[AFIP] 📤 Enviando solicitud CAE...");

                    var response = await wsfeClient.FECAESolicitarAsync(authRequest, request);
                    var resultado = response.Body.FECAESolicitarResult;

                    if (resultado?.Errors != null && resultado.Errors.Length > 0)
                    {
                        string errores = string.Join(", ", resultado.Errors.Select(e => e.Msg));
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ Errores: {errores}");
                        return (false, "", null, errores);
                    }

                    if (resultado?.FeDetResp != null && resultado.FeDetResp.Length > 0)
                    {
                        var detalle = resultado.FeDetResp[0];

                        if (!string.IsNullOrEmpty(detalle.CAE))
                        {
                            DateTime? fechaVencimiento = null;
                            if (!string.IsNullOrEmpty(detalle.CAEFchVto))
                            {
                                DateTime.TryParseExact(detalle.CAEFchVto, "yyyyMMdd",
                                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha);
                                fechaVencimiento = fecha;
                            }

                            System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ CAE obtenido: {detalle.CAE}");
                            return (true, detalle.CAE, fechaVencimiento, "");
                        }
                        else
                        {
                            string errores = detalle.Observaciones != null
                                ? string.Join(", ", detalle.Observaciones.Select(o => o.Msg))
                                : "Sin detalles";
                            return (false, "", null, $"AFIP rechazó: {errores}");
                        }
                    }

                    return (false, "", null, "Respuesta inválida de AFIP");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] 💥 Error: {ex.Message}");
                return (false, "", null, $"Error: {ex.Message}");
            }
        }

        // ✅ COPIADO DE SeleccionImpresionForm
        private int ObtenerPuntoVentaActivo()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                string ambienteActivo = config["AFIP:AmbienteActivo"] ?? "Testing";
                string puntoVentaStr = config[$"AFIP:{ambienteActivo}:PuntoVenta"];

                if (string.IsNullOrEmpty(puntoVentaStr))
                {
                    System.Diagnostics.Debug.WriteLine($"[PUNTO VENTA] ⚠️ No configurado para {ambienteActivo}, usando 1");
                    return 1;
                }

                if (!int.TryParse(puntoVentaStr, out int puntoVenta))
                {
                    System.Diagnostics.Debug.WriteLine($"[PUNTO VENTA] ⚠️ Valor inválido '{puntoVentaStr}', usando 1");
                    return 1;
                }

                System.Diagnostics.Debug.WriteLine($"[PUNTO VENTA] ✅ Ambiente: {ambienteActivo}, PV: {puntoVenta}");
                return puntoVenta;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PUNTO VENTA] ❌ Error: {ex.Message}, usando 1");
                return 1;
            }
        }

        // ✅ COPIADO DE SeleccionImpresionForm
        private string ObtenerCuitEmisor()
        {
            try
            {
                return AfipAuthenticator.ObtenerCUITActivo();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CUIT] Error: {ex.Message}");
                throw new Exception("No se pudo obtener el CUIT del emisor");
            }
        }

        // ✅ COPIADO DE SeleccionImpresionForm
        private string FormatearNumeroFactura(int tipoComprobante, int puntoVenta, int numero)
        {
            string tipoLetra = tipoComprobante switch
            {
                1 => "A",
                6 => "B",
                11 => "C",
                _ => "X"
            };

            return $"{tipoLetra} {puntoVenta:D4}-{numero:D8}";
        }

        // ✅ MODIFICADO: Guardar con SET ARITHABORT ON
        private async Task GuardarFacturaEnBD(
    string tipoFactura,
    int numeroComprobante,
    int puntoVenta,
    decimal total,
    string concepto,
    string cae,
    DateTime vencimientoCae,
    string cuitCliente,
    string numeroFormateado)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            string connectionString = config.GetConnectionString("DefaultConnection");

            // ✅ CRÍTICO: Usar el nuevo método que INCREMENTA el contador
            int numeroRemito = await ObtenerYReservarNumeroRemito(connectionString);

            System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════");
            System.Diagnostics.Debug.WriteLine($"💾 GUARDANDO FACTURA DIRECTA:");
            System.Diagnostics.Debug.WriteLine($"   Número Remito: {numeroRemito}");
            System.Diagnostics.Debug.WriteLine($"   Número Factura: {numeroFormateado}");
            System.Diagnostics.Debug.WriteLine($"   Tipo: Factura{tipoFactura}");
            System.Diagnostics.Debug.WriteLine($"   Total: {total:C2}");
            System.Diagnostics.Debug.WriteLine($"   CAE: {cae}");
            System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════");

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // ✅ CRÍTICO: Establecer opciones de conexión ANTES de cualquier operación
                using (var cmdConfig = new SqlCommand("SET ARITHABORT ON; SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;", connection))
                {
                    await cmdConfig.ExecuteNonQueryAsync();
                }

                System.Diagnostics.Debug.WriteLine($"✅ Opciones SQL configuradas correctamente");

                // ✅ INSERTAR EN FACTURAS CON ESTRUCTURA CORRECTA
                var queryFactura = @"
    INSERT INTO Facturas 
        (NumeroRemito, NroFactura, TipoFactura, ImporteTotal, ImporteFinal, IVA, 
         Fecha, Hora, FormadePago, CAENumero, CAEVencimiento, CUITCliente, esCtaCte)
    VALUES 
        (@numeroRemito, @nroFactura, @tipoFactura, @importeTotal, @importeFinal, @iva,
         @fecha, @hora, @formaPago, @cae, @caeVencimiento, @cuit, 0)";

                decimal importeIVA = tipoFactura == "C" ? 0 : Math.Round(total - (total / 1.21m), 2);

                using (var cmd = new SqlCommand(queryFactura, connection))
                {
                    cmd.Parameters.AddWithValue("@numeroRemito", numeroRemito);
                    cmd.Parameters.AddWithValue("@nroFactura", numeroFormateado);
                    cmd.Parameters.AddWithValue("@tipoFactura", $"Factura{tipoFactura}"); // ej: "FacturaA"
                    cmd.Parameters.AddWithValue("@importeTotal", total);
                    cmd.Parameters.AddWithValue("@importeFinal", total);
                    cmd.Parameters.AddWithValue("@iva", importeIVA);
                    cmd.Parameters.AddWithValue("@fecha", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@hora", DateTime.Now.TimeOfDay);
                    cmd.Parameters.AddWithValue("@formaPago", "Efectivo");
                    cmd.Parameters.AddWithValue("@cae", cae);
                    cmd.Parameters.AddWithValue("@caeVencimiento", vencimientoCae);
                    cmd.Parameters.AddWithValue("@cuit", (object)cuitCliente ?? DBNull.Value);

                    int filasAfectadas = await cmd.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine($"[BD] ✅ Factura insertada - Filas afectadas: {filasAfectadas}");
                }

                // ✅ INSERTAR PRODUCTO FICTICIO EN VENTAS
                var queryVenta = @"
                            INSERT INTO Ventas 
                                (codigo, descripcion, cantidad, precio, total, NroFactura, PorcentajeIva)
                            VALUES 
                                (@codigo, @descripcion, @cantidad, @precio, @total, @nroFactura, @porcentajeIva)";
                decimal precioUnitario = tipoFactura == "C" ? total : Math.Round(total / 1.21m, 2);

                using (var cmd = new SqlCommand(queryVenta, connection))
                {
                    cmd.Parameters.AddWithValue("@codigo", "FDIRECTA");
                    cmd.Parameters.AddWithValue("@descripcion", concepto);
                    cmd.Parameters.AddWithValue("@cantidad", 1);
                    cmd.Parameters.AddWithValue("@precio", precioUnitario);
                    cmd.Parameters.AddWithValue("@total", total);
                    cmd.Parameters.AddWithValue("@nroFactura", numeroRemito); // ✅ USAR numeroRemito, no numeroRemito.ToString()
                    cmd.Parameters.AddWithValue("@porcentajeIva", tipoFactura == "C" ? 0 : 21);

                    int filasVenta = await cmd.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine($"[BD] ✅ Venta insertada - Filas afectadas: {filasVenta}");
                }

                System.Diagnostics.Debug.WriteLine($"✅ Factura guardada completamente - Remito: {numeroRemito}, Factura: {numeroFormateado}");
            }
        }

        // ✅ MODIFICADO: Agregar SET ARITHABORT también aquí
        private async Task<int> ObtenerYReservarNumeroRemito(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // ✅ Configurar opciones de conexión
                using (var cmdConfig = new SqlCommand("SET ARITHABORT ON; SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;", connection))
                {
                    await cmdConfig.ExecuteNonQueryAsync();
                }

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // ✅ PASO 1: Incrementar el contador GLOBAL (IGUAL QUE Ventas.cs)
                        var queryIncrement = "UPDATE numeroremito SET nroremito = nroremito + 1";
                        using (var cmdIncrement = new SqlCommand(queryIncrement, connection, transaction))
                        {
                            await cmdIncrement.ExecuteNonQueryAsync();
                            System.Diagnostics.Debug.WriteLine($"[REMITO] ✅ Contador global incrementado");
                        }

                        // ✅ PASO 2: Obtener el nuevo número
                        var queryObtener = "SELECT nroremito FROM numeroremito";
                        using (var cmdObtener = new SqlCommand(queryObtener, connection, transaction))
                        {
                            var result = await cmdObtener.ExecuteScalarAsync();
                            int numeroRemito = Convert.ToInt32(result);

                            System.Diagnostics.Debug.WriteLine($"[REMITO] ✅ Número reservado: {numeroRemito}");

                            transaction.Commit();
                            return numeroRemito;
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"[REMITO] ❌ Error: {ex.Message}");
                        throw new Exception($"Error reservando número de remito: {ex.Message}");
                    }
                }
            }
        }

    }
}