using System;
using POPSManager.Logic.Automation;
using POPSManager.Settings;
using POPSManager.Services.Interfaces;

namespace POPSManager.Logic
{
    public sealed class AutomationEngine
    {
        private readonly AutomationSettings _auto;
        private readonly INotificationService _notify;

        public Func<string, bool?>? AskUser { get; set; }

        public AutomationEngine(AutomationSettings auto, INotificationService notify)
        {
            _auto = auto;
            _notify = notify;
        }

        public bool ShouldConvert() =>
            Decide(_auto.Conversion, "¿Convertir automáticamente estos juegos?");

        public bool ShouldHandleMultiDisc() =>
            Decide(_auto.MultiDisc, "¿Agrupar multidisco automáticamente?");

        public bool ShouldCreateFolders() =>
            Decide(_auto.FolderCreation, "¿Crear carpetas OPL automáticamente?");

        public bool ShouldDownloadCovers() =>
            Decide(_auto.Covers, "¿Descargar carátulas automáticamente?");

        public bool ShouldUseDatabase() =>
            Decide(_auto.Database, "¿Usar base de datos para nombres oficiales?");

        public bool ShouldGenerateCheats() =>
            Decide(_auto.Cheats, "¿Generar CHEAT.TXT automáticamente?");

        private bool Decide(AutoBehavior behavior, string question)
        {
            if (_auto.Mode == AutomationMode.Manual)
                return false;

            if (_auto.Mode == AutomationMode.Automatico)
                return behavior != AutoBehavior.Manual;

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
                _notify.Info(question);
                return false;
            }

            bool? result = AskUser.Invoke(question);
            return result == true;
        }
    }
}
