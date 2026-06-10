using Comercio.NET.Mobile.Server.Controllers;
using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    // Postgres exclusivo (migrado desde SQL Server).
    // Columnas con mayúscula (case-sensitive en PG) se escapan con comillas dobles.
    // Los booleanos siempre se pasan como parámetros tipados, nunca interpolados.
    public class ProductosService : IProductosService
    {
        private readonly DbService _db;
        private readonly ILogger<ProductosService> _logger;

        // Expresiones compatibles con esquemas legacy (bit) y nuevos (boolean)
        private const string ACTIVO_EXPR =
            "COALESCE((to_jsonb(productos)->>'Activo')::boolean, (to_jsonb(productos)->>'activo') IN ('1','true','t'), false)";
        private const string EDITAR_PRECIO_EXPR =
            "COALESCE((to_jsonb(productos)->>'EditarPrecio')::boolean, (to_jsonb(productos)->>'editarprecio') IN ('1','true','t'), false)";
        private const string PERMITE_ACUMULAR_EXPR =
            "COALESCE((to_jsonb(productos)->>'PermiteAcumular')::boolean, (to_jsonb(productos)->>'permiteacumular') IN ('1','true','t'), false)";

        // Columnas SELECT en el orden que espera MapRow
        private static readonly string COLS =
            $"codigo, descripcion, costo, precio, cantidad, rubro, marca, {EDITAR_PRECIO_EXPR} AS \"EditarPrecio\", COALESCE(porcentaje,0), COALESCE(iva,21), {ACTIVO_EXPR} AS \"Activo\", COALESCE(proveedor,''), {PERMITE_ACUMULAR_EXPR} AS \"PermiteAcumular\"";

        public ProductosService(DbService db, ILogger<ProductosService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<ProductoDto>> BuscarProductosAsync(string termino)
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Enumerable.Empty<ProductoDto>();

            var trimmed = termino.Trim();

            // Si el término es completamente numérico → búsqueda por código exacto (sin importar longitud)
            if (trimmed.All(char.IsDigit))
            {
                var sql = $"""SELECT {COLS} FROM productos WHERE {ACTIVO_EXPR} = @activo AND codigo = @termino""";
                try
                {
                    var rows = await _db.QueryAsync(sql, new() { { "@activo", true }, { "@termino", trimmed } });
                    var lista = rows.Select(MapRow).ToList();
                    if (lista.Count > 0)
                    {
                        _logger.LogInformation("Búsqueda exacta por código: {C} resultado(s)", lista.Count);
                        return lista;
                    }
                    // Si no hay match exacto, continuar con búsqueda parcial por código
                    _logger.LogInformation("No match exacto para código '{Termino}', buscando parcialmente...", trimmed);
                }
                catch (Exception ex) { _logger.LogError(ex, "Error buscando productos"); throw; }
            }

            // Búsqueda por palabras: cada término debe aparecer en descripción, marca, rubro o código
            var palabras   = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var condiciones = new List<string>();
            var parametros  = new Dictionary<string, object?> { { "@activo", true } };

            for (int i = 0; i < palabras.Length; i++)
            {
                var p = $"@p{i}";
                parametros[p] = $"%{palabras[i]}%";
                condiciones.Add(palabras[i].All(char.IsDigit)
                    ? $"(codigo ILIKE {p} OR descripcion ILIKE {p})"
                    : $"(descripcion ILIKE {p} OR marca ILIKE {p} OR rubro ILIKE {p})");
            }

            var where = string.Join(" AND ", condiciones);
            var sqlBusq = $"""SELECT {COLS} FROM productos WHERE {ACTIVO_EXPR} = @activo AND {where} ORDER BY descripcion LIMIT 50""";
            try
            {
                var rows     = await _db.QueryAsync(sqlBusq, parametros);
                var productos = rows.Select(MapRow).ToList();
                _logger.LogInformation("Búsqueda: {C} resultado(s)", productos.Count);
                return productos;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando productos"); throw; }
        }

        public async Task ActualizarProductoAsync(string codigo, ActualizarProductoDto datos)
        {
            const string sql = """
                UPDATE productos
                SET costo=@costo, precio=@precio, cantidad=@cantidad, porcentaje=@pct
                WHERE codigo=@codigo
                """;
            try
            {
                await _db.ExecuteAsync(sql, new()
                {
                    { "@costo",    datos.Costo   },
                    { "@precio",   datos.Precio  },
                    { "@cantidad", datos.Stock   },
                    { "@pct",      datos.Porcentaje },
                    { "@codigo",   codigo        }
                });
                _logger.LogInformation("Producto actualizado: {C}", codigo);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error actualizando producto"); throw; }
        }

        public async Task<int> ContarTodosAsync(string? buscar)
        {
            var parms      = new Dictionary<string, object?> { { "@activo", true } };
            var whereExtra = "";
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                parms["@b"] = $"%{buscar.Trim()}%";
                whereExtra  = " AND (descripcion ILIKE @b OR codigo ILIKE @b OR rubro ILIKE @b OR marca ILIKE @b)";
            }
            var sql = $"""SELECT COUNT(*) FROM productos WHERE {ACTIVO_EXPR} = @activo{whereExtra}""";
            var result = await _db.ScalarAsync<long>(sql, parms);
            return (int)result;
        }

        public async Task<IEnumerable<ProductoDto>> ListarTodosAsync(string? buscar, int pagina, int tamano)
        {
            var parms      = new Dictionary<string, object?> { { "@activo", true } };
            var whereExtra = "";
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                parms["@b"] = $"%{buscar.Trim()}%";
                whereExtra  = " AND (descripcion ILIKE @b OR codigo ILIKE @b OR rubro ILIKE @b OR marca ILIKE @b)";
            }
            var offset = (pagina - 1) * tamano;
            var sql = $"""SELECT {COLS} FROM productos WHERE {ACTIVO_EXPR} = @activo{whereExtra} ORDER BY descripcion LIMIT {tamano} OFFSET {offset}""";
            var rows = await _db.QueryAsync(sql, parms);
            return rows.Select(MapRow);
        }

        public async Task<ProductoDto?> ObtenerAsync(string codigo)
        {
            var sql  = $"""SELECT {COLS} FROM productos WHERE codigo = @codigo""";
            var rows = await _db.QueryAsync(sql, new() { { "@codigo", codigo } });
            return rows.Count == 0 ? null : MapRow(rows[0]);
        }

        public async Task EditarCompletoAsync(string codigo, EditarProductoCompletoDto datos)
        {
            var editarPrecioBit = datos.EditarPrecio    ? "TRUE" : "FALSE";
            var permiteAcumBit  = datos.PermiteAcumular ? "TRUE" : "FALSE";
            var activoBit       = datos.Activo          ? "TRUE" : "FALSE";
            var sql = $"""
                UPDATE productos
                SET descripcion=@desc, rubro=@rubro, marca=@marca,
                    proveedor=@proveedor, costo=@costo, precio=@precio,
                    cantidad=@stock, porcentaje=@pct, iva=@iva,
                    "EditarPrecio"=@editarPrecio, "Activo"=@activo,
                    "PermiteAcumular"=@permiteAcumular,
                    activo={activoBit}, editarprecio={editarPrecioBit}, permiteacumular={permiteAcumBit}
                WHERE codigo=@codigo
                """;
            await _db.ExecuteAsync(sql, new()
            {
                { "@desc",            datos.Descripcion     },
                { "@rubro",           datos.Rubro           },
                { "@marca",           datos.Marca           },
                { "@proveedor",       datos.Proveedor       },
                { "@costo",           datos.Costo           },
                { "@precio",          datos.Precio          },
                { "@stock",           datos.Stock           },
                { "@pct",             datos.Porcentaje      },
                { "@iva",             datos.Iva             },
                { "@editarPrecio",    datos.EditarPrecio    },
                { "@activo",          datos.Activo          },
                { "@permiteAcumular", datos.PermiteAcumular },
                { "@codigo",          codigo                }
            });
        }

        public async Task<string> CrearAsync(NuevoProductoDto datos)
        {
            var editarPrecioBit   = datos.EditarPrecio    ? "TRUE" : "FALSE";
            var permiteAcumBit    = datos.PermiteAcumular ? "TRUE" : "FALSE";
            var sql = $"""
                INSERT INTO productos
                    (codigo, descripcion, rubro, marca, proveedor,
                     costo, precio, cantidad, porcentaje, iva, "EditarPrecio", "Activo", "PermiteAcumular",
                     activo, editarprecio, permiteacumular)
                VALUES
                    (@codigo, @desc, @rubro, @marca, @proveedor,
                     @costo, @precio, @stock, @pct, @iva, @editarPrecio, true, @permiteAcumular,
                     TRUE, {editarPrecioBit}, {permiteAcumBit})
                """;
            await _db.ExecuteAsync(sql, new()
            {
                { "@codigo",          datos.Codigo          },
                { "@desc",            datos.Descripcion     },
                { "@rubro",           datos.Rubro           },
                { "@marca",           datos.Marca           },
                { "@proveedor",       datos.Proveedor       },
                { "@costo",           datos.Costo           },
                { "@precio",          datos.Precio          },
                { "@stock",           datos.Stock           },
                { "@pct",             datos.Porcentaje      },
                { "@iva",             datos.Iva             },
                { "@editarPrecio",    datos.EditarPrecio    },
                { "@permiteAcumular", datos.PermiteAcumular }
            });
            return datos.Codigo;
        }

        public async Task EliminarAsync(string codigo)
        {
            // Baja lógica: desactiva el producto sin borrarlo físicamente
            const string sql = """UPDATE productos SET "Activo" = false WHERE codigo = @codigo""";
            await _db.ExecuteAsync(sql, new() { { "@codigo", codigo } });
        }

        public async Task<IEnumerable<string>> ListarRubrosAsync()
        {
            const string sql = "SELECT DISTINCT rubro FROM productos WHERE rubro IS NOT NULL AND rubro <> '' ORDER BY rubro";
            var rows = await _db.QueryAsync(sql, new());
            return rows.Select(r => GetString(r, 0)).Where(s => !string.IsNullOrWhiteSpace(s));
        }

        public async Task<IEnumerable<string>> ListarMarcasAsync()
        {
            const string sql = "SELECT DISTINCT marca FROM productos WHERE marca IS NOT NULL AND marca <> '' ORDER BY marca";
            var rows = await _db.QueryAsync(sql, new());
            return rows.Select(r => GetString(r, 0)).Where(s => !string.IsNullOrWhiteSpace(s));
        }

        public async Task<bool> ExisteCodigoAsync(string codigo)
        {
            const string sql = "SELECT COUNT(*) FROM productos WHERE codigo = @codigo";
            var rows = await _db.QueryAsync(sql, new() { { "@codigo", codigo } });
            return rows.Count > 0 && rows[0].Count > 0 && rows[0][0].ValueKind == System.Text.Json.JsonValueKind.Number && rows[0][0].GetInt32() > 0;
        }

        // Orden de columnas según COLS:
        // 0=codigo  1=descripcion  2=costo  3=precio  4=cantidad  5=rubro
        // 6=marca   7=EditarPrecio 8=porcentaje 9=iva  10=Activo  11=proveedor  12=PermiteAcumular
        private static ProductoDto MapRow(List<JsonElement> r) => new()
        {
            Codigo          = GetString(r, 0),
            Descripcion     = GetString(r, 1),
            Costo           = GetDecimal(r, 2),
            Precio          = GetDecimal(r, 3),
            Stock           = GetInt(r, 4),
            Rubro           = GetString(r, 5),
            Marca           = GetString(r, 6),
            EditarPrecio    = GetBool(r, 7),
            Porcentaje      = GetInt(r, 8),
            Iva             = GetDecimal(r, 9),
            Activo          = GetBool(r, 10),
            Proveedor       = GetString(r, 11),
            PermiteAcumular = GetBool(r, 12)
        };

        private static string GetString(List<JsonElement> row, int i) =>
            row.Count > i && row[i].ValueKind != JsonValueKind.Null
                ? (row[i].ValueKind == JsonValueKind.String ? row[i].GetString() ?? "" : row[i].ToString())
                : "";

        private static decimal GetDecimal(List<JsonElement> row, int i) =>
            row.Count > i && row[i].ValueKind == JsonValueKind.Number ? row[i].GetDecimal() : 0m;

        private static int GetInt(List<JsonElement> row, int i) =>
            row.Count > i && row[i].ValueKind == JsonValueKind.Number ? row[i].GetInt32() : 0;

        private static bool GetBool(List<JsonElement> row, int i)
        {
            if (row.Count <= i) return false;
            var el = row[i];
            if (el.ValueKind == JsonValueKind.True)  return true;
            if (el.ValueKind == JsonValueKind.False) return false;
            if (el.ValueKind == JsonValueKind.Number) return el.GetInt32() != 0;
            if (el.ValueKind == JsonValueKind.String)
                return el.GetString() == "1" || el.GetString()?.ToLower() is "true" or "t";
            return false;
        }
    }
}
