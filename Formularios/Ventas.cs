using ArcaWS;
using Comercio.NET.Controles;
using Comercio.NET.Formularios;
using Comercio.NET.Models;
using Comercio.NET.Services;
using Comercio.NET.Servicios;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
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
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Forms;
using System.Xml;
using static Comercio.NET.SeleccionImpresionForm;

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

        // NUEVO: botón para anular factura completa
        private Button btnAnularFactura;

        // ✅ NUEVO: Botón para retiros de efectivo
        private Button btnRetirarEfectivo;

        private Button btnPagoProveedor;

        // En lugar del Label lbTotal, usar un RichTextBox para mejor control de formato
        private RichTextBox rtbTotal;

        // NUEVO: Variable para controlar el estado de eliminación
        private bool procesandoEliminacion = false;

        // NUEVO: Variable para controlar el estado de edición de cantidad
        private bool procesandoEdicionCantidad = false;

        // NUEVO: Menú contextual para la grilla
        private ContextMenuStrip contextMenuGrilla;
        private ToolStripMenuItem menuEditarCantidad;
        private ToolStripMenuItem menuEliminarProducto;
        private ToolStripMenuItem menuInfoProducto;

        public bool InicializacionExitosa { get; private set; }

        // ✅ NUEVO: Variables para detectar entrada de lector de código de barras
        private DateTime ultimaTeclaPresionada = DateTime.MinValue;
        private const int UMBRAL_MILISEGUNDOS_SCANNER = 50; // Tiempo entre teclas para considerar scanner
        private bool esEntradaDeScanner = false;

        // ✅ AGREGAR: Variable para controlar si ya se mostró el mensaje del scanner
        private bool mensajeScannerMostrado = false;

        private bool aplicandoOferta = false;

        public Ventas()
        {
            if (!VerificarYSolicitarTurnoAbierto())
            {
                InicializacionExitosa = false;
                InitializeComponent(); // Necesario para evitar errores
                return;
            }

            InicializacionExitosa = true;

            InitializeComponent();
            ConfigurarEstilosFormulario();
            ConfigurarEventHandlers();
            CargarConfiguracion();
            ConfigurarCheckboxCantidad();
            ConfigurarAtajosTeclado();
            ConfigurarMenuContextual(); // NUEVO: Configurar menú contextual

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

        // ✅ NUEVO MÉTODO: Verificar si hay turno abierto y ofrecer apertura
        private bool VerificarYSolicitarTurnoAbierto()
        {
            try
            {
                // Obtener el usuario logueado
                var usuarioActual = AuthenticationService.SesionActual?.Usuario;

                if (usuarioActual == null)
                {
                    MessageBox.Show(
                        "❌ No hay sesión activa.\n\nDebe iniciar sesión para acceder a Ventas.",
                        "Sesión Requerida",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                int numeroCajero = usuarioActual.NumeroCajero;

                // Verificar si existe un turno abierto
                bool tieneTurnoAbierto = VerificarTurnoAbierto(numeroCajero);

                if (tieneTurnoAbierto)
                {
                    // Todo OK, tiene turno abierto - sin mensajes
                    return true;
                }

                // ✅ SIMPLIFICADO: Un solo mensaje preguntando si desea abrir turno
                var resultado = MessageBox.Show(
                    $"⚠️ NO TIENE TURNO ABIERTO\n\n" +
                    $"Usuario: {usuarioActual.NombreUsuario}\n" +
                    $"Cajero: #{numeroCajero}\n\n" +
                    $"Para realizar ventas debe abrir un turno primero.\n\n" +
                    $"¿Desea abrir un turno ahora?",
                    "Turno de Cajero Requerido",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1);

                if (resultado == DialogResult.Yes)
                {
                    // Abrir formulario de apertura de turno
                    using (var formApertura = new AperturaTurnoCajeroForm())
                    {
                        var resultadoApertura = formApertura.ShowDialog();

                        if (resultadoApertura == DialogResult.OK)
                        {
                            // ✅ SIMPLIFICADO: Solo verificar sin mostrar mensaje de éxito
                            if (VerificarTurnoAbierto(numeroCajero))
                            {
                                // ✅ CAMBIO: Sin mensaje de confirmación adicional
                                // El usuario ya sabe que abrió el turno porque lo acaba de hacer
                                return true;
                            }
                            else
                            {
                                // ✅ NUEVO: Solo mostrar error si falló la verificación
                                MessageBox.Show(
                                    "⚠️ El turno no pudo ser verificado.\n\n" +
                                    "Por favor, intente nuevamente desde el menú Caja > Apertura de Turno.",
                                    "Error de Verificación",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                                return false;
                            }
                        }
                        else
                        {
                            // ✅ SIMPLIFICADO: Usuario canceló la apertura - sin mensaje adicional
                            // El cierre del formulario ya es suficiente indicación
                            return false;
                        }
                    }
                }

                // ✅ ELIMINADO: Mensaje final redundante
                // Si llegamos aquí, el usuario dijo "No" a abrir turno
                // No hace falta otro mensaje porque ya rechazó la acción
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Error al verificar turno de caja:\n\n{ex.Message}\n\n" +
                    $"Por seguridad, se cancelará el acceso a Ventas.",
                    "Error de Verificación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private void ConfigurarBotonesCaja()
        {
            // Botón Retirar (existente)
            btnRetirarEfectivo = new Button
            {
                Text = "💰 Retirar",
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TabStop = false
            };
            btnRetirarEfectivo.FlatAppearance.BorderSize = 0;
            btnRetirarEfectivo.Click += BtnRetirarEfectivo_Click;

            if (btnFinalizarVenta?.Parent != null)
            {
                btnFinalizarVenta.Parent.Controls.Add(btnRetirarEfectivo);
            }
            else
            {
                this.Controls.Add(btnRetirarEfectivo);
            }
            btnRetirarEfectivo.BringToFront();

            // ✅ NUEVO: Botón Pagar Proveedor
            btnPagoProveedor = new Button
            {
                Text = "💳 Pagar Prov.",
                Size = new Size(130, 40),
                BackColor = Color.FromArgb(0, 150, 136), // Verde azulado
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TabStop = false
            };
            btnPagoProveedor.FlatAppearance.BorderSize = 0;
            btnPagoProveedor.Click += BtnPagoProveedor_Click;

            if (btnFinalizarVenta?.Parent != null)
            {
                btnFinalizarVenta.Parent.Controls.Add(btnPagoProveedor);
            }
            else
            {
                this.Controls.Add(btnPagoProveedor);
            }
            btnPagoProveedor.BringToFront();

            // ✅ Función de reposicionamiento para AMBOS botones
            void ReposicionarBotonesCaja()
            {
                try
                {
                    if (btnFinalizarVenta != null)
                    {
                        // Retirar a la derecha de Finalizar
                        btnRetirarEfectivo.Height = btnFinalizarVenta.Height;
                        btnRetirarEfectivo.Top = btnFinalizarVenta.Top;
                        btnRetirarEfectivo.Left = btnFinalizarVenta.Right + 15;

                        // Pagar Proveedor a la derecha de Retirar
                        btnPagoProveedor.Height = btnFinalizarVenta.Height;
                        btnPagoProveedor.Top = btnFinalizarVenta.Top;
                        btnPagoProveedor.Left = btnRetirarEfectivo.Right + 15;
                    }
                    else if (btnAnularFactura != null)
                    {
                        btnRetirarEfectivo.Height = btnAnularFactura.Height;
                        btnRetirarEfectivo.Top = btnAnularFactura.Top;
                        btnRetirarEfectivo.Left = btnAnularFactura.Right + 15;

                        btnPagoProveedor.Height = btnAnularFactura.Height;
                        btnPagoProveedor.Top = btnAnularFactura.Top;
                        btnPagoProveedor.Left = btnRetirarEfectivo.Right + 15;
                    }
                    else
                    {
                        btnRetirarEfectivo.Top = 115;
                        btnRetirarEfectivo.Left = 800;

                        btnPagoProveedor.Top = 115;
                        btnPagoProveedor.Left = 935;
                    }
                }
                catch { }
            }

            this.Load += (s, e) => ReposicionarBotonesCaja();
            this.Resize += (s, e) => ReposicionarBotonesCaja();

            if (btnFinalizarVenta?.Parent != null)
            {
                btnFinalizarVenta.Parent.SizeChanged += (s, e) => ReposicionarBotonesCaja();
            }
        }

        // ✅ NUEVO: Event handler para pago a proveedor
        private async void BtnPagoProveedor_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dialogoPago = new Formularios.PagoProveedorRapidoForm())
                {
                    var resultado = dialogoPago.ShowDialog(this);

                    if (resultado == DialogResult.OK && dialogoPago.Confirmado)
                    {
                        await RegistrarPagoProveedorAsync(
                            dialogoPago.ProveedorSeleccionado,
                            dialogoPago.Monto,
                            dialogoPago.Observaciones);

                        MessageBox.Show(
                            $"✅ PAGO REGISTRADO\n\n" +
                            $"Proveedor: {dialogoPago.ProveedorSeleccionado}\n" +
                            $"Monto: {dialogoPago.Monto:C2}\n" +
                            $"Observaciones: {dialogoPago.Observaciones}\n\n" +
                            $"El pago se reflejará en:\n" +
                            $"• Cuenta corriente del proveedor\n" +
                            $"• Cálculos de arqueo y cierre de caja",
                            "Pago a Proveedor",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al registrar el pago: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ✅ NUEVO: Registrar pago en base de datos
        private async Task RegistrarPagoProveedorAsync(string proveedor, decimal monto, string observaciones)
        {
            string connectionString = GetConnectionString();
            string usuario = ObtenerUsuarioActual();
            int numeroCajero = obtenerNumeroCajero();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // ✅ MODIFICADO: Obtener IdProveedor desde el nombre
                        int? idProveedor = null;
                        var queryGetId = "SELECT Id FROM Proveedores WHERE Nombre = @nombre";
                        using (var cmdId = new SqlCommand(queryGetId, connection, transaction))
                        {
                            cmdId.Parameters.AddWithValue("@nombre", proveedor);
                            var result = await cmdId.ExecuteScalarAsync();
                            if (result != null && result != DBNull.Value)
                            {
                                idProveedor = Convert.ToInt32(result);
                            }
                        }

                        // 1. Registrar el pago en tabla de pagos a proveedores
                        var queryPago = @"
                    INSERT INTO PagosProveedores 
                        (IdProveedor, Proveedor, Monto, Observaciones, NumeroCajero, UsuarioRegistro, 
                         FechaPago, NumeroRemito, NombreEquipo)
                    VALUES 
                        (@IdProveedor, @Proveedor, @Monto, @Observaciones, @NumeroCajero, @UsuarioRegistro,
                         @FechaPago, @NumeroRemito, @NombreEquipo);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        int idPago;
                        using (var cmd = new SqlCommand(queryPago, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@IdProveedor", idProveedor.HasValue ? (object)idProveedor.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@Proveedor", proveedor);
                            cmd.Parameters.AddWithValue("@Monto", monto);
                            cmd.Parameters.AddWithValue("@Observaciones",
                                string.IsNullOrWhiteSpace(observaciones) ? (object)DBNull.Value : observaciones);
                            cmd.Parameters.AddWithValue("@NumeroCajero", numeroCajero);
                            cmd.Parameters.AddWithValue("@UsuarioRegistro", usuario);
                            cmd.Parameters.AddWithValue("@FechaPago", DateTime.Now);
                            cmd.Parameters.AddWithValue("@NumeroRemito", (object)nroRemitoActual ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@NombreEquipo", Environment.MachineName);

                            idPago = (int)await cmd.ExecuteScalarAsync();
                        }

                        // 2. Actualizar cuenta corriente del proveedor
                        if (idProveedor.HasValue)
                        {
                            try
                            {
                                var queryCtaCte = @"
                            INSERT INTO CtaCteProveedores 
                                (IdProveedor, Proveedor, Fecha, Concepto, Debe, Haber, IdPago, Usuario)
                            VALUES 
                                (@IdProveedor, @Proveedor, @Fecha, @Concepto, @Debe, @Haber, @IdPago, @Usuario)";

                                using (var cmd = new SqlCommand(queryCtaCte, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@IdProveedor", idProveedor.Value);
                                    cmd.Parameters.AddWithValue("@Proveedor", proveedor);
                                    cmd.Parameters.AddWithValue("@Fecha", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@Concepto",
                                        $"Pago - {(string.IsNullOrWhiteSpace(observaciones) ? "Sin observaciones" : observaciones)}");
                                    cmd.Parameters.AddWithValue("@Debe", 0);
                                    cmd.Parameters.AddWithValue("@Haber", monto);
                                    cmd.Parameters.AddWithValue("@IdPago", idPago);
                                    cmd.Parameters.AddWithValue("@Usuario", usuario);

                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ Error actualizando CtaCte: {ex.Message}");
                            }
                        }

                        transaction.Commit();

                        System.Diagnostics.Debug.WriteLine(
                            $"💳 Pago registrado - Proveedor: {proveedor} (ID: {idProveedor}), Monto: {monto:C2}, Usuario: {usuario}");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Error en transacción de pago: {ex.Message}", ex);
                    }
                }
            }
        }

        // ✅ NUEVO MÉTODO: Verificar si existe turno abierto en la base de datos
        private bool VerificarTurnoAbierto(int numeroCajero)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var query = @"
                        SELECT COUNT(*) 
                        FROM TurnosCajero 
                        WHERE NumeroCajero = @numeroCajero 
                        AND Estado = 'Abierto'";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@numeroCajero", numeroCajero);

                        int count = (int)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error verificando turno: {ex.Message}");
                // En caso de error, por seguridad retornar false
                return false;
            }
        }



        // NUEVO: Configurar menú contextual
        private void ConfigurarMenuContextual()
        {
            contextMenuGrilla = new ContextMenuStrip();

            menuEditarCantidad = new ToolStripMenuItem("Editar Cantidad");
            menuEditarCantidad.Click += MenuEditarCantidad_Click;

            menuEliminarProducto = new ToolStripMenuItem("Eliminar Producto");
            menuEliminarProducto.Click += MenuEliminarProducto_Click;

            menuInfoProducto = new ToolStripMenuItem("Información del Producto");
            menuInfoProducto.Click += MenuInfoProducto_Click;

            // CORREGIDO: Agregar elementos individualmente en lugar de usar array
            contextMenuGrilla.Items.Add(menuEditarCantidad);
            contextMenuGrilla.Items.Add(new ToolStripSeparator());
            contextMenuGrilla.Items.Add(menuEliminarProducto);
            contextMenuGrilla.Items.Add(new ToolStripSeparator());
            contextMenuGrilla.Items.Add(menuInfoProducto);

            // Configurar estilo
            contextMenuGrilla.Font = new Font("Segoe UI", 9F);
            contextMenuGrilla.BackColor = Color.White;
        }

        // NUEVO: Evento de editar cantidad desde menú contextual
        private async void MenuEditarCantidad_Click(object sender, EventArgs e)
        {
            await EditarCantidadProductoSeleccionado();
        }

        // NUEVO: Evento de eliminar producto desde menú contextual
        private async void MenuEliminarProducto_Click(object sender, EventArgs e)
        {
            await EliminarProductoConAuditoria();
        }

        // NUEVO: Evento de información del producto desde menú contextual
        private void MenuInfoProducto_Click(object sender, EventArgs e)
        {
            MostrarInformacionProducto();
        }

        // NUEVO: Mostrar información del producto
        private void MostrarInformacionProducto()
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un producto para ver su información.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var row = dataGridView1.SelectedRows[0];
                var codigo = row.Cells["codigo"].Value?.ToString();
                var descripcion = row.Cells["descripcion"].Value?.ToString();
                var precio = row.Cells["precio"].Value?.ToString();
                var cantidad = row.Cells["cantidad"].Value?.ToString();
                var total = row.Cells["total"].Value?.ToString();

                // Obtener información adicional del producto de la base de datos
                string infoCompleta = ObtenerInformacionCompletaProducto(codigo);

                MessageBox.Show(
                    $"INFORMACIÓN DEL PRODUCTO\n\n" +
                    $"Código: {codigo}\n" +
                    $"Descripción: {descripcion}\n" +
                    $"Precio unitario: {precio}\n" +
                    $"Cantidad: {cantidad}\n" +
                    $"Total línea: {total}\n\n" +
                    infoCompleta,
                    "Información del Producto",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener información del producto: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NUEVO: Obtener información completa del producto
        private string ObtenerInformacionCompletaProducto(string codigo)
        {
            try
            {
                string connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"SELECT rubro, marca, proveedor, costo, cantidad as stock, iva 
                                  FROM Productos WHERE codigo = @codigo";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@codigo", codigo ?? "");
                        connection.Open();

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return $"Rubro: {reader["rubro"]}\n" +
                                       $"Marca: {reader["marca"]}\n" +
                                       $"Proveedor: {reader["proveedor"]}\n" +
                                       $"Costo: ${reader["costo"]:N2}\n" +
                                       $"Stock disponible: {reader["stock"]}\n" +
                                       $"IVA: {reader["iva"]}%";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo info producto: {ex.Message}");
            }

            return "No se pudo obtener información adicional.";
        }

        // NUEVO: Editar cantidad del producto seleccionado - MODIFICADO para usar ID único
        private async Task EditarCantidadProductoSeleccionado()
        {
            if (procesandoEdicionCantidad) return;

            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un producto para editar su cantidad.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            procesandoEdicionCantidad = true;

            try
            {
                var row = dataGridView1.SelectedRows[0];

                // MODIFICADO: Obtener el ID único de la fila en lugar del código
                if (!int.TryParse(row.Cells["id"].Value?.ToString(), out int idVenta))
                {
                    MessageBox.Show("Error: No se pudo obtener el ID de la venta.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var codigo = row.Cells["codigo"].Value?.ToString();
                var descripcion = row.Cells["descripcion"].Value?.ToString();
                var cantidadActual = Convert.ToInt32(row.Cells["cantidad"].Value);
                var precio = Convert.ToDecimal(row.Cells["precio"].Value);

                // MEJORADO: Usar el nuevo diálogo visual
                using (var dialog = new EditarCantidadDialog(codigo, descripcion, cantidadActual))
                {
                    var resultado = dialog.ShowDialog(this);

                    if (resultado == DialogResult.OK && dialog.Confirmado)
                    {
                        int nuevaCantidad = dialog.NuevaCantidad;

                        // MODIFICADO: Actualizar usando el ID único de la fila
                        await ActualizarCantidadEnVentaPorId(idVenta, nuevaCantidad, precio);

                        // Recargar la vista
                        CargarVentasActuales();

                        System.Diagnostics.Debug.WriteLine($"Cantidad actualizada: ID {idVenta} (código {codigo}) - Nueva cantidad: {nuevaCantidad}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al editar cantidad: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                procesandoEdicionCantidad = false;
            }
        }

        // NUEVO: Actualizar cantidad en la base de datos por ID único - REEMPLAZA ActualizarCantidadEnVenta
        private async Task ActualizarCantidadEnVentaPorId(int idVenta, int nuevaCantidad, decimal precio)
        {
            string connectionString = GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // ✅✅✅ CRÍTICO: Configurar ARITHABORT ANTES de cualquier operación
                using (var cmdConfig = new SqlCommand("SET ARITHABORT ON; SET ANSI_WARNINGS ON;", connection))
                {
                    await cmdConfig.ExecuteNonQueryAsync();
                }

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // ✅ PASO 1: Obtener información completa del producto Y cantidad actual
                        string codigo = "";
                        string descripcion = "";
                        int cantidadActual = 0;
                        decimal precioOriginal = 0m;

                        var queryObtenerDatos = @"
                    SELECT v.codigo, v.descripcion, v.cantidad, p.precio as precio_producto 
                    FROM Ventas v
                    INNER JOIN Productos p ON v.codigo = p.codigo
                    WHERE v.id = @idVenta";

                        using (var cmd = new SqlCommand(queryObtenerDatos, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@idVenta", idVenta);

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    codigo = reader["codigo"].ToString();
                                    descripcion = reader["descripcion"].ToString();
                                    cantidadActual = Convert.ToInt32(reader["cantidad"]);
                                    precioOriginal = Convert.ToDecimal(reader["precio_producto"]);
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(codigo))
                        {
                            throw new Exception("No se pudo obtener el código del producto.");
                        }

                        // ✅✅✅ NUEVO: Si se reduce la cantidad, registrar en auditoría
                        if (nuevaCantidad < cantidadActual)
                        {
                            int cantidadEliminada = cantidadActual - nuevaCantidad;
                            string usuario = ObtenerUsuarioActual();
                            int numeroCajero = obtenerNumeroCajero();

                            string queryAuditoria = @"
                        INSERT INTO AuditoriaProductosEliminados 
                            (CodigoProducto, DescripcionProducto, PrecioUnitario, Cantidad, 
                             TotalEliminado, NumeroFactura, FechaHoraVentaOriginal, FechaEliminacion, 
                             MotivoEliminacion, EsCtaCte, NombreCtaCte, UsuarioEliminacion, 
                             NumeroCajero, NombreEquipo, EsEliminacionCompleta, CantidadOriginal)
                        VALUES 
                            (@CodigoProducto, @DescripcionProducto, @PrecioUnitario, @Cantidad,
                             @TotalEliminado, @NumeroFactura, @FechaHoraVentaOriginal, @FechaEliminacion,
                             @MotivoEliminacion, @EsCtaCte, @NombreCtaCte, @UsuarioEliminacion,
                             @NumeroCajero, @NombreEquipo, @EsEliminacionCompleta, @CantidadOriginal)";

                            using (var cmdAudit = new SqlCommand(queryAuditoria, connection, transaction))
                            {
                                cmdAudit.Parameters.AddWithValue("@CodigoProducto", codigo);
                                cmdAudit.Parameters.AddWithValue("@DescripcionProducto", descripcion);
                                cmdAudit.Parameters.AddWithValue("@PrecioUnitario", precio);
                                cmdAudit.Parameters.AddWithValue("@Cantidad", cantidadEliminada);
                                cmdAudit.Parameters.AddWithValue("@TotalEliminado", precio * cantidadEliminada);
                                cmdAudit.Parameters.AddWithValue("@NumeroFactura", nroRemitoActual);
                                cmdAudit.Parameters.AddWithValue("@FechaHoraVentaOriginal", DateTime.Now);
                                cmdAudit.Parameters.AddWithValue("@FechaEliminacion", DateTime.Now);
                                cmdAudit.Parameters.AddWithValue("@MotivoEliminacion", "REDUCCIÓN DE CANTIDAD - EDICIÓN MANUAL");
                                cmdAudit.Parameters.AddWithValue("@EsCtaCte", chkEsCtaCte?.Checked ?? false);
                                cmdAudit.Parameters.AddWithValue("@NombreCtaCte", chkEsCtaCte?.Checked == true ? (object)cbnombreCtaCte?.Text : DBNull.Value);
                                cmdAudit.Parameters.AddWithValue("@UsuarioEliminacion", usuario);
                                cmdAudit.Parameters.AddWithValue("@NumeroCajero", numeroCajero);
                                cmdAudit.Parameters.AddWithValue("@NombreEquipo", Environment.MachineName);
                                cmdAudit.Parameters.AddWithValue("@EsEliminacionCompleta", false); // ✅ SIEMPRE false porque es reducción
                                cmdAudit.Parameters.AddWithValue("@CantidadOriginal", cantidadActual);

                                await cmdAudit.ExecuteNonQueryAsync();

                                System.Diagnostics.Debug.WriteLine(
                                    $"📝 AUDITORÍA REGISTRADA - Reducción de cantidad:\n" +
                                    $"   Producto: {descripcion} ({codigo})\n" +
                                    $"   Cantidad original: {cantidadActual}\n" +
                                    $"   Cantidad eliminada: {cantidadEliminada}\n" +
                                    $"   Cantidad nueva: {nuevaCantidad}\n" +
                                    $"   Usuario: {usuario}");
                            }
                        }

                        // ✅ PASO 2: Verificar si hay oferta para la nueva cantidad
                        var oferta = await BuscarOfertaAplicable(codigo, nuevaCantidad);

                        decimal precioFinal = precioOriginal; // Por defecto, precio normal
                        bool cambioDeOferta = false;
                        string mensajeOferta = "";

                        // ✅✅✅ NUEVO: Si es un COMBO, verificar si se completa
                        if (oferta != null && oferta.TipoOferta == "Combo")
                        {
                            // ✅ Verificar si el combo está completo con las nuevas cantidades
                            bool comboCompleto = await VerificarComboCompleto(
                                oferta.Id,
                                codigo,
                                0, // ✅ NO sumar porque la cantidad ya está actualizada en memoria
                                connection,
                                transaction);

                            if (comboCompleto)
                            {
                                // ✅ COMBO COMPLETO: Aplicar precio prorrateado
                                precioFinal = await CalcularPrecioComboProrrateado(
                                    oferta.Id,
                                    codigo,
                                    oferta.PrecioCombo,
                                    connection,
                                    transaction);

                                System.Diagnostics.Debug.WriteLine(
                                    $"✅ COMBO COMPLETO - Aplicando precio prorrateado: {precioFinal:C2}");

                                if (Math.Abs(precio - precioFinal) > 0.01m)
                                {
                                    cambioDeOferta = true;
                                    mensajeOferta =
                                        $"🎉 ¡COMBO APLICADO!\n\n" +
                                        $"Oferta: {oferta.NombreOferta}\n" +
                                        $"Nueva cantidad: {nuevaCantidad}\n\n" +
                                        $"Precio anterior: {precio:C2}\n" +
                                        $"Precio combo: {precioFinal:C2}";
                                }
                            }
                            else
                            {
                                // ✅ COMBO INCOMPLETO: Usar precio normal
                                precioFinal = precioOriginal;

                                System.Diagnostics.Debug.WriteLine(
                                    $"⚠️ COMBO INCOMPLETO - Usando precio normal: {precioFinal:C2}");
                            }
                        }
                        else if (oferta != null && oferta.PrecioOferta > 0)
                        {
                            // ✅ OFERTA NORMAL (no combo) - Aplicar precio de oferta
                            precioFinal = oferta.PrecioOferta;

                            if (Math.Abs(precio - oferta.PrecioOferta) > 0.01m)
                            {
                                cambioDeOferta = true;

                                if (precio > oferta.PrecioOferta)
                                {
                                    mensajeOferta =
                                        $"🎉 ¡OFERTA APLICADA!\n\n" +
                                        $"Oferta: {oferta.NombreOferta}\n" +
                                        $"Tipo: {oferta.TipoOferta}\n" +
                                        $"Cantidad requerida: {oferta.CantidadMinima}\n" +
                                        $"Nueva cantidad: {nuevaCantidad}\n\n" +
                                        $"Precio anterior: {precio:C2}\n" +
                                        $"Precio oferta: {oferta.PrecioOferta:C2}\n" +
                                        $"Ahorro: {(precio - oferta.PrecioOferta):C2} ({oferta.PorcentajeDescuento:N2}%)";
                                }
                            }

                            System.Diagnostics.Debug.WriteLine(
                                $"✅ OFERTA APLICADA - Tipo: {oferta.TipoOferta}\n" +
                                $"   Precio normal: {precioOriginal:C2}\n" +
                                $"   Precio con oferta: {precioFinal:C2}\n" +
                                $"   Descuento: {(precioOriginal - precioFinal):C2}");
                        }
                        else
                        {
                            // ✅ NO HAY OFERTA para la nueva cantidad
                            precioFinal = precioOriginal;

                            if (precio < precioOriginal - 0.01m)
                            {
                                cambioDeOferta = true;
                                mensajeOferta =
                                    $"⚠️ OFERTA NO DISPONIBLE\n\n" +
                                    $"La cantidad {nuevaCantidad} no cumple el mínimo para ofertas.\n\n" +
                                    $"Precio anterior (oferta): {precio:C2}\n" +
                                    $"Precio normal: {precioOriginal:C2}";
                            }
                        }

                        // ✅✅✅ CRÍTICO: UPDATE COMPLETO incluyendo campos de oferta
                        var query = @"UPDATE Ventas 
                      SET cantidad = @nuevaCantidad, 
                          precio = @precio,
                          total = @nuevaCantidad * @precio,
                          IdOferta = @IdOferta,
                          NombreOferta = @NombreOferta,
                          PrecioOriginal = @PrecioOriginal,
                          PrecioConOferta = @PrecioConOferta,
                          DescuentoAplicado = @DescuentoAplicado,
                          EsOferta = @EsOferta
                      WHERE id = @idVenta";

                        using (var cmd = new SqlCommand(query, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@nuevaCantidad", nuevaCantidad);
                            cmd.Parameters.AddWithValue("@precio", precioFinal);
                            cmd.Parameters.AddWithValue("@idVenta", idVenta);

                            if (oferta != null)
                            {
                                cmd.Parameters.AddWithValue("@IdOferta", oferta.Id);
                                cmd.Parameters.AddWithValue("@NombreOferta", oferta.NombreOferta ?? "");
                                cmd.Parameters.AddWithValue("@PrecioOriginal", precioOriginal);
                                cmd.Parameters.AddWithValue("@PrecioConOferta", precioFinal);
                                cmd.Parameters.AddWithValue("@DescuentoAplicado", Math.Round(precioOriginal - precioFinal, 2));
                                cmd.Parameters.AddWithValue("@EsOferta", 1);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@IdOferta", DBNull.Value);
                                cmd.Parameters.AddWithValue("@NombreOferta", DBNull.Value);
                                cmd.Parameters.AddWithValue("@PrecioOriginal", DBNull.Value);
                                cmd.Parameters.AddWithValue("@PrecioConOferta", DBNull.Value);
                                cmd.Parameters.AddWithValue("@DescuentoAplicado", DBNull.Value);
                                cmd.Parameters.AddWithValue("@EsOferta", 0);
                            }

                            await cmd.ExecuteNonQueryAsync();
                        }

                        // ✅✅✅ NUEVO: Si es COMBO y está completo, actualizar TODOS los productos del combo
                        if (oferta != null && oferta.TipoOferta == "Combo")
                        {
                            bool comboCompleto = await VerificarComboCompleto(
                                oferta.Id,
                                codigo,
                                0,
                                connection,
                                transaction);

                            if (comboCompleto)
                            {
                                await ActualizarPreciosComboCompleto(
                                    oferta.Id,
                                    connection,
                                    transaction);
                            }
                        }

                        transaction.Commit();

                        // ✅ Mostrar mensaje solo si hubo cambio de oferta
                        if (cambioDeOferta && !string.IsNullOrEmpty(mensajeOferta))
                        {
                            MessageBox.Show(
                                mensajeOferta,
                                "Actualización de Precio",
                                MessageBoxButtons.OK,
                                oferta != null ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                        }

                        // DEBUG
                        System.Diagnostics.Debug.WriteLine(
                            $"✅ Cantidad actualizada - ID: {idVenta}, Código: {codigo}\n" +
                            $"   Cantidad original: {cantidadActual}\n" +
                            $"   Nueva cantidad: {nuevaCantidad}\n" +
                            $"   ¿Se redujo?: {(nuevaCantidad < cantidadActual ? "Sí (auditado)" : "No")}\n" +
                            $"   Precio aplicado: {precioFinal:C2}\n" +
                            $"   ¿Tiene oferta?: {(oferta != null ? "Sí" : "No")}\n" +
                            $"   ¿Es combo?: {(oferta?.TipoOferta == "Combo" ? "Sí" : "No")}\n" +
                            $"   ¿Cambió precio?: {cambioDeOferta}");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"❌ Error en ActualizarCantidadEnVentaPorId: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        // NUEVO: Eliminar producto con auditoría - MODIFICADO para usar ID único
        private async Task EliminarProductoConAuditoria()
        {
            if (procesandoEliminacion) return;

            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un producto para eliminar.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            procesandoEliminacion = true;

            try
            {
                var row = dataGridView1.SelectedRows[0];

                // MODIFICADO: Obtener el ID único de la fila en lugar del código
                if (!int.TryParse(row.Cells["id"].Value?.ToString(), out int idVenta))
                {
                    MessageBox.Show("Error: No se pudo obtener el ID de la venta.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var codigo = row.Cells["codigo"].Value?.ToString();
                var descripcion = row.Cells["descripcion"].Value?.ToString();
                var cantidad = Convert.ToInt32(row.Cells["cantidad"].Value);
                var precio = Convert.ToDecimal(row.Cells["precio"].Value);
                var total = Convert.ToDecimal(row.Cells["total"].Value);

                // Verificar permisos de eliminación si el sistema de login está habilitado
                if (AuthenticationService.ConfiguracionLogin?.LoginHabilitado == true)
                {
                    if (AuthenticationService.SesionActual?.Usuario == null)
                    {
                        MessageBox.Show("No hay una sesión activa.", "Error de Autenticación",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var usuario = AuthenticationService.SesionActual.Usuario;
                    if (!usuario.PuedeEliminarProductos && usuario.Nivel != Models.NivelUsuario.Administrador)
                    {
                        MessageBox.Show(
                            "❌ ACCESO DENEGADO\n\n" +
                            "No tienes permisos para eliminar productos de la venta.\n\n" +
                            "Este acción requiere el permiso 'Eliminar Productos'.\n" +
                            "Contacta a un administrador si necesitas realizar esta acción.",
                            "Permisos Insuficientes",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }

                // MEJORADO: Usar el nuevo diálogo visual con soporte para eliminación parcial
                using (var dialog = new MotivoEliminacionForm(descripcion, cantidad, codigo, precio))
                {
                    var resultado = dialog.ShowDialog(this);

                    if (resultado == DialogResult.OK && !string.IsNullOrEmpty(dialog.MotivoSeleccionado))
                    {
                        string motivo = dialog.MotivoSeleccionado;
                        int cantidadAEliminar = dialog.CantidadAEliminar;
                        bool eliminarCompleto = (cantidadAEliminar >= cantidad);

                        // MODIFICADO: Registrar auditoría y procesar eliminación usando ID único
                        await ProcesarEliminacionConAuditoriaPorId(idVenta, codigo, descripcion, cantidad,
                            cantidadAEliminar, precio, eliminarCompleto, motivo);

                        // Recargar la vista
                        CargarVentasActuales();

                        System.Diagnostics.Debug.WriteLine($"Producto procesado - ID: {idVenta}, Código: {codigo}, " +
                            $"Eliminado: {cantidadAEliminar}/{cantidad}, Completo: {eliminarCompleto}, Motivo: {motivo}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar producto: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                procesandoEliminacion = false;
            }
        }

        // NUEVO: Procesar eliminación con auditoría por ID único - REEMPLAZA ProcesarEliminacionConAuditoria
        private async Task ProcesarEliminacionConAuditoriaPorId(int idVenta, string codigo, string descripcion,
            int cantidadTotal, int cantidadAEliminar, decimal precio, bool eliminarCompleto, string motivo)
        {
            string connectionString = GetConnectionString();
            string usuario = AuthenticationService.SesionActual?.Usuario?.NombreUsuario ?? Environment.UserName;
            int numeroCajero = AuthenticationService.SesionActual?.Usuario?.NumeroCajero ?? 1;

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync(); // ✅ USAR OpenAsync en lugar de Open

                // ✅✅✅ CRÍTICO: Configurar ARITHABORT ANTES de la transacción
                using (var cmdConfig = new SqlCommand("SET ARITHABORT ON; SET ANSI_WARNINGS ON;", connection))
                {
                    await cmdConfig.ExecuteNonQueryAsync();
                }

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Registrar la auditoría en AuditoriaProductosEliminados
                        var queryAuditoria = @"INSERT INTO AuditoriaProductosEliminados 
                               (CodigoProducto, DescripcionProducto, PrecioUnitario, Cantidad, 
                                TotalEliminado, NumeroFactura, FechaHoraVentaOriginal, FechaEliminacion, 
                                MotivoEliminacion, EsCtaCte, NombreCtaCte, UsuarioEliminacion, 
                                NumeroCajero, NombreEquipo, EsEliminacionCompleta, CantidadOriginal)
                               VALUES (@CodigoProducto, @DescripcionProducto, @PrecioUnitario, @Cantidad,
                                       @TotalEliminado, @NumeroFactura, @FechaHoraVentaOriginal, @FechaEliminacion,
                                       @MotivoEliminacion, @EsCtaCte, @NombreCtaCte, @UsuarioEliminacion,
                                       @NumeroCajero, @NombreEquipo, @EsEliminacionCompleta, @CantidadOriginal)";

                        using (var cmd = new SqlCommand(queryAuditoria, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CodigoProducto", codigo);
                            cmd.Parameters.AddWithValue("@DescripcionProducto", descripcion);
                            cmd.Parameters.AddWithValue("@PrecioUnitario", precio);
                            cmd.Parameters.AddWithValue("@Cantidad", cantidadAEliminar);
                            cmd.Parameters.AddWithValue("@TotalEliminado", precio * cantidadAEliminar);
                            cmd.Parameters.AddWithValue("@NumeroFactura", nroRemitoActual);
                            cmd.Parameters.AddWithValue("@FechaHoraVentaOriginal", DateTime.Now);
                            cmd.Parameters.AddWithValue("@FechaEliminacion", DateTime.Now);
                            cmd.Parameters.AddWithValue("@MotivoEliminacion", motivo);
                            cmd.Parameters.AddWithValue("@EsCtaCte", chkEsCtaCte.Checked);
                            cmd.Parameters.AddWithValue("@NombreCtaCte", chkEsCtaCte.Checked ? (object)cbnombreCtaCte.Text : DBNull.Value);
                            cmd.Parameters.AddWithValue("@UsuarioEliminacion", usuario);
                            cmd.Parameters.AddWithValue("@NumeroCajero", numeroCajero);
                            cmd.Parameters.AddWithValue("@NombreEquipo", Environment.MachineName);
                            cmd.Parameters.AddWithValue("@EsEliminacionCompleta", eliminarCompleto);
                            cmd.Parameters.AddWithValue("@CantidadOriginal", cantidadTotal);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        // 2. Procesar eliminación en la venta
                        if (eliminarCompleto)
                        {
                            // Eliminar la línea completa
                            var queryEliminar = @"DELETE FROM Ventas WHERE id = @idVenta";

                            using (var cmd = new SqlCommand(queryEliminar, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@idVenta", idVenta);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // ✅ NUEVO: Eliminación parcial - Validar ofertas para la cantidad restante
                            int cantidadRestante = cantidadTotal - cantidadAEliminar;

                            // ✅ Obtener precio original del producto
                            decimal precioOriginal = 0m;
                            var queryPrecioOriginal = @"
        SELECT p.precio 
        FROM Productos p 
        WHERE p.codigo = @codigo";

                            using (var cmd = new SqlCommand(queryPrecioOriginal, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@codigo", codigo);
                                var result = await cmd.ExecuteScalarAsync();
                                if (result != null && result != DBNull.Value)
                                {
                                    precioOriginal = Convert.ToDecimal(result);
                                }
                            }

                            // ✅ Verificar si hay oferta para la cantidad restante
                            var ofertaRestante = await BuscarOfertaAplicable(codigo, cantidadRestante);

                            decimal precioFinal;
                            string mensajeOferta = "";
                            bool cambioDeOferta = false;

                            if (ofertaRestante != null && ofertaRestante.PrecioOferta > 0)
                            {
                                // ✅ AÚN cumple con una oferta
                                precioFinal = ofertaRestante.PrecioOferta;

                                if (Math.Abs(precio - ofertaRestante.PrecioOferta) > 0.01m)
                                {
                                    cambioDeOferta = true;

                                    if (precio > ofertaRestante.PrecioOferta)
                                    {
                                        mensajeOferta =
                                            $"🎉 ¡MEJOR OFERTA ACTIVADA!\n\n" +
                                            $"Al reducir la cantidad, ahora califica para una oferta mejor.\n\n" +
                                            $"Oferta: {ofertaRestante.NombreOferta}\n" +
                                            $"Cantidad restante: {cantidadRestante}\n" +
                                            $"Precio anterior: {precio:C2}\n" +
                                            $"Precio oferta: {ofertaRestante.PrecioOferta:C2}\n" +
                                            $"Ahorro adicional: {(precio - ofertaRestante.PrecioOferta):C2}";
                                    }
                                    else if (precio < ofertaRestante.PrecioOferta)
                                    {
                                        mensajeOferta =
                                            $"⚠️ CAMBIO DE OFERTA\n\n" +
                                            $"La cantidad restante califica para una oferta diferente.\n\n" +
                                            $"Oferta: {ofertaRestante.NombreOferta}\n" +
                                            $"Cantidad restante: {cantidadRestante}\n" +
                                            $"Precio anterior: {precio:C2}\n" +
                                            $"Nuevo precio: {ofertaRestante.PrecioOferta:C2}";
                                    }
                                }
                            }
                            else
                            {
                                // ✅ YA NO cumple con ninguna oferta - usar precio normal
                                precioFinal = precioOriginal;

                                if (precio < precioOriginal - 0.01m)
                                {
                                    cambioDeOferta = true;
                                    mensajeOferta =
                                        $"⚠️ OFERTA PERDIDA\n\n" +
                                        $"La cantidad restante ({cantidadRestante}) no cumple el mínimo para ofertas.\n\n" +
                                        $"Precio anterior (oferta): {precio:C2}\n" +
                                        $"Precio normal: {precioOriginal:C2}\n" +
                                        $"Diferencia: +{(precioOriginal - precio):C2}";
                                }
                            }

                            // ✅✅✅ CRÍTICO: UPDATE COMPLETO con campos de oferta
                            var queryActualizar = @"UPDATE Ventas 
           SET cantidad = @cantidadRestante,
               precio = @precioFinal,
               total = @cantidadRestante * @precioFinal,
               IdOferta = @IdOferta,
               NombreOferta = @NombreOferta,
               PrecioOriginal = @PrecioOriginal,
               PrecioConOferta = @PrecioConOferta,
               DescuentoAplicado = @DescuentoAplicado,
               EsOferta = @EsOferta
           WHERE id = @idVenta";

                            using (var cmd = new SqlCommand(queryActualizar, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@cantidadRestante", cantidadRestante);
                                cmd.Parameters.AddWithValue("@precioFinal", precioFinal);
                                cmd.Parameters.AddWithValue("@idVenta", idVenta);

                                // ✅✅✅ CRÍTICO: Agregar parámetros de oferta
                                if (ofertaRestante != null)
                                {
                                    cmd.Parameters.AddWithValue("@IdOferta", ofertaRestante.Id);
                                    cmd.Parameters.AddWithValue("@NombreOferta", ofertaRestante.NombreOferta ?? "");
                                    cmd.Parameters.AddWithValue("@PrecioOriginal", precioOriginal);
                                    cmd.Parameters.AddWithValue("@PrecioConOferta", precioFinal);
                                    cmd.Parameters.AddWithValue("@DescuentoAplicado", Math.Round(precioOriginal - precioFinal, 2));
                                    cmd.Parameters.AddWithValue("@EsOferta", 1);
                                }
                                else
                                {
                                    // ✅✅✅ LIMPIAR campos cuando NO hay oferta
                                    cmd.Parameters.AddWithValue("@IdOferta", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@NombreOferta", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PrecioOriginal", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PrecioConOferta", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DescuentoAplicado", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EsOferta", 0);
                                }

                                await cmd.ExecuteNonQueryAsync();
                            }

                            // ✅ Mostrar mensaje SOLO si cambió la oferta
                            if (cambioDeOferta && !string.IsNullOrEmpty(mensajeOferta))
                            {
                                transaction.Commit();

                                MessageBox.Show(
                                    mensajeOferta,
                                    "Actualización de Precio",
                                    MessageBoxButtons.OK,
                                    ofertaRestante != null ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

                                System.Diagnostics.Debug.WriteLine(
                                    $"✅ Eliminación parcial procesada - ID: {idVenta}, Código: {codigo}\n" +
                                    $"   Cantidad original: {cantidadTotal}\n" +
                                    $"   Cantidad eliminada: {cantidadAEliminar}\n" +
                                    $"   Cantidad restante: {cantidadRestante}\n" +
                                    $"   Precio anterior: {precio:C2}\n" +
                                    $"   Precio final: {precioFinal:C2}\n" +
                                    $"   ¿Tiene oferta?: {(ofertaRestante != null ? "Sí" : "No")}\n" +
                                    $"   ¿Cambió precio?: {cambioDeOferta}");

                                return;
                            }
                        }

                        transaction.Commit();

                        System.Diagnostics.Debug.WriteLine(
                            $"✅ Eliminación procesada correctamente - ID: {idVenta}, Código: {codigo}, " +
                            $"Eliminado: {cantidadAEliminar}/{cantidadTotal}, Completo: {eliminarCompleto}");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"❌ Error en ProcesarEliminacionConAuditoriaPorId: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        private void Ventas_Resize(object sender, EventArgs e)
        {
            // Ajustar el DataGridView cuando se redimensiona el formulario
            if (dataGridView1 != null)
            {
                dataGridView1.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 171 - 65); // ✅ CAMBIADO: -65 en lugar de -100
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

            // NUEVO: Configurar evento del checkbox de cuenta corriente
            chkEsCtaCte.CheckedChanged += chkEsCtaCte_CheckedChanged;

            ConfigurarEventosTextBox();
            ConfigurarEventosDataGridView();
            this.FormClosing += Ventas_FormClosing;
        }

        // NUEVO: Método para manejar el evento del checkbox de cuenta corriente
        private void chkEsCtaCte_CheckedChanged(object sender, EventArgs e)
        {
            if (chkEsCtaCte.Checked)
            {
                // Activar y cargar el ComboBox con los nombres de cuenta corriente
                cbnombreCtaCte.Enabled = true;
                CargarNombresCuentasCorrientes();

                // Hacer visible si estaba oculto
                cbnombreCtaCte.Visible = true;

                // Opcional: Dar foco al ComboBox para facilitar la selección
                cbnombreCtaCte.Focus();
            }
            else
            {
                // Desactivar el ComboBox y limpiar la selección
                cbnombreCtaCte.Enabled = false;
                cbnombreCtaCte.SelectedIndex = -1;
                cbnombreCtaCte.Text = "";

                // Opcional: Ocultar el ComboBox
                // cbnombreCtaCte.Visible = false;

                // Devolver el foco al campo de búsqueda de productos
                txtBuscarProducto.Focus();
            }
        }

        // NUEVO: Método para cargar los nombres de cuentas corrientes desde la configuración
        private void CargarNombresCuentasCorrientes()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                // Leer la lista de nombres desde la configuración
                var nombresCuentasCorrientes = config.GetSection("CuentasCorrientes:NombresCtaCte").Get<string[]>();

                // Limpiar el ComboBox antes de cargar
                cbnombreCtaCte.Items.Clear();

                if (nombresCuentasCorrientes != null && nombresCuentasCorrientes.Length > 0)
                {
                    // Agregar cada nombre al ComboBox
                    foreach (string nombre in nombresCuentasCorrientes)
                    {
                        if (!string.IsNullOrWhiteSpace(nombre))
                        {
                            cbnombreCtaCte.Items.Add(nombre);
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"=== CUENTAS CORRIENTES CARGADAS ===");
                    System.Diagnostics.Debug.WriteLine($"Total nombres cargados: {cbnombreCtaCte.Items.Count}");
                    foreach (string item in cbnombreCtaCte.Items)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {item}");
                    }
                    System.Diagnostics.Debug.WriteLine($"=====================================");
                }
                else
                {
                    // Si no hay nombres configurados, mostrar un mensaje informativo
                    cbnombreCtaCte.Items.Add("(No hay nombres configurados)");

                    System.Diagnostics.Debug.WriteLine("⚠️ No se encontraron nombres de cuentas corrientes en la configuración");
                }

                // Configurar el ComboBox para permitir escritura libre (autocompletado)
                cbnombreCtaCte.DropDownStyle = ComboBoxStyle.DropDown;
                cbnombreCtaCte.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                cbnombreCtaCte.AutoCompleteSource = AutoCompleteSource.ListItems;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando nombres de cuentas corrientes: {ex.Message}");

                // En caso de error, permitir entrada libre
                cbnombreCtaCte.Items.Clear();
                cbnombreCtaCte.Items.Add("(Error cargando configuración)");

                MessageBox.Show(
                    $"No se pudieron cargar los nombres de cuentas corrientes desde la configuración.\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    $"Puede escribir el nombre manualmente.",
                    "Advertencia - Configuración",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
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

            // NUEVO: Precargar nombres de cuentas corrientes al iniciar (opcional)
            // Esto permite tener los datos listos sin esperar a que se active el checkbox
            try
            {
                var nombresCuentasCorrientes = config.GetSection("CuentasCorrientes:NombresCtaCte").Get<string[]>();

                System.Diagnostics.Debug.WriteLine($"=== CONFIGURACIÓN CUENTAS CORRIENTES ===");
                System.Diagnostics.Debug.WriteLine($"Nombres disponibles: {nombresCuentasCorrientes?.Length ?? 0}");
                if (nombresCuentasCorrientes != null)
                {
                    foreach (var nombre in nombresCuentasCorrientes)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {nombre}");
                    }
                }
                System.Diagnostics.Debug.WriteLine($"========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error leyendo configuración de cuentas corrientes: {ex.Message}");
            }

            // DEBUG: Mostrar configuración cargada
            System.Diagnostics.Debug.WriteLine($"=== CONFIGURACIÓN STOCK ===");
            System.Diagnostics.Debug.WriteLine($"Validar stock habilitado: {validarStockHabilitado}");
            System.Diagnostics.Debug.WriteLine($"================================");
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
                // ELIMINADO: Manejo de la tecla F aquí - se maneja solo en el formulario principal
                // La tecla F se maneja únicamente en ConfigurarAtajosTeclado() para evitar duplicación
            };

            // MODIFICADO: Evento KeyPress - ELIMINAR completamente el manejo de 'F'
            txtBuscarProducto.KeyPress += (s, e) =>
            {
                // Permitir teclas de control (Backspace, Delete, etc.)
                if (char.IsControl(e.KeyChar))
                {
                    return; // Permitir teclas de control
                }

                // ELIMINADO: Manejo de 'F' aquí - causa duplicación de modal
                // La tecla F se maneja únicamente en ConfigurarAtajosTeclado()

                // Permitir solo números para códigos de producto
                if (!char.IsDigit(e.KeyChar))
                {
                    e.Handled = true; // Bloquear cualquier otro carácter
                }
            };

            // MODIFICADO: Configurar eventos para txtPrecio
            ConfigurarEventosPrecio();
        }

        // CORREGIDO: Implementar correctamente los eventos de txtPrecio
        private void ConfigurarEventosPrecio()
        {
            if (txtPrecio != null)
            {
                // ✅ CRÍTICO: KeyDown ANTES que KeyPress para detectar primero
                txtPrecio.KeyDown += TxtPrecio_KeyDown;

                txtPrecio.KeyPress += (s, e) =>
                {
                    TextBox textBox = s as TextBox;

                    // ✅ CRÍTICO: Bloquear TODA entrada si es del scanner
                    if (esEntradaDeScanner)
                    {
                        e.Handled = true; // Bloquear el carácter
                        System.Diagnostics.Debug.WriteLine($"⚠️ [PRECIO] Carácter '{e.KeyChar}' bloqueado (scanner detectado)");
                        return;
                    }

                    // ✅ NUEVO: Si hay texto seleccionado, permitir que se sobrescriba
                    if (textBox.SelectionLength > 0 && !char.IsControl(e.KeyChar))
                    {
                        // Eliminar el texto seleccionado primero
                        textBox.Text = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength);
                        textBox.SelectionStart = textBox.Text.Length;
                        textBox.SelectionLength = 0;
                    }

                    // Permitir teclas de control (Backspace, Delete, etc.)
                    if (char.IsControl(e.KeyChar))
                    {
                        return;
                    }

                    // Permitir el signo menos (-) SOLO al inicio del texto
                    if (e.KeyChar == '-')
                    {
                        if (textBox.SelectionStart == 0 && !textBox.Text.Contains("-"))
                        {
                            return;
                        }
                        else
                        {
                            e.Handled = true;
                            return;
                        }
                    }

                    // Permitir solo números, punto decimal y coma
                    if (!char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',')
                    {
                        e.Handled = true;
                        return;
                    }

                    // Reemplazar coma por punto para consistencia
                    if (e.KeyChar == ',')
                    {
                        e.KeyChar = '.';
                    }

                    // Permitir solo un punto decimal
                    if (e.KeyChar == '.' && textBox.Text.Contains('.'))
                    {
                        e.Handled = true;
                    }

                    // Limitar a 6 dígitos (excluyendo el punto decimal y el signo menos)
                    int digitosActuales = textBox.Text.Count(c => char.IsDigit(c));

                    if (digitosActuales >= 6 && char.IsDigit(e.KeyChar))
                    {
                        e.Handled = true;
                        System.Diagnostics.Debug.WriteLine("⚠️ Límite de 6 dígitos alcanzado en precio");
                        return;
                    }
                };

                // ✅ MODIFICADO: TextChanged - SOLO limpiar si detecta scanner Y hay contenido
                txtPrecio.TextChanged += (s, e) =>
                {
                    TextBox textBox = s as TextBox;

                    // ✅ Si detectamos scanner Y hay texto, limpiarlo SOLO UNA VEZ
                    if (esEntradaDeScanner && textBox.Text.Length > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ [PRECIO] Limpiando entrada de scanner: '{textBox.Text}'");
                        textBox.Clear();
                        return;
                    }

                    // ✅ Solo procesar si NO es entrada de scanner
                    if (esEntradaDeScanner)
                    {
                        return;
                    }
                };

                // ✅ CRÍTICO: Resetear flag SOLO cuando pierde foco
                txtPrecio.Leave += (s, e) =>
                {
                    esEntradaDeScanner = false;
                    ultimaTeclaPresionada = DateTime.MinValue;
                    System.Diagnostics.Debug.WriteLine("[PRECIO] Flag scanner reseteado al PERDER foco");
                };

                // ✅ CORREGIDO: Seleccionar todo el texto cuando obtiene foco
                txtPrecio.Enter += (s, e) =>
                {
                    TextBox textBox = s as TextBox;

                    // ✅ CRÍTICO: Usar BeginInvoke para que SelectAll se ejecute DESPUÉS de que el foco esté completamente establecido
                    textBox.BeginInvoke(new Action(() =>
                    {
                        textBox.SelectAll();
                    }));

                    System.Diagnostics.Debug.WriteLine("[PRECIO] Campo enfocado - flag scanner actual: " + esEntradaDeScanner);
                };

                // ✅ NUEVO: Agregar MouseClick para seleccionar todo al hacer clic
                txtPrecio.MouseClick += (s, e) =>
                {
                    TextBox textBox = s as TextBox;
                    if (textBox.SelectionLength == 0)
                    {
                        textBox.SelectAll();
                    }
                };
            }
        }

        // ✅ CORREGIDO: Método KeyDown - NO cambiar foco, SOLO bloquear y limpiar
        private void TxtPrecio_KeyDown(object sender, KeyEventArgs e)
        {
            DateTime ahora = DateTime.Now;
            double intervalo = (ahora - ultimaTeclaPresionada).TotalMilliseconds;

            // ✅ Inicializar timestamp en la primera tecla
            if (ultimaTeclaPresionada == DateTime.MinValue)
            {
                ultimaTeclaPresionada = ahora;
                return;
            }

            ultimaTeclaPresionada = ahora;

            // ✅ Detectar entrada rápida (scanner)
            if (intervalo > 0 && intervalo < UMBRAL_MILISEGUNDOS_SCANNER)
            {
                esEntradaDeScanner = true;
                System.Diagnostics.Debug.WriteLine($"🚨 [PRECIO] SCANNER DETECTADO - Intervalo: {intervalo:F2}ms");

                // ✅ Bloquear inmediatamente la tecla
                e.SuppressKeyPress = true;
                e.Handled = true;

                // ✅ Limpiar el campo inmediatamente
                ((TextBox)sender).Clear();
                System.Diagnostics.Debug.WriteLine("🧹 [PRECIO] Campo limpiado automáticamente");

                // ✅ Mostrar advertencia SOLO la primera vez
                if (!mensajeScannerMostrado)
                {
                    mensajeScannerMostrado = true;

                    Task.Run(async () =>
                    {
                        await Task.Delay(100);

                        if (this.IsHandleCreated && !this.IsDisposed)
                        {
                            this.Invoke(new Action(() =>
                            {
                                MessageBox.Show(
                                    "⚠️ ENTRADA BLOQUEADA\n\n" +
                                    "No se permite escanear códigos de barras en el campo de precio.\n\n" +
                                    "Por favor, ingrese el precio manualmente con el teclado.",
                                    "Scanner Detectado",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);

                                mensajeScannerMostrado = false;
                            }));
                        }
                    });
                }

                return;
            }
            else if (intervalo >= UMBRAL_MILISEGUNDOS_SCANNER)
            {
                esEntradaDeScanner = false;
                System.Diagnostics.Debug.WriteLine($"✅ [PRECIO] Teclado manual - Intervalo: {intervalo:F2}ms");
            }

            // ✅ CAMBIO CRÍTICO: Manejar Enter con ejecución directa y devolución de foco
            if (e.KeyCode == Keys.Enter && !esEntradaDeScanner)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;

                if (txtPrecio.Enabled && !string.IsNullOrWhiteSpace(txtPrecio.Text))
                {
                    // ✅ EJECUTAR el botón Agregar
                    btnAgregar.PerformClick();

                    // ✅ DEVOLVER inmediatamente el foco a txtBuscarProducto
                    // Usar BeginInvoke para que se ejecute después del PerformClick
                    this.BeginInvoke(new Action(() =>
                    {
                        txtBuscarProducto.Focus();
                        txtBuscarProducto.SelectAll();
                    }));
                }
                else
                {
                    // Si el campo está vacío o deshabilitado, ir al siguiente control
                    this.SelectNextControl(txtPrecio, true, true, true, true);
                }
            }
        }

        // NUEVO: Implementar método ConfigurarTextBoxes
        private void ConfigurarTextBoxes()
        {
            if (txtBuscarProducto != null)
            {
                txtBuscarProducto.Font = new Font("Segoe UI", 11F);
                txtBuscarProducto.BackColor = Color.White;
                txtBuscarProducto.ForeColor = Color.Black;
            }

            if (txtPrecio != null)
            {
                txtPrecio.Font = new Font("Segoe UI", 11F);
                txtPrecio.BackColor = Color.White;
                txtPrecio.ForeColor = Color.Black;
            }
        }

        // NUEVO: Implementar método GetConnectionString
        private string GetConnectionString()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return config.GetConnectionString("DefaultConnection");
        }

        // NUEVO: Implementar método ProcesarCodigo
        private (string codigoBuscado, decimal? precioPersonalizado, bool esEspecial) ProcesarCodigo(string textoIngresado)
        {
            if (textoIngresado.StartsWith("50") && textoIngresado.Length == 13)
            {
                // Código especial de balanza
                // ✅ CORREGIDO: Extraer desde posición 0 para incluir el "50"
                string codigoProducto = textoIngresado.Substring(0, 7); // Posiciones 0-6 = "50" + 5 dígitos
                codigoProducto = codigoProducto.TrimStart('0');
                if (string.IsNullOrEmpty(codigoProducto))
                    codigoProducto = "0";

                // El precio sigue estando en las posiciones 7-11
                string parteEntera = textoIngresado.Substring(7, 5);
                decimal precio = decimal.Parse(parteEntera);

                return (codigoProducto, precio, true);
            }
            else
            {
                // Código normal
                string codigo = textoIngresado.TrimStart('0');
                if (string.IsNullOrEmpty(codigo))
                    codigo = "0";

                return (codigo, null, false);
            }
        }

        // NUEVO: Implementar método CalcularIvaDesdeTotal
        private decimal CalcularIvaDesdeTotal(decimal totalConIva, decimal porcentajeIva)
        {
            if (porcentajeIva <= 0)
                return 0;

            return (totalConIva * porcentajeIva) / (100 + porcentajeIva);
        }

        // CORREGIDO: Implementar método ConfigurarEventosDataGridView con todos los eventos necesarios
        private void ConfigurarEventosDataGridView()
        {
            if (dataGridView1 != null)
            {
                // Evento de tecla Delete para eliminar
                dataGridView1.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Delete)
                    {
                        // Usar el método con auditoría
                        _ = EliminarProductoConAuditoria();
                    }
                };

                // RESTAURADO: Evento de doble click para editar cantidad
                dataGridView1.CellDoubleClick += async (s, e) =>
                {
                    if (e.RowIndex >= 0 // Verificar que no sea el header
                    && dataGridView1.SelectedRows.Count > 0)
                    {
                        dataGridView1.Rows[e.RowIndex].Selected = true;
                        await EditarCantidadProductoSeleccionado();
                    }
                };

                // RESTAURADO: Evento de click derecho para menú contextual
                dataGridView1.MouseClick += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        var hit = dataGridView1.HitTest(e.X, e.Y);
                        if (hit.RowIndex >= 0)
                        {
                            // Seleccionar la fila clickeada
                            dataGridView1.ClearSelection();
                            dataGridView1.Rows[hit.RowIndex].Selected = true;

                            // Mostrar el menú contextual
                            contextMenuGrilla.Show(dataGridView1, e.Location);
                        }
                    }
                };

                // Evento de selección de fila para actualizar estado del menú
                dataGridView1.SelectionChanged += (s, e) =>
                {
                    bool haySeleccion = dataGridView1.SelectedRows.Count > 0;
                    if (menuEditarCantidad != null)
                        menuEditarCantidad.Enabled = haySeleccion;
                    if (menuEliminarProducto != null)
                        menuEliminarProducto.Enabled = haySeleccion;
                    if (menuInfoProducto != null)
                        menuInfoProducto.Enabled = haySeleccion;
                };
            }
        }

        // NUEVO: Implementar método EliminarProductoSeleccionado
        private void EliminarProductoSeleccionado()
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                var row = dataGridView1.SelectedRows[0];

                // MODIFICADO: Obtener el ID único en lugar del código
                if (!int.TryParse(row.Cells["id"].Value?.ToString(), out int idVenta))
                {
                    MessageBox.Show("Error: No se pudo obtener el ID de la venta.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var codigo = row.Cells["codigo"].Value?.ToString();
                var descripcion = row.Cells["descripcion"].Value?.ToString();

                if (!string.IsNullOrEmpty(codigo))
                {
                    var resultado = MessageBox.Show(
                        $"¿Está seguro de eliminar el producto:\n{descripcion}?",
                        "Confirmar eliminación",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (resultado == DialogResult.Yes)
                    {
                        // CORREGIDO: Pasar el código pero el método usará el ID internamente
                        EliminarProductoDeVenta(codigo);
                    }
                }
            }
        }

        // NUEVO: Implementar método EliminarProductoDeVenta
        private async void EliminarProductoDeVenta(string codigo)
        {
            try
            {
                if (dataGridView1.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Seleccione un producto para eliminar.", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var row = dataGridView1.SelectedRows[0];

                // MODIFICADO: Obtener el ID único de la fila seleccionada
                if (!int.TryParse(row.Cells["id"].Value?.ToString(), out int idVenta))
                {
                    MessageBox.Show("Error: No se pudo obtener el ID de la venta.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var descripcion = row.Cells["descripcion"].Value?.ToString();

                var resultado = MessageBox.Show(
                    $"¿Está seguro de eliminar el producto:\n{descripcion}?",
                    "Confirmar eliminación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resultado != DialogResult.Yes)
                    return;

                string connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    // CORREGIDO: Usar el ID único en lugar del código + nrofactura
                    var query = "DELETE FROM Ventas WHERE id = @idVenta";
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@idVenta", idVenta);

                        connection.Open();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                CargarVentasActuales();

                System.Diagnostics.Debug.WriteLine($"Producto eliminado - ID: {idVenta}, Código: {codigo}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar producto: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NUEVO: Implementar método chkCantidad_CheckedChanged
        private void chkCantidad_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCantidad.Checked)
            {
                // MODERNIZADO: Usar el EditarCantidadDialog moderno en lugar del InputBox básico
                using (var dialog = new EditarCantidadDialog("", "Cantidad personalizada", cantidadPersonalizada))
                {
                    var resultado = dialog.ShowDialog(this);

                    if (resultado == DialogResult.OK && dialog.Confirmado)
                    {
                        int nuevaCantidad = dialog.NuevaCantidad;
                        if (nuevaCantidad > 0)
                        {
                            cantidadPersonalizada = nuevaCantidad;
                            System.Diagnostics.Debug.WriteLine($"Cantidad personalizada establecida: {cantidadPersonalizada}");
                        }
                        else
                        {
                            chkCantidad.Checked = false;
                            cantidadPersonalizada = 1;
                        }
                    }
                    else
                    {
                        // Usuario canceló o no confirmó, desmarcar checkbox
                        chkCantidad.Checked = false;
                        cantidadPersonalizada = 1;
                    }
                }
            }
            else
            {
                cantidadPersonalizada = 1;
            }
        }

        // NUEVO: Implementar método cbnombreCtaCte_SelectedIndexChanged
        private void cbnombreCtaCte_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Lógica para manejar cambio de cuenta corriente
        }

        // NUEVO: Implementar método FormatearDataGridView
        private void FormatearDataGridView()
        {
            // ✅✅✅ CRÍTICO: Agregar columna de oferta COMO PRIMERA COLUMNA (MUY ANGOSTA)
            if (dataGridView1.Columns["ColOferta"] == null)
            {
                var colOferta = new DataGridViewTextBoxColumn
                {
                    Name = "ColOferta",
                    HeaderText = "🎁",
                    Width = 35,
                    MinimumWidth = 30,
                    ReadOnly = true,
                    Frozen = false,
                    Resizable = DataGridViewTriState.False,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        Font = new Font("Segoe UI Emoji", 18F, FontStyle.Regular), // ✅ AUMENTADO: 14F → 18F
                        ForeColor = Color.Green,
                        Padding = new Padding(0)
                    }
                };

                dataGridView1.Columns.Insert(0, colOferta);
                System.Diagnostics.Debug.WriteLine("✅ Columna ColOferta creada como PRIMERA columna (35px)");
            }

            // ✅✅✅ CÓDIGO: Fuente más grande
            if (dataGridView1.Columns["codigo"] != null)
            {
                var colCodigo = dataGridView1.Columns["codigo"];
                colCodigo.DefaultCellStyle.Font = new Font("Segoe UI", 16F, FontStyle.Regular); // ✅ AUMENTADO: 12F → 16F
                colCodigo.DefaultCellStyle.ForeColor = Color.FromArgb(33, 33, 33);
                colCodigo.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }

            // ✅✅✅ DESCRIPCIÓN: Fuente más grande y negrita
            if (dataGridView1.Columns["descripcion"] != null)
            {
                var colDescripcion = dataGridView1.Columns["descripcion"];
                colDescripcion.DefaultCellStyle.Font = new Font("Segoe UI", 17F, FontStyle.Bold); // ✅ AUMENTADO: 13F → 17F
                colDescripcion.DefaultCellStyle.ForeColor = Color.FromArgb(33, 33, 33);
                colDescripcion.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                colDescripcion.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                colDescripcion.FillWeight = 250;

                System.Diagnostics.Debug.WriteLine("✅ Columna descripción configurada: 17pt Bold, FillWeight=250");
            }

            // ✅✅✅ PRECIO: Fuente más grande
            if (dataGridView1.Columns["precio"] != null)
            {
                dataGridView1.Columns["precio"].DefaultCellStyle.Format = "C2";
                dataGridView1.Columns["precio"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dataGridView1.Columns["precio"].DefaultCellStyle.Font = new Font("Segoe UI", 16F, FontStyle.Regular); // ✅ AUMENTADO: 12F → 16F
            }

            // ✅✅✅ CANTIDAD: Fuente más grande
            if (dataGridView1.Columns["cantidad"] != null)
            {
                dataGridView1.Columns["cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dataGridView1.Columns["cantidad"].DefaultCellStyle.Font = new Font("Segoe UI", 16F, FontStyle.Regular); // ✅ AUMENTADO: 12F → 16F
            }

            // ✅✅✅ TOTAL: Fuente más grande y negrita
            if (dataGridView1.Columns["total"] != null)
            {
                dataGridView1.Columns["total"].DefaultCellStyle.Format = "C2";
                dataGridView1.Columns["total"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dataGridView1.Columns["total"].DefaultCellStyle.Font = new Font("Segoe UI", 16F, FontStyle.Bold); // ✅ AUMENTADO: 12F → 16F
            }

            // ✅✅✅ IVA%: Fuente más grande
            if (dataGridView1.Columns["PorcentajeIva"] != null)
            {
                dataGridView1.Columns["PorcentajeIva"].DefaultCellStyle.Font = new Font("Segoe UI", 16F, FontStyle.Regular); // ✅ AUMENTADO: 12F → 16F
            }

            // ✅✅✅ OCULTAR columna IvaCalculado (IVA $)
            if (dataGridView1.Columns["IvaCalculado"] != null)
            {
                dataGridView1.Columns["IvaCalculado"].Visible = false;
                System.Diagnostics.Debug.WriteLine("✅ Columna IvaCalculado OCULTADA");
            }

            System.Diagnostics.Debug.WriteLine($"📊 Procesando {dataGridView1.Rows.Count} filas en FormatearDataGridView");

            // ✅ CORREGIDO: Recorrer las filas SOLO para marcar ofertas (sin modificar fuentes)
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                if (row.Cells["EsOferta"] == null)
                {
                    continue;
                }

                var valorEsOferta = row.Cells["EsOferta"].Value;

                if (valorEsOferta != null && valorEsOferta != DBNull.Value)
                {
                    bool esOferta = false;

                    if (valorEsOferta is bool boolValue)
                    {
                        esOferta = boolValue;
                    }
                    else if (valorEsOferta is int intValue)
                    {
                        esOferta = intValue == 1;
                    }
                    else if (int.TryParse(valorEsOferta.ToString(), out int parsedValue))
                    {
                        esOferta = parsedValue == 1;
                    }

                    if (esOferta)
                    {
                        if (row.Cells["ColOferta"] != null)
                        {
                            row.Cells["ColOferta"].Value = "🎁";
                            row.Cells["ColOferta"].Style.ForeColor = Color.Green;
                            row.DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);

                            // ✅ MODIFICADO: Solo cambiar el color de la descripción, NO la fuente
                            // La fuente Bold ya está configurada a nivel de columna
                            if (row.Cells["descripcion"] != null)
                            {
                                row.Cells["descripcion"].Style.ForeColor = Color.FromArgb(0, 100, 0);
                            }
                        }
                    }
                    else
                    {
                        if (row.Cells["ColOferta"] != null)
                        {
                            row.Cells["ColOferta"].Value = "";
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");
        }

        // NUEVO: Implementar método ObtenerUsuarioActual
        private string ObtenerUsuarioActual()
        {
            return AuthenticationService.SesionActual?.Usuario?.NombreUsuario ?? "Sistema";
        }

        // NUEVO: Implementar método obtenerNumeroCajero
        private int obtenerNumeroCajero()
        {
            return AuthenticationService.SesionActual?.Usuario?.NumeroCajero ?? 1;
        }

        // NUEVO: Implementar método FormatearNumeroFacturaParaBD
        private string FormatearNumeroFacturaParaBD(int tipoComprobante, int puntoVenta, int numeroFactura)
        {
            return $"{tipoComprobante:D4}-{puntoVenta:D8}-{numeroFactura:D8}";
        }

        // NUEVO: Implementar método LimpiarYReiniciarVenta
        private void LimpiarYReiniciarVenta()
        {
            dataGridView1.DataSource = null;
            dataGridView1.Rows.Clear();
            remitoActual = null;
            remitoIncrementado = false;

            lbCantidadProductos.Text = "Productos: 0";

            if (rtbTotal != null)
            {
                rtbTotal.Clear();
                rtbTotal.SelectionAlignment = HorizontalAlignment.Right;
                rtbTotal.SelectionFont = new Font("Segoe UI", 24F, FontStyle.Bold);
                rtbTotal.AppendText("TOTAL: $0,00");
            }

            if (btnAnularFactura != null)
            {
                btnAnularFactura.Enabled = false;
                btnAnularFactura.Visible = true;
            }

            if (chkEsCtaCte != null)
            {
                chkEsCtaCte.Checked = false;
                cbnombreCtaCte.Visible = false;
            }

            txtBuscarProducto.Text = "";
            txtBuscarProducto.Focus();
        }

        // NUEVO: Implementar método AbrirFormularioAgregarProductoRapido
        private async Task AbrirFormularioAgregarProductoRapido(string codigo, decimal? precio)
        {
            using (var form = new frmAgregarProducto())
            {
                // Pre-cargar código y precio si se proporcionan
                form.PrecargarDatos(codigo, precio);
                var resultado = form.ShowDialog(this);

                if (resultado == DialogResult.OK)
                {
                    // Producto agregado exitosamente, continuar con la venta
                    System.Diagnostics.Debug.WriteLine($"{DateTime.Now} - Producto {codigo} agregado correctamente");
                }
            }
        }

        private async Task ActualizarPrecioProductoAsync(string codigo, decimal nuevoPrecio)
        {
            try
            {
                string connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    // ✅ CRÍTICO: Actualizar el precio SOLO para productos con EditarPrecio = true
                    var query = @"UPDATE Productos 
                          SET precio = @precio 
                          WHERE codigo = @codigo 
                          AND EditarPrecio = 1";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@precio", nuevoPrecio);
                        cmd.Parameters.AddWithValue("@codigo", codigo);

                        await connection.OpenAsync();
                        int filasAfectadas = await cmd.ExecuteNonQueryAsync();

                        if (filasAfectadas > 0)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"✅ Precio actualizado en BD:\n" +
                                $"   Código: {codigo}\n" +
                                $"   Nuevo precio: {nuevoPrecio:C2}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"⚠️ No se actualizó el precio (producto sin EditarPrecio o no encontrado):\n" +
                                $"   Código: {codigo}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Error silencioso para evitar interrumpir la experiencia del usuario
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando precio: {ex.Message}");
            }
        }

        private void AbrirConsultaRapidaPrecios()
        {
            using (var consultaForm = new ConsultaPrecioForm())
            {
                consultaForm.ShowDialog(this);
            }
        }

        private async Task AbrirFormularioComprasAsync()
        {
            await Task.Yield(); // asegura regresar al hilo de UI en llamadas "fire-and-forget"

            try
            {
                using (var frm = new ComprasProveedorForm())
                {
                    frm.StartPosition = FormStartPosition.CenterParent;
                    frm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error abriendo ComprasProveedorForm: {ex.Message}");
                MessageBox.Show($"No se pudo abrir el formulario de Compras: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ✅ NUEVO: Método helper para finalizar venta con foco específico
        private async void FinalizarVentaConFocoEn(SeleccionImpresionForm.BotonInicial botonInicial)
        {
            try
            {
                if (remitoActual == null || remitoActual.Rows.Count == 0)
                {
                    MessageBox.Show("No hay productos en la venta para finalizar.", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // ✅ CORREGIDO: Calcular el total usando el método existente
                decimal importeTotal = CalcularTotal();

                System.Diagnostics.Debug.WriteLine($"[VENTAS] Iniciando finalización de venta con total: {importeTotal:C2}, Foco: {botonInicial}");

                using (var seleccionModal = new SeleccionImpresionForm(importeTotal, this, botonInicial))
                {
                    // ✅ CRÍTICO: Configurar el callback ANTES de mostrar el modal
                    // ✅✅✅ CORREGIDO: Agregar los parámetros de descuento
                    seleccionModal.OnProcesarVenta = async (tipoComprobante, formaPago, cuitCliente,
                        caeNumero, caeVencimiento, numeroFacturaAfip, numeroFormateado,
                        porcentajeDescuento, importeDescuento) => // ✅ AGREGADOS: últimos 2 parámetros
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"[VENTAS] OnProcesarVenta - Tipo: {tipoComprobante}, FormaPago: {formaPago}");

                            System.Diagnostics.Debug.WriteLine($"[DESCUENTO CAPTURADO] Porcentaje: {porcentajeDescuento}%");
                            System.Diagnostics.Debug.WriteLine($"[DESCUENTO CAPTURADO] Importe: {importeDescuento:C2}");

                            // Guardar en BD
                            await GuardarFacturaEnBD(
                                tipoComprobante,
                                formaPago,
                                cuitCliente,
                                caeNumero,
                                caeVencimiento,
                                numeroFacturaAfip,
                                numeroFormateado,
                                seleccionModal.EsPagoMultiple ? seleccionModal.PagosMultiples : null,
                                porcentajeDescuento,    // ✅ Pasar descuento
                                importeDescuento        // ✅ Pasar descuento
                            );

                            System.Diagnostics.Debug.WriteLine("[VENTAS] ✅ Factura guardada en BD exitosamente");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[VENTAS] ❌ Error en OnProcesarVenta: {ex.Message}");
                            throw;
                        }
                    };

                    var resultado = seleccionModal.ShowDialog();

                    if (resultado == DialogResult.OK)
                    {
                        System.Diagnostics.Debug.WriteLine($"[VENTAS] ✅ Venta finalizada exitosamente - Opción: {seleccionModal.OpcionSeleccionada}");
                        LimpiarYReiniciarVenta();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[VENTAS] ⚠️ Venta cancelada por el usuario");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VENTAS] ❌ Error crítico en FinalizarVentaConFocoEn: {ex.Message}");
                MessageBox.Show($"Error al finalizar la venta:\n\n{ex.Message}", "Error Crítico",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                else if (e.KeyCode == Keys.F8)
                {
                    e.SuppressKeyPress = true;
                    _ = AbrirFormularioComprasAsync();
                }
                else if (e.KeyCode == Keys.F11)
                {
                    e.SuppressKeyPress = true;

                    // NUEVO: Limpiar el txtBuscarProducto antes de finalizar venta
                    txtBuscarProducto.Text = "";

                    // ✅ NUEVO: Finalizar con foco en "Finalizar sin impresión"
                    FinalizarVentaConFocoEn(SeleccionImpresionForm.BotonInicial.FinalizarSinImpresion);
                }
                else if (e.KeyCode == Keys.F || e.KeyCode == Keys.F12)
                {
                    e.SuppressKeyPress = true;

                    // NUEVO: Limpiar el txtBuscarProducto antes de finalizar venta
                    txtBuscarProducto.Text = "";

                    // ✅ MODIFICADO: Finalizar con foco en "Remito" (comportamiento actual)
                    FinalizarVentaConFocoEn(SeleccionImpresionForm.BotonInicial.Remito);
                }
            };
        }

        private void ConfigurarCheckboxCantidad()
        {
            // CheckBox para cantidad personalizada
            chkCantidad = new CheckBox
            {
                Text = "Cantidad",
                Left = 600, // Más a la derecha, separado del chkEsCtaCte
                Top = 135,  // Misma altura que chkEsCtaCte
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
            ConfigurarBotonesCaja();
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

            // Mantener el footer como antes
            ConfigurarPanelFooter();

            // ✅ CORREGIDO: Crear botón "Anular" DESPUÉS de crear btnRetirarEfectivo
            btnAnularFactura = new Button
            {
                Text = "Anular",
                Size = new Size(75, 35),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Enabled = false,
                Visible = true,
                TabStop = false
            };
            btnAnularFactura.FlatAppearance.BorderSize = 0;
            btnAnularFactura.Click += BtnEliminarFacturaCompleta_Click;

            // ✅ Agregar al formulario
            this.Controls.Add(btnAnularFactura);
            btnAnularFactura.BringToFront();

            // ✅ CORREGIDO: Posicionar junto a btnRetirarEfectivo (NO btnFinalizarVenta)
            void ReposicionarAnular()
            {
                try
                {
                    // ✅ OPCIÓN 1: Posicionar junto a btnRetirarEfectivo (PRIMERO)
                    if (btnRetirarEfectivo != null && btnRetirarEfectivo.Visible)
                    {
                        btnAnularFactura.Height = btnRetirarEfectivo.Height;
                        btnAnularFactura.Left = btnRetirarEfectivo.Right + 15;
                        btnAnularFactura.Top = btnRetirarEfectivo.Top;
                        return;
                    }

                    // ✅ OPCIÓN 2: Fallback - junto a btnFinalizarVenta
                    if (btnFinalizarVenta != null && btnFinalizarVenta.Visible)
                    {
                        btnAnularFactura.Height = btnFinalizarVenta.Height;
                        btnAnularFactura.Left = btnFinalizarVenta.Right + 15;
                        btnAnularFactura.Top = btnFinalizarVenta.Top;
                        return;
                    }

                    // ✅ OPCIÓN 3: Fallback - junto a btnAgregar
                    if (btnAgregar != null && btnAgregar.Visible)
                    {
                        btnAnularFactura.Height = btnAgregar.Height;
                        btnAnularFactura.Left = btnAgregar.Right + 15;
                        btnAnularFactura.Top = btnAgregar.Top;
                        return;
                    }

                    // ✅ OPCIÓN 4: Posición fija
                    btnAnularFactura.Left = 950;
                    btnAnularFactura.Top = 115;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error posicionando botón Anular: {ex.Message}");
                }
            }

            // ✅ Ejecutar posicionamiento en diferentes eventos
            this.Load += (s, e) =>
            {
                ReposicionarAnular();
                btnAnularFactura.BringToFront();
            };

            this.Resize += (s, e) => ReposicionarAnular();

            // ✅ CRÍTICO: Reposicionar cuando cambien los botones de referencia
            if (btnRetirarEfectivo != null)
            {
                btnRetirarEfectivo.VisibleChanged += (s, e) => ReposicionarAnular();
            }

            if (btnFinalizarVenta != null)
            {
                btnFinalizarVenta.VisibleChanged += (s, e) => ReposicionarAnular();
            }

            if (btnAgregar != null)
            {
                btnAgregar.VisibleChanged += (s, e) => ReposicionarAnular();
            }

            // Asegurar que el título no tape controles
            lblTitulo.SendToBack();
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

            // ✅ MODIFICADO: NO establecer Font aquí - se configura por columna
            dataGridView1.DefaultCellStyle.BackColor = Color.White;
            dataGridView1.DefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.White;

            // ✅ Fuente para encabezados - MÁS GRANDE
            var headerStyle = dataGridView1.ColumnHeadersDefaultCellStyle;
            headerStyle.Font = new Font("Segoe UI", 17F, FontStyle.Bold); // ✅ AUMENTADO: 13F → 17F
            headerStyle.BackColor = Color.FromArgb(248, 249, 250);
            headerStyle.ForeColor = Color.Black;

            // ✅ MODIFICADO: NO establecer Font en filas alternadas - heredará de las columnas
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 245, 250);
            dataGridView1.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dataGridView1.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

            dataGridView1.RowTemplate.Height = 48; // ✅ AUMENTADO: 36 → 48 (más alto para fuentes grandes)
            dataGridView1.GridColor = Color.FromArgb(220, 220, 220);

            dataGridView1.AutoGenerateColumns = true;
            dataGridView1.ColumnAdded += (s, e) =>
            {
                if (e.Column.Name == "codigo")
                {
                    e.Column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    e.Column.Width = 150;
                }
            };
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // ✅ NUEVO: Event handler para retiros de efectivo
        private async void BtnRetirarEfectivo_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dialogoRetiro = new RetiroEfectivoForm())
                {
                    var resultado = dialogoRetiro.ShowDialog(this);

                    if (resultado == DialogResult.OK && dialogoRetiro.Confirmado)
                    {
                        await RegistrarRetiroEfectivo(
                            dialogoRetiro.Monto,
                            dialogoRetiro.Motivo,
                            dialogoRetiro.Responsable);

                        MessageBox.Show(
                            $"✅ RETIRO REGISTRADO\n\n" +
                            $"Monto: {dialogoRetiro.Monto:C2}\n" +
                            $"Motivo: {dialogoRetiro.Motivo}\n" +
                            $"Responsable: {dialogoRetiro.Responsable}\n\n" +
                            $"El retiro se considerará en el cierre de caja.",
                            "Retiro de Efectivo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al registrar el retiro: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ✅ NUEVO: Registrar retiro en base de datos
        private async Task RegistrarRetiroEfectivo(decimal monto, string motivo, string responsable)
        {
            string connectionString = GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"
            INSERT INTO RetirosEfectivo 
                (Monto, Motivo, Responsable, NumeroCajero, UsuarioRegistro, 
                 FechaRetiro, NumeroRemito, NombreEquipo)
            VALUES 
                (@Monto, @Motivo, @Responsable, @NumeroCajero, @UsuarioRegistro,
                 @FechaRetiro, @NumeroRemito, @NombreEquipo)";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Monto", monto);
                    cmd.Parameters.AddWithValue("@Motivo", motivo);
                    cmd.Parameters.AddWithValue("@Responsable", responsable);
                    cmd.Parameters.AddWithValue("@NumeroCajero", obtenerNumeroCajero());
                    cmd.Parameters.AddWithValue("@UsuarioRegistro", ObtenerUsuarioActual());
                    cmd.Parameters.AddWithValue("@FechaRetiro", DateTime.Now);
                    cmd.Parameters.AddWithValue("@NumeroRemito", (object)nroRemitoActual ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NombreEquipo", Environment.MachineName);

                    await connection.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"💰 Retiro registrado - Monto: {monto:C2}, Motivo: {motivo}, Responsable: {responsable}");
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

        private async Task MostrarProductoAsync(string codigo, decimal? precioPersonalizado, bool esSpecial)
        {
            var producto = await BuscarProductoAsync(codigo);

            if (producto == null)
            {
                // MEJORADO: Mensaje más claro para el usuario
                lbDescripcionProducto.Text = $"?? Producto '{codigo}' no encontrado - Presione 'Agregar' para crearlo";
                LimpiarCamposProducto();
                return;
            }

            lbDescripcionProducto.Text = producto["descripcion"].ToString();
            bool editarPrecio = producto.Table.Columns.Contains("EditarPrecio") &&
                                producto["EditarPrecio"] != DBNull.Value &&
                                Convert.ToBoolean(producto["EditarPrecio"]);

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

            // Si el producto permite editar precio y el precio cargado difiere del de la BD,
            // actualizar la tabla Productos con el nuevo precio (solo para productos con EditarPrecio = true).
            if (editarPrecio)
            {
                try
                {
                    decimal precioActualEnBD = Convert.ToDecimal(producto["precio"]);
                    if (decimal.TryParse(txtPrecio.Text.Replace(".", ","), NumberStyles.Number, CultureInfo.CurrentCulture, out decimal precioMostrado))
                    {
                        // ✅ MEJORADO: Comparar con tolerancia de 0.01 centavos
                        if (Math.Abs(precioMostrado - precioActualEnBD) > 0.01m)
                        {
                            // Actualizar de forma asíncrona y no bloquear la UI si hay algún error
                            await ActualizarPrecioProductoAsync(producto["codigo"].ToString(), precioMostrado);
                            System.Diagnostics.Debug.WriteLine(
                                $"💾 Precio actualizado en BD al cargar producto:\n" +
                                $"   Código: {producto["codigo"]}\n" +
                                $"   Precio anterior: {precioActualEnBD:C2}\n" +
                                $"   Precio nuevo: {precioMostrado:C2}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error actualizando precio al cargar producto: {ex.Message}");
                    // No interrumpir al usuario por un fallo al persistir el precio
                }
            }
        }

        // CORREGIDO: Cambiar el tipo de retorno a Task<DataRow>
        private async Task<DataRow> BuscarProductoAsync(string codigo)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            string connectionString = config.GetConnectionString("DefaultConnection");

            using var connection = new SqlConnection(connectionString);

            // ✅ MODIFICADO: Agregar validación del campo Activo
            var query = @"SELECT codigo, descripcion, precio, cantidad, marca, rubro, costo, proveedor, 
                         CAST(ISNULL(Activo, 1) AS BIT) as Activo,
                         CAST(ISNULL(PermiteAcumular, 0) AS BIT) as PermiteAcumular,
                         CAST(ISNULL(EditarPrecio, 0) AS BIT) as EditarPrecio
                  FROM Productos 
                  WHERE codigo = @codigo AND ISNULL(Activo, 1) = 1";  // ✅ VALIDAR QUE ESTÉ ACTIVO

            using var adapter = new SqlDataAdapter(query, connection);
            adapter.SelectCommand.Parameters.AddWithValue("@codigo", codigo);

            var dt = new DataTable();
            adapter.Fill(dt);

            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0];
            }
            else
            {
                // ✅ NUEVO: Verificar si el producto existe pero está inactivo
                var queryInactivo = @"SELECT codigo, descripcion 
                              FROM Productos 
                              WHERE codigo = @codigo AND ISNULL(Activo, 1) = 0";

                using var adapterInactivo = new SqlDataAdapter(queryInactivo, connection);
                adapterInactivo.SelectCommand.Parameters.AddWithValue("@codigo", codigo);

                var dtInactivo = new DataTable();
                adapterInactivo.Fill(dtInactivo);

                if (dtInactivo.Rows.Count > 0)
                {
                    string descripcion = dtInactivo.Rows[0]["descripcion"]?.ToString() ?? "Sin descripción";
                    MessageBox.Show(
                        $"⚠️ El producto '{descripcion}' (código: {codigo}) está INACTIVO.\n\n" +
                        "No se puede agregar a la venta.\n\n" +
                        "Si necesita activarlo, vaya al módulo de Productos.",
                        "Producto Inactivo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }

                return null;
            }
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

            if (string.IsNullOrEmpty(textoIngresado))
            {
                MessageBox.Show("Ingrese un código de producto válido.");
                txtBuscarProducto.Focus();
                return;
            }

            var resultadoCodigo = ProcesarCodigo(textoIngresado);
            string codigoBuscado = resultadoCodigo.codigoBuscado;
            decimal? precioPersonalizado = resultadoCodigo.precioPersonalizado; // ✅ Precio del código de barras
            bool esCodigoEspecial = resultadoCodigo.esEspecial;

            bool esCodigoTemporal = textoIngresado.Length == 8;

            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            string connectionString = config.GetConnectionString("DefaultConnection");

            DataRow producto = null;
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"SELECT codigo, descripcion, precio, rubro, marca, proveedor, costo, PermiteAcumular, cantidad, EditarPrecio, iva 
                      FROM Productos WHERE codigo = @codigo";
                using (var adapter = new SqlDataAdapter(query, connection))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@codigo", codigoBuscado);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count == 0)
                    {
                        var resultadoMsg = MessageBox.Show(
                            $"El producto con código '{codigoBuscado}' no existe.\n\n" +
                            "¿Desea agregarlo ahora para continuar con la venta?",
                            "Producto no encontrado",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (resultadoMsg == DialogResult.Yes)
                        {
                            await AbrirFormularioAgregarProductoRapido(codigoBuscado, precioPersonalizado);
                            return;
                        }
                        else
                        {
                            txtBuscarProducto.Focus();
                            return;
                        }
                    }
                    producto = dt.Rows[0];
                }
            }

            bool permiteAcumular = producto["PermiteAcumular"] != DBNull.Value && Convert.ToBoolean(producto["PermiteAcumular"]);
            int stockDisponible = Convert.ToInt32(producto["cantidad"]);

            if (permiteAcumular && validarStockHabilitado && stockDisponible < cantidadPersonalizada)
            {
                var resultadoStock = MessageBox.Show(
                    $"ADVERTENCIA: Stock insuficiente\n\n" +
                    $"Producto: {producto["descripcion"]}\n" +
                    $"Stock disponible: {stockDisponible}\n" +
                    $"Cantidad solicitada: {cantidadPersonalizada}\n\n" +
                    "¿Desea continuar de todas formas?\n" +
                    "(El stock quedará en negativo)",
                    "Stock Insuficiente",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (resultadoStock != DialogResult.Yes)
                {
                    txtBuscarProducto.Focus();
                    return;
                }
            }

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

            // ✅✅✅ CRÍTICO: Determinar el precio a usar - MODIFICADO
            decimal precioUnitario;
            bool editarPrecio = producto["EditarPrecio"] != DBNull.Value && Convert.ToBoolean(producto["EditarPrecio"]);

            // ✅✅✅ PRIORIDAD 1: Si viene de un código especial (balanza), usar ese precio SIEMPRE
            if (precioPersonalizado.HasValue && precioPersonalizado.Value > 0)
            {
                precioUnitario = precioPersonalizado.Value;

                System.Diagnostics.Debug.WriteLine(
                    $"✅ PRECIO DE BALANZA APLICADO:\n" +
                    $"   Producto: {producto["descripcion"]}\n" +
                    $"   Código escaneado: {textoIngresado}\n" +
                    $"   Precio BD: {Convert.ToDecimal(producto["precio"]):C2}\n" +
                    $"   Precio balanza: {precioUnitario:C2}");
            }
            // ✅ PRIORIDAD 2: Si el producto permite editar precio Y hay un precio válido en txtPrecio
            else if (editarPrecio && !string.IsNullOrWhiteSpace(txtPrecio.Text))
            {
                if (decimal.TryParse(txtPrecio.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal precioManual))
                {
                    precioUnitario = precioManual;

                    System.Diagnostics.Debug.WriteLine(
                        $"✅ PRECIO MANUAL APLICADO:\n" +
                        $"   Producto: {producto["descripcion"]}\n" +
                        $"   Precio BD: {Convert.ToDecimal(producto["precio"]):C2}\n" +
                        $"   Precio manual: {precioUnitario:C2}");

                    // Guardar el precio manual en la BD para la próxima vez
                    decimal precioOriginalBD = Convert.ToDecimal(producto["precio"]);
                    if (Math.Abs(precioManual - precioOriginalBD) > 0.01m)
                    {
                        await ActualizarPrecioProductoAsync(codigoBuscado, precioManual);
                        System.Diagnostics.Debug.WriteLine(
                            $"💾 Precio guardado en BD para próximas ventas:\n" +
                            $"   Código: {codigoBuscado}\n" +
                            $"   Precio anterior: {precioOriginalBD:C2}\n" +
                            $"   Precio nuevo: {precioManual:C2}");
                    }
                }
                else
                {
                    // Si no se pudo parsear, usar el precio de la BD
                    precioUnitario = Convert.ToDecimal(producto["precio"]);
                    System.Diagnostics.Debug.WriteLine($"⚠️ No se pudo parsear txtPrecio, usando precio BD: {precioUnitario:C2}");
                }
            }
            // ✅ PRIORIDAD 3: Usar precio de la BD
            else
            {
                precioUnitario = Convert.ToDecimal(producto["precio"]);
                System.Diagnostics.Debug.WriteLine($"📋 Usando precio de BD: {precioUnitario:C2}");
            }

            // ✅ PASO 1: Verificar si el producto tiene oferta activa
            // ⚠️ IMPORTANTE: Las ofertas SOLO se aplican si NO viene de balanza ni se modificó manualmente
            var ofertaAplicable = await BuscarOfertaAplicable(codigoBuscado, cantidadPersonalizada);

            // ✅✅✅ CRÍTICO: Aplicar precio de oferta SOLO si NO es código de balanza ni precio manual
            if (!precioPersonalizado.HasValue && (!editarPrecio || string.IsNullOrWhiteSpace(txtPrecio.Text)))
            {
                if (ofertaAplicable != null && ofertaAplicable.TipoOferta != "Combo")
                {
                    if (ofertaAplicable.PrecioOferta > 0)
                    {
                        precioUnitario = ofertaAplicable.PrecioOferta;
                        System.Diagnostics.Debug.WriteLine(
                            $"✅ OFERTA APLICADA AL AGREGAR\n" +
                            $"   Tipo: {ofertaAplicable.TipoOferta}\n" +
                            $"   Producto: {producto["descripcion"]}\n" +
                            $"   Precio normal: {Convert.ToDecimal(producto["precio"]):C2}\n" +
                            $"   Precio con oferta: {precioUnitario:C2}\n" +
                            $"   Descuento: {(Convert.ToDecimal(producto["precio"]) - precioUnitario):C2}");
                    }
                }

                // Verificar combos...
                if (ofertaAplicable != null && ofertaAplicable.TipoOferta == "Combo")
                {
                    bool comboYaCompleto = false;
                    decimal precioProrrateadoPreexistente = 0m;

                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        using (var cmdConfig = new SqlCommand("SET ARITHABORT ON; SET ANSI_WARNINGS ON;", connection))
                        {
                            await cmdConfig.ExecuteNonQueryAsync();
                        }

                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                comboYaCompleto = await VerificarComboCompleto(
                                    ofertaAplicable.Id,
                                    codigoBuscado,
                                    0,
                                    connection,
                                    transaction);

                                if (comboYaCompleto)
                                {
                                    precioProrrateadoPreexistente = await CalcularPrecioComboProrrateado(
                                        ofertaAplicable.Id,
                                        codigoBuscado,
                                        ofertaAplicable.PrecioCombo,
                                        connection,
                                        transaction);

                                    System.Diagnostics.Debug.WriteLine(
                                        $"✅ COMBO PREEXISTENTE - Aplicando precio prorrateado: {precioProrrateadoPreexistente:C2}");
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

                    if (comboYaCompleto)
                    {
                        ofertaAplicable.PrecioOferta = precioProrrateadoPreexistente;
                    }
                    {
                        ofertaAplicable.PrecioOferta = Convert.ToDecimal(producto["precio"]);
                    }
                }
            }
            else
            {
                // ✅ NUEVO: Si es código de balanza o precio manual, anular la oferta
                ofertaAplicable = null;
                System.Diagnostics.Debug.WriteLine(
                    $"⚠️ PRECIO ESPECIAL DETECTADO - Ofertas desactivadas\n" +
                    $"   Tipo: {(precioPersonalizado.HasValue ? "Balanza" : "Manual")}\n" +
                    $"   Precio usado: {precioUnitario:C2}");
            }

            // NUEVO: Obtener el porcentaje de IVA del producto (async-resiliente, sin .Value ni .Close redundante)
            decimal porcentajeIva = 0m;
            using (var connection = new SqlConnection(connectionString))
            {
                var queryIva = @"SELECT iva FROM Productos WHERE codigo = @codigo";
                using (var cmd = new SqlCommand(queryIva, connection))
                {
                    cmd.Parameters.AddWithValue("@codigo", codigoBuscado ?? "");
                    await connection.OpenAsync();
                    var result = await cmd.ExecuteScalarAsync();

                    if (result != null && result != DBNull.Value)
                    {
                        // Intentar parsear respetando la cultura actual; fallback reemplazando separador si hace falta
                        var text = result.ToString();
                        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out porcentajeIva))
                        {
                            decimal.TryParse(text.Replace(".", ","), NumberStyles.Number, CultureInfo.CurrentCulture, out porcentajeIva);
                        }
                    }
                }
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
                            // ✅ NUEVO: Calcular nueva cantidad total
                            int nuevaCantidadTotal = cantidadActual + cantidadPersonalizada;

                            // ✅ NUEVO: Verificar si hay oferta para la nueva cantidad
                            var ofertaActualizacion = await BuscarOfertaAplicable(codigoBuscado, nuevaCantidadTotal);

                            decimal precioFinal = precioUnitario; // Usar el precio ya calculado

                            // ✅ NUEVO: Obtener el precio original del producto
                            decimal precioOriginal = Convert.ToDecimal(producto["precio"]);

                            // ✅ Si hay oferta para la nueva cantidad, aplicarla
                            if (ofertaActualizacion != null && ofertaActualizacion.PrecioOferta > 0)
                            {
                                precioFinal = ofertaActualizacion.PrecioOferta;

                                // Mostrar mensaje informativo
                                MessageBox.Show(
                                    $"🎉 ¡OFERTA APLICADA AL ACTUALIZAR!\n\n" +
                                    $"Producto: {producto["descripcion"]}\n" +
                                    $"Oferta: {ofertaActualizacion.NombreOferta}\n" +
                                    $"Cantidad anterior: {cantidadActual}\n" +
                                    $"Cantidad nueva: {nuevaCantidadTotal}\n" +
                                    $"Cantidad mínima oferta: {ofertaActualizacion.CantidadMinima}\n" +
                                    $"Precio normal: {precioOriginal:C2}\n" +
                                    $"Precio oferta: {ofertaActualizacion.PrecioOferta:C2}\n" +
                                    $"Ahorro por unidad: {(precioOriginal - ofertaActualizacion.PrecioOferta):C2} ({ofertaActualizacion.PorcentajeDescuento:N2}%)",
                                    "Oferta Aplicada",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                            }

                            // ✅✅✅ CRÍTICO: UPDATE COMPLETO con todos los campos de oferta
                            var query = @"UPDATE Ventas 
                                  SET cantidad = cantidad + @nuevaCantidad, 
                                      precio = @precioFinal,
                                      total = (cantidad + @nuevaCantidad) * @precioFinal,
                                      IvaCalculado = (@precioFinal * (cantidad + @nuevaCantidad)) * @porcentajeIva / (100 + @porcentajeIva),
                                      PorcentajeIva = @porcentajeIva,
                                      IdOferta = @IdOferta,
                                      NombreOferta = @NombreOferta,
                                      PrecioOriginal = @PrecioOriginal,
                                      PrecioConOferta = @PrecioConOferta,
                                      DescuentoAplicado = @DescuentoAplicado,
                                      EsOferta = @EsOferta
                                  WHERE nrofactura = @nrofactura AND codigo = @codigo";

                            using (var cmd = new SqlCommand(query, connection, transaction))
                            {
                                var cmdSet = new SqlCommand("SET ARITHABORT ON; SET ANSI_WARNINGS ON;", connection, transaction);
                                await cmdSet.ExecuteNonQueryAsync();

                                cmd.Parameters.AddWithValue("@nuevaCantidad", cantidadPersonalizada);
                                cmd.Parameters.AddWithValue("@precioFinal", precioFinal);
                                cmd.Parameters.AddWithValue("@porcentajeIva", porcentajeIva);
                                cmd.Parameters.AddWithValue("@nrofactura", nroRemitoActual);
                                cmd.Parameters.AddWithValue("@codigo", producto["codigo"]);

                                // ✅ CRÍTICO: Agregar parámetros de oferta en el UPDATE
                                if (ofertaActualizacion != null)
                                {
                                    cmd.Parameters.AddWithValue("@IdOferta", ofertaActualizacion.Id);
                                    cmd.Parameters.AddWithValue("@NombreOferta", ofertaActualizacion.NombreOferta ?? "");
                                    cmd.Parameters.AddWithValue("@PrecioOriginal", precioOriginal);
                                    cmd.Parameters.AddWithValue("@PrecioConOferta", precioFinal);
                                    cmd.Parameters.AddWithValue("@DescuentoAplicado", Math.Round(precioOriginal - precioFinal, 2));
                                    cmd.Parameters.AddWithValue("@EsOferta", 1);
                                }
                                else
                                {
                                    // ✅✅✅ LIMPIAR campos cuando NO hay oferta
                                    cmd.Parameters.AddWithValue("@IdOferta", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@NombreOferta", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PrecioOriginal", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PrecioConOferta", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DescuentoAplicado", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EsOferta", 0);
                                }

                                await cmd.ExecuteNonQueryAsync();

                                // ✅ DEBUG
                                System.Diagnostics.Debug.WriteLine(
                                    $"✅ UPDATE ejecutado - Código: {producto["codigo"]}\n" +
                                    $"   Nueva cantidad: {nuevaCantidadTotal}\n" +
                                    $"   Precio final: {precioFinal:C2}\n" +
                                    $"   ¿Tiene oferta?: {(ofertaActualizacion != null ? "Sí" : "No")}\n" +
                                    $"   EsOferta: {(ofertaActualizacion != null ? 1 : 0)}");
                            }
                        }
                        else
                        {
                            // ✅ NUEVO: Obtener el precio original del producto ANTES de insertar
                            decimal precioOriginal = Convert.ToDecimal(producto["precio"]);

                            // 4b. Si no existe o no permite acumular, hacer INSERT (nueva línea)
                            var query = @"INSERT INTO Ventas 
                                    (NroFactura, codigo, descripcion, cantidad, precio, total, 
                                     IvaCalculado, PorcentajeIva,
                                     IdOferta, NombreOferta, PrecioOriginal, PrecioConOferta, DescuentoAplicado, EsOferta,
                                     rubro, marca, proveedor, costo, fecha, hora, EsCtaCte, NombreCtaCte)
                                VALUES 
                                    (@NroFactura, @codigo, @descripcion, @cantidad, @precio, @total, 
                                     @ivaCalculado, @porcentajeIva,
                                     @IdOferta, @NombreOferta, @PrecioOriginal, @PrecioConOferta, @DescuentoAplicado, @EsOferta,
                                     @rubro, @marca, @proveedor, @costo, @fecha, @hora, @EsCtaCte, @NombreCtaCte)";

                            using (var cmd = new SqlCommand(query, connection, transaction))
                            {
                                var cmdSet = new SqlCommand("SET ARITHABORT ON; SET ANSI_WARNINGS ON;", connection, transaction);
                                await cmdSet.ExecuteNonQueryAsync();

                                // Parámetros básicos del producto
                                cmd.Parameters.AddWithValue("@NroFactura", nroRemitoActual);
                                cmd.Parameters.AddWithValue("@codigo", producto["codigo"]);
                                cmd.Parameters.AddWithValue("@descripcion", producto["descripcion"]);
                                cmd.Parameters.AddWithValue("@cantidad", cantidadPersonalizada);
                                cmd.Parameters.AddWithValue("@precio", precioUnitario);
                                cmd.Parameters.AddWithValue("@total", precioUnitario * cantidadPersonalizada);

                                // ✅✅✅ CRÍTICO: Agregar IVA ANTES de los campos de oferta
                                cmd.Parameters.AddWithValue("@ivaCalculado", ivaCalculado);
                                cmd.Parameters.AddWithValue("@porcentajeIva", porcentajeIva);

                                // Parámetros adicionales del producto
                                cmd.Parameters.AddWithValue("@rubro", producto["rubro"]);
                                cmd.Parameters.AddWithValue("@marca", producto["marca"]);
                                cmd.Parameters.AddWithValue("@proveedor", producto["proveedor"]);
                                cmd.Parameters.AddWithValue("@costo", producto["costo"]);
                                cmd.Parameters.AddWithValue("@fecha", DateTime.Now.Date);
                                cmd.Parameters.AddWithValue("@hora", DateTime.Now.ToString("HH:mm:ss"));

                                // Cuenta corriente
                                cmd.Parameters.AddWithValue("@EsCtaCte", chkEsCtaCte.Checked);
                                cmd.Parameters.AddWithValue("@NombreCtaCte", chkEsCtaCte.Checked ? (object)cbnombreCtaCte.Text : DBNull.Value);

                                // ✅ NUEVO: Registrar información de oferta
                                if (ofertaAplicable != null && ofertaAplicable.TipoOferta != "Combo")
                                {
                                    // ✅ Solo para ofertas NO-Combo
                                    cmd.Parameters.AddWithValue("@IdOferta", ofertaAplicable.Id);
                                    cmd.Parameters.AddWithValue("@NombreOferta", ofertaAplicable.NombreOferta ?? "");
                                    cmd.Parameters.AddWithValue("@PrecioOriginal", precioOriginal);
                                    cmd.Parameters.AddWithValue("@PrecioConOferta", precioUnitario);
                                    cmd.Parameters.AddWithValue("@DescuentoAplicado", Math.Round(precioOriginal - precioUnitario, 2));
                                    cmd.Parameters.AddWithValue("@EsOferta", 1);
                                }
                                else
                                {
                                    // ✅ Para Combos o sin oferta: campos NULL
                                    cmd.Parameters.AddWithValue("@IdOferta", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@NombreOferta", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PrecioOriginal", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PrecioConOferta", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DescuentoAplicado", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EsOferta", 0);
                                }

                                await cmd.ExecuteNonQueryAsync();

                                // ✅ NUEVO: Si es combo y ahora está completo, actualizar precios de todos los productos
                                if (ofertaAplicable != null && ofertaAplicable.TipoOferta == "Combo")
                                {
                                    // ✅ CRÍTICO: Verificar combo DENTRO de la transacción
                                    // ✅ CORREGIDO: Pasar 0 como cantidad porque el INSERT ya guardó el producto
                                    bool comboCompleto = await VerificarComboCompleto(
                                        ofertaAplicable.Id,
                                        codigoBuscado,
                                        0,  // ✅ CAMBIO CRÍTICO: NO sumar cantidad porque ya se hizo INSERT
                                        connection,
                                        transaction);

                                    if (comboCompleto)
                                    {
                                        // ✅ CRÍTICO: Actualizar precios DENTRO de la transacción
                                        await ActualizarPreciosComboCompleto(
                                            ofertaAplicable.Id,
                                            connection,
                                            transaction);

                                        MessageBox.Show(
                                            $"🎉 ¡COMBO COMPLETO!\n\n" +
                                            $"Se aplicó el descuento a todos los productos del combo.\n\n" +
                                            $"Oferta: {ofertaAplicable.NombreOferta}\n" +
                                            $"Precio total combo: {ofertaAplicable.PrecioCombo:C2}",
                                            "Combo Activado",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Information);
                                    }
                                }

                                // ✅ DEBUG
                                System.Diagnostics.Debug.WriteLine(
                                    $"✅ INSERT ejecutado - Código: {producto["codigo"]}\n" +
                                    $"   Cantidad: {cantidadPersonalizada}\n" +
                                    $"   Precio: {precioUnitario:C2}\n" +
                                    $"   Total: {(precioUnitario * cantidadPersonalizada):C2}\n" +
                                    $"   IVA Calculado: {ivaCalculado:C2}\n" +
                                    $"   Porcentaje IVA: {porcentajeIva}%\n" +
                                    $"   ¿Tiene oferta?: {(ofertaAplicable != null ? "Sí" : "No")}");
                            }
                        }

                        // MODIFICADO: Descontar stock SOLO si el producto permite acumular (manejo de inventario)
                        if (permiteAcumular)
                        {
                            var queryStock = @"UPDATE Productos 
                                   SET cantidad = cantidad - @cantidadVendida 
                                   WHERE codigo = @codigo";
                            using (var cmdUpd = new SqlCommand(queryStock, connection, transaction))
                            {
                                cmdUpd.Parameters.AddWithValue("@cantidadVendida", cantidadPersonalizada);
                                cmdUpd.Parameters.AddWithValue("@codigo", producto["codigo"]);
                                await cmdUpd.ExecuteNonQueryAsync();
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"❌ Error en transacción: {ex.Message}");
                        throw;
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

        // NUEVO: Calcular el total del remito actual
        private decimal CalcularTotal()
        {
            decimal total = 0m;

            if (remitoActual != null && remitoActual.Rows.Count > 0)
            {
                foreach (DataRow row in remitoActual.Rows)
                {
                    if (row["total"] != null && row["total"] != DBNull.Value)
                    {
                        if (decimal.TryParse(row["total"].ToString(), out decimal valorTotal))
                        {
                            total += valorTotal;
                        }
                    }
                }
            }

            return total;
        }
        private void Ventas_Load(object sender, EventArgs e)
        {
            // ✅✅✅ MODIFICADO: Formulario más corto y ancho ajustado para MDI
            const int anchoDeseado = 1100; // ✅ Mantener ancho actual
            const int altoDeseado = 600;   // ✅ REDUCIDO: 900 → 750px (más corto, sin scroll)

            if (this.ClientSize.Width < anchoDeseado || this.ClientSize.Height < altoDeseado)
            {
                this.ClientSize = new Size(
                    Math.Max(anchoDeseado, this.ClientSize.Width),
                    Math.Max(altoDeseado, this.ClientSize.Height)
                );
            }

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
            dataGridView1.MultiSelect = false;
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
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 17F, FontStyle.Bold); // ✅ Mantener fuente grande
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 249, 250);
            dataGridView1.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Black;

            // MEJORADO: Estilos de selección más contrastantes
            dataGridView1.DefaultCellStyle.BackColor = Color.White;
            dataGridView1.DefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.White;

            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // MEJORADO: Color más oscuro para filas alternas con mejor selección
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 242, 248);
            dataGridView1.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dataGridView1.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;
        }

        private void ConfigurarPanelFooter()
        {
            // Crear el panel footer programáticamente
            Panel panelFooter = new Panel();
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Height = 120; // ✅ AUMENTADO: 95 → 110 píxeles (más espacio para total)
            panelFooter.BackColor = Color.FromArgb(0, 120, 215);

            // Configurar lbCantidadProductos (dock left)
            lbCantidadProductos.AutoSize = false;
            lbCantidadProductos.TextAlign = ContentAlignment.MiddleLeft;
            lbCantidadProductos.Dock = DockStyle.Left;
            lbCantidadProductos.Width = 250;
            lbCantidadProductos.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lbCantidadProductos.ForeColor = Color.White;
            lbCantidadProductos.Text = "Productos: 0";

            // ✅ Panel contenedor MÁS ANCHO para el total
            Panel panelTotalContainer = new Panel
            {
                Dock = DockStyle.Right,
                Width = 700, // ✅ AUMENTADO: 600 → 700px (más espacio para texto)
                BackColor = Color.FromArgb(0, 120, 215),
                Padding = new Padding(0, 20, 20, 20)
            };

            // RichTextBox para totales
            rtbTotal = new RichTextBox
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.None,
                Multiline = true
            };

            // ✅ Agregar el RichTextBox al contenedor
            panelTotalContainer.Controls.Add(rtbTotal);

            // Agregar controles al panel footer
            panelFooter.Controls.Add(panelTotalContainer);
            panelFooter.Controls.Add(lbCantidadProductos);

            // Agregar el panel al formulario
            this.Controls.Add(panelFooter);
            panelFooter.BringToFront();

            // Ajustar el DataGridView para dejar espacio al footer
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.Dock = DockStyle.None;
            dataGridView1.Location = new Point(0, 171);
            dataGridView1.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 171 - panelFooter.Height);
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
                var query = @"SELECT id, codigo, descripcion, precio, cantidad, total, 
                 PorcentajeIva, IvaCalculado, 
                 ISNULL(EsOferta, 0) AS EsOferta,
                 NombreOferta
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

            // Ocultar columnas internas
            if (dataGridView1.Columns["EsOferta"] != null)
            {
                dataGridView1.Columns["EsOferta"].Visible = false;
            }

            if (dataGridView1.Columns["NombreOferta"] != null)
            {
                dataGridView1.Columns["NombreOferta"].Visible = false;
            }

            if (dataGridView1.Columns["id"] != null)
            {
                dataGridView1.Columns["id"].Visible = false;
            }

            if (dataGridView1.Columns["IvaCalculado"] != null)
            {
                dataGridView1.Columns["IvaCalculado"].Visible = false;
            }

            // Configurar encabezados
            if (dataGridView1.Columns["codigo"] != null)
            {
                dataGridView1.Columns["codigo"].HeaderText = "Codigo";
                dataGridView1.Columns["codigo"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridView1.Columns["codigo"].Width = 170;
            }

            if (dataGridView1.Columns["descripcion"] != null)
            {
                dataGridView1.Columns["descripcion"].HeaderText = "Descripcion";
                dataGridView1.Columns["descripcion"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dataGridView1.Columns["descripcion"].FillWeight = 250;
            }

            if (dataGridView1.Columns["precio"] != null)
            {
                dataGridView1.Columns["precio"].HeaderText = "Precio";
                dataGridView1.Columns["precio"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridView1.Columns["precio"].Width = 120; // ✅ AUMENTADO: 100 → 120
                dataGridView1.Columns["precio"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // Configurar columna total
            if (dataGridView1.Columns["total"] != null)
            {
                var colTotal = dataGridView1.Columns["total"];
                colTotal.HeaderText = "Total";
                colTotal.DefaultCellStyle.Format = "C2";
                colTotal.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                colTotal.DefaultCellStyle.BackColor = dataGridView1.DefaultCellStyle.BackColor;
                colTotal.DefaultCellStyle.Font = new Font("Segoe UI", 16F, FontStyle.Bold); // ✅ AUMENTADO: 12F → 16F
                colTotal.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                colTotal.Width = 140; // ✅ AUMENTADO: 120 → 140
            }

            // IVA%: ancho fijo
            if (dataGridView1.Columns["PorcentajeIva"] != null)
            {
                var colIvaPct = dataGridView1.Columns["PorcentajeIva"];
                colIvaPct.HeaderText = "IVA%";
                colIvaPct.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                colIvaPct.Width = 70; // ✅ AUMENTADO: 60 → 70
                colIvaPct.MinimumWidth = 60;
                colIvaPct.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Cantidad: ancho fijo pequeño
            if (dataGridView1.Columns["cantidad"] != null)
            {
                dataGridView1.Columns["cantidad"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridView1.Columns["cantidad"].HeaderText = "Cant.";
                dataGridView1.Columns["cantidad"].Width = 60; // ✅ AUMENTADO: 50 → 60
                dataGridView1.Columns["cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Llamar a FormatearDataGridView DESPUÉS de configurar todas las columnas
            FormatearDataGridView();

            // FORZAR ancho de ColOferta DESPUÉS de formatear
            if (dataGridView1.Columns["ColOferta"] != null)
            {
                dataGridView1.Columns["ColOferta"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridView1.Columns["ColOferta"].Width = 40; // ✅ AUMENTADO: 35 → 40
                dataGridView1.Columns["ColOferta"].MinimumWidth = 40;
                dataGridView1.Columns["ColOferta"].Resizable = DataGridViewTriState.False;
                dataGridView1.Columns["ColOferta"].DisplayIndex = 0;

                System.Diagnostics.Debug.WriteLine("✅ Ancho ColOferta FORZADO a 40px después de DataSource");
            }

            // Actualizar totales
            lbCantidadProductos.Text = $"Productos: {dataGridView1.Rows.Count}";

            decimal sumaTotal = 0;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["total"]?.Value != null && decimal.TryParse(row.Cells["total"].Value.ToString(), out decimal valorTotal))
                    sumaTotal += valorTotal;
            }

            // Actualizar botón anular
            if (btnAnularFactura != null)
            {
                btnAnularFactura.Visible = true;
                btnAnularFactura.Enabled = (remitoActual != null && remitoActual.Rows.Count > 0);
                btnAnularFactura.BringToFront();
            }

            // ✅✅✅ MODIFICADO: Fuente del TOTAL MUY GRANDE (de 36F a 48F)
            rtbTotal.Clear();
            rtbTotal.SelectionAlignment = HorizontalAlignment.Right;
            rtbTotal.SelectionFont = new Font("Segoe UI", 48F, FontStyle.Bold); // ✅ AUMENTADO: 36F → 48F
            rtbTotal.AppendText($"TOTAL: {sumaTotal:C2}\n");
        }

        // NUEVO: Método async separado para la impresión
        public async Task ImprimirConServicioAsync(SeleccionImpresionForm seleccion)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] === INICIANDO IMPRESIÓN CON SERVICIO ===");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] OpcionSeleccionada: {seleccion.OpcionSeleccionada}");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] CAE: {seleccion.CAENumero}");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] NumeroFacturaAfip: {seleccion.NumeroFacturaAfip}");

                if (remitoActual == null || remitoActual.Rows.Count == 0)
                {
                    MessageBox.Show("No hay productos para imprimir.", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // ✅ NUEVO: Debug de valores de descuento
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] 💰 DATOS DE DESCUENTO:");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   - PorcentajeDescuento: {seleccion.PorcentajeDescuento}");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   - ImporteDescuento: {seleccion.ImporteDescuento:C2}");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   - ImporteTotalConDescuento: {seleccion.ImporteTotalConDescuento:C2}");

                var config = new Servicios.TicketConfig
                {
                    NombreComercio = GetNombreComercio(),
                    DomicilioComercio = GetDomicilioComercio(), // ✅ CORREGIDO: usar el método
                    FormaPago = seleccion.EsPagoMultiple ? "Múltiple" : seleccion.OpcionPagoSeleccionada.ToString(),
                    MensajePie = "Gracias por su compra!",
                    // ✅ NUEVO: Pasar datos de descuento a TicketConfig
                    PorcentajeDescuento = seleccion.PorcentajeDescuento,
                    ImporteDescuento = seleccion.ImporteDescuento,
                    ImporteFinal = seleccion.PorcentajeDescuento > 0
                        ? seleccion.ImporteTotalConDescuento
                        : CalcularTotal()
                };

                // ✅ CRÍTICO: Determinar el TipoComprobante correcto según OpcionSeleccionada
                switch (seleccion.OpcionSeleccionada)
                {
                    case SeleccionImpresionForm.OpcionImpresion.FacturaA:
                        config.TipoComprobante = "FacturaA";
                        config.NumeroComprobante = seleccion.NumeroFacturaAfip > 0
                            ? $"A {seleccion.NumeroFacturaAfip:D4}-{seleccion.NumeroFacturaAfip:D8}"
                            : $"Factura A N° {nroRemitoActual}";
                        config.CAE = seleccion.CAENumero;
                        config.CAEVencimiento = seleccion.CAEVencimiento;
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ✅ Configurado como FACTURA A");
                        break;

                    case SeleccionImpresionForm.OpcionImpresion.FacturaB:
                        config.TipoComprobante = "FacturaB";
                        config.NumeroComprobante = seleccion.NumeroFacturaAfip > 0
                            ? $"B {seleccion.NumeroFacturaAfip:D4}-{seleccion.NumeroFacturaAfip:D8}"
                            : $"Factura B N° {nroRemitoActual}";
                        config.CAE = seleccion.CAENumero;
                        config.CAEVencimiento = seleccion.CAEVencimiento;
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ✅ Configurado como FACTURA B");
                        break;

                    case SeleccionImpresionForm.OpcionImpresion.FacturaC:
                        config.TipoComprobante = "FacturaC";

                        // ✅ CRÍTICO: Obtener punto de venta PRIMERO
                        int puntoVentaFC = ObtenerPuntoVentaActivo();

                        config.NumeroComprobante = seleccion.NumeroFacturaAfip > 0
                            ? $"C {puntoVentaFC:D4}-{seleccion.NumeroFacturaAfip:D8}"  // ✅ CORREGIDO
                            : $"Factura C N° {nroRemitoActual}";

                        config.CAE = seleccion.CAENumero;
                        config.CAEVencimiento = seleccion.CAEVencimiento;

                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ✅ Configurado como FACTURA C");
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   TipoComprobante: {config.TipoComprobante}");
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   Punto Venta: {puntoVentaFC}");
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   Número: {seleccion.NumeroFacturaAfip}");
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   NumeroComprobante: {config.NumeroComprobante}");
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   CAE: {config.CAE}");
                        break;

                    case SeleccionImpresionForm.OpcionImpresion.RemitoTicket:
                    default:
                        config.TipoComprobante = "REMITO";
                        config.NumeroComprobante = $"Remito N° {nroRemitoActual}";
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ✅ Configurado como REMITO");
                        break;
                }

                // ✅ NUEVO: Mostrar configuración completa antes de imprimir
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] 📋 CONFIGURACIÓN FINAL:");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   - TipoComprobante: {config.TipoComprobante}");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   - PorcentajeDescuento: {config.PorcentajeDescuento}");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   - ImporteDescuento: {config.ImporteDescuento:C2}");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN]   - ImporteFinal: {config.ImporteFinal:C2}");

                using (var ticketService = new Servicios.TicketPrintingService())
                {
                    await ticketService.ImprimirTicket(remitoActual, config);
                }

                System.Diagnostics.Debug.WriteLine("[IMPRESIÓN] ✅ Impresión con vista previa completada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ❌ Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        // Agregado: métodos públicos y handlers que faltaban para compilar y enlazar con SeleccionImpresionForm

        // Devuelve el DataTable del remito actual para que el modal lo consulte
        public DataTable GetRemitoActual()
        {
            return remitoActual;
        }

        // Devuelve el número de remito actual
        public int GetNroRemitoActual()
        {
            return nroRemitoActual;
        }

        // Devuelve el nombre del comercio
        public string GetNombreComercio()
        {
            return nombreComercio;
        }

        // ✅ NUEVO: Agregar método para obtener el domicilio
        public string GetDomicilioComercio()
        {
            return domicilioComercio;
        }

        // Handler del botón Finalizar Venta (construido para usar el modal SeleccionImpresionForm)
        private async void btnFinalizarVenta_Click(object sender, EventArgs e)
        {
            try
            {
                if (remitoActual == null || remitoActual.Rows.Count == 0)
                {
                    MessageBox.Show("No hay productos en la venta para finalizar.", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                decimal importeTotal = CalcularTotal();
                System.Diagnostics.Debug.WriteLine($"[VENTAS] Iniciando finalización de venta con total: {importeTotal:C2}");

                using (var seleccion = new SeleccionImpresionForm(importeTotal, this))
                {
                    seleccion.OnProcesarVenta = async (tipoFactura, formaPago, cuitCliente, caeNumero, caeVencimiento, numeroFacturaAfip, numeroFormateado, porcentajeDescuento, importeDescuento) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"[CALLBACK] Iniciando procesamiento - Tipo: {tipoFactura}");

                        try
                        {
                            // ✅ CRÍTICO: Ahora recibimos los descuentos como parámetros
                            System.Diagnostics.Debug.WriteLine($"[DESCUENTO CALLBACK] Porcentaje: {porcentajeDescuento}%");
                            System.Diagnostics.Debug.WriteLine($"[DESCUENTO CALLBACK] Importe: {importeDescuento:C2}");

                            // Obtener pagos múltiples si existen
                            var pagosMultiples = seleccion.EsPagoMultiple
                                ? seleccion.PagosMultiples
                                : null;

                            // ✅ Pasar datos de descuento al método GuardarFacturaEnBD
                            await GuardarFacturaEnBD(
                                tipoFactura,
                                formaPago,
                                cuitCliente,
                                caeNumero,
                                caeVencimiento,
                                numeroFacturaAfip,
                                numeroFormateado,
                                pagosMultiples,
                                porcentajeDescuento,    // ✅ Ahora viene del parámetro
                                importeDescuento        // ✅ Ahora viene del parámetro
                            );

                            System.Diagnostics.Debug.WriteLine("[CALLBACK] Factura guardada exitosamente");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CALLBACK ERROR] {ex.Message}");
                            throw;
                        }
                    };

                    var resultado = seleccion.ShowDialog();

                    if (resultado == DialogResult.OK)
                    {
                        System.Diagnostics.Debug.WriteLine($"[VENTAS] ✅ Venta finalizada exitosamente - Opción: {seleccion.OpcionSeleccionada}");
                        LimpiarYReiniciarVenta();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[VENTAS] ⚠️ Venta cancelada por el usuario");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VENTAS] ❌ Error crítico en btnFinalizarVenta_Click: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[VENTAS] Stack trace: {ex.StackTrace}");

                MessageBox.Show(
                    $"Error al finalizar la venta:\n\n{ex.Message}",
                    "Error Crítico",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // NUEVO: Eliminar factura completa (todas las líneas) con devolución de stock y auditoría
        private async Task EliminarFacturaCompletaAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(nroRemitoActual.ToString()) || dataGridView1.Rows.Count == 0)
                {
                    MessageBox.Show("No hay factura para eliminar.", "Aviso",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // ✅ MODIFICADO: Confirmación simple sin formulario de motivo
                var resultado = MessageBox.Show(
                    $"⚠️ ANULAR FACTURA COMPLETA\n\n" +
                    $"Remito #{nroRemitoActual}\n" +
                    $"Productos: {dataGridView1.Rows.Count}\n" +
                    $"Total: {CalcularTotal():C2}\n\n" +
                    $"¿Está seguro de que desea anular esta factura?\n\n" +
                    $"Esta acción quedará registrada en la auditoría.",
                    "Confirmar Anulación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (resultado != DialogResult.Yes)
                {
                    return; // Usuario canceló
                }

                // ✅ MOTIVO AUTOMÁTICO
                string motivo = "ANULACIÓN FACTURA COMPLETA";

                string connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // ✅✅✅ CRÍTICO: Configurar ARITHABORT ANTES de abrir la transacción
                    using (var cmdConfig = new SqlCommand("SET ARITHABORT ON; SET ANSI_WARNINGS ON;", connection))
                    {
                        await cmdConfig.ExecuteNonQueryAsync();
                    }

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Guardar en auditoría ANTES de eliminar
                            string usuarioActual = ObtenerUsuarioActual();
                            int numeroCajero = obtenerNumeroCajero();

                            // Registrar cada producto de la venta en la auditoría
                            foreach (DataGridViewRow row in dataGridView1.Rows)
                            {
                                if (row.IsNewRow) continue;

                                string codigo = row.Cells["Codigo"].Value?.ToString();
                                string descripcion = row.Cells["Descripcion"].Value?.ToString();
                                int cantidad = Convert.ToInt32(row.Cells["Cantidad"].Value);
                                decimal precio = Convert.ToDecimal(row.Cells["Precio"].Value);

                                // Insertar en AuditoriaProductosEliminados
                                string queryAuditoria = @"
                            INSERT INTO AuditoriaProductosEliminados 
                                (CodigoProducto, DescripcionProducto, PrecioUnitario, Cantidad, 
                                 TotalEliminado, NumeroFactura, FechaHoraVentaOriginal, FechaEliminacion, 
                                 MotivoEliminacion, EsCtaCte, NombreCtaCte, UsuarioEliminacion, 
                                 NumeroCajero, NombreEquipo, EsEliminacionCompleta, CantidadOriginal)
                            VALUES 
                                (@CodigoProducto, @DescripcionProducto, @PrecioUnitario, @Cantidad,
                                 @TotalEliminado, @NumeroFactura, @FechaHoraVentaOriginal, @FechaEliminacion,
                                 @MotivoEliminacion, @EsCtaCte, @NombreCtaCte, @UsuarioEliminacion,
                                 @NumeroCajero, @NombreEquipo, @EsEliminacionCompleta, @CantidadOriginal)";

                                using (var cmdAudit = new SqlCommand(queryAuditoria, connection, transaction))
                                {
                                    cmdAudit.Parameters.AddWithValue("@CodigoProducto", codigo ?? "");
                                    cmdAudit.Parameters.AddWithValue("@DescripcionProducto", descripcion ?? "");
                                    cmdAudit.Parameters.AddWithValue("@PrecioUnitario", precio);
                                    cmdAudit.Parameters.AddWithValue("@Cantidad", cantidad);
                                    cmdAudit.Parameters.AddWithValue("@TotalEliminado", precio * cantidad);
                                    cmdAudit.Parameters.AddWithValue("@NumeroFactura", nroRemitoActual);
                                    cmdAudit.Parameters.AddWithValue("@FechaHoraVentaOriginal", DateTime.Now);
                                    cmdAudit.Parameters.AddWithValue("@FechaEliminacion", DateTime.Now);
                                    cmdAudit.Parameters.AddWithValue("@MotivoEliminacion", motivo); // ✅ Motivo automático
                                    cmdAudit.Parameters.AddWithValue("@EsCtaCte", chkEsCtaCte?.Checked ?? false);
                                    cmdAudit.Parameters.AddWithValue("@NombreCtaCte", chkEsCtaCte?.Checked == true ? (object)cbnombreCtaCte?.Text : DBNull.Value);
                                    cmdAudit.Parameters.AddWithValue("@UsuarioEliminacion", usuarioActual);
                                    cmdAudit.Parameters.AddWithValue("@NumeroCajero", numeroCajero);
                                    cmdAudit.Parameters.AddWithValue("@NombreEquipo", Environment.MachineName);
                                    cmdAudit.Parameters.AddWithValue("@EsEliminacionCompleta", true);
                                    cmdAudit.Parameters.AddWithValue("@CantidadOriginal", cantidad);

                                    await cmdAudit.ExecuteNonQueryAsync();
                                }
                            }

                            // Eliminar de la tabla Ventas
                            string queryEliminarVentas = "DELETE FROM Ventas WHERE nrofactura = @nroRemito";
                            using (var cmdVentas = new SqlCommand(queryEliminarVentas, connection, transaction))
                            {
                                cmdVentas.Parameters.AddWithValue("@nroRemito", nroRemitoActual);
                                await cmdVentas.ExecuteNonQueryAsync();
                            }

                            // Eliminar de la tabla Facturas
                            string queryEliminarFacturas = "DELETE FROM Facturas WHERE NumeroRemito = @nroRemito";
                            using (var cmdFacturas = new SqlCommand(queryEliminarFacturas, connection, transaction))
                            {
                                cmdFacturas.Parameters.AddWithValue("@nroRemito", nroRemitoActual);
                                await cmdFacturas.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();

                            // ✅ MODIFICADO: Mensaje de confirmación simplificado
                            MessageBox.Show(
                                $"✅ FACTURA ANULADA\n\n" +
                                $"Remito: #{nroRemitoActual}\n" +
                                $"Productos eliminados: {dataGridView1.Rows.Count}\n" +
                                $"Usuario: {usuarioActual}\n\n" +
                                $"Registro guardado en auditoría.",
                                "Anulación Exitosa",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                            LimpiarYReiniciarVenta();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception($"Error al anular la factura: {ex.Message}", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al eliminar la factura:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ✅ NUEVO: Obtener punto de venta desde configuración
        private int ObtenerPuntoVentaActivo()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                string ambienteActivo = config["AFIP:AmbienteActivo"] ?? "Testing";
                string puntoVentaStr = config[$"AFIP:{ambienteActivo}:PuntoVenta"];

                if (string.IsNullOrEmpty(puntoVentaStr))
                {
                    System.Diagnostics.Debug.WriteLine($"[PUNTO VENTA] ⚠️ No configurado para {ambienteActivo}, usando 1");
                    return 1;
                }

                if (!int.TryParse(puntoVentaStr, out int puntoVenta))
                {
                    System.Diagnostics.Debug.WriteLine($"[PUNTO VENTA] ⚠️ Valor inválido '{puntoVentaStr}', usando 1");
                    return 1;
                }

                System.Diagnostics.Debug.WriteLine($"[PUNTO VENTA] ✅ Ambiente: {ambienteActivo}, PV: {puntoVenta}");
                return puntoVenta;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PUNTO VENTA] ❌ Error: {ex.Message}, usando 1 por defecto");
                return 1;
            }
        }

        // NUEVO: Método público para vincular a un botón o menú (handler UI)
        public async void BtnEliminarFacturaCompleta_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("BtnEliminarFacturaCompleta_Click invoked");
            await EliminarFacturaCompletaAsync();
        }

        // GuardarFacturaEnBD: implementación mínima que compila y puede ampliarse.
        // Actualmente registra en debug y retorna; si necesitas persistir realmente, lo integro con la lógica completa.
        private async Task GuardarFacturaEnBD(
                        string tipoFactura,
                        string formaPago,
                        string cuitCliente = "",
                        string caeNumero = "",
                        DateTime? caeVencimiento = null,
                        int numeroFacturaAfip = 0,
                        string numeroFormateado = "",
                        List<MultiplePagosControl.DetallePago> pagosMultiples = null,
                        decimal porcentajeDescuento = 0m,
                        decimal importeDescuento = 0m)
        {
            if (remitoActual == null || remitoActual.Rows.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("GuardarFacturaEnBD: no hay remitoActual para guardar.");
                return;
            }

            // Calcular totales desde remitoActual
            decimal totalFactura = 0m;
            decimal ivaTotal = 0m;
            foreach (DataRow r in remitoActual.Rows)
            {
                if (r["total"] != null && decimal.TryParse(r["total"].ToString(), out decimal t))
                    totalFactura += t;
                if (r["IvaCalculado"] != null && decimal.TryParse(r["IvaCalculado"].ToString(), out decimal iv))
                    ivaTotal += iv;
            }

            string connectionString = GetConnectionString();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // ✅ CALCULAR importes con descuento
                            decimal importeTotal = totalFactura;
                            decimal importeFinal = importeTotal - importeDescuento;

                            System.Diagnostics.Debug.WriteLine($"[FACTURA BD] ===================================");
                            System.Diagnostics.Debug.WriteLine($"[FACTURA BD] Importe Total Original: {importeTotal:C2}");
                            System.Diagnostics.Debug.WriteLine($"[FACTURA BD] Porcentaje Descuento: {porcentajeDescuento}%");
                            System.Diagnostics.Debug.WriteLine($"[FACTURA BD] Importe Descuento: {importeDescuento:C2}");
                            System.Diagnostics.Debug.WriteLine($"[FACTURA BD] Importe Final: {importeFinal:C2}");
                            System.Diagnostics.Debug.WriteLine($"[FACTURA BD] IVA Total: {ivaTotal:C2}");
                            System.Diagnostics.Debug.WriteLine($"[FACTURA BD] ===================================");

                            // ✅ DECLARAR la variable idFactura
                            int idFactura = 0;

                            // Obtener datos adicionales
                            string nombreCtaCte = chkEsCtaCte?.Checked == true ? cbnombreCtaCte?.Text : null;
                            string usuarioVenta = ObtenerUsuarioActual();
                            int numeroCajero = obtenerNumeroCajero();

                            // ✅ CORREGIDO: INSERT con nombres de columnas exactos de la base de datos
                            string queryFactura = @"
                        INSERT INTO Facturas 
                            ([NumeroRemito], [Fecha], [Hora], [ImporteTotal], 
                             [FormadePago], [esCtaCte], [CtaCteNombre], [Cajero],
                             [TipoFactura], [CAENumero], [CAEVencimiento], [CUITCliente],
                             [NroFactura], [UsuarioVenta], [IVA],
                             [PorcentajeDescuento], [ImporteDescuento], [ImporteFinal])
                        VALUES 
                            (@NumeroRemito, @Fecha, @Hora, @ImporteTotal,
                             @FormadePago, @esCtaCte, @CtaCteNombre, @Cajero,
                             @TipoFactura, @CAENumero, @CAEVencimiento, @CUITCliente,
                             @NroFactura, @UsuarioVenta, @IVA,
                             @PorcentajeDescuento, @ImporteDescuento, @ImporteFinal);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                            using (var cmdFactura = new SqlCommand(queryFactura, connection, transaction))
                            {
                                // ✅ PARÁMETROS CORREGIDOS según la estructura de la tabla
                                cmdFactura.Parameters.AddWithValue("@NumeroRemito", nroRemitoActual);
                                cmdFactura.Parameters.AddWithValue("@Fecha", DateTime.Now.Date);
                                cmdFactura.Parameters.AddWithValue("@Hora", DateTime.Now);
                                cmdFactura.Parameters.AddWithValue("@ImporteTotal", importeTotal);
                                cmdFactura.Parameters.AddWithValue("@FormadePago", formaPago ?? "");
                                cmdFactura.Parameters.AddWithValue("@esCtaCte", chkEsCtaCte?.Checked ?? false);
                                cmdFactura.Parameters.AddWithValue("@CtaCteNombre", (object)nombreCtaCte ?? DBNull.Value);
                                cmdFactura.Parameters.AddWithValue("@Cajero", numeroCajero.ToString());
                                cmdFactura.Parameters.AddWithValue("@TipoFactura", tipoFactura ?? "");
                                cmdFactura.Parameters.AddWithValue("@CAENumero", (object)caeNumero ?? DBNull.Value);
                                cmdFactura.Parameters.AddWithValue("@CAEVencimiento", (object)caeVencimiento ?? DBNull.Value);
                                cmdFactura.Parameters.AddWithValue("@CUITCliente", (object)cuitCliente ?? DBNull.Value);
                                cmdFactura.Parameters.AddWithValue("@NroFactura", numeroFormateado ?? "");
                                cmdFactura.Parameters.AddWithValue("@UsuarioVenta", usuarioVenta ?? "");
                                cmdFactura.Parameters.AddWithValue("@IVA", ivaTotal);
                                cmdFactura.Parameters.AddWithValue("@PorcentajeDescuento", porcentajeDescuento);
                                cmdFactura.Parameters.AddWithValue("@ImporteDescuento", importeDescuento);
                                cmdFactura.Parameters.AddWithValue("@ImporteFinal", importeFinal);

                                // ✅ ASIGNAR el resultado a idFactura
                                idFactura = (int)await cmdFactura.ExecuteScalarAsync();
                                System.Diagnostics.Debug.WriteLine($"[FACTURA BD] ✅ Factura guardada con ID: {idFactura}");
                            }

                            // ✅ Insertar registros en DetallesPagoFactura (si existe esa tabla)
                            var insertDetalleSql = @"
                        INSERT INTO DetallesPagoFactura
                            (IdFactura, MedioPago, Importe, Observaciones, FechaPago, Usuario, NumeroRemito)
                        VALUES
                            (@IdFactura, @MedioPago, @Importe, @Observaciones, @FechaPago, @Usuario, @NumeroRemito);";

                            if (pagosMultiples != null && pagosMultiples.Any())
                            {
                                foreach (var pago in pagosMultiples)
                                {
                                    using (var cmdPago = new SqlCommand(insertDetalleSql, connection, transaction))
                                    {
                                        cmdPago.Parameters.AddWithValue("@IdFactura", idFactura);
                                        cmdPago.Parameters.AddWithValue("@MedioPago", pago.MedioPago ?? "");
                                        cmdPago.Parameters.AddWithValue("@Importe", pago.Importe);
                                        cmdPago.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(pago.Observaciones) ? (object)DBNull.Value : pago.Observaciones);
                                        cmdPago.Parameters.AddWithValue("@FechaPago", pago.Fecha != default ? pago.Fecha : DateTime.Now);
                                        cmdPago.Parameters.AddWithValue("@Usuario", usuarioVenta);
                                        cmdPago.Parameters.AddWithValue("@NumeroRemito", nroRemitoActual);
                                        await cmdPago.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                            else
                            {
                                // Pago simple
                                using (var cmdPago = new SqlCommand(insertDetalleSql, connection, transaction))
                                {
                                    cmdPago.Parameters.AddWithValue("@IdFactura", idFactura);
                                    cmdPago.Parameters.AddWithValue("@MedioPago", string.IsNullOrEmpty(formaPago) ? "Desconocido" : formaPago);
                                    cmdPago.Parameters.AddWithValue("@Importe", importeFinal); // ✅ Usar importeFinal con descuento
                                    cmdPago.Parameters.AddWithValue("@Observaciones", DBNull.Value);
                                    cmdPago.Parameters.AddWithValue("@FechaPago", DateTime.Now);
                                    cmdPago.Parameters.AddWithValue("@Usuario", usuarioVenta);
                                    cmdPago.Parameters.AddWithValue("@NumeroRemito", nroRemitoActual);
                                    await cmdPago.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            System.Diagnostics.Debug.WriteLine("[GuardarFacturaEnBD] ✅ Factura guardada exitosamente");
                        }
                        catch (Exception exTx)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"[FACTURA BD] ❌ Error en transacción: {exTx.Message}");
                            System.Diagnostics.Debug.WriteLine($"[FACTURA BD] Stack trace: {exTx.StackTrace}");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FACTURA BD] ❌ Error: {ex.Message}");
                MessageBox.Show($"Error procesando remito: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        // ImprimirSinModal: imprime usando el servicio de tickets y los datos actuales en memoria (remitoActual)
        private async Task ImprimirSinModal(SeleccionImpresionForm.OpcionImpresion opcionImpresion,
            SeleccionImpresionForm.OpcionPago opcionPago, string caeNumero, DateTime? caeVencimiento, int numeroFacturaAfip)
        {
            try
            {
                if (remitoActual == null || remitoActual.Rows.Count == 0)
                {
                    MessageBox.Show("No hay productos para imprimir.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var config = new TicketConfig
                {
                    NombreComercio = nombreComercio,
                    DomicilioComercio = domicilioComercio,
                    FormaPago = opcionPago.ToString(),
                    MensajePie = "Gracias por su compra!"
                };

                switch (opcionImpresion)
                {
                    case SeleccionImpresionForm.OpcionImpresion.RemitoTicket:
                        config.TipoComprobante = "REMITO";
                        config.NumeroComprobante = $"Remito N° {nroRemitoActual}";
                        break;
                
                    case SeleccionImpresionForm.OpcionImpresion.FacturaA:
                        config.TipoComprobante = "FacturaA";
                
                        int puntoVentaFA = ObtenerPuntoVentaActivo();

            config.NumeroComprobante = numeroFacturaAfip > 0
                            ? $"A {puntoVentaFA:D4}-{numeroFacturaAfip:D8}"  // ✅ USAR PARÁMETRO
                            : $"Factura A N° {nroRemitoActual}";
                    
                        config.CAE = caeNumero;  // ✅ USAR PARÁMETRO
                        config.CAEVencimiento = caeVencimiento;  // ✅ USAR PARÁMETRO
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ✅ Configurado como FACTURA A");
                        break;

                    case SeleccionImpresionForm.OpcionImpresion.FacturaB:
                        config.TipoComprobante = "FacturaB";
                
                        int puntoVentaFB = ObtenerPuntoVentaActivo();

            config.NumeroComprobante = numeroFacturaAfip > 0
                            ? $"B {puntoVentaFB:D4}-{numeroFacturaAfip:D8}"  // ✅ USAR PARÁMETRO
                            : $"Factura B N° {nroRemitoActual}";
                    
                        config.CAE = caeNumero;  // ✅ USAR PARÁMETRO
                        config.CAEVencimiento = caeVencimiento;  // ✅ USAR PARÁMETRO
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ✅ Configurado como FACTURA B");
                        break;

                    case SeleccionImpresionForm.OpcionImpresion.FacturaC:
                        config.TipoComprobante = "FacturaC";
                
                        int puntoVentaFC = ObtenerPuntoVentaActivo();

            config.NumeroComprobante = numeroFacturaAfip > 0
                            ? $"C {puntoVentaFC:D4}-{numeroFacturaAfip:D8}"  // ✅ USAR PARÁMETRO
                            : $"Factura C N° {nroRemitoActual}";
                    
                        config.CAE = caeNumero;  // ✅ USAR PARÁMETRO
                        config.CAEVencimiento = caeVencimiento;  // ✅ USAR PARÁMETRO
                        System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ✅ Configurado como FACTURA C");
                        break;
                
                    default:
                        config.TipoComprobante = "SINCOMPROBANTE";
                        config.NumeroComprobante = "";
                        break;
                }

        System.Diagnostics.Debug.WriteLine("🖨️ Iniciando impresión (sin modal)...");
        System.Diagnostics.Debug.WriteLine($"   Tipo: {config.TipoComprobante}, Num: {config.NumeroComprobante}, CAE: {config.CAE}");

        using (var ticketService = new TicketPrintingService())
        {
            await ticketService.ImprimirTicket(remitoActual, config);
        }

        System.Diagnostics.Debug.WriteLine("✅ Impresión completada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en ImprimirSinModal: {ex.Message}");
        MessageBox.Show($"Error al imprimir: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        

        // ✅ AGREGAR: Manejador del evento FormClosing
        private async void Ventas_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // Si no hay remito cargado o está vacío, permitir cierre
                if (remitoActual == null || remitoActual.Rows.Count == 0)
                    return;

                // Hay productos cargados -> ofrecer opciones
                var mensaje =
                    "Hay productos cargados en la venta actual.\n\n" +
                    "Debe finalizar la venta (usar 'Finalizar' o la tecla F) o eliminar todos los productos antes de cerrar.\n\n" +
                    "¿Desea finalizar ahora, eliminar todos los productos (anular la venta) o cancelar y volver a la venta?\n\n" +
                    "Sí = Finalizar  |  No = Eliminar todos  |  Cancelar = Volver";

                var resultado = MessageBox.Show(this, mensaje, "Venta en curso - Confirmar",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                if (resultado == DialogResult.Yes)
                {
                    // Indicar al usuario la acción a realizar; no forzamos la finalización automática
                    e.Cancel = true;
                    MessageBox.Show(this,
                        "Pulse el botón 'Finalizar' (o presione F) para completar la venta y elegir la opción de impresión.\n\n" +
                        "El cierre del formulario se reintentará una vez la venta esté finalizada.",
                        "Finalizar venta", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    txtBuscarProducto?.Focus();
                    return;
                }
                else if (resultado == DialogResult.No)
                {
                    // Ejecutar eliminación completa de forma asíncrona
                    e.Cancel = true; // cancelamos mientras procesamos la anulación
                    try
                    {
                        // Feedback visual mínimo
                        var previousCursor = Cursor.Current;
                        Cursor.Current = Cursors.WaitCursor;
                        this.Enabled = false;

                        await EliminarFacturaCompletaAsync().ConfigureAwait(true);

                        this.Enabled = true;
                        Cursor.Current = previousCursor;

                        // Si quedó vacío, permitir cierre
                        if (remitoActual == null || remitoActual.Rows.Count == 0)
                        {
                            // No cancelar: dejar que el cierre continúe
                            e.Cancel = false;
                            return;
                        }
                        else
                        {
                            // Si por alguna razón no se vació, impedir cierre y notificar
                            e.Cancel = true;
                            MessageBox.Show(this,
                                "No se pudo anular completamente la venta. Revise el estado y vuelva a intentar.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Enabled = true;
                        Cursor.Current = Cursors.Default;
                        e.Cancel = true;
                        System.Diagnostics.Debug.WriteLine($"Error anulando venta en cierre: {ex.Message}");
                        MessageBox.Show(this, $"Error anulando la venta: {ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    // Cancel -> no hacer nada, quedarse en el formulario
                    e.Cancel = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                // En caso de fallo, evitar que se cierre por defecto si hay productos
                e.Cancel = true;
                System.Diagnostics.Debug.WriteLine($"Error en Ventas_FormClosing: {ex.Message}");
                MessageBox.Show(this,
                    "Error comprobando estado de la venta. No se permite cerrar el formulario.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // AGREGAR esta clase auxiliar al final del archivo (fuera de la clase Ventas):
        /// <summary>
        /// Clase auxiliar para almacenar datos de ofertas aplicables
        /// </summary>
        internal class OfertaProducto
        {
            public int Id { get; set; }
            public string NombreOferta { get; set; }
            public string TipoOferta { get; set; }
            public int CantidadMinima { get; set; }
            public decimal PrecioOferta { get; set; }
            public decimal PorcentajeDescuento { get; set; }

            // ✅ NUEVO: Campos adicionales para Combos
            public decimal PrecioCombo { get; set; }
            public decimal PorcentajeDescuentoGlobal { get; set; }
        }

        // ✅ MODIFICADO: Buscar oferta con soporte para Combos
        private async Task<OfertaProducto> BuscarOfertaAplicable(string codigoProducto, int cantidad)
        {
            try
            {
                string connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"
                SELECT TOP 1
                    o.Id,
                    o.Nombre AS NombreOferta,
                    o.TipoOferta,
                    o.PrecioCombo,
                    o.PorcentajeDescuentoGlobal,
                    d.CantidadMinima,
                    d.PrecioOferta,
                    d.PorcentajeDescuento
                FROM DetalleOfertasProductos d
                INNER JOIN OfertasProductos o ON d.IdOferta = o.Id
                INNER JOIN productos p ON d.IdProducto = p.ID
                WHERE p.codigo = @CodigoProducto
                    AND o.Activo = 1
                    AND GETDATE() >= o.FechaInicio
                    AND (o.FechaFin IS NULL OR GETDATE() <= o.FechaFin)
                    AND @Cantidad >= d.CantidadMinima
                ORDER BY 
                    CASE 
                        WHEN o.TipoOferta = 'Combo' THEN o.PrecioCombo
                        WHEN o.TipoOferta = 'Descuento' THEN p.precio * (1 - o.PorcentajeDescuentoGlobal / 100)
                        ELSE d.PrecioOferta
                    END ASC";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@CodigoProducto", codigoProducto);
                        cmd.Parameters.AddWithValue("@Cantidad", cantidad);

                        await connection.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var oferta = new OfertaProducto
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    NombreOferta = reader["NombreOferta"].ToString(),
                                    TipoOferta = reader["TipoOferta"].ToString(),
                                    CantidadMinima = Convert.ToInt32(reader["CantidadMinima"]),
                                    PorcentajeDescuento = reader["PorcentajeDescuento"] != DBNull.Value
                                        ? Convert.ToDecimal(reader["PorcentajeDescuento"])
                                        : 0m
                                };

                                // ✅ NUEVO: Calcular precio según tipo de oferta
                                switch (oferta.TipoOferta)
                                {
                                    case "Combo":
                                        // ✅ CRÍTICO: Para combos, verificar si TODOS los productos están en la venta
                                        oferta.PrecioCombo = reader["PrecioCombo"] != DBNull.Value
                                                ? Convert.ToDecimal(reader["PrecioCombo"])
                                                : 0m;

                                        // ✅ IMPORTANTE: NO aplicar precio prorrateado aquí
                                        // Se aplicará después con ActualizarPreciosComboCompleto
                                        oferta.PrecioOferta = await ObtenerPrecioProducto(codigoProducto);

                                        System.Diagnostics.Debug.WriteLine(
                                            $"⚠️ COMBO DETECTADO (sin descuento aún)\n" +
                                            $"   Producto: {codigoProducto}\n" +
                                            $"   Precio normal: {oferta.PrecioOferta:C2}");

                                        // ✅ RETORNAR la oferta SIN verificar combo (se verifica después del INSERT)
                                        return oferta;
                                        break;

                                    case "Descuento":
                                        // Para descuentos, obtener precio original y aplicar porcentaje
                                        decimal precioOriginalDesc = await ObtenerPrecioProducto(codigoProducto);
                                        decimal porcentajeGlobal = reader["PorcentajeDescuentoGlobal"] != DBNull.Value
                                            ? Convert.ToDecimal(reader["PorcentajeDescuentoGlobal"])
                                            : 0m;

                                        // ✅ Calcular precio con descuento
                                        oferta.PrecioOferta = precioOriginalDesc * (1 - porcentajeGlobal / 100);
                                        oferta.PorcentajeDescuento = porcentajeGlobal;

                                        System.Diagnostics.Debug.WriteLine(
                                            $"✅ DESCUENTO APLICADO\n" +
                                            $"   Precio original: {precioOriginalDesc:C2}\n" +
                                            $"   Porcentaje: {porcentajeGlobal}%\n" +
                                            $"   Precio final: {oferta.PrecioOferta:C2}");
                                        break;

                                    case "PorCantidad":
                                    default:
                                        // Para ofertas por cantidad, usar el precio directo
                                        oferta.PrecioOferta = reader["PrecioOferta"] != DBNull.Value
                                            ? Convert.ToDecimal(reader["PrecioOferta"])
                                            : 0m;
                                        break;
                                }

                                System.Diagnostics.Debug.WriteLine(
                                    $"✅ Oferta encontrada - Tipo: {oferta.TipoOferta}, " +
                                    $"Precio: {oferta.PrecioOferta:C2}");

                                return oferta;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error buscando oferta: {ex.Message}");
            }

            return null;
        }

        // ✅ CORREGIDO: Verificar si todos los productos del combo están en la venta actual
        private async Task<bool> VerificarComboCompleto(
            int idOferta,
            string codigoProductoActual,
            int cantidadActual,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Verificando combo completo - IdOferta: {idOferta}");

                // ✅ PASO 1: Obtener todos los productos requeridos del combo
                var queryCombo = @"
            SELECT 
                p.codigo,
                d.CantidadMinima
            FROM DetalleOfertasProductos d
            INNER JOIN productos p ON d.IdProducto = p.ID
            WHERE d.IdOferta = @IdOferta";

                var productosCombo = new Dictionary<string, int>(); // codigo -> cantidad mínima

                using (var cmd = new SqlCommand(queryCombo, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@IdOferta", idOferta);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string codigo = reader["codigo"].ToString();
                            int cantidadMinima = Convert.ToInt32(reader["CantidadMinima"]);
                            productosCombo[codigo] = cantidadMinima;

                            System.Diagnostics.Debug.WriteLine(
                                $"   📦 Producto requerido: {codigo} (mínimo: {cantidadMinima})");
                        }
                    }
                }

                if (productosCombo.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ No se encontraron productos para el combo");
                    return false;
                }

                // ✅ PASO 2: Verificar cuántos productos del combo están en la venta actual
                // CORREGIDO: Sin STRING_SPLIT (compatible con SQL Server 2012+)
                var codigosLista = productosCombo.Keys.ToList();
                var parametrosCodigos = string.Join(",", codigosLista.Select((_, i) => $"@codigo{i}"));

                var queryVenta = $@"
                    SELECT codigo, SUM(cantidad) as cantidadTotal
                    FROM Ventas
                    WHERE nrofactura = @nrofactura
                    AND codigo IN ({parametrosCodigos})
                    GROUP BY codigo";

                var productosEnVenta = new Dictionary<string, int>(); // codigo -> cantidad en venta

                using (var cmd = new SqlCommand(queryVenta, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@nrofactura", nroRemitoActual);

                    // ✅ Agregar parámetros individuales para cada código
                    for (int i = 0; i < codigosLista.Count; i++)
                    {
                        cmd.Parameters.AddWithValue($"@codigo{i}", codigosLista[i]);
                    }

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string codigo = reader["codigo"].ToString();
                            int cantidadTotal = Convert.ToInt32(reader["cantidadTotal"]);
                            productosEnVenta[codigo] = cantidadTotal;

                            System.Diagnostics.Debug.WriteLine(
                                $"   ✅ Producto en venta: {codigo} (cantidad: {cantidadTotal})");
                        }
                    }
                }

                // ✅ PASO 3: Agregar el producto actual SOLO si cantidad > 0
                if (cantidadActual > 0)
                {
                    if (productosEnVenta.ContainsKey(codigoProductoActual))
                    {
                        productosEnVenta[codigoProductoActual] += cantidadActual;
                    }
                    else
                    {
                        productosEnVenta[codigoProductoActual] = cantidadActual;
                    }

                    System.Diagnostics.Debug.WriteLine(
                        $"   📝 Producto actual agregado: {codigoProductoActual} " +
                        $"(cantidad total: {productosEnVenta[codigoProductoActual]})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"   ℹ️ Verificación sin agregar cantidad (cantidad=0)");
                }

                // ✅✅✅ NUEVO DEBUG: Verificar estado ANTES del PASO 4
                System.Diagnostics.Debug.WriteLine($"📋 RESUMEN ANTES DE VALIDAR:");
                System.Diagnostics.Debug.WriteLine($"   Productos requeridos: {productosCombo.Count}");
                System.Diagnostics.Debug.WriteLine($"   Productos en venta: {productosEnVenta.Count}");
                foreach (var kvp in productosEnVenta)
                {
                    System.Diagnostics.Debug.WriteLine($"   - {kvp.Key}: {kvp.Value} unidades");
                }

                // ✅ PASO 4: Verificar que TODOS los productos tengan la MISMA cantidad múltiplo
                int? cantidadComboMinima = null;

                foreach (var kvp in productosCombo)
                {
                    string codigoRequerido = kvp.Key;
                    int cantidadMinimaProducto = kvp.Value;

                    if (!productosEnVenta.ContainsKey(codigoRequerido))
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"❌ Combo INCOMPLETO - Falta producto: {codigoRequerido}");
                        return false;
                    }

                    int cantidadEnVenta = productosEnVenta[codigoRequerido];

                    // ✅ CRÍTICO: Calcular cuántos combos completos se pueden formar con este producto
                    int combosQuePuedeFormar = cantidadEnVenta / cantidadMinimaProducto;

                    System.Diagnostics.Debug.WriteLine(
                        $"   📦 {codigoRequerido}:\n" +
                        $"      Cantidad en venta: {cantidadEnVenta}\n" +
                        $"      Cantidad mínima: {cantidadMinimaProducto}\n" +
                        $"      Combos que puede formar: {combosQuePuedeFormar}");

                    if (combosQuePuedeFormar == 0)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"❌ Combo INCOMPLETO - Producto {codigoRequerido} no alcanza mínimo");
                        return false;
                    }

                    // ✅ Guardar o verificar que todos los productos formen la misma cantidad de combos
                    if (cantidadComboMinima == null)
                    {
                        cantidadComboMinima = combosQuePuedeFormar;
                    }
                    else if (cantidadComboMinima != combosQuePuedeFormar)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"❌ Combo INCOMPLETO - Cantidades no coinciden:\n" +
                            $"   Producto anterior podía formar: {cantidadComboMinima} combos\n" +
                            $"   Producto {codigoRequerido} puede formar: {combosQuePuedeFormar} combos\n" +
                            $"   Las cantidades deben ser iguales para aplicar descuento");
                        return false;
                    }
                }

                System.Diagnostics.Debug.WriteLine(
                    $"🎉 COMBO COMPLETO - IdOferta: {idOferta}\n" +
                    $"   Cantidad de combos formados: {cantidadComboMinima}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"❌ ERROR en VerificarComboCompleto: {ex.Message}\n" +
                    $"   Stack: {ex.StackTrace}");
                return false;
            }
        }

        // ✅ CORREGIDO: Actualizar precios de todos los productos del combo cuando se completa
        private async Task ActualizarPreciosComboCompleto(
            int idOferta,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔥 INICIANDO ActualizarPreciosComboCompleto - IdOferta: {idOferta}");

                // ✅ CRÍTICO: Configurar ARITHABORT
                using (var cmdConfig = new SqlCommand("SET ARITHABORT ON; SET ANSI_WARNINGS ON;", connection, transaction))
                {
                    await cmdConfig.ExecuteNonQueryAsync();
                }

                // ✅ PASO 1: Obtener precio combo y todos los productos DEL COMBO
                decimal precioCombo = 0m;
                string nombreOferta = "";
                var productosCombo = new List<(string codigo, int cantidadMinima, decimal precioOriginal)>();

                var queryOferta = @"
            SELECT o.PrecioCombo, o.Nombre, p.codigo, p.precio, d.CantidadMinima
            FROM OfertasProductos o
            INNER JOIN DetalleOfertasProductos d ON d.IdOferta = o.Id
            INNER JOIN productos p ON d.IdProducto = p.ID
            WHERE o.Id = @IdOferta";

                using (var cmd = new SqlCommand(queryOferta, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@IdOferta", idOferta);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (precioCombo == 0m)
                            {
                                precioCombo = Convert.ToDecimal(reader["PrecioCombo"]);
                                nombreOferta = reader["Nombre"].ToString();

                                System.Diagnostics.Debug.WriteLine(
                                    $"📦 Combo detectado:\n" +
                                    $"   Nombre: {nombreOferta}\n" +
                                    $"   Precio combo total: {precioCombo:C2}");
                            }

                            var codigo = reader["codigo"].ToString();
                            var cantidad = Convert.ToInt32(reader["CantidadMinima"]);
                            var precioOriginal = Convert.ToDecimal(reader["precio"]);

                            productosCombo.Add((codigo, cantidad, precioOriginal));

                            System.Diagnostics.Debug.WriteLine(
                                $"   Producto combo: {codigo} - Cant: {cantidad} - Precio: {precioOriginal:C2}");
                        }
                    }
                }

                if (productosCombo.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("❌ ERROR: No se encontraron productos para el combo");
                    return;
                }

                // ✅ PASO 2: AHORA SÍ verificar cuántos combos completos hay en la venta
                int cantidadCombos = int.MaxValue;

                var codigosLista = productosCombo.Select(p => p.codigo).ToList();
                var parametrosCodigos = string.Join(",", codigosLista.Select((_, i) => $"@codigoVerif{i}"));

                var queryVerificar = $@"
            SELECT codigo, SUM(cantidad) as cantidadTotal
            FROM Ventas
            WHERE nrofactura = @nrofactura
            AND codigo IN ({parametrosCodigos})
            GROUP BY codigo";

                using (var cmdVerif = new SqlCommand(queryVerificar, connection, transaction))
                {
                    cmdVerif.Parameters.AddWithValue("@nrofactura", nroRemitoActual);

                    for (int i = 0; i < codigosLista.Count; i++)
                    {
                        cmdVerif.Parameters.AddWithValue($"@codigoVerif{i}", codigosLista[i]);
                    }

                    using (var reader = await cmdVerif.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string codigo = reader["codigo"].ToString();
                            int cantidadTotal = Convert.ToInt32(reader["cantidadTotal"]);

                            var productoInfo = productosCombo.First(p => p.codigo == codigo);
                            int combosQueForma = cantidadTotal / productoInfo.cantidadMinima;

                            System.Diagnostics.Debug.WriteLine(
                                $"   📊 {codigo}: {cantidadTotal} unidades ÷ {productoInfo.cantidadMinima} = {combosQueForma} combos");

                            cantidadCombos = Math.Min(cantidadCombos, combosQueForma);
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine(
                    $"🎯 COMBOS VÁLIDOS A APLICAR: {cantidadCombos}\n" +
                    $"   Precio combo unitario: {precioCombo:C2}");

                if (cantidadCombos == 0 || cantidadCombos == int.MaxValue)
                {
                    System.Diagnostics.Debug.WriteLine("❌ ERROR: No se pueden formar combos completos");
                    return;
                }

                // ✅ PASO 3: Actualizar cada producto del combo
                foreach (var producto in productosCombo)
                {
                    // ✅ CRÍTICO: Obtener cantidad TOTAL en venta de este producto
                    int cantidadTotalEnVenta = 0;

                    var queryObtenerCantidad = @"
                SELECT SUM(cantidad) as cantidadTotal
                FROM Ventas
                WHERE nrofactura = @nrofactura AND codigo = @codigo";

                    using (var cmdCant = new SqlCommand(queryObtenerCantidad, connection, transaction))
                    {
                        cmdCant.Parameters.AddWithValue("@nrofactura", nroRemitoActual);
                        cmdCant.Parameters.AddWithValue("@codigo", producto.codigo);

                        var result = await cmdCant.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            cantidadTotalEnVenta = Convert.ToInt32(result);
                        }
                    }

                    // ✅ Calcular cuántas unidades tienen descuento y cuántas no
                    int unidadesConDescuento = cantidadCombos * producto.cantidadMinima;
                    int unidadesSinDescuento = cantidadTotalEnVenta - unidadesConDescuento;

                    System.Diagnostics.Debug.WriteLine(
                        $"   📦 {producto.codigo}:\n" +
                        $"      Total en venta: {cantidadTotalEnVenta}\n" +
                        $"      Con descuento: {unidadesConDescuento}\n" +
                        $"      Sin descuento: {unidadesSinDescuento}");

                    // ✅ Calcular precio prorrateado SOLO para las unidades con descuento
                    decimal precioProrrateado = await CalcularPrecioComboProrrateado(
                             idOferta, producto.codigo, precioCombo, connection, transaction);       

                    // ✅ Si TODAS las unidades tienen descuento, actualizar directamente
                    if (unidadesSinDescuento == 0)
                    {
                        var queryUpdate = @"
                    UPDATE Ventas
                    SET precio = @precioProrrateado,
                        total = cantidad * @precioProrrateado,
                        IdOferta = @IdOferta,
                        NombreOferta = @NombreOferta,
                        EsOferta = 1,
                        PrecioOriginal = @PrecioOriginal,
                        PrecioConOferta = @precioProrrateado,
                        DescuentoAplicado = @PrecioOriginal - @precioProrrateado
                    WHERE nrofactura = @nrofactura 
                    AND codigo = @codigo";

                        using (var cmd = new SqlCommand(queryUpdate, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@precioProrrateado", precioProrrateado);
                            cmd.Parameters.AddWithValue("@IdOferta", idOferta);
                            cmd.Parameters.AddWithValue("@NombreOferta", nombreOferta);
                            cmd.Parameters.AddWithValue("@PrecioOriginal", producto.precioOriginal);
                            cmd.Parameters.AddWithValue("@nrofactura", nroRemitoActual);
                            cmd.Parameters.AddWithValue("@codigo", producto.codigo);

                            int filasAfectadas = await cmd.ExecuteNonQueryAsync();

                            System.Diagnostics.Debug.WriteLine(
                                $"   ✅ UPDATE completo - {producto.codigo}: {filasAfectadas} filas");
                        }
                    }
                    else
                    {
                        // ⚠️ CASO COMPLEJO: Hay unidades con y sin descuento
                        System.Diagnostics.Debug.WriteLine(
                            $"   ⚠️ ADVERTENCIA: Producto {producto.codigo} tiene unidades sin descuento\n" +
                            $"      Para aplicar el combo, las cantidades de todos los productos deben ser iguales");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"🎉 COMBO COMPLETADO - Precios actualizados para {cantidadCombos} combos");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR en ActualizarPreciosComboCompleto: {ex.Message}");
                throw;
            }
        }



        // ✅ NUEVO: Método helper para obtener precio de un producto
        private async Task<decimal> ObtenerPrecioProducto(string codigoProducto)
        {
            try
            {
                string connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = "SELECT precio FROM productos WHERE codigo = @codigo";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@codigo", codigoProducto);
                        await connection.OpenAsync();

                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            return Convert.ToDecimal(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo precio producto: {ex.Message}");
            }

            return 0m;
        }

        // ✅ SOBRECARGA: Versión sin conexión/transacción (abre nueva conexión)
        private async Task<decimal> CalcularPrecioComboProrrateado(
            int idOferta,
            string codigoProducto,
            decimal precioComboTotal,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔢 INICIANDO CalcularPrecioComboProrrateado (con transacción):");

                var query = @"
            SELECT 
                p.codigo,
                p.precio AS PrecioOriginal,
                d.CantidadMinima
            FROM DetalleOfertasProductos d
            INNER JOIN productos p ON d.IdProducto = p.ID
            WHERE d.IdOferta = @IdOferta";

                decimal sumaPreciosOriginales = 0m;
                decimal precioOriginalProductoActual = 0m;
                int cantidadProductoActual = 1;

                using (var cmd = new SqlCommand(query, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@IdOferta", idOferta);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string codigo = reader["codigo"].ToString();
                            decimal precioOriginal = Convert.ToDecimal(reader["PrecioOriginal"]);
                            int cantidad = Convert.ToInt32(reader["CantidadMinima"]);

                            decimal subtotalProducto = precioOriginal * cantidad;
                            sumaPreciosOriginales += subtotalProducto;

                            if (codigo == codigoProducto)
                            {
                                precioOriginalProductoActual = precioOriginal;
                                cantidadProductoActual = cantidad;
                            }
                        }
                    }
                }

                if (sumaPreciosOriginales > 0 && precioOriginalProductoActual > 0)
                {
                    decimal subtotalProductoActual = precioOriginalProductoActual * cantidadProductoActual;
                    decimal participacion = subtotalProductoActual / sumaPreciosOriginales;
                    decimal precioProrrateado = (precioComboTotal * participacion) / cantidadProductoActual;

                    return precioProrrateado;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR en CalcularPrecioComboProrrateado: {ex.Message}");
            }

            return 0m;
        }
    }
}