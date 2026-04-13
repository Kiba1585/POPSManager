using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.Win32;
using POPSManager.Services;
using POPSManager.Logic.Automation;

namespace POPSManager.Views
{
    public partial class SettingsView : UserControl
    {
        private static AppServices Services => App.Services!;

        public SettingsView()
        {
            InitializeComponent();
            LoadSettings();
        }

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

                // ============================
                //  MODO GLOBAL
                // ============================
                switch (Services.Settings.Automation.Mode)
                {
                    case AutomationMode.Automatico:
                        AutoModeAutomatic.IsChecked = true;
                        break;

                    case AutomationMode.Asistido:
                        AutoModeIntelligent.IsChecked = true;
                        break;

                    case AutomationMode.Manual:
                        AutoModeManual.IsChecked = true;
                        break;
                }

                // ============================
                //  OPCIONES POR PARTE
                // ============================
                SelectBehaviorItem(NormalizeNamesBehaviorBox, Services.Settings.Automation.Conversion);
                SelectBehaviorItem(GroupMultiDiscBehaviorBox, Services.Settings.Automation.MultiDisc);
                SelectBehaviorItem(DownloadCoversBehaviorBox, Services.Settings.Automation.Covers);
            }
            catch (Exception ex)
            {
                Services.Notifications.Error("Error cargando configuración.");
                Services.LogService.Error($"[SettingsView] Error LoadSettings: {ex.Message}");
            }
        }

        private static void SelectBehaviorItem(ComboBox combo, AutoBehavior behavior)
        {
            foreach (ComboBoxItem item in combo.Items)
            {
                if (item.Tag is string tag &&
                    Enum.TryParse<AutoBehavior>(tag, out var value) &&
                    value == behavior)
                {
                    combo.SelectedItem = item;
                    break;
                }
            }
        }

        // ============================================================
        //  CAMBIOS DE RUTAS
        // ============================================================
        private static bool IsInvalidRoot(string path)
        {
            string folder = Path.GetFileName(path).ToUpperInvariant();

            return folder == "PS2" ||
                   folder == "PS1" ||
                   folder == "POPSMANAGER";
        }

        private void ChangeRootFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Seleccionar carpeta raíz del dispositivo OPL",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            string path = dlg.SelectedPath;

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
                Services.Settings.RootFolder = path;
                Services.Settings.Save();

                Services.Paths.Reload();

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

        private void ChangePopsPath_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Seleccionar carpeta POPS",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            string path = dlg.SelectedPath;

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

        private void ChangeAppsPath_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Seleccionar carpeta APPS",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            string path = dlg.SelectedPath;

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
        //  TOGGLES
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
        //  AUTOMATIZACIÓN
        // ============================================================
        private void AutomationMode_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            if (AutoModeAutomatic.IsChecked == true)
                Services.Settings.Automation.Mode = AutomationMode.Automatico;

            else if (AutoModeIntelligent.IsChecked == true)
                Services.Settings.Automation.Mode = AutomationMode.Asistido;

            else if (AutoModeManual.IsChecked == true)
                Services.Settings.Automation.Mode = AutomationMode.Manual;

            Services.Settings.Save();
        }

        private void NormalizeNamesBehaviorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            if (NormalizeNamesBehaviorBox.SelectedItem is ComboBoxItem item &&
                item.Tag is string tag &&
                Enum.TryParse<AutoBehavior>(tag, out var behavior))
            {
                Services.Settings.Automation.Conversion = behavior;
                Services.Settings.Save();
            }
        }

        private void GroupMultiDiscBehaviorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            if (GroupMultiDiscBehaviorBox.SelectedItem is ComboBoxItem item &&
                item.Tag is string tag &&
                Enum.TryParse<AutoBehavior>(tag, out var behavior))
            {
                Services.Settings.Automation.MultiDisc = behavior;
                Services.Settings.Save();
            }
        }

        private void DownloadCoversBehaviorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            if (DownloadCoversBehaviorBox.SelectedItem is ComboBoxItem item &&
                item.Tag is string tag &&
                Enum.TryParse<AutoBehavior>(tag, out var behavior))
            {
                Services.Settings.Automation.Covers = behavior;
                Services.Settings.Save();
            }
        }

        private void OpenProgramFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string folder = AppContext.BaseDirectory;

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
