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

        private void ReportProgress(int percent, string message)
        {
            _log?.Invoke($"[DB] {percent}% - {message}");
            ProgressChanged?.Invoke(percent, message);
        }

        /// <summary>
        /// Obtiene el tag de la última release de GitHub.
        /// </summary>
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
        /// Descarga y extrae el ZIP completo (ps1db.json, ps2db.json, y todos los CFG).
        /// </summary>
        public async Task DownloadAndExtractFullAsync(string cfgFolder, SettingsService settings)
        {
            ReportProgress(0, "Descargando base de datos completa...");
            string tempZip = Path.GetTempFileName();

            try
            {
                // 1. Descargar ZIP
                using (var response = await _http.GetAsync(FullDbUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = File.Create(tempZip);
                    await stream.CopyToAsync(fileStream);
                }
                ReportProgress(30, "Descarga completada. Extrayendo...");

                // 2. Extraer en carpeta temporal
                string extractDir = Path.Combine(Path.GetTempPath(), "POPSManager_DB");
                if (Directory.Exists(extractDir))
                    Directory.Delete(extractDir, true);
                Directory.CreateDirectory(extractDir);
                ZipFile.ExtractToDirectory(tempZip, extractDir);
                ReportProgress(50, "Extrayendo archivos...");

                // 3. Copiar CFGs
                string cfgSourceDir = Path.Combine(extractDir, "CFG");
                if (Directory.Exists(cfgSourceDir))
                {
                    Directory.CreateDirectory(cfgFolder);
                    int count = 0;
                    foreach (var cfgFile in Directory.GetFiles(cfgSourceDir, "*.cfg"))
                    {
                        string destFile = Path.Combine(cfgFolder, Path.GetFileName(cfgFile));
                        File.Copy(cfgFile, destFile, true);
                        count++;
                    }
                    _log?.Invoke($"[DB] {count} archivos .cfg copiados a {cfgFolder}");
                }
                ReportProgress(70, "CFGs copiados. Guardando bases de datos...");

                // 4. Guardar ps1db.json y ps2db.json en la carpeta de datos de la app
                string dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "POPSManager", "Database");
                Directory.CreateDirectory(dataDir);

                foreach (var jsonFile in new[] { "ps1db.json", "ps2db.json" })
                {
                    string src = Path.Combine(extractDir, jsonFile);
                    if (File.Exists(src))
                    {
                        string dst = Path.Combine(dataDir, jsonFile);
                        File.Copy(src, dst, true);
                        _log?.Invoke($"[DB] {jsonFile} guardado en {dst}");
                    }
                }

                // 5. Actualizar el tag guardado
                var newTag = await GetLatestReleaseTagAsync();
                if (newTag != null)
                {
                    settings.LastDbTag = newTag;
                    await settings.SaveAsync();
                }

                ReportProgress(100, "Actualización completa.");
            }
            finally
            {
                if (File.Exists(tempZip))
                    File.Delete(tempZip);
            }
        }

        /// <summary>
        /// Descarga el ZIP individual, extrae solo los .cfg de los GameIDs proporcionados.
        /// </summary>
        public async Task DownloadAndExtractFilteredAsync(IEnumerable<string> gameIds, string cfgFolder, SettingsService settings)
        {
            var ids = new HashSet<string>(gameIds.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);
            if (ids.Count == 0)
            {
                _log?.Invoke("[DB] No se encontraron Game IDs para filtrar.");
                ReportProgress(100, "No hay juegos que actualizar.");
                return;
            }

            ReportProgress(0, "Descargando base de datos individual...");
            string tempZip = Path.GetTempFileName();

            try
            {
                // 1. Descargar ZIP
                using (var response = await _http.GetAsync(IndividualDbUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = File.Create(tempZip);
                    await stream.CopyToAsync(fileStream);
                }
                ReportProgress(30, "Descarga completada. Leyendo índice...");

                // 2. Leer index.json para obtener las rutas dentro del ZIP
                using (var zip = ZipFile.OpenRead(tempZip))
                {
                    var indexEntry = zip.GetEntry("index.json");
                    if (indexEntry == null)
                    {
                        _log?.Invoke("[DB] index.json no encontrado en el ZIP individual.");
                        ReportProgress(100, "Error: índice no encontrado.");
                        return;
                    }

                    IndexData index;
                    using (var stream = indexEntry.Open())
                    {
                        index = await JsonSerializer.DeserializeAsync<IndexData>(stream) ?? new IndexData();
                    }

                    ReportProgress(50, $"Extrayendo CFGs para {ids.Count} juegos detectados...");

                    Directory.CreateDirectory(cfgFolder);
                    int extracted = 0;

                    // 3. Extraer solo los .cfg que coincidan con los Game IDs detectados
                    if (index.Cfg != null)
                    {
                        foreach (var cfgPath in index.Cfg)
                        {
                            string cfgFileName = Path.GetFileNameWithoutExtension(cfgPath);
                            if (ids.Contains(cfgFileName))
                            {
                                var entry = zip.GetEntry(cfgPath);
                                if (entry != null)
                                {
                                    string destFile = Path.Combine(cfgFolder, Path.GetFileName(cfgPath));
                                    entry.ExtractToFile(destFile, true);
                                    extracted++;
                                }
                            }
                        }
                    }

                    _log?.Invoke($"[DB] {extracted} CFGs extraídos para juegos detectados.");
                    ReportProgress(80, "CFGs extraídos. Actualizando versión...");
                }

                // 4. Actualizar el tag
                var newTag = await GetLatestReleaseTagAsync();
                if (newTag != null)
                {
                    settings.LastDbTag = newTag;
                    await settings.SaveAsync();
                }

                ReportProgress(100, "Actualización individual completada.");
            }
            finally
            {
                if (File.Exists(tempZip))
                    File.Delete(tempZip);
            }
        }

        // Modelo para deserializar index.json
        private class IndexData
        {
            public List<string>? Ps1 { get; set; }
            public List<string>? Ps2 { get; set; }
            public List<string>? Cfg { get; set; }
        }
    }
}