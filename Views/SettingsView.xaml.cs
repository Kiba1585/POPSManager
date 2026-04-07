using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Win32;
using POPSManager.Models;
using POPSManager.Services;

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
            PopsPath.Text = Services.Settings.PopsFolder;
            AppsPath.Text = Services.Settings.AppsFolder;

            DarkModeToggle.IsChecked = Services.Settings.DarkMode;
            NotificationsToggle.IsChecked = Services.Settings.NotificationsEnabled;

            // Mostrar ruta del POPSTARTER.ELF si existe
            ElfPathBox.Text = Services.Settings.CustomElfPath;
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
                // Guardar en Settings
                Services.Settings.SetPopsFolder(dlg.FileName);

                // Actualizar UI
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
                // Guardar en Settings
                Services.Settings.SetAppsFolder(dlg.FileName);

                // Actualizar UI
                AppsPath.Text = dlg.FileName;

                Services.Notifications.Show(
                    new UiNotification(NotificationType.Success, "Ruta APPS actualizada"));
            }
        }

        // ============================
        //  SELECCIONAR POPSTARTER.ELF
        // ============================
        private void SelectElf_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "POPStarter ELF|POPSTARTER.ELF|Todos los archivos|*.*",
                Title = "Seleccionar POPSTARTER.ELF"
            };

            if (dlg.ShowDialog() == true)
            {
                // Guardar en Settings
                Services.Settings.CustomElfPath = dlg.FileName;
                Services.Settings.Save();

                // Actualizar PathsService
                Services.Paths.SetCustomElfPath(dlg.FileName);

                // Mostrar en UI
                ElfPathBox.Text = dlg.FileName;

                Services.Notifications.Show(
                    new UiNotification(NotificationType.Success, "POPSTARTER.ELF configurado correctamente"));
            }
        }

        // ============================
        //  MODO OSCURO
        // ============================
        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            Services.Settings.DarkMode = true;
            Services.Settings.Save();

            Services.Notifications.Show(
                new UiNotification(NotificationType.Info, "Modo oscuro activado"));
        }

        private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            Services.Settings.DarkMode = false;
            Services.Settings.Save();

            Services.Notifications.Show(
                new UiNotification(NotificationType.Info, "Modo oscuro desactivado"));
        }

        // ============================
        //  NOTIFICACIONES
        // ============================
        private void NotificationsToggle_Checked(object sender, RoutedEventArgs e)
        {
            Services.Settings.NotificationsEnabled = true;
            Services.Settings.Save();

            Services.Notifications.Show(
                new UiNotification(NotificationType.Info, "Notificaciones activadas"));
        }

        private void NotificationsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            Services.Settings.NotificationsEnabled = false;
            Services.Settings.Save();

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

            if (!string.IsNullOrWhiteSpace(folder))
                System.Diagnostics.Process.Start("explorer.exe", folder);
            else
                System.Diagnostics.Process.Start("explorer.exe");
        }
    }
}
