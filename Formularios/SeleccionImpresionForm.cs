using Comercio.NET.Controles;
using Comercio.NET.Servicios;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Comercio.NET.SeleccionImpresionForm;

namespace Comercio.NET
{
    public partial class SeleccionImpresionForm : Form
    {
        public enum OpcionImpresion
        {
            Ninguna,
            RemitoTicket,
            FacturaB,
            FacturaA,
            FacturaC  // ✅ NUEVO: Factura C para Monotributo
        }

        public enum OpcionPago
        {
            Efectivo,
            DNI,
            MercadoPago,
            Otro  // NUEVO
        }

        // ✅ NUEVO: Enum para especificar el botón inicial con foco
        public enum BotonInicial
        {
            Remito,
            FinalizarSinImpresion
        }

        public OpcionImpresion OpcionSeleccionada { get; private set; } = OpcionImpresion.Ninguna;
        public OpcionPago OpcionPagoSeleccionada { get; private set; } = OpcionPago.Efectivo;

        // NUEVO: Variables para manejo de descuentos
        private decimal porcentajeDescuentoSeleccionado = 0m;
        private decimal importeDescuento = 0m;
        private decimal importeTotalConDescuento = 0m;
        private CheckBox chkAplicarDescuento;
        private ComboBox cboDescuento;
        private Label lblDescuentoDetalle;
        private Panel panelDescuento;

        private TextBox txtCuit;
        private Label lblRazonSocial;

        // MODIFICADO: Referencias a los botones para poder controlar su estado
        private Button btnRemito;
        private Button btnFacturaC;
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

        // ORIGINAL: Referencias a los RadioButtones para retrocompatibilidad
        private RadioButton rbEfectivo;
        private RadioButton rbDNI;
        private RadioButton rbMercadoPago;
        private RadioButton rbOtro; // NUEVO: Declaración del campo

        public decimal PorcentajeDescuento => porcentajeDescuentoSeleccionado;
        public decimal ImporteDescuento => importeDescuento;
        public decimal ImporteTotalConDescuento => importeTotalConDescuento;

        // CORREGIDO: Eliminar referencias a controles que no existen
        private Label lblMensajeInformativo;
        private CheckBox chkMultiplesPagos; // Referencia corregida

        // NUEVO: Label para mostrar el importe total a pagar
        private Label lblImporteTotal;

        // NUEVO: Referencia al botón Cancelar (antes era variable local)
        private Button btnCancelar;

        // NUEVO: Botón para limpiar cache AFIP
        private Button btnLimpiarCacheAfip;

        private decimal montoLimiteFacturacion = 0m; // NUEVO: Límite configurado
        private decimal montoAcumuladoHoy = 0m; // NUEVO: Total facturado en el día
        private bool limitarFacturacion = false; // NUEVO: Si está habilitada la restricción

        // Delegate para el callback después de procesar la venta
        public Func<string, string, string, string, DateTime?, int, string, decimal, decimal, Task> OnProcesarVenta { get; set; }

        private decimal importeTotalVenta;
        private Ventas formularioPadre;
        private BotonInicial botonInicialFoco;


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

        public SeleccionImpresionForm(
                                decimal importeTotal = 0,
                                Ventas padre = null,
                                BotonInicial botonInicial = BotonInicial.Remito
)
        {
            System.Diagnostics.Debug.WriteLine($"[SELECCIÓN] Iniciando con importe: {importeTotal:C2}");

            this.importeTotalVenta = importeTotal;
            this.importeTotalConDescuento = importeTotal; // NUEVO: Inicializar sin descuento
            this.formularioPadre = padre;
            this.botonInicialFoco = botonInicial;


            this.Text = "Seleccione tipo de impresión y método de pago";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Width = 700;
            this.Height = 470;

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

            // ✅ NUEVO: Establecer foco según el parámetro recibido
            this.Shown += (s, e) => EstablecerFocoInicial();
        }

