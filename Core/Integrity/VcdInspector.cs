using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace POPSManager.Core.Integrity
{
    /// <summary>
    /// Inspector para archivos VCD de PS1.
    /// </summary>
    public sealed class VcdInspector
    {
        private const int SectorSize = 2048;
        private const int HeaderSize = 0x800;

        private static readonly Regex IdRegex =
            new(@"(SLUS|SCUS|SLES|SCES|SLPM|SLPS|SCPS)[-_ ]?(\d{3})[._ ]?(\d{2})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public string Path { get; }

        public VcdInspector(string vcdPath)
        {
            Path = vcdPath ?? throw new ArgumentNullException(nameof(vcdPath));
        }

        /// <summary>
        /// Validación básica usada por GameProcessor.
        /// </summary>
        public IntegrityReport InspectBasic()
        {
            var report = new IntegrityReport();

            if (!File.Exists(Path))
            {
                report.AddError("VCD_NOT_FOUND", $"El archivo VCD no existe: {Path}");
                return report;
            }

            var info = new FileInfo(Path);

            if (info.Length < 1024 * 1024)
                report.AddWarning("VCD_TOO_SMALL", $"El VCD parece demasiado pequeño ({info.Length} bytes).");

            if (info.Length % SectorSize != 0)
                report.AddWarning("VCD_ALIGNMENT", "El tamaño del VCD no es múltiplo de 2048 bytes.");

            if (!ValidateHeader())
                report.AddError("VCD_HEADER", "El header PSX no es válido.");

            report.AddInfo("VCD_OK_BASIC", "Validación básica del VCD completada.");
            return report;
        }

        /// <summary>
        /// Inspección completa del VCD.
        /// </summary>
        public VcdInfo Inspect()
        {
            using var fs = new FileStream(Path, FileMode.Open, FileAccess.Read);

            var info = new VcdInfo
            {
                Path = Path,
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

        private bool ValidateHeader()
        {
            using var fs = new FileStream(Path, FileMode.Open, FileAccess.Read);
            return ValidateHeader(fs);
        }

        private static bool ValidateHeader(FileStream fs)
        {
            byte[] header = new byte[3];
            fs.Seek(0, SeekOrigin.Begin);
            int read = fs.Read(header, 0, 3);
            return read == 3 && Encoding.ASCII.GetString(header) == "PSX";
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
            if (data == null || data.Length == 0)
                return null;

            string text = Encoding.ASCII.GetString(data);
            var m = IdRegex.Match(text);
            return m.Success ? $"{m.Groups[1].Value}_{m.Groups[2].Value}{m.Groups[3].Value}" : null;
        }

        private static PvdInfo ReadPvd(FileStream fs)
        {
            fs.Seek(HeaderSize + 16L * SectorSize, SeekOrigin.Begin);

            byte[] pvd = new byte[SectorSize];
            int read = fs.Read(pvd, 0, SectorSize);
            if (read < SectorSize)
                return new PvdInfo();

            return new PvdInfo
            {
                Identifier = SafeGetString(pvd, 1, 5),
                VolumeName = SafeGetString(pvd, 40, 32).Trim(),
                SystemId = SafeGetString(pvd, 8, 32).Trim()
            };
        }

        private static string SafeGetString(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || offset + count > buffer.Length)
                return "";
            return Encoding.ASCII.GetString(buffer, offset, count);
        }

        private static Dictionary<string, (int lba, int size)> ReadRootDirectory(FileStream fs)
        {
            var files = new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase);

            fs.Seek(HeaderSize + 16L * SectorSize, SeekOrigin.Begin);
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
                if (len == 0) break;
                if (pos + 33 >= sector.Length) break;

                int nameLen = sector[pos + 32];
                if (pos + 33 + nameLen > sector.Length) break;

                string name = Encoding.ASCII.GetString(sector, pos + 33, nameLen).TrimEnd(';', '1');
                int lba = BitConverter.ToInt32(sector, pos + 2);
                int size = BitConverter.ToInt32(sector, pos + 10);

                if (!string.IsNullOrWhiteSpace(name) && lba > 0)
                    files[name] = (lba, size);

                pos += len;
            }

            return files;
        }

        private static byte[]? ReadFile(FileStream fs, Dictionary<string, (int lba, int size)> files, string target)
        {
            if (!files.TryGetValue(target, out var entry))
                return null;

            int sectors = (entry.size + SectorSize - 1) / SectorSize;
            return ReadSector(fs, entry.lba, sectors);
        }

        private static byte[] ReadSector(FileStream fs, int lba, int count = 1)
        {
            byte[] buffer = new byte[count * SectorSize];
            fs.Seek(HeaderSize + (long)lba * SectorSize, SeekOrigin.Begin);
            int read = fs.Read(buffer, 0, buffer.Length);
            if (read < buffer.Length)
            {
                byte[] trimmed = new byte[read];
                Array.Copy(buffer, trimmed, read);
                return trimmed;
            }
            return buffer;
        }
    }
}