using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class ResumenIvaForm : Form
    {
        private DateTime fechaConsulta;
        private bool esCtaCte;
        private List<ResumenIvaGroup> datosResumenActual; // NUEVO: Almacenar datos para impresión

        public ResumenIvaForm(DateTime fecha, bool esCuentaCorriente)
        {
            this.fechaConsulta = fecha;
            this.esCtaCte = esCuentaCorriente;
            
            InitializeComponent();
            ConfigurarFormulario();
            CargarResumenIva();
        }
            
        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configuración básica del formulario
            this.Text = $"Resumen de IVA - {fechaConsulta:dd/MM/yyyy}" + (esCtaCte ? " (Cuenta Corriente)" : " (Contado)");
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
                Text = "📊 Resumen de IVA por Alícuotas",
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

            // Labels para totales generales
            var lblTotalGeneral = new Label
            {
                Name = "lblTotalGeneral",
                Text = "Total General: $0,00",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                AutoSize = true,
                Location = new Point(20, 10)
            };

            var lblTotalIVA = new Label
            {
                Name = "lblTotalIVA",
                Text = "IVA Total: $0,00",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 53, 69),
                AutoSize = true,
                Location = new Point(20, 35)
            };

            var lblSubtotalSinIVA = new Label
            {
                Name = "lblSubtotalSinIVA",
                Text = "Subtotal sin IVA: $0,00",
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(108, 117, 125),
                AutoSize = true,
                Location = new Point(20, 60)
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

            panelInferior.Controls.AddRange(new Control[] { 
                lblTotalGeneral, lblTotalIVA, lblSubtotalSinIVA, 
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

                dgvResumen.Columns.Add(new DataGridViewTextBoxColumn 
                { 
                    Name = "BaseImponible", 
                    HeaderText = "Base Imponible",
                    Width = 130,
                    DefaultCellStyle = new DataGridViewCellStyle 
                    { 
                        Format = "C2",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        BackColor = Color.FromArgb(248, 252, 255)
                    }
                });

                dgvResumen.Columns.Add(new DataGridViewTextBoxColumn 
                { 
                    Name = "MontoIVA", 
                    HeaderText = "Monto IVA",
                    Width = 120,
                    DefaultCellStyle = new DataGridViewCellStyle 
                    { 
                        Format = "C2",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        ForeColor = Color.FromArgb(220, 53, 69),
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                    }
                });

                dgvResumen.Columns.Add(new DataGridViewTextBoxColumn 
                { 
                    Name = "TotalConIVA", 
                    HeaderText = "Total con IVA",
                    Width = 140,
                    DefaultCellStyle = new DataGridViewCellStyle 
                    { 
                        Format = "C2",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        BackColor = Color.FromArgb(240, 248, 255),
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                    }
                });

                // Cargar datos
                datosResumenActual = await ObtenerResumenIvaPorAlicuota(); // MODIFICADO: Almacenar datos
                
                if (datosResumenActual?.Count > 0)
                {
                    decimal totalGeneral = 0;
                    decimal totalIVA = 0;
                    decimal totalSinIVA = 0;

                    foreach (var grupo in datosResumenActual.OrderByDescending(x => x.PorcentajeIVA))
                    {
                        var row = dgvResumen.Rows[dgvResumen.Rows.Add()];
                        
                        row.Cells["Alicuota"].Value = $"{grupo.PorcentajeIVA:N2}%";
                        row.Cells["CantidadProductos"].Value = grupo.CantidadProductos;
                        row.Cells["BaseImponible"].Value = grupo.BaseImponible;
                        row.Cells["MontoIVA"].Value = grupo.MontoIVA;
                        row.Cells["TotalConIVA"].Value = grupo.TotalConIVA;

                        // Aplicar color de fondo según alícuota
                        Color colorFondo = grupo.PorcentajeIVA switch
                        {
                            21.00m => Color.FromArgb(255, 245, 245), // Rosa claro para 21%
                            10.50m => Color.FromArgb(245, 255, 245), // Verde claro para 10.5%
                            6.63m => Color.FromArgb(245, 245, 255),  // Azul claro para 6.63%
                            _ => Color.White
                        };

                        // Aplicar color a todas las celdas excepto MontoIVA
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

                    // Actualizar labels de totales
                    var lblTotalGeneral = this.Controls.Find("lblTotalGeneral", true).FirstOrDefault() as Label;
                    var lblTotalIVA = this.Controls.Find("lblTotalIVA", true).FirstOrDefault() as Label;
                    var lblSubtotalSinIVA = this.Controls.Find("lblSubtotalSinIVA", true).FirstOrDefault() as Label;

                    if (lblTotalGeneral != null)
                        lblTotalGeneral.Text = $"Total General: {totalGeneral:C2}";
                    
                    if (lblTotalIVA != null)
                        lblTotalIVA.Text = $"IVA Total: {totalIVA:C2}";
                    
                    if (lblSubtotalSinIVA != null)
                        lblSubtotalSinIVA.Text = $"Subtotal sin IVA: {totalSinIVA:C2}";
                }
                else
                {
                    datosResumenActual = new List<ResumenIvaGroup>(); // NUEVO: Inicializar lista vacía
                    
                    // Agregar fila indicando que no hay datos
                    var row = dgvResumen.Rows[dgvResumen.Rows.Add()];
                    row.Cells["Alicuota"].Value = "Sin datos para esta fecha";
                    row.Cells["CantidadProductos"].Value = 0;
                    row.Cells["BaseImponible"].Value = 0;
                    row.Cells["MontoIVA"].Value = 0;
                    row.Cells["TotalConIVA"].Value = 0;
                    
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
                string tipoVenta = esCtaCte ? "CtaCte" : "Contado";
                saveDialog.Filter = "Archivos CSV|*.csv|Archivos de Excel|*.xlsx";
                saveDialog.FileName = $"ResumenIVA_{fechaConsulta:yyyyMMdd}_{tipoVenta}";
                
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

        // NUEVO: Método para imprimir el reporte
        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics graphics = e.Graphics;
            Font fontTitulo = new Font("Arial", 16, FontStyle.Bold);
            Font fontSubtitulo = new Font("Arial", 12, FontStyle.Bold);
            Font fontNormal = new Font("Arial", 10);
            Font fontPequeño = new Font("Arial", 9);

            float y = 50;
            float leftMargin = 50;
            float rightMargin = e.PageBounds.Width - 50;

            // Título principal
            string titulo = $"RESUMEN DE IVA POR ALÍCUOTAS";
            SizeF tituloSize = graphics.MeasureString(titulo, fontTitulo);
            float tituloX = (e.PageBounds.Width - tituloSize.Width) / 2;
            graphics.DrawString(titulo, fontTitulo, Brushes.Black, tituloX, y);
            y += tituloSize.Height + 10;

            // Información del reporte
            string tipoVenta = esCtaCte ? "Cuenta Corriente" : "Contado";
            string info = $"Fecha: {fechaConsulta:dd/MM/yyyy} - Tipo: {tipoVenta}";
            SizeF infoSize = graphics.MeasureString(info, fontSubtitulo);
            float infoX = (e.PageBounds.Width - infoSize.Width) / 2;
            graphics.DrawString(info, fontSubtitulo, Brushes.Black, infoX, y);
            y += infoSize.Height + 20;

            // Fecha de generación
            string fechaGen = $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
            graphics.DrawString(fechaGen, fontPequeño, Brushes.Gray, rightMargin - graphics.MeasureString(fechaGen, fontPequeño).Width, y);
            y += 30;

            // Encabezados de tabla
            float colX1 = leftMargin;
            float colX2 = leftMargin + 100;
            float colX3 = leftMargin + 180;
            float colX4 = leftMargin + 320;
            float colX5 = leftMargin + 450;

            graphics.DrawString("Alícuota", fontSubtitulo, Brushes.Black, colX1, y);
            graphics.DrawString("Productos", fontSubtitulo, Brushes.Black, colX2, y);
            graphics.DrawString("Base Imponible", fontSubtitulo, Brushes.Black, colX3, y);
            graphics.DrawString("Monto IVA", fontSubtitulo, Brushes.Black, colX4, y);
            graphics.DrawString("Total con IVA", fontSubtitulo, Brushes.Black, colX5, y);
            y += 25;

            // Línea separadora
            graphics.DrawLine(Pens.Black, leftMargin, y, rightMargin, y);
            y += 10;

            // Datos
            decimal totalGeneral = 0;
            decimal totalIVA = 0;
            decimal totalSinIVA = 0;

            foreach (var grupo in datosResumenActual.OrderByDescending(x => x.PorcentajeIVA))
            {
                graphics.DrawString($"{grupo.PorcentajeIVA:N2}%", fontNormal, Brushes.Black, colX1, y);
                graphics.DrawString(grupo.CantidadProductos.ToString(), fontNormal, Brushes.Black, colX2, y);
                graphics.DrawString(grupo.BaseImponible.ToString("C2"), fontNormal, Brushes.Black, colX3, y);
                graphics.DrawString(grupo.MontoIVA.ToString("C2"), fontNormal, Brushes.Black, colX4, y);
                graphics.DrawString(grupo.TotalConIVA.ToString("C2"), fontNormal, Brushes.Black, colX5, y);
                
                totalGeneral += grupo.TotalConIVA;
                totalIVA += grupo.MontoIVA;
                totalSinIVA += grupo.BaseImponible;
                
                y += 20;
            }

            y += 10;
            graphics.DrawLine(Pens.Black, leftMargin, y, rightMargin, y);
            y += 15;

            // Totales finales
            graphics.DrawString($"SUBTOTAL SIN IVA:", fontSubtitulo, Brushes.Black, colX3, y);
            graphics.DrawString(totalSinIVA.ToString("C2"), fontSubtitulo, Brushes.Black, colX5, y);
            y += 25;

            graphics.DrawString($"IVA TOTAL:", fontSubtitulo, Brushes.Black, colX3, y);
            graphics.DrawString(totalIVA.ToString("C2"), fontSubtitulo, Brushes.Black, colX5, y);
            y += 25;

            graphics.DrawString($"TOTAL GENERAL:", fontTitulo, Brushes.Black, colX3, y);
            graphics.DrawString(totalGeneral.ToString("C2"), fontTitulo, Brushes.Black, colX5, y);

            // Limpiar recursos
            fontTitulo.Dispose();
            fontSubtitulo.Dispose();
            fontNormal.Dispose();
            fontPequeño.Dispose();
        }

        // NUEVO: Método para exportar a CSV
        private void ExportarACSV(string rutaArchivo)
        {
            var lineas = new List<string>();
            
            // Agregar encabezado con información del reporte
            string tipoVenta = esCtaCte ? "Cuenta Corriente" : "Contado";
            lineas.Add($"# RESUMEN DE IVA POR ALÍCUOTAS");
            lineas.Add($"# Fecha: {fechaConsulta:dd/MM/yyyy} - Tipo: {tipoVenta}");
            lineas.Add($"# Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}");
            lineas.Add(""); // Línea vacía

            // Encabezados de columnas
            lineas.Add("Alícuota IVA,Productos,Base Imponible,Monto IVA,Total con IVA");

            // Datos
            decimal totalGeneral = 0;
            decimal totalIVA = 0;
            decimal totalSinIVA = 0;

            foreach (var grupo in datosResumenActual.OrderByDescending(x => x.PorcentajeIVA))
            {
                lineas.Add($"{grupo.PorcentajeIVA:N2}%,{grupo.CantidadProductos}," +
                          $"\"{grupo.BaseImponible:C2}\",\"{grupo.MontoIVA:C2}\",\"{grupo.TotalConIVA:C2}\"");
                
                totalGeneral += grupo.TotalConIVA;
                totalIVA += grupo.MontoIVA;
                totalSinIVA += grupo.BaseImponible;
            }

            // Línea vacía y totales
            lineas.Add("");
            lineas.Add("TOTALES");
            lineas.Add($"Subtotal sin IVA:,,,\"{totalSinIVA:C2}\",");
            lineas.Add($"IVA Total:,,,\"{totalIVA:C2}\",");
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

                    // Query para obtener ventas con sus productos y porcentajes de IVA
                    var query = @"
                        SELECT 
                            p.iva as PorcentajeIVA,
                            SUM(v.cantidad) as CantidadProductos,
                            SUM(v.total) as TotalVentas
                        FROM Facturas f
                        INNER JOIN Ventas v ON f.NumeroRemito = v.NroFactura
                        INNER JOIN Productos p ON v.codigo = p.codigo
                        WHERE CAST(f.Fecha AS DATE) = @fecha 
                        AND f.esCtaCte = @esCtaCte
                        GROUP BY p.iva
                        HAVING SUM(v.total) > 0
                        ORDER BY p.iva DESC";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@fecha", fechaConsulta.Date);
                        cmd.Parameters.AddWithValue("@esCtaCte", esCtaCte);

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