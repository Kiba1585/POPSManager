using POPSManager.Models;
using System;
using System.IO;
using System.Text;

namespace POPSManager.Logic
{
    public static class ElfGenerator
    {
        private const int GameIdOffset = 0x2C;
        private const int VcdPathOffset = 0x100;
        private const int MaxVcdPathLength = 128;

        private const int TitleOffset = 0x200;
        private const int MaxTitleLength = 32;

        public static bool GenerateElf(string baseElfPath,
                                       string outputElfPath,
                                       string gameId,
                                       string vcdPath,
                                       string displayTitle,
                                       Action<string>? log = null)
        {
            try
            {
                if (!File.Exists(baseElfPath))
                {
                    log?.Invoke($"ERROR: No se encontró el ELF base: {baseElfPath}");
                    return false;
                }

                byte[] elf = File.ReadAllBytes(baseElfPath);

                int minSize = Math.Max(
                    VcdPathOffset + MaxVcdPathLength,
                    TitleOffset + MaxTitleLength
                );

                if (elf.Length < minSize)
                {
                    log?.Invoke("ERROR: El ELF base es demasiado pequeño o está corrupto.");
                    return false;
                }

                gameId = gameId.ToUpperInvariant();
                if (gameId.Length > 11)
                    gameId = gameId[..11];

                byte[] idBytes = Encoding.ASCII.GetBytes(gameId.PadRight(11, '\0'));
                Array.Copy(idBytes, 0, elf, GameIdOffset, idBytes.Length);

                if (vcdPath.Length > MaxVcdPathLength)
                    vcdPath = vcdPath[..MaxVcdPathLength];

                byte[] pathBytes = Encoding.ASCII.GetBytes(vcdPath.PadRight(MaxVcdPathLength, '\0'));
                Array.Copy(pathBytes, 0, elf, VcdPathOffset, pathBytes.Length);

                if (string.IsNullOrWhiteSpace(displayTitle))
                    displayTitle = gameId;

                if (displayTitle.Length > MaxTitleLength)
                    displayTitle = displayTitle[..MaxTitleLength];

                byte[] titleBytes = Encoding.ASCII.GetBytes(displayTitle.PadRight(MaxTitleLength, '\0'));
                Array.Copy(titleBytes, 0, elf, TitleOffset, titleBytes.Length);

                Directory.CreateDirectory(Path.GetDirectoryName(outputElfPath)!);
                File.WriteAllBytes(outputElfPath, elf);

                log?.Invoke($"ELF generado correctamente: {outputElfPath}");
                return true;
            }
            catch (Exception ex)
            {
                log?.Invoke($"ERROR generando ELF: {ex.Message}");
                return false;
            }
        }
    }
}
