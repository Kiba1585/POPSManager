using POPSManager.Models;
using POPSManager.Services;
using POPSManager.Logic.MultiDisc;   
using System;
using System.IO;

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
            string fileName = Path.GetFileNameWithoutExtension(vcdPath);

            log($"Procesando juego: {fileName}");

            // Detectar ID del juego
            string gameId = DetectGameId(fileName);

            if (string.IsNullOrWhiteSpace(gameId))
            {
                log($"No se pudo detectar ID para {fileName}");
                notify(new UiNotification(NotificationType.Warning,
                    $"No se pudo detectar ID para {fileName}"));
                return;
            }

            log($"ID detectado: {gameId}");

            // Crear estructura POPS
            string popsFolder = Path.Combine(paths.PopsFolder, gameId);
            Directory.CreateDirectory(popsFolder);

            // Copiar VCD
            string destVcd = Path.Combine(popsFolder, $"{gameId}.VCD");
            File.Copy(vcdPath, destVcd, true);

            log($"Copiado VCD → {destVcd}");

            // ============================================================
            //  MULTIDISCO (NUEVO)
            // ============================================================
            MultiDiscManager.ProcessMultiDisc(paths.PopsFolder, gameId, log);

            // Generar ELF si es PS1
            if (IsPs1(gameId))
            {
                GenerateElf(gameId, popsFolder);
            }

            notify(new UiNotification(NotificationType.Success,
                $"{fileName} procesado correctamente."));
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
        //  GENERAR ELF
        // ============================================================

        private void GenerateElf(string gameId, string folder)
        {
            string elfPath = Path.Combine(folder, $"{gameId}.ELF");

            try
            {
                File.WriteAllText(elfPath, "ELF-DATA");
                log($"ELF generado: {elfPath}");
            }
            catch (Exception ex)
            {
                log($"Error generando ELF: {ex.Message}");
                notify(new UiNotification(NotificationType.Error,
                    $"Error generando ELF para {gameId}"));
            }
        }
    }
}
