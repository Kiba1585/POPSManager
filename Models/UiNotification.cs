namespace POPSManager.Models
{
    /// <summary>
    /// Notificación que se muestra en la UI.
    /// </summary>
    public class UiNotification
    {
        /// <summary>Tipo de notificación (éxito, error, etc.).</summary>
        public NotificationType Type { get; set; }

        /// <summary>Mensaje a mostrar.</summary>
        public string Message { get; set; } = "";
    }
}