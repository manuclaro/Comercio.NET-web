using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class MesasService : IMesasService
    {
        private readonly string _sqlBridgeUrl;
        private readonly ILogger<MesasService> _logger;
        private readonly HttpClient _httpClient;

        public MesasService(
            IConfiguration configuration,
            ILogger<MesasService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _sqlBridgeUrl = Environment.GetEnvironmentVariable("SQL_BRIDGE_URL")
                ?? configuration["SqlBridgeUrl"]
                ?? throw new InvalidOperationException("SQL_BRIDGE_URL no está configurada");
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IEnumerable<MesaDto>> GetMesasAbiertasAsync()
        {
            var sql = @"
                SELECT Id, NumeroMesa, Mozo, Estado, FechaApertura, FechaCierre,
                       ISNULL((SELECT SUM(Subtotal) FROM MesasItems WHERE MesaId = m.Id), 0) AS Total
                FROM Mesas m
                WHERE Estado = 'Abierta'
                ORDER BY NumeroMesa";

            return await EjecutarListaMesasAsync(sql, new Dictionary<string, object?>());
        }

        public async Task<MesaDto> GetMesaAsync(int mesaId)
        {
            var sql = @"
                SELECT Id, NumeroMesa, Mozo, Estado, FechaApertura, FechaCierre,
                       ISNULL((SELECT SUM(Subtotal) FROM MesasItems WHERE MesaId = m.Id), 0) AS Total
                FROM Mesas m
                WHERE Id = @mesaId";

            var parameters = new Dictionary<string, object?> { { "@mesaId", mesaId } };
            var lista = await EjecutarListaMesasAsync(sql, parameters);
            return lista.FirstOrDefault();
        }

        public async Task<IEnumerable<MesaItemDto>> GetItemsMesaAsync(int mesaId)
        {
            var sql = @"
                SELECT Id, MesaId, Codigo, Descripcion, PrecioUnitario, Cantidad, Subtotal
                FROM MesasItems
                WHERE MesaId = @mesaId
                ORDER BY Id";

            var parameters = new Dictionary<string, object?> { { "@mesaId", mesaId } };
            var payload = new { query = sql, parameters };

            var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);
            var content  = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SQL Bridge error: {StatusCode} - {Content}", response.StatusCode, content);
                throw new Exception($"Error en SQL Bridge: {response.StatusCode}");
            }

            var resultado = JsonSerializer.Deserialize<QueryResult>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var items = new List<MesaItemDto>();
            if (resultado?.Data != null)
            {
                foreach (var row in resultado.Data)
                {
                    items.Add(new MesaItemDto
                    {
                        Id             = ConvertToInt32(row.Count > 0 ? row[0] : null),
                        MesaId         = ConvertToInt32(row.Count > 1 ? row[1] : null),
                        Codigo         = ConvertToString(row.Count > 2 ? row[2] : null),
                        Descripcion    = ConvertToString(row.Count > 3 ? row[3] : null),
                        PrecioUnitario = ConvertToDecimal(row.Count > 4 ? row[4] : null),
                        Cantidad       = ConvertToInt32(row.Count > 5 ? row[5] : null),
                        Subtotal       = ConvertToDecimal(row.Count > 6 ? row[6] : null),
                    });
                }
            }
            return items;
        }

        public async Task<MesaDto> AbrirMesaAsync(AbrirMesaRequest request)
        {
            var sql = @"
                INSERT INTO Mesas (NumeroMesa, Mozo, Estado, FechaApertura)
                OUTPUT INSERTED.Id, INSERTED.NumeroMesa, INSERTED.Mozo, INSERTED.Estado,
                       INSERTED.FechaApertura, INSERTED.FechaCierre, 0 AS Total
                VALUES (@numeroMesa, @mozo, 'Abierta', GETDATE())";

            var parameters = new Dictionary<string, object?>
            {
                { "@numeroMesa", request.NumeroMesa },
                { "@mozo",       request.Mozo }
            };

            var lista = await EjecutarListaMesasAsync(sql, parameters);
            return lista.FirstOrDefault();
        }

        public async Task AgregarItemAsync(int mesaId, AgregarItemRequest request)
        {
            var subtotal = request.PrecioUnitario * request.Cantidad;

            var sql = @"
                INSERT INTO MesasItems (MesaId, Codigo, Descripcion, PrecioUnitario, Cantidad, Subtotal)
                VALUES (@mesaId, @codigo, @descripcion, @precio, @cantidad, @subtotal)";

            var parameters = new Dictionary<string, object?>
            {
                { "@mesaId",      mesaId },
                { "@codigo",      request.Codigo },
                { "@descripcion", request.Descripcion },
                { "@precio",      request.PrecioUnitario },
                { "@cantidad",    request.Cantidad },
                { "@subtotal",    subtotal }
            };

            await EjecutarComandoAsync(sql, parameters);
        }

        public async Task EliminarItemAsync(int itemId)
        {
            var sql = "DELETE FROM MesasItems WHERE Id = @itemId";
            var parameters = new Dictionary<string, object?> { { "@itemId", itemId } };
            await EjecutarComandoAsync(sql, parameters);
        }

        public async Task<MesaDto> CerrarMesaAsync(int mesaId, CerrarMesaRequest request)
        {
            var sql = @"
                UPDATE Mesas
                SET Estado = 'Cerrada', FechaCierre = GETDATE()
                WHERE Id = @mesaId;

                SELECT m.Id, m.NumeroMesa, m.Mozo, m.Estado, m.FechaApertura, m.FechaCierre,
                       ISNULL((SELECT SUM(Subtotal) FROM MesasItems WHERE MesaId = m.Id), 0) AS Total
                FROM Mesas m
                WHERE m.Id = @mesaId";

            var parameters = new Dictionary<string, object?> { { "@mesaId", mesaId } };
            var lista = await EjecutarListaMesasAsync(sql, parameters);
            return lista.FirstOrDefault();
        }

        // ??? Helpers ?????????????????????????????????????????????????????????????

        private async Task<IEnumerable<MesaDto>> EjecutarListaMesasAsync(
            string sql, Dictionary<string, object?> parameters)
        {
            var payload = new { query = sql, parameters };
            var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);
            var content  = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SQL Bridge error: {StatusCode} - {Content}", response.StatusCode, content);
                throw new Exception($"Error en SQL Bridge: {response.StatusCode}");
            }

            var resultado = JsonSerializer.Deserialize<QueryResult>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var mesas = new List<MesaDto>();
            if (resultado?.Data != null)
            {
                foreach (var row in resultado.Data)
                {
                    mesas.Add(new MesaDto
                    {
                        Id            = ConvertToInt32(row.Count > 0 ? row[0] : null),
                        NumeroMesa    = ConvertToInt32(row.Count > 1 ? row[1] : null),
                        Mozo          = ConvertToString(row.Count > 2 ? row[2] : null),
                        Estado        = ConvertToString(row.Count > 3 ? row[3] : null),
                        FechaApertura = ConvertToDateTime(row.Count > 4 ? row[4] : null),
                        FechaCierre   = ConvertToNullableDateTime(row.Count > 5 ? row[5] : null),
                        Total         = ConvertToDecimal(row.Count > 6 ? row[6] : null),
                    });
                }
            }
            return mesas;
        }

        private async Task EjecutarComandoAsync(string sql, Dictionary<string, object?> parameters)
        {
            var payload = new { query = sql, parameters };
            var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);
            var content  = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SQL Bridge error: {StatusCode} - {Content}", response.StatusCode, content);
                throw new Exception($"Error en SQL Bridge: {response.StatusCode}");
            }
        }

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
            if (value is null) return 0m;
            if (value is JsonElement j)
                return j.ValueKind switch
                {
                    JsonValueKind.Number => j.GetDecimal(),
                    JsonValueKind.String => decimal.TryParse(j.GetString(), out var r) ? r : 0m,
                    _ => 0m
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