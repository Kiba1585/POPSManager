using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace POPSManager.UI.Progress
{
    public class GameProgressItem : INotifyPropertyChanged
    {
        private int _progress;
        private string _status = "";
        private bool _isCompleted;
        private bool _isError;

        public string Title { get; set; } = "";
        public string GameId { get; set; } = "";

        public int Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set { _isCompleted = value; OnPropertyChanged(); }
        }

        public bool IsError
        {
            get => _isError;
            set { _isError = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
