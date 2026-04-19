namespace POPSManager.Models
{
    /// <summary>
    /// Representa un juego que ha sido convertido a VCD.
    /// </summary>
    public class ConvertedGame
    {
        /// <summary>Ruta completa al archivo .VCD generado.</summary>
        public required string VcdPath { get; set; }

        /// <summary>Nombre base del juego (sin número de disco).</summary>
        public required string BaseName { get; set; }

        /// <summary>Número de disco (1 para juegos de un solo disco).</summary>
        public int DiscNumber { get; set; }

        /// <summary>Indica si el juego es parte de un conjunto multidisco.</summary>
        public bool IsMultiDisc { get; set; }
    }
}