namespace Comercio.NET.Models
{
    public class LoginConfig
    {
        public bool LoginHabilitado { get; set; } = false;
        public string TipoAutenticacion { get; set; } = "Local"; // Local, Database
        public int TiempoExpiracionMinutos { get; set; } = 480; // 8 horas
        public bool RecordarUsuario { get; set; } = true;
        public bool MostrarDebugAutenticacion { get; set; } = false;
        
        // AGREGADO: Propiedades para recordar usuario
        public string UltimoUsuarioLogueado { get; set; } = "";
        public bool RecordarUltimoUsuario { get; set; } = false;
    }

    public enum NivelUsuario
    {
        Administrador = 1,
        Supervisor = 2,
        Vendedor = 3,
        Invitado = 4
    }

    public class Usuario
    {
        public int IdUsuarios { get; set; }
        public string NombreUsuario { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public NivelUsuario Nivel { get; set; }
        public int NumeroCajero { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set; }
        
        // Permisos específicos
        public bool PuedeEliminarProductos { get; set; }
        public bool PuedeEditarPrecios { get; set; }
        public bool PuedeVerReportes { get; set; }
        public bool PuedeGestionarUsuarios { get; set; }
        public bool PuedeAnularFacturas { get; set; }
        
        public string NombreCompleto => $"{Nombre} {Apellido}";
    }

    public class SesionUsuario
    {
        public Usuario Usuario { get; set; }
        public DateTime InicioSesion { get; set; }
        public DateTime UltimaActividad { get; set; }
        public bool SesionActiva { get; set; }
        
        public bool SesionExpirada => 
            DateTime.Now.Subtract(UltimaActividad).TotalMinutes > 480;
    }
}