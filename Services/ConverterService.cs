using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private const int SectorSize = 2352;
        private const int UserDataSize = 2048;

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
        //  CONVERTIR CARPETA COMPLETA (PS1 → VCD)
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
                    var result = ConvertToVcd(file, outputFolder);

                    if (result != null)
                    {
                        _log($"Convertido: {file} → {result.VcdPath}");
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
        //  CONVERTIR UN ARCHIVO PS1 A VCD (OPTIMIZADO + MULTIDISCO)
        // ============================================================
        private ConvertedGame? ConvertToVcd(string inputPath, string outputFolder)
        {
            string ext = Path.GetExtension(inputPath).ToLowerInvariant();

            // Detectar PS2
            if (ext == ".iso" && IsPs2Iso(inputPath))
            {
                _log($"Detectado PS2 ISO, no se convierte: {inputPath}");
                return null;
            }

            // Detectar multidisco
            var discInfo = DetectDiscNumber(inputPath);

            // Nombre base limpio
            string baseName = CleanBaseName(Path.GetFileNameWithoutExtension(inputPath));

            // Nombre final del VCD
            string vcdName = discInfo.IsDisc
                ? $"{baseName} (CD{discInfo.DiscNumber}).vcd"
                : $"{baseName}.vcd";

            string outputPath = Path.Combine(outputFolder, vcdName);

            // Conversión optimizada
            ConvertPs1ToVcd(inputPath, outputPath, baseName);

            return new ConvertedGame
            {
                VcdPath = outputPath,
                BaseName = baseName,
                DiscNumber = discInfo.DiscNumber,
                IsMultiDisc = discInfo.IsDisc
            };
        }

        // ============================================================
        //  CONVERSIÓN PS1 → VCD (OPTIMIZADA)
        // ============================================================
        private void ConvertPs1ToVcd(string inputPath, string outputPath, string name)
        {
            using var input = File.OpenRead(inputPath);
            using var output = File.Create(outputPath);

            // Header POPStarter
            byte[] header = new byte[0x800];
            Array.Copy(Encoding.ASCII.GetBytes("PSX"), header, 3);
            output.Write(header, 0, header.Length);

            byte[] sector = new byte[SectorSize];
            byte[] userData = new byte[UserDataSize];

            long totalSectors = input.Length / SectorSize;
            long processed = 0;

            while (input.Read(sector, 0, SectorSize) == SectorSize)
            {
                Buffer.BlockCopy(sector, 24, userData, 0, UserDataSize);
                output.Write(userData, 0, UserDataSize);

                processed++;
                if (processed % 200 == 0)
                {
                    int percent = (int)((processed / (double)totalSectors) * 100);
                    _setStatus($"Convirtiendo {name}: {percent}%");
                }
            }
        }

        // ============================================================
        //  DETECTAR SI UN ISO ES PS2 (ROBUSTO)
        // ============================================================
        private bool IsPs2Iso(string isoPath)
        {
            try
            {
                using var fs = File.OpenRead(isoPath);
                byte[] buffer = new byte[0x8000];
                fs.Read(buffer, 0, buffer.Length);

                string text = Encoding.ASCII.GetString(buffer);

                if (text.Contains("PLAYSTATION 2", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (text.Contains("BOOT2", StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        //  DETECTAR MULTIDISCO
        // ============================================================
        private (bool IsDisc, int DiscNumber) DetectDiscNumber(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path).ToLower();

            for (int i = 1; i <= 9; i++)
            {
                if (name.Contains($"disc {i}") ||
                    name.Contains($"disc{i}") ||
                    name.Contains($"cd{i}") ||
                    name.Contains($"disk{i}"))
                {
                    return (true, i);
                }
            }

            return (false, 1);
        }

        // ============================================================
        //  LIMPIAR NOMBRE BASE
        // ============================================================
        private string CleanBaseName(string name)
        {
            name = name.Replace("(Disc 1)", "", StringComparison.OrdinalIgnoreCase)
                       .Replace("(Disc 2)", "", StringComparison.OrdinalIgnoreCase)
                       .Replace("(Disc 3)", "", StringComparison.OrdinalIgnoreCase)
                       .Replace("(CD1)", "", StringComparison.OrdinalIgnoreCase)
                       .Replace("(CD2)", "", StringComparison.OrdinalIgnoreCase)
                       .Replace("(CD3)", "", StringComparison.OrdinalIgnoreCase)
                       .Trim();

            return name;
        }
    }
}
