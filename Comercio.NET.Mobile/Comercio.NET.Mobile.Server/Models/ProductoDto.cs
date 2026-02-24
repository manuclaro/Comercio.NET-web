namespace Comercio.NET.Mobile.Server.Models
{
    public class ProductoDto
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Costo { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public string Rubro { get; set; }
        public string Marca { get; set; }

    }
}