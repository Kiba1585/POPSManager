using System;
using System.Threading;
using System.Threading.Tasks;

namespace POPSManager.Logic
{
    /// <summary>
    /// Spinner asíncrono avanzado con múltiples modos de animación,
    /// seguro para WPF, cancelable, sin fugas y ultra optimizado.
    /// </summary>
    public sealed class SpinnerController : IDisposable
    {
        public enum SpinnerMode
        {
            Braille,
            Dots,
            Bar
        }

        private readonly Action<string> update;
        private CancellationTokenSource? cts;
        private readonly object sync = new();
        private bool disposed;

        private int index;

        // Animaciones premium
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

        public SpinnerController(Action<string> update)
        {
            this.update = update ?? throw new ArgumentNullException(nameof(update));
        }

        /// <summary>
        /// Cambia el modo de animación en tiempo real.
        /// </summary>
        public void SetMode(SpinnerMode mode)
        {
            ThrowIfDisposed();
            Mode = mode;
        }

        /// <summary>
        /// Inicia el spinner. Si ya está corriendo, se reinicia limpiamente.
        /// </summary>
        public void Start(int intervalMs = 80)
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
                            update(GetFrame());
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
        /// Obtiene el frame actual según el modo seleccionado.
        /// </summary>
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
