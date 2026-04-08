using System.Globalization;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class NameCleaner
    {
        private static readonly string[] MinorWords =
        {
            "of", "the", "and", "to", "in", "on", "at", "for", "from", "a", "an"
        };

        public static string Clean(string name, out string? cdTag)
        {
            cdTag = DetectDisc(name);

            // Eliminar tag de disco del nombre
            name = Regex.Replace(name, @"(\(|\[|\{)?(Disc|Disk|CD|D)\s*0?([1-9])(\\)?(\)|\]|\})?", "", RegexOptions.IgnoreCase);

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

            // Eliminar símbolos basura
            name = Regex.Replace(name, @"[\[\]\{\}#@%!&]+", "", RegexOptions.IgnoreCase);

            // Normalizar espacios y guiones
            name = Regex.Replace(name, @"[_\.]+", " ");
            name = Regex.Replace(name, @"\s{2,}", " ");

            return ToTitleCaseSmart(name.Trim());
        }

        public static string CleanTitleOnly(string name)
        {
            name = Regex.Replace(name, @"\[(PAL|NTSC|NTSC-U|NTSC-J)\]", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\((PAL|NTSC|NTSC-U|NTSC-J)\)", "", RegexOptions.IgnoreCase);

            name = Regex.Replace(name, @"\((ESP|ES|EN|ENG|FRA|GER|ITA|MULTI|MULTI5)\)", "", RegexOptions.IgnoreCase);

            name = Regex.Replace(name, @"\(v\d+\.\d+\)", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\(Rev\s*\d+\)", "", RegexOptions.IgnoreCase);

            name = Regex.Replace(name, @"(SCES|SLES|SCUS|SLUS|SLPS|SLPM|SCPS)[-_]?\d+", "", RegexOptions.IgnoreCase);

            name = Regex.Replace(name, @"[\[\]\{\}#@%!&]+", "", RegexOptions.IgnoreCase);

            name = Regex.Replace(name, @"[_\.]+", " ");
            name = Regex.Replace(name, @"\s{2,}", " ");

            return ToTitleCaseSmart(name.Trim());
        }

        private static string? DetectDisc(string name)
        {
            var match = Regex.Match(name, @"(Disc|Disk|CD|D)\s*0?([1-9])", RegexOptions.IgnoreCase);
            if (match.Success)
                return $"CD{match.Groups[2].Value}";

            return null;
        }

        private static string ToTitleCaseSmart(string input)
        {
            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i].ToLowerInvariant();

                if (i > 0 && MinorWords.Contains(w))
                {
                    words[i] = w;
                }
                else
                {
                    words[i] = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(w);
                }
            }

            return string.Join(" ", words);
        }
    }
}
