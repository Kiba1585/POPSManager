using System;
using System.IO;

namespace POPSManager.Services
{
    public class PathsService
    {
        public string RootFolder { get; set; }
        public string PopsFolder => Path.Combine(RootFolder, "POPS");
        public string AppsFolder => Path.Combine(RootFolder, "APPS");
        public string CfgFolder => Path.Combine(RootFolder, "CFG");
        public string ArtFolder => Path.Combine(RootFolder, "ART");

        public string PopstarterElfPath { get; set; }

        private readonly Action<string>? log;
        private readonly SettingsService? settings;

        public PathsService(Action<string>? log = null, SettingsService? settings = null)
        {
            this.log = log;
            this.settings = settings;

            // ============================
            //  RUTA RAÍZ
            // ============================
            RootFolder = settings?.RootFolder ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "POPSManager");

            EnsureFolderStructure();

            // ============================
            //  POPSTARTER.ELF
            // ============================
            if (settings != null && !string.IsNullOrWhiteSpace(settings.CustomElfPath))
            {
                PopstarterElfPath = settings.CustomElfPath;
            }
            else
            {
                PopstarterElfPath = FindPopstarterElf();
            }
        }

        // ============================================================
        //  CREAR ESTRUCTURA OPL AUTOMÁTICAMENTE
        // ============================================================
        private void EnsureFolderStructure()
        {
            Directory.CreateDirectory(RootFolder);
            Directory.CreateDirectory(PopsFolder);
            Directory.CreateDirectory(AppsFolder);
            Directory.CreateDirectory(CfgFolder);
            Directory.CreateDirectory(ArtFolder);
        }

        // ============================================================
        //  BUSCAR POPSTARTER.ELF AUTOMÁTICAMENTE
        // ============================================================
        private string FindPopstarterElf()
        {
            string[] searchPaths =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "POPSTARTER.ELF"),
                Path.Combine(RootFolder, "POPSTARTER.ELF"),
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
        //  CONFIGURAR POPSTARTER.ELF MANUALMENTE
        // ============================================================
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

        // ============================================================
        //  GUARDAR CONFIGURACIÓN
        // ============================================================
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
