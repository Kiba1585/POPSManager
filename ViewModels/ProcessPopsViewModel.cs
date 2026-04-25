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

namespace POPSManager.ViewModels
{
    public class ProcessGameItem
    {
        public string Name { get; set; } = "";
        public string GameId { get; set; } = "";
        public string Path { get; set; } = "";
        public string Category { get; set; } = "";
    }

    public class ProcessPopsViewModel : ViewModelBase
    {
        private readonly AppServices _services;
        
        private ObservableCollection<ProcessGameItem> _ps1Games = new();
        private ObservableCollection<ProcessGameItem> _ps2Games = new();
        private ObservableCollection<ProcessGameItem> _appsGames = new();
        private ProcessGameItem? _selectedPs1Game;
        private ProcessGameItem? _selectedPs2Game;
        private ProcessGameItem? _selectedAppsGame;
        private bool _isProcessing;

        public ProcessPopsViewModel()
        {
            _services = App.Services!;

            // Suscribirse a cambios globales
            _services.Settings.OnSettingsChanged += () => LoadGamesFromOplRoot();
            _services.Localization.PropertyChanged += (_, _) => LoadGamesFromOplRoot();

            RefreshGamesCommand = new RelayCommand(LoadGamesFromOplRoot);
            ProcessSelectedCommand = new RelayCommand(async () => await ProcessSelectedAsync(), () => !IsProcessing);
            DownloadCoversCommand = new RelayCommand(async () => await DownloadCoversAsync(), () => SelectedGame != null && !IsProcessing);
            GenerateElfCommand = new RelayCommand(async () => await GenerateElfAsync(), () => SelectedGame != null && !IsProcessing);
            GenerateMetadataCommand = new RelayCommand(async () => await GenerateMetadataAsync(), () => SelectedGame != null && !IsProcessing);
            GenerateCheatsCommand = new RelayCommand(async () => await GenerateCheatsAsync(), () => SelectedGame != null && !IsProcessing);

            LoadGamesFromOplRoot();
        }

        public ObservableCollection<ProcessGameItem> Ps1Games { get => _ps1Games; set => SetProperty(ref _ps1Games, value); }
        public ObservableCollection<ProcessGameItem> Ps2Games { get => _ps2Games; set => SetProperty(ref _ps2Games, value); }
        public ObservableCollection<ProcessGameItem> AppsGames { get => _appsGames; set => SetProperty(ref _appsGames, value); }

        public ProcessGameItem? SelectedPs1Game { get => _selectedPs1Game; set => SetProperty(ref _selectedPs1Game, value); }
        public ProcessGameItem? SelectedPs2Game { get => _selectedPs2Game; set => SetProperty(ref _selectedPs2Game, value); }
        public ProcessGameItem? SelectedAppsGame { get => _selectedAppsGame; set => SetProperty(ref _selectedAppsGame, value); }

        private ProcessGameItem? SelectedGame =>
            SelectedPs1Game ?? SelectedPs2Game ?? SelectedAppsGame;

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public ICommand RefreshGamesCommand { get; }
        public ICommand ProcessSelectedCommand { get; }
        public ICommand DownloadCoversCommand { get; }
        public ICommand GenerateElfCommand { get; }
        public ICommand GenerateMetadataCommand { get; }
        public ICommand GenerateCheatsCommand { get; }

