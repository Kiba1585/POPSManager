using System;
using System.Collections.Generic;
using System.IO;

namespace POPSManager.Core.Integrity
{
    /// <summary>
    /// Sugerencia de reparación para un VCD/ISO.
    /// </summary>
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

    /// <summary>
    /// Resultado del análisis de reparación.
    /// </summary>
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

    /// <summary>
    /// Motor de reparación de archivos VCD/ISO.
    /// Por ahora solo sugiere reparaciones; no modifica archivos automáticamente.
    /// </summary>
    public sealed class RepairEngine
    {
        /// <summary>
        /// Analiza un VCD y genera sugerencias de reparación.
        /// </summary>
        public RepairResult AnalyzeVcdForRepairs(string vcdPath)
        {
            var inspector = new VcdInspector(vcdPath);
            var report = inspector.InspectBasic();
            var suggestions = new List<RepairSuggestion>();

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
                                canAutoApply: false
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

        /// <summary>
        /// Aplica una reparación (no implementado).
        /// </summary>
        public void ApplyRepair(string vcdPath, RepairSuggestion suggestion)
        {
            throw new NotImplementedException("ApplyRepair aún no está implementado.");
        }
    }
}