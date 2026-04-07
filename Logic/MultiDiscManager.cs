using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class MultiDiscManager
    {
        private static readonly Regex DiscRegex = new Regex(@"\(CD(\d+)\)", RegexOptions.IgnoreCase);

        /// <summary>
        /// Genera DISCS.TXT para juegos multidisco.
        /// </summary>
        /// <param name="popsRoot">Carpeta raíz POPS</param>
        /// <param name="gameId">ID del juego (SCES_12345)</param>
        /// <param name="log">Acción de log</param>
        public static void ProcessMultiDisc(string popsRoot, string gameId, Action<string> log)
        {
            // Buscar SOLO dentro de carpetas del juego:
            // Ej: POPS/SCES_12345 (CD1)/
            var gameFolders = Directory.GetDirectories(popsRoot, $"{gameId} (CD*)");

            if (gameFolders.Length <= 1)
                return;

            // Buscar VCDs dentro de cada carpeta del juego
            var discs = gameFolders
                .SelectMany(folder => Directory.GetFiles(folder, "*.VCD"))
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

            // Crear contenido de DISCS.TXT
            string[] lines = discs.Select(d => d.Name).ToArray();

            // Guardar DISCS.TXT en cada carpeta del juego
            foreach (var disc in discs)
            {
                string folder = Path.GetDirectoryName(disc.Path)!;
                File.WriteAllLines(Path.Combine(folder, "DISCS.TXT"), lines);
            }

            log("DISCS.TXT generado correctamente para multidisco.");
        }
    }
}
