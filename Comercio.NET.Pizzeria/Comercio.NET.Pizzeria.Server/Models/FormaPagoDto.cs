namespace Comercio.NET.Pizzeria.Server.Models
{
    public class FormaPagoDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
