using System.IO;
using System.Linq;

namespace POPSManager.Logic
{
    public static class MultiDiscManager
    {
        public static void ProcessMultiDisc(string popsFolder, string gameId, Action<string> log)
        {
            var folders = Directory.GetDirectories(popsFolder)
                                   .Where(d => Path.GetFileName(d).StartsWith(gameId))
                                   .OrderBy(d => d)
                                   .ToArray();

            if (folders.Length <= 1)
                return;

            string discsTxt = Path.Combine(popsFolder, "DISCS.TXT");

            File.WriteAllLines(discsTxt, folders.Select(f => Path.GetFileName(f)));

            log($"Generado multidisco → {discsTxt}");
        }
    }
}
