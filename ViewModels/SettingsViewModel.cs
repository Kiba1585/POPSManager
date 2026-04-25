using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using POPSManager.Commands;
using POPSManager.Logic.Automation;
using POPSManager.Services;

namespace POPSManager.ViewModels
{
    public class LanguageItem
    {
        public AppLanguage Value { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public class SettingsViewModel : ViewModelBase
    {
        private readonly AppServices _services;
        private readonly PathsService _paths;
        private readonly SettingsService _settings;

        private string _rootPath = string.Empty;
        private string _sourcePath = string.Empty;
        private string _destinationPath = string.Empty;
        private string _popsPath = string.Empty;
        private string _appsPath = string.Empty;
        private string _lngPath = string.Empty;
        private string _thmPath = string.Empty;
        private string _elfFolderPath = string.Empty;
        private bool _darkMode;
        private bool _notificationsEnabled;
        private bool _processSubfolders = true;
        private AutomationMode _automationMode;
        private AutoBehavior _normalizeNamesBehavior;
        private AutoBehavior _groupMultiDiscBehavior;
        private AutoBehavior _downloadCoversBehavior;
        private AutoBehavior _lngBehavior;
        private AutoBehavior _thmBehavior;
        private AppLanguage _selectedLanguage;

        public SettingsViewModel()
        {
            _services = App.Services!;
            _paths = _services.Paths;
            _settings = _services.Settings;

            Languages = new ObservableCollection<LanguageItem>
            {
                new LanguageItem { Value = AppLanguage.Auto, DisplayName = "Automático" },
                new LanguageItem { Value = AppLanguage.Spanish, DisplayName = "Español" },
                new LanguageItem { Value = AppLanguage.English, DisplayName = "English" },
                new LanguageItem { Value = AppLanguage.French, DisplayName = "Français" },
                new LanguageItem { Value = AppLanguage.German, DisplayName = "Deutsch" },
                new LanguageItem { Value = AppLanguage.Italian, DisplayName = "Italiano" },
                new LanguageItem { Value = AppLanguage.Portuguese, DisplayName = "Português" },
                new LanguageItem { Value = AppLanguage.Japanese, DisplayName = "日本語" }
            };

            ChangeRootFolderCommand = new RelayCommand(async () => await ChangeRootFolderAsync());
            ChangeSourceFolderCommand = new RelayCommand(async () => await ChangeSourceFolderAsync());
            ChangeDestinationFolderCommand = new RelayCommand(async () => await ChangeDestinationFolderAsync());
            ChangePopsPathCommand = new RelayCommand(async () => await ChangePopsPathAsync());
            ChangeAppsPathCommand = new RelayCommand(async () => await ChangeAppsPathAsync());
            ChangeLngPathCommand = new RelayCommand(async () => await ChangeLngPathAsync());
            ChangeThmPathCommand = new RelayCommand(async () => await ChangeThmPathAsync());
            ChangeElfFolderCommand = new RelayCommand(async () => await ChangeElfFolderAsync());
            OpenProgramFolderCommand = new RelayCommand(OpenProgramFolder);
            SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync());

            LoadSettings();
        }

        public ObservableCollection<LanguageItem> Languages { get; }

        public AppLanguage SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value))
                {
                    _settings.Language = value;
                    _services.Localization?.Refresh();
                    _ = SaveSettingsAsync();
                }
            }
        }

        public string RootPath { get => _rootPath; set => SetProperty(ref _rootPath, value); }
        public string SourcePath { get => _sourcePath; set => SetProperty(ref _sourcePath, value); }
        public string DestinationPath { get => _destinationPath; set => SetProperty(ref _destinationPath, value); }
        public string PopsPath { get => _popsPath; set => SetProperty(ref _popsPath, value); }
        public string AppsPath { get => _appsPath; set => SetProperty(ref _appsPath, value); }
        public string LngPath { get => _lngPath; set => SetProperty(ref _lngPath, value); }
        public string ThmPath { get => _thmPath; set => SetProperty(ref _thmPath, value); }
        public string ElfFolderPath { get => _elfFolderPath; set => SetProperty(ref _elfFolderPath, value); }

        public bool DarkMode
        {
            get => _darkMode;
            set { if (SetProperty(ref _darkMode, value)) _ = SaveSettingsAsync(); }
        }

        public bool NotificationsEnabled
        {
            get => _notificationsEnabled;
            set { if (SetProperty(ref _notificationsEnabled, value)) _ = SaveSettingsAsync(); }
        }

        public bool ProcessSubfolders
        {
            get => _processSubfolders;
            set { if (SetProperty(ref _processSubfolders, value)) _ = SaveSettingsAsync(); }
        }

        public AutomationMode AutomationMode
        {
            get => _automationMode;
            set { if (SetProperty(ref _automationMode, value)) _ = SaveSettingsAsync(); }
        }

        public AutoBehavior NormalizeNamesBehavior
        {
            get => _normalizeNamesBehavior;
            set { if (SetProperty(ref _normalizeNamesBehavior, value)) _ = SaveSettingsAsync(); }
        }
        public AutoBehavior GroupMultiDiscBehavior
        {
            get => _groupMultiDiscBehavior;
            set { if (SetProperty(ref _groupMultiDiscBehavior, value)) _ = SaveSettingsAsync(); }
        }
        public AutoBehavior DownloadCoversBehavior
        {
            get => _downloadCoversBehavior;
            set { if (SetProperty(ref _downloadCoversBehavior, value)) _ = SaveSettingsAsync(); }
        }
        public AutoBehavior LngBehavior
        {
            get => _lngBehavior;
            set { if (SetProperty(ref _lngBehavior, value)) _ = SaveSettingsAsync(); }
        }
        public AutoBehavior ThmBehavior
        {
            get => _thmBehavior;
            set { if (SetProperty(ref _thmBehavior, value)) _ = SaveSettingsAsync(); }
        }

        public ICommand ChangeRootFolderCommand { get; }
        public ICommand ChangeSourceFolderCommand { get; }
        public ICommand ChangeDestinationFolderCommand { get; }
        public ICommand ChangePopsPathCommand { get; }
        public ICommand ChangeAppsPathCommand { get; }
        public ICommand ChangeLngPathCommand { get; }
        public ICommand ChangeThmPathCommand { get; }
        public ICommand ChangeElfFolderCommand { get; }
        public ICommand OpenProgramFolderCommand { get; }
        public ICommand SaveSettingsCommand { get; }

        private void LoadSettings()
        {
            RootPath = _paths.RootFolder;
            SourcePath = _settings.SourceFolder ?? "";
            DestinationPath = _settings.DestinationFolder ?? "";
            PopsPath = _paths.PopsFolder;
            AppsPath = _paths.AppsFolder;
            LngPath = _paths.LngFolder;
            ThmPath = _paths.ThmFolder;
            ElfFolderPath = _settings.ElfFolder ?? "";
            DarkMode = _settings.DarkMode;
            NotificationsEnabled = _settings.NotificationsEnabled;
            ProcessSubfolders = _settings.ProcessSubfolders;
            AutomationMode = _settings.Automation.Mode;
            NormalizeNamesBehavior = _settings.Automation.Conversion;
            GroupMultiDiscBehavior = _settings.Automation.MultiDisc;
            DownloadCoversBehavior = _settings.Automation.Covers;
            LngBehavior = _settings.Automation.Lng;
            ThmBehavior = _settings.Automation.Thm;
            SelectedLanguage = _settings.Language;
        }

        private async Task ChangeRootFolderAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta raíz del dispositivo OPL" };
            if (dialog.ShowDialog() != true) return;

            string path = dialog.FolderName;
            if (!Directory.Exists(path))
            {
                _services.Notifications.Error("La carpeta seleccionada no existe.");
                return;
            }

            if (IsInvalidRoot(path))
            {
                _services.Notifications.Warning("Has seleccionado una carpeta incorrecta (PS2/PS1). Se usará su carpeta padre.");
                path = Directory.GetParent(path)?.FullName ?? path;
            }

            try
            {
                _settings.RootFolder = path;
                await _settings.SaveAsync();
                await _paths.ReloadAsync();
                LoadSettings();
                _services.Notifications.Success("Carpeta raíz actualizada.");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error("No se pudo actualizar la carpeta raíz.");
                _services.LogService.Error($"[Settings] Error ChangeRootFolder: {ex.Message}");
            }
        }

        private async Task ChangeSourceFolderAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta de origen de juegos" };
            if (dialog.ShowDialog() != true) return;

            if (!Directory.Exists(dialog.FolderName))
            {
                _services.Notifications.Error("La carpeta seleccionada no existe.");
                return;
            }

            _settings.SourceFolder = dialog.FolderName;
            await _settings.SaveAsync();
            LoadSettings();
            _services.Notifications.Success("Carpeta de origen actualizada.");
        }

        private async Task ChangeDestinationFolderAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta destino (raíz OPL)" };
            if (dialog.ShowDialog() != true) return;

            if (!Directory.Exists(dialog.FolderName))
            {
                _services.Notifications.Error("La carpeta seleccionada no existe.");
                return;
            }

            _settings.DestinationFolder = dialog.FolderName;
            _settings.RootFolder = dialog.FolderName;
            await _settings.SaveAsync();
            await _paths.ReloadAsync();
            LoadSettings();
            _services.Notifications.Success("Carpeta destino actualizada.");
        }

        private async Task ChangePopsPathAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta POPS" };
            if (dialog.ShowDialog() != true) return;

            string path = dialog.FolderName;
            if (!Directory.Exists(path))
            {
                _services.Notifications.Error("La carpeta seleccionada no existe.");
                return;
            }

            try
            {
                await _paths.SetCustomPopsFolderAsync(path);
                LoadSettings();
                _services.Notifications.Success("Ruta POPS actualizada.");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error("No se pudo actualizar la ruta POPS.");
                _services.LogService.Error($"[Settings] Error ChangePopsPath: {ex.Message}");
            }
        }

        private async Task ChangeAppsPathAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta APPS" };
            if (dialog.ShowDialog() != true) return;

            string path = dialog.FolderName;
            if (!Directory.Exists(path))
            {
                _services.Notifications.Error("La carpeta seleccionada no existe.");
                return;
            }

            try
            {
                await _paths.SetCustomAppsFolderAsync(path);
                LoadSettings();
                _services.Notifications.Success("Ruta APPS actualizada.");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error("No se pudo actualizar la ruta APPS.");
                _services.LogService.Error($"[Settings] Error ChangeAppsPath: {ex.Message}");
            }
        }

        private async Task ChangeLngPathAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta de idiomas (LNG)" };
            if (dialog.ShowDialog() != true) return;

            string path = dialog.FolderName;
            if (!Directory.Exists(path))
            {
                _services.Notifications.Error("La carpeta seleccionada no existe.");
                return;
            }

            try
            {
                await _paths.SetCustomLngFolderAsync(path);
                LoadSettings();
                _services.Notifications.Success("Ruta LNG actualizada.");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error("No se pudo actualizar la ruta LNG.");
                _services.LogService.Error($"[Settings] Error ChangeLngPath: {ex.Message}");
            }
        }

        private async Task ChangeThmPathAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta de temas (THM)" };
            if (dialog.ShowDialog() != true) return;

            string path = dialog.FolderName;
            if (!Directory.Exists(path))
            {
                _services.Notifications.Error("La carpeta seleccionada no existe.");
                return;
            }

            try
            {
                await _paths.SetCustomThmFolderAsync(path);
                LoadSettings();
                _services.Notifications.Success("Ruta THM actualizada.");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error("No se pudo actualizar la ruta THM.");
                _services.LogService.Error($"[Settings] Error ChangeThmPath: {ex.Message}");
            }
        }

        private async Task ChangeElfFolderAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta con archivos ELF" };
            if (dialog.ShowDialog() != true) return;

            if (!Directory.Exists(dialog.FolderName))
            {
                _services.Notifications.Error("La carpeta seleccionada no existe.");
                return;
            }

            _settings.ElfFolder = dialog.FolderName;
            string popstarter = Path.Combine(dialog.FolderName, "POPSTARTER.ELF");
            string pops2 = Path.Combine(dialog.FolderName, "POPS2.ELF");
            if (File.Exists(popstarter)) await _paths.SetCustomElfPathAsync(popstarter);
            if (File.Exists(pops2)) await _paths.SetCustomPs2ElfPathAsync(pops2);
            await _settings.SaveAsync();
            LoadSettings();
            _services.Notifications.Success("Carpeta de ELFs actualizada.");
        }

        private void OpenProgramFolder()
        {
            try
            {
                string folder = AppContext.BaseDirectory;
                System.Diagnostics.Process.Start("explorer.exe", folder);
            }
            catch (Exception ex)
            {
                _services.Notifications.Error("No se pudo abrir la carpeta del programa.");
                _services.LogService.Error($"[Settings] Error OpenProgramFolder: {ex.Message}");
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                _settings.DarkMode = DarkMode;
                _settings.NotificationsEnabled = NotificationsEnabled;
                _settings.ProcessSubfolders = ProcessSubfolders;
                _settings.Automation.Mode = AutomationMode;
                _settings.Automation.Conversion = NormalizeNamesBehavior;
                _settings.Automation.MultiDisc = GroupMultiDiscBehavior;
                _settings.Automation.Covers = DownloadCoversBehavior;
                _settings.Automation.Lng = LngBehavior;
                _settings.Automation.Thm = ThmBehavior;
                _settings.Language = SelectedLanguage;

                await _settings.SaveAsync();
            }
            catch (Exception ex)
            {
                _services.LogService.Error($"[Settings] Error guardando: {ex.Message}");
            }
        }

        private static bool IsInvalidRoot(string path)
        {
            string folder = Path.GetFileName(path).ToUpperInvariant();
            return folder == "PS2" || folder == "PS1" || folder == "POPSMANAGER";
        }
    }
}