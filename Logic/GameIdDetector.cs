using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class GameIdDetector
    {
        // PS1: SLUS_123.45
        private static readonly Regex Ps1Regex =
            new(@"(SLUS|SCUS|SLES|SCES|SLPM|SLPS|SCPS)[-_ ]?(\d{3})[._ ]?(\d{2})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // PS2: SLUS_20312
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

                // ========================================================
                // 1) PS2 primero (IOPRP.IMG → ELF → SYSTEM.CNF)
                // ========================================================
                int rootLba = GetRootDirectoryLba(fs);
                if (rootLba > 0)
                {
                    // IOPRP.IMG (PS2)
                    var iop = FindFile(fs, rootLba, "IOPRP.IMG");
                    if (iop.lba > 0)
                    {
                        var data = ReadFileFromIso(fs, iop.lba, iop.size);
                        var id = ExtractPs2Id(data);
                        if (id != null)
                            return NormalizePs2(id);
                    }

                    // SYSTEM.CNF (PS2)
                    var sys = FindFile(fs, rootLba, "SYSTEM.CNF");
                    if (sys.lba > 0)
                    {
                        var data = ReadFileFromIso(fs, sys.lba, sys.size);
                        var id = ExtractPs2Id(data);
                        if (id != null)
                            return NormalizePs2(id);
                    }
                }

                // ELF interno (PS2)
                fs.Seek(0, SeekOrigin.Begin);
                var elfId = ScanForPs2Id(fs, 2 * 1024 * 1024);
                if (elfId != null)
                    return NormalizePs2(elfId);

                // ========================================================
                // 2) PS1 (SYSTEM.CNF → deep scan)
                // ========================================================
                if (rootLba > 0)
                {
                    var sys = FindFile(fs, rootLba, "SYSTEM.CNF");
                    if (sys.lba > 0)
                    {
                        var data = ReadFileFromIso(fs, sys.lba, sys.size);
                        var id = ExtractPs1Id(data);
                        if (id != null)
                            return NormalizePs1(id);
                    }
                }

                fs.Seek(0, SeekOrigin.Begin);
                var ps1Deep = ScanForPs1Id(fs, 2 * 1024 * 1024);
                if (ps1Deep != null)
                    return NormalizePs1(ps1Deep);
            }
            catch
            {
                // Ignorar errores
            }

            // ========================================================
            // 3) Fallback → nombre del archivo
            // ========================================================
            var nameId = DetectFromName(Path.GetFileName(path));
            if (!string.IsNullOrWhiteSpace(nameId))
            {
                if (Ps2Regex.IsMatch(nameId))
                    return NormalizePs2(nameId);

                if (Ps1Regex.IsMatch(nameId))
                    return NormalizePs1(nameId);
            }

            return null;
        }

        // ============================================================
        //  ISO9660
        // ============================================================
        private static int GetRootDirectoryLba(FileStream fs)
        {
            try
            {
                var pvd = ReadSector(fs, 16);
                return BitConverter.ToInt32(pvd, 156 + 2);
            }
            catch { return 0; }
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
        //  EXTRACCIÓN PS1
        // ============================================================
        private static string? ExtractPs1Id(byte[] data)
        {
            string text = Encoding.ASCII.GetString(data);
            var m = Ps1Regex.Match(text);
            if (m.Success)
                return $"{m.Groups[1].Value}_{m.Groups[2].Value}{m.Groups[3].Value}";
            return null;
        }

        // ============================================================
        //  EXTRACCIÓN PS2
        // ============================================================
        private static string? ExtractPs2Id(byte[] data)
        {
            string text = Encoding.ASCII.GetString(data);
            var m = Ps2Regex.Match(text);
            if (m.Success)
                return $"{m.Groups[1].Value}_{m.Groups[2].Value}";
            return null;
        }

        // ============================================================
        //  ESCANEO PROFUNDO
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
        //  DETECCIÓN DESDE NOMBRE
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
        //  NORMALIZACIÓN PS1
        // ============================================================
        private static string NormalizePs1(string id)
        {
            id = id.ToUpperInvariant()
                   .Replace("-", "_")
                   .Replace(" ", "_")
                   .Replace(".", "_");

            var m = Ps1Regex.Match(id);
            if (m.Success)
                return $"{m.Groups[1].Value}_{m.Groups[2].Value}.{m.Groups[3].Value}";

            return id;
        }

        // ============================================================
        //  NORMALIZACIÓN PS2
        // ============================================================
        private static string NormalizePs2(string id)
        {
            id = id.ToUpperInvariant()
                   .Replace("-", "_")
                   .Replace(" ", "_")
                   .Replace(".", "_");

            var m = Ps2Regex.Match(id);
            if (m.Success)
                return $"{m.Groups[1].Value}_{m.Groups[2].Value}";

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

            // ✅ FIX CA1310: Agregar StringComparison.Ordinal
            return gameId.StartsWith("SLES", StringComparison.Ordinal) ||
                   gameId.StartsWith("SCES", StringComparison.Ordinal) ||
                   gameId.StartsWith("PBPX", StringComparison.Ordinal);
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
