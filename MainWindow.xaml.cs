using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows; // WPF
using Forms = System.Windows.Forms; // Alias para evitar conflictos
using POPSManager.Logic;

namespace POPSManager
{
    public partial class MainWindow : Window
    {
        private string popsFolder = "";
        private string appsFolder = "";
        private readonly GameProcessor processor;

        public MainWindow()
        {
            InitializeComponent();
            processor = new GameProcessor(UpdateProgress, UpdateSpinner, Log);
        }

        private void SelectPOPS_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                popsFolder = dialog.SelectedPath;
                POPSPath.Text = popsFolder;
            }
        }

        private void SelectAPPS_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                appsFolder = dialog.SelectedPath;
                APPSPath.Text = appsFolder;
            }
        }

        private async void Process_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(popsFolder) || !Directory.Exists(appsFolder))
            {
                MessageBox.Show("Selecciona ambas carpetas primero.");
                return;
            }

            Log("Iniciando procesamiento...");
            await processor.ProcessFolder(popsFolder, appsFolder);
            Log("Proceso completado.");
        }

        private void UpdateProgress(int percent)
        {
            Dispatcher.Invoke(() =>
            {
                MainProgress.Value = percent;
                ProgressText.Text = percent + "%";
            });
        }

        private void UpdateSpinner(string frame)
        {
            Dispatcher.Invoke(() =>
            {
                Spinner.Text = frame;
            });
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogBox.AppendText(message + Environment.NewLine);
                LogBox.ScrollToEnd();
            });
        }

        private async void Convert_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() != Forms.DialogResult.OK)
                return;

            string source = dialog.SelectedPath;

            dialog = new Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() != Forms.DialogResult.OK)
                return;

            string destination = dialog.SelectedPath;

            Log("Iniciando conversión BIN/CUE/ISO → VCD...");

            var converter = new Converter(UpdateProgress, UpdateSpinner, Log);
            await converter.ConvertFolder(source, destination);

            Log("Conversión finalizada.");
        }
    }
}
