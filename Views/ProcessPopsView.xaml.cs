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
    public partial class ProcessPopsView : UserControl
    {
        private AppServices Services => App.Services!;

        public ProcessPopsView()
        {
            InitializeComponent();
        }

        // ============================================================
        //  SELECCIONAR CARPETA DE JUEGOS (VCD + ISO)
        // ============================================================
        private void BrowseVcd_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleccionar carpeta con juegos (VCD / ISO)"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                VcdPath.Text = dlg.FileName;
                _ = LoadGamesAsync();
            }
        }

        // ============================================================
        //  CARGAR LISTA DE JUEGOS (PS1 + PS2) — ASYNC ULTRA PRO
        // ============================================================
        private async Task LoadGamesAsync()
        {
            GamesList.Items.Clear();

            string folder = VcdPath.Text;

            if (!Directory.Exists(folder))
            {
                Services.Notifications.Warning("La carpeta seleccionada no existe.");
                return;
            }

            try
            {
                var files = await Task.Run(() =>
                    Directory.GetFiles(folder)
                             .Where(f =>
                                 f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase) ||
                                 f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                             .OrderBy(f => f)
                             .ToArray()
                );

                foreach (var file in files)
                    GamesList.Items.Add(Path.GetFileName(file));

                Services.Notifications.Info(
                    $"Se detectaron {GamesList.Items.Count} juegos (PS1/PS2).");
            }
            catch (Exception ex)
            {
                Services.Notifications.Error("No se pudieron cargar los juegos.");
                Services.LogService.Error($"[ProcessPopsView] Error cargando juegos: {ex.Message}");
            }
        }

        // ============================================================
        //  PROCESAR JUEGOS (PS1 + PS2)
        // ============================================================
        private async void Process_Click(object sender, RoutedEventArgs e)
        {
            string folder = VcdPath.Text;

            if (!Directory.Exists(folder))
            {
                Services.Notifications.Error("Debes seleccionar una carpeta válida.");
                return;
            }

            if (GamesList.Items.Count == 0)
            {
                Services.Notifications.Warning("No hay archivos VCD o ISO para procesar.");
                return;
            }

            Services.Progress.Reset();
            Services.Progress.Start("Procesando juegos...");

            try
            {
                await Task.Run(() =>
                {
                    Services.GameProcessor.ProcessFolder(folder);
                });

                Services.Progress.SetStatus("Listo");
                Services.Notifications.Success("Procesamiento completado.");
            }
            catch (Exception ex)
            {
                Services.Notifications.Error($"Error durante el procesamiento: {ex.Message}");
                Services.LogService.Error($"[ProcessPopsView] Error procesando juegos: {ex}");
            }
            finally
            {
                Services.Progress.Stop();
            }
        }
    }
}
