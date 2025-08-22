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

namespace Comercio.NET
{
    public partial class Ventas : Form
    {
        private int nroRemitoActual = 0;
        private bool remitoIncrementado = false;

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
                var query = @"SELECT codigo, descripcion, precio, rubro, marca, proveedor, costo, fecha, hora, cantidad, total, nrofactura
                              FROM Ventas
                              WHERE nrofactura = @nrofactura
                              ORDER BY id ASC";
                using (var adapter = new SqlDataAdapter(query, connection))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@nrofactura", nroRemitoActual);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dataGridView1.DataSource = dt;
                }
            }
        }

        private void btnFinalizarVenta_Click(object sender, EventArgs e)
        {
            remitoIncrementado = false;
            Ventas_Load(null, null);
            dataGridView1.DataSource = null; // Limpia la grilla
            dataGridView1.Rows.Clear();      // Opcional, asegura que no queden filas
            MessageBox.Show("Venta finalizada. Se generó un nuevo remito.");
        }

        private void btnAgregar_Click_1(object sender, EventArgs e)
        {

        }
    }
}
