using System;
using System.Windows;
using POPSManager.Services;
using POPSManager.Models;

namespace POPSManager
{
    /// <summary>
    /// Punto de entrada de la aplicación con DI y manejo global
    /// de excepciones. Implementa shutdown seguro con flush de logs
    /// vía IAsyncDisposable.
    /// </summary>
    public partial class App : Application
    {
        // Servicios globales accesibles desde toda la aplicación
        public static AppServices Services { get; private set; }
            = null!;

        public App()
        {
            // Inicialización de DI container
            Services = new AppServices();

            // Manejo global de excepciones
            this.DispatcherUnhandledException
                += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException
                += CurrentDomain_UnhandledException;
        }

        protected override void OnStartup(
            StartupEventArgs e)
        {
            base.OnStartup(e);
        }

        // ============================================================
        // SHUTDOWN SEGURO (FLUSH DE LOGS)
        // ============================================================
        protected override async void OnExit(
            ExitEventArgs e)
        {
            try
            {
                // Flush seguro de todos los servicios
                // IAsyncDisposable (LoggingService)
                await Services.DisposeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SHUTDOWN] Error durante dispose: {ex.Message}");
            }

            base.OnExit(e);
        }

        // ============================================================
        // MANEJO GLOBAL DE EXCEPCIONES
        // ============================================================
        private void App_DispatcherUnhandledException(
            object sender,
            System.Windows.Threading
                .DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Services.Notifications.Show(
                    $"Error inesperado: {e.Exception.Message}",
                    NotificationType.Error);
                Services.LogService.Error(
                    $"[ERROR] {e.Exception}");
            }
            catch { }

            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(
            object sender,
            UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Services.Notifications.Show(
                        $"Error crítico: {ex.Message}",
                        NotificationType.Error);
                    Services.LogService.Error(
                        $"[CRITICAL] {ex}");
                }
            }
            catch { }
        }
    }
}
