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

        private bool validarStockHabilitado = true; // Variable de instancia para almacenar la configuración

        // En lugar del Label lbTotal, usar un RichTextBox para mejor control de formato
        private RichTextBox rtbTotal;

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
            lbDescripcionProducto.Text = ""; // AGREGAR ESTA LÍNEA SI NO ESTÁ
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
                // TRATAMIENTO ESPECIAL PARA CÓDIGOS TEMPORALES DE TESTING (8 DÍGITOS)
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
                        // MODIFICADO: Usar MessageBox personalizado sin botón por defecto
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

            // MODIFICADO: Si permiteAcumular = false, mostrar mensaje informativo
            //if (!permiteAcumular)
            //{
            //    MessageBox.Show(
            //        $"ℹ️ INFORMACIÓN: Producto sin control de stock\n\n" +
            //        $"Producto: {producto["descripcion"]}\n" +
            //        $"Este producto no maneja inventario automático.",
            //        "Producto sin control de stock", 
            //        MessageBoxButtons.OK, 
            //        MessageBoxIcon.Information);
            //}

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
                            // 4a. Si ya existe y permite acumular, hacer UPDATE sumando cantidad y recalculando total
                            var query = @"UPDATE Ventas 
                                          SET cantidad = cantidad + @nuevaCantidad, 
                                              total = (cantidad + @nuevaCantidad) * @precio,
                                              IvaCalculado = (@precio * (cantidad + @nuevaCantidad)) * @porcentajeIva / (100 + @porcentajeIva)
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
                            // 4b. Si no existe o no permite acumular, hacer INSERT (nueva línea)
                            var query = @"INSERT INTO Ventas 
                                        (codigo, descripcion, precio, rubro, marca, proveedor, costo, fecha, hora, cantidad, total, nrofactura, EsCtaCte, NombreCtaCte, IvaCalculado)
                                        VALUES (@codigo, @descripcion, @precio, @rubro, @marca, @proveedor, @costo, @fecha, @hora, @cantidad, @total, @nrofactura, @EsCtaCte, @NombreCtaCte, @ivaCalculado)";
                            using (var cmd = new SqlCommand(query, connection, transaction))
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
                                
                                // NUEVO: Agregar IVA calculado
                                cmd.Parameters.AddWithValue("@ivaCalculado", ivaCalculado);

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
            lbTotal.Text = "Total: $0,00";

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
                // MODIFICADO: Incluir IvaCalculado en la consulta
                var query = @"SELECT id, codigo, descripcion, precio, cantidad, total, IvaCalculado
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

            // NUEVO: Configurar la nueva columna IVA
            if (dataGridView1.Columns["IvaCalculado"] != null)
            {
                dataGridView1.Columns["IvaCalculado"].HeaderText = "IVA";
                dataGridView1.Columns["IvaCalculado"].Width = 80;
                dataGridView1.Columns["IvaCalculado"].DefaultCellStyle.Format = "C2";
                dataGridView1.Columns["IvaCalculado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // Ajustar anchos de columnas existentes
            if (dataGridView1.Columns["descripcion"] != null)
            {
                dataGridView1.Columns["descripcion"].Width = 260; // Reducido para hacer espacio al IVA
            }
            if (dataGridView1.Columns["precio"] != null)
            {
                dataGridView1.Columns["precio"].Width = 100;
            }
            if (dataGridView1.Columns["cantidad"] != null)
            {
                dataGridView1.Columns["cantidad"].Width = 50;
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

        // Método btnFinalizarVenta_Click
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
                    // SIMPLIFICADO: Solo llamar al servicio de impresión
                    ImprimirConServicio(seleccion);

                    // Limpiar y reiniciar para nueva venta
                    LimpiarYReiniciarVenta();
                }
            }
        }

        // NUEVO método simplificado para manejar la impresión
        private void ImprimirConServicio(SeleccionImpresionForm seleccion)
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
                        config.NumeroComprobante = nroRemitoActual.ToString(); // Usar número de remito
                        break;

                    case SeleccionImpresionForm.OpcionImpresion.FacturaB:
                        config.TipoComprobante = "FACTURA";
                        // NUEVO: Formatear número de factura para mostrar
                        config.NumeroComprobante = FormatearNumeroFacturaParaBD(6, 1, seleccion.NumeroFacturaAfip);
                        config.CAE = seleccion.CAENumero;
                        config.CAEVencimiento = seleccion.CAEVencimiento;
                        break;

                    case SeleccionImpresionForm.OpcionImpresion.FacturaA:
                        config.TipoComprobante = "FACTURA";
                        // NUEVO: Formatear número de factura para mostrar
                        config.NumeroComprobante = FormatearNumeroFacturaParaBD(1, 1, seleccion.NumeroFacturaAfip);
                        config.CAE = seleccion.CAENumero;
                        config.CAEVencimiento = seleccion.CAEVencimiento;
                        // Obtener CUIT del formulario de selección si es necesario
                        break;
                }

                // Usar el servicio de impresión
                using (var ticketService = new TicketPrintingService())
                {
                    ticketService.ImprimirTicket(remitoActual, config);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                // CORREGIDO: Obtener el importe total directamente del DataGridView en lugar de parsearlo del label
                decimal importeTotal = 0;
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Cells["total"].Value != null && decimal.TryParse(row.Cells["total"].Value.ToString(), out decimal valor))
                        importeTotal += valor;
                }

                // ALTERNATIVA: Si se quiere mantener el parsing del label, usar InvariantCulture
                /*
                decimal importeTotal = 0;
                string textoTotal = lbTotal.Text.Replace("Total: $", "").Trim();
                // Usar InvariantCulture para parsing correcto independiente de la configuración regional
                if (decimal.TryParse(textoTotal, NumberStyles.Currency, CultureInfo.CurrentCulture, out decimal total))
                {
                    importeTotal = total;
                }
                */

                // SIMPLIFICADO: Usar los métodos helper existentes
                string usuarioActual = ObtenerUsuarioActual();
                int numeroCajero = obtenerNumeroCajero();

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"INSERT INTO Facturas (NumeroRemito, NroFactura, Fecha, Hora, ImporteTotal, FormadePago, esCtaCte, CtaCteNombre, Cajero, TipoFactura, CAENumero, CAEVencimiento, CUITCliente, UsuarioVenta) 
                                 VALUES (@NumeroRemito, @NroFactura, @Fecha, @Hora, @ImporteTotal, @FormadePago, @esCtaCte, @CtaCteNombre, 
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
                        cmd.Parameters.AddWithValue("@FormadePago", formaPago);
                        cmd.Parameters.AddWithValue("@esCtaCte", chkEsCtaCte.Checked);
                        cmd.Parameters.AddWithValue("@CtaCteNombre", chkEsCtaCte.Checked ? (object)cbnombreCtaCte.Text : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Cajero", numeroCajero); // Usar el número de cajero correcto
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

                // DEBUG: Para verificar el importe que se está guardando
                System.Diagnostics.Debug.WriteLine($"=== GUARDADO FACTURA ===");
                System.Diagnostics.Debug.WriteLine($"Texto del label: {lbTotal.Text}");
                System.Diagnostics.Debug.WriteLine($"Importe calculado: {importeTotal:C}");
                System.Diagnostics.Debug.WriteLine($"Importe guardado en BD: {importeTotal}");
                System.Diagnostics.Debug.WriteLine($"=======================");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la factura en base de datos: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MODIFICADO: Simplificar el método RegistrarEliminacionEnAuditoria
        private async Task RegistrarEliminacionEnAuditoria(SqlConnection connection, SqlTransaction transaction,
    string codigo, string descripcion, decimal precio, int cantidad, decimal total, string motivo)
{
    // Obtener datos adicionales del producto
    var queryProducto = @"SELECT rubro, marca, proveedor FROM Productos WHERE codigo = @codigo";
    string rubro = "", marca = "", proveedor = "";

    using (var cmd = new SqlCommand(queryProducto, connection, transaction))
    {
        cmd.Parameters.AddWithValue("@codigo", codigo);
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (reader.Read())
            {
                rubro = reader["rubro"]?.ToString() ?? "";
                marca = reader["marca"]?.ToString() ?? "";
                proveedor = reader["proveedor"]?.ToString() ?? "";
            }
        }
    }

    // NUEVO: Obtener IVA calculado de la venta
    var queryVentaIva = @"SELECT IvaCalculado, fecha, hora, EsCtaCte, NombreCtaCte FROM Ventas 
                          WHERE nrofactura = @nrofactura AND codigo = @codigo 
                          ORDER BY id DESC";
    
    decimal ivaCalculado = 0;
    DateTime fechaHoraVentaOriginal = DateTime.Now;
    bool esCtaCte = false;
    string nombreCtaCte = "";

    using (var cmd = new SqlCommand(queryVentaIva, connection, transaction))
    {
        cmd.Parameters.AddWithValue("@nrofactura", nroRemitoActual);
        cmd.Parameters.AddWithValue("@codigo", codigo);
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (reader.Read())
            {
                ivaCalculado = reader["IvaCalculado"] != DBNull.Value ? Convert.ToDecimal(reader["IvaCalculado"]) : 0;
                
                try
                {
                    DateTime fechaVenta = Convert.ToDateTime(reader["fecha"]);
                    string horaVentaString = reader["hora"]?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(horaVentaString) && TimeSpan.TryParse(horaVentaString, out TimeSpan tiempo))
                    {
                        fechaHoraVentaOriginal = fechaVenta.Date + tiempo;
                    }
                    else
                    {
                        fechaHoraVentaOriginal = fechaVenta;
                    }
                }
                catch
                {
                    fechaHoraVentaOriginal = DateTime.Now;
                }

                esCtaCte = reader["EsCtaCte"] != DBNull.Value && Convert.ToBoolean(reader["EsCtaCte"]);
                nombreCtaCte = reader["NombreCtaCte"]?.ToString() ?? "";
            }
        }
    }

    // Obtener usuario y cajero
    string usuarioEliminacion = ObtenerUsuarioActual();
    int numeroCajero = obtenerNumeroCajero();

    // INSERT para auditoría
    var queryAuditoria = @"INSERT INTO AuditoriaProductosEliminados 
              (CodigoProducto, DescripcionProducto, PrecioUnitario, Cantidad, TotalEliminado,
               NumeroFactura, FechaHoraVentaOriginal, FechaEliminacion,
               UsuarioEliminacion, MotivoEliminacion, EsCtaCte, NombreCtaCte,
               IPUsuario, NombreEquipo, NumeroCajero, IvaEliminado)
              VALUES 
              (@CodigoProducto, @DescripcionProducto, @PrecioUnitario, @Cantidad, @TotalEliminado,
               @NumeroFactura, @FechaHoraVentaOriginal, @FechaEliminacion,
               @UsuarioEliminacion, @MotivoEliminacion, @EsCtaCte, @NombreCtaCte,
               @IPUsuario, @NombreEquipo, @NumeroCajero, @IvaEliminado)";

    using (var cmd = new SqlCommand(queryAuditoria, connection, transaction))
    {
        cmd.Parameters.AddWithValue("@CodigoProducto", codigo);
        cmd.Parameters.AddWithValue("@DescripcionProducto", descripcion);
        cmd.Parameters.AddWithValue("@PrecioUnitario", precio);
        cmd.Parameters.AddWithValue("@Cantidad", cantidad);
        cmd.Parameters.AddWithValue("@TotalEliminado", total);
        cmd.Parameters.AddWithValue("@NumeroFactura", nroRemitoActual);
        cmd.Parameters.AddWithValue("@FechaHoraVentaOriginal", fechaHoraVentaOriginal);
        cmd.Parameters.AddWithValue("@FechaEliminacion", DateTime.Now);
        cmd.Parameters.AddWithValue("@UsuarioEliminacion", usuarioEliminacion);
        cmd.Parameters.AddWithValue("@MotivoEliminacion", motivo ?? "");
        cmd.Parameters.AddWithValue("@EsCtaCte", esCtaCte);
        cmd.Parameters.AddWithValue("@NombreCtaCte", nombreCtaCte ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@IPUsuario", ObtenerIPLocal());
        cmd.Parameters.AddWithValue("@NombreEquipo", Environment.MachineName);
        cmd.Parameters.AddWithValue("@NumeroCajero", numeroCajero);
        cmd.Parameters.AddWithValue("@IvaEliminado", ivaCalculado);

        await cmd.ExecuteNonQueryAsync();
    }
}
        private async Task EliminarProductoConAuditoria(string codigo, string descripcion,
            decimal precio, int cantidad, decimal total, string motivo)
        {
            string connectionString = GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Devolver stock al producto
                        await DevolverStockProducto(connection, transaction, codigo, cantidad);

                        // 2. Registrar en auditoría
                        await RegistrarEliminacionEnAuditoria(connection, transaction,
                            codigo, descripcion, precio, cantidad, total, motivo);

                        // 3. Eliminar de la venta actual
                        //await EliminarDeVentaActual(connection, transaction, codigo);

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

            using (var cmd = new SqlCommand(queryPermiteAcumular, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@codigo", codigo);
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    permiteAcumular = Convert.ToBoolean(result);
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
            ConfigurarColumna("IvaCalculado", "C2", DataGridViewContentAlignment.MiddleRight, 80); // NUEVO
            ConfigurarColumna("cantidad", null, DataGridViewContentAlignment.MiddleCenter, 50);
            ConfigurarColumna("descripcion", null, null, 280); // Reducido

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.HeaderText = col.HeaderText.ToUpper();
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            
            // NUEVO: Configurar encabezado específico para IVA
            if (dataGridView1.Columns["IvaCalculado"] != null)
            {
                dataGridView1.Columns["IvaCalculado"].HeaderText = "IVA";
            }
        }

        // Método para configurar columnas específicas
        private void ConfigurarColumna(string nombre, string formato = null,
            DataGridViewContentAlignment? alineacion = null, int? ancho = null)
        {
            var colonia = dataGridView1.Columns[nombre];
            if (colonia == null) return;

            if (!string.IsNullOrEmpty(formato))
                colonia.DefaultCellStyle.Format = formato;
            if (alineacion.HasValue)
                colonia.DefaultCellStyle.Alignment = alineacion.Value;
            if (ancho.HasValue)
                colonia.Width = ancho.Value;

            if (nombre == "cantidad")
                colonia.HeaderText = "CANT.";
        }

        // Método para formatear número de factura
        private string FormatearNumeroFacturaParaBD(int cbteTipo, int ptoVta, int numeroFactura)
        {
            string letra = cbteTipo switch
            {
                1 => "A",
                6 => "B",
                11 => "C",
                51 => "M",
                _ => "X"
            };

            return $"{letra}{ptoVta:D4}-{numeroFactura:D8}";
        }

        // Método para limpiar y reiniciar venta
        private void LimpiarYReiniciarVenta()
        {
            // Limpiar la grilla
            dataGridView1.DataSource = null;
            dataGridView1.Rows.Clear();

            // Actualizar labels
            lbCantidadProductos.Text = "Productos: 0";
            
            // MODIFICADO: Usar RichTextBox para el total
            if (rtbTotal != null)
            {
                rtbTotal.Clear();
                rtbTotal.SelectionAlignment = HorizontalAlignment.Right;
                rtbTotal.SelectionFont = new Font("Segoe UI", 24F, FontStyle.Bold);
                rtbTotal.AppendText("TOTAL: $0,00\n");
                rtbTotal.SelectionFont = new Font("Segoe UI", 11F, FontStyle.Regular);
                rtbTotal.AppendText("IVA: $0,00");
            }

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

        // Evento para cantidad personalizada
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

        // Evento para cuenta corriente
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

        // Evento para combo de cuenta corriente
        private void cbnombreCtaCte_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtBuscarProducto.Focus();
        }

        // Método para abrir formulario de agregar producto rápido
        private async Task AbrirFormularioAgregarProductoRapido(string codigo, decimal? precioPersonalizado)
        {
            try
            {
                using (var formAgregar = new frmAgregarProducto())
                {
                    // Configurar el formulario en modo agregar
                    formAgregar.Modo = frmAgregarProducto.ModoFormulario.Agregar;
                    formAgregar.Origen = frmAgregarProducto.OrigenLlamada.Ventas;
                    formAgregar.StartPosition = FormStartPosition.CenterParent;

                    // Preconfigurar el código y precio si están disponibles
                    formAgregar.Load += (s, e) =>
                    {
                        var txtCodigo = formAgregar.Controls.Find("txtCodigo", true).FirstOrDefault() as TextBox;
                        if (txtCodigo != null)
                        {
                            txtCodigo.Text = codigo;
                            txtCodigo.ReadOnly = true;
                        }

                        if (precioPersonalizado.HasValue)
                        {
                            var txtPrecio = formAgregar.Controls.Find("txtPrecio", true).FirstOrDefault() as TextBox;
                            var txtCosto = formAgregar.Controls.Find("txtCosto", true).FirstOrDefault() as TextBox;

                            if (txtPrecio != null)
                            {
                                txtPrecio.Text = precioPersonalizado.Value.ToString("F2");
                            }

                            if (txtCosto != null)
                            {
                                decimal costoEstimado = precioPersonalizado.Value * 0.9m;
                                txtCosto.Text = costoEstimado.ToString("F2");
                            }
                        }

                        var txtDescripcion = formAgregar.Controls.Find("txtDescripcion", true).FirstOrDefault() as TextBox;
                        txtDescripcion?.Focus();
                    };

                    var resultado = formAgregar.ShowDialog(this);

                    if (resultado == DialogResult.OK && !string.IsNullOrEmpty(formAgregar.CodigoAgregado))
                    {
                        MessageBox.Show(
                            $"Producto '{codigo}' agregado correctamente.\n" +
                            "Ahora puede continuar con la venta.",
                            "Producto agregado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        await MostrarProductoAsync(codigo, precioPersonalizado, false);
                        btnAgregar.Focus();
                    }
                    else
                    {
                        txtBuscarProducto.Text = "";
                        txtBuscarProducto.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al abrir el formulario de agregar producto: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                txtBuscarProducto.Focus();
            }
        }

        private void ConfigurarEventosDataGridView()
        {
            // Agregar evento para eliminar productos con tecla Delete
            dataGridView1.KeyDown += DataGridView1_KeyDown;

            // Agregar menú contextual para eliminar
            var contextMenu = new ContextMenuStrip();
            var eliminarItem = new ToolStripMenuItem("Eliminar Producto", null, EliminarProductoSeleccionado);
            eliminarItem.ShortcutKeys = Keys.Delete;
            contextMenu.Items.Add(eliminarItem);
            dataGridView1.ContextMenuStrip = contextMenu;
        }

        private void DataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && dataGridView1.SelectedRows.Count > 0)
            {
                EliminarProductoSeleccionado(sender, e);
            }
        }

        // AGREGAR: Método para verificar si hay una fila seleccionada
        private bool TieneFilaSeleccionada()
        {
            return dataGridView1.SelectedRows.Count > 0 && dataGridView1.SelectedRows[0].Index >= 0;
        }

        // MODIFICADO: El método EliminarProductoSeleccionado para manejar eliminación por id específico
        private async void EliminarProductoSeleccionado(object sender, EventArgs e)
        {
            try
            {
                // Verificación de permisos (código existente)...
                if (AuthenticationService.SesionActual?.Usuario != null)
                {
                    var usuario = AuthenticationService.SesionActual.Usuario;
                    
                    System.Diagnostics.Debug.WriteLine($"=== VERIFICACIÓN PERMISOS ELIMINAR ===");
                    System.Diagnostics.Debug.WriteLine($"Usuario: {usuario.NombreUsuario}");
                    System.Diagnostics.Debug.WriteLine($"Nivel: {usuario.Nivel}");
                    System.Diagnostics.Debug.WriteLine($"PuedeEliminarProductos: {usuario.PuedeEliminarProductos}");
                    System.Diagnostics.Debug.WriteLine($"Es Admin: {usuario.Nivel == Models.NivelUsuario.Administrador}");
                    System.Diagnostics.Debug.WriteLine($"Login habilitado: {AuthenticationService.ConfiguracionLogin?.LoginHabilitado}");
                    System.Diagnostics.Debug.WriteLine($"=====================================");

                    if (!usuario.PuedeEliminarProductos && usuario.Nivel != Models.NivelUsuario.Administrador)
                    {
                        MessageBox.Show($"No tiene permisos para eliminar productos.\n\n" +
                                      $"Usuario: {usuario.NombreUsuario}\n" +
                                      $"Nivel: {usuario.Nivel}\n" +
                                      $"Permiso eliminar: {usuario.PuedeEliminarProductos}",
                                      "Acceso denegado",
                                      MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("No hay sesión de usuario activa.", "Acceso denegado",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verificando permisos: {ex.Message}", 
                               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

    if (!TieneFilaSeleccionada())
    {
        MessageBox.Show("Seleccione un producto para eliminar haciendo clic en la fila.", "Información",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
    }

    var filaSeleccionada = dataGridView1.SelectedRows[0];

    // NUEVO: Verificar que la fila tenga todos los campos necesarios incluido id
    if (filaSeleccionada.Cells["codigo"].Value == null || filaSeleccionada.Cells["id"].Value == null)
    {
        MessageBox.Show("La fila seleccionada no contiene datos válidos.", "Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
    }

    // NUEVO: Obtener el id específico de la fila seleccionada
    int id = Convert.ToInt32(filaSeleccionada.Cells["id"].Value);
    string codigo = filaSeleccionada.Cells["codigo"].Value?.ToString();
    string descripcion = filaSeleccionada.Cells["descripcion"].Value?.ToString();
    decimal precio = Convert.ToDecimal(filaSeleccionada.Cells["precio"].Value);
    int cantidadTotal = Convert.ToInt32(filaSeleccionada.Cells["cantidad"].Value);
    decimal total = Convert.ToDecimal(filaSeleccionada.Cells["total"].Value);

    // MODIFICADO: Pasar información del producto al formulario de motivo (cantidadTotal siempre será de esta fila específica)
    using (var motivoForm = new MotivoEliminacionForm(descripcion, cantidadTotal, codigo, precio))
    {
        var resultado = motivoForm.ShowDialog();
        if (resultado != DialogResult.OK)
        {
            txtBuscarProducto.Focus();
            return;
        }

        try
        {
            // NUEVO: Usar id específico para eliminación precisa
            int cantidadAEliminar = motivoForm.CantidadAEliminar;
            decimal totalAEliminar = cantidadAEliminar * precio;
            
            // MODIFICADO: Pasar cantidadAEliminar y totalAEliminar a la función de eliminación
            await EliminarProductoPorIdConAuditoria(id, codigo, descripcion, precio, cantidadTotal, cantidadAEliminar, totalAEliminar, motivoForm.Motivo);
            CargarVentasActuales();
            
            string mensaje = cantidadAEliminar == cantidadTotal 
                ? "Producto eliminado completamente." 
                : $"Se eliminaron {cantidadAEliminar} de {cantidadTotal} unidades.";
                
            MessageBox.Show(mensaje, "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.BeginInvoke(new Action(() =>
            {
                txtBuscarProducto.Focus();
                txtBuscarProducto.SelectAll();
            }));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al eliminar producto: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            this.BeginInvoke(new Action(() =>
            {
                txtBuscarProducto.Focus();
            }));
        }
    }
}

// NUEVO: Método para eliminar por id específico con auditoría
private async Task EliminarProductoPorIdConAuditoria(int id, string codigo, string descripcion, 
    decimal precio, int cantidadTotal, int cantidadAEliminar, decimal totalAEliminar, string motivo)
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

                // 2. Registrar en auditoría (con la cantidad específica eliminada)
                await RegistrarEliminacionEnAuditoria(connection, transaction,
                    codigo, descripcion, precio, cantidadAEliminar, totalAEliminar, motivo);

                // 3. NUEVO: Eliminar o actualizar la fila específica por id
                if (cantidadAEliminar == cantidadTotal)
                {
                    // Eliminar completamente el registro específico por id
                    await EliminarFilaPorId(connection, transaction, id);
                }
                else
                {
                    // Actualizar la cantidad y recalcular el total en la fila específica
                    await ActualizarCantidadPorId(connection, transaction, id, cantidadTotal - cantidadAEliminar, precio);
                }

                transaction.Commit();

                // DEBUG: Confirmar la operación específica por id
                System.Diagnostics.Debug.WriteLine($"=== ELIMINACIÓN POR ID ===");
                System.Diagnostics.Debug.WriteLine($"ID eliminado/modificado: {id}");
                System.Diagnostics.Debug.WriteLine($"Producto: {codigo} - {descripcion}");
                System.Diagnostics.Debug.WriteLine($"Cantidad original: {cantidadTotal}");
                System.Diagnostics.Debug.WriteLine($"Cantidad eliminada: {cantidadAEliminar}");
                System.Diagnostics.Debug.WriteLine($"Operación: {(cantidadAEliminar == cantidadTotal ? "ELIMINACIÓN COMPLETA" : "ACTUALIZACIÓN PARCIAL")}");
                System.Diagnostics.Debug.WriteLine($"===========================");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}

// NUEVO: Método para eliminar fila específica por id
private async Task EliminarFilaPorId(SqlConnection connection, SqlTransaction transaction, int id)
{
    var query = @"DELETE FROM Ventas WHERE id = @id";

    using (var cmd = new SqlCommand(query, connection, transaction))
    {
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}

// NUEVO: Método para actualizar cantidad en fila específica por id con IVA
private async Task ActualizarCantidadPorId(SqlConnection connection, SqlTransaction transaction, 
    int id, int nuevaCantidad, decimal precio)
{
    // NUEVO: Obtener porcentaje de IVA del producto para recalcular
    decimal porcentajeIva = 0;
    var queryIva = @"SELECT p.iva FROM Ventas v INNER JOIN Productos p ON v.codigo = p.codigo WHERE v.id = @id";
    using (var cmdIva = new SqlCommand(queryIva, connection, transaction))
    {
        cmdIva.Parameters.AddWithValue("@id", id);
        var resultIva = await cmdIva.ExecuteScalarAsync();
        if (resultIva != null && resultIva != DBNull.Value)
        {
            porcentajeIva = Convert.ToDecimal(resultIva);
        }
    }

    // MODIFICADO: Incluir recálculo de IVA
    var query = @"UPDATE Ventas 
                  SET cantidad = @nuevaCantidad, 
                      total = @nuevaCantidad * @precio,
                      IvaCalculado = (@precio * @nuevaCantidad) * @porcentajeIva / (100 + @porcentajeIva)
                  WHERE id = @id";

    using (var cmd = new SqlCommand(query, connection, transaction))
    {
        cmd.Parameters.AddWithValue("@nuevaCantidad", nuevaCantidad);
        cmd.Parameters.AddWithValue("@precio", precio);
        cmd.Parameters.AddWithValue("@porcentajeIva", porcentajeIva);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }
        }
        // Método ProcesarCodigo
        private (string codigoBuscado, decimal? precioPersonalizado, bool esEspecial) ProcesarCodigo(string textoIngresado)
        {
            string codigoBuscado = textoIngresado.TrimStart('0');
            if (string.IsNullOrEmpty(codigoBuscado))
                codigoBuscado = "0";

            decimal? precioPersonalizado = null;
            bool esEspecial = false;

            // Verificar si es un código especial (formato balanza: 50XXXXX...)
            if (textoIngresado.StartsWith(PREFIJO_CODIGO_ESPECIAL) && textoIngresado.Length == LONGITUD_CODIGO_ESPECIAL)
            {
                try
                {
                    // Extraer código del producto (5 dígitos desde posición 2)
                    string codigoProducto = textoIngresado.Substring(POSICION_CODIGO_PRODUCTO, LONGITUD_CODIGO_PRODUCTO);
                    codigoProducto = codigoProducto.TrimStart('0');
                    if (string.IsNullOrEmpty(codigoProducto))
                        codigoProducto = "0";

                    // Extraer precio (5 dígitos desde posición 7)
                    string partePrecio = textoIngresado.Substring(POSICION_PRECIO, LONGITUD_PRECIO);
                    if (int.TryParse(partePrecio, out int precioEntero))
                    {
                        precioPersonalizado = precioEntero;
                    }

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

        // Método ObtenerIPLocal
        private string ObtenerIPLocal()
        {
            try
            {
                return System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName())
                    .AddressList.FirstOrDefault(ip => ip.AddressFamily ==
                    System.Net.Sockets.AddressFamily.InterNetwork)?.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        // VERSIÓN DIRECTA (igual que funciona para facturas)
        private string ObtenerUsuarioActual()
        {
            try
            {
                // DIRECTO: Solo verificar si hay sesión activa
                if (AuthenticationService.SesionActual?.Usuario?.NombreUsuario != null)
                {
                    string usuario = AuthenticationService.SesionActual.Usuario.NombreUsuario;
                    System.Diagnostics.Debug.WriteLine($"🟢 Usuario logueado encontrado: {usuario}");
                    return usuario;
                }
                
                System.Diagnostics.Debug.WriteLine($"🔴 No hay sesión activa, usando fallback: {Environment.UserName}");
                return Environment.UserName; // Fallback
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🔴 Error obteniendo usuario: {ex.Message}");
                return Environment.UserName;
            }
        }

        // VERSIÓN DIRECTA (igual que funciona para facturas)
        private int obtenerNumeroCajero()
        {
            try
            {
                // DIRECTO: Solo verificar si hay sesión activa
                if (AuthenticationService.SesionActual?.Usuario != null)
                {
                    int cajero = AuthenticationService.SesionActual.Usuario.NumeroCajero;
                    System.Diagnostics.Debug.WriteLine($"🟢 Número de cajero encontrado: {cajero}");
                    return cajero;
                }
                
                System.Diagnostics.Debug.WriteLine($"🔴 No hay sesión activa, usando cajero por defecto: 1");
                return 1; // Cajero por defecto
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🔴 Error obteniendo cajero: {ex.Message}");
                return 1;
            }
        }

        // NUEVO: Clase para MessageBox personalizado sin botón por defecto
        public partial class CustomMessageBox : Form
        {
            public DialogResult Result { get; private set; } = DialogResult.None;

            public CustomMessageBox(string message, string caption)
            {
                InitializeComponent();
                ConfigurarMessageBox(message, caption);
            }

            private void InitializeComponent()
            {
                this.SuspendLayout();

                // Configuración del formulario
                this.Text = "";
                this.Size = new Size(400, 180);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.ShowInTaskbar = false;
                this.KeyPreview = true;

                // IMPORTANT: No establecer ningún AcceptButton o CancelButton
                // this.AcceptButton = null;
                // this.CancelButton = null;

                this.ResumeLayout(false);
            }

            private void ConfigurarMessageBox(string message, string caption)
            {
                this.Text = caption;

                // Icono de pregunta
                var pictureBox = new PictureBox
                {
                    Image = SystemIcons.Question.ToBitmap(),
                    Location = new Point(15, 15),
                    Size = new Size(32, 32),
                    SizeMode = PictureBoxSizeMode.StretchImage
                };
                this.Controls.Add(pictureBox);

                // Mensaje
                var lblMessage = new Label
                {
                    Text = message,
                    Location = new Point(60, 15),
                    Size = new Size(310, 80),
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.FromArgb(62, 80, 100)
                };
                this.Controls.Add(lblMessage);

                // Botón Sí
                var btnYes = new Button
                {
                    Text = "Sí",
                    Location = new Point(215, 100),
                    Size = new Size(75, 30),
                    Font = new Font("Segoe UI", 9F),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(76, 175, 80),
                    ForeColor = Color.White,
                    TabStop = false // IMPORTANTE: No recibir foco por Tab
                };
                btnYes.FlatAppearance.BorderSize = 0;
                // CORRECCIÓN: Establecer DialogResult y cerrar
                btnYes.Click += (s, e) =>
                {
                    Result = DialogResult.Yes;
                    this.DialogResult = DialogResult.Yes; // NUEVO: Establecer DialogResult del formulario
                    this.Close();
                };
                btnYes.MouseEnter += (s, e) => btnYes.BackColor = Color.FromArgb(66, 165, 70);
                btnYes.MouseLeave += (s, e) => btnYes.BackColor = Color.FromArgb(76, 175, 80);
                this.Controls.Add(btnYes);

                // Botón No
                var btnNo = new Button
                {
                    Text = "No",
                    Location = new Point(300, 100),
                    Size = new Size(75, 30),
                    Font = new Font("Segoe UI", 9F),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(220, 53, 69),
                    ForeColor = Color.White,
                    TabStop = false // IMPORTANTE: No recibir foco por Tab
                };
                btnNo.FlatAppearance.BorderSize = 0;
                // CORRECCIÓN: Establecer DialogResult y cerrar
                btnNo.Click += (s, e) =>
                {
                    Result = DialogResult.No;
                    this.DialogResult = DialogResult.No; // NUEVO: Establecer DialogResult del formulario
                    this.Close();
                };
                btnNo.MouseEnter += (s, e) => btnNo.BackColor = Color.FromArgb(210, 43, 59);
                btnNo.MouseLeave += (s, e) => btnNo.BackColor = Color.FromArgb(220, 53, 69);
                this.Controls.Add(btnNo);

                // CLAVE: Establecer el foco en el formulario principal, no en los botones
                this.ActiveControl = null;

                // Manejar teclas
                this.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                    {
                        // No hacer nada - forzar uso del mouse
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        Result = DialogResult.No;
                        this.DialogResult = DialogResult.No; // NUEVO: Establecer DialogResult del formulario
                        this.Close();
                    }
                };

                // Asegurar que ningún botón tenga foco inicial
                this.Shown += (s, e) =>
                {
                    this.ActiveControl = null;
                    this.Focus();
                };

                // NUEVO: Manejar el cierre del formulario sin selección
                this.FormClosing += (s, e) =>
                {
                    // Si se cierra sin haber seleccionado nada, actuar como "No"
                    if (this.DialogResult == DialogResult.None)
                    {
                        this.DialogResult = DialogResult.No;
                        Result = DialogResult.No;
                    }
                };
            }
        }

        // NUEVO: Método para calcular IVA desde el total (precio con IVA incluido)
        private decimal CalcularIvaDesdeTotal(decimal totalConIva, decimal porcentajeIva)
        {
            if (porcentajeIva <= 0) return 0;
    
            // Fórmula: IVA = Total * (% IVA / (100 + % IVA))
            decimal iva = totalConIva * (porcentajeIva / (100 + porcentajeIva));
            return Math.Round(iva, 2);
        }

        // NUEVO: Método alternativo para calcular IVA desde el neto (precio sin IVA)
        private decimal CalcularIvaDesdeNeto(decimal totalNeto, decimal porcentajeIva)
        {
            if (porcentajeIva <= 0) return 0;
    
            // Fórmula: IVA = Total Neto * (% IVA / 100)
            decimal iva = totalNeto * (porcentajeIva / 100);
            return Math.Round(iva, 2);
        }
    }
}