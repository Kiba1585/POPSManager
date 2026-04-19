using System.Windows;
using POPSManager.ViewModels;

namespace POPSManager.UI.Windows
{
    public partial class GameSelectorWindow : Window
    {
        public GameSelectorWindow(GameSelectorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}