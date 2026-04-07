using POPSManager.Logic;
using POPSManager.Models;

namespace POPSManager.Services
{
    public class AppServices
    {
        public SettingsService Settings { get; }
        public PathsService Paths { get; }
        public LoggingService LogService { get; }
        public NotificationService Notifications { get; }
        public ProgressService Progress { get; }
        public ConverterService Converter { get; }
        public GameProcessor GameProcessor => _gameProcessor.Value;

        private readonly Lazy<GameProcessor> _gameProcessor;

        public AppServices()
        {
            LogService = new LoggingService();
            Settings = new SettingsService(LogService.Write);
            Paths = new PathsService(LogService.Write, Settings);

            Notifications = new NotificationService();
            Progress = new ProgressService();

            Converter = new ConverterService(
                LogService.Write,
                Paths,
                Settings,
                Notifications.Show,
                Progress.SetStatus
            );

            _gameProcessor = new Lazy<GameProcessor>(() =>
                new GameProcessor(
                    Progress.SetProgress,
                    Progress.SetStatus,
                    LogService.Write,
                    Notifications.Show,
                    Paths
                ));
        }
    }
}
