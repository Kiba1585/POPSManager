using System;
using System.IO;
using System.Net;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace POPSManager.Logic.Covers
{
    public static class ArtDownloader
    {
        public static string? DownloadArt(string gameId, string url, string artFolder, Action<string> log)
        {
            try
            {
                Directory.CreateDirectory(artFolder);

                string tempJpg = Path.Combine(artFolder, $"{gameId}.jpg");
                string artPath = Path.Combine(artFolder, $"{gameId}.ART");

                using (var client = new WebClient())
                {
                    client.DownloadFile(url, tempJpg);
                }

                // Redimensionar a 140x200 y guardar como JPG renombrado a .ART
                ResizeToArt(tempJpg, artPath);

                File.Delete(tempJpg);

                log($"[COVER] ART generado → {artPath}");
                return artPath;
            }
            catch (Exception ex)
            {
                log($"[COVER] Error generando ART: {ex.Message}");
                return null;
            }
        }

        private static void ResizeToArt(string inputPath, string outputArtPath)
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

            // Guardar como JPG pero con extensión .ART
            bmp.Save(outputArtPath, ImageFormat.Jpeg);
        }
    }
}
