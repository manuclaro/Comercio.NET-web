using Comercio.NET.Mobile.Server.Controllers;
using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class ProductosService : IProductosService
    {
        private readonly DbService _db;
        private readonly ILogger<ProductosService> _logger;

        public ProductosService(DbService db, ILogger<ProductosService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<ProductoDto>> BuscarProductosAsync(string termino)
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Enumerable.Empty<ProductoDto>();

            var likeOp = _db.UsaPostgres ? "ILIKE" : "LIKE";
            var activoVal = _db.UsaPostgres ? "1::bit" : "1";
            string sql;
            var trimmed = termino.Trim();
            // Si el termino es corto (1-3 chars) y numerico, buscar solo por codigo exacto
            if (trimmed.Length <= 3 && trimmed.All(char.IsDigit))
            {
                sql = string.Format(@"SELECT codigo, descripcion, costo, precio, cantidad, rubro, marca, editarprecio FROM productos WHERE activo = {0} AND codigo = @termino", activoVal);
                try
                {
                    var rows = await _db.QueryAsync(sql, new Dictionary<string, object?> { { "@termino", trimmed } });
                    var productos = rows.Select(row => new ProductoDto { Codigo = GetString(row,0), Descripcion = GetString(row,1), Costo = GetDecimal(row,2), Precio = GetDecimal(row,3), Stock = GetInt(row,4), Rubro = GetString(row,5), Marca = GetString(row,6), EditarPrecio = GetBool(row,7) }).ToList();
                    _logger.LogInformation("Busqueda exacta: {C} resultado(s)", productos.Count);
                    return productos;
                }
                catch (Exception ex) { _logger.LogError(ex, "Error buscando productos"); throw; }
            }

            // Separar por espacios: cada palabra debe coincidir en descripcion o marca
            var palabras = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var condiciones = new List<string>();
            var parametros = new Dictionary<string, object?>();

            for (int i = 0; i < palabras.Length; i++)
            {
                var paramName = $"@p{i}";
                parametros[paramName] = $"%{palabras[i]}%";
                // Si la palabra es numerica, buscar solo en descripcion (no en codigo de barras)
                if (palabras[i].All(char.IsDigit))
                    condiciones.Add($"(descripcion {likeOp} {paramName})");
                else
                    condiciones.Add($"(descripcion {likeOp} {paramName} OR marca {likeOp} {paramName} OR rubro {likeOp} {paramName})");
            }

            var whereWords = string.Join(" AND ", condiciones);
            if (_db.UsaPostgres)
                sql = $"SELECT codigo, descripcion, costo, precio, cantidad, rubro, marca, editarprecio FROM productos WHERE activo = {activoVal} AND {whereWords} ORDER BY descripcion LIMIT 50";
            else
                sql = $"SELECT TOP 50 codigo, descripcion, costo, precio, cantidad, rubro, marca, editarprecio FROM productos WHERE activo = {activoVal} AND {whereWords} ORDER BY descripcion";
            try
            {
                var rows = await _db.QueryAsync(sql, parametros);
                var productos = rows.Select(row => new ProductoDto { Codigo = GetString(row,0), Descripcion = GetString(row,1), Costo = GetDecimal(row,2), Precio = GetDecimal(row,3), Stock = GetInt(row,4), Rubro = GetString(row,5), Marca = GetString(row,6), EditarPrecio = GetBool(row,7) }).ToList();
                _logger.LogInformation("Busqueda: {C} resultado(s)", productos.Count);
                return productos;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando productos"); throw; }
        }

        public async Task ActualizarProductoAsync(string codigo, ActualizarProductoDto datos)
        {
            var sql = "UPDATE productos SET costo=@costo, precio=@precio, cantidad=@cantidad WHERE codigo=@codigo";
            try
            {
                await _db.ExecuteAsync(sql, new Dictionary<string, object?> { {"@costo",datos.Costo},{"@precio",datos.Precio},{"@cantidad",datos.Stock},{"@codigo",codigo} });
                _logger.LogInformation("Producto actualizado: {C}", codigo);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error actualizando producto"); throw; }
        }

        private static string GetString(List<JsonElement> row, int i) =>
            row.Count > i ? (row[i].ValueKind == JsonValueKind.String ? row[i].GetString() ?? "" : row[i].ToString()) : "";
        private static decimal GetDecimal(List<JsonElement> row, int i) =>
            row.Count > i && row[i].ValueKind == JsonValueKind.Number ? row[i].GetDecimal() : 0m;
        private static int GetInt(List<JsonElement> row, int i) =>
            row.Count > i && row[i].ValueKind == JsonValueKind.Number ? row[i].GetInt32() : 0;
        private static bool GetBool(List<JsonElement> row, int i)
        {
            if (row.Count <= i) return false;
            var el = row[i];
            if (el.ValueKind == JsonValueKind.True) return true;
            if (el.ValueKind == JsonValueKind.False) return false;
            if (el.ValueKind == JsonValueKind.Number) return el.GetInt32() != 0;
            if (el.ValueKind == JsonValueKind.String) return el.GetString() == "1" || el.GetString()?.ToLower() == "true";
            return false;
        }
    }
}
