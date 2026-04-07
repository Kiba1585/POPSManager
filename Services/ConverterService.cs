using System;
using System.IO;
using POPSManager.Models;

namespace POPSManager.Services
{
    public class ConverterService
    {
        private readonly Action<string> _log;
        private readonly PathsService _paths;
        private readonly SettingsService _settings;
        private readonly Action<UiNotification> _notify;
        private readonly Action<string> _setStatus;

        public ConverterService(
            Action<string> log,
            PathsService paths,
            SettingsService settings,
            Action<UiNotification> notify,
            Action<string> setStatus)
        {
            _log = log;
            _paths = paths;
            _settings = settings;
            _notify = notify;
            _setStatus = setStatus;
        }

        public void ConvertFolder(string sourceFolder, string outputFolder)
        {
            if (!Directory.Exists(sourceFolder) || !Directory.Exists(outputFolder))
            {
                _notify(new UiNotification(NotificationType.Error, "Carpetas inválidas."));
                return;
            }

            var files = Directory.GetFiles(sourceFolder);
            int total = files.Length;
            int processed = 0;

            foreach (var file in files)
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();

                if (ext != ".bin" && ext != ".cue" && ext != ".iso")
                    continue;

                processed++;
                _setStatus($"Convirtiendo {processed}/{total}: {Path.GetFileName(file)}");

                try
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    string dest = Path.Combine(outputFolder, $"{name}.vcd");

                    // Placeholder: copia el archivo como .vcd
                    File.Copy(file, dest, true);

                    _log($"Convertido: {file} → {dest}");
                }
                catch (Exception ex)
                {
                    _log($"ERROR al convertir {file}: {ex.Message}");
                    _notify(new UiNotification(NotificationType.Error, $"Error con {Path.GetFileName(file)}"));
                }
            }

            _notify(new UiNotification(NotificationType.Success, "Conversión completada."));
            _setStatus("Conversión finalizada.");
        }
    }
}
