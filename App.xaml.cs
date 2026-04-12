using System;
using System.Windows;

namespace POPSManager
{
    public partial class App : Application
    {
        public AppServices Services { get; private set; } = null!;

        // ============================================================
        //  CONSTRUCTOR — Manejo global de excepciones
        // ============================================================
        public App()
        {
            // Capturar excepciones no manejadas de UI
            DispatcherUnhandledException += (s, e) =>
            {
                try
                {
                    Services?.LogService?.Error($"[FATAL] {e.Exception}");
                }
                catch { }

                MessageBox.Show(
                    $"Error inesperado:\n{e.Exception.Message}",
                    "POPSManager — Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                e.Handled = true;
            };

            // Capturar excepciones de hilos secundarios
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                try
                {
                    Services?.LogService?.Error($"[FATAL-THREAD] {ex}");
                }
                catch { }

                MessageBox.Show(
                    $"Error crítico en hilo secundario:\n{ex?.Message}",
                    "POPSManager — Error Crítico",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };
        }

        // ============================================================
        //  STARTUP — Inicialización protegida
        // ============================================================
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 1. Inicializar DI Container
                Services = new AppServices();
                Services.LogService.Info("[APP] Servicios inicializados correctamente.");
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

        // ============================================================
        //  EXIT — Liberación segura de recursos
        // ============================================================
        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                if (Services != null)
                {
                    Services.LogService.Info("[APP] Cerrando aplicación...");
                    await Services.DisposeAsync();
                }
            }
            catch { }

            base.OnExit(e);
        }

        // ============================================================
        //  ACCESO GLOBAL (para Views y Windows)
        // ============================================================
        public static new App Current => (App)Application.Current;
    }
}
