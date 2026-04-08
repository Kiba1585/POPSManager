using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class GameIdDetector
    {
        // Regex para detectar IDs PS1/PS2
        private static readonly Regex IdRegex =
            new(@"(SLUS|SCUS|SLES|SCES|SLPM|SLPS|SCPS)[-_ ]?(\d{3})[._ ]?(\d{2})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Prefijos para detección por nombre
        private static readonly string[] Patterns =
        {
            "SCES", "SLES", "SLUS", "SCUS", "SLPS", "SLPM", "SCPS"
        };

        private const int SectorSize = 2048;

        // ============================================================
        //  MÉTODO PRINCIPAL (ULTRA PRO)
        // ============================================================
        public static string? DetectGameId(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);

                int rootLba = GetRootDirectoryLba(fs);
                if (rootLba > 0)
                {
                    // 1. SYSTEM.CNF
                    var sys = FindFile(fs, rootLba, "SYSTEM.CNF");
                    if (sys.lba > 0)
                    {
                        var data = ReadFileFromIso(fs, sys.lba, sys.size);
                        var id = ExtractId(data);
                        if (id != null)
                            return id;
                    }

                    // 2. IOPRP.IMG (PS2 real)
                    var iop = FindFile(fs, rootLba, "IOPRP.IMG");
                    if (iop.lba > 0)
                    {
                        var data = ReadFileFromIso(fs, iop.lba, iop.size);
                        var id = ExtractId(data);
                        if (id != null)
                            return id;
                    }
                }
            }
            catch
            {
                // Ignorar y continuar con fallback
            }

            // 3. Fallback → nombre del archivo
            var nameId = DetectFromName(Path.GetFileName(path));
            if (!string.IsNullOrWhiteSpace(nameId))
                return nameId;

            return null;
        }

        // ============================================================
        //  LECTOR SECTORIAL (ISO9660)
        // ============================================================
        private static byte[] ReadSector(FileStream fs, int lba, int count = 1)
        {
            byte[] buffer = new byte[SectorSize * count];
            fs.Seek(lba * SectorSize, SeekOrigin.Begin);
            fs.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        // ============================================================
        //  LEER PRIMARY VOLUME DESCRIPTOR (sector 16)
        // ============================================================
        private static int GetRootDirectoryLba(FileStream fs)
        {
            try
            {
                var pvd = ReadSector(fs, 16);

                // Offset 156 = Directory Record del root
                int lba = BitConverter.ToInt32(pvd, 156 + 2);
                return lba;
            }
            catch
            {
                return 0;
            }
        }

        // ============================================================
        //  BUSCAR ARCHIVO EN EL DIRECTORIO (SYSTEM.CNF / IOPRP.IMG)
        // ============================================================
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

        // ============================================================
        //  LEER ARCHIVO REAL DESDE EL ISO
        // ============================================================
        private static byte[] ReadFileFromIso(FileStream fs, int lba, int size)
        {
            int sectors = (size + SectorSize - 1) / SectorSize;
            return ReadSector(fs, lba, sectors);
        }

        // ============================================================
        //  EXTRAER ID DE BYTES (SYSTEM.CNF o IOPRP.IMG)
        // ============================================================
        private static string? ExtractId(byte[] data)
        {
            string text = Encoding.ASCII.GetString(data);

            var match = IdRegex.Match(text);
            if (!match.Success)
                return null;

            string prefix = match.Groups[1].Value.ToUpper();
            string part1 = match.Groups[2].Value;
            string part2 = match.Groups[3].Value;

            return $"{prefix}_{part1}{part2}";
        }

        // ============================================================
        //  DETECCIÓN DESDE EL NOMBRE DEL ARCHIVO
        // ============================================================
        public static string DetectFromName(string name)
        {
            name = name.ToUpperInvariant();

            foreach (var p in Patterns)
            {
                int index = name.IndexOf(p);
                if (index >= 0)
                {
                    string raw = name.Substring(index);

                    raw = raw.Replace("-", "_")
                             .Replace(" ", "_")
                             .Replace(".", "_");

                    var m = IdRegex.Match(raw);
                    if (m.Success)
                    {
                        string prefix = m.Groups[1].Value;
                        string part1 = m.Groups[2].Value;
                        string part2 = m.Groups[3].Value;

                        return $"{prefix}_{part1}{part2}";
                    }
                }
            }

            return "";
        }

        // ============================================================
        //  DETECTAR SI ES PAL
        // ============================================================
        public static bool IsPalRegion(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                return false;

            return gameId.StartsWith("SLES", StringComparison.OrdinalIgnoreCase) ||
                   gameId.StartsWith("SCES", StringComparison.OrdinalIgnoreCase) ||
                   gameId.StartsWith("PBPX", StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        //  DETECTAR SI REQUIERE PAL-60
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
