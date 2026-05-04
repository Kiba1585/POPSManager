using System;
using System.Collections.Generic;
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
        private string _tempFolderPath = string.Empty;
        private bool _useTempFolder;
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
            ChangeTempFolderCommand = new RelayCommand(async () => await ChangeTempFolderAsync());
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
            set
            {
                if (SetProperty(ref _dbUpdateStatus, value))
                    OnPropertyChanged(nameof(IsDbUpdateInProgress));
            }
        }

        public int DbUpdateProgress
        {
            get => _dbUpdateProgress;
            set => SetProperty(ref _dbUpdateProgress, value);
        }

        public bool IsDbUpdateInProgress =>
            !string.IsNullOrWhiteSpace(DbUpdateStatus)
            && !DbUpdateStatus.Contains("complet", StringComparison.OrdinalIgnoreCase)
            && !DbUpdateStatus.Contains("falló", StringComparison.OrdinalIgnoreCase);

        // ==================== PROPIEDADES EXISTENTES ====================
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
        public string TempFolderPath { get => _tempFolderPath; set => SetProperty(ref _tempFolderPath, value); }
        public bool UseTempFolder { get => _useTempFolder; set { if (SetProperty(ref _useTempFolder, value)) _ = SaveSettingsAsync(); } }

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
        public ICommand ChangeTempFolderCommand { get; }
        public ICommand SaveSettingsCommand { get; }

        public ICommand CheckDbUpdatesCommand { get; }
        public ICommand DownloadFullDbCommand { get; }
        public ICommand DownloadFilteredDbCommand { get; }

        // ==================== CARGA INICIAL ====================
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
            TempFolderPath = _settings.TempFolder ?? "";
            UseTempFolder = _settings.UseTempFolderForConversion;
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

            LastDbTag = _settings.LastDbTag;
            AutoCheckDbUpdates = _settings.AutoCheckDbUpdates;
        }

        // ==================== MÉTODOS DE SELECCIÓN DE CARPETAS ====================
        private async Task ChangeRootFolderAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta raíz (OPL)" };
            if (dialog.ShowDialog() != true) return;
            _settings.RootFolder = dialog.FolderName;
            RootPath = dialog.FolderName;
            await _paths.ReloadAsync();
            await SaveSettingsAsync();
            PopsPath = _paths.PopsFolder;
            AppsPath = _paths.AppsFolder;
        }

        private async Task ChangeSourceFolderAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta de origen" };
            if (dialog.ShowDialog() != true) return;
            SourcePath = dialog.FolderName;
            _settings.SourceFolder = dialog.FolderName;
            await SaveSettingsAsync();
        }

        private async Task ChangeDestinationFolderAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta de destino (raíz OPL)" };
            if (dialog.ShowDialog() != true) return;
            DestinationPath = dialog.FolderName;
            _settings.DestinationFolder = dialog.FolderName;
            _settings.RootFolder = dialog.FolderName;
            RootPath = dialog.FolderName;
            await _paths.ReloadAsync();
            await SaveSettingsAsync();
            PopsPath = _paths.PopsFolder;
            AppsPath = _paths.AppsFolder;
        }

        private async Task ChangePopsPathAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta POPS personalizada" };
            if (dialog.ShowDialog() != true) return;
            PopsPath = dialog.FolderName;
            await _paths.SetCustomPopsFolderAsync(dialog.FolderName);
            await SaveSettingsAsync();
        }

        private async Task ChangeAppsPathAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta APPS personalizada" };
            if (dialog.ShowDialog() != true) return;
            AppsPath = dialog.FolderName;
            await _paths.SetCustomAppsFolderAsync(dialog.FolderName);
            await SaveSettingsAsync();
        }

        private async Task ChangeLngPathAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta LNG" };
            if (dialog.ShowDialog() != true) return;
            LngPath = dialog.FolderName;
            await _paths.SetCustomLngFolderAsync(dialog.FolderName);
            await SaveSettingsAsync();
        }

        private async Task ChangeThmPathAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta THM" };
            if (dialog.ShowDialog() != true) return;
            ThmPath = dialog.FolderName;
            await _paths.SetCustomThmFolderAsync(dialog.FolderName);
            await SaveSettingsAsync();
        }

        private async Task ChangeElfFolderAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta de ELFs" };
            if (dialog.ShowDialog() != true) return;
            ElfFolderPath = dialog.FolderName;
            _settings.ElfFolder = dialog.FolderName;
            string popstarter = Path.Combine(dialog.FolderName, "POPSTARTER.ELF");
            string pops2 = Path.Combine(dialog.FolderName, "POPS2.ELF");
            if (File.Exists(popstarter)) await _paths.SetCustomElfPathAsync(popstarter);
            if (File.Exists(pops2)) await _paths.SetCustomPs2ElfPathAsync(pops2);
            await SaveSettingsAsync();
        }

        private async Task ChangeTempFolderAsync()
        {
            var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta temporal" };
            if (dialog.ShowDialog() != true) return;
            TempFolderPath = dialog.FolderName;
            _settings.TempFolder = dialog.FolderName;
            await SaveSettingsAsync();
        }

        // ==================== GUARDAR CONFIGURACIÓN ====================
        private async Task SaveSettingsAsync()
        {
            try
            {
                _settings.DarkMode = DarkMode;
                _settings.NotificationsEnabled = NotificationsEnabled;
                _settings.ProcessSubfolders = ProcessSubfolders;
                _settings.UseMetadata = UseMetadata;
                _settings.UseTitleInElfName = UseTitleInElfName;
                _settings.UseTempFolderForConversion = UseTempFolder;
                _settings.TempFolder = TempFolderPath;
                _settings.SourceFolder = SourcePath;
                _settings.DestinationFolder = DestinationPath;
                _settings.ElfFolder = ElfFolderPath;
                _settings.Automation.Mode = AutomationMode;
                _settings.Automation.Conversion = NormalizeNamesBehavior;
                _settings.Automation.MultiDisc = GroupMultiDiscBehavior;
                _settings.Automation.Covers = DownloadCoversBehavior;
                _settings.Automation.ElfGeneration = ElfGenerationBehavior;
                _settings.Automation.Metadata = MetadataBehavior;
                _settings.Automation.Lng = LngBehavior;
                _settings.Automation.Thm = ThmBehavior;
                _settings.Language = SelectedLanguage;

                _settings.LastDbTag = LastDbTag;
                _settings.AutoCheckDbUpdates = AutoCheckDbUpdates;

                await _settings.SaveAsync();
            }
            catch (Exception ex)
            {
                _services.LogService.Error($"[Settings] Error guardando: {ex.Message}");
            }
        }

        // ==================== MÉTODOS DE BASE DE DATOS ====================
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
                DbUpdateStatus = IsDbUpdateAvailable
                    ? $"¡Nueva versión disponible! ({latestTag})"
                    : "La base de datos está actualizada.";
            }
            catch (Exception ex)
            {
                DbUpdateStatus = $"Error al comprobar: {ex.Message}";
                IsDbUpdateAvailable = false;
            }
        }

        private async Task DownloadFullDbAsync() => await PerformDbDownload(useIndividual: false);
        private async Task DownloadFilteredDbAsync() => await PerformDbDownload(useIndividual: true);

        private async Task PerformDbDownload(bool useIndividual)
        {
            DbUpdateProgress = 0;
            DbUpdateStatus = useIndividual
                ? "Descargando metadatos de tus juegos..."
                : "Descargando base de datos completa...";

            try
            {
                var updater = _services.DatabaseUpdater;
                updater.ProgressChanged += OnDbProgressChanged;

                if (useIndividual)
                {
                    var gameIds = DiscoverCurrentGameIds();
                    await updater.DownloadAndExtractFilteredAsync(gameIds, _paths.CfgFolder, _settings);
                }
                else
                {
                    await updater.DownloadAndExtractFullAsync(_paths.CfgFolder, _settings);
                }

                var newTag = await updater.GetLatestReleaseTagAsync();
                if (newTag != null)
                {
                    _settings.LastDbTag = newTag;
                    LastDbTag = newTag;
