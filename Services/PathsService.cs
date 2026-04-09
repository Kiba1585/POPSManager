using System;
using System.IO;

namespace POPSManager.Services
{
    public class PathsService
    {
        public string RootFolder { get; private set; }

        // Carpetas raíz (OPL estándar)
        public string PopsFolder => ResolvePath(_customPopsFolder, "POPS");
        public string AppsFolder => ResolvePath(_customAppsFolder, "APPS");
        public string CfgFolder  => ResolvePath(null, "CFG");
        public string ArtFolder  => ResolvePath(null, "ART");
        public string DvdFolder  => ResolvePath(null, "DVD");

        public string PopstarterElfPath { get; private set; } = "";
        public string PopstarterPs2ElfPath { get; private set; } = "";

        private string? _customPopsFolder;
        private string? _customAppsFolder;

        private readonly Action<string>? log;
        private readonly SettingsService? settings;

        public PathsService(Action<string>? log = null, SettingsService? settings = null)
        {
            this.log = log;
            this.settings = settings;

            // Normalizar raíz
            RootFolder = NormalizeRoot(settings?.RootFolder);

            // Cargar rutas personalizadas
            _customPopsFolder = settings?.CustomPopsFolder;
            _customAppsFolder = settings?.CustomAppsFolder;

            EnsureFolderStructure();

            // Resolver ELF PS1 y PS2
            PopstarterElfPath = ResolveElf("POPSTARTER.ELF");
            PopstarterPs2ElfPath = ResolveElf("POPS2.ELF");
        }

        // ============================================================
        //  NORMALIZAR RAÍZ (sin subcarpetas PS2/POPSManager)
        // ============================================================
        private string NormalizeRoot(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "POPSManager");

            string full = Path.GetFullPath(path);

            // Si termina en PS2 o POPSManager, usar su carpeta padre
            string folderName = Path.GetFileName(full).ToUpperInvariant();
            if (folderName == "PS2" || folderName == "POPSMANAGER")
                full = Path.GetDirectoryName(full) ?? full;

            log?.Invoke($"[Paths] Raíz normalizada: {full}");
            return full;
        }

        // ============================================================
        //  RESOLVER RUTAS PERSONALIZADAS O POR DEFECTO
        // ============================================================
        private string ResolvePath(string? custom, string defaultFolder)
        {
            if (!string.IsNullOrWhiteSpace(custom))
                return NormalizePath(custom);

            return Path.Combine(RootFolder, defaultFolder);
        }

        private string NormalizePath(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                log?.Invoke($"[Paths] ERROR: Ruta inválida → {path}");
                return path;
            }
        }

        // ============================================================
        //  CREACIÓN DE CARPETAS
        // ============================================================
        private void EnsureFolderStructure()
        {
            CreateFolder(PopsFolder);
            CreateFolder(AppsFolder);
            CreateFolder(CfgFolder);
            CreateFolder(ArtFolder);
            CreateFolder(DvdFolder);
        }

        private void CreateFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    log?.Invoke($"[Paths] Carpeta creada: {path}");
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"[Paths] ERROR creando carpeta {path}: {ex.Message}");
            }
        }

        // ============================================================
        //  DETECCIÓN AUTOMÁTICA DE ELF (PS1 + PS2)
        // ============================================================
        private string ResolveElf(string elfName)
        {
            if (settings != null)
            {
                string? custom = elfName == "POPSTARTER.ELF"
                    ? settings.CustomElfPath
                    : settings.CustomPs2ElfPath;

                if (!string.IsNullOrWhiteSpace(custom) && File.Exists(custom))
                {
                    log?.Invoke($"[Paths] Usando {elfName} personalizado: {custom}");
                    return NormalizePath(custom);
                }
            }

            return FindElf(elfName);
        }

        private string FindElf(string elfName)
        {
            string[] searchPaths =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, elfName),
                Path.Combine(RootFolder, elfName),
                Path.Combine(PopsFolder, elfName),
                Path.Combine(AppsFolder, elfName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), elfName)
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    log?.Invoke($"[Paths] {elfName} encontrado en: {path}");
                    return NormalizePath(path);
                }
            }

            log?.Invoke($"[Paths] ADVERTENCIA: No se encontró {elfName}.");
            return "";
        }

        // ============================================================
        //  MÉTODOS PARA SETTINGSVIEW
        // ============================================================
        public void SetCustomPopsFolder(string path)
        {
            _customPopsFolder = NormalizePath(path);
            CreateFolder(_customPopsFolder);
            Save();
            log?.Invoke($"[Paths] Ruta POPS personalizada: {_customPopsFolder}");
        }

        public void SetCustomAppsFolder(string path)
        {
            _customAppsFolder = NormalizePath(path);
            CreateFolder(_customAppsFolder);
            Save();
            log?.Invoke($"[Paths] Ruta APPS personalizada: {_customAppsFolder}");
        }

        public void SetCustomElfPath(string path)
        {
            if (File.Exists(path))
            {
                PopstarterElfPath = NormalizePath(path);
                Save();
                log?.Invoke($"[Paths] POPSTARTER.ELF configurado manualmente: {path}");
            }
            else
            {
                log?.Invoke($"[Paths] ERROR: Archivo no encontrado: {path}");
            }
        }

        public void SetCustomPs2ElfPath(string path)
        {
            if (File.Exists(path))
            {
                PopstarterPs2ElfPath = NormalizePath(path);
                Save();
                log?.Invoke($"[Paths] POPS2.ELF configurado manualmente: {path}");
            }
            else
            {
                log?.Invoke($"[Paths] ERROR: Archivo no encontrado: {path}");
            }
        }

        // ============================================================
        //  RECARGAR RUTAS (para SettingsView)
        // ============================================================
        public void Reload()
        {
            // Releer settings
            _customPopsFolder = settings?.CustomPopsFolder;
            _customAppsFolder = settings?.CustomAppsFolder;

            // Recalcular raíz
            RootFolder = NormalizeRoot(settings?.RootFolder);

            // Recrear estructura
            EnsureFolderStructure();

            // Re-resolver ELF
            PopstarterElfPath = ResolveElf("POPSTARTER.ELF");
            PopstarterPs2ElfPath = ResolveElf("POPS2.ELF");

            log?.Invoke("[Paths] Rutas recargadas correctamente.");
        }

        // ============================================================
        //  GUARDAR SETTINGS
        // ============================================================
        public void Save()
        {
            if (settings == null)
                return;

            settings.RootFolder = RootFolder;
            settings.CustomElfPath = PopstarterElfPath;
            settings.CustomPs2ElfPath = PopstarterPs2ElfPath;
            settings.CustomPopsFolder = _customPopsFolder;
            settings.CustomAppsFolder = _customAppsFolder;
            settings.Save();
        }
    }
}
