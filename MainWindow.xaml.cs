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

            var notifications = new NotificationManager(Notifier, App.Services!.Localization);
            viewModel.Initialize(notifications);
        }

        public void LoadView(System.Windows.Controls.UserControl view)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.CurrentView = view;
            }
        }
    }
}