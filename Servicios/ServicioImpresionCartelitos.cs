using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using Comercio.NET.Formularios;

namespace Comercio.NET.Servicios
{
    public class ServicioImpresionCartelitos : IDisposable
    {
        private readonly PrintDocument printDocument;
        private readonly List<ProductoCartelito> productos;
        private readonly TamañoCartelito tamaño;
        private int productoActualIndex;
        private bool disposed = false;

        public ServicioImpresionCartelitos(List<ProductoCartelito> productos, TamañoCartelito tamaño)
        {
            this.productos = productos ?? throw new ArgumentNullException(nameof(productos));
            this.tamaño = tamaño;
            this.productoActualIndex = 0;

            printDocument = new PrintDocument();
            printDocument.PrintPage += PrintDocument_PrintPage;
            ConfigurarTamañoPapel();
        }

        private void ConfigurarTamañoPapel()
        {
            PaperSize paperSize;

            switch (tamaño)
            {
                case TamañoCartelito.Termico70mm:
                    // Papel térmico 70mm continuo
                    int anchoTermico = (int)(70 / 25.4 * 100);
                    int altoTermico = (int)(297 / 25.4 * 100); // Largo A4 como base
                    paperSize = new PaperSize("Térmico 70mm", anchoTermico, altoTermico);
                    printDocument.DefaultPageSettings.Margins = new Margins(5, 5, 5, 5);
                    break;

                case TamañoCartelito.Estandar:
                case TamañoCartelito.Perfumeria:
                case TamañoCartelito.Oferta:
                    paperSize = new PaperSize("A4", 827, 1169);
                    printDocument.DefaultPageSettings.Margins = new Margins(15, 15, 15, 15);
                    break;

                default:
                    paperSize = new PaperSize("A4", 827, 1169);
                    printDocument.DefaultPageSettings.Margins = new Margins(15, 15, 15, 15);
                    break;
            }

            printDocument.DefaultPageSettings.PaperSize = paperSize;
        }

        public void MostrarVistaPrevia()
        {
            using (var previewDialog = new PrintPreviewDialog())
            {
                previewDialog.Document = printDocument;
                previewDialog.WindowState = FormWindowState.Maximized;
                previewDialog.ShowDialog();
            }
        }

