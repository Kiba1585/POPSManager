using System;
using System.Diagnostics;
using POPSManager.Services.Interfaces;

namespace POPSManager.Services
{
    /// <summary>
    /// Servicio de progreso global optimizado.
    /// Usa Stopwatch monotónico para throttling preciso.
    /// </summary>
    public class ProgressService : IProgressService
    {
        public Action? OnStart { get; set; }
        public Action? OnStop { get; set; }
        public Action<int>? OnProgress { get; set; }
        public Action<string>? OnStatus { get; set; }

        public bool IsRunning { get; private set; }

        private readonly Stopwatch _throttleWatch = new();
        private int _lastReportedValue = -1;

        private const int ThrottleMs = 50;

        // ============================================================
        // INICIAR PROGRESO
        // ============================================================
        public void Start(string? status = null)
        {
            IsRunning = true;
            _lastReportedValue = -1;
            _throttleWatch.Restart();

            OnStart?.Invoke();

            if (!string.IsNullOrWhiteSpace(status))
                OnStatus?.Invoke(status);
        }

        // ============================================================
        // ACTUALIZAR PORCENTAJE
        // ============================================================
        public void SetProgress(int value)
        {
            if (!IsRunning)
                return;

            value = Math.Clamp(value, 0, 100);

            if (value == _lastReportedValue)
                return;

            if (_throttleWatch.ElapsedMilliseconds < ThrottleMs && value < 100)
                return;

            _lastReportedValue = value;
            _throttleWatch.Restart();

            OnProgress?.Invoke(value);
        }

        // ============================================================
        // ACTUALIZAR TEXTO DE ESTADO
        // ============================================================
        public void SetStatus(string text)
        {
            if (!IsRunning)
                return;

            if (!string.IsNullOrWhiteSpace(text))
                OnStatus?.Invoke(text);
        }

        // ============================================================
        // DETENER PROGRESO
        // ============================================================
        public void Stop()
        {
            if (!IsRunning)
                return;

            IsRunning = false;
            _throttleWatch.Stop();

            OnStop?.Invoke();
        }

        // ============================================================
        // REINICIAR
        // ============================================================
        public void Reset()
        {
            IsRunning = false;
            _lastReportedValue = -1;
            _throttleWatch.Reset();
        }
    }
}
