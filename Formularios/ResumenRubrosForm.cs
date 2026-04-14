using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace Comercio.NET.Formularios
{
    public partial class ResumenRubrosForm : Form
    {
        private readonly DateTime fechaDesde;
        private readonly DateTime fechaHasta;
        private readonly bool esCtaCte;
        private readonly List<frmControlFacturas.ResumenRubroGroup> datosResumen;
        
        private DataGridView dgvResumen;
        private Button btnImprimir;
        private Button btnExportar;
        private Button btnCerrar;
        private Label lblTitulo;
        private Label lblPeriodo;
        private Label lblTotalGeneral;
        private PrintDocument printDocument;

        public ResumenRubrosForm(DateTime desde, DateTime hasta, 
            List<frmControlFacturas.ResumenRubroGroup> datos, bool ctaCte = false)
        {
            fechaDesde = desde;
            fechaHasta = hasta;
            datosResumen = datos;
            esCtaCte = ctaCte;

            InitializeComponent();
            ConfigurarFormulario();
            CargarDatos();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.ClientSize = new Size(800, 600);
            this.MinimumSize = new Size(700, 500);
            this.Text = "Resumen de Ventas por Rubro";
            this.StartPosition = FormStartPosition.CenterParent;
            
            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            this.BackColor = Color.FromArgb(245, 248, 250);
            this.Font = new Font("Segoe UI", 10F);

            int margin = 20;
            int currentY = margin;

            // Título
            lblTitulo = new Label
            {
                Text = "📊 RESUMEN DE VENTAS POR RUBRO",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(156, 39, 176),
                Location = new Point(margin, currentY),
                Size = new Size(760, 35),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitulo);
            currentY += 45;

            // Período
            string periodo = $"Período: {fechaDesde:dd/MM/yyyy} - {fechaHasta:dd/MM/yyyy}";
            if (esCtaCte) periodo += " (Solo Cuenta Corriente)";
            
            lblPeriodo = new Label
            {
                Text = periodo,
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(margin, currentY),
                Size = new Size(760, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblPeriodo);
            currentY += 35;

            // DataGridView
            dgvResumen = new DataGridView
            {
                Location = new Point(margin, currentY),
                Size = new Size(760, 350),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 10F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            
            dgvResumen.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(230, 240, 250),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };
            dgvResumen.ColumnHeadersHeight = 40;
            
            this.Controls.Add(dgvResumen);
            currentY += 360;

            // Total general
            lblTotalGeneral = new Label
            {
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 150, 136),
                Location = new Point(margin, currentY),
                Size = new Size(760, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(lblTotalGeneral);
            currentY += 40;

            // Botones
            btnImprimir = CrearBoton("🖨️ Imprimir", new Point(margin, currentY), Color.FromArgb(0, 120, 215));
            btnExportar = CrearBoton("📁 Exportar CSV", new Point(btnImprimir.Right + 10, currentY), Color.FromArgb(0, 150, 136));
            btnCerrar = CrearBoton("❌ Cerrar", new Point(btnExportar.Right + 10, currentY), Color.FromArgb(244, 67, 54));

            btnImprimir.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnExportar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCerrar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            this.Controls.Add(btnImprimir);
            this.Controls.Add(btnExportar);
            this.Controls.Add(btnCerrar);

            // Eventos
            btnImprimir.Click += BtnImprimir_Click;
            btnExportar.Click += BtnExportar_Click;
            btnCerrar.Click += (s, e) => this.Close();

            // PrintDocument
            printDocument = new PrintDocument();
            printDocument.PrintPage += PrintDocument_PrintPage;
        }

        private Button CrearBoton(string texto, Point ubicacion, Color color)
        {
            return new Button
            {
                Text = texto,
                Location = ubicacion,
                Size = new Size(140, 35),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }

        private void CargarDatos()
        {
            // Configurar columnas
            dgvResumen.Columns.Clear();
            dgvResumen.Columns.Add("Rubro", "Rubro");
            dgvResumen.Columns.Add("CantidadFacturas", "Facturas");
            dgvResumen.Columns.Add("CantidadProductos", "Items");
            dgvResumen.Columns.Add("MontoTotal", "Monto Total");

            // Configurar anchos y formatos - USAR EL NAME, NO EL HEADERTEXT
            dgvResumen.Columns["Rubro"].Width = 200;

            dgvResumen.Columns["CantidadFacturas"].Width = 80; // ✅ CORREGIDO
            dgvResumen.Columns["CantidadFacturas"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgvResumen.Columns["CantidadProductos"].Width = 80; // ✅ CORREGIDO
            dgvResumen.Columns["CantidadProductos"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgvResumen.Columns["MontoTotal"].DefaultCellStyle.Format = "C2"; // ✅ CORREGIDO
            dgvResumen.Columns["MontoTotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Cargar datos
            decimal totalGeneral = 0;

            foreach (var dato in datosResumen)
            {
                dgvResumen.Rows.Add(
                    dato.Rubro,
                    dato.CantidadFacturas.ToString(),
                    dato.CantidadProductos.ToString(),
                    dato.MontoTotal
                );
                totalGeneral += dato.MontoTotal;
            }

            // Actualizar total
            lblTotalGeneral.Text = $"TOTAL GENERAL: {totalGeneral:C2}";
        }

        private void BtnImprimir_Click(object sender, EventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog
                {
                    Document = printDocument
                };

                if (printDialog.ShowDialog() == DialogResult.OK)
                {
                    printDocument.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExportar_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Archivo CSV|*.csv",
                    Title = "Exportar Resumen por Rubro",
                    FileName = $"ResumenRubros_{fechaDesde:yyyyMMdd}_{fechaHasta:yyyyMMdd}.csv"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    ExportarACSV(saveDialog.FileName);
                    MessageBox.Show("Archivo exportado correctamente.", "Éxito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics graphics = e.Graphics;
            Font fontTitulo = new Font("Arial", 16, FontStyle.Bold);
            Font fontSubtitulo = new Font("Arial", 12);
            Font fontNormal = new Font("Arial", 10);
            Font fontBold = new Font("Arial", 10, FontStyle.Bold);

            float y = 50;
            float leftMargin = 50;

            // Título
            graphics.DrawString("RESUMEN DE VENTAS POR RUBRO", fontTitulo, Brushes.Black, leftMargin, y);
            y += 30;

            // Período
            string periodo = $"Período: {fechaDesde:dd/MM/yyyy} - {fechaHasta:dd/MM/yyyy}";
            if (esCtaCte) periodo += " (Cuenta Corriente)";
            graphics.DrawString(periodo, fontSubtitulo, Brushes.Black, leftMargin, y);
            y += 35;

            // Encabezados - ✅ MODIFICADO
            graphics.DrawString("RUBRO", fontBold, Brushes.Black, leftMargin, y);
            graphics.DrawString("FACTURAS", fontBold, Brushes.Black, leftMargin + 200, y); // ✅ CAMBIO
            graphics.DrawString("ITEMS", fontBold, Brushes.Black, leftMargin + 300, y); // ✅ CAMBIO
            graphics.DrawString("MONTO TOTAL", fontBold, Brushes.Black, leftMargin + 400, y); // ✅ CAMBIO
            y += 25;

            // Línea separadora
            graphics.DrawLine(Pens.Black, leftMargin, y, leftMargin + 550, y);
            y += 10;

            // Datos
            decimal totalGeneral = 0;
            foreach (var dato in datosResumen)
            {
                graphics.DrawString(dato.Rubro, fontNormal, Brushes.Black, leftMargin, y);
                graphics.DrawString(dato.CantidadFacturas.ToString(), fontNormal, Brushes.Black, leftMargin + 220, y);
                graphics.DrawString(dato.CantidadProductos.ToString(), fontNormal, Brushes.Black, leftMargin + 320, y);
                graphics.DrawString(dato.MontoTotal.ToString("C2"), fontNormal, Brushes.Black, leftMargin + 400, y);

                totalGeneral += dato.MontoTotal;
                y += 20;
            }

            // Línea separadora
            y += 10;
            graphics.DrawLine(Pens.Black, leftMargin, y, leftMargin + 550, y);
            y += 15;

            // Total
            graphics.DrawString("TOTAL GENERAL:", fontBold, Brushes.Black, leftMargin + 250, y);
            graphics.DrawString(totalGeneral.ToString("C2"), fontBold, Brushes.Black, leftMargin + 400, y);
        }

        private void ExportarACSV(string rutaArchivo)
        {
            using (StreamWriter writer = new StreamWriter(rutaArchivo))
            {
                // Encabezado
                writer.WriteLine($"RESUMEN DE VENTAS POR RUBRO");
                writer.WriteLine($"Período: {fechaDesde:dd/MM/yyyy} - {fechaHasta:dd/MM/yyyy}");
                if (esCtaCte) writer.WriteLine("Tipo: Cuenta Corriente");
                writer.WriteLine();

                // Columnas - ✅ MODIFICADO
                writer.WriteLine("Rubro;Facturas;Items;Monto Total");

                // Datos
                decimal totalGeneral = 0;
                foreach (var dato in datosResumen)
                {
                    writer.WriteLine($"{dato.Rubro};{dato.CantidadFacturas};{dato.CantidadProductos};{dato.MontoTotal:F2}");
                    totalGeneral += dato.MontoTotal;
                }

                // Total
                writer.WriteLine();
                writer.WriteLine($"TOTAL GENERAL;;;{totalGeneral:F2}");
            }
        }
    }
}