using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using POPSManager.Models;

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

        private void LoadGames()
        {
            GamesList.Items.Clear();

            if (!Directory.Exists(VcdPath.Text))
                return;

            var files = Directory.GetFiles(VcdPath.Text, "*.vcd");

            foreach (var file in files)
                GamesList.Items.Add(Path.GetFileName(file));
        }

        private async void Process_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(VcdPath.Text))
            {
                Services.Notify(new UiNotification(NotificationType.Error,
                    "Debes seleccionar una carpeta válida."));
                return;
            }

            Services.Progress.Start("Procesando juegos...");

            await Task.Run(() =>
            {
                Services.GameProcessor.ProcessFolder(VcdPath.Text);
            });

            Services.Progress.Stop();

            Services.Notify(new UiNotification(NotificationType.Success,
                "Procesamiento completado."));
        }
    }
}
