using System;
using System.IO;

namespace POPSManager.Services
{
    public class PathsService
    {
        public string RootFolder { get; private set; }
        public string PopsFolder => Path.Combine(RootFolder, "POPS");
        public string AppsFolder => Path.Combine(RootFolder, "APPS");
        public string CfgFolder => Path.Combine(RootFolder, "CFG");
        public string ArtFolder => Path.Combine(RootFolder, "ART");

        public string PopstarterElfPath { get; private set; }

        private readonly Action<string>? log;
        private readonly SettingsService? settings;

        public PathsService(Action<string>? log = null, SettingsService? settings = null)
        {
            this.log = log;
            this.settings = settings;

            RootFolder = settings?.RootFolder ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "POPSManager");

            EnsureFolderStructure();

            PopstarterElfPath = ResolveElfPath();
        }

        private void EnsureFolderStructure()
        {
            CreateFolder(RootFolder);
            CreateFolder(PopsFolder);
            CreateFolder(AppsFolder);
            CreateFolder(CfgFolder);
            CreateFolder(ArtFolder);
        }

        private void CreateFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                log?.Invoke($"Carpeta creada: {path}");
            }
        }

        private string ResolveElfPath()
        {
            if (settings != null && !string.IsNullOrWhiteSpace(settings.CustomElfPath))
            {
                log?.Invoke($"Usando POPSTARTER.ELF personalizado: {settings.CustomElfPath}");
                return settings.CustomElfPath;
            }

            return FindPopstarterElf();
        }

        private string FindPopstarterElf()
        {
            string[] searchPaths =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "POPSTARTER.ELF"),
                Path.Combine(RootFolder, "POPSTARTER.ELF"),
                Path.Combine(PopsFolder, "POPSTARTER.ELF"),
                Path.Combine(AppsFolder, "POPSTARTER.ELF"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "POPSTARTER.ELF")
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    log?.Invoke($"POPSTARTER.ELF encontrado en: {path}");
                    return path;
                }
            }

            log?.Invoke("ADVERTENCIA: No se encontró POPSTARTER.ELF.");
            return "";
        }

        public void SetCustomElfPath(string path)
        {
            if (File.Exists(path))
            {
                PopstarterElfPath = path;
                log?.Invoke($"POPSTARTER.ELF configurado manualmente: {path}");
                Save();
            }
            else
            {
                log?.Invoke($"ERROR: Archivo no encontrado: {path}");
            }
        }

        public void Save()
        {
            if (settings == null)
                return;

            settings.RootFolder = RootFolder;
            settings.CustomElfPath = PopstarterElfPath;
            settings.Save();
        }
    }
}
