using System;
using System.IO;
using System.Text.Json;

namespace POPSManager.Settings
{
    /// <summary>
    /// Servicio para cargar y guardar la configuración de cheats en formato JSON.
    /// </summary>
    public class CheatSettingsService
    {
        private readonly string _settingsPath;
        private readonly Action<string>? _log;

        public CheatSettings Current { get; private set; } = new();

        /// <summary>
        /// Inicializa el servicio de configuración de cheats.
        /// </summary>
        /// <param name="rootFolder">Carpeta raíz donde se creará la subcarpeta "Settings".</param>
        /// <param name="log">Acción opcional para registrar mensajes.</param>
        public CheatSettingsService(string rootFolder, Action<string>? log = null)
        {
            _log = log;

            if (string.IsNullOrWhiteSpace(rootFolder))
                throw new ArgumentException("La carpeta raíz no puede ser nula o vacía.", nameof(rootFolder));

            string settingsFolder = Path.Combine(rootFolder, "Settings");
            Directory.CreateDirectory(settingsFolder);

            _settingsPath = Path.Combine(settingsFolder, "CheatSettings.json");

            Load();
        }

        /// <summary>
        /// Carga la configuración desde el archivo JSON.
        /// </summary>
        private void Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    string json = File.ReadAllText(_settingsPath);
                    var loaded = JsonSerializer.Deserialize<CheatSettings>(json);
                    if (loaded != null)
                        Current = loaded;
                }
                else
                {
                    _log?.Invoke("[Cheats] No se encontró CheatSettings.json. Se usarán valores por defecto.");
                }
            }
            catch (Exception ex)
            {
                _log?.Invoke($"[Cheats] Error cargando CheatSettings: {ex.Message}");
                // Se mantiene la instancia por defecto.
            }
        }

        /// <summary>
        /// Guarda la configuración actual en el archivo JSON.
        /// </summary>
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Current, options);
                File.WriteAllText(_settingsPath, json);

                _log?.Invoke("[Cheats] CheatSettings guardado correctamente.");
            }
            catch (Exception ex)
            {
                _log?.Invoke($"[Cheats] Error guardando CheatSettings: {ex.Message}");
            }
        }
    }
}