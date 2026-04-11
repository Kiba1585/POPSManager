using System;
using System.IO;
using System.Text;

namespace POPSManager.Core.Integrity
{
    public sealed class ElfInfo
    {
        public string Magic { get; }
        public uint InitialPC { get; }
        public uint InitialGP { get; }

        public ElfInfo(string magic, uint initialPc, uint initialGp)
        {
            Magic = magic;
            InitialPC = initialPc;
            InitialGP = initialGp;
        }
    }

    public sealed class ElfInspector
    {
        public string Path { get; }

        public ElfInspector(string elfPath)
        {
            Path = elfPath ?? throw new ArgumentNullException(nameof(elfPath));
        }

        public (ElfInfo? Info, IntegrityReport Report) Inspect()
        {
            var report = new IntegrityReport();

            if (!File.Exists(Path))
            {
                report.AddError("ELF_NOT_FOUND", $"El ELF no existe: {Path}");
                return (null, report);
            }

            try
            {
                using var fs = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var header = new byte[0x800]; // PS-X EXE header típico
                var read = fs.Read(header, 0, header.Length);
                if (read < 0x40)
                {
                    report.AddError("ELF_HEADER_SHORT", "El header del ELF es demasiado pequeño.");
                    return (null, report);
                }

                var magic = Encoding.ASCII.GetString(header, 0, 8).TrimEnd('\0');
                if (!magic.StartsWith("PS-X EXE", StringComparison.OrdinalIgnoreCase))
                {
                    report.AddWarning("ELF_MAGIC", $"Magic inesperado en ELF: '{magic}'.");
                }
                else
                {
                    report.AddInfo("ELF_MAGIC_OK", "Header PS-X EXE válido.");
                }

                // Offsets típicos en PS-X EXE (simplificado)
                uint initialPc = BitConverter.ToUInt32(header, 0x10);
                uint initialGp = BitConverter.ToUInt32(header, 0x14);

                var info = new ElfInfo(magic, initialPc, initialGp);
                report.AddInfo("ELF_OK_BASIC", "Validación básica del ELF completada.");
                return (info, report);
            }
            catch (Exception ex)
            {
                report.AddError("ELF_EXCEPTION", $"Error leyendo ELF: {ex.Message}");
                return (null, report);
            }
        }
    }
}
