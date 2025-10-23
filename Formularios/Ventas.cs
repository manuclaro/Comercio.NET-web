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

        public Ventas()
        {
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
                // MODIFICADO: Usar el ID único en lugar de código + nrofactura
                var query = @"UPDATE Ventas 
                              SET cantidad = @nuevaCantidad, 
                                  total = @nuevaCantidad * precio 
                              WHERE id = @idVenta";
                              
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@nuevaCantidad", nuevaCantidad);
                    cmd.Parameters.AddWithValue("@idVenta", idVenta);
                    
                    connection.Open();
                    await cmd.ExecuteNonQueryAsync();
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
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Crear/verificar tabla de auditoría si no existe (usando la tabla existente)
                        //await VerificarTablaAuditoriaProductosEliminados(connection, transaction);
                        
                        // 2. Registrar la auditoría en AuditoriaProductosEliminados
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
                            cmd.Parameters.AddWithValue("@Cantidad", cantidadAEliminar); // Cantidad eliminada
                            cmd.Parameters.AddWithValue("@TotalEliminado", precio * cantidadAEliminar);
                            cmd.Parameters.AddWithValue("@NumeroFactura", nroRemitoActual);
                            cmd.Parameters.AddWithValue("@FechaHoraVentaOriginal", DateTime.Now); // Fecha de la venta original
                            cmd.Parameters.AddWithValue("@FechaEliminacion", DateTime.Now);
                            cmd.Parameters.AddWithValue("@MotivoEliminacion", motivo);
                            cmd.Parameters.AddWithValue("@EsCtaCte", chkEsCtaCte.Checked);
                            cmd.Parameters.AddWithValue("@NombreCtaCte", chkEsCtaCte.Checked ? (object)cbnombreCtaCte.Text : DBNull.Value);
                            cmd.Parameters.AddWithValue("@UsuarioEliminacion", usuario);
                            cmd.Parameters.AddWithValue("@NumeroCajero", numeroCajero);
                            cmd.Parameters.AddWithValue("@NombreEquipo", Environment.MachineName);
                            cmd.Parameters.AddWithValue("@EsEliminacionCompleta", eliminarCompleto);
                            cmd.Parameters.AddWithValue("@CantidadOriginal", cantidadTotal); // Cantidad original
                            
                            await cmd.ExecuteNonQueryAsync();
                        }
                        
                        // 3. CORREGIDO: Procesar eliminación en la venta usando ID único
                        if (eliminarCompleto)
                        {
                            // Eliminar la línea completa usando el ID único
                            var queryEliminar = @"DELETE FROM Ventas WHERE id = @idVenta";
                                                
                            using (var cmd = new SqlCommand(queryEliminar, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@idVenta", idVenta);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // Actualizar la cantidad restante usando el ID único
                            int cantidadRestante = cantidadTotal - cantidadAEliminar;
                            var queryActualizar = @"UPDATE Ventas 
                                                   SET cantidad = @cantidadRestante,
                                                       total = @cantidadRestante * precio
                                                   WHERE id = @idVenta";
                                                   
                            using (var cmd = new SqlCommand(queryActualizar, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@cantidadRestante", cantidadRestante);
                                cmd.Parameters.AddWithValue("@idVenta", idVenta);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        
                        transaction.Commit();
                        
                        System.Diagnostics.Debug.WriteLine($"✅ Eliminación procesada correctamente - ID: {idVenta}, Código: {codigo}, Eliminado: {cantidadAEliminar}/{cantidadTotal}, Completo: {eliminarCompleto}");
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
                dataGridView1.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 171 - 120); // CAMBIADO: -120 en lugar de -60
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

            ConfigurarEventosPrecio();
        }

        // CORREGIDO: Implementar correctamente los eventos de txtPrecio
        private void ConfigurarEventosPrecio()
        {
            if (txtPrecio != null)
            {
                // CORREGIDO: Configurar evento Enter para txtPrecio
                txtPrecio.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        // Si el precio está habilitado y tiene valor válido, ir al botón agregar
                        if (txtPrecio.Enabled && !string.IsNullOrWhiteSpace(txtPrecio.Text))
                        {
                            btnAgregar.Focus();
                        }
                        else
                        {
                            // Si no, seguir la navegación normal
                            this.SelectNextControl(txtPrecio, true, true, true, true);
                        }
                    }
                };

                txtPrecio.KeyPress += (s, e) =>
                {
                    // Permitir solo números, punto decimal, coma y teclas de control
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',')
                    {
                        e.Handled = true;
                    }

                    // Reemplazar coma por punto para consistencia
                    if (e.KeyChar == ',')
                    {
                        e.KeyChar = '.';
                    }

                    // Permitir solo un punto decimal
                    if (e.KeyChar == '.' && (s as TextBox).Text.Contains('.'))
                    {
                        e.Handled = true;
                    }
                };

                txtPrecio.Enter += (s, e) => txtPrecio.SelectAll();
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
                var query = @"SELECT codigo, descripcion, precio, rubro, marca, proveedor, costo, PermiteAcumular, cantidad 
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
            panelFooter.Height = 100; // AUMENTADO: era 80, ahora 120 para mostrar múltiples alícuotas
            panelFooter.BackColor = Color.FromArgb(0, 120, 215);

            // Configurar lbCantidadProductos
            lbCantidadProductos.AutoSize = false;
            lbCantidadProductos.TextAlign = ContentAlignment.MiddleLeft;
            lbCantidadProductos.Dock = DockStyle.Left;
            lbCantidadProductos.Width = 250;
            lbCantidadProductos.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lbCantidadProductos.ForeColor = Color.White;
            lbCantidadProductos.Text = "Productos: 0";

            // MODIFICADO: Usar RichTextBox más alto para mostrar todas las alícuotas
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

            // MODIFICADO: Ajustar el DataGridView para el panel más alto
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.Dock = DockStyle.None;
            dataGridView1.Location = new Point(0, 171);
            dataGridView1.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 171 - 120); // CAMBIADO: -120 en lugar de -80
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

            if (dataGridView1.Columns["codigo"] != null)
            {
                dataGridView1.Columns["codigo"].HeaderText = "Codigo";
            }

            if (dataGridView1.Columns["descripcion"] != null)
            {
                dataGridView1.Columns["descripcion"].HeaderText = "Descripcion";
            }

            if (dataGridView1.Columns["precio"] != null)
            {
                dataGridView1.Columns["precio"].HeaderText = "Precio";
            }

            if (dataGridView1.Columns["total"] != null)
            {
                dataGridView1.Columns["total"].HeaderText = "Total";
            }

            // NUEVO: Cambiar el título de la columna cantidad a "Cant."
            if (dataGridView1.Columns["cantidad"] != null)
            {
                dataGridView1.Columns["cantidad"].HeaderText = "Cant.";
                dataGridView1.Columns["cantidad"].Width = 50;
            }

            // Configurar PorcentajeIva e IvaCalculado si existen
            if (dataGridView1.Columns["PorcentajeIva"] != null)
            {
                dataGridView1.Columns["PorcentajeIva"].HeaderText = "IVA%";
                dataGridView1.Columns["PorcentajeIva"].Width = 30;
                dataGridView1.Columns["PorcentajeIva"].DefaultCellStyle.Format = "N2";
                dataGridView1.Columns["PorcentajeIva"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dataGridView1.Columns["PorcentajeIva"].DefaultCellStyle.ForeColor = Color.FromArgb(25, 118, 210);
                dataGridView1.Columns["PorcentajeIva"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                dataGridView1.Columns["PorcentajeIva"].DisplayIndex = 6;
            }

            if (dataGridView1.Columns["IvaCalculado"] != null)
            {
                dataGridView1.Columns["IvaCalculado"].HeaderText = "IVA $";
                dataGridView1.Columns["IvaCalculado"].Width = 50;
                dataGridView1.Columns["IvaCalculado"].DefaultCellStyle.Format = "C2";
                dataGridView1.Columns["IvaCalculado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dataGridView1.Columns["IvaCalculado"].DisplayIndex = 7;
            }

            // Ajustar anchos de columnas existentes para hacer espacio
            if (dataGridView1.Columns["descripcion"] != null)
                dataGridView1.Columns["descripcion"].Width = 240;
            if (dataGridView1.Columns["precio"] != null)
                dataGridView1.Columns["precio"].Width = 100;
            if (dataGridView1.Columns["cantidad"] != null)
                dataGridView1.Columns["cantidad"].Width = 50;
            if (dataGridView1.Columns["codigo"] != null)
            {
                dataGridView1.Columns["codigo"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridView1.Columns["codigo"].Width = 100;
            }
            if (dataGridView1.Columns["total"] != null)
                dataGridView1.Columns["total"].Width = 100;

            // Actualizar totales
            lbCantidadProductos.Text = $"Productos: {dataGridView1.Rows.Count}";

            decimal sumaTotal = 0;
            decimal sumaIva = 0;

            var ivaPorAlicuota = new Dictionary<decimal, decimal>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                // total
                if (row.Cells["total"]?.Value != null && decimal.TryParse(row.Cells["total"].Value.ToString(), out decimal valorTotal))
                    sumaTotal += valorTotal;

                // iva de la fila (si existe la columna y el valor)
                decimal valorIva = 0;
                if (dataGridView1.Columns.Contains("IvaCalculado") && row.Cells["IvaCalculado"]?.Value != null &&
                    decimal.TryParse(row.Cells["IvaCalculado"].Value.ToString(), out decimal tmpIva))
                {
                    valorIva = tmpIva;
                    sumaIva += valorIva;
                }

                // agrupar por alícuota (si existe)
                if (dataGridView1.Columns.Contains("PorcentajeIva") && row.Cells["PorcentajeIva"]?.Value != null &&
                    decimal.TryParse(row.Cells["PorcentajeIva"].Value.ToString(), out decimal alicuota))
                {
                    if (!ivaPorAlicuota.ContainsKey(alicuota))
                        ivaPorAlicuota[alicuota] = 0;
                    ivaPorAlicuota[alicuota] += valorIva;
                }
            }

            // Mostrar en RichTextBox
            rtbTotal.Clear();
            rtbTotal.SelectionAlignment = HorizontalAlignment.Right;

            rtbTotal.SelectionFont = new Font("Segoe UI", 22F, FontStyle.Bold);
            rtbTotal.AppendText($"TOTAL: {sumaTotal:C2}\n");

            if (ivaPorAlicuota.Any())
            {
                rtbTotal.SelectionFont = new Font("Segoe UI", 9F, FontStyle.Regular);
                foreach (var kvp in ivaPorAlicuota.OrderBy(x => x.Key))
                {
                    if (kvp.Value > 0)
                        rtbTotal.AppendText($"IVA {kvp.Key:N2}%: {kvp.Value:C2}\n");
                }

                rtbTotal.SelectionFont = new Font("Segoe UI", 10F, FontStyle.Bold);
                rtbTotal.AppendText($"IVA Total: {sumaIva:C2}");
            }
            else
            {
                rtbTotal.SelectionFont = new Font("Segoe UI", 10F, FontStyle.Regular);
                rtbTotal.AppendText($"IVA: {sumaIva:C2}");
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

                System.Diagnostics.Debug.WriteLine("=== INICIO IMPRESIÓN ===");
                System.Diagnostics.Debug.WriteLine($"TipoComprobante configurado: {config.TipoComprobante}");
                System.Diagnostics.Debug.WriteLine($"NumeroComprobante: {config.NumeroComprobante}");
                System.Diagnostics.Debug.WriteLine($"CAE: {config.CAE}");
                System.Diagnostics.Debug.WriteLine($"===========================");

                // CORREGIDO: Usar await con el servicio de impresión
                using (var ticketService = new TicketPrintingService())
                {
                    await ticketService.ImprimirTicket(remitoActual, config);
                }

                System.Diagnostics.Debug.WriteLine("? Impresión completada correctamente");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"Error en impresión: {ex.Message}");
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
            remitoIncrementado = false;

            if (remitoActual == null || remitoActual.Rows.Count == 0)
                return;

            // Calcular el importe total antes de abrir el modal
            decimal importeTotal = 0;
            decimal ivaTotal = 0;

            foreach (DataRow row in remitoActual.Rows)
            {
                if (row["total"] != null && decimal.TryParse(row["total"].ToString(), out decimal valorTotal))
                    importeTotal += valorTotal;

                // Comprobar existencia de la columna y que no sea DBNull
                if (row.Table != null && row.Table.Columns.Contains("IvaCalculado") && !row.IsNull("IvaCalculado"))
                {
                    if (decimal.TryParse(row["IvaCalculado"].ToString(), out decimal valorIva))
                        ivaTotal += valorIva;
                }
            }

            // Variables para almacenar los datos del procesamiento
            List<Comercio.NET.Controles.MultiplePagosControl.DetallePago> pagosMultiples = null;
            SeleccionImpresionForm.OpcionImpresion opcionSeleccionada = SeleccionImpresionForm.OpcionImpresion.Ninguna;
            SeleccionImpresionForm.OpcionPago opcionPagoSeleccionada = SeleccionImpresionForm.OpcionPago.Efectivo;
            string caeNumero = "";
            DateTime? caeVencimiento = null;
            int numeroFacturaAfip = 0;
            bool procesadoExitosamente = false;

            try
            {
                using (var seleccion = new SeleccionImpresionForm(importeTotal, this))
                {
                    // Pasar tokens si los tenemos
                    seleccion.TokenAfip = this.token;
                    seleccion.SignAfip = this.sign;

                    seleccion.OnProcesarVenta = async (tipoFactura, formaPago, cuitCliente, caeNumeroParam, caeVencimientoParam, numeroFacturaAfipParam, numeroFormateado) =>
                    {
                        // Capturar pagos múltiples si aplica
                        if (seleccion.EsPagoMultiple)
                        {
                            pagosMultiples = seleccion.PagosMultiples;
                        }

                        // Guardar en BD (implementación mínima / placeholder más abajo)
                        await GuardarFacturaEnBD(tipoFactura, formaPago, cuitCliente, caeNumeroParam, caeVencimientoParam, numeroFacturaAfipParam, numeroFormateado, pagosMultiples);

                        // Guardar para impresión posterior
                        opcionSeleccionada = seleccion.OpcionSeleccionada;
                        opcionPagoSeleccionada = seleccion.OpcionPagoSeleccionada;
                        caeNumero = caeNumeroParam ?? "";
                        caeVencimiento = caeVencimientoParam;
                        numeroFacturaAfip = numeroFacturaAfipParam;
                        procesadoExitosamente = true;
                    };

                    var resultado = seleccion.ShowDialog(this);

                    // Si el usuario seleccionó Finalizar (sin impresión) desde el modal,
                    // el callback ya guardó y devolvió DialogResult.OK; dejamos procesadoExitosamente en true.
                    if (resultado == DialogResult.OK && procesadoExitosamente)
                    {
                        // Si el modal indicó explícitamente "FinalizadoSinImpresion", NO imprimir
                        if (seleccion.FinalizadoSinImpresion)
                        {
                            LimpiarYReiniciarVenta();
                            return;
                        }

                        // Proceder con impresión según la opción seleccionada
                        await ImprimirSinModal(opcionSeleccionada, opcionPagoSeleccionada, caeNumero, caeVencimiento, numeroFacturaAfip);

                        // Limpiar y reiniciar
                        LimpiarYReiniciarVenta();
                    }
                    else
                    {
                        // Modal cancelado o no procesado
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en btnFinalizarVenta_Click: {ex.Message}");
                MessageBox.Show($"Error finalizando la venta: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                if (r.Table.Columns.Contains("total") && r["total"] != DBNull.Value && decimal.TryParse(r["total"].ToString(), out decimal t))
                    totalFactura += t;
                if (r.Table.Columns.Contains("IvaCalculado") && r["IvaCalculado"] != DBNull.Value && decimal.TryParse(r["IvaCalculado"].ToString(), out decimal iv))
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
    }
}