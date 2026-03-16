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

        // ── Mesas ─────────────────────────────────────────────────────────────

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
                SET Estado = 'Cerrada', FechaCierre = GETDATE(), FormaPago = @formaPago
                WHERE Id = @mesaId;

                SELECT m.Id, m.NumeroMesa, m.Mozo, m.Estado, m.FechaApertura, m.FechaCierre,
                       ISNULL((SELECT SUM(Subtotal) FROM MesasItems WHERE MesaId = m.Id), 0) AS Total
                FROM Mesas m
                WHERE m.Id = @mesaId";

            var parameters = new Dictionary<string, object?>
            {
                { "@mesaId",    mesaId },
                { "@formaPago", request.FormaPago }
            };
            var lista = await EjecutarListaMesasAsync(sql, parameters);
            return lista.FirstOrDefault();
        }

        // ── Mozos ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<MozoDto>> GetMozosAsync()
        {
            var sql = "SELECT Id, Nombre, Activo FROM Mozos WHERE Activo = 1 ORDER BY Nombre";
            return await EjecutarListaMozosAsync(sql, new Dictionary<string, object?>());
        }

        public async Task<MozoDto> CrearMozoAsync(string nombre)
        {
            var sql = @"
                INSERT INTO Mozos (Nombre, Activo)
                OUTPUT INSERTED.Id, INSERTED.Nombre, INSERTED.Activo
                VALUES (@nombre, 1)";

            var parameters = new Dictionary<string, object?> { { "@nombre", nombre } };
            var lista = await EjecutarListaMozosAsync(sql, parameters);
            return lista.FirstOrDefault();
        }

        public async Task EliminarMozoAsync(int id)
        {
            var sql = "UPDATE Mozos SET Activo = 0 WHERE Id = @id";
            var parameters = new Dictionary<string, object?> { { "@id", id } };
            await EjecutarComandoAsync(sql, parameters);
        }

        // ── Productos Bar ─────────────────────────────────────────────────────

        public async Task<IEnumerable<ProductoBarDto>> GetProductosBarAsync()
        {
            var sql = "SELECT Id, Codigo, Descripcion, Precio, Activo FROM ProductosBar WHERE Activo = 1 ORDER BY Descripcion";
            return await EjecutarListaProductosBarAsync(sql, new Dictionary<string, object?>());
        }

        public async Task<ProductoBarDto> CrearProductoBarAsync(ProductoBarDto dto)
        {
            var sql = @"
                INSERT INTO ProductosBar (Codigo, Descripcion, Precio, Activo)
                OUTPUT INSERTED.Id, INSERTED.Codigo, INSERTED.Descripcion, INSERTED.Precio, INSERTED.Activo
                VALUES (@codigo, @descripcion, @precio, 1)";

            var parameters = new Dictionary<string, object?>
            {
                { "@codigo",      dto.Codigo },
                { "@descripcion", dto.Descripcion },
                { "@precio",      dto.Precio }
            };
            var lista = await EjecutarListaProductosBarAsync(sql, parameters);
            return lista.FirstOrDefault();
        }

        public async Task<ProductoBarDto> ActualizarProductoBarAsync(int id, ProductoBarDto dto)
        {
            var sql = @"
                UPDATE ProductosBar
                SET Codigo = @codigo, Descripcion = @descripcion, Precio = @precio
                WHERE Id = @id;

                SELECT Id, Codigo, Descripcion, Precio, Activo
                FROM ProductosBar
                WHERE Id = @id";

            var parameters = new Dictionary<string, object?>
            {
                { "@id",          id },
                { "@codigo",      dto.Codigo },
                { "@descripcion", dto.Descripcion },
                { "@precio",      dto.Precio }
            };
            var lista = await EjecutarListaProductosBarAsync(sql, parameters);
            return lista.FirstOrDefault();
        }

        public async Task EliminarProductoBarAsync(int id)
        {
            var sql = "UPDATE ProductosBar SET Activo = 0 WHERE Id = @id";
            var parameters = new Dictionary<string, object?> { { "@id", id } };
            await EjecutarComandoAsync(sql, parameters);
        }

        // ── Ventas del Día ────────────────────────────────────────────────────

        public async Task<IEnumerable<VentaMesaResumenDto>> GetVentasDelDiaAsync()
        {
            var sql = @"
                SELECT m.Id, m.NumeroMesa, m.Mozo, m.Estado, m.FechaApertura, m.FechaCierre,
                       ISNULL((SELECT SUM(Subtotal) FROM MesasItems WHERE MesaId = m.Id), 0) AS Total,
                       ISNULL(m.FormaPago, '') AS FormaPago
                FROM Mesas m
                WHERE CAST(m.FechaApertura AS DATE) = CAST(GETDATE() AS DATE)
                ORDER BY m.FechaApertura DESC";

            var payload = new { query = sql, parameters = new Dictionary<string, object?>() };
            var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);
            var content  = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SQL Bridge error: {StatusCode} - {Content}", response.StatusCode, content);
                throw new Exception($"Error en SQL Bridge: {response.StatusCode}");
            }

            var resultado = JsonSerializer.Deserialize<QueryResult>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var lista = new List<VentaMesaResumenDto>();
            if (resultado?.Data != null)
            {
                foreach (var row in resultado.Data)
                {
                    lista.Add(new VentaMesaResumenDto
                    {
                        MesaId        = ConvertToInt32(row.Count > 0 ? row[0] : null),
                        NumeroMesa    = ConvertToInt32(row.Count > 1 ? row[1] : null),
                        Mozo          = ConvertToString(row.Count > 2 ? row[2] : null),
                        Estado        = ConvertToString(row.Count > 3 ? row[3] : null),
                        FechaApertura = ConvertToDateTime(row.Count > 4 ? row[4] : null),
                        FechaCierre   = ConvertToNullableDateTime(row.Count > 5 ? row[5] : null),
                        Total         = ConvertToDecimal(row.Count > 6 ? row[6] : null),
                        FormaPago     = ConvertToString(row.Count > 7 ? row[7] : null),
                    });
                }
            }
            return lista;
        }

        // ── Formas de Pago ────────────────────────────────────────────────────

        public async Task<IEnumerable<FormaPagoDto>> GetFormasPagoAsync()
        {
            var sql = "SELECT Id, Descripcion, Activo FROM FormasPago WHERE Activo = 1 ORDER BY Descripcion";
            return await EjecutarListaFormasPagoAsync(sql, new Dictionary<string, object?>());
        }

        public async Task<FormaPagoDto> CrearFormaPagoAsync(string descripcion)
        {
            var sql = @"
                INSERT INTO FormasPago (Descripcion, Activo)
                OUTPUT INSERTED.Id, INSERTED.Descripcion, INSERTED.Activo
                VALUES (@descripcion, 1)";

            var parameters = new Dictionary<string, object?> { { "@descripcion", descripcion } };
            var lista = await EjecutarListaFormasPagoAsync(sql, parameters);
            return lista.FirstOrDefault();
        }

        public async Task<FormaPagoDto> ActualizarFormaPagoAsync(int id, string descripcion)
        {
            var sql = @"
                UPDATE FormasPago SET Descripcion = @descripcion WHERE Id = @id;
                SELECT Id, Descripcion, Activo FROM FormasPago WHERE Id = @id";

            var parameters = new Dictionary<string, object?>
            {
                { "@id",          id },
                { "@descripcion", descripcion }
            };
            var lista = await EjecutarListaFormasPagoAsync(sql, parameters);
            return lista.FirstOrDefault();
        }

        public async Task EliminarFormaPagoAsync(int id)
        {
            var sql = "UPDATE FormasPago SET Activo = 0 WHERE Id = @id";
            var parameters = new Dictionary<string, object?> { { "@id", id } };
            await EjecutarComandoAsync(sql, parameters);
        }

        private async Task<IEnumerable<FormaPagoDto>> EjecutarListaFormasPagoAsync(
            string sql, Dictionary<string, object?> parameters)
        {
            var payload  = new { query = sql, parameters };
            var response = await _httpClient.PostAsJsonAsync($"{_sqlBridgeUrl}/query", payload);
            var content  = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SQL Bridge error: {StatusCode} - {Content}", response.StatusCode, content);
                throw new Exception($"Error en SQL Bridge: {response.StatusCode}");
            }

            var resultado = JsonSerializer.Deserialize<QueryResult>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var lista = new List<FormaPagoDto>();
            if (resultado?.Data != null)
            {
                foreach (var row in resultado.Data)
                {
                    lista.Add(new FormaPagoDto
                    {
                        Id          = ConvertToInt32(row.Count > 0 ? row[0] : null),
                        Descripcion = ConvertToString(row.Count > 1 ? row[1] : null),
                        Activo      = ConvertToInt32(row.Count > 2 ? row[2] : null) == 1,
                    });
                }
            }
            return lista;
        }

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

        private async Task<IEnumerable<MozoDto>> EjecutarListaMozosAsync(
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

            var lista = new List<MozoDto>();
            if (resultado?.Data != null)
            {
                foreach (var row in resultado.Data)
                {
                    lista.Add(new MozoDto
                    {
                        Id     = ConvertToInt32(row.Count > 0 ? row[0] : null),
                        Nombre = ConvertToString(row.Count > 1 ? row[1] : null),
                        Activo = ConvertToInt32(row.Count > 2 ? row[2] : null) == 1,
                    });
                }
            }
            return lista;
        }

        private async Task<IEnumerable<ProductoBarDto>> EjecutarListaProductosBarAsync(
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

            var lista = new List<ProductoBarDto>();
            if (resultado?.Data != null)
            {
                foreach (var row in resultado.Data)
                {
                    lista.Add(new ProductoBarDto
                    {
                        Id          = ConvertToInt32(row.Count > 0 ? row[0] : null),
                        Codigo      = ConvertToString(row.Count > 1 ? row[1] : null),
                        Descripcion = ConvertToString(row.Count > 2 ? row[2] : null),
                        Precio      = ConvertToDecimal(row.Count > 3 ? row[3] : null),
                        Activo      = ConvertToInt32(row.Count > 4 ? row[4] : null) == 1,
                    });
                }
            }
            return lista;
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