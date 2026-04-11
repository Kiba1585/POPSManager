namespace POPSManager.Services.Interfaces
{
    /// <summary>
    /// Contrato para el servicio de logging de la aplicación.
    /// Permite inyección de dependencias y testing.
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>Callback para enviar logs a la UI.</summary>
        Action<string>? OnLog { get; set; }

        /// <summary>Escribe un mensaje con nivel INFO.</summary>
        void Write(string message);

        /// <summary>Log con nivel INFO.</summary>
        void Info(string msg);

        /// <summary>Log con nivel WARN.</summary>
        void Warn(string msg);

        /// <summary>Log con nivel ERROR.</summary>
        void Error(string msg);

        /// <summary>Alias para Warn.</summary>
        void WriteWarn(string msg);

        /// <summary>Alias para Error.</summary>
        void WriteError(string msg);
    }
}

