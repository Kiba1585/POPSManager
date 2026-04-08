using System.Reflection;
using System.Text.Json;
using POPSManager.Models;

namespace POPSManager.Logic.Database
{
    public static class GameDatabase
    {
        private static Dictionary<string, GameInfo>? ps1Db;
        private static Dictionary<string, GameInfo>? ps2Db;

        static GameDatabase()
        {
            LoadEmbeddedJson();
        }

        private static void LoadEmbeddedJson()
        {
            var assembly = Assembly.GetExecutingAssembly();

            // ============================
            // Cargar PS1
            // ============================
            using (var stream = assembly.GetManifestResourceStream("POPSManager.Data.ps1db.json"))
            {
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    string json = reader.ReadToEnd();
                    ps1Db = JsonSerializer.Deserialize<Dictionary<string, GameInfo>>(json);
                }
            }

            // ============================
            // Cargar PS2
            // ============================
            using (var stream = assembly.GetManifestResourceStream("POPSManager.Data.ps2db.json"))
            {
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    string json = reader.ReadToEnd();
                    ps2Db = JsonSerializer.Deserialize<Dictionary<string, GameInfo>>(json);
                }
            }
        }

        // Detectar si un ID es PS1 o PS2
        private static bool IsPs1(string id)
        {
            return id.StartsWith("SCES") ||
                   id.StartsWith("SLES") ||
                   id.StartsWith("SCUS") ||
                   id.StartsWith("SLUS") ||
                   id.StartsWith("SLPS") ||
                   id.StartsWith("SLPM") ||
                   id.StartsWith("SCPS");
        }

        private static bool IsPs2(string id)
        {
            return id.StartsWith("SLES") ||
                   id.StartsWith("SLUS") ||
                   id.StartsWith("SCUS") ||
                   id.StartsWith("SCES") ||
                   id.StartsWith("SLPM") ||
                   id.StartsWith("SLPS");
        }

        public static GameInfo? Lookup(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                return null;

            bool ps1 = IsPs1(gameId);
            bool ps2 = IsPs2(gameId);

            // ============================
            // 1. JSON local
            // ============================
            if (ps1 && ps1Db != null && ps1Db.TryGetValue(gameId, out var info1))
                return info1;

            if (ps2 && ps2Db != null && ps2Db.TryGetValue(gameId, out var info2))
                return info2;

            // ============================
            // 2. Redump
            // ============================
            var info = RedumpClient.Lookup(gameId);
            if (info != null)
                return info;

            // ============================
            // 3. GameFAQs
            // ============================
            info = GameFaqsClient.Lookup(gameId);
            if (info != null)
                return info;

            return null;
        }

        public static string? LookupName(string gameId)
        {
            return Lookup(gameId)?.Name;
        }
    }
}
