using Comercio.NET.Mobile.Server.Models;

namespace Comercio.NET.Mobile.Server.Services
{
    public interface ICajaService
    {
        Task<string>               GenerarNroRemitoAsync();
        Task<TicketActivoDto>      GetTicketAsync(string nroRemito);
        Task<ItemTicketDto>        AgregarItemAsync(AgregarItemRequest request);
        Task<ItemTicketDto>        ActualizarCantidadAsync(long idVenta, int nuevaCantidad);
        Task                       EliminarItemAsync(long idVenta, string? usuario = null, int? numeroCajero = null, string? motivo = null);
        Task                       CancelarTicketAsync(string nroRemito, string? usuario = null, int? numeroCajero = null);
        Task<ConfirmarVentaResponse> ConfirmarVentaAsync(ConfirmarVentaRequest request);
    }
}
