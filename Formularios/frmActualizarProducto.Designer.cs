using System;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    partial class frmActualizarProducto
    {
        private System.ComponentModel.IContainer components = null;
        
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Form
        private void InitializeComponent()
        {
            lblCodigo = new Label();
            txtCodigo = new TextBox();
            btnBuscar = new Button();
            lblNombre = new Label();
            txtNombre = new TextBox();
            lblStockActual = new Label();
            txtStockActual = new TextBox();
            lblNuevoCosto = new Label();
            txtNuevoCosto = new TextBox();
            lblNuevoPorcentaje = new Label();
            txtNuevoPorcentaje = new TextBox();
            lblIva = new Label();
            txtIva = new TextBox();
            lblValorVenta = new Label();
            txtValorVenta = new TextBox();
            btnAplicar = new Button();
            btnCerrar = new Button();
            SuspendLayout();
            // 
            // lblCodigo
            // 
            lblCodigo.AutoSize = true;
            lblCodigo.Location = new Point(12, 15);
            lblCodigo.Name = "lblCodigo";
            lblCodigo.Size = new Size(101, 15);
            lblCodigo.TabIndex = 0;
            lblCodigo.Text = "Código Producto:";
            // 
            // txtCodigo
            // 
            txtCodigo.Location = new Point(136, 12);
            txtCodigo.Name = "txtCodigo";
            txtCodigo.Size = new Size(150, 23);
            txtCodigo.TabIndex = 1;
            // 
            // btnBuscar
            // 
            btnBuscar.Location = new Point(300, 10);
            btnBuscar.Name = "btnBuscar";
            btnBuscar.Size = new Size(75, 25);
            btnBuscar.TabIndex = 2;
            btnBuscar.Text = "Buscar";
            btnBuscar.UseVisualStyleBackColor = true;
            btnBuscar.Click += btnBuscar_Click;
            // 
            // lblNombre
            // 
            lblNombre.AutoSize = true;
            lblNombre.Location = new Point(12, 50);
            lblNombre.Name = "lblNombre";
            lblNombre.Size = new Size(106, 15);
            lblNombre.TabIndex = 3;
            lblNombre.Text = "Descripción:";
            // 
            // txtNombre
            // 
            txtNombre.Location = new Point(136, 47);
            txtNombre.Name = "txtNombre";
            txtNombre.Size = new Size(245, 23);
            txtNombre.TabIndex = 4;
            // 
            // lblStockActual
            // 
            lblStockActual.AutoSize = true;
            lblStockActual.Location = new Point(12, 85);
            lblStockActual.Name = "lblStockActual";
            lblStockActual.Size = new Size(76, 15);
            lblStockActual.TabIndex = 5;
            lblStockActual.Text = "Stock Actual:";
            // 
            // txtStockActual
            // 
            txtStockActual.Location = new Point(136, 82);
            txtStockActual.Name = "txtStockActual";
            txtStockActual.Size = new Size(100, 23);
            txtStockActual.TabIndex = 6;
            // 
            // lblNuevoCosto
            // 
            lblNuevoCosto.AutoSize = true;
            lblNuevoCosto.Location = new Point(12, 120);
            lblNuevoCosto.Name = "lblNuevoCosto";
            lblNuevoCosto.Size = new Size(79, 15);
            lblNuevoCosto.TabIndex = 7;
            lblNuevoCosto.Text = "Costo:";
            // 
            // txtNuevoCosto
            // 
            txtNuevoCosto.Location = new Point(136, 117);
            txtNuevoCosto.Name = "txtNuevoCosto";
            txtNuevoCosto.Size = new Size(100, 23);
            txtNuevoCosto.TabIndex = 8;
            txtNuevoCosto.TextChanged += CalcularVenta;
            // 
            // lblNuevoPorcentaje
            // 
            lblNuevoPorcentaje.AutoSize = true;
            lblNuevoPorcentaje.Location = new Point(12, 155);
            lblNuevoPorcentaje.Name = "lblNuevoPorcentaje";
            lblNuevoPorcentaje.Size = new Size(122, 15);
            lblNuevoPorcentaje.TabIndex = 9;
            lblNuevoPorcentaje.Text = "% de Ganancia:";
            // 
            // txtNuevoPorcentaje
            // 
            txtNuevoPorcentaje.Location = new Point(136, 152);
            txtNuevoPorcentaje.Name = "txtNuevoPorcentaje";
            txtNuevoPorcentaje.Size = new Size(100, 23);
            txtNuevoPorcentaje.TabIndex = 10;
            txtNuevoPorcentaje.TextChanged += CalcularVenta;
            // 
            // lblIva
            // 
            lblIva.AutoSize = true;
            lblIva.Location = new Point(12, 190);
            lblIva.Name = "lblIva";
            lblIva.Size = new Size(84, 15);
            lblIva.TabIndex = 11;
            lblIva.Text = "Alícuota IVA %:";
            // 
            // txtIva
            // 
            txtIva.Location = new Point(136, 187);
            txtIva.Name = "txtIva";
            txtIva.Size = new Size(100, 23);
            txtIva.TabIndex = 12;
            txtIva.KeyPress += txtIva_KeyPress;
            // 
            // lblValorVenta
            // 
            lblValorVenta.AutoSize = true;
            lblValorVenta.Location = new Point(12, 225);
            lblValorVenta.Name = "lblValorVenta";
            lblValorVenta.Size = new Size(68, 15);
            lblValorVenta.TabIndex = 13;
            lblValorVenta.Text = "Precio Venta:";
            // 
            // txtValorVenta
            // 
            txtValorVenta.Location = new Point(136, 222);
            txtValorVenta.Name = "txtValorVenta";
            txtValorVenta.Size = new Size(100, 23);
            txtValorVenta.TabIndex = 14;
            txtValorVenta.KeyPress += txtNuevoCosto_KeyPress;
            // 
            // btnAplicar
            // 
            btnAplicar.Location = new Point(136, 260);
            btnAplicar.Name = "btnAplicar";
            btnAplicar.Size = new Size(75, 25);
            btnAplicar.TabIndex = 15;
            btnAplicar.Text = "Aplicar";
            btnAplicar.UseVisualStyleBackColor = true;
            btnAplicar.Click += btnAplicar_Click;
            // 
            // btnCerrar
            // 
            btnCerrar.Location = new Point(227, 260);
            btnCerrar.Name = "btnCerrar";
            btnCerrar.Size = new Size(80, 25);
            btnCerrar.TabIndex = 16;
            btnCerrar.Text = "Cerrar";
            btnCerrar.UseVisualStyleBackColor = true;
            btnCerrar.Click += btnCerrar_Click;
            // 
            // frmActualizarProducto
            // 
            ClientSize = new Size(400, 305);
            Controls.Add(btnCerrar);
            Controls.Add(btnAplicar);
            Controls.Add(txtValorVenta);
            Controls.Add(lblValorVenta);
            Controls.Add(txtIva);
            Controls.Add(lblIva);
            Controls.Add(txtNuevoPorcentaje);
            Controls.Add(lblNuevoPorcentaje);
            Controls.Add(txtNuevoCosto);
            Controls.Add(lblNuevoCosto);
            Controls.Add(txtStockActual);
            Controls.Add(lblStockActual);
            Controls.Add(txtNombre);
            Controls.Add(lblNombre);
            Controls.Add(btnBuscar);
            Controls.Add(txtCodigo);
            Controls.Add(lblCodigo);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Name = "frmActualizarProducto";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Modificar Producto";
            Load += frmActualizarProducto_Load;
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        private System.Windows.Forms.Label lblCodigo;
        private System.Windows.Forms.TextBox txtCodigo;
        private System.Windows.Forms.Button btnBuscar;
        private System.Windows.Forms.Label lblNombre;
        private System.Windows.Forms.TextBox txtNombre;
        private System.Windows.Forms.Label lblStockActual;
        private System.Windows.Forms.TextBox txtStockActual;
        private System.Windows.Forms.Label lblNuevoCosto;
        private System.Windows.Forms.TextBox txtNuevoCosto;
        private System.Windows.Forms.Label lblNuevoPorcentaje;
        private System.Windows.Forms.TextBox txtNuevoPorcentaje;
        private System.Windows.Forms.Label lblIva;
        private System.Windows.Forms.TextBox txtIva;
        private System.Windows.Forms.Label lblValorVenta;
        private System.Windows.Forms.TextBox txtValorVenta;
        private System.Windows.Forms.Button btnAplicar;
        private System.Windows.Forms.Button btnCerrar;
    }
}