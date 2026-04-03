using System;

namespace POPSManager.Services
{
    public class ProgressService
    {
        // Callbacks asignados desde MainWindow
        public Action? OnStart;
        public Action? OnStop;
        public Action<int>? OnProgress;
        public Action<string>? OnStatus;

        // ============================================================
        //  INICIAR PROGRESO
        // ============================================================
        public void Start(string? status = null)
        {
            OnStart?.Invoke();

            if (!string.IsNullOrWhiteSpace(status))
                OnStatus?.Invoke(status);
        }

        // ============================================================
        //  ACTUALIZAR PORCENTAJE
        // ============================================================
        public void SetProgress(int value)
        {
            OnProgress?.Invoke(value);
        }

        // ============================================================
        //  ACTUALIZAR TEXTO DE ESTADO
        // ============================================================
        public void SetStatus(string text)
        {
            OnStatus?.Invoke(text);
        }

        // ============================================================
        //  DETENER PROGRESO
        // ============================================================
        public void Stop()
        {
            OnStop?.Invoke();
        }
    }
}
