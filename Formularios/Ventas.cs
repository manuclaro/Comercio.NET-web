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
using System.Windows.Forms;
using System.Xml;
using Comercio.NET.Servicios;
using Comercio.NET.Formularios;
using Comercio.NET.Models;
using Comercio.NET.Services;
using System.Net.Sockets;
using System.Net;

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

        private string nombreComercio = "Comercio";
        private string domicilioComercio = "domicilio";

        private string token;
        private string sign;

        private int cantidadPersonalizada = 1;
        private CheckBox chkCantidad;

        private bool validarStockHabilitado = true;

        // En lugar del Label lbTotal, usar un RichTextBox para mejor control de formato
        private RichTextBox rtbTotal;

        // NUEVO: Variable para controlar el estado de eliminación
        private bool procesandoEliminacion = false;

        // NUEVO: Variable para controlar el estado de edición de cantidad
        private bool procesandoEdicionCantidad = false;

        public Ventas()
        {
            InitializeComponent();
            ConfigurarEstilosFormulario();
            ConfigurarEventHandlers();
            CargarConfiguracion();
            ConfigurarCheckboxCantidad();
            ConfigurarAtajosTeclado();

            // DEBUG: Verificar estado de autenticación al abrir Ventas
            System.Diagnostics.Debug.WriteLine($"=== DEBUG VENTAS CONSTRUCTOR ===");
            System.Diagnostics.Debug.WriteLine($"Login habilitado: {AuthenticationService.ConfiguracionLogin?.LoginHabilitado}");
            System.Diagnostics.Debug.WriteLine($"Sesión activa: {AuthenticationService.SesionActual != null}");
            if (AuthenticationService.SesionActual?.Usuario != null)
            {
                var usuario = AuthenticationService.SesionActual.Usuario;
                System.Diagnostics.Debug.WriteLine($"Usuario logueado: {usuario.NombreUsuario}");
                System.Diagnostics.Debug.WriteLine($"Puede eliminar: {usuario.PuedeEliminarProductos}");
                System.Diagnostics.Debug.WriteLine($"Nivel: {usuario.Nivel}");
            }
            System.Diagnostics.Debug.WriteLine($"================================");

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
            btnAgregar.Enter += (s, e) => btnAgregar.PerformClick();

            ConfigurarEventosTextBox();
            ConfigurarEventosDataGridView();
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

        private async Task ActualizarPrecioProductoAsync(string codigo, decimal nuevoPrecio)
        {
            try
            {
                string connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = "UPDATE Productos SET precio = @precio WHERE codigo = @codigo";
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@precio", nuevoPrecio);
                        cmd.Parameters.AddWithValue("@codigo", codigo);
                        connection.Open();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Error silencioso para evitar interrumpir la experiencia del usuario
                System.Diagnostics.Debug.WriteLine($"Error actualizando precio: {ex.Message}");
            }
        }

        private void AbrirConsultaRapidaPrecios()
        {
            using (var consultaForm = new ConsultaPrecioForm())
            {
                consultaForm.ShowDialog(this);
            }
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
                else if (e.KeyCode == Keys.F6)
                {
                    e.SuppressKeyPress = true;
                    // Abrir consulta rápida de precios
                    AbrirConsultaRapidaPrecios();
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

            // NUEVO: Cargar configuración de validación de stock
            validarStockHabilitado = config.GetValue<bool>("Validaciones:ValidarStockDisponible", true);

            // DEBUG: Mostrar configuración cargada
            System.Diagnostics.Debug.WriteLine($"=== CONFIGURACIÓN STOCK ===");
            System.Diagnostics.Debug.WriteLine($"Validar stock habilitado: {validarStockHabilitado}");
            System.Diagnostics.Debug.WriteLine($"===========================");
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

            // MEJORADO: Estilos de selección más contrastantes
            dataGridView1.DefaultCellStyle.BackColor = Color.White;
            dataGridView1.DefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215); // Azul más intenso
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.White; // Texto blanco para mayor contraste
            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 9F);

            // Estilos de encabezados
            var headerStyle = dataGridView1.ColumnHeadersDefaultCellStyle;
            headerStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            headerStyle.BackColor = Color.FromArgb(248, 249, 250);
            headerStyle.ForeColor = Color.Black;

            // MEJORADO: Filas alternadas más oscuras para mejor diferenciación
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 245, 250); // Más oscuro
            dataGridView1.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dataGridView1.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

            // NUEVO: Configuración adicional para mejor experiencia visual
            dataGridView1.RowTemplate.Height = 28; // Filas un poco más altas
            dataGridView1.GridColor = Color.FromArgb(220, 220, 220);

            // Después de asignar el DataSource o en ConfigurarDataGridView, asegúrate de que la columna existe
            if (dataGridView1.Columns["codigo"] != null)
            {
                dataGridView1.Columns["codigo"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridView1.Columns["codigo"].Width = 100; // Puedes ajustar el valor a tu preferencia
            }
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
                // MEJORADO: Mensaje más claro para el usuario
                lbDescripcionProducto.Text = $"⚠️ Producto '{codigo}' no encontrado - Presione 'Agregar' para crearlo";
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
            lbDescripcionProducto.Text = "";
        }

        private async void btnAgregar_Click(object sender, EventArgs e)
        {
            string textoIngresado = txtBuscarProducto.Text.Trim();
            string codigoBuscado = textoIngresado;
            bool esCodigoTemporal = false;
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
            else if (textoIngresado.Length == 8)
            {
                // TRATamiento ESPECIAL PARA CÓDIGOS TEMPORALES DE TESTING (8 DÍGITOS)
                // Asumimos que vienen en el formato XXXXXXXX y son válidos para testing
                codigoBuscado = textoIngresado;
                esCodigoTemporal = true;
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

            // NUEVO: Verificar si el producto existe antes de continuar
            DataRow producto = null;
            using (var connection = new SqlConnection(connectionString))
            {
                // MODIFICADO: Incluir el campo cantidad (stock) en la consulta
                var query = @"SELECT codigo, descripcion, precio, rubro, marca, proveedor, costo, PermiteAcumular, cantidad 
                              FROM Productos WHERE codigo = @codigo";
                using (var adapter = new SqlDataAdapter(query, connection))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@codigo", codigoBuscado);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count == 0)
                    {
                        // CORREGIDO: Usar el CustomMessageBox con el namespace correcto
                        using (var customMsg = new CustomMessageBox(
                            $"El producto con código '{codigoBuscado}' no existe.\n\n" +
                            "¿Desea agregarlo ahora para continuar con la venta?",
                            "Producto no encontrado"))
                        {
                            var resultado = customMsg.ShowDialog(this);

                            if (resultado == DialogResult.Yes)
                            {
                                // Extraer precio si es código especial
                                decimal? precioPersonalizado = null;
                                if (esCodigoEspecial)
                                {
                                    try
                                    {
                                        string parteEntera = textoIngresado.Substring(7, 5);
                                        int parteEnteraNumero = int.Parse(parteEntera);
                                        precioPersonalizado = parteEnteraNumero;
                                    }
                                    catch
                                    {
                                        // Si hay error, continuar sin precio personalizado
                                    }
                                }

                                // CORREGIDO: Usar await correctamente
                                await AbrirFormularioAgregarProductoRapido(codigoBuscado, precioPersonalizado);
                                return;
                            }
                            else
                            {
                                txtBuscarProducto.Focus();
                                return;
                            }
                        }
                    }
                    producto = dt.Rows[0];
                }
            }

            // NUEVO: Verificar permiteAcumular para determinar si se puede agregar y modificar stock
            bool permiteAcumular = producto["PermiteAcumular"] != DBNull.Value && Convert.ToBoolean(producto["PermiteAcumular"]);

            // MODIFICADO: Solo verificar stock si el producto permite acumular (manejo de stock)
            int stockDisponible = Convert.ToInt32(producto["cantidad"]);
            if (permiteAcumular && validarStockHabilitado && stockDisponible < cantidadPersonalizada)
            {
                // CAMBIADO: Mostrar advertencia pero permitir continuar
                var resultado = MessageBox.Show(
                    $"⚠️ ADVERTENCIA: Stock insuficiente\n\n" +
                    $"Producto: {producto["descripcion"]}\n" +
                    $"Stock disponible: {stockDisponible}\n" +
                    $"Cantidad solicitada: {cantidadPersonalizada}\n\n" +
                    "¿Desea continuar de todas formas?\n" +
                    "(El stock quedará en negativo)",
                    "Stock Insuficiente",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (resultado != DialogResult.Yes)
                {
                    txtBuscarProducto.Focus();
                    return;
                }
            }

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

            // NUEVO: Obtener el porcentaje de IVA del producto
            decimal porcentajeIva = 0;
            using (var connection = new SqlConnection(connectionString))
            {
                var queryIva = @"SELECT iva FROM Productos WHERE codigo = @codigo";
                using (var cmd = new SqlCommand(queryIva, connection))
                {
                    cmd.Parameters.AddWithValue("@codigo", codigoBuscado);
                    connection.Open();
                    var resultIva = cmd.ExecuteScalar();
                    if (resultIva != null && resultIva != DBNull.Value)
                    {
                        porcentajeIva = Convert.ToDecimal(resultIva);
                    }
                }
                connection.Close();
            }

            // NUEVO: Calcular el IVA basado en el precio y porcentaje
            decimal ivaCalculado = CalcularIvaDesdeTotal(precioUnitario * cantidadPersonalizada, porcentajeIva);

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

            // MODIFICADO: Usar transacción para asegurar consistencia entre ventas y stock
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        if (productoYaAgregado && permiteAcumular)
                        {
                            // 4a. Si ya existe y permite acumular, hacer UPDATE sumando cantidad y recalculando total e IVA
                            var query = @"UPDATE Ventas 
                                          SET cantidad = cantidad + @nuevaCantidad, 
                                              total = (cantidad + @nuevaCantidad) * @precio,
                                              IvaCalculado = (@precio * (cantidad + @nuevaCantidad)) * @porcentajeIva / (100 + @porcentajeIva),
                                              PorcentajeIva = @porcentajeIva
                                          WHERE nrofactura = @nrofactura AND codigo = @codigo";
                            using (var cmd = new SqlCommand(query, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@nuevaCantidad", cantidadPersonalizada);
                                cmd.Parameters.AddWithValue("@precio", precioUnitario);
                                cmd.Parameters.AddWithValue("@porcentajeIva", porcentajeIva);
                                cmd.Parameters.AddWithValue("@nrofactura", nroRemitoActual);
                                cmd.Parameters.AddWithValue("@codigo", producto["codigo"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // 4b. Si no existe o no permite acumular, hacer INSERT (nueva línea) - INCLUIR PorcentajeIva
                            var query = @"INSERT INTO Ventas 
                                        (codigo, descripcion, precio, rubro, marca, proveedor, costo, fecha, hora, cantidad, total, nrofactura, EsCtaCte, NombreCtaCte, IvaCalculado, PorcentajeIva)
                                        VALUES (@codigo, @descripcion, @precio, @rubro, @marca, @proveedor, @costo, @fecha, @hora, @cantidad, @total, @nrofactura, @EsCtaCte, @NombreCtaCte, @ivaCalculado, @porcentajeIva)";
                            using (var cmd = new SqlCommand(query, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@codigo", producto["codigo"]);
                                cmd.Parameters.AddWithValue("@descripcion", producto["descripcion"]);
                                cmd.Parameters.AddWithValue("@precio", precioUnitario);
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

                                // NUEVO: Agregar IVA calculado y porcentaje de IVA
                                cmd.Parameters.AddWithValue("@ivaCalculado", ivaCalculado);
                                cmd.Parameters.AddWithValue("@porcentajeIva", porcentajeIva);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // MODIFICADO: Solo descontar stock si el producto permite acumular (manejo de inventario)
                        if (permiteAcumular)
                        {
                            var queryStock = @"UPDATE Productos 
                                               SET cantidad = cantidad - @cantidadVendida 
                                               WHERE codigo = @codigo";
                            using (var cmd = new SqlCommand(queryStock, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@cantidadVendida", cantidadPersonalizada);
                                cmd.Parameters.AddWithValue("@codigo", producto["codigo"]);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Si llegamos aquí, todo salió bien
                        transaction.Commit();

                        // DEBUG: Confirmar la operación con información más detallada
                        int stockFinal = permiteAcumular ? (stockDisponible - cantidadPersonalizada) : stockDisponible;
                        System.Diagnostics.Debug.WriteLine($"=== DESCUENTO STOCK ===");
                        System.Diagnostics.Debug.WriteLine($"Configuración ValidarStock: {validarStockHabilitado}");
                        System.Diagnostics.Debug.WriteLine($"Producto: {producto["codigo"]} - {producto["descripcion"]}");
                        System.Diagnostics.Debug.WriteLine($"PermiteAcumular: {permiteAcumular}");
                        System.Diagnostics.Debug.WriteLine($"Stock anterior: {stockDisponible}");
                        System.Diagnostics.Debug.WriteLine($"Cantidad vendida: {cantidadPersonalizada}");
                        System.Diagnostics.Debug.WriteLine($"Stock final: {stockFinal}");
                        System.Diagnostics.Debug.WriteLine($"Stock modificado: {permiteAcumular}");
                        System.Diagnostics.Debug.WriteLine($"=======================");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Error al procesar la venta: {ex.Message}", "Error",
                                       MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
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

            // CORREGIDO: Cambiar SelectionMode para permitir selección de filas completas
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false; // Permitir solo una fila seleccionada
            dataGridView1.Enabled = true;

            // CORREGIDO: Solo limpiar la selección inicial, pero permitir selecciones futuras
            dataGridView1.ClearSelection();
            dataGridView1.CurrentCell = null;

            txtBuscarProducto.Focus();

            lbCantidadProductos.Text = "Productos: 0";

            // CORREGIDO: Verificar si lbTotal existe antes de usarlo
            if (lbTotal != null)
            {
                lbTotal.Text = "Total: $0,00";
            }

            // Configuración general del DataGridView
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // MEJORADO: Personalizar estilo de DataGridView con mejor contraste de selección
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 249, 250);
            dataGridView1.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Black;

            // MEJORADO: Estilos de selección más contrastantes
            dataGridView1.DefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.DefaultCellStyle.BackColor = Color.White;
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215); // Azul más intenso
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.White; // Texto blanco para mayor contraste

            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // MEJORADO: Color más oscuro para filas alternas con mejor selección
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 242, 248); // Más oscuro
            dataGridView1.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dataGridView1.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

            // AGREGADO: Configurar eventos para eliminar productos (llamar al método que ya tienes)
            ConfigurarEventosDataGridView();
        }

        private void ConfigurarPanelFooter()
        {
            // Crear el panel footer programáticamente
            Panel panelFooter = new Panel();
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Height = 80; // Mantenemos la altura aumentada
            panelFooter.BackColor = Color.FromArgb(0, 120, 215);

            // Configurar lbCantidadProductos
            lbCantidadProductos.AutoSize = false;
            lbCantidadProductos.TextAlign = ContentAlignment.MiddleLeft;
            lbCantidadProductos.Dock = DockStyle.Left;
            lbCantidadProductos.Width = 250;
            lbCantidadProductos.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lbCantidadProductos.ForeColor = Color.White;
            lbCantidadProductos.Text = "Productos: 0";

            // NUEVO: Usar RichTextBox para mejor control de formato
            rtbTotal = new RichTextBox
            {
                AutoSize = false,
                Dock = DockStyle.Right,
                Width = 500,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.None,
                Padding = new Padding(0, 0, 20, 0),
                Multiline = true
            };

            // Agregar los controles al panel
            panelFooter.Controls.Add(lbCantidadProductos);
            panelFooter.Controls.Add(rtbTotal);

            // Agregar el panel al formulario
            this.Controls.Add(panelFooter);
            panelFooter.BringToFront();

            // IMPORTANTE: Ajustar el DataGridView para que no se superponga
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.Dock = DockStyle.None;
            dataGridView1.Location = new Point(0, 171);
            dataGridView1.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 171 - 80);
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
                // MODIFICADO: Cambiar el orden de las columnas - PorcentajeIva antes que IvaCalculado
                var query = @"SELECT id, codigo, descripcion, precio, cantidad, total, PorcentajeIva, IvaCalculado
                              FROM Ventas
                              WHERE nrofactura = @nrofactura
                              ORDER BY id DESC";
                using (var adapter = new SqlDataAdapter(query, connection))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@nrofactura", nroRemitoActual);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dataGridView1.DataSource = dt;
                    remitoActual = dt;
                }

               
            }

            // Ocultar la columna id
            if (dataGridView1.Columns["id"] != null)
            {
                dataGridView1.Columns["id"].Visible = false;
            }

            // MODIFICADO: Configurar la columna de porcentaje IVA primero (ahora aparecerá antes)
            if (dataGridView1.Columns["PorcentajeIva"] != null)
            {
                dataGridView1.Columns["PorcentajeIva"].HeaderText = "IVA %";
                dataGridView1.Columns["PorcentajeIva"].Width = 40;
                dataGridView1.Columns["PorcentajeIva"].DefaultCellStyle.Format = "N2";
                dataGridView1.Columns["PorcentajeIva"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                //dataGridView1.Columns["PorcentajeIva"].DefaultCellStyle.BackColor = Color.FromArgb(240, 248, 255);
                dataGridView1.Columns["PorcentajeIva"].DefaultCellStyle.ForeColor = Color.FromArgb(25, 118, 210);
                dataGridView1.Columns["PorcentajeIva"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                // NUEVO: Establecer el orden de visualización
                dataGridView1.Columns["PorcentajeIva"].DisplayIndex = 6; // Aparecerá después de total
            }

            // MODIFICADO: Configurar la columna IVA calculado después
            if (dataGridView1.Columns["IvaCalculado"] != null)
            {
                dataGridView1.Columns["IvaCalculado"].HeaderText = "IVA $";
                dataGridView1.Columns["IvaCalculado"].Width = 80;
                dataGridView1.Columns["IvaCalculado"].DefaultCellStyle.Format = "C2";
                dataGridView1.Columns["IvaCalculado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                // NUEVO: Establecer el orden de visualización
                dataGridView1.Columns["IvaCalculado"].DisplayIndex = 7; // Aparecerá después de IVA %
            }

            // Ajustar anchos de columnas existentes para hacer espacio
            if (dataGridView1.Columns["descripcion"] != null)
            {
                dataGridView1.Columns["descripcion"].Width = 240; // Reducido para hacer espacio
            }
            if (dataGridView1.Columns["precio"] != null)
            {
                dataGridView1.Columns["precio"].Width = 100;
            }
            if (dataGridView1.Columns["cantidad"] != null)
            {
                dataGridView1.Columns["cantidad"].Width = 50;
            }
            // Después de asignar el DataSource o en ConfigurarDataGridView, asegúrate de que la columna existe
            if (dataGridView1.Columns["codigo"] != null)
            {
                dataGridView1.Columns["codigo"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridView1.Columns["codigo"].Width = 100; // Puedes ajustar el valor a tu preferencia
            }
            if (dataGridView1.Columns["total"] != null)
            {
                dataGridView1.Columns["total"].Width = 100;
            }

            // Actualizar totales
            lbCantidadProductos.Text = $"Productos: {dataGridView1.Rows.Count}";

            decimal sumaTotal = 0;
            decimal sumaIva = 0;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["total"].Value != null && decimal.TryParse(row.Cells["total"].Value.ToString(), out decimal valorTotal))
                    sumaTotal += valorTotal;

                if (row.Cells["IvaCalculado"].Value != null && decimal.TryParse(row.Cells["IvaCalculado"].Value.ToString(), out decimal valorIva))
                    sumaIva += valorIva;
            }

            // NUEVO: Formatear con RichTextBox - Total grande e IVA pequeño debajo
            rtbTotal.Clear();
            rtbTotal.SelectionAlignment = HorizontalAlignment.Right;

            // Total con fuente grande
            rtbTotal.SelectionFont = new Font("Segoe UI", 24F, FontStyle.Bold);
            rtbTotal.AppendText($"TOTAL: {sumaTotal:C2}");

            // Nueva línea
            rtbTotal.AppendText("\n");

            // IVA con fuente considerablemente más pequeña
            rtbTotal.SelectionFont = new Font("Segoe UI", 11F, FontStyle.Regular);
            rtbTotal.AppendText($"IVA: {sumaIva:C2}");
        }

        // Método btnFinalizarVenta_Click - CORREGIDO
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

            // Mostrar el modal de selección
            using (var seleccion = new SeleccionImpresionForm(importeTotal, this)
            {
                TokenAfip = this.token,
                SignAfip = this.sign,
                OnProcesarVenta = async (tipoFactura, formaPago, cuitCliente, caeNumero, caeVencimiento, numeroFacturaAfip, numeroFormateado) =>
                {
                    await GuardarFacturaEnBD(tipoFactura, formaPago, cuitCliente, caeNumero, caeVencimiento, numeroFacturaAfip, numeroFormateado);
                }
            })
            {
                var resultado = seleccion.ShowDialog(this);

                if (resultado == DialogResult.OK)
                {
                    // CORREGIDO: Usar await para esperar que se complete la impresión
                    await ImprimirConServicioAsync(seleccion);

                    // Limpiar y reiniciar para nueva venta
                    LimpiarYReiniciarVenta();
                }
            }
        }

        // NUEVO: Método async separado para la impresión
        private async Task ImprimirConServicioAsync(SeleccionImpresionForm seleccion)
        {
            try
            {
                if (remitoActual == null || remitoActual.Rows.Count == 0)
                {
                    MessageBox.Show("No hay productos para imprimir.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Configurar el ticket según el tipo de comprobante
                var config = new TicketConfig
                {
                    NombreComercio = nombreComercio,
                    DomicilioComercio = domicilioComercio,
                    FormaPago = seleccion.OpcionPagoSeleccionada.ToString(),
                    MensajePie = "Gracias por su compra!"
                };

                // NUEVO: Configurar número y tipo según el comprobante seleccionado
                switch (seleccion.OpcionSeleccionada)
                {
                    case SeleccionImpresionForm.OpcionImpresion.RemitoTicket:
                        config.TipoComprobante = "REMITO";
                        config.NumeroComprobante = $"Remito N° {nroRemitoActual}";
                        break;

                    case SeleccionImpresionForm.OpcionImpresion.FacturaB:
                        config.TipoComprobante = "FacturaB"; // CORREGIDO: Usar "FacturaB" específicamente
                        config.NumeroComprobante = FormatearNumeroFacturaParaBD(6, 1, seleccion.NumeroFacturaAfip);
                        config.CAE = seleccion.CAENumero;
                        config.CAEVencimiento = seleccion.CAEVencimiento;
                        break;

                    case SeleccionImpresionForm.OpcionImpresion.FacturaA:
                        config.TipoComprobante = "FacturaA"; // CORREGIDO: Usar "FacturaA" específicamente
                        config.NumeroComprobante = FormatearNumeroFacturaParaBD(1, 1, seleccion.NumeroFacturaAfip);
                        config.CAE = seleccion.CAENumero;
                        config.CAEVencimiento = seleccion.CAEVencimiento;
                        break;
                }

                System.Diagnostics.Debug.WriteLine("🖨️ === INICIO IMPRESIÓN ===");
                System.Diagnostics.Debug.WriteLine($"TipoComprobante configurado: {config.TipoComprobante}");
                System.Diagnostics.Debug.WriteLine($"NumeroComprobante: {config.NumeroComprobante}");
                System.Diagnostics.Debug.WriteLine($"CAE: {config.CAE}");
                System.Diagnostics.Debug.WriteLine($"===========================");

                // CORREGIDO: Usar await con el servicio de impresión
                using (var ticketService = new TicketPrintingService())
                {
                    await ticketService.ImprimirTicket(remitoActual, config);
                }

                System.Diagnostics.Debug.WriteLine("✅ Impresión completada correctamente");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Error en impresión: {ex.Message}");
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

        // Agregar este método pública para que el modal pueda acceder al domicilio
        public string GetDomicilioComercio()
        {
            return domicilioComercio;
        }

        // Modificar el método GuardarFacturaEnBD para usar el método existente obtenerNumeroCajero()
        private async Task GuardarFacturaEnBD(string tipoFactura, string formaPago, string cuitCliente = "", string caeNumero = "", DateTime? caeVencimiento = null, int numeroFacturaAfip = 0, string numeroFormateado = "")
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                // CORREGIDO: Obtener el importe total e IVA directamente del DataGridView
                decimal importeTotal = 0;
                decimal ivaTotal = 0;

                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Cells["total"].Value != null && decimal.TryParse(row.Cells["total"].Value.ToString(), out decimal valorTotal))
                        importeTotal += valorTotal;

                    // NUEVO: Sumar el IVA de cada producto
                    if (row.Cells["IvaCalculado"].Value != null && decimal.TryParse(row.Cells["IvaCalculado"].Value.ToString(), out decimal valorIva))
                        ivaTotal += valorIva;
                }

                // SIMPLIFICADO: Usar los métodos helper existentes
                string usuarioActual = ObtenerUsuarioActual();
                int numeroCajero = obtenerNumeroCajero();

                using (var connection = new SqlConnection(connectionString))
                {
                    // MODIFICADO: Agregar el campo IVA en el INSERT
                    var query = @"INSERT INTO Facturas (NumeroRemito, NroFactura, Fecha, Hora, ImporteTotal, IVA, FormadePago, esCtaCte, CtaCteNombre, Cajero, TipoFactura, CAENumero, CAEVencimiento, CUITCliente, UsuarioVenta) 
                                 VALUES (@NumeroRemito, @NroFactura, @Fecha, @Hora, @ImporteTotal, @IVA, @FormadePago, @esCtaCte, @CtaCteNombre, 
                                 @Cajero, @TipoFactura, @CAENumero, @CAEVencimiento, @CUITCliente, @UsuarioVenta)";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@NumeroRemito", nroRemitoActual);

                        if (tipoFactura == "FacturaA" || tipoFactura == "FacturaB")
                        {
                            cmd.Parameters.AddWithValue("@NroFactura", !string.IsNullOrEmpty(numeroFormateado) ? numeroFormateado : DBNull.Value);
                            cmd.Parameters.AddWithValue("@CAENumero", !string.IsNullOrEmpty(caeNumero) ? (object)caeNumero : DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@NroFactura", DBNull.Value);
                            cmd.Parameters.AddWithValue("@CAENumero", DBNull.Value);
                        }

                        cmd.Parameters.AddWithValue("@Fecha", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@Hora", DateTime.Now);
                        cmd.Parameters.AddWithValue("@ImporteTotal", importeTotal);
                        cmd.Parameters.AddWithValue("@IVA", ivaTotal); // NUEVO: Parámetro para IVA
                        cmd.Parameters.AddWithValue("@FormadePago", formaPago);
                        cmd.Parameters.AddWithValue("@esCtaCte", chkEsCtaCte.Checked);
                        cmd.Parameters.AddWithValue("@CtaCteNombre", chkEsCtaCte.Checked ? (object)cbnombreCtaCte.Text : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Cajero", numeroCajero);
                        cmd.Parameters.AddWithValue("@TipoFactura", tipoFactura);
                        cmd.Parameters.AddWithValue("@UsuarioVenta", usuarioActual);

                        if (tipoFactura == "FacturaA" || tipoFactura == "FacturaB")
                        {
                            cmd.Parameters.AddWithValue("@CAEVencimiento", caeVencimiento.HasValue ? (object)caeVencimiento.Value : DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@CAEVencimiento", DBNull.Value);
                        }

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

                // DEBUG: Para verificar el importe e IVA que se está guardando
                System.Diagnostics.Debug.WriteLine($"=== GUARDADO FACTURA ===");
                System.Diagnostics.Debug.WriteLine($"Importe calculado: {importeTotal:C}");
                System.Diagnostics.Debug.WriteLine($"IVA calculado: {ivaTotal:C}");
                System.Diagnostics.Debug.WriteLine($"Subtotal (sin IVA): {(importeTotal - ivaTotal):C}");
                System.Diagnostics.Debug.WriteLine($"Importe guardado en BD: {importeTotal}");
                System.Diagnostics.Debug.WriteLine($"IVA guardado en BD: {ivaTotal}");
                System.Diagnostics.Debug.WriteLine($"=======================");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la factura en base de datos: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // CORREGIDO: Método actualizado para soportar eliminación parcial
        private async Task EliminarProductoConAuditoria(
            string codigo, string descripcion, decimal precio, int cantidadAEliminar, decimal totalAEliminar, string motivo,
            bool esEliminacionCompleta, int cantidadOriginal, bool permiteAcumular, int idVenta)
        {
            string connectionString = GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Devolver stock al producto (solo la cantidad eliminada)
                        await DevolverStockProducto(connection, transaction, codigo, cantidadAEliminar);

                        // 2. Registrar en auditoría (con la cantidad real eliminada)
                        await RegistrarEliminacionEnAuditoria(connection, transaction,
                            codigo, descripcion, precio, cantidadAEliminar, totalAEliminar, motivo);

                        // 3. Actualizar o eliminar de la venta actual
                        if (esEliminacionCompleta)
                        {
                            // Eliminar completamente el producto
                            await EliminarDeVentaActual(connection, transaction, codigo, permiteAcumular, idVenta);
                        }
                        else
                        {
                            // Actualizar cantidad y total en la venta
                            int nuevaCantidad = cantidadOriginal - cantidadAEliminar;
                            decimal nuevoTotal = Math.Round(precio * nuevaCantidad, 2);

                            await ActualizarCantidadEnVentaActual(connection, transaction, codigo, nuevaCantidad, nuevoTotal, idVenta);
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private async Task DevolverStockProducto(SqlConnection connection, SqlTransaction transaction,
            string codigo, int cantidad)
        {
            // NUEVO: Verificar primero si el producto permite acumular
            var queryPermiteAcumular = @"SELECT PermiteAcumular FROM Productos WHERE codigo = @codigo";
            bool permiteAcumular = false;

            using (var cmdPermite = new SqlCommand(queryPermiteAcumular, connection, transaction))
            {
                cmdPermite.Parameters.AddWithValue("@codigo", codigo);
                var resultPermite = await cmdPermite.ExecuteScalarAsync();
                if (resultPermite != null && resultPermite != DBNull.Value)
                {
                    permiteAcumular = Convert.ToBoolean(resultPermite);
                }
            }

            // MODIFICADO: Solo devolver stock si el producto permite acumular
            if (permiteAcumular)
            {
                var query = @"UPDATE Productos 
                              SET cantidad = ISNULL(cantidad, 0) + @cantidad 
                              WHERE codigo = @codigo";

                using (var cmd = new SqlCommand(query, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@cantidad", cantidad);
                    cmd.Parameters.AddWithValue("@codigo", codigo);
                    await cmd.ExecuteNonQueryAsync();
                }

                // DEBUG: Confirmar la devolución de stock
                System.Diagnostics.Debug.WriteLine($"=== DEVOLUCIÓN STOCK ===");
                System.Diagnostics.Debug.WriteLine($"Producto: {codigo}");
                System.Diagnostics.Debug.WriteLine($"Cantidad devuelta: {cantidad}");
                System.Diagnostics.Debug.WriteLine($"PermiteAcumular: {permiteAcumular}");
                System.Diagnostics.Debug.WriteLine($"Stock actualizado: SÍ");
                System.Diagnostics.Debug.WriteLine($"===========================");
            }
            else
            {
                // DEBUG: Informar que no se devuelve stock
                System.Diagnostics.Debug.WriteLine($"=== DEVOLUCIÓN STOCK ===");
                System.Diagnostics.Debug.WriteLine($"Producto: {codigo}");
                System.Diagnostics.Debug.WriteLine($"Cantidad que se intentó devolver: {cantidad}");
                System.Diagnostics.Debug.WriteLine($"PermiteAcumular: {permiteAcumular}");
                System.Diagnostics.Debug.WriteLine($"Stock actualizado: NO");
                System.Diagnostics.Debug.WriteLine($"===========================");
            }
        }

        // Método para buscar productos en la base de datos
        private async Task<DataRow> BuscarProductoAsync(String codigo)
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

        // Método para obtener la cadena de conexión
        private string GetConnectionString()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return config.GetConnectionString("DefaultConnection");
        }

        // Método para configurar eventos del precio
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

        // Método para configurar estilos de TextBoxes
        private void ConfigurarTextBoxes()
        {
            txtBuscarProducto.BorderStyle = BorderStyle.FixedSingle;
            cbnombreCtaCte.DropDownStyle = ComboBoxStyle.DropDownList;
            cbnombreCtaCte.FlatStyle = FlatStyle.Flat;
        }

        // Método para formatear el DataGridView
        private void FormatearDataGridView()
        {
            ConfigurarColumna("precio", "C2", DataGridViewContentAlignment.MiddleRight);
            ConfigurarColumna("total", "C2", DataGridViewContentAlignment.MiddleRight);
            ConfigurarColumna("PorcentajeIva", "N2", DataGridViewContentAlignment.MiddleCenter, 70); // AHORA PRIMERO
            ConfigurarColumna("IvaCalculado", "C2", DataGridViewContentAlignment.MiddleRight, 80); // AHORA SEGUNDO
            ConfigurarColumna("cantidad", null, DataGridViewContentAlignment.MiddleCenter, 50);
            ConfigurarColumna("descripcion", null, null, 240); // Reducido

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.HeaderText = col.HeaderText.ToUpper();
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Configurar encabezados específicos - ORDEN CAMBIADO
            if (dataGridView1.Columns["PorcentajeIva"] != null)
            {
                dataGridView1.Columns["PorcentajeIva"].HeaderText = "IVA %";
            }

            if (dataGridView1.Columns["IvaCalculado"] != null)
            {
                dataGridView1.Columns["IvaCalculado"].HeaderText = "IVA $";
            }
        }

        // Método para configurar columnas específicas del DataGridView
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

            // Configuraciones específicas por tipo de columna
            if (nombre == "cantidad")
                columna.HeaderText = "CANT.";
            else if (nombre == "precio")
                columna.HeaderText = "PRECIO";
            else if (nombre == "total")
                columna.HeaderText = "TOTAL";
            else if (nombre == "descripcion")
                columna.HeaderText = "DESCRIPCIÓN";
            else if (nombre == "IvaCalculado")
                columna.HeaderText = "IVA $";
            else if (nombre == "PorcentajeIva")
                columna.HeaderText = "IVA %";

            // Estilos especiales para columnas monetarias
            if (formato == "C2")
            {
                columna.DefaultCellStyle.ForeColor = Color.FromArgb(0, 100, 0); // Verde oscuro
                columna.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }

            if (dataGridView1.Columns["total"] != null)
            {
                dataGridView1.Columns["total"].DefaultCellStyle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
                dataGridView1.Columns["total"].DefaultCellStyle.ForeColor = Color.FromArgb(0, 80, 0); // Opcional
            }
        }

        // Método para procesar códigos especiales de productos
        private (string codigoBuscado, decimal? precioPersonalizado, bool esEspecial) ProcesarCodigo(string textoIngresado)
        {
            // Verificar si es un código especial de balanza (empieza con "50" y tiene 13 dígitos)
            if (textoIngresado.StartsWith(PREFIJO_CODIGO_ESPECIAL) && textoIngresado.Length == LONGITUD_CODIGO_ESPECIAL)
            {
                try
                {
                    // Extraer código del producto (posiciones 2-6, 5 dígitos)
                    string codigoProducto = textoIngresado.Substring(POSICION_CODIGO_PRODUCTO, LONGITUD_CODIGO_PRODUCTO)
                        .TrimStart('0');
                    if (string.IsNullOrEmpty(codigoProducto))
                        codigoProducto = "0";

                    // Extraer precio (posiciones 7-11, 5 dígitos)
                    string precioString = textoIngresado.Substring(POSICION_PRECIO, LONGITUD_PRECIO);
                    decimal precioPersonalizado = decimal.Parse(precioString);

                    return (codigoProducto, precioPersonalizado, true);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error procesando código especial: {ex.Message}");
                }
            }

            // Para códigos normales, eliminar ceros a la izquierda
            string codigoLimpio = textoIngresado.TrimStart('0');
            if (string.IsNullOrEmpty(codigoLimpio))
                codigoLimpio = "0";

            return (codigoLimpio, null, false);
        }

        // Método para calcular IVA desde el total
        private decimal CalcularIvaDesdeTotal(decimal total, decimal porcentajeIva)
        {
            if (porcentajeIva <= 0) return 0;

            // Fórmula: IVA = Total * (PorcentajeIVA / (100 + PorcentajeIVA))
            return total * (porcentajeIva / (100 + porcentajeIva));
        }

        // Evento para el checkbox de cantidad personalizada
        private void chkCantidad_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCantidad.Checked)
            {
                using (var cantidadForm = new ModalCantidadForm())
                {
                    cantidadForm.CantidadInicial = cantidadPersonalizada; // CORREGIDO: Establecer cantidad inicial
                    cantidadForm.DescripcionProducto = lbDescripcionProducto.Text;
                    if (cantidadForm.ShowDialog(this) == DialogResult.OK)
                    {
                        cantidadPersonalizada = cantidadForm.CantidadSeleccionada;
                        chkCantidad.Text = $"Cantidad: {cantidadPersonalizada}";
                    }
                    else
                    {
                        chkCantidad.Checked = false;
                        cantidadPersonalizada = 1;
                        chkCantidad.Text = "Cantidad";
                    }
                }
            }
            else
            {
                cantidadPersonalizada = 1;
                chkCantidad.Text = "Cantidad";
            }
        }

        // Evento para cuando cambia la selección de cuenta corriente
        private void cbnombreCtaCte_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Opcional: agregar lógica cuando cambie la selección
            if (cbnombreCtaCte.SelectedItem != null)
            {
                System.Diagnostics.Debug.WriteLine($"Cuenta corriente seleccionada: {cbnombreCtaCte.SelectedItem}");
            }
        }

        // Método para formatear número de factura para base de datos
        private string FormatearNumeroFacturaParaBD(int cbteTipo, int ptoVta, int numeroFactura)
        {
            string tipoTexto = cbteTipo switch
            {
                1 => "Factura A",
                6 => "Factura B",
                _ => cbteTipo.ToString("D4")
            };
            return $"{tipoTexto} N°:{ptoVta:D4}-{numeroFactura:D8}";
        }

        // Método para abrir formulario de agregar producto rápido
        private async Task AbrirFormularioAgregarProductoRapido(string codigo, decimal? precioPersonalizado)
        {
            using (var formAgregar = new frmAgregarProducto())
            {
                formAgregar.Modo = frmAgregarProducto.ModoFormulario.Agregar;
                formAgregar.Origen = frmAgregarProducto.OrigenLlamada.Ventas;

                // Preconfigurar con el código y precio si están disponibles
                if (formAgregar.ShowDialog(this) == DialogResult.OK)
                {
                    // El producto fue agregado, intentar agregarlo a la venta nuevamente
                    await Task.Delay(500); // Pequeña pausa para asegurar que se guardó

                    // Simular click en agregar nuevamente
                    btnAgregar_Click(this, EventArgs.Empty);
                }
            }
        }

        // Método para obtener usuario actual
        private string ObtenerUsuarioActual()
        {
            // Verificar si hay un usuario autenticado
            if (AuthenticationService.SesionActual?.Usuario != null)
            {
                return AuthenticationService.SesionActual.Usuario.NombreUsuario;
            }

            // Si no hay usuario autenticado, usar el nombre del equipo
            return Environment.MachineName;
        }

        // Método para obtener número de cajero
        private int obtenerNumeroCajero()
        {
            // Si hay un usuario autenticado, usar su ID o número
            if (AuthenticationService.SesionActual?.Usuario != null)
            {
                return AuthenticationService.SesionActual.Usuario.IdUsuarios;
            }

            // Si no hay usuario autenticado, usar 1 por defecto
            return 1;
        }

        // Método para configurar eventos del DataGridView
        private void ConfigurarEventosDataGridView()
        {
            // Evento para doble clic - editar cantidad
            dataGridView1.CellDoubleClick += async (s, e) =>
            {
                // CORREGIDO: Agregar control para evitar ejecución múltiple
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && !procesandoEliminacion && !procesandoEdicionCantidad)
                {
                    procesandoEdicionCantidad = true;

                    try
                    {
                        await EditarCantidadProducto(e.RowIndex);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error durante la edición: {ex.Message}", "Error",
                                       MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        // IMPORTANTE: Agregar delay para evitar eventos residuales
                        await Task.Delay(300);
                        procesandoEdicionCantidad = false;
                    }
                }
            };

            // CORREGIDO: Evento para tecla Delete usando variable de clase
            dataGridView1.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Delete && dataGridView1.SelectedRows.Count > 0 && !procesandoEliminacion && !procesandoEdicionCantidad)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;

                    procesandoEliminacion = true;

                    try
                    {
                        await EliminarProductoSeleccionado();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error durante la eliminación: {ex.Message}", "Error",
                                       MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        await Task.Delay(200);
                        procesandoEliminacion = false;
                    }
                }
            };

            // Limpiar eventos residuales
            dataGridView1.KeyUp += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete)
                {
                    e.Handled = true;
                }
            };

            // NUEVO: Evento MouseDown para seleccionar fila con clic derecho
            dataGridView1.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    // Obtener información del hit test para saber dónde se hizo clic
                    var hitTest = dataGridView1.HitTest(e.X, e.Y);

                    // Si se hizo clic en una fila válida (no en el header ni en área vacía)
                    if (hitTest.RowIndex >= 0 && hitTest.RowIndex < dataGridView1.Rows.Count)
                    {
                        // Limpiar selección actual
                        dataGridView1.ClearSelection();

                        // Seleccionar la fila donde se hizo clic derecho
                        dataGridView1.Rows[hitTest.RowIndex].Selected = true;

                        // CORREGIDO: Usar la columna donde se hizo clic, pero verificar que sea visible
                        var targetColumnIndex = hitTest.ColumnIndex;

                        // Si la columna donde se hizo clic no es visible, buscar la primera columna visible
                        if (targetColumnIndex < 0 || !dataGridView1.Columns[targetColumnIndex].Visible)
                        {
                            // Buscar la primera columna visible
                            for (int i = 0; i < dataGridView1.Columns.Count; i++)
                            {
                                if (dataGridView1.Columns[i].Visible)
                                {
                                    targetColumnIndex = i;
                                    break;
                                }
                            }
                        }

                        // Solo establecer CurrentCell si encontramos una columna visible
                        if (targetColumnIndex >= 0 && targetColumnIndex < dataGridView1.Columns.Count &&
                            dataGridView1.Columns[targetColumnIndex].Visible)
                        {
                            dataGridView1.CurrentCell = dataGridView1.Rows[hitTest.RowIndex].Cells[targetColumnIndex];
                        }

                        // DEBUG: Confirmar la selección
                        System.Diagnostics.Debug.WriteLine($"=== CLIC DERECHO ===");
                        System.Diagnostics.Debug.WriteLine($"Fila seleccionada: {hitTest.RowIndex}");
                        System.Diagnostics.Debug.WriteLine($"Columna objetivo: {targetColumnIndex}");
                        System.Diagnostics.Debug.WriteLine($"Producto: {dataGridView1.Rows[hitTest.RowIndex].Cells["codigo"].Value}");
                        System.Diagnostics.Debug.WriteLine($"==================");
                    }
                    else
                    {
                        // Si se hizo clic fuera de las filas, limpiar selección
                        dataGridView1.ClearSelection();
                        dataGridView1.CurrentCell = null;
                    }
                }
            };

            // Menu contextual para clic derecho
            var contextMenu = new ContextMenuStrip();

            var editarItem = new ToolStripMenuItem("✏️ Editar Cantidad");
            editarItem.Click += async (s, e) =>
            {
                if (dataGridView1.SelectedRows.Count > 0 && !procesandoEliminacion && !procesandoEdicionCantidad)
                {
                    procesandoEdicionCantidad = true;

                    try
                    {
                        await EditarCantidadProducto(dataGridView1.SelectedRows[0].Index);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error durante la edición: {ex.Message}", "Error",
                                       MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        await Task.Delay(300);
                        procesandoEdicionCantidad = false;
                    }
                }
            };
            contextMenu.Items.Add(editarItem);

            var eliminarItem = new ToolStripMenuItem("🗑️ Eliminar Producto");
            eliminarItem.Click += async (s, e) =>
            {
                if (!procesandoEliminacion && !procesandoEdicionCantidad)
                {
                    await EliminarProductoSeleccionado();
                }
            };
            contextMenu.Items.Add(eliminarItem);

            // NUEVO: Separador para organización visual
            contextMenu.Items.Add(new ToolStripSeparator());

            // NUEVO: Opción de información del producto
            var infoItem = new ToolStripMenuItem("ℹ️ Información del Producto");
            infoItem.Click += async (s, e) =>
            {
                if (dataGridView1.SelectedRows.Count > 0)
                {
                    var row = dataGridView1.SelectedRows[0];
                    string codigo = row.Cells["codigo"].Value.ToString();
                    string descripcion = row.Cells["descripcion"].Value.ToString();
                    decimal precio = Convert.ToDecimal(row.Cells["precio"].Value);
                    int cantidad = Convert.ToInt32(row.Cells["cantidad"].Value);
                    decimal total = Convert.ToDecimal(row.Cells["total"].Value);

                    // NUEVO: Obtener el stock actual del producto desde la base de datos
                    int stockActual = 0;
                    try
                    {
                        string connectionString = GetConnectionString();
                        using (var connection = new SqlConnection(connectionString))
                        {
                            var queryStock = @"SELECT ISNULL(cantidad, 0) FROM Productos WHERE codigo = @codigo";
                            using (var cmd = new SqlCommand(queryStock, connection))
                            {
                                cmd.Parameters.AddWithValue("@codigo", codigo);
                                connection.Open();
                                var result = await cmd.ExecuteScalarAsync();
                                if (result != null)
                                {
                                    stockActual = Convert.ToInt32(result);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // En caso de error, mostrar "N/A" en lugar del stock
                        System.Diagnostics.Debug.WriteLine($"Error obteniendo stock: {ex.Message}");
                    }

                    // Obtener información adicional si está disponible
                    string ivaInfo = "";
                    if (row.Cells["PorcentajeIva"] != null && row.Cells["PorcentajeIva"].Value != null)
                    {
                        decimal porcentajeIva = Convert.ToDecimal(row.Cells["PorcentajeIva"].Value);
                        decimal ivaCalculado = Convert.ToDecimal(row.Cells["IvaCalculado"].Value);
                        ivaInfo = $"\nIVA: {porcentajeIva:N2}% ({ivaCalculado:C2})";
                    }

                    // MODIFICADO: Incluir información del stock
                    string stockInfo = stockActual >= 0 ? stockActual.ToString() : "N/A";

                    MessageBox.Show(
                        $"📦 INFORMACIÓN DEL PRODUCTO\n\n" +
                        $"Código: {codigo}\n" +
                        $"Descripción: {descripcion}\n" +
                        $"Precio unitario: {precio:C2}\n" +
                        $"Cantidad en venta: {cantidad}\n" +
                        $"Stock disponible: {stockInfo}\n" +
                        $"Total: {total:C2}{ivaInfo}",
                        "Información del Producto",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            };
            contextMenu.Items.Add(infoItem);

            // MEJORADO: Configurar eventos de apertura/cierre del menú contextual
            contextMenu.Opening += (s, e) =>
            {
                // Verificar si hay una fila seleccionada antes de mostrar el menú
                bool haySeleccion = dataGridView1.SelectedRows.Count > 0;

                // CORREGIDO: Incluir procesandoEdicionCantidad en las validaciones
                editarItem.Enabled = haySeleccion && !procesandoEliminacion && !procesandoEdicionCantidad;
                eliminarItem.Enabled = haySeleccion && !procesandoEliminacion && !procesandoEdicionCantidad;
                infoItem.Enabled = haySeleccion;

                // Verificar permisos de eliminación
                if (haySeleccion && AuthenticationService.SesionActual?.Usuario != null)
                {
                    bool puedeEliminar = AuthenticationService.SesionActual.Usuario.PuedeEliminarProductos;
                    eliminarItem.Enabled = eliminarItem.Enabled && puedeEliminar;

                    // Cambiar texto si no tiene permisos
                    if (!puedeEliminar)
                    {
                        eliminarItem.Text = "🔒 Eliminar Producto (Sin permisos)";
                        eliminarItem.ForeColor = Color.Gray;
                    }
                    else
                    {
                        eliminarItem.Text = "🗑️ Eliminar Producto";
                        eliminarItem.ForeColor = Color.Black;
                    }
                }

                // Verificar si el usuario tiene permisos para editar cantidades
                bool puedeModificar = true;
                if (haySeleccion && AuthenticationService.SesionActual?.Usuario != null)
                {
                    puedeModificar = AuthenticationService.SesionActual.Usuario.PuedeEditarPrecios; // CAMBIADO: usar PuedeEditarPrecios en lugar de PuedeModificarCantidad
                }
                editarItem.Enabled = editarItem.Enabled && puedeModificar;

                // Verificar si el usuario tiene permisos para ver información detallada
                bool puedeVerInfo = true;
                if (haySeleccion && AuthenticationService.SesionActual?.Usuario != null)
                {
                    puedeVerInfo = AuthenticationService.SesionActual.Usuario.PuedeVerReportes; // CAMBIADO: usar PuedeVerReportes en lugar de PuedeVerInformacionProducto
                }
                infoItem.Enabled = infoItem.Enabled && puedeVerInfo;

                // Si no hay selección, cancelar la apertura del menú
                if (!haySeleccion)
                {
                    e.Cancel = true;
                }
            };

            dataGridView1.ContextMenuStrip = contextMenu;
        }

        // NUEVO: Evento para checkbox de cuenta corriente
        private void chkEsCtaCte_CheckedChanged(object sender, EventArgs e)
        {
            if (chkEsCtaCte.Checked)
            {
                // Cargar nombres de cuentas corrientes si está habilitado
                CargarCuentasCorrientes();
                cbnombreCtaCte.Visible = true;
                cbnombreCtaCte.Enabled = true;
            }
            else
            {
                // Ocultar y limpiar selección si está deshabilitado
                cbnombreCtaCte.Visible = false;
                cbnombreCtaCte.Enabled = false;
                cbnombreCtaCte.SelectedIndex = -1;
            }
        }

        // NUEVO: Método para cargar cuentas corrientes
        private void CargarCuentasCorrientes()
        {
            try
            {
                string connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    // Consultar tabla de cuentas corrientes o clientes
                    var query = "SELECT DISTINCT NombreCtaCte FROM Facturas WHERE NombreCtaCte IS NOT NULL AND NombreCtaCte != '' ORDER BY NombreCtaCte";

                    using (var adapter = new SqlDataAdapter(query, connection))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        cbnombreCtaCte.Items.Clear();

                        foreach (DataRow row in dt.Rows)
                        {
                            cbnombreCtaCte.Items.Add(row["NombreCtaCte"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando cuentas corrientes: {ex.Message}");
            }
        }

        // CORREGIDO: Método para eliminar producto seleccionado - CON SOPORTE PARA CANTIDAD PARCIAL
        private async Task EliminarProductoSeleccionado()
        {
            if (dataGridView1.SelectedRows.Count == 0)
                return;

            try
            {
                var row = dataGridView1.SelectedRows[0];
                string codigo = row.Cells["codigo"].Value.ToString();
                string descripcion = row.Cells["descripcion"].Value.ToString();
                decimal precio = Convert.ToDecimal(row.Cells["precio"].Value);
                int cantidadTotal = Convert.ToInt32(row.Cells["cantidad"].Value);
                decimal total = Convert.ToDecimal(row.Cells["total"].Value);
                int idVenta = Convert.ToInt32(row.Cells["id"].Value);

                // Verificar permisos de eliminación
                if (AuthenticationService.SesionActual?.Usuario != null)
                {
                    if (!AuthenticationService.SesionActual.Usuario.PuedeEliminarProductos)
                    {
                        MessageBox.Show("No tiene permisos para eliminar productos.", "Sin permisos",
                                      MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                // PASO 1: Pedir motivo Y cantidad a eliminar
                string motivo;
                int cantidadAEliminar;
                using (var motivoForm = new MotivoEliminacionForm(descripcion, cantidadTotal, codigo, precio))
                {
                    if (motivoForm.ShowDialog(this) != DialogResult.OK)
                        return; // Usuario canceló

                    motivo = motivoForm.Motivo;
                    cantidadAEliminar = motivoForm.CantidadAEliminar;
                }

                // Validar cantidad
                if (cantidadAEliminar <= 0 || cantidadAEliminar > cantidadTotal)
                {
                    MessageBox.Show($"Cantidad inválida. Debe estar entre 1 y {cantidadTotal}.", "Error",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // CALCULO: Determinar si es una eliminación completa o parcial
                bool esEliminacionCompleta = cantidadAEliminar == cantidadTotal;

                // CONFIRMAR: Mostrar mensaje de confirmación con detalle
                var resultado = MessageBox.Show(
                    $"¿Confirma la eliminación {(esEliminacionCompleta ? "completa" : "parcial")} de este producto?\n\n" +
                    $"Código: {codigo}\n" +
                    $"Descripción: {descripcion}\n" +
                    $"Cantidad a eliminar: {cantidadAEliminar} {(esEliminacionCompleta ? "" : $"(Quedará {cantidadTotal - cantidadAEliminar})")}\n" +
                    $"Total a eliminar: {Math.Round(precio * cantidadAEliminar, 2):C2}\n\n" +
                    $"Motivo: {motivo}\n\n" +
                    "Esta acción no se puede deshacer.",
                    "Confirmar eliminación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                // PASO 3: Solo proceder si confirma
                if (resultado == DialogResult.Yes)
                {
                    // Obtener permiteAcumular desde la base de datos
                    bool permiteAcumular = false;
                    using (var conn = new SqlConnection(GetConnectionString()))
                    {
                        var query = "SELECT PermiteAcumular FROM Productos WHERE codigo = @codigo";
                        using (var cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@codigo", codigo);
                            conn.Open();
                            var result = await cmd.ExecuteScalarAsync();
                            if (result != null && result != DBNull.Value)
                                permiteAcumular = Convert.ToBoolean(result);
                        }
                    }

                    await EliminarProductoConAuditoria(
                        codigo, descripcion, precio, cantidadAEliminar, Math.Round(precio * cantidadAEliminar, 2), motivo,
                        esEliminacionCompleta, cantidadTotal, permiteAcumular, idVenta
                    );

                    // Refrescar la vista
                    CargarVentasActuales();
                    FormatearDataGridView();

                    // DEBUG
                    System.Diagnostics.Debug.WriteLine($"=== PRODUCTO ELIMINADO ===");
                    System.Diagnostics.Debug.WriteLine($"Código: {codigo}");
                    System.Diagnostics.Debug.WriteLine($"Cantidad eliminada: {cantidadAEliminar} de {cantidadTotal}");
                    System.Diagnostics.Debug.WriteLine($"Es eliminación completa: {esEliminacionCompleta}");
                    System.Diagnostics.Debug.WriteLine($"Motivo: {motivo}");
                    System.Diagnostics.Debug.WriteLine($"Usuario: {ObtenerUsuarioActual()}");
                    System.Diagnostics.Debug.WriteLine($"Remito: {nroRemitoActual}");
                    System.Diagnostics.Debug.WriteLine($"==========================");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar producto: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // CORREGIDO: Método para editar cantidad de producto
        private async Task EditarCantidadProducto(int rowIndex)
        {
            try
            {
                if (rowIndex < 0 || rowIndex >= dataGridView1.Rows.Count)
                    return;

                var row = dataGridView1.Rows[rowIndex];
                string codigo = row.Cells["codigo"].Value.ToString();
                string descripcion = row.Cells["descripcion"].Value.ToString();
                int cantidadActual = Convert.ToInt32(row.Cells["cantidad"].Value);
                decimal precio = Convert.ToDecimal(row.Cells["precio"].Value);
                int idVenta = Convert.ToInt32(row.Cells["id"].Value);

                using (var cantidadForm = new ModalCantidadForm())
                {
                    cantidadForm.Text = "Editar Cantidad";
                    cantidadForm.CantidadInicial = cantidadActual;
                    cantidadForm.DescripcionProducto = descripcion; // <-- Asigna la descripción aquí

                    if (cantidadForm.ShowDialog(this) == DialogResult.OK)
                    {
                        int nuevaCantidad = cantidadForm.CantidadSeleccionada;

                        if (nuevaCantidad <= 0)
                        {
                            MessageBox.Show("La cantidad debe ser mayor a 0.", "Cantidad inválida",
                                          MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        if (nuevaCantidad == cantidadActual)
                        {
                            return; // No hay cambios
                        }

                        string connectionString = GetConnectionString();

                        using (var connection = new SqlConnection(connectionString))
                        {
                            connection.Open();
                            using (var transaction = connection.BeginTransaction())
                            {
                                try
                                {
                                    int diferenciaCantidad = nuevaCantidad - cantidadActual;
                                    decimal nuevoTotal = Math.Round(precio * nuevaCantidad, 2);

                                    // Consultar si permite acumular
                                    bool permiteAcumular = false;
                                    var queryPermite = @"SELECT PermiteAcumular FROM Productos WHERE codigo = @codigo";
                                    using (var cmdPermite = new SqlCommand(queryPermite, connection, transaction))
                                    {
                                        cmdPermite.Parameters.AddWithValue("@codigo", codigo);
                                        var resultPermite = await cmdPermite.ExecuteScalarAsync();
                                        if (resultPermite != null && resultPermite != DBNull.Value)
                                            permiteAcumular = Convert.ToBoolean(resultPermite);
                                    }

                                    // Actualizar stock solo si permite acumular
                                    if (diferenciaCantidad != 0 && permiteAcumular)
                                    {
                                        var queryStock = @"UPDATE Productos 
                                                  SET cantidad = ISNULL(cantidad, 0) + @diferenciaCantidad 
                                                  WHERE codigo = @codigo";
                                        using (var cmdStock = new SqlCommand(queryStock, connection, transaction))
                                        {
                                            cmdStock.Parameters.AddWithValue("@diferenciaCantidad", diferenciaCantidad);
                                            cmdStock.Parameters.AddWithValue("@codigo", codigo);
                                            await cmdStock.ExecuteNonQueryAsync();
                                        }
                                    }

                                    // Actualizar la venta SOLO la fila seleccionada si no permite acumular
                                    if (permiteAcumular)
                                        await ActualizarCantidadEnVentaActual(connection, transaction, codigo, nuevaCantidad, nuevoTotal);
                                    else
                                        await ActualizarCantidadEnVentaActual(connection, transaction, codigo, nuevaCantidad, nuevoTotal, idVenta);

                                    transaction.Commit();
                                }
                                catch
                                {
                                    transaction.Rollback();
                                    throw;
                                }
                            }
                        }

                        // Refrescar la vista
                        CargarVentasActuales();
                        FormatearDataGridView();

                        // DEBUG
                        System.Diagnostics.Debug.WriteLine($"=== EDICIÓN CANTIDAD ===");
                        System.Diagnostics.Debug.WriteLine($"Producto: {codigo} - {descripcion}");
                        System.Diagnostics.Debug.WriteLine($"Cantidad anterior: {cantidadActual}");
                        System.Diagnostics.Debug.WriteLine($"Cantidad nueva: {nuevaCantidad}");
                        System.Diagnostics.Debug.WriteLine($"Nuevo total: {precio * nuevaCantidad:C}");
                        System.Diagnostics.Debug.WriteLine($"Stock actualizado: {true}");
                        System.Diagnostics.Debug.WriteLine($"==============================");
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Error al editar cantidad: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MÉTODO FALTANTE: RegistrarEliminacionEnAuditoria
        private async Task RegistrarEliminacionEnAuditoria(SqlConnection connection, SqlTransaction transaction,
            string codigo, string descripcion, decimal precio, int cantidad, decimal total, string motivo)
        {
            try
            {
                // Obtener información del usuario actual
                string usuarioActual = ObtenerUsuarioActual();
                int numeroCajero = obtenerNumeroCajero();

                var query = @"INSERT INTO AuditoriaEliminaciones 
                         (Fecha, Hora, CodigoProducto, DescripcionProducto, PrecioUnitario, CantidadEliminada, 
                          TotalEliminado, MotivoEliminacion, Usuario, NumeroCajero, NumeroRemito)
                         VALUES 
                         (@Fecha, @Hora, @Codigo, @Descripcion, @Precio, @Cantidad, @Total, @Motivo, 
                          @Usuario, @Cajero, @NumeroRemito)";

                using (var cmd = new SqlCommand(query, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@Fecha", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@Hora", DateTime.Now.ToString("HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.Parameters.AddWithValue("@Descripcion", descripcion);
                    cmd.Parameters.AddWithValue("@Precio", precio);
                    cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                    cmd.Parameters.AddWithValue("@Total", total);
                    cmd.Parameters.AddWithValue("@Motivo", motivo);
                    cmd.Parameters.AddWithValue("@Usuario", usuarioActual);
                    cmd.Parameters.AddWithValue("@Cajero", numeroCajero);
                    cmd.Parameters.AddWithValue("@NumeroRemito", nroRemitoActual);

                    await cmd.ExecuteNonQueryAsync();
                }

                // DEBUG: Confirmar registro de auditoría
                System.Diagnostics.Debug.WriteLine($"=== AUDITORÍA ELIMINACIÓN ===");
                System.Diagnostics.Debug.WriteLine($"Producto: {codigo} - {descripcion}");
                System.Diagnostics.Debug.WriteLine($"Cantidad eliminada: {cantidad}");
                System.Diagnostics.Debug.WriteLine($"Total eliminado: {total:C2}");
                System.Diagnostics.Debug.WriteLine($"Motivo: {motivo}");
                System.Diagnostics.Debug.WriteLine($"Usuario: {usuarioActual}");
                System.Diagnostics.Debug.WriteLine($"Remito: {nroRemitoActual}");
                System.Diagnostics.Debug.WriteLine($"============================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registrando auditoría: {ex.Message}");
                // No lanzar excepción para no interrumpir el proceso principal
            }
        }

        // MÉTODO FALTANTE: ActualizarCantidadEnVentaActual (versión simplificada para eliminación parcial)
        private async Task ActualizarCantidadEnVentaActual(SqlConnection connection, SqlTransaction transaction,
            string codigo, int nuevaCantidad, decimal nuevoTotal, int? idVenta = null)
        {
            try
            {
                // Obtener el porcentaje de IVA del producto para recalcular
                decimal porcentajeIva = 0;
                var queryIva = @"SELECT ISNULL(iva, 0) FROM Productos WHERE codigo = @codigo";
                using (var cmd = new SqlCommand(queryIva, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@codigo", codigo);
                    var resultIva = await cmd.ExecuteScalarAsync();
                    if (resultIva != null && resultIva != DBNull.Value)
                    {
                        porcentajeIva = Convert.ToDecimal(resultIva);
                    }
                }

                // Calcular el nuevo IVA
                decimal nuevoIvaCalculado = CalcularIvaDesdeTotal(nuevoTotal, porcentajeIva);

                string query;
                if (idVenta.HasValue)
                {
                    query = @"UPDATE Ventas 
                          SET cantidad = @nuevaCantidad, 
                              total = @nuevoTotal,
                              IvaCalculado = @nuevoIvaCalculado,
                              PorcentajeIva = @porcentajeIva
                          WHERE nrofactura = @nrofactura AND id = @id";
                }
                else
                {
                    query = @"UPDATE Ventas 
                          SET cantidad = @nuevaCantidad, 
                              total = @nuevoTotal,
                              IvaCalculado = @nuevoIvaCalculado,
                              PorcentajeIva = @porcentajeIva
                         WHERE nrofactura = @nrofactura AND codigo = @codigo";
                }

                using (var cmd = new SqlCommand(query, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@nuevaCantidad", nuevaCantidad);
                    cmd.Parameters.AddWithValue("@nuevoTotal", nuevoTotal);
                    cmd.Parameters.AddWithValue("@nuevoIvaCalculado", nuevoIvaCalculado);
                    cmd.Parameters.AddWithValue("@porcentajeIva", porcentajeIva);
                    cmd.Parameters.AddWithValue("@nrofactura", nroRemitoActual);
                    if (idVenta.HasValue)
                        cmd.Parameters.AddWithValue("@id", idVenta.Value);
                    else
                        cmd.Parameters.AddWithValue("@codigo", codigo);

                    await cmd.ExecuteNonQueryAsync();
                }

                // DEBUG: Confirmar actualización
                System.Diagnostics.Debug.WriteLine($"=== ACTUALIZACIÓN CANTIDAD (ELIMINACIÓN PARCIAL) ===");
                System.Diagnostics.Debug.WriteLine($"Producto: {codigo}");
                System.Diagnostics.Debug.WriteLine($"Nueva cantidad: {nuevaCantidad}");
                System.Diagnostics.Debug.WriteLine($"Nuevo total: {nuevoTotal:C2}");
                System.Diagnostics.Debug.WriteLine($"Nuevo IVA: {nuevoIvaCalculado:C2}");
                System.Diagnostics.Debug.WriteLine($"====================================================");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al actualizar cantidad en venta: {ex.Message}");
            }
        }



        // MÉTODO FALTANTE: EliminarDeVentaActual
        private async Task EliminarDeVentaActual(SqlConnection connection, SqlTransaction transaction, string codigo, bool permiteAcumular, int? idVenta = null)
        {
            try
            {
                string query;
                if (permiteAcumular)
                {
                    query = @"DELETE FROM Ventas WHERE nrofactura = @nrofactura AND codigo = @codigo";
                }
                else
                {
                    query = @"DELETE FROM Ventas WHERE nrofactura = @nrofactura AND id = @id";
                }

                using (var cmd = new SqlCommand(query, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@nrofactura", nroRemitoActual);
                    if (permiteAcumular)
                        cmd.Parameters.AddWithValue("@codigo", codigo);
                    else
                        cmd.Parameters.AddWithValue("@id", idVenta.Value);

                    int filasAfectadas = await cmd.ExecuteNonQueryAsync();
                    if (filasAfectadas == 0)
                        throw new Exception("No se encontró el producto para eliminar.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al eliminar producto de venta: {ex.Message}");
            }
        }

        // MÉTODO FALTANTE: LimpiarYReiniciarVenta
        private void LimpiarYReiniciarVenta()
        {
            try
            {
                // 1. Limpiar el DataGridView
                dataGridView1.DataSource = null;
                dataGridView1.Rows.Clear();
                remitoActual = null;

                // 2. Resetear los totales
                lbCantidadProductos.Text = "Productos: 0";

                if (rtbTotal != null)
                {
                    rtbTotal.Clear();
                    rtbTotal.SelectionAlignment = HorizontalAlignment.Right;
                    rtbTotal.SelectionFont = new Font("Segoe UI", 24F, FontStyle.Bold);
                    rtbTotal.AppendText("TOTAL: $0,00");
                    rtbTotal.AppendText("\n");
                    rtbTotal.SelectionFont = new Font("Segoe UI", 11F, FontStyle.Regular);
                    rtbTotal.AppendText("IVA: $0,00");
                }

                // 3. Limpiar campos de entrada
                txtBuscarProducto.Text = "";
                txtPrecio.Text = "";
                txtPrecio.Enabled = false;
                lbDescripcionProducto.Text = "";

                // 4. Resetear checkboxes y combos
                chkEsCtaCte.Checked = false;
                cbnombreCtaCte.Visible = false;
                cbnombreCtaCte.Enabled = false;
                cbnombreCtaCte.SelectedIndex = -1;

                if (chkCantidad.Checked)
                {
                    chkCantidad.Checked = false;
                    cantidadPersonalizada = 1;
                }

                // 5. Resetear variables de control
                remitoIncrementado = false;
                procesandoEliminacion = false;
                procesandoEdicionCantidad = false;

                // 6. Limpiar selección del DataGridView
                dataGridView1.ClearSelection();
                dataGridView1.CurrentCell = null;

                // 7. Devolver el foco al campo de búsqueda
                txtBuscarProducto.Focus();

                // DEBUG: Confirmar limpieza
                System.Diagnostics.Debug.WriteLine($"=== LIMPIEZA Y REINICIO ===");
                System.Diagnostics.Debug.WriteLine($"Venta limpiada correctamente");
                System.Diagnostics.Debug.WriteLine($"Remito incrementado: {remitoIncrementado}");
                System.Diagnostics.Debug.WriteLine($"Estado procesando eliminación: {procesandoEliminacion}");
                System.Diagnostics.Debug.WriteLine($"Estado procesando edición: {procesandoEdicionCantidad}");
                System.Diagnostics.Debug.WriteLine($"===========================");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al limpiar la venta: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);

                // En caso de error, al menos intentar limpiar lo básico
                txtBuscarProducto.Text = "";
                txtBuscarProducto.Focus();
            }
        }
    }
}