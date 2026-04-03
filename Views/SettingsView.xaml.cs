using Microsoft.WindowsAPICodePack.Dialogs;
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
                PopsPath.Text = dlg.FileName;
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
                AppsPath.Text = dlg.FileName;
            }
        }

        // ============================
        //  SWITCH: MODO OSCURO
        // ============================

        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Modo oscuro activado (placeholder).");
        }

        private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Modo oscuro desactivado (placeholder).");
        }

        // ============================
        //  SWITCH: NOTIFICACIONES
        // ============================

        private void NotificationsToggle_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Notificaciones activadas.");
        }

        private void NotificationsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Notificaciones desactivadas.");
        }

        // ============================
        //  ABRIR CARPETA DEL PROGRAMA
        // ============================

        private void OpenProgramFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", AppContext.BaseDirectory);
        }
    }
}
