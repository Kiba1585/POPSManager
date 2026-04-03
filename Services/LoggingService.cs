using System;

namespace POPSManager.Services
{
    public class LoggingService
    {
        public Action<string>? OnLog;

        public void Write(string message)
        {
            OnLog?.Invoke(message);
            Console.WriteLine(message);
        }
    }
}
