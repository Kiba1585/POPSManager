using System;
using System.IO;

namespace POPSManager.Logic
{
    public static class NameFormatter
    {
        // ============================================================
        //  NOMBRE FINAL PARA PS1 (VCD)
        // ============================================================
        public static string BuildPs1VcdName(string path, int discNumber = 1)
        {
            string ext = ".VCD";

            // 1. Detectar ID interno
            string id = GameIdDetector.DetectGameId(path) ?? "UNKNOWN";

            // 2. Limpiar título base
            string fileName = Path.GetFileNameWithoutExtension(path) ?? "";
            string cleanTitle = NameCleanerBase.CleanTitleOnly(fileName);

            // 3. Disc tag
            if (discNumber > 1)
                cleanTitle += $" (Disc {discNumber})";

            // 4. Ensamblar nombre final
            return $"{id} - {cleanTitle}{ext}";
        }

        // ============================================================
        //  NOMBRE FINAL PARA PS2 (ISO)
        // ============================================================
        public static string BuildPs2IsoName(string path)
        {
            string ext = ".iso";

            // 1. Detectar ID interno
            string id = GameIdDetector.DetectGameId(path) ?? "UNKNOWN";

            // 2. Limpiar título base
            string fileName = Path.GetFileNameWithoutExtension(path) ?? "";
            string cleanTitle = NameCleanerBase.CleanTitleOnly(fileName);

            // 3. Ensamblar nombre final
            return $"{id} - {cleanTitle}{ext}";
        }

        // ============================================================
        //  NOMBRE DE CARPETA POPS (PS1)
        // ============================================================
        public static string BuildPopsFolderName(string path, int discNumber = 1)
        {
            string id = GameIdDetector.DetectGameId(path) ?? "UNKNOWN";

            string fileName = Path.GetFileNameWithoutExtension(path) ?? "";
            string cleanTitle = NameCleanerBase.CleanTitleOnly(fileName);

            if (discNumber > 1)
                cleanTitle += $" (Disc {discNumber})";

            return $"{id} - {cleanTitle}";
        }

        // ============================================================
        //  NOMBRE DE ARCHIVO ELF PARA OPL (PS1)
        // ============================================================
        public static string BuildElfName(string gameId)
        {
            return $"{gameId}.ELF";
        }
    }
}
