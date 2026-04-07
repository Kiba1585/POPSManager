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

        // ============================================================
        //  MÉTODO REQUERIDO POR Action<string>
        // ============================================================

        public void Write(string message)
        {
            WriteInternal(message, "INFO");
        }

        // ============================================================
        //  MÉTODO INTERNO REAL (CON NIVEL)
        // ============================================================

        private void WriteInternal(string message, string level)
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
                // No romper la app si falla el archivo
            }
        }

        // ============================================================
        //  ATAJOS PARA NIVELES
        // ============================================================

        public void Info(string msg) => WriteInternal(msg, "INFO");
        public void Warn(string msg) => WriteInternal(msg, "WARN");
        public void Error(string msg) => WriteInternal(msg, "ERROR");

        // Opcionales (más expresivos)
        public void WriteWarn(string msg) => Warn(msg);
        public void WriteError(string msg) => Error(msg);
    }
}
