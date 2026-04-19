using System;
using System.Diagnostics;
using System.Windows.Input;
using POPSManager.Commands;
using POPSManager.Services;

namespace POPSManager.ViewModels
{
    public class AboutViewModel : ViewModelBase
    {
        private readonly AppServices _services;

        public AboutViewModel()
        {
            _services = App.Services!;

            OpenGitHubCommand = new RelayCommand(OpenGitHub);
            OpenWebsiteCommand = new RelayCommand(OpenWebsite);
        }

        public ICommand OpenGitHubCommand { get; }
        public ICommand OpenWebsiteCommand { get; }

        private void OpenGitHub()
        {
            OpenUrl("https://github.com/"); // Reemplazar por URL real
        }

        private void OpenWebsite()
        {
            OpenUrl("https://example.com/"); // Reemplazar por URL real
        }

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
                _services.LogService.Error($"[AboutViewModel] Error abriendo URL: {ex.Message}");
            }
        }
    }
}