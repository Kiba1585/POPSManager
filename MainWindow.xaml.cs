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
            // y al servicio de localización para los títulos
            var notifications = new NotificationManager(Notifier, App.Services!.Localization);
            viewModel.Initialize(notifications);
        }
    }
}