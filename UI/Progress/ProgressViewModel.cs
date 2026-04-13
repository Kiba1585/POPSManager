using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

namespace POPSManager.UI.Progress
{
    public class ProgressViewModel
    {
        public ObservableCollection<GameProgressItem> Items { get; } = new();

        private readonly Dispatcher _dispatcher;

        public ProgressViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public void AddGame(string title, string gameId)
        {
            _dispatcher.Invoke(() =>
            {
                Items.Add(new GameProgressItem
                {
                    Title = title,
                    GameId = gameId,
                    Progress = 0,
                    Status = "Iniciando…"
                });
            });
        }

        public void UpdateStatus(string gameId, string status)
        {
            _dispatcher.Invoke(() =>
            {
                var item = Items.FirstOrDefault(i => i.GameId == gameId);
                if (item != null)
                    item.Status = status;
            });
        }

        public void UpdateProgress(string gameId, int value)
        {
            _dispatcher.Invoke(() =>
            {
                var item = Items.FirstOrDefault(i => i.GameId == gameId);
                if (item != null)
                    item.Progress = value;
            });
        }

        public void MarkCompleted(string gameId)
        {
            _dispatcher.Invoke(() =>
            {
                var item = Items.FirstOrDefault(i => i.GameId == gameId);
                if (item != null)
                {
                    item.Progress = 100;
                    item.Status = "Completado";
                    item.IsCompleted = true;
                }
            });
        }

        public void MarkError(string gameId, string message)
        {
            _dispatcher.Invoke(() =>
            {
                var item = Items.FirstOrDefault(i => i.GameId == gameId);
                if (item != null)
                {
                    item.Status = message;
                    item.IsError = true;
                }
            });
        }
    }
}
