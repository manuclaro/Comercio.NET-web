public partial class ModalCantidadForm : Form
{
    public int CantidadSeleccionada { get; private set; } = 1;

    private void btnAceptar_Click(object sender, EventArgs e)
    {
        if (int.TryParse(txtCantidad.Text, out int cantidad) && cantidad > 0)
        {
            CantidadSeleccionada = cantidad;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        else
        {
            MessageBox.Show("Ingrese una cantidad válida mayor a 0.");
        }
    }

    private void btnCancelar_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }
}