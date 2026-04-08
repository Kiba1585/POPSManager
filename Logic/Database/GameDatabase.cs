using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace POPSManager.Logic
{
    public static class GameDatabase
    {
        private static readonly Dictionary<string, GameEntry> Cache = new(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, GameEntry>? ps1Db;
        private static Dictionary<string, GameEntry>? ps2Db;

        // ============================================================
        //  CARGA DE BASES DE DATOS EMBEBIDAS
        // ============================================================
        static GameDatabase()
        {
            ps1Db = LoadJson("POPSManager.Data.ps1db.json");
            ps2Db = LoadJson("POPSManager.Data.ps2db.json");
        }

        private static Dictionary<string, GameEntry>? LoadJson(string resourceName)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null)
                    return null;

                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();

                return JsonSerializer.Deserialize<Dictionary<string, GameEntry>>(json);
            }
            catch
            {
                return null;
            }
        }

        // ============================================================
        //  MÉTODO PRINCIPAL
        // ============================================================
        public static bool TryGetEntry(string gameId, out GameEntry entry)
        {
            entry = null!;

            if (string.IsNullOrWhiteSpace(gameId))
                return false;

            // Cache
            if (Cache.TryGetValue(gameId, out entry))
                return true;

            // PS1
            if (ps1Db != null && ps1Db.TryGetValue(gameId, out entry))
            {
                Cache[gameId] = entry;
                return true;
            }

            // PS2
            if (ps2Db != null && ps2Db.TryGetValue(gameId, out entry))
            {
                Cache[gameId] = entry;
                return true;
            }

            // Online lookup (opcional)
            entry = TryOnlineLookup(gameId);
            if (entry != null)
            {
                Cache[gameId] = entry;
                return true;
            }

            return false;
        }

        // ============================================================
        //  LOOKUP ONLINE (Redump / GameFAQs / PSXDatacenter)
        // ============================================================
        private static GameEntry? TryOnlineLookup(string gameId)
        {
            try
            {
                // Aquí no hacemos llamadas reales.
                // POPSManager puede implementar un plugin opcional.
                return null;
            }
            catch
            {
                return null;
            }
        }

        // ============================================================
        //  OBTENER COVER
        // ============================================================
        public static string? TryGetCover(string gameId)
        {
            if (!TryGetEntry(gameId, out var entry))
                return null;

            return entry.CoverUrl;
        }

        // ============================================================
        //  OBTENER FIXES PARA CHEAT.TXT
        // ============================================================
        public static IEnumerable<string>? TryGetFixes(string gameId)
        {
            if (!TryGetEntry(gameId, out var entry))
                return null;

            return entry.CheatFixes;
        }

        // ============================================================
        //  OBTENER METADATA
        // ============================================================
        public static GameEntry? TryGetMetadata(string gameId)
        {
            if (!TryGetEntry(gameId, out var entry))
                return null;

            return entry;
        }
    }

    // ============================================================
    //  MODELO DE DATOS ULTRA PRO
    // ============================================================
    public class GameEntry
    {
        public string GameId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Region { get; set; } = "";
        public string Publisher { get; set; } = "";
        public int Year { get; set; }

        // MultiDisc
        public int DiscCount { get; set; }
        public string[]? DiscNames { get; set; }

        // Covers
        public string? CoverUrl { get; set; }

        // Tags
        public string[]? Tags { get; set; }

        // Fixes
        public string[]? CheatFixes { get; set; }
        public string[]? GraphicsFixes { get; set; }
        public string[]? VideoFixes { get; set; }
        public string[]? SoundFixes { get; set; }

        // Flags
        public bool HasFmvIssues { get; set; }
        public bool HasTimingIssues { get; set; }
        public bool RequiresPal60 { get; set; }
        public bool RequiresSkipVideos { get; set; }
        public bool RequiresFixCdda { get; set; }
        public bool RequiresFixGraphics { get; set; }
        public bool RequiresFixSound { get; set; }
    }
}
