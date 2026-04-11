using POPSManager.Logic;
using POPSManager.Models;
using POPSManager.Settings;
using POPSManager.Logic.Cheats;

namespace POPSManager.Services
{
    public class AppServices
    {
        // ============================
        //  SERVICIOS PRINCIPALES
        // ============================
        public SettingsService Settings { get; } = null!;
        public PathsService Paths { get; } = null!;
        public LoggingService LogService { get; } = null!;
        public NotificationService Notifications { get; } = null!;
        public ProgressService Progress { get; } = null!;
        public ConverterService Converter { get; } = null!;

        // Cheats
        public CheatSettingsService CheatSettings { get; } = null!;
        public CheatManagerService CheatManager { get; } = null!;

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
            //  NOTIFICACIONES (ULTRA PRO)
            // ============================
            Notifications = new NotificationService();

            // ============================
            //  PROGRESO GLOBAL
            // ============================
            Progress = new ProgressService();

            // ============================
            //  CHEATS (CONFIG + MANAGER)
            // ============================
            CheatSettings = new CheatSettingsService(Paths.RootFolder, LogService.Write);
            CheatManager = new CheatManagerService(CheatSettings, LogService.Write);

            // ============================
            //  CONVERSIÓN PS1 (BIN/CUE → VCD)
            // ============================
            Converter = new ConverterService(
                LogService.Write,
                Paths,
                Settings,
                (msg, type) => Notifications.Show(msg, type),
                Progress.SetStatus
            );

            // ============================
            //  GAME PROCESSOR (PS1 + PS2)
            // ============================
            _gameProcessor = new Lazy<GameProcessor>(() =>
                new GameProcessor(
                    Progress,
                    LogService,
                    Notifications,
                    Paths,
                    CheatSettings,
                    CheatManager,
                    Settings.UseDatabase,
                    Settings.UseCovers
                ));
        }
    }
}
