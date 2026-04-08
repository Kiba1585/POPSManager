using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using POPSManager.Services;
using POPSManager.Models;

namespace POPSManager.Views
{
    public partial class AboutView : UserControl
    {
        private readonly AppServices _services;

        public AboutView()
        {
            InitializeComponent();
            _services = App.Services!;
        }

        // ============================================================
        //  ABRIR URL SEGURA
        // ============================================================
        private void OpenUrl(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    _services.Notifications.Error("La URL no está configurada.");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _services.Notifications.Error("No se pudo abrir el enlace.");
                _services.LogService.Error($"[AboutView] Error abriendo URL: {ex.Message}");
            }
        }

        // ============================================================
        //  BOTONES
        // ============================================================
        private void OpenGitHub_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/"); // Reemplaza con tu repo real
        }

        private void OpenWebsite_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://example.com/"); // Reemplaza con tu web real
        }
    }
}
