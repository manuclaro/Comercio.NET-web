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

        // Configuraciones de tamaño (en pulgadas) - MÁS RECTANGULARES
        private readonly Dictionary<TamañoCartelito, (float ancho, float alto)> tamañosCartelito =
            new Dictionary<TamañoCartelito, (float, float)>
            {
                { TamañoCartelito.Estandar, (3.15f, 1.53f) },    // 8x4 cm - más rectangular
                { TamañoCartelito.Perfumeria, (2.36f, 1.15f) },  // 6x3 cm - más rectangular  
                { TamañoCartelito.Oferta, (6.33f, 2.17f) }       // 11x5.5 cm - más rectangular
            };

        // Configuraciones de layout - MÁS CARTELITOS POR PÁGINA
        private readonly Dictionary<TamañoCartelito, (int columnas, int filas)> layoutCartelitos =
            new Dictionary<TamañoCartelito, (int, int)>
            {
                { TamañoCartelito.Estandar, (2, 6) },     // 12 cartelitos por página
                { TamañoCartelito.Perfumeria, (3, 8) },   // 24 cartelitos por página
                { TamañoCartelito.Oferta, (1, 4) }        // 8 cartelitos por página
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
            printDocument.DefaultPageSettings.Margins = new Margins(30, 30, 30, 30); // Márgenes reducidos
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

            // Calcular espaciado - REDUCIDO
            float espacioHorizontal = Math.Max(5f, (margenes.Width - (layout.columnas * anchoCartelitoPx)) / (layout.columnas + 1));
            float espacioVertical = Math.Max(3f, (margenes.Height - (layout.filas * altoCartelitoPx)) / (layout.filas + 1));

            int cartelitosEnPagina = layout.columnas * layout.filas;
            int productosRestantes = productos.Count - indiceProductoActual;
            int cartelitosAImprimir = Math.Min(cartelitosEnPagina, productosRestantes);

            System.Diagnostics.Debug.WriteLine($"🖨️ Imprimiendo página {indicePaginaActual + 1}");
            System.Diagnostics.Debug.WriteLine($"📦 Productos desde índice {indiceProductoActual}, cantidad: {cartelitosAImprimir}");
            System.Diagnostics.Debug.WriteLine($"📏 Tamaño: {tamañoCartelito} ({tamañoPulgadas.ancho}\"x{tamañoPulgadas.alto}\")");
            System.Diagnostics.Debug.WriteLine($"🔢 Layout: {layout.columnas}x{layout.filas}");

            // Imprimir cartelitos en grid
            for (int i = 0; i < cartelitosAImprimir; i++)
            {
                int fila = i / layout.columnas;
                int columna = i % layout.columnas;

                float x = margenes.Left + espacioHorizontal + columna * (anchoCartelitoPx + espacioHorizontal);
                float y = margenes.Top + espacioVertical + fila * (altoCartelitoPx + espacioVertical);

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

            // Áreas del cartelito - NUEVO LAYOUT
            float padding = rect.Width * 0.04f; // Padding reducido
            var areaContent = new RectangleF(
                rect.X + padding,
                rect.Y + padding,
                rect.Width - (padding * 2),
                rect.Height - (padding * 2)
            );

            // Calcular alturas proporcionales - REORGANIZADO
            float alturaDescripcion = areaContent.Height * 0.30f;  // Descripción
            float alturaMarca = areaContent.Height * 0.10f;        // Marca (nueva)
            float alturaPrecio = areaContent.Height * 0.50f;       // Precio
            float alturaCodigo = areaContent.Height * 0.20f;       // Código (en lugar de rubro)

            float yActual = areaContent.Y;

            // Área para descripción
            var areaDescripcion = new RectangleF(
                areaContent.X,
                yActual,
                areaContent.Width,
                alturaDescripcion
            );
            yActual += alturaDescripcion;

            // Área para marca (NUEVA)
            var areaMarca = new RectangleF(
                areaContent.X,
                yActual,
                areaContent.Width,
                alturaMarca
            );
            yActual += alturaMarca;

            // Área para precio
            var areaPrecio = new RectangleF(
                areaContent.X,
                yActual,
                areaContent.Width,
                alturaPrecio
            );
            yActual += alturaPrecio;

            // Área para código (en lugar de rubro)
            var areaCodigo = new RectangleF(
                areaContent.X,
                yActual,
                areaContent.Width,
                alturaCodigo
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

            // Dibujar marca (NUEVA SECCIÓN)
            if (!string.IsNullOrEmpty(producto.Marca))
            {
                using (var fontMarca = new Font("Arial", configuracionFuente.tamañoMarca, FontStyle.Regular))
                {
                    var formatoMarca = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisCharacter
                    };

                    graphics.DrawString(
                        producto.Marca,
                        fontMarca,
                        Brushes.Gray,
                        areaMarca,
                        formatoMarca
                    );
                }
            }

            // Dibujar precio (SIN FONDO DE COLOR)
            using (var fontPrecio = new Font("Arial", configuracionFuente.tamañoPrecio, FontStyle.Bold))
            {
                var formatoPrecio = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                //// Solo borde simple, sin fondo de color
                //var rectPrecioBorde = areaPrecio;
                //rectPrecioBorde.Inflate(-2, -2);
                //graphics.DrawRectangle(Pens.Black, Rectangle.Round(rectPrecioBorde));

                string textoPrecio = producto.Precio.ToString("C2");
                graphics.DrawString(
                    textoPrecio,
                    fontPrecio,
                    Brushes.Black, // Color negro en lugar de rojo
                    areaPrecio,
                    formatoPrecio
                );
            }

            // Dibujar código (EN LUGAR DE RUBRO)
            using (var fontCodigo = new Font("Arial", configuracionFuente.tamañoCodigo, FontStyle.Regular))
            {
                var formatoCodigo = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                string textoCodigo = $"Cód: {producto.Codigo}";
                graphics.DrawString(
                    textoCodigo,
                    fontCodigo,
                    Brushes.DarkGray,
                    areaCodigo,
                    formatoCodigo
                );
            }

            // Debug: mostrar información del cartelito
            System.Diagnostics.Debug.WriteLine($"  🏷️ {producto.Codigo}: {producto.Descripcion} - {producto.Precio:C2}");
        }

        private (float tamañoDescripcion, float tamañoPrecio, float tamañoMarca, float tamañoCodigo) ObtenerConfiguracionFuente(TamañoCartelito tamaño)
        {
            return tamaño switch
            {
                TamañoCartelito.Perfumeria => (10f, 16, 6f, 6f),   // Fuentes pequeñas
                TamañoCartelito.Estandar => (12f, 32f, 8f, 7f),    // Fuentes medianas
                TamañoCartelito.Oferta => (24f, 48f, 16f, 12f),     // Fuentes grandes
                _ => (7f, 10f, 5f, 5f)
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