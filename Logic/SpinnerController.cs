using System;
using System.Threading;
using System.Threading.Tasks;

namespace POPSManager.Logic
{
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

        public void Start()
        {
            Stop();

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

                update("");
            }, token);
        }

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
