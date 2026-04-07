using System;
using System.IO;

namespace POPSManager.Services
{
    public class PathsService
    {
        public string RootFolder { get; private set; }

        // Rutas dinámicas (si el usuario no personaliza, se generan desde RootFolder)
        public string PopsFolder => _customPopsFolder ?? Path.Combine(RootFolder, "POPS");
        public string AppsFolder => _customAppsFolder ?? Path.Combine(RootFolder, "APPS");

        public string CfgFolder  => Path.Combine(RootFolder, "CFG");
        public string ArtFolder  => Path.Combine(RootFolder, "ART");
        public string DvdFolder  => Path.Combine(RootFolder, "DVD");

        public string PopstarterElfPath { get; private set; }

        private string? _customPopsFolder;
        private string? _customAppsFolder;

        private readonly Action<string>? log;
        private readonly SettingsService? settings;

        public PathsService(Action<string>? log = null, SettingsService? settings = null)
        {
            this.log = log;
            this.settings = settings;

            RootFolder = settings?.RootFolder ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "POPSManager");

            // Cargar rutas personalizadas si existen
            _customPopsFolder = settings?.CustomPopsFolder;
            _customAppsFolder = settings?.CustomAppsFolder;

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
            CreateFolder(DvdFolder);
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

        // ============================================================
        //  MÉTODOS NUEVOS PARA SETTINGSVIEW
        // ============================================================

        public void SetCustomPopsFolder(string path)
        {
            _customPopsFolder = path;
            CreateFolder(path);
            Save();
            log?.Invoke($"Ruta POPS personalizada: {path}");
        }

        public void SetCustomAppsFolder(string path)
        {
            _customAppsFolder = path;
            CreateFolder(path);
            Save();
            log?.Invoke($"Ruta APPS personalizada: {path}");
        }

        public void SetCustomElfPath(string path)
        {
            if (File.Exists(path))
            {
                PopstarterElfPath = path;
                Save();
                log?.Invoke($"POPSTARTER.ELF configurado manualmente: {path}");
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

            // Guardar rutas personalizadas
            settings.CustomPopsFolder = _customPopsFolder;
            settings.CustomAppsFolder = _customAppsFolder;

            settings.Save();
        }
    }
}
