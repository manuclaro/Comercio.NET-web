namespace Comercio.NET.Mobile.Server.Models
{
    public class AuditoriaDto
    {
        public int Id { get; set; }
        public string CodigoProducto { get; set; }
        public string DescripcionProducto { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public decimal TotalEliminado { get; set; }
        public int NumeroFactura { get; set; }
        public DateTime FechaEliminacion { get; set; }
        public string UsuarioEliminacion { get; set; }
        public string MotivoEliminacion { get; set; }
        public bool EsCtaCte { get; set; }
        public string NombreCtaCte { get; set; }
        public string IPUsuario { get; set; }
        public string NombreEquipo { get; set; }
        public DateTime? FechaHoraVentaOriginal { get; set; }
        public int NumeroCajero { get; set; }
        public decimal? IvaEliminado { get; set; }
        public int? CantidadOriginal { get; set; }
        public bool? EsEliminacionCompleta { get; set; }
    }
}