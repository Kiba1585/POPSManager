using System;
using System.Windows;
using POPSManager.Services;

namespace POPSManager
{
    public partial class App : Application
    {
        // Servicios globales accesibles desde toda la aplicación
        public static AppServices Services { get; private set; } = null!;

        public App()
        {
            // Inicialización temprana de servicios globales
            Services = new AppServices();

            // Manejo global de excepciones (ULTRA PRO)
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Inicializaciones globales opcionales
            // Services.Paths.EnsureFolderStructure();
        }

        // ============================================================
        //  MANEJO GLOBAL DE EXCEPCIONES (ULTRA PRO)
        // ============================================================
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Services.Notifications.Show(
                    new UiNotification(NotificationType.Error,
                    $"Error inesperado: {e.Exception.Message}"));

                Services.LogService.Log($"[ERROR] {e.Exception}");
            }
            catch { }

            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Services.Notifications.Show(
                        new UiNotification(NotificationType.Error,
                        $"Error crítico: {ex.Message}"));

                    Services.LogService.Log($"[CRITICAL] {ex}");
                }
            }
            catch { }
        }
    }
}
