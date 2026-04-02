using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public class Converter
    {
        private readonly Action<int> updateProgress;
        private readonly Action<string> updateSpinner;
        private readonly Action<string> log;
        private readonly Action<UiNotification>? notify;
        private readonly Logger logger;
        private readonly SpinnerController spinner;

        public Converter(Action<int> updateProgress,
                         Action<string> updateSpinner,
                         Action<string> log,
                         Action<UiNotification>? notify = null)
        {
            this.updateProgress = updateProgress;
            this.updateSpinner = updateSpinner;
            this.log = log;
            this.notify = notify;
            this.logger = new Logger(log);
            this.spinner = new SpinnerController(updateSpinner);
        }

        private void Notify(NotificationType type, string message)
        {
            notify?.Invoke(new UiNotification(type, message));
            logger.Log(message);
        }

        public async Task ConvertFolder(string sourceFolder, string outputFolder)
        {
            await Task.Yield();

            var files = Directory.GetFiles(sourceFolder)
                                 .Where(f => f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".cue", StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                                 .ToArray();

            if (files.Length == 0)
            {
                Notify(NotificationType.Warning, "No se encontraron archivos BIN/CUE/ISO.");
                return;
            }

            Notify(NotificationType.Info, $"Archivos detectados: {files.Length}");

            int current = 0;

            foreach (var file in files)
            {
                current++;
                updateProgress((int)((current / (double)files.Length) * 100));
                await ConvertSingle(file, outputFolder);
            }

            updateProgress(100);
            Notify(NotificationType.Success, "Conversión completada.");
        }

        private async Task ConvertSingle(string inputPath, string outputFolder)
        {
            await Task.Yield();

            string name = Path.GetFileNameWithoutExtension(inputPath);
            string ext = Path.GetExtension(inputPath).ToLowerInvariant();
            string outputPath = Path.Combine(outputFolder, name + ".VCD");

            Notify(NotificationType.Info, $"Convirtiendo: {name} ({ext})");

            if (ext == ".cue")
            {
                string? binFromCue = ResolveBinFromCue(inputPath);
                if (binFromCue == null)
                {
                    Notify(NotificationType.Error, "No se pudo resolver BIN desde CUE.");
                    return;
                }

                inputPath = binFromCue;
                ext = ".bin";
            }

            if (ext == ".iso")
            {
                string? gameId = GameIdDetector.DetectGameId(inputPath);

                if (string.IsNullOrWhiteSpace(gameId))
                {
                    Notify(NotificationType.Warning, "ISO sin Game ID válido. No se convierte.");
                    return;
                }

                if (Regex.IsMatch(gameId, @"^[A-Z]{4}_\d{3}\.\d{2}$"))
                {
                    Notify(NotificationType.Info, $"ISO PS2 detectado ({gameId}). No se convierte.");
                    return;
                }

                Notify(NotificationType.Info, $"ISO PS1 detectado ({gameId}). Convirtiendo...");
            }

            spinner.Start();

            try
            {
                Directory.CreateDirectory(outputFolder);

                using var input = File.OpenRead(inputPath);
                using var output = File.Create(outputPath);

                byte[] header = new byte[0x800];
                Array.Copy(Encoding.ASCII.GetBytes("POPSTARTER"), header, "POPSTARTER".Length);
                output.Write(header, 0, header.Length);

                if (ext == ".iso")
                    await CopyIso2048Async(input, output);
                else if (ext == ".bin")
                    await CopyBin2352As2048Async(input, output);
                else
                {
                    Notify(NotificationType.Warning, "Extensión no soportada.");
                    return;
                }

                Notify(NotificationType.Success, $"VCD generado: {name}.VCD");
            }
            catch (Exception ex)
            {
                Notify(NotificationType.Error, $"ERROR: {ex.Message}");
            }
            finally
            {
                spinner.Stop();
            }
        }

        private async Task CopyIso2048Async(Stream input, Stream output)
        {
            const int sectorSize = 2048;
            byte[] buffer = new byte[sectorSize];

            while (true)
            {
                int read = await input.ReadAsync(buffer, 0, buffer.Length);
                if (read <= 0) break;
                await output.WriteAsync(buffer, 0, read);
            }
        }

        private async Task CopyBin2352As2048Async(Stream input, Stream output)
        {
            const int raw = 2352;
            const int user = 2048;
            const int offset = 24;

            byte[] sector = new byte[raw];
            byte[] userData = new byte[user];

            while (await input.ReadAsync(sector, 0, raw) == raw)
            {
                Array.Copy(sector, offset, userData, 0, user);
                await output.WriteAsync(userData, 0, user);
            }
        }

        private string? ResolveBinFromCue(string cuePath)
        {
            try
            {
                foreach (var line in File.ReadAllLines(cuePath))
                {
                    if (line.TrimStart().StartsWith("FILE", StringComparison.OrdinalIgnoreCase))
                    {
                        int a = line.IndexOf('"');
                        int b = line.LastIndexOf('"');
                        if (a >= 0 && b > a)
                        {
                            string file = line.Substring(a + 1, b - a - 1);
                            string bin = Path.Combine(Path.GetDirectoryName(cuePath)!, file);
                            return File.Exists(bin) ? bin : null;
                        }
                    }
                }
            }
            catch { }

            return null;
        }
    }
}
