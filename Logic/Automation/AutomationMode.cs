namespace POPSManager.Logic.Automation
{
    public enum AutomationMode
    {
        Manual = 0,      // Nada se hace solo, todo lo decide el usuario
        Asistido = 1,    // El programa propone y pregunta
        Automatico = 2   // Hace todo lo posible sin preguntar
    }

    public enum AutoBehavior
    {
        Manual = 0,      // Nunca automático, siempre intervención
        Preguntar = 1,   // Preguntar cuando aplique
        Automatico = 2   // Hacerlo sin preguntar
    }
}
