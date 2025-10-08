using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;

namespace Comercio.NET.Testing
{
    class DebugValidacionAfip
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("?? === DEBUG VALIDACIÓN AFIP ===\n");

            try
            {
                // Mostrar información del archivo de configuración
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                Console.WriteLine($"?? Archivo de configuración: {configPath}");
                Console.WriteLine($"?? Archivo existe: {File.Exists(configPath)}");
                
                if (File.Exists(configPath))
                {
                    string contenido = await File.ReadAllTextAsync(configPath);
                    Console.WriteLine($"?? Tamaño del archivo: {contenido.Length} caracteres");
                }

                Console.WriteLine("\n" + new string('=', 60));

                // 1. VERIFICAR CONECTIVIDAD
                Console.WriteLine("\n?? PASO 1: VERIFICANDO CONECTIVIDAD AFIP");
                bool servicioDisponible = await VerificarConectividadAfip();
                Console.WriteLine($"?? Resultado: {(servicioDisponible ? "? CONECTADO" : "? NO CONECTADO")}");

                // 2. VERIFICAR CONFIGURACIÓN
                Console.WriteLine("\n?? PASO 2: VERIFICANDO CONFIGURACIÓN");
                var (configValida, errorConfig) = await VerificarConfiguracionAfip();
                Console.WriteLine($"?? Resultado: {(configValida ? "? VÁLIDA" : "? INVÁLIDA")}");
                if (!configValida) Console.WriteLine($"?? Error: {errorConfig}");

                // 3. VERIFICAR CERTIFICADOS
                Console.WriteLine("\n?? PASO 3: VERIFICANDO CERTIFICADOS");
                var (certValido, errorCert) = VerificarCertificados();
                Console.WriteLine($"?? Resultado: {(certValido ? "? VÁLIDO" : "? INVÁLIDO")}");
                if (!certValido) Console.WriteLine($"?? Error: {errorCert}");

                // 4. RESULTADO FINAL
                Console.WriteLine("\n?? RESULTADO FINAL:");
                if (!servicioDisponible)
                {
                    Console.WriteLine("?? ESTADO: NO DISPONIBLE - Servicio AFIP no responde");
                }
                else if (!configValida)
                {
                    Console.WriteLine($"?? ESTADO: ERROR CONFIGURACIÓN - {errorConfig}");
                }
                else if (!certValido && !errorCert.Contains("no configurados"))
                {
                    Console.WriteLine($"?? ESTADO: ERROR CERTIFICADO - {errorCert}");
                }
                else
                {
                    Console.WriteLine("?? ESTADO: CONECTADO - Todo funcionando correctamente");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? ERROR CRÍTICO: {ex.Message}");
                Console.WriteLine($"?? StackTrace: {ex.StackTrace}");
            }

            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("Presione cualquier tecla para continuar...");
            Console.ReadKey();
        }

        private static async Task<bool> VerificarConectividadAfip()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                
                string urlHomologacion = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms?wsdl";
                Console.WriteLine($"?? Probando URL: {urlHomologacion}");
                
                var response = await client.GetAsync(urlHomologacion);
                
                Console.WriteLine($"?? Status Code: {response.StatusCode}");
                Console.WriteLine($"?? Es exitoso: {response.IsSuccessStatusCode}");
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? Error conectividad: {ex.Message}");
                return false;
            }
        }

        private static async Task<(bool valida, string error)> VerificarConfiguracionAfip()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

                Console.WriteLine($"?? Configuración cargada correctamente");

                var afipSection = config.GetSection("AFIP");
                Console.WriteLine($"?? Sección AFIP existe: {afipSection.Exists()}");
                
                // Mostrar todas las claves disponibles
                var children = afipSection.GetChildren();
                Console.WriteLine("?? Claves encontradas en sección AFIP:");
                foreach (var child in children)
                {
                    Console.WriteLine($"??   - {child.Key}: '{child.Value}'");
                }
                
                // Verificar CUIT
                string cuitEmisor = afipSection["CUIT"];
                Console.WriteLine($"?? CUIT leído: '{cuitEmisor}'");
                
                if (string.IsNullOrWhiteSpace(cuitEmisor))
                {
                    return (false, "CUIT no configurado en sección AFIP");
                }

                // Validar formato de CUIT
                string cuitLimpio = cuitEmisor.Replace("-", "");
                Console.WriteLine($"?? CUIT limpio: '{cuitLimpio}' (longitud: {cuitLimpio.Length})");
                
                bool esNumerico = long.TryParse(cuitLimpio, out long cuitNumerico);
                Console.WriteLine($"?? Es numérico: {esNumerico}");
                Console.WriteLine($"?? Longitud correcta: {cuitLimpio.Length == 11}");
                
                if (!esNumerico || cuitLimpio.Length != 11)
                {
                    return (false, $"CUIT inválido: {cuitEmisor} (debe tener 11 dígitos numéricos)");
                }

                // Verificar si es un CUIT de prueba
                if (cuitEmisor == "12345678901" || cuitEmisor == "11111111111" || cuitEmisor == "00000000000")
                {
                    return (false, $"CUIT de prueba detectado: {cuitEmisor}");
                }

                // Verificar URLs
                string wsaaUrl = afipSection["WSAAUrl"];
                string wsfeUrl = afipSection["WSFEUrl"];
                
                Console.WriteLine($"?? WSAA URL: '{wsaaUrl}'");
                Console.WriteLine($"?? WSFE URL: '{wsfeUrl}'");
                
                if (string.IsNullOrWhiteSpace(wsaaUrl) || string.IsNullOrWhiteSpace(wsfeUrl))
                {
                    return (false, "URLs de servicios WSAA/WSFE no configuradas");
                }

                return (true, "Configuración AFIP válida");
            }
            catch (Exception ex)
            {
                return (false, $"Error leyendo configuración AFIP: {ex.Message}");
            }
        }

        private static (bool valido, string error) VerificarCertificados()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

                var afipSection = config.GetSection("AFIP");
                string pfxPath = afipSection["CertificadoPath"];
                string pfxPassword = afipSection["CertificadoPassword"];

                Console.WriteLine($"?? Ruta certificado: '{pfxPath}'");
                Console.WriteLine($"?? Password configurado: {!string.IsNullOrWhiteSpace(pfxPassword)}");

                if (string.IsNullOrWhiteSpace(pfxPath))
                {
                    return (false, "Certificados no configurados");
                }

                Console.WriteLine($"?? Archivo existe: {File.Exists(pfxPath)}");

                if (!File.Exists(pfxPath))
                {
                    return (false, $"Archivo de certificado no encontrado: {pfxPath}");
                }

                return (true, "Certificado encontrado");
            }
            catch (Exception ex)
            {
                return (false, $"Error verificando certificados: {ex.Message}");
            }
        }
    }
}