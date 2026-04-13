using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace POPSManager.Logic.Covers
{
    public static class ArtDownloader
    {
        private static readonly HttpClient http = new HttpClient();

        /// <summary>
        /// Versión principal async: descarga JPG, lo pasa a ART y devuelve la ruta.
        /// </summary>
        public static async Task<string?> DownloadArtAsync(
            string gameId,
            string url,
            string artFolder,
            Action<string> log)
        {
            try
            {
                Directory.CreateDirectory(artFolder);

                string tempJpg = Path.Combine(artFolder, $"{gameId}.jpg");
                string artPath = Path.Combine(artFolder, $"{gameId}.ART");

                // Descargar imagen sin bloquear hilo
                byte[] bytes = await http.GetByteArrayAsync(url).ConfigureAwait(false);
                await File.WriteAllBytesAsync(tempJpg, bytes).ConfigureAwait(false);

                // Redimensionar usando ArtResizer (síncrono, pero rápido)
                ArtResizer.ResizeToArt(tempJpg, artPath);

                if (File.Exists(tempJpg))
                    File.Delete(tempJpg);

                log($"[COVER] ART generado → {artPath}");
                return artPath;
            }
            catch (HttpRequestException ex)
            {
                log($"[COVER] Error de red descargando carátula: {ex.Message}");
                return null;
            }
            catch (IOException ex)
            {
                log($"[COVER] Error de archivo generando ART: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                log($"[COVER] Error inesperado generando ART: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Wrapper síncrono para código antiguo que aún no es async.
        /// </summary>
        public static string? DownloadArt(
            string gameId,
            string url,
            string artFolder,
            Action<string> log)
        {
            return DownloadArtAsync(gameId, url, artFolder, log)
                .GetAwaiter()
                .GetResult();
        }
    }
}
