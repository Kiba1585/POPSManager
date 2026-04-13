using System;
using System.IO;
using System.Net.Http;

namespace POPSManager.Logic.Covers
{
    public static class ArtDownloader
    {
        private static readonly HttpClient http = new HttpClient();

        /// <summary>
        /// Descarga una carátula JPG desde una URL, la redimensiona a formato ART
        /// y la guarda en la carpeta especificada.
        /// </summary>
        public static string? DownloadArt(string gameId, string url, string artFolder, Action<string> log)
        {
            try
            {
                Directory.CreateDirectory(artFolder);

                string tempJpg = Path.Combine(artFolder, $"{gameId}.jpg");
                string artPath = Path.Combine(artFolder, $"{gameId}.ART");

                // Descargar imagen (sin bloquear UI)
                byte[] bytes = http.GetByteArrayAsync(url).GetAwaiter().GetResult();
                File.WriteAllBytes(tempJpg, bytes);

                // Redimensionar usando ArtResizer (WPF Imaging)
                ArtResizer.ResizeToArt(tempJpg, artPath);

                // Eliminar temporal
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
    }
}
