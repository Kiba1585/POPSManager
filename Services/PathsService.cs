using System;
using System.IO;
using POPSManager.Services.Interfaces;

namespace POPSManager.Services
{
    /// <summary>
    /// Servicio de rutas del sistema para POPSManager.
    /// Gestiona rutas de OPL, ELF, carpetas personalizadas y estructura de archivos.
    /// Implementa IPathsService para desacoplamiento vía DI.
    /// </summary>
    public class PathsService : IPathsService
    {
        // ============================================================
        //  PROPIEDADES PRINCIPALES
        // ============================================================
        public string RootFolder { get; private set; }

        // Carpetas OPL (respetan rutas personalizadas)
        public string PopsFolder => ResolvePath(_customPopsFolder, "POPS");
        public string AppsFolder => ResolvePath(_customAppsFolder, "APPS");

        // Carpetas dependientes de POPS (no de RootFolder)
        public string CfgFolder => Path.Combine(PopsFolder, "CFG");
        public string ArtFolder => Path.Combine(PopsFolder, "ART");
        public string DvdFolder => Path.Combine(RootFolder, "DVD");

        // ELF base
        public string PopstarterElfPath { get; private set; } = "";
        public string PopstarterPs2ElfPath { get; private set; } = "";

        private string? _customPopsFolder;
        private string? _customAppsFolder;

        private readonly Action<string>? _log;
        private readonly SettingsService? _settings;

        // ============================================================
        //  CONSTRUCTOR
        // ============================================================
        public PathsService(Action<string>? log = null, SettingsService? settings = null)
        {
            _log = log;
            _settings = settings;

            // Normalizar raíz
            RootFolder = NormalizeRoot(settings?.RootFolder);

            // Cargar rutas personalizadas
            _customPopsFolder = settings?.CustomPopsFolder;
            _customAppsFolder = settings?.CustomAppsFolder;

            // Crear estructura
            EnsureFolderStructure();

            // Resolver ELF
            PopstarterElfPath = ResolveElf("POPSTARTER.ELF");
            PopstarterPs2ElfPath = ResolveElf("POPS2.ELF");
        }

        // ============================================================
        //  NORMALIZAR RAÍZ
        // ============================================================
        private string NormalizeRoot(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                string fallback = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "POPSManager");

                _log?.Invoke($"[Paths] Usando raíz por defecto: {fallback}");
                return fallback;
            }

            string full = Path.GetFullPath(path);

            // Evitar seleccionar carpetas internas
            string folderName = Path.GetFileName(full).ToUpperInvariant();
            if (folderName is "PS2" or "POPSMANAGER")
                full = Path.GetDirectoryName(full) ?? full;

            _log?.Invoke($"[Paths] Raíz normalizada: {full}");
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
                _log?.Invoke($"[Paths] ERROR: Ruta inválida → {path}");
                return path;
            }
        }

        // ============================================================
        //  CREAR ESTRUCTURA DE CARPETAS
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
                    _log?.Invoke($"[Paths] Carpeta creada: {path}");
                }
            }
            catch (Exception ex)
            {
                _log?.Invoke($"[Paths] ERROR creando carpeta {path}: {ex.Message}");
            }
        }

        // ============================================================
        //  DETECCIÓN AUTOMÁTICA DE ELF (PS1 + PS2)
        // ============================================================
        private string ResolveElf(string elfName)
        {
            if (_settings != null)
            {
                string? custom = elfName == "POPSTARTER.ELF"
                    ? _settings.CustomElfPath
                    : _settings.CustomPs2ElfPath;

                if (!string.IsNullOrWhiteSpace(custom) && File.Exists(custom))
                {
                    _log?.Invoke($"[Paths] Usando {elfName} personalizado: {custom}");
                    return NormalizePath(custom);
                }
            }

            return FindElf(elfName);
        }

        private string FindElf(string elfName)
        {
            string[] searchPaths =
            {
                Path.Combine(AppContext.BaseDirectory, elfName),
                Path.Combine(RootFolder, elfName),
                Path.Combine(PopsFolder, elfName),
                Path.Combine(AppsFolder, elfName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), elfName)
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    _log?.Invoke($"[Paths] {elfName} encontrado en: {path}");
                    return NormalizePath(path);
                }
            }

            _log?.Invoke($"[Paths] ADVERTENCIA: No se encontró {elfName}.");
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
            _log?.Invoke($"[Paths] Ruta POPS personalizada: {_customPopsFolder}");
        }

        public void SetCustomAppsFolder(string path)
        {
            _customAppsFolder = NormalizePath(path);
            CreateFolder(_customAppsFolder);
            Save();
            _log?.Invoke($"[Paths] Ruta APPS personalizada: {_customAppsFolder}");
        }

        public void SetCustomElfPath(string path)
        {
            if (File.Exists(path))
            {
                PopstarterElfPath = NormalizePath(path);
                Save();
                _log?.Invoke($"[Paths] POPSTARTER.ELF configurado manualmente: {path}");
            }
            else
            {
                _log?.Invoke($"[Paths] ERROR: Archivo no encontrado: {path}");
            }
        }

        public void SetCustomPs2ElfPath(string path)
        {
            if (File.Exists(path))
            {
                PopstarterPs2ElfPath = NormalizePath(path);
                Save();
                _log?.Invoke($"[Paths] POPS2.ELF configurado manualmente: {path}");
            }
            else
            {
                _log?.Invoke($"[Paths] ERROR: Archivo no encontrado: {path}");
            }
        }

        // ============================================================
        //  RECARGAR RUTAS
        // ============================================================
        public void Reload()
        {
            _customPopsFolder = _settings?.CustomPopsFolder;
            _customAppsFolder = _settings?.CustomAppsFolder;

            RootFolder = NormalizeRoot(_settings?.RootFolder);

            EnsureFolderStructure();

            PopstarterElfPath = ResolveElf("POPSTARTER.ELF");
            PopstarterPs2ElfPath = ResolveElf("POPS2.ELF");

            _log?.Invoke("[Paths] Rutas recargadas correctamente.");
        }

        // ============================================================
        //  GUARDAR SETTINGS
        // ============================================================
        public void Save()
        {
            if (_settings == null)
                return;

            _settings.RootFolder = RootFolder;
            _settings.CustomElfPath = PopstarterElfPath;
            _settings.CustomPs2ElfPath = PopstarterPs2ElfPath;
            _settings.CustomPopsFolder = _customPopsFolder;
            _settings.CustomAppsFolder = _customAppsFolder;
            _settings.Save();
        }

        // ============================================================
        //  UTILIDAD: RUTA POPSTARTER (mass:/POPS/...)
        // ============================================================
        public string BuildMassPath(string fullPath)
        {
            string folder = Path.GetFileName(Path.GetDirectoryName(fullPath)) ?? "";
            string file = Path.GetFileName(fullPath) ?? "";
            return $"mass:/POPS/{folder}/{file}";
        }
    }
}
