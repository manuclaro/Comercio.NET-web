namespace Comercio.NET.Mobile.Server.Models
{
    public class EstadisticaVentaDto
    {
        public string Rubro { get; set; } = string.Empty;
        public decimal TotalVentas { get; set; }
        public int CantidadProductos { get; set; }
    }
}