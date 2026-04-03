using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
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

        // ============================
        //  DRAG & DROP
        // ============================

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
            }
        }

        // ============================
        //  SELECCIÓN MANUAL
        // ============================

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
            }
        }

        // ============================
        //  VISTA PREVIA
        // ============================

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

        // ============================
        //  PROCESAR
        // ============================

        private async void ProcessNow_Click(object sender, RoutedEventArgs e)
        {
            if (selectedFolder == null)
            {
                MessageBox.Show("Selecciona una carpeta primero.");
                return;
            }

            string popsFolder = Path.Combine(AppContext.BaseDirectory, "POPS");
            string appsFolder = Path.Combine(AppContext.BaseDirectory, "APPS");

            await App.Services.GameProcessor.ProcessFolder(selectedFolder, appsFolder);

            MessageBox.Show("POPS procesado correctamente.");
        }
    }
}
