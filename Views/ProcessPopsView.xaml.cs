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
        // Acceso seguro a los servicios globales
        private AppServices Services => ((App)Application.Current).Services;

        public ProcessPopsView()
        {
            InitializeComponent();
        }

        // ============================================================
        //  SELECCIONAR CARPETA VCD
        // ============================================================
        private void BrowseVcd_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleccionar carpeta con archivos VCD"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                VcdPath.Text = dlg.FileName;
                LoadGames();
            }
        }

        // ============================================================
        //  CARGAR LISTA DE JUEGOS
        // ============================================================
        private void LoadGames()
        {
            GamesList.Items.Clear();

            if (!Directory.Exists(VcdPath.Text))
                return;

            var files = Directory.GetFiles(VcdPath.Text, "*.vcd")
                                 .OrderBy(f => f);

            foreach (var file in files)
                GamesList.Items.Add(Path.GetFileName(file));

            // Notificación visual
            Services.Notifications.Show(
                new UiNotification(NotificationType.Info,
                $"Se detectaron {GamesList.Items.Count} juegos."));
        }

        // ============================================================
        //  PROCESAR JUEGOS
        // ============================================================
        private async void Process_Click(object sender, RoutedEventArgs e)
        {
            // Validación
            if (!Directory.Exists(VcdPath.Text))
            {
                Services.Notifications.Show(
                    new UiNotification(NotificationType.Error,
                    "Debes seleccionar una carpeta válida."));
                return;
            }

            if (GamesList.Items.Count == 0)
            {
                Services.Notifications.Show(
                    new UiNotification(NotificationType.Warning,
                    "No hay archivos VCD para procesar."));
                return;
            }

            // Iniciar progreso
            Services.Progress.Start("Procesando juegos...");

            try
            {
                await Task.Run(() =>
                {
                    Services.GameProcessor.ProcessFolder(VcdPath.Text);
                });

                Services.Progress.SetStatus("Listo");

                Services.Notifications.Show(
                    new UiNotification(NotificationType.Success,
                    "Procesamiento completado."));
            }
            catch (Exception ex)
            {
                Services.Notifications.Show(
                    new UiNotification(NotificationType.Error,
                    $"Error durante el procesamiento: {ex.Message}"));
            }
            finally
            {
                Services.Progress.Stop();
            }
        }
    }
}
