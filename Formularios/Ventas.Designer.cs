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
            lbBuscarProducto = new Label();
            txtBuscarProducto = new TextBox();
            btnAgregar = new Button();
            btnSalir = new Button();
            lbDescripcionProducto = new Label();
            btnFinalizarVenta = new Button();
            chkEsCtaCte = new CheckBox();
            cbnombreCtaCte = new ComboBox();
            panelFooter = new Panel();
            lbTotal = new Label();
            lbCantidadProductos = new Label();
            panelHeader = new Panel();
            lbPrecio = new Label();
            txtPrecio = new TextBox();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            panelFooter.SuspendLayout();
            panelHeader.SuspendLayout();
            SuspendLayout();
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Window;
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle1.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
            dataGridView1.DefaultCellStyle = dataGridViewCellStyle1;
            dataGridView1.Location = new Point(0, 171);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.Size = new Size(757, 257);
            dataGridView1.TabIndex = 8;
            // 
            // lbBuscarProducto
            // 
            lbBuscarProducto.AutoSize = true;
            lbBuscarProducto.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lbBuscarProducto.Location = new Point(28, 92);
            lbBuscarProducto.Name = "lbBuscarProducto";
            lbBuscarProducto.Size = new Size(101, 15);
            lbBuscarProducto.TabIndex = 11;
            lbBuscarProducto.Text = "Buscar producto:";
            // 
            // txtBuscarProducto
            // 
            txtBuscarProducto.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtBuscarProducto.Location = new Point(135, 81);
            txtBuscarProducto.Name = "txtBuscarProducto";
            txtBuscarProducto.Size = new Size(161, 33);
            txtBuscarProducto.TabIndex = 0;
            // 
            // btnAgregar
            // 
            btnAgregar.Image = Properties.Resources.Add;
            btnAgregar.ImageAlign = ContentAlignment.TopLeft;
            btnAgregar.Location = new Point(317, 79);
            btnAgregar.MinimumSize = new Size(120, 40);
            btnAgregar.Name = "btnAgregar";
            btnAgregar.Size = new Size(120, 42);
            btnAgregar.TabIndex = 2;
            btnAgregar.Text = "Agregar";
            btnAgregar.TextAlign = ContentAlignment.MiddleRight;
            btnAgregar.UseVisualStyleBackColor = true;
            // 
            // btnSalir
            // 
            btnSalir.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSalir.Image = Properties.Resources.exit_door;
            btnSalir.ImageAlign = ContentAlignment.MiddleLeft;
            btnSalir.Location = new Point(632, 81);
            btnSalir.MinimumSize = new Size(100, 40);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(100, 40);
            btnSalir.TabIndex = 4;
            btnSalir.Text = "Salir";
            btnSalir.TextAlign = ContentAlignment.MiddleRight;
            btnSalir.UseVisualStyleBackColor = true;
            btnSalir.Click += btnSalir_Click;
            // 
            // lbDescripcionProducto
            // 
            lbDescripcionProducto.AutoSize = true;
            lbDescripcionProducto.Dock = DockStyle.Right;
            lbDescripcionProducto.Font = new Font("Segoe UI", 27.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lbDescripcionProducto.Location = new Point(370, 0);
            lbDescripcionProducto.Name = "lbDescripcionProducto";
            lbDescripcionProducto.Size = new Size(387, 50);
            lbDescripcionProducto.TabIndex = 8;
            lbDescripcionProducto.Text = "DescripcionProducto";
            // 
            // btnFinalizarVenta
            // 
            btnFinalizarVenta.Image = Properties.Resources.PrintManager;
            btnFinalizarVenta.ImageAlign = ContentAlignment.MiddleLeft;
            btnFinalizarVenta.Location = new Point(456, 81);
            btnFinalizarVenta.MinimumSize = new Size(140, 40);
            btnFinalizarVenta.Name = "btnFinalizarVenta";
            btnFinalizarVenta.Size = new Size(159, 40);
            btnFinalizarVenta.TabIndex = 3;
            btnFinalizarVenta.Text = "Finalizar Venta";
            btnFinalizarVenta.TextAlign = ContentAlignment.MiddleRight;
            btnFinalizarVenta.UseVisualStyleBackColor = true;
            // 
            // chkEsCtaCte
            // 
            chkEsCtaCte.AutoSize = true;
            chkEsCtaCte.Location = new Point(369, 136);
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
            cbnombreCtaCte.Location = new Point(456, 132);
            cbnombreCtaCte.Name = "cbnombreCtaCte";
            cbnombreCtaCte.Size = new Size(121, 23);
            cbnombreCtaCte.TabIndex = 13;
            cbnombreCtaCte.Visible = false;
            // 
            // panelFooter
            // 
            panelFooter.BackColor = Color.FromArgb(0, 120, 215);
            panelFooter.Controls.Add(lbTotal);
            panelFooter.Controls.Add(lbCantidadProductos);
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Location = new Point(0, 432);
            panelFooter.Name = "panelFooter";
            panelFooter.Size = new Size(757, 70);
            panelFooter.TabIndex = 14;
            // 
            // lbTotal
            // 
            lbTotal.AutoSize = true;
            lbTotal.ForeColor = Color.White;
            lbTotal.Location = new Point(711, 22);
            lbTotal.Name = "lbTotal";
            lbTotal.Size = new Size(43, 15);
            lbTotal.TabIndex = 1;
            lbTotal.Text = "lbTotal";
            // 
            // lbCantidadProductos
            // 
            lbCantidadProductos.AutoSize = true;
            lbCantidadProductos.ForeColor = Color.White;
            lbCantidadProductos.Location = new Point(0, 22);
            lbCantidadProductos.Name = "lbCantidadProductos";
            lbCantidadProductos.Size = new Size(119, 15);
            lbCantidadProductos.TabIndex = 0;
            lbCantidadProductos.Text = "lbCantidadProductos";
            // 
            // panelHeader
            // 
            panelHeader.BackColor = SystemColors.Highlight;
            panelHeader.Controls.Add(lbDescripcionProducto);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(757, 58);
            panelHeader.TabIndex = 15;
            // 
            // lbPrecio
            // 
            lbPrecio.AutoSize = true;
            lbPrecio.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lbPrecio.Location = new Point(84, 135);
            lbPrecio.Name = "lbPrecio";
            lbPrecio.Size = new Size(45, 15);
            lbPrecio.TabIndex = 17;
            lbPrecio.Text = "Precio:";
            // 
            // txtPrecio
            // 
            txtPrecio.Enabled = false;
            txtPrecio.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtPrecio.Location = new Point(135, 122);
            txtPrecio.MaxLength = 9;
            txtPrecio.Name = "txtPrecio";
            txtPrecio.Size = new Size(161, 33);
            txtPrecio.TabIndex = 1;
            txtPrecio.TextAlign = HorizontalAlignment.Center;
            // 
            // Ventas
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.WhiteSmoke;
            ClientSize = new Size(757, 502);
            Controls.Add(lbPrecio);
            Controls.Add(txtPrecio);
            Controls.Add(panelHeader);
            Controls.Add(cbnombreCtaCte);
            Controls.Add(chkEsCtaCte);
            Controls.Add(btnFinalizarVenta);
            Controls.Add(btnSalir);
            Controls.Add(btnAgregar);
            Controls.Add(lbBuscarProducto);
            Controls.Add(txtBuscarProducto);
            Controls.Add(dataGridView1);
            Controls.Add(panelFooter);
            Name = "Ventas";
            Text = "Ventas";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            panelFooter.ResumeLayout(false);
            panelFooter.PerformLayout();
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView dataGridView1;
        private Label lbBuscarProducto;
        private TextBox txtBuscarProducto;
        private Button btnAgregar;
        private Button btnSalir;
        private Label lbDescripcionProducto;
        private Button btnFinalizarVenta;
        private CheckBox chkEsCtaCte;
        private ComboBox cbnombreCtaCte;
        private Panel panelFooter;
        private Panel panelHeader;
        private Label lbTotal;
        private Label lbCantidadProductos;
        private Label lbPrecio;
        private TextBox txtPrecio;
    }
}