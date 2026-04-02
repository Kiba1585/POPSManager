namespace POPSManager.Logic
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public record UiNotification(NotificationType Type, string Message);
}
