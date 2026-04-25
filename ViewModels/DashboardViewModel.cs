using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using POPSManager.Commands;
using POPSManager.Services;
using POPSManager.Views;

namespace POPSManager.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly AppServices _services;
        private readonly PathsService _paths;
        private readonly SettingsService _settings;
        private string _rootPath = string.Empty;
        private string _elfPath = string.Empty;
        private string _sourcePath = string.Empty;
        private string _destinationPath = string.Empty;
        private string _elfFolderPath = string.Empty;
        private bool _processSubfolders = true;
        private string _systemInfo = string.Empty;

        public DashboardViewModel()
        {
            _services = App.Services!;
            _paths = _services.Paths;
            _settings = _services.Settings;

            // Comandos de navegación
            OpenConvertCommand = new RelayCommand(() => NavigateTo(new ConvertView()));
            OpenProcessPopsCommand = new RelayCommand(() => NavigateTo(new ProcessPopsView()));

            // Accesos rápidos existentes
            OpenRootFolderCommand = new RelayCommand(OpenRootFolder, () => Directory.Exists(RootPath));
            OpenElfFolderCommand = new RelayCommand(OpenElfFolder, () => File.Exists(ElfPath));

            // NUEVOS COMANDOS para seleccionar rutas
            ChangeSourceFolderCommand = new RelayCommand(async () => await ChangeSourceFolderAsync());
            ChangeDestinationFolderCommand = new RelayCommand(async () => await ChangeDestinationFolderAsync());
            ChangeElfFolderCommand = new RelayCommand(async () => await ChangeElfFolderAsync());

            LoadData();
        }

        // Estadísticas (placeholders)
        public string ProcessedCount => "0";
        public string PendingCount => "0";
        public string ErrorCount => "0";

        // Rutas
        public string RootPath { get => _rootPath; set => SetProperty(ref _rootPath, value); }
        public string ElfPath { get => _elfPath; set => SetProperty(ref _elfPath, value); }
        public string SourcePath { get => _sourcePath; set => SetProperty(ref _sourcePath, value); }
        public string DestinationPath { get => _destinationPath; set => SetProperty(ref _destinationPath, value); }
        public string ElfFolderPath { get => _elfFolderPath; set => SetProperty(ref _elfFolderPath, value); }
        public bool ProcessSubfolders
        {
            get => _processSubfolders;
            set => SetProperty(ref _processSubfolders, value);
        }
        public string SystemInfo { get => _systemInfo; set => SetProperty(ref _systemInfo, value); }

        // Comandos existentes
        public ICommand OpenConvertCommand { get; }
        public ICommand OpenProcessPopsCommand { get; }
        public ICommand OpenRootFolderCommand { get; }
        public ICommand OpenElfFolderCommand { get; }

        // NUEVOS COMANDOS públicos
        public ICommand ChangeSourceFolderCommand { get; }
        public ICommand ChangeDestinationFolderCommand { get; }
        public ICommand ChangeElfFolderCommand { get; }

        private void LoadData()
        {
            RootPath = _paths.RootFolder ?? "";
            ElfPath = _paths.PopstarterElfPath ?? "";
            SourcePath = _settings.SourceFolder ?? "";
            DestinationPath = _settings.DestinationFolder ?? "";
            ElfFolderPath = _settings.ElfFolder ?? "";
            ProcessSubfolders = _settings.ProcessSubfolders;
            SystemInfo = $"Versión: 1.0.0\n" +
                         $"Directorio base: {AppContext.BaseDirectory}\n" +
                         $"Ruta raíz: {_paths.RootFolder}\n" +
                         $"POPS: {_paths.PopsFolder}\n" +
                         $"DVD (PS2): {_paths.DvdFolder}\n" +
                         $"POPSTARTER.ELF: {_paths.PopstarterElfPath}\n" +
                         $"POPS2.ELF: {_paths.PopstarterPs2ElfPath}";
        }

        private static void NavigateTo(System.Windows.Controls.UserControl view)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow main &&
                main.DataContext is MainViewModel vm)
            {
                vm.CurrentView = view;
            }
        }

        // --- Métodos de acceso rápido (sin cambios) ---
        private void OpenRootFolder()
        {
            if (!Directory.Exists(RootPath))
            {
                _services.Notifications.Error("La carpeta raíz no existe.");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo { FileName = RootPath, UseShellExecute = true });
            }
            catch
            {
                _services.Notifications.Error("No se pudo abrir la carpeta raíz.");
            }
        }

        private void OpenElfFolder()
        {
            string elf = ElfPath;

            if (!File.Exists(elf))
            {
                _services.Notifications.Error("POPSTARTER.ELF no está configurado o no existe.");
                return;
            }

            string? folder = Path.GetDirectoryName(elf);
            if (folder == null || !Directory.Exists(folder))
            {
                _services.Notifications.Error("No se pudo determinar la carpeta del ELF.");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
            }
            catch
            {
                _services.Notifications.Error("No se pudo abrir la carpeta del ELF.");
            }
        }

        // --- NUEVOS MÉTODOS para seleccionar rutas ---
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
            LoadData();
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
            LoadData();
            _services.Notifications.Success("Carpeta destino actualizada.");
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
            // Buscar automáticamente los ELFs dentro de la carpeta
            string popstarter = Path.Combine(dialog.FolderName, "POPSTARTER.ELF");
            string pops2 = Path.Combine(dialog.FolderName, "POPS2.ELF");
            if (File.Exists(popstarter)) await _paths.SetCustomElfPathAsync(popstarter);
            if (File.Exists(pops2)) await _paths.SetCustomPs2ElfPathAsync(pops2);
            await _settings.SaveAsync();
            LoadData();
            _services.Notifications.Success("Carpeta de ELFs actualizada.");
        }
    }
}