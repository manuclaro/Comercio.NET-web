namespace Comercio.NET.Mobile.Server.Models
{
    public class TurnoDto
    {
        public int Id { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public string Estado { get; set; } = string.Empty; // "Abierto" | "Cerrado"
    }

    public class AbrirTurnoRequest { }

    public class CerrarTurnoRequest { }
}