using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace POPSManager.Core.Integrity
{
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

        // Ejemplo: lectura mínima del Primary Volume Descriptor (no exhaustivo)
        public bool TryReadPrimaryVolumeDescriptor(out string volumeId, out IntegrityReport report)
        {
            report = new IntegrityReport();
            volumeId = string.Empty;

            try
            {
                // Sector 16 (0x10) * 2048 bytes
                const int sectorSize = 2048;
                const int pvdSector = 16;
                var buffer = new byte[sectorSize];

                _stream.Seek(pvdSector * sectorSize, SeekOrigin.Begin);
                var read = _stream.Read(buffer, 0, buffer.Length);
                if (read != sectorSize)
                {
                    report.AddError("ISO_PVD_READ", "No se pudo leer el Primary Volume Descriptor completo.");
                    return false;
                }

                // Tipo (byte 0) debe ser 1 para PVD
                if (buffer[0] != 1)
                {
                    report.AddError("ISO_PVD_TYPE", "El Primary Volume Descriptor no es de tipo 1.");
                    return false;
                }

                // Identificador "CD001" en bytes 1-5
                var id = Encoding.ASCII.GetString(buffer, 1, 5);
                if (id != "CD001")
                {
                    report.AddError("ISO_PVD_ID", $"Identificador de volumen inválido: {id}.");
                    return false;
                }

                // Volume ID en bytes 40-71 (32 bytes)
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

        // Placeholder para futura expansión: listar archivos, buscar SYSTEM.CNF, etc.
        public IEnumerable<IsoFileEntry> EnumerateFiles()
        {
            // Aquí iría una implementación real de ISO9660.
            // Por ahora devolvemos una lista vacía para no romper nada.
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
