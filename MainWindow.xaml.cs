using POPSManager.Models;
using POPSManager.Services;
using POPSManager.Views;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Threading.Tasks;

namespace POPSManager
{
    public partial class MainWindow : Window
    {
        private readonly AppServices _services;

        public MainWindow()
        {
            InitializeComponent();

            // CORRECCIÓN: App.Services es nullable → usar !
            _services = App.Services!;

            // Notificaciones
            _services.Notifications.OnNotify = ShowNotification;

            // Logs
            _services.LogService.OnLog = AddLog;

            // Progreso global
            _services.Progress.OnStart = ProgressStart;
            _services.Progress.OnStop = ProgressStop;
            _services.Progress.OnProgress = ProgressUpdate;
            _services.Progress.OnStatus = ProgressStatus;

            // Vista inicial
            LoadView(new Dashboard());
        }

        // ============================================================
        //  NAVEGACIÓN ENTRE VISTAS
        // ============================================================
        public void LoadView(UserControl view)
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(view);

            // Animación suave
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

        // ============================================================
        //  NOTIFICACIONES (TOASTS)
        // ============================================================
        private void ShowNotification(UiNotification notification)
        {
            Dispatcher.Invoke(() =>
            {
                string styleKey = notification.Type switch
                {
                    NotificationType.Success => "ToastSuccessStyle",
                    NotificationType.Info => "ToastInfoStyle",
                    NotificationType.Warning => "ToastWarningStyle",
                    NotificationType.Error => "ToastErrorStyle",
                    _ => "ToastInfoStyle"
                };

                var toast = new Border
                {
                    Style = (Style)FindResource(styleKey),
                    RenderTransform = new TranslateTransform()
                };

                toast.Child = new TextBlock
                {
                    Text = notification.Message,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap
                };

                NotificationArea.Children.Insert(0, toast);

                // Animación de entrada
                var show = (Storyboard)FindResource("ToastShowAnimation");
                show.Begin(toast);

                // Animación de salida
                _ = Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var hide = (Storyboard)FindResource("ToastHideAnimation");
                        hide.Completed += (s, e) => NotificationArea.Children.Remove(toast);
                        hide.Begin(toast);
                    });
                });
            });
        }

        // ============================================================
        //  LOGS
        // ============================================================
        private void AddLog(string message)
        {
            Console.WriteLine(message);
        }

        // ============================================================
        //  PROGRESO GLOBAL
        // ============================================================
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
                ProgressPanelControl.UpdateStatus("Listo");
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

        // ============================================================
        //  DRAG & DROP GLOBAL
        // ============================================================
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            _services.Notifications.Show(
                new UiNotification(NotificationType.Info,
                "Arrastra archivos dentro de la vista correspondiente."));
        }
    }
}
