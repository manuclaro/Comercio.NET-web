using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace Comercio.NET.Formularios
{
    public partial class ResumenIvaForm : Form
    {
        // Reemplazado: ahora soportamos un rango (desde → hasta)
        private DateTime fechaDesde;
        private DateTime fechaHasta;
        private bool esCtaCte;
        private List<ResumenIvaGroup> datosResumenActual; // NUEVO: Almacenar datos para impresión

        // Constructor original (mantener compatibilidad): fecha única
        public ResumenIvaForm(DateTime fecha, bool esCuentaCorriente)
        {
            this.fechaDesde = fecha.Date;
            this.fechaHasta = fecha.Date;
            this.esCtaCte = esCuentaCorriente;

            InitializeComponent();
            ConfigurarFormulario();
            CargarResumenIva();
        }

        // NUEVO: Constructor que acepta rango (desde, hasta, esCuentaCorriente)
        public ResumenIvaForm(DateTime desde, DateTime hasta, bool esCuentaCorriente)
        {
            this.fechaDesde = desde.Date;
            this.fechaHasta = hasta.Date;
            this.esCtaCte = esCuentaCorriente;

            InitializeComponent();
            ConfigurarFormulario();
            CargarResumenIva();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Texto del formulario según rango o fecha única
            if (fechaDesde == fechaHasta)
                this.Text = $"Resumen de IVA - {fechaDesde:dd/MM/yyyy}";
            else
                this.Text = $"Resumen de IVA - {fechaDesde:dd/MM/yyyy} → {fechaHasta:dd/MM/yyyy}";

            this.Size = new Size(650, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.White;

            // PASO 1: Panel superior - Título (AGREGAR PRIMERO)
            var panelTitulo = new Panel
            {
                Name = "panelTitulo",
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(0, 120, 215),
                Padding = new Padding(20, 10, 20, 10)
            };

            var lblTitulo = new Label
            {
                Text = "📊 Resumen de IVA por Alícuotas", // MODIFICADO: Indicar que incluye todas las ventas
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            panelTitulo.Controls.Add(lblTitulo);
            this.Controls.Add(panelTitulo); // AGREGAR PRIMERO

            // PASO 2: Panel inferior con totales y botones (AGREGAR SEGUNDO)
            var panelInferior = new Panel
            {
                Name = "panelInferior",
                Dock = DockStyle.Bottom,
                Height = 110,
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(20, 10, 20, 10)
            };

            // REORGANIZACIÓN: IVA Total primero y más prominente
            var lblTotalIVA = new Label
            {
                Name = "lblTotalIVA",
                Text = "💰 IVA TOTAL: $0,00",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold), // MÁS GRANDE
                ForeColor = Color.FromArgb(220, 53, 69), // Rojo destacado
                AutoSize = true,
                Location = new Point(20, 5) // MÁS ARRIBA
            };

            // Total General - segundo lugar
            var lblTotalGeneral = new Label
            {
                Name = "lblTotalGeneral",
                Text = "Total General: $0,00",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold), // REDUCIDO
                ForeColor = Color.FromArgb(0, 120, 215),
                AutoSize = true,
                Location = new Point(20, 30) // AJUSTADO
            };

            // Subtotal - tercer lugar
            var lblSubtotalSinIVA = new Label
            {
                Name = "lblSubtotalSinIVA",
                Text = "Subtotal sin IVA: $0,00",
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(108, 117, 125),
                AutoSize = true,
                Location = new Point(20, 55) // AJUSTADO
            };

            // NUEVO: Botón Imprimir
            var btnImprimir = new Button
            {
                Text = "🖨️ Imprimir",
                Size = new Size(100, 35),
                Location = new Point(330, 15),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnImprimir.FlatAppearance.BorderSize = 0;
            btnImprimir.Click += BtnImprimir_Click;

            // NUEVO: Botón Exportar
            var btnExportar = new Button
            {
                Text = "📊 Exportar",
                Size = new Size(100, 35),
                Location = new Point(440, 15),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExportar.FlatAppearance.BorderSize = 0;
            btnExportar.Click += BtnExportar_Click;

            // Botón cerrar
            var btnCerrar = new Button
            {
                Text = "Cerrar",
                Size = new Size(80, 35),
                Location = new Point(550, 15),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();

            // NUEVO ORDEN en los controles: IVA Total primero
            panelInferior.Controls.AddRange(new Control[] {
                lblTotalIVA, lblTotalGeneral, lblSubtotalSinIVA, // NUEVO ORDEN
                btnImprimir, btnExportar, btnCerrar
            });
            this.Controls.Add(panelInferior); // AGREGAR SEGUNDO

            // PASO 3: DataGridView para mostrar el resumen (AGREGAR AL FINAL)
            var dgvResumen = new DataGridView
            {
                Name = "dgvResumen",
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EnableHeadersVisualStyles = false,
                ColumnHeadersVisible = true,
                Dock = DockStyle.Fill
            };

            // Estilo del header - MEJORADO para mejor visibilidad
            var headerStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(230, 240, 250), // Color más contrastante
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                SelectionBackColor = Color.FromArgb(230, 240, 250),
                WrapMode = DataGridViewTriState.True
            };
            dgvResumen.ColumnHeadersDefaultCellStyle = headerStyle;
            dgvResumen.ColumnHeadersHeight = 45; // AUMENTADO: de 40 a 45 para mejor visibilidad

            // MODIFICADO: Panel contenedor con más padding superior
            var panelContenido = new Panel
            {
                Name = "panelContenido",
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 80, 15, 10), // AUMENTADO: padding superior de 10 a 25
                BackColor = Color.White
            };

            panelContenido.Controls.Add(dgvResumen);
            this.Controls.Add(panelContenido); // AGREGAR AL FINAL

            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    this.Close();
                }
            };
        }

        private async void CargarResumenIva()
        {
            try
            {
                var dgvResumen = this.Controls.Find("dgvResumen", true).FirstOrDefault() as DataGridView;
                if (dgvResumen == null) return;

                // Configurar columnas del DataGridView
                dgvResumen.Columns.Clear();

                // IMPORTANTE: Establecer el modo de auto-tamaño ANTES de agregar columnas
                dgvResumen.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

                // NUEVO ORDEN: 1- Alícuota, 2- Productos, 3- Monto IVA, 4- Base Imponible, 5- Total con IVA
                dgvResumen.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Alicuota",
                    HeaderText = "Alícuota IVA",
                    Width = 120,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(0, 120, 215)
                    }
                });

                dgvResumen.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "CantidadProductos",
                    HeaderText = "Productos",
                    Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter
                    }
                });

                // COLUMNA MÁS PROMINENTE: Monto IVA ahora en 3ra posición
                dgvResumen.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "MontoIVA",
                    HeaderText = "💰 Monto IVA", // NUEVO: Emoji para mayor prominencia
                    Width = 130, // AUMENTADO: de 120 a 130 para mayor prominencia
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "C2",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        ForeColor = Color.FromArgb(220, 53, 69), // Rojo para destacar
                        Font = new Font("Segoe UI", 11F, FontStyle.Bold), // AUMENTADO: de 10F a 11F
                        BackColor = Color.FromArgb(255, 248, 248) // NUEVO: Fondo rosado muy claro
                    }
                });

                dgvResumen.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "BaseImponible",
                    HeaderText = "Base Imponible",
                    Width = 120, // REDUCIDO: de 130 a 120
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "C2",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        BackColor = Color.FromArgb(248, 252, 255)
                    }
                });

                dgvResumen.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "TotalConIVA",
                    HeaderText = "Total con IVA",
                    Width = 130, // REDUCIDO: de 140 a 130
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "C2",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        BackColor = Color.FromArgb(240, 248, 255),
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                    }
                });

                // Cargar datos
                datosResumenActual = await ObtenerResumenIvaPorAlicuota();

                if (datosResumenActual?.Count > 0)
                {
                    decimal totalGeneral = 0;
                    decimal totalIVA = 0;
                    decimal totalSinIVA = 0;

                    foreach (var grupo in datosResumenActual.OrderByDescending(x => x.PorcentajeIVA))
                    {
                        var row = dgvResumen.Rows[dgvResumen.Rows.Add()];

                        // NUEVO ORDEN de asignación de valores
                        row.Cells["Alicuota"].Value = $"{grupo.PorcentajeIVA:N2}%";
                        row.Cells["CantidadProductos"].Value = grupo.CantidadProductos;
                        row.Cells["MontoIVA"].Value = grupo.MontoIVA; // AHORA EN 3RA POSICIÓN
                        row.Cells["BaseImponible"].Value = grupo.BaseImponible; // AHORA EN 4TA POSICIÓN
                        row.Cells["TotalConIVA"].Value = grupo.TotalConIVA; // AHORA EN 5TA POSICIÓN

                        // Aplicar color de fondo según alícuota
                        Color colorFondo = grupo.PorcentajeIVA switch
                        {
                            21.00m => Color.FromArgb(255, 245, 245), // Rosa claro para 21%
                            10.50m => Color.FromArgb(245, 255, 245), // Verde claro para 10.5%
                            6.63m => Color.FromArgb(245, 245, 255),  // Azul claro para 6.63%
                            _ => Color.White
                        };

                        // Aplicar color a todas las celdas EXCEPTO MontoIVA (que ya tiene su propio estilo destacado)
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            if (cell.OwningColumn.Name != "MontoIVA")
                            {
                                cell.Style.BackColor = colorFondo;
                            }
                        }

                        totalGeneral += grupo.TotalConIVA;
                        totalIVA += grupo.MontoIVA;
                        totalSinIVA += grupo.BaseImponible;
                    }

                    // NUEVO ORDEN DE PROMINENCIA EN LOS LABELS: IVA Total primero y más grande
                    var lblTotalIVA = this.Controls.Find("lblTotalIVA", true).FirstOrDefault() as Label;
                    var lblTotalGeneral = this.Controls.Find("lblTotalGeneral", true).FirstOrDefault() as Label;
                    var lblSubtotalSinIVA = this.Controls.Find("lblSubtotalSinIVA", true).FirstOrDefault() as Label;

                    // IVA TOTAL - MÁS PROMINENTE
                    if (lblTotalIVA != null)
                    {
                        lblTotalIVA.Text = $"💰 IVA TOTAL: {totalIVA:C2}"; // NUEVO: Emoji y texto en mayúsculas
                        lblTotalIVA.Font = new Font("Segoe UI", 16F, FontStyle.Bold); // AUMENTADO: de 12F a 16F
                        lblTotalIVA.ForeColor = Color.FromArgb(220, 53, 69); // Mantener color rojo
                    }

                    if (lblTotalGeneral != null)
                    {
                        lblTotalGeneral.Text = $"Total General: {totalGeneral:C2}";
                        lblTotalGeneral.Font = new Font("Segoe UI", 12F, FontStyle.Bold); // REDUCIDO: de 14F a 12F
                    }

                    if (lblSubtotalSinIVA != null)
                        lblSubtotalSinIVA.Text = $"Subtotal sin IVA: {totalSinIVA:C2}";
                }
                else
                {
                    datosResumenActual = new List<ResumenIvaGroup>();

                    // Agregar fila indicando que no hay datos
                    var row = dgvResumen.Rows[dgvResumen.Rows.Add()];
                    row.Cells["Alicuota"].Value = "Sin datos para este rango";
                    row.Cells["CantidadProductos"].Value = 0;
                    row.Cells["MontoIVA"].Value = 0; // NUEVO ORDEN
                    row.Cells["BaseImponible"].Value = 0; // NUEVO ORDEN
                    row.Cells["TotalConIVA"].Value = 0; // NUEVO ORDEN

                    // Centrar el texto "Sin datos"
                    row.Cells["Alicuota"].Style.Font = new Font("Segoe UI", 10F, FontStyle.Italic);
                    row.Cells["Alicuota"].Style.ForeColor = Color.Gray;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el resumen de IVA: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NUEVO: Event handler para el botón Imprimir
        private void BtnImprimir_Click(object sender, EventArgs e)
        {
            if (datosResumenActual == null || datosResumenActual.Count == 0)
            {
                MessageBox.Show("No hay datos para imprimir.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var printDocument = new PrintDocument();
                printDocument.PrintPage += PrintDocument_PrintPage;

                // Mostrar diálogo de vista previa
                var printPreviewDialog = new PrintPreviewDialog
                {
                    Document = printDocument,
                    UseAntiAlias = true,
                    WindowState = FormWindowState.Maximized
                };

                printPreviewDialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NUEVO: Event handler para el botón Exportar
        private void BtnExportar_Click(object sender, EventArgs e)
        {
            if (datosResumenActual == null || datosResumenActual.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var saveDialog = new SaveFileDialog())
            {
                string tipoVenta = esCtaCte ? "CuentaCorriente" : "";

                string nombreArchivo;
                if (fechaDesde == fechaHasta)
                    nombreArchivo = esCtaCte ? $"ResumenIVA_{fechaDesde:yyyyMMdd}_{tipoVenta}" : $"ResumenIVA_{fechaDesde:yyyyMMdd}";
                else
                    nombreArchivo = esCtaCte ? $"ResumenIVA_{fechaDesde:yyyyMMdd}_{fechaHasta:yyyyMMdd}_{tipoVenta}" : $"ResumenIVA_{fechaDesde:yyyyMMdd}_{fechaHasta:yyyyMMdd}";

                saveDialog.Filter = "Archivos CSV|*.csv|Archivos de Excel|*.xlsx";
                saveDialog.FileName = nombreArchivo;

                if (saveDialog.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        if (saveDialog.FileName.EndsWith(".csv"))
                        {
                            ExportarACSV(saveDialog.FileName);
                        }
                        else
                        {
                            // Para .xlsx, por ahora exportar como CSV también
                            // En el futuro se podría implementar exportación real a Excel
                            ExportarACSV(saveDialog.FileName);
                        }

                        MessageBox.Show("Datos exportados correctamente.", "Éxito",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al exportar: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // NUEVO: Método para imprimir el reporte - ORDEN ACTUALIZADO Y ESPACIADO CORREGIDO
        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics graphics = e.Graphics;
            Font fontTitulo = new Font("Arial", 16, FontStyle.Bold);
            Font fontSubtitulo = new Font("Arial", 12, FontStyle.Bold);
            Font fontNormal = new Font("Arial", 10);
            Font fontPequeño = new Font("Arial", 9);
            Font fontIvaDestacado = new Font("Arial", 11, FontStyle.Bold); // Para destacar IVA pero en negro

            float y = 50;
            float leftMargin = 50;
            float rightMargin = e.PageBounds.Width - 50;

            // Título principal
            string titulo = $"RESUMEN DE IVA POR ALÍCUOTAS";
            SizeF tituloSize = graphics.MeasureString(titulo, fontTitulo);
            float tituloX = (e.PageBounds.Width - tituloSize.Width) / 2;
            graphics.DrawString(titulo, fontTitulo, Brushes.Black, tituloX, y);
            y += tituloSize.Height + 10;

            // Información del reporte (fecha o rango)
            string info = fechaDesde == fechaHasta
                ? $"Fecha: {fechaDesde:dd/MM/yyyy}"
                : $"Rango: {fechaDesde:dd/MM/yyyy} → {fechaHasta:dd/MM/yyyy}";
            SizeF infoSize = graphics.MeasureString(info, fontSubtitulo);
            float infoX = (e.PageBounds.Width - infoSize.Width) / 2;
            graphics.DrawString(info, fontSubtitulo, Brushes.Black, infoX, y);
            y += infoSize.Height + 20;

            // Fecha de generación
            string fechaGen = $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
            graphics.DrawString(fechaGen, fontPequeño, Brushes.Gray, rightMargin - graphics.MeasureString(fechaGen, fontPequeño).Width, y);
            y += 30;

            // NUEVO ESPACIADO DE COLUMNAS - MÁS SEPARADAS
            float colX1 = leftMargin;        // Alícuota
            float colX2 = leftMargin + 100;  // Productos
            float colX3 = leftMargin + 200;  // *** MONTO IVA (más espacio: era 180, ahora 200) ***
            float colX4 = leftMargin + 330;  // Base Imponible (más espacio: era 300, ahora 330)
            float colX5 = leftMargin + 480;  // Total con IVA (más espacio: era 450, ahora 480)

            // Encabezados de tabla - NUEVO ORDEN Y TODO EN NEGRO
            graphics.DrawString("Alícuota", fontSubtitulo, Brushes.Black, colX1, y);
            graphics.DrawString("Productos", fontSubtitulo, Brushes.Black, colX2, y);
            graphics.DrawString("MONTO IVA", fontSubtitulo, Brushes.Black, colX3, y); // CAMBIO: De rojo a negro
            graphics.DrawString("Base Imponible", fontSubtitulo, Brushes.Black, colX4, y);
            graphics.DrawString("Total con IVA", fontSubtitulo, Brushes.Black, colX5, y);
            y += 25;

            // Línea separadora
            graphics.DrawLine(Pens.Black, leftMargin, y, rightMargin, y);
            y += 10;

            // Datos - NUEVO ORDEN Y TODO EN NEGRO
            decimal totalGeneral = 0;
            decimal totalIVA = 0;
            decimal totalSinIVA = 0;

            foreach (var grupo in datosResumenActual.OrderByDescending(x => x.PorcentajeIVA))
            {
                graphics.DrawString($"{grupo.PorcentajeIVA:N2}%", fontNormal, Brushes.Black, colX1, y);
                graphics.DrawString(grupo.CantidadProductos.ToString(), fontNormal, Brushes.Black, colX2, y);
                graphics.DrawString(grupo.MontoIVA.ToString("C2"), fontIvaDestacado, Brushes.Black, colX3, y); // CAMBIO: De rojo a negro
                graphics.DrawString(grupo.BaseImponible.ToString("C2"), fontNormal, Brushes.Black, colX4, y);
                graphics.DrawString(grupo.TotalConIVA.ToString("C2"), fontNormal, Brushes.Black, colX5, y);

                totalGeneral += grupo.TotalConIVA;
                totalIVA += grupo.MontoIVA;
                totalSinIVA += grupo.BaseImponible;

                y += 20;
            }

            y += 10;
            graphics.DrawLine(Pens.Black, leftMargin, y, rightMargin, y);
            y += 15;

            // TOTALES - TODO EN NEGRO Y MEJOR ESPACIADO
            graphics.DrawString($"IVA TOTAL:", fontTitulo, Brushes.Black, colX2, y); // CAMBIO: De rojo a negro
            graphics.DrawString(totalIVA.ToString("C2"), fontTitulo, Brushes.Black, colX4, y); // CAMBIO: De rojo a negro
            y += 30;

            graphics.DrawString($"SUBTOTAL SIN IVA:", fontSubtitulo, Brushes.Black, colX2, y);
            graphics.DrawString(totalSinIVA.ToString("C2"), fontSubtitulo, Brushes.Black, colX4, y);
            y += 25;

            graphics.DrawString($"TOTAL GENERAL:", fontSubtitulo, Brushes.Black, colX2, y);
            graphics.DrawString(totalGeneral.ToString("C2"), fontSubtitulo, Brushes.Black, colX4, y);

            // Limpiar recursos
            fontTitulo.Dispose();
            fontSubtitulo.Dispose();
            fontNormal.Dispose();
            fontPequeño.Dispose();
            fontIvaDestacado.Dispose();
        }

        // NUEVO: Método para exportar a CSV - ORDEN ACTUALIZADO
        private void ExportarACSV(string rutaArchivo)
        {
            var lineas = new List<string>();

            // Agregar encabezado con información del reporte
            string tipoVenta = esCtaCte ? "Cuenta Corriente" : "";

            if (fechaDesde == fechaHasta)
                lineas.Add($"# Fecha: {fechaDesde:dd/MM/yyyy}" + (esCtaCte ? " - Tipo: Cuenta Corriente" : ""));
            else
                lineas.Add($"# Fecha: {fechaDesde:dd/MM/yyyy} → {fechaHasta:dd/MM/yyyy}" + (esCtaCte ? " - Tipo: Cuenta Corriente" : ""));

            lineas.Add($"# Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}");
            lineas.Add(""); // Línea vacía

            // Encabezados de columnas - NUEVO ORDEN
            lineas.Add("Alícuota IVA,Productos,Monto IVA,Base Imponible,Total con IVA");

            // Datos - NUEVO ORDEN
            decimal totalGeneral = 0;
            decimal totalIVA = 0;
            decimal totalSinIVA = 0;

            foreach (var grupo in datosResumenActual.OrderByDescending(x => x.PorcentajeIVA))
            {
                // NUEVO ORDEN: Alícuota, Productos, MONTO IVA, Base Imponible, Total con IVA
                lineas.Add($"{grupo.PorcentajeIVA:N2}%,{grupo.CantidadProductos}," +
                          $"\"{grupo.MontoIVA:C2}\",\"{grupo.BaseImponible:C2}\",\"{grupo.TotalConIVA:C2}\"");

                totalGeneral += grupo.TotalConIVA;
                totalIVA += grupo.MontoIVA;
                totalSinIVA += grupo.BaseImponible;
            }

            // Línea vacía y totales - NUEVO ORDEN DE PROMINENCIA
            lineas.Add("");
            lineas.Add("TOTALES");
            lineas.Add($"*** IVA TOTAL:,,,\"{totalIVA:C2}\","); // PRIMERO Y DESTACADO
            lineas.Add($"Subtotal sin IVA:,,,\"{totalSinIVA:C2}\",");
            lineas.Add($"TOTAL GENERAL:,,,\"{totalGeneral:C2}\",");

            // Escribir archivo
            System.IO.File.WriteAllLines(rutaArchivo, lineas, System.Text.Encoding.UTF8);
        }

        private async Task<List<ResumenIvaGroup>> ObtenerResumenIvaPorAlicuota()
        {
            var resumen = new List<ResumenIvaGroup>();

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // MODIFICADO: ahora usamos BETWEEN para rango y opcionalmente filtramos por cuenta corriente
                    var query = @"
                        SELECT 
                            p.iva as PorcentajeIVA,
                            SUM(v.cantidad) as CantidadProductos,
                            SUM(v.total) as TotalVentas
                        FROM Facturas f
                        INNER JOIN Ventas v ON f.NumeroRemito = v.NroFactura
                        INNER JOIN Productos p ON v.codigo = p.codigo
                        WHERE CAST(f.Fecha AS DATE) BETWEEN @desde AND @hasta
                        AND (@esCtaCte = 0 OR f.esCtaCte = 1)
                        AND f.TipoFactura IN ('FacturaA', 'FacturaB') -- <--- FILTRO SOLO FACTURAS A Y B
                        GROUP BY p.iva
                        HAVING SUM(v.total) > 0
                        ORDER BY p.iva DESC";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@desde", fechaDesde.Date);
                        cmd.Parameters.AddWithValue("@hasta", fechaHasta.Date);
                        cmd.Parameters.AddWithValue("@esCtaCte", esCtaCte ? 1 : 0);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                decimal porcentajeIVA = reader.GetDecimal("PorcentajeIVA");
                                int cantidadProductos = reader.GetInt32("CantidadProductos");
                                decimal totalVentas = reader.GetDecimal("TotalVentas");

                                // Calcular base imponible y monto de IVA
                                decimal baseImponible = Math.Round(totalVentas / (1 + (porcentajeIVA / 100m)), 2);
                                decimal montoIVA = Math.Round(totalVentas - baseImponible, 2);

                                resumen.Add(new ResumenIvaGroup
                                {
                                    PorcentajeIVA = porcentajeIVA,
                                    CantidadProductos = cantidadProductos,
                                    BaseImponible = baseImponible,
                                    MontoIVA = montoIVA,
                                    TotalConIVA = totalVentas
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener datos de IVA: {ex.Message}",
                    "Error de Base de Datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return resumen;
        }

        // Clase auxiliar para agrupar datos de IVA
        private class ResumenIvaGroup
        {
            public decimal PorcentajeIVA { get; set; }
            public int CantidadProductos { get; set; }
            public decimal BaseImponible { get; set; }
            public decimal MontoIVA { get; set; }
            public decimal TotalConIVA { get; set; }
        }
    }
}