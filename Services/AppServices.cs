using POPSManager.Services;
using POPSManager.Settings;
using POPSManager.Logic;
using POPSManager.Logic.Automation;
using System;
using System.Threading.Tasks;

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
        public GameProcessor Processor { get; }

        public AppServices()
        {
            // ============================================================
            // 1. Servicios base (sync)
            // ============================================================
            LogService = new LoggingService();
            Notifications = new NotificationService();
            Progress = new ProgressService();

            // ============================================================
            // 2. Settings (sync + async)
            // ============================================================
            Settings = new SettingsService(LogService.Info);

            // ============================================================
            // 3. AutomationEngine (sync)
            // ============================================================
            Automation = new AutomationEngine(Settings, Notifications, LogService);

            // ============================================================
            // 4. PathsService (async-ready)
            // ============================================================
            Paths = new PathsService(LogService.Info, Settings, Automation);

            // ============================================================
            // 5. ConverterService (async)
            // ============================================================
            Converter = new ConverterService(
                LogService.Info,
                Paths,
                Settings,
                Automation,
                Notifications.Show,
                Progress.SetStatus
            );

            // ============================================================
            // 6. GameProcessor (async)
            // ============================================================
            Processor = new GameProcessor(
                Progress,
                LogService,
                Notifications,
                Paths,
                new CheatSettingsService(Settings),
                new CheatManagerService(Settings),
                Settings,
                Automation
            );
        }

        // ============================================================
        //  MÉTODO DE INICIALIZACIÓN ASYNC (OPCIONAL)
        // ============================================================
        public async Task InitializeAsync()
        {
            // Recargar rutas async
            await Paths.ReloadAsync();

            // Guardar settings async
            await Settings.SaveAsync();

            LogService.Info("[AppServices] Inicialización async completada.");
        }
    }
}
