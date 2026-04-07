using System;
using System.IO;
using System.Text;

namespace POPSManager.Logic
{
    public static class IntegrityValidator
    {
        private const int MinSizeBytes = 100_000;
        private const int HeaderSize = 0x800;
        private static readonly byte[] ExpectedHeader = Encoding.ASCII.GetBytes("PSX");

        public static bool Validate(string path)
        {
            if (!File.Exists(path))
                return false;

            var info = new FileInfo(path);

            if (info.Length < MinSizeBytes)
                return false;

            if (!path.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                using var fs = File.OpenRead(path);

                byte[] header = new byte[3];
                fs.Read(header, 0, 3);

                if (!header.AsSpan().SequenceEqual(ExpectedHeader))
                    return false;

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
