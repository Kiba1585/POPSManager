using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace POPSManager.Logic
{
    public static class CheatGenerator
    {
        public static void GenerateCheatTxt(string gameId, string popsDiscFolder, Action<string> log)
        {
            try
            {
                string cheatPath = Path.Combine(popsDiscFolder, "CHEAT.TXT");

                // Comandos base para convertir PAL → NTSC
                var lines = new List<string>
                {
                    "VMODE=NTSC",
                    "BORDER=OFF",
                    "CENTER=ON"
                };

                // ============================================================
                // 1. PAL-60 automático (mejor compatibilidad)
                // ============================================================
                if (GameIdDetector.RequiresPal60(gameId))
                {
                    lines.Add("FORCEVIDEO=1");
                }

                // ============================================================
                // 2. Juegos problemáticos (parcheo automático)
                // ============================================================
                if (IsResidentEvil(gameId))
                {
                    lines.Add("FORCEVIDEO=1");
                    lines.Add("SKIPVIDEOS=ON");
                }

                if (IsSilentHill(gameId) || IsMetalGearSolid(gameId))
                {
                    lines.Add("FORCEVIDEO=1");
                }

                // Guardar archivo
                File.WriteAllLines(cheatPath, lines);

                log($"[PS1] CHEAT.TXT generado → {cheatPath}");
            }
            catch (Exception ex)
            {
                log($"[PS1] Error generando CHEAT.TXT: {ex.Message}");
            }
        }

        // ============================================================
        //  DETECTORES DE JUEGOS PROBLEMÁTICOS
        // ============================================================

        private static bool IsResidentEvil(string gameId)
        {
            string[] ids =
            {
                "SLES00972", "SLES10972", // Resident Evil 2
                "SLES02529"              // Resident Evil 3
            };

            return ids.Any(id => gameId.StartsWith(id, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSilentHill(string gameId)
        {
            return gameId.StartsWith("SLES01514", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMetalGearSolid(string gameId)
        {
            return gameId.StartsWith("SLES01370", StringComparison.OrdinalIgnoreCase) ||
                   gameId.StartsWith("SLES11370", StringComparison.OrdinalIgnoreCase);
        }
    }
}
