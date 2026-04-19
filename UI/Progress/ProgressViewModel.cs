using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using POPSManager.UI.Localization;

namespace POPSManager.UI.Progress
{
    public class GameProgressItem : ViewModelBase
    {
        private string _title = string.Empty;
        private string _gameId = string.Empty;
        private int _progress;
        private string _status = string.Empty;
        private bool _isCompleted;
        private bool _isError;

        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string GameId { get => _gameId; set => SetProperty(ref _gameId, value); }
        public int Progress { get => _progress; set => SetProperty(ref _progress, value); }
        public string Status { get => _status; set => SetProperty(ref _status, value); }
        public bool IsCompleted { get => _isCompleted; set => SetProperty(ref _isCompleted, value); }
        public bool IsError { get => _isError; set => SetProperty(ref _isError, value); }
    }

    public class ProgressViewModel
    {
        public ObservableCollection<GameProgressItem> Items { get; } = new();

        private readonly Dispatcher _dispatcher;
        private readonly LocalizationService _localization;

        public ProgressViewModel(LocalizationService localization)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _localization = localization;
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
                    Status = _localization.GetString("Progress_Starting") // "Iniciando…" / "Starting…"
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
                    item.Status = _localization.GetString("Label_Completed"); // "Completado" / "Completed"
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