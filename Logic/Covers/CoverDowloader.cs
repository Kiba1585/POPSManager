using System.Net;

namespace POPSManager.Logic
{
    public static class CoverDownloader
    {
        public static string? DownloadCover(string gameId, string? url, string coversFolder, Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            try
            {
                Directory.CreateDirectory(coversFolder);

                string dest = Path.Combine(coversFolder, $"{gameId}.jpg");

                using var client = new WebClient();
                client.DownloadFile(url, dest);

                log($"[COVER] Descargada portada → {dest}");
                return dest;
            }
            catch (Exception ex)
            {
                log($"[COVER] Error descargando portada: {ex.Message}");
                return null;
            }
        }
    }
}
