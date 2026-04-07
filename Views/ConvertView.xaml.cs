using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using POPSManager.Models;
using POPSManager.Services;

namespace POPSManager.Views
{
    public partial class ConvertView : UserControl
    {
        // Acceso seguro a los servicios globales
        private AppServices Services => App.Services!;

        public ConvertView()
        {
            InitializeComponent();
        }

        // ============================================================
        //  SELECCIONAR CARPETA DE ORIGEN
        // ============================================================
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

        // ============================================================
        //  SELECCIONAR CARPETA DE DESTINO
        // ============================================================
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

        // ============================================================
        //  CARGAR ARCHIVOS DETECTADOS
        // ============================================================
        private void LoadFiles()
        {
            FilesList.Items.Clear();

            if (!Directory.Exists(SourcePath.Text))
                return;

            var files = Directory.GetFiles(SourcePath.Text, "*.*")
                                 .Where(f =>
                                     f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
                                     f.EndsWith(".cue", StringComparison.OrdinalIgnoreCase) ||
                                     f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                                 .OrderBy(f => f);

            foreach (var file in files)
                FilesList.Items.Add(Path.GetFileName(file));

            Services.Notifications.Show(
                new UiNotification(NotificationType.Info,
                $"Se detectaron {FilesList.Items.Count} archivos."));
        }

        // ============================================================
        //  CONVERTIR ARCHIVOS
        // ============================================================
        private async void Convert_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(SourcePath.Text))
            {
                Services.Notifications.Show(
                    new UiNotification(NotificationType.Error,
                    "La carpeta de origen no existe."));
                return;
            }

            if (!Directory.Exists(OutputPath.Text))
            {
                Services.Notifications.Show(
                    new UiNotification(NotificationType.Error,
                    "La carpeta de destino no existe."));
                return;
            }

            if (FilesList.Items.Count == 0)
            {
                Services.Notifications.Show(
                    new UiNotification(NotificationType.Warning,
                    "No hay archivos para convertir."));
                return;
            }

            Services.Progress.Start("Convirtiendo archivos...");

            try
            {
                await Task.Run(() =>
                {
                    Services.Converter.ConvertFolder(SourcePath.Text, OutputPath.Text);
                });

                Services.Progress.SetStatus("Listo");
            }
            catch (Exception ex)
            {
                Services.Notifications.Show(
                    new UiNotification(NotificationType.Error,
                    $"Error durante la conversión: {ex.Message}"));
            }
            finally
            {
                Services.Progress.Stop();
            }
        }
    }
}
