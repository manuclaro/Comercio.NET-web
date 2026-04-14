namespace Comercio.NET.Mobile.Server.Models
{
    public class VentaProductoDto
    {
        public string Codigo      { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string TipoProducto { get; set; } = string.Empty;
        public int    CantidadTotal { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal TotalRecaudado { get; set; }
        public string Mozo      { get; set; } = string.Empty;
        public string FormaPago { get; set; } = string.Empty;
        public DateTime FechaApertura { get; set; }
    }

    public class VentaProductoFiltroRequest
    {
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
    }
}