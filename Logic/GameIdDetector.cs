using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class GameIdDetector
    {
        private static readonly string[] Patterns =
        {
            "SCES", "SLES", "SLUS", "SCUS", "SLPS", "SLPM", "SCPS"
        };

        // Regex robusto para BOOT = cdrom:\XXXX_999.99;1
        private static readonly Regex BootRegex =
            new(@"BOOT\s*=\s*cdrom:\\\s*([A-Z]{4})[-_ ]?(\d{3})[._ ]?(\d{2})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Regex para IDs PS2 dentro de IOPRP.IMG
        private static readonly Regex Ps2Regex =
            new(@"(SLUS|SCUS|SLES|SCES|SLPM|SLPS)[-_ ]?(\d{3})[._ ]?(\d{2})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // ============================================================
        //  MÉTODO PRINCIPAL (HÍBRIDO)
        // ============================================================
        public static string? DetectGameId(string vcdPath)
        {
            // 1. SYSTEM.CNF (PS1 y algunos PS2)
            var id = DetectFromSystemCnf(vcdPath);
            if (!string.IsNullOrWhiteSpace(id))
                return id;

            // 2. IOPRP.IMG (PS2 real)
            id = DetectPs2FromIoprp(vcdPath);
            if (!string.IsNullOrWhiteSpace(id))
                return id;

            // 3. Nombre del archivo
            id = DetectFromName(Path.GetFileName(vcdPath));
            if (!string.IsNullOrWhiteSpace(id))
                return id;

            return null;
        }

        // ============================================================
        //  DETECCIÓN REAL DESDE SYSTEM.CNF (PS1 + algunos PS2)
        // ============================================================
        private static string? DetectFromSystemCnf(string vcdPath)
        {
            try
            {
                byte[] buffer = new byte[8 * 1024 * 1024];

                using (var fs = new FileStream(vcdPath, FileMode.Open, FileAccess.Read))
                    fs.Read(buffer, 0, buffer.Length);

                string text = Encoding.ASCII.GetString(buffer);

                int index = text.IndexOf("SYSTEM.CNF", StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                    return null;

                string block = text.Substring(index, Math.Min(4000, text.Length - index));

                var match = BootRegex.Match(block);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value.ToUpper();
                    string part1 = match.Groups[2].Value;
                    string part2 = match.Groups[3].Value;

                    return $"{prefix}_{part1}{part2}";
                }
            }
            catch { }

            return null;
        }

        // ============================================================
        //  DETECCIÓN REAL PS2 LEYENDO IOPRP.IMG
        // ============================================================
        private static string? DetectPs2FromIoprp(string vcdPath)
        {
            try
            {
                byte[] buffer = File.ReadAllBytes(vcdPath);
                string text = Encoding.ASCII.GetString(buffer);

                int index = text.IndexOf("IOPRP.IMG", StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                    return null;

                int start = Math.Max(0, index - 200000);
                int length = Math.Min(400000, buffer.Length - start);

                string block = Encoding.ASCII.GetString(buffer, start, length);

                var match = Ps2Regex.Match(block);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value.ToUpper();
                    string part1 = match.Groups[2].Value;
                    string part2 = match.Groups[3].Value;

                    return $"{prefix}_{part1}{part2}";
                }
            }
            catch { }

            return null;
        }

        // ============================================================
        //  DETECCIÓN DESDE EL NOMBRE DEL ARCHIVO
        // ============================================================
        public static string DetectFromName(string name)
        {
            name = name.ToUpperInvariant();

            foreach (var p in Patterns)
            {
                int index = name.IndexOf(p);
                if (index >= 0)
                {
                    string raw = name.Substring(index);

                    raw = raw.Replace("-", "_")
                             .Replace(" ", "_")
                             .Replace(".", "_");

                    var m = Regex.Match(raw, @"([A-Z]{4})[_ ]?(\d{3})(\d{2})?");
                    if (m.Success)
                    {
                        string prefix = m.Groups[1].Value;
                        string part1 = m.Groups[2].Value;
                        string part2 = m.Groups[3].Success ? m.Groups[3].Value : "00";

                        return $"{prefix}_{part1}{part2}";
                    }
                }
            }

            return "";
        }

        // ============================================================
        //  DETECTAR SI ES PAL
        // ============================================================
        public static bool IsPalRegion(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                return false;

            return gameId.StartsWith("SLES", StringComparison.OrdinalIgnoreCase) ||
                   gameId.StartsWith("SCES", StringComparison.OrdinalIgnoreCase) ||
                   gameId.StartsWith("PBPX", StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        //  DETECTAR SI REQUIERE PAL-60
        // ============================================================
        public static bool RequiresPal60(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                return false;

            string[] pal60Games =
            {
                "SLES00972", "SLES10972",
                "SLES02529",
                "SLES02205",
                "SLES01514",
                "SLES01370", "SLES11370",
                "SLES02080", "SLES12080",
                "SLES02965", "SLES12965",
                "SLES12380",
                "SCES01237",
                "SCES02105",
                "SCES02104",
                "SCES02835"
            };

            return pal60Games.Any(id => gameId.StartsWith(id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
