using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs; // Folder picker moderno
using POPSManager.Services;
using POPSManager.Models;

namespace POPSManager.Views
{
    public partial class Dashboard : UserControl
    {
        private readonly AppServices _services;
        private readonly PathsService _paths;

        public Dashboard()
        {
            InitializeComponent();

            // CORRECCIÓN: App.Services es nullable → usar !
            _services = App.Services!;
            _paths = _services.Paths;

            LoadStats();
            LoadSystemInfo();
            LoadPaths();
        }

        // ============================================================
        //  ESTADÍSTICAS (placeholder por ahora)
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
                $"POPS: {_paths.PopsFolder}\n" +
                $"DVD (PS2): {_paths.DvdFolder}\n" +
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
            if (Directory.Exists(_paths.RootFolder))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _paths.RootFolder,
                    UseShellExecute = true
                });
            }
            else
            {
                _services.Notifications.Show(
                    new UiNotification(NotificationType.Error,
                    "La carpeta raíz no está configurada o no existe."));
            }
        }

        // ============================================================
        //  ABRIR UBICACIÓN DEL ELF
        // ============================================================
        private void OpenElfFolder_Click(object sender, RoutedEventArgs e)
        {
            string elf = _paths.PopstarterElfPath;

            if (File.Exists(elf))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.GetDirectoryName(elf),
                    UseShellExecute = true
                });
            }
            else
            {
                _services.Notifications.Show(
                    new UiNotification(NotificationType.Error,
                    "El archivo POPSTARTER.ELF no está configurado o no existe."));
            }
        }

        // ============================================================
        //  CAMBIAR RUTA RAÍZ
        // ============================================================
        private void ChangeRootPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Selecciona la carpeta raíz",
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string newPath = dialog.FileName;

                _services.Settings.SetRootFolder(newPath);

                LoadPaths();
                LoadSystemInfo();

                _services.Notifications.Show(
                    new UiNotification(NotificationType.Success,
                    "Ruta raíz actualizada correctamente."));
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
                _services.Settings.SetCustomElfPath(dialog.FileName);

                LoadPaths();
                LoadSystemInfo();

                _services.Notifications.Show(
                    new UiNotification(NotificationType.Success,
                    "POPSTARTER.ELF actualizado correctamente."));
            }
        }
    }
}
