using System;

namespace POPSManager.Services
{
    public class ProgressService
    {
        public Action<int>? OnProgress;
        public Action<string>? OnStatus;
        public Action? OnStart;
        public Action? OnStop;

        public void Start(string status)
        {
            OnStart?.Invoke();
            OnStatus?.Invoke(status);
        }

        public void Update(int value)
        {
            OnProgress?.Invoke(value);
        }

        public void SetStatus(string text)
        {
            OnStatus?.Invoke(text);
        }

        public void Stop()
        {
            OnStop?.Invoke();
        }
    }
}
