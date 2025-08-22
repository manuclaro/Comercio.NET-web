using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Drawing.Printing;

namespace Comercio.NET
{
    public partial class Ventas : Form
    {
        private int nroRemitoActual = 0;
        private bool remitoIncrementado = false;
        private DataTable remitoActual = null;
        private PrintDocument printDocumentRemito = new PrintDocument();

        public Ventas()
        {
            InitializeComponent();
            txtBuscarProducto.TextChanged += txtBuscarProducto_TextChanged;
            btnAgregar.Click += btnAgregar_Click;
            this.Load += Ventas_Load;
            btnFinalizarVenta.Click += btnFinalizarVenta_Click;

            // Selecciona todo el texto al recibir foco
            txtBuscarProducto.Enter += (s, e) => txtBuscarProducto.SelectAll();

            // Tabula con Enter
            txtBuscarProducto.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    this.SelectNextControl(txtBuscarProducto, true, true, true, true);
                }
            };

            printDocumentRemito.PrintPage += printDocumentRemito_PrintPage;
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void txtBuscarProducto_TextChanged(object sender, EventArgs e)
        {
            string codigoBuscado = txtBuscarProducto.Text.Trim();

            if (string.IsNullOrEmpty(codigoBuscado))
            {
                lbDescripcionProducto.Text = "";
                return;
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            string connectionString = config.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT descripcion FROM Productos WHERE codigo = @codigo";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@codigo", codigoBuscado);
                    connection.Open();
                    var result = cmd.ExecuteScalar();
                    lbDescripcionProducto.Text = result != null ? result.ToString() : "Producto no encontrado";
                }
            }
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            string codigoBuscado = txtBuscarProducto.Text.Trim();
            if (string.IsNullOrEmpty(codigoBuscado))
            {
                MessageBox.Show("Ingrese un código de producto válido.");
                return;
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            string connectionString = config.GetConnectionString("DefaultConnection");

            // 1. Si es el primer producto de la venta, incrementa el remito y obtén el nuevo valor
            if (!remitoIncrementado)
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var query = "UPDATE numeroremito SET nroremito = nroremito + 1";
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                // Obtener el nuevo nroRemitoActual
                using (var connection = new SqlConnection(connectionString))
                {
                    var query = "SELECT nroremito FROM numeroremito";
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        var result = cmd.ExecuteScalar();
                        if (result == null || !int.TryParse(result.ToString(), out nroRemitoActual))
                        {
                            MessageBox.Show("No se pudo obtener el número de remito.");
                            return;
                        }
                    }
                }
                remitoIncrementado = true;
            }

            // 2. Obtener los datos del producto
            DataRow producto = null;
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"SELECT codigo, descripcion, precio, rubro, marca, proveedor, costo 
                              FROM Productos WHERE codigo = @codigo";
                using (var adapter = new SqlDataAdapter(query, connection))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@codigo", codigoBuscado);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count == 0)
                    {
                        MessageBox.Show("Producto no encontrado.");
                        return;
                    }
                    producto = dt.Rows[0];
                }
            }

            // 3. Insertar en la tabla Ventas
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"INSERT INTO Ventas 
                    (codigo, descripcion, precio, rubro, marca, proveedor, costo, fecha, hora, cantidad, total, nrofactura)
                    VALUES (@codigo, @descripcion, @precio, @rubro, @marca, @proveedor, @costo, @fecha, @hora, @cantidad, @total, @nrofactura)";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@codigo", producto["codigo"]);
                    cmd.Parameters.AddWithValue("@descripcion", producto["descripcion"]);
                    cmd.Parameters.AddWithValue("@precio", producto["precio"]);
                    cmd.Parameters.AddWithValue("@rubro", producto["rubro"]);
                    cmd.Parameters.AddWithValue("@marca", producto["marca"]);
                    cmd.Parameters.AddWithValue("@proveedor", producto["proveedor"]);
                    cmd.Parameters.AddWithValue("@costo", producto["costo"]);
                    cmd.Parameters.AddWithValue("@fecha", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@hora", DateTime.Now.ToString("HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@cantidad", 1);
                    cmd.Parameters.AddWithValue("@total", producto["precio"]);
                    cmd.Parameters.AddWithValue("@nrofactura", nroRemitoActual);

                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            // 4. Mostrar todas las ventas del remito actual
            CargarVentasActuales();

            // Ajustar el ancho de las columnas
            if (dataGridView1.Columns["codigo"] != null)
                dataGridView1.Columns["codigo"].Width = 130;
            if (dataGridView1.Columns["descripcion"] != null)
                dataGridView1.Columns["descripcion"].Width = 250;
            if (dataGridView1.Columns["precio"] != null)
                dataGridView1.Columns["precio"].Width = 110;
            if (dataGridView1.Columns["cantidad"] != null)
                dataGridView1.Columns["cantidad"].Width = 60;
            if (dataGridView1.Columns["total"] != null)
                dataGridView1.Columns["total"].Width = 130;

            // Formatear columnas numéricas
            if (dataGridView1.Columns["precio"] != null)
            {
                dataGridView1.Columns["precio"].DefaultCellStyle.Format = "C2";
                dataGridView1.Columns["precio"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (dataGridView1.Columns["total"] != null)
            {
                dataGridView1.Columns["total"].DefaultCellStyle.Format = "C2";
                dataGridView1.Columns["total"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (dataGridView1.Columns["cantidad"] != null)
            {
                dataGridView1.Columns["cantidad"].HeaderText = "CANT.";
                dataGridView1.Columns["cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dataGridView1.Columns["cantidad"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Encabezados en mayúsculas y centrados
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.HeaderText = col.HeaderText.ToUpper();
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Dejar el foco en el campo buscar para el próximo producto
            txtBuscarProducto.Text = "";
            txtBuscarProducto.Focus();
        }

        private void Ventas_Load(object sender, EventArgs e)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            string connectionString = config.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT nroremito FROM numeroremito";
                using (var cmd = new SqlCommand(query, connection))
                {
                    connection.Open();
                    var result = cmd.ExecuteScalar();
                    if (result == null || !int.TryParse(result.ToString(), out nroRemitoActual))
                    {
                        MessageBox.Show("No se pudo obtener el número de remito.");
                        nroRemitoActual = 0;
                    }
                }
            }

            // Deja la grilla vacía al abrir el formulario
            dataGridView1.DataSource = null;
            dataGridView1.Rows.Clear(); // Opcional, asegura que no queden filas

            txtBuscarProducto.Focus();
        }

        private void CargarVentasActuales()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            string connectionString = config.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"SELECT codigo, descripcion, precio,  cantidad, total
                              FROM Ventas
                              WHERE nrofactura = @nrofactura
                              ORDER BY id ASC";
                using (var adapter = new SqlDataAdapter(query, connection))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@nrofactura", nroRemitoActual);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dataGridView1.DataSource = dt;
                    remitoActual = dt;
                }
            }

            // Contar la cantidad de productos
            lblCantidadProductos.Text = $"Productos: {dataGridView1.Rows.Count}";

            // Sumar el total acumulado
            decimal sumaTotal = 0;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["total"].Value != null && decimal.TryParse(row.Cells["total"].Value.ToString(), out decimal valor))
                    sumaTotal += valor;
            }
            lbTotal.Text = $"Total: {sumaTotal:C2}";
        }

        private void btnFinalizarVenta_Click(object sender, EventArgs e)
        {
            remitoIncrementado = false;

            // Imprimir remito antes de limpiar
            if (remitoActual != null && remitoActual.Rows.Count > 0)
            {
                PrintDialog printDialog = new PrintDialog();
                printDialog.Document = printDocumentRemito;
                if (printDialog.ShowDialog() == DialogResult.OK)
                {
                    printDocumentRemito.Print();
                }
            }

            Ventas_Load(null, null);
            dataGridView1.DataSource = null;
            dataGridView1.Rows.Clear();
            MessageBox.Show("Venta finalizada. Se generó un nuevo remito.");
        }

        private void btnAgregar_Click_1(object sender, EventArgs e)
        {

        }

        private void printDocumentRemito_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (remitoActual == null) return;

            // Márgenes y fuentes
            float leftMargin = e.MarginBounds.Left + 20;
            float topMargin = e.MarginBounds.Top + 20;
            float rowHeight = 22;
            Font font = new Font("Arial", 10);
            Font fontBold = new Font("Arial", 10, FontStyle.Bold);
            Font fontTitulo = new Font("Arial", 18, FontStyle.Bold);
            Pen linePen = new Pen(Color.Black, 1);

            // Definir anchos de columnas (ajustados)
            float colCodigo = 120;
            float colDescripcion = 240;
            float colCantidad = 70;
            float colPrecio = 90;
            float colTotal = 120;

            float[] colX = {
                leftMargin,
                leftMargin + colCodigo,
                leftMargin + colCodigo + colDescripcion,
                leftMargin + colCodigo + colDescripcion + colCantidad,
                leftMargin + colCodigo + colDescripcion + colCantidad + colPrecio
            };

            float tablaRight = colX[4] + colTotal; // Límite derecho de la tabla

            float y = topMargin;

            // Título principal "Comercio" centrado y grande
            string nombreComercio = "Comercio";
            SizeF nombreComercioSize = e.Graphics.MeasureString(nombreComercio, fontTitulo);
            e.Graphics.DrawString(
                nombreComercio,
                fontTitulo,
                Brushes.Black,
                leftMargin + ((tablaRight - leftMargin) - nombreComercioSize.Width) / 2,
                y
            );

            // Fecha y hora alineadas con el borde derecho de la columna "TOTAL"
            string fechaStr = $"Fecha: {DateTime.Now:dd/MM/yyyy}";
            string horaStr = $"Hora: {DateTime.Now:HH:mm}";
            SizeF fechaSize = e.Graphics.MeasureString(fechaStr, font);
            SizeF horaSize = e.Graphics.MeasureString(horaStr, font);
            float fechaX = tablaRight - fechaSize.Width;
            float horaX = tablaRight - horaSize.Width;
            e.Graphics.DrawString(fechaStr, font, Brushes.Black, fechaX, y);
            e.Graphics.DrawString(horaStr, font, Brushes.Black, horaX, y + fechaSize.Height);

            y += Math.Max(nombreComercioSize.Height, fechaSize.Height + horaSize.Height) + 10;

            // Título Remito centrado
            string titulo = $"REMITO N°: {nroRemitoActual}";
            SizeF tituloSize = e.Graphics.MeasureString(titulo, fontBold);
            e.Graphics.DrawString(
                titulo,
                fontBold,
                Brushes.Black,
                leftMargin + ((tablaRight - leftMargin) - tituloSize.Width) / 2,
                y
            );
            y += tituloSize.Height + 10;

            // Encabezados de columnas
            string[] headers = { "CÓDIGO", "DESCRIPCIÓN", "CANT.", "PRECIO", "TOTAL" };
            for (int i = 0; i < headers.Length; i++)
            {
                float headerX = colX[i];
                float colWidth = (i == 0) ? colCodigo :
                                 (i == 1) ? colDescripcion :
                                 (i == 2) ? colCantidad :
                                 (i == 3) ? colPrecio : colTotal;

                // Centrar encabezados de CANT., PRECIO y TOTAL
                if (i >= 2)
                {
                    SizeF headerSize = e.Graphics.MeasureString(headers[i], fontBold);
                    headerX += (colWidth - headerSize.Width) / 2;
                }
                e.Graphics.DrawString(headers[i], fontBold, Brushes.Black, headerX, y);
            }

            y += rowHeight - 6;
            // Línea debajo del encabezado (hasta el borde de la columna TOTAL)
            e.Graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 4;

            // Detalle productos
            int cantidadTotal = 0;
            foreach (DataRow row in remitoActual.Rows)
            {
                // Código
                e.Graphics.DrawString(row["codigo"].ToString(), font, Brushes.Black, colX[0], y);

                // Descripción
                e.Graphics.DrawString(row["descripcion"].ToString(), font, Brushes.Black, colX[1], y);

                // Cantidad (centrado)
                string cantidadStr = row["cantidad"].ToString();
                SizeF cantidadSize = e.Graphics.MeasureString(cantidadStr, font);
                float cantidadX = colX[2] + (colCantidad - cantidadSize.Width) / 2;
                e.Graphics.DrawString(cantidadStr, font, Brushes.Black, cantidadX, y);

                // Sumar cantidad total
                if (int.TryParse(cantidadStr, out int cantVal))
                    cantidadTotal += cantVal;

                // Precio (alineado a la derecha)
                string precioStr = Convert.ToDecimal(row["precio"]).ToString("C2");
                SizeF precioSize = e.Graphics.MeasureString(precioStr, font);
                float precioX = colX[3] + colPrecio - precioSize.Width;
                e.Graphics.DrawString(precioStr, font, Brushes.Black, precioX, y);

                // Total (alineado a la derecha)
                string totalStr = Convert.ToDecimal(row["total"]).ToString("C2");
                SizeF totalSize = e.Graphics.MeasureString(totalStr, font);
                float totalX = colX[4] + colTotal - totalSize.Width;
                e.Graphics.DrawString(totalStr, font, Brushes.Black, totalX, y);

                y += rowHeight - 2;
            }

            // Línea encima del total (hasta el borde de la columna TOTAL)
            y += 6;
            e.Graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 6;

            // Total general, alineado a la derecha, en línea con la columna TOTAL
            decimal sumaTotal = 0;
            foreach (DataRow row in remitoActual.Rows)
            {
                if (decimal.TryParse(row["total"].ToString(), out decimal valor))
                    sumaTotal += valor;
            }
            string totalGeneralStr = $"TOTAL: {sumaTotal:C2}";
            SizeF totalGeneralSize = e.Graphics.MeasureString(totalGeneralStr, fontBold);
            float totalGeneralX = colX[4] + colTotal - totalGeneralSize.Width;
            e.Graphics.DrawString(totalGeneralStr, fontBold, Brushes.Black, totalGeneralX, y);

            // Total cantidad de productos, alineado a la izquierda, a la misma altura que el total general
            string cantidadTotalStr = $"CANTIDAD TOTAL DE PRODUCTOS: {cantidadTotal}";
            e.Graphics.DrawString(cantidadTotalStr, fontBold, Brushes.Black, leftMargin, y);

            // Firma (opcional)
            y += rowHeight * 2;
            e.Graphics.DrawString("Firma: ___________________________", font, Brushes.Black, leftMargin, y);
        }
    }
}
