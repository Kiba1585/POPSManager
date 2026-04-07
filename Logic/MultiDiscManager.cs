using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class MultiDiscManager
    {
        private static readonly Regex DiscRegex = new Regex(@"\(CD(\d+)\)", RegexOptions.IgnoreCase);

        public static void ProcessMultiDisc(string popsRoot, string gameId, Action<string> log)
        {
            string gameFolder = Path.Combine(popsRoot, gameId);

            if (!Directory.Exists(gameFolder))
                return;

            var discs = Directory.GetFiles(popsRoot, $"{gameId} (CD*).VCD", SearchOption.TopDirectoryOnly)
                .Select(path => new
                {
                    Path = path,
                    Name = Path.GetFileName(path),
                    Match = DiscRegex.Match(Path.GetFileNameWithoutExtension(path))
                })
                .Where(x => x.Match.Success)
                .OrderBy(x => int.Parse(x.Match.Groups[1].Value))
                .ToList();

            if (discs.Count <= 1)
                return;

            log($"Juego multidisco detectado: {gameId} ({discs.Count} discos)");

            string[] lines = discs.Select(d => d.Name).ToArray();

            foreach (var disc in discs)
            {
                string folder = Path.Combine(popsRoot, Path.GetFileNameWithoutExtension(disc.Name));
                Directory.CreateDirectory(folder);

                File.WriteAllLines(Path.Combine(folder, "DISCS.TXT"), lines);
            }

            log("DISCS.TXT generado correctamente para multidisco.");
        }
    }
}
