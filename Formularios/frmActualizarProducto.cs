using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;

namespace Comercio.NET.Formularios
{
    public partial class frmActualizarProducto : Form
    {
        public frmActualizarProducto()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.KeyDown += frmActualizarProducto_KeyDown;
            // Asociar el evento KeyDown a los TextBox para tabular con ENTER.
            AsociarEventosEnter();
            // Asociar los eventos KeyPress para validar la entrada numérica.
            txtStockActual.KeyPress += txtStockActual_KeyPress;
            txtNuevoCosto.KeyPress += txtNuevoCosto_KeyPress;
            txtNuevoPorcentaje.KeyPress += txtNuevoPorcentaje_KeyPress;
            txtValorVenta.KeyPress += txtNuevoCosto_KeyPress;
        }

        private void AsociarEventosEnter()
        {
            // Lista de controles que deben permitir tabular con ENTER
            txtCodigo.KeyDown += TextBox_KeyDown;
            txtNombre.KeyDown += TextBox_KeyDown;
            txtNuevoCosto.KeyDown += TextBox_KeyDown;
            txtNuevoPorcentaje.KeyDown += TextBox_KeyDown;
            txtValorVenta.KeyDown += TextBox_KeyDown;
            txtStockActual.KeyDown += TextBox_KeyDown;
            // Si existe txtMarca, se añade también
            if (this.Controls.ContainsKey("txtMarca"))
            {
                ((TextBox)this.Controls["txtMarca"]).KeyDown += TextBox_KeyDown;
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // evita el beep
                this.SelectNextControl((Control)sender, true, true, true, true);
            }
        }

