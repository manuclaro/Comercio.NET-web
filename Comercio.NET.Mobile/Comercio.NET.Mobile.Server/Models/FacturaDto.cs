namespace Comercio.NET.Mobile.Server.Models
{
    public class FacturaDto
    {
        public int IdFactura { get; set; }
        public int NumeroRemito { get; set; }
        public string NroFactura { get; set; } = "";
        public decimal ImporteFinal { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public decimal ImporteDescuento { get; set; }
        public decimal Iva { get; set; }
        public decimal Subtotal { get; set; }
        public string Cajero { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string Hora { get; set; } = "";
        public string FormaDePago { get; set; } = "";
        public string TipoFactura { get; set; } = "";
        public string CaeNumero { get; set; } = "";
        public string CtaCteNombre { get; set; } = "";
        public string UsuarioVenta { get; set; } = "";
    }

    public class ResumenFacturasDto
    {
        public int CantidadFacturas { get; set; }
        public decimal TotalImporte { get; set; }
        public decimal TotalEfectivo { get; set; }
        public decimal TotalMercadoPago { get; set; }
        public decimal TotalDni { get; set; }
        public decimal TotalOtros { get; set; }
        public decimal TotalFacturasElectronicas { get; set; }
        public decimal TotalCtaCte { get; set; }
    }
}
