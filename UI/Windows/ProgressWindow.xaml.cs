using System.Windows;
using POPSManager.UI.Progress;

namespace POPSManager.UI.Windows
{
    public partial class ProgressWindow : Window
    {
        public ProgressViewModel ViewModel { get; }

        public ProgressWindow()
        {
            InitializeComponent();

            var services = App.Services!;
            ViewModel = new ProgressViewModel(services.Localization);
            DataContext = ViewModel;

            // Localizar título de la ventana
            Title = services.Localization.GetString("Title_ProgressWindow");
        }
    }
}