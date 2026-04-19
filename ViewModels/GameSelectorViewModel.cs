using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using POPSManager.Commands;
using POPSManager.Services;
using POPSManager.UI.Localization;

namespace POPSManager.ViewModels
{
    public class GameItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
    }

    public class GameSelectorViewModel : ViewModelBase
    {
        private readonly PathsService _paths;
        private readonly LocalizationService _loc;

        private ObservableCollection<GameItem> _games = new();
        private GameItem? _selectedGame;

        public GameSelectorViewModel(PathsService paths, LocalizationService loc)
        {
            _paths = paths;
            _loc = loc;

            CancelCommand = new RelayCommand(Cancel);
            EditCommand = new RelayCommand(Edit, () => SelectedGame != null);

            LoadGames();
        }

        public string WindowTitle => _loc.GetString("GameSelector_Title");
        public string TitleText => _loc.GetString("GameSelector_SelectGame");
        public string CancelButtonText => _loc.GetString("Button_Cancel");
        public string EditButtonText => _loc.GetString("GameSelector_EditCheats");

        public ObservableCollection<GameItem> Games
        {
            get => _games;
            set => SetProperty(ref _games, value);
        }

        public GameItem? SelectedGame
        {
            get => _selectedGame;
            set => SetProperty(ref _selectedGame, value);
        }

        public ICommand CancelCommand { get; }
        public ICommand EditCommand { get; }

        public event EventHandler<string>? GameSelected;

        private void LoadGames()
        {
            try
            {
                if (!Directory.Exists(_paths.PopsFolder))
                    return;

                var folders = Directory.GetDirectories(_paths.PopsFolder)
                    .Where(f => Directory.GetDirectories(f)
                        .Any(d => Path.GetFileName(d).StartsWith("CD1", StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(f => f)
                    .Select(f => new GameItem { Name = Path.GetFileName(f), FullPath = f })
                    .ToList();

                Games.Clear();
                foreach (var f in folders)
                    Games.Add(f);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "POPSManager", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            GameSelected?.Invoke(this, null!);
            CloseWindow();
        }

        private void Edit()
        {
            if (SelectedGame != null)
            {
                GameSelected?.Invoke(this, SelectedGame.FullPath);
                CloseWindow();
            }
        }

        private void CloseWindow()
        {
            Application.Current.Windows.OfType<GameSelectorWindow>().FirstOrDefault()?.Close();
        }
    }
}