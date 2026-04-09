using System;
using System.Collections.Generic;
using System.Linq;

namespace POPSManager.Logic
{
    public static class DiscValidator
    {
        public static bool Validate(List<DiscInfo> discs, Action<string> log)
        {
            if (discs.Count <= 1)
            {
                log("[MultiDisc] No es multidisco.");
                return false;
            }

            // Validar números de disco
            if (discs.Any(d => d.DiscNumber <= 0))
            {
                log("[MultiDisc] ERROR: No se pudo detectar el número de disco.");
                return false;
            }

            // Validar IDs internos
            if (discs.Any(d => string.IsNullOrWhiteSpace(d.GameId)))
            {
                log("[MultiDisc] ERROR: Uno o más discos no tienen ID interno válido.");
                return false;
            }

            // Validar que todos los discos sean del mismo juego
            if (discs.Select(d => d.GameId).Distinct().Count() != 1)
            {
                log("[MultiDisc] ERROR: Los discos no pertenecen al mismo juego.");
                return false;
            }

            // Validar duplicados
            if (discs.GroupBy(d => d.DiscNumber).Any(g => g.Count() > 1))
            {
                log("[MultiDisc] ERROR: Hay discos duplicados.");
                return false;
            }

            // Validar rango
            if (discs.Count > 4)
            {
                log("[MultiDisc] ERROR: POPStarter solo soporta hasta 4 discos.");
                return false;
            }

            // Validar secuencia
            var ordered = discs.OrderBy(d => d.DiscNumber).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].DiscNumber != i + 1)
                {
                    log("[MultiDisc] ERROR: Los discos deben ser consecutivos (CD1, CD2, CD3...).");
                    return false;
                }
            }

            return true;
        }
    }
}
