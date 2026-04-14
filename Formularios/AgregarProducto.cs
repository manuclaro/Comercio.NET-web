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
    public partial class frmAgregarProducto : Form
    {
        public enum ModoFormulario { Agregar, Modificar }
        public enum OrigenLlamada { Productos, Ventas } // NUEVO: Para identificar el origen
        
        public ModoFormulario Modo { get; set; } = ModoFormulario.Agregar;
        public OrigenLlamada Origen { get; set; } = OrigenLlamada.Productos; // NUEVO: Por defecto desde Productos
        public string CodigoOriginal { get; set; } // Para identificar el registro a modificar

        // NO DECLARAR CONTROLES AQUÍ - ya están en el diseñador

        private IEnumerable<Control> GetAllControls(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                yield return c;
                foreach (var child in GetAllControls(c))
                    yield return child;
            }
        }

        public frmAgregarProducto()
        {
            InitializeComponent();



            // dentro del constructor, justo después de InitializeComponent();
            AplicarEstiloModal();

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

        private void AplicarEstiloModal()
        {
            // Estilo general del formulario
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.Padding = new Padding(12);
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.Font = new Font("Segoe UI", 9.5F);

            // Cabecera superior tipo "banner"
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(63, 81, 181),
                Padding = new Padding(12)
            };

            var lblIcon = new Label
            {
                Text = "＋",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(8, 10)
            };
            header.Controls.Add(lblIcon);

            var lblTitle = new Label
            {
                Text = "Agregar Producto",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(72, 8) // <-- desplazado a la derecha
            };
            header.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = "Complete los campos mínimos para continuar",
                ForeColor = Color.FromArgb(220, 230, 255),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(72, 30) // <-- desplazado a la derecha
            };
            header.Controls.Add(lblSub);

            // Insertar al tope (si ya hay uno con DockTop, evitar duplicar)
            if (!this.Controls.OfType<Panel>().Any(p => p.Height == 64 && p.BackColor == header.BackColor))
                this.Controls.Add(header);

            header.BringToFront();

            // Estilizar TextBox / ComboBox / DateTimePicker
            foreach (Control c in GetAllControls(this))
            {
                // placeholder (no-op) si necesitás iterar todos los controles
            }

            foreach (var tb in this.Controls.OfType<TextBox>())
            {
                tb.Font = new Font("Segoe UI", 10F);
                tb.BorderStyle = BorderStyle.FixedSingle;
                tb.BackColor = Color.White;
                tb.ForeColor = Color.Black;
            }

            foreach (var cb in this.Controls.OfType<ComboBox>())
            {
                cb.Font = new Font("Segoe UI", 10F);
                cb.FlatStyle = FlatStyle.Flat;
                cb.BackColor = Color.White;
            }

            foreach (var dt in this.Controls.OfType<DateTimePicker>())
            {
                dt.Font = new Font("Segoe UI", 10F);
                dt.CalendarFont = new Font("Segoe UI", 9F);
            }

            // Botones: buscar por nombres comunes y aplicar estilo moderno
            string[] posiblesGuardar = { "btnGuardar", "BtnGuardar", "btnAceptar", "BtnAceptar" };
            string[] posiblesCancelar = { "btnCancelar", "BtnCancelar", "BtnSalirModal", "btnCerrar" };

            Button btnGuardar = null;
            Button btnCancelar = null;

            foreach (var name in posiblesGuardar)
            {
                var ctrl = this.Controls.Find(name, true).FirstOrDefault() as Button;
                if (ctrl != null) { btnGuardar = ctrl; break; }
            }
            foreach (var name in posiblesCancelar)
            {
                var ctrl = this.Controls.Find(name, true).FirstOrDefault() as Button;
                if (ctrl != null) { btnCancelar = ctrl; break; }
            }

            if (btnGuardar != null)
            {
                btnGuardar.FlatStyle = FlatStyle.Flat;
                btnGuardar.BackColor = Color.FromArgb(40, 167, 69); // verde
                btnGuardar.ForeColor = Color.White;
                btnGuardar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                btnGuardar.FlatAppearance.BorderSize = 0;
                this.AcceptButton = btnGuardar;
            }

            if (btnCancelar != null)
            {
                btnCancelar.FlatStyle = FlatStyle.Flat;
                btnCancelar.BackColor = Color.FromArgb(158, 158, 158); // gris
                btnCancelar.ForeColor = Color.White;
                btnCancelar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                btnCancelar.FlatAppearance.BorderSize = 0;
                this.CancelButton = btnCancelar;
            }

            // Reubicar botones de acción a la derecha inferior si existen (no fuerza layout complejo)
            var actionButtons = new List<Button>();
            if (btnGuardar != null) actionButtons.Add(btnGuardar);
            if (btnCancelar != null && btnCancelar != btnGuardar) actionButtons.Add(btnCancelar);

            if (actionButtons.Any())
            {
                // crear un panel para agrupar botones si no existe
                var panelAccion = new Panel
                {
                    Height = 48,
                    Dock = DockStyle.Bottom,
                    BackColor = Color.Transparent,
                    Padding = new Padding(12)
                };
                // mover los botones al panel (si ya están en otro padre, los reubica)
                foreach (var b in actionButtons)
                {
                    try
                    {
                        b.Parent?.Controls.Remove(b);
                        b.Width = 120;
                        b.Height = 36;
                        b.Margin = new Padding(8, 6, 8, 6);
                        b.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                        panelAccion.Controls.Add(b);
                    }
                    catch { }
                }

                // agregar panel si no está agregado aún
                if (!this.Controls.OfType<Panel>().Any(p => p.Dock == DockStyle.Bottom && p != panelAccion))
                    this.Controls.Add(panelAccion);

                panelAccion.BringToFront();
                // alinear botones a la derecha
                int x = panelAccion.ClientSize.Width - 12;
                for (int i = panelAccion.Controls.Count - 1; i >= 0; i--)
                {
                    Control b = panelAccion.Controls[i];
                    b.Left = x - b.Width;
                    b.Top = (panelAccion.Height - b.Height) / 2;
                    x = b.Left - 12;
                }

                // reajustar cuando cambie tamaño
                panelAccion.SizeChanged += (s, e) =>
                {
                    int xx = panelAccion.ClientSize.Width - 12;
                    for (int i = panelAccion.Controls.Count - 1; i >= 0; i--)
                    {
                        Control b = panelAccion.Controls[i];
                        b.Left = xx - b.Width;
                        b.Top = (panelAccion.Height - b.Height) / 2;
                        xx = b.Left - 12;
                    }
                };
            }

            // Mejorar labels: fuente y color
            foreach (var lbl in GetAllControls(this).OfType<Label>())
            {
                // skip header labels ya creados
                if (lbl == lblTitle || lbl == lblSub || lbl == lblIcon) continue;
                lbl.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                lbl.ForeColor = Color.FromArgb(33, 33, 33);
            }
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
                new { Name = "lblProveedor", Text = "Proveedor:" },
                new { Name = "lblIva", Text = "IVA %:" }
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

            // Crear todos los textboxes que faltan (REMOVIDO txtIva)
            var textBoxesNeeded = new[]
            {
                new { Name = "txtCodigo", ReadOnly = false },
                new { Name = "txtDescripcion", ReadOnly = false },
                new { Name = "txtRubro", ReadOnly = false },
                new { Name = "txtMarca", ReadOnly = false },
                new { Name = "txtCosto", ReadOnly = false },
                new { Name = "txtPorcentaje", ReadOnly = false },
                new { Name = "txtPrecio", ReadOnly = false },
                new { Name = "txtCantidad", ReadOnly = false },
                new { Name = "txtProveedor", ReadOnly = false }
                // REMOVIDO: txtIva ya que usamos solo el ComboBox
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

            // ComboBox para IVA con valores predefinidos
            if (this.Controls.Find("cmbIva", true).Length == 0)
            {
                var cmbIva = new ComboBox
                {
                    Name = "cmbIva",
                    DropDownStyle = ComboBoxStyle.DropDown,
                    AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                    AutoCompleteSource = AutoCompleteSource.ListItems
                };

                // Agregar valores predefinidos de IVA
                cmbIva.Items.AddRange(new object[] {
                    "6.63",   // Cigarrillos
                    "10.50",  // Verdulería, Carnicería, Harinas
                    "21.00"   // Resto de productos, Pollo
                });
                cmbIva.Text = "21.00"; // Valor por defecto

                this.Controls.Add(cmbIva);
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
                // Calcular el Y inicial teniendo en cuenta un posible panel superior (header) y el padding del formulario
                int headerHeight = 0;
                var topPanel = this.Controls.OfType<Panel>().FirstOrDefault(p => p.Dock == DockStyle.Top);
                if (topPanel != null) headerHeight = topPanel.Height;
                // Dejar al menos 18px de margen si no hay header; si lo hay, ubicar controles debajo del mismo (+8px de separación)
                int startY = Math.Max(18, this.Padding.Top + headerHeight + 8);

                int labelWidth = 120;
                int controlHeight = 30;
                int spacingY = 15;
                int textBoxX = startX + labelWidth + 15;
                int textBoxWidth = 280;

                var controlesOrdenados = new List<(Label label, Control control)>();

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
                    var controlesCompletos = new[]
                    {
                        ("lblCodigo", "txtCodigo"),
                        ("lblDescripcion", "txtDescripcion"),
                        ("lblRubro", "txtRubro"),
                        ("lblMarca", "txtMarca"),
                        ("lblCosto", "txtCosto"),
                        ("lblPorcentaje", "txtPorcentaje"),
                        ("lblPrecio", "txtPrecio"),
                        ("lblStock", "txtCantidad"),
                        ("lblProveedor", "txtProveedor"),
                        // NUEVO: Agregar IVA a la lista
                        ("lblIva", "cmbIva")
                    };

                    foreach (var (lblName, ctrlName) in controlesCompletos)
                    {
                        var label = this.Controls.Find(lblName, true).FirstOrDefault() as Label;
                        var control = this.Controls.Find(ctrlName, true).FirstOrDefault();
                        
                        if (label != null && control != null)
                        {
                            if (ctrlName == "txtPrecio")
                            {
                                (control as TextBox).ReadOnly = true;
                            }
                            controlesOrdenados.Add((label, control));
                        }
                    }
                }

                // Posicionar controles en orden
                for (int i = 0; i < controlesOrdenados.Count; i++)
                {
                    var (label, control) = controlesOrdenados[i];
                    int yPos = startY + (controlHeight + spacingY) * i;

                    // Configurar label
                    label.Location = new Point(startX, yPos);
                    label.Size = new Size(labelWidth, controlHeight);
                    label.AutoSize = false;
                    label.TextAlign = ContentAlignment.MiddleLeft;
                    label.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    label.ForeColor = Color.FromArgb(62, 80, 100);
                    label.Visible = true;

                    // Configurar Control (TextBox o ComboBox)
                    control.Location = new Point(textBoxX, yPos);
                    control.Size = new Size(textBoxWidth, controlHeight);
                    control.Font = new Font("Segoe UI", 10F);
                    control.Visible = true;
                    
                    if (control is TextBox textBox)
                    {
                        textBox.BackColor = Color.FromArgb(250, 252, 254);
                        textBox.ForeColor = Color.FromArgb(62, 80, 100);
                    }
                    else if (control is ComboBox comboBox)
                    {
                        comboBox.BackColor = Color.FromArgb(250, 252, 254);
                        comboBox.ForeColor = Color.FromArgb(62, 80, 100);
                    }
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

                // Ajustar la altura del formulario añadiendo el padding inferior para evitar recortes
                int formHeight = buttonY + 100 + this.Padding.Bottom;
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
                foreach (var (label, control) in controlesOrdenados)
                {
                    label.BringToFront();
                    control.BringToFront();
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
            var controlesAOcultar = new[] { 
                "lblRubro", "txtRubro", "lblMarca", "txtMarca", "lblCosto", "txtCosto", 
                "lblPorcentaje", "txtPorcentaje", "lblStock", "txtCantidad", "lblProveedor", "txtProveedor",
                // CORREGIDO: Solo ocultar los controles de IVA que existen
                "lblIva", "cmbIva"
            };
            
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

            // CORREGIDO: Remover txtIva de la lista
            var textBoxes = new[] { "txtCodigo", "txtDescripcion", "txtRubro", "txtMarca", "txtCosto", "txtPorcentaje", "txtPrecio", "txtCantidad", "txtProveedor" };
            
            foreach (var name in textBoxes)
            {
                var textBox = this.Controls.Find(name, true).FirstOrDefault() as TextBox;
                if (textBox != null)
                {
                    textBox.KeyDown += TextBox_EnterAsTab;
                    textBox.GotFocus += TextBox_GotFocus;
                    textBox.MouseClick += TextBox_MouseClick;
                }
            }

            // Configurar eventos para el ComboBox de IVA
            var cmbIva = this.Controls.Find("cmbIva", true).FirstOrDefault() as ComboBox;
            if (cmbIva != null)
            {
                cmbIva.KeyDown += ComboBox_EnterAsTab;
                cmbIva.KeyPress += CmbIva_KeyPress;
            }

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
                var txtPrecio = this.Controls.Find("txtPrecio", true).FirstOrDefault() as TextBox;
                if (txtPrecio != null)
                {
                    txtPrecio.KeyPress += txtCostoPrecio_KeyPress;
                    txtPrecio.ReadOnly = false;
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
                // ACTUALIZADO: Agregar cmbIva al TabIndex
                var controls = new[] { "txtCodigo", "txtDescripcion", "txtRubro", "txtMarca", "txtCosto", "txtPorcentaje", "txtPrecio", "txtCantidad", "txtProveedor", "cmbIva", "btnGuardar", "btnSalirModal" };
                
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
                                    PermiteAcumular = @PermiteAcumular,
                                    iva = @iva
                                  WHERE codigo = @codigoOriginal";
                    cmd = new SqlCommand(query, connection);
                    cmd.Parameters.Add("@codigoOriginal", SqlDbType.VarChar, 50).Value = CodigoOriginal ?? "";
                }
                else
                {
                    var query = @"INSERT INTO Productos 
                                (codigo, descripcion, rubro, marca, precio, costo, porcentaje, cantidad, proveedor, PermiteAcumular, iva)
                                VALUES (@codigo, @descripcion, @rubro, @marca, @precio, @costo, @porcentaje, @cantidad, @proveedor, @PermiteAcumular, @iva)";
                    cmd = new SqlCommand(query, connection);
                }

                // CORREGIDO: Especificar precisión y escala para parámetros decimales
                if (Origen == OrigenLlamada.Ventas)
                {
                    var txtPrecio = this.Controls.Find("txtPrecio", true).FirstOrDefault() as TextBox;
                    
                    // CORREGIDO: Usar cultura actual para parsing correcto
                    decimal precio = decimal.Parse(txtPrecio.Text.Replace(".", ","), CultureInfo.CurrentCulture);
                    decimal costo = Math.Round(precio / 1.5m, 2);
                    
                    cmd.Parameters.Add("@codigo", SqlDbType.VarChar, 50).Value = codigo;
                    cmd.Parameters.Add("@descripcion", SqlDbType.VarChar, 255).Value = txtDescripcion.Text.Trim();
                    cmd.Parameters.Add("@rubro", SqlDbType.VarChar, 100).Value = "Agregado en ventas";
                    cmd.Parameters.Add("@marca", SqlDbType.VarChar, 100).Value = "Ventas";
    
                    // CORREGIDO: Especificar precisión y escala para decimales
                    var precioParam = cmd.Parameters.Add("@precio", SqlDbType.Decimal);
                    precioParam.Precision = 18;
                    precioParam.Scale = 2;
                    precioParam.Value = precio;
    
                    var costoParam = cmd.Parameters.Add("@costo", SqlDbType.Decimal);
                    costoParam.Precision = 18;
                    costoParam.Scale = 2;
                    costoParam.Value = costo;
    
                    var porcentajeParam = cmd.Parameters.Add("@porcentaje", SqlDbType.Decimal);
                    porcentajeParam.Precision = 5;
                    porcentajeParam.Scale = 2;
                    porcentajeParam.Value = 50.00m;
    
                    cmd.Parameters.Add("@cantidad", SqlDbType.Int).Value = 10;
                    cmd.Parameters.Add("@proveedor", SqlDbType.VarChar, 100).Value = "Proveedor";
                    cmd.Parameters.Add("@PermiteAcumular", SqlDbType.Int).Value = PermiteAcumular;
    
                    // CORREGIDO: IVA con precisión y escala correctas
                    var ivaParam = cmd.Parameters.Add("@iva", SqlDbType.Decimal);
                    ivaParam.Precision = 5;
                    ivaParam.Scale = 2;
                    ivaParam.Value = 21.00m;
                }
                else
                {
                    var txtRubro = this.Controls.Find("txtRubro", true).FirstOrDefault() as TextBox;
                    var txtMarca = this.Controls.Find("txtMarca", true).FirstOrDefault() as TextBox;
                    var txtCosto = this.Controls.Find("txtCosto", true).FirstOrDefault() as TextBox;
                    var txtPorcentaje = this.Controls.Find("txtPorcentaje", true).FirstOrDefault() as TextBox;
                    var txtPrecio = this.Controls.Find("txtPrecio", true).FirstOrDefault() as TextBox;
                    var txtCantidad = this.Controls.Find("txtCantidad", true).FirstOrDefault() as TextBox;
                    var txtProveedor = this.Controls.Find("txtProveedor", true).FirstOrDefault() as TextBox;
                    var cmbIva = this.Controls.Find("cmbIva", true).FirstOrDefault() as ComboBox;

                    // CORREGIDO: Usar NumberStyles y CultureInfo correctos para parsing
                    decimal precio = 0;
                    if (!string.IsNullOrWhiteSpace(txtPrecio?.Text))
                    {
                        decimal.TryParse(txtPrecio.Text.Replace(".", ","), NumberStyles.Number, CultureInfo.CurrentCulture, out precio);
                        precio = Math.Round(precio, 2);
                    }

                    decimal costo = 0;
                    if (!string.IsNullOrWhiteSpace(txtCosto?.Text))
                    {
                        decimal.TryParse(txtCosto.Text.Replace(".", ","), NumberStyles.Number, CultureInfo.CurrentCulture, out costo);
                        costo = Math.Round(costo, 2);
                    }

                    decimal porcentaje = 0;
                    if (!string.IsNullOrWhiteSpace(txtPorcentaje?.Text))
                    {
                        decimal.TryParse(txtPorcentaje.Text.Replace(".", ","), NumberStyles.Number, CultureInfo.CurrentCulture, out porcentaje);
                        porcentaje = Math.Round(porcentaje, 2);
                    }

                    // CORREGIDO: Parsing correcto del valor de IVA
                    decimal iva = 21.00m; // Valor por defecto
                    if (!string.IsNullOrWhiteSpace(cmbIva?.Text))
                    {
                        if (!decimal.TryParse(cmbIva.Text.Replace(".", ","), NumberStyles.Number, CultureInfo.CurrentCulture, out iva))
                        {
                            iva = 21.00m; // Si falla el parsing, usar valor por defecto
                        }
                        iva = Math.Round(iva, 2);
                        
                        // VALIDACIÓN: Asegurar que el IVA esté en rango válido para DECIMAL(5,2)
                        if (iva < 0 || iva > 999.99m)
                        {
                            MessageBox.Show("El valor de IVA debe estar entre 0 y 999.99", "Error de Validación");
                            return;
                        }
                    }

                    int cantidad = int.TryParse(txtCantidad?.Text, out var cant) ? cant : 0;

                    cmd.Parameters.Add("@codigo", SqlDbType.VarChar, 50).Value = codigo;
                    cmd.Parameters.Add("@descripcion", SqlDbType.VarChar, 255).Value = txtDescripcion.Text.Trim();
                    cmd.Parameters.Add("@rubro", SqlDbType.VarChar, 100).Value = txtRubro?.Text?.Trim() ?? "";
                    cmd.Parameters.Add("@marca", SqlDbType.VarChar, 100).Value = txtMarca?.Text?.Trim() ?? "";
    
                    // CORREGIDO: Especificar precisión y escala para decimales
                    var precioParam = cmd.Parameters.Add("@precio", SqlDbType.Decimal);
                    precioParam.Precision = 18;
                    precioParam.Scale = 2;
                    precioParam.Value = precio;
    
                    var costoParam = cmd.Parameters.Add("@costo", SqlDbType.Decimal);
                    costoParam.Precision = 18;
                    costoParam.Scale = 2;
                    costoParam.Value = costo;
    
                    var porcentajeParam = cmd.Parameters.Add("@porcentaje", SqlDbType.Decimal);
                    porcentajeParam.Precision = 5;
                    porcentajeParam.Scale = 2;
                    porcentajeParam.Value = porcentaje;
    
                    cmd.Parameters.Add("@cantidad", SqlDbType.Int).Value = cantidad;
                    cmd.Parameters.Add("@proveedor", SqlDbType.VarChar, 100).Value = txtProveedor?.Text?.Trim() ?? "";
                    cmd.Parameters.Add("@PermiteAcumular", SqlDbType.Int).Value = PermiteAcumular;
    
                    // CORREGIDO: IVA con precisión y escala correctas (5,2)
                    var ivaParam = cmd.Parameters.Add("@iva", SqlDbType.Decimal);
                    ivaParam.Precision = 5;
                    ivaParam.Scale = 2;
                    ivaParam.Value = iva;
                }

                try
                {
                    cmd.ExecuteNonQuery();
                    
                    HuboCambios = true;
                    CodigoAgregado = codigo;

                    if (Modo == ModoFormulario.Modificar)
                    {
                        MessageBox.Show("Producto modificado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        string mensajeExito = Origen == OrigenLlamada.Ventas 
                            ? "Producto agregado correctamente para la venta." 
                            : "Producto agregado correctamente.";
                        
                        MessageBox.Show(mensajeExito, "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        if (Origen == OrigenLlamada.Ventas)
                        {
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        else
                        {
                            LimpiarControles();
                            txtCodigo?.Focus();
                        }
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show($"Error al guardar en la base de datos: {ex.Message}\n\nDetalles técnicos: {ex.Number}", 
                        "Error SQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error inesperado: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LimpiarControles()
        {
            // CORREGIDO: Remover txtIva de la lista
            var textBoxNames = new[] { "txtCodigo", "txtDescripcion", "txtRubro", "txtMarca", "txtCosto", "txtPorcentaje", "txtPrecio", "txtCantidad", "txtProveedor" };
            
            foreach (var name in textBoxNames)
            {
                var textBox = this.Controls.Find(name, true).FirstOrDefault() as TextBox;
                textBox?.Clear();
            }
            
            // Limpiar ComboBox de IVA
            var cmbIva = this.Controls.Find("cmbIva", true).FirstOrDefault() as ComboBox;
            if (cmbIva != null)
            {
                cmbIva.Text = "21.00"; // Volver al valor por defecto
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

        // NUEVO: Método para manejar Enter como Tab in ComboBox
        private void ComboBox_EnterAsTab(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Evita el beep y el salto de línea
                this.SelectNextControl((Control)sender, true, true, true, true);
            }
        }

        // NUEVO: Método para validar entrada en ComboBox de IVA
        private void CmbIva_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Permitir solo dígitos, coma, punto y teclas de control
            if (char.IsControl(e.KeyChar))
                return;

            if (e.KeyChar == '.')
                e.KeyChar = ',';

            ComboBox cmb = sender as ComboBox;
            string text = cmb.Text;

            if (char.IsDigit(e.KeyChar))
            {
                if (text.Contains(","))
                {
                    int index = text.IndexOf(",");
                    // Si el cursor está en la parte decimal, permitir máximo 2 decimales
                    if (cmb.SelectionStart > index)
                    {
                        string decimalPart = text.Substring(index + 1);
                        if (decimalPart.Length >= 2 && cmb.SelectionLength == 0)
                            e.Handled = true;
                    }
                    else
                    {
                        // Si el cursor está en la parte entera, permitir máximo 2 dígitos
                        string integerPart = text.Substring(0, index);
                        if (integerPart.Length >= 2 && cmb.SelectionLength == 0)
                            e.Handled = true;
                    }
                }
                else
                {
                    // Sin coma, permitir máximo 2 dígitos enteros
                    if (text.Length >= 2 && cmb.SelectionLength == 0)
                        e.Handled = true;
                }
                return;
            }
            
            if (e.KeyChar == ',')
            {
                if (text.Contains(","))
                    e.Handled = true;
                return;
            }
            
            e.Handled = true;
        }

        public bool HuboCambios { get; private set; } = false;

        // NUEVO: Método para precargar datos desde Ventas
        public void PrecargarDatos(string codigo, decimal? precio)
        {
            Origen = OrigenLlamada.Ventas;

            var txtCodigo = this.Controls.Find("txtCodigo", true).FirstOrDefault() as TextBox;
            var txtDescripcion = this.Controls.Find("txtDescripcion", true).FirstOrDefault() as TextBox;
            var txtPrecio = this.Controls.Find("txtPrecio", true).FirstOrDefault() as TextBox;

            if (txtCodigo != null)
            {
                txtCodigo.Text = codigo;
                txtCodigo.ReadOnly = true; // Bloquear edición del código desde Ventas
            }

            if (txtDescripcion != null)
            {
                txtDescripcion.Focus(); // Enfocar descripción para que el usuario la complete
            }

            if (txtPrecio != null && precio.HasValue)
            {
                txtPrecio.Text = precio.Value.ToString("F2");
            }
        }
    }
}
