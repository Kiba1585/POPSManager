using System;

namespace POPSManager.Services.Interfaces
{
    /// <summary>
    /// Contrato para el servicio de progreso global.
    /// Gestiona el estado de las operaciones largas y notifica a la UI.
    /// </summary>
    public interface IProgressService
    {
        /// <summary>Se dispara al iniciar una operación.</summary>
        Action? OnStart { get; set; }

        /// <summary>Se dispara al finalizar una operación.</summary>
        Action? OnStop { get; set; }

        /// <summary>Se dispara al actualizar el porcentaje (0-100).</summary>
        Action<int>? OnProgress { get; set; }

        /// <summary>Se dispara al cambiar el texto de estado.</summary>
        Action<string>? OnStatus { get; set; }

        /// <summary>Indica si hay una operación en curso.</summary>
        bool IsRunning { get; }

        /// <summary>Inicia el seguimiento de progreso.</summary>
        /// <param name="status">Texto de estado inicial (opcional).</param>
        void Start(string? status = null);

        /// <summary>Actualiza el porcentaje de progreso.</summary>
        /// <param name="value">Valor entre 0 y 100.</param>
        void SetProgress(int value);

        /// <summary>Actualiza el texto de estado.</summary>
        /// <param name="text">Nuevo texto.</param>
        void SetStatus(string text);

        /// <summary>Detiene el seguimiento de progreso.</summary>
        void Stop();

        /// <summary>Reinicia el estado interno (sin notificar).</summary>
        void Reset();
    }
}