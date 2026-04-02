using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace POPSManager.Logic
{
    public class GameProcessor
    {
        private readonly Action<int> updateProgress;
        private readonly Action<string> updateSpinner;
        private readonly Action<string> log;
        private readonly Action<UiNotification>? notify;
        private readonly Logger logger;
        private readonly SpinnerController spinner;

        public GameProcessor(Action<int> updateProgress,
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

        // ============================================================
        //  DETECTAR PLATAFORMA POR GAME ID
        // ============================================================
        private GamePlatform DetectPlatform(string gameId)
        {
            if (Regex.IsMatch(gameId, @"^[A-Z]{4}-\d{5}$"))
                return GamePlatform.PS1;

            if (Regex.IsMatch(gameId, @"^[A-Z]{4}_\d{3}\.\d{2}$"))
                return GamePlatform.PS2;

            return GamePlatform.Unknown;
        }

        private string ConvertPs2ToPs1Id(string ps2Id)
        {
            return ps2Id.Replace("_", "-").Replace(".", "");
        }

        // ============================================================
        //  CREAR ESTRUCTURA DE CARPETAS
        // ============================================================
        private void EnsureFolderStructure(string basePath, GamePlatform platform)
        {
            if (platform == GamePlatform.PS1)
            {
                Directory.CreateDirectory(Path.Combine(basePath, "POPS"));
                Directory.CreateDirectory(Path.Combine(basePath, "POPS", "ART"));
                Directory.CreateDirectory(Path.Combine(basePath, "POPS", "CHEATS"));
            }
            else if (platform == GamePlatform.PS2)
            {
                Directory.CreateDirectory(Path.Combine(basePath, "DVD"));
                Directory.CreateDirectory(Path.Combine(basePath, "CFG"));
                Directory.CreateDirectory(Path.Combine(basePath, "ART"));
            }
        }

        // ============================================================
        //  PROCESAR CARPETA COMPLETA
        // ============================================================
        public async Task ProcessFolder(string popsFolder, string appsFolder)
        {
            await Task.Yield();

            var vcdFiles = Directory.GetFiles(popsFolder, "*.VCD", SearchOption.TopDirectoryOnly);
            var isoFiles = Directory.GetFiles(popsFolder, "*.ISO", SearchOption.TopDirectoryOnly);

            int total = vcdFiles.Length + isoFiles.Length;

            if (total == 0)
            {
                Notify(NotificationType.Warning, "No se encontraron archivos .VCD o .ISO en la carpeta POPS.");
                return;
            }

            Notify(NotificationType.Info, $"Total de juegos encontrados: {total}");

            int current = 0;

            foreach (var vcdPath in vcdFiles)
            {
                current++;
                updateProgress((int)((current / (double)total) * 100));
                await ProcessSingleGame(vcdPath, popsFolder, appsFolder);
            }

            foreach (var isoPath in isoFiles)
            {
                current++;
                updateProgress((int)((current / (double)total) * 100));
                await ProcessSingleGame(isoPath, popsFolder, appsFolder);
            }

            updateProgress(100);
            Notify(NotificationType.Success, "Procesamiento de juegos completado.");
        }

        // ============================================================
        //  PROCESAR JUEGO INDIVIDUAL
        // ============================================================
        private async Task ProcessSingleGame(string filePath, string popsFolder, string appsFolder)
        {
            await Task.Yield();

            string origName = Path.GetFileNameWithoutExtension(filePath);
            string ext = Path.GetExtension(filePath).ToUpperInvariant();

            Notify(NotificationType.Info, $"Juego encontrado: {origName}");

            string? cdTag;
            string cleanName = NameCleaner.Clean(origName, out cdTag);

            logger.Log($"Nombre limpio: {cleanName}");

            // Validación solo para VCD
            if (ext == ".VCD")
            {
                spinner.Start();
                bool ok = IntegrityValidator.Validate(filePath);
                spinner.Stop();

                if (!ok)
                {
                    Notify(NotificationType.Error, "Archivo VCD no supera validación. Juego saltado.");
                    return;
                }
            }

            // Detectar Game ID
            logger.Log("Intentando autodetectar Game ID...");
            string? gameId = GameIdDetector.DetectGameId(filePath);

            if (string.IsNullOrWhiteSpace(gameId))
            {
                Notify(NotificationType.Warning, "No se pudo autodetectar Game ID. Juego saltado.");
                return;
            }

            logger.Log($"Game ID autodetectado: {gameId}");

            var platform = DetectPlatform(gameId);

            if (platform == GamePlatform.Unknown)
            {
                Notify(NotificationType.Error, $"Game ID no corresponde a PS1 ni PS2: {gameId}");
                return;
            }

            // PS2 → convertir ID a formato PS1 para nombres
            if (platform == GamePlatform.PS2)
            {
                logger.Log("Juego PS2 detectado. Convirtiendo Game ID a formato PS1...");
                gameId = ConvertPs2ToPs1Id(gameId);
                logger.Log($"Nuevo Game ID: {gameId}");
            }

            EnsureFolderStructure(popsFolder, platform);

            if (platform == GamePlatform.PS1)
                ProcessPs1(filePath, popsFolder, appsFolder, gameId, cleanName, ext);
            else
                ProcessPs2(filePath, popsFolder, gameId, cleanName);

            Notify(NotificationType.Success, $"Juego procesado correctamente: {gameId}.{cleanName}");
        }

        // ============================================================
        //  PROCESAR PS1
        // ============================================================
        private void ProcessPs1(string filePath,
                               string popsFolder,
                               string appsFolder,
                               string gameId,
                               string cleanName,
                               string ext)
        {
            string newFileName = $"{gameId}.{cleanName}{ext}";
            string newPath = Path.Combine(popsFolder, newFileName);

            try
            {
                if (!File.Exists(newPath))
                    File.Move(filePath, newPath);
            }
            catch (Exception ex)
            {
                Notify(NotificationType.Error, $"ERROR al renombrar VCD: {ex.Message}");
                return;
            }

            string gameFolder = Path.Combine(popsFolder, $"{gameId}.{cleanName}");
            Directory.CreateDirectory(gameFolder);

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
                    Notify(NotificationType.Info, "CHEAT.TXT generado (PAL → NTSC).");
                }
                catch (Exception ex)
                {
                    Notify(NotificationType.Error, $"ERROR al crear CHEAT.TXT: {ex.Message}");
                }
            }

            // Generar ELF real
            try
            {
                string appGameFolder = Path.Combine(appsFolder, $"{gameId}.{cleanName}");
                Directory.CreateDirectory(appGameFolder);

                string baseElf = Path.Combine(AppContext.BaseDirectory, "Tools", "POPSTARTER.ELF");
                string elfPath = Path.Combine(appGameFolder, "BOOT.ELF");

                string vcdInternalPath = $"mass:/POPS/{newFileName}";

                bool elfOk = ElfGenerator.GenerateElf(baseElf, elfPath, gameId, vcdInternalPath, logger.Log);

                if (!elfOk)
                    Notify(NotificationType.Error, "No se pudo generar ELF real.");
                else
                    Notify(NotificationType.Success, $"ELF generado en APP: {elfPath}");
            }
            catch (Exception ex)
            {
                Notify(NotificationType.Error, $"ERROR al crear ELF: {ex.Message}");
            }
        }

        // ============================================================
        //  PROCESAR PS2
        // ============================================================
        private void ProcessPs2(string filePath,
                               string popsFolder,
                               string gameId,
                               string cleanName)
        {
            string dvdFolder = Path.Combine(popsFolder, "DVD");
            Directory.CreateDirectory(dvdFolder);

            string newIsoPath = Path.Combine(dvdFolder, $"{gameId}.ISO");

            try
            {
                File.Move(filePath, newIsoPath, true);
                Notify(NotificationType.Info, $"ISO PS2 movido a DVD: {gameId}.ISO");
            }
            catch (Exception ex)
            {
                Notify(NotificationType.Error, $"ERROR moviendo ISO PS2: {ex.Message}");
            }

            string cfgFolder = Path.Combine(popsFolder, "CFG");
            Directory.CreateDirectory(cfgFolder);

            string cfgPath = Path.Combine(cfgFolder, $"{gameId}.CFG");
            try
            {
                File.WriteAllText(cfgPath, "// Configuración PS2");
                logger.Log("CFG generado para PS2.");
            }
            catch (Exception ex)
            {
                Notify(NotificationType.Error, $"ERROR al crear CFG de PS2: {ex.Message}");
            }
        }

        // ============================================================
        //  DETECTAR REGIÓN
        // ============================================================
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

    public enum GamePlatform
    {
        PS1,
        PS2,
        Unknown
    }
}
