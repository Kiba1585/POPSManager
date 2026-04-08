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

        /// <summary>
        /// Carga una portada .ART compatible con OPL.
        /// baseFolder = paths.PopsFolder o paths.DvdFolder
        /// </summary>
        public void LoadCover(string gameId, string title, string baseFolder)
        {
            GameTitle.Text = title;
            GameId.Text = gameId;

            // Ruta OPL: /ART/{GAMEID}.ART
            string artPath = Path.Combine(baseFolder, "ART", $"{gameId}.ART");

            if (File.Exists(artPath))
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(artPath);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();

                CoverImage.Source = img;
            }
            else
            {
                // Placeholder si no existe ART
                CoverImage.Source = new BitmapImage(
                    new Uri("pack://application:,,,/POPSManager;component/Assets/placeholder.jpg"));
            }
        }
    }
}
