using POPSManager.Models;
using POPSManager.Services;
using POPSManager.Settings;
using POPSManager.Logic.Cheats;
using POPSManager.Logic.Covers;
using POPSManager.Logic.Automation;
using POPSManager.UI.Progress;
using POPSManager.UI.Localization;
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
        private readonly LocalizationService _loc;

        private static readonly int _maxCoverParallel = Math.Clamp(Environment.ProcessorCount * 2, 5, 10);
        private static readonly SemaphoreSlim _coverSemaphore = new(_maxCoverParallel, _maxCoverParallel);

        public GameProcessor(
            ProgressService progress,
            LoggingService log,
            NotificationService notify,
            PathsService paths,
            CheatSettingsService cheatSettings,
            CheatManagerService cheatManager,
            SettingsService settings,
            AutomationEngine auto,
            LocalizationService loc)
        {
            _progress = progress;
            _log = log;
            _notify = notify;
            _paths = paths;
            _cheatSettings = cheatSettings;
            _cheatManager = cheatManager;
            _settings = settings;
            _auto = auto;
            _loc = loc;
        }

        public void ProcessFolder(string folder) => ProcessFolderAsync(folder, null).GetAwaiter().GetResult();

        public async Task ProcessFolderAsync(string folder, ProgressViewModel? perGameProgress, CancellationToken ct = default)
        {
            if (!Directory.Exists(folder))
            {
                _notify.Error(_loc.GetString("GameProcessor_InvalidFolder"));
                return;
            }

            var files = Directory.GetFiles(folder)
                .Where(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f).ToArray();

            if (files.Length == 0)
            {
                _notify.Warning(_loc.GetString("GameProcessor_NoFilesFound"));
                return;
            }

            var groups = GroupByRealGame(files);
            var groupList = groups.ToList();
            int total = groupList.Count;
            int completed = 0;

            var options = new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = ct };

            try
            {
                await Parallel.ForEachAsync(groupList, options, async (group, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    string baseTitle = group.Key;
                    var discs = group.Value;
                    string gameIdForUi = baseTitle;

                    try
                    {
                        bool isPs1 = discs.Any(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase));

                        if (isPs1)
                        {
                            string firstDisc = discs.First();
                            string detectedId = GameIdDetector.DetectGameId(firstDisc) ?? GameIdDetector.DetectFromName(baseTitle) ?? "";
                            if (!string.IsNullOrWhiteSpace(detectedId)) gameIdForUi = detectedId;

                            perGameProgress?.AddGame(baseTitle, gameIdForUi);
                            perGameProgress?.UpdateStatus(gameIdForUi, _loc.GetString("Progress_Preparing"));

                            var valid = discs.Where(ValidateVcd).ToList();
                            if (valid.Count == 0)
                            {
                                _log.Warn(string.Format("[PS1] {0} {1}", _loc.GetString("GameProcessor_AllVcdInvalid"), baseTitle));
                                perGameProgress?.MarkError(gameIdForUi, _loc.GetString("GameProcessor_VcdInvalid"));
                            }
                            else
                            {
                                await ProcessPS1GroupAsync(baseTitle, valid, gameIdForUi, perGameProgress, token);
                                perGameProgress?.MarkCompleted(gameIdForUi);
                            }
                        }
                        else
                        {
                            string iso = discs.First();
                            string originalName = Path.GetFileNameWithoutExtension(iso);
                            string detectedId = GameIdDetector.DetectGameId(iso) ?? GameIdDetector.DetectFromName(originalName) ?? "";
                            if (!string.IsNullOrWhiteSpace(detectedId)) gameIdForUi = detectedId;

                            perGameProgress?.AddGame(originalName, gameIdForUi);
                            perGameProgress?.UpdateStatus(gameIdForUi, _loc.GetString("Progress_Preparing"));

                            if (!ValidateIso(iso))
                            {
                                _log.Warn(string.Format("[PS2] {0}: {1}", _loc.GetString("GameProcessor_InvalidIso"), iso));
                                perGameProgress?.MarkError(gameIdForUi, _loc.GetString("GameProcessor_IsoInvalid"));
                            }
                            else
                            {
                                await ProcessPS2Async(iso, gameIdForUi, perGameProgress, token);
                                perGameProgress?.MarkCompleted(gameIdForUi);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _log.Warn(string.Format("{0} {1}", _loc.GetString("GameProcessor_ProcessingCancelled"), baseTitle));
                        _notify.Warning(_loc.GetString("GameProcessor_ProcessingCancelled"));
                        perGameProgress?.MarkError(gameIdForUi, _loc.GetString("GameProcessor_Cancelled"));
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _log.Error(string.Format("{0} {1}: {2}", _loc.GetString("GameProcessor_ErrorProcessing"), baseTitle, ex.Message));
                        _notify.Error(string.Format("{0} {1}", _loc.GetString("GameProcessor_ErrorProcessing"), baseTitle));
                        perGameProgress?.MarkError(gameIdForUi, _loc.GetString("GameProcessor_ErrorInProcessing"));
                    }
                    finally
                    {
                        int done = Interlocked.Increment(ref completed);
                        _progress.SetProgress((int)((done / (double)total) * 100));
                    }
                });

                _progress.SetStatus(_loc.GetString("Label_Completed"));
                await CopyCustomAssetsAsync();
            }
            catch (OperationCanceledException)
            {
                _progress.SetStatus(_loc.GetString("GameProcessor_Cancelled"));
            }
        }

        private async Task CopyCustomAssetsAsync()
        {
            if (_auto.ShouldCopyLng())
                await Task.Run(() => CopyCustomFolderContents(_paths.LngFolder, "LNG", _log.Info));
            else
                _log.Info("[AUTO] Copia de archivos LNG desactivada por automatizacion.");

            if (_auto.ShouldCopyThm())
                await Task.Run(() => CopyCustomFolderContents(_paths.ThmFolder, "THM", _log.Info));
            else
                _log.Info("[AUTO] Copia de temas THM desactivada por automatizacion.");
        }

        private Dictionary<string, List<string>> GroupByRealGame(string[] files)
        {
            var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string realTitle = NameCleanerBase.CleanTitleOnly(name);
                if (!groups.ContainsKey(realTitle)) groups[realTitle] = new List<string>();
                groups[realTitle].Add(file);
            }
            return groups;
        }

        private bool ValidateVcd(string vcdPath)
        {
            if (!IntegrityValidator.Validate(vcdPath))
            {
                _log.Warn(string.Format("[PS1] {0}: {1}", _loc.GetString("GameProcessor_InvalidVcd"), vcdPath));
                _notify.Warning(string.Format("{0}: {1}", _loc.GetString("GameProcessor_InvalidVcd"), Path.GetFileName(vcdPath)));
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
                    _notify.Warning(string.Format("{0}: {1}", _loc.GetString("GameProcessor_SuspiciousIso"), Path.GetFileName(isoPath)));
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _log.Error(string.Format("[PS2] {0}: {1}", _loc.GetString("GameProcessor_ErrorValidatingIso"), ex.Message));
                return false;
            }
        }

        private async Task ProcessPS1GroupAsync(string baseName, List<string> discs, string gameIdForUi, ProgressViewModel? perGameProgress, CancellationToken ct)
        {
            _log.Info(string.Format("[PS1] {0}: {1}", _loc.GetString("GameProcessor_ProcessingGroup"), baseName));

            discs = discs.OrderBy(d =>
            {
                string fileName = Path.GetFileNameWithoutExtension(d);
                NameCleanerBase.Clean(fileName, out string cdTag);
                return MultiDiscManager.ExtractDiscNumber(cdTag ?? fileName);
            }).ToList();

            string firstDisc = discs.First();
            string detectedId = GameIdDetector.DetectGameId(firstDisc) ?? GameIdDetector.DetectFromName(baseName) ?? "";
            if (string.IsNullOrWhiteSpace(detectedId))
            {
                _notify.Warning(string.Format("{0} {1}", _loc.GetString("GameProcessor_CouldNotDetectId"), baseName));
                perGameProgress?.MarkError(gameIdForUi, _loc.GetString("GameProcessor_CouldNotDetectId"));
                return;
            }

            bool useDb = _settings.UseDatabase && _auto.ShouldUseDatabase();
            bool useCovers = _settings.UseCovers && _auto.ShouldDownloadCovers();
            bool genCheats = _auto.ShouldGenerateCheats();
            bool handleMultiDisc = _auto.ShouldHandleMultiDisc();
            bool useMetadata = _settings.UseMetadata;

            string cleanTitle = NameCleanerBase.CleanTitleOnly(baseName);
            GameEntry dbEntry = null;
            if (useDb && GameDatabase.TryGetEntry(detectedId, out var entry))
            {
                dbEntry = entry;
                if (!string.IsNullOrWhiteSpace(dbEntry.Name))
                {
                    cleanTitle = dbEntry.Name;
                    _log.Info(string.Format("[DB] {0}: {1}", _loc.GetString("GameProcessor_OfficialNameFound"), cleanTitle));
                }
            }

            if (useCovers && dbEntry?.CoverUrl != null)
            {
                string artFolder = Path.Combine(_paths.PopsFolder, "ART");
                Directory.CreateDirectory(artFolder);
                perGameProgress?.UpdateStatus(gameIdForUi, _loc.GetString("Progress_DownloadingCover"));
                await _coverSemaphore.WaitAsync(ct);
                try
                {
                    string art = await ArtDownloader.DownloadArtAsync(detectedId, dbEntry.CoverUrl, artFolder, _log.Info);
                    if (art != null) _log.Info(string.Format("[COVER] PS1 ART {0} -> {1}", _loc.GetString("GameProcessor_Generated"), art));
                }
                finally { _coverSemaphore.Release(); }
            }

            string gameRootFolder = Path.Combine(_paths.PopsFolder, cleanTitle);
            Directory.CreateDirectory(gameRootFolder);
            int discNumber = 1;
            List<string> discPaths = new();

            foreach (var disc in discs)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    perGameProgress?.UpdateStatus(gameIdForUi, string.Format(_loc.GetString("Progress_CopyingDisc"), discNumber));
                    string discFolder = Path.Combine(gameRootFolder, string.Format("CD{0}", discNumber));
                    Directory.CreateDirectory(discFolder);
                    string finalFileName = string.Format("{0} (Disc {1}).VCD", cleanTitle, discNumber);
                    string destVcd = Path.Combine(discFolder, finalFileName);
                    File.Copy(disc, destVcd, true);
                    discPaths.Add(destVcd);
                    _log.Info(string.Format("[PS1] {0} {1} -> {2}", _loc.GetString("GameProcessor_CopiedDisc"), discNumber, destVcd));
                }
                catch (Exception ex)
                {
                    _log.Error(string.Format("[PS1] {0} {1}: {2}", _loc.GetString("GameProcessor_ErrorCopyingDisc"), discNumber, ex.Message));
                }
                discNumber++;
            }

            if (handleMultiDisc)
            {
                perGameProgress?.UpdateStatus(gameIdForUi, _loc.GetString("Progress_GeneratingDiscsTxt"));
                MultiDiscManager.GenerateDiscsTxt(_paths.PopsFolder, detectedId, discPaths, _log.Info, _auto);
            }

            if (genCheats && GameIdDetector.IsPalRegion(detectedId))
            {
                perGameProgress?.UpdateStatus(gameIdForUi, _loc.GetString("Progress_GeneratingCheats"));
                string cd1Folder = Path.Combine(gameRootFolder, "CD1");
                CheatGenerator.GenerateCheatTxt(detectedId, cd1Folder, _log.Info);
            }

            // Generar un ELF por cada disco
            perGameProgress?.UpdateStatus(gameIdForUi, _loc.GetString("Progress_GeneratingELF"));
            GenerateElfsForAllDiscs(detectedId, cleanTitle, gameRootFolder, discs.Count);

            if (useMetadata)
            {
                perGameProgress?.UpdateStatus(gameIdForUi, "Generando metadatos…");
                GenerateMetadataFile(detectedId, cleanTitle, dbEntry);
            }

            _notify.Success(string.Format("{0} {1}", cleanTitle, _loc.GetString("GameProcessor_ProcessedSuccessfully")));
        }

        /// <summary>
        /// Genera un ELF independiente para cada disco del juego.
        /// </summary>
        private void GenerateElfsForAllDiscs(string gameId, string title, string gameRootFolder, int totalDiscs)
        {
            for (int discNumber = 1; discNumber <= totalDiscs; discNumber++)
            {
                string cdFolder = Path.Combine(gameRootFolder, string.Format("CD{0}", discNumber));
                string? vcdPath = Directory.GetFiles(cdFolder, "*.VCD").FirstOrDefault();

                if (vcdPath == null)
                {
                    _log.Warn(string.Format("[PS1] {0} {1} {2}", _loc.GetString("GameProcessor_NoVcdFoundIn"), cdFolder, $"(Disco {discNumber})"));
                    continue;
                }

                bool ok = ElfGenerator.GeneratePs1Elf(
                    _paths.PopstarterElfPath,
                    vcdPath,
                    _paths.AppsFolder,
                    discNumber,
                    title,
                    gameId,
                    _log.Info
                );

                if (!ok)
                {
                    _notify.Error(string.Format("{0} {1} (Disco {2})", _loc.GetString("GameProcessor_ErrorGeneratingElf"), gameId, discNumber));
                    continue;
                }

                // Renombrar ELF si el usuario prefiere solo el GameID
                if (!_settings.UseTitleInElfName)
                {
                    string appsFolder = _paths.AppsFolder;
                    string oldName = $"{gameId} - {title}.ELF.NTSC";
                    string newName = $"{gameId}.ELF.NTSC";
                    string oldPath = Path.Combine(appsFolder, oldName);
                    string newPath = Path.Combine(appsFolder, newName);

                    if (File.Exists(oldPath) && !File.Exists(newPath))
                    {
                        File.Move(oldPath, newPath);
                        _log.Info($"[ELF] Renombrado {oldName} -> {newName}");
                    }
                }

                _log.Info(string.Format("[PS1] ELF {0} (Disco {1}) {2}", _loc.GetString("GameProcessor_GeneratedFor"), discNumber, gameId));
            }
        }

        private async Task ProcessPS2Async(string isoPath, string gameIdForUi, ProgressViewModel? perGameProgress, CancellationToken ct)
        {
            string originalName = Path.GetFileNameWithoutExtension(isoPath);
            _log.Info(string.Format("[PS2] {0}: {1}", _loc.GetString("GameProcessor_Processing"), originalName));

            string detectedId = GameIdDetector.DetectGameId(isoPath) ?? GameIdDetector.DetectFromName(originalName) ?? "";
            if (string.IsNullOrWhiteSpace(detectedId))
            {
                _notify.Warning(string.Format("{0} {1}", _loc.GetString("GameProcessor_CouldNotDetectIdCopying"), originalName));
                detectedId = originalName.Replace(" ", "_");
            }

            bool useDb = _settings.UseDatabase && _auto.ShouldUseDatabase();
            bool useCovers = _settings.UseCovers && _auto.ShouldDownloadCovers();
            bool useMetadata = _settings.UseMetadata;
            string cleanTitle = NameCleanerBase.CleanTitleOnly(originalName);
            GameEntry dbEntry = null;

            if (useDb && GameDatabase.TryGetEntry(detectedId, out var entry))
            {
                dbEntry = entry;
                if (!string.IsNullOrWhiteSpace(dbEntry.Name))
                {
                    cleanTitle = dbEntry.Name;
                    _log.Info(string.Format("[DB] {0}: {1}", _loc.GetString("GameProcessor_OfficialNameFoundPs2"), cleanTitle));
                }

                if (useCovers && dbEntry?.CoverUrl != null)
                {
                    string artFolder = Path.Combine(_paths.DvdFolder, "ART");
                    Directory.CreateDirectory(artFolder);
                    perGameProgress?.UpdateStatus(gameIdForUi, _loc.GetString("Progress_DownloadingCover"));
                    await _coverSemaphore.WaitAsync(ct);
                    try
                    {
                        string art = await ArtDownloader.DownloadArtAsync(detectedId, dbEntry.CoverUrl, artFolder, _log.Info);
                        if (art != null) _log.Info(string.Format("[COVER] PS2 ART {0} -> {1}", _loc.GetString("GameProcessor_Generated"), art));
                    }
                    finally { _coverSemaphore.Release(); }
                }
            }

            Directory.CreateDirectory(_paths.DvdFolder);
            perGameProgress?.UpdateStatus(gameIdForUi, _loc.GetString("Progress_CopyingISO"));
            string dest = Path.Combine(_paths.DvdFolder, string.Format("{0}.ISO", cleanTitle));
            ct.ThrowIfCancellationRequested();
            File.Copy(isoPath, dest, true);
            _log.Info(string.Format("[PS2] {0} -> {1}", _loc.GetString("GameProcessor_CopiedIso"), dest));

            if (useMetadata)
            {
 