using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace POPSManager.UI.Notifications
{
    public partial class NotificationToast : UserControl
    {
        private readonly int durationMs = 3500;

        public NotificationToast(string title, string message, NotificationType type)
        {
            InitializeComponent();

            Title.Text = title;
            Message.Text = message;

            ApplyStyle(type);

            Loaded += (s, e) => StartAutoClose();
        }

        private void ApplyStyle(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success:
                    Root.Background = new SolidColorBrush(Color.FromRgb(30, 70, 30));
                    Icon.Text = "✔";
                    break;

                case NotificationType.Info:
                    Root.Background = new SolidColorBrush(Color.FromRgb(30, 50, 90));
                    Icon.Text = "ℹ";
                    break;

                case NotificationType.Warning:
                    Root.Background = new SolidColorBrush(Color.FromRgb(90, 70, 20));
                    Icon.Text = "⚠";
                    break;

                case NotificationType.Error:
                    Root.Background = new SolidColorBrush(Color.FromRgb(90, 20, 20));
                    Icon.Text = "✖";
                    break;
            }
        }

        private void StartAutoClose()
        {
            var timer = new System.Timers.Timer(durationMs);
            timer.Elapsed += (s, e) =>
            {
                timer.Stop();
                Dispatcher.Invoke(Close);
            };
            timer.Start();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Close()
        {
            var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
            anim.Completed += (s, e) =>
            {
                var parent = this.Parent as Panel;
                parent?.Children.Remove(this);
            };
            BeginAnimation(OpacityProperty, anim);
        }
    }
}
