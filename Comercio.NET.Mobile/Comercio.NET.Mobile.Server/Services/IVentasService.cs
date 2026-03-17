using Comercio.NET.Mobile.Server.Models;

namespace Comercio.NET.Mobile.Server.Services
{
    public interface IVentasService
    {
        Task<IEnumerable<VentaDto>> GetVentasDelDiaAsync(DateTime desde, DateTime hasta, int? numeroCajero = null, string formaPago = null, string tipoFactura = null);
        Task<ResumenVentasDto> GetResumenAsync(DateTime desde, DateTime hasta, int? numeroCajero = null, string formaPago = null, string tipoFactura = null);
    }
}