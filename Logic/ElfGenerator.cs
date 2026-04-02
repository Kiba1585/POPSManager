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

        public static bool GenerateElf(string baseElfPath,
                                       string outputElfPath,
                                       string gameId,
                                       string vcdPath,
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

                if (gameId.Length > 11)
                    gameId = gameId.Substring(0, 11);

                byte[] idBytes = Encoding.ASCII.GetBytes(gameId.PadRight(11, '\0'));

                Array.Copy(idBytes, 0, elf, GameIdOffset, idBytes.Length);

                if (vcdPath.Length > MaxVcdPathLength)
                    vcdPath = vcdPath.Substring(0, MaxVcdPathLength);

                byte[] pathBytes = Encoding.ASCII.GetBytes(vcdPath.PadRight(MaxVcdPathLength, '\0'));

                Array.Copy(pathBytes, 0, elf, VcdPathOffset, pathBytes.Length);

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
