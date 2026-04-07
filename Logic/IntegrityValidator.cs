using System;
using System.IO;
using System.Text;

namespace POPSManager.Logic
{
    public static class IntegrityValidator
    {
        private const int MinSizeBytes = 100_000; // seguridad básica
        private const int HeaderSize = 0x800;     // header POPStarter
        private static readonly byte[] ExpectedHeader = Encoding.ASCII.GetBytes("PSX");

        public static bool Validate(string path)
        {
            if (!File.Exists(path))
                return false;

            var info = new FileInfo(path);

            // Tamaño mínimo
            if (info.Length < MinSizeBytes)
                return false;

            // Extensión válida
            if (!path.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                using var fs = File.OpenRead(path);

                // Validar header "PSX"
                byte[] header = new byte[3];
                fs.Read(header, 0, 3);

                if (!header.AsSpan().SequenceEqual(ExpectedHeader))
                    return false;

                // Validar que el tamaño total sea múltiplo de 2048 después del header
                long dataSize = info.Length - HeaderSize;

                if (dataSize <= 0 || dataSize % 2048 != 0)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
