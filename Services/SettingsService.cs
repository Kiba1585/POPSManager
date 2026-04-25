using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using POPSManager.Services.Interfaces;
using POPSManager.Settings;
using POPSManager.Logic.Automation;

namespace POPSManager.Services
{
    public enum AppLanguage
    {
        Auto = 0,
        Spanish = 1,
        English = 2,
        French = 3,
        German = 4,
        Italian = 5,
        Portuguese = 6,
        Japanese = 7
    }

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

        // NUEVO: usar metadatos (CFG) para OPL
        public bool UseMetadata { get; set; } = true;

        // NUEVO: usar carpeta temporal para conversión en dispositivos extraíbles
        public bool UseTempFolderForConversion { get; set; } = true;

        public string RootFolder { get; set; } = "";
        public string CustomElfPath { get; set; } = "";
        public string CustomPs2ElfPath { get; set; } = "";

        public string? CustomPopsFolder { get; set; }
        public string? CustomAppsFolder { get; set; }

        public string? CustomLngFolder { get; set; }
        public string? CustomThmFolder { get; set; }

        // NUEVO: carpeta temporal personalizada (si está vacía, se usa %TEMP%\POPSManager\)
        public string? TempFolder { get; set; }

        // ============================
        //  NUEVAS PROPIEDADES (RUTAS CENTRALIZADAS)
        // ============================
        public string? SourceFolder { get; set; }
        public string? DestinationFolder { get; set; }
        public string? ElfFolder { get; set; }
        public bool ProcessSubfolders { get; set; } = true;

        // NUEVO: formato del nombre del ELF
        public bool UseTitleInElfName { get; set; } = true;

        // ============================
        //  IDIOMA
        // ============================
        public AppLanguage Language { get; set; } = AppLanguage.Auto;

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
                UseMetadata = data.UseMetadata;
                UseTempFolderForConversion = data.UseTempFolderForConversion;

                RootFolder = data.RootFolder ?? "";
                CustomElfPath = data.CustomElfPath ?? "";
                CustomPs2ElfPath = data.CustomPs2ElfPath ?? "";

                CustomPopsFolder = data.CustomPopsFolder;
                CustomAppsFolder = data.CustomAppsFolder;
                CustomLngFolder = data.CustomLngFolder;
                CustomThmFolder = data.CustomThmFolder;
                TempFolder = data.TempFolder;

                SourceFolder = data.SourceFolder;
                DestinationFolder = data.DestinationFolder;
                ElfFolder = data.ElfFolder;
                ProcessSubfolders = data.ProcessSubfolders;
                UseTitleInElfName = data.UseTitleInElfName;

                Automation = data.Automation ?? new AutomationSettings();

                Language = data.Language;

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
                    UseMetadata = UseMetadata,
                    UseTempFolderForConversion = UseTempFolderForConversion,

                    RootFolder = RootFolder,
                    CustomElfPath = CustomElfPath,
                    CustomPs2ElfPath = CustomPs2ElfPath,
                    CustomPopsFolder = CustomPopsFolder,
                    CustomAppsFolder = CustomAppsFolder,
                    CustomLngFolder = CustomLngFolder,
                    CustomThmFolder = CustomThmFolder,
                    TempFolder = TempFolder,

                    SourceFolder = SourceFolder,
                    DestinationFolder = DestinationFolder,
                    ElfFolder = ElfFolder,
                    ProcessSubfolders = ProcessSubfolders,
                    UseTitleInElfName = UseTitleInElfName,

                    Automation = Automation,
                    Language = Language
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

            if (!string.IsNullOrWhiteSpace(CustomLngFolder))
                CustomLngFolder = Normalize(CustomLngFolder);

            if (!string.IsNullOrWhiteSpace(CustomThmFolder))
                CustomThmFolder = Normalize(CustomThmFolder);

            if (!string.IsNullOrWhiteSpace(TempFolder))
                TempFolder = Normalize(TempFolder);

            if (!string.IsNullOrWhiteSpace(SourceFolder))
                SourceFolder = Normalize(SourceFolder);

            if (!string.IsNullOrWhiteSpace(DestinationFolder))
                DestinationFolder = Normalize(DestinationFolder);

            if (!string.IsNullOrWhiteSpace(ElfFolder))
                ElfFolder = Normalize(ElfFolder);
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

        // ============================================================
        // MÉTODOS ASINCRÓNICOS Y DE CONVENIENCIA
        // ============================================================
        public async Task SaveAsync()
        {
            await Task.Run(() => Save());
        }

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

        // ============================================================
        // CLASE INTERNA PARA SERIALIZACIÓN
        // ============================================================
        private sealed class SettingsData
        {
            public bool DarkMode { get; set; }
            public bool NotificationsEnabled { get; set; }

            public bool UseDatabase { get; set; } = true;
            public bool UseCovers { get; set; } = true;
            public bool UseMetadata { get; set; } = true;
            public bool UseTempFolderForConversion { get; set; } = true;

            public string? RootFolder { get; set; }
            public string? CustomElfPath { get; set; }
            public string? CustomPs2ElfPath { get; set; }
            public string? CustomPopsFolder { get; set; }
            public string? CustomAppsFolder { get; set; }

            public string? CustomLngFolder { get; set; }
            public string? CustomThmFolder { get; set; }
            public string? TempFolder { get; set; }

            public string? SourceFolder { get; set; }
            public string? DestinationFolder { get; set; }
            public string? ElfFolder { get; set; }
            public bool ProcessSubfolders { get; set; } = true;
            public bool UseTitleInElfName { get; set; } = true;

            public AutomationSettings? Automation { get; set; } = new();

            public AppLanguage Language { get; set; } = AppLanguage.Auto;
        }
    }
}