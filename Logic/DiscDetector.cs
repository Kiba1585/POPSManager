using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class DiscDetector
    {
        // Regex profesional para detectar discos en TODOS los formatos reales
        private static readonly Regex DiscRegex = new Regex(
            @"(?:DISC|DISK|CD)[\s\-_]*0?(\d{1,2})|(?:D)(\d{1,2})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string? Detect(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var match = DiscRegex.Match(name);
            if (!match.Success)
                return null;

            // match.Groups[1] = Disc 1, CD1, Disk1
            // match.Groups[2] = D1 (formato japonés)
            string number = match.Groups[1].Success
                ? match.Groups[1].Value
                : match.Groups[2].Value;

            return $"CD{number}";
        }
    }
}
