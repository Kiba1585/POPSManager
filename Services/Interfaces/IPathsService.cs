namespace POPSManager.Services.Interfaces
{
    /// <summary>
    /// Contrato para el servicio de gestión de rutas de archivos y carpetas.
    /// </summary>
    public interface IPathsService
    {
        /// <summary>Carpeta raíz del dispositivo OPL.</summary>
        string RootFolder { get; }

        /// <summary>Carpeta POPS (juegos PS1).</summary>
        string PopsFolder { get; }

        /// <summary>Carpeta APPS (homebrew).</summary>
        string AppsFolder { get; }

        /// <summary>Subcarpeta CFG dentro de POPS.</summary>
        string CfgFolder { get; }

        /// <summary>Subcarpeta ART dentro de POPS.</summary>
        string ArtFolder { get; }

        /// <summary>Carpeta DVD (juegos PS2).</summary>
        string DvdFolder { get; }

        /// <summary>Ruta al archivo POPSTARTER.ELF.</summary>
        string PopstarterElfPath { get; }

        /// <summary>Ruta al archivo POPS2.ELF (para PS2).</summary>
        string PopstarterPs2ElfPath { get; }

        /// <summary>Establece una carpeta POPS personalizada.</summary>
        void SetCustomPopsFolder(string path);

        /// <summary>Establece una carpeta APPS personalizada.</summary>
        void SetCustomAppsFolder(string path);

        /// <summary>Establece una ruta personalizada para POPSTARTER.ELF.</summary>
        void SetCustomElfPath(string path);

        /// <summary>Establece una ruta personalizada para POPS2.ELF.</summary>
        void SetCustomPs2ElfPath(string path);

        /// <summary>Recarga todas las rutas desde la configuración.</summary>
        void Reload();

        /// <summary>Guarda la configuración actual de rutas.</summary>
        void Save();

        /// <summary>
        /// Construye una ruta en formato mass:/ para OPL.
        /// </summary>
        /// <param name="fullPath">Ruta física completa.</param>
        /// <returns>Ruta en formato mass:/POPS/...</returns>
        string BuildMassPath(string fullPath);
    }
}