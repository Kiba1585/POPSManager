using System;
using System.Collections.Generic;
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
            // 2. Validar números de disco
            // ============================================================
            if (discs.Any(d => d.DiscNumber <= 0))
            {
                log("[MultiDisc] ERROR: No se pudo detectar el número de disco.");
                return false;
            }

            // ============================================================
            // 3. Validar duplicados
            // ============================================================
            if (discs.GroupBy(d => d.DiscNumber).Any(g => g.Count() > 1))
            {
                log("[MultiDisc] ERROR: Hay discos duplicados.");
                return false;
            }

            // ============================================================
            // 4. Validar máximo 4 discos
            // ============================================================
            if (discs.Count > 4)
            {
                log("[MultiDisc] ERROR: POPStarter solo soporta hasta 4 discos.");
                return false;
            }

            // ============================================================
            // 5. Validar secuencia consecutiva
            // ============================================================
            var ordered = discs.OrderBy(d => d.DiscNumber).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].DiscNumber != i + 1)
                {
                    log("[MultiDisc] ERROR: Los discos deben ser consecutivos (CD1, CD2, CD3...).");
                    return false;
                }
            }

            // ============================================================
            // 6. Validar que todos los discos pertenezcan al mismo juego
            //    usando NameCleanerBase (NO GameId)
            // ============================================================
            var titles = discs
                .Select(d => NameCleanerBase.CleanTitleOnly(
                    System.IO.Path.GetFileNameWithoutExtension(d.FileName)))
                .Distinct()
                .ToList();

            if (titles.Count > 1)
            {
                log("[MultiDisc] ERROR: Los discos parecen pertenecer a juegos distintos.");
                return false;
            }

            return true;
        }
    }
}
