using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class NameCleaner
    {
        // Detecta CD1, CD 1, CD-1, Disc 1, Disk 1, etc.
        private static readonly Regex DiscRegex =
            new Regex(@"(?:CD|DISC|DISK)[\s\-_]*0?(\d+)", RegexOptions.IgnoreCase);

        // Detecta regiones
        private static readonly Regex RegionRegex =
            new Regex(@"\b(PAL|NTSC|NTSC-U|NTSC-J)\b", RegexOptions.IgnoreCase);

        // Detecta idiomas
        private static readonly Regex LangRegex =
            new Regex(@"\b(ESP|ES|EN|ENG|FRA|GER|ITA|MULTI|MULTI5)\b", RegexOptions.IgnoreCase);

        // Detecta versiones
        private static readonly Regex VersionRegex =
            new Regex(@"\b(v\d+\.\d+|Rev\s*\d+|Beta|Demo)\b", RegexOptions.IgnoreCase);

        // Detecta tracks
        private static readonly Regex TrackRegex =
            new Regex(@"\(Track\s*\d+\)", RegexOptions.IgnoreCase);

        // Detecta IDs de PS1 dentro del nombre
        private static readonly Regex GameIdRegex =
            new Regex(@"(SCES|SLES|SCUS|SLUS|SLPS|SLPM|SCPS)[\-_]?\d+", RegexOptions.IgnoreCase);

        public static string Clean(string name, out string? cdTag)
        {
            // ============================================================
            // 1. Detectar número de disco (CDX)
            // ============================================================

            var discMatch = DiscRegex.Match(name);
            cdTag = discMatch.Success ? $"CD{discMatch.Groups[1].Value}" : null;

            // ============================================================
            // 2. Eliminar región
            // ============================================================

            name = RegionRegex.Replace(name, "");

            // ============================================================
            // 3. Eliminar idioma
            // ============================================================

            name = LangRegex.Replace(name, "");

            // ============================================================
            // 4. Eliminar versiones, betas, demos
            // ============================================================

            name = VersionRegex.Replace(name, "");

            // ============================================================
            // 5. Eliminar tracks
            // ============================================================

            name = TrackRegex.Replace(name, "");

            // ============================================================
            // 6. Eliminar Game ID incrustado
            // ============================================================

            name = GameIdRegex.Replace(name, "");

            // ============================================================
            // 7. Limpieza general
            // ============================================================

            name = name
                .Replace("_", " ")
                .Replace(".", " ")
                .Replace("-", " ")
                .Trim();

            // Colapsar espacios dobles
            name = Regex.Replace(name, @"\s{2,}", " ");

            // ============================================================
            // 8. Añadir CDX al final si existe
            // ============================================================

            if (!string.IsNullOrWhiteSpace(cdTag))
                name += $" ({cdTag})";

            return name.Trim();
        }
    }
}
