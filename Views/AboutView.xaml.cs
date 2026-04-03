using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace POPSManager.Views
{
    public partial class AboutView : UserControl
    {
        public AboutView()
        {
            InitializeComponent();
        }

        private void OpenGitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/",
                UseShellExecute = true
            });
        }

        private void OpenWebsite_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://example.com/",
                UseShellExecute = true
            });
        }
    }
}
