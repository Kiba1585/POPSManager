using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace POPSManager.UI.Notifications
{
    public static class Notifier
    {
        private static Panel? _container;

        /// <summary>
        /// Inicializa el contenedor donde se mostrarán los toasts.
        /// Debe llamarse desde MainWindow.xaml.cs después de InitializeComponent().
        /// </summary>
        public static void Initialize(Panel container)
        {
            _container = container;
        }

        /// <summary>
        /// Muestra un toast en pantalla.
        /// </summary>
        public static void ShowToast(NotificationToast toast)
        {
            if (_container == null)
                return;

            // Insertar arriba
            _container.Children.Insert(0, toast);

            // Animación de entrada
            var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(250)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            toast.BeginAnimation(UIElement.OpacityProperty, fade);
        }
    }
}
