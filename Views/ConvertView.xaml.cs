using System.Windows.Controls;
using POPSManager.ViewModels;

namespace POPSManager.Views
{
    public partial class ConvertView : System.Windows.Controls.UserControl
    {
        public ConvertView()
        {
            InitializeComponent();
            DataContext = new ConvertViewModel();
        }
    }
}