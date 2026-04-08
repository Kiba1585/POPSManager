using System;
using System.IO;
using System.Text;

namespace POPSManager.Services
{
    public class LoggingService
    {
        public Action<string>? OnLog;

        private readonly string logFolder;
        private readonly string logFilePath;

        private readonly object _lock = new();

        public LoggingService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            logFolder = Path.Combine(appData, "POPSManager", "Logs");

            Directory.CreateDirectory(logFolder);

            logFilePath = Path.Combine(logFolder, $"log_{DateTime.Now:yyyyMMdd}.txt");

            RotateOldLogs();
        }

        // ============================================================
        //  MÉTODO PRINCIPAL (Action<string>)
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
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string formatted = $"[{timestamp}] [{level}] {message}";

            // Enviar a la UI
            OnLog?.Invoke(formatted);

            // Enviar a consola
            Console.WriteLine(formatted);

            // Guardar en archivo (seguro)
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(logFilePath, formatted + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOG ERROR] No se pudo escribir en el archivo: {ex.Message}");
            }
        }

        // ============================================================
        //  ATAJOS PARA NIVELES
        // ============================================================
        public void Info(string msg) => WriteInternal(msg, "INFO");
        public void Warn(string msg) => WriteInternal(msg, "WARN");
        public void Error(string msg) => WriteInternal(msg, "ERROR");

        // Alias opcionales
        public void WriteWarn(string msg) => Warn(msg);
        public void WriteError(string msg) => Error(msg);

        // ============================================================
        //  ROTACIÓN AUTOMÁTICA DE LOGS (ULTRA PRO)
        //  Mantiene solo 7 días de logs
        // ============================================================
        private void RotateOldLogs()
        {
            try
            {
                var files = Directory.GetFiles(logFolder, "log_*.txt");

                foreach (var file in files)
                {
                    var info = new FileInfo(file);

                    if (info.CreationTime < DateTime.Now.AddDays(-7))
                        info.Delete();
                }
            }
            catch
            {
                // No romper la app si falla la limpieza
            }
        }
    }
}
