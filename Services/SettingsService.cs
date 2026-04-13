using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using POPSManager.Services.Interfaces;
using POPSManager.Settings;
using POPSManager.Logic.Automation;

namespace POPSManager.Services
{
    /// <summary>
    /// Servicio centralizado de configuración.
    /// Seguro, robusto, validado y optimizado para .NET 8.
    /// </summary>
    public sealed class SettingsService
    {
        private readonly string _settingsPath;
        private readonly Action<string> _log;
        private readonly INotificationService _notifications;

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
        public AutomationSettings Automation { get; set; } = new();

        public event Action? OnSettingsChanged;

        public SettingsService(Action<string> log, INotificationService notifications)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "POPSManager");

            Directory.CreateDirectory(folder);

            _settingsPath = Path.Combine(folder, "settings.json");

            Load();
            NormalizeValues();
            EnsureDefaults();
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    _log("[Settings] No existe settings.json, usando valores por defecto.");
                    _notifications.Info("Configuración inicial creada");
                    return;
                }

                var json = File.ReadAllText(_settingsPath);
                var data = JsonSerializer.Deserialize<SettingsData>(json, JsonOptions);

                if (data == null)
                {
                    _log("[Settings] Archivo corrupto → regenerando settings.");
                    _notifications.Warning("Archivo de configuración corrupto. Se regenerará.");
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

                Automation = data.Automation ?? new AutomationSettings();

                _log("[Settings] Settings cargados correctamente.");
            }
            catch (Exception ex)
            {
                _log($"[Settings] ERROR cargando settings: {ex.Message}");
                _notifications.Error("Error cargando configuración");
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

                    Automation = Automation
                };

                var json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(_settingsPath, json);

                _log("[Settings] Settings guardados correctamente.");
                _notifications.Success("Configuración guardada");
                OnSettingsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                _log($"[Settings] ERROR guardando settings: {ex.Message}");
                _notifications.Error("Error guardando configuración");
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
                _log($"[Settings] Ruta inválida: {path}");
                _notifications.Warning($"Ruta inválida: {path}");
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

                _log("[Settings] RootFolder vacío → asignado valor por defecto.");
                _notifications.Info("Se asignó carpeta raíz por defecto");
            }
        }

        public void SetRootFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                _log($"[Settings] ERROR: Carpeta inválida: {path}");
                _notifications.Error("Carpeta inválida");
                return;
            }

            RootFolder = Normalize(path);
            Save();
        }

        public void SetCustomElfPath(string path)
        {
            if (!File.Exists(path))
            {
                _log($"[Settings] ERROR: Archivo no encontrado: {path}");
                _notifications.Error("Archivo ELF no encontrado");
                return;
            }

            CustomElfPath = Normalize(path);
            Save();
        }

        public void SetCustomPs2ElfPath(string path)
        {
            if (!File.Exists(path))
            {
                _log($"[Settings] ERROR: Archivo no encontrado: {path}");
                _notifications.Error("Archivo PS2 ELF no encontrado");
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

            public AutomationSettings? Automation { get; set; } = new();
        }
    }
}
