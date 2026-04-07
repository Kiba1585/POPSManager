using System;
using System.IO;
using System.Text;

namespace POPSManager.Logic
{
    public static class ElfGenerator
    {
        /// <summary>
        /// Genera un ELF específico para un juego a partir del POPSTARTER.ELF base.
        /// Escribe:
        /// - Game ID
        /// - Ruta del VCD
        /// - Título mostrado
        /// </summary>
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
                if (!File.Exists(baseElf))
                {
                    log($"[ELF] POPSTARTER.ELF base no encontrado: {baseElf}");
                    return false;
                }

                // Copiar ELF base
                Directory.CreateDirectory(Path.GetDirectoryName(outputElf)!);
                File.Copy(baseElf, outputElf, true);

                // Normalizar valores
                string safeGameId = (gameId ?? string.Empty).Trim();
                string safeVcdPath = (vcdPath ?? string.Empty).Trim();
                string safeTitle = (title ?? string.Empty).Trim();

                using var stream = new FileStream(outputElf, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

                // Escribir Game ID
                WriteAsciiFixed(writer,
                    ElfOffsets.GameId,
                    safeGameId,
                    ElfOffsets.GameIdMaxLength);

                // Escribir ruta del VCD
                WriteAsciiFixed(writer,
                    ElfOffsets.VcdPath,
                    safeVcdPath,
                    ElfOffsets.VcdPathMaxLength);

                // Escribir título
                WriteAsciiFixed(writer,
                    ElfOffsets.Title,
                    safeTitle,
                    ElfOffsets.TitleMaxLength);

                log($"[ELF] Generado ELF → {outputElf}");
                log($"[ELF]   ID:   {safeGameId}");
                log($"[ELF]   VCD:  {safeVcdPath}");
                log($"[ELF]   Título: {safeTitle}");

                return true;
            }
            catch (Exception ex)
            {
                log($"[ELF] Error generando ELF: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Escribe una cadena ASCII en un offset fijo, truncando o rellenando con 0x00.
        /// </summary>
        private static void WriteAsciiFixed(BinaryWriter writer, int offset, string value, int maxLength)
        {
            writer.BaseStream.Seek(offset, SeekOrigin.Begin);

            // Convertir a ASCII, truncar si es necesario
            var bytes = Encoding.ASCII.GetBytes(value);
            if (bytes.Length > maxLength)
                Array.Resize(ref bytes, maxLength);

            // Escribir bytes
            writer.Write(bytes);

            // Rellenar con 0x00 hasta completar el bloque
            int remaining = maxLength - bytes.Length;
            if (remaining > 0)
                writer.Write(new byte[remaining]);
        }
    }
}
