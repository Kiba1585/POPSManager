using POPSManager.Settings;
using System.Windows;

namespace POPSManager.UI.Windows
{
    public partial class CheatSettingsWindow : Window
    {
        private readonly CheatSettingsService _service;

        public CheatSettingsWindow(CheatSettingsService service)
        {
            InitializeComponent();
            _service = service;

            LoadSettings();
        }

        private void LoadSettings()
        {
            var s = _service.Current;

            ModeCombo.SelectedIndex = (int)s.Mode;
            AutoFixesCheck.IsChecked = s.UseAutoGameFixes;
            EngineFixesCheck.IsChecked = s.UseEngineFixes;
            HeuristicFixesCheck.IsChecked = s.UseHeuristicFixes;
            DatabaseFixesCheck.IsChecked = s.UseDatabaseFixes;
            CustomCheatsCheck.IsChecked = s.EnableCustomCheats;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var s = _service.Current;

            s.Mode = (CheatMode)ModeCombo.SelectedIndex;
            s.UseAutoGameFixes = AutoFixesCheck.IsChecked == true;
            s.UseEngineFixes = EngineFixesCheck.IsChecked == true;
            s.UseHeuristicFixes = HeuristicFixesCheck.IsChecked == true;
            s.UseDatabaseFixes = DatabaseFixesCheck.IsChecked == true;
            s.EnableCustomCheats = CustomCheatsCheck.IsChecked == true;

            _service.Save();

            MessageBox.Show("Configuración guardada correctamente.",
                            "POPSManager",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
