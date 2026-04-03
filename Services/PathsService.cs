using System.IO;

namespace POPSManager.Services
{
    public class PathsService
    {
        public string PopsFolder { get; set; }
        public string AppsFolder { get; set; }

        public PathsService()
        {
            PopsFolder = Path.Combine(AppContext.BaseDirectory, "POPS");
            AppsFolder = Path.Combine(AppContext.BaseDirectory, "APPS");

            Directory.CreateDirectory(PopsFolder);
            Directory.CreateDirectory(AppsFolder);
        }
    }
}
