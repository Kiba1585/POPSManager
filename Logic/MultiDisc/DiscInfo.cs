namespace POPSManager.Logic
{
    public class DiscInfo
    {
        // Ruta completa al archivo VCD
        public string Path { get; set; } = "";

        // Número de disco detectado (CD1, CD2, etc.)
        public int DiscNumber { get; set; }

        // GameID detectado (opcional, usado para validaciones avanzadas)
        public string GameId { get; set; } = "";

        // Nombre de la carpeta donde está el disco (CD1, CD2...)
        public string FolderName { get; set; } = "";

        // Nombre del archivo (ej: SLUS_123.45 - Final Fantasy VII (CD1).VCD)
        public string FileName { get; set; } = "";

        // ============================================================
        // PROPIEDADES DERIVADAS (ÚTILES PARA VALIDACIÓN E INFORMES)
        // ============================================================

        // Nombre del archivo sin extensión
        public string FileNameNoExt =>
            System.IO.Path.GetFileNameWithoutExtension(FileName) ?? "";

        // Carpeta completa donde está el disco
        public string FolderPath =>
            System.IO.Path.GetDirectoryName(Path) ?? "";

        // Título limpio (sin región, idioma, versión, etc.)
        public string CleanTitle =>
            NameCleanerBase.CleanTitleOnly(FileNameNoExt);

        // ¿La carpeta está correctamente nombrada como CDX?
        // ✅ FIX CA1310: Reemplazar ToUpper().StartsWith("CD")
        //    por StartsWith con StringComparison.OrdinalIgnoreCase
        public bool FolderMatchesDisc =>
            FolderName.StartsWith("CD", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(FolderName.Substring(2), out int n) &&
            n == DiscNumber;
    }
}
