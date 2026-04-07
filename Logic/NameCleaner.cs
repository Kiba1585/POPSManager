using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class NameCleaner
    {
        private static readonly Regex DiscRegex =
            new Regex(@"(?:CD|DISC|DISK)[\s\-_]*0?(\d+)", RegexOptions.IgnoreCase);

        private static readonly Regex RegionRegex =
            new Regex(@"\b(PAL|NTSC|NTSC-U|NTSC-J)\b", RegexOptions.IgnoreCase);

        private static readonly Regex LangRegex =
            new Regex(@"\b(ESP|ES|EN|ENG|FRA|GER|ITA|MULTI|MULTI5)\b", RegexOptions.IgnoreCase);

        private static readonly Regex VersionRegex =
            new Regex(@"\b(v\d+\.\d+|Rev\s*\d+|Beta|Demo)\b", RegexOptions.IgnoreCase);

        private static readonly Regex TrackRegex =
            new Regex(@"\(Track\s*\d+\)", RegexOptions.IgnoreCase);

        private static readonly Regex GameIdRegex =
            new Regex(@"(SCES|SLES|SCUS|SLUS|SLPS|SLPM|SCPS)[\-_]?\d+", RegexOptions.IgnoreCase);

        public static string Clean(string name, out string? cdTag)
        {
            var discMatch = DiscRegex.Match(name);
            cdTag = discMatch.Success ? $"CD{discMatch.Groups[1].Value}" : null;

            name = RegionRegex.Replace(name, "");
            name = LangRegex.Replace(name, "");
            name = VersionRegex.Replace(name, "");
            name = TrackRegex.Replace(name, "");
            name = GameIdRegex.Replace(name, "");

            name = name
                .Replace("_", " ")
                .Replace(".", " ")
                .Replace("-", " ")
                .Trim();

            name = Regex.Replace(name, @"\s{2,}", " ");

            if (!string.IsNullOrWhiteSpace(cdTag))
                name += $" ({cdTag})";

            return name.Trim();
        }
    }
}
