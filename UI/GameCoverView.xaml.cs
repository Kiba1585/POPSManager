using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace POPSManager.UI
{
    public partial class GameCoverView : UserControl
    {
        public GameCoverView()
        {
            InitializeComponent();
        }

        public void LoadCover(string gameId, string title, string popsFolder)
        {
            GameTitle.Text = title;
            GameId.Text = gameId;

            string coverPath = Path.Combine(popsFolder, "COVERS", $"{gameId}.jpg");

            if (File.Exists(coverPath))
            {
                CoverImage.Source = new BitmapImage(new Uri(coverPath));
            }
            else
            {
                CoverImage.Source = new BitmapImage(
                    new Uri("pack://application:,,,/POPSManager;component/Assets/placeholder.jpg"));
            }
        }
    }
}
