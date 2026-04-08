using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class MultiDiscManager
    {
        private static readonly Regex DiscRegex =
            new(@"(?:DISC|DISK|CD)[\s\-_]*0?(\d{1,2})|(?:D)(\d{1,2})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // ============================================================
        //  MÉTODO REQUERIDO POR GameProcessor
        // ============================================================
        public static int ExtractDiscNumber(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return 1;

            var m = DiscRegex.Match(name);
            if (m.Success)
            {
                if (m.Groups[1].Success)
                    return int.Parse(m.Groups[1].Value);

                if (m.Groups[2].Success)
                    return int.Parse(m.Groups[2].Value);
            }

            return 1;
        }

        // ============================================================
        //  GENERAR DISCS.TXT
        // ============================================================
        public static void GenerateDiscsTxt(
            string popsFolder,
            string gameId,
            List<string> discPaths,
            Action<string> log)
        {
            if (discPaths == null || discPaths.Count <= 1)
            {
                log("[MultiDisc] No es multidisco.");
                return;
            }

            var discs = discPaths
                .Select(path => new DiscInfo
                {
                    Path = path,
                    Disc = DetectDiscNumber(path, log),
                    Id = GameIdDetector.DetectGameId(path) ?? ""
                })
                .ToList();

            // Validar detección
            if (discs.Any(d => d.Disc <= 0))
            {
                log("[MultiDisc] ERROR: No se pudo detectar el número de disco.");
                return;
            }

            // Validar IDs internos
            if (discs.Any(d => string.IsNullOrWhiteSpace(d.Id)))
            {
                log("[MultiDisc] ERROR: Uno o más discos no tienen ID interno válido.");
                return;
            }

            if (discs.Select(d => d.Id).Distinct().Count() != 1)
            {
                log("[MultiDisc] ERROR: Los discos no pertenecen al mismo juego.");
                return;
            }

            // Validar duplicados
            if (discs.GroupBy(d => d.Disc).Any(g => g.Count() > 1))
            {
                log("[MultiDisc] ERROR: Hay discos duplicados.");
                return;
            }

            // Validar rango
            if (discs.Count > 4)
            {
                log("[MultiDisc] ERROR: POPStarter solo soporta hasta 4 discos.");
                return;
            }

            // Ordenar y validar secuencia
            discs = discs.OrderBy(d => d.Disc).ToList();

            for (int i = 0; i < discs.Count; i++)
            {
                if (discs[i].Disc != i + 1)
                {
                    log("[MultiDisc] ERROR: Los discos deben ser consecutivos (CD1, CD2, CD3...).");
                    return;
                }
            }

            // Construir rutas POPStarter reales
            List<string> lines = new();

            foreach (var d in discs)
            {
                string? folderName = Path.GetFileName(Path.GetDirectoryName(d.Path));
                string fileName = Path.GetFileName(d.Path);

                if (folderName == null)
                {
                    log("[MultiDisc] ERROR: Carpeta inválida.");
                    return;
                }

                string popsPath = $"mass:/POPS/{folderName}/{fileName}";
                lines.Add(popsPath);
            }

            // Guardar DISCS.TXT en cada carpeta
            foreach (var d in discs)
            {
                string? folder = Path.GetDirectoryName(d.Path);
                if (folder == null)
                {
                    log("[MultiDisc] ERROR: Carpeta inválida.");
                    continue;
                }

                string discsTxtPath = Path.Combine(folder, "DISCS.TXT");
                File.WriteAllLines(discsTxtPath, lines);
                log($"[MultiDisc] DISCS.TXT generado → {discsTxtPath}");
            }
        }

        // ============================================================
        //  DETECCIÓN ULTRA PRO DEL NÚMERO DE DISCO
        // ============================================================
        private static int DetectDiscNumber(string path, Action<string> log)
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

            // 3. SYSTEM.CNF real
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

            // 4. DISCS.TXT existente
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

            // 5. Fallback → CD1
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

        private class DiscInfo
        {
            public string Path { get; set; } = "";
            public int Disc { get; set; }
            public string Id { get; set; } = "";
        }
    }
}
