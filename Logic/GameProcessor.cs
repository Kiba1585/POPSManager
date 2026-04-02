using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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

        // ============================================================
        //  NUEVO: Detectar plataforma por Game ID
        // ============================================================
        private GamePlatform DetectPlatform(string gameId)
        {
            if (Regex.IsMatch(gameId, @"^[A-Z]{4}-\d{5}$"))
                return GamePlatform.PS1;

            if (Regex.IsMatch(gameId, @"^[A-Z]{4}_\d{3}\.\d{2}$"))
                return GamePlatform.PS2;

            return GamePlatform.Unknown;
        }

        // ============================================================
        //  NUEVO: Convertir Game ID PS2 → PS1
        // ============================================================
        private string ConvertPs2ToPs1Id(string ps2Id)
        {
            return ps2Id.Replace("_", "-").Replace(".", "");
        }

        // ============================================================
        //  NUEVO: Crear estructura de carpetas
        // ============================================================
        private void EnsureFolderStructure(string basePath, GamePlatform platform)
        {
            if (platform == GamePlatform.PS1)
            {
                Directory.CreateDirectory(Path.Combine(basePath, "POPS"));
                Directory.CreateDirectory(Path.Combine(basePath, "POPS", "ART"));
                Directory.CreateDirectory(Path.Combine(basePath, "POPS", "CHEATS"));
                Directory.CreateDirectory(Path.Combine(basePath, "POPS", "ELFS"));
            }
            else if (platform == GamePlatform.PS2)
            {
                Directory.CreateDirectory(Path.Combine(basePath, "DVD"));
                Directory.CreateDirectory(Path.Combine(basePath, "ART"));
                Directory.CreateDirectory(Path.Combine(basePath, "CFG"));
            }
        }

        // ============================================================
        //  PROCESAR CARPETA (PS1 + PS2)
        // ============================================================
        public async Task ProcessFolder(string popsFolder, string appsFolder)
        {
            await Task.Yield();

            var vcdFiles = Directory.GetFiles(popsFolder, "*.VCD", SearchOption.TopDirectoryOnly);
            var isoFiles = Directory.GetFiles(popsFolder, "*.ISO", SearchOption.TopDirectoryOnly);

            int total = vcdFiles.Length + isoFiles.Length;

            if (total == 0)
            {
                logger.Log("No se encontraron archivos .VCD o .ISO.");
                return;
            }

            logger.Log($"Total de juegos encontrados: {total}");

            int current = 0;

            // Procesar PS1
            foreach (var vcdPath in vcdFiles)
            {
                current++;
                updateProgress((int)((current / (double)total) * 100));
                await ProcessSingleGame(vcdPath, popsFolder, appsFolder);
            }

            // Procesar PS2
            foreach (var isoPath in isoFiles)
            {
                current++;
                updateProgress((int)((current / (double)total) * 100));
                await ProcessSingleGame(isoPath, popsFolder, appsFolder);
            }

            updateProgress(100);
        }

        // ============================================================
        //  PROCESAR JUEGO INDIVIDUAL (PS1 o PS2)
        // ============================================================
        private async Task ProcessSingleGame(string filePath, string popsFolder, string appsFolder)
        {
            await Task.Yield();

            string origName = Path.GetFileNameWithoutExtension(filePath);
            string ext = Path.GetExtension(filePath).ToUpperInvariant();

            logger.Log("-----------------------------------------");
            logger.Log($"Juego encontrado: {origName}");

            // Limpieza de nombre
            string? cdTag;
            string cleanName = NameCleaner.Clean(origName, out cdTag);
            logger.Log($"Nombre limpio: {cleanName}");

            // Validación de integridad (solo PS1)
            if (ext == ".VCD")
            {
                spinner.Start();
                bool ok = IntegrityValidator.Validate(filePath);
                spinner.Stop();

                if (!ok)
                {
                    logger.Log("ERROR: Archivo VCD no supera validación. Saltando...");
                    return;
                }
            }

            // Autodetectar Game ID
            logger.Log("Intentando autodetectar Game ID...");
            string? gameId = GameIdDetector.DetectGameId(filePath);

            if (string.IsNullOrWhiteSpace(gameId))
            {
                logger.Log("No se pudo autodetectar Game ID. Saltando...");
                return;
            }

            logger.Log($"Game ID autodetectado: {gameId}");

            // Detectar plataforma
            var platform = DetectPlatform(gameId);

            if (platform == GamePlatform.Unknown)
            {
                logger.Log("ERROR: Game ID no corresponde a PS1 ni PS2.");
                return;
            }

            // Convertir PS2 → PS1
            if (platform == GamePlatform.PS2)
            {
                logger.Log("Juego PS2 detectado. Convirtiendo Game ID a formato PS1...");
                gameId = ConvertPs2ToPs1Id(gameId);
                logger.Log($"Nuevo Game ID: {gameId}");
            }

            // Crear carpetas necesarias
            EnsureFolderStructure(popsFolder, platform);

            // Procesar según plataforma
            if (platform == GamePlatform.PS1)
                ProcessPs1(filePath, popsFolder, appsFolder, gameId, cleanName, ext);
            else
                ProcessPs2(filePath, popsFolder, gameId, cleanName);

            logger.Log("Juego procesado correctamente.");
        }

        // ============================================================
        //  PROCESAR PS1
        // ============================================================
        private void ProcessPs1(string filePath, string popsFolder, string appsFolder,
                                string gameId, string cleanName, string ext)
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
                logger.Log($"ERROR al renombrar: {ex.Message}");
                return;
            }

            string gameFolder = Path.Combine(popsFolder, $"{gameId}.{cleanName}");
            Directory.CreateDirectory(gameFolder);

            // Región
            string region = DetectRegion(gameId);
            logger.Log($"Región detectada: {region}");

            // CHEAT.TXT para PAL
            if (region == "PAL")
            {
                File.WriteAllLines(Path.Combine(gameFolder, "CHEAT.TXT"), new[]
                {
                    "$NOPAL",
                    "$VMODE_6",
                    "$FORCE_NTSC",
                    "$YPOS_12"
                });
                logger.Log("CHEAT.TXT generado (PAL → NTSC).");
            }

            // Crear ELF
            Directory.CreateDirectory(appsFolder);
            string elfName = $"{gameId}.{cleanName}.elf.NTSC";
            string elfPath = Path.Combine(appsFolder, elfName);

            if (!File.Exists(elfPath))
                File.WriteAllBytes(elfPath, Array.Empty<byte>());

            logger.Log($"ELF creado: {elfName}");
        }

        // ============================================================
        //  PROCESAR PS2
        // ============================================================
        private void ProcessPs2(string filePath, string popsFolder, string gameId, string cleanName)
        {
            string dvdFolder = Path.Combine(popsFolder, "DVD");
            Directory.CreateDirectory(dvdFolder);

            string newPath = Path.Combine(dvdFolder, $"{gameId}.ISO");

            try
            {
                File.Move(filePath, newPath, true);
                logger.Log($"ISO movido a DVD/: {gameId}.ISO");
            }
            catch (Exception ex)
            {
                logger.Log($"ERROR moviendo ISO: {ex.Message}");
            }

            // Crear CFG
            string cfgFolder = Path.Combine(popsFolder, "CFG");
            Directory.CreateDirectory(cfgFolder);

            string cfgPath = Path.Combine(cfgFolder, $"{gameId}.CFG");
            File.WriteAllText(cfgPath, "// Configuración PS2");

            logger.Log("CFG generado para PS2.");
        }

        // ============================================================
        //  VALIDACIÓN GAME ID
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
}            string newPath = Path.Combine(popsFolder, newFileName);

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
