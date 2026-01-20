using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Comercio.NET.Models;

namespace Comercio.NET.Services
{
    public class AuthenticationService
    {
        private static SesionUsuario _sesionActual;
        private readonly string _connectionString;
        private static LoginConfig _loginConfig;

        public AuthenticationService()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            
            _connectionString = config.GetConnectionString("DefaultConnection");
            CargarConfiguracionLogin();
        }

        public static SesionUsuario SesionActual => _sesionActual;
        public static LoginConfig ConfiguracionLogin => _loginConfig;

        private void CargarConfiguracionLogin()
        {
            try
            {
                // CORREGIDO: Primero intentar cargar desde loginconfig.json
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "loginconfig.json");
                
                if (File.Exists(configPath))
                {
                    string jsonString = File.ReadAllText(configPath);
                    _loginConfig = JsonSerializer.Deserialize<LoginConfig>(jsonString);
                    
                    if (_loginConfig != null)
                    {
                        return; // Configuración cargada exitosamente
                    }
                }

                // Si no existe loginconfig.json, cargar desde appsettings.json
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                var loginSection = config.GetSection("Login");
                _loginConfig = new LoginConfig
                {
                    LoginHabilitado = loginSection.GetValue<bool>("Habilitado", false),
                    TipoAutenticacion = loginSection.GetValue<string>("TipoAutenticacion", "Local"),
                    TiempoExpiracionMinutos = loginSection.GetValue<int>("TiempoExpiracionMinutos", 480),
                    RecordarUsuario = loginSection.GetValue<bool>("RecordarUsuario", true),
                    MostrarDebugAutenticacion = loginSection.GetValue<bool>("MostrarDebugAutenticacion", false),
                    UltimoUsuarioLogueado = "",
                    RecordarUltimoUsuario = false
                };

                // Guardar la configuración inicial en loginconfig.json
                GuardarConfiguracionLogin();
            }
            catch (Exception ex)
            {
                // Si hay error, usar configuración por defecto
                _loginConfig = new LoginConfig();
                GuardarConfiguracionLogin();
            }
        }

        // AGREGADO: Método público estático para guardar configuración
        public static void GuardarConfiguracionLogin()
        {
            try
            {
                if (_loginConfig != null)
                {
                    string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "loginconfig.json");
                    string jsonString = JsonSerializer.Serialize(_loginConfig, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    File.WriteAllText(configPath, jsonString);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al guardar configuración: {ex.Message}");
            }
        }

        public async Task<(bool exito, string mensaje, Usuario usuario)> AutenticarAsync(string nombreUsuario, string password)
        {
            // Crear archivo de log para debug
            string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_auth.txt");
            
            try
            {
                // Log inicial
                await File.AppendAllTextAsync(logFile, $"\n=== {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
                await File.AppendAllTextAsync(logFile, $"INICIO AUTENTICACIÓN\n");
                await File.AppendAllTextAsync(logFile, $"Usuario: {nombreUsuario}\n");
                await File.AppendAllTextAsync(logFile, $"Password: {password}\n");
                await File.AppendAllTextAsync(logFile, $"Config debug: {_loginConfig?.MostrarDebugAutenticacion}\n");

                // PASO 1: Verificar si existe la tabla Usuarios
                bool tablaExiste = await VerificarTablaUsuariosAsync();
                
                await File.AppendAllTextAsync(logFile, $"Tabla existe: {tablaExiste}\n");
                
                if (!tablaExiste)
                {
                    await File.AppendAllTextAsync(logFile, "Usando usuarios por defecto - tabla no existe\n");
                    return AutenticarUsuarioDefecto(nombreUsuario, password);
                }

                // PASO 2: Verificar si hay usuarios en la tabla
                int cantidadUsuarios = await ContarUsuariosAsync();
                
                await File.AppendAllTextAsync(logFile, $"Cantidad usuarios: {cantidadUsuarios}\n");
                
                if (cantidadUsuarios == 0)
                {
                    await File.AppendAllTextAsync(logFile, "Usando usuarios por defecto - tabla vacía\n");
                    return AutenticarUsuarioDefecto(nombreUsuario, password);
                }

                // PASO 3: Intentar autenticación con base de datos
                await File.AppendAllTextAsync(logFile, "Intentando autenticación con BD\n");
                var (exitoBD, mensajeBD, usuarioBD) = await AutenticarConBaseDatosAsync(nombreUsuario, password, logFile);
                
                if (exitoBD)
                {
                    await File.AppendAllTextAsync(logFile, "BD exitosa\n");
                    return (exitoBD, mensajeBD, usuarioBD);
                }

                // PASO 4: Si falló la BD, intentar con usuarios por defecto como backup
                await File.AppendAllTextAsync(logFile, "BD falló, usando usuarios por defecto como backup\n");
                return AutenticarUsuarioDefecto(nombreUsuario, password);
            }
            catch (Exception ex)
            {
                // En caso de error, intentar usuarios por defecto
                await File.AppendAllTextAsync(logFile, $"Error en autenticación: {ex.Message}\n");
                return AutenticarUsuarioDefecto(nombreUsuario, password);
            }
        }

        private async Task<bool> VerificarTablaUsuariosAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var query = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = 'Usuarios'";

                using var cmd = new SqlCommand(query, connection);
                connection.Open();
                int count = (int)await cmd.ExecuteScalarAsync();
                return count > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task<int> ContarUsuariosAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var query = "SELECT COUNT(*) FROM Usuarios";

                using var cmd = new SqlCommand(query, connection);
                connection.Open();
                return (int)await cmd.ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        private (bool exito, string mensaje, Usuario usuario) AutenticarUsuarioDefecto(string nombreUsuario, string password)
        {
            // Usuarios por defecto para desarrollo/pruebas
            var usuariosDefecto = new Dictionary<string, (string password, NivelUsuario nivel, string nombre, string apellido)>
            {
                { "admin", ("1506", NivelUsuario.Administrador, "Administrador", "Sistema") },
                //{ "supervisor", ("supervisor", NivelUsuario.Supervisor, "Supervisor", "Sistema") },
                //{ "vendedor", ("vendedor", NivelUsuario.Vendedor, "Vendedor", "Sistema") },
                //{ "invitado", ("invitado", NivelUsuario.Invitado, "Invitado", "Sistema") }
            };

            if (usuariosDefecto.ContainsKey(nombreUsuario.ToLower()) && 
                usuariosDefecto[nombreUsuario.ToLower()].password == password)
            {
                var datosUsuario = usuariosDefecto[nombreUsuario.ToLower()];
                
                var usuario = new Usuario
                {
                    IdUsuarios = 1,
                    NombreUsuario = nombreUsuario,
                    Nombre = datosUsuario.nombre,
                    Apellido = datosUsuario.apellido,
                    Email = $"{nombreUsuario}@comercio.net",
                    Nivel = datosUsuario.nivel,
                    NumeroCajero = 1,
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    UltimoAcceso = DateTime.Now,
                    PuedeEliminarProductos = datosUsuario.nivel == NivelUsuario.Administrador,
                    PuedeEditarPrecios = datosUsuario.nivel == NivelUsuario.Administrador || datosUsuario.nivel == NivelUsuario.Supervisor,
                    PuedeVerReportes = datosUsuario.nivel != NivelUsuario.Invitado,
                    PuedeGestionarUsuarios = datosUsuario.nivel == NivelUsuario.Administrador,
                    PuedeAnularFacturas = datosUsuario.nivel == NivelUsuario.Administrador || datosUsuario.nivel == NivelUsuario.Supervisor
                };

                // Crear sesión
                _sesionActual = new SesionUsuario
                {
                    Usuario = usuario,
                    InicioSesion = DateTime.Now,
                    UltimaActividad = DateTime.Now,
                    SesionActiva = true
                };

                return (true, "Autenticación exitosa (usuario de prueba)", usuario);
            }

            return (false, "Usuario o contraseña incorrectos", null);
        }

        private async Task<(bool exito, string mensaje, Usuario usuario)> AutenticarConBaseDatosAsync(string nombreUsuario, string password, string logFile)
        {
            await File.AppendAllTextAsync(logFile, "=== AUTENTICACIÓN CON BASE DE DATOS ===\n");
            
            string passwordHash = ComputeHash(password);
            
            await File.AppendAllTextAsync(logFile, $"Hash calculado: {passwordHash}\n");
            
            using var connection = new SqlConnection(_connectionString);
            
            // Consulta de debugging para ver qué hay en la base de datos
            var debugQuery = @"
                SELECT NombreUsuario, PasswordHash, Activo 
                FROM Usuarios 
                WHERE NombreUsuario = @nombreUsuario";

            using var debugCmd = new SqlCommand(debugQuery, connection);
            debugCmd.Parameters.AddWithValue("@nombreUsuario", nombreUsuario);

            connection.Open();
            
            // DEBUGGING: Ver qué usuario existe en la BD
            using var debugReader = await debugCmd.ExecuteReaderAsync();
            if (debugReader.Read())
            {
                string hashEnBD = debugReader.GetString("PasswordHash");
                bool activo = debugReader.GetBoolean("Activo");
                bool coinciden = passwordHash == hashEnBD;
                
                // Log detallado (SOLO A ARCHIVO - SIN MESSAGEBOX)
                await File.AppendAllTextAsync(logFile, $"Usuario encontrado en BD\n");
                await File.AppendAllTextAsync(logFile, $"Hash en BD: {hashEnBD}\n");
                await File.AppendAllTextAsync(logFile, $"Activo: {activo}\n");
                await File.AppendAllTextAsync(logFile, $"Hashes coinciden: {coinciden}\n");
                await File.AppendAllTextAsync(logFile, $"Config debug: {_loginConfig?.MostrarDebugAutenticacion}\n");
                
                await File.AppendAllTextAsync(logFile, "Debug completado - solo logging a archivo\n");
            }
            else
            {
                await File.AppendAllTextAsync(logFile, "Usuario NO encontrado en BD\n");
            }
            debugReader.Close();

            // Consulta original para autenticación
            var query = @"
                SELECT IdUsuarios, NombreUsuario, Nombre, Apellido, Email, PasswordHash, 
                       Nivel, NumeroCajero, Activo, FechaCreacion, UltimoAcceso,
                       PuedeEliminarProductos, PuedeEditarPrecios, PuedeVerReportes, 
                       PuedeGestionarUsuarios, PuedeAnularFacturas
                FROM Usuarios 
                WHERE NombreUsuario = @nombreUsuario AND PasswordHash = @passwordHash AND Activo = 1";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@nombreUsuario", nombreUsuario);
            cmd.Parameters.AddWithValue("@passwordHash", passwordHash);

            using var reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                await File.AppendAllTextAsync(logFile, "Autenticación BD EXITOSA\n");
                
                var usuario = new Usuario
                {
                    IdUsuarios = reader.GetInt32("IdUsuarios"),
                    NombreUsuario = reader.GetString("NombreUsuario"),
                    Nombre = reader.GetString("Nombre"),
                    Apellido = reader.GetString("Apellido"),
                    Email = reader.IsDBNull("Email") ? "" : reader.GetString("Email"),
                    PasswordHash = reader.GetString("PasswordHash"),
                    Nivel = (NivelUsuario)reader.GetInt32("Nivel"),
                    NumeroCajero = reader.GetInt32("NumeroCajero"),
                    Activo = reader.GetBoolean("Activo"),
                    FechaCreacion = reader.GetDateTime("FechaCreacion"),
                    UltimoAcceso = reader.IsDBNull("UltimoAcceso") ? null : reader.GetDateTime("UltimoAcceso"),
                    PuedeEliminarProductos = reader.GetBoolean("PuedeEliminarProductos"),
                    PuedeEditarPrecios = reader.GetBoolean("PuedeEditarPrecios"),
                    PuedeVerReportes = reader.GetBoolean("PuedeVerReportes"),
                    PuedeGestionarUsuarios = reader.GetBoolean("PuedeGestionarUsuarios"),
                    PuedeAnularFacturas = reader.GetBoolean("PuedeAnularFacturas")
                };

                // Actualizar último acceso
                reader.Close();
                await ActualizarUltimoAccesoAsync(usuario.IdUsuarios);

                // Crear sesión
                _sesionActual = new SesionUsuario
                {
                    Usuario = usuario,
                    InicioSesion = DateTime.Now,
                    UltimaActividad = DateTime.Now,
                    SesionActiva = true
                };

                return (true, "Autenticación exitosa (base de datos)", usuario);
            }
            else
            {
                await File.AppendAllTextAsync(logFile, "Autenticación BD FALLÓ\n");
                return (false, "Usuario o contraseña incorrectos (BD)", null);
            }
        }

        public async Task<bool> ActualizarPasswordUsuarioAsync(string nombreUsuario, string nuevaPassword)
        {
            try
            {
                string nuevoHash = ComputeHash(nuevaPassword);
                
                using var connection = new SqlConnection(_connectionString);
                var query = "UPDATE Usuarios SET PasswordHash = @passwordHash WHERE NombreUsuario = @nombreUsuario";
                
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@passwordHash", nuevoHash);
                cmd.Parameters.AddWithValue("@nombreUsuario", nombreUsuario);

                connection.Open();
                int filasAfectadas = await cmd.ExecuteNonQueryAsync();
                
                return filasAfectadas > 0;
            }
            catch
            {
                return false;
            }
        }

        public void CerrarSesion()
        {
            if (_sesionActual != null)
            {
                _sesionActual.SesionActiva = false;
                _sesionActual = null;
            }
        }

        public void ActualizarActividad()
        {
            if (_sesionActual != null && _sesionActual.SesionActiva)
            {
                _sesionActual.UltimaActividad = DateTime.Now;
            }
        }

        public bool ValidarSesion()
        {
            if (_sesionActual == null || !_sesionActual.SesionActiva)
                return false;

            if (_sesionActual.SesionExpirada)
            {
                CerrarSesion();
                return false;
            }

            ActualizarActividad();
            return true;
        }

        public bool TienePermiso(string accion)
        {
            if (!ValidarSesion())
                return false;

            var usuario = _sesionActual.Usuario;

            return accion.ToLower() switch
            {
                "eliminar_productos" => usuario.PuedeEliminarProductos,
                "editar_precios" => usuario.PuedeEditarPrecios,
                "ver_reportes" => usuario.PuedeVerReportes,
                "gestionar_usuarios" => usuario.PuedeGestionarUsuarios,
                "anular_facturas" => usuario.PuedeAnularFacturas,
                _ => usuario.Nivel == NivelUsuario.Administrador
            };
        }

        private async Task ActualizarUltimoAccesoAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var query = "UPDATE Usuarios SET UltimoAcceso = @ultimoAcceso WHERE IdUsuarios = @IdUsuarios";
                
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ultimoAcceso", DateTime.Now);
                cmd.Parameters.AddWithValue("@IdUsuarios", userId);

                connection.Open();
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Log error pero no fallar la autenticación
            }
        }

        private string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input + "ComercioNET_Salt"));
            return Convert.ToBase64String(hashedBytes);
        }

        public async Task<bool> CrearUsuarioAsync(Usuario usuario, string password)
        {
            try
            {
                // Verificar si la tabla existe primero
                bool tablaExiste = await VerificarTablaUsuariosAsync();
                if (!tablaExiste)
                {
                    await CrearTablaUsuariosAsync();
                }

                usuario.PasswordHash = ComputeHash(password);
                usuario.FechaCreacion = DateTime.Now;
                usuario.Activo = true;

                using var connection = new SqlConnection(_connectionString);
                var query = @"
                    INSERT INTO Usuarios (NombreUsuario, Nombre, Apellido, Email, PasswordHash, 
                                        Nivel, NumeroCajero, Activo, FechaCreacion,
                                        PuedeEliminarProductos, PuedeEditarPrecios, PuedeVerReportes,
                                        PuedeGestionarUsuarios, PuedeAnularFacturas)
                    VALUES (@nombreUsuario, @nombre, @apellido, @email, @passwordHash,
                           @nivel, @numeroCajero, @activo, @fechaCreacion,
                           @puedeEliminarProductos, @puedeEditarPrecios, @puedeVerReportes,
                           @puedeGestionarUsuarios, @puedeAnularFacturas)";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@nombreUsuario", usuario.NombreUsuario);
                cmd.Parameters.AddWithValue("@nombre", usuario.Nombre);
                cmd.Parameters.AddWithValue("@apellido", usuario.Apellido);
                cmd.Parameters.AddWithValue("@email", usuario.Email ?? "");
                cmd.Parameters.AddWithValue("@passwordHash", usuario.PasswordHash);
                cmd.Parameters.AddWithValue("@nivel", (int)usuario.Nivel);
                cmd.Parameters.AddWithValue("@numeroCajero", usuario.NumeroCajero);
                cmd.Parameters.AddWithValue("@activo", usuario.Activo);
                cmd.Parameters.AddWithValue("@fechaCreacion", usuario.FechaCreacion);
                cmd.Parameters.AddWithValue("@puedeEliminarProductos", usuario.PuedeEliminarProductos);
                cmd.Parameters.AddWithValue("@puedeEditarPrecios", usuario.PuedeEditarPrecios);
                cmd.Parameters.AddWithValue("@puedeVerReportes", usuario.PuedeVerReportes);
                cmd.Parameters.AddWithValue("@puedeGestionarUsuarios", usuario.PuedeGestionarUsuarios);
                cmd.Parameters.AddWithValue("@puedeAnularFacturas", usuario.PuedeAnularFacturas);

                connection.Open();
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task CrearTablaUsuariosAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var query = @"
                    CREATE TABLE Usuarios (
                        IdUsuarios INT IDENTITY(1,1) PRIMARY KEY,
                        NombreUsuario NVARCHAR(50) NOT NULL UNIQUE,
                        Nombre NVARCHAR(100) NOT NULL,
                        Apellido NVARCHAR(100) NOT NULL,
                        Email NVARCHAR(200),
                        PasswordHash NVARCHAR(200) NOT NULL,
                        Nivel INT NOT NULL DEFAULT 1,
                        NumeroCajero INT NOT NULL DEFAULT 1,
                        Activo BIT NOT NULL DEFAULT 1,
                        FechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),
                        UltimoAcceso DATETIME,
                        PuedeEliminarProductos BIT NOT NULL DEFAULT 0,
                        PuedeEditarPrecios BIT NOT NULL DEFAULT 0,
                        PuedeVerReportes BIT NOT NULL DEFAULT 1,
                        PuedeGestionarUsuarios BIT NOT NULL DEFAULT 0,
                        PuedeAnularFacturas BIT NOT NULL DEFAULT 0
                    )";

                using var cmd = new SqlCommand(query, connection);
                connection.Open();
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Si hay error al crear la tabla, continuar con usuarios por defecto
            }
        }

        // Agregar este método a tu servicio de AFIP para diagnóstico
        public async Task<bool> VerificarEstadoServicioAfipAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                
                // URL del servicio WSAA de homologación
                string urlHomologacion = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms?wsdl";
                
                var response = await client.GetAsync(urlHomologacion);
                
                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Servicio AFIP disponible");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error AFIP: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error conectividad AFIP: {ex.Message}");
                return false;
            }
        }
    }
}