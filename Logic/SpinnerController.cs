using System;
using System.Threading;
using System.Threading.Tasks;

namespace POPSManager.Logic
{
    /// <summary>
    /// Controlador de spinner asíncrono para la UI.
    /// Seguro, cancelable y sin fugas de tareas.
    /// </summary>
    public class SpinnerController
    {
        private readonly Action<string> update;
        private CancellationTokenSource? cts;
        private readonly string frames = "|/-\\";
        private int index = 0;

        public SpinnerController(Action<string> update)
        {
            this.update = update ?? throw new ArgumentNullException(nameof(update));
        }

        /// <summary>
        /// Inicia la animación del spinner.
        /// Si ya está corriendo, se reinicia limpiamente.
        /// </summary>
        public void Start()
        {
            Stop(); // Garantiza que no haya un spinner previo corriendo

            cts = new CancellationTokenSource();
            var token = cts.Token;

            Task.Run(async () =>
            {
                index = 0;

                while (!token.IsCancellationRequested)
                {
                    update(frames[index % frames.Length].ToString());
                    index++;
                    await Task.Delay(100, token).ConfigureAwait(false);
                }

                update(""); // Limpia el spinner al detenerse
            }, token);
        }

        /// <summary>
        /// Detiene la animación del spinner.
        /// </summary>
        public void Stop()
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
                cts.Dispose();
            }

            cts = null;
        }
    }
}
