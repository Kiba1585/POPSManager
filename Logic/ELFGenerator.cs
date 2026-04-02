using System;
using System.IO;
using System.Text;

namespace POPSManager.Logic
{
    public static class ElfGenerator
    {
        // Offset donde POPStarter espera el Game ID dentro del ELF
        private const int GameIdOffset = 0x2C;  

        // Offset donde POPStarter espera la ruta del VCD
        private const int VcdPathOffset = 0x100;  

        // Tamaño máximo permitido para la ruta dentro del ELF
        private const int MaxVcdPathLength = 128;

        /// <summary>
        /// Genera un ELF real basado en POPSTARTER.ELF
        /// </summary>
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

                // Leer ELF base
                byte[] elf = File.ReadAllBytes(baseElfPath);

                // ============================
                // 1. Insertar Game ID
                // ============================
                if (gameId.Length > 11)
                    gameId = gameId.Substring(0, 11);

                byte[] idBytes = Encoding.ASCII.GetBytes(gameId.PadRight(11, '\0'));

                if (GameIdOffset + idBytes.Length > elf.Length)
                {
                    log?.Invoke("ERROR: Offset de Game ID fuera de rango.");
                    return false;
                }

                Array.Copy(idBytes, 0, elf, GameIdOffset, idBytes.Length);

                // ============================
                // 2. Insertar ruta del VCD
                // ============================
                if (vcdPath.Length > MaxVcdPathLength)
                {
                    log?.Invoke("ADVERTENCIA: Ruta VCD demasiado larga. Se truncará.");
                    vcdPath = vcdPath.Substring(0, MaxVcdPathLength);
                }

                byte[] pathBytes = Encoding.ASCII.GetBytes(vcdPath.PadRight(MaxVcdPathLength, '\0'));

                if (VcdPathOffset + pathBytes.Length > elf.Length)
                {
                    log?.Invoke("ERROR: Offset de ruta VCD fuera de rango.");
                    return false;
                }

                Array.Copy(pathBytes, 0, elf, VcdPathOffset, pathBytes.Length);

                // ============================
                // 3. Guardar ELF final
                // ============================
                Directory.CreateDirectory(Path.GetDirectoryName(outputElfPath)!);
                File.WriteAllBytes(outputElfPath, elf);

                log?.Invoke($"ELF generado correctamente: {Path.GetFileName(outputElfPath)}");
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
