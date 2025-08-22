using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comercio.NET.Formularios
{
    public partial class frmAgregarProducto : Form
    {
        public enum ModoFormulario { Agregar, Modificar }
        public ModoFormulario Modo { get; set; } = ModoFormulario.Agregar;
        public string CodigoOriginal { get; set; } // Para identificar el registro a modificar

        public frmAgregarProducto()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;

            txtCosto.KeyPress += TextBoxDecimal_KeyPress;
            txtPrecio.KeyPress += TextBoxDecimal_KeyPress;
            txtPorcentaje.KeyPress += TextBoxDecimal_KeyPress;
            txtCantidad.KeyPress += TextBoxEntero_KeyPress;

            // Asociar el evento KeyDown para tabular con Enter
            txtCodigo.KeyDown += TextBox_EnterAsTab;
            txtDescripcion.KeyDown += TextBox_EnterAsTab;
            txtRubro.KeyDown += TextBox_EnterAsTab;
            txtMarca.KeyDown += TextBox_EnterAsTab;
            txtCosto.KeyDown += TextBox_EnterAsTab;
            txtPorcentaje.KeyDown += TextBox_EnterAsTab;
            txtPrecio.KeyDown += TextBox_EnterAsTab;
            txtCantidad.KeyDown += TextBox_EnterAsTab;
            txtProveedor.KeyDown += TextBox_EnterAsTab;
        }

        public string CodigoAgregado { get; private set; }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            // Validación básica
            if (string.IsNullOrWhiteSpace(txtCodigo.Text) ||
                string.IsNullOrWhiteSpace(txtDescripcion.Text))
            {
                MessageBox.Show("Complete los campos obligatorios.");
                return;
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            string connectionString = config.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand cmd;

                if (Modo == ModoFormulario.Agregar)
                {
                    var query = @"INSERT INTO Productos 
                        (codigo, descripcion, rubro, marca, precio, costo, porcentaje, cantidad, proveedor)
                        VALUES (@codigo, @descripcion, @rubro, @marca, @precio, @costo, @porcentaje, @cantidad, @proveedor)";
                    cmd = new SqlCommand(query, connection);
                }
                else // Modificar
                {
                    var query = @"UPDATE Productos SET
                        codigo = @codigo,
                        descripcion = @descripcion,
                        rubro = @rubro,
                        marca = @marca,
                        precio = @precio,
                        costo = @costo,
                        porcentaje = @porcentaje,
                        cantidad = @cantidad,
                        proveedor = @proveedor
                        WHERE codigo = @codigoOriginal";
                    cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@codigoOriginal", CodigoOriginal);
                }

                cmd.Parameters.AddWithValue("@codigo", txtCodigo.Text.Trim());
                cmd.Parameters.AddWithValue("@descripcion", txtDescripcion.Text.Trim());
                cmd.Parameters.AddWithValue("@rubro", txtRubro.Text.Trim());
                cmd.Parameters.AddWithValue("@marca", txtMarca.Text.Trim());
                cmd.Parameters.AddWithValue("@precio", decimal.TryParse(txtPrecio.Text, out var precio) ? precio : 0);
                cmd.Parameters.AddWithValue("@costo", decimal.TryParse(txtCosto.Text, out var costo) ? costo : 0);
                cmd.Parameters.AddWithValue("@porcentaje", decimal.TryParse(txtPorcentaje.Text, out var porcentaje) ? porcentaje : 0);
                cmd.Parameters.AddWithValue("@cantidad", int.TryParse(txtCantidad.Text, out var cantidad) ? cantidad : 0);
                cmd.Parameters.AddWithValue("@proveedor", txtProveedor.Text.Trim());

                cmd.ExecuteNonQuery();
            }

            MessageBox.Show(Modo == ModoFormulario.Agregar ? "Producto agregado correctamente." : "Producto modificado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.DialogResult = DialogResult.OK;
            CodigoAgregado = txtCodigo.Text.Trim();
            this.Close();
        }

        // Permite solo números, una coma o punto decimal y control de teclas
        private void TextBoxDecimal_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox txt = sender as TextBox;
            char ch = e.KeyChar;

            // Permitir control (backspace, etc.)
            if (char.IsControl(ch))
                return;

            // Permitir solo un separador decimal
            if ((ch == ',' || ch == '.') && (txt.Text.Contains(",") || txt.Text.Contains(".")))
            {
                e.Handled = true;
                return;
            }

            // Permitir dígitos y un separador decimal
            if (!char.IsDigit(ch) && ch != ',' && ch != '.')
            {
                e.Handled = true;
            }
        }

        // Permite solo números enteros
        private void TextBoxEntero_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void TextBox_EnterAsTab(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Evita el beep y el salto de línea
                this.SelectNextControl((Control)sender, true, true, true, true);
            }
        }

        private void frmAgregarProducto_Load(object sender, EventArgs e)
        {

        }
    }
}
