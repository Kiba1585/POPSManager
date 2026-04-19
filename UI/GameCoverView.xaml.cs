using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace POPSManager.UI
{
    public partial class GameCoverView : UserControl
    {
        public GameCoverView()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty GameIdProperty =
            DependencyProperty.Register(nameof(GameId), typeof(string), typeof(GameCoverView),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty GameTitleProperty =
            DependencyProperty.Register(nameof(GameTitle), typeof(string), typeof(GameCoverView),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty CoverImageSourceProperty =
            DependencyProperty.Register(nameof(CoverImageSource), typeof(ImageSource), typeof(GameCoverView),
                new PropertyMetadata(null));

        public string GameId
        {
            get => (string)GetValue(GameIdProperty);
            set => SetValue(GameIdProperty, value);
        }

        public string GameTitle
        {
            get => (string)GetValue(GameTitleProperty);
            set => SetValue(GameTitleProperty, value);
        }

        public ImageSource CoverImageSource
        {
            get => (ImageSource)GetValue(CoverImageSourceProperty);
            set => SetValue(CoverImageSourceProperty, value);
        }
    }
}