using System;
using System.IO;
using System.Text.Json;

namespace POPSManager.Services
{
    public class SettingsService
    {
        private readonly string settingsPath;
        private readonly Action<string> log;

        // ============================
        //  PROPIEDADES DEL USUARIO
        // ============================
        public bool DarkMode { get; set; } = false;
        public bool NotificationsEnabled { get; set; } = true;

        public bool UseDatabase { get; set; } = true;
        public bool UseCovers { get; set; } = true;

        public string RootFolder { get; set; } = "";
        public string CustomElfPath { get; set; } = "";
        public string CustomPs2ElfPath { get; set; } = "";

        public string? CustomPopsFolder { get; set; }
        public string? CustomAppsFolder { get; set; }

        // Evento para notificar cambios globales
        public event Action? OnSettingsChanged;

        public SettingsService(Action<string> log)
        {
            this.log = log;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "POPSManager");

            Directory.CreateDirectory(folder);

            settingsPath = Path.Combine(folder, "settings.json");

            Load();
            NormalizeValues();
            EnsureDefaults();
        }

        // ============================================================
        //  CARGAR SETTINGS
        // ============================================================
        public void Load()
        {
            try
            {
                if (!File.Exists(settingsPath))
                {
                    log("[Settings] No existe settings.json, usando valores por defecto.");
                    return;
                }

                var json = File.ReadAllText(settingsPath);
                var data = JsonSerializer.Deserialize<SettingsData>(json);

                if (data == null)
                {
                    log("[Settings] Archivo corrupto, regenerando settings.");
                    Save(); // Regenera archivo limpio
                    return;
                }

                DarkMode = data.DarkMode;
                NotificationsEnabled = data.NotificationsEnabled;

                UseDatabase = data.UseDatabase;
                UseCovers = data.UseCovers;

                RootFolder = data.RootFolder ?? "";
                CustomElfPath = data.CustomElfPath ?? "";
                CustomPs2ElfPath = data.CustomPs2ElfPath ?? "";

                CustomPopsFolder = data.CustomPopsFolder;
                CustomAppsFolder = data.CustomAppsFolder;

                log("[Settings] Settings cargados correctamente.");
            }
            catch (Exception ex)
            {
                log($"[Settings] ERROR cargando settings: {ex.Message}");
            }
        }

        // ============================================================
        //  GUARDAR SETTINGS
        // ============================================================
        public void Save()
        {
            try
            {
                NormalizeValues();

                var data = new SettingsData
                {
                    DarkMode = DarkMode,
                    NotificationsEnabled = NotificationsEnabled,

                    UseDatabase = UseDatabase,
                    UseCovers = UseCovers,

                    RootFolder = RootFolder,
                    CustomElfPath = CustomElfPath,
                    CustomPs2ElfPath = CustomPs2ElfPath,
                    CustomPopsFolder = CustomPopsFolder,
                    CustomAppsFolder = CustomAppsFolder
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);

                log("[Settings] Settings guardados correctamente.");
                OnSettingsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                log($"[Settings] ERROR guardando settings: {ex.Message}");
            }
        }

        // ============================================================
        //  NORMALIZAR VALORES
        // ============================================================
        private void NormalizeValues()
        {
            RootFolder = Normalize(RootFolder);
            CustomElfPath = Normalize(CustomElfPath);
            CustomPs2ElfPath = Normalize(CustomPs2ElfPath);

            if (!string.IsNullOrWhiteSpace(CustomPopsFolder))
                CustomPopsFolder = Normalize(CustomPopsFolder);

            if (!string.IsNullOrWhiteSpace(CustomAppsFolder))
                CustomAppsFolder = Normalize(CustomAppsFolder);
        }

        private string Normalize(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "";

            try
            {
                return Path.GetFullPath(path.Trim().TrimEnd('\\', '/'));
            }
            catch
            {
                log($"[Settings] Ruta inválida: {path}");
                return path;
            }
        }

        // ============================================================
        //  ASEGURAR VALORES POR DEFECTO
        // ============================================================
        private void EnsureDefaults()
        {
            if (string.IsNullOrWhiteSpace(RootFolder))
            {
                RootFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "POPSManager"
                );

                log("[Settings] RootFolder vacío → asignado valor por defecto.");
            }
        }

        // ============================================================
        //  SETTERS SEGUROS
        // ============================================================
        public void SetRootFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                log($"[Settings] ERROR: Carpeta inválida: {path}");
                return;
            }

            RootFolder = Normalize(path);
            Save();
        }

        public void SetCustomElfPath(string path)
        {
            if (!File.Exists(path))
            {
                log($"[Settings] ERROR: Archivo no encontrado: {path}");
                return;
            }

            CustomElfPath = Normalize(path);
            Save();
        }

        public void SetCustomPs2ElfPath(string path)
        {
            if (!File.Exists(path))
            {
                log($"[Settings] ERROR: Archivo no encontrado: {path}");
                return;
            }

            CustomPs2ElfPath = Normalize(path);
            Save();
        }

        // ============================================================
        //  CLASE INTERNA PARA JSON
        // ============================================================
        private class SettingsData
        {
            public bool DarkMode { get; set; }
            public bool NotificationsEnabled { get; set; }

            public bool UseDatabase { get; set; } = true;
            public bool UseCovers { get; set; } = true;

            public string? RootFolder { get; set; }
            public string? CustomElfPath { get; set; }
            public string? CustomPs2ElfPath { get; set; }
            public string? CustomPopsFolder { get; set; }
            public string? CustomAppsFolder { get; set; }
        }
    }
}
