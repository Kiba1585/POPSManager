using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

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
            // Aquí puedes conectar tus estadísticas reales
            ProcessedCount.Text = "0";
            PendingCount.Text = "0";
            ErrorCount.Text = "0";
        }

        private void LoadSystemInfo()
        {
            SystemInfo.Text =
                $"Versión: 1.0.0\n" +
                $"Directorio base: {AppContext.BaseDirectory}\n" +
                $"POPS: {App.Services.Paths.PopsFolder}\n" +
                $"APPS: {App.Services.Paths.AppsFolder}";
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
            Process.Start(new ProcessStartInfo
            {
                FileName = App.Services.Paths.PopsFolder,
                UseShellExecute = true
            });
        }

        private void OpenAppsFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = App.Services.Paths.AppsFolder,
                UseShellExecute = true
            });
        }
    }
}