        // ✅ NUEVO: Método para establecer el foco inicial
        private void EstablecerFocoInicial()
        {
            try
            {
                switch (botonInicialFoco)
                {
                    case BotonInicial.FinalizarSinImpresion:
                        if (btnFinalizarSinImpresion != null && btnFinalizarSinImpresion.Visible && btnFinalizarSinImpresion.Enabled)
                        {
                            btnFinalizarSinImpresion.Focus();
                            System.Diagnostics.Debug.WriteLine("✅ Foco establecido en 'Finalizar sin impresión'");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("⚠️ Botón 'Finalizar sin impresión' no disponible, usando Remito");
                            btnRemito?.Focus();
                        }
                        break;

                    case BotonInicial.Remito:
                    default:
                        btnRemito?.Focus();
                        System.Diagnostics.Debug.WriteLine("✅ Foco establecido en 'Remito'");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error estableciendo foco inicial: {ex.Message}");
            }
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
                Text = "Efectivo (F1)",
                Left = 10,
                Top = 30,
                Width = 120,
                Height = 25,
                Font = fontRadio,
                Checked = true
            };

            rbDNI = new RadioButton
            {
                Text = "DNI (F2)",
                Left = 140,
                Top = 30,
                Width = 100,
                Height = 25,
                Font = fontRadio
            };

            rbMercadoPago = new RadioButton
            {
                Text = "MercadoPago (F3)",
                Left = 250,
                Top = 30,
                Width = 160,
                Height = 25,
                Font = fontRadio
            };

            // NUEVO: RadioButton para "Otro"
            rbOtro = new RadioButton
            {
                Text = "Otro (F4)",
                Left = 420,
                Top = 30,
                Width = 100,
                Height = 25,
                Font = fontRadio
            };

            // Agregar el nuevo RadioButton al panelPagoSimple
            panelPagoSimple.Controls.AddRange(new Control[] { lblPago, rbEfectivo, rbDNI, rbMercadoPago, rbOtro });

            // AJUSTADO: Label para mostrar el importe total a pagar con fuente grande
            lblImporteTotal = new Label
            {
                Left = 40,
                Top = 120,
                Width = 600,
                Height = 100,  // ✅ AUMENTADO de 80 a 100 para dar espacio al texto de descuento
                Font = new Font("Segoe UI", 30F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(0, 102, 204),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Text = $"TOTAL A PAGAR: {importeTotalVenta:C2}",
                BackColor = System.Drawing.Color.FromArgb(240, 248, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = true
            };

            // NUEVO: Panel para descuentos
            panelDescuento = new Panel
            {
                Left = 40,
                Top = 230,
                Width = 600,
                Height = 80,
                BackColor = System.Drawing.Color.FromArgb(255, 250, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = true
            };

            // NUEVO: CheckBox para habilitar descuento
            chkAplicarDescuento = new CheckBox
            {
                Text = "💰 Aplicar descuento",
                Left = 10,
                Top = 10,
                Width = 180,
                Height = 25,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(204, 102, 0),
                Checked = false
            };

            // NUEVO: ComboBox para seleccionar porcentaje
            cboDescuento = new ComboBox
            {
                Left = 200,
                Top = 10,
                Width = 120,
                Height = 25,
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };

            // Cargar opciones de descuento desde configuración
            CargarOpcionesDescuento();

            // NUEVO: Label para mostrar detalle del descuento
            lblDescuentoDetalle = new Label
            {
                Left = 10,
                Top = 45,
                Width = 580,
                Height = 25,
                Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                ForeColor = System.Drawing.Color.FromArgb(51, 102, 0),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Text = ""
            };

            panelDescuento.Controls.AddRange(new Control[] {
        chkAplicarDescuento,
        cboDescuento,
        lblDescuentoDetalle
    });

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
            int topCuit = 320; // ✅ CORREGIDO: Definir ANTES de usar

            // ✅ CORREGIDO: Crear label CUIT PRIMERO (una sola vez)
            var lblCuit = new Label
            {
                Name = "lblCuit",
                Text = "CUIT:",
                Left = 40,
                Top = topCuit + 2,
                Width = 50,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

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

            // AJUSTADO: Label para mensaje informativo
            lblMensajeInformativo = new Label
            {
                Left = 40,
                Top = 315,
                Width = 600,
                Height = 25,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = System.Drawing.Color.Blue,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Text = ""
            };

            // AJUSTADO: Botones de impresión
            int topBotones = 370; // ✅ AJUSTADO para dar espacio

            btnRemito = new Button
            {
                Text = "Remito (F5)",
                Width = 130,
                Top = topBotones,
                Height = 45,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(102, 51, 153),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnFacturaC = new Button
            {
                Text = "Factura C (F6)",
                Width = 130,
                Top = topBotones,
                Height = 45,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(255, 87, 34),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnFacturaB = new Button
            {
                Text = "Factura B (F6)",
                Width = 130,
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
                Top = topBotones,
                Height = 45,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(40, 167, 69),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnFinalizarSinImpresion = new Button
            {
                Text = "Sin impresión (F7)",
                Width = 130,
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
                Top = topBotones,
                Height = 45,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(220, 53, 69),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };

            // ✅ NUEVO: Botón para limpiar cache AFIP
            btnLimpiarCacheAfip = new Button
            {
                Text = "🗑️ Limpiar Cache AFIP",
                Width = 150,
                Height = 28,
                Left = this.ClientSize.Width - 165,
                Top = 10,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                BackColor = System.Drawing.Color.FromArgb(220, 53, 69),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLimpiarCacheAfip.FlatAppearance.BorderSize = 0;

            // ✅ Agregar tooltip explicativo
            var toolTip = new ToolTip();
            toolTip.SetToolTip(rbEfectivo, "Atajo: F1\nSelecciona pago en efectivo.");
            toolTip.SetToolTip(rbDNI, "Atajo: F2\nSelecciona pago con DNI.");
            toolTip.SetToolTip(rbMercadoPago, "Atajo: F3\nSelecciona pago con MercadoPago.");
            toolTip.SetToolTip(rbOtro, "Atajo: F4\nSelecciona otro método de pago.");

            toolTip.SetToolTip(btnRemito, "Atajo: F5\nGenera un remito.");
            toolTip.SetToolTip(btnFacturaC, "Atajo: F6\nGenera Factura C.");
            toolTip.SetToolTip(btnFacturaB, "Atajo: F6\nGenera Factura B.");
            toolTip.SetToolTip(btnFinalizarSinImpresion, "Atajo: F7\nFinaliza la venta sin imprimir.");
            toolTip.SetToolTip(btnLimpiarCacheAfip,
                "Elimina tokens AFIP en cache.\n" +
                "Usar solo si hay problemas de autenticación.\n\n" +
                "Requiere reiniciar el sistema después de limpiar.");

            // ✅ IMPORTANTE: Agregar todos los controles al formulario
            this.Controls.AddRange(new Control[] {
                chkPagoMultiple,
                panelPagoSimple,
                lblImporteTotal,
                panelDescuento,      // ✅ Panel de descuentos
                panelPagoMultiple,
                lblCuit,             // ✅ Label CUIT
                txtCuit,
                lblRazonSocial,
                lblMensajeInformativo,
                btnRemito,
                btnFacturaC,
                btnFacturaB,
                btnFacturaA,
                btnFinalizarSinImpresion,
                btnCancelar,
                btnLimpiarCacheAfip
            });

            // Posicionar botones de forma centrada
            PosicionarBotones();
        }

        private void MostrarTooltipPago(Control control)
        {
            var toolTip = new ToolTip();
            toolTip.Show("Atajo activado", control, 0, control.Height, 1200);
        }

        private void MostrarTooltipBoton(Control control)
        {
            var toolTip = new ToolTip();
            toolTip.Show("Atajo activado", control, 0, control.Height, 1200);
        }


        // Método que posiciona secuencialmente los botones pero CENTRADOS en el ancho del formulario.
        private void PosicionarBotones()
        {
            try
            {
                // ✅ ACTUALIZADO: Incluir btnFacturaC
                var botones = new List<Button> {
            btnRemito,
            btnFacturaB,
            btnFacturaC,  // ✅ NUEVO
            btnFacturaA,
            btnFinalizarSinImpresion,
            btnCancelar
        }
                .Where(b => b != null && b.Visible)
                .ToList();

                if (!botones.Any())
                    return;

                int spacing = 15;
                int totalButtonsWidth = botones.Sum(b => b.Width);
                int totalSpacing = spacing * Math.Max(0, botones.Count - 1);
                int totalWidth = totalButtonsWidth + totalSpacing;

                int startLeft = (this.ClientSize.Width - totalWidth) / 2;
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
                switch (e.KeyCode)
                {
                    case Keys.F1:
                        rbEfectivo.Checked = true;
                        //rbEfectivo.Focus();
                        MostrarTooltipPago(rbEfectivo);
                        break;
                    case Keys.F2:
                        rbDNI.Checked = true;
                        //rbDNI.Focus();
                        MostrarTooltipPago(rbDNI);
                        break;
                    case Keys.F3:
                        rbMercadoPago.Checked = true;
                        //rbMercadoPago.Focus();
                        MostrarTooltipPago(rbMercadoPago);
                        break;
                    case Keys.F4:
                        rbOtro.Checked = true;
                        //rbOtro.Focus();
                        MostrarTooltipPago(rbOtro);
                        break;
                    case Keys.F5:
                        if (btnRemito.Enabled && btnRemito.Visible)
                        {
                            btnRemito.PerformClick();
                            MostrarTooltipBoton(btnRemito);
                        }
                        break;
                    case Keys.F6:
                        // Prioridad: Factura C si está visible, sino Factura B
                        if (btnFacturaC.Visible && btnFacturaC.Enabled)
                        {
                            btnFacturaC.PerformClick();
                            MostrarTooltipBoton(btnFacturaC);
                        }
                        else if (btnFacturaB.Visible && btnFacturaB.Enabled)
                        {
                            btnFacturaB.PerformClick();
                            MostrarTooltipBoton(btnFacturaB);
                        }
                        break;
                    case Keys.F7:
                        if (btnFinalizarSinImpresion.Enabled && btnFinalizarSinImpresion.Visible)
                        {
                            btnFinalizarSinImpresion.PerformClick();
                            MostrarTooltipBoton(btnFinalizarSinImpresion);
                        }
                        break;
                    case Keys.Escape:
                        e.SuppressKeyPress = true;
                        this.DialogResult = DialogResult.Cancel;
                        this.Close();
                        break;
                }
            };


            // Evento para cambiar modo de pago
            chkPagoMultiple.CheckedChanged += (s, e) =>
            {
                bool esPagoMultiple = chkPagoMultiple.Checked;

                panelPagoSimple.Visible = !esPagoMultiple;
                panelPagoMultiple.Visible = esPagoMultiple;

                // ✅ NUEVO: Controlar visibilidad del label de importe total
                lblImporteTotal.Visible = !esPagoMultiple;

                // ✅ NUEVO: Ocultar panel de descuento cuando se activa pago múltiple
                panelDescuento.Visible = !esPagoMultiple;

                if (esPagoMultiple)
                {
                    multiplePagosControl.EstablecerImporteTotal(importeTotalVenta);
                    this.Height = 550;

                    // Posiciones para modo MÚLTIPLE
                    txtCuit.Top = 340;
                    lblRazonSocial.Top = 342;
                    lblMensajeInformativo.Top = 365;

                    var lblCuit = this.Controls.Find("lblCuit", true).FirstOrDefault();
                    if (lblCuit != null)
                    {
                        lblCuit.SetBounds(40, 342, 50, 20);
                        lblCuit.Visible = true;
                    }

                    // ✅ CORREGIDO: Posición consistente de botones en modo múltiple
                    int topBotones = 390;
                    btnRemito.Top = topBotones;
                    btnFacturaB.Top = topBotones;
                    btnFacturaC.Top = topBotones;
                    btnFacturaA.Top = topBotones;
                    btnFinalizarSinImpresion.Top = topBotones;
                    btnCancelar.Top = topBotones;
                }
                else
                {
                    // ✅ MODO SIMPLE: Restaurar posiciones originales
                    this.Height = 550;  // ✅ AUMENTADO de 500 a 550 para dar más espacio

                    // Reposicionar label de importe total (visible en modo simple)
                    lblImporteTotal.Top = 120;
                    lblImporteTotal.Visible = true;

                    // Reposicionar panel de descuento (visible en modo simple)
                    panelDescuento.Top = 230;
                    panelDescuento.Visible = true;

                    // Reposicionar CUIT
                    txtCuit.Top = 320;
                    lblRazonSocial.Top = 322;
                    lblMensajeInformativo.Top = 345;

                    var lblCuit = this.Controls.Find("lblCuit", true).FirstOrDefault();
                    if (lblCuit != null)
                    {
                        lblCuit.SetBounds(40, 322, 50, 20);
                        lblCuit.Visible = true;
                    }

                    // ✅ CORREGIDO: Posición consistente de botones en modo simple
                    int topBotones = 370;
                    btnRemito.Top = topBotones;
                    btnFacturaB.Top = topBotones;
                    btnFacturaC.Top = topBotones;
                    btnFacturaA.Top = topBotones;
                    btnFinalizarSinImpresion.Top = topBotones;
                    btnCancelar.Top = topBotones;
                }

                // ✅ CRÍTICO: Reposicionar botones CENTRADOS después de ajustar posiciones Y
                PosicionarBotones();

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

            rbOtro.CheckedChanged += (s, e) =>
            {
                if (rbOtro.Checked)
                {
                    OpcionPagoSeleccionada = OpcionPago.Otro;
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

            btnFacturaC.Click += async (s, e) =>
            {
                try
                {
                    await ProcesarFacturaElectronica(OpcionImpresion.FacturaC);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error en Factura C: {ex.Message}", "Error Crítico",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnFacturaB.Click += async (s, e) =>
            {
                try
                {
                    // ✅ VALIDAR: Si es Monotributo, no permitir (ya se ocultó el botón, pero por seguridad)
                    var (esMonotributo, condicionEmisor) = DeterminarCondicionIVAEmisor();

                    if (esMonotributo)
                    {
                        MessageBox.Show(
                            "⚠️ RESTRICCIÓN DE AFIP\n\n" +
                            "Su condición tributaria es MONOTRIBUTO.\n\n" +
                            "Los Monotributistas NO pueden emitir Factura B (código 6).\n" +
                            "Debe utilizar Factura C (código 11).\n\n" +
                            "Por favor, use el botón 'Factura C' para continuar.",
                            "Factura B No Permitida",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    await ProcesarFacturaElectronica(OpcionImpresion.FacturaB);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error en Factura B: {ex.Message}", "Error Crítico",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            // ✅ NUEVO: Evento para limpiar cache AFIP
            btnLimpiarCacheAfip.Click += async (s, e) =>
            {
                try
                {
                    var resultado = MessageBox.Show(
                        "⚠️ ADVERTENCIA: LIMPIEZA DE CACHE AFIP\n\n" +
                        "Esta acción eliminará:\n" +
                        "• Todos los tokens AFIP en memoria\n" +
                        "• El archivo de tokens guardados\n" +
                        "• Cache de autenticación\n\n" +
                        "Después de limpiar, la próxima factura electrónica\n" +
                        "solicitará nuevos tokens a AFIP.\n\n" +
                        "⚠️ Use esto solo si tiene problemas de autenticación.\n\n" +
                        "¿Desea continuar?",
                        "Confirmar Limpieza de Cache",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2 // No por defecto
                    );

                    if (resultado == DialogResult.Yes)
                    {
                        // Deshabilitar botón mientras se procesa
                        btnLimpiarCacheAfip.Enabled = false;
                        btnLimpiarCacheAfip.Text = "🔄 Limpiando...";
                        this.Cursor = Cursors.WaitCursor;

                        try
                        {
                            System.Diagnostics.Debug.WriteLine("[CACHE AFIP] === INICIANDO LIMPIEZA MANUAL ===");

                            // Limpiar cache en memoria y archivo
                            AfipAuthenticator.ClearTokenCache();

                            // Verificar que el archivo fue eliminado
                            string tokenFilePath = System.IO.Path.Combine(
                                AppDomain.CurrentDomain.BaseDirectory,
                                "afip_tokens.json");

                            bool archivoEliminado = !System.IO.File.Exists(tokenFilePath);

                            System.Diagnostics.Debug.WriteLine($"[CACHE AFIP] Archivo tokens eliminado: {archivoEliminado}");
                            System.Diagnostics.Debug.WriteLine("[CACHE AFIP] === LIMPIEZA COMPLETADA ===");

                            MessageBox.Show(
                                "✅ CACHE AFIP LIMPIADO EXITOSAMENTE\n\n" +
                                "Se han eliminado:\n" +
                                $"• Tokens en memoria: ✓\n" +
                                $"• Archivo de tokens: {(archivoEliminado ? "✓" : "⚠️ No encontrado")}\n\n" +
                                "💡 IMPORTANTE:\n" +
                                "La próxima factura electrónica solicitará\n" +
                                "nuevos tokens a AFIP automáticamente.\n\n" +
                                "Si los problemas persisten, verifique:\n" +
                                "1. Certificado AFIP válido y no expirado\n" +
                                "2. Conexión a internet estable\n" +
                                "3. Configuración en appsettings.json",
                                "Cache Limpiado",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );

                            // Restaurar botón
                            btnLimpiarCacheAfip.Text = "🗑️ Limpiar Cache AFIP";
                            btnLimpiarCacheAfip.Enabled = true;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CACHE AFIP] ❌ Error durante limpieza: {ex.Message}");

                            MessageBox.Show(
                                $"⚠️ Error durante la limpieza:\n\n{ex.Message}\n\n" +
                                $"El cache en memoria se limpió, pero el archivo\n" +
                                $"puede requerir eliminación manual.\n\n" +
                                $"Ruta del archivo:\n" +
                                $"{System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "afip_tokens.json")}",
                                "Error en Limpieza",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning
                            );

                            // Restaurar botón aunque haya error
                            btnLimpiarCacheAfip.Text = "🗑️ Limpiar Cache AFIP";
                            btnLimpiarCacheAfip.Enabled = true;
                        }
                        finally
                        {
                            this.Cursor = Cursors.Default;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CACHE AFIP] 💥 Error crítico: {ex.Message}");
                    MessageBox.Show(
                        $"Error inesperado:\n\n{ex.Message}",
                        "Error Crítico",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );

                    btnLimpiarCacheAfip.Text = "🗑️ Limpiar Cache AFIP";
                    btnLimpiarCacheAfip.Enabled = true;
                    this.Cursor = Cursors.Default;
                }
            };

            // ✅ NUEVO: Ajustar posición del botón cuando se redimensiona el formulario
            this.Resize += (s, e) =>
            {
                if (btnLimpiarCacheAfip != null)
                {
                    btnLimpiarCacheAfip.Left = this.ClientSize.Width - 165;
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
            // NUEVO: Eventos para descuentos
            chkAplicarDescuento.CheckedChanged += (s, e) =>
            {
                cboDescuento.Enabled = chkAplicarDescuento.Checked;

                if (!chkAplicarDescuento.Checked)
                {
                    // Limpiar descuento
                    porcentajeDescuentoSeleccionado = 0m;
                    importeDescuento = 0m;
                    lblDescuentoDetalle.Text = "";
                }
                else
                {
                    // ✅ NUEVO: Si se habilita el descuento, seleccionar "Efectivo" automáticamente
                    if (!EsPagoMultiple) // Solo si NO es pago múltiple
                    {
                        rbEfectivo.Checked = true;
                        OpcionPagoSeleccionada = OpcionPago.Efectivo;
                        System.Diagnostics.Debug.WriteLine("[DESCUENTO] ✅ Método de pago cambiado automáticamente a Efectivo");
                    }
                }

                AplicarDescuento();
            };

            cboDescuento.SelectedIndexChanged += (s, e) =>
            {
                if (chkAplicarDescuento.Checked)
                {
                    AplicarDescuento();
                }
            };

            // NUEVO: Validar restricciones al cambiar método de pago
            rbEfectivo.CheckedChanged += (s, e) =>
            {
                if (rbEfectivo.Checked)
                {
                    OpcionPagoSeleccionada = OpcionPago.Efectivo;
                    ValidarDescuentoPorMetodoPago();
                    ActualizarOpcionesImpresion();
                }
            };

            rbDNI.CheckedChanged += (s, e) =>
            {
                if (rbDNI.Checked)
                {
                    OpcionPagoSeleccionada = OpcionPago.DNI;
                    ValidarDescuentoPorMetodoPago();
                    ActualizarOpcionesImpresion();
                }
            };

            rbMercadoPago.CheckedChanged += (s, e) =>
            {
                if (rbMercadoPago.Checked)
                {
                    OpcionPagoSeleccionada = OpcionPago.MercadoPago;
                    ValidarDescuentoPorMetodoPago();
                    ActualizarOpcionesImpresion();
                }
            };

            rbOtro.CheckedChanged += (s, e) =>
            {
                if (rbOtro.Checked)
                {
                    OpcionPagoSeleccionada = OpcionPago.Otro;
                    ValidarDescuentoPorMetodoPago();
                    ActualizarOpcionesImpresion();
                }
            };

        }
        /// <summary>
        /// Valida si el descuento es compatible con el método de pago actual
        /// </summary>
        private void ValidarDescuentoPorMetodoPago()
        {
            if (!chkAplicarDescuento.Checked || porcentajeDescuentoSeleccionado == 0)
            {
                return; // Sin descuento activo, no validar
            }

            if (!ValidarRestriccionesDescuento(out string mensajeError))
            {
                MessageBox.Show(
                    mensajeError,
                    "Restricción de Descuento",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                // Desactivar descuento automáticamente
                chkAplicarDescuento.Checked = false;
            }
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

                // ✅ NUEVO: Validar condición IVA del emisor
                var (esMonotributo, condicionEmisor) = DeterminarCondicionIVAEmisor();

                System.Diagnostics.Debug.WriteLine($"[EMISOR] Condición IVA: {condicionEmisor}");
                System.Diagnostics.Debug.WriteLine($"[EMISOR] Es Monotributo: {esMonotributo}");

                // ✅ VALIDACIÓN 1: Si es Monotributo, NO puede emitir Factura A
                if (esMonotributo && tipoFactura == OpcionImpresion.FacturaA)
                {
                    MessageBox.Show(
                        "⚠️ RESTRICCIÓN DE AFIP\n\n" +
                        "Los Monotributistas NO pueden emitir Factura A.\n\n" +
                        "Solo puede emitir:\n" +
                        "• Factura B (para consumidores finales)\n" +
                        "• Factura C (entre monotributistas)\n\n" +
                        "Para emitir Factura A debe estar inscripto como\n" +
                        "Responsable Inscripto en el Impuesto al IVA.",
                        "Tipo de Factura No Permitido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // ✅ CRÍTICO: CAPTURAR DESCUENTOS ANTES DE INICIAR EL PROCESO
                decimal porcentajeDescuentoCapturado = porcentajeDescuentoSeleccionado;
                decimal importeDescuentoCapturado = importeDescuento;
                decimal importeTotalConDescuentoCapturado = importeTotalConDescuento;

                System.Diagnostics.Debug.WriteLine($"[FACTURA] 💰 DESCUENTOS CAPTURADOS:");
                System.Diagnostics.Debug.WriteLine($"[FACTURA]   - Porcentaje: {porcentajeDescuentoCapturado}%");
                System.Diagnostics.Debug.WriteLine($"[FACTURA]   - Importe descuento: {importeDescuentoCapturado:C2}");
                System.Diagnostics.Debug.WriteLine($"[FACTURA]   - Total con descuento: {importeTotalConDescuentoCapturado:C2}");

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

                btnRemito.Enabled = false;
                btnFacturaA.Enabled = false;
                btnFacturaB.Enabled = false;
                btnFinalizarSinImpresion.Enabled = false;

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string cuitEmisor = AfipAuthenticator.ObtenerCUITActivo();
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

                    // ✅ CALCULAR VARIABLES PRIMERO
                    int tipoComprobante;
                    if (tipoFactura == OpcionImpresion.FacturaA)
                    {
                        tipoComprobante = 1; // Factura A
                    }
                    else if (tipoFactura == OpcionImpresion.FacturaC)
                    {
                        tipoComprobante = 11; // Factura C (Monotributo)
                        System.Diagnostics.Debug.WriteLine($"[AFIP] Factura C seleccionada manualmente");
                    }
                    else // Factura B
                    {
                        // Detección automática B vs C
                        tipoComprobante = esMonotributo ? 11 : 6;
                        System.Diagnostics.Debug.WriteLine($"[AFIP] Tipo comprobante seleccionado: {tipoComprobante} ({(esMonotributo ? "Factura C (Monotributo)" : "Factura B")})");
                    }

                    int puntoVenta = ObtenerPuntoVentaActivo();
                    int ultimoNumero = await ObtenerUltimoNumeroComprobanteReal(tipoComprobante, puntoVenta);
                    int numero = ultimoNumero + 1;

                    string cuitCliente = tipoFactura == OpcionImpresion.FacturaA ? txtCuit.Text.Trim() : "";

                    // Determinar docTipo y docNro
                    int docTipo;
                    long docNro;
                    int ivaPerNro;

                    if (tipoComprobante == 1) // Factura A
                    {
                        docTipo = 80; // CUIT
                        docNro = !string.IsNullOrEmpty(cuitCliente) ? long.Parse(cuitCliente.Replace("-", "")) : 0;
                        ivaPerNro = DeterminarCondicionIvaReceptor(tipoComprobante, cuitCliente);
                    }
                    else if (tipoComprobante == 11) // Factura C
                    {
                        docTipo = 99; // Sin identificación
                        docNro = 0;
                        ivaPerNro = 5; // Consumidor Final
                    }
                    else // Factura B
                    {
                        docTipo = 99;
                        docNro = 0;
                        ivaPerNro = 5; // Consumidor Final
                    }

                    decimal importeTotalCalculado = Math.Round(
                        porcentajeDescuentoSeleccionado > 0 ? importeTotalConDescuento : importeTotalVenta,
                        2);

                    System.Diagnostics.Debug.WriteLine($"[FACTURA] Total original: {importeTotalVenta:C2}");
                    if (porcentajeDescuentoSeleccionado > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FACTURA] Descuento {porcentajeDescuentoSeleccionado}%: -{importeDescuento:C2}");
                        System.Diagnostics.Debug.WriteLine($"[FACTURA] Total con descuento: {importeTotalCalculado:C2}");
                    }

                    // ✅ AHORA SÍ, LOGS DESPUÉS DE CALCULAR LAS VARIABLES
                    System.Diagnostics.Debug.WriteLine($"[AFIP] === DATOS DEL COMPROBANTE ===");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Tipo: {tipoComprobante} ({(tipoComprobante == 1 ? "Factura A" : tipoComprobante == 11 ? "Factura C" : "Factura B")})");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Punto de Venta: {puntoVenta}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Número: {numero}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] CUIT Emisor: {ObtenerCuitEmisor()}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] CUIT Cliente: {cuitCliente}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] DocTipo: {docTipo}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] DocNro: {docNro}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] IVAPerNro (Condición IVA Receptor): {ivaPerNro}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Importe Total: {importeTotalCalculado:F2}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] ================================");

                    System.Diagnostics.Debug.WriteLine($"📋 Tipo: {tipoComprobante}, PV: {puntoVenta}, Último: {ultimoNumero}, Nuevo: {numero}");

                    lblProgress.Text = "Solicitando CAE a AFIP...";
                    Application.DoEvents();

                    var resultadoCAE = await SolicitarCAEReal(tipoComprobante, puntoVenta, numero, cuitCliente);

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
                    NumeroFacturaAfip = numero;
                    OpcionSeleccionada = tipoFactura;

                    string numeroFormateado = FormatearNumeroFactura(tipoComprobante, puntoVenta, numero);

                    string formaPago = EsPagoMultiple ? "Múltiple" : OpcionPagoSeleccionada.ToString();
                    string tipoFacturaString = tipoFactura switch
                    {
                        OpcionImpresion.FacturaA => "FacturaA",
                        OpcionImpresion.FacturaC => "FacturaC",
                        _ => "FacturaB"
                    };

                    System.Diagnostics.Debug.WriteLine($"🔄 Ejecutando callback OnProcesarVenta para {tipoFacturaString}...");

                    if (OnProcesarVenta != null)
                    {
                        // ✅ CRÍTICO: Pasar los descuentos CAPTURADOS al callback
                        await OnProcesarVenta(
                                tipoFacturaString,
                                formaPago,
                                cuitCliente,
                                CAENumero,
                                CAEVencimiento,
                                NumeroFacturaAfip,
                                numeroFormateado,
                                porcentajeDescuentoCapturado,
                                importeDescuentoCapturado);

                        System.Diagnostics.Debug.WriteLine("✅ Callback OnProcesarVenta completado exitosamente");
                    }

                    // ✅ AGREGAR AQUÍ: Descontar stock DESPUÉS de guardar la factura
                    await DescontarStockProductos();

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

        /// <summary>
        /// Descuenta el stock de los productos vendidos en la base de datos
        /// </summary>
        private async Task DescontarStockProductos()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📦 === INICIANDO DESCUENTO DE STOCK ===");

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                // Obtener el remito actual desde el formulario padre
                var remito = formularioPadre?.GetRemitoActual();

                if (remito == null || remito.Rows.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ No hay productos para descontar stock");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"📋 Productos en remito: {remito.Rows.Count}");

                // ✅ NUEVO: Mostrar columnas disponibles en el DataTable
                System.Diagnostics.Debug.WriteLine("📋 Columnas disponibles en remito:");
                foreach (DataColumn col in remito.Columns)
                {
                    System.Diagnostics.Debug.WriteLine($"   - {col.ColumnName} ({col.DataType.Name})");
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    System.Diagnostics.Debug.WriteLine("✅ Conexión a base de datos abierta");

                    // ✅ NUEVO: Verificar estructura de tabla Productos
                    string checkTableQuery = @"
                SELECT COLUMN_NAME, DATA_TYPE 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'Productos' 
                AND COLUMN_NAME IN ('cantidad', 'stock', 'Stock', 'Cantidad', 'existencia')";

                    using (var checkCmd = new SqlCommand(checkTableQuery, connection))
                    using (var reader = await checkCmd.ExecuteReaderAsync())
                    {
                        System.Diagnostics.Debug.WriteLine("📊 Columnas de stock encontradas en tabla Productos:");
                        bool hayColumnaStock = false;
                        while (await reader.ReadAsync())
                        {
                            string columnName = reader.GetString(0);
                            string dataType = reader.GetString(1);
                            System.Diagnostics.Debug.WriteLine($"   - {columnName} ({dataType})");
                            hayColumnaStock = true;
                        }

                        if (!hayColumnaStock)
                        {
                            System.Diagnostics.Debug.WriteLine("❌ ERROR CRÍTICO: No se encontró columna de stock en tabla Productos");
                            MessageBox.Show(
                                "⚠️ CONFIGURACIÓN INCORRECTA\n\n" +
                                "No se encontró una columna de stock en la tabla Productos.\n\n" +
                                "Verifique que exista alguna de estas columnas:\n" +
                                "• cantidad\n" +
                                "• stock\n" +
                                "• Stock\n" +
                                "• existencia",
                                "Error de Base de Datos",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return;
                        }
                    }

                    int productosDescontados = 0;
                    int productosNoEncontrados = 0;
                    int productosError = 0;

                    foreach (DataRow row in remito.Rows)
                    {
                        string codigo = row["codigo"]?.ToString()?.Trim();
                        int cantidad = row["cantidad"] != DBNull.Value ? Convert.ToInt32(row["cantidad"]) : 0;
                        string descripcion = row["descripcion"]?.ToString() ?? "";

                        System.Diagnostics.Debug.WriteLine($"\n🔍 Procesando producto:");
                        System.Diagnostics.Debug.WriteLine($"   - Código: '{codigo}'");
                        System.Diagnostics.Debug.WriteLine($"   - Cantidad a descontar: {cantidad}");
                        System.Diagnostics.Debug.WriteLine($"   - Descripción: {descripcion}");

                        if (string.IsNullOrEmpty(codigo))
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Código vacío o null - SALTANDO");
                            productosError++;
                            continue;
                        }

                        if (cantidad <= 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Cantidad inválida ({cantidad}) - SALTANDO");
                            productosError++;
                            continue;
                        }

                        // ✅ PRIMERO: Verificar si el producto existe y obtener stock actual
                        string checkQuery = "SELECT cantidad FROM Productos WHERE codigo = @codigo";
                        using (var checkCmd = new SqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@codigo", codigo);
                            var stockActual = await checkCmd.ExecuteScalarAsync();

                            if (stockActual == null || stockActual == DBNull.Value)
                            {
                                System.Diagnostics.Debug.WriteLine($"❌ Producto NO encontrado en BD con código: '{codigo}'");
                                productosNoEncontrados++;
                                continue;
                            }

                            int stockActualInt = Convert.ToInt32(stockActual);
                            System.Diagnostics.Debug.WriteLine($"   📊 Stock actual en BD: {stockActualInt}");
                            System.Diagnostics.Debug.WriteLine($"   📉 Stock después de descuento: {stockActualInt - cantidad}");
                        }

                        // ✅ SEGUNDO: Descontar stock en la base de datos
                        string updateQuery = @"
                    UPDATE Productos 
                    SET cantidad = cantidad - @cantidad 
                    WHERE codigo = @codigo";

                        using (var cmd = new SqlCommand(updateQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@cantidad", cantidad);
                            cmd.Parameters.AddWithValue("@codigo", codigo);

                            int rowsAffected = await cmd.ExecuteNonQueryAsync();

                            if (rowsAffected > 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"✅ Stock DESCONTADO exitosamente");
                                productosDescontados++;

                                // ✅ VERIFICAR el stock después del update
                                using (var verifyCmd = new SqlCommand("SELECT cantidad FROM Productos WHERE codigo = @codigo", connection))
                                {
                                    verifyCmd.Parameters.AddWithValue("@codigo", codigo);
                                    var nuevoStock = await verifyCmd.ExecuteScalarAsync();
                                    int nuevoStockInt = nuevoStock != null && nuevoStock != DBNull.Value ? Convert.ToInt32(nuevoStock) : -1;
                                    System.Diagnostics.Debug.WriteLine($"   ✅ VERIFICACIÓN: Nuevo stock en BD: {nuevoStockInt}");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ UPDATE no afectó ninguna fila (código: '{codigo}')");
                                productosNoEncontrados++;
                            }
                        }
                    }

                    System.Diagnostics.Debug.WriteLine("\n📊 === RESUMEN DESCUENTO DE STOCK ===");
                    System.Diagnostics.Debug.WriteLine($"   ✅ Productos descontados: {productosDescontados}");
                    System.Diagnostics.Debug.WriteLine($"   ⚠️ Productos no encontrados: {productosNoEncontrados}");
                    System.Diagnostics.Debug.WriteLine($"   ❌ Productos con errores: {productosError}");
                    System.Diagnostics.Debug.WriteLine("=====================================");

                    if (productosNoEncontrados > 0 || productosError > 0)
                    {
                        MessageBox.Show(
                            $"⚠️ ADVERTENCIA: Descuento de stock incompleto\n\n" +
                            $"Productos descontados: {productosDescontados}\n" +
                            $"Productos no encontrados: {productosNoEncontrados}\n" +
                            $"Productos con errores: {productosError}\n\n" +
                            $"Revise el inventario manualmente.",
                            "Advertencia de Stock",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }

                System.Diagnostics.Debug.WriteLine("✅ Descuento de stock completado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR CRÍTICO descontando stock: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");

                MessageBox.Show(
                    $"⚠️ ERROR AL ACTUALIZAR STOCK\n\n" +
                    $"La venta se registró correctamente, pero hubo un error al descontar el stock:\n\n" +
                    $"{ex.Message}\n\n" +
                    $"Por favor, verifique manualmente el inventario.",
                    "Error de Stock",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                // ✅ NUEVO: Debug de valores de descuento
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] 💰 DATOS DE DESCUENTO:");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   - PorcentajeDescuento: {porcentajeDescuentoSeleccionado}");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   - ImporteDescuento: {importeDescuento:C2}");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   - ImporteTotalConDescuento: {importeTotalConDescuento:C2}");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   - ImporteTotalVenta: {importeTotalVenta:C2}");

                var config = new Servicios.TicketConfig
                {
                    NombreComercio = formularioPadre.GetNombreComercio(),
                    DomicilioComercio = formularioPadre.GetDomicilioComercio(), // ✅ Verificar que esto no esté vacío
                    FormaPago = EsPagoMultiple ? "Múltiple" : OpcionPagoSeleccionada.ToString(), // ✅ CORREGIDO
                    MensajePie = "Gracias por su compra!",
                    // ✅ NUEVO: Pasar datos de descuento a TicketConfig
                    PorcentajeDescuento = porcentajeDescuentoSeleccionado,
                    ImporteDescuento = importeDescuento,
                    ImporteFinal = porcentajeDescuentoSeleccionado > 0 ? importeTotalConDescuento : importeTotalVenta
                };

                // ✅ CORREGIDO: Determinar tipo de comprobante basado en OpcionImpresion real
                switch (tipoComprobante)
                {
                    case OpcionImpresion.RemitoTicket:
                        config.TipoComprobante = "REMITO";
                        config.NumeroComprobante = $"Remito N° {formularioPadre.GetNroRemitoActual()}";
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ✅ Configurando REMITO");
                        break;

                    case OpcionImpresion.FacturaC:
                        config.TipoComprobante = "FacturaC";

                        // ✅ CRÍTICO: Debug COMPLETO antes de formatear
                        int puntoVentaObtenido = ObtenerPuntoVentaActivo();

                        System.Diagnostics.Debug.WriteLine($"[DEBUG FACTURA C] ============================");
                        System.Diagnostics.Debug.WriteLine($"[DEBUG FACTURA C] NumeroFacturaAfip (propiedad): {NumeroFacturaAfip}");
                        System.Diagnostics.Debug.WriteLine($"[DEBUG FACTURA C] Punto Venta obtenido: {puntoVentaObtenido}");
                        System.Diagnostics.Debug.WriteLine($"[DEBUG FACTURA C] ============================");

                        // ✅ VERIFICAR: Si NumeroFacturaAfip está mal, usar el número correcto
                        string numeroFormateado = FormatearNumeroFactura(11, puntoVentaObtenido, NumeroFacturaAfip);

                        System.Diagnostics.Debug.WriteLine($"[DEBUG FACTURA C] Número formateado: {numeroFormateado}");

                        config.NumeroComprobante = numeroFormateado;
                        config.CAE = CAENumero;
                        config.CAEVencimiento = CAEVencimiento;

                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ✅ Configurando FACTURA C");
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   Número: {config.NumeroComprobante}");
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   CAE: {config.CAE}");
                        break;

                    case OpcionImpresion.FacturaB:
                        config.TipoComprobante = "FacturaB";
                        config.NumeroComprobante = FormatearNumeroFactura(6, ObtenerPuntoVentaActivo(), NumeroFacturaAfip);
                        config.CAE = CAENumero;
                        config.CAEVencimiento = CAEVencimiento;
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ✅ Configurando FACTURA B");
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   Número: {config.NumeroComprobante}");
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   CAE: {config.CAE}");
                        break;

                    case OpcionImpresion.FacturaA:
                        config.TipoComprobante = "FacturaA";
                        config.NumeroComprobante = FormatearNumeroFactura(1, ObtenerPuntoVentaActivo(), NumeroFacturaAfip);
                        config.CAE = CAENumero;
                        config.CAEVencimiento = CAEVencimiento;
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ✅ Configurando FACTURA A");
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   Número: {config.NumeroComprobante}");
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   CAE: {config.CAE}");
                        break;
                }

                // ✅ NUEVO: Mostrar configuración completa antes de imprimir
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] 📋 CONFIGURACIÓN FINAL:");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   - TipoComprobante: {config.TipoComprobante}");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   - PorcentajeDescuento: {config.PorcentajeDescuento}");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   - ImporteDescuento: {config.ImporteDescuento:C2}");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   - ImporteFinal: {config.ImporteFinal:C2}");

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

        // ✅ AGREGAR este método nuevo (línea ~1450):
        /// <summary>
        /// Determina si el CUIT emisor es Monotributista o Responsable Inscripto
        /// </summary>
        private (bool esMonotributo, string condicion) DeterminarCondicionIVAEmisor()
        {
            try
            {
                // ✅ PASO 1: Leer condición desde configuración PRIMERO
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string ambienteActivo = config["AFIP:AmbienteActivo"] ?? "Produccion";

                // ✅ CRÍTICO: Buscar en la sección correcta según el ambiente
                string condicionConfig = config[$"AFIP:{ambienteActivo}:CondicionIVA"];

                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] ========================================");
                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] Ambiente activo: {ambienteActivo}");
                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] Ruta búsqueda: AFIP:{ambienteActivo}:CondicionIVA");
                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] Valor encontrado: '{condicionConfig}'");

                if (!string.IsNullOrEmpty(condicionConfig))
                {
                    bool esMonotributo = condicionConfig.Equals("Monotributo", StringComparison.OrdinalIgnoreCase);
                    System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] ✅ Configuración explícita encontrada");
                    System.Diagnostics.Debug.WriteLine($"[CONDICION IVA]    Condición: {condicionConfig}");
                    System.Diagnostics.Debug.WriteLine($"[CONDICION IVA]    Es Monotributo: {esMonotributo}");
                    System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] ========================================");
                    return (esMonotributo, condicionConfig);
                }

                // ⚠️ FALLBACK SOLO SI NO HAY CONFIGURACIÓN
                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] ⚠️ NO hay configuración explícita de CondicionIVA");
                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] ⚠️ Determinando por prefijo CUIT (FALLBACK)...");

                string cuitEmisor = ObtenerCuitEmisor().Replace("-", "");

                if (string.IsNullOrEmpty(cuitEmisor) || cuitEmisor.Length != 11)
                {
                    System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] ⚠️ CUIT inválido o vacío: '{cuitEmisor}'");
                    System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] ⚠️ ASUMIENDO Responsable Inscripto por defecto");
                    System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] ========================================");
                    return (false, "Responsable Inscripto (por defecto)");
                }

                string prefijo = cuitEmisor.Substring(0, 2);
                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] CUIT: {cuitEmisor}");
                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] Prefijo: {prefijo}");

                // Prefijos de empresas (30, 33, 34) = Responsable Inscripto
                if (prefijo == "30" || prefijo == "33" || prefijo == "34")
                {
                    System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] ✅ Prefijo {prefijo} -> Responsable Inscripto (empresa)");
                    System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] ========================================");
                    return (false, "Responsable Inscripto");
                }

                // Personas físicas (20, 23, 24, 27) - Asumir Monotributo
                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] ⚠️ Prefijo {prefijo} -> Monotributo (persona física - fallback)");
                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] ========================================");
                return (true, "Monotributo");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] ❌ Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] ⚠️ Asumiendo Responsable Inscripto por error");
                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] ========================================");
                return (false, "Responsable Inscripto (por error)");
            }
        }

