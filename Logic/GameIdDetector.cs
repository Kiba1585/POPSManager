using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class GameIdDetector
    {
        private static readonly string[] Patterns =
        {
            "SCES", "SLES", "SLUS", "SCUS", "SLPS", "SLPM", "SCPS"
        };

        // ============================================================
        //  DETECCIÓN DESDE ARCHIVO (VCD)
        //  (Actualmente no implementado, pero mantenido para futuro)
        // ============================================================
        public static string? DetectGameId(string vcdPath)
        {
            // Aquí podrías leer sectores del VCD si deseas detección real
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
                if (name.Contains(p))
                {
                    int index = name.IndexOf(p);
                    string id = name.Substring(index);

                    // Normalizar
                    id = id.Replace("-", "_")
                           .Replace(" ", "_");

                    // Limitar longitud típica SCES_12345
                    if (id.Length > 12)
                        id = id.Substring(0, 12);

                    return id;
                }
            }

            return "";
        }

        // ============================================================
        //  DETECTAR SI EL JUEGO ES PAL
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
        //  DETECTAR SI EL JUEGO REQUIERE PAL-60
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
