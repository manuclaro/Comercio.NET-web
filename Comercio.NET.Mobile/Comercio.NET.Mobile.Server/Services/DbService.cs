using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data;
using System.Data.Common;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    /// <summary>
    /// Abstrae el acceso a la base de datos permitiendo dos modos:
    /// - SqlBridge: envía queries HTTP al servicio SqlBridge (para acceso remoto / fuera de red local)
    /// - Directo:   conecta directamente a SQL Server (cuando la API corre en la misma red del local)
    /// Se elige automáticamente según la configuración:
    ///   - Si existe ConnectionStrings:DefaultConnection ? modo directo
    ///   - Si existe SqlBridgeUrl o SQL_BRIDGE_URL        ? modo SqlBridge
    /// </summary>
    public class DbService
    {
        public enum Modo { SqlBridge, Directo }

        private readonly Modo _modo;
        private readonly string? _connectionString;
        private readonly string? _sqlBridgeUrl;
        private readonly ILogger<DbService> _logger;
        private readonly HttpClient _httpClient;

        public DbService(IConfiguration configuration, ILogger<DbService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger     = logger;
            _httpClient = httpClientFactory.CreateClient();

            // Preferir conexión directa si hay un ConnectionString configurado
            var connStr = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(connStr))
            {
                _modo             = Modo.Directo;
                _connectionString = connStr;
                // Detectar si es Postgres por el formato del connection string
                UsaPostgres = connStr.Contains("Host=", StringComparison.OrdinalIgnoreCase)
                           || connStr.Contains("Port=5432", StringComparison.OrdinalIgnoreCase);
                logger.LogInformation("[DbService] Modo: DIRECTO ({Motor})", UsaPostgres ? "PostgreSQL" : "SQL Server");
                return;
            }

            // Sino, usar SqlBridge
            var bridgeUrl = Environment.GetEnvironmentVariable("SQL_BRIDGE_URL")
                         ?? configuration["SqlBridgeUrl"];

            if (string.IsNullOrWhiteSpace(bridgeUrl))
                throw new InvalidOperationException(
                    "No se encontró configuración de base de datos. " +
                    "Configure 'ConnectionStrings:DefaultConnection' para conexión directa " +
                    "o 'SqlBridgeUrl' para modo remoto.");

            _modo         = Modo.SqlBridge;
            _sqlBridgeUrl = bridgeUrl;
            // En modo SqlBridge el motor se configura en el SqlBridge mismo.
            // Leemos DbEngine de la config local de la API para adaptar las queries.
            var engineCfg = (configuration["DbEngine"] ?? "").ToLowerInvariant();
            UsaPostgres   = engineCfg is "postgres" or "postgresql";
            logger.LogInformation("[DbService] Modo: SQLBRIDGE ({Url}) Motor: {Motor}",
                _sqlBridgeUrl, UsaPostgres ? "PostgreSQL" : "SQL Server");
        }

        /// <summary>
        /// True si la BD de destino es PostgreSQL.
        /// Usado por los servicios para adaptar la sintaxis de queries (RETURNING vs OUTPUT INSERTED).
        /// </summary>
        public bool UsaPostgres { get; }

        /// <summary>Ejecuta un SELECT y devuelve las filas como listas de JsonElement.</summary>
        public async Task<List<List<JsonElement>>> QueryAsync(string sql, Dictionary<string, object?> parameters)
        {
            if (_modo == Modo.Directo)
                return await QueryDirectoAsync(sql, parameters);
            return await QueryBridgeAsync(sql, parameters);
        }

        /// <summary>Ejecuta INSERT / UPDATE / DELETE.</summary>
        public async Task ExecuteAsync(string sql, Dictionary<string, object?> parameters)
        {
            if (_modo == Modo.Directo)
                await ExecuteDirectoAsync(sql, parameters);
            else
                await ExecuteBridgeAsync(sql, parameters);
        }

        /// <summary>Ejecuta un comando y devuelve el primer valor de la primera fila.</summary>
        public async Task<T?> ScalarAsync<T>(string sql, Dictionary<string, object?> parameters)
        {
            if (_modo == Modo.Directo)
                return await ScalarDirectoAsync<T>(sql, parameters);
            return await ScalarBridgeAsync<T>(sql, parameters);
        }

        // ?? Modo Directo ????????????????????????????????????????????????????????

        private DbConnection CrearConexionDirecta()
        {
            if (UsaPostgres)
                return new NpgsqlConnection(_connectionString);
            return new SqlConnection(_connectionString);
        }

        private static DbCommand BuildCommand(DbConnection conn, string sql, Dictionary<string, object?> parameters)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandTimeout = 30;
            foreach (var p in parameters)
            {
                var param = cmd.CreateParameter();
                // PostgreSQL usa @param, SQL Server también; compatibles.
                param.ParameterName = p.Key;
                param.Value = p.Value ?? DBNull.Value;
                cmd.Parameters.Add(param);
            }
            return cmd;
        }

        private async Task<List<List<JsonElement>>> QueryDirectoAsync(string sql, Dictionary<string, object?> parameters)
        {
            var result = new List<List<JsonElement>>();
            await using var conn = CrearConexionDirecta();
            await conn.OpenAsync();
            await using var cmd    = BuildCommand(conn, sql, parameters);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new List<JsonElement>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row.Add(ToJsonElement(val));
                }
                result.Add(row);
            }
            return result;
        }

        private async Task ExecuteDirectoAsync(string sql, Dictionary<string, object?> parameters)
        {
            await using var conn = CrearConexionDirecta();
            await conn.OpenAsync();
            await using var cmd = BuildCommand(conn, sql, parameters);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<T?> ScalarDirectoAsync<T>(string sql, Dictionary<string, object?> parameters)
        {
            await using var conn = CrearConexionDirecta();
            await conn.OpenAsync();
            await using var cmd = BuildCommand(conn, sql, parameters);
            var val = await cmd.ExecuteScalarAsync();
            if (val == null || val == DBNull.Value) return default;
            return (T)Convert.ChangeType(val, typeof(T));
        }

        // ?? Modo SqlBridge ??????????????????????????????????????????????????????

        private async Task<List<List<JsonElement>>> QueryBridgeAsync(string sql, Dictionary<string, object?> parameters)
        {
            var payload  = new { query = sql, parameters };
            using var res = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);
            await EnsureSuccessAsync(res, "/query");

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var result = new List<List<JsonElement>>();
            foreach (var row in doc.RootElement.GetProperty("data").EnumerateArray())
            {
                var cols = new List<JsonElement>();
                foreach (var col in row.EnumerateArray()) cols.Add(col.Clone());
                result.Add(cols);
            }
            return result;
        }

        private async Task ExecuteBridgeAsync(string sql, Dictionary<string, object?> parameters)
        {
            var payload  = new { query = sql, parameters };
            using var res = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/execute", payload);
            await EnsureSuccessAsync(res, "/execute");
        }

        private async Task<T?> ScalarBridgeAsync<T>(string sql, Dictionary<string, object?> parameters)
        {
            var payload  = new { query = sql, parameters };
            using var res = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/scalar", payload);
            await EnsureSuccessAsync(res, "/scalar");

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var val = doc.RootElement.GetProperty("value");
            if (val.ValueKind == JsonValueKind.Null) return default;
            return val.Deserialize<T>();
        }

        // ?? Helpers ?????????????????????????????????????????????????????????????

        private async Task EnsureSuccessAsync(HttpResponseMessage res, string endpoint)
        {
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                throw new Exception($"SqlBridge {endpoint} error {res.StatusCode}: {body}");
            }
        }

        private static JsonElement ToJsonElement(object? val)
        {
            var json = JsonSerializer.Serialize(val);
            return JsonDocument.Parse(json).RootElement.Clone();
        }
    }
}
