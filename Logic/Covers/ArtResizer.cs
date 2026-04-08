using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace POPSManager.Logic.Covers
{
    public static class ArtResizer
    {
        public static void ResizeToArt(string inputPath, string outputArtPath)
        {
            using var original = Image.FromFile(inputPath);
            using var bmp = new Bitmap(140, 200);

            using (var g = Graphics.FromImage(bmp))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(original, 0, 0, 140, 200);
            }

            bmp.Save(outputArtPath, ImageFormat.Jpeg);
        }
    }
}
