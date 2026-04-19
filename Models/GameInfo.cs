namespace POPSManager.Models
{
    /// <summary>
    /// Información básica de un juego (usada en búsquedas online).
    /// </summary>
    public class GameInfo
    {
        /// <summary>Nombre del juego.</summary>
        public string Name { get; set; } = "";

        /// <summary>Número de disco (por defecto 1).</summary>
        public int DiscNumber { get; set; } = 1;

        /// <summary>URL de la carátula (opcional).</summary>
        public string? CoverUrl { get; set; }
    }
}