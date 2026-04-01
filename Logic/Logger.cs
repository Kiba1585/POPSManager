using System;

namespace POPSManager.Logic
{
    public class Logger
    {
        private readonly Action<string> logAction;

        public Logger(Action<string> logAction)
        {
            this.logAction = logAction;
        }

        public void Log(string message)
        {
            logAction($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
