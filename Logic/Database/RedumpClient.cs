using POPSManager.Models;

namespace POPSManager.Logic.Database
{
    public static class RedumpClient
    {
        public static GameInfo? Lookup(string gameId)
        {
            // ============================================================
            //  PLANTILLA BASE
            //  Aquí puedes implementar:
            //  - API real de Redump
            //  - Scraping HTML
            //  - Cache local
            // ============================================================

            // Ejemplo de cómo se vería una respuesta real:
            /*
            return new GameInfo
            {
                Name = "Crash Team Racing",
                DiscNumber = 1,
                CoverUrl = "https://..."
            };
            */

            return null; // Fallback automático
        }
    }
}
