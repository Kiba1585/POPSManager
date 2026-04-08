using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace POPSManager.Logic.Covers
{
    public static class ArtResizer
    {
        public static void ResizeToArt(string inputPath, string outputArtPath)
        {
            // Cargar imagen original
            BitmapImage original = new BitmapImage();
            original.BeginInit();
            original.UriSource = new Uri(inputPath, UriKind.Absolute);
            original.CacheOption = BitmapCacheOption.OnLoad;
            original.EndInit();

            // Crear un transform para redimensionar
            var scale = new ScaleTransform(
                scaleX: 140.0 / original.PixelWidth,
                scaleY: 200.0 / original.PixelHeight
            );

            var transformed = new TransformedBitmap(original, scale);

            // Codificar como JPG (renombrado a .ART)
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 90;
            encoder.Frames.Add(BitmapFrame.Create(transformed));

            using (var fs = new FileStream(outputArtPath, FileMode.Create, FileAccess.Write))
            {
                encoder.Save(fs);
            }
        }
    }
}
