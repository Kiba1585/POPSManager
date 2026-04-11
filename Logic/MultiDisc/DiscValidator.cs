using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace POPSManager.Logic
{
    public static class DiscValidator
    {
        public static bool Validate(List<DiscInfo> discs, Action<string> log)
        {
            // ============================================================
            // 1. Debe haber más de un disco
            // ============================================================
            if (discs.Count <= 1)
            {
                log("[MultiDisc] No es multidisco.");
                return false;
            }

            // ============================================================
            // 2. Validar números de disco detectados
            // ============================================================
            if (discs.Any(d => d.DiscNumber <= 0))
            {
                log("[MultiDisc] ERROR: No se pudo detectar el número de disco.");
                return false;
            }

            // ============================================================
            // 3. Validar duplicados
            // ============================================================
            var duplicates = discs.GroupBy(d => d.DiscNumber)
                                  .Where(g => g.Count() > 1)
                                  .ToList();

            if (duplicates.Any())
            {
                string dupList = string.Join(", ", duplicates.Select(g => $"CD{g.Key}"));
                log($"[MultiDisc] ERROR: Hay discos duplicados → {dupList}");
                return false;
            }

            // ============================================================
            // 4. Validar máximo 4 discos (POPStarter)
            // ============================================================
            if (discs.Count > 4)
            {
                log("[MultiDisc] ERROR: POPStarter solo soporta hasta 4 discos.");
                return false;
            }

            // ============================================================
            // 5. Validar secuencia consecutiva estricta
            // ============================================================
            var ordered = discs.OrderBy(d => d.DiscNumber).ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                int expected = i + 1;
                if (ordered[i].DiscNumber != expected)
                {
                    log($"[MultiDisc] ERROR: Secuencia incorrecta. Se esperaba CD{expected}, encontrado CD{ordered[i].DiscNumber}.");
                    return false;
                }
            }

            // ============================================================
            // 6. Validar que todos los discos pertenezcan al MISMO juego
            //    usando NameCleanerBase (NO GameId)
            // ============================================================
            var titles = discs
                .Select(d => NameCleanerBase.CleanTitleOnly(
                    Path.GetFileNameWithoutExtension(d.FileName)))
                .Distinct()
                .ToList();

            if (titles.Count > 1)
            {
                log("[MultiDisc] ERROR: Los discos parecen pertenecer a juegos distintos.");
                log($"[MultiDisc] Títulos detectados: {string.Join(" | ", titles)}");
                return false;
            }

            // ============================================================
            // 7. Validar carpetas CD1, CD2, CD3, CD4
            // ============================================================
            foreach (var d in discs)
            {
                if (!d.FolderName.StartsWith("CD", StringComparison.OrdinalIgnoreCase))
                {
                    log($"[MultiDisc] ERROR: Carpeta inválida para {d.FileName}. Debe ser CD1, CD2, etc.");
                    return false;
                }

                if (!int.TryParse(d.FolderName.Substring(2), out int folderNum) ||
                    folderNum != d.DiscNumber)
                {
                    log($"[MultiDisc] ERROR: Carpeta incorrecta. {d.FileName} está en {d.FolderName} pero debería estar en CD{d.DiscNumber}.");
                    return false;
                }
            }

            // ============================================================
            // 8. Validar que CD1 exista siempre
            // ============================================================
            if (!discs.Any(d => d.DiscNumber == 1))
            {
                log("[MultiDisc] ERROR: Falta CD1. Un multidisco siempre debe tener CD1.");
                return false;
            }

            // ============================================================
            // 9. Validar que no haya saltos (CD1 → CD3 sin CD2)
            // ============================================================
            var discNumbers = ordered.Select(d => d.DiscNumber).ToList();
            for (int i = 1; i <= discNumbers.Max(); i++)
            {
                if (!discNumbers.Contains(i))
                {
                    log($"[MultiDisc] ERROR: Falta CD{i}. Secuencia incompleta.");
                    return false;
                }
            }

            // ============================================================
            // 10. Validación final OK
            // ============================================================
            log("[MultiDisc] Validación multidisco completada correctamente.");
            return true;
        }
    }
}
