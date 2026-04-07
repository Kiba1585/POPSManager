using System.IO;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class GameIdDetector
    {
        private static readonly string[] Patterns =
        {
            "SCES", "SLES", "SLUS", "SCUS", "SLPS", "SLPM", "SCPS"
        };

        public static string? DetectGameId(string vcdPath)
        {
            // Aquí podrías leer sectores del VCD si deseas detección real
            return null;
        }

        public static string DetectFromName(string name)
        {
            name = name.ToUpperInvariant();

            foreach (var p in Patterns)
            {
                if (name.Contains(p))
                {
                    int index = name.IndexOf(p);
                    string id = name.Substring(index);

                    id = id.Replace("-", "_")
                           .Replace(" ", "_");

                    if (id.Length > 12)
                        id = id.Substring(0, 12);

                    return id;
                }
            }

            return "";
        }
    }
}
