using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
        private bool _isProcessing;

        public ConvertViewModel()
        {
            _services = App.Services!;

            // Destino siempre es la raíz OPL
            OutputPath = _services.Settings.DestinationFolder ?? "";

            BrowseSourceCommand = new RelayCommand(BrowseSource);
            BrowseOutputCommand = new RelayCommand(BrowseOutput);
            ConvertCommand = new RelayCommand(async () => await ConvertAsync(), () => !IsProcessing);
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

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public ICommand BrowseSourceCommand { get; }
        public ICommand BrowseOutputCommand { get; }
        public ICommand ConvertCommand { get; }

        private void BrowseSource()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Seleccionar carpeta de origen (archivos a convertir)",
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
                Title = "Seleccionar carpeta de destino (raíz OPL)",
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
            if (!Directory.Exists(SourcePath)) return;

            var files = Directory.GetFiles(SourcePath, "*.*")
                .Where(f => f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".cue", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();

            foreach (var file in files)
                Files.Add(file);

            _services.Notifications.Info($"Se detectaron {Files.Count} archivos para convertir.");
        }

        private async Task ConvertAsync()
        {
            if (!Directory.Exists(SourcePath))
            {
                _services.Notifications.Error("La carpeta de origen no existe.");
                return;
            }

            if (!Directory.Exists(OutputPath))
            {
                _services.Notifications.Error("La carpeta de destino no existe.");
                return;
            }

            IsProcessing = true;
            try
            {
                // 1. CONVERTIR
                _services.Progress.Start("Convirtiendo archivos…");
                await Task.Run(() =>
                {
                    _services.Converter.ConvertFolder(SourcePath, OutputPath);
                });
                _services.Progress.SetStatus("Conversión finalizada.");

                // 2. POST‑PROCESAMIENTO (automático según configuración)
                var convertedFiles = Directory.GetFiles(OutputPath, "*.vcd")
                    .Where(vcd => Files.Any(f =>
                        Path.GetFileNameWithoutExtension(f).Equals(
                            Path.GetFileNameWithoutExtension(vcd),
                            StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                foreach (var vcdPath in convertedFiles)
                {
                    await ProcessConvertedFileAsync(vcdPath);
                }

                _services.Notifications.Success("Conversión y procesamiento completados.");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error($"Error durante la conversión: {ex.Message}");
            }
            finally
            {
                _services.Progress.Stop();
                IsProcessing = false;
            }
        }

        /// <summary>
        /// Procesa un VCD recién convertido según el modo de automatización global.
        /// </summary>
        private async Task ProcessConvertedFileAsync(string vcdPath)
        {
            var mode = _services.Automation.Mode;

            if (mode == Logic.Automation.AutomationMode.Manual)
            {
                // No hacer nada extra
                return;
            }

            if (mode == Logic.Automation.AutomationMode.Asistido)
            {
                var result = System.Windows.MessageBox.Show(
                    $"¿Deseas procesar {Path.GetFileName(vcdPath)}?\n(Se copiará a la estructura OPL, se generará ELF, etc.)",
                    "POPSManager",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            // Procesar el VCD
            await _services.GameProcessor.ProcessSingleGameAsync(vcdPath, "PS1");
        }
    }
}