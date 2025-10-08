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

        // NUEVO: Método para obtener nombre del comercio
        public static string ObtenerNombreComercio()
        {
            return _configuration?["Comercio:Nombre"] ?? "Mi Comercio";
        }

        // NUEVO: Método para obtener domicilio del comercio
        public static string ObtenerDomicilioComercio()
        {
            return _configuration?["Comercio:Domicilio"] ?? "";
        }

        public static TicketConfig ObtenerConfiguracionTicket(string tipoComprobante, string numeroComprobante)
        {
            return new TicketConfig
            {
                NombreComercio = ObtenerNombreComercio(),
                DomicilioComercio = ObtenerDomicilioComercio(),
                TipoComprobante = tipoComprobante,
                NumeroComprobante = numeroComprobante,
                MensajePie = _configuration?["Comercio:MensajePie"] ?? "Gracias por su compra!"
            };
        }

        // NUEVO: Método para obtener datos de facturación
        public static DatosFacturacion ObtenerDatosFacturacion()
        {
            return new DatosFacturacion
            {
                RazonSocial = _configuration?["Facturacion:RazonSocial"] ?? "",
                CUIT = _configuration?["Facturacion:CUIT"] ?? "",
                IngBrutos = _configuration?["Facturacion:IngBrutos"] ?? "",
                DomicilioFiscal = _configuration?["Facturacion:DomicilioFiscal"] ?? "",
                CodigoPostal = _configuration?["Facturacion:CodigoPostal"] ?? "",
                InicioActividades = _configuration?["Facturacion:InicioActividades"] ?? "",
                Condicion = _configuration?["Facturacion:Condicion"] ?? ""
            };
        }

        // NUEVO: Método para obtener configuración completa incluyendo datos de facturación para facturas
        public static TicketConfig ObtenerConfiguracionCompleta(string tipoComprobante, string numeroComprobante, bool incluirDatosFacturacion = false)
        {
            var config = ObtenerConfiguracionTicket(tipoComprobante, numeroComprobante);
            
            // Si es factura o se solicita explícitamente, cargar datos de facturación
            bool esFactura = tipoComprobante.Contains("Factura") || tipoComprobante.Contains("FACTURA");
            if (esFactura || incluirDatosFacturacion)
            {
                var datosFacturacion = ObtenerDatosFacturacion();
                // Los datos de facturación se manejan internamente en el TicketPrintingService
                // Este método queda preparado para futuras extensiones
            }

            return config;
        }

        // NUEVO: Método para validar que los datos de facturación estén completos
        public static (bool esValido, string mensajeError) ValidarDatosFacturacion()
        {
            var datos = ObtenerDatosFacturacion();

            if (string.IsNullOrEmpty(datos.RazonSocial))
                return (false, "La Razón Social es requerida en la configuración de facturación.");

            if (string.IsNullOrEmpty(datos.CUIT))
                return (false, "El CUIT es requerido en la configuración de facturación.");

            if (string.IsNullOrEmpty(datos.DomicilioFiscal))
                return (false, "El Domicilio Fiscal es requerido en la configuración de facturación.");

            if (string.IsNullOrEmpty(datos.Condicion))
                return (false, "La Condición ante IVA es requerida en la configuración de facturación.");

            return (true, "Configuración de facturación válida.");
        }
    }
}