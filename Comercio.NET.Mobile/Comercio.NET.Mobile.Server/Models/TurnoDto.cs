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

    public class AbrirTurnoRequest { }

    public class CerrarTurnoRequest { }
}