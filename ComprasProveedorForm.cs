using System;
using System.Drawing;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    partial class ComprasProveedorForm
    {
        private System.ComponentModel.IContainer components = null;

        private TextBox txtNumero;
        private TextBox txtProveedor;
        private TextBox txtCuit;
        private DateTimePicker dtpFecha;
        private TextBox txtImporteNeto;
        private TextBox txtImporteIva;
        private TextBox txtImporteTotal;
        private DataGridView dgvIva;
        private Button btnAgregarIva;
        private Button btnEliminarIva;
        private Button btnGuardar;
        private Button btnCancelar;
        private TextBox txtAlicuota;
        private TextBox txtBase;

        /// <summary>
        /// Método requerido para el diseñador — no modificar con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.Text = "Registrar Compra Proveedor";
            this.ClientSize = new Size(760, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            var lblNumero = new Label { Text = "N° Factura", Left = 12, Top = 12, Width = 100 };
            txtNumero = new TextBox { Left = 120, Top = 10, Width = 220, Name = "txtNumero" };

            var lblProveedor = new Label { Text = "Proveedor", Left = 12, Top = 44, Width = 100 };
            txtProveedor = new TextBox { Left = 120, Top = 42, Width = 400, Name = "txtProveedor" };

            var lblCuit = new Label { Text = "CUIT", Left = 12, Top = 76, Width = 100 };
            txtCuit = new TextBox { Left = 120, Top = 74, Width = 150, Name = "txtCuit" };

            var lblFecha = new Label { Text = "Fecha", Left = 300, Top = 76, Width = 50 };
            dtpFecha = new DateTimePicker { Left = 360, Top = 72, Width = 160, Format = DateTimePickerFormat.Short, Name = "dtpFecha" };

            var pnlIva = new Panel { Left = 12, Top = 110, Width = 736, Height = 300, BorderStyle = BorderStyle.FixedSingle, Name = "pnlIva" };

            dgvIva = new DataGridView
            {
                Left = 8,
                Top = 8,
                Width = 716,
                Height = 220,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Name = "dgvIva",
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                RowTemplate = { Height = 24 }
            };

            var colAlicuota = new DataGridViewTextBoxColumn { Name = "Alicuota", HeaderText = "Alicuota %", Width = 100 };
            var colBase = new DataGridViewTextBoxColumn { Name = "Base", HeaderText = "Base Imponible", Width = 180 };
            var colIva = new DataGridViewTextBoxColumn { Name = "ImporteIva", HeaderText = "IVA $", Width = 160 };

            dgvIva.Columns.AddRange(new DataGridViewColumn[] { colAlicuota, colBase, colIva });

            var lblAlicuota = new Label { Text = "Alicuota %", Left = 8, Top = 236, Width = 70 };
            txtAlicuota = new TextBox { Left = 86, Top = 234, Width = 80, Name = "txtAlicuota" };
            var lblBase = new Label { Text = "Base", Left = 176, Top = 236, Width = 40 };
            txtBase = new TextBox { Left = 220, Top = 234, Width = 120, Name = "txtBase" };

            btnAgregarIva = new Button { Text = "Agregar alícuota", Left = 352, Top = 232, Width = 130, Name = "btnAgregarIva" };
            btnEliminarIva = new Button { Text = "Eliminar seleccionada", Left = 492, Top = 232, Width = 140, Name = "btnEliminarIva" };

            pnlIva.Controls.Add(dgvIva);
            pnlIva.Controls.Add(lblAlicuota);
            pnlIva.Controls.Add(txtAlicuota);
            pnlIva.Controls.Add(lblBase);
            pnlIva.Controls.Add(txtBase);
            pnlIva.Controls.Add(btnAgregarIva);
            pnlIva.Controls.Add(btnEliminarIva);

            var lblNeto = new Label { Text = "Importe Neto", Left = 12, Top = 428, Width = 100 };
            txtImporteNeto = new TextBox { Left = 120, Top = 424, Width = 140, ReadOnly = true, Name = "txtImporteNeto" };

            var lblIva = new Label { Text = "Importe IVA", Left = 280, Top = 428, Width = 100 };
            txtImporteIva = new TextBox { Left = 360, Top = 424, Width = 140, ReadOnly = true, Name = "txtImporteIva" };

            var lblTotal = new Label { Text = "Importe Total", Left = 520, Top = 428, Width = 100 };
            txtImporteTotal = new TextBox { Left = 620, Top = 424, Width = 128, ReadOnly = true, Name = "txtImporteTotal" };

            btnGuardar = new Button { Text = "Guardar", Left = 540, Top = 464, Width = 100, Name = "btnGuardar" };
            btnCancelar = new Button { Text = "Cancelar", Left = 660, Top = 464, Width = 100, Name = "btnCancelar" };

            // Agregar controles al formulario
            this.Controls.Add(lblNumero);
            this.Controls.Add(txtNumero);
            this.Controls.Add(lblProveedor);
            this.Controls.Add(txtProveedor);
            this.Controls.Add(lblCuit);
            this.Controls.Add(txtCuit);
            this.Controls.Add(lblFecha);
            this.Controls.Add(dtpFecha);
            this.Controls.Add(pnlIva);
            this.Controls.Add(lblNeto);
            this.Controls.Add(txtImporteNeto);
            this.Controls.Add(lblIva);
            this.Controls.Add(txtImporteIva);
            this.Controls.Add(lblTotal);
            this.Controls.Add(txtImporteTotal);
            this.Controls.Add(btnGuardar);
            this.Controls.Add(btnCancelar);

            // Eventos: los métodos ya existen en la lógica del formulario principal
            btnAgregarIva.Click += BtnAgregarIva_Click;
            btnEliminarIva.Click += BtnEliminarIva_Click;
            dgvIva.RowsRemoved += (s, e) => RecalcularTotales();
            btnGuardar.Click += async (s, e) => await BtnGuardar_ClickAsync();
            btnCancelar.Click += (s, e) => this.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}