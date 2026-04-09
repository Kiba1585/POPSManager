using POPSManager.Models;

namespace POPSManager.Services
{
    public class NotificationService
    {
        // Evento que la UI (MainWindow) conectará al NotificationManager
        public Action<string, NotificationType>? OnShowToast { get; set; }

        // Métodos simples para disparar notificaciones
        public void Show(string message, NotificationType type)
        {
            OnShowToast?.Invoke(message, type);
        }

        public void Success(string message) =>
            Show(message, NotificationType.Success);

        public void Error(string message) =>
            Show(message, NotificationType.Error);

        public void Warning(string message) =>
            Show(message, NotificationType.Warning);

        public void Info(string message) =>
            Show(message, NotificationType.Info);
    }
}
