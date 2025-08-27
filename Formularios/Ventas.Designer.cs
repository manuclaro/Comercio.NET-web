namespace Comercio.NET
{
    partial class Ventas
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            dataGridView1 = new DataGridView();
            lblCantidadProductos = new Label();
            lbTotal = new Label();
            lbBuscarProducto = new Label();
            txtBuscarProducto = new TextBox();
            btnAgregar = new Button();
            btnSalir = new Button();
            lbDescripcionProducto = new Label();
            btnFinalizarVenta = new Button();
            chkEsCtaCte = new CheckBox();
            cbnombreCtaCte = new ComboBox();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Window;
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle1.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
            dataGridView1.DefaultCellStyle = dataGridViewCellStyle1;
            dataGridView1.Location = new Point(2, 144);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new Size(754, 274);
            dataGridView1.TabIndex = 8;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridView1.RowHeadersVisible = false;
            // 
            // lblCantidadProductos
            // 
            lblCantidadProductos.AutoSize = true;
            lblCantidadProductos.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            lblCantidadProductos.Location = new Point(73, 439);
            lblCantidadProductos.Name = "lblCantidadProductos";
            lblCantidadProductos.Size = new Size(70, 17);
            lblCantidadProductos.TabIndex = 9;
            lblCantidadProductos.Text = "Productos";
            // 
            // lbTotal
            // 
            lbTotal.AutoSize = true;
            lbTotal.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            lbTotal.Location = new Point(572, 438);
            lbTotal.Name = "lbTotal";
            lbTotal.Size = new Size(39, 17);
            lbTotal.TabIndex = 10;
            lbTotal.Text = "Total";
            // 
            // lbBuscarProducto
            // 
            lbBuscarProducto.AutoSize = true;
            lbBuscarProducto.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lbBuscarProducto.Location = new Point(31, 45);
            lbBuscarProducto.Name = "lbBuscarProducto";
            lbBuscarProducto.Size = new Size(101, 15);
            lbBuscarProducto.TabIndex = 11;
            lbBuscarProducto.Text = "Buscar producto:";
            // 
            // txtBuscarProducto
            // 
            txtBuscarProducto.Location = new Point(138, 42);
            txtBuscarProducto.Name = "txtBuscarProducto";
            txtBuscarProducto.Size = new Size(161, 23);
            txtBuscarProducto.TabIndex = 0;
            // 
            // btnAgregar
            // 
            btnAgregar.Image = Properties.Resources.Add;
            btnAgregar.ImageAlign = ContentAlignment.TopLeft;
            btnAgregar.Location = new Point(339, 41);
            btnAgregar.MinimumSize = new Size(120, 40);
            btnAgregar.Name = "btnAgregar";
            btnAgregar.Size = new Size(120, 40);
            btnAgregar.TabIndex = 1;
            btnAgregar.Text = "Agregar";
            btnAgregar.TextAlign = ContentAlignment.MiddleRight;
            btnAgregar.UseVisualStyleBackColor = true;
            // 
            // btnSalir
            // 
            btnSalir.Image = Properties.Resources.exit_door;
            btnSalir.ImageAlign = ContentAlignment.MiddleLeft;
            btnSalir.Location = new Point(645, 41);
            btnSalir.MinimumSize = new Size(100, 40);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(100, 40);
            btnSalir.TabIndex = 3;
            btnSalir.Text = "Salir";
            btnSalir.TextAlign = ContentAlignment.MiddleRight;
            btnSalir.UseVisualStyleBackColor = true;
            btnSalir.Click += btnSalir_Click;
            // 
            // lbDescripcionProducto
            // 
            lbDescripcionProducto.AutoSize = true;
            lbDescripcionProducto.Location = new Point(131, 104);
            lbDescripcionProducto.Name = "lbDescripcionProducto";
            lbDescripcionProducto.Size = new Size(118, 15);
            lbDescripcionProducto.TabIndex = 8;
            lbDescripcionProducto.Text = "DescripcionProducto";
            // 
            // btnFinalizarVenta
            // 
            btnFinalizarVenta.Image = Properties.Resources.PrintManager;
            btnFinalizarVenta.ImageAlign = ContentAlignment.MiddleLeft;
            btnFinalizarVenta.Location = new Point(482, 40);
            btnFinalizarVenta.MinimumSize = new Size(140, 40);
            btnFinalizarVenta.Name = "btnFinalizarVenta";
            btnFinalizarVenta.Size = new Size(140, 40);
            btnFinalizarVenta.TabIndex = 2;
            btnFinalizarVenta.Text = "Finalizar Venta";
            btnFinalizarVenta.TextAlign = ContentAlignment.MiddleRight;
            btnFinalizarVenta.UseVisualStyleBackColor = true;
            // 
            // chkEsCtaCte
            // 
            chkEsCtaCte.AutoSize = true;
            chkEsCtaCte.Location = new Point(339, 100);
            chkEsCtaCte.Name = "chkEsCtaCte";
            chkEsCtaCte.Size = new Size(68, 19);
            chkEsCtaCte.TabIndex = 12;
            chkEsCtaCte.Text = "Cta.Cte.";
            chkEsCtaCte.UseVisualStyleBackColor = true;
            chkEsCtaCte.CheckedChanged += chkEsCtaCte_CheckedChanged;
            // 
            // cbnombreCtaCte
            // 
            cbnombreCtaCte.FormattingEnabled = true;
            cbnombreCtaCte.Location = new Point(432, 96);
            cbnombreCtaCte.Name = "cbnombreCtaCte";
            cbnombreCtaCte.Size = new Size(121, 23);
            cbnombreCtaCte.TabIndex = 13;
            cbnombreCtaCte.Visible = false;
            // 
            // Ventas
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.LightBlue;
            ClientSize = new Size(757, 462);
            Controls.Add(cbnombreCtaCte);
            Controls.Add(chkEsCtaCte);
            Controls.Add(btnFinalizarVenta);
            Controls.Add(lbDescripcionProducto);
            Controls.Add(btnSalir);
            Controls.Add(btnAgregar);
            Controls.Add(lbBuscarProducto);
            Controls.Add(txtBuscarProducto);
            Controls.Add(lbTotal);
            Controls.Add(lblCantidadProductos);
            Controls.Add(dataGridView1);
            MinimumSize = new Size(773, 501);
            Name = "Ventas";
            Text = "Ventas";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView dataGridView1;
        private Label lblCantidadProductos;
        private Label lbTotal;
        private Label lbBuscarProducto;
        private TextBox txtBuscarProducto;
        private Button btnAgregar;
        private Button btnSalir;
        private Label lbDescripcionProducto;
        private Button btnFinalizarVenta;
        private CheckBox chkEsCtaCte;
        private ComboBox cbnombreCtaCte;
    }
}