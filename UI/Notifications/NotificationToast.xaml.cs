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
        private readonly int durationMs = 3500;

        // Evento para que NotificationManager pueda removerlo
        public event Action<NotificationToast>? Closed;

        public NotificationToast(string title, string message, NotificationType type)
        {
            InitializeComponent();

            Title.Text = title;
            Message.Text = message;

            ApplyStyle(type);

            Loaded += (_, _) => PlayShowAnimation();
        }

        // ============================================================
        //  ESTILOS POR TIPO
        // ============================================================
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

        // ============================================================
        //  ANIMACIÓN DE ENTRADA (FADE + SLIDE)
        // ============================================================
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

        // ============================================================
        //  AUTO-CLOSE (WPF DispatcherTimer)
        // ============================================================
        private void StartAutoClose()
        {
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(durationMs)
            };

            timer.Tick += (_, _) =>
            {
                timer.Stop();
                PlayHideAnimation();
            };

            timer.Start();
        }

        // ============================================================
        //  CIERRE MANUAL
        // ============================================================
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            PlayHideAnimation();
        }

        // ============================================================
        //  ANIMACIÓN DE SALIDA (FADE OUT)
        // ============================================================
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
    }
}
