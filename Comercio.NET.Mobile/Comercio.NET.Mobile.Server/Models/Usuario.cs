namespace Comercio.NET.Mobile.Server.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string? NombreCompleto { get; set; }
        public string? Rol { get; set; } // "Admin", "Cajero", "Consulta"
    }

    public class LoginRequest
    {
        public string Usuario { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Exito { get; set; }
        public string? Mensaje { get; set; }
        public string? Token { get; set; }
        public Usuario? Usuario { get; set; }
    }
}