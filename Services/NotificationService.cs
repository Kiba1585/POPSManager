using POPSManager.UI.Notifications;

namespace POPSManager.Services
{
    public class NotificationService
    {
        private readonly NotificationManager _manager;

        public NotificationService(NotificationManager manager)
        {
            _manager = manager;
        }

        public void Success(string msg) => _manager.ShowSuccess(msg);
        public void Error(string msg)   => _manager.ShowError(msg);
        public void Warning(string msg) => _manager.ShowWarning(msg);
        public void Info(string msg)    => _manager.ShowInfo(msg);
    }
}
