using System.Windows.Controls;
using POPSManager.ViewModels;

namespace POPSManager.Views
{
    public partial class ProcessPopsView : System.Windows.Controls.UserControl
    {
        public ProcessPopsView()
        {
            InitializeComponent();
            DataContext = new ProcessPopsViewModel();
        }
    }
}