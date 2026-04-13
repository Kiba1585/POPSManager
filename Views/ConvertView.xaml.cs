using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
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
            using var dlg = new FolderBrowserDialog
            {
                Description = "Seleccionar carpeta de origen",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            SourcePath.Text = dlg.SelectedPath;
            LoadFiles();
        }

        // ============================================================
        //  SELECCIONAR CARPETA DE DESTINO
        // ============================================================
        private void BrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Seleccionar carpeta de destino",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            OutputPath.Text = dlg.SelectedPath;
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

            Services.Notifications.Info(
                $"Se detectaron {FilesList.Items.Count} archivos.");
        }

        // ============================================================
        //  CONVERTIR ARCHIVOS
        // ============================================================
        private async void Convert_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(SourcePath.Text))
            {
                Services.Notifications.Error("La carpeta de origen no existe.");
                return;
            }

            if (!Directory.Exists(OutputPath.Text))
            {
                Services.Notifications.Error("La carpeta de destino no existe.");
                return;
            }

            if (FilesList.Items.Count == 0)
            {
                Services.Notifications.Warning("No hay archivos para convertir.");
                return;
            }

            Services.Progress.Start("Convirtiendo archivos...");

            try
            {
                await Task.Run(() =>
                {
                    Services.Converter.ConvertFolder(SourcePath.Text, OutputPath.Text);
                });

                Services.Notifications.Success("Conversión completada.");
            }
            catch (Exception ex)
            {
                Services.Notifications.Error($"Error durante la conversión: {ex.Message}");
            }
            finally
            {
                Services.Progress.Stop();
            }
        }
    }
}
