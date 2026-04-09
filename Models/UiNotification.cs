namespace POPSManager.Models
{
    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class UiNotification
    {
        public NotificationType Type { get; set; }
        public string Message { get; set; } = "";
    }
}
