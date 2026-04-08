using POPSManager.Models;
using POPSManager.Services;
using POPSManager.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace POPSManager.Logic
{
    public class GameProcessor
    {
        private readonly Action<int> updateProgress;
        private readonly Action<string> updateSpinner;
        private readonly Action<string> log;
        private readonly Action<UiNotification> notify;
        private readonly PathsService paths;

        private readonly bool useDatabase = false;
        private readonly bool useCovers = false;

        public GameProcessor(
            Action<int> updateProgress,
            Action<string> updateSpinner,
            Action<string> log,
            Action<UiNotification> notify,
            PathsService paths,
            bool useDatabase = false,
            bool useCovers = false)
        {
            this.updateProgress = updateProgress;
            this.updateSpinner = updateSpinner;
            this.log = log;
            this.notify = notify;
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
                notify(new UiNotification(NotificationType.Warning,
                    "No se encontraron archivos VCD o ISO."));
                return;
            }

            var groups = GroupMultiDisc(files);

            int index = 0;
            int total = groups.Count;

            foreach (var group in groups)
            {
                index++;
                int progress = (int)((index / (double)total) * 100);

                updateProgress(progress);
                updateSpinner($"Procesando {group.Key}");

                try
                {
                    var discs = group.Value;

                    // Validación previa por tipo
                    if (discs.Any(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase)))
                    {
                        // PS1 (VCD)
                        discs = discs.Where(ValidateVcd).ToList();
                        if (discs.Count == 0)
                        {
                            log($"[PS1] Todos los VCD de {group.Key} fueron inválidos.");
                            continue;
                        }

                        ProcessPS1Group(group.Key, discs);
                    }
                    else
                    {
                        // PS2 (ISO)
                        var iso = discs.First();
                        if (!ValidateIso(iso))
                        {
                            log($"[PS2] ISO inválido: {iso}");
                            continue;
                        }

                        ProcessPS2(iso);
                    }
                }
                catch (Exception ex)
                {
                    log($"Error procesando {group.Key}: {ex.Message}");
                    notify(new UiNotification(NotificationType.Error,
                        $"Error procesando {group.Key}"));
                }
            }

            updateSpinner("Completado");
        }

        // ============================================================
        //  VALIDACIÓN VCD / ISO
        // ============================================================
        private bool ValidateVcd(string vcdPath)
        {
            if (!IntegrityValidator.Validate(vcdPath))
            {
                log($"[PS1] VCD inválido: {vcdPath}");
                notify(new UiNotification(NotificationType.Warning,
                    $"VCD inválido: {Path.GetFileName(vcdPath)}"));
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
                    notify(new UiNotification(NotificationType.Warning,
                        $"ISO sospechoso o demasiado pequeño: {Path.GetFileName(isoPath)}"));
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                log($"[PS2] Error validando ISO: {ex.Message}");
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
                string baseName = NameCleaner.Clean(name, out _);

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
            log($"[PS1] Procesando grupo: {baseName}");

            // Ordenar discos por número usando NameCleaner
            discs = discs.OrderBy(d =>
            {
                NameCleaner.Clean(Path.GetFileNameWithoutExtension(d), out string? cdTag);
                return cdTag != null ? int.Parse(cdTag.Replace("CD", "")) : 1;
            }).ToList();

            // Detectar ID real desde el VCD
            string firstDisc = discs.First();
            string? detectedId = GameIdDetector.DetectGameId(firstDisc);

            if (string.IsNullOrWhiteSpace(detectedId))
                detectedId = GameIdDetector.DetectFromName(baseName);

            if (string.IsNullOrWhiteSpace(detectedId))
            {
                notify(new UiNotification(NotificationType.Warning,
                    $"No se pudo detectar ID para {baseName}"));
                return;
            }

            // Obtener nombre del juego
            string cleanTitle = NameCleaner.Clean(baseName, out _);

            // ============================================================
            //  BASE DE DATOS
            // ============================================================
            GameEntry? dbEntry = null;

            if (useDatabase && GameDatabase.TryGetEntry(detectedId, out var entry))
            {
                dbEntry = entry;
                if (!string.IsNullOrWhiteSpace(dbEntry.Name))
                {
                    cleanTitle = dbEntry.Name;
                    log($"[DB] Nombre oficial encontrado: {cleanTitle}");
                }
            }

            // ============================================================
            //  COVER PS1
            // ============================================================
            if (useCovers && dbEntry?.CoverUrl != null)
            {
                string artFolder = Path.Combine(paths.PopsFolder, "ART");
                Directory.CreateDirectory(artFolder);

                string? art = ArtDownloader.DownloadArt(detectedId, dbEntry.CoverUrl, artFolder, log);

                if (art != null)
                    log($"[COVER] PS1 ART generado → {art}");
            }

            // ============================================================
            //  ESTRUCTURA FINAL PS1
            // ============================================================
            string gameRootFolder = Path.Combine(paths.PopsFolder, $"{detectedId} ({cleanTitle})");
            Directory.CreateDirectory(gameRootFolder);

            int discNumber = 1;
            List<string> discPaths = new();

            foreach (var disc in discs)
            {
                string discFolder = Path.Combine(gameRootFolder, $"CD{discNumber}");
                Directory.CreateDirectory(discFolder);

                string finalFileName = $"{detectedId}.{cleanTitle} (CD{discNumber}).VCD";
                string destVcd = Path.Combine(discFolder, finalFileName);

                File.Copy(disc, destVcd, true);
                discPaths.Add(destVcd);

                log($"[PS1] Copiado disco {discNumber} → {destVcd}");

                discNumber++;
            }

            // ============================================================
            //  DISCS.TXT
            // ============================================================
            MultiDiscManager.GenerateDiscsTxt(paths.PopsFolder, detectedId, discPaths, log);

            // ============================================================
            //  CHEAT.TXT (solo PAL)
            // ============================================================
            if (GameIdDetector.IsPalRegion(detectedId))
            {
                string cd1Folder = Path.Combine(gameRootFolder, "CD1");
                CheatGenerator.GenerateCheatTxt(detectedId, cd1Folder, log);
            }

            // ============================================================
            //  ELF (PS1) — CD1 + APPS
            // ============================================================
            GenerateElfForDisc1(detectedId, cleanTitle, gameRootFolder);

            notify(new UiNotification(NotificationType.Success,
                $"{cleanTitle} procesado correctamente."));
        }

        // ============================================================
        //  GENERAR ELF PARA CD1 (PS1) + COPIA A APPS
        // ============================================================
        private void GenerateElfForDisc1(string gameId, string title, string gameRootFolder)
        {
            string cd1Folder = Path.Combine(gameRootFolder, "CD1");

            string vcdName = Directory.GetFiles(cd1Folder, "*.VCD").FirstOrDefault();
            if (vcdName == null)
            {
                log($"[PS1] No se encontró VCD en {cd1Folder}");
                return;
            }

            string vcdPopstarterPath =
                $"mass:/POPS/{Path.GetFileName(gameRootFolder)}/CD1/{Path.GetFileName(vcdName)}";

            string outputElf = Path.Combine(cd1Folder, $"{gameId}.ELF");

            bool ok = ElfGenerator.GenerateElf(
                paths.PopstarterElfPath,
                outputElf,
                gameId,
                vcdPopstarterPath,
                $"{title} (CD1)",
                log
            );

            if (!ok)
            {
                notify(new UiNotification(NotificationType.Error,
                    $"Error generando ELF para {gameId}"));
                return;
            }

            // COPIA A APPS (OPL)
            string appsElf = Path.Combine(paths.AppsFolder, $"{gameId}.ELF");
            File.Copy(outputElf, appsElf, true);

            log($"[PS1] ELF copiado a APPS → {appsElf}");
        }

        // ============================================================
        //  PROCESAR PS2 (ISO)
        // ============================================================
        private void ProcessPS2(string isoPath)
        {
            string originalName = Path.GetFileNameWithoutExtension(isoPath);
            log($"[PS2] Procesando: {originalName}");

            string? detectedId = GameIdDetector.DetectGameId(isoPath);

            if (string.IsNullOrWhiteSpace(detectedId))
                detectedId = GameIdDetector.DetectFromName(originalName);

            if (string.IsNullOrWhiteSpace(detectedId))
            {
                notify(new UiNotification(NotificationType.Warning,
                    $"No se pudo detectar ID para {originalName}. Se copiará sin renombrar."));
                detectedId = originalName.Replace(" ", "_");
            }

            string cleanTitle = NameCleaner.CleanTitleOnly(originalName);

            // ============================================================
            //  BASE DE DATOS PS2
            // ============================================================
            GameEntry? dbEntry = null;

            if (useDatabase && GameDatabase.TryGetEntry(detectedId, out var entry))
            {
                dbEntry = entry;
                if (!string.IsNullOrWhiteSpace(dbEntry.Name))
                {
                    cleanTitle = dbEntry.Name;
                    log($"[DB] Nombre oficial PS2 encontrado: {cleanTitle}");
                }

                // COVER PS2
                if (useCovers && dbEntry.CoverUrl != null)
                {
                    string artFolder = Path.Combine(paths.DvdFolder, "ART");
                    Directory.CreateDirectory(artFolder);

                    string? art = ArtDownloader.DownloadArt(detectedId, dbEntry.CoverUrl, artFolder, log);

                    if (art != null)
                        log($"[COVER] PS2 ART generado → {art}");
                }
            }

            string finalName = $"{detectedId}.{cleanTitle}.iso";

            Directory.CreateDirectory(paths.DvdFolder);
            string dest = Path.Combine(paths.DvdFolder, finalName);

            File.Copy(isoPath, dest, true);

            log($"[PS2] Copiado ISO → {dest}");

            notify(new UiNotification(NotificationType.Success,
                $"{cleanTitle} copiado a DVD correctamente."));
        }
    }
}
