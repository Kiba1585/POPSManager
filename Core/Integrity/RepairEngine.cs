using System;
using System.IO;

namespace POPSManager.Core.Integrity
{
    public sealed class RepairSuggestion
    {
        public string Code { get; }
        public string Description { get; }
        public bool CanAutoApply { get; }

        public RepairSuggestion(string code, string description, bool canAutoApply)
        {
            Code = code;
            Description = description;
            CanAutoApply = canAutoApply;
        }

        public override string ToString() =>
            $"{Code} - {Description} (Auto: {(CanAutoApply ? "Sí" : "No")})";
    }

    public sealed class RepairResult
    {
        public IntegrityReport Report { get; }
        public RepairSuggestion[] Suggestions { get; }

        public RepairResult(IntegrityReport report, RepairSuggestion[] suggestions)
        {
            Report = report;
            Suggestions = suggestions;
        }
    }

    public sealed class RepairEngine
    {
        // IMPORTANTE: este motor NO modifica nada in-place sin que tú lo decidas.
        // Devuelve sugerencias y, si quieres, puedes implementar Apply() aparte.

        public RepairResult AnalyzeVcdForRepairs(string vcdPath)
        {
            var inspector = new VcdInspector(vcdPath);
            var report = inspector.InspectBasic();
            var suggestions = new System.Collections.Generic.List<RepairSuggestion>();

            if (report.HasErrors || report.HasWarnings)
            {
                foreach (var issue in report.Issues)
                {
                    switch (issue.Code)
                    {
                        case "VCD_ALIGNMENT":
                            suggestions.Add(new RepairSuggestion(
                                "FIX_ALIGNMENT",
                                "Recrear el VCD con tamaño alineado a 2048 bytes.",
                                canAutoApply: false // requiere recreación, no parche simple
                            ));
                            break;

                        case "VCD_TOO_SMALL":
                            suggestions.Add(new RepairSuggestion(
                                "CHECK_SOURCE",
                                "Verificar la fuente del VCD. Parece truncado o incompleto.",
                                canAutoApply: false
                            ));
                            break;
                    }
                }
            }

            return new RepairResult(report, suggestions.ToArray());
        }

        // Hook para futuras reparaciones automáticas (SYSTEM.CNF, header POPStarter, etc.)
        public void ApplyRepair(string vcdPath, RepairSuggestion suggestion)
        {
            // Aquí NO hacemos nada todavía para no tocar datos.
            // La idea es que tú decidas qué reparaciones quieres permitir
            // y cómo implementarlas (copias temporales, backups, etc.).
            throw new NotImplementedException("ApplyRepair aún no está implementado.");
        }
    }
}
