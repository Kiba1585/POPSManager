using POPSManager.Models;
using POPSManager.Services;
using POPSManager.Settings;
using POPSManager.Logic;
using POPSManager.Logic.Cheats;
using POPSManager.Logic.Covers;
using POPSManager.Logic.Automation;
using POPSManager.UI.Progress;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POPSManager.Logic
{
    public sealed class GameProcessor
    {
        private readonly ProgressService _progress;
        private readonly LoggingService _log;
        private readonly NotificationService _notify;
        private readonly PathsService _paths;
        private readonly CheatSettingsService _cheatSettings;
        private readonly CheatManagerService _cheatManager;
        private readonly SettingsService _settings;
        private readonly AutomationEngine _auto;

        private static readonly int _maxCoverParallel =
            Math.Clamp(Environment.ProcessorCount * 2, 5, 10);

        private static readonly SemaphoreSlim _coverSemaphore =
            new SemaphoreSlim(_maxCoverParallel, _maxCoverParallel);

        public GameProcessor(
            ProgressService progress,
            LoggingService log,
            NotificationService notify,
            PathsService paths,
            CheatSettingsService cheatSettings,
            CheatManagerService cheatManager,
            SettingsService settings,
            AutomationEngine auto)
        {
            _progress = progress;
            _log = log;
            _notify = notify;
            _paths = paths;
            _cheatSettings = cheatSettings;
            _cheatManager = cheatManager;
            _settings = settings;
            _auto = auto;
        }

        // ============================================================
        //  API PÚBLICA
        // ============================================================
        public void ProcessFolder(string folder)
        {
            ProcessFolderAsync(folder, null).GetAwaiter().GetResult();
        }

        public async Task ProcessFolderAsync(
            string folder,
            ProgressViewModel? perGameProgress,
            CancellationToken ct = default)
        {
            if (!Directory.Exists(folder))
            {
                _notify.Error("La carpeta seleccionada no existe.");
                return;
            }

            var files = Directory.GetFiles(folder)
                .Where(f =>
                    f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f)
                .ToArray();

            if (files.Length == 0)
            {
                _notify.Warning("No se encontraron archivos VCD o ISO.");
                return;
            }

            var groups = GroupByRealGame(files);
            var groupList = groups.ToList();

            int total = groupList.Count;
            int completed = 0;

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = 2,
                CancellationToken = ct
            };

            try
            {
                await Parallel.ForEachAsync(groupList, options, async (group, token) =>
                {
                    token.ThrowIfCancellationRequested();

                    string baseTitle = group.Key;
                    var discs = group.Value;

                    string gameIdForUi = baseTitle; // se ajusta luego si detectamos ID

                    try
                    {
                        // Detectar tipo PS1/PS2
                        bool isPs1 = discs.Any(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase));

                        if (isPs1)
                        {
                            string firstDisc = discs.First();
                            string? detectedId = GameIdDetector.DetectGameId(firstDisc)
                                                ?? GameIdDetector.DetectFromName(baseTitle);

                            if (!string.IsNullOrWhiteSpace(detectedId))
                                gameIdForUi = detectedId;

                            perGameProgress?.AddGame(baseTitle, gameIdForUi);
                            perGameProgress?.UpdateStatus(gameIdForUi, "Preparando juego…");

                            var valid = discs.Where(ValidateVcd).ToList();
                            if (valid.Count == 0)
                            {
                                _log.Warn($"[PS1] Todos los VCD de {baseTitle} fueron inválidos.");
                                perGameProgress?.MarkError(gameIdForUi, "VCD inválidos.");
                            }
                            else
                            {
                                await ProcessPS1GroupAsync(
                                    baseTitle,
                                    valid,
                                    gameIdForUi,
                                    perGameProgress,
                                    token
                                ).ConfigureAwait(false);

                                perGameProgress?.MarkCompleted(gameIdForUi);
                            }
                        }
                        else
                        {
                            string iso = discs.First();

                            string originalName = Path.GetFileNameWithoutExtension(iso);
                            string? detectedId = GameIdDetector.DetectGameId(iso)
                                                ?? GameIdDetector.DetectFromName(originalName);

                            if (!string.IsNullOrWhiteSpace(detectedId))
                                gameIdForUi = detectedId;

                            perGameProgress?.AddGame(originalName, gameIdForUi);
                            perGameProgress?.UpdateStatus(gameIdForUi, "Preparando juego…");

                            if (!ValidateIso(iso))
                            {
                                _log.Warn($"[PS2] ISO inválido: {iso}");
                                perGameProgress?.MarkError(gameIdForUi, "ISO inválido.");
                            }
                            else
                            {
                                await ProcessPS2Async(
                                    iso,
                                    gameIdForUi,
                                    perGameProgress,
                                    token
                                ).ConfigureAwait(false);

                                perGameProgress?.MarkCompleted(gameIdForUi);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _log.Warn($"Procesamiento cancelado en {baseTitle}.");
                        _notify.Warning("Procesamiento cancelado.");
                        perGameProgress?.MarkError(gameIdForUi, "Cancelado.");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"Error procesando {baseTitle}: {ex.Message}");
                        _notify.Error($"Error procesando {baseTitle}");
                        perGameProgress?.MarkError(gameIdForUi, "Error en el procesamiento.");
                    }
                    finally
                    {
                        int done = Interlocked.Increment(ref completed);
                        int progressValue = (int)((done / (double)total) * 100);
                        _progress.SetProgress(progressValue);
                    }
                }).ConfigureAwait(false);

                _progress.SetStatus("Completado");
            }
            catch (OperationCanceledException)
            {
                _progress.SetStatus("Cancelado");
            }
        }

        // ============================================================
        //  AGRUPAR POR JUEGO REAL
        // ============================================================
        private Dictionary<string, List<string>> GroupByRealGame(string[] files)
        {
            var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string realTitle = NameCleanerBase.CleanTitleOnly(name);

                if (!groups.ContainsKey(realTitle))
                    groups[realTitle] = new List<string>();

                groups[realTitle].Add(file);
            }

            return groups;
        }

        private bool ValidateVcd(string vcdPath)
        {
            if (!IntegrityValidator.Validate(vcdPath))
            {
                _log.Warn($"[PS1] VCD inválido: {vcdPath}");
                _notify.Warning($"VCD inválido: {Path.GetFileName(vcdPath)}");
                return false;
            }
            return true;
        }

        private bool ValidateIso(string isoPath)
        {
            try
            {
                var info = new FileInfo(isoPath);
                if (!info.Exists || info.Length < 100_000_000)
                {
                    _notify.Warning($"ISO sospechoso o demasiado pequeño: {Path.GetFileName(isoPath)}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _log.Error($"[PS2] Error validando ISO: {ex.Message}");
                return false;
            }
        }

        // ============================================================
        //  PS1 MULTIDISCO
        // ============================================================
        private async Task ProcessPS1GroupAsync(
            string baseName,
            List<string> discs,
            string gameIdForUi,
            ProgressViewModel? perGameProgress,
            CancellationToken ct)
        {
            _log.Info($"[PS1] Procesando grupo: {baseName}");

            discs = discs.OrderBy(d =>
            {
                string fileName = Path.GetFileNameWithoutExtension(d);
                NameCleanerBase.Clean(fileName, out string? cdTag);
                return MultiDiscManager.ExtractDiscNumber(cdTag ?? fileName);
            }).ToList();

            string firstDisc = discs.First();
            string? detectedId = GameIdDetector.DetectGameId(firstDisc)
                                ?? GameIdDetector.DetectFromName(baseName);

            if (string.IsNullOrWhiteSpace(detectedId))
            {
                _notify.Warning($"No se pudo detectar ID para {baseName}");
                perGameProgress?.MarkError(gameIdForUi, "No se pudo detectar ID.");
                return;
            }

            bool useDb = _settings.UseDatabase && _auto.ShouldUseDatabase();
            bool useCovers = _settings.UseCovers && _auto.ShouldDownloadCovers();
            bool genCheats = _auto.ShouldGenerateCheats();
            bool handleMultiDisc = _auto.ShouldHandleMultiDisc();

            string cleanTitle = NameCleanerBase.CleanTitleOnly(baseName);

            GameEntry? dbEntry = null;
            if (useDb && GameDatabase.TryGetEntry(detectedId, out var entry))
            {
                dbEntry = entry;
                if (!string.IsNullOrWhiteSpace(dbEntry.Name))
                {
                    cleanTitle = dbEntry.Name;
                    _log.Info($"[DB] Nombre oficial encontrado: {cleanTitle}");
                }
            }

            if (useCovers && dbEntry?.CoverUrl != null)
            {
                string artFolder = Path.Combine(_paths.PopsFolder, "ART");
                Directory.CreateDirectory(artFolder);

                perGameProgress?.UpdateStatus(gameIdForUi, "Descargando cover…");

                await _coverSemaphore.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    string? art = await ArtDownloader
                        .DownloadArtAsync(detectedId, dbEntry.CoverUrl, artFolder, _log.Info)
                        .ConfigureAwait(false);

                    if (art != null)
                        _log.Info($"[COVER] PS1 ART generado → {art}");
                }
                finally
                {
                    _coverSemaphore.Release();
                }
            }

            string gameRootFolder = Path.Combine(_paths.PopsFolder, $"{cleanTitle}");
            Directory.CreateDirectory(gameRootFolder);

            int discNumber = 1;
            List<string> discPaths = new();

            foreach (var disc in discs)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    perGameProgress?.UpdateStatus(gameIdForUi, $"Copiando CD{discNumber}…");

                    string discFolder = Path.Combine(gameRootFolder, $"CD{discNumber}");
                    Directory.CreateDirectory(discFolder);

                    string finalFileName = $"{cleanTitle} (Disc {discNumber}).VCD";
                    string destVcd = Path.Combine(discFolder, finalFileName);

                    File.Copy(disc, destVcd, true);
                    discPaths.Add(destVcd);

                    _log.Info($"[PS1] Copiado disco {discNumber} → {destVcd}");
                }
                catch (Exception ex)
                {
                    _log.Error($"[PS1] Error copiando disco {discNumber}: {ex.Message}");
                }

                discNumber++;
            }

            if (handleMultiDisc)
            {
                perGameProgress?.UpdateStatus(gameIdForUi, "Generando DISCS.TXT…");
                MultiDiscManager.GenerateDiscsTxt(_paths.PopsFolder, detectedId, discPaths, _log.Info, _auto);
            }

            if (genCheats && GameIdDetector.IsPalRegion(detectedId))
            {
                perGameProgress?.UpdateStatus(gameIdForUi, "Generando cheats…");
                string cd1Folder = Path.Combine(gameRootFolder, "CD1");
                CheatGenerator.GenerateCheatTxt(detectedId, cd1Folder, _log.Info);
            }

            perGameProgress?.UpdateStatus(gameIdForUi, "Generando ELF…");
            GenerateElfForDisc1(detectedId, cleanTitle, gameRootFolder);

            _notify.Success($"{cleanTitle} procesado correctamente.");
        }

        private void GenerateElfForDisc1(string gameId, string title, string gameRootFolder)
        {
            string cd1Folder = Path.Combine(gameRootFolder, "CD1");
            string? vcdPath = Directory.GetFiles(cd1Folder, "*.VCD").FirstOrDefault();

            if (vcdPath == null)
            {
                _log.Warn($"[PS1] No se encontró VCD en {cd1Folder}");
                return;
            }

            bool ok = ElfGenerator.GeneratePs1Elf(
                _paths.PopstarterElfPath,
                vcdPath,
                _paths.AppsFolder,
                1,
                title,
                gameId,
                _log.Info
            );

            if (!ok)
            {
                _notify.Error($"Error generando ELF para {gameId}");
                return;
            }

            _log.Info($"[PS1] ELF generado para {gameId}");
        }

        // ============================================================
        //  PS2
        // ============================================================
        private async Task ProcessPS2Async(
            string isoPath,
            string gameIdForUi,
            ProgressViewModel? perGameProgress,
            CancellationToken ct)
        {
            string originalName = Path.GetFileNameWithoutExtension(isoPath);
            _log.Info($"[PS2] Procesando: {originalName}");

            string? detectedId = GameIdDetector.DetectGameId(isoPath)
                                ?? GameIdDetector.DetectFromName(originalName);

            if (string.IsNullOrWhiteSpace(detectedId))
            {
                _notify.Warning($"No se pudo detectar ID para {originalName}. Se copiará sin renombrar.");
                detectedId = originalName.Replace(" ", "_");
            }

            bool useDb = _settings.UseDatabase && _auto.ShouldUseDatabase();
            bool useCovers = _settings.UseCovers && _auto.ShouldDownloadCovers();

            string cleanTitle = NameCleanerBase.CleanTitleOnly(originalName);

            GameEntry? dbEntry = null;
            if (useDb && GameDatabase.TryGetEntry(detectedId, out var entry))
            {
                dbEntry = entry;
                if (!string.IsNullOrWhiteSpace(dbEntry.Name))
                {
                    cleanTitle = dbEntry.Name;
                    _log.Info($"[DB] Nombre oficial PS2 encontrado: {cleanTitle}");
                }

                if (useCovers && dbEntry?.CoverUrl != null)
                {
                    string artFolder = Path.Combine(_paths.DvdFolder, "ART");
                    Directory.CreateDirectory(artFolder);

                    perGameProgress?.UpdateStatus(gameIdForUi, "Descargando cover…");

                    await _coverSemaphore.WaitAsync(ct).ConfigureAwait(false);
                    try
                    {
                        string? art = await ArtDownloader
                            .DownloadArtAsync(detectedId, dbEntry.CoverUrl, artFolder, _log.Info)
                            .ConfigureAwait(false);

                        if (art != null)
                            _log.Info($"[COVER] PS2 ART generado → {art}");
                    }
                    finally
                    {
                        _coverSemaphore.Release();
                    }
                }
            }

            Directory.CreateDirectory(_paths.DvdFolder);

            perGameProgress?.UpdateStatus(gameIdForUi, "Copiando ISO…");

            string dest = Path.Combine(_paths.DvdFolder, $"{cleanTitle}.ISO");

            ct.ThrowIfCancellationRequested();
            File.Copy(isoPath, dest, true);

            _log.Info($"[PS2] Copiado ISO → {dest}");
            _notify.Success($"{cleanTitle} copiado a DVD correctamente.");
        }
    }
}
