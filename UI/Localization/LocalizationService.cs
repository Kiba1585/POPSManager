using System;
using System.Collections.Generic;
using System.Globalization;
using POPSManager.Services;

namespace POPSManager.UI.Localization
{
    public static class LocalizationService
    {
        private static SettingsService Settings => App.Services!.Settings;

        // ============================================================
        //  DICCIONARIO PARA TEXTOS DINÁMICOS (progreso, subtareas)
        // ============================================================
        private static readonly Dictionary<string, (string es, string en)> DynamicTexts =
            new()
            {
                { "Preparing", ("Preparando…", "Preparing…") },
                { "CopyingDisc", ("Copiando CD{0}…", "Copying Disc {0}…") },
                { "DownloadingCover", ("Descargando cover…", "Downloading cover…") },
                { "GeneratingELF", ("Generando ELF…", "Generating ELF…") },
                { "GeneratingCheats", ("Generando cheats…", "Generating cheats…") },
                { "GeneratingDiscsTxt", ("Generando DISCS.TXT…", "Generating DISCS.TXT…") },
                { "CopyingISO", ("Copiando ISO…", "Copying ISO…") },
                { "Completed", ("Completado", "Completed") },
                { "Error", ("Error", "Error") }
            };

        // ============================================================
        //  OBTENER IDIOMA ACTUAL
        // ============================================================
        private static string CurrentLang
        {
            get
            {
                return Settings.Language switch
                {
                    AppLanguage.Spanish => "es",
                    AppLanguage.English => "en",
                    AppLanguage.Auto => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
                    _ => "en"
                };
            }
        }

        // ============================================================
        //  TRADUCIR TEXTO DINÁMICO
        // ============================================================
        public static string T(string key, params object[] args)
        {
            if (!DynamicTexts.TryGetValue(key, out var pair))
                return key;

            string raw = CurrentLang == "es" ? pair.es : pair.en;

            return args.Length > 0 ? string.Format(raw, args) : raw;
        }
    }
}
