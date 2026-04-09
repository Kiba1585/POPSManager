using POPSManager.UI.Notifications;
using POPSManager.Models;
using POPSManager.Services;
using POPSManager.Views;
using POPSManager.Logic;
using POPSManager.UI;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace POPSManager
{
    public partial class MainWindow : Window
    {
        private readonly AppServices _services;

        // Notificaciones globales accesibles desde cualquier módulo
        public static NotificationManager Notifications { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            // ============================================================
            //  NOTIFICATION MANAGER (ULTRA PRO)
            // ============================================================
            Notifications = new NotificationManager(ToastContainer);

            _services = App.Services!;

            // Conectar servicios internos a las notificaciones
            _services.Notifications.OnShowToast = (msg, type) =>
            {
                Notifications.Show(new UiNotification(type, msg));
            };

            // ============================================================
            //  LOGS
            // ============================================================
            _services.LogService.OnLog = AddLog;

            // ============================================================
            //  PROGRESO GLOBAL
            // ============================================================
            _services.Progress.OnStart = ProgressStart;
            _services.Progress.OnStop = ProgressStop;
            _services.Progress.OnProgress = ProgressUpdate;
            _services.Progress.OnStatus = ProgressStatus;

            // ============================================================
            //  VISTA INICIAL
            // ============================================================
            LoadView(new Dashboard());
        }

        // ============================================================
        //  NAVEGACIÓN ENTRE VISTAS
        // ============================================================
        public void LoadView(UserControl view)
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
            Notifications.Show(new UiNotification(
                NotificationType.Info,
                "Arrastra archivos dentro de la vista correspondiente."
            ));
        }
    }
}
