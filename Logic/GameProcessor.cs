using POPSManager.Models;
using POPSManager.Services;
using POPSManager.Logic.Database;
using POPSManager.Logic.Covers;
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
                    if (group.Value.Any(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase)))
                        ProcessPS1Group(group.Key, group.Value);
                    else
                        ProcessPS2(group.Value.First());
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
        //  PROCESAR GRUPO PS1 (MULTIDISCO)
        // ============================================================
        private void ProcessPS1Group(string baseName, List<string> discs)
        {
            log($"[PS1] Procesando grupo: {baseName}");

            // Ordenar discos por número
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
            //  INTEGRACIÓN CON BASE DE DATOS (OPCIONAL)
            // ============================================================
            GameInfo? dbInfo = null;

            if (useDatabase)
            {
                dbInfo = GameDatabase.Lookup(detectedId);

                if (dbInfo != null && !string.IsNullOrWhiteSpace(dbInfo.Name))
                {
                    cleanTitle = dbInfo.Name;
                    log($"[DB] Nombre oficial encontrado: {cleanTitle}");
                }
            }

            // ============================================================
            //  DESCARGA DE COVER (OPCIONAL) — OPL COMPATIBLE
            // ============================================================
            if (useCovers && dbInfo?.CoverUrl != null)
            {
                string artFolder = Path.Combine(paths.PopsFolder, "ART");
                string artPath = Path.Combine(artFolder, $"{detectedId}.ART");

                Directory.CreateDirectory(artFolder);

                string? jpg = CoverDownloader.DownloadCover(detectedId, dbInfo.CoverUrl, artFolder, log);

                if (jpg != null)
                {
                    File.Move(jpg, artPath, true);
                    log($"[COVER] Convertido a ART → {artPath}");
                }
            }

            // Procesar cada disco
            int discNumber = 1;
            List<string> discPaths = new();

            foreach (var disc in discs)
            {
                string finalFileName = $"{detectedId}.{cleanTitle} (CD{discNumber}).VCD";

                string popsDiscFolder = Path.Combine(paths.PopsFolder, $"{detectedId} (CD{discNumber})");
                Directory.CreateDirectory(popsDiscFolder);

                string destVcd = Path.Combine(popsDiscFolder, finalFileName);
                File.Copy(disc, destVcd, true);

                discPaths.Add(destVcd);

                log($"[PS1] Copiado disco {discNumber} → {destVcd}");

                discNumber++;
            }

            // Generar DISCS.TXT
            MultiDiscManager.GenerateDiscsTxt(paths.PopsFolder, detectedId, discPaths, log);

            // Generar CHEAT.TXT solo si es PAL
            if (GameIdDetector.IsPalRegion(detectedId))
            {
                string popsDiscFolder = Path.Combine(paths.PopsFolder, $"{detectedId} (CD1)");
                CheatGenerator.GenerateCheatTxt(detectedId, popsDiscFolder, log);
            }

            // Generar ELF solo para CD1
            GenerateElfForDisc1(detectedId, cleanTitle);

            notify(new UiNotification(NotificationType.Success,
                $"{cleanTitle} procesado correctamente."));
        }

        // ============================================================
        //  GENERAR ELF PARA CD1
        // ============================================================
        private void GenerateElfForDisc1(string gameId, string title)
        {
            string popsDiscFolder = Path.Combine(paths.PopsFolder, $"{gameId} (CD1)");
            string vcdName = Directory.GetFiles(popsDiscFolder, "*.VCD").First();

            string vcdPopstarterPath =
                $"mass:/POPS/{gameId} (CD1)/{Path.GetFileName(vcdName)}";

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
        //  PROCESAR PS2 (ISO) — AHORA CON COVERS OPL
        // ============================================================
        private void ProcessPS2(string isoPath)
        {
            string originalName = Path.GetFileNameWithoutExtension(isoPath);
            log($"[PS2] Procesando: {originalName}");

            string? detectedId = GameIdDetector.DetectFromName(originalName);

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
            GameInfo? dbInfo = null;

            if (useDatabase)
            {
                dbInfo = GameDatabase.Lookup(detectedId);

                if (dbInfo != null)
                {
                    cleanTitle = dbInfo.Name;
                    log($"[DB] Nombre oficial PS2 encontrado: {cleanTitle}");

                    // ============================================================
                    //  DESCARGA DE COVER PS2 — OPL COMPATIBLE
                    // ============================================================
                    if (useCovers && dbInfo.CoverUrl != null)
                    {
                        string artFolder = Path.Combine(paths.DvdFolder, "ART");
                        string artPath = Path.Combine(artFolder, $"{detectedId}.ART");

                        Directory.CreateDirectory(artFolder);

                        string? jpg = CoverDownloader.DownloadCover(detectedId, dbInfo.CoverUrl, artFolder, log);

                        if (jpg != null)
                        {
                            File.Move(jpg, artPath, true);
                            log($"[COVER] PS2 convertido a ART → {artPath}");
                        }
                    }
                }
            }

            string finalName = $"{detectedId}.{cleanTitle}.iso";

            string dest = Path.Combine(paths.DvdFolder, finalName);
            Directory.CreateDirectory(paths.DvdFolder);

            File.Copy(isoPath, dest, true);

            log($"[PS2] Copiado ISO → {dest}");

            notify(new UiNotification(NotificationType.Success,
                $"{cleanTitle} copiado a DVD correctamente."));
        }
    }
}
