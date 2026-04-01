using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class DiscDetector
    {
        public static string Detect(string name)
        {
            var match = Regex.Match(name, @"(Disc|Disk|CD)\s*([1-9])", RegexOptions.IgnoreCase);
            if (match.Success)
                return "CD" + match.Groups[2].Value;

            return null;
        }
    }
}
