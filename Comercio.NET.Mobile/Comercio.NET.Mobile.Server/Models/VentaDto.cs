namespace Comercio.NET.Mobile.Server.Models
{
    public class VentaDto
    {
        public int Id { get; set; }
        public int NroFactura { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
        public decimal PorcentajeIva { get; set; }
        public bool EsOferta { get; set; }
        public string NombreOferta { get; set; }
        public string FormaPago { get; set; }
        public string TipoFactura { get; set; }
        public DateTime Fecha { get; set; }
        public string Hora { get; set; }
        public bool EsCtaCte { get; set; }
        public string NombreCtaCte { get; set; }
        public string UsuarioVenta { get; set; }
        public int NumeroCajero { get; set; }
    }

    public class ResumenVentasDto
    {
        public decimal TotalVendido { get; set; }
        public int CantidadTransacciones { get; set; }
        public int CantidadProductos { get; set; }
        public decimal TotalEfectivo { get; set; }
        public decimal TotalMercadoPago { get; set; }
        public decimal TotalDni { get; set; }
        public decimal TotalCtaCte { get; set; }
        public decimal TotalOtros { get; set; }
    }
}