using System;
using System.Threading;
using System.Threading.Tasks;
using POPSManager.Services.Interfaces;

namespace POPSManager.Logic
{
    public sealed class SpinnerController : IDisposable
    {
        public enum SpinnerMode
        {
            Braille,
            Dots,
            Bar
        }

        private readonly Action<string> update;
        private readonly INotificationService notifications;
        private CancellationTokenSource? cts;
        private readonly object sync = new();
        private bool disposed;
        private int index;

        private static readonly string[] BrailleFrames =
        {
            "⠋","⠙","⠹","⠸","⠼","⠴","⠦","⠧","⠇","⠏"
        };

        private static readonly string[] DotFrames =
        {
            ".", "..", "...", "...."
        };

        private static readonly string[] BarFrames =
        {
            "|", "/", "-", "\\"
        };

        public SpinnerMode Mode { get; private set; } = SpinnerMode.Braille;

        public SpinnerController(Action<string> update, INotificationService notifications)
        {
            this.update = update ?? throw new ArgumentNullException(nameof(update));
            this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        }

        public void SetMode(SpinnerMode mode)
        {
            ThrowIfDisposed();
            Mode = mode;
        }

        public void Start(int intervalMs = 80, string? message = "Procesando…")
        {
            ThrowIfDisposed();

            lock (sync)
            {
                StopInternal();

                notifications.Info(message ?? "Procesando…");

                cts = new CancellationTokenSource();
                var token = cts.Token;

                Task.Run(async () =>
                {
                    try
                    {
                        index = 0;

                        while (!token.IsCancellationRequested)
                        {
                            update(GetFrame());
                            index++;

                            await Task.Delay(intervalMs, token).ConfigureAwait(false);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        notifications.Warning("Operación cancelada");
                    }
                    catch (Exception ex)
                    {
                        notifications.Error($"Error: {ex.Message}");
                    }
                    finally
                    {
                        update("");
                        notifications.Success("Completado");
                    }
                }, token);
            }
        }

        private string GetFrame()
        {
            return Mode switch
            {
                SpinnerMode.Braille => BrailleFrames[index % BrailleFrames.Length],
                SpinnerMode.Dots => DotFrames[index % DotFrames.Length],
                SpinnerMode.Bar => BarFrames[index % BarFrames.Length],
                _ => "?"
            };
        }

        public void Stop()
        {
            ThrowIfDisposed();

            lock (sync)
            {
                StopInternal();
            }
        }

        private void StopInternal()
        {
            if (cts != null)
            {
                try
                {
                    if (!cts.IsCancellationRequested)
                        cts.Cancel();
                }
                catch { }

                cts.Dispose();
                cts = null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SpinnerController));
        }

        public void Dispose()
        {
            if (disposed)
                return;

            lock (sync)
            {
                StopInternal();
                disposed = true;
            }
        }
    }
}
