using System;

namespace POPSManager.Services.Interfaces
{
    /// <summary>
    /// Contrato para el servicio de logging de la aplicación.
    /// Permite inyección de dependencias y testing.
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Callback opcional para enviar logs a la UI en tiempo real.
        /// </summary>
        Action<string>? OnLog { get; set; }

        /// <summary>
        /// Escribe un mensaje con nivel INFO.
        /// </summary>
        /// <param name="message">Mensaje a registrar.</param>
        void Write(string message);

        /// <summary>
        /// Registra un mensaje informativo.
        /// </summary>
        /// <param name="msg">Mensaje.</param>
        void Info(string msg);

        /// <summary>
        /// Registra una advertencia.
        /// </summary>
        /// <param name="msg">Mensaje de advertencia.</param>
        void Warn(string msg);

        /// <summary>
        /// Registra un error.
        /// </summary>
        /// <param name="msg">Mensaje de error.</param>
        void Error(string msg);

        /// <summary>
        /// Alias de Warn.
        /// </summary>
        /// <param name="msg">Mensaje de advertencia.</param>
        void WriteWarn(string msg);

        /// <summary>
        /// Alias de Error.
        /// </summary>
        /// <param name="msg">Mensaje de error.</param>
        void WriteError(string msg);
    }
}