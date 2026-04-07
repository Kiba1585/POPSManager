using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class DiscDetector
    {
        private static readonly Regex DiscRegex =
            new Regex(@"(?:CD|DISC|DISK)[\s\-_]*0?(\d+)", RegexOptions.IgnoreCase);

        public static string? Detect(string name)
        {
            var match = DiscRegex.Match(name);
            if (!match.Success)
                return null;

            return $"CD{match.Groups[1].Value}";
        }
    }
}
