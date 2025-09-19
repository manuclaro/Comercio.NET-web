using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace Comercio.NET.Servicios
{
    public class TicketPrintingService : IDisposable
    {
        private readonly PrintDocument printDocument;
        private DataTable datosTicket;
        private TicketConfig configuracion;
        private bool disposed = false;

        public TicketPrintingService()
        {
            printDocument = new PrintDocument();
            printDocument.PrintPage += PrintDocument_PrintPage;
            ConfigurarTamañoTicket();
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

        public void ImprimirTicket(DataTable datos, TicketConfig config)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(TicketPrintingService));

            datosTicket = datos;
            configuracion = config;

            using (PrintPreviewDialog previewDialog = new PrintPreviewDialog())
            {
                previewDialog.Document = printDocument;
                previewDialog.WindowState = FormWindowState.Maximized;
                previewDialog.ShowDialog();
            }
        }

        public void ImprimirTicketDirecto(DataTable datos, TicketConfig config)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(TicketPrintingService));

            datosTicket = datos;
            configuracion = config;
            printDocument.Print();
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (datosTicket == null || configuracion == null) return;

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
            
            // NUEVO: Redistribución de columnas para usar todo el ancho disponible
            float colCantidad = anchoTotal * 0.08f;      // 8% del ancho total
            float colDescripcion = anchoTotal * 0.50f;   // 50% del ancho total  
            float colPrecio = anchoTotal * 0.21f;        // 21% del ancho total
            float colTotal = anchoTotal * 0.21f;         // 21% del ancho total

            float[] colX = {
                leftMargin,
                leftMargin + colCantidad,
                leftMargin + colCantidad + colDescripcion,
                leftMargin + colCantidad + colDescripcion + colPrecio
            };
            
            float tablaRight = leftMargin + anchoTotal; // Usar todo el ancho disponible

            // 1. ENCABEZADO - Fecha y hora
            y = ImprimirFechaHora(e.Graphics, fontNormal, leftMargin, tablaRight, y);

            // 2. INFORMACIÓN DEL COMERCIO
            y = ImprimirInfoComercio(e.Graphics, fontTitulo, fontSubtitulo, leftMargin, tablaRight, y);

            // 3. TÍTULO DEL COMPROBANTE
            y = ImprimirTituloComprobante(e.Graphics, fontBold, leftMargin, tablaRight, y);

            // 4. ENCABEZADOS DE TABLA
            y = ImprimirEncabezadosTabla(e.Graphics, fontBold, colX, new float[] { colCantidad, colDescripcion, colPrecio, colTotal }, y);

            // 5. LÍNEA SEPARADORA
            e.Graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 4;

            // 6. DETALLE DE PRODUCTOS
            var resultado = ImprimirDetalleProductos(e.Graphics, fontNormal, colX, new float[] { colCantidad, colDescripcion, colPrecio, colTotal }, y);
            y = resultado.y;
            int cantidadTotal = resultado.cantidadTotal;
            decimal sumaTotal = resultado.sumaTotal;

            // 7. LÍNEA SEPARADORA FINAL
            y += 4;
            e.Graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 6;

            // 8. TOTALES
            y = ImprimirTotales(e.Graphics, fontBold, leftMargin, tablaRight, y, cantidadTotal, sumaTotal);

            // 9. PIE DE PÁGINA
            if (!string.IsNullOrEmpty(configuracion.MensajePie))
            {
                y = ImprimirPieTicket(e.Graphics, fontSubtitulo, leftMargin, tablaRight, y);
            }

            // 10. NUEVA SECCIÓN: Información adicional de facturas
            if (!string.IsNullOrEmpty(configuracion.CAE))
            {
                y = ImprimirInformacionCAE(e.Graphics, fontSubtitulo, leftMargin, tablaRight, y);
            }
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
            string titulo = $"{configuracion.TipoComprobante.ToUpper()} N°: {configuracion.NumeroComprobante}";
            
            SizeF tituloSize = graphics.MeasureString(titulo, fontBold);
            float tituloX = leftMargin + (anchoUtil - tituloSize.Width) / 2;
            graphics.DrawString(titulo, fontBold, Brushes.Black, tituloX, y);
            
            return y + tituloSize.Height + 8;
        }

        private float ImprimirEncabezadosTabla(Graphics graphics, Font fontBold, float[] colX, float[] colWidths, float y)
        {
            string[] headers = { "C", "DESCRIPCIÓN", "PRECIO", "TOTAL" };
            
            for (int i = 0; i < headers.Length; i++)
            {
                float headerX = colX[i];
                SizeF headerSize = graphics.MeasureString(headers[i], fontBold);

                // Alineación según la columna
                switch (i)
                {
                    case 0: // Cantidad - centrado
                        headerX += (colWidths[i] - headerSize.Width) / 2;
                        break;
                    case 1: // Descripción - izquierda
                        // Ya está alineado a la izquierda
                        break;
                    case 2: // Precio - derecha
                    case 3: // Total - derecha
                        headerX += colWidths[i] - headerSize.Width - 2;
                        break;
                }
                
                graphics.DrawString(headers[i], fontBold, Brushes.Black, headerX, y);
            }
            
            return y + 16;
        }

        private (float y, int cantidadTotal, decimal sumaTotal) ImprimirDetalleProductos(Graphics graphics, Font font, float[] colX, float[] colWidths, float y)
        {
            int cantidadTotal = 0;
            decimal sumaTotal = 0;
            float rowHeight = 16;

            foreach (DataRow row in datosTicket.Rows)
            {
                float filaYInicial = y;

                // Cantidad centrada
                string cantidadStr = row["cantidad"].ToString();
                if (int.TryParse(cantidadStr, out int cantVal))
                    cantidadTotal += cantVal;

                SizeF cantidadSize = graphics.MeasureString(cantidadStr, font);
                float cantidadX = colX[0] + (colWidths[0] - cantidadSize.Width) / 2;

                // Descripción con salto de línea MEJORADO
                string descripcion = row["descripcion"].ToString();
                float maxDescripcionWidth = colWidths[1] - 4; // Margen interno
                List<string> lineasDescripcion = DividirTextoEnLineasMejorado(graphics, descripcion, font, maxDescripcionWidth);

                // Precio y Total con formato más compacto
                decimal precio = Convert.ToDecimal(row["precio"]);
                decimal total = Convert.ToDecimal(row["total"]);
                sumaTotal += total;
                
                string precioStr = precio < 1000 ? precio.ToString("C2") : precio.ToString("C0");
                string totalStr = total < 1000 ? total.ToString("C2") : total.ToString("C0");
                
                SizeF precioSize = graphics.MeasureString(precioStr, font);
                SizeF totalSize = graphics.MeasureString(totalStr, font);
                
                float precioX = colX[2] + colWidths[2] - precioSize.Width - 2;
                float totalX = colX[3] + colWidths[3] - totalSize.Width - 2;

                // Imprimir líneas de descripción
                float filaY = filaYInicial;
                for (int i = 0; i < lineasDescripcion.Count; i++)
                {
                    // Cantidad solo en la primera línea
                    if (i == 0)
                    {
                        float cantidadYLinea = filaY + ((rowHeight - cantidadSize.Height) / 2);
                        graphics.DrawString(cantidadStr, font, Brushes.Black, cantidadX, cantidadYLinea);

                        // Precio y Total solo en la primera línea
                        graphics.DrawString(precioStr, font, Brushes.Black, precioX, filaY);
                        graphics.DrawString(totalStr, font, Brushes.Black, totalX, filaY);
                    }

                    // Descripción en cada línea
                    graphics.DrawString(lineasDescripcion[i], font, Brushes.Black, colX[1], filaY);
                    filaY += rowHeight;
                }

                y = filaY;
            }

            return (y, cantidadTotal, sumaTotal);
        }

        private float ImprimirTotales(Graphics graphics, Font fontBold, float leftMargin, float rightMargin, float y, int cantidadTotal, decimal sumaTotal)
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

            return y + totalGeneralSize.Height + 8; // Reducir espacio después de totales
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