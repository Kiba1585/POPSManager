using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace POPSManager.Logic
{
    public static class CheatGenerator
    {
        /// <summary>
        /// Genera CHEAT.TXT con fixes avanzados para PS1 PAL.
        /// Solo CD1 genera CHEAT.TXT en multidisco.
        /// </summary>
        public static void GenerateCheatTxt(string gameId, string popsDiscFolder, Action<string> log)
        {
            try
            {
                // Solo PS1
                if (!GameIdValidator.IsPs1(gameId))
                {
                    log("[PS1] No se genera CHEAT.TXT para PS2.");
                    return;
                }

                // Solo PAL
                if (!GameIdDetector.IsPalRegion(gameId))
                {
                    log("[PS1] No se genera CHEAT.TXT para juegos NTSC.");
                    return;
                }

                // Solo CD1 en multidisco
                if (IsMultiDiscFolder(popsDiscFolder) && !IsDisc1Folder(popsDiscFolder))
                {
                    log("[PS1] Multidisco detectado. CHEAT.TXT solo se genera en CD1.");
                    return;
                }

                string cheatPath = Path.Combine(popsDiscFolder, "CHEAT.TXT");

                var lines = new List<string>
                {
                    "VMODE=NTSC",
                    "BORDER=OFF",
                    "CENTER=ON"
                };

                // ============================================================
                // 1. PAL-60 automático
                // ============================================================
                if (GameIdDetector.RequiresPal60(gameId))
                {
                    lines.Add("FORCEVIDEO=1");
                    log("[PS1] PAL-60 aplicado automáticamente.");
                }

                // ============================================================
                // 2. Fixes por juego (base de datos interna)
                // ============================================================
                ApplyGameSpecificFixes(gameId, lines, log);

                // ============================================================
                // 3. Fixes por engine (Crash, Spyro, FF…)
                // ============================================================
                ApplyEngineFixes(gameId, lines, log);

                // ============================================================
                // 4. Fixes heurísticos seguros
                // ============================================================
                ApplyHeuristicFixes(gameId, lines, log);

                // ============================================================
                // 5. Fixes desde GameDatabase (si existe entrada avanzada)
                // ============================================================
                ApplyDatabaseFixes(gameId, lines, log);

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
        //  DETECTAR MULTIDISCO
        // ============================================================
        private static bool IsMultiDiscFolder(string folder)
        {
            string? parent = Path.GetDirectoryName(folder);
            if (parent == null)
                return false;

            return Directory.GetFiles(parent)
                .Any(f => f.EndsWith("DISCS.TXT", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsDisc1Folder(string folder)
        {
            string name = Path.GetFileName(folder).ToUpperInvariant();
            return name.Contains("CD1") || name.Contains("(CD1)") || name.Contains("DISC1");
        }

        // ============================================================
        //  FIXES AVANZADOS POR JUEGO (BASE DE DATOS INTERNA)
        // ============================================================
        private static void ApplyGameSpecificFixes(string gameId, List<string> lines, Action<string> log)
        {
            string id = gameId.ToUpperInvariant();

            var fixes = new Dictionary<string, Action>
            {
                // Resident Evil 2 / 3
                { "SLES00972", () => { lines.Add("FORCEVIDEO=1"); lines.Add("SKIPVIDEOS=ON"); } },
                { "SLES10972", () => { lines.Add("FORCEVIDEO=1"); lines.Add("SKIPVIDEOS=ON"); } },
                { "SLES02529", () => { lines.Add("FORCEVIDEO=1"); lines.Add("SKIPVIDEOS=ON"); } },

                // Silent Hill
                { "SLES01514", () => lines.Add("FORCEVIDEO=1") },

                // Metal Gear Solid
                { "SLES01370", () => lines.Add("FORCEVIDEO=1") },
                { "SLES11370", () => lines.Add("FORCEVIDEO=1") },

                // Final Fantasy VIII / IX
                { "SLES02080", () => lines.Add("FORCEVIDEO=1") },
                { "SLES12080", () => lines.Add("FORCEVIDEO=1") },
                { "SLES02965", () => lines.Add("FORCEVIDEO=1") },
                { "SLES12965", () => lines.Add("FORCEVIDEO=1") },

                // Gran Turismo 2
                { "SLES12380", () => { lines.Add("FORCEVIDEO=1"); lines.Add("FIXGRAPHICS=ON"); } },

                // Tekken 3
                { "SCES01237", () => lines.Add("FORCEVIDEO=1") },

                // Crash Team Racing
                { "SCES02105", () => lines.Add("FORCEVIDEO=1") },

                // Spyro 2 / 3
                { "SCES02104", () => lines.Add("FORCEVIDEO=1") },
                { "SCES02835", () => lines.Add("FORCEVIDEO=1") }
            };

            foreach (var kv in fixes)
            {
                if (id.StartsWith(kv.Key))
                {
                    kv.Value();
                    log($"[PS1] Fix aplicado: {kv.Key}");
                }
            }
        }

        // ============================================================
        //  FIXES POR ENGINE (Crash, Spyro, FF…)
        // ============================================================
        private static void ApplyEngineFixes(string gameId, List<string> lines, Action<string> log)
        {
            string id = gameId.ToUpperInvariant();

            // Crash Bandicoot engine
            if (id.StartsWith("SCES00") || id.StartsWith("SCUS94"))
            {
                lines.Add("FIXSOUND=ON");
                log("[PS1] Fix engine: Crash Bandicoot (FIXSOUND)");
            }

            // Spyro engine
            if (id.StartsWith("SCES02") || id.StartsWith("SCUS94"))
            {
                lines.Add("FIXGRAPHICS=ON");
                log("[PS1] Fix engine: Spyro (FIXGRAPHICS)");
            }

            // Final Fantasy engine
            if (id.StartsWith("SLES02") || id.StartsWith("SCES02"))
            {
                lines.Add("FIXCDDA=ON");
                log("[PS1] Fix engine: Final Fantasy (FIXCDDA)");
            }
        }

        // ============================================================
        //  FIXES HEURÍSTICOS SEGUROS
        // ============================================================
        private static void ApplyHeuristicFixes(string gameId, List<string> lines, Action<string> log)
        {
            string id = gameId.ToUpperInvariant();

            // Heurística real: IDs terminados en 80 o 65 suelen tener timings sensibles
            if (id.EndsWith("80") || id.EndsWith("65"))
            {
                lines.Add("FORCEVIDEO=1");
                log("[PS1] Heurística: Timings sensibles detectados (FORCEVIDEO)");
            }
        }

        // ============================================================
        //  FIXES DESDE GAMEDATABASE (si existe entrada avanzada)
        // ============================================================
        private static void ApplyDatabaseFixes(string gameId, List<string> lines, Action<string> log)
        {
            if (!GameDatabase.TryGetEntry(gameId, out var entry) || entry == null)
                return;

            if (entry.CheatFixes == null)
                return;

            foreach (var fix in entry.CheatFixes)
            {
                if (!string.IsNullOrWhiteSpace(fix))
                {
                    lines.Add(fix);
                    log($"[PS1] Fix desde GameDatabase: {fix}");
                }
            }
        }
    }
}
