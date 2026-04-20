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
        private string _rootPath = string.Empty;
        private string _elfPath = string.Empty;
        private string _systemInfo = string.Empty;

        public DashboardViewModel()
        {
            _services = App.Services!;
            _paths = _services.Paths;

            OpenConvertCommand = new RelayCommand(() => NavigateTo(new ConvertView()));
            OpenProcessPopsCommand = new RelayCommand(() => NavigateTo(new ProcessPopsView()));
            OpenRootFolderCommand = new RelayCommand(OpenRootFolder, () => Directory.Exists(RootPath));
            OpenElfFolderCommand = new RelayCommand(OpenElfFolder, () => File.Exists(ElfPath));
            ChangeRootPathCommand = new RelayCommand(ChangeRootPath);
            SelectElfCommand = new RelayCommand(SelectElf);

            LoadData();
        }

        public string ProcessedCount => "0";
        public string PendingCount => "0";
        public string ErrorCount => "0";

        public string RootPath { get => _rootPath; set => SetProperty(ref _rootPath, value); }
        public string ElfPath { get => _elfPath; set => SetProperty(ref _elfPath, value); }
        public string SystemInfo { get => _systemInfo; set => SetProperty(ref _systemInfo, value); }

        public ICommand OpenConvertCommand { get; }
        public ICommand OpenProcessPopsCommand { get; }
        public ICommand OpenRootFolderCommand { get; }
        public ICommand OpenElfFolderCommand { get; }
        public ICommand ChangeRootPathCommand { get; }
        public ICommand SelectElfCommand { get; }

        private void LoadData()
        {
            RootPath = _paths.RootFolder ?? "";
            ElfPath = _paths.PopstarterElfPath ?? "";
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

        private void ChangeRootPath()
        {
            var dialog = new OpenFolderDialog { Title = "Selecciona la carpeta raíz" };
            if (dialog.ShowDialog() != true) return;

            string newPath = dialog.FolderName;
            if (!Directory.Exists(newPath))
            {
                _services.Notifications.Error("La carpeta seleccionada no existe.");
                return;
            }

            _services.Settings.SetRootFolder(newPath);
            LoadData();
            _services.Notifications.Success("Ruta raíz actualizada correctamente.");
        }

        private void SelectElf()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar POPSTARTER.ELF",
                Filter = "ELF files (*.ELF)|*.ELF|Todos los archivos (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true) return;

            string path = dialog.FileName;
            if (!File.Exists(path))
            {
                _services.Notifications.Error("El archivo seleccionado no existe.");
                return;
            }

            _services.Settings.SetCustomElfPath(path);
            LoadData();
            _services.Notifications.Success("POPSTARTER.ELF actualizado correctamente.");
        }
    }
}