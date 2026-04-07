using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace POPSManager.Logic
{
    public static class MultiDiscManager
    {
        // Detecta nombres como (CD1), (CD2), (CD3)...
        private static readonly Regex DiscRegex = new Regex(@"\(CD(\d+)\)", RegexOptions.IgnoreCase);

        /// <summary>
        /// Procesa un juego y genera DISCS.TXT si es multidisco.
        /// </summary>
        /// <param name="folder">Carpeta donde están los VCD renombrados</param>
        /// <param name="popsFolder">Carpeta POPS donde se copiarán los discos</param>
        public static void ProcessMultiDisc(string folder, string popsFolder)
        {
            if (!Directory.Exists(folder))
                return;

            // Buscar todos los VCD que tengan (CDX)
            var discs = Directory.GetFiles(folder, "*.VCD")
                .Select(path => new
                {
                    Path = path,
                    Name = Path.GetFileName(path),
                    Match = DiscRegex.Match(Path.GetFileNameWithoutExtension(path))
                })
                .Where(x => x.Match.Success)
                .OrderBy(x => int.Parse(x.Match.Groups[1].Value))
                .ToList();

            // Si no es multidisco, no hacemos nada
            if (discs.Count <= 1)
                return;

            // Crear contenido del DISCS.TXT
            var lines = discs.Select(d => d.Name).ToArray();

            // Guardar DISCS.TXT en la carpeta original
            File.WriteAllLines(Path.Combine(folder, "DISCS.TXT"), lines);

            // Copiar DISCS.TXT a cada carpeta POPS del juego
            foreach (var disc in discs)
            {
                string discFolder = Path.Combine(popsFolder,
                    Path.GetFileNameWithoutExtension(disc.Name));

                Directory.CreateDirectory(discFolder);

                File.WriteAllLines(
                    Path.Combine(discFolder, "DISCS.TXT"),
                    lines
                );
            }
        }
    }
}
