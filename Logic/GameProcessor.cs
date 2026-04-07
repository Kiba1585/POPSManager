using POPSManager.Models;
using POPSManager.Services;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
        //  PROCESAR CARPETA COMPLETA
        // ============================================================
        public void ProcessFolder(string folder)
        {
            var files = Directory.GetFiles(folder, "*.vcd")
                                 .OrderBy(f => f)
                                 .ToArray();

            if (files.Length == 0)
            {
                notify(new UiNotification(NotificationType.Warning,
                    "No se encontraron archivos VCD."));
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
                    ProcessSingle(file);
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
        //  PROCESAR UN SOLO JUEGO
        // ============================================================
        private void ProcessSingle(string vcdPath)
        {
            string originalName = Path.GetFileNameWithoutExtension(vcdPath);
            log($"Procesando juego: {originalName}");

            // ============================================================
            // 1) Validar integridad del VCD
            // ============================================================
            if (!IntegrityValidator.Validate(vcdPath))
            {
                notify(new UiNotification(NotificationType.Error,
                    $"{originalName}.VCD está corrupto o incompleto."));
                return;
            }

            // ============================================================
            // 2) Detectar ID real del juego
            // ============================================================
            string? detectedId = GameIdDetector.DetectGameId(vcdPath);

            string gameId = !string.IsNullOrWhiteSpace(detectedId)
                ? detectedId
                : DetectGameIdFromName(originalName);

            if (string.IsNullOrWhiteSpace(gameId))
            {
                log($"No se pudo detectar ID para {originalName}");
                notify(new UiNotification(NotificationType.Warning,
                    $"No se pudo detectar ID para {originalName}"));
                return;
            }

            log($"ID detectado: {gameId}");

            // ============================================================
            // 3) Limpiar nombre del juego y detectar CDX
            // ============================================================
            string cleanTitle = NameCleaner.Clean(originalName, out string? cdTag);

            int discNumber = cdTag != null
                ? int.Parse(cdTag.Replace("CD", ""))
                : 1;

            if (string.IsNullOrWhiteSpace(cleanTitle))
                cleanTitle = gameId;

            // ============================================================
            // 4) Nombre final profesional
            // ============================================================
            string finalFileName = $"{gameId}.{cleanTitle}.VCD";

            // Carpeta POPS por disco
            string popsDiscFolder = Path.Combine(paths.PopsFolder, $"{gameId} (CD{discNumber})");
            Directory.CreateDirectory(popsDiscFolder);

            // Copiar VCD
            string destVcd = Path.Combine(popsDiscFolder, finalFileName);
            File.Copy(vcdPath, destVcd, true);

            log($"Copiado VCD → {destVcd}");

            // ============================================================
            // 5) MULTIDISCO
            // ============================================================
            MultiDiscManager.ProcessMultiDisc(paths.PopsFolder, gameId, log);

            // ============================================================
            // 6) GENERAR ELF SOLO PARA CD1
            // ============================================================
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
        //  DETECTAR ID DESDE EL NOMBRE (FALLBACK)
        // ============================================================
        private string DetectGameIdFromName(string fileName)
        {
            fileName = fileName.ToUpperInvariant();

            string[] patterns =
            {
                "SCES", "SLES", "SLUS", "SCUS", "SLPS", "SLPM", "SCPS"
            };

            foreach (var p in patterns)
            {
                if (fileName.Contains(p))
                {
                    int index = fileName.IndexOf(p);
                    string id = fileName.Substring(index);

                    id = id.Replace("-", "_")
                           .Replace(" ", "_");

                    if (id.Length > 12)
                        id = id.Substring(0, 12);

                    return id;
                }
            }

            return "";
        }
    }
}
