using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Comercio.NET.Servicios;

namespace Comercio.NET.Utilidades
{
    public static class VerificadorCertificadoAfip
    {
        public static void VerificarConfiguracionAfip()
        {
            try
            {
                // Cargar configuración
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string cuit = config["AFIP:CUIT"];
                string certificadoPath = config["AFIP:CertificadoPath"];
                string certificadoPassword = config["AFIP:CertificadoPassword"];
                string wsaaUrl = config["AFIP:WSAAUrl"];

                var resultado = new System.Text.StringBuilder();
                resultado.AppendLine("=== VERIFICACIÓN DE CONFIGURACIÓN AFIP ===\n");

                // Verificar CUIT
                resultado.AppendLine($"? CUIT: {cuit}");
                if (string.IsNullOrEmpty(cuit))
                {
                    resultado.AppendLine("? ERROR: CUIT no configurado");
                    return;
                }

                // Verificar URLs
                resultado.AppendLine($"? WSAA URL: {wsaaUrl}");

                // Verificar archivo de certificado
                resultado.AppendLine($"? Certificado: {certificadoPath}");
                
                if (!File.Exists(certificadoPath))
                {
                    resultado.AppendLine("? ERROR: El archivo de certificado no existe");
                    resultado.AppendLine("\n?? SOLUCIONES POSIBLES:");
                    resultado.AppendLine("1. Verifica que el archivo existe en la ruta especificada");
                    resultado.AppendLine("2. Asegúrate de que sea un archivo .p12 (no .crt)");
                    resultado.AppendLine("3. Si tienes .crt y .key, conviértelos a .p12 usando OpenSSL");
                    
                    MessageBox.Show(resultado.ToString(), "Error de Certificado", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Verificar formato y contenido del certificado
                var (valido, mensaje, vence) = AfipAuthenticator.VerificarCertificado(certificadoPath, certificadoPassword);
                
                if (valido)
                {
                    resultado.AppendLine($"? Certificado VÁLIDO");
                    resultado.AppendLine($"   {mensaje}");
                    
                    try
                    {
                        // Intentar cargar el certificado para más detalles
                        var cert = new X509Certificate2(certificadoPath, certificadoPassword,
                            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                        
                        resultado.AppendLine($"\n?? DETALLES DEL CERTIFICADO:");
                        resultado.AppendLine($"   Subject: {cert.Subject}");
                        resultado.AppendLine($"   Issuer: {cert.Issuer}");
                        resultado.AppendLine($"   Valid From: {cert.NotBefore:dd/MM/yyyy}");
                        resultado.AppendLine($"   Valid To: {cert.NotAfter:dd/MM/yyyy}");
                        resultado.AppendLine($"   Has Private Key: {cert.HasPrivateKey}");
                        resultado.AppendLine($"   Serial Number: {cert.SerialNumber}");
                    }
                    catch (Exception ex)
                    {
                        resultado.AppendLine($"? Error leyendo detalles: {ex.Message}");
                    }
                }
                else
                {
                    resultado.AppendLine($"? ERROR EN CERTIFICADO: {mensaje}");
                    
                    resultado.AppendLine("\n?? POSIBLES SOLUCIONES:");
                    resultado.AppendLine("1. Verifica que el certificado sea para AFIP (no un certificado genérico)");
                    resultado.AppendLine("2. Asegúrate de que la contraseña sea correcta");
                    resultado.AppendLine("3. El certificado debe estar en formato .p12 con clave privada incluida");
                    resultado.AppendLine("4. Si tienes certificado .crt, necesitas también el .key para crear el .p12");
                    
                    if (certificadoPath.EndsWith(".crt"))
                    {
                        resultado.AppendLine("\n?? DETECTADO: Usas archivo .crt");
                        resultado.AppendLine("AFIP requiere formato .p12 que incluya la clave privada.");
                        resultado.AppendLine("Busca también el archivo .key correspondiente y conviértelos a .p12");
                    }
                }

                resultado.AppendLine($"\n????? PRÓXIMO PASO:");
                resultado.AppendLine("Si todo está correcto, intenta nuevamente la facturación.");

                MessageBox.Show(resultado.ToString(), "Verificación AFIP", 
                    MessageBoxButtons.OK, valido ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verificando configuración AFIP:\n\n{ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}