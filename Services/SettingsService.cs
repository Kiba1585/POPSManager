using System;
using System.IO;
using System.Text.Json;

namespace POPSManager.Services
{
    public class SettingsService
    {
        private readonly string settingsPath;
        private readonly Action<string> log;

        public string CustomElfPath { get; private set; } = "";

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
                    return;

                var json = File.ReadAllText(settingsPath);
                var data = JsonSerializer.Deserialize<SettingsData>(json);

                if (data != null)
                {
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

        public void SetCustomElfPath(string path)
        {
            CustomElfPath = path;
            Save();
        }

        private class SettingsData
        {
            public string? CustomElfPath { get; set; }
        }
    }
}
