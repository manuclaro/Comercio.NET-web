using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class frmAgregarProducto : Form
    {
        public enum ModoFormulario { Agregar, Modificar }
        public enum OrigenLlamada { Productos, Ventas } // NUEVO: Para identificar el origen
        
        public ModoFormulario Modo { get; set; } = ModoFormulario.Agregar;
        public OrigenLlamada Origen { get; set; } = OrigenLlamada.Productos; // NUEVO: Por defecto desde Productos
        public string CodigoOriginal { get; set; } // Para identificar el registro a modificar

        // NO DECLARAR CONTROLES AQUÍ - ya están en el diseñador

        public frmAgregarProducto()
        {
            InitializeComponent();
            
            ConfigurarFormulario();
            this.StartPosition = FormStartPosition.CenterParent;

            // Crear solo los controles que no existen
            CrearControlesFaltantes();

            // NUEVO: Esperar un poco y luego organizar
            this.Load += (s, e) => {
                // Pequeña pausa para asegurar que todos los controles estén listos
                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                timer.Interval = 50; // 50ms
                timer.Tick += (sender, args) => {
                    timer.Stop();
                    timer.Dispose();
                    OrganizarControles();
                    ConfigurarTabIndex();
                    AsignarEventosControles();
                };
                timer.Start();
            };
        }

        private void CrearControlesFaltantes()
        {
            // Crear todos los labels que faltan
            var labelsNeeded = new[]
            {
                new { Name = "lblCodigo", Text = "Código:" },
                new { Name = "lblDescripcion", Text = "Descripción:" },
                new { Name = "lblRubro", Text = "Rubro:" },
                new { Name = "lblMarca", Text = "Marca:" },
                new { Name = "lblCosto", Text = "Costo:" },
                new { Name = "lblPorcentaje", Text = "Porcentaje:" },
                new { Name = "lblPrecio", Text = "Precio:" },
                new { Name = "lblStock", Text = "Stock:" },
                new { Name = "lblProveedor", Text = "Proveedor:" }
            };

            foreach (var labelInfo in labelsNeeded)
            {
                if (this.Controls.Find(labelInfo.Name, true).Length == 0)
                {
                    var label = new Label 
                    { 
                        Name = labelInfo.Name, 
                        Text = labelInfo.Text,
                        AutoSize = false
                    };
                    this.Controls.Add(label);
                }
            }

            // Crear todos los textboxes que faltan
            var textBoxesNeeded = new[]
            {
                new { Name = "txtCodigo", ReadOnly = false },
                new { Name = "txtDescripcion", ReadOnly = false },
                new { Name = "txtRubro", ReadOnly = false },
                new { Name = "txtMarca", ReadOnly = false },
                new { Name = "txtCosto", ReadOnly = false },
                new { Name = "txtPorcentaje", ReadOnly = false },
                new { Name = "txtPrecio", ReadOnly = false }, // CAMBIADO: Ahora editable desde Ventas
                new { Name = "txtCantidad", ReadOnly = false },
                new { Name = "txtProveedor", ReadOnly = false }
            };

            foreach (var textBoxInfo in textBoxesNeeded)
            {
                if (this.Controls.Find(textBoxInfo.Name, true).Length == 0)
                {
                    var textBox = new TextBox 
                    { 
                        Name = textBoxInfo.Name,
                        ReadOnly = textBoxInfo.ReadOnly
                    };
                    this.Controls.Add(textBox);
                }
            }

            // Crear botones que faltan
            if (this.Controls.Find("btnGuardar", true).Length == 0)
            {
                var btnGuardar = new Button { Name = "btnGuardar", Text = "Guardar" };
                this.Controls.Add(btnGuardar);
            }

            if (this.Controls.Find("btnSalirModal", true).Length == 0)
            {
                var btnSalirModal = new Button { Name = "btnSalirModal", Text = "Salir" };
                this.Controls.Add(btnSalirModal);
            }
        }

        private void OrganizarControles()
        {
            this.SuspendLayout();
            
            try
            {
                int startX = 30;
                int startY = 30;
                int labelWidth = 120;
                int controlHeight = 30;
                int spacingY = 15;
                int textBoxX = startX + labelWidth + 15;
                int textBoxWidth = 280;

                // MODIFICADO: Crear lista de controles según el origen
                var controlesOrdenados = new List<(Label label, TextBox textBox)>();

                if (Origen == OrigenLlamada.Ventas)
                {
                    // DESDE VENTAS: Solo mostrar Código, Descripción y Precio
                    var lblCodigo = this.Controls.Find("lblCodigo", true).FirstOrDefault() as Label;
                    var txtCodigo = this.Controls.Find("txtCodigo", true).FirstOrDefault() as TextBox;
                    if (lblCodigo != null && txtCodigo != null)
                        controlesOrdenados.Add((lblCodigo, txtCodigo));

                    var lblDescripcion = this.Controls.Find("lblDescripcion", true).FirstOrDefault() as Label;
                    var txtDescripcion = this.Controls.Find("txtDescripcion", true).FirstOrDefault() as TextBox;
                    if (lblDescripcion != null && txtDescripcion != null)
                        controlesOrdenados.Add((lblDescripcion, txtDescripcion));

                    var lblPrecio = this.Controls.Find("lblPrecio", true).FirstOrDefault() as Label;
                    var txtPrecio = this.Controls.Find("txtPrecio", true).FirstOrDefault() as TextBox;
                    if (lblPrecio != null && txtPrecio != null)
                    {
                        txtPrecio.ReadOnly = false; // Permitir edición desde Ventas
                        controlesOrdenados.Add((lblPrecio, txtPrecio));
                    }

                    // OCULTAR los controles que no se usan desde Ventas
                    OcultarControlesNoUsados();
                }
                else
                {
                    // DESDE PRODUCTOS: Mostrar todos los controles como antes
                    var lblCodigo = this.Controls.Find("lblCodigo", true).FirstOrDefault() as Label;
                    var txtCodigo = this.Controls.Find("txtCodigo", true).FirstOrDefault() as TextBox;
                    if (lblCodigo != null && txtCodigo != null)
                        controlesOrdenados.Add((lblCodigo, txtCodigo));

                    var lblDescripcion = this.Controls.Find("lblDescripcion", true).FirstOrDefault() as Label;
                    var txtDescripcion = this.Controls.Find("txtDescripcion", true).FirstOrDefault() as TextBox;
                    if (lblDescripcion != null && txtDescripcion != null)
                        controlesOrdenados.Add((lblDescripcion, txtDescripcion));

                    var lblRubro = this.Controls.Find("lblRubro", true).FirstOrDefault() as Label;
                    var txtRubro = this.Controls.Find("txtRubro", true).FirstOrDefault() as TextBox;
                    if (lblRubro != null && txtRubro != null)
                        controlesOrdenados.Add((lblRubro, txtRubro));

                    var lblMarca = this.Controls.Find("lblMarca", true).FirstOrDefault() as Label;
                    var txtMarca = this.Controls.Find("txtMarca", true).FirstOrDefault() as TextBox;
                    if (lblMarca != null && txtMarca != null)
                        controlesOrdenados.Add((lblMarca, txtMarca));

                    var lblCosto = this.Controls.Find("lblCosto", true).FirstOrDefault() as Label;
                    var txtCosto = this.Controls.Find("txtCosto", true).FirstOrDefault() as TextBox;
                    if (lblCosto != null && txtCosto != null)
                        controlesOrdenados.Add((lblCosto, txtCosto));

                    var lblPorcentaje = this.Controls.Find("lblPorcentaje", true).FirstOrDefault() as Label;
                    var txtPorcentaje = this.Controls.Find("txtPorcentaje", true).FirstOrDefault() as TextBox;
                    if (lblPorcentaje != null && txtPorcentaje != null)
                        controlesOrdenados.Add((lblPorcentaje, txtPorcentaje));

                    var lblPrecio = this.Controls.Find("lblPrecio", true).FirstOrDefault() as Label;
                    var txtPrecio = this.Controls.Find("txtPrecio", true).FirstOrDefault() as TextBox;
                    if (lblPrecio != null && txtPrecio != null)
                    {
                        txtPrecio.ReadOnly = true; // Solo lectura desde Productos
                        controlesOrdenados.Add((lblPrecio, txtPrecio));
                    }

                    var lblStock = this.Controls.Find("lblStock", true).FirstOrDefault() as Label;
                    var txtCantidad = this.Controls.Find("txtCantidad", true).FirstOrDefault() as TextBox;
                    if (lblStock != null && txtCantidad != null)
                        controlesOrdenados.Add((lblStock, txtCantidad));

                    var lblProveedor = this.Controls.Find("lblProveedor", true).FirstOrDefault() as Label;
                    var txtProveedor = this.Controls.Find("txtProveedor", true).FirstOrDefault() as TextBox;
                    if (lblProveedor != null && txtProveedor != null)
                        controlesOrdenados.Add((lblProveedor, txtProveedor));
                }

                // Posicionar controles en orden
                for (int i = 0; i < controlesOrdenados.Count; i++)
                {
                    var (label, textBox) = controlesOrdenados[i];
                    int yPos = startY + (controlHeight + spacingY) * i;

                    // Configurar label
                    label.Location = new Point(startX, yPos);
                    label.Size = new Size(labelWidth, controlHeight);
                    label.AutoSize = false;
                    label.TextAlign = ContentAlignment.MiddleLeft;
                    label.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    label.ForeColor = Color.FromArgb(62, 80, 100);
                    label.Visible = true; // Asegurar que esté visible

                    // Configurar TextBox
                    textBox.Location = new Point(textBoxX, yPos);
                    textBox.Size = new Size(textBoxWidth, controlHeight);
                    textBox.Font = new Font("Segoe UI", 10F);
                    textBox.BackColor = Color.FromArgb(250, 252, 254);
                    textBox.ForeColor = Color.FromArgb(62, 80, 100);
                    textBox.Visible = true; // Asegurar que esté visible
                }

                // Posicionar botones
                int buttonY = startY + (controlHeight + spacingY) * controlesOrdenados.Count + 30;
                
                var btnGuardar = this.Controls.Find("btnGuardar", true).FirstOrDefault() as Button;
                if (btnGuardar != null)
                {
                    btnGuardar.Location = new Point(textBoxX, buttonY);
                    btnGuardar.Size = new Size(100, 40);
                    btnGuardar.BackColor = Color.FromArgb(76, 175, 80);
                    btnGuardar.ForeColor = Color.White;
                    btnGuardar.FlatStyle = FlatStyle.Flat;
                    btnGuardar.FlatAppearance.BorderSize = 0;
                    btnGuardar.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }

                var btnSalirModal = this.Controls.Find("btnSalirModal", true).FirstOrDefault() as Button;
                if (btnSalirModal != null)
                {
                    btnSalirModal.Location = new Point(textBoxX + 110, buttonY);
                    btnSalirModal.Size = new Size(100, 40);
                    btnSalirModal.BackColor = Color.FromArgb(158, 158, 158);
                    btnSalirModal.ForeColor = Color.White;
                    btnSalirModal.FlatStyle = FlatStyle.Flat;
                    btnSalirModal.FlatAppearance.BorderSize = 0;
                    btnSalirModal.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }

                // Ajustar tamaño del formulario según el origen
                int formHeight = buttonY + 100;
                int formWidth = textBoxX + textBoxWidth + 50;
                
                if (Origen == OrigenLlamada.Ventas)
                {
                    // Formulario más pequeño para Ventas (solo 3 campos)
                    formHeight = buttonY + 80;
                    this.Text = "Agregar Producto Rápido";
                }
                else
                {
                    this.Text = Modo == ModoFormulario.Agregar ? "Agregar Producto" : "Modificar Producto";
                }
                
                this.ClientSize = new Size(formWidth, formHeight);

                // NUEVO: Forzar que los controles se traigan al frente en orden
                foreach (var (label, textBox) in controlesOrdenados)
                {
                    label.BringToFront();
                    textBox.BringToFront();
                }
                btnGuardar?.BringToFront();
                btnSalirModal?.BringToFront();
            }
            finally
            {
                this.ResumeLayout(true);
                this.Refresh(); // Forzar redibujado
            }
        }

        // NUEVO: Método para ocultar controles no utilizados desde Ventas
        private void OcultarControlesNoUsados()
        {
            var controlesAOcultar = new[] { "lblRubro", "txtRubro", "lblMarca", "txtMarca", "lblCosto", "txtCosto", 
                                           "lblPorcentaje", "txtPorcentaje", "lblStock", "txtCantidad", "lblProveedor", "txtProveedor" };
            
            foreach (var nombreControl in controlesAOcultar)
            {
                var control = this.Controls.Find(nombreControl, true).FirstOrDefault();
                if (control != null)
                {
                    control.Visible = false;
                }
            }
        }

        private void AsignarEventosControles()
        {
            var btnGuardar = this.Controls.Find("btnGuardar", true).FirstOrDefault() as Button;
            var btnSalirModal = this.Controls.Find("btnSalirModal", true).FirstOrDefault() as Button;
            
            if (btnGuardar != null) btnGuardar.Click += btnGuardar_Click;
            if (btnSalirModal != null) btnSalirModal.Click += BtnSalirModal_Click;

            // Buscar y asignar eventos a TextBoxes
            var textBoxes = new[] { "txtCodigo", "txtDescripcion", "txtRubro", "txtMarca", "txtCosto", "txtPorcentaje", "txtPrecio", "txtCantidad", "txtProveedor" };
            
            foreach (var name in textBoxes)
            {
                var textBox = this.Controls.Find(name, true).FirstOrDefault() as TextBox;
                if (textBox != null)
                {
                    textBox.KeyDown += TextBox_EnterAsTab;
                    
                    // NUEVO: Seleccionar todo el texto cuando reciba el foco
                    textBox.GotFocus += TextBox_GotFocus;
                    
                    // NUEVO: Seleccionar todo el texto cuando se hace clic con el mouse
                    textBox.MouseClick += TextBox_MouseClick;
                }
            }

            // MODIFICADO: Solo configurar eventos específicos si no es desde Ventas
            if (Origen != OrigenLlamada.Ventas)
            {
                var txtCosto = this.Controls.Find("txtCosto", true).FirstOrDefault() as TextBox;
                var txtPrecio = this.Controls.Find("txtPrecio", true).FirstOrDefault() as TextBox;
                var txtCantidad = this.Controls.Find("txtCantidad", true).FirstOrDefault() as TextBox;
                var txtPorcentaje = this.Controls.Find("txtPorcentaje", true).FirstOrDefault() as TextBox;

                if (txtCosto != null)
                {
                    txtCosto.KeyPress += txtCostoPrecio_KeyPress;
                    txtCosto.TextChanged += CalcularPrecioAuto;
                }

                if (txtPrecio != null)
                {
                    txtPrecio.KeyPress += txtCostoPrecio_KeyPress;
                    txtPrecio.ReadOnly = true;
                }

                if (txtCantidad != null)
                {
                    txtCantidad.KeyPress += TextBoxEntero_KeyPress;
                }

                if (txtPorcentaje != null)
                {
                    txtPorcentaje.KeyPress += txtPorcentaje_KeyPress;
                    txtPorcentaje.TextChanged += CalcularPrecioAuto;
                }
            }
            else
            {
                // Desde Ventas: Configurar eventos solo para txtPrecio
                var txtPrecio = this.Controls.Find("txtPrecio", true).FirstOrDefault() as TextBox;
                if (txtPrecio != null)
                {
                    txtPrecio.KeyPress += txtCostoPrecio_KeyPress;
                    txtPrecio.ReadOnly = false; // Permitir edición
                }
            }
        }

        private void ConfigurarTabIndex()
        {
            if (Origen == OrigenLlamada.Ventas)
            {
                // Desde Ventas: Solo configurar TabIndex para los campos visibles
                var controls = new[] { "txtCodigo", "txtDescripcion", "txtPrecio", "btnGuardar", "btnSalirModal" };
                
                for (int i = 0; i < controls.Length; i++)
                {
                    var control = this.Controls.Find(controls[i], true).FirstOrDefault();
                    if (control != null)
                    {
                        control.TabIndex = i;
                    }
                }
            }
            else
            {
                // Desde Productos: Configurar todos los campos
                var controls = new[] { "txtCodigo", "txtDescripcion", "txtRubro", "txtMarca", "txtCosto", "txtPorcentaje", "txtPrecio", "txtCantidad", "txtProveedor", "btnGuardar", "btnSalirModal" };
                
                for (int i = 0; i < controls.Length; i++)
                {
                    var control = this.Controls.Find(controls[i], true).FirstOrDefault();
                    if (control != null)
                    {
                        control.TabIndex = i;
                    }
                }
            }
        }

        public string CodigoAgregado { get; private set; }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            var txtCodigo = this.Controls.Find("txtCodigo", true).FirstOrDefault() as TextBox;
            var txtDescripcion = this.Controls.Find("txtDescripcion", true).FirstOrDefault() as TextBox;

            if (string.IsNullOrWhiteSpace(txtCodigo?.Text) ||
                string.IsNullOrWhiteSpace(txtDescripcion?.Text))
            {
                MessageBox.Show("Complete los campos obligatorios (Código y Descripción).");
                return;
            }

            string codigo = txtCodigo.Text.Trim();
            string[] prefijos = { "72", "75", "77", "78", "79" };
            int PermiteAcumular = prefijos.Any(p => codigo.StartsWith(p)) ? 1 : 0;

            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            string connectionString = config.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand cmd;

                if (Modo == ModoFormulario.Modificar)
                {
                    var query = @"UPDATE Productos SET
                                    codigo = @codigo,
                                    descripcion = @descripcion,
                                    rubro = @rubro,
                                    marca = @marca,
                                    precio = @precio,
                                    costo = @costo,
                                    porcentaje = @porcentaje,
                                    cantidad = @cantidad,
                                    proveedor = @proveedor,
                                    PermiteAcumular = @PermiteAcumular
                                  WHERE codigo = @codigoOriginal";
                    cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@codigoOriginal", CodigoOriginal);
                }
                else
                {
                    var query = @"INSERT INTO Productos 
                                (codigo, descripcion, rubro, marca, precio, costo, porcentaje, cantidad, proveedor, PermiteAcumular)
                                VALUES (@codigo, @descripcion, @rubro, @marca, @precio, @costo, @porcentaje, @cantidad, @proveedor, @PermiteAcumular)";
                    cmd = new SqlCommand(query, connection);
                }

                // MODIFICADO: Obtener valores según el origen
                if (Origen == OrigenLlamada.Ventas)
                {
                    // DESDE VENTAS: Usar valores predeterminados y cálculos automáticos
                    var txtPrecio = this.Controls.Find("txtPrecio", true).FirstOrDefault() as TextBox;
                    decimal precio = decimal.TryParse(txtPrecio?.Text, out var p) ? p : 0;
                    
                    // Calcular costo usando porcentaje 50 (precio = costo + (costo * 50 / 100))
                    // Despejando: costo = precio / (1 + 50/100) = precio / 1.5
                    decimal costo = precio / 1.5m;
                    
                    cmd.Parameters.AddWithValue("@codigo", codigo);
                    cmd.Parameters.AddWithValue("@descripcion", txtDescripcion.Text.Trim());
                    cmd.Parameters.AddWithValue("@rubro", "Agregado en ventas");
                    cmd.Parameters.AddWithValue("@marca", "Ventas");
                    cmd.Parameters.AddWithValue("@precio", precio);
                    cmd.Parameters.AddWithValue("@costo", Math.Round(costo, 2));
                    cmd.Parameters.AddWithValue("@porcentaje", 50);
                    cmd.Parameters.AddWithValue("@cantidad", 10);
                    cmd.Parameters.AddWithValue("@proveedor", "Proveedor");
                    cmd.Parameters.AddWithValue("@PermiteAcumular", PermiteAcumular);
                }
                else
                {
                    // DESDE PRODUCTOS: Usar valores de los controles como antes
                    var txtRubro = this.Controls.Find("txtRubro", true).FirstOrDefault() as TextBox;
                    var txtMarca = this.Controls.Find("txtMarca", true).FirstOrDefault() as TextBox;
                    var txtCosto = this.Controls.Find("txtCosto", true).FirstOrDefault() as TextBox;
                    var txtPorcentaje = this.Controls.Find("txtPorcentaje", true).FirstOrDefault() as TextBox;
                    var txtPrecio = this.Controls.Find("txtPrecio", true).FirstOrDefault() as TextBox;
                    var txtCantidad = this.Controls.Find("txtCantidad", true).FirstOrDefault() as TextBox;
                    var txtProveedor = this.Controls.Find("txtProveedor", true).FirstOrDefault() as TextBox;

                    cmd.Parameters.AddWithValue("@codigo", codigo);
                    cmd.Parameters.AddWithValue("@descripcion", txtDescripcion.Text.Trim());
                    cmd.Parameters.AddWithValue("@rubro", txtRubro?.Text?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@marca", txtMarca?.Text?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@precio", decimal.TryParse(txtPrecio?.Text, out var precio) ? precio : 0);
                    cmd.Parameters.AddWithValue("@costo", decimal.TryParse(txtCosto?.Text, out var costo) ? costo : 0);
                    cmd.Parameters.AddWithValue("@porcentaje", decimal.TryParse(txtPorcentaje?.Text, out var porcentaje) ? porcentaje : 0);
                    cmd.Parameters.AddWithValue("@cantidad", int.TryParse(txtCantidad?.Text, out var cantidad) ? cantidad : 0);
                    cmd.Parameters.AddWithValue("@proveedor", txtProveedor?.Text?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@PermiteAcumular", PermiteAcumular);
                }

                cmd.ExecuteNonQuery();
                
                // NUEVO: Marcar que hubo cambios
                HuboCambios = true;

                CodigoAgregado = codigo;

                // MODIFICADO: Comportamiento según el origen y modo
                if (Modo == ModoFormulario.Modificar)
                {
                    MessageBox.Show("Producto modificado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else // Modo Agregar
                {
                    string mensajeExito = Origen == OrigenLlamada.Ventas 
                        ? "Producto agregado correctamente para la venta." 
                        : "Producto agregado correctamente.";
                    
                    MessageBox.Show(mensajeExito, "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // NUEVO: Comportamiento según el origen
                    if (Origen == OrigenLlamada.Ventas)
                    {
                        // Desde Ventas: Cerrar el modal y volver al formulario de ventas
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        // Desde Productos: Mantener el comportamiento actual (limpiar para agregar otro)
                        LimpiarControles();
                        txtCodigo?.Focus();
                    }
                }
            }
        }

        private void LimpiarControles()
        {
            var textBoxNames = new[] { "txtCodigo", "txtDescripcion", "txtRubro", "txtMarca", "txtCosto", "txtPorcentaje", "txtPrecio", "txtCantidad", "txtProveedor" };
            
            foreach (var name in textBoxNames)
            {
                var textBox = this.Controls.Find(name, true).FirstOrDefault() as TextBox;
                textBox?.Clear();
            }
        }

        // Permite solo números, una coma o punto decimal y control de teclas
        private void TextBoxDecimal_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox txt = sender as TextBox;
            char ch = e.KeyChar;

            // Permitir control (backspace, etc.)
            if (char.IsControl(ch))
                return;

            // Permitir solo un separador decimal
            if ((ch == ',' || ch == '.') && (txt.Text.Contains(",") || txt.Text.Contains(".")))
            {
                e.Handled = true;
                return;
            }

            // Permitir dígitos y un separador decimal
            if (!char.IsDigit(ch) && ch != ',' && ch != '.')
            {
                e.Handled = true;
            }
        }

        // Permite solo números enteros
        private void TextBoxEntero_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = sender as TextBox;
            // Permitir teclas de control.
            if (char.IsControl(e.KeyChar))
                return;
            
            // Permitir solo dígitos.
            if (!char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
                return;
            }
            
            // Limitar a 4 dígitos.
            if (tb.Text.Length >= 4 && tb.SelectionLength == 0)
            {
                e.Handled = true;
            }
        }

        private void TextBox_EnterAsTab(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Evita el beep y el salto de línea
                this.SelectNextControl((Control)sender, true, true, true, true);
            }
        }

        private void frmAgregarProducto_Load(object sender, EventArgs e)
        {
            EstablecerFocoInicial();
            // Eliminar esta línea porque ya se llama desde el timer
            // OrganizarControles();
        }

        private void CalcularPrecioAuto(object sender, EventArgs e)
        {
            var txtCosto = this.Controls.Find("txtCosto", true).FirstOrDefault() as TextBox;
            var txtPorcentaje = this.Controls.Find("txtPorcentaje", true).FirstOrDefault() as TextBox;
            var txtPrecio = this.Controls.Find("txtPrecio", true).FirstOrDefault() as TextBox;

            if (txtCosto != null && txtPorcentaje != null && txtPrecio != null)
            {
                if (decimal.TryParse(txtCosto.Text, out decimal costo) &&
                    decimal.TryParse(txtPorcentaje.Text, out decimal porcentaje))
                {
                    decimal precioCalculado = costo + ((costo * porcentaje) / 100);
                    txtPrecio.Text = precioCalculado.ToString("F2");
                }
                else
                {
                    txtPrecio.Text = string.Empty;
                }
            }
        }

        private void txtPorcentaje_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Si se presiona punto, lo convierte a coma.
            if (e.KeyChar == '.')
                e.KeyChar = ',';

            TextBox tb = sender as TextBox;
            string text = tb.Text;

            // Permitir teclas de control.
            if (char.IsControl(e.KeyChar))
                return;

            // Si se presiona un dígito:
            if (char.IsDigit(e.KeyChar))
            {
                if (text.Contains(","))
                {
                    int index = text.IndexOf(",");
                    // Si el cursor se encuentra después de la coma, limitar a 2 decimales.
                    if (tb.SelectionStart > index)
                    {
                        string decimalPart = text.Substring(index + 1);
                        if (decimalPart.Length >= 2 && tb.SelectionLength == 0)
                            e.Handled = true;
                    }
                    else
                    {
                        // Si el cursor se encuentra en la parte entera, limitar a 3 dígitos.
                        string integerPart = text.Substring(0, index);
                        if (integerPart.Length >= 3 && tb.SelectionLength == 0)
                            e.Handled = true;
                    }
                }
                else
                {
                    // Sin coma, se permite un máximo de 3 dígitos en total.
                    if (text.Length >= 3 && tb.SelectionLength == 0)
                        e.Handled = true;
                }
                return;
            }
            if (e.KeyChar == ',')
            {
                // Permitir solo una coma.
                if (text.Contains(","))
                    e.Handled = true;
                return;
            }
            // Otros caracteres no permitidos.
            e.Handled = true;
        }

        private void txtCostoPrecio_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Si se presiona punto, lo convierte a coma.
            if (e.KeyChar == '.')
                e.KeyChar = ',';

            TextBox tb = sender as TextBox;
            string text = tb.Text;

            // Permitir teclas de control (p.ej., Backspace)
            if (char.IsControl(e.KeyChar))
                return;

            // Si se presiona un dígito:
            if (char.IsDigit(e.KeyChar))
            {
                if (text.Contains(","))
                {
                    int index = text.IndexOf(",");
                    // Si el cursor está en la parte decimal, permitir máximo 2 decimales.
                    if (tb.SelectionStart > index)
                    {
                        string decimalPart = text.Substring(index + 1);
                        if (decimalPart.Length >= 2 && tb.SelectionLength == 0)
                            e.Handled = true;
                    }
                    else
                    {
                        // Si el cursor está en la parte entera, permitir máximo 6 dígitos.
                        string integerPart = text.Substring(0, index);
                        if (integerPart.Length >= 6 && tb.SelectionLength == 0)
                            e.Handled = true;
                    }
                }
                else
                {
                    // Sin coma, limitar a 6 dígitos enteros.
                    if (text.Length >= 6 && tb.SelectionLength == 0)
                        e.Handled = true;
                }
                return;
            }
            if (e.KeyChar == ',')
            {
                // Permitir solo una coma.
                if (text.Contains(","))
                    e.Handled = true;
                return;
            }
            // Otros caracteres no permitidos.
            e.Handled = true;
        }

        private void BtnSalirModal_Click(object sender, EventArgs e)
        {
            // CORREGIDO: Solo devolver OK si hubo cambios, sino devolver Cancel
            this.DialogResult = HuboCambios ? DialogResult.OK : DialogResult.Cancel;
            this.Close();
        }

        private void ConfigurarFormulario()
        {
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.KeyPreview = true;
            
            this.KeyDown += frmAgregarProducto_KeyDown;
        }

        private void frmAgregarProducto_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                // CORREGIDO: Usar el método del botón Salir para mantener consistencia
                BtnSalirModal_Click(sender, e);
            }
            else if (e.KeyCode == Keys.F2)
            {
                btnGuardar_Click(sender, e);
            }
        }

        private void EstablecerFocoInicial()
        {
            var txtCodigo = this.Controls.Find("txtCodigo", true).FirstOrDefault() as TextBox;
            var txtDescripcion = this.Controls.Find("txtDescripcion", true).FirstOrDefault() as TextBox;

            if (Modo == ModoFormulario.Agregar)
            {
                if (Origen == OrigenLlamada.Ventas && txtCodigo != null && txtCodigo.ReadOnly)
                {
                    // Si viene de Ventas y el código está bloqueado, enfocar descripción
                    txtDescripcion?.Focus();
                    txtDescripcion?.SelectAll();
                }
                else
                {
                    txtCodigo?.Focus();
                    txtCodigo?.SelectAll();
                }
            }
            else
            {
                txtDescripcion?.Focus();
                txtDescripcion?.SelectAll();
            }
        }

        // NUEVO: Seleccionar todo el texto cuando el TextBox recibe el foco
        private void TextBox_GotFocus(object sender, EventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Usar BeginInvoke para asegurar que la selección ocurra después de que el control esté completamente enfocado
                this.BeginInvoke((Action)(() => textBox.SelectAll()));
            }
        }

        // NUEVO: Seleccionar todo el texto cuando se hace clic con el mouse
        private void TextBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Solo seleccionar todo si el TextBox no tenía el foco antes del clic
                if (!textBox.Focused)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
            }
        }

        public bool HuboCambios { get; private set; } = false;
    }
}
