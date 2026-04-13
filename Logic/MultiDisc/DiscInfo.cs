using System;
using System.IO;

namespace POPSManager.Logic
{
    public sealed class DiscInfo
    {
        // Ruta completa al archivo VCD
        public string Path { get; set; } = string.Empty;

        // Número de disco detectado (CD1, CD2, etc.)
        public int DiscNumber { get; set; }

        // GameID detectado (opcional, usado para validaciones avanzadas)
        public string GameId { get; set; } = string.Empty;

        // Nombre de la carpeta donde está el disco (CD1, CD2...)
        public string FolderName { get; set; } = string.Empty;

        // Nombre del archivo (ej: Final Fantasy VII (Disc 1).VCD)
        public string FileName { get; set; } = string.Empty;

        // Nombre del archivo sin extensión
        public string FileNameNoExt =>
            Path.GetFileNameWithoutExtension(FileName) ?? string.Empty;

        // Carpeta completa donde está el disco
        public string FolderPath =>
            Path.GetDirectoryName(Path) ?? string.Empty;

        // Título limpio (sin región, idioma, versión, etc.)
        public string CleanTitle =>
            NameCleanerBase.CleanTitleOnly(FileNameNoExt);

        // ¿La carpeta está correctamente nombrada como CDX?
        public bool FolderMatchesDisc =>
            FolderName.StartsWith("CD", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(FolderName.AsSpan(2), out int n) &&
            n == DiscNumber;
    }
}
