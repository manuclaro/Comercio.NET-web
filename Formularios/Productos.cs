using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Comercio.NET.Formularios
{
    public partial class Productos : Form
    {
        // Declarar a nivel de clase
        private DataTable productosTable;

        public Productos()
        {
            InitializeComponent();
            this.Load += new System.EventHandler(this.Productos_Load);
            txtFiltroDescripcion.TextChanged += txtFiltroDescripcion_TextChanged;
        }

        private void Productos_Load(object sender, EventArgs e)
        {
            CargarProductos();
        }

        private void CargarProductos()
        {
            string textoFiltro = txtFiltroDescripcion.Text;

            // Cargar configuración desde appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            string connectionString = config.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT codigo, descripcion, rubro, marca, costo, porcentaje, precio, cantidad, proveedor FROM Productos";
                var adapter = new SqlDataAdapter(query, connection);
                productosTable = new DataTable();
                adapter.Fill(productosTable);
            }

            GrillaProductos.DataSource = productosTable;
            GrillaProductos.Refresh();
            Application.DoEvents();

            // Formatear la columna "precio" como moneda y alinear a la derecha
            if (GrillaProductos.Columns["precio"] != null)
            {
                GrillaProductos.Columns["precio"].DefaultCellStyle.Format = "C2";
                GrillaProductos.Columns["precio"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            if (GrillaProductos.Columns["costo"] != null)
            {
                GrillaProductos.Columns["costo"].DefaultCellStyle.Format = "C2";
                GrillaProductos.Columns["costo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // Encabezados en mayúsculas y centrados
            foreach (DataGridViewColumn col in GrillaProductos.Columns)
            {
                col.HeaderText = col.HeaderText.ToUpper();
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            GrillaProductos.Columns["porcentaje"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            GrillaProductos.Columns["cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            GrillaProductos.Columns["porcentaje"].HeaderText = "%";
            GrillaProductos.Columns["cantidad"].HeaderText = "CANT.";

            // Actualizar contador de registros
            lblContador.Text = $"Registros: {productosTable.Rows.Count}";

            // Reaplicar el filtro usando el texto actual de búsqueda
            AplicarFiltroDescripcion(textoFiltro);
        }

        // Nuevo método para aplicar el filtro
        private void AplicarFiltroDescripcion(string texto)
        {
            if (productosTable == null) return;
            texto = texto.Replace("'", "''").Trim();
            if (string.IsNullOrEmpty(texto))
            {
                productosTable.DefaultView.RowFilter = "";
            }
            else
            {
                var palabras = texto.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var filtros = palabras.Select(palabra => $"descripcion LIKE '%{palabra}%'");
                string filtroFinal = string.Join(" AND ", filtros);
                productosTable.DefaultView.RowFilter = filtroFinal;
            }
            // Actualizar contador de registros filtrados
            lblContador.Text = $"Registros: {productosTable.DefaultView.Count}";
        }

        // Evento para filtrar
        private void txtFiltroDescripcion_TextChanged(object sender, EventArgs e)
        {
            AplicarFiltroDescripcion(txtFiltroDescripcion.Text);
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAgregarProducto_Click(object sender, EventArgs e)
        {
            using (var form = new frmAgregarProducto())
            {
                var result = form.ShowDialog(this);
                if (result == DialogResult.OK && !string.IsNullOrEmpty(form.CodigoAgregado))
                {
                    CargarProductos();

                    // Selecciona y posiciona la fila agregada
                    foreach (DataGridViewRow r in GrillaProductos.Rows)
                    {
                        if (r.Cells["codigo"].Value?.ToString() == form.CodigoAgregado)
                        {
                            r.Selected = true;
                            GrillaProductos.CurrentCell = r.Cells["codigo"];
                            GrillaProductos.FirstDisplayedScrollingRowIndex = r.Index;
                            break;
                        }
                    }
                }
            }
        }

        private void btnModificarProducto_Click(object sender, EventArgs e)
        {
            if (GrillaProductos.CurrentRow == null) return;

            var row = GrillaProductos.CurrentRow;
            using (var form = new frmAgregarProducto())
            {
                form.Modo = frmAgregarProducto.ModoFormulario.Modificar;
                form.CodigoOriginal = row.Cells["codigo"].Value?.ToString();

                // Precargar los datos en los controles del formulario
                form.Controls["txtCodigo"].Text = row.Cells["codigo"].Value?.ToString();
                form.Controls["txtDescripcion"].Text = row.Cells["descripcion"].Value?.ToString();
                form.Controls["txtRubro"].Text = row.Cells["rubro"].Value?.ToString();
                form.Controls["txtMarca"].Text = row.Cells["marca"].Value?.ToString();
                form.Controls["txtCosto"].Text = row.Cells["costo"].Value?.ToString();
                form.Controls["txtPorcentaje"].Text = row.Cells["porcentaje"].Value?.ToString();
                form.Controls["txtPrecio"].Text = row.Cells["precio"].Value?.ToString();
                form.Controls["txtCantidad"].Text = row.Cells["cantidad"].Value?.ToString();
                form.Controls["txtProveedor"].Text = row.Cells["proveedor"].Value?.ToString();

                var result = form.ShowDialog(this);
                if (result == DialogResult.OK && !string.IsNullOrEmpty(form.CodigoAgregado))
                {
                    CargarProductos(); // Recarga la grilla
                    AplicarFiltroDescripcion(txtFiltroDescripcion.Text); // Reaplica el filtro actual

                    // Selecciona y posiciona la fila modificada
                    foreach (DataGridViewRow r in GrillaProductos.Rows)
                    {
                        if (r.Cells["codigo"].Value?.ToString() == form.CodigoAgregado)
                        {
                            r.Selected = true;
                            GrillaProductos.CurrentCell = r.Cells["codigo"];
                            GrillaProductos.FirstDisplayedScrollingRowIndex = r.Index;
                            break;
                        }
                    }
                }
            }
        }
    }
}
