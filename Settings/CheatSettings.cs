namespace POPSManager.Settings
{
    public enum CheatMode
    {
        Disabled,       // No generar CHEAT.TXT nunca
        AutoForPal,     // Usar CheatGenerator solo para juegos PAL
        AskEachTime,    // Preguntar por cada juego al procesar
        ManualSelection // No auto, solo editor/manual
    }

    public class CheatSettings
    {
        public CheatMode Mode { get; set; } = CheatMode.AutoForPal;

        // ¿Usar fixes automáticos por juego (CheatGenerator)?
        public bool UseAutoGameFixes { get; set; } = true;

        // ¿Usar fixes por engine (Crash, Spyro, FF...)?
        public bool UseEngineFixes { get; set; } = true;

        // ¿Usar fixes heurísticos?
        public bool UseHeuristicFixes { get; set; } = true;

        // ¿Usar fixes desde GameDatabase?
        public bool UseDatabaseFixes { get; set; } = true;

        // ¿Permitir cheats personalizados del usuario?
        public bool EnableCustomCheats { get; set; } = true;
    }
}
