namespace Comercio.NET.Mobile.Server.Models
{
    public class ArqueoCajaDto
    {
        public DateTime Fecha { get; set; }
        public string? Cajero { get; set; }
        public int CantidadVentas { get; set; }
        public decimal TotalIngresos { get; set; }

        // Medios de pago según el sistema real
        public decimal DNI { get; set; }
        public decimal Efectivo { get; set; }
        public decimal MercadoPago { get; set; }
        public decimal Otro { get; set; }

        public List<FormaPagoDetalle> DetalleFormasPago { get; set; } = new();
    }

    public class FormaPagoDetalle
    {
        public string FormaPago { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
    }
}