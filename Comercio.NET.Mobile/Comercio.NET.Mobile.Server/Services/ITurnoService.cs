using Comercio.NET.Mobile.Server.Models;

using Comercio.NET.Mobile.Server.Models;

namespace Comercio.NET.Mobile.Server.Services
{
    public interface ITurnoService
    {
        Task<TurnoDto?> GetTurnoActivoAsync();
        Task<TurnoDto> AbrirTurnoAsync(decimal montoInicial = 0, int numeroCajero = 1, string usuario = "");
        Task<TurnoDto> CerrarTurnoAsync();
        Task<TurnoDto> CerrarTurnoPorIdAsync(int id, List<DeclaracionMedioPago>? declaraciones = null, int cantidadVentas = 0);
        Task<bool> HayMesasAbiertasAsync();
        Task<ResumenTurnoDto?> GetResumenTurnoAsync(int? turnoId = null);
        Task<List<TurnoDto>> GetTurnosAbiertosAsync();
        Task<List<TurnoDto>> GetTurnosDelDiaAsync(DateTime fecha);
        Task<List<HistorialCierreDto>> GetHistorialCierresAsync(DateTime desde, DateTime hasta, string? cajero = null);
        Task<List<string>> GetCajerosAsync();
    }
}