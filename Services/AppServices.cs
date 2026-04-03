using POPSManager.Logic;
using POPSManager.Models;

namespace POPSManager.Services
{
    public class AppServices
    {
        public PathsService Paths { get; }
        public SettingsService Settings { get; }
        public LoggingService LogService { get; }
        public NotificationService Notifications { get; }
        public ProgressService Progress { get; }
        public ConverterService Converter { get; }
        public GameProcessor GameProcessor { get; }

        public AppServices()
        {
            // Inicializar servicios base
            Paths = new PathsService();
            Settings = new SettingsService();
            LogService = new LoggingService();
            Notifications = new NotificationService();
            Progress = new ProgressService();
            Converter = new ConverterService();

            // Inicializar GameProcessor con callbacks correctos
            GameProcessor = new GameProcessor(
                Progress.SetProgress,   // Actualizar porcentaje
                Progress.SetStatus,     // Actualizar texto de estado
                LogService.Write,       // Registrar logs
                Notifications.Show,     // Mostrar notificaciones
                Paths                   // Acceso a rutas
            );
        }
    }
}
