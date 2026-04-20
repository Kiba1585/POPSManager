using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using POPSManager.Commands;
using POPSManager.Services;
using POPSManager.UI.Windows;

namespace POPSManager.ViewModels
{
    public class ProcessPopsViewModel : ViewModelBase
    {
        private readonly AppServices _services;
        private string _vcdPath = string.Empty;
        private ObservableCollection<string> _games = new();

        public ProcessPopsViewModel()
        {
            _services = App.Services!;

            BrowseVcdCommand = new RelayCommand(BrowseVcd);
            ProcessCommand = new RelayCommand(async () => await ProcessAsync(), CanProcess);
        }

        public string VcdPath
        {
            get => _vcdPath;
            set { _vcdPath = value; OnPropertyChanged(); LoadGames(); }
        }

        public ObservableCollection<string> Games
        {
            get => _games;
            set { _games = value; OnPropertyChanged(); }
        }

        public ICommand BrowseVcdCommand { get; }
        public ICommand ProcessCommand { get; }

        private void BrowseVcd()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Seleccionar carpeta con juegos (VCD / ISO)",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                VcdPath = dialog.FolderName;
            }
        }

        private async void LoadGames()
        {
            Games.Clear();

            if (!Directory.Exists(VcdPath))
            {
                _services.Notifications.Warning("La carpeta seleccionada no existe.");
                return;
            }

            try
            {
                var files = await Task.Run(() =>
                    Directory.GetFiles(VcdPath)
                             .Where(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase) ||
                                         f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                             .OrderBy(f => f)
                             .Select(Path.GetFileName)
                             .ToArray()
                );

                foreach (var file in files)
                    Games.Add(file!);

                _services.Notifications.Info($"Se detectaron {Games.Count} juegos (PS1/PS2).");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error("No se pudieron cargar los juegos.");
                _services.LogService.Error($"[ProcessPopsViewModel] Error cargando juegos: {ex.Message}");
            }
        }

        private bool CanProcess() => Directory.Exists(VcdPath) && Games.Count > 0;

        private async Task ProcessAsync()
        {
            bool useAdvancedWindow = Games.Count >= 2;
            ProgressWindow? win = null;

            if (useAdvancedWindow)
            {
                win = new ProgressWindow
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };
                win.Show();
            }
            else
            {
                _services.Progress.Reset();
                _services.Progress.Start(_services.Localization.GetString("Label_ProcessingGames"));
            }

            try
            {
                await Task.Run(async () =>
                {
                    await _services.GameProcessor.ProcessFolderAsync(
                        VcdPath,
                        win?.ViewModel
                    );
                });

                _services.Notifications.Success("Procesamiento completado.");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error($"Error durante el procesamiento: {ex.Message}");
                _services.LogService.Error($"[ProcessPopsViewModel] Error procesando juegos: {ex}");
            }
            finally
            {
                _services.Progress.Stop();
                win?.Close();
            }
        }
    }
}