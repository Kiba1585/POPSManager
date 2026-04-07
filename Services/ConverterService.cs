using System;
using System.IO;
using System.Linq;
using POPSManager.Models;
using POPSManager.Logic;

namespace POPSManager.Services
{
    public class ConverterService
    {
        private readonly Action<string> _log;
        private readonly PathsService _paths;
        private readonly SettingsService _settings;
        private readonly Action<UiNotification> _notify;
        private readonly Action<string> _setStatus;

        public ConverterService(
            Action<string> log,
            PathsService paths,
            SettingsService settings,
            Action<UiNotification> notify,
            Action<string> setStatus)
        {
            _log = log;
            _paths = paths;
            _settings = settings;
            _notify = notify;
            _setStatus = setStatus;
        }

        // ============================================================
        //  CONVERTIR CARPETA (PS1 → VCD) Y ENVIAR A GAMEPROCESSOR
        // ============================================================
        public void ConvertFolder(string sourceFolder, string outputFolder)
        {
            if (!Directory.Exists(sourceFolder))
            {
                _notify(new UiNotification(NotificationType.Error, "Carpeta de origen inválida."));
                return;
            }

            Directory.CreateDirectory(outputFolder);

            var files = Directory.GetFiles(sourceFolder)
                                 .Where(f => f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".cue", StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                                 .OrderBy(f => f)
                                 .ToArray();

            if (files.Length == 0)
            {
                _notify(new UiNotification(NotificationType.Warning, "No se encontraron archivos BIN/CUE/ISO."));
                return;
            }

            int index = 0;

            foreach (var file in files)
            {
                index++;
                _setStatus($"Convirtiendo {index}/{files.Length}: {Path.GetFileName(file)}");

                try
                {
                    string vcdPath = ConvertToVcd(file, outputFolder);

                    if (!string.IsNullOrWhiteSpace(vcdPath))
                    {
                        _log($"Convertido: {file} → {vcdPath}");
                    }
                }
                catch (Exception ex)
                {
                    _log($"ERROR al convertir {file}: {ex.Message}");
                    _notify(new UiNotification(NotificationType.Error, $"Error con {Path.GetFileName(file)}"));
                }
            }

            _notify(new UiNotification(NotificationType.Success, "Conversión completada."));
            _setStatus("Conversión finalizada.");
        }

        // ============================================================
        //  CONVERTIR UN ARCHIVO PS1 A VCD
        // ============================================================
        private string ConvertToVcd(string inputPath, string outputFolder)
        {
            string ext = Path.GetExtension(inputPath).ToLowerInvariant();

            // Si es ISO, verificar si es PS1 o PS2
            if (ext == ".iso")
            {
                if (IsPs2Iso(inputPath))
                {
                    _log($"Detectado PS2 ISO, no se convierte: {inputPath}");
                    return "";
                }
            }

            // PS1 → convertir a VCD
            string name = Path.GetFileNameWithoutExtension(inputPath);
            string outputPath = Path.Combine(outputFolder, $"{name}.vcd");

            using var input = File.OpenRead(inputPath);
            using var output = File.Create(outputPath);

            // Header POPStarter
            byte[] header = new byte[0x800];
            Array.Copy(System.Text.Encoding.ASCII.GetBytes("PSX"), header, 3);
            output.Write(header, 0, header.Length);

            // Sector 2352 → 2048
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
                    _setStatus($"Convirtiendo {name}: {percent}%");
                }
            }

            return outputPath;
        }

        // ============================================================
        //  DETECTAR SI UN ISO ES PS2
        // ============================================================
        private bool IsPs2Iso(string isoPath)
        {
            try
            {
                using var fs = File.OpenRead(isoPath);
                byte[] buffer = new byte[0x8000];
                fs.Read(buffer, 0, buffer.Length);

                string text = System.Text.Encoding.ASCII.GetString(buffer);

                return text.Contains("PLAYSTATION 2", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
