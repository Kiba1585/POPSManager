using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace POPSManager.UI.Notifications
{
    public partial class NotificationManager : UserControl
    {
        public NotificationManager()
        {
            InitializeComponent();
        }

        public void ShowToast(NotificationToast toast)
        {
            ToastPanel.Children.Insert(0, toast);

            // Animación de entrada
            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            toast.BeginAnimation(OpacityProperty, anim);
        }
    }
}
