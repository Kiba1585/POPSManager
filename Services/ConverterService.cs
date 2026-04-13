using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using POPSManager.Models;
using POPSManager.Logic;

namespace POPSManager.Services
{
    public sealed class ConverterService
    {
        private readonly Action<string> _log;
        private readonly PathsService _paths;
        private readonly SettingsService _settings;

        private readonly Action<string, NotificationType> _notify;
        private readonly Action<string> _setStatus;

        private const int SectorSize = 2352;
        private const int UserDataSize = 2048;

        public ConverterService(
            Action<string> log,
            PathsService paths,
            SettingsService settings,
            Action<string, NotificationType> notify,
            Action<string> setStatus)
        {
            _log = log;
            _paths = paths;
            _settings = settings;
            _notify = notify;
            _setStatus = setStatus;
        }

        // ============================================================
        //  CONVERTIR CARPETA COMPLETA
        // ============================================================
        public void ConvertFolder(string sourceFolder, string outputFolder)
        {
            if (!Directory.Exists(sourceFolder))
            {
                _notify("Carpeta de origen inválida.", NotificationType.Error);
                return;
            }

            Directory.CreateDirectory(outputFolder);

            var files = Directory.GetFiles(sourceFolder)
                .Where(f =>
                    f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".cue", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f)
                .ToArray();

            if (files.Length == 0)
            {
                _notify("No se encontraron archivos BIN/CUE/ISO.", NotificationType.Warning);
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
                        _log($"Convertido: {file} → {result.VcdPath}");
                }
                catch (Exception ex)
                {
                    _log($"ERROR al convertir {file}: {ex.Message}");
                    _notify($"Error con {Path.GetFileName(file)}", NotificationType.Error);
                }
            }

            _notify("Conversión completada.", NotificationType.Success);
            _setStatus("Conversión finalizada.");
        }

        // ============================================================
        //  CONVERTIR ARCHIVO INDIVIDUAL
        // ============================================================
        private ConvertedGame? ConvertToVcd(string inputPath, string outputFolder)
        {
            string ext = Path.GetExtension(inputPath).ToLowerInvariant();

            if (ext == ".iso" && IsPs2Iso(inputPath))
            {
                _log($"Detectado PS2 ISO, no se convierte: {inputPath}");
                return null;
            }

            var discInfo = DetectDiscNumber(inputPath);

            string baseName = CleanBaseName(Path.GetFileNameWithoutExtension(inputPath));

            string vcdName = discInfo.IsDisc
                ? $"{baseName} (CD{discInfo.DiscNumber}).vcd"
                : $"{baseName}.vcd";

            string outputPath = Path.Combine(outputFolder, vcdName);

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
        //  CONVERSIÓN PS1 → VCD
        // ============================================================
        private void ConvertPs1ToVcd(string inputPath, string outputPath, string name)
        {
            using var input = File.OpenRead(inputPath);
            using var output = File.Create(outputPath);

            byte[] header = new byte[0x800];
            Array.Copy(Encoding.ASCII.GetBytes("PSX"), header, 3);
            output.Write(header, 0, header.Length);

            byte[] sector = new byte[SectorSize];
            byte[] userData = new byte[UserDataSize];

            long totalSectors = input.Length / SectorSize;
            long processed = 0;

            while (true)
            {
                int read = input.Read(sector, 0, SectorSize);
                if (read == 0) break;

                if (read != SectorSize)
                {
                    _log($"[WARN] Sector incompleto detectado en {name}");
                    break;
                }

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
        //  DETECTAR ISO PS2
        // ============================================================
        private static bool IsPs2Iso(string isoPath)
        {
            try
            {
                using var fs = File.OpenRead(isoPath);

                byte[] buffer = new byte[0x20000];
                fs.Read(buffer, 0, buffer.Length);

                string text = Encoding.ASCII.GetString(buffer);

                return text.Contains("BOOT2", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("PLAYSTATION 2", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("PS2", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        //  DETECTAR MULTIDISCO
        // ============================================================
        private static (bool IsDisc, int DiscNumber) DetectDiscNumber(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

            for (int i = 1; i <= 9; i++)
            {
                if (name.Contains($"disc {i}", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains($"disc{i}", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains($"cd{i}", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains($"cd {i}", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains($"disk{i}", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains($"disk {i}", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains($"(cd{i})", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains($"(disc{i})", StringComparison.OrdinalIgnoreCase))
                {
                    return (true, i);
                }
            }

            return (false, 1);
        }

        // ============================================================
        //  LIMPIAR NOMBRE BASE
        // ============================================================
        private static string CleanBaseName(string name)
        {
            string[] patterns =
            {
                "(disc 1)", "(disc 2)", "(disc 3)",
                "(cd1)", "(cd2)", "(cd3)",
                "disc 1", "disc 2", "disc 3",
                "cd1", "cd2", "cd3"
            };

            foreach (var p in patterns)
                name = name.Replace(p, "", StringComparison.OrdinalIgnoreCase);

            return name.Trim();
        }
    }
}
