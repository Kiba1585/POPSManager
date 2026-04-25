using POPSManager.Logic.Automation;

namespace POPSManager.Settings
{
    /// <summary>
    /// Configuración de automatización inteligente.
    /// Define el modo global y el comportamiento por cada parte del proceso.
    /// </summary>
    public sealed class AutomationSettings
    {
        /// <summary>Modo global de automatización.</summary>
        public AutomationMode Mode { get; set; } = AutomationMode.Asistido;

        /// <summary>Comportamiento para normalización de nombres.</summary>
        public AutoBehavior Conversion { get; set; } = AutoBehavior.Automatico;

        /// <summary>Comportamiento para agrupación de juegos multidisco.</summary>
        public AutoBehavior MultiDisc { get; set; } = AutoBehavior.Automatico;

        /// <summary>Comportamiento para creación de carpetas.</summary>
        public AutoBehavior FolderCreation { get; set; } = AutoBehavior.Automatico;

        /// <summary>Comportamiento para generación de archivos ELF.</summary>
        public AutoBehavior ElfGeneration { get; set; } = AutoBehavior.Automatico;

        /// <summary>Comportamiento para descarga de carátulas.</summary>
        public AutoBehavior Covers { get; set; } = AutoBehavior.Preguntar;

        /// <summary>Comportamiento para uso de base de datos.</summary>
        public AutoBehavior Database { get; set; } = AutoBehavior.Automatico;

        /// <summary>Comportamiento para generación de cheats.</summary>
        public AutoBehavior Cheats { get; set; } = AutoBehavior.Preguntar;

        /// <summary>Comportamiento para generación de metadatos (.cfg).</summary>
        public AutoBehavior Metadata { get; set; } = AutoBehavior.Automatico;

        /// <summary>Comportamiento para notificaciones.</summary>
        public AutoBehavior Notifications { get; set; } = AutoBehavior.Automatico;

        /// <summary>Comportamiento para copiar archivos de idioma (LNG).</summary>
        public AutoBehavior Lng { get; set; } = AutoBehavior.Automatico;

        /// <summary>Comportamiento para copiar temas (THM).</summary>
        public AutoBehavior Thm { get; set; } = AutoBehavior.Automatico;
    }
}