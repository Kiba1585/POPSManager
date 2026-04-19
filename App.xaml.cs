using System;
using System.Windows;
using POPSManager.Services;

namespace POPSManager
{
    public partial class App : System.Windows.Application
    {
        public static AppServices Services { get; private set; } = null!;

        public App()
        {
            // (código de manejo de excepciones igual que antes)
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                Services = new AppServices();
                Services.LogService.Info("[APP] Servicios inicializados correctamente.");

                // Crear ViewModel y ventana principal
                var mainViewModel = new MainViewModel();
                var mainWindow = new MainWindow(mainViewModel);
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error inicializando servicios:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "POPSManager — Error de Arranque",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        // (resto igual)
    }
}