using System;
using System.IO;

namespace POPSManager.Services
{
    public class LoggingService
    {
        public Action<string>? OnLog;

        private readonly string logFilePath;

        public LoggingService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "POPSManager", "Logs");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            logFilePath = Path.Combine(folder, $"log_{DateTime.Now:yyyyMMdd}.txt");
        }

        // ============================
        //  MÉTODO PRINCIPAL
        // ============================

        public void Write(string message, string level = "INFO")
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string formatted = $"[{timestamp}] [{level}] {message}";

            // Enviar a la UI
            OnLog?.Invoke(formatted);

            // Enviar a consola
            Console.WriteLine(formatted);

            // Guardar en archivo
            try
            {
                File.AppendAllText(logFilePath, formatted + Environment.NewLine);
            }
            catch
            {
                // Si falla el archivo, no rompemos la app
            }
        }

        // ============================
        //  ATAJOS PARA NIVELES
        // ============================

        public void Info(string msg) => Write(msg, "INFO");
        public void Warn(string msg) => Write(msg, "WARN");
        public void Error(string msg) => Write(msg, "ERROR");
    }
}
