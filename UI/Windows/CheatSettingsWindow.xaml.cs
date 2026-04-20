using System.Windows;
using POPSManager.Settings;
using POPSManager.ViewModels;

namespace POPSManager.UI.Windows
{
    public partial class CheatSettingsWindow : Window
    {
        private readonly CheatSettingsService _service;
        private readonly CheatSettingsViewModel _viewModel;

        public CheatSettingsWindow(CheatSettingsService service)
        {
            InitializeComponent();
            _service = service;

            var loc = App.Services!.Localization;
            _viewModel = new CheatSettingsViewModel(loc);
            DataContext = _viewModel;

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

            System.Windows.MessageBox.Show(
                _viewModel.SettingsSavedMessage,
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