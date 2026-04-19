using System.Windows;
using POPSManager.UI.Notifications;
using POPSManager.ViewModels;

namespace POPSManager
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Notifications manager necesita acceso a la UI para mostrar toasts
            var notifications = new NotificationManager(Notifier);
            viewModel.Initialize(notifications);
        }
    }
}