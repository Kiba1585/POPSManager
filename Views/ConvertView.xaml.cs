using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using POPSManager.Models;
using POPSManager.Services;   // ← ESTE ERA EL QUE FALTABA

namespace POPSManager.Views
{
    public partial class ConvertView : UserControl
    {
        // Acceso seguro a los servicios globales
        private AppServices Services => ((App)Application.Current).Services;

        public ConvertView()
        {
            InitializeComponent();
        }

        private void BrowseSource_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleccionar carpeta de origen"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SourcePath.Text = dlg.FileName;
                LoadFiles();
            }
        }

        private void BrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleccionar carpeta de destino"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                OutputPath.Text = dlg.FileName;
            }
        }

        private void LoadFiles()
        {
            FilesList.Items.Clear();

            if (!Directory.Exists(SourcePath.Text))
                return;

            var files = Directory.GetFiles(SourcePath.Text, "*.*")
                                 .Where(f => f.EndsWith(".bin", System.StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".cue", System.StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".iso", System.StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
                FilesList.Items.Add(Path.GetFileName(file));
        }

        private async void Convert_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(SourcePath.Text) ||
                !Directory.Exists(OutputPath.Text))
            {
                Services.Notifications.Show(
                    new UiNotification(NotificationType.Error,
                    "Debes seleccionar carpetas válidas."));
                return;
            }

            Services.Progress.SetStatus("Convirtiendo archivos...");

            await Task.Run(() =>
            {
                Services.Converter.ConvertFolder(SourcePath.Text, OutputPath.Text);
            });

            Services.Progress.SetStatus("Listo");

            Services.Notifications.Show(
                new UiNotification(NotificationType.Success,
                "Conversión completada."));
        }
    }
}
