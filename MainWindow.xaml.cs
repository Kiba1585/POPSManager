using POPSManager.UI.Notifications;
using POPSManager.Models;
using POPSManager.Services;
using POPSManager.Views;
using POPSManager.Logic;
using POPSManager.Settings;
using POPSManager.UI.Windows;
using POPSManager.UI.Localization;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace POPSManager
{
    public partial class MainWindow : Window
    {
        private readonly AppServices _services;

        public static NotificationManager Notifications { get; private set; } = null!;

        public MainWindow()
        {
            InitializeComponent();

            Notifications = new NotificationManager(Notifier);

            _services = App.Services!;

            _services.Notifications.OnShowToast = (msg, type) =>
            {
                Notifications.Show(new UiNotification
                {
                    Type = type,
                    Message = msg
                });
            };

            _services.LogService.OnLog = AddLog;

            _services.Progress.OnStart = ProgressStart;
            _services.Progress.OnStop = ProgressStop;
            _services.Progress.OnProgress = ProgressUpdate;
            _services.Progress.OnStatus = ProgressStatus;

            LoadView(new Dashboard());
        }

        public void LoadView(System.Windows.Controls.UserControl view)
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(view);

            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            view.BeginAnimation(OpacityProperty, fade);
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e) => LoadView(new Dashboard());
        private void Convert_Click(object sender, RoutedEventArgs e) => LoadView(new ConvertView());
        private void ProcessPops_Click(object sender, RoutedEventArgs e) => LoadView(new ProcessPopsView());
        private void Settings_Click(object sender, RoutedEventArgs e) => LoadView(new SettingsView());
        private void About_Click(object sender, RoutedEventArgs e) => LoadView(new AboutView());

        private void CheatsSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cheatSettings = new CheatSettingsService(
                    _services.Paths.RootFolder,
                    _services.LogService.Info
                );

                var win = new CheatSettingsWindow(cheatSettings)
                {
                    Owner = this
                };

                win.ShowDialog();
            }
            catch (Exception ex)
            {
                Notifications.Show(new UiNotification
                {
                    Type = NotificationType.Error,
                    Message = $"Error abriendo configuración de cheats: {ex.Message}"
                });
            }
        }

        private void AddLog(string message)
        {
            Console.WriteLine(message);
        }

        private void ProgressStart()
        {
            Dispatcher.Invoke(() =>
            {
                ProgressPanelControl.Visibility = Visibility.Visible;
                ProgressPanelControl.StartSpinner();
            });
        }

        private void ProgressStop()
        {
            Dispatcher.Invoke(() =>
            {
                ProgressPanelControl.StopSpinner();
                ProgressPanelControl.UpdateProgress(0);
                ProgressPanelControl.UpdateStatus(LocalizationService.T("Completed"));
                ProgressPanelControl.Visibility = Visibility.Collapsed;
            });
        }

        private void ProgressUpdate(int value)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressPanelControl.UpdateProgress(value);
            });
        }

        private void ProgressStatus(string text)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressPanelControl.UpdateStatus(text);
                StatusText.Text = text;
            });
        }

        private void Window_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            Notifications.Show(new UiNotification
            {
                Type = NotificationType.Info,
                Message = "Arrastra archivos dentro de la vista correspondiente."
            });
        }
    }
}