using System.Windows.Controls;
using POPSManager.ViewModels;

namespace POPSManager.Views
{
    public partial class Dashboard : System.Windows.Controls.UserControl
    {
        public Dashboard()
        {
            InitializeComponent();
            DataContext = new DashboardViewModel();
        }
    }
}