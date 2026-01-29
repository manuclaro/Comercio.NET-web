using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Comercio.NET.Formularios;
using Comercio.NET.Services;
using Comercio.NET.Servicios;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Comercio.NET
{
    static class Program
    {
        // ✅ CONFIGURACIÓN DE VERSIÓN - ACTUALIZAR CON CADA RELEASE
        private const string CURRENT_VERSION = "1.2.0";
        
        // ✅ URL del servidor de actualizaciones - CONFIGURAR SEGÚN TU SERVIDOR
        private const string UPDATE_SERVER = "https://tu-servidor.com/updates/comercio-net";
        
        // ✅ Verificar actualizaciones al iniciar (true) o solo manualmente (false)
        private const bool AUTO_CHECK_UPDATES = true;

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Debug.WriteLine($"[APP] Iniciando Comercio .NET v{CURRENT_VERSION}");

            // ✅ Verificar actualizaciones al iniciar
            if (AUTO_CHECK_UPDATES)
            {
                _ = CheckForUpdatesAsync();
            }

            try
            {
                // Mostrar login
                using (var loginForm = new LoginForm())
                {
                    var result = loginForm.ShowDialog();
                    
                    if (result != DialogResult.OK || !loginForm.LoginExitoso)
                    {
                        return;
                    }
                }

                // Crear el formulario principal
                var menuPrincipal = new MenuPrincipal();

                // MEJORADO: Validación completa de AFIP (conectividad + configuración)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(2000); // Dar tiempo para que cargue la UI
                        
                        var (afipDisponible, estadoCompleto, detalleEstado) = await VerificarEstadoCompletoAfip();
                        
                        menuPrincipal.Invoke(new Action(() =>
                        {
                            // Actualizar barra de estado siempre
                            if (menuPrincipal.Controls.Find("statusStrip", true).FirstOrDefault() is StatusStrip statusStrip)
                            {
                                string textoEstado = estadoCompleto switch
                                {
                                    EstadoAfip.Conectado => "🟢 AFIP Conectado",
                                    EstadoAfip.ServicioDisponible => "🟡 AFIP Disponible (Config. Incompleta)",
                                    EstadoAfip.ErrorConfiguracion => "🟠 AFIP: Error Configuración",
                                    EstadoAfip.NoDisponible => "🔴 AFIP No Disponible",
                                    _ => "⚫ AFIP: Estado Desconocido"
                                };

                                var colorEstado = estadoCompleto switch
                                {
                                    EstadoAfip.Conectado => System.Drawing.Color.Green,
                                    EstadoAfip.ServicioDisponible => System.Drawing.Color.Orange,
                                    EstadoAfip.ErrorConfiguracion => System.Drawing.Color.DarkOrange,
                                    EstadoAfip.NoDisponible => System.Drawing.Color.Red,
                                    _ => System.Drawing.Color.Gray
                                };

                                var labelAfip = new ToolStripStatusLabel(textoEstado)
                                {
                                    ForeColor = colorEstado,
                                    Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold),
                                    ToolTipText = detalleEstado
                                };
                                statusStrip.Items.Add(labelAfip);
                            }

                            // Solo mostrar mensaje emergente si hay problemas importantes
                            if (estadoCompleto != EstadoAfip.Conectado && estadoCompleto != EstadoAfip.ServicioDisponible)
                            {
                                string mensaje = estadoCompleto switch
                                {
                                    EstadoAfip.ErrorConfiguracion => 
                                        "⚠️ AFIP: Error de Configuración\n\n" +
                                        $"{detalleEstado}\n\n" +
                                        "Verifique la configuración en el sistema.",
                                    EstadoAfip.NoDisponible => 
                                        "⚠️ AFIP no disponible\n\n" +
                                        "Los servicios de AFIP no están respondiendo.\n" +
                                        "La aplicación funcionará en modo offline.\n\n" +
                                        "✅ Puede continuar trabajando normalmente.",
                                    _ => $"⚠️ AFIP: {detalleEstado}"
                                };

                                MessageBox.Show(mensaje, "Estado AFIP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }));
                        
                        System.Diagnostics.Debug.WriteLine($"=== ESTADO AFIP COMPLETO ===");
                        System.Diagnostics.Debug.WriteLine($"Estado: {estadoCompleto}");
                        System.Diagnostics.Debug.WriteLine($"Disponible: {afipDisponible}");
                        System.Diagnostics.Debug.WriteLine($"Detalle: {detalleEstado}");
                        System.Diagnostics.Debug.WriteLine($"============================");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error verificación AFIP: {ex.Message}");
                        
                        // Mostrar estado de error en la barra
                        menuPrincipal.Invoke(new Action(() =>
                        {
                            if (menuPrincipal.Controls.Find("statusStrip", true).FirstOrDefault() is StatusStrip statusStrip)
                            {
                                var labelAfip = new ToolStripStatusLabel("❌ AFIP: Error Verificación")
                                {
                                    ForeColor = System.Drawing.Color.Red,
                                    Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold),
                                    ToolTipText = $"Error: {ex.Message}"
                                };
                                statusStrip.Items.Add(labelAfip);
                            }
                        }));
                    }
                });

                // Mostrar menú principal
                Application.Run(menuPrincipal);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error iniciando aplicación: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NUEVO: Enumeración para estados de AFIP
        private enum EstadoAfip
        {
            Conectado,          // Todo configurado y funcionando
            ServicioDisponible, // Servicio responde pero configuración incompleta  
            ErrorConfiguracion, // Hay errores en la configuración
            NoDisponible        // Servicio no responde
        }

        // NUEVO: Método mejorado para verificar estado completo de AFIP
        private static async Task<(bool disponible, EstadoAfip estado, string detalle)> VerificarEstadoCompletoAfip()
        {
            System.Diagnostics.Debug.WriteLine("🔍 === INICIANDO VERIFICACIÓN ESTADO AFIP ===");

            try
            {
                // 1. VERIFICAR CONECTIVIDAD DEL SERVICIO
                System.Diagnostics.Debug.WriteLine("📡 Paso 1: Verificando conectividad del servicio...");
                bool servicioDisponible = await AfipAuthenticator.VerificarEstadoServicioAfipAsync();
                System.Diagnostics.Debug.WriteLine($"📡 Servicio disponible: {servicioDisponible}");

                if (!servicioDisponible)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Servicio AFIP no responde - terminando verificación");
                    return (false, EstadoAfip.NoDisponible, "Servicio AFIP no responde");
                }

                // 2. VERIFICAR CONFIGURACIÓN LOCAL
                System.Diagnostics.Debug.WriteLine("⚙️ Paso 2: Verificando configuración local...");

                try
                {
                    var config = new ConfigurationBuilder()
                        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false)
                        .Build();

                    var afipSection = config.GetSection("AFIP");

                    // DEBUG: Mostrar toda la configuración AFIP
                    System.Diagnostics.Debug.WriteLine("=== DEBUG CONFIGURACIÓN AFIP ===");
                    System.Diagnostics.Debug.WriteLine($"Sección AFIP existe: {afipSection.Exists()}");

                    // CORREGIDO: Leer ambiente activo y configuración correspondiente
                    string ambienteActivo = afipSection["AmbienteActivo"] ?? "Testing";
                    System.Diagnostics.Debug.WriteLine($"Ambiente activo: {ambienteActivo}");

                    // CORREGIDO: Leer configuración del ambiente correspondiente
                    var ambienteSection = afipSection.GetSection(ambienteActivo);

                    if (!ambienteSection.Exists())
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ No existe configuración para ambiente: {ambienteActivo}");
                        return (false, EstadoAfip.ErrorConfiguracion, $"No existe configuración para ambiente '{ambienteActivo}' en sección AFIP");
                    }

                    // Verificar CUIT del ambiente activo
                    string cuitEmisor = ambienteSection["CUIT"];
                    System.Diagnostics.Debug.WriteLine($"CUIT ({ambienteActivo}): '{cuitEmisor}'");

                    if (string.IsNullOrWhiteSpace(cuitEmisor))
                    {
                        System.Diagnostics.Debug.WriteLine("❌ CUIT vacío o nulo");
                        return (false, EstadoAfip.ErrorConfiguracion, $"CUIT no configurado en sección AFIP → {ambienteActivo}");
                    }

                    // Validar formato de CUIT (debe ser numérico y tener 11 dígitos)
                    string cuitLimpio = cuitEmisor.Replace("-", "");
                    System.Diagnostics.Debug.WriteLine($"CUIT limpio: '{cuitLimpio}' (longitud: {cuitLimpio.Length})");

                    bool esNumerico = long.TryParse(cuitLimpio, out long cuitNumerico);
                    System.Diagnostics.Debug.WriteLine($"Es numérico: {esNumerico}");
                    System.Diagnostics.Debug.WriteLine($"Longitud correcta: {cuitLimpio.Length == 11}");

                    if (!esNumerico || cuitLimpio.Length != 11)
                    {
                        string error = $"CUIT inválido: {cuitEmisor} (debe tener 11 dígitos numéricos)";
                        System.Diagnostics.Debug.WriteLine($"❌ {error}");
                        return (false, EstadoAfip.ErrorConfiguracion, error);
                    }

                    // NUEVA VALIDACIÓN: Verificar que no sea un CUIT de prueba obvio
                    if (cuitEmisor == "12345678901" || cuitEmisor == "11111111111" || cuitEmisor == "00000000000")
                    {
                        string error = $"CUIT de prueba detectado: {cuitEmisor}";
                        System.Diagnostics.Debug.WriteLine($"⚠️ {error}");
                        return (false, EstadoAfip.ErrorConfiguracion, error);
                    }

                    // Verificar URLs de servicios
                    string wsaaUrl = ambienteSection["WSAAUrl"];
                    string wsfeUrl = ambienteSection["WSFEUrl"];

                    System.Diagnostics.Debug.WriteLine($"WSAA URL: '{wsaaUrl}'");
                    System.Diagnostics.Debug.WriteLine($"WSFE URL: '{wsfeUrl}'");

                    if (string.IsNullOrWhiteSpace(wsaaUrl) || string.IsNullOrWhiteSpace(wsfeUrl))
                    {
                        System.Diagnostics.Debug.WriteLine("❌ URLs de servicios faltantes");
                        return (false, EstadoAfip.ErrorConfiguracion, $"URLs de servicios WSAA/WSFE no configuradas para ambiente {ambienteActivo}");
                    }

                    // Verificar configuración de certificados (rutas)
                    string pfxPath = ambienteSection["CertificadoPath"];
                    string pfxPassword = ambienteSection["CertificadoPassword"];

                    System.Diagnostics.Debug.WriteLine($"Certificado Path: '{pfxPath}'");
                    System.Diagnostics.Debug.WriteLine($"Certificado existe: {!string.IsNullOrWhiteSpace(pfxPath) && File.Exists(pfxPath)}");

                    if (!string.IsNullOrWhiteSpace(pfxPath) && !File.Exists(pfxPath))
                    {
                        string error = $"Ruta de certificado no válida para {ambienteActivo}: {pfxPath}";
                        System.Diagnostics.Debug.WriteLine($"❌ {error}");
                        return (false, EstadoAfip.ErrorConfiguracion, error);
                    }

                    // 3. VERIFICAR CERTIFICADOS (si están configurados)
                    System.Diagnostics.Debug.WriteLine("🔐 Paso 3: Verificando certificados...");

                    if (!string.IsNullOrWhiteSpace(pfxPath))
                    {
                        var (certValido, errorCert) = VerificarCertificadosAfip();
                        System.Diagnostics.Debug.WriteLine($"🔐 Certificado válido: {certValido}");
                        System.Diagnostics.Debug.WriteLine($"🔐 Error certificado: '{errorCert}'");

                        if (!certValido && !errorCert.Contains("no configurados"))
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Error en certificados: {errorCert}");
                            return (false, EstadoAfip.ErrorConfiguracion, $"Certificado: {errorCert}");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"✅ Configuración AFIP válida para ambiente {ambienteActivo}");
                    System.Diagnostics.Debug.WriteLine("================================");
                    System.Diagnostics.Debug.WriteLine("🎉 === VERIFICACIÓN COMPLETADA EXITOSAMENTE ===");

                    return (true, EstadoAfip.Conectado, $"Configuración AFIP válida (ambiente: {ambienteActivo})");
                }
                catch (Exception ex)
                {
                    string error = $"Error leyendo configuración AFIP: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine($"💥 {error}");
                    System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                    return (false, EstadoAfip.ErrorConfiguracion, error);
                }
            }
            catch (Exception ex)
            {
                string error = $"Error verificación: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"💥 Error en verificación: {error}");
                System.Diagnostics.Debug.WriteLine($"💥 StackTrace: {ex.StackTrace}");
                return (false, EstadoAfip.ErrorConfiguracion, error);
            }
        }

        //// NUEVO: Verificar configuración AFIP desde appsettings.json
        //private static async Task<(bool valida, string error)> VerificarConfiguracionAfip()
        //{
        //    try
        //    {
        //        var config = new ConfigurationBuilder()
        //            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        //            .AddJsonFile("appsettings.json", optional: false)
        //            .Build();

        //        var afipSection = config.GetSection("AFIP");

        //        // DEBUG: Mostrar toda la configuración AFIP
        //        //System.Diagnostics.Debug.WriteLine("=== DEBUG CONFIGURACIÓN AFIP ===");
        //        //System.Diagnostics.Debug.WriteLine($"Sección AFIP existe: {afipSection.Exists()}");

        //        // Verificar CUIT
        //        string cuitEmisor = afipSection["CUIT"]; // CORREGIDO: usar "CUIT" en lugar de "CuitEmisor"
        //        System.Diagnostics.Debug.WriteLine($"CUIT leído: '{cuitEmisor}'");

        //        if (string.IsNullOrWhiteSpace(cuitEmisor))
        //        {
        //            System.Diagnostics.Debug.WriteLine("❌ CUIT vacío o nulo");
        //            return (false, "CUIT no configurado en sección AFIP");
        //        }

        //        // Validar formato de CUIT (debe ser numérico y tener 11 dígitos)
        //        string cuitLimpio = cuitEmisor.Replace("-", "");
        //        System.Diagnostics.Debug.WriteLine($"CUIT limpio: '{cuitLimpio}' (longitud: {cuitLimpio.Length})");

        //        bool esNumerico = long.TryParse(cuitLimpio, out long cuitNumerico);
        //        System.Diagnostics.Debug.WriteLine($"Es numérico: {esNumerico}");
        //        System.Diagnostics.Debug.WriteLine($"Longitud correcta: {cuitLimpio.Length == 11}");

        //        if (!esNumerico || cuitLimpio.Length != 11)
        //        {
        //            string error = $"CUIT inválido: {cuitEmisor} (debe tener 11 dígitos numéricos)";
        //            System.Diagnostics.Debug.WriteLine($"❌ {error}");
        //            return (false, error);
        //        }

        //        // NUEVA VALIDACIÓN: Verificar que no sea un CUIT de prueba obvio
        //        if (cuitEmisor == "12345678901" || cuitEmisor == "11111111111" || cuitEmisor == "00000000000")
        //        {
        //            string error = $"CUIT de prueba detectado: {cuitEmisor}";
        //            System.Diagnostics.Debug.WriteLine($"⚠️ {error}");
        //            return (false, error);
        //        }

        //        // Verificar URLs de servicios
        //        string wsaaUrl = afipSection["WSAAUrl"];
        //        string wsfeUrl = afipSection["WSFEUrl"];

        //        System.Diagnostics.Debug.WriteLine($"WSAA URL: '{wsaaUrl}'");
        //        System.Diagnostics.Debug.WriteLine($"WSFE URL: '{wsfeUrl}'");

        //        if (string.IsNullOrWhiteSpace(wsaaUrl) || string.IsNullOrWhiteSpace(wsfeUrl))
        //        {
        //            System.Diagnostics.Debug.WriteLine("❌ URLs de servicios faltantes");
        //            return (false, "URLs de servicios WSAA/WSFE no configuradas");
        //        }

        //        // Verificar configuración de certificados (rutas)
        //        string pfxPath = afipSection["CertificadoPath"]; // CORREGIDO: usar "CertificadoPath"
        //        string pfxPassword = afipSection["CertificadoPassword"]; // CORREGIDO: usar "CertificadoPassword"

        //        System.Diagnostics.Debug.WriteLine($"Certificado Path: '{pfxPath}'");
        //        System.Diagnostics.Debug.WriteLine($"Certificado existe: {!string.IsNullOrWhiteSpace(pfxPath) && File.Exists(pfxPath)}");

        //        if (!string.IsNullOrWhiteSpace(pfxPath) && !File.Exists(pfxPath))
        //        {
        //            string error = $"Ruta de certificado no válida: {pfxPath}";
        //            System.Diagnostics.Debug.WriteLine($"❌ {error}");
        //            return (false, error);
        //        }

        //        System.Diagnostics.Debug.WriteLine("✅ Configuración AFIP válida");
        //        System.Diagnostics.Debug.WriteLine("================================");
        //        return (true, "Configuración AFIP válida");
        //    }
        //    catch (Exception ex)
        //    {
        //        string error = $"Error leyendo configuración AFIP: {ex.Message}";
        //        System.Diagnostics.Debug.WriteLine($"💥 {error}");
        //        System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        //        return (false, error);
        //    }
        //}

        // NUEVO: Verificar certificados AFIP
        private static (bool valido, string error) VerificarCertificadosAfip()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

                var afipSection = config.GetSection("AFIP");
                string pfxPath = afipSection["CertificadoPath"]; // CORREGIDO: usar "CertificadoPath"
                string pfxPassword = afipSection["CertificadoPassword"]; // CORREGIDO: usar "CertificadoPassword"

                // Si no hay certificados configurados, no es error (puede usar otros métodos)
                if (string.IsNullOrWhiteSpace(pfxPath))
                {
                    return (false, "Certificados no configurados");
                }

                // Si está configurado, verificar que sea válido
                var (valido, mensaje, vence) = AfipAuthenticator.VerificarCertificado(pfxPath, pfxPassword ?? "");
                
                return (valido, mensaje);
            }
            catch (Exception ex)
            {
                return (false, $"Error verificando certificados: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica si hay actualizaciones disponibles
        /// </summary>
        public static async Task CheckForUpdatesAsync()
        {
            try
            {
                Debug.WriteLine("[APP] Verificando actualizaciones...");

                using (var updater = new AutoUpdaterService(UPDATE_SERVER, CURRENT_VERSION))
                {
                    var versionInfo = await updater.CheckForUpdatesAsync();

                    if (versionInfo != null)
                    {
                        Debug.WriteLine($"[APP] ✅ Nueva versión disponible: {versionInfo.Version}");

                        // Mostrar formulario de actualización
                        using (var frmUpdate = new frmActualizacion(versionInfo, CURRENT_VERSION, UPDATE_SERVER))
                        {
                            frmUpdate.ShowDialog();
                        }
                    }
                    else
                    {
                        Debug.WriteLine("[APP] ℹ️ No hay actualizaciones disponibles");
                    }
                }
            }
            catch (Exception ex)
            {
                // No interrumpir la aplicación si falla la verificación
                Debug.WriteLine($"[APP] ⚠️ Error verificando actualizaciones: {ex.Message}");
                
                // Opcionalmente, puedes mostrar un mensaje al usuario
                // MessageBox.Show("No se pudo verificar si hay actualizaciones disponibles.", 
                //     "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}