using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace POPSManager.Views
{
    public partial class ConvertView : UserControl
    {
        private readonly List<string> pendingFiles = new();

        public ConvertView()
        {
            InitializeComponent();
        }

        // ============================
        //  DRAG & DROP
        // ============================

        private void DragOverFiles(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void DropFiles(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var file in files)
            {
                if (IsValidFile(file))
                {
                    pendingFiles.Add(file);
                    UpdatePreview(file);
                }
            }
        }

        private bool IsValidFile(string file)
        {
            string ext = Path.GetExtension(file).ToLower();
            return ext is ".bin" or ".cue" or ".iso";
        }

        // ============================
        //  SELECCIÓN MANUAL
        // ============================

        private void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new()
            {
                Filter = "Archivos PS1|*.bin;*.cue;*.iso",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                foreach (var file in dlg.FileNames)
                {
                    pendingFiles.Add(file);
                    UpdatePreview(file);
                }
            }
        }

        // ============================
        //  VISTA PREVIA
        // ============================

        private void UpdatePreview(string file)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            PreviewName.Text = name;

            string? gameId = GameIdDetector.DetectGameId(file);
            PreviewGameId.Text = gameId ?? "Desconocido";

            PreviewRegion.Text = gameId != null && gameId.StartsWith("SCES") ? "PAL" : "NTSC";

            PreviewType.Text = file.EndsWith(".iso", StringComparison.OrdinalIgnoreCase)
                ? "ISO"
                : "BIN/CUE";
        }

        // ============================
        //  PROCESAR
        // ============================

        private async void ConvertNow_Click(object sender, RoutedEventArgs e)
        {
            if (pendingFiles.Count == 0)
            {
                MessageBox.Show("No hay archivos para convertir.");
                return;
            }

            foreach (var file in pendingFiles)
            {
                string folder = Path.GetDirectoryName(file)!;

                await App.Services.Converter.ConvertFolder(
                    folder,
                    Path.Combine(AppContext.BaseDirectory, "POPS")
                );
            }

            MessageBox.Show("Conversión completada.");
            pendingFiles.Clear();
        }
    }
}
