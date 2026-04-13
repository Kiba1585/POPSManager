using System;
using POPSManager.Models;
using POPSManager.Services.Interfaces;

namespace POPSManager.Services
{
    /// <summary>
    /// Servicio de notificaciones desacoplado.
    /// La UI se suscribe mediante OnShowToast.
    /// </summary>
    public class NotificationService : INotificationService
    {
        /// <summary>
        /// Callback para mostrar toasts en la UI.
        /// Firma: (mensaje, tipo)
        /// </summary>
        public Action<string, NotificationType>? OnShowToast { get; set; }

        /// <summary>
        /// Método principal para mostrar notificaciones.
        /// </summary>
        public void Show(string message, NotificationType type)
        {
            OnShowToast?.Invoke(message, type);
        }

        public void Success(string message) => Show(message, NotificationType.Success);
        public void Error(string message)   => Show(message, NotificationType.Error);
        public void Warning(string message) => Show(message, NotificationType.Warning);
        public void Info(string message)    => Show(message, NotificationType.Info);
    }
}
