using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using POPSManager.Services.Interfaces;

namespace POPSManager.Services
{
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
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logFolder = Path.Combine(appData, "POPSManager", "Logs");
            Directory.CreateDirectory(_logFolder);

            _logFilePath = Path.Combine(_logFolder, $"log_{DateTime.Now:yyyyMMdd}.txt");

            _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            _writerTask = Task.Run(() => ProcessLogQueueAsync(_cts.Token));

            RotateOldLogs();
        }

        public void Write(string message) => WriteInternal(message, "INFO");
        public void Info(string msg) => WriteInternal(msg, "INFO");
        public void Warn(string msg) => WriteInternal(msg, "WARN");
        public void Error(string msg) => WriteInternal(msg, "ERROR");

        public void WriteWarn(string msg) => Warn(msg);
        public void WriteError(string msg) => Error(msg);

        private void WriteInternal(string message, string level)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            string formatted = $"[{timestamp}] [{level}] {message}";

            OnLog?.Invoke(formatted);
            Debug.WriteLine(formatted);

            _channel.Writer.TryWrite(formatted);
        }

        private async Task ProcessLogQueueAsync(CancellationToken ct)
        {
            var buffer = new StringBuilder(4096);

            try
            {
                await foreach (var line in _channel.Reader.ReadAllAsync(ct))
                {
                    buffer.AppendLine(line);

                    if (_flushTimer.ElapsedMilliseconds >= FlushIntervalMs || buffer.Length > 8192)
                        await FlushBufferAsync(buffer);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (buffer.Length > 0)
                    await FlushBufferAsync(buffer);
            }
        }

        private async Task FlushBufferAsync(StringBuilder buffer)
        {
            if (buffer.Length == 0) return;

            try
            {
                await File.AppendAllTextAsync(_logFilePath, buffer.ToString(), Encoding.UTF8);
                buffer.Clear();
                _flushTimer.Restart();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOG ERROR] No se pudo escribir en el archivo: {ex.Message}");
            }
        }

        private void RotateOldLogs()
        {
            try
            {
                var cutoff = DateTime.Now.AddDays(-MaxDaysToKeep);

                foreach (var file in Directory.GetFiles(_logFolder, "log_*.txt"))
                {
                    if (new FileInfo(file).CreationTime < cutoff)
                        File.Delete(file);
                }
            }
            catch
            {
            }
        }

        public async ValueTask DisposeAsync()
        {
            _channel.Writer.Complete();

            try
            {
                await _writerTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
                Debug.WriteLine("[LOG] Timeout esperando flush de logs.");
            }
            finally
            {
                _cts.Cancel();
                _cts.Dispose();
            }
        }
    }
}
