namespace POPSManager.Logic.Automation
{
    public enum AutomationMode
    {
        Manual = 0,      // Nada automático
        Asistido = 1,    // Pregunta antes de actuar
        Automatico = 2   // Hace todo sin preguntar
    }

    public enum AutoBehavior
    {
        Manual = 0,      // Nunca automático
        Preguntar = 1,   // Preguntar antes
        Automatico = 2   // Hacerlo sin preguntar
    }
}
