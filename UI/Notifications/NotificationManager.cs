using System;
using System.Collections.Generic;
using System.Windows;
using POPSManager.Models;
using POPSManager.UI.Localization;

namespace POPSManager.UI.Notifications
{
    public class NotificationManager
    {
        private readonly NotificationHost _host;
        private readonly LocalizationService _localization;
        private readonly List<NotificationToast> _activeToasts = new();

        public NotificationManager(NotificationHost host, LocalizationService localization)
        {
            _host = host;
            _localization = localization;
        }

        public void Show(UiNotification notification)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var toast = new NotificationToast(
                    title: GetLocalizedTitle(notification.Type),
                    message: notification.Message,
                    type: notification.Type
                );

                toast.Closed += OnToastClosed;

                _activeToasts.Add(toast);
                _host.ShowToast(toast);
            });
        }

        private void OnToastClosed(NotificationToast toast)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _activeToasts.Remove(toast);
                _host.RemoveToast(toast);
            });
        }

        private string GetLocalizedTitle(NotificationType type)
        {
            string key = type switch
            {
                NotificationType.Success => "Notification_Success",
                NotificationType.Error   => "Notification_Error",
                NotificationType.Warning => "Notification_Warning",
                NotificationType.Info    => "Notification_Info",
                _ => "Notification_Message"
            };

            return _localization.GetString(key);
        }
    }
}