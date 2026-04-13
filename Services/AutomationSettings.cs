using POPSManager.Logic.Automation;

namespace POPSManager.Settings
{
    /// <summary>
    /// Configuración de automatización por parte.
    /// Se puede guardar dentro de SettingsService o en un bloque separado.
    /// </summary>
    public sealed class AutomationSettings
    {
        // Modo global
        public AutomationMode Mode { get; set; } = AutomationMode.Asistido;

        // Comportamientos por área
        public AutoBehavior Conversion { get; set; } = AutoBehavior.Automatico;
        public AutoBehavior MultiDisc { get; set; } = AutoBehavior.Automatico;
        public AutoBehavior FolderCreation { get; set; } = AutoBehavior.Automatico;
        public AutoBehavior Covers { get; set; } = AutoBehavior.Preguntar;
        public AutoBehavior Database { get; set; } = AutoBehavior.Automatico;
        public AutoBehavior Cheats { get; set; } = AutoBehavior.Preguntar;
        public AutoBehavior Notifications { get; set; } = AutoBehavior.Automatico;
    }
}
