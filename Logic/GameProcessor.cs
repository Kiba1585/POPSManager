using POPSManager.Models;
using POPSManager.Services;
using POPSManager.Settings;
using POPSManager.Logic;
using POPSManager.Logic.Cheats;
using POPSManager.Logic.Covers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POPSManager.Logic
{
    /// <summary>
    /// Procesador principal de juegos PS1 (VCD) y PS2 (ISO).
    /// Versión Ultra Pro Max: multidisco antes de convertir, nombres reales,
    /// estructura OPL automática, covers opcionales, validación avanzada.
    /// Integrado con AutomationEngine.
    /// </summary>
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
        //  PROCESAR CARPETA (ASYNC)
        // ============================================================
        public async Task ProcessFolderAsync(string folder, CancellationToken ct = default)
        {
            var files = Directory.GetFiles(folder)
                .Where(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f)
                .ToArray();

            if (files.Length == 0)
            {
                _notify.Warning("No se encontraron archivos VCD o ISO.");
                return;
            }

            // Agrupación por juego real (antes de convertir)
            var groups = GroupByRealGame(files);

            int index = 0;
            int total = groups.Count;

            foreach (var group in groups)
            {
                ct.ThrowIfCancellationRequested();

                index++;
                int progressValue = (int)((index / (double)total) * 100);
                _progress.SetProgress(progressValue);
                _progress.SetStatus($"Procesando {group.Key}");

                try
                {
                    var discs = group.Value;

                    if (discs.Any(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase)))
                    {
                        discs = discs.Where(ValidateVcd).ToList();
                        if (discs.Count == 0)
                        {
                            _log.Warn($"[PS1] Todos los VCD de {group.Key} fueron inválidos.");
                            continue;
                        }

                        await Task.Run(() => ProcessPS1Group(group.Key, discs), ct);
                    }
                    else
                    {
                        var iso = discs.First();
                        if (!ValidateIso(iso))
                        {
                            _log.Warn($"[PS2] ISO inválido: {iso}");
                            continue;
                        }

                        await Task.Run(() => ProcessPS2(iso), ct);
                    }
                }
                catch (OperationCanceledException)
                {
                    _log.Warn($"Procesamiento cancelado en {group.Key}.");
                    _notify.Warning("Procesamiento cancelado.");
                    throw;
                }
                catch (Exception ex)
                {
                    _log.Error($"Error procesando {group.Key}: {ex.Message}");
                    _notify.Error($"Error procesando {group.Key}");
                }
            }

            _progress.SetStatus("Completado");
        }

        // ============================================================
        //  AGRUPAR POR JUEGO REAL (ANTES DE CONVERTIR)
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

        // ============================================================
        //  VALIDACIÓN VCD / ISO
        // ============================================================
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
        //  PROCESAR PS1 MULTIDISCO
        // ============================================================
        private void ProcessPS1Group(string baseName, List<string> discs)
        {
            _log.Info($"[PS1] Procesando grupo: {baseName}");

            // Ordenar discos por número real
            discs = discs.OrderBy(d =>
            {
                string fileName = Path.GetFileNameWithoutExtension(d);
                NameCleanerBase.Clean(fileName, out string? cdTag);
                return MultiDiscManager.ExtractDiscNumber(cdTag ?? fileName);
            }).ToList();

            // Detectar ID real
            string firstDisc = discs.First();
            string? detectedId = GameIdDetector.DetectGameId(firstDisc)
                                ?? GameIdDetector.DetectFromName(baseName);

            if (string.IsNullOrWhiteSpace(detectedId))
            {
                _notify.Warning($"No se pudo detectar ID para {baseName}");
                return;
            }

            // Flags de automatización
            bool useDb = _settings.UseDatabase && _auto.ShouldUseDatabase();
            bool useCovers = _settings.UseCovers && _auto.ShouldDownloadCovers();
            bool genCheats = _auto.ShouldGenerateCheats();
            bool handleMultiDisc = _auto.ShouldHandleMultiDisc();

            // Nombre real del juego
            string cleanTitle = NameCleanerBase.CleanTitleOnly(baseName);

            // Base de datos opcional
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

            // Covers opcionales
            if (useCovers && dbEntry?.CoverUrl != null)
            {
                string artFolder = Path.Combine(_paths.PopsFolder, "ART");
                Directory.CreateDirectory(artFolder);

                string? art = ArtDownloader.DownloadArt(
                    detectedId, dbEntry.CoverUrl, artFolder, _log.Info);

                if (art != null)
                    _log.Info($"[COVER] PS1 ART generado → {art}");
            }

            // Crear carpeta del juego
            string gameRootFolder = Path.Combine(_paths.PopsFolder, $"{cleanTitle}");
            Directory.CreateDirectory(gameRootFolder);

            int discNumber = 1;
            List<string> discPaths = new();

            foreach (var disc in discs)
            {
                try
                {
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

            // DISCS.TXT (solo si automatización lo permite)
            if (handleMultiDisc)
            {
                MultiDiscManager.GenerateDiscsTxt(_paths.PopsFolder, detectedId, discPaths, _log.Info);
            }
            else
            {
                _log.Info("[PS1] Automatización multidisco desactivada. No se genera DISCS.TXT.");
            }

            // CHEAT.TXT (solo PAL + automatización)
            if (genCheats && GameIdDetector.IsPalRegion(detectedId))
            {
                string cd1Folder = Path.Combine(gameRootFolder, "CD1");
                CheatGenerator.GenerateCheatTxt(detectedId, cd1Folder, _log.Info);
            }

            // ELF
            GenerateElfForDisc1(detectedId, cleanTitle, gameRootFolder);

            _notify.Success($"{cleanTitle} procesado correctamente.");
        }

        // ============================================================
        //  GENERAR ELF PARA CD1
        // ============================================================
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
        //  PROCESAR PS2
        // ============================================================
        private void ProcessPS2(string isoPath)
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

            // Flags de automatización
            bool useDb = _settings.UseDatabase && _auto.ShouldUseDatabase();
            bool useCovers = _settings.UseCovers && _auto.ShouldDownloadCovers();

            string cleanTitle = NameCleanerBase.CleanTitleOnly(originalName);

            // Base de datos opcional
            GameEntry? dbEntry = null;
            if (useDb && GameDatabase.TryGetEntry(detectedId, out var entry))
            {
                dbEntry = entry;
                if (!string.IsNullOrWhiteSpace(dbEntry.Name))
                {
                    cleanTitle = dbEntry.Name;
                    _log.Info($"[DB] Nombre oficial PS2 encontrado: {cleanTitle}");
                }

                // Cover PS2
                if (useCovers && dbEntry?.CoverUrl != null)
                {
                    string artFolder = Path.Combine(_paths.DvdFolder, "ART");
                    Directory.CreateDirectory(artFolder);

                    string? art = ArtDownloader.DownloadArt(
                        detectedId, dbEntry.CoverUrl, artFolder, _log.Info);

                    if (art != null)
                        _log.Info($"[COVER] PS2 ART generado → {art}");
                }
            }

            Directory.CreateDirectory(_paths.DvdFolder);

            string dest = Path.Combine(
                _paths.DvdFolder,
                $"{cleanTitle}.ISO"
            );

            File.Copy(isoPath, dest, true);
            _log.Info($"[PS2] Copiado ISO → {dest}");
            _notify.Success($"{cleanTitle} copiado a DVD correctamente.");
        }
    }
}
