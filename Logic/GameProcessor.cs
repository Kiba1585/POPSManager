using POPSManager.Models;
using POPSManager.Services;
using System;
using System.IO;
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
            var files = Directory.GetFiles(folder, "*.vcd");

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

            // Detectar ID del juego
            string gameId = DetectGameId(originalName);

            if (string.IsNullOrWhiteSpace(gameId))
            {
                log($"No se pudo detectar ID para {originalName}");
                notify(new UiNotification(NotificationType.Warning,
                    $"No se pudo detectar ID para {originalName}"));
                return;
            }

            log($"ID detectado: {gameId}");

            // Detectar número de disco
            int discNumber = DetectDiscNumber(originalName);

            // Obtener nombre limpio del juego
            string cleanTitle = ExtractCleanTitle(originalName, gameId);
            if (string.IsNullOrWhiteSpace(cleanTitle))
                cleanTitle = gameId;

            // Nombre final: ID.Nombre (CDX).VCD
            string finalFileName = $"{gameId}.{cleanTitle} (CD{discNumber}).VCD";

            // Carpeta POPS por disco
            string popsDiscFolder = Path.Combine(paths.PopsFolder, $"{gameId} (CD{discNumber})");
            Directory.CreateDirectory(popsDiscFolder);

            // Copiar VCD
            string destVcd = Path.Combine(popsDiscFolder, finalFileName);
            File.Copy(vcdPath, destVcd, true);

            log($"Copiado VCD → {destVcd}");

            // MULTIDISCO
            MultiDiscManager.ProcessMultiDisc(paths.PopsFolder, gameId, log);

            // ============================================================
            //  GENERAR ELF SOLO PARA CD1
            // ============================================================

            if (IsPs1(gameId) && discNumber == 1)
            {
                if (string.IsNullOrWhiteSpace(paths.PopstarterElfPath))
                {
                    notify(new UiNotification(NotificationType.Error,
                        "No se encontró POPSTARTER.ELF. Configúralo en Ajustes."));
                    return;
                }

                string outputElf = Path.Combine(popsDiscFolder, $"{gameId}.ELF");

                // Ruta POPStarter correcta
                string vcdPopstarterPath =
                    $"mass:/POPS/{gameId} (CD{discNumber})/{finalFileName}";

                // Título visible en OPL
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
        //  DETECTAR ID DEL JUEGO
        // ============================================================

        private string DetectGameId(string fileName)
        {
            fileName = fileName.ToUpperInvariant();

            string[] patterns =
            {
                "SCES", "SLES", "SLUS", "SCUS", "SLPS", "SLPM", "SCPH"
            };

            foreach (var p in patterns)
            {
                if (fileName.Contains(p))
                {
                    string cleaned = CleanId(fileName, p);
                    return cleaned;
                }
            }

            return "";
        }

        private string CleanId(string name, string prefix)
        {
            int index = name.IndexOf(prefix);
            if (index < 0) return "";

            string id = name.Substring(index);

            id = id.Replace("-", "_")
                   .Replace(" ", "_");

            if (id.Length > 12)
                id = id.Substring(0, 12);

            return id;
        }

        private bool IsPs1(string id)
        {
            return id.StartsWith("SCES") ||
                   id.StartsWith("SLES") ||
                   id.StartsWith("SLUS") ||
                   id.StartsWith("SCUS");
        }

        // ============================================================
        //  DETECTAR NÚMERO DE DISCO
        // ============================================================

        private int DetectDiscNumber(string name)
        {
            var regex = new Regex(@"\((CD|DISC|DISK)\s*(\d+)\)", RegexOptions.IgnoreCase);
            var match = regex.Match(name);
            if (match.Success && int.TryParse(match.Groups[2].Value, out int num))
                return num;

            return 1;
        }

        // ============================================================
        //  LIMPIAR NOMBRE DEL JUEGO
        // ============================================================

        private string ExtractCleanTitle(string originalName, string gameId)
        {
            string name = originalName;

            int idx = name.IndexOf(gameId, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
                name = name.Remove(idx, gameId.Length);

            name = Regex.Replace(name, @"\((CD|DISC|DISK)\s*\d+\)", "", RegexOptions.IgnoreCase);

            name = name.Replace("_", " ")
                       .Replace(".", " ")
                       .Trim();

            name = Regex.Replace(name, @"\s{2,}", " ");

            return name;
        }
    }
}