        // Validación para el TextBox de stock: solo dígitos y limitado a 4.
        private void txtStockActual_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
                return;
            }

            // Limitar a 4 dígitos.
            if (!char.IsControl(e.KeyChar) && tb.Text.Length >= 4 && tb.SelectionLength == 0)
            {
                e.Handled = true;
            }
        }

        // Validación para el TextBox de costo: máximo 6 dígitos enteros y 2 decimales
        private void txtNuevoCosto_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Reemplazar punto por coma.
            if (e.KeyChar == '.')
                e.KeyChar = ',';

            TextBox tb = sender as TextBox;
            string text = tb.Text;

            // Permitir teclas de control.
            if (char.IsControl(e.KeyChar))
                return;

            // Permitir solo dígitos y una coma.
            if (char.IsDigit(e.KeyChar))
            {
                if (text.Contains(","))
                {
                    int index = text.IndexOf(",");
                    // Si el cursor está en la parte decimal, se permite máximo 2 decimales.
                    if (tb.SelectionStart > index)
                    {
                        string decimalPart = text.Substring(index + 1);
                        if (decimalPart.Length >= 2 && tb.SelectionLength == 0)
                            e.Handled = true;
                    }
                    // Si el cursor está en la parte entera, se permite máximo 6 dígitos.
                    else
                    {
                        string integerPart = text.Substring(0, index);
                        if (integerPart.Length >= 6 && tb.SelectionLength == 0)
                            e.Handled = true;
                    }
                }
                else
                {
                    // Sin coma, se permite máximo 6 dígitos enteros.
                    if (text.Length >= 6 && tb.SelectionLength == 0)
                        e.Handled = true;
                }
                return;
            }
            if (e.KeyChar == ',')
            {
                // Permitir coma si aún no existe.
                if (text.Contains(","))
                    e.Handled = true;
                return;
            }
            // Otros caracteres no permitidos.
            e.Handled = true;
        }

        // Validación modificada para el TextBox de porcentaje: máximo 3 dígitos enteros y 2 decimales.
        private void txtNuevoPorcentaje_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Reemplazar punto por coma.
            if (e.KeyChar == '.')
                e.KeyChar = ',';

            TextBox tb = sender as TextBox;
            string text = tb.Text;

            if (char.IsControl(e.KeyChar))
                return;

            if (char.IsDigit(e.KeyChar))
            {
                if (text.Contains(","))
                {
                    int index = text.IndexOf(",");
                    // Si el cursor está en la parte decimal, se permite máximo 2 decimales.
                    if (tb.SelectionStart > index)
                    {
                        string decimalPart = text.Substring(index + 1);
                        if (decimalPart.Length >= 2 && tb.SelectionLength == 0)
                            e.Handled = true;
                    }
                    else
                    {
                        // Se permite hasta 3 dígitos en la parte entera.
                        string integerPart = text.Substring(0, index);
                        if (integerPart.Length >= 3 && tb.SelectionLength == 0)
                            e.Handled = true;
                    }
                }
                else
                {
                    // Sin coma, se permiten máximo 3 dígitos enteros.
                    if (text.Length >= 3 && tb.SelectionLength == 0)
                        e.Handled = true;
                }
                return;
            }
            if (e.KeyChar == ',')
            {
                if (text.Contains(","))
                    e.Handled = true;
                return;
            }
            e.Handled = true;
        }

        // En el botón Buscar, se formatean los valores para mostrar 2 decimales.
        private void btnBuscar_Click(object sender, EventArgs e)
        {
            string codigo = txtCodigo.Text.Trim();
            if (string.IsNullOrEmpty(codigo))
            {
                MessageBox.Show("Ingrese un código de producto.", "Búsqueda", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // MODIFICADO: Agregar columna IVA a la consulta
                    string query = @"SELECT descripcion, marca, costo, porcentaje, precio, cantidad, iva 
                                     FROM Productos
                                     WHERE codigo = @codigo";
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@codigo", codigo);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtNombre.Text = reader["descripcion"].ToString();
                                if (this.Controls.ContainsKey("txtMarca"))
                                    ((TextBox)this.Controls["txtMarca"]).Text = reader["marca"].ToString();

                                txtNuevoCosto.Text = Convert.ToDecimal(reader["costo"]).ToString("F2");
                                txtNuevoPorcentaje.Text = Convert.ToDecimal(reader["porcentaje"]).ToString("F2");
                                txtValorVenta.Text = Convert.ToDecimal(reader["precio"]).ToString("F2");
                                txtStockActual.Text = reader["cantidad"].ToString();
                                
                                // NUEVO: Cargar valor de IVA
                                if (this.Controls.ContainsKey("txtIva"))
                                    ((TextBox)this.Controls["txtIva"]).Text = Convert.ToDecimal(reader["iva"]).ToString("F2");

                                txtStockActual.Focus();
                                CalcularVenta(null, null);
                            }
                            else
                            {
                                MessageBox.Show("Producto no encontrado.", "Búsqueda", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                LimpiarControles();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar el producto: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CalcularVenta(object sender, EventArgs e)
        {
            if (decimal.TryParse(txtNuevoCosto.Text, out decimal costo) &&
                decimal.TryParse(txtNuevoPorcentaje.Text, out decimal porcentaje))
            {
                decimal valorVenta = costo + ((costo * porcentaje) / 100);
                txtValorVenta.Text = valorVenta.ToString("F2");
            }
            else
            {
                txtValorVenta.Clear();
            }
        }

        // Botón Aplicar: Actualiza el producto en la base de datos y limpia los controles
        private void btnAplicar_Click(object sender, EventArgs e)
        {
            string codigo = txtCodigo.Text.Trim();
            if (string.IsNullOrEmpty(codigo))
            {
                MessageBox.Show("Código de producto no válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            if (!decimal.TryParse(txtNuevoCosto.Text, out decimal nuevoCosto) ||
                !decimal.TryParse(txtNuevoPorcentaje.Text, out decimal nuevoPorcentaje) ||
                !int.TryParse(txtStockActual.Text, out int nuevoStock))
            {
                MessageBox.Show("Revise los valores ingresados.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal nuevoPrecio = nuevoCosto + ((nuevoCosto * nuevoPorcentaje) / 100);

            // NUEVO: Obtener valor de IVA si existe el control
            decimal nuevoIva = 21.00m; // Valor por defecto
            if (this.Controls.ContainsKey("txtIva"))
            {
                decimal.TryParse(((TextBox)this.Controls["txtIva"]).Text, out nuevoIva);
            }

            try
            {
                var config = new ConfigurationBuilder()
                                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                                .AddJsonFile("appsettings.json")
                                .Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // MODIFICADO: Agregar columna IVA al UPDATE
                    string updateQuery = @"UPDATE Productos
                                           SET cantidad = @nuevoStock,
                                               costo = @nuevoCosto,
                                               porcentaje = @nuevoPorcentaje,
                                               precio = @nuevoPrecio,
                                               iva = @nuevoIva
                                           WHERE codigo = @codigo";
                    using (var cmd = new SqlCommand(updateQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@nuevoStock", nuevoStock);
                        cmd.Parameters.AddWithValue("@nuevoCosto", nuevoCosto);
                        cmd.Parameters.AddWithValue("@nuevoPorcentaje", nuevoPorcentaje);
                        cmd.Parameters.AddWithValue("@nuevoPrecio", nuevoPrecio);
                        cmd.Parameters.AddWithValue("@nuevoIva", nuevoIva);
                        cmd.Parameters.AddWithValue("@codigo", codigo);

                        int affected = cmd.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            MessageBox.Show("Datos actualizados correctamente.", "Actualización", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LimpiarControles();
                            txtCodigo.Focus();
                        }
                        else
                        {
                            MessageBox.Show("No se actualizó ningún registro.", "Actualización", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar el producto: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LimpiarControles()
        {
            txtCodigo.Clear();
            txtNombre.Clear();
            if (this.Controls.ContainsKey("txtMarca"))
                ((TextBox)this.Controls["txtMarca"]).Clear();
            txtNuevoCosto.Clear();
            txtNuevoPorcentaje.Clear();
            txtValorVenta.Clear();
            txtStockActual.Clear();
            // NUEVO: Limpiar campo IVA si existe
            if (this.Controls.ContainsKey("txtIva"))
                ((TextBox)this.Controls["txtIva"]).Clear();
        }

        // Botón Cerrar: Antes de cerrar, se busca ejecutar el botón "Refrescar" del formulario Productos.
        private void btnCerrar_Click(object sender, EventArgs e)
        {
            // Verificar si hay un producto cargado (por ejemplo, si txtCodigo no está vacío).
            if (!string.IsNullOrEmpty(txtCodigo.Text.Trim()))
            {
                DialogResult result = MessageBox.Show("¿Desea guardar los cambios?", "Confirmar", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    // Ejecuta el botón Aplicar para guardar los cambios.
                    btnAplicar.PerformClick();
                }
                else if (result == DialogResult.Cancel)
                {
                    // Si el usuario cancela, no se cierra el modal.
                    return;
                }
                // Si el usuario respondió No, continúa cerrando sin guardar.
            }

            // Actualizar la grilla en el formulario principal.
            var mainForm = Application.OpenForms.OfType<Productos>().FirstOrDefault();
            mainForm?.RefrescarProductos();
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void frmActualizarProducto_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                btnCerrar.PerformClick(); // Simula el clic en el botón Cerrar.
                e.SuppressKeyPress = true; // Evita el sonido de beep.
            }
        }
    }
}