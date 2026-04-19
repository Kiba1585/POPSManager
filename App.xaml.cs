using System;
using System.Windows;
using POPSManager.Services;
using POPSManager.ViewModels;

namespace POPSManager
{
    public partial class App : System.Windows.Application
    {
        public static AppServices Services { get; private set; } = null!;

        public App()
        {
            DispatcherUnhandledException += (s, e) =>
            {
                try { Services?.LogService?.Error($"[FATAL] {e.Exception}"); } catch { }
                System.Windows.MessageBox.Show(
                    $"Error inesperado:\n{e.Exception.Message}",
                    "POPSManager — Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                e.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                try { Services?.LogService?.Error($"[FATAL-THREAD] {ex}"); } catch { }
                System.Windows.MessageBox.Show(
                    $"Error crítico en hilo secundario:\n{ex?.Message}",
                    "POPSManager — Error Crítico",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                Services = new AppServices();
                await Services.InitializeAsync();
                Services.LogService.Info("[APP] Servicios inicializados correctamente.");

                var mainViewModel = new MainViewModel();
                var mainWindow = new MainWindow(mainViewModel);
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error inicializando servicios:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "POPSManager — Error de Arranque",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try { Services?.LogService?.Info("[APP] Cerrando aplicación..."); } catch { }
            base.OnExit(e);
        }
    }
}