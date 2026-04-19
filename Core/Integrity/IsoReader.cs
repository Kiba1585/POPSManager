using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace POPSManager.Core.Integrity
{
    /// <summary>
    /// Entrada de archivo dentro de un ISO.
    /// </summary>
    public sealed class IsoFileEntry
    {
        public string Path { get; }
        public long Size { get; }

        public IsoFileEntry(string path, long size)
        {
            Path = path;
            Size = size;
        }
    }

    /// <summary>
    /// Lector de bajo nivel para archivos ISO9660.
    /// </summary>
    public sealed class IsoReader : IDisposable
    {
        private readonly FileStream _stream;
        private bool _disposed;

        public IsoReader(string isoPath)
        {
            if (!File.Exists(isoPath))
                throw new FileNotFoundException("ISO file not found", isoPath);

            _stream = new FileStream(isoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <summary>
        /// Intenta leer el Primary Volume Descriptor (PVD).
        /// </summary>
        public bool TryReadPrimaryVolumeDescriptor(out string volumeId, out IntegrityReport report)
        {
            report = new IntegrityReport();
            volumeId = string.Empty;

            try
            {
                const int sectorSize = 2048;
                const int pvdSector = 16;
                var buffer = new byte[sectorSize];

                _stream.Seek((long)pvdSector * sectorSize, SeekOrigin.Begin);
                int read = _stream.Read(buffer, 0, buffer.Length);
                if (read != sectorSize)
                {
                    report.AddError("ISO_PVD_READ", "No se pudo leer el Primary Volume Descriptor completo.");
                    return false;
                }

                if (buffer[0] != 1)
                {
                    report.AddError("ISO_PVD_TYPE", "El Primary Volume Descriptor no es de tipo 1.");
                    return false;
                }

                string id = Encoding.ASCII.GetString(buffer, 1, 5);
                if (id != "CD001")
                {
                    report.AddError("ISO_PVD_ID", $"Identificador de volumen inválido: {id}.");
                    return false;
                }

                volumeId = Encoding.ASCII.GetString(buffer, 40, 32).Trim();
                report.AddInfo("ISO_PVD_OK", $"Primary Volume Descriptor válido. Volume ID: {volumeId}.");
                return true;
            }
            catch (Exception ex)
            {
                report.AddError("ISO_PVD_EXCEPTION", $"Error leyendo el PVD: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Enumera los archivos del ISO (placeholder para futura implementación).
        /// </summary>
        public IEnumerable<IsoFileEntry> EnumerateFiles()
        {
            // Implementación real pendiente.
            yield break;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _stream.Dispose();
        }
    }
}