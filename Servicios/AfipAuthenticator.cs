using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Net.Http;
using System.ServiceModel;
using System.Security.Cryptography.Pkcs;
using System.Collections.Generic;

namespace Comercio.NET.Servicios
{
    public static class AfipAuthenticator
    {
        // NUEVO: Caché de tokens por servicio
        private static readonly Dictionary<string, CachedToken> _tokenCache = new Dictionary<string, CachedToken>();

        // NUEVO: Clase para almacenar tokens en caché
        private class CachedToken
        {
            public string Token { get; set; }
            public string Sign { get; set; }
            public DateTime ExpirationTime { get; set; }

            public bool IsValid => DateTime.UtcNow < ExpirationTime.AddMinutes(-5); // 5 min de margen
        }

        public static async Task<(string token, string sign)> GetTAAsync(string service, string pfxPath, string pfxPassword, string wsaaUrl)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Iniciando autenticación para servicio: {service}");

                // NUEVO: Verificar caché primero
                if (_tokenCache.TryGetValue(service, out var cachedToken) && cachedToken.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Usando token en caché para servicio: {service}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Token expira en: {cachedToken.ExpirationTime}");
                    return (cachedToken.Token, cachedToken.Sign);
                }

                System.Diagnostics.Debug.WriteLine($"[AFIP] Token no encontrado en caché o expirado, obteniendo nuevo token...");

