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

            // Determinar si usamos carpeta temporal
            bool useTemp = _settings.UseTempFolderForConversion && IsRemovableDrive(_paths.RootFolder);
            string tempRoot = useTemp ? _paths.TempFolder : _paths.RootFolder;
            if (useTemp)
            {
                Directory.CreateDirectory(tempRoot);
                _log.Info($"[GameProcessor] Usando carpeta temporal: {tempRoot}");
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
                                await ProcessPS1GroupAsync(baseTitle, valid, gameIdForUi, perGameProgress, token, tempRoot);
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
                                await ProcessPS2Async(iso, gameIdForUi, perGameProgress, token, tempRoot);
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

                // Copiar todo al destino final si se usó carpeta temporal
                if (useTemp)
                {
                    _progress.SetStatus("Copiando al destino final...");
                    await Task.Run(() => CopyDirectoryRecursive(tempRoot, _paths.RootFolder, _log.Info));
                    // Limpiar carpeta temporal
                    try { Directory.Delete(tempRoot, true); } catch { }
                    _log.Info("[GameProcessor] Copia al destino final completada.");
                }

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

        private static bool IsRemovableDrive(string path)
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(path)!);
                return drive.DriveType == DriveType.Removable;
            }
            catch { return false; }
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

        private async Task ProcessPS1GroupAsync(string baseName, List<string> discs, string gameIdForUi,
            ProgressViewModel? perGameProgress, CancellationToken ct, string workingRoot)
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

            // Las rutas ahora usan workingRoot en lugar de _paths.RootFolder
            string popsFolder = Path.Combine(workingRoot, "POPS");
            string appsFolder = Path.Combine(workingRoot, "APPS");
            string artFolder = Path.Combine(popsFolder, "ART");
            string cfgFolder = Path.Combine(workingRoot, "CFG");

            if (useCovers && dbEntry?.CoverUrl != null)
            {
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

            string gameRootFolder = Path.Combine(popsFolder, cleanTitle);
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
                MultiDiscManager.GenerateDiscsTxt(popsFolder, detectedId, discPaths, _log.Info, _auto);
            }

            if (genCheats && GameIdDetector.IsPalRegion(detectedId))
            {
                perGameProgress?.UpdateStatus(gameIdForUi, _loc.GetString("Progress_GeneratingCheats"));
                string cd1Folder = Path.Combine(gameRootFolder, "CD1");
                CheatGenerator.GenerateCheatTxt(detectedId, cd1Folder, _log.Info);
            }

            perGameProgress?.UpdateStatus(gameIdForUi, _loc.GetString("Progress_GeneratingELF"));
            GenerateElfForDisc1(detectedId, cleanTitle, gameRootFolder, appsFolder);

            if (useMetadata)
            {
                perGameProgress?.UpdateStatus(gameIdForUi, "Generando metadatos…");
                GenerateMetadataFile(detectedId, cleanTitle, dbEntry, cfgFolder);
            }

            _notify.Success(string.Format("{0} {1}", cleanTitle, _loc.GetString("GameProcessor_ProcessedSuccessfully")));
        }

        private void GenerateElfForDisc1(string gameId, string title, string gameRootFolder, string appsFolder)
        {
            string cd1Folder = Path.Combine(gameRootFolder, "CD1");
            string vcdPath = Directory.GetFiles(cd1Folder, "*.VCD").FirstOrDefault();
            if (vcdPath == null)
            {
                _log.Warn(string.Format("[PS1] {0} {1}", _loc.GetString("GameProcessor_NoVcdFoundIn"), cd1Folder));
                return;
            }
            bool ok = ElfGenerator.GeneratePs1Elf(_paths.PopstarterElfPath, vcdPath, appsFolder, 1, title, gameId, _log.Info);
            if (!ok)
            {
                _notify.Error(string.Format("{0} {1}", _loc.GetString("GameProcessor_ErrorGeneratingElf"), gameId));
                return;
            }

            if (!_settings.UseTitleInElfName)
            {
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

            _log.Info(string.Format("[PS1] ELF {0} {1}", _loc.GetString("GameProcessor_GeneratedFor"), gameId));
        }

        private async Task ProcessPS2Async(string isoPath, string gameIdForUi,
            ProgressViewModel? perGameProgress, CancellationToken ct, string workingRoot)
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

            string dvdFolder = Path.Combine(workingRoot, "DVD");
            string artFolder = Path.Combine(dvdFolder, "ART");
            string cfgFolder = Path.Combine(workingRoot, "CFG");

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

            Directory.CreateDirectory(dvdFolder);
            perGameProgress?.UpdateStatus(gameIdForUi, _loc.GetString("Progress_CopyingISO"));
            string dest = Path.Combine(dvdFolder, string.Format("{0}.ISO", cleanTitle));
            ct.ThrowIfCancellationRequested();
            File.Copy(isoPath, dest, true);
            _log.Info(string.Format("[PS2] {0} -> {1}", _loc.GetString("GameProcessor_CopiedIso"), dest));

            if (useMetadata)
            {
                perGameProgress?.UpdateStatus(gameIdForUi, "Generando metadatos…");
                GenerateMetadataFile(detectedId, cleanTitle, dbEntry, cfgFolder);
            }

            _notify.Success(string.Format("{0} {1}", cleanTitle, _loc.GetString("GameProcessor_CopiedToDvdSuccessfully")));
        }

