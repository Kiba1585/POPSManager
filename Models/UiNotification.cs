namespace POPSManager.Models
{
    public class UiNotification
    {
        public NotificationType Type { get; }
        public string Message { get; }

        public UiNotification(NotificationType type, string message)
        {
            Type = type;
            Message = message;
        }
    }
}
