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
            ToastPanel.Children.Insert(0, toast);

            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            toast.BeginAnimation(OpacityProperty, anim);
        }
    }
}
