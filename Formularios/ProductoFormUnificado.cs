using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class ProductoFormUnificado : Form
    {
        public enum ModoOperacion { Agregar, Modificar }
        public enum OrigenLlamada { Productos, Ventas }
        
        // Propiedades principales
        public ModoOperacion Modo { get; set; } = ModoOperacion.Agregar;
        public OrigenLlamada Origen { get; set; } = OrigenLlamada.Productos;
        public string CodigoProducto { get; set; } = "";
        public string CodigoAgregado { get; private set; } = "";
        public bool HuboCambios { get; private set; } = false;
        
        // Estado interno
        private bool _cargandoDatos = false;
        private bool _precioModificadoManualmente = false;
        private ProductoInfo? _datosOriginales = null;
        
        // Controles dinámicos
        private Panel panelHeader;
        private Panel panelContenido;
        private Panel panelBotones;
        private Label lblTitulo;
        private Label lblSubtitulo;
        private PictureBox iconoFormulario;
        
        // Campos del producto
        private TextBox txtCodigo;
        private TextBox txtDescripcion;
        private TextBox? txtMarca;
        private TextBox? txtRubro;
        private ComboBox? cmbProveedor;
        private TextBox? txtCosto;
        private TextBox? txtPorcentaje;
        private TextBox txtPrecio;
        private NumericUpDown? numStock;
        private ComboBox? cmbIva;
        private CheckBox? chkPermiteAcumular;
        private CheckBox? chkEditarPrecio;
        
        // Botones
        private Button btnGuardar;
        private Button btnCancelar;
        private Button? btnBuscar; // Solo para modo Modificar
        
        public ProductoFormUnificado()
        {
            InitializeComponent();
            ConfigurarFormulario();
            CrearControles();
            ConfigurarEventos();
        }
        
        public ProductoFormUnificado(ModoOperacion modo, string codigo = "", OrigenLlamada origen = OrigenLlamada.Productos) : this()
        {
            Modo = modo;
            CodigoProducto = codigo?.Trim() ?? "";
            Origen = origen;
            
            AjustarFormularioSegunModo();
            
            if (modo == ModoOperacion.Modificar && !string.IsNullOrEmpty(codigo))
            {
                this.Load += async (s, e) => await CargarProductoAsync(codigo);
            }
        }
        
        private void ConfigurarFormulario()
        {
            this.Text = "Gestión de Productos";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.KeyPreview = true;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Size = new Size(650, 600);
            
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    btnCancelar.PerformClick();
                }
                else if (e.KeyCode == Keys.F5 && Modo == ModoOperacion.Modificar)
                {
                    RecalcularPrecio();
                }
            };
        }
        
        private void CrearControles()
        {
            this.SuspendLayout();
            
            // Panel Header - Reducido
            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(63, 81, 181),
                Padding = new Padding(20, 10, 20, 10)
            };
            
            iconoFormulario = new PictureBox
            {
                Size = new Size(40, 40),
                Location = new Point(20, 15),
                SizeMode = PictureBoxSizeMode.CenterImage,
                BackColor = Color.Transparent
            };
            
            lblTitulo = new Label
            {
                Location = new Point(70, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Text = "Agregar Producto"
            };
            
            lblSubtitulo = new Label
            {
                Location = new Point(70, 40),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 220, 255),
                BackColor = Color.Transparent,
                Text = "Complete la información del producto"
            };
            
            panelHeader.Controls.AddRange(new Control[] { iconoFormulario, lblTitulo, lblSubtitulo });
            
            // Panel Contenido
            panelContenido = new Panel
            {
                Location = new Point(0, 70),
                Size = new Size(650, 460),
                BackColor = Color.Transparent,
                AutoScroll = true,
                Padding = new Padding(25, 15, 25, 15)
            };
            
            CrearCamposFormulario();
            
            // Panel Botones
            panelBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.FromArgb(240, 242, 245),
                Padding = new Padding(30, 15, 30, 15)
            };
            
            CrearBotones();
            
            this.Controls.AddRange(new Control[] { panelHeader, panelContenido, panelBotones });
            this.ResumeLayout(true);
        }
        
        private void CrearCamposFormulario()
        {
            int yPos = 15;
            int margenCampo = 40;
            int anchoLabel = 110;
            int anchoControl = 200;
            int xLabel = 25;
            int xControl = xLabel + anchoLabel + 10;
            
            // Código (con botón buscar para modo Modificar)
            txtCodigo = CrearTextBox(xControl, yPos - 2, anchoControl);
            CrearLabel("Código:", xLabel, yPos, anchoLabel);
            panelContenido.Controls.AddRange(new Control[] { txtCodigo });
            
            if (Modo == ModoOperacion.Modificar)
            {
                btnBuscar = new Button
                {
                    Text = "Buscar",
                    Location = new Point(xControl + anchoControl + 10, yPos - 2),
                    Size = new Size(80, 25),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(76, 175, 80),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
                };
                btnBuscar.FlatAppearance.BorderSize = 0;
                btnBuscar.Click += async (s, e) => await BuscarProducto();
                panelContenido.Controls.Add(btnBuscar);
            }
            yPos += margenCampo;
            
            // Descripción
            txtDescripcion = CrearTextBox(xControl, yPos - 2, anchoControl + 100);
            CrearLabel("Descripción:", xLabel, yPos, anchoLabel);
            panelContenido.Controls.Add(txtDescripcion);
            yPos += margenCampo;
            
            // Mostrar campos según el origen
            if (Origen == OrigenLlamada.Productos)
            {
                // Marca y Rubro en la misma línea - CORREGIDO: Mejor espaciado
                txtMarca = CrearTextBox(xControl, yPos - 2, 140); // Reducido ancho
                CrearLabel("Marca:", xLabel, yPos, anchoLabel);
                panelContenido.Controls.Add(txtMarca);
                
                // CORREGIDO: Mejor posicionamiento para Rubro
                int xRubroLabel = xControl + 150; // Más espacio entre controles
                int xRubroControl = xRubroLabel + 55; // Espacio para la label "Rubro:"
                txtRubro = CrearTextBox(xRubroControl, yPos - 2, 150);
                CrearLabelSecundario("Rubro:", xRubroLabel, yPos, 55);
                panelContenido.Controls.Add(txtRubro);
                yPos += margenCampo;
                
                // Proveedor
                cmbProveedor = CrearComboBox(xControl, yPos - 2, anchoControl, 
                    new[] { "Proveedor Principal", "Proveedor Secundario", "Distribuidora", "Fábrica", "Otro" });
                CrearLabel("Proveedor:", xLabel, yPos, anchoLabel);
                panelContenido.Controls.Add(cmbProveedor);
                yPos += margenCampo;
                
                // Costo y % Ganancia en la misma línea - CORREGIDO: Mejor espaciado
                txtCosto = CrearTextBox(xControl, yPos - 2, 90); // Reducido ancho
                CrearLabel("Costo ($):", xLabel, yPos, anchoLabel);
                panelContenido.Controls.Add(txtCosto);
                
                // CORREGIDO: Mejor posicionamiento para % Ganancia
                int xPorcentajeLabel = xControl + 100; // Más espacio
                int xPorcentajeControl = xPorcentajeLabel + 80; // Espacio para "% Ganancia:"
                txtPorcentaje = CrearTextBox(xPorcentajeControl, yPos - 2, 100);
                CrearLabelSecundario("% Ganancia:", xPorcentajeLabel, yPos, 80);
                panelContenido.Controls.Add(txtPorcentaje);
                yPos += margenCampo;
            }
            
            // Precio (siempre visible)
            txtPrecio = CrearTextBox(xControl, yPos - 2, 120);
            CrearLabel("Precio Venta ($):", xLabel, yPos, anchoLabel);
            panelContenido.Controls.Add(txtPrecio);
            yPos += margenCampo;
            
            if (Origen == OrigenLlamada.Productos)
            {
                // Stock e IVA en la misma línea - CORREGIDO: Mejor espaciado
                numStock = CrearNumericUpDown(xControl, yPos - 2, 70); // Reducido ancho
                CrearLabel("Stock:", xLabel, yPos, anchoLabel);
                panelContenido.Controls.Add(numStock);
                
                // CORREGIDO: Mejor posicionamiento para IVA
                int xIvaLabel = xControl + 80; // Más espacio
                int xIvaControl = xIvaLabel + 50; // Espacio para "IVA %:"
                cmbIva = CrearComboBox(xIvaControl, yPos - 2, 80, new[] { "0.00", "10.50", "21.00", "27.00" });
                CrearLabelSecundario("IVA %:", xIvaLabel, yPos, 45);
                panelContenido.Controls.Add(cmbIva);
                yPos += margenCampo;
                
                // Checkboxes - Reducido espacio
                yPos += 5;
                chkPermiteAcumular = CrearCheckBox("Permite Acumular", xLabel, yPos, 180);
                panelContenido.Controls.Add(chkPermiteAcumular);
                yPos += 30;
                
                chkEditarPrecio = CrearCheckBox("Editar Precio en Ventas", xLabel, yPos, 280);
                panelContenido.Controls.Add(chkEditarPrecio);
                yPos += 30;
                
                //// Nota explicativa
                //var lblNota = new Label
                //{
                //    Text = "Nota: Las opciones 'Permite Acumular' y 'Editar Precio' son excluyentes",
                //    Location = new Point(xLabel, yPos),
                //    Size = new Size(450, 30),
                //    Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                //    ForeColor = Color.FromArgb(120, 120, 120),
                //    AutoSize = false
                //};
                //panelContenido.Controls.Add(lblNota);
                //yPos += 35;
            }
            
            // Ajustar altura dinámicamente
            panelContenido.Height = Math.Max(yPos + 20, 400);
            int alturaTotal = 70 + panelContenido.Height + 70; // Header + Contenido + Botones
            if (this.Height < alturaTotal)
            {
                this.Height = alturaTotal;
            }
        }
        
        // MÉTODOS HELPER SIMPLIFICADOS
        private TextBox CrearTextBox(int x, int y, int ancho)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(ancho, 25),
                Font = new Font("Segoe UI", 9.5F),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(62, 80, 100),
                BorderStyle = BorderStyle.FixedSingle
            };
        }
        
        private ComboBox CrearComboBox(int x, int y, int ancho, string[] opciones)
        {
            var comboBox = new ComboBox
            {
                Location = new Point(x, y),
                Size = new Size(ancho, 25),
                Font = new Font("Segoe UI", 9.5F),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(62, 80, 100),
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            
            comboBox.Items.AddRange(opciones);
            if (opciones.Length > 0)
            {
                comboBox.Text = opciones[0];
            }
            
            return comboBox;
        }
        
        private NumericUpDown CrearNumericUpDown(int x, int y, int ancho)
        {
            return new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(ancho, 25),
                Font = new Font("Segoe UI", 9.5F),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(62, 80, 100),
                Minimum = -9999, // Permitir valores negativos
                Maximum = 9999,
                DecimalPlaces = 0,
                Value = 0
            };
        }
        
        private CheckBox CrearCheckBox(string texto, int x, int y, int ancho)
        {
            return new CheckBox
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(ancho, 22),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(62, 80, 100),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }
        
        private Label CrearLabel(string texto, int x, int y, int ancho)
        {
            var label = new Label
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(ancho, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 80, 100),
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            panelContenido.Controls.Add(label);
            return label;
        }
        
        // Método para crear labels secundarios (más pequeños)
        private Label CrearLabelSecundario(string texto, int x, int y, int ancho)
        {
            var label = new Label
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(ancho, 22),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), // Font más pequeño
                ForeColor = Color.FromArgb(62, 80, 100),
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            panelContenido.Controls.Add(label);
            return label;
        }
        
        private void CrearBotones()
        {
            btnGuardar = new Button
            {
                Text = Modo == ModoOperacion.Agregar ? "Guardar" : "Actualizar",
                Size = new Size(120, 40),
                Location = new Point(panelBotones.Width - 280, 15),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            
            btnCancelar = new Button
            {
                Text = "Cancelar",
                Size = new Size(120, 40),
                Location = new Point(panelBotones.Width - 150, 15),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            
            panelBotones.Controls.AddRange(new Control[] { btnGuardar, btnCancelar });
        }
        
        private void AjustarFormularioSegunModo()
        {
            if (Modo == ModoOperacion.Agregar)
            {
                lblTitulo.Text = Origen == OrigenLlamada.Ventas ? "Agregar Producto Rápido" : "Agregar Nuevo Producto";
                lblSubtitulo.Text = Origen == OrigenLlamada.Ventas ? 
                    "Agregue el producto para continuar con la venta" : 
                    "Complete todos los campos del producto";
                iconoFormulario.Image = CrearIcono("+", Color.White);
            }
            else
            {
                lblTitulo.Text = "Modificar Producto";
                lblSubtitulo.Text = "Edite los campos que desea actualizar - F5 para recalcular precio";
                iconoFormulario.Image = CrearIcono("E", Color.White);
            }
            
            // Ajustar tamaño para modo Ventas (solo campos básicos)
            if (Origen == OrigenLlamada.Ventas)
            {
                this.Size = new Size(650, 320);
            }
        }
        
        private Bitmap CrearIcono(string texto, Color color)
        {
            var bitmap = new Bitmap(40, 40);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                using (var brush = new SolidBrush(color))
                using (var font = new Font("Segoe UI", 20F, FontStyle.Bold))
                {
                    var size = g.MeasureString(texto, font);
                    var point = new PointF((40 - size.Width) / 2, (40 - size.Height) / 2);
                    g.DrawString(texto, font, brush, point);
                }
            }
            return bitmap;
        }
        
        private void ConfigurarEventos()
        {
            // Eventos de teclado para navegación
            foreach (Control control in panelContenido.Controls)
            {
                if (control is TextBox textBox)
                {
                    textBox.KeyDown += (s, e) =>
                    {
                        if (e.KeyCode == Keys.Enter)
                        {
                            e.SuppressKeyPress = true;
                            this.SelectNextControl((Control)s, true, true, true, true);
                        }
                    };
                    
                    textBox.GotFocus += (s, e) => ((TextBox)s).SelectAll();
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.KeyDown += (s, e) =>
                    {
                        if (e.KeyCode == Keys.Enter)
                        {
                            e.SuppressKeyPress = true;
                            this.SelectNextControl((Control)s, true, true, true, true);
                        }
                    };
                }
            }
            
            // Eventos específicos
            btnGuardar.Click += async (s, e) => await GuardarProducto();
            btnCancelar.Click += (s, e) => CerrarFormulario();
            
            if (Origen == OrigenLlamada.Productos)
            {
                // Cálculo automático de precio
                if (txtCosto != null) txtCosto.TextChanged += CalcularPrecioAutomatico;
                if (txtPorcentaje != null) txtPorcentaje.TextChanged += CalcularPrecioAutomatico;
                
                // Exclusión mutua de checkboxes
                if (chkPermiteAcumular != null)
                {
                    chkPermiteAcumular.CheckedChanged += (s, e) =>
                    {
                        if (chkPermiteAcumular.Checked && chkEditarPrecio != null)
                        {
                            chkEditarPrecio.Checked = false;
                        }
                    };
                }
                
                if (chkEditarPrecio != null)
                {
                    chkEditarPrecio.CheckedChanged += (s, e) =>
                    {
                        if (chkEditarPrecio.Checked && chkPermiteAcumular != null)
                        {
                            chkPermiteAcumular.Checked = false;
                        }
                    };
                }
            }
            
            // Validaciones de entrada
            ConfigurarValidaciones();
        }
        
        private void ConfigurarValidaciones()
        {
            // Validación para campos numéricos
            if (txtCosto != null) txtCosto.KeyPress += ValidarDecimal;
            if (txtPorcentaje != null) txtPorcentaje.KeyPress += ValidarDecimal;
            if (txtPrecio != null) txtPrecio.KeyPress += ValidarDecimal;
            if (cmbIva != null) cmbIva.KeyPress += ValidarDecimal;
        }
        
        private void ValidarDecimal(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '.') e.KeyChar = ',';
            
            if (char.IsControl(e.KeyChar)) return;
            
            // Obtener el texto actual según el tipo de control
            string textoActual = "";
            if (sender is TextBox textBox)
            {
                textoActual = textBox.Text;
            }
            else if (sender is ComboBox comboBox)
            {
                textoActual = comboBox.Text;
            }
            
            if (char.IsDigit(e.KeyChar) || e.KeyChar == ',')
            {
                if (e.KeyChar == ',' && textoActual.Contains(","))
                {
                    e.Handled = true;
                }
            }
            else
            {
                e.Handled = true;
            }
        }
        
        private void CalcularPrecioAutomatico(object sender, EventArgs e)
        {
            if (_cargandoDatos || _precioModificadoManualmente) return;
            
            if (decimal.TryParse(txtCosto?.Text?.Replace(".", ","), out decimal costo) &&
                decimal.TryParse(txtPorcentaje?.Text?.Replace(".", ","), out decimal porcentaje))
            {
                decimal precio = costo + (costo * porcentaje / 100);
                if (txtPrecio != null)
                {
                    txtPrecio.Text = precio.ToString("F2");
                }
            }
        }
        
        private void RecalcularPrecio()
        {
            _precioModificadoManualmente = false;
            CalcularPrecioAutomatico(null, null);
        }
        
        private async Task BuscarProducto()
        {
            string codigo = txtCodigo?.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(codigo))
            {
                MessageBox.Show("Ingrese un código de producto para buscar.", "Búsqueda", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCodigo?.Focus();
                return;
            }
            
            await CargarProductoAsync(codigo);
        }
        
        private async Task CargarProductoAsync(string codigo)
        {
            if (string.IsNullOrEmpty(codigo)) return;
            
            _cargandoDatos = true;
            try
            {
                this.Cursor = Cursors.WaitCursor;
                btnGuardar.Enabled = false;
                
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                
                string connectionString = config.GetConnectionString("DefaultConnection") ?? "";
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    string query = @"SELECT codigo, descripcion, marca, rubro, proveedor, costo, porcentaje, precio, 
                                          cantidad, iva, 
                                          CAST(ISNULL(PermiteAcumular, 0) AS BIT) as PermiteAcumular, 
                                          CAST(ISNULL(EditarPrecio, 0) AS BIT) as EditarPrecio 
                                   FROM Productos WHERE codigo = @codigo";
                    
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@codigo", codigo);
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                // Guardar datos originales
                                _datosOriginales = new ProductoInfo
                                {
                                    Codigo = reader["codigo"].ToString() ?? "",
                                    Descripcion = reader["descripcion"].ToString() ?? "",
                                    Marca = reader["marca"].ToString() ?? "",
                                    Rubro = reader["rubro"].ToString() ?? "",
                                    Proveedor = reader["proveedor"].ToString() ?? "",
                                    Costo = Convert.ToDecimal(reader["costo"]),
                                    Porcentaje = Convert.ToDecimal(reader["porcentaje"]),
                                    Precio = Convert.ToDecimal(reader["precio"]),
                                    Stock = Convert.ToInt32(reader["cantidad"]),
                                    Iva = Convert.ToDecimal(reader["iva"]),
                                    PermiteAcumular = Convert.ToBoolean(reader["PermiteAcumular"]),
                                    EditarPrecio = Convert.ToBoolean(reader["EditarPrecio"])
                                };
                                
                                // Cargar datos en el formulario
                                CargarDatosEnFormulario(_datosOriginales);
                                
                                // Enfocar en el primer campo editable
                                txtDescripcion?.Focus();
                                txtDescripcion?.SelectAll();
                            }
                            else
                            {
                                MessageBox.Show($"No se encontró un producto con el código '{codigo}'.", 
                                    "Producto no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                LimpiarFormulario();
                                txtCodigo?.Focus();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el producto: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _cargandoDatos = false;
                this.Cursor = Cursors.Default;
                btnGuardar.Enabled = true;
            }
        }
        
        private void CargarDatosEnFormulario(ProductoInfo datos)
        {
            txtCodigo.Text = datos.Codigo;
            txtDescripcion.Text = datos.Descripcion;
            
            if (Origen == OrigenLlamada.Productos)
            {
                if (txtMarca != null) txtMarca.Text = datos.Marca;
                if (txtRubro != null) txtRubro.Text = datos.Rubro;
                if (cmbProveedor != null) cmbProveedor.Text = datos.Proveedor;
                if (txtCosto != null) txtCosto.Text = datos.Costo.ToString("F2");
                if (txtPorcentaje != null) txtPorcentaje.Text = datos.Porcentaje.ToString("F2");
                if (numStock != null) numStock.Value = datos.Stock;
                if (cmbIva != null) cmbIva.Text = datos.Iva.ToString("F2");
                
                // Cargar valores de checkbox correctamente
                if (chkPermiteAcumular != null) 
                {
                    chkPermiteAcumular.Checked = datos.PermiteAcumular;
                    System.Diagnostics.Debug.WriteLine($"✅ Cargando PermiteAcumular: {datos.PermiteAcumular}");
                }
                if (chkEditarPrecio != null) 
                {
                    chkEditarPrecio.Checked = datos.EditarPrecio;
                    System.Diagnostics.Debug.WriteLine($"✅ Cargando EditarPrecio: {datos.EditarPrecio}");
                }
            }
            
            txtPrecio.Text = datos.Precio.ToString("F2");
        }
        
        private async Task GuardarProducto()
        {
            if (!ValidarDatos()) return;
            
            try
            {
                this.Cursor = Cursors.WaitCursor;
                btnGuardar.Enabled = false;
                
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                
                string connectionString = config.GetConnectionString("DefaultConnection") ?? "";
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            SqlCommand cmd;
                            
                            if (Modo == ModoOperacion.Agregar)
                            {
                                cmd = new SqlCommand(@"
                                    INSERT INTO Productos (codigo, descripcion, marca, rubro, proveedor, costo, porcentaje, 
                                                         precio, cantidad, iva, PermiteAcumular, EditarPrecio)
                                    VALUES (@codigo, @descripcion, @marca, @rubro, @proveedor, @costo, @porcentaje, 
                                           @precio, @cantidad, @iva, @permiteAcumular, @editarPrecio)", connection, transaction);
                            }
                            else
                            {
                                cmd = new SqlCommand(@"
                                    UPDATE Productos SET 
                                        descripcion = @descripcion, marca = @marca, rubro = @rubro, proveedor = @proveedor,
                                        costo = @costo, porcentaje = @porcentaje, precio = @precio, cantidad = @cantidad,
                                        iva = @iva, PermiteAcumular = @permiteAcumular, EditarPrecio = @editarPrecio
                                    WHERE codigo = @codigo", connection, transaction);
                            }
                            
                            AgregarParametros(cmd);
                            
                            int filasAfectadas = await cmd.ExecuteNonQueryAsync();
                            
                            if (filasAfectadas > 0)
                            {
                                transaction.Commit();
                                HuboCambios = true;
                                CodigoAgregado = txtCodigo.Text.Trim();
                                
                                string mensaje = Modo == ModoOperacion.Agregar ? 
                                    "Producto agregado exitosamente." : 
                                    "Producto actualizado exitosamente.";
                                
                                // CORREGIDO: No mostrar mensaje y cerrar inmediatamente en modo Modificar
                                if (Modo == ModoOperacion.Modificar)
                                {
                                    // Cerrar sin mostrar mensaje para una experiencia más fluida
                                    this.DialogResult = DialogResult.OK;
                                    this.Close();
                                    return; // Salir inmediatamente
                                }
                                
                                // Solo mostrar mensaje para modo Agregar
                                MessageBox.Show(mensaje, "Operación exitosa", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                                
                                if (Modo == ModoOperacion.Agregar && Origen == OrigenLlamada.Productos)
                                {
                                    // Limpiar para agregar otro producto
                                    LimpiarFormulario();
                                    txtCodigo?.Focus();
                                }
                                else
                                {
                                    this.DialogResult = DialogResult.OK;
                                    this.Close();
                                }
                            }
                            else
                            {
                                transaction.Rollback();
                                MessageBox.Show("No se pudieron guardar los cambios.", "Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627) // Error de clave duplicada
                {
                    MessageBox.Show("Ya existe un producto con ese código.", "Código duplicado", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCodigo?.Focus();
                }
                else
                {
                    MessageBox.Show($"Error en la base de datos: {ex.Message}", "Error SQL", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnGuardar.Enabled = true;
            }
        }
        
        private void AgregarParametros(SqlCommand cmd)
        {
            cmd.Parameters.AddWithValue("@codigo", txtCodigo.Text.Trim());
            cmd.Parameters.AddWithValue("@descripcion", txtDescripcion.Text.Trim());
            
            if (Origen == OrigenLlamada.Productos)
            {
                cmd.Parameters.AddWithValue("@marca", txtMarca?.Text?.Trim() ?? "");
                cmd.Parameters.AddWithValue("@rubro", txtRubro?.Text?.Trim() ?? "");
                cmd.Parameters.AddWithValue("@proveedor", cmbProveedor?.Text?.Trim() ?? "");
                cmd.Parameters.AddWithValue("@costo", ParseDecimal(txtCosto?.Text));
                cmd.Parameters.AddWithValue("@porcentaje", ParseDecimal(txtPorcentaje?.Text));
                cmd.Parameters.AddWithValue("@cantidad", (int)(numStock?.Value ?? 0));
                cmd.Parameters.AddWithValue("@iva", ParseDecimal(cmbIva?.Text, 21.00m));
                cmd.Parameters.AddWithValue("@permiteAcumular", chkPermiteAcumular?.Checked ?? false);
                cmd.Parameters.AddWithValue("@editarPrecio", chkEditarPrecio?.Checked ?? false);
            }
            else
            {
                // Valores por defecto para agregar desde Ventas
                cmd.Parameters.AddWithValue("@marca", "Ventas");
                cmd.Parameters.AddWithValue("@rubro", "Agregado en ventas");
                cmd.Parameters.AddWithValue("@proveedor", "Proveedor");
                
                decimal precio = ParseDecimal(txtPrecio?.Text);
                decimal costo = Math.Round(precio / 1.5m, 2);
                
                cmd.Parameters.AddWithValue("@costo", costo);
                cmd.Parameters.AddWithValue("@porcentaje", 50.00m);
                cmd.Parameters.AddWithValue("@cantidad", 10);
                cmd.Parameters.AddWithValue("@iva", 21.00m);
                cmd.Parameters.AddWithValue("@permiteAcumular", false);
                cmd.Parameters.AddWithValue("@editarPrecio", false);
            }
            
            cmd.Parameters.AddWithValue("@precio", ParseDecimal(txtPrecio?.Text));
        }
        
        private decimal ParseDecimal(string? text, decimal defaultValue = 0)
        {
            if (string.IsNullOrWhiteSpace(text)) return defaultValue;
            
            text = text.Replace(".", ",");
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal result) 
                ? result : defaultValue;
        }
        
        private bool ValidarDatos()
        {
            var errores = new List<string>();
            
            if (string.IsNullOrWhiteSpace(txtCodigo?.Text))
                errores.Add("• El código es obligatorio");
            
            if (string.IsNullOrWhiteSpace(txtDescripcion?.Text))
                errores.Add("• La descripción es obligatoria");
            
            if (string.IsNullOrWhiteSpace(txtPrecio?.Text) || ParseDecimal(txtPrecio?.Text) <= 0)
                errores.Add("• El precio debe ser mayor a cero");
            
            if (Origen == OrigenLlamada.Productos)
            {
                if (ParseDecimal(txtCosto?.Text) < 0)
                    errores.Add("• El costo no puede ser negativo");
                
                decimal iva = ParseDecimal(cmbIva?.Text, 21.00m);
                if (iva < 0 || iva > 99.99m)
                    errores.Add("• El IVA debe estar entre 0 y 99.99%");
            }
            
            if (errores.Any())
            {
                string mensaje = "Por favor corrija los siguientes errores:\n\n" + string.Join("\n", errores);
                MessageBox.Show(mensaje, "Datos incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            
            return true;
        }
        
        private void LimpiarFormulario()
        {
            foreach (Control control in panelContenido.Controls)
            {
                if (control is TextBox textBox)
                    textBox.Clear();
                else if (control is ComboBox comboBox)
                    comboBox.Text = comboBox.Items.Count > 0 ? comboBox.Items[0].ToString() ?? "" : "";
                else if (control is NumericUpDown numeric)
                    numeric.Value = 0;
                else if (control is CheckBox checkBox)
                    checkBox.Checked = false;
            }
            
            _datosOriginales = null;
            _precioModificadoManualmente = false;
        }
        
        private void CerrarFormulario()
        {
            if (HuboCambios)
            {
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                this.DialogResult = DialogResult.Cancel;
            }
            this.Close();
        }
        
        // Clase auxiliar para almacenar datos del producto
        private class ProductoInfo
        {
            public string Codigo { get; set; } = "";
            public string Descripcion { get; set; } = "";
            public string Marca { get; set; } = "";
            public string Rubro { get; set; } = "";
            public string Proveedor { get; set; } = "";
            public decimal Costo { get; set; }
            public decimal Porcentaje { get; set; }
            public decimal Precio { get; set; }
            public int Stock { get; set; }
            public decimal Iva { get; set; }
            public bool PermiteAcumular { get; set; }
            public bool EditarPrecio { get; set; }
        }
    }
}