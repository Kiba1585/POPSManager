using System;
using System.IO;

namespace POPSManager.Services
{
    public class PathsService
    {
        public string PopsFolder { get; set; }
        public string AppsFolder { get; set; }
        public string BaseElfPath { get; private set; }

        private readonly Action<string>? log;

        public PathsService(Action<string>? log = null, SettingsService? settings = null)
        {
            this.log = log;

            // POPS
            PopsFolder = settings?.PopsFolder ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "POPS");

            if (!Directory.Exists(PopsFolder))
                Directory.CreateDirectory(PopsFolder);

            // APPS
            AppsFolder = settings?.AppsFolder ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "APPS");

            if (!Directory.Exists(AppsFolder))
                Directory.CreateDirectory(AppsFolder);

            // POPSTARTER.ELF
            if (settings != null && !string.IsNullOrWhiteSpace(settings.CustomElfPath))
                BaseElfPath = settings.CustomElfPath;
            else
                BaseElfPath = FindPopstarterElf();
        }

        private string FindPopstarterElf()
        {
            string[] searchPaths =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "POPSTARTER.ELF"),
                Path.Combine(PopsFolder, "POPSTARTER.ELF"),
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
                BaseElfPath = path;
                log?.Invoke($"POPSTARTER.ELF configurado manualmente: {path}");
            }
            else
            {
                log?.Invoke($"ERROR: Archivo no encontrado: {path}");
            }
        }
    }
}
