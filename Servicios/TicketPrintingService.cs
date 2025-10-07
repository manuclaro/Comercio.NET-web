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
                // Inicializar con valores por defecto en caso de error
                datosFacturacion = new DatosFacturacion();
            }
        }

        private void ConfigurarTamañoTicket()
        {
            // Configuración estándar para ticket térmico (80mm)
            int anchoTicket = (int)(80 / 25.4 * 100); // 80mm a centésimas de pulgada
            int altoTicket = (int)(200 / 25.4 * 100); // 200mm máximo

            PaperSize ticketSize = new PaperSize("Ticket", anchoTicket, altoTicket);
            printDocument.DefaultPageSettings.PaperSize = ticketSize;

            // CAMBIO: Reducir márgenes significativamente para aprovechar más ancho
            printDocument.DefaultPageSettings.Margins = new Margins(2, 2, 5, 5); // Izq, Der, Arr, Abj
        }

        // CORREGIDO: Cambiar a Task para que sea awaitable
        public async Task ImprimirTicket(DataTable datos, TicketConfig config)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(TicketPrintingService));

            datosTicket = datos;
            configuracion = config;

            // NUEVO: Si es factura, obtener datos de IVA ANTES de mostrar el preview
            if (config.TipoComprobante.Contains("Factura") || config.TipoComprobante.Contains("FACTURA"))
            {
                System.Diagnostics.Debug.WriteLine("🔍 Detectada factura, cargando datos IVA...");
                await CargarDatosIvaParaFactura();
                System.Diagnostics.Debug.WriteLine($"✅ Datos IVA cargados: {productosConIva?.Count ?? 0} productos");
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

        // CORREGIDO: Cambiar a Task para que sea awaitable
        public async Task ImprimirTicketDirecto(DataTable datos, TicketConfig config)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(TicketPrintingService));

            datosTicket = datos;
            configuracion = config;

            // NUEVO: Si es factura, obtener datos de IVA ANTES de imprimir
            if (config.TipoComprobante.Contains("Factura") || config.TipoComprobante.Contains("FACTURA"))
            {
                await CargarDatosIvaParaFactura();
            }

            printDocument.Print();
        }

        // MEJORADO: Método para cargar datos de IVA con mejor debugging
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
                                : 21.00m; // Default

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

                            System.Diagnostics.Debug.WriteLine($"  📦 {codigo}: IVA {iva}% - Total: ${subtotal} - Base: ${baseImponible} - IVA: ${importeIva}");

                            // Agrupar en resumen por alícuota
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

                System.Diagnostics.Debug.WriteLine($"✅ Carga completada: {productosConIva.Count} productos, {resumenIva.Count} alícuotas diferentes");
                foreach (var kvp in resumenIva)
                {
                    System.Diagnostics.Debug.WriteLine($"  💰 IVA {kvp.Key}%: Base ${kvp.Value.BaseImponible} - IVA ${kvp.Value.ImporteIva}");
                }
            }
            catch (Exception ex)
            {
                // En caso de error, usar datos básicos
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando datos IVA: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"🔧 Stack trace: {ex.StackTrace}");

                // Inicializar listas vacías para evitar null reference
                productosConIva = new List<ProductoConIva>();
                resumenIva = new Dictionary<decimal, (decimal BaseImponible, decimal ImporteIva)>();
            }
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (datosTicket == null || configuracion == null) return;

            System.Diagnostics.Debug.WriteLine("🖨️ Iniciando impresión...");
            System.Diagnostics.Debug.WriteLine($"📄 Tipo comprobante: {configuracion.TipoComprobante}");
            System.Diagnostics.Debug.WriteLine($"📦 Productos con IVA cargados: {productosConIva?.Count ?? 0}");

            // Configuración de fuentes y estilos
            Font fontNormal = new Font("Arial", 8);
            Font fontBold = new Font("Arial", 8, FontStyle.Bold);
            Font fontTitulo = new Font("Arial", 14, FontStyle.Bold);
            Font fontSubtitulo = new Font("Arial", 7);
            Pen linePen = new Pen(Color.Black, 1);

            // CAMBIO: Márgenes más ajustados para aprovechar el ancho completo
            float leftMargin = e.MarginBounds.Left;
            float topMargin = e.MarginBounds.Top;
            float rightMargin = e.MarginBounds.Right;
            float y = topMargin;

            // CAMBIO: Calcular el ancho total disponible y redistribuir las columnas
            float anchoTotal = rightMargin - leftMargin;

            // 1. ENCABEZADO - Fecha y hora
            y = ImprimirFechaHora(e.Graphics, fontNormal, leftMargin, rightMargin, y);

            // DETECTAR SI ES FACTURA PARA MOSTRAR DATOS DE FACTURACIÓN
            bool esFactura = configuracion.TipoComprobante.Contains("Factura") || 
                           configuracion.TipoComprobante.Contains("FACTURA") ||
                           !string.IsNullOrEmpty(configuracion.CAE);

            if (esFactura)
            {
                // 2. INFORMACIÓN DE FACTURACIÓN (NUEVA SECCIÓN PARA FACTURAS)
                y = ImprimirDatosFacturacion(e.Graphics, fontTitulo, fontSubtitulo, leftMargin, rightMargin, y);
            }
            else
            {
                // 2. INFORMACIÓN DEL COMERCIO (PARA REMITOS)
                y = ImprimirInfoComercio(e.Graphics, fontTitulo, fontSubtitulo, leftMargin, rightMargin, y);
            }

            // 3. TÍTULO DEL COMPROBANTE
            y = ImprimirTituloComprobante(e.Graphics, fontBold, leftMargin, rightMargin, y);

            // 4. ENCABEZADOS DE TABLA Y DETALLE DE PRODUCTOS
            if (esFactura && productosConIva != null && productosConIva.Count > 0)
            {
                // FORMATO FACTURA: Con alícuota IVA usando el mismo formato del formulario
                System.Diagnostics.Debug.WriteLine("🎯 Usando formato FACTURA con IVA (formato Control de Facturas)");
                y = ImprimirFacturaConIvaFormatoControlFacturas(e.Graphics, fontNormal, fontBold, leftMargin, rightMargin, anchoTotal, y);
            }
            else
            {
                // FORMATO REMITO: Sin IVA
                System.Diagnostics.Debug.WriteLine("📄 Usando formato REMITO sin IVA");
                y = ImprimirRemitoSinIva(e.Graphics, fontNormal, fontBold, leftMargin, rightMargin, anchoTotal, y, linePen);
            }

            // 9. PIE DE PÁGINA
            if (!string.IsNullOrEmpty(configuracion.MensajePie))
            {
                y = ImprimirPieTicket(e.Graphics, fontSubtitulo, leftMargin, rightMargin, y);
            }

            // 10. NUEVA SECCIÓN: Información adicional de facturas
            if (!string.IsNullOrEmpty(configuracion.CAE))
            {
                y = ImprimirInformacionCAE(e.Graphics, fontSubtitulo, leftMargin, rightMargin, y);  
            }
        }

        // MODIFICADO: Método para imprimir los datos de facturación en la cabecera
        private float ImprimirDatosFacturacion(Graphics graphics, Font fontTitulo, Font fontSubtitulo, float leftMargin, float rightMargin, float y)
        {
            float anchoUtil = rightMargin - leftMargin;

            // ===== MANTENER CABECERA TRADICIONAL COMO EN REMITOS =====
            
            // Nombre del comercio (igual que remitos)
            SizeF nombreSize = graphics.MeasureString(configuracion.NombreComercio, fontTitulo);
            float nombreX = leftMargin + (anchoUtil - nombreSize.Width) / 2;
            graphics.DrawString(configuracion.NombreComercio, fontTitulo, Brushes.Black, nombreX, y);
            y += nombreSize.Height;

            // Domicilio del comercio (igual que remitos)
            if (!string.IsNullOrEmpty(configuracion.DomicilioComercio))
            {
                SizeF domicilioSize = graphics.MeasureString(configuracion.DomicilioComercio, fontSubtitulo);
                float domicilioX = leftMargin + (anchoUtil - domicilioSize.Width) / 2;
                graphics.DrawString(configuracion.DomicilioComercio, fontSubtitulo, Brushes.Black, domicilioX, y);
                y += domicilioSize.Height;
            }

            y += 6; // Espacio después de la cabecera tradicional

            // ===== DATOS DE FACTURACIÓN DISCRETOS (ALINEADOS A LA IZQUIERDA) =====
            
            // Fuente más pequeña y discreta para datos fiscales
            Font fontDatosFiscales = new Font("Arial", 6, FontStyle.Regular);
            
            // Razón Social (más pequeña, alineada a la izquierda)
            if (!string.IsNullOrEmpty(datosFacturacion.RazonSocial))
            {
                string razonText = $"Razón Social: {datosFacturacion.RazonSocial}";
                graphics.DrawString(razonText, fontDatosFiscales, Brushes.Black, leftMargin, y);
                y += 10; // Espaciado menor
            }

            // CUIT (alineado a la izquierda)
            if (!string.IsNullOrEmpty(datosFacturacion.CUIT))
            {
                string cuitText = $"CUIT: {datosFacturacion.CUIT}";
                graphics.DrawString(cuitText, fontDatosFiscales, Brushes.Black, leftMargin, y);
                y += 10;
            }

            // Domicilio Fiscal (alineado a la izquierda)
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

            // Ingresos Brutos (alineado a la izquierda)
            if (!string.IsNullOrEmpty(datosFacturacion.IngBrutos))
            {
                string ingBrutosText = $"Ing. Brutos: {datosFacturacion.IngBrutos}";
                graphics.DrawString(ingBrutosText, fontDatosFiscales, Brushes.Black, leftMargin, y);
                y += 10;
            }

            // Condición ante IVA (alineado a la izquierda)
            if (!string.IsNullOrEmpty(datosFacturacion.Condicion))
            {
                string condicionText = $"Condición IVA: {datosFacturacion.Condicion}";
                graphics.DrawString(condicionText, fontDatosFiscales, Brushes.Black, leftMargin, y);
                y += 10;
            }

            // Inicio de Actividades (alineado a la izquierda)
            if (!string.IsNullOrEmpty(datosFacturacion.InicioActividades))
            {
                string inicioText = $"Inicio Actividades: {datosFacturacion.InicioActividades}";
                graphics.DrawString(inicioText, fontDatosFiscales, Brushes.Black, leftMargin, y);
                y += 10;
            }

            // Limpiar fuente temporal
            fontDatosFiscales.Dispose();

            return y + 8; // Espacio adicional antes del número de factura
        }

        // NUEVO: Método para imprimir factura con el MISMO formato que Control de Facturas
        private float ImprimirFacturaConIvaFormatoControlFacturas(Graphics graphics, Font fontNormal, Font fontBold, float leftMargin, float rightMargin, float anchoTotal, float y)
        {
            System.Diagnostics.Debug.WriteLine("🎯 Ejecutando ImprimirFacturaConIvaFormatoControlFacturas - MISMO FORMATO que Control de Facturas");
            Pen linePen = new Pen(Color.Black, 1);

            // FORMATO EXACTO DEL CONTROL DE FACTURAS: Descripción | Precio Unit. | Cantidad | Total
            // Mismas proporciones que en el DataGridView del formulario
            float colDescripcion = anchoTotal * 0.50f;    // 50% para descripción (igual que formulario)
            float colPrecioUnit = anchoTotal * 0.20f;     // 20% para precio unitario
            float colCantidad = anchoTotal * 0.10f;       // 10% para cantidad
            float colTotal = anchoTotal * 0.20f;          // 20% para total

            float[] colX = {
                leftMargin,
                leftMargin + colDescripcion,
                leftMargin + colDescripcion + colPrecioUnit,
                leftMargin + colDescripcion + colPrecioUnit + colCantidad
            };

            float tablaRight = leftMargin + anchoTotal;

            // ENCABEZADOS EXACTOS DEL FORMULARIO CONTROL DE FACTURAS
            string[] headers = { "PRODUCTO", "PRECIO UNIT.", "C", "TOTAL" };

            for (int i = 0; i < headers.Length; i++)
            {
                float headerX = colX[i];
                SizeF headerSize = graphics.MeasureString(headers[i], fontBold);

                // Alineación según la columna (mismo estilo que formulario)
                switch (i)
                {
                    case 0: // Producto - izquierda
                        // Ya está alineado a la izquierda
                        break;
                    case 1: // Precio Unit. - centrado (como en formulario)
                        headerX += (colPrecioUnit - headerSize.Width) / 2;
                        break;
                    case 2: // Cantidad - centrado
                        headerX += (colCantidad - headerSize.Width) / 2;
                        break;
                    case 3: // Total - centrado (como en formulario)
                        headerX += (colTotal - headerSize.Width) / 2;
                        break;
                }

                graphics.DrawString(headers[i], fontBold, Brushes.Black, headerX, y);
            }

            y += 16;

            // LÍNEA SEPARADORA
            graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 4;

            // DETALLE DE PRODUCTOS CON FORMATO EXACTO DEL FORMULARIO
            int cantidadTotal = 0;
            decimal sumaTotal = 0;
            float rowHeight = 16;

            // Fuente para IVA (igual que en formulario)
            Font fontIva = new Font("Arial", 6, FontStyle.Regular);
            Brush ivaGrayBrush = new SolidBrush(Color.FromArgb(100, 100, 100));

            System.Diagnostics.Debug.WriteLine($"🔄 Imprimiendo {productosConIva.Count} productos con formato Control de Facturas");

            foreach (var producto in productosConIva)
            {
                float filaYInicial = y;

                // DESCRIPCIÓN con texto de IVA al final de la línea (como en formulario)
                string descripcionCompleta = producto.Descripcion;
                string textoIva = producto.AlicuotaIva > 0 ? $"({producto.AlicuotaIva:N1}%)" : "";
                
                // Calcular espacio disponible para descripción
                float anchoDisponibleDescripcion = colDescripcion - 4; // Margen interno
                
                // Si hay texto de IVA, reservar espacio
                SizeF ivaSize = SizeF.Empty;
                if (!string.IsNullOrEmpty(textoIva))
                {
                    ivaSize = graphics.MeasureString(textoIva, fontIva);
                    anchoDisponibleDescripcion -= ivaSize.Width + 8; // Espacio para IVA + margen
                }
                
                // Dividir descripción en líneas
                List<string> lineasDescripcion = DividirTextoEnLineasMejorado(graphics, descripcionCompleta, fontNormal, anchoDisponibleDescripcion);

                // PRECIO UNITARIO centrado (formato formulario)
                string precioStr = producto.Precio.ToString("C2");
                SizeF precioSize = graphics.MeasureString(precioStr, fontNormal);
                float precioX = colX[1] + (colPrecioUnit - precioSize.Width) / 2;

                // CANTIDAD centrada (formato formulario)
                string cantidadStr = producto.Cantidad.ToString();
                cantidadTotal += producto.Cantidad;
                SizeF cantidadSize = graphics.MeasureString(cantidadStr, fontNormal);
                float cantidadX = colX[2] + (colCantidad - cantidadSize.Width) / 2;

                // TOTAL centrado (formato formulario)
                decimal total = producto.Subtotal;
                sumaTotal += total;
                string totalStr = total.ToString("C2");
                SizeF totalSize = graphics.MeasureString(totalStr, fontNormal);
                float totalX = colX[3] + (colTotal - totalSize.Width) / 2;

                // Imprimir líneas de descripción
                float filaY = filaYInicial;
                for (int i = 0; i < lineasDescripcion.Count; i++)
                {
                    // Precio, Cantidad y Total solo en la primera línea
                    if (i == 0)
                    {
                        graphics.DrawString(precioStr, fontNormal, Brushes.Black, precioX, filaY);
                        graphics.DrawString(cantidadStr, fontNormal, Brushes.Black, cantidadX, filaY);
                        graphics.DrawString(totalStr, fontNormal, Brushes.Black, totalX, filaY);
                    }

                    // Descripción en cada línea
                    graphics.DrawString(lineasDescripcion[i], fontNormal, Brushes.Black, colX[0], filaY);
                    filaY += rowHeight;
                }

                // IVA alineado a la derecha del área de descripción (igual que formulario)
                if (!string.IsNullOrEmpty(textoIva))
                {
                    float ivaX = leftMargin + colDescripcion - ivaSize.Width - 2;
                    graphics.DrawString(textoIva, fontIva, ivaGrayBrush, ivaX, filaYInicial + 1);
                }

                y = filaY;
                System.Diagnostics.Debug.WriteLine($"  📦 {producto.Codigo}: {descripcionCompleta} {textoIva}");
            }

            // Limpiar recursos
            fontIva.Dispose();
            ivaGrayBrush.Dispose();

            // LÍNEA SEPARADORA FINAL
            y += 4;
            graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 6;

            // TOTALES BÁSICOS (igual formato que formulario)
            y = ImprimirTotalesBasicosFormatoFactura(graphics, fontBold, leftMargin, rightMargin, y, cantidadTotal, sumaTotal);

            // RESUMEN DE IVA PARA FACTURAS (igual que formulario)
            if (resumenIva != null && resumenIva.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("💰 Imprimiendo resumen de IVA con formato Control de Facturas");
                y = ImprimirResumenIvaFormatoFactura(graphics, fontBold, fontNormal, leftMargin, rightMargin, y);
            }

            return y;
        }

        // NUEVO: Totales básicos con formato específico para facturas - FUENTES EN NEGRO
        private float ImprimirTotalesBasicosFormatoFactura(Graphics graphics, Font fontBold, float leftMargin, float rightMargin, float y, int cantidadTotal, decimal sumaTotal)
        {
            float anchoUtil = rightMargin - leftMargin;

            // Productos totales a la izquierda
            string productosStr = $"PRODUCTOS: {cantidadTotal}";
            graphics.DrawString(productosStr, fontBold, Brushes.Black, leftMargin, y);

            // Total general a la derecha - CAMBIO: usar negro en lugar de verde
            string totalStr = $"TOTAL FACTURA: {sumaTotal:C2}";
            SizeF totalSize = graphics.MeasureString(totalStr, fontBold);
            float totalX = rightMargin - totalSize.Width;
            
            // CAMBIO: Usar negro en lugar de color verde para compatibilidad con impresoras monocromáticas
            graphics.DrawString(totalStr, fontBold, Brushes.Black, totalX, y);

            return y + totalSize.Height + 8;
        }

        // NUEVO: Resumen de IVA con formato específico para facturas - FUENTES EN NEGRO
        private float ImprimirResumenIvaFormatoFactura(Graphics graphics, Font fontBold, Font fontNormal, float leftMargin, float rightMargin, float y)
        {
            y += 8; // Espacio adicional

            // Título del resumen (igual que formulario)
            string tituloResumen = "=== RESUMEN IVA ===";
            SizeF tituloSize = graphics.MeasureString(tituloResumen, fontBold);
            float tituloX = leftMargin + ((rightMargin - leftMargin - tituloSize.Width) / 2);
            graphics.DrawString(tituloResumen, fontBold, Brushes.Black, tituloX, y);
            y += tituloSize.Height + 4;

            // Fuente para detalle (igual que formulario)
            Font fontIvaDetalle = new Font("Arial", 7, FontStyle.Regular);
            Font fontIvaDetalleBold = new Font("Arial", 7, FontStyle.Bold);

            decimal totalBaseImponible = 0;
            decimal totalIva = 0;

            // Detalle por alícuota ordenado (igual que formulario)
            foreach (var kvp in resumenIva.OrderByDescending(x => x.Key))
            {
                decimal alicuota = kvp.Key;
                decimal baseImponible = kvp.Value.BaseImponible;
                decimal importeIva = kvp.Value.ImporteIva;

                totalBaseImponible += baseImponible;
                totalIva += importeIva;

                // Formato exacto del formulario
                string lineaIva = $"IVA {alicuota:N1}%: Base {baseImponible:C2} - IVA {importeIva:C2}";
                graphics.DrawString(lineaIva, fontIvaDetalle, Brushes.Black, leftMargin, y);
                y += 14;

                System.Diagnostics.Debug.WriteLine($"  💰 {lineaIva}");
            }

            // Línea divisoria
            Pen linePen = new Pen(Color.Black, 1);
            graphics.DrawLine(linePen, leftMargin, y, rightMargin, y);
            y += 4;

            // Totales finales - CAMBIO: usar negro en lugar de colores
            string lineaTotalBase = $"TOTAL BASE IMPONIBLE: {totalBaseImponible:C2}";
            graphics.DrawString(lineaTotalBase, fontIvaDetalleBold, Brushes.Black, leftMargin, y);
            y += 16;

            string lineaTotalIva = $"TOTAL IVA: {totalIva:C2}";
            // CAMBIO: Usar negro en lugar de rojo para compatibilidad con impresoras monocromáticas
            graphics.DrawString(lineaTotalIva, fontIvaDetalleBold, Brushes.Black, leftMargin, y);
            y += 16;

            System.Diagnostics.Debug.WriteLine($"💰 {lineaTotalBase}");
            System.Diagnostics.Debug.WriteLine($"💰 {lineaTotalIva}");

            // Limpiar recursos
            linePen.Dispose();
            fontIvaDetalle.Dispose();
            fontIvaDetalleBold.Dispose();

            return y;
        }

        // Método para imprimir remito sin IVA
        private float ImprimirRemitoSinIva(Graphics graphics, Font fontNormal, Font fontBold, float leftMargin, float rightMargin, float anchoTotal, float y, Pen linePen)
        {
            // NUEVA DISTRIBUCIÓN: Descripción | Precio | Cantidad | Total
            float colDescripcion = anchoTotal * 0.50f;   // 50% del ancho total  
            float colPrecio = anchoTotal * 0.20f;        // 20% del ancho total
            float colCantidad = anchoTotal * 0.10f;      // 10% del ancho total (nueva posición)
            float colTotal = anchoTotal * 0.20f;         // 20% del ancho total

            float[] colX = {
                leftMargin,
                leftMargin + colDescripcion,
                leftMargin + colDescripcion + colPrecio,
                leftMargin + colDescripcion + colPrecio + colCantidad
            };

            float tablaRight = leftMargin + anchoTotal;

            // ENCABEZADOS DE TABLA CON NUEVO ORDEN
            string[] headers = { "DESCRIPCIÓN", "PRECIO", "C", "TOTAL" };

            for (int i = 0; i < headers.Length; i++)
            {
                float headerX = colX[i];
                SizeF headerSize = graphics.MeasureString(headers[i], fontBold);

                // Alineación según la columna
                switch (i)
                {
                    case 0: // Descripción - izquierda
                        // Ya está alineado a la izquierda
                        break;
                    case 1: // Precio - derecha
                        headerX += colPrecio - headerSize.Width - 2;
                        break;
                    case 2: // Cantidad - centrado
                        headerX += (colCantidad - headerSize.Width) / 2;
                        break;
                    case 3: // Total - derecha
                        headerX += colTotal - headerSize.Width - 2;
                        break;
                }

                graphics.DrawString(headers[i], fontBold, Brushes.Black, headerX, y);
            }

            y += 16;

            // LÍNEA SEPARADORA
            graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 4;

            // DETALLE DE PRODUCTOS CON NUEVO ORDEN
            var resultado = ImprimirDetalleProductosNuevoOrden(graphics, fontNormal, colX, new float[] { colDescripcion, colPrecio, colCantidad, colTotal }, y);
            y = resultado.y;
            int cantidadTotal = resultado.cantidadTotal;
            decimal sumaTotal = resultado.sumaTotal;

            // LÍNEA SEPARADORA FINAL
            y += 4;
            graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 6;

            // TOTALES BÁSICOS
            y = ImprimirTotalesBasicos(graphics, fontBold, leftMargin, rightMargin, y, cantidadTotal, sumaTotal);

            return y;
        }

        // Método para imprimir productos con el nuevo orden de columnas
        private (float y, int cantidadTotal, decimal sumaTotal) ImprimirDetalleProductosNuevoOrden(Graphics graphics, Font font, float[] colX, float[] colWidths, float y)
        {
            int cantidadTotal = 0;
            decimal sumaTotal = 0;
            float rowHeight = 16;

            foreach (DataRow row in datosTicket.Rows)
            {
                float filaYInicial = y;

                // Descripción con salto de línea
                string descripcion = row["descripcion"].ToString();
                float maxDescripcionWidth = colWidths[0] - 4; // Margen interno
                List<string> lineasDescripcion = DividirTextoEnLineasMejorado(graphics, descripcion, font, maxDescripcionWidth);

                // Precio alineado a la derecha
                decimal precio = Convert.ToDecimal(row["precio"]);
                string precioStr = precio < 1000 ? precio.ToString("C2") : precio.ToString("C0");
                SizeF precioSize = graphics.MeasureString(precioStr, font);
                float precioX = colX[1] + colWidths[1] - precioSize.Width - 2;

                // Cantidad centrada (nueva posición)
                string cantidadStr = row["cantidad"].ToString();
                if (int.TryParse(cantidadStr, out int cantVal))
                    cantidadTotal += cantVal;

                SizeF cantidadSize = graphics.MeasureString(cantidadStr, font);
                float cantidadX = colX[2] + (colWidths[2] - cantidadSize.Width) / 2;

                // Total alineado a la derecha
                decimal total = Convert.ToDecimal(row["total"]);
                sumaTotal += total;
                string totalStr = total < 1000 ? total.ToString("C2") : total.ToString("C0");
                SizeF totalSize = graphics.MeasureString(totalStr, font);
                float totalX = colX[3] + colWidths[3] - totalSize.Width - 2;

                // Imprimir líneas de descripción
                float filaY = filaYInicial;
                for (int i = 0; i < lineasDescripcion.Count; i++)
                {
                    // Precio, Cantidad y Total solo en la primera línea
                    if (i == 0)
                    {
                        graphics.DrawString(precioStr, font, Brushes.Black, precioX, filaY);

                        float cantidadYLinea = filaY + ((rowHeight - cantidadSize.Height) / 2);
                        graphics.DrawString(cantidadStr, font, Brushes.Black, cantidadX, cantidadYLinea);

                        graphics.DrawString(totalStr, font, Brushes.Black, totalX, filaY);
                    }

                    // Descripción en cada línea
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

            // Nombre del comercio
            SizeF nombreSize = graphics.MeasureString(configuracion.NombreComercio, fontTitulo);
            float nombreX = leftMargin + (anchoUtil - nombreSize.Width) / 2;
            graphics.DrawString(configuracion.NombreComercio, fontTitulo, Brushes.Black, nombreX, y);
            y += nombreSize.Height;

            // Domicilio
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
            // Mostrar solo el número formateado (ya incluye letra y guión)
            string titulo = configuracion.NumeroComprobante;
            SizeF tituloSize = graphics.MeasureString(titulo, fontBold);
            float tituloX = leftMargin + (anchoUtil - tituloSize.Width) / 2;
            graphics.DrawString(titulo, fontBold, Brushes.Black, tituloX, y);
            return y + tituloSize.Height + 8;
        }

        private float ImprimirTotalesBasicos(Graphics graphics, Font fontBold, float leftMargin, float rightMargin, float y, int cantidadTotal, decimal sumaTotal)
        {
            // CAMBIO: Usar todo el ancho disponible para los totales
            float anchoUtil = rightMargin - leftMargin;

            // Cantidad total a la izquierda
            string cantidadTotalStr = $"PRODUCTOS: {cantidadTotal}";
            graphics.DrawString(cantidadTotalStr, fontBold, Brushes.Black, leftMargin, y);

            // Total general a la derecha, usando todo el ancho disponible
            string totalGeneralStr = $"TOTAL: {sumaTotal:C2}";
            SizeF totalGeneralSize = graphics.MeasureString(totalGeneralStr, fontBold);
            float totalGeneralX = rightMargin - totalGeneralSize.Width;
            graphics.DrawString(totalGeneralStr, fontBold, Brushes.Black, totalGeneralX, y);

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
            y += 8; // Espacio adicional

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

            // Dividir por palabras para mejor ruptura
            string[] palabras = texto.Split(' ');
            string lineaActual = "";

            foreach (string palabraOriginal in palabras)
            {
                string palabra = palabraOriginal; // Crear una copia modificable
                string pruebaLinea = string.IsNullOrEmpty(lineaActual) ? palabra : lineaActual + " " + palabra;
                SizeF size = graphics.MeasureString(pruebaLinea, font);

                if (size.Width <= anchoMaximo)
                {
                    lineaActual = pruebaLinea;
                }
                else
                {
                    // Si la línea actual no está vacía, agregarla
                    if (!string.IsNullOrEmpty(lineaActual))
                    {
                        lineas.Add(lineaActual);
                        lineaActual = palabra;
                    }
                    else
                    {
                        // La palabra sola es muy larga, cortarla por caracteres
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
                                // Si ni un carácter cabe, agregar lo que sea
                                lineas.Add(palabra.Substring(0, 1));
                                palabra = palabra.Substring(1);
                            }
                        }
                        lineaActual = "";
                    }
                }
            }

            // Agregar la última línea si no está vacía
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
                    // Liberar recursos administrados
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

        // NUEVA: Clase para representar productos con información de IVA
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

    // NUEVA: Clase para almacenar datos de facturación
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

        // Configuración adicional para facturas
        public string CAE { get; set; } = "";
        public DateTime? CAEVencimiento { get; set; }
        public string CUIT { get; set; } = "";
    }
}