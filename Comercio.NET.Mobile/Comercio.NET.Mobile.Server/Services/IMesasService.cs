using Comercio.NET.Mobile.Server.Models;

namespace Comercio.NET.Mobile.Server.Services
{
    public interface IMesasService
    {
        Task<IEnumerable<MesaDto>> GetMesasAbiertasAsync();
        Task<MesaDto> GetMesaAsync(int mesaId);
        Task<IEnumerable<MesaItemDto>> GetItemsMesaAsync(int mesaId);
        Task<MesaDto> AbrirMesaAsync(AbrirMesaRequest request);
        Task AgregarItemAsync(int mesaId, AgregarItemRequest request);
        Task EliminarItemAsync(int itemId);
        Task<MesaDto> CerrarMesaAsync(int mesaId, CerrarMesaRequest request);

        // Mozos
        Task<IEnumerable<MozoDto>> GetMozosAsync();
        Task<MozoDto> CrearMozoAsync(string nombre);
        Task<MozoDto> ActualizarMozoAsync(int id, string nombre);
        Task EliminarMozoAsync(int id);

        // Productos Bar
        Task<IEnumerable<ProductoBarDto>> GetProductosBarAsync();
        Task<ProductoBarDto> CrearProductoBarAsync(ProductoBarDto dto);
        Task<ProductoBarDto> ActualizarProductoBarAsync(int id, ProductoBarDto dto);
        Task EliminarProductoBarAsync(int id);

        // Formas de Pago
        Task<IEnumerable<FormaPagoDto>> GetFormasPagoAsync();
        Task<FormaPagoDto> CrearFormaPagoAsync(string descripcion);
        Task<FormaPagoDto> ActualizarFormaPagoAsync(int id, string descripcion);
        Task EliminarFormaPagoAsync(int id);

        // Ventas del día
        Task<IEnumerable<VentaMesaResumenDto>> GetVentasDelDiaAsync();
    }
}