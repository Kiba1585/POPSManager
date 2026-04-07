using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class NameCleaner
    {
        /// <summary>
        /// Limpia el nombre completo y detecta el tag de disco (CD1, CD2, etc.)
        /// </summary>
        public static string Clean(string name, out string? cdTag)
        {
            cdTag = DetectDisc(name);

            // Eliminar tag de disco del nombre
            if (cdTag != null)
                name = Regex.Replace(name, @"(\(|\[)?(Disc|Disk|CD)\s*([1-9])(\\)?(\)|\])?", "", RegexOptions.IgnoreCase);

            // Eliminar región
            name = Regex.Replace(name, @"\[(PAL|NTSC|NTSC-U|NTSC-J)\]", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\((PAL|NTSC|NTSC-U|NTSC-J)\)", "", RegexOptions.IgnoreCase);

            // Eliminar idiomas
            name = Regex.Replace(name, @"\((ESP|ES|EN|ENG|FRA|GER|ITA|MULTI|MULTI5)\)", "", RegexOptions.IgnoreCase);

            // Eliminar versiones
            name = Regex.Replace(name, @"\(v\d+\.\d+\)", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\(Rev\s*\d+\)", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\(Beta\)", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\(Demo\)", "", RegexOptions.IgnoreCase);

            // Eliminar tracks
            name = Regex.Replace(name, @"\(Track\s*\d+\)", "", RegexOptions.IgnoreCase);

            // Eliminar IDs incrustados
            name = Regex.Replace(name, @"(SCES|SLES|SCUS|SLUS|SLPS|SLPM|SCPS)[-_]?\d+", "", RegexOptions.IgnoreCase);

            // Normalizar espacios
            name = Regex.Replace(name, @"[_\-.]+", " ");
            name = Regex.Replace(name, @"\s{2,}", " ");

            return name.Trim();
        }

        /// <summary>
        /// Limpia solo el título (sin detectar disco)
        /// </summary>
        public static string CleanTitleOnly(string name)
        {
            // Eliminar región
            name = Regex.Replace(name, @"\[(PAL|NTSC|NTSC-U|NTSC-J)\]", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\((PAL|NTSC|NTSC-U|NTSC-J)\)", "", RegexOptions.IgnoreCase);

            // Eliminar idiomas
            name = Regex.Replace(name, @"\((ESP|ES|EN|ENG|FRA|GER|ITA|MULTI|MULTI5)\)", "", RegexOptions.IgnoreCase);

            // Eliminar versiones
            name = Regex.Replace(name, @"\(v\d+\.\d+\)", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\(Rev\s*\d+\)", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\(Beta\)", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\(Demo\)", "", RegexOptions.IgnoreCase);

            // Eliminar IDs
            name = Regex.Replace(name, @"(SCES|SLES|SCUS|SLUS|SLPS|SLPM|SCPS)[-_]?\d+", "", RegexOptions.IgnoreCase);

            // Normalizar
            name = Regex.Replace(name, @"[_\-.]+", " ");
            name = Regex.Replace(name, @"\s{2,}", " ");

            return name.Trim();
        }

        /// <summary>
        /// Detecta CD1, CD2, Disc 1, Disk 1, etc.
        /// </summary>
        private static string? DetectDisc(string name)
        {
            var match = Regex.Match(name, @"(Disc|Disk|CD)\s*([1-9])", RegexOptions.IgnoreCase);
            if (match.Success)
                return $"CD{match.Groups[2].Value}";

            return null;
        }
    }
}
