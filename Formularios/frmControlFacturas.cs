using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;  // AGREGAR: Para List<string>
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;  // AGREGAR: Para NumberStyles y CultureInfo
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;  // NUEVO: Para métodos async
using Comercio.NET.Servicios;
using TicketConfig = Comercio.NET.Servicios.TicketConfig; // Alias específico

namespace Comercio.NET.Formularios
{
    public class frmControlFacturas : Form
    {
        private DataGridView dgvVentas;
        private DateTimePicker dtpDesde;
        private DateTimePicker dtpHasta;
        private Label lblDesde;
        private Label lblHasta;
        private Button btnSemana;
        private Button btnMes;
        private Button btnBuscar;
        private Button btnHoy;
        private Button btnAyer;
        private Label lblTotal;
        private Label lblCantidadVentas;
        private Label lblTitulo;
        private Panel panelFiltros;
        private Panel panelResumen;
        private Form frmDetalle; // Ventana flotante para el detalle
        private CheckBox chkCtaCte; // Checkbox para Cuenta Corriente
        private Panel panelTotales;
        private Label lblDetalleTiposFactura;
        private Label lblDetalleFormasPago;
        private TextBox txtFiltroCtaCte; // AGREGAR: TextBox para filtrar por nombre Cta Cte
        private Button btnAuditoriaEliminados; // Botón para Auditoría de Eliminados
        // AGREGAR: TextBox para filtrar por cajero
        private TextBox txtFiltroCajero; // TextBox para filtrar por número de cajero

        // NUEVO: ComboBox para filtrar por forma de pago
        private ComboBox cboFiltroFormaPago;
        private Label lblFiltroFormaPago;

        private ComboBox cboFiltroRubro;
        private Label lblFiltroRubro;

        private ComboBox cboFiltroProveedor;
        private Label lblFiltroProveedor;

        private CheckedListBox clbFiltroTipoFactura;
        private Label lblFiltroTipoFactura;
        private TextBox txtCheckedComboTipo;
        private Button btnCheckedComboTipo;
        private ToolStripDropDown dropDownTipo;

        private Button btnEstadisticasOfertas; // NUEVO: Botón para estadísticas de ofertas

        // AGREGAR: Clase auxiliar para los datos de la factura
        private class DatosFactura
        {
            public string TipoFactura { get; set; }
            public string FormaPago { get; set; }
            public string CAENumero { get; set; }
            public DateTime? CAEVencimiento { get; set; }
            public string CUITCliente { get; set; }
            public string NombreComercio { get; set; }
            public string DomicilioComercio { get; set; }
        }

