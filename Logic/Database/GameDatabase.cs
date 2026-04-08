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
            ps1Db = LoadDbFromResource(assembly, "POPSManager.Data.ps1db.json");

            // ============================
            // Cargar PS2
            // ============================
            ps2Db = LoadDbFromResource(assembly, "POPSManager.Data.ps2db.json");
        }

        private static Dictionary<string, GameInfo>? LoadDbFromResource(Assembly assembly, string resourceName)
        {
            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                    return null;

                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();

                return JsonSerializer.Deserialize<Dictionary<string, GameInfo>>(json);
            }
            catch
            {
                return null;
            }
        }

        // ============================
        // DETECCIÓN PS1 / PS2 REAL
        // ============================
        private static bool IsPs1(string id)
        {
            // PS1 → 4 dígitos
            // Ej: SCES_02105
            return id.Length == 10;
        }

        private static bool IsPs2(string id)
        {
            // PS2 → 5 dígitos
            // Ej: SLUS_20946
            return id.Length == 11;
        }

        // ============================
        // LOOKUP PRINCIPAL
        // ============================
        public static GameInfo? Lookup(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                return null;

            // Normalizar
            gameId = gameId.Trim().ToUpper();

            bool ps1 = IsPs1(gameId);
            bool ps2 = IsPs2(gameId);

            // ============================
            // 1. JSON local
            // ============================
            if (ps1 && ps1Db != null && ps1Db.TryGetValue(gameId, out var info1))
                return info1;

            if (ps2 && ps2Db != null && ps2Db.TryGetValue(gameId, out var info2))
                return info2;

            // Fallback cruzado (por si un ID está en la otra DB)
            if (ps1Db != null && ps1Db.TryGetValue(gameId, out var infoCross1))
                return infoCross1;

            if (ps2Db != null && ps2Db.TryGetValue(gameId, out var infoCross2))
                return infoCross2;

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
