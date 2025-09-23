using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Net.Http;
using System.ServiceModel;

namespace Comercio.NET.Servicios
{
    public static class AfipAuthenticator
    {
        public static async Task<(string token, string sign)> GetTAAsync(string service, string pfxPath, string pfxPassword, string wsaaUrl)
        {
            try
            {
                // Verificar que el archivo del certificado existe
                if (!File.Exists(pfxPath))
                {
                    throw new FileNotFoundException($"El certificado no se encuentra en: {pfxPath}");
                }

                // Cargar el certificado
                var certificate = new X509Certificate2(pfxPath, pfxPassword);

                // Crear el request XML para WSAA
                string tra = CreateTRA(service);

                // Firmar el TRA
                string cms = SignTRA(tra, certificate);

                // Enviar a WSAA y obtener respuesta
                string taXml = await SendToWSAA(cms, wsaaUrl);

                // Extraer token y sign del XML de respuesta
                return ExtractTokenAndSign(taXml);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en autenticación AFIP: {ex.Message}", ex);
            }
        }

        private static string CreateTRA(string service)
        {
            DateTime now = DateTime.Now;
            DateTime from = now.AddMinutes(-10);
            DateTime to = now.AddHours(12);

            string tra = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<loginTicketRequest version=""1.0"">
    <header>
        <uniqueId>{DateTimeOffset.Now.ToUnixTimeSeconds()}</uniqueId>
        <generationTime>{from:yyyy-MM-ddTHH:mm:ss.fffZ}</generationTime>
        <expirationTime>{to:yyyy-MM-ddTHH:mm:ss.fffZ}</expirationTime>
    </header>
    <service>{service}</service>
</loginTicketRequest>";

            return tra;
        }

        private static string SignTRA(string tra, X509Certificate2 certificate)
        {
            try
            {
                var content = new System.Security.Cryptography.Pkcs.ContentInfo(Encoding.UTF8.GetBytes(tra));
                var signed = new System.Security.Cryptography.Pkcs.SignedCms(content, true);
                var signer = new System.Security.Cryptography.Pkcs.CmsSigner(certificate);
                
                signed.ComputeSignature(signer);
                return Convert.ToBase64String(signed.Encode());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al firmar TRA: {ex.Message}", ex);
            }
        }

        private static async Task<string> SendToWSAA(string cms, string wsaaUrl)
        {
            try
            {
                string soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/""
               xmlns:wsaa=""http://wsaa.view.sua.dvadac.desein.afip.gov"">
    <soap:Body>
        <wsaa:loginCms>
            <wsaa:in0>{cms}</wsaa:in0>
        </wsaa:loginCms>
    </soap:Body>
</soap:Envelope>";

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                    content.Headers.Clear();
                    content.Headers.Add("Content-Type", "text/xml; charset=utf-8");
                    content.Headers.Add("SOAPAction", "");

                    var response = await client.PostAsync(wsaaUrl, content);
                    response.EnsureSuccessStatusCode();

                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al comunicarse con WSAA: {ex.Message}", ex);
            }
        }

        private static (string token, string sign) ExtractTokenAndSign(string xmlResponse)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlResponse);

                var nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
                nsManager.AddNamespace("wsaa", "http://wsaa.view.sua.dvadac.desein.afip.gov");

                var tokenNode = xmlDoc.SelectSingleNode("//token", nsManager);
                var signNode = xmlDoc.SelectSingleNode("//sign", nsManager);

                if (tokenNode == null || signNode == null)
                {
                    throw new Exception("No se pudo extraer token y sign del XML de respuesta");
                }

                return (tokenNode.InnerText, signNode.InnerText);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al extraer token y sign: {ex.Message}", ex);
            }
        }
    }
}