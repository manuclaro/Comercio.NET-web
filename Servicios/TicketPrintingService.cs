using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Comercio.NET.Servicios
{
    public class TicketPrintingService : IDisposable
    {
        private readonly PrintDocument printDocument;
        private DataTable datosTicket;
        private TicketConfig configuracion;
        private bool disposed = false;

        // NUEVO: Almacenar datos de IVA por producto para facturas
        private List<ProductoConIva> productosConIva;
        private Dictionary<decimal, (decimal BaseImponible, decimal ImporteIva)> resumenIva;

        // NUEVO: Datos de facturación desde appsettings.json
        private DatosFacturacion datosFacturacion;

        // ✅ MODIFICADO: Variables para el cálculo de altura
        private float alturaContenidoCalculada = 0f;
        private bool modoCalculoAltura = true; // Indica si estamos calculando o imprimiendo

        public TicketPrintingService()
        {
            printDocument = new PrintDocument();
            printDocument.PrintPage += PrintDocument_PrintPage;
            ConfigurarTamañoTicket();

            // NUEVO: Cargar datos de facturación
            CargarDatosFacturacion();
        }

        // NUEVO: Método para cargar datos de facturación desde appsettings.json
        private void CargarDatosFacturacion()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                datosFacturacion = new DatosFacturacion
                {
                    RazonSocial = config["Facturacion:RazonSocial"] ?? "",
                    CUIT = config["Facturacion:CUIT"] ?? "",
                    IngBrutos = config["Facturacion:IngBrutos"] ?? "",
                    DomicilioFiscal = config["Facturacion:DomicilioFiscal"] ?? "",
                    CodigoPostal = config["Facturacion:CodigoPostal"] ?? "",
                    InicioActividades = config["Facturacion:InicioActividades"] ?? "",
                    Condicion = config["Facturacion:Condicion"] ?? ""
                };

                System.Diagnostics.Debug.WriteLine($"✅ Datos de facturación cargados: {datosFacturacion.RazonSocial}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando datos de facturación: {ex.Message}");
                datosFacturacion = new DatosFacturacion();
            }
        }

        private void ConfigurarTamañoTicket()
        {
            // ✅ MODIFICADO: Tamaño inicial para cálculo
            int anchoTicket = (int)(70 / 25.4 * 100); // 70mm de ancho
            int altoTicket = (int)(297 / 25.4 * 100); // Tamaño A4 inicial para cálculo

            PaperSize ticketSize = new PaperSize("Ticket", anchoTicket, altoTicket);
            printDocument.DefaultPageSettings.PaperSize = ticketSize;
            printDocument.DefaultPageSettings.Margins = new Margins(2, 2, 5, 5);

            System.Diagnostics.Debug.WriteLine($"[PAPEL] ✅ Configurado papel inicial - Ancho: {anchoTicket / 100.0 * 25.4:F1}mm, Alto: {altoTicket / 100.0 * 25.4:F1}mm");
        }

        public async Task ImprimirTicket(DataTable datos, TicketConfig config)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(TicketPrintingService));

            datosTicket = datos;
            configuracion = config;

            // ✅ NUEVO: Resetear estado antes de imprimir
            ResetearEstadoImpresion();

            // ✅ NO cargar IVA para Factura C
            if (config.TipoComprobante.Contains("Factura") || config.TipoComprobante.Contains("FACTURA"))
            {
                System.Diagnostics.Debug.WriteLine("🔍 Detectada factura, cargando datos IVA...");

                if (!config.TipoComprobante.Contains("FacturaC") && !config.TipoComprobante.Contains("FACTURA C"))
                {
                    await CargarDatosIvaParaFactura();
                    System.Diagnostics.Debug.WriteLine($"✅ Datos IVA cargados: {productosConIva?.Count ?? 0} productos");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("🔵 Factura C detectada - NO se discrimina IVA");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("📄 Detectado remito, usando formato estándar");
            }

            // ✅ CRÍTICO: Primera pasada - calcular altura
            System.Diagnostics.Debug.WriteLine("📐 PASO 1: Calculando altura del contenido...");
            modoCalculoAltura = true;

            using (Bitmap bmp = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                PrintPageEventArgs args = new PrintPageEventArgs(
                    g,
                    printDocument.DefaultPageSettings.Bounds,
                    printDocument.DefaultPageSettings.Bounds,
                    printDocument.DefaultPageSettings
                );
                PrintDocument_PrintPage(printDocument, args);
            }

            // ✅ CRÍTICO: Ajustar tamaño del papel con la altura calculada
            AjustarTamanoPapelAlContenido();

            // ✅ CRÍTICO: Segunda pasada - imprimir con tamaño correcto
            System.Diagnostics.Debug.WriteLine("🖨️ PASO 2: Imprimiendo con tamaño ajustado...");
            modoCalculoAltura = false;

            using (PrintPreviewDialog previewDialog = new PrintPreviewDialog())
            {
                previewDialog.Document = printDocument;
                previewDialog.WindowState = FormWindowState.Maximized;
                previewDialog.ShowDialog();
            }
        }

        public async Task ImprimirTicketDirecto(DataTable datos, TicketConfig config)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(TicketPrintingService));

            datosTicket = datos;
            configuracion = config;

            // ✅ NUEVO: Resetear estado antes de imprimir
            ResetearEstadoImpresion();

            if (config.TipoComprobante.Contains("Factura") || config.TipoComprobante.Contains("FACTURA"))
            {
                if (!config.TipoComprobante.Contains("FacturaC") && !config.TipoComprobante.Contains("FACTURA C"))
                {
                    await CargarDatosIvaParaFactura();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("🔵 Factura C - Impresión directa sin IVA");
                }
            }

            // ✅ CRÍTICO: Primera pasada - calcular altura
            System.Diagnostics.Debug.WriteLine("📐 PASO 1: Calculando altura del contenido (impresión directa)...");
            modoCalculoAltura = true;

            using (Bitmap bmp = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                PrintPageEventArgs args = new PrintPageEventArgs(
                    g,
                    printDocument.DefaultPageSettings.Bounds,
                    printDocument.DefaultPageSettings.Bounds,
                    printDocument.DefaultPageSettings
                );
                PrintDocument_PrintPage(printDocument, args);
            }

            // ✅ CRÍTICO: Ajustar tamaño del papel
            AjustarTamanoPapelAlContenido();

            // ✅ CRÍTICO: Segunda pasada - imprimir
            System.Diagnostics.Debug.WriteLine("🖨️ PASO 2: Imprimiendo directamente con tamaño ajustado...");
            modoCalculoAltura = false;

            printDocument.Print();
        }

        private async Task CargarDatosIvaParaFactura()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Iniciando carga de datos IVA...");

                productosConIva = new List<ProductoConIva>();
                resumenIva = new Dictionary<decimal, (decimal BaseImponible, decimal ImporteIva)>();

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                System.Diagnostics.Debug.WriteLine($"📊 Procesando {datosTicket.Rows.Count} productos...");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    foreach (DataRow row in datosTicket.Rows)
                    {
                        string codigo = row["codigo"]?.ToString();
                        if (string.IsNullOrEmpty(codigo)) continue;

                        string query = "SELECT iva FROM Productos WHERE codigo = @codigo";
                        using (var cmd = new SqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@codigo", codigo);
                            var result = await cmd.ExecuteScalarAsync();

                            decimal iva = result != null && decimal.TryParse(result.ToString(), out decimal ivaValue)
                                ? ivaValue
                                : 21.00m;

                            decimal subtotal = decimal.TryParse(row["total"]?.ToString(), out decimal total) ? total : 0;
                            decimal baseImponible = Math.Round(subtotal / (1 + (iva / 100m)), 2);
                            decimal importeIva = Math.Round(subtotal - baseImponible, 2);

                            var productoConIva = new ProductoConIva
                            {
                                Codigo = codigo,
                                Descripcion = row["descripcion"]?.ToString() ?? "",
                                Cantidad = int.TryParse(row["cantidad"]?.ToString(), out int cantidad) ? cantidad : 0,
                                Precio = decimal.TryParse(row["precio"]?.ToString(), out decimal precio) ? precio : 0,
                                Subtotal = subtotal,
                                AlicuotaIva = iva,
                                BaseImponible = baseImponible,
                                ImporteIva = importeIva
                            };

                            productosConIva.Add(productoConIva);

                            if (resumenIva.ContainsKey(iva))
                            {
                                var actual = resumenIva[iva];
                                resumenIva[iva] = (
                                    actual.BaseImponible + baseImponible,
                                    actual.ImporteIva + importeIva
                                );
                            }
                            else
                            {
                                resumenIva[iva] = (baseImponible, importeIva);
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Carga completada: {productosConIva.Count} productos");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando datos IVA: {ex.Message}");
                productosConIva = new List<ProductoConIva>();
                resumenIva = new Dictionary<decimal, (decimal BaseImponible, decimal ImporteIva)>();
            }
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (datosTicket == null || configuracion == null) return;

            if (modoCalculoAltura)
            {
                System.Diagnostics.Debug.WriteLine("📐 Modo CÁLCULO - Midiendo altura del contenido...");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("🖨️ Modo IMPRESIÓN - Renderizando contenido...");
            }

            Font fontNormal = new Font("Arial", 8);
            Font fontBold = new Font("Arial", 8, FontStyle.Bold);
            Font fontTitulo = new Font("Arial", 14, FontStyle.Bold);
            Font fontSubtitulo = new Font("Arial", 7);
            Pen linePen = new Pen(Color.Black, 1);

            float leftMargin = e.MarginBounds.Left;
            float topMargin = e.MarginBounds.Top;
            float rightMargin = e.MarginBounds.Right;
            float y = topMargin;
            float anchoTotal = rightMargin - leftMargin;

            // ✅ CRÍTICO: Asegurar una sola página
            e.HasMorePages = false;

            y = ImprimirFechaHora(e.Graphics, fontNormal, leftMargin, rightMargin, y);

            bool esFactura = (configuracion.TipoComprobante.Contains("Factura") ||
                             configuracion.TipoComprobante.Contains("FACTURA")) &&
                             !configuracion.TipoComprobante.Contains("REMITO") &&
                             !configuracion.TipoComprobante.ToUpper().Contains("REMITO");

            if (esFactura)
            {
                y = ImprimirDatosFacturacion(e.Graphics, fontTitulo, fontSubtitulo, leftMargin, rightMargin, y);
            }
            else
            {
                y = ImprimirInfoComercio(e.Graphics, fontTitulo, fontSubtitulo, leftMargin, rightMargin, y);
            }

            y = ImprimirTituloComprobante(e.Graphics, fontBold, leftMargin, rightMargin, y);

            bool esFacturaC = configuracion.TipoComprobante.Contains("FacturaC") ||
                              configuracion.TipoComprobante.Contains("FACTURA C");

            if (esFactura)
            {
                if (esFacturaC)
                {
                    y = ImprimirFacturaCSimple(e.Graphics, fontNormal, fontBold, leftMargin, rightMargin, anchoTotal, y);
                }
                else if (productosConIva != null && productosConIva.Count > 0)
                {
                    y = ImprimirFacturaConIvaFormatoControlFacturas(e.Graphics, fontNormal, fontBold, leftMargin, rightMargin, anchoTotal, y);
                }
                else
                {
                    y = ImprimirRemitoSinIva(e.Graphics, fontNormal, fontBold, leftMargin, rightMargin, anchoTotal, y, linePen);
                }
            }
            else
            {
                y = ImprimirRemitoSinIva(e.Graphics, fontNormal, fontBold, leftMargin, rightMargin, anchoTotal, y, linePen);
            }

            if (!string.IsNullOrEmpty(configuracion.MensajePie))
            {
                y = ImprimirPieTicket(e.Graphics, fontSubtitulo, leftMargin, rightMargin, y);
            }

            if (esFactura && !string.IsNullOrEmpty(configuracion.CAE))
            {
                y = ImprimirInformacionCAE(e.Graphics, fontSubtitulo, leftMargin, rightMargin, y);
            }

            // ✅ MODIFICADO: Guardar altura solo en modo cálculo
            if (modoCalculoAltura)
            {
                alturaContenidoCalculada = y - topMargin + 20; // +20 para margen inferior
                System.Diagnostics.Debug.WriteLine($"📏 Altura calculada: {alturaContenidoCalculada:F2} unidades ({alturaContenidoCalculada / 100 * 25.4:F1}mm)");
            }

            fontNormal.Dispose();
            fontBold.Dispose();
            fontTitulo.Dispose();
            fontSubtitulo.Dispose();
            linePen.Dispose();
        }

        // ✅ MODIFICADO: Método para ajustar el tamaño del papel al contenido real
        private void AjustarTamanoPapelAlContenido()
        {
            try
            {
                if (alturaContenidoCalculada <= 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ No se calculó altura, usando tamaño por defecto");
                    return;
                }

                // Convertir unidades de impresión (1/100 inch) a milímetros
                float alturaEnMm = (alturaContenidoCalculada / 100f) * 25.4f;

                // Redondear hacia arriba y agregar margen de seguridad
                int altoTicketAjustado = (int)Math.Ceiling((alturaEnMm + 5) / 25.4 * 100); // +5mm margen

                // Mantener el ancho existente
                int anchoTicket = printDocument.DefaultPageSettings.PaperSize.Width;

                // Crear nuevo tamaño de papel con altura ajustada
                PaperSize ticketSizeAjustado = new PaperSize("TicketDinamico", anchoTicket, altoTicketAjustado);
                printDocument.DefaultPageSettings.PaperSize = ticketSizeAjustado;

                System.Diagnostics.Debug.WriteLine($"[PAPEL] ✅ Papel ajustado dinámicamente:");
                System.Diagnostics.Debug.WriteLine($"   - Ancho: {anchoTicket / 100.0 * 25.4:F1}mm");
                System.Diagnostics.Debug.WriteLine($"   - Alto calculado: {alturaEnMm:F1}mm");
                System.Diagnostics.Debug.WriteLine($"   - Alto ajustado: {altoTicketAjustado / 100.0 * 25.4:F1}mm");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error ajustando tamaño de papel: {ex.Message}");
            }
        }

        // ✅ MODIFICADO: Resetear estado antes de cada impresión
        private void ResetearEstadoImpresion()
        {
            alturaContenidoCalculada = 0f;
            modoCalculoAltura = true;
        }

        private float ImprimirDatosFacturacion(Graphics graphics, Font fontTitulo, Font fontSubtitulo, float leftMargin, float rightMargin, float y)
        {
            float anchoUtil = rightMargin - leftMargin;

            SizeF nombreSize = graphics.MeasureString(configuracion.NombreComercio, fontTitulo);
            float nombreX = leftMargin + (anchoUtil - nombreSize.Width) / 2;
            graphics.DrawString(configuracion.NombreComercio, fontTitulo, Brushes.Black, nombreX, y);
            y += nombreSize.Height;

            // ✅ AGREGADO: Imprimir domicilio comercial si está disponible
            if (!string.IsNullOrEmpty(configuracion.DomicilioComercio))
            {
                SizeF domicilioSize = graphics.MeasureString(configuracion.DomicilioComercio, fontSubtitulo);
                float domicilioX = leftMargin + (anchoUtil - domicilioSize.Width) / 2;
                graphics.DrawString(configuracion.DomicilioComercio, fontSubtitulo, Brushes.Black, domicilioX, y);
                y += domicilioSize.Height;
            }

            y += 6;

            Font fontDatosFiscales = new Font("Arial", 6, FontStyle.Regular);

            if (!string.IsNullOrEmpty(datosFacturacion.RazonSocial))
            {
                string razonText = $"Razón Social: {datosFacturacion.RazonSocial}";
                graphics.DrawString(razonText, fontDatosFiscales, Brushes.Black, leftMargin, y);
                y += 10;
            }

            if (!string.IsNullOrEmpty(datosFacturacion.CUIT))
            {
                string cuitText = $"CUIT: {datosFacturacion.CUIT}";
                graphics.DrawString(cuitText, fontDatosFiscales, Brushes.Black, leftMargin, y);
                y += 10;
            }

            if (!string.IsNullOrEmpty(datosFacturacion.DomicilioFiscal))
            {
                string domicilioCompleto = $"Dom. Fiscal: {datosFacturacion.DomicilioFiscal}";
                if (!string.IsNullOrEmpty(datosFacturacion.CodigoPostal))
                {
                    domicilioCompleto += $" - CP: {datosFacturacion.CodigoPostal}";
                }

                graphics.DrawString(domicilioCompleto, fontDatosFiscales, Brushes.Black, leftMargin, y);
                y += 10;
            }

            if (!string.IsNullOrEmpty(datosFacturacion.IngBrutos))
            {
                string ingBrutosText = $"Ing. Brutos: {datosFacturacion.IngBrutos}";
                graphics.DrawString(ingBrutosText, fontDatosFiscales, Brushes.Black, leftMargin, y);
                y += 10;
            }

            if (!string.IsNullOrEmpty(datosFacturacion.Condicion))
            {
                string condicionText = $"Condición IVA: {datosFacturacion.Condicion}";
                graphics.DrawString(condicionText, fontDatosFiscales, Brushes.Black, leftMargin, y);
                y += 10;
            }

            if (!string.IsNullOrEmpty(datosFacturacion.InicioActividades))
            {
                string inicioText = $"Inicio Actividades: {datosFacturacion.InicioActividades}";
                graphics.DrawString(inicioText, fontDatosFiscales, Brushes.Black, leftMargin, y);
                y += 10;
            }

            fontDatosFiscales.Dispose();

            return y + 8;
        }

        // ✅ NUEVO: Método para Factura C (sin IVA discriminado)
        private float ImprimirFacturaCSimple(Graphics graphics, Font fontNormal, Font fontBold, float leftMargin, float rightMargin, float anchoTotal, float y)
        {
            System.Diagnostics.Debug.WriteLine("🎯 Ejecutando ImprimirFacturaCSimple - FACTURA C SIN DISCRIMINAR IVA");
            Pen linePen = new Pen(Color.Black, 1);

            float colDescripcion = anchoTotal * 0.50f;
            float colPrecioUnit = anchoTotal * 0.20f;
            float colCantidad = anchoTotal * 0.10f;
            float colTotal = anchoTotal * 0.20f;

            float[] colX = {
                leftMargin,
                leftMargin + colDescripcion,
                leftMargin + colDescripcion + colPrecioUnit,
                leftMargin + colDescripcion + colPrecioUnit + colCantidad
            };

            float tablaRight = leftMargin + anchoTotal;

            string[] headers = { "PRODUCTO", "PRECIO UNIT.", "C", "TOTAL" };

            for (int i = 0; i < headers.Length; i++)
            {
                float headerX = colX[i];
                SizeF headerSize = graphics.MeasureString(headers[i], fontBold);

                switch (i)
                {
                    case 0:
                        break;
                    case 1:
                        headerX += (colPrecioUnit - headerSize.Width) / 2;
                        break;
                    case 2:
                        headerX += (colCantidad - headerSize.Width) / 2;
                        break;
                    case 3:
                        headerX += (colTotal - headerSize.Width) / 2;
                        break;
                }

                graphics.DrawString(headers[i], fontBold, Brushes.Black, headerX, y);
            }

            y += 16;
            graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 4;

            int cantidadTotal = 0;
            decimal sumaTotal = 0;
            float rowHeight = 16;

            foreach (DataRow row in datosTicket.Rows)
            {
                float filaYInicial = y;

                string descripcion = row["descripcion"]?.ToString() ?? "";
                float anchoDisponibleDescripcion = colDescripcion - 4;

                List<string> lineasDescripcion = DividirTextoEnLineasMejorado(graphics, descripcion, fontNormal, anchoDisponibleDescripcion);

                decimal precio = decimal.TryParse(row["precio"]?.ToString(), out decimal p) ? p : 0;
                string precioStr = precio.ToString("C2");
                SizeF precioSize = graphics.MeasureString(precioStr, fontNormal);
                float precioX = colX[1] + (colPrecioUnit - precioSize.Width) / 2;

                int cantidad = int.TryParse(row["cantidad"]?.ToString(), out int c) ? c : 0;
                cantidadTotal += cantidad;
                string cantidadStr = cantidad.ToString();
                SizeF cantidadSize = graphics.MeasureString(cantidadStr, fontNormal);
                float cantidadX = colX[2] + (colCantidad - cantidadSize.Width) / 2;

                decimal total = decimal.TryParse(row["total"]?.ToString(), out decimal t) ? t : 0;
                sumaTotal += total;
                string totalStr = total.ToString("C2");
                SizeF totalSize = graphics.MeasureString(totalStr, fontNormal);
                float totalX = colX[3] + (colTotal - totalSize.Width) / 2;

                float filaY = filaYInicial;
                for (int i = 0; i < lineasDescripcion.Count; i++)
                {
                    if (i == 0)
                    {
                        graphics.DrawString(precioStr, fontNormal, Brushes.Black, precioX, filaY);
                        graphics.DrawString(cantidadStr, fontNormal, Brushes.Black, cantidadX, filaY);
                        graphics.DrawString(totalStr, fontNormal, Brushes.Black, totalX, filaY);
                    }

                    graphics.DrawString(lineasDescripcion[i], fontNormal, Brushes.Black, colX[0], filaY);
                    filaY += rowHeight;
                }

                y = filaY;
            }

            y += 4;
            graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 6;

            y = ImprimirTotalesFacturaC(graphics, fontBold, leftMargin, rightMargin, y, cantidadTotal, sumaTotal);

            linePen.Dispose();
            return y;
        }

        private float ImprimirTotalesFacturaC(Graphics graphics, Font fontBold, float leftMargin, float rightMargin, float y, int cantidadTotal, decimal sumaTotal)
        {
            float anchoUtil = rightMargin - leftMargin;

            string productosStr = $"PRODUCTOS: {cantidadTotal}";
            graphics.DrawString(productosStr, fontBold, Brushes.Black, leftMargin, y);

            // ✅ MODIFICADO: Mostrar subtotal
            string subtotalStr = $"SUBTOTAL: {sumaTotal:C2}";
            SizeF subtotalSize = graphics.MeasureString(subtotalStr, fontBold);
            float subtotalX = rightMargin - subtotalSize.Width;
            graphics.DrawString(subtotalStr, fontBold, Brushes.Black, subtotalX, y);
            y += subtotalSize.Height + 4;

            // ✅ MODIFICADO: Mostrar descuento si existe
            if (configuracion != null && configuracion.PorcentajeDescuento > 0)
            {
                string descuentoStr = $"DESCUENTO ({configuracion.PorcentajeDescuento:N2}%): -{configuracion.ImporteDescuento:C2}";
                SizeF descuentoSize = graphics.MeasureString(descuentoStr, fontBold);
                float descuentoX = rightMargin - descuentoSize.Width;

                graphics.DrawString(descuentoStr, fontBold, Brushes.Black, descuentoX, y);
                y += descuentoSize.Height + 4;
            }

            // ✅ MODIFICADO: TOTAL FINAL con validación mejorada
            decimal totalFinal;

            if (configuracion != null && configuracion.ImporteFinal > 0)
            {
                totalFinal = configuracion.ImporteFinal;
            }
            else if (configuracion != null && configuracion.PorcentajeDescuento > 0 && configuracion.ImporteDescuento > 0)
            {
                totalFinal = sumaTotal - configuracion.ImporteDescuento;
            }
            else
            {
                totalFinal = sumaTotal;
            }

            string totalStr = $"TOTAL: {totalFinal:C2}";
            Font fontTotal = new Font(fontBold.FontFamily, fontBold.Size + 2, FontStyle.Bold);
            SizeF totalSize = graphics.MeasureString(totalStr, fontTotal);
            float totalX = rightMargin - totalSize.Width;

            graphics.DrawString(totalStr, fontTotal, Brushes.Black, totalX, y);
            fontTotal.Dispose();
            y += totalSize.Height + 6;

            Font fontNota = new Font("Arial", 6, FontStyle.Italic);
            string notaIva = "IVA incluido - Monotributo - No discriminado";
            SizeF notaSize = graphics.MeasureString(notaIva, fontNota);
            float notaX = leftMargin + (anchoUtil - notaSize.Width) / 2;

            graphics.DrawString(notaIva, fontNota, Brushes.Gray, notaX, y);
            y += notaSize.Height + 8;

            fontNota.Dispose();

            return y;
        }

        // ✅ Método para Factura A/B (con IVA discriminado)
        private float ImprimirFacturaConIvaFormatoControlFacturas(Graphics graphics, Font fontNormal, Font fontBold, float leftMargin, float rightMargin, float anchoTotal, float y)
        {
            Pen linePen = new Pen(Color.Black, 1);

            float colDescripcion = anchoTotal * 0.50f;
            float colPrecioUnit = anchoTotal * 0.20f;
            float colCantidad = anchoTotal * 0.10f;
            float colTotal = anchoTotal * 0.20f;

            float[] colX = {
                leftMargin,
                leftMargin + colDescripcion,
                leftMargin + colDescripcion + colPrecioUnit,
                leftMargin + colDescripcion + colPrecioUnit + colCantidad
            };

            float tablaRight = leftMargin + anchoTotal;

            string[] headers = { "PRODUCTO", "PRECIO UNIT.", "C", "TOTAL" };

            for (int i = 0; i < headers.Length; i++)
            {
                float headerX = colX[i];
                SizeF headerSize = graphics.MeasureString(headers[i], fontBold);

                switch (i)
                {
                    case 0:
                        break;
                    case 1:
                        headerX += (colPrecioUnit - headerSize.Width) / 2;
                        break;
                    case 2:
                        headerX += (colCantidad - headerSize.Width) / 2;
                        break;
                    case 3:
                        headerX += (colTotal - headerSize.Width) / 2;
                        break;
                }

                graphics.DrawString(headers[i], fontBold, Brushes.Black, headerX, y);
            }

            y += 16;
            graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 4;

            int cantidadTotal = 0;
            decimal sumaTotal = 0;
            float rowHeight = 16;

            Font fontIva = new Font("Arial", 6, FontStyle.Regular);
            Brush ivaGrayBrush = new SolidBrush(Color.FromArgb(100, 100, 100));

            foreach (var producto in productosConIva)
            {
                float filaYInicial = y;

                string descripcionCompleta = producto.Descripcion;
                string textoIva = producto.AlicuotaIva > 0 ? $"({producto.AlicuotaIva:N1}%)" : "";

                float anchoDisponibleDescripcion = colDescripcion - 4;

                SizeF ivaSize = SizeF.Empty;
                if (!string.IsNullOrEmpty(textoIva))
                {
                    ivaSize = graphics.MeasureString(textoIva, fontIva);
                    anchoDisponibleDescripcion -= ivaSize.Width + 8;
                }

                List<string> lineasDescripcion = DividirTextoEnLineasMejorado(graphics, descripcionCompleta, fontNormal, anchoDisponibleDescripcion);

                string precioStr = producto.Precio.ToString("C2");
                SizeF precioSize = graphics.MeasureString(precioStr, fontNormal);
                float precioX = colX[1] + (colPrecioUnit - precioSize.Width) / 2;

                string cantidadStr = producto.Cantidad.ToString();
                cantidadTotal += producto.Cantidad;
                SizeF cantidadSize = graphics.MeasureString(cantidadStr, fontNormal);
                float cantidadX = colX[2] + (colCantidad - cantidadSize.Width) / 2;

                decimal total = producto.Subtotal;
                sumaTotal += total;
                string totalStr = total.ToString("C2");
                SizeF totalSize = graphics.MeasureString(totalStr, fontNormal);
                float totalX = colX[3] + (colTotal - totalSize.Width) / 2;

                float filaY = filaYInicial;
                for (int i = 0; i < lineasDescripcion.Count; i++)
                {
                    if (i == 0)
                    {
                        graphics.DrawString(precioStr, fontNormal, Brushes.Black, precioX, filaY);
                        graphics.DrawString(cantidadStr, fontNormal, Brushes.Black, cantidadX, filaY);
                        graphics.DrawString(totalStr, fontNormal, Brushes.Black, totalX, filaY);
                    }

                    graphics.DrawString(lineasDescripcion[i], fontNormal, Brushes.Black, colX[0], filaY);
                    filaY += rowHeight;
                }

                if (!string.IsNullOrEmpty(textoIva))
                {
                    float ivaX = leftMargin + colDescripcion - ivaSize.Width - 2;
                    graphics.DrawString(textoIva, fontIva, ivaGrayBrush, ivaX, filaYInicial + 1);
                }

                y = filaY;
            }

            fontIva.Dispose();
            ivaGrayBrush.Dispose();

            y += 4;
            graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 6;

            y = ImprimirTotalesBasicosFormatoFactura(graphics, fontBold, leftMargin, rightMargin, y, cantidadTotal, sumaTotal);

            if (resumenIva != null && resumenIva.Count > 0)
            {
                y = ImprimirResumenIvaFormatoFactura(graphics, fontBold, fontNormal, leftMargin, rightMargin, y);
            }

            linePen.Dispose();
            return y;
        }

        private float ImprimirTotalesBasicosFormatoFactura(Graphics graphics, Font fontBold, float leftMargin, float rightMargin, float y, int cantidadTotal, decimal sumaTotal)
        {
            float anchoUtil = rightMargin - leftMargin;

            string productosStr = $"PRODUCTOS: {cantidadTotal}";
            graphics.DrawString(productosStr, fontBold, Brushes.Black, leftMargin, y);

            // ✅ MODIFICADO: Mostrar subtotal antes del descuento
            string subtotalStr = $"SUBTOTAL: {sumaTotal:C2}";
            SizeF subtotalSize = graphics.MeasureString(subtotalStr, fontBold);
            float subtotalX = rightMargin - subtotalSize.Width;
            graphics.DrawString(subtotalStr, fontBold, Brushes.Black, subtotalX, y);
            y += subtotalSize.Height + 4;

            // ✅ MODIFICADO: Mostrar descuento si existe
            if (configuracion != null && configuracion.PorcentajeDescuento > 0)
            {
                string descuentoStr = $"DESCUENTO ({configuracion.PorcentajeDescuento:N2}%): -{configuracion.ImporteDescuento:C2}";
                SizeF descuentoSize = graphics.MeasureString(descuentoStr, fontBold);
                float descuentoX = rightMargin - descuentoSize.Width;

                graphics.DrawString(descuentoStr, fontBold, Brushes.Black, descuentoX, y);
                y += descuentoSize.Height + 4;
            }

            // ✅ MODIFICADO: Mostrar TOTAL FINAL con validación mejorada
            decimal totalFinal;

            if (configuracion != null && configuracion.ImporteFinal > 0)
            {
                totalFinal = configuracion.ImporteFinal;
            }
            else if (configuracion != null && configuracion.PorcentajeDescuento > 0 && configuracion.ImporteDescuento > 0)
            {
                totalFinal = sumaTotal - configuracion.ImporteDescuento;
            }
            else
            {
                totalFinal = sumaTotal;
            }

            string totalFinalStr = $"TOTAL: {totalFinal:C2}";
            Font fontTotal = new Font(fontBold.FontFamily, fontBold.Size + 2, FontStyle.Bold);
            SizeF totalSize = graphics.MeasureString(totalFinalStr, fontTotal);
            float totalX = rightMargin - totalSize.Width;

            graphics.DrawString(totalFinalStr, fontTotal, Brushes.Black, totalX, y);
            fontTotal.Dispose();

            return y + totalSize.Height + 8;
        }

        private float ImprimirResumenIvaFormatoFactura(Graphics graphics, Font fontBold, Font fontNormal, float leftMargin, float rightMargin, float y)
        {
            y += 8;

            string tituloResumen = "=== RESUMEN IVA ===";
            SizeF tituloSize = graphics.MeasureString(tituloResumen, fontBold);
            float tituloX = leftMargin + ((rightMargin - leftMargin - tituloSize.Width) / 2);
            graphics.DrawString(tituloResumen, fontBold, Brushes.Black, tituloX, y);
            y += tituloSize.Height + 4;

            Font fontIvaDetalle = new Font("Arial", 7, FontStyle.Regular);
            Font fontIvaDetalleBold = new Font("Arial", 7, FontStyle.Bold);

            decimal totalBaseImponible = 0;
            decimal totalIva = 0;

            foreach (var kvp in resumenIva.OrderByDescending(x => x.Key))
            {
                decimal alicuota = kvp.Key;
                decimal baseImponible = kvp.Value.BaseImponible;
                decimal importeIva = kvp.Value.ImporteIva;

                totalBaseImponible += baseImponible;
                totalIva += importeIva;

                string lineaIva = $"IVA {alicuota:N1}%: Base {baseImponible:C2} - IVA {importeIva:C2}";
                graphics.DrawString(lineaIva, fontIvaDetalle, Brushes.Black, leftMargin, y);
                y += 14;
            }

            Pen linePen = new Pen(Color.Black, 1);
            graphics.DrawLine(linePen, leftMargin, y, rightMargin, y);
            y += 4;

            string lineaTotalBase = $"TOTAL BASE IMPONIBLE: {totalBaseImponible:C2}";
            graphics.DrawString(lineaTotalBase, fontIvaDetalleBold, Brushes.Black, leftMargin, y);
            y += 16;

            string lineaTotalIva = $"TOTAL IVA: {totalIva:C2}";
            graphics.DrawString(lineaTotalIva, fontIvaDetalleBold, Brushes.Black, leftMargin, y);
            y += 16;

            linePen.Dispose();
            fontIvaDetalle.Dispose();
            fontIvaDetalleBold.Dispose();

            return y;
        }

        private float ImprimirRemitoSinIva(Graphics graphics, Font fontNormal, Font fontBold, float leftMargin, float rightMargin, float anchoTotal, float y, Pen linePen)
        {
            float colDescripcion = anchoTotal * 0.50f;
            float colPrecio = anchoTotal * 0.20f;
            float colCantidad = anchoTotal * 0.10f;
            float colTotal = anchoTotal * 0.20f;

            float[] colX = {
                leftMargin,
                leftMargin + colDescripcion,
                leftMargin + colDescripcion + colPrecio,
                leftMargin + colDescripcion + colPrecio + colCantidad
            };

            float tablaRight = leftMargin + anchoTotal;

            string[] headers = { "DESCRIPCIÓN", "PRECIO", "C", "TOTAL" };

            for (int i = 0; i < headers.Length; i++)
            {
                float headerX = colX[i];
                SizeF headerSize = graphics.MeasureString(headers[i], fontBold);

                switch (i)
                {
                    case 0:
                        break;
                    case 1:
                        headerX += colPrecio - headerSize.Width - 2;
                        break;
                    case 2:
                        headerX += (colCantidad - headerSize.Width) / 2;
                        break;
                    case 3:
                        headerX += colTotal - headerSize.Width - 2;
                        break;
                }

                graphics.DrawString(headers[i], fontBold, Brushes.Black, headerX, y);
            }

            y += 16;
            graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 4;

            var resultado = ImprimirDetalleProductosNuevoOrden(graphics, fontNormal, colX, new float[] { colDescripcion, colPrecio, colCantidad, colTotal }, y);
            y = resultado.y;
            int cantidadTotal = resultado.cantidadTotal;
            decimal sumaTotal = resultado.sumaTotal;

            y += 4;
            graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 6;

            y = ImprimirTotalesBasicos(graphics, fontBold, leftMargin, rightMargin, y, cantidadTotal, sumaTotal);

            return y;
        }

        private (float y, int cantidadTotal, decimal sumaTotal) ImprimirDetalleProductosNuevoOrden(Graphics graphics, Font font, float[] colX, float[] colWidths, float y)
        {
            int cantidadTotal = 0;
            decimal sumaTotal = 0;
            float rowHeight = 16;

            foreach (DataRow row in datosTicket.Rows)
            {
                float filaYInicial = y;

                string descripcion = row["descripcion"].ToString();
                float maxDescripcionWidth = colWidths[0] - 4;
                List<string> lineasDescripcion = DividirTextoEnLineasMejorado(graphics, descripcion, font, maxDescripcionWidth);

                decimal precio = Convert.ToDecimal(row["precio"]);
                string precioStr = precio.ToString("C2");
                SizeF precioSize = graphics.MeasureString(precioStr, font);
                float precioX = colX[1] + colWidths[1] - precioSize.Width - 2;

                string cantidadStr = row["cantidad"].ToString();
                if (int.TryParse(cantidadStr, out int cantVal))
                    cantidadTotal += cantVal;

                SizeF cantidadSize = graphics.MeasureString(cantidadStr, font);
                float cantidadX = colX[2] + (colWidths[2] - cantidadSize.Width) / 2;

                decimal total = Convert.ToDecimal(row["total"]);
                sumaTotal += total;
                string totalStr = total.ToString("C2");
                SizeF totalSize = graphics.MeasureString(totalStr, font);
                float totalX = colX[3] + colWidths[3] - totalSize.Width - 2;

                float filaY = filaYInicial;
                for (int i = 0; i < lineasDescripcion.Count; i++)
                {
                    if (i == 0)
                    {
                        graphics.DrawString(precioStr, font, Brushes.Black, precioX, filaY);

                        float cantidadYLinea = filaY + ((rowHeight - cantidadSize.Height) / 2);
                        graphics.DrawString(cantidadStr, font, Brushes.Black, cantidadX, cantidadYLinea);

                        graphics.DrawString(totalStr, font, Brushes.Black, totalX, filaY);
                    }

                    graphics.DrawString(lineasDescripcion[i], font, Brushes.Black, colX[0], filaY);
                    filaY += rowHeight;
                }

                y = filaY;
            }

            return (y, cantidadTotal, sumaTotal);
        }

        private float ImprimirFechaHora(Graphics graphics, Font font, float leftMargin, float rightMargin, float y)
        {
            string fechaStr = $"Fecha: {DateTime.Now:dd/MM/yyyy}";
            string horaStr = $"Hora: {DateTime.Now:HH:mm}";

            SizeF fechaSize = graphics.MeasureString(fechaStr, font);
            SizeF horaSize = graphics.MeasureString(horaStr, font);

            float fechaX = rightMargin - fechaSize.Width;
            float horaX = rightMargin - horaSize.Width;

            graphics.DrawString(fechaStr, font, Brushes.Black, fechaX, y);
            graphics.DrawString(horaStr, font, Brushes.Black, horaX, y + fechaSize.Height);

            return y + fechaSize.Height + horaSize.Height + 10;
        }

        private float ImprimirInfoComercio(Graphics graphics, Font fontTitulo, Font fontSubtitulo, float leftMargin, float rightMargin, float y)
        {
            float anchoUtil = rightMargin - leftMargin;

            SizeF nombreSize = graphics.MeasureString(configuracion.NombreComercio, fontTitulo);
            float nombreX = leftMargin + (anchoUtil - nombreSize.Width) / 2;
            graphics.DrawString(configuracion.NombreComercio, fontTitulo, Brushes.Black, nombreX, y);
            y += nombreSize.Height;

            if (!string.IsNullOrEmpty(configuracion.DomicilioComercio))
            {
                SizeF domicilioSize = graphics.MeasureString(configuracion.DomicilioComercio, fontSubtitulo);
                float domicilioX = leftMargin + (anchoUtil - domicilioSize.Width) / 2;
                graphics.DrawString(configuracion.DomicilioComercio, fontSubtitulo, Brushes.Black, domicilioX, y);
                y += domicilioSize.Height;
            }

            return y + 6;
        }

        private float ImprimirTituloComprobante(Graphics graphics, Font fontBold, float leftMargin, float rightMargin, float y)
        {
            float anchoUtil = rightMargin - leftMargin;

            string titulo = FormatearNumeroParaImpresion(configuracion.NumeroComprobante, configuracion.TipoComprobante);

            SizeF tituloSize = graphics.MeasureString(titulo, fontBold);
            float tituloX = leftMargin + (anchoUtil - tituloSize.Width) / 2;
            graphics.DrawString(titulo, fontBold, Brushes.Black, tituloX, y);
            return y + tituloSize.Height + 8;
        }

        private string FormatearNumeroParaImpresion(string numeroCompleto, string tipoComprobante)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 FormatearNumeroParaImpresion:");
                System.Diagnostics.Debug.WriteLine($"   - Número completo recibido: '{numeroCompleto}'");
                System.Diagnostics.Debug.WriteLine($"   - Tipo comprobante: '{tipoComprobante}'");

                // ✅ NUEVO: Detectar formato legado 'B 0002-00000002' o 'A 0002-00000002'
                if (numeroCompleto.Contains(" ") && numeroCompleto.Contains("-"))
                {
                    var partes = numeroCompleto.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);

                    if (partes.Length >= 2)
                    {
                        string letraFactura = partes[0]; // "A", "B", o "C"

                        // ✅ CRÍTICO: El segundo valor es el NÚMERO, NO el punto de venta
                        // Necesitamos obtener el punto de venta del JSON
                        int puntoVentaJson = ObtenerPuntoVentaDesdeConfiguracion();

                        if (int.TryParse(partes[partes.Length - 1], out int numeroFactura))
                        {
                            System.Diagnostics.Debug.WriteLine($"   ⚠️ Formato legado detectado: '{numeroCompleto}'");
                            System.Diagnostics.Debug.WriteLine($"   ✅ Usando punto de venta del JSON: {puntoVentaJson}");
                            System.Diagnostics.Debug.WriteLine($"   ✅ Número factura: {numeroFactura}");

                            return $"FACTURA {letraFactura} N° {puntoVentaJson:D4}-{numeroFactura:D8}";
                        }
                    }
                }

                if (tipoComprobante.Contains("REMITO") || tipoComprobante.Contains("Remito"))
                {
                    if (numeroCompleto.Contains("Remito N°"))
                    {
                        return numeroCompleto;
                    }
                    else if (numeroCompleto.Contains("-"))
                    {
                        string[] partes = numeroCompleto.Split('-');
                        if (partes.Length >= 3 && int.TryParse(partes[2], out int numeroRemito))
                        {
                            return $"REMITO N° {numeroRemito}";
                        }
                    }
                    return $"REMITO N° {numeroCompleto}";
                }

                if (tipoComprobante.Contains("Factura") || tipoComprobante.Contains("FACTURA"))
                {
                    // ✅ FORMATO CORRECTO: "0006-0007-00000002"
                    if (numeroCompleto.Contains("-") && numeroCompleto.Length >= 19)
                    {
                        string[] partes = numeroCompleto.Split('-');
                        if (partes.Length == 3)
                        {
                            string tipoFormateado = "FACTURA";

                            // Determinar tipo de factura desde el primer segmento
                            if (partes[0].StartsWith("0001") || partes[0].StartsWith("1"))
                            {
                                tipoFormateado = "FACTURA A";
                            }
                            else if (partes[0].StartsWith("0006") || partes[0].StartsWith("6"))
                            {
                                tipoFormateado = "FACTURA B";
                            }
                            else if (partes[0].StartsWith("0011") || partes[0].StartsWith("11"))
                            {
                                tipoFormateado = "FACTURA C";
                            }

                            // ✅ USAR el punto de venta del segundo segmento TAL CUAL VIENE
                            string puntoVentaFormateado = partes[1];

                            System.Diagnostics.Debug.WriteLine($"   ✅ Punto de venta extraído: '{puntoVentaFormateado}'");
                            System.Diagnostics.Debug.WriteLine($"   ✅ Número formateado: {tipoFormateado} N° {puntoVentaFormateado}-{partes[2]}");

                            return $"{tipoFormateado} N° {puntoVentaFormateado}-{partes[2]}";
                        }
                    }

                    // ✅ FALLBACK: Si el número no tiene el formato esperado
                    if (int.TryParse(numeroCompleto, out int numeroSimple))
                    {
                        string letra = "";
                        if (tipoComprobante.ToUpper().Contains("FACTURAA") || tipoComprobante.ToUpper().Contains("FACTURA A"))
                        {
                            letra = "A";
                        }
                        else if (tipoComprobante.ToUpper().Contains("FACTURAB") || tipoComprobante.ToUpper().Contains("FACTURA B"))
                        {
                            letra = "B";
                        }
                        else if (tipoComprobante.ToUpper().Contains("FACTURAC") || tipoComprobante.ToUpper().Contains("FACTURA C"))
                        {
                            letra = "C";
                        }
                        else
                        {
                            letra = "B";
                        }

                        int puntoVentaJson = ObtenerPuntoVentaDesdeConfiguracion();

                        System.Diagnostics.Debug.WriteLine($"   ⚠️ Número simple detectado, usando punto de venta del JSON: {puntoVentaJson}");

                        return $"FACTURA {letra} N° {puntoVentaJson:D4}-{numeroSimple:D8}";
                    }

                    // Si viene con formato pero no es el esperado
                    if (tipoComprobante.ToUpper().Contains("FACTURA A"))
                    {
                        return $"FACTURA A N° {numeroCompleto}";
                    }
                    else if (tipoComprobante.ToUpper().Contains("FACTURA B"))
                    {
                        return $"FACTURA B N° {numeroCompleto}";
                    }
                    else if (tipoComprobante.ToUpper().Contains("FACTURA C"))
                    {
                        return $"FACTURA C N° {numeroCompleto}";
                    }
                    else
                    {
                        return $"FACTURA N° {numeroCompleto}";
                    }
                }

                return numeroCompleto;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error formateando número de comprobante: {ex.Message}");
                return numeroCompleto;
            }
        }

        // ✅ NUEVO: Método helper para obtener punto de venta desde configuración
        private int ObtenerPuntoVentaDesdeConfiguracion()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string ambienteActivo = config["AFIP:AmbienteActivo"] ?? "Testing";
                int puntoVenta = int.Parse(config[$"AFIP:{ambienteActivo}:PuntoVenta"] ?? "1");

                System.Diagnostics.Debug.WriteLine($"📋 Punto de venta desde JSON ({ambienteActivo}): {puntoVenta}");

                return puntoVenta;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error leyendo punto de venta del JSON: {ex.Message}");
                return 1; // Valor por defecto
            }
        }

        private float ImprimirTotalesBasicos(Graphics graphics, Font fontBold, float leftMargin, float rightMargin, float y, int cantidadTotal, decimal sumaTotal)
        {
            float anchoUtil = rightMargin - leftMargin;

            string cantidadTotalStr = $"PRODUCTOS: {cantidadTotal}";
            graphics.DrawString(cantidadTotalStr, fontBold, Brushes.Black, leftMargin, y);

            // ✅ MODIFICADO: Mostrar subtotal
            string subtotalStr = $"SUBTOTAL: {sumaTotal:C2}";
            SizeF subtotalSize = graphics.MeasureString(subtotalStr, fontBold);
            float subtotalX = rightMargin - subtotalSize.Width;
            graphics.DrawString(subtotalStr, fontBold, Brushes.Black, subtotalX, y);
            y += subtotalSize.Height + 4;

            // ✅ MODIFICADO: Mostrar descuento si existe
            if (configuracion != null && configuracion.PorcentajeDescuento > 0)
            {
                string descuentoStr = $"DESCUENTO ({configuracion.PorcentajeDescuento:N2}%): -{configuracion.ImporteDescuento:C2}";
                SizeF descuentoSize = graphics.MeasureString(descuentoStr, fontBold);
                float descuentoX = rightMargin - descuentoSize.Width;

                graphics.DrawString(descuentoStr, fontBold, Brushes.Black, descuentoX, y);
                y += descuentoSize.Height + 4;
            }

            // ✅ MODIFICADO: TOTAL FINAL con validación mejorada
            decimal totalFinal;

            if (configuracion != null && configuracion.ImporteFinal > 0)
            {
                totalFinal = configuracion.ImporteFinal;
            }
            else if (configuracion != null && configuracion.PorcentajeDescuento > 0 && configuracion.ImporteDescuento > 0)
            {
                totalFinal = sumaTotal - configuracion.ImporteDescuento;
            }
            else
            {
                totalFinal = sumaTotal;
            }

            string totalGeneralStr = $"TOTAL: {totalFinal:C2}";
            Font fontTotal = new Font(fontBold.FontFamily, fontBold.Size + 2, FontStyle.Bold);
            SizeF totalGeneralSize = graphics.MeasureString(totalGeneralStr, fontTotal);
            float totalGeneralX = rightMargin - totalGeneralSize.Width;

            graphics.DrawString(totalGeneralStr, fontTotal, Brushes.Black, totalGeneralX, y);
            fontTotal.Dispose();

            return y + totalGeneralSize.Height + 8;
        }


        private float ImprimirPieTicket(Graphics graphics, Font fontSubtitulo, float leftMargin, float rightMargin, float y)
        {
            float anchoUtil = rightMargin - leftMargin;
            var lineasPie = DividirTextoEnLineasMejorado(graphics, configuracion.MensajePie, fontSubtitulo, anchoUtil);

            foreach (string linea in lineasPie)
            {
                SizeF lineaSize = graphics.MeasureString(linea, fontSubtitulo);
                float lineaX = leftMargin + (anchoUtil - lineaSize.Width) / 2;
                graphics.DrawString(linea, fontSubtitulo, Brushes.Black, lineaX, y);
                y += lineaSize.Height;
            }

            return y;
        }

        private float ImprimirInformacionCAE(Graphics graphics, Font fontSubtitulo, float leftMargin, float rightMargin, float y)
        {
            y += 8;

            if (!string.IsNullOrEmpty(configuracion.CAE))
            {
                string caeInfo = $"CAE: {configuracion.CAE}";
                graphics.DrawString(caeInfo, fontSubtitulo, Brushes.Black, leftMargin, y);
                y += 12;

                if (configuracion.CAEVencimiento.HasValue)
                {
                    string vencInfo = $"Vencimiento: {configuracion.CAEVencimiento.Value:dd/MM/yyyy}";
                    graphics.DrawString(vencInfo, fontSubtitulo, Brushes.Black, leftMargin, y);
                    y += 12;
                }
            }

            if (!string.IsNullOrEmpty(configuracion.CUIT))
            {
                string cuitInfo = $"CUIT Cliente: {configuracion.CUIT}";
                graphics.DrawString(cuitInfo, fontSubtitulo, Brushes.Black, leftMargin, y);
                y += 12;
            }

            if (!string.IsNullOrEmpty(configuracion.FormaPago))
            {
                string pagoInfo = $"Forma de Pago: {configuracion.FormaPago}";
                graphics.DrawString(pagoInfo, fontSubtitulo, Brushes.Black, leftMargin, y);
                y += 12;
            }

            return y;
        }

        private List<string> DividirTextoEnLineasMejorado(Graphics graphics, string texto, Font font, float anchoMaximo)
        {
            var lineas = new List<string>();
            if (string.IsNullOrEmpty(texto)) return lineas;

            string[] palabras = texto.Split(' ');
            string lineaActual = "";

            foreach (string palabraOriginal in palabras)
            {
                string palabra = palabraOriginal;
                string pruebaLinea = string.IsNullOrEmpty(lineaActual) ? palabra : lineaActual + " " + palabra;
                SizeF size = graphics.MeasureString(pruebaLinea, font);

                if (size.Width <= anchoMaximo)
                {
                    lineaActual = pruebaLinea;
                }
                else
                {
                    if (!string.IsNullOrEmpty(lineaActual))
                    {
                        lineas.Add(lineaActual);
                        lineaActual = palabra;
                    }
                    else
                    {
                        while (!string.IsNullOrEmpty(palabra))
                        {
                            int maxChars = palabra.Length;
                            string subCadena = palabra;

                            while (maxChars > 0)
                            {
                                subCadena = palabra.Substring(0, maxChars);
                                SizeF subSize = graphics.MeasureString(subCadena, font);

                                if (subSize.Width <= anchoMaximo)
                                    break;
                                maxChars--;
                            }

                            if (maxChars > 0)
                            {
                                lineas.Add(subCadena);
                                palabra = palabra.Substring(maxChars);
                            }
                            else
                            {
                                lineas.Add(palabra.Substring(0, 1));
                                palabra = palabra.Substring(1);
                            }
                        }
                        lineaActual = "";
                    }
                }
            }

            if (!string.IsNullOrEmpty(lineaActual))
                lineas.Add(lineaActual);

            return lineas;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    printDocument?.Dispose();
                }
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private class ProductoConIva
        {
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public int Cantidad { get; set; }
            public decimal Precio { get; set; }
            public decimal Subtotal { get; set; }
            public decimal AlicuotaIva { get; set; }
            public decimal BaseImponible { get; set; }
            public decimal ImporteIva { get; set; }
        }
    }

    public class DatosFacturacion
    {
        public string RazonSocial { get; set; } = "";
        public string CUIT { get; set; } = "";
        public string IngBrutos { get; set; } = "";
        public string DomicilioFiscal { get; set; } = "";
        public string CodigoPostal { get; set; } = "";
        public string InicioActividades { get; set; } = "";
        public string Condicion { get; set; } = "";
    }

    public class TicketConfig
    {
        public string NombreComercio { get; set; } = "Mi Comercio";
        public string DomicilioComercio { get; set; } = "";
        public string TipoComprobante { get; set; } = "REMITO";
        public string NumeroComprobante { get; set; } = "1";
        public string MensajePie { get; set; } = "";
        public string FormaPago { get; set; } = "";
        public string CAE { get; set; } = "";
        public DateTime? CAEVencimiento { get; set; }
        public string CUIT { get; set; } = "";

        // ✅ NUEVO: Propiedades para descuento
        public decimal PorcentajeDescuento { get; set; }
        public decimal ImporteDescuento { get; set; }
        public decimal ImporteFinal { get; set; }
    }
}