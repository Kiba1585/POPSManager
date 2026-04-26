using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace POPSManager.Controls
{
    /// <summary>
    /// Una entrada individual del registro de actividad.
    /// </summary>
    public class LogEntry : INotifyPropertyChanged
    {
        private string _message = "";
        private string _icon = "";
        private Brush _color = Brushes.White;

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(); }
        }

        public string Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(); }
        }

        public Brush Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Panel de registro de actividad con categorías visuales.
    /// </summary>
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

        private ObservableCollection<LogEntry> _logEntries = new();

        public LogsPanel()
        {
            InitializeComponent();
            LogItemsControl.ItemsSource = _logEntries;
        }

        /// <summary>
        /// Agrega un mensaje informativo al registro.
        /// </summary>
        public void AddInfo(string message)
        {
            _logEntries.Insert(0, new LogEntry
            {
                Message = message,
                Icon = "ℹ️",
                Color = new SolidColorBrush(Color.FromRgb(180, 210, 255)) // azul claro
            });
        }

        /// <summary>
        /// Agrega un mensaje de advertencia al registro.
        /// </summary>
        public void AddWarning(string message)
        {
            _logEntries.Insert(0, new LogEntry
            {
                Message = message,
                Icon = "⚠️",
                Color = new SolidColorBrush(Color.FromRgb(255, 210, 120)) // naranja claro
            });
        }

        /// <summary>
        /// Agrega un mensaje de error al registro.
        /// </summary>
        public void AddError(string message)
        {
            _logEntries.Insert(0, new LogEntry
            {
                Message = message,
                Icon = "❌",
                Color = new SolidColorBrush(Color.FromRgb(255, 130, 130)) // rojo claro
            });
        }

        /// <summary>
        /// Agrega un mensaje de éxito al registro.
        /// </summary>
        public void AddSuccess(string message)
        {
            _logEntries.Insert(0, new LogEntry
            {
                Message = message,
                Icon = "✅",
                Color = new SolidColorBrush(Color.FromRgb(130, 255, 130)) // verde claro
            });
        }

        /// <summary>
        /// Agrega un mensaje de depuración al registro.
        /// </summary>
        public void AddDebug(string message)
        {
            _logEntries.Insert(0, new LogEntry
            {
                Message = message,
                Icon = "🔍",
                Color = new SolidColorBrush(Color.FromRgb(200, 200, 200)) // gris claro
            });
        }

        /// <summary>
        /// Limpia todos los mensajes del registro.
        /// </summary>
        public void Clear()
        {
            _logEntries.Clear();
        }
    }
}