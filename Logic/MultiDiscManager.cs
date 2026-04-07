using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class MultiDiscManager
    {
        private static readonly Regex DiscRegex = new Regex(@"\(CD(\d+)\)", RegexOptions.IgnoreCase);

        // popsRoot = carpeta POPS raíz
        // gameId   = ID del juego (SCES_12345, etc.)
        public static void ProcessMultiDisc(string popsRoot, string gameId, Action<string> log)
        {
            // Buscar todos los VCD de ese juego en subcarpetas:
            // Ej: POPS/SCES_12345 (CD1)/SCES_12345.Final Fantasy IX (CD1).VCD
            var discs = Directory.GetFiles(popsRoot, $"{gameId}.*.VCD", SearchOption.AllDirectories)
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
                string folder = Path.GetDirectoryName(disc.Path)!;
                File.WriteAllLines(Path.Combine(folder, "DISCS.TXT"), lines);
            }

            log("DISCS.TXT generado correctamente para multidisco.");
        }
    }
}
