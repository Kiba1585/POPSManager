using POPSManager.Models;
using POPSManager.Services;
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
    /// Soporta ejecución síncrona y asíncrona con cancelación.
    /// </summary>
    public class GameProcessor
    {
        // ============================================================
        //  DEPENDENCIAS (inyectadas vía DI)
        // ============================================================
        private readonly ProgressService _progress;
        private readonly LoggingService _logService;
        private readonly NotificationService _notifications;
        private readonly PathsService _paths;
        private readonly CheatSettingsService _cheatSettings;
        private readonly CheatManagerService _cheatManager;

        private readonly bool _useDatabase;
        private readonly bool _useCovers;

        // ============================================================
        //  CONSTRUCTOR (compatible con AppServices DI)
        // ============================================================
        public GameProcessor(
            ProgressService progress,
            LoggingService logService,
            NotificationService notifications,
            PathsService paths,
            CheatSettingsService cheatSettings,
            CheatManagerService cheatManager,
            bool useDatabase = false,
            bool useCovers = false)
        {
            _progress = progress;
            _logService = logService;
            _notifications = notifications;
            _paths = paths;
            _cheatSettings = cheatSettings;
            _cheatManager = cheatManager;
            _useDatabase = useDatabase;
            _useCovers = useCovers;
        }

        // ============================================================
        //  PROCESAR CARPETA — VERSIÓN ASYNC (NUEVO)
        //  Permite cancelación desde la UI vía CancellationToken
        // ============================================================
        public async Task ProcessFolderAsync(string folder, CancellationToken ct = default)
        {
            var files = Directory.GetFiles(folder)
                                 .Where(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                                 .OrderBy(f => f)
                                 .ToArray();

            if (files.Length == 0)
            {
                _notifications.Warning("No se encontraron archivos VCD o ISO.");
                return;
            }

            var groups = GroupMultiDisc(files);

            int index = 0;
            int total = groups.Count;

            foreach (var group in groups)
            {
                // ── Verificar cancelación antes de cada grupo ──
                ct.ThrowIfCancellationRequested();

                index++;
                int progressValue = (int)((index / (double)total) * 100);

                _progress.SetProgress(progressValue);
                _progress.SetStatus($"Procesando {group.Key}");

                try
                {
                    var discs = group.Value;

                    // PS1
                    if (discs.Any(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase)))
                    {
                        discs = discs.Where(ValidateVcd).ToList();
                        if (discs.Count == 0)
                        {
                            _logService.Warn($"[PS1] Todos los VCD de {group.Key} fueron inválidos.");
                            continue;
                        }

                        // Ejecutar en background para no bloquear UI
                        await Task.Run(() => ProcessPS1Group(group.Key, discs), ct);
                    }
                    else
                    {
                        // PS2
                        var iso = discs.First();
                        if (!ValidateIso(iso))
                        {
                            _logService.Warn($"[PS2] ISO inválido: {iso}");
                            continue;
                        }

                        await Task.Run(() => ProcessPS2(iso), ct);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logService.Warn($"Procesamiento cancelado por el usuario en {group.Key}.");
                    _notifications.Warning("Procesamiento cancelado.");
                    throw; // Re-lanzar para que el llamador sepa
                }
                catch (Exception ex)
                {
                    _logService.Error($"Error procesando {group.Key}: {ex.Message}");
                    _notifications.Error($"Error procesando {group.Key}");
                }
            }

            _progress.SetStatus("Completado");
        }

        // ============================================================
        //  PROCESAR CARPETA — VERSIÓN SÍNCRONA (COMPATIBILIDAD)
        // ============================================================
        public void ProcessFolder(string folder)
        {
            var files = Directory.GetFiles(folder)
                                 .Where(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                                 .OrderBy(f => f)
                                 .ToArray();

            if (files.Length == 0)
            {
                _notifications.Warning("No se encontraron archivos VCD o ISO.");
                return;
            }

            var groups = GroupMultiDisc(files);

            int index = 0;
            int total = groups.Count;

            foreach (var group in groups)
            {
                index++;
                int progressValue = (int)((index / (double)total) * 100);

                _progress.SetProgress(progressValue);
                _progress.SetStatus($"Procesando {group.Key}");

                try
                {
                    var discs = group.Value;

                    // PS1
                    if (discs.Any(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase)))
                    {
                        discs = discs.Where(ValidateVcd).ToList();
                        if (discs.Count == 0)
                        {
                            _logService.Warn($"[PS1] Todos los VCD de {group.Key} fueron inválidos.");
                            continue;
                        }

                        ProcessPS1Group(group.Key, discs);
                    }
                    else
                    {
                        // PS2
                        var iso = discs.First();
                        if (!ValidateIso(iso))
                        {
                            _logService.Warn($"[PS2] ISO inválido: {iso}");
                            continue;
                        }

                        ProcessPS2(iso);
                    }
                }
                catch (Exception ex)
                {
                    _logService.Error($"Error procesando {group.Key}: {ex.Message}");
                    _notifications.Error($"Error procesando {group.Key}");
                }
            }

            _progress.SetStatus("Completado");
        }

        // ============================================================
        //  VALIDACIÓN VCD / ISO
        // ============================================================
        private bool ValidateVcd(string vcdPath)
        {
            if (!IntegrityValidator.Validate(vcdPath))
            {
                _logService.Warn($"[PS1] VCD inválido: {vcdPath}");
                _notifications.Warning($"VCD inválido: {Path.GetFileName(vcdPath)}");
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
                    _notifications.Warning($"ISO sospechoso o demasiado pequeño: {Path.GetFileName(isoPath)}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logService.Error($"[PS2] Error validando ISO: {ex.Message}");
                return false;
            }
        }

        // ============================================================
        //  AGRUPAR MULTIDISCO
        // ============================================================
        private Dictionary<string, List<string>> GroupMultiDisc(string[] files)
        {
            var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string baseName = NameCleanerBase.CleanTitleOnly(name);

                if (!groups.ContainsKey(baseName))
                    groups[baseName] = new List<string>();

                groups[baseName].Add(file);
            }

            return groups;
        }

        // ============================================================
        //  PROCESAR PS1 (MULTIDISCO)
        // ============================================================
        private void ProcessPS1Group(string baseName, List<string> discs)
        {
            _logService.Info($"[PS1] Procesando grupo: {baseName}");

            // Ordenar discos por número usando MultiDiscManager Ultra-Pro
            discs = discs.OrderBy(d =>
            {
                string fileName = Path.GetFileNameWithoutExtension(d);
                NameCleanerBase.Clean(fileName, out string? cdTag);
                return MultiDiscManager.ExtractDiscNumber(cdTag ?? fileName);
            }).ToList();

            // Detectar ID real desde el VCD
            string firstDisc = discs.First();
            string? detectedId = GameIdDetector.DetectGameId(firstDisc);

            if (string.IsNullOrWhiteSpace(detectedId))
                detectedId = GameIdDetector.DetectFromName(baseName);

            if (string.IsNullOrWhiteSpace(detectedId))
            {
                _notifications.Warning($"No se pudo detectar ID para {baseName}");
                return;
            }

            // Obtener nombre del juego
            string cleanTitle = NameCleanerBase.CleanTitleOnly(baseName);

            // ── BASE DE DATOS ──
            GameEntry? dbEntry = null;

            if (_useDatabase && GameDatabase.TryGetEntry(detectedId, out var entry))
            {
                dbEntry = entry;

                if (dbEntry != null && !string.IsNullOrWhiteSpace(dbEntry.Name))
                {
                    cleanTitle = dbEntry.Name;
                    _logService.Info($"[DB] Nombre oficial encontrado: {cleanTitle}");
                }
            }

            // ── COVER PS1 ──
            if (_useCovers && dbEntry?.CoverUrl != null)
            {
                string artFolder = Path.Combine(_paths.PopsFolder, "ART");
                Directory.CreateDirectory(artFolder);

                string? art = ArtDownloader.DownloadArt(detectedId, dbEntry.CoverUrl, artFolder, _logService.Info);

                if (art != null)
                    _logService.Info($"[COVER] PS1 ART generado → {art}");
            }

            // ── ESTRUCTURA FINAL PS1 ──
            string gameRootFolder = Path.Combine(_paths.PopsFolder, $"{detectedId} - {cleanTitle}");
            Directory.CreateDirectory(gameRootFolder);

            int discNumber = 1;
            List<string> discPaths = new();

            foreach (var disc in discs)
            {
                try
                {
                    string discFolder = Path.Combine(gameRootFolder, $"CD{discNumber}");
                    Directory.CreateDirectory(discFolder);

                    string finalFileName = NameFormatter.BuildPs1VcdName(
                        disc,
                        discNumber,
                        detectedId,
                        cleanTitle
                    );

                    string destVcd = Path.Combine(discFolder, finalFileName);

                    File.Copy(disc, destVcd, true);
                    discPaths.Add(destVcd);

                    _logService.Info($"[PS1] Copiado disco {discNumber} → {destVcd}");
                }
                catch (Exception ex)
                {
                    _logService.Error($"[PS1] Error copiando disco {discNumber}: {ex.Message}");
                }

                discNumber++;
            }

            // ── DISCS.TXT ──
            MultiDiscManager.GenerateDiscsTxt(_paths.PopsFolder, detectedId, discPaths, _logService.Info);

            // ── CHEAT.TXT (solo PAL) ──
            if (GameIdDetector.IsPalRegion(detectedId))
            {
                string cd1Folder = Path.Combine(gameRootFolder, "CD1");
                CheatGenerator.GenerateCheatTxt(detectedId, cd1Folder, _logService.Info);
            }

            // ── ELF (PS1) ──
            GenerateElfForDisc1(detectedId, cleanTitle, gameRootFolder);

            _notifications.Success($"{cleanTitle} procesado correctamente.");
        }

        // ============================================================
        //  GENERAR ELF PARA CD1 (PS1)
        // ============================================================
        private void GenerateElfForDisc1(string gameId, string title, string gameRootFolder)
        {
            string cd1Folder = Path.Combine(gameRootFolder, "CD1");

            string? vcdPath = Directory.GetFiles(cd1Folder, "*.VCD").FirstOrDefault();
            if (vcdPath == null)
            {
                _logService.Warn($"[PS1] No se encontró VCD en {cd1Folder}");
                return;
            }

            bool ok = ElfGenerator.GeneratePs1Elf(
                _paths.PopstarterElfPath,
                vcdPath,
                _paths.AppsFolder,
                1,
                title,
                gameId,
                _logService.Info
            );

            if (!ok)
            {
                _notifications.Error($"Error generando ELF para {gameId}");
                return;
            }

            _logService.Info($"[PS1] ELF generado para {gameId}");
        }

        // ============================================================
        //  PROCESAR PS2 (ISO)
        // ============================================================
        private void ProcessPS2(string isoPath)
        {
            string originalName = Path.GetFileNameWithoutExtension(isoPath);
            _logService.Info($"[PS2] Procesando: {originalName}");

            string? detectedId = GameIdDetector.DetectGameId(isoPath);

            if (string.IsNullOrWhiteSpace(detectedId))
                detectedId = GameIdDetector.DetectFromName(originalName);

            if (string.IsNullOrWhiteSpace(detectedId))
            {
                _notifications.Warning($"No se pudo detectar ID para {originalName}. Se copiará sin renombrar.");
                detectedId = originalName.Replace(" ", "_");
            }

            string cleanTitle = NameCleanerBase.CleanTitleOnly(originalName);

            // ── BASE DE DATOS PS2 ──
            GameEntry? dbEntry = null;

            if (_useDatabase && GameDatabase.TryGetEntry(detectedId, out var entry))
            {
                dbEntry = entry;

                if (dbEntry != null && !string.IsNullOrWhiteSpace(dbEntry.Name))
                {
                    cleanTitle = dbEntry.Name;
                    _logService.Info($"[DB] Nombre oficial PS2 encontrado: {cleanTitle}");
                }

                // COVER PS2
                if (_useCovers && dbEntry?.CoverUrl != null)
                {
                    string artFolder = Path.Combine(_paths.DvdFolder, "ART");
                    Directory.CreateDirectory(artFolder);

                    string? art = ArtDownloader.DownloadArt(detectedId, dbEntry.CoverUrl, artFolder, _logService.Info);

                    if (art != null)
                        _logService.Info($"[COVER] PS2 ART generado → {art}");
                }
            }

            Directory.CreateDirectory(_paths.DvdFolder);

            string dest = Path.Combine(
                _paths.DvdFolder,
                NameFormatter.BuildPs2IsoName(isoPath, detectedId, cleanTitle)
            );

            File.Copy(isoPath, dest, true);

            _logService.Info($"[PS2] Copiado ISO → {dest}");

            _notifications.Success($"{cleanTitle} copiado a DVD correctamente.");
        }
    }
}
