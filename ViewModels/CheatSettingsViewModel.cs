using POPSManager.UI.Localization;

namespace POPSManager.ViewModels
{
    public class CheatSettingsViewModel : ViewModelBase
    {
        private readonly LocalizationService _loc;

        public CheatSettingsViewModel(LocalizationService localization)
        {
            _loc = localization;
        }

        public string WindowTitle => _loc.GetString("Title_CheatSettings");
        public string TitleText => _loc.GetString("Title_CheatSettings");
        public string ModeLabel => _loc.GetString("Cheat_ModeLabel");
        public string ModeDisabled => _loc.GetString("Cheat_ModeDisabled");
        public string ModeAutoPal => _loc.GetString("Cheat_ModeAutoPal");
        public string ModeAsk => _loc.GetString("Cheat_ModeAsk");
        public string ModeManual => _loc.GetString("Cheat_ModeManual");
        public string AdvancedLabel => _loc.GetString("Cheat_AdvancedLabel");
        public string AutoFixesText => _loc.GetString("Cheat_AutoFixes");
        public string EngineFixesText => _loc.GetString("Cheat_EngineFixes");
        public string HeuristicFixesText => _loc.GetString("Cheat_HeuristicFixes");
        public string DatabaseFixesText => _loc.GetString("Cheat_DatabaseFixes");
        public string CustomCheatsText => _loc.GetString("Cheat_CustomCheats");
        public string CancelButtonText => _loc.GetString("Button_Cancel");
        public string SaveButtonText => _loc.GetString("Button_Save");
        public string SettingsSavedMessage => _loc.GetString("Message_SettingsSaved");
    }
}