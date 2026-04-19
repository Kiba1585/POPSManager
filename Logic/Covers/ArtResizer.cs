using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace POPSManager.Logic.Covers
{
    /// <summary>
    /// Redimensiona imágenes al formato requerido por OPL (140x200 JPG renombrado a .ART).
    /// </summary>
    public static class ArtResizer
    {
        /// <summary>
        /// Redimensiona una imagen a 140x200 píxeles y la guarda como .ART.
        /// </summary>
        /// <param name="inputPath">Ruta de la imagen original (JPG).</param>
        /// <param name="outputArtPath">Ruta de destino del archivo .ART.</param>
        public static void ResizeToArt(string inputPath, string outputArtPath)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
                throw new ArgumentException("La ruta de entrada no puede estar vacía.", nameof(inputPath));
            if (string.IsNullOrWhiteSpace(outputArtPath))
                throw new ArgumentException("La ruta de salida no puede estar vacía.", nameof(outputArtPath));
            if (!File.Exists(inputPath))
                throw new FileNotFoundException($"No se encontró el archivo: {inputPath}");

            // Cargar imagen original
            var original = new BitmapImage();
            original.BeginInit();
            original.UriSource = new Uri(inputPath, UriKind.Absolute);
            original.CacheOption = BitmapCacheOption.OnLoad;
            original.EndInit();

            // Redimensionar a 140x200
            var scale = new ScaleTransform(
                scaleX: 140.0 / original.PixelWidth,
                scaleY: 200.0 / original.PixelHeight
            );

            var transformed = new TransformedBitmap(original, scale);

            // Codificar como JPG (extensión .ART)
            var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
            encoder.Frames.Add(BitmapFrame.Create(transformed));

            using var fs = new FileStream(outputArtPath, FileMode.Create, FileAccess.Write);
            encoder.Save(fs);
        }
    }
}