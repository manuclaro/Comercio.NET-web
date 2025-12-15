namespace Comercio.NET.Formularios
{
    partial class ProductosOptimizado
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.GrillaProductos = new System.Windows.Forms.DataGridView();
            this.txtFiltroDescripcion = new System.Windows.Forms.TextBox();
            this.lblFiltro = new System.Windows.Forms.Label();
            this.btnAgregarProducto = new System.Windows.Forms.Button();
            this.btnModificarProducto = new System.Windows.Forms.Button();
            this.lblContador = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.GrillaProductos)).BeginInit();
            this.SuspendLayout();
            // 
            // GrillaProductos
            // 
            this.GrillaProductos.AllowUserToAddRows = false;
            this.GrillaProductos.AllowUserToDeleteRows = false;
            this.GrillaProductos.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GrillaProductos.ColumnHeadersHeight = 40;
            this.GrillaProductos.Location = new System.Drawing.Point(15, 90);
            this.GrillaProductos.MultiSelect = false;
            this.GrillaProductos.Name = "GrillaProductos";
            this.GrillaProductos.ReadOnly = true;
            this.GrillaProductos.RowHeadersVisible = false;
            this.GrillaProductos.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.GrillaProductos.Size = new System.Drawing.Size(957, 485);
            this.GrillaProductos.TabIndex = 0;
            this.GrillaProductos.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.GrillaProductos_CellDoubleClick);
            this.GrillaProductos.SelectionChanged += new System.EventHandler(this.GrillaProductos_SelectionChanged);
            this.GrillaProductos.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.GrillaProductos_DataBindingComplete);
            // 
            // txtFiltroDescripcion
            // 
            this.txtFiltroDescripcion.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtFiltroDescripcion.Location = new System.Drawing.Point(15, 40);
            this.txtFiltroDescripcion.Name = "txtFiltroDescripcion";
            this.txtFiltroDescripcion.PlaceholderText = "Escriba para filtrar productos...";
            this.txtFiltroDescripcion.Size = new System.Drawing.Size(400, 27);
            this.txtFiltroDescripcion.TabIndex = 1;
            this.txtFiltroDescripcion.TextChanged += new System.EventHandler(this.TxtFiltroDescripcion_TextChanged);
            this.txtFiltroDescripcion.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtFiltroDescripcion_KeyDown);
            // 
            // lblFiltro
            // 
            this.lblFiltro.AutoSize = true;
            this.lblFiltro.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblFiltro.Location = new System.Drawing.Point(15, 15);
            this.lblFiltro.Name = "lblFiltro";
            this.lblFiltro.Size = new System.Drawing.Size(115, 15);
            this.lblFiltro.TabIndex = 2;
            this.lblFiltro.Text = "🔍 Buscar producto:";
            // 
            // btnModificarProducto
            // 
            this.btnModificarProducto.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnModificarProducto.Location = new System.Drawing.Point(425, 40);
            this.btnModificarProducto.Name = "btnModificarProducto";
            this.btnModificarProducto.Size = new System.Drawing.Size(120, 27);
            this.btnModificarProducto.TabIndex = 4;
            this.btnModificarProducto.Text = "✏️ Modificar";
            this.btnModificarProducto.UseVisualStyleBackColor = true;
            // 
            // btnAgregarProducto
            // 
            this.btnAgregarProducto.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnAgregarProducto.Location = new System.Drawing.Point(555, 40);
            this.btnAgregarProducto.Name = "btnAgregarProducto";
            this.btnAgregarProducto.Size = new System.Drawing.Size(120, 27);
            this.btnAgregarProducto.TabIndex = 3;
            this.btnAgregarProducto.Text = "➕ Agregar";
            this.btnAgregarProducto.UseVisualStyleBackColor = true;
            // 
            // lblContador
            // 
            this.lblContador.AutoSize = true;
            this.lblContador.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(227)))), ((int)(((byte)(242)))), ((int)(((byte)(253)))));
            this.lblContador.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblContador.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(80)))), ((int)(((byte)(100)))));
            this.lblContador.Location = new System.Drawing.Point(425, 15);
            this.lblContador.Name = "lblContador";
            this.lblContador.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.lblContador.Size = new System.Drawing.Size(124, 23);
            this.lblContador.TabIndex = 5;
            this.lblContador.Text = "📊 Registros: 0 de 0";
            this.lblContador.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // ProductosOptimizado
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.ClientSize = new System.Drawing.Size(984, 591);
            this.Controls.Add(this.lblContador);
            this.Controls.Add(this.btnModificarProducto);
            this.Controls.Add(this.btnAgregarProducto);
            this.Controls.Add(this.lblFiltro);
            this.Controls.Add(this.txtFiltroDescripcion);
            this.Controls.Add(this.GrillaProductos);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "ProductosOptimizado";
            this.Text = "Gestión de Productos";
            this.Load += new System.EventHandler(this.ProductosOptimizado_Load);
            ((System.ComponentModel.ISupportInitialize)(this.GrillaProductos)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView GrillaProductos;
        private System.Windows.Forms.TextBox txtFiltroDescripcion;
        private System.Windows.Forms.Label lblFiltro;
        private System.Windows.Forms.Button btnAgregarProducto;
        private System.Windows.Forms.Button btnModificarProducto;
        private System.Windows.Forms.Label lblContador;
    }
}