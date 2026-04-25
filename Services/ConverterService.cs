using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using POPSManager.Logic;
using POPSManager.Logic.Automation;
using POPSManager.Models;
using POPSManager.UI.Localization;

namespace POPSManager.Services
{
    public sealed class ConverterService
    {
        private readonly Action<string> _log;
        private readonly PathsService _paths;
        private readonly SettingsService _settings;
        private readonly AutomationEngine _auto;
        private readonly LocalizationService _loc;

        private readonly Action<string, NotificationType> _notify;
        private readonly Action<string> _setStatus;

        private const int SectorSize = 2352;
        private const int UserDataSize = 2048;
        private const int HeaderSize = 0x800;
        private const int BufferSize = 1024 * 1024; // 1 MB

        public ConverterService(
            Action<string> log,
            PathsService paths,
            SettingsService settings,
            AutomationEngine auto,
            LocalizationService loc,
            Action<string, NotificationType> notify,
            Action<string> setStatus)
        {
            _log = log;
            _paths = paths;
            _settings = settings;
            _auto = auto;
            _loc = loc;
            _notify = notify;
            _setStatus = setStatus;
        }

        public async Task ConvertFolderAsync(string sourceFolder, string outputFolder, CancellationToken ct = default)
        {
            if (!Directory.Exists(sourceFolder))
            {
                _notify(_loc.GetString("Converter_InvalidSourceFolder"), NotificationType.Error);
                return;
            }

            if (!_auto.ShouldConvert())
            {
                _log("[Convert] Automatización de conversión desactivada.");
                _notify(_loc.GetString("Converter_ConversionCancelledByAutomation"), NotificationType.Warning);
                return;
            }

            // Determinar carpeta de trabajo (temporal si el destino es extraíble)
            string workFolder = outputFolder;
            bool useTempFolder = _settings.UseTempFolderForConversion && IsRemovableDrive(outputFolder);
            string? tempFolder = null;

            if (useTempFolder)
            {
                tempFolder = _settings.TempFolder;
                if (string.IsNullOrWhiteSpace(tempFolder) || !Directory.Exists(tempFolder))
                {
                    tempFolder = Path.Combine(Path.GetTempPath(), "POPSManager");
                }
                Directory.CreateDirectory(tempFolder);
                workFolder = tempFolder;
                _log($"[Convert] Usando carpeta temporal: {workFolder}");
            }

            Directory.CreateDirectory(workFolder);

            var files = Directory.GetFiles(sourceFolder)
                .Where(f =>
                    f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".cue", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f)
                .ToArray();

            if (files.Length == 0)
            {
                _notify(_loc.GetString("Converter_NoFilesFound"), NotificationType.Warning);
                return;
            }

            int total = files.Length;
            int index = 0;

            int maxParallel = Math.Min(Environment.ProcessorCount, 2);

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxParallel,
                CancellationToken = ct
            };

            // Cola de copia final si usamos carpeta temporal
            Task? copyTask = null;

