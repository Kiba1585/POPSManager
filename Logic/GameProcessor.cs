using POPSManager.Models;
using POPSManager.Services;
using System;
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

        public GameProcessor(
            Action<int> updateProgress,
            Action<string> updateSpinner,
            Action<string> log,
            Action<UiNotification> notify,
            PathsService paths)
        {
            this.updateProgress = updateProgress;
            this.updateSpinner = updateSpinner;
            this.log = log;
            this.notify = notify;
            this.paths = paths;
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

            int index = 0;

            foreach (var file in files)
            {
                index++;
                int progress = (int)((index / (double)files.Length) * 100);

                updateProgress(progress);
                updateSpinner($"Procesando {Path.GetFileName(file)}");

                try
                {
                    if (file.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase))
                        ProcessPS1(file);
                    else if (file.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                        ProcessPS2(file);
                }
                catch (Exception ex)
                {
                    log($"Error procesando {file}: {ex.Message}");
                    notify(new UiNotification(NotificationType.Error,
                        $"Error procesando {Path.GetFileName(file)}"));
                }
            }

            updateSpinner("Completado");
        }

        // ============================================================
        //  PROCESAR PS1 (VCD)
        // ============================================================
        private void ProcessPS1(string vcdPath)
        {
            string originalName = Path.GetFileNameWithoutExtension(vcdPath);
            log($"[PS1] Procesando: {originalName}");

            if (!IntegrityValidator.Validate(vcdPath))
            {
                notify(new UiNotification(NotificationType.Error,
                    $"{originalName}.VCD está corrupto o incompleto."));
                return;
            }

            string? detectedId = GameIdDetector.DetectGameId(vcdPath);
            string gameId = !string.IsNullOrWhiteSpace(detectedId)
                ? detectedId
                : GameIdDetector.DetectFromName(originalName);

            if (string.IsNullOrWhiteSpace(gameId))
            {
                notify(new UiNotification(NotificationType.Warning,
                    $"No se pudo detectar ID para {originalName}"));
                return;
            }

            string cleanTitle = NameCleaner.Clean(originalName, out string? cdTag);
            int discNumber = cdTag != null ? int.Parse(cdTag.Replace("CD", "")) : 1;

            if (string.IsNullOrWhiteSpace(cleanTitle))
                cleanTitle = gameId;

            string finalFileName = $"{gameId}.{cleanTitle}.VCD";

            string popsDiscFolder = Path.Combine(paths.PopsFolder, $"{gameId} (CD{discNumber})");
            Directory.CreateDirectory(popsDiscFolder);

            string destVcd = Path.Combine(popsDiscFolder, finalFileName);
            File.Copy(vcdPath, destVcd, true);

            log($"[PS1] Copiado VCD → {destVcd}");

            MultiDiscManager.ProcessMultiDisc(paths.PopsFolder, gameId, log);

            if (discNumber == 1)
            {
                if (string.IsNullOrWhiteSpace(paths.PopstarterElfPath))
                {
                    notify(new UiNotification(NotificationType.Error,
                        "No se encontró POPSTARTER.ELF. Configúralo en Ajustes."));
                    return;
                }

                string outputElf = Path.Combine(popsDiscFolder, $"{gameId}.ELF");
                string vcdPopstarterPath =
                    $"mass:/POPS/{gameId} (CD{discNumber})/{finalFileName}";
                string displayTitle = $"{cleanTitle} (CD{discNumber})";

                bool ok = ElfGenerator.GenerateElf(
                    paths.PopstarterElfPath,
                    outputElf,
                    gameId,
                    vcdPopstarterPath,
                    displayTitle,
                    log
                );

                if (!ok)
                {
                    notify(new UiNotification(NotificationType.Error,
                        $"Error generando ELF para {gameId}"));
                }
            }

            notify(new UiNotification(NotificationType.Success,
                $"{originalName} procesado correctamente."));
        }

        // ============================================================
        //  PROCESAR PS2 (ISO)
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

            string finalName = $"{detectedId}.{cleanTitle}.iso";

            string dest = Path.Combine(paths.DvdFolder, finalName);
            Directory.CreateDirectory(paths.DvdFolder);

            File.Copy(isoPath, dest, true);

            log($"[PS2] Copiado ISO → {dest}");

            notify(new UiNotification(NotificationType.Success,
                $"{originalName} copiado a DVD correctamente."));
        }
    }
}
