using System.Windows;
using System.Windows.Controls;

namespace POPSManager.Controls
{
    public partial class LogsPanel : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(LogsPanel),
                new PropertyMetadata("Registro de actividad"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public LogsPanel()
        {
            InitializeComponent();
        }

        // Aquí irían los métodos para agregar logs (AddLog, etc.)
    }
}