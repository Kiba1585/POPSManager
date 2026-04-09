using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace POPSManager.Logic
{
    public static class MultiDiscManager
    {
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
