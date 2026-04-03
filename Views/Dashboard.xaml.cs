using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using POPSManager.Services;

namespace POPSManager.Views
{
    public partial class Dashboard : UserControl
    {
        public Dashboard()
        {
            InitializeComponent();
            LoadStats();
            LoadSystemInfo();
        }

        private void LoadStats()
        {
            ProcessedCount.Text = "0";
            PendingCount.Text = "0";
            ErrorCount.Text = "0";
        }

        private void LoadSystemInfo()
        {
            var services = ((App)Application.Current).Services;

            SystemInfo.Text =
                $"Versión: 1.0.0\n" +
                $"Directorio base: {AppContext.BaseDirectory}\n" +
                $"POPS: {services.Paths.PopsFolder}\n" +
                $"APPS: {services.Paths.AppsFolder}";
        }

        private void OpenConvert_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).LoadView(new ConvertView());
        }

        private void OpenProcessPops_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).LoadView(new ProcessPopsView());
        }

        private void OpenPopsFolder_Click(object sender, RoutedEventArgs e)
        {
            var services = ((App)Application.Current).Services;

            Process.Start(new ProcessStartInfo
            {
                FileName = services.Paths.PopsFolder,
                UseShellExecute = true
            });
        }

        private void OpenAppsFolder_Click(object sender, RoutedEventArgs e)
        {
            var services = ((App)Application.Current).Services;

            Process.Start(new ProcessStartInfo
            {
                FileName = services.Paths.AppsFolder,
                UseShellExecute = true
            });
        }
    }
}
