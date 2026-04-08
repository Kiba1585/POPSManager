using System;
using System.IO;
using System.Text;

namespace POPSManager.Logic
{
    public static class ElfGenerator
    {
        public static bool GenerateElf(
            string baseElf,
            string outputElf,
            string gameId,
            string vcdPath,
            string title,
            Action<string> log)
        {
            try
            {
                // ============================
                // Validaciones iniciales
                // ============================
                if (!File.Exists(baseElf))
                {
                    log($"[ELF] ERROR: POPSTARTER.ELF base no encontrado: {baseElf}");
                    return false;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputElf)!);

                // Copiar ELF base
                File.Copy(baseElf, outputElf, true);

                // Normalizar valores
                string safeGameId = NormalizeAscii(gameId);
                string safeVcdPath = NormalizeAscii(vcdPath);
                string safeTitle = NormalizeAscii(title);

                // Validar longitudes
                safeGameId = TruncateWithLog(safeGameId, ElfOffsets.GameIdMaxLength, log, "GameID");
                safeVcdPath = TruncateWithLog(safeVcdPath, ElfOffsets.VcdPathMaxLength, log, "VCD Path");
                safeTitle = TruncateWithLog(safeTitle, ElfOffsets.TitleMaxLength, log, "Title");

                // ============================
                // Escritura binaria
                // ============================
                using var stream = new FileStream(outputElf, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

                // Validar tamaño del ELF
                if (stream.Length < ElfOffsets.MinimumElfSize)
                {
                    log("[ELF] ERROR: ELF base demasiado pequeño o corrupto.");
                    return false;
                }

                // Escribir campos
                WriteAsciiFixed(writer, ElfOffsets.GameId, safeGameId, ElfOffsets.GameIdMaxLength);
                WriteAsciiFixed(writer, ElfOffsets.VcdPath, safeVcdPath, ElfOffsets.VcdPathMaxLength);
                WriteAsciiFixed(writer, ElfOffsets.Title, safeTitle, ElfOffsets.TitleMaxLength);

                // ============================
                // Logs finales
                // ============================
                log($"[ELF] Generado ELF → {outputElf}");
                log($"[ELF]   ID:     {safeGameId}");
                log($"[ELF]   VCD:    {safeVcdPath}");
                log($"[ELF]   Título: {safeTitle}");

                return true;
            }
            catch (Exception ex)
            {
                log($"[ELF] ERROR generando ELF: {ex.Message}");
                return false;
            }
        }

        // ============================================================
        // Helpers
        // ============================================================

        private static string NormalizeAscii(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            // Reemplazar caracteres no ASCII
            var sb = new StringBuilder();
            foreach (char c in value)
            {
                sb.Append(c <= 127 ? c : '_');
            }
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
