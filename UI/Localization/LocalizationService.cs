using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using POPSManager.Services;

namespace POPSManager.UI.Localization
{
    /// <summary>
    /// Servicio de localización que carga cadenas desde archivos .resx y notifica cambios de idioma.
    /// </summary>
    public class LocalizationService : INotifyPropertyChanged
    {
        private readonly SettingsService _settings;
        private ResourceManager _resourceManager;

        public event PropertyChangedEventHandler? PropertyChanged;

        public LocalizationService(SettingsService settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _resourceManager = new ResourceManager("POPSManager.UI.Localization.Strings", GetType().Assembly);
        }

        /// <summary>
        /// Idioma actual (código ISO de dos letras).
        /// </summary>
        public string CurrentLanguage
        {
            get => _settings.Language switch
            {
                AppLanguage.Spanish => "es",
                AppLanguage.English => "en",
                AppLanguage.Auto => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
                _ => "en"
            };
        }

        /// <summary>
        /// Obtiene una cadena localizada por su clave.
        /// </summary>
        public string GetString(string key)
        {
            try
            {
                var culture = new CultureInfo(CurrentLanguage);
                return _resourceManager.GetString(key, culture) ?? key;
            }
            catch
            {
                return key;
            }
        }

        /// <summary>
        /// Obtiene una cadena localizada con formato.
        /// </summary>
        public string GetString(string key, params object[] args)
        {
            string format = GetString(key);
            return args.Length > 0 ? string.Format(format, args) : format;
        }

        /// <summary>
        /// Fuerza la recarga del ResourceManager y notifica cambio de idioma.
        /// </summary>
        public void Refresh()
        {
            _resourceManager = new ResourceManager("POPSManager.UI.Localization.Strings", GetType().Assembly);
            OnPropertyChanged(nameof(CurrentLanguage));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}