        private async Task AutenticarConAfipReal(string cuitEmisor)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔑 === AUTENTICACIÓN AFIP (SIMPLIFICADA) ===");

                // NUEVO: Verificar si ya hay token válido
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

                // SIMPLIFICADO: Ya no necesitamos leer el appsettings.json aquí
                // El AfipAuthenticator.GetTAAsync() lo hace automáticamente
                try
                {
                    // NUEVO: Llamada simplificada sin parámetros
                    var (token, sign, expiration) = await AfipAuthenticator.GetTAAsync("wsfe");

                    TokenAfip = token;
                    SignAfip = sign;

                    System.Diagnostics.Debug.WriteLine("✅ Autenticación AFIP completada");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Token válido hasta: {expiration:dd/MM/yyyy HH:mm:ss}");
                }
                catch (Exception ex) when (ex.Message.Contains("TOKEN") || ex.Message.Contains("token") || ex.Message.Contains("Ya existe"))
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] 🔄 Usando token existente debido a: {ex.Message}");

                    var tokenUltimoRecurso = AfipAuthenticator.GetExistingToken("wsfe");
                    if (tokenUltimoRecurso.HasValue)
                    {
                        TokenAfip = tokenUltimoRecurso.Value.token;
                        SignAfip = tokenUltimoRecurso.Value.sign;
                        System.Diagnostics.Debug.WriteLine("✅ Token existente recuperado exitosamente");
                        return;
                    }

