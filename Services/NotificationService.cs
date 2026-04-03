using POPSManager.Models;
using System;

namespace POPSManager.Services
{
    public class NotificationService
    {
        public Action<UiNotification>? OnNotify;

        public void Show(UiNotification notification)
        {
            OnNotify?.Invoke(notification);
        }
    }
}
