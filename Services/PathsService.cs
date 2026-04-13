using System;
using System.IO;
using System.Threading.Tasks;
using POPSManager.Services.Interfaces;
using POPSManager.Settings;
using POPSManager.Logic.Automation;

namespace POPSManager.Services
{
    public class PathsService : IPathsService
    {
        public string RootFolder { get; private set; }

        public string PopsFolder => ResolvePath(_customPopsFolder, "POPS");
        public string AppsFolder => ResolvePath(_customAppsFolder, "APPS");

        public string CfgFolder => Path.Combine(PopsFolder, "CFG");
        public string ArtFolder => Path.Combine(PopsFolder, "ART");
        public string DvdFolder => Path.Combine(RootFolder, "DVD");

        public string PopstarterElfPath { get; private set; } = "";
        public string PopstarterPs2ElfPath { get; private set; } = "";

        private string? _customPopsFolder;
        private string? _customAppsFolder;

        private readonly Action<string>? _log;
        private readonly SettingsService _settings;
        private readonly AutomationEngine _auto;

        public PathsService(Action<string>? log, SettingsService settings, AutomationEngine auto)
        {
            _log = log;
            _settings = settings;
            _auto = auto;

            RootFolder = NormalizeRoot(settings.RootFolder);

            _customPopsFolder = settings.CustomPopsFolder;
            _customAppsFolder = settings.CustomAppsFolder;

            // Estructura inicial (sync, pero rápida)
            EnsureFolderStructure();

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

            string folderName = Path.GetFileName(full).ToUpperInvariant();
            if (folderName is "PS2" or "POPSMANAGER")
                full = Path.GetDirectoryName(full) ?? full;

            _log?.Invoke($"[Paths] Raíz normalizada: {full}");
            return full;
        }

        // ============================================================
        //  RESOLVER RUTAS
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
        //  CREAR ESTRUCTURA (SYNC)
        // ============================================================
        private void EnsureFolderStructure()
        {
            if (!_auto.ShouldCreateFolders())
            {
                _log?.Invoke("[Paths] Automatización: creación de carpetas desactivada.");
                return;
            }

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
        //  DETECCIÓN ELF (SYNC)
        // ============================================================
        private string ResolveElf(string elfName)
        {
            string? custom = elfName == "POPSTARTER.ELF"
                ? _settings.CustomElfPath
                : _settings.CustomPs2ElfPath;

            if (!string.IsNullOrWhiteSpace(custom) && File.Exists(custom))
            {
                _log?.Invoke($"[Paths] Usando {elfName} personalizado: {custom}");
                return NormalizePath(custom);
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
        //  MÉTODOS ASYNC PARA SETTINGSVIEW
        // ============================================================
        public async Task SetCustomPopsFolderAsync(string path)
        {
            _customPopsFolder = NormalizePath(path);
            CreateFolder(_customPopsFolder);
            await SaveAsync();
            _log?.Invoke($"[Paths] Ruta POPS personalizada: {_customPopsFolder}");
        }

        public async Task SetCustomAppsFolderAsync(string path)
        {
            _customAppsFolder = NormalizePath(path);
            CreateFolder(_customAppsFolder);
            await SaveAsync();
            _log?.Invoke($"[Paths] Ruta APPS personalizada: {_customAppsFolder}");
        }

        public async Task SetCustomElfPathAsync(string path)
        {
            if (File.Exists(path))
            {
                PopstarterElfPath = NormalizePath(path);
                await SaveAsync();
                _log?.Invoke($"[Paths] POPSTARTER.ELF configurado manualmente: {path}");
            }
            else
            {
                _log?.Invoke($"[Paths] ERROR: Archivo no encontrado: {path}");
            }
        }

        public async Task SetCustomPs2ElfPathAsync(string path)
        {
            if (File.Exists(path))
            {
                PopstarterPs2ElfPath = NormalizePath(path);
                await SaveAsync();
                _log?.Invoke($"[Paths] POPS2.ELF configurado manualmente: {path}");
            }
            else
            {
                _log?.Invoke($"[Paths] ERROR: Archivo no encontrado: {path}");
            }
        }

        // ============================================================
        //  RECARGAR RUTAS (ASYNC)
        // ============================================================
        public async Task ReloadAsync()
        {
            _customPopsFolder = _settings.CustomPopsFolder;
            _customAppsFolder = _settings.CustomAppsFolder;

            RootFolder = NormalizeRoot(_settings.RootFolder);

            EnsureFolderStructure();

            PopstarterElfPath = ResolveElf("POPSTARTER.ELF");
            PopstarterPs2ElfPath = ResolveElf("POPS2.ELF");

            await SaveAsync();

            _log?.Invoke("[Paths] Rutas recargadas correctamente.");
        }

        // ============================================================
        //  GUARDAR SETTINGS (ASYNC)
        // ============================================================
        public async Task SaveAsync()
        {
            _settings.RootFolder = RootFolder;
            _settings.CustomElfPath = PopstarterElfPath;
            _settings.CustomPs2ElfPath = PopstarterPs2ElfPath;
            _settings.CustomPopsFolder = _customPopsFolder;
            _settings.CustomAppsFolder = _customAppsFolder;

            await _settings.SaveAsync();
        }

        // Wrapper síncrono (compatibilidad)
        public void Save() => SaveAsync().GetAwaiter().GetResult();

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
