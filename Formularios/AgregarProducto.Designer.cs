
namespace Comercio.NET.Formularios
{
    partial class frmAgregarProducto
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnGuardar = new Button();
            txtCodigo = new TextBox();
            txtDescripcion = new TextBox();
            txtMarca = new TextBox();
            txtRubro = new TextBox();
            txtProveedor = new TextBox();
            txtCantidad = new TextBox();
            txtPorcentaje = new TextBox();
            txtCosto = new TextBox();
            txtPrecio = new TextBox();
            lbCodigo = new Label();
            lbDescripción = new Label();
            lbMarca = new Label();
            lbRubro = new Label();
            lbCantidad = new Label();
            lbPorcentaje = new Label();
            lbCosto = new Label();
            lbPrecio = new Label();
            lbProveedor = new Label();
            SuspendLayout();
            // 
            // btnGuardar
            // 
            btnGuardar.Location = new Point(438, 244);
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Size = new Size(75, 23);
            btnGuardar.TabIndex = 9;
            btnGuardar.Text = "Guardar";
            btnGuardar.UseVisualStyleBackColor = true;
            btnGuardar.Click += btnGuardar_Click;
            // 
            // txtCodigo
            // 
            txtCodigo.Location = new Point(195, 12);
            txtCodigo.Name = "txtCodigo";
            txtCodigo.Size = new Size(139, 23);
            txtCodigo.TabIndex = 0;
            // 
            // txtDescripcion
            // 
            txtDescripcion.Location = new Point(195, 41);
            txtDescripcion.Name = "txtDescripcion";
            txtDescripcion.Size = new Size(250, 23);
            txtDescripcion.TabIndex = 1;
            // 
            // txtMarca
            // 
            txtMarca.Location = new Point(195, 99);
            txtMarca.Name = "txtMarca";
            txtMarca.Size = new Size(139, 23);
            txtMarca.TabIndex = 3;
            // 
            // txtRubro
            // 
            txtRubro.Location = new Point(195, 70);
            txtRubro.Name = "txtRubro";
            txtRubro.Size = new Size(139, 23);
            txtRubro.TabIndex = 2;
            // 
            // txtProveedor
            // 
            txtProveedor.Location = new Point(195, 244);
            txtProveedor.Name = "txtProveedor";
            txtProveedor.Size = new Size(139, 23);
            txtProveedor.TabIndex = 8;
            // 
            // txtCantidad
            // 
            txtCantidad.Location = new Point(195, 215);
            txtCantidad.Name = "txtCantidad";
            txtCantidad.Size = new Size(139, 23);
            txtCantidad.TabIndex = 7;
            // 
            // txtPorcentaje
            // 
            txtPorcentaje.Location = new Point(195, 157);
            txtPorcentaje.Name = "txtPorcentaje";
            txtPorcentaje.Size = new Size(139, 23);
            txtPorcentaje.TabIndex = 5;
            // 
            // txtCosto
            // 
            txtCosto.Location = new Point(195, 128);
            txtCosto.Name = "txtCosto";
            txtCosto.Size = new Size(139, 23);
            txtCosto.TabIndex = 4;
            // 
            // txtPrecio
            // 
            txtPrecio.Location = new Point(195, 186);
            txtPrecio.Name = "txtPrecio";
            txtPrecio.Size = new Size(139, 23);
            txtPrecio.TabIndex = 6;
            // 
            // lbCodigo
            // 
            lbCodigo.AutoSize = true;
            lbCodigo.Location = new Point(142, 15);
            lbCodigo.Name = "lbCodigo";
            lbCodigo.Size = new Size(46, 15);
            lbCodigo.TabIndex = 11;
            lbCodigo.Text = "Codigo";
            // 
            // lbDescripción
            // 
            lbDescripción.AutoSize = true;
            lbDescripción.Location = new Point(120, 44);
            lbDescripción.Name = "lbDescripción";
            lbDescripción.Size = new Size(69, 15);
            lbDescripción.TabIndex = 12;
            lbDescripción.Text = "Descripcion";
            // 
            // lbMarca
            // 
            lbMarca.AutoSize = true;
            lbMarca.Location = new Point(148, 102);
            lbMarca.Name = "lbMarca";
            lbMarca.Size = new Size(40, 15);
            lbMarca.TabIndex = 14;
            lbMarca.Text = "Marca";
            // 
            // lbRubro
            // 
            lbRubro.AutoSize = true;
            lbRubro.Location = new Point(149, 73);
            lbRubro.Name = "lbRubro";
            lbRubro.Size = new Size(39, 15);
            lbRubro.TabIndex = 13;
            lbRubro.Text = "Rubro";
            // 
            // lbCantidad
            // 
            lbCantidad.AutoSize = true;
            lbCantidad.Location = new Point(133, 218);
            lbCantidad.Name = "lbCantidad";
            lbCantidad.Size = new Size(55, 15);
            lbCantidad.TabIndex = 18;
            lbCantidad.Text = "Cantidad";
            // 
            // lbPorcentaje
            // 
            lbPorcentaje.AutoSize = true;
            lbPorcentaje.Location = new Point(126, 160);
            lbPorcentaje.Name = "lbPorcentaje";
            lbPorcentaje.Size = new Size(63, 15);
            lbPorcentaje.TabIndex = 17;
            lbPorcentaje.Text = "Porcentaje";
            // 
            // lbCosto
            // 
            lbCosto.AutoSize = true;
            lbCosto.Location = new Point(148, 131);
            lbCosto.Name = "lbCosto";
            lbCosto.Size = new Size(38, 15);
            lbCosto.TabIndex = 16;
            lbCosto.Text = "Costo";
            // 
            // lbPrecio
            // 
            lbPrecio.AutoSize = true;
            lbPrecio.Location = new Point(148, 189);
            lbPrecio.Name = "lbPrecio";
            lbPrecio.Size = new Size(40, 15);
            lbPrecio.TabIndex = 15;
            lbPrecio.Text = "Precio";
            // 
            // lbProveedor
            // 
            lbProveedor.AutoSize = true;
            lbProveedor.Location = new Point(125, 247);
            lbProveedor.Name = "lbProveedor";
            lbProveedor.Size = new Size(61, 15);
            lbProveedor.TabIndex = 19;
            lbProveedor.Text = "Proveedor";
            // 
            // frmAgregarProducto
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.DarkSeaGreen;
            ClientSize = new Size(799, 283);
            Controls.Add(lbProveedor);
            Controls.Add(lbCantidad);
            Controls.Add(lbPorcentaje);
            Controls.Add(lbCosto);
            Controls.Add(lbPrecio);
            Controls.Add(lbMarca);
            Controls.Add(lbRubro);
            Controls.Add(lbDescripción);
            Controls.Add(lbCodigo);
            Controls.Add(txtPrecio);
            Controls.Add(txtProveedor);
            Controls.Add(txtCantidad);
            Controls.Add(txtPorcentaje);
            Controls.Add(txtCosto);
            Controls.Add(txtMarca);
            Controls.Add(txtRubro);
            Controls.Add(txtDescripcion);
            Controls.Add(txtCodigo);
            Controls.Add(btnGuardar);
            Name = "frmAgregarProducto";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Agregar Producto";
            Load += frmAgregarProducto_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnGuardar;
        private TextBox txtCodigo;
        private TextBox txtDescripcion;
        private TextBox txtMarca;
        private TextBox txtRubro;
        private TextBox txtProveedor;
        private TextBox txtCantidad;
        private TextBox txtPorcentaje;
        private TextBox txtCosto;
        private TextBox txtPrecio;
        private Label lbCodigo;
        private Label lbDescripción;
        private Label lbMarca;
        private Label lbRubro;
        private Label lbCantidad;
        private Label lbPorcentaje;
        private Label lbCosto;
        private Label lbPrecio;
        private Label lbProveedor;
    }
}