                // MODIFICADO: Intentar obtener token de AFIP con manejo especial
                try
                {
                    var (token, sign, expirationTime) = await TryGetNewTokenFromAfip(service, pfxPath, pfxPassword, wsaaUrl);

                    // NUEVO: Guardar en caché
                    _tokenCache[service] = new CachedToken
                    {
                        Token = token,
                        Sign = sign,
                        ExpirationTime = expirationTime
                    };

                    System.Diagnostics.Debug.WriteLine($"[AFIP] Token guardado en caché hasta: {expirationTime}");

                    return (token, sign);
                }
                catch (TokenAlreadyExistsException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Token ya existe en AFIP, implementando estrategia de espera...");

                    // NUEVO: Crear token placeholder con tiempo de espera
                    var placeholderToken = new CachedToken
                    {
                        Token = "WAITING_FOR_EXPIRY",
                        Sign = "WAITING_FOR_EXPIRY",
                        ExpirationTime = DateTime.UtcNow.AddMinutes(10) // Esperar 10 minutos
                    };

                    _tokenCache[service] = placeholderToken;

                    // NUEVO: Devolver token especial que indica que hay que esperar
                    return ("WAITING_FOR_EXPIRY", "WAITING_FOR_EXPIRY");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] ERROR: {ex.GetType().Name} - {ex.Message}");
                throw new Exception($"Error en autenticación AFIP: {ex.Message}", ex);
            }
        }

        // NUEVO: Método para intentar obtener token con manejo de "ya existe"
        private static async Task<(string token, string sign, DateTime expirationTime)> TryGetNewTokenFromAfip(
            string service, string pfxPath, string pfxPassword, string wsaaUrl)
        {
            // Verificar que el archivo del certificado existe
            if (!File.Exists(pfxPath))
            {
                throw new FileNotFoundException($"El certificado no se encuentra en: {pfxPath}");
            }

            System.Diagnostics.Debug.WriteLine($"[AFIP] Certificado encontrado: {pfxPath}");

            // Cargar el certificado con flags específicos para AFIP
            var certificate = new X509Certificate2(pfxPath, pfxPassword,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

            // Verificar validez del certificado
            DateTime now = DateTime.Now;
            if (certificate.NotAfter < now)
            {
                throw new Exception($"⚠️ El certificado ha expirado el {certificate.NotAfter:dd/MM/yyyy}. Debe solicitar uno nuevo a AFIP.");
            }

            if (certificate.NotBefore > now)
            {
                throw new Exception($"⚠️ El certificado aún no es válido. Será válido desde el {certificate.NotBefore:dd/MM/yyyy}.");
            }

            System.Diagnostics.Debug.WriteLine($"[AFIP] Certificado válido: {certificate.NotBefore:dd/MM/yyyy} - {certificate.NotAfter:dd/MM/yyyy}");
            System.Diagnostics.Debug.WriteLine($"[AFIP] Subject: {certificate.Subject}");

            // Crear el request XML para WSAA
            string tra = CreateTRA(service);
            System.Diagnostics.Debug.WriteLine($"[AFIP] TRA creado para servicio: {service}");

            // Firmar el TRA
            string cms = SignTRA(tra, certificate);
            System.Diagnostics.Debug.WriteLine($"[AFIP] TRA firmado exitosamente");

            // Enviar a WSAA y obtener respuesta
            string taXml = await SendToWSAA(cms, wsaaUrl);
            System.Diagnostics.Debug.WriteLine($"[AFIP] Respuesta WSAA recibida");

            // Extraer token y sign del XML de respuesta
            return ExtractTokenAndSign(taXml);
        }

        // CORREGIDO: Formato exacto del TRA según especificaciones AFIP
        private static string CreateTRA(string service)
        {
            DateTime now = DateTime.UtcNow; // IMPORTANTE: Usar UTC para AFIP
            DateTime from = now.AddMinutes(-10);
            DateTime to = now.AddHours(12);

            long uniqueId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // CORREGIDO: Formato exacto esperado por AFIP (sin millisegundos y con Z al final)
            string generationTime = from.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";
            string expirationTime = to.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";

            string tra = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<loginTicketRequest version=""1.0"">
<header>
<uniqueId>{uniqueId}</uniqueId>
<generationTime>{generationTime}</generationTime>
<expirationTime>{expirationTime}</expirationTime>
</header>
<service>{service}</service>
</loginTicketRequest>";

            System.Diagnostics.Debug.WriteLine($"[AFIP] TRA XML generado:");
            System.Diagnostics.Debug.WriteLine(tra);

            return tra;
        }

        // CORREGIDO: Firma del TRA con algoritmos específicos para AFIP
        private static string SignTRA(string tra, X509Certificate2 certificate)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Iniciando firma del TRA...");

                // Convertir el TRA a bytes usando UTF-8
                byte[] traBytes = Encoding.UTF8.GetBytes(tra);

                // Crear ContentInfo
                ContentInfo contentInfo = new ContentInfo(traBytes);

                // Crear SignedCms con detached = false (el contenido va incluido)
                SignedCms signedCms = new SignedCms(contentInfo, false);

                // Crear CmsSigner con configuración específica para AFIP
                CmsSigner signer = new CmsSigner(certificate);

                // IMPORTANTE: Configuraciones específicas para AFIP
                signer.DigestAlgorithm = new System.Security.Cryptography.Oid("2.16.840.1.101.3.4.2.1"); // SHA-256
                signer.IncludeOption = X509IncludeOption.ExcludeRoot; // No incluir certificado raíz

                // Agregar certificado a la colección
                signer.Certificates.Add(certificate);

                System.Diagnostics.Debug.WriteLine($"[AFIP] Configuración del signer completada");
                System.Diagnostics.Debug.WriteLine($"[AFIP] Digest Algorithm: {signer.DigestAlgorithm.Value}");
                System.Diagnostics.Debug.WriteLine($"[AFIP] Include Option: {signer.IncludeOption}");

                // Firmar el contenido
                signedCms.ComputeSignature(signer);

                // Obtener los bytes firmados
                byte[] signedBytes = signedCms.Encode();

                // Convertir a Base64
                string result = Convert.ToBase64String(signedBytes);

                System.Diagnostics.Debug.WriteLine($"[AFIP] CMS firmado exitosamente. Length: {result.Length}");
                System.Diagnostics.Debug.WriteLine($"[AFIP] Primeros 100 caracteres: {result.Substring(0, Math.Min(100, result.Length))}");

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] ERROR al firmar TRA: {ex.GetType().Name} - {ex.Message}");
                throw new Exception($"Error al firmar TRA: {ex.Message}", ex);
            }
        }

        // CORREGIDO: Formato del SOAP mejorado
        private static async Task<string> SendToWSAA(string cms, string wsaaUrl)
        {
            try
            {
                // CORREGIDO: SOAP envelope con formato exacto para AFIP
                string soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
<soap:Body>
<loginCms xmlns=""http://wsaa.view.sua.dvadac.desein.afip.gov"">
<in0>{cms}</in0>
</loginCms>
</soap:Body>
</soap:Envelope>";

                System.Diagnostics.Debug.WriteLine($"[AFIP] Enviando solicitud a: {wsaaUrl}");
                System.Diagnostics.Debug.WriteLine($"[AFIP] SOAP Envelope Length: {soapEnvelope.Length}");

                using (var client = new HttpClient())
                {
                    // IMPORTANTE: Configuración específica para AFIP
                    client.Timeout = TimeSpan.FromSeconds(60); // Mayor timeout
                    client.DefaultRequestHeaders.Clear();

                    var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

                    // CORREGIDO: Headers específicos para AFIP
                    content.Headers.Clear();
                    content.Headers.Add("Content-Type", "text/xml; charset=utf-8");
                    content.Headers.Add("SOAPAction", "http://wsaa.view.sua.dvadac.desein.afip.gov/loginCms");

                    System.Diagnostics.Debug.WriteLine($"[AFIP] Headers configurados:");
                    foreach (var header in content.Headers)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] {header.Key}: {string.Join(", ", header.Value)}");
                    }

                    var response = await client.PostAsync(wsaaUrl, content);

                    System.Diagnostics.Debug.WriteLine($"[AFIP] Status Code: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Reason Phrase: {response.ReasonPhrase}");

                    string responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Response Content Length: {responseContent.Length}");

                    // NUEVO: Verificar si es un SOAP Fault antes de procesar como error HTTP
                    if (responseContent.Contains("<soapenv:Fault>") || responseContent.Contains("<soap:Fault>"))
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] SOAP Fault detectado, procesando...");
                        HandleSoapFault(responseContent);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] Error Response Content:");
                        System.Diagnostics.Debug.WriteLine(responseContent);

                        // MEJORADO: Análisis más detallado del error
                        string mensaje = response.StatusCode switch
                        {
                            System.Net.HttpStatusCode.InternalServerError =>
                                "🔧 Error 500 - Problema en el servidor AFIP\n\n" +
                                "💡 Causas más comunes:\n" +
                                "• Formato incorrecto del TRA (fechas, uniqueId)\n" +
                                "• Error en la firma digital (CMS)\n" +
                                "• Certificado con problemas de formato\n" +
                                "• Algoritmo de hash incorrecto\n\n" +
                                "🔍 Revise los logs de debug para más detalles",

                            System.Net.HttpStatusCode.BadRequest =>
                                "❌ Solicitud inválida (Error 400).\n\n" +
                                "💡 Posibles causas:\n" +
                                "• SOAP envelope mal formateado\n" +
                                "• CMS (mensaje firmado) inválido",

                            System.Net.HttpStatusCode.Unauthorized =>
                                "🔐 No autorizado (Error 401).\n\n" +
                                "💡 Verifique:\n" +
                                "• Certificado válido y no expirado\n" +
                                "• Contraseña del certificado correcta",

                            _ => $"Error HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                        };

                        throw new System.Net.Http.HttpRequestException(mensaje + $"\n\nRespuesta completa:\n{responseContent}");
                    }

                    System.Diagnostics.Debug.WriteLine($"[AFIP] Response Content (primeros 500 chars):");
                    System.Diagnostics.Debug.WriteLine(responseContent.Substring(0, Math.Min(500, responseContent.Length)));

                    return responseContent;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] ERROR en SendToWSAA: {ex.GetType().Name} - {ex.Message}");
                throw new Exception($"Error al comunicarse con WSAA: {ex.Message}", ex);
            }
        }

        // NUEVO: Método para manejar SOAP Faults específicos de AFIP con excepción específica
        private static void HandleSoapFault(string soapResponse)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(soapResponse);

                var faultCodeNode = xmlDoc.SelectSingleNode("//faultcode") ??
                                   xmlDoc.SelectSingleNode("//*[local-name()='faultcode']");

                var faultStringNode = xmlDoc.SelectSingleNode("//faultstring") ??
                                     xmlDoc.SelectSingleNode("//*[local-name()='faultstring']");

                string faultCode = faultCodeNode?.InnerText ?? "Desconocido";
                string faultString = faultStringNode?.InnerText ?? "Error no especificado";

                System.Diagnostics.Debug.WriteLine($"[AFIP] SOAP Fault Code: {faultCode}");
                System.Diagnostics.Debug.WriteLine($"[AFIP] SOAP Fault String: {faultString}");

                // NUEVO: Manejar error específico de token ya existente con excepción específica
                if (faultCode.Contains("coe.alreadyAuthenticated"))
                {
                    throw new TokenAlreadyExistsException(
                        "Ya existe un token válido para este servicio en AFIP. El token anterior debe expirar antes de solicitar uno nuevo.",
                        faultString
                    );
                }

                // Otros errores SOAP
                throw new Exception($"SOAP Fault de AFIP: {faultString} (Código: {faultCode})");
            }
            catch (TokenAlreadyExistsException)
            {
                throw; // Re-lanzar excepciones específicas
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Error procesando SOAP Fault: {ex.Message}");
                throw new Exception($"Error procesando respuesta SOAP de AFIP: {ex.Message}");
            }
        }

        // CORREGIDO: Método para extraer token, sign y fecha de expiración
        private static (string token, string sign, DateTime expirationTime) ExtractTokenAndSign(string xmlResponse)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Extrayendo token y sign del XML...");

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlResponse);

                // CORREGIDO: Primero extraer el contenido de loginCmsReturn y decodificarlo
                var loginCmsReturnNode = xmlDoc.SelectSingleNode("//loginCmsReturn") ??
                                        xmlDoc.SelectSingleNode("//*[local-name()='loginCmsReturn']");

                if (loginCmsReturnNode == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] ERROR: No se encontró loginCmsReturn");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] XML Response completo:");
                    System.Diagnostics.Debug.WriteLine(xmlResponse);
                    throw new Exception("No se encontró el nodo loginCmsReturn en la respuesta de AFIP");
                }

                // NUEVO: Decodificar las entidades HTML del contenido
                string decodedXml = System.Net.WebUtility.HtmlDecode(loginCmsReturnNode.InnerText);

                System.Diagnostics.Debug.WriteLine($"[AFIP] XML decodificado:");
                System.Diagnostics.Debug.WriteLine(decodedXml);

                // NUEVO: Cargar el XML decodificado en un nuevo documento
                var decodedDoc = new XmlDocument();
                decodedDoc.LoadXml(decodedXml);

                // CORREGIDO: Buscar token y sign en el XML decodificado
                var tokenNode = decodedDoc.SelectSingleNode("//token") ??
                               decodedDoc.SelectSingleNode("//*[local-name()='token']");

                var signNode = decodedDoc.SelectSingleNode("//sign") ??
                              decodedDoc.SelectSingleNode("//*[local-name()='sign']");

                // NUEVO: Extraer fecha de expiración
                var expirationNode = decodedDoc.SelectSingleNode("//expirationTime") ??
                                    decodedDoc.SelectSingleNode("//*[local-name()='expirationTime']");

                if (tokenNode == null || signNode == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] ERROR: No se encontraron nodos token/sign en XML decodificado");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] XML decodificado completo:");
                    System.Diagnostics.Debug.WriteLine(decodedXml);

                    // Intentar buscar errores en la respuesta
                    var errorNode = decodedDoc.SelectSingleNode("//faultstring") ??
                                   decodedDoc.SelectSingleNode("//*[local-name()='faultstring']");

                    if (errorNode != null)
                    {
                        throw new Exception($"Error de AFIP: {errorNode.InnerText}");
                    }

                    throw new Exception("No se pudo extraer token y sign del XML decodificado de AFIP");
                }

                string token = tokenNode.InnerText;
                string sign = signNode.InnerText;

                // NUEVO: Parsear fecha de expiración
                DateTime expirationTime = DateTime.UtcNow.AddHours(12); // Default 12 horas
                if (expirationNode != null && DateTime.TryParse(expirationNode.InnerText, out var parsedExpiration))
                {
                    expirationTime = parsedExpiration.ToUniversalTime();
                }

                System.Diagnostics.Debug.WriteLine($"[AFIP] Token extraído exitosamente. Length: {token.Length}");
                System.Diagnostics.Debug.WriteLine($"[AFIP] Sign extraído exitosamente. Length: {sign.Length}");
                System.Diagnostics.Debug.WriteLine($"[AFIP] Expiration Time: {expirationTime}");
                System.Diagnostics.Debug.WriteLine($"[AFIP] Token (primeros 100 chars): {token.Substring(0, Math.Min(100, token.Length))}");
                System.Diagnostics.Debug.WriteLine($"[AFIP] Sign (primeros 50 chars): {sign.Substring(0, Math.Min(50, sign.Length))}");

                return (token, sign, expirationTime);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] ERROR en ExtractTokenAndSign: {ex.Message}");
                throw new Exception($"Error al extraer token y sign: {ex.Message}", ex);
            }
        }

        // NUEVO: Método para limpiar caché de tokens
        public static void ClearTokenCache()
        {
            _tokenCache.Clear();
            System.Diagnostics.Debug.WriteLine($"[AFIP] Caché de tokens limpiado");
        }

        // NUEVO: Método para limpiar caché de un servicio específico
        public static void ClearTokenCache(string service)
        {
            if (_tokenCache.Remove(service))
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Token en caché eliminado para servicio: {service}");
            }
        }

        // CORREGIDO: Método estático para verificar estado del servicio AFIP
        public static async Task<bool> VerificarEstadoServicioAfipAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10); // Timeout más corto para verificación

                // URL del servicio WSAA de homologación
                string urlHomologacion = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms?wsdl";

                var response = await client.GetAsync(urlHomologacion);

                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Servicio AFIP disponible");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error AFIP: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error conectividad AFIP: {ex.Message}");
                return false;
            }
        }

        // NUEVO: Método para verificar validez del certificado con más detalles
        public static (bool valido, string mensaje, DateTime? vence) VerificarCertificado(string pfxPath, string pfxPassword)
        {
            try
            {
                if (!File.Exists(pfxPath))
                {
                    return (false, "Certificado no encontrado", null);
                }

                var certificate = new X509Certificate2(pfxPath, pfxPassword,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

                DateTime now = DateTime.Now;

                System.Diagnostics.Debug.WriteLine($"[CERT] Subject: {certificate.Subject}");
                System.Diagnostics.Debug.WriteLine($"[CERT] Issuer: {certificate.Issuer}");
                System.Diagnostics.Debug.WriteLine($"[CERT] Serial: {certificate.SerialNumber}");
                System.Diagnostics.Debug.WriteLine($"[CERT] Valid From: {certificate.NotBefore}");
                System.Diagnostics.Debug.WriteLine($"[CERT] Valid To: {certificate.NotAfter}");
                System.Diagnostics.Debug.WriteLine($"[CERT] Has Private Key: {certificate.HasPrivateKey}");

                if (certificate.NotAfter < now)
                {
                    return (false, $"Certificado expirado el {certificate.NotAfter:dd/MM/yyyy}", certificate.NotAfter);
                }

                if (certificate.NotBefore > now)
                {
                    return (false, $"Certificado será válido desde el {certificate.NotBefore:dd/MM/yyyy}", certificate.NotBefore);
                }

                if (!certificate.HasPrivateKey)
                {
                    return (false, "El certificado no tiene clave privada", certificate.NotAfter);
                }

                // Verificar si está próximo a vencer (30 días)
                if ((certificate.NotAfter - now).TotalDays <= 30)
                {
                    return (true, $"⚠️ Certificado válido pero vence pronto: {certificate.NotAfter:dd/MM/yyyy}", certificate.NotAfter);
                }

                return (true, $"Certificado válido hasta {certificate.NotAfter:dd/MM/yyyy}", certificate.NotAfter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CERT] ERROR: {ex.Message}");
                return (false, $"Error verificando certificado: {ex.Message}", null);
            }
        }

        // RESTO DE MÉTODOS SIN CAMBIOS...
        public static async Task<(bool exito, string mensaje)> AutenticarAfipAsync()
        {
            try
            {
                if (!await VerificarEstadoServicioAfipAsync())
                {
                    return (false, "⚠️ Los servicios de AFIP no están disponibles temporalmente. Intente más tarde.");
                }

                return (true, "Autenticación AFIP exitosa");
            }
            catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("500"))
            {
                return (false, "🔧 AFIP está en mantenimiento. Intente en unos minutos.");
            }
            catch (TaskCanceledException)
            {
                return (false, "⏱️ Tiempo de espera agotado. Verifique su conexión a internet.");
            }
            catch (Exception ex)
            {
                return (false, $"❌ Error AFIP: {ex.Message}");
            }
        }

        public static async Task<(bool exito, string mensaje, string token, string sign)> AutenticarAfipConParametrosAsync(
            string service, string pfxPath, string pfxPassword, string wsaaUrl)
        {
            try
            {
                if (!await VerificarEstadoServicioAfipAsync())
                {
                    return (false, "⚠️ Los servicios de AFIP no están disponibles temporalmente. Intente más tarde.", null, null);
                }

                var (token, sign) = await GetTAAsync(service, pfxPath, pfxPassword, wsaaUrl);

                return (true, "Autenticación AFIP exitosa", token, sign);
            }
            catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("500"))
            {
                return (false, "🔧 AFIP está en mantenimiento. Intente en unos minutos.", null, null);
            }
            catch (TaskCanceledException)
            {
                return (false, "⏱️ Tiempo de espera agotado. Verifique su conexión a internet.", null, null);
            }
            catch (Exception ex)
            {
                return (false, $"❌ Error AFIP: {ex.Message}", null, null);
            }
        }

        // CORREGIDO: Método para obtener token existente del caché
        public static (string token, string sign)? GetExistingToken(string service)
        {
            try
            {
                if (_tokenCache.TryGetValue(service, out var cached) && 
                    !string.IsNullOrEmpty(cached.Token) && 
                    !string.IsNullOrEmpty(cached.Sign) && 
                    cached.ExpirationTime > DateTime.UtcNow.AddMinutes(5))
                {
                    return (cached.Token, cached.Sign);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    // NUEVO: Excepción específica para cuando ya existe un token en AFIP
    public class TokenAlreadyExistsException : Exception
    {
        public string FaultString { get; }

        public TokenAlreadyExistsException(string message, string faultString) : base(message)
        {
            FaultString = faultString;
        }
    }
}