        public void Imprimir()
        {
            printDocument.Print();
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            try
            {
                if (tamaño == TamañoCartelito.Termico70mm)
                {
                    ImprimirCartelitoTermico(e);
                }
                else
                {
                    ImprimirCartelitosA4(e);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CARTELITOS] ❌ Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Imprime cartelitos en papel térmico 70mm (varios uno debajo del otro)
        /// </summary>
        private void ImprimirCartelitoTermico(PrintPageEventArgs e)
        {
            float leftMargin = e.MarginBounds.Left;
            float topMargin = e.MarginBounds.Top;
            float anchoDisponible = e.MarginBounds.Width;
            float y = topMargin;
            float altoMaximoPagina = e.MarginBounds.Height;

            Font fontDescripcion = new Font("Arial", 13F, FontStyle.Bold);
            Font fontMarca = new Font("Arial", 10F, FontStyle.Regular);
            Font fontPrecio = new Font("Arial", 24F, FontStyle.Bold);
            Font fontCodigo = new Font("Arial", 8F, FontStyle.Regular);

            try
            {
                bool primerProductoEnPagina = true;

                while (productoActualIndex < productos.Count)
                {
                    var producto = productos[productoActualIndex];
                    float yInicio = y;

                    // Calcular altura necesaria para este producto
                    var lineasDescripcion = DividirTextoEnLineas(e.Graphics, producto.Descripcion, fontDescripcion, anchoDisponible);
                    float alturaDescripcion = lineasDescripcion.Sum(linea => e.Graphics.MeasureString(linea, fontDescripcion).Height);
                    float alturaMarca = !string.IsNullOrEmpty(producto.Marca) ? e.Graphics.MeasureString(producto.Marca, fontMarca).Height + 5 : 0;
                    float alturaPrecio = e.Graphics.MeasureString(producto.Precio.ToString("C2"), fontPrecio).Height + 10;
                    float alturaCodigo = e.Graphics.MeasureString($"Cód: {producto.Codigo}", fontCodigo).Height + 8;
                    
                    // ✅ ALTURA TOTAL REDUCIDA (menos espaciado)
                    float alturaTotal = alturaDescripcion + alturaMarca + alturaPrecio + alturaCodigo + 15;

                    // Si no cabe en esta página, pasar a la siguiente
                    if (!primerProductoEnPagina && y + alturaTotal > topMargin + altoMaximoPagina)
                    {
                        e.HasMorePages = true;
                        return;
                    }

                    // DESCRIPCIÓN (centrada)
                    foreach (var linea in lineasDescripcion)
                    {
                        SizeF lineaSize = e.Graphics.MeasureString(linea, fontDescripcion);
                        float x = leftMargin + (anchoDisponible - lineaSize.Width) / 2;
                        e.Graphics.DrawString(linea, fontDescripcion, Brushes.Black, x, y);
                        y += lineaSize.Height;
                    }

                    // ✅ ESPACIADO REDUCIDO
                    y += 3;

                    // MARCA (centrada)
                    if (!string.IsNullOrEmpty(producto.Marca))
                    {
                        SizeF marcaSize = e.Graphics.MeasureString(producto.Marca, fontMarca);
                        float marcaX = leftMargin + (anchoDisponible - marcaSize.Width) / 2;
                        e.Graphics.DrawString(producto.Marca, fontMarca, Brushes.Black, marcaX, y);
                        // ✅ ESPACIADO REDUCIDO
                        y += marcaSize.Height + 6;
                    }

                    // PRECIO (centrado y grande)
                    string precioTexto = producto.Precio.ToString("C2");
                    SizeF precioSize = e.Graphics.MeasureString(precioTexto, fontPrecio);
                    float precioX = leftMargin + (anchoDisponible - precioSize.Width) / 2;
                    e.Graphics.DrawString(precioTexto, fontPrecio, Brushes.Black, precioX, y);
                    // ✅ ESPACIADO REDUCIDO
                    y += precioSize.Height + 5;

                    // CÓDIGO (centrado)
                    string codigoTexto = $"Cód: {producto.Codigo}";
                    SizeF codigoSize = e.Graphics.MeasureString(codigoTexto, fontCodigo);
                    float codigoX = leftMargin + (anchoDisponible - codigoSize.Width) / 2;
                    e.Graphics.DrawString(codigoTexto, fontCodigo, Brushes.Black, codigoX, y);
                    // ✅ ESPACIADO REDUCIDO
                    y += codigoSize.Height + 12;

                    // Línea separadora entre productos
                    if (productoActualIndex < productos.Count - 1)
                    {
                        e.Graphics.DrawLine(new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }, 
                            leftMargin, y, leftMargin + anchoDisponible, y);
                        // ✅ ESPACIADO REDUCIDO
                        y += 10;
                    }

                    productoActualIndex++;
                    primerProductoEnPagina = false;
                }

                e.HasMorePages = false;
            }
            finally
            {
                fontDescripcion.Dispose();
                fontMarca.Dispose();
                fontPrecio.Dispose();
                fontCodigo.Dispose();
            }
        }

        /// <summary>
        /// Imprime múltiples cartelitos en A4
        /// </summary>
        private void ImprimirCartelitosA4(PrintPageEventArgs e)
        {
            var (columnas, filas, ancho, alto) = ObtenerLayoutCartelito();

            float leftMargin = e.MarginBounds.Left;
            float topMargin = e.MarginBounds.Top;

            for (int fila = 0; fila < filas && productoActualIndex < productos.Count; fila++)
            {
                for (int columna = 0; columna < columnas && productoActualIndex < productos.Count; columna++)
                {
                    float x = leftMargin + (columna * ancho);
                    float y = topMargin + (fila * alto);

                    DibujarCartelitoA4(e.Graphics, productos[productoActualIndex], x, y, ancho, alto);
                    productoActualIndex++;
                }
            }

            e.HasMorePages = productoActualIndex < productos.Count;
        }

        private (int columnas, int filas, float ancho, float alto) ObtenerLayoutCartelito()
        {
            return tamaño switch
            {
                // ✅ ESTÁNDAR: ALTURA REDUCIDA de 157 a 135 (más corto)
                TamañoCartelito.Estandar => (3, 7, 252, 135),
                
                TamañoCartelito.Perfumeria => (4, 10, 189, 94),
                
                TamañoCartelito.Oferta => (1, 4, 756, 270),
                
                _ => (3, 7, 252, 135)
            };
        }

        /// <summary>
        /// Dibuja un cartelito individual en A4 con formato centralizado
        /// </summary>
        private void DibujarCartelitoA4(Graphics g, ProductoCartelito producto, float x, float y, float ancho, float alto)
        {
            // Borde rectangular
            g.DrawRectangle(Pens.Black, x, y, ancho, alto);

            // Fuentes según tamaño
            Font fontDescripcion, fontMarca, fontPrecio, fontCodigo;

            if (tamaño == TamañoCartelito.Perfumeria) // Chiquito
            {
                fontDescripcion = new Font("Arial", 8F, FontStyle.Bold);
                fontMarca = new Font("Arial", 6F, FontStyle.Regular);
                fontPrecio = new Font("Arial", 16F, FontStyle.Bold);
                fontCodigo = new Font("Arial", 6F, FontStyle.Regular);
            }
            else if (tamaño == TamañoCartelito.Oferta) // Grande
            {
                fontDescripcion = new Font("Arial", 32F, FontStyle.Bold);
                fontMarca = new Font("Arial", 22F, FontStyle.Regular);
                fontPrecio = new Font("Arial", 80F, FontStyle.Bold);
                fontCodigo = new Font("Arial", 16F, FontStyle.Regular);
            }
            else // Estándar
            {
                fontDescripcion = new Font("Arial", 12F, FontStyle.Bold);
                fontMarca = new Font("Arial", 9F, FontStyle.Regular);
                fontPrecio = new Font("Arial", 26F, FontStyle.Bold);
                fontCodigo = new Font("Arial", 7F, FontStyle.Regular);
            }

            try
            {
                float margenInternoX = 8;
                float anchoTexto = ancho - (margenInternoX * 2);
                // ✅ MARGEN SUPERIOR REDUCIDO
                float yActual = y + 6;

                // 1. DESCRIPCIÓN (centrada, arriba)
                var lineasDesc = DividirTextoEnLineas(g, producto.Descripcion, fontDescripcion, anchoTexto);
                foreach (var linea in lineasDesc)
                {
                    SizeF lineaSize = g.MeasureString(linea, fontDescripcion);
                    float xCentrado = x + (ancho - lineaSize.Width) / 2;
                    g.DrawString(linea, fontDescripcion, Brushes.Black, xCentrado, yActual);
                    yActual += lineaSize.Height;
                }

                // ✅ ESPACIADO REDUCIDO
                yActual += 3;

                // 2. MARCA (centrada, debajo de descripción)
                if (!string.IsNullOrEmpty(producto.Marca))
                {
                    SizeF marcaSize = g.MeasureString(producto.Marca, fontMarca);
                    float marcaX = x + (ancho - marcaSize.Width) / 2;
                    g.DrawString(producto.Marca, fontMarca, Brushes.Black, marcaX, yActual);
                }

                // 3. PRECIO (centrado, posicionado más abajo)
                string precioTexto = producto.Precio.ToString("C2");
                SizeF precioSize = g.MeasureString(precioTexto, fontPrecio)
                    ;
                // ✅ AJUSTADO PARA FORMATO MÁS CORTO (58% del alto)
                float precioY = y + (alto * 0.65F) - (precioSize.Height / 2);
                float precioX = x + (ancho - precioSize.Width) / 2;
                g.DrawString(precioTexto, fontPrecio, Brushes.Black, precioX, precioY);

                // 4. CÓDIGO (centrado, abajo)
                string codigoTexto = $"Cód: {producto.Codigo}";
                SizeF codigoSize = g.MeasureString(codigoTexto, fontCodigo);
                float codigoX = x + (ancho - codigoSize.Width) / 2;
                // ✅ MARGEN INFERIOR REDUCIDO
                float codigoY = y + alto - codigoSize.Height - 5;
                g.DrawString(codigoTexto, fontCodigo, Brushes.Black, codigoX, codigoY);
            }
            finally
            {
                fontDescripcion.Dispose();
                fontMarca.Dispose();
                fontPrecio.Dispose();
                fontCodigo.Dispose();
            }
        }

        private List<string> DividirTextoEnLineas(Graphics g, string texto, Font font, float anchoMaximo)
        {
            var lineas = new List<string>();
            if (string.IsNullOrEmpty(texto)) return lineas;

            var palabras = texto.Split(' ');
            string lineaActual = "";

            foreach (var palabra in palabras)
            {
                string pruebaLinea = string.IsNullOrEmpty(lineaActual) ? palabra : lineaActual + " " + palabra;
                SizeF size = g.MeasureString(pruebaLinea, font);

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
                        lineas.Add(palabra);
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
    }
}