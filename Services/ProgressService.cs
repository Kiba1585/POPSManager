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

        // Estado interno
        public bool IsRunning { get; private set; }
        private DateTime _lastUpdate = DateTime.MinValue;

        // ============================================================
        //  INICIAR PROGRESO
        // ============================================================
        public void Start(string? status = null)
        {
            IsRunning = true;

            OnStart?.Invoke();

            if (!string.IsNullOrWhiteSpace(status))
                OnStatus?.Invoke(status);
        }

        // ============================================================
        //  ACTUALIZAR PORCENTAJE (con validación + throttling)
        // ============================================================
        public void SetProgress(int value)
        {
            if (!IsRunning)
                return;

            // Clamp 0–100
            if (value < 0) value = 0;
            if (value > 100) value = 100;

            // Throttling: evitar saturar la UI
            if ((DateTime.Now - _lastUpdate).TotalMilliseconds < 50)
                return;

            _lastUpdate = DateTime.Now;

            OnProgress?.Invoke(value);
        }

        // ============================================================
        //  ACTUALIZAR TEXTO DE ESTADO
        // ============================================================
        public void SetStatus(string text)
        {
            if (!IsRunning)
                return;

            if (!string.IsNullOrWhiteSpace(text))
                OnStatus?.Invoke(text);
        }

        // ============================================================
        //  DETENER PROGRESO
        // ============================================================
        public void Stop()
        {
            if (!IsRunning)
                return;

            IsRunning = false;

            OnStop?.Invoke();
        }

        // ============================================================
        //  REINICIAR (opcional)
        // ============================================================
        public void Reset()
        {
            IsRunning = false;
            _lastUpdate = DateTime.MinValue;
        }
    }
}
