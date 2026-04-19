using System;
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
    public class SettingsViewModel : ViewModelBase
    {
        private readonly AppServices _services;
        private readonly PathsService _paths;

        private string _rootPath;
        private string _popsPath;
        private string _appsPath;
        private string _elfPath;
        private bool _darkMode;
        private bool _notificationsEnabled;
        private AutomationMode _automationMode;
        private AutoBehavior _normalizeNamesBehavior;
        private AutoBehavior _groupMultiDiscBehavior;
        private AutoBehavior _downloadCoversBehavior;

        public SettingsViewModel()
        {
            _services = App.Services!;
            _paths = _services.Paths;

            ChangeRootFolderCommand = new RelayCommand(async () => await ChangeRootFolderAsync());
            ChangePopsPathCommand = new RelayCommand(async () => await ChangePopsPathAsync());
            ChangeAppsPathCommand = new RelayCommand(async () => await ChangeAppsPathAsync());
            SelectElfCommand = new RelayCommand(async () => await SelectElfAsync());
            OpenProgramFolderCommand = new RelayCommand(OpenProgramFolder);
            SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync());

            LoadSettings();
        }

        #region Propiedades

        public string RootPath
        {
            get => _rootPath;
            set { _rootPath = value; OnPropertyChanged(); }
        }

        public string PopsPath
        {
            get => _popsPath;
            set { _popsPath = value; OnPropertyChanged(); }
        }

        public string AppsPath
        {
            get => _appsPath;
            set { _appsPath = value; OnPropertyChanged(); }
        }

        public string ElfPath
        {
            get => _elfPath;
            set { _elfPath = value; OnPropertyChanged(); }
        }

        public bool DarkMode
        {
            get => _darkMode;
            set { _darkMode = value; OnPropertyChanged(); SaveSettingsCommand.Execute(null); }
        }

        public bool NotificationsEnabled
        {
            get => _notificationsEnabled;
            set { _notificationsEnabled = value; OnPropertyChanged(); SaveSettingsCommand.Execute(null); }
        }

        public AutomationMode AutomationMode
        {
            get => _automationMode;
            set { _automationMode = value; OnPropertyChanged(); SaveSettingsCommand.Execute(null); }
        }

        public AutoBehavior NormalizeNamesBehavior
        {
            get => _normalizeNamesBehavior;
            set { _normalizeNamesBehavior = value; OnPropertyChanged(); SaveSettingsCommand.Execute(null); }
        }

        public AutoBehavior GroupMultiDiscBehavior
        {
            get => _groupMultiDiscBehavior;
            set { _groupMultiDiscBehavior = value; OnPropertyChanged(); SaveSettingsCommand.Execute(null); }
        }

        public AutoBehavior DownloadCoversBehavior
        {
            get => _downloadCoversBehavior;
            set { _downloadCoversBehavior = value; OnPropertyChanged(); SaveSettingsCommand.Execute(null); }
        }

        #endregion

        #region Comandos

        public ICommand ChangeRootFolderCommand { get; }
        public ICommand ChangePopsPathCommand { get; }
        public ICommand ChangeAppsPathCommand { get; }
        public ICommand SelectElfCommand { get; }
        public ICommand OpenProgramFolderCommand { get; }
        public ICommand SaveSettingsCommand { get; }

        #endregion

        private void LoadSettings()
        {
            RootPath = _paths.RootFolder;
            PopsPath = _paths.PopsFolder;
            AppsPath = _paths.AppsFolder;
            ElfPath = _paths.PopstarterElfPath;
            DarkMode = _services.Settings.DarkMode;
            NotificationsEnabled = _services.Settings.NotificationsEnabled;
            AutomationMode = _services.Settings.Automation.Mode;
            NormalizeNamesBehavior = _services.Settings.Automation.Conversion;
            GroupMultiDiscBehavior = _services.Settings.Automation.MultiDisc;
            DownloadCoversBehavior = _services.Settings.Automation.Covers;
        }

        private async Task ChangeRootFolderAsync()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Seleccionar carpeta raíz del dispositivo OPL",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

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
                _services.Settings.RootFolder = path;
                await _services.Settings.SaveAsync();
                await _paths.ReloadAsync();
                LoadSettings();
                _services.Notifications.Success("Carpeta raíz actualizada correctamente.");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error("No se pudo actualizar la carpeta raíz.");
                _services.LogService.Error($"[SettingsViewModel] Error ChangeRootFolder: {ex.Message}");
            }
        }

        private async Task ChangePopsPathAsync()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Seleccionar carpeta POPS",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

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
                await _paths.SetCustomPopsFolderAsync(path);
                LoadSettings();
                _services.Notifications.Success("Ruta POPS actualizada correctamente.");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error("No se pudo actualizar la ruta POPS.");
                _services.LogService.Error($"[SettingsViewModel] Error ChangePopsPath: {ex.Message}");
            }
        }

        private async Task ChangeAppsPathAsync()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Seleccionar carpeta APPS",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

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
                await _paths.SetCustomAppsFolderAsync(path);
                LoadSettings();
                _services.Notifications.Success("Ruta APPS actualizada correctamente.");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error("No se pudo actualizar la ruta APPS.");
                _services.LogService.Error($"[SettingsViewModel] Error ChangeAppsPath: {ex.Message}");
            }
        }

        private async Task SelectElfAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "POPStarter ELF|POPSTARTER.ELF|Todos los archivos|*.*",
                Title = "Seleccionar POPSTARTER.ELF"
            };

            if (dialog.ShowDialog() != true)
                return;

            string path = dialog.FileName;

            if (!File.Exists(path))
            {
                _services.Notifications.Error("El archivo seleccionado no existe.");
                return;
            }

            try
            {
                await _paths.SetCustomElfPathAsync(path);
                LoadSettings();
                _services.Notifications.Success("POPSTARTER.ELF configurado correctamente.");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error("No se pudo configurar POPSTARTER.ELF.");
                _services.LogService.Error($"[SettingsViewModel] Error SelectElf: {ex.Message}");
            }
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
                _services.LogService.Error($"[SettingsViewModel] Error OpenProgramFolder: {ex.Message}");
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                _services.Settings.DarkMode = DarkMode;
                _services.Settings.NotificationsEnabled = NotificationsEnabled;
                _services.Settings.Automation.Mode = AutomationMode;
                _services.Settings.Automation.Conversion = NormalizeNamesBehavior;
                _services.Settings.Automation.MultiDisc = GroupMultiDiscBehavior;
                _services.Settings.Automation.Covers = DownloadCoversBehavior;

                await _services.Settings.SaveAsync();
            }
            catch (Exception ex)
            {
                _services.LogService.Error($"[SettingsViewModel] Error guardando configuración: {ex.Message}");
            }
        }

        private static bool IsInvalidRoot(string path)
        {
            string folder = Path.GetFileName(path).ToUpperInvariant();
            return folder == "PS2" || folder == "PS1" || folder == "POPSMANAGER";
        }
    }
}