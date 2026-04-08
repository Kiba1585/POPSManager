using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace POPSManager.Logic.Covers
{
    public static class ArtDownloader
    {
        private static readonly HttpClient http = new HttpClient();

        public static string? DownloadArt(string gameId, string url, string artFolder, Action<string> log)
        {
            try
            {
                Directory.CreateDirectory(artFolder);

                string tempJpg = Path.Combine(artFolder, $"{gameId}.jpg");
                string artPath = Path.Combine(artFolder, $"{gameId}.ART");

                // Descargar imagen con HttpClient
                var bytes = http.GetByteArrayAsync(url).Result;
                File.WriteAllBytes(tempJpg, bytes);

                // Redimensionar usando ArtResizer (WPF Imaging)
                ArtResizer.ResizeToArt(tempJpg, artPath);

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
    }
}
