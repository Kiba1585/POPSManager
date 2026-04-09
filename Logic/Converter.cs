using POPSManager.Models;
using POPSManager.Services;
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
        private readonly PathsService paths;

        public Converter(
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

        public void ConvertSingle(string inputPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputPath);

            // Resolver CUE
            if (inputPath.EndsWith(".cue", StringComparison.OrdinalIgnoreCase))
            {
                var cue = CueParser.Parse(inputPath, log);
                if (cue == null || !File.Exists(cue.BinPath))
                {
                    notify(new UiNotification(NotificationType.Error, $"CUE inválido: {fileName}"));
                    return;
                }

                inputPath = cue.BinPath;
                fileName = Path.GetFileNameWithoutExtension(cue.BinPath);
            }

            // Detectar PS2
            if (IsPs2Iso(inputPath))
            {
                log($"ISO ignorado (PS2): {inputPath}");
                return;
            }

            string outputPath = Path.Combine(paths.PopsFolder, fileName + ".VCD");

            log("-----------------------------------------");
            log($"Convirtiendo: {fileName}");

            using var input = File.OpenRead(inputPath);
            using var output = File.Create(outputPath);

            // Detectar modo de sector
            var mode = SectorDetector.Detect(input, log);
            if (mode == SectorMode.Unknown)
            {
                notify(new UiNotification(NotificationType.Error, $"Formato de sector desconocido: {fileName}"));
                return;
            }

            // Escribir header VCD real
            VcdHeader.Write(output, fileName, input.Length, log);

            // Convertir sectores con validación
            SectorConverter.Convert(input, output, mode, updateProgress, log);

            notify(new UiNotification(NotificationType.Success, $"{fileName}.VCD generado correctamente."));
        }

        private bool IsPs2Iso(string path)
        {
            string name = Path.GetFileName(path).ToUpperInvariant();

            string[] ps2Patterns =
            {
                "SLUS_2", "SLUS_3",
                "SCUS_9",
                "SLES_5", "SLES_6",
                "SCES_5", "SCES_6"
            };

            return ps2Patterns.Any(p => name.Contains(p));
        }
    }

    // ------------------------------
    //  Módulos auxiliares
    // ------------------------------

    public enum SectorMode { Mode1, Mode2Form1, Mode2Form2, Raw2448, Unknown }

    public static class SectorDetector
    {
        public static SectorMode Detect(FileStream input, Action<string> log)
        {
            byte[] buffer = new byte[2448];
            input.Seek(0, SeekOrigin.Begin);

            int read = input.Read(buffer, 0, buffer.Length);
            if (read < 2352)
                return SectorMode.Unknown;

            // RAW 2448
            if (read == 2448)
                return SectorMode.Raw2448;

            // Mode1: sector[15] == 0x01
            if (buffer[15] == 0x01)
                return SectorMode.Mode1;

            // Mode2: sector[15] == 0x02
            if (buffer[15] == 0x02)
            {
                // Form2: user data 2324 bytes
                int form = buffer[18];
                if (form == 0x02)
                    return SectorMode.Mode2Form2;

                return SectorMode.Mode2Form1;
            }

            return SectorMode.Unknown;
        }
    }

    public static class SectorConverter
    {
        public static void Convert(FileStream input, FileStream output, SectorMode mode,
                                   Action<int> updateProgress, Action<string> log)
        {
            int sectorSize = mode == SectorMode.Raw2448 ? 2448 : 2352;
            long total = input.Length / sectorSize;
            long processed = 0;

            byte[] sector = new byte[sectorSize];
            byte[] userData = new byte[2048];

            input.Seek(0, SeekOrigin.Begin);

            while (input.Read(sector, 0, sectorSize) == sectorSize)
            {
                ExtractUserData(sector, userData, mode);
                output.Write(userData, 0, 2048);

                processed++;
                if (processed % 200 == 0)
                {
                    int percent = (int)((processed / (double)total) * 100);
                    updateProgress(percent);
                }
            }

            log($"Conversión completada ({processed} sectores)");
        }

        private static void ExtractUserData(byte[] sector, byte[] userData, SectorMode mode)
        {
            switch (mode)
            {
                case SectorMode.Mode1:
                    Array.Copy(sector, 16, userData, 0, 2048);
                    break;

                case SectorMode.Mode2Form1:
                    Array.Copy(sector, 24, userData, 0, 2048);
                    break;

                case SectorMode.Mode2Form2:
                    // Form2 tiene 2324 bytes, se trunca a 2048
                    Array.Copy(sector, 24, userData, 0, 2048);
                    break;

                case SectorMode.Raw2448:
                    Array.Copy(sector, 24, userData, 0, 2048);
                    break;
            }
        }
    }

    public static class VcdHeader
    {
        public static void Write(FileStream output, string name, long binSize, Action<string> log)
        {
            byte[] header = new byte[0x800];
            Array.Copy(Encoding.ASCII.GetBytes("PSX"), header, 0, 3);

            // Sector count
            int sectors = (int)(binSize / 2352);
            BitConverter.GetBytes(sectors).CopyTo(header, 0x10);

            // Disc label
            var label = Encoding.ASCII.GetBytes(name.ToUpperInvariant());
            Array.Copy(label, 0, header, 0x20, Math.Min(label.Length, 32));

            output.Write(header, 0, header.Length);
            log("Header VCD escrito correctamente");
        }
    }
}
