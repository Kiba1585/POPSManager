using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using POPSManager.Commands;
using POPSManager.Services;

namespace POPSManager.ViewModels
{
    public class ConvertViewModel : ViewModelBase
    {
        private readonly AppServices _services;
        private string _sourcePath = string.Empty;
        private string _outputPath = string.Empty;
        private ObservableCollection<string> _files = new();

        public ConvertViewModel()
        {
            _services = App.Services!;

            BrowseSourceCommand = new RelayCommand(BrowseSource);
            BrowseOutputCommand = new RelayCommand(BrowseOutput);
            ConvertCommand = new RelayCommand(async () => await ConvertAsync(), CanConvert);
        }

        public string SourcePath
        {
            get => _sourcePath;
            set { _sourcePath = value; OnPropertyChanged(); LoadFiles(); }
        }

        public string OutputPath
        {
            get => _outputPath;
            set { _outputPath = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Files
        {
            get => _files;
            set { _files = value; OnPropertyChanged(); }
        }

        public ICommand BrowseSourceCommand { get; }
        public ICommand BrowseOutputCommand { get; }
        public ICommand ConvertCommand { get; }

        private void BrowseSource()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Seleccionar carpeta de origen",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                SourcePath = dialog.FolderName;
            }
        }

        private void BrowseOutput()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Seleccionar carpeta de destino",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                OutputPath = dialog.FolderName;
            }
        }

        private void LoadFiles()
        {
            Files.Clear();

            if (!Directory.Exists(SourcePath))
                return;

            var files = Directory.GetFiles(SourcePath, "*.*")
                .Where(f => f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".cue", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name)) // Filtra posibles null
                .ToList();

            foreach (var file in files)
            {
                Files.Add(file);
            }

            _services.Notifications.Info($"Se detectaron {Files.Count} archivos.");
        }

        private bool CanConvert() =>
            Directory.Exists(SourcePath) &&
            Directory.Exists(OutputPath) &&
            Files.Count > 0;

        private async Task ConvertAsync()
        {
            _services.Progress.Start("Convirtiendo archivos...");

            try
            {
                await Task.Run(() =>
                {
                    _services.Converter.ConvertFolder(SourcePath, OutputPath);
                });

                _services.Progress.SetStatus("Listo");
                _services.Notifications.Success("Conversión completada.");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error($"Error durante la conversión: {ex.Message}");
            }
            finally
            {
                _services.Progress.Stop();
            }
        }
    }
}