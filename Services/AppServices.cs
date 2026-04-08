using POPSManager.Logic;
using POPSManager.Models;

namespace POPSManager.Services
{
    public class AppServices
    {
        // ============================
        //  SERVICIOS PRINCIPALES
        // ============================
        public SettingsService Settings { get; }
        public PathsService Paths { get; }
        public LoggingService LogService { get; }
        public NotificationService Notifications { get; }
        public ProgressService Progress { get; }
        public ConverterService Converter { get; }

        // GameProcessor se inicializa bajo demanda (Lazy)
        public GameProcessor GameProcessor => _gameProcessor.Value;
        private readonly Lazy<GameProcessor> _gameProcessor;

        public AppServices()
        {
            // ============================
            //  LOGGING (PRIMERO SIEMPRE)
            // ============================
            LogService = new LoggingService();

            // ============================
            //  SETTINGS (depende de Logging)
            // ============================
            Settings = new SettingsService(LogService.Write);

            // ============================
            //  PATHS (depende de Settings + Logging)
            // ============================
            Paths = new PathsService(LogService.Write, Settings);

            // ============================
            //  NOTIFICACIONES
            // ============================
            Notifications = new NotificationService();

            // ============================
            //  PROGRESO GLOBAL
            // ============================
            Progress = new ProgressService();

            // ============================
            //  CONVERSIÓN PS1 (BIN/CUE → VCD)
            // ============================
            Converter = new ConverterService(
                LogService.Write,
                Paths,
                Settings,
                Notifications.Show,
                Progress.SetStatus
            );

            // ============================
            //  GAME PROCESSOR (PS1 + PS2)
            //  Lazy → solo se crea cuando se usa
            // ============================
            _gameProcessor = new Lazy<GameProcessor>(() =>
                new GameProcessor(
                    Progress,        // ✔ ProgressService
                    LogService,      // ✔ LoggingService
                    Notifications,   // ✔ NotificationService
                    Paths,           // ✔ PathsService
                    Settings.UseDatabase, // ✔ bool
                    Settings.UseCovers    // ✔ bool
                ));
        }
    }
}
