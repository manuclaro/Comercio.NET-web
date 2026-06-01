namespace Comercio.NET.Mobile.Server.Models
{
    public class TurnoDto
    {
        public int Id { get; set; }
        public int NumeroCajero { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal MontoInicial { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
    }

    public class AbrirTurnoRequest
    {
        public decimal MontoInicial { get; set; }
        public int NumeroCajero { get; set; } = 1;
        public string Usuario { get; set; } = "";
    }

    public class CerrarTurnoRequest
    {
        public List<DeclaracionMedioPago> Declaraciones { get; set; } = new();
        public int CantidadVentas { get; set; }
    }

    public class DeclaracionMedioPago
    {
        public string MedioPago { get; set; } = string.Empty;
        public decimal TotalEsperado { get; set; }
        public decimal TotalDeclarado { get; set; }
    }

    public class ResumenTurnoDto
    {
        public int IdTurno { get; set; }
        public DateTime FechaApertura { get; set; }
        public decimal MontoInicial { get; set; }
        public int CantidadVentas { get; set; }
        public decimal TotalVentas { get; set; }
        public decimal TotalEfectivo { get; set; }
        public decimal TotalDNI { get; set; }
        public decimal TotalMercadoPago { get; set; }
        public decimal TotalOtro { get; set; }
        public decimal TotalCtaCte { get; set; }
        public decimal PagosProveedores { get; set; }
        public decimal EfectivoEnCaja { get; set; }
    }

    public class HistorialCierreDto
    {
        public int IdTurno { get; set; }
        public DateTime FechaCierre { get; set; }
        public string UsuarioCierre { get; set; } = string.Empty;
        public List<DetalleCierreDto> Detalles { get; set; } = new();
    }

    public class DetalleCierreDto
    {
        public string MedioPago { get; set; } = string.Empty;
        public decimal TotalEsperado { get; set; }
        public decimal TotalDeclarado { get; set; }
        public decimal Diferencia { get; set; }
        public int CantidadTransacciones { get; set; }
    }
}
