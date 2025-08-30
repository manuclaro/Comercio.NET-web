namespace Comercio.NET.Formularios
{
    partial class frmControlFacturas
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
            dgControlFacturas = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)dgControlFacturas).BeginInit();
            SuspendLayout();
            // 
            // dgControlFacturas
            // 
            dgControlFacturas.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgControlFacturas.Location = new Point(43, 107);
            dgControlFacturas.Name = "dgControlFacturas";
            dgControlFacturas.Size = new Size(711, 300);
            dgControlFacturas.TabIndex = 0;
            // 
            // frmControlFacturas
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(dgControlFacturas);
            Name = "frmControlFacturas";
            Text = "Control Facturas";
            ((System.ComponentModel.ISupportInitialize)dgControlFacturas).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView dgControlFacturas;
    }
}