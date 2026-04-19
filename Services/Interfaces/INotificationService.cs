using System;
using POPSManager.Models;

namespace POPSManager.Services.Interfaces
{
    /// <summary>
    /// Contrato para el servicio de notificaciones visuales.
    /// La UI se suscribe mediante <see cref="OnShowToast"/>.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Evento que la UI conecta al NotificationManager para mostrar toasts.
        /// </summary>
        Action<string, NotificationType>? OnShowToast { get; set; }

        /// <summary>
        /// Muestra una notificación genérica.
        /// </summary>
        /// <param name="message">Texto de la notificación.</param>
        /// <param name="type">Tipo (Success, Error, etc.).</param>
        void Show(string message, NotificationType type);

        /// <summary>
        /// Muestra una notificación de éxito.
        /// </summary>
        void Success(string message);

        /// <summary>
        /// Muestra una notificación de error.
        /// </summary>
        void Error(string message);

        /// <summary>
        /// Muestra una notificación de advertencia.
        /// </summary>
        void Warning(string message);

        /// <summary>
        /// Muestra una notificación informativa.
        /// </summary>
        void Info(string message);
    }
}