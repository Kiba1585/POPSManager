using Microsoft.Win32;
using POPSManager.Logic;
using POPSManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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

        // ============================================================
        //  DRAG & DROP
        // ============================================================

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

            App.Services.Notify(new UiNotification(NotificationType.Info,
                $"{pendingFiles.Count} archivo(s) listos para convertir."));
        }

        private bool IsValidFile(string file)
        {
            string ext = Path.GetExtension(file).ToLower();
            return ext is ".bin" or ".cue" or ".iso";
        }

        // ============================================================
        //  SELECCIÓN MANUAL
        // ============================================================

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

                App.Services.Notify(new UiNotification(NotificationType.Info,
                    $"{pendingFiles.Count} archivo(s) listos para convertir."));
            }
        }

        // ============================================================
        //  VISTA PREVIA
        // ============================================================

        private void UpdatePreview(string file)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            PreviewName.Text = name;

            string? gameId = GameIdDetector.DetectGameId(file);
            PreviewGameId.Text = gameId ?? "Desconocido";

            PreviewRegion.Text = gameId != null && gameId.StartsWith("SCES")
                ? "PAL"
                : "NTSC";

            PreviewType.Text = file.EndsWith(".iso", StringComparison.OrdinalIgnoreCase)
                ? "ISO"
                : "BIN/CUE";
        }

        // ============================================================
        //  PROCESAR ARCHIVOS
        // ============================================================

        private async void ConvertNow_Click(object sender, RoutedEventArgs e)
        {
            if (pendingFiles.Count == 0)
            {
                App.Services.Notify(new UiNotification(NotificationType.Warning,
                    "No hay archivos para convertir."));
                return;
            }

            App.Services.Notify(new UiNotification(NotificationType.Info,
                "Iniciando conversión..."));

            App.Services.Progress.Start("Convirtiendo juegos...");

            int processed = 0;

            foreach (var file in pendingFiles)
            {
                try
                {
                    string folder = Path.GetDirectoryName(file)!;

                    await App.Services.Converter.ConvertFolder(
                        folder,
                        App.Services.Paths.PopsFolder
                    );

                    processed++;
                    App.Services.Log($"Convertido: {file}");
                }
                catch (Exception ex)
                {
                    App.Services.Log($"ERROR al convertir {file}: {ex.Message}");
                    App.Services.Notify(new UiNotification(NotificationType.Error,
                        $"Error al convertir {Path.GetFileName(file)}"));
                }

                App.Services.Progress.Update((processed * 100) / pendingFiles.Count);
            }

            App.Services.Progress.Stop();
            App.Services.Notify(new UiNotification(NotificationType.Success,
                "Conversión completada."));

            pendingFiles.Clear();
        }
    }
}
