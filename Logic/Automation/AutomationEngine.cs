using System;
using POPSManager.Logic.Automation;
using POPSManager.Settings;
using POPSManager.Services.Interfaces;

namespace POPSManager.Logic
{
    /// <summary>
    /// Motor de decisión de automatización.
    /// Centraliza la lógica de: auto / preguntar / manual.
    /// </summary>
    public sealed class AutomationEngine
    {
        private readonly AutomationSettings _auto;
        private readonly INotificationService _notifications;

        /// <summary>
        /// Callback opcional para preguntar al usuario (UI).
        /// Devuelve true = sí, false = no, null = cancelado.
        /// </summary>
        public Func<string, bool?>? AskUser { get; set; }

        public AutomationEngine(AutomationSettings auto, INotificationService notifications)
        {
            _auto = auto ?? throw new ArgumentNullException(nameof(auto));
            _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        }

        // ============================================================
        //  API PÚBLICA: DECISIONES POR FEATURE
        // ============================================================

        public bool ShouldConvert()
            => Decide(_auto.Conversion, "¿Quieres que POPSManager convierta automáticamente estos juegos?");

        public bool ShouldHandleMultiDisc()
            => Decide(_auto.MultiDisc, "He detectado un juego multidisco. ¿Quieres que lo agrupe y genere DISCS.TXT automáticamente?");

        public bool ShouldCreateFolders()
            => Decide(_auto.FolderCreation, "¿Quieres que se creen automáticamente todas las carpetas necesarias para OPL (POPS, APPS, ART, CFG, DVD)?");

        public bool ShouldDownloadCovers()
            => Decide(_auto.Covers, "¿Quieres que se descarguen automáticamente las carátulas de los juegos?");

        public bool ShouldUseDatabase()
            => Decide(_auto.Database, "¿Quieres que se use la base de datos para nombres oficiales y metadatos?");

        public bool ShouldGenerateCheats()
            => Decide(_auto.Cheats, "¿Quieres que se genere CHEAT.TXT automáticamente para juegos compatibles?");

        public bool ShouldShowNotifications()
            => _auto.Notifications != AutoBehavior.Manual;

        // ============================================================
        //  NÚCLEO DE DECISIÓN
        // ============================================================
        private bool Decide(AutoBehavior behavior, string question)
        {
            // Modo global Manual → nunca automático
            if (_auto.Mode == AutomationMode.Manual)
                return false;

            // Modo global Automático → siempre que no esté forzado a Manual
            if (_auto.Mode == AutomationMode.Automatico)
                return behavior != AutoBehavior.Manual;

            // Modo Asistido → depende del comportamiento
            return behavior switch
            {
                AutoBehavior.Automatico => true,
                AutoBehavior.Manual => false,
                AutoBehavior.Preguntar => Ask(question),
                _ => false
            };
        }

        private bool Ask(string question)
        {
            if (AskUser == null)
            {
                // Si no hay callback, degradar a notificación informativa
                _notifications.Info(question);
                return false;
            }

            bool? result = AskUser.Invoke(question);
            return result == true;
        }
    }
}
