using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace POPSManager.Logic
{
    public static class MultiDiscManager
    {
        /// <summary>
        /// Genera DISCS.TXT correctamente para un juego multidisco.
        /// Debe recibir:
        /// - popsFolder: carpeta POPS raíz
        /// - gameId: SCES_12345
        /// - discPaths: rutas completas de los VCD ya copiados
        /// </summary>
        public static void GenerateDiscsTxt(
            string popsFolder,
            string gameId,
            List<string> discPaths,
            Action<string> log)
        {
            if (discPaths == null || discPaths.Count <= 1)
                return;

            // Ordenar por número de disco
            discPaths = discPaths
                .OrderBy(path =>
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    int cd = ExtractDiscNumber(name);
                    return cd;
                })
                .ToList();

            // Crear contenido DISCS.TXT con rutas POPStarter reales
            List<string> lines = new();

            foreach (var path in discPaths)
            {
                string folderName = Path.GetFileName(Path.GetDirectoryName(path)!);
                string fileName = Path.GetFileName(path);

                string popsPath = $"mass:/POPS/{folderName}/{fileName}";
                lines.Add(popsPath);
            }

            // Guardar DISCS.TXT en cada carpeta de disco
            foreach (var path in discPaths)
            {
                string folder = Path.GetDirectoryName(path)!;
                string discsTxtPath = Path.Combine(folder, "DISCS.TXT");

                File.WriteAllLines(discsTxtPath, lines);
                log($"[MultiDisc] DISCS.TXT generado → {discsTxtPath}");
            }
        }

        /// <summary>
        /// Extrae el número de disco desde un nombre como:
        /// - (CD1)
        /// - (CD2)
        /// - Final Fantasy IX (CD3)
        /// </summary>
        private static int ExtractDiscNumber(string name)
        {
            name = name.ToLower();

            for (int i = 1; i <= 9; i++)
            {
                if (name.Contains($"cd{i}"))
                    return i;
            }

            return 1;
        }
    }
}
