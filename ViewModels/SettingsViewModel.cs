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

        // ==================== NUEVAS PROPIEDADES DB ====================
        private string _lastDbTag = "";
        private bool _autoCheckDbUpdates = true;
        private bool _isDbUpdateAvailable;
        private string _dbUpdateStatus = "";
        private int _dbUpdateProgress;

        // ==================== PROPIEDADES EXISTENTES ====================
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
        private bool _useMetadata = true;
        private bool _useTitleInElfName = true;
        private AutomationMode _automationMode;
        private AutoBehavior _normalizeNamesBehavior;
        private AutoBehavior _groupMultiDiscBehavior;
        private AutoBehavior _downloadCoversBehavior;
        private AutoBehavior _elfGenerationBehavior;
        private AutoBehavior _metadataBehavior;
        private AutoBehavior _lngBehavior;
        private AutoBehavior _thmBehavior;
        private AppLanguage _selectedLanguage;

        // ==================== CONSTRUCTOR ====================
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

            // --- Comandos existentes ---
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

            // --- NUEVOS COMANDOS PARA LA BASE DE DATOS ---
            CheckDbUpdatesCommand = new RelayCommand(async () => await CheckDbUpdatesAsync());
            DownloadFullDbCommand = new RelayCommand(async () => await DownloadFullDbAsync(), () => !IsDbUpdateInProgress);
            DownloadFilteredDbCommand = new RelayCommand(async () => await DownloadFilteredDbAsync(), () => !IsDbUpdateInProgress);

            LoadSettings();
        }

        // ==================== NUEVAS PROPIEDADES DB ====================
        public string LastDbTag
        {
            get => _lastDbTag;
            set => SetProperty(ref _lastDbTag, value);
        }

        public bool AutoCheckDbUpdates
        {
            get => _autoCheckDbUpdates;
            set
            {
                if (SetProperty(ref _autoCheckDbUpdates, value))
                    _ = SaveSettingsAsync();
            }
        }

        public bool IsDbUpdateAvailable
        {
            get => _isDbUpdateAvailable;
            set => SetProperty(ref _isDbUpdateAvailable, value);
        }

        public string DbUpdateStatus
        {
            get => _dbUpdateStatus;
            set => SetProperty(ref _dbUpdateStatus, value);
        }

        public int DbUpdateProgress
        {
            get => _dbUpdateProgress;
            set => SetProperty(ref _dbUpdateProgress, value);
        }

        private bool IsDbUpdateInProgress =>
            !string.IsNullOrWhiteSpace(DbUpdateStatus) && !DbUpdateStatus.Contains("complet") && !DbUpdateStatus.Contains("falló");

        // ==================== PROPIEDADES EXISTENTES ====================
        public ObservableCollection<LanguageItem> Languages { get; }

        public AppLanguage SelectedLanguage
        {
            get => _selectedLanguage;
            set { if (SetProperty(ref _selectedLanguage, value)) { _settings.Language = value; _services.Localization?.Refresh(); _ = SaveSettingsAsync(); } }
        }

        public string RootPath { get => _rootPath; set => SetProperty(ref _rootPath, value); }
        public string SourcePath { get => _sourcePath; set => SetProperty(ref _sourcePath, value); }
        public string DestinationPath { get => _destinationPath; set => SetProperty(ref _destinationPath, value); }
        public string PopsPath { get => _popsPath; set => SetProperty(ref _popsPath, value); }
        public string AppsPath { get => _appsPath; set => SetProperty(ref _appsPath, value); }
        public string LngPath { get => _lngPath; set => SetProperty(ref _lngPath, value); }
        public string ThmPath { get => _thmPath; set => SetProperty(ref _thmPath, value); }
        public string ElfFolderPath { get => _elfFolderPath; set => SetProperty(ref _elfFolderPath, value); }

        public bool DarkMode { get => _darkMode; set { if (SetProperty(ref _darkMode, value)) _ = SaveSettingsAsync(); } }
        public bool NotificationsEnabled { get => _notificationsEnabled; set { if (SetProperty(ref _notificationsEnabled, value)) _ = SaveSettingsAsync(); } }
        public bool ProcessSubfolders { get => _processSubfolders; set { if (SetProperty(ref _processSubfolders, value)) _ = SaveSettingsAsync(); } }
        public bool UseMetadata { get => _useMetadata; set { if (SetProperty(ref _useMetadata, value)) _ = SaveSettingsAsync(); } }
        public bool UseTitleInElfName { get => _useTitleInElfName; set { if (SetProperty(ref _useTitleInElfName, value)) _ = SaveSettingsAsync(); } }

        public AutomationMode AutomationMode { get => _automationMode; set { if (SetProperty(ref _automationMode, value)) _ = SaveSettingsAsync(); } }
        public AutoBehavior NormalizeNamesBehavior { get => _normalizeNamesBehavior; set { if (SetProperty(ref _normalizeNamesBehavior, value)) _ = SaveSettingsAsync(); } }
        public AutoBehavior GroupMultiDiscBehavior { get => _groupMultiDiscBehavior; set { if (SetProperty(ref _groupMultiDiscBehavior, value)) _ = SaveSettingsAsync(); } }
        public AutoBehavior DownloadCoversBehavior { get => _downloadCoversBehavior; set { if (SetProperty(ref _downloadCoversBehavior, value)) _ = SaveSettingsAsync(); } }
        public AutoBehavior ElfGenerationBehavior { get => _elfGenerationBehavior; set { if (SetProperty(ref _elfGenerationBehavior, value)) _ = SaveSettingsAsync(); } }
        public AutoBehavior MetadataBehavior { get => _metadataBehavior; set { if (SetProperty(ref _metadataBehavior, value)) _ = SaveSettingsAsync(); } }
        public AutoBehavior LngBehavior { get => _lngBehavior; set { if (SetProperty(ref _lngBehavior, value)) _ = SaveSettingsAsync(); } }
        public AutoBehavior ThmBehavior { get => _thmBehavior; set { if (SetProperty(ref _thmBehavior, value)) _ = SaveSettingsAsync(); } }

        // ==================== COMANDOS ====================
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

        // NUEVOS COMANDOS
        public ICommand CheckDbUpdatesCommand { get; }
        public ICommand DownloadFullDbCommand { get; }
        public ICommand DownloadFilteredDbCommand { get; }

        // ==================== CARGA INICIAL ====================
        private void LoadSettings()
        {
            // ... (existente sin cambios)
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
            UseMetadata = _settings.UseMetadata;
            UseTitleInElfName = _settings.UseTitleInElfName;
            AutomationMode = _settings.Automation.Mode;
            NormalizeNamesBehavior = _settings.Automation.Conversion;
            GroupMultiDiscBehavior = _settings.Automation.MultiDisc;
            DownloadCoversBehavior = _settings.Automation.Covers;
            ElfGenerationBehavior = _settings.Automation.ElfGeneration;
            MetadataBehavior = _settings.Automation.Metadata;
            LngBehavior = _settings.Automation.Lng;
            ThmBehavior = _settings.Automation.Thm;
            SelectedLanguage = _settings.Language;

            // NUEVOS
            LastDbTag = _settings.LastDbTag;
            AutoCheckDbUpdates = _settings.AutoCheckDbUpdates;
        }

        // ==================== MÉTODOS EXISTENTES (SIN CAMBIOS) ====================
        private async Task ChangeRootFolderAsync() { /* ... igual que antes ... */ }
        private async Task ChangeSourceFolderAsync() { /* ... */ }
        private async Task ChangeDestinationFolderAsync() { /* ... */ }
        private async Task ChangePopsPathAsync() { /* ... */ }
        private async Task ChangeAppsPathAsync() { /* ... */ }
        private async Task ChangeLngPathAsync() { /* ... */ }
        private async Task ChangeThmPathAsync() { /* ... */ }
        private async Task ChangeElfFolderAsync() { /* ... */ }
        private void OpenProgramFolder() { /* ... */ }
        private async Task SaveSettingsAsync()
        {
            try
            {
                _settings.DarkMode = DarkMode;
                _settings.NotificationsEnabled = NotificationsEnabled;
                _settings.ProcessSubfolders = ProcessSubfolders;
                _settings.UseMetadata = UseMetadata;
                _settings.UseTitleInElfName = UseTitleInElfName;
                _settings.Automation.Mode = AutomationMode;
                _settings.Automation.Conversion = NormalizeNamesBehavior;
                _settings.Automation.MultiDisc = GroupMultiDiscBehavior;
                _settings.Automation.Covers = DownloadCoversBehavior;
                _settings.Automation.ElfGeneration = ElfGenerationBehavior;
                _settings.Automation.Metadata = MetadataBehavior;
                _settings.Automation.Lng = LngBehavior;
                _settings.Automation.Thm = ThmBehavior;
                _settings.Language = SelectedLanguage;

                // NUEVOS
                _settings.LastDbTag = LastDbTag;
                _settings.AutoCheckDbUpdates = AutoCheckDbUpdates;

                await _settings.SaveAsync();
            }
            catch (Exception ex)
            {
                _services.LogService.Error($"[Settings] Error guardando: {ex.Message}");
            }
        }
        private static bool IsInvalidRoot(string path) { /* ... */ }

        // ==================== NUEVOS MÉTODOS PARA DB ====================
        private async Task CheckDbUpdatesAsync()
        {
            DbUpdateStatus = "Consultando última versión...";
            try
            {
                var updater = _services.DatabaseUpdater;
                string? latestTag = await updater.GetLatestReleaseTagAsync();
                if (latestTag == null)
                {
                    DbUpdateStatus = "No se pudo obtener información de la versión.";
                    IsDbUpdateAvailable = false;
                    return;
                }

                IsDbUpdateAvailable = latestTag != _settings.LastDbTag;
                if (IsDbUpdateAvailable)
                {
                    DbUpdateStatus = $"¡Nueva versión disponible! ({latestTag})";
                }
                else
                {
                    DbUpdateStatus = "La base de datos está actualizada.";
                }
            }
            catch (Exception ex)
            {
                DbUpdateStatus = $"Error al comprobar: {ex.Message}";
                IsDbUpdateAvailable = false;
            }
        }

        private async Task DownloadFullDbAsync()
        {
            await PerformDbDownload(useIndividual: false);
        }

        private async Task DownloadFilteredDbAsync()
        {
            await PerformDbDownload(useIndividual: true);
        }

        private async Task PerformDbDownload(bool useIndividual)
        {
            DbUpdateProgress = 0;
            DbUpdateStatus = useIndividual ? "Descargando metadatos de tus juegos..." : "Descargando base de datos completa...";

            try
            {
                var updater = _services.DatabaseUpdater;
                updater.ProgressChanged += (pct, msg) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DbUpdateProgress = pct;
                        DbUpdateStatus = msg;
                    });
                };

                if (useIndividual)
                {
                    // Obtener lista de GameIDs presentes en las carpetas configuradas
                    var gameIds = DiscoverCurrentGameIds();
                    await updater.DownloadAndExtractFilteredAsync(gameIds, _paths.CfgFolder, _settings);
                }
                else
                {
                    await updater.DownloadAndExtractFullAsync(_paths.CfgFolder, _settings);
                }

                // Guardar el tag de la versión descargada
                var newTag = await updater.GetLatestReleaseTagAsync();
                if (newTag != null)
                {
                    _settings.LastDbTag = newTag;
                    LastDbTag = newTag;
                    await _settings.SaveAsync();
                }

                DbUpdateStatus = "Actualización completada correctamente.";
                IsDbUpdateAvailable = false;
            }
            catch (Exception ex)
            {
                DbUpdateStatus = $"Error al descargar: {ex.Message}";
            }
        }

        private System.Collections.Generic.List<string> DiscoverCurrentGameIds()
        {
            var ids = new System.Collections.Generic.List<string>();

            // Buscar en POPS (PS1)
            if (Directory.Exists(_paths.PopsFolder))
            {
                foreach (var dir in Directory.GetDirectories(_paths.PopsFolder))
                {
                    // Intentar obtener el ID desde el nombre o desde los VCD
                    string? id = POPSManager.Logic.GameIdDetector.DetectFromName(Path.GetFileName(dir));
                    if (!string.IsNullOrWhiteSpace(id) && !ids.Contains(id))
                        ids.Add(id);
                }
            }

            // Buscar en DVD (PS2)
            if (Directory.Exists(_paths.DvdFolder))
            {
                foreach (var file in Directory.GetFiles(_paths.DvdFolder, "*.ISO"))
                {
                    string? id = POPSManager.Logic.GameIdDetector.DetectGameId(file);
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        // Normalizar formato PS2: XXXX_YYYYY
                        id = id.Replace("-", "_").Replace(" ", "_").Replace(".", "_");
                        if (!ids.Contains(id))
                            ids.Add(id);
                    }
                    else
                    {
                        // Intentar desde el nombre del archivo
                        id = POPSManager.Logic.GameIdDetector.DetectFromName(Path.GetFileNameWithoutExtension(file));
                        if (!string.IsNullOrWhiteSpace(id) && !ids.Contains(id))
                            ids.Add(id);
                    }
                }
            }

            return ids;
        }
    }
}