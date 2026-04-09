using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Win32;
using POPSManager.Services;

namespace POPSManager.Views
{
    public partial class SettingsView : UserControl
    {
        private AppServices Services => App.Services!;

        public SettingsView()
        {
            InitializeComponent();
            LoadSettings();
        }

        // ============================================================
        //  CARGAR SETTINGS EN LA UI
        // ============================================================
        private void LoadSettings()
        {
            try
            {
                RootPathBox.Text = Services.Paths.RootFolder;

                PopsPath.Text = Services.Paths.PopsFolder;
                AppsPath.Text = Services.Paths.AppsFolder;

                ElfPathBox.Text = Services.Paths.PopstarterElfPath;

                DarkModeToggle.IsChecked = Services.Settings.DarkMode;
                NotificationsToggle.IsChecked = Services.Settings.NotificationsEnabled;
            }
            catch (Exception ex)
            {
                Services.Notifications.Error("Error cargando configuración.");
                Services.LogService.Error($"[SettingsView] Error LoadSettings: {ex.Message}");
            }
        }

        // ============================================================
        //  VALIDAR RUTA RAÍZ (evitar PS2/PS1/POPSManager)
        // ============================================================
        private bool IsInvalidRoot(string path)
        {
            string folder = Path.GetFileName(path).ToUpperInvariant();

            return folder == "PS2" ||
                   folder == "PS1" ||
                   folder == "POPSMANAGER";
        }

        // ============================================================
        //  CAMBIAR CARPETA RAÍZ DEL DISPOSITIVO OPL
        // ============================================================
        private void ChangeRootFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleccionar carpeta raíz del dispositivo OPL"
            };

            if (dlg.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            string path = dlg.FileName;

            if (!Directory.Exists(path))
            {
                Services.Notifications.Error("La carpeta seleccionada no existe.");
                return;
            }

            // Validación: evitar PS2/PS1/POPSManager
            if (IsInvalidRoot(path))
            {
                Services.Notifications.Warning(
                    "Has seleccionado una carpeta incorrecta (PS2/PS1). Se usará su carpeta padre."
                );

                path = Directory.GetParent(path)?.FullName ?? path;
            }

            try
            {
                // Guardar la nueva raíz
                Services.Settings.RootFolder = path;
                Services.Settings.Save();

                // Regenerar PathsService con la nueva raíz
                App.Services.Paths = new PathsService(
                    Services.LogService.Write,
                    Services.Settings
                );

                // Actualizar UI
                RootPathBox.Text = Services.Paths.RootFolder;
                PopsPath.Text = Services.Paths.PopsFolder;
                AppsPath.Text = Services.Paths.AppsFolder;

                Services.Notifications.Success("Carpeta raíz actualizada correctamente.");
            }
            catch (Exception ex)
            {
                Services.Notifications.Error("No se pudo actualizar la carpeta raíz.");
                Services.LogService.Error($"[SettingsView] Error ChangeRootFolder: {ex.Message}");
            }
        }

        // ============================================================
        //  CAMBIAR CARPETA POPS
        // ============================================================
        private void ChangePopsPath_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleccionar carpeta POPS"
            };

            if (dlg.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            string path = dlg.FileName;

            if (!Directory.Exists(path))
            {
                Services.Notifications.Error("La carpeta seleccionada no existe.");
                return;
            }

            if (IsInvalidRoot(path))
            {
                Services.Notifications.Warning(
                    "Has seleccionado una carpeta incorrecta (PS2/PS1). Se usará su carpeta padre."
                );

                path = Directory.GetParent(path)?.FullName ?? path;
            }

            try
            {
                Services.Paths.SetCustomPopsFolder(path);
                PopsPath.Text = Services.Paths.PopsFolder;

                Services.Notifications.Success("Ruta POPS actualizada correctamente.");
            }
            catch (Exception ex)
            {
                Services.Notifications.Error("No se pudo actualizar la ruta POPS.");
                Services.LogService.Error($"[SettingsView] Error ChangePopsPath: {ex.Message}");
            }
        }

        // ============================================================
        //  CAMBIAR CARPETA APPS
        // ============================================================
        private void ChangeAppsPath_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleccionar carpeta APPS"
            };

            if (dlg.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            string path = dlg.FileName;

            if (!Directory.Exists(path))
            {
                Services.Notifications.Error("La carpeta seleccionada no existe.");
                return;
            }

            if (IsInvalidRoot(path))
            {
                Services.Notifications.Warning(
                    "Has seleccionado una carpeta incorrecta (PS2/PS1). Se usará su carpeta padre."
                );

                path = Directory.GetParent(path)?.FullName ?? path;
            }

            try
            {
                Services.Paths.SetCustomAppsFolder(path);
                AppsPath.Text = Services.Paths.AppsFolder;

                Services.Notifications.Success("Ruta APPS actualizada correctamente.");
            }
            catch (Exception ex)
            {
                Services.Notifications.Error("No se pudo actualizar la ruta APPS.");
                Services.LogService.Error($"[SettingsView] Error ChangeAppsPath: {ex.Message}");
            }
        }

        // ============================================================
        //  SELECCIONAR POPSTARTER.ELF
        // ============================================================
        private void SelectElf_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "POPStarter ELF|POPSTARTER.ELF|Todos los archivos|*.*",
                Title = "Seleccionar POPSTARTER.ELF"
            };

            if (dlg.ShowDialog() != true)
                return;

            string path = dlg.FileName;

            if (!File.Exists(path))
            {
                Services.Notifications.Error("El archivo seleccionado no existe.");
                return;
            }

            try
            {
                Services.Paths.SetCustomElfPath(path);
                ElfPathBox.Text = Services.Paths.PopstarterElfPath;

                Services.Notifications.Success("POPSTARTER.ELF configurado correctamente.");
            }
            catch (Exception ex)
            {
                Services.Notifications.Error("No se pudo configurar POPSTARTER.ELF.");
                Services.LogService.Error($"[SettingsView] Error SelectElf: {ex.Message}");
            }
        }

        // ============================================================
        //  TOGGLE: MODO OSCURO
        // ============================================================
        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            Services.Settings.DarkMode = true;
            Services.Settings.Save();
            Services.Notifications.Info("Modo oscuro activado");
        }

        private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            Services.Settings.DarkMode = false;
            Services.Settings.Save();
            Services.Notifications.Info("Modo oscuro desactivado");
        }

        // ============================================================
        //  TOGGLE: NOTIFICACIONES
        // ============================================================
        private void NotificationsToggle_Checked(object sender, RoutedEventArgs e)
        {
            Services.Settings.NotificationsEnabled = true;
            Services.Settings.Save();
            Services.Notifications.Info("Notificaciones activadas");
        }

        private void NotificationsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            Services.Settings.NotificationsEnabled = false;
            Services.Settings.Save();
            Services.Notifications.Info("Notificaciones desactivadas");
        }

        // ============================================================
        //  ABRIR CARPETA DEL PROGRAMA
        // ============================================================
        private void OpenProgramFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? folder = Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);

                if (!string.IsNullOrWhiteSpace(folder))
                    System.Diagnostics.Process.Start("explorer.exe", folder);
                else
                    System.Diagnostics.Process.Start("explorer.exe");
            }
            catch (Exception ex)
            {
                Services.Notifications.Error("No se pudo abrir la carpeta del programa.");
                Services.LogService.Error($"[SettingsView] Error OpenProgramFolder: {ex.Message}");
            }
        }
    }
}
