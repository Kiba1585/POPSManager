using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace POPSManager.Logic.Inspectors
{
    public static class VcdInspector
    {
        private const int SectorSize = 2048;
        private const int HeaderSize = 0x800;

        private static readonly Regex IdRegex =
            new(@"(SLUS|SCUS|SLES|SCES|SLPM|SLPS|SCPS)[-_ ]?(\d{3})[._ ]?(\d{2})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static VcdInfo Inspect(string vcdPath)
        {
            using var fs = new FileStream(vcdPath, FileMode.Open, FileAccess.Read);

            var info = new VcdInfo
            {
                Path = vcdPath,
                SizeBytes = fs.Length,
                HeaderValid = ValidateHeader(fs),
                Pvd = ReadPvd(fs),
                Files = ReadRootDirectory(fs)
            };

            info.SystemCnf = ReadFile(fs, info.Files, "SYSTEM.CNF");
            info.GameId = ExtractId(info.SystemCnf);
            info.Region = DetectRegion(info.GameId);

            return info;
        }

        private static bool ValidateHeader(FileStream fs)
        {
            byte[] header = new byte[3];
            fs.Seek(0, SeekOrigin.Begin);
            int read = fs.Read(header, 0, 3);

            if (read < 3)
                return false;

            return Encoding.ASCII.GetString(header) == "PSX";
        }

        private static string DetectRegion(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return "Unknown";

            if (id.StartsWith("SLUS", StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("SCUS", StringComparison.OrdinalIgnoreCase))
                return "NTSC-U";

            if (id.StartsWith("SLES", StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("SCES", StringComparison.OrdinalIgnoreCase))
                return "PAL";

            if (id.StartsWith("SLPM", StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("SLPS", StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("SCPS", StringComparison.OrdinalIgnoreCase))
                return "NTSC-J";

            return "Unknown";
        }

        private static string? ExtractId(byte[]? data)
        {
            if (data == null)
                return null;

            string text = Encoding.ASCII.GetString(data);
            var m = IdRegex.Match(text);
            if (!m.Success)
                return null;

            return $"{m.Groups[1].Value}_{m.Groups[2].Value}{m.Groups[3].Value}";
        }

        private static PvdInfo ReadPvd(FileStream fs)
        {
            fs.Seek(HeaderSize + 16 * SectorSize, SeekOrigin.Begin);

            byte[] pvd = new byte[SectorSize];
            int read = fs.Read(pvd, 0, SectorSize);

            if (read < SectorSize)
                return new PvdInfo();

            return new PvdInfo
            {
                Identifier = Encoding.ASCII.GetString(pvd, 1, 5),
                VolumeName = Encoding.ASCII.GetString(pvd, 40, 32).Trim(),
                SystemId = Encoding.ASCII.GetString(pvd, 8, 32).Trim()
            };
        }

        private static Dictionary<string, (int lba, int size)> ReadRootDirectory(FileStream fs)
        {
            var files = new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase);

            fs.Seek(HeaderSize + 16 * SectorSize, SeekOrigin.Begin);
            byte[] pvd = new byte[SectorSize];
            int read = fs.Read(pvd, 0, SectorSize);

            if (read < SectorSize)
                return files;

            int rootLba = BitConverter.ToInt32(pvd, 156 + 2);

            byte[] sector = ReadSector(fs, rootLba);
            int pos = 0;

            while (pos < sector.Length)
            {
                int len = sector[pos];
                if (len == 0)
                    break;

                if (pos + 33 >= sector.Length)
                    break;

                int nameLen = sector[pos + 32];
                if (pos + 33 + nameLen > sector.Length)
                    break;

                string name = Encoding.ASCII.GetString(sector, pos + 33, nameLen)
                    .TrimEnd(';', '1');

                int lba = BitConverter.ToInt32(sector, pos + 2);
                int size = BitConverter.ToInt32(sector, pos + 10);

                files[name] = (lba, size);

                pos += len;
            }

            return files;
        }

        private static byte[]? ReadFile(FileStream fs,
            Dictionary<string, (int lba, int size)> files,
            string target)
        {
            if (!files.TryGetValue(target, out var entry))
                return null;

            int sectors = (entry.size + SectorSize - 1) / SectorSize;
            return ReadSector(fs, entry.lba, sectors);
        }

        private static byte[] ReadSector(FileStream fs, int lba, int count = 1)
        {
            byte[] buffer = new byte[count * SectorSize];
            fs.Seek(HeaderSize + lba * SectorSize, SeekOrigin.Begin);
            fs.Read(buffer, 0, buffer.Length);
            return buffer;
        }
    }

    public class VcdInfo
    {
        public string Path { get; set; } = "";
        public long SizeBytes { get; set; }
        public bool HeaderValid { get; set; }

        public string? GameId { get; set; }
        public string Region { get; set; } = "Unknown";

        public PvdInfo? Pvd { get; set; }
        public Dictionary<string, (int lba, int size)> Files { get; set; } = new();
        public byte[]? SystemCnf { get; set; }
    }
}
