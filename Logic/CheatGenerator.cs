using System;
using System.IO;

namespace POPSManager.Logic
{
    public static class CheatGenerator
    {
        public static void GenerateCheatTxt(string gameId, string popsDiscFolder, Action<string> log)
        {
            try
            {
                string cheatPath = Path.Combine(popsDiscFolder, "CHEAT.TXT");

                string[] lines =
                {
                    "VMODE=NTSC",
                    "BORDER=OFF",
                    "CENTER=ON"
                };

                File.WriteAllLines(cheatPath, lines);

                log($"[PS1] CHEAT.TXT generado → {cheatPath}");
            }
            catch (Exception ex)
            {
                log($"[PS1] Error generando CHEAT.TXT: {ex.Message}");
            }
        }
    }
}
