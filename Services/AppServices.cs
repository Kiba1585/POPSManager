using POPSManager.Logic;
using POPSManager.Models;

namespace POPSManager.Services
{
    public class AppServices
    {
        // ============================
        //  SERVICIOS GLOBALES
        // ============================

        public SettingsService Settings { get; }
        public PathsService Paths { get; }
        public LoggingService LogService { get; }
        public NotificationService Notifications { get; }
        public ProgressService Progress { get; }
        public ConverterService Converter { get; }
        public GameProcessor GameProcessor { get; }

        public AppServices()
        {
            // ============================
            // 1. Logging
            // ============================
            LogService = new LoggingService();

            // ============================
            // 2. Settings (carga settings.json)
            // ============================
            Settings = new SettingsService(LogService.Write);

            // ============================
            // 3. PathsService (usa Settings)
            // ============================
            Paths = new PathsService(LogService.Write, Settings);

            // ============================
            // 4. Notificaciones
            // ============================
            Notifications = new NotificationService();

            // ============================
            // 5. Progreso
            // ============================
            Progress = new ProgressService();

            // ============================
            // 6. Conversor
            // ============================
            Converter = new ConverterService();

            // ============================
            // 7. GameProcessor FINAL
            // ============================
            GameProcessor = new GameProcessor(
                Progress.SetProgress,      // Actualizar porcentaje
                Progress.SetStatus,        // Actualizar texto de estado
                LogService.Write,          // Registrar logs
                Notifications.Show,        // Mostrar notificaciones
                Paths                      // Acceso a rutas (incluye POPStarter)
            );
        }
    }
}
