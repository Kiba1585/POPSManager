using System;
using System.Threading.Tasks;

namespace POPSManager.Logic
{
    public class SpinnerController
    {
        private readonly Action<string> update;
        private bool running;

        public SpinnerController(Action<string> update)
        {
            this.update = update;
        }

        public void Start()
        {
            running = true;

            Task.Run(async () =>
            {
                string frames = "|/-\\";
                int i = 0;

                while (running)
                {
                    update(frames[i % frames.Length].ToString());
                    i++;
                    await Task.Delay(100);
                }

                update("");
            });
        }

        public void Stop()
        {
            running = false;
        }
    }
}
