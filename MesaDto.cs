namespace Comercio.NET.Mobile.Server.Models
{
    public class MesaDto
    {
        public int Id { get; set; }
        public int NumeroMesa { get; set; }
        public string Mozo { get; set; }
        public string Estado { get; set; } // "Abierta" | "Cerrada"
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal Total { get; set; }
    }

    public class MesaItemDto
    {
        public int Id { get; set; }
        public int MesaId { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class AbrirMesaRequest
    {
        public int NumeroMesa { get; set; }
        public string Mozo { get; set; }
    }

    public class AgregarItemRequest
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
    }

    public class CerrarMesaRequest
    {
        public string FormaPago { get; set; }
    }
}