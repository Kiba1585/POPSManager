using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class NameCleanerBase
    {
        // Palabras menores que deben ir en minúsculas
        private static readonly string[] MinorWords =
        {
            "of", "the", "and", "to", "in", "on", "at", "for", "from", "a", "an"
        };

        // Regex profesional para detectar discos
        private static readonly Regex DiscRegex =
            new(@"(?:DISC|DISK|CD)[\s\-_]*0?(\d{1,2})|(?:D)(\d{1,2})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Regex para eliminar tags comunes
        private static readonly Regex RegionRegex =
            new(@"\b(PAL|NTSC|NTSC-U|NTSC-J)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex LanguageRegex =
            new(@"\b(ESP|ES|EN|ENG|FRA|GER|ITA|MULTI|MULTI\d?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex VersionRegex =
            new(@"\b(v\d+\.\d+|Rev\s*\d+|Beta|Demo)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TrackRegex =
            new(@"\bTrack\s*\d+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex EmbeddedIdRegex =
            new(@"\b(SCES|SLES|SCUS|SLUS|SLPS|SLPM|SCPS)[-_]?\d+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex BadSymbolsRegex =
            new(@"[\[\]\{\}#@%&]+", RegexOptions.Compiled);

        private static readonly Regex ParenthesisCleaner =
            new(@"\([^)]*\)", RegexOptions.Compiled);

        private static readonly Regex BracketCleaner =
            new(@"\[[^\]]*\]", RegexOptions.Compiled);

        private static readonly Regex SpaceNormalizer =
            new(@"\s{2,}", RegexOptions.Compiled);

        private static readonly Regex UnderscoreDotNormalizer =
            new(@"[_\.]+", RegexOptions.Compiled);

        /// <summary>
        /// Limpieza completa del nombre, con detección de disco.
        /// </summary>
        public static string Clean(string name, out string? cdTag)
        {
            cdTag = DetectDisc(name);

            // 1. Eliminar tag de disco
            name = DiscRegex.Replace(name, "");

            // 2. Eliminar contenido entre paréntesis y corchetes
            name = ParenthesisCleaner.Replace(name, "");
            name = BracketCleaner.Replace(name, "");

            // 3. Eliminar región
            name = RegionRegex.Replace(name, "");

            // 4. Eliminar idiomas
            name = LanguageRegex.Replace(name, "");

            // 5. Eliminar versiones
            name = VersionRegex.Replace(name, "");

            // 6. Eliminar tracks
            name = TrackRegex.Replace(name, "");

            // 7. Eliminar IDs incrustados
            name = EmbeddedIdRegex.Replace(name, "");

            // 8. Eliminar símbolos basura
            name = BadSymbolsRegex.Replace(name, "");

            // 9. Normalizar separadores
            name = UnderscoreDotNormalizer.Replace(name, " ");

            // 10. Normalizar espacios
            name = SpaceNormalizer.Replace(name, " ");

            // 11. Limpieza final
            name = name.Trim();

            // 12. Title Case inteligente
            return ToTitleCaseSmart(name);
        }

        /// <summary>
        /// Limpieza sin detección de disco (para títulos).
        /// </summary>
        public static string CleanTitleOnly(string name)
        {
            name = ParenthesisCleaner.Replace(name, "");
            name = BracketCleaner.Replace(name, "");
            name = RegionRegex.Replace(name, "");
            name = LanguageRegex.Replace(name, "");
            name = VersionRegex.Replace(name, "");
            name = EmbeddedIdRegex.Replace(name, "");
            name = BadSymbolsRegex.Replace(name, "");
            name = UnderscoreDotNormalizer.Replace(name, " ");
            name = SpaceNormalizer.Replace(name, " ");

            return ToTitleCaseSmart(name.Trim());
        }

        /// <summary>
        /// Detección avanzada del número de disco.
        /// </summary>
        private static string? DetectDisc(string name)
        {
            var m = DiscRegex.Match(name);
            if (!m.Success)
                return null;

            if (m.Groups[1].Success)
                return $"CD{m.Groups[1].Value}";

            if (m.Groups[2].Success)
                return $"CD{m.Groups[2].Value}";

            return null;
        }

        /// <summary>
        /// Title Case inteligente con preservación de símbolos válidos.
        /// </summary>
        private static string ToTitleCaseSmart(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i].ToLowerInvariant();

                // Palabras menores en minúsculas (excepto la primera)
                if (i > 0 && MinorWords.Contains(w))
                {
                    words[i] = w;
                    continue;
                }

                // Preservar símbolos válidos
                if (w.Contains(':') || w.Contains('-') || w.Contains('!') || w.Contains('?'))
                {
                    words[i] = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(w);
                    continue;
                }

                // Title Case normal
                words[i] = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(w);
            }

            return string.Join(" ", words);
        }
    }
}
