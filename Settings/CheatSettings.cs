namespace POPSManager.Settings
{
    /// <summary>
    /// Modo de generación de cheats.
    /// </summary>
    public enum CheatMode
    {
        /// <summary>No generar CHEAT.TXT nunca.</summary>
        Disabled,

        /// <summary>Usar CheatGenerator solo para juegos PAL.</summary>
        AutoForPal,

        /// <summary>Preguntar por cada juego al procesar.</summary>
        AskEachTime,

        /// <summary>No automático, solo edición manual.</summary>
        ManualSelection
    }

    /// <summary>
    /// Configuración detallada de generación de cheats.
    /// </summary>
    public class CheatSettings
    {
        /// <summary>Modo principal de generación.</summary>
        public CheatMode Mode { get; set; } = CheatMode.AutoForPal;

        /// <summary>Usar fixes automáticos por juego (CheatGenerator).</summary>
        public bool UseAutoGameFixes { get; set; } = true;

        /// <summary>Usar fixes por engine (Crash, Spyro, FF...).</summary>
        public bool UseEngineFixes { get; set; } = true;

        /// <summary>Usar fixes heurísticos.</summary>
        public bool UseHeuristicFixes { get; set; } = true;

        /// <summary>Usar fixes desde GameDatabase.</summary>
        public bool UseDatabaseFixes { get; set; } = true;

        /// <summary>Permitir cheats personalizados del usuario.</summary>
        public bool EnableCustomCheats { get; set; } = true;
    }
}