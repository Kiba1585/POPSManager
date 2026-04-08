using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class IntegrityValidator
    {
        private const int SectorSize = 2048;
        private const int HeaderSize = 0x800; // POPStarter header
        private static readonly byte[] ExpectedHeader = Encoding.ASCII.GetBytes("PSX");

        private static readonly Regex IdRegex =
            new(@"(SLUS|SCUS|SLES|SCES|SLPM|SLPS|SCPS)[-_ ]?(\d{3})[._ ]?(\d{2})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // ============================================================
        //  MÉTODO PRINCIPAL
        // ============================================================
        public static bool Validate(string vcdPath)
        {
            try
            {
                var info = new FileInfo(vcdPath);
                if (!info.Exists)
                    return false;

                // 1. Tamaño mínimo real
                if (info.Length < 1_000_000)
                    return false;

                using var fs = new FileStream(vcdPath, FileMode.Open, FileAccess.Read);

                // 2. Validar header POPStarter
                if (!ValidateHeader(fs))
                    return false;

                // 3. Validar alineación de sectores
                if (!ValidateAlignment(info.Length))
                    return false;

                // 4. Validar Primary Volume Descriptor (sector 16)
                if (!ValidatePvd(fs))
                    return false;

                // 5. Obtener root directory LBA
                int rootLba = GetRootDirectoryLba(fs);
                if (rootLba <= 0)
                    return false;

                // 6. Validar SYSTEM.CNF real
                if (!ValidateSystemCnf(fs, rootLba))
                    return false;

                // 7. Validar ID PS1/PS2 real
                if (!ValidateGameId(fs, rootLba))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        //  HEADER POPSTARTER
        // ============================================================
        private static bool ValidateHeader(FileStream fs)
        {
            byte[] header = new byte[3];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(header, 0, 3);

            return header.AsSpan().SequenceEqual(ExpectedHeader);
        }

        // ============================================================
        //  ALINEACIÓN DE SECTORES
        // ============================================================
        private static bool ValidateAlignment(long fileSize)
        {
            long dataSize = fileSize - HeaderSize;
            return dataSize > 0 && dataSize % SectorSize == 0;
        }

        // ============================================================
        //  VALIDAR PVD (Primary Volume Descriptor)
        // ============================================================
        private static bool ValidatePvd(FileStream fs)
        {
            fs.Seek(16 * SectorSize, SeekOrigin.Begin);
            byte[] pvd = new byte[SectorSize];
            fs.Read(pvd, 0, SectorSize);

            // Byte 1–5 = "CD001"
            return Encoding.ASCII.GetString(pvd, 1, 5) == "CD001";
        }

        // ============================================================
        //  LEER SECTOR
        // ============================================================
        private static byte[] ReadSector(FileStream fs, int lba, int count = 1)
        {
            byte[] buffer = new byte[SectorSize * count];
            fs.Seek(lba * SectorSize, SeekOrigin.Begin);
            fs.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        // ============================================================
        //  ROOT DIRECTORY RECORD
        // ============================================================
        private static int GetRootDirectoryLba(FileStream fs)
        {
            var pvd = ReadSector(fs, 16);
            return BitConverter.ToInt32(pvd, 156 + 2);
        }

        // ============================================================
        //  BUSCAR ARCHIVO EN ISO9660
        // ============================================================
        private static (int lba, int size) FindFile(FileStream fs, int rootLba, string target)
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

            return (0, 0);
        }

        // ============================================================
        //  VALIDAR SYSTEM.CNF REAL
        // ============================================================
        private static bool ValidateSystemCnf(FileStream fs, int rootLba)
        {
            var sys = FindFile(fs, rootLba, "SYSTEM.CNF");
            if (sys.lba <= 0)
                return false;

            var data = ReadFile(fs, sys.lba, sys.size);
            string text = Encoding.ASCII.GetString(data);

            return text.Contains("BOOT", StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        //  VALIDAR ID PS1/PS2 REAL
        // ============================================================
        private static bool ValidateGameId(FileStream fs, int rootLba)
        {
            // 1. SYSTEM.CNF
            var sys = FindFile(fs, rootLba, "SYSTEM.CNF");
            if (sys.lba > 0)
            {
                var data = ReadFile(fs, sys.lba, sys.size);
                if (IdRegex.IsMatch(Encoding.ASCII.GetString(data)))
                    return true;
            }

            // 2. IOPRP.IMG (PS2)
            var iop = FindFile(fs, rootLba, "IOPRP.IMG");
            if (iop.lba > 0)
            {
                var data = ReadFile(fs, iop.lba, iop.size);
                if (IdRegex.IsMatch(Encoding.ASCII.GetString(data)))
                    return true;
            }

            return false;
        }

        // ============================================================
        //  LEER ARCHIVO REAL
        // ============================================================
        private static byte[] ReadFile(FileStream fs, int lba, int size)
        {
            int sectors = (size + SectorSize - 1) / SectorSize;
            return ReadSector(fs, lba, sectors);
        }
    }
}
