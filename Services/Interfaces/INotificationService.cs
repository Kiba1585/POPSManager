using POPSManager.Models;

namespace POPSManager.Services.Interfaces
{
    /// <summary>
    /// Contrato para el servicio de notificaciones de la aplicación.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>Evento que la UI conecta al NotificationManager.</summary>
        Action<string, NotificationType>? OnShowToast { get; set; }

        void Show(string message, NotificationType type);
        void Success(string message);
        void Error(string message);
        void Warning(string message);
        void Info(string message);
    }
}
