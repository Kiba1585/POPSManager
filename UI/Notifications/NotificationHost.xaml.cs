using System;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace POPSManager.UI.Notifications
{
    public partial class NotificationHost : UserControl
    {
        public NotificationHost()
        {
            InitializeComponent();
        }

        public void ShowToast(NotificationToast toast)
        {
            // Limitar a 5 toasts visibles
            while (ToastPanel.Children.Count >= 5)
            {
                if (ToastPanel.Children[0] is NotificationToast oldest)
                {
                    oldest.CloseImmediately();
                    ToastPanel.Children.RemoveAt(0);
                }
            }

            ToastPanel.Children.Insert(0, toast);

            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            toast.BeginAnimation(OpacityProperty, anim);
        }

        public void RemoveToast(NotificationToast toast)
        {
            ToastPanel.Children.Remove(toast);
        }
    }
}