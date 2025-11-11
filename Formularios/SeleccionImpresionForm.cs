using System;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Drawing.Printing;
using System.Globalization;
using System.ServiceModel;
using System.Threading.Tasks;
using Comercio.NET.Servicios;
using Comercio.NET.Controles;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace Comercio.NET
{
    public partial class SeleccionImpresionForm : Form
    {
        public enum OpcionImpresion
        {
            Ninguna,
            RemitoTicket,
            FacturaB,
            FacturaA
        }

        public enum OpcionPago
        {
            Efectivo,
            DNI,
            MercadoPago
        }

        public OpcionImpresion OpcionSeleccionada { get; private set; } = OpcionImpresion.Ninguna;
        public OpcionPago OpcionPagoSeleccionada { get; private set; } = OpcionPago.Efectivo;

        private TextBox txtCuit;
        private Label lblRazonSocial;

        // MODIFICADO: Referencias a los botones para poder controlar su estado
        private Button btnRemito;
        private Button btnFacturaB;
        private Button btnFacturaA;

        // NUEVO: Botón para finalizar sin imprimir
        private Button btnFinalizarSinImpresion;

        // NUEVO: Control de múltiples pagos
        private MultiplePagosControl multiplePagosControl;

        // NUEVO: Toggle para modo de pago múltiple
        private CheckBox chkPagoMultiple;
        private Panel panelPagoSimple;
        private Panel panelPagoMultiple;

        // ORIGINAL: Referencias a los RadioButtons para retrocompatibilidad
        private RadioButton rbEfectivo;
        private RadioButton rbDNI;
        private RadioButton rbMercadoPago;

        // CORREGIDO: Eliminar referencias a controles que no existen
        private Label lblMensajeInformativo;
        private CheckBox chkMultiplesPagos; // Referencia corregida

        // NUEVO: Label para mostrar el importe total a pagar
        private Label lblImporteTotal;

        // NUEVO: Referencia al botón Cancelar (antes era variable local)
        private Button btnCancelar;

        private decimal montoLimiteFacturacion = 0m; // NUEVO: Límite configurado
        private decimal montoAcumuladoHoy = 0m; // NUEVO: Total facturado en el día
        private bool limitarFacturacion = false; // NUEVO: Si está habilitada la restricción

        // Delegate para el callback después de procesar la venta
        public Func<string, string, string, string, DateTime?, int, string, Task> OnProcesarVenta { get; set; }

        private decimal importeTotalVenta;
        private Ventas formularioPadre;

        // ELIMINADO: Cache de tokens local - usar el del AfipAuthenticator
        // NUEVO: Método para limpiar cache de tokens AFIP
        public static void LimpiarCacheTokensAfip()
        {
            AfipAuthenticator.ClearTokenCache("wsfe");
            System.Diagnostics.Debug.WriteLine("🗑️ Cache de tokens AFIP limpiado");
        }

        // NUEVO: Método para verificar estado del cache
        public static bool TieneCacheTokensValido()
        {
            var tokenCache = AfipAuthenticator.GetExistingToken("wsfe");
            bool esValido = tokenCache.HasValue &&
                           !string.IsNullOrEmpty(tokenCache.Value.token) &&
                           !string.IsNullOrEmpty(tokenCache.Value.sign);

            System.Diagnostics.Debug.WriteLine($"🔍 Estado cache tokens: {(esValido ? "VÁLIDO" : "INVÁLIDO")}");

            return esValido;
        }

        // Propiedades para almacenar datos del CAE
        public string CAENumero { get; private set; } = "";
        public DateTime? CAEVencimiento { get; private set; } = null;
        public int NumeroFacturaAfip { get; private set; } = 0;
        public bool FinalizadoSinImpresion { get; private set; } = false;

        public string TokenAfip { get; set; }
        public string SignAfip { get; set; }

        // NUEVO: Propiedades para acceder a los datos de pagos múltiples
        public List<MultiplePagosControl.DetallePago> PagosMultiples => multiplePagosControl?.Pagos ?? new List<MultiplePagosControl.DetallePago>();
        public bool EsPagoMultiple => chkPagoMultiple?.Checked ?? false;
        public string ResumenPagos => EsPagoMultiple ? multiplePagosControl?.ObtenerResumenPagos() ?? "" : OpcionPagoSeleccionada.ToString();

        // AGREGAR: En la sección de variables de la clase (alrededor de línea ~40)
        private bool usarVistaPrevia = true; // NUEVO: Variable para controlar el modo de impresión

        public SeleccionImpresionForm(decimal importeTotal = 0, Ventas padre = null)
        {
            System.Diagnostics.Debug.WriteLine($"[SELECCIÓN] Iniciando con importe: {importeTotal:C2}");

            this.importeTotalVenta = importeTotal;
            this.formularioPadre = padre;

            this.Text = "Seleccione tipo de impresión y método de pago";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Width = 700;
            this.Height = 400;

            CrearControles();
            ConfigurarEventos();

            // ✅ NUEVO: Cargar límite de facturación ANTES de actualizar opciones
            CargarLimiteFacturacion();

            // ✅ CORRECTO: Ahora es síncrono, no causa deadlock
            CargarMontoAcumuladoHoy();

            ActualizarOpcionesImpresion();

            this.Resize += (s, e) => PosicionarBotones();

            SettingsManager.SettingsReloaded += () =>
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            AplicarConfiguracionFacturacion();
                            CargarLimiteFacturacion(); // ✅ NUEVO: Recargar límite
                            ActualizarOpcionesImpresion();
                            CargarConfiguracionVistaPrevia();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error aplicando configuración recargada: {ex.Message}");
                        }
                    }));
                }
            };

            CargarConfiguracionVistaPrevia();
        }

        // ✅ NUEVO: Método para cargar la configuración del límite de facturación
        private void CargarLimiteFacturacion()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                limitarFacturacion = config.GetValue<bool>("RestriccionesImpresion:LimitarFacturacion", false);
                montoLimiteFacturacion = config.GetValue<decimal>("RestriccionesImpresion:MontoLimiteFacturacion", 0m);

                System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] Habilitado: {limitarFacturacion}");
                System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] Monto límite: {montoLimiteFacturacion:C2}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] Error cargando configuración: {ex.Message}");
                limitarFacturacion = false;
                montoLimiteFacturacion = 0m;
            }
        }

        // ✅ NUEVO: Método async para calcular el monto acumulado de facturas electrónicas del día
        private void CargarMontoAcumuladoHoy()
        {
            montoAcumuladoHoy = 0m;

            if (!limitarFacturacion || montoLimiteFacturacion <= 0)
            {
                System.Diagnostics.Debug.WriteLine("[LÍMITE FACTURACIÓN] Restricción deshabilitada o sin límite configurado");
                return;
            }

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
                SELECT ISNULL(SUM(ImporteTotal), 0) AS TotalFacturado
                FROM Facturas
                WHERE CAST(Fecha AS DATE) = CAST(GETDATE() AS DATE)
                  AND TipoFactura IN ('FacturaA', 'FacturaB')";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        connection.Open(); // ✅ Síncrono
                        var result = cmd.ExecuteScalar(); // ✅ Síncrono

                        if (result != null && result != DBNull.Value)
                        {
                            montoAcumuladoHoy = Convert.ToDecimal(result);
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] ===== ESTADO ACTUAL =====");
                System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] Monto acumulado hoy: {montoAcumuladoHoy:C2}");
                System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] Límite diario: {montoLimiteFacturacion:C2}");
                System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] Disponible: {(montoLimiteFacturacion - montoAcumuladoHoy):C2}");
                System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] Factura actual: {importeTotalVenta:C2}");
                System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] =============================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] ❌ Error calculando monto acumulado: {ex.Message}");
                montoAcumuladoHoy = 0m;
            }
        }

        // ✅ NUEVO: Método para validar si se puede generar factura electrónica
        private bool ValidarLimiteFacturacion(out string mensajeError)
        {
            mensajeError = "";

            // Si la restricción está deshabilitada, permitir siempre
            if (!limitarFacturacion || montoLimiteFacturacion <= 0)
            {
                System.Diagnostics.Debug.WriteLine("[LÍMITE FACTURACIÓN] ✅ Restricción deshabilitada - Permitir");
                return true;
            }

            decimal totalConFacturaActual = montoAcumuladoHoy + importeTotalVenta;

            System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] 🔍 VALIDACIÓN:");
            System.Diagnostics.Debug.WriteLine($"  • Acumulado hoy: {montoAcumuladoHoy:C2}");
            System.Diagnostics.Debug.WriteLine($"  • Factura actual: {importeTotalVenta:C2}");
            System.Diagnostics.Debug.WriteLine($"  • Total proyectado: {totalConFacturaActual:C2}");
            System.Diagnostics.Debug.WriteLine($"  • Límite diario: {montoLimiteFacturacion:C2}");

            // ✅ VALIDACIÓN 1: Ya se alcanzó/superó el límite (sin contar la factura actual)
            if (montoAcumuladoHoy >= montoLimiteFacturacion)
            {
                decimal excedente = montoAcumuladoHoy - montoLimiteFacturacion;
                mensajeError = $"⛔ LÍMITE DIARIO DE FACTURACIÓN ALCANZADO\n\n" +
                              $"• Límite diario: {montoLimiteFacturacion:C2}\n" +
                              $"• Facturado hoy: {montoAcumuladoHoy:C2}\n" +
                              $"• Excedente: {excedente:C2}\n\n" +
                              $"⚠️ NO SE PUEDEN GENERAR MÁS FACTURAS ELECTRÓNICAS HOY\n\n" +
                              $"Solo puede generar REMITO o cerrar sin impresión.";

                System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] ❌ BLOQUEADO - Límite ya alcanzado");
                return false;
            }

            // ✅ VALIDACIÓN 2: Con esta factura se superaría el límite
            if (totalConFacturaActual > montoLimiteFacturacion)
            {
                decimal excedente = totalConFacturaActual - montoLimiteFacturacion;
                mensajeError = $"⚠️ ADVERTENCIA: SUPERARÍA EL LÍMITE DIARIO\n\n" +
                              $"• Límite diario: {montoLimiteFacturacion:C2}\n" +
                              $"• Facturado hoy: {montoAcumuladoHoy:C2}\n" +
                              $"• Factura actual: {importeTotalVenta:C2}\n" +
                              $"• Total proyectado: {totalConFacturaActual:C2}\n\n" +
                              $"🔴 Excedente proyectado: {excedente:C2}\n\n" +
                              $"¿Desea generar la factura de todos modos?";

                System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] ⚠️ ADVERTENCIA - Se superaría el límite por {excedente:C2}");
                // Retornar false para mostrar advertencia (se maneja en ProcesarFacturaElectronica)
                return false;
            }

            // ✅ TODO OK: Dentro del límite
            decimal disponibleDespues = montoLimiteFacturacion - totalConFacturaActual;
            System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] ✅ PERMITIDO - Disponible después: {disponibleDespues:C2}");
            return true;
        }

        // ✅ NUEVO: Método para cargar la configuración de vista previa
        private void CargarConfiguracionVistaPrevia()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string jsonPath = System.IO.Path.Combine(basePath, "appsettings.json");

                System.Diagnostics.Debug.WriteLine($"[CONFIG] ====================================");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Base Path: {basePath}");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] JSON Path: {jsonPath}");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] ¿Archivo existe? {System.IO.File.Exists(jsonPath)}");

                if (!System.IO.File.Exists(jsonPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[CONFIG] ⚠️ ADVERTENCIA: appsettings.json NO EXISTE en la ruta esperada");
                    usarVistaPrevia = true; // Valor por defecto
                    return;
                }

                // Leer el contenido del archivo para debug
                string jsonContent = System.IO.File.ReadAllText(jsonPath);
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Contenido JSON completo:");
                System.Diagnostics.Debug.WriteLine(jsonContent);

                var config = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                // Intentar leer la configuración de múltiples formas
                var seccionRestricciones = config.GetSection("RestriccionesImpresion");

                System.Diagnostics.Debug.WriteLine($"[CONFIG] ¿Sección RestriccionesImpresion existe? {seccionRestricciones.Exists()}");

                if (seccionRestricciones.Exists())
                {
                    // Leer cada valor individualmente para debug
                    string valorUsarVistaPrevia = seccionRestricciones["UsarVistaPrevia"];
                    System.Diagnostics.Debug.WriteLine($"[CONFIG] Valor RAW UsarVistaPrevia: '{valorUsarVistaPrevia}'");

                    // Intentar parsear de dos formas diferentes
                    bool valorParseado1 = config.GetValue<bool>("RestriccionesImpresion:UsarVistaPrevia", true);
                    System.Diagnostics.Debug.WriteLine($"[CONFIG] Valor PARSEADO (GetValue<bool>): {valorParseado1}");

                    // Segunda forma: parsear manualmente
                    bool valorParseado2 = true;
                    if (!string.IsNullOrEmpty(valorUsarVistaPrevia))
                    {
                        if (bool.TryParse(valorUsarVistaPrevia, out bool resultado))
                        {
                            valorParseado2 = resultado;
                            System.Diagnostics.Debug.WriteLine($"[CONFIG] Valor PARSEADO (TryParse): {valorParseado2}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[CONFIG] ⚠️ No se pudo parsear el valor: '{valorUsarVistaPrevia}'");
                        }
                    }

                    // Usar el valor parseado
                    usarVistaPrevia = valorParseado1;
                    System.Diagnostics.Debug.WriteLine($"[CONFIG] ✅ Valor FINAL asignado a usarVistaPrevia: {usarVistaPrevia}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CONFIG] ⚠️ Sección RestriccionesImpresion NO ENCONTRADA");
                    usarVistaPrevia = true;
                }

                System.Diagnostics.Debug.WriteLine($"[CONFIG] ====================================");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] RESULTADO FINAL:");
                System.Diagnostics.Debug.WriteLine($"[CONFIG]   usarVistaPrevia = {usarVistaPrevia}");
                System.Diagnostics.Debug.WriteLine($"[CONFIG]   Modo: {(usarVistaPrevia ? "VISTA PREVIA 👁️" : "IMPRESIÓN DIRECTA 🖨️")}");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] ====================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CONFIG] ❌ ERROR CRÍTICO: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[CONFIG] Inner Exception: {ex.InnerException.Message}");
                }

                usarVistaPrevia = true; // Por defecto en caso de error
            }
        }

        private void CrearControles()
        {
            var fontRadio = new Font("Segoe UI", 12F, FontStyle.Regular);

            // CheckBox para habilitar pago múltiple - CORREGIDO: Sin emoji problemático
            chkPagoMultiple = new CheckBox
            {
                Text = "Habilitar múltiples medios de pago",
                Left = 40,
                Top = 20,
                Width = 300,
                Height = 25,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(0, 120, 215),
                Checked = false
            };

            // CORREGIDO: Asignar referencia correcta
            chkMultiplesPagos = chkPagoMultiple;

            // Panel para pago simple (modo original)
            panelPagoSimple = new Panel
            {
                Left = 40,
                Top = 50,
                Width = 600,
                Height = 60,
                BackColor = System.Drawing.Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = true
            };

            var lblPago = new Label
            {
                Text = "Forma de pago:",
                Left = 10,
                Top = 10,
                Width = 120,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            rbEfectivo = new RadioButton
            {
                Text = "Efectivo",
                Left = 10,
                Top = 30,
                Width = 100,
                Height = 25,
                Font = fontRadio,
                Checked = true
            };

            rbDNI = new RadioButton
            {
                Text = "DNI",
                Left = 120,
                Top = 30,
                Width = 100,
                Height = 25,
                Font = fontRadio
            };

            rbMercadoPago = new RadioButton
            {
                Text = "MercadoPago",
                Left = 230,
                Top = 30,
                Width = 140,
                Height = 25,
                Font = fontRadio
            };

            panelPagoSimple.Controls.AddRange(new Control[] { lblPago, rbEfectivo, rbDNI, rbMercadoPago });

            // AJUSTADO: Label para mostrar el importe total a pagar con fuente grande
            lblImporteTotal = new Label
            {
                Left = 40,
                Top = 120, // CAMBIADO de 130 a 120 para dar más espacio
                Width = 600,
                Height = 80,
                Font = new Font("Segoe UI", 24F, FontStyle.Bold), // Fuente grande y negrita
                ForeColor = System.Drawing.Color.FromArgb(0, 102, 204), // Color azul
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Text = $"TOTAL A PAGAR: {importeTotalVenta:C2}",
                BackColor = System.Drawing.Color.FromArgb(240, 248, 255), // Fondo azul claro
                BorderStyle = BorderStyle.FixedSingle,
                Visible = true
            };

            // Panel para pago múltiple
            panelPagoMultiple = new Panel
            {
                Left = 40,
                Top = 50,
                Width = 600,
                Height = 280,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            // Control de múltiples pagos
            multiplePagosControl = new MultiplePagosControl
            {
                Dock = DockStyle.Fill
            };

            panelPagoMultiple.Controls.Add(multiplePagosControl);

            // AJUSTADO: Controles para CUIT y Razón Social con nueva posición inicial
            int topCuit = 220; // CAMBIADO de 340 a 220 para modo simple inicialmente

            txtCuit = new TextBox
            {
                Left = 90,
                Top = topCuit,
                Width = 120,
                Font = new Font("Segoe UI", 10F)
            };

            lblRazonSocial = new Label
            {
                Text = "",
                Left = 220,
                Top = topCuit + 2,
                Width = 400,
                Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                ForeColor = System.Drawing.Color.DarkGreen
            };

            var lblCuit = new Label
            {
                Name = "lblCuit", // NUEVO: Asignar nombre para poder encontrarlo
                Text = "CUIT:",
                Left = 40,
                Top = topCuit + 2,
                Width = 50,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            // AJUSTADO: Label para mensaje informativo
            lblMensajeInformativo = new Label
            {
                Left = 40,
                Top = 245, // AJUSTADO para la nueva altura
                Width = 600,
                Height = 25,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = System.Drawing.Color.Blue,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Text = ""
            };

            // AJUSTADO: Botones de impresión
            int topBotones = 270; // AJUSTADO para la nueva altura

            btnRemito = new Button
            {
                Text = "Remito",
                Width = 130,
                //Left = 60, <-- ahora se posicionan con PosicionarBotones()
                Top = topBotones,
                Height = 45,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(102, 51, 153),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnFacturaB = new Button
            {
                Text = "Factura B",
                Width = 130,
                //Left = 200,
                Top = topBotones,
                Height = 45,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(0, 123, 255),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnFacturaA = new Button
            {
                Text = "Factura A",
                Width = 130,
                //Left = 340,
                Top = topBotones,
                Height = 45,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(40, 167, 69),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            // NUEVO: Botón para finalizar sin imprimir
            btnFinalizarSinImpresion = new Button
            {
                Text = "Finalizar (Sin impresión)",
                Width = 120,
                //Left = 480,
                Top = topBotones,
                Height = 45,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(255, 193, 7),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Width = 90,
                //Left = 610, // REPOSICIONADO para dar espacio al nuevo botón (ahora se posiciona centralmente más a la izquierda)
                Top = topBotones, // AJUSTADO
                Height = 45,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(220, 53, 69),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };

            // Agregar todos los controles al formulario
            this.Controls.AddRange(new Control[] {
                chkPagoMultiple,
                panelPagoSimple,
                lblImporteTotal, // NUEVO: Agregar el label del importe total
                panelPagoMultiple,
                lblCuit, // IMPORTANTE: Asegurarse de que se agregue al formulario
                txtCuit,
                lblRazonSocial,
                lblMensajeInformativo,
                btnRemito,
                btnFacturaB,
                btnFacturaA,
                btnFinalizarSinImpresion,
                btnCancelar
            });

            // Posicionar botones de forma consistente y más a la izquierda
            PosicionarBotones();
        }

        // Método que posiciona secuencialmente los botones pero CENTRADOS en el ancho del formulario.
        private void PosicionarBotones()
        {
            try
            {
                // Lista de botones en el orden visual. Solo considerar los visibles (por si los ocultas algún día).
                var botones = new List<Button> { btnRemito, btnFacturaB, btnFacturaA, btnFinalizarSinImpresion, btnCancelar }
                    .Where(b => b != null && b.Visible)
                    .ToList();

                if (!botones.Any())
                    return;

                int spacing = 15; // espacio entre botones
                int totalButtonsWidth = botones.Sum(b => b.Width);
                int totalSpacing = spacing * Math.Max(0, botones.Count - 1);
                int totalWidth = totalButtonsWidth + totalSpacing;

                // Calcular inicio centrado
                int startLeft = (this.ClientSize.Width - totalWidth) / 2;

                // Mantener un margen mínimo (evitar que queden pegados al borde)
                int minMargin = 10;
                if (startLeft < minMargin)
                    startLeft = minMargin;

                int left = startLeft;
                foreach (var b in botones)
                {
                    b.Left = left;
                    left += b.Width + spacing;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en PosicionarBotones: {ex.Message}");
            }
        }

        private void ConfigurarEventos()
        {
            // NUEVO: Configurar KeyPreview y manejo de tecla Escape
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
            };

            // Evento para cambiar modo de pago
            chkPagoMultiple.CheckedChanged += (s, e) =>
            {
                bool esPagoMultiple = chkPagoMultiple.Checked;

                panelPagoSimple.Visible = !esPagoMultiple;
                panelPagoMultiple.Visible = esPagoMultiple;

                // NUEVO: Controlar visibilidad del label de importe total
                lblImporteTotal.Visible = !esPagoMultiple; // Solo visible en modo pago simple

                if (esPagoMultiple)
                {
                    multiplePagosControl.EstablecerImporteTotal(importeTotalVenta);
                    this.Height = 550;

                    // CORREGIDO: Ajustar posiciones para que CUIT sea visible en modo múltiple
                    txtCuit.Top = 340;
                    lblRazonSocial.Top = 342;
                    lblMensajeInformativo.Top = 365;

                    // CORREGIDO: Asegurar que el label CUIT también se posicione correctamente
                    var lblCuit = this.Controls.Find("lblCuit", true).FirstOrDefault();
                    if (lblCuit != null)
                    {
                        lblCuit.SetBounds(40, 342, 50, 20);
                        lblCuit.Visible = true; // NUEVO: Asegurar que sea visible
                    }

                    // AJUSTADO: Posicionar botones más abajo para dar espacio al CUIT
                    btnRemito.Top = 390;
                    btnFacturaB.Top = 390;
                    btnFacturaA.Top = 390;
                    btnFinalizarSinImpresion.Top = 390;
                    btnCancelar.Top = 390;

                    // Recalcular lefts para mantener cluster hacia la izquierda
                    PosicionarBotones();
                }
                else
                {
                    // CORREGIDO: Aumentar altura para que se vea el CUIT y todos los elementos
                    this.Height = 400;

                    // AJUSTADO: Reposicionar elementos para el nuevo tamaño
                    txtCuit.Top = 220;
                    lblRazonSocial.Top = 222;
                    lblMensajeInformativo.Top = 245;

                    // CORREGIDO: Posicionar también el label CUIT correctamente
                    var lblCuit = this.Controls.Find("lblCuit", true).FirstOrDefault();
                    if (lblCuit != null)
                    {
                        lblCuit.SetBounds(40, 222, 50, 20);
                        lblCuit.Visible = true; // NUEVO: Asegurar que sea visible
                    }

                    btnRemito.Top = 270;
                    btnFacturaB.Top = 270;
                    btnFacturaA.Top = 270;
                    btnFinalizarSinImpresion.Top = 270;
                    btnCancelar.Top = 270;

                    // Recalcular lefts para modo simple
                    PosicionarBotones();
                }

                ActualizarOpcionesImpresion();
            };

            // Eventos de RadioButtons originales
            rbEfectivo.CheckedChanged += (s, e) =>
            {
                if (rbEfectivo.Checked)
                {
                    OpcionPagoSeleccionada = OpcionPago.Efectivo;
                    ActualizarOpcionesImpresion();
                }
            };

            rbDNI.CheckedChanged += (s, e) =>
            {
                if (rbDNI.Checked)
                {
                    OpcionPagoSeleccionada = OpcionPago.DNI;
                    ActualizarOpcionesImpresion();
                }
            };

            rbMercadoPago.CheckedChanged += (s, e) =>
            {
                if (rbMercadoPago.Checked)
                {
                    OpcionPagoSeleccionada = OpcionPago.MercadoPago;
                    ActualizarOpcionesImpresion();
                }
            };

            // Evento para cambios en pagos múltiples
            if (multiplePagosControl != null)
            {
                multiplePagosControl.OnPagosChanged += (s, e) =>
                {
                    ActualizarOpcionesImpresion();
                };
            }

            // Eventos CUIT mejorados
            txtCuit.TextChanged += TxtCuit_TextChanged;
            txtCuit.Leave += async (s, e) => await ConsultarCuitAsync();
            txtCuit.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await ConsultarCuitAsync();
                }
            };

            // Eventos de botones de impresión
            btnRemito.Click += async (s, e) =>
            {
                try
                {
                    await ProcesarRemito();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error en Remito: {ex.Message}", "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnFacturaB.Click += async (s, e) =>
            {
                try
                {
                    await ProcesarFacturaElectronica(OpcionImpresion.FacturaB);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error en Factura B: {ex.Message}", "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnFacturaA.Click += async (s, e) =>
            {
                try
                {
                    // MEJORADO: Validación completa de CUIT para Factura A
                    string cuitFormateado = txtCuit.Text.Trim();
                    string cuitLimpio = cuitFormateado.Replace("-", "");

                    if (string.IsNullOrEmpty(cuitLimpio))
                    {
                        MessageBox.Show("Para Factura A debe ingresar un CUIT.", "CUIT Requerido",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtCuit.Focus();
                        return;
                    }

                    if (cuitLimpio.Length != 11)
                    {
                        MessageBox.Show("El CUIT debe tener exactamente 11 dígitos.", "CUIT Inválido",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtCuit.Focus();
                        return;
                    }

                    // NUEVO: Validar código verificador según la ley
                    if (!ValidarCuitVerificador(cuitLimpio))
                    {
                        MessageBox.Show(
                            "El CUIT ingresado no es válido.\n\n" +
                            "El código verificador no cumple con el algoritmo oficial.\n" +
                            "Por favor verifique los dígitos ingresados.",
                            "CUIT Inválido",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtCuit.Focus();
                        return;
                    }

                    // RESTAURADO: Procesar Factura A con AFIP real
                    await ProcesarFacturaElectronica(OpcionImpresion.FacturaA);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error en Factura A: {ex.Message}", "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // NUEVO: Evento para finalizar sin imprimir
            btnFinalizarSinImpresion.Click += async (s, e) =>
            {
                try
                {
                    await ProcesarFinalizarSinImpresion();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al finalizar sin impresión: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            this.Shown += (s, e) =>
            {
                // Reposicionar botones al mostrarse (asegura centrado en start)
                PosicionarBotones();

                // Aplicar visibilidad de Factura A/B por configuración (por si cambió)
                AplicarConfiguracionFacturacion();

                if (btnRemito.Enabled)
                    btnRemito.Focus();
                else if (btnFacturaB.Enabled)
                    btnFacturaB.Focus();
                else if (btnFacturaA.Enabled)
                    btnFacturaA.Focus();
                else if (btnFinalizarSinImpresion.Enabled)
                    btnFinalizarSinImpresion.Focus();
            };
        }

        /// <summary>
        /// Evento para formatear automáticamente el CUIT mientras se escribe - CORREGIDO: Manejo mejorado del cursor
        /// </summary>
        private void TxtCuit_TextChanged(object sender, EventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // NUEVO: Flag para evitar recursión
            if (textBox.Tag?.ToString() == "FORMATTING")
                return;

            // Guardar posición del cursor y texto actual
            int cursorPosition = textBox.SelectionStart;
            string textoOriginal = textBox.Text;

            // Limpiar solo números
            string soloNumeros = new string(textoOriginal.Where(char.IsDigit).ToArray());

            // Limitar a 11 dígitos
            if (soloNumeros.Length > 11)
            {
                soloNumeros = soloNumeros.Substring(0, 11);
            }

            string textoFormateado = "";

            // Formatear según la longitud
            if (soloNumeros.Length <= 2)
            {
                textoFormateado = soloNumeros;
            }
            else if (soloNumeros.Length <= 10)
            {
                textoFormateado = $"{soloNumeros.Substring(0, 2)}-{soloNumeros.Substring(2)}";
            }
            else
            {
                textoFormateado = $"{soloNumeros.Substring(0, 2)}-{soloNumeros.Substring(2, 8)}-{soloNumeros.Substring(10)}";
            }

            // Actualizar texto solo si cambió
            if (textoFormateado != textoOriginal)
            {
                // NUEVO: Marcar como formateando para evitar recursión
                textBox.Tag = "FORMATTING";

                // MEJORADO: Calcular nueva posición del cursor basado en dígitos
                int digitosAntesDelCursor = 0;
                for (int i = 0; i < Math.Min(cursorPosition, textoOriginal.Length); i++)
                {
                    if (char.IsDigit(textoOriginal[i]))
                        digitosAntesDelCursor++;
                }

                // Encontrar la nueva posición basada en la cantidad de dígitos
                int nuevaPosicion = 0;
                int digitosContados = 0;

                for (int i = 0; i < textoFormateado.Length && digitosContados < digitosAntesDelCursor; i++)
                {
                    if (char.IsDigit(textoFormateado[i]))
                    {
                        digitosContados++;
                    }
                    nuevaPosicion = i + 1;
                }

                // CORREGIDO: Si estamos al final de un grupo de dígitos y hay un guión después,positionar después del guión
                if (nuevaPosicion < textoFormateado.Length && textoFormateado[nuevaPosicion] == '-')
                {
                    nuevaPosicion++;
                }

                // Asegurar que la posición esté dentro de los límites
                nuevaPosicion = Math.Max(0, Math.Min(nuevaPosicion, textoFormateado.Length));

                // Actualizar el texto y posición
                textBox.Text = textoFormateado;
                textBox.SelectionStart = nuevaPosicion;
                textBox.SelectionLength = 0;

                // NUEVO: Quitar flag de formateo
                textBox.Tag = null;
            }

            // Limpiar label si se está editando
            if (!string.IsNullOrEmpty(textoOriginal) && lblRazonSocial.Text.Contains("✓"))
            {
                lblRazonSocial.Text = "";
            }
        }

        // RESTAURADO: Método centralizado para procesar facturas electrónicas con AFIP REAL
        private async Task ProcesarFacturaElectronica(OpcionImpresion tipoFactura)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 === INICIANDO PROCESAMIENTO {tipoFactura} CON AFIP REAL ===");

                // ✅ NUEVO: VALIDAR LÍMITE DE FACTURACIÓN ANTES DE CONTINUAR
                if (!ValidarLimiteFacturacion(out string mensajeError))
                {
                    // Verificar si es bloqueo total o advertencia
                    if (mensajeError.Contains("⛔"))
                    {
                        // ❌ BLOQUEO TOTAL: Ya se alcanzó el límite
                        MessageBox.Show(mensajeError,
                            "Límite de Facturación Alcanzado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Stop);
                        return;
                    }
                    else
                    {
                        // ⚠️ ADVERTENCIA: Se superaría el límite - Preguntar al usuario
                        var resultado = MessageBox.Show(mensajeError,
                            "Advertencia - Límite de Facturación",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (resultado != DialogResult.Yes)
                        {
                            System.Diagnostics.Debug.WriteLine("[LÍMITE FACTURACIÓN] ❌ Usuario canceló por exceso de límite");
                            return;
                        }

                        System.Diagnostics.Debug.WriteLine("[LÍMITE FACTURACIÓN] ⚠️ Usuario autorizó exceder el límite");
                    }
                }

                if (EsPagoMultiple && !multiplePagosControl.PagoCompleto)
                {
                    MessageBox.Show(
                        "ERROR: El pago no está completo.\n\n" +
                        $"Total factura: {importeTotalVenta:C2}\n" +
                        $"Importe asignado: {multiplePagosControl.ImporteAsignado:C2}\n" +
                        $"Importe pendiente: {multiplePagosControl.ImportePendiente:C2}\n\n" +
                        "Complete el pago antes de continuar.",
                        "Pago incompleto",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // ... (resto del método sin cambios)
                btnRemito.Enabled = false;
                btnFacturaA.Enabled = false;
                btnFacturaB.Enabled = false;
                btnFinalizarSinImpresion.Enabled = false;

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string cuitEmisor = config["AFIP:CUIT"];
                if (string.IsNullOrEmpty(cuitEmisor))
                {
                    MessageBox.Show("Error: CUIT del emisor no configurado en appsettings.json", "Error de Configuración",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var progressForm = new Form
                {
                    Text = "Procesando factura electrónica con AFIP...",
                    Width = 400,
                    Height = 150,
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    ShowInTaskbar = false
                };

                var lblProgress = new Label
                {
                    Text = "Conectando con AFIP...",
                    Left = 20,
                    Top = 30,
                    Width = 360,
                    Height = 30,
                    Font = new Font("Segoe UI", 10F),
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter
                };

                var progressBar = new ProgressBar
                {
                    Left = 20,
                    Top = 60,
                    Width = 360,
                    Height = 25,
                    Style = ProgressBarStyle.Marquee,
                    MarqueeAnimationSpeed = 50
                };

                progressForm.Controls.AddRange(new Control[] { lblProgress, progressBar });
                progressForm.Show(this);
                Application.DoEvents();

                try
                {
                    lblProgress.Text = "Autenticando con AFIP...";
                    Application.DoEvents();

                    await AutenticarConAfipReal(cuitEmisor);

                    lblProgress.Text = "Obteniendo número de comprobante...";
                    Application.DoEvents();

                    int tipoComprobante = tipoFactura == OpcionImpresion.FacturaA ? 1 : 6;
                    int puntoVenta = 1;
                    int ultimoNumero = await ObtenerUltimoNumeroComprobanteReal(tipoComprobante, puntoVenta);
                    int numeroFactura = ultimoNumero + 1;

                    System.Diagnostics.Debug.WriteLine($"📋 Tipo: {tipoComprobante}, PV: {puntoVenta}, Último: {ultimoNumero}, Nuevo: {numeroFactura}");

                    lblProgress.Text = "Solicitando CAE a AFIP...";
                    Application.DoEvents();

                    string cuitCliente = tipoFactura == OpcionImpresion.FacturaA ? txtCuit.Text.Trim() : "";
                    var resultadoCAE = await SolicitarCAEReal(tipoComprobante, puntoVenta, numeroFactura, cuitCliente);

                    if (!resultadoCAE.exito)
                    {
                        progressForm.Close();
                        MessageBox.Show($"Error obteniendo CAE de AFIP:\n\n{resultadoCAE.error}",
                            "Error AFIP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    lblProgress.Text = "Finalizando proceso...";
                    Application.DoEvents();

                    CAENumero = resultadoCAE.cae;
                    CAEVencimiento = resultadoCAE.vencimiento;
                    NumeroFacturaAfip = numeroFactura;
                    OpcionSeleccionada = tipoFactura;

                    string numeroFormateado = FormatearNumeroFactura(tipoComprobante, puntoVenta, numeroFactura);

                    string formaPago = EsPagoMultiple ? "Múltiple" : OpcionPagoSeleccionada.ToString();
                    string tipoFacturaString = tipoFactura == OpcionImpresion.FacturaA ? "FacturaA" : "FacturaB";

                    System.Diagnostics.Debug.WriteLine($"🔄 Ejecutando callback OnProcesarVenta para {tipoFacturaString}...");

                    if (OnProcesarVenta != null)
                    {
                        await OnProcesarVenta(tipoFacturaString, formaPago, cuitCliente,
                            CAENumero, CAEVencimiento, NumeroFacturaAfip, numeroFormateado);
                        System.Diagnostics.Debug.WriteLine("✅ Callback OnProcesarVenta completado exitosamente");
                    }

                    progressForm.Close();

                    System.Diagnostics.Debug.WriteLine($"✅ {tipoFactura} completada exitosamente con AFIP REAL");
                    System.Diagnostics.Debug.WriteLine($"CAE: {CAENumero}, Vencimiento: {CAEVencimiento:dd/MM/yyyy}");

                    if (usarVistaPrevia)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FACTURA] 🖨️ Imprimiendo {tipoFactura} con vista previa");
                        await formularioPadre.ImprimirConServicioAsync(this);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[FACTURA] 🖨️ Imprimiendo {tipoFactura} directamente a la impresora");
                        await ImprimirDirectoSinPreview(tipoFactura);
                    }

                    System.Diagnostics.Debug.WriteLine("✅ Impresión completada - Cerrando modal");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    progressForm.Close();
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en ProcesarFacturaElectronica: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");

                btnRemito.Enabled = true;
                btnFacturaA.Enabled = true;
                btnFacturaB.Enabled = true;
                btnFinalizarSinImpresion.Enabled = true;
                ActualizarOpcionesImpresion();

                MessageBox.Show($"Error procesando factura electrónica:\n\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ✅ NUEVO: Método para impresión directa sin preview
        private async Task ImprimirDirectoSinPreview(OpcionImpresion tipoComprobante)
        {
            try
            {
                if (formularioPadre == null)
                {
                    System.Diagnostics.Debug.WriteLine("[IMPRESIÓN] Error: formularioPadre es null");
                    return;
                }

                var remitoActual = formularioPadre.GetRemitoActual();
                if (remitoActual == null || remitoActual.Rows.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[IMPRESIÓN] Error: No hay datos para imprimir");
                    MessageBox.Show("No hay productos para imprimir.", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var config = new Servicios.TicketConfig
                {
                    NombreComercio = formularioPadre.GetNombreComercio(),
                    DomicilioComercio = "",
                    FormaPago = OpcionPagoSeleccionada.ToString(),
                    MensajePie = "Gracias por su compra!"
                };

                switch (tipoComprobante)
                {
                    case OpcionImpresion.RemitoTicket:
                        config.TipoComprobante = "REMITO";
                        config.NumeroComprobante = $"Remito N° {formularioPadre.GetNroRemitoActual()}";
                        break;

                    case OpcionImpresion.FacturaB:
                        config.TipoComprobante = "FacturaB";
                        config.NumeroComprobante = FormatearNumeroFactura(6, 1, NumeroFacturaAfip);
                        config.CAE = CAENumero;
                        config.CAEVencimiento = CAEVencimiento;
                        break;

                    case OpcionImpresion.FacturaA:
                        config.TipoComprobante = "FacturaA";
                        config.NumeroComprobante = FormatearNumeroFactura(1, 1, NumeroFacturaAfip);
                        config.CAE = CAENumero;
                        config.CAEVencimiento = CAEVencimiento;
                        break;
                }

                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] 🖨️ Imprimiendo directamente: {config.TipoComprobante}");

                using (var ticketService = new Servicios.TicketPrintingService())
                {
                    await ticketService.ImprimirTicketDirecto(remitoActual, config);
                }

                System.Diagnostics.Debug.WriteLine("[IMPRESIÓN] ✅ Impresión directa completada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ❌ Error en impresión directa: {ex.Message}");
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task AutenticarConAfipReal(string cuitEmisor)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔑 === AUTENTICACIÓN AFIP TRANSPARENTE ===");

                var (tieneTokenValido, mensaje, minutosRestantes) = AfipAuthenticator.VerificarTokensExistentes("wsfe");

                if (tieneTokenValido && minutosRestantes > 2)
                {
                    var tokenExistente = AfipAuthenticator.GetExistingToken("wsfe");
                    if (tokenExistente.HasValue)
                    {
                        TokenAfip = tokenExistente.Value.token;
                        SignAfip = tokenExistente.Value.sign;
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Usando token válido existente: {mensaje}");
                        return;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[AFIP] 🔍 Estado tokens: {mensaje}");

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string certificadoPath = config["AFIP:CertificadoPath"];
                string certificadoPassword = config["AFIP:CertificadoPassword"];
                string wsaaUrl = config["AFIP:WSAAUrl"] ?? "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";

                if (string.IsNullOrEmpty(certificadoPath))
                {
                    certificadoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certificado.p12");
                }

                var (esCertificadoValido, mensajeCert, fechaVencimiento) = AfipAuthenticator.VerificarCertificado(certificadoPath, certificadoPassword ?? "");
                if (!esCertificadoValido)
                {
                    throw new Exception($"Certificado AFIP no válido: {mensajeCert}");
                }

                System.Diagnostics.Debug.WriteLine($"✅ Certificado válido: {mensajeCert}");

                try
                {
                    var (token, sign, expiration) = await AfipAuthenticator.GetTAAsync("wsfe", certificadoPath, certificadoPassword ?? "", wsaaUrl);

                    TokenAfip = token;
                    SignAfip = sign;

                    System.Diagnostics.Debug.WriteLine("✅ Autenticación AFIP transparente completada");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Token válido hasta: {expiration:dd/MM/yyyy HH:mm:ss}");
                }
                catch (Exception ex) when (ex.Message.Contains("TOKEN") || ex.Message.Contains("token") || ex.Message.Contains("Ya existe"))
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] 🔄 Forzando uso de token existente debido a: {ex.Message}");

                    try
                    {
                        var (token, sign, expiration) = await AfipAuthenticator.ForzarUsoTokenExistente("wsfe", certificadoPath, certificadoPassword ?? "");

                        TokenAfip = token;
                        SignAfip = sign;

                        System.Diagnostics.Debug.WriteLine("✅ Token forzado exitosamente - proceso transparente");
                        return;
                    }
                    catch (Exception exForzar)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ Error forzando token: {exForzar.Message}");
                        throw new Exception($"No se pudo obtener token AFIP de manera transparente: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine("✅ === AUTENTICACIÓN TRANSPARENTE COMPLETADA ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 Error en autenticación transparente: {ex.Message}");

                try
                {
                    var tokenUltimoRecurso = AfipAuthenticator.GetExistingToken("wsfe");
                    if (tokenUltimoRecurso.HasValue)
                    {
                        TokenAfip = tokenUltimoRecurso.Value.token;
                        SignAfip = tokenUltimoRecurso.Value.sign;
                        System.Diagnostics.Debug.WriteLine("🆘 Usando token de último recurso de manera transparente");
                        return;
                    }
                }
                catch (Exception exCache)
                {
                    System.Diagnostics.Debug.WriteLine($"💥 Error en último recurso: {exCache.Message}");
                }

                throw new Exception($"Error crítico de autenticación AFIP: {ex.Message}\n\nNo se pudo obtener tokens válidos para continuar.");
            }
        }

        private async Task<int> ObtenerUltimoNumeroComprobanteReal(int tipoComprobante, int puntoVenta)
        {
            try
            {
                using (var wsfeClient = new ArcaWS.ServiceSoapClient(ArcaWS.ServiceSoapClient.EndpointConfiguration.ServiceSoap))
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
                        string errores = string.Join(", ", resultado.Errors.Select(e => e.Msg));
                        System.Diagnostics.Debug.WriteLine($"[AFIP] Errores: {errores}");
                        throw new Exception($"Error AFIP: {errores}");
                    }

                    int ultimoNumero = resultado?.CbteNro ?? 0;
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Último número autorizado: {ultimoNumero}");

                    return ultimoNumero;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error obteniendo último número: {ex.Message}");
                return 0;
            }
        }

        private async Task<(bool exito, string cae, DateTime? vencimiento, string error)> SolicitarCAEReal(
            int tipoComprobante, int puntoVenta, int numero, string cuitCliente = "")
        {
            try
            {
                using (var wsfeClient = new ArcaWS.ServiceSoapClient(ArcaWS.ServiceSoapClient.EndpointConfiguration.ServiceSoap))
                {
                    var authRequest = new ArcaWS.FEAuthRequest
                    {
                        Token = TokenAfip,
                        Sign = SignAfip,
                        Cuit = long.Parse(ObtenerCuitEmisor().Replace("-", ""))
                    };

                    // CORREGIDO: Determinar correctamente DocTipo e IVAPerNro según el tipo de factura
                    int docTipo;
                    long docNro;
                    int ivaPerNro; // NUEVO: Condición IVA del receptor


                    if (tipoComprobante == 1) // Factura A
                    {
                        docTipo = 80; // CUIT
                        docNro = !string.IsNullOrEmpty(cuitCliente) ? long.Parse(cuitCliente.Replace("-", "")) : 0;
                        ivaPerNro = DeterminarCondicionIvaReceptor(tipoComprobante, cuitCliente);
                    }
                    else // Factura B
                    {
                        docTipo = 99; // Sin identificación / Consumidor Final
                        docNro = 0;
                        ivaPerNro = DeterminarCondicionIvaReceptor(tipoComprobante, cuitCliente);
                    }

                    // MEJORADO: Calcular valores con mayor precisión y control de límites
                    decimal importeNetoCalculado = Math.Round(CalcularImporteNeto(), 2);
                    decimal importeIvaCalculado = Math.Round(CalcularImporteIVA(), 2);
                    decimal importeTotalCalculado = Math.Round(importeTotalVenta, 2);

                    // NUEVO: Asegurar consistencia en los totales
                    if (Math.Abs((importeNetoCalculado + importeIvaCalculado) - importeTotalCalculado) > 0.02m)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Ajustando diferencias de redondeo en totales");
                        // Ajustar el IVA para que cuadren los totales
                        importeIvaCalculado = importeTotalCalculado - importeNetoCalculado;
                        importeIvaCalculado = Math.Round(importeIvaCalculado, 2);
                    }

                    // Preparar datos del comprobante
                    var comprobante = new ArcaWS.FECAEDetRequest
                    {
                        Concepto = 1, // Productos
                        DocTipo = docTipo,
                        DocNro = docNro,
                        CondicionIVAReceptorId = ivaPerNro, // CORREGIDO: Usar el nombre correcto de la propiedad
                        CbteDesde = numero,
                        CbteHasta = numero,
                        CbteFch = DateTime.Now.ToString("yyyyMMdd"),
                        ImpTotal = (double)importeTotalCalculado,
                        ImpTotConc = 0,
                        ImpNeto = (double)importeNetoCalculado,
                        ImpOpEx = 0,
                        ImpIVA = (double)importeIvaCalculado,
                        ImpTrib = 0,
                        MonId = "PES",
                        MonCotiz = 1
                    };

                    // MEJORADO: Preparar array de IVA con validaciones estrictas
                    var ivaArray = new List<ArcaWS.AlicIva>();
                    var datosIva = CalcularDetalleIVA();

                    // NUEVO: Validar datos antes de enviar a AFIP
                    var (esValido, errorValidacion) = ValidarDatosParaAfip(datosIva, importeTotalCalculado);
                    if (!esValido)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ Validación falló: {errorValidacion}");
                        return (false, "", null, $"Error de validación: {errorValidacion}");
                    }

                    foreach (var iva in datosIva)
                    {
                        if (iva.Value.baseImponible > 0)
                        {
                            // NUEVO: Validar y formatear BaseImp antes de enviar a AFIP
                            decimal baseImponible = Math.Round(iva.Value.baseImponible, 2);
                            decimal importeIva = Math.Round(iva.Value.importeIva, 2);

                            // CRÍTICO: Asegurar que BaseImp cumple exactamente con el formato AFIP
                            if (baseImponible >= 10000000000000m) // 13 dígitos
                            {
                                System.Diagnostics.Debug.WriteLine($"🚨 BaseImp excede límite AFIP: {baseImponible:F2}");
                                baseImponible = 9999999999999.99m; // Máximo permitido por AFIP
                                importeIva = Math.Round(iva.Value.baseImponible + iva.Value.importeIva - baseImponible, 2);
                                System.Diagnostics.Debug.WriteLine($"🔧 BaseImp ajustada: {baseImponible:F2}, IVA ajustado: {importeIva:F2}");
                            }

                            // NUEVO: Validación formato exacto con regex-like check
                            string baseFormateada = baseImponible.ToString("F2", CultureInfo.InvariantCulture);
                            string[] partesBase = baseFormateada.Split('.');

                            if (partesBase[0].Length > 13 || partesBase[1].Length != 2)
                            {
                                System.Diagnostics.Debug.WriteLine($"❌ Formato inválido BaseImp: {baseFormateada}");
                                // Forzar formato correcto
                                baseImponible = Math.Min(baseImponible, 9999999999999.99m);
                                baseImponible = Math.Round(baseImponible, 2);
                                System.Diagnostics.Debug.WriteLine($"🔧 BaseImp corregida: {baseImponible.ToString("F2", CultureInfo.InvariantCulture)}");
                            }

                            // Mapear porcentaje de IVA a código AFIP
                            int codigoAfip = MapearPorcentajeIvaACodigoAfip(iva.Key);

                            // NUEVO: Validación final antes de agregar al array
                            double baseImpDouble = (double)baseImponible;
                            double importeIvaDouble = (double)importeIva;

                            // Verificar que los valores double mantengan la precisión
                            if (Math.Abs((decimal)baseImpDouble - baseImponible) > 0.01m)
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ Pérdida de precisión en BaseImp: {baseImponible:F2} -> {baseImpDouble:F2}");
                            }

                            ivaArray.Add(new ArcaWS.AlicIva
                            {
                                Id = codigoAfip,
                                BaseImp = baseImpDouble,
                                Importe = importeIvaDouble
                            });

                            System.Diagnostics.Debug.WriteLine($"📊 IVA {iva.Key}% -> Código AFIP: {codigoAfip}, BaseImp: {baseImponible:F2} ({baseImpDouble}), Importe: {importeIva:F2} ({importeIvaDouble})");
                        }
                    }

                    // Solicitar CAE
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

                    // Agregar IVA si hay
                    if (ivaArray.Any())
                    {
                        comprobante.Iva = ivaArray.ToArray();
                    }

                    System.Diagnostics.Debug.WriteLine($"[AFIP] 📤 Enviando solicitud CAE - Comprobante: {tipoComprobante}-{puntoVenta:D4}-{numero:D8}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] DocTipo: {docTipo}, DocNro: {docNro}, IVAPerNro: {ivaPerNro}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Importe Total: {comprobante.ImpTotal:F2}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Base Imponible: {comprobante.ImpNeto:F2}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] IVA: {comprobante.ImpIVA:F2}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Cantidad AlicIva: {ivaArray.Count}");

                    var response = await wsfeClient.FECAESolicitarAsync(authRequest, request);
                    var resultado = response.Body.FECAESolicitarResult;

                    if (resultado?.Errors != null && resultado.Errors.Length > 0)
                    {
                        string errores = string.Join(", ", resultado.Errors.Select(e => e.Msg));
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ Errores CAE: {errores}");
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
                                if (DateTime.TryParseExact(detalle.CAEFchVto, "yyyyMMdd",
                                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha))
                                {
                                    fechaVencimiento = fecha;
                                }
                            }

                            System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ CAE obtenido exitosamente: {detalle.CAE}");
                            System.Diagnostics.Debug.WriteLine($"[AFIP] Vencimiento CAE: {fechaVencimiento:dd/MM/yyyy}");

                            return (true, detalle.CAE, fechaVencimiento, "");
                        }
                        else
                        {
                            string errores = "";
                            if (detalle.Observaciones != null)
                            {
                                errores = string.Join(", ", detalle.Observaciones.Select(o => o.Msg));
                            }
                            System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ AFIP rechazó el comprobante: {errores}");
                            return (false, "", null, $"AFIP rechazó el comprobante: {errores}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ Respuesta inválida de AFIP");
                        return (false, "", null, "Respuesta inválida de AFIP");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] 💥 Error crítico solicitando CAE: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AFIP] Stack trace: {ex.StackTrace}");
                return (false, "", null, $"Error solicitando CAE: {ex.Message}");
            }
        }

        // NUEVO: Mapear porcentajes de IVA a códigos AFIP
        private int MapearPorcentajeIvaACodigoAfip(decimal porcentaje)
        {
            return porcentaje switch
            {
                0m => 3,      // 0% - No Gravado
                10.5m => 4,   // 10.5%
                21m => 5,     // 21%
                27m => 6,     // 27%
                _ => 5        // Por defecto 21%
            };
        }

        // NUEVO: Determinar condición IVA del receptor según AFIP
        private int DeterminarCondicionIvaReceptor(int tipoComprobante, string cuitCliente = "")
        {
            // Códigos de condición IVA según AFIP:
            // 1 - IVA Responsable Inscripto
            // 2 - IVA Responsable no Inscripto
            // 3 - IVA no Responsable
            // 4 - IVA Sujeto Exento
            // 5 - Consumidor Final
            // 6 - Responsable Monotributo
            // 7 - Sujeto no Categorizado
            // 8 - Proveedor del Exterior
            // 9 - Cliente del Exterior
            // 10 - IVA Liberado – Ley Nº 19.640
            // 11 - IVA Responsable Inscripto – Agente de Percepción
            // 12 - Pequeño Contribuyente

            if (tipoComprobante == 1) // Factura A
            {
                string cuitLimpio = cuitCliente?.Replace("-", "") ?? "";

                if (string.IsNullOrEmpty(cuitLimpio) || cuitLimpio.Length != 11)
                {
                    System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] CUIT inválido para Factura A: '{cuitCliente}' -> usando Responsable Inscripto (1)");
                    return 1; // NUNCA retornar 0 - usar Responsable Inscripto por defecto
                }

                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] Factura A con CUIT válido: '{cuitCliente}' -> Responsable Inscripto (1)");

                // Dejar como 1 (IVA Responsable Inscripto) si el CUIT es válido - AFIP no permite otra cosa
                return 1;
            }
            else // Factura B
            {
                // Para Factura B, asumir Consumidor Final (sin identificación)
                return 5;
            }
        }

        // NUEVO: Método para validar datos antes de enviar a AFIP
        private (bool esValido, string error) ValidarDatosParaAfip(Dictionary<decimal, (decimal baseImponible, decimal importeIva)> datosIva, decimal importeTotal)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 === VALIDACIÓN DATOS AFIP ===");

                // 1. Validar formato de importes totales
                if (importeTotal >= 10000000000000m)
                {
                    return (false, $"Importe total excede límite AFIP: {importeTotal:F2} (máximo: 9,999,999,999,999.99)");
                }

                if (importeTotal <= 0)
                {
                    return (false, $"Importe total debe ser mayor a cero: {importeTotal:F2}");
                }

                // 2. Validar cada alícuota de IVA
                decimal sumaBaseImponible = 0;
                decimal sumaIva = 0;

                foreach (var iva in datosIva)
                {
                    decimal baseImponible = Math.Round(iva.Value.baseImponible, 2);
                    decimal importeIva = Math.Round(iva.Value.importeIva, 2);

                    // Validar BaseImp - CRÍTICO para el error de AFIP
                    string baseStr = baseImponible.ToString("F2", CultureInfo.InvariantCulture);
                    string[] partesBase = baseStr.Split('.');

                    if (partesBase[0].Length > 13)
                    {
                        return (false, $"BaseImp para IVA {iva.Key}% excede 13 dígitos enteros: {baseStr} (valor: {baseImponible})");
                    }

                    if (partesBase[1].Length != 2)
                    {
                        return (false, $"BaseImp para IVA {iva.Key}% debe tener exactamente 2 decimales: {baseStr}");
                    }

                    // Validar que BaseImp sea positiva
                    if (baseImponible <= 0)
                    {
                        return (false, $"BaseImp para IVA {iva.Key}% debe ser mayor a cero: {baseImponible:F2}");
                    }

                    // Validar Importe IVA
                    if (importeIva < 0)
                    {
                        return (false, $"Importe IVA para alícuota {iva.Key}% no puede ser negativo: {importeIva:F2}");
                    }

                    // Validar coherencia entre porcentaje, base e importe
                    decimal ivaCalculadoEsperado = Math.Round(baseImponible * iva.Key / 100, 2);
                    decimal diferencia = Math.Abs(importeIva - ivaCalculadoEsperado);

                    if (diferencia > 0.05m) // Tolerancia de 5 centavos por redondeos
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Diferencia en cálculo IVA {iva.Key}%: Calculado={ivaCalculadoEsperado:F2}, Enviado={importeIva:F2}, Diferencia={diferencia:F2}");
                        // Solo advertencia, no error crítico
                    }

                    sumaBaseImponible += baseImponible;
                    sumaIva += importeIva;

                    System.Diagnostics.Debug.WriteLine($"✅ IVA {iva.Key}%: Base={baseImponible:F2}, IVA={importeIva:F2} - VÁLIDO");
                }

                // 3. Validar coherencia de totales
                decimal totalCalculado = Math.Round(sumaBaseImponible + sumaIva, 2);
                decimal diferenciaTotales = Math.Abs(totalCalculado - importeTotal);

                if (diferenciaTotales > 0.02m) // Tolerancia de 2 centavos
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Diferencia en totales: Calculado={totalCalculado:F2}, Esperado={importeTotal:F2}, Diferencia={diferenciaTotales:F2}");
                    // Solo advertencia para diferencias menores
                    if (diferenciaTotales > 0.10m) // Error si la diferencia es mayor a 10 centavos
                    {
                        return (false, $"Diferencia excesiva en totales: Calculado={totalCalculado:F2}, Esperado={importeTotal:F2}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"💰 Resumen validación:");
                System.Diagnostics.Debug.WriteLine($"   Total factura: {importeTotal:F2}");
                System.Diagnostics.Debug.WriteLine($"   Suma BaseImp: {sumaBaseImponible:F2}");
                System.Diagnostics.Debug.WriteLine($"   Suma IVA: {sumaIva:F2}");
                System.Diagnostics.Debug.WriteLine($"   Total calculado: {totalCalculado:F2}");
                System.Diagnostics.Debug.WriteLine($"   Diferencia: {diferenciaTotales:F2}");
                System.Diagnostics.Debug.WriteLine("===============================");

                return (true, "");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en validación AFIP: {ex.Message}");
                return (false, $"Error interno en validación: {ex.Message}");
            }
        }

        private void AplicarConfiguracionFacturacion()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .Build();

                bool permitirA = config.GetValue<bool>("Facturacion:PermitirFacturaA", true);
                bool permitirB = config.GetValue<bool>("Facturacion:PermitirFacturaB", true);

                if (btnFacturaA != null) btnFacturaA.Visible = permitirA;
                if (btnFacturaB != null) btnFacturaB.Visible = permitirB;

                // Recalcular posiciones si cambió la visibilidad
                PosicionarBotones();

                System.Diagnostics.Debug.WriteLine($"[CONFIG] FacturaA visible={permitirA}, FacturaB visible={permitirB}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CONFIG ERROR] AplicarConfiguracionFacturacion: {ex.Message}");
            }
        }

        // NUEVO: Métodos helper para cálculos de facturación
        private decimal CalcularImporteNeto()
        {
            // Variable acumuladora
            decimal neto = 0;

            // Obtener remito de forma segura
            var remito = formularioPadre != null ? formularioPadre.GetRemitoActual() : null;
            if (remito != null)
            {
                foreach (DataRow row in remito.Rows)
                {
                    if (decimal.TryParse(row["total"].ToString(), out decimal total) &&
                        decimal.TryParse(row["PorcentajeIva"].ToString(), out decimal porcIva))
                    {
                        // Calcular base imponible (total / (1 + %iva/100))
                        decimal baseImponible = Math.Round(total / (1 + porcIva / 100), 2);
                        neto += baseImponible;
                    }
                }

                // NUEVO: Validar que el neto total no exceda límites AFIP
                neto = Math.Round(neto, 2);
                if (neto >= 10000000000000m) // 13 dígitos
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Importe neto total excede límite: {neto:N2}");
                    neto = Math.Min(neto, 9999999999999.99m);
                    System.Diagnostics.Debug.WriteLine($"🔧 Importe neto ajustado: {neto:N2}");
                }

                return neto;
            }

            // Aproximación si no hay datos detallados
            decimal netoAproximado = Math.Round(importeTotalVenta / 1.21m, 2);

            // NUEVO: Validar aproximación también
            if (netoAproximado >= 10000000000000m)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Importe neto aproximado excede límite: {netoAproximado:N2}");
                netoAproximado = Math.Min(netoAproximado, 9999999999999.99m);
                System.Diagnostics.Debug.WriteLine($"🔧 Importe neto aproximado ajustado: {netoAproximado:N2}");
            }

            return netoAproximado;
        }

        private decimal CalcularImporteIVA()
        {
            decimal neto = CalcularImporteNeto();
            decimal iva = Math.Round(importeTotalVenta - neto, 2);

            // NUEVO: Asegurar que el IVA también esté dentro de límites razonables
            if (iva < 0)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ IVA negativo calculado: {iva:N2}, corrigiendo...");
                iva = 0;
            }

            // Si el IVA resultante es muy grande (edge case), ajustar
            if (iva >= 10000000000000m)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ IVA excede límites: {iva:N2}");
                iva = Math.Min(iva, 9999999999999.99m);
                System.Diagnostics.Debug.WriteLine($"🔧 IVA ajustado: {iva:N2}");
            }

            return iva;
        }

        private void MostrarInformacionEstado(bool hayPagosDigitales, bool pagoCompleto)
        {
            // Label estático para mostrar información del estado
            Label lblMensajeEstado = null;

            // Buscar si ya existe un label de estado
            foreach (Control control in this.Controls)
            {
                if (control is Label lbl && lbl.Name == "lblMensajeEstado")
                {
                    lblMensajeEstado = lbl;
                    break;
                }
            }

            // Remover el label anterior si existe
            if (lblMensajeEstado != null)
            {
                this.Controls.Remove(lblMensajeEstado);
                lblMensajeEstado.Dispose();
                lblMensajeEstado = null;
            }

            string mensaje = "";
            System.Drawing.Color colorFondo = System.Drawing.Color.Transparent;
            System.Drawing.Color colorTexto = System.Drawing.Color.Black;

            // NUEVO: Verificar si las restricciones están deshabilitadas
            bool debeRestringir = DebeRestringirRemitoPorTipoPago();

            if (EsPagoMultiple && !pagoCompleto)
            {
                mensaje = "ATENCION: Complete el pago para habilitar las opciones de impresión";
                colorFondo = System.Drawing.Color.FromArgb(255, 248, 225);
                colorTexto = System.Drawing.Color.FromArgb(133, 100, 4);
            }
            else if (hayPagosDigitales && debeRestringir) // MODIFICADO: Solo mostrar si las restricciones están habilitadas
            {
                mensaje = "INFO: Para pagos digitales solo se permiten facturas electrónicas";
                colorFondo = System.Drawing.Color.FromArgb(217, 237, 247);
                colorTexto = System.Drawing.Color.FromArgb(12, 84, 96);
            }
            else if (EsPagoMultiple && pagoCompleto)
            {
                mensaje = "LISTO: Pago completo - Todas las opciones disponibles";
                colorFondo = System.Drawing.Color.FromArgb(212, 237, 218);
                colorTexto = System.Drawing.Color.FromArgb(21, 87, 36);
            }

            if (!string.IsNullOrEmpty(mensaje))
            {
                // Calcular la posición Y para que quede debajo de los botones
                int buttonsBottom = 0;
                var botones = new Button[] { btnRemito, btnFacturaB, btnFacturaA, btnFinalizarSinImpresion, btnCancelar };
                foreach (var b in botones)
                {
                    if (b != null && b.Visible)
                    {
                        buttonsBottom = Math.Max(buttonsBottom, b.Top + b.Height);
                    }
                }

                // Fallback si por alguna razón no hay botones inicializados aún
                if (buttonsBottom == 0)
                {
                    buttonsBottom = EsPagoMultiple ? (390 + 45) : (270 + 45);
                }

                int topPos = buttonsBottom + 8; // margen de 8px por debajo de los botones

                lblMensajeEstado = new Label
                {
                    Name = "lblMensajeEstado",
                    Text = mensaje,
                    Left = 40,
                    Top = topPos,
                    Width = 600,
                    Height = 25,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    ForeColor = colorTexto,
                    BackColor = colorFondo,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    BorderStyle = BorderStyle.FixedSingle
                };

                this.Controls.Add(lblMensajeEstado);
                lblMensajeEstado.BringToFront();
            }

            System.Diagnostics.Debug.WriteLine($"[INFO ESTADO] Debe restringir: {debeRestringir}, Hay pagos digitales: {hayPagosDigitales}, Mensaje: '{mensaje}'");
        }

        // csharp
        private void ActualizarOpcionesImpresion()
        {
            try
            {
                AplicarConfiguracionFacturacion();

                bool hayPagosDigitales = false;
                bool pagoCompleto = true;

                if (EsPagoMultiple)
                {
                    hayPagosDigitales = multiplePagosControl?.TienePagoDigital() ?? false;
                    pagoCompleto = multiplePagosControl?.PagoCompleto ?? false;
                }
                else
                {
                    hayPagosDigitales = OpcionPagoSeleccionada == OpcionPago.DNI || OpcionPagoSeleccionada == OpcionPago.MercadoPago;
                    pagoCompleto = true;
                }

                bool debeRestringirPorPago = DebeRestringirRemitoPorTipoPago();
                bool puedeRemito = EsPagoMultiple ? pagoCompleto : (pagoCompleto && (!debeRestringirPorPago || !hayPagosDigitales));

                // ✅ NUEVO: Validar límite de facturación para facturas electrónicas
                bool puedeFacturas = pagoCompleto;
                string mensajeLimite = "";

                if (limitarFacturacion && montoLimiteFacturacion > 0 && montoAcumuladoHoy >= montoLimiteFacturacion)
                {
                    // ❌ BLOQUEAR: Ya se alcanzó el límite
                    puedeFacturas = false;
                    mensajeLimite = $"⛔ Límite diario alcanzado ({montoLimiteFacturacion:C2})";
                    System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] ❌ Facturas bloqueadas - Límite alcanzado");
                }

                btnRemito.Enabled = puedeRemito;
                btnFacturaA.Enabled = puedeFacturas;
                btnFacturaB.Enabled = puedeFacturas;
                btnFinalizarSinImpresion.Enabled = pagoCompleto;

                // Actualizar apariencia
                btnRemito.BackColor = puedeRemito ? Color.FromArgb(102, 51, 153) : Color.LightGray;
                btnFacturaA.BackColor = puedeFacturas ? Color.FromArgb(40, 167, 69) : Color.LightGray;
                btnFacturaB.BackColor = puedeFacturas ? Color.FromArgb(0, 123, 255) : Color.LightGray;
                btnFinalizarSinImpresion.BackColor = btnFinalizarSinImpresion.Enabled ? Color.FromArgb(255, 193, 7) : Color.LightGray;

                // ✅ NUEVO: Mostrar mensaje de límite alcanzado
                if (!string.IsNullOrEmpty(mensajeLimite))
                {
                    MostrarMensajeLimiteAlcanzado(mensajeLimite);
                }
                else
                {
                    MostrarInformacionEstado(hayPagosDigitales, pagoCompleto);
                }

                PosicionarBotones();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ActualizarOpcionesImpresion: {ex.Message}");
            }
        }

        // ✅ NUEVO: Mostrar mensaje de límite alcanzado
        private void MostrarMensajeLimiteAlcanzado(string mensaje)
        {
            // Remover mensaje anterior si existe
            Label lblMensajeEstado = null;
            foreach (Control control in this.Controls)
            {
                if (control is Label lbl && lbl.Name == "lblMensajeEstado")
                {
                    lblMensajeEstado = lbl;
                    break;
                }
            }

            if (lblMensajeEstado != null)
            {
                this.Controls.Remove(lblMensajeEstado);
                lblMensajeEstado.Dispose();
                lblMensajeEstado = null;
            }

            // Crear nuevo mensaje de límite alcanzado
            int buttonsBottom = 0;
            var botones = new Button[] { btnRemito, btnFacturaB, btnFacturaA, btnFinalizarSinImpresion, btnCancelar };
            foreach (var b in botones)
            {
                if (b != null && b.Visible)
                {
                    buttonsBottom = Math.Max(buttonsBottom, b.Top + b.Height);
                }
            }

            if (buttonsBottom == 0)
            {
                buttonsBottom = EsPagoMultiple ? (390 + 45) : (270 + 45);
            }

            int topPos = buttonsBottom + 8;

            lblMensajeEstado = new Label
            {
                Name = "lblMensajeEstado",
                Text = mensaje,
                Left = 40,
                Top = topPos,
                Width = 600,
                Height = 16,
                Font = new Font("Segoe UI", 6.5F, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(180, 53, 69), // Rojo para límite alcanzado
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };

            this.Controls.Add(lblMensajeEstado);
            lblMensajeEstado.BringToFront();
        }



        private async Task ConsultarCuitAsync()
        {
            // Implementación mejorada para consultar CUIT con razón social
            string cuitFormateado = txtCuit.Text.Trim();
            lblRazonSocial.Text = "";
            lblRazonSocial.ForeColor = System.Drawing.Color.DarkGreen;

            if (string.IsNullOrEmpty(cuitFormateado))
            {
                return;
            }

            try
            {
                // Limpiar guiones para validación
                string cuitLimpio = cuitFormateado.Replace("-", "");

                // PASO 1: Validar longitud
                if (cuitLimpio.Length != 11)
                {
                    lblRazonSocial.Text = "❌ CUIT debe tener 11 dígitos";
                    lblRazonSocial.ForeColor = System.Drawing.Color.Red;
                    System.Diagnostics.Debug.WriteLine($"[CUIT] ❌ Longitud incorrecta: {cuitLimpio.Length} dígitos");
                    return;
                }

                // PASO 2: Validar que sean solo números
                if (!cuitLimpio.All(char.IsDigit))
                {
                    lblRazonSocial.Text = "❌ CUIT debe contener solo números";
                    lblRazonSocial.ForeColor = System.Drawing.Color.Red;
                    System.Diagnostics.Debug.WriteLine($"[CUIT] ❌ Contiene caracteres no numéricos");
                    return;
                }

                // PASO 3: Validar código verificador según la ley argentina
                if (!ValidarCuitVerificador(cuitLimpio))
                {
                    lblRazonSocial.Text = "❌ CUIT inválido - Código verificadorincorrecto";
                    lblRazonSocial.ForeColor = System.Drawing.Color.Red;
                    System.Diagnostics.Debug.WriteLine($"[CUIT] ❌ Código verificador incorrecto para: {cuitFormateado}");
                    return;
                }

                // PASO 4: CUIT válido - mostrar estado de consulta
                string tipoContribuyente = DeterminarTipoContribuyente(cuitLimpio);
                lblRazonSocial.Text = $"✅ CUIT válido - {tipoContribuyente}";
                lblRazonSocial.ForeColor = System.Drawing.Color.Green;
                Application.DoEvents();

                System.Diagnostics.Debug.WriteLine($"[CUIT] ✅ CUIT válido: {cuitFormateado}");

            }
            catch (Exception ex)
            {
                lblRazonSocial.Text = $"❌ Error consultando CUIT: {ex.Message}";
                lblRazonSocial.ForeColor = System.Drawing.Color.Red;
                System.Diagnostics.Debug.WriteLine($"[CUIT] 💥 Error crítico: {ex.Message}");

            }
        }


        private async Task ProcesarRemito()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 === INICIANDO PROCESAMIENTO REMITO ===");

                bool debeRestringirPorPago = DebeRestringirRemitoPorTipoPago();

                if (EsPagoMultiple)
                {
                    if (!multiplePagosControl.PagoCompleto)
                    {
                        MessageBox.Show(
                            $"ERROR: El pago no está completo.\n\n" +
                            $"Total factura: {importeTotalVenta:C2}\n" +
                            $"Importe asignado: {multiplePagosControl.ImporteAsignado:C2}\n" +
                            $"Importe pendiente: {multiplePagosControl.ImportePendiente:C2}\n\n" +
                            "Complete el pago antes de continuar.",
                            "Pago incompleto",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }
                else
                {
                    if (debeRestringirPorPago &&
                        (OpcionPagoSeleccionada == OpcionPago.DNI || OpcionPagoSeleccionada == OpcionPago.MercadoPago))
                    {
                        MessageBox.Show(
                            "ERROR: No se puede generar un remito con métodos de pago digitales.\n\n" +
                            "Para pagos con DNI o MercadoPago debe generar una factura electrónica (A o B).",
                            "Método de pago no compatible",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }

                btnRemito.Enabled = false;
                btnFacturaA.Enabled = false;
                btnFacturaB.Enabled = false;
                btnFinalizarSinImpresion.Enabled = false;

                this.Cursor = Cursors.WaitCursor;

                OpcionSeleccionada = OpcionImpresion.RemitoTicket;
                string formaPago = EsPagoMultiple ? "Múltiple" : OpcionPagoSeleccionada.ToString();

                System.Diagnostics.Debug.WriteLine($"📋 Procesando remito - Forma de pago: {formaPago}");

                try
                {
                    if (OnProcesarVenta != null)
                    {
                        System.Diagnostics.Debug.WriteLine("🔄 Ejecutando callback OnProcesarVenta...");
                        await OnProcesarVenta("Remito", formaPago, "", "", null, 0, "");
                        System.Diagnostics.Debug.WriteLine("✅ Callback OnProcesarVenta completado exitosamente");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ OnProcesarVenta es null");
                    }

                    System.Diagnostics.Debug.WriteLine("✅ Procesamiento de remito completado exitosamente");

                    // ✅ NUEVO: Aplicar configuración de vista previa (IGUAL QUE EN FACTURAS)
                    if (usarVistaPrevia)
                    {
                        System.Diagnostics.Debug.WriteLine("[REMITO] 🖨️ Imprimiendo con vista previa");
                        await formularioPadre.ImprimirConServicioAsync(this);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[REMITO] 🖨️ Imprimiendo directamente a la impresora");
                        await ImprimirDirectoSinPreview(OpcionImpresion.RemitoTicket);
                    }

                    // ✅ IMPORTANTE: Cerrar DESPUÉS de imprimir
                    this.DialogResult = DialogResult.OK;
                    this.Close();

                    System.Diagnostics.Debug.WriteLine("✅ Modal cerrado - Remito completado");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error en callback OnProcesarVenta: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");

                    this.Cursor = Cursors.Default;
                    btnRemito.Enabled = true;
                    btnFacturaA.Enabled = true;
                    btnFacturaB.Enabled = true;
                    btnFinalizarSinImpresion.Enabled = true;
                    ActualizarOpcionesImpresion();

                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error general en ProcesarRemito: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");

                this.Cursor = Cursors.Default;
                btnRemito.Enabled = true;
                btnFacturaA.Enabled = true;
                btnFacturaB.Enabled = true;
                btnFinalizarSinImpresion.Enabled = true;
                ActualizarOpcionesImpresion();

                MessageBox.Show($"Error procesando remito: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                System.Diagnostics.Debug.WriteLine("🔄 === FIN PROCESAMIENTO REMITO ===");
            }
        }

        // NUEVO: Procesar finalizar venta sin imprimir nada
        private async Task ProcesarFinalizarSinImpresion()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 === INICIANDO FINALIZAR SIN IMPRESIÓN ===");

                bool pagoCompleto = true;
                if (EsPagoMultiple)
                {
                    pagoCompleto = multiplePagosControl?.PagoCompleto ?? false;
                    if (!pagoCompleto)
                    {
                        MessageBox.Show(
                            $"ERROR: El pago no está completo.\n\n" +
                            $"Total factura: {importeTotalVenta:C2}\n" +
                            $"Importe asignado: {multiplePagosControl.ImporteAsignado:C2}\n" +
                            $"Importe pendiente: {multiplePagosControl.ImportePendiente:C2}\n\n" +
                            "Complete el pago antes de continuar.",
                            "Pago incompleto",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }

                if (!pagoCompleto)
                {
                    MessageBox.Show("ERROR: Pago incompleto.", "Pago incompleto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Deshabilitar botones
                btnRemito.Enabled = false;
                btnFacturaA.Enabled = false;
                btnFacturaB.Enabled = false;
                btnFinalizarSinImpresion.Enabled = false;

                this.Cursor = Cursors.WaitCursor;

                // MARCAR explícitamente que se finaliza sin imprimir
                FinalizadoSinImpresion = true;
                OpcionSeleccionada = OpcionImpresion.Ninguna;

                string formaPago = EsPagoMultiple ? "Múltiple" : OpcionPagoSeleccionada.ToString();
                string cuitCliente = txtCuit?.Text.Trim() ?? "";

                try
                {
                    if (OnProcesarVenta != null)
                    {
                        System.Diagnostics.Debug.WriteLine("🔄 Ejecutando callback OnProcesarVenta para SinImpresion...");
                        await OnProcesarVenta("SinImpresion", formaPago, cuitCliente, "", null, 0, "");
                        System.Diagnostics.Debug.WriteLine("✅ Callback OnProcesarVenta (SinImpresion) completado");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ OnProcesarVenta es null para SinImpresion");
                    }

                    // Cerrar modal indicando OK
                    this.DialogResult = DialogResult.OK;
                    this.Close();

                    System.Diagnostics.Debug.WriteLine("✅ Finalizar sin impresión completado y modal cerrado");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error en callback OnProcesarVenta (SinImpresion): {ex.Message}");
                    this.Cursor = Cursors.Default;
                    btnRemito.Enabled = true;
                    btnFacturaA.Enabled = true;
                    btnFacturaB.Enabled = true;
                    btnFinalizarSinImpresion.Enabled = true;
                    ActualizarOpcionesImpresion();
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error general en ProcesarFinalizarSinImpresion: {ex.Message}");
                this.Cursor = Cursors.Default;
                btnRemito.Enabled = true;
                btnFacturaA.Enabled = true;
                btnFacturaB.Enabled = true;
                btnFinalizarSinImpresion.Enabled = true;
                ActualizarOpcionesImpresion();
                MessageBox.Show($"Error al finalizar sin impresión: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                System.Diagnostics.Debug.WriteLine("🔄 === FIN FINALIZAR SIN IMPRESIÓN ===");
            }
        }

        private string FormatearNumeroFactura(int tipoComprobante, int puntoVenta, int numero)
        {
            string tipoLetra = tipoComprobante == 1 ? "A" : "B";
            return $"{tipoLetra} {puntoVenta:D4}-{numero:D8}";
        }

        private string ObtenerCuitEmisor()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            return config["AFIP:CUIT"] ?? "";
        }

        private Dictionary<decimal, (decimal baseImponible, decimal importeIva)> CalcularDetalleIVA()
        {
            var resultado = new Dictionary<decimal, (decimal baseImponible, decimal importeIva)>();

            var remito = formularioPadre != null ? formularioPadre.GetRemitoActual() : null;
            if (remito != null)
            {
                foreach (DataRow row in remito.Rows)
                {
                    if (decimal.TryParse(row["total"].ToString(), out decimal total) &&
                        decimal.TryParse(row["PorcentajeIva"].ToString(), out decimal porcIva))
                    {
                        // CORREGIDO: Asegurar que los cálculos estén dentro de los límites de AFIP
                        decimal baseImponible = Math.Round(total / (1 + porcIva / 100), 2);
                        decimal importeIva = Math.Round(total - baseImponible, 2);

                        // NUEVO: Validar que BaseImp no exceda el límite de AFIP (13 dígitos)
                        if (baseImponible >= 10000000000000m) // 13 dígitos
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ BaseImp muy grande: {baseImponible:N2}, ajustando...");
                            baseImponible = Math.Min(baseImponible, 9999999999999.99m); // Máximo permitido
                            importeIva = Math.Round(total - baseImponible, 2);
                        }

                        if (resultado.ContainsKey(porcIva))
                        {
                            var actual = resultado[porcIva];
                            decimal nuevaBase = Math.Round(actual.baseImponible + baseImponible, 2);
                            decimal nuevoIva = Math.Round(actual.importeIva + importeIva, 2);

                            // NUEVO: Validar el acumulado también
                            if (nuevaBase >= 10000000000000m) // 13 dígitos
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ BaseImp acumulada muy grande: {nuevaBase:N2}, ajustando...");
                                nuevaBase = Math.Min(nuevaBase, 9999999999999.99m);
                                // Recalar IVA para mantener consistencia
                                nuevoIva = Math.Round(nuevaBase * porcIva / 100, 2);
                            }

                            resultado[porcIva] = (nuevaBase, nuevoIva);
                        }
                        else
                        {
                            resultado[porcIva] = (baseImponible, importeIva);
                        }

                        // DEBUG: Mostrar valores calculados
                        System.Diagnostics.Debug.WriteLine($"[IVA] {porcIva}% - Total: {total:N2}, Base: {baseImponible:N2}, IVA: {importeIva:N2}");
                    }
                }
            }
            else
            {
                // Valores por defecto si no hay datos detallados
                decimal baseImponible = Math.Round(CalcularImporteNeto(), 2);
                decimal importeIva = Math.Round(CalcularImporteIVA(), 2);

                // NUEVO: Validar valores por defecto también
                if (baseImponible >= 10000000000000m)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ BaseImp por defecto muy grande: {baseImponible:N2}, ajustando...");
                    baseImponible = Math.Min(baseImponible, 9999999999999.99m);
                    importeIva = Math.Round(importeTotalVenta - baseImponible, 2);
                }

                resultado[21] = (baseImponible, importeIva);
            }

            // DEBUG: Resumen final
            System.Diagnostics.Debug.WriteLine($"=== RESUMEN IVA PARA AFIP ===");
            foreach (var iva in resultado)
            {
                System.Diagnostics.Debug.WriteLine($"IVA {iva.Key}%: Base={iva.Value.baseImponible:N2}, IVA={iva.Value.importeIva:N2}");

                // VALIDACIÓN FINAL: Verificar que BaseImp cumple formato AFIP
                string baseStr = iva.Value.baseImponible.ToString("F2", CultureInfo.InvariantCulture);
                string[] partes = baseStr.Split('.');

                if (partes.Length == 2 && (partes[0].Length > 13 || partes[1].Length != 2))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ ERROR: BaseImp {baseStr} no cumple formato AFIP");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✅ BaseImp {baseStr} cumple formato AFIP");
                }
            }
            System.Diagnostics.Debug.WriteLine("=============================");

            return resultado;
        }

        // Método para verificar si se debe restringir el remito por tipo de pago
        private bool DebeRestringirRemitoPorTipoPago()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                return config.GetValue<bool>("RestriccionesImpresion:RestringirRemitoPorPago", true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error leyendo configuración de restricciones: {ex.Message}");
                return true; // Por defecto restringir para cumplimiento normativo
            }
        }

        // Método para actualizar el estado del remito basado en el tipo de pago
        private void ActualizarEstadoRemitoPorTipoPago()
        {
            if (!DebeRestringirRemitoPorTipoPago())
            {
                // Si está deshabilitada la restricción, permitir remito siempre
                return;
            }

            // Si la restricción está habilitada, verificar el tipo de pago
            bool esEfectivo = false;

            if (rbEfectivo?.Checked == true)
            {
                esEfectivo = true;
            }
            else if (chkMultiplesPagos?.Checked == true && multiplePagosControl != null)
            {
                // Para pagos múltiples, verificar si solo es efectivo
                var pagos = multiplePagosControl.ObtenerPagosPorMedio();
                esEfectivo = pagos.Count == 1 && pagos.ContainsKey("Efectivo");
            }

            // CORREGIDO: Usar btnRemito en lugar de rbRemito
            if (btnRemito != null)
            {
                // No cambiar enabled aquí, se maneja en ActualizarOpcionesImpresion()
                // Solo actualizar mensaje informativo
            }

            // Actualizar mensaje informativo
            ActualizarMensajeInformativo();
        }

        private void ActualizarMensajeInformativo()
        {
            if (lblMensajeInformativo == null) return;

            bool debeRestringir = DebeRestringirRemitoPorTipoPago();

            if (!debeRestringir)
            {
                lblMensajeInformativo.Text = "ℹ️ Restricciones de remito deshabilitadas en configuración";
                lblMensajeInformativo.ForeColor = System.Drawing.Color.Blue;
                lblMensajeInformativo.Visible = true;
                return;
            }

            bool esEfectivo = rbEfectivo?.Checked == true;

            if (chkMultiplesPagos?.Checked == true && multiplePagosControl != null)
            {
                var pagos = multiplePagosControl.ObtenerPagosPorMedio();
                esEfectivo = pagos.Count == 1 && pagos.ContainsKey("Efectivo");
            }

            if (esEfectivo)
            {
                // NUEVO: Ocultar el mensaje cuando es solo efectivo (no hay restricciones activas)
                lblMensajeInformativo.Visible = false;
            }
            else
            {
                lblMensajeInformativo.Text = "⚠️ Para pagos no efectivo solo se permiten Facturas A o B";
                lblMensajeInformativo.ForeColor = System.Drawing.Color.Orange;
                lblMensajeInformativo.Visible = true;
            }

            System.Diagnostics.Debug.WriteLine($"[MENSAJE INFO] Debe restringir: {debeRestringir}, Es efectivo: {esEfectivo}, Visible: {lblMensajeInformativo.Visible}");
        }

        /// <summary>
        /// Valida un CUIT según el algoritmo oficial argentino
        /// </summary>
        /// <param name="cuit">CUIT sin guiones (11 dígitos)</param>
        /// <returns>True si el CUIT es válido</returns>
        private bool ValidarCuitVerificador(string cuit)
        {
            try
            {
                // Validar que tenga exactamente 11 dígitos
                if (string.IsNullOrEmpty(cuit) || cuit.Length != 11 || !cuit.All(char.IsDigit))
                {
                    return false;
                }

                // Convertir a array de enteros
                int[] digitos = cuit.Select(c => int.Parse(c.ToString())).ToArray();

                // Secuencia multiplicadora oficial para CUIT
                int[] multiplicadores = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };

                // Calcular suma ponderada de los primeros 10 dígitos
                int suma = 0;
                for (int i = 0; i < 10; i++)
                {
                    suma += digitos[i] * multiplicadores[i];
                }

                // Calcular resto de la división por 11
                int resto = suma % 11;

                // Determinar dígito verificador según las reglas oficiales
                int digitoVerificadorCalculado;
                if (resto < 2)
                {
                    digitoVerificadorCalculado = resto;
                }
                else
                {
                    digitoVerificadorCalculado = 11 - resto;
                }

                // Comparar con el dígito verificador real (posición 10)
                int digitoVerificadorReal = digitos[10];

                bool esValido = digitoVerificadorCalculado == digitoVerificadorReal;

                System.Diagnostics.Debug.WriteLine($"[CUIT VALIDACIÓN] CUIT: {cuit}");
                System.Diagnostics.Debug.WriteLine($"[CUIT VALIDACIÓN] Suma ponderada: {suma}");
                System.Diagnostics.Debug.WriteLine($"[CUIT VALIDACIÓN] Resto: {resto}");
                System.Diagnostics.Debug.WriteLine($"[CUIT VALIDACIÓN] DV calculado: {digitoVerificadorCalculado}");
                System.Diagnostics.Debug.WriteLine($"[CUIT VALIDACIÓN] DV real: {digitoVerificadorReal}");
                System.Diagnostics.Debug.WriteLine($"[CUIT VALIDACIÓN] Resultado: {(esValido ? "VÁLIDO" : "INVÁLIDO")}");

                return esValido;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CUIT VALIDACIÓN] Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Determina el tipo de contribuyente basado en los primeros 2 dígitos del CUIT
        /// </summary>
        /// <param name="cuit">CUIT sin guiones (11 dígitos)</param>
        /// <returns>Descripción del tipo de contribuyente</returns>
        private string DeterminarTipoContribuyente(string cuit)
        {
            if (string.IsNullOrEmpty(cuit) || cuit.Length < 2)
                return "Indeterminado";

            string prefijo = cuit.Substring(0, 2);

            return prefijo switch
            {
                "20" => "Persona Física Masculino",
                "23" => "Persona Física Femenino",
                "24" => "Persona Física Femenino",
                "27" => "Persona Física Masculino",
                "30" => "Persona Jurídica",
                "33" => "Persona Jurídica",
                "34" => "Persona Jurídica",
                _ => $"Tipo {prefijo} (Otros)"
            };
        }
    }
}
