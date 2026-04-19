using System;
using System.Threading.Tasks;
using POPSManager.Logic;
using POPSManager.Logic.Automation;
using POPSManager.Logic.Cheats;
using POPSManager.Services;
using POPSManager.Settings;
using POPSManager.UI.Localization;

namespace POPSManager
{
    /// <summary>
    /// Contenedor global de servicios con soporte async.
    /// Mantiene compatibilidad total con la arquitectura original.
    /// </summary>
    public sealed class AppServices
    {
        public LoggingService LogService { get; }
        public NotificationService Notifications { get; }
        public ProgressService Progress { get; }
        public PathsService Paths { get; }
        public SettingsService Settings { get; }
        public AutomationEngine Automation { get; }
        public ConverterService Converter { get; }
        public GameProcessor GameProcessor { get; }
        public LocalizationService Localization { get; }

        public AppServices()
        {
            // ============================================================
            // 1. Servicios base
            // ============================================================
            LogService = new LoggingService();
            Notifications = new NotificationService();
            Progress = new ProgressService();

            // ============================================================
            // 2. Settings
            // ============================================================
            Settings = new SettingsService(LogService.Info, Notifications);

            // ============================================================
            // 3. AutomationEngine
            // ============================================================
            Automation = new AutomationEngine(Settings, Notifications, LogService);

            // ============================================================
            // 4. PathsService
            // ============================================================
            Paths = new PathsService(LogService.Info, Settings, Automation);

            // ============================================================
            // 5. LocalizationService (debe crearse después de Settings)
            // ============================================================
            Localization = new LocalizationService(Settings);

            // ============================================================
            // 6. ConverterService (ahora recibe LocalizationService)
            // ============================================================
            Converter = new ConverterService(
                LogService.Info,
                Paths,
                Settings,
                Automation,
                Localization,
                Notifications.Show,
                Progress.SetStatus
            );

            // ============================================================
            // 7. Cheat Services (usando rutas desde PathsService)
            // ============================================================
            var cheatSettings = new CheatSettingsService(
                Paths.RootFolder,               // ✅ Ruta raíz real
                LogService.Info
            );
            var cheatManager = new CheatManagerService(cheatSettings, LogService.Info);

            // ============================================================
            // 8. GameProcessor
            // ============================================================
            GameProcessor = new GameProcessor(
                Progress,
                LogService,
                Notifications,
                Paths,
                cheatSettings,
                cheatManager,
                Settings,
                Automation
            );
        }

        // ============================================================
        //  Inicialización async
        // ============================================================
        public async Task InitializeAsync()
        {
            await Paths.ReloadAsync();
            await Settings.SaveAsync();

            LogService.Info("[AppServices] Inicialización async completada.");
        }
    }
}