using System;
using System.Threading;
using System.Threading.Tasks;

namespace POPSManager.Logic
{
    /// <summary>
    /// Controlador de spinner asíncrono, seguro, cancelable y sin fugas.
    /// Compatible con WPF (UI thread-safe).
    /// </summary>
    public class SpinnerController
    {
        private readonly Action<string> update;
        private CancellationTokenSource? cts;
        private readonly string frames = "|/-\\";
        private int index = 0;
        private readonly object sync = new();

        public SpinnerController(Action<string> update)
        {
            this.update = update ?? throw new ArgumentNullException(nameof(update));
        }

        /// <summary>
        /// Inicia el spinner. Si ya está corriendo, se reinicia limpiamente.
        /// </summary>
        public void Start(int intervalMs = 100)
        {
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
                            // UI-safe update
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
                        // Nunca dejamos que el spinner rompa la app
                        update($"ERR:{ex.Message}");
                    }
                    finally
                    {
                        // Limpia el spinner al detenerse
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
            lock (sync)
            {
                StopInternal();
            }
        }

        private void StopInternal()
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                try
                {
                    cts.Cancel();
                }
                catch { }

                cts.Dispose();
            }

            cts = null;
        }
    }
}
