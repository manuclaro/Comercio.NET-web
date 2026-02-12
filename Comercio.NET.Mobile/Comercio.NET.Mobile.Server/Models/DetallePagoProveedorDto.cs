namespace Comercio.NET.Mobile.Server.Models
{
    public class DetallePagoProveedorDto
    {
        public int Id { get; set; }
        public string NombreProveedor { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string FormaPago { get; set; } = string.Empty;
        public DateTime FechaPago { get; set; }
        public string? Concepto { get; set; }
        public string? NumeroComprobante { get; set; }
        public string? UsuarioRegistro { get; set; }
    }
}