                    throw new Exception($"No se pudo obtener token AFIP: {ex.Message}");
                }

                System.Diagnostics.Debug.WriteLine("✅ === AUTENTICACIÓN COMPLETADA ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 Error en autenticación: {ex.Message}");

                // Último recurso: intentar usar token del cache
                try
                {
                    var tokenUltimoRecurso = AfipAuthenticator.GetExistingToken("wsfe");
                    if (tokenUltimoRecurso.HasValue)
                    {
                        TokenAfip = tokenUltimoRecurso.Value.token;
                        SignAfip = tokenUltimoRecurso.Value.sign;
                        System.Diagnostics.Debug.WriteLine("🆘 Usando token de último recurso");
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
                // ✅ USAR CLIENTE DINÁMICO
                using (var wsfeClient = AfipAuthenticator.CrearClienteWSFE())
                {
                    var authRequest = new ArcaWS.FEAuthRequest
                    {
                        Token = TokenAfip,
                        Sign = SignAfip,
                        Cuit = long.Parse(ObtenerCuitEmisor().Replace("-", ""))  // ✅ CAMBIADO de ObtenerCUITActivo()
                    };

                    System.Diagnostics.Debug.WriteLine($"[AFIP] Consultando último número - Tipo: {tipoComprobante}, PV: {puntoVenta}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Usando CUIT: {ObtenerCuitEmisor()}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Token length: {TokenAfip?.Length ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Sign length: {SignAfip?.Length ?? 0}");

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
                System.Diagnostics.Debug.WriteLine($"[AFIP] Stack: {ex.StackTrace}");
                return 0;
            }
        }

        private async Task<(bool exito, string cae, DateTime? vencimiento, string error)> SolicitarCAEReal(
            int tipoComprobante, int puntoVenta, int numero, string cuitCliente = "")
        {
            try
            {
                using (var wsfeClient = AfipAuthenticator.CrearClienteWSFE())
                {
                    var authRequest = new ArcaWS.FEAuthRequest
                    {
                        Token = TokenAfip,
                        Sign = SignAfip,
                        Cuit = long.Parse(ObtenerCuitEmisor().Replace("-", ""))  // ✅ CAMBIADO de ObtenerCUITActivo()
                    };

                    System.Diagnostics.Debug.WriteLine($"[AFIP] === SOLICITUD CAE ===");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Tipo: {tipoComprobante}, PV: {puntoVenta}, Num: {numero}");

                    bool esFacturaC = (tipoComprobante == 11);

                    int docTipo;
                    long docNro;
                    int ivaPerNro;

                    if (tipoComprobante == 1) // Factura A
                    {
                        docTipo = 80; // CUIT
                        docNro = !string.IsNullOrEmpty(cuitCliente) ? long.Parse(cuitCliente.Replace("-", "")) : 0;
                        ivaPerNro = DeterminarCondicionIvaReceptor(tipoComprobante, cuitCliente);
                    }
                    else if (esFacturaC) // Factura C
                    {
                        docTipo = 99; // Sin identificación
                        docNro = 0;
                        ivaPerNro = 5; // Consumidor Final
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Factura C - DocTipo: {docTipo}, IVAPerNro: {ivaPerNro}");
                    }
                    else // Factura B
                    {
                        docTipo = 99;
                        docNro = 0;
                        ivaPerNro = 5; // ✅ CORREGIDO: Siempre Consumidor Final para Monotributo
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Factura B - DocTipo: {docTipo}, IVAPerNro: {ivaPerNro}");
                    }

                    decimal importeTotalCalculado = Math.Round(
                    porcentajeDescuentoSeleccionado > 0 ? importeTotalConDescuento : importeTotalVenta,
                    2);

                    System.Diagnostics.Debug.WriteLine($"[FACTURA] Total original: {importeTotalVenta:C2}");
                    if (porcentajeDescuentoSeleccionado > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FACTURA] Descuento {porcentajeDescuentoSeleccionado}%: -{importeDescuento:C2}");
                        System.Diagnostics.Debug.WriteLine($"[FACTURA] Total con descuento: {importeTotalCalculado:C2}");
                    }
                    decimal importeNetoCalculado;
                    decimal importeIvaCalculado;

                    if (esFacturaC)
                    {
                        importeNetoCalculado = importeTotalCalculado;
                        importeIvaCalculado = 0;
                        System.Diagnostics.Debug.WriteLine($"[AFIP] Factura C - Total: {importeTotalCalculado:F2} (sin IVA discriminado)");
                    }
                    else
                    {
                        importeNetoCalculado = Math.Round(CalcularImporteNeto(), 2);
                        importeIvaCalculado = Math.Round(CalcularImporteIVA(), 2);

                        if (Math.Abs((importeNetoCalculado + importeIvaCalculado) - importeTotalCalculado) > 0.02m)
                        {
                            importeIvaCalculado = importeTotalCalculado - importeNetoCalculado;
                            importeIvaCalculado = Math.Round(importeIvaCalculado, 2);
                        }
                    }

                    // ✅ CRÍTICO: Crear comprobante con TODOS los campos obligatorios
                    var comprobante = new ArcaWS.FECAEDetRequest
                    {
                        Concepto = 1,
                        DocTipo = docTipo,
                        DocNro = docNro,
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

                    // ✅ CRÍTICO: Intentar asignar IVAPerNro con múltiples nombres posibles
                    try
                    {
                        // Opción 1: IVAPerNro
                        var propertyIVAPerNro = comprobante.GetType().GetProperty("IVAPerNro");
                        if (propertyIVAPerNro != null)
                        {
                            propertyIVAPerNro.SetValue(comprobante, ivaPerNro);
                            System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Campo asignado: IVAPerNro = {ivaPerNro}");
                        }
                        else
                        {
                            // Opción 2: IvaPer
                            var propertyIvaPer = comprobante.GetType().GetProperty("IvaPer");
                            if (propertyIvaPer != null)
                            {
                                propertyIvaPer.SetValue(comprobante, ivaPerNro);
                                System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Campo asignado: IvaPer = {ivaPerNro}");
                            }
                            else
                            {
                                // Opción 3: CondicionIVAReceptorId (usado en tu código anterior)
                                var propertyCondicionIVA = comprobante.GetType().GetProperty("CondicionIVAReceptorId");
                                if (propertyCondicionIVA != null)
                                {
                                    propertyCondicionIVA.SetValue(comprobante, ivaPerNro);
                                    System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Campo asignado: CondicionIVAReceptorId = {ivaPerNro}");
                                }
                                else
                                {
                                    // Si no existe ninguno de estos campos, listar todos los disponibles
                                    var props = comprobante.GetType().GetProperties();
                                    System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ NO SE ENCONTRÓ campo IVAPerNro. Propiedades disponibles:");
                                    foreach (var prop in props)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"   - {prop.Name} ({prop.PropertyType.Name})");
                                    }

                                    return (false, "", null, "Error crítico: No se pudo asignar condición IVA del receptor. Verifique la referencia del servicio AFIP.");
                                }
                            }
                        }
                    }
                    catch (Exception exReflection)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ Error asignando IVAPerNro: {exReflection.Message}");
                        return (false, "", null, $"Error asignando condición IVA: {exReflection.Message}");
                    }

                    // Solo para Factura A y B: agregar array de IVA
                    if (!esFacturaC)
                    {
                        var ivaArray = new List<ArcaWS.AlicIva>();
                        var datosIva = CalcularDetalleIVA();

                        var (esValido, errorValidacion) = ValidarDatosParaAfip(datosIva, importeTotalCalculado);
                        if (!esValido)
                        {
                            return (false, "", null, $"Error de validación: {errorValidacion}");
                        }

                        foreach (var iva in datosIva)
                        {
                            if (iva.Value.baseImponible > 0)
                            {
                                decimal baseImponible = Math.Round(iva.Value.baseImponible, 2);
                                decimal importeIva = Math.Round(iva.Value.importeIva, 2);

                                if (baseImponible >= 10000000000000m)
                                {
                                    baseImponible = 9999999999999.99m;
                                    importeIva = Math.Round(iva.Value.baseImponible + iva.Value.importeIva - baseImponible, 2);
                                }

                                string baseFormateada = baseImponible.ToString("F2", CultureInfo.InvariantCulture);
                                string[] partesBase = baseFormateada.Split('.');

                                if (partesBase[0].Length > 13 || partesBase[1].Length != 2)
                                {
                                    baseImponible = Math.Min(baseImponible, 9999999999999.99m);
                                    baseImponible = Math.Round(baseImponible, 2);
                                }

                                int codigoAfip = MapearPorcentajeIvaACodigoAfip(iva.Key);

                                ivaArray.Add(new ArcaWS.AlicIva
                                {
                                    Id = codigoAfip,
                                    BaseImp = (double)baseImponible,
                                    Importe = (double)importeIva
                                });
                            }
                        }

                        if (ivaArray.Any())
                        {
                            comprobante.Iva = ivaArray.ToArray();
                            System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Array IVA: {ivaArray.Count} alícuotas");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Factura C - Sin array IVA");
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

                    System.Diagnostics.Debug.WriteLine($"[AFIP] 📤 Enviando: {tipoComprobante}-{puntoVenta:D4}-{numero:D8}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] DocTipo: {docTipo}, DocNro: {docNro}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Total: {comprobante.ImpTotal:F2}, Neto: {comprobante.ImpNeto:F2}, IVA: {comprobante.ImpIVA:F2}");

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

                            System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ CAE: {detalle.CAE}");
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
                    return 1; // Por defecto Responsable Inscripto para Factura A
                }

                // ✅ NUEVO: Determinar tipo según prefijo CUIT
                string prefijo = cuitLimpio.Substring(0, 2);

                // Prefijos para Responsables Inscriptos (empresas): 30, 33, 34
                if (prefijo == "30" || prefijo == "33" || prefijo == "34")
                {
                    System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] Factura A - Empresa (prefijo {prefijo}) -> Responsable Inscripto (1)");
                    return 1; // Responsable Inscripto
                }

                // Prefijos para personas físicas: 20, 23, 24, 27
                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] Factura A con CUIT válido: '{cuitCliente}' -> Responsable Inscripto (1)");
                return 1; // Para Factura A, siempre Responsable Inscripto
            }
            else // Factura B
            {
                // ✅ CORREGIDO: Para Factura B, el receptor es SIEMPRE Consumidor Final
                // porque el emisor (tu CUIT) es Monotributista
                System.Diagnostics.Debug.WriteLine($"[CONDICION IVA] Factura B -> Consumidor Final (5)");
                return 5; // Consumidor Final
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

                    // ✅ CORREGIDO: Validar coherencia entre porcentaje, base e importe
                    decimal ivaCalculadoEsperado = Math.Round(baseImponible * (iva.Key / 100m), 2);
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

        // ✅ AGREGAR este nuevo método al final de la clase (línea ~1900):
        /// <summary>
        /// Obtiene el punto de venta configurado para el ambiente activo
        /// </summary>
        private int ObtenerPuntoVentaActivo()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                string ambienteActivo = config["AFIP:AmbienteActivo"] ?? "Testing";

                // ✅ NUEVO: Intentar leer de múltiples formas
                string puntoVentaStr = config[$"AFIP:{ambienteActivo}:PuntoVenta"];

                if (string.IsNullOrEmpty(puntoVentaStr))
                {
                    System.Diagnostics.Debug.WriteLine($"[PUNTO VENTA] ⚠️ No se encontró configuración para {ambienteActivo}, usando 1");
                    return 1;
                }

                if (!int.TryParse(puntoVentaStr, out int puntoVenta))
                {
                    System.Diagnostics.Debug.WriteLine($"[PUNTO VENTA] ⚠️ Valor inválido '{puntoVentaStr}', usando 1");
                    return 1;
                }

                if (puntoVenta < 1 || puntoVenta > 9999)
                {
                    System.Diagnostics.Debug.WriteLine($"[PUNTO VENTA] ⚠️ Punto de venta fuera de rango ({puntoVenta}), usando 1");
                    return 1;
                }

                System.Diagnostics.Debug.WriteLine($"[PUNTO VENTA] ✅ Ambiente: {ambienteActivo}, PV: {puntoVenta}");
                return puntoVenta;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PUNTO VENTA] ❌ Error: {ex.Message}, usando PV 1 por defecto");
                return 1;
            }
        }

        private void AplicarConfiguracionFacturacion()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                // Leer configuración de checkboxes
                bool permitirA = config.GetValue<bool>("Facturacion:PermitirFacturaA", true);
                bool permitirB = config.GetValue<bool>("Facturacion:PermitirFacturaB", true);
                bool permitirC = config.GetValue<bool>("Facturacion:PermitirFacturaC", true);

                System.Diagnostics.Debug.WriteLine($"[CONFIG] Permitir A: {permitirA}, B: {permitirB}, C: {permitirC}");

                // ✅ NUEVO: Obtener condición IVA del emisor
                var (esMonotributo, condicionEmisor) = DeterminarCondicionIVAEmisor();

                System.Diagnostics.Debug.WriteLine($"[CONFIG] Condición emisor: {condicionEmisor}");
                System.Diagnostics.Debug.WriteLine($"[CONFIG] Es Monotributo: {esMonotributo}");

                // ✅ NUEVO: Si es Responsable Inscripto, OCULTAR Factura C
                if (!esMonotributo && condicionEmisor.Contains("Responsable Inscripto"))
                {
                    permitirC = false; // Forzar ocultar Factura C para Responsables Inscriptos
                    System.Diagnostics.Debug.WriteLine($"[CONFIG] ⚠️ RESPONSABLE INSCRIPTO detectado - Forzando: C=false");
                    System.Diagnostics.Debug.WriteLine($"[CONFIG]    Razón: Los Responsables Inscriptos deben emitir Factura A o B según el cliente");
                }

                // ✅ NUEVO: Si es Monotributo, forzar configuración específica
                if (esMonotributo)
                {
                    permitirA = false; // Monotributo NO puede emitir A
                    permitirB = false; // Monotributo NO puede emitir B
                    permitirC = true;  // Monotributo DEBE emitir C

                    System.Diagnostics.Debug.WriteLine($"[CONFIG] ⚠️ MONOTRIBUTO detectado - Forzando: A=false, B=false, C=true");
                }

                // Aplicar visibilidad de botones
                btnFacturaA.Visible = permitirA;
                btnFacturaB.Visible = permitirB;
                btnFacturaC.Visible = permitirC;

                System.Diagnostics.Debug.WriteLine($"[CONFIG] Botones visibles - A: {btnFacturaA.Visible}, B: {btnFacturaB.Visible}, C: {btnFacturaC.Visible}");

                // Si ningún botón de factura está visible, mostrar advertencia
                if (!permitirA && !permitirB && !permitirC)
                {
                    System.Diagnostics.Debug.WriteLine("[CONFIG] ⚠️ ADVERTENCIA: Ninguna opción de factura habilitada");
                }

                // Reposicionar botones visibles para que queden centrados
                PosicionarBotones();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CONFIG ERROR] Error aplicando configuración: {ex.Message}");

                // Valores por defecto en caso de error
                btnFacturaA.Visible = true;
                btnFacturaB.Visible = true;
                btnFacturaC.Visible = true;
                PosicionarBotones();
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

                // ✅ MODIFICADO: "Finalizar sin impresión" sigue la misma lógica que Remito
                bool puedeFinalizarSinImpresion = puedeRemito;

                // ✅ NUEVO: Validar límite de facturación para facturas electrónicas
                bool puedeFacturas = pagoCompleto;
                string mensajeLimite = "";

                if (limitarFacturacion && montoLimiteFacturacion > 0 && montoAcumuladoHoy >= montoLimiteFacturacion)
                {
                    // ❌ BLOQUEO TOTAL: Ya se alcanzó el límite
                    puedeFacturas = false;
                    mensajeLimite = $"⛔ Límite diario alcanzado ({montoLimiteFacturacion:C2})";
                    System.Diagnostics.Debug.WriteLine($"[LÍMITE FACTURACIÓN] ❌ Facturas bloqueadas - Límite alcanzado");
                }

                btnRemito.Enabled = puedeRemito;
                btnFacturaA.Enabled = puedeFacturas;
                btnFacturaB.Enabled = puedeFacturas;
                btnFacturaC.Enabled = puedeFacturas;  // ✅ NUEVO
                btnFinalizarSinImpresion.Enabled = puedeFinalizarSinImpresion; // ✅ CORREGIDO

                // Actualizar apariencia
                btnRemito.BackColor = puedeRemito ? Color.FromArgb(102, 51, 153) : Color.LightGray;
                btnFacturaA.BackColor = puedeFacturas ? Color.FromArgb(40, 167, 69) : Color.LightGray;
                btnFacturaB.BackColor = puedeFacturas ? Color.FromArgb(0, 123, 255) : Color.LightGray;
                btnFacturaC.BackColor = puedeFacturas ? Color.FromArgb(255, 87, 34) : Color.LightGray;  // ✅ NUEVO
                btnFinalizarSinImpresion.BackColor = puedeFinalizarSinImpresion ? Color.FromArgb(255, 193, 7) : Color.LightGray;

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

            int topPos = buttonsBottom + 8; // margen de 8px por debajo de los botones

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

                // PASO 3: Validar código verificador según la ley
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
                        await OnProcesarVenta("Remito", formaPago, "", "", null, 0, "",
                            porcentajeDescuentoSeleccionado,  // ✅ AGREGAR
                            importeDescuento);                 // ✅ AGREGAR
                        System.Diagnostics.Debug.WriteLine("✅ Callback OnProcesarVenta completado exitosamente");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ OnProcesarVenta es null");
                    }

                    await DescontarStockProductos();

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

                    System.Diagnostics.Debug.WriteLine("✅ Finalizar sin impresión completado y modal cerrado");
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
                        await OnProcesarVenta("SinImpresion", formaPago, cuitCliente, "", null, 0, "",
                                porcentajeDescuentoSeleccionado,  // ✅ AGREGAR
                                importeDescuento);                 // ✅ AGREGAR
                        System.Diagnostics.Debug.WriteLine("✅ Callback OnProcesarVenta (SinImpresion) completado");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ OnProcesarVenta es null para SinImpresion");
                    }

                    await DescontarStockProductos();

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
        //{
        //    string tipoLetra = tipoComprobante switch
        //    {
        //        1 => "A",     // Factura A
        //        6 => "B",     // Factura B
        //        11 => "C",    // ✅ NUEVO: Factura C (Monotributo)
        //        _ => "X"      // Desconocido
        //    };

        //    return $"{tipoLetra} {puntoVenta:D4}-{numero:D8}";
        //}
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔧 FormatearNumeroFactura:");
                System.Diagnostics.Debug.WriteLine($"   - Tipo: {tipoComprobante}");
                System.Diagnostics.Debug.WriteLine($"   - Punto Venta: {puntoVenta}");
                System.Diagnostics.Debug.WriteLine($"   - Número: {numero}");

                // ✅ CRÍTICO: Convertir código numérico AFIP a letra
                string tipoLetra = tipoComprobante switch
                {
                    1 => "A",      // Factura A
                    6 => "B",      // Factura B
                    11 => "C",     // Factura C (Monotributo)
                    _ => "X"       // Desconocido
                };

                // ✅ FORMATO CORRECTO PARA BASE DE DATOS: "B 0007-00000003"
                string numeroFormateado = $"{tipoLetra} {puntoVenta:D4}-{numero:D8}";

                System.Diagnostics.Debug.WriteLine($"   ✅ Número formateado final: {numeroFormateado}");

                return numeroFormateado;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en FormatearNumeroFactura: {ex.Message}");
                // Fallback seguro
                return $"X {puntoVenta:D4}-{numero:D8}";
            }
        }

        private string ObtenerCuitEmisor()
        {
            try
            {
                // NUEVO: Usar el método simplificado del AfipAuthenticator
                return AfipAuthenticator.ObtenerCUITActivo();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CUIT] Error obteniendo CUIT: {ex.Message}");

                // Fallback al método anterior si falla
                try
                {
                    var config = new ConfigurationBuilder()
                        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                        .AddJsonFile("appsettings.json")
                        .Build();

                    return config["AFIP:CUIT"] ?? "";
                }
                catch
                {
                    return "";
                }
            }
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
                            System.Diagnostics.Debug.WriteLine($"⚠️ BaseImp muy grande: {baseImponible:N2}, corrigiendo...");
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
                                System.Diagnostics.Debug.WriteLine($"⚠️ BaseImp acumulada muy grande: {nuevaBase:N2}, corrigiendo...");
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
                    System.Diagnostics.Debug.WriteLine($"⚠️ BaseImp por defecto muy grande: {baseImponible:N2}, corrigiendo...");
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
        /// <summary>
        /// Carga las opciones de descuento desde appsettings.json
        /// </summary>
        private void CargarOpcionesDescuento()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                // Leer opciones de descuento configuradas
                var descuentosConfig = config.GetSection("Descuentos:OpcionesDisponibles").Get<List<decimal>>();

                if (descuentosConfig != null && descuentosConfig.Any())
                {
                    cboDescuento.Items.Clear();
                    foreach (var descuento in descuentosConfig)
                    {
                        cboDescuento.Items.Add($"{descuento}%");
                    }

                    // Seleccionar el primero por defecto
                    if (cboDescuento.Items.Count > 0)
                    {
                        cboDescuento.SelectedIndex = 0;
                    }

                    System.Diagnostics.Debug.WriteLine($"[DESCUENTOS] Cargadas {descuentosConfig.Count} opciones");
                }
                else
                {
                    // Valores por defecto si no hay configuración
                    cboDescuento.Items.AddRange(new object[] { "5%", "10%", "15%", "20%" });
                    cboDescuento.SelectedIndex = 0;
                    System.Diagnostics.Debug.WriteLine("[DESCUENTOS] Usando opciones por defecto");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DESCUENTOS] Error cargando configuración: {ex.Message}");

                // Valores por defecto en caso de error
                cboDescuento.Items.AddRange(new object[] { "5%", "10%" });
                cboDescuento.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Calcula y aplica el descuento al total de la factura
        /// </summary>
        private void AplicarDescuento()
        {
            try
            {
                if (!chkAplicarDescuento.Checked || cboDescuento.SelectedItem == null)
                {
                    // Sin descuento
                    porcentajeDescuentoSeleccionado = 0m;
                    importeDescuento = 0m;
                    importeTotalConDescuento = importeTotalVenta;
                    lblDescuentoDetalle.Text = "";

                    System.Diagnostics.Debug.WriteLine("[DESCUENTO] Descuento desactivado");
                }
                else
                {
                    // Extraer porcentaje del texto seleccionado (ej: "10%" -> 10)
                    string textoSeleccionado = cboDescuento.SelectedItem.ToString();
                    string numeroPorcentaje = textoSeleccionado.Replace("%", "").Trim();

                    if (decimal.TryParse(numeroPorcentaje, out decimal porcentaje))
                    {
                        porcentajeDescuentoSeleccionado = porcentaje;
                        importeDescuento = Math.Round(importeTotalVenta * (porcentaje / 100m), 2);
                        importeTotalConDescuento = Math.Round(importeTotalVenta - importeDescuento, 2);

                        // Validar que el descuento no sea mayor al total
                        if (importeTotalConDescuento < 0)
                        {
                            importeTotalConDescuento = 0;
                            importeDescuento = importeTotalVenta;
                        }

                        lblDescuentoDetalle.Text =
                            $"✓ Descuento aplicado: -{importeDescuento:C2} " +
                            $"(Total anterior: {importeTotalVenta:C2})";

                        System.Diagnostics.Debug.WriteLine($"[DESCUENTO] ========================================");
                        System.Diagnostics.Debug.WriteLine($"[DESCUENTO] Porcentaje: {porcentajeDescuentoSeleccionado}%");
                        System.Diagnostics.Debug.WriteLine($"[DESCUENTO] Total original: {importeTotalVenta:C2}");
                        System.Diagnostics.Debug.WriteLine($"[DESCUENTO] Importe descuento: {importeDescuento:C2}");
                        System.Diagnostics.Debug.WriteLine($"[DESCUENTO] Total con descuento: {importeTotalConDescuento:C2}");
                        System.Diagnostics.Debug.WriteLine($"[DESCUENTO] ========================================");
                    }
                }

                // Actualizar label de total a pagar
                ActualizarLabelTotalAPagar();

                // Revalidar restricciones con el nuevo total
                ActualizarOpcionesImpresion();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DESCUENTO] Error aplicando descuento: {ex.Message}");
                MessageBox.Show(
                    $"Error al calcular el descuento:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Actualiza el label que muestra el total a pagar
        /// </summary>
        private void ActualizarLabelTotalAPagar()
        {
            if (lblImporteTotal != null)
            {
                if (porcentajeDescuentoSeleccionado > 0)
                {
                    // ✅ MEJORADO: Usar fuente más pequeña cuando hay descuento para evitar cortes
                    lblImporteTotal.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
                    lblImporteTotal.Text =
                        $"TOTAL A PAGAR: {importeTotalConDescuento:C2}\n" +
                        $"(Descuento {porcentajeDescuentoSeleccionado}% aplicado)";
                    lblImporteTotal.ForeColor = System.Drawing.Color.FromArgb(0, 153, 51); // Verde
                }
                else
                {
                    // ✅ RESTAURADO: Fuente original cuando no hay descuento
                    lblImporteTotal.Font = new Font("Segoe UI", 30F, FontStyle.Bold);
                    lblImporteTotal.Text = $"TOTAL A PAGAR: {importeTotalVenta:C2}";
                    lblImporteTotal.ForeColor = System.Drawing.Color.FromArgb(0, 102, 204); // Azul original
                }
            }
        }

        /// <summary>
        /// Valida restricciones de descuento según método de pago y configuración
        /// </summary>
        private bool ValidarRestriccionesDescuento(out string mensajeError)
        {
            mensajeError = "";

            try
            {
                if (!chkAplicarDescuento.Checked || porcentajeDescuentoSeleccionado == 0)
                {
                    return true; // Sin descuento, siempre válido
                }

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                // Validar si hay restricciones por método de pago
                bool restringirPorPago = config.GetValue<bool>("Descuentos:RestringirPorMetodoPago", false);

                if (restringirPorPago)
                {
                    var metodosPermitidos = config.GetSection("Descuentos:MetodosPagoPermitidos")
                        .Get<List<string>>() ?? new List<string>();

                    if (metodosPermitidos.Any())
                    {
                        string metodoPagoActual = EsPagoMultiple
                            ? "Múltiple"
                            : OpcionPagoSeleccionada.ToString();

                        if (!metodosPermitidos.Contains(metodoPagoActual))
                        {
                            mensajeError =
                                $"⚠️ RESTRICCIÓN DE DESCUENTO\n\n" +
                                $"El descuento solo está disponible para:\n" +
                                $"• {string.Join("\n• ", metodosPermitidos)}\n\n" +
                                $"Método de pago actual: {metodoPagoActual}";

                            System.Diagnostics.Debug.WriteLine($"[DESCUENTO] ❌ Método de pago no permitido: {metodoPagoActual}");
                            return false;
                        }
                    }
                }

                // Validar descuento máximo permitido
                decimal descuentoMaximo = config.GetValue<decimal>("Descuentos:PorcentajeMaximo", 100m);

                if (porcentajeDescuentoSeleccionado > descuentoMaximo)
                {
                    mensajeError =
                        $"⚠️ DESCUENTO EXCESIVO\n\n" +
                        $"El descuento máximo permitido es {descuentoMaximo}%.\n" +
                        $"Descuento seleccionado: {porcentajeDescuentoSeleccionado}%";

                    System.Diagnostics.Debug.WriteLine($"[DESCUENTO] ❌ Descuento excede máximo: {porcentajeDescuentoSeleccionado}% > {descuentoMaximo}%");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[DESCUENTO] ✅ Validación exitosa");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DESCUENTO] Error en validación: {ex.Message}");
                mensajeError = $"Error validando descuento: {ex.Message}";
                return false;
            }
        }
    }
}
