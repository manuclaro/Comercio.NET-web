namespace Comercio.NET.Formularios
{
    partial class InformesForm
    {
        private System.Windows.Forms.Button btnReporteIVAxDia;
        private System.Windows.Forms.DataGridView dgvReporteIVAxDia;
        private System.Windows.Forms.Panel panelContenido;

        private void InitializeComponent()
        {
            btnReporteIVAxDia = new Button();
            dgvReporteIVAxDia = new DataGridView();
            panelContenido = new Panel();
            ((System.ComponentModel.ISupportInitialize)dgvReporteIVAxDia).BeginInit();
            panelContenido.SuspendLayout();
            SuspendLayout();
            // 
            // btnReporteIVAxDia
            // 
            btnReporteIVAxDia.Location = new Point(20, 70);
            btnReporteIVAxDia.Name = "btnReporteIVAxDia";
            btnReporteIVAxDia.Size = new Size(146, 28);
            btnReporteIVAxDia.TabIndex = 0;
            btnReporteIVAxDia.Text = "Reporte IVA x Día";
            btnReporteIVAxDia.UseVisualStyleBackColor = true;
            btnReporteIVAxDia.Click += btnReporteIVAxDia_Click;
            // 
            // dgvReporteIVAxDia
            // 
            dgvReporteIVAxDia.AllowUserToAddRows = false;
            dgvReporteIVAxDia.AllowUserToDeleteRows = false;
            dgvReporteIVAxDia.ReadOnly = true; 
            dgvReporteIVAxDia.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvReporteIVAxDia.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvReporteIVAxDia.Location = new Point(20, 110);
            dgvReporteIVAxDia.Name = "dgvReporteIVAxDia";
            dgvReporteIVAxDia.ScrollBars = ScrollBars.Vertical;
            dgvReporteIVAxDia.Size = new Size(760, 350); // <--- Tamaño ajustado para formulario 800x500
            dgvReporteIVAxDia.TabIndex = 1;
            // 
            // panelContenido
            // 
            panelContenido.BackColor = Color.White;
            panelContenido.Controls.Add(btnReporteIVAxDia);
            panelContenido.Controls.Add(dgvReporteIVAxDia);
            panelContenido.Dock = DockStyle.Fill;
            panelContenido.Location = new Point(0, 0);
            panelContenido.Name = "panelContenido";
            panelContenido.Padding = new Padding(20, 10, 20, 20);
            panelContenido.Size = new Size(800, 500);
            panelContenido.TabIndex = 0;
            // 
            // InformesForm
            // 
            ClientSize = new Size(800, 500);
            Controls.Add(panelContenido);
            Name = "InformesForm";
            Text = "Informes";
            ((System.ComponentModel.ISupportInitialize)dgvReporteIVAxDia).EndInit();
            panelContenido.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}