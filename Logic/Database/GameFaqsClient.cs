using POPSManager.Models;

namespace POPSManager.Logic.Database
{
    public static class GameFaqsClient
    {
        public static GameInfo? Lookup(string gameId)
        {
            // ============================================================
            //  PLANTILLA BASE
            //  Aquí puedes implementar:
            //  - Scraping de GameFAQs
            //  - API no oficial
            //  - Búsqueda por nombre aproximado
            // ============================================================

            // Ejemplo de cómo se vería una respuesta real:
            /*
            return new GameInfo
            {
                Name = "Metal Gear Solid",
                DiscNumber = 1,
                CoverUrl = "https://gamefaqs.gamespot.com/a/box/0/1/7/217_front.jpg"
            };
            */

            return null; // Fallback automático
        }
    }
}
