using POPSManager.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace POPSManager.Logic
{
    public class Converter
    {
        private readonly Action<int> updateProgress;
        private readonly Action<string> updateSpinner;
        private readonly Action<string> log;
        private readonly Action<UiNotification> notify;

        public Converter(
            Action<int> updateProgress,
            Action<string> updateSpinner,
            Action<string> log,
            Action<UiNotification> notify)
        {
            this.updateProgress = updateProgress;
            this.updateSpinner = updateSpinner;
            this.log = log;
            this.notify = notify;
        }

        // ============================================================
        //  CONVERTIR CARPETA COMPLETA
        // ============================================================

        public void ConvertFolder(string sourceFolder, string outputFolder)
        {
            var files = Directory.GetFiles(sourceFolder)
                                 .Where(f => f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".cue", StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                                 .ToArray();

            if (files.Length == 0)
            {
                notify(new UiNotification(NotificationType.Warning,
                    "No se encontraron archivos BIN/CUE/ISO."));
                return;
            }

            log($"Archivos detectados: {files.Length}");

            int index = 0;

            foreach (var file in files)
            {
                index++;
                int percent = (int)((index / (double)files.Length) * 100);
                updateProgress(percent);

                updateSpinner($"Convirtiendo {Path.GetFileName(file)}");

                try
                {
                    ConvertSingle(file, outputFolder);
                }
                catch (Exception ex)
                {
                    log($"ERROR al convertir {file}: {ex.Message}");
                    notify(new UiNotification(NotificationType.Error,
                        $"Error al convertir {Path.GetFileName(file)}"));
                }
            }

            updateSpinner("Completado");
        }

        // ============================================================
        //  CONVERTIR UN SOLO ARCHIVO
        // ============================================================

        private void ConvertSingle(string inputPath, string outputFolder)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputPath);

            // Resolver CUE → BIN
            if (inputPath.EndsWith(".cue", StringComparison.OrdinalIgnoreCase))
            {
                string? bin = ResolveCue(inputPath);
                if (bin == null)
                {
                    log($"CUE inválido: {inputPath}");
                    notify(new UiNotification(NotificationType.Error,
                        $"CUE inválido: {fileName}"));
                    return;
                }

                inputPath = bin;
                fileName = Path.GetFileNameWithoutExtension(bin);
            }

            // Ignorar ISOs de PS2
            if (IsPs2Iso(inputPath))
            {
                log($"ISO ignorado (PS2): {inputPath}");
                return;
            }

            string outputPath = Path.Combine(outputFolder, fileName + ".VCD");

            log("-----------------------------------------");
            log($"Convirtiendo: {fileName}");

            using var input = File.OpenRead(inputPath);
            using var output = File.Create(outputPath);

            // Escribir encabezado POPStarter
            WriteHeader(output);

            // Convertir sectores
            ConvertSectors(input, output, fileName);

            notify(new UiNotification(NotificationType.Success,
                $"{fileName}.VCD generado correctamente."));
        }

        // ============================================================
        //  RESOLVER CUE → BIN
        // ============================================================

        private string? ResolveCue(string cuePath)
        {
            var lines = File.ReadAllLines(cuePath);

            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("FILE", StringComparison.OrdinalIgnoreCase))
                {
                    string cleaned = line.Replace("FILE", "")
                                         .Replace("\"", "")
                                         .Trim();

                    string binName = cleaned.Split(' ')[0];
                    string binPath = Path.Combine(Path.GetDirectoryName(cuePath)!, binName);

                    if (File.Exists(binPath))
                        return binPath;
                }
            }

            return null;
        }

        // ============================================================
        //  DETECTAR SI ES ISO DE PS2
        // ============================================================

        private bool IsPs2Iso(string path)
        {
            string name = Path.GetFileName(path).ToUpper();

            return name.Contains("SLUS") ||
                   name.Contains("SCUS") ||
                   name.Contains("SLES") ||
                   name.Contains("SCES");
        }

        // ============================================================
        //  ESCRIBIR ENCABEZADO POPSTARTER
        // ============================================================

        private void WriteHeader(FileStream output)
        {
            byte[] header = new byte[0x800];
            Array.Copy(Encoding.ASCII.GetBytes("PSX"), header, 3);
            output.Write(header, 0, header.Length);
        }

        // ============================================================
        //  CONVERTIR SECTORES 2352 → 2048
        // ============================================================

        private void ConvertSectors(FileStream input, FileStream output, string name)
        {
            byte[] sector = new byte[2352];
            byte[] userData = new byte[2048];

            long totalSectors = input.Length / 2352;
            long processed = 0;

            while (input.Read(sector, 0, 2352) == 2352)
            {
                Array.Copy(sector, 24, userData, 0, 2048);
                output.Write(userData, 0, 2048);

                processed++;

                if (processed % 200 == 0)
                {
                    int percent = (int)((processed / (double)totalSectors) * 100);
                    updateProgress(percent);
                }
            }

            log($"Conversión completada: {name}.VCD");
        }
    }
}
