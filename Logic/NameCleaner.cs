using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class NameCleaner
    {
        public static string Clean(string name, out string? cdTag)
        {
            cdTag = null;

            var match = Regex.Match(name, @"CD ?(\d)", RegexOptions.IgnoreCase);
            if (match.Success)
                cdTag = $"CD{match.Groups[1].Value}";

            name = Regex.Replace(name, @"CD ?\d", "", RegexOptions.IgnoreCase);

            return CleanTitleOnly(name);
        }

        public static string CleanTitleOnly(string name)
        {
            name = Regex.Replace(name, @"[_\-.]+", " ");
            name = Regex.Replace(name, @"\s+", " ").Trim();
            return name;
        }
    }
}
