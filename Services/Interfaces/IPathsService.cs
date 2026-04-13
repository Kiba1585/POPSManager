namespace POPSManager.Services.Interfaces
{
    public interface IPathsService
    {
        string RootFolder { get; }
        string PopsFolder { get; }
        string AppsFolder { get; }
        string CfgFolder { get; }
        string ArtFolder { get; }
        string DvdFolder { get; }
        string PopstarterElfPath { get; }
        string PopstarterPs2ElfPath { get; }

        void SetCustomPopsFolder(string path);
        void SetCustomAppsFolder(string path);
        void SetCustomElfPath(string path);
        void SetCustomPs2ElfPath(string path);

        void Reload();
        void Save();
        string BuildMassPath(string fullPath);
    }
}
