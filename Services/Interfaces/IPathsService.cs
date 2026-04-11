namespace POPSManager.Services.Interfaces
{
    /// <summary>
    /// Contrato para el servicio de rutas de la aplicación.
    /// </summary>
    public interface IPathsService
    {
        string RootFolder { get; }
        string PopsFolder { get; }
        string DvdFolder { get; }
        string AppsFolder { get; }
        string PopstarterElfPath { get; }

        /// <summary>Callback de log (para inyección en módulos externos).</summary>
        Action<string> LogAction { get; }
    }
}
