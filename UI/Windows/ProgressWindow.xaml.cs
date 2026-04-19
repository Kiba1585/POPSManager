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

            // El título se obtiene del ViewModel (ya localizado)
        }
    }
}