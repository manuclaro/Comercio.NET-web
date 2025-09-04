using ArcaWS;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace Comercio.NET
{
    public partial class Ventas : Form
    {
        private const string PREFIJO_CODIGO_ESPECIAL = "50";
        private const int LONGITUD_CODIGO_ESPECIAL = 13;
        private const int POSICION_CODIGO_PRODUCTO = 2;
        private const int LONGITUD_CODIGO_PRODUCTO = 5;
        private const int POSICION_PRECIO = 7;
        private const int LONGITUD_PRECIO = 5;

        private int nroRemitoActual = 0;
        private bool remitoIncrementado = false;
        private DataTable remitoActual = null;
        private PrintDocument printDocumentRemito = new PrintDocument();
        private string nombreComercio = "Comercio";
        private string domicilioComercio = "domicilio";

        private string token;
        private string sign;

        private int cantidadPersonalizada = 1;
        private CheckBox chkCantidad;

        public Ventas()
        {
            InitializeComponent();
            ConfigurarEstilosFormulario();
            ConfigurarEventHandlers();
            CargarConfiguracion();
            ConfigurarCheckboxCantidad();
            ConfigurarAtajosTeclado();

            // Agregar evento de redimensionamiento
            this.Resize += Ventas_Resize;
        }

        private void Ventas_Resize(object sender, EventArgs e)
        {
            // Ajustar el DataGridView cuando se redimensiona el formulario
            if (dataGridView1 != null)
            {
                dataGridView1.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 171 - 60);
            }
        }

        private void ConfigurarEventHandlers()
        {
            txtBuscarProducto.TextChanged += txtBuscarProducto_TextChanged;
            btnAgregar.Click += btnAgregar_Click;
            this.Load += Ventas_Load;
            btnFinalizarVenta.Click += btnFinalizarVenta_Click;
            cbnombreCtaCte.SelectedIndexChanged += cbnombreCtaCte_SelectedIndexChanged;

            ConfigurarEventosTextBox();
        }

        private void ConfigurarEventosTextBox()
        {
            txtBuscarProducto.Enter += (s, e) => txtBuscarProducto.SelectAll();
            txtBuscarProducto.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    this.SelectNextControl(txtBuscarProducto, true, true, true, true);
                }
            };

            ConfigurarEventosPrecio();
        }

        private void ConfigurarAtajosTeclado()
        {
            this.KeyPreview = true; // Importante: permite que el formulario capture las teclas
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F9)
                {
                    e.SuppressKeyPress = true;
                    // Alternar el estado del checkbox
                    chkCantidad.Checked = !chkCantidad.Checked;
                }
            };
        }

        private void CargarConfiguracion()
        {
            // Leer desde appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            nombreComercio = config["Comercio:Nombre"] ?? "Comercio";
            domicilioComercio = config["Comercio:Domicilio"] ?? "domicilio";
        }

        private void ConfigurarCheckboxCantidad()
        {
            // CheckBox para cantidad personalizada
            chkCantidad = new CheckBox
            {
                Text = "Cantidad",
                Left = 500, // Más a la derecha, separado del chkEsCtaCte
                Top = 136,  // Misma altura que chkEsCtaCte
                Width = 180,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Black
            };
            chkCantidad.CheckedChanged += chkCantidad_CheckedChanged;
            this.Controls.Add(chkCantidad);
        }

        private void ConfigurarEstilosFormulario()
        {
            this.Font = new Font("Segoe UI", 10F);
            this.BackColor = Color.WhiteSmoke;

            ConfigurarBoton(btnAgregar, Color.FromArgb(0, 120, 215));
            ConfigurarBoton(btnFinalizarVenta, Color.FromArgb(0, 150, 136));
            ConfigurarBoton(btnSalir, Color.FromArgb(220, 53, 69));

            ConfigurarPaneles();
            ConfigurarDataGridView();
            ConfigurarTextBoxes();
        }

        private void ConfigurarBoton(Button btn, Color backColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = backColor;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        }

        private void ConfigurarPaneles()
        {
            // Panel superior
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Height = 70;
            panelHeader.BackColor = Color.FromArgb(0, 120, 215);

            Label lblTitulo = new Label
            {
                Text = "Ventas",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelHeader.Controls.Add(lblTitulo);

            // Panel inferior
            ConfigurarPanelFooter();
        }

        private void ConfigurarDataGridView()
        {
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // Estilos
            var headerStyle = dataGridView1.ColumnHeadersDefaultCellStyle;
            headerStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            headerStyle.BackColor = Color.FromArgb(248, 249, 250);
            headerStyle.ForeColor = Color.Black;

            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(220, 235, 255);
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void txtBuscarProducto_TextChanged(object sender, EventArgs e)
        {
            string textoIngresado = txtBuscarProducto.Text.Trim();

            if (string.IsNullOrEmpty(textoIngresado))
            {
                LimpiarCamposProducto();
                return;
            }

            try
            {
                // Corrección: usar declaraciones separadas
                var resultado = ProcesarCodigo(textoIngresado);
                await MostrarProductoAsync(resultado.codigoBuscado, resultado.precioPersonalizado, resultado.esEspecial);
            }
            catch (Exception ex)
            {
                lbDescripcionProducto.Text = ex.Message;
                LimpiarCamposProducto();
            }
        }

        private async Task MostrarProductoAsync(string codigo, decimal? precioPersonalizado, bool esEspecial)
        {
            var producto = await BuscarProductoAsync(codigo);

            if (producto == null)
            {
                lbDescripcionProducto.Text = $"Producto no encontrado (buscado: '{codigo}')";
                LimpiarCamposProducto();
                return;
            }

            lbDescripcionProducto.Text = producto["descripcion"].ToString();
            bool editarPrecio = producto["EditarPrecio"] != DBNull.Value && Convert.ToBoolean(producto["EditarPrecio"]);

            if (precioPersonalizado.HasValue)
            {
                txtPrecio.Text = precioPersonalizado.Value.ToString("F0");
                lbDescripcionProducto.Text += " (Precio Balanza)";
            }
            else
            {
                txtPrecio.Text = Convert.ToDecimal(producto["precio"]).ToString("N2");
            }

            txtPrecio.Enabled = editarPrecio;
        }

        private void LimpiarCamposProducto()
        {
            txtPrecio.Text = "";
            txtPrecio.Enabled = false;
            lbDescripcionProducto.Text = ""; // AGREGAR ESTA LÍNEA SI NO ESTÁ
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            string textoIngresado = txtBuscarProducto.Text.Trim();
            string codigoBuscado = textoIngresado;
            bool esCodigoEspecial = false;

            if (string.IsNullOrEmpty(textoIngresado))
            {
                MessageBox.Show("Ingrese un código de producto válido.");
                txtBuscarProducto.Focus();
                return;
            }

            // NUEVO: Procesar código especial también en btnAgregar_Click
            if (textoIngresado.StartsWith("50") && textoIngresado.Length == 13)
            {
                try
                {
                    string codigoProducto = textoIngresado.Substring(2, 5); // 5 dígitos (posiciones 2-6)

                    // NUEVO: Eliminar ceros a la izquierda del código del producto
                    codigoProducto = codigoProducto.TrimStart('0');
                    if (string.IsNullOrEmpty(codigoProducto))
                        codigoProducto = "0";

                    codigoBuscado = codigoProducto;
                    esCodigoEspecial = true;
                }
                catch
                {
                    MessageBox.Show("Error procesando código especial.");
                    txtBuscarProducto.Focus();
                    return;
                }
            }
            else
            {
                // NUEVO: Para códigos normales también eliminar ceros a la izquierda
                codigoBuscado = codigoBuscado.TrimStart('0');
                if (string.IsNullOrEmpty(codigoBuscado))
                    codigoBuscado = "0";
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

            // 2. Obtener los datos del producto, incluyendo PermiteAcumular
            DataRow producto = null;
            bool permiteAcumular = false;
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"SELECT codigo, descripcion, precio, rubro, marca, proveedor, costo, PermiteAcumular 
                              FROM Productos WHERE codigo = @codigo";
                using (var adapter = new SqlDataAdapter(query, connection))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@codigo", codigoBuscado); // Usar codigoBuscado procesado
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count == 0)
                    {
                        MessageBox.Show($"Producto no encontrado (código: {codigoBuscado}).");
                        txtBuscarProducto.Focus();
                        return;
                    }
                    producto = dt.Rows[0];
                    permiteAcumular = producto["PermiteAcumular"] != DBNull.Value && Convert.ToBoolean(producto["PermiteAcumular"]);
                }
            }

            // 3. Determinar el precio a usar
            decimal precioUnitario;
            if (esCodigoEspecial)
            {
                // Para códigos especiales, SIEMPRE usar el precio del txtPrecio
                if (decimal.TryParse(txtPrecio.Text, out decimal precioEspecial))
                {
                    precioUnitario = precioEspecial;

                    // ACTUALIZAR el precio en la tabla Productos
                    using (var connection = new SqlConnection(connectionString))
                    {
                        var query = "UPDATE Productos SET precio = @precio WHERE codigo = @codigo";
                        using (var cmd = new SqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@precio", precioUnitario);
                            cmd.Parameters.AddWithValue("@codigo", codigoBuscado);
                            connection.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Error: Precio inválido en código especial.");
                    txtBuscarProducto.Focus();
                    return;
                }
            }
            else
            {
                // Para códigos normales, usar la lógica anterior
                if (txtPrecio.Enabled && decimal.TryParse(txtPrecio.Text, out decimal precioEditado))
                {
                    precioUnitario = precioEditado;
                }
                else
                {
                    precioUnitario = Convert.ToDecimal(producto["precio"]);
                }
            }

            // 4. Verificar si el producto ya está en la venta actual
            bool productoYaAgregado = false;
            int cantidadActual = 0;
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"SELECT cantidad FROM Ventas WHERE nrofactura = @nrofactura AND codigo = @codigo";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@nrofactura", nroRemitoActual);
                    cmd.Parameters.AddWithValue("@codigo", producto["codigo"]);
                    connection.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out cantidadActual))
                    {
                        productoYaAgregado = true;
                    }
                }
            }

            if (productoYaAgregado && permiteAcumular)
            {
                // 4a. Si ya existe y permite acumular, hacer UPDATE sumando cantidad y recalculando total
                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"UPDATE Ventas 
      SET cantidad = cantidad + @nuevaCantidad, 
          total = (cantidad + @nuevaCantidad) * @precio
      WHERE nrofactura = @nrofactura AND codigo = @codigo";
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@nuevaCantidad", cantidadPersonalizada);
                        cmd.Parameters.AddWithValue("@precio", precioUnitario);
                        cmd.Parameters.AddWithValue("@nrofactura", nroRemitoActual);
                        cmd.Parameters.AddWithValue("@codigo", producto["codigo"]);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                // 4b. Si no existe o no permite acumular, hacer INSERT (nueva línea)
                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"INSERT INTO Ventas 
    (codigo, descripcion, precio, rubro, marca, proveedor, costo, fecha, hora, cantidad, total, nrofactura, EsCtaCte, NombreCtaCte)
    VALUES (@codigo, @descripcion, @precio, @rubro, @marca, @proveedor, @costo, @fecha, @hora, @cantidad, @total, @nrofactura, @EsCtaCte, @NombreCtaCte)";
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@codigo", producto["codigo"]);
                        cmd.Parameters.AddWithValue("@descripcion", producto["descripcion"]);
                        cmd.Parameters.AddWithValue("@precio", precioUnitario); // Usar el precio correcto
                        cmd.Parameters.AddWithValue("@rubro", producto["rubro"]);
                        cmd.Parameters.AddWithValue("@marca", producto["marca"]);
                        cmd.Parameters.AddWithValue("@proveedor", producto["proveedor"]);
                        cmd.Parameters.AddWithValue("@costo", producto["costo"]);
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@hora", DateTime.Now.ToString("HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@cantidad", cantidadPersonalizada);
                        cmd.Parameters.AddWithValue("@total", precioUnitario * cantidadPersonalizada);
                        cmd.Parameters.AddWithValue("@nrofactura", nroRemitoActual);
                        cmd.Parameters.AddWithValue("@EsCtaCte", chkEsCtaCte.Checked);
                        cmd.Parameters.AddWithValue("@NombreCtaCte", chkEsCtaCte.Checked ? (object)cbnombreCtaCte.Text : DBNull.Value);

                        connection.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // 5. Mostrar todas las ventas del remito actual
            CargarVentasActuales();

            // Formatear columnas y encabezados (resto del código igual)
            FormatearDataGridView();

            // Dejar el foco en el campo buscar para el próximo producto
            txtBuscarProducto.Text = "";
            txtBuscarProducto.Focus();

            // Desmarcar el checkbox de cantidad después de agregar el producto
            if (chkCantidad.Checked)
            {
                chkCantidad.Checked = false;
                cantidadPersonalizada = 1;
            }
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
            dataGridView1.Rows.Clear();

            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.ClearSelection();
            dataGridView1.CurrentCell = null;
            dataGridView1.Enabled = true;

            // Evento para evitar cualquier selección por el usuario
            dataGridView1.SelectionChanged += (s, e) =>
            {
                dataGridView1.ClearSelection();
            };

            txtBuscarProducto.Focus();

            lbCantidadProductos.Text = "Productos: 0";
            lbTotal.Text = "Total: $0,00";

            // Configuración general del DataGridView
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Personalizar estilo de DataGridView
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 254);
            dataGridView1.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Black;
            dataGridView1.DefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 254);
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            //Color suave para filas alternas
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(220, 235, 255);
            dataGridView1.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;
        }

        private void ConfigurarPanelFooter()
        {
            // Crear el panel footer programáticamente
            Panel panelFooter = new Panel();
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Height = 60; // Aumentar altura para mejor visibilidad
            panelFooter.BackColor = Color.FromArgb(0, 120, 215);

            // Configurar lbCantidadProductos
            lbCantidadProductos.AutoSize = false;
            lbCantidadProductos.TextAlign = ContentAlignment.MiddleLeft;
            lbCantidadProductos.Dock = DockStyle.Left;
            lbCantidadProductos.Width = 250;
            lbCantidadProductos.Font = new Font("Segoe UI", 14F, FontStyle.Bold); // Reducir tamaño de fuente
            lbCantidadProductos.ForeColor = Color.White;
            lbCantidadProductos.Text = "Productos: 0";

            // Configurar lbTotal
            lbTotal.AutoSize = false;
            lbTotal.TextAlign = ContentAlignment.MiddleRight;
            lbTotal.Dock = DockStyle.Right;
            lbTotal.Width = 400; // Reducir ancho
            lbTotal.Font = new Font("Segoe UI", 24F, FontStyle.Bold); // Reducir tamaño de fuente
            lbTotal.ForeColor = Color.White;
            lbTotal.Text = "Total: $0,00";
            lbTotal.Padding = new Padding(0, 0, 20, 0);

            // Agregar los labels al panel
            panelFooter.Controls.Add(lbCantidadProductos);
            panelFooter.Controls.Add(lbTotal);

            // Agregar el panel al formulario
            this.Controls.Add(panelFooter);
            panelFooter.BringToFront();

            // IMPORTANTE: Ajustar el DataGridView para que no se superponga
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.Dock = DockStyle.None;
            dataGridView1.Location = new Point(0, 171);
            dataGridView1.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 171 - 60); // Restar altura del footer
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
                      ORDER BY idd DESC";
                using (var adapter = new SqlDataAdapter(query, connection))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@nrofactura", nroRemitoActual);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dataGridView1.DataSource = dt;
                    remitoActual = dt;
                }
            }

            // Ajustar anchos de columnas
            if (dataGridView1.Columns["descripcion"] != null)
            {
                dataGridView1.Columns["descripcion"].Width = 330; // Más ancha
            }
            if (dataGridView1.Columns["precio"] != null)
            {
                dataGridView1.Columns["precio"].Width = 110;
            }
            if (dataGridView1.Columns["cantidad"] != null)
            {
                dataGridView1.Columns["cantidad"].Width = 50; // Más angosta
            }

            lbCantidadProductos.Text = $"Productos: {dataGridView1.Rows.Count}";

            decimal sumaTotal = 0;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["total"].Value != null && decimal.TryParse(row.Cells["total"].Value.ToString(), out decimal valor))
                    sumaTotal += valor;
            }
            lbTotal.Text = $"Total: {sumaTotal:C2}";
        }

        // Reemplaza la llamada incorrecta a CrearFacturaBAsync con parámetros por la llamada correcta sin parámetros
        private async void btnFinalizarVenta_Click(object sender, EventArgs e)
        {
            remitoIncrementado = false;

            if (remitoActual == null || remitoActual.Rows.Count == 0)
                return;

            // Calcular el importe total antes de abrir el modal
            decimal importeTotal = 0;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["total"].Value != null && decimal.TryParse(row.Cells["total"].Value.ToString(), out decimal valor))
                    importeTotal += valor;
            }

            // Mostrar el modal de selección pasando el importe total Y la referencia al formulario padre
            using (var seleccion = new SeleccionImpresionForm(importeTotal, this)
            {
                TokenAfip = this.token,
                SignAfip = this.sign,
                OnProcesarVenta = async (tipoFactura, formaPago, cuitCliente, caeNumero, caeVencimiento) =>
                {
                    // Primero guardar en BD - CORREGIR: usar los parámetros que vienen del modal
                    await GuardarFacturaEnBD(tipoFactura, formaPago, cuitCliente, caeNumero, caeVencimiento);
                    
                    // Luego ejecutar la acción específica según el tipo
                    switch (tipoFactura)
                    {
                        case "Remito":
                            ImprimirTicket();
                            break;
                        case "FacturaB":
                            // Lógica específica para Factura B si es necesaria
                            break;
                        case "FacturaA":
                            // Lógica específica para Factura A si es necesaria
                            break;
                    }
                    
                    // Finalmente limpiar y reiniciar
                    LimpiarYReiniciarVenta();
                }
    })
    {
        seleccion.ShowDialog(this);
    }
}
        // Agregar este método público para que el modal pueda acceder a él
        public DataTable GetRemitoActual()
        {
            return remitoActual;
        }

        // Agregar este método público para que el modal pueda acceder al número de remito
        public int GetNroRemitoActual()
        {
            return nroRemitoActual;
        }

        // Agregar este método público para que el modal pueda acceder al nombre del comercio
        public string GetNombreComercio()
        {
            return nombreComercio;
        }

        // Agregar este método público para que el modal pueda acceder al domicilio
        public string GetDomicilioComercio()
        {
            return domicilioComercio;
        }

        private async Task GuardarFacturaEnBD(string tipoFactura, string formaPago, string cuitCliente = "", string caeNumero = "", DateTime? caeVencimiento = null)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                // Calcular el importe total
                decimal importeTotal = 0;
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Cells["total"].Value != null && decimal.TryParse(row.Cells["total"].Value.ToString(), out decimal valor))
                        importeTotal += valor;
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"INSERT INTO Facturas 
                (NumeroFactura, Fecha, Hora, ImporteTotal, FormadePago, esCtaCte, CtaCteNombre, 
                 Cajero, TipoFactura, CAENumero, CAEVencimiento, CUITCliente)
                VALUES 
                (@NumeroFactura, @Fecha, @Hora, @ImporteTotal, @FormadePago, @esCtaCte, @CtaCteNombre, 
                 @Cajero, @TipoFactura, @CAENumero, @CAEVencimiento, @CUITCliente)";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@NumeroFactura", nroRemitoActual);
                        cmd.Parameters.AddWithValue("@Fecha", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@Hora", DateTime.Now);
                        cmd.Parameters.AddWithValue("@ImporteTotal", importeTotal);
                        cmd.Parameters.AddWithValue("@FormadePago", formaPago);
                        cmd.Parameters.AddWithValue("@esCtaCte", chkEsCtaCte.Checked);
                        cmd.Parameters.AddWithValue("@CtaCteNombre", chkEsCtaCte.Checked ? (object)cbnombreCtaCte.Text : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Cajero", "1"); // Por defecto
                        cmd.Parameters.AddWithValue("@TipoFactura", tipoFactura);
                        
                        // Solo guardar CAE si es Factura A o B
                        if (tipoFactura == "FacturaA" || tipoFactura == "FacturaB")
                        {
                            cmd.Parameters.AddWithValue("@CAENumero", !string.IsNullOrEmpty(caeNumero) ? (object)caeNumero : DBNull.Value);
                            cmd.Parameters.AddWithValue("@CAEVencimiento", caeVencimiento.HasValue ? (object)caeVencimiento.Value : DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@CAENumero", DBNull.Value);
                            cmd.Parameters.AddWithValue("@CAEVencimiento", DBNull.Value);
                        }

                        // Solo guardar CUIT si es Factura A
                        if (tipoFactura == "FacturaA" && !string.IsNullOrEmpty(cuitCliente))
                        {
                            cmd.Parameters.AddWithValue("@CUITCliente", cuitCliente);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@CUITCliente", DBNull.Value);
                        }

                        connection.Open();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la factura en base de datos: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LimpiarYReiniciarVenta()
        {
            // Limpiar la grilla
            dataGridView1.DataSource = null;
            dataGridView1.Rows.Clear();
            
            // Actualizar labels
            lbCantidadProductos.Text = "Productos: 0";
            lbTotal.Text = "Total: $0,00";
            
            // Limpiar checkboxes
            chkEsCtaCte.Checked = false;
            
            // Limpiar campos de producto
            LimpiarCamposProducto();
            
            // Limpiar campos de búsqueda
            txtBuscarProducto.Text = "";
            lbDescripcionProducto.Text = "";
            
            // Resetear variables de control
            remitoActual = null;
            
            // IMPORTANTE: Usar BeginInvoke para asegurar que el foco se establezca después de que se complete el cierre del modal
            this.BeginInvoke(new Action(() =>
            {
                txtBuscarProducto.Focus();
                txtBuscarProducto.SelectAll();
            }));
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

            // Título principal (variable)
            SizeF nombreComercioSize = e.Graphics.MeasureString(nombreComercio, fontTitulo);
            e.Graphics.DrawString(
                nombreComercio,
                fontTitulo,
                Brushes.Black,
                leftMargin + ((tablaRight - leftMargin) - nombreComercioSize.Width) / 2,
                y
            );

            // Domicilio debajo del título, centrado y con fuente más pequeña
            Font fontDomicilio = new Font("Arial", 8, FontStyle.Regular);
            SizeF domicilioSize = e.Graphics.MeasureString(domicilioComercio, fontDomicilio);
            e.Graphics.DrawString(
                domicilioComercio,
                fontDomicilio,
                Brushes.Black,
                leftMargin + ((tablaRight - leftMargin) - domicilioSize.Width) / 2,
                y + nombreComercioSize.Height
            );
            y += nombreComercioSize.Height + domicilioSize.Height + 6;

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

                SizeF headerSize = e.Graphics.MeasureString(headers[i], fontBold);

                if (i == 3 || i == 4)
                {
                    // Alinear a la derecha PRECIO y TOTAL
                    headerX += colWidth - headerSize.Width;
                }
                else if (i == 2)
                {
                    // Centrar CANT.
                    headerX += (colWidth - headerSize.Width) / 2;
                }
                // CÓDIGO y DESCRIPCIÓN quedan alineados a la izquierda

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
            //y += rowHeight * 2;
            //e.Graphics.DrawString("Firma: ___________________________", font, Brushes.Black, leftMargin, y);
        }

        private void printDocumentRemito_PrintPageTicket(object sender, PrintPageEventArgs e)
        {
            if (remitoActual == null) return;

            // Márgenes y fuentes para ticket
            float leftMargin = e.MarginBounds.Left;
            float topMargin = e.MarginBounds.Top + 1;
            float rowHeight = 18;
            Font font = new Font("Arial", 9);
            Font fontBold = new Font("Arial", 9, FontStyle.Bold);
            Font fontTitulo = new Font("Arial", 16, FontStyle.Bold);
            Pen linePen = new Pen(Color.Black, 1);

            // Definir anchos de columnas para ticket
            float colCantidad = 20;
            float colDescripcion = 120;
            float colPrecio = 50;
            float colTotal = 70;

            float[] colX = {
                leftMargin,
                leftMargin + colCantidad,
                leftMargin + colCantidad + colDescripcion,
                leftMargin + colCantidad + colDescripcion + colPrecio
            };

            float tablaRight = colX[3] + colTotal;
            float y = topMargin;

            // Fecha y hora
            string fechaStr = $"Fecha: {DateTime.Now:dd/MM/yyyy}";
            string horaStr = $"Hora: {DateTime.Now:HH:mm}";
            SizeF fechaSize = e.Graphics.MeasureString(fechaStr, font);
            SizeF horaSize = e.Graphics.MeasureString(horaStr, font);
            float fechaX = tablaRight - fechaSize.Width;
            float horaX = tablaRight - horaSize.Width;
            e.Graphics.DrawString(fechaStr, font, Brushes.Black, fechaX, y);
            e.Graphics.DrawString(horaStr, font, Brushes.Black, horaX, y + fechaSize.Height);

            y += Math.Max(fechaSize.Height + horaSize.Height, 10) + 6;

            // Título principal
            SizeF nombreComercioSize = e.Graphics.MeasureString(nombreComercio, fontTitulo);
            e.Graphics.DrawString(
                nombreComercio,
                fontTitulo,
                Brushes.Black,
                leftMargin + ((tablaRight - leftMargin) - nombreComercioSize.Width) / 2,
                y
            );

            // Domicilio
            Font fontDomicilio = new Font("Arial", 8, FontStyle.Regular);
            SizeF domicilioSize = e.Graphics.MeasureString(domicilioComercio, fontDomicilio);
            e.Graphics.DrawString(
                domicilioComercio,
                fontDomicilio,
                Brushes.Black,
                leftMargin + ((tablaRight - leftMargin) - domicilioSize.Width) / 2,
                y + nombreComercioSize.Height
            );
            y += nombreComercioSize.Height + domicilioSize.Height + 6;

            // Título Remito
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

            // Encabezados
            string[] headers = { "C", "DESCRIPCIÓN", "PRECIO", "TOTAL" };
            float[] colWidths = { colCantidad, colDescripcion, colPrecio, colTotal };
            for (int i = 0; i < headers.Length; i++)
            {
                float headerX = colX[i];
                float colWidth = colWidths[i];
                SizeF headerSize = e.Graphics.MeasureString(headers[i], fontBold);

                if (i == 0)
                {
                    headerX += (colWidth - headerSize.Width) / 2;
                }
                else if (i == 1)
                {
                    // "DESCRIPCIÓN" alineado a la izquierda
                }
                else
                {
                    headerX += colWidth - headerSize.Width;
                }
                e.Graphics.DrawString(headers[i], fontBold, Brushes.Black, headerX, y);
            }

            y += rowHeight - 2;
            e.Graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 4;

            // Detalle productos
            int cantidadTotal = 0;
            foreach (DataRow row in remitoActual.Rows)
            {
                float filaY = y;

                // Descripción con salto de línea si es necesario
                string descripcion = row["descripcion"].ToString();
                float maxDescripcionWidth = colDescripcion - 2;
                List<string> lineasDescripcion = new List<string>();
                string resto = descripcion;
                while (!string.IsNullOrEmpty(resto))
                {
                    int len = resto.Length;
                    string linea = resto;
                    SizeF descSize = e.Graphics.MeasureString(linea, font);
                    while (descSize.Width > maxDescripcionWidth && len > 0)
                    {
                        len--;
                        linea = resto.Substring(0, len);
                        descSize = e.Graphics.MeasureString(linea + "...", font);
                    }
                    if (len < resto.Length)
                    {
                        linea = resto.Substring(0, len) + "...";
                        lineasDescripcion.Add(linea);
                        resto = resto.Substring(len);
                    }
                    else
                    {
                        lineasDescripcion.Add(linea);
                        break;
                    }
                }

                // Cantidad centrada
                string cantidadStr = row["cantidad"].ToString();
                SizeF cantidadSize = e.Graphics.MeasureString(cantidadStr, font);
                float cantidadX = colX[0] + (colCantidad - cantidadSize.Width) / 2;

                if (int.TryParse(cantidadStr, out int cantVal))
                    cantidadTotal += cantVal;

                // Precio y Total
                string precioStr = Convert.ToDecimal(row["precio"]).ToString("C2");
                SizeF precioSize = e.Graphics.MeasureString(precioStr, font);
                float precioX = colX[2] + colPrecio - precioSize.Width;

                string totalStr = Convert.ToDecimal(row["total"]).ToString("C2");
                SizeF totalSize = e.Graphics.MeasureString(totalStr, font);
                float totalX = colX[3] + colTotal - totalSize.Width;

                // Imprimir líneas
                for (int i = 0; i < lineasDescripcion.Count; i++)
                {
                    float cantidadYLinea = filaY + ((rowHeight - cantidadSize.Height) / 2);
                    if (i == 0)
                        e.Graphics.DrawString(cantidadStr, font, Brushes.Black, cantidadX, cantidadYLinea);

                    e.Graphics.DrawString(lineasDescripcion[i], font, Brushes.Black, colX[1], filaY);

                    if (i == 0)
                    {
                        e.Graphics.DrawString(precioStr, font, Brushes.Black, precioX, filaY);
                        e.Graphics.DrawString(totalStr, font, Brushes.Black, totalX, filaY);
                    }
                    filaY += rowHeight - 2;
                }

                y = filaY;
            }

            // Total final
            y += 6;
            e.Graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 6;

            decimal sumaTotal = 0;
            foreach (DataRow row in remitoActual.Rows)
            {
                if (decimal.TryParse(row["total"].ToString(), out decimal valor))
                    sumaTotal += valor;
            }
            string totalGeneralStr = $"TOTAL: {sumaTotal:C2}";
            SizeF totalGeneralSize = e.Graphics.MeasureString(totalGeneralStr, fontBold);
            float totalGeneralX = colX[3] + colTotal - totalGeneralSize.Width;
            e.Graphics.DrawString(totalGeneralStr, fontBold, Brushes.Black, totalGeneralX, y);

            string cantidadTotalStr = $"PRODUCTOS: {cantidadTotal}";
            e.Graphics.DrawString(cantidadTotalStr, fontBold, Brushes.Black, leftMargin, y);
        }

        private async Task ActualizarPrecioProductoAsync(string codigo, decimal precio)
        {
            using var connection = new SqlConnection(GetConnectionString());
            var query = "UPDATE Productos SET precio = @precio WHERE codigo = @codigo";
            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@precio", precio);
            cmd.Parameters.AddWithValue("@codigo", codigo);
            await connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }



        private void printDocumentRemito_PrintPageA4(object sender, PrintPageEventArgs e)
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

            // Título principal (variable)
            SizeF nombreComercioSize = e.Graphics.MeasureString(nombreComercio, fontTitulo);
            e.Graphics.DrawString(
                nombreComercio,
                fontTitulo,
                Brushes.Black,
                leftMargin + ((tablaRight - leftMargin) - nombreComercioSize.Width) / 2,
                y
            );

            // Domicilio debajo del título, centrado y con fuente más pequeña
            Font fontDomicilio = new Font("Arial", 8, FontStyle.Regular);
            SizeF domicilioSize = e.Graphics.MeasureString(domicilioComercio, fontDomicilio);
            e.Graphics.DrawString(
                domicilioComercio,
                fontDomicilio,
                Brushes.Black,
                leftMargin + ((tablaRight - leftMargin) - domicilioSize.Width) / 2,
                y + nombreComercioSize.Height
            );
            y += nombreComercioSize.Height + domicilioSize.Height + 6;

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

                SizeF headerSize = e.Graphics.MeasureString(headers[i], fontBold);

                if (i == 3 || i == 4)
                {
                    // Alinear a la derecha PRECIO y TOTAL
                    headerX += colWidth - headerSize.Width;
                }
                else if (i == 2)
                {
                    // Centrar CANT.
                    headerX += (colWidth - headerSize.Width) / 2;
                }
                // CÓDIGO y DESCRIPCIÓN quedan alineados a la izquierda

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
            //y += rowHeight * 2;
            //e.Graphics.DrawString("Firma: ___________________________", font, Brushes.Black, leftMargin, y);
        }

        private async Task<DataRow> BuscarProductoAsync(string codigo)
        {
            using var connection = new SqlConnection(GetConnectionString());
            var query = @"SELECT codigo, descripcion, precio, rubro, marca, proveedor, costo, 
                  PermiteAcumular, EditarPrecio FROM Productos WHERE codigo = @codigo";

            using var adapter = new SqlDataAdapter(query, connection);
            adapter.SelectCommand.Parameters.AddWithValue("@codigo", codigo);

            var dt = new DataTable();
            adapter.Fill(dt);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        private (string codigoBuscado, decimal? precioPersonalizado, bool esEspecial) ProcesarCodigo(string textoIngresado)
        {
            string codigoBuscado = textoIngresado.TrimStart('0');
            if (string.IsNullOrEmpty(codigoBuscado))
                codigoBuscado = "0";

            decimal? precioPersonalizado = null;
            bool esEspecial = false;

            if (textoIngresado.StartsWith("50") && textoIngresado.Length == 13)
            {
                try
                {
                    string codigoProducto = textoIngresado.Substring(2, 5).TrimStart('0');
                    if (string.IsNullOrEmpty(codigoProducto))
                        codigoProducto = "0";

                    string parteEntera = textoIngresado.Substring(7, 5);
                    int parteEnteraNumero = int.Parse(parteEntera);

                    precioPersonalizado = parteEnteraNumero;
                    codigoBuscado = codigoProducto;
                    esEspecial = true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error procesando código especial: {ex.Message}");
                }
            }

            return (codigoBuscado, precioPersonalizado, esEspecial);
        }

        private string GetConnectionString()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return config.GetConnectionString("DefaultConnection");
        }

        private void chkCantidad_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCantidad.Checked)
            {
                using (var modalCantidad = new ModalCantidadForm())
                {
                    if (modalCantidad.ShowDialog() == DialogResult.OK)
                    {
                        cantidadPersonalizada = modalCantidad.CantidadSeleccionada;
                        txtBuscarProducto.Focus();
                    }
                    else
                    {
                        chkCantidad.Checked = false;
                        txtBuscarProducto.Focus();
                    }
                }
            }
            else
            {
                cantidadPersonalizada = 1;
            }
        }

        private void chkEsCtaCte_CheckedChanged(object sender, EventArgs e)
        {
            if (chkEsCtaCte.Checked)
            {
                string rutaArchivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ctacte_nombres.txt");
                if (File.Exists(rutaArchivo))
                {
                    var nombres = File.ReadAllLines(rutaArchivo)
                                      .Select(l => l.Trim())
                                      .Where(l => !string.IsNullOrEmpty(l))
                                      .ToList();
                    cbnombreCtaCte.Items.Clear();
                    cbnombreCtaCte.Items.AddRange(nombres.ToArray());
                    if (cbnombreCtaCte.Items.Count > 0)
                        cbnombreCtaCte.SelectedIndex = 0;
                }
                cbnombreCtaCte.Enabled = true;
                cbnombreCtaCte.Visible = true;
                cbnombreCtaCte.Focus();
            }
            else
            {
                cbnombreCtaCte.Items.Clear();
                cbnombreCtaCte.Text = "";
                cbnombreCtaCte.Enabled = false;
                cbnombreCtaCte.Visible = false;
                txtBuscarProducto.Focus();
            }
        }

        private void cbnombreCtaCte_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtBuscarProducto.Focus();
        }

        private void ConfigurarEventosPrecio()
        {
            txtPrecio.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    this.SelectNextControl(txtPrecio, true, true, true, true);
                }
            };
            txtPrecio.Enter += (s, e) => txtPrecio.SelectAll();

            txtPrecio.KeyPress += (s, e) =>
            {
                if (e.KeyChar == '.')
                {
                    e.KeyChar = ',';
                }

                if (char.IsControl(e.KeyChar))
                    return;

                if (!char.IsDigit(e.KeyChar) && e.KeyChar != ',')
                {
                    e.Handled = true;
                    return;
                }

                var txt = ((TextBox)s).Text;
                int selStart = ((TextBox)s).SelectionStart;
                int selLength = ((TextBox)s).SelectionLength;

                string newText = txt.Substring(0, selStart) + e.KeyChar + txt.Substring(selStart + selLength);

                if (e.KeyChar == ',' && txt.Contains(","))
                {
                    e.Handled = true;
                    return;
                }

                string[] partes = newText.Split(',');
                if (partes[0].Length > 6)
                {
                    e.Handled = true;
                    return;
                }
                if (partes.Length > 1 && partes[1].Length > 2)
                {
                    e.Handled = true;
                    return;
                }
            };

            txtPrecio.Leave += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtPrecio.Text))
                {
                    if (!decimal.TryParse(txtPrecio.Text, out decimal valor))
                    {
                        MessageBox.Show("Ingrese un precio válido (solo números y hasta 2 decimales).");
                        txtPrecio.Focus();
                    }
                    else
                    {
                        txtPrecio.Text = valor.ToString("N2");
                    }
                }
            };

            txtPrecio.TextChanged += async (s, e) =>
            {
                if (!txtPrecio.Enabled) return;

                string codigoBuscado = txtBuscarProducto.Text.Trim();
                if (string.IsNullOrEmpty(codigoBuscado)) return;

                if (decimal.TryParse(txtPrecio.Text, out decimal nuevoPrecio))
                {
                    await ActualizarPrecioProductoAsync(codigoBuscado, nuevoPrecio);
                }
            };
        }

        private void ConfigurarTextBoxes()
        {
            txtBuscarProducto.BorderStyle = BorderStyle.FixedSingle;
            cbnombreCtaCte.DropDownStyle = ComboBoxStyle.DropDownList;
            cbnombreCtaCte.FlatStyle = FlatStyle.Flat;
        }

        private void FormatearDataGridView()
        {
            ConfigurarColumna("precio", "C2", DataGridViewContentAlignment.MiddleRight);
            ConfigurarColumna("total", "C2", DataGridViewContentAlignment.MiddleRight);
            ConfigurarColumna("cantidad", null, DataGridViewContentAlignment.MiddleCenter, 50);
            ConfigurarColumna("descripcion", null, null, 330);

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.HeaderText = col.HeaderText.ToUpper();
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void ConfigurarColumna(string nombre, string formato = null,
            DataGridViewContentAlignment? alineacion = null, int? ancho = null)
        {
            var columna = dataGridView1.Columns[nombre];
            if (columna == null) return;

            if (!string.IsNullOrEmpty(formato))
                columna.DefaultCellStyle.Format = formato;
            if (alineacion.HasValue)
                columna.DefaultCellStyle.Alignment = alineacion.Value;
            if (ancho.HasValue)
                columna.Width = ancho.Value;

            if (nombre == "cantidad")
                columna.HeaderText = "CANT.";
        }

        private void ImprimirTicket()
        {
            printDocumentRemito.PrintPage -= printDocumentRemito_PrintPage;
            printDocumentRemito.PrintPage += printDocumentRemito_PrintPageTicket;

            int anchoTicket = (int)(75 / 25.4 * 100);
            int altoTicket = (int)(200 / 25.4 * 100);

            PaperSize ticketSize = new PaperSize("Ticket", anchoTicket, altoTicket);
            printDocumentRemito.DefaultPageSettings.PaperSize = ticketSize;
            printDocumentRemito.DefaultPageSettings.Margins = new Margins(8, 8, 8, 8);

            using (PrintPreviewDialog previewDialog = new PrintPreviewDialog())
            {
                previewDialog.Document = printDocumentRemito;
                previewDialog.WindowState = FormWindowState.Maximized;
                previewDialog.ShowDialog();
            }

            printDocumentRemito.PrintPage -= printDocumentRemito_PrintPageTicket;
        }
    }

    public class WSAAHelper
    {
        public static string CrearTRA(string service)
        {
            var uniqueId = ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();
            var generationTime = DateTime.UtcNow.AddMinutes(-10).ToString("yyyy-MM-ddTHH:mm:ssZ");
            var expirationTime = DateTime.UtcNow.AddMinutes(+10).ToString("yyyy-MM-ddTHH:mm:ssZ");

            return $@"<loginTicketRequest version=""1.0"">
                      <header>
                        <uniqueId>{uniqueId}</uniqueId>
                        <generationTime>{generationTime}</generationTime>
                        <expirationTime>{expirationTime}</expirationTime>
                      </header>
                      <service>{service}</service>
                    </loginTicketRequest>";
        }

        public static byte[] FirmarTRA(string traXml, string pfxPath, string pfxPassword)
        {
            var cert = new X509Certificate2(pfxPath, pfxPassword, X509KeyStorageFlags.MachineKeySet);
            var contentInfo = new ContentInfo(Encoding.UTF8.GetBytes(traXml));
            var signedCms = new SignedCms(contentInfo);
            var cmsSigner = new CmsSigner(cert);
            signedCms.ComputeSignature(cmsSigner);
            return signedCms.Encode();
        }

        public static async Task<string> LlamarWSAA(byte[] cms, string wsaaUrl)
        {
            string cmsBase64 = Convert.ToBase64String(cms);

            string soapRequest = $@"<?xml version=""1.0"" encoding=""UTF-8""?
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:wsaa=""http://wsaa.view.sua.dvadac.desein.afip.gov.ar/"">
              <soap:Header/>
              <soap:Body>
                <wsaa:loginCms>
                  <wsaa:in0>{cmsBase64}</wsaa:in0>
                </wsaa:loginCms>
              </soap:Body>
            </soap:Envelope>";

            using var client = new HttpClient();
            var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "\"\"");
            var response = await client.PostAsync(wsaaUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error WSAA: {response.StatusCode}\n{responseBody}");
            }

            return responseBody;
        }
    }

    public class AfipAuthenticator
    {
        private static string _token;
        private static string _sign;
        private static DateTime _expiration;

        public static (string token, string sign) GetTA(string service, string pfxPath, string pfxPassword, string wsaaUrl)
        {
            return GetTAAsync(service, pfxPath, pfxPassword, wsaaUrl).GetAwaiter().GetResult();
        }

        public static async Task<(string token, string sign)> GetTAAsync(string service, string pfxPath, string pfxPassword, string wsaaUrl)
        {
            string taPath = GetTaPath(service);

            var cachedTA = TryLoadCachedTA(taPath);
            if (cachedTA.HasValue)
            {
                return cachedTA.Value;
            }

            return await GenerateNewTAAsync(service, pfxPath, pfxPassword, wsaaUrl, taPath);
        }

        private static (string token, string sign)? TryLoadCachedTA(string taPath)
        {
            if (!File.Exists(taPath))
                return null;

            try
            {
                var taXml = new XmlDocument();
                taXml.Load(taPath);

                _token = taXml.SelectSingleNode("//token")?.InnerText;
                _sign = taXml.SelectSingleNode("//sign")?.InnerText;
                var expirationStr = taXml.SelectSingleNode("//expirationTime")?.InnerText;

                if (!DateTime.TryParse(expirationStr, out _expiration))
                {
                    File.Delete(taPath);
                    return null;
                }

                if (!string.IsNullOrEmpty(_token) && !string.IsNullOrEmpty(_sign) && _expiration > DateTime.UtcNow.AddMinutes(1))
                {
                    return (_token, _sign);
                }
            }
            catch
            {
                File.Delete(taPath);
            }

            return null;
        }

        private static async Task<(string token, string sign)> GenerateNewTAAsync(string service, string pfxPath, string pfxPassword, string wsaaUrl, string taPath)
        {
            string traXml = WSAAHelper.CrearTRA(service);
            byte[] cms = WSAAHelper.FirmarTRA(traXml, pfxPath, pfxPassword);
            string soapResponse = await WSAAHelper.LlamarWSAA(cms, wsaaUrl);

            var loginCmsReturn = ProcessSoapResponse(soapResponse);
            if (loginCmsReturn != null && !string.IsNullOrEmpty(loginCmsReturn))
            {
                File.WriteAllText(taPath, loginCmsReturn);
                return ParseTAFromXml(loginCmsReturn);
            }

            throw new Exception("No se pudo obtener una respuesta válida del servicio WSAA.");
        }

        private static string ProcessSoapResponse(string soapResponse)
        {
            var xml = new XmlDocument();
            xml.LoadXml(soapResponse);

            var nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsmgr.AddNamespace("ns1", "http://wsaa.view.sua.dvadac.desein.afip.gov.ar/");

            return xml.SelectSingleNode("//ns1:loginCmsReturn", nsmgr)?.InnerText;
        }

        private static (string token, string sign) ParseTAFromXml(string taXmlContent)
        {
            var taXml = new XmlDocument();
            taXml.LoadXml(taXmlContent);

            _token = taXml.SelectSingleNode("//token")?.InnerText;
            _sign = taXml.SelectSingleNode("//sign")?.InnerText;
            var expirationStr = taXml.SelectSingleNode("//expirationTime")?.InnerText;
            _expiration = DateTime.ParseExact(expirationStr, new[] { "yyyy-MM-ddTHH:mm:ss.fffK", "yyyy-MM-ddTHH:mm:ssK" }, CultureInfo.InvariantCulture, DateTimeStyles.None);

            return (_token, _sign);
        }

        private static string GetTaPath(string service)
        {
            string safeService = service.Replace("/", "_").Replace("\\", "_");
            return $"ta_{safeService}.xml";
        }
    }
}