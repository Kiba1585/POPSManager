using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using POPSManager.Commands;
using POPSManager.Models;
using POPSManager.Services;
using POPSManager.Settings;
using POPSManager.UI.Notifications;
using POPSManager.UI.Windows;
using POPSManager.Views;

namespace POPSManager.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly AppServices _services;
        private NotificationManager? _notifications;

        private System.Windows.Controls.UserControl _currentView;
        private string _statusMessage = "Listo";
        private bool _isProgressVisible;
        private int _progressValue;
        private string _progressStatusText = string.Empty;

        public MainViewModel()
        {
            _services = App.Services ?? throw new InvalidOperationException("App.Services no está inicializado.");

            _currentView = new Dashboard();

            NavigateCommand = new RelayCommand<string>(Navigate);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            OpenAboutCommand = new RelayCommand(OpenAbout);
            OpenCheatsSettingsCommand = new RelayCommand(OpenCheatsSettings);

            // Forzar refresco de la vista actual al cambiar el idioma
            _services.Localization.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(_services.Localization.CurrentLanguage))
                {
                    SafeUpdate(() =>
                    {
                        var type = _currentView.GetType();
                        CurrentView = (System.Windows.Controls.UserControl)Activator.CreateInstance(type)!;
                    });
                }
            };

            SubscribeToServiceEvents();
        }

        private void SubscribeToServiceEvents()
        {
            _services.Progress.OnStart += () => SafeUpdate(() =>
            {
                IsProgressVisible = true;
                ProgressStatusText = "Iniciando...";
            });

            _services.Progress.OnStop += () => SafeUpdate(() =>
            {
                IsProgressVisible = false;
                ProgressValue = 0;
                ProgressStatusText = "Completado";
            });

            _services.Progress.OnProgress += val => SafeUpdate(() =>
            {
                ProgressValue = val;
            });

            _services.Progress.OnStatus += msg => SafeUpdate(() =>
            {
                ProgressStatusText = msg;
                StatusMessage = msg;
            });

            _services.LogService.OnLog += msg => System.Diagnostics.Debug.WriteLine(msg);
        }

        private void SafeUpdate(Action action)
        {
            try
            {
                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(action, DispatcherPriority.Normal);
                }
                else
                {
                    action();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Error en SafeUpdate: {ex.Message}");
            }
        }

        public void Initialize(NotificationManager notifications)
        {
            _notifications = notifications;
            _services.Notifications.OnShowToast = (msg, type) =>
            {
                _notifications?.Show(new UiNotification
                {
                    Type = type,
                    Message = msg
                });
            };
        }

        public System.Windows.Controls.UserControl CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsProgressVisible
        {
            get => _isProgressVisible;
            set { _isProgressVisible = value; OnPropertyChanged(); }
        }

        public int ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
        }

        public string ProgressStatusText
        {
            get => _progressStatusText;
            set { _progressStatusText = value; OnPropertyChanged(); }
        }

        public ICommand NavigateCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand OpenAboutCommand { get; }
        public ICommand OpenCheatsSettingsCommand { get; }

        private void Navigate(string destination)
        {
            switch (destination)
            {
                case "Dashboard": CurrentView = new Dashboard(); break;
                case "Convert": CurrentView = new ConvertView(); break;
                case "ProcessPops": CurrentView = new ProcessPopsView(); break;
                default: CurrentView = new Dashboard(); break;
            }
        }

        private void OpenSettings()
        {
            CurrentView = new SettingsView();
        }

        private void OpenAbout()
        {
            CurrentView = new AboutView();
        }

        private void OpenCheatsSettings()
        {
            try
            {
                var cheatSettings = new CheatSettingsService(
                    _services.Paths.RootFolder,
                    _services.LogService.Info);

                var win = new CheatSettingsWindow(cheatSettings)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                _notifications?.Show(new UiNotification
                {
                    Type = NotificationType.Error,
                    Message = $"Error abriendo configuración de cheats: {ex.Message}"
                });
            }
        }
    }
}