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
            int anchoTicket = (int)(80 / 25.4 * 100);
            int altoTicket = (int)(200 / 25.4 * 100);

            PaperSize ticketSize = new PaperSize("Ticket", anchoTicket, altoTicket);
            printDocument.DefaultPageSettings.PaperSize = ticketSize;
            printDocument.DefaultPageSettings.Margins = new Margins(2, 2, 5, 5);
        }

        public async Task ImprimirTicket(DataTable datos, TicketConfig config)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(TicketPrintingService));

            datosTicket = datos;
            configuracion = config;

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

            System.Diagnostics.Debug.WriteLine("🖨️ Iniciando impresión...");
            System.Diagnostics.Debug.WriteLine($"📄 Tipo comprobante: {configuracion.TipoComprobante}");

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

            y = ImprimirFechaHora(e.Graphics, fontNormal, leftMargin, rightMargin, y);

            // ✅ CORREGIDO: Validación estricta del tipo de comprobante
            // Un REMITO nunca debe mostrar datos fiscales, incluso si tiene CAE
            bool esFactura = (configuracion.TipoComprobante.Contains("Factura") ||
                             configuracion.TipoComprobante.Contains("FACTURA")) &&
                             !configuracion.TipoComprobante.Contains("REMITO") &&
                             !configuracion.TipoComprobante.ToUpper().Contains("REMITO");

            System.Diagnostics.Debug.WriteLine($"🔍 Validación: esFactura = {esFactura}");
            System.Diagnostics.Debug.WriteLine($"   - TipoComprobante: '{configuracion.TipoComprobante}'");
            System.Diagnostics.Debug.WriteLine($"   - CAE: '{configuracion.CAE}'");

            if (esFactura)
            {
                System.Diagnostics.Debug.WriteLine("📄 Imprimiendo como FACTURA con datos fiscales");
                y = ImprimirDatosFacturacion(e.Graphics, fontTitulo, fontSubtitulo, leftMargin, rightMargin, y);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("📄 Imprimiendo como REMITO sin datos fiscales");
                y = ImprimirInfoComercio(e.Graphics, fontTitulo, fontSubtitulo, leftMargin, rightMargin, y);
            }

            y = ImprimirTituloComprobante(e.Graphics, fontBold, leftMargin, rightMargin, y);

            bool esFacturaC = configuracion.TipoComprobante.Contains("FacturaC") ||
                              configuracion.TipoComprobante.Contains("FACTURA C");

            if (esFactura)
            {
                if (esFacturaC)
                {
                    System.Diagnostics.Debug.WriteLine("🎯 Usando formato FACTURA C sin discriminar IVA");
                    y = ImprimirFacturaCSimple(e.Graphics, fontNormal, fontBold, leftMargin, rightMargin, anchoTotal, y);
                }
                else if (productosConIva != null && productosConIva.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("🎯 Usando formato FACTURA A/B con IVA");
                    y = ImprimirFacturaConIvaFormatoControlFacturas(e.Graphics, fontNormal, fontBold, leftMargin, rightMargin, anchoTotal, y);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Sin datos IVA, usando formato REMITO");
                    y = ImprimirRemitoSinIva(e.Graphics, fontNormal, fontBold, leftMargin, rightMargin, anchoTotal, y, linePen);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("📄 Usando formato REMITO sin IVA");
                y = ImprimirRemitoSinIva(e.Graphics, fontNormal, fontBold, leftMargin, rightMargin, anchoTotal, y, linePen);
            }

            if (!string.IsNullOrEmpty(configuracion.MensajePie))
            {
                y = ImprimirPieTicket(e.Graphics, fontSubtitulo, leftMargin, rightMargin, y);
            }

            // ✅ CORREGIDO: Solo imprimir información CAE para FACTURAS
            if (esFactura && !string.IsNullOrEmpty(configuracion.CAE))
            {
                System.Diagnostics.Debug.WriteLine("📋 Imprimiendo información CAE");
                y = ImprimirInformacionCAE(e.Graphics, fontSubtitulo, leftMargin, rightMargin, y);
            }
            else if (!string.IsNullOrEmpty(configuracion.CAE))
            {
                System.Diagnostics.Debug.WriteLine("⚠️ CAE presente pero NO se imprime (es REMITO)");
            }
        }

        private float ImprimirDatosFacturacion(Graphics graphics, Font fontTitulo, Font fontSubtitulo, float leftMargin, float rightMargin, float y)
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

            // ✅ DEBUG
            System.Diagnostics.Debug.WriteLine($"🔍 ImprimirTotalesFacturaC:");
            System.Diagnostics.Debug.WriteLine($"   - PorcentajeDescuento: {configuracion?.PorcentajeDescuento ?? 0}");
            System.Diagnostics.Debug.WriteLine($"   - ImporteDescuento: {configuracion?.ImporteDescuento ?? 0}");

            // ✅ MODIFICADO: Mostrar descuento si existe
            if (configuracion != null && configuracion.PorcentajeDescuento > 0)
            {
                string descuentoStr = $"DESCUENTO ({configuracion.PorcentajeDescuento:N2}%): -{configuracion.ImporteDescuento:C2}";
                SizeF descuentoSize = graphics.MeasureString(descuentoStr, fontBold);
                float descuentoX = rightMargin - descuentoSize.Width;

                // ✅ CAMBIO: Color negro en lugar de rojo
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

            // ✅ CAMBIO: Color negro en lugar de verde
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

            // ✅ DEBUG
            System.Diagnostics.Debug.WriteLine($"🔍 ImprimirTotalesBasicosFormatoFactura:");
            System.Diagnostics.Debug.WriteLine($"   - PorcentajeDescuento: {configuracion?.PorcentajeDescuento ?? 0}");
            System.Diagnostics.Debug.WriteLine($"   - ImporteDescuento: {configuracion?.ImporteDescuento ?? 0}");
            System.Diagnostics.Debug.WriteLine($"   - ImporteFinal: {configuracion?.ImporteFinal ?? 0}");

            // ✅ MODIFICADO: Mostrar descuento si existe
            if (configuracion != null && configuracion.PorcentajeDescuento > 0)
            {
                string descuentoStr = $"DESCUENTO ({configuracion.PorcentajeDescuento:N2}%): -{configuracion.ImporteDescuento:C2}";
                SizeF descuentoSize = graphics.MeasureString(descuentoStr, fontBold);
                float descuentoX = rightMargin - descuentoSize.Width;

                // ✅ CAMBIO: Color negro en lugar de rojo
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

            // ✅ CAMBIO: Color negro en lugar de verde
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
                string precioStr = precio < 1000 ? precio.ToString("C2") : precio.ToString("C0");
                SizeF precioSize = graphics.MeasureString(precioStr, font);
                float precioX = colX[1] + colWidths[1] - precioSize.Width - 2;

                string cantidadStr = row["cantidad"].ToString();
                if (int.TryParse(cantidadStr, out int cantVal))
                    cantidadTotal += cantVal;

                SizeF cantidadSize = graphics.MeasureString(cantidadStr, font);
                float cantidadX = colX[2] + (colWidths[2] - cantidadSize.Width) / 2;

                decimal total = Convert.ToDecimal(row["total"]);
                sumaTotal += total;
                string totalStr = total < 1000 ? total.ToString("C2") : total.ToString("C0");
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
                    if (numeroCompleto.Contains("-") && numeroCompleto.Length >= 19)
                    {
                        string[] partes = numeroCompleto.Split('-');
                        if (partes.Length == 3)
                        {
                            string tipoFormateado = "FACTURA";

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

                            if (int.TryParse(partes[1], out int puntoVentaNumero))
                            {
                                string puntoVentaFormateado = puntoVentaNumero.ToString("D4");
                                return $"{tipoFormateado} N° {puntoVentaFormateado}-{partes[2]}";
                            }
                        }
                    }

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

                        return $"FACTURA {letra} N° 0001-{numeroSimple:D8}";
                    }

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

            // ✅ DEBUG: Verificar valores de configuración
            System.Diagnostics.Debug.WriteLine($"🔍 ImprimirTotalesBasicos - Configuración:");
            System.Diagnostics.Debug.WriteLine($"   - PorcentajeDescuento: {configuracion?.PorcentajeDescuento ?? 0}");
            System.Diagnostics.Debug.WriteLine($"   - ImporteDescuento: {configuracion?.ImporteDescuento ?? 0}");
            System.Diagnostics.Debug.WriteLine($"   - ImporteFinal: {configuracion?.ImporteFinal ?? 0}");
            System.Diagnostics.Debug.WriteLine($"   - SumaTotal recibido: {sumaTotal}");

            // ✅ MODIFICADO: Mostrar descuento si existe
            if (configuracion != null && configuracion.PorcentajeDescuento > 0)
            {
                string descuentoStr = $"DESCUENTO ({configuracion.PorcentajeDescuento:N2}%): -{configuracion.ImporteDescuento:C2}";
                SizeF descuentoSize = graphics.MeasureString(descuentoStr, fontBold);
                float descuentoX = rightMargin - descuentoSize.Width;

                // ✅ CAMBIO: Color negro en lugar de rojo
                graphics.DrawString(descuentoStr, fontBold, Brushes.Black, descuentoX, y);
                y += descuentoSize.Height + 4;

                System.Diagnostics.Debug.WriteLine($"✅ Descuento mostrado en ticket: {descuentoStr}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ NO se muestra descuento - PorcentajeDescuento: {configuracion?.PorcentajeDescuento ?? 0}");
            }

            // ✅ MODIFICADO: TOTAL FINAL con validación mejorada
            decimal totalFinal;

            if (configuracion != null && configuracion.ImporteFinal > 0)
            {
                // Si ImporteFinal tiene valor, usarlo directamente
                totalFinal = configuracion.ImporteFinal;
                System.Diagnostics.Debug.WriteLine($"✅ Usando ImporteFinal: {totalFinal:C2}");
            }
            else if (configuracion != null && configuracion.PorcentajeDescuento > 0 && configuracion.ImporteDescuento > 0)
            {
                // Si no hay ImporteFinal pero hay descuento, calcularlo
                totalFinal = sumaTotal - configuracion.ImporteDescuento;
                System.Diagnostics.Debug.WriteLine($"✅ Calculando: {sumaTotal:C2} - {configuracion.ImporteDescuento:C2} = {totalFinal:C2}");
            }
            else
            {
                // Si no hay descuento, usar el total sin descuento
                totalFinal = sumaTotal;
                System.Diagnostics.Debug.WriteLine($"✅ Sin descuento, usando sumaTotal: {totalFinal:C2}");
            }

            System.Diagnostics.Debug.WriteLine($"💰 Total Final a imprimir: {totalFinal:C2}");

            string totalGeneralStr = $"TOTAL: {totalFinal:C2}";
            Font fontTotal = new Font(fontBold.FontFamily, fontBold.Size + 2, FontStyle.Bold);
            SizeF totalGeneralSize = graphics.MeasureString(totalGeneralStr, fontTotal);
            float totalGeneralX = rightMargin - totalGeneralSize.Width;

            // ✅ CAMBIO: Color negro en lugar de verde
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