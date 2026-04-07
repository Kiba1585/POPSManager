using System;
using System.IO;
using System.Text.Json;

namespace POPSManager.Services
{
    public class SettingsService
    {
        private readonly string settingsPath;
        private readonly Action<string> log;

        public bool DarkMode { get; set; } = false;
        public bool NotificationsEnabled { get; set; } = true;

        public string RootFolder { get; set; } = "";
        public string CustomElfPath { get; set; } = "";

        public event Action? OnSettingsChanged;

        public SettingsService(Action<string> log)
        {
            this.log = log;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "POPSManager");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            settingsPath = Path.Combine(folder, "settings.json");

            Load();
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(settingsPath))
                {
                    log("No existe settings.json, usando valores por defecto.");
                    return;
                }

                var json = File.ReadAllText(settingsPath);
                var data = JsonSerializer.Deserialize<SettingsData>(json);

                if (data != null)
                {
                    DarkMode = data.DarkMode;
                    NotificationsEnabled = data.NotificationsEnabled;
                    RootFolder = data.RootFolder ?? "";
                    CustomElfPath = data.CustomElfPath ?? "";
                }

                log("Settings cargados correctamente.");
            }
            catch (Exception ex)
            {
                log($"ERROR cargando settings: {ex.Message}");
            }
        }

        public void Save()
        {
            try
            {
                var data = new SettingsData
                {
                    DarkMode = DarkMode,
                    NotificationsEnabled = NotificationsEnabled,
                    RootFolder = RootFolder,
                    CustomElfPath = CustomElfPath
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);

                log("Settings guardados correctamente.");
                OnSettingsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                log($"ERROR guardando settings: {ex.Message}");
            }
        }

        public void SetRootFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                log($"ERROR: Carpeta inválida: {path}");
                return;
            }

            RootFolder = path;
            Save();
        }

        public void SetCustomElfPath(string path)
        {
            if (!File.Exists(path))
            {
                log($"ERROR: Archivo no encontrado: {path}");
                return;
            }

            CustomElfPath = path;
            Save();
        }

        private class SettingsData
        {
            public bool DarkMode { get; set; }
            public bool NotificationsEnabled { get; set; }
            public string? RootFolder { get; set; }
            public string? CustomElfPath { get; set; }
        }
    }
}
