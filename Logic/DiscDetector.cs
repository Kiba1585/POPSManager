using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class DiscDetector
    {
        // Detecta:
        // CD1, CD 1, CD-1, CD_1
        // Disc 1, Disk 1
        // (CD1), [Disc 1], {Disk 01}
        private static readonly Regex DiscRegex =
            new Regex(@"(?:CD|DISC|DISK)[\s\-_]*0?(\d+)", RegexOptions.IgnoreCase);

        public static string? Detect(string name)
        {
            var match = DiscRegex.Match(name);
            if (!match.Success)
                return null;

            // match.Groups[1] = número del disco
            string number = match.Groups[1].Value;

            return $"CD{number}";
        }
    }
}
