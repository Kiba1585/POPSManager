using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace POPSManager.UI.Notifications
{
    public partial class NotificationManager
    {
        private readonly Panel _container;
        private readonly List<NotificationToast> _activeToasts = new();

        public NotificationManager(Panel container)
        {
            _container = container;
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
                _container.Children.Insert(0, toast);
            });
        }

        // ============================================================
        //  CUANDO UN TOAST SE CIERRA
        // ============================================================
        private void OnToastClosed(NotificationToast toast)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _container.Children.Remove(toast);
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
