using System;
using POPSManager.Models;

namespace POPSManager.Services
{
    public class NotificationService
    {
        // Callback único para mostrar toasts en la UI
        public Action<string, NotificationType>? OnShowToast { get; set; }

        // ============================================================
        //  MÉTODO PRINCIPAL
        // ============================================================
        public void Show(string message, NotificationType type)
        {
            try
            {
                OnShowToast?.Invoke(message, type);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NOTIFY ERROR] {ex.Message}");
            }
        }

        // ============================================================
        //  ATAJOS ULTRA PRO
        // ============================================================
        public void Success(string msg) =>
            Show(msg, NotificationType.Success);

        public void Info(string msg) =>
            Show(msg, NotificationType.Info);

        public void Warning(string msg) =>
            Show(msg, NotificationType.Warning);

        public void Error(string msg) =>
            Show(msg, NotificationType.Error);
    }
}
