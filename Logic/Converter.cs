using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task ConvertFolder(string sourceFolder, string outputFolder)
        {
            await Task.Yield(); // Evita CS1998

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

        private async Task ConvertSingle(string inputPath, string outputFolder)
        {
            await Task.Yield(); // Evita CS1998

            string name = Path.GetFileNameWithoutExtension(inputPath);
            string outputPath = Path.Combine(outputFolder, name + ".VCD");

            logger.Log("-----------------------------------------");
            logger.Log($"Convirtiendo: {name}");

            spinner.Start();

            try
            {
                using var input = File.OpenRead(inputPath);
                using var output = File.Create(outputPath);

                // Escribir encabezado POPStarter
                byte[] header = new byte[0x800];
                Array.Copy(System.Text.Encoding.ASCII.GetBytes("PSX"), header, 3);
                output.Write(header, 0, header.Length);

                // Copiar sectores de 2352 bytes → 2048 bytes
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
}            }
        }
    }
}
