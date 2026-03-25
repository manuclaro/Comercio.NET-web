using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class TurnoService : ITurnoService
    {
        private readonly string _sqlBridgeUrl;
        private readonly ILogger<TurnoService> _logger;
        private readonly HttpClient _httpClient;

        public TurnoService(
            IConfiguration configuration,
            ILogger<TurnoService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _sqlBridgeUrl = Environment.GetEnvironmentVariable("SQL_BRIDGE_URL")
                ?? configuration["SqlBridgeUrl"]
                ?? throw new InvalidOperationException("SQL_BRIDGE_URL no está configurada");
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<TurnoDto?> GetTurnoActivoAsync()
        {
            var sql = @"
                SELECT TOP 1 Id, NumeroCajero, Usuario, FechaApertura, FechaCierre, MontoInicial, Estado, ISNULL(Observaciones, '') as Observaciones
                FROM TurnosCajero
                WHERE Estado = 'Abierto'
                ORDER BY Id DESC";

            var payload = new { query = sql, parameters = new Dictionary<string, object?>() };

            var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);
            var content  = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ SQL Bridge error en GetTurnoActivoAsync: {StatusCode} - {Content}",
                    response.StatusCode, content);
                return null;
            }

            var resultado = JsonSerializer.Deserialize<QueryResult>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (resultado?.Data == null || resultado.Data.Count == 0)
                return null;

            return MapTurno(resultado.Data[0]);
        }

        public async Task<TurnoDto> AbrirTurnoAsync()
        {
            var sql = @"
                INSERT INTO TurnosCajero (FechaApertura, Estado, MontoInicial)
                OUTPUT INSERTED.Id, INSERTED.NumeroCajero, INSERTED.Usuario, INSERTED.FechaApertura,
                       INSERTED.FechaCierre, INSERTED.MontoInicial, INSERTED.Estado, ISNULL(INSERTED.Observaciones, '') as Observaciones
                VALUES (GETDATE(), 'Abierto', 0)";

            var payload = new { query = sql, parameters = new Dictionary<string, object?>() };

            var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);
            var content  = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error en SQL Bridge: {response.StatusCode}");

            var resultado = JsonSerializer.Deserialize<QueryResult>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var row = resultado?.Data?.FirstOrDefault()
                ?? throw new Exception("No se pudo obtener el turno creado.");

            return MapTurno(row);
        }

        public async Task<TurnoDto> CerrarTurnoAsync()
        {
            var sql = @"
                UPDATE TurnosCajero
                SET FechaCierre = GETDATE(), Estado = 'Cerrado'
                WHERE Estado = 'Abierto';

                SELECT TOP 1 Id, NumeroCajero, Usuario, FechaApertura, FechaCierre, MontoInicial, Estado, ISNULL(Observaciones, '') as Observaciones
                FROM TurnosCajero
                WHERE Estado = 'Cerrado'
                ORDER BY Id DESC";

            var payload = new { query = sql, parameters = new Dictionary<string, object?>() };

            var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);
            var content  = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error en SQL Bridge: {response.StatusCode}");

            var resultado = JsonSerializer.Deserialize<QueryResult>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var row = resultado?.Data?.FirstOrDefault()
                ?? throw new Exception("No se pudo obtener el turno cerrado.");

            return MapTurno(row);
        }

        public async Task<bool> HayMesasAbiertasAsync()
        {
            var sql = "SELECT COUNT(*) FROM Mesas WHERE Estado = 'Abierta'";
            var payload = new { query = sql, parameters = new Dictionary<string, object?>() };

            var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);
            var content  = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error en SQL Bridge: {response.StatusCode}");

            var resultado = JsonSerializer.Deserialize<QueryResult>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var row = resultado?.Data?.FirstOrDefault();
            if (row == null || row.Count == 0) return false;

            return ConvertToInt32(row[0]) > 0;
        }

        private static TurnoDto MapTurno(List<object?> row) => new()
        {
            Id            = ConvertToInt32(row.Count > 0 ? row[0] : null),
            NumeroCajero  = ConvertToInt32(row.Count > 1 ? row[1] : null),
            Usuario       = ConvertToString(row.Count > 2 ? row[2] : null),
            FechaApertura = ConvertToDateTime(row.Count > 3 ? row[3] : null),
            FechaCierre   = ConvertToNullableDateTime(row.Count > 4 ? row[4] : null),
            MontoInicial  = ConvertToDecimal(row.Count > 5 ? row[5] : null),
            Estado        = ConvertToString(row.Count > 6 ? row[6] : null),
            Observaciones = ConvertToString(row.Count > 7 ? row[7] : null),
        };

        private static int ConvertToInt32(object? value)
        {
            if (value is null) return 0;
            if (value is JsonElement j)
                return j.ValueKind switch
                {
                    JsonValueKind.Number => j.GetInt32(),
                    JsonValueKind.String => int.TryParse(j.GetString(), out var r) ? r : 0,
                    _ => 0
                };
            return Convert.ToInt32(value);
        }

        private static decimal ConvertToDecimal(object? value)
        {
            if (value is null) return 0;
            if (value is JsonElement j)
                return j.ValueKind switch
                {
                    JsonValueKind.Number => j.GetDecimal(),
                    JsonValueKind.String => decimal.TryParse(j.GetString(), out var r) ? r : 0,
                    _ => 0
                };
            return Convert.ToDecimal(value);
        }

        private static string ConvertToString(object? value)
        {
            if (value is null) return string.Empty;
            if (value is JsonElement j)
                return j.ValueKind switch
                {
                    JsonValueKind.String => j.GetString() ?? string.Empty,
                    JsonValueKind.Null   => string.Empty,
                    _                   => j.ToString()
                };
            return value.ToString() ?? string.Empty;
        }

        private static DateTime ConvertToDateTime(object? value)
        {
            if (value is null) return DateTime.MinValue;
            if (value is JsonElement j && j.ValueKind == JsonValueKind.String)
                return DateTime.TryParse(j.GetString(), out var d) ? d : DateTime.MinValue;
            return Convert.ToDateTime(value);
        }

        private static DateTime? ConvertToNullableDateTime(object? value)
        {
            if (value is null) return null;
            if (value is JsonElement j && j.ValueKind == JsonValueKind.Null) return null;
            return ConvertToDateTime(value);
        }
    }
}