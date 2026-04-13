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
            ViewModel = new ProgressViewModel();
            DataContext = ViewModel;
        }
    }
}
