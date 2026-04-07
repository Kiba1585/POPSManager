using DiscUtils.Iso9660;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class GameIdDetector
    {
        private static readonly Regex IdRegex =
            new Regex(@"(SLUS|SCUS|SLES|SCES|SLPS|SLPM|SCPS)[\-_\.]?(\d{3,5})",
                      RegexOptions.IgnoreCase);

        public static string? DetectGameId(string vcdPath)
        {
            if (!File.Exists(vcdPath))
                throw new FileNotFoundException("El archivo VCD no existe.", vcdPath);

            try
            {
                using var stream = File.OpenRead(vcdPath);
                using var cd = new CDReader(stream, true);

                var files = cd.GetFiles("/", "*.*", SearchOption.AllDirectories);

                // Buscar cualquier archivo que contenga un ID válido
                foreach (var file in files)
                {
                    string name = Path.GetFileName(file);

                    var match = IdRegex.Match(name);
                    if (match.Success)
                    {
                        string prefix = match.Groups[1].Value.ToUpperInvariant();
                        string number = match.Groups[2].Value.PadLeft(5, '0');

                        // Formato final: SCES-01234
                        return $"{prefix}-{number}";
                    }
                }

                return null;
            }
            catch
            {
                // Si el VCD está corrupto o no es ISO9660 válido
                return null;
            }
        }
    }
}
