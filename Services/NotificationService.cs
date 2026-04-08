using System;
using System.Collections.Generic;
using POPSManager.Models;

namespace POPSManager.Services
{
    public class NotificationService
    {
        // Lista de listeners (permite múltiples suscriptores)
        private readonly List<Action<UiNotification>> _listeners = new();

        // ============================================================
        //  SUSCRIPCIÓN
        // ============================================================
        public void Subscribe(Action<UiNotification> callback)
        {
            if (callback == null) return;
            if (!_listeners.Contains(callback))
                _listeners.Add(callback);
        }

        public void Unsubscribe(Action<UiNotification> callback)
        {
            if (callback == null) return;
            _listeners.Remove(callback);
        }

        // ============================================================
        //  MÉTODO PRINCIPAL
        // ============================================================
        public void Show(UiNotification notification)
        {
            foreach (var listener in _listeners)
            {
                try
                {
                    listener(notification);
                }
                catch (Exception ex)
                {
                    // Nunca romper la app por una notificación
                    Console.WriteLine($"[NOTIFY ERROR] {ex.Message}");
                }
            }
        }

        // ============================================================
        //  ATAJOS ULTRA PRO
        // ============================================================
        public void Success(string msg) =>
            Show(new UiNotification(NotificationType.Success, msg));

        public void Info(string msg) =>
            Show(new UiNotification(NotificationType.Info, msg));

        public void Warning(string msg) =>
            Show(new UiNotification(NotificationType.Warning, msg));

        public void Error(string msg) =>
            Show(new UiNotification(NotificationType.Error, msg));
    }
}
