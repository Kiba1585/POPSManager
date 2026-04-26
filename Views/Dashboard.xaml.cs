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

            // Conectar el LogsPanel al servicio de logging global
            App.Services!.LogService.OnLog += msg =>
            {
                Dispatcher.Invoke(() =>
                {
                    LogsPanelControl.AddDebug(msg);
                });
            };
        }
    }
}