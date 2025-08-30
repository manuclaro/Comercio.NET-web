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
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace Comercio.NET
{
    public partial class Ventas : Form
    {
        private int nroRemitoActual = 0;
        private bool remitoIncrementado = false;
        private DataTable remitoActual = null;
        private PrintDocument printDocumentRemito = new PrintDocument();
        private string nombreComercio = "Comercio";
        private string domicilioComercio = "domicilio";

        public Ventas()
        {
            InitializeComponent();
            txtBuscarProducto.TextChanged += txtBuscarProducto_TextChanged;
            btnAgregar.Click += btnAgregar_Click;
            this.Load += Ventas_Load;
            btnFinalizarVenta.Click += btnFinalizarVenta_Click;
            cbnombreCtaCte.SelectedIndexChanged += cbnombreCtaCte_SelectedIndexChanged;

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

            // Leer desde appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            nombreComercio = config["Comercio:Nombre"] ?? "Comercio";
            domicilioComercio = config["Comercio:Domicilio"] ?? "domicilio";

            this.Font = new Font("Segoe UI", 10F);
            this.BackColor = Color.WhiteSmoke;
            btnAgregar.FlatStyle = FlatStyle.Flat;
            btnAgregar.BackColor = Color.FromArgb(0, 120, 215); // Azul moderno
            btnAgregar.ForeColor = Color.White;
            btnAgregar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            //btnAgregar.Image = Properties.Resources.add_32; // Si tienes un recurso de imagen
            btnAgregar.ImageAlign = ContentAlignment.MiddleLeft;
            btnAgregar.TextAlign = ContentAlignment.MiddleRight;

            btnFinalizarVenta.FlatStyle = FlatStyle.Flat;
            btnFinalizarVenta.BackColor = Color.FromArgb(0, 150, 136); // Verde azulado moderno
            btnFinalizarVenta.ForeColor = Color.White;
            btnFinalizarVenta.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnAgregar.ImageAlign = ContentAlignment.MiddleLeft;
            btnAgregar.TextAlign = ContentAlignment.MiddleRight;

            btnSalir.FlatStyle = FlatStyle.Flat;
            btnSalir.BackColor = Color.FromArgb(220, 53, 69); // Rojo para acciones destructivas o de advertencia
            btnSalir.ForeColor = Color.White;
            btnSalir.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSalir.Padding = new Padding(0, 0, 20, 0); // 20px de margen derecho

            txtBuscarProducto.BorderStyle = BorderStyle.FixedSingle;
            cbnombreCtaCte.DropDownStyle = ComboBoxStyle.DropDownList;
            cbnombreCtaCte.FlatStyle = FlatStyle.Flat;

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

            // Panel inferior para el total y cantidad de productos
            panelFooter = new Panel();
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Height = 36;
            panelFooter.BackColor = Color.FromArgb(0, 120, 215);

            // Label de cantidad de productos (izquierda)
            lbCantidadProductos.AutoSize = false;
            lbCantidadProductos.TextAlign = ContentAlignment.MiddleLeft;
            lbCantidadProductos.Dock = DockStyle.Left;
            lbCantidadProductos.Width = 200;
            lbCantidadProductos.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lbCantidadProductos.ForeColor = Color.White;
            lbCantidadProductos.Text = "Productos: 0";

            // Label de total (derecha)
            lbTotal.AutoSize = false;
            lbTotal.TextAlign = ContentAlignment.MiddleRight;
            lbTotal.Dock = DockStyle.Right;
            lbTotal.Width = 600; // Aumenta el ancho, por ejemplo 300
            lbTotal.Font = new Font("Segoe UI", 32F, FontStyle.Bold);
            lbTotal.ForeColor = Color.White;
            lbTotal.Text = "Total: $0,00";
            lbTotal.Padding = new Padding(0, 0, 20, 0); // 20px de margen derecho
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
                    Text = result != null ? result.ToString() : " Producto no encontrado";
                }
            }
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            string codigoBuscado = txtBuscarProducto.Text.Trim();
            if (string.IsNullOrEmpty(codigoBuscado))
            {
                MessageBox.Show("Ingrese un código de producto válido.");
                txtBuscarProducto.Focus();
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
                        txtBuscarProducto.Focus();
                        return;
                    }
                    producto = dt.Rows[0];
                }
            }

            // 3. Verificar si el producto ya está en la venta actual
            bool productoYaAgregado = false;
            int cantidadActual = 0;
            decimal precioUnitario = Convert.ToDecimal(producto["precio"]);
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

            if (productoYaAgregado)
            {
                // 4a. Si ya existe, hacer UPDATE sumando cantidad y recalculando total
                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"UPDATE Ventas 
                          SET cantidad = cantidad + 1, 
                              total = (cantidad + 1) * @precio
                          WHERE nrofactura = @nrofactura AND codigo = @codigo";
                    using (var cmd = new SqlCommand(query, connection))
                    {
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
                // 4b. Si no existe, hacer INSERT
                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"INSERT INTO Ventas 
                (codigo, descripcion, precio, rubro, marca, proveedor, costo, fecha, hora, cantidad, total, nrofactura, EsCtaCte)
                VALUES (@codigo, @descripcion, @precio, @rubro, @marca, @proveedor, @costo, @fecha, @hora, @cantidad, @total, @nrofactura, @EsCtaCte)";
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
                        cmd.Parameters.AddWithValue("@EsCtaCte", chkEsCtaCte.Checked);

                        connection.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // 5. Mostrar todas las ventas del remito actual
            CargarVentasActuales();

            // Formatear columnas y encabezados (igual que antes)
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

            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.ClearSelection();
            dataGridView1.CurrentCell = null;
            dataGridView1.Enabled = true; // Permite scroll y visualización, pero no selección

            // Evento para evitar cualquier selección por el usuario
            dataGridView1.SelectionChanged += (s, e) =>
            {
                dataGridView1.ClearSelection();
            };

            txtBuscarProducto.Focus();

            lbCantidadProductos.Text = "Productos: 0";
            lbTotal.Text = "Total: $0,00";

            // Inicialización de dataGridView1 si no usas el diseñador
            //if (dataGridView1 == null)
            //{
            //    dataGridView1 = new DataGridView();
            //    this.Controls.Add(dataGridView1);
            //}

            // Configuración general
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; // <-- Esto hace que las columnas ocupen todo el ancho

            // Personalizar estilo de DataGridView
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250); // Blanco suave
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black; // Blanco para el texto
            dataGridView1.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 254);
            dataGridView1.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Black;
            //dataGridView1.DefaultCellStyle.BackColor = Color.White;
            dataGridView1.DefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 254);
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            //Color suave para filas alternas
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(220, 235, 255); // AliceBlue
            dataGridView1.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;
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

            lbCantidadProductos.Text = $"Productos: {dataGridView1.Rows.Count}";

            decimal sumaTotal = 0;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["total"].Value != null && decimal.TryParse(row.Cells["total"].Value.ToString(), out decimal valor))
                    sumaTotal += valor;
            }
            lbTotal.Text = $"Total: {sumaTotal:C2}";
        }

        // Cambia la firma del método btnFinalizarVenta_Click para que sea async Task en vez de void
        private async void btnFinalizarVenta_Click(object sender, EventArgs e)
        {
            remitoIncrementado = false;

            if (remitoActual != null && remitoActual.Rows.Count > 0)
            {
                ImprimirTicket();
                //ImprimirA4();
            }

            Ventas_Load(null, null);
            dataGridView1.DataSource = null;
            dataGridView1.Rows.Clear();
            lbCantidadProductos.Text = "Productos: 0";
            lbTotal.Text = "Total: $0,00";
            chkEsCtaCte.Checked = false;
            MessageBox.Show("Venta finalizada. Se generó un nuevo remito.");

            // Integración con ARCA
            string cuit = "20280694739";
            string service = "wsfe";
            string pfxPath = @"C:\Certificados\certificado.pfx";
            string pfxPassword = "Micertificado";
            string wsaaUrl = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";

            // Usa la versión asíncrona correctamente
            var (token, sign) = await AfipAuthenticator.GetTAAsync(service, pfxPath, pfxPassword, wsaaUrl);

            // Llamar al método (usa await si es async)
            var client = new ArcaWS.ServiceSoapClient(ArcaWS.ServiceSoapClient.EndpointConfiguration.ServiceSoap);

            // Autenticación (usa los valores reales de token y sign obtenidos con WSAA)
            var auth = new ArcaWS.FEAuthRequest
            {
                Token = token, // variable string con el token obtenido
                Sign = sign,   // variable string con el sign obtenido
                Cuit = Convert.ToInt64(cuit) // tu CUIT como long
            };
        
            // Parámetros para la consulta
            int ptoVta = 1;      // Punto de venta
            int cbteTipo = 6;    // Tipo de comprobante (por ejemplo, 6 = Factura B)

            // Consulta a AFIP el último número autorizado
            var ultimoResp = await client.FECompUltimoAutorizadoAsync(auth, ptoVta, cbteTipo);
            int ultimoNroAfip = ultimoResp.Body.FECompUltimoAutorizadoResult.CbteNro;
            int nuevoNroComprobante = ultimoNroAfip + 1;

            // Encabezado
            var feCabReq = new ArcaWS.FECAECabRequest
            {
                CantReg = 1,
                PtoVta = ptoVta,
                CbteTipo = cbteTipo
            };

            // Crear la alícuota de IVA 21%
            var iva21 = new ArcaWS.AlicIva
            {
                Id = 5, // Código para IVA 21%
                BaseImp = 100.00, // Neto gravado
                Importe = 21.00   // IVA calculado
            };

            // Detalle único con los totales de la venta
            var feDetReq = new ArcaWS.FECAEDetRequest
            {
                Concepto = 1,
                DocTipo = 99, // Consumidor final
                DocNro = 0,
                CbteDesde = nuevoNroComprobante,
                CbteHasta = nuevoNroComprobante,
                CbteFch = DateTime.Now.ToString("yyyyMMdd"),
                ImpTotal = 121,    // Total de la venta
                ImpNeto = 100,     // Neto gravado
                ImpIVA = 21,       // IVA total
                MonId = "PES",
                MonCotiz = 1,
                CondicionIVAReceptorId = 5,
                ImpTrib = 0,
                ImpOpEx = 0,
                Iva = new ArcaWS.AlicIva[] { iva21 } // <-- Aquí asignas el array de alícuotas
                // ...otros campos requeridos
            };

            // Armar el request principal
            var feCAEReq = new ArcaWS.FECAERequest
            {
                FeCabReq = feCabReq,
                FeDetReq = new ArcaWS.FECAEDetRequest[] { feDetReq }
            };

            
            var respuesta = await client.FECAESolicitarAsync(auth, feCAEReq);

            // Acceso típico a la estructura de la respuesta
            var resultado = respuesta?.Body?.FECAESolicitarResult;

            if (resultado != null && resultado.FeDetResp != null && resultado.FeDetResp.Length > 0)
            {
                var detalle = resultado.FeDetResp[0];
                if (detalle.Resultado == "A")
                {
                    string cae = detalle.CAE;
                    string nroComprobante = detalle.CbteDesde.ToString();
                    MessageBox.Show($"Factura Aprobada.\nCAE: {cae}\nComprobante: {nroComprobante}");
                }
                else if (detalle.Resultado == "R")
                {
                    // Factura rechazada, mostrar motivos
                    string mensaje = "La factura fue RECHAZADA.\n";
                    if (detalle.Observaciones != null && detalle.Observaciones.Length > 0)
                    {
                        foreach (var obs in detalle.Observaciones)
                        {
                            mensaje += $"Código: {obs.Code} - {obs.Msg}\n";
                        }
                    }
                    else
                    {
                        mensaje += "No se recibieron observaciones de AFIP.";
                    }
                    MessageBox.Show(mensaje, "Rechazo AFIP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show($"Resultado desconocido: {detalle.Resultado}");
                }
            }
            else if (resultado?.Errors != null && resultado.Errors.Length > 0)
            {
                var error = resultado.Errors[0];
                MessageBox.Show($"Error {error.Code}: {error.Msg}");
            }
            else
            {
                MessageBox.Show("No se obtuvo respuesta válida de ARCA.");
            }
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

        private void ImprimirA4()
        {
            printDocumentRemito.PrintPage -= printDocumentRemito_PrintPage;
            printDocumentRemito.PrintPage += printDocumentRemito_PrintPageA4;

            using (PrintPreviewDialog previewDialog = new PrintPreviewDialog())
            {
                previewDialog.Document = printDocumentRemito;
                previewDialog.WindowState = FormWindowState.Maximized;
                previewDialog.ShowDialog();
            }

            printDocumentRemito.PrintPage -= printDocumentRemito_PrintPageA4;
        }

        private void ImprimirTicket()
        {
            printDocumentRemito.PrintPage -= printDocumentRemito_PrintPage;
            printDocumentRemito.PrintPage += printDocumentRemito_PrintPageTicket;

            // 8 cm = 80 mm = 315 centésimas de pulgada (1 pulgada = 25,4 mm)
            int anchoTicket = (int)(75 / 25.4 * 100); // 80mm a centésimas de pulgada
            int altoTicket = (int)(200 / 25.4 * 100); // 200mm de alto (ajustable)

            PaperSize ticketSize = new PaperSize("Ticket", anchoTicket, altoTicket);
            printDocumentRemito.DefaultPageSettings.PaperSize = ticketSize;

            // Márgenes mínimos (2 mm)
            printDocumentRemito.DefaultPageSettings.Margins = new Margins(8, 8, 8, 8); // 8 centésimas de pulgada ≈ 2 mm

            using (PrintPreviewDialog previewDialog = new PrintPreviewDialog())
            {
                previewDialog.Document = printDocumentRemito;
                previewDialog.WindowState = FormWindowState.Maximized;
                previewDialog.ShowDialog();
            }

            printDocumentRemito.PrintPage -= printDocumentRemito_PrintPageTicket;
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
            //string nombreComercio = "Comercio";
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

            // Márgenes y fuentes
            float leftMargin = e.MarginBounds.Left; // Margen izquierdo 0
            float topMargin = e.MarginBounds.Top + 1; // Margen superior más chico
            float rowHeight = 18;
            Font font = new Font("Arial", 9);
            Font fontBold = new Font("Arial", 9, FontStyle.Bold);
            Font fontTitulo = new Font("Arial", 16, FontStyle.Bold);
            Pen linePen = new Pen(Color.Black, 1);

            // Definir anchos de columnas (ajustados)
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

            float tablaRight = colX[3] + colTotal; // Límite derecho de la tabla

            float y = topMargin;

            // Fecha y hora alineadas con el borde derecho de la columna "TOTAL"
            string fechaStr = $"Fecha: {DateTime.Now:dd/MM/yyyy}";
            string horaStr = $"Hora: {DateTime.Now:HH:mm}";
            SizeF fechaSize = e.Graphics.MeasureString(fechaStr, font);
            SizeF horaSize = e.Graphics.MeasureString(horaStr, font);
            float fechaX = tablaRight - fechaSize.Width;
            float horaX = tablaRight - horaSize.Width;
            e.Graphics.DrawString(fechaStr, font, Brushes.Black, fechaX, y);
            e.Graphics.DrawString(horaStr, font, Brushes.Black, horaX, y + fechaSize.Height);

            // Subir el título (menos espacio después de la fecha/hora)
            y += Math.Max(fechaSize.Height + horaSize.Height, 10) + 6;

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
            y += tituloSize.Height + 8;

            // Encabezados de columnas
            string[] headers = { "C", "DESCRIPCIÓN", "PRECIO", "TOTAL" };
            float[] colWidths = { colCantidad, colDescripcion, colPrecio, colTotal };
            for (int i = 0; i < headers.Length; i++)
            {
                float headerX = colX[i];
                float colWidth = colWidths[i];
                SizeF headerSize = e.Graphics.MeasureString(headers[i], fontBold);

                if (i == 0)
                {
                    // Centrar encabezado de cantidad
                    headerX += (colWidth - headerSize.Width) / 2;
                }
                else if (i == 1)
                {
                    // "DESCRIPCIÓN" alineado a la izquierda (sin ajuste)
                }
                else
                {
                    // Alinear encabezados de PRECIO y TOTAL a la derecha
                    headerX += colWidth - headerSize.Width;
                }
                e.Graphics.DrawString(headers[i], fontBold, Brushes.Black, headerX, y);
            }

            y += rowHeight - 2; // Más espacio debajo de los encabezados
            // Línea debajo del encabezado (hasta el borde de la columna TOTAL)
            e.Graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 4;

            // Detalle productos
            int cantidadTotal = 0;
            foreach (DataRow row in remitoActual.Rows)
            {
                float filaY = y;

                // Descripción (corte y salto de línea si es necesario)
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

                // Cantidad (centrado vertical y horizontal en cada línea)
                string cantidadStr = row["cantidad"].ToString();
                SizeF cantidadSize = e.Graphics.MeasureString(cantidadStr, font);
                float cantidadX = colX[0] + (colCantidad - cantidadSize.Width) / 2;
                float cantidadY = filaY + ((rowHeight - cantidadSize.Height) / 2);

                // Sumar cantidad total
                if (int.TryParse(cantidadStr, out int cantVal))
                    cantidadTotal += cantVal;

                // Precio y Total (solo en la primera línea de la descripción)
                string precioStr = Convert.ToDecimal(row["precio"]).ToString("C2");
                SizeF precioSize = e.Graphics.MeasureString(precioStr, font);
                float precioX = colX[2] + colPrecio - precioSize.Width;

                string totalStr = Convert.ToDecimal(row["total"]).ToString("C2");
                SizeF totalSize = e.Graphics.MeasureString(totalStr, font);
                float totalX = colX[3] + colTotal - totalSize.Width;

                // Imprimir líneas de descripción y datos
                for (int i = 0; i < lineasDescripcion.Count; i++)
                {
                    // Centrar cantidad en cada línea (vertical y horizontal)
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

            // Línea encima del total (hasta el borde de la columna TOTAL)
            y += 6;
            e.Graphics.DrawLine(linePen, leftMargin, y, tablaRight, y);
            y += 6;

            // Total general, alineado a la derecha, in línea con la columna TOTAL
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

            // Total cantidad de productos, alineado a la izquierda, a la misma altura que el total general
            string cantidadTotalStr = $"PRODUCTOS: {cantidadTotal}";
            e.Graphics.DrawString(cantidadTotalStr, fontBold, Brushes.Black, leftMargin, y);

            // Firma (opcional)
            //y += rowHeight * 2;
            //e.Graphics.DrawString("Firma: ___________________________", font, Brushes.Black, leftMargin, y);
        }

        private void chkEsCtaCte_CheckedChanged(object sender, EventArgs e)
        {
            if (chkEsCtaCte.Checked)
            {
                // Cargar nombres de CtaCte desde archivo de texto
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

            string soapRequest = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
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
content.Headers.Add("SOAPAction", "\"\""); // Add this line
var response = await client.PostAsync(wsaaUrl, content);
var responseBody = await response.Content.ReadAsStringAsync();

if (!response.IsSuccessStatusCode)
{
    // Aquí puedes loguear o mostrar el contenido para diagnóstico
    throw new Exception($"Error WSAA: {response.StatusCode}\n{responseBody}");
}

       return responseBody;
   }

        public static (string Token, string Sign) ExtraerTokenSign(string soapResponse)
        {
            var xml = new XmlDocument();
            xml.LoadXml(soapResponse);

            var nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsmgr.AddNamespace("ns1", "http://wsaa.view.sua.dvadac.desein.afip.gov.ar/");

            // Verificar si hay un error SOAP
            var fault = xml.SelectSingleNode("//soap:Fault", nsmgr);
            if (fault != null)
                throw new Exception("Respuesta de error de WSAA: " + fault.InnerXml);

            // Buscar el nodo con namespace
            var loginCmsReturn = xml.SelectSingleNode("//ns1:loginCmsReturn", nsmgr);
            if (loginCmsReturn == null || string.IsNullOrEmpty(loginCmsReturn.InnerText))
                throw new Exception("La respuesta del servicio WSAA fue nula o vacía, pero no arrojó un FaultException.");

            var taXml = new XmlDocument();
            taXml.LoadXml(loginCmsReturn.InnerText);

            var token = taXml.SelectSingleNode("//token")?.InnerText;
            var sign = taXml.SelectSingleNode("//sign")?.InnerText;

            return (token, sign);
        }
    }

    public class AfipAuthenticator
    {
        private static string _token;
        private static string _sign;
        private static DateTime _expiration;

        private static readonly string TaPath = "ta.xml";

        public static (string token, string sign) GetTA(string service, string pfxPath, string pfxPassword, string wsaaUrl)
        {
            // Intenta leer el TA guardado
            if (File.Exists(TaPath))
            {
                var taXml = new XmlDocument();
                taXml.Load(TaPath);
                _token = taXml.SelectSingleNode("//token")?.InnerText;
                _sign = taXml.SelectSingleNode("//sign")?.InnerText;
                var expirationStr = taXml.SelectSingleNode("//expirationTime")?.InnerText;
                _expiration = DateTime.ParseExact(expirationStr, new[] { "yyyy-MM-ddTHH:mm:ss.fffK", "yyyy-MM-ddTHH:mm:ssK" }, CultureInfo.InvariantCulture, DateTimeStyles.None);

                // LOG para depuración
                Console.WriteLine($"TA encontrado. Expira: {_expiration}, Token: {_token}, Sign: {_sign}");

                if (!string.IsNullOrEmpty(_token) && !string.IsNullOrEmpty(_sign) && _expiration > DateTime.UtcNow.AddMinutes(1))
                {
                    return (_token, _sign);
                }
                else
                {
                    Console.WriteLine("TA inválido o vencido, se solicitará uno nuevo.");
                }
            }
            else
            {
                Console.WriteLine("No existe ta.xml, se solicitará un nuevo TA.");
            }

            string traXml = WSAAHelper.CrearTRA(service);
            byte[] cms = WSAAHelper.FirmarTRA(traXml, pfxPath, pfxPassword);
            string soapResponse = WSAAHelper.LlamarWSAA(cms, wsaaUrl).GetAwaiter().GetResult();

            var xml = new XmlDocument();
            xml.LoadXml(soapResponse);
            var nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsmgr.AddNamespace("ns1", "http://wsaa.view.sua.dvadac.desein.afip.gov.ar/");
            var loginCmsReturn = xml.SelectSingleNode("//ns1:loginCmsReturn", nsmgr);
            if (loginCmsReturn != null && !string.IsNullOrEmpty(loginCmsReturn.InnerText))
            {
                File.WriteAllText(TaPath, loginCmsReturn.InnerText);
                var taXml = new XmlDocument();
                taXml.LoadXml(loginCmsReturn.InnerText);
                _token = taXml.SelectSingleNode("//token")?.InnerText;
                _sign = taXml.SelectSingleNode("//sign")?.InnerText;
                var expirationStr = taXml.SelectSingleNode("//expirationTime")?.InnerText;
                _expiration = DateTime.ParseExact(expirationStr, new[] { "yyyy-MM-ddTHH:mm:ss.fffK", "yyyy-MM-ddTHH:mm:ssK" }, CultureInfo.InvariantCulture, DateTimeStyles.None);
            }

            return (_token, _sign);
        }

        // Cambia la firma:
        public static async Task<(string token, string sign)> GetTAAsync(string service, string pfxPath, string pfxPassword, string wsaaUrl)
        {
            if (File.Exists(TaPath))
            {
                try
                {
                    var taXml = new XmlDocument();
                    taXml.Load(TaPath);
                    _token = taXml.SelectSingleNode("//token")?.InnerText;
                    _sign = taXml.SelectSingleNode("//sign")?.InnerText;
                    var expirationStr = taXml.SelectSingleNode("//expirationTime")?.InnerText;

                    // Validar formato de fecha
                    if (!DateTime.TryParse(expirationStr, out _expiration))
                    {
                        Console.WriteLine($"[WSAA] Formato de fecha inválido en ta.xml: '{expirationStr}'");
                        File.Delete(TaPath); // Elimina el archivo corrupto
                    }
                    else
                    {
                        Console.WriteLine($"[WSAA] TA encontrado. Expira: {_expiration}, Token: {_token}, Sign: {_sign}");

                        if (!string.IsNullOrEmpty(_token) && !string.IsNullOrEmpty(_sign) && _expiration > DateTime.UtcNow.AddMinutes(1))
                        {
                            return (_token, _sign);
                        }
                        else
                        {
                            Console.WriteLine("[WSAA] TA inválido o vencido, se solicitará uno nuevo.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WSAA] Error leyendo ta.xml: {ex.Message}");
                    File.Delete(TaPath); // Elimina el archivo corrupto
                }
            }
            else
            {
                Console.WriteLine("[WSAA] No existe ta.xml, se solicitará un nuevo TA.");
            }

            string traXml = WSAAHelper.CrearTRA(service);
            byte[] cms = WSAAHelper.FirmarTRA(traXml, pfxPath, pfxPassword);
            string soapResponse = await WSAAHelper.LlamarWSAA(cms, wsaaUrl);

            var xml = new XmlDocument();
            xml.LoadXml(soapResponse);
            var nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsmgr.AddNamespace("ns1", "http://wsaa.view.sua.dvadac.desein.afip.gov.ar/");
            var loginCmsReturn = xml.SelectSingleNode("//ns1:loginCmsReturn", nsmgr);
            if (loginCmsReturn != null && !string.IsNullOrEmpty(loginCmsReturn.InnerText))
            {
                File.WriteAllText(TaPath, loginCmsReturn.InnerText);
                var taXml = new XmlDocument();
                taXml.LoadXml(loginCmsReturn.InnerText);
                _token = taXml.SelectSingleNode("//token")?.InnerText;
                _sign = taXml.SelectSingleNode("//sign")?.InnerText;
                var expirationStr = taXml.SelectSingleNode("//expirationTime")?.InnerText;
                _expiration = DateTime.ParseExact(expirationStr, new[] { "yyyy-MM-ddTHH:mm:ss.fffK", "yyyy-MM-ddTHH:mm:ssK" }, CultureInfo.InvariantCulture, DateTimeStyles.None);
            }

            return (_token, _sign);
        }
    }
}
