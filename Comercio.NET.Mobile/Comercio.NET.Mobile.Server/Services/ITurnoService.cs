using Comercio.NET.Mobile.Server.Models;

namespace Comercio.NET.Mobile.Server.Services
{
    public interface ITurnoService
    {
        Task<TurnoDto?> GetTurnoActivoAsync();
        Task<TurnoDto> AbrirTurnoAsync();
        Task<TurnoDto> CerrarTurnoAsync();
        Task<bool> HayMesasAbiertasAsync();
    }
}