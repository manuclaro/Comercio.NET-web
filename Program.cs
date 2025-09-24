using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Comercio.NET.Formularios;
using Comercio.NET.Services;
using Comercio.NET.Servicios;

namespace Comercio.NET
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

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

                // VERSIÓN PRODUCCIÓN: Solo mostrar cuando hay problemas + notificación sutil cuando funciona
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(2000); // Dar tiempo para que cargue la UI
                        
                        bool afipDisponible = await AfipAuthenticator.VerificarEstadoServicioAfipAsync();
                        
                        menuPrincipal.Invoke(new Action(() =>
                        {
                            if (!afipDisponible)
                            {
                                // Solo mostrar cuando hay problemas
                                MessageBox.Show(
                                    "⚠️ AFIP no disponible\n\n" +
                                    "Los servicios de AFIP no están respondiendo.\n" +
                                    "La aplicación funcionará en modo offline.\n\n" +
                                    "✅ Puede continuar trabajando normalmente.",
                                    "Aviso AFIP",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                            }
                            else
                            {
                                // Notificación sutil en la barra de estado (si existe)
                                if (menuPrincipal.Controls.Find("statusStrip", true).FirstOrDefault() is StatusStrip statusStrip)
                                {
                                    var labelAfip = new ToolStripStatusLabel("✅ AFIP Conectado")
                                    {
                                        ForeColor = System.Drawing.Color.Green,
                                        Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold)
                                    };
                                    statusStrip.Items.Add(labelAfip);
                                }
                            }
                        }));
                        
                        System.Diagnostics.Debug.WriteLine($"Estado AFIP: {(afipDisponible ? "✅ Disponible" : "❌ No disponible")}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error verificación AFIP: {ex.Message}");
                        // No mostrar errores técnicos al usuario final en producción
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
    }
}