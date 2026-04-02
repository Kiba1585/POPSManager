using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class NameCleaner
    {
        public static string Clean(string name, out string? cdTag)
        {
            cdTag = DetectDisc(name);

            name = Regex.Replace(name, @"\[(PAL|NTSC|NTSC-U|NTSC-J)\]", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\((PAL|NTSC|NTSC-U|NTSC-J)\)", "", RegexOptions.IgnoreCase);

            name = Regex.Replace(name, @"\((ESP|ES|EN|ENG|FRA|GER|ITA|MULTI|MULTI5)\)", "", RegexOptions.IgnoreCase);

            name = Regex.Replace(name, @"\(v\d+\.\d+\)", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\(Rev\s*\d+\)", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\(Beta\)", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\(Demo\)", "", RegexOptions.IgnoreCase);

            name = Regex.Replace(name, @"\(Track\s*\d+\)", "", RegexOptions.IgnoreCase);

            name = Regex.Replace(name, @"(SCES|SLES|SCUS|SLUS|SLPS|SLPM|SCPS)[-_]?\d+", "", RegexOptions.IgnoreCase);

            name = Regex.Replace(name, @"\s{2,}", " ");

            name = name.Trim();

            if (!string.IsNullOrEmpty(cdTag))
                name += $" ({cdTag})";

            return name;
        }

        private static string? DetectDisc(string name)
        {
            var match = Regex.Match(name, @"(Disc|Disk|CD)\s*([1-9])", RegexOptions.IgnoreCase);
            if (match.Success)
                return "CD" + match.Groups[2].Value;

            return null;
        }
    }
}
