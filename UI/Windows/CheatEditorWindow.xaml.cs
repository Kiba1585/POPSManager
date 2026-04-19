using System.Windows;
using POPSManager.ViewModels;

namespace POPSManager.UI.Windows
{
    public partial class CheatEditorWindow : Window
    {
        public CheatEditorWindow(CheatEditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}