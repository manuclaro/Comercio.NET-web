using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Comercio.NET.Formularios;
using Comercio.NET.Services;

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
                        return; // Usuario canceló o login falló
                    }
                }

                // Login exitoso - ejecutar corrección de password en background
                //Task.Run(async () =>
                //{
                //    try
                //    {
                //        var authService = new AuthenticationService();
                //        await authService.CorregirPasswordAdminAsync();
                //    }
                //    catch (Exception ex)
                //    {
                //        // Log error pero no detener la aplicación
                //        System.Diagnostics.Debug.WriteLine($"Error corrigiendo password: {ex.Message}");
                //    }
                //});

                // Mostrar menú principal
                Application.Run(new MenuPrincipal());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error iniciando aplicación: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}