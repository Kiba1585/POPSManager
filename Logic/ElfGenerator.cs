using System;
using System.IO;
using System.Text;

namespace POPSManager.Logic
{
    public static class ElfGenerator
    {
        // ============================================================
        //  API PRINCIPAL (SOLO PS1, ULTRA PRO)
        // ============================================================
        public static bool GeneratePs1Elf(
            string baseElfPath,
            string vcdFullPath,
            string appsFolder,
            int discNumber,
            Action<string> log)
        {
            try
            {
                // -----------------------------
                // Validaciones iniciales
                // -----------------------------
                if (!File.Exists(baseElfPath))
                {
                    log($"[ELF] ERROR: POPSTARTER.ELF base no encontrado → {baseElfPath}");
                    return false;
                }

                if (!File.Exists(vcdFullPath))
                {
                    log($"[ELF] ERROR: VCD no encontrado → {vcdFullPath}");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(appsFolder))
                {
                    log("[ELF] ERROR: Carpeta APPS inválida.");
                    return false;
                }

                Directory.CreateDirectory(appsFolder);

                // -----------------------------
                // Datos del juego
                // -----------------------------
                string gameId = GameIdDetector.DetectGameId(vcdFullPath) ?? "UNKNOWN";

                // Título limpio SIN GameID (lo que verá OPL)
                string baseName = Path.GetFileNameWithoutExtension(vcdFullPath) ?? "";
                string cleanTitle = NameCleanerBase.CleanTitleOnly(baseName);

                if (discNumber > 1)
                    cleanTitle += $" (Disc {discNumber})";

                // Nombre de archivo ELF (con .ELF.NTSC)
                string elfFileName = $"{gameId} - {cleanTitle}.ELF.NTSC";
                string outputElf = Path.Combine(appsFolder, elfFileName);

                // Ruta POPStarter al VCD (mass:/POPS/Carpeta/VCD)
                string vcdRelativePath = BuildPopsVcdPath(vcdFullPath);

                log("[ELF] Preparando generación de ELF PS1:");
                log($"[ELF]   ID:     {gameId}");
                log($"[ELF]   Título: {cleanTitle}");
                log($"[ELF]   VCD:    {vcdRelativePath}");
                log($"[ELF]   ELF:    {outputElf}");

                // -----------------------------
                // Copiar ELF base
                // -----------------------------
                Directory.CreateDirectory(Path.GetDirectoryName(outputElf)!);
                File.Copy(baseElfPath, outputElf, true);

                // Normalizar valores ASCII
                string safeGameId = NormalizeAscii(gameId);
                string safeVcdPath = NormalizeAscii(vcdRelativePath);
                string safeTitle = NormalizeAscii(cleanTitle);

                // Validar longitudes
                safeGameId = TruncateWithLog(safeGameId, ElfOffsets.GameIdMaxLength, log, "GameID");
                safeVcdPath = TruncateWithLog(safeVcdPath, ElfOffsets.VcdPathMaxLength, log, "VCD Path");
                safeTitle = TruncateWithLog(safeTitle, ElfOffsets.TitleMaxLength, log, "Title");

                // -----------------------------
                // Escritura binaria
                // -----------------------------
                using var stream = new FileStream(outputElf, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

                if (stream.Length < ElfOffsets.MinimumElfSize)
                {
                    log("[ELF] ERROR: ELF base demasiado pequeño o corrupto.");
                    return false;
                }

                WriteAsciiFixed(writer, ElfOffsets.GameId, safeGameId, ElfOffsets.GameIdMaxLength);
                WriteAsciiFixed(writer, ElfOffsets.VcdPath, safeVcdPath, ElfOffsets.VcdPathMaxLength);
                WriteAsciiFixed(writer, ElfOffsets.Title, safeTitle, ElfOffsets.TitleMaxLength);

                log("[ELF] ELF PS1 generado correctamente.");
                return true;
            }
            catch (Exception ex)
            {
                log($"[ELF] ERROR generando ELF: {ex.Message}");
                return false;
            }
        }

        // ============================================================
        //  Construir ruta POPStarter al VCD
        //  mass:/POPS/{Carpeta}/{Archivo.VCD}
        // ============================================================
        private static string BuildPopsVcdPath(string vcdFullPath)
        {
            string folder = Path.GetFileName(Path.GetDirectoryName(vcdFullPath)) ?? "";
            string file = Path.GetFileName(vcdFullPath) ?? "";
            return $"mass:/POPS/{folder}/{file}";
        }

        // ============================================================
        // Helpers
        // ============================================================
        private static string NormalizeAscii(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            var sb = new StringBuilder();
            foreach (char c in value)
                sb.Append(c <= 127 ? c : '_');

            return sb.ToString().Trim();
        }

        private static string TruncateWithLog(string value, int max, Action<string> log, string field)
        {
            if (value.Length > max)
            {
                log($"[ELF] Aviso: {field} truncado a {max} caracteres.");
                return value.Substring(0, max);
            }
            return value;
        }

        private static void WriteAsciiFixed(BinaryWriter writer, int offset, string value, int maxLength)
        {
            writer.BaseStream.Seek(offset, SeekOrigin.Begin);

            var bytes = Encoding.ASCII.GetBytes(value);
            if (bytes.Length > maxLength)
                Array.Resize(ref bytes, maxLength);

            writer.Write(bytes);

            int remaining = maxLength - bytes.Length;
            if (remaining > 0)
                writer.Write(new byte[remaining]);
        }
    }
}