private void GenerateMetadataFile(string gameId, string title, GameEntry? dbEntry, string cfgFolder)
{
    try
    {
        Directory.CreateDirectory(cfgFolder);
        string cfgPath = Path.Combine(cfgFolder, $"{gameId}.cfg");

        // Determinar la descripción (evitamos el ternario en string interpolada)
        string description;
        if (dbEntry?.CheatFixes != null)
            description = "Fixes disponibles";
        else
            description = "Sin descripción";

        var lines = new List<string>
        {
            $"Title={title}",
            $"Description={description}",
            $"Release={dbEntry?.Year.ToString() ?? "2000"}",
            $"Genre={(dbEntry?.Tags != null && dbEntry.Tags.Length > 0 ? dbEntry.Tags[0] : "Action")}",
            "Players=1",
            $"Developer={dbEntry?.Publisher ?? "Desconocido"}",
            "Rating=ESRB=E"
        };

        File.WriteAllLines(cfgPath, lines);
        _log.Info($"[METADATA] Archivo CFG generado -> {cfgPath}");
    }
    catch (Exception ex)
    {
        _log.Error($"[METADATA] Error generando CFG para {gameId}: {ex.Message}");
    }
}

        private void CopyCustomFolderContents(string sourceFolder, string folderName, Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder))
            {
                log(string.Format("[Copy] No se encontro carpeta {0} personalizada o no existe.", folderName));
                return;
            }
            string destFolder = Path.Combine(_paths.RootFolder, folderName);
            try
            {
                Directory.CreateDirectory(destFolder);
                foreach (var file in Directory.GetFiles(sourceFolder))
                {
                    string destFile = Path.Combine(destFolder, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                    log(string.Format("[Copy] {0} -> {1}", file, destFile));
                }
                foreach (var dir in Directory.GetDirectories(sourceFolder))
                {
                    string destDir = Path.Combine(destFolder, Path.GetFileName(dir));
                    CopyDirectoryRecursive(dir, destDir, log);
                }
                log(string.Format("[Copy] Contenido de {0} copiado a {1}", folderName, destFolder));
            }
            catch (Exception ex) { log(string.Format("[ERROR] Copiando {0}: {1}", folderName, ex.Message)); }
        }

        private void CopyDirectoryRecursive(string source, string dest, Action<string> log)
        {
            Directory.CreateDirectory(dest);
            foreach (var file in Directory.GetFiles(source))
            {
                string destFile = Path.Combine(dest, Path.GetFileName(file));
                File.Copy(file, destFile, true);
                log(string.Format("[Copy] {0} -> {1}", file, destFile));
            }
            foreach (var dir in Directory.GetDirectories(source))
                CopyDirectoryRecursive(dir, Path.Combine(dest, Path.GetFileName(dir)), log);
        }
    }
}