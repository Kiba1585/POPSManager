using System.Collections.Generic;

namespace POPSManager.Core.Integrity
{
    public class VcdInfo
    {
        public string Path { get; set; } = "";
        public long SizeBytes { get; set; }
        public bool HeaderValid { get; set; }

        public string? GameId { get; set; }
        public string Region { get; set; } = "Unknown";

        public PvdInfo? Pvd { get; set; }

        // LBA + size por archivo
        public Dictionary<string, (int lba, int size)> Files { get; set; } = new();

        // SYSTEM.CNF leído del VCD
        public byte[]? SystemCnf { get; set; }
    }
}
