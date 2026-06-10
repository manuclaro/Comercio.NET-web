namespace Comercio.NET.Mobile.Server.Models
{
    public class UsuarioAdminDto
    {
        public int Id { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public int Nivel { get; set; }
        public string Rol { get; set; } = string.Empty;
        public int NumeroCajero { get; set; }
        public bool Activo { get; set; }
    }

    public class GuardarUsuarioDto
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public int Nivel { get; set; }
        public int NumeroCajero { get; set; }
        public string? Password { get; set; }
    }

    public class CambiarPasswordDto
    {
        public string NuevaPassword { get; set; } = string.Empty;
    }
}
