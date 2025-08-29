namespace Comercio.NET.Formularios
{
    partial class Productos
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
            GrillaProductos = new DataGridView();
            lblContador = new Label();
            txtFiltroDescripcion = new TextBox();
            lbBuscar = new Label();
            btnSalir = new Button();
            btnAgregarProducto = new Button();
            btnModificarProducto = new Button();
            ((System.ComponentModel.ISupportInitialize)GrillaProductos).BeginInit();
            SuspendLayout();
            // 
            // GrillaProductos
            // 
            GrillaProductos.AllowUserToAddRows = false;
            GrillaProductos.AllowUserToDeleteRows = false;
            GrillaProductos.AllowUserToResizeRows = false;
            GrillaProductos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            //GrillaProductos.Location = new Point(12, 47);
            GrillaProductos.Name = "GrillaProductos";
            GrillaProductos.ReadOnly = true;
            GrillaProductos.Size = new Size(927, 366);
            GrillaProductos.TabIndex = 0;
            // 
            // lblContador
            // 
            lblContador.AutoSize = true;
            lblContador.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblContador.Location = new Point(832, 426);
            lblContador.Name = "lblContador";
            lblContador.Size = new Size(62, 17);
            lblContador.TabIndex = 1;
            lblContador.Text = "registros";
            // 
            // txtFiltroDescripcion
            // 
            txtFiltroDescripcion.Location = new Point(156, 12);
            txtFiltroDescripcion.Name = "txtFiltroDescripcion";
            txtFiltroDescripcion.Size = new Size(158, 23);
            txtFiltroDescripcion.TabIndex = 2;
            // 
            // lbBuscar
            // 
            lbBuscar.AutoSize = true;
            lbBuscar.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lbBuscar.Location = new Point(20, 16);
            lbBuscar.Name = "lbBuscar";
            lbBuscar.Size = new Size(130, 15);
            lbBuscar.TabIndex = 3;
            lbBuscar.Text = "Buscar por descipción:";
            // 
            // btnSalir
            // 
            btnSalir.Location = new Point(864, 12);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(75, 23);
            btnSalir.TabIndex = 4;
            btnSalir.Text = "Salir";
            btnSalir.UseVisualStyleBackColor = true;
            btnSalir.Click += btnSalir_Click;
            // 
            // btnAgregarProducto
            // 
            btnAgregarProducto.Location = new Point(397, 13);
            btnAgregarProducto.Name = "btnAgregarProducto";
            btnAgregarProducto.Size = new Size(131, 23);
            btnAgregarProducto.TabIndex = 5;
            btnAgregarProducto.Text = "Agregar Producto";
            btnAgregarProducto.UseVisualStyleBackColor = true;
            btnAgregarProducto.Click += btnAgregarProducto_Click;
            // 
            // btnModificarProducto
            // 
            btnModificarProducto.Location = new Point(567, 12);
            btnModificarProducto.Name = "btnModificarProducto";
            btnModificarProducto.Size = new Size(131, 23);
            btnModificarProducto.TabIndex = 6;
            btnModificarProducto.Text = "Modificar Producto";
            btnModificarProducto.UseVisualStyleBackColor = true;
            btnModificarProducto.Click += btnModificarProducto_Click;
            // 
            // Productos
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.BlanchedAlmond;
            ClientSize = new Size(953, 450);
            Controls.Add(btnModificarProducto);
            Controls.Add(btnAgregarProducto);
            Controls.Add(btnSalir);
            Controls.Add(lbBuscar);
            Controls.Add(txtFiltroDescripcion);
            Controls.Add(lblContador);
            Controls.Add(GrillaProductos);
            MinimumSize = new Size(969, 489);
            Name = "Productos";
            Text = "Productos";
            Load += Productos_Load;
            ((System.ComponentModel.ISupportInitialize)GrillaProductos).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView GrillaProductos;
        private Label lblContador;
        private TextBox txtFiltroDescripcion;
        private Label lbBuscar;
        private Button btnSalir;
        private Button btnAgregarProducto;
        private Button btnModificarProducto;
    }
}