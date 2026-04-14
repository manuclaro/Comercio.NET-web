using Comercio.NET.Mobile.Server.Models;

namespace Comercio.NET.Mobile.Server.Services
{
    public interface IAuditoriaService
    {
        Task<IEnumerable<AuditoriaDto>> GetAuditoriaAsync(DateTime desde, DateTime hasta, string usuario = null, int? numeroCajero = null);
    }
}