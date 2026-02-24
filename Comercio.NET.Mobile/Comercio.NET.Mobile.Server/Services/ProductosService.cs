using Comercio.NET.Mobile.Server.Controllers;
using Comercio.NET.Mobile.Server.Models;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace Comercio.NET.Mobile.Server.Services
{
    public class ProductosService : IProductosService
    {
        private readonly IConfiguration _configuration;

        public ProductosService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IEnumerable<ProductoDto>> BuscarProductosAsync(string termino)
        {
            var productos = new List<ProductoDto>();
            var sqlBridgeUrl = _configuration["SqlBridgeUrl"];

            using var httpClient = new HttpClient();

            var query = @"
                SELECT codigo, descripcion, precio, cantidad, rubro, marca
                FROM Productos
                WHERE ISNULL(Activo, 1) = 1
                  AND (
                        codigo      LIKE @termino
                     OR descripcion LIKE @termino
                     OR rubro       LIKE @termino
                     OR marca       LIKE @termino
                  )
                ORDER BY descripcion";

            var payload = new
            {
                query,
                parameters = new Dictionary<string, object>
                {
                    { "@termino", $"%{termino}%" }
                }
            };

            var response = await httpClient.PostAsJsonAsync($"{sqlBridgeUrl}/query", payload);
            response.EnsureSuccessStatusCode();

            var resultado = await response.Content.ReadFromJsonAsync<SqlBridgeResult>();

            if (resultado?.Rows != null)
            {
                foreach (var row in resultado.Rows)
                {
                    productos.Add(new ProductoDto
                    {
                        Codigo      = row.GetValueOrDefault("codigo")?.ToString() ?? "",
                        Descripcion = row.GetValueOrDefault("descripcion")?.ToString() ?? "",
                        Precio      = Convert.ToDecimal(row.GetValueOrDefault("precio") ?? 0),
                        Stock       = Convert.ToInt32(row.GetValueOrDefault("cantidad") ?? 0),
                        Rubro       = row.GetValueOrDefault("rubro")?.ToString() ?? "",
                        Marca       = row.GetValueOrDefault("marca")?.ToString() ?? ""
                    });
                }
            }

            return productos;
        }
    }

    internal class SqlBridgeResult
    {
        public List<Dictionary<string, object>> Rows { get; set; }
    }
}