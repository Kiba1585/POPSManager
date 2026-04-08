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

        // ============================================================
        //  DETECCIÓN REAL DESDE EL VCD (LEYENDO SYSTEM.CNF)
        // ============================================================
        public static string? DetectGameId(string vcdPath)
        {
            try
            {
                // Leer primeros 8 MB (más seguro que 3 MB)
                byte[] buffer = new byte[8 * 1024 * 1024];

                using (var fs = new FileStream(vcdPath, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(buffer, 0, buffer.Length);
                }

                string text = Encoding.ASCII.GetString(buffer);

                // Buscar SYSTEM.CNF
                int index = text.IndexOf("SYSTEM.CNF", StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                    return null;

                // Extraer un bloque alrededor
                string block = text.Substring(index, Math.Min(4000, text.Length - index));

                // Buscar BOOT
                var match = BootRegex.Match(block);

                if (match.Success)
                {
                    string prefix = match.Groups[1].Value.ToUpper();
                    string part1 = match.Groups[2].Value;
                    string part2 = match.Groups[3].Value;

                    // Normalizar → SCES_02105
                    return $"{prefix}_{part1}{part2}";
                }
            }
            catch
            {
                return null;
            }

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

                    // Quitar basura
                    raw = raw.Replace("-", "_")
                             .Replace(" ", "_")
                             .Replace(".", "_");

                    // Extraer solo prefijo + números
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
                "SLES00972", "SLES10972", // Resident Evil 2
                "SLES02529",             // Resident Evil 3
                "SLES02205",             // Dino Crisis
                "SLES01514",             // Silent Hill
                "SLES01370", "SLES11370",// Metal Gear Solid
                "SLES02080", "SLES12080",// Final Fantasy VIII
                "SLES02965", "SLES12965",// Final Fantasy IX
                "SLES12380",             // Gran Turismo 2
                "SCES01237",             // Tekken 3
                "SCES02105",             // Crash Team Racing
                "SCES02104",             // Spyro 2
                "SCES02835"              // Spyro 3
            };

            return pal60Games.Any(id => gameId.StartsWith(id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