        public frmControlFacturas()
        {
            InitializeComponent();
            ConfigurarFormulario();
            CrearVentanaDetalle();
            CargarVentasDelDia();
            
            // NUEVO: Cargar formas de pago después de cargar los datos iniciales
            CargarFormasDePago();
            
            // MAXIMIZAR EL FORMULARIO AL ABRIRSE
            this.WindowState = FormWindowState.Maximized;
        }

        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmControlFacturas));
            dgvVentas = new DataGridView();
            dtpDesde = new DateTimePicker();
            dtpHasta = new DateTimePicker();
            btnBuscar = new Button();
            btnHoy = new Button();
            btnAyer = new Button();
            btnSemana = new Button();
            btnMes = new Button();
            lblDesde = new Label();
            lblHasta = new Label();
            lblTotal = new Label();
            lblCantidadVentas = new Label();
            lblTitulo = new Label();
            panelFiltros = new Panel();
            txtFiltroCtaCte = new TextBox();
            chkCtaCte = new CheckBox();
            panelResumen = new Panel();
            panelTotales = new Panel();
            lblDetalleTiposFactura = new Label();
            lblDetalleFormasPago = new Label();
            btnAuditoriaEliminados = new Button();

            // NUEVO: Controles para filtro por forma de pago
            lblFiltroFormaPago = new Label();
            cboFiltroFormaPago = new ComboBox();

            // NUEVO: Agregar labels para IVA
            var lblTotalIVA = new Label();
            var lblSubtotalSinIVA = new Label();

            // txtFiltroCajero
            txtFiltroCajero = new TextBox();
            txtFiltroCajero.Font = new Font("Segoe UI", 10F);
            txtFiltroCajero.PlaceholderText = "Buscar cajero...";
            txtFiltroCajero.Size = new Size(160, 25);
            txtFiltroCajero.TabIndex = 6;
            txtFiltroCajero.TextChanged += TxtFiltroCajero_TextChanged;

            // Fecha Desde
            lblDesde = new Label();
            lblDesde.Text = "Desde:";
            lblDesde.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblDesde.ForeColor = Color.FromArgb(0, 120, 215);
            lblDesde.AutoSize = true;
            lblDesde.Location = new Point(12, 14);

            dtpDesde.Font = new Font("Segoe UI", 10F);
            dtpDesde.Format = DateTimePickerFormat.Short;
            dtpDesde.Size = new Size(110, 25);
            dtpDesde.Value = DateTime.Today;

            // Fecha Hasta
            lblHasta = new Label();
            lblHasta.Text = "Hasta:";
            lblHasta.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblHasta.ForeColor = Color.FromArgb(0, 120, 215);
            lblHasta.AutoSize = true;

            dtpHasta.Font = new Font("Segoe UI", 10F);
            dtpHasta.Format = DateTimePickerFormat.Short;
            dtpHasta.Size = new Size(110, 25);
            dtpHasta.Value = DateTime.Today;

            // Botones de búsqueda y rápidos
            btnBuscar.BackColor = Color.FromArgb(0, 120, 215);
            btnBuscar.FlatStyle = FlatStyle.Flat;
            btnBuscar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnBuscar.ForeColor = Color.White;
            btnBuscar.Size = new Size(80, 28);
            btnBuscar.Text = "Buscar";
            btnBuscar.UseVisualStyleBackColor = false;
            btnBuscar.Click += BtnBuscar_Click;

            btnHoy.BackColor = Color.FromArgb(0, 150, 136);
            btnHoy.FlatStyle = FlatStyle.Flat;
            btnHoy.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnHoy.ForeColor = Color.White;
            btnHoy.Size = new Size(60, 28);
            btnHoy.Text = "Hoy";
            btnHoy.UseVisualStyleBackColor = false;
            btnHoy.Click += BtnHoy_Click;

            // ✅ NUEVO: Botón Ayer
            btnAyer = new Button();
            btnAyer.BackColor = Color.FromArgb(156, 39, 176); // Color púrpura
            btnAyer.FlatStyle = FlatStyle.Flat;
            btnAyer.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnAyer.ForeColor = Color.White;
            btnAyer.Size = new Size(60, 28);
            btnAyer.Text = "Ayer";
            btnAyer.UseVisualStyleBackColor = false;
            btnAyer.Click += BtnAyer_Click;

            btnSemana.BackColor = Color.FromArgb(255, 193, 7);
            btnSemana.FlatStyle = FlatStyle.Flat;
            btnSemana.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSemana.ForeColor = Color.White;
            btnSemana.Size = new Size(80, 28);
            btnSemana.Text = "Semana";
            btnSemana.UseVisualStyleBackColor = false;
            btnSemana.Click += (s, e) =>
            {
                var today = DateTime.Today;
                // Asume semana comenzando lunes
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                dtpDesde.Value = today.AddDays(-diff);
                dtpHasta.Value = dtpDesde.Value.AddDays(6);
                CargarVentasPorFecha(dtpDesde.Value.Date, dtpHasta.Value.Date);
            };

            btnMes.BackColor = Color.FromArgb(0, 123, 255);
            btnMes.FlatStyle = FlatStyle.Flat;
            btnMes.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnMes.ForeColor = Color.White;
            btnMes.Size = new Size(60, 28);
            btnMes.Text = "Mes";
            btnMes.UseVisualStyleBackColor = false;
            btnMes.Click += (s, e) =>
            {
                var today = DateTime.Today;
                dtpDesde.Value = new DateTime(today.Year, today.Month, 1);
                dtpHasta.Value = dtpDesde.Value.AddMonths(1).AddDays(-1);
                CargarVentasPorFecha(dtpDesde.Value.Date, dtpHasta.Value.Date);
            };

            // lblFiltroFormaPago
            lblFiltroFormaPago.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblFiltroFormaPago.ForeColor = Color.FromArgb(0, 120, 215);
            lblFiltroFormaPago.Text = "Forma de Pago:";
            lblFiltroFormaPago.AutoSize = true;

            // cboFiltroFormaPago
            cboFiltroFormaPago.DropDownStyle = ComboBoxStyle.DropDownList;
            cboFiltroFormaPago.Font = new Font("Segoe UI", 10F);
            cboFiltroFormaPago.Size = new Size(160, 25);
            cboFiltroFormaPago.SelectedIndexChanged += CboFiltroFormaPago_SelectedIndexChanged;

            // NUEVO: Label y ComboBox para filtro por Rubro
            lblFiltroRubro = new Label();
            lblFiltroRubro.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblFiltroRubro.ForeColor = Color.FromArgb(0, 120, 215);
            lblFiltroRubro.Text = "Rubro:";
            lblFiltroRubro.AutoSize = true;

            cboFiltroRubro = new ComboBox();
            cboFiltroRubro.Name = "cboFiltroRubro";
            cboFiltroRubro.DropDownStyle = ComboBoxStyle.DropDownList;
            cboFiltroRubro.Font = new Font("Segoe UI", 10F);
            cboFiltroRubro.Size = new Size(150, 25);
            cboFiltroRubro.SelectedIndexChanged += CboFiltroRubro_SelectedIndexChanged;

            // NUEVO: Label y ComboBox para filtro por Proveedor
            lblFiltroProveedor = new Label();
            lblFiltroProveedor.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblFiltroProveedor.ForeColor = Color.FromArgb(0, 120, 215);
            lblFiltroProveedor.Text = "Proveedor:";
            lblFiltroProveedor.AutoSize = true;

            cboFiltroProveedor = new ComboBox();
            cboFiltroProveedor.Name = "cboFiltroProveedor";
            cboFiltroProveedor.DropDownStyle = ComboBoxStyle.DropDownList;
            cboFiltroProveedor.Font = new Font("Segoe UI", 10F);
            cboFiltroProveedor.Size = new Size(150, 25);
            cboFiltroProveedor.SelectedIndexChanged += CboFiltroProveedor_SelectedIndexChanged;

            // Label y CheckedListBox para filtro por Tipo de Factura (multi-selección)
            lblFiltroTipoFactura = new Label();
            lblFiltroTipoFactura.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblFiltroTipoFactura.ForeColor = Color.FromArgb(0, 120, 215);
            lblFiltroTipoFactura.Text = "Tipo Comprobante:";
            lblFiltroTipoFactura.AutoSize = true;

            // CheckedListBox que actúa como "combo" multi-select
            clbFiltroTipoFactura = new CheckedListBox();
            clbFiltroTipoFactura.CheckOnClick = true;
            clbFiltroTipoFactura.Font = new Font("Segoe UI", 10F);
            clbFiltroTipoFactura.ItemHeight = 18;
            clbFiltroTipoFactura.Size = new Size(100, clbFiltroTipoFactura.ItemHeight * 4 + 8);
            clbFiltroTipoFactura.FormattingEnabled = true;
            clbFiltroTipoFactura.ItemCheck += ClbFiltroTipoFactura_ItemCheck;

            // dtp layout: reubicamos todo dentro de panelFiltros en DOS filas
            panelFiltros.BackColor = Color.FromArgb(248, 249, 250);
            panelFiltros.Dock = DockStyle.Top;
            panelFiltros.Padding = new Padding(9);
            // Altura para dos filas
            panelFiltros.Height = 88;

            // Primera fila (fechas + botones rápidos + buscar + CtaCte en el medio + auditoría / IVA a la derecha)
            int x = 12;
            int y1 = 6;
            lblDesde.Location = new Point(x, y1);
            panelFiltros.Controls.Add(lblDesde);
            x += lblDesde.Width + 6;

            dtpDesde.Location = new Point(x, y1);
            panelFiltros.Controls.Add(dtpDesde);
            x += dtpDesde.Width + 8;

            lblHasta.Location = new Point(x, y1 + 2);
            panelFiltros.Controls.Add(lblHasta);
            x += lblHasta.Width + 6;

            dtpHasta.Location = new Point(x, y1);
            panelFiltros.Controls.Add(dtpHasta);
            x += dtpHasta.Width + 10;

            btnBuscar.Location = new Point(x, y1);
            panelFiltros.Controls.Add(btnBuscar);
            x += btnBuscar.Width + 6;

            btnHoy.Location = new Point(x, y1);
            panelFiltros.Controls.Add(btnHoy);
            x += btnHoy.Width + 6; // ✅ CAMBIO: Reducir espacio de 8 a 6

            // ✅ NUEVO: Agregar botón Ayer
            btnAyer.Location = new Point(x, y1);
            panelFiltros.Controls.Add(btnAyer);
            x += btnAyer.Width + 8;

            btnSemana.Location = new Point(x, y1);
            panelFiltros.Controls.Add(btnSemana);
            x += btnSemana.Width + 6;

            btnMes.Location = new Point(x, y1);
            panelFiltros.Controls.Add(btnMes);
            x += btnMes.Width + 20;

            // CheckBox Cta.Cte. en la primera fila (entre Mes y Auditoría)
            chkCtaCte.Location = new Point(x, y1);
            chkCtaCte.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            chkCtaCte.ForeColor = Color.FromArgb(0, 120, 215);
            chkCtaCte.Size = new Size(90, 28);
            chkCtaCte.Text = "Cta. Cte.";
            chkCtaCte.UseVisualStyleBackColor = true;
            chkCtaCte.CheckedChanged += ChkCtaCte_CheckedChanged;
            panelFiltros.Controls.Add(chkCtaCte);

            // Botones "Auditoría" y "IVA" más a la derecha y juntos
            var btnIvaTop = new Button();
            btnIvaTop.BackColor = Color.FromArgb(76, 175, 80);
            btnIvaTop.FlatStyle = FlatStyle.Flat;
            btnIvaTop.FlatAppearance.BorderSize = 0;
            btnIvaTop.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnIvaTop.ForeColor = Color.White;
            btnIvaTop.Size = new Size(50, 28);
            btnIvaTop.Text = "IVA";
            btnIvaTop.UseVisualStyleBackColor = false;
            btnIvaTop.Click += BtnResumenIva_Click;
            btnIvaTop.Location = new Point(panelFiltros.Width - 60, y1);
            btnIvaTop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panelFiltros.Controls.Add(btnIvaTop);

            // ✅ Botón de Estadísticas de Ofertas
            btnEstadisticasOfertas = new Button();
            btnEstadisticasOfertas.BackColor = Color.FromArgb(0, 150, 136);
            btnEstadisticasOfertas.FlatStyle = FlatStyle.Flat;
            btnEstadisticasOfertas.FlatAppearance.BorderSize = 0;
            btnEstadisticasOfertas.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnEstadisticasOfertas.ForeColor = Color.White;
            btnEstadisticasOfertas.Size = new Size(90, 28);
            btnEstadisticasOfertas.Text = "🎁 Ofertas";
            btnEstadisticasOfertas.UseVisualStyleBackColor = false;
            btnEstadisticasOfertas.Location = new Point(panelFiltros.Width - 158, y1);
            btnEstadisticasOfertas.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnEstadisticasOfertas.Click += (s, e) =>
            {
                try
                {
                    var formEstadisticas = new EstadisticasOfertasForm();
                    formEstadisticas.ShowDialog(this);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al abrir estadísticas de ofertas: {ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            panelFiltros.Controls.Add(btnEstadisticasOfertas);


            // ✅ NUEVO: Botón para exportar/imprimir listado
            var btnExportarListado = new Button();
            btnExportarListado.Name = "btnExportarListado";
            btnExportarListado.BackColor = Color.FromArgb(0, 150, 136); // Color verde azulado
            btnExportarListado.FlatStyle = FlatStyle.Flat;
            btnExportarListado.FlatAppearance.BorderSize = 0;
            btnExportarListado.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnExportarListado.ForeColor = Color.White;
            btnExportarListado.Size = new Size(110, 28);
            btnExportarListado.Text = "📋 Exportar";
            btnExportarListado.UseVisualStyleBackColor = false;
            btnExportarListado.Location = new Point(panelFiltros.Width - 398, y1); // ✅ A LA IZQUIERDA de Auditoría
            btnExportarListado.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExportarListado.Click += BtnExportarListado_Click;
            panelFiltros.Controls.Add(btnExportarListado);

            // ✅ Botón de Auditoría
            btnAuditoriaEliminados.BackColor = Color.FromArgb(255, 152, 0);
            btnAuditoriaEliminados.FlatStyle = FlatStyle.Flat;
            btnAuditoriaEliminados.FlatAppearance.BorderSize = 0;
            btnAuditoriaEliminados.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnAuditoriaEliminados.ForeColor = Color.White;
            btnAuditoriaEliminados.Size = new Size(110, 28);
            btnAuditoriaEliminados.Text = "🗑️ Auditoría";
            btnAuditoriaEliminados.UseVisualStyleBackColor = false;
            btnAuditoriaEliminados.Click += BtnAuditoriaEliminados_Click;
            btnAuditoriaEliminados.Location = new Point(panelFiltros.Width - 278, y1);
            btnAuditoriaEliminados.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panelFiltros.Controls.Add(btnAuditoriaEliminados);

            // Segunda fila (filtros: cajero, forma de pago, rubro, tipo + textbox CtaCte cuando está visible)
            int y2 = 46;

            txtFiltroCajero.Location = new Point(12, y2);
            panelFiltros.Controls.Add(txtFiltroCajero);

            // Label y combo "Forma de Pago"
            lblFiltroFormaPago.Location = new Point(190, y2 + 2);
            panelFiltros.Controls.Add(lblFiltroFormaPago);

            cboFiltroFormaPago.Size = new Size(130, 25);
            cboFiltroFormaPago.Location = new Point(lblFiltroFormaPago.Right + 8, y2 + 2);
            panelFiltros.Controls.Add(cboFiltroFormaPago);

            // Label y combo "Rubro"
            lblFiltroRubro.Location = new Point(cboFiltroFormaPago.Right + 12, y2 + 2);
            panelFiltros.Controls.Add(lblFiltroRubro);

            cboFiltroRubro.Location = new Point(lblFiltroRubro.Right + 8, y2 + 2);
            panelFiltros.Controls.Add(cboFiltroRubro);

            // Label y combo "Proveedor"
            lblFiltroProveedor.Location = new Point(cboFiltroRubro.Right + 12, y2 + 2);
            panelFiltros.Controls.Add(lblFiltroProveedor);

            cboFiltroProveedor.Location = new Point(lblFiltroProveedor.Right + 8, y2 + 2);
            panelFiltros.Controls.Add(cboFiltroProveedor);

            // Label para Tipo
            lblFiltroTipoFactura.Location = new Point(cboFiltroProveedor.Right + 12, y2 + 2);
            panelFiltros.Controls.Add(lblFiltroTipoFactura);

            // TextBox que actúa como "checked combo" (solo lectura)
            txtCheckedComboTipo = new TextBox();
            txtCheckedComboTipo.ReadOnly = true;
            txtCheckedComboTipo.Font = new Font("Segoe UI", 9F);
            txtCheckedComboTipo.Size = new Size(100, 25);
            txtCheckedComboTipo.Location = new Point(lblFiltroTipoFactura.Right + 8, y2 + 2);
            txtCheckedComboTipo.BackColor = Color.White;
            txtCheckedComboTipo.Cursor = Cursors.Hand;
            txtCheckedComboTipo.Click += (s, e) => {
                if (dropDownTipo == null) return;
                if (dropDownTipo.Visible) dropDownTipo.Close();
                else dropDownTipo.Show(txtCheckedComboTipo, new Point(0, txtCheckedComboTipo.Height));
            };
            panelFiltros.Controls.Add(txtCheckedComboTipo);

            // Botón pequeño para desplegar (flecha)
            btnCheckedComboTipo = new Button();
            btnCheckedComboTipo.Text = "▾";
            btnCheckedComboTipo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnCheckedComboTipo.Size = new Size(28, 25);
            btnCheckedComboTipo.Location = new Point(txtCheckedComboTipo.Right + 2, y2 + 2);
            btnCheckedComboTipo.FlatStyle = FlatStyle.Flat;
            btnCheckedComboTipo.FlatAppearance.BorderSize = 0;
            btnCheckedComboTipo.BackColor = Color.FromArgb(240, 240, 240);
            btnCheckedComboTipo.Click += (s, e) => {
                if (dropDownTipo == null) return;
                if (dropDownTipo.Visible) dropDownTipo.Close();
                else dropDownTipo.Show(txtCheckedComboTipo, new Point(0, txtCheckedComboTipo.Height));
            };
            panelFiltros.Controls.Add(btnCheckedComboTipo);

            clbFiltroTipoFactura.Size = new Size(txtCheckedComboTipo.Width, clbFiltroTipoFactura.ItemHeight * 4 + 8);

            // TextBox para filtrar por nombre CtaCte - SE POSICIONA ENCIMA DEL LABEL "Tipo Comprobante"
            // Inicialmente oculto
            txtFiltroCtaCte.Location = new Point(lblFiltroTipoFactura.Left, y2 + 2);
            txtFiltroCtaCte.Size = new Size(140, 25);
            txtFiltroCtaCte.PlaceholderText = "Buscar cliente...";
            txtFiltroCtaCte.Visible = false; // OCULTO POR DEFECTO
            txtFiltroCtaCte.TextChanged += TxtFiltroCtaCte_TextChanged;
            panelFiltros.Controls.Add(txtFiltroCtaCte);
            txtFiltroCtaCte.BringToFront(); // Traer al frente para que esté sobre otros controles

            // Crear el host y el dropdown
            var host = new ToolStripControlHost(clbFiltroTipoFactura) { AutoSize = false, Margin = Padding.Empty, Padding = Padding.Empty };
            host.Size = clbFiltroTipoFactura.Size;
            dropDownTipo = new ToolStripDropDown { AutoClose = true, Padding = Padding.Empty };
            dropDownTipo.Items.Add(host);

            txtCheckedComboTipo.Text = "Todos";

            // Setup dgvVentas
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(248, 249, 250);
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = Color.Black;
            dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(248, 249, 250);
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dgvVentas.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvVentas.ColumnHeadersHeight = 35;
            dgvVentas.Dock = DockStyle.Fill;
            dgvVentas.EnableHeadersVisualStyles = false;
            dgvVentas.ReadOnly = true;
            dgvVentas.RowHeadersVisible = false;
            dgvVentas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvVentas.Size = new Size(909, 311);
            dgvVentas.CellDoubleClick += DgvVentas_CellDoubleClick;
            dgvVentas.Click += FrmControlFacturas_Click;
            //dgvVentas.SelectionChanged += DgvVentas_SelectionChanged;
            dgvVentas.AllowUserToAddRows = false;
            dgvVentas.AllowUserToDeleteRows = false;
            dgvVentas.AllowUserToResizeRows = false;

            // Resto de la configuración del panel resumen y totales
            lblTotal.Dock = DockStyle.Top;
            lblTotal.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTotal.ForeColor = Color.White;
            lblTotal.Size = new Size(909, 23);
            lblTotal.TabIndex = 2;
            lblTotal.Text = "Total: $0,00";
            lblTotal.TextAlign = ContentAlignment.MiddleRight;

            lblTotalIVA.Name = "lblTotalIVA";
            lblTotalIVA.Dock = DockStyle.Top;
            lblTotalIVA.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblTotalIVA.ForeColor = Color.FromArgb(255, 182, 193);
            lblTotalIVA.Size = new Size(909, 18);
            lblTotalIVA.TabIndex = 3;
            lblTotalIVA.Text = "IVA Total: $0,00";
            lblTotalIVA.TextAlign = ContentAlignment.MiddleRight;

            lblSubtotalSinIVA.Name = "lblSubtotalSinIVA";
            lblSubtotalSinIVA.Dock = DockStyle.Top;
            lblSubtotalSinIVA.Font = new Font("Segoe UI", 10F);
            lblSubtotalSinIVA.ForeColor = Color.FromArgb(200, 200, 200);
            lblSubtotalSinIVA.Size = new Size(909, 16);
            lblSubtotalSinIVA.TabIndex = 4;
            lblSubtotalSinIVA.Text = "Subtotal sin IVA: $0,00";
            lblSubtotalSinIVA.TextAlign = ContentAlignment.MiddleRight;

            lblCantidadVentas.Dock = DockStyle.Left;
            lblCantidadVentas.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblCantidadVentas.ForeColor = Color.White;
            lblCantidadVentas.Size = new Size(175, 95);
            lblCantidadVentas.TabIndex = 0;
            lblCantidadVentas.Text = "Ventas: 0";
            lblCantidadVentas.TextAlign = ContentAlignment.MiddleLeft;

            lblTitulo.Dock = DockStyle.Top;
            lblTitulo.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitulo.ForeColor = Color.FromArgb(0, 120, 215);
            lblTitulo.Size = new Size(909, 47);
            lblTitulo.TabIndex = 3;
            lblTitulo.Text = "Control de Facturas - Ventas del Día";
            lblTitulo.TextAlign = ContentAlignment.MiddleCenter;

            // panelResumen y panelTotales
            panelResumen.BackColor = Color.FromArgb(0, 120, 215);
            panelResumen.Controls.Add(lblCantidadVentas);
            panelResumen.Controls.Add(panelTotales);
            panelResumen.Dock = DockStyle.Bottom;
            panelResumen.Size = new Size(909, 95);
            panelResumen.Click += FrmControlFacturas_Click;

            panelTotales.BackColor = Color.FromArgb(0, 120, 215);
            panelTotales.Controls.Add(lblSubtotalSinIVA);
            panelTotales.Controls.Add(lblTotalIVA);
            panelTotales.Controls.Add(lblDetalleTiposFactura);
            panelTotales.Controls.Add(lblDetalleFormasPago);
            panelTotales.Controls.Add(lblTotal);
            panelTotales.Dock = DockStyle.Fill;
            panelTotales.Size = new Size(909, 95);

            lblDetalleTiposFactura.Dock = DockStyle.Top;
            lblDetalleTiposFactura.Font = new Font("Segoe UI", 9F);
            lblDetalleTiposFactura.ForeColor = Color.White;
            lblDetalleTiposFactura.Size = new Size(909, 19);
            lblDetalleTiposFactura.TextAlign = ContentAlignment.MiddleRight;

            lblDetalleFormasPago.Dock = DockStyle.Top;
            lblDetalleFormasPago.Font = new Font("Segoe UI", 9F);
            lblDetalleFormasPago.ForeColor = Color.White;
            lblDetalleFormasPago.Size = new Size(909, 18);
            lblDetalleFormasPago.TextAlign = ContentAlignment.MiddleRight;

            // Añadir controles al formulario
            Controls.Add(dgvVentas);
            Controls.Add(panelResumen);
            Controls.Add(panelFiltros);
            Controls.Add(lblTitulo);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(925, 550);
            Name = "frmControlFacturas";
            Text = "Control de Facturas";

            ((System.ComponentModel.ISupportInitialize)dgvVentas).EndInit();
            panelFiltros.ResumeLayout(false);
            panelFiltros.PerformLayout();
            panelResumen.ResumeLayout(false);
            panelTotales.ResumeLayout(false);
            ResumeLayout(false);
        }

        // ✅ NUEVO: Event handler para el botón Ayer
        private void BtnAyer_Click(object sender, EventArgs e)
        {
            DateTime ayer = DateTime.Today.AddDays(-1);
            dtpDesde.Value = ayer;
            dtpHasta.Value = ayer;
            CargarVentasPorFecha(ayer, ayer);
        }

        private void ConfigurarFormulario()
        {
            // Configuración inicial del formulario
            this.dgvVentas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvVentas.MultiSelect = false;
            this.dgvVentas.ReadOnly = true;
            this.dgvVentas.AutoGenerateColumns = true; // CAMBIAR: permitir auto-generación

            // Cargar los eventos de las columnas
            CargarEventosColumnas();

            // Establecer el rango de fechas por defecto
            dtpDesde.Value = DateTime.Today;
            dtpHasta.Value = DateTime.Today;
        }

        // NUEVO: Método para cargar proveedores en el ComboBox
        private void CargarProveedores()
        {
            try
            {
                cboFiltroProveedor.Items.Clear();
                cboFiltroProveedor.Items.Add("Todos los proveedores"); // Opción para mostrar todos

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    // Obtener proveedores únicos desde la tabla Productos
                    // que tienen ventas registradas
                    var query = @"
                SELECT DISTINCT p.proveedor 
                FROM Productos p
                INNER JOIN Ventas v ON p.codigo = v.codigo
                WHERE p.proveedor IS NOT NULL 
                  AND RTRIM(LTRIM(p.proveedor)) <> '' 
                ORDER BY p.proveedor";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string proveedor = reader["proveedor"]?.ToString()?.Trim();
                                if (!string.IsNullOrEmpty(proveedor))
                                {
                                    cboFiltroProveedor.Items.Add(proveedor);
                                }
                            }
                        }
                    }
                }

                // Seleccionar "Todos los proveedores" por defecto
                cboFiltroProveedor.SelectedIndex = 0;

                System.Diagnostics.Debug.WriteLine($"CargarProveedores: Se cargaron {cboFiltroProveedor.Items.Count - 1} proveedores disponibles");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando proveedores: {ex.Message}");
                cboFiltroProveedor.Items.Clear();
                cboFiltroProveedor.Items.Add("Todos los proveedores");
                cboFiltroProveedor.SelectedIndex = 0;
            }
        }

        // NUEVO: Event handler para cambio en el ComboBox de proveedor
        private void CboFiltroProveedor_SelectedIndexChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        // Cargar tipos de factura en el CheckedListBox
        private void CargarTiposFactura()
        {
            try
            {
                clbFiltroTipoFactura.Items.Clear();

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"SELECT DISTINCT TipoFactura 
                  FROM Facturas
                  WHERE TipoFactura IS NOT NULL
                    AND RTRIM(LTRIM(TipoFactura)) <> ''
                    AND RTRIM(LTRIM(TipoFactura)) <> 'DNI'
                  ORDER BY TipoFactura";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string tipo = reader["TipoFactura"]?.ToString()?.Trim();
                                // ✅ Filtro adicional en código para mayor seguridad
                                if (!string.IsNullOrEmpty(tipo) && !tipo.Equals("DNI", StringComparison.OrdinalIgnoreCase))
                                {
                                    clbFiltroTipoFactura.Items.Add(tipo, false);
                                }
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"CargarTiposFactura: se cargaron {clbFiltroTipoFactura.Items.Count} tipos");
                UpdateCheckedComboTexto();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando tipos de factura: {ex.Message}");
                clbFiltroTipoFactura.Items.Clear();
                UpdateCheckedComboTexto();
            }
        }


        private void UpdateCheckedComboTexto()
        {
            try
            {
                if (clbFiltroTipoFactura == null || clbFiltroTipoFactura.Items.Count == 0)
                {
                    txtCheckedComboTipo.Text = "Sin tipos";
                    return;
                }

                var checkedItems = clbFiltroTipoFactura.CheckedItems.Cast<object>().Select(i => i.ToString()).ToList();
                if (checkedItems.Count == 0)
                {
                    txtCheckedComboTipo.Text = "Todos los tipos";
                }
                else if (checkedItems.Count <= 3)
                {
                    txtCheckedComboTipo.Text = string.Join(", ", checkedItems);
                }
                else
                {
                    txtCheckedComboTipo.Text = $"{checkedItems.Count} seleccionados";
                }
            }
            catch
            {
                txtCheckedComboTipo.Text = "Todos los tipos";
            }
        }

        private void ClbFiltroTipoFactura_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // Se ejecuta antes de cambiar el estado; usar BeginInvoke para leer el estado actualizado
            this.BeginInvoke((MethodInvoker)(() =>
            {
                UpdateCheckedComboTexto();
                AplicarFiltros();
            }));
        }

        // AGREGAR: Método para cargar eventos de columnas
        private void CargarEventosColumnas()
        {
            // Aquí puedes agregar eventos específicos para las columnas si es necesario
            // Por ahora está vacío pero se necesita para evitar errores
        }

        // NUEVO: Método para cargar formas de pago en el ComboBox
        private void CargarFormasDePago()
        {
            try
            {
                cboFiltroFormaPago.Items.Clear();
                cboFiltroFormaPago.Items.Add("Todas las formas"); // Opción para mostrar todas

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    // CORREGIDO: Obtener TODAS las formas de pago disponibles en la base de datos
                    var query = @"SELECT DISTINCT FormadePago 
                                 FROM Facturas 
                                 WHERE FormadePago IS NOT NULL 
                                 AND RTRIM(LTRIM(FormadePago)) <> '' 
                                 ORDER BY FormadePago";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string formaPago = reader["FormadePago"]?.ToString()?.Trim();
                                if (!string.IsNullOrEmpty(formaPago))
                                {
                                    cboFiltroFormaPago.Items.Add(formaPago);
                                }
                            }
                        }
                    }
                }

                // Seleccionar "Todas las formas" por defecto
                cboFiltroFormaPago.SelectedIndex = 0;
                
                // Debug: mostrar cuántas formas de pago se cargaron
                System.Diagnostics.Debug.WriteLine($"CargarFormasDePago: Se cargaron {cboFiltroFormaPago.Items.Count - 1} formas de pago disponibles en total");
                
                // Debug adicional: mostrar las formas de pago cargadas
                for (int i = 1; i < cboFiltroFormaPago.Items.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Forma de pago {i}: '{cboFiltroFormaPago.Items[i]}'");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando formas de pago: {ex.Message}");
                // En caso de error, solo mantener la opción por defecto
                cboFiltroFormaPago.Items.Clear();
                cboFiltroFormaPago.Items.Add("Todas las formas");
                cboFiltroFormaPago.SelectedIndex = 0;
            }
        }

        // NUEVO: Método para cargar rubros en el ComboBox
        private void CargarRubros()
        {
            try
            {
                cboFiltroRubro.Items.Clear();
                cboFiltroRubro.Items.Add("Todos los rubros"); // Opción para mostrar todos

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    // Obtener rubros únicos desde la tabla Productos
                    // ya que los productos de las ventas están relacionados con sus rubros
                    var query = @"
                SELECT DISTINCT p.rubro 
                FROM Productos p
                INNER JOIN Ventas v ON p.codigo = v.codigo
                WHERE p.rubro IS NOT NULL 
                  AND RTRIM(LTRIM(p.rubro)) <> '' 
                ORDER BY p.rubro";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string rubro = reader["rubro"]?.ToString()?.Trim();
                                if (!string.IsNullOrEmpty(rubro))
                                {
                                    cboFiltroRubro.Items.Add(rubro);
                                }
                            }
                        }
                    }
                }

                // Seleccionar "Todos los rubros" por defecto
                cboFiltroRubro.SelectedIndex = 0;

                System.Diagnostics.Debug.WriteLine($"CargarRubros: Se cargaron {cboFiltroRubro.Items.Count - 1} rubros disponibles");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando rubros: {ex.Message}");
                cboFiltroRubro.Items.Clear();
                cboFiltroRubro.Items.Add("Todos los rubros");
                cboFiltroRubro.SelectedIndex = 0;
            }
        }

        // NUEVO: Event handler para cambio en el ComboBox de forma de pago
        private void CboFiltroFormaPago_SelectedIndexChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        // NUEVO: Event handler para cambio en el ComboBox de rubro
        private void CboFiltroRubro_SelectedIndexChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        private void CrearVentanaDetalle()
        {
            // Crear la ventana flotante para el detalle de la factura (MÁS PEQUEÑA)
            frmDetalle = new Form();
            frmDetalle.Text = "Detalle de Factura";
            frmDetalle.Size = new Size(600, 400);
            frmDetalle.StartPosition = FormStartPosition.Manual;
            frmDetalle.FormBorderStyle = FormBorderStyle.FixedDialog;
            frmDetalle.MaximizeBox = false;
            frmDetalle.MinimizeBox = false;
            frmDetalle.BackColor = Color.White;

            // DataGridView para mostrar los detalles
            var dgvDetalle = new DataGridView();
            dgvDetalle.Name = "dgvDetalle";
            dgvDetalle.Dock = DockStyle.Fill;
            dgvDetalle.AllowUserToAddRows = false;
            dgvDetalle.AllowUserToDeleteRows = false;
            dgvDetalle.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDetalle.BackgroundColor = Color.White;
            dgvDetalle.BorderStyle = BorderStyle.None;
            dgvDetalle.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvDetalle.ReadOnly = true;
            dgvDetalle.RowHeadersVisible = false;
            dgvDetalle.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDetalle.EnableHeadersVisualStyles = false;
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(248, 249, 250);
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = Color.Black;
            dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(248, 249, 250);
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True; // ✅ Mantener wrap habilitado
            dataGridViewCellStyle1.Padding = new Padding(3, 2, 3, 2); // ✅ NUEVO: Agregar padding
    
            dgvDetalle.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvDetalle.ColumnHeadersHeight = 55; // ✅ AUMENTADO de 50 a 55 píxeles
            dgvDetalle.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing; // ✅ CAMBIO: Habilitar resizing

            frmDetalle.Controls.Add(dgvDetalle);

            // Panel para mostrar totales y formas de pago (altura aumentada)
            var panelTotales = new Panel();
            panelTotales.Dock = DockStyle.Bottom;
            panelTotales.Height = 90; // un poco más alto para totales + formas de pago
            panelTotales.BackColor = Color.FromArgb(0, 120, 215);
            frmDetalle.Controls.Add(panelTotales);

            // REORGANIZACIÓN: primero los labels de Totales (arriba)...
            var lblCantidadProductos = new Label();
            lblCantidadProductos.Name = "lblCantidadProductos";
            lblCantidadProductos.Text = "Productos: 0";
            lblCantidadProductos.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblCantidadProductos.ForeColor = Color.White;
            lblCantidadProductos.AutoSize = false;
            lblCantidadProductos.Dock = DockStyle.Left;
            lblCantidadProductos.Width = 140;
            lblCantidadProductos.TextAlign = ContentAlignment.MiddleLeft;
            lblCantidadProductos.Padding = new Padding(10, 0, 0, 0);
            panelTotales.Controls.Add(lblCantidadProductos);

            var lblTotalFactura = new Label();
            lblTotalFactura.Name = "lblTotalFactura";
            lblTotalFactura.Text = "Total: $0,00";
            lblTotalFactura.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTotalFactura.ForeColor = Color.White;
            lblTotalFactura.AutoSize = false;
            lblTotalFactura.Dock = DockStyle.Right;
            lblTotalFactura.Width = 220;
            lblTotalFactura.TextAlign = ContentAlignment.MiddleRight;
            lblTotalFactura.Padding = new Padding(0, 0, 12, 0);
            panelTotales.Controls.Add(lblTotalFactura);

            var lblCantidadTotalDetalle = new Label();
            lblCantidadTotalDetalle.Name = "lblCantidadTotalDetalle";
            lblCantidadTotalDetalle.Text = "Cantidad: 0";
            lblCantidadTotalDetalle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblCantidadTotalDetalle.ForeColor = Color.White;
            lblCantidadTotalDetalle.AutoSize = false;
            lblCantidadTotalDetalle.Dock = DockStyle.Fill;
            lblCantidadTotalDetalle.TextAlign = ContentAlignment.MiddleCenter;
            panelTotales.Controls.Add(lblCantidadTotalDetalle);

            // ...y después el label con el desglose por formas de pago, anclado abajo
            var lblDetalleFormasPagoDetalle = new Label();
            lblDetalleFormasPagoDetalle.Name = "lblDetalleFormasPagoDetalle";
            lblDetalleFormasPagoDetalle.Text = ""; // se llenará dinámicamente
            lblDetalleFormasPagoDetalle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblDetalleFormasPagoDetalle.ForeColor = Color.White;
            lblDetalleFormasPagoDetalle.AutoSize = false;
            lblDetalleFormasPagoDetalle.Dock = DockStyle.Bottom;
            lblDetalleFormasPagoDetalle.Height = 24;
            lblDetalleFormasPagoDetalle.TextAlign = ContentAlignment.MiddleCenter;
            lblDetalleFormasPagoDetalle.Padding = new Padding(8, 2, 8, 2);
            lblDetalleFormasPagoDetalle.AutoEllipsis = true;
            // Ajustar el ancho máximo al redimensionar el panel para permitir wrap adecuado
            panelTotales.Resize += (s, e) =>
            {
                try
                {
                    lblDetalleFormasPagoDetalle.MaximumSize = new Size(panelTotales.ClientSize.Width - 20, 0);
                }
                catch { }
            };
            panelTotales.Controls.Add(lblDetalleFormasPagoDetalle);

            // Panel inferior para botones
            var panelBotones = new Panel();
            panelBotones.Dock = DockStyle.Bottom;
            panelBotones.Height = 50;
            panelBotones.BackColor = Color.FromArgb(248, 249, 250);
            frmDetalle.Controls.Add(panelBotones);

            var btnImprimir = new Button();
            btnImprimir.Name = "btnImprimir";
            btnImprimir.Text = "Imprimir";
            btnImprimir.BackColor = Color.FromArgb(0, 120, 215);
            btnImprimir.ForeColor = Color.White;
            btnImprimir.FlatStyle = FlatStyle.Flat;
            btnImprimir.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnImprimir.Size = new Size(80, 30);
            btnImprimir.Location = new Point(panelBotones.Width - 180, 10);
            btnImprimir.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnImprimir.Click += BtnImprimir_Click;
            panelBotones.Controls.Add(btnImprimir);

            var btnCerrar = new Button();
            btnCerrar.Text = "Cerrar";
            btnCerrar.BackColor = Color.FromArgb(0, 150, 136);
            btnCerrar.ForeColor = Color.White;
            btnCerrar.FlatStyle = FlatStyle.Flat;
            btnCerrar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCerrar.Size = new Size(80, 30);
            btnCerrar.Location = new Point(panelBotones.Width - 90, 10);
            btnCerrar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCerrar.Click += (s, e) => { frmDetalle.Hide(); };
            panelBotones.Controls.Add(btnCerrar);

            frmDetalle.FormClosing += (s, e) => {
                e.Cancel = true;
                frmDetalle.Hide();
            };

            panelBotones.Resize += (s, e) => {
                btnCerrar.Location = new Point(panelBotones.Width - 90, 10);
                btnImprimir.Location = new Point(panelBotones.Width - 180, 10);
            };
        }
        private void CargarVentasDelDia()
        {
            // Por defecto ambas fechas hoy
            dtpDesde.Value = DateTime.Today;
            dtpHasta.Value = DateTime.Today;
            CargarVentasPorFecha(DateTime.Today, DateTime.Today);
        }

        
        // CORREGIR: Método de carga con conversión de datos numéricos
        private void CargarVentasPorFecha(DateTime desde, DateTime hasta)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    // ✅ MODIFICADO: Remover columnas CUITCliente y Rubro
                    var query = chkCtaCte.Checked
                       ? @"SELECT 
                            f.NumeroRemito as 'Remito',
                            f.NroFactura as 'N° Factura',
                            CAST(ISNULL(f.ImporteFinal, 0) AS DECIMAL(18,2)) as 'Importe Final',
                            CAST(ISNULL(f.PorcentajeDescuento, 0) AS DECIMAL(5,2)) as '% Descuento',
                            CAST(ISNULL(f.ImporteDescuento, 0) AS DECIMAL(18,2)) as 'Descuento',
                            CAST(ISNULL(f.IVA, 0) AS DECIMAL(18,2)) as 'IVA',
                            CAST(ISNULL(f.ImporteFinal, 0) - ISNULL(f.IVA, 0) AS DECIMAL(18,2)) as 'Subtotal',
                            ISNULL(f.Cajero, '') as 'Cajero',
                            f.Fecha as 'Fecha',
                            f.Hora as 'Hora',
                            ISNULL(f.FormadePago, 'No especificado') as 'Forma de Pago',
                            ISNULL(f.TipoFactura, 'No especificado') as 'Tipo',
                            f.CAENumero as 'CAE',
                            f.CtaCteNombre as 'Cta. Cte. Nombre',
                            (SELECT TOP 1 p.proveedor 
                             FROM Ventas v 
                             INNER JOIN Productos p ON v.codigo = p.codigo 
                             WHERE v.NroFactura = f.NumeroRemito) as 'Proveedor'
                        FROM Facturas f
                        WHERE CAST(f.Fecha AS DATE) BETWEEN @desde AND @hasta
                        AND f.esCtaCte = @esCtaCte
                        ORDER BY f.NumeroRemito DESC"
                            : @"SELECT 
                            f.NumeroRemito as 'Remito',
                            f.NroFactura as 'N° Factura',
                            CAST(ISNULL(f.ImporteFinal, 0) AS DECIMAL(18,2)) as 'Importe Final',
                            CAST(ISNULL(f.PorcentajeDescuento, 0) AS DECIMAL(5,2)) as '% Descuento',
                            CAST(ISNULL(f.ImporteDescuento, 0) AS DECIMAL(18,2)) as 'Descuento',
                            CAST(ISNULL(f.IVA, 0) AS DECIMAL(18,2)) as 'IVA',
                            CAST(ISNULL(f.ImporteFinal, 0) - ISNULL(f.IVA, 0) AS DECIMAL(18,2)) as 'Subtotal',
                            ISNULL(f.Cajero, '') as 'Cajero',
                            f.Fecha as 'Fecha',
                            f.Hora as 'Hora',
                            ISNULL(f.FormadePago, 'No especificado') as 'Forma de Pago',
                            ISNULL(f.TipoFactura, 'No especificado') as 'Tipo',
                            f.CAENumero as 'CAE',
                            (SELECT TOP 1 p.proveedor 
                             FROM Ventas v 
                             INNER JOIN Productos p ON v.codigo = p.codigo 
                             WHERE v.NroFactura = f.NumeroRemito) as 'Proveedor'
                        FROM Facturas f
                        WHERE CAST(f.Fecha AS DATE) BETWEEN @desde AND @hasta
                        AND f.esCtaCte = @esCtaCte
                        ORDER BY f.NumeroRemito DESC";

                    using (var adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@desde", desde.Date);
                        adapter.SelectCommand.Parameters.AddWithValue("@hasta", hasta.Date);
                        adapter.SelectCommand.Parameters.AddWithValue("@esCtaCte", chkCtaCte.Checked);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        // Eliminar filas completamente vacías
                        for (int i = dt.Rows.Count - 1; i >= 0; i--)
                        {
                            var row = dt.Rows[i];
                            bool empty = true;
                            foreach (var cell in row.ItemArray)
                            {
                                if (cell != DBNull.Value && !string.IsNullOrWhiteSpace(cell?.ToString()))
                                {
                                    empty = false;
                                    break;
                                }
                            }
                            if (empty)
                                dt.Rows.RemoveAt(i);
                        }

                        dgvVentas.DataSource = dt;
                        FormatearColumnas();

                        CargarFormasDePago();
                        CargarTiposFactura();
                        CargarRubros();
                        CargarProveedores(); // ✅ NUEVO

                        if (!string.IsNullOrEmpty(txtFiltroCajero.Text) ||
                            (chkCtaCte.Checked && !string.IsNullOrEmpty(txtFiltroCtaCte.Text)) ||
                            (cboFiltroFormaPago.SelectedItem != null && cboFiltroFormaPago.SelectedItem.ToString() != "Todas las formas"))
                        {
                            AplicarFiltros();
                        }
                        else
                        {
                            ActualizarResumen(dt);
                            ActualizarTituloConFiltros(0, dt.Rows.Count, dt.Rows.Count);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las ventas: {ex.Message}\n\nDetalles: {ex.ToString()}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MODIFICAR: Event handler para el checkbox
        private void ChkCtaCte_CheckedChanged(object sender, EventArgs e)
        {
            // Mostrar/ocultar el textbox según el estado del checkbox
            txtFiltroCtaCte.Visible = chkCtaCte.Checked;

            if (chkCtaCte.Checked)
            {
                txtFiltroCtaCte.Text = "";
                txtFiltroCtaCte.Focus();
            }
            else
            {
                // Al destildar, ocultar el textbox y limpiar el texto
                txtFiltroCtaCte.Text = "";
            }

            // Recargar con el rango seleccionado
            CargarVentasPorFecha(dtpDesde.Value.Date, dtpHasta.Value.Date);
        }       

        // AGREGAR: Event handler para el TextBox de filtro
        private void TxtFiltroCtaCte_TextChanged(object sender, EventArgs e)
        {
            if (chkCtaCte.Checked)
            {
                AplicarFiltros();
            }
        }

        // AGREGAR: Event handler para el filtro por cajero
        private void TxtFiltroCajero_TextChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        // AGREGAR: Método unificado para aplicar filtros
        private void AplicarFiltros()
        {
            if (dgvVentas.DataSource is DataTable dt)
            {
                var filtros = new List<string>();
                
                // Filtro por cajero
                string filtroCajero = txtFiltroCajero.Text.Trim();
                if (!string.IsNullOrEmpty(filtroCajero))
                {
                    filtros.Add($"[Cajero] LIKE '%{filtroCajero.Replace("'", "''")}%'");
                }
                
                // Filtro por cuenta corriente (solo si está habilitado)
                if (chkCtaCte.Checked)
                {
                    string filtroCtaCte = txtFiltroCtaCte.Text.Trim();
                    if (!string.IsNullOrEmpty(filtroCtaCte))
                    {
                        filtros.Add($"[Cta. Cte. Nombre] LIKE '%{filtroCtaCte.Replace("'", "''")}%'");
                    }
                }

                // NUEVO: Filtro por forma de pago
                if (cboFiltroFormaPago.SelectedItem != null)
                {
                    string formaPagoSeleccionada = cboFiltroFormaPago.SelectedItem.ToString();
                    if (formaPagoSeleccionada != "Todas las formas")
                    {
                        filtros.Add($"[Forma de Pago] = '{formaPagoSeleccionada.Replace("'", "''")}'");
                    }
                }

                // NUEVO: Filtro por rubro
                if (cboFiltroRubro.SelectedItem != null)
                {
                    string rubroSeleccionado = cboFiltroRubro.SelectedItem.ToString();
                    if (rubroSeleccionado != "Todos los rubros")
                    {
                        filtros.Add($"[Rubro] = '{rubroSeleccionado.Replace("'", "''")}'");
                    }
                }

                // NUEVO: Filtro por proveedor
                if (cboFiltroProveedor.SelectedItem != null)
                {
                    string proveedorSeleccionado = cboFiltroProveedor.SelectedItem.ToString();
                    if (proveedorSeleccionado != "Todos los proveedores")
                    {
                        // Necesitamos filtrar por productos que pertenezcan al proveedor seleccionado
                        // Esto requiere verificar si algún producto de la venta pertenece al proveedor
                        filtros.Add($"[Proveedor] = '{proveedorSeleccionado.Replace("'", "''")}'");
                    }
                }
                var query = chkCtaCte.Checked;

                // Filtro por tipo de factura (multi-selección)
                if (clbFiltroTipoFactura != null && clbFiltroTipoFactura.CheckedItems.Count > 0)
                {
                    var tipos = new List<string>();
                    foreach (var item in clbFiltroTipoFactura.CheckedItems)
                        tipos.Add(item.ToString().Replace("'", "''"));

                    if (tipos.Count > 0)
                    {
                        // Usamos IN si el DataView lo soporta; si no, se puede reemplazar por ORs
                        filtros.Add($"[Tipo] IN ('{string.Join("','", tipos)}')");
                    }
                }
                
                // Aplicar filtros combinados
                string filtroCompleto = filtros.Count > 0 ? string.Join(" AND ", filtros) : "";
                dt.DefaultView.RowFilter = filtroCompleto;
                
                // Actualizar el resumen con los datos filtrados
                ActualizarResumenFiltrado();
                
                // Si hay coincidencia única, seleccionar la fila
                if (dt.DefaultView.Count == 1 && dgvVentas.Rows.Count > 0)
                {
                    try
                    {
                        var dataRowView = dt.DefaultView[0];
                        string numeroRemito = dataRowView["Remito"]?.ToString();

                        foreach (DataGridViewRow dgvRow in dgvVentas.Rows)
                        {
                            string valorCelda = dgvRow.Cells["Remito"]?.Value?.ToString();
                            if (valorCelda == numeroRemito)
                            {
                                dgvVentas.ClearSelection();
                                dgvRow.Selected = true;
                                dgvVentas.CurrentCell = dgvRow.Cells[0];
                                
                                if (dgvRow.Index < dgvVentas.FirstDisplayedScrollingRowIndex || 
                                    dgvRow.Index > dgvVentas.FirstDisplayedScrollingRowIndex + dgvVentas.DisplayedRowCount(false))
                                {
                                    dgvVentas.FirstDisplayedScrollingRowIndex = dgvRow.Index;
                                }
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // Si hay algún error, simplemente no seleccionar nada
                    }
                }

                // NUEVO: Mostrar información del filtro en el título
                ActualizarTituloConFiltros(filtros.Count, dt.DefaultView.Count, dt.Rows.Count);
            }
        }

        // AGREGAR: Método para actualizar resumen con datos filtrados
        private void ActualizarResumenFiltrado()
        {
            if (dgvVentas.DataSource is DataTable dt)
            {
                // Crear un DataTable temporal con solo las filas visibles
                DataTable dtFiltrado = dt.DefaultView.ToTable();
                ActualizarResumen(dtFiltrado);
            }
        }

        // NUEVO: Método para actualizar el título con información de filtros
        private void ActualizarTituloConFiltros(int cantidadFiltros, int registrosFiltrados, int totalRegistros)
        {
            try
            {
                DateTime inicio = dtpDesde.Value.Date;
                DateTime fin = dtpHasta.Value.Date;

                string tituloBase;
                if (inicio == DateTime.Today && fin == DateTime.Today)
                    tituloBase = $"Control de Facturas - Ventas del Día";
                else if (inicio == fin)
                    tituloBase = $"Control de Facturas - Ventas del {inicio:dd/MM/yyyy}";
                else
                    tituloBase = $"Control de Facturas - Ventas {inicio:dd/MM/yyyy} → {fin:dd/MM/yyyy}";

                if (cantidadFiltros > 0)
                {
                    var filtrosActivos = new List<string>();

                    if (!string.IsNullOrEmpty(txtFiltroCajero.Text.Trim()))
                        filtrosActivos.Add($"Cajero: '{txtFiltroCajero.Text.Trim()}'");

                    if (chkCtaCte.Checked && !string.IsNullOrEmpty(txtFiltroCtaCte.Text.Trim()))
                        filtrosActivos.Add($"Cliente: '{txtFiltroCtaCte.Text.Trim()}'");

                    if (cboFiltroFormaPago.SelectedItem != null && cboFiltroFormaPago.SelectedItem.ToString() != "Todas las formas")
                        filtrosActivos.Add($"Forma Pago: '{cboFiltroFormaPago.SelectedItem}'");

                    // NUEVO: Agregar rubro a los filtros activos
                    if (cboFiltroRubro.SelectedItem != null && cboFiltroRubro.SelectedItem.ToString() != "Todos los rubros")
                        filtrosActivos.Add($"Rubro: '{cboFiltroRubro.SelectedItem}'");

                    // NUEVO: Agregar proveedor a los filtros activos
                    if (cboFiltroProveedor.SelectedItem != null && cboFiltroProveedor.SelectedItem.ToString() != "Todos los proveedores")
                        filtrosActivos.Add($"Proveedor: '{cboFiltroProveedor.SelectedItem}'");

                    string infoFiltros = string.Join(" | ", filtrosActivos);
                    lblTitulo.Text = $"{tituloBase} - Filtrado: {registrosFiltrados}/{totalRegistros} ({infoFiltros})";
                    lblTitulo.ForeColor = Color.FromArgb(255, 111, 0);
                }
                else
                {
                    lblTitulo.Text = tituloBase;
                    lblTitulo.ForeColor = Color.FromArgb(0, 120, 215);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando título con filtros: {ex.Message}");
            }
        }

        // AGREGAR: Event handler para el botón Buscar
        private void BtnBuscar_Click(object sender, EventArgs e)
        {
            CargarVentasPorFecha(dtpDesde.Value.Date, dtpHasta.Value.Date);
        }

        // AGREGAR: Event handler para el botón Hoy
        private void BtnHoy_Click(object sender, EventArgs e)
        {
            dtpDesde.Value = DateTime.Today;
            dtpHasta.Value = DateTime.Today;
            CargarVentasPorFecha(DateTime.Today, DateTime.Today);
        }

        // AGREGAR: Event handler para el click en las celdas del DataGridView
        private void DgvVentas_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var numeroRemito = dgvVentas.Rows[e.RowIndex].Cells["Remito"].Value?.ToString();
                if (!string.IsNullOrEmpty(numeroRemito))
                {
                    CargarDetalleFactura(numeroRemito);
                    MostrarVentanaDetalle();
                }
            }
        }

        // AGREGAR: Event handler para el click en el formulario
        private void FrmControlFacturas_Click(object sender, EventArgs e)
        {
            if (frmDetalle != null && !frmDetalle.IsDisposed && frmDetalle.Visible)
            {
                frmDetalle.Hide();
            }
        }

        // CORREGIDO: Event handler para el botón Imprimir - IMPLEMENTACIÓN COMPLETA
        private async void BtnImprimir_Click(object sender, EventArgs e)
        {
            try
            {
                // Obtener el número de Remito actual desde el título de la ventana
                string tituloCompleto = frmDetalle.Text;
                string numeroRemito = ExtraerNumeroRemitoDelTitulo(tituloCompleto);
                
                if (string.IsNullOrEmpty(numeroRemito))
                {
                    MessageBox.Show("No se puede determinar el número de Remito para imprimir.", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"🖨️ Iniciando impresión de factura/remito: {numeroRemito}");

                // Obtener los datos de la factura desde la base de datos
                var datosFactura = await ObtenerDatosParaImpresion(numeroRemito);
                if (datosFactura == null)
                {
                    MessageBox.Show("No se pudieron obtener los datos de la factura para imprimir.", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Crear configuración de ticket con el número correcto
                var config = await CrearConfiguracionTicketAsync(datosFactura, numeroRemito);

                // Obtener los productos de la factura
                var datosProductos = await ObtenerProductosFactura(numeroRemito);
                if (datosProductos == null || datosProductos.Rows.Count == 0)
                {
                    MessageBox.Show("No se encontraron productos para esta factura.", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Imprimir usando el servicio de impresión existente
                using (var printingService = new TicketPrintingService())
                {
                    await printingService.ImprimirTicket(datosProductos, config);
                }

                System.Diagnostics.Debug.WriteLine($"✅ Impresión completada para: {numeroRemito}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en impresión: {ex.Message}");
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NUEVO: Método async para crear configuración de ticket con número correcto
        private async Task<TicketConfig> CrearConfiguracionTicketAsync(DatosFactura datos, string numeroRemito)
        {
            string numeroComprobanteFinal = numeroRemito;
            decimal porcentajeDescuento = 0m;
            decimal importeDescuento = 0m;
            decimal importeFinal = 0m;

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    // Obtener también los datos de descuento
                    var query = @"SELECT NroFactura, PorcentajeDescuento, ImporteDescuento, ImporteFinal 
                     FROM Facturas 
                     WHERE NumeroRemito = @numeroRemito";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@numeroRemito", numeroRemito);
                        await connection.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var nroFactura = reader["NroFactura"];
                                if (nroFactura != null && !string.IsNullOrEmpty(nroFactura.ToString()))
                                {
                                    numeroComprobanteFinal = nroFactura.ToString();
                                }

                                // Leer datos de descuento
                                porcentajeDescuento = reader["PorcentajeDescuento"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["PorcentajeDescuento"]) : 0m;
                                importeDescuento = reader["ImporteDescuento"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["ImporteDescuento"]) : 0m;
                                importeFinal = reader["ImporteFinal"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["ImporteFinal"]) : 0m;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo número de factura: {ex.Message}");
            }

            // ✅ CRÍTICO: Normalizar el tipo de comprobante
            string tipoComprobanteNormalizado = "REMITO"; // Por defecto

            if (!string.IsNullOrEmpty(datos.TipoFactura))
            {
                string tipoOriginal = datos.TipoFactura.Trim();

                System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] Tipo original desde BD: '{tipoOriginal}'");

                // Mapear todos los posibles valores a los tipos estándar
                if (tipoOriginal.Equals("FacturaA", StringComparison.OrdinalIgnoreCase) ||
                    tipoOriginal.Equals("Factura A", StringComparison.OrdinalIgnoreCase))
                {
                    tipoComprobanteNormalizado = "FacturaA";
                }
                else if (tipoOriginal.Equals("FacturaB", StringComparison.OrdinalIgnoreCase) ||
                         tipoOriginal.Equals("Factura B", StringComparison.OrdinalIgnoreCase))
                {
                    tipoComprobanteNormalizado = "FacturaB";
                }
                else if (tipoOriginal.Equals("FacturaC", StringComparison.OrdinalIgnoreCase) ||
                         tipoOriginal.Equals("Factura C", StringComparison.OrdinalIgnoreCase))
                {
                    tipoComprobanteNormalizado = "FacturaC";
                }
                else if (tipoOriginal.Equals("Remito", StringComparison.OrdinalIgnoreCase) ||
                         tipoOriginal.Equals("RemitoTicket", StringComparison.OrdinalIgnoreCase) ||
                         tipoOriginal.Equals("Ticket", StringComparison.OrdinalIgnoreCase))
                {
                    tipoComprobanteNormalizado = "REMITO";
                }
                else
                {
                    // Si no coincide con ninguno conocido, usar REMITO por seguridad
                    System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ⚠️ Tipo no reconocido: '{tipoOriginal}', usando REMITO");
                    tipoComprobanteNormalizado = "REMITO";
                }
            }

            System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ✅ Tipo normalizado: '{tipoComprobanteNormalizado}'");
            System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ✅ Número comprobante: '{numeroComprobanteFinal}'");
            System.Diagnostics.Debug.WriteLine($"[IMPRESIÓN] ✅ Descuento: {porcentajeDescuento}% ({importeDescuento:C2})");

            return new TicketConfig
            {
                NombreComercio = datos.NombreComercio,
                DomicilioComercio = datos.DomicilioComercio,
                TipoComprobante = tipoComprobanteNormalizado,  // ✅ USAR TIPO NORMALIZADO
                NumeroComprobante = numeroComprobanteFinal,
                FormaPago = datos.FormaPago,
                CAE = datos.CAENumero,
                CAEVencimiento = datos.CAEVencimiento,
                CUIT = datos.CUITCliente,
                MensajePie = "¡Gracias por su compra!",
                PorcentajeDescuento = porcentajeDescuento,
                ImporteDescuento = importeDescuento,
                ImporteFinal = importeFinal
            };
        }

        // NUEVO: Método para obtener datos de la factura para impresión
        private async Task<DatosFactura> ObtenerDatosParaImpresion(string numeroRemito)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"
                        SELECT 
                            NumeroRemito,
                            NroFactura,
                            TipoFactura,
                            FormadePago,
                            CAENumero,
                            CAEVencimiento,
                            CUITCliente,
                            CtaCteNombre,
                            ImporteTotal,
                            IVA,
                            Fecha,
                            Hora
                        FROM Facturas 
                        WHERE NumeroRemito = @numeroRemito";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@numeroRemito", numeroRemito);
                        await connection.OpenAsync();
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new DatosFactura
                                {
                                    TipoFactura = reader["TipoFactura"]?.ToString() ?? "",
                                    FormaPago = reader["FormadePago"]?.ToString() ?? "",
                                    CAENumero = reader["CAENumero"]?.ToString() ?? "",
                                    CAEVencimiento = reader["CAEVencimiento"] as DateTime?,
                                    CUITCliente = reader["CUITCliente"]?.ToString() ?? "",
                                    NombreComercio = config["Comercio:Nombre"] ?? "Mi Comercio",
                                    DomicilioComercio = config["Comercio:Domicilio"] ?? ""
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error obteniendo datos de factura: {ex.Message}");
                throw;
            }

            return null;
        }

        // NUEVO: Método para obtener productos de la factura
        private async Task<DataTable> ObtenerProductosFactura(string numeroRemito)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"
                        SELECT 
                            codigo,
                            descripcion,
                            cantidad,
                            precio,
                            total
                        FROM Ventas 
                        WHERE NroFactura = @numeroRemito
                        ORDER BY descripcion";

                    using (var adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@numeroRemito", numeroRemito);
                        DataTable dt = new DataTable();
                        await Task.Run(() => adapter.Fill(dt));
                        
                        System.Diagnostics.Debug.WriteLine($"📦 Productos obtenidos: {dt.Rows.Count} items");
                        return dt;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error obteniendo productos: {ex.Message}");
                throw;
            }
        }

        // SIMPLIFICADO: Método auxiliar para extraer número de remito del título
        private string ExtraerNumeroRemitoDelTitulo(string titulo)
        {
            try
            {
                if (string.IsNullOrEmpty(titulo)) return "";
                
                // Buscar el primer número que aparece después de "N°"
                var match = System.Text.RegularExpressions.Regex.Match(titulo, @"N°\s*(\d+)");
                return match.Success ? match.Groups[1].Value : "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ExtraerNumeroRemitoDelTitulo: {ex.Message}");
                return "";
            }
        }

        // AGREGAR: Método para cargar detalle de una factura específica
        private void CargarDetalleFactura(string nroFactura)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    // CORREGIDO: Usar el nombre correcto de la columna
                    var query = @"
                SELECT 
                    codigo as 'Código',
                    descripcion as 'Producto',
                    cantidad as 'Cantidad',
                    precio as 'Precio Unit.',
                    total as 'Total'
                FROM Ventas 
                WHERE NroFactura = @nroFactura
                ORDER BY descripcion";

                    using (var adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@nroFactura", nroFactura);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        // Buscar el DataGridView en la ventana flotante
                        var dgvDetalle = frmDetalle.Controls.Find("dgvDetalle", true).FirstOrDefault() as DataGridView;
                        if (dgvDetalle != null)
                        {
                            dgvDetalle.DataSource = dt;
                            FormatearColumnasDetalle(dgvDetalle);
                        }

                        // Actualizar totales
                        ActualizarTotalesDetalle(dt);

                        // NUEVO: Actualizar detalle de formas de pago para este remito/factura
                        ActualizarFormasPagoDetalle(nroFactura);

                        // Actualizar el título de la ventana con información adicional
                        ActualizarTituloDetalle(nroFactura);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el detalle de la factura: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ActualizarFormasPagoDetalle(string nroFactura)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                var lista = new List<string>();

                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"
                SELECT ISNULL(MedioPago, '(No especificado)') AS MedioPago, SUM(ISNULL(Importe,0)) AS Importe
                FROM DetallesPagoFactura
                WHERE NumeroRemito = @numeroRemito
                GROUP BY MedioPago
                ORDER BY MedioPago";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@numeroRemito", nroFactura);
                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string medio = reader["MedioPago"]?.ToString() ?? "(No especificado)";
                                decimal importe = reader["Importe"] != DBNull.Value ? Convert.ToDecimal(reader["Importe"]) : 0m;
                                lista.Add($"{medio}: {importe:C2}");
                            }
                        }
                    }
                }

                var lblDetalleFormasPagoDetalle = frmDetalle.Controls.Find("lblDetalleFormasPagoDetalle", true).FirstOrDefault() as Label;
                if (lblDetalleFormasPagoDetalle != null)
                {
                    // Si hay muchas formas, separar en varias líneas para que quepan mejor
                    if (lista.Count == 0)
                    {
                        lblDetalleFormasPagoDetalle.Text = "";
                    }
                    else if (lista.Count <= 3)
                    {
                        // agrupar en una sola línea con separador
                        lblDetalleFormasPagoDetalle.Text = string.Join("  |  ", lista);
                    }
                    else
                    {
                        // más de 3 formas: mostrar cada una en nueva línea
                        lblDetalleFormasPagoDetalle.Text = string.Join(Environment.NewLine, lista);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ActualizarFormasPagoDetalle: {ex.Message}");
                var lblDetalleFormasPagoDetalle = frmDetalle.Controls.Find("lblDetalleFormasPagoDetalle", true).FirstOrDefault() as Label;
                if (lblDetalleFormasPagoDetalle != null)
                    lblDetalleFormasPagoDetalle.Text = "";
            }
        }

        // AGREGAR: Método para formatear columnas del DataGridView de detalle
        private void FormatearColumnasDetalle(DataGridView dgvDetalle)
        {
            if (dgvDetalle.Columns.Count == 0) return;

            // Temporalmente deshabilitar AutoSizeColumnsMode para permitir configuración manual
            var originalAutoSizeMode = dgvDetalle.AutoSizeColumnsMode;
            dgvDetalle.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            try
            {
                var codigoCol = dgvDetalle.Columns["Código"];
                if (codigoCol != null)
                {
                    codigoCol.Width = 100;
                    codigoCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var productoCol = dgvDetalle.Columns["Producto"];
                if (productoCol != null)
                {
                    productoCol.Width = 300;
                }

                var cantidadCol = dgvDetalle.Columns["Cantidad"];
                if (cantidadCol != null)
                {
                    cantidadCol.Width = 100;
                    cantidadCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var precioCol = dgvDetalle.Columns["Precio Unit."];
                if (precioCol != null)
                {
                    precioCol.DefaultCellStyle.Format = "C2";
                    precioCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    precioCol.Width = 120;
                }

                var totalCol = dgvDetalle.Columns["Total"];
                if (totalCol != null)
                {
                    totalCol.DefaultCellStyle.Format = "C2";
                    totalCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    totalCol.Width = 120;
                }
            }
            finally
            {
                // Restaurar el modo original
                dgvDetalle.AutoSizeColumnsMode = originalAutoSizeMode;
            }
        }

        // AGREGAR: Método para actualizar totales en la ventana de detalle
        private void ActualizarTotalesDetalle(DataTable dt)
        {
            int cantidadProductos = dt.Rows.Count;
            decimal cantidadTotal = 0;
            decimal totalFactura = 0;

            foreach (DataRow row in dt.Rows)
            {
                if (decimal.TryParse(row["Cantidad"].ToString(), out decimal cantidad))
                {
                    cantidadTotal += cantidad;
                }

                if (decimal.TryParse(row["Total"].ToString(), out decimal total))
                {
                    totalFactura += total;
                }
            }

            // Actualizar los labels de totales
            var lblCantidadProductos = frmDetalle.Controls.Find("lblCantidadProductos", true).FirstOrDefault() as Label;
            if (lblCantidadProductos != null)
                lblCantidadProductos.Text = $"Productos: {cantidadProductos}";

            var lblCantidadTotalDetalle = frmDetalle.Controls.Find("lblCantidadTotalDetalle", true).FirstOrDefault() as Label;
            if (lblCantidadTotalDetalle != null)
                lblCantidadTotalDetalle.Text = $"Cantidad: {cantidadTotal:N0}";

            var lblTotalFactura = frmDetalle.Controls.Find("lblTotalFactura", true).FirstOrDefault() as Label;
            if (lblTotalFactura != null)
                lblTotalFactura.Text = $"Total: {totalFactura:C2}";
        }

        // AGREGAR: Método para actualizar el título de la ventana de detalle
        private void ActualizarTituloDetalle(string nroFactura)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    // MODIFICADO: Obtener también el número de remito y número de factura
                    var query = @"
                        SELECT 
                            NumeroRemito,
                            NroFactura,
                            TipoFactura,
                            FormadePago,
                            CAENumero,
                            CUITCliente
                        FROM Facturas 
                        WHERE NumeroRemito = @nroFactura"; // Usar NumeroRemito ya que nroFactura contiene el número de remito

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@nroFactura", nroFactura);
                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string numeroRemito = reader["NumeroRemito"]?.ToString() ?? "";
                                string numeroFactura = reader["NroFactura"]?.ToString() ?? "";
                                string tipoFactura = reader["TipoFactura"]?.ToString() ?? "";
                                string formaPago = reader["FormadePago"]?.ToString() ?? "";
                                string cae = reader["CAENumero"]?.ToString() ?? "";
                                string cuit = reader["CUITCliente"]?.ToString() ?? "";

                                // NUEVO: Construir título con número de remito y factura según corresponda
                                string titulo = "";
                        
                                if (tipoFactura == "FacturaA" || tipoFactura == "FacturaB")
                                {
                                    // Para facturas A/B: mostrar tanto remito como factura
                                    titulo = $"Detalle  - Remito N° {numeroRemito}";
                                    if (!string.IsNullOrEmpty(numeroFactura))
                                    {
                                        titulo += $" - Factura N° {numeroFactura}";
                                    }
                                }
                                else
                                {
                                    // Para remitos: solo mostrar número de remito
                                    titulo = $"Detalle {tipoFactura} N° {numeroRemito}";
                                }

                                // Agregar información adicional
                                if (!string.IsNullOrEmpty(cae))
                                    titulo += $" - CAE: {cae}";

                                if (!string.IsNullOrEmpty(cuit))
                                    titulo += $" - CUIT: {cuit}";

                                frmDetalle.Text = titulo;
                            }
 else
                            {
                                frmDetalle.Text = $"Detalle de Comprobante N° {nroFactura}";
                            }
                        }
                    }
                }
            }
            catch
            {
                frmDetalle.Text = $"Detalle de Comprobante N° {nroFactura}";
            }
        }

        // AGREGAR: Método para mostrar la ventana de detalle
        private void MostrarVentanaDetalle()
        {
            // Check if form is disposed and recreate if necessary
            if (frmDetalle == null || frmDetalle.IsDisposed)
            {
                CrearVentanaDetalle();
            }
            
            if (frmDetalle != null && !frmDetalle.IsDisposed)
            {
                // CENTRAR RESPECTO AL FORMULARIO MDI PADRE
                Form mdiParent = this.MdiParent;
                
                if (mdiParent != null)
                {
                    Rectangle parentBounds;
            
                    if (mdiParent.WindowState == FormWindowState.Maximized)
                    {
                        // Si el MDI parent está maximizado, usar el área de trabajo de la pantalla
                        parentBounds = Screen.FromControl(mdiParent).WorkingArea;
                    }
                    else
                    {
                        // Si el MDI parent no está maximizado, usar sus bounds reales
                        parentBounds = mdiParent.Bounds;
                    }
            
                    int x = parentBounds.X + (parentBounds.Width - frmDetalle.Width) / 2;
                    int y = parentBounds.Y + (parentBounds.Height - frmDetalle.Height) / 2;
            
                    // Asegurar que no se salga de la pantalla
                    Rectangle screenBounds = Screen.FromControl(this).WorkingArea;
                    if (x < screenBounds.X) x = screenBounds.X;
                    if (y < screenBounds.Y) y = screenBounds.Y;
                    if (x + frmDetalle.Width > screenBounds.Right) 
                        x = screenBounds.Right - frmDetalle.Width;
                    if (y + frmDetalle.Height > screenBounds.Bottom) 
                        y = screenBounds.Bottom - frmDetalle.Height;
            
                    frmDetalle.Location = new Point(x, y);
                }
                else
                {
                    // Si no hay MDI parent, centrar en pantalla
                    Rectangle workingArea = Screen.FromControl(this).WorkingArea;
                    int x = workingArea.X + (workingArea.Width - frmDetalle.Width) / 2;
                    int y = workingArea.Y + (workingArea.Height - frmDetalle.Height) / 2;
                    frmDetalle.Location = new Point(x, y);
                }
                
                frmDetalle.Show();
                frmDetalle.BringToFront();
            }
        }

        // MODIFICAR: Método para formatear columnas con corrección de tipos
        private void FormatearColumnas()
        {
            if (dgvVentas.Columns.Count == 0) return;

            var originalAutoSizeMode = dgvVentas.AutoSizeColumnsMode;
            dgvVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            try
            {
                // COLUMNAS FIJAS
                var remitoCol = dgvVentas.Columns["Remito"];
                if (remitoCol != null)
                {
                    remitoCol.Width = 80;
                    remitoCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    remitoCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    remitoCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var facturaCol = dgvVentas.Columns["N° Factura"];
                if (facturaCol != null)
                {
                    facturaCol.Width = 100;
                    facturaCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    facturaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    facturaCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // ✅ MODIFICADO: Aumentar ancho de "Total Final" (era 100, ahora 120)
                var importeFinalCol = dgvVentas.Columns["Importe Final"];
                if (importeFinalCol != null)
                {
                    importeFinalCol.Width = 140; // ✅ CAMBIO: de 100 a 120
                    importeFinalCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    importeFinalCol.DefaultCellStyle.Format = "C2";
                    importeFinalCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    importeFinalCol.DefaultCellStyle.ForeColor = Color.FromArgb(40, 167, 69);
                    importeFinalCol.DefaultCellStyle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
                    importeFinalCol.HeaderCell.Style.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
                    importeFinalCol.HeaderText = "Total Final";
                }

                // Columna % Descuento
                var porcentajeDescCol = dgvVentas.Columns["% Descuento"];
                if (porcentajeDescCol != null)
                {
                    porcentajeDescCol.Width = 70;
                    porcentajeDescCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    porcentajeDescCol.DefaultCellStyle.Format = "N2";
                    porcentajeDescCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    porcentajeDescCol.DefaultCellStyle.ForeColor = Color.FromArgb(255, 152, 0);
                    porcentajeDescCol.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    porcentajeDescCol.HeaderText = "% Desc";
                    porcentajeDescCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // Columna Descuento
                var descuentoCol = dgvVentas.Columns["Descuento"];
                if (descuentoCol != null)
                {
                    descuentoCol.Width = 80;
                    descuentoCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    descuentoCol.DefaultCellStyle.Format = "C2";
                    descuentoCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    descuentoCol.DefaultCellStyle.ForeColor = Color.FromArgb(220, 53, 69);
                    descuentoCol.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    descuentoCol.HeaderText = "Descuento";
                    descuentoCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // Columna IVA
                var ivaCol = dgvVentas.Columns["IVA"];
                if (ivaCol != null)
                {
                    ivaCol.Width = 80;
                    ivaCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    ivaCol.DefaultCellStyle.Format = "C2";
                    ivaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    ivaCol.DefaultCellStyle.ForeColor = Color.FromArgb(220, 53, 69);
                    ivaCol.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    ivaCol.HeaderText = "IVA";
                    ivaCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // Columna Subtotal
                var subtotalCol = dgvVentas.Columns["Subtotal"];
                if (subtotalCol != null)
                {
                    subtotalCol.Width = 90;
                    subtotalCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    subtotalCol.DefaultCellStyle.Format = "C2";
                    subtotalCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    subtotalCol.DefaultCellStyle.ForeColor = Color.FromArgb(108, 117, 125);
                    subtotalCol.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
                    subtotalCol.HeaderText = "Subtotal";
                    subtotalCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // Columna Cajero
                var cajeroCol = dgvVentas.Columns["Cajero"];
                if (cajeroCol != null)
                {
                    cajeroCol.Width = 60;
                    cajeroCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    cajeroCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    cajeroCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var fechaCol = dgvVentas.Columns["Fecha"];
                if (fechaCol != null)
                {
                    fechaCol.DefaultCellStyle.Format = "dd/MM/yyyy";
                    fechaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    fechaCol.Width = 80;
                    fechaCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    fechaCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var horaCol = dgvVentas.Columns["Hora"];
                if (horaCol != null)
                {
                    horaCol.Width = 60;
                    horaCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    horaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    horaCol.DefaultCellStyle.Format = "HH:mm";
                    horaCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // ✅ MODIFICADO: Reducir ancho de "Forma de Pago" (era Fill con FillWeight 120, ahora ancho fijo 90)
                var formaPagoCol = dgvVentas.Columns["Forma de Pago"];
                if (formaPagoCol != null)
                {
                    formaPagoCol.Width = 110; // ✅ CAMBIO: de Fill a ancho fijo de 90
                    formaPagoCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // ✅ CAMBIO: de Fill a None
                    formaPagoCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    formaPagoCol.HeaderText = "Forma/Pago";
                    formaPagoCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var tipoFacturaCol = dgvVentas.Columns["Tipo"];
                if (tipoFacturaCol != null)
                {
                    tipoFacturaCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    tipoFacturaCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    tipoFacturaCol.FillWeight = 80;
                    tipoFacturaCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // ✅ MODIFICADO: Reducir ancho de "CAE" (era Fill con FillWeight 100, ahora ancho fijo 70)
                var caeCol = dgvVentas.Columns["CAE"];
                if (caeCol != null)
                {
                    caeCol.Width = 110; // ✅ CAMBIO: de Fill a ancho fijo de 70
                    caeCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // ✅ CAMBIO: de Fill a None
                    caeCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    caeCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var ctaCteCol = dgvVentas.Columns["Cta. Cte. Nombre"];
                if (ctaCteCol != null)
                {
                    ctaCteCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    ctaCteCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    ctaCteCol.FillWeight = 120;
                    ctaCteCol.HeaderText = "CC.Nombre";
                    ctaCteCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var proveedorCol = dgvVentas.Columns["Proveedor"];
                if (proveedorCol != null)
                {
                    proveedorCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    proveedorCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    proveedorCol.FillWeight = 100;
                    proveedorCol.HeaderText = "Proveedor";
                    proveedorCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    proveedorCol.Visible = false; // ✅ Ocultar por defecto si no quieres mostrarla en la grilla
                }
            }
            finally
            {
                //dgvVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnMode.None;
            }
        }

        // AGREGAR: Método para actualizar resumen en el panel principal
        private void ActualizarResumen(DataTable dt)
        {
            try
            {
                int cantidadVentas = dt.Rows.Count;
                decimal totalVentas = 0;
                decimal totalIVA = 0;
                decimal subtotalSinIVA = 0;
                decimal totalDescuentos = 0; // ✅ NUEVO

                // Diccionarios para contar tipos de factura y formas de pago
                var tiposFactura = new Dictionary<string, int>();
                var formasPago = new Dictionary<string, decimal>();

                // Variables para debugging
                int filasConErrores = 0;
                var errores = new List<string>();

                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        // Procesar Importe Total
                        object importeObj = row["Importe Final"];
                        decimal importe = 0;
                        
                        if (importeObj != null && importeObj != DBNull.Value)
                        {
                            if (importeObj is decimal decimalValue)
                            {
                                importe = decimalValue;
                                totalVentas += importe;
                            }
                            else
                            {
                                string importeStr = importeObj.ToString().Trim();
                                
                                if (decimal.TryParse(importeStr, out importe))
                                {
                                    totalVentas += importe;
                                }
                                else if (decimal.TryParse(importeStr, NumberStyles.Currency, CultureInfo.CurrentCulture, out importe) ||
                                         decimal.TryParse(importeStr, NumberStyles.Number, CultureInfo.InvariantCulture, out importe))
                                {
                                    totalVentas += importe;
                                }
                                else
                                {
                                    filasConErrores++;
                                    errores.Add($"Fila {dt.Rows.IndexOf(row) + 1}: '{importeStr}' no es un número válido");
                                }
                            }
                        }

                        // ✅ NUEVO: Procesar Descuento
                        object descuentoObj = row["Descuento"];
                        decimal descuento = 0;

                        if (descuentoObj != null && descuentoObj != DBNull.Value)
                        {
                            if (descuentoObj is decimal descuentoDecimalValue)
                            {
                                descuento = descuentoDecimalValue;
                                totalDescuentos += descuento;
                            }
                            else
                            {
                                string descuentoStr = descuentoObj.ToString().Trim();

                                if (decimal.TryParse(descuentoStr, out descuento) ||
                                    decimal.TryParse(descuentoStr, NumberStyles.Currency, CultureInfo.CurrentCulture, out descuento) ||
                                    decimal.TryParse(descuentoStr, NumberStyles.Number, CultureInfo.InvariantCulture, out descuento))
                                {
                                    totalDescuentos += descuento;
                                }
                            }
                        }

                        // NUEVO: Procesar IVA
                        object ivaObj = row["IVA"];
                        decimal iva = 0;
                        
                        if (ivaObj != null && ivaObj != DBNull.Value)
                        {
                            if (ivaObj is decimal ivaDecimalValue)
                            {
                                iva = ivaDecimalValue;
                                totalIVA += iva;
                            }
                            else
                            {
                                string ivaStr = ivaObj.ToString().Trim();
                                
                                if (decimal.TryParse(ivaStr, out iva) ||
                                    decimal.TryParse(ivaStr, NumberStyles.Currency, CultureInfo.CurrentCulture, out iva) ||
                                    decimal.TryParse(ivaStr, NumberStyles.Number, CultureInfo.InvariantCulture, out iva))
                                {
                                    totalIVA += iva;
                                }
                            }
                        }

                        // NUEVO: Procesar Subtotal
                        object subtotalObj = row["Subtotal"];
                        decimal subtotal = 0;
                        
                        if (subtotalObj != null && subtotalObj != DBNull.Value)
                        {
                            if (subtotalObj is decimal subtotalDecimalValue)
                            {
                                subtotal = subtotalDecimalValue;
                                subtotalSinIVA += subtotal;
                            }
                            else
                            {
                                string subtotalStr = subtotalObj.ToString().Trim();
                                
                                if (decimal.TryParse(subtotalStr, out subtotal) ||
                                    decimal.TryParse(subtotalStr, NumberStyles.Currency, CultureInfo.CurrentCulture, out subtotal) ||
                                    decimal.TryParse(subtotalStr, NumberStyles.Number, CultureInfo.InvariantCulture, out subtotal))
                                {
                                    subtotalSinIVA += subtotal;
                                }
                            }
                        }

                        // Contar tipos de factura
                        string tipoFactura = row["Tipo"]?.ToString()?.Trim() ?? "Sin especificar";
                        if (tiposFactura.ContainsKey(tipoFactura))
                            tiposFactura[tipoFactura]++;
                        else
                            tiposFactura[tipoFactura] = 1;

                        // Sumar por formas de pago
                        string formaPago = row["Forma de Pago"]?.ToString()?.Trim() ?? "Sin especificar";
                        if (formasPago.ContainsKey(formaPago))
                            formasPago[formaPago] += importe;
                        else
                            formasPago[formaPago] = importe;
                    }
                    catch (Exception rowEx)
                    {
                        filasConErrores++;
                        errores.Add($"Fila {dt.Rows.IndexOf(row) + 1}: Error procesando fila - {rowEx.Message}");
                    }
                }

                // Actualizar labels principales
                lblCantidadVentas.Text = $"Ventas: {cantidadVentas}";

                // ✅ MODIFICADO: Mostrar también el total de descuentos si existe
                if (totalDescuentos > 0)
                {
                    lblTotal.Text = $"Total Final: {totalVentas:C2} (Desc: {totalDescuentos:C2})";
                }
                else
                {
                    lblTotal.Text = $"Total Final: {totalVentas:C2}";
                }

                var lblTotalIVA = this.Controls.Find("lblTotalIVA", true).FirstOrDefault() as Label;
                if (lblTotalIVA != null)
                    lblTotalIVA.Text = $"IVA Total: {totalIVA:C2}";

                var lblSubtotalSinIVA = this.Controls.Find("lblSubtotalSinIVA", true).FirstOrDefault() as Label;
                if (lblSubtotalSinIVA != null)
                    lblSubtotalSinIVA.Text = $"Subtotal sin IVA: {subtotalSinIVA:C2}";

                // Actualizar detalle de tipos de factura
                string detalleTipos = string.Join(" | ",
                    tiposFactura.Select(kv => $"{kv.Key}: {kv.Value}"));
                lblDetalleTiposFactura.Text = detalleTipos;

                // Actualizar detalle de formas de pago
                string detalleFormas = string.Join(" | ",
                    formasPago.Select(kv => $"{kv.Key}: {kv.Value:C2}"));
                lblDetalleFormasPago.Text = detalleFormas;

                if (filasConErrores > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"ADVERTENCIA: {filasConErrores} filas con errores en ActualizarResumen:");
                    foreach (var error in errores.Take(5))
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {error}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"ActualizarResumen: {cantidadVentas} ventas, Total Final: {totalVentas:C2}, Descuentos: {totalDescuentos:C2}, IVA: {totalIVA:C2}, Subtotal: {subtotalSinIVA:C2}, Errores: {filasConErrores}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ActualizarResumen: {ex.Message}");

                lblCantidadVentas.Text = "Ventas: 0";
                lblTotal.Text = "Total Final: $0,00";

                var lblTotalIVA = this.Controls.Find("lblTotalIVA", true).FirstOrDefault() as Label;
                if (lblTotalIVA != null)
                    lblTotalIVA.Text = "IVA Total: $0,00";

                var lblSubtotalSinIVA = this.Controls.Find("lblSubtotalSinIVA", true).FirstOrDefault() as Label;
                if (lblSubtotalSinIVA != null)
                    lblSubtotalSinIVA.Text = "Subtotal sin IVA: $0,00";

                lblDetalleTiposFactura.Text = "Error calculando tipos";
                lblDetalleFormasPago.Text = "Error calculando formas de pago";

                MessageBox.Show($"❌ Error calculando totales:\n{ex.Message}",
                       "Error Cálculo Totales", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // AGREGAR: Event handler para el botón de auditoría de eliminados
        private void BtnAuditoriaEliminados_Click(object sender, EventArgs e)
        {
            try
            {
                // TODO: Implementar la ventana de auditoría de eliminados
                using (var consultaForm = new ConsultaAuditoriaEliminados())
                {
                    consultaForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir la consulta de auditoría: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // AGREGAR: Event handler para el botón de resumen de IVA
        private void BtnResumenIva_Click(object sender, EventArgs e)
        {
            try
            {
                // Abrir el resumen de IVA usando el rango seleccionado (desde → hasta)
                using (var resumenForm = new ResumenIvaForm(dtpDesde.Value.Date, dtpHasta.Value.Date, chkCtaCte.Checked))
                {
                    resumenForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir el resumen de IVA: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        

        

        
        // ✅ NUEVO: Event handler para Exportar Listado
        private void BtnExportarListado_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvVentas.Rows.Count == 0)
                {
                    MessageBox.Show("No hay datos para exportar.",
                        "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Mostrar diálogo de opciones
                using (var dialogoOpciones = new Form())
                {
                    dialogoOpciones.Text = "Exportar Listado de Facturas";
                    dialogoOpciones.Size = new Size(400, 200);
                    dialogoOpciones.StartPosition = FormStartPosition.CenterParent;
                    dialogoOpciones.FormBorderStyle = FormBorderStyle.FixedDialog;
                    dialogoOpciones.MaximizeBox = false;
                    dialogoOpciones.MinimizeBox = false;

                    var lblPregunta = new Label
                    {
                        Text = "Seleccione el formato de exportación:",
                        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                        Location = new Point(20, 20),
                        AutoSize = true
                    };

                    var btnExcel = new Button
                    {
                        Text = "📊 Exportar a Excel",
                        BackColor = Color.FromArgb(33, 150, 83),
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                        Size = new Size(160, 40),
                        Location = new Point(20, 60)
                    };

                    var btnPDF = new Button
                    {
                        Text = "📄 Exportar a PDF",
                        BackColor = Color.FromArgb(220, 53, 69),
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                        Size = new Size(160, 40),
                        Location = new Point(200, 60)
                    };

                    var btnImprimir = new Button
                    {
                        Text = "🖨️ Imprimir Listado",
                        BackColor = Color.FromArgb(0, 120, 215),
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                        Size = new Size(340, 40),
                        Location = new Point(20, 110)
                    };

                    btnExcel.Click += (s, args) =>
                    {
                        ExportarAExcel();
                        dialogoOpciones.Close();
                    };

                    btnPDF.Click += (s, args) =>
                    {
                        ExportarAPDF();
                        dialogoOpciones.Close();
                    };

                    btnImprimir.Click += (s, args) =>
                    {
                        ImprimirListado();
                        dialogoOpciones.Close();
                    };

                    dialogoOpciones.Controls.Add(lblPregunta);
                    dialogoOpciones.Controls.Add(btnExcel);
                    dialogoOpciones.Controls.Add(btnPDF);
                    dialogoOpciones.Controls.Add(btnImprimir);

                    dialogoOpciones.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar listado: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ✅ NUEVO: Exportar a Excel
        private void ExportarAExcel()
        {
            try
            {
                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "Archivos Excel (*.csv)|*.csv";
                    saveDialog.FileName = $"Facturas_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                    saveDialog.Title = "Guardar listado como CSV (Excel)";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        var sb = new System.Text.StringBuilder();

                        // ✅ Encabezados - EXCLUIR "Fecha" y RENOMBRAR "Hora" a "Fecha"
                        var headers = new List<string>();
                        foreach (DataGridViewColumn col in dgvVentas.Columns)
                        {
                            if (col.Visible)
                            {
                                // ✅ EXCLUIR columna "Fecha"
                                if (col.HeaderText == "Fecha")
                                    continue;

                                // ✅ RENOMBRAR "Hora" a "Fecha"
                                string headerText = col.HeaderText == "Hora" ? "Fecha" : col.HeaderText;
                                headers.Add($"\"{headerText}\"");
                            }
                        }
                        sb.AppendLine(string.Join(",", headers));

                        // ✅ Datos - EXCLUIR columna "Fecha"
                        foreach (DataGridViewRow row in dgvVentas.Rows)
                        {
                            if (row.IsNewRow) continue;

                            var cells = new List<string>();
                            foreach (DataGridViewColumn col in dgvVentas.Columns)
                            {
                                if (col.Visible)
                                {
                                    // ✅ EXCLUIR columna "Fecha"
                                    if (col.HeaderText == "Fecha")
                                        continue;

                                    var value = row.Cells[col.Index].Value?.ToString() ?? "";

                                    // ✅ FORMATEAR columna "Hora" con fecha completa
                                    if (col.HeaderText == "Hora" && row.Cells["Fecha"] != null)
                                    {
                                        try
                                        {
                                            // Combinar Fecha + Hora para exportar
                                            var fecha = row.Cells["Fecha"].Value;
                                            var hora = row.Cells["Hora"].Value;

                                            if (fecha != null && fecha != DBNull.Value &&
                                                hora != null && hora != DBNull.Value)
                                            {
                                                DateTime fechaDt = Convert.ToDateTime(fecha);
                                                TimeSpan horaDt = hora is TimeSpan ts ? ts : TimeSpan.Parse(hora.ToString());

                                                var fechaCompleta = fechaDt.Date.Add(horaDt);
                                                value = fechaCompleta.ToString("dd/MM/yyyy HH:mm");
                                            }
                                        }
                                        catch
                                        {
                                            // Si hay error, usar el valor original
                                        }
                                    }

                                    cells.Add($"\"{value.Replace("\"", "\"\"")}\"");
                                }
                            }
                            sb.AppendLine(string.Join(",", cells));
                        }

                        System.IO.File.WriteAllText(saveDialog.FileName, sb.ToString(), System.Text.Encoding.UTF8);

                        MessageBox.Show($"Listado exportado correctamente a:\n{saveDialog.FileName}",
                            "Exportación exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Abrir el archivo
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar a Excel: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ✅ NUEVO: Exportar a PDF
        private void ExportarAPDF()
        {
            try
            {
                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "Archivos PDF (*.pdf)|*.pdf";
                    saveDialog.FileName = $"Facturas_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    saveDialog.Title = "Guardar listado como PDF";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        // ⚠️ NOTA: Para generar PDF necesitarás una librería como iTextSharp o QuestPDF
                        // Por ahora, exportamos como HTML que se puede imprimir a PDF
                        var html = GenerarHTMLListado();
                        var htmlFile = saveDialog.FileName.Replace(".pdf", ".html");

                        System.IO.File.WriteAllText(htmlFile, html, System.Text.Encoding.UTF8);

                        MessageBox.Show($"Listado exportado como HTML:\n{htmlFile}\n\nPuede abrirlo en el navegador e imprimir como PDF.",
                            "Exportación exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Abrir el archivo HTML
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = htmlFile,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar a PDF: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ✅ NUEVO: Generar HTML del listado
        private string GenerarHTMLListado()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='utf-8'>");
            sb.AppendLine("<title>Listado de Facturas</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            sb.AppendLine("h1 { color: #0078d7; text-align: center; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            sb.AppendLine("th { background-color: #0078d7; color: white; padding: 10px; text-align: left; }");
            sb.AppendLine("td { border: 1px solid #ddd; padding: 8px; }");
            sb.AppendLine("tr:nth-child(even) { background-color: #f2f2f2; }");
            sb.AppendLine(".total { font-weight: bold; background-color: #e3f2fd; }");
            sb.AppendLine("@media print { .no-print { display: none; } }");
            sb.AppendLine("</style></head><body>");

            // Título
            DateTime inicio = dtpDesde.Value.Date;
            DateTime fin = dtpHasta.Value.Date;
            sb.AppendLine($"<h1>Listado de Facturas - {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy}</h1>");
            sb.AppendLine($"<p><strong>Generado:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");

            // Tabla
            sb.AppendLine("<table>");
            sb.AppendLine("<thead><tr>");

            // ✅ Encabezados - EXCLUIR "Fecha" y RENOMBRAR "Hora"
            foreach (DataGridViewColumn col in dgvVentas.Columns)
            {
                if (col.Visible)
                {
                    // ✅ EXCLUIR columna "Fecha"
                    if (col.HeaderText == "Fecha")
                        continue;

                    // ✅ RENOMBRAR "Hora" a "Fecha"
                    string headerText = col.HeaderText == "Hora" ? "Fecha" : col.HeaderText;
                    sb.AppendLine($"<th>{headerText}</th>");
                }
            }
            sb.AppendLine("</tr></thead><tbody>");

            // ✅ Datos - EXCLUIR columna "Fecha"
            foreach (DataGridViewRow row in dgvVentas.Rows)
            {
                if (row.IsNewRow) continue;
                sb.AppendLine("<tr>");

                foreach (DataGridViewColumn col in dgvVentas.Columns)
                {
                    if (col.Visible)
                    {
                        // ✅ EXCLUIR columna "Fecha"
                        if (col.HeaderText == "Fecha")
                            continue;

                        var value = row.Cells[col.Index].Value?.ToString() ?? "";

                        // ✅ FORMATEAR columna "Hora" con fecha completa
                        if (col.HeaderText == "Hora" && row.Cells["Fecha"] != null)
                        {
                            try
                            {
                                var fecha = row.Cells["Fecha"].Value;
                                var hora = row.Cells["Hora"].Value;

                                if (fecha != null && fecha != DBNull.Value &&
                                    hora != null && hora != DBNull.Value)
                                {
                                    DateTime fechaDt = Convert.ToDateTime(fecha);
                                    TimeSpan horaDt = hora is TimeSpan ts ? ts : TimeSpan.Parse(hora.ToString());

                                    var fechaCompleta = fechaDt.Date.Add(horaDt);
                                    value = fechaCompleta.ToString("dd/MM/yyyy HH:mm");
                                }
                            }
                            catch
                            {
                                // Si hay error, usar el valor original
                            }
                        }

                        sb.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(value)}</td>");
                    }
                }
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");

            // Resumen
            sb.AppendLine("<div style='margin-top: 30px; padding: 15px; background-color: #f8f9fa; border-radius: 5px;'>");
            sb.AppendLine($"<h3>Resumen</h3>");
            sb.AppendLine($"<p><strong>Total de ventas:</strong> {lblCantidadVentas.Text}</p>");
            sb.AppendLine($"<p><strong>{lblTotal.Text}</strong></p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

        // ✅ NUEVO: Imprimir listado directamente
        private void ImprimirListado()
        {
            try
            {
                // Generar HTML temporal
                var html = GenerarHTMLListado();
                var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Facturas_{DateTime.Now:yyyyMMdd_HHmmss}.html");

                System.IO.File.WriteAllText(tempFile, html, System.Text.Encoding.UTF8);

                // Abrir en navegador para imprimir
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true
                });

                MessageBox.Show("Se abrió el listado en el navegador.\nUse Ctrl+P para imprimir.",
                    "Impresión", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir listado: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (frmDetalle != null && !frmDetalle.IsDisposed)
                {
                    frmDetalle.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
