using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace POPSManager.Services
{
    /// <summary>
    /// Servicio para descargar y extraer la base de datos de metadatos desde GitHub Releases.
    /// </summary>
    public class DatabaseUpdater
    {
        private readonly HttpClient _http;
        private readonly Action<string>? _log;

        // URLs fijas
        private const string FullDbUrl = "https://github.com/Kiba1585/POPSManager.DBGenerator/releases/latest/download/POPSManager_DB.zip";
        private const string IndividualDbUrl = "https://github.com/Kiba1585/POPSManager.DBGenerator/releases/latest/download/POPSManager_DB_individual.zip";
        private const string ApiUrl = "https://api.github.com/repos/Kiba1585/POPSManager.DBGenerator/releases/latest";

        // Evento para reportar progreso a la UI
        public event Action<int, string>? ProgressChanged;

        public DatabaseUpdater(Action<string>? log = null)
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("User-Agent", "POPSManager-DatabaseUpdater/1.0");
            _log = log;
        }

        /// <summary>
        /// Obtiene el tag de la última release de GitHub.
        /// </summary>
        /// <returns>El tag_name (ej. "db-11") o null si falla.</returns>
        public async Task<string?> GetLatestReleaseTagAsync()
        {
            try
            {
                var response = await _http.GetStringAsync(ApiUrl);
                using var doc = JsonDocument.Parse(response);
                var tag = doc.RootElement.GetProperty("tag_name").GetString();
                _log?.Invoke($"[DB] Última versión en GitHub: {tag}");
                return tag;
            }
            catch (Exception ex)
            {
                _log?.Invoke($"[DB] Error obteniendo versión: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Descarga el ZIP completo y lo extrae.
        /// </summary>
        /// <param name="cfgFolder">Carpeta CFG de OPL donde copiar los .cfg</param>
        /// <param name="settings">SettingsService para guardar los psXdb.json</param>
        public async Task DownloadAndExtractFullAsync(string cfgFolder, SettingsService settings)
        {
            ReportProgress(0, "Descargando base de datos completa...");
            string tempZip = Path.GetTempFileName();

            try
            {
                // Descargar
                var response = await _http.GetAsync(FullDbUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = File.Create(tempZip);
                await stream.CopyToAsync(fileStream);
                fileStream.Close();

                ReportProgress(50, "Extrayendo archivos