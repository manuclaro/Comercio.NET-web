using ArcaWS;
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

        // NUEVO: botón para anular factura completa
        private Button btnAnularFactura;

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

                // ✅ PASO 1: Obtener información completa del producto
                string codigo = "";
                decimal precioOriginal = 0m;

                var queryObtenerDatos = @"
    SELECT v.codigo, p.precio as precio_producto 
    FROM Ventas v
    INNER JOIN Productos p ON v.codigo = p.codigo
    WHERE v.id = @idVenta";

                using (var cmd = new SqlCommand(queryObtenerDatos, connection))
                {
                    cmd.Parameters.AddWithValue("@idVenta", idVenta);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            codigo = reader["codigo"].ToString();
                            precioOriginal = Convert.ToDecimal(reader["precio_producto"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(codigo))
                {
                    throw new Exception("No se pudo obtener el código del producto.");
                }

                // ✅ PASO 2: Verificar si hay oferta para la nueva cantidad
                var oferta = await BuscarOfertaAplicable(codigo, nuevaCantidad);

                decimal precioFinal;
                bool cambioDeOferta = false;
                string mensajeOferta = "";

                if (oferta != null && oferta.PrecioOferta > 0)
                {
                    // ✅ HAY OFERTA para la nueva cantidad
                    precioFinal = oferta.PrecioOferta;

                    // Verificar si el precio actual es diferente al de oferta
                    if (Math.Abs(precio - oferta.PrecioOferta) > 0.01m)
                    {
                        cambioDeOferta = true;

                        if (precio > oferta.PrecioOferta)
                        {
                            mensajeOferta =
                                $"🎉 ¡OFERTA APLICADA!\n\n" +
                                $"Oferta: {oferta.NombreOferta}\n" +
                                $"Cantidad requerida: {oferta.CantidadMinima}\n" +
                                $"Nueva cantidad: {nuevaCantidad}\n\n" +
                                $"Precio anterior: {precio:C2}\n" +
                                $"Precio oferta: {oferta.PrecioOferta:C2}\n" +
                                $"Ahorro: {(precio - oferta.PrecioOferta):C2} ({oferta.PorcentajeDescuento:N2}%)";
                        }
                        else
                        {
                            mensajeOferta =
                                $"🎉 ¡MEJOR OFERTA!\n\n" +
                                $"Nueva oferta: {oferta.NombreOferta}\n" +
                                $"Cantidad: {nuevaCantidad}\n\n" +
                                $"Precio anterior: {precio:C2}\n" +
                                $"Precio oferta: {oferta.PrecioOferta:C2}\n" +
                                $"Ahorro adicional: {(precio - oferta.PrecioOferta):C2}";
                        }
                    }
                }
                else
                {
                    // ✅ NO HAY OFERTA para la nueva cantidad
                    precioFinal = precioOriginal;

                    // Verificar si perdió una oferta que tenía antes
                    if (precio < precioOriginal - 0.01m)
                    {
                        cambioDeOferta = true;
                        mensajeOferta =
                            $"⚠️ OFERTA NO DISPONIBLE\n\n" +
                            $"La cantidad {nuevaCantidad} no cumple el mínimo para ofertas.\n\n" +
                            $"Precio anterior (oferta): {precio:C2}\n" +
                            $"Precio normal: {precioOriginal:C2}\n" +
                            $"Diferencia: +{(precioOriginal - precio):C2}";
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

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@nuevaCantidad", nuevaCantidad);
                    cmd.Parameters.AddWithValue("@precio", precioFinal);
                    cmd.Parameters.AddWithValue("@idVenta", idVenta);

                    // ✅✅✅ CRÍTICO: Agregar parámetros de oferta (con o sin oferta)
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

                // ✅ PASO 4: Mostrar mensaje solo si hubo cambio de oferta
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
                    $"   Nueva cantidad: {nuevaCantidad}\n" +
                    $"   Precio aplicado: {precioFinal:C2}\n" +
                    $"   ¿Tiene oferta?: {(oferta != null ? "Sí" : "No")}\n" +
                    $"   ¿Cambió precio?: {cambioDeOferta}");
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
                        return; // ✅ NO HACER NADA MÁS - mantener foco aquí
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

                    //Limitar a 6 dígitos(excluyendo el punto decimal y el signo menos)
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
                        // ✅ NO cambiar foco - mantenerlo aquí en txtPrecio
                        return;
                    }

                    // ✅ Solo procesar si NO es entrada de scanner
                    if (esEntradaDeScanner)
                    {
                        return; // No procesar más si es scanner
                    }

                    // Guardar la posición del cursor
                    //int selectionStart = textBox.SelectionStart;

                    // Obtener solo los dígitos, punto y signo menos
                    //string textoLimpio = new string(textBox.Text.Where(c => char.IsDigit(c) || c == '-' || c == '.').ToArray());

                    // Si el texto cambió, actualizar
                    //if (textBox.Text != textoLimpio)
                    //{
                    //    textBox.Text = textoLimpio;
                    //    textBox.SelectionStart = Math.Min(selectionStart, textBox.Text.Length);
                    //}

                    //// Validar que no supere 6 dígitos
                    //int digitosActuales = textoLimpio.Count(c => char.IsDigit(c));
                    //if (digitosActuales > 6)
                    //{
                    //    string signo = textoLimpio.StartsWith("-") ? "-" : "";
                    //    string soloDigitos = new string(textoLimpio.Where(char.IsDigit).ToArray());
                    //    textBox.Text = signo + soloDigitos.Substring(0, 6);
                    //    textBox.SelectionStart = textBox.Text.Length;
                    //}
                };

                // ✅ CRÍTICO: Resetear flag SOLO cuando pierde foco
                txtPrecio.Leave += (s, e) =>
                {
                    esEntradaDeScanner = false;
                    ultimaTeclaPresionada = DateTime.MinValue;
                    System.Diagnostics.Debug.WriteLine("[PRECIO] Flag scanner reseteado al PERDER foco");
                };

                txtPrecio.Enter += (s, e) =>
                {
                    txtPrecio.SelectAll();
                    System.Diagnostics.Debug.WriteLine("[PRECIO] Campo enfocado - flag scanner actual: " + esEntradaDeScanner);
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

                // ✅✅✅ CRÍTICO: Limpiar el campo inmediatamente
                ((TextBox)sender).Clear();
                System.Diagnostics.Debug.WriteLine("🧹 [PRECIO] Campo limpiado automáticamente");

                // ✅ Mostrar advertencia SOLO la primera vez usando el campo de clase
                if (!mensajeScannerMostrado)
                {
                    mensajeScannerMostrado = true;

                    // ✅ Mostrar advertencia sin bloquear el hilo
                    Task.Run(async () =>
                    {
                        await Task.Delay(100); // Esperar a que termine el escaneo

                        // Volver al hilo de UI para mostrar el mensaje
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

                                // Resetear para permitir mostrar de nuevo en futuras ocasiones
                                mensajeScannerMostrado = false;
                            }));
                        }
                    });
                }

                // ✅ NO cambiar foco - mantenerlo en txtPrecio
                return;
            }
            else if (intervalo >= UMBRAL_MILISEGUNDOS_SCANNER)
            {
                esEntradaDeScanner = false;
                System.Diagnostics.Debug.WriteLine($"✅ [PRECIO] Teclado manual - Intervalo: {intervalo:F2}ms");
            }

            // Manejar Enter normalmente solo si NO es scanner
            if (e.KeyCode == Keys.Enter && !esEntradaDeScanner)
            {
                e.SuppressKeyPress = true;
                if (txtPrecio.Enabled && !string.IsNullOrWhiteSpace(txtPrecio.Text))
                {
                    btnAgregar.Focus();
                }
                else
                {
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
                string codigoProducto = textoIngresado.Substring(2, 5);
                codigoProducto = codigoProducto.TrimStart('0');
                if (string.IsNullOrEmpty(codigoProducto))
                    codigoProducto = "0";

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
                    Width = 35, // ✅ REDUCIDO: Ancho mínimo para el emoji
                    MinimumWidth = 30,
                    ReadOnly = true,
                    Frozen = false,
                    Resizable = DataGridViewTriState.False,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        Font = new Font("Segoe UI Emoji", 11F, FontStyle.Regular),
                        ForeColor = Color.Green,
                        Padding = new Padding(0)
                    }
                };

                // ✅✅✅ CRÍTICO: Insertar como PRIMERA columna (índice 0)
                dataGridView1.Columns.Insert(0, colOferta);

                System.Diagnostics.Debug.WriteLine("✅ Columna ColOferta creada como PRIMERA columna (35px)");
            }

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
                dataGridView1.Columns["cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // ✅✅✅ NUEVO: OCULTAR columna IvaCalculado (IVA $)
            if (dataGridView1.Columns["IvaCalculado"] != null)
            {
                dataGridView1.Columns["IvaCalculado"].Visible = false;
                System.Diagnostics.Debug.WriteLine("✅ Columna IvaCalculado OCULTADA");
            }

            // ✅✅✅ MEJORADO: Configurar columna descripción con fuente MÁS GRANDE, NEGRITA y MÁS ESPACIO
            if (dataGridView1.Columns["descripcion"] != null)
            {
                var colDescripcion = dataGridView1.Columns["descripcion"];
                colDescripcion.DefaultCellStyle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
                colDescripcion.DefaultCellStyle.ForeColor = Color.FromArgb(33, 33, 33);
                colDescripcion.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                // ✅ NUEVO: Dar más espacio con Fill y FillWeight aumentado
                colDescripcion.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                colDescripcion.FillWeight = 250; // ✅ AUMENTADO de 200 a 250 para ocupar espacio de IVA $

                System.Diagnostics.Debug.WriteLine("✅ Columna descripción configurada: 11pt Bold, FillWeight=250");
            }

            // ✅ DEBUG: Verificar cuántas filas hay
            System.Diagnostics.Debug.WriteLine($"📊 Procesando {dataGridView1.Rows.Count} filas en FormatearDataGridView");

            // ✅ CORREGIDO: Recorrer las filas y marcar visualmente las que tienen oferta
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                // Verificar si la celda EsOferta existe
                if (row.Cells["EsOferta"] == null)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Fila {row.Index}: Celda EsOferta es NULL");
                    continue;
                }

                var valorEsOferta = row.Cells["EsOferta"].Value;
                System.Diagnostics.Debug.WriteLine($"🔍 Fila {row.Index}: EsOferta Value = '{valorEsOferta ?? "NULL"}' (Type: {valorEsOferta?.GetType().Name ?? "NULL"})");

                if (valorEsOferta != null && valorEsOferta != DBNull.Value)
                {
                    // ✅ Manejo robusto de conversión
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

                    System.Diagnostics.Debug.WriteLine($"   └─ EsOferta parseado: {esOferta}");

                    if (esOferta)
                    {
                        // ✅ Producto con oferta - mostrar emoji
                        if (row.Cells["ColOferta"] != null)
                        {
                            row.Cells["ColOferta"].Value = "🎁";
                            row.Cells["ColOferta"].Style.ForeColor = Color.Green;

                            // ✅ OPCIONAL: Colorear toda la fila para destacar más
                            row.DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240); // Verde muy claro

                            // ✅ NUEVO: Hacer la descripción aún más visible en productos con oferta
                            if (row.Cells["descripcion"] != null)
                            {
                                row.Cells["descripcion"].Style.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
                                row.Cells["descripcion"].Style.ForeColor = Color.FromArgb(0, 100, 0); // Verde oscuro
                            }

                            System.Diagnostics.Debug.WriteLine($"   └─ ✅ EMOJI APLICADO en fila {row.Index}");
                        }
                    }
                    else
                    {
                        // Producto sin oferta - dejar vacío
                        if (row.Cells["ColOferta"] != null)
                        {
                            row.Cells["ColOferta"].Value = "";
                            System.Diagnostics.Debug.WriteLine($"   └─ Celda vacía (sin oferta) en fila {row.Index}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"   └─ Valor NULL o DBNull en fila {row.Index}");
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
                else if (e.KeyCode == Keys.F8) // <-- AÑADIDO: abrir Compras con F8
                {
                    e.SuppressKeyPress = true;
                    _ = AbrirFormularioComprasAsync();
                }
                else if (e.KeyCode == Keys.F || e.KeyCode == Keys.F12)
                {
                    e.SuppressKeyPress = true;

                    // NUEVO: Limpiar el txtBuscarProducto antes de finalizar venta
                    txtBuscarProducto.Text = "";

                    // ÚNICO LUGAR donde se maneja F para finalizar venta
                    btnFinalizarVenta.PerformClick();
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

            // Crear el botón "Anular" pero añadirlo AL MISMO CONTENEDOR que los botones grandes
            // (btnAgregar/btnFinalizarVenta/btnSalir suelen existir en el diseñador y comparten padre).
            btnAnularFactura = new Button
            {
                Text = "Anular",
                Size = new Size(64, 28),                // mucho más pequeño
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Enabled = false,
                Visible = true,
                TabStop = false
            };
            btnAnularFactura.FlatAppearance.BorderSize = 0;
            btnAnularFactura.Click += BtnEliminarFacturaCompleta_Click;

            // Intentar añadir al mismo padre que btnAgregar (si existe), sino al formulario
            Control buttonContainer = btnAgregar?.Parent ?? this;
            buttonContainer.Controls.Add(btnAnularFactura);
            btnAnularFactura.BringToFront();

            // Función de posicionamiento relativa a btnFinalizarVenta (si existe) o btnAgregar
            void ReposicionarAnular()
            {
                try
                {
                    if (btnFinalizarVenta != null && btnFinalizarVenta.Parent != null)
                    {
                        // Si btnFinalizarVenta comparte el mismo padre, posicionar a su derecha
                        if (btnFinalizarVenta.Parent == buttonContainer)
                        {
                            btnAnularFactura.Left = btnFinalizarVenta.Right + 20;
                            btnAnularFactura.Top = btnFinalizarVenta.Top + (btnFinalizarVenta.Height - btnAnularFactura.Height) / 2;
                            return;
                        }
                    }

                    // Fallback: posicionar junto a btnAgregar si está en el mismo contenedor
                    if (btnAgregar != null && btnAgregar.Parent == buttonContainer)
                    {
                        btnAnularFactura.Left = btnAgregar.Right + 8;
                        btnAnularFactura.Top = btnAgregar.Top + (btnAgregar.Height - btnAnularFactura.Height) / 2;
                        return;
                    }

                    // Último recurso: esquina superior derecha del formulario, con márgen
                    btnAnularFactura.Left = Math.Max(8, this.ClientSize.Width - btnAnularFactura.Width - 12);
                    btnAnularFactura.Top = panelHeader.Bottom - btnAnularFactura.Height - 10;
                }
                catch
                {
                    // Silenciar errores de posicionamiento en tiempo de diseño/ejecución temprana
                }
            }

            // Posicionar ahora y al cambiar tamaño del contenedor/formulario
            ReposicionarAnular();
            buttonContainer.SizeChanged += (s, e) => ReposicionarAnular();
            this.Resize += (s, e) => ReposicionarAnular();

            // Asegurar que el título no tape controles si usa DockFill
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

    // MEJORADO: Estilos de selección más contrastantes
    dataGridView1.DefaultCellStyle.BackColor = Color.White;
    dataGridView1.DefaultCellStyle.ForeColor = Color.Black;
    dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
    dataGridView1.DefaultCellStyle.SelectionForeColor = Color.White;
    dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 9F);

    // Estilos de encabezados
    var headerStyle = dataGridView1.ColumnHeadersDefaultCellStyle;
    headerStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
    headerStyle.BackColor = Color.FromArgb(248, 249, 250);
    headerStyle.ForeColor = Color.Black;

    // MEJORADO: Filas alternadas más oscuras para mejor diferenciación
    dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 245, 250);
    dataGridView1.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;
    dataGridView1.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
    dataGridView1.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

    // NUEVO: Configuración adicional para mejor experiencia visual
    dataGridView1.RowTemplate.Height = 28;
    dataGridView1.GridColor = Color.FromArgb(220, 220, 220);

    // ✅✅✅ NUEVO: Desactivar auto-resize inicial para columnas específicas
    dataGridView1.AutoGenerateColumns = true; // Permitir auto-generación
    dataGridView1.ColumnAdded += (s, e) =>
    {
        // Al agregar columna "codigo", fijar ancho inmediatamente
        if (e.Column.Name == "codigo")
        {
            e.Column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            e.Column.Width = 100;
        }
    };
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
                        // Comparar con tolerancia de 0.01
                        if (Math.Abs(precioMostrado - precioActualEnBD) > 0.005m)
                        {
                            // Actualizar de forma asíncrona y no bloquear la UI si hay algún error
                            await ActualizarPrecioProductoAsync(producto["codigo"].ToString(), precioMostrado);
                            System.Diagnostics.Debug.WriteLine($"Precio actualizado en BD: Código={producto["codigo"]}, NuevoPrecio={precioMostrado:F2}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error actualizando precio al cargar producto: {ex.Message}");
                    // No interrumpir al usuario por un fallo al persistir el precio
                }
            }
        }

        // CORREGIDO: Cambiar el tipo de retorno a Task<DataRow>
        private async Task<DataRow> BuscarProductoAsync(string codigo)
        {
            try
            {
                string connectionString = GetConnectionString();
                
                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"SELECT codigo, descripcion, precio, rubro, marca, proveedor, costo, PermiteAcumular, EditarPrecio, cantidad, iva 
                                  FROM Productos WHERE codigo = @codigo";
                    
                    using (var adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@codigo", codigo);
                        DataTable dt = new DataTable();
                        await Task.Run(() => adapter.Fill(dt));
                        
                        return dt.Rows.Count > 0 ? dt.Rows[0] : null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error buscando producto: {ex.Message}");
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
                var query = @"SELECT codigo, descripcion, precio, rubro, marca, proveedor, costo, PermiteAcumular, cantidad, EditarPrecio, iva 
                      FROM Productos WHERE codigo = @codigo";
                using (var adapter = new SqlDataAdapter(query, connection))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@codigo", codigoBuscado);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count == 0)
                    {
                        // Usar MessageBox estándar en lugar de CustomMessageBox
                        var resultado = MessageBox.Show(
                            $"El producto con código '{codigoBuscado}' no existe.\n\n" +
                            "¿Desea agregarlo ahora para continuar con la venta?",
                            "Producto no encontrado",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

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
                    $"ADVERTENCIA: Stock insuficiente\n\n" +
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

            // NUEVO: Verificar si el producto tiene oferta activa ANTES de agregar
            var ofertaAplicable = await BuscarOfertaAplicable(codigoBuscado, cantidadPersonalizada);

            if (ofertaAplicable != null && ofertaAplicable.PrecioOferta > 0)
            {
                // Aplicar precio de oferta
                precioUnitario = ofertaAplicable.PrecioOferta;

                // Mostrar mensaje informativo
                MessageBox.Show(
                    $"🎉 ¡OFERTA APLICADA!\n\n" +
                    $"Producto: {producto["descripcion"]}\n" +
                    $"Oferta: {ofertaAplicable.NombreOferta}\n" +
                    $"Cantidad mínima: {ofertaAplicable.CantidadMinima}\n" +
                    $"Precio normal: {Convert.ToDecimal(producto["precio"]):C2}\n" +
                    $"Precio oferta: {ofertaAplicable.PrecioOferta:C2}\n" +
                    $"Ahorro: {(Convert.ToDecimal(producto["precio"]) - ofertaAplicable.PrecioOferta):C2} ({ofertaAplicable.PorcentajeDescuento:N2}%)",
                    "Oferta Aplicada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            // ✅ PRIORIZAR precio de oferta si existe
            if (ofertaAplicable != null)
            {
                precioUnitario = ofertaAplicable.PrecioOferta;
            }
            else if (esCodigoEspecial)
            {
                // Para códigos especiales, SIEMPRE usar el precio del txtPrecio
                if (decimal.TryParse(txtPrecio.Text, out decimal precioEspecial))
                {
                    precioUnitario = precioEspecial;

                    // ACTUALIZAR el precio en la tabla Productos SOLO si el producto permite editar precio
                    bool editarPrecioFlag = false;
                    if (producto.Table.Columns.Contains("EditarPrecio") && producto["EditarPrecio"] != DBNull.Value)
                        editarPrecioFlag = Convert.ToBoolean(producto["EditarPrecio"]);

                    if (editarPrecioFlag)
                    {
                        // Usar el helper asíncrono
                        await ActualizarPrecioProductoAsync(codigoBuscado, precioUnitario);
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

                    // Si el campo precio estaba habilitado (producto con EditarPrecio),
                    // actualizar el valor persistente en Productos para mantener consistencia.
                    if (txtPrecio.Enabled)
                    {
                        try
                        {
                            await ActualizarPrecioProductoAsync(codigoBuscado, precioUnitario);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error actualizando precio al agregar: {ex.Message}");
                        }
                    }
                }
                else
                {
                    precioUnitario = Convert.ToDecimal(producto["precio"]);
                }
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
                                    // Sin oferta: limpiar los campos
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
                                if (ofertaAplicable != null)
                                {
                                    cmd.Parameters.AddWithValue("@IdOferta", ofertaAplicable.Id);
                                    cmd.Parameters.AddWithValue("@NombreOferta", ofertaAplicable.NombreOferta ?? "");
                                    cmd.Parameters.AddWithValue("@PrecioOriginal", precioOriginal);
                                    cmd.Parameters.AddWithValue("@PrecioConOferta", precioUnitario);
                                    cmd.Parameters.AddWithValue("@DescuentoAplicado", Math.Round(precioOriginal - precioUnitario, 2));
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
            // Abrir un poco más ancho si la ventana de diseño es más pequeña
            const int anchoDeseado = 850;
            if (this.ClientSize.Width < anchoDeseado)
            {
                this.ClientSize = new Size(anchoDeseado, this.ClientSize.Height);
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

            // Reposicionar el botón anular ahora que el tamaño inicial se ha establecido
            //ReposicionarBotonAnular();

            // (resto del Ventas_Load original sigue igual...)
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
            dataGridView1.DefaultCellStyle.BackColor = Color.White;
            dataGridView1.DefaultCellStyle.ForeColor = Color.Black;
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
            // NOTA: Ya se configura en el constructor mediante ConfigurarEventHandlers(), 
            // por eso evitamos hacerlo nuevamente para no duplicar handlers.
        }

        private void ConfigurarPanelFooter()
        {
            // Crear el panel footer programáticamente
            Panel panelFooter = new Panel();
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Height = 65; // ✅ REDUCIDO: de 100 a 65 píxeles
            panelFooter.BackColor = Color.FromArgb(0, 120, 215);

            // Configurar lbCantidadProductos (dock left)
            lbCantidadProductos.AutoSize = false;
            lbCantidadProductos.TextAlign = ContentAlignment.MiddleLeft;
            lbCantidadProductos.Dock = DockStyle.Left;
            lbCantidadProductos.Width = 220;
            lbCantidadProductos.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lbCantidadProductos.ForeColor = Color.White;
            lbCantidadProductos.Text = "Productos: 0";

            // ✅ NUEVO: Panel contenedor para centrar verticalmente el RichTextBox
            Panel panelTotalContainer = new Panel
            {
                Dock = DockStyle.Right,
                Width = 500,
                BackColor = Color.FromArgb(0, 120, 215),
                Padding = new Padding(0, 12, 20, 12) // ✅ Padding vertical para centrar
            };

            // RichTextBox para totales - ✅ MODIFICADO: Ahora dentro del contenedor
            rtbTotal = new RichTextBox
            {
                AutoSize = false,
                Dock = DockStyle.Fill, // ✅ CAMBIADO: Fill en lugar de Right
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
            panelFooter.Controls.Add(panelTotalContainer); // ✅ CAMBIADO: Agregar el contenedor en lugar del RichTextBox directamente
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
                // ✅ ASEGURAR que EsOferta está en la consulta
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

            // ✅ Ocultar columnas internas
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

            // ✅✅✅ NUEVO: OCULTAR columna IvaCalculado (IVA $)
            if (dataGridView1.Columns["IvaCalculado"] != null)
            {
                dataGridView1.Columns["IvaCalculado"].Visible = false;
            }

            // Configurar encabezados
            if (dataGridView1.Columns["codigo"] != null)
            {
                dataGridView1.Columns["codigo"].HeaderText = "Codigo";
                dataGridView1.Columns["codigo"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridView1.Columns["codigo"].Width = 100;
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
                dataGridView1.Columns["precio"].Width = 100;
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
                colTotal.DefaultCellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);
                colTotal.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                colTotal.Width = 120;
            }

            // IVA%: ancho fijo
            if (dataGridView1.Columns["PorcentajeIva"] != null)
            {
                var colIvaPct = dataGridView1.Columns["PorcentajeIva"];
                colIvaPct.HeaderText = "IVA%";
                colIvaPct.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                colIvaPct.Width = 60;
                colIvaPct.MinimumWidth = 50;
                colIvaPct.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Cantidad: ancho fijo pequeño
            if (dataGridView1.Columns["cantidad"] != null)
            {
                dataGridView1.Columns["cantidad"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridView1.Columns["cantidad"].HeaderText = "Cant.";
                dataGridView1.Columns["cantidad"].Width = 50;
                dataGridView1.Columns["cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // ✅ CRÍTICO: Llamar a FormatearDataGridView DESPUÉS de configurar todas las columnas
            FormatearDataGridView();

            // ✅✅✅ NUEVO: FORZAR ancho de ColOferta DESPUÉS de formatear
            if (dataGridView1.Columns["ColOferta"] != null)
            {
                dataGridView1.Columns["ColOferta"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridView1.Columns["ColOferta"].Width = 35;
                dataGridView1.Columns["ColOferta"].MinimumWidth = 35;
                dataGridView1.Columns["ColOferta"].Resizable = DataGridViewTriState.False;
                dataGridView1.Columns["ColOferta"].DisplayIndex = 0; // ✅ ASEGURAR que es la primera

                System.Diagnostics.Debug.WriteLine("✅ Ancho ColOferta FORZADO a 35px después de DataSource");
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

            // Mostrar en RichTextBox
            rtbTotal.Clear();
            rtbTotal.SelectionAlignment = HorizontalAlignment.Right;
            rtbTotal.SelectionFont = new Font("Segoe UI", 22F, FontStyle.Bold);
            rtbTotal.AppendText($"TOTAL: {sumaTotal:C2}\n");
        }

        // NUEVO: Método async separado para la impresión
        public async Task ImprimirConServicioAsync(SeleccionImpresionForm seleccion)
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

                using (var seleccionModal = new SeleccionImpresionForm(importeTotal, this))
                {
                    // ✅ CRÍTICO: Configurar el callback ANTES de mostrar el modal
                    seleccionModal.OnProcesarVenta = async (tipoComprobante, formaPago, cuitCliente,
                        caeNumero, caeVencimiento, numeroFacturaAfip, numeroFormateado) =>
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"[VENTAS] OnProcesarVenta - Tipo: {tipoComprobante}, FormaPago: {formaPago}");

                            // Guardar en BD
                            await GuardarFacturaEnBD(
                                tipoComprobante,
                                formaPago,
                                cuitCliente,
                                caeNumero,
                                caeVencimiento,
                                numeroFacturaAfip,
                                numeroFormateado,
                                seleccionModal.EsPagoMultiple ? seleccionModal.PagosMultiples : null
                            );

                            System.Diagnostics.Debug.WriteLine("[VENTAS] ✅ Factura guardada en BD exitosamente");

                            // ✅✅✅ CRÍTICO: NO IMPRIMIR AQUÍ - YA LO HIZO SeleccionImpresionForm
                            // La impresión ya se realizó en ProcesarRemito() o ProcesarFacturaElectronica()
                            // según la configuración de usarVistaPrevia

                            // ELIMINADO:
                            // await ImprimirSinModal(...); 

                            System.Diagnostics.Debug.WriteLine("[VENTAS] ✅ Proceso de venta completado (sin impresión adicional)");
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

                        // ✅ NUEVO: Verificar si finalizó sin impresión
                        if (seleccionModal.FinalizadoSinImpresion)
                        {
                            System.Diagnostics.Debug.WriteLine("[VENTAS] ℹ️ Venta finalizada sin impresión");
                            MessageBox.Show(
                                "Venta registrada exitosamente sin impresión de comprobante.",
                                "Venta Finalizada",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[VENTAS] ℹ️ Comprobante: {seleccionModal.OpcionSeleccionada}");
                        }

                        // Limpiar y reiniciar para nueva venta
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
            if (procesandoEliminacion) return;

            if (nroRemitoActual <= 0)
            {
                MessageBox.Show("No hay un remito/factura seleccionado para eliminar.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Asegurar que tenemos las líneas cargadas
            if (remitoActual == null || remitoActual.Rows.Count == 0)
            {
                // Intentar recargar
                CargarVentasActuales();
                if (remitoActual == null || remitoActual.Rows.Count == 0)
                {
                    MessageBox.Show("No se encontraron productos para el remito actual.", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            // Confirmación por parte del usuario (mostrar resumen mínimo)
            decimal importeTotal = 0m;
            foreach (DataRow r in remitoActual.Rows)
            {
                if (r["total"] != null && decimal.TryParse(r["total"].ToString(), out decimal t))
                    importeTotal += t;
            }

            var confirmar = MessageBox.Show(
                $"¿Confirma la eliminación TOTAL del remito/factura N° {nroRemitoActual}?\n\n" +
                $"Se eliminarán {remitoActual.Rows.Count} líneas por un total de {importeTotal:C2}.\n\n" +
                "Esta acción devolverá las cantidades vendidas al stock y registrará una auditoría.",
                "Confirmar eliminación completa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirmar != DialogResult.Yes)
                return;

            // Verificar permisos si aplica
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
                        "No tienes permisos para eliminar facturas completas.\n" +
                        "Contacta a un administrador si necesitas realizar esta acción.",
                        "Permisos Insuficientes",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            procesandoEliminacion = true;

            try
            {
                string connectionString = GetConnectionString();
                string usuarioNombre = AuthenticationService.SesionActual?.Usuario?.NombreUsuario ?? Environment.UserName;
                int numeroCajero = AuthenticationService.SesionActual?.Usuario?.NumeroCajero ?? 1;

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync(); // ✅ USAR OpenAsync

                    // ✅✅✅ CRÍTICO: Configurar ARITHABORT ANTES de la transacción
                    using (var cmdConfig = new SqlCommand("SET ARITHABORT ON; SET ANSI_WARNINGS ON;", connection))
                    {
                        await cmdConfig.ExecuteNonQueryAsync();
                    }

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // ... resto del código de eliminación ...

                            transaction.Commit();
                            LimpiarYReiniciarVenta();
                            System.Diagnostics.Debug.WriteLine($"✅ Factura/remito {nroRemitoActual} eliminada por {usuarioNombre}. Stock devuelto donde aplica.");
                        }
                        catch (Exception exTx)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"❌ Error al eliminar factura completa: {exTx.Message}");
                            MessageBox.Show($"Error al eliminar la factura completa: {exTx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al procesar eliminación completa: {ex.Message}");
                MessageBox.Show($"Error al procesar la eliminación: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                procesandoEliminacion = false;
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
        private async Task GuardarFacturaEnBD(string tipoFactura, string formaPago, string cuitCliente = "", string caeNumero = "", DateTime? caeVencimiento = null, int numeroFacturaAfip = 0, string numeroFormateado = "", List<Comercio.NET.Controles.MultiplePagosControl.DetallePago> pagosMultiples = null)
        {
            if (remitoActual == null || remitoActual.Rows.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("GuardarFacturaEnBD: no hay remitoActual para guardar.");
                return;
            }

            // Calcular totales desde remitoActual (defensivo)
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
                            // Insertar cabecera en Facturas (mapeado a tu esquema)
                            var insertFacturaSql = @"
                        INSERT INTO Facturas
                            (NumeroRemito, Fecha, Hora, ImporteTotal, FormadePago, esCtaCte, CtaCteNombre,
                             Cajero, TipoFactura, CAENumero, CAEVencimiento, CUITCliente, NroFactura, UsuarioVenta, IVA)
                        VALUES
                            (@NumeroRemito, @Fecha, @Hora, @ImporteTotal, @FormadePago, @esCtaCte, @CtaCteNombre,
                             @Cajero, @TipoFactura, @CAENumero, @CAEVencimiento, @CUITCliente, @NroFactura, @UsuarioVenta, @IVA);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                            int facturaId;
                            using (var cmd = new SqlCommand(insertFacturaSql, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@NumeroRemito", nroRemitoActual);
                                cmd.Parameters.AddWithValue("@Fecha", DateTime.Now.Date);
                                cmd.Parameters.AddWithValue("@Hora", DateTime.Now);
                                cmd.Parameters.AddWithValue("@ImporteTotal", totalFactura);
                                cmd.Parameters.AddWithValue("@FormadePago", string.IsNullOrEmpty(formaPago) ? (object)DBNull.Value : formaPago);
                                cmd.Parameters.AddWithValue("@esCtaCte", chkEsCtaCte.Checked);
                                cmd.Parameters.AddWithValue("@CtaCteNombre", chkEsCtaCte.Checked ? (object)cbnombreCtaCte.Text : DBNull.Value);

                                int numeroCajero = AuthenticationService.SesionActual?.Usuario?.NumeroCajero ?? 0;
                                cmd.Parameters.AddWithValue("@Cajero", numeroCajero.ToString());

                                cmd.Parameters.AddWithValue("@TipoFactura", string.IsNullOrEmpty(tipoFactura) ? (object)DBNull.Value : tipoFactura);
                                cmd.Parameters.AddWithValue("@CAENumero", string.IsNullOrEmpty(caeNumero) ? (object)DBNull.Value : caeNumero);
                                cmd.Parameters.AddWithValue("@CAEVencimiento", caeVencimiento.HasValue ? (object)caeVencimiento.Value : DBNull.Value);
                                cmd.Parameters.AddWithValue("@CUITCliente", string.IsNullOrEmpty(cuitCliente) ? (object)DBNull.Value : cuitCliente);
                                cmd.Parameters.AddWithValue("@NroFactura", !string.IsNullOrEmpty(numeroFormateado) ? (object)numeroFormateado : (numeroFacturaAfip > 0 ? (object)numeroFacturaAfip.ToString() : (object)DBNull.Value));
                                cmd.Parameters.AddWithValue("@UsuarioVenta", ObtenerUsuarioActual());
                                cmd.Parameters.AddWithValue("@IVA", ivaTotal);

                                var result = await cmd.ExecuteScalarAsync();
                                facturaId = result != null && int.TryParse(result.ToString(), out int id) ? id : 0;
                                System.Diagnostics.Debug.WriteLine($"GuardarFacturaEnBD: Factura insertada con Id={facturaId}");
                            }

                            // Insertar registros en DetallesPagoFactura
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
                                        cmdPago.Parameters.AddWithValue("@IdFactura", facturaId);
                                        cmdPago.Parameters.AddWithValue("@MedioPago", pago.MedioPago ?? "");
                                        cmdPago.Parameters.AddWithValue("@Importe", pago.Importe);

                                        // GRABAR Observaciones correctamente (usar DBNull si vacío)
                                        var obsVal = string.IsNullOrWhiteSpace(pago.Observaciones) ? (object)DBNull.Value : pago.Observaciones;
                                        cmdPago.Parameters.AddWithValue("@Observaciones", obsVal);

                                        var fechaPago = pago.Fecha != default ? pago.Fecha : DateTime.Now;
                                        cmdPago.Parameters.AddWithValue("@FechaPago", fechaPago);
                                        cmdPago.Parameters.AddWithValue("@Usuario", ObtenerUsuarioActual());
                                        cmdPago.Parameters.AddWithValue("@NumeroRemito", nroRemitoActual);
                                        await cmdPago.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                            else
                            {
                                // Pago simple: registrar un único detalle con la forma de pago y el total
                                using (var cmdPago = new SqlCommand(insertDetalleSql, connection, transaction))
                                {
                                    cmdPago.Parameters.AddWithValue("@IdFactura", facturaId);
                                    cmdPago.Parameters.AddWithValue("@MedioPago", string.IsNullOrEmpty(formaPago) ? "Desconocido" : formaPago);
                                    cmdPago.Parameters.AddWithValue("@Importe", totalFactura);
                                    cmdPago.Parameters.AddWithValue("@Observaciones", DBNull.Value);
                                    cmdPago.Parameters.AddWithValue("@FechaPago", DateTime.Now);
                                    cmdPago.Parameters.AddWithValue("@Usuario", ObtenerUsuarioActual());
                                    cmdPago.Parameters.AddWithValue("@NumeroRemito", nroRemitoActual);
                                    await cmdPago.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                        }
                        catch (Exception exTx)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"GuardarFacturaEnBD ERROR: {exTx.Message}");
                            MessageBox.Show($"Error al guardar la factura en la base de datos: {exTx.Message}", "Error BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GuardarFacturaEnBD (conexion) ERROR: {ex.Message}");
                MessageBox.Show($"Error al guardar la factura: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    case SeleccionImpresionForm.OpcionImpresion.FacturaB:
                        config.TipoComprobante = "FacturaB";
                        config.NumeroComprobante = FormatearNumeroFacturaParaBD(6, 1, numeroFacturaAfip);
                        config.CAE = caeNumero;
                        config.CAEVencimiento = caeVencimiento;
                        break;
                    case SeleccionImpresionForm.OpcionImpresion.FacturaA:
                        config.TipoComprobante = "FacturaA";
                        config.NumeroComprobante = FormatearNumeroFacturaParaBD(1, 1, numeroFacturaAfip);
                        config.CAE = caeNumero;
                        config.CAEVencimiento = caeVencimiento;
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

        // AGREGAR al final de la clase Ventas (después de los métodos existentes)
        /// <summary>
        /// Busca si existe una oferta activa aplicable para el producto y cantidad especificada
        /// </summary>
        private async Task<OfertaProducto> BuscarOfertaAplicable(string codigoProducto, int cantidad)
        {
            try
            {
                string connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    // ✅ MODIFICADO: JOIN con productos usando ID
                    var query = @"
                SELECT TOP 1
                    o.Id,
                    o.Nombre AS NombreOferta,
                    o.TipoOferta,
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
                ORDER BY d.PrecioOferta ASC";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@CodigoProducto", codigoProducto);
                        cmd.Parameters.AddWithValue("@Cantidad", cantidad);

                        await connection.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new OfertaProducto
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    NombreOferta = reader["NombreOferta"].ToString(),
                                    TipoOferta = reader["TipoOferta"].ToString(),
                                    CantidadMinima = Convert.ToInt32(reader["CantidadMinima"]),
                                    PrecioOferta = Convert.ToDecimal(reader["PrecioOferta"]),
                                    PorcentajeDescuento = Convert.ToDecimal(reader["PorcentajeDescuento"])
                                };
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
        }
    }
}