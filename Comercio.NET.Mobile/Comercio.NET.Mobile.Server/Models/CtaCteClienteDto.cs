namespace Comercio.NET.Mobile.Server.Models
{
    public class ClienteCtaCteDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public decimal SaldoDeudor { get; set; }
        public decimal TotalCompras { get; set; }
        public decimal TotalPagado { get; set; }
        public DateTime? UltimaCompra { get; set; }
        public DateTime? UltimoPago { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class GuardarClienteCtaCteDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
    }

    public class MovimientoCtaCteDto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string Tipo { get; set; } = string.Empty; // "Venta" | "Pago"
        public decimal Monto { get; set; }
        public string? Referencia { get; set; }
        public string? MedioPago { get; set; }
        public string? Usuario { get; set; }
        public DateTime Fecha { get; set; }
        public string? NroFactura { get; set; }
    }

    public class RegistrarPagoCtaCteDto
    {
        public decimal Monto { get; set; }
        public string MedioPago { get; set; } = "Efectivo";
        public string? Referencia { get; set; }
        public string? Usuario { get; set; }
    }
}
