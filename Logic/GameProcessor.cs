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

            // Detectar número de disco (por defecto 1)
            int discNumber = DetectDiscNumber(originalName);

            // Obtener nombre limpio del juego (sin ID ni tags de disco)
            string cleanTitle = ExtractCleanTitle(originalName, gameId);

            // Si por alguna razón queda vacío, usamos el ID como fallback
            if (string.IsNullOrWhiteSpace(cleanTitle))
                cleanTitle = gameId;

            // Nombre final: ID.NombreLimpio (CDX).VCD
            string finalFileName = $"{gameId}.{cleanTitle} (CD{discNumber}).VCD";

            // Carpeta POPS por disco: POPS/ID (CDX)/
            string popsDiscFolder = Path.Combine(paths.PopsFolder, $"{gameId} (CD{discNumber})");
            Directory.CreateDirectory(popsDiscFolder);

            // Copiar VCD con el nombre final
            string destVcd = Path.Combine(popsDiscFolder, finalFileName);
            File.Copy(vcdPath, destVcd, true);

            log($"Copiado VCD → {destVcd}");

            // MULTIDISCO: genera DISCS.TXT en todas las carpetas de ese juego
            MultiDiscManager.ProcessMultiDisc(paths.PopsFolder, gameId, log);

            // Generar ELF si es PS1 (lo dejamos en la carpeta del primer disco)
            if (IsPs1(gameId) && discNumber == 1)
            {
                GenerateElf(gameId, popsDiscFolder);
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
            // Soporta: (Disc 1), (Disk 1), (CD 1), (CD1), etc.
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

            // Quitar el ID si viene en el nombre
            int idx = name.IndexOf(gameId, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                name = name.Remove(idx, gameId.Length);
            }

            // Quitar tags de disco: (Disc 1), (Disk 1), (CD 1), (CD1), etc.
            name = Regex.Replace(name, @"\((CD|DISC|DISK)\s*\d+\)", "", RegexOptions.IgnoreCase);

            // Limpiar separadores raros
            name = name.Replace("_", " ")
                       .Replace(".", " ")
                       .Trim();

            // Colapsar espacios múltiples
            name = Regex.Replace(name, @"\s{2,}", " ");

            return name;
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
