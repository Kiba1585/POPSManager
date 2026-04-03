using Microsoft.WindowsAPICodePack.Dialogs;
using POPSManager.Models;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace POPSManager.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();

            // Cargar valores actuales
            PopsPath.Text = App.Services.Paths.PopsFolder;
            AppsPath.Text = App.Services.Paths.AppsFolder;
        }

        // ============================================================
        //  CAMBIAR RUTA POPS
        // ============================================================

        private void ChangePopsPath_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleccionar carpeta POPS"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                PopsPath.Text = dlg.FileName;
                App.Services.Paths.PopsFolder = dlg.FileName;

                App.Services.Notify(new UiNotification(NotificationType.Success,
                    "Ruta POPS actualizada."));
                App.Services.Log($"Ruta POPS cambiada a: {dlg.FileName}");
            }
        }

        // ============================================================
        //  CAMBIAR RUTA APPS
        // ============================================================

        private void ChangeAppsPath_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleccionar carpeta APPS"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                AppsPath.Text = dlg.FileName;
                App.Services.Paths.AppsFolder = dlg.FileName;

                App.Services.Notify(new UiNotification(NotificationType.Success,
                    "Ruta APPS actualizada."));
                App.Services.Log($"Ruta APPS cambiada a: {dlg.FileName}");
            }
        }

        // ============================================================
        //  SWITCH: MODO OSCURO
        // ============================================================

        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            App.Services.Notify(new UiNotification(NotificationType.Info,
                "Modo oscuro activado (pendiente de implementación)."));

            App.Services.Log("Modo oscuro activado.");
        }

        private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            App.Services.Notify(new UiNotification(NotificationType.Info,
                "Modo oscuro desactivado (pendiente de implementación)."));

            App.Services.Log("Modo oscuro desactivado.");
        }

        // ============================================================
        //  SWITCH: NOTIFICACIONES
        // ============================================================

        private void NotificationsToggle_Checked(object sender, RoutedEventArgs e)
        {
            App.Services.Settings.NotificationsEnabled = true;

            App.Services.Notify(new UiNotification(NotificationType.Success,
                "Notificaciones activadas."));
            App.Services.Log("Notificaciones activadas.");
        }

        private void NotificationsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            App.Services.Settings.NotificationsEnabled = false;

            App.Services.Notify(new UiNotification(NotificationType.Warning,
                "Notificaciones desactivadas."));
            App.Services.Log("Notificaciones desactivadas.");
        }

        // ============================================================
        //  ABRIR CARPETA DEL PROGRAMA
        // ============================================================

        private void OpenProgramFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AppContext.BaseDirectory,
                UseShellExecute = true
            });

            App.Services.Log("Carpeta del programa abierta.");
        }
    }
}
