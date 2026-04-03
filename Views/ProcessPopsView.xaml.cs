using Microsoft.WindowsAPICodePack.Dialogs;
using POPSManager.Logic;
using POPSManager.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace POPSManager.Views
{
    public partial class ProcessPopsView : UserControl
    {
        private string? selectedFolder;

        public ProcessPopsView()
        {
            InitializeComponent();
        }

        // ============================================================
        //  DRAG & DROP
        // ============================================================

        private void DragOverFolder(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void DropFolder(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            string[] items = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (items.Length == 1 && Directory.Exists(items[0]))
            {
                selectedFolder = items[0];
                UpdatePreview(selectedFolder);

                App.Services.Notify(new UiNotification(NotificationType.Info,
                    "Carpeta cargada correctamente."));
            }
            else
            {
                App.Services.Notify(new UiNotification(NotificationType.Warning,
                    "Debes arrastrar solo una carpeta."));
            }
        }

        // ============================================================
        //  SELECCIÓN MANUAL
        // ============================================================

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleccionar carpeta con VCD"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                selectedFolder = dlg.FileName;
                UpdatePreview(selectedFolder);

                App.Services.Notify(new UiNotification(NotificationType.Info,
                    "Carpeta seleccionada."));
            }
        }

        // ============================================================
        //  VISTA PREVIA
        // ============================================================

        private void UpdatePreview(string folder)
        {
            PreviewFolder.Text = folder;

            string? vcd = Directory.GetFiles(folder, "*.vcd").FirstOrDefault();

            if (vcd != null)
            {
                string? gameId = GameIdDetector.DetectGameId(vcd);
                PreviewGameId.Text = gameId ?? "Desconocido";

                PreviewRegion.Text = gameId != null && gameId.StartsWith("SCES")
                    ? "PAL"
                    : "NTSC";
            }
            else
            {
                PreviewGameId.Text = "No encontrado";
                PreviewRegion.Text = "-";
            }
        }

        // ============================================================
        //  PROCESAR POPS
        // ============================================================

        private async void ProcessNow_Click(object sender, RoutedEventArgs e)
        {
            if (selectedFolder == null)
            {
                App.Services.Notify(new UiNotification(NotificationType.Warning,
                    "Selecciona una carpeta primero."));
                return;
            }

            string appsFolder = App.Services.Paths.AppsFolder;

            App.Services.Notify(new UiNotification(NotificationType.Info,
                "Procesando POPS..."));

            App.Services.Progress.Start("Procesando POPS...");
            App.Services.Log($"Procesando carpeta: {selectedFolder}");

            try
            {
                await App.Services.GameProcessor.ProcessFolder(selectedFolder, appsFolder);

                App.Services.Notify(new UiNotification(NotificationType.Success,
                    "POPS procesado correctamente."));

                App.Services.Log("Proceso completado sin errores.");
            }
            catch (System.Exception ex)
            {
                App.Services.Log($"ERROR: {ex.Message}");
                App.Services.Notify(new UiNotification(NotificationType.Error,
                    "Error al procesar POPS."));
            }

            App.Services.Progress.Stop();
        }
    }
}