            try
            {
                await Parallel.ForEachAsync(files, options, async (file, token) =>
                {
                    token.ThrowIfCancellationRequested();

                    int current = Interlocked.Increment(ref index);
                    _setStatus(string.Format(_loc.GetString("Converter_ConvertingStatus"), current, total, Path.GetFileName(file)));

                    try
                    {
                        var result = await ConvertToVcdAsync(file, workFolder).ConfigureAwait(false);

                        if (result != null)
                        {
                            _log($"Convertido: {file} → {result.VcdPath}");

                            // Si estamos usando carpeta temporal, copiar al destino final
                            if (useTempFolder && tempFolder != null)
                            {
                                string finalPath = Path.Combine(outputFolder, Path.GetFileName(result.VcdPath));
                                // Copiar en background sin esperar
                                _ = Task.Run(() =>
                                {
                                    try
                                    {
                                        File.Copy(result.VcdPath, finalPath, true);
                                        _log($"[Copy] VCD copiado a destino final: {finalPath}");
                                    }
                                    catch (Exception ex)
                                    {
                                        _log($"[ERROR] Copiando VCD al destino final: {ex.Message}");
                                    }
                                }, CancellationToken.None);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log($"ERROR al convertir {file}: {ex.Message}");
                        _notify(string.Format(_loc.GetString("Converter_ErrorConvertingFile"), Path.GetFileName(file)), NotificationType.Error);
                    }
                }).ConfigureAwait(false);

                // Esperar a que terminen las copias pendientes (si las hay)
                if (copyTask != null)
                    await copyTask.ConfigureAwait(false);

                _notify(_loc.GetString("Converter_ConversionCompleted"), NotificationType.Success);
                _setStatus(_loc.GetString("Converter_ConversionFinished"));
            }
            catch (OperationCanceledException)
            {
                _notify(_loc.GetString("Converter_ConversionCancelled"), NotificationType.Warning);
                _log("[Convert] Conversión cancelada por el usuario.");
            }
            finally
            {
                // Limpiar carpeta temporal si se usó
                if (useTempFolder && tempFolder != null && Directory.Exists(tempFolder))
                {
                    try { Directory.Delete(tempFolder, true); }
                    catch (Exception ex) { _log($"[Convert] No se pudo eliminar carpeta temporal: {ex.Message}"); }
                }
            }
        }

        public void ConvertFolder(string sourceFolder, string outputFolder)
        {
            ConvertFolderAsync(sourceFolder, outputFolder)
                .GetAwaiter()
                .GetResult();
        }

        private async Task<ConvertedGame?> ConvertToVcdAsync(string inputPath, string outputFolder)
        {
            string ext = Path.GetExtension(inputPath).ToLowerInvariant();

            if (ext == ".iso" && IsPs2Iso(inputPath))
            {
                _log($"Detectado PS2 ISO, no se convierte: {inputPath}");
                return null;
            }

            var discInfo = DetectDiscNumber(inputPath);

            string rawName = Path.GetFileNameWithoutExtension(inputPath);
            string baseName = NameCleanerBase.CleanTitleOnly(CleanBaseName(rawName));

            string vcdName = discInfo.IsDisc
                ? $"{baseName} (CD{discInfo.DiscNumber}).vcd"
                : $"{baseName}.vcd";

            string outputPath = Path.Combine(outputFolder, vcdName);

            await ConvertPs1ToVcdAsync(inputPath, outputPath, baseName).ConfigureAwait(false);

            return new ConvertedGame
            {
                VcdPath = outputPath,
                BaseName = baseName,
                DiscNumber = discInfo.DiscNumber,
                IsMultiDisc = discInfo.IsDisc
            };
        }

        private async Task ConvertPs1ToVcdAsync(string inputPath, string outputPath, string name)
        {
            await using var input = File.OpenRead(inputPath);
            await using var output = File.Create(outputPath);

            byte[] header = new byte[HeaderSize];
            Array.Copy(Encoding.ASCII.GetBytes("PSX"), header, 3);
            await output.WriteAsync(header, 0, header.Length).ConfigureAwait(false);

            long totalSectors = input.Length / SectorSize;
            long processed = 0;

            byte[] outputBuffer = new byte[BufferSize];
            int bufferPos = 0;

            byte[] sector = new byte[SectorSize];

            var lastReport = System.Diagnostics.Stopwatch.StartNew();
            const int reportIntervalMs = 500;

            while (true)
            {
                int read = await input.ReadAsync(sector, 0, SectorSize).ConfigureAwait(false);
                if (read == 0) break;

                if (read != SectorSize)
                {
                    _log($"[WARN] Sector incompleto detectado en {name}");
                    break;
                }

                if (bufferPos + UserDataSize > outputBuffer.Length)
                {
                    await output.WriteAsync(outputBuffer, 0, bufferPos).ConfigureAwait(false);
                    bufferPos = 0;
                }

                Buffer.BlockCopy(sector, 24, outputBuffer, bufferPos, UserDataSize);
                bufferPos += UserDataSize;

                processed++;

                if (lastReport.ElapsedMilliseconds >= reportIntervalMs)
                {
                    int percent = (int)((processed / (double)totalSectors) * 100);
                    _setStatus(string.Format(_loc.GetString("Converter_ConvertingPercentage"), name, percent));
                    lastReport.Restart();
                }
            }

            if (bufferPos > 0)
                await output.WriteAsync(outputBuffer, 0, bufferPos).ConfigureAwait(false);
        }

        private static bool IsRemovableDrive(string path)
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(path)!);
                return drive.DriveType == DriveType.Removable;
            }
            catch { return false; }
        }

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