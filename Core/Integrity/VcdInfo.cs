using System.Collections.Generic;

namespace POPSManager.Core.Integrity
{
    /// <summary>
    /// Información extraída de un archivo VCD.
    /// </summary>
    public class VcdInfo
    {
        public string Path { get; set; } = "";
        public long SizeBytes { get; set; }
        public bool HeaderValid { get; set; }

        public string? GameId { get; set; }
        public string Region { get; set; } = "Unknown";

        public PvdInfo? Pvd { get; set; }

        /// <summary>Archivos dentro del VCD (nombre → (lba, size)).</summary>
        public Dictionary<string, (int lba, int size)> Files { get; set; } = new();

        /// <summary>Contenido de SYSTEM.CNF.</summary>
        public byte[]? SystemCnf { get; set; }
    }
}