using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class MultiDiscManager
    {
        // ============================================================
        //  DETECTAR NÚMERO DE DISCO (ULTRA PRO)
        // ============================================================
        public static int ExtractDiscNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 1;

            input = input.ToLower();

            // CD1, CD 1, (CD1), [CD1]
            var cdMatch = Regex.Match(input, @"cd[\s\-_]*([0-9])");
            if (cdMatch.Success && int.TryParse(cdMatch.Groups[1].Value, out int cdNum))
                return cdNum;

            // DISC1, DISC 1, (DISC1)
            var discMatch = Regex.Match(input, @"disc[\s\-_]*([0-9])");
            if (discMatch.Success && int.TryParse(discMatch.Groups[1].Value, out int discNum))
                return discNum;

            // DISK1, DISK 1
            var diskMatch = Regex.Match(input, @"disk[\s\-_]*([0-9])");
            if (diskMatch.Success && int.TryParse(diskMatch.Groups[1].Value, out int diskNum))
                return diskNum;

            return 1;
        }

        // ============================================================
        //  GENERAR DISCS.TXT (TU VERSIÓN ORIGINAL)
        // ============================================================
        public static void GenerateDiscsTxt(
            string popsFolder,
            string gameId,
            List<string> discPaths,
            Action<string> log)
        {
            var discs = discPaths
                .Select(path => new DiscInfo
                {
                    Path = path,
                    DiscNumber = DiscDetector.DetectDiscNumber(path, log),
                    GameId = GameIdDetector.DetectGameId(path) ?? "",
                    FolderName = Path.GetFileName(Path.GetDirectoryName(path)) ?? "",
                    FileName = Path.GetFileName(path) ?? ""
                })
                .ToList();

            if (!DiscValidator.Validate(discs, log))
                return;

            discs = discs.OrderBy(d => d.DiscNumber).ToList();

            List<string> lines = discs
                .Select(d => $"mass:/POPS/{d.FolderName}/{d.FileName}")
                .ToList();

            foreach (var d in discs)
            {
                string folder = Path.GetDirectoryName(d.Path)!;
                string discsTxtPath = Path.Combine(folder, "DISCS.TXT");

                File.WriteAllLines(discsTxtPath, lines);
                log($"[MultiDisc] DISCS.TXT generado → {discsTxtPath}");
            }
        }
    }
}
