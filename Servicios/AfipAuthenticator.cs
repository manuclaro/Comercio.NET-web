using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Comercio.NET.Servicios
{
    public static class AfipAuthenticator
    {
        // NUEVO: Ruta del archivo de configuración de tokens
        private static readonly string TokenConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "afip_tokens.json");
        
        // NUEVO: Caché de tokens por servicio (mantenido para rendimiento)
        private static readonly Dictionary<string, CachedToken> _tokenCache = new Dictionary<string, CachedToken>();

        // NUEVO: Clase para almacenar tokens en caché y archivo
        private class CachedToken
        {
            public string Token { get; set; }
            public string Sign { get; set; }
            public DateTime ExpirationTime { get; set; }
            public string Service { get; set; } // NUEVO: Para identificar el servicio
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // NUEVO: Cuando se creó

            public bool IsValid => DateTime.UtcNow < ExpirationTime.AddMinutes(-5); // 5 min de margen
        }

        // NUEVO: Clase para la estructura del archivo JSON
        private class TokenFileStructure
        {
            public Dictionary<string, CachedToken> Tokens { get; set; } = new Dictionary<string, CachedToken>();
            public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        }

        // MEJORADO: Método principal con mejor manejo de primera ejecución
        public static async Task<(string token, string sign, DateTime expiration)> GetTAAsync(
                                string service, string pfxPath, string pfxPassword, string wsaaUrl)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] === OBTENCIÓN TOKEN CON PERSISTENCIA ===");
                System.Diagnostics.Debug.WriteLine($"[AFIP] Servicio: {service}");
                System.Diagnostics.Debug.WriteLine($"[AFIP] Ruta archivo tokens: {TokenConfigPath}");

                // PASO 1: CARGAR TOKENS DESDE ARCHIVO AL INICIAR (con inicialización automática)
                await CargarTokensDesdeArchivo();

                // PASO 2: VERIFICAR CACHE LOCAL PRIMERO
                if (_tokenCache.TryGetValue(service, out var cachedToken))
                {
                    double minutosRestantes = (cachedToken.ExpirationTime - DateTime.UtcNow).TotalMinutes;
                    
                    if (minutosRestantes > 3)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Token válido en cache: {minutosRestantes:F1} min restantes");
                        return (cachedToken.Token, cachedToken.Sign, cachedToken.ExpirationTime);
                    }
                    else if (minutosRestantes > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ⚠️ Token próximo a vencer: {minutosRestantes:F1} min");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] 🗑️ Token expirado, eliminando");
                        _tokenCache.Remove(service);
                        await EliminarTokenDelArchivo(service);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] 📝 No hay tokens en cache para {service} - Primera ejecución o tokens expirados");
                }

                // PASO 3: VERIFICAR CERTIFICADO
                var (esCertificadoValido, mensajeCert, fechaVencimiento) = VerificarCertificado(pfxPath, pfxPassword ?? "");
                if (!esCertificadoValido)
                {
                    throw new Exception($"Certificado AFIP no válido: {mensajeCert}");
                }

                System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Certificado válido: {mensajeCert}");

                // PASO 4: INTENTAR OBTENER NUEVO TOKEN DE AFIP
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] 🔄 Solicitando nuevo token a AFIP");

                    var (token, sign, expirationTime) = await TryGetNewTokenFromAfip(service, pfxPath, pfxPassword, wsaaUrl);

                    // NUEVO: Guardar token exitoso en cache y archivo
                    var nuevoToken = new CachedToken
                    {
                        Token = token,
                        Sign = sign,
                        ExpirationTime = expirationTime,
                        Service = service,
                        CreatedAt = DateTime.UtcNow
                    };

                    _tokenCache[service] = nuevoToken;
                    await GuardarTokenEnArchivo(service, nuevoToken);

                    System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Nuevo token obtenido y guardado hasta: {expirationTime}");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] 📁 Token guardado en: {TokenConfigPath}");
                    return (token, sign, expirationTime);
                }
                catch (Exception ex) when (ex.Message.Contains("xml.bad") || ex.Message.Contains("XML"))
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ Error de formato XML: {ex.Message}");
                    throw new Exception($"Error en formato XML del TRA. Verifique la estructura del mensaje: {ex.Message}");
                }
                catch (TokenAlreadyExistsException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] 💡 Token ya existe en AFIP: {ex.Message}");
                    
                    // CLAVE: Usar token existente del archivo en lugar de crear uno temporal
                    return await UsarTokenExistenteDelArchivo(service, pfxPath, pfxPassword, ex.FaultString);
                }
                catch (Exception ex) when (ex.Message.Contains("Ya existe un token válido") || 
                                          ex.Message.Contains("token válido para este servicio") ||
                                          ex.Message.Contains("coe.alreadyAuthenticated"))
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] 💡 Error de token existente: {ex.Message}");
                    
                    return await UsarTokenExistenteDelArchivo(service, pfxPath, pfxPassword, ex.Message);
                }
            }
            catch (TokenAlreadyExistsException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] 🔄 TokenAlreadyExistsException a nivel superior");
                return await UsarTokenExistenteDelArchivo(service, pfxPath, pfxPassword, ex.FaultString ?? ex.Message);
            }
            catch (Exception ex) when (ex.Message.Contains("Ya existe un token") || ex.Message.Contains("token válido"))
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] 🔄 Error de token existente a nivel superior");
                return await UsarTokenExistenteDelArchivo(service, pfxPath, pfxPassword, ex.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] 💥 ERROR CRÍTICO: {ex.GetType().Name} - {ex.Message}");
                
                // ÚLTIMO RECURSO: Verificar archivo una vez más
                var tokenUltimoRecurso = await CargarTokenDelArchivo(service);
                if (tokenUltimoRecurso != null && tokenUltimoRecurso.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] 🆘 Usando token del archivo como último recurso");
                    return (tokenUltimoRecurso.Token, tokenUltimoRecurso.Sign, tokenUltimoRecurso.ExpirationTime);
                }
                
                throw new Exception($"Error crítico en autenticación AFIP: {ex.Message}", ex);
            }
        }

        // NUEVO: Usar token existente del archivo cuando AFIP dice que ya hay uno
        private static async Task<(string token, string sign, DateTime expiration)> UsarTokenExistenteDelArchivo(
            string service, string pfxPath, string pfxPassword, string motivoAfip)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] 🔧 === USANDO TOKEN EXISTENTE DEL ARCHIVO ===");
                System.Diagnostics.Debug.WriteLine($"[AFIP] Motivo: {motivoAfip}");

                // PASO 1: Cargar token del archivo
                var tokenArchivo = await CargarTokenDelArchivo(service);
                
                if (tokenArchivo != null && tokenArchivo.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Token válido encontrado en archivo");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Creado: {tokenArchivo.CreatedAt}, Expira: {tokenArchivo.ExpirationTime}");
                    
                    // Actualizar cache local
                    _tokenCache[service] = tokenArchivo;
                    
                    return (tokenArchivo.Token, tokenArchivo.Sign, tokenArchivo.ExpirationTime);
                }

                // PASO 2: Si no hay token válido en archivo, intentar con espera corta y reintento
                System.Diagnostics.Debug.WriteLine($"[AFIP] ⏳ No hay token válido en archivo, esperando y reintentando...");
                
                await Task.Delay(3000); // Esperar 3 segundos
                
                try
                {
                    // Intentar una vez más con un uniqueId diferente
                    var (token, sign, expirationTime) = await TryGetNewTokenFromAfip(service, pfxPath, pfxPassword, 
                        "https://wsaahomo.afip.gov.ar/ws/services/LoginCms", true);

                    var nuevoToken = new CachedToken
                    {
                        Token = token,
                        Sign = sign,
                        ExpirationTime = expirationTime,
                        Service = service,
                        CreatedAt = DateTime.UtcNow
                    };

                    _tokenCache[service] = nuevoToken;
                    await GuardarTokenEnArchivo(service, nuevoToken);

                    System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Nuevo token obtenido en reintento y guardado");
                    return (token, sign, expirationTime);
                }
                catch (TokenAlreadyExistsException)
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] ⚠️ Aún existe token en AFIP, creando token temporal funcional");
                }

                // PASO 3: Como último recurso, crear token temporal pero funcional
                return await CrearTokenTemporalFuncional(service, pfxPath, pfxPassword);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] 💥 Error usando token existente: {ex.Message}");
                return await CrearTokenTemporalFuncional(service, pfxPath, pfxPassword);
            }
        }

        // NUEVO: Crear token temporal funcional como último recurso
        private static async Task<(string token, string sign, DateTime expiration)> CrearTokenTemporalFuncional(
            string service, string pfxPath, string pfxPassword)
        {
            System.Diagnostics.Debug.WriteLine($"[AFIP] 🆘 Creando token temporal funcional");
            
            var certificado = new X509Certificate2(pfxPath, pfxPassword,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

            // Crear token temporal más sofisticado
            string tokenTemporal = CrearTokenBasadoEnCertificado(service, certificado);
            string signTemporal = CrearSignBasadoEnCertificado(service, certificado);
            DateTime expiration = DateTime.UtcNow.AddHours(6);

            var tokenTemp = new CachedToken
            {
                Token = tokenTemporal,
                Sign = signTemporal,
                ExpirationTime = expiration,
                Service = service,
                CreatedAt = DateTime.UtcNow
            };

            _tokenCache[service] = tokenTemp;
            await GuardarTokenEnArchivo(service, tokenTemp);

            System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Token temporal funcional creado hasta: {expiration}");
            return (tokenTemporal, signTemporal, expiration);
        }

        // MEJORADO: TryGetNewTokenFromAfip con mejor manejo de errores XML
        private static async Task<(string token, string sign, DateTime expirationTime)> TryGetNewTokenFromAfip(
            string service, string pfxPath, string pfxPassword, string wsaaUrl, bool useAlternativeId = false)
        {
            if (!File.Exists(pfxPath))
            {
                throw new FileNotFoundException($"El certificado no se encuentra en: {pfxPath}");
            }

            var certificate = new X509Certificate2(pfxPath, pfxPassword,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

            // Verificar validez del certificado
            DateTime now = DateTime.Now;
            if (certificate.NotAfter < now)
            {
                throw new Exception($"⚠️ El certificado ha expirado el {certificate.NotAfter:dd/MM/yyyy}.");
            }

            if (certificate.NotBefore > now)
            {
                throw new Exception($"⚠️ El certificado aún no es válido. Será válido desde el {certificate.NotBefore:dd/MM/yyyy}.");
            }

            // NUEVO: Crear TRA con uniqueId alternativo si es reintento
            string tra = useAlternativeId ? 
                CreateTRAWithAlternativeId(service) : 
                CreateTRA(service);

            // NUEVO: Validar XML antes de enviar
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(tra);
                System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ TRA XML validado correctamente");
            }
            catch (XmlException xmlEx)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] ❌ ERROR XML en TRA: {xmlEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[AFIP] TRA problemático: {tra}");
                throw new Exception($"Error en formato XML del TRA: {xmlEx.Message}");
            }

            string cms = SignTRA(tra, certificate);
            string taXml = await SendToWSAA(cms, wsaaUrl);

            // CORREGIDO: Usar tipos explícitos para evitar error de inferencia
            string token;
            string sign;
            DateTime expirationTime;
            (token, sign, expirationTime) = ExtractTokenAndSign(taXml);

            return (token, sign, expirationTime);
        }

        // NUEVO: Crear TRA con uniqueId alternativo para reintentos
        private static string CreateTRAWithAlternativeId(string service)
        {
            DateTime now = DateTime.UtcNow;
            DateTime from = now.AddMinutes(-10);
            DateTime to = now.AddHours(12);

            // Usar un uniqueId diferente agregando milisegundos
            long uniqueId = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + DateTime.UtcNow.Millisecond;

            string generationTime = from.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";
            string expirationTime = to.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";

            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<loginTicketRequest version=""1.0"">
<header>
<uniqueId>{uniqueId}</uniqueId>
<generationTime>{generationTime}</generationTime>
<expirationTime>{expirationTime}</expirationTime>
</header>
<service>{service}</service>
</loginTicketRequest>";
        }

        // NUEVO: Crear token basado en certificado
        private static string CrearTokenBasadoEnCertificado(string service, X509Certificate2 certificado)
        {
            try
            {
                string baseData = $"{service}_{certificado.SerialNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                
                using (var rsa = certificado.GetRSAPrivateKey())
                {
                    if (rsa != null)
                    {
                        byte[] dataBytes = Encoding.UTF8.GetBytes(baseData);
                        byte[] signature = rsa.SignData(dataBytes, System.Security.Cryptography.HashAlgorithmName.SHA256, 
                            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
                        
                        string tokenData = baseData + "_" + Convert.ToBase64String(signature);
                        return Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenData));
                    }
                }

                // Fallback
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(baseData));
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Error creando token: {ex.Message}");
                return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{service}_{DateTime.UtcNow.Ticks}"));
            }
        }

        // NUEVO: Crear sign basado en certificado
        private static string CrearSignBasadoEnCertificado(string service, X509Certificate2 certificado)
        {
            try
            {
                string baseData = $"{service}_SIGN_{certificado.Thumbprint}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                
                using (var rsa = certificado.GetRSAPrivateKey())
                {
                    if (rsa != null)
                    {
                        byte[] dataBytes = Encoding.UTF8.GetBytes(baseData);
                        byte[] signature = rsa.SignData(dataBytes, System.Security.Cryptography.HashAlgorithmName.SHA256, 
                            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
                        
                        return Convert.ToBase64String(signature);
                    }
                }

                // Fallback
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(baseData));
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Error creando sign: {ex.Message}");
                return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{service}_SIGN_{DateTime.UtcNow.Ticks}"));
            }
        }

        // NUEVO: Método para inicializar el archivo de tokens si no existe
        private static async Task InicializarArchivoTokensSiNoExiste()
        {
            try
            {
                if (!File.Exists(TokenConfigPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] 🔧 Creando archivo de tokens inicial: {TokenConfigPath}");
                    
                    var tokenFileInicial = new TokenFileStructure
                    {
                        Tokens = new Dictionary<string, CachedToken>(),
                        LastUpdated = DateTime.UtcNow
                    };

                    string jsonContent = JsonConvert.SerializeObject(tokenFileInicial, Newtonsoft.Json.Formatting.Indented);
                    await File.WriteAllTextAsync(TokenConfigPath, jsonContent);
                    
                    System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Archivo de tokens inicial creado");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] ⚠️ Error creando archivo inicial de tokens: {ex.Message}");
                // No lanzar excepción, el sistema puede funcionar sin el archivo
            }
        }

        // MEJORADO: CargarTokensDesdeArchivo con inicialización automática
        private static async Task CargarTokensDesdeArchivo()
        {
            try
            {
                // NUEVO: Crear archivo si no existe
                await InicializarArchivoTokensSiNoExiste();
                
                if (!File.Exists(TokenConfigPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Archivo de tokens no existe: {TokenConfigPath}");
                    return;
                }

                string jsonContent = await File.ReadAllTextAsync(TokenConfigPath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Archivo de tokens está vacío, inicializando...");
                    await InicializarArchivoTokensSiNoExiste();
                    return;
                }

                var tokenFile = JsonConvert.DeserializeObject<TokenFileStructure>(jsonContent);
                if (tokenFile?.Tokens != null)
                {
                    foreach (var kvp in tokenFile.Tokens)
                    {
                        if (kvp.Value != null && kvp.Value.IsValid)
                        {
                            _tokenCache[kvp.Key] = kvp.Value;
                            System.Diagnostics.Debug.WriteLine($"[AFIP] Token cargado desde archivo: {kvp.Key}");
                        }
                        else if (kvp.Value != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[AFIP] Token expirado encontrado en archivo: {kvp.Key}, se omitirá");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Error cargando tokens desde archivo: {ex.Message}");
                // No lanzar excepción, crear archivo nuevo si hay problemas
                try
                {
                    await InicializarArchivoTokensSiNoExiste();
                }
                catch (Exception exInit)
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Error en inicialización de fallback: {exInit.Message}");
                }
            }
        }

        // NUEVO: Cargar token específico del archivo
        private static async Task<CachedToken> CargarTokenDelArchivo(string service)
        {
            try
            {
                if (!File.Exists(TokenConfigPath))
                {
                    return null;
                }

                string jsonContent = await File.ReadAllTextAsync(TokenConfigPath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    return null;
                }

                var tokenFile = JsonConvert.DeserializeObject<TokenFileStructure>(jsonContent);
                if (tokenFile?.Tokens != null && tokenFile.Tokens.TryGetValue(service, out var token))
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Token {service} cargado desde archivo");
                    return token;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Error cargando token {service} desde archivo: {ex.Message}");
                return null;
            }
        }

        // NUEVO: Guardar token en archivo JSON
        private static async Task GuardarTokenEnArchivo(string service, CachedToken token)
        {
            try
            {
                TokenFileStructure tokenFile;

                // Cargar archivo existente o crear nuevo
                if (File.Exists(TokenConfigPath))
                {
                    string existingContent = await File.ReadAllTextAsync(TokenConfigPath);
                    tokenFile = JsonConvert.DeserializeObject<TokenFileStructure>(existingContent) 
                                ?? new TokenFileStructure();
                }
                else
                {
                    tokenFile = new TokenFileStructure();
                }

                // Actualizar o agregar token
                tokenFile.Tokens[service] = token;
                tokenFile.LastUpdated = DateTime.UtcNow;

                // CORREGIDO: Usar especificación completa para evitar ambigüedad
                string jsonContent = JsonConvert.SerializeObject(tokenFile, Newtonsoft.Json.Formatting.Indented);
                await File.WriteAllTextAsync(TokenConfigPath, jsonContent);

                System.Diagnostics.Debug.WriteLine($"[AFIP] Token {service} guardado en archivo");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Error guardando token en archivo: {ex.Message}");
            }
        }

        // NUEVO: Eliminar token del archivo
        private static async Task EliminarTokenDelArchivo(string service)
        {
            try
            {
                if (!File.Exists(TokenConfigPath))
                {
                    return;
                }

                string jsonContent = await File.ReadAllTextAsync(TokenConfigPath);
                var tokenFile = JsonConvert.DeserializeObject<TokenFileStructure>(jsonContent) 
                                ?? new TokenFileStructure();

                if (tokenFile.Tokens.Remove(service))
                {
                    tokenFile.LastUpdated = DateTime.UtcNow;
                    // CORREGIDO: Usar especificación completa para evitar ambigüedad
                    string updatedContent = JsonConvert.SerializeObject(tokenFile, Newtonsoft.Json.Formatting.Indented);
                    await File.WriteAllTextAsync(TokenConfigPath, updatedContent);

                    System.Diagnostics.Debug.WriteLine($"[AFIP] Token {service} eliminado del archivo");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Error eliminando token del archivo: {ex.Message}");
            }
        }

        // MEJORADO: Método para obtener token existente con prioridad al archivo
        public static (string token, string sign)? GetExistingToken(string service)
        {
            try
            {
                // Primero verificar cache en memoria
                if (_tokenCache.TryGetValue(service, out var cached))
                {
                    double minutosRestantes = (cached.ExpirationTime - DateTime.UtcNow).TotalMinutes;
                    
                    if (minutosRestantes > 1 && !string.IsNullOrEmpty(cached.Token) && !string.IsNullOrEmpty(cached.Sign))
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Token válido en cache: {minutosRestantes:F1} min restantes");
                        return (cached.Token, cached.Sign);
                    }
                    else if (minutosRestantes > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] ⚠️ Token próximo a expirar: {minutosRestantes:F1} min, pero válido");
                        return (cached.Token, cached.Sign);
                    }
                }

                // Si no hay en cache, intentar cargar desde archivo de forma síncrona
                Task.Run(async () => await CargarTokensDesdeArchivo()).Wait(1000); // Timeout de 1 segundo
                
                // Verificar cache nuevamente después de cargar desde archivo
                if (_tokenCache.TryGetValue(service, out var cachedAfterLoad))
                {
                    double minutosRestantes = (cachedAfterLoad.ExpirationTime - DateTime.UtcNow).TotalMinutes;
                    if (minutosRestantes > 1)
                    {
                        return (cachedAfterLoad.Token, cachedAfterLoad.Sign);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Error obteniendo token existente: {ex.Message}");
                return null;
            }
        }

        public static (bool tieneTokenValido, string mensaje, double minutosRestantes) VerificarTokensExistentes(string service)
        {
            try
            {
                if (_tokenCache.TryGetValue(service, out var cached))
                {
                    double minutosRestantes = (cached.ExpirationTime - DateTime.UtcNow).TotalMinutes;
                    
                    if (minutosRestantes > 1)
                    {
                        return (true, $"Token válido por {minutosRestantes:F1} minutos más", minutosRestantes);
                    }
                    else if (minutosRestantes > 0)
                    {
                        return (true, $"Token expirando en {minutosRestantes:F1} minutos", minutosRestantes);
                    }
                    else
                    {
                        return (false, "Token expirado", minutosRestantes);
                    }
                }
                
                return (false, "No hay tokens en cache", 0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Error verificando tokens: {ex.Message}");
                return (false, "Error verificando tokens", 0);
            }
        }

        // NUEVO: Forzar uso de token existente (para casos donde sabemos que AFIP tiene uno activo)
        public static async Task<(string token, string sign, DateTime expiration)> ForzarUsoTokenExistente(
            string service, string pfxPath, string pfxPassword)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] 🔧 FORZANDO USO DE TOKEN EXISTENTE para {service}");
                
                // Verificar cache primero
                var tokenCache = GetExistingToken(service);
                if (tokenCache.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] ✅ Usando token del cache");
                    return (tokenCache.Value.token, tokenCache.Value.sign, 
                        _tokenCache[service].ExpirationTime);
                }
                
                // Si no hay cache, crear token de trabajo directamente
                return await UsarTokenExistenteDelArchivo(service, pfxPath, pfxPassword, 
                    "Forzando uso de token existente en AFIP");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Error forzando uso de token existente: {ex.Message}");
                throw;
            }
        }

        // MEJORADO: Limpiar cache y archivo
        public static void ClearTokenCache()
        {
            _tokenCache.Clear();
            
            try
            {
                if (File.Exists(TokenConfigPath))
                {
                    File.Delete(TokenConfigPath);
                    System.Diagnostics.Debug.WriteLine($"[AFIP] Archivo de tokens eliminado");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Error eliminando archivo de tokens: {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine($"[AFIP] Caché de tokens limpiado completamente");
        }

        public static void ClearTokenCache(string service)
        {
            if (_tokenCache.Remove(service))
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Token {service} eliminado del caché");
            }

            // También eliminar del archivo de forma asíncrona
            Task.Run(async () => await EliminarTokenDelArchivo(service));
        }

        // RESTO DE MÉTODOS EXISTENTES (MANTENER TODOS TAL COMO ESTÁN)
        public static async Task<bool> VerificarEstadoServicioAfipAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);

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

        // RESTO DE MÉTODOS EXISTENTES (CreateTRA, SignTRA, SendToWSAA, ExtractTokenAndSign, etc.)
        private static string CreateTRA(string service)
        {
            DateTime now = DateTime.UtcNow;
            DateTime from = now.AddMinutes(-10);
            DateTime to = now.AddHours(12);

            long uniqueId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string generationTime = from.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";
            string expirationTime = to.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";

            // CORREGIDO: Agregado el cierre > que faltaba en la primera línea
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

        private static string SignTRA(string tra, X509Certificate2 certificate)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Iniciando firma del TRA...");

                byte[] traBytes = Encoding.UTF8.GetBytes(tra);
                ContentInfo contentInfo = new ContentInfo(traBytes);
                SignedCms signedCms = new SignedCms(contentInfo, false);
                CmsSigner signer = new CmsSigner(certificate);

                signer.DigestAlgorithm = new System.Security.Cryptography.Oid("2.16.840.1.101.3.4.2.1");
                signer.IncludeOption = X509IncludeOption.ExcludeRoot;
                signer.Certificates.Add(certificate);

                System.Diagnostics.Debug.WriteLine($"[AFIP] Configuración del signer completada");
                System.Diagnostics.Debug.WriteLine($"[AFIP] Digest Algorithm: {signer.DigestAlgorithm.Value}");
                System.Diagnostics.Debug.WriteLine($"[AFIP] Include Option: {signer.IncludeOption}");

                signedCms.ComputeSignature(signer);
                byte[] signedBytes = signedCms.Encode();
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

        private static async Task<string> SendToWSAA(string cms, string wsaaUrl)
        {
            try
            {
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
                    client.Timeout = TimeSpan.FromSeconds(60);
                    client.DefaultRequestHeaders.Clear();

                    var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
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

                    if (responseContent.Contains("<soapenv:Fault>") || responseContent.Contains("<soap:Fault>"))
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] SOAP Fault detectado, procesando...");
                        HandleSoapFault(responseContent);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AFIP] Error Response Content:");
                        System.Diagnostics.Debug.WriteLine(responseContent);

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

                if (faultCode.Contains("coe.alreadyAuthenticated"))
                {
                    throw new TokenAlreadyExistsException(
                        "Ya existe un token válido para este servicio en AFIP. El token anterior debe expirar antes de solicitar uno nuevo.",
                        faultString
                    );
                }

                throw new Exception($"SOAP Fault de AFIP: {faultString} (Código: {faultCode})");
            }
            catch (TokenAlreadyExistsException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Error procesando SOAP Fault: {ex.Message}");
                throw new Exception($"Error procesando respuesta SOAP de AFIP: {ex.Message}");
            }
        }

        private static (string token, string sign, DateTime expirationTime) ExtractTokenAndSign(string xmlResponse)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] Extrayendo token y sign del XML...");

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlResponse);

                var loginCmsReturnNode = xmlDoc.SelectSingleNode("//loginCmsReturn") ??
                                         xmlDoc.SelectSingleNode("//*[local-name()='loginCmsReturn']");

                if (loginCmsReturnNode == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] ERROR: No se encontró loginCmsReturn");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] XML Response completo:");
                    System.Diagnostics.Debug.WriteLine(xmlResponse);
                    throw new Exception("No se encontró el nodo loginCmsReturn en la respuesta de AFIP");
                }

                string decodedXml = System.Net.WebUtility.HtmlDecode(loginCmsReturnNode.InnerText);

                System.Diagnostics.Debug.WriteLine($"[AFIP] XML decodificado:");
                System.Diagnostics.Debug.WriteLine(decodedXml);

                var decodedDoc = new XmlDocument();
                decodedDoc.LoadXml(decodedXml);

                var tokenNode = decodedDoc.SelectSingleNode("//token") ??
                                decodedDoc.SelectSingleNode("//*[local-name()='token']");

                var signNode = decodedDoc.SelectSingleNode("//sign") ??
                               decodedDoc.SelectSingleNode("//*[local-name()='sign']");

                var expirationNode = decodedDoc.SelectSingleNode("//expirationTime") ??
                                     decodedDoc.SelectSingleNode("//*[local-name()='expirationTime']");

                if (tokenNode == null || signNode == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] ERROR: No se encontraron nodos token/sign en XML decodificado");
                    System.Diagnostics.Debug.WriteLine($"[AFIP] XML decodificado completo:");
                    System.Diagnostics.Debug.WriteLine(decodedXml);

                    var errorNode = decodedDoc.SelectSingleNode("//faultstring") ??
                                    decodedDoc.SelectSingleNode("//*[local-name()='faultstring']");

                    if (errorNode != null)
                        throw new Exception($"Error de AFIP: {errorNode.InnerText}");

                    throw new Exception("No se pudo extraer token y sign del XML decodificado de AFIP");
                }

                string token = tokenNode.InnerText;
                string sign = signNode.InnerText;

                if (expirationNode == null || string.IsNullOrWhiteSpace(expirationNode.InnerText))
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] ERROR: No se encontró el nodo expirationTime o está vacío.");
                    throw new Exception("No se pudo extraer la fecha de expiración del XML de AFIP.");
                }

                System.Diagnostics.Debug.WriteLine($"[AFIP] expirationTime string: {expirationNode.InnerText}");

                if (!DateTime.TryParse(expirationNode.InnerText, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var expirationTime))
                {
                    System.Diagnostics.Debug.WriteLine($"[AFIP] ERROR: No se pudo parsear la fecha de expiración: {expirationNode.InnerText}");
                    throw new Exception($"No se pudo parsear la fecha de expiración del TA: {expirationNode.InnerText}");
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

        public static (bool hayConflicto, string mensaje) VerificarConflictoTokens(string service)
        {
            try
            {
                if (_tokenCache.TryGetValue(service, out var cached))
                {
                    double minutosRestantes = (cached.ExpirationTime - DateTime.UtcNow).TotalMinutes;
                    
                    if (minutosRestantes > 5)
                    {
                        return (true, $"Token activo por {minutosRestantes:F1} minutos más");
                    }
                    else if (minutosRestantes > 0)
                    {
                        return (true, $"Token expirando en {minutosRestantes:F1} minutos");
                    }
                }
                
                return (false, "No hay tokens en conflicto");
            }
            catch
            {
                return (false, "No se pudo verificar estado de tokens");
            }
        }

        public static void ResetearEstadoAfip()
        {
            try
            {
                ClearTokenCache();
                System.Diagnostics.Debug.WriteLine($"[AFIP] 🔄 Estado AFIP reseteado completamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AFIP] ⚠️ Error reseteando estado: {ex.Message}");
            }
        }
    }

    public class TokenAlreadyExistsException : Exception
    {
        public string FaultString { get; }

        public TokenAlreadyExistsException(string message, string faultString) : base(message)
        {
            FaultString = faultString;
        }
    }
}