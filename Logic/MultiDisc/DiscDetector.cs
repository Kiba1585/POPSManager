using POPSManager.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class DiscDetector
    {
        private static readonly Regex DiscRegex =
            new(@"(?:DISC|DISK|CD)[\s\-_]*0?(\d{1,2})|(?:D)(\d{1,2})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static int DetectDiscNumber(string path, Action<string> log)
        {
            string name = Path.GetFileNameWithoutExtension(path) ?? "";

            // 1. Nombre del archivo
            var m = DiscRegex.Match(name);
            if (m.Success)
                return Extract(m);

            // 2. Nombre de la carpeta
            string? folder = Path.GetFileName(Path.GetDirectoryName(path));
            if (!string.IsNullOrWhiteSpace(folder))
            {
                m = DiscRegex.Match(folder);
                if (m.Success)
                    return Extract(m);
            }

            // 3. CUE avanzado
            string cuePath = Path.ChangeExtension(path, ".cue");
            if (File.Exists(cuePath))
            {
                var cue = CueParser.Parse(cuePath, log);
                if (cue != null)
                {
                    // Comentarios o FILE pueden contener CD1/CD2
                    var cd = DiscRegex.Match(cuePath);
                    if (cd.Success)
                        return Extract(cd);
                }
            }

            // 4. SYSTEM.CNF (GameIdDetector)
            var id = GameIdDetector.DetectGameId(path);
            if (!string.IsNullOrWhiteSpace(id))
            {
                char last = id[^1];
                if (last is '1' or '2' or '3' or '4')
                {
                    int n = last - '0';
                    log($"[MultiDisc] Detectado número de disco desde SYSTEM.CNF → CD{n}");
                    return n;
                }
            }

            // 5. DISCS.TXT existente
            string? folderPath = Path.GetDirectoryName(path);
            if (folderPath != null)
            {
                string discsTxt = Path.Combine(folderPath, "DISCS.TXT");
                if (File.Exists(discsTxt))
                {
                    var lines = File.ReadAllLines(discsTxt);
                    string fileName = Path.GetFileName(path);

                    string? match = lines.FirstOrDefault(l => l.Contains(fileName));
                    if (match != null)
                    {
                        int index = Array.IndexOf(lines, match);
                        if (index >= 0)
                        {
                            log($"[MultiDisc] Detectado número de disco desde DISCS.TXT → CD{index + 1}");
                            return index + 1;
                        }
                    }
                }
            }

            // 6. Fallback → CD1
            log("[MultiDisc] Aviso: No se pudo detectar el número de disco. Asignando CD1.");
            return 1;
        }

        private static int Extract(Match m)
        {
            if (m.Groups[1].Success)
                return int.Parse(m.Groups[1].Value);

            if (m.Groups[2].Success)
                return int.Parse(m.Groups[2].Value);

            return -1;
        }
    }
}
