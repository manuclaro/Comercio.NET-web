namespace Comercio.NET.Mobile.Server.Models
{
    public class MesaDto
    {
        public int Id { get; set; }
        public int NumeroMesa { get; set; }
        public string Mozo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal Total { get; set; }
    }

    public class MesaItemDto
    {
        public int Id { get; set; }
        public int MesaId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class AbrirMesaRequest
    {
        public int NumeroMesa { get; set; }
        public string Mozo { get; set; } = string.Empty;
    }

    public class AgregarItemRequest
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
    }

    public class CerrarMesaRequest
    {
        public string FormaPago { get; set; } = string.Empty;
    }

    public class ActualizarCantidadRequest
    {
        public int Cantidad { get; set; }
    }

    public class MozoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class ProductoBarDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public bool Activo { get; set; }
    }

    public class VentaMesaResumenDto
    {
        public int MesaId { get; set; }
        public int NumeroMesa { get; set; }
        public string Mozo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal Total { get; set; }
        public string FormaPago { get; set; } = string.Empty;
    }
}