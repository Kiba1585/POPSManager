using System;
using System.IO;
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
        private AppServices Services => App.Services;

        public SettingsView()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Rutas reales desde PathsService
            PopsPath.Text = Services.Paths.PopsFolder;
            AppsPath.Text = Services.Paths.AppsFolder;

            DarkModeToggle.IsChecked = Services.Settings.DarkMode;
            NotificationsToggle.IsChecked = Services.Settings.NotificationsEnabled;

            ElfPathBox.Text = Services.Settings.CustomElfPath;
        }

        private void ChangePopsPath_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleccionar carpeta POPS"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (!Directory.Exists(dlg.FileName))
                {
                    Services.Notifications.Show(
                        new UiNotification(NotificationType.Error,
                        "La carpeta seleccionada no existe."));
                    return;
                }

                Services.Paths.SetCustomPopsFolder(dlg.FileName);
                PopsPath.Text = dlg.FileName;

                Services.Notifications.Show(
                    new UiNotification(NotificationType.Success,
                    "Ruta POPS actualizada correctamente."));
            }
        }

        private void ChangeAppsPath_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleccionar carpeta APPS"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (!Directory.Exists(dlg.FileName))
                {
                    Services.Notifications.Show(
                        new UiNotification(NotificationType.Error,
                        "La carpeta seleccionada no existe."));
                    return;
                }

                Services.Paths.SetCustomAppsFolder(dlg.FileName);
                AppsPath.Text = dlg.FileName;

                Services.Notifications.Show(
                    new UiNotification(NotificationType.Success,
                    "Ruta APPS actualizada correctamente."));
            }
        }

        private void SelectElf_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "POPStarter ELF|POPSTARTER.ELF|Todos los archivos|*.*",
                Title = "Seleccionar POPSTARTER.ELF"
            };

            if (dlg.ShowDialog() == true)
            {
                if (!File.Exists(dlg.FileName))
                {
                    Services.Notifications.Show(
                        new UiNotification(NotificationType.Error,
                        "El archivo seleccionado no existe."));
                    return;
                }

                Services.Settings.CustomElfPath = dlg.FileName;
                Services.Settings.Save();

                Services.Paths.SetCustomElfPath(dlg.FileName);

                ElfPathBox.Text = dlg.FileName;

                Services.Notifications.Show(
                    new UiNotification(NotificationType.Success,
                    "POPSTARTER.ELF configurado correctamente."));
            }
        }

        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            Services.Settings.DarkMode = true;
            Services.Settings.Save();

            Services.Notifications.Show(
                new UiNotification(NotificationType.Info,
                "Modo oscuro activado"));
        }

        private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            Services.Settings.DarkMode = false;
            Services.Settings.Save();

            Services.Notifications.Show(
                new UiNotification(NotificationType.Info,
                "Modo oscuro desactivado"));
        }

        private void NotificationsToggle_Checked(object sender, RoutedEventArgs e)
        {
            Services.Settings.NotificationsEnabled = true;
            Services.Settings.Save();

            Services.Notifications.Show(
                new UiNotification(NotificationType.Info,
                "Notificaciones activadas"));
        }

        private void NotificationsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            Services.Settings.NotificationsEnabled = false;
            Services.Settings.Save();

            Services.Notifications.Show(
                new UiNotification(NotificationType.Info,
                "Notificaciones desactivadas"));
        }

        private void OpenProgramFolder_Click(object sender, RoutedEventArgs e)
        {
            var folder = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);

            try
            {
                if (!string.IsNullOrWhiteSpace(folder))
                    System.Diagnostics.Process.Start("explorer.exe", folder);
                else
                    System.Diagnostics.Process.Start("explorer.exe");
            }
            catch
            {
                Services.Notifications.Show(
                    new UiNotification(NotificationType.Error,
                    "No se pudo abrir la carpeta del programa."));
            }
        }
    }
}
