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
        private readonly List<ProductoCartelito> productos;
        private readonly TamañoCartelito tamañoCartelito;
        private readonly PrintDocument printDocument;
        private int indicePaginaActual = 0;
        private int indiceProductoActual = 0;
        private bool disposed = false;

        // Configuraciones de tamaño (en pulgadas)
        private readonly Dictionary<TamañoCartelito, (float ancho, float alto)> tamañosCartelito = 
            new Dictionary<TamañoCartelito, (float, float)>
            {
                { TamañoCartelito.Estandar, (2.76f, 1.97f) },    // 7x5 cm
                { TamañoCartelito.Perfumeria, (1.97f, 1.18f) },  // 5x3 cm
                { TamañoCartelito.Oferta, (3.94f, 2.76f) }       // 10x7 cm
            };

        // Configuraciones de layout
        private readonly Dictionary<TamañoCartelito, (int columnas, int filas)> layoutCartelitos = 
            new Dictionary<TamañoCartelito, (int, int)>
            {
                { TamañoCartelito.Estandar, (3, 4) },     // 12 cartelitos por página
                { TamañoCartelito.Perfumeria, (4, 6) },   // 24 cartelitos por página
                { TamañoCartelito.Oferta, (2, 3) }        // 6 cartelitos por página
            };

        public ServicioImpresionCartelitos(List<ProductoCartelito> productos, TamañoCartelito tamaño)
        {
            this.productos = productos ?? throw new ArgumentNullException(nameof(productos));
            this.tamañoCartelito = tamaño;
            
            printDocument = new PrintDocument();
            printDocument.PrintPage += PrintDocument_PrintPage;
            ConfigurarPagina();
        }

        private void ConfigurarPagina()
        {
            // Configurar tamaño de página A4
            printDocument.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169); // A4 en centésimas de pulgada
            printDocument.DefaultPageSettings.Margins = new Margins(50, 50, 50, 50); // Márgenes mínimos
            printDocument.DefaultPageSettings.Landscape = false;
        }

        public void MostrarVistaPrevia()
        {
            try
            {
                using (var previewDialog = new PrintPreviewDialog())
                {
                    previewDialog.Document = printDocument;
                    previewDialog.WindowState = FormWindowState.Maximized;
                    previewDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en vista previa: {ex.Message}", ex);
            }
        }

        public void Imprimir()
        {
            try
            {
                printDocument.Print();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al imprimir: {ex.Message}", ex);
            }
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            var graphics = e.Graphics;
            var margenes = e.MarginBounds;
            
            // Obtener configuraciones para el tamaño seleccionado
            var tamañoPulgadas = tamañosCartelito[tamañoCartelito];
            var layout = layoutCartelitos[tamañoCartelito];
            
            // Convertir tamaño a píxeles (96 DPI)
            float anchoCartelitoPx = tamañoPulgadas.ancho * 96f;
            float altoCartelitoPx = tamañoPulgadas.alto * 96f;
            
            // Calcular espaciado
            float espacioHorizontal = (margenes.Width - (layout.columnas * anchoCartelitoPx)) / (layout.columnas - 1);
            float espacioVertical = (margenes.Height - (layout.filas * altoCartelitoPx)) / (layout.filas - 1);
            
            int cartelitosEnPagina = layout.columnas * layout.filas;
            int productosRestantes = productos.Count - indiceProductoActual;
            int cartelitosAImprimir = Math.Min(cartelitosEnPagina, productosRestantes);
            
            System.Diagnostics.Debug.WriteLine($"??? Imprimiendo página {indicePaginaActual + 1}");
            System.Diagnostics.Debug.WriteLine($"?? Productos desde índice {indiceProductoActual}, cantidad: {cartelitosAImprimir}");
            System.Diagnostics.Debug.WriteLine($"?? Tamaño: {tamañoCartelito} ({tamañoPulgadas.ancho}\"x{tamañoPulgadas.alto}\")");
            System.Diagnostics.Debug.WriteLine($"?? Layout: {layout.columnas}x{layout.filas}");

            // Imprimir cartelitos en grid
            for (int i = 0; i < cartelitosAImprimir; i++)
            {
                int fila = i / layout.columnas;
                int columna = i % layout.columnas;
                
                float x = margenes.Left + columna * (anchoCartelitoPx + espacioHorizontal);
                float y = margenes.Top + fila * (altoCartelitoPx + espacioVertical);
                
                var rectCartelito = new RectangleF(x, y, anchoCartelitoPx, altoCartelitoPx);
                var producto = productos[indiceProductoActual + i];
                
                DibujarCartelito(graphics, rectCartelito, producto);
            }
            
            indiceProductoActual += cartelitosAImprimir;
            indicePaginaActual++;
            
            // Verificar si hay más páginas
            e.HasMorePages = indiceProductoActual < productos.Count;
            
            if (!e.HasMorePages)
            {
                // Resetear para próxima impresión
                indiceProductoActual = 0;
                indicePaginaActual = 0;
            }
        }

        private void DibujarCartelito(Graphics graphics, RectangleF rect, ProductoCartelito producto)
        {
            // Configurar calidad de renderizado
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Borde del cartelito
            using (var borderPen = new Pen(Color.Black, 1f))
            {
                graphics.DrawRectangle(borderPen, Rectangle.Round(rect));
            }

            // Configuración de fuentes según el tamaño
            var configuracionFuente = ObtenerConfiguracionFuente(tamañoCartelito);

            // Áreas del cartelito
            float padding = rect.Width * 0.05f; // 5% de padding
            var areaContent = new RectangleF(
                rect.X + padding,
                rect.Y + padding,
                rect.Width - (padding * 2),
                rect.Height - (padding * 2)
            );

            // Calcular alturas proporcionales
            float alturaDescripcion = areaContent.Height * 0.50f;
            float alturaPrecio = areaContent.Height * 0.35f;
            float alturaMarca = areaContent.Height * 0.15f;

            // Área para descripción
            var areaDescripcion = new RectangleF(
                areaContent.X,
                areaContent.Y,
                areaContent.Width,
                alturaDescripcion
            );

            // Área para precio (centrada y destacada)
            var areaPrecio = new RectangleF(
                areaContent.X,
                areaDescripcion.Bottom,
                areaContent.Width,
                alturaPrecio
            );

            // Área para marca (parte inferior)
            var areaMarca = new RectangleF(
                areaContent.X,
                areaPrecio.Bottom,
                areaContent.Width,
                alturaMarca
            );

            // Dibujar descripción del producto
            using (var fontDescripcion = new Font("Arial", configuracionFuente.tamañoDescripcion, FontStyle.Bold))
            {
                var formatoDescripcion = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.Word
                };

                graphics.DrawString(
                    producto.Descripcion,
                    fontDescripcion,
                    Brushes.Black,
                    areaDescripcion,
                    formatoDescripcion
                );
            }

            // Dibujar precio (destacado)
            using (var fontPrecio = new Font("Arial", configuracionFuente.tamañoPrecio, FontStyle.Bold))
            {
                var formatoPrecio = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                // Fondo del precio
                using (var brushPrecio = new SolidBrush(Color.FromArgb(255, 255, 200)))
                {
                    var rectPrecioFondo = areaPrecio;
                    rectPrecioFondo.Inflate(-2, -2);
                    graphics.FillRectangle(brushPrecio, rectPrecioFondo);
                    graphics.DrawRectangle(Pens.DarkGray, Rectangle.Round(rectPrecioFondo));
                }

                string textoPrecio = producto.Precio.ToString("C2");
                graphics.DrawString(
                    textoPrecio,
                    fontPrecio,
                    Brushes.DarkRed,
                    areaPrecio,
                    formatoPrecio
                );
            }

            // Dibujar marca e información adicional
            using (var fontMarca = new Font("Arial", configuracionFuente.tamañoMarca, FontStyle.Regular))
            {
                var formatoMarca = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter
                };

                string textoMarca = !string.IsNullOrEmpty(producto.Marca) 
                    ? producto.Marca 
                    : $"Cód: {producto.Codigo}";

                graphics.DrawString(
                    textoMarca,
                    fontMarca,
                    Brushes.DarkGray,
                    areaMarca,
                    formatoMarca
                );
            }

            // Debug: mostrar información del cartelito
            System.Diagnostics.Debug.WriteLine($"  ??? {producto.Codigo}: {producto.Descripcion} - {producto.Precio:C2}");
        }

        private (float tamañoDescripcion, float tamañoPrecio, float tamañoMarca) ObtenerConfiguracionFuente(TamañoCartelito tamaño)
        {
            return tamaño switch
            {
                TamañoCartelito.Perfumeria => (6f, 8f, 5f),   // Fuentes pequeñas
                TamañoCartelito.Estandar => (8f, 12f, 6f),    // Fuentes medianas
                TamañoCartelito.Oferta => (12f, 18f, 8f),     // Fuentes grandes
                _ => (8f, 12f, 6f)
            };
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