using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using POPSManager.Commands;
using POPSManager.Logic.Cheats;
using POPSManager.Services;
using POPSManager.Settings;
using POPSManager.UI.Localization;
using POPSManager.UI.Windows;

namespace POPSManager.ViewModels
{
    public class CheatEditorViewModel : ViewModelBase
    {
        private readonly string _gameFolder;
        private readonly string _cd1Folder;
        private readonly string _cheatPath;
        private readonly CheatManagerService _manager;
        private readonly CheatSettingsService _settings;
        private readonly PathsService _paths;
        private readonly LocalizationService _loc;

        private ObservableCollection<CheatDefinition> _cheats = new();
        private CheatDefinition? _selectedCheat;
        private bool _isCheatEnabled;

        public CheatEditorViewModel(
            string gameFolder,
            CheatManagerService manager,
            CheatSettingsService settings,
            PathsService paths,
            LocalizationService loc)
        {
            _gameFolder = gameFolder;
            _manager = manager;
            _settings = settings;
            _paths = paths;
            _loc = loc;

            _cd1Folder = System.IO.Path.Combine(gameFolder, "CD1");
            _cheatPath = System.IO.Path.Combine(_cd1Folder, "CHEAT.TXT");

            TitleText = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                _loc.GetString("CheatEditor_Title"),
                System.IO.Path.GetFileName(gameFolder));

            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            AddCustomCheatCommand = new RelayCommand(AddCustomCheat);
            DeleteCustomCheatCommand = new RelayCommand(DeleteCustomCheat, () => SelectedCheat?.IsUserDefined == true);

            LoadCheats();
        }

        public string WindowTitle => _loc.GetString("CheatEditor_WindowTitle");
        public string TitleText { get; }
        public string EnableCheatText => _loc.GetString("CheatEditor_EnableCheat");
        public string AddCustomCheatText => _loc.GetString("CheatEditor_AddCustom");
        public string DeleteCheatText => _loc.GetString("CheatEditor_Delete");
        public string CancelButtonText => _loc.GetString("Button_Cancel");
        public string SaveButtonText => _loc.GetString("Button_Save");

        public ObservableCollection<CheatDefinition> Cheats
        {
            get => _cheats;
            set => SetProperty(ref _cheats, value);
        }

        public CheatDefinition? SelectedCheat
        {
            get => _selectedCheat;
            set
            {
                if (SetProperty(ref _selectedCheat, value))
                {
                    var currentCheats = _manager.LoadCheatFile(_cheatPath);
                    IsCheatEnabled = value != null && currentCheats.Contains(value.Code);
                }
            }
        }

        public bool IsCheatEnabled
        {
            get => _isCheatEnabled;
            set => SetProperty(ref _isCheatEnabled, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddCustomCheatCommand { get; }
        public ICommand DeleteCustomCheatCommand { get; }

        private void LoadCheats()
        {
            var official = CheatLibrary.GetOfficialCheats();
            var user = _manager.LoadUserCheats(_paths.RootFolder);
            Cheats.Clear();
            foreach (var cheat in official.Concat(user))
                Cheats.Add(cheat);
        }

        private bool CanSave() => true;

        private void Save()
        {
            var currentFile = _manager.LoadCheatFile(_cheatPath);
            var selectedCodes = Cheats
                .Where(c => currentFile.Contains(c.Code) || (c == SelectedCheat && IsCheatEnabled))
                .Select(c => c.Code)
                .Distinct()
                .ToList();

            _manager.SaveCheatFile(_cheatPath, selectedCodes);

            System.Windows.MessageBox.Show(
                _loc.GetString("CheatEditor_SaveSuccess"),
                "POPSManager",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            CloseWindow();
        }

        private void Cancel() => CloseWindow();

        private void AddCustomCheat()
        {
            string code = Microsoft.VisualBasic.Interaction.InputBox(
                _loc.GetString("CheatEditor_EnterCode"),
                _loc.GetString("CheatEditor_NewCustom"));

            if (string.IsNullOrWhiteSpace(code)) return;

            var cheat = new CheatDefinition
            {
                Code = code.Trim(),
                Name = code.Trim(),
                Description = _loc.GetString("CheatEditor_CustomDescription"),
                Category = _loc.GetString("CheatEditor_CustomCategory"),
                IsUserDefined = true
            };

            var userCheats = _manager.LoadUserCheats(_paths.RootFolder).ToList();
            userCheats.Add(cheat);
            _manager.SaveUserCheats(_paths.RootFolder, userCheats);
            LoadCheats();
        }

        private void DeleteCustomCheat()
        {
            if (SelectedCheat?.IsUserDefined != true) return;
            var userCheats = _manager.LoadUserCheats(_paths.RootFolder).ToList();
            userCheats.RemoveAll(c => c.Code == SelectedCheat.Code);
            _manager.SaveUserCheats(_paths.RootFolder, userCheats);
            LoadCheats();
        }

        private void CloseWindow()
        {
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window is CheatEditorWindow)
                {
                    window.Close();
                    break;
                }
            }
        }
    }
}