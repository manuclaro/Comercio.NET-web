using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Comercio.NET.Testing
{
    class PruebaValidacionAfip
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== PRUEBA DE VALIDACIÓN AFIP ===\n");

            // Probar con configuración original
            Console.WriteLine("1. PRUEBA CON CONFIGURACIÓN ORIGINAL:");
            await ProbarValidacion("appsettings.json");

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            // Probar con configuración de prueba (CUIT inválido)
            Console.WriteLine("2. PRUEBA CON CUIT INVÁLIDO:");
            await ProbarValidacion("appsettings_prueba.json");

            Console.WriteLine("\nPresione cualquier tecla para continuar...");
            Console.ReadKey();
        }

        private static async Task ProbarValidacion(string configFile)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile(configFile, optional: false)
                    .Build();

                var afipSection = config.GetSection("AFIP");
                string cuit = afipSection["CUIT"];
                string certificadoPath = afipSection["CertificadoPath"];

                Console.WriteLine($"?? Archivo: {configFile}");
                Console.WriteLine($"?? CUIT: {cuit}");
                Console.WriteLine($"?? Certificado: {certificadoPath}");

                // Validar CUIT
                var (cuitValido, errorCuit) = ValidarCuit(cuit);
                Console.WriteLine($"? CUIT válido: {cuitValido}");
                if (!cuitValido) Console.WriteLine($"? Error CUIT: {errorCuit}");

                // Validar certificado
                var (certValido, errorCert) = ValidarCertificado(certificadoPath);
                Console.WriteLine($"?? Certificado válido: {certValido}");
                if (!certValido) Console.WriteLine($"?? Error Certificado: {errorCert}");

                // Resultado general
                bool configuracionValida = cuitValido && certValido;
                Console.WriteLine($"\n?? RESULTADO: {(configuracionValida ? "? CONFIGURACIÓN VÁLIDA" : "? CONFIGURACIÓN INVÁLIDA")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? ERROR: {ex.Message}");
            }
        }

        private static (bool valido, string error) ValidarCuit(string cuit)
        {
            if (string.IsNullOrWhiteSpace(cuit))
            {
                return (false, "CUIT no configurado");
            }

            // Validar formato de CUIT (debe ser numérico y tener 11 dígitos)
            string cuitLimpio = cuit.Replace("-", "");
            if (!long.TryParse(cuitLimpio, out long cuitNumerico) || cuitLimpio.Length != 11)
            {
                return (false, $"CUIT inválido: {cuit} (debe tener 11 dígitos numéricos)");
            }

            return (true, "CUIT válido");
        }

        private static (bool valido, string error) ValidarCertificado(string rutaCertificado)
        {
            if (string.IsNullOrWhiteSpace(rutaCertificado))
            {
                return (false, "Ruta de certificado no configurada");
            }

            if (!File.Exists(rutaCertificado))
            {
                return (false, $"Archivo de certificado no encontrado: {rutaCertificado}");
            }

            return (true, "Certificado encontrado");
        }
    }
}