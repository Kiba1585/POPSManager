namespace POPSManager.Services.Interfaces
{
    /// <summary>
    /// Contrato para el servicio de rutas del sistema.
    /// Coincide exactamente con los miembros públicos de PathsService.
    /// </summary>
    public interface IPathsService
    {
        // ── Propiedades de solo lectura ──
        string RootFolder { get; }
        string PopsFolder { get; }
        string AppsFolder { get; }
        string CfgFolder { get; }
        string ArtFolder { get; }
        string DvdFolder { get; }
        string PopstarterElfPath { get; }
        string PopstarterPs2ElfPath { get; }

        // ── Métodos de configuración ──
        void SetCustomPopsFolder(string path);
        void SetCustomAppsFolder(string path);
        void SetCustomElfPath(string path);
        void SetCustomPs2ElfPath(string path);

        // ── Operaciones ──
        void Reload();
        void Save();
        string BuildMassPath(string fullPath);
    }
}
