using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace POPSManager.Views
{
    public partial class ConvertView : UserControl
    {
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
                                 .Where(f => f.EndsWith(".bin") ||
                                             f.EndsWith(".cue") ||
                                             f.EndsWith(".iso"));

            foreach (var file in files)
                FilesList.Items.Add(System.IO.Path.GetFileName(file));
        }

        private async void Convert_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(SourcePath.Text) ||
                !Directory.Exists(OutputPath.Text))
            {
                App.Services.Notify(new UiNotification(NotificationType.Error,
                    "Debes seleccionar carpetas válidas."));
                return;
            }

            App.Services.Progress.Start("Convirtiendo archivos...");

            await Task.Run(() =>
            {
                App.Services.Converter.ConvertFolder(SourcePath.Text, OutputPath.Text);
            });

            App.Services.Progress.Stop();

            App.Services.Notify(new UiNotification(NotificationType.Success,
                "Conversión completada."));
        }
    }
}
