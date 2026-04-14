using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Comercio.NET.Servicios
{
    /// <summary>
    /// Gestiona la ejecución automática de scripts de migración de base de datos
    /// durante el proceso de actualización de la aplicación.
    ///
    /// Convención de nombres de scripts:
    ///   migrate_1.4.0.sql  ?  se ejecuta al actualizar a la versión 1.4.0
    ///   migrate_1.5.0.sql  ?  se ejecuta al actualizar a la versión 1.5.0
    ///
    /// Los scripts SQL se incluyen en el .zip del GitHub Release dentro de la
    /// carpeta "migrations\". El script .bat de actualización los ejecuta
    /// automáticamente antes de reiniciar la aplicación.
    ///
    /// Cada script debe ser idempotente (puede ejecutarse más de una vez sin error).
    /// </summary>
    public class DatabaseMigrationService
    {
        private readonly string _connectionString;
        private readonly string _migrationsFolder;

        // Nombre de la tabla que registra qué migraciones ya fueron aplicadas
        private const string MIGRATIONS_TABLE = "__MigracionesAplicadas";

        public DatabaseMigrationService(string migrationsFolder = null)
        {
            _migrationsFolder = migrationsFolder
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "migrations");

            _connectionString = ObtenerConnectionString();
        }

        /// <summary>
        /// Detecta y ejecuta todos los scripts de migración pendientes.
        /// Retorna los resultados de cada migración aplicada.
        /// </summary>
        public async Task<List<MigrationResult>> AplicarMigracionesPendientesAsync()
        {
            var resultados = new List<MigrationResult>();

            if (!Directory.Exists(_migrationsFolder))
            {
                Debug.WriteLine($"[MIGRATION] Carpeta de migraciones no encontrada: {_migrationsFolder}");
                return resultados;
            }

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                Debug.WriteLine("[MIGRATION] No se pudo obtener la cadena de conexión");
                return resultados;
            }

            // Asegurar que la tabla de control exista
            await CrearTablaMigracionesSiNoExisteAsync();

            // Obtener scripts pendientes ordenados por versión
            var scriptsPendientes = await ObtenerScriptsPendientesAsync();

            if (scriptsPendientes.Count == 0)
            {
                Debug.WriteLine("[MIGRATION] No hay migraciones pendientes");
                return resultados;
            }

            Debug.WriteLine($"[MIGRATION] {scriptsPendientes.Count} migración(es) pendiente(s)");

            foreach (var script in scriptsPendientes)
            {
                var resultado = await EjecutarScriptAsync(script);
                resultados.Add(resultado);

                if (!resultado.Exitoso)
                {
                    // Detener ante el primer error para no dejar la BD en estado inconsistente
                    Debug.WriteLine($"[MIGRATION] ? Deteniendo migraciones por error en: {script.NombreArchivo}");
                    break;
                }
            }

            return resultados;
        }

        /// <summary>
        /// Retorna los scripts .sql de la carpeta migrations que aún no fueron aplicados,
        /// ordenados por versión ascendente.
        /// </summary>
        private async Task<List<MigrationScript>> ObtenerScriptsPendientesAsync()
        {
            var aplicadas = await ObtenerMigracionesAplicadasAsync();
            var archivos = Directory.GetFiles(_migrationsFolder, "migrate_*.sql");

            var todos = archivos.Select(ruta => new MigrationScript(ruta)).ToList();

            // Log de diagnóstico: mostrar qué se descarta y por qué
            foreach (var s in todos)
            {
                if (!s.EsValido)
                    Debug.WriteLine($"[MIGRATION] IGNORADO (nombre inválido, no tiene versión X.Y.Z): {s.NombreArchivo}");
                else if (aplicadas.Contains(s.NombreArchivo))
                    Debug.WriteLine($"[MIGRATION] YA APLICADO: {s.NombreArchivo}");
                else
                    Debug.WriteLine($"[MIGRATION] PENDIENTE: {s.NombreArchivo}");
            }

            return todos
                .Where(s => s.EsValido && !aplicadas.Contains(s.NombreArchivo))
                .OrderBy(s => s.Version)
                .ToList();
        }

        /// <summary>
        /// Ejecuta un script SQL y registra el resultado en la tabla de control.
        /// </summary>
        private async Task<MigrationResult> EjecutarScriptAsync(MigrationScript script)
        {
            var resultado = new MigrationResult
            {
                NombreArchivo = script.NombreArchivo,
                Version = script.Version.ToString(),
                FechaEjecucion = DateTime.Now
            };

            Debug.WriteLine($"[MIGRATION] Aplicando: {script.NombreArchivo}");

            try
            {
                string sql = await File.ReadAllTextAsync(script.RutaCompleta);

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Dividir por GO para ejecutar en bloques (igual que SSMS)
                var bloques = sql.Split(new[] { "\nGO", "\r\nGO" },
                    StringSplitOptions.RemoveEmptyEntries);

                using var transaction = connection.BeginTransaction();
                try
                {
                    foreach (var bloque in bloques)
                    {
                        var sentencia = bloque.Trim();
                        if (string.IsNullOrWhiteSpace(sentencia)) continue;

                        using var command = new SqlCommand(sentencia, connection, transaction);
                        command.CommandTimeout = 120;
                        await command.ExecuteNonQueryAsync();
                    }

                    // Registrar como aplicada dentro de la misma transacción
                    await RegistrarMigracionAplicadaAsync(connection, transaction, script.NombreArchivo);

                    transaction.Commit();
                    resultado.Exitoso = true;
                    Debug.WriteLine($"[MIGRATION] ? {script.NombreArchivo} aplicado correctamente");
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Error = ex.Message;
                Debug.WriteLine($"[MIGRATION] ? Error en {script.NombreArchivo}: {ex.Message}");
            }

            return resultado;
        }

        private async Task CrearTablaMigracionesSiNoExisteAsync()
        {
            var sql = $@"
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = '{MIGRATIONS_TABLE}'
)
BEGIN
    CREATE TABLE [{MIGRATIONS_TABLE}] (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        NombreArchivo NVARCHAR(255) NOT NULL UNIQUE,
        FechaAplicada DATETIME      NOT NULL DEFAULT GETDATE()
    )
END";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand(sql, connection);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MIGRATION] Error creando tabla de control: {ex.Message}");
            }
        }

        private async Task<HashSet<string>> ObtenerMigracionesAplicadasAsync()
        {
            var aplicadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var sql = $"SELECT NombreArchivo FROM [{MIGRATIONS_TABLE}]";
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    aplicadas.Add(reader.GetString(0));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MIGRATION] Error leyendo migraciones aplicadas: {ex.Message}");
            }

            return aplicadas;
        }

        private async Task RegistrarMigracionAplicadaAsync(
            SqlConnection connection, SqlTransaction transaction, string nombreArchivo)
        {
            var sql = $@"
IF NOT EXISTS (SELECT 1 FROM [{MIGRATIONS_TABLE}] WHERE NombreArchivo = @nombre)
    INSERT INTO [{MIGRATIONS_TABLE}] (NombreArchivo) VALUES (@nombre)";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@nombre", nombreArchivo);
            await command.ExecuteNonQueryAsync();
        }

        private static string ObtenerConnectionString()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

                return config.GetConnectionString("DefaultConnection");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MIGRATION] Error leyendo connection string: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Representa un script de migración con su versión parseada del nombre de archivo.
    /// Formato esperado: migrate_1.4.0.sql
    /// </summary>
    public class MigrationScript
    {
        public string RutaCompleta { get; }
        public string NombreArchivo { get; }
        public Version Version { get; }
        public bool EsValido { get; }

        public MigrationScript(string rutaCompleta)
        {
            RutaCompleta = rutaCompleta;
            NombreArchivo = Path.GetFileName(rutaCompleta);

            // Parsear versión desde "migrate_1.4.0.sql" ? "1.4.0"
            var sinPrefijo = NombreArchivo
                .Replace("migrate_", "", StringComparison.OrdinalIgnoreCase)
                .Replace(".sql", "", StringComparison.OrdinalIgnoreCase);

            EsValido = Version.TryParse(sinPrefijo, out var version);
            Version = version ?? new Version(0, 0, 0);
        }
    }

    /// <summary>
    /// Resultado de la ejecución de un script de migración.
    /// </summary>
    public class MigrationResult
    {
        public string NombreArchivo { get; set; }
        public string Version { get; set; }
        public bool Exitoso { get; set; }
        public string Error { get; set; }
        public DateTime FechaEjecucion { get; set; }

        public override string ToString() =>
            Exitoso
                ? $"? {NombreArchivo} — OK"
                : $"? {NombreArchivo} — Error: {Error}";
    }
}
