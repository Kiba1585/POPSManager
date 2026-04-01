using DiscUtils.Iso9660;
using System;
using System.IO;
using System.Linq;

namespace POPSManager.Logic
{
    public static class GameIdDetector
    {
        private static readonly string[] ValidPrefixes =
        {
            "SLUS_", "SCUS_", "SLES_", "SCES_", "SLPS_", "SLPM_", "SCPS_"
        };

        public static string DetectGameId(string vcdPath)
        {
            if (!File.Exists(vcdPath))
                throw new FileNotFoundException("El archivo VCD no existe.", vcdPath);

            using var stream = File.OpenRead(vcdPath);
            using var cd = new CDReader(stream, true);

            var files = cd.GetFiles("/", "*.*", SearchOption.AllDirectories);

            var exe = files.FirstOrDefault(f =>
                ValidPrefixes.Any(prefix =>
                    Path.GetFileName(f).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));

            if (exe == null)
                return null;

            string fileName = Path.GetFileNameWithoutExtension(exe);

            // Normaliza: SCES_01234 → SCES-01234
            return fileName.Replace("_", "-").Replace(".", "");
        }
    }
}
