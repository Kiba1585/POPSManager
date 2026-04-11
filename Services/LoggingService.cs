using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using POPSManager.Services.Interfaces;

namespace POPSManager.Services
{
    /// <summary>
    /// Servicio de logging asíncrono con Channel<T> para escritura no-bloqueante.
    /// Implementa IAsyncDisposable para flush seguro al cerrar la aplicación.
    /// </summary>
    public sealed class LoggingService : ILoggingService, IAsyncDisposable
    {
        public Action<string>? OnLog { get; set; }

        private readonly string _logFolder;
        private readonly string _logFilePath;
        private readonly Channel<string> _channel;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _writerTask;
        private readonly Stopwatch _flushTimer = Stopwatch.StartNew();
        private const int FlushIntervalMs = 500;
        private const int MaxDaysToKeep = 7;

        public LoggingService()
        {
            string appData = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);
            _logFolder = Path.Combine(appData, "POPSManager", "Logs");
            Directory.CreateDirectory(_logFolder);
            _logFilePath = Path.Combine(_logFolder,
                $"log_{DateTime.Now:yyyyMMdd}.txt");

            // Channel sin límite para evitar bloqueos en la UI
            _channel = Channel.CreateUnbounded<string>(
                new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            // Iniciar writer en background
            _writerTask = Task.Run(()
                => ProcessLogQueueAsync(_cts.Token));

            RotateOldLogs();
        }

        // ============================================================
        // MÉTODO PRINCIPAL (Action<string> compatible)
        // ============================================================
        public void Write(string message)
            => WriteInternal(message, "INFO");

        // ============================================================
        // ATAJOS PARA NIVELES
        // ============================================================
        public void Info(string msg)
            => WriteInternal(msg, "INFO");
        public void Warn(string msg)
            => WriteInternal(msg, "WARN");
        public void Error(string msg)
            => WriteInternal(msg, "ERROR");
        public void WriteWarn(string msg)
            => Warn(msg);
        public void WriteError(string msg)
            => Error(msg);

        // ============================================================
        // MÉTODO INTERNO (ENQUEUE NO-BLOQUEANTE)
        // ============================================================
        private void WriteInternal(string message, string level)
        {
            string timestamp = DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss");
            string formatted = $"[{timestamp}] [{level}] {message}";

            // Enviar a la UI (síncrono, rápido)
            OnLog?.Invoke(formatted);

            // Enviar a consola (debug)
            Debug.WriteLine(formatted);

            // Enqueue para escritura asíncrona en archivo
            _channel.Writer.TryWrite(formatted);
        }

        // ============================================================
        // WRITER BACKGROUND (CONSUME DEL CHANNEL)
        // ============================================================
        private async Task ProcessLogQueueAsync(
            CancellationToken ct)
        {
            var buffer = new StringBuilder(4096);

            try
            {
                await foreach (var line in
                    _channel.Reader.ReadAllAsync(ct))
                {
                    buffer.AppendLine(line);

                    // Flush por tiempo o por tamaño del buffer
                    if (_flushTimer.ElapsedMilliseconds
                        >= FlushIntervalMs
                        || buffer.Length > 8192)
                    {
                        await FlushBufferAsync(buffer);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada al cerrar
            }
            finally
            {
                // Flush final de lo que quede en el buffer
                if (buffer.Length > 0)
                    await FlushBufferAsync(buffer);
            }
        }

        private async Task FlushBufferAsync(
            StringBuilder buffer)
        {
            if (buffer.Length == 0) return;

            try
            {
                await File.AppendAllTextAsync(
                    _logFilePath,
                    buffer.ToString(),
                    Encoding.UTF8);
                buffer.Clear();
                _flushTimer.Restart();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[LOG ERROR] No se pudo escribir en el archivo: {ex.Message}");
            }
        }

        // ============================================================
        // ROTACIÓN AUTOMÁTICA DE LOGS
        // ============================================================
        private void RotateOldLogs()
        {
            try
            {
                var cutoff = DateTime.Now.AddDays(-MaxDaysToKeep);
                foreach (var file in
                    Directory.GetFiles(_logFolder, "log_*.txt"))
                {
                    if (new FileInfo(file).CreationTime < cutoff)
                        File.Delete(file);
                }
            }
            catch
            {
                // No romper la app si falla la limpieza
            }
        }

        // ============================================================
        // DISPOSE ASYNC (FLUSH SEGURO AL CERRAR)
        // ============================================================
        public async ValueTask DisposeAsync()
        {
            _channel.Writer.Complete();

            try
            {
                // Esperar a que se procesen todos los logs pendientes
                await _writerTask.WaitAsync(
                    TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
                Debug.WriteLine(
                    "[LOG] Timeout esperando flush de logs.");
            }
            finally
            {
                _cts.Cancel();
                _cts.Dispose();
            }
        }
    }
}
