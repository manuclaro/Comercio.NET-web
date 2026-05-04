namespace Comercio.NET.Mobile.Server.Services
{
    /// <summary>
    /// Servicio en segundo plano que periódicamente fuerza la recolección de basura
    /// y libera memoria al sistema operativo. Evita el crecimiento gradual de memoria
    /// en entornos de contenedores (Railway) donde el GC no devuelve memoria al OS
    /// de forma agresiva por defecto.
    /// </summary>
    public class MemoryManagementService : BackgroundService
    {
        private static readonly TimeSpan _interval = TimeSpan.FromHours(4);
        private readonly ILogger<MemoryManagementService> _logger;

        public MemoryManagementService(ILogger<MemoryManagementService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MemoryManagementService iniciado. Intervalo de limpieza: {Interval}h", _interval.TotalHours);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_interval, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    var antes = GC.GetTotalMemory(false);

                    // Forzar recolección completa de todas las generaciones
                    GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

                    var despues = GC.GetTotalMemory(false);
                    var liberados = antes - despues;

                    _logger.LogInformation(
                        "GC completado. Antes: {Antes:N0} bytes | Después: {Despues:N0} bytes | Liberados: {Liberados:N0} bytes",
                        antes, despues, liberados);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Error durante la limpieza periódica de memoria");
                }
            }
        }
    }
}
