using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace POPSManager.Logic
{
    public class GameProcessor
    {
        private readonly Action<int> updateProgress;
        private readonly Action<string> updateSpinner;
        private readonly Action<string> log;
        private readonly Logger logger;
        private readonly SpinnerController spinner;

        public GameProcessor(Action<int> updateProgress,
                             Action<string> updateSpinner,
                             Action<string> log)
        {
            this.updateProgress = updateProgress;
            this.updateSpinner = updateSpinner;
            this.log = log;
            this.logger = new Logger(log);
            this.spinner = new SpinnerController(updateSpinner);
        }

        public async Task ProcessFolder(string popsFolder, string appsFolder)
        {
            await Task.Yield();

            var vcdFiles = Directory.GetFiles(popsFolder, "*.VCD", SearchOption.TopDirectoryOnly);

            if (vcdFiles.Length == 0)
            {
                logger.Log("No se encontraron archivos .VCD en la carpeta POPS.");
                return;
            }

            logger.Log($"Total de juegos encontrados: {vcdFiles.Length}");

            int current = 0;

            foreach (var vcdPath in vcdFiles)
            {
                current++;
                int percent = (int)((current / (double)vcdFiles.Length) * 100);
                updateProgress(percent);

                await ProcessSingleGame(vcdPath, popsFolder, appsFolder);
            }

            updateProgress(100);
        }

        private async Task ProcessSingleGame(string vcdPath, string popsFolder, string appsFolder)
        {
            await Task.Yield();

            string origName = Path.GetFileNameWithoutExtension(vcdPath);
            string origExt = Path.GetExtension(vcdPath);

            logger.Log("-----------------------------------------");
            logger.Log($"Juego encontrado: {origName}");

            string? cdTag;
            string cleanName = NameCleaner.Clean(origName, out cdTag);
            logger.Log($"Nombre limpio: {cleanName}");

            spinner.Start();
            bool ok = IntegrityValidator.Validate(vcdPath);
            spinner.Stop();

            if (!ok)
            {
                logger.Log("ERROR: Archivo no supera la validación de integridad. Saltando...");
                return;
            }

            logger.Log("Intentando autodetectar Game ID...");
            string? gameId = GameIdDetector.DetectGameId(vcdPath);

            if (string.IsNullOrWhiteSpace(gameId))
            {
                logger.Log("No se pudo autodetectar Game ID. Juego saltado.");
                return;
            }

            logger.Log($"Game ID autodetectado: {gameId}");

            if (!IsValidGameId(gameId))
            {
                logger.Log($"ERROR: Game ID inválido ({gameId}). Juego saltado.");
                return;
            }

            logger.Log($"Game ID válido: {gameId}");

            string newFileName = $"{gameId}.{cleanName}{origExt}";
            string newPath = Path.Combine(popsFolder, newFileName);

            try
            {
                if (!File.Exists(newPath))
                    File.Move(vcdPath, newPath);
                else
                {
                    logger.Log("ADVERTENCIA: Ya existe un archivo con el nombre final. No se renombra.");
                    newPath = vcdPath;
                }
            }
            catch (Exception ex)
            {
                logger.Log($"ERROR al renombrar: {ex.Message}");
                return;
            }

            string gameFolder = Path.Combine(popsFolder, $"{gameId}.{cleanName}");
            try
            {
                Directory.CreateDirectory(gameFolder);
            }
            catch (Exception ex)
            {
                logger.Log($"ERROR al crear carpeta del juego: {ex.Message}");
            }

            string region = DetectRegion(gameId);
            logger.Log($"Región detectada: {region}");

            if (region == "PAL")
            {
                string cheatPath = Path.Combine(gameFolder, "CHEAT.TXT");
                try
                {
                    File.WriteAllLines(cheatPath, new[]
                    {
                        "$NOPAL",
                        "$VMODE_6",
                        "$FORCE_NTSC",
                        "$YPOS_12"
                    });
                    logger.Log("CHEAT.TXT generado (PAL → NTSC).");
                }
                catch (Exception ex)
                {
                    logger.Log($"ERROR al crear CHEAT.TXT: {ex.Message}");
                }
            }
            else
            {
                logger.Log("NTSC detectado: no se creó CHEAT.TXT.");
            }

            try
            {
                Directory.CreateDirectory(appsFolder);
                string elfName = $"{gameId}.{cleanName}.elf.NTSC";
                string elfPath = Path.Combine(appsFolder, elfName);

                if (!File.Exists(elfPath))
                    File.WriteAllBytes(elfPath, Array.Empty<byte>());

                logger.Log($"ELF creado: {elfName}");
            }
            catch (Exception ex)
            {
                logger.Log($"ERROR al crear ELF: {ex.Message}");
            }

            logger.Log("Juego procesado correctamente.");
        }

        private bool IsValidGameId(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                return false;

            if (gameId.Length != 10)
                return false;

            string prefix = gameId.Substring(0, 5).ToUpperInvariant();
            string[] validPrefixes =
            {
                "SCES-", "SLES-", "SCED-", "SLED-",
                "SCUS-", "SLUS-", "SCPS-", "SLPS-", "SLPM-"
            };

            if (!validPrefixes.Contains(prefix))
                return false;

            string numeric = gameId.Substring(5, 5);
            return numeric.All(char.IsDigit);
        }

        private string DetectRegion(string gameId)
        {
            string prefix = gameId.Substring(0, 5).ToUpperInvariant();
            return prefix switch
            {
                "SCES-" or "SLES-" or "SCED-" or "SLED-" => "PAL",
                _ => "NTSC"
            };
        }
    }
}
