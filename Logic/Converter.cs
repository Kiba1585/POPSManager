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
        private readonly Logger logger;
        private readonly SpinnerController spinner;

        public Converter(Action<int> updateProgress,
                         Action<string> updateSpinner,
                         Action<string> log)
        {
            this.updateProgress = updateProgress;
            this.updateSpinner = updateSpinner;
            this.log = log;
            this.logger = new Logger(log);
            this.spinner = new SpinnerController(updateSpinner);
        }

        // ============================================================
        //  CONVERTIR CARPETA COMPLETA
        // ============================================================
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
                logger.Log("No se encontraron archivos BIN/CUE/ISO.");
                return;
            }

            logger.Log($"Archivos detectados: {files.Length}");

            int current = 0;

            foreach (var file in files)
            {
                current++;
                int percent = (int)((current / (double)files.Length) * 100);
                updateProgress(percent);

                await ConvertSingle(file, outputFolder);
            }

            updateProgress(100);
        }

        // ============================================================
        //  CONVERTIR ARCHIVO INDIVIDUAL
        // ============================================================
        private async Task ConvertSingle(string inputPath, string outputFolder)
        {
            await Task.Yield();

            string name = Path.GetFileNameWithoutExtension(inputPath);
            string ext = Path.GetExtension(inputPath).ToLowerInvariant();
            string outputPath = Path.Combine(outputFolder, name + ".VCD");

            logger.Log("-----------------------------------------");
            logger.Log($"Convirtiendo: {name} ({ext})");

            // Resolver CUE → BIN
            if (ext == ".cue")
            {
                string? binFromCue = ResolveBinFromCue(inputPath);
                if (binFromCue == null)
                {
                    logger.Log("ERROR: No se pudo resolver BIN desde el CUE.");
                    return;
                }

                inputPath = binFromCue;
                ext = ".bin";
                logger.Log($"CUE resuelto a BIN: {Path.GetFileName(binFromCue)}");
            }

            // ============================================================
            //  DETECTAR SI EL ISO ES PS1 O PS2
            // ============================================================
            if (ext == ".iso")
            {
                string? gameId = GameIdDetector.DetectGameId(inputPath);

                if (string.IsNullOrWhiteSpace(gameId))
                {
                    logger.Log("ISO sin Game ID válido. No se convierte.");
                    return;
                }

                // PS2 → NO convertir
                if (Regex.IsMatch(gameId, @"^[A-Z]{4}_\d{3}\.\d{2}$"))
                {
                    logger.Log($"ISO detectado como PS2 ({gameId}). No se convierte.");
                    return;
                }

                // PS1 → convertir
                logger.Log($"ISO detectado como PS1 ({gameId}). Convirtiendo a VCD...");
            }

            spinner.Start();

            try
            {
                Directory.CreateDirectory(outputFolder);

                using var input = File.OpenRead(inputPath);
                using var output = File.Create(outputPath);

                // Encabezado POPStarter
                byte[] header = new byte[0x800];
                Array.Copy(Encoding.ASCII.GetBytes("POPSTARTER"), header, "POPSTARTER".Length);
                output.Write(header, 0, header.Length);

                if (ext == ".iso")
                {
                    await CopyIso2048Async(input, output);
                }
                else if (ext == ".bin")
                {
                    await CopyBin2352As2048Async(input, output);
                }
                else
                {
                    logger.Log("Extensión no soportada para conversión.");
                    return;
                }

                logger.Log($"Conversión completada: {name}.VCD");
            }
            catch (Exception ex)
            {
                logger.Log($"ERROR al convertir {name}: {ex.Message}");
            }
            finally
            {
                spinner.Stop();
            }
        }

        // ============================================================
        //  COPIAR ISO (2048 bytes por sector)
        // ============================================================
        private async Task CopyIso2048Async(Stream input, Stream output)
        {
            const int sectorSize = 2048;
            byte[] buffer = new byte[sectorSize];

            long totalSectors = input.Length / sectorSize;
            long processed = 0;

            int read;
            while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await output.WriteAsync(buffer, 0, read);

                processed++;
                if (processed % 200 == 0 && totalSectors > 0)
                {
                    int percent = (int)((processed / (double)totalSectors) * 100);
                    updateProgress(percent);
                }
            }
        }

        // ============================================================
        //  COPIAR BIN (2352 → 2048)
        // ============================================================
        private async Task CopyBin2352As2048Async(Stream input, Stream output)
        {
            const int rawSectorSize = 2352;
            const int userDataSize = 2048;
            const int userDataOffset = 24;

            byte[] sector = new byte[rawSectorSize];
            byte[] userData = new byte[userDataSize];

            long totalSectors = input.Length / rawSectorSize;
            long processed = 0;

            while (await input.ReadAsync(sector, 0, rawSectorSize) == rawSectorSize)
            {
                Array.Copy(sector, userDataOffset, userData, 0, userDataSize);
                await output.WriteAsync(userData, 0, userDataSize);

                processed++;
                if (processed % 200 == 0 && totalSectors > 0)
                {
                    int percent = (int)((processed / (double)totalSectors) * 100);
                    updateProgress(percent);
                }
            }
        }

        // ============================================================
        //  RESOLVER CUE → BIN
        // ============================================================
        private string? ResolveBinFromCue(string cuePath)
        {
            try
            {
                var lines = File.ReadAllLines(cuePath);
                foreach (var line in lines)
                {
                    if (line.TrimStart().StartsWith("FILE", StringComparison.OrdinalIgnoreCase))
                    {
                        int firstQuote = line.IndexOf('"');
                        int lastQuote = line.LastIndexOf('"');
                        if (firstQuote >= 0 && lastQuote > firstQuote)
                        {
                            string fileName = line.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
                            string binPath = Path.Combine(Path.GetDirectoryName(cuePath)!, fileName);
                            return File.Exists(binPath) ? binPath : null;
                        }
                    }
                }
            }
            catch
            {
                // ignorado
            }

            return null;
        }
    }
}                }

                logger.Log($"Conversión completada: {name}.VCD");
            }
            catch (Exception ex)
            {
                logger.Log($"ERROR al convertir {name}: {ex.Message}");
            }
            finally
            {
                spinner.Stop();
            }
        }
    }
}
