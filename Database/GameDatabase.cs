using System.Reflection;
using System.Text.Json;

namespace POPSManager.Logic
{
    public static class GameDatabase
    {
        private static Dictionary<string, GameInfo>? localDb;

        static GameDatabase()
        {
            LoadEmbeddedJson();
        }

        private static void LoadEmbeddedJson()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "POPSManager.Data.ps1db.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return;

            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();

            localDb = JsonSerializer.Deserialize<Dictionary<string, GameInfo>>(json);
        }

        public static GameInfo? Lookup(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                return null;

            // 1. Buscar en JSON local
            if (localDb != null && localDb.TryGetValue(gameId, out var info))
                return info;

            // 2. Buscar en Redump
            info = RedumpClient.Lookup(gameId);
            if (info != null)
                return info;

            // 3. Buscar en GameFAQs
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

    public class GameInfo
    {
        public string Name { get; set; } = "";
        public int DiscNumber { get; set; } = 1;
        public string? CoverUrl { get; set; }
    }
}
