using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Forms;
using POPSManager.Services;

namespace POPSManager.Views
{
    public partial class Dashboard : UserControl
    {
        private readonly PathsService _paths;

        public Dashboard()
        {
            InitializeComponent();

            _paths = ((App)Application.Current).Services.Paths;

            LoadStats();
            LoadSystemInfo();
            LoadPaths();
        }

        // ============================================================
        //  ESTADÍSTICAS
        // ============================================================
        private void LoadStats()
        {
            ProcessedCount.Text = "0";
            PendingCount.Text = "0";
            ErrorCount.Text = "0";
        }

        // ============================================================
        //  INFORMACIÓN DEL SISTEMA
        // ============================================================
        private void LoadSystemInfo()
        {
            SystemInfo.Text =
                $"Versión: 1.0.0\n" +
                $"Directorio base: {AppContext.BaseDirectory}\n" +
                $"Ruta raíz: {_paths.RootFolder}\n" +
                $"POPSTARTER.ELF: {_paths.PopstarterElfPath}";
        }

        // ============================================================
        //  CARGAR RUTAS EN LA UI
        // ============================================================
        private void LoadPaths()
        {
            RootPath.Text = _paths.RootFolder ?? "";
            ElfPath.Text = _paths.PopstarterElfPath ?? "";
        }

        // ============================================================
        //  ACCIONES RÁPIDAS
        // ============================================================
        private void OpenConvert_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).LoadView(new ConvertView());
        }

        private void OpenProcessPops_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).LoadView(new ProcessPopsView());
        }

        // ============================================================
        //  ABRIR CARPETA RAÍZ
        // ============================================================
        private void OpenRootFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_paths.RootFolder) && Directory.Exists(_paths.RootFolder))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _paths.RootFolder,
                    UseShellExecute = true
                });
            }
            else
            {
                System.Windows.MessageBox.Show("La carpeta raíz no está configurada o no existe.");
            }
        }

        // ============================================================
        //  ABRIR UBICACIÓN DEL ELF
        // ============================================================
        private void OpenElfFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_paths.PopstarterElfPath) &&
                File.Exists(_paths.PopstarterElfPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.GetDirectoryName(_paths.PopstarterElfPath),
                    UseShellExecute = true
                });
            }
            else
            {
                System.Windows.MessageBox.Show("El archivo POPSTARTER.ELF no está configurado o no existe.");
            }
        }

        // ============================================================
        //  CAMBIAR RUTA RAÍZ
        // ============================================================
        private void ChangeRootPath_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Selecciona la carpeta raíz donde se crearán POPS, APPS, CFG, ART...",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _paths.RootFolder = dialog.SelectedPath;
                RootPath.Text = _paths.RootFolder;

                // Crear estructura OPL automáticamente
                Directory.CreateDirectory(Path.Combine(_paths.RootFolder, "POPS"));
                Directory.CreateDirectory(Path.Combine(_paths.RootFolder, "APPS"));
                Directory.CreateDirectory(Path.Combine(_paths.RootFolder, "CFG"));
                Directory.CreateDirectory(Path.Combine(_paths.RootFolder, "ART"));

                _paths.Save();

                LoadSystemInfo();
            }
        }

        // ============================================================
        //  SELECCIONAR POPSTARTER.ELF
        // ============================================================
        private void SelectElf_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar POPSTARTER.ELF",
                Filter = "ELF files (*.ELF)|*.ELF|Todos los archivos (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                _paths.PopstarterElfPath = dialog.FileName;
                ElfPath.Text = _paths.PopstarterElfPath;

                _paths.Save();
                LoadSystemInfo();
            }
        }
    }
}
