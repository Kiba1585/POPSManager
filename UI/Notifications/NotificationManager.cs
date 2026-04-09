using System;
using System.Collections.Generic;
using System.Windows;
using POPSManager.Models;

namespace POPSManager.UI.Notifications
{
    public class NotificationManager
    {
        private readonly NotificationHost _host;
        private readonly List<NotificationToast> _activeToasts = new();

        public NotificationManager(NotificationHost host)
        {
            _host = host;
        }

        // ============================================================
        //  MOSTRAR NOTIFICACIÓN
        // ============================================================
        public void Show(UiNotification notification)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var toast = new NotificationToast(
                    title: GetTitle(notification.Type),
                    message: notification.Message,
                    type: notification.Type
                );

                toast.Closed += OnToastClosed;

                _activeToasts.Add(toast);
                _host.ShowToast(toast);
            });
        }

        // ============================================================
        //  CUANDO UN TOAST SE CIERRA
        // ============================================================
        private void OnToastClosed(NotificationToast toast)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _activeToasts.Remove(toast);
            });
        }

        // ============================================================
        //  TÍTULOS AUTOMÁTICOS POR TIPO
        // ============================================================
        private string GetTitle(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => "Éxito",
                NotificationType.Error   => "Error",
                NotificationType.Warning => "Advertencia",
                NotificationType.Info    => "Información",
                _ => "Mensaje"
            };
        }
    }
}
