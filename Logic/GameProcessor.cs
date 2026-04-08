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
                if (!info.Exists || info.Length < 100_000_000) // ~100MB mínimo
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
        //  AGRUPAR MULTIDISCO (POR TÍTULO LIMPIO)
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
        //  PROCESAR GRUPO PS1 (MULTIDISCO)
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

            // Detectar ID real desde el VCD (sectorial)
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

            // Obtener nombre del juego (base)
            string cleanTitle = NameCleaner.Clean(baseName, out _);

            // ============================================================
            //  INTEGRACIÓN CON BASE DE DATOS (OPCIONAL)
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
            //  DESCARGA DE COVER PS1 (OPL COMPATIBLE)
            // ============================================================
            if (useCovers && dbEntry?.CoverUrl != null)
            {
                string artFolder = Path.Combine(paths.PopsFolder, "ART");
                Directory.CreateDirectory(artFolder);

                string? art = ArtDownloader.DownloadArt(detectedId, dbEntry.CoverUrl, artFolder, log);

                if (art != null)
                    log($"[COVER] PS1 ART generado → {art}");
            }

            // Procesar cada disco
            int discNumber = 1;
            List<string> discPaths = new();

            foreach (var disc in discs)
            {
                string folderName = $"{detectedId} ({cleanTitle}) (CD{discNumber})";
                string finalFileName = $"{detectedId}.{cleanTitle} (CD{discNumber}).VCD";

                string popsDiscFolder = Path.Combine(paths.PopsFolder, folderName);
                Directory.CreateDirectory(popsDiscFolder);

                string destVcd = Path.Combine(popsDiscFolder, finalFileName);
                File.Copy(disc, destVcd, true);

                discPaths.Add(destVcd);

                log($"[PS1] Copiado disco {discNumber} → {destVcd}");

                discNumber++;
            }

            // Generar DISCS.TXT (MultiDiscManager ULTRA PRO)
            MultiDiscManager.GenerateDiscsTxt(paths.PopsFolder, detectedId, discPaths, log);

            // Generar CHEAT.TXT solo si es PAL (CheatGenerator ULTRA PRO MAX)
            if (GameIdDetector.IsPalRegion(detectedId))
            {
                string popsDiscFolder = Path.Combine(paths.PopsFolder, $"{detectedId} ({cleanTitle}) (CD1)");
                CheatGenerator.GenerateCheatTxt(detectedId, popsDiscFolder, log);
            }

            // Generar ELF solo para CD1 (PS1)
            GenerateElfForDisc1(detectedId, cleanTitle);

            notify(new UiNotification(NotificationType.Success,
                $"{cleanTitle} procesado correctamente."));
        }

        // ============================================================
        //  GENERAR ELF PARA CD1 (PS1)
        // ============================================================
        private void GenerateElfForDisc1(string gameId, string title)
        {
            string popsDiscFolder = Directory.GetDirectories(paths.PopsFolder)
                .FirstOrDefault(d => Path.GetFileName(d).StartsWith($"{gameId} (", StringComparison.OrdinalIgnoreCase) &&
                                     d.Contains("(CD1)", StringComparison.OrdinalIgnoreCase))
                ?? Path.Combine(paths.PopsFolder, $"{gameId} (CD1)");

            if (!Directory.Exists(popsDiscFolder))
            {
                log($"[PS1] Carpeta de CD1 no encontrada para {gameId}");
                return;
            }

            string vcdName = Directory.GetFiles(popsDiscFolder, "*.VCD").FirstOrDefault();
            if (vcdName == null)
            {
                log($"[PS1] No se encontró VCD en {popsDiscFolder}");
                return;
            }

            string vcdPopstarterPath =
                $"mass:/POPS/{Path.GetFileName(popsDiscFolder)}/{Path.GetFileName(vcdName)}";

            string outputElf = Path.Combine(popsDiscFolder, $"{gameId}.ELF");

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
            }
        }

        // ============================================================
        //  PROCESAR PS2 (ISO) — CON COVERS OPL
        // ============================================================
        private void ProcessPS2(string isoPath)
        {
            string originalName = Path.GetFileNameWithoutExtension(isoPath);
            log($"[PS2] Procesando: {originalName}");

            // Detección real de ID desde ISO
            string? detectedId = GameIdDetector.DetectGameId(isoPath);

            if (string.IsNullOrWhiteSpace(detectedId))
                detectedId = GameIdDetector.DetectFromName(originalName);

            if (string.IsNullOrWhiteSpace(detectedId))
            {
                notify(new UiNotification(NotificationType.Warning,
                    $"No se pudo detectar ID para {originalName}. Se copiará sin renombrar."));
                detectedId = originalName.Replace(" ", "_");
            }

            // Confirmar que es PS2
            if (!GameIdValidator.IsPs2(detectedId))
            {
                log($"[PS2] ID no reconocido como PS2: {detectedId}. Se copiará igualmente.");
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

                // ============================================================
                //  DESCARGA DE COVER PS2 — OPL COMPATIBLE
                // ============================================================
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
