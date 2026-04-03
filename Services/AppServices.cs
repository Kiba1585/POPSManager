using POPSManager.Logic;
using POPSManager.Models;

namespace POPSManager.Services
{
    public class AppServices
    {
        public NotificationService Notifications { get; }
        public LoggingService Logging { get; }
        public ProgressService Progress { get; }
        public PathsService Paths { get; }
        public SettingsService Settings { get; }

        public Converter Converter { get; }
        public GameProcessor GameProcessor { get; }

        public AppServices()
        {
            Notifications = new NotificationService();
            Logging = new LoggingService();
            Progress = new ProgressService();
            Paths = new PathsService();
            Settings = new SettingsService();

            Converter = new Converter(
                updateProgress: Progress.Update,
                updateSpinner: Progress.SetStatus,
                log: Logging.Write,
                notify: Notifications.Show
            );

            GameProcessor = new GameProcessor(
                updateProgress: Progress.Update,
                updateSpinner: Progress.SetStatus,
                log: Logging.Write,
                notify: Notifications.Show
            );
        }

        // Métodos accesibles globalmente
        public void Notify(UiNotification n) => Notifications.Show(n);
        public void Log(string msg) => Logging.Write(msg);
    }
}
