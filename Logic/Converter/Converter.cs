using POPSManager.Models;
using POPSManager.Services;
using System;
using System.IO;
using System.Linq;

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

            // ---------------------------------------------------------
            //  CUE → BIN
            // ---------------------------------------------------------
            if (inputPath.EndsWith(".cue", StringComparison.OrdinalIgnoreCase))
            {
                var cue = CueParser.Parse(inputPath, log);
                if (cue == null)
                {
                    notify(new UiNotification
                    {
                        Type = NotificationType.Error,
                        Message = $"CUE inválido: {fileName}"
                    });
                    return;
                }

                inputPath = cue.BinPath;
                fileName = Path.GetFileNameWithoutExtension(cue.BinPath);
            }

            // ---------------------------------------------------------
            //  IGNORAR PS2
            // ---------------------------------------------------------
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

            var mode = SectorDetector.Detect(input, log);
            if (mode == SectorMode.Unknown)
            {
                notify(new UiNotification
                {
                    Type = NotificationType.Error,
                    Message = $"Formato de sector desconocido: {fileName}"
                });
                return;
            }

            // ---------------------------------------------------------
            //  HEADER + CONVERSIÓN
            // ---------------------------------------------------------
            VcdHeader.Write(output, fileName, input.Length, log);
            SectorConverter.Convert(input, output, mode, updateProgress, log);

            notify(new UiNotification
            {
                Type = NotificationType.Success,
                Message = $"{fileName}.VCD generado correctamente."
            });
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
}
