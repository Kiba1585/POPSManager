using POPSManager.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class DiscDetector
    {
        // Regex ultra-pro para detectar TODOS los formatos reales
        private static readonly Regex DiscRegex =
            new(@"(?:DISC|DISK|CD)[\s\-_]*0?(\d{1,2})|(?:D)(\d{1,2})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static int DetectDiscNumber(string path, Action<string> log)
        {
            string name = Path.GetFileNameWithoutExtension(path) ?? "";

            // ============================================================
            // 1. NameCleanerBase (nuevo sistema)
            // ============================================================
            NameCleanerBase.Clean(name, out string? cdTag);
            if (cdTag != null && cdTag.StartsWith("CD"))
            {
                if (int.TryParse(cdTag.Substring(2), out int n))
                {
                    log($"[MultiDisc] Detectado desde NameCleanerBase → CD{n}");
                    return n;
                }
            }

            // ============================================================
            // 2. Nombre del archivo
            // ============================================================
            var m = DiscRegex.Match(name);
            if (m.Success)
            {
                int n = Extract(m);
                log($"[MultiDisc] Detectado desde nombre del archivo → CD{n}");
                return n;
            }

            // ============================================================
            // 3. Nombre de la carpeta
            // ============================================================
            string? folder = Path.GetFileName(Path.GetDirectoryName(path));
            if (!string.IsNullOrWhiteSpace(folder))
            {
                m = DiscRegex.Match(folder);
                if (m.Success)
                {
                    int n = Extract(m);
                    log($"[MultiDisc] Detectado desde carpeta → CD{n}");
                    return n;
                }
            }

            // ============================================================
            // 4. CUE avanzado (corregido)
            // ============================================================
            string cuePath = Path.ChangeExtension(path, ".cue");
            if (File.Exists(cuePath))
            {
                string cueText = File.ReadAllText(cuePath);
                m = DiscRegex.Match(cueText);
                if (m.Success)
                {
                    int n = Extract(m);
                    log($"[MultiDisc] Detectado desde CUE → CD{n}");
                    return n;
                }
            }

            // ============================================================
            // 5. SYSTEM.CNF (fallback débil)
            // ============================================================
            var id = GameIdDetector.DetectGameId(path);
            if (!string.IsNullOrWhiteSpace(id))
            {
                char last = id[^1];
                if (char.IsDigit(last))
                {
                    int n = last - '0';
                    if (n is >= 1 and <= 4)
                    {
                        log($"[MultiDisc] Detectado desde SYSTEM.CNF → CD{n}");
                        return n;
                    }
                }
            }

            // ============================================================
            // 6. DISCS.TXT existente
            // ============================================================
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
                            log($"[MultiDisc] Detectado desde DISCS.TXT → CD{index + 1}");
                            return index + 1;
                        }
                    }
                }
            }

            // ============================================================
            // 7. Fallback → CD1
            // ============================================================
            log("[MultiDisc] Aviso: No se pudo detectar el número de disco. Asignando CD1.");
            return 1;
        }

        private static int Extract(Match m)
        {
            if (m.Groups[1].Success)
                return int.Parse(m.Groups[1].Value);

            if (m.Groups[2].Success)
                return int.Parse(m.Groups[2].Value);

            return 1;
        }
    }
}
