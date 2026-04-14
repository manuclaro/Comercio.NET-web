namespace Comercio.NET.Mobile.Server.Models
{
    public class DetallePagoProveedorDto
    {
        public int Id { get; set; }
        public string Proveedor { get; set; } = string.Empty; // ✅ Cambiado de NombreProveedor
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
        public string? Observaciones { get; set; } // ✅ Cambiado de Concepto
        public string? UsuarioRegistro { get; set; }
        public int NumeroCajero { get; set; }
        public int? NumeroRemito { get; set; }
        public string? NombreEquipo { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int? IdProveedor { get; set; }
        public int? CompraId { get; set; }
        public int? CtaCteId { get; set; }
        public string? Origen { get; set; }
    }
}