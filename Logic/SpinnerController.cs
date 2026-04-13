using System;
using System.Threading;
using System.Threading.Tasks;

namespace POPSManager.Logic
{
    /// <summary>
    /// Controlador de spinner asíncrono, seguro, cancelable y sin fugas.
    /// Compatible con WPF (UI thread-safe).
    /// </summary>
    public sealed class SpinnerController : IDisposable
    {
        private readonly Action<string> update;
        private CancellationTokenSource? cts;
        private readonly string frames = "|/-\\";
        private int index; // CA1805 corregido (sin inicialización redundante)
        private readonly object sync = new();
        private bool disposed;

        public SpinnerController(Action<string> update)
        {
            this.update = update ?? throw new ArgumentNullException(nameof(update));
        }

        /// <summary>
        /// Inicia el spinner. Si ya está corriendo, se reinicia limpiamente.
        /// </summary>
        public void Start(int intervalMs = 100)
        {
            ThrowIfDisposed();

            lock (sync)
            {
                StopInternal();

                cts = new CancellationTokenSource();
                var token = cts.Token;

                Task.Run(async () =>
                {
                    try
                    {
                        index = 0;

                        while (!token.IsCancellationRequested)
                        {
                            update(frames[index % frames.Length].ToString());
                            index++;

                            await Task.Delay(intervalMs, token).ConfigureAwait(false);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Cancelación normal
                    }
                    catch (Exception ex)
                    {
                        update($"ERR:{ex.Message}");
                    }
                    finally
                    {
                        update("");
                    }
                }, token);
            }
        }

        /// <summary>
        /// Detiene el spinner inmediatamente.
        /// </summary>
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
                catch
                {
                    // Ignorar errores de cancelación
                }

                cts.Dispose();
                cts = null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SpinnerController));
        }

        /// <summary>
        /// Libera los recursos del spinner.
        /// </summary>
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
