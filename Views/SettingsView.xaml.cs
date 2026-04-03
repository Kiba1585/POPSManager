using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using POPSManager.Models;
using POPSManager.Services;   // ← ESTE ERA EL QUE FALTABA

namespace POPSManager.Views
{
    public partial class SettingsView : UserControl
    {
        // Acceso seguro a los servicios globales
        private AppServices Services => ((App)Application.Current).Services;

        public SettingsView()
        {
            InitializeComponent();

            // Cargar valores iniciales
            PopsPath.Text = Services.Paths.PopsFolder;
            AppsPath.Text = Services.Paths.AppsFolder;

            DarkModeToggle.IsChecked = Services.Settings.DarkMode;
            NotificationsToggle.IsChecked = Services.Settings.NotificationsEnabled;
        }

        // ============================
        //  CAMBIAR RUTA POPS
        // ============================
        private void ChangePopsPath_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleccionar carpeta POPS"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Services.Paths.PopsFolder = dlg.FileName;
                PopsPath.Text = dlg.FileName;

                Services.Notifications.Show(
                    new UiNotification(NotificationType.Success, "Ruta POPS actualizada"));
            }
        }

        // ============================
        //  CAMBIAR RUTA APPS
        // ============================
        private void ChangeAppsPath_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleccionar carpeta APPS"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Services.Paths.AppsFolder = dlg.FileName;
                AppsPath.Text = dlg.FileName;

                Services.Notifications.Show(
                    new UiNotification(NotificationType.Success, "Ruta APPS actualizada"));
            }
        }

        // ============================
        //  MODO OSCURO
        // ============================
        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            Services.Settings.DarkMode = true;
            Services.Notifications.Show(
                new UiNotification(NotificationType.Info, "Modo oscuro activado"));
        }

        private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            Services.Settings.DarkMode = false;
            Services.Notifications.Show(
                new UiNotification(NotificationType.Info, "Modo oscuro desactivado"));
        }

        // ============================
        //  NOTIFICACIONES
        // ============================
        private void NotificationsToggle_Checked(object sender, RoutedEventArgs e)
        {
            Services.Settings.NotificationsEnabled = true;
            Services.Notifications.Show(
                new UiNotification(NotificationType.Info, "Notificaciones activadas"));
        }

        private void NotificationsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            Services.Settings.NotificationsEnabled = false;
            Services.Notifications.Show(
                new UiNotification(NotificationType.Info, "Notificaciones desactivadas"));
        }

        // ============================
        //  ABRIR CARPETA DEL PROGRAMA
        // ============================
        private void OpenProgramFolder_Click(object sender, RoutedEventArgs e)
        {
            var folder = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);

            System.Diagnostics.Process.Start("explorer.exe", folder);
        }
    }
}
