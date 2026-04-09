using POPSManager.Models;
using POPSManager.Services;
using POPSManager.Logic;
using POPSManager.Logic.Covers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace POPSManager.Logic
{
    public class GameProcessor
    {
        private readonly ProgressService progress;
        private readonly LoggingService logService;
        private readonly NotificationService notifications;
        private readonly PathsService paths;

        private readonly bool useDatabase;
        private readonly bool useCovers;

        public GameProcessor(
            ProgressService progress,
            LoggingService logService,
            NotificationService notifications,
            PathsService paths,
            bool useDatabase = false,
            bool useCovers = false)
        {
            this.progress = progress;
            this.logService = logService;
            this.notifications = notifications;
            this.paths = paths;
            this.useDatabase = useDatabase;
            this.useCovers = useCovers;
        }

        // ============================================================
        //  PROCESAR CARPETA COMPLETA (PS1 + PS2)
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
                notifications.Warning("No se encontraron archivos VCD o ISO.");
                return;
            }

            var groups = GroupMultiDisc(files);

            int index = 0;
            int total = groups.Count;

            foreach (var group in groups)
            {
                index++;
                int progressValue = (int)((index / (double)total) * 100);

                progress.SetProgress(progressValue);
                progress.SetStatus($"Procesando {group.Key}");

                try
                {
                    var discs = group.Value;

                    // PS1
                    if (discs.Any(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase)))
                    {
                        discs = discs.Where(ValidateVcd).ToList();
                        if (discs.Count == 0)
                        {
                            logService.Warn($"[PS1] Todos los VCD de {group.Key} fueron inválidos.");
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
                            logService.Warn($"[PS2] ISO inválido: {iso}");
                            continue;
                        }

                        ProcessPS2(iso);
                    }
                }
                catch (Exception ex)
                {
                    logService.Error($"Error procesando {group.Key}: {ex.Message}");
                    notifications.Error($"Error procesando {group.Key}");
                }
            }

            progress.SetStatus("Completado");
        }

        // ============================================================
        //  VALIDACIÓN VCD / ISO
        // ============================================================
        private bool ValidateVcd(string vcdPath)
        {
            if (!IntegrityValidator.Validate(vcdPath))
            {
                logService.Warn($"[PS1] VCD inválido: {vcdPath}");
                notifications.Warning($"VCD inválido: {Path.GetFileName(vcdPath)}");
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
                    notifications.Warning($"ISO sospechoso o demasiado pequeño: {Path.GetFileName(isoPath)}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                logService.Error($"[PS2] Error validando ISO: {ex.Message}");
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
            logService.Info($"[PS1] Procesando grupo: {baseName}");

            // Ordenar discos por número usando MultiDiscManager Ultra‑Pro
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
                notifications.Warning($"No se pudo detectar ID para {baseName}");
                return;
            }

            // Obtener nombre del juego
            string cleanTitle = NameCleanerBase.CleanTitleOnly(baseName);

            // ============================================================
            //  BASE DE DATOS
            // ============================================================
            GameEntry? dbEntry = null;

            if (useDatabase && GameDatabase.TryGetEntry(detectedId, out var entry))
            {
                dbEntry = entry;

                if (dbEntry != null && !string.IsNullOrWhiteSpace(dbEntry.Name))
                {
                    cleanTitle = dbEntry.Name;
                    logService.Info($"[DB] Nombre oficial encontrado: {cleanTitle}");
                }
            }

            // ============================================================
            //  COVER PS1
            // ============================================================
            if (useCovers && dbEntry?.CoverUrl != null)
            {
                string artFolder = Path.Combine(paths.PopsFolder, "ART");
                Directory.CreateDirectory(artFolder);

                string? art = ArtDownloader.DownloadArt(detectedId, dbEntry.CoverUrl, artFolder, logService.Info);

                if (art != null)
                    logService.Info($"[COVER] PS1 ART generado → {art}");
            }

            // ============================================================
            //  ESTRUCTURA FINAL PS1
            // ============================================================
            string gameRootFolder = Path.Combine(paths.PopsFolder, $"{detectedId} - {cleanTitle}");
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

                    logService.Info($"[PS1] Copiado disco {discNumber} → {destVcd}");
                }
                catch (Exception ex)
                {
                    logService.Error($"[PS1] Error copiando disco {discNumber}: {ex.Message}");
                }

                discNumber++;
            }

            // ============================================================
            //  DISCS.TXT
            // ============================================================
            MultiDiscManager.GenerateDiscsTxt(paths.PopsFolder, detectedId, discPaths, logService.Info);

            // ============================================================
            //  CHEAT.TXT (solo PAL)
            // ============================================================
            if (GameIdDetector.IsPalRegion(detectedId))
            {
                string cd1Folder = Path.Combine(gameRootFolder, "CD1");
                CheatGenerator.GenerateCheatTxt(detectedId, cd1Folder, logService.Info);
            }

            // ============================================================
            //  ELF (PS1)
            // ============================================================
            GenerateElfForDisc1(detectedId, cleanTitle, gameRootFolder);

            notifications.Success($"{cleanTitle} procesado correctamente.");
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
                logService.Warn($"[PS1] No se encontró VCD en {cd1Folder}");
                return;
            }

            bool ok = ElfGenerator.GeneratePs1Elf(
                paths.PopstarterElfPath,
                vcdPath,
                paths.AppsFolder,
                1,
                title,
                gameId,
                logService.Info
            );

            if (!ok)
            {
                notifications.Error($"Error generando ELF para {gameId}");
                return;
            }

            logService.Info($"[PS1] ELF generado para {gameId}");
        }

        // ============================================================
        //  PROCESAR PS2 (ISO)
        // ============================================================
        private void ProcessPS2(string isoPath)
        {
            string originalName = Path.GetFileNameWithoutExtension(isoPath);
            logService.Info($"[PS2] Procesando: {originalName}");

            string? detectedId = GameIdDetector.DetectGameId(isoPath);

            if (string.IsNullOrWhiteSpace(detectedId))
                detectedId = GameIdDetector.DetectFromName(originalName);

            if (string.IsNullOrWhiteSpace(detectedId))
            {
                notifications.Warning($"No se pudo detectar ID para {originalName}. Se copiará sin renombrar.");
                detectedId = originalName.Replace(" ", "_");
            }

            string cleanTitle = NameCleanerBase.CleanTitleOnly(originalName);

            // ============================================================
            //  BASE DE DATOS PS2
            // ============================================================
            GameEntry? dbEntry = null;

            if (useDatabase && GameDatabase.TryGetEntry(detectedId, out var entry))
            {
                dbEntry = entry;

                if (dbEntry != null && !string.IsNullOrWhiteSpace(dbEntry.Name))
                {
                    cleanTitle = dbEntry.Name;
                    logService.Info($"[DB] Nombre oficial PS2 encontrado: {cleanTitle}");
                }

                // COVER PS2
                if (useCovers && dbEntry?.CoverUrl != null)
                {
                    string artFolder = Path.Combine(paths.DvdFolder, "ART");
                    Directory.CreateDirectory(artFolder);

                    string? art = ArtDownloader.DownloadArt(detectedId, dbEntry.CoverUrl, artFolder, logService.Info);

                    if (art != null)
                        logService.Info($"[COVER] PS2 ART generado → {art}");
                }
            }

            Directory.CreateDirectory(paths.DvdFolder);

            string dest = Path.Combine(
                paths.DvdFolder,
                NameFormatter.BuildPs2IsoName(isoPath, detectedId, cleanTitle)
            );

            File.Copy(isoPath, dest, true);

            logService.Info($"[PS2] Copiado ISO → {dest}");

            notifications.Success($"{cleanTitle} copiado a DVD correctamente.");
        }
    }
}
