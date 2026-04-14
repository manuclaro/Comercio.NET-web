using Comercio.NET.Mobile.Server.Models;

namespace Comercio.NET.Mobile.Server.Controllers
{
    public interface IProductosService
    {
        Task<IEnumerable<ProductoDto>> BuscarProductosAsync(string termino);
        Task ActualizarProductoAsync(string codigo, ActualizarProductoDto datos);
    }
}