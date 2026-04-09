using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class GameIdDetector
    {
        // PS1: SLUS_123.45, SCES_543.21, etc.
        private static readonly Regex Ps1Regex =
            new(@"(SLUS|SCUS|SLES|SCES|SLPM|SLPS|SCPS)[-_ ]?(\d{3})[._ ]?(\d{2})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // PS2: SLUS_20312, SLES_54321, etc. (sin punto)
        private static readonly Regex Ps2Regex =
            new(@"(SLUS|SCUS|SLES|SCES|SLPM|SLPS|SCPS)[-_ ]?(\d{5})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const int SectorSize = 2048;

        // ============================================================
        //  MÉTODO PRINCIPAL (ULTRA PRO)
        // ============================================================
        public static string? DetectGameId(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);

                // 1) ISO9660 → SYSTEM.CNF (PS1 y PS2)
                int rootLba = GetRootDirectoryLba(fs);
                if (rootLba > 0)
                {
                    var sys = FindFile(fs, rootLba, "SYSTEM.CNF");
                    if (sys.lba > 0)
                    {
                        var data = ReadFileFromIso(fs, sys.lba, sys.size);
                        var id = ExtractPs1OrPs2Id(data);
                        if (id != null)
                            return Normalize(id);
                    }

                    // 2) PS2 → IOPRP.IMG
                    var iop = FindFile(fs, rootLba, "IOPRP.IMG");
                    if (iop.lba > 0)
                    {
                        var data = ReadFileFromIso(fs, iop.lba, iop.size);
                        var id = ExtractPs2Id(data);
                        if (id != null)
                            return Normalize(id);
                    }
                }

                // 3) PS2 → ELF (muy común)
                fs.Seek(0, SeekOrigin.Begin);
                var elfId = ScanForPs2Id(fs, 8 * 1024 * 1024);
                if (elfId != null)
                    return Normalize(elfId);

                // 4) PS1 → escaneo profundo (primeros 16MB)
                fs.Seek(0, SeekOrigin.Begin);
                var ps1Deep = ScanForPs1Id(fs, 16 * 1024 * 1024);
                if (ps1Deep != null)
                    return Normalize(ps1Deep);
            }
            catch
            {
                // Ignorar errores y continuar con fallback
            }

            // 5) Fallback → nombre del archivo
            var nameId = DetectFromName(Path.GetFileName(path));
            if (!string.IsNullOrWhiteSpace(nameId))
                return Normalize(nameId);

            return null;
        }

        // ============================================================
        //  ISO9660: Primary Volume Descriptor
        // ============================================================
        private static int GetRootDirectoryLba(FileStream fs)
        {
            try
            {
                var pvd = ReadSector(fs, 16);
                return BitConverter.ToInt32(pvd, 156 + 2);
            }
            catch
            {
                return 0;
            }
        }

        private static byte[] ReadSector(FileStream fs, int lba, int count = 1)
        {
            byte[] buffer = new byte[SectorSize * count];
            fs.Seek(lba * SectorSize, SeekOrigin.Begin);
            fs.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        private static (int lba, int size) FindFile(FileStream fs, int rootLba, string target)
        {
            try
            {
                var sector = ReadSector(fs, rootLba);
                int pos = 0;

                while (pos < sector.Length)
                {
                    int len = sector[pos];
                    if (len == 0)
                        break;

                    int nameLen = sector[pos + 32];
                    string name = Encoding.ASCII.GetString(sector, pos + 33, nameLen)
                        .TrimEnd(';', '1');

                    if (name.Equals(target, StringComparison.OrdinalIgnoreCase))
                    {
                        int lba = BitConverter.ToInt32(sector, pos + 2);
                        int size = BitConverter.ToInt32(sector, pos + 10);
                        return (lba, size);
                    }

                    pos += len;
                }
            }
            catch { }

            return (0, 0);
        }

        private static byte[] ReadFileFromIso(FileStream fs, int lba, int size)
        {
            int sectors = (size + SectorSize - 1) / SectorSize;
            return ReadSector(fs, lba, sectors);
        }

        // ============================================================
        //  EXTRACCIÓN DE ID (PS1 + PS2)
        // ============================================================
        private static string? ExtractPs1OrPs2Id(byte[] data)
        {
            string text = Encoding.ASCII.GetString(data);

            var ps1 = Ps1Regex.Match(text);
            if (ps1.Success)
                return $"{ps1.Groups[1].Value}_{ps1.Groups[2].Value}{ps1.Groups[3].Value}";

            var ps2 = Ps2Regex.Match(text);
            if (ps2.Success)
                return $"{ps2.Groups[1].Value}_{ps2.Groups[2].Value}";

            return null;
        }

        private static string? ExtractPs2Id(byte[] data)
        {
            string text = Encoding.ASCII.GetString(data);
            var m = Ps2Regex.Match(text);
            if (m.Success)
                return $"{m.Groups[1].Value}_{m.Groups[2].Value}";
            return null;
        }

        // ============================================================
        //  ESCANEO PROFUNDO (PS1 y PS2)
        // ============================================================
        private static string? ScanForPs1Id(FileStream fs, int bytes)
        {
            int toRead = (int)Math.Min(bytes, fs.Length);
            byte[] buffer = new byte[toRead];
            fs.Read(buffer, 0, toRead);

            string text = Encoding.ASCII.GetString(buffer);
            var m = Ps1Regex.Match(text);
            if (m.Success)
                return $"{m.Groups[1].Value}_{m.Groups[2].Value}{m.Groups[3].Value}";

            return null;
        }

        private static string? ScanForPs2Id(FileStream fs, int bytes)
        {
            int toRead = (int)Math.Min(bytes, fs.Length);
            byte[] buffer = new byte[toRead];
            fs.Read(buffer, 0, toRead);

            string text = Encoding.ASCII.GetString(buffer);
            var m = Ps2Regex.Match(text);
            if (m.Success)
                return $"{m.Groups[1].Value}_{m.Groups[2].Value}";

            return null;
        }

        // ============================================================
        //  DETECCIÓN DESDE EL NOMBRE
        // ============================================================
        public static string DetectFromName(string name)
        {
            name = name.ToUpperInvariant();

            var m1 = Ps1Regex.Match(name);
            if (m1.Success)
                return $"{m1.Groups[1].Value}_{m1.Groups[2].Value}{m1.Groups[3].Value}";

            var m2 = Ps2Regex.Match(name);
            if (m2.Success)
                return $"{m2.Groups[1].Value}_{m2.Groups[2].Value}";

            return "";
        }

        // ============================================================
        //  NORMALIZACIÓN
        // ============================================================
        private static string Normalize(string id)
        {
            id = id.ToUpperInvariant()
                   .Replace("-", "_")
                   .Replace(" ", "_")
                   .Replace(".", "_");

            // PS1 → SLUS_123.45
            var ps1 = Ps1Regex.Match(id);
            if (ps1.Success)
                return $"{ps1.Groups[1].Value}_{ps1.Groups[2].Value}.{ps1.Groups[3].Value}";

            // PS2 → SLUS_20312
            var ps2 = Ps2Regex.Match(id);
            if (ps2.Success)
                return $"{ps2.Groups[1].Value}_{ps2.Groups[2].Value}";

            return id;
        }

        // ============================================================
        //  REGIÓN
        // ============================================================
        public static bool IsPalRegion(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                return false;

            gameId = gameId.ToUpperInvariant();

            return gameId.StartsWith("SLES") ||
                   gameId.StartsWith("SCES") ||
                   gameId.StartsWith("PBPX");
        }

        // ============================================================
        //  PAL-60
        // ============================================================
        public static bool RequiresPal60(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                return false;

            string[] pal60Games =
            {
                "SLES00972", "SLES10972",
                "SLES02529",
                "SLES02205",
                "SLES01514",
                "SLES01370", "SLES11370",
                "SLES02080", "SLES12080",
                "SLES02965", "SLES12965",
                "SLES12380",
                "SCES01237",
                "SCES02105",
                "SCES02104",
                "SCES02835"
            };

            return Array.Exists(pal60Games, id => gameId.StartsWith(id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
