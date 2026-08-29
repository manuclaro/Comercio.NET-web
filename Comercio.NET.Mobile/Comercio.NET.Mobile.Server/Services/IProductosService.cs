using Comercio.NET.Mobile.Server.Models;

namespace Comercio.NET.Mobile.Server.Controllers
{
    public interface IProductosService
    {
        Task<IEnumerable<ProductoDto>> BuscarProductosAsync(string termino);
        Task ActualizarProductoAsync(string codigo, ActualizarProductoDto datos);
        Task<IEnumerable<ProductoDto>> ListarTodosAsync(string? buscar, int pagina, int tamano);
        Task<int> ContarTodosAsync(string? buscar);
        Task<ProductoDto?> ObtenerAsync(string codigo);
        Task EditarCompletoAsync(string codigo, EditarProductoCompletoDto datos);
        Task<string> CrearAsync(NuevoProductoDto datos);
        Task EliminarAsync(string codigo);
        Task<IEnumerable<string>> ListarRubrosAsync();
        Task<IEnumerable<string>> ListarMarcasAsync();
        Task<bool> ExisteCodigoAsync(string codigo);
    }
}