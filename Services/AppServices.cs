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
        public GameProcessor GameProcessor { get; }

        public AppServices()
        {
            Paths = new PathsService();
            Settings = new SettingsService();
            LogService = new LoggingService();
            Notifications = new NotificationService();
            Progress = new ProgressService();

            GameProcessor = new GameProcessor(
                Progress.SetProgress,
                Progress.SetStatus,
                LogService.Write,
                Notifications.Show,
                Paths
            );
        }
    }
}
