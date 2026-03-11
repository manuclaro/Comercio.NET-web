using Comercio.NET.Mobile.Server.Models;

namespace Comercio.NET.Mobile.Server.Services
{
    public interface IVentasService
    {
        Task<IEnumerable<VentaDto>> GetVentasDelDiaAsync(DateTime fecha, int? numeroCajero = null, string formaPago = null);
        Task<ResumenVentasDto> GetResumenAsync(DateTime fecha, int? numeroCajero = null);
    }
}