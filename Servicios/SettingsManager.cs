using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Comercio.NET.Servicios
{
    public static class SettingsManager
    {
        public static IConfigurationRoot Configuration { get; }

        // Suscriptores se registran aquí
        public static event Action? SettingsReloaded;

        static SettingsManager()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Cuando el archivo cambie, recargar y notificar
            ChangeToken.OnChange(() => Configuration.GetReloadToken(), () =>
            {
                try
                {
                    // Configuration ya se recargará automáticamente por reloadOnChange.
                    // Solo notificar a los suscriptores.
                    SettingsReloaded?.Invoke();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al notificar SettingsReloaded: {ex.Message}");
                }
            });
        }

        public static T GetValue<T>(string key, T defaultValue = default!)
        {
            try
            {
                return Configuration.GetValue<T>(key, defaultValue);
            }
            catch
            {
                return defaultValue;
            }
        }

        // Método público para forzar recarga y notificación desde fuera del tipo
        public static void ReloadConfiguration()
        {
            try
            {
                if (Configuration != null)
                {
                    Configuration.Reload();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al recargar Configuration: {ex.Message}");
            }

            // Invocar evento desde dentro del tipo (permitido)
            try
            {
                SettingsReloaded?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al invocar SettingsReloaded: {ex.Message}");
            }
        }

        // Método alternativo para solo notificar sin recargar
        public static void NotifySettingsReloaded()
        {
            try
            {
                SettingsReloaded?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en NotifySettingsReloaded: {ex.Message}");
            }
        }
    }
}