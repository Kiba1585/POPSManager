using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
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

            _services = App.Services!;
            _paths = _services.Paths;

            LoadStats();
            LoadSystemInfo();
            LoadPaths();
        }

        // ============================================================
        //  ESTADÍSTICAS (placeholder)
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
                $"POPSTARTER.ELF: {_paths.PopstarterElfPath}\n" +
                $"POPS2.ELF: {_paths.PopstarterPs2ElfPath}";
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
            if (Application.Current.MainWindow is MainWindow main)
                main.LoadView(new ConvertView());
        }

        private void OpenProcessPops_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow main)
                main.LoadView(new ProcessPopsView());
        }

        // ============================================================
        //  ABRIR CARPETA RAÍZ
        // ============================================================
        private void OpenRootFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(_paths.RootFolder))
            {
                _services.Notifications.Error("La carpeta raíz no existe.");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _paths.RootFolder,
                    UseShellExecute = true
                });
            }
            catch
            {
                _services.Notifications.Error("No se pudo abrir la carpeta raíz.");
            }
        }

        // ============================================================
        //  ABRIR UBICACIÓN DEL ELF
        // ============================================================
        private void OpenElfFolder_Click(object sender, RoutedEventArgs e)
        {
            string elf = _paths.PopstarterElfPath;

            if (!File.Exists(elf))
            {
                _services.Notifications.Error("POPSTARTER.ELF no está configurado o no existe.");
                return;
            }

            string? folder = Path.GetDirectoryName(elf);

            if (folder == null || !Directory.Exists(folder))
            {
                _services.Notifications.Error("No se pudo determinar la carpeta del ELF.");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
            catch
            {
                _services.Notifications.Error("No se pudo abrir la carpeta del ELF.");
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

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            string newPath = dialog.FileName;

            if (!Directory.Exists(newPath))
            {
                _services.Notifications.Error("La carpeta seleccionada no existe.");
                return;
            }

            _services.Settings.SetRootFolder(newPath);

            LoadPaths();
            LoadSystemInfo();

            _services.Notifications.Success("Ruta raíz actualizada correctamente.");
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

            if (dialog.ShowDialog() != true)
                return;

            string path = dialog.FileName;

            if (!File.Exists(path))
            {
                _services.Notifications.Error("El archivo seleccionado no existe.");
                return;
            }

            _services.Settings.SetCustomElfPath(path);

            LoadPaths();
            LoadSystemInfo();

            _services.Notifications.Success("POPSTARTER.ELF actualizado correctamente.");
        }
    }
}
