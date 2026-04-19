using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using POPSManager.Models;

namespace POPSManager.UI.Notifications
{
    public partial class NotificationToast : UserControl
    {
        private readonly int _durationMs = 3500;
        private DispatcherTimer? _timer;

        public event Action<NotificationToast>? Closed;

        public NotificationToast(string title, string message, NotificationType type)
        {
            InitializeComponent();

            Title.Text = title;
            Message.Text = message;

            ApplyStyle(type);

            Loaded += (_, _) => PlayShowAnimation();
        }

        private void ApplyStyle(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success:
                    Root.Background = new SolidColorBrush(Color.FromRgb(40, 120, 40));
                    Icon.Text = "✔";
                    break;

                case NotificationType.Info:
                    Root.Background = new SolidColorBrush(Color.FromRgb(40, 70, 140));
                    Icon.Text = "ℹ";
                    break;

                case NotificationType.Warning:
                    Root.Background = new SolidColorBrush(Color.FromRgb(180, 140, 40));
                    Icon.Text = "⚠";
                    break;

                case NotificationType.Error:
                    Root.Background = new SolidColorBrush(Color.FromRgb(160, 40, 40));
                    Icon.Text = "✖";
                    break;
            }
        }

        private void PlayShowAnimation()
        {
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var slide = new ThicknessAnimation(
                new Thickness(0, 20, 0, 10),
                new Thickness(0, 0, 0, 10),
                TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            BeginAnimation(OpacityProperty, fade);
            Root.BeginAnimation(MarginProperty, slide);

            StartAutoClose();
        }

        private void StartAutoClose()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_durationMs)
            };

            _timer.Tick += (_, _) =>
            {
                _timer.Stop();
                PlayHideAnimation();
            };

            _timer.Start();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            PlayHideAnimation();
        }

        private void PlayHideAnimation()
        {
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            fade.Completed += (_, _) =>
            {
                Closed?.Invoke(this);
            };

            BeginAnimation(OpacityProperty, fade);
        }

        /// <summary>
        /// Cierra el toast inmediatamente sin animación.
        /// </summary>
        public void CloseImmediately()
        {
            _timer?.Stop();
            Closed?.Invoke(this);
        }
    }
}