        private void LoadGamesFromOplRoot()
        {
            Ps1Games.Clear();
            Ps2Games.Clear();
            AppsGames.Clear();

            string root = _services.Paths.RootFolder;
            if (!Directory.Exists(root))
            {
                _services.Notifications.Warning("La carpeta raíz OPL no existe. Configúrala en el Dashboard o en Configuración.");
                return;
            }

            string popsFolder = _services.Paths.PopsFolder;
            if (Directory.Exists(popsFolder))
            {
                var ps1Dirs = Directory.GetDirectories(popsFolder);
                foreach (var dir in ps1Dirs)
                {
                    string dirName = Path.GetFileName(dir);
                    string cd1Folder = Path.Combine(dir, "CD1");
                    string? vcdFile = Directory.Exists(cd1Folder)
                        ? Directory.GetFiles(cd1Folder, "*.VCD").FirstOrDefault()
                        : null;

                    string gameId = vcdFile != null
                        ? GameIdDetector.DetectGameId(vcdFile) ?? dirName
                        : dirName;

                    Ps1Games.Add(new ProcessGameItem
                    {
                        Name = dirName,
                        GameId = gameId,
                        Path = dir,
                        Category = "PS1"
                    });
                }
            }

            string dvdFolder = _services.Paths.DvdFolder;
            if (Directory.Exists(dvdFolder))
            {
                var isoFiles = Directory.GetFiles(dvdFolder, "*.ISO");
                foreach (var iso in isoFiles)
                {
                    string name = Path.GetFileNameWithoutExtension(iso);
                    string gameId = GameIdDetector.DetectGameId(iso) ?? name;

                    Ps2Games.Add(new ProcessGameItem
                    {
                        Name = name,
                        GameId = gameId,
                        Path = iso,
                        Category = "PS2"
                    });
                }
            }

            string appsFolder = _services.Paths.AppsFolder;
            if (Directory.Exists(appsFolder))
            {
                var elfFiles = Directory.GetFiles(appsFolder, "*.ELF*");
                var ps1GameIds = Ps1Games.Select(g => g.GameId).ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var elf in elfFiles)
                {
                    string name = Path.GetFileName(elf);
                    string gameId = GameIdDetector.DetectFromName(name);

                    if (!string.IsNullOrWhiteSpace(gameId) && ps1GameIds.Contains(gameId))
                        continue;

                    AppsGames.Add(new ProcessGameItem
                    {
                        Name = name,
                        GameId = gameId,
                        Path = elf,
                        Category = "APP"
                    });
                }
            }

            _services.Notifications.Info($"Juegos encontrados: PS1={Ps1Games.Count}, PS2={Ps2Games.Count}, APPs={AppsGames.Count}");
        }

        private async Task ProcessSelectedAsync()
        {
            var game = SelectedGame;
            if (game == null) return;

            IsProcessing = true;
            try
            {
                await _services.GameProcessor.ProcessSingleGameAsync(game.Path, game.Category);
                _services.Notifications.Success($"{game.Name} procesado correctamente.");
                LoadGamesFromOplRoot();
            }
            catch (Exception ex)
            {
                _services.Notifications.Error($"Error procesando {game.Name}: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task DownloadCoversAsync()
        {
            var game = SelectedGame;
            if (game == null) return;

            IsProcessing = true;
            try
            {
                await _services.GameProcessor.DownloadCoverAsync(game.GameId, game.Category);
                _services.Notifications.Success($"Carátula descargada para {game.Name}.");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error($"Error descargando carátula: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task GenerateElfAsync()
        {
            var game = SelectedGame;
            if (game == null) return;

            IsProcessing = true;
            try
            {
                await _services.GameProcessor.GenerateElfAsync(game.Path, game.GameId, game.Category);
                _services.Notifications.Success($"ELF generado para {game.Name}.");
                LoadGamesFromOplRoot();
            }
            catch (Exception ex)
            {
                _services.Notifications.Error($"Error generando ELF: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task GenerateMetadataAsync()
        {
            var game = SelectedGame;
            if (game == null) return;

            IsProcessing = true;
            try
            {
                await _services.GameProcessor.GenerateMetadataAsync(game.GameId, game.Path, game.Category);
                _services.Notifications.Success($"Metadatos generados para {game.Name}.");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error($"Error generando metadatos: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task GenerateCheatsAsync()
        {
            var game = SelectedGame;
            if (game == null) return;

            IsProcessing = true;
            try
            {
                await _services.GameProcessor.GenerateCheatsAsync(game.GameId, game.Path, game.Category);
                _services.Notifications.Success($"Cheats generados para {game.Name}.");
            }
            catch (Exception ex)
            {
                _services.Notifications.Error($"Error generando cheats: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}