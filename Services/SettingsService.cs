using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using POPSManager.Services.Interfaces;

namespace POPSManager.Services
{
    public enum AutomationMode
    {
        Automatic,
        Intelligent,
        Manual
    }

    public enum AutomationBehavior
    {
        Auto,
        Ask,
        Manual
    }

    /// <summary>
    /// Servicio centralizado de configuración.
    /// Seguro, robusto, validado y optimizado para .NET 8.
    /// </summary>
    public sealed class SettingsService
    {
        private readonly string settingsPath;
        private readonly Action<string> log;
        private readonly INotificationService notifications;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // ============================
        //  PROPIEDADES DEL USUARIO
        // ============================
        public bool DarkMode { get; set; }
        public bool NotificationsEnabled { get; set; } = true;

        public bool UseDatabase { get; set; } = true;
        public bool UseCovers { get; set; } = true;

        public string RootFolder { get; set; } = "";
        public string CustomElfPath { get; set; } = "";
        public string CustomPs2ElfPath { get; set; } = "";

        public string? CustomPopsFolder { get; set; }
        public string? CustomAppsFolder { get; set; }

        // ============================
        //  AUTOMATIZACIÓN INTELIGENTE
        // ============================
        public AutomationMode GlobalAutomationMode { get; set; } = AutomationMode.Intelligent;

        public AutomationBehavior NormalizeNamesBehavior { get; set; } = AutomationBehavior.Auto;
        public AutomationBehavior GroupMultiDiscBehavior { get; set; } = AutomationBehavior.Auto;
        public AutomationBehavior DownloadCoversBehavior { get; set; } = AutomationBehavior.Ask;

        public event Action? OnSettingsChanged;

        public SettingsService(Action<string> log, INotificationService notifications)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "POPSManager");

            Directory.CreateDirectory(folder);

            settingsPath = Path.Combine(folder, "settings.json");

            Load();
            NormalizeValues();
            EnsureDefaults();
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(settingsPath))
                {
                    log("[Settings] No existe settings.json, usando valores por defecto.");
                    notifications.Info("Configuración inicial creada");
                    return;
                }

                var json = File.ReadAllText(settingsPath);
                var data = JsonSerializer.Deserialize<SettingsData>(json, JsonOptions);

                if (data == null)
                {
                    log("[Settings] Archivo corrupto → regenerando settings.");
                    notifications.Warning("Archivo de configuración corrupto. Se regenerará.");
                    Save();
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

                GlobalAutomationMode = data.GlobalAutomationMode;
                NormalizeNamesBehavior = data.NormalizeNamesBehavior;
                GroupMultiDiscBehavior = data.GroupMultiDiscBehavior;
                DownloadCoversBehavior = data.DownloadCoversBehavior;

                log("[Settings] Settings cargados correctamente.");
            }
            catch (Exception ex)
            {
                log($"[Settings] ERROR cargando settings: {ex.Message}");
                notifications.Error("Error cargando configuración");
            }
        }

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
                    CustomAppsFolder = CustomAppsFolder,

                    GlobalAutomationMode = GlobalAutomationMode,
                    NormalizeNamesBehavior = NormalizeNamesBehavior,
                    GroupMultiDiscBehavior = GroupMultiDiscBehavior,
                    DownloadCoversBehavior = DownloadCoversBehavior
                };

                var json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(settingsPath, json);

                log("[Settings] Settings guardados correctamente.");
                notifications.Success("Configuración guardada");
                OnSettingsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                log($"[Settings] ERROR guardando settings: {ex.Message}");
                notifications.Error("Error guardando configuración");
            }
        }

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
                notifications.Warning($"Ruta inválida: {path}");
                return path;
            }
        }

        private void EnsureDefaults()
        {
            if (string.IsNullOrWhiteSpace(RootFolder))
            {
                RootFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "POPSManager"
                );

                log("[Settings] RootFolder vacío → asignado valor por defecto.");
                notifications.Info("Se asignó carpeta raíz por defecto");
            }
        }

        public void SetRootFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                log($"[Settings] ERROR: Carpeta inválida: {path}");
                notifications.Error("Carpeta inválida");
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
                notifications.Error("Archivo ELF no encontrado");
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
                notifications.Error("Archivo PS2 ELF no encontrado");
                return;
            }

            CustomPs2ElfPath = Normalize(path);
            Save();
        }

        private sealed class SettingsData
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

            public AutomationMode GlobalAutomationMode { get; set; } = AutomationMode.Intelligent;
            public AutomationBehavior NormalizeNamesBehavior { get; set; } = AutomationBehavior.Auto;
            public AutomationBehavior GroupMultiDiscBehavior { get; set; } = AutomationBehavior.Auto;
            public AutomationBehavior DownloadCoversBehavior { get; set; } = AutomationBehavior.Ask;
        }
    }
}
