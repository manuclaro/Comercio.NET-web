using Microsoft.Extensions.Configuration;
using System;

namespace Comercio.NET.Servicios
{
    public static class ConfiguracionComercioService
    {
        private static IConfiguration _configuration;

        public static void ConfigurarConfiguracion(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static TicketConfig ObtenerConfiguracionTicket(string tipoComprobante, string numeroComprobante)
        {
            return new TicketConfig
            {
                NombreComercio = _configuration?["Comercio:Nombre"] ?? "Mi Comercio",
                DomicilioComercio = _configuration?["Comercio:Domicilio"] ?? "",
                TipoComprobante = tipoComprobante,
                NumeroComprobante = numeroComprobante,
                MensajePie = _configuration?["Comercio:MensajePie"] ?? "Gracias por su compra!"
            };
        }
    }
}