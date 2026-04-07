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
        //  PROPIEDADES CONFIGURABLES
        // ============================

        public bool DarkMode { get; set; } = false;
        public bool NotificationsEnabled { get; set; } = true;

        // NUEVA ARQUITECTURA
        public string RootFolder { get; set; } = "";
        public string CustomElfPath { get; set; } = "";

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

        // ============================
        //  CARGAR SETTINGS
        // ============================

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

        // ============================
        //  GUARDAR SETTINGS
        // ============================

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
            }
            catch (Exception ex)
            {
                log($"ERROR guardando settings: {ex.Message}");
            }
        }

        // ============================
        //  SETTERS ESPECÍFICOS
        // ============================

        public void SetRootFolder(string path)
        {
            RootFolder = path;
            Save();
        }

        public void SetCustomElfPath(string path)
        {
            CustomElfPath = path;
            Save();
        }

        // ============================
        //  CLASE INTERNA PARA JSON
        // ============================

        private class SettingsData
        {
            public bool DarkMode { get; set; }
            public bool NotificationsEnabled { get; set; }

            public string? RootFolder { get; set; }
            public string? CustomElfPath { get; set; }
        }
    }
}
