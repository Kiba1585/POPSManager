namespace POPSManager.Logic.Automation
{
    /// <summary>
    /// Modo global de automatización.
    /// </summary>
    public enum AutomationMode
    {
        Manual = 0,
        Asistido = 1,
        Automatico = 2
    }

    /// <summary>
    /// Comportamiento específico para cada acción automatizable.
    /// </summary>
    public enum AutoBehavior
    {
        /// <summary>
        /// No ejecutar nunca.
        /// </summary>
        Manual = 0,

        /// <summary>
        /// Preguntar al usuario antes de ejecutar.
        /// </summary>
        Preguntar = 1,

        /// <summary>
        /// Ejecutar siempre sin preguntar.
        /// </summary>
        Automatico = 2
    }
}